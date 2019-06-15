using System;
using System.Collections.Generic;
using System.Text;
using McMaster.Extensions.CommandLineUtils;

namespace Internal.AspNetCore.ReportGenerator
{
    internal static class ConsoleExtensions
    {
        public static void WriteWarningLineAsync(this IConsole console, string message)
        {
            var oldFg = console.ForegroundColor;
            console.ForegroundColor = ConsoleColor.Yellow;
            console.Error.Write("warning: ");
            console.ForegroundColor = oldFg;
            console.Error.WriteLine(message);
        }

        public static void WriteErrorLineAsync(this IConsole console, string message)
        {
            var oldFg = console.ForegroundColor;
            console.ForegroundColor = ConsoleColor.Red;
            console.Error.Write("error: ");
            console.ForegroundColor = oldFg;
            console.Error.WriteLine(message);
        }
    }
}
