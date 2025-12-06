 
namespace System.Windows.Forms.Design.Behavior {
    using System;
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing;
    using System.Windows.Forms.Design; 

    /// <include file='doc\BehaviorDragDropEventArgs.uex' path='docs/doc[@for="BehaviorDragDropEventArgs"]/*' />
    /// <devdoc>
    ///     This class represents the arguments describing a BehaviorDragDrop event 
    ///     fired by the BehaviorService.
    /// </devdoc> 
    public class BehaviorDragDropEventArgs : EventArgs { 

        private ICollection dragComponents;//the list of components being dragged 

        /// <include file='doc\BehaviorDragDropEventArgs.uex' path='docs/doc[@for="BehaviorDragDropEventArgs.BehaviorDragDropEventArgs"]/*' />
        /// <devdoc>
        ///     Constructor.  This class is created by the BehaviorService directly 
        ///     before a drag operation begins.
        /// </devdoc> 
        public BehaviorDragDropEventArgs(ICollection dragComponents) { 
            this.dragComponents = dragComponents;
        } 

        /// <include file='doc\BehaviorDragDropEventArgs.uex' path='docs/doc[@for="BehaviorDragDropEventArgs.DragComponents"]/*' />
        /// <devdoc>
        ///     Returns the list of IComponents currently being dragged. 
        /// </devdoc>
        public ICollection DragComponents { 
            get { 
                return dragComponents;
            } 
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

    /// <include file='doc\BehaviorDragDropEventArgs.uex' path='docs/doc[@for="BehaviorDragDropEventArgs"]/*' />
    /// <devdoc>
    ///     This class represents the arguments describing a BehaviorDragDrop event 
    ///     fired by the BehaviorService.
    /// </devdoc> 
    public class BehaviorDragDropEventArgs : EventArgs { 

        private ICollection dragComponents;//the list of components being dragged 

        /// <include file='doc\BehaviorDragDropEventArgs.uex' path='docs/doc[@for="BehaviorDragDropEventArgs.BehaviorDragDropEventArgs"]/*' />
        /// <devdoc>
        ///     Constructor.  This class is created by the BehaviorService directly 
        ///     before a drag operation begins.
        /// </devdoc> 
        public BehaviorDragDropEventArgs(ICollection dragComponents) { 
            this.dragComponents = dragComponents;
        } 

        /// <include file='doc\BehaviorDragDropEventArgs.uex' path='docs/doc[@for="BehaviorDragDropEventArgs.DragComponents"]/*' />
        /// <devdoc>
        ///     Returns the list of IComponents currently being dragged. 
        /// </devdoc>
        public ICollection DragComponents { 
            get { 
                return dragComponents;
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
