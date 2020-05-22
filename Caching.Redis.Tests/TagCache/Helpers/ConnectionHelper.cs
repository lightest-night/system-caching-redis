using System;
using System.Globalization;

namespace LightestNight.System.Caching.Redis.Tests.TagCache.Helpers
{
    public static class ConnectionHelper
    {
        public static string IntegrationTestHost => Environment.GetEnvironmentVariable("INTEGRATION_TEST_HOST") ?? string.Empty;
        public static string? Password => Environment.GetEnvironmentVariable("INTEGRATION_TEST_PASSWORD");
        public static int Port => Convert.ToInt32(Environment.GetEnvironmentVariable("INTEGRATION_TEST_PORT"), CultureInfo.InvariantCulture);
        public static bool UseSsl => Convert.ToBoolean(Environment.GetEnvironmentVariable("INTEGRATION_TEST_USE_SSL"), CultureInfo.InvariantCulture);
    }
}