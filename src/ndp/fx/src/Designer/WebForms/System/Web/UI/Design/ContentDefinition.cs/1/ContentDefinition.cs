//------------------------------------------------------------------------------ 
// <copyright file="ContentDefinition.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
 
    /// <include file='doc\ContentDefinition.uex' path='docs/doc[@for="ContentDefinition"]/*' />
    /// <devdoc>
    /// </devdoc>
    public class ContentDefinition { 

        private string _contentPlaceHolderID; 
        private string _defaultContent; 
        private string _defaultDesignTimeHTML;
 
        public ContentDefinition(string id, string content, string designTimeHtml ) {
            _contentPlaceHolderID = id;
            _defaultContent = content;
            _defaultDesignTimeHTML = designTimeHtml; 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public string ContentPlaceHolderID { 
            get {
                return _contentPlaceHolderID;
            }
        } 

        /// <devdoc> 
        /// </devdoc> 
        public string DefaultContent {
            get { 
                return _defaultContent;
            }
        }
 
        /// <devdoc>
        /// </devdoc> 
        public string DefaultDesignTimeHtml { 
            get {
                return _defaultDesignTimeHTML; 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ContentDefinition.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
 
    /// <include file='doc\ContentDefinition.uex' path='docs/doc[@for="ContentDefinition"]/*' />
    /// <devdoc>
    /// </devdoc>
    public class ContentDefinition { 

        private string _contentPlaceHolderID; 
        private string _defaultContent; 
        private string _defaultDesignTimeHTML;
 
        public ContentDefinition(string id, string content, string designTimeHtml ) {
            _contentPlaceHolderID = id;
            _defaultContent = content;
            _defaultDesignTimeHTML = designTimeHtml; 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public string ContentPlaceHolderID { 
            get {
                return _contentPlaceHolderID;
            }
        } 

        /// <devdoc> 
        /// </devdoc> 
        public string DefaultContent {
            get { 
                return _defaultContent;
            }
        }
 
        /// <devdoc>
        /// </devdoc> 
        public string DefaultDesignTimeHtml { 
            get {
                return _defaultDesignTimeHTML; 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
