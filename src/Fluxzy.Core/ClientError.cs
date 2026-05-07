// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using MessagePack;

namespace Fluxzy
{
    /// <summary>
    /// Holds information about a client error
    /// </summary>
    [MessagePackObject]
    public class ClientError
    {
        /// <summary>
        /// Create a new instance from error code and message
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="message"></param>
        public ClientError(int errorCode, string message)
        {
            ErrorCode = errorCode;
            Message = message;
        }

        /// <summary>
        /// Create a new instance with an explicit network error code
        /// </summary>
        public ClientError(int errorCode, string message, string? networkErrorCode)
        {
            ErrorCode = errorCode;
            Message = message;
            NetworkErrorCode = networkErrorCode;
        }

        /// <summary>
        /// OS error code
        /// </summary>
        [Key(0)]
        public int ErrorCode { get; }

        /// <summary>
        /// Friendly error message
        /// </summary>
        [Key(1)]
        public string Message { get; }

        /// <summary>
        /// Exception message
        /// </summary>
        [Key(2)]
        public string? ExceptionMessage { get; set; }

        /// <summary>
        /// Stable errno-like identifier for the kind of network failure (e.g. <c>connection_refused</c>,
        /// <c>dns_notfound</c>, <c>tls_cert_expired</c>). See <see cref="Fluxzy.Core.NetworkErrorCodes"/>.
        /// </summary>
        [Key(3)]
        public string? NetworkErrorCode { get; set; }


        protected bool Equals(ClientError other)
        {
            return ErrorCode == other.ErrorCode && Message == other.Message
                && ExceptionMessage == other.ExceptionMessage
                && NetworkErrorCode == other.NetworkErrorCode;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != this.GetType())
                return false;

            return Equals((ClientError)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ErrorCode, Message, ExceptionMessage, NetworkErrorCode);
        }
    }
}
