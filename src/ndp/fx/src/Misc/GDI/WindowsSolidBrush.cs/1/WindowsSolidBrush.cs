//------------------------------------------------------------------------------ 
// <copyright file="Brush.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

#if WINFORMS_NAMESPACE 
namespace System.Windows.Forms.Internal 
#elif DRAWING_NAMESPACE
namespace System.Drawing.Internal 
#else
namespace System.Experimental.Gdi
#endif
{ 
    using System;
    using System.Internal; 
    using System.Runtime.InteropServices; 
    using System.ComponentModel;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Globalization;

#if WINFORMS_PUBLIC_GRAPHICS_LIBRARY 
    public
#else 
    internal 
#endif
    sealed class WindowsSolidBrush : WindowsBrush 
    {
        protected override void CreateBrush()
        {
            IntPtr nativeHandle = IntSafeNativeMethods.CreateSolidBrush(ColorTranslator.ToWin32( this.Color)); 
            if(nativeHandle == IntPtr.Zero) // Don't use Debug.Assert, DbgUtil.GetLastErrorStr would always be evaluated.
            { 
                Debug.Fail("CreateSolidBrush failed : " + DbgUtil.GetLastErrorStr()); 
            }
 
            this.NativeHandle = nativeHandle;  // sets the handle value in the base class.
        }

        public WindowsSolidBrush(DeviceContext dc)  : base(dc) 
        {
            // CreateBrush() on demand. 
        } 

        public WindowsSolidBrush(DeviceContext dc, Color color) : base( dc, color ) 
        {
            // CreateBrush() on demand.
        }
 
        public override object Clone()
        { 
            return new WindowsSolidBrush(this.DC, this.Color); 
        }
 
        public override string ToString()
        {
            return String.Format( CultureInfo.InvariantCulture, "{0}: Color={1}", this.GetType().Name,  this.Color );
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="Brush.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

#if WINFORMS_NAMESPACE 
namespace System.Windows.Forms.Internal 
#elif DRAWING_NAMESPACE
namespace System.Drawing.Internal 
#else
namespace System.Experimental.Gdi
#endif
{ 
    using System;
    using System.Internal; 
    using System.Runtime.InteropServices; 
    using System.ComponentModel;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Globalization;

#if WINFORMS_PUBLIC_GRAPHICS_LIBRARY 
    public
#else 
    internal 
#endif
    sealed class WindowsSolidBrush : WindowsBrush 
    {
        protected override void CreateBrush()
        {
            IntPtr nativeHandle = IntSafeNativeMethods.CreateSolidBrush(ColorTranslator.ToWin32( this.Color)); 
            if(nativeHandle == IntPtr.Zero) // Don't use Debug.Assert, DbgUtil.GetLastErrorStr would always be evaluated.
            { 
                Debug.Fail("CreateSolidBrush failed : " + DbgUtil.GetLastErrorStr()); 
            }
 
            this.NativeHandle = nativeHandle;  // sets the handle value in the base class.
        }

        public WindowsSolidBrush(DeviceContext dc)  : base(dc) 
        {
            // CreateBrush() on demand. 
        } 

        public WindowsSolidBrush(DeviceContext dc, Color color) : base( dc, color ) 
        {
            // CreateBrush() on demand.
        }
 
        public override object Clone()
        { 
            return new WindowsSolidBrush(this.DC, this.Color); 
        }
 
        public override string ToString()
        {
            return String.Format( CultureInfo.InvariantCulture, "{0}: Color={1}", this.GetType().Name,  this.Color );
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
