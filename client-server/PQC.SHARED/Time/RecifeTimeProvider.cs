using System;
using System.Collections.Generic;
using System.Text;

namespace PQC.SHARED.Time
{
    public static class RecifeTimeProvider
    {
        public static DateTime Now()
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Recife");
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                }
                catch
                {
                    return DateTime.UtcNow.AddHours(-3);
                }
            }
        }
    }
}
