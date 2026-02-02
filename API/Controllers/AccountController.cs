using API.DTOs;
using API.Errors;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{

    public class AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService) : BaseApiController
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
                return BadRequest(result.Errors);
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
    }
}
