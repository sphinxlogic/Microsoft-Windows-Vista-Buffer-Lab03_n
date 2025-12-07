//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionVerbItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.ComponentModel; 
    using System.Design;
    using System.Reflection;

    /// <include file='doc\DesignerActionVerbItem.uex' path='docs/doc[@for="DesignerActionVerbItem"]/*' /> 
    /// <devdoc>
    ///     [to be provided] 
    /// </devdoc> 
    internal class DesignerActionVerbItem  : DesignerActionMethodItem {
        private DesignerVerb _targetVerb; 



        public DesignerActionVerbItem(DesignerVerb verb) { 
            if(verb == null) {
                throw (new ArgumentNullException()); 
            } 
            _targetVerb = verb;
        } 

        public override string Category {
            get {
                return "Verbs"; 
            }
        } 
 
        public override string Description {
            get { 
                return _targetVerb.Description;
            }
        }
 
        public override string DisplayName {
            get { 
                return _targetVerb.Text; 
            }
        } 

        public override string MemberName {
            get {
                return null; 
            }
        } 
 
        public override bool IncludeAsDesignerVerb{
            get { 
                return false;
            }
        }
 
        public override void Invoke() {
            _targetVerb.Invoke(); 
        } 

    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionVerbItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.ComponentModel; 
    using System.Design;
    using System.Reflection;

    /// <include file='doc\DesignerActionVerbItem.uex' path='docs/doc[@for="DesignerActionVerbItem"]/*' /> 
    /// <devdoc>
    ///     [to be provided] 
    /// </devdoc> 
    internal class DesignerActionVerbItem  : DesignerActionMethodItem {
        private DesignerVerb _targetVerb; 



        public DesignerActionVerbItem(DesignerVerb verb) { 
            if(verb == null) {
                throw (new ArgumentNullException()); 
            } 
            _targetVerb = verb;
        } 

        public override string Category {
            get {
                return "Verbs"; 
            }
        } 
 
        public override string Description {
            get { 
                return _targetVerb.Description;
            }
        }
 
        public override string DisplayName {
            get { 
                return _targetVerb.Text; 
            }
        } 

        public override string MemberName {
            get {
                return null; 
            }
        } 
 
        public override bool IncludeAsDesignerVerb{
            get { 
                return false;
            }
        }
 
        public override void Invoke() {
            _targetVerb.Invoke(); 
        } 

    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
