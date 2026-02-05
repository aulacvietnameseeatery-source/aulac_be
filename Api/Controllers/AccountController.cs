using Core.Attribute;
using Core.Data;
using Core.DTO.Auth;
using Core.Interface.Service.Entity;
using Microsoft.AspNetCore.Mvc;
using API.Models;
using System.Security.Claims;
using Core.DTO.Account;

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

        /// <summary>
        /// Creates a new staff account with system-generated username and temporary password.
        /// </summary>
        /// <param name="request">Account creation details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created account information</returns>
        /// <response code="201">Account created successfully</response>
        /// <response code="400">Validation error or business rule violation</response>
        /// <response code="404">Role not found</response>
        /// <response code="409">Email already exists</response>
        /// <remarks>
        /// **Business Flow:**
        /// 1. Validates email uniqueness and role existence
        /// 2. Generates unique username from full name (pattern: firstname.lastname)
        /// 3. Generates secure random temporary password (12+ characters)
        /// 4. Creates account with status = LOCKED
        /// 5. Sends temporary password to user's email
        /// 6. User must change password on first login to activate account
        /// 
        /// **Username Generation:**
        /// - Pattern: firstname.lastname (e.g., "nguyen.an" from "Nguyễn Văn An")
        /// - Handles Vietnamese diacritics (removes accents)
        /// - Collision handling: appends incremental numbers (nguyen.an2, nguyen.an3, etc.)
        /// 
        /// **Security:**
        /// - Temporary password is cryptographically random
        /// - Password is hashed before storage (never stored in plaintext)
        /// - Account starts in LOCKED state
        /// - Requires CreateAccount permission
        /// </remarks>
        [HttpPost("create")]
        [HasPermission(Permissions.CreateAccount)]
        [ProducesResponseType(typeof(ApiResponse<CreateAccountResult>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _accountService.CreateAccountAsync(request, cancellationToken);

            _logger.LogInformation("Account created successfully. ID: {AccountId}, Username: {Username}", result.AccountId, result.Username);

            return StatusCode(201, new ApiResponse<CreateAccountResult>
            {
                Success = true,
                Code = 201,
                UserMessage = "Account created successfully. Temporary password sent to email.",
                Data = result,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// Updates account profile information (excluding password).
        /// </summary>
        /// <param name="id">Account ID to update</param>
        /// <param name="request">Updated account fields</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated account information</returns>
        /// <response code="200">Account updated successfully</response>
        /// <response code="400">Validation error</response>
        /// <response code="403">Unauthorized to change role (non-admin)</response>
        /// <response code="404">Account or role not found</response>
        /// <response code="409">Email already exists</response>
        /// <remarks>
        /// **Updatable Fields:**
        /// - Email (must be unique)
        /// - Full Name
        /// - Phone (can be cleared with null or empty string)
        /// - Role (admin-only)
        /// 
        /// **Authorization Rules:**
        /// - Any authenticated user can call this endpoint
        /// - Non-admin users can update their own or other accounts' email/name/phone
        /// - Only admins can change roles
        /// - Attempting to change role as non-admin returns 403 Forbidden
        /// 
        /// **Password Updates:**
        /// - Password cannot be updated via this endpoint
        /// - Use the dedicated password change endpoint instead
        /// </remarks>
        [HttpPut("{id}")]
        [HasPermission(Permissions.UpdateAccount)]
        [ProducesResponseType(typeof(ApiResponse<AccountDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateAccount(long id, [FromBody] UpdateAccountRequest request, CancellationToken cancellationToken = default)
        {
            var requestingUserId = GetCurrentUserId();
            if (!requestingUserId.HasValue)
            {
                return Unauthorized();
            }

            var result = await _accountService.UpdateAccountAsync(
                id,
                request,
                requestingUserId.Value,
                cancellationToken);

            _logger.LogInformation(
             "Account updated. ID: {AccountId}, UpdatedBy: {RequestingUserId}",
                id,
            requestingUserId);

            return Ok(new ApiResponse<AccountDetailDto>
            {
                Success = true,
                Code = 200,
                UserMessage = "Account updated successfully.",
                Data = result,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// Gets detailed account information including role and resolved status.
        /// </summary>
        /// <param name="id">Account ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Detailed account information</returns>
        /// <response code="200">Account found</response>
        /// <response code="404">Account not found</response>
        /// <remarks>
        /// **Returned Information:**
        /// - Core fields: ID, username, email, full name, phone
        /// - Account status (resolved from lookup: "ACTIVE", "LOCKED", etc.)
        /// - Lock status
        /// - Timestamps: created at, last login
        /// - Role information (if assigned)
        /// 
        /// **Use Case:**
        /// - Display account details in admin panel
        /// - View user profile
        /// - Check account status before operations
        /// </remarks>
        [HttpGet("{id}/detail")]
        [HasPermission(Permissions.ViewAccount)]
        [ProducesResponseType(typeof(ApiResponse<AccountDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAccountDetail(
            long id,
            CancellationToken cancellationToken = default)
        {
            var account = await _accountService.GetAccountDetailAsync(id, cancellationToken);

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

            return Ok(new ApiResponse<AccountDetailDto>
            {
                Success = true,
                Code = 200,
                UserMessage = "Account retrieved successfully.",
                Data = account,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// Gets the current user's account information.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current user's account details</returns>
        /// <response code="200">Account found</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="404">Account not found (user deleted)</response>
        [HttpGet("me")]
        [HasPermission(Permissions.ViewAccount)]
        [ProducesResponseType(typeof(ApiResponse<AccountDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var account = await _accountService.GetAccountDetailAsync(userId.Value, cancellationToken);

            if (account == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Code = 404,
                    UserMessage = "Your account was not found.",
                    Data = new { },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }

            return Ok(new ApiResponse<AccountDetailDto>
            {
                Success = true,
                Code = 200,
                UserMessage = "Account retrieved successfully.",
                Data = account,
                ServerTime = DateTimeOffset.UtcNow
            });
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
        /// 
        /// **Post-Reset Behavior:**
        /// - Account becomes LOCKED
        /// - User must change password on next login
        /// - Account will be activated after password change
        /// </remarks>
        [HttpPost("{id}/reset-password")]
        [HasPermission(Permissions.ResetPassword)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword(long id, CancellationToken cancellationToken = default)
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
                UserMessage = $"Password has been reset for account '{result.Username}'. User must change password on next login.",
                SystemMessage = "Password reset successful",
                Data = new
                {
                    AccountId = result.AccountId,
                    Username = result.Username,
                    FullName = result.FullName,
                    Message = result.Message
                },
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// Gets account details by ID (legacy method).
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

        /// <summary>
        /// Changes the current user's password.
        /// Supports both first-time password change (for locked accounts) and normal password change.
        /// </summary>
        /// <param name="request">Password change request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success confirmation</returns>
        /// <response code="200">Password changed successfully</response>
        /// <response code="400">Validation error (weak password, passwords don't match, etc.)</response>
        /// <response code="401">Not authenticated or current password incorrect</response>
        /// <response code="404">Account not found</response>
        /// <remarks>
        /// **Two Scenarios:**
        /// 
        /// 1. **First-Time Password Change (Locked Account)**:
        ///    - User has just logged in with temporary password
        ///    - Account status is LOCKED
        ///    - Current password is NOT required
        ///    - After successful change:
        ///      - Account status changes to ACTIVE
        ///      - IsLocked set to false
        ///      - User can login normally
        /// 
        /// 2. **Normal Password Change (Active Account)**:
        ///    - User is changing their existing password
        ///    - Current password IS required and must be verified
        ///    - New password must be different from current password
        ///    - Account status remains ACTIVE
        /// 
        /// **Password Requirements:**
        /// - Minimum 8 characters
        /// - Maximum 128 characters
        /// - New password and confirmation must match
        /// 
        /// **Security:**
        /// - User can only change their own password
        /// - Current password verification for normal changes
        /// - Password strength validation
        /// - All changes are logged for audit
        /// 
        /// **Request Body:**
        /// ```json
        /// {
        ///   "currentPassword": "OldPassword123!",  // Required for normal change, optional for first-time
        ///   "newPassword": "NewSecurePassword123!",
        ///   "confirmPassword": "NewSecurePassword123!"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("change-password")]
        //[HasPermission(Permissions.ViewAccount)] // User needs to be authenticated
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var success = await _accountService.ChangePasswordForSelfAsync(
                userId.Value,
                request.CurrentPassword,
                request.NewPassword,
                cancellationToken);

            if (success)
            {
                _logger.LogInformation(
                    "Password changed successfully for user ID {UserId}",
                    userId);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Code = 200,
                    UserMessage = "Password changed successfully. Please login with your new password.",
                    Data = new
                    {
                        Message = "Password has been updated",
                        RequiresReLogin = true
                    },
                    ServerTime = DateTimeOffset.UtcNow
                });
            }

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Code = 400,
                UserMessage = "Failed to change password.",
                Data = new { },
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        #region Helper Methods

        /// <summary>
        /// Gets the current user's ID from JWT claims.
        /// </summary>
        private long? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("user_id");
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }

        #endregion
    }
}
