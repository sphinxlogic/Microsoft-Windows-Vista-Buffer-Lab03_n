// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
using System;
using System.Diagnostics; 
using System.Runtime.InteropServices; 
using Microsoft.Win32;
using System.Security.Permissions; 
using System.Security;
using System.Collections;
using System.Globalization;
using System.Runtime.Versioning; 

namespace System.Diagnostics { 
 
    internal static class AssertWrapper {
 
#if DEBUG && !FEATURE_PAL
        static BooleanSwitch DisableVsAssert = new BooleanSwitch("DisableVsAssert", "Switch to disable usage of VSASSERT for DefaultTraceListener.");
        static bool vsassertPresent = true;
        static Hashtable ignoredAsserts = new Hashtable(StringComparer.OrdinalIgnoreCase); 

        [ResourceExposure(ResourceScope.None)] 
        private static void ShowVsAssert(string stackTrace, StackFrame frame, string message, string detailMessage) { 
            int[] disable = new int[1];
            try { 
                string detailMessage2;

                if (detailMessage == null)
                    detailMessage2 = stackTrace; 
                else
                    detailMessage2 = detailMessage + "\r\n" + stackTrace; 
                string fileName = (frame == null) ? string.Empty : frame.GetFileName(); 
                if (fileName == null) {
                    fileName = string.Empty; 
                }

                int lineNumber = (frame == null) ? 0 : frame.GetFileLineNumber();
                int returnCode = VsAssert(detailMessage2, message, fileName, lineNumber, disable); 
                if (returnCode != 0) {
                    if (!System.Diagnostics.Debugger.IsAttached) { 
                        System.Diagnostics.Debugger.Launch(); 
                    }
                    System.Diagnostics.Debugger.Break(); 
                }
                if (disable[0] != 0)
                    ignoredAsserts[MakeAssertKey(fileName, lineNumber)] = null;
            } 
            catch (Exception) {
                vsassertPresent = false; 
            } 
        }
 
        [DllImport(ExternDll.Vsassert, CharSet=System.Runtime.InteropServices.CharSet.Ansi, BestFitMapping=true)]
        [ResourceExposure(ResourceScope.None)]
        public static extern int VsAssert(string message, string assert, string file, int line, [In, Out]int[] pfDisable);
 
        [ResourceExposure(ResourceScope.None)]
        public static void ShowAssert(string stackTrace, StackFrame frame, string message, string detailMessage) { 
            bool userInteractive = Environment.UserInteractive; 

            string fileName = (frame == null) ? string.Empty : frame.GetFileName(); 
            if (fileName == null) {
                fileName = string.Empty;
            }
 
            int lineNumber = (frame == null) ? 0 : frame.GetFileLineNumber();
 
            if (ignoredAsserts.ContainsKey(MakeAssertKey(fileName, lineNumber))) 
                return;
 
            if (vsassertPresent && !DisableVsAssert.Enabled && userInteractive)
                ShowVsAssert(stackTrace, frame, message, detailMessage);

            // the following is not in an 'else' because vsassertPresent might 
            // have gone from true to false.
 
            if (!vsassertPresent || DisableVsAssert.Enabled || !userInteractive) 
                ShowMessageBoxAssert(stackTrace, message, detailMessage);
        } 

        private static string MakeAssertKey(string fileName, int lineNumber) {
            return fileName + ":" + lineNumber.ToString(CultureInfo.InvariantCulture);
        } 

#else // DEBUG && !FEATURE_PAL 
 
        public static void ShowAssert(string stackTrace, StackFrame frame, string message, string detailMessage) {
            ShowMessageBoxAssert(stackTrace, message, detailMessage); 
        }

#endif // DEBUG && !FEATURE_PAL
 

