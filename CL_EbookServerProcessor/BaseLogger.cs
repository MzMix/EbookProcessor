using Serilog;

namespace CL_EbookServerProcessor
{
    /// <summary>
    /// Abstract logger class that is used for injecting loggers into ebook processor.
    /// </summary>
    public abstract class BaseLogger
    {
        public abstract void LogMessage(string message);
        public abstract void LogMessage(Exception exception);
    }

    /// <summary>
    /// Logger based on <c>Console.WriteLine</c>x.
    /// </summary>
    public class NativeConsoleLogger : BaseLogger
    {
        public override void LogMessage(string message) => Console.WriteLine(message);
        public override void LogMessage(Exception exception) => LogMessage(exception.Message);
    }

    /// <summary>
    /// Logger base on Serilog that outputs Information or Error logs based on provided type(string, Exception)
    /// </summary>
    public class SerilogConsoleLogger : BaseLogger
    {
        public SerilogConsoleLogger()
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        }

        public override void LogMessage(string message)
        {
            Log.Information(message);
        }

        public override void LogMessage(Exception exception)
        {
            Log.Error(exception.Message);
        }
    }
}
