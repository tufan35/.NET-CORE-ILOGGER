namespace WebApplication7.Logging
{
    public static class LoggingExtension
    {
        public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, IConfiguration configuration)
        {
            builder.Services.AddSingleton<ILoggerProvider>(new FileLoggerProvider(configuration, (category) => true));
            return builder;
        }
    }
}
