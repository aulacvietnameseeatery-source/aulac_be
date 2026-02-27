using API.Models;
using Core.DTO.LookUpValue;
using Core.DTO.Table;
using Core.Interface.Service.Table;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/tables")]
    public class TablesController : ControllerBase
    {
        private readonly ITableService _service;

        public TablesController(ITableService service)
        {
            _service = service;
        }

        //[HttpGet("select")]
        //public async Task<IActionResult> GetTablesForSelect()
        //{
        //    var result = await _service.GetTablesForSelectAsync();

        //    return Ok(new ApiResponse<List<TableSelectDto>>
        //    {
        //        Success = true,
        //        Code = 200,
        //        UserMessage = $"Get All Table For Create Order",
        //        Data = result,
        //        ServerTime = DateTimeOffset.UtcNow
        //    });
        //}
    }
}
