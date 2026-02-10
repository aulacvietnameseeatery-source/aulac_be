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
    }
}