        [ResourceExposure(ResourceScope.None)] 
        private static void ShowMessageBoxAssert(string stackTrace, string message, string detailMessage) { 
            string fullMessage = message + "\r\n" + detailMessage + "\r\n" + stackTrace;
 
#if !FEATURE_PAL
            fullMessage = TruncateMessageToFitScreen(fullMessage);
#endif // !FEATURE_PAL
 
            int flags = 0x00000002 /*AbortRetryIgnore*/ | 0x00000200 /*DefaultButton3*/ | 0x00000010 /*IconHand*/ |
                        0x00040000 /* TopMost */; 
 
            if (!Environment.UserInteractive)
                flags = flags | 0x00200000 /*ServiceNotification */; 

            if (IsRTLResources)
                flags = flags | SafeNativeMethods.MB_RIGHT | SafeNativeMethods.MB_RTLREADING;
 
            int rval = SafeNativeMethods.MessageBox(NativeMethods.NullHandleRef, fullMessage, SR.GetString(SR.DebugAssertTitle), flags);
            switch (rval) { 
                case 3: // abort 
                    new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
                    try { 
                        Environment.Exit(1);
                    }
                    finally {
                        CodeAccessPermission.RevertAssert(); 
                    }
                    break; 
                case 4: // retry 
                    if (!System.Diagnostics.Debugger.IsAttached) {
                        System.Diagnostics.Debugger.Launch(); 
                    }
                    System.Diagnostics.Debugger.Break();
                    break;
            } 
        }
 
 
        private static bool IsRTLResources {
            get { 
                return SR.GetString(SR.RTL) != "RTL_False";
            }
        }
 
#if !FEATURE_PAL
        // Since MessageBox will grow taller than the screen if there are too many lines do 
        // a rough calculation to make it fit. 
        [ResourceExposure(ResourceScope.None)]
        private static string TruncateMessageToFitScreen(string message) { 
            const int MaxCharsPerLine = 80;

            IntPtr hFont = SafeNativeMethods.GetStockObject(NativeMethods.DEFAULT_GUI_FONT);
            IntPtr hdc = UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef); 
            NativeMethods.TEXTMETRIC tm = new NativeMethods.TEXTMETRIC();
            hFont = UnsafeNativeMethods.SelectObject(new HandleRef(null, hdc), new HandleRef(null, hFont)); 
            SafeNativeMethods.GetTextMetrics(new HandleRef(null, hdc), tm); 
            UnsafeNativeMethods.SelectObject(new HandleRef(null, hdc), new HandleRef(null, hFont));
            UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, new HandleRef(null, hdc)); 
            hdc = IntPtr.Zero;
            int cy = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
            int maxLines = cy / tm.tmHeight - 15;
 
            int lineCount = 0;
            int lineCharCount = 0; 
            int i = 0; 
            while (lineCount < maxLines && i < message.Length - 1) {
                char ch = message[i]; 
                lineCharCount++;
                if (ch == '\n' || ch == '\r' || lineCharCount > MaxCharsPerLine) {
                    lineCount++;
                    lineCharCount = 0; 
                }
 
                // treat \r\n or \n\r as a single line break 
                if (ch == '\r' && message[i + 1] == '\n')
                    i+=2; 
                else if (ch == '\n' && message[i + 1] == '\r')
                    i+=2;
                else
                    i++; 
            }
            if (i < message.Length - 1) 
                message = SR.GetString(SR.DebugMessageTruncated, message.Substring(0, i)); 
            return message;
        } 
#endif // !FEATURE_PAL
    }
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
using System;
using System.Diagnostics; 
using System.Runtime.InteropServices; 
using Microsoft.Win32;
using System.Security.Permissions; 
using System.Security;
using System.Collections;
using System.Globalization;
using System.Runtime.Versioning; 

namespace System.Diagnostics { 
 
    internal static class AssertWrapper {
 
#if DEBUG && !FEATURE_PAL
        static BooleanSwitch DisableVsAssert = new BooleanSwitch("DisableVsAssert", "Switch to disable usage of VSASSERT for DefaultTraceListener.");
        static bool vsassertPresent = true;
        static Hashtable ignoredAsserts = new Hashtable(StringComparer.OrdinalIgnoreCase); 

        [ResourceExposure(ResourceScope.None)] 
        private static void ShowVsAssert(string stackTrace, StackFrame frame, string message, string detailMessage) { 
            int[] disable = new int[1];
            try { 
                string detailMessage2;

                if (detailMessage == null)
                    detailMessage2 = stackTrace; 
                else
                    detailMessage2 = detailMessage + "\r\n" + stackTrace; 
                string fileName = (frame == null) ? string.Empty : frame.GetFileName(); 
                if (fileName == null) {
                    fileName = string.Empty; 
                }

                int lineNumber = (frame == null) ? 0 : frame.GetFileLineNumber();
                int returnCode = VsAssert(detailMessage2, message, fileName, lineNumber, disable); 
                if (returnCode != 0) {
                    if (!System.Diagnostics.Debugger.IsAttached) { 
                        System.Diagnostics.Debugger.Launch(); 
                    }
                    System.Diagnostics.Debugger.Break(); 
                }
                if (disable[0] != 0)
                    ignoredAsserts[MakeAssertKey(fileName, lineNumber)] = null;
            } 
            catch (Exception) {
                vsassertPresent = false; 
            } 
        }
 
        [DllImport(ExternDll.Vsassert, CharSet=System.Runtime.InteropServices.CharSet.Ansi, BestFitMapping=true)]
        [ResourceExposure(ResourceScope.None)]
        public static extern int VsAssert(string message, string assert, string file, int line, [In, Out]int[] pfDisable);
 
