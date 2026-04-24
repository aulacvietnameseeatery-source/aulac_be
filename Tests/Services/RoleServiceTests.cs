using Core.DTO.Common;
using Core.DTO.Role;
using Core.Entity;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Service;
using Moq;
using FluentAssertions;

namespace Tests.Services;

/// <summary>
/// Unit Test — RoleService
/// Code Module : Core/Service/RoleService.cs
/// Methods     : GetPagedAsync, GetRoleDetailAsync, DeleteRoleAsync, CreateRoleAsync, UpdateRoleAsync
/// Created By  : AI Agent
/// Executed By : Tester
/// Test Req.   : Validate role CRUD operations including paging, detail retrieval,
///               soft-delete, creation with permissions, and update with differential permission sync.
/// </summary>
public class RoleServiceTests
{
    // ── Mocks ──────────────────────────────────────────────────────────────
    private readonly Mock<IRoleRepository> _roleRepoMock = new();

    // ── Factory ────────────────────────────────────────────────────────────
    private RoleService CreateService() => new(_roleRepoMock.Object);

    // ── Constants ──────────────────────────────────────────────────────────
    private const uint ActiveStatusId = 10u;
    private const uint InactiveStatusId = 11u;

    // ── Test Data Helpers ──────────────────────────────────────────────────

    private static Role MakeRole(long id = 1, string code = "MANAGER", string name = "Manager", uint statusLvId = 10u)
        => new()
        {
            RoleId = id,
            RoleCode = code,
            RoleName = name,
            RoleStatusLvId = statusLvId,
            RoleStatusLv = new LookupValue
            {
                ValueId = statusLvId,
                ValueCode = statusLvId == 10u ? "ACTIVE" : "INACTIVE",
                ValueName = statusLvId == 10u ? "Active" : "Inactive",
                TypeId = 1,
                SortOrder = 1
            },
            StaffAccounts = new List<StaffAccount>(),
            Permissions = new List<Permission>
            {
                new() { PermissionId = 1, ScreenCode = "ROLE", ActionCode = "READ" },
                new() { PermissionId = 2, ScreenCode = "ROLE", ActionCode = "CREATE" }
            }
        };

    private static Permission MakePermission(long id, string screen, string action)
        => new() { PermissionId = id, ScreenCode = screen, ActionCode = action };

    private static List<Permission> MakeAllPermissions() => new()
    {
        MakePermission(1, "ROLE", "READ"),
        MakePermission(2, "ROLE", "CREATE"),
        MakePermission(3, "ROLE", "EDIT"),
        MakePermission(4, "ROLE", "DELETE"),
        MakePermission(5, "ACCOUNT", "READ"),
        MakePermission(6, "ACCOUNT", "CREATE")
    };

    private static CreateRoleRequestDto MakeCreateRequest(string name = "Floor Staff", List<long>? permIds = null)
        => new()
        {
            RoleName = name,
            IsActive = true,
            PermissionIds = permIds ?? new List<long> { 1, 2 }
        };

    private static UpdateRoleRequestDto MakeUpdateRequest(string name = "Updated Role", bool isActive = true, List<long>? permIds = null)
        => new()
        {
            RoleName = name,
            IsActive = isActive,
            PermissionIds = permIds ?? new List<long> { 1, 3 }
        };

    // ══════════════════════════════════════════════════════════════════════
    //  GetPagedAsync
    // ══════════════════════════════════════════════════════════════════════

