using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Role
{
    public class ArchiveRoleRequestDto
    {
        /// <summary>
        /// Active replacement role that should receive existing staff accounts before this role is archived.
        /// Required only when the role still has assigned staff accounts.
        /// </summary>
        [Range(1, long.MaxValue, ErrorMessage = "Replacement role ID must be greater than 0.")]
        public long? ReplacementRoleId { get; set; }
    }
}