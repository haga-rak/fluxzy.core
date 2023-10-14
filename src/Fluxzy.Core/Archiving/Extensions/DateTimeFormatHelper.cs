// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Archiving.Extensions
{
    internal static class DateTimeFormatHelper
    {
        public static string? FormatWithLocalKind(this DateTime date)
        {
            return date == DateTime.MinValue ? null : DateTime.SpecifyKind(date, DateTimeKind.Local).ToString("o");
        }
    }
}
