// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Rules.Extensions;

namespace Fluxzy.Rules.Actions
{
    public static class TransformActionExtensions
    {
        /// <summary>
        /// Transform the response body using a function that takes the transform context and the original content as a string and returns the new content as a string.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigureActionBuilder"/> object.</param>
        /// <param name="transformFunction">A transformation function that shall return BodyContent or null if no change is made</param>
        /// <returns>The <see cref="IConfigureFilterBuilder"/> object.</returns>
        public static IConfigureFilterBuilder TransformResponse(this IConfigureActionBuilder builder,
            Func<TransformContext, IBodyReader, Task<BodyContent?>> transformFunction)
        {
            builder.Do(new TransformResponseBodyAction(transformFunction));
            return new ConfigureFilterBuilderBuilder(builder.Setting);
        }

        /// <summary>
        /// Transform the response body using a function that takes the transform context and the original content as a string and returns the new content as a string.
        /// This overload consumes the original stream directly. 
        /// </summary>
        /// <param name="builder">The <see cref="IConfigureActionBuilder"/> object.</param>
        /// <param name="transformFunction">Function that takes  the transform context and the original content as a string and returns the new content as a string, return null to avoid making changes</param>
        /// <returns>The <see cref="IConfigureFilterBuilder"/> object.</returns>
        public static IConfigureFilterBuilder TransformResponse(this IConfigureActionBuilder builder,
            Func<TransformContext, string, Task<string>> transformFunction)
        {
            var action = new TransformResponseBodyAction(async (c , reader) =>
            {
                var content = await reader.ConsumeAsString();
                return await transformFunction(c, content);
            });

            builder.Do(action);
            return new ConfigureFilterBuilderBuilder(builder.Setting);
        }

        /// <summary>
        /// Transform the response body using a function that takes the transform context and the original content as a string and returns the new content as a string.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigureActionBuilder"/> object.</param>
        /// <param name="transformFunction">Function that takes  the transform context and the original stream and returns the new content as stream, return null to avoid making changes</param>
        /// <returns>The <see cref="IConfigureFilterBuilder"/> object.</returns>
        public static IConfigureFilterBuilder TransformResponse(this IConfigureActionBuilder builder,
            Func<TransformContext, Stream, Task<Stream?>> transformFunction)
        {
            var action = new TransformResponseBodyAction(async (c , reader) => {
                var originalStream  = reader.ConsumeAsStream();
                var result = await transformFunction(c, originalStream);

                if (result == null)
                    return null;

                return new BodyContent(result, Encoding.UTF8);

            });

            builder.Do(action);
            return new ConfigureFilterBuilderBuilder(builder.Setting);
        }


        /// <summary>
        /// Transform the request body using a function that takes the transform context and the original content as a string and returns the new content as a string.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigureActionBuilder"/> object.</param>
        /// <param name="transformFunction">A transformation function that shall return BodyContent or null if no change is made</param>
        /// <returns>The <see cref="IConfigureFilterBuilder"/> object.</returns>
        public static IConfigureFilterBuilder TransformRequest(this IConfigureActionBuilder builder,
            Func<TransformContext, IBodyReader, Task<BodyContent?>> transformFunction)
        {
            builder.Do(new TransformRequestBodyAction(transformFunction));
            return new ConfigureFilterBuilderBuilder(builder.Setting);
        }

        /// <summary>
        /// Transform the request body using a function that takes the transform context and the original content as a string and returns the new content as a string.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigureActionBuilder"/> object.</param>
        /// <param name="transformFunction"> </param>
        /// <returns></returns>
        public static IConfigureFilterBuilder TransformRequest(this IConfigureActionBuilder builder,
            Func<TransformContext, string, Task<string>> transformFunction)
        {
            var action = new TransformRequestBodyAction(async (c, reader) =>
            {
                var content = await reader.ConsumeAsString();
                return await transformFunction(c, content);
            });
            builder.Do(action);
            return new ConfigureFilterBuilderBuilder(builder.Setting);
        }
    }
}
