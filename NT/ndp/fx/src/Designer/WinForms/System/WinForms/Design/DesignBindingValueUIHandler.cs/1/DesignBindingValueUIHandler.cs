//------------------------------------------------------------------------------ 
// <copyright file="DesignBindingValueUIHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System;
    using System.Collections; 
    using System.Windows.Forms;
    using Microsoft.Win32;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Drawing.Design;
    using System.Drawing; 
 
    /// <include file='doc\DesignBindingValueUIHandler.uex' path='docs/doc[@for="DesignBindingValueUIHandler"]/*' />
    /// <internalonly/> 
    /// <devdoc>
    /// </devdoc>
    internal class DesignBindingValueUIHandler : IDisposable {
 
        private Bitmap dataBitmap;
 
        internal Bitmap DataBitmap { 
            get {
                if (dataBitmap == null) { 
                    dataBitmap = new Bitmap(typeof(DesignBindingValueUIHandler), "BoundProperty.bmp");
                    dataBitmap.MakeTransparent();
                }
                return dataBitmap; 
            }
        } 
 

        internal void OnGetUIValueItem(ITypeDescriptorContext context, PropertyDescriptor propDesc, ArrayList valueUIItemList){ 
            if (context.Instance is Control) {
                Control control = (Control) context.Instance;
                foreach (Binding binding in control.DataBindings) {
 
                    // Only add the binding if it is one of the data source types we recognize.  Otherwise, our drop-down list won't show it as
                    // an option, which is confusing. 
                    if ((binding.DataSource is IListSource || binding.DataSource is IList || binding.DataSource is Array) &&  binding.PropertyName.Equals(propDesc.Name)) { 
                        valueUIItemList.Add(new LocalUIItem(this, binding));
                    } 
                }
            }
        }
 
        class LocalUIItem : PropertyValueUIItem {
            Binding binding; 
 
            internal LocalUIItem(DesignBindingValueUIHandler handler, Binding binding) : base(handler.DataBitmap, new PropertyValueUIItemInvokeHandler(handler.OnPropertyValueUIItemInvoke), GetToolTip(binding)) {
                this.binding = binding; 
            }

            internal Binding Binding {
                get { 
                    return binding;
                } 
            } 

            static string GetToolTip(Binding binding) { 
                string name = "";
                if (binding.DataSource is IComponent) {
                    IComponent comp = (IComponent) binding.DataSource;
                    if (comp.Site != null) { 
                        name = comp.Site.Name;
                    } 
                } 
                if (name.Length == 0) {
                    name = "(List)"; 
                }
                name += " - " + binding.BindingMemberInfo.BindingMember;
                return name;
            } 
        }
 
        private void OnPropertyValueUIItemInvoke(ITypeDescriptorContext context, PropertyDescriptor descriptor, PropertyValueUIItem invokedItem) { 
            LocalUIItem localItem = (LocalUIItem) invokedItem;
             IServiceProvider  sop = null; 
            Control control = localItem.Binding.Control;
            if (control.Site != null) {
               sop = ( IServiceProvider ) control.Site.GetService(typeof( IServiceProvider ));
            } 
            if (sop != null) {
                AdvancedBindingPropertyDescriptor.advancedBindingEditor.EditValue(context, sop, control.DataBindings); 
            } 
        }
 
        public void Dispose() {
            if (dataBitmap != null) {
                dataBitmap.Dispose();
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignBindingValueUIHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System;
    using System.Collections; 
    using System.Windows.Forms;
    using Microsoft.Win32;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Drawing.Design;
    using System.Drawing; 
 
    /// <include file='doc\DesignBindingValueUIHandler.uex' path='docs/doc[@for="DesignBindingValueUIHandler"]/*' />
    /// <internalonly/> 
    /// <devdoc>
    /// </devdoc>
    internal class DesignBindingValueUIHandler : IDisposable {
 
        private Bitmap dataBitmap;
 
        internal Bitmap DataBitmap { 
            get {
                if (dataBitmap == null) { 
                    dataBitmap = new Bitmap(typeof(DesignBindingValueUIHandler), "BoundProperty.bmp");
                    dataBitmap.MakeTransparent();
                }
                return dataBitmap; 
            }
        } 
 

        internal void OnGetUIValueItem(ITypeDescriptorContext context, PropertyDescriptor propDesc, ArrayList valueUIItemList){ 
            if (context.Instance is Control) {
                Control control = (Control) context.Instance;
                foreach (Binding binding in control.DataBindings) {
 
                    // Only add the binding if it is one of the data source types we recognize.  Otherwise, our drop-down list won't show it as
                    // an option, which is confusing. 
                    if ((binding.DataSource is IListSource || binding.DataSource is IList || binding.DataSource is Array) &&  binding.PropertyName.Equals(propDesc.Name)) { 
                        valueUIItemList.Add(new LocalUIItem(this, binding));
                    } 
                }
            }
        }
 
        class LocalUIItem : PropertyValueUIItem {
            Binding binding; 
 
            internal LocalUIItem(DesignBindingValueUIHandler handler, Binding binding) : base(handler.DataBitmap, new PropertyValueUIItemInvokeHandler(handler.OnPropertyValueUIItemInvoke), GetToolTip(binding)) {
                this.binding = binding; 
            }

            internal Binding Binding {
                get { 
                    return binding;
                } 
            } 

            static string GetToolTip(Binding binding) { 
                string name = "";
                if (binding.DataSource is IComponent) {
                    IComponent comp = (IComponent) binding.DataSource;
                    if (comp.Site != null) { 
                        name = comp.Site.Name;
                    } 
                } 
                if (name.Length == 0) {
                    name = "(List)"; 
                }
                name += " - " + binding.BindingMemberInfo.BindingMember;
                return name;
            } 
        }
 
        private void OnPropertyValueUIItemInvoke(ITypeDescriptorContext context, PropertyDescriptor descriptor, PropertyValueUIItem invokedItem) { 
            LocalUIItem localItem = (LocalUIItem) invokedItem;
             IServiceProvider  sop = null; 
            Control control = localItem.Binding.Control;
            if (control.Site != null) {
               sop = ( IServiceProvider ) control.Site.GetService(typeof( IServiceProvider ));
            } 
            if (sop != null) {
                AdvancedBindingPropertyDescriptor.advancedBindingEditor.EditValue(context, sop, control.DataBindings); 
            } 
        }
 
        public void Dispose() {
            if (dataBitmap != null) {
                dataBitmap.Dispose();
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
