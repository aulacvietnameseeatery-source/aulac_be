using Core.DTO.Dish;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Microsoft.Extensions.Logging;

namespace Core.Service;

/// <summary>
/// Service implementation for dish business logic operations.
/// </summary>
public class DishService : IDishService
{
    private readonly IDishRepository _dishRepository;
    private readonly ILookupResolver _lookupResolver;
    private readonly ILogger<DishService> _logger;

    public DishService(
        IDishRepository dishRepository,
        ILookupResolver lookupResolver,
        ILogger<DishService> logger)
    {
        _dishRepository = dishRepository;
        _lookupResolver = lookupResolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DishStatusDto> UpdateDishStatusAsync(
        long dishId,
        DishStatusCode newStatus,
        CancellationToken ct = default)
    {
        try
        {
            // Check if dish exists
            var dish = await _dishRepository.FindByIdAsync(dishId, ct);
            if (dish == null)
            {
                _logger.LogWarning("Dish with ID {DishId} not found", dishId);
                throw new KeyNotFoundException($"Dish with ID {dishId} not found.");
            }

            // Resolve status code to lookup value ID using extension method
            var statusId = await newStatus.ToDishStatusIdAsync(_lookupResolver, ct);

            // Update the dish status
            await _dishRepository.UpdateStatusAsync(dishId, statusId, ct);

            _logger.LogInformation(
                "Updated dish {DishId} ({DishName}) status to {Status}",
                dishId,
                dish.DishName,
                newStatus);

            return new DishStatusDto
            {
                DishId = dish.DishId,
                DishName = dish.DishName,
                StatusCode = newStatus,
                StatusId = statusId,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (KeyNotFoundException)
        {
            // Re-throw KeyNotFoundException to be handled by controller
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dish status for dish ID {DishId}", dishId);
            throw new InvalidOperationException($"Failed to update dish status: {ex.Message}", ex);
        }
    }
}
