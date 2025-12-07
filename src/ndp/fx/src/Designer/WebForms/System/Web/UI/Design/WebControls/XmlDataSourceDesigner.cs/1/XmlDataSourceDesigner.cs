//------------------------------------------------------------------------------ 
// <copyright file="XmlDataSourceDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design;
    using System.Diagnostics; 
    using System.IO;
    using System.Web.UI; 
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design;
    using System.Xml;
    using System.Xml.XPath; 

    /// <include file='doc\XmlDataSourceDesigner.uex' path='docs/doc[@for="XmlDataSourceDesigner"]/*' /> 
    /// <devdoc> 
    /// The designer for XmlDataSource controls.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class XmlDataSourceDesigner : HierarchicalDataSourceDesigner, IDataSourceDesigner {

        private string _mappedDataFile; 
        private string _mappedTransformFile;
 
        private XmlDataSource _xmlDataSource; 
        private XmlDesignerDataSourceView _view;
 
        private static readonly string[] _shadowProperties = new string[] {
            "Data",
            "DataFile",
            "Transform", 
            "TransformFile",
            "XPath", 
        }; 

 
        /// <include file='doc\XmlDataSourceDesigner.uex' path='docs/doc[@for="XmlDataSourceDesigner.ConfigureEnabled"]/*' />
        /// <devdoc>
        /// </devdoc>
        public override bool CanConfigure { 
            get {
                return true; 
            } 
        }
 
        public override bool CanRefreshSchema {
            get {
                return true;
            } 
        }
 
        /// <devdoc> 
        /// Shadow property to raise the SchemaRefreshed event.
        /// </devdoc> 
        public string Data {
            get {
                return XmlDataSource.Data;
            } 
            set {
                if (value != XmlDataSource.Data) { 
                    XmlDataSource.Data = value; 
                    OnDataSourceChanged(EventArgs.Empty);
                    OnSchemaRefreshed(EventArgs.Empty); 
                }
            }
        }
 
        /// <devdoc>
        /// Shadow property to raise the SchemaRefreshed event. 
        /// </devdoc> 
        public string DataFile {
            get { 
                return XmlDataSource.DataFile;
            }
            set {
                if (value != XmlDataSource.DataFile) { 
                    _mappedDataFile = null;
                    XmlDataSource.DataFile = value; 
                    OnDataSourceChanged(EventArgs.Empty); 
                    OnSchemaRefreshed(EventArgs.Empty);
                } 
            }
        }

        /// <devdoc> 
        /// Shadow property to raise the SchemaRefreshed event.
        /// </devdoc> 
        public string Transform { 
            get {
                return XmlDataSource.Transform; 
            }
            set {
                if (value != XmlDataSource.Transform) {
                    XmlDataSource.Transform = value; 
                    OnDataSourceChanged(EventArgs.Empty);
                    OnSchemaRefreshed(EventArgs.Empty); 
                } 
            }
        } 

        /// <devdoc>
        /// Shadow property to raise the SchemaRefreshed event.
        /// </devdoc> 
        public string TransformFile {
            get { 
                return XmlDataSource.TransformFile; 
            }
            set { 
                if (value != XmlDataSource.TransformFile) {
                    _mappedTransformFile = null;
                    XmlDataSource.TransformFile = value;
                    OnDataSourceChanged(EventArgs.Empty); 
                    OnSchemaRefreshed(EventArgs.Empty);
                } 
            } 
        }
 
        /// <include file='doc\XmlDataSourceDesigner.uex' path='docs/doc[@for="XmlDataSourceDesigner.ConfigureVisible"]/*' />
        /// <devdoc>
        /// The XmlDataSource associated with this designer.
        /// </devdoc> 
        private XmlDataSource XmlDataSource {
            get { 
                return _xmlDataSource; 
            }
        } 

        /// <devdoc>
        /// Shadow property to raise the SchemaRefreshed event.
        /// </devdoc> 
        public string XPath {
            get { 
                return XmlDataSource.XPath; 
            }
            set { 
                if (value != XmlDataSource.XPath) {
                    XmlDataSource.XPath = value;
                    OnDataSourceChanged(EventArgs.Empty);
                    OnSchemaRefreshed(EventArgs.Empty); 
                }
            } 
        } 

 
        /// <include file='doc\XmlDataSourceDesigner.uex' path='docs/doc[@for="XmlDataSourceDesigner.Configure"]/*' />
        /// <devdoc>
        /// Handles the Configure DataSource designer verb event.
        /// </devdoc> 
        public override void Configure() {
            InvokeTransactedChange(Component, new TransactedChangeCallback(ConfigureDataSourceChangeCallback), null, SR.GetString(SR.DataSource_ConfigureTransactionDescription)); 
        } 

        /// <devdoc> 
        /// Transacted change callback to invoke the Configure DataSource dialog.
        /// </devdoc>
        private bool ConfigureDataSourceChangeCallback(object context) {
            try { 
                SuppressDataSourceEvents();
                IServiceProvider site = Component.Site; 
                XmlDataSourceConfigureDataSourceForm form = new XmlDataSourceConfigureDataSourceForm(site, XmlDataSource); 
                DialogResult result = UIServiceHelper.ShowDialog(site, form);
                return (result == DialogResult.OK); 
            }
            finally {
                ResumeDataSourceEvents();
            } 
        }
 
        internal XmlDataSource GetDesignTimeXmlDataSource(string viewPath) { 
            XmlDataSource designTimeXmlDataSource = new XmlDataSource();
 
            designTimeXmlDataSource.EnableCaching = false;

            designTimeXmlDataSource.Data = XmlDataSource.Data;
            designTimeXmlDataSource.Transform = XmlDataSource.Transform; 
            designTimeXmlDataSource.XPath = (String.IsNullOrEmpty(viewPath) ? XmlDataSource.XPath : viewPath);
 
            // Change the DataFile's path from a relative path to a physical path if necessary 
            if (XmlDataSource.DataFile.Length > 0) {
                if (_mappedDataFile == null) { 
                    _mappedDataFile = UrlPath.MapPath(Component.Site, XmlDataSource.DataFile);
                }
                designTimeXmlDataSource.DataFile = _mappedDataFile;
 
                // Check if the file exists
                if (!File.Exists(designTimeXmlDataSource.DataFile)) { 
                    return null; 
                }
            } 
            else {
                if (designTimeXmlDataSource.Data.Length == 0) {
                    // If neither a DataFile, nor inline data are specified, we can't load data using the runtime control
                    return null; 
                }
            } 
 
            // Change the TransformFile's path from a relative path to a physical path if necessary
            if (XmlDataSource.TransformFile.Length > 0) { 
                if (_mappedTransformFile == null) {
                    _mappedTransformFile = UrlPath.MapPath(Component.Site, XmlDataSource.TransformFile);
                }
                designTimeXmlDataSource.TransformFile = _mappedTransformFile; 

                // Check if the file exists 
                if (!File.Exists(designTimeXmlDataSource.TransformFile)) { 
                    return null;
                } 
            }

            return designTimeXmlDataSource;
        } 

        /// <devdoc> 
        /// Uses the runtime control to get XML data representing the hierarchical data specified in the path parameter. 
        /// </devdoc>
        internal IHierarchicalEnumerable GetHierarchicalRuntimeEnumerable(string path) { 
            XmlDataSource xmlDataSource = GetDesignTimeXmlDataSource(String.Empty);
            if (xmlDataSource == null) {
                return null;
            } 
            HierarchicalDataSourceView view = ((IHierarchicalDataSource)xmlDataSource).GetHierarchicalView(path);
            if (view == null) { 
                return null; 
            }
            return view.Select(); 
        }

        /// <devdoc>
        /// Uses the runtime control to get XML data representing the list specified in the listName parameter. 
        /// If no rows are returned, this method will return null.
        /// </devdoc> 
        internal IEnumerable GetRuntimeEnumerable(string listName) { 
            XmlDataSource xmlDataSource = GetDesignTimeXmlDataSource(String.Empty);
            if (xmlDataSource == null) { 
                return null;
            }
            XmlDataSourceView view = (XmlDataSourceView)((IDataSource)xmlDataSource).GetView(listName);
            if (view == null) { 
                return null;
            } 
 
            // Get rows from data source
            IEnumerable rows = view.Select(DataSourceSelectArguments.Empty); 

            // Try to determine if there is any data
            ICollection rowCollection = rows as ICollection;
            if ((rowCollection != null) && (rowCollection.Count == 0)) { 
                // Definitely no data in the data source
                return null; 
            } 

            // Either the data source did not implement ICollection (should 
            // never be the case here), or there was data in the data source.
            return rows;
        }
 
        public override DesignerHierarchicalDataSourceView GetView(string viewPath) {
            return new XmlDesignerHierarchicalDataSourceView(this, viewPath); 
        } 

        /// <include file='doc\XmlDataSourceDesigner.uex' path='docs/doc[@for="XmlDataSourceDesigner.Initialize"]/*' /> 
        /// <devdoc>
        /// Initializes the designer using the specified component.
        /// </devdoc>
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(XmlDataSource));
            base.Initialize(component); 
 
            _xmlDataSource = (XmlDataSource)component;
        } 

        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties);
 
            // Add DataMember type converter
            PropertyDescriptor property; 
 
            foreach (string propName in _shadowProperties) {
                property = (PropertyDescriptor)properties[propName]; 
                Debug.Assert(property != null, "Expected a property named '" + propName + "'");
                properties[propName] = TypeDescriptor.CreateProperty(GetType(), property);
            }
        } 

        public override void RefreshSchema(bool preferSilent) { 
            try { 
                SuppressDataSourceEvents();
 
                Debug.Assert(CanRefreshSchema, "CanRefreshSchema is false - RefreshSchema should not be called");

                // Since there is no way to actually know when the schema has changed,
                // we always raise the SchemaRefreshed event. The problem is that we 
                // can't retain an "old" copy of the schema since the schema is dependent
                // on the view that was using it (in terms of what XPath was used to create 
                // it). Since it is not practical to store every combination of XPath/Schema 
                // we go the simple route and don't store it at all.
                OnDataSourceChanged(EventArgs.Empty); 
                OnSchemaRefreshed(EventArgs.Empty);
            }
            finally {
                ResumeDataSourceEvents(); 
            }
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
            if (!String.IsNullOrEmpty(viewName)) { 
                return null;
            }
            if (_view == null) {
                _view = new XmlDesignerDataSourceView(this, String.Empty); 
            }
            return _view; 
        } 

        string[] IDataSourceDesigner.GetViewNames() { 
            return new string[0];
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
// <copyright file="XmlDataSourceDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Data;
    using System.Design;
    using System.Diagnostics; 
    using System.IO;
    using System.Web.UI; 
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design;
    using System.Xml;
    using System.Xml.XPath; 

    /// <include file='doc\XmlDataSourceDesigner.uex' path='docs/doc[@for="XmlDataSourceDesigner"]/*' /> 
    /// <devdoc> 
    /// The designer for XmlDataSource controls.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class XmlDataSourceDesigner : HierarchicalDataSourceDesigner, IDataSourceDesigner {

        private string _mappedDataFile; 
        private string _mappedTransformFile;
 
        private XmlDataSource _xmlDataSource; 
        private XmlDesignerDataSourceView _view;
 
        private static readonly string[] _shadowProperties = new string[] {
            "Data",
            "DataFile",
            "Transform", 
            "TransformFile",
            "XPath", 
        }; 

 
        /// <include file='doc\XmlDataSourceDesigner.uex' path='docs/doc[@for="XmlDataSourceDesigner.ConfigureEnabled"]/*' />
        /// <devdoc>
        /// </devdoc>
        public override bool CanConfigure { 
            get {
                return true; 
            } 
        }
 
        public override bool CanRefreshSchema {
            get {
                return true;
            } 
        }
 
        /// <devdoc> 
        /// Shadow property to raise the SchemaRefreshed event.
        /// </devdoc> 
        public string Data {
            get {
                return XmlDataSource.Data;
            } 
            set {
                if (value != XmlDataSource.Data) { 
                    XmlDataSource.Data = value; 
                    OnDataSourceChanged(EventArgs.Empty);
                    OnSchemaRefreshed(EventArgs.Empty); 
                }
            }
        }
 
        /// <devdoc>
        /// Shadow property to raise the SchemaRefreshed event. 
        /// </devdoc> 
        public string DataFile {
            get { 
                return XmlDataSource.DataFile;
            }
            set {
                if (value != XmlDataSource.DataFile) { 
                    _mappedDataFile = null;
                    XmlDataSource.DataFile = value; 
                    OnDataSourceChanged(EventArgs.Empty); 
                    OnSchemaRefreshed(EventArgs.Empty);
                } 
            }
        }

        /// <devdoc> 
        /// Shadow property to raise the SchemaRefreshed event.
        /// </devdoc> 
        public string Transform { 
            get {
                return XmlDataSource.Transform; 
            }
            set {
                if (value != XmlDataSource.Transform) {
                    XmlDataSource.Transform = value; 
                    OnDataSourceChanged(EventArgs.Empty);
                    OnSchemaRefreshed(EventArgs.Empty); 
                } 
            }
        } 

        /// <devdoc>
        /// Shadow property to raise the SchemaRefreshed event.
        /// </devdoc> 
        public string TransformFile {
            get { 
                return XmlDataSource.TransformFile; 
            }
            set { 
                if (value != XmlDataSource.TransformFile) {
                    _mappedTransformFile = null;
                    XmlDataSource.TransformFile = value;
                    OnDataSourceChanged(EventArgs.Empty); 
                    OnSchemaRefreshed(EventArgs.Empty);
                } 
            } 
        }
 
        /// <include file='doc\XmlDataSourceDesigner.uex' path='docs/doc[@for="XmlDataSourceDesigner.ConfigureVisible"]/*' />
        /// <devdoc>
        /// The XmlDataSource associated with this designer.
        /// </devdoc> 
        private XmlDataSource XmlDataSource {
            get { 
                return _xmlDataSource; 
            }
        } 

        /// <devdoc>
        /// Shadow property to raise the SchemaRefreshed event.
        /// </devdoc> 
        public string XPath {
            get { 
                return XmlDataSource.XPath; 
            }
            set { 
                if (value != XmlDataSource.XPath) {
                    XmlDataSource.XPath = value;
                    OnDataSourceChanged(EventArgs.Empty);
                    OnSchemaRefreshed(EventArgs.Empty); 
                }
            } 
        } 

 
        /// <include file='doc\XmlDataSourceDesigner.uex' path='docs/doc[@for="XmlDataSourceDesigner.Configure"]/*' />
        /// <devdoc>
        /// Handles the Configure DataSource designer verb event.
        /// </devdoc> 
        public override void Configure() {
            InvokeTransactedChange(Component, new TransactedChangeCallback(ConfigureDataSourceChangeCallback), null, SR.GetString(SR.DataSource_ConfigureTransactionDescription)); 
        } 

        /// <devdoc> 
        /// Transacted change callback to invoke the Configure DataSource dialog.
        /// </devdoc>
        private bool ConfigureDataSourceChangeCallback(object context) {
            try { 
                SuppressDataSourceEvents();
                IServiceProvider site = Component.Site; 
                XmlDataSourceConfigureDataSourceForm form = new XmlDataSourceConfigureDataSourceForm(site, XmlDataSource); 
                DialogResult result = UIServiceHelper.ShowDialog(site, form);
                return (result == DialogResult.OK); 
            }
            finally {
                ResumeDataSourceEvents();
            } 
        }
 
        internal XmlDataSource GetDesignTimeXmlDataSource(string viewPath) { 
            XmlDataSource designTimeXmlDataSource = new XmlDataSource();
 
            designTimeXmlDataSource.EnableCaching = false;

            designTimeXmlDataSource.Data = XmlDataSource.Data;
            designTimeXmlDataSource.Transform = XmlDataSource.Transform; 
            designTimeXmlDataSource.XPath = (String.IsNullOrEmpty(viewPath) ? XmlDataSource.XPath : viewPath);
 
            // Change the DataFile's path from a relative path to a physical path if necessary 
            if (XmlDataSource.DataFile.Length > 0) {
                if (_mappedDataFile == null) { 
                    _mappedDataFile = UrlPath.MapPath(Component.Site, XmlDataSource.DataFile);
                }
                designTimeXmlDataSource.DataFile = _mappedDataFile;
 
                // Check if the file exists
                if (!File.Exists(designTimeXmlDataSource.DataFile)) { 
                    return null; 
                }
            } 
            else {
                if (designTimeXmlDataSource.Data.Length == 0) {
                    // If neither a DataFile, nor inline data are specified, we can't load data using the runtime control
                    return null; 
                }
            } 
 
            // Change the TransformFile's path from a relative path to a physical path if necessary
            if (XmlDataSource.TransformFile.Length > 0) { 
                if (_mappedTransformFile == null) {
                    _mappedTransformFile = UrlPath.MapPath(Component.Site, XmlDataSource.TransformFile);
                }
                designTimeXmlDataSource.TransformFile = _mappedTransformFile; 

                // Check if the file exists 
                if (!File.Exists(designTimeXmlDataSource.TransformFile)) { 
                    return null;
                } 
            }

            return designTimeXmlDataSource;
        } 

        /// <devdoc> 
        /// Uses the runtime control to get XML data representing the hierarchical data specified in the path parameter. 
        /// </devdoc>
        internal IHierarchicalEnumerable GetHierarchicalRuntimeEnumerable(string path) { 
            XmlDataSource xmlDataSource = GetDesignTimeXmlDataSource(String.Empty);
            if (xmlDataSource == null) {
                return null;
            } 
            HierarchicalDataSourceView view = ((IHierarchicalDataSource)xmlDataSource).GetHierarchicalView(path);
            if (view == null) { 
                return null; 
            }
            return view.Select(); 
        }

        /// <devdoc>
        /// Uses the runtime control to get XML data representing the list specified in the listName parameter. 
        /// If no rows are returned, this method will return null.
        /// </devdoc> 
        internal IEnumerable GetRuntimeEnumerable(string listName) { 
            XmlDataSource xmlDataSource = GetDesignTimeXmlDataSource(String.Empty);
            if (xmlDataSource == null) { 
                return null;
            }
            XmlDataSourceView view = (XmlDataSourceView)((IDataSource)xmlDataSource).GetView(listName);
            if (view == null) { 
                return null;
            } 
 
            // Get rows from data source
            IEnumerable rows = view.Select(DataSourceSelectArguments.Empty); 

            // Try to determine if there is any data
            ICollection rowCollection = rows as ICollection;
            if ((rowCollection != null) && (rowCollection.Count == 0)) { 
                // Definitely no data in the data source
                return null; 
            } 

            // Either the data source did not implement ICollection (should 
            // never be the case here), or there was data in the data source.
            return rows;
        }
 
        public override DesignerHierarchicalDataSourceView GetView(string viewPath) {
            return new XmlDesignerHierarchicalDataSourceView(this, viewPath); 
        } 

        /// <include file='doc\XmlDataSourceDesigner.uex' path='docs/doc[@for="XmlDataSourceDesigner.Initialize"]/*' /> 
        /// <devdoc>
        /// Initializes the designer using the specified component.
        /// </devdoc>
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(XmlDataSource));
            base.Initialize(component); 
 
            _xmlDataSource = (XmlDataSource)component;
        } 

        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties);
 
            // Add DataMember type converter
            PropertyDescriptor property; 
 
            foreach (string propName in _shadowProperties) {
                property = (PropertyDescriptor)properties[propName]; 
                Debug.Assert(property != null, "Expected a property named '" + propName + "'");
                properties[propName] = TypeDescriptor.CreateProperty(GetType(), property);
            }
        } 

        public override void RefreshSchema(bool preferSilent) { 
            try { 
                SuppressDataSourceEvents();
 
                Debug.Assert(CanRefreshSchema, "CanRefreshSchema is false - RefreshSchema should not be called");

                // Since there is no way to actually know when the schema has changed,
                // we always raise the SchemaRefreshed event. The problem is that we 
                // can't retain an "old" copy of the schema since the schema is dependent
                // on the view that was using it (in terms of what XPath was used to create 
                // it). Since it is not practical to store every combination of XPath/Schema 
                // we go the simple route and don't store it at all.
                OnDataSourceChanged(EventArgs.Empty); 
                OnSchemaRefreshed(EventArgs.Empty);
            }
            finally {
                ResumeDataSourceEvents(); 
            }
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
            if (!String.IsNullOrEmpty(viewName)) { 
                return null;
            }
            if (_view == null) {
                _view = new XmlDesignerDataSourceView(this, String.Empty); 
            }
            return _view; 
        } 

        string[] IDataSourceDesigner.GetViewNames() { 
            return new string[0];
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
