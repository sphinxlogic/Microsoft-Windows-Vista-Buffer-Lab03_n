namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing;
    using System.Windows.Forms.Design; 
    using System.Diagnostics.CodeAnalysis;

    /// <include file='doc\DesignerActionBehavior.uex' path='docs/doc[@for="DesignerActionBehavior"]/*' />
    /// <devdoc> 
    ///     This is the Behavior that represents DesignerActions for a particular
    ///     control.  The DesignerActionBehavior is responsible for responding to the 
    ///     MouseDown message and either 1) selecting the control and changing the 
    ///     DesignerActionGlyph's image or 2) building up a chrome menu
    ///     and requesting it to be shown. 
    ///     Also, this Behavior acts as a proxy between "clicked" context menu
    ///     items and the actual DesignerActions that they represent.
    /// </devdoc>
    internal sealed class DesignerActionBehavior : Behavior { 

        private IComponent relatedComponent;//The component we are bound to 
        private DesignerActionUI parentUI;//ptr to the parenting UI, used for showing menus and setting selection 

        private DesignerActionListCollection actionLists;//all the shortcuts! 
        private IServiceProvider serviceProvider; // we need to cache the service provider here to be able to create the panel with the proper arguments
        private bool ignoreNextMouseUp = false;

        /// <include file='doc\DesignerActionBehavior.uex' path='docs/doc[@for="DesignerActionBehavior.DesignerActionBehavior"]/*' /> 
        /// <devdoc>
        ///     Constructor that calls base and caches off the action lists. 
        /// </devdoc> 
        internal DesignerActionBehavior(IServiceProvider serviceProvider, IComponent relatedComponent, DesignerActionListCollection actionLists ,DesignerActionUI parentUI)  {
            this.actionLists = actionLists; 
            this.serviceProvider = serviceProvider;
            this.relatedComponent = relatedComponent;
            this.parentUI = parentUI;
        } 

        /// <include file='doc\DesignerActionBehavior.uex' path='docs/doc[@for="DesignerActionBehavior.ActionLists"]/*' /> 
        /// <devdoc> 
        ///     Returns the collection of DesignerActionLists this Behavior is managing.
        ///     These will be dynamically updated (some can be removed, new ones can be 
        ///     added, etc...).
        /// </devdoc>
        internal DesignerActionListCollection ActionLists {
            get { 
                return actionLists;
            } 
            set { 
                actionLists = value;
            } 
        }

        /// <include file='doc\DesignerBehaviorBase.uex' path='docs/doc[@for="DesignerActionBehavior.ParentUI"]/*' />
        /// <devdoc> 
        ///     Returns the parenting UI (a DesignerActionUI)
        /// </devdoc> 
        internal DesignerActionUI ParentUI { 
            get {
                return parentUI; 
            }
        }

        /// <include file='doc\DesignerBehaviorBase.uex' path='docs/doc[@for="DesignerBehaviorBase.RelatedComponent"]/*' /> 
        /// <devdoc>
        ///     Returns the Component that this glyph is attached to. 
        /// </devdoc> 
        internal IComponent RelatedComponent {
            get { 
                return relatedComponent;
            }
        }
 
        /// <include file='doc\DesignerActionBehavior.uex' path='docs/doc[@for="DesignerActionBehavior.HideUI"]/*' />
        /// <devdoc> 
        ///     Hides the designer action panel UI. 
        /// </devdoc>
        internal void HideUI() { 
            ParentUI.HideDesignerActionPanel();
        }

 
        internal DesignerActionPanel CreateDesignerActionPanel(IComponent relatedComponent) {
            // BUILD AND SHOW THE CHROME UI 
            DesignerActionListCollection lists = new DesignerActionListCollection(); 
            lists.AddRange(ActionLists);
 
            DesignerActionPanel dap = new DesignerActionPanel(serviceProvider);
            dap.UpdateTasks(lists, new DesignerActionListCollection(), SR.GetString(SR.DesignerActionPanel_DefaultPanelTitle, relatedComponent.GetType().Name), null);
            return dap;
        } 

 
        /// <include file='doc\DesignerActionBehavior.uex' path='docs/doc[@for="DesignerActionBehavior.ShowUI"]/*' /> 
        /// <devdoc>
        ///     Shows the designer action panel UI associated with this glyph. 
        /// </devdoc>
        internal void ShowUI(Glyph g) {

            DesignerActionGlyph glyph = g as DesignerActionGlyph; 
            if (glyph == null) {
                Debug.Fail("Why are we trying to 'showui' on a glyph that's not a DesignerActionGlyph?"); 
                return; 
            }
 
            DesignerActionPanel dap = CreateDesignerActionPanel(RelatedComponent);
            ParentUI.ShowDesignerActionPanel(RelatedComponent, dap, glyph);
        }
 
        internal bool IgnoreNextMouseUp {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
            set { 
                ignoreNextMouseUp = value;
            } 
        }

        public override bool OnMouseDoubleClick(Glyph g, MouseButtons button, Point mouseLoc) {
            ignoreNextMouseUp = true; 
            return true;
        } 
 
        public override bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc) { // we take the msg
                return (!ParentUI.IsDesignerActionPanelVisible); 
        }


        /// <include file='doc\DesignerActionBehavior.uex' path='docs/doc[@for="DesignerActionBehavior.OnMouseUp"]/*' /> 
        /// <devdoc>
        ///     In response to a MouseUp, we will either 1) select the Glyph 
        ///     and control if not selected, or 2) Build up our context menu 
        ///     representing our DesignerActions and show it.
        /// </devdoc> 
        public override bool OnMouseUp(Glyph g, MouseButtons button) {
            if (button != MouseButtons.Left || ParentUI == null) {
               return true;
            } 
            bool returnValue = true;
            if(ParentUI.IsDesignerActionPanelVisible) { 
                HideUI(); 
            } else if(!ignoreNextMouseUp) {
                if(serviceProvider != null) { 
                    ISelectionService selectionService = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService));
                    if(selectionService != null) {
                        if(selectionService.PrimarySelection != RelatedComponent) {
                            List<IComponent> componentList = new List<IComponent>(); 
                            componentList.Add(RelatedComponent);
                            selectionService.SetSelectedComponents(componentList, SelectionTypes.Primary); 
                        } 
                    }
                } 
                ShowUI(g);
            } else {
                returnValue = false;
            } 
            ignoreNextMouseUp = false;
 
 
            return returnValue;
        } 
    }


 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing;
    using System.Windows.Forms.Design; 
    using System.Diagnostics.CodeAnalysis;

    /// <include file='doc\DesignerActionBehavior.uex' path='docs/doc[@for="DesignerActionBehavior"]/*' />
    /// <devdoc> 
    ///     This is the Behavior that represents DesignerActions for a particular
    ///     control.  The DesignerActionBehavior is responsible for responding to the 
    ///     MouseDown message and either 1) selecting the control and changing the 
    ///     DesignerActionGlyph's image or 2) building up a chrome menu
    ///     and requesting it to be shown. 
    ///     Also, this Behavior acts as a proxy between "clicked" context menu
    ///     items and the actual DesignerActions that they represent.
    /// </devdoc>
    internal sealed class DesignerActionBehavior : Behavior { 

        private IComponent relatedComponent;//The component we are bound to 
        private DesignerActionUI parentUI;//ptr to the parenting UI, used for showing menus and setting selection 

        private DesignerActionListCollection actionLists;//all the shortcuts! 
        private IServiceProvider serviceProvider; // we need to cache the service provider here to be able to create the panel with the proper arguments
        private bool ignoreNextMouseUp = false;

        /// <include file='doc\DesignerActionBehavior.uex' path='docs/doc[@for="DesignerActionBehavior.DesignerActionBehavior"]/*' /> 
        /// <devdoc>
        ///     Constructor that calls base and caches off the action lists. 
        /// </devdoc> 
        internal DesignerActionBehavior(IServiceProvider serviceProvider, IComponent relatedComponent, DesignerActionListCollection actionLists ,DesignerActionUI parentUI)  {
            this.actionLists = actionLists; 
            this.serviceProvider = serviceProvider;
            this.relatedComponent = relatedComponent;
            this.parentUI = parentUI;
        } 

        /// <include file='doc\DesignerActionBehavior.uex' path='docs/doc[@for="DesignerActionBehavior.ActionLists"]/*' /> 
        /// <devdoc> 
        ///     Returns the collection of DesignerActionLists this Behavior is managing.
        ///     These will be dynamically updated (some can be removed, new ones can be 
        ///     added, etc...).
        /// </devdoc>
        internal DesignerActionListCollection ActionLists {
            get { 
                return actionLists;
            } 
            set { 
                actionLists = value;
            } 
        }

        /// <include file='doc\DesignerBehaviorBase.uex' path='docs/doc[@for="DesignerActionBehavior.ParentUI"]/*' />
        /// <devdoc> 
        ///     Returns the parenting UI (a DesignerActionUI)
        /// </devdoc> 
        internal DesignerActionUI ParentUI { 
            get {
                return parentUI; 
            }
        }

        /// <include file='doc\DesignerBehaviorBase.uex' path='docs/doc[@for="DesignerBehaviorBase.RelatedComponent"]/*' /> 
        /// <devdoc>
        ///     Returns the Component that this glyph is attached to. 
        /// </devdoc> 
        internal IComponent RelatedComponent {
            get { 
                return relatedComponent;
            }
        }
 
        /// <include file='doc\DesignerActionBehavior.uex' path='docs/doc[@for="DesignerActionBehavior.HideUI"]/*' />
        /// <devdoc> 
        ///     Hides the designer action panel UI. 
        /// </devdoc>
        internal void HideUI() { 
            ParentUI.HideDesignerActionPanel();
        }

 
        internal DesignerActionPanel CreateDesignerActionPanel(IComponent relatedComponent) {
            // BUILD AND SHOW THE CHROME UI 
            DesignerActionListCollection lists = new DesignerActionListCollection(); 
            lists.AddRange(ActionLists);
 
            DesignerActionPanel dap = new DesignerActionPanel(serviceProvider);
            dap.UpdateTasks(lists, new DesignerActionListCollection(), SR.GetString(SR.DesignerActionPanel_DefaultPanelTitle, relatedComponent.GetType().Name), null);
            return dap;
        } 

 
        /// <include file='doc\DesignerActionBehavior.uex' path='docs/doc[@for="DesignerActionBehavior.ShowUI"]/*' /> 
        /// <devdoc>
        ///     Shows the designer action panel UI associated with this glyph. 
        /// </devdoc>
        internal void ShowUI(Glyph g) {

            DesignerActionGlyph glyph = g as DesignerActionGlyph; 
            if (glyph == null) {
                Debug.Fail("Why are we trying to 'showui' on a glyph that's not a DesignerActionGlyph?"); 
                return; 
            }
 
            DesignerActionPanel dap = CreateDesignerActionPanel(RelatedComponent);
            ParentUI.ShowDesignerActionPanel(RelatedComponent, dap, glyph);
        }
 
        internal bool IgnoreNextMouseUp {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
            set { 
                ignoreNextMouseUp = value;
            } 
        }

        public override bool OnMouseDoubleClick(Glyph g, MouseButtons button, Point mouseLoc) {
            ignoreNextMouseUp = true; 
            return true;
        } 
 
        public override bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc) { // we take the msg
                return (!ParentUI.IsDesignerActionPanelVisible); 
        }


        /// <include file='doc\DesignerActionBehavior.uex' path='docs/doc[@for="DesignerActionBehavior.OnMouseUp"]/*' /> 
        /// <devdoc>
        ///     In response to a MouseUp, we will either 1) select the Glyph 
        ///     and control if not selected, or 2) Build up our context menu 
        ///     representing our DesignerActions and show it.
        /// </devdoc> 
        public override bool OnMouseUp(Glyph g, MouseButtons button) {
            if (button != MouseButtons.Left || ParentUI == null) {
               return true;
            } 
            bool returnValue = true;
            if(ParentUI.IsDesignerActionPanelVisible) { 
                HideUI(); 
            } else if(!ignoreNextMouseUp) {
                if(serviceProvider != null) { 
                    ISelectionService selectionService = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService));
                    if(selectionService != null) {
                        if(selectionService.PrimarySelection != RelatedComponent) {
                            List<IComponent> componentList = new List<IComponent>(); 
                            componentList.Add(RelatedComponent);
                            selectionService.SetSelectedComponents(componentList, SelectionTypes.Primary); 
                        } 
                    }
                } 
                ShowUI(g);
            } else {
                returnValue = false;
            } 
            ignoreNextMouseUp = false;
 
 
            return returnValue;
        } 
    }


 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
