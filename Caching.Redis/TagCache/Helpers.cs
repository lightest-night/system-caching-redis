using System;

namespace LightestNight.System.Caching.Redis.TagCache
{
    internal static class Helpers
    {
        internal static DateTime RankToTime(double value)
            => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(value);

        internal static long TimeToRank(DateTime date)
            => Convert.ToInt64((date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
    }
}