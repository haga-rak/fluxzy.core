// Copyright © 2023 Haga RAKOTOHARIVELO

using System;

namespace Fluxzy.Utils.Curl
{
    public static class CurlExportSetting
    {
        static CurlExportSetting()
        {
            CurlPostDataTempPath = Environment.GetEnvironmentVariable("FLUXZY_CURL_TEMP_DATA")
                                   ?? "%appdata%/Fluxzy/Curl/Temp";

            CurlPostDataTempPath = Environment.ExpandEnvironmentVariables(CurlPostDataTempPath);
        }

        public static string CurlPostDataTempPath { get; internal set; }
    }
}
