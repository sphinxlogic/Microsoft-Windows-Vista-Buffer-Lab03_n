//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Text.RegularExpressions;
 
    /// <include file='doc\DesignerTask.uex' path='docs/doc[@for="DesignerTask"]/*' />
    /// <devdoc> 
    ///     A menu command that defines text and other metadata to describe a targeted task that can be performed. 
    //      Tasks typically walk the user through some multi-step process, such as configuring a data source for a component.
    //      Designer tasks are shown in a custom piece of UI (Chrome). 
    /// <include file='doc\DesignerActionItem.uex' path='docs/doc[@for="DesignerActionItem"]/*' />
    /// </devdoc>
    public abstract class DesignerActionItem {
 
        private bool                            allowAssociate;
        private string                          displayName; 
        private string                          description; 
        private string                          category;
        private IDictionary                     properties; 


        /// <include file='doc\DesignerActionItem.uex' path='docs/doc[@for="DesignerActionItem.DesignerActionItem"]/*' />
        /// <devdoc> 
        ///     [to be provvided]
        /// </devdoc> 
        public DesignerActionItem(string displayName, string category, string description) { 
            this.category = category;
            this.description = description; 
            this.displayName = displayName == null ? null : Regex.Replace(displayName, @"\(\&.\)", ""); // VSWHIDBEY 485835
        }

        internal DesignerActionItem() { 
        }
 
        /// <include file='doc\DesignerActionItem.uex' path='docs/doc[@for="DesignerActionItem.AllowAssociate"]/*' /> 
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc>
        public bool AllowAssociate {
            get {
                return allowAssociate; 
            }
            set { 
                allowAssociate = value; 
            }
        } 

        /// <include file='doc\DesignerActionItem.uex' path='docs/doc[@for="DesignerActionItem.Category"]/*' />
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc>
        public virtual string Category { 
            get { 
                return category;
            } 
        }

        /// <include file='doc\DesignerActionItem.uex' path='docs/doc[@for="DesignerActionItem.Description"]/*' />
        /// <devdoc> 
        ///     [to be provvided]
        /// </devdoc> 
        public virtual string Description { 
            get {
                return description; 
            }
        }

        /// <include file='doc\DesignerActionItem.uex' path='docs/doc[@for="DesignerActionItem.DisplayName"]/*' /> 
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc> 
        public virtual string DisplayName {
            get { 
                return displayName;
            }
        }
 
        /// <include file='doc\DesignerActionItem.uex' path='docs/doc[@for="DesignerActionItem.Properties"]/*' />
        /// <devdoc> 
        ///     [to be provvided] 
        /// </devdoc>
        public IDictionary Properties { 
            get {
                if (properties == null) {
                    properties = new HybridDictionary();
                } 
                return properties;
            } 
        } 
    }
 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Text.RegularExpressions;
 
    /// <include file='doc\DesignerTask.uex' path='docs/doc[@for="DesignerTask"]/*' />
    /// <devdoc> 
    ///     A menu command that defines text and other metadata to describe a targeted task that can be performed. 
    //      Tasks typically walk the user through some multi-step process, such as configuring a data source for a component.
    //      Designer tasks are shown in a custom piece of UI (Chrome). 
    /// <include file='doc\DesignerActionItem.uex' path='docs/doc[@for="DesignerActionItem"]/*' />
    /// </devdoc>
    public abstract class DesignerActionItem {
 
        private bool                            allowAssociate;
        private string                          displayName; 
        private string                          description; 
        private string                          category;
        private IDictionary                     properties; 


        /// <include file='doc\DesignerActionItem.uex' path='docs/doc[@for="DesignerActionItem.DesignerActionItem"]/*' />
        /// <devdoc> 
        ///     [to be provvided]
        /// </devdoc> 
        public DesignerActionItem(string displayName, string category, string description) { 
            this.category = category;
            this.description = description; 
            this.displayName = displayName == null ? null : Regex.Replace(displayName, @"\(\&.\)", ""); // VSWHIDBEY 485835
        }

        internal DesignerActionItem() { 
        }
 
        /// <include file='doc\DesignerActionItem.uex' path='docs/doc[@for="DesignerActionItem.AllowAssociate"]/*' /> 
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc>
        public bool AllowAssociate {
            get {
                return allowAssociate; 
            }
            set { 
                allowAssociate = value; 
            }
        } 

        /// <include file='doc\DesignerActionItem.uex' path='docs/doc[@for="DesignerActionItem.Category"]/*' />
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc>
        public virtual string Category { 
            get { 
                return category;
            } 
        }

        /// <include file='doc\DesignerActionItem.uex' path='docs/doc[@for="DesignerActionItem.Description"]/*' />
        /// <devdoc> 
        ///     [to be provvided]
        /// </devdoc> 
        public virtual string Description { 
            get {
                return description; 
            }
        }

        /// <include file='doc\DesignerActionItem.uex' path='docs/doc[@for="DesignerActionItem.DisplayName"]/*' /> 
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc> 
        public virtual string DisplayName {
            get { 
                return displayName;
            }
        }
 
        /// <include file='doc\DesignerActionItem.uex' path='docs/doc[@for="DesignerActionItem.Properties"]/*' />
        /// <devdoc> 
        ///     [to be provvided] 
        /// </devdoc>
        public IDictionary Properties { 
            get {
                if (properties == null) {
                    properties = new HybridDictionary();
                } 
                return properties;
            } 
        } 
    }
 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
