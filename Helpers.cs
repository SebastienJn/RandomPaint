using System;
using System.Diagnostics;
using System.IO;

namespace RandomPaint
{
    internal static class Helpers
    {
        public static readonly Random Random = new Random();

        public static void ShowFileInExplorer(string filePath)
        {
            try
            {
                var winDir = Environment.GetEnvironmentVariable("windir");
                if (winDir != null)
                {
                    var explorerPath = Path.Combine(winDir, @"explorer.exe");
                    var arguments = String.Format("/select, {0}{1}{0}", (char)34, filePath);
                    Process.Start(explorerPath, arguments);
                }
            }
            catch (Exception)
            {
                //handle the exception your way!
            }
        }
    }
}