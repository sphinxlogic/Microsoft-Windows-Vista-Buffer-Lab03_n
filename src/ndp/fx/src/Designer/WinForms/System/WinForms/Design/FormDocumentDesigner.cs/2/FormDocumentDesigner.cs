//------------------------------------------------------------------------------ 
// <copyright file="FormDocumentDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
 
    using System.Design;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using System.Collections;
    using System.ComponentModel.Design; 
    using System.Windows.Forms; 
    using System.Windows.Forms.Design;
    using System.Drawing; 
    using Microsoft.Win32;
    using System.Drawing.Design;
    using System.Windows.Forms.Design.Behavior;
    using System.Globalization; 

    /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner"]/*' /> 
    /// <devdoc> 
    ///      The FormDocumentDesigner class builds on the DocumentDesigner.  It adds shadowing
    ///      for form properties that need to be shadowed and it also adds logic to properly 
    ///      paint the form's title bar to match the active document window.
    /// </devdoc>
    internal class FormDocumentDesigner : DocumentDesigner {
        private Size autoScaleBaseSize = Size.Empty; 
        private bool inAutoscale = false;
        private int  heightDelta = 0; 
        private bool                isMenuInherited;       //indicates if the 'active menu' is inherited 
        private bool hasMenu = false;
        private InheritanceAttribute inheritanceAttribute; 
        private bool initializing = false;
        private bool autoSize = false;

        private ToolStripAdornerWindowService toolStripAdornerWindowService = null; 

        /// <devdoc> 
        ///     Shadow the AcceptButton property at design-time so that we 
        ///     can preserve it when the form is rebuilt.  Otherwise, form.Controls.Clear()
        ///     will clear it out when we don't want it to. 
        /// </devdoc>
        private IButtonControl AcceptButton {
            get {
                return ShadowProperties["AcceptButton"] as IButtonControl; 
            }
            set { 
                ((Form)Component).AcceptButton = value; 
                ShadowProperties["AcceptButton"] = value;
            } 
        }

        /// <devdoc>
        ///     Shadow the CancelButton property at design-time so that we 
        ///     can preserve it when the form is rebuilt.  Otherwise, form.Controls.Clear()
        ///     will clear it out when we don't want it to. 
        /// </devdoc> 
        private IButtonControl CancelButton {
            get { 
                return ShadowProperties["CancelButton"] as IButtonControl;
            }
            set {
                ((Form)Component).CancelButton = value; 
                ShadowProperties["CancelButton"] = value;
            } 
        } 

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.AutoScaleBaseSize"]/*' /> 
        /// <devdoc>
        ///      Shadowed version of the AutoScaleBaseSize property.  We shadow this
        ///      so that it always persists.  Normally only properties that differ
        ///      from the default values at instantiation are persisted, but this 
        ///      should always be written.  So, we shadow it and add our own
        ///      ShouldSerialize method. 
        /// </devdoc> 
        private Size AutoScaleBaseSize {
            get { 
                // we don't want to get inherited value from a base form
                // that might have been designed in a different DPI
                // so we recalculate the thing instead of getting
                // AutoScaleBaseSize (QFE 2280) 
#pragma warning disable 618
                SizeF real = Form.GetAutoScaleSize(((Form)Component).Font); 
#pragma warning restore 618 
                return new Size((int)Math.Round(real.Width), (int)Math.Round(real.Height));
                //return ((Form)Component).AutoScaleBaseSize; 
            }

            set {
                // We do nothing at design time for this property; we always want 
                // to use the calculated value from the component.
                autoScaleBaseSize = value; 
                ShadowProperties["AutoScaleBaseSize"] = value; 
            }
        } 

        /// <devdoc>
        ///     We shadow the AutoSize property at design-time so that
        ///     the form doesn't grow and shrink as users fiddle with 
        ///     autosize related properties.
        /// </devdoc> 
        private bool AutoSize { 
            get {
                return autoSize; 
            }
            set {
                autoSize = value;
            } 
        }
 
        private bool ShouldSerializeAutoScaleBaseSize() 
        {
            // Never serialize this unless AutoScale is turned 
            // on
#pragma warning disable 618
            return initializing ? false
                : ((Form)Component).AutoScale && ShadowProperties.Contains("AutoScaleBaseSize"); 
#pragma warning restore 618
        } 
 
        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.ClientSize"]/*' />
        /// <devdoc> 
        ///      Shadow property for the ClientSize property -- this allows us to intercept client size changes
        ///      and apply the new menu height if necessary
        /// </devdoc>
        private Size ClientSize { 
            get {
               if (initializing) { 
                   return new Size(-1, -1); 
               }
               else { 
                   Size size = new Size(-1,-1);
                   Form form = Component as Form;
                   if (form != null)  {
                       size = form.ClientSize; 

                       // don't report the size decremented by the scroll bars, otherwise, we'll just lose 
                       // that size when we run because the form doesn't take that into consideration 
                       // (it's too early, it hasn't layed out and doesn't know it needs scrollbars)
                       // when sizing. 
                       ///
                       if (form.HorizontalScroll.Visible) {
                           size.Height += SystemInformation.HorizontalScrollBarHeight;
                       } 

                       if (form.VerticalScroll.Visible) { 
                           size.Width += SystemInformation.VerticalScrollBarWidth; 
                       }
                   } 

                   return size;
               }
            } 
            set {
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
 
                if (host != null) {
                    if (host.Loading) { 

                        heightDelta = GetMenuHeight();
                    }
                } 
                ((Form)Component).ClientSize = value;
            } 
        } 

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.IsMdiContainer"]/*' /> 
        /// <devdoc>
        ///      Shadow property for the IsMDIContainer property on a form.
        /// </devdoc>
        private bool IsMdiContainer { 
            get {
                return((Form)Control).IsMdiContainer; 
            } 
            set {
                if (!value) { 
                    UnhookChildControls(Control);
                }
                ((Form)Control).IsMdiContainer = value;
                if (value) { 
                    HookChildControls(Control);
                } 
            } 
        }
 
        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.IsMenuInherited"]/*' />
        /// <devdoc>
        ///      Returns true if the active menu is an inherited component.  We use this to determine if we
        ///      we need to resize the base control or not. 
        /// </devdoc>
        private bool IsMenuInherited { 
            get { 
                if (inheritanceAttribute == null && Menu != null) {
                    inheritanceAttribute = (InheritanceAttribute)TypeDescriptor.GetAttributes(Menu)[typeof(InheritanceAttribute)]; 
                    if (inheritanceAttribute.Equals(InheritanceAttribute.NotInherited)) {
                        isMenuInherited = false;
                    }
                    else { 
                        isMenuInherited = true;
                    } 
                } 
                return isMenuInherited;
            } 
        }


        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.Menu"]/*' /> 
        /// <devdoc>
        ///     Accessor method for the menu property on control.  We shadow 
        ///     this property at design time. 
        /// </devdoc>
        internal MainMenu Menu { 
            get {
                return (MainMenu)ShadowProperties["Menu"];
            }
 
            set {
                if (value == ShadowProperties["Menu"]) { 
                    return; 
                }
 
                ShadowProperties["Menu"] = value;

                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
 
                if (host != null && !host.Loading) {
                    EnsureMenuEditorService(value); 
                    if (menuEditorService != null) 
                       menuEditorService.SetMenu(value);
 
                }

                if (heightDelta == 0) {
                    heightDelta = GetMenuHeight(); 
                }
            } 
        } 

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.Opacity"]/*' /> 
        /// <devdoc>
        ///     Opacity property on control.  We shadow this property at design time.
        /// </devdoc>
        private double Opacity { 
            get {
                return (double)ShadowProperties["Opacity"]; 
            } 
            set {
                if (value < 0.0f || value > 1.0f) { 
                    throw new ArgumentException(SR.GetString(SR.InvalidBoundArgument,
                                                                    "value",
                                                                    value.ToString(CultureInfo.CurrentCulture),
                                                                    (0.0f).ToString(CultureInfo.CurrentCulture), 
                                                                    (1.0f).ToString(CultureInfo.CurrentCulture)),
                                                                    "value"); 
                } 
                ShadowProperties["Opacity"] = value;
            } 
        }

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.SnapLines"]/*' />
        /// <devdoc> 
        ///     Overrides the default implementation of ParentControlDesigner SnapLines.  Note that if the Padding
        ///     property is not set on our Form - we'll special case this and add default Padding values to our 
        ///     SnapLines.  This was a usability request specific to the Form itself. 
        ///
        ///     Note that a Form only has Padding SnapLines. 
        ///
        /// </devdoc>
        public override IList SnapLines {
            get { 

                ArrayList snapLines = null; 
                base.AddPaddingSnapLines(ref snapLines); 
                if (snapLines == null) {
                    Debug.Fail("why did base.AddPaddingSnapLines return null?"); 
                    snapLines = new ArrayList(4);
                }

                //if the padding has not been set - then we'll auto-add padding 
                //to form - this is a Usability request
                // 
                if (Control.Padding == Padding.Empty && snapLines != null) { 

                    int paddingsFound = 0;//used to short-circuit once we find 4 paddings 

                    for(int i = 0; i < snapLines.Count; i++) {
                        // remove previous padding snaplines
                        SnapLine snapLine = snapLines[i] as SnapLine; 

                        if(snapLine != null && snapLine.Filter != null && snapLine.Filter.StartsWith(SnapLine.Padding)) { 
                            if (snapLine.Filter.Equals(SnapLine.PaddingLeft) || snapLine.Filter.Equals(SnapLine.PaddingTop)) { 
                                snapLine.AdjustOffset(DesignerUtils.DEFAULTFORMPADDING);
                                paddingsFound ++; 
                            }

                            if (snapLine.Filter.Equals(SnapLine.PaddingRight) || snapLine.Filter.Equals(SnapLine.PaddingBottom)) {
                                snapLine.AdjustOffset(-DesignerUtils.DEFAULTFORMPADDING); 
                                paddingsFound ++;
                            } 
 
                            if (paddingsFound == 4) {
                                break;//we adjusted all of our paddings 
                            }
                        }
                    }
                } 

                return snapLines; 
            } 
        }
 

        private Size Size {
            get {
                return Control.Size; 
            }
            set { 
                IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService)); 
                PropertyDescriptorCollection props = TypeDescriptor.GetProperties(Component);
                if (cs != null) { 
                    cs.OnComponentChanging(Component, props["ClientSize"]);
                }

                Control.Size = value; 

                if (cs != null) { 
                    cs.OnComponentChanged(Component, props["ClientSize"], null, null); 
                }
            } 
        }

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.ShowInTaskbar"]/*' />
        /// <devdoc> 
        ///     Accessor method for the showInTaskbar property on control.  We shadow
        ///     this property at design time. 
        /// </devdoc> 
        private bool ShowInTaskbar {
            get { 
                return (bool)ShadowProperties["ShowInTaskbar"];
            }
            set {
                ShadowProperties["ShowInTaskbar"] = value; 
            }
        } 
 
        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.WindowState"]/*' />
        /// <devdoc> 
        ///     Accessor method for the windowState property on control.  We shadow
        ///     this property at design time.
        /// </devdoc>
        private FormWindowState WindowState { 
            get {
                return (FormWindowState)ShadowProperties["WindowState"]; 
            } 
            set {
                ShadowProperties["WindowState"] = value; 
            }
        }

        private void ApplyAutoScaling(SizeF baseVar, Form form) { 

            // We also don't do this if the property is empty.  Otherwise we will perform 
            // two GetAutoScaleBaseSize calls only to find that they returned the same 
            // value.
            // 
            if (!baseVar.IsEmpty)
            {
#pragma warning disable 618
                SizeF newVarF = Form.GetAutoScaleSize(form.Font); 
#pragma warning restore 618
                Size newVar = new Size((int)Math.Round(newVarF.Width), (int)Math.Round(newVarF.Height)); 
 
                // We save a significant amount of time by bailing early if there's no work to be done
                if (baseVar.Equals(newVar)) 
                    return;

                float percY = ((float)newVar.Height) / ((float)baseVar.Height);
                float percX = ((float)newVar.Width) / ((float)baseVar.Width); 
                try {
                    inAutoscale = true; 
#pragma warning disable 618 
                    form.Scale(percX, percY);
#pragma warning restore 618 
                }
                finally {
                    inAutoscale = false;
                } 
            }
        } 
 

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.Dispose"]/*' /> 
        /// <devdoc>
        ///     Disposes of this designer.
        /// </devdoc>
        protected override void Dispose(bool disposing) { 
            if (disposing) {
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                Debug.Assert(host != null, "Must have a designer host on dispose"); 

                if (host != null) { 
                    host.LoadComplete -= new EventHandler(OnLoadComplete);
                    host.Activated -= new EventHandler(OnDesignerActivate);
                    host.Deactivated -= new EventHandler(OnDesignerDeactivate);
                } 

                IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService)); 
                if (cs != null) { 
                    cs.ComponentAdded -= new ComponentEventHandler(this.OnComponentAdded);
                    cs.ComponentRemoved -= new ComponentEventHandler(this.OnComponentRemoved); 
                }
            }
            base.Dispose(disposing);
        } 

        internal override void DoProperMenuSelection(ICollection selComponents) { 
            foreach(object obj in selComponents) { 
                //first check to see if our selection is any kind of menu: main, context, item
                // AND the designer for the component is this one 
                //
                Menu menu = obj as Menu;
                if (menu != null) {
                    //if it's a menu item, set the selection 
                    MenuItem item = menu as MenuItem;
                    if (item != null) { 
                        Menu currentMenu = menuEditorService.GetMenu(); 
                        //before we set the selection, we need to check if the item belongs the current menu,
                        //if not, we need to set the menu editor to the appropiate menu, then set selection 
                        //
                        MenuItem parent = item;
                        while (parent.Parent is MenuItem) {
                            parent = (MenuItem)parent.Parent; 
                        }
 
                        if( !(currentMenu == parent.Parent) ) { 
                            menuEditorService.SetMenu(parent.Parent);
                        } 

                        //ok, here we have the correct editor selected for this item.
                        //Now, if there's only one item selected, then let the editor service know,
                        //if there is more than one - then the selection was done through the 
                        //menu editor and we don't need to tell it
                        if(selComponents.Count == 1) { 
                            menuEditorService.SetSelection(item); 
                        }
                    } 
                    //here, either it's a main or context menu, even if the menu is the current one,
                    //we still want to call this "SetMenu" method, 'cause that'll collapse it and
                    //remove the focus
                    // 
                    else {
                        menuEditorService.SetMenu(menu); 
                    } 
                    return;
                } 
                //Here, something is selected, but it is in no way, shape, or form a menu
                //so, we'll collapse our active menu accordingly
                else {
                    if (Menu != null && Menu.MenuItems.Count == 0) { 
                        menuEditorService.SetMenu(null);
                    } 
                    else { 
                        menuEditorService.SetMenu(Menu);
                    } 
                    NativeMethods.SendMessage(Control.Handle, NativeMethods.WM_NCACTIVATE, 1, 0);
                }
            }
        } 

        /// <devdoc> 
        ///      Determines if a MenuEditorService has already been started.  If not, 
        ///      this method will create a new instance of the service.  We override
        ///      this because we want to allow any kind of menu to start the service, 
        ///      not just ContextMenus.
        /// </devdoc>
        protected override void EnsureMenuEditorService(IComponent c) {
            if (menuEditorService == null && c is Menu) { 
                menuEditorService = (IMenuEditorService)GetService(typeof(IMenuEditorService));
            } 
        } 

         private void EnsureToolStripWindowAdornerService() { 
             if (toolStripAdornerWindowService == null) {
                toolStripAdornerWindowService = (ToolStripAdornerWindowService)GetService(typeof(ToolStripAdornerWindowService));
            }
         } 

        /// <devdoc> 
        /// Gets the current menu height so we know how much to increment the form size by 
        /// </devdoc>
        private int GetMenuHeight() { 

            if (Menu == null || (IsMenuInherited && initializing)) {
                return 0;
            } 

            if (menuEditorService != null) { 
                // there is a magic property on teh menueditorservice that gives us this 
                // information.  Unfortuantely, we can't compute it ourselves -- the menu
                // shown in the designer isn't a windows one so we can't ask windows. 
                //
                PropertyDescriptor heightProp = TypeDescriptor.GetProperties(menuEditorService)["MenuHeight"];
                if (heightProp != null) {
                    int height = (int)heightProp.GetValue(menuEditorService); 
                    return height;
                } 
            } 
            return SystemInformation.MenuHeight;
        } 

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.Initialize"]/*' />
        /// <devdoc>
        ///     Initializes the designer with the given component.  The designer can 
        ///     get the component's site and request services from it in this call.
        /// </devdoc> 
        public override void Initialize(IComponent component) { 
            // We have to shadow the WindowState before we call base.Initialize
            PropertyDescriptor windowStateProp = TypeDescriptor.GetProperties(component.GetType())["WindowState"]; 
            if (windowStateProp != null && windowStateProp.PropertyType == typeof(FormWindowState))
            {
                WindowState = (FormWindowState)windowStateProp.GetValue(component);
            } 

            initializing = true; 
            base.Initialize(component); 
            initializing = false;
 
            AutoResizeHandles = true;

            Debug.Assert(component is Form, "FormDocumentDesigner expects its component to be a form.");
 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (host != null) { 
                host.LoadComplete += new EventHandler(OnLoadComplete); 
                host.Activated += new EventHandler(OnDesignerActivate);
                host.Deactivated += new EventHandler(OnDesignerDeactivate); 
            }

            Form form = (Form)Control;
            form.WindowState = FormWindowState.Normal; 
            ShadowProperties["AcceptButton"] = form.AcceptButton;
            ShadowProperties["CancelButton"] = form.CancelButton; 
 
            // Monitor component/remove add events for our tray
            // 
            IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            if (cs != null) {
                cs.ComponentAdded += new ComponentEventHandler(this.OnComponentAdded);
                cs.ComponentRemoved += new ComponentEventHandler(this.OnComponentRemoved); 
            }
        } 
 
        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.OnComponentAdded"]/*' />
        /// <devdoc> 
        ///      Called when a component is added to the design container.
        ///      If the component isn't a control, this will demand create
        ///      the component tray and add the component to it.
        /// </devdoc> 
        private void OnComponentAdded(object source, ComponentEventArgs ce) {
            if (ce.Component is Menu) { 
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                if (host != null && !host.Loading) {
                    //if it's a MainMenu & we don't have one set for the form yet, then do it... 
                    //
                    if(ce.Component is MainMenu && !hasMenu) {
                        PropertyDescriptor menuProp = TypeDescriptor.GetProperties(Component)["Menu"];
                        Debug.Assert(menuProp != null, "What happened to the Menu property"); 
                        menuProp.SetValue(Component, ce.Component);
                        hasMenu = true; 
                    } 
                }
            } 
            if(ce.Component is ToolStrip && toolStripAdornerWindowService == null) {
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                if (host != null) {
                    EnsureToolStripWindowAdornerService(); 
                }
 
 
            }
        } 

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.OnComponentRemoved"]/*' />
        /// <devdoc>
        ///      Called when a component is removed from the design container. 
        ///      Here, we check if a menu is being removed and handle removing
        ///      the Form's mainmenu vs. other menus properly. 
        /// </devdoc> 
        private void OnComponentRemoved(object source, ComponentEventArgs ce) {
            if (ce.Component is Menu) { 
                //if we deleted the form's mainmenu, set it null...
                if (ce.Component == Menu) {
                    PropertyDescriptor menuProp = TypeDescriptor.GetProperties(Component)["Menu"];
                    Debug.Assert(menuProp != null, "What happened to the Menu property"); 
                    menuProp.SetValue(Component, null);
                    hasMenu = false; 
                } 
                else if (menuEditorService != null && ce.Component == menuEditorService.GetMenu()) {
                    menuEditorService.SetMenu(Menu); 
                }
            }
            if (ce.Component is ToolStrip && toolStripAdornerWindowService != null) {
                toolStripAdornerWindowService = null; 
            }
            if (ce.Component is IButtonControl) { 
                if (ce.Component == ShadowProperties["AcceptButton"]) { 
                    this.AcceptButton = null;
                } 
                if (ce.Component == ShadowProperties["CancelButton"]) {
                    this.CancelButton = null;
                }
            } 
        }
 
        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.OnCreateHandle"]/*' /> 
        /// <devdoc>
        ///      We're watching the handle creation in case we have a menu editor. 
        ///      If we do, the menu editor will have to be torn down and recreated.
        /// </devdoc>
        protected override void OnCreateHandle() {
            if (Menu != null && menuEditorService !=null) { 
                menuEditorService.SetMenu(null);
                menuEditorService.SetMenu(Menu); 
            } 

            // VSWhidbey 366241 -- in case we get a handle recreate, we need to re-apply the delta. 
            //
            if (heightDelta != 0)
            {
                ((Form)Component).Height += heightDelta; 
                heightDelta = 0;
            } 
        } 

        // <doc> 
        // <desc>
        //      Called when our document becomes active.  We paint our form's
        //      border the appropriate color here.
        // </desc> 
        // </doc>
        // 
        private void OnDesignerActivate(object source, EventArgs evevent) { 
            // Paint the form's title bar UI-active
            // 
            Control control = Control;

            if (control != null && control.IsHandleCreated) {
                NativeMethods.SendMessage(control.Handle, NativeMethods.WM_NCACTIVATE, 1, 0); 
                SafeNativeMethods.RedrawWindow(control.Handle, null, IntPtr.Zero, NativeMethods.RDW_FRAME);
            } 
        } 

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.OnDesignerDeactivate"]/*' /> 
        /// <devdoc>
        ///     Called by the host when we become inactive.  Here we update the
        ///     title bar of our form so it's the inactive color.
        /// </devdoc> 
        private void OnDesignerDeactivate(object sender, EventArgs e) {
            Control control = Control; 
            if (control != null && control.IsHandleCreated) { 
                NativeMethods.SendMessage(control.Handle, NativeMethods.WM_NCACTIVATE, 0, 0);
                SafeNativeMethods.RedrawWindow(control.Handle, null, IntPtr.Zero, NativeMethods.RDW_FRAME); 
            }
        }

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.OnLoadComplete"]/*' /> 
        /// <devdoc>
        ///      Called when our code loads.  Here we connect us as the selection 
        ///      UI handler for ourselves.  This is a special case because for 
        ///      the top level document, we are our own selection UI handler.
        /// </devdoc> 
        private void OnLoadComplete(object source, EventArgs evevent) {
            Form form = Control as Form;
            if (form != null) {
 
                // VSWhidbey #146082
                // The form's ClientSize is reported 
                // including the ScrollBar's height. We 
                // need to account for this in order to display the form with
                // scrollbars correctly. 
                int clientWidth = form.ClientSize.Width;
                int clientHeight = form.ClientSize.Height;
                if (form.HorizontalScroll.Visible && form.AutoScroll) {
                    clientHeight += SystemInformation.HorizontalScrollBarHeight; 
                }
                if (form.VerticalScroll.Visible && form.AutoScroll) { 
                    clientWidth += SystemInformation.VerticalScrollBarWidth; 
                }
 
                // ApplyAutoScaling causes WmWindowPosChanging to be called
                // and there we calculate if we need to compensate for a menu being visible
                // we were causing that calculation to fail if we set ClientSize too early.
                // we now do the right thing AND check again if we need to compensate for 
                // the menu.
                ApplyAutoScaling(autoScaleBaseSize, form); 
 
                this.ClientSize = new Size(clientWidth, clientHeight);
 
                BehaviorService svc = (BehaviorService)GetService( typeof(BehaviorService) );
                if (svc != null) {
                    svc.SyncSelection();
                } 

                // if there is a menu and we need to update our height because of it, 
                // do it now. 
                //
 
                if (heightDelta == 0) {
                    heightDelta = GetMenuHeight();
                }
 
                if (heightDelta != 0) {
                    form.Height += heightDelta; 
                    heightDelta = 0; 
                }
 
                // VSWhidbey #281912
                // After loading the form if the ControlBox and ShowInTaskbar properties are false,
                // the form will be sized incorrectly.  This is due to the text property being set
                // after the ControlBox and ShowInTaskbar properties, which causes windows to recalculate 
                // our client area wrong.  The reason it does this is because after setting the ShowInTaskbar
                // and ControlBox it assumes we have no titlebar, and bases the clientSize we pass it on that. 
                // In reality our ClientSize DOES depend on having a titlebar, so windows gets confused. 
                // This only happens at designtime, because at runtime our special DesignTime only MainMenu
                // is not around to mess things up.  Because of this, I'm adding this nasty workaround to 
                // correctly update the height at design time.
                if (!form.ControlBox && !form.ShowInTaskbar && !string.IsNullOrEmpty(form.Text) && Menu != null && !IsMenuInherited) {
                    form.Height += SystemInformation.CaptionHeight + 1;
                } 

                // To work around 444699 and fix 94431, we will force a layout. 
                form.PerformLayout(); 

            } 

        }

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.PreFilterProperties"]/*' /> 
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
            //
            string[] shadowProps = new string[] {
                "Opacity", 
                "Menu",
                "IsMdiContainer", 
                "Size", 
                "ShowInTaskBar",
                "WindowState", 
                "AutoSize",
                "AcceptButton",
                "CancelButton"
            }; 

            Attribute[] empty = new Attribute[0]; 
 
            for (int i = 0; i < shadowProps.Length; i++) {
                prop = (PropertyDescriptor)properties[shadowProps[i]]; 
                if (prop != null) {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(FormDocumentDesigner), prop, empty);
                }
            } 

            // Mark auto scale base size as serializable again so we can 
            // monitor it for backwards compat. 
            prop = (PropertyDescriptor)properties["AutoScaleBaseSize"];
            if (prop != null) { 
                properties["AutoScaleBaseSize"] = TypeDescriptor.CreateProperty(typeof(FormDocumentDesigner), prop, DesignerSerializationVisibilityAttribute.Visible);
            }

            // And set the new default value attribute for client base size, and shadow it as well. 
            //
            prop = (PropertyDescriptor)properties["ClientSize"]; 
            if (prop != null) { 
                properties["ClientSize"] = TypeDescriptor.CreateProperty(typeof(FormDocumentDesigner), prop, new DefaultValueAttribute(new Size(-1, -1)));
            } 
        }

        /// <devdoc>
        ///     Handles the WM_WINDOWPOSCHANGING message 
        /// </devdoc>
        /// <internalonly/> 
        private unsafe void WmWindowPosChanging(ref Message m) { 

            NativeMethods.WINDOWPOS* wp = (NativeMethods.WINDOWPOS *)m.LParam; 


            bool updateSize = inAutoscale;
 
            if (!updateSize) {
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
 
                if (host != null) {
                    updateSize = host.Loading; 
                }
            }

 
            // we want to update the size if we have a menu and...
            // 1) we're doing an autoscale 
            // 2) we're loading a form without an inherited menu (inherited forms will already have the right size) 
            //
            if (updateSize && Menu != null && (wp->flags & NativeMethods.SWP_NOSIZE) == 0 && (IsMenuInherited || inAutoscale)) { 
                heightDelta = GetMenuHeight();
            }
        }
 
                 /// <include file='doc\DocumentDesigner.uex' path='docs/doc[@for="DocumentDesigner.WndProc"]/*' />
        /// <devdoc> 
        ///      Overrides our base class WndProc to provide support for 
        ///      the menu editor service.
        /// </devdoc> 
        protected override void WndProc(ref Message m) {
            switch (m.Msg) {
                case NativeMethods.WM_WINDOWPOSCHANGING:
                    WmWindowPosChanging(ref m); 
                    break;
            } 
            base.WndProc(ref m); 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="FormDocumentDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
 
    using System.Design;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using System.Collections;
    using System.ComponentModel.Design; 
    using System.Windows.Forms; 
    using System.Windows.Forms.Design;
    using System.Drawing; 
    using Microsoft.Win32;
    using System.Drawing.Design;
    using System.Windows.Forms.Design.Behavior;
    using System.Globalization; 

    /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner"]/*' /> 
    /// <devdoc> 
    ///      The FormDocumentDesigner class builds on the DocumentDesigner.  It adds shadowing
    ///      for form properties that need to be shadowed and it also adds logic to properly 
    ///      paint the form's title bar to match the active document window.
    /// </devdoc>
    internal class FormDocumentDesigner : DocumentDesigner {
        private Size autoScaleBaseSize = Size.Empty; 
        private bool inAutoscale = false;
        private int  heightDelta = 0; 
        private bool                isMenuInherited;       //indicates if the 'active menu' is inherited 
        private bool hasMenu = false;
        private InheritanceAttribute inheritanceAttribute; 
        private bool initializing = false;
        private bool autoSize = false;

        private ToolStripAdornerWindowService toolStripAdornerWindowService = null; 

        /// <devdoc> 
        ///     Shadow the AcceptButton property at design-time so that we 
        ///     can preserve it when the form is rebuilt.  Otherwise, form.Controls.Clear()
        ///     will clear it out when we don't want it to. 
        /// </devdoc>
        private IButtonControl AcceptButton {
            get {
                return ShadowProperties["AcceptButton"] as IButtonControl; 
            }
            set { 
                ((Form)Component).AcceptButton = value; 
                ShadowProperties["AcceptButton"] = value;
            } 
        }

        /// <devdoc>
        ///     Shadow the CancelButton property at design-time so that we 
        ///     can preserve it when the form is rebuilt.  Otherwise, form.Controls.Clear()
        ///     will clear it out when we don't want it to. 
        /// </devdoc> 
        private IButtonControl CancelButton {
            get { 
                return ShadowProperties["CancelButton"] as IButtonControl;
            }
            set {
                ((Form)Component).CancelButton = value; 
                ShadowProperties["CancelButton"] = value;
            } 
        } 

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.AutoScaleBaseSize"]/*' /> 
        /// <devdoc>
        ///      Shadowed version of the AutoScaleBaseSize property.  We shadow this
        ///      so that it always persists.  Normally only properties that differ
        ///      from the default values at instantiation are persisted, but this 
        ///      should always be written.  So, we shadow it and add our own
        ///      ShouldSerialize method. 
        /// </devdoc> 
        private Size AutoScaleBaseSize {
            get { 
                // we don't want to get inherited value from a base form
                // that might have been designed in a different DPI
                // so we recalculate the thing instead of getting
                // AutoScaleBaseSize (QFE 2280) 
#pragma warning disable 618
                SizeF real = Form.GetAutoScaleSize(((Form)Component).Font); 
#pragma warning restore 618 
                return new Size((int)Math.Round(real.Width), (int)Math.Round(real.Height));
                //return ((Form)Component).AutoScaleBaseSize; 
            }

            set {
                // We do nothing at design time for this property; we always want 
                // to use the calculated value from the component.
                autoScaleBaseSize = value; 
                ShadowProperties["AutoScaleBaseSize"] = value; 
            }
        } 

        /// <devdoc>
        ///     We shadow the AutoSize property at design-time so that
        ///     the form doesn't grow and shrink as users fiddle with 
        ///     autosize related properties.
        /// </devdoc> 
        private bool AutoSize { 
            get {
                return autoSize; 
            }
            set {
                autoSize = value;
            } 
        }
 
        private bool ShouldSerializeAutoScaleBaseSize() 
        {
            // Never serialize this unless AutoScale is turned 
            // on
#pragma warning disable 618
            return initializing ? false
                : ((Form)Component).AutoScale && ShadowProperties.Contains("AutoScaleBaseSize"); 
#pragma warning restore 618
        } 
 
        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.ClientSize"]/*' />
        /// <devdoc> 
        ///      Shadow property for the ClientSize property -- this allows us to intercept client size changes
        ///      and apply the new menu height if necessary
        /// </devdoc>
        private Size ClientSize { 
            get {
               if (initializing) { 
                   return new Size(-1, -1); 
               }
               else { 
                   Size size = new Size(-1,-1);
                   Form form = Component as Form;
                   if (form != null)  {
                       size = form.ClientSize; 

                       // don't report the size decremented by the scroll bars, otherwise, we'll just lose 
                       // that size when we run because the form doesn't take that into consideration 
                       // (it's too early, it hasn't layed out and doesn't know it needs scrollbars)
                       // when sizing. 
                       ///
                       if (form.HorizontalScroll.Visible) {
                           size.Height += SystemInformation.HorizontalScrollBarHeight;
                       } 

                       if (form.VerticalScroll.Visible) { 
                           size.Width += SystemInformation.VerticalScrollBarWidth; 
                       }
                   } 

                   return size;
               }
            } 
            set {
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
 
                if (host != null) {
                    if (host.Loading) { 

                        heightDelta = GetMenuHeight();
                    }
                } 
                ((Form)Component).ClientSize = value;
            } 
        } 

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.IsMdiContainer"]/*' /> 
        /// <devdoc>
        ///      Shadow property for the IsMDIContainer property on a form.
        /// </devdoc>
        private bool IsMdiContainer { 
            get {
                return((Form)Control).IsMdiContainer; 
            } 
            set {
                if (!value) { 
                    UnhookChildControls(Control);
                }
                ((Form)Control).IsMdiContainer = value;
                if (value) { 
                    HookChildControls(Control);
                } 
            } 
        }
 
        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.IsMenuInherited"]/*' />
        /// <devdoc>
        ///      Returns true if the active menu is an inherited component.  We use this to determine if we
        ///      we need to resize the base control or not. 
        /// </devdoc>
        private bool IsMenuInherited { 
            get { 
                if (inheritanceAttribute == null && Menu != null) {
                    inheritanceAttribute = (InheritanceAttribute)TypeDescriptor.GetAttributes(Menu)[typeof(InheritanceAttribute)]; 
                    if (inheritanceAttribute.Equals(InheritanceAttribute.NotInherited)) {
                        isMenuInherited = false;
                    }
                    else { 
                        isMenuInherited = true;
                    } 
                } 
                return isMenuInherited;
            } 
        }


        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.Menu"]/*' /> 
        /// <devdoc>
        ///     Accessor method for the menu property on control.  We shadow 
        ///     this property at design time. 
        /// </devdoc>
        internal MainMenu Menu { 
            get {
                return (MainMenu)ShadowProperties["Menu"];
            }
 
            set {
                if (value == ShadowProperties["Menu"]) { 
                    return; 
                }
 
                ShadowProperties["Menu"] = value;

                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
 
                if (host != null && !host.Loading) {
                    EnsureMenuEditorService(value); 
                    if (menuEditorService != null) 
                       menuEditorService.SetMenu(value);
 
                }

                if (heightDelta == 0) {
                    heightDelta = GetMenuHeight(); 
                }
            } 
        } 

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.Opacity"]/*' /> 
        /// <devdoc>
        ///     Opacity property on control.  We shadow this property at design time.
        /// </devdoc>
        private double Opacity { 
            get {
                return (double)ShadowProperties["Opacity"]; 
            } 
            set {
                if (value < 0.0f || value > 1.0f) { 
                    throw new ArgumentException(SR.GetString(SR.InvalidBoundArgument,
                                                                    "value",
                                                                    value.ToString(CultureInfo.CurrentCulture),
                                                                    (0.0f).ToString(CultureInfo.CurrentCulture), 
                                                                    (1.0f).ToString(CultureInfo.CurrentCulture)),
                                                                    "value"); 
                } 
                ShadowProperties["Opacity"] = value;
            } 
        }

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.SnapLines"]/*' />
        /// <devdoc> 
        ///     Overrides the default implementation of ParentControlDesigner SnapLines.  Note that if the Padding
        ///     property is not set on our Form - we'll special case this and add default Padding values to our 
        ///     SnapLines.  This was a usability request specific to the Form itself. 
        ///
        ///     Note that a Form only has Padding SnapLines. 
        ///
        /// </devdoc>
        public override IList SnapLines {
            get { 

                ArrayList snapLines = null; 
                base.AddPaddingSnapLines(ref snapLines); 
                if (snapLines == null) {
                    Debug.Fail("why did base.AddPaddingSnapLines return null?"); 
                    snapLines = new ArrayList(4);
                }

                //if the padding has not been set - then we'll auto-add padding 
                //to form - this is a Usability request
                // 
                if (Control.Padding == Padding.Empty && snapLines != null) { 

                    int paddingsFound = 0;//used to short-circuit once we find 4 paddings 

                    for(int i = 0; i < snapLines.Count; i++) {
                        // remove previous padding snaplines
                        SnapLine snapLine = snapLines[i] as SnapLine; 

                        if(snapLine != null && snapLine.Filter != null && snapLine.Filter.StartsWith(SnapLine.Padding)) { 
                            if (snapLine.Filter.Equals(SnapLine.PaddingLeft) || snapLine.Filter.Equals(SnapLine.PaddingTop)) { 
                                snapLine.AdjustOffset(DesignerUtils.DEFAULTFORMPADDING);
                                paddingsFound ++; 
                            }

                            if (snapLine.Filter.Equals(SnapLine.PaddingRight) || snapLine.Filter.Equals(SnapLine.PaddingBottom)) {
                                snapLine.AdjustOffset(-DesignerUtils.DEFAULTFORMPADDING); 
                                paddingsFound ++;
                            } 
 
                            if (paddingsFound == 4) {
                                break;//we adjusted all of our paddings 
                            }
                        }
                    }
                } 

                return snapLines; 
            } 
        }
 

        private Size Size {
            get {
                return Control.Size; 
            }
            set { 
                IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService)); 
                PropertyDescriptorCollection props = TypeDescriptor.GetProperties(Component);
                if (cs != null) { 
                    cs.OnComponentChanging(Component, props["ClientSize"]);
                }

                Control.Size = value; 

                if (cs != null) { 
                    cs.OnComponentChanged(Component, props["ClientSize"], null, null); 
                }
            } 
        }

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.ShowInTaskbar"]/*' />
        /// <devdoc> 
        ///     Accessor method for the showInTaskbar property on control.  We shadow
        ///     this property at design time. 
        /// </devdoc> 
        private bool ShowInTaskbar {
            get { 
                return (bool)ShadowProperties["ShowInTaskbar"];
            }
            set {
                ShadowProperties["ShowInTaskbar"] = value; 
            }
        } 
 
        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.WindowState"]/*' />
        /// <devdoc> 
        ///     Accessor method for the windowState property on control.  We shadow
        ///     this property at design time.
        /// </devdoc>
        private FormWindowState WindowState { 
            get {
                return (FormWindowState)ShadowProperties["WindowState"]; 
            } 
            set {
                ShadowProperties["WindowState"] = value; 
            }
        }

        private void ApplyAutoScaling(SizeF baseVar, Form form) { 

            // We also don't do this if the property is empty.  Otherwise we will perform 
            // two GetAutoScaleBaseSize calls only to find that they returned the same 
            // value.
            // 
            if (!baseVar.IsEmpty)
            {
#pragma warning disable 618
                SizeF newVarF = Form.GetAutoScaleSize(form.Font); 
#pragma warning restore 618
                Size newVar = new Size((int)Math.Round(newVarF.Width), (int)Math.Round(newVarF.Height)); 
 
                // We save a significant amount of time by bailing early if there's no work to be done
                if (baseVar.Equals(newVar)) 
                    return;

                float percY = ((float)newVar.Height) / ((float)baseVar.Height);
                float percX = ((float)newVar.Width) / ((float)baseVar.Width); 
                try {
                    inAutoscale = true; 
#pragma warning disable 618 
                    form.Scale(percX, percY);
#pragma warning restore 618 
                }
                finally {
                    inAutoscale = false;
                } 
            }
        } 
 

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.Dispose"]/*' /> 
        /// <devdoc>
        ///     Disposes of this designer.
        /// </devdoc>
        protected override void Dispose(bool disposing) { 
            if (disposing) {
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                Debug.Assert(host != null, "Must have a designer host on dispose"); 

                if (host != null) { 
                    host.LoadComplete -= new EventHandler(OnLoadComplete);
                    host.Activated -= new EventHandler(OnDesignerActivate);
                    host.Deactivated -= new EventHandler(OnDesignerDeactivate);
                } 

                IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService)); 
                if (cs != null) { 
                    cs.ComponentAdded -= new ComponentEventHandler(this.OnComponentAdded);
                    cs.ComponentRemoved -= new ComponentEventHandler(this.OnComponentRemoved); 
                }
            }
            base.Dispose(disposing);
        } 

        internal override void DoProperMenuSelection(ICollection selComponents) { 
            foreach(object obj in selComponents) { 
                //first check to see if our selection is any kind of menu: main, context, item
                // AND the designer for the component is this one 
                //
                Menu menu = obj as Menu;
                if (menu != null) {
                    //if it's a menu item, set the selection 
                    MenuItem item = menu as MenuItem;
                    if (item != null) { 
                        Menu currentMenu = menuEditorService.GetMenu(); 
                        //before we set the selection, we need to check if the item belongs the current menu,
                        //if not, we need to set the menu editor to the appropiate menu, then set selection 
                        //
                        MenuItem parent = item;
                        while (parent.Parent is MenuItem) {
                            parent = (MenuItem)parent.Parent; 
                        }
 
                        if( !(currentMenu == parent.Parent) ) { 
                            menuEditorService.SetMenu(parent.Parent);
                        } 

                        //ok, here we have the correct editor selected for this item.
                        //Now, if there's only one item selected, then let the editor service know,
                        //if there is more than one - then the selection was done through the 
                        //menu editor and we don't need to tell it
                        if(selComponents.Count == 1) { 
                            menuEditorService.SetSelection(item); 
                        }
                    } 
                    //here, either it's a main or context menu, even if the menu is the current one,
                    //we still want to call this "SetMenu" method, 'cause that'll collapse it and
                    //remove the focus
                    // 
                    else {
                        menuEditorService.SetMenu(menu); 
                    } 
                    return;
                } 
                //Here, something is selected, but it is in no way, shape, or form a menu
                //so, we'll collapse our active menu accordingly
                else {
                    if (Menu != null && Menu.MenuItems.Count == 0) { 
                        menuEditorService.SetMenu(null);
                    } 
                    else { 
                        menuEditorService.SetMenu(Menu);
                    } 
                    NativeMethods.SendMessage(Control.Handle, NativeMethods.WM_NCACTIVATE, 1, 0);
                }
            }
        } 

        /// <devdoc> 
        ///      Determines if a MenuEditorService has already been started.  If not, 
        ///      this method will create a new instance of the service.  We override
        ///      this because we want to allow any kind of menu to start the service, 
        ///      not just ContextMenus.
        /// </devdoc>
        protected override void EnsureMenuEditorService(IComponent c) {
            if (menuEditorService == null && c is Menu) { 
                menuEditorService = (IMenuEditorService)GetService(typeof(IMenuEditorService));
            } 
        } 

         private void EnsureToolStripWindowAdornerService() { 
             if (toolStripAdornerWindowService == null) {
                toolStripAdornerWindowService = (ToolStripAdornerWindowService)GetService(typeof(ToolStripAdornerWindowService));
            }
         } 

        /// <devdoc> 
        /// Gets the current menu height so we know how much to increment the form size by 
        /// </devdoc>
        private int GetMenuHeight() { 

            if (Menu == null || (IsMenuInherited && initializing)) {
                return 0;
            } 

            if (menuEditorService != null) { 
                // there is a magic property on teh menueditorservice that gives us this 
                // information.  Unfortuantely, we can't compute it ourselves -- the menu
                // shown in the designer isn't a windows one so we can't ask windows. 
                //
                PropertyDescriptor heightProp = TypeDescriptor.GetProperties(menuEditorService)["MenuHeight"];
                if (heightProp != null) {
                    int height = (int)heightProp.GetValue(menuEditorService); 
                    return height;
                } 
            } 
            return SystemInformation.MenuHeight;
        } 

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.Initialize"]/*' />
        /// <devdoc>
        ///     Initializes the designer with the given component.  The designer can 
        ///     get the component's site and request services from it in this call.
        /// </devdoc> 
        public override void Initialize(IComponent component) { 
            // We have to shadow the WindowState before we call base.Initialize
            PropertyDescriptor windowStateProp = TypeDescriptor.GetProperties(component.GetType())["WindowState"]; 
            if (windowStateProp != null && windowStateProp.PropertyType == typeof(FormWindowState))
            {
                WindowState = (FormWindowState)windowStateProp.GetValue(component);
            } 

            initializing = true; 
            base.Initialize(component); 
            initializing = false;
 
            AutoResizeHandles = true;

            Debug.Assert(component is Form, "FormDocumentDesigner expects its component to be a form.");
 
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (host != null) { 
                host.LoadComplete += new EventHandler(OnLoadComplete); 
                host.Activated += new EventHandler(OnDesignerActivate);
                host.Deactivated += new EventHandler(OnDesignerDeactivate); 
            }

            Form form = (Form)Control;
            form.WindowState = FormWindowState.Normal; 
            ShadowProperties["AcceptButton"] = form.AcceptButton;
            ShadowProperties["CancelButton"] = form.CancelButton; 
 
            // Monitor component/remove add events for our tray
            // 
            IComponentChangeService cs = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            if (cs != null) {
                cs.ComponentAdded += new ComponentEventHandler(this.OnComponentAdded);
                cs.ComponentRemoved += new ComponentEventHandler(this.OnComponentRemoved); 
            }
        } 
 
        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.OnComponentAdded"]/*' />
        /// <devdoc> 
        ///      Called when a component is added to the design container.
        ///      If the component isn't a control, this will demand create
        ///      the component tray and add the component to it.
        /// </devdoc> 
        private void OnComponentAdded(object source, ComponentEventArgs ce) {
            if (ce.Component is Menu) { 
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                if (host != null && !host.Loading) {
                    //if it's a MainMenu & we don't have one set for the form yet, then do it... 
                    //
                    if(ce.Component is MainMenu && !hasMenu) {
                        PropertyDescriptor menuProp = TypeDescriptor.GetProperties(Component)["Menu"];
                        Debug.Assert(menuProp != null, "What happened to the Menu property"); 
                        menuProp.SetValue(Component, ce.Component);
                        hasMenu = true; 
                    } 
                }
            } 
            if(ce.Component is ToolStrip && toolStripAdornerWindowService == null) {
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                if (host != null) {
                    EnsureToolStripWindowAdornerService(); 
                }
 
 
            }
        } 

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.OnComponentRemoved"]/*' />
        /// <devdoc>
        ///      Called when a component is removed from the design container. 
        ///      Here, we check if a menu is being removed and handle removing
        ///      the Form's mainmenu vs. other menus properly. 
        /// </devdoc> 
        private void OnComponentRemoved(object source, ComponentEventArgs ce) {
            if (ce.Component is Menu) { 
                //if we deleted the form's mainmenu, set it null...
                if (ce.Component == Menu) {
                    PropertyDescriptor menuProp = TypeDescriptor.GetProperties(Component)["Menu"];
                    Debug.Assert(menuProp != null, "What happened to the Menu property"); 
                    menuProp.SetValue(Component, null);
                    hasMenu = false; 
                } 
                else if (menuEditorService != null && ce.Component == menuEditorService.GetMenu()) {
                    menuEditorService.SetMenu(Menu); 
                }
            }
            if (ce.Component is ToolStrip && toolStripAdornerWindowService != null) {
                toolStripAdornerWindowService = null; 
            }
            if (ce.Component is IButtonControl) { 
                if (ce.Component == ShadowProperties["AcceptButton"]) { 
                    this.AcceptButton = null;
                } 
                if (ce.Component == ShadowProperties["CancelButton"]) {
                    this.CancelButton = null;
                }
            } 
        }
 
        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.OnCreateHandle"]/*' /> 
        /// <devdoc>
        ///      We're watching the handle creation in case we have a menu editor. 
        ///      If we do, the menu editor will have to be torn down and recreated.
        /// </devdoc>
        protected override void OnCreateHandle() {
            if (Menu != null && menuEditorService !=null) { 
                menuEditorService.SetMenu(null);
                menuEditorService.SetMenu(Menu); 
            } 

            // VSWhidbey 366241 -- in case we get a handle recreate, we need to re-apply the delta. 
            //
            if (heightDelta != 0)
            {
                ((Form)Component).Height += heightDelta; 
                heightDelta = 0;
            } 
        } 

        // <doc> 
        // <desc>
        //      Called when our document becomes active.  We paint our form's
        //      border the appropriate color here.
        // </desc> 
        // </doc>
        // 
        private void OnDesignerActivate(object source, EventArgs evevent) { 
            // Paint the form's title bar UI-active
            // 
            Control control = Control;

            if (control != null && control.IsHandleCreated) {
                NativeMethods.SendMessage(control.Handle, NativeMethods.WM_NCACTIVATE, 1, 0); 
                SafeNativeMethods.RedrawWindow(control.Handle, null, IntPtr.Zero, NativeMethods.RDW_FRAME);
            } 
        } 

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.OnDesignerDeactivate"]/*' /> 
        /// <devdoc>
        ///     Called by the host when we become inactive.  Here we update the
        ///     title bar of our form so it's the inactive color.
        /// </devdoc> 
        private void OnDesignerDeactivate(object sender, EventArgs e) {
            Control control = Control; 
            if (control != null && control.IsHandleCreated) { 
                NativeMethods.SendMessage(control.Handle, NativeMethods.WM_NCACTIVATE, 0, 0);
                SafeNativeMethods.RedrawWindow(control.Handle, null, IntPtr.Zero, NativeMethods.RDW_FRAME); 
            }
        }

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.OnLoadComplete"]/*' /> 
        /// <devdoc>
        ///      Called when our code loads.  Here we connect us as the selection 
        ///      UI handler for ourselves.  This is a special case because for 
        ///      the top level document, we are our own selection UI handler.
        /// </devdoc> 
        private void OnLoadComplete(object source, EventArgs evevent) {
            Form form = Control as Form;
            if (form != null) {
 
                // VSWhidbey #146082
                // The form's ClientSize is reported 
                // including the ScrollBar's height. We 
                // need to account for this in order to display the form with
                // scrollbars correctly. 
                int clientWidth = form.ClientSize.Width;
                int clientHeight = form.ClientSize.Height;
                if (form.HorizontalScroll.Visible && form.AutoScroll) {
                    clientHeight += SystemInformation.HorizontalScrollBarHeight; 
                }
                if (form.VerticalScroll.Visible && form.AutoScroll) { 
                    clientWidth += SystemInformation.VerticalScrollBarWidth; 
                }
 
                // ApplyAutoScaling causes WmWindowPosChanging to be called
                // and there we calculate if we need to compensate for a menu being visible
                // we were causing that calculation to fail if we set ClientSize too early.
                // we now do the right thing AND check again if we need to compensate for 
                // the menu.
                ApplyAutoScaling(autoScaleBaseSize, form); 
 
                this.ClientSize = new Size(clientWidth, clientHeight);
 
                BehaviorService svc = (BehaviorService)GetService( typeof(BehaviorService) );
                if (svc != null) {
                    svc.SyncSelection();
                } 

                // if there is a menu and we need to update our height because of it, 
                // do it now. 
                //
 
                if (heightDelta == 0) {
                    heightDelta = GetMenuHeight();
                }
 
                if (heightDelta != 0) {
                    form.Height += heightDelta; 
                    heightDelta = 0; 
                }
 
                // VSWhidbey #281912
                // After loading the form if the ControlBox and ShowInTaskbar properties are false,
                // the form will be sized incorrectly.  This is due to the text property being set
                // after the ControlBox and ShowInTaskbar properties, which causes windows to recalculate 
                // our client area wrong.  The reason it does this is because after setting the ShowInTaskbar
                // and ControlBox it assumes we have no titlebar, and bases the clientSize we pass it on that. 
                // In reality our ClientSize DOES depend on having a titlebar, so windows gets confused. 
                // This only happens at designtime, because at runtime our special DesignTime only MainMenu
                // is not around to mess things up.  Because of this, I'm adding this nasty workaround to 
                // correctly update the height at design time.
                if (!form.ControlBox && !form.ShowInTaskbar && !string.IsNullOrEmpty(form.Text) && Menu != null && !IsMenuInherited) {
                    form.Height += SystemInformation.CaptionHeight + 1;
                } 

                // To work around 444699 and fix 94431, we will force a layout. 
                form.PerformLayout(); 

            } 

        }

        /// <include file='doc\FormDocumentDesigner.uex' path='docs/doc[@for="FormDocumentDesigner.PreFilterProperties"]/*' /> 
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
            //
            string[] shadowProps = new string[] {
                "Opacity", 
                "Menu",
                "IsMdiContainer", 
                "Size", 
                "ShowInTaskBar",
                "WindowState", 
                "AutoSize",
                "AcceptButton",
                "CancelButton"
            }; 

            Attribute[] empty = new Attribute[0]; 
 
            for (int i = 0; i < shadowProps.Length; i++) {
                prop = (PropertyDescriptor)properties[shadowProps[i]]; 
                if (prop != null) {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(FormDocumentDesigner), prop, empty);
                }
            } 

            // Mark auto scale base size as serializable again so we can 
            // monitor it for backwards compat. 
            prop = (PropertyDescriptor)properties["AutoScaleBaseSize"];
            if (prop != null) { 
                properties["AutoScaleBaseSize"] = TypeDescriptor.CreateProperty(typeof(FormDocumentDesigner), prop, DesignerSerializationVisibilityAttribute.Visible);
            }

            // And set the new default value attribute for client base size, and shadow it as well. 
            //
            prop = (PropertyDescriptor)properties["ClientSize"]; 
            if (prop != null) { 
                properties["ClientSize"] = TypeDescriptor.CreateProperty(typeof(FormDocumentDesigner), prop, new DefaultValueAttribute(new Size(-1, -1)));
            } 
        }

        /// <devdoc>
        ///     Handles the WM_WINDOWPOSCHANGING message 
        /// </devdoc>
        /// <internalonly/> 
        private unsafe void WmWindowPosChanging(ref Message m) { 

            NativeMethods.WINDOWPOS* wp = (NativeMethods.WINDOWPOS *)m.LParam; 


            bool updateSize = inAutoscale;
 
            if (!updateSize) {
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
 
                if (host != null) {
                    updateSize = host.Loading; 
                }
            }

 
            // we want to update the size if we have a menu and...
            // 1) we're doing an autoscale 
            // 2) we're loading a form without an inherited menu (inherited forms will already have the right size) 
            //
            if (updateSize && Menu != null && (wp->flags & NativeMethods.SWP_NOSIZE) == 0 && (IsMenuInherited || inAutoscale)) { 
                heightDelta = GetMenuHeight();
            }
        }
 
                 /// <include file='doc\DocumentDesigner.uex' path='docs/doc[@for="DocumentDesigner.WndProc"]/*' />
        /// <devdoc> 
        ///      Overrides our base class WndProc to provide support for 
        ///      the menu editor service.
        /// </devdoc> 
        protected override void WndProc(ref Message m) {
            switch (m.Msg) {
                case NativeMethods.WM_WINDOWPOSCHANGING:
                    WmWindowPosChanging(ref m); 
                    break;
            } 
            base.WndProc(ref m); 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
