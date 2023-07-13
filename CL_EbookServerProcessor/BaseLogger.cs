namespace CL_EbookServerProcessor
{
    public abstract class BaseLogger
    {
        public abstract void LogMessage(string message);
    }

    public class ConsoleLogger : BaseLogger
    {
        public override void LogMessage(string message) => Console.WriteLine(message);
    }
}
