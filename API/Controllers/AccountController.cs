using API.DTOs;
using API.Errors;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private const string RefreshTokenCookieName = "refreshToken";
        private static readonly HashSet<string> SupportedCountryCodes = ["IT", "US", "GB"];
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
                    FirstName = NormalizeRequired(registerDto.Address.FirstName),
                    LastName = NormalizeRequired(registerDto.Address.LastName),
                    AddressLine1 = NormalizeRequired(registerDto.Address.AddressLine1),
                    AddressLine2 = NormalizeOptional(registerDto.Address.AddressLine2),
                    City = NormalizeRequired(registerDto.Address.City),
                    PostalCode = NormalizeRequired(registerDto.Address.PostalCode),
                    CountryCode = NormalizeCountryCode(registerDto.Address.CountryCode),
                    Region = NormalizeOptional(registerDto.Address.Region)
                }
            };

            var result = await userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(error => error.Description);
                return BadRequest(ApiValidationErrorResponse.FromIdentityErrors(errors));
            }

            var (refreshToken, rawToken) = tokenService.CreateRefreshToken(user, GetIpAddress(), Request.Headers.UserAgent.ToString());
            user.RefreshTokens.Add(refreshToken);
            await userManager.UpdateAsync(user);
            SetRefreshTokenCookie(rawToken, refreshToken.ExpiresAt);

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

            var (refreshToken, rawToken) = tokenService.CreateRefreshToken(user, GetIpAddress(), Request.Headers.UserAgent.ToString());
            user.RefreshTokens.Add(refreshToken);
            await userManager.UpdateAsync(user);
            SetRefreshTokenCookie(rawToken, refreshToken.ExpiresAt);

            var jwtToken = await tokenService.CreateTokenAsync(user);
            SetAccessTokenCookie(jwtToken, DateTime.UtcNow.AddDays(7));

            return Ok(new UserDto
            {
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Token = jwtToken
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

            var jwtToken = await tokenService.CreateTokenAsync(user);
            SetAccessTokenCookie(jwtToken, DateTime.UtcNow.AddDays(7));

            return Ok(new UserDto
            {
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Token = jwtToken
            });
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<UserDto>> Refresh()
        {
            var refreshToken = Request.Cookies[RefreshTokenCookieName];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return Unauthorized(new ApiErrorResponse(
                    StatusCodes.Status401Unauthorized,
                    "Refresh token mancante",
                    null));
            }

            var hashedToken = tokenService.HashToken(refreshToken);
            var user = await userManager.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.TokenHash == hashedToken));

            if (user == null)
            {
                return Unauthorized(new ApiErrorResponse(
                    StatusCodes.Status401Unauthorized,
                    "Refresh token non valido",
                    null));
            }

            var storedToken = user.RefreshTokens.First(t => t.TokenHash == hashedToken);
            if (!storedToken.IsActive)
            {
                return Unauthorized(new ApiErrorResponse(
                    StatusCodes.Status401Unauthorized,
                    "Refresh token scaduto o revocato",
                    null));
            }

            var (newToken, rawToken) = tokenService.CreateRefreshToken(user, GetIpAddress(), Request.Headers.UserAgent.ToString());
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReplacedByTokenHash = newToken.TokenHash;
            user.RefreshTokens.Add(newToken);
            await userManager.UpdateAsync(user);
            SetRefreshTokenCookie(rawToken, newToken.ExpiresAt);

            var jwtToken = await tokenService.CreateTokenAsync(user);
            SetAccessTokenCookie(jwtToken, DateTime.UtcNow.AddDays(7));

            return Ok(new UserDto
            {
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Token = jwtToken
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            var refreshToken = Request.Cookies[RefreshTokenCookieName];
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var hashedToken = tokenService.HashToken(refreshToken);
                var user = await userManager.Users
                    .Include(u => u.RefreshTokens)
                    .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.TokenHash == hashedToken));

                if (user != null)
                {
                    var storedToken = user.RefreshTokens.FirstOrDefault(t => t.TokenHash == hashedToken);
                    if (storedToken != null && storedToken.RevokedAt == null)
                    {
                        storedToken.RevokedAt = DateTime.UtcNow;
                        await userManager.UpdateAsync(user);
                    }
                }
            }

            Response.Cookies.Delete(RefreshTokenCookieName);
            Response.Cookies.Delete("accessToken");
            await signInManager.SignOutAsync();
            return Ok(new { message = "Logout effettuato" });
        }

        private void SetRefreshTokenCookie(string token, DateTime expiresAt)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = expiresAt
            };

            Response.Cookies.Append(RefreshTokenCookieName, token, options);
        }

        private void SetAccessTokenCookie(string token, DateTime expiresAt)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = expiresAt
            };

            Response.Cookies.Append("accessToken", token, options);
        }

        private string? GetIpAddress()
        {
            if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
            {
                return forwarded.ToString();
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        private static string NormalizeRequired(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static string? NormalizeOptional(string? value)
        {
            var normalized = value?.Trim();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private static string NormalizeCountryCode(string? countryCode)
        {
            return NormalizeRequired(countryCode).ToUpperInvariant();
        }

        private static Dictionary<string, string[]> ValidateNormalizedAddress(Address address)
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(address.FirstName))
                errors["firstName"] = ["Il campo FirstName è obbligatorio."];
            if (string.IsNullOrWhiteSpace(address.LastName))
                errors["lastName"] = ["Il campo LastName è obbligatorio."];
            if (string.IsNullOrWhiteSpace(address.AddressLine1))
                errors["addressLine1"] = ["Il campo AddressLine1 è obbligatorio."];
            if (string.IsNullOrWhiteSpace(address.City))
                errors["city"] = ["Il campo City è obbligatorio."];
            if (string.IsNullOrWhiteSpace(address.PostalCode))
                errors["postalCode"] = ["Il campo PostalCode è obbligatorio."];

            if (string.IsNullOrWhiteSpace(address.CountryCode) || address.CountryCode.Length != 2)
            {
                errors["countryCode"] = ["CountryCode deve essere un codice ISO a 2 caratteri."];
            }
            else if (!SupportedCountryCodes.Contains(address.CountryCode))
            {
                errors["countryCode"] = ["CountryCode non supportato."];
            }

            var isRegionRequired = address.CountryCode is "IT" or "US";
            if (isRegionRequired && string.IsNullOrWhiteSpace(address.Region))
            {
                errors["region"] = ["Region è obbligatorio per il paese selezionato."];
            }

            return errors;
        }

        [Authorize]
        [HttpGet("address")]
        public async Task<ActionResult<AddressDto>> GetAddress()
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

            if (user.Address == null)
            {
                return NotFound(new ApiErrorResponse(
                    StatusCodes.Status404NotFound,
                    "Indirizzo non trovato",
                    null));
            }

            return Ok(new AddressDto
            {
                FirstName = user.Address.FirstName,
                LastName = user.Address.LastName,
                AddressLine1 = user.Address.AddressLine1,
                AddressLine2 = user.Address.AddressLine2,
                City = user.Address.City,
                PostalCode = user.Address.PostalCode,
                CountryCode = user.Address.CountryCode,
                Region = user.Address.Region
            });
        }

        [Authorize]
        [HttpPut("address")]
        public async Task<ActionResult<AddressDto>> UpdateAddress([FromBody] UpdateAddressDto updateAddressDto)
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

            var normalizedAddress = new Address
            {
                FirstName = NormalizeRequired(updateAddressDto.FirstName),
                LastName = NormalizeRequired(updateAddressDto.LastName),
                AddressLine1 = NormalizeRequired(updateAddressDto.AddressLine1),
                AddressLine2 = NormalizeOptional(updateAddressDto.AddressLine2),
                City = NormalizeRequired(updateAddressDto.City),
                PostalCode = NormalizeRequired(updateAddressDto.PostalCode),
                CountryCode = NormalizeCountryCode(updateAddressDto.CountryCode),
                Region = NormalizeOptional(updateAddressDto.Region)
            };

            var validationErrors = ValidateNormalizedAddress(normalizedAddress);
            if (validationErrors.Count > 0)
            {
                return BadRequest(new ApiValidationErrorResponse(StatusCodes.Status400BadRequest, validationErrors));
            }

            user.Address = normalizedAddress;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(error => error.Description);
                return BadRequest(ApiValidationErrorResponse.FromIdentityErrors(errors));
            }

            return Ok(new AddressDto
            {
                FirstName = user.Address.FirstName,
                LastName = user.Address.LastName,
                AddressLine1 = user.Address.AddressLine1,
                AddressLine2 = user.Address.AddressLine2,
                City = user.Address.City,
                PostalCode = user.Address.PostalCode,
                CountryCode = user.Address.CountryCode,
                Region = user.Address.Region
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
