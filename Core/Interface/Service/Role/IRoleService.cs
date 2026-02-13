using Core.DTO.Common;
using Core.DTO.Role;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Role
{
    public interface IRoleService
    {
        /// <summary>
        /// Retrieves a paged list of roles based on the specified query parameters.
        /// </summary>
        /// <param name="query">The paging and search criteria.</param>
        /// <returns>
        /// A tuple containing the list of role items and the total count of roles matching the query.
        /// </returns>
        Task<(List<RoleListItemDto> Items, int TotalCount)> GetPagedAsync(PagedQuery query);

        /// <summary>
        /// Deletes a role.
        /// </summary>
        /// <param name="roleId">The role ID</param>
        /// <returns>Task</returns>
        Task DeleteRoleAsync(long roleId);

        /// <summary>
        /// Gets detailed information about a role including all permissions.
        /// </summary>
        /// <param name="roleId">The role ID</param>
        /// <returns>Role detail with permissions grouped by screen</returns>
        /// <exception cref="KeyNotFoundException">Thrown when role is not found</exception>
        Task<RoleDetailDto> GetRoleDetailAsync(long roleId);

        /// <summary>
        /// Creates a new role with the specified permissions.
        /// </summary>
        /// <param name="request">The create role request containing role name, status, and permissions</param>
        /// <returns>The created role detail</returns>
        /// <exception cref="InvalidOperationException">Thrown when role code already exists</exception>
        Task<RoleDetailDto> CreateRoleAsync(CreateRoleRequestDto request);

        /// <summary>
        /// Updates an existing role with the specified permissions.
        /// </summary>
        /// <param name="roleId">The role ID to update</param>
        /// <param name="request">The update role request containing role name, status, and permissions</param>
        /// <returns>The updated role detail</returns>
        /// <exception cref="KeyNotFoundException">Thrown when role is not found</exception>
        /// <exception cref="ConflictException">Thrown when role code already exists for another role</exception>
        Task<RoleDetailDto> UpdateRoleAsync(long roleId, UpdateRoleRequestDto request);
    }
}
