namespace System.Runtime { 
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Security.Permissions; 

    // This is the same format as in clr\src\vm\gcpriv.h 
    // make sure you change that one if you change this one! 
    [Serializable]
    public enum GCLatencyMode 
    {
        Batch = 0,
        Interactive = 1,
        LowLatency = 2 
    }
 
    public static class GCSettings 
    {
        public static GCLatencyMode LatencyMode 
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            { 
                return (GCLatencyMode)(GC.nativeGetGCLatencyMode());
            } 
 
            // We don't want to allow this API when hosted.
            [HostProtection(MayLeakOnAbort = true)] 
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [PermissionSetAttribute(SecurityAction.LinkDemand, Name="FullTrust")]
            set
            { 
                if ((value < GCLatencyMode.Batch) || (value > GCLatencyMode.LowLatency))
                { 
                    throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_Enum")); 
                }
 
                GC.nativeSetGCLatencyMode((int)value);
            }
        }
 
        public static bool IsServerGC
        { 
            get { 
                return GC.nativeIsServerGC();
            } 
        }
    }
}
namespace System.Runtime { 
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Security.Permissions; 

    // This is the same format as in clr\src\vm\gcpriv.h 
    // make sure you change that one if you change this one! 
    [Serializable]
    public enum GCLatencyMode 
    {
        Batch = 0,
        Interactive = 1,
        LowLatency = 2 
    }
 
    public static class GCSettings 
    {
        public static GCLatencyMode LatencyMode 
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            { 
                return (GCLatencyMode)(GC.nativeGetGCLatencyMode());
            } 
 
            // We don't want to allow this API when hosted.
            [HostProtection(MayLeakOnAbort = true)] 
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [PermissionSetAttribute(SecurityAction.LinkDemand, Name="FullTrust")]
            set
            { 
                if ((value < GCLatencyMode.Batch) || (value > GCLatencyMode.LowLatency))
                { 
                    throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_Enum")); 
                }
 
                GC.nativeSetGCLatencyMode((int)value);
            }
        }
 
        public static bool IsServerGC
        { 
            get { 
                return GC.nativeIsServerGC();
            } 
        }
    }
}
