namespace InventoryService.Models{
     public class ReduceQuantityRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}