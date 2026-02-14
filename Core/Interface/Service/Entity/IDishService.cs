using Core.DTO.Dish;
using Core.Entity;
using Microsoft.AspNetCore.Http;

namespace Core.Interface.Service.Entity;

/// <summary>
/// Service interface for dish-related business logic
/// </summary>
public interface IDishService
{
    /// <summary>
    /// Get dish detail by ID
    /// </summary>
    /// <param name="id">Dish ID</param>
    /// <param name="langCode">Language code. Supported: "en" (English - default), "fr" (French), "vi" (Vietnamese)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dish detail DTO or null if not found</returns>
    Task<DishDetailDto?> GetDishByIdAsync(long id, string? langCode = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a dish.
    /// </summary>
    /// <param name="dishId">The dish ID</param>
    /// <param name="newStatus">The new status code</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated dish status information</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the dish is not found</exception>
    Task<DishStatusDto> UpdateDishStatusAsync(
        long dishId,
        DishStatusCode newStatus,
        CancellationToken ct = default);
    /// <summary>
    /// Lấy danh sách món ăn cho trang quản trị (Admin).
    /// Bao gồm đầy đủ thông tin để quản lý (Status, IsOnline, CreatedAt...)
    /// </summary>
    Task<(List<DishManagementDto> Items, int TotalCount)> GetDishesForAdminAsync(
        GetDishesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách món ăn cho trang khách hàng (Customer/Menu).
    /// Chỉ bao gồm các thông tin hiển thị (Name, Price, Image...)
    /// </summary>
    Task<(List<DishDisplayDto> Items, int TotalCount)> GetDishesForCustomerAsync(
        GetDishesRequest request,
        CancellationToken cancellationToken = default);

    // --- CÁC PHƯƠNG THỨC HỖ TRỢ FILTER (DROPDOWN) ---

    /// <summary>
    /// Lấy danh sách tất cả danh mục để hiển thị Dropdown lọc.
    /// </summary>
    Task<List<string>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách trạng thái món ăn để hiển thị Dropdown lọc.
    /// </summary>
    Task<List<DishStatusDto>> GetDishStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new dish with the specified details and associated images.
    /// </summary>
    /// <param name="request">The dish creation request data.</param>
    /// <param name="staticImages">A list of static image files for the dish.</param>
    /// <param name="images360">A list of 360-degree image files for the dish.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The ID of the newly created dish.</returns>
    Task<long> CreateDishAsync(
        CreateDishRequest request,
        IReadOnlyList<IFormFile> staticImages,
        IReadOnlyList<IFormFile> images360,
        CancellationToken ct
    );

    /// <summary>
    /// Retrieves the details of a dish by its ID.
    /// </summary>
    /// <param name="dishId">The ID of the dish to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The detailed information of the dish.</returns>
    Task<DishDetailForActionsDto> GetDishByIdAsync(
        long dishId,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Updates an existing dish with new details and manages associated images.
    /// </summary>
    /// <param name="request">The dish update request data.</param>
    /// <param name="staticImages">A list of new static image files for the dish.</param>
    /// <param name="images360">A list of new 360-degree image files for the dish.</param>
    /// <param name="removedMediaIds">A list of media IDs to be removed from the dish.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateDishAsync(
        UpdateDishRequest request,
        IReadOnlyList<IFormFile> staticImages,
        IReadOnlyList<IFormFile> images360,
        IReadOnlyList<long> removedMediaIds,
        CancellationToken ct
    );

    /// <summary>
    /// Gets a list of all active dish statuses.
    /// </summary>
    /// <returns>A list of active dish status DTOs.</returns>
    Task<List<ActiveDishStatusDto>> GetActiveDishStatusesAsync();

    /// <summary>
    /// Gets a list of all dish categories.
    /// </summary>
    /// <returns>A list of simple dish category DTOs.</returns>
    Task<List<DishCategorySimpleDto>> GetAllDishCategoriesAsync();

    /// <summary>
    /// Gets a list of all active dish tags.
    /// </summary>
    /// <returns>A list of active dish tag DTOs.</returns>
    Task<List<DishTagDto>> GetAllActiveTagsAsync();
}

