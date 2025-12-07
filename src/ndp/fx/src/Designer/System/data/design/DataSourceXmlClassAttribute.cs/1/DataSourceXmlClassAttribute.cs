 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Design {
    using System; 
    using System.ComponentModel;

    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class DataSourceXmlClassAttribute : Attribute { 
        private string  name;
 
        internal DataSourceXmlClassAttribute(string elementName) { 
            this.name = elementName;
        } 

        public string Name {
            get {
                return name; 
            }
            set { 
                name = value; 
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

    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class DataSourceXmlClassAttribute : Attribute { 
        private string  name;
 
        internal DataSourceXmlClassAttribute(string elementName) { 
            this.name = elementName;
        } 

        public string Name {
            get {
                return name; 
            }
            set { 
                name = value; 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
