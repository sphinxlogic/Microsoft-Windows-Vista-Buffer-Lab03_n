// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

using System.Runtime.CompilerServices; 
 
namespace System
{ 
#if MDA_SUPPORTED
    internal static class Mda
    {
        private enum MdaState { 
            Unknown = 0,
            Enabled = 1, 
            Disabled = 2 
        }
 
        private static MdaState _streamWriterMDAState = MdaState.Unknown;

        internal static bool StreamWriterBufferMDAEnabled {
            get { 
                if (_streamWriterMDAState == 0) {
                    if (IsStreamWriterBufferedDataLostEnabled()) 
                        _streamWriterMDAState = MdaState.Enabled; 
                    else
                        _streamWriterMDAState = MdaState.Disabled; 
                }

                return _streamWriterMDAState == MdaState.Enabled;
            } 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void MemberInfoCacheCreation();
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void DateTimeInvalidLocalFormat();

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void StreamWriterBufferedDataLost(String text);
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern bool IsStreamWriterBufferedDataLostEnabled();
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsInvalidGCHandleCookieProbeEnabled();

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void FireInvalidGCHandleCookieProbe(IntPtr cookie);
    } 
#endif 
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

using System.Runtime.CompilerServices; 
 
namespace System
{ 
#if MDA_SUPPORTED
    internal static class Mda
    {
        private enum MdaState { 
            Unknown = 0,
            Enabled = 1, 
            Disabled = 2 
        }
 
        private static MdaState _streamWriterMDAState = MdaState.Unknown;

        internal static bool StreamWriterBufferMDAEnabled {
            get { 
                if (_streamWriterMDAState == 0) {
                    if (IsStreamWriterBufferedDataLostEnabled()) 
                        _streamWriterMDAState = MdaState.Enabled; 
                    else
                        _streamWriterMDAState = MdaState.Disabled; 
                }

                return _streamWriterMDAState == MdaState.Enabled;
            } 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void MemberInfoCacheCreation();
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void DateTimeInvalidLocalFormat();

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void StreamWriterBufferedDataLost(String text);
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern bool IsStreamWriterBufferedDataLostEnabled();
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsInvalidGCHandleCookieProbeEnabled();

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        internal static extern void FireInvalidGCHandleCookieProbe(IntPtr cookie);
    } 
#endif 
}
