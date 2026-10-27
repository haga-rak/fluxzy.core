// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fluxzy.Cli.Commands.Dissects
{
    /// <summary>
    /// Exchange selection for dissect. Every set criterion must match (AND).
    /// </summary>
    internal class ExchangeFilterOptions
    {
        public HashSet<int>? ExchangeIds { get; init; }

        /// <summary>Glob on the authority host name, case insensitive.</summary>
        public string? Host { get; init; }

        /// <summary>Glob on the path (with query). Wrap in slashes for a regex: /^\/api\/.*$/</summary>
        public string? Path { get; init; }

        /// <summary>Comma separated methods, case insensitive.</summary>
        public string? Method { get; init; }

        /// <summary>Comma separated codes, ranges (400-499) or classes (5xx).</summary>
        public string? Status { get; init; }

        /// <summary>Substring on the simplified content type, case insensitive.</summary>
        public string? ContentType { get; init; }

        public int Offset { get; init; }

        public int? Limit { get; init; }

        public bool HasCriteria =>
            ExchangeIds is { Count: > 0 } || Host != null || Path != null || Method != null
            || Status != null || ContentType != null;

        public ExchangeMatcher Compile()
        {
            return new ExchangeMatcher(this);
        }
    }

    internal class ExchangeMatcher
    {
        private readonly HashSet<int>? _ids;
        private readonly Regex? _host;
        private readonly Regex? _path;
        private readonly HashSet<string>? _methods;
        private readonly List<(int From, int To)>? _statusRanges;
        private readonly string? _contentType;

        public ExchangeMatcher(ExchangeFilterOptions options)
        {
            _ids = options.ExchangeIds is { Count: > 0 } ? options.ExchangeIds : null;
            _host = options.Host == null ? null : GlobOrRegex(options.Host);
            _path = options.Path == null ? null : GlobOrRegex(options.Path);

            _methods = options.Method == null
                ? null
                : options.Method.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         .ToHashSet(StringComparer.OrdinalIgnoreCase);

            _statusRanges = options.Status == null ? null : ParseStatus(options.Status);
            _contentType = options.ContentType;
        }

        public bool Match(ExchangeInfo e)
        {
            if (_ids != null && !_ids.Contains(e.Id)) {
                return false;
            }

            if (_host != null && !_host.IsMatch(e.KnownAuthority)) {
                return false;
            }

            if (_path != null && !_path.IsMatch(e.Path)) {
                return false;
            }

            if (_methods != null && !_methods.Contains(e.Method)) {
                return false;
            }

            if (_statusRanges != null && !_statusRanges.Any(r => e.StatusCode >= r.From && e.StatusCode <= r.To)) {
                return false;
            }

            if (_contentType != null
                && (e.ContentType == null
                    || !e.ContentType.Contains(_contentType, StringComparison.OrdinalIgnoreCase))) {
                return false;
            }

            return true;
        }

        public static Regex GlobOrRegex(string value)
        {
            if (value.Length >= 2 && value[0] == '/' && value[^1] == '/') {
                return new Regex(value[1..^1], RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            var pattern = "^" + Regex.Escape(value).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";

            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        public static List<(int From, int To)> ParseStatus(string value)
        {
            var result = new List<(int, int)>();

            foreach (var raw in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                if (raw.Length == 3 && raw.EndsWith("xx", StringComparison.OrdinalIgnoreCase)
                    && char.IsDigit(raw[0])) {
                    var c = raw[0] - '0';
                    result.Add((c * 100, c * 100 + 99));

                    continue;
                }

                var dash = raw.IndexOf('-');

                if (dash > 0) {
                    if (int.TryParse(raw[..dash], out var from) && int.TryParse(raw[(dash + 1)..], out var to)) {
                        result.Add((from, to));

                        continue;
                    }
                }
                else if (int.TryParse(raw, out var code)) {
                    result.Add((code, code));

                    continue;
                }

                throw new ArgumentException(
                    $"Invalid status filter \"{raw}\". Use a code (404), a range (400-499) or a class (5xx)");
            }

            return result;
        }
    }
}
