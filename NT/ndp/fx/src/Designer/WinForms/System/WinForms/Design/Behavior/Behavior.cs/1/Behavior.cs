namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis; 
    using System.Drawing;
    using System.Windows.Forms.Design; 

    /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior"]/*' />
    /// <devdoc>
    ///     This abstract class represents the Behavior objects that are managed 
    ///     by the BehaviorService.  This class can be extended to develop any
    ///     type of UI 'behavior'.  Ex: selection, drag, and resize behaviors. 
    /// </devdoc> 
    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public abstract class Behavior { 

        private bool callParentBehavior = false;
        private BehaviorService bhvSvc;
 
        protected Behavior() {
        } 
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.Behavior"]/*' />
        /// 
        /// <devdoc>
        /// callParentBehavior - true if the parentBehavior should be called if it exists. The
        /// parentBehavior is the next behavior on the behaviorService stack.
        /// 
        /// If callParentBehavior is true, then behaviorService must be non-null
        /// 
        /// </devdoc> 
        protected Behavior(bool callParentBehavior, BehaviorService behaviorService) {
            if ((callParentBehavior == true) && (behaviorService == null)) { 
                throw new ArgumentException("behaviorService");
            }

            this.callParentBehavior = callParentBehavior; 
            this.bhvSvc = behaviorService;
 
        } 

        private Behavior GetNextBehavior { 
            get {
                if (bhvSvc != null) {
                    return bhvSvc.GetNextBehavior(this);
                } 

                return null; 
            } 
        }
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.Cursor"]/*' />
        /// <devdoc>
        ///     The cursor that should be displayed for this behavior.
        /// </devdoc> 
        public virtual Cursor Cursor {
            get { 
                return Cursors.Default; 
            }
        } 

        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.DisableAllCommands"]/*' />
        /// <devdoc>
        ///     Rerturning true from here indicates to the BehaviorService that 
        ///     all MenuCommands the designer receives should have their
        ///     state set to 'Enabled = false' when this Behavior is active. 
        /// </devdoc> 
        public virtual bool DisableAllCommands {
            get { 
                if(callParentBehavior && GetNextBehavior != null) {
                    return GetNextBehavior.DisableAllCommands;
                } else {
                    return false; 
                }
            } 
        } 

        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.FindCommand"]/*' /> 
        /// <devdoc>
        ///     Called from the BehaviorService, this function provides an opportunity
        ///     for the Behavior to return its own custom MenuCommand thereby
        ///     intercepting this message. 
        /// </devdoc>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
        public virtual MenuCommand FindCommand(CommandID commandId) {
            try 
            {
                if(callParentBehavior && GetNextBehavior != null) {
                    return GetNextBehavior.FindCommand(commandId);
                } else { 
                    return null;
                } 
            } 
            catch //Catch any exception and return null MenuCommand.
            { 
                return null;
            }
        }
 
        /// <devdoc>
        ///     The heuristic we will follow when any of these methods are called 
        ///     is that we will attempt to pass the message along to the glyph. 
        ///     This is a helper method to ensure validity before forwarding the message.
        /// </devdoc> 
        private bool GlyphIsValid(Glyph g) {
            return g != null && g.Behavior != null && g.Behavior != this;
        }
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnLoseCapture"]/*' />
        /// <devdoc> 
        ///     A behavior can request mouse capture through the behavior service by pushing 
        ///     itself with PushCaptureBehavior.  If it does so, it will be notified through
        ///     OnLoseCapture when capture is lost.  Generally the behavior pops itself at 
        ///     this time.  Capture is lost when one of the following occurs:
        ///
        ///     1.  Someone else requests capture.
        ///     2.  Another behavior is pushed. 
        ///     3.  This behavior is popped.
        /// 
        ///     In each of these cases OnLoseCapture on the behavior will be called. 
        /// </devdoc>
        public virtual void OnLoseCapture(Glyph g, EventArgs e) { 
            if(callParentBehavior && GetNextBehavior != null) {
                GetNextBehavior.OnLoseCapture(g, e);
            } else if (GlyphIsValid(g)) {
                g.Behavior.OnLoseCapture(g, e); 
            }
        } 
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseDown"]/*' />
        /// <devdoc> 
        ///     When any MouseDown message enters the BehaviorService's AdornerWindow
        ///     (nclbuttondown, lbuttondown, rbuttondown, nclrbuttondown) it is first
        ///     passed here, to the top-most Behavior in the BehaviorStack.  Returning
        ///     'true' from this function signifies that the Message was 'handled' by 
        ///     the Behavior and should not continue to be processed.
        /// </devdoc> 
        public virtual bool OnMouseDoubleClick(Glyph g, MouseButtons button, Point mouseLoc) { 
            if(callParentBehavior && GetNextBehavior != null) {
                return GetNextBehavior.OnMouseDoubleClick(g, button, mouseLoc); 
            } else if (GlyphIsValid(g)) {
                return g.Behavior.OnMouseDoubleClick(g, button, mouseLoc);
            } else {
                return false; 
            }
        } 
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseDown"]/*' />
        /// <devdoc> 
        ///     When any MouseDown message enters the BehaviorService's AdornerWindow
        ///     (nclbuttondown, lbuttondown, rbuttondown, nclrbuttondown) it is first
        ///     passed here, to the top-most Behavior in the BehaviorStack.  Returning
        ///     'true' from this function signifies that the Message was 'handled' by 
        ///     the Behavior and should not continue to be processed.
        /// </devdoc> 
        public virtual bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc) { 
            if(callParentBehavior && GetNextBehavior != null) {
                return GetNextBehavior.OnMouseDown(g, button, mouseLoc); 
            } else if (GlyphIsValid(g)) {
                return g.Behavior.OnMouseDown(g, button, mouseLoc);
            } else {
                return false; 
            }
        } 
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseEnter"]/*' />
        /// <devdoc> 
        ///     When the mouse pointer's location is positively hit-tested with a
        ///     different Glyph than previous hit-tests, this event is fired on the
        ///     Behavior associated with the Glyph.
        /// </devdoc> 
        public virtual bool OnMouseEnter(Glyph g) {
            if(callParentBehavior && GetNextBehavior != null) { 
                return GetNextBehavior.OnMouseEnter(g); 
            } else if (GlyphIsValid(g)) {
                return g.Behavior.OnMouseEnter(g); 
            } else {
                return false;
            }
        } 

        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseHover"]/*' /> 
        /// <devdoc> 
        ///     When a MouseHover message enters the BehaviorService's AdornerWindow
        ///     it is first passed here, to the top-most Behavior 
        ///     in the BehaviorStack.  Returning 'true' from this function signifies that
        ///     the Message was 'handled' by the Behavior and should not continue to be processed.
        /// </devdoc>
        public virtual bool OnMouseHover(Glyph g, Point mouseLoc) { 
            if(callParentBehavior && GetNextBehavior != null) {
                return GetNextBehavior.OnMouseHover(g, mouseLoc); 
            } else if (GlyphIsValid(g)) { 
                return g.Behavior.OnMouseHover(g, mouseLoc);
            } else { 
                return false;
            }
        }
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseLeave"]/*' />
        /// <devdoc> 
        ///     When the mouse pointer leaves a positively hit-tested Glyph 
        ///     with a valid Behavior, this method is invoked.
        /// </devdoc> 
        public virtual bool OnMouseLeave(Glyph g) {
            if(callParentBehavior && GetNextBehavior != null) {
                return GetNextBehavior.OnMouseLeave(g);
            } else if (GlyphIsValid(g)) { 
                return g.Behavior.OnMouseLeave(g);
            } else { 
                return false; 
            }
        } 

        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseMove"]/*' />
        /// <devdoc>
        ///     When any MouseMove message enters the BehaviorService's AdornerWindow 
        ///     (mousemove, ncmousemove) it is first passed here, to the top-most Behavior
        ///     in the BehaviorStack.  Returning 'true' from this function signifies that 
        ///     the Message was 'handled' by the Behavior and should not continue to be processed. 
        /// </devdoc>
        public virtual bool OnMouseMove(Glyph g, MouseButtons button, Point mouseLoc) { 
            if(callParentBehavior && GetNextBehavior != null) {
                return GetNextBehavior.OnMouseMove(g, button, mouseLoc);
            } else if (GlyphIsValid(g)) {
                return g.Behavior.OnMouseMove(g, button, mouseLoc); 
            } else {
                return false; 
            } 
        }
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseUp"]/*' />
        /// <devdoc>
        ///     When any MouseUp message enters the BehaviorService's AdornerWindow
        ///     (nclbuttonupown, lbuttonup, rbuttonup, nclrbuttonup) it is first 
        ///     passed here, to the top-most Behavior in the BehaviorStack.  Returning
        ///     'true' from this function signifies that the Message was 'handled' by 
        ///     the Behavior and should not continue to be processed. 
        /// </devdoc>
        public virtual bool OnMouseUp(Glyph g, MouseButtons button) { 
            if(callParentBehavior && GetNextBehavior != null) {
                return GetNextBehavior.OnMouseUp(g, button);
            } else if (GlyphIsValid(g)) {
                return g.Behavior.OnMouseUp(g, button); 
            } else {
                return false; 
            } 
        }
 
        //OLE DragDrop virtual methods
        //

        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnDragDrop"]/*' /> 
        /// <devdoc>
        ///     OnDragDrop can be overridden so that a Behavior can specify its own 
        ///     Drag/Drop rules. 
        /// </devdoc>
        public virtual void OnDragDrop(Glyph g, DragEventArgs e) { 
            if(callParentBehavior && GetNextBehavior != null) {
                GetNextBehavior.OnDragDrop(g, e);
            } else if (GlyphIsValid(g)) {
                g.Behavior.OnDragDrop(g, e); 
            }
        } 
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnDragEnter"]/*' />
        /// <devdoc> 
        ///     OnDragEnter can be overridden so that a Behavior can specify its own
        ///     Drag/Drop rules.
        /// </devdoc>
        public virtual void OnDragEnter(Glyph g, DragEventArgs e) { 
            if(callParentBehavior && GetNextBehavior != null) {
                GetNextBehavior.OnDragEnter(g, e); 
            } else if (GlyphIsValid(g)) { 
                g.Behavior.OnDragEnter(g, e);
            } 
        }

        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnDragLeave"]/*' />
        /// <devdoc> 
        ///     OnDragLeave can be overridden so that a Behavior can specify its own
        ///     Drag/Drop rules. 
        /// </devdoc> 
        public virtual void OnDragLeave(Glyph g, EventArgs e) {
            if(callParentBehavior && GetNextBehavior != null) { 
                GetNextBehavior.OnDragLeave(g, e);
            } else if (GlyphIsValid(g)) {
                g.Behavior.OnDragLeave(g, e);
            } 
        }
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnDragOver"]/*' /> 
        /// <devdoc>
        ///     OnDragOver can be overridden so that a Behavior can specify its own 
        ///     Drag/Drop rules.
        /// </devdoc>
        public virtual void OnDragOver(Glyph g, DragEventArgs e) {
            if(callParentBehavior && GetNextBehavior != null) { 
                GetNextBehavior.OnDragOver(g, e);
            } else if (GlyphIsValid(g)) { 
                g.Behavior.OnDragOver(g, e); 
            } else if (e.Effect != DragDropEffects.None) {
                e.Effect = (Control.ModifierKeys == Keys.Control) ? DragDropEffects.Copy : DragDropEffects.Move; 
            }
        }

        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnGiveFeedback"]/*' /> 
        /// <devdoc>
        ///     OnGiveFeedback can be overridden so that a Behavior can specify its own 
        ///     Drag/Drop rules. 
        /// </devdoc>
        public virtual void OnGiveFeedback(Glyph g, GiveFeedbackEventArgs e) { 
            if(callParentBehavior && GetNextBehavior != null) {
                GetNextBehavior.OnGiveFeedback(g, e);
            } else if (GlyphIsValid(g)) {
                g.Behavior.OnGiveFeedback(g, e); 
            }
        } 
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.QueryContinueDrag"]/*' />
        /// <devdoc> 
        ///     QueryContinueDrag can be overridden so that a Behavior can specify its own
        ///     Drag/Drop rules.
        /// </devdoc>
        public virtual void OnQueryContinueDrag(Glyph g, QueryContinueDragEventArgs e) { 
            if(callParentBehavior && GetNextBehavior != null) {
                GetNextBehavior.OnQueryContinueDrag(g, e); 
            } else if (GlyphIsValid(g)) { 
                g.Behavior.OnQueryContinueDrag(g, e);
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
    using System.Diagnostics.CodeAnalysis; 
    using System.Drawing;
    using System.Windows.Forms.Design; 

    /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior"]/*' />
    /// <devdoc>
    ///     This abstract class represents the Behavior objects that are managed 
    ///     by the BehaviorService.  This class can be extended to develop any
    ///     type of UI 'behavior'.  Ex: selection, drag, and resize behaviors. 
    /// </devdoc> 
    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public abstract class Behavior { 

        private bool callParentBehavior = false;
        private BehaviorService bhvSvc;
 
        protected Behavior() {
        } 
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.Behavior"]/*' />
        /// 
        /// <devdoc>
        /// callParentBehavior - true if the parentBehavior should be called if it exists. The
        /// parentBehavior is the next behavior on the behaviorService stack.
        /// 
        /// If callParentBehavior is true, then behaviorService must be non-null
        /// 
        /// </devdoc> 
        protected Behavior(bool callParentBehavior, BehaviorService behaviorService) {
            if ((callParentBehavior == true) && (behaviorService == null)) { 
                throw new ArgumentException("behaviorService");
            }

            this.callParentBehavior = callParentBehavior; 
            this.bhvSvc = behaviorService;
 
        } 

        private Behavior GetNextBehavior { 
            get {
                if (bhvSvc != null) {
                    return bhvSvc.GetNextBehavior(this);
                } 

                return null; 
            } 
        }
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.Cursor"]/*' />
        /// <devdoc>
        ///     The cursor that should be displayed for this behavior.
        /// </devdoc> 
        public virtual Cursor Cursor {
            get { 
                return Cursors.Default; 
            }
        } 

        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.DisableAllCommands"]/*' />
        /// <devdoc>
        ///     Rerturning true from here indicates to the BehaviorService that 
        ///     all MenuCommands the designer receives should have their
        ///     state set to 'Enabled = false' when this Behavior is active. 
        /// </devdoc> 
        public virtual bool DisableAllCommands {
            get { 
                if(callParentBehavior && GetNextBehavior != null) {
                    return GetNextBehavior.DisableAllCommands;
                } else {
                    return false; 
                }
            } 
        } 

        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.FindCommand"]/*' /> 
        /// <devdoc>
        ///     Called from the BehaviorService, this function provides an opportunity
        ///     for the Behavior to return its own custom MenuCommand thereby
        ///     intercepting this message. 
        /// </devdoc>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
        public virtual MenuCommand FindCommand(CommandID commandId) {
            try 
            {
                if(callParentBehavior && GetNextBehavior != null) {
                    return GetNextBehavior.FindCommand(commandId);
                } else { 
                    return null;
                } 
            } 
            catch //Catch any exception and return null MenuCommand.
            { 
                return null;
            }
        }
 
        /// <devdoc>
        ///     The heuristic we will follow when any of these methods are called 
        ///     is that we will attempt to pass the message along to the glyph. 
        ///     This is a helper method to ensure validity before forwarding the message.
        /// </devdoc> 
        private bool GlyphIsValid(Glyph g) {
            return g != null && g.Behavior != null && g.Behavior != this;
        }
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnLoseCapture"]/*' />
        /// <devdoc> 
        ///     A behavior can request mouse capture through the behavior service by pushing 
        ///     itself with PushCaptureBehavior.  If it does so, it will be notified through
        ///     OnLoseCapture when capture is lost.  Generally the behavior pops itself at 
        ///     this time.  Capture is lost when one of the following occurs:
        ///
        ///     1.  Someone else requests capture.
        ///     2.  Another behavior is pushed. 
        ///     3.  This behavior is popped.
        /// 
        ///     In each of these cases OnLoseCapture on the behavior will be called. 
        /// </devdoc>
        public virtual void OnLoseCapture(Glyph g, EventArgs e) { 
            if(callParentBehavior && GetNextBehavior != null) {
                GetNextBehavior.OnLoseCapture(g, e);
            } else if (GlyphIsValid(g)) {
                g.Behavior.OnLoseCapture(g, e); 
            }
        } 
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseDown"]/*' />
        /// <devdoc> 
        ///     When any MouseDown message enters the BehaviorService's AdornerWindow
        ///     (nclbuttondown, lbuttondown, rbuttondown, nclrbuttondown) it is first
        ///     passed here, to the top-most Behavior in the BehaviorStack.  Returning
        ///     'true' from this function signifies that the Message was 'handled' by 
        ///     the Behavior and should not continue to be processed.
        /// </devdoc> 
        public virtual bool OnMouseDoubleClick(Glyph g, MouseButtons button, Point mouseLoc) { 
            if(callParentBehavior && GetNextBehavior != null) {
                return GetNextBehavior.OnMouseDoubleClick(g, button, mouseLoc); 
            } else if (GlyphIsValid(g)) {
                return g.Behavior.OnMouseDoubleClick(g, button, mouseLoc);
            } else {
                return false; 
            }
        } 
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseDown"]/*' />
        /// <devdoc> 
        ///     When any MouseDown message enters the BehaviorService's AdornerWindow
        ///     (nclbuttondown, lbuttondown, rbuttondown, nclrbuttondown) it is first
        ///     passed here, to the top-most Behavior in the BehaviorStack.  Returning
        ///     'true' from this function signifies that the Message was 'handled' by 
        ///     the Behavior and should not continue to be processed.
        /// </devdoc> 
        public virtual bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc) { 
            if(callParentBehavior && GetNextBehavior != null) {
                return GetNextBehavior.OnMouseDown(g, button, mouseLoc); 
            } else if (GlyphIsValid(g)) {
                return g.Behavior.OnMouseDown(g, button, mouseLoc);
            } else {
                return false; 
            }
        } 
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseEnter"]/*' />
        /// <devdoc> 
        ///     When the mouse pointer's location is positively hit-tested with a
        ///     different Glyph than previous hit-tests, this event is fired on the
        ///     Behavior associated with the Glyph.
        /// </devdoc> 
        public virtual bool OnMouseEnter(Glyph g) {
            if(callParentBehavior && GetNextBehavior != null) { 
                return GetNextBehavior.OnMouseEnter(g); 
            } else if (GlyphIsValid(g)) {
                return g.Behavior.OnMouseEnter(g); 
            } else {
                return false;
            }
        } 

        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseHover"]/*' /> 
        /// <devdoc> 
        ///     When a MouseHover message enters the BehaviorService's AdornerWindow
        ///     it is first passed here, to the top-most Behavior 
        ///     in the BehaviorStack.  Returning 'true' from this function signifies that
        ///     the Message was 'handled' by the Behavior and should not continue to be processed.
        /// </devdoc>
        public virtual bool OnMouseHover(Glyph g, Point mouseLoc) { 
            if(callParentBehavior && GetNextBehavior != null) {
                return GetNextBehavior.OnMouseHover(g, mouseLoc); 
            } else if (GlyphIsValid(g)) { 
                return g.Behavior.OnMouseHover(g, mouseLoc);
            } else { 
                return false;
            }
        }
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseLeave"]/*' />
        /// <devdoc> 
        ///     When the mouse pointer leaves a positively hit-tested Glyph 
        ///     with a valid Behavior, this method is invoked.
        /// </devdoc> 
        public virtual bool OnMouseLeave(Glyph g) {
            if(callParentBehavior && GetNextBehavior != null) {
                return GetNextBehavior.OnMouseLeave(g);
            } else if (GlyphIsValid(g)) { 
                return g.Behavior.OnMouseLeave(g);
            } else { 
                return false; 
            }
        } 

        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseMove"]/*' />
        /// <devdoc>
        ///     When any MouseMove message enters the BehaviorService's AdornerWindow 
        ///     (mousemove, ncmousemove) it is first passed here, to the top-most Behavior
        ///     in the BehaviorStack.  Returning 'true' from this function signifies that 
        ///     the Message was 'handled' by the Behavior and should not continue to be processed. 
        /// </devdoc>
        public virtual bool OnMouseMove(Glyph g, MouseButtons button, Point mouseLoc) { 
            if(callParentBehavior && GetNextBehavior != null) {
                return GetNextBehavior.OnMouseMove(g, button, mouseLoc);
            } else if (GlyphIsValid(g)) {
                return g.Behavior.OnMouseMove(g, button, mouseLoc); 
            } else {
                return false; 
            } 
        }
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnMouseUp"]/*' />
        /// <devdoc>
        ///     When any MouseUp message enters the BehaviorService's AdornerWindow
        ///     (nclbuttonupown, lbuttonup, rbuttonup, nclrbuttonup) it is first 
        ///     passed here, to the top-most Behavior in the BehaviorStack.  Returning
        ///     'true' from this function signifies that the Message was 'handled' by 
        ///     the Behavior and should not continue to be processed. 
        /// </devdoc>
        public virtual bool OnMouseUp(Glyph g, MouseButtons button) { 
            if(callParentBehavior && GetNextBehavior != null) {
                return GetNextBehavior.OnMouseUp(g, button);
            } else if (GlyphIsValid(g)) {
                return g.Behavior.OnMouseUp(g, button); 
            } else {
                return false; 
            } 
        }
 
        //OLE DragDrop virtual methods
        //

        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnDragDrop"]/*' /> 
        /// <devdoc>
        ///     OnDragDrop can be overridden so that a Behavior can specify its own 
        ///     Drag/Drop rules. 
        /// </devdoc>
        public virtual void OnDragDrop(Glyph g, DragEventArgs e) { 
            if(callParentBehavior && GetNextBehavior != null) {
                GetNextBehavior.OnDragDrop(g, e);
            } else if (GlyphIsValid(g)) {
                g.Behavior.OnDragDrop(g, e); 
            }
        } 
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnDragEnter"]/*' />
        /// <devdoc> 
        ///     OnDragEnter can be overridden so that a Behavior can specify its own
        ///     Drag/Drop rules.
        /// </devdoc>
        public virtual void OnDragEnter(Glyph g, DragEventArgs e) { 
            if(callParentBehavior && GetNextBehavior != null) {
                GetNextBehavior.OnDragEnter(g, e); 
            } else if (GlyphIsValid(g)) { 
                g.Behavior.OnDragEnter(g, e);
            } 
        }

        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnDragLeave"]/*' />
        /// <devdoc> 
        ///     OnDragLeave can be overridden so that a Behavior can specify its own
        ///     Drag/Drop rules. 
        /// </devdoc> 
        public virtual void OnDragLeave(Glyph g, EventArgs e) {
            if(callParentBehavior && GetNextBehavior != null) { 
                GetNextBehavior.OnDragLeave(g, e);
            } else if (GlyphIsValid(g)) {
                g.Behavior.OnDragLeave(g, e);
            } 
        }
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnDragOver"]/*' /> 
        /// <devdoc>
        ///     OnDragOver can be overridden so that a Behavior can specify its own 
        ///     Drag/Drop rules.
        /// </devdoc>
        public virtual void OnDragOver(Glyph g, DragEventArgs e) {
            if(callParentBehavior && GetNextBehavior != null) { 
                GetNextBehavior.OnDragOver(g, e);
            } else if (GlyphIsValid(g)) { 
                g.Behavior.OnDragOver(g, e); 
            } else if (e.Effect != DragDropEffects.None) {
                e.Effect = (Control.ModifierKeys == Keys.Control) ? DragDropEffects.Copy : DragDropEffects.Move; 
            }
        }

        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.OnGiveFeedback"]/*' /> 
        /// <devdoc>
        ///     OnGiveFeedback can be overridden so that a Behavior can specify its own 
        ///     Drag/Drop rules. 
        /// </devdoc>
        public virtual void OnGiveFeedback(Glyph g, GiveFeedbackEventArgs e) { 
            if(callParentBehavior && GetNextBehavior != null) {
                GetNextBehavior.OnGiveFeedback(g, e);
            } else if (GlyphIsValid(g)) {
                g.Behavior.OnGiveFeedback(g, e); 
            }
        } 
 
        /// <include file='doc\Behavior.uex' path='docs/doc[@for="Behavior.QueryContinueDrag"]/*' />
        /// <devdoc> 
        ///     QueryContinueDrag can be overridden so that a Behavior can specify its own
        ///     Drag/Drop rules.
        /// </devdoc>
        public virtual void OnQueryContinueDrag(Glyph g, QueryContinueDragEventArgs e) { 
            if(callParentBehavior && GetNextBehavior != null) {
                GetNextBehavior.OnQueryContinueDrag(g, e); 
            } else if (GlyphIsValid(g)) { 
                g.Behavior.OnQueryContinueDrag(g, e);
            } 
        }

    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
