//------------------------------------------------------------------------------ 
// <copyright file="ToolStripActionList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Windows.Forms.Design {
    using System.Design; 
    using System.Runtime.InteropServices; 
    using System.ComponentModel;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis;
    using System;
    using System.Security;
    using System.Security.Permissions; 
    using System.Collections;
    using System.ComponentModel.Design; 
    using System.Windows.Forms; 

    /// <include file='doc\ToolStripActionList.uex' path='docs/doc[@for="ToolStripActionList"]/*' /> 

    // IMPORTANT NOTE
    //THE CONTENTS OF THIS FILE ARE NOT ARRANGED IN ALPHABETICAL ORDER BUT MAP THERE POSITION IN THE "CHROME"
    // 
    internal class ToolStripActionList : DesignerActionList {
 
        private ToolStrip _toolStrip; 
        private bool _autoShow  = false;
        private ToolStripDesigner designer; 

        private ChangeToolStripParentVerb changeParentVerb = null;
        private StandardMenuStripVerb standardItemsVerb = null;
 

        /// <include file='doc\ToolStripActionList.uex' path='docs/doc[@for="ToolStripActionList.ToolStripActionList"]/*' /> 
        public ToolStripActionList(ToolStripDesigner designer) : base(designer.Component) { 
            _toolStrip = (ToolStrip)designer.Component;
            this.designer = designer; 

            changeParentVerb = new ChangeToolStripParentVerb(SR.GetString(SR.ToolStripDesignerEmbedVerb), designer);
            if (!(_toolStrip is StatusStrip))
            { 
                standardItemsVerb = new StandardMenuStripVerb(SR.GetString(SR.ToolStripDesignerStandardItemsVerb), designer);
            } 
        } 

        /// <summary> 
        ///  False if were inherited and can't be modified.
        /// </summary>
        private bool CanAddItems
        { 
            get
            { 
                // Make sure the component is not being inherited -- we can't delete these! 
                //
                InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(_toolStrip)[typeof(InheritanceAttribute)]; 
                if (ia == null || ia.InheritanceLevel == InheritanceLevel.NotInherited)
                {
                    return true;
                } 
                return false;
            } 
        } 

        private bool IsReadOnly 
        {
            get
            {
                // Make sure the component is not being inherited -- we can't delete these! 
                //
                InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(_toolStrip)[typeof(InheritanceAttribute)]; 
                if (ia == null || ia.InheritanceLevel == InheritanceLevel.InheritedReadOnly) 
                {
                    return true; 
                }
                return false;
            }
        } 

        //helper function to get the property on the actual Control 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        private object GetProperty(string propertyName)
        { 
           PropertyDescriptor getProperty = TypeDescriptor.GetProperties(_toolStrip)[propertyName];
           Debug.Assert( getProperty != null, "Could not find given property in control.");
           if( getProperty != null )
           { 
               return getProperty.GetValue(_toolStrip);
           } 
           return null; 
        }
 
        //helper function to change the property on the actual Control

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void ChangeProperty(string propertyName, object value) 
        {
           PropertyDescriptor changingProperty = TypeDescriptor.GetProperties(_toolStrip)[propertyName]; 
           Debug.Assert( changingProperty != null, "Could not find given property in control." ); 
           if( changingProperty != null )
           { 
               changingProperty.SetValue(_toolStrip, value);
           }
        }
 
        /// <include file='doc\ToolStripActionList.uex' path='docs/doc[@for="ToolStripActionList.AutoShow"]/*' />
        /// <summary> 
        ///  Controls whether the Chrome is Automatically shown on selection 
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
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


 
        /// <include file='doc\ToolStripActionList.uex' path='docs/doc[@for="ToolStripActionList.Dock"]/*' />
        /// <summary> 
        /// Sets Dock 
        /// </summary>
        public DockStyle Dock { 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            get {
                return (DockStyle)GetProperty("Dock");
            } 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            set { 
                if (value != Dock) { 
                    ChangeProperty("Dock", (object)value);
                } 
            }
        }

 
        /// <include file='doc\ToolStripActionList.uex' path='docs/doc[@for="ToolStripActionList.RenderMode"]/*' />
        /// <summary> 
        /// Sets RenderMode 
        /// </summary>
        public ToolStripRenderMode RenderMode { 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            get {
                return (ToolStripRenderMode)GetProperty("RenderMode");
            } 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            set { 
                if (value != RenderMode) { 
                    ChangeProperty("RenderMode", (object)value);
                } 
            }
        }

 
        /// <include file='doc\ToolStripActionList.uex' path='docs/doc[@for="ToolStripActionList.GripStyle"]/*' />
        /// <summary> 
        /// Sets GripStyle 
        /// </summary>
        public ToolStripGripStyle GripStyle { 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            get {
                return (ToolStripGripStyle)GetProperty("GripStyle");
            } 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            set { 
                if (value != GripStyle) { 
                    ChangeProperty("GripStyle", (object)value);
                } 
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        private void InvokeEmbedVerb() {
            // Hide the Panel... 
            DesignerActionUIService actionUIService = (DesignerActionUIService)_toolStrip.Site.GetService(typeof(DesignerActionUIService)); 
            if (actionUIService != null)
            { 
               actionUIService.HideUI(_toolStrip);
            }
            changeParentVerb.ChangeParent();
        } 

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        private void InvokeInsertStandardItemsVerb() { 
            standardItemsVerb.InsertItems();
        } 


        /// <include file='doc\ToolStripActionList.uex' path='docs/doc[@for="ToolStripActionList.GetSortedActionItems"]/*' />
        /// <summary> 
        ///     The Main method to group the ActionItems and pass it to the Panel.
        /// </summary> 
        public override DesignerActionItemCollection GetSortedActionItems() { 
 			DesignerActionItemCollection items = new DesignerActionItemCollection();
 
            if (!IsReadOnly)
            {
                items.Add(new DesignerActionMethodItem(this, "InvokeEmbedVerb", SR.GetString(SR.ToolStripDesignerEmbedVerb), "", SR.GetString(SR.ToolStripDesignerEmbedVerbDesc), true));
            } 

            if (CanAddItems) 
            { 
                if (!(_toolStrip is StatusStrip))
                { 
                    items.Add(new DesignerActionMethodItem(this, "InvokeInsertStandardItemsVerb", SR.GetString(SR.ToolStripDesignerStandardItemsVerb),"", SR.GetString(SR.ToolStripDesignerStandardItemsVerbDesc), true));
                }

                items.Add(new DesignerActionPropertyItem("RenderMode", 
                                                                  SR.GetString(SR.ToolStripActionList_RenderMode),
                                                                  SR.GetString(SR.ToolStripActionList_Layout), 
                                                                  SR.GetString(SR.ToolStripActionList_RenderModeDesc))); 
            }
 
            if (!(_toolStrip.Parent is ToolStripPanel))
            {
            items.Add(new DesignerActionPropertyItem("Dock",
                                                              SR.GetString(SR.ToolStripActionList_Dock), 
                                                              SR.GetString(SR.ToolStripActionList_Layout),
                                                              SR.GetString(SR.ToolStripActionList_DockDesc))); 
            } 
            if (!(_toolStrip is StatusStrip))
            { 
                items.Add(new DesignerActionPropertyItem("GripStyle",
                                                              SR.GetString(SR.ToolStripActionList_GripStyle),
                                                              SR.GetString(SR.ToolStripActionList_Layout),
                                                              SR.GetString(SR.ToolStripActionList_GripStyleDesc))); 
            }
            return items; 
        } 

    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ToolStripActionList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Windows.Forms.Design {
    using System.Design; 
    using System.Runtime.InteropServices; 
    using System.ComponentModel;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis;
    using System;
    using System.Security;
    using System.Security.Permissions; 
    using System.Collections;
    using System.ComponentModel.Design; 
    using System.Windows.Forms; 

    /// <include file='doc\ToolStripActionList.uex' path='docs/doc[@for="ToolStripActionList"]/*' /> 

    // IMPORTANT NOTE
    //THE CONTENTS OF THIS FILE ARE NOT ARRANGED IN ALPHABETICAL ORDER BUT MAP THERE POSITION IN THE "CHROME"
    // 
    internal class ToolStripActionList : DesignerActionList {
 
        private ToolStrip _toolStrip; 
        private bool _autoShow  = false;
        private ToolStripDesigner designer; 

        private ChangeToolStripParentVerb changeParentVerb = null;
        private StandardMenuStripVerb standardItemsVerb = null;
 

        /// <include file='doc\ToolStripActionList.uex' path='docs/doc[@for="ToolStripActionList.ToolStripActionList"]/*' /> 
        public ToolStripActionList(ToolStripDesigner designer) : base(designer.Component) { 
            _toolStrip = (ToolStrip)designer.Component;
            this.designer = designer; 

            changeParentVerb = new ChangeToolStripParentVerb(SR.GetString(SR.ToolStripDesignerEmbedVerb), designer);
            if (!(_toolStrip is StatusStrip))
            { 
                standardItemsVerb = new StandardMenuStripVerb(SR.GetString(SR.ToolStripDesignerStandardItemsVerb), designer);
            } 
        } 

        /// <summary> 
        ///  False if were inherited and can't be modified.
        /// </summary>
        private bool CanAddItems
        { 
            get
            { 
                // Make sure the component is not being inherited -- we can't delete these! 
                //
                InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(_toolStrip)[typeof(InheritanceAttribute)]; 
                if (ia == null || ia.InheritanceLevel == InheritanceLevel.NotInherited)
                {
                    return true;
                } 
                return false;
            } 
        } 

        private bool IsReadOnly 
        {
            get
            {
                // Make sure the component is not being inherited -- we can't delete these! 
                //
                InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(_toolStrip)[typeof(InheritanceAttribute)]; 
                if (ia == null || ia.InheritanceLevel == InheritanceLevel.InheritedReadOnly) 
                {
                    return true; 
                }
                return false;
            }
        } 

        //helper function to get the property on the actual Control 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        private object GetProperty(string propertyName)
        { 
           PropertyDescriptor getProperty = TypeDescriptor.GetProperties(_toolStrip)[propertyName];
           Debug.Assert( getProperty != null, "Could not find given property in control.");
           if( getProperty != null )
           { 
               return getProperty.GetValue(_toolStrip);
           } 
           return null; 
        }
 
        //helper function to change the property on the actual Control

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void ChangeProperty(string propertyName, object value) 
        {
           PropertyDescriptor changingProperty = TypeDescriptor.GetProperties(_toolStrip)[propertyName]; 
           Debug.Assert( changingProperty != null, "Could not find given property in control." ); 
           if( changingProperty != null )
           { 
               changingProperty.SetValue(_toolStrip, value);
           }
        }
 
        /// <include file='doc\ToolStripActionList.uex' path='docs/doc[@for="ToolStripActionList.AutoShow"]/*' />
        /// <summary> 
        ///  Controls whether the Chrome is Automatically shown on selection 
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
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


 
        /// <include file='doc\ToolStripActionList.uex' path='docs/doc[@for="ToolStripActionList.Dock"]/*' />
        /// <summary> 
        /// Sets Dock 
        /// </summary>
        public DockStyle Dock { 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            get {
                return (DockStyle)GetProperty("Dock");
            } 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            set { 
                if (value != Dock) { 
                    ChangeProperty("Dock", (object)value);
                } 
            }
        }

 
        /// <include file='doc\ToolStripActionList.uex' path='docs/doc[@for="ToolStripActionList.RenderMode"]/*' />
        /// <summary> 
        /// Sets RenderMode 
        /// </summary>
        public ToolStripRenderMode RenderMode { 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            get {
                return (ToolStripRenderMode)GetProperty("RenderMode");
            } 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            set { 
                if (value != RenderMode) { 
                    ChangeProperty("RenderMode", (object)value);
                } 
            }
        }

 
        /// <include file='doc\ToolStripActionList.uex' path='docs/doc[@for="ToolStripActionList.GripStyle"]/*' />
        /// <summary> 
        /// Sets GripStyle 
        /// </summary>
        public ToolStripGripStyle GripStyle { 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            get {
                return (ToolStripGripStyle)GetProperty("GripStyle");
            } 
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            set { 
                if (value != GripStyle) { 
                    ChangeProperty("GripStyle", (object)value);
                } 
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        private void InvokeEmbedVerb() {
            // Hide the Panel... 
            DesignerActionUIService actionUIService = (DesignerActionUIService)_toolStrip.Site.GetService(typeof(DesignerActionUIService)); 
            if (actionUIService != null)
            { 
               actionUIService.HideUI(_toolStrip);
            }
            changeParentVerb.ChangeParent();
        } 

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        private void InvokeInsertStandardItemsVerb() { 
            standardItemsVerb.InsertItems();
        } 


        /// <include file='doc\ToolStripActionList.uex' path='docs/doc[@for="ToolStripActionList.GetSortedActionItems"]/*' />
        /// <summary> 
        ///     The Main method to group the ActionItems and pass it to the Panel.
        /// </summary> 
        public override DesignerActionItemCollection GetSortedActionItems() { 
 			DesignerActionItemCollection items = new DesignerActionItemCollection();
 
            if (!IsReadOnly)
            {
                items.Add(new DesignerActionMethodItem(this, "InvokeEmbedVerb", SR.GetString(SR.ToolStripDesignerEmbedVerb), "", SR.GetString(SR.ToolStripDesignerEmbedVerbDesc), true));
            } 

            if (CanAddItems) 
            { 
                if (!(_toolStrip is StatusStrip))
                { 
                    items.Add(new DesignerActionMethodItem(this, "InvokeInsertStandardItemsVerb", SR.GetString(SR.ToolStripDesignerStandardItemsVerb),"", SR.GetString(SR.ToolStripDesignerStandardItemsVerbDesc), true));
                }

                items.Add(new DesignerActionPropertyItem("RenderMode", 
                                                                  SR.GetString(SR.ToolStripActionList_RenderMode),
                                                                  SR.GetString(SR.ToolStripActionList_Layout), 
                                                                  SR.GetString(SR.ToolStripActionList_RenderModeDesc))); 
            }
 
            if (!(_toolStrip.Parent is ToolStripPanel))
            {
            items.Add(new DesignerActionPropertyItem("Dock",
                                                              SR.GetString(SR.ToolStripActionList_Dock), 
                                                              SR.GetString(SR.ToolStripActionList_Layout),
                                                              SR.GetString(SR.ToolStripActionList_DockDesc))); 
            } 
            if (!(_toolStrip is StatusStrip))
            { 
                items.Add(new DesignerActionPropertyItem("GripStyle",
                                                              SR.GetString(SR.ToolStripActionList_GripStyle),
                                                              SR.GetString(SR.ToolStripActionList_Layout),
                                                              SR.GetString(SR.ToolStripActionList_GripStyleDesc))); 
            }
            return items; 
        } 

    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
