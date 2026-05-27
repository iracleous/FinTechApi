using FinTechApi.Models;
using System.Linq.Expressions;

namespace FinTechApi.Repositories
{


    public interface ICustomerRepository : IAsyncRepository<Customer, Guid>
    {
        // Additional methods specific to Customer can be defined here if needed
    }

    /// <summary>
    /// Generic asynchronous repository abstraction.
    /// </summary>
    /// <typeparam name="T">Entity type (reference type).</typeparam>
    /// <typeparam name="TKey">Key type for entity identifiers.</typeparam>
    /// 

    public interface IAsyncRepository<T, TKey>
        where T : class
    {
        Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

        Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    }

}