//------------------------------------------------------------------------------ 
/// <copyright from='1997' to='2001' company='Microsoft Corporation'>
///    Copyright (c) Microsoft Corporation. All Rights Reserved.
///    Information Contained Herein is Proprietary and Confidential.
/// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Web.UI.Design.WebControls { 

    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Diagnostics;
    using System.Text; 
    using System.Web; 
    using System.Web.UI;
    using System.Web.UI.Design; 
    using System.Web.UI.WebControls;

    /// <include file='doc\SiteMapDataSourceDesigner.uex' path='docs/doc[@for="SiteMapDataSourceDesigner"]/*' />
    /// <devdoc> 
    /// <para>
    /// Provides design-time support for the 
    /// <see cref='System.Web.UI.WebControls.SiteMapDataSource'/> web control. 
    /// </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand,
        Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class SiteMapDataSourceDesigner : HierarchicalDataSourceDesigner, IDataSourceDesigner {
 
        internal static readonly SiteMapSchema SiteMapHierarchicalSchema = new SiteMapSchema();
 
        private SiteMapDataSource _siteMapDataSource; 
        private SiteMapProvider _siteMapProvider;
        private static readonly string _siteMapNodeType = typeof(SiteMapNode).Name; 

        public override bool CanRefreshSchema {
            get {
                return true; 
            }
        } 
 
        internal SiteMapProvider DesignTimeSiteMapProvider {
            get { 
                if (_siteMapProvider == null) {
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                    _siteMapProvider = new DesignTimeSiteMapProvider(host);
                } 

                return _siteMapProvider; 
            } 
        }
 
        internal SiteMapDataSource SiteMapDataSource {
            get {
                return _siteMapDataSource;
            } 
        }
 
        public override DesignerHierarchicalDataSourceView GetView(string viewPath) { 
            return new SiteMapDesignerHierarchicalDataSourceView(this, viewPath);
        } 

        public virtual string[] GetViewNames() {
            return new string[0];
        } 

        /// <include file='doc\SiteMapDataSourceDesigner.uex' path='docs/doc[@for="SiteMapDataSourceDesigner.Initialize"]/*' /> 
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(SiteMapDataSource));
            base.Initialize(component); 

            _siteMapDataSource = (SiteMapDataSource)component;
        }
 
        /// <include file='doc\SiteMapDataSourceDesigner.uex' path='docs/doc[@for="SiteMapDataSourceDesigner.OnComponentChanged"]/*' />
        public override void OnComponentChanged(object sender, ComponentChangedEventArgs e) { 
            base.OnComponentChanged(sender, e); 
            OnDataSourceChanged(EventArgs.Empty);
        } 

        public override void RefreshSchema(bool preferSilent) {
            try {
                SuppressDataSourceEvents(); 

                Debug.Assert(CanRefreshSchema, "CanRefreshSchema is false - RefreshSchema should not be called"); 
 
                // The schema of the SiteMapDataSource never changes, however
                // its design-time data can change. By clearing out the provider 
                // we will cause a new one to be created on the next query for
                // design time data, and it will have all the changes from the
                // .sitemap file.
                _siteMapProvider = null; 
                OnDataSourceChanged(EventArgs.Empty);
            } 
            finally { 
                ResumeDataSourceEvents();
            } 
        }

        internal class SiteMapSchema : IDataSourceSchema {
            IDataSourceViewSchema[] IDataSourceSchema.GetViews() { 
                return new SiteMapDataSourceViewSchema[] {new SiteMapDataSourceViewSchema()};
            } 
        } 

        internal class SiteMapDataSourceViewSchema : IDataSourceViewSchema { 
            string IDataSourceViewSchema.Name {
                get {
                    return _siteMapNodeType;
                } 
            }
 
            IDataSourceViewSchema[] IDataSourceViewSchema.GetChildren() { 
                return null;
            } 

            IDataSourceFieldSchema[] IDataSourceViewSchema.GetFields() {
                return new SiteMapDataSourceTextField[] {
                    SiteMapDataSourceTextField.DescriptionField, 
                    SiteMapDataSourceTextField.TitleField,
                    SiteMapDataSourceTextField.UrlField, 
                }; 
            }
        } 

        private class SiteMapDataSourceTextField : IDataSourceFieldSchema {
            internal static readonly SiteMapDataSourceTextField DescriptionField =
                new SiteMapDataSourceTextField("Description"); 

            internal static readonly SiteMapDataSourceTextField TitleField = 
                new SiteMapDataSourceTextField("Title"); 

            internal static readonly SiteMapDataSourceTextField UrlField = 
                new SiteMapDataSourceTextField("Url");

            private string _fieldName;
 
            internal SiteMapDataSourceTextField(string fieldName) {
                _fieldName = fieldName; 
            } 

            Type IDataSourceFieldSchema.DataType { get { return typeof(string); } } 
            bool IDataSourceFieldSchema.Identity { get { return false; } }
            bool IDataSourceFieldSchema.IsReadOnly { get { return true; } }
            bool IDataSourceFieldSchema.IsUnique { get { return false; } }
            int IDataSourceFieldSchema.Length { get { return -1; } } 
            string IDataSourceFieldSchema.Name { get { return _fieldName; } }
            bool IDataSourceFieldSchema.Nullable { get { return true; } } 
            int IDataSourceFieldSchema.Precision { get { return -1; } } 
            bool IDataSourceFieldSchema.PrimaryKey { get { return false; } }
            int IDataSourceFieldSchema.Scale { get { return -1; } } 
        }


        #region IDataSourceDesigner implementation 
        bool IDataSourceDesigner.CanConfigure {
            get { 
                return CanConfigure; 
            }
        } 

        bool IDataSourceDesigner.CanRefreshSchema {
            get {
                return CanRefreshSchema; 
            }
        } 
 
        event EventHandler IDataSourceDesigner.DataSourceChanged {
            add { 
                DataSourceChanged += value;
            }
            remove {
                DataSourceChanged -= value; 
            }
        } 
 
        event EventHandler IDataSourceDesigner.SchemaRefreshed {
            add { 
                SchemaRefreshed += value;
            }
            remove {
                SchemaRefreshed -= value; 
            }
        } 
 
        void IDataSourceDesigner.Configure() {
            Configure(); 
        }

        DesignerDataSourceView IDataSourceDesigner.GetView(string viewName) {
            return new SiteMapDesignerDataSourceView(this, viewName); 
        }
 
        string[] IDataSourceDesigner.GetViewNames() { 
            return GetViewNames();
        } 

        void IDataSourceDesigner.RefreshSchema(bool preferSilent) {
            RefreshSchema(preferSilent);
        } 

        void IDataSourceDesigner.ResumeDataSourceEvents() { 
            ResumeDataSourceEvents(); 
        }
 
        void IDataSourceDesigner.SuppressDataSourceEvents() {
            SuppressDataSourceEvents();
        }
        #endregion 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
