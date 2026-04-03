using Core.DTO.Account;
using Core.DTO.General;
using Core.Interface.Service.Entity;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Service;

/// <summary>
/// Provides account-scoped activity data for the account detail tabs.
/// Uses DbContext directly for efficient read-only projection queries.
/// </summary>
public class AccountActivityService : IAccountActivityService
{
    private readonly RestaurantMgmtContext _context;

    public AccountActivityService(RestaurantMgmtContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<PagedResultDTO<AccountOrderSummaryDTO>> GetAccountOrdersAsync(
        long accountId,
        AccountSubResourceQueryDTO query,
        CancellationToken ct = default)
    {
        var q = _context.Orders
            .AsNoTracking()
            .Where(o => o.StaffId == accountId);

        if (query.FromDate.HasValue)
            q = q.Where(o => o.CreatedAt >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            q = q.Where(o => o.CreatedAt <= query.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(o =>
                o.OrderId.ToString().Contains(term) ||
                (o.Customer != null && o.Customer.FullName != null && o.Customer.FullName.ToLower().Contains(term)) ||
                (o.Table != null && o.Table.TableCode.ToLower().Contains(term)));
        }

        var totalCount = await q.CountAsync(ct);
        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Max(1, query.PageSize);

        var items = await q
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new AccountOrderSummaryDTO
            {
                OrderId = o.OrderId,
                TableCode = o.Table != null ? o.Table.TableCode : null,
                CustomerName = o.Customer != null ? o.Customer.FullName : null,
                TotalAmount = o.TotalAmount,
                TaxAmount = o.TaxAmount,
                TipAmount = o.TipAmount,
                OrderStatus = o.OrderStatusLv.ValueName,
                Source = o.SourceLv.ValueName,
                CreatedAt = o.CreatedAt,
                ItemCount = o.OrderItems.Count,
                IsPaid = o.Payments.Any()
            })
            .ToListAsync(ct);

        return new PagedResultDTO<AccountOrderSummaryDTO>
        {
            PageData = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDTO<AccountAuditLogDTO>> GetAccountAuditLogsAsync(
        long accountId,
        AccountSubResourceQueryDTO query,
        CancellationToken ct = default)
    {
        var q = _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.StaffId == accountId);

        if (query.FromDate.HasValue)
            q = q.Where(a => a.CreatedAt >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            q = q.Where(a => a.CreatedAt <= query.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(a =>
                (a.ActionCode != null && a.ActionCode.ToLower().Contains(term)) ||
                (a.TargetTable != null && a.TargetTable.ToLower().Contains(term)));
        }

        var totalCount = await q.CountAsync(ct);
        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Max(1, query.PageSize);

        var items = await q
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AccountAuditLogDTO
            {
                LogId = a.LogId,
                ActionCode = a.ActionCode,
                TargetTable = a.TargetTable,
                TargetId = a.TargetId,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(ct);

        return new PagedResultDTO<AccountAuditLogDTO>
        {
            PageData = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDTO<AccountLoginActivityDTO>> GetAccountLoginActivityAsync(
        long accountId,
        AccountSubResourceQueryDTO query,
        CancellationToken ct = default)
    {
        var q = _context.LoginActivities
            .AsNoTracking()
            .Where(la => la.StaffId == accountId);

        if (query.FromDate.HasValue)
            q = q.Where(la => la.OccurredAt >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            q = q.Where(la => la.OccurredAt <= query.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(la =>
                la.EventType.ToLower().Contains(term) ||
                (la.DeviceInfo != null && la.DeviceInfo.ToLower().Contains(term)) ||
                (la.IpAddress != null && la.IpAddress.Contains(term)));
        }

        var totalCount = await q.CountAsync(ct);
        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Max(1, query.PageSize);

        var items = await q
            .OrderByDescending(la => la.OccurredAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(la => new AccountLoginActivityDTO
            {
                LoginActivityId = la.LoginActivityId,
                EventType = la.EventType,
                DeviceInfo = la.DeviceInfo,
                IpAddress = la.IpAddress,
                OccurredAt = la.OccurredAt
            })
            .ToListAsync(ct);

        return new PagedResultDTO<AccountLoginActivityDTO>
        {
            PageData = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDTO<AccountServiceErrorDTO>> GetAccountServiceErrorsAsync(
        long accountId,
        AccountSubResourceQueryDTO query,
        CancellationToken ct = default)
    {
        var q = _context.ServiceErrors
            .AsNoTracking()
            .Where(se => se.StaffId == accountId);

        if (query.FromDate.HasValue)
            q = q.Where(se => se.CreatedAt >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            q = q.Where(se => se.CreatedAt <= query.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(se =>
                se.Description.ToLower().Contains(term) ||
                se.Category.CategoryName.ToLower().Contains(term) ||
                se.Category.CategoryCode.ToLower().Contains(term));
        }

        var totalCount = await q.CountAsync(ct);
        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Max(1, query.PageSize);

        var items = await q
            .OrderByDescending(se => se.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(se => new AccountServiceErrorDTO
            {
                ErrorId = se.ErrorId,
                OrderId = se.OrderId,
                CategoryName = se.Category.CategoryName,
                CategoryCode = se.Category.CategoryCode,
                Description = se.Description,
                SeverityName = se.SeverityLv.ValueName,
                PenaltyAmount = se.PenaltyAmount,
                IsResolved = se.IsResolved ?? false,
                ResolvedByName = se.ResolvedByNavigation != null ? se.ResolvedByNavigation.FullName : null,
                ResolvedAt = se.ResolvedAt,
                CreatedAt = se.CreatedAt
            })
            .ToListAsync(ct);

        return new PagedResultDTO<AccountServiceErrorDTO>
        {
            PageData = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDTO<AccountInventoryActivityDTO>> GetAccountInventoryActivityAsync(
        long accountId,
        AccountSubResourceQueryDTO query,
        CancellationToken ct = default)
    {
        var q = _context.InventoryTransactions
            .AsNoTracking()
            .Where(t => t.CreatedBy == accountId || t.ApprovedBy == accountId);

        if (query.FromDate.HasValue)
            q = q.Where(t => t.CreatedAt >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            q = q.Where(t => t.CreatedAt <= query.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(t =>
                (t.TransactionCode != null && t.TransactionCode.ToLower().Contains(term)) ||
                (t.Note != null && t.Note.ToLower().Contains(term)));
        }

        var totalCount = await q.CountAsync(ct);
        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Max(1, query.PageSize);

        var items = await q
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new AccountInventoryActivityDTO
            {
                TransactionId = t.TransactionId,
                TransactionCode = t.TransactionCode,
                TypeName = t.TypeLv.ValueName,
                StatusName = t.StatusLv.ValueName,
                Note = t.Note,
                CreatedAt = t.CreatedAt,
                StaffRole = t.CreatedBy == accountId ? "Creator" : "Approver",
                ItemCount = t.InventoryTransactionItems.Count
            })
            .ToListAsync(ct);

        return new PagedResultDTO<AccountInventoryActivityDTO>
        {
            PageData = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }
}
