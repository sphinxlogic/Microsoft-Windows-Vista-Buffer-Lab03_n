[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.PropertyGridDesigner..ctor()")] 
namespace System.Windows.Forms.Design {


    using System.Diagnostics; 

    using System; 
    using System.Windows.Forms; 
    using System.Drawing;
    using Microsoft.Win32; 

    using System.ComponentModel;
    using System.Drawing.Design;
    using System.ComponentModel.Design; 
    using System.Collections;
 
    /// <include file='doc\PictureBoxDesigner.uex' path='docs/doc[@for="PictureBoxDesigner"]/*' /> 
    /// <devdoc>
    ///     This class handles all design time behavior for the property grid class.  Group 
    ///     boxes may contain sub-components and therefore use the frame designer.
    /// </devdoc>
    internal class PropertyGridDesigner : ControlDesigner {
 
        public PropertyGridDesigner() {
            AutoResizeHandles = true; 
        } 
       /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.PreFilterProperties"]/*' />
        /// <devdoc> 
        ///      Allows a designer to filter the set of properties
        ///      the component it is designing will expose through the
        ///      TypeDescriptor object.  This method is called
        ///      immediately before its corresponding "Post" method. 
        ///      If you are overriding this method you should call
        ///      the base implementation before you perform your own 
        ///      filtering. 
        /// </devdoc>
        protected override void PreFilterProperties(IDictionary properties) { 

            // remove the ScrollableControl props...
            //
            properties.Remove("AutoScroll"); 
            properties.Remove("AutoScrollMargin");
            properties.Remove("DockPadding"); 
            properties.Remove("AutoScrollMinSize"); 

            base.PreFilterProperties(properties); 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.PropertyGridDesigner..ctor()")] 
namespace System.Windows.Forms.Design {


    using System.Diagnostics; 

    using System; 
    using System.Windows.Forms; 
    using System.Drawing;
    using Microsoft.Win32; 

    using System.ComponentModel;
    using System.Drawing.Design;
    using System.ComponentModel.Design; 
    using System.Collections;
 
    /// <include file='doc\PictureBoxDesigner.uex' path='docs/doc[@for="PictureBoxDesigner"]/*' /> 
    /// <devdoc>
    ///     This class handles all design time behavior for the property grid class.  Group 
    ///     boxes may contain sub-components and therefore use the frame designer.
    /// </devdoc>
    internal class PropertyGridDesigner : ControlDesigner {
 
        public PropertyGridDesigner() {
            AutoResizeHandles = true; 
        } 
       /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.PreFilterProperties"]/*' />
        /// <devdoc> 
        ///      Allows a designer to filter the set of properties
        ///      the component it is designing will expose through the
        ///      TypeDescriptor object.  This method is called
        ///      immediately before its corresponding "Post" method. 
        ///      If you are overriding this method you should call
        ///      the base implementation before you perform your own 
        ///      filtering. 
        /// </devdoc>
        protected override void PreFilterProperties(IDictionary properties) { 

            // remove the ScrollableControl props...
            //
            properties.Remove("AutoScroll"); 
            properties.Remove("AutoScrollMargin");
            properties.Remove("DockPadding"); 
            properties.Remove("AutoScrollMinSize"); 

            base.PreFilterProperties(properties); 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
