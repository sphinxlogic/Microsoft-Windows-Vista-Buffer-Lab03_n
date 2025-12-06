namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Drawing.Drawing2D;
    using System.Windows.Forms.Design; 
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis;

    /// <include file='doc\TableLayoutPanelBehavior.uex' path='docs/doc[@for="TableLayoutPanelBehavior"]/*' /> 
    /// <devdoc>
    ///     The TableLayoutPanelDesigner has a single instance of this behavior.  Each TableLayoutPanelGlyph 
    //      will use this one instance to resize its rows and columns. 
    /// </devdoc>
    internal class TableLayoutPanelBehavior : Behavior { 

        private TableLayoutPanelDesigner    designer;//pointer back to our designer.
        private Point                       lastMouseLoc;//used to track mouse movement deltas
        private bool                        pushedBehavior;//tracks if we've pushed ourself onto the stack 
        private BehaviorService             behaviorService;//used for bounds translation
        private IServiceProvider            serviceProvider;//cached to allow our behavior to get services 
        private TableLayoutPanelResizeGlyph     tableGlyph;//the glyph being resized 
        private DesignerTransaction         resizeTransaction;//used to make size adjustements within transaction
        private PropertyDescriptor          resizeProp;//cached property descriptor representing either the row or column styles 
        private PropertyDescriptor          changedProp; //cached property descriptor that refers to the RowSTyles or ColumnStyles collection.
        private TableLayoutPanel            table;
        private StyleHelper                 rightStyle;
        private StyleHelper                 leftStyle; 
        private ArrayList                   styles; //List of the styles
        private bool                        currentColumnStyles; // is Styles for Columns or Rows 
 
        #if DEBUG
        private static readonly TraceSwitch          tlpResizeSwitch = new TraceSwitch("TLPRESIZE", "Behavior service drag & drop messages"); 
        #else
        private static readonly TraceSwitch          tlpResizeSwitch;
        #endif
 
        /// <include file='doc\TableLayoutPanelBehavior.uex' path='docs/doc[@for="TableLayoutPanelBehavior.TableLayoutPanelBehavior"]/*' />
        /// <devdoc> 
        ///     Standard constructor that caches services and clears values. 
        /// </devdoc>
        internal TableLayoutPanelBehavior(TableLayoutPanel panel, TableLayoutPanelDesigner designer, IServiceProvider serviceProvider) {//: base(designer) { 
            this.table = panel;
            this.designer = designer;
            this.serviceProvider = serviceProvider;
 
            behaviorService = serviceProvider.GetService(typeof(BehaviorService)) as BehaviorService;
 
            if (behaviorService == null) { 
                Debug.Fail("BehaviorService could not be found!");
                return; 
            }

            pushedBehavior = false;
            lastMouseLoc = Point.Empty; 
        }
 
        /// <include file='doc\TableLayoutPanelBehavior.uex' path='docs/doc[@for="TableLayoutPanelBehavior.FinishResize"]/*' /> 
        /// <devdoc>
        ///     Called at the end of a resize operation -this clears state and refreshes the selection 
        /// </devdoc>
        private void FinishResize() {
            //clear state
            pushedBehavior = false; 
            behaviorService.PopBehavior(this);
            lastMouseLoc = Point.Empty; 
            styles = null; 

            // fire ComponentChange events so this event is undoable 
            IComponentChangeService cs = serviceProvider.GetService(typeof(IComponentChangeService)) as IComponentChangeService;
            if (cs != null && changedProp != null) {
                cs.OnComponentChanged(table, changedProp, null, null);
                changedProp = null; 
            }
 
            //attempt to refresh the selection 
            SelectionManager selManager = serviceProvider.GetService(typeof(SelectionManager)) as SelectionManager;
            if (selManager != null) { 
                selManager.Refresh();
            }
        }
 
        /// <include file='doc\TableLayoutPanelBehavior.uex' path='docs/doc[@for="TableLayoutPanelBehavior.OnLoseCapture"]/*' />
        /// <devdoc> 
        ///     This method is called when we lose capture during our resize actions. 
        ///     Here, we clear our state and cancel our transaction.
        /// </devdoc> 
        public override void OnLoseCapture(Glyph g, EventArgs e) {
            if (pushedBehavior) {
                FinishResize();
 
                // If we still have a transaction, roll it back.
                // 
                if (resizeTransaction != null) { 
                    DesignerTransaction t = resizeTransaction;
                    resizeTransaction = null; 
                    using (t) {
                        t.Cancel();
                    }
                } 
            }
        } 
 
        internal struct StyleHelper
        { 
            public int index;
            public PropertyDescriptor styleProp;
            public TableLayoutStyle   style;
        } 

        /// <include file='doc\TableLayoutPanelBehavior.uex' path='docs/doc[@for="TableLayoutPanelBehavior.OnMouseDown"]/*' /> 
        /// <devdoc> 
        ///     This method will cache off our glyph, set the proper selection and
        //      push this behavior onto the stack. 
        /// </devdoc>
        public override bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc) {

            //we only care about the right mouse button for resizing 
            if (button == MouseButtons.Left && g is TableLayoutPanelResizeGlyph) {
 
                tableGlyph = g as TableLayoutPanelResizeGlyph; 

                //select the table 
                ISelectionService selSvc = serviceProvider.GetService(typeof(ISelectionService)) as ISelectionService;
                if (selSvc != null) {
                    selSvc.SetSelectedComponents(new object[] {designer.Component}, SelectionTypes.Primary);
                } 

                bool isColumn = tableGlyph.Type == TableLayoutPanelResizeGlyph.TableLayoutResizeType.Column; 
 
                //cache some state
                lastMouseLoc = mouseLoc; 
                resizeProp = TypeDescriptor.GetProperties(tableGlyph.Style)[isColumn ? "Width" : "Height"];
                Debug.Assert(resizeProp != null, "Unable to get the resize property for tableGlyph's Style");

                IComponentChangeService cs = serviceProvider.GetService(typeof(IComponentChangeService)) as IComponentChangeService; 
                if (cs != null) {
                    changedProp = TypeDescriptor.GetProperties(table)[isColumn ? "ColumnStyles" : "RowStyles"]; 
                    int[] widths = isColumn ? table.GetColumnWidths() : table.GetRowHeights(); 

                    if (changedProp != null) { 
                        GetActiveStyleCollection(isColumn);
                        if (styles != null && CanResizeStyle(widths)) {
                            IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;
                            if (host != null) { 
                                resizeTransaction = host.CreateTransaction(SR.GetString(SR.TableLayoutPanelRowColResize, (isColumn ? "Column" : "Row"), designer.Control.Site.Name));
                            } 
 
                            try {
                                int moveIndex = styles.IndexOf(tableGlyph.Style); 
                                rightStyle.index = IndexOfNextStealableStyle(true /*forward*/, moveIndex, widths);
                                rightStyle.style = (TableLayoutStyle)styles[rightStyle.index];
                                rightStyle.styleProp = TypeDescriptor.GetProperties(rightStyle.style)[isColumn ? "Width" : "Height"];
 
                                leftStyle.index = IndexOfNextStealableStyle(false /*backwards*/, moveIndex, widths);
                                leftStyle.style = (TableLayoutStyle)styles[leftStyle.index]; 
                                leftStyle.styleProp = TypeDescriptor.GetProperties(leftStyle.style)[isColumn ? "Width" : "Height"]; 

                                Debug.Assert(leftStyle.styleProp != null && rightStyle.styleProp != null, "Couldn't find property descriptor for width or height"); 

                                cs.OnComponentChanging(table, changedProp);
                            }
                            catch (CheckoutException checkoutException) { 
                                if (CheckoutException.Canceled.Equals(checkoutException)) {
                                    if ((resizeTransaction != null) && (!resizeTransaction.Canceled)) { 
                                        resizeTransaction.Cancel(); 
                                    }
                                } 
                                throw;
                            }
                        }
                        else { 
                            return false;
                        } 
                    } 
                }
 
                //push this resizebehavior
                behaviorService.PushCaptureBehavior(this);
                pushedBehavior = true;
            } 

            return false; 
        } 

        private void GetActiveStyleCollection(bool isColumn) { 
            if ((styles == null || isColumn != currentColumnStyles) && table != null) {
                styles = new ArrayList(changedProp.GetValue(table) as TableLayoutStyleCollection);
                currentColumnStyles = isColumn;
            } 
        }
 
        private bool ColumnResize  { 
            get {
                bool ret = false; 
                if (tableGlyph != null) {
                    ret = tableGlyph.Type == TableLayoutPanelResizeGlyph.TableLayoutResizeType.Column;
                }
                return ret; 
            }
        } 
 
        private bool CanResizeStyle(int[] widths) {
            bool canStealFromRight = false; 
            bool canStealFromLeft = false;
            int moveIndex = ((IList)styles).IndexOf(tableGlyph.Style);
            if (moveIndex > -1 && moveIndex != styles.Count) {
                canStealFromRight = IndexOfNextStealableStyle(true, moveIndex, widths) != -1; 
                canStealFromLeft  = IndexOfNextStealableStyle(false, moveIndex, widths) != -1;
            } 
            else { 
                Debug.Fail("Can't find style " + moveIndex);
                return false; 
            }

            return canStealFromRight && canStealFromLeft;
        } 

        private int IndexOfNextStealableStyle(bool forward, int startIndex, int[] widths) { 
            int stealIndex = -1; 

            if (styles != null) { 
                if (forward) {
                    for (int i = startIndex + 1; i < styles.Count; i++) {
                        if (((TableLayoutStyle)styles[i]).SizeType != SizeType.AutoSize && widths[i] >= DesignerUtils.MINUMUMSTYLESIZEDRAG) {
                            stealIndex = i; 
                            break;
                        } 
                    } 
                }
                else { 
                    for (int i = startIndex; i >= 0; i--) {
                        if (((TableLayoutStyle)styles[i]).SizeType != SizeType.AutoSize && widths[i] >= DesignerUtils.MINUMUMSTYLESIZEDRAG) {
                            stealIndex = i;
                            break; 
                        }
                    } 
                } 
            }
 
            return stealIndex;
        }

        /// <include file='doc\TableLayoutPanelBehavior.uex' path='docs/doc[@for="TableLayoutPanelBehavior.OnMouseMove"]/*' /> 
        /// <devdoc>
        ///     If we have pushed ourselves onto the stack then use our propertyDescriptor to 
        ///     make realtime adjustments to the table's rows or columns. 
        /// </devdoc>
        [SuppressMessage("Microsoft.Portability", "CA1902:AvoidTestingForFloatingPointEquality")] 
        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        public override bool OnMouseMove(Glyph g, MouseButtons button, Point mouseLoc) {
            if (pushedBehavior) {
                bool isColumn = ColumnResize; 
                GetActiveStyleCollection(isColumn);
                if (styles != null) { 
                    int rightIndex = rightStyle.index; 
                    int leftIndex  = leftStyle.index;
 
                    int delta  = isColumn ? mouseLoc.X - lastMouseLoc.X : mouseLoc.Y - lastMouseLoc.Y;
                    if (isColumn && table.RightToLeft == RightToLeft.Yes) {
                        delta *= -1;
                    } 

                    if (delta == 0) { 
                        Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "0 mouse delta"); 
                        return false;
                    } 

                    Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "BEGIN RESIZE");
                    Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "mouse delta: " + delta);
 
                    int[] oldWidths = isColumn ? table.GetColumnWidths() : table.GetRowHeights();
 
                    int[] newWidths = oldWidths.Clone() as int[]; 

                    newWidths[rightIndex] -= delta; 
                    newWidths[leftIndex ] += delta;

                    if (newWidths[rightIndex] < DesignerUtils.MINUMUMSTYLESIZEDRAG ||
                        newWidths[leftIndex] < DesignerUtils.MINUMUMSTYLESIZEDRAG) { 
                        Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "Bottomed out.");
                        Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "END RESIZE\n"); 
                        return false; 
                    }
 
                    // now we must renormalize our new widths into the correct sizes
                    table.SuspendLayout();

                    int totalSize = oldWidths[rightIndex] + oldWidths[leftIndex]; 
                    int totalPercent = 0;
 
                    //simplest case: two absolute columns just affect each other. 
                    if (((TableLayoutStyle)styles[rightIndex]).SizeType == SizeType.Absolute &&
                        ((TableLayoutStyle)styles[leftIndex]).SizeType  == SizeType.Absolute) { 

                        // VSWhidbey 465751
                        // The dimensions reported by GetColumnsWidths() are different
                        // than the style dimensions when the TLP has borders. Instead 
                        // of always setting the new size directly based on the reported
                        // sizes, we now base them on the style size if necessary. 
                        float newRightSize = newWidths[rightIndex]; 
                        float rightStyleSize = (float) rightStyle.styleProp.GetValue(rightStyle.style);
 
                        if (rightStyleSize != oldWidths[rightIndex]) {
                            newRightSize = Math.Max(rightStyleSize - delta, DesignerUtils.MINUMUMSTYLESIZEDRAG);
                        }
 
                        float newLeftSize  = newWidths[leftIndex];
                        float leftStyleSize = (float) leftStyle.styleProp.GetValue(leftStyle.style); 
 
                        if (leftStyleSize != oldWidths[leftIndex]) {
                            newLeftSize = Math.Max(leftStyleSize + delta, DesignerUtils.MINUMUMSTYLESIZEDRAG); 
                        }

                        rightStyle.styleProp.SetValue(rightStyle.style, newRightSize);
                        leftStyle.styleProp.SetValue(leftStyle.style, newLeftSize); 
                    }
                    // two percents just steal from each other. 
                    else if(((TableLayoutStyle)styles[rightIndex]).SizeType == SizeType.Percent && 
                        ((TableLayoutStyle)styles[leftIndex ]).SizeType == SizeType.Percent) {
 
                        for (int i = 0; i < styles.Count; i++) {
                            if (((TableLayoutStyle)styles[i]).SizeType == SizeType.Percent) {
                                totalPercent += oldWidths[i];
                            } 
                        }
                        for (int j = 0; j < 2; j++) { 
                            int i = j == 0 ? rightIndex : leftIndex; 
                            float newSize = (float) newWidths[i] * 100 / (float) totalPercent;
                            Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "NewSize " + newSize); 

                            PropertyDescriptor prop = TypeDescriptor.GetProperties(styles[i])[isColumn ? "Width" : "Height"];
                            if (prop != null) {
                                prop.SetValue(styles[i], newSize); 
                                Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "Resizing column (per) " + i.ToString(CultureInfo.InvariantCulture) + " to " + newSize.ToString(CultureInfo.InvariantCulture));
                            } 
                        } 

                    } 
                    else {

                        #if DEBUG
                        for (int i = 0; i < oldWidths.Length; i++) { 
                            Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "Col " + i + ": Old: " + oldWidths[i] + " New: " + newWidths[i]);
                        } 
                        #endif 

                        //mixed - just update absolute 
                        int absIndex = ((TableLayoutStyle)styles[rightIndex]).SizeType == SizeType.Absolute ? rightIndex : leftIndex;
                        PropertyDescriptor prop = TypeDescriptor.GetProperties(styles[absIndex])[isColumn ? "Width" : "Height"];
                        if (prop != null) {
 
                            // VSWhidbey 465751
                            // The dimensions reported by GetColumnsWidths() are different 
                            // than the style dimensions when the TLP has borders. Instead 
                            // of always setting the new size directly based on the reported
                            // sizes, we now base them on the style size if necessary. 
                            float newAbsSize = newWidths[absIndex];
                            float curAbsStyleSize = (float) prop.GetValue(styles[absIndex]);

                            if (curAbsStyleSize != oldWidths[absIndex]) { 
                                newAbsSize = Math.Max(absIndex == rightIndex ? curAbsStyleSize - delta : curAbsStyleSize + delta,
                                                        DesignerUtils.MINUMUMSTYLESIZEDRAG); 
                            } 

                            prop.SetValue(styles[absIndex], newAbsSize); 
                            Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "Resizing column (abs) " + absIndex.ToString(CultureInfo.InvariantCulture) + " to " + newWidths[absIndex]);
                        }
                        else {
                            Debug.Fail("Can't resize - no propertyescriptor for column"); 
                        }
 
                    } 
                    table.ResumeLayout(true);
 
                    // now determine if the values we pushed into the TLP
                    // actually had any effect.  If they didn't,
                    // we delay updating the last mouse position so that
                    // next time a mouse move message comes in the delta is larger. 
                    bool updatedSize = true;
                    int[] updatedWidths = isColumn ? table.GetColumnWidths() : table.GetRowHeights(); 
 
                    for(int i = 0; i < updatedWidths.Length; i++) {
                        if (updatedWidths[i] == oldWidths[i] && newWidths[i] != oldWidths[i]) { 
                            updatedSize = false;
                        }
                    }
 
                    if (updatedSize) {
                        lastMouseLoc = mouseLoc; 
                    } 
                }
                else { 
                    lastMouseLoc = mouseLoc;
                }
            }
            Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose && pushedBehavior, "END RESIZE\n"); 
            return false;
        } 
 
        /// <include file='doc\TableLayoutPanelBehavior.uex' path='docs/doc[@for="TableLayoutPanelBehavior.OnMouseUp"]/*' />
        /// <devdoc> 
        ///     Here, we clear our cached state and commit the transaction.
        /// </devdoc>
        public override bool OnMouseUp(Glyph g, MouseButtons button) {
            if (pushedBehavior) { 
                FinishResize();
                //commit transaction 
                if (resizeTransaction != null) { 
                    DesignerTransaction t = resizeTransaction;
                    resizeTransaction = null; 
                    using (t) {

                        t.Commit();
                    } 
                    resizeProp = null;
                } 
 
            }
            return false; 
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
    using System.Drawing.Drawing2D;
    using System.Windows.Forms.Design; 
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis;

    /// <include file='doc\TableLayoutPanelBehavior.uex' path='docs/doc[@for="TableLayoutPanelBehavior"]/*' /> 
    /// <devdoc>
    ///     The TableLayoutPanelDesigner has a single instance of this behavior.  Each TableLayoutPanelGlyph 
    //      will use this one instance to resize its rows and columns. 
    /// </devdoc>
    internal class TableLayoutPanelBehavior : Behavior { 

        private TableLayoutPanelDesigner    designer;//pointer back to our designer.
        private Point                       lastMouseLoc;//used to track mouse movement deltas
        private bool                        pushedBehavior;//tracks if we've pushed ourself onto the stack 
        private BehaviorService             behaviorService;//used for bounds translation
        private IServiceProvider            serviceProvider;//cached to allow our behavior to get services 
        private TableLayoutPanelResizeGlyph     tableGlyph;//the glyph being resized 
        private DesignerTransaction         resizeTransaction;//used to make size adjustements within transaction
        private PropertyDescriptor          resizeProp;//cached property descriptor representing either the row or column styles 
        private PropertyDescriptor          changedProp; //cached property descriptor that refers to the RowSTyles or ColumnStyles collection.
        private TableLayoutPanel            table;
        private StyleHelper                 rightStyle;
        private StyleHelper                 leftStyle; 
        private ArrayList                   styles; //List of the styles
        private bool                        currentColumnStyles; // is Styles for Columns or Rows 
 
        #if DEBUG
        private static readonly TraceSwitch          tlpResizeSwitch = new TraceSwitch("TLPRESIZE", "Behavior service drag & drop messages"); 
        #else
        private static readonly TraceSwitch          tlpResizeSwitch;
        #endif
 
        /// <include file='doc\TableLayoutPanelBehavior.uex' path='docs/doc[@for="TableLayoutPanelBehavior.TableLayoutPanelBehavior"]/*' />
        /// <devdoc> 
        ///     Standard constructor that caches services and clears values. 
        /// </devdoc>
        internal TableLayoutPanelBehavior(TableLayoutPanel panel, TableLayoutPanelDesigner designer, IServiceProvider serviceProvider) {//: base(designer) { 
            this.table = panel;
            this.designer = designer;
            this.serviceProvider = serviceProvider;
 
            behaviorService = serviceProvider.GetService(typeof(BehaviorService)) as BehaviorService;
 
            if (behaviorService == null) { 
                Debug.Fail("BehaviorService could not be found!");
                return; 
            }

            pushedBehavior = false;
            lastMouseLoc = Point.Empty; 
        }
 
        /// <include file='doc\TableLayoutPanelBehavior.uex' path='docs/doc[@for="TableLayoutPanelBehavior.FinishResize"]/*' /> 
        /// <devdoc>
        ///     Called at the end of a resize operation -this clears state and refreshes the selection 
        /// </devdoc>
        private void FinishResize() {
            //clear state
            pushedBehavior = false; 
            behaviorService.PopBehavior(this);
            lastMouseLoc = Point.Empty; 
            styles = null; 

            // fire ComponentChange events so this event is undoable 
            IComponentChangeService cs = serviceProvider.GetService(typeof(IComponentChangeService)) as IComponentChangeService;
            if (cs != null && changedProp != null) {
                cs.OnComponentChanged(table, changedProp, null, null);
                changedProp = null; 
            }
 
            //attempt to refresh the selection 
            SelectionManager selManager = serviceProvider.GetService(typeof(SelectionManager)) as SelectionManager;
            if (selManager != null) { 
                selManager.Refresh();
            }
        }
 
        /// <include file='doc\TableLayoutPanelBehavior.uex' path='docs/doc[@for="TableLayoutPanelBehavior.OnLoseCapture"]/*' />
        /// <devdoc> 
        ///     This method is called when we lose capture during our resize actions. 
        ///     Here, we clear our state and cancel our transaction.
        /// </devdoc> 
        public override void OnLoseCapture(Glyph g, EventArgs e) {
            if (pushedBehavior) {
                FinishResize();
 
                // If we still have a transaction, roll it back.
                // 
                if (resizeTransaction != null) { 
                    DesignerTransaction t = resizeTransaction;
                    resizeTransaction = null; 
                    using (t) {
                        t.Cancel();
                    }
                } 
            }
        } 
 
        internal struct StyleHelper
        { 
            public int index;
            public PropertyDescriptor styleProp;
            public TableLayoutStyle   style;
        } 

        /// <include file='doc\TableLayoutPanelBehavior.uex' path='docs/doc[@for="TableLayoutPanelBehavior.OnMouseDown"]/*' /> 
        /// <devdoc> 
        ///     This method will cache off our glyph, set the proper selection and
        //      push this behavior onto the stack. 
        /// </devdoc>
        public override bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc) {

            //we only care about the right mouse button for resizing 
            if (button == MouseButtons.Left && g is TableLayoutPanelResizeGlyph) {
 
                tableGlyph = g as TableLayoutPanelResizeGlyph; 

                //select the table 
                ISelectionService selSvc = serviceProvider.GetService(typeof(ISelectionService)) as ISelectionService;
                if (selSvc != null) {
                    selSvc.SetSelectedComponents(new object[] {designer.Component}, SelectionTypes.Primary);
                } 

                bool isColumn = tableGlyph.Type == TableLayoutPanelResizeGlyph.TableLayoutResizeType.Column; 
 
                //cache some state
                lastMouseLoc = mouseLoc; 
                resizeProp = TypeDescriptor.GetProperties(tableGlyph.Style)[isColumn ? "Width" : "Height"];
                Debug.Assert(resizeProp != null, "Unable to get the resize property for tableGlyph's Style");

                IComponentChangeService cs = serviceProvider.GetService(typeof(IComponentChangeService)) as IComponentChangeService; 
                if (cs != null) {
                    changedProp = TypeDescriptor.GetProperties(table)[isColumn ? "ColumnStyles" : "RowStyles"]; 
                    int[] widths = isColumn ? table.GetColumnWidths() : table.GetRowHeights(); 

                    if (changedProp != null) { 
                        GetActiveStyleCollection(isColumn);
                        if (styles != null && CanResizeStyle(widths)) {
                            IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;
                            if (host != null) { 
                                resizeTransaction = host.CreateTransaction(SR.GetString(SR.TableLayoutPanelRowColResize, (isColumn ? "Column" : "Row"), designer.Control.Site.Name));
                            } 
 
                            try {
                                int moveIndex = styles.IndexOf(tableGlyph.Style); 
                                rightStyle.index = IndexOfNextStealableStyle(true /*forward*/, moveIndex, widths);
                                rightStyle.style = (TableLayoutStyle)styles[rightStyle.index];
                                rightStyle.styleProp = TypeDescriptor.GetProperties(rightStyle.style)[isColumn ? "Width" : "Height"];
 
                                leftStyle.index = IndexOfNextStealableStyle(false /*backwards*/, moveIndex, widths);
                                leftStyle.style = (TableLayoutStyle)styles[leftStyle.index]; 
                                leftStyle.styleProp = TypeDescriptor.GetProperties(leftStyle.style)[isColumn ? "Width" : "Height"]; 

                                Debug.Assert(leftStyle.styleProp != null && rightStyle.styleProp != null, "Couldn't find property descriptor for width or height"); 

                                cs.OnComponentChanging(table, changedProp);
                            }
                            catch (CheckoutException checkoutException) { 
                                if (CheckoutException.Canceled.Equals(checkoutException)) {
                                    if ((resizeTransaction != null) && (!resizeTransaction.Canceled)) { 
                                        resizeTransaction.Cancel(); 
                                    }
                                } 
                                throw;
                            }
                        }
                        else { 
                            return false;
                        } 
                    } 
                }
 
                //push this resizebehavior
                behaviorService.PushCaptureBehavior(this);
                pushedBehavior = true;
            } 

            return false; 
        } 

        private void GetActiveStyleCollection(bool isColumn) { 
            if ((styles == null || isColumn != currentColumnStyles) && table != null) {
                styles = new ArrayList(changedProp.GetValue(table) as TableLayoutStyleCollection);
                currentColumnStyles = isColumn;
            } 
        }
 
        private bool ColumnResize  { 
            get {
                bool ret = false; 
                if (tableGlyph != null) {
                    ret = tableGlyph.Type == TableLayoutPanelResizeGlyph.TableLayoutResizeType.Column;
                }
                return ret; 
            }
        } 
 
        private bool CanResizeStyle(int[] widths) {
            bool canStealFromRight = false; 
            bool canStealFromLeft = false;
            int moveIndex = ((IList)styles).IndexOf(tableGlyph.Style);
            if (moveIndex > -1 && moveIndex != styles.Count) {
                canStealFromRight = IndexOfNextStealableStyle(true, moveIndex, widths) != -1; 
                canStealFromLeft  = IndexOfNextStealableStyle(false, moveIndex, widths) != -1;
            } 
            else { 
                Debug.Fail("Can't find style " + moveIndex);
                return false; 
            }

            return canStealFromRight && canStealFromLeft;
        } 

        private int IndexOfNextStealableStyle(bool forward, int startIndex, int[] widths) { 
            int stealIndex = -1; 

            if (styles != null) { 
                if (forward) {
                    for (int i = startIndex + 1; i < styles.Count; i++) {
                        if (((TableLayoutStyle)styles[i]).SizeType != SizeType.AutoSize && widths[i] >= DesignerUtils.MINUMUMSTYLESIZEDRAG) {
                            stealIndex = i; 
                            break;
                        } 
                    } 
                }
                else { 
                    for (int i = startIndex; i >= 0; i--) {
                        if (((TableLayoutStyle)styles[i]).SizeType != SizeType.AutoSize && widths[i] >= DesignerUtils.MINUMUMSTYLESIZEDRAG) {
                            stealIndex = i;
                            break; 
                        }
                    } 
                } 
            }
 
            return stealIndex;
        }

        /// <include file='doc\TableLayoutPanelBehavior.uex' path='docs/doc[@for="TableLayoutPanelBehavior.OnMouseMove"]/*' /> 
        /// <devdoc>
        ///     If we have pushed ourselves onto the stack then use our propertyDescriptor to 
        ///     make realtime adjustments to the table's rows or columns. 
        /// </devdoc>
        [SuppressMessage("Microsoft.Portability", "CA1902:AvoidTestingForFloatingPointEquality")] 
        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        public override bool OnMouseMove(Glyph g, MouseButtons button, Point mouseLoc) {
            if (pushedBehavior) {
                bool isColumn = ColumnResize; 
                GetActiveStyleCollection(isColumn);
                if (styles != null) { 
                    int rightIndex = rightStyle.index; 
                    int leftIndex  = leftStyle.index;
 
                    int delta  = isColumn ? mouseLoc.X - lastMouseLoc.X : mouseLoc.Y - lastMouseLoc.Y;
                    if (isColumn && table.RightToLeft == RightToLeft.Yes) {
                        delta *= -1;
                    } 

                    if (delta == 0) { 
                        Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "0 mouse delta"); 
                        return false;
                    } 

                    Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "BEGIN RESIZE");
                    Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "mouse delta: " + delta);
 
                    int[] oldWidths = isColumn ? table.GetColumnWidths() : table.GetRowHeights();
 
                    int[] newWidths = oldWidths.Clone() as int[]; 

                    newWidths[rightIndex] -= delta; 
                    newWidths[leftIndex ] += delta;

                    if (newWidths[rightIndex] < DesignerUtils.MINUMUMSTYLESIZEDRAG ||
                        newWidths[leftIndex] < DesignerUtils.MINUMUMSTYLESIZEDRAG) { 
                        Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "Bottomed out.");
                        Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "END RESIZE\n"); 
                        return false; 
                    }
 
                    // now we must renormalize our new widths into the correct sizes
                    table.SuspendLayout();

                    int totalSize = oldWidths[rightIndex] + oldWidths[leftIndex]; 
                    int totalPercent = 0;
 
                    //simplest case: two absolute columns just affect each other. 
                    if (((TableLayoutStyle)styles[rightIndex]).SizeType == SizeType.Absolute &&
                        ((TableLayoutStyle)styles[leftIndex]).SizeType  == SizeType.Absolute) { 

                        // VSWhidbey 465751
                        // The dimensions reported by GetColumnsWidths() are different
                        // than the style dimensions when the TLP has borders. Instead 
                        // of always setting the new size directly based on the reported
                        // sizes, we now base them on the style size if necessary. 
                        float newRightSize = newWidths[rightIndex]; 
                        float rightStyleSize = (float) rightStyle.styleProp.GetValue(rightStyle.style);
 
                        if (rightStyleSize != oldWidths[rightIndex]) {
                            newRightSize = Math.Max(rightStyleSize - delta, DesignerUtils.MINUMUMSTYLESIZEDRAG);
                        }
 
                        float newLeftSize  = newWidths[leftIndex];
                        float leftStyleSize = (float) leftStyle.styleProp.GetValue(leftStyle.style); 
 
                        if (leftStyleSize != oldWidths[leftIndex]) {
                            newLeftSize = Math.Max(leftStyleSize + delta, DesignerUtils.MINUMUMSTYLESIZEDRAG); 
                        }

                        rightStyle.styleProp.SetValue(rightStyle.style, newRightSize);
                        leftStyle.styleProp.SetValue(leftStyle.style, newLeftSize); 
                    }
                    // two percents just steal from each other. 
                    else if(((TableLayoutStyle)styles[rightIndex]).SizeType == SizeType.Percent && 
                        ((TableLayoutStyle)styles[leftIndex ]).SizeType == SizeType.Percent) {
 
                        for (int i = 0; i < styles.Count; i++) {
                            if (((TableLayoutStyle)styles[i]).SizeType == SizeType.Percent) {
                                totalPercent += oldWidths[i];
                            } 
                        }
                        for (int j = 0; j < 2; j++) { 
                            int i = j == 0 ? rightIndex : leftIndex; 
                            float newSize = (float) newWidths[i] * 100 / (float) totalPercent;
                            Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "NewSize " + newSize); 

                            PropertyDescriptor prop = TypeDescriptor.GetProperties(styles[i])[isColumn ? "Width" : "Height"];
                            if (prop != null) {
                                prop.SetValue(styles[i], newSize); 
                                Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "Resizing column (per) " + i.ToString(CultureInfo.InvariantCulture) + " to " + newSize.ToString(CultureInfo.InvariantCulture));
                            } 
                        } 

                    } 
                    else {

                        #if DEBUG
                        for (int i = 0; i < oldWidths.Length; i++) { 
                            Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "Col " + i + ": Old: " + oldWidths[i] + " New: " + newWidths[i]);
                        } 
                        #endif 

                        //mixed - just update absolute 
                        int absIndex = ((TableLayoutStyle)styles[rightIndex]).SizeType == SizeType.Absolute ? rightIndex : leftIndex;
                        PropertyDescriptor prop = TypeDescriptor.GetProperties(styles[absIndex])[isColumn ? "Width" : "Height"];
                        if (prop != null) {
 
                            // VSWhidbey 465751
                            // The dimensions reported by GetColumnsWidths() are different 
                            // than the style dimensions when the TLP has borders. Instead 
                            // of always setting the new size directly based on the reported
                            // sizes, we now base them on the style size if necessary. 
                            float newAbsSize = newWidths[absIndex];
                            float curAbsStyleSize = (float) prop.GetValue(styles[absIndex]);

                            if (curAbsStyleSize != oldWidths[absIndex]) { 
                                newAbsSize = Math.Max(absIndex == rightIndex ? curAbsStyleSize - delta : curAbsStyleSize + delta,
                                                        DesignerUtils.MINUMUMSTYLESIZEDRAG); 
                            } 

                            prop.SetValue(styles[absIndex], newAbsSize); 
                            Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose, "Resizing column (abs) " + absIndex.ToString(CultureInfo.InvariantCulture) + " to " + newWidths[absIndex]);
                        }
                        else {
                            Debug.Fail("Can't resize - no propertyescriptor for column"); 
                        }
 
                    } 
                    table.ResumeLayout(true);
 
                    // now determine if the values we pushed into the TLP
                    // actually had any effect.  If they didn't,
                    // we delay updating the last mouse position so that
                    // next time a mouse move message comes in the delta is larger. 
                    bool updatedSize = true;
                    int[] updatedWidths = isColumn ? table.GetColumnWidths() : table.GetRowHeights(); 
 
                    for(int i = 0; i < updatedWidths.Length; i++) {
                        if (updatedWidths[i] == oldWidths[i] && newWidths[i] != oldWidths[i]) { 
                            updatedSize = false;
                        }
                    }
 
                    if (updatedSize) {
                        lastMouseLoc = mouseLoc; 
                    } 
                }
                else { 
                    lastMouseLoc = mouseLoc;
                }
            }
            Debug.WriteLineIf(tlpResizeSwitch.TraceVerbose && pushedBehavior, "END RESIZE\n"); 
            return false;
        } 
 
        /// <include file='doc\TableLayoutPanelBehavior.uex' path='docs/doc[@for="TableLayoutPanelBehavior.OnMouseUp"]/*' />
        /// <devdoc> 
        ///     Here, we clear our cached state and commit the transaction.
        /// </devdoc>
        public override bool OnMouseUp(Glyph g, MouseButtons button) {
            if (pushedBehavior) { 
                FinishResize();
                //commit transaction 
                if (resizeTransaction != null) { 
                    DesignerTransaction t = resizeTransaction;
                    resizeTransaction = null; 
                    using (t) {

                        t.Commit();
                    } 
                    resizeProp = null;
                } 
 
            }
            return false; 
        }
    }

} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
