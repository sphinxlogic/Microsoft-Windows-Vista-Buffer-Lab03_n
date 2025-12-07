//------------------------------------------------------------------------------ 
// <copyright file="EditorServiceContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Windows.Forms.Design.Behavior; 
    using System.Drawing;
    using System.Drawing.Design; 
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis;
 
    /// <include file='doc\EditorServiceContext.uex' path='docs/doc[@for="EditorServiceContext"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides an implementation of IWindowsFormsEditorService and ITypeDescriptorContext. 
    ///		  Also provides a static method to invoke a UITypeEditor given a designer, an object
    ///		  and a property name. 
    ///	   </para> 
    /// </devdoc>
    internal class EditorServiceContext : IWindowsFormsEditorService, ITypeDescriptorContext { 

        private ComponentDesigner _designer;
        private IComponentChangeService _componentChangeSvc;
        private PropertyDescriptor _targetProperty; 

 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        internal EditorServiceContext(ComponentDesigner designer)    {
            this._designer = designer; 
        }

        internal EditorServiceContext(ComponentDesigner designer, PropertyDescriptor prop) {
            this._designer = designer; 
            this._targetProperty = prop;
 
            if (prop == null) { 
                prop = TypeDescriptor.GetDefaultProperty(designer.Component);
                if (prop != null && typeof(ICollection).IsAssignableFrom(prop.PropertyType)) { 
                    _targetProperty = prop;
                }
            }
 
            Debug.Assert(_targetProperty != null, "Need PropertyDescriptor for ICollection property to associate collectoin edtior with.");
        } 
 
        internal EditorServiceContext(ComponentDesigner designer, PropertyDescriptor prop, string newVerbText) : this(designer, prop) {
            Debug.Assert(!string.IsNullOrEmpty(newVerbText), "newVerbText cannot be null or empty"); 
            _designer.Verbs.Add(new DesignerVerb(newVerbText, new EventHandler(this.OnEditItems)));
        }

        public static object EditValue(ComponentDesigner designer, object objectToChange, string propName) { 
            // Get PropertyDescriptor
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(objectToChange)[propName]; 
 
            // Create a Context
            EditorServiceContext context = new EditorServiceContext(designer, descriptor); 

            // Get Editor
            UITypeEditor editor = descriptor.GetEditor(typeof(UITypeEditor)) as UITypeEditor;
 
            // Get value to edit
            object value = descriptor.GetValue(objectToChange); 
 
            // Edit value
            object newValue = editor.EditValue(context, context, value); 

            if (newValue != value) {
                try
                { 
                    descriptor.SetValue(objectToChange, newValue);
                } 
                catch (CheckoutException) { 

                } 
            }

            return newValue;
        } 

        /// <devdoc> 
        /// Our caching property for the IComponentChangeService 
        /// </devdoc>
        private IComponentChangeService ChangeService { 
            get {
                if (_componentChangeSvc == null) {
                    _componentChangeSvc = (IComponentChangeService)((IServiceProvider)this).GetService(typeof(IComponentChangeService));
                } 
                return _componentChangeSvc;
            } 
        } 

        /// <devdoc> 
        /// Self-explanitory interface impl.
        /// </devdoc>
        IContainer ITypeDescriptorContext.Container {
            get { 
                if (_designer.Component.Site != null) {
                    return _designer.Component.Site.Container; 
                } 
                return null;
            } 
        }

        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc>
        void ITypeDescriptorContext.OnComponentChanged() { 
            ChangeService.OnComponentChanged(_designer.Component, _targetProperty, null, null); 
        }
 

        /// <devdoc>
        /// Self-explanitory interface impl.
        /// </devdoc> 
        bool ITypeDescriptorContext.OnComponentChanging() {
            try { 
                ChangeService.OnComponentChanging(_designer.Component, _targetProperty); 
            } catch (CheckoutException checkoutException) {
                if (checkoutException == CheckoutException.Canceled) { 
                    return false;
                }
                throw;
            } 
            return true;
        } 
 
        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc>
        object ITypeDescriptorContext.Instance {
            get {
                return _designer.Component; 
            }
        } 
 
        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc>
        PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor {
            get {
                return _targetProperty; 
            }
        } 
 
        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc>
        object IServiceProvider.GetService(Type serviceType) {
            if (serviceType == typeof(ITypeDescriptorContext) ||
                serviceType == typeof(IWindowsFormsEditorService)) { 
                return this;
            } 
 
            if (_designer.Component.Site != null) {
                return _designer.Component.Site.GetService(serviceType); 
            }
            return null;
        }
 
        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc> 
        void IWindowsFormsEditorService.CloseDropDown() {
            // we'll never be called to do this. 
            //
            Debug.Fail("NOTIMPL");
            return;
        } 

        /// <devdoc> 
        /// Self-explanitory interface impl. 
        /// </devdoc>
        void IWindowsFormsEditorService.DropDownControl(Control control) { 
            // nope, sorry
            //
            Debug.Fail("NOTIMPL");
            return; 
        }
 
        /// <devdoc> 
        /// Self-explanitory interface impl.
        /// </devdoc> 
        System.Windows.Forms.DialogResult IWindowsFormsEditorService.ShowDialog(Form dialog) {
            IUIService uiSvc = (IUIService)((IServiceProvider)this).GetService(typeof(IUIService));
            if (uiSvc != null) {
                return uiSvc.ShowDialog(dialog); 
            } else {
                return dialog.ShowDialog(_designer.Component as IWin32Window); 
            } 
        }
 
        /// <devdoc>
        /// When the verb is invoked, use all the stuff above to show the dialog, etc.
        /// </devdoc>
        private void OnEditItems(object sender, EventArgs e)     { 
            object propertyValue = _targetProperty.GetValue(_designer.Component);
            if (propertyValue == null) { 
                return; 
            }
            CollectionEditor itemsEditor = TypeDescriptor.GetEditor(propertyValue, typeof(UITypeEditor)) as CollectionEditor; 

            Debug.Assert(itemsEditor != null, "Didn't get a collection editor for type '" + _targetProperty.PropertyType.FullName + "'");
            if (itemsEditor != null) {
                itemsEditor.EditValue(this, this, propertyValue); 
            }
        } 
    } 
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="EditorServiceContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Windows.Forms.Design.Behavior; 
    using System.Drawing;
    using System.Drawing.Design; 
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis;
 
    /// <include file='doc\EditorServiceContext.uex' path='docs/doc[@for="EditorServiceContext"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides an implementation of IWindowsFormsEditorService and ITypeDescriptorContext. 
    ///		  Also provides a static method to invoke a UITypeEditor given a designer, an object
    ///		  and a property name. 
    ///	   </para> 
    /// </devdoc>
    internal class EditorServiceContext : IWindowsFormsEditorService, ITypeDescriptorContext { 

        private ComponentDesigner _designer;
        private IComponentChangeService _componentChangeSvc;
        private PropertyDescriptor _targetProperty; 

 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        internal EditorServiceContext(ComponentDesigner designer)    {
            this._designer = designer; 
        }

        internal EditorServiceContext(ComponentDesigner designer, PropertyDescriptor prop) {
            this._designer = designer; 
            this._targetProperty = prop;
 
            if (prop == null) { 
                prop = TypeDescriptor.GetDefaultProperty(designer.Component);
                if (prop != null && typeof(ICollection).IsAssignableFrom(prop.PropertyType)) { 
                    _targetProperty = prop;
                }
            }
 
            Debug.Assert(_targetProperty != null, "Need PropertyDescriptor for ICollection property to associate collectoin edtior with.");
        } 
 
        internal EditorServiceContext(ComponentDesigner designer, PropertyDescriptor prop, string newVerbText) : this(designer, prop) {
            Debug.Assert(!string.IsNullOrEmpty(newVerbText), "newVerbText cannot be null or empty"); 
            _designer.Verbs.Add(new DesignerVerb(newVerbText, new EventHandler(this.OnEditItems)));
        }

        public static object EditValue(ComponentDesigner designer, object objectToChange, string propName) { 
            // Get PropertyDescriptor
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(objectToChange)[propName]; 
 
            // Create a Context
            EditorServiceContext context = new EditorServiceContext(designer, descriptor); 

            // Get Editor
            UITypeEditor editor = descriptor.GetEditor(typeof(UITypeEditor)) as UITypeEditor;
 
            // Get value to edit
            object value = descriptor.GetValue(objectToChange); 
 
            // Edit value
            object newValue = editor.EditValue(context, context, value); 

            if (newValue != value) {
                try
                { 
                    descriptor.SetValue(objectToChange, newValue);
                } 
                catch (CheckoutException) { 

                } 
            }

            return newValue;
        } 

        /// <devdoc> 
        /// Our caching property for the IComponentChangeService 
        /// </devdoc>
        private IComponentChangeService ChangeService { 
            get {
                if (_componentChangeSvc == null) {
                    _componentChangeSvc = (IComponentChangeService)((IServiceProvider)this).GetService(typeof(IComponentChangeService));
                } 
                return _componentChangeSvc;
            } 
        } 

        /// <devdoc> 
        /// Self-explanitory interface impl.
        /// </devdoc>
        IContainer ITypeDescriptorContext.Container {
            get { 
                if (_designer.Component.Site != null) {
                    return _designer.Component.Site.Container; 
                } 
                return null;
            } 
        }

        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc>
        void ITypeDescriptorContext.OnComponentChanged() { 
            ChangeService.OnComponentChanged(_designer.Component, _targetProperty, null, null); 
        }
 

        /// <devdoc>
        /// Self-explanitory interface impl.
        /// </devdoc> 
        bool ITypeDescriptorContext.OnComponentChanging() {
            try { 
                ChangeService.OnComponentChanging(_designer.Component, _targetProperty); 
            } catch (CheckoutException checkoutException) {
                if (checkoutException == CheckoutException.Canceled) { 
                    return false;
                }
                throw;
            } 
            return true;
        } 
 
        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc>
        object ITypeDescriptorContext.Instance {
            get {
                return _designer.Component; 
            }
        } 
 
        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc>
        PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor {
            get {
                return _targetProperty; 
            }
        } 
 
        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc>
        object IServiceProvider.GetService(Type serviceType) {
            if (serviceType == typeof(ITypeDescriptorContext) ||
                serviceType == typeof(IWindowsFormsEditorService)) { 
                return this;
            } 
 
            if (_designer.Component.Site != null) {
                return _designer.Component.Site.GetService(serviceType); 
            }
            return null;
        }
 
        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc> 
        void IWindowsFormsEditorService.CloseDropDown() {
            // we'll never be called to do this. 
            //
            Debug.Fail("NOTIMPL");
            return;
        } 

        /// <devdoc> 
        /// Self-explanitory interface impl. 
        /// </devdoc>
        void IWindowsFormsEditorService.DropDownControl(Control control) { 
            // nope, sorry
            //
            Debug.Fail("NOTIMPL");
            return; 
        }
 
        /// <devdoc> 
        /// Self-explanitory interface impl.
        /// </devdoc> 
        System.Windows.Forms.DialogResult IWindowsFormsEditorService.ShowDialog(Form dialog) {
            IUIService uiSvc = (IUIService)((IServiceProvider)this).GetService(typeof(IUIService));
            if (uiSvc != null) {
                return uiSvc.ShowDialog(dialog); 
            } else {
                return dialog.ShowDialog(_designer.Component as IWin32Window); 
            } 
        }
 
        /// <devdoc>
        /// When the verb is invoked, use all the stuff above to show the dialog, etc.
        /// </devdoc>
        private void OnEditItems(object sender, EventArgs e)     { 
            object propertyValue = _targetProperty.GetValue(_designer.Component);
            if (propertyValue == null) { 
                return; 
            }
            CollectionEditor itemsEditor = TypeDescriptor.GetEditor(propertyValue, typeof(UITypeEditor)) as CollectionEditor; 

            Debug.Assert(itemsEditor != null, "Didn't get a collection editor for type '" + _targetProperty.PropertyType.FullName + "'");
            if (itemsEditor != null) {
                itemsEditor.EditValue(this, this, propertyValue); 
            }
        } 
    } 
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
