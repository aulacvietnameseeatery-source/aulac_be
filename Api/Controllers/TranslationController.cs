using Core.DTO.Dish;
using Core.DTO.SystemSetting;
using Core.Interface.Service.Others;
using Microsoft.AspNetCore.Mvc;
using API.Models;
using Core.DTO.LookUpValue;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/translate")]
    public class TranslationController : Controller
    {
        private readonly ITranslationService _translationService;

        public TranslationController(ITranslationService translationService)
        {
            _translationService = translationService;
        }

        [HttpPost("dish")]
        public async Task<IActionResult> TranslateDish([FromBody] TranslateDishRequest request)
        {
            if (!new[] { "vi", "en", "fr" }.Contains(request.SourceLang))
                return BadRequest(new ApiResponse<object> { Success = false, Code = 400, UserMessage = "Unsupported language." });

            var translations = await _translationService
                .TranslateDishAsync(request.SourceLang, request.Data);

            return Ok(new ApiResponse<TranslateDishResponse>
            {
                Success = true,
                Code = 200,
                Data = new TranslateDishResponse { Translations = translations }
            });
        }

        [HttpPost("system-settings")]
        public async Task<IActionResult> TranslateSystemSettings([FromBody] TranslateSystemSettingsRequest request)
        {
            if (!new[] { "vi", "en", "fr" }.Contains(request.SourceLang))
                return BadRequest(new ApiResponse<object> { Success = false, Code = 400, UserMessage = "Unsupported language." });

            var translations = await _translationService
                .TranslateSystemSettingsAsync(request.SourceLang, request.Data);

            return Ok(new ApiResponse<TranslateSystemSettingsResponse>
            {
                Success = true,
                Code = 200,
                Data = new TranslateSystemSettingsResponse { Translations = translations }
            });
        }

        [HttpPost("lookup")]
        public async Task<IActionResult> TranslateLookup([FromBody] TranslateLookupRequest request)
        {
            if (!new[] { "vi", "en", "fr" }.Contains(request.SourceLang))
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Code = 400,
                    UserMessage = "Unsupported language."
                });

            var translations = await _translationService
                .TranslateLookupAsync(request.SourceLang, request.Data);

            return Ok(new ApiResponse<TranslateLookupResponse>
            {
                Success = true,
                Code = 200,
                Data = new TranslateLookupResponse
                {
                    Translations = translations
                }
            });
        }
    }
}
