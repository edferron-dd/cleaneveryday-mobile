using DatadogSdk.Maui;
using Microsoft.Extensions.Logging;

namespace CleanEverydayMobile.Services;

public sealed class DatadogLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new DatadogLogger(categoryName);
    }

    public void Dispose()
    {
    }

    private sealed class DatadogLogger : ILogger
    {
        private readonly string _categoryName;

        public DatadogLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            if (string.IsNullOrWhiteSpace(message) && exception is null)
            {
                return;
            }

            var formattedMessage = $"[{_categoryName}] {message}";
            if (exception is not null)
            {
                formattedMessage = $"{formattedMessage}{Environment.NewLine}{exception}";
            }

            try
            {
                switch (logLevel)
                {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    DdLogs.Debug(formattedMessage);
                    break;
                case LogLevel.Information:
                    DdLogs.Info(formattedMessage);
                    break;
                case LogLevel.Warning:
                    DdLogs.Warn(formattedMessage);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    DdLogs.Error(formattedMessage);
                    break;
                }
            }
            catch
            {
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}