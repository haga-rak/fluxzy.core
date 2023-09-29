// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;

namespace Fluxzy.Misc
{
    /// <summary>
    /// Extensions utilities for allowing easing appending serialized data with mpack
    /// Every Stream must be seekable 
    /// </summary>
    internal static class MessagePackQueueExtensions
    {
        public static void AppendMultiple<T>(string filename, T payload, MessagePackSerializerOptions options)
        {
            using var fileStream = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            AppendMultiple(fileStream, payload, options);
        }

        public static void AppendMultiple<T>(Stream stream, T payload, MessagePackSerializerOptions options)
        {
            MessagePackSerializer.Serialize(stream, payload, options);
        }

        public static IEnumerable<T> DeserializeMultiple<T>(this Stream stream, MessagePackSerializerOptions options)
        {
            while (true)
            {
                if (stream.Position == stream.Length)
                    break;

                T item;

                try
                {
                    item = MessagePackSerializer.Deserialize<T>(stream, options);

                }
                catch (Exception)
                {
                    // 
                    yield break;
                }

                yield return item;
            }
        }

        public static IReadOnlyCollection<T> DeserializeMultiple<T>(string filename, MessagePackSerializerOptions options)
        {
            if (!File.Exists(filename))
                return Array.Empty<T>();

            using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return DeserializeMultiple<T>(fileStream, options).ToList();
        }
    }
}
