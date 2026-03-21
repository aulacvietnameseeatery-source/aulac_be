using Core.Data;
using Core.DTO.Ingredient;
using Core.DTO.Notification;
using Core.DTO.Supplier;
using Core.Entity;
using Core.Enum;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public class IngredientService : IIngredientService
    {
        private readonly IIngredientRepository _repo;
        private readonly INotificationService _notificationService;

        public IngredientService(IIngredientRepository repo, INotificationService notificationService)
        {
            _repo = repo;
            _notificationService = notificationService;
        }

        public async Task<(List<IngredientDTO> Items, int TotalCount)> GetListAsync(IngredientFilterParams filter)
        {
            var (items, totalCount) = await _repo.GetPagedAsync(filter);

            var dtos = items.Select(MapToDto).ToList();
            return (dtos, totalCount);
        }

        public async Task<IngredientDTO> GetDetailAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) throw new Exception("Ingredient not found.");
            return MapToDto(entity);
        }

        public async Task<IngredientDTO> CreateAsync(SaveIngredientRequest request)
        {
            if (await _repo.IsNameExistAsync(request.IngredientName))
                throw new Exception("Ingredient name already exists.");

            var entity = new Ingredient
            {
                IngredientName = request.IngredientName,
                Unit = request.Unit,
                TypeLvId = request.TypeLvId,
                ImageId = request.ImageId,
                CurrentStock = new CurrentStock
                {
                    QuantityOnHand = 0,
                    MinStockLevel = request.MinStockLevel,
                    LastUpdatedAt = DateTime.UtcNow
                },
                IngredientSuppliers = request.SupplierIds.Select(sid => new IngredientSupplier
                {
                    SupplierId = sid,
                    CreatedAt = DateTime.UtcNow
                }).ToList()
            };

            var created = await _repo.AddAsync(entity);
            var reloaded = await _repo.GetByIdAsync(created.IngredientId);
            return MapToDto(reloaded!);
        }

        public async Task<IngredientDTO> UpdateAsync(long id, SaveIngredientRequest request)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) throw new Exception("Ingredient not found.");

            if (await _repo.IsNameExistAsync(request.IngredientName, id))
                throw new Exception("Ingredient name already exists.");

            entity.IngredientName = request.IngredientName;
            entity.Unit = request.Unit;
            entity.TypeLvId = request.TypeLvId;
            entity.ImageId = request.ImageId;

            if (entity.CurrentStock != null)
            {
                entity.CurrentStock.MinStockLevel = request.MinStockLevel;
            }

           
            entity.IngredientSuppliers.Clear();
            foreach (var sid in request.SupplierIds)
            {
                entity.IngredientSuppliers.Add(new IngredientSupplier { SupplierId = sid, CreatedAt = DateTime.UtcNow });
            }

            await _repo.UpdateAsync(entity);
            return MapToDto(entity);
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) throw new Exception("Ingredient not found.");

            if (entity.CurrentStock != null && entity.CurrentStock.QuantityOnHand > 0)
                throw new Exception("Cannot delete ingredient with existing stock in inventory.");

            if (entity.InventoryTransactionItems != null && entity.InventoryTransactionItems.Any())
                throw new Exception("Cannot delete ingredient because it has transaction history.");

            await _repo.DeleteAsync(entity);
        }

        public async Task AdjustStockAsync(long id, AdjustStockRequest request)
        {
            await _repo.AdjustStockAsync(id, request.Quantity, request.Note);

            // Check if stock fell below minimum after adjustment
            var entity = await _repo.GetByIdAsync(id);
            if (entity?.CurrentStock != null &&
                entity.CurrentStock.QuantityOnHand < entity.CurrentStock.MinStockLevel)
            {
                await _notificationService.PublishAsync(new PublishNotificationRequest
                {
                    Type = nameof(NotificationType.LOW_STOCK_ALERT),
                    Title = "Low Stock Alert",
                    Body = $"{entity.IngredientName} stock is low: {entity.CurrentStock.QuantityOnHand} {entity.Unit} (min: {entity.CurrentStock.MinStockLevel})",
                    Priority = nameof(NotificationPriority.High),
                    SoundKey = "notification_high",
                    ActionUrl = $"/dashboard/ingredients/{id}/history",
                    EntityType = "Ingredient",
                    EntityId = id.ToString(),
                    Metadata = new Dictionary<string, object>
                    {
                        ["ingredientId"] = id.ToString(),
                        ["ingredientName"] = entity.IngredientName,
                        ["currentStock"] = entity.CurrentStock.QuantityOnHand.ToString(),
                        ["minStock"] = entity.CurrentStock.MinStockLevel.ToString(),
                        ["unit"] = entity.Unit
                    },
                    TargetPermissions = new List<string> { Permissions.ViewDish }
                });
            }
        }

        public async Task<List<StockHistoryDto>> GetStockHistoryAsync(long id)
        {
            var history = await _repo.GetStockHistoryAsync(id);
            return history.Select(tx => new StockHistoryDto
            {
                TransactionItemId = tx.TransactionItemId,
                QuantityChanged = tx.Quantity,
                Note = tx.Note ?? "",
                CreatedAt = tx.Transaction?.CreatedAt ?? DateTime.UtcNow
            }).ToList();
        }

        // DTO Mapper Helper
        private IngredientDTO MapToDto(Ingredient entity)
        {
            return new IngredientDTO
            {
                IngredientId = entity.IngredientId,
                IngredientName = entity.IngredientName,
                Unit = entity.Unit,
                TypeLvId = entity.TypeLvId,
                TypeName = entity.TypeLv?.ValueName,
                ImageId = entity.ImageId,
                ImageUrl = entity.Image?.Url,
                QuantityOnHand = entity.CurrentStock?.QuantityOnHand ?? 0,
                MinStockLevel = entity.CurrentStock?.MinStockLevel ?? 0,
                LastUpdatedAt = entity.CurrentStock?.LastUpdatedAt,

                // Lấy thông tin Supplier 
                Suppliers = entity.IngredientSuppliers
                    .Where(ise => ise.Supplier != null)
                    .Select(ise => new SupplierDto
                    {
                         SupplierId = ise.Supplier!.SupplierId,
                         SupplierName = ise.Supplier.SupplierName,
                         Phone = ise.Supplier.Phone,
                         Email = ise.Supplier.Email
                    }).ToList()
            };
        }
    }
}
