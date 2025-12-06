//------------------------------------------------------------------------------ 
// <copyright file="ToolStripItemBehavior.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
*/ 
namespace System.Windows.Forms.Design
{ 
    using System.Design;
    using Accessibility;
    using System.Runtime.Serialization.Formatters;
    using System.Threading; 
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Diagnostics.CodeAnalysis; 
    using System.Diagnostics;
    using System; 
    using System.Security;
    using System.Security.Permissions;
    using System.Collections;
    using System.ComponentModel.Design; 
    using System.ComponentModel.Design.Serialization;
    using System.Windows.Forms; 
    using System.Drawing; 
    using System.Drawing.Drawing2D;
    using System.Drawing.Design; 
    using Microsoft.Win32;
    using System.Windows.Forms.Design.Behavior;
    using System.Reflection;
 
    /// <summary>
    ///  The behavior for the glpyh that covers the items themselves.  This selects the items 
    /// when they are clicked, and will (when implemented) do the dragging/reordering of them. 
    /// </summary>
    internal class ToolStripItemBehavior : System.Windows.Forms.Design.Behavior.Behavior 
    {

        private const int GLYPHBORDER = 1;
        private const int GLYPHINSET = 2; 

        //new privates for "Drag Drop" 
        internal Rectangle dragBoxFromMouseDown = Rectangle.Empty; 
        private System.Windows.Forms.Timer _timer = null;
        private ToolStripItemGlyph selectedGlyph; 
        private bool doubleClickFired = false;
        private bool mouseUpFired = false;
        private Control dropSource;
        private IEventHandlerService eventSvc = null; 

        public ToolStripItemBehavior() 
        { 

        } 

        // Gets the DropSource control.
        private Control DropSource
        { 
            get
            { 
                if (dropSource == null) 
                {
                    dropSource = new Control(); 
                }
                return dropSource;
            }
        } 

 
        // Returns true if oldSelection and newSelection have a commonParent. 
        private bool CommonParent(ToolStripItem oldSelection, ToolStripItem newSelection)
        { 
            ToolStrip oldSelectionParent = oldSelection.GetCurrentParent();
            ToolStrip newSelectionParent = newSelection.GetCurrentParent();
            return (oldSelectionParent == newSelectionParent);
        } 

        /// <devdoc> 
        /// Clears the insertion mark when items are being reordered 
        /// </devdoc>
        private void ClearInsertionMark(ToolStripItem item) 
        {
            // Dont paint if cursor hasnt moved.
            if (ToolStripDesigner.LastCursorPosition != Point.Empty && ToolStripDesigner.LastCursorPosition == Cursor.Position)
            { 
                return;
            } 
 
            // Dont paint any "MouseOver" glyohs if TemplateNode is ACTIVE !
            ToolStripKeyboardHandlingService keyService = GetKeyBoardHandlingService(item); 
            if (keyService != null && keyService.TemplateNodeActive)
            {
                return;
            } 

            // stuff away the lastInsertionMarkRect 
            // and clear it out _before_ we call paint OW 
            // the call to invalidate wont help as it will get
            // repainted. 
            Rectangle bounds = Rectangle.Empty;
            if (item != null && item.Site != null)
            {
                IDesignerHost designerHost = (IDesignerHost)item.Site.GetService(typeof(IDesignerHost)); 
                if (designerHost != null)
                { 
                    bounds = GetPaintingBounds(designerHost, item); 
                    bounds.Inflate(GLYPHBORDER, GLYPHBORDER);
                    Region rgn = new Region(bounds); 
                    try
                    {
                        bounds.Inflate(-GLYPHINSET, -GLYPHINSET);
                        rgn.Exclude(bounds); 

                        BehaviorService bSvc = GetBehaviorService(item); 
                        if (bSvc != null && bounds != Rectangle.Empty) 
                        {
                            bSvc.Invalidate(rgn); 
                        }
                    }
                    finally
                    { 
                        rgn.Dispose();
                        rgn = null; 
                    } 
                }
            } 
        }

        // Tries to put the item in the Insitu edit mode after the double click timer has ticked
        private void EnterInSituMode(ToolStripItemGlyph glyph) 
        {
            if (glyph.ItemDesigner != null && !glyph.ItemDesigner.IsEditorActive) 
            { 

                glyph.ItemDesigner.ShowEditNode(false); 
            }
        }

        // Gets the Selection Service. 
        private ISelectionService GetSelectionService(ToolStripItem item)
        { 
            Debug.Assert(item != null, "Item passed is null, SelectionService cannot be obtained"); 
            if (item.Site != null)
            { 
                ISelectionService selSvc = (ISelectionService)item.Site.GetService(typeof(ISelectionService));
                Debug.Assert(selSvc != null, "Failed to get Selection Service!");
                return selSvc;
            } 
            return null;
 
        } 

        // Gets the Behavior Service. 
        private BehaviorService GetBehaviorService(ToolStripItem item)
        {
            Debug.Assert(item != null, "Item passed is null, BehaviorService cannot be obtained");
            if (item.Site != null) 
            {
                BehaviorService behaviorSvc = (BehaviorService)item.Site.GetService(typeof(BehaviorService)); 
                Debug.Assert(behaviorSvc != null, "Failed to get Behavior Service!"); 
                return behaviorSvc;
            } 
            return null;

        }
 
        // Gets the ToolStripKeyBoardHandling Service.
        private ToolStripKeyboardHandlingService GetKeyBoardHandlingService(ToolStripItem item) 
        { 
            Debug.Assert(item != null, "Item passed is null, ToolStripKeyBoardHandlingService cannot be obtained");
            if (item.Site != null) 
            {
                ToolStripKeyboardHandlingService keyBoardSvc = (ToolStripKeyboardHandlingService)item.Site.GetService(typeof(ToolStripKeyboardHandlingService));
                Debug.Assert(keyBoardSvc != null, "Failed to get ToolStripKeyboardHandlingService!");
                return keyBoardSvc; 
            }
            return null; 
 
        }
 
        // Gets the painting rect for SelectionRects
        private static Rectangle GetPaintingBounds(IDesignerHost designerHost, ToolStripItem item)
        {
            Rectangle bounds = Rectangle.Empty; 

            ToolStripItemDesigner itemDesigner = designerHost.GetDesigner(item) as ToolStripItemDesigner; 
            if (itemDesigner != null) 
            {
                bounds = itemDesigner.GetGlyphBounds(); 
                ToolStripDesignerUtils.GetAdjustedBounds(item, ref bounds);
                // So that the mouseOver glyph matches the selectionGlyph.
                bounds.Inflate(1,1);
                bounds.Width--; 
                bounds.Height--;
            } 
            return bounds; 
        }
 
        // This helper function will return true if any other MouseHandler (say TabOrder UI) is active, in which case we should not handle any Mouse Messages..
        // Since the TabOrder UI is pre-Whidbey when the TabOrder UI is up,
        // It adds a new Overlay (a window) to the DesignerFrame (something similar to AdornerWindow).
        // This UI is a transaparent control which has overrides foir Mouse Messages. It listens for all mouse messages through the IMouseHandler interface instead of using the new 
        // BehaviorService. Hence we have to special case this scenario. (CONTROL DESIGNER ALSO DOES THIS).
 
        private bool MouseHandlerPresent(ToolStripItem item) 
        {
            IMouseHandler mouseHandler = null; 

            if (eventSvc == null) {
                eventSvc = (IEventHandlerService)item.Site.GetService(typeof(IEventHandlerService));
            } 

            if (eventSvc != null) { 
                mouseHandler = (IMouseHandler)eventSvc.GetHandler(typeof(IMouseHandler)); 
            }
 
            return (mouseHandler != null);

        }
 
        // Occurs when the timer ticks after user has doubleclicked an item
        private void OnDoubleClickTimerTick(object sender, EventArgs e) 
        { 
            if (_timer != null)
            { 
                _timer.Enabled = false;
                _timer.Tick -= new System.EventHandler(OnDoubleClickTimerTick);
                _timer.Dispose();
                _timer = null; 
                // Enter Insitu ...
                if (selectedGlyph != null && selectedGlyph.Item is ToolStripMenuItem) 
                { 
                    EnterInSituMode(selectedGlyph);
                } 
            }

        }
 

 
 
        // Occurs when user doubleclicks on the TooLStripItem glyph
        public override bool OnMouseDoubleClick(Glyph g, MouseButtons button, Point mouseLoc) 
        {
            if (mouseUpFired)
            {
                doubleClickFired = true; 
            }
            return false; 
        } 

        // Occurs when MouseUp TooLStripItem glyph 
        public override bool OnMouseUp(Glyph g, MouseButtons button)
        {
            ToolStripItemGlyph glyph = g as ToolStripItemGlyph;
            ToolStripItem glyphItem = glyph.Item; 
            if (MouseHandlerPresent(glyphItem))
            { 
                return false; 
            }
            SetParentDesignerValuesForDragDrop(glyphItem, false, Point.Empty); 
            if (doubleClickFired)
            {
                if (glyph != null && button == MouseButtons.Left)
                { 
                    ISelectionService selSvc = GetSelectionService(glyphItem);
                    if (selSvc == null) 
                    { 
                        return false;
                    } 

                    ToolStripItem selectedItem = selSvc.PrimarySelection as ToolStripItem;
                    // Check if this item is already selected ...
                    if (selectedItem == glyphItem) 
                    {
                        // If timer != null.. we are in DoubleClick before the "InSitu Timer" so KILL IT. 
                        if (_timer != null) 
                        {
                            _timer.Enabled = false; 
                            _timer.Tick -= new System.EventHandler(OnDoubleClickTimerTick);
                            _timer.Dispose();
                            _timer = null;
                        } 
                        // If the Selecteditem is already in editmode ... bail out
                        if (selectedItem != null) 
                        { 
                            ToolStripItemDesigner selectedItemDesigner = glyph.ItemDesigner;
                            if (selectedItemDesigner != null && selectedItemDesigner.IsEditorActive) 
                            {
                                return false;
                            }
                            selectedItemDesigner.DoDefaultAction(); 
                        }
                        doubleClickFired = false; 
                        mouseUpFired = false; 
                    }
                } 
            }
            else
            {
                mouseUpFired = true; 
            }
            return false; 
        } 

