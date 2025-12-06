 
//------------------------------------------------------------------------------
// <copyright file="CollectionEditVerbManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.Windows.Forms.Design { 
    using System.Design;
    using Accessibility;
    using System.Runtime.Serialization.Formatters;
    using System.Threading; 
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Diagnostics; 
    using System;
    using System.Security; 
    using System.Security.Permissions;
    using System.Collections;
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Drawing;
    using System.Drawing.Design; 
    using Microsoft.Win32; 

    /// <devdoc> 
    /// Class for sharing code for launching the ToolStripItemsCollectionEditor from a verb.
    /// This class implments the IWindowsFormsEditorService and ITypeDescriptorContext to
    /// display the dialog.
    /// 
    /// </devdoc>
    internal class CollectionEditVerbManager : IWindowsFormsEditorService, ITypeDescriptorContext { 
 
        private ComponentDesigner               _designer;
        private IComponentChangeService         _componentChangeSvc; 
        private PropertyDescriptor              _targetProperty;
        private DesignerVerb                    _editItemsVerb;

        /// <devdoc> 
        /// Create one of these things...
        /// </devdoc> 
        internal CollectionEditVerbManager(string text, ComponentDesigner designer, PropertyDescriptor prop, bool addToDesignerVerbs) { 
            Debug.Assert(designer != null, "Can't have a CollectionEditVerbManager without an associated designer");
 
            this._designer = designer;
            this._targetProperty = prop;

            if (prop == null) { 
                prop = TypeDescriptor.GetDefaultProperty(designer.Component);
                if (prop != null && typeof(ICollection).IsAssignableFrom(prop.PropertyType)) { 
                    _targetProperty = prop; 
                }
            } 

            Debug.Assert(_targetProperty != null, "Need PropertyDescriptor for ICollection property to associate collectoin edtior with.");

            if (text == null) { 
                text = SR.GetString(SR.ToolStripItemCollectionEditorVerb);
            } 
            _editItemsVerb = new DesignerVerb(text, new EventHandler(this.OnEditItems)); 

            if (addToDesignerVerbs) 
            {
                _designer.Verbs.Add(_editItemsVerb);
            }
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
        IContainer ITypeDescriptorContext.Container
        { 
            get
            { 
                if (_designer.Component.Site != null) { 
                    return _designer.Component.Site.Container;
                } 
                return null;
            }
        }
 

        public DesignerVerb EditItemsVerb 
        { 
            get
            { 
                return _editItemsVerb;
            }
        }
 
        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc> 
        void ITypeDescriptorContext.OnComponentChanged()
        { 
            ChangeService.OnComponentChanged(_designer.Component, _targetProperty, null, null);
        }

 
        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc> 
        bool ITypeDescriptorContext.OnComponentChanging()
        { 
            try {
                ChangeService.OnComponentChanging(_designer.Component, _targetProperty);
            }
            catch(CheckoutException checkoutException) { 
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
        object ITypeDescriptorContext.Instance 
        {
            get 
            {
                return _designer.Component;
            }
        } 

        /// <devdoc> 
        /// Self-explanitory interface impl. 
        /// </devdoc>
        PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor 
        {
            get
            {
                return _targetProperty; 
            }
        } 
 
        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc>
        object IServiceProvider.GetService(Type serviceType)
        {
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
        void IWindowsFormsEditorService.CloseDropDown() 
        {
            // we'll never be called to do this.
            //
            Debug.Fail("NOTIMPL"); 
            return;
        } 
 
        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc>
        void IWindowsFormsEditorService.DropDownControl(Control control)
        {
            // nope, sorry 
            //
            Debug.Fail("NOTIMPL"); 
            return; 
        }
 
        /// <devdoc>
        /// Self-explanitory interface impl.
        /// </devdoc>
        System.Windows.Forms.DialogResult IWindowsFormsEditorService.ShowDialog(Form dialog) 
        {
            IUIService uiSvc = (IUIService)((IServiceProvider)this).GetService(typeof(IUIService)); 
            if (uiSvc != null) { 
                return uiSvc.ShowDialog(dialog);
            } 
            else {
                return dialog.ShowDialog(_designer.Component as IWin32Window);
            }
        } 

        /// <devdoc> 
        /// When the verb is invoked, use all the stuff above to show the dialog, etc. 
        /// </devdoc>
        private void OnEditItems(object sender, EventArgs e) { 
            // Hide the Chrome..
            DesignerActionUIService actionUIService = (DesignerActionUIService)((IServiceProvider)this).GetService(typeof(DesignerActionUIService));
            if (actionUIService != null)
            { 
               actionUIService.HideUI(_designer.Component);
            } 
 
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
// <copyright file="CollectionEditVerbManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
/* 
 */
namespace System.Windows.Forms.Design { 
    using System.Design;
    using Accessibility;
    using System.Runtime.Serialization.Formatters;
    using System.Threading; 
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Diagnostics; 
    using System;
    using System.Security; 
    using System.Security.Permissions;
    using System.Collections;
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Drawing;
    using System.Drawing.Design; 
    using Microsoft.Win32; 

    /// <devdoc> 
    /// Class for sharing code for launching the ToolStripItemsCollectionEditor from a verb.
    /// This class implments the IWindowsFormsEditorService and ITypeDescriptorContext to
    /// display the dialog.
    /// 
    /// </devdoc>
    internal class CollectionEditVerbManager : IWindowsFormsEditorService, ITypeDescriptorContext { 
 
        private ComponentDesigner               _designer;
        private IComponentChangeService         _componentChangeSvc; 
        private PropertyDescriptor              _targetProperty;
        private DesignerVerb                    _editItemsVerb;

        /// <devdoc> 
        /// Create one of these things...
        /// </devdoc> 
        internal CollectionEditVerbManager(string text, ComponentDesigner designer, PropertyDescriptor prop, bool addToDesignerVerbs) { 
            Debug.Assert(designer != null, "Can't have a CollectionEditVerbManager without an associated designer");
 
            this._designer = designer;
            this._targetProperty = prop;

            if (prop == null) { 
                prop = TypeDescriptor.GetDefaultProperty(designer.Component);
                if (prop != null && typeof(ICollection).IsAssignableFrom(prop.PropertyType)) { 
                    _targetProperty = prop; 
                }
            } 

            Debug.Assert(_targetProperty != null, "Need PropertyDescriptor for ICollection property to associate collectoin edtior with.");

            if (text == null) { 
                text = SR.GetString(SR.ToolStripItemCollectionEditorVerb);
            } 
            _editItemsVerb = new DesignerVerb(text, new EventHandler(this.OnEditItems)); 

            if (addToDesignerVerbs) 
            {
                _designer.Verbs.Add(_editItemsVerb);
            }
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
        IContainer ITypeDescriptorContext.Container
        { 
            get
            { 
                if (_designer.Component.Site != null) { 
                    return _designer.Component.Site.Container;
                } 
                return null;
            }
        }
 

        public DesignerVerb EditItemsVerb 
        { 
            get
            { 
                return _editItemsVerb;
            }
        }
 
        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc> 
        void ITypeDescriptorContext.OnComponentChanged()
        { 
            ChangeService.OnComponentChanged(_designer.Component, _targetProperty, null, null);
        }

 
        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc> 
        bool ITypeDescriptorContext.OnComponentChanging()
        { 
            try {
                ChangeService.OnComponentChanging(_designer.Component, _targetProperty);
            }
            catch(CheckoutException checkoutException) { 
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
        object ITypeDescriptorContext.Instance 
        {
            get 
            {
                return _designer.Component;
            }
        } 

        /// <devdoc> 
        /// Self-explanitory interface impl. 
        /// </devdoc>
        PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor 
        {
            get
            {
                return _targetProperty; 
            }
        } 
 
        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc>
        object IServiceProvider.GetService(Type serviceType)
        {
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
        void IWindowsFormsEditorService.CloseDropDown() 
        {
            // we'll never be called to do this.
            //
            Debug.Fail("NOTIMPL"); 
            return;
        } 
 
        /// <devdoc>
        /// Self-explanitory interface impl. 
        /// </devdoc>
        void IWindowsFormsEditorService.DropDownControl(Control control)
        {
            // nope, sorry 
            //
            Debug.Fail("NOTIMPL"); 
            return; 
        }
 
        /// <devdoc>
        /// Self-explanitory interface impl.
        /// </devdoc>
        System.Windows.Forms.DialogResult IWindowsFormsEditorService.ShowDialog(Form dialog) 
        {
            IUIService uiSvc = (IUIService)((IServiceProvider)this).GetService(typeof(IUIService)); 
            if (uiSvc != null) { 
                return uiSvc.ShowDialog(dialog);
            } 
            else {
                return dialog.ShowDialog(_designer.Component as IWin32Window);
            }
        } 

        /// <devdoc> 
        /// When the verb is invoked, use all the stuff above to show the dialog, etc. 
        /// </devdoc>
        private void OnEditItems(object sender, EventArgs e) { 
            // Hide the Chrome..
            DesignerActionUIService actionUIService = (DesignerActionUIService)((IServiceProvider)this).GetService(typeof(DesignerActionUIService));
            if (actionUIService != null)
            { 
               actionUIService.HideUI(_designer.Component);
            } 
 
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
