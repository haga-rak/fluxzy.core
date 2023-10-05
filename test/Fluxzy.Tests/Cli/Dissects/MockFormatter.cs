// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Threading.Tasks;
using Fluxzy.Cli.Commands.Dissects;

namespace Fluxzy.Tests.Cli.Dissects
{
    internal class MockFormatter : IDissectionFormatter<int>
    {
        private readonly string _value;

        public MockFormatter(string indicator, string value)
        {
            _value = value;
            Indicator = indicator;
        }

        public string Indicator { get; }

        public Task Write(int exchangeInfo, StreamWriter stdOutWriter)
        {
            stdOutWriter.Write(_value);
            return Task.CompletedTask;
        }
    }
}
