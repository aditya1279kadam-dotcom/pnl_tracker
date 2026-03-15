using Microsoft.AspNetCore.Mvc;
using FinanceOS.Backend.Models;
using FinanceOS.Backend.Services;

namespace FinanceOS.Backend.Controllers
{
    [ApiController]
    [Route("api")]
    public class CalculationController : ControllerBase
    {
        private readonly IFinanceEngineService _financeEngine;
        private readonly IResourceEngineService _resourceEngine;
        private readonly IAppState _appState;

        public CalculationController(IFinanceEngineService financeEngine, IResourceEngineService resourceEngine, IAppState appState)
        {
            _financeEngine = financeEngine;
            _resourceEngine = resourceEngine;
            _appState = appState;
        }

        [HttpGet("calculate")]
        public IActionResult Calculate([FromQuery] CalculationFilters filters)
        {
            _appState.FinanceState.Filters = filters;
            var results = _financeEngine.Process(_appState.FinanceState);
            results.LastRefreshed = _appState.LastRefreshed?.ToString("yyyy-MM-ddTHH:mm:ssZ");
            return Ok(results);
        }

        [HttpGet("calculate-resource")]
        public IActionResult CalculateResource([FromQuery] CalculationFilters filters)
        {
            _appState.ResourceState.Filters = filters;
            // Ensure resource state has necessary data from finance state if needed
            var results = _resourceEngine.Process(_appState.ResourceState);
            return Ok(results);
        }
    }
}
