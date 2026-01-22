using Core.Attribute;
using Core.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        [HttpGet("status")]
        [HasPermission(Permissions.ViewAccount)]
        public IActionResult GetAccountStatus()
        {
            // Placeholder implementation
            return Ok(new { Status = "account is active" });
        }
    }
}
