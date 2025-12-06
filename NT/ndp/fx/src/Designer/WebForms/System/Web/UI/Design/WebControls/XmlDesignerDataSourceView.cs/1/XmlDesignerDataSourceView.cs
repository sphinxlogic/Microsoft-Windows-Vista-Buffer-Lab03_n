//------------------------------------------------------------------------------ 
// <copyright file="XmlDesignerDataSourceView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Diagnostics;
    using System.Web.UI.WebControls;

    /// <devdoc> 
    /// XmlDesignerDataSourceView is the designer view associated with a XmlDataSourceDesigner.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class XmlDesignerDataSourceView : DesignerDataSourceView {
        private XmlDataSourceDesigner _owner; 

        public XmlDesignerDataSourceView(XmlDataSourceDesigner owner, string viewName) : base(owner, viewName) {
            _owner = owner;
        } 

        public override IDataSourceViewSchema Schema { 
            get { 
                XmlDataSource xmlDataSource = _owner.GetDesignTimeXmlDataSource(String.Empty);
                if (xmlDataSource == null) { 
                    return null;
                }
                string xPath = xmlDataSource.XPath;
                if (xPath.Length == 0) { 
                    xPath = "/node()/node()";
                } 
                IDataSourceSchema schema = new XmlDocumentSchema(xmlDataSource.GetXmlDocument(), xPath); 
                if (schema != null) {
                    IDataSourceViewSchema[] viewSchemas = schema.GetViews(); 
                    if ((viewSchemas != null) && (viewSchemas.Length > 0)) {
                        return viewSchemas[0];
                    }
                } 
                return null;
            } 
        } 

        public override IEnumerable GetDesignTimeData(int minimumRows, out bool isSampleData) { 
            // First try to use the runtime control to load actual data
            IEnumerable runtimeData = _owner.GetRuntimeEnumerable(Name);
            if (runtimeData != null) {
                // Runtime data was loaded, return it 
                isSampleData = false;
                return runtimeData; 
            } 

            // No design time data could be retrieved, show dummy data 
            return base.GetDesignTimeData(minimumRows, out isSampleData);
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="XmlDesignerDataSourceView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Diagnostics;
    using System.Web.UI.WebControls;

    /// <devdoc> 
    /// XmlDesignerDataSourceView is the designer view associated with a XmlDataSourceDesigner.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class XmlDesignerDataSourceView : DesignerDataSourceView {
        private XmlDataSourceDesigner _owner; 

        public XmlDesignerDataSourceView(XmlDataSourceDesigner owner, string viewName) : base(owner, viewName) {
            _owner = owner;
        } 

        public override IDataSourceViewSchema Schema { 
            get { 
                XmlDataSource xmlDataSource = _owner.GetDesignTimeXmlDataSource(String.Empty);
                if (xmlDataSource == null) { 
                    return null;
                }
                string xPath = xmlDataSource.XPath;
                if (xPath.Length == 0) { 
                    xPath = "/node()/node()";
                } 
                IDataSourceSchema schema = new XmlDocumentSchema(xmlDataSource.GetXmlDocument(), xPath); 
                if (schema != null) {
                    IDataSourceViewSchema[] viewSchemas = schema.GetViews(); 
                    if ((viewSchemas != null) && (viewSchemas.Length > 0)) {
                        return viewSchemas[0];
                    }
                } 
                return null;
            } 
        } 

        public override IEnumerable GetDesignTimeData(int minimumRows, out bool isSampleData) { 
            // First try to use the runtime control to load actual data
            IEnumerable runtimeData = _owner.GetRuntimeEnumerable(Name);
            if (runtimeData != null) {
                // Runtime data was loaded, return it 
                isSampleData = false;
                return runtimeData; 
            } 

            // No design time data could be retrieved, show dummy data 
            return base.GetDesignTimeData(minimumRows, out isSampleData);
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
