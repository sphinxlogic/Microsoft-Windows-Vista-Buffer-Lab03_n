//------------------------------------------------------------------------------ 
// <copyright file="ContextMenuStripActionList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
    using System.Design; 
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Diagnostics;
    using System;
    using System.Security;
    using System.Security.Permissions; 
    using System.Collections;
    using System.ComponentModel.Design; 
    using System.Windows.Forms; 

    /// <include file='doc\ContextMenuStripActionList.uex' path='docs/doc[@for="ContextMenuStripActionList"]/*' /> 
    internal class ContextMenuStripActionList : DesignerActionList {

        private ToolStripDropDown _toolStripDropDown;
        private bool _autoShow  = false; 

        /// <include file='doc\ContextMenuStripActionList.uex' path='docs/doc[@for="ContextMenuStripActionList.ContextMenuStripActionList"]/*' /> 
        public ContextMenuStripActionList(ToolStripDropDownDesigner designer) : base (designer.Component) { 
            _toolStripDropDown = (ToolStripDropDown)designer.Component;
        } 

        //helper function to get the property on the actual Control
        private object GetProperty(string propertyName)
        { 
           PropertyDescriptor getProperty = TypeDescriptor.GetProperties(_toolStripDropDown)[propertyName];
           Debug.Assert( getProperty != null, "Could not find given property in control."); 
           if( getProperty != null ) 
           {
               return getProperty.GetValue(_toolStripDropDown); 
           }
           return null;
        }
 
        //helper function to change the property on the actual Control
        private void ChangeProperty(string propertyName, object value) 
        { 
           PropertyDescriptor changingProperty = TypeDescriptor.GetProperties(_toolStripDropDown)[propertyName];
           Debug.Assert( changingProperty != null, "Could not find given property in control." ); 
           if( changingProperty != null )
           {
               changingProperty.SetValue(_toolStripDropDown, value);
           } 
        }
 
        /// <include file='doc\ContextMenuStripActionList.uex' path='docs/doc[@for="ContextMenuStripActionList.AutoShow"]/*' /> 
        /// <summary>
        ///  Controls whether the Chrome is Automatically shown on selection 
        /// </summary>
        public override bool AutoShow {
            get {
                return _autoShow; 
            }
            set { 
                if(_autoShow != value) { 
                    _autoShow = value;
                } 
            }
        }

 
        /// <include file='doc\ContextMenuStripActionList.uex' path='docs/doc[@for="ContextMenuStripActionList.ShowImageMargin"]/*' />
        /// <summary> 
        /// Sets ShowImageMargin 
        /// </summary>
        public bool ShowImageMargin { 
            get {
                return (bool)GetProperty("ShowImageMargin");
            }
            set { 
                if (value != ShowImageMargin) {
                    ChangeProperty("ShowImageMargin", (object)value); 
                } 
            }
        } 

        /// <include file='doc\ContextMenuStripActionList.uex' path='docs/doc[@for="ContextMenuStripActionList.ShowCheckMargin"]/*' />
        /// <summary>
        /// Sets ShowCheckMargin 
        /// </summary>
        public bool ShowCheckMargin { 
            get { 
                return (bool)GetProperty("ShowCheckMargin");
            } 
            set {
                if (value != ShowCheckMargin) {
                    ChangeProperty("ShowCheckMargin", (object)value);
                } 
            }
        } 
 

        /// <include file='doc\ContextMenuStripActionList.uex' path='docs/doc[@for="ContextMenuStripActionList.RenderMode"]/*' /> 
        /// <summary>
        /// Sets RenderMode
        /// </summary>
        public ToolStripRenderMode RenderMode { 
            get {
                return (ToolStripRenderMode)GetProperty("RenderMode"); 
            } 
            set {
                if (value != RenderMode) { 
                    ChangeProperty("RenderMode", (object)value);
                }
            }
        } 

 
        /// <include file='doc\ContextMenuStripActionList.uex' path='docs/doc[@for="ContextMenuStripActionList.GetSortedActionItems"]/*' /> 
        /// <summary>
        ///     The Main method to group the ActionItems and pass it to the Panel. 
        /// </summary>
        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = new DesignerActionItemCollection();
 
            items.Add(new DesignerActionPropertyItem(SR.GetString(SR.ToolStripActionList_RenderMode),
                                                                   SR.GetString(SR.ToolStripActionList_RenderMode), 
                                                                   SR.GetString(SR.ToolStripActionList_Layout), 
                                                                   SR.GetString(SR.ToolStripActionList_RenderModeDesc)));
            if (_toolStripDropDown is ToolStripDropDownMenu) 
            {
                items.Add(new DesignerActionPropertyItem(SR.GetString(SR.ContextMenuStripActionList_ShowImageMargin),
                                                                       SR.GetString(SR.ContextMenuStripActionList_ShowImageMargin),
                                                                       SR.GetString(SR.ToolStripActionList_Layout), 
                                                                       SR.GetString(SR.ContextMenuStripActionList_ShowImageMarginDesc)));
 
                items.Add(new DesignerActionPropertyItem(SR.GetString(SR.ContextMenuStripActionList_ShowCheckMargin), 
                                                                       SR.GetString(SR.ContextMenuStripActionList_ShowCheckMargin),
                                                                       SR.GetString(SR.ToolStripActionList_Layout), 
                                                                       SR.GetString(SR.ContextMenuStripActionList_ShowCheckMarginDesc)));
            }
            return items;
 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ContextMenuStripActionList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
    using System.Design; 
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Diagnostics;
    using System;
    using System.Security;
    using System.Security.Permissions; 
    using System.Collections;
    using System.ComponentModel.Design; 
    using System.Windows.Forms; 

    /// <include file='doc\ContextMenuStripActionList.uex' path='docs/doc[@for="ContextMenuStripActionList"]/*' /> 
    internal class ContextMenuStripActionList : DesignerActionList {

        private ToolStripDropDown _toolStripDropDown;
        private bool _autoShow  = false; 

        /// <include file='doc\ContextMenuStripActionList.uex' path='docs/doc[@for="ContextMenuStripActionList.ContextMenuStripActionList"]/*' /> 
        public ContextMenuStripActionList(ToolStripDropDownDesigner designer) : base (designer.Component) { 
            _toolStripDropDown = (ToolStripDropDown)designer.Component;
        } 

        //helper function to get the property on the actual Control
        private object GetProperty(string propertyName)
        { 
           PropertyDescriptor getProperty = TypeDescriptor.GetProperties(_toolStripDropDown)[propertyName];
           Debug.Assert( getProperty != null, "Could not find given property in control."); 
           if( getProperty != null ) 
           {
               return getProperty.GetValue(_toolStripDropDown); 
           }
           return null;
        }
 
        //helper function to change the property on the actual Control
        private void ChangeProperty(string propertyName, object value) 
        { 
           PropertyDescriptor changingProperty = TypeDescriptor.GetProperties(_toolStripDropDown)[propertyName];
           Debug.Assert( changingProperty != null, "Could not find given property in control." ); 
           if( changingProperty != null )
           {
               changingProperty.SetValue(_toolStripDropDown, value);
           } 
        }
 
        /// <include file='doc\ContextMenuStripActionList.uex' path='docs/doc[@for="ContextMenuStripActionList.AutoShow"]/*' /> 
        /// <summary>
        ///  Controls whether the Chrome is Automatically shown on selection 
        /// </summary>
        public override bool AutoShow {
            get {
                return _autoShow; 
            }
            set { 
                if(_autoShow != value) { 
                    _autoShow = value;
                } 
            }
        }

 
        /// <include file='doc\ContextMenuStripActionList.uex' path='docs/doc[@for="ContextMenuStripActionList.ShowImageMargin"]/*' />
        /// <summary> 
        /// Sets ShowImageMargin 
        /// </summary>
        public bool ShowImageMargin { 
            get {
                return (bool)GetProperty("ShowImageMargin");
            }
            set { 
                if (value != ShowImageMargin) {
                    ChangeProperty("ShowImageMargin", (object)value); 
                } 
            }
        } 

        /// <include file='doc\ContextMenuStripActionList.uex' path='docs/doc[@for="ContextMenuStripActionList.ShowCheckMargin"]/*' />
        /// <summary>
        /// Sets ShowCheckMargin 
        /// </summary>
        public bool ShowCheckMargin { 
            get { 
                return (bool)GetProperty("ShowCheckMargin");
            } 
            set {
                if (value != ShowCheckMargin) {
                    ChangeProperty("ShowCheckMargin", (object)value);
                } 
            }
        } 
 

        /// <include file='doc\ContextMenuStripActionList.uex' path='docs/doc[@for="ContextMenuStripActionList.RenderMode"]/*' /> 
        /// <summary>
        /// Sets RenderMode
        /// </summary>
        public ToolStripRenderMode RenderMode { 
            get {
                return (ToolStripRenderMode)GetProperty("RenderMode"); 
            } 
            set {
                if (value != RenderMode) { 
                    ChangeProperty("RenderMode", (object)value);
                }
            }
        } 

 
        /// <include file='doc\ContextMenuStripActionList.uex' path='docs/doc[@for="ContextMenuStripActionList.GetSortedActionItems"]/*' /> 
        /// <summary>
        ///     The Main method to group the ActionItems and pass it to the Panel. 
        /// </summary>
        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = new DesignerActionItemCollection();
 
            items.Add(new DesignerActionPropertyItem(SR.GetString(SR.ToolStripActionList_RenderMode),
                                                                   SR.GetString(SR.ToolStripActionList_RenderMode), 
                                                                   SR.GetString(SR.ToolStripActionList_Layout), 
                                                                   SR.GetString(SR.ToolStripActionList_RenderModeDesc)));
            if (_toolStripDropDown is ToolStripDropDownMenu) 
            {
                items.Add(new DesignerActionPropertyItem(SR.GetString(SR.ContextMenuStripActionList_ShowImageMargin),
                                                                       SR.GetString(SR.ContextMenuStripActionList_ShowImageMargin),
                                                                       SR.GetString(SR.ToolStripActionList_Layout), 
                                                                       SR.GetString(SR.ContextMenuStripActionList_ShowImageMarginDesc)));
 
                items.Add(new DesignerActionPropertyItem(SR.GetString(SR.ContextMenuStripActionList_ShowCheckMargin), 
                                                                       SR.GetString(SR.ContextMenuStripActionList_ShowCheckMargin),
                                                                       SR.GetString(SR.ToolStripActionList_Layout), 
                                                                       SR.GetString(SR.ContextMenuStripActionList_ShowCheckMarginDesc)));
            }
            return items;
 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
