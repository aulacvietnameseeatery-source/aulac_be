using Core.DTO.Dish;
using Core.Interface.Service.Others;
using Microsoft.AspNetCore.Mvc;

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
                return BadRequest("Unsupported language.");

            var translations = await _translationService
                .TranslateDishAsync(request.SourceLang, request.Data);

            return Ok(new TranslateDishResponse
            {
                Translations = translations
            });
        }
    }
}
