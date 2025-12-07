//------------------------------------------------------------------------------ 
// <copyright file="ClientScriptItemCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Collections;
 
    /// <devdoc>
    /// Represents a collection of client script blocks in a web form document.
    /// </devdoc>
    public sealed class ClientScriptItemCollection : ReadOnlyCollectionBase { 
        public ClientScriptItemCollection(ClientScriptItem[] clientScriptItems) {
            if (clientScriptItems != null) { 
                foreach (ClientScriptItem item in clientScriptItems) { 
                    InnerList.Add(item);
                } 
            }
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
﻿//------------------------------------------------------------------------------ 
// <copyright file="ClientScriptItemCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Collections;
 
    /// <devdoc>
    /// Represents a collection of client script blocks in a web form document.
    /// </devdoc>
    public sealed class ClientScriptItemCollection : ReadOnlyCollectionBase { 
        public ClientScriptItemCollection(ClientScriptItem[] clientScriptItems) {
            if (clientScriptItems != null) { 
                foreach (ClientScriptItem item in clientScriptItems) { 
                    InnerList.Add(item);
                } 
            }
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
