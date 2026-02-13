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
        /// The name of the role to create.
        /// Role code will be auto-generated from this name (uppercase with underscores).
        /// </summary>
        [Required(ErrorMessage = "Role name is required.")]
        [StringLength(100, ErrorMessage = "Role name must not exceed 100 characters.")]
        public string RoleName { get; set; } = null!;

        /// <summary>
        /// Indicates whether the role should be active upon creation.
        /// Default is true (always active on creation).
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// List of permission IDs to assign to the role.
        /// </summary>
        public List<long> PermissionIds { get; set; } = new List<long>();
    }
}
