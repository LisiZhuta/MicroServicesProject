using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly OrderContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public OrderController(OrderContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Order>>> Get()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("User ID is missing in the token.");
            }
            var userIdInt = int.Parse(userId);
            return await _context.Orders.Include(o => o.Items).Where(o => o.UserId == userIdInt).ToListAsync();
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> Get(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("User ID is missing in the token.");
            }
            var userIdInt = int.Parse(userId);
            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id && o.UserId == userIdInt);
            if (order == null)
            {
                return NotFound($"Order with ID {id} doesn't exist");
            }
            return order;
        }

 [Authorize]
[HttpPost]
public async Task<ActionResult<Order>> Create(Order order)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null)
    {
        return Unauthorized("User ID is missing in the token.");
    }
    order.UserId = int.Parse(userId);

    var total = 0m;

    // Calculate the total cost of the order
    foreach (var item in order.Items)
    {
        var productExists = await CheckProductExistsAsync(item.ProductId);
        if (!productExists)
        {
            return BadRequest($"Product with ID {item.ProductId} does not exist.");
        }

        var price = await GetProductPriceAsync(item.ProductId);
        if (price == null)
        {
            return BadRequest($"Could not fetch price for product ID {item.ProductId}");
        }

        item.Order = order; // Associate the Order with OrderItems
        total += item.Quantity * price.Value;
    }

    // Check user's balance
    var userBalance = await GetUserBalanceAsync();
    if (userBalance == null)
    {
        return BadRequest("Unable to retrieve user balance.");
    }

    // Verify if user has enough balance
    if (userBalance < total)
    {
        return BadRequest($"Insufficient balance. Available: {userBalance}, Required: {total}");
    }

    // Deduct user's balance first
    var balanceDeducted = await DeductUserBalanceAsync(order.UserId, total);
    if (!balanceDeducted)
    {
        return BadRequest("Could not deduct balance.");
    }

    // Reduce the inventory after confirming balance deduction
    foreach (var item in order.Items)
    {
        var productQuantity = await GetProductQuantityAsync(item.ProductId);
        if (productQuantity == null || productQuantity < item.Quantity)
        {
            // Revert the balance deduction if inventory reduction fails
            await RefundUserBalanceAsync(order.UserId, total);
            return BadRequest($"Insufficient quantity for product ID {item.ProductId}. Available quantity: {productQuantity}");
        }

        var reduced = await ReduceInventoryQuantityAsync(item.ProductId, item.Quantity);
        if (!reduced)
        {
            // Revert the balance deduction if inventory reduction fails
            await RefundUserBalanceAsync(order.UserId, total);
            return BadRequest($"Could not reduce quantity for product ID {item.ProductId}");
        }
    }

    order.Total = total;

    _context.Orders.Add(order);
    await _context.SaveChangesAsync();
    return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
}



        [Authorize]
