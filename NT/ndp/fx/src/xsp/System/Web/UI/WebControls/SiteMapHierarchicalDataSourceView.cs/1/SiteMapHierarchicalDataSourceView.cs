//------------------------------------------------------------------------------ 
// <copyright file="SiteMapHierarchicalDataSourceView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Web.UI.WebControls {
 
    using System.Collections; 
    using System.Security.Permissions;
    using System.Web; 
    using System.Web.UI;


    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class SiteMapHierarchicalDataSourceView : HierarchicalDataSourceView { 
 
        private SiteMapNodeCollection _collection;
 

        public SiteMapHierarchicalDataSourceView(SiteMapNode node) {
            _collection = new SiteMapNodeCollection(node);
        } 

 
        public SiteMapHierarchicalDataSourceView(SiteMapNodeCollection collection) { 
            _collection = collection;
        } 


        public override IHierarchicalEnumerable Select() {
            return _collection; 
        }
    } 
} 
//------------------------------------------------------------------------------ 
// <copyright file="SiteMapHierarchicalDataSourceView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Web.UI.WebControls {
 
    using System.Collections; 
    using System.Security.Permissions;
    using System.Web; 
    using System.Web.UI;


    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class SiteMapHierarchicalDataSourceView : HierarchicalDataSourceView { 
 
        private SiteMapNodeCollection _collection;
 

        public SiteMapHierarchicalDataSourceView(SiteMapNode node) {
            _collection = new SiteMapNodeCollection(node);
        } 

 
        public SiteMapHierarchicalDataSourceView(SiteMapNodeCollection collection) { 
            _collection = collection;
        } 


        public override IHierarchicalEnumerable Select() {
            return _collection; 
        }
    } 
} 
