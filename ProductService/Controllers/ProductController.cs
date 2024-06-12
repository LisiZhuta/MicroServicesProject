using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly ProductContext _context;

        public ProductController(ProductContext context)
        {
            _context = context;
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
            var existingproduct=await _context.Products.FirstOrDefaultAsync(p=>p.Name==product.Name);
            if(existingproduct!=null)
            {
                return BadRequest($"Product with name {product.Name} already exists");
            }
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("delete/{id}")]
        public async Task<ActionResult<Product>> Delete(int id)
        {
            var product=await _context.Products.FirstOrDefaultAsync(p=>p.Id==id);
            if(product==null)
            {
                return BadRequest($"Product with name {id} doesnt exist");
            }
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Ok("Product Deleted");
        }
    }
}
