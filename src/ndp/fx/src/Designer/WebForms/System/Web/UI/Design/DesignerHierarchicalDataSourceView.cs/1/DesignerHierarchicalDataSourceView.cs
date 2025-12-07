//------------------------------------------------------------------------------ 
// <copyright file="DesignerHierarchicalDataSourceView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;

    public abstract class DesignerHierarchicalDataSourceView { 

        private string _path; 
        private IHierarchicalDataSourceDesigner _owner; 

        protected DesignerHierarchicalDataSourceView(IHierarchicalDataSourceDesigner owner, string viewPath) { 
            if (owner == null) {
                throw new ArgumentNullException("owner");
            }
            if (viewPath == null) { 
                throw new ArgumentNullException("viewPath");
            } 
 
            _owner = owner;
            _path = viewPath; 
        }


        public IHierarchicalDataSourceDesigner DataSourceDesigner { 
            get {
                return _owner; 
            } 
        }
 
        public string Path {
            get {
                return _path;
            } 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public virtual IDataSourceSchema Schema { 
            get {
                return null;
            }
        } 

 
        /// <devdoc> 
        /// Provides a design-time version of the hierarchical data source. This method
        /// will attempt to create sample data that matches the schema of the data source, 
        /// though it might not necessarily match.
        /// </devdoc>
        public virtual IHierarchicalEnumerable GetDesignTimeData(out bool isSampleData) {
            isSampleData = true; 
            //
 
            return null; 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerHierarchicalDataSourceView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;

    public abstract class DesignerHierarchicalDataSourceView { 

        private string _path; 
        private IHierarchicalDataSourceDesigner _owner; 

        protected DesignerHierarchicalDataSourceView(IHierarchicalDataSourceDesigner owner, string viewPath) { 
            if (owner == null) {
                throw new ArgumentNullException("owner");
            }
            if (viewPath == null) { 
                throw new ArgumentNullException("viewPath");
            } 
 
            _owner = owner;
            _path = viewPath; 
        }


        public IHierarchicalDataSourceDesigner DataSourceDesigner { 
            get {
                return _owner; 
            } 
        }
 
        public string Path {
            get {
                return _path;
            } 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public virtual IDataSourceSchema Schema { 
            get {
                return null;
            }
        } 

 
        /// <devdoc> 
        /// Provides a design-time version of the hierarchical data source. This method
        /// will attempt to create sample data that matches the schema of the data source, 
        /// though it might not necessarily match.
        /// </devdoc>
        public virtual IHierarchicalEnumerable GetDesignTimeData(out bool isSampleData) {
            isSampleData = true; 
            //
 
            return null; 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
