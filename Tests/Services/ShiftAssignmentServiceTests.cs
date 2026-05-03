using Core.Data;
using Core.DTO.Notification;
using Core.DTO.Shift;
using Core.Entity;
using Core.Enum;
using Core.Exceptions;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Notification;
using Core.Interface.Service.Shift;
using Core.Service;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests.Services;

/// <summary>
/// Unit Test — ShiftAssignmentService
/// Code Module : Core/Service/ShiftAssignmentService.cs
/// Method      : CreateAssignmentAsync, UpdateAssignmentAsync, CancelAssignmentAsync,
///               GetByIdAsync, GetAssignmentsAsync, ConfirmAssignmentAsync,
///               GetMyShiftsAsync, GetTeamScheduleAsync, ReassignAsync,
///               CopyWeekAsync, BulkCreateAssignmentsAsync, PublishAssignmentsAsync
/// Created By  : Automation
/// Executed By : Test Runner
/// Test Req.   : Restaurant manager creates, updates, cancels, reassigns, and publishes
///               shift assignments for staff members while validating template activity,
///               staff existence, time-window overlaps, and assignment lifecycle constraints.
/// </summary>
public class ShiftAssignmentServiceTests
{
    // ── Mocks ──
    private readonly Mock<IShiftAssignmentRepository> _assignmentRepoMock = new();
    private readonly Mock<IShiftTemplateRepository> _templateRepoMock = new();
    private readonly Mock<IAccountRepository> _accountRepoMock = new();
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<INotificationService> _notificationMock = new();
    private readonly Mock<ILookupResolver> _lookupResolverMock = new();
    private readonly Mock<IShiftLiveRealtimePublisher> _realtimePublisherMock = new();
    private readonly AttendanceOptions _options = new();

    // ── Factory ──
    private ShiftAssignmentService CreateService() => new(
        _assignmentRepoMock.Object,
        _templateRepoMock.Object,
        _accountRepoMock.Object,
        _orderRepoMock.Object,
        _unitOfWorkMock.Object,
        _notificationMock.Object,
        _lookupResolverMock.Object,
        Options.Create(_options),
        _realtimePublisherMock.Object);

    // ── Test Data Helpers ──

    private static readonly DateOnly TestWorkDate = new(2026, 4, 20);
    private static readonly DateTime PlannedStart = new(2026, 4, 20, 8, 0, 0);
    private static readonly DateTime PlannedEnd = new(2026, 4, 20, 16, 0, 0);