        // Occurs when MouseDown on the TooLStripItem glyph 
        public override bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc)
        {
            ToolStripItemGlyph glyph = g as ToolStripItemGlyph;
            ToolStripItem glyphItem = glyph.Item; 

            ISelectionService selSvc = GetSelectionService(glyphItem); 
            BehaviorService bSvc = GetBehaviorService(glyphItem); 
            ToolStripKeyboardHandlingService keyService = GetKeyBoardHandlingService(glyphItem);
 
            if ((button == MouseButtons.Left) && (keyService != null) && (keyService.TemplateNodeActive))
            {
                if (keyService.ActiveTemplateNode.IsSystemContextMenuDisplayed)
                { 
                    // DevDiv Bugs: 144618 : skip behaviors when the context menu is displayed
                    return false; 
                } 
            }
 
            IDesignerHost designerHost = (IDesignerHost)glyphItem.Site.GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null, "Invalid DesignerHost");

            ToolStripItem currentSel = selSvc.PrimarySelection as ToolStripItem; 

            //Cache original selection 
            ICollection originalSelComps = null; 
            if (selSvc != null)
            { 
                originalSelComps = selSvc.GetSelectedComponents();
            }

            // Add the TemplateNode to the Selection if it is currently Selected as the GetSelectedComponents wont do it for us. 
            ArrayList origSel = new ArrayList(originalSelComps);
            if (origSel.Count == 0) 
            { 
                if (keyService != null && keyService.SelectedDesignerControl != null)
                { 
                    origSel.Add(keyService.SelectedDesignerControl);
                }
            }
 

            if (keyService != null) 
            { 
                keyService.SelectedDesignerControl = null;
                if (keyService.TemplateNodeActive) 
                {
                    // If templateNode Active .. commit and Select it
                    keyService.ActiveTemplateNode.CommitAndSelect();
                    // if the selected item is clicked .. then commit the node and reset the selection (refer 488002) 
                    if (currentSel != null && currentSel == glyphItem)
                    { 
                        selSvc.SetSelectedComponents(null, SelectionTypes.Replace); 
                    }
                } 
            }

            if (selSvc == null || MouseHandlerPresent(glyphItem))
            { 
                return false;
            } 
 
            if (glyph != null && button == MouseButtons.Left)
            { 
                ToolStripItem selectedItem = selSvc.PrimarySelection as ToolStripItem;

                // Always set the Drag-Rect for Drag-Drop...
                SetParentDesignerValuesForDragDrop(glyphItem, true, mouseLoc); 

                // Check if this item is already selected ... 
                if (selectedItem != null && selectedItem == glyphItem) 
                {
                    // If the Selecteditem is already in editmode ... bail out 
                    if (selectedItem != null)
                    {
                        ToolStripItemDesigner selectedItemDesigner = glyph.ItemDesigner;
                        if (selectedItemDesigner != null && selectedItemDesigner.IsEditorActive) 
                        {
                            return false; 
                        } 
                    }
 
                    // Check if this is CTRL + Click or SHIFT + Click, if so then just remove the selection
                    bool removeSel = (Control.ModifierKeys & (Keys.Control | Keys.Shift)) > 0;
                    if (removeSel)
                    { 
                        selSvc.SetSelectedComponents(new IComponent[] { selectedItem }, SelectionTypes.Remove);
                        return false; 
                    } 

                    //start Double Click Timer 
                    // This is required for the second down in selection
                    // which can be the first down of a Double click on the glyph
                    // confusing... hence this comment ...
                    // Heres the scenario .... 
                    // DOWN 1 - selects the ITEM
                    // DOWN 2 - ITEM goes into INSITU.... 
                    // DOUBLE CLICK - dont show code.. 
                    // Open INSITU after the double click time
                    if (selectedItem is ToolStripMenuItem) 
                    {
                        _timer = new System.Windows.Forms.Timer();
                        _timer.Interval = SystemInformation.DoubleClickTime;
                        _timer.Tick += new System.EventHandler(OnDoubleClickTimerTick); 
                        _timer.Enabled = true;
                        selectedGlyph = glyph; 
                    } 
                }
                else 
                {

                    bool shiftPressed = (Control.ModifierKeys & Keys.Shift) > 0;
 
                    // We should process MouseDown only if we are not yet selected....
                    if (!selSvc.GetComponentSelected(glyphItem)) 
                    { 
                        //Reset the State...
                        // On the Glpyhs .. we get MouseDown - Mouse UP (for single Click) 
                        // And we get MouseDown - MouseUp - DoubleClick - Up (for double Click)
                        // Hence reset the state at start....
                        mouseUpFired = false;
                        doubleClickFired = false; 

                        //Implementing Shift + Click.... 
                        // we have 2 items, namely, selectedItem (current PrimarySelection) and glyphItem (item which has received mouseDown) 
                        // FIRST check if they have common parent...
                        // IF YES then get the indices of the two and SELECT all items from LOWER index to the HIGHER index. 
                        if (shiftPressed && (selectedItem != null && CommonParent(selectedItem, glyphItem)))
                        {
                            ToolStrip parent = null;
                            if (glyphItem.IsOnOverflow) 
                            {
                                parent = glyphItem.Owner; 
                            } 
                            else
                            { 
                                parent = glyphItem.GetCurrentParent();
                            }
                            int startIndexOfSelection = Math.Min(parent.Items.IndexOf(selectedItem), parent.Items.IndexOf(glyphItem));
                            int endIndexOfSelection = Math.Max(parent.Items.IndexOf(selectedItem), parent.Items.IndexOf(glyphItem)); 
                            int countofItemsSelected = (endIndexOfSelection - startIndexOfSelection) + 1;
 
                            // if two adjacent items are selected ... 
                            if (countofItemsSelected == 2)
                            { 
                                selSvc.SetSelectedComponents(new IComponent[] { glyphItem });
                            }
                            else
                            { 
                                object[] totalObjects = new object[countofItemsSelected];
                                int j = 0; 
                                for (int i = startIndexOfSelection; i <= endIndexOfSelection; i++) 
                                {
                                    totalObjects[j++] = parent.Items[i]; 
                                }
                                selSvc.SetSelectedComponents(new IComponent[] { parent } , SelectionTypes.Replace);
                                ToolStripDesigner.shiftState = true;
                                selSvc.SetSelectedComponents(totalObjects, SelectionTypes.Replace); 
                            }
                        } 
                        //End Implmentation 
                        else
                        { 
                            if (glyphItem.IsOnDropDown && ToolStripDesigner.shiftState)
                            {
                                //Invalidate glyh only if we are in ShiftState...
                                ToolStripDesigner.shiftState = false; 
                                if (bSvc != null)
                                { 
                                    bSvc.Invalidate(glyphItem.Owner.Bounds); 
                                }
                            } 
                            selSvc.SetSelectedComponents(new IComponent[] { glyphItem }, SelectionTypes.Auto);
                        }
                        // Set the appropriate object.
                        if (keyService != null) 
                        {
                            keyService.ShiftPrimaryItem = glyphItem; 
                        } 
                    }
                    // we are already selected and if shiftpressed... 
                    else if (shiftPressed || (Control.ModifierKeys & Keys.Control) > 0)
                    {
                        selSvc.SetSelectedComponents(new IComponent[] { glyphItem }, SelectionTypes.Remove);
                    } 
                }
            } 
 
            if (glyph != null && button == MouseButtons.Right)
            { 
                if (!selSvc.GetComponentSelected(glyphItem))
                {
                    selSvc.SetSelectedComponents(new IComponent[] { glyphItem });
                } 
            }
 
            // finally Invalidate all selections 
            ToolStripDesignerUtils.InvalidateSelection(origSel, glyphItem, glyphItem.Site, false);
 
