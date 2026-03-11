using Core.DTO.Shift;

namespace Core.Interface.Service.Shift;

public interface IShiftAssignmentService
{
    Task<(List<ShiftAssignmentDto> Items, int TotalCount)> GetAssignmentsAsync(
        GetShiftAssignmentRequest request, CancellationToken ct = default);

    Task<List<ShiftAssignmentDto>> CreateAssignmentsAsync(
        CreateShiftAssignmentRequest request, long assignedByStaffId, CancellationToken ct = default);

    Task CancelAssignmentAsync(long assignmentId, CancellationToken ct = default);
}
