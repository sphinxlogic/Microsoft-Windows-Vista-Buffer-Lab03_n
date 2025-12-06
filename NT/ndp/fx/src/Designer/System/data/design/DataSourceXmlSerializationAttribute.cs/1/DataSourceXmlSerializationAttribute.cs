 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Design {
    using System; 
    using System.ComponentModel;

    internal abstract class DataSourceXmlSerializationAttribute : Attribute {
        private bool    specialWay; 
        private Type    itemType;
        private string  name; 
 
        internal DataSourceXmlSerializationAttribute() {
            this.specialWay = false; 
        }

        public Type ItemType {
            get { 
                return itemType;
            } 
            set { 
                itemType = value;
            } 
        }

        public string Name {
            get { 
                return name;
            } 
            set { 
                name = value;
            } 
        }

        public bool SpecialWay {
            get { 
                return specialWay;
            } 
            set { 
                specialWay = value;
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

    internal abstract class DataSourceXmlSerializationAttribute : Attribute {
        private bool    specialWay; 
        private Type    itemType;
        private string  name; 
 
        internal DataSourceXmlSerializationAttribute() {
            this.specialWay = false; 
        }

        public Type ItemType {
            get { 
                return itemType;
            } 
            set { 
                itemType = value;
            } 
        }

        public string Name {
            get { 
                return name;
            } 
            set { 
                name = value;
            } 
        }

        public bool SpecialWay {
            get { 
                return specialWay;
            } 
            set { 
                specialWay = value;
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
