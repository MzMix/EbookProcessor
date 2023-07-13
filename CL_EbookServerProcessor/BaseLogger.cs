using Serilog;

namespace CL_EbookServerProcessor
{
    public abstract class BaseLogger
    {
        public abstract void LogMessage(string message);
        public abstract void LogMessage(Exception exception);
    }

    public class NativeConsoleLogger : BaseLogger
    {
        public override void LogMessage(string message) => Console.WriteLine(message);
        public override void LogMessage(Exception exception) => LogMessage(exception.Message);
    }

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
