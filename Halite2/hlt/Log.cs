using System.IO;

namespace Halite2.hlt
{
    public static class Log
    {
        private static StreamWriter writer;

        public static void Initialize(StreamWriter w)
        {
            writer = w;
        }

        public static void LogMessage(string message)
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
