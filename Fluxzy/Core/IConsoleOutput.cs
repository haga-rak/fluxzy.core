using System.IO;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    public interface IConsoleOutput
    {
        void WriteLine(string str);

        Task WriteLineAsync(string str); 
    }

    public class DefaultConsoleOutput : IConsoleOutput
    {
        private readonly TextWriter _writer;

        public DefaultConsoleOutput(TextWriter writer)
        {
            _writer = writer;
        }

        public void WriteLine(string str)
        {
            _writer?.WriteLine(str);
            _writer?.Flush();
        }

        public async Task WriteLineAsync(string str)
        {
            if (_writer != null)
                await _writer.WriteLineAsync(str).ConfigureAwait(false);

        }
    }
}