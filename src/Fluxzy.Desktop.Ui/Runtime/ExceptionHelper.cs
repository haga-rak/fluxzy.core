// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Desktop.Ui.Runtime
{
    internal static class ExceptionHelper
    {
        public static Exception? FindException(this Exception original, Func<Exception, bool> predicate)
        {
            var current = original;

            while (current != null) {
                if (predicate(current))
                    return current;

                current = current.InnerException;
            }

            return null;
        }
    }
}