    private static ShiftTemplate MakeActiveTemplate(long id = 1) => new()
    {
        ShiftTemplateId = id,
        TemplateName = "Morning Shift",
        DefaultStartTime = new TimeOnly(8, 0),
        DefaultEndTime = new TimeOnly(16, 0),
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static ShiftTemplate MakeInactiveTemplate(long id = 2) => new()
    {
        ShiftTemplateId = id,
        TemplateName = "Old Shift",
        DefaultStartTime = new TimeOnly(8, 0),
        DefaultEndTime = new TimeOnly(16, 0),
        IsActive = false,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static StaffAccount MakeStaff(long id = 10) => new()
    {
        AccountId = id,
        FullName = "Nguyen Van A",
        Username = "nguyenvana",
        PasswordHash = "hash",
        RoleId = 1,
        Role = new Role { RoleId = 1, RoleCode = "SERVER", RoleName = "Waiter" }
    };

    private static ShiftAssignment MakeAssignment(
        long id = 100,
        long staffId = 10,
        bool isActive = true,
        uint statusLvId = 23001,
        AttendanceRecord? attendance = null) => new()
    {
        ShiftAssignmentId = id,
        ShiftTemplateId = 1,
        StaffId = staffId,
        WorkDate = TestWorkDate,
        PlannedStartAt = PlannedStart,
        PlannedEndAt = PlannedEnd,
        AssignmentStatusLvId = statusLvId,
        IsActive = isActive,
        Notes = "Test notes",
        Tags = "test",
        AssignedBy = 1,
        AssignedAt = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        AttendanceRecord = attendance,
        ShiftTemplate = MakeActiveTemplate(),
        Staff = MakeStaff(staffId),
        AssignedByStaff = MakeStaff(1),
        AssignmentStatusLv = new LookupValue { ValueId = statusLvId, TypeId = 23, ValueCode = "ASSIGNED", ValueName = "Assigned" }
    };

    private static CreateShiftAssignmentRequest MakeCreateRequest(
        long templateId = 1,
        long staffId = 10,
        bool isDraft = false) => new()
    {
        ShiftTemplateId = templateId,
        StaffId = staffId,
        WorkDate = TestWorkDate,
        PlannedStartAt = PlannedStart,
        PlannedEndAt = PlannedEnd,
        Notes = "Test",
        Tags = "tag",
        IsDraft = isDraft
    };

    private void SetupLookupResolver()
    {
        // ShiftAssignmentStatusCode uses LookupType.ShiftAssignmentStatus (23)
        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(23001u);
    }

    private void SetupRealtimePublisher()
    {
        _realtimePublisherMock
            .Setup(p => p.PublishBoardChangedAsync(It.IsAny<ShiftLiveRealtimeEventDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // ══════════════════════════════════════════════════════════════
    // GetByIdAsync
    // ══════════════════════════════════════════════════════════════

    #region GetByIdAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetByIdAsync")]
    public async Task GetByIdAsync_WhenAssignmentExists_ReturnsDetailDto()
    {
        // Arrange
        var assignment = MakeAssignment();
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(100);

        // Assert
        result.Should().NotBeNull();
        result.ShiftAssignmentId.Should().Be(100);
        result.StaffName.Should().Be("Nguyen Van A");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetByIdAsync")]
    public async Task GetByIdAsync_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShiftAssignment?)null);

        var service = CreateService();

        // Act
        var act = () => service.GetByIdAsync(999);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetByIdAsync")]
    public async Task GetByIdAsync_WhenIdIsZero_ThrowsNotFoundException()
    {
        // Arrange
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShiftAssignment?)null);

        var service = CreateService();

        // Act
        var act = () => service.GetByIdAsync(0);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // GetAssignmentsAsync
    // ══════════════════════════════════════════════════════════════

    #region GetAssignmentsAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAssignmentsAsync")]
    public async Task GetAssignmentsAsync_WhenDataExists_ReturnsMappedList()
    {
        // Arrange
        var assignments = new List<ShiftAssignment> { MakeAssignment() };
        _assignmentRepoMock
            .Setup(r => r.GetAssignmentsAsync(It.IsAny<GetShiftAssignmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((assignments, 1));

        var service = CreateService();

        // Act
        var (items, totalCount) = await service.GetAssignmentsAsync(new GetShiftAssignmentRequest());

        // Assert
        totalCount.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].ShiftAssignmentId.Should().Be(100);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAssignmentsAsync")]
    public async Task GetAssignmentsAsync_WhenNoData_ReturnsEmptyList()
    {
        // Arrange
        _assignmentRepoMock
            .Setup(r => r.GetAssignmentsAsync(It.IsAny<GetShiftAssignmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ShiftAssignment>(), 0));

        var service = CreateService();

        // Act
        var (items, totalCount) = await service.GetAssignmentsAsync(new GetShiftAssignmentRequest());

        // Assert
        totalCount.Should().Be(0);
        items.Should().BeEmpty();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // CreateAssignmentAsync
    // ══════════════════════════════════════════════════════════════

    #region CreateAssignmentAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateAssignmentAsync")]
    public async Task CreateAssignmentAsync_WhenValidRequest_CreatesAndReturnsDetail()
    {
        // Arrange
        var template = MakeActiveTemplate();
        var staff = MakeStaff();
        var request = MakeCreateRequest();
        var savedAssignment = MakeAssignment();

        _templateRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(1, TestWorkDate, It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(10, TestWorkDate, PlannedStart, PlannedEnd, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedAssignment);

        SetupLookupResolver();
        SetupRealtimePublisher();

        var service = CreateService();

        // Act
        var result = await service.CreateAssignmentAsync(request, assignedByStaffId: 1);

        // Assert
        result.Should().NotBeNull();
        _assignmentRepoMock.Verify(r => r.Add(It.IsAny<ShiftAssignment>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateAssignmentAsync")]
    public async Task CreateAssignmentAsync_WhenDraft_DoesNotSendNotification()
    {
        // Arrange
        var template = MakeActiveTemplate();
        var staff = MakeStaff();
        var request = MakeCreateRequest(isDraft: true);
        var savedAssignment = MakeAssignment();

        _templateRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(1, TestWorkDate, It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(10, TestWorkDate, PlannedStart, PlannedEnd, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedAssignment);

        SetupLookupResolver();
        SetupRealtimePublisher();

        var service = CreateService();

        // Act
        await service.CreateAssignmentAsync(request, assignedByStaffId: 1);

        // Assert
        _notificationMock.Verify(
            n => n.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateAssignmentAsync")]
    public async Task CreateAssignmentAsync_WhenNotDraft_SendsNotification()
    {
        // Arrange
        var template = MakeActiveTemplate();
        var staff = MakeStaff();
        var request = MakeCreateRequest(isDraft: false);
        var savedAssignment = MakeAssignment();

        _templateRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(1, TestWorkDate, It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(10, TestWorkDate, PlannedStart, PlannedEnd, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedAssignment);

        SetupLookupResolver();
        SetupRealtimePublisher();

        var service = CreateService();

        // Act
        await service.CreateAssignmentAsync(request, assignedByStaffId: 1);

        // Assert
        _notificationMock.Verify(
            n => n.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateAssignmentAsync")]
    public async Task CreateAssignmentAsync_WhenTemplateNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShiftTemplate?)null);

        var request = MakeCreateRequest(templateId: 999);
        var service = CreateService();

        // Act
        var act = () => service.CreateAssignmentAsync(request, assignedByStaffId: 1);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*template*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateAssignmentAsync")]
    public async Task CreateAssignmentAsync_WhenTemplateInactive_ThrowsValidationException()
    {
        // Arrange
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeInactiveTemplate());

        var request = MakeCreateRequest(templateId: 2);
        var service = CreateService();

        // Act
        var act = () => service.CreateAssignmentAsync(request, assignedByStaffId: 1);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*inactive*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateAssignmentAsync")]
    public async Task CreateAssignmentAsync_WhenStaffNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeActiveTemplate());
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        var request = MakeCreateRequest(staffId: 999);
        var service = CreateService();

        // Act
        var act = () => service.CreateAssignmentAsync(request, assignedByStaffId: 1);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Staff account*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateAssignmentAsync")]
    public async Task CreateAssignmentAsync_WhenStartAfterEnd_ThrowsValidationException()
    {
        // Arrange
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeActiveTemplate());
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeStaff());

        var request = MakeCreateRequest();
        request.PlannedStartAt = PlannedEnd;   // swap: start >= end
        request.PlannedEndAt = PlannedStart;

        var service = CreateService();

        // Act
        var act = () => service.CreateAssignmentAsync(request, assignedByStaffId: 1);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*start*earlier*end*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateAssignmentAsync")]
    public async Task CreateAssignmentAsync_WhenAlreadyAssigned_ThrowsConflictException()
    {
        // Arrange
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeActiveTemplate());
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeStaff());
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(1, TestWorkDate, It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { 10 });

        var request = MakeCreateRequest();
        var service = CreateService();

        // Act
        var act = () => service.CreateAssignmentAsync(request, assignedByStaffId: 1);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already assigned*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateAssignmentAsync")]
    public async Task CreateAssignmentAsync_WhenOverlappingShift_ThrowsConflictException()
    {
        // Arrange
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeActiveTemplate());
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeStaff());
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(1, TestWorkDate, It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(10, TestWorkDate, PlannedStart, PlannedEnd, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = MakeCreateRequest();
        var service = CreateService();

        // Act
        var act = () => service.CreateAssignmentAsync(request, assignedByStaffId: 1);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*overlapping*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateAssignmentAsync")]
    public async Task CreateAssignmentAsync_WhenNoPlannedTimes_UsesTemplateDefaults()
    {
        // Arrange
        var template = MakeActiveTemplate();
        var staff = MakeStaff();
        var savedAssignment = MakeAssignment();

        var request = new CreateShiftAssignmentRequest
        {
            ShiftTemplateId = 1,
            StaffId = 10,
            WorkDate = TestWorkDate,
            PlannedStartAt = null, // should use template defaults
            PlannedEndAt = null,
        };

        _templateRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(1, TestWorkDate, It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(
                10, TestWorkDate,
                TestWorkDate.ToDateTime(template.DefaultStartTime),
                TestWorkDate.ToDateTime(template.DefaultEndTime),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedAssignment);

        SetupLookupResolver();
        SetupRealtimePublisher();

        var service = CreateService();

        // Act
        var result = await service.CreateAssignmentAsync(request, assignedByStaffId: 1);

        // Assert
        result.Should().NotBeNull();
        _assignmentRepoMock.Verify(r => r.Add(It.Is<ShiftAssignment>(a =>
            a.PlannedStartAt == TestWorkDate.ToDateTime(template.DefaultStartTime) &&
            a.PlannedEndAt == TestWorkDate.ToDateTime(template.DefaultEndTime)
        )), Times.Once);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // UpdateAssignmentAsync
    // ══════════════════════════════════════════════════════════════

    #region UpdateAssignmentAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateAssignmentAsync")]
    public async Task UpdateAssignmentAsync_WhenValidUpdate_SavesAndReturnsDetail()
    {
        // Arrange
        var assignment = MakeAssignment();
        var updatedAssignment = MakeAssignment();

        _assignmentRepoMock
            .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedAssignment);

        SetupRealtimePublisher();

        var request = new UpdateShiftAssignmentRequest
        {
            Notes = "Updated notes",
            Tags = "updated-tag"
        };

        var service = CreateService();

        // Act
        var result = await service.UpdateAssignmentAsync(100, request, updatedByStaffId: 1);

        // Assert
        result.Should().NotBeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateAssignmentAsync")]
    public async Task UpdateAssignmentAsync_WhenUpdatingTimes_ChecksOverlapAndSaves()
    {
        // Arrange
        var assignment = MakeAssignment();
        var updatedAssignment = MakeAssignment();
        var newStart = new DateTime(2026, 4, 20, 9, 0, 0);
        var newEnd = new DateTime(2026, 4, 20, 17, 0, 0);

        _assignmentRepoMock
            .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(10, TestWorkDate, newStart, newEnd, 100L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedAssignment);

        SetupRealtimePublisher();

        var request = new UpdateShiftAssignmentRequest
        {
            PlannedStartAt = newStart,
            PlannedEndAt = newEnd
        };

        var service = CreateService();

        // Act
        var result = await service.UpdateAssignmentAsync(100, request, updatedByStaffId: 1);

        // Assert
        result.Should().NotBeNull();
        _assignmentRepoMock.Verify(r => r.HasOverlappingAssignmentAsync(
            10, TestWorkDate, newStart, newEnd, 100L, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateAssignmentAsync")]
    public async Task UpdateAssignmentAsync_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _assignmentRepoMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShiftAssignment?)null);

        var service = CreateService();

        // Act
        var act = () => service.UpdateAssignmentAsync(999, new UpdateShiftAssignmentRequest(), 1);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateAssignmentAsync")]
    public async Task UpdateAssignmentAsync_WhenCancelled_ThrowsValidationException()
    {
        // Arrange
        var assignment = MakeAssignment(isActive: false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var service = CreateService();

        // Act
        var act = () => service.UpdateAssignmentAsync(100, new UpdateShiftAssignmentRequest(), 1);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*cancelled*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateAssignmentAsync")]
    public async Task UpdateAssignmentAsync_WhenCheckedIn_ThrowsConflictException()
    {
        // Arrange
        var attendance = new AttendanceRecord { ActualCheckInAt = DateTime.UtcNow };
        var assignment = MakeAssignment(attendance: attendance);
        _assignmentRepoMock
            .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var service = CreateService();

        // Act
        var act = () => service.UpdateAssignmentAsync(100, new UpdateShiftAssignmentRequest(), 1);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*checked in*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateAssignmentAsync")]
    public async Task UpdateAssignmentAsync_WhenStartAfterEnd_ThrowsValidationException()
    {
        // Arrange
        var assignment = MakeAssignment();
        _assignmentRepoMock
            .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var request = new UpdateShiftAssignmentRequest
        {
            PlannedStartAt = PlannedEnd,
            PlannedEndAt = PlannedStart
        };

        var service = CreateService();

        // Act
        var act = () => service.UpdateAssignmentAsync(100, request, 1);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*start*earlier*end*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateAssignmentAsync")]
    public async Task UpdateAssignmentAsync_WhenOverlapping_ThrowsConflictException()
    {
        // Arrange
        var assignment = MakeAssignment();
        var newStart = new DateTime(2026, 4, 20, 9, 0, 0);
        var newEnd = new DateTime(2026, 4, 20, 17, 0, 0);

        _assignmentRepoMock
            .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(10, TestWorkDate, newStart, newEnd, 100L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateShiftAssignmentRequest
        {
            PlannedStartAt = newStart,
            PlannedEndAt = newEnd
        };

        var service = CreateService();

        // Act
        var act = () => service.UpdateAssignmentAsync(100, request, 1);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*overlaps*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // CancelAssignmentAsync
    // ══════════════════════════════════════════════════════════════

    #region CancelAssignmentAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CancelAssignmentAsync")]
    public async Task CancelAssignmentAsync_WhenValid_CancelsAndSaves()
    {
        // Arrange
        var assignment = MakeAssignment();
        _assignmentRepoMock
            .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        SetupLookupResolver();
        SetupRealtimePublisher();

        var service = CreateService();

        // Act
        await service.CancelAssignmentAsync(100);

        // Assert
        assignment.IsActive.Should().BeFalse();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CancelAssignmentAsync")]
    public async Task CancelAssignmentAsync_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _assignmentRepoMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShiftAssignment?)null);

        var service = CreateService();

        // Act
        var act = () => service.CancelAssignmentAsync(999);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CancelAssignmentAsync")]
    public async Task CancelAssignmentAsync_WhenAlreadyCancelled_ThrowsValidationException()
    {
        // Arrange
        var assignment = MakeAssignment(isActive: false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var service = CreateService();

        // Act
        var act = () => service.CancelAssignmentAsync(100);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*already cancelled*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CancelAssignmentAsync")]
    public async Task CancelAssignmentAsync_WhenCheckedIn_ThrowsConflictException()
    {
        // Arrange
        var attendance = new AttendanceRecord { ActualCheckInAt = DateTime.UtcNow };
        var assignment = MakeAssignment(attendance: attendance);
        _assignmentRepoMock
            .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var service = CreateService();

        // Act
        var act = () => service.CancelAssignmentAsync(100);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*checked in*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // ConfirmAssignmentAsync
    // ══════════════════════════════════════════════════════════════

    #region ConfirmAssignmentAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ConfirmAssignmentAsync")]
    public async Task ConfirmAssignmentAsync_WhenAssignedToSelf_ConfirmsAndReturnsDetail()
    {
        // Arrange
        var assignment = MakeAssignment(staffId: 10, statusLvId: 23001);
        var confirmedAssignment = MakeAssignment(staffId: 10);

        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        // Setup: ASSIGNED status lookup returns 23001 (same as assignment's current status)
        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.Is<System.Enum>(e => e.ToString() == "ASSIGNED"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(23001u);
        // Setup: CONFIRMED status lookup returns 23002
        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.Is<System.Enum>(e => e.ToString() == "CONFIRMED"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(23002u);

        SetupRealtimePublisher();

        var service = CreateService();

        // Act
        var result = await service.ConfirmAssignmentAsync(100, staffId: 10);

        // Assert
        result.Should().NotBeNull();
        assignment.AssignmentStatusLvId.Should().Be(23002u);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ConfirmAssignmentAsync")]
    public async Task ConfirmAssignmentAsync_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShiftAssignment?)null);

        var service = CreateService();

        // Act
        var act = () => service.ConfirmAssignmentAsync(999, staffId: 10);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ConfirmAssignmentAsync")]
    public async Task ConfirmAssignmentAsync_WhenNotOwnShift_ThrowsForbiddenException()
    {
        // Arrange
        var assignment = MakeAssignment(staffId: 10);
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var service = CreateService();

        // Act — staff 99 tries to confirm staff 10's shift
        var act = () => service.ConfirmAssignmentAsync(100, staffId: 99);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*only confirm your own*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ConfirmAssignmentAsync")]
    public async Task ConfirmAssignmentAsync_WhenNotAssignedStatus_ThrowsValidationException()
    {
        // Arrange — assignment has DRAFT status (23000), not ASSIGNED (23001)
        var assignment = MakeAssignment(staffId: 10, statusLvId: 23000);
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        // ASSIGNED lookup returns 23001, but assignment has 23000
        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.Is<System.Enum>(e => e.ToString() == "ASSIGNED"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(23001u);

        var service = CreateService();

        // Act
        var act = () => service.ConfirmAssignmentAsync(100, staffId: 10);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*ASSIGNED*confirmed*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // GetMyShiftsAsync
    // ══════════════════════════════════════════════════════════════

    #region GetMyShiftsAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetMyShiftsAsync")]
    public async Task GetMyShiftsAsync_WhenStaffHasShifts_ReturnsMappedList()
    {
        // Arrange
        var assignments = new List<ShiftAssignment> { MakeAssignment(staffId: 10) };
        _assignmentRepoMock
            .Setup(r => r.GetAssignmentsAsync(It.Is<GetShiftAssignmentRequest>(req => req.StaffId == 10), It.IsAny<CancellationToken>()))
            .ReturnsAsync((assignments, 1));

        var service = CreateService();

        // Act
        var (items, totalCount) = await service.GetMyShiftsAsync(10, new GetShiftAssignmentRequest());

        // Assert
        totalCount.Should().Be(1);
        items.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetMyShiftsAsync")]
    public async Task GetMyShiftsAsync_ForcesStaffIdFilter()
    {
        // Arrange
        _assignmentRepoMock
            .Setup(r => r.GetAssignmentsAsync(It.IsAny<GetShiftAssignmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ShiftAssignment>(), 0));

        var request = new GetShiftAssignmentRequest { StaffId = 999 }; // will be overridden
        var service = CreateService();

        // Act
        await service.GetMyShiftsAsync(10, request);

        // Assert
        request.StaffId.Should().Be(10); // was forced to authenticated staff
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetMyShiftsAsync")]
    public async Task GetMyShiftsAsync_WhenNoShifts_ReturnsEmptyList()
    {
        // Arrange
        _assignmentRepoMock
            .Setup(r => r.GetAssignmentsAsync(It.IsAny<GetShiftAssignmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ShiftAssignment>(), 0));

        var service = CreateService();

        // Act
        var (items, totalCount) = await service.GetMyShiftsAsync(10, new GetShiftAssignmentRequest());

        // Assert
        totalCount.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetMyShiftsAsync")]
    public async Task GetMyShiftsAsync_WhenMultipleShifts_ReturnsAllMapped()
    {
        // Arrange
        var assignments = new List<ShiftAssignment>
        {
            MakeAssignment(id: 100, staffId: 10),
            MakeAssignment(id: 101, staffId: 10),
            MakeAssignment(id: 102, staffId: 10)
        };
        _assignmentRepoMock
            .Setup(r => r.GetAssignmentsAsync(It.Is<GetShiftAssignmentRequest>(req => req.StaffId == 10), It.IsAny<CancellationToken>()))
            .ReturnsAsync((assignments, 3));

        var service = CreateService();

        // Act
        var (items, totalCount) = await service.GetMyShiftsAsync(10, new GetShiftAssignmentRequest());

        // Assert
        totalCount.Should().Be(3);
        items.Should().HaveCount(3);
        items.Select(i => i.ShiftAssignmentId).Should().BeEquivalentTo(new long[] { 100, 101, 102 });
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetMyShiftsAsync")]
    public async Task GetMyShiftsAsync_PreservesRequestFilters()
    {
        // Arrange
        var fromDate = new DateOnly(2026, 4, 1);
        var toDate = new DateOnly(2026, 4, 30);
        var request = new GetShiftAssignmentRequest
        {
            StaffId = 999, // will be overridden
            FromDate = fromDate,
            ToDate = toDate,
            IsActive = true,
            PageIndex = 2,
            PageSize = 5
        };

        _assignmentRepoMock
            .Setup(r => r.GetAssignmentsAsync(It.IsAny<GetShiftAssignmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ShiftAssignment>(), 0));

        var service = CreateService();

        // Act
        await service.GetMyShiftsAsync(10, request);

        // Assert — StaffId forced, but other filters preserved
        request.StaffId.Should().Be(10);
        request.FromDate.Should().Be(fromDate);
        request.ToDate.Should().Be(toDate);
        request.IsActive.Should().BeTrue();
        request.PageIndex.Should().Be(2);
        request.PageSize.Should().Be(5);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetMyShiftsAsync")]
    public async Task GetMyShiftsAsync_MapsFieldsToDetailDto()
    {
        // Arrange
        var assignment = MakeAssignment(id: 200, staffId: 10);
        _assignmentRepoMock
            .Setup(r => r.GetAssignmentsAsync(It.Is<GetShiftAssignmentRequest>(req => req.StaffId == 10), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ShiftAssignment> { assignment }, 1));

        var service = CreateService();

        // Act
        var (items, _) = await service.GetMyShiftsAsync(10, new GetShiftAssignmentRequest());

        // Assert
        var dto = items.Should().ContainSingle().Subject;
        dto.ShiftAssignmentId.Should().Be(200);
        dto.StaffId.Should().Be(10);
        dto.TemplateName.Should().Be("Morning Shift");
        dto.StaffName.Should().Be("Nguyen Van A");
        dto.WorkDate.Should().Be(TestWorkDate);
        dto.AssignmentStatusCode.Should().Be("ASSIGNED");
        dto.IsActive.Should().BeTrue();
        dto.Notes.Should().Be("Test notes");
        dto.Tags.Should().Be("test");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetMyShiftsAsync")]
    public async Task GetMyShiftsAsync_WhenTotalCountExceedsItemsCount_ReturnsPagedResult()
    {
        // Arrange — repo returns 2 items but reports totalCount = 10 (page 1 of 5)
        var assignments = new List<ShiftAssignment>
        {
            MakeAssignment(id: 100, staffId: 10),
            MakeAssignment(id: 101, staffId: 10)
        };
        _assignmentRepoMock
            .Setup(r => r.GetAssignmentsAsync(It.IsAny<GetShiftAssignmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((assignments, 10));

        var service = CreateService();

        // Act
        var (items, totalCount) = await service.GetMyShiftsAsync(10, new GetShiftAssignmentRequest { PageSize = 2 });

        // Assert
        items.Should().HaveCount(2);
        totalCount.Should().Be(10);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetMyShiftsAsync")]
    public async Task GetMyShiftsAsync_WhenStaffIdIsZero_PassesZeroToRepo()
    {
        // Arrange
        _assignmentRepoMock
            .Setup(r => r.GetAssignmentsAsync(It.Is<GetShiftAssignmentRequest>(req => req.StaffId == 0), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ShiftAssignment>(), 0));

        var service = CreateService();

        // Act
        var (items, totalCount) = await service.GetMyShiftsAsync(0, new GetShiftAssignmentRequest());

        // Assert
        totalCount.Should().Be(0);
        items.Should().BeEmpty();
        _assignmentRepoMock.Verify(
            r => r.GetAssignmentsAsync(It.Is<GetShiftAssignmentRequest>(req => req.StaffId == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetMyShiftsAsync")]
    public async Task GetMyShiftsAsync_WhenAssignmentHasAttendance_MapsAttendanceInDto()
    {
        // Arrange
        var attendance = new AttendanceRecord
        {
            AttendanceId = 50,
            ShiftAssignmentId = 100,
            ActualCheckInAt = new DateTime(2026, 4, 20, 8, 5, 0),
            ActualCheckOutAt = new DateTime(2026, 4, 20, 16, 10, 0)
        };
        var assignment = MakeAssignment(id: 100, staffId: 10, attendance: attendance);

        _assignmentRepoMock
            .Setup(r => r.GetAssignmentsAsync(It.IsAny<GetShiftAssignmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ShiftAssignment> { assignment }, 1));

        var service = CreateService();

        // Act
        var (items, _) = await service.GetMyShiftsAsync(10, new GetShiftAssignmentRequest());

        // Assert
        var dto = items.Should().ContainSingle().Subject;
        dto.Attendance.Should().NotBeNull();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetMyShiftsAsync")]
    public async Task GetMyShiftsAsync_WhenNullNavigationProperties_DefaultsToUnknown()
    {
        // Arrange — assignment with null ShiftTemplate, Staff, and AssignmentStatusLv
        var assignment = new ShiftAssignment
        {
            ShiftAssignmentId = 300,
            ShiftTemplateId = 1,
            StaffId = 10,
            WorkDate = TestWorkDate,
            PlannedStartAt = PlannedStart,
            PlannedEndAt = PlannedEnd,
            AssignmentStatusLvId = 23001,
            IsActive = true,
            AssignedBy = 1,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ShiftTemplate = null,
            Staff = null,
            AssignedByStaff = null,
            AssignmentStatusLv = null
        };

        _assignmentRepoMock
            .Setup(r => r.GetAssignmentsAsync(It.IsAny<GetShiftAssignmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ShiftAssignment> { assignment }, 1));

        var service = CreateService();

        // Act
        var (items, _) = await service.GetMyShiftsAsync(10, new GetShiftAssignmentRequest());

        // Assert
        var dto = items.Should().ContainSingle().Subject;
        dto.TemplateName.Should().Be("Unknown");
        dto.StaffName.Should().Be("Unknown");
        dto.AssignmentStatusCode.Should().Be("UNKNOWN");
        dto.AssignmentStatusName.Should().Be("Unknown");
        dto.AssignedByName.Should().Be("Unknown");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // GetTeamScheduleAsync
    // ══════════════════════════════════════════════════════════════

    #region GetTeamScheduleAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetTeamScheduleAsync")]
    public async Task GetTeamScheduleAsync_WhenValidWeek_ReturnsSchedule()
    {
        // Arrange
        var weekStart = new DateOnly(2026, 4, 20);
        var assignments = new List<ShiftAssignment> { MakeAssignment() };

        _assignmentRepoMock
            .Setup(r => r.GetTeamScheduleAsync(weekStart, weekStart.AddDays(6), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignments);

        var service = CreateService();

        // Act
        var result = await service.GetTeamScheduleAsync(new TeamScheduleRequest { WeekStart = weekStart });

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetTeamScheduleAsync")]
    public async Task GetTeamScheduleAsync_WhenWeekEndBeforeStart_ThrowsValidationException()
    {
        // Arrange
        var service = CreateService();
        var request = new TeamScheduleRequest
        {
            WeekStart = new DateOnly(2026, 4, 26),
            WeekEnd = new DateOnly(2026, 4, 20) // before start
        };

        // Act
        var act = () => service.GetTeamScheduleAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*WeekEnd*after*WeekStart*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetTeamScheduleAsync")]
    public async Task GetTeamScheduleAsync_WhenCustomWeekEnd_UsesProvidedEnd()
    {
        // Arrange
        var weekStart = new DateOnly(2026, 4, 20);
        var weekEnd = new DateOnly(2026, 4, 22); // 3-day range

        _assignmentRepoMock
            .Setup(r => r.GetTeamScheduleAsync(weekStart, weekEnd, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment>());

        var service = CreateService();

        // Act
        var result = await service.GetTeamScheduleAsync(new TeamScheduleRequest
        {
            WeekStart = weekStart,
            WeekEnd = weekEnd
        });

        // Assert
        result.Should().BeEmpty();
        _assignmentRepoMock.Verify(r => r.GetTeamScheduleAsync(weekStart, weekEnd, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetTeamScheduleAsync")]
    public async Task GetTeamScheduleAsync_WhenMultipleAssignments_ReturnsAllMapped()
    {
        // Arrange
        var weekStart = new DateOnly(2026, 4, 20);
        var assignments = new List<ShiftAssignment>
        {
            MakeAssignment(id: 100, staffId: 10),
            MakeAssignment(id: 101, staffId: 20),
            MakeAssignment(id: 102, staffId: 30)
        };

        _assignmentRepoMock
            .Setup(r => r.GetTeamScheduleAsync(weekStart, weekStart.AddDays(6), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignments);

        var service = CreateService();

        // Act
        var result = await service.GetTeamScheduleAsync(new TeamScheduleRequest { WeekStart = weekStart });

        // Assert
        result.Should().HaveCount(3);
        result.Select(r => r.ShiftAssignmentId).Should().BeEquivalentTo(new long[] { 100, 101, 102 });
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetTeamScheduleAsync")]
    public async Task GetTeamScheduleAsync_WhenShiftTemplateIdProvided_PassesToRepo()
    {
        // Arrange
        var weekStart = new DateOnly(2026, 4, 20);
        long templateId = 5;

        _assignmentRepoMock
            .Setup(r => r.GetTeamScheduleAsync(weekStart, weekStart.AddDays(6), templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { MakeAssignment() });

        var service = CreateService();

        // Act
        var result = await service.GetTeamScheduleAsync(new TeamScheduleRequest
        {
            WeekStart = weekStart,
            ShiftTemplateId = templateId
        });

        // Assert
        result.Should().HaveCount(1);
        _assignmentRepoMock.Verify(
            r => r.GetTeamScheduleAsync(weekStart, weekStart.AddDays(6), templateId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetTeamScheduleAsync")]
    public async Task GetTeamScheduleAsync_MapsFieldsToListDto()
    {
        // Arrange
        var weekStart = new DateOnly(2026, 4, 20);
        var assignment = MakeAssignment(id: 200, staffId: 10);

        _assignmentRepoMock
            .Setup(r => r.GetTeamScheduleAsync(weekStart, weekStart.AddDays(6), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { assignment });

        var service = CreateService();

        // Act
        var result = await service.GetTeamScheduleAsync(new TeamScheduleRequest { WeekStart = weekStart });

        // Assert
        var dto = result.Should().ContainSingle().Subject;
        dto.ShiftAssignmentId.Should().Be(200);
        dto.StaffId.Should().Be(10);
        dto.TemplateName.Should().Be("Morning Shift");
        dto.StaffName.Should().Be("Nguyen Van A");
        dto.WorkDate.Should().Be(TestWorkDate);
        dto.AssignmentStatusCode.Should().Be("ASSIGNED");
        dto.IsActive.Should().BeTrue();
        dto.Notes.Should().Be("Test notes");
        dto.Tags.Should().Be("test");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetTeamScheduleAsync")]
    public async Task GetTeamScheduleAsync_WhenNoAssignments_ReturnsEmptyList()
    {
        // Arrange
        var weekStart = new DateOnly(2026, 4, 20);

        _assignmentRepoMock
            .Setup(r => r.GetTeamScheduleAsync(weekStart, weekStart.AddDays(6), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment>());

        var service = CreateService();

        // Act
        var result = await service.GetTeamScheduleAsync(new TeamScheduleRequest { WeekStart = weekStart });

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetTeamScheduleAsync")]
    public async Task GetTeamScheduleAsync_WhenWeekEndEqualsWeekStart_ReturnsSingleDaySchedule()
    {
        // Arrange
        var singleDay = new DateOnly(2026, 4, 20);

        _assignmentRepoMock
            .Setup(r => r.GetTeamScheduleAsync(singleDay, singleDay, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { MakeAssignment() });

        var service = CreateService();

        // Act
        var result = await service.GetTeamScheduleAsync(new TeamScheduleRequest
        {
            WeekStart = singleDay,
            WeekEnd = singleDay
        });

        // Assert
        result.Should().HaveCount(1);
        _assignmentRepoMock.Verify(
            r => r.GetTeamScheduleAsync(singleDay, singleDay, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetTeamScheduleAsync")]
    public async Task GetTeamScheduleAsync_WhenNullNavigationProperties_DefaultsToUnknown()
    {
        // Arrange
        var weekStart = new DateOnly(2026, 4, 20);
        var assignment = new ShiftAssignment
        {
            ShiftAssignmentId = 300,
            ShiftTemplateId = 1,
            StaffId = 10,
            WorkDate = TestWorkDate,
            PlannedStartAt = PlannedStart,
            PlannedEndAt = PlannedEnd,
            AssignmentStatusLvId = 23001,
            IsActive = true,
            AssignedBy = 1,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ShiftTemplate = null,
            Staff = null,
            AssignedByStaff = null,
            AssignmentStatusLv = null
        };

        _assignmentRepoMock
            .Setup(r => r.GetTeamScheduleAsync(weekStart, weekStart.AddDays(6), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { assignment });

        var service = CreateService();

        // Act
        var result = await service.GetTeamScheduleAsync(new TeamScheduleRequest { WeekStart = weekStart });

        // Assert
        var dto = result.Should().ContainSingle().Subject;
        dto.TemplateName.Should().Be("Unknown");
        dto.StaffName.Should().Be("Unknown");
        dto.AssignmentStatusCode.Should().Be("UNKNOWN");
        dto.AssignmentStatusName.Should().Be("Unknown");
        dto.AssignedByName.Should().Be("Unknown");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetTeamScheduleAsync")]
    public async Task GetTeamScheduleAsync_WhenWeekEndNull_DefaultsToWeekStartPlus6()
    {
        // Arrange
        var weekStart = new DateOnly(2026, 4, 20);
        var expectedEnd = weekStart.AddDays(6); // 2026-04-26

        _assignmentRepoMock
            .Setup(r => r.GetTeamScheduleAsync(weekStart, expectedEnd, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { MakeAssignment() });

        var service = CreateService();

        // Act
        await service.GetTeamScheduleAsync(new TeamScheduleRequest { WeekStart = weekStart, WeekEnd = null });

        // Assert
        _assignmentRepoMock.Verify(
            r => r.GetTeamScheduleAsync(weekStart, expectedEnd, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetTeamScheduleAsync")]
    public async Task GetTeamScheduleAsync_WhenWeekEndOneDayBeforeStart_ThrowsValidationException()
    {
        // Arrange
        var service = CreateService();
        var request = new TeamScheduleRequest
        {
            WeekStart = new DateOnly(2026, 4, 21),
            WeekEnd = new DateOnly(2026, 4, 20) // 1 day before
        };

        // Act
        var act = () => service.GetTeamScheduleAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*WeekEnd*after*WeekStart*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // ReassignAsync
    // ══════════════════════════════════════════════════════════════

    #region ReassignAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ReassignAsync")]
    public async Task ReassignAsync_WhenValid_ReassignsToNewStaff()
    {
        // Arrange
        var assignment = MakeAssignment(staffId: 10);
        var newStaff = MakeStaff(20);
        var updatedAssignment = MakeAssignment(staffId: 20);

        _assignmentRepoMock
            .SetupSequence(r => r.GetByIdWithDetailsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment)
            .ReturnsAsync(updatedAssignment);
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newStaff);
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(20, TestWorkDate, It.IsAny<DateTime>(), It.IsAny<DateTime>(), 100L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        SetupRealtimePublisher();

        var request = new ReassignRequest { NewStaffId = 20, Reason = "Schedule change" };
        var service = CreateService();

        // Act
        var result = await service.ReassignAsync(100, request, reassignedByStaffId: 1);

        // Assert
        result.Should().NotBeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ReassignAsync")]
    public async Task ReassignAsync_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShiftAssignment?)null);

        var service = CreateService();

        // Act
        var act = () => service.ReassignAsync(999, new ReassignRequest { NewStaffId = 20 }, 1);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ReassignAsync")]
    public async Task ReassignAsync_WhenCancelled_ThrowsValidationException()
    {
        // Arrange
        var assignment = MakeAssignment(isActive: false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var service = CreateService();

        // Act
        var act = () => service.ReassignAsync(100, new ReassignRequest { NewStaffId = 20 }, 1);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*cancelled*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ReassignAsync")]
    public async Task ReassignAsync_WhenCheckedIn_ThrowsConflictException()
    {
        // Arrange
        var attendance = new AttendanceRecord { ActualCheckInAt = DateTime.UtcNow };
        var assignment = MakeAssignment(attendance: attendance);
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var service = CreateService();

        // Act
        var act = () => service.ReassignAsync(100, new ReassignRequest { NewStaffId = 20 }, 1);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*checked in*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ReassignAsync")]
    public async Task ReassignAsync_WhenSameStaffAndDate_ThrowsValidationException()
    {
        // Arrange
        var assignment = MakeAssignment(staffId: 10);
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeStaff(10));

        var request = new ReassignRequest { NewStaffId = 10 }; // same staff, no new date
        var service = CreateService();

        // Act
        var act = () => service.ReassignAsync(100, request, 1);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*already belongs*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ReassignAsync")]
    public async Task ReassignAsync_WhenNewStaffNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var assignment = MakeAssignment(staffId: 10);
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        var request = new ReassignRequest { NewStaffId = 20 };
        var service = CreateService();

        // Act
        var act = () => service.ReassignAsync(100, request, 1);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*staff account*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ReassignAsync")]
    public async Task ReassignAsync_WhenOverlapping_ThrowsConflictException()
    {
        // Arrange
        var assignment = MakeAssignment(staffId: 10);
        _assignmentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeStaff(20));
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(20, TestWorkDate, It.IsAny<DateTime>(), It.IsAny<DateTime>(), 100L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new ReassignRequest { NewStaffId = 20 };
        var service = CreateService();

        // Act
        var act = () => service.ReassignAsync(100, request, 1);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*overlapping*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // CopyWeekAsync
    // ══════════════════════════════════════════════════════════════

    #region CopyWeekAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CopyWeekAsync")]
    public async Task CopyWeekAsync_WhenValid_CopiesAssignments()
    {
        // Arrange
        var sourceMonday = new DateOnly(2026, 4, 20); // Monday
        var targetMonday = new DateOnly(2026, 4, 27); // next Monday
        var sourceAssignment = MakeAssignment(staffId: 10);
        sourceAssignment.WorkDate = sourceMonday;

        _assignmentRepoMock
            .Setup(r => r.GetWeekAssignmentsAsync(sourceMonday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { sourceAssignment });
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { MakeAssignment() });

        SetupLookupResolver();
        SetupRealtimePublisher();

        var request = new CopyWeekRequest
        {
            SourceWeekStart = sourceMonday,
            TargetWeekStart = targetMonday,
            AsDraft = true
        };
        var service = CreateService();

        // Act
        var result = await service.CopyWeekAsync(request, assignedByStaffId: 1);

        // Assert
        result.Should().NotBeEmpty();
        _assignmentRepoMock.Verify(r => r.AddRange(It.IsAny<IEnumerable<ShiftAssignment>>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CopyWeekAsync")]
    public async Task CopyWeekAsync_WhenSourceNotMonday_ThrowsValidationException()
    {
        // Arrange
        var request = new CopyWeekRequest
        {
            SourceWeekStart = new DateOnly(2026, 4, 21), // Tuesday
            TargetWeekStart = new DateOnly(2026, 4, 27)
        };
        var service = CreateService();

        // Act
        var act = () => service.CopyWeekAsync(request, 1);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Monday*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CopyWeekAsync")]
    public async Task CopyWeekAsync_WhenTargetNotMonday_ThrowsValidationException()
    {
        // Arrange
        var request = new CopyWeekRequest
        {
            SourceWeekStart = new DateOnly(2026, 4, 20), // Monday
            TargetWeekStart = new DateOnly(2026, 4, 28)  // Tuesday
        };
        var service = CreateService();

        // Act
        var act = () => service.CopyWeekAsync(request, 1);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Monday*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CopyWeekAsync")]
    public async Task CopyWeekAsync_WhenNoSourceAssignments_ThrowsNotFoundException()
    {
        // Arrange
        var sourceMonday = new DateOnly(2026, 4, 20);
        _assignmentRepoMock
            .Setup(r => r.GetWeekAssignmentsAsync(sourceMonday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment>());

        var request = new CopyWeekRequest
        {
            SourceWeekStart = sourceMonday,
            TargetWeekStart = new DateOnly(2026, 4, 27)
        };
        var service = CreateService();

        // Act
        var act = () => service.CopyWeekAsync(request, 1);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*source week*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CopyWeekAsync")]
    public async Task CopyWeekAsync_WhenAllSlotsFilled_ThrowsConflictException()
    {
        // Arrange
        var sourceMonday = new DateOnly(2026, 4, 20);
        var targetMonday = new DateOnly(2026, 4, 27);
        var sourceAssignment = MakeAssignment(staffId: 10);
        sourceAssignment.WorkDate = sourceMonday;

        _assignmentRepoMock
            .Setup(r => r.GetWeekAssignmentsAsync(sourceMonday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { sourceAssignment });
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { 10 }); // already assigned

        SetupLookupResolver();

        var request = new CopyWeekRequest
        {
            SourceWeekStart = sourceMonday,
            TargetWeekStart = targetMonday
        };
        var service = CreateService();

        // Act
        var act = () => service.CopyWeekAsync(request, 1);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already filled*overlap*");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CopyWeekAsync")]
    public async Task CopyWeekAsync_WhenAsDraftFalse_UsesAssignedStatus()
    {
        // Arrange
        var sourceMonday = new DateOnly(2026, 4, 20);
        var targetMonday = new DateOnly(2026, 4, 27);
        var sourceAssignment = MakeAssignment(staffId: 10);
        sourceAssignment.WorkDate = sourceMonday;

        _assignmentRepoMock
            .Setup(r => r.GetWeekAssignmentsAsync(sourceMonday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { sourceAssignment });
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { MakeAssignment() });

        SetupLookupResolver();
        SetupRealtimePublisher();

        var request = new CopyWeekRequest
        {
            SourceWeekStart = sourceMonday,
            TargetWeekStart = targetMonday,
            AsDraft = false
        };
        var service = CreateService();

        // Act
        var result = await service.CopyWeekAsync(request, assignedByStaffId: 1);

        // Assert
        result.Should().NotBeEmpty();
        _lookupResolverMock.Verify(
            r => r.GetIdAsync(It.IsAny<ushort>(), It.IsAny<System.Enum>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CopyWeekAsync")]
    public async Task CopyWeekAsync_WhenMultipleSourceAssignments_CopiesAll()
    {
        // Arrange
        var sourceMonday = new DateOnly(2026, 4, 20);
        var targetMonday = new DateOnly(2026, 4, 27);

        var src1 = MakeAssignment(id: 100, staffId: 10);
        src1.WorkDate = sourceMonday;
        var src2 = MakeAssignment(id: 101, staffId: 20);
        src2.WorkDate = sourceMonday.AddDays(1); // Tuesday
        var src3 = MakeAssignment(id: 102, staffId: 30);
        src3.WorkDate = sourceMonday.AddDays(2); // Wednesday

        _assignmentRepoMock
            .Setup(r => r.GetWeekAssignmentsAsync(sourceMonday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { src1, src2, src3 });
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var savedList = new List<ShiftAssignment> { MakeAssignment(id: 200), MakeAssignment(id: 201), MakeAssignment(id: 202) };
        _assignmentRepoMock
            .Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedList);

        SetupLookupResolver();
        SetupRealtimePublisher();

        var request = new CopyWeekRequest { SourceWeekStart = sourceMonday, TargetWeekStart = targetMonday, AsDraft = true };
        var service = CreateService();

        // Act
        var result = await service.CopyWeekAsync(request, assignedByStaffId: 1);

        // Assert
        result.Should().HaveCount(3);
        _assignmentRepoMock.Verify(
            r => r.AddRange(It.Is<IEnumerable<ShiftAssignment>>(e => e.Count() == 3)),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CopyWeekAsync")]
    public async Task CopyWeekAsync_ShiftsDatesByDayOffset()
    {
        // Arrange
        var sourceMonday = new DateOnly(2026, 4, 20);
        var targetMonday = new DateOnly(2026, 5, 4); // 2 weeks later
        var dayOffset = 14;

        var src = MakeAssignment(staffId: 10);
        src.WorkDate = sourceMonday.AddDays(2); // Wednesday Apr 22
        src.PlannedStartAt = new DateTime(2026, 4, 22, 8, 0, 0);
        src.PlannedEndAt = new DateTime(2026, 4, 22, 16, 0, 0);

        _assignmentRepoMock
            .Setup(r => r.GetWeekAssignmentsAsync(sourceMonday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { src });
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { MakeAssignment() });

        SetupLookupResolver();
        SetupRealtimePublisher();

        ShiftAssignment? capturedEntity = null;
        _assignmentRepoMock
            .Setup(r => r.AddRange(It.IsAny<IEnumerable<ShiftAssignment>>()))
            .Callback<IEnumerable<ShiftAssignment>>(entities => capturedEntity = entities.First());

        var request = new CopyWeekRequest { SourceWeekStart = sourceMonday, TargetWeekStart = targetMonday, AsDraft = true };
        var service = CreateService();

        // Act
        await service.CopyWeekAsync(request, assignedByStaffId: 1);

        // Assert
        capturedEntity.Should().NotBeNull();
        capturedEntity!.WorkDate.Should().Be(sourceMonday.AddDays(2 + dayOffset)); // Wed May 6
        capturedEntity.PlannedStartAt.Should().Be(new DateTime(2026, 5, 6, 8, 0, 0));
        capturedEntity.PlannedEndAt.Should().Be(new DateTime(2026, 5, 6, 16, 0, 0));
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CopyWeekAsync")]
    public async Task CopyWeekAsync_CopiesNotesAndTagsFromSource()
    {
        // Arrange
        var sourceMonday = new DateOnly(2026, 4, 20);
        var targetMonday = new DateOnly(2026, 4, 27);

        var src = MakeAssignment(staffId: 10);
        src.WorkDate = sourceMonday;
        src.Notes = "Important notes";
        src.Tags = "vip,morning";

        _assignmentRepoMock
            .Setup(r => r.GetWeekAssignmentsAsync(sourceMonday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { src });
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { MakeAssignment() });

        SetupLookupResolver();
        SetupRealtimePublisher();

        ShiftAssignment? capturedEntity = null;
        _assignmentRepoMock
            .Setup(r => r.AddRange(It.IsAny<IEnumerable<ShiftAssignment>>()))
            .Callback<IEnumerable<ShiftAssignment>>(entities => capturedEntity = entities.First());

        var request = new CopyWeekRequest { SourceWeekStart = sourceMonday, TargetWeekStart = targetMonday, AsDraft = true };
        var service = CreateService();

        // Act
        await service.CopyWeekAsync(request, assignedByStaffId: 1);

        // Assert
        capturedEntity.Should().NotBeNull();
        capturedEntity!.Notes.Should().Be("Important notes");
        capturedEntity.Tags.Should().Be("vip,morning");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CopyWeekAsync")]
    public async Task CopyWeekAsync_SetsAssignedByToCallerStaffId()
    {
        // Arrange
        var sourceMonday = new DateOnly(2026, 4, 20);
        var targetMonday = new DateOnly(2026, 4, 27);
        var src = MakeAssignment(staffId: 10);
        src.WorkDate = sourceMonday;

        _assignmentRepoMock
            .Setup(r => r.GetWeekAssignmentsAsync(sourceMonday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { src });
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { MakeAssignment() });

        SetupLookupResolver();
        SetupRealtimePublisher();

        ShiftAssignment? capturedEntity = null;
        _assignmentRepoMock
            .Setup(r => r.AddRange(It.IsAny<IEnumerable<ShiftAssignment>>()))
            .Callback<IEnumerable<ShiftAssignment>>(entities => capturedEntity = entities.First());

        var request = new CopyWeekRequest { SourceWeekStart = sourceMonday, TargetWeekStart = targetMonday, AsDraft = true };
        var service = CreateService();

        // Act
        await service.CopyWeekAsync(request, assignedByStaffId: 42);

        // Assert
        capturedEntity.Should().NotBeNull();
        capturedEntity!.AssignedBy.Should().Be(42);
        capturedEntity.IsActive.Should().BeTrue();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CopyWeekAsync")]
    public async Task CopyWeekAsync_PublishesRealtimeEvent()
    {
        // Arrange
        var sourceMonday = new DateOnly(2026, 4, 20);
        var targetMonday = new DateOnly(2026, 4, 27);
        var src = MakeAssignment(staffId: 10);
        src.WorkDate = sourceMonday;

        _assignmentRepoMock
            .Setup(r => r.GetWeekAssignmentsAsync(sourceMonday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { src });
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { MakeAssignment() });

        SetupLookupResolver();
        SetupRealtimePublisher();

        var request = new CopyWeekRequest { SourceWeekStart = sourceMonday, TargetWeekStart = targetMonday, AsDraft = true };
        var service = CreateService();

        // Act
        await service.CopyWeekAsync(request, assignedByStaffId: 1);

        // Assert
        _realtimePublisherMock.Verify(
            p => p.PublishBoardChangedAsync(
                It.Is<ShiftLiveRealtimeEventDto>(e => e.EventType == "assignments_copied"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CopyWeekAsync")]
    public async Task CopyWeekAsync_WhenSomeSkippedDueToAlreadyAssigned_CopiesRemaining()
    {
        // Arrange
        var sourceMonday = new DateOnly(2026, 4, 20);
        var targetMonday = new DateOnly(2026, 4, 27);

        var src1 = MakeAssignment(id: 100, staffId: 10);
        src1.WorkDate = sourceMonday;
        var src2 = MakeAssignment(id: 101, staffId: 20);
        src2.WorkDate = sourceMonday;

        _assignmentRepoMock
            .Setup(r => r.GetWeekAssignmentsAsync(sourceMonday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { src1, src2 });

        // Staff 10 already assigned on target date, staff 20 is not
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.Is<IEnumerable<long>>(ids => ids.Contains(10)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { 10 });
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.Is<IEnumerable<long>>(ids => ids.Contains(20)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());

        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { MakeAssignment(id: 200, staffId: 20) });

        SetupLookupResolver();
        SetupRealtimePublisher();

        var request = new CopyWeekRequest { SourceWeekStart = sourceMonday, TargetWeekStart = targetMonday, AsDraft = true };
        var service = CreateService();

        // Act
        var result = await service.CopyWeekAsync(request, assignedByStaffId: 1);

        // Assert
        result.Should().HaveCount(1);
        _assignmentRepoMock.Verify(
            r => r.AddRange(It.Is<IEnumerable<ShiftAssignment>>(e => e.Count() == 1)),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CopyWeekAsync")]
    public async Task CopyWeekAsync_WhenSomeSkippedDueToOverlap_CopiesRemaining()
    {
        // Arrange
        var sourceMonday = new DateOnly(2026, 4, 20);
        var targetMonday = new DateOnly(2026, 4, 27);

        var src1 = MakeAssignment(id: 100, staffId: 10);
        src1.WorkDate = sourceMonday;
        var src2 = MakeAssignment(id: 101, staffId: 20);
        src2.WorkDate = sourceMonday;

        _assignmentRepoMock
            .Setup(r => r.GetWeekAssignmentsAsync(sourceMonday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { src1, src2 });
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());

        // Staff 10 has overlap, staff 20 does not
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(10, It.IsAny<DateOnly>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(20, It.IsAny<DateOnly>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _assignmentRepoMock
            .Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { MakeAssignment(id: 200, staffId: 20) });

        SetupLookupResolver();
        SetupRealtimePublisher();

        var request = new CopyWeekRequest { SourceWeekStart = sourceMonday, TargetWeekStart = targetMonday, AsDraft = true };
        var service = CreateService();

        // Act
        var result = await service.CopyWeekAsync(request, assignedByStaffId: 1);

        // Assert
        result.Should().HaveCount(1);
        _assignmentRepoMock.Verify(
            r => r.AddRange(It.Is<IEnumerable<ShiftAssignment>>(e => e.Count() == 1 && e.First().StaffId == 20)),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CopyWeekAsync")]
    public async Task CopyWeekAsync_WhenAllOverlapping_ThrowsConflictException()
    {
        // Arrange
        var sourceMonday = new DateOnly(2026, 4, 20);
        var targetMonday = new DateOnly(2026, 4, 27);
        var src = MakeAssignment(staffId: 10);
        src.WorkDate = sourceMonday;

        _assignmentRepoMock
            .Setup(r => r.GetWeekAssignmentsAsync(sourceMonday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { src });
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>()); // not already assigned
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // but overlaps

        SetupLookupResolver();

        var request = new CopyWeekRequest { SourceWeekStart = sourceMonday, TargetWeekStart = targetMonday, AsDraft = true };
        var service = CreateService();

        // Act
        var act = () => service.CopyWeekAsync(request, 1);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already filled*overlap*");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CopyWeekAsync")]
    public async Task CopyWeekAsync_SavesAndReloadsWithDetails()
    {
        // Arrange
        var sourceMonday = new DateOnly(2026, 4, 20);
        var targetMonday = new DateOnly(2026, 4, 27);
        var src = MakeAssignment(staffId: 10);
        src.WorkDate = sourceMonday;

        _assignmentRepoMock
            .Setup(r => r.GetWeekAssignmentsAsync(sourceMonday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { src });
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { MakeAssignment() });

        SetupLookupResolver();
        SetupRealtimePublisher();

        var request = new CopyWeekRequest { SourceWeekStart = sourceMonday, TargetWeekStart = targetMonday, AsDraft = true };
        var service = CreateService();

        // Act
        await service.CopyWeekAsync(request, assignedByStaffId: 1);

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // PublishAssignmentsAsync
    // ══════════════════════════════════════════════════════════════

    #region PublishAssignmentsAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "PublishAssignmentsAsync")]
    public async Task PublishAssignmentsAsync_WhenByIds_PublishesDrafts()
    {
        // Arrange
        var draftLvId = 23000u;
        var assignedLvId = 23001u;
        var draftAssignment = MakeAssignment(statusLvId: draftLvId);
        draftAssignment.AssignmentStatusLv = new LookupValue { ValueId = draftLvId, TypeId = 23, ValueCode = "DRAFT", ValueName = "Draft" };

        _assignmentRepoMock
            .Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { draftAssignment });

        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.Is<System.Enum>(e => e.ToString() == "DRAFT"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftLvId);
        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.Is<System.Enum>(e => e.ToString() == "ASSIGNED"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignedLvId);

        SetupRealtimePublisher();

        var request = new PublishAssignmentsRequest { AssignmentIds = new List<long> { 100 } };
        var service = CreateService();

        // Act
        var result = await service.PublishAssignmentsAsync(request, publishedByStaffId: 1);

        // Assert
        result.Should().HaveCount(1);
        draftAssignment.AssignmentStatusLvId.Should().Be(assignedLvId);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "PublishAssignmentsAsync")]
    public async Task PublishAssignmentsAsync_WhenByDateRange_PublishesDrafts()
    {
        // Arrange
        var draftLvId = 23000u;
        var assignedLvId = 23001u;
        var from = new DateOnly(2026, 4, 20);
        var to = new DateOnly(2026, 4, 26);

        var draftAssignment = MakeAssignment(statusLvId: draftLvId);
        draftAssignment.AssignmentStatusLv = new LookupValue { ValueId = draftLvId, TypeId = 23, ValueCode = "DRAFT", ValueName = "Draft" };

        _assignmentRepoMock
            .Setup(r => r.GetDraftAssignmentsAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { draftAssignment });

        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.Is<System.Enum>(e => e.ToString() == "ASSIGNED"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignedLvId);

        SetupRealtimePublisher();

        var request = new PublishAssignmentsRequest { FromDate = from, ToDate = to };
        var service = CreateService();

        // Act
        var result = await service.PublishAssignmentsAsync(request, publishedByStaffId: 1);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "PublishAssignmentsAsync")]
    public async Task PublishAssignmentsAsync_WhenNeitherIdsNorDates_ThrowsValidationException()
    {
        // Arrange
        var request = new PublishAssignmentsRequest(); // no IDs, no dates
        var service = CreateService();

        // Act
        var act = () => service.PublishAssignmentsAsync(request, publishedByStaffId: 1);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*AssignmentIds*FromDate*ToDate*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "PublishAssignmentsAsync")]
    public async Task PublishAssignmentsAsync_WhenNoDraftsFound_ReturnsEmptyList()
    {
        // Arrange
        var draftLvId = 23000u;

        _assignmentRepoMock
            .Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment>()); // no drafts

        _lookupResolverMock
            .Setup(r => r.GetIdAsync(It.IsAny<ushort>(), It.Is<System.Enum>(e => e.ToString() == "DRAFT"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftLvId);

        var request = new PublishAssignmentsRequest { AssignmentIds = new List<long> { 100 } };
        var service = CreateService();

        // Act
        var result = await service.PublishAssignmentsAsync(request, publishedByStaffId: 1);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // BulkCreateAssignmentsAsync
    // ══════════════════════════════════════════════════════════════

    #region BulkCreateAssignmentsAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "BulkCreateAssignmentsAsync")]
    public async Task BulkCreateAssignmentsAsync_WhenValid_CreatesMultipleAssignments()
    {
        // Arrange
        var template = MakeActiveTemplate();
        var staff1 = MakeStaff(10);
        var staff2 = MakeStaff(20);

        _templateRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff1);
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff2);
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());
        _assignmentRepoMock
            .Setup(r => r.HasOverlappingAssignmentAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assignmentRepoMock
            .Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShiftAssignment> { MakeAssignment(id: 100), MakeAssignment(id: 101, staffId: 20) });

        SetupLookupResolver();
        SetupRealtimePublisher();

        var request = new BulkCreateAssignmentRequest
        {
            ShiftTemplateId = 1,
            StaffIds = new List<long> { 10, 20 },
            WorkDate = TestWorkDate,
            PlannedStartAt = PlannedStart,
            PlannedEndAt = PlannedEnd,
        };
        var service = CreateService();

        // Act
        var result = await service.BulkCreateAssignmentsAsync(request, assignedByStaffId: 1);

        // Assert
        result.Should().HaveCount(2);
        _assignmentRepoMock.Verify(r => r.AddRange(It.IsAny<IEnumerable<ShiftAssignment>>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "BulkCreateAssignmentsAsync")]
    public async Task BulkCreateAssignmentsAsync_WhenTemplateNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShiftTemplate?)null);

        var request = new BulkCreateAssignmentRequest
        {
            ShiftTemplateId = 999,
            StaffIds = new List<long> { 10 },
            WorkDate = TestWorkDate
        };
        var service = CreateService();

        // Act
        var act = () => service.BulkCreateAssignmentsAsync(request, 1);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*template*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "BulkCreateAssignmentsAsync")]
    public async Task BulkCreateAssignmentsAsync_WhenAllOverlapping_ThrowsConflictException()
    {
        // Arrange
        var template = MakeActiveTemplate();
        var staff = MakeStaff(10);

        _templateRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);
        _assignmentRepoMock
            .Setup(r => r.GetAlreadyAssignedStaffIdsAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { 10 }); // all already assigned

        SetupLookupResolver();

        var request = new BulkCreateAssignmentRequest
        {
            ShiftTemplateId = 1,
            StaffIds = new List<long> { 10 },
            WorkDate = TestWorkDate,
            PlannedStartAt = PlannedStart,
            PlannedEndAt = PlannedEnd,
        };
        var service = CreateService();

        // Act
        var act = () => service.BulkCreateAssignmentsAsync(request, 1);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*No assignments could be created*");
    }

    #endregion
}
