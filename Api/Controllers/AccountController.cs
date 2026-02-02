using Core.Attribute;
using Core.Data;
using Core.Interface.Service.Entity;
using Microsoft.AspNetCore.Mvc;
using API.Models;

namespace Api.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
  IAccountService accountService,
    ILogger<AccountController> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        [HttpGet("status")]
        [HasPermission(Permissions.ViewAccount)]
        public IActionResult GetAccountStatus()
        {
       // Placeholder implementation
            return Ok(new { Status = "account is active" });
   }

        /// <summary>
    /// Resets a staff account password to the system's default password.
        /// </summary>
        /// <param name="id">The staff account ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success confirmation</returns>
        /// <response code="200">Password reset successful</response>
    /// <response code="404">Account not found</response>
     /// <response code="400">Default password setting not configured</response>
        /// <remarks>
        /// This endpoint resets the specified user's password to the default password
   /// configured in the system settings (setting_key: 'default_password').
        ///
        /// **Security Notes:**
        /// - Requires ResetPassword permission
        /// - Retrieves default password from system_setting table via service layer
        /// - Password is properly hashed before storage
        /// - Operation is logged for audit purposes
        /// - Uses clean architecture with repository and service patterns
        /// </remarks>
    [HttpPost("{id}/reset-password")]
  [HasPermission(Permissions.ResetPassword)]
 [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword(long id, CancellationToken cancellationToken = default)
        {
       try
    {
           // Use the account service for business logic
      var result = await _accountService.ResetToDefaultPasswordAsync(id, cancellationToken);

  _logger.LogInformation(
          "Password reset successful for account ID {AccountId} (Username: {Username})",
            result.AccountId,
  result.Username);

                return Ok(new ApiResponse<object>
                {
          Success = true,
          Code = 200,
      UserMessage = $"Password has been reset to the default password for account '{result.Username}'.",
       SystemMessage = "Password reset successful",
     Data = new
       {
         AccountId = result.AccountId,
       Username = result.Username,
         FullName = result.FullName,
             Message = "User should change their password after first login"
 },
         ServerTime = DateTimeOffset.UtcNow
    });
 }
        catch (KeyNotFoundException ex)
        {
    _logger.LogWarning(ex, "Account not found for password reset: {AccountId}", id);

    return NotFound(new ApiResponse<object>
   {
      Success = false,
       Code = 404,
      UserMessage = "Account not found.",
            SystemMessage = ex.Message,
  Data = new { },
        ServerTime = DateTimeOffset.UtcNow
   });
      }
            catch (InvalidOperationException ex)
  {
    _logger.LogError(ex, "Default password not configured");

    return BadRequest(new ApiResponse<object>
    {
             Success = false,
      Code = 400,
  UserMessage = ex.Message,
        SystemMessage = "Missing default_password configuration in system_setting table",
      Data = new { },
                  ServerTime = DateTimeOffset.UtcNow
       });
            }
        catch (Exception ex)
            {
           _logger.LogError(ex, "Unexpected error while resetting password for account ID {AccountId}", id);

           return StatusCode(500, new ApiResponse<object>
  {
Success = false,
            Code = 500,
        UserMessage = "An error occurred while resetting the password. Please try again.",
    SystemMessage = ex.Message,
         Data = new { },
      ServerTime = DateTimeOffset.UtcNow
      });
            }
 }

     /// <summary>
     /// Gets account details by ID.
        /// </summary>
    /// <param name="id">The account ID</param>
 /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Account information</returns>
        /// <response code="200">Account found</response>
        /// <response code="404">Account not found</response>
        [HttpGet("{id}")]
  [HasPermission(Permissions.ViewAccount)]
 [ProducesResponseType(typeof(ApiResponse<AccountDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAccountById(long id, CancellationToken cancellationToken = default)
        {
            try
       {
   var account = await _accountService.GetAccountByIdAsync(id, includeRole: true, cancellationToken);

    if (account == null)
       {
  return NotFound(new ApiResponse<object>
  {
       Success = false,
            Code = 404,
            UserMessage = "Account not found.",
       Data = new { },
  ServerTime = DateTimeOffset.UtcNow
   });
      }

                return Ok(new ApiResponse<AccountDto>
      {
           Success = true,
        Code = 200,
 UserMessage = "Account retrieved successfully.",
          Data = account,
                    ServerTime = DateTimeOffset.UtcNow
    });
            }
 catch (Exception ex)
     {
             _logger.LogError(ex, "Error retrieving account {AccountId}", id);

          return StatusCode(500, new ApiResponse<object>
    {
   Success = false,
    Code = 500,
       UserMessage = "An error occurred while retrieving the account.",
        SystemMessage = ex.Message,
         Data = new { },
     ServerTime = DateTimeOffset.UtcNow
        });
     }
        }
    }
}
