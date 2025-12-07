//------------------------------------------------------------------------------ 
// <copyright file="DBProviderSupportedClasses.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Common {
    using System; 

    [Serializable]
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
#if WINFSInternalOnly 
    internal
#else 
    public 
#endif
    sealed class DbProviderSpecificTypePropertyAttribute : System.Attribute { 

        private bool _isProviderSpecificTypeProperty;

        public DbProviderSpecificTypePropertyAttribute(bool isProviderSpecificTypeProperty) { 
            _isProviderSpecificTypeProperty = isProviderSpecificTypeProperty;
        } 
 
        public bool IsProviderSpecificTypeProperty {
            get { 
                return _isProviderSpecificTypeProperty;
            }
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DBProviderSupportedClasses.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Common {
    using System; 

    [Serializable]
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
#if WINFSInternalOnly 
    internal
#else 
    public 
#endif
    sealed class DbProviderSpecificTypePropertyAttribute : System.Attribute { 

        private bool _isProviderSpecificTypeProperty;

        public DbProviderSpecificTypePropertyAttribute(bool isProviderSpecificTypeProperty) { 
            _isProviderSpecificTypeProperty = isProviderSpecificTypeProperty;
        } 
 
        public bool IsProviderSpecificTypeProperty {
            get { 
                return _isProviderSpecificTypeProperty;
            }
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
