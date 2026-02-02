using Core.DTO.Email;
using Core.Interface.Service.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static System.Net.WebRequestMethods;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Test : ControllerBase
    {

        private readonly ILogger<Test> _logger;
        private readonly IEmailQueue _emailQueue;
        public Test(ILogger<Test> logger, IEmailQueue emailQueue)
        {
            _logger = logger;
            _emailQueue = emailQueue;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("pong");
        }

        [HttpGet("test-email")]
        public async Task<IActionResult> TestEmail()
        {
            var email = "nguyendatrip123@gmail.com";
            var otp = "123456";


            await _emailQueue.EnqueueAsync(new QueuedEmail(
                To: email,
                Subject: "Reset password code",
                HtmlBody: $"<p>Your code is <b>{otp}</b></p>",
                CorrelationId: Guid.NewGuid().ToString("N")
            ));


            return Ok("Email test initiated.");
        }
    }
}
