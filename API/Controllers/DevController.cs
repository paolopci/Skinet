using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DevController(StoreContext context, IWebHostEnvironment env) : BaseApiController
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

            return Ok(new { message = "Identity reset completato" });
        }
    }
}
