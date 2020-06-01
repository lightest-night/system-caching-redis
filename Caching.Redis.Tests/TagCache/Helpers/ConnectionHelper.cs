using System;
using System.Globalization;

namespace LightestNight.System.Caching.Redis.Tests.TagCache.Helpers
{
    public static class ConnectionHelper
    {
        // public static string IntegrationTestHost => Environment.GetEnvironmentVariable("INTEGRATION_TEST_HOST") ?? string.Empty;
        // public static string? Password => Environment.GetEnvironmentVariable("INTEGRATION_TEST_PASSWORD");
        // public static int Port => Convert.ToInt32(Environment.GetEnvironmentVariable("INTEGRATION_TEST_PORT"), CultureInfo.InvariantCulture);
        // public static bool UseSsl => Convert.ToBoolean(Environment.GetEnvironmentVariable("INTEGRATION_TEST_USE_SSL"), CultureInfo.InvariantCulture);
        public static string IntegrationTestHost => "192.168.150.106";
        public static string? Password => "j3d1kn1g#t";
        public static int Port => 6379;
        public static bool UseSsl => false;
    }
}