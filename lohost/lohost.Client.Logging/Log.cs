using Serilog;
using Serilog.Core;
using System;

namespace lohost.Client.Logging
{
    public class Log
    {
        private Logger _logger;

        public Log(string path)
        {
            _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
            .WriteTo.File($"{path}\\logfile.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .CreateLogger();
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        public void Info(string message)
        {
            _logger.Information(message);
        }

        public void Error(string message, Exception ex = null)
        {
            if (ex != null) _logger.Error(message, ex);
            else _logger.Error(message);
        }
    }
}