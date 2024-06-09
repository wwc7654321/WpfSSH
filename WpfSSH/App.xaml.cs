using Microsoft.Extensions.Logging;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
//using NLog;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace WpfSSH
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        ILogger<App>? logger;

        protected override void OnStartup(StartupEventArgs e)
        {
            var config = new ConfigurationBuilder()
                .Build();
            //.SetBasePath(System.IO.Directory.GetCurrentDirectory()) //From NuGet Package Microsoft.Extensions.Configuration.Json
            //.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            //string logFilePath = "log.log";
            //StreamWriter logFileWriter = new StreamWriter(logFilePath, append: true);
            //using var loggerFactory = LoggerFactory.Create(builder =>
            //{
            //    builder
            //        .AddFilter("Microsoft", LogLevel.Warning)
            //        .AddFilter("System", LogLevel.Warning)
            //        .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
            //        .AddProvider(new CustomFileLoggerProvider(logFileWriter));
            //});

            using var servicesProvider = new ServiceCollection()
                .AddTransient<MainWindow>() // Runner is the custom class
                .AddLogging(loggingBuilder =>
                {
                    // configure Logging with NLog
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddNLog(config);
                }).BuildServiceProvider();

            ILoggerFactory? loggerFactory = servicesProvider.GetService<ILoggerFactory>();
            logger = loggerFactory?.CreateLogger<App>();
            logger?.LogInformation("App Start");

            var mainWindow = servicesProvider.GetRequiredService<MainWindow>();

            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
        }
        protected override void OnExit(ExitEventArgs e)
        {
            logger?.LogInformation("App Exit\n");
        }

        public static string? GetProductVersion()
        {
            Assembly? assembly = Assembly.GetEntryAssembly();
            //Version? version = Assembly.GetEntryAssembly()?.GetName().Version;
            //string? informationalVersion = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }
    }

    //// Customized ILoggerProvider, writes logs to text files
    //public class CustomFileLoggerProvider : ILoggerProvider
    //{
    //    private readonly StreamWriter _logFileWriter;
    //
    //    public CustomFileLoggerProvider(StreamWriter logFileWriter)
    //    {
    //        _logFileWriter = logFileWriter ?? throw new ArgumentNullException(nameof(logFileWriter));
    //    }
    //
    //    public ILogger CreateLogger(string categoryName)
    //    {
    //        return new CustomFileLogger(categoryName, _logFileWriter);
    //    }
    //
    //    public void Dispose()
    //    {
    //        _logFileWriter.Dispose();
    //    }
    //}
    //
    //// Customized ILogger, writes logs to text files
    //public class CustomFileLogger : ILogger
    //{
    //    private readonly string _categoryName;
    //    private readonly StreamWriter _logFileWriter;
    //
    //    public CustomFileLogger(string categoryName, StreamWriter logFileWriter)
    //    {
    //        _categoryName = categoryName;
    //        _logFileWriter = logFileWriter;
    //    }
    //
    //    public IDisposable BeginScope<TState>(TState state)
    //    {
    //        return null;
    //    }
    //
    //    public bool IsEnabled(LogLevel logLevel)
    //    {
    //        // Ensure that only information level and higher logs are recorded
    //        return logLevel >= LogLevel.Information;
    //    }
    //
    //    public void Log<TState>(
    //        LogLevel logLevel,
    //        EventId eventId,
    //        TState state,
    //        Exception exception,
    //        Func<TState, Exception, string> formatter)
    //    {
    //        // Ensure that only information level and higher logs are recorded
    //        if (!IsEnabled(logLevel))
    //        {
    //            return;
    //        }
    //
    //        // Get the formatted log message
    //        var message = formatter(state, exception);
    //
    //        //Write log messages to text file
    //        _logFileWriter.WriteLine($"[{logLevel}] [{_categoryName}] {message}");
    //        _logFileWriter.Flush();
    //    }
    //}
}
