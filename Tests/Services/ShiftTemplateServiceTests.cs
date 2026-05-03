using Core.DTO.Shift;
using Core.Entity;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Service;
using Moq;
using FluentAssertions;

namespace Tests.Services;

/// <summary>
/// Unit Test — ShiftTemplateService.CreateAsync, UpdateAsync
/// Code Module : Core/Service/ShiftTemplateService.cs
/// Methods     : CreateAsync, UpdateAsync
/// Created By  : Tester
/// Executed By : Tester
/// Test Req.   : Staff creates and updates shift templates used for scheduling
/// </summary>
public class ShiftTemplateServiceTests
{
    // ── Mocks ──
    private readonly Mock<IShiftTemplateRepository> _templateRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    // ── Factory ──
    private ShiftTemplateService CreateService() => new(
        _templateRepoMock.Object,
        _unitOfWorkMock.Object
    );

    // ── Helpers ──
    private static ShiftTemplate MakeValidTemplate(long id = 1L) => new()
    {
        ShiftTemplateId = id,
        TemplateName = "Morning",
        DefaultStartTime = new TimeOnly(6, 0),
        DefaultEndTime = new TimeOnly(14, 0),
        Description = "Morning shift",
        BufferBeforeMinutes = 15,
        BufferAfterMinutes = 10,
        IsActive = true,
        CreatedBy = 100L,
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CreatedByStaff = new StaffAccount { FullName = "Admin User" }
    };

    private static CreateShiftTemplateRequest MakeValidCreateRequest() => new()
    {
        TemplateName = "Morning",
        DefaultStartTime = new TimeOnly(6, 0),
        DefaultEndTime = new TimeOnly(14, 0),
        Description = "Morning shift",
        BufferBeforeMinutes = 15,
        BufferAfterMinutes = 10
    };

    // ══════════════════════════════════════════════════════════════
    //  CreateAsync
    // ══════════════════════════════════════════════════════════════

    #region CreateAsync

