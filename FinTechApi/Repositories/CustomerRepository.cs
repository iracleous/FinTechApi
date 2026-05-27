using FinTechApi.Data;
using FinTechApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FinTechApi.Repositories
{


    public class CustomerRepository : ICustomerRepository
    {

        private readonly FinTechContext _db;

        public CustomerRepository(FinTechContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.Customers
                .AsNoTracking()
                .Include(c => c.Accounts)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<Customer>> ListAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Customers
                .AsNoTracking()
                .Include(c => c.Accounts)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<Customer>> FindAsync(Expression<Func<Customer, bool>> predicate, CancellationToken cancellationToken = default)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return await _db.Customers
                .AsNoTracking()
                .Where(predicate)
                .Include(c => c.Accounts)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<Customer> AddAsync(Customer entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            await _db.Customers.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return entity;
        }

        public async Task AddRangeAsync(IEnumerable<Customer> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            await _db.Customers.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateAsync(Customer entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            // Attach and mark modified to update all scalar properties.
            _db.Customers.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(Customer entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            _db.Customers.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> ExistsAsync(Expression<Func<Customer, bool>> predicate, CancellationToken cancellationToken = default)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return await _db.Customers.AnyAsync(predicate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<int> CountAsync(Expression<Func<Customer, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null)
            {
                return await _db.Customers.CountAsync(cancellationToken).ConfigureAwait(false);
            }

            return await _db.Customers.CountAsync(predicate, cancellationToken).ConfigureAwait(false);
        }
    }
}