using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace lohost.API.Logging
{
    public class SystemLogging
    {
        private ILogger _logger;

        private TelemetryClient _telemetryClient;

        public SystemLogging()
        {
        }

        public SystemLogging(ILogger logger)
        {
            _logger = logger;
        }

        public SystemLogging(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public void Debug(string log, Exception ex = null)
        {
            log = BuildLog(log, ex);

            Trace.TraceInformation("Debug: " + log);

            if (_telemetryClient != null)
            {
                _telemetryClient.TrackTrace(log, SeverityLevel.Information, new Dictionary<string, string>()
                {
                    { "level", "DEBUG" },
                    { "ThreadId", Thread.CurrentThread.ManagedThreadId.ToString() }
                });
            }

            if (_logger != null)
            {
                if (ex != null) _logger.LogDebug(ex, log);
                else _logger.LogDebug(log);
            }
        }

        public void Info(string log, Exception ex = null)
        {
            log = BuildLog(log, ex);

            Trace.TraceInformation("Info: " + log);

            if (_telemetryClient != null)
            {
                _telemetryClient.TrackTrace(log, SeverityLevel.Information, new Dictionary<string, string>()
                {
                    { "level", "INFO" },
                    { "ThreadId", Thread.CurrentThread.ManagedThreadId.ToString() }
                });
            }

            if (_logger != null)
            {
                if (ex != null) _logger.LogInformation(ex, log);
                else _logger.LogInformation(log);
            }
        }

        public void Warn(string log, Exception ex = null)
        {
            log = BuildLog(log, ex);

            Trace.TraceError("Warn: " + log);

            if (_telemetryClient != null)
            {
                _telemetryClient.TrackTrace(log, SeverityLevel.Information, new Dictionary<string, string>()
                {
                    { "level", "WARN" },
                    { "ThreadId", Thread.CurrentThread.ManagedThreadId.ToString() }
                });
            }

            if (_logger != null)
            {
                if (ex != null) _logger.LogWarning(ex, log);
                else _logger.LogWarning(log);
            }
        }

        public void Error(string log, Exception ex = null)
        {
            log = BuildLog(log, ex);

            Trace.TraceError("Error: " + log);

            if (_telemetryClient != null)
            {
                _telemetryClient.TrackTrace(log, SeverityLevel.Information, new Dictionary<string, string>()
                {
                    { "level", "ERROR" },
                    { "ThreadId", Thread.CurrentThread.ManagedThreadId.ToString() }
                });
            }

            if (_logger != null)
            {
                if (ex != null) _logger.LogError(ex, log);
                else _logger.LogError(log);
            }
        }

        public void Fatal(string log, Exception ex = null)
        {
            log = BuildLog(log, ex);

            Trace.TraceError("Fatal: " + log);

            if (_telemetryClient != null)
            {
                _telemetryClient.TrackTrace(log, SeverityLevel.Information, new Dictionary<string, string>()
                {
                    { "level", "FATAL" },
                    { "ThreadId", Thread.CurrentThread.ManagedThreadId.ToString() }
                });
            }

            if (_logger != null)
            {
                if (ex != null) _logger.LogError(ex, "*** FATAL *** " + log);
                else _logger.LogError("*** FATAL *** " + log);
            }
        }

        private string BuildLog(string log, Exception ex = null)
        {
            string logMessage = log;

            if (ex != null) logMessage += ": " + ex.ToString();

            return logMessage;
        }
    }
}