/// <copyright from='1997' to='2001' company='Microsoft Corporation'>
///    Copyright (c) Microsoft Corporation. All Rights Reserved.
///    Information Contained Herein is Proprietary and Confidential.
/// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Web.UI.Design.WebControls { 

    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Diagnostics;
    using System.Text; 
    using System.Web; 
    using System.Web.UI;
    using System.Web.UI.Design; 
    using System.Web.UI.WebControls;

    /// <include file='doc\SiteMapDataSourceDesigner.uex' path='docs/doc[@for="SiteMapDataSourceDesigner"]/*' />
    /// <devdoc> 
    /// <para>
    /// Provides design-time support for the 
    /// <see cref='System.Web.UI.WebControls.SiteMapDataSource'/> web control. 
    /// </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand,
        Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class SiteMapDataSourceDesigner : HierarchicalDataSourceDesigner, IDataSourceDesigner {
 
        internal static readonly SiteMapSchema SiteMapHierarchicalSchema = new SiteMapSchema();
 
        private SiteMapDataSource _siteMapDataSource; 
        private SiteMapProvider _siteMapProvider;
        private static readonly string _siteMapNodeType = typeof(SiteMapNode).Name; 

        public override bool CanRefreshSchema {
            get {
                return true; 
            }
        } 
 
        internal SiteMapProvider DesignTimeSiteMapProvider {
            get { 
                if (_siteMapProvider == null) {
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                    _siteMapProvider = new DesignTimeSiteMapProvider(host);
                } 

                return _siteMapProvider; 
            } 
        }
 
        internal SiteMapDataSource SiteMapDataSource {
            get {
                return _siteMapDataSource;
            } 
        }
 
        public override DesignerHierarchicalDataSourceView GetView(string viewPath) { 
            return new SiteMapDesignerHierarchicalDataSourceView(this, viewPath);
        } 

        public virtual string[] GetViewNames() {
            return new string[0];
        } 

        /// <include file='doc\SiteMapDataSourceDesigner.uex' path='docs/doc[@for="SiteMapDataSourceDesigner.Initialize"]/*' /> 
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(SiteMapDataSource));
            base.Initialize(component); 

            _siteMapDataSource = (SiteMapDataSource)component;
        }
 
        /// <include file='doc\SiteMapDataSourceDesigner.uex' path='docs/doc[@for="SiteMapDataSourceDesigner.OnComponentChanged"]/*' />
        public override void OnComponentChanged(object sender, ComponentChangedEventArgs e) { 
            base.OnComponentChanged(sender, e); 
            OnDataSourceChanged(EventArgs.Empty);
        } 

        public override void RefreshSchema(bool preferSilent) {
            try {
                SuppressDataSourceEvents(); 

                Debug.Assert(CanRefreshSchema, "CanRefreshSchema is false - RefreshSchema should not be called"); 
 
                // The schema of the SiteMapDataSource never changes, however
                // its design-time data can change. By clearing out the provider 
                // we will cause a new one to be created on the next query for
                // design time data, and it will have all the changes from the
                // .sitemap file.
                _siteMapProvider = null; 
                OnDataSourceChanged(EventArgs.Empty);
            } 
            finally { 
                ResumeDataSourceEvents();
            } 
        }

        internal class SiteMapSchema : IDataSourceSchema {
            IDataSourceViewSchema[] IDataSourceSchema.GetViews() { 
                return new SiteMapDataSourceViewSchema[] {new SiteMapDataSourceViewSchema()};
            } 
        } 

        internal class SiteMapDataSourceViewSchema : IDataSourceViewSchema { 
            string IDataSourceViewSchema.Name {
                get {
                    return _siteMapNodeType;
                } 
            }
 
            IDataSourceViewSchema[] IDataSourceViewSchema.GetChildren() { 
                return null;
            } 

            IDataSourceFieldSchema[] IDataSourceViewSchema.GetFields() {
                return new SiteMapDataSourceTextField[] {
                    SiteMapDataSourceTextField.DescriptionField, 
                    SiteMapDataSourceTextField.TitleField,
                    SiteMapDataSourceTextField.UrlField, 
                }; 
            }
        } 

        private class SiteMapDataSourceTextField : IDataSourceFieldSchema {
            internal static readonly SiteMapDataSourceTextField DescriptionField =
                new SiteMapDataSourceTextField("Description"); 

            internal static readonly SiteMapDataSourceTextField TitleField = 
                new SiteMapDataSourceTextField("Title"); 

            internal static readonly SiteMapDataSourceTextField UrlField = 
                new SiteMapDataSourceTextField("Url");

            private string _fieldName;
 
            internal SiteMapDataSourceTextField(string fieldName) {
                _fieldName = fieldName; 
            } 

            Type IDataSourceFieldSchema.DataType { get { return typeof(string); } } 
            bool IDataSourceFieldSchema.Identity { get { return false; } }
            bool IDataSourceFieldSchema.IsReadOnly { get { return true; } }
            bool IDataSourceFieldSchema.IsUnique { get { return false; } }
            int IDataSourceFieldSchema.Length { get { return -1; } } 
            string IDataSourceFieldSchema.Name { get { return _fieldName; } }
            bool IDataSourceFieldSchema.Nullable { get { return true; } } 
            int IDataSourceFieldSchema.Precision { get { return -1; } } 
            bool IDataSourceFieldSchema.PrimaryKey { get { return false; } }
            int IDataSourceFieldSchema.Scale { get { return -1; } } 
        }


        #region IDataSourceDesigner implementation 
        bool IDataSourceDesigner.CanConfigure {
            get { 
                return CanConfigure; 
            }
        } 

        bool IDataSourceDesigner.CanRefreshSchema {
            get {
                return CanRefreshSchema; 
            }
        } 
 
        event EventHandler IDataSourceDesigner.DataSourceChanged {
            add { 
                DataSourceChanged += value;
            }
            remove {
                DataSourceChanged -= value; 
            }
        } 
 
        event EventHandler IDataSourceDesigner.SchemaRefreshed {
            add { 
                SchemaRefreshed += value;
            }
            remove {
                SchemaRefreshed -= value; 
            }
        } 
 
        void IDataSourceDesigner.Configure() {
            Configure(); 
        }

        DesignerDataSourceView IDataSourceDesigner.GetView(string viewName) {
            return new SiteMapDesignerDataSourceView(this, viewName); 
        }
 
        string[] IDataSourceDesigner.GetViewNames() { 
            return GetViewNames();
        } 

        void IDataSourceDesigner.RefreshSchema(bool preferSilent) {
            RefreshSchema(preferSilent);
        } 

        void IDataSourceDesigner.ResumeDataSourceEvents() { 
            ResumeDataSourceEvents(); 
        }
 
        void IDataSourceDesigner.SuppressDataSourceEvents() {
            SuppressDataSourceEvents();
        }
        #endregion 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
