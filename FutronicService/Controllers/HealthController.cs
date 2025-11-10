using System.Threading.Tasks;
using FutronicService.Models;
using FutronicService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FutronicService.Controllers
{
    [ApiController]
    [Route("")]
 public class HealthController : ControllerBase
{
   private readonly IFingerprintService _fingerprintService;
        private readonly ILogger<HealthController> _logger;

 public HealthController(IFingerprintService fingerprintService, ILogger<HealthController> logger)
       {
            _fingerprintService = fingerprintService;
    _logger = logger;
        }

        /// <summary>
   /// GET /health
  /// Verifica estado del servicio
  /// </summary>
     [HttpGet("health")]
 public async Task<IActionResult> GetHealth()
        {
        _logger.LogInformation("Health endpoint called");
     var result = await _fingerprintService.GetHealthAsync();

   if (!result.Success)
       {
     return StatusCode(503, result);
  }

 return Ok(result);
  }
    }
}
