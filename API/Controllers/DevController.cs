using Core.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DevController(
        StoreContext context,
        IWebHostEnvironment env,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager) : BaseApiController
    {
        [HttpPost("reset-identity")]
        public async Task<ActionResult> ResetIdentity()
        {
            if (!env.IsDevelopment())
            {
                return NotFound();
            }

            await using var transaction = await context.Database.BeginTransactionAsync();

            await context.Database.ExecuteSqlRawAsync("DELETE FROM dbo.AspNetUserTokens");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM dbo.AspNetUserLogins");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM dbo.AspNetUserClaims");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM dbo.AspNetUserRoles");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM dbo.AspNetRoleClaims");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM dbo.AspNetUsers");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM dbo.AspNetRoles");

            await transaction.CommitAsync();

            var adminRoleName = "Admin";
            if (!await roleManager.RoleExistsAsync(adminRoleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(adminRoleName));
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join("; ", roleResult.Errors.Select(error => error.Description));
                    return BadRequest(new { message = "Creazione role Admin fallita", errors });
                }
            }

            var adminUser = new AppUser
            {
                FirstName = "Paolo",
                LastName = "Paci",
                Email = "paolopci@yahoo.it",
                UserName = "paolopci@yahoo.it",
                PhoneNumber = "3283834012",
                EmailConfirmed = true,
                Address = new Address
                {
                    Street = "Via del Canarino 5",
                    City = "Pesaro",
                    State = "Italia",
                    PostalCode = "61122"
                }
            };

            var createResult = await userManager.CreateAsync(adminUser, "Micene@65");
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(error => error.Description));
                return BadRequest(new { message = "Creazione utente admin fallita", errors });
            }

            var roleAssignResult = await userManager.AddToRoleAsync(adminUser, adminRoleName);
            if (!roleAssignResult.Succeeded)
            {
                var errors = string.Join("; ", roleAssignResult.Errors.Select(error => error.Description));
                return BadRequest(new { message = "Assegnazione role Admin fallita", errors });
            }

            var standardUsers = new[]
            {
                new AppUser
                {
                    FirstName = "Gigi",
                    LastName = "Rossi",
                    Email = "gigi@yahoo.it",
                    UserName = "gigi@yahoo.it",
                    PhoneNumber = "3280000001",
                    EmailConfirmed = true,
                    Address = new Address
                    {
                        Street = "Via Roma 10",
                        City = "Milano",
                        State = "Italia",
                        PostalCode = "20100"
                    }
                },
                new AppUser
                {
                    FirstName = "Carlo",
                    LastName = "Bianchi",
                    Email = "carlo@yahoo.it",
                    UserName = "carlo@yahoo.it",
                    PhoneNumber = "3280000002",
                    EmailConfirmed = true,
                    Address = new Address
                    {
                        Street = "Corso Italia 22",
                        City = "Roma",
                        State = "Italia",
                        PostalCode = "00100"
                    }
                }
            };

            foreach (var user in standardUsers)
            {
                var createUserResult = await userManager.CreateAsync(user, "Micene@65");
                if (!createUserResult.Succeeded)
                {
                    var errors = string.Join("; ", createUserResult.Errors.Select(error => error.Description));
                    return BadRequest(new { message = $"Creazione utente {user.Email} fallita", errors });
                }
            }

            return Ok(new
            {
                message = "Identity reset completato",
                adminEmail = adminUser.Email,
                users = standardUsers.Select(user => user.Email).ToArray()
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-check")]
        public ActionResult AdminCheck()
        {
            if (!env.IsDevelopment())
            {
                return NotFound();
            }

            return Ok(new { message = "Admin access ok" });
        }
    }
}
