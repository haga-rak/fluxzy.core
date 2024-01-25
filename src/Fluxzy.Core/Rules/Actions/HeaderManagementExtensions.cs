// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Rules.Extensions;

namespace Fluxzy.Rules.Actions
{
    public static class HeaderManagementExtensions
    {
        /// <summary>
        ///     Deletes a request header from the action builder.
        /// </summary>
        /// <param name="actionBuilder">The action builder.</param>
        /// <param name="headerName">The name of the header to delete.</param>
        /// <returns>The updated configure filter builder.</returns>
        public static IConfigureFilterBuilder
            DeleteRequestHeader(this IConfigureActionBuilder actionBuilder, string headerName)
        {
            actionBuilder.Do(new DeleteRequestHeaderAction(headerName));

            return new ConfigureFilterBuilderBuilder(actionBuilder.Setting);
        }

        /// <summary>
        ///     Deletes the specified response header from the action builder.
        /// </summary>
        /// <param name="actionBuilder">The action builder.</param>
        /// <param name="headerName">The name of the header to delete.</param>
        /// <returns>An instance of <see cref="IConfigureFilterBuilder" /> for further configuration.</returns>
        public static IConfigureFilterBuilder
            DeleteResponseHeader(this IConfigureActionBuilder actionBuilder, string headerName)
        {
            actionBuilder.Do(new DeleteResponseHeaderAction(headerName));

            return new ConfigureFilterBuilderBuilder(actionBuilder.Setting);
        }

        /// <summary>
        ///     Adds a request header to the <see cref="IConfigureActionBuilder" /> instance.
        /// </summary>
        /// <param name="actionBuilder">The configure action builder to add the request header to.</param>
        /// <param name="headerName">The name of the header to add.</param>
        /// <param name="headerValue">The value of the header to add.</param>
        /// <returns>A new instance of <see cref="IConfigureFilterBuilder" /> which allows further configuration.</returns>
        public static IConfigureFilterBuilder
            AddRequestHeader(this IConfigureActionBuilder actionBuilder, string headerName, string headerValue)
        {
            actionBuilder.Do(new AddRequestHeaderAction(headerName, headerValue));

            return new ConfigureFilterBuilderBuilder(actionBuilder.Setting);
        }

        /// <summary>
        ///     Adds a response header with the specified name and value to the action builder.
        /// </summary>
        /// <param name="actionBuilder">The action builder to add the response header to.</param>
        /// <param name="headerName">The name of the response header.</param>
        /// <param name="headerValue">The value of the response header.</param>
        /// <returns>The action builder with the added response header.</returns>
        public static IConfigureFilterBuilder
            AddResponseHeader(this IConfigureActionBuilder actionBuilder, string headerName, string headerValue)
        {
            actionBuilder.Do(new AddResponseHeaderAction(headerName, headerValue));

            return new ConfigureFilterBuilderBuilder(actionBuilder.Setting);
        }

        /// <summary>
        ///     Replaces the value of a request header with a new value.
        /// </summary>
        /// <param name="actionBuilder">The action builder.</param>
        /// <param name="headerName">The name of the header to replace.</param>
        /// <param name="headerValue">The new value for the header.</param>
        /// <returns>A new instance of the <see cref="IConfigureFilterBuilder" /> interface.</returns>
        public static IConfigureFilterBuilder
            UpdateRequestHeader(this IConfigureActionBuilder actionBuilder, string headerName, string headerValue)
        {
            actionBuilder.Do(new UpdateRequestHeaderAction(headerName, headerValue));

            return new ConfigureFilterBuilderBuilder(actionBuilder.Setting);
        }

        /// <summary>
        ///     Replaces the value of a specified response header with a new value.
        /// </summary>
        /// <param name="actionBuilder">The <see cref="IConfigureActionBuilder" /> used to configure the action.</param>
        /// <param name="headerName">The name of the header to replace.</param>
        /// <param name="headerValue">The new value to set for the header.</param>
        /// <returns>A new <see cref="IConfigureFilterBuilder" /> with the updated response header.</returns>
        public static IConfigureFilterBuilder
            UpdateResponseHeader(this IConfigureActionBuilder actionBuilder, string headerName, string headerValue)
        {
            actionBuilder.Do(new UpdateResponseHeaderAction(headerName, headerValue));

            return new ConfigureFilterBuilderBuilder(actionBuilder.Setting);
        }
    }
}