    /// <summary>UTCID01 — Normal: valid request creates template and returns detail DTO.</summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateAsync")]
    public async Task CreateAsync_ValidRequest_ReturnsDetailDto()
    {
        // Arrange
        var request = MakeValidCreateRequest();
        var saved = MakeValidTemplate();

        _templateRepoMock
            .Setup(r => r.ExistsByNameAsync(request.TemplateName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saved);

        var svc = CreateService();

        // Act
        var result = await svc.CreateAsync(request, createdByStaffId: 100L);

        // Assert
        result.Should().NotBeNull();
        result.TemplateName.Should().Be("Morning");
        result.DefaultStartTime.Should().Be(new TimeOnly(6, 0));
        result.DefaultEndTime.Should().Be(new TimeOnly(14, 0));
        result.Description.Should().Be("Morning shift");
        result.BufferBeforeMinutes.Should().Be(15);
        result.BufferAfterMinutes.Should().Be(10);
        result.IsActive.Should().BeTrue();
        result.CreatedByName.Should().Be("Admin User");

        _templateRepoMock.Verify(r => r.Add(It.IsAny<ShiftTemplate>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID02 — Normal: description is null, buffers are null — optional fields omitted.</summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateAsync")]
    public async Task CreateAsync_NullOptionalFields_ReturnsDetailDto()
    {
        // Arrange
        var request = new CreateShiftTemplateRequest
        {
            TemplateName = "Evening",
            DefaultStartTime = new TimeOnly(18, 0),
            DefaultEndTime = new TimeOnly(23, 0),
            Description = null,
            BufferBeforeMinutes = null,
            BufferAfterMinutes = null
        };

        var saved = MakeValidTemplate();
        saved.TemplateName = "Evening";
        saved.DefaultStartTime = new TimeOnly(18, 0);
        saved.DefaultEndTime = new TimeOnly(23, 0);
        saved.Description = null;
        saved.BufferBeforeMinutes = null;
        saved.BufferAfterMinutes = null;

        _templateRepoMock
            .Setup(r => r.ExistsByNameAsync("Evening", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saved);

        var svc = CreateService();

        // Act
        var result = await svc.CreateAsync(request, createdByStaffId: 100L);

        // Assert
        result.Should().NotBeNull();
        result.TemplateName.Should().Be("Evening");
        result.Description.Should().BeNull();
        result.BufferBeforeMinutes.Should().BeNull();
        result.BufferAfterMinutes.Should().BeNull();
    }

    /// <summary>UTCID03 — Abnormal: start time equals end time → ValidationException.</summary>
    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateAsync")]
    public async Task CreateAsync_StartTimeEqualsEndTime_ThrowsValidationException()
    {
        // Arrange
        var request = MakeValidCreateRequest();
        request.DefaultStartTime = new TimeOnly(10, 0);
        request.DefaultEndTime = new TimeOnly(10, 0);

        var svc = CreateService();

        // Act
        var act = () => svc.CreateAsync(request, createdByStaffId: 100L);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*start time must be earlier*");
    }

    /// <summary>UTCID04 — Abnormal: start time after end time → ValidationException.</summary>
    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateAsync")]
    public async Task CreateAsync_StartTimeAfterEndTime_ThrowsValidationException()
    {
        // Arrange
        var request = MakeValidCreateRequest();
        request.DefaultStartTime = new TimeOnly(15, 0);
        request.DefaultEndTime = new TimeOnly(8, 0);

        var svc = CreateService();

        // Act
        var act = () => svc.CreateAsync(request, createdByStaffId: 100L);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*start time must be earlier*");
    }

    /// <summary>UTCID05 — Abnormal: duplicate template name → ConflictException.</summary>
    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateAsync")]
    public async Task CreateAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        var request = MakeValidCreateRequest();

        _templateRepoMock
            .Setup(r => r.ExistsByNameAsync(request.TemplateName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var svc = CreateService();

        // Act
        var act = () => svc.CreateAsync(request, createdByStaffId: 100L);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already exists*");
    }

    /// <summary>UTCID06 — Boundary: template name with leading/trailing spaces → trimmed in entity.</summary>
    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateAsync")]
    public async Task CreateAsync_NameWithSpaces_TrimsName()
    {
        // Arrange
        var request = MakeValidCreateRequest();
        request.TemplateName = "  Morning  ";

        ShiftTemplate? captured = null;
        _templateRepoMock
            .Setup(r => r.ExistsByNameAsync("  Morning  ", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _templateRepoMock
            .Setup(r => r.Add(It.IsAny<ShiftTemplate>()))
            .Callback<ShiftTemplate>(e => captured = e);

        var saved = MakeValidTemplate();
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saved);

        var svc = CreateService();

        // Act
        await svc.CreateAsync(request, createdByStaffId: 100L);

        // Assert
        captured.Should().NotBeNull();
        captured!.TemplateName.Should().Be("Morning");
    }

    /// <summary>UTCID07 — Boundary: description with spaces → trimmed.</summary>
    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateAsync")]
    public async Task CreateAsync_DescriptionWithSpaces_TrimsDescription()
    {
        // Arrange
        var request = MakeValidCreateRequest();
        request.Description = "  Early birds  ";

        ShiftTemplate? captured = null;
        _templateRepoMock
            .Setup(r => r.ExistsByNameAsync(request.TemplateName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _templateRepoMock
            .Setup(r => r.Add(It.IsAny<ShiftTemplate>()))
            .Callback<ShiftTemplate>(e => captured = e);

        var saved = MakeValidTemplate();
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saved);

        var svc = CreateService();

        // Act
        await svc.CreateAsync(request, createdByStaffId: 100L);

        // Assert
        captured.Should().NotBeNull();
        captured!.Description.Should().Be("Early birds");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    //  UpdateAsync
    // ══════════════════════════════════════════════════════════════

    #region UpdateAsync

    /// <summary>UTCID01 — Normal: update all fields on an existing template.</summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateAsync")]
    public async Task UpdateAsync_AllFields_ReturnsUpdatedDto()
    {
        // Arrange
        var existing = MakeValidTemplate();
        var request = new UpdateShiftTemplateRequest
        {
            TemplateName = "Afternoon",
            DefaultStartTime = new TimeOnly(12, 0),
            DefaultEndTime = new TimeOnly(20, 0),
            Description = "Afternoon shift",
            IsActive = false,
            BufferBeforeMinutes = 20,
            BufferAfterMinutes = 5
        };

        _templateRepoMock
            .Setup(r => r.GetByIdAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _templateRepoMock
            .Setup(r => r.ExistsByNameAsync("Afternoon", 1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var updated = MakeValidTemplate();
        updated.TemplateName = "Afternoon";
        updated.DefaultStartTime = new TimeOnly(12, 0);
        updated.DefaultEndTime = new TimeOnly(20, 0);
        updated.Description = "Afternoon shift";
        updated.IsActive = false;
        updated.BufferBeforeMinutes = 20;
        updated.BufferAfterMinutes = 5;
        updated.UpdatedByStaff = new StaffAccount { FullName = "Manager" };

        // Second GetByIdAsync call (after save) returns updated entity
        _templateRepoMock
            .SetupSequence(r => r.GetByIdAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing)
            .ReturnsAsync(updated);

        var svc = CreateService();

        // Act
        var result = await svc.UpdateAsync(1L, request, updatedByStaffId: 200L);

        // Assert
        result.Should().NotBeNull();
        result.TemplateName.Should().Be("Afternoon");
        result.DefaultStartTime.Should().Be(new TimeOnly(12, 0));
        result.DefaultEndTime.Should().Be(new TimeOnly(20, 0));
        result.Description.Should().Be("Afternoon shift");
        result.IsActive.Should().BeFalse();
        result.BufferBeforeMinutes.Should().Be(20);
        result.BufferAfterMinutes.Should().Be(5);
        result.UpdatedByName.Should().Be("Manager");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID02 — Normal: partial update — only description changed, other fields stay.</summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateAsync")]
    public async Task UpdateAsync_OnlyDescription_ReturnsUpdatedDto()
    {
        // Arrange
        var existing = MakeValidTemplate();
        var request = new UpdateShiftTemplateRequest
        {
            Description = "Updated description"
        };

        var updated = MakeValidTemplate();
        updated.Description = "Updated description";

        _templateRepoMock
            .SetupSequence(r => r.GetByIdAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing)
            .ReturnsAsync(updated);

        var svc = CreateService();

        // Act
        var result = await svc.UpdateAsync(1L, request, updatedByStaffId: 200L);

        // Assert
        result.Description.Should().Be("Updated description");
        result.TemplateName.Should().Be("Morning"); // unchanged
        result.DefaultStartTime.Should().Be(new TimeOnly(6, 0)); // unchanged
    }

    /// <summary>UTCID03 — Abnormal: template id not found → NotFoundException.</summary>
    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateAsync")]
    public async Task UpdateAsync_IdNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(999L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShiftTemplate?)null);

        var request = new UpdateShiftTemplateRequest { TemplateName = "Any" };
        var svc = CreateService();

        // Act
        var act = () => svc.UpdateAsync(999L, request, updatedByStaffId: 200L);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Shift template not found*");
    }

    /// <summary>UTCID04 — Abnormal: rename to blank (whitespace-only) name → ValidationException.</summary>
    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateAsync")]
    public async Task UpdateAsync_BlankName_ThrowsValidationException()
    {
        // Arrange
        var existing = MakeValidTemplate();
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new UpdateShiftTemplateRequest { TemplateName = "   " };
        var svc = CreateService();

        // Act
        var act = () => svc.UpdateAsync(1L, request, updatedByStaffId: 200L);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*cannot be blank*");
    }

    /// <summary>UTCID05 — Abnormal: rename to a name already used by another template → ConflictException.</summary>
    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateAsync")]
    public async Task UpdateAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        var existing = MakeValidTemplate();
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _templateRepoMock
            .Setup(r => r.ExistsByNameAsync("Dinner", 1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateShiftTemplateRequest { TemplateName = "Dinner" };
        var svc = CreateService();

        // Act
        var act = () => svc.UpdateAsync(1L, request, updatedByStaffId: 200L);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already exists*");
    }

    /// <summary>UTCID06 — Abnormal: new start time >= existing end time → ValidationException.</summary>
    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateAsync")]
    public async Task UpdateAsync_NewStartTimeAfterExistingEnd_ThrowsValidationException()
    {
        // Arrange
        var existing = MakeValidTemplate(); // EndTime = 14:00
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new UpdateShiftTemplateRequest
        {
            DefaultStartTime = new TimeOnly(15, 0) // 15:00 >= 14:00
        };
        var svc = CreateService();

        // Act
        var act = () => svc.UpdateAsync(1L, request, updatedByStaffId: 200L);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*start time must be earlier*");
    }

    /// <summary>UTCID07 — Boundary: update only start time, keeping valid range against existing end time.</summary>
    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateAsync")]
    public async Task UpdateAsync_OnlyStartTime_ValidRange_ReturnsUpdatedDto()
    {
        // Arrange
        var existing = MakeValidTemplate(); // Start=06:00, End=14:00
        var request = new UpdateShiftTemplateRequest
        {
            DefaultStartTime = new TimeOnly(7, 0) // 07:00 < 14:00 → valid
        };

        var updated = MakeValidTemplate();
        updated.DefaultStartTime = new TimeOnly(7, 0);

        _templateRepoMock
            .SetupSequence(r => r.GetByIdAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing)
            .ReturnsAsync(updated);

        var svc = CreateService();

        // Act
        var result = await svc.UpdateAsync(1L, request, updatedByStaffId: 200L);

        // Assert
        result.DefaultStartTime.Should().Be(new TimeOnly(7, 0));
        result.DefaultEndTime.Should().Be(new TimeOnly(14, 0)); // unchanged
    }

    /// <summary>UTCID08 — Boundary: rename with leading/trailing spaces → name is trimmed.</summary>
    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateAsync")]
    public async Task UpdateAsync_NameWithSpaces_TrimsName()
    {
        // Arrange
        var existing = MakeValidTemplate();

        _templateRepoMock
            .Setup(r => r.ExistsByNameAsync("Afternoon", 1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var updated = MakeValidTemplate();
        updated.TemplateName = "Afternoon";

        _templateRepoMock
            .SetupSequence(r => r.GetByIdAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing)
            .ReturnsAsync(updated);

        var request = new UpdateShiftTemplateRequest { TemplateName = "  Afternoon  " };
        var svc = CreateService();

        // Act
        var result = await svc.UpdateAsync(1L, request, updatedByStaffId: 200L);

        // Assert
        result.TemplateName.Should().Be("Afternoon");
        // Verify ExistsByNameAsync was called with trimmed name
        _templateRepoMock.Verify(
            r => r.ExistsByNameAsync("Afternoon", 1L, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>UTCID09 — Boundary: new end time equals new start time → ValidationException.</summary>
    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateAsync")]
    public async Task UpdateAsync_NewStartEqualsNewEnd_ThrowsValidationException()
    {
        // Arrange
        var existing = MakeValidTemplate();
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new UpdateShiftTemplateRequest
        {
            DefaultStartTime = new TimeOnly(12, 0),
            DefaultEndTime = new TimeOnly(12, 0)
        };
        var svc = CreateService();

        // Act
        var act = () => svc.UpdateAsync(1L, request, updatedByStaffId: 200L);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*start time must be earlier*");
    }

    #endregion
}
