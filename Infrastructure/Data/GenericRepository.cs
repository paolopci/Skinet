using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Data
{
    public class GenericRepository<T>(StoreContext context) : IGenericRepository<T> where T : BaseEntity
    {
        public async Task<T?> GetByIdAsync(int id)
        {
            return await context.Set<T>().FindAsync(id);
        }

        public async Task<IReadOnlyList<T>> ListAllAsync()
        {
            return await context.Set<T>().ToListAsync();
        }

        public void Add(T entity)
        {
            context.Set<T>().Add(entity);
        }

        public void Update(T entity)
        {
            context.Set<T>().Update(entity);
        }

        public void Remove(T entity)
        {
            context.Set<T>().Remove(entity);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await context.SaveChangesAsync() > 0;
        }

        public bool Exists(int id)
        {
            return context.Set<T>().Any(x => x.Id == id);
        }

        public async Task<T?> GetEntityWithSpec(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<T>> ListAsync(
            ISpecification<T> spec,
            Expression<Func<T, object>>? orderBy = null,
            SortDirection? direction = null)
        {
            var query = ApplySpecification(spec);

            if (orderBy != null)
            {
                query = direction == SortDirection.Desc
                    ? query.OrderByDescending(orderBy)
                    : query.OrderBy(orderBy);
            }

            return await query.ToListAsync();
        }


        private IQueryable<T> ApplySpecification(ISpecification<T> spec)
        {
            return SpecificationEvaluator<T>.GetQuery(context.Set<T>().AsQueryable(), spec);
        }
    }
}
