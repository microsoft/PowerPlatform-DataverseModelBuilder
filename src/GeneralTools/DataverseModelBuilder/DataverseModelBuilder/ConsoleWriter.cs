using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerPlatform.Dataverse.DataverseModelBuilder
{
    internal class ConsoleWriter
    {
        public void WriteLine(string value)
        {
            Console.WriteLine(value);
        }

        public void WriteWarning(string value)
        {
            using (new OutputColors(ConsoleColor.Yellow))
            {
                Console.WriteLine(value);
            }
        }

        public void WriteError(string value)
        {
            using (new OutputColors(ConsoleColor.Red))
            {
                Console.WriteLine(value);
            }
        }

        /// <summary>
        /// Sets console output colors and restores original ones on dispose
        /// </summary>
        private class OutputColors : IDisposable
        {
            // only set if we actually changed the color
            private readonly ConsoleColor? _previousForegroundColor;

            public OutputColors(ConsoleColor foregroundColor)
            {
                // It is impossible to read if Background == Foreground
                if (Console.BackgroundColor != foregroundColor)
                {
                    _previousForegroundColor = Console.ForegroundColor;
                    Console.ForegroundColor = foregroundColor;
                }
            }

            public void Dispose()
            {
                if (_previousForegroundColor != null)
                    Console.ForegroundColor = _previousForegroundColor.Value;
            }
        }

    }
}
