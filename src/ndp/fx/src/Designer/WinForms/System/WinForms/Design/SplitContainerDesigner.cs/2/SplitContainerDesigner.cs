//------------------------------------------------------------------------------ 
// <copyright file="SplitContainerDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.SplitContainerDesigner..ctor()")]
 
namespace System.Windows.Forms.Design {
    using System.Design;
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Collections;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis; 
    using System;
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Design;
    using System.Windows.Forms; 
    using Microsoft.Win32;
    using System.Windows.Forms.Design.Behavior; 
 
    /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner"]/*' />
    /// <devdoc> 
    ///      This class handles all design time behavior for the SplitContainer class.  This
    ///      draws a visible border on the splitter if it doesn't have a border so the
    ///      user knows where the boundaries of the splitter lie.
    /// </devdoc> 
    internal class SplitContainerDesigner : ParentControlDesigner {
 
        private const string panel1Name = "Panel1"; 
        private const string panel2Name = "Panel2";
        // 
        // The Desinger host ....
        //
        IDesignerHost designerHost;
 
        //
        // The Control for which this is the Designer... 
        // 
        SplitContainer splitContainer;
 
        //
        // SplitterPanels....
        //
 
        SplitterPanel  selectedPanel;
        private static int numberOfSplitterPanels = 2; 
        SplitterPanel splitterPanel1, splitterPanel2; 

        // 
        // The Container Shouldnt Show any GRIDs in the Splitter Region ...
        //
        private bool  disableDrawGrid = false;
 
        //
        // 
        // 
        private bool disabledGlyphs = false; //did we disable glyphs as part of the user moving the splitter
 
        //
        // Hittest Boolean...
        //
        private bool splitContainerSelected = false; 

        // To deal with checkout cancelation 
        private int initialSplitterDist = 0; 
        private bool splitterDistanceException = false;
 
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                      PROPERTIES ..                                                      //
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
 

        /// <devdoc> 
        ///     SplitContainer designer action list property.  Gets the design-time supported actions on the control. 
        /// </devdoc>
        public override DesignerActionListCollection ActionLists 
        {
            get
            {
                DesignerActionListCollection actions = new DesignerActionListCollection(); 
                //heres our action list we'll use
                OrientationActionList orientationAction = new OrientationActionList(this); 
                actions.Add(orientationAction); 
                return actions;
            } 
        }

        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.AllowControlLasso"]/*' />
        /// <devdoc> 
        ///     The SplitContainerDesigner will not re-parent any controls that are within it's lasso at
        ///     creation time. 
        /// </devdoc> 
        protected override bool AllowControlLasso {
            get { 
                return false;
            }
        }
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.DrawGrid"]/*' />
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

        /// <devdoc> 
        ///     This property is used by deriving classes to determine if it returns the control being designed or some other Container ...
        ///     while adding a component to it.
        ///     e.g: When SplitContainer is selected and a component is being added ... the SplitContainer designer would return a
        ///     SelectedPanel as the ParentControl for all the items being added rather than itself. 
        /// </devdoc>
        protected override Control GetParentForComponent(IComponent component) { 
            return splitterPanel1; 
        }
 
        /// <include file='doc\SplitterContainerDesigner.uex' path='docs/doc[@for="SplitterContainerDesigner.SnapLines"]/*' />
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
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.NumberOfInternalControlDesigners"]/*' />
        /// <devdoc> 
        ///     Returns the number of internal control designers in the SplitContainerDesigner. An internal control
        ///     is a control that is not in the IDesignerHost.Container.Components collection.
        ///     We use this to get SnapLines for the internal control designers.
        /// </devdoc> 

        public override int NumberOfInternalControlDesigners() { 
            return numberOfSplitterPanels; 
        }
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.InternalControlDesigner"]/*' />
        /// <devdoc>
        ///     Returns the internal control designer with the specified index in the ControlDesigner.
        /// 
        ///     internalControlIndex is zero-based.
        /// </devdoc> 
 
        public override ControlDesigner InternalControlDesigner(int internalControlIndex) {
 
            SplitterPanel panel;

            switch (internalControlIndex) {
                case 0: 
                    panel = splitterPanel1;
                    break; 
                case 1: 
                    panel = splitterPanel2;
                    break; 
                default:
                    return null;
            }
 
            return(designerHost.GetDesigner(panel) as ControlDesigner);
        } 
 

 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.Selected"]/*' />
        /// <devdoc>
        ///     This is the internal Property which stores the currently selected panel.If the
        ///     user double clicks a controls it is placed in the SelectedPanel. 
        /// </devdoc>
        internal SplitterPanel Selected { 
            get { 
                return selectedPanel;
            } 
            set {
                if (selectedPanel != null) {
                    SplitterPanelDesigner panelDesigner1 = (SplitterPanelDesigner)designerHost.GetDesigner(selectedPanel);
                    panelDesigner1.Selected = false; 
                }
                if (value != null) 
                { 
                    SplitterPanelDesigner panelDesigner = (SplitterPanelDesigner)designerHost.GetDesigner(value);
                    selectedPanel = value; 
                    panelDesigner.Selected = true;
                }
                else { //value == null
                    if (selectedPanel != null) 
                    {
                        SplitterPanelDesigner panelDesigner = (SplitterPanelDesigner)designerHost.GetDesigner(selectedPanel); 
                        selectedPanel = null; 
                        panelDesigner.Selected = false;
                    } 
                }
            }
        }
 
