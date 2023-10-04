// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.CommandLine;

namespace Fluxzy.Cli.Commands
{
    public class DissectCommandBuilder
    {
        public Command Build()
        {
            var command = new Command("dissect", "Read content of a previously captured file or directory"); 

            command.AddAlias("dis");

            // Command list 

            // options 

            // -fi : Filter by exchange id, can be comma separated of exchanges 
            // 

            // -f "content;url;method;authority;host;port;request-body;pcap;header:'Authority'" // formatting option 
            // -o  "output-file" 
            // -u  --unique: Result must be unique or exit error 
            // 
            
            return command;
        }


    }
}
