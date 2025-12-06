//------------------------------------------------------------------------------ 
// <copyright file="ToolStripItemDataObject.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
*/ 
namespace System.Windows.Forms.Design
{ 
    using System.Diagnostics;
    using System;
    using System.Windows.Forms;
    using System.Collections; 

    /// <summary> 
    ///  Wrapper class for DataObject. This wrapped object is passed when a ToolStripItem is Drag-Dropped during DesignTime. 
    /// </summary>
    internal class ToolStripItemDataObject : DataObject 
    {
            private ArrayList dragComponents;
            private ToolStrip owner;
            private ToolStripItem primarySelection; 

            internal ToolStripItemDataObject(ArrayList dragComponents, ToolStripItem primarySelection, ToolStrip owner) : base(){ 
                this.dragComponents = dragComponents; 
                this.owner = owner;
                this.primarySelection = primarySelection; 
            }

            internal ArrayList DragComponents {
                get{ 
                    return dragComponents;
                } 
            } 

            internal ToolStrip Owner { 
                get {
                    return owner;
                }
            } 

            internal ToolStripItem PrimarySelection { 
                get { 
                    return primarySelection;
                } 
            }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ToolStripItemDataObject.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
*/ 
namespace System.Windows.Forms.Design
{ 
    using System.Diagnostics;
    using System;
    using System.Windows.Forms;
    using System.Collections; 

    /// <summary> 
    ///  Wrapper class for DataObject. This wrapped object is passed when a ToolStripItem is Drag-Dropped during DesignTime. 
    /// </summary>
    internal class ToolStripItemDataObject : DataObject 
    {
            private ArrayList dragComponents;
            private ToolStrip owner;
            private ToolStripItem primarySelection; 

            internal ToolStripItemDataObject(ArrayList dragComponents, ToolStripItem primarySelection, ToolStrip owner) : base(){ 
                this.dragComponents = dragComponents; 
                this.owner = owner;
                this.primarySelection = primarySelection; 
            }

            internal ArrayList DragComponents {
                get{ 
                    return dragComponents;
                } 
            } 

            internal ToolStrip Owner { 
                get {
                    return owner;
                }
            } 

            internal ToolStripItem PrimarySelection { 
                get { 
                    return primarySelection;
                } 
            }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