    #region GetPagedAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetPagedAsync")]
    public async Task GetPagedAsync_WhenRolesExist_ReturnsPagedItems()
    {
        // Arrange
        var roles = new List<Role>
        {
            MakeRole(1, "ADMIN", "Admin"),
            MakeRole(2, "MANAGER", "Manager")
        };
        _roleRepoMock
            .Setup(r => r.GetPagedWithStaffCountAsync(1, 20, null))
            .ReturnsAsync((roles, 2));

        var service = CreateService();
        var query = new PagedQuery { PageIndex = 1, PageSize = 20 };

        // Act
        var (items, totalCount) = await service.GetPagedAsync(query);

        // Assert
        totalCount.Should().Be(2);
        items.Should().HaveCount(2);
        items[0].RoleId.Should().Be(1);
        items[0].RoleCode.Should().Be("ADMIN");
        items[0].RoleName.Should().Be("Admin");
        items[1].RoleId.Should().Be(2);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetPagedAsync")]
    public async Task GetPagedAsync_WhenSearchProvided_PassesSearchToRepo()
    {
        // Arrange
        var roles = new List<Role> { MakeRole(1, "ADMIN", "Admin") };
        _roleRepoMock
            .Setup(r => r.GetPagedWithStaffCountAsync(1, 10, "admin"))
            .ReturnsAsync((roles, 1));

        var service = CreateService();
        var query = new PagedQuery { PageIndex = 1, PageSize = 10, Search = "admin" };

        // Act
        var (items, totalCount) = await service.GetPagedAsync(query);

        // Assert
        totalCount.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].RoleName.Should().Be("Admin");
        _roleRepoMock.Verify(r => r.GetPagedWithStaffCountAsync(1, 10, "admin"), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetPagedAsync")]
    public async Task GetPagedAsync_WhenNoRolesExist_ReturnsEmptyList()
    {
        // Arrange
        _roleRepoMock
            .Setup(r => r.GetPagedWithStaffCountAsync(1, 20, null))
            .ReturnsAsync((new List<Role>(), 0));

        var service = CreateService();
        var query = new PagedQuery { PageIndex = 1, PageSize = 20 };

        // Act
        var (items, totalCount) = await service.GetPagedAsync(query);

        // Assert
        totalCount.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetPagedAsync")]
    public async Task GetPagedAsync_WhenSearchIsEmptyString_PassesEmptyStringToRepo()
    {
        // Arrange
        var roles = new List<Role> { MakeRole(1, "ADMIN", "Admin") };
        _roleRepoMock
            .Setup(r => r.GetPagedWithStaffCountAsync(1, 20, ""))
            .ReturnsAsync((roles, 1));

        var service = CreateService();
        var query = new PagedQuery { PageIndex = 1, PageSize = 20, Search = "" };

        // Act
        var (items, totalCount) = await service.GetPagedAsync(query);

        // Assert
        totalCount.Should().Be(1);
        items.Should().HaveCount(1);
        _roleRepoMock.Verify(r => r.GetPagedWithStaffCountAsync(1, 20, ""), Times.Once);
    }


    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  GetRoleDetailAsync
    // ══════════════════════════════════════════════════════════════════════

    #region GetRoleDetailAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetRoleDetailAsync")]
    public async Task GetRoleDetailAsync_WhenRoleExists_ReturnsDetailWithPermissions()
    {
        // Arrange
        var role = MakeRole(1);
        var allPermissions = MakeAllPermissions();

        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsAsync(1, default)).ReturnsAsync(role);
        _roleRepoMock.Setup(r => r.GetAllPermissionsAsync(default)).ReturnsAsync(allPermissions);

        var service = CreateService();

        // Act
        var result = await service.GetRoleDetailAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.RoleId.Should().Be(1);
        result.RoleCode.Should().Be("MANAGER");
        result.RoleName.Should().Be("Manager");
        result.IsActive.Should().BeTrue();
        result.PermissionGroups.Should().NotBeEmpty();
    }

 
    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetRoleDetailAsync")]
    public async Task GetRoleDetailAsync_WhenRoleNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsAsync(999, default)).ReturnsAsync((Role?)null);

        var service = CreateService();

        // Act
        var act = () => service.GetRoleDetailAsync(999);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetRoleDetailAsync")]
    public async Task GetRoleDetailAsync_WhenRoleHasNoPermissions_ReturnsEmptyAssignments()
    {
        // Arrange
        var role = MakeRole(1);
        role.Permissions = new List<Permission>();
        var allPermissions = MakeAllPermissions();

        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsAsync(1, default)).ReturnsAsync(role);
        _roleRepoMock.Setup(r => r.GetAllPermissionsAsync(default)).ReturnsAsync(allPermissions);

        var service = CreateService();

        // Act
        var result = await service.GetRoleDetailAsync(1);

        // Assert
        result.PermissionGroups.SelectMany(g => g.Permissions)
            .Should().OnlyContain(p => p.IsAssigned == false);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetRoleDetailAsync")]
    public async Task GetRoleDetailAsync_WhenRoleIsInactive_ReturnsIsActiveFalse()
    {
        // Arrange
        var role = MakeRole(1, "MANAGER", "Manager", InactiveStatusId);
        var allPermissions = MakeAllPermissions();

        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsAsync(1, default)).ReturnsAsync(role);
        _roleRepoMock.Setup(r => r.GetAllPermissionsAsync(default)).ReturnsAsync(allPermissions);

        var service = CreateService();

        // Act
        var result = await service.GetRoleDetailAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeFalse();
        result.RoleId.Should().Be(1);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetRoleDetailAsync")]
    public async Task GetRoleDetailAsync_WhenRoleHasAllPermissions_ReturnsAllAssigned()
    {
        // Arrange
        var allPermissions = MakeAllPermissions();
        var role = MakeRole(1);
        role.Permissions = new List<Permission>(allPermissions);

        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsAsync(1, default)).ReturnsAsync(role);
        _roleRepoMock.Setup(r => r.GetAllPermissionsAsync(default)).ReturnsAsync(allPermissions);

        var service = CreateService();

        // Act
        var result = await service.GetRoleDetailAsync(1);

        // Assert
        result.PermissionGroups.SelectMany(g => g.Permissions)
            .Should().OnlyContain(p => p.IsAssigned == true);
    }


    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  DeleteRoleAsync
    // ══════════════════════════════════════════════════════════════════════

