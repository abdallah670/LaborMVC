using System;

namespace LaborDAL.Common
{
    /// <summary>
    /// Abstraction for time provider to enable testability and consistent UTC handling
    /// </summary>
    public interface IClock
    {
        /// <summary>
        /// Gets the current UTC time
        /// </summary>
        DateTimeOffset UtcNow { get; }
    }

    /// <summary>
    /// Production implementation using system clock
    /// </summary>
    public class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Testable implementation for unit testing with fixed or manipulable time
    /// </summary>
    public class TestableClock : IClock
    {
        private DateTimeOffset _currentTime;

        public TestableClock(DateTimeOffset? initialTime = null)
        {
            _currentTime = initialTime ?? DateTimeOffset.UtcNow;
        }

        public DateTimeOffset UtcNow => _currentTime;

        /// <summary>
        /// Sets the clock to a specific time (for testing)
        /// </summary>
        public void SetTime(DateTimeOffset time)
        {
            _currentTime = time;
        }

        /// <summary>
        /// Advances the clock by a specified duration (for testing)
        /// </summary>
        public void Advance(TimeSpan duration)
        {
            _currentTime = _currentTime.Add(duration);
        }
    }
}
