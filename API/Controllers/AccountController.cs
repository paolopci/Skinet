using API.DTOs;
using API.Errors;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace API.Controllers
{

    public class AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        IEmailSender emailSender,
        IWebHostEnvironment env) : BaseApiController
    {
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var existingUser = await userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return BadRequest(new ApiErrorResponse(
                    StatusCodes.Status400BadRequest,
                    "Email già in uso",
                    null));
            }

            var user = new AppUser
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                UserName = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber,
                Address = new Address
                {
                    Street = registerDto.Address.Street,
                    City = registerDto.Address.City,
                    State = registerDto.Address.State,
                    PostalCode = registerDto.Address.PostalCode
                }
            };

            var result = await userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(error => error.Description);
                return BadRequest(ApiValidationErrorResponse.FromIdentityErrors(errors));
            }

            return StatusCode(StatusCodes.Status201Created, new
            {
                user.Email,
                user.FirstName,
                user.LastName
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login([FromBody] LoginDto loginDto)
        {
            var user = await userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return Unauthorized(new ApiErrorResponse(
                    StatusCodes.Status401Unauthorized,
                    "Credenziali non valide",
                    null));
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new ApiErrorResponse(
                    StatusCodes.Status401Unauthorized,
                    "Credenziali non valide",
                    null));
            }

            return Ok(new UserDto
            {
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Token = tokenService.CreateToken(user)
            });
        }

        [Authorize]
        [HttpGet("current-user")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                return Unauthorized(new ApiErrorResponse(
                    StatusCodes.Status401Unauthorized,
                    "Token non valido",
                    null));
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Unauthorized(new ApiErrorResponse(
                    StatusCodes.Status401Unauthorized,
                    "Utente non trovato",
                    null));
            }

            return Ok(new UserDto
            {
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Token = tokenService.CreateToken(user)
            });
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            var user = await userManager.FindByEmailAsync(forgotPasswordDto.Email);
            if (user == null)
            {
                return Ok(new { message = "Se l'email esiste, verrà inviato un link di reset." });
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);

            var resetLink = $"{Request.Scheme}://{Request.Host}/reset-password?email={WebUtility.UrlEncode(user.Email)}&token={encodedToken}";
            await emailSender.SendAsync(
                user.Email ?? forgotPasswordDto.Email,
                "Reset password",
                $"Reset password link: {resetLink}");

            if (env.IsDevelopment())
            {
                return Ok(new
                {
                    message = "Se l'email esiste, verrà inviato un link di reset.",
                    token = encodedToken
                });
            }

            return Ok(new { message = "Se l'email esiste, verrà inviato un link di reset." });
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            var user = await userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null)
            {
                return BadRequest(new ApiErrorResponse(
                    StatusCodes.Status400BadRequest,
                    "Utente non trovato",
                    null));
            }

            var decodedToken = WebUtility.UrlDecode(resetPasswordDto.Token);
            var result = await userManager.ResetPasswordAsync(user, decodedToken, resetPasswordDto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(error => error.Description);
                return BadRequest(ApiValidationErrorResponse.FromIdentityErrors(errors));
            }

            return Ok(new { message = "Password aggiornata" });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                return Unauthorized(new ApiErrorResponse(
                    StatusCodes.Status401Unauthorized,
                    "Token non valido",
                    null));
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Unauthorized(new ApiErrorResponse(
                    StatusCodes.Status401Unauthorized,
                    "Utente non trovato",
                    null));
            }

            var result = await userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(error => error.Description);
                return BadRequest(ApiValidationErrorResponse.FromIdentityErrors(errors));
            }

            return Ok(new { message = "Password aggiornata" });
        }

        [HttpPost("verify-email")]
        public async Task<ActionResult> VerifyEmail([FromBody] VerifyEmailDto verifyEmailDto)
        {
            var user = await userManager.FindByEmailAsync(verifyEmailDto.Email);
            if (user == null)
            {
                return BadRequest(new ApiErrorResponse(
                    StatusCodes.Status400BadRequest,
                    "Utente non trovato",
                    null));
            }

            var decodedToken = WebUtility.UrlDecode(verifyEmailDto.Token);
            var result = await userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(error => error.Description);
                return BadRequest(ApiValidationErrorResponse.FromIdentityErrors(errors));
            }

            return Ok(new { message = "Email verificata" });
        }

        [HttpPost("send-verify-email")]
        public async Task<ActionResult> SendVerifyEmail([FromBody] SendVerifyEmailDto sendVerifyEmailDto)
        {
            var user = await userManager.FindByEmailAsync(sendVerifyEmailDto.Email);
            if (user == null)
            {
                return Ok(new { message = "Se l'email esiste, verrà inviato un link di verifica." });
            }

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);

            var verifyLink = $"{Request.Scheme}://{Request.Host}/verify-email?email={WebUtility.UrlEncode(user.Email)}&token={encodedToken}";
            await emailSender.SendAsync(
                user.Email ?? sendVerifyEmailDto.Email,
                "Verifica email",
                $"Verify email link: {verifyLink}");

            if (env.IsDevelopment())
            {
                return Ok(new
                {
                    message = "Se l'email esiste, verrà inviato un link di verifica.",
                    token = encodedToken
                });
            }

            return Ok(new { message = "Se l'email esiste, verrà inviato un link di verifica." });
        }
    }
}