    #region DeleteRoleAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "DeleteRoleAsync")]
    public async Task DeleteRoleAsync_WhenRoleExistsAndNoStaff_SoftDeletesSuccessfully()
    {
        // Arrange
        var role = MakeRole(1);
        _roleRepoMock.Setup(r => r.FindByIdAsync(1, default)).ReturnsAsync(role);
        _roleRepoMock.Setup(r => r.HasStaffAssignedAsync(1, default)).ReturnsAsync(false);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("INACTIVE")).ReturnsAsync(InactiveStatusId);
        _roleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.DeleteRoleAsync(1);

        // Assert
        role.RoleStatusLvId.Should().Be(InactiveStatusId);
        _roleRepoMock.Verify(r => r.UpdateAsync(role), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeleteRoleAsync")]
    public async Task DeleteRoleAsync_WhenRoleNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _roleRepoMock.Setup(r => r.FindByIdAsync(999, default)).ReturnsAsync((Role?)null);

        var service = CreateService();

        // Act
        var act = () => service.DeleteRoleAsync(999);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeleteRoleAsync")]
    public async Task DeleteRoleAsync_WhenStaffAssigned_ThrowsInvalidOperationException()
    {
        // Arrange
        var role = MakeRole(1);
        _roleRepoMock.Setup(r => r.FindByIdAsync(1, default)).ReturnsAsync(role);
        _roleRepoMock.Setup(r => r.HasStaffAssignedAsync(1, default)).ReturnsAsync(true);

        var service = CreateService();

        // Act
        var act = () => service.DeleteRoleAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*assigned staff*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeleteRoleAsync")]
    public async Task DeleteRoleAsync_WhenInactiveStatusMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var role = MakeRole(1);
        _roleRepoMock.Setup(r => r.FindByIdAsync(1, default)).ReturnsAsync(role);
        _roleRepoMock.Setup(r => r.HasStaffAssignedAsync(1, default)).ReturnsAsync(false);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("INACTIVE")).ReturnsAsync((uint?)null);

        var service = CreateService();

        // Act
        var act = () => service.DeleteRoleAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*INACTIVE*not found*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  CreateRoleAsync
    // ══════════════════════════════════════════════════════════════════════

    #region CreateRoleAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateRoleAsync")]
    public async Task CreateRoleAsync_WhenValidRequest_CreatesRoleSuccessfully()
    {
        // Arrange
        var request = MakeCreateRequest("Floor Staff", new List<long> { 1, 2 });
        var permissions = new List<Permission>
        {
            MakePermission(1, "ROLE", "READ"),
            MakePermission(2, "ROLE", "CREATE")
        };
        var createdRole = MakeRole(10, "FLOOR_STAFF", "Floor Staff");

        _roleRepoMock.Setup(r => r.RoleCodeExistsAsync("FLOOR_STAFF", default)).ReturnsAsync(false);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("ACTIVE")).ReturnsAsync(ActiveStatusId);
        _roleRepoMock.Setup(r => r.GetPermissionsByIdsAsync(It.IsAny<List<long>>(), default)).ReturnsAsync(permissions);
        _roleRepoMock.Setup(r => r.AddAsync(It.IsAny<Role>(), default)).ReturnsAsync(createdRole);
        // Setup for GetRoleDetailAsync called internally
        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsAsync(10, default)).ReturnsAsync(createdRole);
        _roleRepoMock.Setup(r => r.GetAllPermissionsAsync(default)).ReturnsAsync(MakeAllPermissions());

