using System.Text;

namespace WebApplication7.Logging
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly IConfiguration configuration1;
        private readonly Func<string, bool> logLevelFunc;

        public FileLoggerProvider(IConfiguration configuration1, Func<string, bool> logLevelFunc)
        {
            this.configuration1 = configuration1;
            this.logLevelFunc = logLevelFunc;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(configuration1, logLevelFunc, categoryName);
        }

        public void Dispose()
        {

        }
    }

    public class FileLogger : ILogger
    {

        private readonly string logPath;
        private readonly int maxFileSize;
        private readonly LogLevel logLevel;
        private readonly Func<string, bool> logLevelFunc;
        private readonly string categoryName;

        private string currentFilePath;
        private long currentFileSize;

        private SemaphoreSlim semaphore = new(1, 1);

        public FileLogger(IConfiguration configuration, Func<string, bool> logLevelFunc, string categoryName)
        {
            logPath = configuration["File:Path"];
            maxFileSize = configuration.GetValue<int>("File:MaxFileSize");
            string logLevelString = configuration["Logging:LogLevel:Default"];
            Enum.TryParse<LogLevel>(logLevelString, out logLevel);
            this.logLevelFunc = logLevelFunc;
            this.categoryName = categoryName;

            InitilazeLogger();
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return default;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel < this.logLevel)
            {
                return false;
            }

            return logLevelFunc(categoryName);
        }

        public async void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);

            message = $"[{DateTime.UtcNow:dd.MM.yyyy HH:mm:ss}] [{logLevel}] - {message}";

            await WriteLogToFile(message);
        }

        private async Task WriteLogToFile(string message)
        {
            try
            {
                await semaphore.WaitAsync();

                if (currentFileSize >= maxFileSize)
                {
                    currentFilePath = GetNextFilePath();
                    currentFileSize = 0;
                }

                byte[] encodedText = Encoding.UTF8.GetBytes(message + Environment.NewLine);
                using FileStream sourceStream = new FileStream(currentFilePath, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
                await sourceStream.WriteAsync(encodedText);
                currentFileSize += encodedText.Length;

            }
            finally
            {
                semaphore.Release();
            }
        }

        private string GetNextFilePath()
        {
            string baseFileName = Path.GetFileNameWithoutExtension(logPath);
            string directory = Path.GetDirectoryName(logPath);
            string extension = Path.GetExtension(logPath);

            for (int index = 0; ; index++)
            {
                string filePath = Path.Combine(directory, $"{baseFileName}_{index}{extension}");
                if (!File.Exists(filePath))
                {
                    return filePath;
                }
            }
        }

        private void InitilazeLogger()
        {
            string directory = Path.GetDirectoryName(logPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            currentFilePath = logPath;
            currentFileSize = File.Exists(currentFilePath) ? new FileInfo(currentFilePath).Length : 0;

        }
    }
}
