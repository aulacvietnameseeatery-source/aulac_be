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

    /// <summary>Bulk-create assignments for multiple staff on a single shift/date.</summary>
    Task<List<ShiftAssignmentDetailDto>> BulkCreateAssignmentsAsync(
        BulkCreateAssignmentRequest request, long assignedByStaffId, CancellationToken ct = default);

    /// <summary>Updates planned times or notes on an existing assignment.</summary>
    Task<ShiftAssignmentDetailDto> UpdateAssignmentAsync(
        long id, UpdateShiftAssignmentRequest request, long updatedByStaffId, CancellationToken ct = default);

    /// <summary>Cancels an assignment. Fails if attendance has already started.</summary>
    Task CancelAssignmentAsync(long id, CancellationToken ct = default);

    /// <summary>Returns assignments for a specific staff member ("My Shifts" view).</summary>
    Task<(List<ShiftAssignmentDetailDto> Items, int TotalCount)> GetMyShiftsAsync(
        long staffId, GetShiftAssignmentRequest request, CancellationToken ct = default);

    /// <summary>Publishes DRAFT assignments → ASSIGNED, notifying each staff member.</summary>
    Task<List<ShiftAssignmentListDto>> PublishAssignmentsAsync(
        PublishAssignmentsRequest request, long publishedByStaffId, CancellationToken ct = default);

    /// <summary>Copies all active assignments from one week (Mon-Sun) to a target week.</summary>
    Task<List<ShiftAssignmentListDto>> CopyWeekAsync(
        CopyWeekRequest request, long assignedByStaffId, CancellationToken ct = default);

    /// <summary>Reassigns a shift to a different staff member.</summary>
    Task<ShiftAssignmentDetailDto> ReassignAsync(
        long assignmentId, ReassignRequest request, long reassignedByStaffId, CancellationToken ct = default);

    /// <summary>Staff confirms their own upcoming assigned shift.</summary>
    Task<ShiftAssignmentDetailDto> ConfirmAssignmentAsync(
        long assignmentId, long staffId, CancellationToken ct = default);

    /// <summary>Returns the team schedule matrix (all staff × 7 days) for a given week.</summary>
    Task<List<ShiftAssignmentListDto>> GetTeamScheduleAsync(
        TeamScheduleRequest request, CancellationToken ct = default);
}
