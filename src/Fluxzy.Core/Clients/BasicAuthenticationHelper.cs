// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;

namespace Fluxzy.Clients
{
    internal static class BasicAuthenticationHelper
    {
        public static string GetBasicAuthHeader(NetworkCredential credential)
        {
            var auth = $"{credential.UserName}:{credential.Password}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(auth);
            return $"Basic {System.Convert.ToBase64String(bytes)}";
        }
    }
}
