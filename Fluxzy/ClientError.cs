// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy
{
    public class ClientError
    {
        public ClientError(int errorCode, string message)
        {
            ErrorCode = errorCode;
            Message = message;
        }

        public int ErrorCode { get; }

        public string Message { get; }

        public string? ExceptionMessage { get; set; }
    }
}
