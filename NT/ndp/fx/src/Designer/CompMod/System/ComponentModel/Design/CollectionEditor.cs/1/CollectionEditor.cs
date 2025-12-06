//------------------------------------------------------------------------------ 
// <copyright file="CollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.ComponentModel.Design {
    using System.Design; 
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.Remoting.Activation;
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.ComponentModel.Design.Serialization;
    using System; 
    using System.Collections; 
    using Microsoft.Win32;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging; 
    using System.IO;
    using System.Drawing.Design; 
    using System.Reflection; 
    using System.Windows.Forms;
    using System.Windows.Forms.ComponentModel; 
    using System.Windows.Forms.Design;
    using System.Windows.Forms.VisualStyles;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Globalization; 

 
    /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor"]/*' /> 
    /// <devdoc>
    ///    <para>Provides a generic editor for most any collection.</para> 
    /// </devdoc>
    public class CollectionEditor : UITypeEditor {
        private Type                   type;
        private Type                   collectionItemType; 
        private Type[]                 newItemTypes;
        private ITypeDescriptorContext currentContext; 
 
        private bool                   ignoreChangedEvents = false;
        private bool                   ignoreChangingEvents = false; 

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CancelChanges"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Useful for derived classes to do processing when cancelling changes
        ///    </para> 
        /// </devdoc> 
        protected virtual void CancelChanges() {
        } 

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditor"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.CollectionEditor'/> class using the
        ///       specified collection type. 
        ///    </para> 
        /// </devdoc>
        public CollectionEditor(Type type) { 
            this.type = type;
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionItemType"]/*' /> 
        /// <devdoc>
        ///    <para>Gets or sets the data type of each item in the collection.</para> 
        /// </devdoc> 
        protected Type CollectionItemType {
            get { 
                if (collectionItemType == null) {
                    collectionItemType = CreateCollectionItemType();
                }
                return collectionItemType; 
            }
        } 
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionType"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets or sets the type of the collection.
        ///    </para>
        /// </devdoc> 
        protected Type CollectionType {
            get { 
                return type; 
            }
        } 

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.Context"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Gets or sets a type descriptor that indicates the current context.
        ///    </para> 
        /// </devdoc> 
        protected ITypeDescriptorContext Context {
            get { 
                return currentContext;
            }
        }
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.NewItemTypes"]/*' />
        /// <devdoc> 
        ///    <para>Gets or sets 
        ///       the available item types that can be created for this collection.</para>
        /// </devdoc> 
        protected Type[] NewItemTypes {
            get {
                if (newItemTypes == null) {
                    newItemTypes = CreateNewItemTypes(); 
                }
                return newItemTypes; 
            } 
        }
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.HelpTopic"]/*' />
        /// <devdoc>
        ///    <para>Gets the help topic to display for the dialog help button or pressing F1. Override to
        ///          display a different help topic.</para> 
        /// </devdoc>
        protected virtual string HelpTopic { 
            get { 
                return "net.ComponentModel.CollectionEditor";
            } 
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CanRemoveInstance"]/*' />
        /// <devdoc> 
        ///    <para>Gets or sets a value indicating whether original members of the collection can be removed.</para>
        /// </devdoc> 
        protected virtual bool CanRemoveInstance(object value) { 
            IComponent comp = value as IComponent;
            if (comp != null) { 
                // Make sure the component is not being inherited -- we can't delete these!
                //
                InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(comp)[typeof(InheritanceAttribute)];
                if (ia != null && ia.InheritanceLevel != InheritanceLevel.NotInherited) { 
                    return false;
                } 
            } 

            return true; 
        }


        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CanSelectMultipleInstances"]/*' /> 
        /// <devdoc>
        ///    <para>Gets or sets a value indicating whether multiple collection members can be 
        ///       selected.</para> 
        /// </devdoc>
        protected virtual bool CanSelectMultipleInstances() { 
            return true;
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CreateCollectionForm"]/*' /> 
        /// <devdoc>
        ///    <para>Creates a 
        ///       new form to show the current collection.</para> 
        /// </devdoc>
        protected virtual CollectionForm CreateCollectionForm() { 
            return new CollectionEditorCollectionForm(this);
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CreateInstance"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Creates a new instance of the specified collection item type. 
        ///    </para>
        /// </devdoc> 
        protected virtual object CreateInstance(Type itemType) {
            return CollectionEditor.CreateInstance(itemType, (IDesignerHost)GetService(typeof(IDesignerHost)), null);
        }
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.GetObjectsFromInstance"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       This Function gets the object from the givem object. The input is an arrayList returned as an Object.
        ///       The output is a arraylist which contains the individual objects that need to be created. 
        ///    </para>
        /// </devdoc>
        protected virtual IList GetObjectsFromInstance(object instance) {
            ArrayList ret = new ArrayList(); 
            ret.Add(instance);
            return ret; 
        } 

        internal static object CreateInstance(Type itemType, IDesignerHost host, string name) { 
            object instance = null;

            if (typeof(IComponent).IsAssignableFrom(itemType)) {
                if (host != null) { 
                    instance = host.CreateComponent(itemType, (string)name);
 
                    // Set component defaults 
                    if (host != null) {
                        IComponentInitializer init = host.GetDesigner((IComponent)instance) as IComponentInitializer; 
                        if (init != null) {
                            init.InitializeNewComponent(null);
                        }
                    } 
                }
            } 
 
            if (instance == null) {
                instance = TypeDescriptor.CreateInstance(host, itemType, null, null); 
            }

            return instance;
        } 

 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.GetDisplayText"]/*' /> 
        /// <devdoc>
        ///      Retrieves the display text for the given list item. 
        /// </devdoc>
        protected virtual string GetDisplayText(object value) {
            string text;
 
            if (value == null) {
                return string.Empty; 
            } 

            PropertyDescriptor prop = TypeDescriptor.GetProperties(value)["Name"]; 
            if (prop != null && prop.PropertyType == typeof(string)) {
                text = (string) prop.GetValue( value );
                if (text != null && text.Length > 0) {
                    return text; 
                }
            } 
 
            prop = TypeDescriptor.GetDefaultProperty(CollectionType);
            if (prop != null && prop.PropertyType == typeof(string)) { 
                text = (string)prop.GetValue(value);
                if (text != null && text.Length > 0) {
                    return text;
                } 
            }
 
            text = TypeDescriptor.GetConverter(value).ConvertToString(value); 

            if (text == null || text.Length == 0) { 
                text = value.GetType().Name;
            }

            return text; 
        }
 
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CreateCollectionItemType"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets an instance of
        ///       the data type this collection contains.
        ///    </para> 
        /// </devdoc>
        protected virtual Type CreateCollectionItemType() { 
            PropertyInfo[] props = TypeDescriptor.GetReflectionType(CollectionType).GetProperties(BindingFlags.Public | BindingFlags.Instance); 

            for (int i = 0; i < props.Length; i++) { 
                if (props[i].Name.Equals("Item") || props[i].Name.Equals("Items")) {
                    return props[i].PropertyType;
                }
            } 

            // Couldn't find anything.  Return Object 
 
            Debug.Fail("Collection " + CollectionType.FullName + " contains no Item or Items property so we cannot display and edit any values");
            return typeof(object); 
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CreateNewItemTypes"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the data 
        ///       types this collection editor can create. 
        ///    </para>
        /// </devdoc> 
        protected virtual Type[] CreateNewItemTypes() {
            return new Type[] {CollectionItemType};
        }
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.DestroyInstance"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Destroys the specified instance of the object.
        ///    </para> 
        /// </devdoc>
        protected virtual void DestroyInstance(object instance) {
            IComponent compInstance = instance as IComponent;
            if (compInstance != null) { 
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                if (host != null) { 
                    host.DestroyComponent(compInstance); 
                }
                else { 
                    compInstance.Dispose();
                }
            }
            else { 
                IDisposable dispInstance = instance as IDisposable;
                if (dispInstance != null) { 
                    dispInstance.Dispose(); 
                }
            } 
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.EditValue"]/*' />
        /// <devdoc> 
        ///    <para>Edits the specified object value using the editor style
        ///       provided by <see cref='System.ComponentModel.Design.CollectionEditor.GetEditStyle'/>.</para> 
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { 
            if (provider != null) {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

                if (edSvc != null) { 
                    this.currentContext = context;
 
                    // Always create a new CollectionForm.  We used to do reuse the form in V1 and Everett 
                    // but this implies that the form will never be disposed.
                    CollectionForm localCollectionForm = CreateCollectionForm(); 

                    ITypeDescriptorContext lastContext = currentContext;
                    localCollectionForm.EditValue = value;
                    ignoreChangingEvents = false; 
                    ignoreChangedEvents = false;
                    DesignerTransaction trans = null; 
 
                    bool commitChange = true;
                    IComponentChangeService cs = null; 
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));

                    try {
                        try { 
                            if (host != null) {
                                trans = host.CreateTransaction(SR.GetString(SR.CollectionEditorUndoBatchDesc, CollectionItemType.Name)); 
                            } 
                        }
                        catch(CheckoutException cxe) { 
                            if (cxe == CheckoutException.Canceled)
                                return value;

                            throw cxe; 
                        }
 
                        cs = host != null ? (IComponentChangeService)host.GetService(typeof(IComponentChangeService)) : null; 

                        if (cs != null) { 
                            cs.ComponentChanged += new ComponentChangedEventHandler(this.OnComponentChanged);
                            cs.ComponentChanging += new ComponentChangingEventHandler(this.OnComponentChanging);
                        }
 
                        if (localCollectionForm.ShowEditorDialog(edSvc) == DialogResult.OK) {
                            value = localCollectionForm.EditValue; 
                        } 
                        else {
                            commitChange = false; 
                        }
                    }
                    finally {
                        localCollectionForm.EditValue = null; 
                        this.currentContext = lastContext;
                        if (trans != null) { 
                            if (commitChange) { 
                                trans.Commit();
                            } 
                            else {
                                trans.Cancel();
                            }
                        } 

                        if (cs != null) { 
                            cs.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged); 
                            cs.ComponentChanging -= new ComponentChangingEventHandler(this.OnComponentChanging);
                        } 

                        localCollectionForm.Dispose();
                    }
                } 
            }
 
 
            return value;
        } 

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.GetEditStyle"]/*' />
        /// <devdoc>
        ///    <para>Gets the editing style of the Edit method.</para> 
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust. 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            return UITypeEditorEditStyle.Modal;
        } 

        private bool IsAnyObjectInheritedReadOnly(object[] items) {
            // If the object implements IComponent, and is not sited, check with
            // the inheritance service (if it exists) to see if this is a component 
            // that is being inherited from another class.  If it is, then we do
            // not want to place it in the collection editor.  If the inheritance service 
            // chose not to site the component, that indicates it should be hidden from 
            // the user.
 
            IInheritanceService iSvc = null;
            bool checkISvc = false;

            foreach(object o in items) { 
                IComponent comp = o as IComponent;
                if (comp != null && comp.Site == null) { 
                    if (!checkISvc) { 
                        checkISvc = true;
                        if (Context != null) { 
                            iSvc = (IInheritanceService)Context.GetService(typeof(IInheritanceService));
                        }
                    }
 
                    if (iSvc != null && iSvc.GetInheritanceAttribute(comp).Equals(InheritanceAttribute.InheritedReadOnly)) {
                        return true; 
                    } 
                }
            } 

            return false;
        }
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.GetItems"]/*' />
        /// <devdoc> 
        ///    <para>Converts the specified collection into an array of objects.</para> 
        /// </devdoc>
        protected virtual object[] GetItems(object editValue) { 
            if (editValue != null) {
                // We look to see if the value implements ICollection, and if it does,
                // we set through that.
                // 
                if (editValue is System.Collections.ICollection) {
                    ArrayList list = new ArrayList(); 
 
                    System.Collections.ICollection col = (System.Collections.ICollection)editValue;
                    foreach(object o in col) { 
                        list.Add(o);
                    }

                    object[] values = new object[list.Count]; 
                    list.CopyTo(values, 0);
                    return values; 
                } 
            }
 
            return new object[0];
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.GetService"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Gets the requested service, if it is available. 
        ///    </para>
        /// </devdoc> 
        protected object GetService(Type serviceType) {
            if (Context != null) {
                return Context.GetService(serviceType);
            } 
            return null;
        } 
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.OnComponentChanged"]/*' />
        /// <devdoc> 
        /// reflect any change events to the instance object
        /// </devdoc>
        private void OnComponentChanged(object sender, ComponentChangedEventArgs e) {
            if (!ignoreChangedEvents && sender != Context.Instance) { 
                ignoreChangedEvents = true;
                Context.OnComponentChanged(); 
            } 
        }
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.OnComponentChanging"]/*' />
        /// <devdoc>
        ///  reflect any changed events to the instance object
        /// </devdoc> 
        private void OnComponentChanging(object sender, ComponentChangingEventArgs e) {
            if (!ignoreChangingEvents && sender != Context.Instance) { 
                ignoreChangingEvents = true; 
                Context.OnComponentChanging();
            } 
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.OnItemRemoving"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Removes the item from the column header from the listview column header collection 
        ///    </para> 
        /// </devdoc>
        internal virtual void OnItemRemoving(object item) { 
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.SetItems"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Sets 
        ///       the specified collection to have the specified array of items. 
        ///    </para>
        /// </devdoc> 
        protected virtual object SetItems(object editValue, object[] value) {
            if (editValue != null) {
                Array oldValue = (Array)GetItems(editValue);
                bool  valueSame = (oldValue.Length == value.Length); 
                // We look to see if the value implements IList, and if it does,
                // we set through that. 
                // 
                Debug.Assert(editValue is System.Collections.IList, "editValue is not an IList");
                if (editValue is System.Collections.IList) { 
                    System.Collections.IList list = (System.Collections.IList)editValue;

                    list.Clear();
                    for (int i = 0; i < value.Length; i++) { 
                        list.Add(value[i]);
                    } 
                } 
            }
            return editValue; 
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.ShowHelp"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Called when the help button is clicked. 
        ///    </para> 
        /// </devdoc>
        protected virtual void ShowHelp() { 
            IHelpService helpService = GetService(typeof(IHelpService)) as IHelpService;
            if (helpService != null) {
                helpService.ShowHelpFromKeyword(HelpTopic);
            } 
            else {
                Debug.Fail("Unable to get IHelpService."); 
            } 
        }
 
       internal class SplitButton : Button
        {
            private PushButtonState _state;
            private const int pushButtonWidth = 14; 
            private Rectangle dropDownRectangle = new Rectangle();
            private bool showSplit = false; 
 

 
            public bool ShowSplit
            {
                set
                { 
                    if (value != showSplit)
                    { 
                        showSplit = value; 
                        Invalidate();
                    } 
                }
            }

            private PushButtonState State 
            {
                get 
                { 
                    return _state;
                } 
                set
                {
                    if (!_state.Equals(value))
                    { 
                        _state = value;
                        Invalidate(); 
                    } 
                }
            } 

            public override Size GetPreferredSize(Size proposedSize)
            {
                Size preferredSize = base.GetPreferredSize(proposedSize); 
                if (showSplit && !string.IsNullOrEmpty(Text) && TextRenderer.MeasureText(Text, Font).Width + pushButtonWidth > preferredSize.Width)
                { 
                    return preferredSize + new Size(pushButtonWidth, 0); 
                }
 
                return preferredSize;
            }

            protected override bool IsInputKey(Keys keyData) 
            {
                if (keyData.Equals(Keys.Down) && showSplit) 
                { 
                    return true;
                } 
                else
                {
                    return base.IsInputKey(keyData);
                } 
            }
 
            protected override void OnGotFocus(EventArgs e) 
            {
                if (!showSplit) 
                {
                    base.OnGotFocus(e);
                    return;
                } 

                if (!State.Equals(PushButtonState.Pressed) && !State.Equals(PushButtonState.Disabled)) 
                { 
                    State = PushButtonState.Default;
                } 
            }

            protected override void OnKeyDown(KeyEventArgs kevent)
            { 
                if (kevent.KeyCode.Equals(Keys.Down) && showSplit)
                { 
                    ShowContextMenuStrip(); 
                }
            } 

            protected override void OnLostFocus(EventArgs e)
            {
                if (!showSplit) 
                {
                    base.OnLostFocus(e); 
                    return; 
                }
                if (!State.Equals(PushButtonState.Pressed) && !State.Equals(PushButtonState.Disabled)) 
                {
                    State = PushButtonState.Normal;
                }
            } 

            protected override void OnMouseDown(MouseEventArgs e) 
            { 
                if (!showSplit)
                { 
                    base.OnMouseDown(e);
                    return;
                }
 
                if (dropDownRectangle.Contains(e.Location))
                { 
                    ShowContextMenuStrip(); 
                }
                else 
                {
                    State = PushButtonState.Pressed;
                }
            } 

            protected override void OnMouseEnter(EventArgs e) 
            { 
                if (!showSplit)
                { 
                    base.OnMouseEnter(e);
                    return;
                }
 
                if (!State.Equals(PushButtonState.Pressed) && !State.Equals(PushButtonState.Disabled))
                { 
                    State = PushButtonState.Hot; 
                }
            } 

            protected override void OnMouseLeave(EventArgs e)
            {
                if (!showSplit) 
                {
                    base.OnMouseLeave(e); 
                    return; 
                }
 
                if (!State.Equals(PushButtonState.Pressed) && !State.Equals(PushButtonState.Disabled))
                {
                    if (Focused)
                    { 
                        State = PushButtonState.Default;
                    } 
                    else 
                    {
                        State = PushButtonState.Normal; 
                    }
                }
            }
 
            protected override void OnMouseUp(MouseEventArgs mevent)
            { 
                if (!showSplit) 
                {
                    base.OnMouseUp(mevent); 
                    return;
                }

                if (ContextMenuStrip == null || !ContextMenuStrip.Visible) 
                {
                    SetButtonDrawState(); 
                    if (Bounds.Contains(Parent.PointToClient(Cursor.Position)) && !dropDownRectangle.Contains(mevent.Location)) 
                    {
                        OnClick(new EventArgs()); 
                    }
                }
            }
 
            protected override void OnPaint(PaintEventArgs pevent)
            { 
                base.OnPaint(pevent); 

                if (!showSplit) 
                {
                    return;
                }
 
                Graphics g = pevent.Graphics;
                Rectangle bounds = new Rectangle(0, 0, Width, Height); 
                TextFormatFlags formatFlags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter; 

                ButtonRenderer.DrawButton(g, bounds, State); 

                dropDownRectangle = new Rectangle(bounds.Right - pushButtonWidth - 1, 4, pushButtonWidth, bounds.Height - 8);

 
                if (RightToLeft == RightToLeft.Yes) {
                    dropDownRectangle.X = bounds.Left + 1; 
 
                    g.DrawLine(SystemPens.ButtonHighlight, bounds.Left + pushButtonWidth, 4, bounds.Left + pushButtonWidth, bounds.Bottom -4);
                    g.DrawLine(SystemPens.ButtonHighlight, bounds.Left + pushButtonWidth + 1, 4, bounds.Left + pushButtonWidth + 1, bounds.Bottom -4); 
                    bounds.Offset(pushButtonWidth, 0);
                    bounds.Width = bounds.Width - pushButtonWidth;
                }
                else { 
                    g.DrawLine(SystemPens.ButtonHighlight, bounds.Right - pushButtonWidth, 4, bounds.Right - pushButtonWidth, bounds.Bottom -4);
                    g.DrawLine(SystemPens.ButtonHighlight, bounds.Right - pushButtonWidth - 1, 4, bounds.Right - pushButtonWidth - 1, bounds.Bottom -4); 
                    bounds.Width = bounds.Width - pushButtonWidth; 
                }
 
                PaintArrow(g, dropDownRectangle);

                // If we dont' use mnemonic, set formatFlag to NoPrefix as this will show ampersand.
                if (!UseMnemonic) { 
                    formatFlags = formatFlags | TextFormatFlags.NoPrefix;
                } 
                else if (!ShowKeyboardCues) { 
                    formatFlags = formatFlags | TextFormatFlags.HidePrefix;
                } 

                if (!string.IsNullOrEmpty(this.Text)) {
                    TextRenderer.DrawText(g, Text, Font, bounds, SystemColors.ControlText, formatFlags);
                } 

                if (Focused) { 
                    bounds.Inflate(-4,-4); 
                    //ControlPaint.DrawFocusRectangle(g, bounds);
                } 
            }

            private void PaintArrow(Graphics g, Rectangle dropDownRect) {
                Point middle = new Point(Convert.ToInt32(dropDownRect.Left + dropDownRect.Width / 2), Convert.ToInt32(dropDownRect.Top + dropDownRect.Height / 2)); 

                //if the width is odd - favor pushing it over one pixel right. 
                middle.X += (dropDownRect.Width % 2); 

                Point[] arrow = new Point[] {new Point(middle.X - 2, middle.Y - 1), new Point(middle.X + 3, middle.Y - 1), new Point(middle.X, middle.Y + 2)}; 

                g.FillPolygon(SystemBrushes.ControlText, arrow);
            }
 
            private void ShowContextMenuStrip() {
                State = PushButtonState.Pressed; 
                if (ContextMenuStrip != null) { 
                    ContextMenuStrip.Closed += new ToolStripDropDownClosedEventHandler(ContextMenuStrip_Closed);
                    ContextMenuStrip.Show(this, 0, Height); 
                }
            }

            private void ContextMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e) 
            {
                ContextMenuStrip cms = sender as ContextMenuStrip; 
                if (cms != null) 
                {
                    cms.Closed -= new ToolStripDropDownClosedEventHandler(ContextMenuStrip_Closed); 
                }

                SetButtonDrawState();
            } 

            private void SetButtonDrawState() 
            { 
                if (Bounds.Contains(Parent.PointToClient(Cursor.Position)))
                { 
                    State = PushButtonState.Hot;
                }
                else if (Focused)
                { 
                    State = PushButtonState.Default;
                } 
                else 
                {
                    State = PushButtonState.Normal; 
                }
            }
        }
 

 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm"]/*' /> 
        /// <devdoc>
        ///      This is the collection editor's default implementation of a 
        ///      collection form.
        /// </devdoc>
        private class CollectionEditorCollectionForm : CollectionForm {
 
            private const int               TEXT_INDENT = 1;
            private const int               PAINT_WIDTH = 20; 
            private const int               PAINT_INDENT = 26; 
            private static readonly double  LOG10 = Math.Log(10);
 
            // Manipulation of the collection.
            //
            private ArrayList              createdItems;
            private ArrayList              removedItems; 
            private ArrayList              originalItems;
 
            // Calling Editor 
            private CollectionEditor       editor;
 
            // Dialog UI
            //
            private FilterListBox          listbox;
            private SplitButton            addButton; 
            private Button                 removeButton;
            private Button                 cancelButton; 
            private Button                 okButton; 
            private Button                 downButton;
            private Button                 upButton; 
            private VsPropertyGrid         propertyBrowser;
            private Label                  membersLabel;
            private Label                  propertiesLabel;
            private ContextMenuStrip       addDownMenu; 
            private TableLayoutPanel       okCancelTableLayoutPanel;
            private TableLayoutPanel       overArchingTableLayoutPanel; 
            private TableLayoutPanel       addRemoveTableLayoutPanel; 

            // Prevent flicker when switching selection 
            private int                    suspendEnabledCount = 0;

            // our flag for if something changed
            // 
            private bool                   dirty;
 
            public CollectionEditorCollectionForm(CollectionEditor editor) : base(editor) { 
                this.editor = editor;
                InitializeComponent(); 
                this.Text = SR.GetString(SR.CollectionEditorCaption, CollectionItemType.Name);

                HookEvents();
 

 
                Type[] newItemTypes = NewItemTypes; 
                if (newItemTypes.Length > 1) {
                    EventHandler addDownMenuClick = new EventHandler(this.AddDownMenu_click); 
                    addButton.ShowSplit = true;
                    addDownMenu = new ContextMenuStrip();
                    addButton.ContextMenuStrip = addDownMenu;
                    for (int i = 0; i < newItemTypes.Length; i++) { 
                        addDownMenu.Items.Add(new TypeMenuItem(newItemTypes[i], addDownMenuClick));
                    } 
                } 

                AdjustListBoxItemHeight(); 
            }

            private bool IsImmutable {
                get { 
                    bool immutable = true;
 
                    // We are considered immutable if the converter is defined as requiring a 
                    // create instance or all the properties are read-only.
                    // 
                    if (!TypeDescriptor.GetConverter(CollectionItemType).GetCreateInstanceSupported()) {
                        foreach (PropertyDescriptor p in TypeDescriptor.GetProperties(CollectionItemType)) {
                            if (!p.IsReadOnly) {
                                immutable = false; 
                                break;
                            } 
                        } 
                    }
 
                    return immutable;
                }
            }
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.AddButton_click"]/*' />
            /// <devdoc> 
            ///      Adds a new element to the collection. 
            /// </devdoc>
            private void AddButton_click(object sender, EventArgs e) { 
                PerformAdd();
            }

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.AddDownMenu_click"]/*' /> 
            /// <devdoc>
            ///      Processes a click of the drop down type menu.  This creates a 
            ///      new instance. 
            /// </devdoc>
            private void AddDownMenu_click(object sender, EventArgs e) { 
                if (sender is TypeMenuItem) {
                    TypeMenuItem typeMenuItem = (TypeMenuItem) sender;
                    CreateAndAddInstance(typeMenuItem.ItemType);
                } 
            }
 
            /// <internalonly/> 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.AddItems"]/*' />
            /// <devdoc> 
            ///      This Function adds the individual objects to the ListBox.
            /// </devdoc>
            private void AddItems(IList instances) {
 
                if (createdItems == null) {
                   createdItems = new ArrayList(); 
                } 

                listbox.BeginUpdate(); 
                try {
                    foreach( object instance in instances ){
                        if (instance != null) {
                            dirty = true; 
                            createdItems.Add(instance);
                            ListItem created = new ListItem(editor, instance); 
                            listbox.Items.Add(created); 
                        }
                    } 
                }
                finally {
                    listbox.EndUpdate();
                } 

                if (instances.Count == 1) { 
                    // optimize for the case where we just added one thing... 
                    UpdateItemWidths(listbox.Items[listbox.Items.Count -1] as ListItem);
                } 
                else {
                    // othewise go through the entire list
                    UpdateItemWidths(null);
                } 

                // Select the last item 
                // 
                SuspendEnabledUpdates();
                try { 
                    listbox.ClearSelected();
                    listbox.SelectedIndex = listbox.Items.Count - 1;

                    object[] items = new object[listbox.Items.Count]; 
                    for (int i = 0; i < items.Length; i++) {
                        items[i] = ((ListItem)listbox.Items[i]).Value; 
                    } 
                    Items = items;
 
                    //fringe case -- someone changes the edit value which resets the selindex, we should keep the new index.
                    if (listbox.Items.Count > 0 && listbox.SelectedIndex != listbox.Items.Count - 1) {
                        listbox.ClearSelected();
                        listbox.SelectedIndex = listbox.Items.Count - 1; 
                    }
                } 
 
                finally {
                    ResumeEnabledUpdates(true); 
                }
            }

            private void AdjustListBoxItemHeight() { 
                listbox.ItemHeight = Font.Height + SystemInformation.BorderSize.Width*2;
            } 
 
            /// <devdoc>
            ///     Determines whether removal of a specific list item should be permitted. 
            ///     Used to determine enabled/disabled state of the Remove (X) button.
            ///     Items added after editor was opened may always be removed.
            ///     Items that existed before editor was opened require a call to CanRemoveInstance.
            /// </devdoc> 
            private bool AllowRemoveInstance(object value) {
                if (createdItems != null && createdItems.Contains(value)) { 
                    return true; 
                }
                else { 
                    return CanRemoveInstance(value);
                }
            }
 
            private int CalcItemWidth(Graphics g, ListItem item) {
                int c = listbox.Items.Count; 
                if (c < 2) { 
                    c = 2;  //for c-1 should be greater than zero.
                } 

                SizeF sizeW = g.MeasureString(c.ToString(CultureInfo.CurrentCulture), listbox.Font);

                int charactersInNumber = ((int)(Math.Log((double)(c-1)) / LOG10) + 1); 
                int w = 4 + charactersInNumber * (Font.Height /2);
 
                w = Math.Max(w, (int)Math.Ceiling(sizeW.Width)); 
                w += SystemInformation.BorderSize.Width * 4;
 
                SizeF size = g.MeasureString(GetDisplayText(item), listbox.Font);
                int pic = 0;
                if (item.Editor != null && item.Editor.GetPaintValueSupported()) {
                    pic = PAINT_WIDTH + TEXT_INDENT; 
                }
                return (int)Math.Ceiling(size.Width) + w + pic + SystemInformation.BorderSize.Width * 4; 
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.CancelButton_click"]/*' /> 
            /// <devdoc>
            ///      Aborts changes made in the editor.
            /// </devdoc>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
            private void CancelButton_click(object sender, EventArgs e) {
                try { 
 
                    editor.CancelChanges();
 
                    if (!CollectionEditable || !dirty) {
                        return;
                    }
 
                    dirty = false;
                    listbox.Items.Clear(); 
 
                    if (createdItems != null) {
                        object[] items = createdItems.ToArray(); 
                        if(items.Length > 0 && items[0] is IComponent && ((IComponent)items[0]).Site != null) {
                            // here we bail now because we don't want to do the "undo" manually,
                            // we're part of a trasaction, we've added item, the rollback will be
                            // handled by the undo engine because the component in the collection are sited 
                            // doing it here kills perfs because the undo of the transaction has to rollback the remove and then
                            // rollback the add. This is useless and is only needed for non sited component or other classes 
                            return; 
                        }
                        for (int i=0; i<items.Length; i++) { 
                            DestroyInstance(items[i]);
                        }
                        createdItems.Clear();
                    } 
                    if (removedItems != null) {
                        removedItems.Clear(); 
                    } 

 
                    // Restore the original contents. Because objects get parented during CreateAndAddInstance, the underlying collection
                    // gets changed during add, but not other operations. Not all consumers of this dialog can roll back every single change,
                    // but this will at least roll back the additions, removals and reordering. See ASURT #85470.
                    if (originalItems != null && (originalItems.Count > 0)) { 
                        object[] items = new object[originalItems.Count];
                        for (int i = 0; i < originalItems.Count; i++) { 
                            items[i] = originalItems[i]; 
                        }
                        Items = items; 
                        originalItems.Clear();
                    }
                    else {
                        Items = new object[0]; 
                    }
 
                } 
                catch (Exception ex) {
                    DialogResult = DialogResult.None; 
                    DisplayError(ex);
                }
            }
 

 
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.CreateAndAddInstance"]/*' />
            /// <devdoc> 
            ///      Performs a create instance and then adds the instance to
            ///      the list box.
            /// </devdoc>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
            private void CreateAndAddInstance(Type type) {
                try { 
                    object instance = CreateInstance(type); 
                    IList multipleInstance = editor.GetObjectsFromInstance(instance);
 
                    if (multipleInstance != null) {
                        AddItems(multipleInstance);
                    }
                } 
                catch (Exception e) {
                    DisplayError(e); 
                } 
            }
 



            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.DownButton_click"]/*' /> 
            /// <devdoc>
            ///      Moves the selected item down one. 
            /// </devdoc> 
            private void DownButton_click(object sender, EventArgs e) {
                try { 
                    SuspendEnabledUpdates();
                    dirty = true;
                    int index = listbox.SelectedIndex;
                    if (index == listbox.Items.Count - 1) 
                        return;
                    int ti = listbox.TopIndex; 
                    object itemMove = listbox.Items[index]; 
                    listbox.Items[index] = listbox.Items[index+1];
                    listbox.Items[index+1] = itemMove; 

                    if (ti < listbox.Items.Count - 1)
                        listbox.TopIndex = ti + 1;
 
                    listbox.ClearSelected();
                    listbox.SelectedIndex = index + 1; 
 
                    // enabling/disabling the buttons has moved the focus to the OK button, move it back to the sender
                    Control ctrlSender = (Control)sender; 

                    if (ctrlSender.Enabled) {
                        ctrlSender.Focus ();
                    } 
                }
                finally { 
 
                    ResumeEnabledUpdates(true);
                } 
            }

            private void CollectionEditor_HelpButtonClicked(object sender, CancelEventArgs e) {
                e.Cancel = true; 
                editor.ShowHelp();
            } 
 
            private void Form_HelpRequested(object sender, HelpEventArgs e) {
                editor.ShowHelp(); 
            }

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.GetDisplayText"]/*' />
            /// <devdoc> 
            ///     Retrieves the display text for the given list item (if any). The item determines its own display text
            ///     through its ToString() method, which delegates to the GetDisplayText() override on the parent CollectionEditor. 
            ///     This means in theory that the text can change at any time (ie. its not fixed when the item is added to the list). 
            ///     The item returns its display text through ToString() so that the same text will be reported to Accessibility clients.
            /// </devdoc> 
            private string GetDisplayText(ListItem item) {
                return (item == null) ? String.Empty : item.ToString();
            }
 
            private void HookEvents() {
                listbox.KeyDown += new KeyEventHandler(this.Listbox_keyDown); 
                listbox.DrawItem += new DrawItemEventHandler(this.Listbox_drawItem); 
                listbox.SelectedIndexChanged += new EventHandler(this.Listbox_selectedIndexChanged);
                listbox.HandleCreated += new EventHandler(this.Listbox_handleCreated); 
                upButton.Click += new EventHandler(this.UpButton_click);
                downButton.Click += new EventHandler(this.DownButton_click);
                propertyBrowser.PropertyValueChanged += new PropertyValueChangedEventHandler(this.PropertyGrid_propertyValueChanged);
                addButton.Click += new EventHandler(this.AddButton_click); 
                removeButton.Click += new EventHandler(this.RemoveButton_click);
                okButton.Click += new EventHandler(this.OKButton_click); 
                cancelButton.Click += new EventHandler(this.CancelButton_click); 
                this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.CollectionEditor_HelpButtonClicked);
                this.HelpRequested += new HelpEventHandler(this.Form_HelpRequested); 
            }

            private void InitializeComponent()
            { 
                System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CollectionEditor));
                this.membersLabel = new System.Windows.Forms.Label(); 
                this.listbox = new FilterListBox(); 
                this.upButton = new Button();
                this.downButton = new Button(); 
                this.propertiesLabel = new System.Windows.Forms.Label();
                this.propertyBrowser = new VsPropertyGrid(Context);
                this.addButton = new SplitButton();
                this.removeButton = new System.Windows.Forms.Button(); 
                this.okButton = new System.Windows.Forms.Button();
                this.cancelButton = new System.Windows.Forms.Button(); 
                this.okCancelTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
                this.overArchingTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
                this.addRemoveTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
                this.okCancelTableLayoutPanel.SuspendLayout();
                this.overArchingTableLayoutPanel.SuspendLayout();
                this.addRemoveTableLayoutPanel.SuspendLayout();
                this.SuspendLayout(); 
                //
                // membersLabel 
                // 
                resources.ApplyResources(this.membersLabel, "membersLabel");
                this.membersLabel.Name = "membersLabel"; 
                //
                // listbox
                //
                resources.ApplyResources(this.listbox, "listbox"); 
                this.listbox.SelectionMode = (CanSelectMultipleInstances() ? SelectionMode.MultiExtended : SelectionMode.One);
                this.listbox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed; 
                this.listbox.FormattingEnabled = true; 
                this.listbox.Name = "listbox";
                this.overArchingTableLayoutPanel.SetRowSpan(this.listbox, 2); 
                //
                // upButton
                //
                resources.ApplyResources(this.upButton, "upButton"); 
                this.upButton.Name = "upButton";
                // 
                // downButton 
                //
                resources.ApplyResources(this.downButton, "downButton"); 
                this.downButton.Name = "downButton";
                //
                // propertiesLabel
                // 
                resources.ApplyResources(this.propertiesLabel, "propertiesLabel");
                this.propertiesLabel.AutoEllipsis = true; 
                this.propertiesLabel.Name = "propertiesLabel"; 
                //
                // propertyBrowser 
                //
                resources.ApplyResources(this.propertyBrowser, "propertyBrowser");
                this.propertyBrowser.CommandsVisibleIfAvailable = false;
                this.propertyBrowser.Name = "propertyBrowser"; 
                this.overArchingTableLayoutPanel.SetRowSpan(this.propertyBrowser, 3);
                // 
                // addButton 
                //
                resources.ApplyResources(this.addButton, "addButton"); 
                this.addButton.Name = "addButton";
                //
                // removeButton
                // 
                resources.ApplyResources(this.removeButton, "removeButton");
                this.removeButton.Name = "removeButton"; 
                // 
                // okButton
                // 
                resources.ApplyResources(this.okButton, "okButton");
                this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.okButton.Name = "okButton";
                // 
                // cancelButton
                // 
                resources.ApplyResources(this.cancelButton, "cancelButton"); 
                this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                this.cancelButton.Name = "cancelButton"; 
                //
                // okCancelTableLayoutPanel
                //
                resources.ApplyResources(this.okCancelTableLayoutPanel, "okCancelTableLayoutPanel"); 
                this.overArchingTableLayoutPanel.SetColumnSpan(this.okCancelTableLayoutPanel, 3);
                this.okCancelTableLayoutPanel.Controls.Add(this.okButton, 0, 0); 
                this.okCancelTableLayoutPanel.Controls.Add(this.cancelButton, 1, 0); 
                this.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel";
                // 
                // overArchingTableLayoutPanel
                //
                resources.ApplyResources(this.overArchingTableLayoutPanel, "overArchingTableLayoutPanel");
                this.overArchingTableLayoutPanel.Controls.Add(this.downButton, 1, 2); 
                this.overArchingTableLayoutPanel.Controls.Add(this.addRemoveTableLayoutPanel, 0, 3);
                this.overArchingTableLayoutPanel.Controls.Add(this.propertiesLabel, 2, 0); 
                this.overArchingTableLayoutPanel.Controls.Add(this.membersLabel, 0, 0); 
                this.overArchingTableLayoutPanel.Controls.Add(this.listbox, 0, 1);
                this.overArchingTableLayoutPanel.Controls.Add(this.propertyBrowser, 2, 1); 
                this.overArchingTableLayoutPanel.Controls.Add(this.okCancelTableLayoutPanel, 0, 4);
                this.overArchingTableLayoutPanel.Controls.Add(this.upButton, 1, 1);
                this.overArchingTableLayoutPanel.Name = "overArchingTableLayoutPanel";
                // 
                // addRemoveTableLayoutPanel
                // 
                resources.ApplyResources(this.addRemoveTableLayoutPanel, "addRemoveTableLayoutPanel"); 
                this.addRemoveTableLayoutPanel.Controls.Add(this.addButton, 0, 0);
                this.addRemoveTableLayoutPanel.Controls.Add(this.removeButton, 2, 0); 
                this.addRemoveTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
                this.addRemoveTableLayoutPanel.Name = "addRemoveTableLayoutPanel";
                //
                // CollectionEditor 
                //
                this.AcceptButton = this.okButton; 
                resources.ApplyResources(this, "$this"); 
                this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
                this.CancelButton = this.cancelButton; 
                this.Controls.Add(this.overArchingTableLayoutPanel);
                this.HelpButton = true;
                this.MaximizeBox = false;
                this.MinimizeBox = false; 
                this.Name = "CollectionEditor";
                this.ShowIcon = false; 
                this.ShowInTaskbar = false; 
                this.okCancelTableLayoutPanel.ResumeLayout(false);
                this.okCancelTableLayoutPanel.PerformLayout(); 
                this.overArchingTableLayoutPanel.ResumeLayout(false);
                this.overArchingTableLayoutPanel.PerformLayout();
                this.addRemoveTableLayoutPanel.ResumeLayout(false);
                this.addRemoveTableLayoutPanel.PerformLayout(); 
                this.ResumeLayout(false);
            } 
 
            private void UpdateItemWidths(ListItem item) {
                // VSWhidbey#384112: Its neither safe nor accurate to perform these width 
                // calculations prior to normal listbox handle creation. So we nop in this case now.
                if (!listbox.IsHandleCreated) {
                    return;
                } 

                using (Graphics g = listbox.CreateGraphics()) { 
                    int old = listbox.HorizontalExtent; 

                    if (item != null) { 
                        int w = CalcItemWidth(g, item);
                        if (w > old) {
                            listbox.HorizontalExtent = w;
                        } 
                    }
                    else { 
                        int max = 0; 
                        foreach (ListItem i in listbox.Items) {
                            int w = CalcItemWidth(g, i); 
                            if (w > max) {
                                max = w;
                            }
                        } 
                        listbox.HorizontalExtent = max;
                    } 
                } 
            }
 


            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.Listbox_drawItem"]/*' />
            /// <devdoc> 
            ///     This draws a row of the listbox.
            /// </devdoc> 
            private void Listbox_drawItem(object sender, DrawItemEventArgs e) { 
                if (e.Index != -1) {
                    ListItem item = (ListItem)listbox.Items[e.Index]; 

                    Graphics g = e.Graphics;

                    int c = listbox.Items.Count; 
                    int maxC = (c > 1) ? c - 1: c;
                    // We add the +4 is a fudge factor... 
                    // 
                    SizeF sizeW = g.MeasureString(maxC.ToString(CultureInfo.CurrentCulture), listbox.Font);
 
                    int charactersInNumber = ((int)(Math.Log((double)maxC) / LOG10) + 1);// Luckily, this is never called if count = 0
                    int w = 4 + charactersInNumber * (Font.Height / 2);

                    w = Math.Max(w, (int)Math.Ceiling(sizeW.Width)); 
                    w += SystemInformation.BorderSize.Width * 4;
 
                    Rectangle button = new Rectangle(e.Bounds.X, e.Bounds.Y, w, e.Bounds.Height); 

                    ControlPaint.DrawButton(g, button, ButtonState.Normal); 
                    button.Inflate(-SystemInformation.BorderSize.Width*2, -SystemInformation.BorderSize.Height*2);

                    int offset = w;
 
                    Color backColor = SystemColors.Window;
                    Color textColor = SystemColors.WindowText; 
                    if ((e.State & DrawItemState.Selected) == DrawItemState.Selected) { 
                        backColor = SystemColors.Highlight;
                        textColor = SystemColors.HighlightText; 
                    }
                    Rectangle res = new Rectangle(e.Bounds.X + offset, e.Bounds.Y,
                                                  e.Bounds.Width - offset,
                                                  e.Bounds.Height); 
                    g.FillRectangle(new SolidBrush(backColor), res);
                    if ((e.State & DrawItemState.Focus) == DrawItemState.Focus) { 
                        ControlPaint.DrawFocusRectangle(g, res); 
                    }
                    offset+=2; 

                    if (item.Editor != null && item.Editor.GetPaintValueSupported()) {
                        Rectangle baseVar = new Rectangle(e.Bounds.X + offset, e.Bounds.Y + 1, PAINT_WIDTH, e.Bounds.Height - 3);
                        g.DrawRectangle(SystemPens.ControlText, baseVar.X, baseVar.Y, baseVar.Width - 1, baseVar.Height - 1); 
                        baseVar.Inflate(-1, -1);
                        item.Editor.PaintValue(item.Value, g, baseVar); 
                        offset += PAINT_INDENT + TEXT_INDENT; 
                    }
 
                    StringFormat format = new StringFormat();
                    try {
                        format.Alignment = StringAlignment.Center;
                        g.DrawString(e.Index.ToString(CultureInfo.CurrentCulture), Font, SystemBrushes.ControlText, 
                                     new Rectangle(e.Bounds.X, e.Bounds.Y, w, e.Bounds.Height), format);
                    } 
 
                    finally {
                        if (format != null) { 
                            format.Dispose();
                        }
                    }
 
                    Brush textBrush = new SolidBrush(textColor);
 
                    string itemText = GetDisplayText(item); 

                    try { 
                        g.DrawString(itemText, Font, textBrush,
                                     new Rectangle(e.Bounds.X + offset, e.Bounds.Y, e.Bounds.Width - offset, e.Bounds.Height));
                    }
 
                    finally {
                        if (textBrush != null) { 
                            textBrush.Dispose(); 
                        }
                    } 

                    // Check to see if we need to change the horizontal extent of the listbox
                    //
                    int width = offset + (int)g.MeasureString(itemText, Font).Width; 
                    if (width > e.Bounds.Width && listbox.HorizontalExtent < width) {
                        listbox.HorizontalExtent = width; 
                    } 
                }
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.Listbox_keyPress"]/*' />
            /// <devdoc>
            ///      Handles keypress events for the list box. 
            /// </devdoc>
            private void Listbox_keyDown(object sender, KeyEventArgs kevent) { 
                switch (kevent.KeyData) { 
                    case Keys.Delete:
                        PerformRemove(); 
                        break;
                    case Keys.Insert:
                        PerformAdd();
                        break; 
                }
            } 
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.Listbox_selectedIndexChanged"]/*' />
            /// <devdoc> 
            ///      Event that fires when the selected list box index changes.
            /// </devdoc>
            private void Listbox_selectedIndexChanged(object sender, EventArgs e) {
                UpdateEnabled(); 
            }
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.Listbox_handleCreated"]/*' /> 
            /// <devdoc>
            ///      Event that fires when the list box's window handle is created. 
            /// </devdoc>
            private void Listbox_handleCreated(object sender, EventArgs e) {
                // VSWhidbey#384112: Since we no longer perform width calculations prior to handle
                // creation now, we need to ensure we do it at least once after handle creation. 
                UpdateItemWidths(null);
            } 
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.OKButton_click"]/*' />
            /// <devdoc> 
            ///      Commits the changes to the editor.
            /// </devdoc>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
            private void OKButton_click(object sender, EventArgs e) { 
                try {
 
                    if (!dirty || !CollectionEditable) { 
                        dirty = false;
                        DialogResult = DialogResult.Cancel; 
                        return;
                    }

                    // Now apply the changes to the actual value. 
                    //
                    if (dirty) { 
                        object[] items = new object[listbox.Items.Count]; 
                        for (int i = 0; i < items.Length; i++) {
                            items[i] = ((ListItem)listbox.Items[i]).Value; 
                        }

                        Items = items;
                    } 

 
                    // Now destroy any existing items we had. 
                    //
                    if (removedItems != null && dirty) { 
                        object[] deadItems = removedItems.ToArray();

                        for (int i=0; i<deadItems.Length; i++) {
                            DestroyInstance(deadItems[i]); 
                        }
                        removedItems.Clear(); 
                    } 
                    if (createdItems != null) {
                        createdItems.Clear(); 
                    }

                    if (originalItems != null) {
                        originalItems.Clear(); 
                    }
 
                    listbox.Items.Clear(); 
                    dirty = false;
                } 
                catch (Exception ex) {
                    DialogResult = DialogResult.None;
                    DisplayError(ex);
                } 
            }
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.OnComponentChanged"]/*' /> 
            /// <devdoc>
            /// reflect any change events to the instance object 
            /// </devdoc>
            private void OnComponentChanged(object sender, ComponentChangedEventArgs e) {

                // see if this is any of the items in our list...this can happen if 
                // we launched a child editor
                if (!dirty) { 
                    foreach (object item in originalItems) { 
                        if (item == e.Component) {
                            dirty = true; 
                            break;
                        }
                    }
                } 

            } 
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.OnEditValueChanged"]/*' />
            /// <devdoc> 
            ///      This is called when the value property in the CollectionForm has changed.
            ///      In it you should update your user interface to reflect the current value.
            /// </devdoc>
            protected override void OnEditValueChanged() { 

                // Remember these contents for cancellation 
                if (originalItems == null) { 
                    originalItems = new ArrayList();
                } 
                originalItems.Clear();

                // Now update the list box.
                // 
                listbox.Items.Clear();
                propertyBrowser.Site = new PropertyGridSite(Context, propertyBrowser); 
                if (EditValue != null) { 
                    SuspendEnabledUpdates();
                    try { 
                        object[] items = Items;
                        for (int i = 0; i < items.Length; i++) {
                            listbox.Items.Add(new ListItem(editor, items[i]));
                            originalItems.Add(items[i]); 
                        }
                        if (listbox.Items.Count > 0) { 
                            listbox.SelectedIndex = 0; 
                        }
                    } 
                    finally {
                        ResumeEnabledUpdates(true);
                    }
                } 
                else {
                    UpdateEnabled(); 
                } 

                AdjustListBoxItemHeight(); 
                UpdateItemWidths(null);

            }
 
            protected override void OnFontChanged(EventArgs e) {
                base.OnFontChanged(e); 
                AdjustListBoxItemHeight(); 
            }
 
            /// <devdoc>
            ///     Performs the actual add of new items.  This is invoked by the
            ///     add button as well as the insert key on the list box.
            /// </devdoc> 
            private void PerformAdd() {
                CreateAndAddInstance(NewItemTypes[0]); 
            } 

            /// <devdoc> 
            ///     Performs a remove by deleting all items currently selected in
            ///     the list box.  This is called by the delete button as well as
            ///     the delete key on the list box.
            /// </devdoc> 
            private void PerformRemove() {
                int index = listbox.SelectedIndex; 
 
                if (index != -1) {
                    SuspendEnabledUpdates(); 
                    try {

                        // single object selected or multiple ?
                        if(listbox.SelectedItems.Count > 1) { 
                            ArrayList toBeDeleted = new ArrayList(listbox.SelectedItems);
                            foreach (ListItem item in toBeDeleted) 
                            { 
                                RemoveInternal(item);
                            } 
                        } else {
                            RemoveInternal((ListItem)listbox.SelectedItem);
                        }
                        // set the new selected index 
                        if (index < listbox.Items.Count) {
                            listbox.SelectedIndex = index; 
                        } 
                        else if (listbox.Items.Count > 0) {
                            listbox.SelectedIndex = listbox.Items.Count - 1; 
                        }
                    }
                    finally {
                        ResumeEnabledUpdates(true); 
                    }
                } 
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.PropertyGrid_propertyValueChanged"]/*' /> 
            /// <devdoc>
            ///      When something in the properties window changes, we update pertinent text here.
            /// </devdoc>
            private void PropertyGrid_propertyValueChanged(object sender, PropertyValueChangedEventArgs e) { 

                dirty = true; 
 
                // Refresh selected listbox item so that it picks up any name change
                SuspendEnabledUpdates(); 
                try {
                    listbox.RefreshItem(listbox.SelectedIndex);
                }
                finally { 
                    ResumeEnabledUpdates(false);
                } 
 
                // if a property changes, invalidate the grid in case
                // it affects the item's name. 
                UpdateItemWidths(null);
                listbox.Invalidate();

                // also update the string above the grid. 
                propertiesLabel.Text = SR.GetString(SR.CollectionEditorProperties, GetDisplayText((ListItem)listbox.SelectedItem));
            } 
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.RemoveInternal"]/*' />
            /// <devdoc> 
            ///      Used to actually remove the items, one by one.
            /// </devdoc>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
            private void RemoveInternal(ListItem item) { 
                if (item != null) {
 
                    editor.OnItemRemoving(item.Value); 

                    dirty = true; 
                    //ListItem item = (ListItem)listbox.Items[index];

                    if (createdItems != null && createdItems.Contains(item.Value)) {
                        DestroyInstance(item.Value); 
                        createdItems.Remove(item.Value);
                        listbox.Items.Remove(item); 
                    } 
                    else {
                        try { 
                            if (CanRemoveInstance(item.Value)) {
                                if (removedItems == null) {
                                    removedItems = new ArrayList();
                                } 
                                removedItems.Add(item.Value);
                                listbox.Items.Remove(item); 
                            } else { 
                                throw new Exception(SR.GetString(SR.CollectionEditorCantRemoveItem, GetDisplayText(item)));
                            } 
                        }
                        catch (Exception ex) {
                            DisplayError(ex);
                        } 
                    }
                    // othewise go through the entire list 
                    UpdateItemWidths(null); 

 
                }
            }

 

 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.RemoveButton_click"]/*' /> 
            /// <devdoc>
            ///      Removes the selected item. 
            /// </devdoc>
            private void RemoveButton_click(object sender, EventArgs e) {
                PerformRemove();
 
                // enabling/disabling the buttons has moved the focus to the OK button, move it back to the sender
                Control ctrlSender = (Control)sender; 
                if(ctrlSender.Enabled) { 
                    ctrlSender.Focus ();
                } 
            }

            /// <devdoc>
            /// used to prevent flicker when playing with the list box selection 
            /// call resume when done.  Calls to UpdateEnabled will return silently until Resume is called
            /// </devdoc> 
            private void ResumeEnabledUpdates(bool updateNow){ 
                 suspendEnabledCount--;
 
                 Debug.Assert(suspendEnabledCount >= 0, "Mismatch suspend/resume enabled");

                 if (updateNow) {
                     UpdateEnabled(); 
                 }
                 else { 
                     this.BeginInvoke(new MethodInvoker(this.UpdateEnabled)); 
                 }
            } 
            /// <devdoc>
            /// used to prevent flicker when playing with the list box selection
            /// call resume when done.  Calls to UpdateEnabled will return silently until Resume is called
            /// </devdoc> 
            private void SuspendEnabledUpdates(){
               suspendEnabledCount++; 
            } 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.ShowEditorDialog"]/*' />
            /// <devdoc> 
            ///    <para>
            ///       Called to show the dialog via the IWindowsFormsEditorService
            ///    </para>
            /// </devdoc> 
            protected internal override DialogResult ShowEditorDialog(IWindowsFormsEditorService edSvc) {
              IComponentChangeService cs = null; 
              DialogResult result = DialogResult.OK; 
              try {
 
                  cs = (IComponentChangeService)editor.Context.GetService(typeof(IComponentChangeService));

                  if (cs != null) {
                      cs.ComponentChanged += new ComponentChangedEventHandler(this.OnComponentChanged); 
                  }
 
                  // This is cached across requests, so reset the initial focus. 
                  ActiveControl = listbox;
                  //SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(this.OnSysColorChange); 
                  result = base.ShowEditorDialog(edSvc);
                  //SystemEvents.UserPreferenceChanged -= new UserPreferenceChangedEventHandler(this.OnSysColorChange);
              }
              finally{ 

                  if (cs != null) { 
                      cs.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged); 
                  }
              } 
              return result;
            }

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.UpButton_click"]/*' /> 
            /// <devdoc>
            ///      Moves an item up one in the list box. 
            /// </devdoc> 
            private void UpButton_click(object sender, EventArgs e) {
                int index = listbox.SelectedIndex; 
                if (index == 0)
                    return;

                dirty = true; 
                try {
                    SuspendEnabledUpdates(); 
                    int ti = listbox.TopIndex; 
                    object itemMove = listbox.Items[index];
                    listbox.Items[index] = listbox.Items[index-1]; 
                    listbox.Items[index-1] = itemMove;

                    if (ti > 0)
                        listbox.TopIndex = ti - 1; 

                    listbox.ClearSelected(); 
                    listbox.SelectedIndex = index - 1; 

                    // enabling/disabling the buttons has moved the focus to the OK button, move it back to the sender 
                    Control ctrlSender = (Control)sender;

                    if (ctrlSender.Enabled) {
                        ctrlSender.Focus (); 
                    }
                } 
                finally { 

                    ResumeEnabledUpdates(true); 
                }

            }
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.UpdateEnabled"]/*' />
            /// <devdoc> 
            ///      Updates the set of enabled buttons. 
            /// </devdoc>
            private void UpdateEnabled() { 
                if (suspendEnabledCount > 0) {
                    // We're in the midst of a suspend/resume block  Resume should call us back.
                    return;
                } 

                bool editEnabled = (listbox.SelectedItem != null) && this.CollectionEditable; 
                removeButton.Enabled = editEnabled && AllowRemoveInstance(((ListItem) listbox.SelectedItem).Value); 
                upButton.Enabled = editEnabled && listbox.Items.Count > 1;
                downButton.Enabled = editEnabled && listbox.Items.Count > 1; 
                propertyBrowser.Enabled = editEnabled;
                addButton.Enabled = this.CollectionEditable;

                if (listbox.SelectedItem != null) { 
                    object[] items;
 
                    // If we are to create new instances from the items, then we must wrap them in an outer object. 
                    // otherwise, the user will be presented with a batch of read only properties, which isn't terribly
                    // useful. 
                    //
                    if (IsImmutable) {
                        items = new object[] {new SelectionWrapper(CollectionType, CollectionItemType, listbox, listbox.SelectedItems)};
                    } 
                    else {
                        items = new object[listbox.SelectedItems.Count]; 
                        for (int i = 0; i < items.Length; i++) { 
                            items[i] = ((ListItem)listbox.SelectedItems[i]).Value;
                        } 
                    }

                    int selectedItemCount = listbox.SelectedItems.Count;
                    if ((selectedItemCount == 1) || (selectedItemCount == -1)) { 
                        // handle both single select listboxes and a single item selected in a multi-select listbox
                        propertiesLabel.Text = SR.GetString(SR.CollectionEditorProperties, GetDisplayText((ListItem)listbox.SelectedItem)); 
                    } 
                    else {
                        propertiesLabel.Text = SR.GetString(SR.CollectionEditorPropertiesMultiSelect); 
                    }

                    if (editor.IsAnyObjectInheritedReadOnly(items)) {
                        propertyBrowser.SelectedObjects = null; 
                        propertyBrowser.Enabled = false;
                        removeButton.Enabled = false; 
                        upButton.Enabled = false; 
                        downButton.Enabled = false;
                        propertiesLabel.Text = SR.GetString(SR.CollectionEditorInheritedReadOnlySelection); 
                    }
                    else {
                        propertyBrowser.Enabled = true;
                        propertyBrowser.SelectedObjects = items; 
                    }
                } 
                else { 
                    propertiesLabel.Text = SR.GetString(SR.CollectionEditorPropertiesNone);
                    propertyBrowser.SelectedObject = null; 
                }
            }

 

 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper"]/*' /> 
            /// <devdoc>
            ///     This class implements a custom type descriptor that is used to provide properties for the set of 
            ///     selected items in the collection editor.  It provides a single property that is equivalent
            ///     to the editor's collection item type.
            /// </devdoc>
            private class SelectionWrapper : PropertyDescriptor, ICustomTypeDescriptor { 
                private Type collectionType;
                private Type collectionItemType; 
                private Control control; 
                private ICollection collection;
                private PropertyDescriptorCollection properties; 
                private object value;

                public SelectionWrapper(Type collectionType, Type collectionItemType, Control control, ICollection collection) :
                base("Value", 
                     new Attribute[] {new CategoryAttribute(collectionItemType.Name)}
                    ) { 
                    this.collectionType = collectionType; 
                    this.collectionItemType = collectionItemType;
                    this.control = control; 
                    this.collection = collection;
                    this.properties = new PropertyDescriptorCollection(new PropertyDescriptor[] {this});

                    Debug.Assert(collection.Count > 0, "We should only be wrapped if there is a selection"); 
                    value = this;
 
                    // In a multiselect case, see if the values are different.  If so, 
                    // NULL our value to represent indeterminate.
                    // 
                    foreach (ListItem li in collection) {
                        if (value == this) {
                            value = li.Value;
                        } 
                        else {
                            object nextValue = li.Value; 
                            if (value != null) { 
                                if (nextValue == null) {
                                    value = null; 
                                    break;
                                }
                                else {
                                    if (!value.Equals(nextValue)) { 
                                        value = null;
                                        break; 
                                    } 
                                }
                            } 
                            else {
                                if (nextValue != null) {
                                    value = null;
                                    break; 
                                }
                            } 
                        } 
                    }
                } 

                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ComponentType"]/*' />
                /// <devdoc>
                ///    <para> 
                ///       When overridden in a derived class, gets the type of the
                ///       component this property 
                ///       is bound to. 
                ///    </para>
                /// </devdoc> 
                public override Type ComponentType {
                    get {
                        return collectionType;
                    } 
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.IsReadOnly"]/*' /> 
                /// <devdoc>
                ///    <para> 
                ///       When overridden in
                ///       a derived class, gets a value
                ///       indicating whether this property is read-only.
                ///    </para> 
                /// </devdoc>
                public override bool IsReadOnly { 
                    get { 
                        return false;
                    } 
                }

                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.PropertyType"]/*' />
                /// <devdoc> 
                ///    <para>
                ///       When overridden in a derived class, 
                ///       gets the type of the property. 
                ///    </para>
                /// </devdoc> 
                public override Type PropertyType {
                    get {
                        return collectionItemType;
                    } 
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.CanResetValue"]/*' /> 
                /// <devdoc>
                ///    <para> 
                ///       When overridden in a derived class, indicates whether
                ///       resetting the <paramref name="component "/>will change the value of the
                ///    <paramref name="component"/>.
                /// </para> 
                /// </devdoc>
                public override bool CanResetValue(object component) { 
                    return false; 
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.GetValue"]/*' />
                /// <devdoc>
                ///    <para>
                ///       When overridden in a derived class, gets the current 
                ///       value
                ///       of the 
                ///       property on a component. 
                ///    </para>
                /// </devdoc> 
                public override object GetValue(object component) {
                    return value;
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ResetValue"]/*' />
                /// <devdoc> 
                ///    <para> 
                ///       When overridden in a derived class, resets the
                ///       value 
                ///       for this property
                ///       of the component.
                ///    </para>
                /// </devdoc> 
                public override void ResetValue(object component) {
                } 
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.SetValue"]/*' />
                /// <devdoc> 
                ///    <para>
                ///       When overridden in a derived class, sets the value of
                ///       the component to a different value.
                ///    </para> 
                /// </devdoc>
                public override void SetValue(object component, object value) { 
                    this.value = value; 

                    foreach(ListItem li in collection) { 
                        li.Value = value;
                    }
                    control.Invalidate();
                    OnValueChanged(component, EventArgs.Empty); 
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ShouldSerializeValue"]/*' /> 
                /// <devdoc>
                ///    <para> 
                ///       When overridden in a derived class, indicates whether the
                ///       value of
                ///       this property needs to be persisted.
                ///    </para> 
                /// </devdoc>
                public override bool ShouldSerializeValue(object component) { 
                    return false; 
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetAttributes"]/*' />
                /// <devdoc>
                ///     Retrieves an array of member attributes for the given object.
                /// </devdoc> 
                AttributeCollection ICustomTypeDescriptor.GetAttributes() {
                    return TypeDescriptor.GetAttributes(collectionItemType); 
                } 

                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetClassName"]/*' /> 
                /// <devdoc>
                ///     Retrieves the class name for this object.  If null is returned,
                ///     the type name is used.
                /// </devdoc> 
                string ICustomTypeDescriptor.GetClassName() {
                    return collectionItemType.Name; 
                } 

                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetComponentName"]/*' /> 
                /// <devdoc>
                ///     Retrieves the name for this object.  If null is returned,
                ///     the default is used.
                /// </devdoc> 
                string ICustomTypeDescriptor.GetComponentName() {
                    return null; 
                } 

                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetConverter"]/*' /> 
                /// <devdoc>
                ///      Retrieves the type converter for this object.
                /// </devdoc>
                TypeConverter ICustomTypeDescriptor.GetConverter() { 
                    return null;
                } 
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetDefaultEvent"]/*' />
                /// <devdoc> 
                ///     Retrieves the default event.
                /// </devdoc>
                EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() {
                    return null; 
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetDefaultProperty"]/*' /> 
                /// <devdoc>
                ///     Retrieves the default property. 
                /// </devdoc>
                PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() {
                    return this;
                } 

                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetEditor"]/*' /> 
                /// <devdoc> 
                ///      Retrieves the an editor for this object.
                /// </devdoc> 
                object ICustomTypeDescriptor.GetEditor(Type editorBaseType) {
                    return null;
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetEvents"]/*' />
                /// <devdoc> 
                ///     Retrieves an array of events that the given component instance 
                ///     provides.  This may differ from the set of events the class
                ///     provides.  If the component is sited, the site may add or remove 
                ///     additional events.
                /// </devdoc>
                EventDescriptorCollection ICustomTypeDescriptor.GetEvents() {
                    return EventDescriptorCollection.Empty; 
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetEvents1"]/*' /> 
                /// <devdoc>
                ///     Retrieves an array of events that the given component instance 
                ///     provides.  This may differ from the set of events the class
                ///     provides.  If the component is sited, the site may add or remove
                ///     additional events.  The returned array of events will be
                ///     filtered by the given set of attributes. 
                /// </devdoc>
                EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) { 
                    return EventDescriptorCollection.Empty; 
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetProperties"]/*' />
                /// <devdoc>
                ///     Retrieves an array of properties that the given component instance
                ///     provides.  This may differ from the set of properties the class 
                ///     provides.  If the component is sited, the site may add or remove
                ///     additional properties. 
                /// </devdoc> 
                PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() {
                    return properties; 
                }

                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetProperties1"]/*' />
                /// <devdoc> 
                ///     Retrieves an array of properties that the given component instance
                ///     provides.  This may differ from the set of properties the class 
                ///     provides.  If the component is sited, the site may add or remove 
                ///     additional properties.  The returned array of properties will be
                ///     filtered by the given set of attributes. 
                /// </devdoc>
                PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) {
                    return properties;
                } 

                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetPropertyOwner"]/*' /> 
                /// <devdoc> 
                ///     Retrieves the object that directly depends on this value being edited.  This is
                ///     generally the object that is required for the PropertyDescriptor's GetValue and SetValue 
                ///     methods.  If 'null' is passed for the PropertyDescriptor, the ICustomComponent
                ///     descripotor implemementation should return the default object, that is the main
                ///     object that exposes the properties and attributes,
                /// </devdoc> 
                object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) {
                    return this; 
                } 
            }
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.ListItem"]/*' />
            /// <devdoc>
            ///      ListItem class.  This is a single entry in our list box.  It contains the value we're editing
            ///      as well as accessors for the type converter and UI editor. 
            /// </devdoc>
            private class ListItem { 
                private object value; 
                private object uiTypeEditor;
                private CollectionEditor parentCollectionEditor; 

                public ListItem(CollectionEditor parentCollectionEditor, object value) {
                    this.value = value;
                    this.parentCollectionEditor = parentCollectionEditor; 
                }
 
                public override string ToString() { 
                    return parentCollectionEditor.GetDisplayText(this.value);
                } 

                public UITypeEditor Editor {
                    get {
                        if (uiTypeEditor == null) { 
                            uiTypeEditor = TypeDescriptor.GetEditor(value, typeof(UITypeEditor));
                            if (uiTypeEditor == null) { 
                                uiTypeEditor = this; 
                            }
                        } 

                        if (uiTypeEditor != this) {
                            return (UITypeEditor) uiTypeEditor;
                        } 

                        return null; 
                    } 
                }
 
                public object Value {
                    get {
                        return value;
                    } 
                    set {
                        uiTypeEditor = null; 
                        this.value = value; 
                    }
                } 
            }

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.TypeMenuItem"]/*' />
            /// <devdoc> 
            ///      Menu items we attach to the drop down menu if there are multiple
            ///      types the collection editor can create. 
            /// </devdoc> 
            private class TypeMenuItem : ToolStripMenuItem {
                Type itemType; 

                public TypeMenuItem(Type itemType, EventHandler handler) :
                base(itemType.Name, null, handler) {
                    this.itemType = itemType; 
                }
 
                public Type ItemType { 
                    get {
                        return itemType; 
                    }
                }
            }
        } 

 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.ListItemListBox"]/*' /> 
        /// <devdoc>
        ///      List box filled with ListItem objects representing the collection. 
        /// </devdoc>
        internal class FilterListBox : ListBox {

 
            private PropertyGrid grid;
            private Message      lastKeyDown; 
 
            private PropertyGrid PropertyGrid {
                get { 
                    if (grid == null) {
                        foreach (Control c in Parent.Controls) {
                            if (c is PropertyGrid) {
                                grid = (PropertyGrid)c; 
                                break;
                            } 
                        } 
                    }
                    return grid; 
                }

            }
 
            // Expose the protected RefreshItem() method so that CollectionEditor can use it
            public new void RefreshItem(int index) { 
                base.RefreshItem(index); 
            }
 
            protected override void WndProc(ref Message m) {
                switch (m.Msg) {
                    case NativeMethods.WM_KEYDOWN:
                        this.lastKeyDown = m; 

                        // the first thing the ime does on a key it cares about is send a VK_PROCESSKEY, 
                        // so we use that to sling focus to the grid. 
                        //
                        if ((int)m.WParam == NativeMethods.VK_PROCESSKEY) { 
                            if (PropertyGrid != null) {
                                PropertyGrid.Focus();
                                UnsafeNativeMethods.SetFocus(new HandleRef(PropertyGrid, PropertyGrid.Handle));
                                Application.DoEvents(); 
                            }
                            else { 
                                break; 
                            }
 
                            if(PropertyGrid.Focused || PropertyGrid.ContainsFocus) {
                                // recreate the keystroke to the newly activated window
                                NativeMethods.SendMessage(UnsafeNativeMethods.GetFocus(), NativeMethods.WM_KEYDOWN, lastKeyDown.WParam, lastKeyDown.LParam);
                            } 
                        }
                        break; 
 
                    case NativeMethods.WM_CHAR:
 
                        if ((Control.ModifierKeys & (Keys.Control | Keys.Alt)) != 0) {
                            break;
                        }
 
                        if (PropertyGrid != null) {
                            PropertyGrid.Focus(); 
                            UnsafeNativeMethods.SetFocus(new HandleRef(PropertyGrid, PropertyGrid.Handle)); 
                            Application.DoEvents();
                        } 
                        else {
                            break;
                        }
 
                        // Make sure we changed focus properly
                        // recreate the keystroke to the newly activated window 
                        // 
                        if (PropertyGrid.Focused || PropertyGrid.ContainsFocus) {
                            IntPtr hWnd = UnsafeNativeMethods.GetFocus(); 
                            NativeMethods.SendMessage(hWnd, NativeMethods.WM_KEYDOWN, lastKeyDown.WParam, lastKeyDown.LParam);
                            NativeMethods.SendMessage(hWnd, NativeMethods.WM_CHAR, m.WParam, m.LParam);
                            return;
                        } 
                        break;
 
                } 
                base.WndProc(ref m);
            } 

        }

 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       The <see cref='System.ComponentModel.Design.CollectionEditor.CollectionForm'/>
        ///       provides a modal dialog for editing the 
        ///       contents of a collection.
        ///    </para>
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors")] //breaking change 
        protected abstract class CollectionForm : Form {
 
            // Manipulation of the collection. 
            //
            private CollectionEditor       editor; 
            private object                 value;
            private short                  editableState = EditableDynamic;

            private const short            EditableDynamic = 0; 
            private const short            EditableYes     = 1;
            private const short            EditableNo      = 2; 
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.CollectionForm"]/*' />
            /// <devdoc> 
            ///    <para>
            ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.CollectionEditor.CollectionForm'/> class.
            ///    </para>
            /// </devdoc> 
            public CollectionForm(CollectionEditor editor) {
                this.editor = editor; 
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.CollectionItemType"]/*' /> 
            /// <devdoc>
            ///    <para>
            ///       Gets or sets the data type of each item in the collection.
            ///    </para> 
            /// </devdoc>
            protected Type CollectionItemType { 
                get { 
                    return editor.CollectionItemType;
                } 
            }

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.CollectionType"]/*' />
            /// <devdoc> 
            ///    <para>
            ///       Gets or sets the type of the collection. 
            ///    </para> 
            /// </devdoc>
            protected Type CollectionType { 
                get {
                    return editor.CollectionType;
                }
            } 

            /// <internalonly/> 
            internal virtual bool CollectionEditable { 
                get {
                    if (editableState != EditableDynamic) { 
                        return editableState == EditableYes;
                    }

                    bool editable = typeof(IList).IsAssignableFrom(editor.CollectionType); 

                    if (editable) { 
                        IList list = EditValue as IList; 
                        if (list != null) {
                            return !list.IsReadOnly; 
                        }
                    }
                    return editable;
                } 
                set {
                    if (value) { 
                        editableState = EditableYes; 
                    }
                    else { 
                        editableState = EditableNo;
                    }
                }
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.Context"]/*' /> 
            /// <devdoc> 
            ///    <para>
            ///       Gets or sets a type descriptor that indicates the current context. 
            ///    </para>
            /// </devdoc>
            protected ITypeDescriptorContext Context {
                get { 
                    return editor.Context;
                } 
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.EditValue"]/*' /> 
            /// <devdoc>
            ///    <para>Gets or sets the value of the item being edited.</para>
            /// </devdoc>
            public object EditValue { 
                get {
                    return value; 
                } 
                set {
                    this.value = value; 
                    OnEditValueChanged();
                }
            }
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.Items"]/*' />
            /// <devdoc> 
            ///    <para> 
            ///       Gets or sets the
            ///       array of items this form is to display. 
            ///    </para>
            /// </devdoc>
            protected object[] Items {
                get { 
                    return editor.GetItems(EditValue);
                } 
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
                set {
                    // Request our desire to make a change. 
                    //
                    bool canChange = false;
                    try {
                        canChange = Context.OnComponentChanging(); 
                    } catch (Exception ex) {
                        if(!ClientUtils.IsCriticalException(ex)) { 
                            DisplayError(ex); 
                        } else {
                            throw; 
                        }
                    }
                    if (canChange) {
                        object newValue = editor.SetItems(EditValue, value); 
                        if (newValue != EditValue) {
                            EditValue = newValue; 
                        } 
                        Context.OnComponentChanged();
                    } 

                }
            }
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.NewItemTypes"]/*' />
            /// <devdoc> 
            ///    <para> 
            ///       Gets or sets the available item types that can be created for this
            ///       collection. 
            ///    </para>
            /// </devdoc>
            protected Type[] NewItemTypes {
                get { 
                    return editor.NewItemTypes;
                } 
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.CanRemoveInstance"]/*' /> 
            /// <devdoc>
            ///    <para>Gets or sets a value indicating whether original members of the collection
            ///       can be removed.</para>
            /// </devdoc> 
            protected bool CanRemoveInstance(object value) {
                return editor.CanRemoveInstance(value); 
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.CanSelectMultipleInstances"]/*' /> 
            /// <devdoc>
            ///    <para>Gets or sets a value indicating whether multiple collection members can be
            ///       selected.</para>
            /// </devdoc> 
            protected virtual bool CanSelectMultipleInstances() {
                return editor.CanSelectMultipleInstances(); 
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.CreateInstance"]/*' /> 
            /// <devdoc>
            ///    <para>
            ///       Creates a new instance of the specified collection item type.
            ///    </para> 
            /// </devdoc>
            protected object CreateInstance(Type itemType) { 
                return editor.CreateInstance(itemType); 
            }
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.DestroyInstance"]/*' />
            /// <devdoc>
            ///    <para>
            ///       Destroys the specified instance of the object. 
            ///    </para>
            /// </devdoc> 
            protected void DestroyInstance(object instance) { 
                editor.DestroyInstance(instance);
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.DisplayError"]/*' />
            /// <devdoc>
            ///    Displays the given exception to the user. 
            /// </devdoc>
            protected virtual void DisplayError(Exception e) { 
                IUIService uis = (IUIService)GetService(typeof(IUIService)); 
                if (uis != null) {
                    uis.ShowError(e); 
                }
                else {
                    string message = e.Message;
                    if (message == null || message.Length == 0) { 
                        message = e.ToString();
                    } 
                    RTLAwareMessageBox.Show(null, message, null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, 0); 
                }
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.GetService"]/*' />
            /// <devdoc>
            ///    <para> 
            ///       Gets the requested service, if it is available.
            ///    </para> 
            /// </devdoc> 
            protected override object GetService(Type serviceType) {
                return editor.GetService(serviceType); 
            }

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.ShowEditorDialog"]/*' />
            /// <devdoc> 
            ///    <para>
            ///       Called to show the dialog via the IWindowsFormsEditorService 
            ///    </para> 
            /// </devdoc>
            protected internal virtual DialogResult ShowEditorDialog(IWindowsFormsEditorService edSvc) { 
                return edSvc.ShowDialog(this);
            }

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.OnEditValueChanged"]/*' /> 
            /// <devdoc>
            ///    <para> 
            ///       This is called when the value property in 
            ///       the <see cref='System.ComponentModel.Design.CollectionEditor.CollectionForm'/>
            ///       has changed. 
            ///    </para>
            /// </devdoc>
            protected abstract void OnEditValueChanged();
        } 

 
     internal class PropertyGridSite : ISite { 

            private IServiceProvider sp; 
            private IComponent comp;
            private bool       inGetService = false;

            public PropertyGridSite(IServiceProvider sp, IComponent comp) { 
                this.sp = sp;
                this.comp = comp; 
            } 

             /** The component sited by this component site. */ 
            /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Component"]/*' />
            /// <devdoc>
            ///    <para>When implemented by a class, gets the component associated with the <see cref='System.ComponentModel.ISite'/>.</para>
            /// </devdoc> 
            public IComponent Component {get {return comp;}}
 
            /** The container in which the component is sited. */ 
            /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Container"]/*' />
            /// <devdoc> 
            /// <para>When implemented by a class, gets the container associated with the <see cref='System.ComponentModel.ISite'/>.</para>
            /// </devdoc>
            public IContainer Container {get {return null;}}
 
            /** Indicates whether the component is in design mode. */
            /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.DesignMode"]/*' /> 
            /// <devdoc> 
            ///    <para>When implemented by a class, determines whether the component is in design mode.</para>
            /// </devdoc> 
            public  bool DesignMode {get {return false;}}

            /**
             * The name of the component. 
             */
                /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Name"]/*' /> 
                /// <devdoc> 
                ///    <para>When implemented by a class, gets or sets the name of
                ///       the component associated with the <see cref='System.ComponentModel.ISite'/>.</para> 
                /// </devdoc>
                public String Name {
                        get {return null;}
                        set {} 
                }
 
            public object GetService(Type t) { 
                if (!inGetService && sp != null) {
                    try { 
                        inGetService = true;
                        return sp.GetService(t);
                    }
                    finally { 
                        inGetService = false;
                    } 
                } 
                return null;
            } 

        }
    }
} 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="CollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.ComponentModel.Design {
    using System.Design; 
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.Remoting.Activation;
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.ComponentModel.Design.Serialization;
    using System; 
    using System.Collections; 
    using Microsoft.Win32;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging; 
    using System.IO;
    using System.Drawing.Design; 
    using System.Reflection; 
    using System.Windows.Forms;
    using System.Windows.Forms.ComponentModel; 
    using System.Windows.Forms.Design;
    using System.Windows.Forms.VisualStyles;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Globalization; 

 
    /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor"]/*' /> 
    /// <devdoc>
    ///    <para>Provides a generic editor for most any collection.</para> 
    /// </devdoc>
    public class CollectionEditor : UITypeEditor {
        private Type                   type;
        private Type                   collectionItemType; 
        private Type[]                 newItemTypes;
        private ITypeDescriptorContext currentContext; 
 
        private bool                   ignoreChangedEvents = false;
        private bool                   ignoreChangingEvents = false; 

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CancelChanges"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Useful for derived classes to do processing when cancelling changes
        ///    </para> 
        /// </devdoc> 
        protected virtual void CancelChanges() {
        } 

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditor"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.CollectionEditor'/> class using the
        ///       specified collection type. 
        ///    </para> 
        /// </devdoc>
        public CollectionEditor(Type type) { 
            this.type = type;
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionItemType"]/*' /> 
        /// <devdoc>
        ///    <para>Gets or sets the data type of each item in the collection.</para> 
        /// </devdoc> 
        protected Type CollectionItemType {
            get { 
                if (collectionItemType == null) {
                    collectionItemType = CreateCollectionItemType();
                }
                return collectionItemType; 
            }
        } 
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionType"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets or sets the type of the collection.
        ///    </para>
        /// </devdoc> 
        protected Type CollectionType {
            get { 
                return type; 
            }
        } 

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.Context"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Gets or sets a type descriptor that indicates the current context.
        ///    </para> 
        /// </devdoc> 
        protected ITypeDescriptorContext Context {
            get { 
                return currentContext;
            }
        }
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.NewItemTypes"]/*' />
        /// <devdoc> 
        ///    <para>Gets or sets 
        ///       the available item types that can be created for this collection.</para>
        /// </devdoc> 
        protected Type[] NewItemTypes {
            get {
                if (newItemTypes == null) {
                    newItemTypes = CreateNewItemTypes(); 
                }
                return newItemTypes; 
            } 
        }
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.HelpTopic"]/*' />
        /// <devdoc>
        ///    <para>Gets the help topic to display for the dialog help button or pressing F1. Override to
        ///          display a different help topic.</para> 
        /// </devdoc>
        protected virtual string HelpTopic { 
            get { 
                return "net.ComponentModel.CollectionEditor";
            } 
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CanRemoveInstance"]/*' />
        /// <devdoc> 
        ///    <para>Gets or sets a value indicating whether original members of the collection can be removed.</para>
        /// </devdoc> 
        protected virtual bool CanRemoveInstance(object value) { 
            IComponent comp = value as IComponent;
            if (comp != null) { 
                // Make sure the component is not being inherited -- we can't delete these!
                //
                InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(comp)[typeof(InheritanceAttribute)];
                if (ia != null && ia.InheritanceLevel != InheritanceLevel.NotInherited) { 
                    return false;
                } 
            } 

            return true; 
        }


        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CanSelectMultipleInstances"]/*' /> 
        /// <devdoc>
        ///    <para>Gets or sets a value indicating whether multiple collection members can be 
        ///       selected.</para> 
        /// </devdoc>
        protected virtual bool CanSelectMultipleInstances() { 
            return true;
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CreateCollectionForm"]/*' /> 
        /// <devdoc>
        ///    <para>Creates a 
        ///       new form to show the current collection.</para> 
        /// </devdoc>
        protected virtual CollectionForm CreateCollectionForm() { 
            return new CollectionEditorCollectionForm(this);
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CreateInstance"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Creates a new instance of the specified collection item type. 
        ///    </para>
        /// </devdoc> 
        protected virtual object CreateInstance(Type itemType) {
            return CollectionEditor.CreateInstance(itemType, (IDesignerHost)GetService(typeof(IDesignerHost)), null);
        }
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.GetObjectsFromInstance"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       This Function gets the object from the givem object. The input is an arrayList returned as an Object.
        ///       The output is a arraylist which contains the individual objects that need to be created. 
        ///    </para>
        /// </devdoc>
        protected virtual IList GetObjectsFromInstance(object instance) {
            ArrayList ret = new ArrayList(); 
            ret.Add(instance);
            return ret; 
        } 

        internal static object CreateInstance(Type itemType, IDesignerHost host, string name) { 
            object instance = null;

            if (typeof(IComponent).IsAssignableFrom(itemType)) {
                if (host != null) { 
                    instance = host.CreateComponent(itemType, (string)name);
 
                    // Set component defaults 
                    if (host != null) {
                        IComponentInitializer init = host.GetDesigner((IComponent)instance) as IComponentInitializer; 
                        if (init != null) {
                            init.InitializeNewComponent(null);
                        }
                    } 
                }
            } 
 
            if (instance == null) {
                instance = TypeDescriptor.CreateInstance(host, itemType, null, null); 
            }

            return instance;
        } 

 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.GetDisplayText"]/*' /> 
        /// <devdoc>
        ///      Retrieves the display text for the given list item. 
        /// </devdoc>
        protected virtual string GetDisplayText(object value) {
            string text;
 
            if (value == null) {
                return string.Empty; 
            } 

            PropertyDescriptor prop = TypeDescriptor.GetProperties(value)["Name"]; 
            if (prop != null && prop.PropertyType == typeof(string)) {
                text = (string) prop.GetValue( value );
                if (text != null && text.Length > 0) {
                    return text; 
                }
            } 
 
            prop = TypeDescriptor.GetDefaultProperty(CollectionType);
            if (prop != null && prop.PropertyType == typeof(string)) { 
                text = (string)prop.GetValue(value);
                if (text != null && text.Length > 0) {
                    return text;
                } 
            }
 
            text = TypeDescriptor.GetConverter(value).ConvertToString(value); 

            if (text == null || text.Length == 0) { 
                text = value.GetType().Name;
            }

            return text; 
        }
 
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CreateCollectionItemType"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets an instance of
        ///       the data type this collection contains.
        ///    </para> 
        /// </devdoc>
        protected virtual Type CreateCollectionItemType() { 
            PropertyInfo[] props = TypeDescriptor.GetReflectionType(CollectionType).GetProperties(BindingFlags.Public | BindingFlags.Instance); 

            for (int i = 0; i < props.Length; i++) { 
                if (props[i].Name.Equals("Item") || props[i].Name.Equals("Items")) {
                    return props[i].PropertyType;
                }
            } 

            // Couldn't find anything.  Return Object 
 
            Debug.Fail("Collection " + CollectionType.FullName + " contains no Item or Items property so we cannot display and edit any values");
            return typeof(object); 
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CreateNewItemTypes"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the data 
        ///       types this collection editor can create. 
        ///    </para>
        /// </devdoc> 
        protected virtual Type[] CreateNewItemTypes() {
            return new Type[] {CollectionItemType};
        }
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.DestroyInstance"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Destroys the specified instance of the object.
        ///    </para> 
        /// </devdoc>
        protected virtual void DestroyInstance(object instance) {
            IComponent compInstance = instance as IComponent;
            if (compInstance != null) { 
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                if (host != null) { 
                    host.DestroyComponent(compInstance); 
                }
                else { 
                    compInstance.Dispose();
                }
            }
            else { 
                IDisposable dispInstance = instance as IDisposable;
                if (dispInstance != null) { 
                    dispInstance.Dispose(); 
                }
            } 
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.EditValue"]/*' />
        /// <devdoc> 
        ///    <para>Edits the specified object value using the editor style
        ///       provided by <see cref='System.ComponentModel.Design.CollectionEditor.GetEditStyle'/>.</para> 
        /// </devdoc> 
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) { 
            if (provider != null) {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

                if (edSvc != null) { 
                    this.currentContext = context;
 
                    // Always create a new CollectionForm.  We used to do reuse the form in V1 and Everett 
                    // but this implies that the form will never be disposed.
                    CollectionForm localCollectionForm = CreateCollectionForm(); 

                    ITypeDescriptorContext lastContext = currentContext;
                    localCollectionForm.EditValue = value;
                    ignoreChangingEvents = false; 
                    ignoreChangedEvents = false;
                    DesignerTransaction trans = null; 
 
                    bool commitChange = true;
                    IComponentChangeService cs = null; 
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));

                    try {
                        try { 
                            if (host != null) {
                                trans = host.CreateTransaction(SR.GetString(SR.CollectionEditorUndoBatchDesc, CollectionItemType.Name)); 
                            } 
                        }
                        catch(CheckoutException cxe) { 
                            if (cxe == CheckoutException.Canceled)
                                return value;

                            throw cxe; 
                        }
 
                        cs = host != null ? (IComponentChangeService)host.GetService(typeof(IComponentChangeService)) : null; 

                        if (cs != null) { 
                            cs.ComponentChanged += new ComponentChangedEventHandler(this.OnComponentChanged);
                            cs.ComponentChanging += new ComponentChangingEventHandler(this.OnComponentChanging);
                        }
 
                        if (localCollectionForm.ShowEditorDialog(edSvc) == DialogResult.OK) {
                            value = localCollectionForm.EditValue; 
                        } 
                        else {
                            commitChange = false; 
                        }
                    }
                    finally {
                        localCollectionForm.EditValue = null; 
                        this.currentContext = lastContext;
                        if (trans != null) { 
                            if (commitChange) { 
                                trans.Commit();
                            } 
                            else {
                                trans.Cancel();
                            }
                        } 

                        if (cs != null) { 
                            cs.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged); 
                            cs.ComponentChanging -= new ComponentChangingEventHandler(this.OnComponentChanging);
                        } 

                        localCollectionForm.Dispose();
                    }
                } 
            }
 
 
            return value;
        } 

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.GetEditStyle"]/*' />
        /// <devdoc>
        ///    <para>Gets the editing style of the Edit method.</para> 
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust. 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            return UITypeEditorEditStyle.Modal;
        } 

        private bool IsAnyObjectInheritedReadOnly(object[] items) {
            // If the object implements IComponent, and is not sited, check with
            // the inheritance service (if it exists) to see if this is a component 
            // that is being inherited from another class.  If it is, then we do
            // not want to place it in the collection editor.  If the inheritance service 
            // chose not to site the component, that indicates it should be hidden from 
            // the user.
 
            IInheritanceService iSvc = null;
            bool checkISvc = false;

            foreach(object o in items) { 
                IComponent comp = o as IComponent;
                if (comp != null && comp.Site == null) { 
                    if (!checkISvc) { 
                        checkISvc = true;
                        if (Context != null) { 
                            iSvc = (IInheritanceService)Context.GetService(typeof(IInheritanceService));
                        }
                    }
 
                    if (iSvc != null && iSvc.GetInheritanceAttribute(comp).Equals(InheritanceAttribute.InheritedReadOnly)) {
                        return true; 
                    } 
                }
            } 

            return false;
        }
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.GetItems"]/*' />
        /// <devdoc> 
        ///    <para>Converts the specified collection into an array of objects.</para> 
        /// </devdoc>
        protected virtual object[] GetItems(object editValue) { 
            if (editValue != null) {
                // We look to see if the value implements ICollection, and if it does,
                // we set through that.
                // 
                if (editValue is System.Collections.ICollection) {
                    ArrayList list = new ArrayList(); 
 
                    System.Collections.ICollection col = (System.Collections.ICollection)editValue;
                    foreach(object o in col) { 
                        list.Add(o);
                    }

                    object[] values = new object[list.Count]; 
                    list.CopyTo(values, 0);
                    return values; 
                } 
            }
 
            return new object[0];
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.GetService"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Gets the requested service, if it is available. 
        ///    </para>
        /// </devdoc> 
        protected object GetService(Type serviceType) {
            if (Context != null) {
                return Context.GetService(serviceType);
            } 
            return null;
        } 
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.OnComponentChanged"]/*' />
        /// <devdoc> 
        /// reflect any change events to the instance object
        /// </devdoc>
        private void OnComponentChanged(object sender, ComponentChangedEventArgs e) {
            if (!ignoreChangedEvents && sender != Context.Instance) { 
                ignoreChangedEvents = true;
                Context.OnComponentChanged(); 
            } 
        }
 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.OnComponentChanging"]/*' />
        /// <devdoc>
        ///  reflect any changed events to the instance object
        /// </devdoc> 
        private void OnComponentChanging(object sender, ComponentChangingEventArgs e) {
            if (!ignoreChangingEvents && sender != Context.Instance) { 
                ignoreChangingEvents = true; 
                Context.OnComponentChanging();
            } 
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.OnItemRemoving"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Removes the item from the column header from the listview column header collection 
        ///    </para> 
        /// </devdoc>
        internal virtual void OnItemRemoving(object item) { 
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.SetItems"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Sets 
        ///       the specified collection to have the specified array of items. 
        ///    </para>
        /// </devdoc> 
        protected virtual object SetItems(object editValue, object[] value) {
            if (editValue != null) {
                Array oldValue = (Array)GetItems(editValue);
                bool  valueSame = (oldValue.Length == value.Length); 
                // We look to see if the value implements IList, and if it does,
                // we set through that. 
                // 
                Debug.Assert(editValue is System.Collections.IList, "editValue is not an IList");
                if (editValue is System.Collections.IList) { 
                    System.Collections.IList list = (System.Collections.IList)editValue;

                    list.Clear();
                    for (int i = 0; i < value.Length; i++) { 
                        list.Add(value[i]);
                    } 
                } 
            }
            return editValue; 
        }

        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.ShowHelp"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Called when the help button is clicked. 
        ///    </para> 
        /// </devdoc>
        protected virtual void ShowHelp() { 
            IHelpService helpService = GetService(typeof(IHelpService)) as IHelpService;
            if (helpService != null) {
                helpService.ShowHelpFromKeyword(HelpTopic);
            } 
            else {
                Debug.Fail("Unable to get IHelpService."); 
            } 
        }
 
       internal class SplitButton : Button
        {
            private PushButtonState _state;
            private const int pushButtonWidth = 14; 
            private Rectangle dropDownRectangle = new Rectangle();
            private bool showSplit = false; 
 

 
            public bool ShowSplit
            {
                set
                { 
                    if (value != showSplit)
                    { 
                        showSplit = value; 
                        Invalidate();
                    } 
                }
            }

            private PushButtonState State 
            {
                get 
                { 
                    return _state;
                } 
                set
                {
                    if (!_state.Equals(value))
                    { 
                        _state = value;
                        Invalidate(); 
                    } 
                }
            } 

            public override Size GetPreferredSize(Size proposedSize)
            {
                Size preferredSize = base.GetPreferredSize(proposedSize); 
                if (showSplit && !string.IsNullOrEmpty(Text) && TextRenderer.MeasureText(Text, Font).Width + pushButtonWidth > preferredSize.Width)
                { 
                    return preferredSize + new Size(pushButtonWidth, 0); 
                }
 
                return preferredSize;
            }

            protected override bool IsInputKey(Keys keyData) 
            {
                if (keyData.Equals(Keys.Down) && showSplit) 
                { 
                    return true;
                } 
                else
                {
                    return base.IsInputKey(keyData);
                } 
            }
 
            protected override void OnGotFocus(EventArgs e) 
            {
                if (!showSplit) 
                {
                    base.OnGotFocus(e);
                    return;
                } 

                if (!State.Equals(PushButtonState.Pressed) && !State.Equals(PushButtonState.Disabled)) 
                { 
                    State = PushButtonState.Default;
                } 
            }

            protected override void OnKeyDown(KeyEventArgs kevent)
            { 
                if (kevent.KeyCode.Equals(Keys.Down) && showSplit)
                { 
                    ShowContextMenuStrip(); 
                }
            } 

            protected override void OnLostFocus(EventArgs e)
            {
                if (!showSplit) 
                {
                    base.OnLostFocus(e); 
                    return; 
                }
                if (!State.Equals(PushButtonState.Pressed) && !State.Equals(PushButtonState.Disabled)) 
                {
                    State = PushButtonState.Normal;
                }
            } 

            protected override void OnMouseDown(MouseEventArgs e) 
            { 
                if (!showSplit)
                { 
                    base.OnMouseDown(e);
                    return;
                }
 
                if (dropDownRectangle.Contains(e.Location))
                { 
                    ShowContextMenuStrip(); 
                }
                else 
                {
                    State = PushButtonState.Pressed;
                }
            } 

            protected override void OnMouseEnter(EventArgs e) 
            { 
                if (!showSplit)
                { 
                    base.OnMouseEnter(e);
                    return;
                }
 
                if (!State.Equals(PushButtonState.Pressed) && !State.Equals(PushButtonState.Disabled))
                { 
                    State = PushButtonState.Hot; 
                }
            } 

            protected override void OnMouseLeave(EventArgs e)
            {
                if (!showSplit) 
                {
                    base.OnMouseLeave(e); 
                    return; 
                }
 
                if (!State.Equals(PushButtonState.Pressed) && !State.Equals(PushButtonState.Disabled))
                {
                    if (Focused)
                    { 
                        State = PushButtonState.Default;
                    } 
                    else 
                    {
                        State = PushButtonState.Normal; 
                    }
                }
            }
 
            protected override void OnMouseUp(MouseEventArgs mevent)
            { 
                if (!showSplit) 
                {
                    base.OnMouseUp(mevent); 
                    return;
                }

                if (ContextMenuStrip == null || !ContextMenuStrip.Visible) 
                {
                    SetButtonDrawState(); 
                    if (Bounds.Contains(Parent.PointToClient(Cursor.Position)) && !dropDownRectangle.Contains(mevent.Location)) 
                    {
                        OnClick(new EventArgs()); 
                    }
                }
            }
 
            protected override void OnPaint(PaintEventArgs pevent)
            { 
                base.OnPaint(pevent); 

                if (!showSplit) 
                {
                    return;
                }
 
                Graphics g = pevent.Graphics;
                Rectangle bounds = new Rectangle(0, 0, Width, Height); 
                TextFormatFlags formatFlags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter; 

                ButtonRenderer.DrawButton(g, bounds, State); 

                dropDownRectangle = new Rectangle(bounds.Right - pushButtonWidth - 1, 4, pushButtonWidth, bounds.Height - 8);

 
                if (RightToLeft == RightToLeft.Yes) {
                    dropDownRectangle.X = bounds.Left + 1; 
 
                    g.DrawLine(SystemPens.ButtonHighlight, bounds.Left + pushButtonWidth, 4, bounds.Left + pushButtonWidth, bounds.Bottom -4);
                    g.DrawLine(SystemPens.ButtonHighlight, bounds.Left + pushButtonWidth + 1, 4, bounds.Left + pushButtonWidth + 1, bounds.Bottom -4); 
                    bounds.Offset(pushButtonWidth, 0);
                    bounds.Width = bounds.Width - pushButtonWidth;
                }
                else { 
                    g.DrawLine(SystemPens.ButtonHighlight, bounds.Right - pushButtonWidth, 4, bounds.Right - pushButtonWidth, bounds.Bottom -4);
                    g.DrawLine(SystemPens.ButtonHighlight, bounds.Right - pushButtonWidth - 1, 4, bounds.Right - pushButtonWidth - 1, bounds.Bottom -4); 
                    bounds.Width = bounds.Width - pushButtonWidth; 
                }
 
                PaintArrow(g, dropDownRectangle);

                // If we dont' use mnemonic, set formatFlag to NoPrefix as this will show ampersand.
                if (!UseMnemonic) { 
                    formatFlags = formatFlags | TextFormatFlags.NoPrefix;
                } 
                else if (!ShowKeyboardCues) { 
                    formatFlags = formatFlags | TextFormatFlags.HidePrefix;
                } 

                if (!string.IsNullOrEmpty(this.Text)) {
                    TextRenderer.DrawText(g, Text, Font, bounds, SystemColors.ControlText, formatFlags);
                } 

                if (Focused) { 
                    bounds.Inflate(-4,-4); 
                    //ControlPaint.DrawFocusRectangle(g, bounds);
                } 
            }

            private void PaintArrow(Graphics g, Rectangle dropDownRect) {
                Point middle = new Point(Convert.ToInt32(dropDownRect.Left + dropDownRect.Width / 2), Convert.ToInt32(dropDownRect.Top + dropDownRect.Height / 2)); 

                //if the width is odd - favor pushing it over one pixel right. 
                middle.X += (dropDownRect.Width % 2); 

                Point[] arrow = new Point[] {new Point(middle.X - 2, middle.Y - 1), new Point(middle.X + 3, middle.Y - 1), new Point(middle.X, middle.Y + 2)}; 

                g.FillPolygon(SystemBrushes.ControlText, arrow);
            }
 
            private void ShowContextMenuStrip() {
                State = PushButtonState.Pressed; 
                if (ContextMenuStrip != null) { 
                    ContextMenuStrip.Closed += new ToolStripDropDownClosedEventHandler(ContextMenuStrip_Closed);
                    ContextMenuStrip.Show(this, 0, Height); 
                }
            }

            private void ContextMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e) 
            {
                ContextMenuStrip cms = sender as ContextMenuStrip; 
                if (cms != null) 
                {
                    cms.Closed -= new ToolStripDropDownClosedEventHandler(ContextMenuStrip_Closed); 
                }

                SetButtonDrawState();
            } 

            private void SetButtonDrawState() 
            { 
                if (Bounds.Contains(Parent.PointToClient(Cursor.Position)))
                { 
                    State = PushButtonState.Hot;
                }
                else if (Focused)
                { 
                    State = PushButtonState.Default;
                } 
                else 
                {
                    State = PushButtonState.Normal; 
                }
            }
        }
 

 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm"]/*' /> 
        /// <devdoc>
        ///      This is the collection editor's default implementation of a 
        ///      collection form.
        /// </devdoc>
        private class CollectionEditorCollectionForm : CollectionForm {
 
            private const int               TEXT_INDENT = 1;
            private const int               PAINT_WIDTH = 20; 
            private const int               PAINT_INDENT = 26; 
            private static readonly double  LOG10 = Math.Log(10);
 
            // Manipulation of the collection.
            //
            private ArrayList              createdItems;
            private ArrayList              removedItems; 
            private ArrayList              originalItems;
 
            // Calling Editor 
            private CollectionEditor       editor;
 
            // Dialog UI
            //
            private FilterListBox          listbox;
            private SplitButton            addButton; 
            private Button                 removeButton;
            private Button                 cancelButton; 
            private Button                 okButton; 
            private Button                 downButton;
            private Button                 upButton; 
            private VsPropertyGrid         propertyBrowser;
            private Label                  membersLabel;
            private Label                  propertiesLabel;
            private ContextMenuStrip       addDownMenu; 
            private TableLayoutPanel       okCancelTableLayoutPanel;
            private TableLayoutPanel       overArchingTableLayoutPanel; 
            private TableLayoutPanel       addRemoveTableLayoutPanel; 

            // Prevent flicker when switching selection 
            private int                    suspendEnabledCount = 0;

            // our flag for if something changed
            // 
            private bool                   dirty;
 
            public CollectionEditorCollectionForm(CollectionEditor editor) : base(editor) { 
                this.editor = editor;
                InitializeComponent(); 
                this.Text = SR.GetString(SR.CollectionEditorCaption, CollectionItemType.Name);

                HookEvents();
 

 
                Type[] newItemTypes = NewItemTypes; 
                if (newItemTypes.Length > 1) {
                    EventHandler addDownMenuClick = new EventHandler(this.AddDownMenu_click); 
                    addButton.ShowSplit = true;
                    addDownMenu = new ContextMenuStrip();
                    addButton.ContextMenuStrip = addDownMenu;
                    for (int i = 0; i < newItemTypes.Length; i++) { 
                        addDownMenu.Items.Add(new TypeMenuItem(newItemTypes[i], addDownMenuClick));
                    } 
                } 

                AdjustListBoxItemHeight(); 
            }

            private bool IsImmutable {
                get { 
                    bool immutable = true;
 
                    // We are considered immutable if the converter is defined as requiring a 
                    // create instance or all the properties are read-only.
                    // 
                    if (!TypeDescriptor.GetConverter(CollectionItemType).GetCreateInstanceSupported()) {
                        foreach (PropertyDescriptor p in TypeDescriptor.GetProperties(CollectionItemType)) {
                            if (!p.IsReadOnly) {
                                immutable = false; 
                                break;
                            } 
                        } 
                    }
 
                    return immutable;
                }
            }
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.AddButton_click"]/*' />
            /// <devdoc> 
            ///      Adds a new element to the collection. 
            /// </devdoc>
            private void AddButton_click(object sender, EventArgs e) { 
                PerformAdd();
            }

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.AddDownMenu_click"]/*' /> 
            /// <devdoc>
            ///      Processes a click of the drop down type menu.  This creates a 
            ///      new instance. 
            /// </devdoc>
            private void AddDownMenu_click(object sender, EventArgs e) { 
                if (sender is TypeMenuItem) {
                    TypeMenuItem typeMenuItem = (TypeMenuItem) sender;
                    CreateAndAddInstance(typeMenuItem.ItemType);
                } 
            }
 
            /// <internalonly/> 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.AddItems"]/*' />
            /// <devdoc> 
            ///      This Function adds the individual objects to the ListBox.
            /// </devdoc>
            private void AddItems(IList instances) {
 
                if (createdItems == null) {
                   createdItems = new ArrayList(); 
                } 

                listbox.BeginUpdate(); 
                try {
                    foreach( object instance in instances ){
                        if (instance != null) {
                            dirty = true; 
                            createdItems.Add(instance);
                            ListItem created = new ListItem(editor, instance); 
                            listbox.Items.Add(created); 
                        }
                    } 
                }
                finally {
                    listbox.EndUpdate();
                } 

                if (instances.Count == 1) { 
                    // optimize for the case where we just added one thing... 
                    UpdateItemWidths(listbox.Items[listbox.Items.Count -1] as ListItem);
                } 
                else {
                    // othewise go through the entire list
                    UpdateItemWidths(null);
                } 

                // Select the last item 
                // 
                SuspendEnabledUpdates();
                try { 
                    listbox.ClearSelected();
                    listbox.SelectedIndex = listbox.Items.Count - 1;

                    object[] items = new object[listbox.Items.Count]; 
                    for (int i = 0; i < items.Length; i++) {
                        items[i] = ((ListItem)listbox.Items[i]).Value; 
                    } 
                    Items = items;
 
                    //fringe case -- someone changes the edit value which resets the selindex, we should keep the new index.
                    if (listbox.Items.Count > 0 && listbox.SelectedIndex != listbox.Items.Count - 1) {
                        listbox.ClearSelected();
                        listbox.SelectedIndex = listbox.Items.Count - 1; 
                    }
                } 
 
                finally {
                    ResumeEnabledUpdates(true); 
                }
            }

            private void AdjustListBoxItemHeight() { 
                listbox.ItemHeight = Font.Height + SystemInformation.BorderSize.Width*2;
            } 
 
            /// <devdoc>
            ///     Determines whether removal of a specific list item should be permitted. 
            ///     Used to determine enabled/disabled state of the Remove (X) button.
            ///     Items added after editor was opened may always be removed.
            ///     Items that existed before editor was opened require a call to CanRemoveInstance.
            /// </devdoc> 
            private bool AllowRemoveInstance(object value) {
                if (createdItems != null && createdItems.Contains(value)) { 
                    return true; 
                }
                else { 
                    return CanRemoveInstance(value);
                }
            }
 
            private int CalcItemWidth(Graphics g, ListItem item) {
                int c = listbox.Items.Count; 
                if (c < 2) { 
                    c = 2;  //for c-1 should be greater than zero.
                } 

                SizeF sizeW = g.MeasureString(c.ToString(CultureInfo.CurrentCulture), listbox.Font);

                int charactersInNumber = ((int)(Math.Log((double)(c-1)) / LOG10) + 1); 
                int w = 4 + charactersInNumber * (Font.Height /2);
 
                w = Math.Max(w, (int)Math.Ceiling(sizeW.Width)); 
                w += SystemInformation.BorderSize.Width * 4;
 
                SizeF size = g.MeasureString(GetDisplayText(item), listbox.Font);
                int pic = 0;
                if (item.Editor != null && item.Editor.GetPaintValueSupported()) {
                    pic = PAINT_WIDTH + TEXT_INDENT; 
                }
                return (int)Math.Ceiling(size.Width) + w + pic + SystemInformation.BorderSize.Width * 4; 
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.CancelButton_click"]/*' /> 
            /// <devdoc>
            ///      Aborts changes made in the editor.
            /// </devdoc>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
            private void CancelButton_click(object sender, EventArgs e) {
                try { 
 
                    editor.CancelChanges();
 
                    if (!CollectionEditable || !dirty) {
                        return;
                    }
 
                    dirty = false;
                    listbox.Items.Clear(); 
 
                    if (createdItems != null) {
                        object[] items = createdItems.ToArray(); 
                        if(items.Length > 0 && items[0] is IComponent && ((IComponent)items[0]).Site != null) {
                            // here we bail now because we don't want to do the "undo" manually,
                            // we're part of a trasaction, we've added item, the rollback will be
                            // handled by the undo engine because the component in the collection are sited 
                            // doing it here kills perfs because the undo of the transaction has to rollback the remove and then
                            // rollback the add. This is useless and is only needed for non sited component or other classes 
                            return; 
                        }
                        for (int i=0; i<items.Length; i++) { 
                            DestroyInstance(items[i]);
                        }
                        createdItems.Clear();
                    } 
                    if (removedItems != null) {
                        removedItems.Clear(); 
                    } 

 
                    // Restore the original contents. Because objects get parented during CreateAndAddInstance, the underlying collection
                    // gets changed during add, but not other operations. Not all consumers of this dialog can roll back every single change,
                    // but this will at least roll back the additions, removals and reordering. See ASURT #85470.
                    if (originalItems != null && (originalItems.Count > 0)) { 
                        object[] items = new object[originalItems.Count];
                        for (int i = 0; i < originalItems.Count; i++) { 
                            items[i] = originalItems[i]; 
                        }
                        Items = items; 
                        originalItems.Clear();
                    }
                    else {
                        Items = new object[0]; 
                    }
 
                } 
                catch (Exception ex) {
                    DialogResult = DialogResult.None; 
                    DisplayError(ex);
                }
            }
 

 
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.CreateAndAddInstance"]/*' />
            /// <devdoc> 
            ///      Performs a create instance and then adds the instance to
            ///      the list box.
            /// </devdoc>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
            private void CreateAndAddInstance(Type type) {
                try { 
                    object instance = CreateInstance(type); 
                    IList multipleInstance = editor.GetObjectsFromInstance(instance);
 
                    if (multipleInstance != null) {
                        AddItems(multipleInstance);
                    }
                } 
                catch (Exception e) {
                    DisplayError(e); 
                } 
            }
 



            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.DownButton_click"]/*' /> 
            /// <devdoc>
            ///      Moves the selected item down one. 
            /// </devdoc> 
            private void DownButton_click(object sender, EventArgs e) {
                try { 
                    SuspendEnabledUpdates();
                    dirty = true;
                    int index = listbox.SelectedIndex;
                    if (index == listbox.Items.Count - 1) 
                        return;
                    int ti = listbox.TopIndex; 
                    object itemMove = listbox.Items[index]; 
                    listbox.Items[index] = listbox.Items[index+1];
                    listbox.Items[index+1] = itemMove; 

                    if (ti < listbox.Items.Count - 1)
                        listbox.TopIndex = ti + 1;
 
                    listbox.ClearSelected();
                    listbox.SelectedIndex = index + 1; 
 
                    // enabling/disabling the buttons has moved the focus to the OK button, move it back to the sender
                    Control ctrlSender = (Control)sender; 

                    if (ctrlSender.Enabled) {
                        ctrlSender.Focus ();
                    } 
                }
                finally { 
 
                    ResumeEnabledUpdates(true);
                } 
            }

            private void CollectionEditor_HelpButtonClicked(object sender, CancelEventArgs e) {
                e.Cancel = true; 
                editor.ShowHelp();
            } 
 
            private void Form_HelpRequested(object sender, HelpEventArgs e) {
                editor.ShowHelp(); 
            }

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.GetDisplayText"]/*' />
            /// <devdoc> 
            ///     Retrieves the display text for the given list item (if any). The item determines its own display text
            ///     through its ToString() method, which delegates to the GetDisplayText() override on the parent CollectionEditor. 
            ///     This means in theory that the text can change at any time (ie. its not fixed when the item is added to the list). 
            ///     The item returns its display text through ToString() so that the same text will be reported to Accessibility clients.
            /// </devdoc> 
            private string GetDisplayText(ListItem item) {
                return (item == null) ? String.Empty : item.ToString();
            }
 
            private void HookEvents() {
                listbox.KeyDown += new KeyEventHandler(this.Listbox_keyDown); 
                listbox.DrawItem += new DrawItemEventHandler(this.Listbox_drawItem); 
                listbox.SelectedIndexChanged += new EventHandler(this.Listbox_selectedIndexChanged);
                listbox.HandleCreated += new EventHandler(this.Listbox_handleCreated); 
                upButton.Click += new EventHandler(this.UpButton_click);
                downButton.Click += new EventHandler(this.DownButton_click);
                propertyBrowser.PropertyValueChanged += new PropertyValueChangedEventHandler(this.PropertyGrid_propertyValueChanged);
                addButton.Click += new EventHandler(this.AddButton_click); 
                removeButton.Click += new EventHandler(this.RemoveButton_click);
                okButton.Click += new EventHandler(this.OKButton_click); 
                cancelButton.Click += new EventHandler(this.CancelButton_click); 
                this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.CollectionEditor_HelpButtonClicked);
                this.HelpRequested += new HelpEventHandler(this.Form_HelpRequested); 
            }

            private void InitializeComponent()
            { 
                System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CollectionEditor));
                this.membersLabel = new System.Windows.Forms.Label(); 
                this.listbox = new FilterListBox(); 
                this.upButton = new Button();
                this.downButton = new Button(); 
                this.propertiesLabel = new System.Windows.Forms.Label();
                this.propertyBrowser = new VsPropertyGrid(Context);
                this.addButton = new SplitButton();
                this.removeButton = new System.Windows.Forms.Button(); 
                this.okButton = new System.Windows.Forms.Button();
                this.cancelButton = new System.Windows.Forms.Button(); 
                this.okCancelTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
                this.overArchingTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
                this.addRemoveTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
                this.okCancelTableLayoutPanel.SuspendLayout();
                this.overArchingTableLayoutPanel.SuspendLayout();
                this.addRemoveTableLayoutPanel.SuspendLayout();
                this.SuspendLayout(); 
                //
                // membersLabel 
                // 
                resources.ApplyResources(this.membersLabel, "membersLabel");
                this.membersLabel.Name = "membersLabel"; 
                //
                // listbox
                //
                resources.ApplyResources(this.listbox, "listbox"); 
                this.listbox.SelectionMode = (CanSelectMultipleInstances() ? SelectionMode.MultiExtended : SelectionMode.One);
                this.listbox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed; 
                this.listbox.FormattingEnabled = true; 
                this.listbox.Name = "listbox";
                this.overArchingTableLayoutPanel.SetRowSpan(this.listbox, 2); 
                //
                // upButton
                //
                resources.ApplyResources(this.upButton, "upButton"); 
                this.upButton.Name = "upButton";
                // 
                // downButton 
                //
                resources.ApplyResources(this.downButton, "downButton"); 
                this.downButton.Name = "downButton";
                //
                // propertiesLabel
                // 
                resources.ApplyResources(this.propertiesLabel, "propertiesLabel");
                this.propertiesLabel.AutoEllipsis = true; 
                this.propertiesLabel.Name = "propertiesLabel"; 
                //
                // propertyBrowser 
                //
                resources.ApplyResources(this.propertyBrowser, "propertyBrowser");
                this.propertyBrowser.CommandsVisibleIfAvailable = false;
                this.propertyBrowser.Name = "propertyBrowser"; 
                this.overArchingTableLayoutPanel.SetRowSpan(this.propertyBrowser, 3);
                // 
                // addButton 
                //
                resources.ApplyResources(this.addButton, "addButton"); 
                this.addButton.Name = "addButton";
                //
                // removeButton
                // 
                resources.ApplyResources(this.removeButton, "removeButton");
                this.removeButton.Name = "removeButton"; 
                // 
                // okButton
                // 
                resources.ApplyResources(this.okButton, "okButton");
                this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.okButton.Name = "okButton";
                // 
                // cancelButton
                // 
                resources.ApplyResources(this.cancelButton, "cancelButton"); 
                this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                this.cancelButton.Name = "cancelButton"; 
                //
                // okCancelTableLayoutPanel
                //
                resources.ApplyResources(this.okCancelTableLayoutPanel, "okCancelTableLayoutPanel"); 
                this.overArchingTableLayoutPanel.SetColumnSpan(this.okCancelTableLayoutPanel, 3);
                this.okCancelTableLayoutPanel.Controls.Add(this.okButton, 0, 0); 
                this.okCancelTableLayoutPanel.Controls.Add(this.cancelButton, 1, 0); 
                this.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel";
                // 
                // overArchingTableLayoutPanel
                //
                resources.ApplyResources(this.overArchingTableLayoutPanel, "overArchingTableLayoutPanel");
                this.overArchingTableLayoutPanel.Controls.Add(this.downButton, 1, 2); 
                this.overArchingTableLayoutPanel.Controls.Add(this.addRemoveTableLayoutPanel, 0, 3);
                this.overArchingTableLayoutPanel.Controls.Add(this.propertiesLabel, 2, 0); 
                this.overArchingTableLayoutPanel.Controls.Add(this.membersLabel, 0, 0); 
                this.overArchingTableLayoutPanel.Controls.Add(this.listbox, 0, 1);
                this.overArchingTableLayoutPanel.Controls.Add(this.propertyBrowser, 2, 1); 
                this.overArchingTableLayoutPanel.Controls.Add(this.okCancelTableLayoutPanel, 0, 4);
                this.overArchingTableLayoutPanel.Controls.Add(this.upButton, 1, 1);
                this.overArchingTableLayoutPanel.Name = "overArchingTableLayoutPanel";
                // 
                // addRemoveTableLayoutPanel
                // 
                resources.ApplyResources(this.addRemoveTableLayoutPanel, "addRemoveTableLayoutPanel"); 
                this.addRemoveTableLayoutPanel.Controls.Add(this.addButton, 0, 0);
                this.addRemoveTableLayoutPanel.Controls.Add(this.removeButton, 2, 0); 
                this.addRemoveTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
                this.addRemoveTableLayoutPanel.Name = "addRemoveTableLayoutPanel";
                //
                // CollectionEditor 
                //
                this.AcceptButton = this.okButton; 
                resources.ApplyResources(this, "$this"); 
                this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
                this.CancelButton = this.cancelButton; 
                this.Controls.Add(this.overArchingTableLayoutPanel);
                this.HelpButton = true;
                this.MaximizeBox = false;
                this.MinimizeBox = false; 
                this.Name = "CollectionEditor";
                this.ShowIcon = false; 
                this.ShowInTaskbar = false; 
                this.okCancelTableLayoutPanel.ResumeLayout(false);
                this.okCancelTableLayoutPanel.PerformLayout(); 
                this.overArchingTableLayoutPanel.ResumeLayout(false);
                this.overArchingTableLayoutPanel.PerformLayout();
                this.addRemoveTableLayoutPanel.ResumeLayout(false);
                this.addRemoveTableLayoutPanel.PerformLayout(); 
                this.ResumeLayout(false);
            } 
 
            private void UpdateItemWidths(ListItem item) {
                // VSWhidbey#384112: Its neither safe nor accurate to perform these width 
                // calculations prior to normal listbox handle creation. So we nop in this case now.
                if (!listbox.IsHandleCreated) {
                    return;
                } 

                using (Graphics g = listbox.CreateGraphics()) { 
                    int old = listbox.HorizontalExtent; 

                    if (item != null) { 
                        int w = CalcItemWidth(g, item);
                        if (w > old) {
                            listbox.HorizontalExtent = w;
                        } 
                    }
                    else { 
                        int max = 0; 
                        foreach (ListItem i in listbox.Items) {
                            int w = CalcItemWidth(g, i); 
                            if (w > max) {
                                max = w;
                            }
                        } 
                        listbox.HorizontalExtent = max;
                    } 
                } 
            }
 


            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.Listbox_drawItem"]/*' />
            /// <devdoc> 
            ///     This draws a row of the listbox.
            /// </devdoc> 
            private void Listbox_drawItem(object sender, DrawItemEventArgs e) { 
                if (e.Index != -1) {
                    ListItem item = (ListItem)listbox.Items[e.Index]; 

                    Graphics g = e.Graphics;

                    int c = listbox.Items.Count; 
                    int maxC = (c > 1) ? c - 1: c;
                    // We add the +4 is a fudge factor... 
                    // 
                    SizeF sizeW = g.MeasureString(maxC.ToString(CultureInfo.CurrentCulture), listbox.Font);
 
                    int charactersInNumber = ((int)(Math.Log((double)maxC) / LOG10) + 1);// Luckily, this is never called if count = 0
                    int w = 4 + charactersInNumber * (Font.Height / 2);

                    w = Math.Max(w, (int)Math.Ceiling(sizeW.Width)); 
                    w += SystemInformation.BorderSize.Width * 4;
 
                    Rectangle button = new Rectangle(e.Bounds.X, e.Bounds.Y, w, e.Bounds.Height); 

                    ControlPaint.DrawButton(g, button, ButtonState.Normal); 
                    button.Inflate(-SystemInformation.BorderSize.Width*2, -SystemInformation.BorderSize.Height*2);

                    int offset = w;
 
                    Color backColor = SystemColors.Window;
                    Color textColor = SystemColors.WindowText; 
                    if ((e.State & DrawItemState.Selected) == DrawItemState.Selected) { 
                        backColor = SystemColors.Highlight;
                        textColor = SystemColors.HighlightText; 
                    }
                    Rectangle res = new Rectangle(e.Bounds.X + offset, e.Bounds.Y,
                                                  e.Bounds.Width - offset,
                                                  e.Bounds.Height); 
                    g.FillRectangle(new SolidBrush(backColor), res);
                    if ((e.State & DrawItemState.Focus) == DrawItemState.Focus) { 
                        ControlPaint.DrawFocusRectangle(g, res); 
                    }
                    offset+=2; 

                    if (item.Editor != null && item.Editor.GetPaintValueSupported()) {
                        Rectangle baseVar = new Rectangle(e.Bounds.X + offset, e.Bounds.Y + 1, PAINT_WIDTH, e.Bounds.Height - 3);
                        g.DrawRectangle(SystemPens.ControlText, baseVar.X, baseVar.Y, baseVar.Width - 1, baseVar.Height - 1); 
                        baseVar.Inflate(-1, -1);
                        item.Editor.PaintValue(item.Value, g, baseVar); 
                        offset += PAINT_INDENT + TEXT_INDENT; 
                    }
 
                    StringFormat format = new StringFormat();
                    try {
                        format.Alignment = StringAlignment.Center;
                        g.DrawString(e.Index.ToString(CultureInfo.CurrentCulture), Font, SystemBrushes.ControlText, 
                                     new Rectangle(e.Bounds.X, e.Bounds.Y, w, e.Bounds.Height), format);
                    } 
 
                    finally {
                        if (format != null) { 
                            format.Dispose();
                        }
                    }
 
                    Brush textBrush = new SolidBrush(textColor);
 
                    string itemText = GetDisplayText(item); 

                    try { 
                        g.DrawString(itemText, Font, textBrush,
                                     new Rectangle(e.Bounds.X + offset, e.Bounds.Y, e.Bounds.Width - offset, e.Bounds.Height));
                    }
 
                    finally {
                        if (textBrush != null) { 
                            textBrush.Dispose(); 
                        }
                    } 

                    // Check to see if we need to change the horizontal extent of the listbox
                    //
                    int width = offset + (int)g.MeasureString(itemText, Font).Width; 
                    if (width > e.Bounds.Width && listbox.HorizontalExtent < width) {
                        listbox.HorizontalExtent = width; 
                    } 
                }
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.Listbox_keyPress"]/*' />
            /// <devdoc>
            ///      Handles keypress events for the list box. 
            /// </devdoc>
            private void Listbox_keyDown(object sender, KeyEventArgs kevent) { 
                switch (kevent.KeyData) { 
                    case Keys.Delete:
                        PerformRemove(); 
                        break;
                    case Keys.Insert:
                        PerformAdd();
                        break; 
                }
            } 
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.Listbox_selectedIndexChanged"]/*' />
            /// <devdoc> 
            ///      Event that fires when the selected list box index changes.
            /// </devdoc>
            private void Listbox_selectedIndexChanged(object sender, EventArgs e) {
                UpdateEnabled(); 
            }
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.Listbox_handleCreated"]/*' /> 
            /// <devdoc>
            ///      Event that fires when the list box's window handle is created. 
            /// </devdoc>
            private void Listbox_handleCreated(object sender, EventArgs e) {
                // VSWhidbey#384112: Since we no longer perform width calculations prior to handle
                // creation now, we need to ensure we do it at least once after handle creation. 
                UpdateItemWidths(null);
            } 
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.OKButton_click"]/*' />
            /// <devdoc> 
            ///      Commits the changes to the editor.
            /// </devdoc>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
            private void OKButton_click(object sender, EventArgs e) { 
                try {
 
                    if (!dirty || !CollectionEditable) { 
                        dirty = false;
                        DialogResult = DialogResult.Cancel; 
                        return;
                    }

                    // Now apply the changes to the actual value. 
                    //
                    if (dirty) { 
                        object[] items = new object[listbox.Items.Count]; 
                        for (int i = 0; i < items.Length; i++) {
                            items[i] = ((ListItem)listbox.Items[i]).Value; 
                        }

                        Items = items;
                    } 

 
                    // Now destroy any existing items we had. 
                    //
                    if (removedItems != null && dirty) { 
                        object[] deadItems = removedItems.ToArray();

                        for (int i=0; i<deadItems.Length; i++) {
                            DestroyInstance(deadItems[i]); 
                        }
                        removedItems.Clear(); 
                    } 
                    if (createdItems != null) {
                        createdItems.Clear(); 
                    }

                    if (originalItems != null) {
                        originalItems.Clear(); 
                    }
 
                    listbox.Items.Clear(); 
                    dirty = false;
                } 
                catch (Exception ex) {
                    DialogResult = DialogResult.None;
                    DisplayError(ex);
                } 
            }
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.OnComponentChanged"]/*' /> 
            /// <devdoc>
            /// reflect any change events to the instance object 
            /// </devdoc>
            private void OnComponentChanged(object sender, ComponentChangedEventArgs e) {

                // see if this is any of the items in our list...this can happen if 
                // we launched a child editor
                if (!dirty) { 
                    foreach (object item in originalItems) { 
                        if (item == e.Component) {
                            dirty = true; 
                            break;
                        }
                    }
                } 

            } 
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.OnEditValueChanged"]/*' />
            /// <devdoc> 
            ///      This is called when the value property in the CollectionForm has changed.
            ///      In it you should update your user interface to reflect the current value.
            /// </devdoc>
            protected override void OnEditValueChanged() { 

                // Remember these contents for cancellation 
                if (originalItems == null) { 
                    originalItems = new ArrayList();
                } 
                originalItems.Clear();

                // Now update the list box.
                // 
                listbox.Items.Clear();
                propertyBrowser.Site = new PropertyGridSite(Context, propertyBrowser); 
                if (EditValue != null) { 
                    SuspendEnabledUpdates();
                    try { 
                        object[] items = Items;
                        for (int i = 0; i < items.Length; i++) {
                            listbox.Items.Add(new ListItem(editor, items[i]));
                            originalItems.Add(items[i]); 
                        }
                        if (listbox.Items.Count > 0) { 
                            listbox.SelectedIndex = 0; 
                        }
                    } 
                    finally {
                        ResumeEnabledUpdates(true);
                    }
                } 
                else {
                    UpdateEnabled(); 
                } 

                AdjustListBoxItemHeight(); 
                UpdateItemWidths(null);

            }
 
            protected override void OnFontChanged(EventArgs e) {
                base.OnFontChanged(e); 
                AdjustListBoxItemHeight(); 
            }
 
            /// <devdoc>
            ///     Performs the actual add of new items.  This is invoked by the
            ///     add button as well as the insert key on the list box.
            /// </devdoc> 
            private void PerformAdd() {
                CreateAndAddInstance(NewItemTypes[0]); 
            } 

            /// <devdoc> 
            ///     Performs a remove by deleting all items currently selected in
            ///     the list box.  This is called by the delete button as well as
            ///     the delete key on the list box.
            /// </devdoc> 
            private void PerformRemove() {
                int index = listbox.SelectedIndex; 
 
                if (index != -1) {
                    SuspendEnabledUpdates(); 
                    try {

                        // single object selected or multiple ?
                        if(listbox.SelectedItems.Count > 1) { 
                            ArrayList toBeDeleted = new ArrayList(listbox.SelectedItems);
                            foreach (ListItem item in toBeDeleted) 
                            { 
                                RemoveInternal(item);
                            } 
                        } else {
                            RemoveInternal((ListItem)listbox.SelectedItem);
                        }
                        // set the new selected index 
                        if (index < listbox.Items.Count) {
                            listbox.SelectedIndex = index; 
                        } 
                        else if (listbox.Items.Count > 0) {
                            listbox.SelectedIndex = listbox.Items.Count - 1; 
                        }
                    }
                    finally {
                        ResumeEnabledUpdates(true); 
                    }
                } 
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.PropertyGrid_propertyValueChanged"]/*' /> 
            /// <devdoc>
            ///      When something in the properties window changes, we update pertinent text here.
            /// </devdoc>
            private void PropertyGrid_propertyValueChanged(object sender, PropertyValueChangedEventArgs e) { 

                dirty = true; 
 
                // Refresh selected listbox item so that it picks up any name change
                SuspendEnabledUpdates(); 
                try {
                    listbox.RefreshItem(listbox.SelectedIndex);
                }
                finally { 
                    ResumeEnabledUpdates(false);
                } 
 
                // if a property changes, invalidate the grid in case
                // it affects the item's name. 
                UpdateItemWidths(null);
                listbox.Invalidate();

                // also update the string above the grid. 
                propertiesLabel.Text = SR.GetString(SR.CollectionEditorProperties, GetDisplayText((ListItem)listbox.SelectedItem));
            } 
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.RemoveInternal"]/*' />
            /// <devdoc> 
            ///      Used to actually remove the items, one by one.
            /// </devdoc>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
            private void RemoveInternal(ListItem item) { 
                if (item != null) {
 
                    editor.OnItemRemoving(item.Value); 

                    dirty = true; 
                    //ListItem item = (ListItem)listbox.Items[index];

                    if (createdItems != null && createdItems.Contains(item.Value)) {
                        DestroyInstance(item.Value); 
                        createdItems.Remove(item.Value);
                        listbox.Items.Remove(item); 
                    } 
                    else {
                        try { 
                            if (CanRemoveInstance(item.Value)) {
                                if (removedItems == null) {
                                    removedItems = new ArrayList();
                                } 
                                removedItems.Add(item.Value);
                                listbox.Items.Remove(item); 
                            } else { 
                                throw new Exception(SR.GetString(SR.CollectionEditorCantRemoveItem, GetDisplayText(item)));
                            } 
                        }
                        catch (Exception ex) {
                            DisplayError(ex);
                        } 
                    }
                    // othewise go through the entire list 
                    UpdateItemWidths(null); 

 
                }
            }

 

 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.RemoveButton_click"]/*' /> 
            /// <devdoc>
            ///      Removes the selected item. 
            /// </devdoc>
            private void RemoveButton_click(object sender, EventArgs e) {
                PerformRemove();
 
                // enabling/disabling the buttons has moved the focus to the OK button, move it back to the sender
                Control ctrlSender = (Control)sender; 
                if(ctrlSender.Enabled) { 
                    ctrlSender.Focus ();
                } 
            }

            /// <devdoc>
            /// used to prevent flicker when playing with the list box selection 
            /// call resume when done.  Calls to UpdateEnabled will return silently until Resume is called
            /// </devdoc> 
            private void ResumeEnabledUpdates(bool updateNow){ 
                 suspendEnabledCount--;
 
                 Debug.Assert(suspendEnabledCount >= 0, "Mismatch suspend/resume enabled");

                 if (updateNow) {
                     UpdateEnabled(); 
                 }
                 else { 
                     this.BeginInvoke(new MethodInvoker(this.UpdateEnabled)); 
                 }
            } 
            /// <devdoc>
            /// used to prevent flicker when playing with the list box selection
            /// call resume when done.  Calls to UpdateEnabled will return silently until Resume is called
            /// </devdoc> 
            private void SuspendEnabledUpdates(){
               suspendEnabledCount++; 
            } 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.ShowEditorDialog"]/*' />
            /// <devdoc> 
            ///    <para>
            ///       Called to show the dialog via the IWindowsFormsEditorService
            ///    </para>
            /// </devdoc> 
            protected internal override DialogResult ShowEditorDialog(IWindowsFormsEditorService edSvc) {
              IComponentChangeService cs = null; 
              DialogResult result = DialogResult.OK; 
              try {
 
                  cs = (IComponentChangeService)editor.Context.GetService(typeof(IComponentChangeService));

                  if (cs != null) {
                      cs.ComponentChanged += new ComponentChangedEventHandler(this.OnComponentChanged); 
                  }
 
                  // This is cached across requests, so reset the initial focus. 
                  ActiveControl = listbox;
                  //SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(this.OnSysColorChange); 
                  result = base.ShowEditorDialog(edSvc);
                  //SystemEvents.UserPreferenceChanged -= new UserPreferenceChangedEventHandler(this.OnSysColorChange);
              }
              finally{ 

                  if (cs != null) { 
                      cs.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged); 
                  }
              } 
              return result;
            }

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.UpButton_click"]/*' /> 
            /// <devdoc>
            ///      Moves an item up one in the list box. 
            /// </devdoc> 
            private void UpButton_click(object sender, EventArgs e) {
                int index = listbox.SelectedIndex; 
                if (index == 0)
                    return;

                dirty = true; 
                try {
                    SuspendEnabledUpdates(); 
                    int ti = listbox.TopIndex; 
                    object itemMove = listbox.Items[index];
                    listbox.Items[index] = listbox.Items[index-1]; 
                    listbox.Items[index-1] = itemMove;

                    if (ti > 0)
                        listbox.TopIndex = ti - 1; 

                    listbox.ClearSelected(); 
                    listbox.SelectedIndex = index - 1; 

                    // enabling/disabling the buttons has moved the focus to the OK button, move it back to the sender 
                    Control ctrlSender = (Control)sender;

                    if (ctrlSender.Enabled) {
                        ctrlSender.Focus (); 
                    }
                } 
                finally { 

                    ResumeEnabledUpdates(true); 
                }

            }
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.UpdateEnabled"]/*' />
            /// <devdoc> 
            ///      Updates the set of enabled buttons. 
            /// </devdoc>
            private void UpdateEnabled() { 
                if (suspendEnabledCount > 0) {
                    // We're in the midst of a suspend/resume block  Resume should call us back.
                    return;
                } 

                bool editEnabled = (listbox.SelectedItem != null) && this.CollectionEditable; 
                removeButton.Enabled = editEnabled && AllowRemoveInstance(((ListItem) listbox.SelectedItem).Value); 
                upButton.Enabled = editEnabled && listbox.Items.Count > 1;
                downButton.Enabled = editEnabled && listbox.Items.Count > 1; 
                propertyBrowser.Enabled = editEnabled;
                addButton.Enabled = this.CollectionEditable;

                if (listbox.SelectedItem != null) { 
                    object[] items;
 
                    // If we are to create new instances from the items, then we must wrap them in an outer object. 
                    // otherwise, the user will be presented with a batch of read only properties, which isn't terribly
                    // useful. 
                    //
                    if (IsImmutable) {
                        items = new object[] {new SelectionWrapper(CollectionType, CollectionItemType, listbox, listbox.SelectedItems)};
                    } 
                    else {
                        items = new object[listbox.SelectedItems.Count]; 
                        for (int i = 0; i < items.Length; i++) { 
                            items[i] = ((ListItem)listbox.SelectedItems[i]).Value;
                        } 
                    }

                    int selectedItemCount = listbox.SelectedItems.Count;
                    if ((selectedItemCount == 1) || (selectedItemCount == -1)) { 
                        // handle both single select listboxes and a single item selected in a multi-select listbox
                        propertiesLabel.Text = SR.GetString(SR.CollectionEditorProperties, GetDisplayText((ListItem)listbox.SelectedItem)); 
                    } 
                    else {
                        propertiesLabel.Text = SR.GetString(SR.CollectionEditorPropertiesMultiSelect); 
                    }

                    if (editor.IsAnyObjectInheritedReadOnly(items)) {
                        propertyBrowser.SelectedObjects = null; 
                        propertyBrowser.Enabled = false;
                        removeButton.Enabled = false; 
                        upButton.Enabled = false; 
                        downButton.Enabled = false;
                        propertiesLabel.Text = SR.GetString(SR.CollectionEditorInheritedReadOnlySelection); 
                    }
                    else {
                        propertyBrowser.Enabled = true;
                        propertyBrowser.SelectedObjects = items; 
                    }
                } 
                else { 
                    propertiesLabel.Text = SR.GetString(SR.CollectionEditorPropertiesNone);
                    propertyBrowser.SelectedObject = null; 
                }
            }

 

 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper"]/*' /> 
            /// <devdoc>
            ///     This class implements a custom type descriptor that is used to provide properties for the set of 
            ///     selected items in the collection editor.  It provides a single property that is equivalent
            ///     to the editor's collection item type.
            /// </devdoc>
            private class SelectionWrapper : PropertyDescriptor, ICustomTypeDescriptor { 
                private Type collectionType;
                private Type collectionItemType; 
                private Control control; 
                private ICollection collection;
                private PropertyDescriptorCollection properties; 
                private object value;

                public SelectionWrapper(Type collectionType, Type collectionItemType, Control control, ICollection collection) :
                base("Value", 
                     new Attribute[] {new CategoryAttribute(collectionItemType.Name)}
                    ) { 
                    this.collectionType = collectionType; 
                    this.collectionItemType = collectionItemType;
                    this.control = control; 
                    this.collection = collection;
                    this.properties = new PropertyDescriptorCollection(new PropertyDescriptor[] {this});

                    Debug.Assert(collection.Count > 0, "We should only be wrapped if there is a selection"); 
                    value = this;
 
                    // In a multiselect case, see if the values are different.  If so, 
                    // NULL our value to represent indeterminate.
                    // 
                    foreach (ListItem li in collection) {
                        if (value == this) {
                            value = li.Value;
                        } 
                        else {
                            object nextValue = li.Value; 
                            if (value != null) { 
                                if (nextValue == null) {
                                    value = null; 
                                    break;
                                }
                                else {
                                    if (!value.Equals(nextValue)) { 
                                        value = null;
                                        break; 
                                    } 
                                }
                            } 
                            else {
                                if (nextValue != null) {
                                    value = null;
                                    break; 
                                }
                            } 
                        } 
                    }
                } 

                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ComponentType"]/*' />
                /// <devdoc>
                ///    <para> 
                ///       When overridden in a derived class, gets the type of the
                ///       component this property 
                ///       is bound to. 
                ///    </para>
                /// </devdoc> 
                public override Type ComponentType {
                    get {
                        return collectionType;
                    } 
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.IsReadOnly"]/*' /> 
                /// <devdoc>
                ///    <para> 
                ///       When overridden in
                ///       a derived class, gets a value
                ///       indicating whether this property is read-only.
                ///    </para> 
                /// </devdoc>
                public override bool IsReadOnly { 
                    get { 
                        return false;
                    } 
                }

                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.PropertyType"]/*' />
                /// <devdoc> 
                ///    <para>
                ///       When overridden in a derived class, 
                ///       gets the type of the property. 
                ///    </para>
                /// </devdoc> 
                public override Type PropertyType {
                    get {
                        return collectionItemType;
                    } 
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.CanResetValue"]/*' /> 
                /// <devdoc>
                ///    <para> 
                ///       When overridden in a derived class, indicates whether
                ///       resetting the <paramref name="component "/>will change the value of the
                ///    <paramref name="component"/>.
                /// </para> 
                /// </devdoc>
                public override bool CanResetValue(object component) { 
                    return false; 
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.GetValue"]/*' />
                /// <devdoc>
                ///    <para>
                ///       When overridden in a derived class, gets the current 
                ///       value
                ///       of the 
                ///       property on a component. 
                ///    </para>
                /// </devdoc> 
                public override object GetValue(object component) {
                    return value;
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ResetValue"]/*' />
                /// <devdoc> 
                ///    <para> 
                ///       When overridden in a derived class, resets the
                ///       value 
                ///       for this property
                ///       of the component.
                ///    </para>
                /// </devdoc> 
                public override void ResetValue(object component) {
                } 
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.SetValue"]/*' />
                /// <devdoc> 
                ///    <para>
                ///       When overridden in a derived class, sets the value of
                ///       the component to a different value.
                ///    </para> 
                /// </devdoc>
                public override void SetValue(object component, object value) { 
                    this.value = value; 

                    foreach(ListItem li in collection) { 
                        li.Value = value;
                    }
                    control.Invalidate();
                    OnValueChanged(component, EventArgs.Empty); 
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ShouldSerializeValue"]/*' /> 
                /// <devdoc>
                ///    <para> 
                ///       When overridden in a derived class, indicates whether the
                ///       value of
                ///       this property needs to be persisted.
                ///    </para> 
                /// </devdoc>
                public override bool ShouldSerializeValue(object component) { 
                    return false; 
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetAttributes"]/*' />
                /// <devdoc>
                ///     Retrieves an array of member attributes for the given object.
                /// </devdoc> 
                AttributeCollection ICustomTypeDescriptor.GetAttributes() {
                    return TypeDescriptor.GetAttributes(collectionItemType); 
                } 

                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetClassName"]/*' /> 
                /// <devdoc>
                ///     Retrieves the class name for this object.  If null is returned,
                ///     the type name is used.
                /// </devdoc> 
                string ICustomTypeDescriptor.GetClassName() {
                    return collectionItemType.Name; 
                } 

                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetComponentName"]/*' /> 
                /// <devdoc>
                ///     Retrieves the name for this object.  If null is returned,
                ///     the default is used.
                /// </devdoc> 
                string ICustomTypeDescriptor.GetComponentName() {
                    return null; 
                } 

                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetConverter"]/*' /> 
                /// <devdoc>
                ///      Retrieves the type converter for this object.
                /// </devdoc>
                TypeConverter ICustomTypeDescriptor.GetConverter() { 
                    return null;
                } 
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetDefaultEvent"]/*' />
                /// <devdoc> 
                ///     Retrieves the default event.
                /// </devdoc>
                EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() {
                    return null; 
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetDefaultProperty"]/*' /> 
                /// <devdoc>
                ///     Retrieves the default property. 
                /// </devdoc>
                PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() {
                    return this;
                } 

                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetEditor"]/*' /> 
                /// <devdoc> 
                ///      Retrieves the an editor for this object.
                /// </devdoc> 
                object ICustomTypeDescriptor.GetEditor(Type editorBaseType) {
                    return null;
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetEvents"]/*' />
                /// <devdoc> 
                ///     Retrieves an array of events that the given component instance 
                ///     provides.  This may differ from the set of events the class
                ///     provides.  If the component is sited, the site may add or remove 
                ///     additional events.
                /// </devdoc>
                EventDescriptorCollection ICustomTypeDescriptor.GetEvents() {
                    return EventDescriptorCollection.Empty; 
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetEvents1"]/*' /> 
                /// <devdoc>
                ///     Retrieves an array of events that the given component instance 
                ///     provides.  This may differ from the set of events the class
                ///     provides.  If the component is sited, the site may add or remove
                ///     additional events.  The returned array of events will be
                ///     filtered by the given set of attributes. 
                /// </devdoc>
                EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) { 
                    return EventDescriptorCollection.Empty; 
                }
 
                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetProperties"]/*' />
                /// <devdoc>
                ///     Retrieves an array of properties that the given component instance
                ///     provides.  This may differ from the set of properties the class 
                ///     provides.  If the component is sited, the site may add or remove
                ///     additional properties. 
                /// </devdoc> 
                PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() {
                    return properties; 
                }

                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetProperties1"]/*' />
                /// <devdoc> 
                ///     Retrieves an array of properties that the given component instance
                ///     provides.  This may differ from the set of properties the class 
                ///     provides.  If the component is sited, the site may add or remove 
                ///     additional properties.  The returned array of properties will be
                ///     filtered by the given set of attributes. 
                /// </devdoc>
                PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) {
                    return properties;
                } 

                /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.SelectionWrapper.ICustomTypeDescriptor.GetPropertyOwner"]/*' /> 
                /// <devdoc> 
                ///     Retrieves the object that directly depends on this value being edited.  This is
                ///     generally the object that is required for the PropertyDescriptor's GetValue and SetValue 
                ///     methods.  If 'null' is passed for the PropertyDescriptor, the ICustomComponent
                ///     descripotor implemementation should return the default object, that is the main
                ///     object that exposes the properties and attributes,
                /// </devdoc> 
                object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) {
                    return this; 
                } 
            }
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.ListItem"]/*' />
            /// <devdoc>
            ///      ListItem class.  This is a single entry in our list box.  It contains the value we're editing
            ///      as well as accessors for the type converter and UI editor. 
            /// </devdoc>
            private class ListItem { 
                private object value; 
                private object uiTypeEditor;
                private CollectionEditor parentCollectionEditor; 

                public ListItem(CollectionEditor parentCollectionEditor, object value) {
                    this.value = value;
                    this.parentCollectionEditor = parentCollectionEditor; 
                }
 
                public override string ToString() { 
                    return parentCollectionEditor.GetDisplayText(this.value);
                } 

                public UITypeEditor Editor {
                    get {
                        if (uiTypeEditor == null) { 
                            uiTypeEditor = TypeDescriptor.GetEditor(value, typeof(UITypeEditor));
                            if (uiTypeEditor == null) { 
                                uiTypeEditor = this; 
                            }
                        } 

                        if (uiTypeEditor != this) {
                            return (UITypeEditor) uiTypeEditor;
                        } 

                        return null; 
                    } 
                }
 
                public object Value {
                    get {
                        return value;
                    } 
                    set {
                        uiTypeEditor = null; 
                        this.value = value; 
                    }
                } 
            }

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.TypeMenuItem"]/*' />
            /// <devdoc> 
            ///      Menu items we attach to the drop down menu if there are multiple
            ///      types the collection editor can create. 
            /// </devdoc> 
            private class TypeMenuItem : ToolStripMenuItem {
                Type itemType; 

                public TypeMenuItem(Type itemType, EventHandler handler) :
                base(itemType.Name, null, handler) {
                    this.itemType = itemType; 
                }
 
                public Type ItemType { 
                    get {
                        return itemType; 
                    }
                }
            }
        } 

 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionEditorCollectionForm.ListItemListBox"]/*' /> 
        /// <devdoc>
        ///      List box filled with ListItem objects representing the collection. 
        /// </devdoc>
        internal class FilterListBox : ListBox {

 
            private PropertyGrid grid;
            private Message      lastKeyDown; 
 
            private PropertyGrid PropertyGrid {
                get { 
                    if (grid == null) {
                        foreach (Control c in Parent.Controls) {
                            if (c is PropertyGrid) {
                                grid = (PropertyGrid)c; 
                                break;
                            } 
                        } 
                    }
                    return grid; 
                }

            }
 
            // Expose the protected RefreshItem() method so that CollectionEditor can use it
            public new void RefreshItem(int index) { 
                base.RefreshItem(index); 
            }
 
            protected override void WndProc(ref Message m) {
                switch (m.Msg) {
                    case NativeMethods.WM_KEYDOWN:
                        this.lastKeyDown = m; 

                        // the first thing the ime does on a key it cares about is send a VK_PROCESSKEY, 
                        // so we use that to sling focus to the grid. 
                        //
                        if ((int)m.WParam == NativeMethods.VK_PROCESSKEY) { 
                            if (PropertyGrid != null) {
                                PropertyGrid.Focus();
                                UnsafeNativeMethods.SetFocus(new HandleRef(PropertyGrid, PropertyGrid.Handle));
                                Application.DoEvents(); 
                            }
                            else { 
                                break; 
                            }
 
                            if(PropertyGrid.Focused || PropertyGrid.ContainsFocus) {
                                // recreate the keystroke to the newly activated window
                                NativeMethods.SendMessage(UnsafeNativeMethods.GetFocus(), NativeMethods.WM_KEYDOWN, lastKeyDown.WParam, lastKeyDown.LParam);
                            } 
                        }
                        break; 
 
                    case NativeMethods.WM_CHAR:
 
                        if ((Control.ModifierKeys & (Keys.Control | Keys.Alt)) != 0) {
                            break;
                        }
 
                        if (PropertyGrid != null) {
                            PropertyGrid.Focus(); 
                            UnsafeNativeMethods.SetFocus(new HandleRef(PropertyGrid, PropertyGrid.Handle)); 
                            Application.DoEvents();
                        } 
                        else {
                            break;
                        }
 
                        // Make sure we changed focus properly
                        // recreate the keystroke to the newly activated window 
                        // 
                        if (PropertyGrid.Focused || PropertyGrid.ContainsFocus) {
                            IntPtr hWnd = UnsafeNativeMethods.GetFocus(); 
                            NativeMethods.SendMessage(hWnd, NativeMethods.WM_KEYDOWN, lastKeyDown.WParam, lastKeyDown.LParam);
                            NativeMethods.SendMessage(hWnd, NativeMethods.WM_CHAR, m.WParam, m.LParam);
                            return;
                        } 
                        break;
 
                } 
                base.WndProc(ref m);
            } 

        }

 
        /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       The <see cref='System.ComponentModel.Design.CollectionEditor.CollectionForm'/>
        ///       provides a modal dialog for editing the 
        ///       contents of a collection.
        ///    </para>
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors")] //breaking change 
        protected abstract class CollectionForm : Form {
 
            // Manipulation of the collection. 
            //
            private CollectionEditor       editor; 
            private object                 value;
            private short                  editableState = EditableDynamic;

            private const short            EditableDynamic = 0; 
            private const short            EditableYes     = 1;
            private const short            EditableNo      = 2; 
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.CollectionForm"]/*' />
            /// <devdoc> 
            ///    <para>
            ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.CollectionEditor.CollectionForm'/> class.
            ///    </para>
            /// </devdoc> 
            public CollectionForm(CollectionEditor editor) {
                this.editor = editor; 
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.CollectionItemType"]/*' /> 
            /// <devdoc>
            ///    <para>
            ///       Gets or sets the data type of each item in the collection.
            ///    </para> 
            /// </devdoc>
            protected Type CollectionItemType { 
                get { 
                    return editor.CollectionItemType;
                } 
            }

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.CollectionType"]/*' />
            /// <devdoc> 
            ///    <para>
            ///       Gets or sets the type of the collection. 
            ///    </para> 
            /// </devdoc>
            protected Type CollectionType { 
                get {
                    return editor.CollectionType;
                }
            } 

            /// <internalonly/> 
            internal virtual bool CollectionEditable { 
                get {
                    if (editableState != EditableDynamic) { 
                        return editableState == EditableYes;
                    }

                    bool editable = typeof(IList).IsAssignableFrom(editor.CollectionType); 

                    if (editable) { 
                        IList list = EditValue as IList; 
                        if (list != null) {
                            return !list.IsReadOnly; 
                        }
                    }
                    return editable;
                } 
                set {
                    if (value) { 
                        editableState = EditableYes; 
                    }
                    else { 
                        editableState = EditableNo;
                    }
                }
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.Context"]/*' /> 
            /// <devdoc> 
            ///    <para>
            ///       Gets or sets a type descriptor that indicates the current context. 
            ///    </para>
            /// </devdoc>
            protected ITypeDescriptorContext Context {
                get { 
                    return editor.Context;
                } 
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.EditValue"]/*' /> 
            /// <devdoc>
            ///    <para>Gets or sets the value of the item being edited.</para>
            /// </devdoc>
            public object EditValue { 
                get {
                    return value; 
                } 
                set {
                    this.value = value; 
                    OnEditValueChanged();
                }
            }
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.Items"]/*' />
            /// <devdoc> 
            ///    <para> 
            ///       Gets or sets the
            ///       array of items this form is to display. 
            ///    </para>
            /// </devdoc>
            protected object[] Items {
                get { 
                    return editor.GetItems(EditValue);
                } 
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")] 
                set {
                    // Request our desire to make a change. 
                    //
                    bool canChange = false;
                    try {
                        canChange = Context.OnComponentChanging(); 
                    } catch (Exception ex) {
                        if(!ClientUtils.IsCriticalException(ex)) { 
                            DisplayError(ex); 
                        } else {
                            throw; 
                        }
                    }
                    if (canChange) {
                        object newValue = editor.SetItems(EditValue, value); 
                        if (newValue != EditValue) {
                            EditValue = newValue; 
                        } 
                        Context.OnComponentChanged();
                    } 

                }
            }
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.NewItemTypes"]/*' />
            /// <devdoc> 
            ///    <para> 
            ///       Gets or sets the available item types that can be created for this
            ///       collection. 
            ///    </para>
            /// </devdoc>
            protected Type[] NewItemTypes {
                get { 
                    return editor.NewItemTypes;
                } 
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.CanRemoveInstance"]/*' /> 
            /// <devdoc>
            ///    <para>Gets or sets a value indicating whether original members of the collection
            ///       can be removed.</para>
            /// </devdoc> 
            protected bool CanRemoveInstance(object value) {
                return editor.CanRemoveInstance(value); 
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.CanSelectMultipleInstances"]/*' /> 
            /// <devdoc>
            ///    <para>Gets or sets a value indicating whether multiple collection members can be
            ///       selected.</para>
            /// </devdoc> 
            protected virtual bool CanSelectMultipleInstances() {
                return editor.CanSelectMultipleInstances(); 
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.CreateInstance"]/*' /> 
            /// <devdoc>
            ///    <para>
            ///       Creates a new instance of the specified collection item type.
            ///    </para> 
            /// </devdoc>
            protected object CreateInstance(Type itemType) { 
                return editor.CreateInstance(itemType); 
            }
 
            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.DestroyInstance"]/*' />
            /// <devdoc>
            ///    <para>
            ///       Destroys the specified instance of the object. 
            ///    </para>
            /// </devdoc> 
            protected void DestroyInstance(object instance) { 
                editor.DestroyInstance(instance);
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.DisplayError"]/*' />
            /// <devdoc>
            ///    Displays the given exception to the user. 
            /// </devdoc>
            protected virtual void DisplayError(Exception e) { 
                IUIService uis = (IUIService)GetService(typeof(IUIService)); 
                if (uis != null) {
                    uis.ShowError(e); 
                }
                else {
                    string message = e.Message;
                    if (message == null || message.Length == 0) { 
                        message = e.ToString();
                    } 
                    RTLAwareMessageBox.Show(null, message, null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, 0); 
                }
            } 

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.GetService"]/*' />
            /// <devdoc>
            ///    <para> 
            ///       Gets the requested service, if it is available.
            ///    </para> 
            /// </devdoc> 
            protected override object GetService(Type serviceType) {
                return editor.GetService(serviceType); 
            }

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.ShowEditorDialog"]/*' />
            /// <devdoc> 
            ///    <para>
            ///       Called to show the dialog via the IWindowsFormsEditorService 
            ///    </para> 
            /// </devdoc>
            protected internal virtual DialogResult ShowEditorDialog(IWindowsFormsEditorService edSvc) { 
                return edSvc.ShowDialog(this);
            }

            /// <include file='doc\CollectionEditor.uex' path='docs/doc[@for="CollectionEditor.CollectionForm.OnEditValueChanged"]/*' /> 
            /// <devdoc>
            ///    <para> 
            ///       This is called when the value property in 
            ///       the <see cref='System.ComponentModel.Design.CollectionEditor.CollectionForm'/>
            ///       has changed. 
            ///    </para>
            /// </devdoc>
            protected abstract void OnEditValueChanged();
        } 

 
     internal class PropertyGridSite : ISite { 

            private IServiceProvider sp; 
            private IComponent comp;
            private bool       inGetService = false;

            public PropertyGridSite(IServiceProvider sp, IComponent comp) { 
                this.sp = sp;
                this.comp = comp; 
            } 

             /** The component sited by this component site. */ 
            /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Component"]/*' />
            /// <devdoc>
            ///    <para>When implemented by a class, gets the component associated with the <see cref='System.ComponentModel.ISite'/>.</para>
            /// </devdoc> 
            public IComponent Component {get {return comp;}}
 
            /** The container in which the component is sited. */ 
            /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Container"]/*' />
            /// <devdoc> 
            /// <para>When implemented by a class, gets the container associated with the <see cref='System.ComponentModel.ISite'/>.</para>
            /// </devdoc>
            public IContainer Container {get {return null;}}
 
            /** Indicates whether the component is in design mode. */
            /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.DesignMode"]/*' /> 
            /// <devdoc> 
            ///    <para>When implemented by a class, determines whether the component is in design mode.</para>
            /// </devdoc> 
            public  bool DesignMode {get {return false;}}

            /**
             * The name of the component. 
             */
                /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Name"]/*' /> 
                /// <devdoc> 
                ///    <para>When implemented by a class, gets or sets the name of
                ///       the component associated with the <see cref='System.ComponentModel.ISite'/>.</para> 
                /// </devdoc>
                public String Name {
                        get {return null;}
                        set {} 
                }
 
            public object GetService(Type t) { 
                if (!inGetService && sp != null) {
                    try { 
                        inGetService = true;
                        return sp.GetService(t);
                    }
                    finally { 
                        inGetService = false;
                    } 
                } 
                return null;
            } 

        }
    }
} 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
