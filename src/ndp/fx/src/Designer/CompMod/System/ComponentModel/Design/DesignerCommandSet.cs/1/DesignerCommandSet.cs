//------------------------------------------------------------------------------ 
// <copyright file="DesginerCommandSet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;

    /// <include file='doc\DesginerCommandSet.uex' path='docs/doc[@for="DesginerCommandSet"]/*' />
    /// <devdoc> 
    ///     [to be provided]
    /// </devdoc> 
    public class DesignerCommandSet { 

        /// <include file='doc\DesginerCommandSet.uex' path='docs/doc[@for="GetCommands"]/*' /> 
        /// <devdoc>
        ///     [to be provided]
        /// </devdoc>
        public virtual ICollection GetCommands(string name) { 
            return null;
        } 
 
/*
        public DesignerActionList CreateVerbsActionList() { 
            DesignerActionList result = null;
            DesignerVerbCollection verbs = Verbs;
            if(verbs != null) {
                DesignerVerb[] verbsArray = new DesignerVerb[verbs.Count]; 
                verbs.CopyTo(verbsArray, 0);
                result = new DesignerActionVerbList(verbsArray); 
            } 
            return result;
        } 
        */

        /// <include file='doc\DesginerCommandSet.uex' path='docs/doc[@for="Verbs"]/*' />
        /// <devdoc> 
        ///     [to be provided]
        /// </devdoc> 
        public DesignerVerbCollection Verbs { 
            get {
                return (DesignerVerbCollection)GetCommands("Verbs"); 
            }
        }

        /// <include file='doc\DesginerCommandSet.uex' path='docs/doc[@for="Actions"]/*' /> 
        /// <devdoc>
        ///     [to be provided] 
        /// </devdoc> 
        public DesignerActionListCollection ActionLists {
            get { 
                return (DesignerActionListCollection)GetCommands("ActionLists");
            }
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesginerCommandSet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;

    /// <include file='doc\DesginerCommandSet.uex' path='docs/doc[@for="DesginerCommandSet"]/*' />
    /// <devdoc> 
    ///     [to be provided]
    /// </devdoc> 
    public class DesignerCommandSet { 

        /// <include file='doc\DesginerCommandSet.uex' path='docs/doc[@for="GetCommands"]/*' /> 
        /// <devdoc>
        ///     [to be provided]
        /// </devdoc>
        public virtual ICollection GetCommands(string name) { 
            return null;
        } 
 
/*
        public DesignerActionList CreateVerbsActionList() { 
            DesignerActionList result = null;
            DesignerVerbCollection verbs = Verbs;
            if(verbs != null) {
                DesignerVerb[] verbsArray = new DesignerVerb[verbs.Count]; 
                verbs.CopyTo(verbsArray, 0);
                result = new DesignerActionVerbList(verbsArray); 
            } 
            return result;
        } 
        */

        /// <include file='doc\DesginerCommandSet.uex' path='docs/doc[@for="Verbs"]/*' />
        /// <devdoc> 
        ///     [to be provided]
        /// </devdoc> 
        public DesignerVerbCollection Verbs { 
            get {
                return (DesignerVerbCollection)GetCommands("Verbs"); 
            }
        }

        /// <include file='doc\DesginerCommandSet.uex' path='docs/doc[@for="Actions"]/*' /> 
        /// <devdoc>
        ///     [to be provided] 
        /// </devdoc> 
        public DesignerActionListCollection ActionLists {
            get { 
                return (DesignerActionListCollection)GetCommands("ActionLists");
            }
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
