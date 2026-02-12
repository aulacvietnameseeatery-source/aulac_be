using Core.DTO.Common;
using Core.DTO.Role;
using Core.Entity;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Interface.Service.Role;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;

        public RoleService(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<(List<RoleListItemDto> Items, int TotalCount)>
            GetPagedAsync(PagedQuery query)
        {
            var (roles, totalCount) =
                await _roleRepository.GetPagedWithStaffCountAsync(
                    query.PageIndex,
                    query.PageSize,
                    query.Search
                );

            var items = roles.Select(r => new RoleListItemDto
            {
                RoleId = r.RoleId,
                RoleCode = r.RoleCode,
                RoleName = r.RoleName,
                StaffCount = r.StaffAccounts.Count
            }).ToList();

            return (items, totalCount);
        }

        public async Task<RoleDetailDto> GetRoleDetailAsync(long roleId)
        {
            // Get role with its permissions
            var role = await _roleRepository.GetRoleWithPermissionsAsync(roleId);
            if (role == null)
            {
                throw new KeyNotFoundException($"Role with ID {roleId} not found.");
            }

            // Get all available permissions
            var allPermissions = await _roleRepository.GetAllPermissionsAsync();

            // Get the permissions assigned to this role
            var assignedPermissionIds = role.Permissions.Select(p => p.PermissionId).ToHashSet();

            // Group permissions by screen code
            var permissionGroups = allPermissions
                .GroupBy(p => p.ScreenCode)
                .Select(group => new PermissionGroupDto
                {
                    ScreenCode = group.Key,
                    DisplayName = GetScreenDisplayName(group.Key),
                    Permissions = group.Select(p => new PermissionItemDto
                    {
                        PermissionId = p.PermissionId,
                        ScreenCode = p.ScreenCode,
                        ActionCode = p.ActionCode,
                        DisplayName = GetActionDisplayName(p.ActionCode),
                        IsAssigned = assignedPermissionIds.Contains(p.PermissionId)
                    })
                    .OrderBy(p => GetActionOrder(p.ActionCode))
                    .ToList()
                })
                .OrderBy(g => GetScreenOrder(g.ScreenCode))
                .ToList();

            // Determine if role is active
            var isActive = role.RoleStatusLv != null && 
                           role.RoleStatusLv.ValueCode == RoleStatusCode.ACTIVE.ToString();

            return new RoleDetailDto
            {
                RoleId = role.RoleId,
                RoleCode = role.RoleCode,
                RoleName = role.RoleName,
                RoleStatusLvId = role.RoleStatusLvId,
                IsActive = isActive,
                PermissionGroups = permissionGroups
            };
        }

        public async Task DeleteRoleAsync(long roleId)
        {
            var role = await _roleRepository.FindByIdAsync(roleId);
            if (role == null)
            {
                throw new KeyNotFoundException($"Role with ID {roleId} not found.");
            }

            // Check if staff are assigned using optimized query
            if (await _roleRepository.HasStaffAssignedAsync(roleId))
            {
                throw new InvalidOperationException("Cannot delete role that has assigned staff accounts.");
            }

            // Soft delete: Update status to INACTIVE
            // Look up the "INACTIVE" status value since IDs are dynamic
            var inactiveStatus = await _roleRepository.GetRoleStatusIdAsync(RoleStatusCode.INACTIVE.ToString());
            if (inactiveStatus == null)
            {
                 // Fallback or error if status configuration is missing. 
                 // Ideally this shouldn't happen if SQL script ran correctly.
                 // For safety, throwing exception or logging.
                 throw new InvalidOperationException("Role Status 'INACTIVE' not found config in database.");
            }

            role.RoleStatusLvId = inactiveStatus.Value;
            await _roleRepository.UpdateAsync(role);
        }

        public async Task<RoleDetailDto> CreateRoleAsync(CreateRoleRequestDto request)
        {
            // Check if role code already exists
            if (await _roleRepository.RoleCodeExistsAsync(request.RoleCode))
            {
                throw new ConflictException($"A role with code '{request.RoleCode}' already exists. Please use a different role code.");
            }

            // Get the appropriate status ID based on IsActive flag
            var statusCode = request.IsActive ? RoleStatusCode.ACTIVE.ToString() : RoleStatusCode.INACTIVE.ToString();
            var roleStatusId = await _roleRepository.GetRoleStatusIdAsync(statusCode);
            if (roleStatusId == null)
            {
                throw new InvalidOperationException($"Role Status '{statusCode}' not found in database configuration.");
            }

            // Query requested permissions directly and validate
            var requestedPermissionIds = request.PermissionIds.Distinct().ToList();
            var validPermissions = await _roleRepository.GetPermissionsByIdsAsync(requestedPermissionIds);
            var validPermissionIds = validPermissions.Select(p => p.PermissionId).ToList();

            // Strict validation: throw if any requested permission ID doesn't exist
            if (validPermissionIds.Count != requestedPermissionIds.Count)
            {
                var invalidIds = requestedPermissionIds.Except(validPermissionIds);
                throw new InvalidOperationException($"Invalid permission IDs: {string.Join(", ", invalidIds)}");
            }

            // Create the new role entity
            var newRole = new Role
            {
                RoleCode = request.RoleCode,
                RoleName = request.RoleName,
                RoleStatusLvId = roleStatusId.Value,
                Permissions = validPermissions.ToList()
            };

            // Save to database
            var createdRole = await _roleRepository.AddAsync(newRole);

            // Return the created role as RoleDetailDto
            return await GetRoleDetailAsync(createdRole.RoleId);
        }

        public async Task<RoleDetailDto> UpdateRoleAsync(long roleId, UpdateRoleRequestDto request)
        {
            // Check if role exists - use tracked version for update
            var existingRole = await _roleRepository.GetRoleWithPermissionsForUpdateAsync(roleId);
            if (existingRole == null)
            {
                throw new KeyNotFoundException($"Role with ID {roleId} not found.");
            }

            // Check if role code already exists for another role
            var roleWithSameCode = await _roleRepository.FindByCodeAsync(request.RoleCode);
            if (roleWithSameCode != null && roleWithSameCode.RoleId != roleId)
            {
                throw new ConflictException($"A role with code '{request.RoleCode}' already exists. Please use a different role code.");
            }

            // Get the appropriate status ID based on IsActive flag
            var statusCode = request.IsActive ? RoleStatusCode.ACTIVE.ToString() : RoleStatusCode.INACTIVE.ToString();
            var roleStatusId = await _roleRepository.GetRoleStatusIdAsync(statusCode);
            if (roleStatusId == null)
            {
                throw new InvalidOperationException($"Role Status '{statusCode}' not found in database configuration.");
            }

            // Query requested permissions directly and validate
            var requestedPermissionIds = request.PermissionIds.Distinct().ToList();
            var permissionsToAdd = await _roleRepository.GetPermissionsByIdsAsync(requestedPermissionIds);
            var validPermissionIds = permissionsToAdd.Select(p => p.PermissionId).ToList();

            // Strict validation: throw if any requested permission ID doesn't exist
            if (validPermissionIds.Count != requestedPermissionIds.Count)
            {
                var invalidIds = requestedPermissionIds.Except(validPermissionIds);
                throw new InvalidOperationException($"Invalid permission IDs: {string.Join(", ", invalidIds)}");
            }

            // Update role properties
            existingRole.RoleCode = request.RoleCode;
            existingRole.RoleName = request.RoleName;
            existingRole.RoleStatusLvId = roleStatusId.Value;

            // Differential update: only add/remove what's necessary
            var currentIds = existingRole.Permissions.Select(p => p.PermissionId).ToHashSet();
            var requestedIds = validPermissionIds.ToHashSet();

            var toRemove = currentIds.Except(requestedIds).ToHashSet();
            var toAdd = requestedIds.Except(currentIds).ToHashSet();

            // Remove permissions that are no longer needed
            if (toRemove.Any())
            {
                var permissionsToRemove = existingRole.Permissions
                    .Where(p => toRemove.Contains(p.PermissionId))
                    .ToList();
                
                foreach (var permission in permissionsToRemove)
                {
                    existingRole.Permissions.Remove(permission);
                }
            }

            // Add new permissions
            if (toAdd.Any())
            {
                var newPermissions = permissionsToAdd
                    .Where(p => toAdd.Contains(p.PermissionId))
                    .ToList();
                
                foreach (var permission in newPermissions)
                {
                    existingRole.Permissions.Add(permission);
                }
            }

            // Save changes to database
            await _roleRepository.UpdateAsync(existingRole);

            // Return the updated role as RoleDetailDto
            return await GetRoleDetailAsync(roleId);
        }

        #region Helper Methods

        private static string GetScreenDisplayName(string screenCode)
        {
            return screenCode switch
            {
                "ROLE" => "Role Management",
                "ACCOUNT" => "Staff Management",
                "DISH" => "Dish Management",
                "PROMOTION" => "Promotion Management",
                "SYSTEM_SETTING" => "System Settings",
                _ => screenCode
            };
        }

        private static string GetActionDisplayName(string actionCode)
        {
            return actionCode switch
            {
                "CREATE" => "Create",
                "READ" => "View",
                "EDIT" => "Edit",
                "UPDATE" => "Edit",
                "DELETE" => "Delete",
                "RESET_PASSWORD" => "Reset Password",
                "ACTIVATE_DEACTIVATE" => "Activate/Deactivate",
                _ => actionCode
            };
        }

        private static int GetScreenOrder(string screenCode)
        {
            return screenCode switch
            {
                "ROLE" => 1,
                "ACCOUNT" => 2,
                "DISH" => 3,
                "PROMOTION" => 4,
                "SYSTEM_SETTING" => 5,
                _ => 999
            };
        }

        private static int GetActionOrder(string actionCode)
        {
            return actionCode switch
            {
                "CREATE" => 1,
                "READ" => 2,
                "EDIT" => 3,
                "UPDATE" => 3,
                "DELETE" => 4,
                "RESET_PASSWORD" => 5,
                "ACTIVATE_DEACTIVATE" => 6,
                _ => 999
            };
        }

        #endregion
    }
}
