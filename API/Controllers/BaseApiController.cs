using API.RequestHelpers;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseApiController : ControllerBase
    {
        protected async Task<ActionResult> CreatePageResult<T>(IGenericRepository<T> repo, ISpecification<T> spec,
                          int pageIndex, int pageSize) where T : BaseEntity
        {
            var items = await repo.ListAsync(spec);
            // Conteggio totale per calcolo pagine lato client.
            var count = await repo.CountAsync(spec);

            // Risposta paginata con totale elementi e totale pagine.
            var pagination = new Pagination<T>(
                pageIndex,
                pageSize,
                count,
                items
            );

            return Ok(pagination);
        }
    }
}
