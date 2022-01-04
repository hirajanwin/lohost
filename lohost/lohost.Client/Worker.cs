using lohost.Logging;
using lohost.Client.Models;
using lohost.Client.Services;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace lohost.Client
{
    public class Worker : BackgroundService
    {
        private readonly ApplicationDataService _applicationDataService;

        private Log _logger;
        private ApplicationData _applicationData;
        private ApplicationAPI _applicationAPI;

        public Worker(ApplicationDataService applicationDataService)
        {
            _applicationDataService = applicationDataService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_applicationData == null) _applicationData = await _applicationDataService.GetApplicationData();

            if (_logger == null) _logger = new Log(_applicationData.GetLogsFolder(), 7);

            _logger.Info("lohost client: v0.1");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_applicationAPI == null)
                { 
                    try
                    {
                        _applicationAPI = new ApplicationAPI(_logger, _applicationData);
                        await _applicationAPI.ConnectSignalR();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Error connecting to website API", ex);
                    }
                }
            }

            if (_applicationAPI != null)
            {
                _logger.Info("Stopping connection");


                await _applicationAPI.Stop();
                _applicationAPI = null;
            }
        }
    }
}
