using Core.DTO.Shift;

namespace Core.Interface.Service.Shift;

public interface IShiftAssignmentService
{
    /// <summary>Lists assignments with filtering and paging.</summary>
    Task<(List<ShiftAssignmentListDto> Items, int TotalCount)> GetAssignmentsAsync(
        GetShiftAssignmentRequest request, CancellationToken ct = default);

    /// <summary>Returns full detail for a single assignment including attendance.</summary>
    Task<ShiftAssignmentDetailDto> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>Assigns a staff member to a shift template on a specific date.</summary>
    Task<ShiftAssignmentDetailDto> CreateAssignmentAsync(
        CreateShiftAssignmentRequest request, long assignedByStaffId, CancellationToken ct = default);

    /// <summary>Updates planned times or notes on an existing assignment.</summary>
    Task<ShiftAssignmentDetailDto> UpdateAssignmentAsync(
        long id, UpdateShiftAssignmentRequest request, long updatedByStaffId, CancellationToken ct = default);

    /// <summary>Cancels an assignment. Fails if attendance has already started.</summary>
    Task CancelAssignmentAsync(long id, CancellationToken ct = default);
}
