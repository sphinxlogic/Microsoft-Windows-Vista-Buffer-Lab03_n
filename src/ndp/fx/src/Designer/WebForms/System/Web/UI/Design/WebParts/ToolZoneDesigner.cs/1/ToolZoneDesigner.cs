//------------------------------------------------------------------------------ 
// <copyright file="ToolZoneDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Web.UI.WebControls.WebParts;
 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class ToolZoneDesigner : WebZoneDesigner { 
 
        public override DesignerActionListCollection ActionLists {
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new ToolZoneDesignerActionList(this));
 
                return actionLists;
            } 
        } 

        protected bool ViewInBrowseMode { 
            get {
                object o = DesignerState["ViewInBrowseMode"];
                return (o != null) ? (bool)o : false;
            } 
            private set {
                if (value != ViewInBrowseMode) { 
                    DesignerState["ViewInBrowseMode"] = value; 
                    UpdateDesignTimeHtml();
                } 
            }
        }

        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(ToolZone));
            base.Initialize(component); 
        } 

        private class ToolZoneDesignerActionList : DesignerActionList { 
            private ToolZoneDesigner _parent;

            public ToolZoneDesignerActionList(ToolZoneDesigner parent) : base (parent.Component){
                _parent = parent; 
            }
 
            public override bool AutoShow { 
                get {
                    return true; 
                }
                set {
                }
            } 

            public bool ViewInBrowseMode { 
                get { 
                    return _parent.ViewInBrowseMode;
                } 
                set {
                    _parent.ViewInBrowseMode = value;
                }
            } 

            public override DesignerActionItemCollection GetSortedActionItems() { 
                DesignerActionItemCollection items = new DesignerActionItemCollection(); 
                items.Add(new DesignerActionPropertyItem("ViewInBrowseMode",
                                                          SR.GetString(SR.ToolZoneDesigner_ViewInBrowseMode), 
                                                          String.Empty,
                                                          SR.GetString(SR.ToolZoneDesigner_ViewInBrowseModeDesc)));
                return items;
            } 
        }
    } 
} 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ToolZoneDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls.WebParts { 
 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Web.UI.WebControls.WebParts;
 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class ToolZoneDesigner : WebZoneDesigner { 
 
        public override DesignerActionListCollection ActionLists {
            get { 
                DesignerActionListCollection actionLists = new DesignerActionListCollection();
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new ToolZoneDesignerActionList(this));
 
                return actionLists;
            } 
        } 

        protected bool ViewInBrowseMode { 
            get {
                object o = DesignerState["ViewInBrowseMode"];
                return (o != null) ? (bool)o : false;
            } 
            private set {
                if (value != ViewInBrowseMode) { 
                    DesignerState["ViewInBrowseMode"] = value; 
                    UpdateDesignTimeHtml();
                } 
            }
        }

        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(ToolZone));
            base.Initialize(component); 
        } 

        private class ToolZoneDesignerActionList : DesignerActionList { 
            private ToolZoneDesigner _parent;

            public ToolZoneDesignerActionList(ToolZoneDesigner parent) : base (parent.Component){
                _parent = parent; 
            }
 
            public override bool AutoShow { 
                get {
                    return true; 
                }
                set {
                }
            } 

            public bool ViewInBrowseMode { 
                get { 
                    return _parent.ViewInBrowseMode;
                } 
                set {
                    _parent.ViewInBrowseMode = value;
                }
            } 

            public override DesignerActionItemCollection GetSortedActionItems() { 
                DesignerActionItemCollection items = new DesignerActionItemCollection(); 
                items.Add(new DesignerActionPropertyItem("ViewInBrowseMode",
                                                          SR.GetString(SR.ToolZoneDesigner_ViewInBrowseMode), 
                                                          String.Empty,
                                                          SR.GetString(SR.ToolZoneDesigner_ViewInBrowseModeDesc)));
                return items;
            } 
        }
    } 
} 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
