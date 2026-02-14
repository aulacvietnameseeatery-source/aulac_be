using Core.DTO.Dish;
using Core.Entity;
using Core.Enum;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Infa.Repo;

/// <summary>
/// Repository implementation for dish data access operations.
/// </summary>
public class DishRepository : IDishRepository
{
    private readonly RestaurantMgmtContext _context;

    public DishRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    public async Task<(List<Dish> Items, int TotalCount)> GetDishesAsync(
        GetDishesRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Khởi tạo Queryable với các Include cần thiết
        var query = _context.Dishes
            .Include(d => d.Category)
            .Include(d => d.DishStatusLv)
            .Include(d => d.DishMedia)
            .AsNoTracking() // Tối ưu hiệu năng cho việc chỉ đọc (Read-only)
            .AsQueryable();

        // 2. Logic Searching (Tìm kiếm)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim().ToLower();
            query = query.Where(d =>
                d.DishName.ToLower().Contains(searchTerm) ||
                (d.Description != null && d.Description.ToLower().Contains(searchTerm)) ||
                (d.Category != null && d.Category.CategoryName.ToLower().Contains(searchTerm))
            );
        }

        // 3. Logic Filtering (Bộ lọc)

        // 3a. Filter cho Customer (Logic nghiệp vụ đặc thù)
        if (request.IsCustomerView)
        {
            // 1. Chuyển Enum thành chữ "AVAILABLE"
            var availableCode = DishStatusCode.AVAILABLE.ToString();
            var lookupTypeId = (long)Core.Enum.LookupType.DishStatus;

            // 2. Query DB để tìm ID thực tế của trạng thái này
            var availableId = await _context.LookupValues
                .Where(lv => lv.TypeId == lookupTypeId && lv.ValueCode == availableCode)
                .Select(lv => lv.ValueId)
                .FirstOrDefaultAsync(cancellationToken);

            // 3. Filter bằng ID vừa tìm được
            query = query.Where(d => d.IsOnline == true &&
                                     d.DishStatusLvId == availableId);
        }
        else
        {
            // 3b. Filter cho Admin (Theo Status cụ thể nếu có)
            if (request.Status.HasValue)
            {
                var statusId = (long)request.Status.Value;
                query = query.Where(d => d.DishStatusLvId == statusId);
            }
        }

        // 4. Logic Sorting (Sắp xếp)
        if (string.IsNullOrWhiteSpace(request.SortBy))
        {
            // Mặc định sắp xếp theo ID giảm dần (Mới nhất lên đầu)
            query = query.OrderByDescending(d => d.DishId);
        }
        else
        {
            switch (request.SortBy.ToLower())
            {
                case "price":
                    query = request.IsDescending
                        ? query.OrderByDescending(d => d.Price)
                        : query.OrderBy(d => d.Price);
                    break;
                case "name":
                    query = request.IsDescending
                        ? query.OrderByDescending(d => d.DishName)
                        : query.OrderBy(d => d.DishName);
                    break;
                default:
                    query = query.OrderByDescending(d => d.CreatedAt);
                    break;
            }
        }

        // 5. Pagination (Phân trang)

        // Đếm tổng số bản ghi thỏa mãn điều kiện TRƯỚC khi cắt trang
        var totalCount = await query.CountAsync(cancellationToken);

        // Xử lý trang hợp lệ (giống code mẫu)
        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    // Lấy danh sách Categories để đổ vào dropdown filter
    public async Task<List<string>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DishCategories
            .Select(c => c.CategoryName)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    // Lấy danh sách Statuses để đổ vào dropdown filter 
    public async Task<List<LookupValue>> GetDishStatusesAsync(CancellationToken cancellationToken = default)
    {

        var typeId = (long)Core.Enum.LookupType.DishStatus;

        return await _context.LookupValues
            .Where(lv => lv.TypeId == typeId
                         && lv.IsActive == true
                         && lv.DeletedAt == null)
            .OrderBy(lv => lv.ValueId) // Sẽ trả về 42, 43, 44 theo thứ tự
            .ToListAsync(cancellationToken);
    }

