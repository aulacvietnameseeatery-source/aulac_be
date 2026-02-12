using API.Models;
using Core.Attribute;
using Core.Data;
using Core.DTO.Common;
using Core.DTO.Role;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Role;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers
{
    [Route("api/roles")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly ILogger<RoleController> _logger;

        public RoleController(
            IRoleService roleService,
            ILogger<RoleController> logger)
        {
            _roleService = roleService;
            _logger = logger;
        }

        /// <summary>
        /// Gets paged list of roles with staff count.
        /// </summary>
        /// <param name="query">Paging and search parameters</param>
        /// <returns>Paged list of roles</returns>
        /// <response code="200">Roles retrieved successfully</response>
        /// <remarks>
        /// **Returned Fields:**
        /// - RoleCode
        /// - RoleName
        /// - StaffCount (number of staff accounts assigned to the role)
        ///
        /// **Search Behavior:**
        /// - Searches by RoleCode or RoleName (case-insensitive)
        ///
        /// **Use Case:**
        /// - Role management screen
        /// - Permission assignment
        /// - Admin configuration
        /// </remarks>
        [HttpGet]
        [HasPermission(Permissions.ViewRole)]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<RoleListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoles([FromQuery] PagedQuery query)
        {
            var (items, totalCount) = await _roleService.GetPagedAsync(query);

            var pagedResult = new PagedResult<RoleListItemDto>
            {
                PageData = items.ToList(),
                PageIndex = query.PageIndex,
                PageSize = query.PageSize,
                TotalCount = totalCount,
                TotalPage = (int)Math.Ceiling(
                    totalCount / (double)query.PageSize)
            };

            _logger.LogInformation(
                "Retrieved role list. PageIndex={PageIndex}, PageSize={PageSize}, Total={TotalCount}",
                query.PageIndex,
                query.PageSize,
                totalCount);

            return Ok(new ApiResponse<PagedResult<RoleListItemDto>>
            {
                Success = true,
                Code = 200,
                UserMessage = "Roles retrieved successfully.",
                Data = pagedResult,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// Gets detailed information about a specific role including permissions.
        /// </summary>
        /// <param name="id">The role ID</param>
        /// <returns>Role detail with permissions grouped by screen</returns>
        /// <response code="200">Role detail retrieved successfully</response>
        /// <response code="404">Role not found</response>
        /// <remarks>
        /// **Returned Fields:**
        /// - RoleId
        /// - RoleCode
        /// - RoleName
        /// - RoleStatusLvId
        /// - IsActive (whether the role status is ACTIVE)
        /// - PermissionGroups (permissions grouped by screen/module)
        ///
        /// **Permission Groups:**
        /// Each group contains:
        /// - ScreenCode (e.g., "ROLE", "ACCOUNT", "DISH")
        /// - DisplayName (user-friendly name like "Role Management")
        /// - DisplayName (user-friendly name like "Role Management")
        /// - Permissions (list of permissions with IsAssigned flag)
        ///
        /// **Use Case:**
        /// - Role detail view screen
        /// - Role editing
        /// - Permission assignment management
        /// </remarks>
        [HttpGet("{id}")]
        [HasPermission(Permissions.ViewRole)]
        [ProducesResponseType(typeof(ApiResponse<RoleDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoleDetail(long id)
        {
            try
            {
                var roleDetail = await _roleService.GetRoleDetailAsync(id);

                _logger.LogInformation("Retrieved role detail. RoleId: {RoleId}", id);

                return Ok(new ApiResponse<RoleDetailDto>
                {
                    Success = true,
                    Code = 200,
                    UserMessage = "Role detail retrieved successfully.",
                    Data = roleDetail,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Get role detail failed: {Message}", ex.Message);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Code = 404,
                    UserMessage = ex.Message,
                    Data = new { },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
        }

        /// <summary>
        /// Creates a new role with specified permissions.
        /// </summary>
        /// <param name="request">The create role request containing role code, role name, status, and permissions</param>
        /// <returns>The created role detail</returns>
        /// <response code="201">Role created successfully</response>
        /// <response code="400">Invalid request (validation error)</response>
        /// <response code="409">Role code already exists</response>
        /// <remarks>
        /// **Request Body:**
        /// - RoleCode: The unique code for the role (required, max 50 characters, e.g., "MANAGER", "STAFF")
        /// - RoleName: The name of the role (required, max 100 characters)
        /// - IsActive: Whether the role should be active (required)
        /// - PermissionIds: List of permission IDs to assign to the role
        ///
        /// **Conflict Check:**
        /// - Role code must be unique
        /// - Returns 409 Conflict if duplicate role code found
        ///
        /// **Response:**
        /// Returns the complete role detail including all permissions grouped by screen.
        ///
        /// **Use Case:**
        /// - Creating new roles from the role management screen
        /// - Setting up initial permissions for a new role
        /// - Admin role configuration
        /// </remarks>
        [HttpPost]
        [HasPermission(Permissions.CreateRole)]
        [ProducesResponseType(typeof(ApiResponse<RoleDetailDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequestDto request)
        {
            try
            {
                var createdRole = await _roleService.CreateRoleAsync(request);

                _logger.LogInformation(
                    "Role created successfully. RoleId: {RoleId}, RoleCode: {RoleCode}, RoleName: {RoleName}",
                    createdRole.RoleId,
                    createdRole.RoleCode,
                    createdRole.RoleName);

                return CreatedAtAction(
                    nameof(GetRoleDetail),
                    new { id = createdRole.RoleId },
                    new ApiResponse<RoleDetailDto>
                    {
                        Success = true,
                        Code = 201,
                        UserMessage = "Role created successfully.",
                        Data = createdRole,
                        ServerTime = DateTimeOffset.UtcNow
                    });
            }
            catch (Core.Exceptions.ConflictException ex)
            {
                _logger.LogWarning("Create role failed - conflict: {Message}", ex.Message);
                return Conflict(new ApiResponse<object>
                {
                    Success = false,
                    Code = 409,
                    UserMessage = ex.Message,
                    Data = new { },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Create role failed: {Message}", ex.Message);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Code = 400,
                    UserMessage = ex.Message,
                    Data = new { },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
        }

        /// <summary>
        /// Updates an existing role with specified permissions.
        /// </summary>
        /// <param name="id">The role ID to update</param>
        /// <param name="request">The update role request containing role code, role name, status, and permissions</param>
        /// <returns>The updated role detail</returns>
        /// <response code="200">Role updated successfully</response>
        /// <response code="400">Invalid request (validation error)</response>
        /// <response code="404">Role not found</response>
        /// <response code="409">Role code already exists for another role</response>
        /// <remarks>
        /// **Request Body:**
        /// - RoleCode: The unique code for the role (required, max 50 characters, e.g., "MANAGER", "STAFF")
        /// - RoleName: The name of the role (required, max 100 characters)
        /// - IsActive: Whether the role should be active (required)
        /// - PermissionIds: List of permission IDs to assign to the role
        ///
        /// **Conflict Check:**
        /// - Role code must be unique across all roles except the current one being updated
        /// - Returns 409 Conflict if duplicate role code found
        ///
        /// **Response:**
        /// Returns the complete role detail including all permissions grouped by screen.
        ///
        /// **Use Case:**
        /// - Editing existing roles from the role management screen
        /// - Updating permissions for a role
        /// - Activating/deactivating roles
        /// - Admin role configuration
        /// </remarks>
        [HttpPut("{id}")]
        [HasPermission(Permissions.UpdateRole)]
        [ProducesResponseType(typeof(ApiResponse<RoleDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateRole(long id, [FromBody] UpdateRoleRequestDto request)
        {
            try
            {
                var updatedRole = await _roleService.UpdateRoleAsync(id, request);

                _logger.LogInformation(
                    "Role updated successfully. RoleId: {RoleId}, RoleCode: {RoleCode}, RoleName: {RoleName}",
                    updatedRole.RoleId,
                    updatedRole.RoleCode,
                    updatedRole.RoleName);

                return Ok(new ApiResponse<RoleDetailDto>
                {
                    Success = true,
                    Code = 200,
                    UserMessage = "Role updated successfully.",
                    Data = updatedRole,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Update role failed - not found: {Message}", ex.Message);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Code = 404,
                    UserMessage = ex.Message,
                    Data = new { },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (Core.Exceptions.ConflictException ex)
            {
                _logger.LogWarning("Update role failed - conflict: {Message}", ex.Message);
                return Conflict(new ApiResponse<object>
                {
                    Success = false,
                    Code = 409,
                    UserMessage = ex.Message,
                    Data = new { },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Update role failed: {Message}", ex.Message);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Code = 400,
                    UserMessage = ex.Message,
                    Data = new { },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
        }

        /// <summary>
        /// Deletes a role by ID.
        /// </summary>
        /// <param name="id">The role ID</param>
        /// <returns>Success confirmation</returns>
        /// <response code="200">Role deleted successfully</response>
        /// <response code="404">Role not found</response>
        /// <response code="400">Role cannot be deleted (e.g., has assigned users)</response>
        [HttpDelete("{id}")]
        [HasPermission(Permissions.DeleteRole)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteRole(long id)
        {
            try
            {
                await _roleService.DeleteRoleAsync(id);

                _logger.LogInformation("Role deleted successfully. ID: {RoleId}", id);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Code = 200,
                    UserMessage = "Role deleted successfully.",
                    Data = new { },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (KeyNotFoundException ex)
            {
                 _logger.LogWarning("Delete role failed: {Message}", ex.Message);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Code = 404,
                    UserMessage = ex.Message,
                    Data = new { },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (InvalidOperationException ex)
            {
                 _logger.LogWarning("Delete role failed: {Message}", ex.Message);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Code = 400,
                    UserMessage = ex.Message,
                    Data = new { },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
        }
    }
}
