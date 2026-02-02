using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class SendVerifyEmailDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
