//------------------------------------------------------------------------------ 
// <copyright file="ToolStripContainerDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ToolStripContainerDesigner..ctor()")] 

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
 
    /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner"]/*' />
    /// <devdoc> 
    ///      Designer for ToolStripContainer.
    /// </devdoc>
    internal class ToolStripContainerDesigner : ParentControlDesigner {
 

        private ToolStripPanel          topToolStripPanel; 
        private ToolStripPanel          bottomToolStripPanel; 
        private ToolStripPanel          leftToolStripPanel;
        private ToolStripPanel          rightToolStripPanel; 
        private ToolStripContentPanel   contentToolStripPanel;


        private Control[] panels; 

        private const string topToolStripPanelName = "TopToolStripPanel"; 
        private const string bottomToolStripPanelName = "BottomToolStripPanel"; 
        private const string leftToolStripPanelName = "LeftToolStripPanel";
        private const string rightToolStripPanelName = "RightToolStripPanel"; 
        private const string contentToolStripPanelName = "ContentPanel";

        //
        // The Desinger host .... 
        //
        IDesignerHost designerHost; 
 
        //
        // The SelectionService.. 
        //
        ISelectionService selectionSvc;

        // 
        // The Control for which this is the Designer...
        // 
        ToolStripContainer toolStripContainer; 

        // 
        // The Container Shouldnt Show any GRIDs in the Splitter Region ...
        //
        private bool  disableDrawGrid = false;
 
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                      Shadow Properties ..                                                      // 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////// 

        /// <devdoc> 
        ///     Shadow the TopToolStripPanelVisible property at design-time so that
        ///     we only set the visibility at design time if the user sets it directly
        ///     see VSWhidbey 557977
        /// </devdoc> 
        private bool TopToolStripPanelVisible {
            get { 
                return (bool)ShadowProperties["TopToolStripPanelVisible"]; 
            }
            set { 
                ShadowProperties["TopToolStripPanelVisible"] = value;
                ((ToolStripContainer)Component).TopToolStripPanelVisible = value;
            }
        } 

        /// <devdoc> 
        ///     Shadow the LeftToolStripPanelVisible property at design-time so that 
        ///     we only set the visibility at design time if the user sets it directly
        ///     see VSWhidbey 557977 
        /// </devdoc>
        private bool LeftToolStripPanelVisible {
            get {
                return (bool)ShadowProperties["LeftToolStripPanelVisible"]; 
            }
            set { 
                ShadowProperties["LeftToolStripPanelVisible"] = value; 
                ((ToolStripContainer)Component).LeftToolStripPanelVisible = value;
            } 
        }

        /// <devdoc>
        ///     Shadow the RightToolStripPanelVisible property at design-time so that 
        ///     we only set the visibility at design time if the user sets it directly
        ///     see VSWhidbey 557977 
        /// </devdoc> 
        private bool RightToolStripPanelVisible {
            get { 
                return (bool)ShadowProperties["RightToolStripPanelVisible"];
            }
            set {
                ShadowProperties["RightToolStripPanelVisible"] = value; 
                ((ToolStripContainer)Component).RightToolStripPanelVisible = value;
            } 
        } 

        /// <devdoc> 
        ///     Shadow the BottomToolStripPanelVisible property at design-time so that
        ///     we only set the visibility at design time if the user sets it directly
        ///     see VSWhidbey 557977
        /// </devdoc> 
        private bool BottomToolStripPanelVisible {
            get { 
                return (bool)ShadowProperties["BottomToolStripPanelVisible"]; 
            }
            set { 
                ShadowProperties["BottomToolStripPanelVisible"] = value;
                ((ToolStripContainer)Component).BottomToolStripPanelVisible = value;
            }
        } 

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////// 
        //                      End Shadow Properties ..                                                      // 
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                      PROPERTIES ..                                                      //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <devdoc> 
        ///     Actionlist for ToolStripContainer.
        /// </devdoc> 
 
        public override DesignerActionListCollection ActionLists
        { 
            get
            {
                DesignerActionListCollection actions = new DesignerActionListCollection();
                //heres our action list we'll use 
                ToolStripContainerActionList actionlist = new ToolStripContainerActionList(toolStripContainer);
                actionlist.AutoShow = true; 
                actions.Add(actionlist); 
                return actions;
            } 
        }


        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.AllowControlLasso"]/*' /> 
        /// <devdoc>
        ///     The ToolStripContainerDesigner will re-parent any controls that are within it's lasso at 
        ///     creation time. 
        /// </devdoc>
        protected override bool AllowControlLasso { 
            get {
                return false;
            }
        } 

        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.DrawGrid"]/*' /> 
        /// <devdoc> 
        ///     Override to Turn DrawGrid to False.
        /// </devdoc> 
        protected override bool DrawGrid {
             get {
                 if (disableDrawGrid) {
                     return false; 
                 }
                 return base.DrawGrid; 
             } 
        }
 
        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.SnapLines"]/*' />
        /// <devdoc>
        ///     Returns a list of SnapLine objects representing interesting
        ///     alignment points for this control.  These SnapLines are used 
        ///     to assist in the positioning of the control on a parent's
        ///     surface. 
        /// </devdoc> 
        public override IList SnapLines {
            get { 
                // We don't want padding snaplines, so call directly to the internal method.
                ArrayList snapLines = base.SnapLinesInternal() as ArrayList;
                return snapLines;
            } 
        }
 
        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.NumberOfInternalControlDesigners"]/*' /> 
        /// <devdoc>
        ///     Returns the number of internal control designers in the ToolStripContainerDesigner. An internal control 
        ///     is a control that is not in the IDesignerHost.Container.Components collection.
        ///     We use this to get SnapLines for the internal control designers.
        /// </devdoc>
        public override int NumberOfInternalControlDesigners() { 
            return panels.Length;
        } 
 
        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.InternalControlDesigner"]/*' />
        /// <devdoc> 
        ///     Returns the internal control designer with the specified index in the ControlDesigner.
        ///     internalControlIndex is zero-based.
        /// </devdoc>
        public override ControlDesigner InternalControlDesigner(int internalControlIndex) { 
            if (internalControlIndex < panels.Length && internalControlIndex >= 0) {
                Control panel = panels[internalControlIndex]; 
                return(designerHost.GetDesigner(panel) as ControlDesigner); 
            }
            Debug.Fail("accessed out of bounds"); 
            return null;
        }

        /// <summary> 
        ///  We want those to come with in any cut, copy opreations.
        /// </summary> 
        public override System.Collections.ICollection AssociatedComponents 
        {
           get 
           {
               ArrayList components = new ArrayList();
               foreach (Control parent in toolStripContainer.Controls) {
                    foreach(Control c in parent.Controls) 
                    {
                        components.Add(c); 
                    } 
                }
                return (ICollection)components; 
           }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
        //                                      End Properties                                          //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
 

 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                     Start Overrides                                                          //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.CreateToolCore"]/*' /> 
        /// <devdoc>
        ///      This is the worker method of all CreateTool methods.  It is the only one 
        ///      that can be overridden. 
        /// </devdoc>
        protected override IComponent[] CreateToolCore(ToolboxItem tool, int x, int y, int width, int height, bool hasLocation, bool hasSize) { 

            if (tool != null) {
                Type toolType = tool.GetType(this.designerHost);
 
                if (typeof(StatusStrip).IsAssignableFrom(toolType)) {
                   InvokeCreateTool(GetDesigner(bottomToolStripPanel), tool); 
                } 
                else if (typeof(ToolStrip).IsAssignableFrom(toolType)) {
                   InvokeCreateTool(GetDesigner(topToolStripPanel), tool); 
                }
                else {
                    InvokeCreateTool(GetDesigner(contentToolStripPanel), tool);
                } 
            }
            return null; 
        } 

        /// <devdoc> 
        ///     Determines if the this designer can parent to the specified desinger --
        ///     generally this means if the control for this designer can parent the
        ///     given ControlDesigner's designer.
        /// </devdoc> 
        public override bool CanParent(Control control) {
            return false; 
        } 

 
        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.Dispose"]/*' />
        /// <devdoc>
        ///     Disposes of this designer.
        /// </devdoc> 
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing); 
            if (selectionSvc != null) 
            {
               selectionSvc = null; 
            }
        }

        private ToolStripPanelDesigner GetDesigner(ToolStripPanel panel) { 
            return designerHost.GetDesigner(panel) as ToolStripPanelDesigner;
        } 
 
        private PanelDesigner GetDesigner(ToolStripContentPanel panel) {
            return designerHost.GetDesigner(panel) as PanelDesigner; 
        }

        private ToolStripContainer ContainerParent(Control c)
        { 
            ToolStripContainer parent = null;
            if (c != null && !(c is ToolStripContainer)) 
            { 
                while (c.Parent != null)
                { 
                    if (c.Parent is ToolStripContainer)
                    {
                        parent = c.Parent as ToolStripContainer;
                        break; 
                    }
                    c = c.Parent; 
                } 
            }
            return parent; 
        }

        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.GetBodyGlyph"]/*' />
        /// <devdoc> 
        ///     Returns a 'BodyGlyph' representing the bounds of this control.
        ///     The BodyGlyph is responsible for hit testing the related CtrlDes 
        ///     and forwarding messages directly to the designer. 
        /// </devdoc>
        protected override ControlBodyGlyph GetControlGlyph(GlyphSelectionType selectionType) { 



            SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager)); 
            if (selMgr != null)
            { 
                // 
                //Create BodyGlyphs for all panels
                for (int i =0; i<=4; i++) 
                {
                    Control curentPanel = panels[i];
                    Rectangle translatedBounds = BehaviorService.ControlRectInAdornerWindow(curentPanel);
                    ControlDesigner panelDesigner = InternalControlDesigner(i); 
                    OnSetCursor();
 
                    if (panelDesigner != null) 
                    {
                        //create our glyph, and set its cursor appropriately 
                        ControlBodyGlyph bodyGlyph = new ControlBodyGlyph(translatedBounds, Cursor.Current, curentPanel, panelDesigner);
                        selMgr.BodyGlyphAdorner.Glyphs.Add(bodyGlyph);

                        bool addGlyphs = true; 
                        ICollection selComponents = selectionSvc.GetSelectedComponents();
                        if (!selectionSvc.GetComponentSelected(toolStripContainer)) 
                        { 
                            foreach (object comp in selComponents)
                            { 
                                ToolStripContainer container = ContainerParent(comp as Control);
                                if (container == toolStripContainer)
                                {
                                    addGlyphs = true; 
                                }
                                else 
                                { 
                                    addGlyphs = false;
                                } 
                            }
                        }
                        if (addGlyphs)
                        { 
                            ToolStripPanelDesigner designer = panelDesigner as ToolStripPanelDesigner;
                            if (designer != null) 
                            { 
                                AddPanelSelectionGlyph(designer, selMgr);
                            } 
                        }
                    }
                }
            } 
            return base.GetControlGlyph(selectionType);
        } 
 

        /// <devdoc> 
        ///     Returns the Control or the ParentControl if the component is a ToolStripItem..
        /// </devdoc>
        private Control GetAssociatedControl(Component c)
        { 
            if (c is Control)
            { 
                return c as Control; 
            }
            if (c is ToolStripItem) 
            {
                ToolStripItem item = c as ToolStripItem;
                Control parent = item.GetCurrentParent();
                if (parent == null) 
                {
                    parent = item.Owner; 
                } 
                return parent;
            } 
            return null;
        }

 
        /// <devdoc>
        ///     If the component selected is a ToolStripDropDownItem, then this checks the Bounds of its Dropdown with the glyphBounds if the two 
        ///     overlap. 
        /// </devdoc>
        private bool CheckDropDownBounds(ToolStripDropDownItem dropDownItem, Glyph childGlyph, GlyphCollection glyphs) 
        {
            if (dropDownItem != null)
            {
                Rectangle glyphBounds = childGlyph.Bounds; 
                Rectangle controlBounds = BehaviorService.ControlRectInAdornerWindow(dropDownItem.DropDown);
                if (!glyphBounds.IntersectsWith(controlBounds)) 
                { 
                    glyphs.Insert(0, childGlyph);
                } 
                return true;
            }
            return false;
        } 

        /// <devdoc> 
        ///     Checks if the associatedControlBounds overlap the PanelSelection Glyph bounds. 
        /// </devdoc>
        private bool CheckAssociatedControl(Component c, Glyph childGlyph, GlyphCollection glyphs) 
        {
            bool ret = false;

            ToolStripDropDownItem item = c as ToolStripDropDownItem; 
            if (item != null)
            { 
                ret = CheckDropDownBounds(item, childGlyph, glyphs); 
            }
            if (!ret) 
            {
                Control associatedControl = GetAssociatedControl(c);
                if (associatedControl != null && associatedControl != toolStripContainer)
                { 
                    if (!UnsafeNativeMethods.IsChild(new HandleRef(toolStripContainer, toolStripContainer.Handle), new HandleRef(associatedControl, associatedControl.Handle)))
                    { 
                        Rectangle glyphBounds = childGlyph.Bounds; 
                        Rectangle controlBounds = BehaviorService.ControlRectInAdornerWindow(associatedControl);
                        if ((c == designerHost.RootComponent) || !glyphBounds.IntersectsWith(controlBounds)) 
                        {
                            glyphs.Insert(0, childGlyph);
                        }
                        ret = true; 
                    }
                } 
            } 
            return ret;
        } 


        /// <devdoc>
        ///     This property is used by deriving classes to determine if it returns the control being designed or some other Container ... 
        ///     while adding a component to it.
        ///     e.g: When ToolStripContainer is selected and a component is being added ... this designer would return a 
        ///     the panel depending on the type of component being added. 
        /// </devdoc>
        protected override Control GetParentForComponent(IComponent component) { 
                Type toolType = component.GetType();

                if (typeof(StatusStrip).IsAssignableFrom(toolType)) {
                   return bottomToolStripPanel; 
                }
                else if (typeof(ToolStrip).IsAssignableFrom(toolType)) { 
                   return topToolStripPanel; 
                }
                else { 
                    return contentToolStripPanel;
                }
        }
 
        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.Initialize"]/*' />
        /// <devdoc> 
        ///     Called by the host when we're first initialized. 
        /// </devdoc>
        public override void Initialize(IComponent component) { 
            toolStripContainer = (ToolStripContainer)component;
            base.Initialize(component);
            AutoResizeHandles = true;
 
            Debug.Assert(component is ToolStripContainer, "Component must be a SplitContainer, it is a: "+component.GetType().FullName);
 
            topToolStripPanel = toolStripContainer.TopToolStripPanel; 
            bottomToolStripPanel = toolStripContainer.BottomToolStripPanel;
            leftToolStripPanel = toolStripContainer.LeftToolStripPanel; 
            rightToolStripPanel = toolStripContainer.RightToolStripPanel;
            contentToolStripPanel = toolStripContainer.ContentPanel;

            panels = new Control[] { contentToolStripPanel, leftToolStripPanel, rightToolStripPanel, topToolStripPanel, bottomToolStripPanel }; 

            // add custom bitmaps for the child toolstrippanels. 
            ToolboxBitmapAttribute bottomToolboxBitmapAttribute = new ToolboxBitmapAttribute(typeof(ToolStripPanel), "ToolStripContainer_BottomToolStripPanel.bmp"); 
            ToolboxBitmapAttribute rightToolboxBitmapAttribute = new ToolboxBitmapAttribute(typeof(ToolStripPanel), "ToolStripContainer_RightToolStripPanel.bmp");
            ToolboxBitmapAttribute topToolboxBitmapAttribute = new ToolboxBitmapAttribute(typeof(ToolStripPanel), "ToolStripContainer_TopToolStripPanel.bmp"); 
            ToolboxBitmapAttribute leftToolboxBitmapAttribute = new ToolboxBitmapAttribute(typeof(ToolStripPanel), "ToolStripContainer_LeftToolStripPanel.bmp");


            TypeDescriptor.AddAttributes(bottomToolStripPanel, bottomToolboxBitmapAttribute, new DescriptionAttribute("bottom")); 
            TypeDescriptor.AddAttributes(rightToolStripPanel, rightToolboxBitmapAttribute, new DescriptionAttribute("right"));
            TypeDescriptor.AddAttributes(leftToolStripPanel, leftToolboxBitmapAttribute, new DescriptionAttribute("left")); 
            TypeDescriptor.AddAttributes(topToolStripPanel, topToolboxBitmapAttribute, new DescriptionAttribute("top")); 

 

            EnableDesignMode(topToolStripPanel, topToolStripPanelName);
            EnableDesignMode(bottomToolStripPanel, bottomToolStripPanelName);
            EnableDesignMode(leftToolStripPanel, leftToolStripPanelName); 
            EnableDesignMode(rightToolStripPanel, rightToolStripPanelName);
            EnableDesignMode(contentToolStripPanel, contentToolStripPanelName); 
 
            designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (selectionSvc == null) 
            {
                selectionSvc = (ISelectionService)GetService(typeof(ISelectionService));
            }
 
            if (topToolStripPanel != null)
            { 
 
               ToolStripPanelDesigner panelDesigner = designerHost.GetDesigner(topToolStripPanel) as ToolStripPanelDesigner;
               panelDesigner.ExpandTopPanel(); 
            }

            // Set ShadowProperties
            TopToolStripPanelVisible = toolStripContainer.TopToolStripPanelVisible; 
            LeftToolStripPanelVisible = toolStripContainer.LeftToolStripPanelVisible;
            RightToolStripPanelVisible = toolStripContainer.RightToolStripPanelVisible; 
            BottomToolStripPanelVisible = toolStripContainer.BottomToolStripPanelVisible; 
        }
 
        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.InitializeNewComponent"]/*' />
        /// <devdoc>
        ///     Called by the host when we're first initialized when dropped from the ToolBox.
        /// </devdoc> 
        public override void InitializeNewComponent(IDictionary defaultValues)
        { 
            base.InitializeNewComponent(defaultValues); 

        } 

        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.OnPaintAdornments"]/*' />
        /// <devdoc>
        ///      Overrides our base class. We dont draw the Grids for this Control. Also we Select the Panel1 if nothing is 
        ///      still Selected.
        /// </devdoc> 
        protected override void OnPaintAdornments(PaintEventArgs pe) { 
            try {
               this.disableDrawGrid = true; 

               // we don't want to do this for the tab control designer
               // because you can't drag anything onto it anyway.
               // so we will always return false for draw grid. 
               base.OnPaintAdornments(pe);
 
            } 
            finally {
               this.disableDrawGrid = false; 
            }
        }

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
            PropertyDescriptor prop;
 
            base.PreFilterProperties(properties); 

            // Handle shadowed properties 
            string[] shadowProps = new string[] {
                "TopToolStripPanelVisible",
                "LeftToolStripPanelVisible",
                "RightToolStripPanelVisible", 
                "BottomToolStripPanelVisible"
            }; 
 
            Attribute[] empty = new Attribute[0];
 
            for (int i = 0; i < shadowProps.Length; i++) {
                prop = (PropertyDescriptor)properties[shadowProps[i]];
                if (prop != null) {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(ToolStripContainerDesigner), prop, empty); 
                }
            } 
        } 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                     End Overrides                                            // 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
        //                                     Private Implementations                                  //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
        private void AddPanelSelectionGlyph(ToolStripPanelDesigner designer, SelectionManager selMgr) 
        {
            //now create SelectionGlyph for the panel and add it 
            if (designer != null)
            {
                Glyph childGlyph = designer.GetGlyph();
                if (childGlyph != null) 
                {
                    ICollection selectedComponents = selectionSvc.GetSelectedComponents(); 
                    foreach (object obj in selectedComponents) 
                    {
                        Component c = obj as Component; 
                        if (c != null)
                        {
                            if (!CheckAssociatedControl(c, childGlyph, selMgr.BodyGlyphAdorner.Glyphs))
                            { 
                                selMgr.BodyGlyphAdorner.Glyphs.Insert(0, childGlyph);
                            } 
                        } 
                    }
                } 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ToolStripContainerDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ToolStripContainerDesigner..ctor()")] 

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
 
    /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner"]/*' />
    /// <devdoc> 
    ///      Designer for ToolStripContainer.
    /// </devdoc>
    internal class ToolStripContainerDesigner : ParentControlDesigner {
 

        private ToolStripPanel          topToolStripPanel; 
        private ToolStripPanel          bottomToolStripPanel; 
        private ToolStripPanel          leftToolStripPanel;
        private ToolStripPanel          rightToolStripPanel; 
        private ToolStripContentPanel   contentToolStripPanel;


        private Control[] panels; 

        private const string topToolStripPanelName = "TopToolStripPanel"; 
        private const string bottomToolStripPanelName = "BottomToolStripPanel"; 
        private const string leftToolStripPanelName = "LeftToolStripPanel";
        private const string rightToolStripPanelName = "RightToolStripPanel"; 
        private const string contentToolStripPanelName = "ContentPanel";

        //
        // The Desinger host .... 
        //
        IDesignerHost designerHost; 
 
        //
        // The SelectionService.. 
        //
        ISelectionService selectionSvc;

        // 
        // The Control for which this is the Designer...
        // 
        ToolStripContainer toolStripContainer; 

        // 
        // The Container Shouldnt Show any GRIDs in the Splitter Region ...
        //
        private bool  disableDrawGrid = false;
 
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                      Shadow Properties ..                                                      // 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////// 

        /// <devdoc> 
        ///     Shadow the TopToolStripPanelVisible property at design-time so that
        ///     we only set the visibility at design time if the user sets it directly
        ///     see VSWhidbey 557977
        /// </devdoc> 
        private bool TopToolStripPanelVisible {
            get { 
                return (bool)ShadowProperties["TopToolStripPanelVisible"]; 
            }
            set { 
                ShadowProperties["TopToolStripPanelVisible"] = value;
                ((ToolStripContainer)Component).TopToolStripPanelVisible = value;
            }
        } 

        /// <devdoc> 
        ///     Shadow the LeftToolStripPanelVisible property at design-time so that 
        ///     we only set the visibility at design time if the user sets it directly
        ///     see VSWhidbey 557977 
        /// </devdoc>
        private bool LeftToolStripPanelVisible {
            get {
                return (bool)ShadowProperties["LeftToolStripPanelVisible"]; 
            }
            set { 
                ShadowProperties["LeftToolStripPanelVisible"] = value; 
                ((ToolStripContainer)Component).LeftToolStripPanelVisible = value;
            } 
        }

        /// <devdoc>
        ///     Shadow the RightToolStripPanelVisible property at design-time so that 
        ///     we only set the visibility at design time if the user sets it directly
        ///     see VSWhidbey 557977 
        /// </devdoc> 
        private bool RightToolStripPanelVisible {
            get { 
                return (bool)ShadowProperties["RightToolStripPanelVisible"];
            }
            set {
                ShadowProperties["RightToolStripPanelVisible"] = value; 
                ((ToolStripContainer)Component).RightToolStripPanelVisible = value;
            } 
        } 

        /// <devdoc> 
        ///     Shadow the BottomToolStripPanelVisible property at design-time so that
        ///     we only set the visibility at design time if the user sets it directly
        ///     see VSWhidbey 557977
        /// </devdoc> 
        private bool BottomToolStripPanelVisible {
            get { 
                return (bool)ShadowProperties["BottomToolStripPanelVisible"]; 
            }
            set { 
                ShadowProperties["BottomToolStripPanelVisible"] = value;
                ((ToolStripContainer)Component).BottomToolStripPanelVisible = value;
            }
        } 

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////// 
        //                      End Shadow Properties ..                                                      // 
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                      PROPERTIES ..                                                      //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <devdoc> 
        ///     Actionlist for ToolStripContainer.
        /// </devdoc> 
 
        public override DesignerActionListCollection ActionLists
        { 
            get
            {
                DesignerActionListCollection actions = new DesignerActionListCollection();
                //heres our action list we'll use 
                ToolStripContainerActionList actionlist = new ToolStripContainerActionList(toolStripContainer);
                actionlist.AutoShow = true; 
                actions.Add(actionlist); 
                return actions;
            } 
        }


        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.AllowControlLasso"]/*' /> 
        /// <devdoc>
        ///     The ToolStripContainerDesigner will re-parent any controls that are within it's lasso at 
        ///     creation time. 
        /// </devdoc>
        protected override bool AllowControlLasso { 
            get {
                return false;
            }
        } 

        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.DrawGrid"]/*' /> 
        /// <devdoc> 
        ///     Override to Turn DrawGrid to False.
        /// </devdoc> 
        protected override bool DrawGrid {
             get {
                 if (disableDrawGrid) {
                     return false; 
                 }
                 return base.DrawGrid; 
             } 
        }
 
        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.SnapLines"]/*' />
        /// <devdoc>
        ///     Returns a list of SnapLine objects representing interesting
        ///     alignment points for this control.  These SnapLines are used 
        ///     to assist in the positioning of the control on a parent's
        ///     surface. 
        /// </devdoc> 
        public override IList SnapLines {
            get { 
                // We don't want padding snaplines, so call directly to the internal method.
                ArrayList snapLines = base.SnapLinesInternal() as ArrayList;
                return snapLines;
            } 
        }
 
        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.NumberOfInternalControlDesigners"]/*' /> 
        /// <devdoc>
        ///     Returns the number of internal control designers in the ToolStripContainerDesigner. An internal control 
        ///     is a control that is not in the IDesignerHost.Container.Components collection.
        ///     We use this to get SnapLines for the internal control designers.
        /// </devdoc>
        public override int NumberOfInternalControlDesigners() { 
            return panels.Length;
        } 
 
        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.InternalControlDesigner"]/*' />
        /// <devdoc> 
        ///     Returns the internal control designer with the specified index in the ControlDesigner.
        ///     internalControlIndex is zero-based.
        /// </devdoc>
        public override ControlDesigner InternalControlDesigner(int internalControlIndex) { 
            if (internalControlIndex < panels.Length && internalControlIndex >= 0) {
                Control panel = panels[internalControlIndex]; 
                return(designerHost.GetDesigner(panel) as ControlDesigner); 
            }
            Debug.Fail("accessed out of bounds"); 
            return null;
        }

        /// <summary> 
        ///  We want those to come with in any cut, copy opreations.
        /// </summary> 
        public override System.Collections.ICollection AssociatedComponents 
        {
           get 
           {
               ArrayList components = new ArrayList();
               foreach (Control parent in toolStripContainer.Controls) {
                    foreach(Control c in parent.Controls) 
                    {
                        components.Add(c); 
                    } 
                }
                return (ICollection)components; 
           }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
        //                                      End Properties                                          //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
 

 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                     Start Overrides                                                          //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.CreateToolCore"]/*' /> 
        /// <devdoc>
        ///      This is the worker method of all CreateTool methods.  It is the only one 
        ///      that can be overridden. 
        /// </devdoc>
        protected override IComponent[] CreateToolCore(ToolboxItem tool, int x, int y, int width, int height, bool hasLocation, bool hasSize) { 

            if (tool != null) {
                Type toolType = tool.GetType(this.designerHost);
 
                if (typeof(StatusStrip).IsAssignableFrom(toolType)) {
                   InvokeCreateTool(GetDesigner(bottomToolStripPanel), tool); 
                } 
                else if (typeof(ToolStrip).IsAssignableFrom(toolType)) {
                   InvokeCreateTool(GetDesigner(topToolStripPanel), tool); 
                }
                else {
                    InvokeCreateTool(GetDesigner(contentToolStripPanel), tool);
                } 
            }
            return null; 
        } 

        /// <devdoc> 
        ///     Determines if the this designer can parent to the specified desinger --
        ///     generally this means if the control for this designer can parent the
        ///     given ControlDesigner's designer.
        /// </devdoc> 
        public override bool CanParent(Control control) {
            return false; 
        } 

 
        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.Dispose"]/*' />
        /// <devdoc>
        ///     Disposes of this designer.
        /// </devdoc> 
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing); 
            if (selectionSvc != null) 
            {
               selectionSvc = null; 
            }
        }

        private ToolStripPanelDesigner GetDesigner(ToolStripPanel panel) { 
            return designerHost.GetDesigner(panel) as ToolStripPanelDesigner;
        } 
 
        private PanelDesigner GetDesigner(ToolStripContentPanel panel) {
            return designerHost.GetDesigner(panel) as PanelDesigner; 
        }

        private ToolStripContainer ContainerParent(Control c)
        { 
            ToolStripContainer parent = null;
            if (c != null && !(c is ToolStripContainer)) 
            { 
                while (c.Parent != null)
                { 
                    if (c.Parent is ToolStripContainer)
                    {
                        parent = c.Parent as ToolStripContainer;
                        break; 
                    }
                    c = c.Parent; 
                } 
            }
            return parent; 
        }

        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.GetBodyGlyph"]/*' />
        /// <devdoc> 
        ///     Returns a 'BodyGlyph' representing the bounds of this control.
        ///     The BodyGlyph is responsible for hit testing the related CtrlDes 
        ///     and forwarding messages directly to the designer. 
        /// </devdoc>
        protected override ControlBodyGlyph GetControlGlyph(GlyphSelectionType selectionType) { 



            SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager)); 
            if (selMgr != null)
            { 
                // 
                //Create BodyGlyphs for all panels
                for (int i =0; i<=4; i++) 
                {
                    Control curentPanel = panels[i];
                    Rectangle translatedBounds = BehaviorService.ControlRectInAdornerWindow(curentPanel);
                    ControlDesigner panelDesigner = InternalControlDesigner(i); 
                    OnSetCursor();
 
                    if (panelDesigner != null) 
                    {
                        //create our glyph, and set its cursor appropriately 
                        ControlBodyGlyph bodyGlyph = new ControlBodyGlyph(translatedBounds, Cursor.Current, curentPanel, panelDesigner);
                        selMgr.BodyGlyphAdorner.Glyphs.Add(bodyGlyph);

                        bool addGlyphs = true; 
                        ICollection selComponents = selectionSvc.GetSelectedComponents();
                        if (!selectionSvc.GetComponentSelected(toolStripContainer)) 
                        { 
                            foreach (object comp in selComponents)
                            { 
                                ToolStripContainer container = ContainerParent(comp as Control);
                                if (container == toolStripContainer)
                                {
                                    addGlyphs = true; 
                                }
                                else 
                                { 
                                    addGlyphs = false;
                                } 
                            }
                        }
                        if (addGlyphs)
                        { 
                            ToolStripPanelDesigner designer = panelDesigner as ToolStripPanelDesigner;
                            if (designer != null) 
                            { 
                                AddPanelSelectionGlyph(designer, selMgr);
                            } 
                        }
                    }
                }
            } 
            return base.GetControlGlyph(selectionType);
        } 
 

        /// <devdoc> 
        ///     Returns the Control or the ParentControl if the component is a ToolStripItem..
        /// </devdoc>
        private Control GetAssociatedControl(Component c)
        { 
            if (c is Control)
            { 
                return c as Control; 
            }
            if (c is ToolStripItem) 
            {
                ToolStripItem item = c as ToolStripItem;
                Control parent = item.GetCurrentParent();
                if (parent == null) 
                {
                    parent = item.Owner; 
                } 
                return parent;
            } 
            return null;
        }

 
        /// <devdoc>
        ///     If the component selected is a ToolStripDropDownItem, then this checks the Bounds of its Dropdown with the glyphBounds if the two 
        ///     overlap. 
        /// </devdoc>
        private bool CheckDropDownBounds(ToolStripDropDownItem dropDownItem, Glyph childGlyph, GlyphCollection glyphs) 
        {
            if (dropDownItem != null)
            {
                Rectangle glyphBounds = childGlyph.Bounds; 
                Rectangle controlBounds = BehaviorService.ControlRectInAdornerWindow(dropDownItem.DropDown);
                if (!glyphBounds.IntersectsWith(controlBounds)) 
                { 
                    glyphs.Insert(0, childGlyph);
                } 
                return true;
            }
            return false;
        } 

        /// <devdoc> 
        ///     Checks if the associatedControlBounds overlap the PanelSelection Glyph bounds. 
        /// </devdoc>
        private bool CheckAssociatedControl(Component c, Glyph childGlyph, GlyphCollection glyphs) 
        {
            bool ret = false;

            ToolStripDropDownItem item = c as ToolStripDropDownItem; 
            if (item != null)
            { 
                ret = CheckDropDownBounds(item, childGlyph, glyphs); 
            }
            if (!ret) 
            {
                Control associatedControl = GetAssociatedControl(c);
                if (associatedControl != null && associatedControl != toolStripContainer)
                { 
                    if (!UnsafeNativeMethods.IsChild(new HandleRef(toolStripContainer, toolStripContainer.Handle), new HandleRef(associatedControl, associatedControl.Handle)))
                    { 
                        Rectangle glyphBounds = childGlyph.Bounds; 
                        Rectangle controlBounds = BehaviorService.ControlRectInAdornerWindow(associatedControl);
                        if ((c == designerHost.RootComponent) || !glyphBounds.IntersectsWith(controlBounds)) 
                        {
                            glyphs.Insert(0, childGlyph);
                        }
                        ret = true; 
                    }
                } 
            } 
            return ret;
        } 


        /// <devdoc>
        ///     This property is used by deriving classes to determine if it returns the control being designed or some other Container ... 
        ///     while adding a component to it.
        ///     e.g: When ToolStripContainer is selected and a component is being added ... this designer would return a 
        ///     the panel depending on the type of component being added. 
        /// </devdoc>
        protected override Control GetParentForComponent(IComponent component) { 
                Type toolType = component.GetType();

                if (typeof(StatusStrip).IsAssignableFrom(toolType)) {
                   return bottomToolStripPanel; 
                }
                else if (typeof(ToolStrip).IsAssignableFrom(toolType)) { 
                   return topToolStripPanel; 
                }
                else { 
                    return contentToolStripPanel;
                }
        }
 
        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.Initialize"]/*' />
        /// <devdoc> 
        ///     Called by the host when we're first initialized. 
        /// </devdoc>
        public override void Initialize(IComponent component) { 
            toolStripContainer = (ToolStripContainer)component;
            base.Initialize(component);
            AutoResizeHandles = true;
 
            Debug.Assert(component is ToolStripContainer, "Component must be a SplitContainer, it is a: "+component.GetType().FullName);
 
            topToolStripPanel = toolStripContainer.TopToolStripPanel; 
            bottomToolStripPanel = toolStripContainer.BottomToolStripPanel;
            leftToolStripPanel = toolStripContainer.LeftToolStripPanel; 
            rightToolStripPanel = toolStripContainer.RightToolStripPanel;
            contentToolStripPanel = toolStripContainer.ContentPanel;

            panels = new Control[] { contentToolStripPanel, leftToolStripPanel, rightToolStripPanel, topToolStripPanel, bottomToolStripPanel }; 

            // add custom bitmaps for the child toolstrippanels. 
            ToolboxBitmapAttribute bottomToolboxBitmapAttribute = new ToolboxBitmapAttribute(typeof(ToolStripPanel), "ToolStripContainer_BottomToolStripPanel.bmp"); 
            ToolboxBitmapAttribute rightToolboxBitmapAttribute = new ToolboxBitmapAttribute(typeof(ToolStripPanel), "ToolStripContainer_RightToolStripPanel.bmp");
            ToolboxBitmapAttribute topToolboxBitmapAttribute = new ToolboxBitmapAttribute(typeof(ToolStripPanel), "ToolStripContainer_TopToolStripPanel.bmp"); 
            ToolboxBitmapAttribute leftToolboxBitmapAttribute = new ToolboxBitmapAttribute(typeof(ToolStripPanel), "ToolStripContainer_LeftToolStripPanel.bmp");


            TypeDescriptor.AddAttributes(bottomToolStripPanel, bottomToolboxBitmapAttribute, new DescriptionAttribute("bottom")); 
            TypeDescriptor.AddAttributes(rightToolStripPanel, rightToolboxBitmapAttribute, new DescriptionAttribute("right"));
            TypeDescriptor.AddAttributes(leftToolStripPanel, leftToolboxBitmapAttribute, new DescriptionAttribute("left")); 
            TypeDescriptor.AddAttributes(topToolStripPanel, topToolboxBitmapAttribute, new DescriptionAttribute("top")); 

 

            EnableDesignMode(topToolStripPanel, topToolStripPanelName);
            EnableDesignMode(bottomToolStripPanel, bottomToolStripPanelName);
            EnableDesignMode(leftToolStripPanel, leftToolStripPanelName); 
            EnableDesignMode(rightToolStripPanel, rightToolStripPanelName);
            EnableDesignMode(contentToolStripPanel, contentToolStripPanelName); 
 
            designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (selectionSvc == null) 
            {
                selectionSvc = (ISelectionService)GetService(typeof(ISelectionService));
            }
 
            if (topToolStripPanel != null)
            { 
 
               ToolStripPanelDesigner panelDesigner = designerHost.GetDesigner(topToolStripPanel) as ToolStripPanelDesigner;
               panelDesigner.ExpandTopPanel(); 
            }

            // Set ShadowProperties
            TopToolStripPanelVisible = toolStripContainer.TopToolStripPanelVisible; 
            LeftToolStripPanelVisible = toolStripContainer.LeftToolStripPanelVisible;
            RightToolStripPanelVisible = toolStripContainer.RightToolStripPanelVisible; 
            BottomToolStripPanelVisible = toolStripContainer.BottomToolStripPanelVisible; 
        }
 
        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.InitializeNewComponent"]/*' />
        /// <devdoc>
        ///     Called by the host when we're first initialized when dropped from the ToolBox.
        /// </devdoc> 
        public override void InitializeNewComponent(IDictionary defaultValues)
        { 
            base.InitializeNewComponent(defaultValues); 

        } 

        /// <include file='doc\ToolStripContainerDesigner.uex' path='docs/doc[@for="ToolStripContainerDesigner.OnPaintAdornments"]/*' />
        /// <devdoc>
        ///      Overrides our base class. We dont draw the Grids for this Control. Also we Select the Panel1 if nothing is 
        ///      still Selected.
        /// </devdoc> 
        protected override void OnPaintAdornments(PaintEventArgs pe) { 
            try {
               this.disableDrawGrid = true; 

               // we don't want to do this for the tab control designer
               // because you can't drag anything onto it anyway.
               // so we will always return false for draw grid. 
               base.OnPaintAdornments(pe);
 
            } 
            finally {
               this.disableDrawGrid = false; 
            }
        }

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
            PropertyDescriptor prop;
 
            base.PreFilterProperties(properties); 

            // Handle shadowed properties 
            string[] shadowProps = new string[] {
                "TopToolStripPanelVisible",
                "LeftToolStripPanelVisible",
                "RightToolStripPanelVisible", 
                "BottomToolStripPanelVisible"
            }; 
 
            Attribute[] empty = new Attribute[0];
 
            for (int i = 0; i < shadowProps.Length; i++) {
                prop = (PropertyDescriptor)properties[shadowProps[i]];
                if (prop != null) {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(ToolStripContainerDesigner), prop, empty); 
                }
            } 
        } 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                     End Overrides                                            // 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
        //                                     Private Implementations                                  //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
        private void AddPanelSelectionGlyph(ToolStripPanelDesigner designer, SelectionManager selMgr) 
        {
            //now create SelectionGlyph for the panel and add it 
            if (designer != null)
            {
                Glyph childGlyph = designer.GetGlyph();
                if (childGlyph != null) 
                {
                    ICollection selectedComponents = selectionSvc.GetSelectedComponents(); 
                    foreach (object obj in selectedComponents) 
                    {
                        Component c = obj as Component; 
                        if (c != null)
                        {
                            if (!CheckAssociatedControl(c, childGlyph, selMgr.BodyGlyphAdorner.Glyphs))
                            { 
                                selMgr.BodyGlyphAdorner.Glyphs.Insert(0, childGlyph);
                            } 
                        } 
                    }
                } 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
