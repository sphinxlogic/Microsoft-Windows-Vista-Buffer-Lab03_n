[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ToolStripContentPanelDesigner..ctor()")] 

namespace System.Windows.Forms.Design {
    using System.Design;
    using System.Runtime.InteropServices; 
    using System.ComponentModel;
    using System.Collections; 
    using System.Diagnostics; 
    using System;
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Design;
    using System.Windows.Forms; 
    using Microsoft.Win32;
    using System.Windows.Forms.Design.Behavior; 
 
    internal class ToolStripContentPanelDesigner : PanelDesigner {
 
        private BaseContextMenuStrip contextMenu;

        private ContextMenuStrip DesignerContextMenu
        { 
            get
            { 
                if (contextMenu == null) 
                {
                    contextMenu = new BaseContextMenuStrip(Component.Site, Component as Component); 
                    // If multiple Items Selected dont show the custom properties...
                    contextMenu.GroupOrdering.Clear();
                    contextMenu.GroupOrdering.AddRange(new string[] { StandardGroups.Code,
                                               StandardGroups.Verbs, 
                                               StandardGroups.Custom,
                                               StandardGroups.Selection, 
                                               StandardGroups.Edit, 
                                               StandardGroups.Properties});
                    contextMenu.Text = "CustomContextMenu"; 
                }
                return contextMenu;
            }
        } 

        public override IList SnapLines { 
            get { 
                // We don't want margin snaplines, so call directly to the internal method.
                ArrayList snapLines = null; 
                AddPaddingSnapLines(ref snapLines);
                return snapLines;
            }
        } 

        public override bool CanBeParentedTo(IDesigner parentDesigner) { 
            return false; 
        }
 
        protected override void OnContextMenu(int x, int y)
        {
            ToolStripContentPanel panel = Component as ToolStripContentPanel;
            if (panel != null && panel.Parent is ToolStripContainer) { 
                DesignerContextMenu.Show(x, y);
            } 
            else 
            {
                base.OnContextMenu(x, y); 
            }
        }

 
        protected override void PreFilterEvents(IDictionary events)
        { 
            base.PreFilterEvents(events); 
            EventDescriptor evnt;
            string[] noBrowseEvents = new string[] { 
                "BindingContextChanged",
                "ChangeUICues",
                "ClientSizeChanged",
                "EnabledChanged", 
                "FontChanged",
                "ForeColorChanged", 
                "GiveFeedback", 
                "ImeModeChanged",
                "Move", 
                "QueryAccessibilityHelp",
                "Validated",
                "Validating",
                "VisibleChanged", 
            };
 
            for (int i = 0; i < noBrowseEvents.Length; i++) 
            {
                evnt = (EventDescriptor)events[noBrowseEvents[i]]; 
                if (evnt != null)
                {
                    events[noBrowseEvents[i]] = TypeDescriptor.CreateEvent(evnt.ComponentType, evnt, BrowsableAttribute.No);
                } 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ToolStripContentPanelDesigner..ctor()")] 

namespace System.Windows.Forms.Design {
    using System.Design;
    using System.Runtime.InteropServices; 
    using System.ComponentModel;
    using System.Collections; 
    using System.Diagnostics; 
    using System;
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Design;
    using System.Windows.Forms; 
    using Microsoft.Win32;
    using System.Windows.Forms.Design.Behavior; 
 
    internal class ToolStripContentPanelDesigner : PanelDesigner {
 
        private BaseContextMenuStrip contextMenu;

        private ContextMenuStrip DesignerContextMenu
        { 
            get
            { 
                if (contextMenu == null) 
                {
                    contextMenu = new BaseContextMenuStrip(Component.Site, Component as Component); 
                    // If multiple Items Selected dont show the custom properties...
                    contextMenu.GroupOrdering.Clear();
                    contextMenu.GroupOrdering.AddRange(new string[] { StandardGroups.Code,
                                               StandardGroups.Verbs, 
                                               StandardGroups.Custom,
                                               StandardGroups.Selection, 
                                               StandardGroups.Edit, 
                                               StandardGroups.Properties});
                    contextMenu.Text = "CustomContextMenu"; 
                }
                return contextMenu;
            }
        } 

        public override IList SnapLines { 
            get { 
                // We don't want margin snaplines, so call directly to the internal method.
                ArrayList snapLines = null; 
                AddPaddingSnapLines(ref snapLines);
                return snapLines;
            }
        } 

        public override bool CanBeParentedTo(IDesigner parentDesigner) { 
            return false; 
        }
 
        protected override void OnContextMenu(int x, int y)
        {
            ToolStripContentPanel panel = Component as ToolStripContentPanel;
            if (panel != null && panel.Parent is ToolStripContainer) { 
                DesignerContextMenu.Show(x, y);
            } 
            else 
            {
                base.OnContextMenu(x, y); 
            }
        }

 
        protected override void PreFilterEvents(IDictionary events)
        { 
            base.PreFilterEvents(events); 
            EventDescriptor evnt;
            string[] noBrowseEvents = new string[] { 
                "BindingContextChanged",
                "ChangeUICues",
                "ClientSizeChanged",
                "EnabledChanged", 
                "FontChanged",
                "ForeColorChanged", 
                "GiveFeedback", 
                "ImeModeChanged",
                "Move", 
                "QueryAccessibilityHelp",
                "Validated",
                "Validating",
                "VisibleChanged", 
            };
 
            for (int i = 0; i < noBrowseEvents.Length; i++) 
            {
                evnt = (EventDescriptor)events[noBrowseEvents[i]]; 
                if (evnt != null)
                {
                    events[noBrowseEvents[i]] = TypeDescriptor.CreateEvent(evnt.ComponentType, evnt, BrowsableAttribute.No);
                } 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
