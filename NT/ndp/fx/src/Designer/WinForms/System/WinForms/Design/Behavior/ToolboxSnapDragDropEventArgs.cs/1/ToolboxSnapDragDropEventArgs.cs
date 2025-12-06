namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Windows.Forms.Design;
 
    /// <include file='doc\ToolboxSnapDragDropEventArgs.uex' path='docs/doc[@for="ToolboxSnapDragDropEventArgs"]/*' />
    /// <devdoc>
    ///     This class is created by the ToolboxItemSnapLineBehavior when the
    ///     user clicks, drags, and drops a control from the toolbox.  This class 
    ///     adds value to the standard DragEventArgs by holding information
    ///     about how the user snapped a control when it was dropped.  We'll 
    ///     use this information in ParentControlDesigner when this new control 
    ///     is created to properly position and size the new control.
    /// </devdoc> 
    internal sealed class ToolboxSnapDragDropEventArgs :  DragEventArgs {

        private SnapDirection   snapDirections;//direction in which the user's cursor was snapped
        private Point           offset;//offset from the cursor to our 'drag box' 

        /// <include file='doc\ToolboxSnapDragDropEventArgs.uex' path='docs/doc[@for="ToolboxSnapDragDropEventArgs.ToolboxSnapDragDropEventArgs"]/*' /> 
        /// <devdoc> 
        ///     Constructor that is called when the user drops - here, we'll essentially
        ///     push the original drag event info down to the base class and store off 
        ///     our direction and offset.
        /// </devdoc>
        public ToolboxSnapDragDropEventArgs(SnapDirection snapDirections, Point offset, DragEventArgs origArgs) :
                                      base (origArgs.Data, origArgs.KeyState, origArgs.X, origArgs.Y, origArgs.AllowedEffect, origArgs.Effect) { 
            this.snapDirections = snapDirections;
            this.offset = offset; 
        } 

        /// <include file='doc\ToolboxSnapDragDropEventArgs.uex' path='docs/doc[@for="ToolboxSnapDragDropEventArgs.SnapDirections"]/*' /> 
        /// <devdoc>
        ///     This is the last direction that the user was snapped to directly before
        ///     the drop happened...
        /// </devdoc> 
        public SnapDirection SnapDirections {
            get { 
                return snapDirections; 
            }
        } 

        /// <include file='doc\ToolboxSnapDragDropEventArgs.uex' path='docs/doc[@for="ToolboxSnapDragDropEventArgs.Offset"]/*' />
        /// <devdoc>
        ///     The offset in pixel between the mouse cursor (at time of drop) and the 
        ///     'drag box' that is dancing around and snapping to other components.
        /// </devdoc> 
        public Point Offset { 
            get {
                return offset; 
            }
        }

        /// <include file='doc\ToolboxSnapDragDropEventArgs.uex' path='docs/doc[@for="ToolboxSnapDragDropEventArgs.SnapDirection"]/*' /> 
        /// <devdoc>
        ///     Flag enum used to define the different directions a 'drag box' could be 
        ///     snapped to. 
        /// </devdoc>
        [Flags] 
        public enum SnapDirection {
            None = 0x00,
            Top = 0x01,
            Bottom = 0x02, 
            Right = 0x04,
            Left = 0x08 
        } 

    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Windows.Forms.Design;
 
    /// <include file='doc\ToolboxSnapDragDropEventArgs.uex' path='docs/doc[@for="ToolboxSnapDragDropEventArgs"]/*' />
    /// <devdoc>
    ///     This class is created by the ToolboxItemSnapLineBehavior when the
    ///     user clicks, drags, and drops a control from the toolbox.  This class 
    ///     adds value to the standard DragEventArgs by holding information
    ///     about how the user snapped a control when it was dropped.  We'll 
    ///     use this information in ParentControlDesigner when this new control 
    ///     is created to properly position and size the new control.
    /// </devdoc> 
    internal sealed class ToolboxSnapDragDropEventArgs :  DragEventArgs {

        private SnapDirection   snapDirections;//direction in which the user's cursor was snapped
        private Point           offset;//offset from the cursor to our 'drag box' 

        /// <include file='doc\ToolboxSnapDragDropEventArgs.uex' path='docs/doc[@for="ToolboxSnapDragDropEventArgs.ToolboxSnapDragDropEventArgs"]/*' /> 
        /// <devdoc> 
        ///     Constructor that is called when the user drops - here, we'll essentially
        ///     push the original drag event info down to the base class and store off 
        ///     our direction and offset.
        /// </devdoc>
        public ToolboxSnapDragDropEventArgs(SnapDirection snapDirections, Point offset, DragEventArgs origArgs) :
                                      base (origArgs.Data, origArgs.KeyState, origArgs.X, origArgs.Y, origArgs.AllowedEffect, origArgs.Effect) { 
            this.snapDirections = snapDirections;
            this.offset = offset; 
        } 

        /// <include file='doc\ToolboxSnapDragDropEventArgs.uex' path='docs/doc[@for="ToolboxSnapDragDropEventArgs.SnapDirections"]/*' /> 
        /// <devdoc>
        ///     This is the last direction that the user was snapped to directly before
        ///     the drop happened...
        /// </devdoc> 
        public SnapDirection SnapDirections {
            get { 
                return snapDirections; 
            }
        } 

        /// <include file='doc\ToolboxSnapDragDropEventArgs.uex' path='docs/doc[@for="ToolboxSnapDragDropEventArgs.Offset"]/*' />
        /// <devdoc>
        ///     The offset in pixel between the mouse cursor (at time of drop) and the 
        ///     'drag box' that is dancing around and snapping to other components.
        /// </devdoc> 
        public Point Offset { 
            get {
                return offset; 
            }
        }

        /// <include file='doc\ToolboxSnapDragDropEventArgs.uex' path='docs/doc[@for="ToolboxSnapDragDropEventArgs.SnapDirection"]/*' /> 
        /// <devdoc>
        ///     Flag enum used to define the different directions a 'drag box' could be 
        ///     snapped to. 
        /// </devdoc>
        [Flags] 
        public enum SnapDirection {
            None = 0x00,
            Top = 0x01,
            Bottom = 0x02, 
            Right = 0x04,
            Left = 0x08 
        } 

    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
