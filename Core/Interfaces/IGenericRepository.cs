using Core.Entities;

namespace Core.Interfaces
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(int id);

        Task<IReadOnlyList<T>> ListAllAsync();

        Task<IReadOnlyList<T>> ListAsync(
            ISpecification<T> spec,
            System.Linq.Expressions.Expression<Func<T, object>>? orderBy = null,
            SortDirection? direction = null);

        Task<T?> GetEntityWithSpec(ISpecification<T> spec);

        void Add(T entity);

        void Update(T entity);

        void Remove(T entity);

        Task<bool> SaveAllAsync();

        bool Exists(int id);
    }
}
