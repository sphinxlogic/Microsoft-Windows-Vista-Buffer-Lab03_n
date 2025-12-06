//------------------------------------------------------------------------------ 
// <copyright file="TraceContextRecord.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web { 
    using System.Security.Permissions; 

 
    /// <devdoc>
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class TraceContextRecord { 
        private string _category;
        private string _message; 
        private Exception _errorInfo; 
        private bool _isWarning;
 

        public TraceContextRecord(string category, string msg, bool isWarning, Exception errorInfo) {
            _category = category;
            _message = msg; 
            _isWarning = isWarning;
            _errorInfo = errorInfo; 
        } 

 
        public string Category {
            get { return _category; }
        }
 

        public Exception ErrorInfo { 
            get { return _errorInfo; } 
        }
 

        public string Message {
            get { return _message; }
        } 

 
        public bool IsWarning { 
            get { return _isWarning; }
        } 
    }
}

 

//------------------------------------------------------------------------------ 
// <copyright file="TraceContextRecord.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web { 
    using System.Security.Permissions; 

 
    /// <devdoc>
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class TraceContextRecord { 
        private string _category;
        private string _message; 
        private Exception _errorInfo; 
        private bool _isWarning;
 

        public TraceContextRecord(string category, string msg, bool isWarning, Exception errorInfo) {
            _category = category;
            _message = msg; 
            _isWarning = isWarning;
            _errorInfo = errorInfo; 
        } 

 
        public string Category {
            get { return _category; }
        }
 

        public Exception ErrorInfo { 
            get { return _errorInfo; } 
        }
 

        public string Message {
            get { return _message; }
        } 

 
        public bool IsWarning { 
            get { return _isWarning; }
        } 
    }
}

 

