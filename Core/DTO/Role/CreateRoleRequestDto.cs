using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Role
{
    public class CreateRoleRequestDto
    {
        /// <summary>
        /// The unique code for the role (e.g., "MANAGER", "STAFF").
        /// </summary>
        [Required(ErrorMessage = "Role code is required.")]
        [StringLength(50, ErrorMessage = "Role code must not exceed 50 characters.")]
        public string RoleCode { get; set; } = null!;

        /// <summary>
        /// The name of the role to create.
        /// </summary>
        [Required(ErrorMessage = "Role name is required.")]
        [StringLength(100, ErrorMessage = "Role name must not exceed 100 characters.")]
        public string RoleName { get; set; } = null!;

        /// <summary>
        /// Indicates whether the role should be active upon creation.
        /// </summary>
        [Required]
        public bool IsActive { get; set; }

        /// <summary>
        /// List of permission IDs to assign to the role.
        /// </summary>
        public List<long> PermissionIds { get; set; } = new List<long>();
    }
}
