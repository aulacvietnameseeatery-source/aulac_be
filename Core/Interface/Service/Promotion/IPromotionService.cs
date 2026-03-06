using Core.DTO.General;
using Core.DTO.Promotion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Promotion
{
    public interface IPromotionService
    {
        Task<PagedResultDTO<PromotionListDTO>> GetPromotionsAsync(PromotionListQueryDTO query, CancellationToken ct);
        Task<long> CreatePromotionAsync(PromotionDto request, CancellationToken ct);
        Task UpdatePromotionAsync(PromotionDto request, CancellationToken ct);
        Task<PromotionDto> GetPromotionByIdAsync(long promotionId, CancellationToken ct);
        Task<PromotionDetailDto> GetPromotionDetailAsync(
        long promotionId,
        CancellationToken ct);
        Task DisablePromotionAsync(long promotionId, CancellationToken ct);
        Task ActivatePromotionAsync(long promotionId, CancellationToken ct);
        Task<List<PromotionAvailableDTO>> GetAvailablePromotionsAsync(long orderId, CancellationToken ct);
    }
}
