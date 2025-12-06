//------------------------------------------------------------------------------ 
// <copyright file="PropertyCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
// <owner current="false" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

namespace System.Data { 
    using System;
    using System.Collections;
    using System.Runtime.Serialization;
 
    /// <devdoc>
    /// <para>Represents a collection of properties that can be added to <see cref='System.Data.DataColumn'/>, 
    /// <see cref='System.Data.DataSet'/>, 
    ///    or <see cref='System.Data.DataTable'/>.</para>
    /// </devdoc> 
    [Serializable]
#if WINFSInternalOnly
    internal
#else 
    public
#endif 
    class PropertyCollection : Hashtable { 
        public PropertyCollection() : base() {
        } 

        protected PropertyCollection(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    } 
    //3 NOTE: This should have been named PropertyDictionary, to avoid fxcop warnings about not having strongly typed IList and ICollection implementations, but it's too late now...
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="PropertyCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
// <owner current="false" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

namespace System.Data { 
    using System;
    using System.Collections;
    using System.Runtime.Serialization;
 
    /// <devdoc>
    /// <para>Represents a collection of properties that can be added to <see cref='System.Data.DataColumn'/>, 
    /// <see cref='System.Data.DataSet'/>, 
    ///    or <see cref='System.Data.DataTable'/>.</para>
    /// </devdoc> 
    [Serializable]
#if WINFSInternalOnly
    internal
#else 
    public
#endif 
    class PropertyCollection : Hashtable { 
        public PropertyCollection() : base() {
        } 

        protected PropertyCollection(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    } 
    //3 NOTE: This should have been named PropertyDictionary, to avoid fxcop warnings about not having strongly typed IList and ICollection implementations, but it's too late now...
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
