//------------------------------------------------------------------------------ 
// <copyright file="DesignerContextDescriptor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

namespace System.Windows.Forms.Design { 
    using System;
    using System.Design;
    using System.ComponentModel;
    using System.Windows.Forms; 
    using System.ComponentModel.Design;
    using System.Drawing; 
    using System.Windows.Forms.Design.Behavior; 
    using System.Drawing.Design;
    using System.Diagnostics; 
    using System.Collections;
    using System.Runtime.InteropServices;
    using System.Diagnostics.CodeAnalysis;
 

    /// <include file='doc\TemplateNode.uex' path='docs/doc[@for="TemplateNode"]/*' /> 
    /// <devdoc> 
    //  ---------------------------------------------------------------------------------
    //  Class Implemented for opening the ImageEditor ... 
    //
    //  This class is replaces the ContextDescriptor in ToolStripItem designer.
    //
    // DesignerContextDescriptor implements the IWindowsFormsEditorService which is required 
    // to open the ImageEditor .. Hence the need for this Implementation
    //----------------------------------------------------------------------------------- 
    /// </devdoc> 
    internal class DesignerContextDescriptor  : IWindowsFormsEditorService, ITypeDescriptorContext
    { 
        private Component _component;
        private PropertyDescriptor    _propertyDescriptor;
        private IDesignerHost _host;
 
        //
        // Constructor 
        // 
        /// <include file='doc\DesignerContextDescriptor.uex' path='docs/doc[@for="DesignerContextDescriptor.DesignerContextDescriptor"]/*' />
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        public DesignerContextDescriptor(Component component, PropertyDescriptor imageProperty, IDesignerHost host)
        {
            _component = component;
            _propertyDescriptor = imageProperty; 
            _host = host;
        } 
 
        /// <include file='doc\DesignerContextDescriptor.uex' path='docs/doc[@for="DesignerContextDescriptor.OpenImageCollection"]/*' />
        /// <devdoc> 
        ///    Gets called thru the TemplateNode to open the ImageEditor.
        /// </devdoc>
        /// <internalonly/>
 
        // Called through reflection
 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        public Image OpenImageCollection()
        { 
            object propertyValue = _propertyDescriptor.GetValue(_component);
            if (_propertyDescriptor != null) {
                Image image = null;
 
               UITypeEditor itemsEditor = _propertyDescriptor.GetEditor(typeof(UITypeEditor)) as UITypeEditor;
                Debug.Assert(itemsEditor != null, "Didn't get a collection editor for type '" + _propertyDescriptor.PropertyType.FullName + "'"); 
 
                if (itemsEditor != null) {
                    image = (Image)itemsEditor.EditValue(this, (IServiceProvider)this, propertyValue); 
                }
                if (image != null) {
                    return image;
                } 
            }
            // Always Return old Image if Image is not changed... 
            return (Image)propertyValue; 
        }
 
        IContainer ITypeDescriptorContext.Container {
            get {
                return null;
            } 
        }
 
        object ITypeDescriptorContext.Instance { 
            get {
                return _component; 
            }
        }

        PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor { 
            get {
                return _propertyDescriptor; 
            } 
        }
 
        void ITypeDescriptorContext.OnComponentChanged() {

        }
 
        bool ITypeDescriptorContext.OnComponentChanging() {
            return false; 
        } 

        /// <devdoc> 
        /// Self-explanitory interface impl.
        /// </devdoc>
        object IServiceProvider.GetService(Type serviceType)
        { 
            if (serviceType == typeof(IWindowsFormsEditorService)) {
                return this; 
            } 
            else {
                return _host.GetService(serviceType); 
            }
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
            IntPtr priorFocus = UnsafeNativeMethods.GetFocus(); 
            DialogResult result;
            IUIService uiSvc = (IUIService)((IServiceProvider)this).GetService(typeof(IUIService)); 
            if (uiSvc != null) { 
                result = uiSvc.ShowDialog(dialog);
            } 
            else {
                result = dialog.ShowDialog(_component as IWin32Window);
            }
            if (priorFocus != IntPtr.Zero) { 
                UnsafeNativeMethods.SetFocus(new HandleRef(null, priorFocus));
            } 
            return result; 
        }
 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerContextDescriptor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

namespace System.Windows.Forms.Design { 
    using System;
    using System.Design;
    using System.ComponentModel;
    using System.Windows.Forms; 
    using System.ComponentModel.Design;
    using System.Drawing; 
    using System.Windows.Forms.Design.Behavior; 
    using System.Drawing.Design;
    using System.Diagnostics; 
    using System.Collections;
    using System.Runtime.InteropServices;
    using System.Diagnostics.CodeAnalysis;
 

