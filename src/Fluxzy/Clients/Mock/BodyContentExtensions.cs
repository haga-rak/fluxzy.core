// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Clients.Mock
{
    public static class BodyContentExtensions
    {
        public static BodyContent AddHeader(this BodyContent body, string key, string value)
        {
            body.Headers.Add(key, value);
            return body;
        }
    }
}
