using Core.DTO.Shift;
using Core.Entity;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Interface.Service.Shift;

namespace Core.Service;

public class ShiftTemplateService : IShiftTemplateService
{
    private readonly IShiftTemplateRepository _templateRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ShiftTemplateService(IShiftTemplateRepository templateRepo, IUnitOfWork unitOfWork)
    {
        _templateRepo = templateRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ShiftTemplateListDto>> GetAllAsync(bool? isActive = null, CancellationToken ct = default)
    {
        var templates = isActive == true
            ? await _templateRepo.GetAllActiveAsync(ct)
            : await _templateRepo.GetAllAsync(ct);

        if (isActive == false)
            templates = templates.Where(t => !t.IsActive).ToList();

        return templates.Select(MapToListDto).ToList();
    }

    public async Task<ShiftTemplateDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var template = await _templateRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Shift template not found");
        return MapToDetailDto(template);
    }

    public async Task<ShiftTemplateDetailDto> CreateAsync(
        CreateShiftTemplateRequest request, long createdByStaffId, CancellationToken ct = default)
    {
        if (request.DefaultStartTime >= request.DefaultEndTime)
            throw new ValidationException("Default start time must be earlier than default end time");

        if (await _templateRepo.ExistsByNameAsync(request.TemplateName, ct: ct))
            throw new ConflictException($"A shift template named '{request.TemplateName}' already exists");

        var entity = new ShiftTemplate
        {
            TemplateName = request.TemplateName.Trim(),
            DefaultStartTime = request.DefaultStartTime,
            DefaultEndTime = request.DefaultEndTime,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedBy = createdByStaffId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _templateRepo.Add(entity);
        await _unitOfWork.SaveChangesAsync(ct);

        var saved = await _templateRepo.GetByIdAsync(entity.ShiftTemplateId, ct)
            ?? throw new NotFoundException("Template not found after creation");
        return MapToDetailDto(saved);
    }

    public async Task<ShiftTemplateDetailDto> UpdateAsync(
        long id, UpdateShiftTemplateRequest request, long updatedByStaffId, CancellationToken ct = default)
    {
        var template = await _templateRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Shift template not found");

        if (request.TemplateName is not null)
        {
            var trimmed = request.TemplateName.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new ValidationException("Template name cannot be blank");
            if (await _templateRepo.ExistsByNameAsync(trimmed, excludeId: id, ct: ct))
                throw new ConflictException($"A shift template named '{trimmed}' already exists");
            template.TemplateName = trimmed;
        }

        var startTime = request.DefaultStartTime ?? template.DefaultStartTime;
        var endTime = request.DefaultEndTime ?? template.DefaultEndTime;

        if (startTime >= endTime)
            throw new ValidationException("Default start time must be earlier than default end time");

        if (request.DefaultStartTime.HasValue) template.DefaultStartTime = request.DefaultStartTime.Value;
        if (request.DefaultEndTime.HasValue) template.DefaultEndTime = request.DefaultEndTime.Value;
        if (request.Description is not null) template.Description = request.Description.Trim();
        if (request.IsActive.HasValue) template.IsActive = request.IsActive.Value;

        template.UpdatedBy = updatedByStaffId;
        template.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        var updated = await _templateRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Template not found after update");
        return MapToDetailDto(updated);
    }

    public async Task DeactivateAsync(long id, CancellationToken ct = default)
    {
        var template = await _templateRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Shift template not found");

        if (await _templateRepo.HasActiveAssignmentsAsync(id, ct))
            throw new ConflictException(
                "Cannot deactivate a template that still has active shift assignments. " +
                "Please cancel those assignments first.");

        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static ShiftTemplateListDto MapToListDto(ShiftTemplate t) => new()
    {
        ShiftTemplateId = t.ShiftTemplateId,
        TemplateName = t.TemplateName,
        DefaultStartTime = t.DefaultStartTime,
        DefaultEndTime = t.DefaultEndTime,
        Description = t.Description,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt
    };

    internal static ShiftTemplateDetailDto MapToDetailDto(ShiftTemplate t) => new()
    {
        ShiftTemplateId = t.ShiftTemplateId,
        TemplateName = t.TemplateName,
        DefaultStartTime = t.DefaultStartTime,
        DefaultEndTime = t.DefaultEndTime,
        Description = t.Description,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt,
        CreatedByName = t.CreatedByStaff?.FullName ?? "Unknown",
        UpdatedByName = t.UpdatedByStaff?.FullName,
        UpdatedAt = t.UpdatedAt
    };
}
