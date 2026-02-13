using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Dish
{
    public class UpdateDishFormRequest
    {
        public string Dish { get; set; } = string.Empty;

        public List<IFormFile> StaticImages { get; set; } = [];

        public List<IFormFile> Images360 { get; set; } = [];

        public string RemovedMediaIds { get; set; } = "[]";
    }
}
