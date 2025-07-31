using Microsoft.Extensions.Logging;

namespace UKHO.ADDS.Configuration.Dashboard.Services
{
    public class DashboardService
    {
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(ILogger<DashboardService> logger) => _logger = logger;

    }
}