        /// <summary>
        ///  The ToolStripItems are the associated components. 
        ///  We want those to come with in any cut, copy opreations. 
        /// </summary>
        public override System.Collections.ICollection AssociatedComponents 
        {
           get
           {
               ArrayList components = new ArrayList(); 
               foreach (SplitterPanel panel in splitContainer.Controls) {
                    foreach(Control c in panel.Controls) 
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
        //                                     Start Overrides                                          // 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
        protected override void OnDragEnter(DragEventArgs de) { 
            de.Effect = DragDropEffects.None;
        } 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.CreateToolCore"]/*' />
        /// <devdoc>
        ///      This is the worker method of all CreateTool methods.  It is the only one
        ///      that can be overridden. 
        /// </devdoc>
        protected override IComponent[] CreateToolCore(ToolboxItem tool, int x, int y, int width, int height, bool hasLocation, bool hasSize) { 
 
            // We invoke the drag drop handler for this.  This implementation is shared between all designers that
            // create components. 
            //
            if (this.Selected == null)
            {
                this.Selected = splitterPanel1; 
            }
            SplitterPanelDesigner selectedPanelDesigner = (SplitterPanelDesigner)designerHost.GetDesigner(this.Selected); 
            InvokeCreateTool(selectedPanelDesigner, tool); 

            //return Dummy null as the InvokeCreateTool of SPliiterPanel would do the necessary hookups. 
            return null;

        }
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.Dispose"]/*' />
        /// <devdoc> 
        ///     Disposes of this designer. 
        /// </devdoc>
        protected override void Dispose(bool disposing) { 
            ISelectionService svc = (ISelectionService)GetService(typeof(ISelectionService));
            if (svc != null) {
                svc.SelectionChanged -= new EventHandler(this.OnSelectionChanged);
            } 

            splitContainer.MouseDown -= new MouseEventHandler(this.OnSplitContainer); 
            splitContainer.SplitterMoved -= new SplitterEventHandler(this.OnSplitterMoved); 
            splitContainer.SplitterMoving -= new SplitterCancelEventHandler(this.OnSplitterMoving);
            splitContainer.DoubleClick -= new EventHandler(this.OnSplitContainerDoubleClick); 

            base.Dispose(disposing);
        }
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.GetHitTest"]/*' />
        /// <devdoc> 
        /// 

        protected override bool GetHitTest(Point point) { 
              if (!(InheritanceAttribute == InheritanceAttribute.InheritedReadOnly)) {
                return splitContainerSelected;
              }
              return false; 
        }
 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetBodyGlyph"]/*' />
        /// <devdoc> 
        ///     Returns a 'BodyGlyph' representing the bounds of this control.
        ///     The BodyGlyph is responsible for hit testing the related CtrlDes
        ///     and forwarding messages directly to the designer.
        /// </devdoc> 
        protected override ControlBodyGlyph GetControlGlyph(GlyphSelectionType selectionType) {
            ControlBodyGlyph bodyGlyph = null; 
            SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager)); 
            if (selMgr != null)
            { 
                Rectangle translatedBounds = BehaviorService.ControlRectInAdornerWindow(splitterPanel1);
                SplitterPanelDesigner panelDesigner = designerHost.GetDesigner(splitterPanel1) as SplitterPanelDesigner;
                OnSetCursor();
 
                if (panelDesigner != null)
                { 
                    //create our glyph, and set its cursor appropriately 
                    bodyGlyph = new ControlBodyGlyph(translatedBounds, Cursor.Current, splitterPanel1, panelDesigner);
 

                    selMgr.BodyGlyphAdorner.Glyphs.Add(bodyGlyph);
                }
 
                translatedBounds = BehaviorService.ControlRectInAdornerWindow(splitterPanel2);
                panelDesigner = designerHost.GetDesigner(splitterPanel2) as SplitterPanelDesigner; 
 
                if (panelDesigner != null)
                { 
                    //create our glyph, and set its cursor appropriately
                    bodyGlyph = new ControlBodyGlyph(translatedBounds, Cursor.Current, splitterPanel2, panelDesigner);

                    selMgr.BodyGlyphAdorner.Glyphs.Add(bodyGlyph); 
                }
            } 
            return base.GetControlGlyph(selectionType); 
        }
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.Initialize"]/*' />
        /// <devdoc>
        ///     Called by the host when we're first initialized.
        /// </devdoc> 
        public override void Initialize(IComponent component) {
            base.Initialize(component); 
 
            AutoResizeHandles = true;
 
            splitContainer = component as SplitContainer;
            Debug.Assert(splitContainer != null, "Component must be a non-null SplitContainer, it is a: "+component.GetType().FullName);
            splitterPanel1 = splitContainer.Panel1;
            splitterPanel2 = splitContainer.Panel2; 

            EnableDesignMode(splitContainer.Panel1, panel1Name); 
            EnableDesignMode(splitContainer.Panel2, panel2Name); 

            designerHost = (IDesignerHost)component.Site.GetService(typeof(IDesignerHost)); 

            if (selectedPanel == null) {
                this.Selected = splitterPanel1;
            } 

            splitContainer.MouseDown += new MouseEventHandler(this.OnSplitContainer); 
            splitContainer.SplitterMoved += new SplitterEventHandler(this.OnSplitterMoved); 
            splitContainer.SplitterMoving += new System.Windows.Forms.SplitterCancelEventHandler(OnSplitterMoving);
            splitContainer.DoubleClick += new EventHandler(this.OnSplitContainerDoubleClick); 


            ISelectionService svc = (ISelectionService)GetService(typeof(ISelectionService));
            if (svc != null) { 
                svc.SelectionChanged += new EventHandler(this.OnSelectionChanged);
            } 
 
        }
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.OnPaintAdornments"]/*' />
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


 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                     End Overrides                                                            // 
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 

 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                     Private Implementations                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
        /// <devdoc>
        ///     Determines if the this designer can parent to the specified desinger -- 
        ///     generally this means if the control for this designer can parent the 
        ///     given ControlDesigner's designer.
        /// </devdoc> 
        public override bool CanParent(Control control) {
            return false;
        }
 

        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.OnSplitContainer"]/*' /> 
        /// <devdoc> 
        ///     Called when the user clicks the SplitContainer.
        /// </devdoc> 
        private void OnSplitContainer(object sender, MouseEventArgs e) {
            ISelectionService svc = (ISelectionService)GetService(typeof(ISelectionService));
            svc.SetSelectedComponents(new Object[] {(Control)});
        } 

        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.OnSplitContainerDoubleClick"]/*' /> 
        /// <devdoc> 
        ///     Called when the user clicks the SplitContainer.
        /// </devdoc> 
        // Standard 'catch all - rethrow critical' exception pattern
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        private void OnSplitContainerDoubleClick(object sender, EventArgs e) { 
            if (splitContainerSelected)
            { 
               try { 
                    DoDefaultAction();
                } 
                catch (Exception ex) {
                    if (ClientUtils.IsCriticalException(ex)) {
                        throw;
                    } 
                    else
                    { 
                        DisplayError(ex); 
                    }
                } 
            }
        }

        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.OnSplitterMoved"]/*' /> 
        /// <devdoc>
        ///     Called when the user Moves the splitter in the Design Mode. 
        /// </devdoc> 
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
        private void OnSplitterMoved(object sender, SplitterEventArgs e) {
            if (InheritanceAttribute == InheritanceAttribute.InheritedReadOnly || splitterDistanceException) {
                return;
            } 
            try
            { 
                base.RaiseComponentChanging(TypeDescriptor.GetProperties(splitContainer)["SplitterDistance"]); 
                base.RaiseComponentChanged(TypeDescriptor.GetProperties(splitContainer)["SplitterDistance"], null, null);
 
                //enable all adorners except for bodyglyph adorner
                //
                //but only if we turned off the adorners
                if (disabledGlyphs) { 
                    foreach (Adorner a in BehaviorService.Adorners ) {
                        a.Enabled = true; 
                    } 

                    //Refresh the All Selection Glyphs 
                    SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager));
                    if (selMgr != null)
                    {
                        selMgr.Refresh(); 
                    }
 
                    disabledGlyphs = false; 
                }
            } 
            catch (System.InvalidOperationException ex) {
                IUIService uiService = (IUIService) this.Component.Site.GetService(typeof(IUIService));
                uiService.ShowError(ex.Message);
            } 
            catch (CheckoutException checkoutException) {
                if (checkoutException == CheckoutException.Canceled) { 
                    try { 
                        splitterDistanceException = true;
                        splitContainer.SplitterDistance = initialSplitterDist; 
                    }
                    finally {
                       splitterDistanceException = false;
                    } 
                }
                else 
                    throw; 
            }
        } 

        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.OnSplitterMoved"]/*' />
        /// <devdoc>
        ///     Called when the user Moves the splitter in the Design Mode. 
        /// </devdoc>
        private void OnSplitterMoving(object sender, SplitterCancelEventArgs e) 
        { 
            initialSplitterDist = splitContainer.SplitterDistance;
 
            if (InheritanceAttribute == InheritanceAttribute.InheritedReadOnly) {
                return;
            }
 
            //We are moving the splitter via the mouse or key and not as a result of resize of
            //the container itself (through the resizebehavior::onmousemove) 
            disabledGlyphs = true; 

            //find our bodyglyph adorner offered by the behavior service 
            //we don't want to disable the transparent body glyphs
            //
            Adorner bodyGlyphAdorner = null;
            SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager)); 
            if (selMgr != null) {
                bodyGlyphAdorner = selMgr.BodyGlyphAdorner; 
            } 

            //disable all adorners except for bodyglyph adorner 
            //
            foreach (Adorner a in BehaviorService.Adorners) {
                if (bodyGlyphAdorner != null && a.Equals(bodyGlyphAdorner)) {
                    continue; 
                }
                a.Enabled = false; 
            } 

            //From the BodyAdorners Remove all Glyphs Except the ones for SplitterPanels. 
            ArrayList glyphsToRemove = new ArrayList();
            foreach (ControlBodyGlyph g in bodyGlyphAdorner.Glyphs) {
                if (!(g.RelatedComponent is SplitterPanel))
                { 
                    glyphsToRemove.Add(g);
                } 
            } 

            foreach (Glyph g in glyphsToRemove) { 
                bodyGlyphAdorner.Glyphs.Remove(g);
            }
        }
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.OnSelectionChanged"]/*' />
        /// <devdoc> 
        ///      Called when the current selection changes.  Here we check to 
        ///      see if the newly selected component is one of our Panels.  If it
        ///      is, we make sure that the tab is the currently visible tab. 
        /// </devdoc>
        private void OnSelectionChanged(Object sender, EventArgs e) {
            ISelectionService svc = (ISelectionService)GetService( typeof(ISelectionService) );
            splitContainerSelected = false; 

            if (svc != null) { 
                ICollection selComponents = svc.GetSelectedComponents(); 
                foreach(object comp in selComponents) {
                    SplitterPanel panel = CheckIfPanelSelected(comp); 
                    if (panel != null && panel.Parent == splitContainer) {
                        splitContainerSelected = false;
                        this.Selected = panel;
                        break; 
                    }
                    else { 
                        this.Selected = null; 
                    }
                    if (comp == splitContainer) { 
                        splitContainerSelected = true;//this is for HitTest purposes
                        break;
                    }
                } 
            }
        } 
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.GetTabPageOfComponent"]/*' />
        /// <devdoc> 
        ///     Given a component, this retrieves the splitter panel that it's parented to, or
        ///     null if it's not parented to any splitter panel.
        /// </devdoc>
        private static SplitterPanel CheckIfPanelSelected(object comp) { 
            return comp as SplitterPanel;
        } 
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.SplitterPanelHover"]/*' />
        /// <devdoc> 
        ///     Called when one of the child splitter panels receives a MouseHover message.  Here,
        ///     we will simply call the parenting SplitContainer.OnMouseHover so we can get a
        ///     grab handle for moving this thing around.
        /// </devdoc> 
        internal void SplitterPanelHover() {
            this.OnMouseHover(); 
        } 

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
        //                                     Private Implementation                                    //
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <devdoc> 
        ///     This class is used to provide the horz or vert splitter orientation
        ///     action items. 
        /// </devdoc> 
        private class OrientationActionList : DesignerActionList {
 
            private string actionName;
            private SplitContainerDesigner owner;
            private Component ownerComponent;
 
            /// <devdoc>
            ///     Caches off the localized name of our action 
            /// </devdoc> 
            [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
            public OrientationActionList(SplitContainerDesigner owner) 
                : base(owner.Component)
            {
                this.owner = owner;
                this.ownerComponent = owner.Component as Component; 
                //Set the initial ActionName
                if (ownerComponent != null) 
                { 
                    PropertyDescriptor orientationProp = TypeDescriptor.GetProperties(ownerComponent)["Orientation"];
                    if (orientationProp != null) { 
                        bool needsVertical = ((Orientation)orientationProp.GetValue(ownerComponent)) == Orientation.Horizontal;
                        actionName = needsVertical ? SR.GetString(SR.DesignerShortcutVerticalOrientation) :
                                                 SR.GetString(SR.DesignerShortcutHorizontalOrientation);
                    } 
                }
            } 
 
            /// <devdoc>
            ///     Called when our SplitterOrientation DesignerActions are clicked.  Here, we set the 
            ///     control's orientation appropriately (either Horizontal or Vertical).
            /// </devdoc>
            [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
            private void OnOrientationActionClick(object sender, EventArgs e) 
            {
                DesignerVerb verb = sender as DesignerVerb; 
                if (verb != null) { 
                    Orientation orientation = verb.Text.Equals(SR.GetString(SR.DesignerShortcutHorizontalOrientation)) ? Orientation.Horizontal : Orientation.Vertical;
 
                    //switch the text of the orientation action from vertical to horizontal or visa-versa
                    actionName = (orientation == Orientation.Horizontal) ?
                                             SR.GetString(SR.DesignerShortcutVerticalOrientation) :
                                             SR.GetString(SR.DesignerShortcutHorizontalOrientation); 

                    //get the prop and actually modify the orientation 
                    PropertyDescriptor orientationProp = TypeDescriptor.GetProperties(ownerComponent)["Orientation"]; 
                    if (orientationProp != null && ((Orientation)orientationProp.GetValue(ownerComponent)) != orientation) {
                        orientationProp.SetValue(ownerComponent, orientation); 
                    }

                    DesignerActionUIService actionUIService = (DesignerActionUIService)owner.GetService(typeof(DesignerActionUIService));
                    if (actionUIService != null) 
                    {
                        actionUIService.Refresh(ownerComponent); 
                    } 
                }
            } 

            /// <devdoc>
            ///     Returns our undock or dock item
            /// </devdoc> 
            public override DesignerActionItemCollection GetSortedActionItems()     {
                DesignerActionItemCollection items = new DesignerActionItemCollection(); 
                items.Add(new DesignerActionVerbItem(new DesignerVerb(actionName, this.OnOrientationActionClick))); 
                return items;
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SplitContainerDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.SplitContainerDesigner..ctor()")]
 
namespace System.Windows.Forms.Design {
    using System.Design;
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Collections;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis; 
    using System;
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Design;
    using System.Windows.Forms; 
    using Microsoft.Win32;
    using System.Windows.Forms.Design.Behavior; 
 
    /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner"]/*' />
    /// <devdoc> 
    ///      This class handles all design time behavior for the SplitContainer class.  This
    ///      draws a visible border on the splitter if it doesn't have a border so the
    ///      user knows where the boundaries of the splitter lie.
    /// </devdoc> 
    internal class SplitContainerDesigner : ParentControlDesigner {
 
        private const string panel1Name = "Panel1"; 
        private const string panel2Name = "Panel2";
        // 
        // The Desinger host ....
        //
        IDesignerHost designerHost;
 
        //
        // The Control for which this is the Designer... 
        // 
        SplitContainer splitContainer;
 
        //
        // SplitterPanels....
        //
 
        SplitterPanel  selectedPanel;
        private static int numberOfSplitterPanels = 2; 
        SplitterPanel splitterPanel1, splitterPanel2; 

        // 
        // The Container Shouldnt Show any GRIDs in the Splitter Region ...
        //
        private bool  disableDrawGrid = false;
 
        //
        // 
        // 
        private bool disabledGlyphs = false; //did we disable glyphs as part of the user moving the splitter
 
        //
        // Hittest Boolean...
        //
        private bool splitContainerSelected = false; 

        // To deal with checkout cancelation 
        private int initialSplitterDist = 0; 
        private bool splitterDistanceException = false;
 
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                      PROPERTIES ..                                                      //
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
 

        /// <devdoc> 
        ///     SplitContainer designer action list property.  Gets the design-time supported actions on the control. 
        /// </devdoc>
        public override DesignerActionListCollection ActionLists 
        {
            get
            {
                DesignerActionListCollection actions = new DesignerActionListCollection(); 
                //heres our action list we'll use
                OrientationActionList orientationAction = new OrientationActionList(this); 
                actions.Add(orientationAction); 
                return actions;
            } 
        }

        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.AllowControlLasso"]/*' />
        /// <devdoc> 
        ///     The SplitContainerDesigner will not re-parent any controls that are within it's lasso at
        ///     creation time. 
        /// </devdoc> 
        protected override bool AllowControlLasso {
            get { 
                return false;
            }
        }
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.DrawGrid"]/*' />
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

        /// <devdoc> 
        ///     This property is used by deriving classes to determine if it returns the control being designed or some other Container ...
        ///     while adding a component to it.
        ///     e.g: When SplitContainer is selected and a component is being added ... the SplitContainer designer would return a
        ///     SelectedPanel as the ParentControl for all the items being added rather than itself. 
        /// </devdoc>
        protected override Control GetParentForComponent(IComponent component) { 
            return splitterPanel1; 
        }
 
        /// <include file='doc\SplitterContainerDesigner.uex' path='docs/doc[@for="SplitterContainerDesigner.SnapLines"]/*' />
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
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.NumberOfInternalControlDesigners"]/*' />
        /// <devdoc> 
        ///     Returns the number of internal control designers in the SplitContainerDesigner. An internal control
        ///     is a control that is not in the IDesignerHost.Container.Components collection.
        ///     We use this to get SnapLines for the internal control designers.
        /// </devdoc> 

        public override int NumberOfInternalControlDesigners() { 
            return numberOfSplitterPanels; 
        }
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.InternalControlDesigner"]/*' />
        /// <devdoc>
        ///     Returns the internal control designer with the specified index in the ControlDesigner.
        /// 
        ///     internalControlIndex is zero-based.
        /// </devdoc> 
 
        public override ControlDesigner InternalControlDesigner(int internalControlIndex) {
 
            SplitterPanel panel;

            switch (internalControlIndex) {
                case 0: 
                    panel = splitterPanel1;
                    break; 
                case 1: 
                    panel = splitterPanel2;
                    break; 
                default:
                    return null;
            }
 
            return(designerHost.GetDesigner(panel) as ControlDesigner);
        } 
 

 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.Selected"]/*' />
        /// <devdoc>
        ///     This is the internal Property which stores the currently selected panel.If the
        ///     user double clicks a controls it is placed in the SelectedPanel. 
        /// </devdoc>
        internal SplitterPanel Selected { 
            get { 
                return selectedPanel;
            } 
            set {
                if (selectedPanel != null) {
                    SplitterPanelDesigner panelDesigner1 = (SplitterPanelDesigner)designerHost.GetDesigner(selectedPanel);
                    panelDesigner1.Selected = false; 
                }
                if (value != null) 
                { 
                    SplitterPanelDesigner panelDesigner = (SplitterPanelDesigner)designerHost.GetDesigner(value);
                    selectedPanel = value; 
                    panelDesigner.Selected = true;
                }
                else { //value == null
                    if (selectedPanel != null) 
                    {
                        SplitterPanelDesigner panelDesigner = (SplitterPanelDesigner)designerHost.GetDesigner(selectedPanel); 
                        selectedPanel = null; 
                        panelDesigner.Selected = false;
                    } 
                }
            }
        }
 
        /// <summary>
        ///  The ToolStripItems are the associated components. 
        ///  We want those to come with in any cut, copy opreations. 
        /// </summary>
        public override System.Collections.ICollection AssociatedComponents 
        {
           get
           {
               ArrayList components = new ArrayList(); 
               foreach (SplitterPanel panel in splitContainer.Controls) {
                    foreach(Control c in panel.Controls) 
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
        //                                     Start Overrides                                          // 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
        protected override void OnDragEnter(DragEventArgs de) { 
            de.Effect = DragDropEffects.None;
        } 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.CreateToolCore"]/*' />
        /// <devdoc>
        ///      This is the worker method of all CreateTool methods.  It is the only one
        ///      that can be overridden. 
        /// </devdoc>
        protected override IComponent[] CreateToolCore(ToolboxItem tool, int x, int y, int width, int height, bool hasLocation, bool hasSize) { 
 
            // We invoke the drag drop handler for this.  This implementation is shared between all designers that
            // create components. 
            //
            if (this.Selected == null)
            {
                this.Selected = splitterPanel1; 
            }
            SplitterPanelDesigner selectedPanelDesigner = (SplitterPanelDesigner)designerHost.GetDesigner(this.Selected); 
            InvokeCreateTool(selectedPanelDesigner, tool); 

            //return Dummy null as the InvokeCreateTool of SPliiterPanel would do the necessary hookups. 
            return null;

        }
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.Dispose"]/*' />
        /// <devdoc> 
        ///     Disposes of this designer. 
        /// </devdoc>
        protected override void Dispose(bool disposing) { 
            ISelectionService svc = (ISelectionService)GetService(typeof(ISelectionService));
            if (svc != null) {
                svc.SelectionChanged -= new EventHandler(this.OnSelectionChanged);
            } 

            splitContainer.MouseDown -= new MouseEventHandler(this.OnSplitContainer); 
            splitContainer.SplitterMoved -= new SplitterEventHandler(this.OnSplitterMoved); 
            splitContainer.SplitterMoving -= new SplitterCancelEventHandler(this.OnSplitterMoving);
            splitContainer.DoubleClick -= new EventHandler(this.OnSplitContainerDoubleClick); 

            base.Dispose(disposing);
        }
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.GetHitTest"]/*' />
        /// <devdoc> 
        /// 

        protected override bool GetHitTest(Point point) { 
              if (!(InheritanceAttribute == InheritanceAttribute.InheritedReadOnly)) {
                return splitContainerSelected;
              }
              return false; 
        }
 
 
        /// <include file='doc\ControlDesigner.uex' path='docs/doc[@for="ControlDesigner.GetBodyGlyph"]/*' />
        /// <devdoc> 
        ///     Returns a 'BodyGlyph' representing the bounds of this control.
        ///     The BodyGlyph is responsible for hit testing the related CtrlDes
        ///     and forwarding messages directly to the designer.
        /// </devdoc> 
        protected override ControlBodyGlyph GetControlGlyph(GlyphSelectionType selectionType) {
            ControlBodyGlyph bodyGlyph = null; 
            SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager)); 
            if (selMgr != null)
            { 
                Rectangle translatedBounds = BehaviorService.ControlRectInAdornerWindow(splitterPanel1);
                SplitterPanelDesigner panelDesigner = designerHost.GetDesigner(splitterPanel1) as SplitterPanelDesigner;
                OnSetCursor();
 
                if (panelDesigner != null)
                { 
                    //create our glyph, and set its cursor appropriately 
                    bodyGlyph = new ControlBodyGlyph(translatedBounds, Cursor.Current, splitterPanel1, panelDesigner);
 

                    selMgr.BodyGlyphAdorner.Glyphs.Add(bodyGlyph);
                }
 
                translatedBounds = BehaviorService.ControlRectInAdornerWindow(splitterPanel2);
                panelDesigner = designerHost.GetDesigner(splitterPanel2) as SplitterPanelDesigner; 
 
                if (panelDesigner != null)
                { 
                    //create our glyph, and set its cursor appropriately
                    bodyGlyph = new ControlBodyGlyph(translatedBounds, Cursor.Current, splitterPanel2, panelDesigner);

                    selMgr.BodyGlyphAdorner.Glyphs.Add(bodyGlyph); 
                }
            } 
            return base.GetControlGlyph(selectionType); 
        }
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.Initialize"]/*' />
        /// <devdoc>
        ///     Called by the host when we're first initialized.
        /// </devdoc> 
        public override void Initialize(IComponent component) {
            base.Initialize(component); 
 
            AutoResizeHandles = true;
 
            splitContainer = component as SplitContainer;
            Debug.Assert(splitContainer != null, "Component must be a non-null SplitContainer, it is a: "+component.GetType().FullName);
            splitterPanel1 = splitContainer.Panel1;
            splitterPanel2 = splitContainer.Panel2; 

            EnableDesignMode(splitContainer.Panel1, panel1Name); 
            EnableDesignMode(splitContainer.Panel2, panel2Name); 

            designerHost = (IDesignerHost)component.Site.GetService(typeof(IDesignerHost)); 

            if (selectedPanel == null) {
                this.Selected = splitterPanel1;
            } 

            splitContainer.MouseDown += new MouseEventHandler(this.OnSplitContainer); 
            splitContainer.SplitterMoved += new SplitterEventHandler(this.OnSplitterMoved); 
            splitContainer.SplitterMoving += new System.Windows.Forms.SplitterCancelEventHandler(OnSplitterMoving);
            splitContainer.DoubleClick += new EventHandler(this.OnSplitContainerDoubleClick); 


            ISelectionService svc = (ISelectionService)GetService(typeof(ISelectionService));
            if (svc != null) { 
                svc.SelectionChanged += new EventHandler(this.OnSelectionChanged);
            } 
 
        }
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.OnPaintAdornments"]/*' />
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


 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                     End Overrides                                                            // 
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 

 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                     Private Implementations                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
        /// <devdoc>
        ///     Determines if the this designer can parent to the specified desinger -- 
        ///     generally this means if the control for this designer can parent the 
        ///     given ControlDesigner's designer.
        /// </devdoc> 
        public override bool CanParent(Control control) {
            return false;
        }
 

        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.OnSplitContainer"]/*' /> 
        /// <devdoc> 
        ///     Called when the user clicks the SplitContainer.
        /// </devdoc> 
        private void OnSplitContainer(object sender, MouseEventArgs e) {
            ISelectionService svc = (ISelectionService)GetService(typeof(ISelectionService));
            svc.SetSelectedComponents(new Object[] {(Control)});
        } 

        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.OnSplitContainerDoubleClick"]/*' /> 
        /// <devdoc> 
        ///     Called when the user clicks the SplitContainer.
        /// </devdoc> 
        // Standard 'catch all - rethrow critical' exception pattern
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        private void OnSplitContainerDoubleClick(object sender, EventArgs e) { 
            if (splitContainerSelected)
            { 
               try { 
                    DoDefaultAction();
                } 
                catch (Exception ex) {
                    if (ClientUtils.IsCriticalException(ex)) {
                        throw;
                    } 
                    else
                    { 
                        DisplayError(ex); 
                    }
                } 
            }
        }

        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.OnSplitterMoved"]/*' /> 
        /// <devdoc>
        ///     Called when the user Moves the splitter in the Design Mode. 
        /// </devdoc> 
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
        private void OnSplitterMoved(object sender, SplitterEventArgs e) {
            if (InheritanceAttribute == InheritanceAttribute.InheritedReadOnly || splitterDistanceException) {
                return;
            } 
            try
            { 
                base.RaiseComponentChanging(TypeDescriptor.GetProperties(splitContainer)["SplitterDistance"]); 
                base.RaiseComponentChanged(TypeDescriptor.GetProperties(splitContainer)["SplitterDistance"], null, null);
 
                //enable all adorners except for bodyglyph adorner
                //
                //but only if we turned off the adorners
                if (disabledGlyphs) { 
                    foreach (Adorner a in BehaviorService.Adorners ) {
                        a.Enabled = true; 
                    } 

                    //Refresh the All Selection Glyphs 
                    SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager));
                    if (selMgr != null)
                    {
                        selMgr.Refresh(); 
                    }
 
                    disabledGlyphs = false; 
                }
            } 
            catch (System.InvalidOperationException ex) {
                IUIService uiService = (IUIService) this.Component.Site.GetService(typeof(IUIService));
                uiService.ShowError(ex.Message);
            } 
            catch (CheckoutException checkoutException) {
                if (checkoutException == CheckoutException.Canceled) { 
                    try { 
                        splitterDistanceException = true;
                        splitContainer.SplitterDistance = initialSplitterDist; 
                    }
                    finally {
                       splitterDistanceException = false;
                    } 
                }
                else 
                    throw; 
            }
        } 

        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.OnSplitterMoved"]/*' />
        /// <devdoc>
        ///     Called when the user Moves the splitter in the Design Mode. 
        /// </devdoc>
        private void OnSplitterMoving(object sender, SplitterCancelEventArgs e) 
        { 
            initialSplitterDist = splitContainer.SplitterDistance;
 
            if (InheritanceAttribute == InheritanceAttribute.InheritedReadOnly) {
                return;
            }
 
            //We are moving the splitter via the mouse or key and not as a result of resize of
            //the container itself (through the resizebehavior::onmousemove) 
            disabledGlyphs = true; 

            //find our bodyglyph adorner offered by the behavior service 
            //we don't want to disable the transparent body glyphs
            //
            Adorner bodyGlyphAdorner = null;
            SelectionManager selMgr = (SelectionManager)GetService(typeof(SelectionManager)); 
            if (selMgr != null) {
                bodyGlyphAdorner = selMgr.BodyGlyphAdorner; 
            } 

            //disable all adorners except for bodyglyph adorner 
            //
            foreach (Adorner a in BehaviorService.Adorners) {
                if (bodyGlyphAdorner != null && a.Equals(bodyGlyphAdorner)) {
                    continue; 
                }
                a.Enabled = false; 
            } 

            //From the BodyAdorners Remove all Glyphs Except the ones for SplitterPanels. 
            ArrayList glyphsToRemove = new ArrayList();
            foreach (ControlBodyGlyph g in bodyGlyphAdorner.Glyphs) {
                if (!(g.RelatedComponent is SplitterPanel))
                { 
                    glyphsToRemove.Add(g);
                } 
            } 

            foreach (Glyph g in glyphsToRemove) { 
                bodyGlyphAdorner.Glyphs.Remove(g);
            }
        }
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.OnSelectionChanged"]/*' />
        /// <devdoc> 
        ///      Called when the current selection changes.  Here we check to 
        ///      see if the newly selected component is one of our Panels.  If it
        ///      is, we make sure that the tab is the currently visible tab. 
        /// </devdoc>
        private void OnSelectionChanged(Object sender, EventArgs e) {
            ISelectionService svc = (ISelectionService)GetService( typeof(ISelectionService) );
            splitContainerSelected = false; 

            if (svc != null) { 
                ICollection selComponents = svc.GetSelectedComponents(); 
                foreach(object comp in selComponents) {
                    SplitterPanel panel = CheckIfPanelSelected(comp); 
                    if (panel != null && panel.Parent == splitContainer) {
                        splitContainerSelected = false;
                        this.Selected = panel;
                        break; 
                    }
                    else { 
                        this.Selected = null; 
                    }
                    if (comp == splitContainer) { 
                        splitContainerSelected = true;//this is for HitTest purposes
                        break;
                    }
                } 
            }
        } 
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.GetTabPageOfComponent"]/*' />
        /// <devdoc> 
        ///     Given a component, this retrieves the splitter panel that it's parented to, or
        ///     null if it's not parented to any splitter panel.
        /// </devdoc>
        private static SplitterPanel CheckIfPanelSelected(object comp) { 
            return comp as SplitterPanel;
        } 
 
        /// <include file='doc\SplitContainerDesigner.uex' path='docs/doc[@for="SplitContainerDesigner.SplitterPanelHover"]/*' />
        /// <devdoc> 
        ///     Called when one of the child splitter panels receives a MouseHover message.  Here,
        ///     we will simply call the parenting SplitContainer.OnMouseHover so we can get a
        ///     grab handle for moving this thing around.
        /// </devdoc> 
        internal void SplitterPanelHover() {
            this.OnMouseHover(); 
        } 

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
        //                                     Private Implementation                                    //
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <devdoc> 
        ///     This class is used to provide the horz or vert splitter orientation
        ///     action items. 
        /// </devdoc> 
        private class OrientationActionList : DesignerActionList {
 
            private string actionName;
            private SplitContainerDesigner owner;
            private Component ownerComponent;
 
            /// <devdoc>
            ///     Caches off the localized name of our action 
            /// </devdoc> 
            [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
            public OrientationActionList(SplitContainerDesigner owner) 
                : base(owner.Component)
            {
                this.owner = owner;
                this.ownerComponent = owner.Component as Component; 
                //Set the initial ActionName
                if (ownerComponent != null) 
                { 
                    PropertyDescriptor orientationProp = TypeDescriptor.GetProperties(ownerComponent)["Orientation"];
                    if (orientationProp != null) { 
                        bool needsVertical = ((Orientation)orientationProp.GetValue(ownerComponent)) == Orientation.Horizontal;
                        actionName = needsVertical ? SR.GetString(SR.DesignerShortcutVerticalOrientation) :
                                                 SR.GetString(SR.DesignerShortcutHorizontalOrientation);
                    } 
                }
            } 
 
            /// <devdoc>
            ///     Called when our SplitterOrientation DesignerActions are clicked.  Here, we set the 
            ///     control's orientation appropriately (either Horizontal or Vertical).
            /// </devdoc>
            [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
            private void OnOrientationActionClick(object sender, EventArgs e) 
            {
                DesignerVerb verb = sender as DesignerVerb; 
                if (verb != null) { 
                    Orientation orientation = verb.Text.Equals(SR.GetString(SR.DesignerShortcutHorizontalOrientation)) ? Orientation.Horizontal : Orientation.Vertical;
 
                    //switch the text of the orientation action from vertical to horizontal or visa-versa
                    actionName = (orientation == Orientation.Horizontal) ?
                                             SR.GetString(SR.DesignerShortcutVerticalOrientation) :
                                             SR.GetString(SR.DesignerShortcutHorizontalOrientation); 

                    //get the prop and actually modify the orientation 
                    PropertyDescriptor orientationProp = TypeDescriptor.GetProperties(ownerComponent)["Orientation"]; 
                    if (orientationProp != null && ((Orientation)orientationProp.GetValue(ownerComponent)) != orientation) {
                        orientationProp.SetValue(ownerComponent, orientation); 
                    }

                    DesignerActionUIService actionUIService = (DesignerActionUIService)owner.GetService(typeof(DesignerActionUIService));
                    if (actionUIService != null) 
                    {
                        actionUIService.Refresh(ownerComponent); 
                    } 
                }
            } 

            /// <devdoc>
            ///     Returns our undock or dock item
            /// </devdoc> 
            public override DesignerActionItemCollection GetSortedActionItems()     {
                DesignerActionItemCollection items = new DesignerActionItemCollection(); 
                items.Add(new DesignerActionVerbItem(new DesignerVerb(actionName, this.OnOrientationActionClick))); 
                return items;
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
