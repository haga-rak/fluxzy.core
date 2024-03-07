// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Core
{
    public class ClientErrorException : Exception
    {
        public ClientErrorException(int errorCode, string message, string? innerMessageException = null,
            Exception ? innerException = null)
            : base(message, innerException)
        {
            ClientError = new ClientError(errorCode, message) {
                ExceptionMessage = innerMessageException
            };
        }

        public ClientError ClientError { get; }
    }
}
