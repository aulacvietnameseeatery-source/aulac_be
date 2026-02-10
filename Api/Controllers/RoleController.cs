using API.Models;
using Core.Attribute;
using Core.Data;
using Core.DTO.Common;
using Core.DTO.Role;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Role;
using Microsoft.AspNetCore.Mvc;

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
        //[HasPermission(Permissions.ViewRole)]
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
    }
}
