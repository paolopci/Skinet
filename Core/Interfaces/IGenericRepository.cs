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
        // Conteggio totale degli elementi per una specifica (usato per la paginazione).
        Task<int> CountAsync(ISpecification<T> spec);

        Task<T?> GetEntityWithSpec(ISpecification<T> spec);

        void Add(T entity);

        void Update(T entity);

        void Remove(T entity);

        Task<bool> SaveAllAsync();

        bool Exists(int id);
    }
}