        var service = CreateService();

        // Act
        var result = await service.CreateRoleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.RoleCode.Should().Be("FLOOR_STAFF");
        _roleRepoMock.Verify(r => r.AddAsync(It.Is<Role>(role =>
            role.RoleCode == "FLOOR_STAFF" &&
            role.RoleName == "Floor Staff" &&
            role.RoleStatusLvId == ActiveStatusId &&
            role.Permissions.Count == 2
        ), default), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateRoleAsync")]
    public async Task CreateRoleAsync_GeneratesCodeFromName_UppercaseWithUnderscores()
    {
        // Arrange
        var request = MakeCreateRequest("  Head Chef  ", new List<long> { 1 });
        var permissions = new List<Permission> { MakePermission(1, "ROLE", "READ") };
        var createdRole = MakeRole(10, "HEAD_CHEF", "Head Chef");

        _roleRepoMock.Setup(r => r.RoleCodeExistsAsync("HEAD_CHEF", default)).ReturnsAsync(false);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("ACTIVE")).ReturnsAsync(ActiveStatusId);
        _roleRepoMock.Setup(r => r.GetPermissionsByIdsAsync(It.IsAny<List<long>>(), default)).ReturnsAsync(permissions);
        _roleRepoMock.Setup(r => r.AddAsync(It.IsAny<Role>(), default)).ReturnsAsync(createdRole);
        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsAsync(10, default)).ReturnsAsync(createdRole);
        _roleRepoMock.Setup(r => r.GetAllPermissionsAsync(default)).ReturnsAsync(MakeAllPermissions());

        var service = CreateService();

        // Act
        await service.CreateRoleAsync(request);

        // Assert
        _roleRepoMock.Verify(r => r.AddAsync(It.Is<Role>(role =>
            role.RoleCode == "HEAD_CHEF" && role.RoleName == "Head Chef"
        ), default), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateRoleAsync")]
    public async Task CreateRoleAsync_WhenRoleCodeAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        var request = MakeCreateRequest("Manager");
        _roleRepoMock.Setup(r => r.RoleCodeExistsAsync("MANAGER", default)).ReturnsAsync(true);

        var service = CreateService();

        // Act
        var act = () => service.CreateRoleAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*MANAGER*already exists*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateRoleAsync")]
    public async Task CreateRoleAsync_WhenActiveStatusMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = MakeCreateRequest();
        _roleRepoMock.Setup(r => r.RoleCodeExistsAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("ACTIVE")).ReturnsAsync((uint?)null);

        var service = CreateService();

        // Act
        var act = () => service.CreateRoleAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ACTIVE*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateRoleAsync")]
    public async Task CreateRoleAsync_WhenInvalidPermissionIds_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = MakeCreateRequest("New Role", new List<long> { 1, 2, 999 });
        var validPermissions = new List<Permission>
        {
            MakePermission(1, "ROLE", "READ"),
            MakePermission(2, "ROLE", "CREATE")
        };

        _roleRepoMock.Setup(r => r.RoleCodeExistsAsync("NEW_ROLE", default)).ReturnsAsync(false);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("ACTIVE")).ReturnsAsync(ActiveStatusId);
        _roleRepoMock.Setup(r => r.GetPermissionsByIdsAsync(It.IsAny<List<long>>(), default)).ReturnsAsync(validPermissions);

