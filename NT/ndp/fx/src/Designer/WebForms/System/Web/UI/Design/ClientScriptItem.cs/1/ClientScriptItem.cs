//------------------------------------------------------------------------------ 
// <copyright file="ClientScriptItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    /// <devdoc>
    /// Represents a client script block in a web form document. 
    /// </devdoc>
    public sealed class ClientScriptItem {
        private string _text;
        private string _source; 
        private string _language;
        private string _type; 
        private string _id; 

        public ClientScriptItem(string text, string source, string language, string type, string id) { 
            _text = text;
            _source = source;
            _language = language;
            _type = type; 
            _id = id;
        } 
 
        public string Id {
            get { 
                return _id;
            }
        }
 
        public string Language {
            get { 
                return _language; 
            }
        } 

        public string Source {
            get {
                return _source; 
            }
        } 
 
        public string Text {
            get { 
                return _text;
            }
        }
 
        public string Type {
            get { 
                return _type; 
            }
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
﻿//------------------------------------------------------------------------------ 
// <copyright file="ClientScriptItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    /// <devdoc>
    /// Represents a client script block in a web form document. 
    /// </devdoc>
    public sealed class ClientScriptItem {
        private string _text;
        private string _source; 
        private string _language;
        private string _type; 
        private string _id; 

        public ClientScriptItem(string text, string source, string language, string type, string id) { 
            _text = text;
            _source = source;
            _language = language;
            _type = type; 
            _id = id;
        } 
 
        public string Id {
            get { 
                return _id;
            }
        }
 
        public string Language {
            get { 
                return _language; 
            }
        } 

        public string Source {
            get {
                return _source; 
            }
        } 
 
        public string Text {
            get { 
                return _text;
            }
        }
 
        public string Type {
            get { 
                return _type; 
            }
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
