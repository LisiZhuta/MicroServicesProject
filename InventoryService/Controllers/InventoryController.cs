using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryService.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace InventoryService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly InventoryContext _context;

        public InventoryController(InventoryContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItem>>> Get()
        {
            return await _context.InventoryItems.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryItem>> Get(int id)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(id);
            if (inventoryItem == null)
            {
                return NotFound();
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

        [HttpPost]
        public async Task<ActionResult<InventoryItem>> Create(InventoryItem inventoryItem)
        {
            _context.InventoryItems.Add(inventoryItem);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = inventoryItem.Id }, inventoryItem);
        }

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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var inventoryItem = await _context.InventoryItems.FindAsync(id);
            if (inventoryItem == null)
            {
                return NotFound();
            }

            _context.InventoryItems.Remove(inventoryItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        
        [HttpPut("reduce")]
        public async Task<IActionResult> ReduceQuantity([FromBody] ReduceQuantityRequest request)
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

    }
}
