using System.Threading.Tasks;

namespace LightestNight.System.Caching.Redis.TagCache
{
    public interface IRedisCacheLogger
    {
        /// <summary>
        /// Logs a message
        /// </summary>
        /// <param name="method">The method used when logging</param>
        /// <param name="arg">The argument to the logger</param>
        /// <param name="message">The log message</param>
        Task Log(string method, string arg, string? message);
    }
}