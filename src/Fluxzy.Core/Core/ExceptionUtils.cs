// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Core
{
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
    }
}
