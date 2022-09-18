using System;

namespace Fluxzy
{
    internal static class DateTimeFormatHelper
    {
        public static string FormatWithLocalKind(this DateTime date)
        {
            return date == DateTime.MinValue ? null : DateTime.SpecifyKind(date, DateTimeKind.Local).ToString("o");
        }
    }
}