using AIForgedPollingExample.Models;
using AIForgedPollingExample.WorkerFunctions;

namespace AIForgedPollingExample
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfig _config;

        //Our logger and config is automatically passed in by the application host when it creates an instance of this worker
        public Worker(ILogger<Worker> logger, IConfig config)
        {
            _logger = logger;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //Create an instance of our AIForgedWorker class and pass in our logger and config
            AIForgedWorker worker = new AIForgedWorker(_logger, _config);

            //Run our worker until app stop is requested
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                //Call our Poll and Store Data function
                await worker.PollAndStoreDataAsync();

                //Delay the next run by the interval configuration
                await Task.Delay((int)_config.Interval.TotalMilliseconds, stoppingToken);
            }
        }
    }
}
