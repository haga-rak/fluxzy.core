// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Rules
{
    public class RuleConfigReaderError
    {
        public RuleConfigReaderError(string message)
        {
            Message = message;
        }

        /// <summary>
        ///     Creates an error carrying the YAML position, also appended to <see cref="Message" />.
        /// </summary>
        public RuleConfigReaderError(string message, int? line, int? column = null, string? path = null)
        {
            Line = line;
            Column = column;
            Path = path;

            Message = line.HasValue
                ? column.HasValue
                    ? $"{message} (line {line}, col {column})"
                    : $"{message} (line {line})"
                : message;
        }

        public string Message { get; }

        /// <summary>
        ///     The 1-based line where the error occurred, when known.
        /// </summary>
        public int? Line { get; }

        /// <summary>
        ///     The 1-based column where the error occurred, when known.
        /// </summary>
        public int? Column { get; }

        /// <summary>
        ///     Best-effort location within the document (e.g. <c>rules[1]</c>), when known.
        /// </summary>
        public string? Path { get; }

        public override string ToString()
        {
            return Message;
        }
    }
}
