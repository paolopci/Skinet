using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class CartItem
    {
        [Key]
        public int ProductId { get; set; }
        [Required]
        public required string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        [Required]
        public required string PictureUrl { get; set; }
        [Required]
        public required string Brand { get; set; }
        [Required]
        public required string Type { get; set; }
    }
}
