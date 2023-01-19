using System;

namespace siliu
{
    public class ServerTime
    {
        private static long offset;

        public static void Init(long s)
        {
            offset = s - LocalNow;
        }

        /// <summary>
        /// 服务器当前时间戳, 毫秒
        /// </summary>
        public static long Now => offset + LocalNow;

        /// <summary>
        /// 服务器当前时间戳, 秒
        /// </summary>
        public static int NowSeconds = (int)(Now / 1000);

        /// <summary>
        /// 本地时间戳, 毫秒
        /// </summary>
        private static long LocalNow
        {
            get
            {
                var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                return Convert.ToInt64(ts.TotalMilliseconds);
            }
        }
        public static DateTime DateTimeNow()
        {
            long timeStamp = Now;
            DateTime dtStart = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1970, 1, 1, 0, 0, 0), TimeZoneInfo.Local);
            long lTime = ((long)timeStamp * 10000);
            TimeSpan toNow = new TimeSpan(lTime);
            DateTime targetDt = dtStart.Add(toNow);
            return targetDt;
        }
    }
}