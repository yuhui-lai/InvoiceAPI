namespace InvoiceAPI.Utils
{
    public class TimeUtil
    {
        public static DateTime UnifiedNow()
        {
            return DateTime.UtcNow.AddHours(8);
        }
    }
}
