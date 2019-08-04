using System;

namespace LightestNight.System.Caching.Redis.Tests.TagCache.Helpers
{
    public static class ConnectionHelper
    {
//        public static string IntegrationTestHost => Environment.GetEnvironmentVariable("INTEGRATION_TEST_HOST");
//        public static string Password => Environment.GetEnvironmentVariable("INTEGRATION_TEST_PASSWORD");
//        public static int Port => Convert.ToInt32(Environment.GetEnvironmentVariable("INTEGRATION_TEST_PORT"));
//        public static bool UseSsl => Convert.ToBoolean(Environment.GetEnvironmentVariable("INTEGRATION_TEST_USE_SSL"));
        public static string IntegrationTestHost => "integration-tests-lightestnight-71a6.aivencloud.com";
        public static string Password =>  "ngf1fkibv9fh5cv7";
        public static int Port => 19198;
        public static bool UseSsl => true;
    }
}