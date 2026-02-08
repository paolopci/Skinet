using Microsoft.AspNetCore.Identity;

namespace Core.Entities
{
    public class AppUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? StripeCustomerId { get; set; }
        public Address? Address { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; } = new();
    }
}
