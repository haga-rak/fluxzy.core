// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Desktop.Services
{
    public class DesktopException : Exception
    {
        public DesktopException(string message)
            : base(message)
        {
        }

        public DesktopErrorMessage DesktopMessage => new(base.Message);
    }
}
