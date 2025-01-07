using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Helpers
{
    public static class Utility
    {
        public static DateTime getCurrentTimestamps()
        {
            DateTime utcNow = DateTime.UtcNow; // Get the current UTC time
            TimeZoneInfo gmtPlus7 = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Time zone for GMT+7
            DateTime gmtPlus7Time = TimeZoneInfo.ConvertTimeFromUtc(utcNow, gmtPlus7); // Convert UTC to GMT+7
            return gmtPlus7Time;
        }
    }

}