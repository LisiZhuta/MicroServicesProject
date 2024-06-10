using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryService.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;


namespace InventoryService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly InventoryContext _context; 
        private readonly IHttpClientFactory _httpClientFactory;

        public InventoryController(InventoryContext context,IHttpClientFactory httpClientFactory)
        {
            _context = context;     
            _httpClientFactory=httpClientFactory;    
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItem>>> Get()
        {
            return await _context.InventoryItems.ToListAsync();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryItem>> Get(int id)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(id);
            if (inventoryItem == null)
            {
                return NotFound($"Inventory with ID {id} doesnt exist");
            }
            return inventoryItem;
        }
        
        
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<InventoryItem>> GetByProductId(int productId)
        {
            var inventoryItem = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId);
            if (inventoryItem == null)
            {
                return NotFound();
            }
            return inventoryItem;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<InventoryItem>> Create(InventoryItem inventoryItem)
        {
            var productExists = await CheckProductExistsAsync(inventoryItem.ProductId);
            if (!productExists)
                {
                    return BadRequest($"Product with ID {inventoryItem.ProductId} does not exist.");
                }
            _context.InventoryItems.Add(inventoryItem);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = inventoryItem.Id }, inventoryItem);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, InventoryItem inventoryItem)
        {
            if (id != inventoryItem.Id)
            {
                return BadRequest();
            }

            _context.Entry(inventoryItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.InventoryItems.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(id);
            if (inventoryItem == null)
            {
                return NotFound($"Inventory with id {id} doesnt exist");
            }

            _context.InventoryItems.Remove(inventoryItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("reduce")]
        public async Task<IActionResult> ReduceQuantity([FromBody] QuantityRequest request)
        {
            var inventoryItem = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == request.ProductId);
            if (inventoryItem == null)
            {
                return NotFound($"Product with ID {request.ProductId} not found.");
            }

            if (inventoryItem.Quantity < request.Quantity)
            {
                return BadRequest($"Insufficient quantity for product ID {request.ProductId}. Available quantity: {inventoryItem.Quantity}");
            }

            inventoryItem.Quantity -= request.Quantity;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("increase")]
        public async Task<IActionResult> IncreaseQuantity([FromBody] QuantityRequest request)
        {
            var inventoryItem = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == request.ProductId);
            if (inventoryItem == null)
            {
                return NotFound($"Product with ID {request.ProductId} not found.");
            }

            inventoryItem.Quantity += request.Quantity;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> CheckProductExistsAsync(int productId)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"http://localhost:5284/api/product/{productId}"); // Replace with actual URL
            return response.IsSuccessStatusCode;
        }
        
    }
}
