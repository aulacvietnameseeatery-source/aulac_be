using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Repo
{
    public interface IUnitOfWork
    {
        /// <summary>
        /// Begins a new database transaction asynchronously.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        Task BeginTransactionAsync(CancellationToken ct);

        /// <summary>
        /// Commits the current transaction asynchronously.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        Task CommitAsync(CancellationToken ct);

        /// <summary>
        /// Rolls back the current transaction asynchronously.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        Task RollbackAsync(CancellationToken ct);

        /// <summary>
        /// Persists all changes to the database asynchronously.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        Task SaveChangesAsync(CancellationToken ct);
    }
}
