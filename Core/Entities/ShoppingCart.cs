using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class ShoppingCart
    {
        [Required]
        public required string Id { get; set; }

        public List<CartItem> Items { get; set; } = [];

        // for Stripe Payment
        public int? DeliverMethodId { get; set; }
        public string? ClientSecret { get; set; }
        public string? PaymentIntentId { get; set; }
    }
}
