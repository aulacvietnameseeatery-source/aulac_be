using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dish
{
    public class GetDishesRequest
    {
        // default paging values
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // filtering and searching
        public string? Search { get; set; }     
        public string? Category { get; set; }
        public DishStatusCode? Status { get; set; }

        // sorting
        public string? SortBy { get; set; }     
        public bool IsDescending { get; set; } = false;

        // view context
        public bool IsCustomerView { get; set; } = false;
    }
}
