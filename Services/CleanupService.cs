using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using USPTOQueryBuilder.Services;

namespace USPTOQueryBuilder.Services
{
    public class CleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CleanupService> _logger;

        public CleanupService(IServiceProvider serviceProvider, ILogger<CleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var fileService = scope.ServiceProvider.GetRequiredService<FileProcessingService>();
                        var queryStorage = scope.ServiceProvider.GetRequiredService<QueryStorageService>();

                        // Clean up old files
                        fileService.CleanupOldFiles(7); // Keep files for 7 days

                        // Clean up old query metadata
                        queryStorage.CleanupOldQueries(30); // Keep queries for 30 days

                        _logger.LogInformation("Cleanup service executed successfully");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in cleanup service");
                }

                // Run cleanup once a day
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}