        var service = CreateService();

        // Act
        var act = () => service.CreateRoleAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid permission IDs*999*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateRoleAsync")]
    public async Task CreateRoleAsync_WhenDuplicatePermissionIds_DeduplicatesBeforeValidation()
    {
        // Arrange
        var request = MakeCreateRequest("New Role", new List<long> { 1, 1, 2, 2 });
        var permissions = new List<Permission>
        {
            MakePermission(1, "ROLE", "READ"),
            MakePermission(2, "ROLE", "CREATE")
        };
        var createdRole = MakeRole(10, "NEW_ROLE", "New Role");

        _roleRepoMock.Setup(r => r.RoleCodeExistsAsync("NEW_ROLE", default)).ReturnsAsync(false);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("ACTIVE")).ReturnsAsync(ActiveStatusId);
        _roleRepoMock.Setup(r => r.GetPermissionsByIdsAsync(
            It.Is<List<long>>(ids => ids.Count == 2), default)).ReturnsAsync(permissions);
        _roleRepoMock.Setup(r => r.AddAsync(It.IsAny<Role>(), default)).ReturnsAsync(createdRole);
        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsAsync(10, default)).ReturnsAsync(createdRole);
        _roleRepoMock.Setup(r => r.GetAllPermissionsAsync(default)).ReturnsAsync(MakeAllPermissions());

        var service = CreateService();

        // Act
        var result = await service.CreateRoleAsync(request);

        // Assert
        result.Should().NotBeNull();
        _roleRepoMock.Verify(r => r.GetPermissionsByIdsAsync(
            It.Is<List<long>>(ids => ids.Count == 2), default), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateRoleAsync")]
    public async Task CreateRoleAsync_WhenEmptyPermissionIds_CreatesRoleWithNoPermissions()
    {
        // Arrange
        var request = MakeCreateRequest("New Role", new List<long>());
        var createdRole = MakeRole(10, "NEW_ROLE", "New Role");
        createdRole.Permissions = new List<Permission>();

        _roleRepoMock.Setup(r => r.RoleCodeExistsAsync("NEW_ROLE", default)).ReturnsAsync(false);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("ACTIVE")).ReturnsAsync(ActiveStatusId);
        _roleRepoMock.Setup(r => r.GetPermissionsByIdsAsync(
            It.Is<List<long>>(ids => ids.Count == 0), default)).ReturnsAsync(new List<Permission>());
        _roleRepoMock.Setup(r => r.AddAsync(It.IsAny<Role>(), default)).ReturnsAsync(createdRole);
        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsAsync(10, default)).ReturnsAsync(createdRole);
        _roleRepoMock.Setup(r => r.GetAllPermissionsAsync(default)).ReturnsAsync(MakeAllPermissions());

        var service = CreateService();

        // Act
        var result = await service.CreateRoleAsync(request);

        // Assert
        result.Should().NotBeNull();
        _roleRepoMock.Verify(r => r.AddAsync(It.Is<Role>(role =>
            role.Permissions.Count == 0
        ), default), Times.Once);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  UpdateRoleAsync
    // ══════════════════════════════════════════════════════════════════════

    #region UpdateRoleAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateRoleAsync")]
    public async Task UpdateRoleAsync_WhenValidRequest_UpdatesRoleSuccessfully()
    {
        // Arrange
        var existingRole = MakeRole(1, "MANAGER", "Manager");
        var request = MakeUpdateRequest("Senior Manager", true, new List<long> { 1, 3 });
        var newPermissions = new List<Permission>
        {
            MakePermission(1, "ROLE", "READ"),
            MakePermission(3, "ROLE", "EDIT")
        };
        var updatedRole = MakeRole(1, "SENIOR_MANAGER", "Senior Manager");

        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsForUpdateAsync(1, default)).ReturnsAsync(existingRole);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("ACTIVE")).ReturnsAsync(ActiveStatusId);
        _roleRepoMock.Setup(r => r.RoleCodeExistsAsync("SENIOR_MANAGER", default)).ReturnsAsync(false);
        _roleRepoMock.Setup(r => r.GetPermissionsByIdsAsync(It.IsAny<List<long>>(), default)).ReturnsAsync(newPermissions);
        _roleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);
        // Setup for internal GetRoleDetailAsync call
        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsAsync(1, default)).ReturnsAsync(updatedRole);
        _roleRepoMock.Setup(r => r.GetAllPermissionsAsync(default)).ReturnsAsync(MakeAllPermissions());

        var service = CreateService();

        // Act
        var result = await service.UpdateRoleAsync(1, request);

        // Assert
        result.Should().NotBeNull();
        existingRole.RoleCode.Should().Be("SENIOR_MANAGER");
        existingRole.RoleName.Should().Be("Senior Manager");
        _roleRepoMock.Verify(r => r.UpdateAsync(existingRole), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateRoleAsync")]
    public async Task UpdateRoleAsync_WhenDeactivating_SetsInactiveStatus()
    {
        // Arrange
        var existingRole = MakeRole(1, "MANAGER", "Manager");
        var request = MakeUpdateRequest("Manager", false, new List<long> { 1, 2 });
        var permissions = new List<Permission>
        {
            MakePermission(1, "ROLE", "READ"),
            MakePermission(2, "ROLE", "CREATE")
        };

        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsForUpdateAsync(1, default)).ReturnsAsync(existingRole);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("INACTIVE")).ReturnsAsync(InactiveStatusId);
        _roleRepoMock.Setup(r => r.GetPermissionsByIdsAsync(It.IsAny<List<long>>(), default)).ReturnsAsync(permissions);
        _roleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);
        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsAsync(1, default)).ReturnsAsync(existingRole);
        _roleRepoMock.Setup(r => r.GetAllPermissionsAsync(default)).ReturnsAsync(MakeAllPermissions());

        var service = CreateService();

        // Act
        await service.UpdateRoleAsync(1, request);

        // Assert
        existingRole.RoleStatusLvId.Should().Be(InactiveStatusId);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateRoleAsync")]
    public async Task UpdateRoleAsync_WhenSameRoleName_SkipsCodeConflictCheck()
    {
        // Arrange
        var existingRole = MakeRole(1, "MANAGER", "Manager");
        var request = MakeUpdateRequest("Manager", true, new List<long> { 1, 2 });
        var permissions = new List<Permission>
        {
            MakePermission(1, "ROLE", "READ"),
            MakePermission(2, "ROLE", "CREATE")
        };

        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsForUpdateAsync(1, default)).ReturnsAsync(existingRole);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("ACTIVE")).ReturnsAsync(ActiveStatusId);
        _roleRepoMock.Setup(r => r.GetPermissionsByIdsAsync(It.IsAny<List<long>>(), default)).ReturnsAsync(permissions);
        _roleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);
        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsAsync(1, default)).ReturnsAsync(existingRole);
        _roleRepoMock.Setup(r => r.GetAllPermissionsAsync(default)).ReturnsAsync(MakeAllPermissions());

        var service = CreateService();

        // Act
        await service.UpdateRoleAsync(1, request);

        // Assert
        _roleRepoMock.Verify(r => r.RoleCodeExistsAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateRoleAsync")]
    public async Task UpdateRoleAsync_DifferentialPermissionSync_AddsAndRemovesCorrectly()
    {
        // Arrange — existing has perm 1,2; request wants 2,3
        var existingRole = MakeRole(1, "MANAGER", "Manager");
        existingRole.Permissions = new List<Permission>
        {
            MakePermission(1, "ROLE", "READ"),
            MakePermission(2, "ROLE", "CREATE")
        };
        var request = MakeUpdateRequest("Manager", true, new List<long> { 2, 3 });
        var newPermissions = new List<Permission>
        {
            MakePermission(2, "ROLE", "CREATE"),
            MakePermission(3, "ROLE", "EDIT")
        };

        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsForUpdateAsync(1, default)).ReturnsAsync(existingRole);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("ACTIVE")).ReturnsAsync(ActiveStatusId);
        _roleRepoMock.Setup(r => r.GetPermissionsByIdsAsync(It.IsAny<List<long>>(), default)).ReturnsAsync(newPermissions);
        _roleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);
        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsAsync(1, default)).ReturnsAsync(existingRole);
        _roleRepoMock.Setup(r => r.GetAllPermissionsAsync(default)).ReturnsAsync(MakeAllPermissions());

        var service = CreateService();

        // Act
        await service.UpdateRoleAsync(1, request);

        // Assert — perm 1 removed, perm 3 added, perm 2 remains
        existingRole.Permissions.Should().Contain(p => p.PermissionId == 2);
        existingRole.Permissions.Should().Contain(p => p.PermissionId == 3);
        existingRole.Permissions.Should().NotContain(p => p.PermissionId == 1);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateRoleAsync")]
    public async Task UpdateRoleAsync_WhenRoleNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsForUpdateAsync(999, default)).ReturnsAsync((Role?)null);
        var request = MakeUpdateRequest();

        var service = CreateService();

        // Act
        var act = () => service.UpdateRoleAsync(999, request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateRoleAsync")]
    public async Task UpdateRoleAsync_WhenNewCodeConflicts_ThrowsConflictException()
    {
        // Arrange
        var existingRole = MakeRole(1, "MANAGER", "Manager");
        var request = MakeUpdateRequest("Admin", true, new List<long> { 1 });
        var permissions = new List<Permission> { MakePermission(1, "ROLE", "READ") };

        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsForUpdateAsync(1, default)).ReturnsAsync(existingRole);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("ACTIVE")).ReturnsAsync(ActiveStatusId);
        _roleRepoMock.Setup(r => r.GetPermissionsByIdsAsync(It.IsAny<List<long>>(), default)).ReturnsAsync(permissions);
        _roleRepoMock.Setup(r => r.RoleCodeExistsAsync("ADMIN", default)).ReturnsAsync(true);

        var service = CreateService();

        // Act
        var act = () => service.UpdateRoleAsync(1, request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*ADMIN*already exists*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateRoleAsync")]
    public async Task UpdateRoleAsync_WhenStatusConfigMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingRole = MakeRole(1, "MANAGER", "Manager");
        var request = MakeUpdateRequest("Manager", true, new List<long> { 1 });

        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsForUpdateAsync(1, default)).ReturnsAsync(existingRole);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("ACTIVE")).ReturnsAsync((uint?)null);

        var service = CreateService();

        // Act
        var act = () => service.UpdateRoleAsync(1, request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ACTIVE*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateRoleAsync")]
    public async Task UpdateRoleAsync_WhenInvalidPermissionIds_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingRole = MakeRole(1, "MANAGER", "Manager");
        var request = MakeUpdateRequest("Manager", true, new List<long> { 1, 888 });
        var validPermissions = new List<Permission> { MakePermission(1, "ROLE", "READ") };

        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsForUpdateAsync(1, default)).ReturnsAsync(existingRole);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("ACTIVE")).ReturnsAsync(ActiveStatusId);
        _roleRepoMock.Setup(r => r.GetPermissionsByIdsAsync(It.IsAny<List<long>>(), default)).ReturnsAsync(validPermissions);

        var service = CreateService();

        // Act
        var act = () => service.UpdateRoleAsync(1, request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid permission IDs*888*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateRoleAsync")]
    public async Task UpdateRoleAsync_WhenPermissionsUnchanged_NoAddOrRemove()
    {
        // Arrange — existing has perm 1,2; request also wants 1,2
        var existingRole = MakeRole(1, "MANAGER", "Manager");
        existingRole.Permissions = new List<Permission>
        {
            MakePermission(1, "ROLE", "READ"),
            MakePermission(2, "ROLE", "CREATE")
        };
        var request = MakeUpdateRequest("Manager", true, new List<long> { 1, 2 });
        var permissions = new List<Permission>
        {
            MakePermission(1, "ROLE", "READ"),
            MakePermission(2, "ROLE", "CREATE")
        };

        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsForUpdateAsync(1, default)).ReturnsAsync(existingRole);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("ACTIVE")).ReturnsAsync(ActiveStatusId);
        _roleRepoMock.Setup(r => r.GetPermissionsByIdsAsync(It.IsAny<List<long>>(), default)).ReturnsAsync(permissions);
        _roleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);
        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsAsync(1, default)).ReturnsAsync(existingRole);
        _roleRepoMock.Setup(r => r.GetAllPermissionsAsync(default)).ReturnsAsync(MakeAllPermissions());

        var service = CreateService();

        // Act
        await service.UpdateRoleAsync(1, request);

        // Assert — permissions unchanged: still 1 and 2
        existingRole.Permissions.Should().HaveCount(2);
        existingRole.Permissions.Select(p => p.PermissionId).Should().BeEquivalentTo(new[] { 1L, 2L });
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateRoleAsync")]
    public async Task UpdateRoleAsync_WhenDuplicatePermissionIds_DeduplicatesBeforeValidation()
    {
        // Arrange
        var existingRole = MakeRole(1, "MANAGER", "Manager");
        existingRole.Permissions = new List<Permission>
        {
            MakePermission(1, "ROLE", "READ"),
            MakePermission(2, "ROLE", "CREATE")
        };
        var request = MakeUpdateRequest("Manager", true, new List<long> { 1, 1, 2, 2 });
        var permissions = new List<Permission>
        {
            MakePermission(1, "ROLE", "READ"),
            MakePermission(2, "ROLE", "CREATE")
        };

        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsForUpdateAsync(1, default)).ReturnsAsync(existingRole);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("ACTIVE")).ReturnsAsync(ActiveStatusId);
        _roleRepoMock.Setup(r => r.GetPermissionsByIdsAsync(
            It.Is<List<long>>(ids => ids.Count == 2), default)).ReturnsAsync(permissions);
        _roleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);
        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsAsync(1, default)).ReturnsAsync(existingRole);
        _roleRepoMock.Setup(r => r.GetAllPermissionsAsync(default)).ReturnsAsync(MakeAllPermissions());

        var service = CreateService();

        // Act
        await service.UpdateRoleAsync(1, request);

        // Assert — permissions deduplicated: still 1 and 2
        existingRole.Permissions.Should().HaveCount(2);
        _roleRepoMock.Verify(r => r.GetPermissionsByIdsAsync(
            It.Is<List<long>>(ids => ids.Count == 2), default), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateRoleAsync")]
    public async Task UpdateRoleAsync_WhenEmptyPermissionIds_RemovesAllPermissions()
    {
        // Arrange — existing has perm 1,2; request wants empty
        var existingRole = MakeRole(1, "MANAGER", "Manager");
        existingRole.Permissions = new List<Permission>
        {
            MakePermission(1, "ROLE", "READ"),
            MakePermission(2, "ROLE", "CREATE")
        };
        var request = MakeUpdateRequest("Manager", true, new List<long>());

        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsForUpdateAsync(1, default)).ReturnsAsync(existingRole);
        _roleRepoMock.Setup(r => r.GetRoleStatusIdAsync("ACTIVE")).ReturnsAsync(ActiveStatusId);
        _roleRepoMock.Setup(r => r.GetPermissionsByIdsAsync(
            It.Is<List<long>>(ids => ids.Count == 0), default)).ReturnsAsync(new List<Permission>());
        _roleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);
        _roleRepoMock.Setup(r => r.GetRoleWithPermissionsAsync(1, default)).ReturnsAsync(existingRole);
        _roleRepoMock.Setup(r => r.GetAllPermissionsAsync(default)).ReturnsAsync(MakeAllPermissions());

        var service = CreateService();

        // Act
        await service.UpdateRoleAsync(1, request);

        // Assert — all permissions removed
        existingRole.Permissions.Should().BeEmpty();
    }

    #endregion
}
