//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataSourceView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;

    public abstract class DesignerDataSourceView { 

        private string _name; 
        private IDataSourceDesigner _owner; 

        protected DesignerDataSourceView(IDataSourceDesigner owner, string viewName) { 
            if (owner == null) {
                throw new ArgumentNullException("owner");
            }
            if (viewName == null) { 
                throw new ArgumentNullException("viewName");
            } 
 
            _owner = owner;
            _name = viewName; 
        }


        // CanX properties indicate whether the data source allows each 
        // operation as it is currently configured.
        // For instance, a control may allow Deletion, but if a required Delete 
        // command isn't set, CanDelete should be false, because a Delete 
        // operation would fail.
        public virtual bool CanDelete { 
            get {
                return false;
            }
        } 

        public virtual bool CanInsert { 
            get { 
                return false;
            } 
        }

        public virtual bool CanPage {
            get { 
                return false;
            } 
        } 

        public virtual bool CanRetrieveTotalRowCount { 
            get {
                return false;
            }
        } 

        public virtual bool CanSort { 
            get { 
                return false;
            } 
        }

        public virtual bool CanUpdate {
            get { 
                return false;
            } 
        } 

        public IDataSourceDesigner DataSourceDesigner { 
            get {
                return _owner;
            }
        } 

        public string Name { 
            get { 
                return _name;
            } 
        }

        /// <summary>
        /// Provides a schema that describes the data source view represented by 
        /// the DataSourceView. This allows the designer of a data-bound control
        /// to provide intelligent choices based on the DataSourceView that is 
        /// selected for data binding. 
        /// </summary>
        /// <returns> 
        /// An object describing the view, and the properties of the objects
        /// in the list; null if this is unavailable.
        /// </returns>
        public virtual IDataSourceViewSchema Schema { 
            get {
                return null; 
            } 
        }
 

        /// <summary>
        /// Provides a design-time version of the data source view for use by the
        /// data-bound control designer. It is not expected that the designer 
        /// will perform actual data access at design-time time. The designer
        /// may create sample data instead that matches the schema of 
        /// the data source. 
        /// </summary>
        /// <param name="minimumRows"> 
        /// The minimum number of rows to be retrieved.
        /// </param>
        /// <param name="isSampleData">
        /// An output parameter indicating whether the data returned is sample 
        /// data or real data.
        /// </param> 
        /// <returns> 
        /// A sample of the data represented by the DataSourceControl; null if the
        /// designer cannot generate any sample data. 
        /// </returns>
        public virtual IEnumerable GetDesignTimeData(int minimumRows, out bool isSampleData) {
            isSampleData = true;
            // 
            return DesignTimeData.GetDesignTimeDataSource(DesignTimeData.CreateDummyDataBoundDataTable(), minimumRows);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerDataSourceView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;

    public abstract class DesignerDataSourceView { 

        private string _name; 
        private IDataSourceDesigner _owner; 

        protected DesignerDataSourceView(IDataSourceDesigner owner, string viewName) { 
            if (owner == null) {
                throw new ArgumentNullException("owner");
            }
            if (viewName == null) { 
                throw new ArgumentNullException("viewName");
            } 
 
            _owner = owner;
            _name = viewName; 
        }


        // CanX properties indicate whether the data source allows each 
        // operation as it is currently configured.
        // For instance, a control may allow Deletion, but if a required Delete 
        // command isn't set, CanDelete should be false, because a Delete 
        // operation would fail.
        public virtual bool CanDelete { 
            get {
                return false;
            }
        } 

        public virtual bool CanInsert { 
            get { 
                return false;
            } 
        }

        public virtual bool CanPage {
            get { 
                return false;
            } 
        } 

        public virtual bool CanRetrieveTotalRowCount { 
            get {
                return false;
            }
        } 

        public virtual bool CanSort { 
            get { 
                return false;
            } 
        }

        public virtual bool CanUpdate {
            get { 
                return false;
            } 
        } 

        public IDataSourceDesigner DataSourceDesigner { 
            get {
                return _owner;
            }
        } 

        public string Name { 
            get { 
                return _name;
            } 
        }

        /// <summary>
        /// Provides a schema that describes the data source view represented by 
        /// the DataSourceView. This allows the designer of a data-bound control
        /// to provide intelligent choices based on the DataSourceView that is 
        /// selected for data binding. 
        /// </summary>
        /// <returns> 
        /// An object describing the view, and the properties of the objects
        /// in the list; null if this is unavailable.
        /// </returns>
        public virtual IDataSourceViewSchema Schema { 
            get {
                return null; 
            } 
        }
 

        /// <summary>
        /// Provides a design-time version of the data source view for use by the
        /// data-bound control designer. It is not expected that the designer 
        /// will perform actual data access at design-time time. The designer
        /// may create sample data instead that matches the schema of 
        /// the data source. 
        /// </summary>
        /// <param name="minimumRows"> 
        /// The minimum number of rows to be retrieved.
        /// </param>
        /// <param name="isSampleData">
        /// An output parameter indicating whether the data returned is sample 
        /// data or real data.
        /// </param> 
        /// <returns> 
        /// A sample of the data represented by the DataSourceControl; null if the
        /// designer cannot generate any sample data. 
        /// </returns>
        public virtual IEnumerable GetDesignTimeData(int minimumRows, out bool isSampleData) {
            isSampleData = true;
            // 
            return DesignTimeData.GetDesignTimeDataSource(DesignTimeData.CreateDummyDataBoundDataTable(), minimumRows);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
