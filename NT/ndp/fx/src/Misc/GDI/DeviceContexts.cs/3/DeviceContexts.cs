//------------------------------------------------------------------------------ 
// <copyright file="WindowsFont.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

#if WGCM_TEST_SUITE // Enable tracking when built for the test suites. 
#define TRACK_HDC 
#define GDI_FONT_CACHE_TRACK
#endif 

#if WINFORMS_NAMESPACE
namespace System.Windows.Forms.Internal
#elif DRAWING_NAMESPACE 
namespace System.Drawing.Internal
#else 
namespace System.Experimental.Gdi 
#endif
{ 
    using System;
    using System.Collections.Generic;
    using System.Internal;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Threading; 
    using Microsoft.Win32; 

    /// <devdoc> 
    ///     Keeps a cache of some graphics primitives.
    ///     Created to improve performance of TextRenderer.MeasureText methods that don't receive a WindowsGraphics.
    ///     This class mantains a cache of MRU WindowsFont objects in the process. (See VSWhidbey#301492).
    /// </devdoc> 
#if WINFORMS_PUBLIC_GRAPHICS_LIBRARY
    public 
#else 
    internal
#endif 
    static class DeviceContexts
    {
        [ThreadStatic]
        private static ClientUtils.WeakRefCollection activeDeviceContexts; 

 
        /// <devdoc> 
        /// WindowsGraphicsCacheManager needs to track DeviceContext
        /// objects so it can ask them if a font is in use before they 
        /// it's deleted.
        internal static void AddDeviceContext(DeviceContext dc) {
            if (activeDeviceContexts == null) {
                activeDeviceContexts = new ClientUtils.WeakRefCollection(); 
                activeDeviceContexts.RefCheckThreshold = 20; // See DevDiv#82664 (the repro app attached to this bug was used to determine this number).
            } 
 
            if (!activeDeviceContexts.Contains(dc)) {
                activeDeviceContexts.Add(dc); 
            }
        }

        internal static void RemoveDeviceContext(DeviceContext dc) { 
            if (activeDeviceContexts == null) {
                return; 
            } 
            activeDeviceContexts.RemoveByHashCode(dc);
        } 

 #if !DRAWING_NAMESPACE
        internal static bool IsFontInUse(WindowsFont wf) {
            if (wf == null) { 
                return false;
            } 
 
            for (int i = 0; i < activeDeviceContexts.Count; i++) {
                DeviceContext dc = activeDeviceContexts[i] as DeviceContext; 
                if (dc != null && (dc.ActiveFont == wf || dc.IsFontOnContextStack(wf))) {
                    return true;
                }
            } 

            return false; 
        } 
 #endif
 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="WindowsFont.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

#if WGCM_TEST_SUITE // Enable tracking when built for the test suites. 
#define TRACK_HDC 
#define GDI_FONT_CACHE_TRACK
#endif 

#if WINFORMS_NAMESPACE
namespace System.Windows.Forms.Internal
#elif DRAWING_NAMESPACE 
namespace System.Drawing.Internal
#else 
namespace System.Experimental.Gdi 
#endif
{ 
    using System;
    using System.Collections.Generic;
    using System.Internal;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Threading; 
    using Microsoft.Win32; 

    /// <devdoc> 
    ///     Keeps a cache of some graphics primitives.
    ///     Created to improve performance of TextRenderer.MeasureText methods that don't receive a WindowsGraphics.
    ///     This class mantains a cache of MRU WindowsFont objects in the process. (See VSWhidbey#301492).
    /// </devdoc> 
#if WINFORMS_PUBLIC_GRAPHICS_LIBRARY
    public 
#else 
    internal
#endif 
    static class DeviceContexts
    {
        [ThreadStatic]
        private static ClientUtils.WeakRefCollection activeDeviceContexts; 

 
        /// <devdoc> 
        /// WindowsGraphicsCacheManager needs to track DeviceContext
        /// objects so it can ask them if a font is in use before they 
        /// it's deleted.
        internal static void AddDeviceContext(DeviceContext dc) {
            if (activeDeviceContexts == null) {
                activeDeviceContexts = new ClientUtils.WeakRefCollection(); 
                activeDeviceContexts.RefCheckThreshold = 20; // See DevDiv#82664 (the repro app attached to this bug was used to determine this number).
            } 
 
            if (!activeDeviceContexts.Contains(dc)) {
                activeDeviceContexts.Add(dc); 
            }
        }

        internal static void RemoveDeviceContext(DeviceContext dc) { 
            if (activeDeviceContexts == null) {
                return; 
            } 
            activeDeviceContexts.RemoveByHashCode(dc);
        } 

 #if !DRAWING_NAMESPACE
        internal static bool IsFontInUse(WindowsFont wf) {
            if (wf == null) { 
                return false;
            } 
 
            for (int i = 0; i < activeDeviceContexts.Count; i++) {
                DeviceContext dc = activeDeviceContexts[i] as DeviceContext; 
                if (dc != null && (dc.ActiveFont == wf || dc.IsFontOnContextStack(wf))) {
                    return true;
                }
            } 

            return false; 
        } 
 #endif
 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
