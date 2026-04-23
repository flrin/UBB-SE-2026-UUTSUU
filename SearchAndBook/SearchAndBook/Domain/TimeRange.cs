namespace SearchAndBook.Domain
{
    using System;

    /// <summary>
    /// Represents a range of time defined by a start and end point.
    /// </summary>
    /// <remarks>The start and end times are inclusive. The end time must not be earlier than the start time.
    /// This class can be used to model intervals for scheduling, filtering, or time-based calculations.</remarks>
    public class TimeRange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeRange"/> class with the specified start and end times.
        /// </summary>
        /// <param name="startTime">The start time of the range. Must be less than or equal to endTime.</param>
        /// <param name="endTime">The end time of the range. Must be greater than or equal to startTime.</param>
        /// <exception cref="ArgumentException">Thrown when endTime is earlier than startTime.</exception>
        public TimeRange(DateTime startTime, DateTime endTime)
        {
            if (endTime < startTime)
            {
                throw new ArgumentException("EndTime must be after StartTime");
            }

            this.StartTime = startTime;
            this.EndTime = endTime;
        }

        /// <summary>
        /// Gets or sets the start time for the operation or event.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of the event or operation.
        /// </summary>
        public DateTime EndTime { get; set; }
    }
}