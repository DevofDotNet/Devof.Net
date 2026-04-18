using Blog.Application.Services;

namespace Blog.Web.Services;

/// <summary>
/// Background service that periodically recalculates trending scores for all posts.
/// Runs every hour.
/// </summary>
public class TrendingScoreBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TrendingScoreBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public TrendingScoreBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TrendingScoreBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TrendingScoreBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var postService = scope.ServiceProvider.GetRequiredService<IPostService>();

                _logger.LogInformation("Updating trending scores...");
                await postService.UpdateTrendingScoresAsync(stoppingToken);
                _logger.LogInformation("Trending scores updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating trending scores.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