            return false;
        }

        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseEnter"]/*' /> 
        /// <devdoc>
        ///    Overriden to paint the border on mouse enter..... 
        /// </devdoc> 
        public override bool OnMouseEnter(Glyph g)
        { 
            ToolStripItemGlyph glyph = g as ToolStripItemGlyph;
            if (glyph != null)
            {
                ToolStripItem glyphItem = glyph.Item; 

                if (MouseHandlerPresent(glyphItem)) 
                { 
                    return false;
                } 

                ISelectionService selSvc = GetSelectionService(glyphItem);
                if (selSvc != null)
                { 
                    if (!selSvc.GetComponentSelected(glyphItem))
                    { 
                        PaintInsertionMark(glyphItem); 
                    }
                } 
            }
            return false;
        }
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseLeave"]/*' />
        /// <devdoc> 
        ///     overriden to "clear" the boundary-paint when the mouse leave the item 
        /// </devdoc>
        public override bool OnMouseLeave(Glyph g) 
        {
            ToolStripItemGlyph glyph = g as ToolStripItemGlyph;
            if (glyph != null)
            { 
                ToolStripItem glyphItem = glyph.Item;
 
                if (MouseHandlerPresent(glyphItem)) 
                {
                    return false; 
                }

                ISelectionService selSvc = GetSelectionService(glyphItem);
                if (selSvc != null) 
                {
                    if (!selSvc.GetComponentSelected(glyphItem)) 
                    { 
                        ClearInsertionMark(glyphItem);
                    } 
                }
            }
            return false;
        } 

        // Implementation of Drag Drop .... 
        /// <include file='doc\ToolStripItemBehavior.uex' path='docs/doc[@for="ToolStripItemBehavior.OnMouseMove"]/*' /> 
        /// <devdoc>
        ///     When any MouseMove message enters the BehaviorService's AdornerWindow 
        ///     (mousemove, ncmousemove) it is first passed here, to the top-most Behavior
        ///     in the BehaviorStack.  Returning 'true' from this function signifies that
        ///     the Message was 'handled' by the Behavior and should not continue to be processed.
        /// </devdoc> 
        public override bool OnMouseMove(Glyph g, MouseButtons button, Point mouseLoc)
        { 
 
            bool retVal = false;
 
            ToolStripItemGlyph glyph = g as ToolStripItemGlyph;
            ToolStripItem glyphItem = glyph.Item;

            ISelectionService selSvc = GetSelectionService(glyphItem); 

            if (selSvc == null || glyphItem.Site == null || MouseHandlerPresent(glyphItem)) 
            { 
                return false;
            } 



            //ToolStirpItems now support selection on mousemove .... 
            if (!selSvc.GetComponentSelected(glyphItem))
            { 
                PaintInsertionMark(glyphItem); 
                retVal = false;
            } 


            if (button == MouseButtons.Left && glyph != null && glyph.ItemDesigner != null && !glyph.ItemDesigner.IsEditorActive)
            { 
                Rectangle dragBox = Rectangle.Empty;
 
                IDesignerHost designerHost = (IDesignerHost)glyphItem.Site.GetService(typeof(IDesignerHost)); 
                Debug.Assert(designerHost != null, "Invalid DesignerHost");
 
                if (glyphItem.Placement == ToolStripItemPlacement.Overflow || (glyphItem.Placement == ToolStripItemPlacement.Main && !(glyphItem.IsOnDropDown)))
                {
                    ToolStripItemDesigner itemDesigner = glyph.ItemDesigner;
                    ToolStrip parentToolStrip = itemDesigner.GetMainToolStrip(); 
                    ToolStripDesigner parentDesigner = designerHost.GetDesigner(parentToolStrip) as ToolStripDesigner;
                    if (parentDesigner != null) 
                    { 
                        dragBox = parentDesigner.DragBoxFromMouseDown;
                    } 
                }
                else if (glyphItem.IsOnDropDown)
                {
                    //Get the OwnerItem's Designer and set the value... 
                    ToolStripDropDown parentDropDown = glyphItem.Owner as ToolStripDropDown;
                    if (parentDropDown != null) 
                    { 
                        ToolStripItem ownerItem = parentDropDown.OwnerItem;
                        ToolStripItemDesigner ownerItemDesigner = designerHost.GetDesigner(ownerItem) as ToolStripItemDesigner; 
                        if (ownerItemDesigner != null)
                        {
                            dragBox = ownerItemDesigner.dragBoxFromMouseDown;
                        } 
                    }
                } 
 

                // If the mouse moves outside the rectangle, start the drag. 
                if (dragBox != Rectangle.Empty && !dragBox.Contains(mouseLoc.X, mouseLoc.Y))
                {

                    if (_timer != null) 
                    {
                        _timer.Enabled = false; 
                        _timer.Tick -= new System.EventHandler(OnDoubleClickTimerTick); 
                        _timer.Dispose();
                        _timer = null; 
                    }

                    // Proceed with the drag and drop, passing in the list item.
                    try 
                    {
                        // ---------------- create list of components -------------------------------------------------// 
                        ArrayList dragItems = new ArrayList(); 
                        ICollection selComps = selSvc.GetSelectedComponents();
 
                        //create our list of controls-to-drag
                        foreach (IComponent comp in selComps)
                        {
                            ToolStripItem item = comp as ToolStripItem; 
                            if (item != null)
                            { 
                                dragItems.Add(item); 
                            }
                        } 

                        ToolStripItem selectedItem = selSvc.PrimarySelection as ToolStripItem;
                        //Start Drag-Drop only if ToolStripItem is the primary Selection
                        if (selectedItem != null) 
                        {
                            ToolStrip owner = selectedItem.Owner; 
 
                            ToolStripItemDataObject data = new ToolStripItemDataObject(dragItems, selectedItem, owner);
                            // ---------------- create list of components -------------------------------------------------// 


                            DropSource.QueryContinueDrag += new QueryContinueDragEventHandler(QueryContinueDrag);
                            ToolStripDropDownItem ddItem = glyphItem as ToolStripDropDownItem; 
                            if (ddItem != null)
                            { 
                                ToolStripMenuItemDesigner itemDesigner = designerHost.GetDesigner(ddItem) as ToolStripMenuItemDesigner; 
                                if (itemDesigner != null)
                                { 
                                    itemDesigner.InitializeBodyGlyphsForItems(false, ddItem);
                                    ddItem.HideDropDown();
                                }
                            } 
                            else if (glyphItem.IsOnDropDown && !glyphItem.IsOnOverflow)
                            { 
                                ToolStripDropDown dropDown = glyphItem.GetCurrentParent() as ToolStripDropDown; 
                                ToolStripDropDownItem ownerItem = dropDown.OwnerItem as ToolStripDropDownItem;
                                selSvc.SetSelectedComponents(new IComponent[] { ownerItem }, SelectionTypes.Replace); 
                            }
                            DragDropEffects dropEffect = DropSource.DoDragDrop(data, DragDropEffects.All);
                        }
                    } 
                    finally
                    { 
                        DropSource.QueryContinueDrag -= new QueryContinueDragEventHandler(QueryContinueDrag); 
                        //Reset all Drag-Variables
                        SetParentDesignerValuesForDragDrop(glyphItem, false, Point.Empty); 
                        ToolStripDesigner.dragItem = null;
                        dropSource = null;
                    }
 
                    retVal = false;
                } 
            } 
            return retVal;
        } 


        //  OLE DragDrop virtual methods
        // 
        /// <include file='doc\ToolStripItemBehavior.uex' path='docs/doc[@for="ToolStripItemBehavior.OnDragDrop"]/*' />
        /// <devdoc> 
        ///     OnDragDrop can be overridden so that a Behavior can specify its own 
        ///     Drag/Drop rules.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        public override void OnDragDrop(Glyph g, DragEventArgs e)
        {
            ToolStripItem currentDropItem = ToolStripDesigner.dragItem; 

            // Ensure that the list item index is contained in the data. 
            if (e.Data is ToolStripItemDataObject && currentDropItem != null) 
            {
                ToolStripItemDataObject data = (ToolStripItemDataObject)e.Data; 
                // Get the PrimarySelection before the Drag operation...
                ToolStripItem selectedItem = data.PrimarySelection;

                IDesignerHost designerHost = (IDesignerHost)currentDropItem.Site.GetService(typeof(IDesignerHost)); 
                Debug.Assert(designerHost != null, "Invalid DesignerHost");
 
                //Do DragDrop only if currentDropItem has changed. 
                if (currentDropItem != selectedItem && designerHost != null)
                { 

                    ArrayList components = data.DragComponents;
                    ToolStrip parentToolStrip = currentDropItem.GetCurrentParent() as ToolStrip;
                    int primaryIndex = -1; 
                    string transDesc;
                    bool copy = (e.Effect == DragDropEffects.Copy); 
 
                    if (components.Count == 1)
                    { 
                        string name = TypeDescriptor.GetComponentName(components[0]);
                        if (name == null || name.Length == 0)
                        {
                            name = components[0].GetType().Name; 
                        }
                        transDesc = SR.GetString(copy ? SR.BehaviorServiceCopyControl : SR.BehaviorServiceMoveControl, name); 
                    } 
                    else
                    { 
                        transDesc = SR.GetString(copy ? SR.BehaviorServiceCopyControls : SR.BehaviorServiceMoveControls, components.Count);
                    }

 
                    DesignerTransaction designerTransaction = designerHost.CreateTransaction(transDesc);
                    try 
                    { 
                        IComponentChangeService changeSvc = (IComponentChangeService)currentDropItem.Site.GetService(typeof(IComponentChangeService));
                        if (changeSvc != null) 
                        {
                            ToolStripDropDown dropDown = parentToolStrip as ToolStripDropDown;
                            if (dropDown != null)
                            { 
                                ToolStripItem ownerItem = dropDown.OwnerItem;
                                changeSvc.OnComponentChanging(ownerItem, TypeDescriptor.GetProperties(ownerItem)["DropDownItems"]); 
                            } 
                            else
                            { 
                                changeSvc.OnComponentChanging(parentToolStrip, TypeDescriptor.GetProperties(parentToolStrip)["Items"]);
                            }
                        }
 
                        // If we are copying, then we want to make a copy of the components we are dragging
                        if (copy) 
                        { 

                            // Remember the primary selection if we had one 
                            if (selectedItem != null)
                            {
                                primaryIndex = components.IndexOf(selectedItem);
                            } 
                            ToolStripKeyboardHandlingService keyboardHandlingService = GetKeyBoardHandlingService(selectedItem);
                            if (keyboardHandlingService != null) 
                            { 
                                keyboardHandlingService.CopyInProgress = true;
                            } 
                            components = DesignerUtils.CopyDragObjects(components, currentDropItem.Site) as ArrayList;
                            if (keyboardHandlingService != null)
                            {
                                keyboardHandlingService.CopyInProgress = false; 
                            }
                            if (primaryIndex != -1) 
                            { 
                                selectedItem = components[primaryIndex] as ToolStripItem;
                            } 
                        }


                        if (e.Effect == DragDropEffects.Move || copy) 
                        {
                            ISelectionService selSvc = GetSelectionService(currentDropItem); 
                            if (selSvc != null) 
                            {
                                // Insert the item. 
                                if (parentToolStrip is ToolStripOverflow)
                                {
                                    parentToolStrip = (((ToolStripOverflow)parentToolStrip).OwnerItem).Owner;
                                } 

                                int indexOfItemUnderMouseToDrop = parentToolStrip.Items.IndexOf(ToolStripDesigner.dragItem); 
 
                                if (indexOfItemUnderMouseToDrop != -1)
                                { 

                                    int indexOfPrimarySelection = 0;

                                    if (selectedItem != null) 
                                    {
                                        indexOfPrimarySelection = parentToolStrip.Items.IndexOf(selectedItem); 
                                    } 

                                    if (indexOfPrimarySelection != -1 && indexOfItemUnderMouseToDrop > indexOfPrimarySelection) 
                                    {
                                        indexOfItemUnderMouseToDrop--;
                                    }
                                    foreach (ToolStripItem item in components) 
                                    {
                                        parentToolStrip.Items.Insert(indexOfItemUnderMouseToDrop, item); 
                                    } 
                                }
                                selSvc.SetSelectedComponents(new IComponent[] { selectedItem }, SelectionTypes.Primary | SelectionTypes.Replace); 
                            }

                        }
                        if (changeSvc != null) 
                        {
                            ToolStripDropDown dropDown = parentToolStrip as ToolStripDropDown; 
 
                            if (dropDown != null)
                            { 
                                ToolStripItem ownerItem = dropDown.OwnerItem;
                                changeSvc.OnComponentChanged(ownerItem, TypeDescriptor.GetProperties(ownerItem)["DropDownItems"], null, null);
                            }
                            else 
                            {
                                changeSvc.OnComponentChanged(parentToolStrip, TypeDescriptor.GetProperties(parentToolStrip)["Items"], null, null); 
                            } 

                            //fire extra changing/changed events. 
                            if (copy)
                            {
                                if (dropDown != null)
                                { 
                                    ToolStripItem ownerItem = dropDown.OwnerItem;
                                    changeSvc.OnComponentChanging(ownerItem, TypeDescriptor.GetProperties(ownerItem)["DropDownItems"]); 
                                    changeSvc.OnComponentChanged(ownerItem, TypeDescriptor.GetProperties(ownerItem)["DropDownItems"], null, null); 
                                }
                                else 
                                {
                                    changeSvc.OnComponentChanging(parentToolStrip, TypeDescriptor.GetProperties(parentToolStrip)["Items"]);
                                    changeSvc.OnComponentChanged(parentToolStrip, TypeDescriptor.GetProperties(parentToolStrip)["Items"], null, null);
                                } 

                            } 
                        } 

 
                        //If Parent is DropDown... we have to manage the Glyphs ....
                        foreach (ToolStripItem item in components)
                        {
                            if (item is ToolStripDropDownItem) 
                            {
                                ToolStripMenuItemDesigner itemDesigner = designerHost.GetDesigner(item) as ToolStripMenuItemDesigner; 
                                if (itemDesigner != null) 
                                {
                                    itemDesigner.InitializeDropDown(); 
                                }
                            }
                            ToolStripDropDown dropDown = item.GetCurrentParent() as ToolStripDropDown;
                            if (dropDown != null && !(dropDown is ToolStripOverflow)) 
                            {
                                ToolStripDropDownItem ownerItem = dropDown.OwnerItem as ToolStripDropDownItem; 
                                if (ownerItem != null) 
                                {
                                    ToolStripMenuItemDesigner ownerDesigner = designerHost.GetDesigner(ownerItem) as ToolStripMenuItemDesigner; 
                                    if (ownerDesigner != null)
                                    {
                                        ownerDesigner.InitializeBodyGlyphsForItems(false, ownerItem);
                                        ownerDesigner.InitializeBodyGlyphsForItems(true, ownerItem); 
                                    }
                                } 
                            } 
                        }
                        // Refresh on SelectionManager... 
                        BehaviorService bSvc = GetBehaviorService(currentDropItem);
                        if (bSvc != null)
                        {
                            bSvc.SyncSelection(); 
                        }
 
                    } 
                    catch (Exception ex)
                    { 
                        if (designerTransaction != null)
                        {
                            designerTransaction.Cancel();
                            designerTransaction = null; 
                        }
                        if (ClientUtils.IsCriticalException(ex)) 
                        { 
                            throw;
                        } 
                    }
                    finally
                    {
                        if (designerTransaction != null) 
                        {
                            designerTransaction.Commit(); 
                            designerTransaction = null; 
                        }
 
                    }

                }
            } 
        }
 
 
        /// <include file='doc\ToolStripItemBehavior.uex' path='docs/doc[@for="ToolStripItemBehavior.OnDragEnter"]/*' />
        /// <devdoc> 
        ///     OnDragEnter can be overridden so that a Behavior can specify its own
        ///     Drag/Drop rules.
        /// </devdoc>
        public override void OnDragEnter(Glyph g, DragEventArgs e) 
        {
            ToolStripItemGlyph glyph = g as ToolStripItemGlyph; 
            ToolStripItem glyphItem = glyph.Item; 
            ToolStripItemDataObject data = e.Data as ToolStripItemDataObject;
 
            if (data != null)
            {
                // support move only within same container.
                if (data.Owner == glyphItem.Owner) 
                {
                    PaintInsertionMark(glyphItem); 
                    ToolStripDesigner.dragItem = glyphItem; 
                    e.Effect = DragDropEffects.Move;
                } 
                else
                {
                    e.Effect = DragDropEffects.None;
                } 
            }
            else 
            { 
                e.Effect = DragDropEffects.None;
            } 
        }

        /// <include file='doc\ToolStripItemBehavior.uex' path='docs/doc[@for="ToolStripItemBehavior.OnDragLeave"]/*' />
        /// <devdoc> 
        ///     OnDragLeave can be overridden so that a Behavior can specify its own
        ///     Drag/Drop rules. 
        /// </devdoc> 
        public override void OnDragLeave(Glyph g, EventArgs e)
        { 
            ToolStripItemGlyph glyph = g as ToolStripItemGlyph;
            ClearInsertionMark(glyph.Item);
        }
 
        /// <include file='doc\ToolStripItemBehavior.uex' path='docs/doc[@for="ToolStripItemBehavior.OnDragOver"]/*' />
        /// <devdoc> 
        ///     OnDragOver can be overridden so that a Behavior can specify its own 
        ///     Drag/Drop rules.
        /// </devdoc> 
        public override void OnDragOver(Glyph g, DragEventArgs e)
        {
            // Determine whether string data exists in the drop data. If not, then
            // the drop effect reflects that the drop cannot occur. 
            ToolStripItemGlyph glyph = g as ToolStripItemGlyph;
            ToolStripItem glyphItem = glyph.Item; 
            if (e.Data is ToolStripItemDataObject) 
            {
                PaintInsertionMark(glyphItem); 
                e.Effect = (Control.ModifierKeys == Keys.Control) ? DragDropEffects.Copy : DragDropEffects.Move;
            }
            else
            { 
                e.Effect = DragDropEffects.None;
            } 
        } 

        /// <devdoc> 
        /// Paints the insertion mark when items are being reordered
        /// </devdoc>
        private void PaintInsertionMark(ToolStripItem item)
        { 
            // Dont paint if cursor hasnt moved.
            if (ToolStripDesigner.LastCursorPosition != Point.Empty && ToolStripDesigner.LastCursorPosition == Cursor.Position) 
            { 
                return;
            } 
            // Dont paint any "MouseOver" glyohs if TemplateNode is ACTIVE !
            ToolStripKeyboardHandlingService keyService = GetKeyBoardHandlingService(item);
            if (keyService != null && keyService.TemplateNodeActive)
            { 
                return;
            } 
 
            //Start from fresh State...
            if (item != null && item.Site != null) 
            {
                ToolStripDesigner.LastCursorPosition = Cursor.Position;
                IDesignerHost designerHost = (IDesignerHost)item.Site.GetService(typeof(IDesignerHost));
                if (designerHost != null) 
                {
 
                    Rectangle bounds = GetPaintingBounds(designerHost, item); 
                    BehaviorService bSvc = GetBehaviorService(item);
                    if (bSvc != null) 
                    {
                        Graphics g = bSvc.AdornerWindowGraphics;
                        try
                        { 
                            using (Pen p = new Pen(new SolidBrush(Color.Black)))
                            { 
                                p.DashStyle = DashStyle.Dot; 
                                g.DrawRectangle(p, bounds);
                            } 
                        }
                        finally
                        {
                            g.Dispose(); 
                        }
                    } 
 
                }
            } 
        }


 
        /// <include file='doc\ToolStripItemBehavior.uex' path='docs/doc[@for="ToolStripItemBehavior.QueryContinueDrag"]/*' />
        /// <devdoc> 
        ///     QueryContinueDrag can be overridden so that a Behavior can specify its own 
        ///     Drag/Drop rules.
        /// </devdoc> 
        private void QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            // Cancel the drag if the mouse moves off the form.
            if (e.Action == DragAction.Continue) 
            {
                return; 
            } 
            if (e.EscapePressed)
            { 
                e.Action = DragAction.Cancel;
                ToolStripItem item = sender as ToolStripItem;
                SetParentDesignerValuesForDragDrop(item, false, Point.Empty);
                ISelectionService selSvc = GetSelectionService(item); 
                if (selSvc != null)
                { 
                    selSvc.SetSelectedComponents(new IComponent[] { item }, SelectionTypes.Auto); ; 
                }
                ToolStripDesigner.dragItem = null; 
            }
        }

        // Set values before initiating the Drag-Drop 
        private void SetParentDesignerValuesForDragDrop(ToolStripItem glyphItem, bool setValues, Point mouseLoc)
        { 
            if (glyphItem.Site == null) 
            {
                return; 
            }
            // Remember the point where the mouse down occurred. The DragSize indicates
            // the size that the mouse can move before a drag event should be started.
            Size dragSize = new Size(1, 1); 

            IDesignerHost designerHost = (IDesignerHost)glyphItem.Site.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null, "Invalid DesignerHost"); 

 
            // implement Drag Drop for individual ToolStrip Items
            // While this item is getting selected..
            // Get the index of the item the mouse is below.
            if (glyphItem.Placement == ToolStripItemPlacement.Overflow || (glyphItem.Placement == ToolStripItemPlacement.Main && !(glyphItem.IsOnDropDown))) 
            {
                ToolStripItemDesigner itemDesigner = designerHost.GetDesigner(glyphItem) as ToolStripItemDesigner; 
                ToolStrip parentToolStrip = itemDesigner.GetMainToolStrip(); 
                ToolStripDesigner parentDesigner = designerHost.GetDesigner(parentToolStrip) as ToolStripDesigner;
                if (parentDesigner != null) 
                {
                    if (setValues)
                    {
                        parentDesigner.IndexOfItemUnderMouseToDrag = parentToolStrip.Items.IndexOf(glyphItem); 
                        // Create a rectangle using the DragSize, with the mouse position being
                        // at the center of the rectangle. 
                        // On SelectionChanged we recreate the Glyphs ... 
                        // so need to stash this value on the parentDesigner....
                        parentDesigner.DragBoxFromMouseDown = dragBoxFromMouseDown = new Rectangle(new Point(mouseLoc.X - (dragSize.Width / 2), mouseLoc.Y - (dragSize.Height / 2)), dragSize); 
                    }
                    else
                    {
                        parentDesigner.IndexOfItemUnderMouseToDrag = -1; 
                        parentDesigner.DragBoxFromMouseDown = dragBoxFromMouseDown = Rectangle.Empty;
                    } 
                } 

            } 
            else if (glyphItem.IsOnDropDown)
            {
                //Get the OwnerItem's Designer and set the value...
                ToolStripDropDown parentDropDown = glyphItem.Owner as ToolStripDropDown; 
                if (parentDropDown != null)
                { 
                    ToolStripItem ownerItem = parentDropDown.OwnerItem; 
                    ToolStripItemDesigner ownerItemDesigner = designerHost.GetDesigner(ownerItem) as ToolStripItemDesigner;
                    if (ownerItemDesigner != null) 
                    {

                        if (setValues)
                        { 
                            ownerItemDesigner.indexOfItemUnderMouseToDrag = parentDropDown.Items.IndexOf(glyphItem);
                            // Create a rectangle using the DragSize, with the mouse position being 
                            // at the center of the rectangle. 
                            // On SelectionChanged we recreate the Glyphs ...
                            // so need to stash this value on the parentDesigner.... 
                            ownerItemDesigner.dragBoxFromMouseDown = dragBoxFromMouseDown = new Rectangle(new Point(mouseLoc.X - (dragSize.Width / 2), mouseLoc.Y - (dragSize.Height / 2)), dragSize);
                        }
                        else
                        { 
                            ownerItemDesigner.indexOfItemUnderMouseToDrag = -1;
                            ownerItemDesigner.dragBoxFromMouseDown = dragBoxFromMouseDown = Rectangle.Empty; 
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
// <copyright file="ToolStripItemBehavior.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
*/ 
namespace System.Windows.Forms.Design
{ 
    using System.Design;
    using Accessibility;
    using System.Runtime.Serialization.Formatters;
    using System.Threading; 
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Diagnostics.CodeAnalysis; 
    using System.Diagnostics;
    using System; 
    using System.Security;
    using System.Security.Permissions;
    using System.Collections;
    using System.ComponentModel.Design; 
    using System.ComponentModel.Design.Serialization;
    using System.Windows.Forms; 
    using System.Drawing; 
    using System.Drawing.Drawing2D;
    using System.Drawing.Design; 
    using Microsoft.Win32;
    using System.Windows.Forms.Design.Behavior;
    using System.Reflection;
 
    /// <summary>
    ///  The behavior for the glpyh that covers the items themselves.  This selects the items 
    /// when they are clicked, and will (when implemented) do the dragging/reordering of them. 
    /// </summary>
    internal class ToolStripItemBehavior : System.Windows.Forms.Design.Behavior.Behavior 
    {

        private const int GLYPHBORDER = 1;
        private const int GLYPHINSET = 2; 

        //new privates for "Drag Drop" 
        internal Rectangle dragBoxFromMouseDown = Rectangle.Empty; 
        private System.Windows.Forms.Timer _timer = null;
        private ToolStripItemGlyph selectedGlyph; 
        private bool doubleClickFired = false;
        private bool mouseUpFired = false;
        private Control dropSource;
        private IEventHandlerService eventSvc = null; 

        public ToolStripItemBehavior() 
        { 

        } 

        // Gets the DropSource control.
        private Control DropSource
        { 
            get
            { 
                if (dropSource == null) 
                {
                    dropSource = new Control(); 
                }
                return dropSource;
            }
        } 

 
        // Returns true if oldSelection and newSelection have a commonParent. 
        private bool CommonParent(ToolStripItem oldSelection, ToolStripItem newSelection)
        { 
            ToolStrip oldSelectionParent = oldSelection.GetCurrentParent();
            ToolStrip newSelectionParent = newSelection.GetCurrentParent();
            return (oldSelectionParent == newSelectionParent);
        } 

        /// <devdoc> 
        /// Clears the insertion mark when items are being reordered 
        /// </devdoc>
        private void ClearInsertionMark(ToolStripItem item) 
        {
            // Dont paint if cursor hasnt moved.
            if (ToolStripDesigner.LastCursorPosition != Point.Empty && ToolStripDesigner.LastCursorPosition == Cursor.Position)
            { 
                return;
            } 
 
            // Dont paint any "MouseOver" glyohs if TemplateNode is ACTIVE !
            ToolStripKeyboardHandlingService keyService = GetKeyBoardHandlingService(item); 
            if (keyService != null && keyService.TemplateNodeActive)
            {
                return;
            } 

            // stuff away the lastInsertionMarkRect 
            // and clear it out _before_ we call paint OW 
            // the call to invalidate wont help as it will get
            // repainted. 
            Rectangle bounds = Rectangle.Empty;
            if (item != null && item.Site != null)
            {
                IDesignerHost designerHost = (IDesignerHost)item.Site.GetService(typeof(IDesignerHost)); 
                if (designerHost != null)
                { 
                    bounds = GetPaintingBounds(designerHost, item); 
                    bounds.Inflate(GLYPHBORDER, GLYPHBORDER);
                    Region rgn = new Region(bounds); 
                    try
                    {
                        bounds.Inflate(-GLYPHINSET, -GLYPHINSET);
                        rgn.Exclude(bounds); 

                        BehaviorService bSvc = GetBehaviorService(item); 
                        if (bSvc != null && bounds != Rectangle.Empty) 
                        {
                            bSvc.Invalidate(rgn); 
                        }
                    }
                    finally
                    { 
                        rgn.Dispose();
                        rgn = null; 
                    } 
                }
            } 
        }

        // Tries to put the item in the Insitu edit mode after the double click timer has ticked
        private void EnterInSituMode(ToolStripItemGlyph glyph) 
        {
            if (glyph.ItemDesigner != null && !glyph.ItemDesigner.IsEditorActive) 
            { 

                glyph.ItemDesigner.ShowEditNode(false); 
            }
        }

        // Gets the Selection Service. 
        private ISelectionService GetSelectionService(ToolStripItem item)
        { 
            Debug.Assert(item != null, "Item passed is null, SelectionService cannot be obtained"); 
            if (item.Site != null)
            { 
                ISelectionService selSvc = (ISelectionService)item.Site.GetService(typeof(ISelectionService));
                Debug.Assert(selSvc != null, "Failed to get Selection Service!");
                return selSvc;
            } 
            return null;
 
        } 

        // Gets the Behavior Service. 
        private BehaviorService GetBehaviorService(ToolStripItem item)
        {
            Debug.Assert(item != null, "Item passed is null, BehaviorService cannot be obtained");
            if (item.Site != null) 
            {
                BehaviorService behaviorSvc = (BehaviorService)item.Site.GetService(typeof(BehaviorService)); 
                Debug.Assert(behaviorSvc != null, "Failed to get Behavior Service!"); 
                return behaviorSvc;
            } 
            return null;

        }
 
        // Gets the ToolStripKeyBoardHandling Service.
        private ToolStripKeyboardHandlingService GetKeyBoardHandlingService(ToolStripItem item) 
        { 
            Debug.Assert(item != null, "Item passed is null, ToolStripKeyBoardHandlingService cannot be obtained");
            if (item.Site != null) 
            {
                ToolStripKeyboardHandlingService keyBoardSvc = (ToolStripKeyboardHandlingService)item.Site.GetService(typeof(ToolStripKeyboardHandlingService));
                Debug.Assert(keyBoardSvc != null, "Failed to get ToolStripKeyboardHandlingService!");
                return keyBoardSvc; 
            }
            return null; 
 
        }
 
        // Gets the painting rect for SelectionRects
        private static Rectangle GetPaintingBounds(IDesignerHost designerHost, ToolStripItem item)
        {
            Rectangle bounds = Rectangle.Empty; 

            ToolStripItemDesigner itemDesigner = designerHost.GetDesigner(item) as ToolStripItemDesigner; 
            if (itemDesigner != null) 
            {
                bounds = itemDesigner.GetGlyphBounds(); 
                ToolStripDesignerUtils.GetAdjustedBounds(item, ref bounds);
                // So that the mouseOver glyph matches the selectionGlyph.
                bounds.Inflate(1,1);
                bounds.Width--; 
                bounds.Height--;
            } 
            return bounds; 
        }
 
        // This helper function will return true if any other MouseHandler (say TabOrder UI) is active, in which case we should not handle any Mouse Messages..
        // Since the TabOrder UI is pre-Whidbey when the TabOrder UI is up,
        // It adds a new Overlay (a window) to the DesignerFrame (something similar to AdornerWindow).
        // This UI is a transaparent control which has overrides foir Mouse Messages. It listens for all mouse messages through the IMouseHandler interface instead of using the new 
        // BehaviorService. Hence we have to special case this scenario. (CONTROL DESIGNER ALSO DOES THIS).
 
        private bool MouseHandlerPresent(ToolStripItem item) 
        {
            IMouseHandler mouseHandler = null; 

            if (eventSvc == null) {
                eventSvc = (IEventHandlerService)item.Site.GetService(typeof(IEventHandlerService));
            } 

            if (eventSvc != null) { 
                mouseHandler = (IMouseHandler)eventSvc.GetHandler(typeof(IMouseHandler)); 
            }
 
            return (mouseHandler != null);

        }
 
        // Occurs when the timer ticks after user has doubleclicked an item
        private void OnDoubleClickTimerTick(object sender, EventArgs e) 
        { 
            if (_timer != null)
            { 
                _timer.Enabled = false;
                _timer.Tick -= new System.EventHandler(OnDoubleClickTimerTick);
                _timer.Dispose();
                _timer = null; 
                // Enter Insitu ...
                if (selectedGlyph != null && selectedGlyph.Item is ToolStripMenuItem) 
                { 
                    EnterInSituMode(selectedGlyph);
                } 
            }

        }
 

 
 
        // Occurs when user doubleclicks on the TooLStripItem glyph
        public override bool OnMouseDoubleClick(Glyph g, MouseButtons button, Point mouseLoc) 
        {
            if (mouseUpFired)
            {
                doubleClickFired = true; 
            }
            return false; 
        } 

        // Occurs when MouseUp TooLStripItem glyph 
        public override bool OnMouseUp(Glyph g, MouseButtons button)
        {
            ToolStripItemGlyph glyph = g as ToolStripItemGlyph;
            ToolStripItem glyphItem = glyph.Item; 
            if (MouseHandlerPresent(glyphItem))
            { 
                return false; 
            }
            SetParentDesignerValuesForDragDrop(glyphItem, false, Point.Empty); 
            if (doubleClickFired)
            {
                if (glyph != null && button == MouseButtons.Left)
                { 
                    ISelectionService selSvc = GetSelectionService(glyphItem);
                    if (selSvc == null) 
                    { 
                        return false;
                    } 

                    ToolStripItem selectedItem = selSvc.PrimarySelection as ToolStripItem;
                    // Check if this item is already selected ...
                    if (selectedItem == glyphItem) 
                    {
                        // If timer != null.. we are in DoubleClick before the "InSitu Timer" so KILL IT. 
                        if (_timer != null) 
                        {
                            _timer.Enabled = false; 
                            _timer.Tick -= new System.EventHandler(OnDoubleClickTimerTick);
                            _timer.Dispose();
                            _timer = null;
                        } 
                        // If the Selecteditem is already in editmode ... bail out
                        if (selectedItem != null) 
                        { 
                            ToolStripItemDesigner selectedItemDesigner = glyph.ItemDesigner;
                            if (selectedItemDesigner != null && selectedItemDesigner.IsEditorActive) 
                            {
                                return false;
                            }
                            selectedItemDesigner.DoDefaultAction(); 
                        }
                        doubleClickFired = false; 
                        mouseUpFired = false; 
                    }
                } 
            }
            else
            {
                mouseUpFired = true; 
            }
            return false; 
        } 

        // Occurs when MouseDown on the TooLStripItem glyph 
        public override bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc)
        {
            ToolStripItemGlyph glyph = g as ToolStripItemGlyph;
            ToolStripItem glyphItem = glyph.Item; 

            ISelectionService selSvc = GetSelectionService(glyphItem); 
            BehaviorService bSvc = GetBehaviorService(glyphItem); 
            ToolStripKeyboardHandlingService keyService = GetKeyBoardHandlingService(glyphItem);
 
            if ((button == MouseButtons.Left) && (keyService != null) && (keyService.TemplateNodeActive))
            {
                if (keyService.ActiveTemplateNode.IsSystemContextMenuDisplayed)
                { 
                    // DevDiv Bugs: 144618 : skip behaviors when the context menu is displayed
                    return false; 
                } 
            }
 
            IDesignerHost designerHost = (IDesignerHost)glyphItem.Site.GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null, "Invalid DesignerHost");

            ToolStripItem currentSel = selSvc.PrimarySelection as ToolStripItem; 

            //Cache original selection 
            ICollection originalSelComps = null; 
            if (selSvc != null)
            { 
                originalSelComps = selSvc.GetSelectedComponents();
            }

            // Add the TemplateNode to the Selection if it is currently Selected as the GetSelectedComponents wont do it for us. 
            ArrayList origSel = new ArrayList(originalSelComps);
            if (origSel.Count == 0) 
            { 
                if (keyService != null && keyService.SelectedDesignerControl != null)
                { 
                    origSel.Add(keyService.SelectedDesignerControl);
                }
            }
 

            if (keyService != null) 
            { 
                keyService.SelectedDesignerControl = null;
                if (keyService.TemplateNodeActive) 
                {
                    // If templateNode Active .. commit and Select it
                    keyService.ActiveTemplateNode.CommitAndSelect();
                    // if the selected item is clicked .. then commit the node and reset the selection (refer 488002) 
                    if (currentSel != null && currentSel == glyphItem)
                    { 
                        selSvc.SetSelectedComponents(null, SelectionTypes.Replace); 
                    }
                } 
            }

            if (selSvc == null || MouseHandlerPresent(glyphItem))
            { 
                return false;
            } 
 
            if (glyph != null && button == MouseButtons.Left)
            { 
                ToolStripItem selectedItem = selSvc.PrimarySelection as ToolStripItem;

                // Always set the Drag-Rect for Drag-Drop...
                SetParentDesignerValuesForDragDrop(glyphItem, true, mouseLoc); 

                // Check if this item is already selected ... 
                if (selectedItem != null && selectedItem == glyphItem) 
                {
                    // If the Selecteditem is already in editmode ... bail out 
                    if (selectedItem != null)
                    {
                        ToolStripItemDesigner selectedItemDesigner = glyph.ItemDesigner;
                        if (selectedItemDesigner != null && selectedItemDesigner.IsEditorActive) 
                        {
                            return false; 
                        } 
                    }
 
                    // Check if this is CTRL + Click or SHIFT + Click, if so then just remove the selection
                    bool removeSel = (Control.ModifierKeys & (Keys.Control | Keys.Shift)) > 0;
                    if (removeSel)
                    { 
                        selSvc.SetSelectedComponents(new IComponent[] { selectedItem }, SelectionTypes.Remove);
                        return false; 
                    } 

                    //start Double Click Timer 
                    // This is required for the second down in selection
                    // which can be the first down of a Double click on the glyph
                    // confusing... hence this comment ...
                    // Heres the scenario .... 
                    // DOWN 1 - selects the ITEM
                    // DOWN 2 - ITEM goes into INSITU.... 
                    // DOUBLE CLICK - dont show code.. 
                    // Open INSITU after the double click time
                    if (selectedItem is ToolStripMenuItem) 
                    {
                        _timer = new System.Windows.Forms.Timer();
                        _timer.Interval = SystemInformation.DoubleClickTime;
                        _timer.Tick += new System.EventHandler(OnDoubleClickTimerTick); 
                        _timer.Enabled = true;
                        selectedGlyph = glyph; 
                    } 
                }
                else 
                {

                    bool shiftPressed = (Control.ModifierKeys & Keys.Shift) > 0;
 
                    // We should process MouseDown only if we are not yet selected....
                    if (!selSvc.GetComponentSelected(glyphItem)) 
                    { 
                        //Reset the State...
                        // On the Glpyhs .. we get MouseDown - Mouse UP (for single Click) 
                        // And we get MouseDown - MouseUp - DoubleClick - Up (for double Click)
                        // Hence reset the state at start....
                        mouseUpFired = false;
                        doubleClickFired = false; 

                        //Implementing Shift + Click.... 
                        // we have 2 items, namely, selectedItem (current PrimarySelection) and glyphItem (item which has received mouseDown) 
                        // FIRST check if they have common parent...
                        // IF YES then get the indices of the two and SELECT all items from LOWER index to the HIGHER index. 
                        if (shiftPressed && (selectedItem != null && CommonParent(selectedItem, glyphItem)))
                        {
                            ToolStrip parent = null;
                            if (glyphItem.IsOnOverflow) 
                            {
                                parent = glyphItem.Owner; 
                            } 
                            else
                            { 
                                parent = glyphItem.GetCurrentParent();
                            }
                            int startIndexOfSelection = Math.Min(parent.Items.IndexOf(selectedItem), parent.Items.IndexOf(glyphItem));
                            int endIndexOfSelection = Math.Max(parent.Items.IndexOf(selectedItem), parent.Items.IndexOf(glyphItem)); 
                            int countofItemsSelected = (endIndexOfSelection - startIndexOfSelection) + 1;
 
                            // if two adjacent items are selected ... 
                            if (countofItemsSelected == 2)
                            { 
                                selSvc.SetSelectedComponents(new IComponent[] { glyphItem });
                            }
                            else
                            { 
                                object[] totalObjects = new object[countofItemsSelected];
                                int j = 0; 
                                for (int i = startIndexOfSelection; i <= endIndexOfSelection; i++) 
                                {
                                    totalObjects[j++] = parent.Items[i]; 
                                }
                                selSvc.SetSelectedComponents(new IComponent[] { parent } , SelectionTypes.Replace);
                                ToolStripDesigner.shiftState = true;
                                selSvc.SetSelectedComponents(totalObjects, SelectionTypes.Replace); 
                            }
                        } 
                        //End Implmentation 
                        else
                        { 
                            if (glyphItem.IsOnDropDown && ToolStripDesigner.shiftState)
                            {
                                //Invalidate glyh only if we are in ShiftState...
                                ToolStripDesigner.shiftState = false; 
                                if (bSvc != null)
                                { 
                                    bSvc.Invalidate(glyphItem.Owner.Bounds); 
                                }
                            } 
                            selSvc.SetSelectedComponents(new IComponent[] { glyphItem }, SelectionTypes.Auto);
                        }
                        // Set the appropriate object.
                        if (keyService != null) 
                        {
                            keyService.ShiftPrimaryItem = glyphItem; 
                        } 
                    }
                    // we are already selected and if shiftpressed... 
                    else if (shiftPressed || (Control.ModifierKeys & Keys.Control) > 0)
                    {
                        selSvc.SetSelectedComponents(new IComponent[] { glyphItem }, SelectionTypes.Remove);
                    } 
                }
            } 
 
            if (glyph != null && button == MouseButtons.Right)
            { 
                if (!selSvc.GetComponentSelected(glyphItem))
                {
                    selSvc.SetSelectedComponents(new IComponent[] { glyphItem });
                } 
            }
 
            // finally Invalidate all selections 
            ToolStripDesignerUtils.InvalidateSelection(origSel, glyphItem, glyphItem.Site, false);
 
            return false;
        }

        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseEnter"]/*' /> 
        /// <devdoc>
        ///    Overriden to paint the border on mouse enter..... 
        /// </devdoc> 
        public override bool OnMouseEnter(Glyph g)
        { 
            ToolStripItemGlyph glyph = g as ToolStripItemGlyph;
            if (glyph != null)
            {
                ToolStripItem glyphItem = glyph.Item; 

                if (MouseHandlerPresent(glyphItem)) 
                { 
                    return false;
                } 

                ISelectionService selSvc = GetSelectionService(glyphItem);
                if (selSvc != null)
                { 
                    if (!selSvc.GetComponentSelected(glyphItem))
                    { 
                        PaintInsertionMark(glyphItem); 
                    }
                } 
            }
            return false;
        }
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseLeave"]/*' />
        /// <devdoc> 
        ///     overriden to "clear" the boundary-paint when the mouse leave the item 
        /// </devdoc>
        public override bool OnMouseLeave(Glyph g) 
        {
            ToolStripItemGlyph glyph = g as ToolStripItemGlyph;
            if (glyph != null)
            { 
                ToolStripItem glyphItem = glyph.Item;
 
                if (MouseHandlerPresent(glyphItem)) 
                {
                    return false; 
                }

                ISelectionService selSvc = GetSelectionService(glyphItem);
                if (selSvc != null) 
                {
                    if (!selSvc.GetComponentSelected(glyphItem)) 
                    { 
                        ClearInsertionMark(glyphItem);
                    } 
                }
            }
            return false;
        } 

        // Implementation of Drag Drop .... 
        /// <include file='doc\ToolStripItemBehavior.uex' path='docs/doc[@for="ToolStripItemBehavior.OnMouseMove"]/*' /> 
        /// <devdoc>
        ///     When any MouseMove message enters the BehaviorService's AdornerWindow 
        ///     (mousemove, ncmousemove) it is first passed here, to the top-most Behavior
        ///     in the BehaviorStack.  Returning 'true' from this function signifies that
        ///     the Message was 'handled' by the Behavior and should not continue to be processed.
        /// </devdoc> 
        public override bool OnMouseMove(Glyph g, MouseButtons button, Point mouseLoc)
        { 
 
            bool retVal = false;
 
            ToolStripItemGlyph glyph = g as ToolStripItemGlyph;
            ToolStripItem glyphItem = glyph.Item;

            ISelectionService selSvc = GetSelectionService(glyphItem); 

            if (selSvc == null || glyphItem.Site == null || MouseHandlerPresent(glyphItem)) 
            { 
                return false;
            } 



            //ToolStirpItems now support selection on mousemove .... 
            if (!selSvc.GetComponentSelected(glyphItem))
            { 
                PaintInsertionMark(glyphItem); 
                retVal = false;
            } 


            if (button == MouseButtons.Left && glyph != null && glyph.ItemDesigner != null && !glyph.ItemDesigner.IsEditorActive)
            { 
                Rectangle dragBox = Rectangle.Empty;
 
                IDesignerHost designerHost = (IDesignerHost)glyphItem.Site.GetService(typeof(IDesignerHost)); 
                Debug.Assert(designerHost != null, "Invalid DesignerHost");
 
                if (glyphItem.Placement == ToolStripItemPlacement.Overflow || (glyphItem.Placement == ToolStripItemPlacement.Main && !(glyphItem.IsOnDropDown)))
                {
                    ToolStripItemDesigner itemDesigner = glyph.ItemDesigner;
                    ToolStrip parentToolStrip = itemDesigner.GetMainToolStrip(); 
                    ToolStripDesigner parentDesigner = designerHost.GetDesigner(parentToolStrip) as ToolStripDesigner;
                    if (parentDesigner != null) 
                    { 
                        dragBox = parentDesigner.DragBoxFromMouseDown;
                    } 
                }
                else if (glyphItem.IsOnDropDown)
                {
                    //Get the OwnerItem's Designer and set the value... 
                    ToolStripDropDown parentDropDown = glyphItem.Owner as ToolStripDropDown;
                    if (parentDropDown != null) 
                    { 
                        ToolStripItem ownerItem = parentDropDown.OwnerItem;
                        ToolStripItemDesigner ownerItemDesigner = designerHost.GetDesigner(ownerItem) as ToolStripItemDesigner; 
                        if (ownerItemDesigner != null)
                        {
                            dragBox = ownerItemDesigner.dragBoxFromMouseDown;
                        } 
                    }
                } 
 

                // If the mouse moves outside the rectangle, start the drag. 
                if (dragBox != Rectangle.Empty && !dragBox.Contains(mouseLoc.X, mouseLoc.Y))
                {

                    if (_timer != null) 
                    {
                        _timer.Enabled = false; 
                        _timer.Tick -= new System.EventHandler(OnDoubleClickTimerTick); 
                        _timer.Dispose();
                        _timer = null; 
                    }

                    // Proceed with the drag and drop, passing in the list item.
                    try 
                    {
                        // ---------------- create list of components -------------------------------------------------// 
                        ArrayList dragItems = new ArrayList(); 
                        ICollection selComps = selSvc.GetSelectedComponents();
 
                        //create our list of controls-to-drag
                        foreach (IComponent comp in selComps)
                        {
                            ToolStripItem item = comp as ToolStripItem; 
                            if (item != null)
                            { 
                                dragItems.Add(item); 
                            }
                        } 

                        ToolStripItem selectedItem = selSvc.PrimarySelection as ToolStripItem;
                        //Start Drag-Drop only if ToolStripItem is the primary Selection
                        if (selectedItem != null) 
                        {
                            ToolStrip owner = selectedItem.Owner; 
 
                            ToolStripItemDataObject data = new ToolStripItemDataObject(dragItems, selectedItem, owner);
                            // ---------------- create list of components -------------------------------------------------// 


                            DropSource.QueryContinueDrag += new QueryContinueDragEventHandler(QueryContinueDrag);
                            ToolStripDropDownItem ddItem = glyphItem as ToolStripDropDownItem; 
                            if (ddItem != null)
                            { 
                                ToolStripMenuItemDesigner itemDesigner = designerHost.GetDesigner(ddItem) as ToolStripMenuItemDesigner; 
                                if (itemDesigner != null)
                                { 
                                    itemDesigner.InitializeBodyGlyphsForItems(false, ddItem);
                                    ddItem.HideDropDown();
                                }
                            } 
                            else if (glyphItem.IsOnDropDown && !glyphItem.IsOnOverflow)
                            { 
                                ToolStripDropDown dropDown = glyphItem.GetCurrentParent() as ToolStripDropDown; 
                                ToolStripDropDownItem ownerItem = dropDown.OwnerItem as ToolStripDropDownItem;
                                selSvc.SetSelectedComponents(new IComponent[] { ownerItem }, SelectionTypes.Replace); 
                            }
                            DragDropEffects dropEffect = DropSource.DoDragDrop(data, DragDropEffects.All);
                        }
                    } 
                    finally
                    { 
                        DropSource.QueryContinueDrag -= new QueryContinueDragEventHandler(QueryContinueDrag); 
                        //Reset all Drag-Variables
                        SetParentDesignerValuesForDragDrop(glyphItem, false, Point.Empty); 
                        ToolStripDesigner.dragItem = null;
                        dropSource = null;
                    }
 
                    retVal = false;
                } 
            } 
            return retVal;
        } 


        //  OLE DragDrop virtual methods
        // 
        /// <include file='doc\ToolStripItemBehavior.uex' path='docs/doc[@for="ToolStripItemBehavior.OnDragDrop"]/*' />
        /// <devdoc> 
        ///     OnDragDrop can be overridden so that a Behavior can specify its own 
        ///     Drag/Drop rules.
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        public override void OnDragDrop(Glyph g, DragEventArgs e)
        {
            ToolStripItem currentDropItem = ToolStripDesigner.dragItem; 

            // Ensure that the list item index is contained in the data. 
            if (e.Data is ToolStripItemDataObject && currentDropItem != null) 
            {
                ToolStripItemDataObject data = (ToolStripItemDataObject)e.Data; 
                // Get the PrimarySelection before the Drag operation...
                ToolStripItem selectedItem = data.PrimarySelection;

                IDesignerHost designerHost = (IDesignerHost)currentDropItem.Site.GetService(typeof(IDesignerHost)); 
                Debug.Assert(designerHost != null, "Invalid DesignerHost");
 
                //Do DragDrop only if currentDropItem has changed. 
                if (currentDropItem != selectedItem && designerHost != null)
                { 

                    ArrayList components = data.DragComponents;
                    ToolStrip parentToolStrip = currentDropItem.GetCurrentParent() as ToolStrip;
                    int primaryIndex = -1; 
                    string transDesc;
                    bool copy = (e.Effect == DragDropEffects.Copy); 
 
                    if (components.Count == 1)
                    { 
                        string name = TypeDescriptor.GetComponentName(components[0]);
                        if (name == null || name.Length == 0)
                        {
                            name = components[0].GetType().Name; 
                        }
                        transDesc = SR.GetString(copy ? SR.BehaviorServiceCopyControl : SR.BehaviorServiceMoveControl, name); 
                    } 
                    else
                    { 
                        transDesc = SR.GetString(copy ? SR.BehaviorServiceCopyControls : SR.BehaviorServiceMoveControls, components.Count);
                    }

 
                    DesignerTransaction designerTransaction = designerHost.CreateTransaction(transDesc);
                    try 
                    { 
                        IComponentChangeService changeSvc = (IComponentChangeService)currentDropItem.Site.GetService(typeof(IComponentChangeService));
                        if (changeSvc != null) 
                        {
                            ToolStripDropDown dropDown = parentToolStrip as ToolStripDropDown;
                            if (dropDown != null)
                            { 
                                ToolStripItem ownerItem = dropDown.OwnerItem;
                                changeSvc.OnComponentChanging(ownerItem, TypeDescriptor.GetProperties(ownerItem)["DropDownItems"]); 
                            } 
                            else
                            { 
                                changeSvc.OnComponentChanging(parentToolStrip, TypeDescriptor.GetProperties(parentToolStrip)["Items"]);
                            }
                        }
 
                        // If we are copying, then we want to make a copy of the components we are dragging
                        if (copy) 
                        { 

                            // Remember the primary selection if we had one 
                            if (selectedItem != null)
                            {
                                primaryIndex = components.IndexOf(selectedItem);
                            } 
                            ToolStripKeyboardHandlingService keyboardHandlingService = GetKeyBoardHandlingService(selectedItem);
                            if (keyboardHandlingService != null) 
                            { 
                                keyboardHandlingService.CopyInProgress = true;
                            } 
                            components = DesignerUtils.CopyDragObjects(components, currentDropItem.Site) as ArrayList;
                            if (keyboardHandlingService != null)
                            {
                                keyboardHandlingService.CopyInProgress = false; 
                            }
                            if (primaryIndex != -1) 
                            { 
                                selectedItem = components[primaryIndex] as ToolStripItem;
                            } 
                        }


                        if (e.Effect == DragDropEffects.Move || copy) 
                        {
                            ISelectionService selSvc = GetSelectionService(currentDropItem); 
                            if (selSvc != null) 
                            {
                                // Insert the item. 
                                if (parentToolStrip is ToolStripOverflow)
                                {
                                    parentToolStrip = (((ToolStripOverflow)parentToolStrip).OwnerItem).Owner;
                                } 

                                int indexOfItemUnderMouseToDrop = parentToolStrip.Items.IndexOf(ToolStripDesigner.dragItem); 
 
                                if (indexOfItemUnderMouseToDrop != -1)
                                { 

                                    int indexOfPrimarySelection = 0;

                                    if (selectedItem != null) 
                                    {
                                        indexOfPrimarySelection = parentToolStrip.Items.IndexOf(selectedItem); 
                                    } 

                                    if (indexOfPrimarySelection != -1 && indexOfItemUnderMouseToDrop > indexOfPrimarySelection) 
                                    {
                                        indexOfItemUnderMouseToDrop--;
                                    }
                                    foreach (ToolStripItem item in components) 
                                    {
                                        parentToolStrip.Items.Insert(indexOfItemUnderMouseToDrop, item); 
                                    } 
                                }
                                selSvc.SetSelectedComponents(new IComponent[] { selectedItem }, SelectionTypes.Primary | SelectionTypes.Replace); 
                            }

                        }
                        if (changeSvc != null) 
                        {
                            ToolStripDropDown dropDown = parentToolStrip as ToolStripDropDown; 
 
                            if (dropDown != null)
                            { 
                                ToolStripItem ownerItem = dropDown.OwnerItem;
                                changeSvc.OnComponentChanged(ownerItem, TypeDescriptor.GetProperties(ownerItem)["DropDownItems"], null, null);
                            }
                            else 
                            {
                                changeSvc.OnComponentChanged(parentToolStrip, TypeDescriptor.GetProperties(parentToolStrip)["Items"], null, null); 
                            } 

                            //fire extra changing/changed events. 
                            if (copy)
                            {
                                if (dropDown != null)
                                { 
                                    ToolStripItem ownerItem = dropDown.OwnerItem;
                                    changeSvc.OnComponentChanging(ownerItem, TypeDescriptor.GetProperties(ownerItem)["DropDownItems"]); 
                                    changeSvc.OnComponentChanged(ownerItem, TypeDescriptor.GetProperties(ownerItem)["DropDownItems"], null, null); 
                                }
                                else 
                                {
                                    changeSvc.OnComponentChanging(parentToolStrip, TypeDescriptor.GetProperties(parentToolStrip)["Items"]);
                                    changeSvc.OnComponentChanged(parentToolStrip, TypeDescriptor.GetProperties(parentToolStrip)["Items"], null, null);
                                } 

                            } 
                        } 

 
                        //If Parent is DropDown... we have to manage the Glyphs ....
                        foreach (ToolStripItem item in components)
                        {
                            if (item is ToolStripDropDownItem) 
                            {
                                ToolStripMenuItemDesigner itemDesigner = designerHost.GetDesigner(item) as ToolStripMenuItemDesigner; 
                                if (itemDesigner != null) 
                                {
                                    itemDesigner.InitializeDropDown(); 
                                }
                            }
                            ToolStripDropDown dropDown = item.GetCurrentParent() as ToolStripDropDown;
                            if (dropDown != null && !(dropDown is ToolStripOverflow)) 
                            {
                                ToolStripDropDownItem ownerItem = dropDown.OwnerItem as ToolStripDropDownItem; 
                                if (ownerItem != null) 
                                {
                                    ToolStripMenuItemDesigner ownerDesigner = designerHost.GetDesigner(ownerItem) as ToolStripMenuItemDesigner; 
                                    if (ownerDesigner != null)
                                    {
                                        ownerDesigner.InitializeBodyGlyphsForItems(false, ownerItem);
                                        ownerDesigner.InitializeBodyGlyphsForItems(true, ownerItem); 
                                    }
                                } 
                            } 
                        }
                        // Refresh on SelectionManager... 
                        BehaviorService bSvc = GetBehaviorService(currentDropItem);
                        if (bSvc != null)
                        {
                            bSvc.SyncSelection(); 
                        }
 
                    } 
                    catch (Exception ex)
                    { 
                        if (designerTransaction != null)
                        {
                            designerTransaction.Cancel();
                            designerTransaction = null; 
                        }
                        if (ClientUtils.IsCriticalException(ex)) 
                        { 
                            throw;
                        } 
                    }
                    finally
                    {
                        if (designerTransaction != null) 
                        {
                            designerTransaction.Commit(); 
                            designerTransaction = null; 
                        }
 
                    }

                }
            } 
        }
 
 
        /// <include file='doc\ToolStripItemBehavior.uex' path='docs/doc[@for="ToolStripItemBehavior.OnDragEnter"]/*' />
        /// <devdoc> 
        ///     OnDragEnter can be overridden so that a Behavior can specify its own
        ///     Drag/Drop rules.
        /// </devdoc>
        public override void OnDragEnter(Glyph g, DragEventArgs e) 
        {
            ToolStripItemGlyph glyph = g as ToolStripItemGlyph; 
            ToolStripItem glyphItem = glyph.Item; 
            ToolStripItemDataObject data = e.Data as ToolStripItemDataObject;
 
            if (data != null)
            {
                // support move only within same container.
                if (data.Owner == glyphItem.Owner) 
                {
                    PaintInsertionMark(glyphItem); 
                    ToolStripDesigner.dragItem = glyphItem; 
                    e.Effect = DragDropEffects.Move;
                } 
                else
                {
                    e.Effect = DragDropEffects.None;
                } 
            }
            else 
            { 
                e.Effect = DragDropEffects.None;
            } 
        }

        /// <include file='doc\ToolStripItemBehavior.uex' path='docs/doc[@for="ToolStripItemBehavior.OnDragLeave"]/*' />
        /// <devdoc> 
        ///     OnDragLeave can be overridden so that a Behavior can specify its own
        ///     Drag/Drop rules. 
        /// </devdoc> 
        public override void OnDragLeave(Glyph g, EventArgs e)
        { 
            ToolStripItemGlyph glyph = g as ToolStripItemGlyph;
            ClearInsertionMark(glyph.Item);
        }
 
        /// <include file='doc\ToolStripItemBehavior.uex' path='docs/doc[@for="ToolStripItemBehavior.OnDragOver"]/*' />
        /// <devdoc> 
        ///     OnDragOver can be overridden so that a Behavior can specify its own 
        ///     Drag/Drop rules.
        /// </devdoc> 
        public override void OnDragOver(Glyph g, DragEventArgs e)
        {
            // Determine whether string data exists in the drop data. If not, then
            // the drop effect reflects that the drop cannot occur. 
            ToolStripItemGlyph glyph = g as ToolStripItemGlyph;
            ToolStripItem glyphItem = glyph.Item; 
            if (e.Data is ToolStripItemDataObject) 
            {
                PaintInsertionMark(glyphItem); 
                e.Effect = (Control.ModifierKeys == Keys.Control) ? DragDropEffects.Copy : DragDropEffects.Move;
            }
            else
            { 
                e.Effect = DragDropEffects.None;
            } 
        } 

        /// <devdoc> 
        /// Paints the insertion mark when items are being reordered
        /// </devdoc>
        private void PaintInsertionMark(ToolStripItem item)
        { 
            // Dont paint if cursor hasnt moved.
            if (ToolStripDesigner.LastCursorPosition != Point.Empty && ToolStripDesigner.LastCursorPosition == Cursor.Position) 
            { 
                return;
            } 
            // Dont paint any "MouseOver" glyohs if TemplateNode is ACTIVE !
            ToolStripKeyboardHandlingService keyService = GetKeyBoardHandlingService(item);
            if (keyService != null && keyService.TemplateNodeActive)
            { 
                return;
            } 
 
            //Start from fresh State...
            if (item != null && item.Site != null) 
            {
                ToolStripDesigner.LastCursorPosition = Cursor.Position;
                IDesignerHost designerHost = (IDesignerHost)item.Site.GetService(typeof(IDesignerHost));
                if (designerHost != null) 
                {
 
                    Rectangle bounds = GetPaintingBounds(designerHost, item); 
                    BehaviorService bSvc = GetBehaviorService(item);
                    if (bSvc != null) 
                    {
                        Graphics g = bSvc.AdornerWindowGraphics;
                        try
                        { 
                            using (Pen p = new Pen(new SolidBrush(Color.Black)))
                            { 
                                p.DashStyle = DashStyle.Dot; 
                                g.DrawRectangle(p, bounds);
                            } 
                        }
                        finally
                        {
                            g.Dispose(); 
                        }
                    } 
 
                }
            } 
        }


 
        /// <include file='doc\ToolStripItemBehavior.uex' path='docs/doc[@for="ToolStripItemBehavior.QueryContinueDrag"]/*' />
        /// <devdoc> 
        ///     QueryContinueDrag can be overridden so that a Behavior can specify its own 
        ///     Drag/Drop rules.
        /// </devdoc> 
        private void QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            // Cancel the drag if the mouse moves off the form.
            if (e.Action == DragAction.Continue) 
            {
                return; 
            } 
            if (e.EscapePressed)
            { 
                e.Action = DragAction.Cancel;
                ToolStripItem item = sender as ToolStripItem;
                SetParentDesignerValuesForDragDrop(item, false, Point.Empty);
                ISelectionService selSvc = GetSelectionService(item); 
                if (selSvc != null)
                { 
                    selSvc.SetSelectedComponents(new IComponent[] { item }, SelectionTypes.Auto); ; 
                }
                ToolStripDesigner.dragItem = null; 
            }
        }

        // Set values before initiating the Drag-Drop 
        private void SetParentDesignerValuesForDragDrop(ToolStripItem glyphItem, bool setValues, Point mouseLoc)
        { 
            if (glyphItem.Site == null) 
            {
                return; 
            }
            // Remember the point where the mouse down occurred. The DragSize indicates
            // the size that the mouse can move before a drag event should be started.
            Size dragSize = new Size(1, 1); 

            IDesignerHost designerHost = (IDesignerHost)glyphItem.Site.GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null, "Invalid DesignerHost"); 

 
            // implement Drag Drop for individual ToolStrip Items
            // While this item is getting selected..
            // Get the index of the item the mouse is below.
            if (glyphItem.Placement == ToolStripItemPlacement.Overflow || (glyphItem.Placement == ToolStripItemPlacement.Main && !(glyphItem.IsOnDropDown))) 
            {
                ToolStripItemDesigner itemDesigner = designerHost.GetDesigner(glyphItem) as ToolStripItemDesigner; 
                ToolStrip parentToolStrip = itemDesigner.GetMainToolStrip(); 
                ToolStripDesigner parentDesigner = designerHost.GetDesigner(parentToolStrip) as ToolStripDesigner;
                if (parentDesigner != null) 
                {
                    if (setValues)
                    {
                        parentDesigner.IndexOfItemUnderMouseToDrag = parentToolStrip.Items.IndexOf(glyphItem); 
                        // Create a rectangle using the DragSize, with the mouse position being
                        // at the center of the rectangle. 
                        // On SelectionChanged we recreate the Glyphs ... 
                        // so need to stash this value on the parentDesigner....
                        parentDesigner.DragBoxFromMouseDown = dragBoxFromMouseDown = new Rectangle(new Point(mouseLoc.X - (dragSize.Width / 2), mouseLoc.Y - (dragSize.Height / 2)), dragSize); 
                    }
                    else
                    {
                        parentDesigner.IndexOfItemUnderMouseToDrag = -1; 
                        parentDesigner.DragBoxFromMouseDown = dragBoxFromMouseDown = Rectangle.Empty;
                    } 
                } 

            } 
            else if (glyphItem.IsOnDropDown)
            {
                //Get the OwnerItem's Designer and set the value...
                ToolStripDropDown parentDropDown = glyphItem.Owner as ToolStripDropDown; 
                if (parentDropDown != null)
                { 
                    ToolStripItem ownerItem = parentDropDown.OwnerItem; 
                    ToolStripItemDesigner ownerItemDesigner = designerHost.GetDesigner(ownerItem) as ToolStripItemDesigner;
                    if (ownerItemDesigner != null) 
                    {

                        if (setValues)
                        { 
                            ownerItemDesigner.indexOfItemUnderMouseToDrag = parentDropDown.Items.IndexOf(glyphItem);
                            // Create a rectangle using the DragSize, with the mouse position being 
                            // at the center of the rectangle. 
                            // On SelectionChanged we recreate the Glyphs ...
                            // so need to stash this value on the parentDesigner.... 
                            ownerItemDesigner.dragBoxFromMouseDown = dragBoxFromMouseDown = new Rectangle(new Point(mouseLoc.X - (dragSize.Width / 2), mouseLoc.Y - (dragSize.Height / 2)), dragSize);
                        }
                        else
                        { 
                            ownerItemDesigner.indexOfItemUnderMouseToDrag = -1;
                            ownerItemDesigner.dragBoxFromMouseDown = dragBoxFromMouseDown = Rectangle.Empty; 
                        } 
                    }
 
                }

            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
