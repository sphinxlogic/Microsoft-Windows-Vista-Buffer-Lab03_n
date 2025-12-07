 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design { 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Reflection;
 
    /// <summary>
 	/// This is the design time object for the so-called "DataAccessor" 
	/// in the DataSource designer delta spec. 
	/// To make the minimum code change(e.g. persistence, command routing),
	/// we will use the existing code model for DbTable and only expose the necessary 
 	/// properties in this class.
	/// </summary>
    internal class DataAccessor : DataSourceComponent {
        private DesignTable designTable; 
        internal const string DEFAULT_BASE_CLASS = "System.ComponentModel.Component";
 		internal const string DEFAULT_NAME_POSTFIX = "TableAdapter"; 
 
        /// <summary>
        /// DataAccessor is always live with a designTable 
        /// </summary>
        /// <param name="designTable"></param>
 		public DataAccessor(DesignTable designTable){
            Debug.Assert(designTable != null, "Need to pass in designTable"); 
            if (designTable == null) {
                throw new ArgumentNullException("DesignTable"); 
            } 
            this.designTable = designTable;
		} 

        internal DesignTable DesignTable {
            get {
                Debug.Assert(this.designTable != null, "Should have a DesignerTable for DataAccessor"); 
                return designTable;
            } 
        } 
 	}
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design { 
    using System;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Reflection;
 
    /// <summary>
 	/// This is the design time object for the so-called "DataAccessor" 
	/// in the DataSource designer delta spec. 
	/// To make the minimum code change(e.g. persistence, command routing),
	/// we will use the existing code model for DbTable and only expose the necessary 
 	/// properties in this class.
	/// </summary>
    internal class DataAccessor : DataSourceComponent {
        private DesignTable designTable; 
        internal const string DEFAULT_BASE_CLASS = "System.ComponentModel.Component";
 		internal const string DEFAULT_NAME_POSTFIX = "TableAdapter"; 
 
        /// <summary>
        /// DataAccessor is always live with a designTable 
        /// </summary>
        /// <param name="designTable"></param>
 		public DataAccessor(DesignTable designTable){
            Debug.Assert(designTable != null, "Need to pass in designTable"); 
            if (designTable == null) {
                throw new ArgumentNullException("DesignTable"); 
            } 
            this.designTable = designTable;
		} 

        internal DesignTable DesignTable {
            get {
                Debug.Assert(this.designTable != null, "Should have a DesignerTable for DataAccessor"); 
                return designTable;
            } 
        } 
 	}
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