[HttpDelete("{id}")]
public async Task<IActionResult> Cancel(int id)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null)
    {
        return Unauthorized("User ID is missing in the token.");
    }
    var userIdInt = int.Parse(userId);

    var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id && o.UserId == userIdInt);
    if (order == null)
    {
        return BadRequest($"Order with ID {id} doesn't exist");
    }

    // Calculate the total amount to refund
    var totalRefundAmount = order.Total;

    // Refund the balance
    var balanceRefunded = await RefundUserBalanceAsync(order.UserId, totalRefundAmount);
    if (!balanceRefunded)
    {
        return BadRequest("Could not refund balance.");
    }

    // Add back the quantities to inventory
    foreach (var item in order.Items)
    {
        var increased = await IncreaseInventoryQuantityAsync(item.ProductId, item.Quantity);
        if (!increased)
        {
            return BadRequest($"Could not increase quantity for product ID {item.ProductId}");
        }
    }

    _context.Orders.Remove(order);
    await _context.SaveChangesAsync();

    return NoContent();
}


        private async Task<decimal?> GetProductPriceAsync(int productId)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"http://localhost:5284/api/product/{productId}"); // Replace with actual URL
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var product = JsonSerializer.Deserialize<ProductDTO>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return product?.Price;
            }

            return null;
        }

        private async Task<bool> CheckProductExistsAsync(int productId)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"http://localhost:5284/api/product/{productId}"); // Replace with actual URL
            return response.IsSuccessStatusCode;
        }

        private async Task<int?> GetProductQuantityAsync(int productId)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"http://localhost:5137/api/inventory/product/{productId}"); // Replace with actual URL and port
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var inventoryItem = JsonSerializer.Deserialize<InventoryItemDTO>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return inventoryItem?.Quantity;
            }

            return null;
        }

        private async Task<bool> ReduceInventoryQuantityAsync(int productId, int quantity)
        {
            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(new { ProductId = productId, Quantity = quantity }), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"http://localhost:5137/api/inventory/reduce", content); // Replace with actual URL and port
            return response.IsSuccessStatusCode;
        }
        private async Task<bool> IncreaseInventoryQuantityAsync(int productId, int quantity)
        {
            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(new { ProductId = productId, Quantity = quantity }), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"http://localhost:5137/api/inventory/increase", content); // Replace with actual URL and port
            return response.IsSuccessStatusCode;
        }

  private async Task<decimal?> GetUserBalanceAsync()
{
    var client = _httpClientFactory.CreateClient();
    
    // Extract the token from the current request's Authorization header
    var token = Request.Headers["Authorization"].ToString();
    if (string.IsNullOrEmpty(token))
    {
        Console.WriteLine("Authorization token is missing.");
        return null;
    }

    // Add the token to the HttpClient's Authorization header
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Split(' ').Last());

    var response = await client.GetAsync("http://localhost:5033/api/transaction/balance"); // Ensure this URL is correct
    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        var transaction = JsonSerializer.Deserialize<TransactionDTO>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return transaction?.Balance;
    }
    else
    {
        // Log the response for debugging
        var errorContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Error retrieving balance: {response.StatusCode}, {errorContent}");
    }

    return null;
}


private async Task<bool> DeductUserBalanceAsync(int userId, decimal amount)
{
    var client = _httpClientFactory.CreateClient();
    
    // Extract the token from the current request's Authorization header
    var token = Request.Headers["Authorization"].ToString();
    if (string.IsNullOrEmpty(token))
    {
        Console.WriteLine("Authorization token is missing.");
        return false;
    }

    // Add the token to the HttpClient's Authorization header
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Split(' ').Last());

    var content = new StringContent(JsonSerializer.Serialize(new { Amount = amount }), System.Text.Encoding.UTF8, "application/json");
    var response = await client.PutAsync($"http://localhost:5033/api/transaction/deduct-balance", content); // Replace with actual URL
    
    if (response.IsSuccessStatusCode)
    {
        return true;
    }
    else
    {
        // Log the response for debugging
        var errorContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Error deducting balance: {response.StatusCode}, {errorContent}");
    }

    return false;
}
private async Task<bool> RefundUserBalanceAsync(int userId, decimal amount)
{
    var client = _httpClientFactory.CreateClient();

    // Extract the token from the current request's Authorization header
    var token = Request.Headers["Authorization"].ToString();
    if (string.IsNullOrEmpty(token))
    {
        Console.WriteLine("Authorization token is missing.");
        return false;
    }

    // Add the token to the HttpClient's Authorization header
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Split(' ').Last());

    var content = new StringContent(JsonSerializer.Serialize(new { Amount = amount }), System.Text.Encoding.UTF8, "application/json");
    Console.WriteLine($"Sending refund request for user {userId} with amount {amount}");
    var response = await client.PutAsync($"http://localhost:5033/api/transaction/refund-balance", content); // Replace with actual URL
    
    if (response.IsSuccessStatusCode)
    {
        Console.WriteLine("Balance refunded successfully.");
        return true;
    }
    else
    {
        // Log the response for debugging
        var errorContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Error refunding balance: {response.StatusCode}, {errorContent}");
    }

    return false;
}



    }
}
