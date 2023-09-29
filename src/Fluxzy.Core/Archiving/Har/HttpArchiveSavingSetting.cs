// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Har
{
    public sealed class HttpArchiveSavingSetting
    {
        /// <summary>
        ///     Default implementation
        /// </summary>
        public static HttpArchiveSavingSetting Default { get; } = new() {
            Policy = HttpArchiveSavingBodyPolicy.MaxLengthSave
        };

        /// <summary>
        ///     Defines when the request/response body should be saved
        /// </summary>
        public HttpArchiveSavingBodyPolicy Policy { get; set; } = HttpArchiveSavingBodyPolicy.SkipBody;

        /// <summary>
        ///     When HttpArchiveSavingBodyPolicy.MaxLengthSave is set, this value defines the maximum length of the body to be
        ///     saved
        /// </summary>
        public int HarLimitMaxBodyLength { get; set; } = 512 * 1024;

        public bool Comply(long length)
        {
            switch (Policy) {
                case HttpArchiveSavingBodyPolicy.SkipBody:
                    return false;

                case HttpArchiveSavingBodyPolicy.MaxLengthSave:
                    return length <= HarLimitMaxBodyLength;

                case HttpArchiveSavingBodyPolicy.AlwaysSave:
                    return true;

                default:
                    return false;
            }
        }
    }
}
