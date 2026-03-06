using Core.DTO.General;
using Core.DTO.Promotion;
using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infa.Repo
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly RestaurantMgmtContext _context;

        public PromotionRepository(RestaurantMgmtContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Promotion promotion, CancellationToken ct)
        {
            await _context.Promotions.AddAsync(promotion, ct);
        }

        public async Task<Promotion?> GetByIdAsync(long id, CancellationToken ct)
        {
            return await _context.Promotions
                .Include(x => x.PromotionRules)
                .Include(x => x.PromotionTargets)
                .Include(x => x.PromotionStatusLv)
                .Include(x => x.TypeLv)
                .FirstOrDefaultAsync(x => x.PromotionId == id, ct);
        }

        public async Task<PagedResultDTO<PromotionListDTO>> GetPromotionsAsync(
            PromotionListQueryDTO query,
            CancellationToken cancellationToken = default)
        {
            var queryable = _context.Promotions
                .Include(p => p.TypeLv)
                .Include(p => p.PromotionStatusLv)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLower();

                queryable = queryable.Where(p =>
                    p.PromoName.ToLower().Contains(search) ||
                    (p.PromoCode != null && p.PromoCode.ToLower().Contains(search))
                );
            }

            if (!string.IsNullOrWhiteSpace(query.PromotionStatus))
            {
                queryable = queryable.Where(p =>
                    p.PromotionStatusLv.ValueCode == query.PromotionStatus);
            }

            if (!string.IsNullOrWhiteSpace(query.Type))
            {
                queryable = queryable.Where(p =>
                    p.TypeLv.ValueCode == query.Type);
            }

            if (query.FromDate.HasValue)
            {
                queryable = queryable.Where(p =>
                    p.StartTime >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                queryable = queryable.Where(p =>
                    p.EndTime <= query.ToDate.Value);
            }

            var totalCount = await queryable.CountAsync(cancellationToken);

            var pageIndex = query.PageIndex < 1 ? 1 : query.PageIndex;
            var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

            var promotions = await queryable
                .OrderByDescending(p => p.PromotionId)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PromotionListDTO
                {
                    PromotionId = p.PromotionId,
                    PromoCode = p.PromoCode!,
                    PromoName = p.PromoName,
                    StartTime = p.StartTime,
                    EndTime = p.EndTime,
                    DiscountValue = p.DiscountValue,
                    MaxUsage = p.MaxUsage,
                    UsedCount = p.UsedCount,
                    Type = p.TypeLv.ValueCode,
                    PromotionStatus = p.PromotionStatusLv.ValueCode
                })
                .ToListAsync(cancellationToken);

            return new PagedResultDTO<PromotionListDTO>
            {
                PageData = promotions,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public void RemoveRules(IEnumerable<PromotionRule> rules)
        {
            _context.PromotionRules.RemoveRange(rules);
        }

        public void RemoveTargets(IEnumerable<PromotionTarget> targets)
        {
            _context.PromotionTargets.RemoveRange(targets);
        }

        public async Task SaveChangesAsync(CancellationToken ct)
        {
            await _context.SaveChangesAsync(ct);
        }

        public async Task<List<Promotion>> GetActivePromotionsAsync(
        DateTime now,
        CancellationToken ct)
        {
            return await _context.Promotions
                .Include(p => p.PromotionRules)
                .Include(p => p.PromotionTargets)
                .Include(p => p.TypeLv)
                .Include(p => p.PromotionStatusLv)
                .Where(p =>
                    p.StartTime <= now &&
                    p.EndTime >= now &&
                    p.PromotionStatusLv.ValueCode != PromotionStatusCode.DISABLED.ToString())
                .ToListAsync(ct);
        }
    }
}
