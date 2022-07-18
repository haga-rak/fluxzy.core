using System;

namespace Echoes
{
    internal static class DateTimeFormatHelper
    {
        public static string FormatWithLocalKind(this DateTime date)
        {
            if (date == DateTime.MinValue)
                return null;

            var printable = DateTime.SpecifyKind(date, DateTimeKind.Local);
            return printable.ToString("o");
        }
    }
}
