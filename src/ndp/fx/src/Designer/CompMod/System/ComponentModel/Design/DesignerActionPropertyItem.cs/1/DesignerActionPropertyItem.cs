//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionPropertyItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.ComponentModel; 

    /// <include file='doc\DesignerActionPropertyItem.uex' path='docs/doc[@for="DesignerActionPropertyItem"]/*' />
    /// <devdoc>
    ///     [to be provided] 
    /// </devdoc>
    public sealed class DesignerActionPropertyItem    : DesignerActionItem { 
 
        private string              memberName;
        private IComponent          relatedComponent; 


        /// <include file='doc\DesignerActionPropertyItem.uex' path='docs/doc[@for="DesignerActionPropertyItem.DesignerActionPropertyItem"]/*' />
        /// <devdoc> 
        ///     [to be provvided]
        /// </devdoc> 
        public DesignerActionPropertyItem(string memberName, string displayName, string category, string description) 
            : base( displayName, category, description)
        { 
            this.memberName = memberName;
        }

        /// <include file='doc\DesignerActionPropertyItem.uex' path='docs/doc[@for="DesignerActionPropertyItem.DesignerActionPropertyItem2"]/*' /> 
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc> 
        public DesignerActionPropertyItem(string memberName, string displayName)
            : this(memberName, displayName, null, null) { 
        }

        /// <include file='doc\DesignerActionPropertyItem.uex' path='docs/doc[@for="DesignerActionPropertyItem.DesignerActionPropertyItem3"]/*' />
        /// <devdoc> 
        ///     [to be provvided]
        /// </devdoc> 
        public DesignerActionPropertyItem(string memberName, string displayName, string category) 
            : this(memberName, displayName, category, null) {
        } 

        /// <include file='doc\DesignerActionPropertyItem.uex' path='docs/doc[@for="DesignerActionPropertyItem.MemberName"]/*' />
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc>
        public string MemberName { 
            get { 
                return memberName;
            } 
        }

        /// <include file='doc\DesignerActionPropertyItem.uex' path='docs/doc[@for="DesignerActionPropertyItem.RelatedComponent"]/*' />
        /// <devdoc> 
        ///     [to be provvided]
        /// </devdoc> 
        public IComponent RelatedComponent { 
            get {
                return relatedComponent; 
            }
            set {
                relatedComponent = value;
            } 
        }
 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionPropertyItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.ComponentModel; 

    /// <include file='doc\DesignerActionPropertyItem.uex' path='docs/doc[@for="DesignerActionPropertyItem"]/*' />
    /// <devdoc>
    ///     [to be provided] 
    /// </devdoc>
    public sealed class DesignerActionPropertyItem    : DesignerActionItem { 
 
        private string              memberName;
        private IComponent          relatedComponent; 


        /// <include file='doc\DesignerActionPropertyItem.uex' path='docs/doc[@for="DesignerActionPropertyItem.DesignerActionPropertyItem"]/*' />
        /// <devdoc> 
        ///     [to be provvided]
        /// </devdoc> 
        public DesignerActionPropertyItem(string memberName, string displayName, string category, string description) 
            : base( displayName, category, description)
        { 
            this.memberName = memberName;
        }

        /// <include file='doc\DesignerActionPropertyItem.uex' path='docs/doc[@for="DesignerActionPropertyItem.DesignerActionPropertyItem2"]/*' /> 
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc> 
        public DesignerActionPropertyItem(string memberName, string displayName)
            : this(memberName, displayName, null, null) { 
        }

        /// <include file='doc\DesignerActionPropertyItem.uex' path='docs/doc[@for="DesignerActionPropertyItem.DesignerActionPropertyItem3"]/*' />
        /// <devdoc> 
        ///     [to be provvided]
        /// </devdoc> 
        public DesignerActionPropertyItem(string memberName, string displayName, string category) 
            : this(memberName, displayName, category, null) {
        } 

        /// <include file='doc\DesignerActionPropertyItem.uex' path='docs/doc[@for="DesignerActionPropertyItem.MemberName"]/*' />
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc>
        public string MemberName { 
            get { 
                return memberName;
            } 
        }

        /// <include file='doc\DesignerActionPropertyItem.uex' path='docs/doc[@for="DesignerActionPropertyItem.RelatedComponent"]/*' />
        /// <devdoc> 
        ///     [to be provvided]
        /// </devdoc> 
        public IComponent RelatedComponent { 
            get {
                return relatedComponent; 
            }
            set {
                relatedComponent = value;
            } 
        }
 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