    public async Task<Dish?> GetDishByIdAsync(long dishId, CancellationToken cancellationToken = default)
    {
        return await _context.Dishes
            .Include(d => d.Category)
            .Include(d => d.DishStatusLv)
            .Include(d => d.DishMedia)
                .ThenInclude(dm => dm.Media)
            .Include(d => d.Recipes)
                .ThenInclude(r => r.Ingredient)
                    .ThenInclude(i => i.IngredientNameText)
                        .ThenInclude(t => t!.I18nTranslations)
            .Include(d => d.DishNameText)
                .ThenInclude(t => t.I18nTranslations)
            .Include(d => d.DescriptionText)
                .ThenInclude(t => t!.I18nTranslations)
            .Include(d => d.ShortDescriptionText)
                .ThenInclude(t => t!.I18nTranslations)
            .Include(d => d.SloganText)
                .ThenInclude(t => t!.I18nTranslations)
            .FirstOrDefaultAsync(d => d.DishId == dishId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Dish?> FindByIdAsync(long dishId, CancellationToken ct = default)
    {
        return await _context.Dishes
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DishId == dishId, ct);
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(long dishId, uint statusLvId, CancellationToken ct = default)
    {
        await _context.Dishes
            .Where(d => d.DishId == dishId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.DishStatusLvId, statusLvId),
                ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(long dishId, CancellationToken ct = default)
    {
        return await _context.Dishes
            .AsNoTracking()
            .AnyAsync(d => d.DishId == dishId, ct);
    }

    public async Task AddAsync(Dish dish, CancellationToken ct)
    {
        _context.Dishes.Add(dish); // Add new Dish entity to the context
        await _context.SaveChangesAsync(ct); // Persist changes to the database
    }

    public async Task<Dish?> FindByIdForActionAsync(long id, CancellationToken ct)
    {
        // Retrieve a Dish by ID, including related entities for full details
        return await _context.Dishes
        .Include(x => x.DishNameText).ThenInclude(x => x.I18nTranslations)
        .Include(x => x.DescriptionText).ThenInclude(x => x.I18nTranslations)
        .Include(x => x.ShortDescriptionText).ThenInclude(x => x.I18nTranslations)
        .Include(x => x.SloganText).ThenInclude(x => x.I18nTranslations)
        .Include(x => x.NoteText).ThenInclude(x => x.I18nTranslations)
        .Include(x => x.DishStatusLv)
        .Include(x => x.Category)
        .Include(x => x.DishMedia)
            .ThenInclude(x => x.Media)
                .ThenInclude(x => x.MediaTypeLv)
        .FirstOrDefaultAsync(x => x.DishId == id, ct); // Return the first match or null
    }

    public async Task<List<LookupValue>> GetActiveDishStatusEntitiesAsync()
    {
        // Get all active dish status lookup values
        return await _context.LookupValues
        .AsNoTracking() // No tracking for read-only query
        .Where(lv =>
            lv.TypeId == (ushort)Core.Enum.LookupType.DishStatus &&
            lv.IsActive == true &&
            lv.DeletedAt == null
        )
        .OrderBy(lv => lv.ValueId) // Order by ValueId
        .ToListAsync();
    }

    public async Task<List<DishCategory>> GetAllDishCategoriesAsync()
    {
        // Retrieve all dish categories, ordered by CategoryId
        return await _context.DishCategories
            .AsNoTracking()
            .Include(c => c.CategoryNameText)
                .ThenInclude(t => t.I18nTranslations)
            .Where(c => !c.IsDisabled)
            .OrderBy(c => c.CategoryId)
            .ToListAsync();
    }

    public async Task<List<LookupValue>> GetAllActiveTagsAsync()
    {
        // Get all active tag lookup values
        return await _context.LookupValues
            .AsNoTracking()
            .Where(lv =>
                lv.TypeId == (ushort)Core.Enum.LookupType.Tag &&
                lv.IsActive == true &&
                lv.DeletedAt == null
            )
            .OrderBy(lv => lv.SortOrder)
            .ToListAsync();
    }

    public async Task AddDishTagAsync(DishTag dishTag, CancellationToken ct)
    {
        _context.DishTags.Add(dishTag); // Add new DishTag entity to the context
        await _context.SaveChangesAsync(ct); // Persist changes to the database
    }

    public async Task<DishTag?> FindTagByDishIdAsync(long id, ushort typeId, CancellationToken ct)
    {
        // Find the DishTag for a given DishId, including the related Tag
        return await _context.DishTags
            .Include(x => x.Tag)
        .FirstOrDefaultAsync(x => x.DishId == id && x.Tag.TypeId == typeId, ct);
    }
}

