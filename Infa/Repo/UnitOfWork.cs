using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infa.Repo
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly RestaurantMgmtContext _db; // Database context for EF Core operations
        private IDbContextTransaction? _tx; // Holds the current transaction, if any

        public UnitOfWork(RestaurantMgmtContext db)
        {
            _db = db; // Inject the database context
        }

        public async Task BeginTransactionAsync(CancellationToken ct)
        {
            _tx = await _db.Database.BeginTransactionAsync(ct); // Start a new database transaction
        }

        public async Task CommitAsync(CancellationToken ct)
        {
            if (_tx != null)
                await _tx.CommitAsync(ct); // Commit the current transaction if it exists
        }

        public async Task RollbackAsync(CancellationToken ct)
        {
            if (_tx != null)
                await _tx.RollbackAsync(ct); // Roll back the current transaction if it exists
        }

        public Task SaveChangesAsync(CancellationToken ct)
            => _db.SaveChangesAsync(ct); // Persist all changes to the database
    }

}
