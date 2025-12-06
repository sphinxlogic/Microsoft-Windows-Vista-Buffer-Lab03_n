//------------------------------------------------------------------------------ 
// <copyright file="CacheMemory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Caching { 
    using System.Web.Configuration; 
    using System.Runtime.InteropServices;
    using System.Web.Util; 
    using System.Web;
    using System.Web.Hosting;

    abstract class CacheMemoryPressure { 
        protected const int     TERABYTE_SHIFT = 40;
        protected const long    TERABYTE = 1L << TERABYTE_SHIFT; 
 
        protected const int     GIGABYTE_SHIFT = 30;
        protected const long    GIGABYTE = 1L << GIGABYTE_SHIFT; 

        protected const int     MEGABYTE_SHIFT = 20;
        protected const long    MEGABYTE = 1L << MEGABYTE_SHIFT; // 1048576
 
        protected const int     KILOBYTE_SHIFT = 10;
        protected const long    KILOBYTE = 1L << KILOBYTE_SHIFT; // 1024 
 
        protected const int     HISTORY_COUNT = 6;
 
        protected int           _pressureHigh;      // high pressure level
        protected int           _pressureMiddle;    // middle pressure level - target
        protected int           _pressureLow;       // low pressure level - slow growth here
 
        protected int           _i0;
        protected int[]         _pressureHist; 
        protected int           _pressureTotal; 
        protected int           _pressureAvg;
 
        private static long     s_totalPhysical;
        private static long     s_totalVirtual;

        internal static void GetMemoryStatusOnce() { 
            UnsafeNativeMethods.MEMORYSTATUSEX  memoryStatusEx = new UnsafeNativeMethods.MEMORYSTATUSEX();
            memoryStatusEx.Init(); 
            if (UnsafeNativeMethods.GlobalMemoryStatusEx(ref memoryStatusEx) != 0) { 
                s_totalPhysical = memoryStatusEx.ullTotalPhys;
                s_totalVirtual = memoryStatusEx.ullTotalVirtual; 
            }
        }

        internal static long TotalPhysical { get { return s_totalPhysical; } } 
        internal static long TotalVirtual { get { return s_totalVirtual; } }
 
        protected abstract int GetCurrentPressure(); 

        internal virtual void ReadConfig(CacheSection cacheSection) {} 

        protected void InitHistory() {
            Debug.Assert(_pressureHigh > 0, "_pressureHigh > 0");
            Debug.Assert(_pressureLow > 0, "_pressureLow > 0"); 
            Debug.Assert(_pressureLow <= _pressureHigh, "_pressureLow <= _pressureHigh");
 
            int pressure = GetCurrentPressure(); 

            _pressureHist = new int[HISTORY_COUNT]; 
            for (int i = 0; i < HISTORY_COUNT; i++) {
                _pressureHist[i] = pressure;
                _pressureTotal +=  pressure;
            } 

            _pressureAvg = pressure; 
        } 

        // Get current pressure and update history 
        internal void Update() {
            int pressure = GetCurrentPressure();

 
#if FEATURE_PAL // _pressureHist may not be initialized if calls to
                // GlobalMemoryStatusEx fail. 
            if (_pressureHist != null) 
#endif // FEATURE_PAL
            { 

                _i0 = (_i0 + 1) % HISTORY_COUNT;

                _pressureTotal -= _pressureHist[_i0]; 
                _pressureTotal += pressure;
                _pressureHist[_i0] = pressure; 
                _pressureAvg = _pressureTotal / HISTORY_COUNT; 
            }
#if DBG 
            if (HttpRuntime.AppDomainAppIdInternal != null && HttpRuntime.AppDomainAppIdInternal.Length > 0) {
                if (!(pressure == 0 && PressureHigh == 99)) {
                    Debug.Trace("CacheMemory", this.GetType().Name + ".Update: last=" + pressure + ",avg=" + PressureAvg + ",high=" + PressureHigh + ",low=" + PressureLow + ",middle=" + PressureMiddle + " " + Debug.FormatLocalDate(DateTime.Now));
                } 
            }
#endif 
        } 

        internal int PressureLast { 
            get {

#if FEATURE_PAL // _pressureHist may not be initialized if calls to
                // GlobalMemoryStatusEx fail. 
                return (_pressureHist != null) ? _pressureHist[_i0] : 0;
#else // FEATURE_PAL 
                return _pressureHist[_i0]; 
#endif // FEATURE_PAL
 
            }

        }
 
        internal int PressureAvg {
            get { return _pressureAvg; } 
        } 

        internal int PressureHigh { 
            get { return _pressureHigh; }
        }

        internal int PressureLow { 
            get { return _pressureLow; }
        } 
 
        internal int PressureMiddle {
            get { return _pressureMiddle; } 
        }

        internal bool IsAboveHighPressure() {
            return PressureLast >= PressureHigh; 
        }
 
        internal bool IsAboveMediumPressure() { 
            return PressureLast > PressureMiddle;
        } 
    }

    // The GC aggressively collects when it receives a low memory notification. Make sure
    //  we have released references before it aggressively collects. 
    sealed class CacheMemoryTotalMemoryPressure : CacheMemoryPressure {
 
        internal CacheMemoryTotalMemoryPressure() { 
            /*
              The chart below shows physical memory in megabytes, and the 1, 3, and 10% values. 
              When we reach "middle" pressure, we begin trimming the cache.

              RAM     1%      3%      10%
              ----------------------------- 
              128     1.28    3.84    12.8
              256     2.56    7.68    25.6 
              512     5.12    15.36   51.2 
              1024    10.24   30.72   102.4
              2048    20.48   61.44   204.8 
              4096    40.96   122.88  409.6
              8192    81.92   245.76  819.2

              Low memory notifications from CreateMemoryResourceNotification are calculated as follows 
              (.\base\ntos\mm\initsup.c):
 
              MiInitializeMemoryEvents() { 
              ...
              // 
              // Scale the threshold so on servers the low threshold is
              // approximately 32MB per 4GB, capping it at 64MB.
              //
 
              MmLowMemoryThreshold = MmPlentyFreePages;
 
              if (MmNumberOfPhysicalPages > 0x40000) { 
                  MmLowMemoryThreshold = MI_MB_TO_PAGES (32);
                  MmLowMemoryThreshold += ((MmNumberOfPhysicalPages - 0x40000) >> 7); 
              }
              else if (MmNumberOfPhysicalPages > 0x8000) {
                  MmLowMemoryThreshold += ((MmNumberOfPhysicalPages - 0x8000) >> 5);
              } 

              if (MmLowMemoryThreshold > MI_MB_TO_PAGES (64)) { 
                  MmLowMemoryThreshold = MI_MB_TO_PAGES (64); 
              }
              ... 

              E.g.

              RAM(mb) low      % 
              -------------------
              256	  20	  92% 
              512	  24	  95% 
              768	  28	  96%
              1024	  32	  97% 
              2048	  40	  98%
              3072	  48	  98%
              4096	  56	  99%
              5120	  64	  99% 
            */
 
            long memory = TotalPhysical; 
            Debug.Assert(memory != 0, "memory != 0");
            if (memory >= 0x100000000) { 
                _pressureHigh = 99;
            }
            else if (memory >= 0x80000000) {
                _pressureHigh = 98; 
            }
            else if (memory >= 0x40000000) { 
                _pressureHigh = 97; 
            }
            else if (memory >= 0x30000000) { 
                _pressureHigh = 96;
            }
            else {
                _pressureHigh = 95; 
            }
 
            _pressureMiddle = _pressureHigh - 2; 
            _pressureLow = _pressureHigh - 9;
 
            InitHistory();

            // PerfCounter: Cache Percentage Machine Memory Limit Used
            //    = total physical memory used / total physical memory used limit 
            PerfCounters.SetCounter(AppPerfCounter.CACHE_PERCENT_MACH_MEM_LIMIT_USED_BASE, _pressureHigh);
        } 
 
        override internal void ReadConfig(CacheSection cacheSection) {
            // Read the percentagePhysicalMemoryUsedLimit set in config 
            int limit = cacheSection.PercentagePhysicalMemoryUsedLimit;
            if (limit == 0) {
                // use defaults
                return; 
            }
 
            _pressureHigh = Math.Max(3, limit); 
            _pressureMiddle = Math.Max(2, _pressureHigh - 2);
            _pressureLow = Math.Max(1, _pressureHigh - 9); 

            // PerfCounter: Cache Percentage Machine Memory Limit Used
            //    = total physical memory used / total physical memory used limit
            PerfCounters.SetCounter(AppPerfCounter.CACHE_PERCENT_MACH_MEM_LIMIT_USED_BASE, _pressureHigh); 

            Debug.Trace("CacheMemory", "CacheMemoryTotalMemoryPressure.ReadConfig: _pressureHigh=" + _pressureHigh + 
                        ", _pressureMiddle=" + _pressureMiddle + ", _pressureLow=" + _pressureLow); 
        }
 
        override protected int GetCurrentPressure() {
            UnsafeNativeMethods.MEMORYSTATUSEX  memoryStatusEx = new UnsafeNativeMethods.MEMORYSTATUSEX();
            memoryStatusEx.Init();
            if (UnsafeNativeMethods.GlobalMemoryStatusEx(ref memoryStatusEx) == 0) 
                return 0;
 
            int memoryLoad = memoryStatusEx.dwMemoryLoad; 
            if (_pressureHigh != 0) {
                // PerfCounter: Cache Percentage Machine Memory Limit Used 
                //    = total physical memory used / total physical memory used limit
                PerfCounters.SetCounter(AppPerfCounter.CACHE_MACH_MEM_USED, memoryLoad);
            }
 
            return memoryLoad;
        } 
 

        // Returns the percentage of physical machine memory that can be consumed by an 
        // application before ASP.NET starts forcibly removing items from the cache.
        internal long MemoryLimit {
            get { return _pressureHigh; }
        } 
    }
 
    // Make sure we don't hit the per-process private bytes memory limit, 
    // or the process will be restarted
    sealed class CacheMemoryPrivateBytesPressure : CacheMemoryPressure { 
        long        _memoryLimit;

        static bool s_isIIS6 = HostingEnvironment.IsUnderIIS6Process;
        static long s_autoPrivateBytesLimit = -1; 
        static long s_lastReadPrivateBytes;
        static DateTime s_lastTimeReadPrivateBytes = DateTime.MinValue; 
        static uint s_pid = 0; 
        static int  s_pollInterval;
 
        const long  PRIVATE_BYTES_LIMIT_2GB = 800 * MEGABYTE;
        const long  PRIVATE_BYTES_LIMIT_3GB = 1800 * MEGABYTE;
        const long  PRIVATE_BYTES_LIMIT_64BIT = 1L * TERABYTE;
 
        DateTime    _startupTime;
 
        long        _pressureHighMemoryLimit; // high pressure in bytes = _pressureHigh * _memoryLimit / 100 

        internal CacheMemoryPrivateBytesPressure() { 
            _pressureHigh = 99;
            _pressureMiddle = 98;
            _pressureLow = 97;
 
            _startupTime = DateTime.UtcNow;
 
            InitHistory(); 

        } 

        // Auto-generate the private bytes limit:
        // - On 64bit, the auto value is MIN(60% physical_ram, 1 TB)
        // - On x86, for 2GB, the auto value is MIN(60% physical_ram, 800 MB) 
        // - On x86, for 3GB, the auto value is MIN(60% physical_ram, 1800 MB)
        // 
        // - If it's not a hosted environment (e.g. console app), the 60% in the above 
        //   formulas will become 100% because in un-hosted environment we don't launch
        //   other processes such as compiler, etc. 
        internal static long AutoPrivateBytesLimit {
            get {
                if (s_autoPrivateBytesLimit == -1) {
 
                    bool    is64bit = (IntPtr.Size == 8);
 
                    long totalPhysical = TotalPhysical; 
                    long totalVirtual = TotalVirtual;
                    if (totalPhysical != 0) { 
                        long    recommendedPrivateByteLimit;
                        if (is64bit) {
                            recommendedPrivateByteLimit = PRIVATE_BYTES_LIMIT_64BIT;
                        } 
                        else {
                            // Figure out if it's 2GB or 3GB 
 
                            if (totalVirtual > 2 * GIGABYTE) {
                                recommendedPrivateByteLimit = PRIVATE_BYTES_LIMIT_3GB; 
                            }
                            else {
                                recommendedPrivateByteLimit = PRIVATE_BYTES_LIMIT_2GB;
                            } 
                        }
 
                        // if we're hosted, use 60% of physical RAM; otherwise 100% 
                        long usableMemory = HostingEnvironment.IsHosted ? totalPhysical * 3 / 5 : totalPhysical;
                        s_autoPrivateBytesLimit = Math.Min(usableMemory, recommendedPrivateByteLimit); 
                    }
                    else {
                        // If GlobalMemoryStatusEx fails, we'll use these as our auto-gen private bytes limit
                        s_autoPrivateBytesLimit = is64bit ? PRIVATE_BYTES_LIMIT_64BIT : PRIVATE_BYTES_LIMIT_2GB; 
                    }
                } 
 
                return s_autoPrivateBytesLimit;
            } 
        }

        override internal void ReadConfig(CacheSection cacheSection) {
            // Read the private bytes limit set in config 
            long    privateBytesLimit;
            privateBytesLimit = cacheSection.PrivateBytesLimit; 
 
            // per-process information
            if (UnsafeNativeMethods.GetModuleHandle(ModName.WP_FULL_NAME) != IntPtr.Zero) { 
                _memoryLimit = (long)UnsafeNativeMethods.PMGetMemoryLimitInMB() << MEGABYTE_SHIFT;
            }
            else if (UnsafeNativeMethods.GetModuleHandle(ModName.W3WP_FULL_NAME) != IntPtr.Zero) {
                IServerConfig serverConfig = ServerConfig.GetInstance(); 
                _memoryLimit = (long)serverConfig.GetW3WPMemoryLimitInKB() << KILOBYTE_SHIFT;
            } 
 
            // VSWhidbey 546381: never override what the user specifies as the limit;
            // only call AutoPrivateBytesLimit when the user does not specify one. 
            if (privateBytesLimit == 0 && _memoryLimit == 0) {
                // Zero means we impose a limit
                _memoryLimit = AutoPrivateBytesLimit;
            } 
            else if (privateBytesLimit != 0 && _memoryLimit != 0) {
                // Take the min of "process recycle limit" and our internal "private bytes limit" 
                _memoryLimit = Math.Min(_memoryLimit, privateBytesLimit); 
            }
            else if (privateBytesLimit != 0) { 
                // _memoryLimit is 0, but privateBytesLimit is non-zero, so use it as the limit
                _memoryLimit = privateBytesLimit;
            }
 
            Debug.Trace("CacheMemory", "CacheMemoryPrivateBytesPressure.ReadConfig: _memoryLimit=" + (_memoryLimit >> MEGABYTE_SHIFT) + "Mb");
 
            if (_memoryLimit > 0) { 

                if (s_pid == 0) // only set this once 
                    s_pid = (uint) SafeNativeMethods.GetCurrentProcessId();

                if (_memoryLimit >= 256 * MEGABYTE) {
                    // we leave arbitrary breathing room 
                    //
                    _pressureHigh = (int)Math.Max(90, (_memoryLimit - (96 * MEGABYTE)) * 100 / _memoryLimit); 
                    _pressureLow = (int)Math.Max(80, (_memoryLimit - (224 * MEGABYTE)) * 100 / _memoryLimit); 
                    _pressureMiddle = (_pressureHigh + _pressureLow) / 2;
                } 
                else {
                    // if memory limit is small, use these hard coded values
                    _pressureHigh = 90;
                    _pressureMiddle = 85; 
                    _pressureLow = 78;
                } 
                _pressureHighMemoryLimit = _pressureHigh * _memoryLimit / 100; 
            }
 
            // convert <cache privateBytesPollTime/> to milliseconds
            s_pollInterval = (int)Math.Min(cacheSection.PrivateBytesPollTime.TotalMilliseconds, (double)Int32.MaxValue);

            // PerfCounter: Cache Percentage Process Memory Limit Used 
            //    = memory used by this process / process memory limit at pressureHigh
 
            // Set private bytes limit in kilobytes becuase the counter is a DWORD 
            PerfCounters.SetCounter(AppPerfCounter.CACHE_PERCENT_PROC_MEM_LIMIT_USED_BASE, (int)(_pressureHighMemoryLimit >> KILOBYTE_SHIFT));
 
            Debug.Trace("CacheMemory", "CacheMemoryPrivateBytesPressure.ReadConfig: _pressureHigh=" + _pressureHigh +
                        ", _pressureMiddle=" + _pressureMiddle + ", _pressureLow=" + _pressureLow);
        }
 
        internal long MemoryLimit {
            get { return _memoryLimit; } 
        } 

        internal static int PollInterval { 
            get { return s_pollInterval; }
        }

        // Get Private Bytes if we have not updated it within the poll interval, otherwise 
        // use the cached value.
        internal static long GetPrivateBytes(bool nocache) { 
            long privateBytes; 

            int hr = 0; 
            // NtQuerySystemInformation is a very expensive call. A new function
            // exists on XP Pro and later versions of the OS and it performs much
            // better. The name of that function is GetProcessMemoryInfo. For hosting
            // scenarios where a larger number of w3wp.exe instances are running, we 
            // want to use the new API (VSWhidbey 417366).
            if (s_isIIS6) { 
                long privatePageCount; 
                hr = UnsafeNativeMethods.GetPrivateBytesIIS6(out privatePageCount, nocache);
                privateBytes = privatePageCount; 
            }
            else {
                uint    dummy;
                uint    privatePageCount = 0; 
                hr = UnsafeNativeMethods.GetProcessMemoryInformation(s_pid, out privatePageCount, out dummy, nocache);
                privateBytes = (long)privatePageCount << MEGABYTE_SHIFT; 
            } 

            // for debugging, store value and time that we last got private bytes 
            if (hr == 0) {
                s_lastReadPrivateBytes = privateBytes;
                s_lastTimeReadPrivateBytes = DateTime.UtcNow;
            } 

            Debug.Trace("CacheMemory", "GetPrivateBytes: hr=" + hr + ",privateBytes=" + (privateBytes >> MEGABYTE_SHIFT) + "Mb"); 
            return privateBytes; 
        }
 
        override protected int GetCurrentPressure() {
            if (_memoryLimit == 0) {
                return 0;
            } 
            long privateBytes =  GetPrivateBytes(false);
 
            if (_pressureHighMemoryLimit != 0) { 
                // PerfCounter: Cache Percentage Process Memory Limit Used
                //    = memory used by this process / process memory limit at pressureHigh 
                // Set private bytes used in kilobytes because the counter is a DWORD
                PerfCounters.SetCounter(AppPerfCounter.CACHE_PROC_MEM_USED, (int)(privateBytes >> KILOBYTE_SHIFT));
            }
 
            int result = (int)(privateBytes * 100 / _memoryLimit);
            return result; 
        } 

        internal long PressureHighMemoryLimit { 
            get {
                return _pressureHighMemoryLimit;
            }
        } 

        internal bool HasLimit() { 
            return _memoryLimit != 0; 
        }
 
    }

    class CacheMemoryStats {
        CacheMemoryTotalMemoryPressure  _pressureTotalMemory; 
        CacheMemoryPrivateBytesPressure _pressurePrivateBytes;
        long _minTotalMemoryChange = -1; // if a collection didn't reduce the heaps by this amount, it was ineffective 
        long _lastTotalMemoryChange;     // updated after each collection to track effectiveness of collections 

        internal CacheMemoryStats() { 
            CacheMemoryPressure.GetMemoryStatusOnce();
            _pressureTotalMemory = new CacheMemoryTotalMemoryPressure();
            _pressurePrivateBytes = new CacheMemoryPrivateBytesPressure();
        } 

        internal CacheMemoryPrivateBytesPressure PrivateBytesPressure { 
            get {return _pressurePrivateBytes;} 
        }
 
        internal CacheMemoryTotalMemoryPressure TotalMemoryPressure {
            get {return _pressureTotalMemory;}
        }
 
        internal bool IsGcCollectIneffective(long totalMemoryChange) {
            if (_minTotalMemoryChange == -1 && _pressurePrivateBytes.HasLimit()) { // need to initialize 
                    // 1% of memory limit 
                    _minTotalMemoryChange = _pressurePrivateBytes.MemoryLimit / 100;
            } 

            // store this to assist debugging
            _lastTotalMemoryChange = totalMemoryChange;
 
            return (totalMemoryChange < _minTotalMemoryChange);
        } 
 
        internal bool IsAboveHighPressure() {
            return _pressureTotalMemory.IsAboveHighPressure() || _pressurePrivateBytes.IsAboveHighPressure(); 
        }

        internal bool IsAboveMediumPressure() {
            return _pressureTotalMemory.IsAboveMediumPressure() || _pressurePrivateBytes.IsAboveMediumPressure(); 
        }
 
        internal void ReadConfig(CacheSection cacheSection) { 
            _pressureTotalMemory.ReadConfig(cacheSection);
            _pressurePrivateBytes.ReadConfig(cacheSection); 
        }

        internal void Update() {
            _pressureTotalMemory.Update(); 
            _pressurePrivateBytes.Update();
        } 
    } 
}
 
