//------------------------------------------------------------------------------ 
// <copyright file="ExpressionEditorSheet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.ComponentModel;
 
    /// <include file='doc\ExpressionEditorSheet.uex' path='docs/doc[@for="ExpressionEditorSheet"]/*' />
    public abstract class ExpressionEditorSheet {
        private IServiceProvider _serviceProvider;
 
        protected ExpressionEditorSheet(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider; 
        } 

        [Browsable(false)] 
        public virtual bool IsValid {
            get {
                return true;
            } 
        }
 
        [Browsable(false)] 
        public IServiceProvider ServiceProvider {
            get { 
                return _serviceProvider;
            }
        }
 
        /// <include file='doc\ExpressionEditorSheet.uex' path='docs/doc[@for="ExpressionEditorSheet.GetExpression"]/*' />
        /// <devdov> 
        /// Gets the expression constructed from this expression editor sheet 
        /// </devdoc>
        public abstract string GetExpression(); 
    }

}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ExpressionEditorSheet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.ComponentModel;
 
    /// <include file='doc\ExpressionEditorSheet.uex' path='docs/doc[@for="ExpressionEditorSheet"]/*' />
    public abstract class ExpressionEditorSheet {
        private IServiceProvider _serviceProvider;
 
        protected ExpressionEditorSheet(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider; 
        } 

        [Browsable(false)] 
        public virtual bool IsValid {
            get {
                return true;
            } 
        }
 
        [Browsable(false)] 
        public IServiceProvider ServiceProvider {
            get { 
                return _serviceProvider;
            }
        }
 
        /// <include file='doc\ExpressionEditorSheet.uex' path='docs/doc[@for="ExpressionEditorSheet.GetExpression"]/*' />
        /// <devdov> 
        /// Gets the expression constructed from this expression editor sheet 
        /// </devdoc>
        public abstract string GetExpression(); 
    }

}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
