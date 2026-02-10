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

            await _roleRepository.DeleteAsync(role);
        }
    }
}