//------------------------------------------------------------------------------ 
// <copyright file="CacheMemory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Caching { 
    using System.Web.Configuration; 
    using System.Runtime.InteropServices;
    using System.Web.Util; 
    using System.Web;
    using System.Web.Hosting;

    abstract class CacheMemoryPressure { 
        protected const int     TERABYTE_SHIFT = 40;
        protected const long    TERABYTE = 1L << TERABYTE_SHIFT; 
 
        protected const int     GIGABYTE_SHIFT = 30;
        protected const long    GIGABYTE = 1L << GIGABYTE_SHIFT; 

        protected const int     MEGABYTE_SHIFT = 20;
        protected const long    MEGABYTE = 1L << MEGABYTE_SHIFT; // 1048576
 
        protected const int     KILOBYTE_SHIFT = 10;
        protected const long    KILOBYTE = 1L << KILOBYTE_SHIFT; // 1024 
 
        protected const int     HISTORY_COUNT = 6;
 
        protected int           _pressureHigh;      // high pressure level
        protected int           _pressureMiddle;    // middle pressure level - target
        protected int           _pressureLow;       // low pressure level - slow growth here
 
        protected int           _i0;
        protected int[]         _pressureHist; 
        protected int           _pressureTotal; 
        protected int           _pressureAvg;
 
        private static long     s_totalPhysical;
        private static long     s_totalVirtual;

        internal static void GetMemoryStatusOnce() { 
            UnsafeNativeMethods.MEMORYSTATUSEX  memoryStatusEx = new UnsafeNativeMethods.MEMORYSTATUSEX();
            memoryStatusEx.Init(); 
            if (UnsafeNativeMethods.GlobalMemoryStatusEx(ref memoryStatusEx) != 0) { 
                s_totalPhysical = memoryStatusEx.ullTotalPhys;
                s_totalVirtual = memoryStatusEx.ullTotalVirtual; 
            }
        }

        internal static long TotalPhysical { get { return s_totalPhysical; } } 
        internal static long TotalVirtual { get { return s_totalVirtual; } }
 
        protected abstract int GetCurrentPressure(); 

        internal virtual void ReadConfig(CacheSection cacheSection) {} 

        protected void InitHistory() {
            Debug.Assert(_pressureHigh > 0, "_pressureHigh > 0");
            Debug.Assert(_pressureLow > 0, "_pressureLow > 0"); 
            Debug.Assert(_pressureLow <= _pressureHigh, "_pressureLow <= _pressureHigh");
 
            int pressure = GetCurrentPressure(); 

            _pressureHist = new int[HISTORY_COUNT]; 
            for (int i = 0; i < HISTORY_COUNT; i++) {
                _pressureHist[i] = pressure;
                _pressureTotal +=  pressure;
            } 

            _pressureAvg = pressure; 
        } 

        // Get current pressure and update history 
        internal void Update() {
            int pressure = GetCurrentPressure();

 
#if FEATURE_PAL // _pressureHist may not be initialized if calls to
                // GlobalMemoryStatusEx fail. 
            if (_pressureHist != null) 
#endif // FEATURE_PAL
            { 

                _i0 = (_i0 + 1) % HISTORY_COUNT;

                _pressureTotal -= _pressureHist[_i0]; 
                _pressureTotal += pressure;
                _pressureHist[_i0] = pressure; 
                _pressureAvg = _pressureTotal / HISTORY_COUNT; 
            }
#if DBG 
            if (HttpRuntime.AppDomainAppIdInternal != null && HttpRuntime.AppDomainAppIdInternal.Length > 0) {
                if (!(pressure == 0 && PressureHigh == 99)) {
                    Debug.Trace("CacheMemory", this.GetType().Name + ".Update: last=" + pressure + ",avg=" + PressureAvg + ",high=" + PressureHigh + ",low=" + PressureLow + ",middle=" + PressureMiddle + " " + Debug.FormatLocalDate(DateTime.Now));
                } 
            }
#endif 
        } 

        internal int PressureLast { 
            get {

#if FEATURE_PAL // _pressureHist may not be initialized if calls to
                // GlobalMemoryStatusEx fail. 
                return (_pressureHist != null) ? _pressureHist[_i0] : 0;
#else // FEATURE_PAL 
                return _pressureHist[_i0]; 
#endif // FEATURE_PAL
 
            }

        }
 
        internal int PressureAvg {
            get { return _pressureAvg; } 
        } 

        internal int PressureHigh { 
            get { return _pressureHigh; }
        }

        internal int PressureLow { 
            get { return _pressureLow; }
        } 
 
        internal int PressureMiddle {
            get { return _pressureMiddle; } 
        }

        internal bool IsAboveHighPressure() {
            return PressureLast >= PressureHigh; 
        }
 
        internal bool IsAboveMediumPressure() { 
            return PressureLast > PressureMiddle;
        } 
    }

    // The GC aggressively collects when it receives a low memory notification. Make sure
    //  we have released references before it aggressively collects. 
    sealed class CacheMemoryTotalMemoryPressure : CacheMemoryPressure {
 
        internal CacheMemoryTotalMemoryPressure() { 
            /*
              The chart below shows physical memory in megabytes, and the 1, 3, and 10% values. 
              When we reach "middle" pressure, we begin trimming the cache.

              RAM     1%      3%      10%
              ----------------------------- 
              128     1.28    3.84    12.8
              256     2.56    7.68    25.6 
              512     5.12    15.36   51.2 
              1024    10.24   30.72   102.4
              2048    20.48   61.44   204.8 
              4096    40.96   122.88  409.6
              8192    81.92   245.76  819.2

              Low memory notifications from CreateMemoryResourceNotification are calculated as follows 
              (.\base\ntos\mm\initsup.c):
 
              MiInitializeMemoryEvents() { 
              ...
              // 
              // Scale the threshold so on servers the low threshold is
              // approximately 32MB per 4GB, capping it at 64MB.
              //
 
              MmLowMemoryThreshold = MmPlentyFreePages;
 
              if (MmNumberOfPhysicalPages > 0x40000) { 
                  MmLowMemoryThreshold = MI_MB_TO_PAGES (32);
                  MmLowMemoryThreshold += ((MmNumberOfPhysicalPages - 0x40000) >> 7); 
              }
              else if (MmNumberOfPhysicalPages > 0x8000) {
                  MmLowMemoryThreshold += ((MmNumberOfPhysicalPages - 0x8000) >> 5);
              } 

              if (MmLowMemoryThreshold > MI_MB_TO_PAGES (64)) { 
                  MmLowMemoryThreshold = MI_MB_TO_PAGES (64); 
              }
              ... 

              E.g.

              RAM(mb) low      % 
              -------------------
              256	  20	  92% 
              512	  24	  95% 
              768	  28	  96%
              1024	  32	  97% 
              2048	  40	  98%
              3072	  48	  98%
              4096	  56	  99%
              5120	  64	  99% 
            */
 
            long memory = TotalPhysical; 
            Debug.Assert(memory != 0, "memory != 0");
            if (memory >= 0x100000000) { 
                _pressureHigh = 99;
            }
            else if (memory >= 0x80000000) {
                _pressureHigh = 98; 
            }
            else if (memory >= 0x40000000) { 
                _pressureHigh = 97; 
            }
            else if (memory >= 0x30000000) { 
                _pressureHigh = 96;
            }
            else {
                _pressureHigh = 95; 
            }
 
            _pressureMiddle = _pressureHigh - 2; 
            _pressureLow = _pressureHigh - 9;
 
            InitHistory();

            // PerfCounter: Cache Percentage Machine Memory Limit Used
            //    = total physical memory used / total physical memory used limit 
            PerfCounters.SetCounter(AppPerfCounter.CACHE_PERCENT_MACH_MEM_LIMIT_USED_BASE, _pressureHigh);
        } 
 
        override internal void ReadConfig(CacheSection cacheSection) {
            // Read the percentagePhysicalMemoryUsedLimit set in config 
            int limit = cacheSection.PercentagePhysicalMemoryUsedLimit;
            if (limit == 0) {
                // use defaults
                return; 
            }
 
            _pressureHigh = Math.Max(3, limit); 
            _pressureMiddle = Math.Max(2, _pressureHigh - 2);
            _pressureLow = Math.Max(1, _pressureHigh - 9); 

            // PerfCounter: Cache Percentage Machine Memory Limit Used
            //    = total physical memory used / total physical memory used limit
            PerfCounters.SetCounter(AppPerfCounter.CACHE_PERCENT_MACH_MEM_LIMIT_USED_BASE, _pressureHigh); 

            Debug.Trace("CacheMemory", "CacheMemoryTotalMemoryPressure.ReadConfig: _pressureHigh=" + _pressureHigh + 
                        ", _pressureMiddle=" + _pressureMiddle + ", _pressureLow=" + _pressureLow); 
        }
 
        override protected int GetCurrentPressure() {
            UnsafeNativeMethods.MEMORYSTATUSEX  memoryStatusEx = new UnsafeNativeMethods.MEMORYSTATUSEX();
            memoryStatusEx.Init();
            if (UnsafeNativeMethods.GlobalMemoryStatusEx(ref memoryStatusEx) == 0) 
                return 0;
 
            int memoryLoad = memoryStatusEx.dwMemoryLoad; 
            if (_pressureHigh != 0) {
                // PerfCounter: Cache Percentage Machine Memory Limit Used 
                //    = total physical memory used / total physical memory used limit
                PerfCounters.SetCounter(AppPerfCounter.CACHE_MACH_MEM_USED, memoryLoad);
            }
 
            return memoryLoad;
        } 
 

        // Returns the percentage of physical machine memory that can be consumed by an 
        // application before ASP.NET starts forcibly removing items from the cache.
        internal long MemoryLimit {
            get { return _pressureHigh; }
        } 
    }
 
    // Make sure we don't hit the per-process private bytes memory limit, 
    // or the process will be restarted
    sealed class CacheMemoryPrivateBytesPressure : CacheMemoryPressure { 
        long        _memoryLimit;

        static bool s_isIIS6 = HostingEnvironment.IsUnderIIS6Process;
        static long s_autoPrivateBytesLimit = -1; 
        static long s_lastReadPrivateBytes;
        static DateTime s_lastTimeReadPrivateBytes = DateTime.MinValue; 
        static uint s_pid = 0; 
        static int  s_pollInterval;
 
        const long  PRIVATE_BYTES_LIMIT_2GB = 800 * MEGABYTE;
        const long  PRIVATE_BYTES_LIMIT_3GB = 1800 * MEGABYTE;
        const long  PRIVATE_BYTES_LIMIT_64BIT = 1L * TERABYTE;
 
        DateTime    _startupTime;
 
        long        _pressureHighMemoryLimit; // high pressure in bytes = _pressureHigh * _memoryLimit / 100 

        internal CacheMemoryPrivateBytesPressure() { 
            _pressureHigh = 99;
            _pressureMiddle = 98;
            _pressureLow = 97;
 
            _startupTime = DateTime.UtcNow;
 
            InitHistory(); 

        } 

        // Auto-generate the private bytes limit:
        // - On 64bit, the auto value is MIN(60% physical_ram, 1 TB)
        // - On x86, for 2GB, the auto value is MIN(60% physical_ram, 800 MB) 
        // - On x86, for 3GB, the auto value is MIN(60% physical_ram, 1800 MB)
        // 
        // - If it's not a hosted environment (e.g. console app), the 60% in the above 
        //   formulas will become 100% because in un-hosted environment we don't launch
        //   other processes such as compiler, etc. 
        internal static long AutoPrivateBytesLimit {
            get {
                if (s_autoPrivateBytesLimit == -1) {
 
                    bool    is64bit = (IntPtr.Size == 8);
 
                    long totalPhysical = TotalPhysical; 
                    long totalVirtual = TotalVirtual;
                    if (totalPhysical != 0) { 
                        long    recommendedPrivateByteLimit;
                        if (is64bit) {
                            recommendedPrivateByteLimit = PRIVATE_BYTES_LIMIT_64BIT;
                        } 
                        else {
                            // Figure out if it's 2GB or 3GB 
 
                            if (totalVirtual > 2 * GIGABYTE) {
                                recommendedPrivateByteLimit = PRIVATE_BYTES_LIMIT_3GB; 
                            }
                            else {
                                recommendedPrivateByteLimit = PRIVATE_BYTES_LIMIT_2GB;
                            } 
                        }
 
                        // if we're hosted, use 60% of physical RAM; otherwise 100% 
                        long usableMemory = HostingEnvironment.IsHosted ? totalPhysical * 3 / 5 : totalPhysical;
                        s_autoPrivateBytesLimit = Math.Min(usableMemory, recommendedPrivateByteLimit); 
                    }
                    else {
                        // If GlobalMemoryStatusEx fails, we'll use these as our auto-gen private bytes limit
                        s_autoPrivateBytesLimit = is64bit ? PRIVATE_BYTES_LIMIT_64BIT : PRIVATE_BYTES_LIMIT_2GB; 
                    }
                } 
 
                return s_autoPrivateBytesLimit;
            } 
        }

        override internal void ReadConfig(CacheSection cacheSection) {
            // Read the private bytes limit set in config 
            long    privateBytesLimit;
            privateBytesLimit = cacheSection.PrivateBytesLimit; 
 
            // per-process information
            if (UnsafeNativeMethods.GetModuleHandle(ModName.WP_FULL_NAME) != IntPtr.Zero) { 
                _memoryLimit = (long)UnsafeNativeMethods.PMGetMemoryLimitInMB() << MEGABYTE_SHIFT;
            }
            else if (UnsafeNativeMethods.GetModuleHandle(ModName.W3WP_FULL_NAME) != IntPtr.Zero) {
                IServerConfig serverConfig = ServerConfig.GetInstance(); 
                _memoryLimit = (long)serverConfig.GetW3WPMemoryLimitInKB() << KILOBYTE_SHIFT;
            } 
 
            // VSWhidbey 546381: never override what the user specifies as the limit;
            // only call AutoPrivateBytesLimit when the user does not specify one. 
            if (privateBytesLimit == 0 && _memoryLimit == 0) {
                // Zero means we impose a limit
                _memoryLimit = AutoPrivateBytesLimit;
            } 
            else if (privateBytesLimit != 0 && _memoryLimit != 0) {
                // Take the min of "process recycle limit" and our internal "private bytes limit" 
                _memoryLimit = Math.Min(_memoryLimit, privateBytesLimit); 
            }
            else if (privateBytesLimit != 0) { 
                // _memoryLimit is 0, but privateBytesLimit is non-zero, so use it as the limit
                _memoryLimit = privateBytesLimit;
            }
 
            Debug.Trace("CacheMemory", "CacheMemoryPrivateBytesPressure.ReadConfig: _memoryLimit=" + (_memoryLimit >> MEGABYTE_SHIFT) + "Mb");
 
            if (_memoryLimit > 0) { 

                if (s_pid == 0) // only set this once 
                    s_pid = (uint) SafeNativeMethods.GetCurrentProcessId();

                if (_memoryLimit >= 256 * MEGABYTE) {
                    // we leave arbitrary breathing room 
                    //
                    _pressureHigh = (int)Math.Max(90, (_memoryLimit - (96 * MEGABYTE)) * 100 / _memoryLimit); 
                    _pressureLow = (int)Math.Max(80, (_memoryLimit - (224 * MEGABYTE)) * 100 / _memoryLimit); 
                    _pressureMiddle = (_pressureHigh + _pressureLow) / 2;
                } 
                else {
                    // if memory limit is small, use these hard coded values
                    _pressureHigh = 90;
                    _pressureMiddle = 85; 
                    _pressureLow = 78;
                } 
                _pressureHighMemoryLimit = _pressureHigh * _memoryLimit / 100; 
            }
 
            // convert <cache privateBytesPollTime/> to milliseconds
            s_pollInterval = (int)Math.Min(cacheSection.PrivateBytesPollTime.TotalMilliseconds, (double)Int32.MaxValue);

            // PerfCounter: Cache Percentage Process Memory Limit Used 
            //    = memory used by this process / process memory limit at pressureHigh
 
            // Set private bytes limit in kilobytes becuase the counter is a DWORD 
            PerfCounters.SetCounter(AppPerfCounter.CACHE_PERCENT_PROC_MEM_LIMIT_USED_BASE, (int)(_pressureHighMemoryLimit >> KILOBYTE_SHIFT));
 
            Debug.Trace("CacheMemory", "CacheMemoryPrivateBytesPressure.ReadConfig: _pressureHigh=" + _pressureHigh +
                        ", _pressureMiddle=" + _pressureMiddle + ", _pressureLow=" + _pressureLow);
        }
 
        internal long MemoryLimit {
            get { return _memoryLimit; } 
        } 

        internal static int PollInterval { 
            get { return s_pollInterval; }
        }

        // Get Private Bytes if we have not updated it within the poll interval, otherwise 
        // use the cached value.
        internal static long GetPrivateBytes(bool nocache) { 
            long privateBytes; 

            int hr = 0; 
            // NtQuerySystemInformation is a very expensive call. A new function
            // exists on XP Pro and later versions of the OS and it performs much
            // better. The name of that function is GetProcessMemoryInfo. For hosting
            // scenarios where a larger number of w3wp.exe instances are running, we 
            // want to use the new API (VSWhidbey 417366).
            if (s_isIIS6) { 
                long privatePageCount; 
                hr = UnsafeNativeMethods.GetPrivateBytesIIS6(out privatePageCount, nocache);
                privateBytes = privatePageCount; 
            }
            else {
                uint    dummy;
                uint    privatePageCount = 0; 
                hr = UnsafeNativeMethods.GetProcessMemoryInformation(s_pid, out privatePageCount, out dummy, nocache);
                privateBytes = (long)privatePageCount << MEGABYTE_SHIFT; 
            } 

            // for debugging, store value and time that we last got private bytes 
            if (hr == 0) {
                s_lastReadPrivateBytes = privateBytes;
                s_lastTimeReadPrivateBytes = DateTime.UtcNow;
            } 

            Debug.Trace("CacheMemory", "GetPrivateBytes: hr=" + hr + ",privateBytes=" + (privateBytes >> MEGABYTE_SHIFT) + "Mb"); 
            return privateBytes; 
        }
 
        override protected int GetCurrentPressure() {
            if (_memoryLimit == 0) {
                return 0;
            } 
            long privateBytes =  GetPrivateBytes(false);
 
            if (_pressureHighMemoryLimit != 0) { 
                // PerfCounter: Cache Percentage Process Memory Limit Used
                //    = memory used by this process / process memory limit at pressureHigh 
                // Set private bytes used in kilobytes because the counter is a DWORD
                PerfCounters.SetCounter(AppPerfCounter.CACHE_PROC_MEM_USED, (int)(privateBytes >> KILOBYTE_SHIFT));
            }
 
            int result = (int)(privateBytes * 100 / _memoryLimit);
            return result; 
        } 

        internal long PressureHighMemoryLimit { 
            get {
                return _pressureHighMemoryLimit;
            }
        } 

        internal bool HasLimit() { 
            return _memoryLimit != 0; 
        }
 
    }

    class CacheMemoryStats {
        CacheMemoryTotalMemoryPressure  _pressureTotalMemory; 
        CacheMemoryPrivateBytesPressure _pressurePrivateBytes;
        long _minTotalMemoryChange = -1; // if a collection didn't reduce the heaps by this amount, it was ineffective 
        long _lastTotalMemoryChange;     // updated after each collection to track effectiveness of collections 

        internal CacheMemoryStats() { 
            CacheMemoryPressure.GetMemoryStatusOnce();
            _pressureTotalMemory = new CacheMemoryTotalMemoryPressure();
            _pressurePrivateBytes = new CacheMemoryPrivateBytesPressure();
        } 

        internal CacheMemoryPrivateBytesPressure PrivateBytesPressure { 
            get {return _pressurePrivateBytes;} 
        }
 
        internal CacheMemoryTotalMemoryPressure TotalMemoryPressure {
            get {return _pressureTotalMemory;}
        }
 
        internal bool IsGcCollectIneffective(long totalMemoryChange) {
            if (_minTotalMemoryChange == -1 && _pressurePrivateBytes.HasLimit()) { // need to initialize 
                    // 1% of memory limit 
                    _minTotalMemoryChange = _pressurePrivateBytes.MemoryLimit / 100;
            } 

            // store this to assist debugging
            _lastTotalMemoryChange = totalMemoryChange;
 
            return (totalMemoryChange < _minTotalMemoryChange);
        } 
 
        internal bool IsAboveHighPressure() {
            return _pressureTotalMemory.IsAboveHighPressure() || _pressurePrivateBytes.IsAboveHighPressure(); 
        }

        internal bool IsAboveMediumPressure() {
            return _pressureTotalMemory.IsAboveMediumPressure() || _pressurePrivateBytes.IsAboveMediumPressure(); 
        }
 
        internal void ReadConfig(CacheSection cacheSection) { 
            _pressureTotalMemory.ReadConfig(cacheSection);
            _pressurePrivateBytes.ReadConfig(cacheSection); 
        }

        internal void Update() {
            _pressureTotalMemory.Update(); 
            _pressurePrivateBytes.Update();
        } 
    } 
}
 
