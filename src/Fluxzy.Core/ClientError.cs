// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using MessagePack;

namespace Fluxzy
{
    [MessagePackObject]
    public class ClientError
    {

        public ClientError(int errorCode, string message)
        {
            ErrorCode = errorCode;
            Message = message;
        }

        [Key(0)]
        public int ErrorCode { get; }

        [Key(1)]
        public string Message { get; }

        [Key(2)]
        public string? ExceptionMessage { get; set; }


        protected bool Equals(ClientError other)
        {
            return ErrorCode == other.ErrorCode && Message == other.Message && ExceptionMessage == other.ExceptionMessage;
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
            return HashCode.Combine(ErrorCode, Message, ExceptionMessage);
        }
    }
}