        [ResourceExposure(ResourceScope.None)]
        public static void ShowAssert(string stackTrace, StackFrame frame, string message, string detailMessage) { 
            bool userInteractive = Environment.UserInteractive; 

            string fileName = (frame == null) ? string.Empty : frame.GetFileName(); 
            if (fileName == null) {
                fileName = string.Empty;
            }
 
            int lineNumber = (frame == null) ? 0 : frame.GetFileLineNumber();
 
            if (ignoredAsserts.ContainsKey(MakeAssertKey(fileName, lineNumber))) 
                return;
 
            if (vsassertPresent && !DisableVsAssert.Enabled && userInteractive)
                ShowVsAssert(stackTrace, frame, message, detailMessage);

            // the following is not in an 'else' because vsassertPresent might 
            // have gone from true to false.
 
            if (!vsassertPresent || DisableVsAssert.Enabled || !userInteractive) 
                ShowMessageBoxAssert(stackTrace, message, detailMessage);
        } 

        private static string MakeAssertKey(string fileName, int lineNumber) {
            return fileName + ":" + lineNumber.ToString(CultureInfo.InvariantCulture);
        } 

#else // DEBUG && !FEATURE_PAL 
 
        public static void ShowAssert(string stackTrace, StackFrame frame, string message, string detailMessage) {
            ShowMessageBoxAssert(stackTrace, message, detailMessage); 
        }

#endif // DEBUG && !FEATURE_PAL
 

        [ResourceExposure(ResourceScope.None)] 
        private static void ShowMessageBoxAssert(string stackTrace, string message, string detailMessage) { 
            string fullMessage = message + "\r\n" + detailMessage + "\r\n" + stackTrace;
 
#if !FEATURE_PAL
            fullMessage = TruncateMessageToFitScreen(fullMessage);
#endif // !FEATURE_PAL
 
            int flags = 0x00000002 /*AbortRetryIgnore*/ | 0x00000200 /*DefaultButton3*/ | 0x00000010 /*IconHand*/ |
                        0x00040000 /* TopMost */; 
 
            if (!Environment.UserInteractive)
                flags = flags | 0x00200000 /*ServiceNotification */; 

            if (IsRTLResources)
                flags = flags | SafeNativeMethods.MB_RIGHT | SafeNativeMethods.MB_RTLREADING;
 
            int rval = SafeNativeMethods.MessageBox(NativeMethods.NullHandleRef, fullMessage, SR.GetString(SR.DebugAssertTitle), flags);
            switch (rval) { 
                case 3: // abort 
                    new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
                    try { 
                        Environment.Exit(1);
                    }
                    finally {
                        CodeAccessPermission.RevertAssert(); 
                    }
                    break; 
                case 4: // retry 
                    if (!System.Diagnostics.Debugger.IsAttached) {
                        System.Diagnostics.Debugger.Launch(); 
                    }
                    System.Diagnostics.Debugger.Break();
                    break;
            } 
        }
 
 
        private static bool IsRTLResources {
            get { 
                return SR.GetString(SR.RTL) != "RTL_False";
            }
        }
 
#if !FEATURE_PAL
        // Since MessageBox will grow taller than the screen if there are too many lines do 
        // a rough calculation to make it fit. 
        [ResourceExposure(ResourceScope.None)]
        private static string TruncateMessageToFitScreen(string message) { 
            const int MaxCharsPerLine = 80;

            IntPtr hFont = SafeNativeMethods.GetStockObject(NativeMethods.DEFAULT_GUI_FONT);
            IntPtr hdc = UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef); 
            NativeMethods.TEXTMETRIC tm = new NativeMethods.TEXTMETRIC();
            hFont = UnsafeNativeMethods.SelectObject(new HandleRef(null, hdc), new HandleRef(null, hFont)); 
            SafeNativeMethods.GetTextMetrics(new HandleRef(null, hdc), tm); 
            UnsafeNativeMethods.SelectObject(new HandleRef(null, hdc), new HandleRef(null, hFont));
            UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, new HandleRef(null, hdc)); 
            hdc = IntPtr.Zero;
            int cy = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
            int maxLines = cy / tm.tmHeight - 15;
 
            int lineCount = 0;
            int lineCharCount = 0; 
            int i = 0; 
            while (lineCount < maxLines && i < message.Length - 1) {
                char ch = message[i]; 
                lineCharCount++;
                if (ch == '\n' || ch == '\r' || lineCharCount > MaxCharsPerLine) {
                    lineCount++;
                    lineCharCount = 0; 
                }
 
                // treat \r\n or \n\r as a single line break 
                if (ch == '\r' && message[i + 1] == '\n')
                    i+=2; 
                else if (ch == '\n' && message[i + 1] == '\r')
                    i+=2;
                else
                    i++; 
            }
            if (i < message.Length - 1) 
                message = SR.GetString(SR.DebugMessageTruncated, message.Substring(0, i)); 
            return message;
        } 
#endif // !FEATURE_PAL
    }
}
