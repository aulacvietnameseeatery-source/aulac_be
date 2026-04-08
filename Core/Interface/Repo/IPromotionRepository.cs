using Core.DTO.General;
using Core.DTO.Promotion;
using Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Repo
{
    public interface IPromotionRepository
    {
        Task<PagedResultDTO<PromotionListDTO>> GetPromotionsAsync(PromotionListQueryDTO query, CancellationToken ct);

        Task AddAsync(Promotion promotion, CancellationToken ct);

        Task<Promotion?> GetByIdAsync(long promotionId, CancellationToken ct);

        Task SaveChangesAsync(CancellationToken ct);

        void RemoveRules(IEnumerable<PromotionRule> rules);

        void RemoveTargets(IEnumerable<PromotionTarget> targets);

        Task<List<Promotion>> GetActivePromotionsAsync(DateTime now, CancellationToken ct);

        Task<Promotion?> GetByIdWithRelationsAsync(long id, CancellationToken ct);

        Task DeleteAsync(Promotion promotion, CancellationToken ct);
    }
}
