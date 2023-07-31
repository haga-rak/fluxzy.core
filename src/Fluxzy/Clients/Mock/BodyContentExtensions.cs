// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Clients.Mock
{
    public static class BodyContentExtensions
    {
        public static BodyContent AddHeader(this BodyContent bodyContent, string key, string value)
        {
            bodyContent.Headers.Add(key, value);
            return bodyContent;
        }
    }
}
