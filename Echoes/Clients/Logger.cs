// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.IO;
using System.Threading;
using Echoes.H2;

namespace Echoes
{
    public static class Logger
    {
        private static readonly object _writeFileLock = new object(); 
        
        public static void WriteLine(ref H2FrameReadResult frame, WindowSizeHolder holder)
        {
            if (!DebugContext.EnableWindowSizeTrace)
                return;

            if (frame.BodyType == H2FrameType.WindowUpdate)
            {
                var data = frame.GetWindowUpdateFrame();

                Logger.WriteLine(
                    $"Receiving {frame.BodyType} ({data.WindowSizeIncrement}) " +
                    $"on streamId {frame.StreamIdentifier} / Flags : {frame.Flags} / ({holder.WindowSize})");
            }
            else
            {
                WriteLine(
                    $"Receiving {frame.BodyType} ({frame.BodyLength}) on streamId {frame.StreamIdentifier} / Flags : {frame.Flags}");
            }
        }

        private static int TotalSent = 0; 

        internal static void WriteLine(WriteTask writeTask)
        {
            if (!DebugContext.EnableWindowSizeTrace)
                return; 

            if (writeTask.FrameType == H2FrameType.Data)
                Interlocked.Add(ref TotalSent, writeTask.BufferBytes.Length); 

            WriteLine($"Sending {writeTask.BufferBytes.Length} " +
                      $"on streamId {writeTask.StreamIdentifier} {writeTask.FrameType} (Total : {TotalSent})");
        }


        public static void WriteLine(string line)
        {
            if (!DebugContext.EnableWindowSizeTrace)
                return;

            if (DebugContext.EnableWindowSizeTrace)
            {
                var fileName = DebugContext.WindowSizeTraceDumpDirectory + $"/{DebugContext.SessionDate}-trace.txt"; 
                
                lock(_writeFileLock)
                    File.AppendAllText(fileName, line + Environment.NewLine);
            }
            // Console.WriteLine(line);
        }
    }
}