namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Windows.Forms.Design;
 
    /// <include file='doc\ContainerSelectorBehavior.uex' path='docs/doc[@for="ContainerSelectorBehavior"]/*' />
    /// <devdoc>
    ///     This behavior is associated with the ContainerGlyph offered up
    ///     by ParentControlDesigner.  This Behavior simply 
    ///     starts a new dragdrop behavior.
    /// </devdoc> 
    internal sealed class ContainerSelectorBehavior : Behavior { 

        private Control                 containerControl;           //our related control 
        private IServiceProvider        serviceProvider;            //used for starting a drag/drop
        private BehaviorService         behaviorService;            //ptr to where we start our drag/drop operation
        private bool                    okToMove;                   //state identifying if we are allowed to move the container
        private Point                   initialDragPoint;           //cached "mouse down" point 

        // For VSWhidbey 464206. For some controls, we want to change the original drag point to be the upper-left of the control in 
        // order to make it easier to drop the control at a desired location. But not all controls want this behavior. E.g. we want 
        // to do it for Panel and ToolStrip, but not for Label. Label has a ContainerSelectorBehavior via the NoResizeSelectionBorder
        // glyph. 
        private bool                    setInitialDragPoint;

        /// <include file='doc\ContainerSelectorBehavior.uex' path='docs/doc[@for="ContainerSelectorBehavior.ContainerSelectorBehavior"]/*' />
        /// <devdoc> 
        ///     Constructor, here we cache off all of our member vars and sync
        ///     location & size changes. 
        /// </devdoc> 
        internal ContainerSelectorBehavior(Control containerControl, IServiceProvider serviceProvider) {
            Init(containerControl, serviceProvider); 
            this.setInitialDragPoint = false;
        }

 
        /// <include file='doc\ContainerSelectorBehavior.uex' path='docs/doc[@for="ContainerSelectorBehavior.ContainerSelectorBehavior"]/*' />
        /// <devdoc> 
        ///     Constructor, here we cache off all of our member vars and sync 
        ///     location & size changes.
        /// </devdoc> 
        internal ContainerSelectorBehavior(Control containerControl, IServiceProvider serviceProvider, bool setInitialDragPoint) {
            Init(containerControl, serviceProvider);
            this.setInitialDragPoint = setInitialDragPoint;
        } 

        private void Init(Control containerControl, IServiceProvider serviceProvider) { 
            this.behaviorService = (BehaviorService)serviceProvider.GetService(typeof(BehaviorService)); 
            if (behaviorService == null) {
                Debug.Fail("Could not get the BehaviorService from ContainerSelectroBehavior!"); 
                return;
            }

            this.containerControl = containerControl; 
            this.serviceProvider = serviceProvider;
            this.initialDragPoint = Point.Empty; 
            this.okToMove = false; 
        }
 

        public Control ContainerControl {
            get {
                return containerControl; 
            }
        } 
 
        /// <include file='doc\ContainerSelectorBehavior.uex' path='docs/doc[@for="ContainerSelectorBehavior.OkToMove"]/*' />
        /// <devdoc> 
        ///     This will be true when we detect a mousedown on our glyph.  The Glyph can use this state
        ///     to always return 'true' from hittesting indicating that it would like all messages (like mousemove).
        /// </devdoc>
        public bool OkToMove { 
            get {
                return okToMove; 
            } 

            set { 
                okToMove = value;
            }
        }
 
        public Point InitialDragPoint {
            get { 
                return initialDragPoint; 
            }
 
            set {
                initialDragPoint = value;
            }
        } 

 
        /// <include file='doc\ContainerSelectorBehavior.uex' path='docs/doc[@for="ContainerSelectorBehavior.OnMouseDown"]/*' /> 
        /// <devdoc>
        ///     If the user selects the containerglyph - select our related component. 
        /// </devdoc>
        public override bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc) {
            if (button == MouseButtons.Left) {
                //select our component 
                ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService));
                if (selSvc != null && !containerControl.Equals(selSvc.PrimarySelection as Control)) { 
                    selSvc.SetSelectedComponents(new object[] {containerControl}, SelectionTypes.Primary | SelectionTypes.Toggle); 

                    // VSWhidbey #488545 
                    // Setting the selected component will create a new glyph, so this instance of the glyph won't
                    // receive any more mouse messages. So we need to tell the new glyph what the initialDragPoint and okToMove are.

                    // Sigh.... Here we go. 

                    ContainerSelectorGlyph selOld = g as ContainerSelectorGlyph; 
                    if (selOld == null) { 
                        return false;
                    } 

                    foreach (Adorner a in behaviorService.Adorners) {
                        foreach (Glyph glyph in a.Glyphs) {
                            ContainerSelectorGlyph selNew = glyph as ContainerSelectorGlyph; 
                            if (selNew == null) {
                                continue; 
                            } 

                            // Don't care if we are looking at the same containerselectorglyph 
                            if (selNew.Equals(selOld)) {
                                continue;
                            }
 
                            // Check if the containercontrols are the same
                            ContainerSelectorBehavior behNew = selNew.RelatedBehavior as ContainerSelectorBehavior; 
                            ContainerSelectorBehavior behOld = selOld.RelatedBehavior as ContainerSelectorBehavior; 
                            if (behNew == null || behOld == null) {
                                continue; 
                            }


                            // and the relatedcomponents are the same, then we have found the new glyph that just got added 
                            if (behOld.ContainerControl.Equals(behNew.ContainerControl)) {
                                behNew.OkToMove = true; 
                                behNew.InitialDragPoint = DetermineInitialDragPoint(mouseLoc); 
                                break;
                            } 
                        }
                    }

 
                }
                else { 
                    InitialDragPoint = DetermineInitialDragPoint(mouseLoc); 

                    //set 'okToMove' to true since the user actually clicked down on the glyph 
                    OkToMove = true;
                }
            }
            return false; 
        }
 
        private Point DetermineInitialDragPoint(Point mouseLoc) { 
            if (setInitialDragPoint) {
                // Set the mouse location to be to control's location. 
                Point controlOrigin = behaviorService.ControlToAdornerWindow(containerControl);
                controlOrigin = behaviorService.AdornerWindowPointToScreen(controlOrigin);
                Cursor.Position = controlOrigin;
                return controlOrigin; 
            }
            else { 
                // This really amounts to doing nothing 
                return mouseLoc;
            } 
        }

        /// <include file='doc\ContainerSelectorBehavior.uex' path='docs/doc[@for="ContainerSelectorBehavior.OnMouseMove"]/*' />
        /// <devdoc> 
        ///     We will compare the mouse loc to the initial point (set in onmousedown)
        ///     and if we're far enough, we'll create a dropsourcebehavior object and start 
        ///     out drag operation! 
        /// </devdoc>
        public override bool OnMouseMove(Glyph g, MouseButtons button, Point mouseLoc) { 
            if (button == MouseButtons.Left && OkToMove) {
                if (InitialDragPoint == Point.Empty) {
                    InitialDragPoint = DetermineInitialDragPoint(mouseLoc);
                } 
                Size delta = new Size(Math.Abs(mouseLoc.X - InitialDragPoint.X), Math.Abs(mouseLoc.Y - InitialDragPoint.Y));
                if (delta.Width >= DesignerUtils.MinDragSize.Width/2 || delta.Height >= DesignerUtils.MinDragSize.Height/2) { 
                    //start our drag! 
                    Point screenLoc = behaviorService.AdornerWindowToScreen();
                    screenLoc.Offset(mouseLoc.X, mouseLoc.Y); 
                    StartDragOperation(screenLoc);
                }
            }
            return false; 
        }
 
        /// <include file='doc\ContainerSelectorBehavior.uex' path='docs/doc[@for="ContainerSelectorBehavior.OnMouseUp"]/*' /> 
        /// <devdoc>
        ///     Simply clear the initial drag point, so we can start again 
        ///     on the next mouse down.
        /// </devdoc>
        public override bool OnMouseUp(Glyph g, MouseButtons button) {
            InitialDragPoint = Point.Empty; 
            OkToMove = false;
            return false; 
        } 

        /// <devdoc> 
        ///     Called when we've identified that we want to start a drag operation
        ///     with our data container.
        /// </devdoc>
        private void StartDragOperation(Point initialMouseLocation) { 

            //need to grab a hold of some services 
            ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService)); 
            IDesignerHost host = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
 
            if (selSvc == null || host == null) {
                Debug.Fail("Can't drag this Container! Either SelectionService is null or DesignerHost is null");
                return;
            } 

            //must identify a required parent to avoid dragging mixes of children 
            Control requiredParent = containerControl.Parent; 

            ArrayList dragControls = new ArrayList(); 
            ICollection selComps = selSvc.GetSelectedComponents();

            //create our list of controls-to-drag
            foreach (IComponent comp in selComps) { 
                Control ctrl = comp as Control;
                if (ctrl != null) { 
                    if (!ctrl.Parent.Equals(requiredParent)) { 
                        continue;//mixed selection of different parents - don't add this
                    } 

                    ControlDesigner des = host.GetDesigner(ctrl) as ControlDesigner;
                    if (des != null && (des.SelectionRules & SelectionRules.Moveable) != 0) {
                        dragControls.Add(ctrl); 
                    }
                } 
            } 

            //if we have controls-to-drag, create our new behavior and start the drag/drop operation 

            if (dragControls.Count > 0) {

                Point controlOrigin; 

                if (setInitialDragPoint) { 
                    // In this case we want the initialmouselocation to be the control's origin. 
                    controlOrigin = behaviorService.ControlToAdornerWindow(containerControl);
                    controlOrigin = behaviorService.AdornerWindowPointToScreen(controlOrigin); 
                }
                else {
                    controlOrigin = initialMouseLocation;
                } 

                DropSourceBehavior dsb = new DropSourceBehavior(dragControls, containerControl.Parent, controlOrigin); 
                try { 
                    behaviorService.DoDragDrop(dsb);
                } 
                finally {
                    OkToMove = false;
                    InitialDragPoint = Point.Empty;
                } 
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
 
    /// <include file='doc\ContainerSelectorBehavior.uex' path='docs/doc[@for="ContainerSelectorBehavior"]/*' />
    /// <devdoc>
    ///     This behavior is associated with the ContainerGlyph offered up
    ///     by ParentControlDesigner.  This Behavior simply 
    ///     starts a new dragdrop behavior.
    /// </devdoc> 
    internal sealed class ContainerSelectorBehavior : Behavior { 

        private Control                 containerControl;           //our related control 
        private IServiceProvider        serviceProvider;            //used for starting a drag/drop
        private BehaviorService         behaviorService;            //ptr to where we start our drag/drop operation
        private bool                    okToMove;                   //state identifying if we are allowed to move the container
        private Point                   initialDragPoint;           //cached "mouse down" point 

        // For VSWhidbey 464206. For some controls, we want to change the original drag point to be the upper-left of the control in 
        // order to make it easier to drop the control at a desired location. But not all controls want this behavior. E.g. we want 
        // to do it for Panel and ToolStrip, but not for Label. Label has a ContainerSelectorBehavior via the NoResizeSelectionBorder
        // glyph. 
        private bool                    setInitialDragPoint;

        /// <include file='doc\ContainerSelectorBehavior.uex' path='docs/doc[@for="ContainerSelectorBehavior.ContainerSelectorBehavior"]/*' />
        /// <devdoc> 
        ///     Constructor, here we cache off all of our member vars and sync
        ///     location & size changes. 
        /// </devdoc> 
        internal ContainerSelectorBehavior(Control containerControl, IServiceProvider serviceProvider) {
            Init(containerControl, serviceProvider); 
            this.setInitialDragPoint = false;
        }

 
        /// <include file='doc\ContainerSelectorBehavior.uex' path='docs/doc[@for="ContainerSelectorBehavior.ContainerSelectorBehavior"]/*' />
        /// <devdoc> 
        ///     Constructor, here we cache off all of our member vars and sync 
        ///     location & size changes.
        /// </devdoc> 
        internal ContainerSelectorBehavior(Control containerControl, IServiceProvider serviceProvider, bool setInitialDragPoint) {
            Init(containerControl, serviceProvider);
            this.setInitialDragPoint = setInitialDragPoint;
        } 

        private void Init(Control containerControl, IServiceProvider serviceProvider) { 
            this.behaviorService = (BehaviorService)serviceProvider.GetService(typeof(BehaviorService)); 
            if (behaviorService == null) {
                Debug.Fail("Could not get the BehaviorService from ContainerSelectroBehavior!"); 
                return;
            }

            this.containerControl = containerControl; 
            this.serviceProvider = serviceProvider;
            this.initialDragPoint = Point.Empty; 
            this.okToMove = false; 
        }
 

        public Control ContainerControl {
            get {
                return containerControl; 
            }
        } 
 
        /// <include file='doc\ContainerSelectorBehavior.uex' path='docs/doc[@for="ContainerSelectorBehavior.OkToMove"]/*' />
        /// <devdoc> 
        ///     This will be true when we detect a mousedown on our glyph.  The Glyph can use this state
        ///     to always return 'true' from hittesting indicating that it would like all messages (like mousemove).
        /// </devdoc>
        public bool OkToMove { 
            get {
                return okToMove; 
            } 

            set { 
                okToMove = value;
            }
        }
 
        public Point InitialDragPoint {
            get { 
                return initialDragPoint; 
            }
 
            set {
                initialDragPoint = value;
            }
        } 

 
        /// <include file='doc\ContainerSelectorBehavior.uex' path='docs/doc[@for="ContainerSelectorBehavior.OnMouseDown"]/*' /> 
        /// <devdoc>
        ///     If the user selects the containerglyph - select our related component. 
        /// </devdoc>
        public override bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc) {
            if (button == MouseButtons.Left) {
                //select our component 
                ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService));
                if (selSvc != null && !containerControl.Equals(selSvc.PrimarySelection as Control)) { 
                    selSvc.SetSelectedComponents(new object[] {containerControl}, SelectionTypes.Primary | SelectionTypes.Toggle); 

                    // VSWhidbey #488545 
                    // Setting the selected component will create a new glyph, so this instance of the glyph won't
                    // receive any more mouse messages. So we need to tell the new glyph what the initialDragPoint and okToMove are.

                    // Sigh.... Here we go. 

                    ContainerSelectorGlyph selOld = g as ContainerSelectorGlyph; 
                    if (selOld == null) { 
                        return false;
                    } 

                    foreach (Adorner a in behaviorService.Adorners) {
                        foreach (Glyph glyph in a.Glyphs) {
                            ContainerSelectorGlyph selNew = glyph as ContainerSelectorGlyph; 
                            if (selNew == null) {
                                continue; 
                            } 

                            // Don't care if we are looking at the same containerselectorglyph 
                            if (selNew.Equals(selOld)) {
                                continue;
                            }
 
                            // Check if the containercontrols are the same
                            ContainerSelectorBehavior behNew = selNew.RelatedBehavior as ContainerSelectorBehavior; 
                            ContainerSelectorBehavior behOld = selOld.RelatedBehavior as ContainerSelectorBehavior; 
                            if (behNew == null || behOld == null) {
                                continue; 
                            }


                            // and the relatedcomponents are the same, then we have found the new glyph that just got added 
                            if (behOld.ContainerControl.Equals(behNew.ContainerControl)) {
                                behNew.OkToMove = true; 
                                behNew.InitialDragPoint = DetermineInitialDragPoint(mouseLoc); 
                                break;
                            } 
                        }
                    }

 
                }
                else { 
                    InitialDragPoint = DetermineInitialDragPoint(mouseLoc); 

                    //set 'okToMove' to true since the user actually clicked down on the glyph 
                    OkToMove = true;
                }
            }
            return false; 
        }
 
        private Point DetermineInitialDragPoint(Point mouseLoc) { 
            if (setInitialDragPoint) {
                // Set the mouse location to be to control's location. 
                Point controlOrigin = behaviorService.ControlToAdornerWindow(containerControl);
                controlOrigin = behaviorService.AdornerWindowPointToScreen(controlOrigin);
                Cursor.Position = controlOrigin;
                return controlOrigin; 
            }
            else { 
                // This really amounts to doing nothing 
                return mouseLoc;
            } 
        }

        /// <include file='doc\ContainerSelectorBehavior.uex' path='docs/doc[@for="ContainerSelectorBehavior.OnMouseMove"]/*' />
        /// <devdoc> 
        ///     We will compare the mouse loc to the initial point (set in onmousedown)
        ///     and if we're far enough, we'll create a dropsourcebehavior object and start 
        ///     out drag operation! 
        /// </devdoc>
        public override bool OnMouseMove(Glyph g, MouseButtons button, Point mouseLoc) { 
            if (button == MouseButtons.Left && OkToMove) {
                if (InitialDragPoint == Point.Empty) {
                    InitialDragPoint = DetermineInitialDragPoint(mouseLoc);
                } 
                Size delta = new Size(Math.Abs(mouseLoc.X - InitialDragPoint.X), Math.Abs(mouseLoc.Y - InitialDragPoint.Y));
                if (delta.Width >= DesignerUtils.MinDragSize.Width/2 || delta.Height >= DesignerUtils.MinDragSize.Height/2) { 
                    //start our drag! 
                    Point screenLoc = behaviorService.AdornerWindowToScreen();
                    screenLoc.Offset(mouseLoc.X, mouseLoc.Y); 
                    StartDragOperation(screenLoc);
                }
            }
            return false; 
        }
 
        /// <include file='doc\ContainerSelectorBehavior.uex' path='docs/doc[@for="ContainerSelectorBehavior.OnMouseUp"]/*' /> 
        /// <devdoc>
        ///     Simply clear the initial drag point, so we can start again 
        ///     on the next mouse down.
        /// </devdoc>
        public override bool OnMouseUp(Glyph g, MouseButtons button) {
            InitialDragPoint = Point.Empty; 
            OkToMove = false;
            return false; 
        } 

        /// <devdoc> 
        ///     Called when we've identified that we want to start a drag operation
        ///     with our data container.
        /// </devdoc>
        private void StartDragOperation(Point initialMouseLocation) { 

            //need to grab a hold of some services 
            ISelectionService selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService)); 
            IDesignerHost host = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
 
            if (selSvc == null || host == null) {
                Debug.Fail("Can't drag this Container! Either SelectionService is null or DesignerHost is null");
                return;
            } 

            //must identify a required parent to avoid dragging mixes of children 
            Control requiredParent = containerControl.Parent; 

            ArrayList dragControls = new ArrayList(); 
            ICollection selComps = selSvc.GetSelectedComponents();

            //create our list of controls-to-drag
            foreach (IComponent comp in selComps) { 
                Control ctrl = comp as Control;
                if (ctrl != null) { 
                    if (!ctrl.Parent.Equals(requiredParent)) { 
                        continue;//mixed selection of different parents - don't add this
                    } 

                    ControlDesigner des = host.GetDesigner(ctrl) as ControlDesigner;
                    if (des != null && (des.SelectionRules & SelectionRules.Moveable) != 0) {
                        dragControls.Add(ctrl); 
                    }
                } 
            } 

            //if we have controls-to-drag, create our new behavior and start the drag/drop operation 

            if (dragControls.Count > 0) {

                Point controlOrigin; 

                if (setInitialDragPoint) { 
                    // In this case we want the initialmouselocation to be the control's origin. 
                    controlOrigin = behaviorService.ControlToAdornerWindow(containerControl);
                    controlOrigin = behaviorService.AdornerWindowPointToScreen(controlOrigin); 
                }
                else {
                    controlOrigin = initialMouseLocation;
                } 

                DropSourceBehavior dsb = new DropSourceBehavior(dragControls, containerControl.Parent, controlOrigin); 
                try { 
                    behaviorService.DoDragDrop(dsb);
                } 
                finally {
                    OkToMove = false;
                    InitialDragPoint = Point.Empty;
                } 
            }
 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
