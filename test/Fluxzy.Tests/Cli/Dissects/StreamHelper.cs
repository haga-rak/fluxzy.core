// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Text;

namespace Fluxzy.Tests.Cli.Dissects
{
    internal static class StreamHelper
    {
        public static string ReadAsString(this Stream stream)
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public static string[] ReadAsLines(this Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd().Split(new[] { Environment.NewLine }, 
                System.StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
