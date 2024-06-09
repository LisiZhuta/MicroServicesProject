using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Models
{
    public class InventoryContext : DbContext
    {
        public InventoryContext(DbContextOptions<InventoryContext> options) : base(options) { }

        public DbSet<InventoryItem> InventoryItems { get; set; }
    }
}
