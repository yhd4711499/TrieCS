/* High precision timer class
 *   - http://www.eggheadcafe.com/articles/20021111.asp
 * 
 *  (Thanks to the author!)
 */

using System.Runtime.InteropServices;
using System.ComponentModel;

namespace TrieCS
{
    public class HiPerfTimer
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);
        private long _startTime;
        private long _stopTime;
        private long _freq;
        /// <summary>
        /// ctor
        /// </summary>
        public HiPerfTimer()
        {
            _startTime = 0;
            _stopTime = 0;
            _freq = 0;
            if (QueryPerformanceFrequency(out _freq) == false)
            {
                throw new Win32Exception(); // timer not supported
            }
        }
        /// <summary>
        /// Start the timer
        /// </summary>
        /// <returns>long - tick count</returns>
        public long Start()
        {
            QueryPerformanceCounter(out _startTime);
            return _startTime;
        }
        /// <summary>
        /// Stop timer 
        /// </summary>
        /// <returns>long - tick count</returns>
        public long Stop()
        {
            QueryPerformanceCounter(out _stopTime);
            return _stopTime;
        }
        /// <summary>
        /// Return the duration of the timer (in seconds)
        /// </summary>
        /// <returns>double - duration</returns>
        public double Duration
        {
            get
            {
                return (_stopTime - _startTime) / (double)_freq;
            }
        }
        /// <summary>
        /// Frequency of timer (no counts in one second on this machine)
        /// </summary>
        ///<returns>long - Frequency</returns>
        public long Frequency
        {
            get
            {
                QueryPerformanceFrequency(out _freq);
                return _freq;
            }
        }
    }
}