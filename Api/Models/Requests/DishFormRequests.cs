using Microsoft.AspNetCore.Http;

namespace API.Models.Requests;

public class CreateDishFormRequest
{
    public string Dish { get; set; } = string.Empty;
    public List<IFormFile> StaticImages { get; set; } = [];
    public List<IFormFile> Images360 { get; set; } = [];
    public IFormFile? Video { get; set; }
}

public class UpdateDishFormRequest
{
    public string Dish { get; set; } = string.Empty;
    public List<IFormFile> StaticImages { get; set; } = [];
    public List<IFormFile> Images360 { get; set; } = [];
    public IFormFile? Video { get; set; }
    public string RemovedMediaIds { get; set; } = "[]";
}
