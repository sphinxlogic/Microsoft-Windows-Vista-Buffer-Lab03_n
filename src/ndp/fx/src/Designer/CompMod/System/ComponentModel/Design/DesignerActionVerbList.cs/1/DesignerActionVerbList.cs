//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionVerbList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Design; 
    using System.Collections;

    /// <include file='doc\DesignerActionVerbList.uex' path='docs/doc[@for="DesignerActionVerbList"]/*' />
    /// <devdoc> 
    /// </devdoc>
    internal class DesignerActionVerbList : DesignerActionList { 
        private DesignerVerb[] _verbs; 

        /// <include file='doc\DesignerActionVerbList.uex' path='docs/doc[@for="DesignerActionVerbList.DesignerActionVerbList"]/*' /> 
        /// <devdoc>
        ///     [to be provvided]
        /// </devdoc>
        public DesignerActionVerbList(DesignerVerb[] verbs) : base(null) { 
            _verbs = verbs;
        } 
 

        /// <include file='doc\DesignerActionVerbList.uex' path='docs/doc[@for="DesignerActionVerbList.AutoShow"]/*' /> 
        /// <devdoc>
        ///     [to be provvided]
        /// </devdoc>
        public override bool AutoShow { 
            get {
                return false; 
            } 
        }
 

        /// <include file='doc\DesignerActionVerbList.uex' path='docs/doc[@for="DesignerActionVerbList.GetSortedTasks"]/*' />
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc>
        public override DesignerActionItemCollection GetSortedActionItems() { 
            DesignerActionItemCollection items = new DesignerActionItemCollection(); 
            for(int i=0;i<_verbs.Length; i++) {
                if(_verbs[i].Visible && _verbs[i].Enabled && _verbs[i].Supported) { 
                    items.Add(new DesignerActionVerbItem(_verbs[i]));
                }
            }
            return items; 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionVerbList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Design; 
    using System.Collections;

    /// <include file='doc\DesignerActionVerbList.uex' path='docs/doc[@for="DesignerActionVerbList"]/*' />
    /// <devdoc> 
    /// </devdoc>
    internal class DesignerActionVerbList : DesignerActionList { 
        private DesignerVerb[] _verbs; 

        /// <include file='doc\DesignerActionVerbList.uex' path='docs/doc[@for="DesignerActionVerbList.DesignerActionVerbList"]/*' /> 
        /// <devdoc>
        ///     [to be provvided]
        /// </devdoc>
        public DesignerActionVerbList(DesignerVerb[] verbs) : base(null) { 
            _verbs = verbs;
        } 
 

        /// <include file='doc\DesignerActionVerbList.uex' path='docs/doc[@for="DesignerActionVerbList.AutoShow"]/*' /> 
        /// <devdoc>
        ///     [to be provvided]
        /// </devdoc>
        public override bool AutoShow { 
            get {
                return false; 
            } 
        }
 

        /// <include file='doc\DesignerActionVerbList.uex' path='docs/doc[@for="DesignerActionVerbList.GetSortedTasks"]/*' />
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc>
        public override DesignerActionItemCollection GetSortedActionItems() { 
            DesignerActionItemCollection items = new DesignerActionItemCollection(); 
            for(int i=0;i<_verbs.Length; i++) {
                if(_verbs[i].Visible && _verbs[i].Enabled && _verbs[i].Supported) { 
                    items.Add(new DesignerActionVerbItem(_verbs[i]));
                }
            }
            return items; 
        }
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
