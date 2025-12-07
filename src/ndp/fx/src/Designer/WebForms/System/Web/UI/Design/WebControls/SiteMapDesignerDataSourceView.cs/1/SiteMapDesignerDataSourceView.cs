//------------------------------------------------------------------------------ 
// <copyright file="SiteMapDesignerDataSourceView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Data;
    using System.Web.UI.WebControls;

    /// <devdoc> 
    /// SiteMapDesignerDataSourceView is the designer view associated with a SiteMapDataSourceDesigner.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class SiteMapDesignerDataSourceView : DesignerDataSourceView {
 
        private static readonly SiteMapDataSourceDesigner.SiteMapDataSourceViewSchema _siteMapViewSchema = new SiteMapDataSourceDesigner.SiteMapDataSourceViewSchema();

        private SiteMapDataSourceDesigner _owner;
        private SiteMapDataSource _siteMapDataSource; 

        public SiteMapDesignerDataSourceView(SiteMapDataSourceDesigner owner, string viewName) : base(owner, viewName) { 
            _owner = owner; 
            _siteMapDataSource = (SiteMapDataSource)_owner.Component;
        } 


        public override IDataSourceViewSchema Schema {
            get { 
                return _siteMapViewSchema;
            } 
        } 

        public override IEnumerable GetDesignTimeData(int minimumRows, out bool isSampleData) { 
            string oldProvider = null;
            string oldStartingNodeUrl = null;

            SiteMapNodeCollection data = null; 

            oldProvider = _siteMapDataSource.SiteMapProvider; 
            oldStartingNodeUrl = _siteMapDataSource.StartingNodeUrl; 

            _siteMapDataSource.Provider = _owner.DesignTimeSiteMapProvider; 

            try {
                _siteMapDataSource.StartingNodeUrl = null;
                data = ((SiteMapDataSourceView)((IDataSource)_siteMapDataSource).GetView(Name)).Select(DataSourceSelectArguments.Empty) as SiteMapNodeCollection; 
                isSampleData = false;
            } 
            finally { 
                _siteMapDataSource.StartingNodeUrl = oldStartingNodeUrl;
                _siteMapDataSource.SiteMapProvider = oldProvider; 
            }

            if ((data != null) && (data.Count == 0)) {
                // No design time data could be retrieved, show dummy data 
                isSampleData = true;
                return DesignTimeData.GetDesignTimeDataSource(DesignTimeData.CreateDummyDataBoundDataTable(), minimumRows); 
            } 
            return data;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SiteMapDesignerDataSourceView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Data;
    using System.Web.UI.WebControls;

    /// <devdoc> 
    /// SiteMapDesignerDataSourceView is the designer view associated with a SiteMapDataSourceDesigner.
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class SiteMapDesignerDataSourceView : DesignerDataSourceView {
 
        private static readonly SiteMapDataSourceDesigner.SiteMapDataSourceViewSchema _siteMapViewSchema = new SiteMapDataSourceDesigner.SiteMapDataSourceViewSchema();

        private SiteMapDataSourceDesigner _owner;
        private SiteMapDataSource _siteMapDataSource; 

        public SiteMapDesignerDataSourceView(SiteMapDataSourceDesigner owner, string viewName) : base(owner, viewName) { 
            _owner = owner; 
            _siteMapDataSource = (SiteMapDataSource)_owner.Component;
        } 


        public override IDataSourceViewSchema Schema {
            get { 
                return _siteMapViewSchema;
            } 
        } 

        public override IEnumerable GetDesignTimeData(int minimumRows, out bool isSampleData) { 
            string oldProvider = null;
            string oldStartingNodeUrl = null;

            SiteMapNodeCollection data = null; 

            oldProvider = _siteMapDataSource.SiteMapProvider; 
            oldStartingNodeUrl = _siteMapDataSource.StartingNodeUrl; 

            _siteMapDataSource.Provider = _owner.DesignTimeSiteMapProvider; 

            try {
                _siteMapDataSource.StartingNodeUrl = null;
                data = ((SiteMapDataSourceView)((IDataSource)_siteMapDataSource).GetView(Name)).Select(DataSourceSelectArguments.Empty) as SiteMapNodeCollection; 
                isSampleData = false;
            } 
            finally { 
                _siteMapDataSource.StartingNodeUrl = oldStartingNodeUrl;
                _siteMapDataSource.SiteMapProvider = oldProvider; 
            }

            if ((data != null) && (data.Count == 0)) {
                // No design time data could be retrieved, show dummy data 
                isSampleData = true;
                return DesignTimeData.GetDesignTimeDataSource(DesignTimeData.CreateDummyDataBoundDataTable(), minimumRows); 
            } 
            return data;
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
