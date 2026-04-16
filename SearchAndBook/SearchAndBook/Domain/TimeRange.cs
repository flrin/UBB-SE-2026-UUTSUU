using System;

namespace SearchAndBook.Domain
{
    public class TimeRange
    {
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public TimeRange(DateTime startTime, DateTime endTime)
        {
            if (endTime < startTime)
            {
                throw new ArgumentException("EndTime must be after StartTime");
            }

            StartTime = startTime;
            EndTime = endTime;
        }
    }
}