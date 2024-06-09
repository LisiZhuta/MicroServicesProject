using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        public async Task<ActionResult<IEnumerable<Order>>> Get()
        {
            return await _context.Orders.Include(o => o.Items).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> Get(int id)
        {
            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }
            return order;
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Order>> Create(Order order)
        {
            var total = 0m;

            foreach (var item in order.Items)
            {
                var productExists = await CheckProductExistsAsync(item.ProductId);
                if (!productExists)
                {
                    return BadRequest($"Product with ID {item.ProductId} does not exist.");
                }

                var productQuantity = await GetProductQuantityAsync(item.ProductId);
                if (productQuantity == null || productQuantity < item.Quantity)
                {
                    return BadRequest($"Insufficient quantity for product ID {item.ProductId}. Available quantity: {productQuantity}");
                }

                var price = await GetProductPriceAsync(item.ProductId);
                if (price == null)
                {
                    return BadRequest($"Could not fetch price for product ID {item.ProductId}");
                }

                item.Order = order; // Associate the Order with OrderItems
                total += item.Quantity * price.Value;

                // Reduce the quantity in the inventory
                var reduced = await ReduceInventoryQuantityAsync(item.ProductId, item.Quantity);
                if (!reduced)
                {
                    return BadRequest($"Could not reduce quantity for product ID {item.ProductId}");
                }
            }

            order.Total = total;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
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
    }

   
}
