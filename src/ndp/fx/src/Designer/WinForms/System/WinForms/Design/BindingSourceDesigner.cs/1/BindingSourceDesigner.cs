//------------------------------------------------------------------------------ 
// <copyright file="BindingSourceDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.BindingSourceDesigner..ctor()")] 
 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Design;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.ComponentModel.Design.Data;
    using System.ComponentModel.Design.Serialization; 
    using System.Diagnostics; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design.Behavior; 
    using System.Reflection;

    /// <include file='doc\BindingSourceDesigner.uex' path='docs/doc[@for="BindingSourceDesigner"]/*' />
    /// <devdoc> 
    ///     Designer for the BindingSource class.
    /// </devdoc> 
    internal class BindingSourceDesigner : ComponentDesigner { 

        private bool bindingUpdatedByUser = false; 

        /// <summary>
        ///     Flag set by UITypeEditors to notify us when the BindingSource binding has been
        ///     manually changed by the user (ie. DataSource or DataMember value was edited). 
        ///
        ///     In response to this action, we will send a notification to the DataSourceProviderService 
        ///     once the corresponding ComponentChanged event comes through (indicating that the new 
        ///     binding has actually taken effect).
        /// 
        ///     This notification will cause the DataSourceProviderService to re-examine the data source
        ///     that the BindingSource is now bound to, so it can generate any table adapters or fill
        ///     statements necessary to properly set up that data source.
        /// 
        ///     This serves the scenario of an advanced user configuring a BindingSource manually. In
        ///     normal user scenarios, BindingSources get auto-generated as a result of various drag/drop 
        ///     operations, and in those scenarios the above notification is sent automatically. 
        ///
        ///     VSWhidbey#256272 
        /// </summary>
        public bool BindingUpdatedByUser {
            set {
                bindingUpdatedByUser = value; 
            }
        } 
 
        /// <summary>
        ///     Initialize this designer. 
        /// </summary>
        public override void Initialize(IComponent component) {
            base.Initialize(component);
 
            IComponentChangeService componentChangeSvc = (IComponentChangeService) GetService(typeof(IComponentChangeService));
            if (componentChangeSvc != null) { 
                componentChangeSvc.ComponentChanged += new ComponentChangedEventHandler(OnComponentChanged); 
                componentChangeSvc.ComponentRemoving+= new ComponentEventHandler(OnComponentRemoving);
            } 
        }

        /// <summary>
        ///     Dispose this designer. 
        /// </summary>
        protected override void Dispose(bool disposing) { 
            if (disposing) { 
                IComponentChangeService componentChangeSvc = (IComponentChangeService) GetService(typeof(IComponentChangeService));
                if (componentChangeSvc != null) { 
                    componentChangeSvc.ComponentChanged -= new ComponentChangedEventHandler(OnComponentChanged);
                    componentChangeSvc.ComponentRemoving-= new ComponentEventHandler(OnComponentRemoving);
                }
            } 

            base.Dispose(disposing); 
        } 

        /// <summary> 
        ///     Spot when a change to the BindingSource's DataSource or DataMember is made by the user, and notify the
        ///     DataSourceProviderService accordingly. See description of BindingUpdatedByUser property for full details.
        /// </summary>
        private void OnComponentChanged(object sender, ComponentChangedEventArgs e) { 
            if (bindingUpdatedByUser &&
                e.Component == this.Component && 
                e.Member != null && (e.Member.Name == "DataSource" || e.Member.Name == "DataMember")) { 

                bindingUpdatedByUser = false; 

                DataSourceProviderService dspSvc = (DataSourceProviderService) GetService(typeof(DataSourceProviderService));
                if (dspSvc != null) {
                    dspSvc.NotifyDataSourceComponentAdded(Component); 
                }
            } 
        } 

        private void OnComponentRemoving(object sender, ComponentEventArgs e) { 
            BindingSource b = this.Component as BindingSource;
            if (b != null && b.DataSource == e.Component) {
                IComponentChangeService ccs = (IComponentChangeService) GetService(typeof(IComponentChangeService));
                string previousDataMember = b.DataMember; 

                PropertyDescriptorCollection props = TypeDescriptor.GetProperties(b); 
                PropertyDescriptor dmPD = props != null ? props["DataMember"] : null; 

                if (ccs != null) { 
                    if (dmPD != null) {
                        ccs.OnComponentChanging(b, dmPD);
                    }
                } 

                b.DataSource = null; 
 
                if (ccs != null) {
                   if (dmPD != null) { 
                       ccs.OnComponentChanged(b, dmPD, previousDataMember, "");
                   }
                }
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="BindingSourceDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.BindingSourceDesigner..ctor()")] 
 
namespace System.Windows.Forms.Design {
    using System; 
    using System.Design;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.ComponentModel.Design.Data;
    using System.ComponentModel.Design.Serialization; 
    using System.Diagnostics; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design.Behavior; 
    using System.Reflection;

    /// <include file='doc\BindingSourceDesigner.uex' path='docs/doc[@for="BindingSourceDesigner"]/*' />
    /// <devdoc> 
    ///     Designer for the BindingSource class.
    /// </devdoc> 
    internal class BindingSourceDesigner : ComponentDesigner { 

        private bool bindingUpdatedByUser = false; 

        /// <summary>
        ///     Flag set by UITypeEditors to notify us when the BindingSource binding has been
        ///     manually changed by the user (ie. DataSource or DataMember value was edited). 
        ///
        ///     In response to this action, we will send a notification to the DataSourceProviderService 
        ///     once the corresponding ComponentChanged event comes through (indicating that the new 
        ///     binding has actually taken effect).
        /// 
        ///     This notification will cause the DataSourceProviderService to re-examine the data source
        ///     that the BindingSource is now bound to, so it can generate any table adapters or fill
        ///     statements necessary to properly set up that data source.
        /// 
        ///     This serves the scenario of an advanced user configuring a BindingSource manually. In
        ///     normal user scenarios, BindingSources get auto-generated as a result of various drag/drop 
        ///     operations, and in those scenarios the above notification is sent automatically. 
        ///
        ///     VSWhidbey#256272 
        /// </summary>
        public bool BindingUpdatedByUser {
            set {
                bindingUpdatedByUser = value; 
            }
        } 
 
        /// <summary>
        ///     Initialize this designer. 
        /// </summary>
        public override void Initialize(IComponent component) {
            base.Initialize(component);
 
            IComponentChangeService componentChangeSvc = (IComponentChangeService) GetService(typeof(IComponentChangeService));
            if (componentChangeSvc != null) { 
                componentChangeSvc.ComponentChanged += new ComponentChangedEventHandler(OnComponentChanged); 
                componentChangeSvc.ComponentRemoving+= new ComponentEventHandler(OnComponentRemoving);
            } 
        }

        /// <summary>
        ///     Dispose this designer. 
        /// </summary>
        protected override void Dispose(bool disposing) { 
            if (disposing) { 
                IComponentChangeService componentChangeSvc = (IComponentChangeService) GetService(typeof(IComponentChangeService));
                if (componentChangeSvc != null) { 
                    componentChangeSvc.ComponentChanged -= new ComponentChangedEventHandler(OnComponentChanged);
                    componentChangeSvc.ComponentRemoving-= new ComponentEventHandler(OnComponentRemoving);
                }
            } 

            base.Dispose(disposing); 
        } 

        /// <summary> 
        ///     Spot when a change to the BindingSource's DataSource or DataMember is made by the user, and notify the
        ///     DataSourceProviderService accordingly. See description of BindingUpdatedByUser property for full details.
        /// </summary>
        private void OnComponentChanged(object sender, ComponentChangedEventArgs e) { 
            if (bindingUpdatedByUser &&
                e.Component == this.Component && 
                e.Member != null && (e.Member.Name == "DataSource" || e.Member.Name == "DataMember")) { 

                bindingUpdatedByUser = false; 

                DataSourceProviderService dspSvc = (DataSourceProviderService) GetService(typeof(DataSourceProviderService));
                if (dspSvc != null) {
                    dspSvc.NotifyDataSourceComponentAdded(Component); 
                }
            } 
        } 

        private void OnComponentRemoving(object sender, ComponentEventArgs e) { 
            BindingSource b = this.Component as BindingSource;
            if (b != null && b.DataSource == e.Component) {
                IComponentChangeService ccs = (IComponentChangeService) GetService(typeof(IComponentChangeService));
                string previousDataMember = b.DataMember; 

                PropertyDescriptorCollection props = TypeDescriptor.GetProperties(b); 
                PropertyDescriptor dmPD = props != null ? props["DataMember"] : null; 

                if (ccs != null) { 
                    if (dmPD != null) {
                        ccs.OnComponentChanging(b, dmPD);
                    }
                } 

                b.DataSource = null; 
 
                if (ccs != null) {
                   if (dmPD != null) { 
                       ccs.OnComponentChanged(b, dmPD, previousDataMember, "");
                   }
                }
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
