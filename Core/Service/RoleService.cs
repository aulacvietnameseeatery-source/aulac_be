using Core.DTO.Common;
using Core.DTO.Role;
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
    }
}
