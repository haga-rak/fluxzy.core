// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text;

namespace Fluxzy.Core;

public static class ExceptionUtils
{
    /// <summary>
    ///     Retrieve an inner or aggregate exception
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ex"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool TryGetException<T>(this Exception ex, out T result)
        where T : Exception
    {
        if (ex is T exception) {
            result = exception;

            return true;
        }

        if (ex.InnerException != null)
            return TryGetException(ex.InnerException, out result);

        if (ex is AggregateException aggregateException) {
            foreach (var innerException in aggregateException.InnerExceptions) {
                if (TryGetException(innerException, out result))
                    return true;
            }
        }

        result = null!;

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    public static string SanitizeHeaderValue(string input, int maxLength = 1024)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length);
        bool previousWasSpace = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            // Reject CR/LF entirely to prevent header injection
            if (c == '\r' || c == '\n')
                continue;

            // Allowed: HTAB, SP, VCHAR, obs-text (0x80-0xFF)
            bool isValid =
                c == '\t' ||
                (c >= ' ' && c <= '~') ||
                (c >= 0x80 && c <= 0xFF);

            if (!isValid)
                continue;

            // Normalize whitespace: collapse spaces and tabs
            if (c == ' ' || c == '\t')
            {
                if (previousWasSpace)
                    continue;

                sb.Append(' ');
                previousWasSpace = true;
            }
            else
            {
                sb.Append(c);
                previousWasSpace = false;
            }

            if (sb.Length >= maxLength)
                break;
        }

        // Trim trailing space if any
        if (sb.Length > 0 && sb[^1] == ' ')
            sb.Length -= 1;

        return sb.ToString();
    }
}