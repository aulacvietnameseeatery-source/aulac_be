using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Ingredient
{
    /// <summary>
    /// Simple ingredient DTO for dropdowns and lists
    /// </summary>
    public class IngredientSimpleDto
    {
        public long IngredientId { get; set; }
        public string IngredientName { get; set; } = null!;
        public string UnitLvId { get; set; } = null!;
        public string? UnitName { get; set; }
    }
}
