using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AIApp5Agent.Plugins
{
    public class DateTimePlugin
    {
        [KernelFunction]
        [Description("Gets the current date and time in India (IST timezone")]
        public string GetCurrentDateTime()
        {
            var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var indiaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);
            return $"Current date and time in India: {indiaTime:dddd, MMMM dd, yyyy 'at' hh:mm:tt} IST";
        }

        [KernelFunction]
        [Description("Gets only the current date in India")]
        public string GetCurrentDate()
        {
            var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var indiaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);
            return $"Today is {indiaTime:dddd, MMMM dd, yyyy} in India";
        }

        [KernelFunction]
        [Description("Gets the current day of week in India")]
        public string GetDayOfWeek()
        {
            var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var indiaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);
            return $"Today is {indiaTime:dddd}";
        }
    }
}
