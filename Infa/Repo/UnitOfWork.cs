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
        private readonly RestaurantMgmtContext _db;
        private IDbContextTransaction? _tx;

        public UnitOfWork(RestaurantMgmtContext db)
        {
            _db = db;
        }

        public async Task BeginTransactionAsync(CancellationToken ct)
        {
            _tx = await _db.Database.BeginTransactionAsync(ct);
        }

        public async Task CommitAsync(CancellationToken ct)
        {
            if (_tx != null)
                await _tx.CommitAsync(ct);
        }

        public async Task RollbackAsync(CancellationToken ct)
        {
            if (_tx != null)
                await _tx.RollbackAsync(ct);
        }

        public Task SaveChangesAsync(CancellationToken ct)
            => _db.SaveChangesAsync(ct);
    }

}
