 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 

namespace System.Data.Design { 
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Collections; 
    using System.Diagnostics;
    using System.Globalization; 
 

 


    internal class ConnectionString {
        private string providerName;      // Invariant name of the ADO.NET provider 
        private string connectionString;
 
 
        public ConnectionString( string providerName, string connectionString ) {
            this.connectionString = connectionString; 
            this.providerName = providerName;
        }

 		//public string ProviderName { 
		//    get {
		//        return this.providerName; 
		//    } 
 		//}
 
        public string ToFullString() {
            return this.connectionString.ToString();
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
    using System.Data;
    using System.Data.Common;
    using System.Collections; 
    using System.Diagnostics;
    using System.Globalization; 
 

 


    internal class ConnectionString {
        private string providerName;      // Invariant name of the ADO.NET provider 
        private string connectionString;
 
 
        public ConnectionString( string providerName, string connectionString ) {
            this.connectionString = connectionString; 
            this.providerName = providerName;
        }

 		//public string ProviderName { 
		//    get {
		//        return this.providerName; 
		//    } 
 		//}
 
        public string ToFullString() {
            return this.connectionString.ToString();
        }
 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
