//------------------------------------------------------------------------------ 
// <copyright file="ToolStripPanelDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design
{ 
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

    /// <include file='doc\ToolStripPanelDesigner.uex' path='docs/doc[@for="ToolStripPanelDesigner"]/*' /> 
    /// <devdoc> 
    ///   Designer for the ToolStripPanel
    /// </devdoc> 
    internal class ToolStripPanelDesigner : ScrollableControlDesigner
    {
        // actual component
        private ToolStripPanel panel; 

        // component change service cache 
        private IComponentChangeService componentChangeSvc; 

        // default padding value. 
        private static Padding _defaultPadding = new Padding(0);

        // The Desinger host ....
        IDesignerHost designerHost; 

        // the container selector glyph which is associated with this designer. 
        private ToolStripPanelSelectionGlyph containerSelectorGlyph; 
        private ToolStripPanelSelectionBehavior behavior;
 
        //Designer context Menu for this designer
        private BaseContextMenuStrip contextMenu;

        // The SelectionService.. 
        ISelectionService selectionSvc;
 
        private MenuCommand designerShortCutCommand = null; 
        private MenuCommand oldShortCutCommand = null;
 
        /// <devdoc>
        ///      Creates a Dashed-Pen of appropriate color.
        /// </devdoc>
        private Pen BorderPen 
        {
            get 
            { 
                Color penColor = Control.BackColor.GetBrightness() < .5 ?
                              ControlPaint.Light(Control.BackColor) : 
                              ControlPaint.Dark(Control.BackColor);

                Pen pen = new Pen(penColor);
                pen.DashStyle = DashStyle.Dash; 

                return pen; 
            } 
        }
 
        // Custom ContextMenu.
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
 
        // ToolStripPanels if Inherited ACT as Readonly.
        protected override InheritanceAttribute InheritanceAttribute 
        { 
            get
            { 
                if ((panel != null && panel.Parent is ToolStripContainer) && (base.InheritanceAttribute == InheritanceAttribute.Inherited))
                {
                    return InheritanceAttribute.InheritedReadOnly;
                } 
                return base.InheritanceAttribute;
            } 
        } 

        /// <devdoc> 
        ///      ShadowProperty.
        /// </devdoc>
        private Padding Padding
        { 
            get
            { 
                return (Padding)ShadowProperties["Padding"]; 
            }
            set 
            {
                ShadowProperties["Padding"] = value;
            }
        } 

        /// <devdoc> 
        ///     This designer doesnt particiapte in Snaplines for the controls contained.. 
        /// </devdoc>
        public override bool ParticipatesWithSnapLines 
        {
            get
            {
                return false; 
            }
        } 
 
        /// <include file='doc\ToolStripPanelDesigner.uex' path='docs/doc[@for="ToolStripPanelDesigner.SelectionRules"]/*' />
        /// <devdoc> 
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc> 
        public override SelectionRules SelectionRules
        { 
            get 
            {
                SelectionRules rules = base.SelectionRules; 

                if (panel != null && panel.Parent is ToolStripContainer)
                {
                    rules = SelectionRules.Locked; 
                }
 
                return rules; 
            }
        } 


        //ContainerSelectorGlyph .. called from ToolStripContainerActionList to set the Expanded state to false when the panel's visibility is changed.
        public ToolStripPanelSelectionGlyph ToolStripPanelSelectorGlyph 
        {
            get 
            { 
                return containerSelectorGlyph;
            } 
        }

        /// <devdoc>
        ///      ShadowProperty. 
        /// </devdoc>
        private bool Visible 
        { 
            get
            { 
                return (bool)ShadowProperties["Visible"];
            }
            set
            { 
                ShadowProperties["Visible"] = value;
                panel.Visible = value; 
            } 
        }
 

        /// <devdoc>
        ///     Determines if the this designer can parent to the specified desinger --
        ///     generally this means if the control for this designer can parent the 
        ///     given ControlDesigner's designer.
        /// </devdoc> 
        public override bool CanParent(Control control) 
        {
            return (control is ToolStrip); 
        }

        /// <devdoc>
        ///     This designer can be parented to only ToolStripContainer. 
        /// </devdoc>
        public override bool CanBeParentedTo(IDesigner parentDesigner) 
        { 
            if (panel != null)
            { 
                return !(panel.Parent is ToolStripContainer);
            }
            return false;
        } 

        /// <devdoc> 
        ///   Update the glyph whenever component is changed... 
        /// </devdoc>
        private void ComponentChangeSvc_ComponentChanged(object sender, ComponentChangedEventArgs e) 
        {
            if (containerSelectorGlyph != null)
            {
                containerSelectorGlyph.UpdateGlyph(); 
            }
        } 
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.CreateToolCore"]/*' />
        /// <devdoc> 
        ///      This is the worker method of all CreateTool methods.  It is the only one
        ///      that can be overridden.
        /// </devdoc>
        protected override IComponent[] CreateToolCore(ToolboxItem tool, int x, int y, int width, int height, bool hasLocation, bool hasSize) 
        {
            if (tool != null) 
            { 
                Type toolType = tool.GetType(this.designerHost);
 
                if (!(typeof(ToolStrip).IsAssignableFrom(toolType)))
                {
                    ToolStripContainer parent = panel.Parent as ToolStripContainer;
                    if (parent != null) 
                    {
                        ToolStripContentPanel contentPanel = parent.ContentPanel; 
                        if (contentPanel != null) 
                        {
                            PanelDesigner designer = designerHost.GetDesigner(contentPanel) as PanelDesigner; 
                            if (designer != null)
                            {
                                InvokeCreateTool(designer, tool);
                            } 
                        }
                    } 
                } 
                else
                { 
                    base.CreateToolCore(tool, x, y, width, height, hasLocation, hasSize);
                }
            }
            return null; 
        }
 
        /// <include file='doc\ToolStripPanelDesigner.uex' path='docs/doc[@for="ToolStripPanelDesigner.Dispose"]/*' /> 
        /// <devdoc>
        ///     Disposes of this designer. 
        /// </devdoc>
        protected override void Dispose(bool disposing)
        {
            try 
            {
                base.Dispose(disposing); 
            } 
            finally
            { 
                if (disposing) {
                    if (contextMenu != null) {
                        contextMenu.Dispose();
                    } 
                }
 
                if (selectionSvc != null) 
                {
                    selectionSvc.SelectionChanging -= new EventHandler(this.OnSelectionChanging); 
                    selectionSvc.SelectionChanged -= new EventHandler(this.OnSelectionChanged);
                    selectionSvc = null;
                }
 
                if (componentChangeSvc != null)
                { 
                    componentChangeSvc.ComponentChanged -= new ComponentChangedEventHandler(ComponentChangeSvc_ComponentChanged); 
                }
                panel.ControlAdded -= new System.Windows.Forms.ControlEventHandler(OnControlAdded); 
                panel.ControlRemoved -= new System.Windows.Forms.ControlEventHandler(OnControlRemoved);
            }
        }
 
        /// <include file='doc\ToolStripPanelDesigner.uex' path='docs/doc[@for="ToolStripPanelDesigner.DrawBorder"]/*' />
        /// <devdoc> 
        ///      This draws a nice border around our RaftingContainer.  We need 
        ///      this because the panel can have no border and you can't
        ///      tell where it is. 
        /// </devdoc>
        /// <internalonly/>
        private void DrawBorder(Graphics graphics)
        { 
            Pen pen = BorderPen;
            Rectangle rc = Control.ClientRectangle; 
 
            rc.Width--;
            rc.Height--; 

            graphics.DrawRectangle(pen, rc);

            pen.Dispose(); 
        }
 
        /// <devdoc> 
        ///   We need to expand the TopToolStripPanel only when the control is Dropped onto the form .. for the first time...
        /// </devdoc> 
        internal void ExpandTopPanel()
        {
            if (containerSelectorGlyph == null)
            { 
                //get the adornerwindow-relative coords for the container control
                behavior = new ToolStripPanelSelectionBehavior(panel, Component.Site); 
                containerSelectorGlyph = new ToolStripPanelSelectionGlyph(Rectangle.Empty, Cursors.Default, panel, Component.Site, behavior); 
            }
            if (panel != null && panel.Dock == DockStyle.Top) 
            {
                panel.Padding = new Padding(0, 0, 25, 25);
                containerSelectorGlyph.IsExpanded = true;
            } 
        }
 
        private void OnKeyShowDesignerActions(object sender, EventArgs e) 
        {
            if (containerSelectorGlyph != null) 
            {
                behavior.OnMouseDown(containerSelectorGlyph, MouseButtons.Left, Point.Empty);
            }
        } 

        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.GetGlyphs"]/*' /> 
        /// <devdoc> 
        ///     Since we have to initialize glyphs for SplitterPanel (which is not a part of Components.) we overide the
        ///     GetGlyphs for the parent. 
        /// </devdoc>
        internal Glyph GetGlyph()
        {
            if (panel != null) 
            {
                // Add own Glyphs... 
                if (containerSelectorGlyph == null) 
                {
                    //get the adornerwindow-relative coords for the container control 
                    behavior = new ToolStripPanelSelectionBehavior(panel, Component.Site);
                    containerSelectorGlyph = new ToolStripPanelSelectionGlyph(Rectangle.Empty, Cursors.Default, panel, Component.Site, behavior);
                }
                // Show the Glyph only if Panel is Visible.... 
                if (panel.Visible)
                { 
                    return containerSelectorGlyph; 
                }
            } 
            return null;
        }

        /// <devdoc> 
        ///     This property is used by deriving classes to determine if it returns the control being designed or some other Container ...
        ///     while adding a component to it. 
        ///     e.g: When SplitContainer is selected and a component is being added ... the SplitContainer designer would return a 
        ///     SelectedPanel as the ParentControl for all the items being added rather than itself.
        /// </devdoc> 
        protected override Control GetParentForComponent(IComponent component)
        {
            Type toolType = component.GetType();
 
            if (typeof(ToolStrip).IsAssignableFrom(toolType))
            { 
                return panel; 
            }
            ToolStripContainer parent = panel.Parent as ToolStripContainer; 
            if (parent != null)
            {
                return parent.ContentPanel;
            } 
            return null;
 
        } 

        /// <summary> 
        ///  Get the designer set up to run.
        /// </summary>
        public override void Initialize(IComponent component)
        { 
            panel = component as ToolStripPanel;
 
            base.Initialize(component); 

            this.Padding = panel.Padding; 
            designerHost = (IDesignerHost)component.Site.GetService(typeof(IDesignerHost));

            if (selectionSvc == null)
            { 
                selectionSvc = (ISelectionService)GetService(typeof(ISelectionService));
                selectionSvc.SelectionChanging += new EventHandler(this.OnSelectionChanging); 
                selectionSvc.SelectionChanged += new EventHandler(this.OnSelectionChanged); 
            }
 
            if (designerHost != null)
            {
                componentChangeSvc = (IComponentChangeService)designerHost.GetService(typeof(IComponentChangeService));
            } 

            if (componentChangeSvc != null) 
            { 
                componentChangeSvc.ComponentChanged += new ComponentChangedEventHandler(ComponentChangeSvc_ComponentChanged);
            } 

            //Hook up the ControlAdded Event
            panel.ControlAdded += new System.Windows.Forms.ControlEventHandler(OnControlAdded);
            panel.ControlRemoved += new System.Windows.Forms.ControlEventHandler(OnControlRemoved); 
        }
 
        /// <devdoc> 
        ///   We need to invlidate the glyphBounds when the glyphs are turned off..
        /// </devdoc> 
        internal void InvalidateGlyph()
        {
            if (containerSelectorGlyph != null)
            { 
                BehaviorService.Invalidate(containerSelectorGlyph.Bounds);
            } 
        } 

        /// <devdoc> 
        ///      Required to CodeGen the Controls collection..
        /// </devdoc>
        private void OnControlAdded(object sender, System.Windows.Forms.ControlEventArgs e)
        { 
            if (e.Control is ToolStrip)
            { 
                //change the padding which might have been set by the Behavior if the panel is Expanded. 
                panel.Padding = new Padding(0);
                if (containerSelectorGlyph != null) 
                {
                    containerSelectorGlyph.IsExpanded = false;
                }
 
                // smoke the dock property whenever we add a toolstrip to a toolstrip panel.
                PropertyDescriptor dockProp = TypeDescriptor.GetProperties(e.Control)["Dock"]; 
                if (dockProp != null) 
                {
                    dockProp.SetValue(e.Control, DockStyle.None); 
                }

                //Reset the Glyphs.
                // Refer to VsWhidbey : 531684. Dont Refresh the glyph if we are loading the designer. 
                if (designerHost != null && !designerHost.Loading)
                { 
                    SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager)); 
                    if (selMgr != null)
                    { 
                        selMgr.Refresh();
                    }
                }
            } 
        }
 
        /// <devdoc> 
        ///      Required to CodeGen the Controls collection..
        /// </devdoc> 
        private void OnControlRemoved(object sender, System.Windows.Forms.ControlEventArgs e)
        {
            if (panel.Controls.Count == 0)
            { 
                if (containerSelectorGlyph != null)
                { 
                    containerSelectorGlyph.IsExpanded = false; 
                }
                //Reset the Glyphs. 
                // Refer to VsWhidbey : 531684. Dont Refresh the glyph if we are loading the designer.
                if (designerHost != null && !designerHost.Loading)
                {
                    SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager)); 
                    if (selMgr != null)
                    { 
                        selMgr.Refresh(); 
                    }
                } 
            }
        }

        /// <devdoc> 
        ///      Called when ContextMenu is invoked.
        /// </devdoc> 
        protected override void OnContextMenu(int x, int y) 
        {
            if (panel != null && panel.Parent is ToolStripContainer) 
            {
                DesignerContextMenu.Show(x, y);
            }
            else 
            {
                base.OnContextMenu(x, y); 
            } 
        }
 
        private void OnSelectionChanging(object sender, EventArgs e)
        {
            //Remove our DesignerShortCutHandler
            if (designerShortCutCommand != null) 
            {
                IMenuCommandService menuCommandService = (IMenuCommandService)GetService(typeof(IMenuCommandService)); 
                if (menuCommandService != null) 
                {
                    menuCommandService.RemoveCommand(designerShortCutCommand); 
                    if (oldShortCutCommand != null)
                    {
                        menuCommandService.AddCommand(oldShortCutCommand);
                    } 
                }
                designerShortCutCommand = null; 
            } 
        }
 
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (selectionSvc.PrimarySelection == panel)
            { 
                designerShortCutCommand = new MenuCommand(new EventHandler(OnKeyShowDesignerActions), MenuCommands.KeyInvokeSmartTag);
                IMenuCommandService menuCommandService = (IMenuCommandService)GetService(typeof(IMenuCommandService)); 
                if (menuCommandService != null) 
                {
                    oldShortCutCommand = menuCommandService.FindCommand(MenuCommands.KeyInvokeSmartTag); 
                    if (oldShortCutCommand != null)
                    {
                        menuCommandService.RemoveCommand(oldShortCutCommand);
                    } 
                    menuCommandService.AddCommand(designerShortCutCommand);
                } 
            } 
        }
 
        /// <devdoc>
        ///      Paint the borders for the panels..
        /// </devdoc>
        protected override void OnPaintAdornments(PaintEventArgs pe) 
        {
            if (!ToolStripDesignerUtils.DisplayInformation.TerminalServer && !ToolStripDesignerUtils.DisplayInformation.HighContrast && !ToolStripDesignerUtils.DisplayInformation.LowResolution) 
            { 
                using (Brush b = new SolidBrush(Color.FromArgb(50, Color.White)))
                { 
                    pe.Graphics.FillRectangle(b, panel.ClientRectangle);
                }
            }
            DrawBorder(pe.Graphics); 
        }
 
 
        protected override void PreFilterEvents(IDictionary events)
        { 
            base.PreFilterEvents(events);
            EventDescriptor evnt;

            if (panel.Parent is ToolStripContainer) 
            {
                string[] noBrowseEvents = new string[] { 
                    "AutoSizeChanged", 
                    "BindingContextChanged",
                    "CausesValidationChanged", 
                    "ChangeUICues",
                    "DockChanged",
                    "DragDrop",
                    "DragEnter", 
                    "DragLeave",
                    "DragOver", 
                    "EnabledChanged", 
                    "FontChanged",
                    "ForeColorChanged", 
                    "GiveFeedback",
                    "ImeModeChanged",
                    "KeyDown",
                    "KeyPress", 
                    "KeyUp",
                    "LocationChanged", 
                    "MarginChanged", 
                    "MouseCaptureChanged",
                    "Move", 
                    "QueryAccessibilityHelp",
                    "QueryContinueDrag",
                    "RegionChanged",
                    "Scroll", 
                    "Validated",
                    "Validating" 
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

        /// <devdoc>
        ///      Set some properties to non-browsable depending on the Parent. (StandAlone ToolStripPanel should support properties that are usually hidden when its a part of ToolStripcontainer)
        /// </devdoc> 
        protected override void PreFilterProperties(IDictionary properties)
        { 
            base.PreFilterProperties(properties); 
            PropertyDescriptor prop;
 
            if (panel.Parent is ToolStripContainer)
            {
                properties.Remove("Modifiers");
                properties.Remove("Locked"); 
                properties.Remove("GenerateMember");
 
                string[] noBrowseProps = new string[] { 
                    "Anchor",
                    "AutoSize", 
                    "Dock",
                    "DockPadding",
                    "Height",
                    "Location", 
                    "Name",
                    "Orientation", 
                    "Renderer", 
                    "RowMargin",
                    "Size", 
                    "Visible",
                    "Width",
                };
 
                for (int i = 0; i < noBrowseProps.Length; i++)
                { 
                    prop = (PropertyDescriptor)properties[noBrowseProps[i]]; 
                    if (prop != null)
                    { 
                        properties[noBrowseProps[i]] = TypeDescriptor.CreateProperty(prop.ComponentType, prop, BrowsableAttribute.No, DesignerSerializationVisibilityAttribute.Hidden);
                    }
                }
            } 
            string[] shadowProps = new string[] {
               "Padding", 
               "Visible" 
            };
            Attribute[] empty = new Attribute[0]; 
            for (int i = 0; i < shadowProps.Length; i++)
            {
                prop = (PropertyDescriptor)properties[shadowProps[i]];
                if (prop != null) 
                {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(ToolStripPanelDesigner), prop, empty); 
                } 
            }
        } 

        /// <devdoc>
        ///      Should Serialize Padding
        /// </devdoc> 
        private bool ShouldSerializePadding()
        { 
            Padding padding = (Padding)ShadowProperties["Padding"]; 
            return !padding.Equals(_defaultPadding);
        } 

        /// <devdoc>
        ///      Should serialize for visible property
        /// </devdoc> 
        private bool ShouldSerializeVisible()
        { 
            return !Visible; 
        }
 

    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ToolStripPanelDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design
{ 
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

    /// <include file='doc\ToolStripPanelDesigner.uex' path='docs/doc[@for="ToolStripPanelDesigner"]/*' /> 
    /// <devdoc> 
    ///   Designer for the ToolStripPanel
    /// </devdoc> 
    internal class ToolStripPanelDesigner : ScrollableControlDesigner
    {
        // actual component
        private ToolStripPanel panel; 

        // component change service cache 
        private IComponentChangeService componentChangeSvc; 

        // default padding value. 
        private static Padding _defaultPadding = new Padding(0);

        // The Desinger host ....
        IDesignerHost designerHost; 

        // the container selector glyph which is associated with this designer. 
        private ToolStripPanelSelectionGlyph containerSelectorGlyph; 
        private ToolStripPanelSelectionBehavior behavior;
 
        //Designer context Menu for this designer
        private BaseContextMenuStrip contextMenu;

        // The SelectionService.. 
        ISelectionService selectionSvc;
 
        private MenuCommand designerShortCutCommand = null; 
        private MenuCommand oldShortCutCommand = null;
 
        /// <devdoc>
        ///      Creates a Dashed-Pen of appropriate color.
        /// </devdoc>
        private Pen BorderPen 
        {
            get 
            { 
                Color penColor = Control.BackColor.GetBrightness() < .5 ?
                              ControlPaint.Light(Control.BackColor) : 
                              ControlPaint.Dark(Control.BackColor);

                Pen pen = new Pen(penColor);
                pen.DashStyle = DashStyle.Dash; 

                return pen; 
            } 
        }
 
        // Custom ContextMenu.
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
 
        // ToolStripPanels if Inherited ACT as Readonly.
        protected override InheritanceAttribute InheritanceAttribute 
        { 
            get
            { 
                if ((panel != null && panel.Parent is ToolStripContainer) && (base.InheritanceAttribute == InheritanceAttribute.Inherited))
                {
                    return InheritanceAttribute.InheritedReadOnly;
                } 
                return base.InheritanceAttribute;
            } 
        } 

        /// <devdoc> 
        ///      ShadowProperty.
        /// </devdoc>
        private Padding Padding
        { 
            get
            { 
                return (Padding)ShadowProperties["Padding"]; 
            }
            set 
            {
                ShadowProperties["Padding"] = value;
            }
        } 

        /// <devdoc> 
        ///     This designer doesnt particiapte in Snaplines for the controls contained.. 
        /// </devdoc>
        public override bool ParticipatesWithSnapLines 
        {
            get
            {
                return false; 
            }
        } 
 
        /// <include file='doc\ToolStripPanelDesigner.uex' path='docs/doc[@for="ToolStripPanelDesigner.SelectionRules"]/*' />
        /// <devdoc> 
        ///     Retrieves a set of rules concerning the movement capabilities of a component.
        ///     This should be one or more flags from the SelectionRules class.  If no designer
        ///     provides rules for a component, the component will not get any UI services.
        /// </devdoc> 
        public override SelectionRules SelectionRules
        { 
            get 
            {
                SelectionRules rules = base.SelectionRules; 

                if (panel != null && panel.Parent is ToolStripContainer)
                {
                    rules = SelectionRules.Locked; 
                }
 
                return rules; 
            }
        } 


        //ContainerSelectorGlyph .. called from ToolStripContainerActionList to set the Expanded state to false when the panel's visibility is changed.
        public ToolStripPanelSelectionGlyph ToolStripPanelSelectorGlyph 
        {
            get 
            { 
                return containerSelectorGlyph;
            } 
        }

        /// <devdoc>
        ///      ShadowProperty. 
        /// </devdoc>
        private bool Visible 
        { 
            get
            { 
                return (bool)ShadowProperties["Visible"];
            }
            set
            { 
                ShadowProperties["Visible"] = value;
                panel.Visible = value; 
            } 
        }
 

        /// <devdoc>
        ///     Determines if the this designer can parent to the specified desinger --
        ///     generally this means if the control for this designer can parent the 
        ///     given ControlDesigner's designer.
        /// </devdoc> 
        public override bool CanParent(Control control) 
        {
            return (control is ToolStrip); 
        }

        /// <devdoc>
        ///     This designer can be parented to only ToolStripContainer. 
        /// </devdoc>
        public override bool CanBeParentedTo(IDesigner parentDesigner) 
        { 
            if (panel != null)
            { 
                return !(panel.Parent is ToolStripContainer);
            }
            return false;
        } 

        /// <devdoc> 
        ///   Update the glyph whenever component is changed... 
        /// </devdoc>
        private void ComponentChangeSvc_ComponentChanged(object sender, ComponentChangedEventArgs e) 
        {
            if (containerSelectorGlyph != null)
            {
                containerSelectorGlyph.UpdateGlyph(); 
            }
        } 
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.CreateToolCore"]/*' />
        /// <devdoc> 
        ///      This is the worker method of all CreateTool methods.  It is the only one
        ///      that can be overridden.
        /// </devdoc>
        protected override IComponent[] CreateToolCore(ToolboxItem tool, int x, int y, int width, int height, bool hasLocation, bool hasSize) 
        {
            if (tool != null) 
            { 
                Type toolType = tool.GetType(this.designerHost);
 
                if (!(typeof(ToolStrip).IsAssignableFrom(toolType)))
                {
                    ToolStripContainer parent = panel.Parent as ToolStripContainer;
                    if (parent != null) 
                    {
                        ToolStripContentPanel contentPanel = parent.ContentPanel; 
                        if (contentPanel != null) 
                        {
                            PanelDesigner designer = designerHost.GetDesigner(contentPanel) as PanelDesigner; 
                            if (designer != null)
                            {
                                InvokeCreateTool(designer, tool);
                            } 
                        }
                    } 
                } 
                else
                { 
                    base.CreateToolCore(tool, x, y, width, height, hasLocation, hasSize);
                }
            }
            return null; 
        }
 
        /// <include file='doc\ToolStripPanelDesigner.uex' path='docs/doc[@for="ToolStripPanelDesigner.Dispose"]/*' /> 
        /// <devdoc>
        ///     Disposes of this designer. 
        /// </devdoc>
        protected override void Dispose(bool disposing)
        {
            try 
            {
                base.Dispose(disposing); 
            } 
            finally
            { 
                if (disposing) {
                    if (contextMenu != null) {
                        contextMenu.Dispose();
                    } 
                }
 
                if (selectionSvc != null) 
                {
                    selectionSvc.SelectionChanging -= new EventHandler(this.OnSelectionChanging); 
                    selectionSvc.SelectionChanged -= new EventHandler(this.OnSelectionChanged);
                    selectionSvc = null;
                }
 
                if (componentChangeSvc != null)
                { 
                    componentChangeSvc.ComponentChanged -= new ComponentChangedEventHandler(ComponentChangeSvc_ComponentChanged); 
                }
                panel.ControlAdded -= new System.Windows.Forms.ControlEventHandler(OnControlAdded); 
                panel.ControlRemoved -= new System.Windows.Forms.ControlEventHandler(OnControlRemoved);
            }
        }
 
        /// <include file='doc\ToolStripPanelDesigner.uex' path='docs/doc[@for="ToolStripPanelDesigner.DrawBorder"]/*' />
        /// <devdoc> 
        ///      This draws a nice border around our RaftingContainer.  We need 
        ///      this because the panel can have no border and you can't
        ///      tell where it is. 
        /// </devdoc>
        /// <internalonly/>
        private void DrawBorder(Graphics graphics)
        { 
            Pen pen = BorderPen;
            Rectangle rc = Control.ClientRectangle; 
 
            rc.Width--;
            rc.Height--; 

            graphics.DrawRectangle(pen, rc);

            pen.Dispose(); 
        }
 
        /// <devdoc> 
        ///   We need to expand the TopToolStripPanel only when the control is Dropped onto the form .. for the first time...
        /// </devdoc> 
        internal void ExpandTopPanel()
        {
            if (containerSelectorGlyph == null)
            { 
                //get the adornerwindow-relative coords for the container control
                behavior = new ToolStripPanelSelectionBehavior(panel, Component.Site); 
                containerSelectorGlyph = new ToolStripPanelSelectionGlyph(Rectangle.Empty, Cursors.Default, panel, Component.Site, behavior); 
            }
            if (panel != null && panel.Dock == DockStyle.Top) 
            {
                panel.Padding = new Padding(0, 0, 25, 25);
                containerSelectorGlyph.IsExpanded = true;
            } 
        }
 
        private void OnKeyShowDesignerActions(object sender, EventArgs e) 
        {
            if (containerSelectorGlyph != null) 
            {
                behavior.OnMouseDown(containerSelectorGlyph, MouseButtons.Left, Point.Empty);
            }
        } 

        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.GetGlyphs"]/*' /> 
        /// <devdoc> 
        ///     Since we have to initialize glyphs for SplitterPanel (which is not a part of Components.) we overide the
        ///     GetGlyphs for the parent. 
        /// </devdoc>
        internal Glyph GetGlyph()
        {
            if (panel != null) 
            {
                // Add own Glyphs... 
                if (containerSelectorGlyph == null) 
                {
                    //get the adornerwindow-relative coords for the container control 
                    behavior = new ToolStripPanelSelectionBehavior(panel, Component.Site);
                    containerSelectorGlyph = new ToolStripPanelSelectionGlyph(Rectangle.Empty, Cursors.Default, panel, Component.Site, behavior);
                }
                // Show the Glyph only if Panel is Visible.... 
                if (panel.Visible)
                { 
                    return containerSelectorGlyph; 
                }
            } 
            return null;
        }

        /// <devdoc> 
        ///     This property is used by deriving classes to determine if it returns the control being designed or some other Container ...
        ///     while adding a component to it. 
        ///     e.g: When SplitContainer is selected and a component is being added ... the SplitContainer designer would return a 
        ///     SelectedPanel as the ParentControl for all the items being added rather than itself.
        /// </devdoc> 
        protected override Control GetParentForComponent(IComponent component)
        {
            Type toolType = component.GetType();
 
            if (typeof(ToolStrip).IsAssignableFrom(toolType))
            { 
                return panel; 
            }
            ToolStripContainer parent = panel.Parent as ToolStripContainer; 
            if (parent != null)
            {
                return parent.ContentPanel;
            } 
            return null;
 
        } 

        /// <summary> 
        ///  Get the designer set up to run.
        /// </summary>
        public override void Initialize(IComponent component)
        { 
            panel = component as ToolStripPanel;
 
            base.Initialize(component); 

            this.Padding = panel.Padding; 
            designerHost = (IDesignerHost)component.Site.GetService(typeof(IDesignerHost));

            if (selectionSvc == null)
            { 
                selectionSvc = (ISelectionService)GetService(typeof(ISelectionService));
                selectionSvc.SelectionChanging += new EventHandler(this.OnSelectionChanging); 
                selectionSvc.SelectionChanged += new EventHandler(this.OnSelectionChanged); 
            }
 
            if (designerHost != null)
            {
                componentChangeSvc = (IComponentChangeService)designerHost.GetService(typeof(IComponentChangeService));
            } 

            if (componentChangeSvc != null) 
            { 
                componentChangeSvc.ComponentChanged += new ComponentChangedEventHandler(ComponentChangeSvc_ComponentChanged);
            } 

            //Hook up the ControlAdded Event
            panel.ControlAdded += new System.Windows.Forms.ControlEventHandler(OnControlAdded);
            panel.ControlRemoved += new System.Windows.Forms.ControlEventHandler(OnControlRemoved); 
        }
 
        /// <devdoc> 
        ///   We need to invlidate the glyphBounds when the glyphs are turned off..
        /// </devdoc> 
        internal void InvalidateGlyph()
        {
            if (containerSelectorGlyph != null)
            { 
                BehaviorService.Invalidate(containerSelectorGlyph.Bounds);
            } 
        } 

        /// <devdoc> 
        ///      Required to CodeGen the Controls collection..
        /// </devdoc>
        private void OnControlAdded(object sender, System.Windows.Forms.ControlEventArgs e)
        { 
            if (e.Control is ToolStrip)
            { 
                //change the padding which might have been set by the Behavior if the panel is Expanded. 
                panel.Padding = new Padding(0);
                if (containerSelectorGlyph != null) 
                {
                    containerSelectorGlyph.IsExpanded = false;
                }
 
                // smoke the dock property whenever we add a toolstrip to a toolstrip panel.
                PropertyDescriptor dockProp = TypeDescriptor.GetProperties(e.Control)["Dock"]; 
                if (dockProp != null) 
                {
                    dockProp.SetValue(e.Control, DockStyle.None); 
                }

                //Reset the Glyphs.
                // Refer to VsWhidbey : 531684. Dont Refresh the glyph if we are loading the designer. 
                if (designerHost != null && !designerHost.Loading)
                { 
                    SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager)); 
                    if (selMgr != null)
                    { 
                        selMgr.Refresh();
                    }
                }
            } 
        }
 
        /// <devdoc> 
        ///      Required to CodeGen the Controls collection..
        /// </devdoc> 
        private void OnControlRemoved(object sender, System.Windows.Forms.ControlEventArgs e)
        {
            if (panel.Controls.Count == 0)
            { 
                if (containerSelectorGlyph != null)
                { 
                    containerSelectorGlyph.IsExpanded = false; 
                }
                //Reset the Glyphs. 
                // Refer to VsWhidbey : 531684. Dont Refresh the glyph if we are loading the designer.
                if (designerHost != null && !designerHost.Loading)
                {
                    SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager)); 
                    if (selMgr != null)
                    { 
                        selMgr.Refresh(); 
                    }
                } 
            }
        }

        /// <devdoc> 
        ///      Called when ContextMenu is invoked.
        /// </devdoc> 
        protected override void OnContextMenu(int x, int y) 
        {
            if (panel != null && panel.Parent is ToolStripContainer) 
            {
                DesignerContextMenu.Show(x, y);
            }
            else 
            {
                base.OnContextMenu(x, y); 
            } 
        }
 
        private void OnSelectionChanging(object sender, EventArgs e)
        {
            //Remove our DesignerShortCutHandler
            if (designerShortCutCommand != null) 
            {
                IMenuCommandService menuCommandService = (IMenuCommandService)GetService(typeof(IMenuCommandService)); 
                if (menuCommandService != null) 
                {
                    menuCommandService.RemoveCommand(designerShortCutCommand); 
                    if (oldShortCutCommand != null)
                    {
                        menuCommandService.AddCommand(oldShortCutCommand);
                    } 
                }
                designerShortCutCommand = null; 
            } 
        }
 
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (selectionSvc.PrimarySelection == panel)
            { 
                designerShortCutCommand = new MenuCommand(new EventHandler(OnKeyShowDesignerActions), MenuCommands.KeyInvokeSmartTag);
                IMenuCommandService menuCommandService = (IMenuCommandService)GetService(typeof(IMenuCommandService)); 
                if (menuCommandService != null) 
                {
                    oldShortCutCommand = menuCommandService.FindCommand(MenuCommands.KeyInvokeSmartTag); 
                    if (oldShortCutCommand != null)
                    {
                        menuCommandService.RemoveCommand(oldShortCutCommand);
                    } 
                    menuCommandService.AddCommand(designerShortCutCommand);
                } 
            } 
        }
 
        /// <devdoc>
        ///      Paint the borders for the panels..
        /// </devdoc>
        protected override void OnPaintAdornments(PaintEventArgs pe) 
        {
            if (!ToolStripDesignerUtils.DisplayInformation.TerminalServer && !ToolStripDesignerUtils.DisplayInformation.HighContrast && !ToolStripDesignerUtils.DisplayInformation.LowResolution) 
            { 
                using (Brush b = new SolidBrush(Color.FromArgb(50, Color.White)))
                { 
                    pe.Graphics.FillRectangle(b, panel.ClientRectangle);
                }
            }
            DrawBorder(pe.Graphics); 
        }
 
 
        protected override void PreFilterEvents(IDictionary events)
        { 
            base.PreFilterEvents(events);
            EventDescriptor evnt;

            if (panel.Parent is ToolStripContainer) 
            {
                string[] noBrowseEvents = new string[] { 
                    "AutoSizeChanged", 
                    "BindingContextChanged",
                    "CausesValidationChanged", 
                    "ChangeUICues",
                    "DockChanged",
                    "DragDrop",
                    "DragEnter", 
                    "DragLeave",
                    "DragOver", 
                    "EnabledChanged", 
                    "FontChanged",
                    "ForeColorChanged", 
                    "GiveFeedback",
                    "ImeModeChanged",
                    "KeyDown",
                    "KeyPress", 
                    "KeyUp",
                    "LocationChanged", 
                    "MarginChanged", 
                    "MouseCaptureChanged",
                    "Move", 
                    "QueryAccessibilityHelp",
                    "QueryContinueDrag",
                    "RegionChanged",
                    "Scroll", 
                    "Validated",
                    "Validating" 
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

        /// <devdoc>
        ///      Set some properties to non-browsable depending on the Parent. (StandAlone ToolStripPanel should support properties that are usually hidden when its a part of ToolStripcontainer)
        /// </devdoc> 
        protected override void PreFilterProperties(IDictionary properties)
        { 
            base.PreFilterProperties(properties); 
            PropertyDescriptor prop;
 
            if (panel.Parent is ToolStripContainer)
            {
                properties.Remove("Modifiers");
                properties.Remove("Locked"); 
                properties.Remove("GenerateMember");
 
                string[] noBrowseProps = new string[] { 
                    "Anchor",
                    "AutoSize", 
                    "Dock",
                    "DockPadding",
                    "Height",
                    "Location", 
                    "Name",
                    "Orientation", 
                    "Renderer", 
                    "RowMargin",
                    "Size", 
                    "Visible",
                    "Width",
                };
 
                for (int i = 0; i < noBrowseProps.Length; i++)
                { 
                    prop = (PropertyDescriptor)properties[noBrowseProps[i]]; 
                    if (prop != null)
                    { 
                        properties[noBrowseProps[i]] = TypeDescriptor.CreateProperty(prop.ComponentType, prop, BrowsableAttribute.No, DesignerSerializationVisibilityAttribute.Hidden);
                    }
                }
            } 
            string[] shadowProps = new string[] {
               "Padding", 
               "Visible" 
            };
            Attribute[] empty = new Attribute[0]; 
            for (int i = 0; i < shadowProps.Length; i++)
            {
                prop = (PropertyDescriptor)properties[shadowProps[i]];
                if (prop != null) 
                {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(ToolStripPanelDesigner), prop, empty); 
                } 
            }
        } 

        /// <devdoc>
        ///      Should Serialize Padding
        /// </devdoc> 
        private bool ShouldSerializePadding()
        { 
            Padding padding = (Padding)ShadowProperties["Padding"]; 
            return !padding.Equals(_defaultPadding);
        } 

        /// <devdoc>
        ///      Should serialize for visible property
        /// </devdoc> 
        private bool ShouldSerializeVisible()
        { 
            return !Visible; 
        }
 

    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
