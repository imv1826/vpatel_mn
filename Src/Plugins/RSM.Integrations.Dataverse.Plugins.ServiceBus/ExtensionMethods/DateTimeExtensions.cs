using System;

namespace RSM.Integrations.Dataverse.Common.ExtensionMethods
{
    public static class DateTimeExtensions
    {
        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToUnixTimeSeconds(this DateTime dateTime)
        {
            return (long)GetUnixTime(dateTime).TotalSeconds;
        }

        public static long ToUnixTime(this DateTime dateTime)
        {
            return (long)GetUnixTime(dateTime).TotalMilliseconds;
        }

        private static TimeSpan GetUnixTime(DateTime dateTime)
        {
            return dateTime.ToUniversalTime() - _unixEpoch;
        }
    }
}