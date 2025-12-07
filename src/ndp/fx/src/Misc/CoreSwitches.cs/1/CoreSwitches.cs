 
//------------------------------------------------------------------------------
// <copyright file="Component.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.ComponentModel { 
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 

    /// <internalonly/>
    // Shared between dlls
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
    internal static class CoreSwitches {
 
        private static BooleanSwitch perfTrack; 

        public static BooleanSwitch PerfTrack { 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            get {
                if (perfTrack == null) {
                    perfTrack  = new BooleanSwitch("PERFTRACK", "Debug performance critical sections."); 
                }
                return perfTrack; 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright file="Component.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.ComponentModel { 
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 

    /// <internalonly/>
    // Shared between dlls
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
    internal static class CoreSwitches {
 
        private static BooleanSwitch perfTrack; 

        public static BooleanSwitch PerfTrack { 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            get {
                if (perfTrack == null) {
                    perfTrack  = new BooleanSwitch("PERFTRACK", "Debug performance critical sections."); 
                }
                return perfTrack; 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