    /// <include file='doc\TemplateNode.uex' path='docs/doc[@for="TemplateNode"]/*' /> 
    /// <devdoc> 
    //  ---------------------------------------------------------------------------------
    //  Class Implemented for opening the ImageEditor ... 
    //
    //  This class is replaces the ContextDescriptor in ToolStripItem designer.
    //
    // DesignerContextDescriptor implements the IWindowsFormsEditorService which is required 
    // to open the ImageEditor .. Hence the need for this Implementation
    //----------------------------------------------------------------------------------- 
    /// </devdoc> 
    internal class DesignerContextDescriptor  : IWindowsFormsEditorService, ITypeDescriptorContext
    { 
        private Component _component;
        private PropertyDescriptor    _propertyDescriptor;
        private IDesignerHost _host;
 
        //
        // Constructor 
        // 
        /// <include file='doc\DesignerContextDescriptor.uex' path='docs/doc[@for="DesignerContextDescriptor.DesignerContextDescriptor"]/*' />
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        public DesignerContextDescriptor(Component component, PropertyDescriptor imageProperty, IDesignerHost host)
        {
            _component = component;
            _propertyDescriptor = imageProperty; 
            _host = host;
        } 
 
        /// <include file='doc\DesignerContextDescriptor.uex' path='docs/doc[@for="DesignerContextDescriptor.OpenImageCollection"]/*' />
        /// <devdoc> 
        ///    Gets called thru the TemplateNode to open the ImageEditor.
        /// </devdoc>
        /// <internalonly/>
 
        // Called through reflection
 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        public Image OpenImageCollection()
        { 
            object propertyValue = _propertyDescriptor.GetValue(_component);
            if (_propertyDescriptor != null) {
                Image image = null;
 
               UITypeEditor itemsEditor = _propertyDescriptor.GetEditor(typeof(UITypeEditor)) as UITypeEditor;
                Debug.Assert(itemsEditor != null, "Didn't get a collection editor for type '" + _propertyDescriptor.PropertyType.FullName + "'"); 
 
                if (itemsEditor != null) {
                    image = (Image)itemsEditor.EditValue(this, (IServiceProvider)this, propertyValue); 
                }
                if (image != null) {
                    return image;
                } 
            }
            // Always Return old Image if Image is not changed... 
            return (Image)propertyValue; 
        }
 
        IContainer ITypeDescriptorContext.Container {
            get {
                return null;
            } 
        }
 
        object ITypeDescriptorContext.Instance { 
            get {
                return _component; 
            }
        }

        PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor { 
            get {
                return _propertyDescriptor; 
            } 
        }
 
        void ITypeDescriptorContext.OnComponentChanged() {

        }
 
        bool ITypeDescriptorContext.OnComponentChanging() {
            return false; 
        } 

        /// <devdoc> 
        /// Self-explanitory interface impl.
        /// </devdoc>
        object IServiceProvider.GetService(Type serviceType)
        { 
            if (serviceType == typeof(IWindowsFormsEditorService)) {
                return this; 
            } 
            else {
                return _host.GetService(serviceType); 
            }
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
            IntPtr priorFocus = UnsafeNativeMethods.GetFocus(); 
            DialogResult result;
            IUIService uiSvc = (IUIService)((IServiceProvider)this).GetService(typeof(IUIService)); 
            if (uiSvc != null) { 
                result = uiSvc.ShowDialog(dialog);
            } 
            else {
                result = dialog.ShowDialog(_component as IWin32Window);
            }
            if (priorFocus != IntPtr.Zero) { 
                UnsafeNativeMethods.SetFocus(new HandleRef(null, priorFocus));
            } 
            return result; 
        }
 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
