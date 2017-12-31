using System.IO;

namespace Halite2.hlt
{
    public static class Log
    {
        private static StreamWriter writer;

        public static bool IsEnabled { get; set; } = true;

        public static void Initialize(StreamWriter w)
        {
            writer = w;
        }

        public static void LogMessage(string message)
        {
            if (IsEnabled)
            {
                try
                {
                    writer.WriteLine(message);
                    writer.Flush();
                }
                catch (IOException)
                {
                } 
            }
        }
    }
}
