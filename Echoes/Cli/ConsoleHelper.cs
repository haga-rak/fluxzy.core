using System;
using System.Threading.Tasks;

namespace Echoes.Cli
{
    public static class ConsoleHelper
    {
        public static Task WaitForExit()
        {
            TaskCompletionSource<object> source = new TaskCompletionSource<object>();

            Console.CancelKeyPress += (sender, args) =>
            {
                source.SetResult(null); ;
            };

            AppDomain.CurrentDomain.ProcessExit += (o, args) =>
            {
                if (!source.Task.IsCompleted)
                    source.SetResult(null);
            };

            return source.Task;
        }
    }
}