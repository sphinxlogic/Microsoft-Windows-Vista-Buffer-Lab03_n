 
//------------------------------------------------------------------------------
// <copyright file="FlowLayoutPanelDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.Windows.Forms.Design { 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics; 
    using System;
    using System.Design; 
    using System.Drawing; 
    using System.Drawing.Drawing2D;
    using System.Windows.Forms; 
    using System.Windows.Forms.Design.Behavior;
    using Microsoft.Win32;

    /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner"]/*' /> 
    /// <devdoc>
    ///     This class handles all design time behavior for the FlowLayoutPanel control.  Basically, 
    ///     this designer carefully watches drag operations.  During a drag, we attempt to draw an 
    ///     "I" bar for insertion/feedback purposes.  When a control is added to our designer, we check
    ///     some cached state to see if we believe that it needs to be inserted at a particular index.  If 
    ///     so, we re-insert the control appropriately.
    /// </devdoc>
    internal class FlowLayoutPanelDesigner : FlowPanelDesigner {
        private struct ChildInfo { 
            public Rectangle            marginBounds;//represents the bounds (incl. margins) of a child - used for hittesting
            public Rectangle            controlBounds; //bounds of the control -- used for drawing the IBar 
            public bool                 inSelectionColl;//is this child in the selection collection? 
        }
 
        private ChildInfo[]             childInfo;

        private ArrayList               dragControls;   //the controls that are actually being dragged -- used for an internal drag
        private Control                 primaryDragControl; //the primary drag control 

        // commonSizes is used to store the maximum height/width of each row/col. We need to do so by storing 
        // <top,bottom | left, right> rather than Height/Width. See VSWhidbey 584477, DevDiv Bugs 71258 
        private ArrayList               commonSizes;//format: Rectangle[] (Top | Left, Bottom | Right, height/width of row/col, index of Last ctrl on Row/Col)
 
        private int                     insertIndex;//the index which we will re-insert a newly added child
        private Point                   lastMouseLoc;//use for mouse tracking purposes
        private Point                   oldP1, oldP2;//tracks the last rendered I-bar location
 
        private static readonly int     InvalidIndex = -1;
        private const int               iBarHalfSize = 2; 
        private const int               minIBar = 10; //if space for IBar is <= minIBar we draw a simple IBar 
        private const int               iBarHatHeight = 3;
        private const int               iBarSpace = 2; 
        private const int               iBarLineOffset = (iBarHatHeight + iBarSpace);
        private const int               iBarHatWidth = 5;
        private int                     maxIBarWidth = Math.Max(iBarHalfSize, (iBarHatWidth - 1)/2); //since we don't always know whic IBar we are going draw we want to invalidate max area
 
        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.FlowLayoutPanelDesigner"]/*' />
        /// <devdoc> 
        ///     Simple constructor that cretes our array list and clears our Ibar points. 
        /// </devdoc>
        public FlowLayoutPanelDesigner() { 
            commonSizes = new ArrayList();
            oldP1 = oldP2 = Point.Empty;
            insertIndex = InvalidIndex;
        } 

        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.AllowGenericDragBox"]/*' /> 
        /// <devdoc> 
        ///     This is called to check whether a generic dragbox should be drawn when dragging a toolbox item
        ///     over the designer's surface. 
        /// </devdoc>
        protected override bool AllowGenericDragBox {
            get {
                return false; 
            }
        } 
 
        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.AllowSetChildIndexOnDrop"]/*' />
        /// <devdoc> 
        ///     This is called to check whether the z-order of dragged controls should be maintained when dropped on a
        ///     ParentControlDesigner. By default it will, but e.g. FlowLayoutPanelDesigner wants to do its own z-ordering.
        ///
        ///     If this returns true, then the DropSourceBehavior will attempt to set the index of the controls being 
        ///     dropped to preserve the original order (in the dragSource). If it returns false, the index will not
        ///     be set. 
        /// 
        ///     If this is set to false, then the DropSourceBehavior will not treat a drag as a local drag even
        ///     if the dragSource and the dragTarget are the same. This will allow a ParentControlDesigner to hook 
        ///     OnChildControlAdded to set the right child index, since in this case, the control(s) being dragged
        ///     will be removed from the dragSource and then added to the dragTarget.
        ///
        /// </devdoc> 
        protected internal override bool AllowSetChildIndexOnDrop {
            get { 
                return false; 
            }
        } 



        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.Control"]/*' /> 
        /// <devdoc>
        ///     Simply returns the designer's control as a FlowLayoutPanel 
        /// </devdoc> 
        private new FlowLayoutPanel Control {
            get { 
                return base.Control as FlowLayoutPanel;
            }
        }
 
        // per VSWhidbey #424850 adding this to this class...
        protected override InheritanceAttribute InheritanceAttribute   { 
            get { 
                if ((base.InheritanceAttribute == InheritanceAttribute.Inherited)
                    || (base.InheritanceAttribute ==  InheritanceAttribute.InheritedReadOnly)) { 
                    return InheritanceAttribute.InheritedReadOnly;
                }
                return base.InheritanceAttribute;
            } 
        }
 
        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.FlowDirection"]/*' /> 
        /// <devdoc>
        ///     Shadows the FlowDirection property.  We do this so that we can update the areas 
        ///     covered by glyphs correctly. VSWhidbey# 232910.
        /// </devdoc>
        private FlowDirection FlowDirection {
            get { 
                return Control.FlowDirection;
            } 
            set { 
                if (value != Control.FlowDirection) {
                    //Since we don't know which control is going to go where, 
                    //we just invalidate the area corresponding to the ClientRectangle
                    //in the adornerwindow
                    BehaviorService.Invalidate(BehaviorService.ControlRectInAdornerWindow(Control));
                    Control.FlowDirection = value; 
                }
            } 
        } 

        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.HorizontalFlow"]/*' /> 
        /// <devdoc>
        ///     Returns true if flow direction is right-to-left or left-to-right
        /// </devdoc>
        private bool HorizontalFlow { 
            get {
                return Control.FlowDirection == FlowDirection.RightToLeft || Control.FlowDirection == FlowDirection.LeftToRight; 
            } 
        }
 
        private FlowDirection RTLTranslateFlowDirection(FlowDirection direction) {
            if (Control.RightToLeft == RightToLeft.No) {
                return direction;
            } 

            switch (direction) { 
                case FlowDirection.LeftToRight: 
                    return FlowDirection.RightToLeft;
                case FlowDirection.RightToLeft: 
                    return FlowDirection.LeftToRight;
                case FlowDirection.TopDown:
                case FlowDirection.BottomUp:
                    return direction; 
                default: {
                    Debug.Fail("Unknown FlowDirection"); 
                    return direction; 
                }
            } 
        }

        /// <devdoc>
        ///     Returns a Rectangle representing the margin bounds of the control. 
        /// </devdoc>
        private Rectangle GetMarginBounds(Control control) { 
            //If the FLP is RightToLeft.Yes, then the values of Right and Left margins are swapped, account for that here. 
            return new Rectangle(control.Bounds.Left - (Control.RightToLeft == RightToLeft.No ? control.Margin.Left : control.Margin.Right),
                              control.Bounds.Top - control.Margin.Top, 
                              control.Bounds.Width + control.Margin.Horizontal, // width should include both margins
                              control.Bounds.Height+ control.Margin.Vertical); // so should height
        }
 

        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.CreateMarginBoundsList"]/*' /> 
        /// <devdoc> 
        ///     Called when we receive a DragEnter notification - here we attempt to cache child position and information
        ///     intended to be used by drag move & drop messages.  Basically we pass through the children twice - first 
        ///     we build up an array of rects representing the children bounds (w/margins) and identify where the row/
        ///     column changes are.  Secondly, we normalize the child rects so that children in each row/column are the
        ///     same height/width;
        /// </devdoc> 
        private void CreateMarginBoundsList() {
            commonSizes.Clear(); 
 
            if (Control.Controls.Count == 0) {
                childInfo = new ChildInfo[0]; 
                return;
            }

            //this will cache all needed info for the children 
            childInfo = new ChildInfo[Control.Controls.Count];
            Point offset = Control.PointToScreen(Point.Empty); 
 
            // cache these 2
            FlowDirection flowDirection = RTLTranslateFlowDirection(Control.FlowDirection); 
            bool horizontalFlow = HorizontalFlow;

            int currentMinTopLeft = Int32.MaxValue;
            int currentMaxBottomRight = -1; 
            int lastOffset = -1;
 
            if ((horizontalFlow && flowDirection == FlowDirection.RightToLeft) || 
                (!horizontalFlow && flowDirection == FlowDirection.BottomUp)) {
                lastOffset = Int32.MaxValue; 
            }

            int i = 0;
 
            bool isRTL = (Control.RightToLeft == RightToLeft.Yes);
            //pass 1 - store off the original margin rects & identify row/column sizes 
            for( i = 0; i < Control.Controls.Count; i++) { 

                Control currentControl = Control.Controls[i];//save time 

                Rectangle rect = GetMarginBounds(currentControl);
                Rectangle bounds = currentControl.Bounds;
                //fix up bounds such that the IBar is not drawn right on top of the control 
                if (horizontalFlow) {
                    //difference between bounds and rect is that we do not adjust top, bottom, height 
                    //If the FLP is RightToLeft.Yes, then the values of Right and Left margins are swapped, account for that here. 
                    bounds.X -= (!isRTL ? currentControl.Margin.Left : currentControl.Margin.Right);
                    //to draw correctly in dead areas 
                    bounds.Width += currentControl.Margin.Horizontal;
                    //we want the IBar to stop at the very edge of the control. Offset height
                    //by 1 pixel to ensure that. This is the classic - how many pixels to draw when you
                    //draw from Bounds.Top to Bounds.Bottom. 
                    bounds.Height -= 1;
                } 
                else { 
                    //difference between bounds and rect is that we do not adjust left, right, width
                    bounds.Y -= currentControl.Margin.Top; 
                    //to draw correctly in dead areas
                    bounds.Height += currentControl.Margin.Vertical;
                    //we want the IBar to stop at the very edge of the control. Offset width
                    //by 1 pixel to ensure that. This is the classic - how many pixels to draw when you 
                    //draw from Bounds.Left to Bounds.Right.
                    bounds.Width -= 1; 
                } 

                rect.Offset(offset.X, offset.Y); 
                bounds.Offset(offset.X, offset.Y);

                childInfo[i].marginBounds = rect;
                childInfo[i].controlBounds = bounds; 
                childInfo[i].inSelectionColl = false;
 
                if (dragControls != null && dragControls.Contains(currentControl)) { 
                    childInfo[i].inSelectionColl = true;
                } 

                if (horizontalFlow) {
                    //identify a new row
                    if (flowDirection == FlowDirection.LeftToRight ? rect.X < lastOffset : rect.X > lastOffset) { 
                        Debug.Assert(currentMinTopLeft > 0 && currentMaxBottomRight > 0, "How can we not have a min/max value?");
                        if (currentMinTopLeft > 0 && currentMaxBottomRight > 0) { 
                            //store off the 
                            // min Top|Left for Row/Col
                            // max Bottom|Right for Row/Col 
                            // Height/Width of Row/Col
                            // Control index
                            commonSizes.Add(new Rectangle(currentMinTopLeft, currentMaxBottomRight, currentMaxBottomRight - currentMinTopLeft, i));
                            currentMinTopLeft = Int32.MaxValue; 
                            currentMaxBottomRight = -1;
                        } 
                    } 

                    lastOffset = rect.X; 

                    //be sure to track the largest row size
                    if (rect.Top < currentMinTopLeft) {
                        currentMinTopLeft = rect.Top; 
                    }
 
                    if (rect.Bottom > currentMaxBottomRight) { 
                        currentMaxBottomRight = rect.Bottom;
                    } 

                }
                else {
                    //identify a new column 
                    if (flowDirection == FlowDirection.TopDown ? rect.Y < lastOffset : rect.Y > lastOffset) {
                        Debug.Assert(currentMinTopLeft > 0 && currentMaxBottomRight > 0, "How can we not have a min/max value?"); 
                        if (currentMinTopLeft > 0 && currentMaxBottomRight > 0) { 
                            //store off the
                            // min Top|Left for Row/Col 
                            // max Bottom|Right for Row/Col
                            // Height/Width of Row/Col
                            // Control index
                            commonSizes.Add(new Rectangle(currentMinTopLeft, currentMaxBottomRight, currentMaxBottomRight - currentMinTopLeft, i)); 
                            currentMinTopLeft = Int32.MaxValue;
                            currentMaxBottomRight = -1; 
                        } 
                    }
 
                    lastOffset = rect.Y;

                    //be sure to track the column size
                    if (rect.Left < currentMinTopLeft) { 
                        currentMinTopLeft = rect.Left;
                    } 
 
                    if (rect.Right > currentMaxBottomRight) {
                        currentMaxBottomRight = rect.Right; 
                    }
                }

            } 

            //add the last row/column to our commonsizes 
            if (currentMinTopLeft > 0 && currentMaxBottomRight > 0) { 
                //store off the max size for this row
                commonSizes.Add(new Rectangle(currentMinTopLeft, currentMaxBottomRight, currentMaxBottomRight - currentMinTopLeft, i)); 
            }

            //pass2 - adjust all controls to max width/height according to their row/col
            int controlIndex = 0; 
            for (i = 0; i < commonSizes.Count; i++) {
                while (controlIndex < ((Rectangle)commonSizes[i]).Height) {// Remember that Height is control index 
                    if (horizontalFlow) { 
                        childInfo[controlIndex].marginBounds.Y = ((Rectangle)commonSizes[i]).X; // X is Top
                        childInfo[controlIndex].marginBounds.Height = ((Rectangle)commonSizes[i]).Width; 
                    }
                    else {
                        childInfo[controlIndex].marginBounds.X = ((Rectangle)commonSizes[i]).X; // X is Left
                        childInfo[controlIndex].marginBounds.Width = ((Rectangle)commonSizes[i]).Width; 
                    }
 
                    controlIndex++; 
                }
            } 

        }
        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.Initialize"]/*' />
        /// <devdoc> 
        ///      Overrides the base .
        /// </devdoc> 
        public override void Initialize(IComponent component) { 
            base.Initialize(component);
 
            // VSWhidbey #424845. If the FLP is inheritedreadonly, so should all of the children
            if (InheritanceAttribute == InheritanceAttribute.InheritedReadOnly) {
                for (int i = 0; i < Control.Controls.Count; i++) {
                    TypeDescriptor.AddAttributes(Control.Controls[i], InheritanceAttribute.InheritedReadOnly); 
                }
            } 
 

 
        }


        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.OnChildControlAdded"]/*' /> 
        /// <devdoc>
        ///     When a child is added -we check to see if we cached an index 
        ///     representing where this control should be inserted.  If so, we 
        ///     re-insert the new child.
        /// 
        ///     This is only done on an external drag-drop
        /// </devdoc>
        private void OnChildControlAdded(object sender, ControlEventArgs e) {
 
            if (insertIndex != InvalidIndex) { // this will only be true on a drag-drop
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost; 
                if (host != null) { 
                    IComponentChangeService cs = GetService(typeof(IComponentChangeService)) as IComponentChangeService;
                    PropertyDescriptor controlsProp = TypeDescriptor.GetProperties(Control)["Controls"]; 
                    if (cs != null && controlsProp != null) {
                        cs.OnComponentChanging(Control, controlsProp);
                        //on an external drag/drop, the control will have been inserted at the end, so we can safely
                        //set the index and increment it, since we are moving the control backwards. Check out 
                        //SetChildIndex and MoveElement.
                        Control.Controls.SetChildIndex(e.Control, insertIndex); 
                        ++insertIndex; 
                        cs.OnComponentChanged(Control, controlsProp, null, null);
                    } 
                }
            }
        }
 

        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.OnDragDrop"]/*' /> 
        /// <devdoc> 
        ///     On a drop, if we have cached a special index where we think a control
        ///     should be inserted - we check to see if this was a pure-local drag 
        ///     (i.e. we dragged a child control inside ourself).  If so, we re-insert the
        ///     child to the appropriate index.  Otherwise, we'll do this in the ChildAdded
        ///     event.
        /// </devdoc> 

        protected override void OnDragDrop(DragEventArgs de) { 
 
            bool localDrag = false;
            if (dragControls != null && primaryDragControl != null && Control.Controls.Contains(primaryDragControl)) { 
                localDrag = true;
            }

            if (!localDrag) { 
                //if we are not a local drag then just let the base handle it.
 
                if (Control != null) { 
                    Control.ControlAdded += new ControlEventHandler(this.OnChildControlAdded);
                } 
                try {
                        base.OnDragDrop(de);
                }
                finally { 
                    if (Control != null) {
                        Control.ControlAdded -= new ControlEventHandler(this.OnChildControlAdded); 
                    } 
                }
            } 
            else {

                // local drag. We do it ourselves, so that we can set the indices right
 
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;
                if (host != null) { 
 
                    DesignerTransaction trans = null;
                    string transDesc; 
                    bool performCopy = (de.Effect == DragDropEffects.Copy);
                    // We use this list when doing a Drag-Copy, so that we can correctly restore state when we are done.
                    ArrayList originalControls = null;
                    ISelectionService selSvc = null; 

 
                    if (dragControls.Count == 1) { 
                        string name = TypeDescriptor.GetComponentName(dragControls[0]);
                        if (name == null || name.Length == 0) { 
                            name = dragControls[0].GetType().Name;
                        }
                        transDesc = SR.GetString(performCopy ? SR.BehaviorServiceCopyControl : SR.BehaviorServiceMoveControl, name);
                    } 
                    else {
                        transDesc = SR.GetString(performCopy ? SR.BehaviorServiceCopyControls : SR.BehaviorServiceMoveControls, dragControls.Count); 
                    } 

                    trans = host.CreateTransaction(transDesc) ; 

                    try {

                        //In order to be able to set the index correctly, we need to create a backwards move. 
                        //We do this by first finding the control foo that corresponds to insertIndex.
                        //We then remove all the drag controls from the FLP. 
                        //Then we get the new childIndex for the control foo. 
                        //Finally we loop:
                        //      add the ith drag control 
                        //      set its child index to (index of control foo) - 1
                        //On each iteration, the child index of control foo will change.
                        //
                        //This ensures that we can move both contiguous and non-contiguous selections. 

                        //Special case when the element we are inserting before is a part of the dragControls 
                        while ((insertIndex < childInfo.Length-1) && (childInfo[insertIndex].inSelectionColl)){ 
                            // Find the next control that is not a part of the selection
                            ++insertIndex; 
                        }

                        IComponentChangeService cs = GetService(typeof(IComponentChangeService)) as IComponentChangeService;
                        PropertyDescriptor controlsProp = TypeDescriptor.GetProperties(Control)["Controls"]; 
                        Control control = null;
 
                        if (insertIndex != childInfo.Length) { 
                            control = Control.Controls[insertIndex];
                        } 
                        else {
                            //we are inserting past the last control
                            insertIndex = -1;
                        } 

 
                        if (cs != null && controlsProp != null) { 
                            cs.OnComponentChanging(Control, controlsProp);
                        } 

                        //remove the controls in the dragcollection - don't need to do this if we are copying
                        if (!performCopy) {
                            for (int i = 0; i < dragControls.Count; i++) { 
                                Control.Controls.Remove(dragControls[i] as Control);
                            } 
 
                            // get the new index -- if we are performing a copy, then the index is the same
                            if (control != null) { 
                                insertIndex = Control.Controls.GetChildIndex(control, false);
                            }
                        }
                        else { 
                            // We are doing a copy, so let's copy the controls
#if DEBUG 
                            if (control != null) { 
                                Debug.Assert(insertIndex == Control.Controls.GetChildIndex(control, false), "Why are the indices not the same?");
                            } 
#endif

                            // Get the objects to copy
                            ArrayList temp = new ArrayList(); 
                            for (int i = 0; i < dragControls.Count; i++) {
                                temp.Add(dragControls[i]); 
                            } 

                            // Create a copy of them 
                            temp = DesignerUtils.CopyDragObjects(temp, Component.Site) as ArrayList;
                            if (temp == null) {
                                Debug.Fail("Couldn't create copies of the controls we are dragging.");
                                return; 
                            }
 
                            originalControls = new ArrayList(); 

                            // And stick the copied controls back into the dragControls array 
                            for (int j = 0; j < temp.Count; j++) {
                                // ... but save off the old controls first
                                originalControls.Add(dragControls[j]);
                                // remember to set the new primary control 
                                if (primaryDragControl.Equals(dragControls[j] as Control)) {
                                    primaryDragControl = temp[j] as Control; 
                                } 
                                dragControls[j] = temp[j];
                            } 

                            selSvc = (ISelectionService)GetService(typeof(ISelectionService));

                        } 

                        if (insertIndex == -1) { 
                            //Either insertIndex was childInfo.Length (inserting past the end) or 
                            //insertIndex was childInfo.Length - 1 and the control at that index was also
                            //a part of the dragCollection. In either case, the new index is equal to the count 
                            //of existing controls in the controlCollection. Helps to draw this out...
                            insertIndex = Control.Controls.Count;
                        }
 
                        //do the primary control first
                        Control.Controls.Add(primaryDragControl); 
                        Control.Controls.SetChildIndex(primaryDragControl, insertIndex); 
                        ++insertIndex;
                        if (selSvc != null) { 
                            Debug.Assert(performCopy, "selSvc should only be non-null when we are doing a local copy");
                            selSvc.SetSelectedComponents(new object[] {primaryDragControl}, SelectionTypes.Primary | SelectionTypes.Replace);
                        }
 
                        //now do the rest
                        // 
                        //Note dragControls are in opposite zorder than what FLP uses, 
                        //so add from the end.
                        for (int i = dragControls.Count - 1; i >= 0; i--) { 
                            if (primaryDragControl.Equals(dragControls[i] as Control)) {
                                continue;
                            }
                            Control.Controls.Add(dragControls[i] as Control); 
                            Control.Controls.SetChildIndex(dragControls[i] as Control, insertIndex);
                            ++insertIndex; 
                            if (selSvc != null) { 
                                selSvc.SetSelectedComponents(new object[] {dragControls[i]}, SelectionTypes.Add);
                            } 

                        }

                        if (cs != null && controlsProp != null) { 
                            cs.OnComponentChanged(Control, controlsProp, null, null);
                        } 
 
                        // If we did a Copy, then restore the old controls to make sure we set state correctly
                        if (originalControls != null) { 
                            for (int i = 0; i < originalControls.Count; i++) {
                                dragControls[i] = originalControls[i];
                            }
                        } 

                        base.OnDragComplete(de); 
 
                        if (trans != null) {
                            trans.Commit(); 
                            trans = null;
                        }
                    }
 
                    finally {
                        if (trans != null) { 
                            trans.Cancel(); 
                        }
                    } 

                }

            } 

            insertIndex = InvalidIndex; 
        } 

        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.OnDragLeave"]/*' /> 
        /// <devdoc>
        ///     Called when a drag-drop operation leaves the control designer view
        ///
        /// </devdoc> 
        protected override void OnDragLeave(EventArgs e) {
            EraseIBar(); 
            insertIndex = InvalidIndex; 
            primaryDragControl = null;
            if (dragControls != null) { 
                dragControls.Clear();
            }
            base.OnDragLeave(e);
        } 

 
        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.OnDragEnter"]/*' /> 
        /// <devdoc>
        ///     When we receive a drag enter notification - we clear our recommended insertion 
        ///     index and mose loc - then call our method to cache all the bounds of the children.
        /// </devdoc>
        protected override void OnDragEnter(DragEventArgs de) {
            base.OnDragEnter(de); 

            insertIndex = InvalidIndex; 
            lastMouseLoc = Point.Empty; 
            primaryDragControl = null;
 
            //Get the sorted drag controls. We use these for an internal drag.
            DropSourceBehavior.BehaviorDataObject data = de.Data as DropSourceBehavior.BehaviorDataObject;
            if (data != null) {
                int primaryIndex = -1; 
                dragControls = data.GetSortedDragControls(ref primaryIndex);
                primaryDragControl = dragControls[primaryIndex] as Control; 
            } 

 
            //cache all child bounds and identify rows/cols
            CreateMarginBoundsList();

        } 

        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.OnDragOver"]/*' /> 
        /// <devdoc> 
        ///     During a drag over, if we have successfully cached marign/row/col information
        ///     we will attempt to render an "I-bar" for the user based on where we think the 
        ///     user is attempting to insert the control at.  Note that we also cache off this
        ///     guessed-index so that if a control is dropped/added we can re-insert it at this
        ///     spot.
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        protected override void OnDragOver(DragEventArgs de) { 
            base.OnDragOver(de); 

            Point mouseLoc = System.Windows.Forms.Control.MousePosition; 

            if (mouseLoc.Equals(lastMouseLoc) || childInfo == null || childInfo.Length == 0 || commonSizes.Count == 0) {
                //no layout data to work with
                return; 
            }
 
            Rectangle bounds = Rectangle.Empty; 
            lastMouseLoc = mouseLoc;
            Point controlOffset = Control.PointToScreen(new Point(0,0)); 
            if (Control.RightToLeft == RightToLeft.Yes) {
                controlOffset.X += Control.Width;
            }
 
            insertIndex = InvalidIndex;
 
            //brute force hit testing to first determine if we're over one 
            //of our margin bounds
            // 
            int i = 0;
            for(i = 0; i < childInfo.Length; i++) {
                if (childInfo[i].marginBounds.Contains(mouseLoc)) {
                    bounds = childInfo[i].controlBounds; 
                    break;
                } 
            } 

            //If we found the bounds - then we need to draw our "I-Beam" 
            //If the mouse is over one of the marginbounds, then the dragged control
            //will always be inserted before the control the margin area represents. Thus
            //we will always draw the I-Beam to the left or above (FlowDirection.LRT | TB) or
            //to the right or below (FlowDirection.RTL | BT). 
            if (!bounds.IsEmpty) {
                insertIndex = i;//the insertion index will always be the boxed area (called margin area) we are over 
 
                if (childInfo[i].inSelectionColl) {
                    //If the marginBounds is part of the selection, then don't draw the IBar. But actually 
                    //setting insertIndex, will allows us to correctly drop the control in the right place.
                    EraseIBar();
                }
                else { 
                    FlowDirection direction = RTLTranslateFlowDirection(Control.FlowDirection);
                    if (direction == FlowDirection.LeftToRight) { 
                        ReDrawIBar(new Point(bounds.Left, bounds.Top), new Point( bounds.Left, bounds.Bottom)); 
                    }
                    else if (direction == FlowDirection.RightToLeft) { 
                        ReDrawIBar(new Point(bounds.Right, bounds.Top), new Point( bounds.Right, bounds.Bottom));
                    }
                    else if (direction == FlowDirection.TopDown) {
                        ReDrawIBar(new Point(bounds.Left, bounds.Top), new Point(bounds.Right, bounds.Top)); 
                    }
                    else if (direction == FlowDirection.BottomUp) { 
                        ReDrawIBar(new Point(bounds.Left, bounds.Bottom), new Point(bounds.Right, bounds.Bottom)); 
                    }
                    else { 
                        Debug.Fail("Unknown FlowDirection");
                    }

                } 
            }
            else { 
                //here, we're in a dead area - see what row / column we're in for a 
                //best-guess at the insertion index
                int offset = HorizontalFlow ? controlOffset.Y : controlOffset.X; 
                bool isRTL = (Control.RightToLeft == RightToLeft.Yes);

                for ( i =0; i < commonSizes.Count; i++) {
                    if (isRTL) { 
                        offset -= ((Rectangle)commonSizes[i]).Width; // Width is height/width of row/col
                    } 
                    else { 
                        offset += ((Rectangle)commonSizes[i]).Width;
                    } 

                    // Just introducing match for readability.
                    bool match = false;
                    if (!isRTL) { 
                        match = (HorizontalFlow ? (mouseLoc.Y) : (mouseLoc.X)) <= offset;
                    } 
                    else { 
                        match = (HorizontalFlow && mouseLoc.Y <= offset) || (!HorizontalFlow && mouseLoc.X >= offset);
                    } 

                    if (match) {
                        insertIndex = ((Rectangle)commonSizes[i]).Height; // Height is index of last control
                        bounds = childInfo[insertIndex - 1].controlBounds; 
                        if (childInfo[insertIndex - 1].inSelectionColl) {
                            EraseIBar(); 
                        } 
                        else {
                            FlowDirection direction = RTLTranslateFlowDirection(Control.FlowDirection); 
                            if (direction == FlowDirection.LeftToRight) {
                                ReDrawIBar(new Point(bounds.Right, bounds.Top), new Point(bounds.Right, bounds.Bottom));
                            }
                            else if (direction == FlowDirection.RightToLeft) { 
                                ReDrawIBar(new Point(bounds.Left, bounds.Top), new Point(bounds.Left, bounds.Bottom));
                            } 
                            else if (direction == FlowDirection.TopDown) { 
                                ReDrawIBar(new Point(bounds.Left, bounds.Bottom), new Point(bounds.Right, bounds.Bottom));
                            } 
                            else if (direction == FlowDirection.BottomUp) {
                                ReDrawIBar(new Point(bounds.Left, bounds.Top), new Point(bounds.Right, bounds.Top));
                            }
                            else { 
                                Debug.Fail("Unknown FlowDirection");
                            } 
                        } 
                        break;
                    } 


                }
            } 

            if (insertIndex == InvalidIndex) { 
                //here, we're at the 'end' of the flowlayoutpanel - not over 
                //any controls and not in a row/column.
                insertIndex = Control.Controls.Count; 
                EraseIBar();
            }

        } 

 
        private void EraseIBar() { 
            ReDrawIBar(Point.Empty, Point.Empty);
        } 

        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.ReDrawIBar"]/*' />
        /// <devdoc>
        ///     Given two points, we'll draw an 'Ibar".  Note that we only erase at our 
        ///     old points if they are different from the new ones.  Also note that if
        ///     the points are empty - we will simply erase and not draw. 
        /// </devdoc> 
        private void ReDrawIBar(Point p1, Point p2) {
 
            //offset the points to adorner coords
            Point offset = BehaviorService.AdornerWindowToScreen();

            Pen pen = SystemPens.ControlText; 
            if (Control.BackColor != Color.Empty && Control.BackColor.GetBrightness() < .5) {
                pen = SystemPens.ControlLight; 
            } 

 
            //don't offset if p1 is empty. Empty really just means that we want to erase the IBar.
            if (p1 != Point.Empty) {
                p1.Offset(-offset.X, -offset.Y);
                p2.Offset(-offset.X, -offset.Y); 
            }
 
            //only erase the ibar if the points are different from last time 
            //Only invalidate if there's something to invalidate
            if (p1 != oldP1 && p2 != oldP2 && oldP1 != Point.Empty) { 
                Rectangle invalidRect = new Rectangle(oldP1.X, oldP1.Y, oldP2.X - oldP1.X + 1, oldP2.Y - oldP1.Y + 1);
                invalidRect.Inflate(maxIBarWidth, maxIBarWidth);//akways invalidate max area
                BehaviorService.Invalidate(invalidRect);
            } 

            //cache this for next time around -- but do so before changing p1 and p2 below. 
            oldP1 = p1; 
            oldP2 = p2;
 
            //if we have valid new points - redraw our ibar
            //we always want to redraw the line. This is because part of it could have been erased when
            //the dragimage (see DropSourceBehavior) is being moved over the IBar.
 
            if (p1 != Point.Empty) {
                using (Graphics g = BehaviorService.AdornerWindowGraphics) { 
                    if (HorizontalFlow) { 

                        if (Math.Abs(p1.Y - p2.Y) <= minIBar) { 
                            //draw the smaller, simpler IBar
                            g.DrawLine(pen, p1, p2);//vertical line
                            g.DrawLine(pen, p1.X -iBarHalfSize, p1.Y, p1.X + iBarHalfSize, p1.Y);//top hat
                            g.DrawLine(pen, p2.X -iBarHalfSize, p2.Y, p2.X + iBarHalfSize, p2.Y);//bottom hat 
                        }
                        else { 
 
                            //top and bottom hat
                            for (int i = 0; i < iBarHatHeight - 1; i++) { // stop 1 pixel before, since we can't draw a 1 pixel line 
                                // reducing the width of the hat with 2 pixel on each iteration
                                g.DrawLine(pen, p1.X - (iBarHatWidth - 1 - (i*2))/2, p1.Y+i, p1.X + (iBarHatWidth - 1 - (i*2))/2, p1.Y+i);//top hat

                                g.DrawLine(pen, p2.X - (iBarHatWidth - 1 - (i*2))/2, p2.Y-i, p2.X + (iBarHatWidth - 1 - (i*2))/2, p2.Y-i);//bottom hat 
                            }
 
                            //can't draw a 1 pixel line, so draw a vertical line 
                            g.DrawLine(pen, p1.X, p1.Y, p1.X, p1.Y + iBarHatHeight - 1); // top hat
                            g.DrawLine(pen, p2.X, p2.Y, p2.X, p2.Y - iBarHatHeight + 1); // bottom hat 

                            // vertical line

                            g.DrawLine(pen, p1.X, p1.Y + iBarLineOffset, p2.X, p2.Y - iBarLineOffset);//vertical line 
                        }
 
                    } 
                    else {
                        if (Math.Abs(p1.X - p2.X) <= minIBar) { 
                            //draw the smaller, simpler IBar
                            g.DrawLine(pen, p1, p2);//horizontal line
                            g.DrawLine(pen, p1.X, p1.Y -iBarHalfSize, p1.X, p1.Y+ iBarHalfSize);//top hat
                            g.DrawLine(pen, p2.X, p2.Y -iBarHalfSize, p2.X, p2.Y + iBarHalfSize);//bottom hat 
                        }
                        else { 
                            //left and right hat 
                            for (int i = 0; i < iBarHatHeight - 1; i++) { // stop 1 pixel before, since we can't draw a 1 pixel line
                                // reducing the width of the hat with 2 pixel on each iteration 
                                g.DrawLine(pen, p1.X+i, p1.Y - (iBarHatWidth - 1 - (i*2))/2, p1.X+i, p1.Y + (iBarHatWidth - 1 - (i*2))/2);//left hat

                                g.DrawLine(pen, p2.X-i, p2.Y - (iBarHatWidth - 1 - (i*2))/2, p2.X-i, p2.Y + (iBarHatWidth - 1 - (i*2))/2);//right hat
                            } 

                            //can't draw a 1 pixel line, so draw a horizontal line 
                            g.DrawLine(pen, p1.X, p1.Y, p1.X + iBarHatHeight - 1, p1.Y); // left hat 
                            g.DrawLine(pen, p2.X, p2.Y, p2.X - iBarHatHeight + 1, p2.Y); // right hat
 
                            // horizontal line

                            g.DrawLine(pen, p1.X + iBarLineOffset, p1.Y, p2.X - iBarLineOffset, p2.Y);//horizontal line
                        } 

                    } 
                } 
            }
 

        }

        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.PreFilterProperties"]/*' /> 
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
            base.PreFilterProperties(properties); 
 
            PropertyDescriptor prop;
 
            // Handle shadowed properties
            //
            string[] shadowProps = new string[] {
                "FlowDirection" 
            };
 
            Attribute[] empty = new Attribute[0]; 

            for (int i = 0; i < shadowProps.Length; i++) { 
                prop = (PropertyDescriptor)properties[shadowProps[i]];
                if (prop != null) {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(FlowLayoutPanelDesigner), prop, empty);
                } 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright file="FlowLayoutPanelDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.Windows.Forms.Design { 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics; 
    using System;
    using System.Design; 
    using System.Drawing; 
    using System.Drawing.Drawing2D;
    using System.Windows.Forms; 
    using System.Windows.Forms.Design.Behavior;
    using Microsoft.Win32;

    /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner"]/*' /> 
    /// <devdoc>
    ///     This class handles all design time behavior for the FlowLayoutPanel control.  Basically, 
    ///     this designer carefully watches drag operations.  During a drag, we attempt to draw an 
    ///     "I" bar for insertion/feedback purposes.  When a control is added to our designer, we check
    ///     some cached state to see if we believe that it needs to be inserted at a particular index.  If 
    ///     so, we re-insert the control appropriately.
    /// </devdoc>
    internal class FlowLayoutPanelDesigner : FlowPanelDesigner {
        private struct ChildInfo { 
            public Rectangle            marginBounds;//represents the bounds (incl. margins) of a child - used for hittesting
            public Rectangle            controlBounds; //bounds of the control -- used for drawing the IBar 
            public bool                 inSelectionColl;//is this child in the selection collection? 
        }
 
        private ChildInfo[]             childInfo;

        private ArrayList               dragControls;   //the controls that are actually being dragged -- used for an internal drag
        private Control                 primaryDragControl; //the primary drag control 

        // commonSizes is used to store the maximum height/width of each row/col. We need to do so by storing 
        // <top,bottom | left, right> rather than Height/Width. See VSWhidbey 584477, DevDiv Bugs 71258 
        private ArrayList               commonSizes;//format: Rectangle[] (Top | Left, Bottom | Right, height/width of row/col, index of Last ctrl on Row/Col)
 
        private int                     insertIndex;//the index which we will re-insert a newly added child
        private Point                   lastMouseLoc;//use for mouse tracking purposes
        private Point                   oldP1, oldP2;//tracks the last rendered I-bar location
 
        private static readonly int     InvalidIndex = -1;
        private const int               iBarHalfSize = 2; 
        private const int               minIBar = 10; //if space for IBar is <= minIBar we draw a simple IBar 
        private const int               iBarHatHeight = 3;
        private const int               iBarSpace = 2; 
        private const int               iBarLineOffset = (iBarHatHeight + iBarSpace);
        private const int               iBarHatWidth = 5;
        private int                     maxIBarWidth = Math.Max(iBarHalfSize, (iBarHatWidth - 1)/2); //since we don't always know whic IBar we are going draw we want to invalidate max area
 
        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.FlowLayoutPanelDesigner"]/*' />
        /// <devdoc> 
        ///     Simple constructor that cretes our array list and clears our Ibar points. 
        /// </devdoc>
        public FlowLayoutPanelDesigner() { 
            commonSizes = new ArrayList();
            oldP1 = oldP2 = Point.Empty;
            insertIndex = InvalidIndex;
        } 

        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.AllowGenericDragBox"]/*' /> 
        /// <devdoc> 
        ///     This is called to check whether a generic dragbox should be drawn when dragging a toolbox item
        ///     over the designer's surface. 
        /// </devdoc>
        protected override bool AllowGenericDragBox {
            get {
                return false; 
            }
        } 
 
        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.AllowSetChildIndexOnDrop"]/*' />
        /// <devdoc> 
        ///     This is called to check whether the z-order of dragged controls should be maintained when dropped on a
        ///     ParentControlDesigner. By default it will, but e.g. FlowLayoutPanelDesigner wants to do its own z-ordering.
        ///
        ///     If this returns true, then the DropSourceBehavior will attempt to set the index of the controls being 
        ///     dropped to preserve the original order (in the dragSource). If it returns false, the index will not
        ///     be set. 
        /// 
        ///     If this is set to false, then the DropSourceBehavior will not treat a drag as a local drag even
        ///     if the dragSource and the dragTarget are the same. This will allow a ParentControlDesigner to hook 
        ///     OnChildControlAdded to set the right child index, since in this case, the control(s) being dragged
        ///     will be removed from the dragSource and then added to the dragTarget.
        ///
        /// </devdoc> 
        protected internal override bool AllowSetChildIndexOnDrop {
            get { 
                return false; 
            }
        } 



        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.Control"]/*' /> 
        /// <devdoc>
        ///     Simply returns the designer's control as a FlowLayoutPanel 
        /// </devdoc> 
        private new FlowLayoutPanel Control {
            get { 
                return base.Control as FlowLayoutPanel;
            }
        }
 
        // per VSWhidbey #424850 adding this to this class...
        protected override InheritanceAttribute InheritanceAttribute   { 
            get { 
                if ((base.InheritanceAttribute == InheritanceAttribute.Inherited)
                    || (base.InheritanceAttribute ==  InheritanceAttribute.InheritedReadOnly)) { 
                    return InheritanceAttribute.InheritedReadOnly;
                }
                return base.InheritanceAttribute;
            } 
        }
 
        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.FlowDirection"]/*' /> 
        /// <devdoc>
        ///     Shadows the FlowDirection property.  We do this so that we can update the areas 
        ///     covered by glyphs correctly. VSWhidbey# 232910.
        /// </devdoc>
        private FlowDirection FlowDirection {
            get { 
                return Control.FlowDirection;
            } 
            set { 
                if (value != Control.FlowDirection) {
                    //Since we don't know which control is going to go where, 
                    //we just invalidate the area corresponding to the ClientRectangle
                    //in the adornerwindow
                    BehaviorService.Invalidate(BehaviorService.ControlRectInAdornerWindow(Control));
                    Control.FlowDirection = value; 
                }
            } 
        } 

        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.HorizontalFlow"]/*' /> 
        /// <devdoc>
        ///     Returns true if flow direction is right-to-left or left-to-right
        /// </devdoc>
        private bool HorizontalFlow { 
            get {
                return Control.FlowDirection == FlowDirection.RightToLeft || Control.FlowDirection == FlowDirection.LeftToRight; 
            } 
        }
 
        private FlowDirection RTLTranslateFlowDirection(FlowDirection direction) {
            if (Control.RightToLeft == RightToLeft.No) {
                return direction;
            } 

            switch (direction) { 
                case FlowDirection.LeftToRight: 
                    return FlowDirection.RightToLeft;
                case FlowDirection.RightToLeft: 
                    return FlowDirection.LeftToRight;
                case FlowDirection.TopDown:
                case FlowDirection.BottomUp:
                    return direction; 
                default: {
                    Debug.Fail("Unknown FlowDirection"); 
                    return direction; 
                }
            } 
        }

        /// <devdoc>
        ///     Returns a Rectangle representing the margin bounds of the control. 
        /// </devdoc>
        private Rectangle GetMarginBounds(Control control) { 
            //If the FLP is RightToLeft.Yes, then the values of Right and Left margins are swapped, account for that here. 
            return new Rectangle(control.Bounds.Left - (Control.RightToLeft == RightToLeft.No ? control.Margin.Left : control.Margin.Right),
                              control.Bounds.Top - control.Margin.Top, 
                              control.Bounds.Width + control.Margin.Horizontal, // width should include both margins
                              control.Bounds.Height+ control.Margin.Vertical); // so should height
        }
 

        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.CreateMarginBoundsList"]/*' /> 
        /// <devdoc> 
        ///     Called when we receive a DragEnter notification - here we attempt to cache child position and information
        ///     intended to be used by drag move & drop messages.  Basically we pass through the children twice - first 
        ///     we build up an array of rects representing the children bounds (w/margins) and identify where the row/
        ///     column changes are.  Secondly, we normalize the child rects so that children in each row/column are the
        ///     same height/width;
        /// </devdoc> 
        private void CreateMarginBoundsList() {
            commonSizes.Clear(); 
 
            if (Control.Controls.Count == 0) {
                childInfo = new ChildInfo[0]; 
                return;
            }

            //this will cache all needed info for the children 
            childInfo = new ChildInfo[Control.Controls.Count];
            Point offset = Control.PointToScreen(Point.Empty); 
 
            // cache these 2
            FlowDirection flowDirection = RTLTranslateFlowDirection(Control.FlowDirection); 
            bool horizontalFlow = HorizontalFlow;

            int currentMinTopLeft = Int32.MaxValue;
            int currentMaxBottomRight = -1; 
            int lastOffset = -1;
 
            if ((horizontalFlow && flowDirection == FlowDirection.RightToLeft) || 
                (!horizontalFlow && flowDirection == FlowDirection.BottomUp)) {
                lastOffset = Int32.MaxValue; 
            }

            int i = 0;
 
            bool isRTL = (Control.RightToLeft == RightToLeft.Yes);
            //pass 1 - store off the original margin rects & identify row/column sizes 
            for( i = 0; i < Control.Controls.Count; i++) { 

                Control currentControl = Control.Controls[i];//save time 

                Rectangle rect = GetMarginBounds(currentControl);
                Rectangle bounds = currentControl.Bounds;
                //fix up bounds such that the IBar is not drawn right on top of the control 
                if (horizontalFlow) {
                    //difference between bounds and rect is that we do not adjust top, bottom, height 
                    //If the FLP is RightToLeft.Yes, then the values of Right and Left margins are swapped, account for that here. 
                    bounds.X -= (!isRTL ? currentControl.Margin.Left : currentControl.Margin.Right);
                    //to draw correctly in dead areas 
                    bounds.Width += currentControl.Margin.Horizontal;
                    //we want the IBar to stop at the very edge of the control. Offset height
                    //by 1 pixel to ensure that. This is the classic - how many pixels to draw when you
                    //draw from Bounds.Top to Bounds.Bottom. 
                    bounds.Height -= 1;
                } 
                else { 
                    //difference between bounds and rect is that we do not adjust left, right, width
                    bounds.Y -= currentControl.Margin.Top; 
                    //to draw correctly in dead areas
                    bounds.Height += currentControl.Margin.Vertical;
                    //we want the IBar to stop at the very edge of the control. Offset width
                    //by 1 pixel to ensure that. This is the classic - how many pixels to draw when you 
                    //draw from Bounds.Left to Bounds.Right.
                    bounds.Width -= 1; 
                } 

                rect.Offset(offset.X, offset.Y); 
                bounds.Offset(offset.X, offset.Y);

                childInfo[i].marginBounds = rect;
                childInfo[i].controlBounds = bounds; 
                childInfo[i].inSelectionColl = false;
 
                if (dragControls != null && dragControls.Contains(currentControl)) { 
                    childInfo[i].inSelectionColl = true;
                } 

                if (horizontalFlow) {
                    //identify a new row
                    if (flowDirection == FlowDirection.LeftToRight ? rect.X < lastOffset : rect.X > lastOffset) { 
                        Debug.Assert(currentMinTopLeft > 0 && currentMaxBottomRight > 0, "How can we not have a min/max value?");
                        if (currentMinTopLeft > 0 && currentMaxBottomRight > 0) { 
                            //store off the 
                            // min Top|Left for Row/Col
                            // max Bottom|Right for Row/Col 
                            // Height/Width of Row/Col
                            // Control index
                            commonSizes.Add(new Rectangle(currentMinTopLeft, currentMaxBottomRight, currentMaxBottomRight - currentMinTopLeft, i));
                            currentMinTopLeft = Int32.MaxValue; 
                            currentMaxBottomRight = -1;
                        } 
                    } 

                    lastOffset = rect.X; 

                    //be sure to track the largest row size
                    if (rect.Top < currentMinTopLeft) {
                        currentMinTopLeft = rect.Top; 
                    }
 
                    if (rect.Bottom > currentMaxBottomRight) { 
                        currentMaxBottomRight = rect.Bottom;
                    } 

                }
                else {
                    //identify a new column 
                    if (flowDirection == FlowDirection.TopDown ? rect.Y < lastOffset : rect.Y > lastOffset) {
                        Debug.Assert(currentMinTopLeft > 0 && currentMaxBottomRight > 0, "How can we not have a min/max value?"); 
                        if (currentMinTopLeft > 0 && currentMaxBottomRight > 0) { 
                            //store off the
                            // min Top|Left for Row/Col 
                            // max Bottom|Right for Row/Col
                            // Height/Width of Row/Col
                            // Control index
                            commonSizes.Add(new Rectangle(currentMinTopLeft, currentMaxBottomRight, currentMaxBottomRight - currentMinTopLeft, i)); 
                            currentMinTopLeft = Int32.MaxValue;
                            currentMaxBottomRight = -1; 
                        } 
                    }
 
                    lastOffset = rect.Y;

                    //be sure to track the column size
                    if (rect.Left < currentMinTopLeft) { 
                        currentMinTopLeft = rect.Left;
                    } 
 
                    if (rect.Right > currentMaxBottomRight) {
                        currentMaxBottomRight = rect.Right; 
                    }
                }

            } 

            //add the last row/column to our commonsizes 
            if (currentMinTopLeft > 0 && currentMaxBottomRight > 0) { 
                //store off the max size for this row
                commonSizes.Add(new Rectangle(currentMinTopLeft, currentMaxBottomRight, currentMaxBottomRight - currentMinTopLeft, i)); 
            }

            //pass2 - adjust all controls to max width/height according to their row/col
            int controlIndex = 0; 
            for (i = 0; i < commonSizes.Count; i++) {
                while (controlIndex < ((Rectangle)commonSizes[i]).Height) {// Remember that Height is control index 
                    if (horizontalFlow) { 
                        childInfo[controlIndex].marginBounds.Y = ((Rectangle)commonSizes[i]).X; // X is Top
                        childInfo[controlIndex].marginBounds.Height = ((Rectangle)commonSizes[i]).Width; 
                    }
                    else {
                        childInfo[controlIndex].marginBounds.X = ((Rectangle)commonSizes[i]).X; // X is Left
                        childInfo[controlIndex].marginBounds.Width = ((Rectangle)commonSizes[i]).Width; 
                    }
 
                    controlIndex++; 
                }
            } 

        }
        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.Initialize"]/*' />
        /// <devdoc> 
        ///      Overrides the base .
        /// </devdoc> 
        public override void Initialize(IComponent component) { 
            base.Initialize(component);
 
            // VSWhidbey #424845. If the FLP is inheritedreadonly, so should all of the children
            if (InheritanceAttribute == InheritanceAttribute.InheritedReadOnly) {
                for (int i = 0; i < Control.Controls.Count; i++) {
                    TypeDescriptor.AddAttributes(Control.Controls[i], InheritanceAttribute.InheritedReadOnly); 
                }
            } 
 

 
        }


        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.OnChildControlAdded"]/*' /> 
        /// <devdoc>
        ///     When a child is added -we check to see if we cached an index 
        ///     representing where this control should be inserted.  If so, we 
        ///     re-insert the new child.
        /// 
        ///     This is only done on an external drag-drop
        /// </devdoc>
        private void OnChildControlAdded(object sender, ControlEventArgs e) {
 
            if (insertIndex != InvalidIndex) { // this will only be true on a drag-drop
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost; 
                if (host != null) { 
                    IComponentChangeService cs = GetService(typeof(IComponentChangeService)) as IComponentChangeService;
                    PropertyDescriptor controlsProp = TypeDescriptor.GetProperties(Control)["Controls"]; 
                    if (cs != null && controlsProp != null) {
                        cs.OnComponentChanging(Control, controlsProp);
                        //on an external drag/drop, the control will have been inserted at the end, so we can safely
                        //set the index and increment it, since we are moving the control backwards. Check out 
                        //SetChildIndex and MoveElement.
                        Control.Controls.SetChildIndex(e.Control, insertIndex); 
                        ++insertIndex; 
                        cs.OnComponentChanged(Control, controlsProp, null, null);
                    } 
                }
            }
        }
 

        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.OnDragDrop"]/*' /> 
        /// <devdoc> 
        ///     On a drop, if we have cached a special index where we think a control
        ///     should be inserted - we check to see if this was a pure-local drag 
        ///     (i.e. we dragged a child control inside ourself).  If so, we re-insert the
        ///     child to the appropriate index.  Otherwise, we'll do this in the ChildAdded
        ///     event.
        /// </devdoc> 

        protected override void OnDragDrop(DragEventArgs de) { 
 
            bool localDrag = false;
            if (dragControls != null && primaryDragControl != null && Control.Controls.Contains(primaryDragControl)) { 
                localDrag = true;
            }

            if (!localDrag) { 
                //if we are not a local drag then just let the base handle it.
 
                if (Control != null) { 
                    Control.ControlAdded += new ControlEventHandler(this.OnChildControlAdded);
                } 
                try {
                        base.OnDragDrop(de);
                }
                finally { 
                    if (Control != null) {
                        Control.ControlAdded -= new ControlEventHandler(this.OnChildControlAdded); 
                    } 
                }
            } 
            else {

                // local drag. We do it ourselves, so that we can set the indices right
 
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;
                if (host != null) { 
 
                    DesignerTransaction trans = null;
                    string transDesc; 
                    bool performCopy = (de.Effect == DragDropEffects.Copy);
                    // We use this list when doing a Drag-Copy, so that we can correctly restore state when we are done.
                    ArrayList originalControls = null;
                    ISelectionService selSvc = null; 

 
                    if (dragControls.Count == 1) { 
                        string name = TypeDescriptor.GetComponentName(dragControls[0]);
                        if (name == null || name.Length == 0) { 
                            name = dragControls[0].GetType().Name;
                        }
                        transDesc = SR.GetString(performCopy ? SR.BehaviorServiceCopyControl : SR.BehaviorServiceMoveControl, name);
                    } 
                    else {
                        transDesc = SR.GetString(performCopy ? SR.BehaviorServiceCopyControls : SR.BehaviorServiceMoveControls, dragControls.Count); 
                    } 

                    trans = host.CreateTransaction(transDesc) ; 

                    try {

                        //In order to be able to set the index correctly, we need to create a backwards move. 
                        //We do this by first finding the control foo that corresponds to insertIndex.
                        //We then remove all the drag controls from the FLP. 
                        //Then we get the new childIndex for the control foo. 
                        //Finally we loop:
                        //      add the ith drag control 
                        //      set its child index to (index of control foo) - 1
                        //On each iteration, the child index of control foo will change.
                        //
                        //This ensures that we can move both contiguous and non-contiguous selections. 

                        //Special case when the element we are inserting before is a part of the dragControls 
                        while ((insertIndex < childInfo.Length-1) && (childInfo[insertIndex].inSelectionColl)){ 
                            // Find the next control that is not a part of the selection
                            ++insertIndex; 
                        }

                        IComponentChangeService cs = GetService(typeof(IComponentChangeService)) as IComponentChangeService;
                        PropertyDescriptor controlsProp = TypeDescriptor.GetProperties(Control)["Controls"]; 
                        Control control = null;
 
                        if (insertIndex != childInfo.Length) { 
                            control = Control.Controls[insertIndex];
                        } 
                        else {
                            //we are inserting past the last control
                            insertIndex = -1;
                        } 

 
                        if (cs != null && controlsProp != null) { 
                            cs.OnComponentChanging(Control, controlsProp);
                        } 

                        //remove the controls in the dragcollection - don't need to do this if we are copying
                        if (!performCopy) {
                            for (int i = 0; i < dragControls.Count; i++) { 
                                Control.Controls.Remove(dragControls[i] as Control);
                            } 
 
                            // get the new index -- if we are performing a copy, then the index is the same
                            if (control != null) { 
                                insertIndex = Control.Controls.GetChildIndex(control, false);
                            }
                        }
                        else { 
                            // We are doing a copy, so let's copy the controls
#if DEBUG 
                            if (control != null) { 
                                Debug.Assert(insertIndex == Control.Controls.GetChildIndex(control, false), "Why are the indices not the same?");
                            } 
#endif

                            // Get the objects to copy
                            ArrayList temp = new ArrayList(); 
                            for (int i = 0; i < dragControls.Count; i++) {
                                temp.Add(dragControls[i]); 
                            } 

                            // Create a copy of them 
                            temp = DesignerUtils.CopyDragObjects(temp, Component.Site) as ArrayList;
                            if (temp == null) {
                                Debug.Fail("Couldn't create copies of the controls we are dragging.");
                                return; 
                            }
 
                            originalControls = new ArrayList(); 

                            // And stick the copied controls back into the dragControls array 
                            for (int j = 0; j < temp.Count; j++) {
                                // ... but save off the old controls first
                                originalControls.Add(dragControls[j]);
                                // remember to set the new primary control 
                                if (primaryDragControl.Equals(dragControls[j] as Control)) {
                                    primaryDragControl = temp[j] as Control; 
                                } 
                                dragControls[j] = temp[j];
                            } 

                            selSvc = (ISelectionService)GetService(typeof(ISelectionService));

                        } 

                        if (insertIndex == -1) { 
                            //Either insertIndex was childInfo.Length (inserting past the end) or 
                            //insertIndex was childInfo.Length - 1 and the control at that index was also
                            //a part of the dragCollection. In either case, the new index is equal to the count 
                            //of existing controls in the controlCollection. Helps to draw this out...
                            insertIndex = Control.Controls.Count;
                        }
 
                        //do the primary control first
                        Control.Controls.Add(primaryDragControl); 
                        Control.Controls.SetChildIndex(primaryDragControl, insertIndex); 
                        ++insertIndex;
                        if (selSvc != null) { 
                            Debug.Assert(performCopy, "selSvc should only be non-null when we are doing a local copy");
                            selSvc.SetSelectedComponents(new object[] {primaryDragControl}, SelectionTypes.Primary | SelectionTypes.Replace);
                        }
 
                        //now do the rest
                        // 
                        //Note dragControls are in opposite zorder than what FLP uses, 
                        //so add from the end.
                        for (int i = dragControls.Count - 1; i >= 0; i--) { 
                            if (primaryDragControl.Equals(dragControls[i] as Control)) {
                                continue;
                            }
                            Control.Controls.Add(dragControls[i] as Control); 
                            Control.Controls.SetChildIndex(dragControls[i] as Control, insertIndex);
                            ++insertIndex; 
                            if (selSvc != null) { 
                                selSvc.SetSelectedComponents(new object[] {dragControls[i]}, SelectionTypes.Add);
                            } 

                        }

                        if (cs != null && controlsProp != null) { 
                            cs.OnComponentChanged(Control, controlsProp, null, null);
                        } 
 
                        // If we did a Copy, then restore the old controls to make sure we set state correctly
                        if (originalControls != null) { 
                            for (int i = 0; i < originalControls.Count; i++) {
                                dragControls[i] = originalControls[i];
                            }
                        } 

                        base.OnDragComplete(de); 
 
                        if (trans != null) {
                            trans.Commit(); 
                            trans = null;
                        }
                    }
 
                    finally {
                        if (trans != null) { 
                            trans.Cancel(); 
                        }
                    } 

                }

            } 

            insertIndex = InvalidIndex; 
        } 

        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.OnDragLeave"]/*' /> 
        /// <devdoc>
        ///     Called when a drag-drop operation leaves the control designer view
        ///
        /// </devdoc> 
        protected override void OnDragLeave(EventArgs e) {
            EraseIBar(); 
            insertIndex = InvalidIndex; 
            primaryDragControl = null;
            if (dragControls != null) { 
                dragControls.Clear();
            }
            base.OnDragLeave(e);
        } 

 
        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.OnDragEnter"]/*' /> 
        /// <devdoc>
        ///     When we receive a drag enter notification - we clear our recommended insertion 
        ///     index and mose loc - then call our method to cache all the bounds of the children.
        /// </devdoc>
        protected override void OnDragEnter(DragEventArgs de) {
            base.OnDragEnter(de); 

            insertIndex = InvalidIndex; 
            lastMouseLoc = Point.Empty; 
            primaryDragControl = null;
 
            //Get the sorted drag controls. We use these for an internal drag.
            DropSourceBehavior.BehaviorDataObject data = de.Data as DropSourceBehavior.BehaviorDataObject;
            if (data != null) {
                int primaryIndex = -1; 
                dragControls = data.GetSortedDragControls(ref primaryIndex);
                primaryDragControl = dragControls[primaryIndex] as Control; 
            } 

 
            //cache all child bounds and identify rows/cols
            CreateMarginBoundsList();

        } 

        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.OnDragOver"]/*' /> 
        /// <devdoc> 
        ///     During a drag over, if we have successfully cached marign/row/col information
        ///     we will attempt to render an "I-bar" for the user based on where we think the 
        ///     user is attempting to insert the control at.  Note that we also cache off this
        ///     guessed-index so that if a control is dropped/added we can re-insert it at this
        ///     spot.
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        protected override void OnDragOver(DragEventArgs de) { 
            base.OnDragOver(de); 

            Point mouseLoc = System.Windows.Forms.Control.MousePosition; 

            if (mouseLoc.Equals(lastMouseLoc) || childInfo == null || childInfo.Length == 0 || commonSizes.Count == 0) {
                //no layout data to work with
                return; 
            }
 
            Rectangle bounds = Rectangle.Empty; 
            lastMouseLoc = mouseLoc;
            Point controlOffset = Control.PointToScreen(new Point(0,0)); 
            if (Control.RightToLeft == RightToLeft.Yes) {
                controlOffset.X += Control.Width;
            }
 
            insertIndex = InvalidIndex;
 
            //brute force hit testing to first determine if we're over one 
            //of our margin bounds
            // 
            int i = 0;
            for(i = 0; i < childInfo.Length; i++) {
                if (childInfo[i].marginBounds.Contains(mouseLoc)) {
                    bounds = childInfo[i].controlBounds; 
                    break;
                } 
            } 

            //If we found the bounds - then we need to draw our "I-Beam" 
            //If the mouse is over one of the marginbounds, then the dragged control
            //will always be inserted before the control the margin area represents. Thus
            //we will always draw the I-Beam to the left or above (FlowDirection.LRT | TB) or
            //to the right or below (FlowDirection.RTL | BT). 
            if (!bounds.IsEmpty) {
                insertIndex = i;//the insertion index will always be the boxed area (called margin area) we are over 
 
                if (childInfo[i].inSelectionColl) {
                    //If the marginBounds is part of the selection, then don't draw the IBar. But actually 
                    //setting insertIndex, will allows us to correctly drop the control in the right place.
                    EraseIBar();
                }
                else { 
                    FlowDirection direction = RTLTranslateFlowDirection(Control.FlowDirection);
                    if (direction == FlowDirection.LeftToRight) { 
                        ReDrawIBar(new Point(bounds.Left, bounds.Top), new Point( bounds.Left, bounds.Bottom)); 
                    }
                    else if (direction == FlowDirection.RightToLeft) { 
                        ReDrawIBar(new Point(bounds.Right, bounds.Top), new Point( bounds.Right, bounds.Bottom));
                    }
                    else if (direction == FlowDirection.TopDown) {
                        ReDrawIBar(new Point(bounds.Left, bounds.Top), new Point(bounds.Right, bounds.Top)); 
                    }
                    else if (direction == FlowDirection.BottomUp) { 
                        ReDrawIBar(new Point(bounds.Left, bounds.Bottom), new Point(bounds.Right, bounds.Bottom)); 
                    }
                    else { 
                        Debug.Fail("Unknown FlowDirection");
                    }

                } 
            }
            else { 
                //here, we're in a dead area - see what row / column we're in for a 
                //best-guess at the insertion index
                int offset = HorizontalFlow ? controlOffset.Y : controlOffset.X; 
                bool isRTL = (Control.RightToLeft == RightToLeft.Yes);

                for ( i =0; i < commonSizes.Count; i++) {
                    if (isRTL) { 
                        offset -= ((Rectangle)commonSizes[i]).Width; // Width is height/width of row/col
                    } 
                    else { 
                        offset += ((Rectangle)commonSizes[i]).Width;
                    } 

                    // Just introducing match for readability.
                    bool match = false;
                    if (!isRTL) { 
                        match = (HorizontalFlow ? (mouseLoc.Y) : (mouseLoc.X)) <= offset;
                    } 
                    else { 
                        match = (HorizontalFlow && mouseLoc.Y <= offset) || (!HorizontalFlow && mouseLoc.X >= offset);
                    } 

                    if (match) {
                        insertIndex = ((Rectangle)commonSizes[i]).Height; // Height is index of last control
                        bounds = childInfo[insertIndex - 1].controlBounds; 
                        if (childInfo[insertIndex - 1].inSelectionColl) {
                            EraseIBar(); 
                        } 
                        else {
                            FlowDirection direction = RTLTranslateFlowDirection(Control.FlowDirection); 
                            if (direction == FlowDirection.LeftToRight) {
                                ReDrawIBar(new Point(bounds.Right, bounds.Top), new Point(bounds.Right, bounds.Bottom));
                            }
                            else if (direction == FlowDirection.RightToLeft) { 
                                ReDrawIBar(new Point(bounds.Left, bounds.Top), new Point(bounds.Left, bounds.Bottom));
                            } 
                            else if (direction == FlowDirection.TopDown) { 
                                ReDrawIBar(new Point(bounds.Left, bounds.Bottom), new Point(bounds.Right, bounds.Bottom));
                            } 
                            else if (direction == FlowDirection.BottomUp) {
                                ReDrawIBar(new Point(bounds.Left, bounds.Top), new Point(bounds.Right, bounds.Top));
                            }
                            else { 
                                Debug.Fail("Unknown FlowDirection");
                            } 
                        } 
                        break;
                    } 


                }
            } 

            if (insertIndex == InvalidIndex) { 
                //here, we're at the 'end' of the flowlayoutpanel - not over 
                //any controls and not in a row/column.
                insertIndex = Control.Controls.Count; 
                EraseIBar();
            }

        } 

 
        private void EraseIBar() { 
            ReDrawIBar(Point.Empty, Point.Empty);
        } 

        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.ReDrawIBar"]/*' />
        /// <devdoc>
        ///     Given two points, we'll draw an 'Ibar".  Note that we only erase at our 
        ///     old points if they are different from the new ones.  Also note that if
        ///     the points are empty - we will simply erase and not draw. 
        /// </devdoc> 
        private void ReDrawIBar(Point p1, Point p2) {
 
            //offset the points to adorner coords
            Point offset = BehaviorService.AdornerWindowToScreen();

            Pen pen = SystemPens.ControlText; 
            if (Control.BackColor != Color.Empty && Control.BackColor.GetBrightness() < .5) {
                pen = SystemPens.ControlLight; 
            } 

 
            //don't offset if p1 is empty. Empty really just means that we want to erase the IBar.
            if (p1 != Point.Empty) {
                p1.Offset(-offset.X, -offset.Y);
                p2.Offset(-offset.X, -offset.Y); 
            }
 
            //only erase the ibar if the points are different from last time 
            //Only invalidate if there's something to invalidate
            if (p1 != oldP1 && p2 != oldP2 && oldP1 != Point.Empty) { 
                Rectangle invalidRect = new Rectangle(oldP1.X, oldP1.Y, oldP2.X - oldP1.X + 1, oldP2.Y - oldP1.Y + 1);
                invalidRect.Inflate(maxIBarWidth, maxIBarWidth);//akways invalidate max area
                BehaviorService.Invalidate(invalidRect);
            } 

            //cache this for next time around -- but do so before changing p1 and p2 below. 
            oldP1 = p1; 
            oldP2 = p2;
 
            //if we have valid new points - redraw our ibar
            //we always want to redraw the line. This is because part of it could have been erased when
            //the dragimage (see DropSourceBehavior) is being moved over the IBar.
 
            if (p1 != Point.Empty) {
                using (Graphics g = BehaviorService.AdornerWindowGraphics) { 
                    if (HorizontalFlow) { 

                        if (Math.Abs(p1.Y - p2.Y) <= minIBar) { 
                            //draw the smaller, simpler IBar
                            g.DrawLine(pen, p1, p2);//vertical line
                            g.DrawLine(pen, p1.X -iBarHalfSize, p1.Y, p1.X + iBarHalfSize, p1.Y);//top hat
                            g.DrawLine(pen, p2.X -iBarHalfSize, p2.Y, p2.X + iBarHalfSize, p2.Y);//bottom hat 
                        }
                        else { 
 
                            //top and bottom hat
                            for (int i = 0; i < iBarHatHeight - 1; i++) { // stop 1 pixel before, since we can't draw a 1 pixel line 
                                // reducing the width of the hat with 2 pixel on each iteration
                                g.DrawLine(pen, p1.X - (iBarHatWidth - 1 - (i*2))/2, p1.Y+i, p1.X + (iBarHatWidth - 1 - (i*2))/2, p1.Y+i);//top hat

                                g.DrawLine(pen, p2.X - (iBarHatWidth - 1 - (i*2))/2, p2.Y-i, p2.X + (iBarHatWidth - 1 - (i*2))/2, p2.Y-i);//bottom hat 
                            }
 
                            //can't draw a 1 pixel line, so draw a vertical line 
                            g.DrawLine(pen, p1.X, p1.Y, p1.X, p1.Y + iBarHatHeight - 1); // top hat
                            g.DrawLine(pen, p2.X, p2.Y, p2.X, p2.Y - iBarHatHeight + 1); // bottom hat 

                            // vertical line

                            g.DrawLine(pen, p1.X, p1.Y + iBarLineOffset, p2.X, p2.Y - iBarLineOffset);//vertical line 
                        }
 
                    } 
                    else {
                        if (Math.Abs(p1.X - p2.X) <= minIBar) { 
                            //draw the smaller, simpler IBar
                            g.DrawLine(pen, p1, p2);//horizontal line
                            g.DrawLine(pen, p1.X, p1.Y -iBarHalfSize, p1.X, p1.Y+ iBarHalfSize);//top hat
                            g.DrawLine(pen, p2.X, p2.Y -iBarHalfSize, p2.X, p2.Y + iBarHalfSize);//bottom hat 
                        }
                        else { 
                            //left and right hat 
                            for (int i = 0; i < iBarHatHeight - 1; i++) { // stop 1 pixel before, since we can't draw a 1 pixel line
                                // reducing the width of the hat with 2 pixel on each iteration 
                                g.DrawLine(pen, p1.X+i, p1.Y - (iBarHatWidth - 1 - (i*2))/2, p1.X+i, p1.Y + (iBarHatWidth - 1 - (i*2))/2);//left hat

                                g.DrawLine(pen, p2.X-i, p2.Y - (iBarHatWidth - 1 - (i*2))/2, p2.X-i, p2.Y + (iBarHatWidth - 1 - (i*2))/2);//right hat
                            } 

                            //can't draw a 1 pixel line, so draw a horizontal line 
                            g.DrawLine(pen, p1.X, p1.Y, p1.X + iBarHatHeight - 1, p1.Y); // left hat 
                            g.DrawLine(pen, p2.X, p2.Y, p2.X - iBarHatHeight + 1, p2.Y); // right hat
 
                            // horizontal line

                            g.DrawLine(pen, p1.X + iBarLineOffset, p1.Y, p2.X - iBarLineOffset, p2.Y);//horizontal line
                        } 

                    } 
                } 
            }
 

        }

        /// <include file='doc\FlowLayoutPanelDesigner.uex' path='docs/doc[@for="FlowLayoutPanelDesigner.PreFilterProperties"]/*' /> 
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
            base.PreFilterProperties(properties); 
 
            PropertyDescriptor prop;
 
            // Handle shadowed properties
            //
            string[] shadowProps = new string[] {
                "FlowDirection" 
            };
 
            Attribute[] empty = new Attribute[0]; 

            for (int i = 0; i < shadowProps.Length; i++) { 
                prop = (PropertyDescriptor)properties[shadowProps[i]];
                if (prop != null) {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(FlowLayoutPanelDesigner), prop, empty);
                } 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
