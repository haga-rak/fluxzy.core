// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Threading.Tasks;

namespace Fluxzy.Cli.Commands.Dissects
{
    internal interface IDissectionFormatter<in T>
    {
        string Indicator { get; }

        Task Write(T payload, StreamWriter stdOutWriter);
    }
}
