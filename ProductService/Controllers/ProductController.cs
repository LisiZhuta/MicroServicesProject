using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly ProductContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ProductController(ProductContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> Get()
        {
            return await _context.Products.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> Get(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return product;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Product>> Create(Product product)
        {
            var existingProduct = await _context.Products.FirstOrDefaultAsync(p => p.Name == product.Name);
            if (existingProduct != null)
            {
                return BadRequest($"Product with name {product.Name} already exists.");
            }
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound($"Product with ID {id} does not exist.");
            }


            var inventoryDeleted = await DeleteInventoryByProductIdAsync(id);
            if (!inventoryDeleted)
            {
                return StatusCode(500, "Failed to delete inventory items.");
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Ok("Product and its inventory deleted successfully.");
        }

    private async Task<bool> DeleteInventoryByProductIdAsync(int productId)
{
    var client = _httpClientFactory.CreateClient();
    

    var token = Request.Headers["Authorization"].ToString();
    if (string.IsNullOrEmpty(token))
    {
        Console.WriteLine("Authorization token is missing.");
        return false;
    }

    // Add the token to the HttpClient's Authorization header
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Split(' ').Last());

    var request = new HttpRequestMessage(HttpMethod.Delete, $"http://localhost:5137/api/inventory/product/{productId}");

    var response = await client.SendAsync(request);

    if (!response.IsSuccessStatusCode)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        // Log the error for debugging purposes
        Console.WriteLine($"Failed to delete inventory items. Status Code: {response.StatusCode}, Error: {errorContent}");
        return false;
    }

    return true;
}


    }
}
