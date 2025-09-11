using LoanSuite.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboardService;

        public DashboardController(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetDashboardOverview()
        {
            var result = await _dashboardService.GetDashboardOverviewAsync();
            return Ok(result);
        }
    }
}
