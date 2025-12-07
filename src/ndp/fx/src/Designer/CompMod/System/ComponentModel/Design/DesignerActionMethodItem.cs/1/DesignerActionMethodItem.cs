//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionMethodItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.ComponentModel; 
    using System.Design;
    using System.Reflection;

    /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem"]/*' /> 
    /// <devdoc>
    ///     [to be provided] 
    /// </devdoc> 
    public class DesignerActionMethodItem  : DesignerActionItem {
 
        private string                       memberName;
        private bool                         includeAsDesignerVerb;
        private DesignerActionList           actionList;
        private MethodInfo                   methodInfo; 
        private IComponent                   relatedComponent;
 
        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.DesignerActionMethodItem"]/*' /> 
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc>
        public DesignerActionMethodItem(DesignerActionList actionList, string memberName, string displayName, string category, string description, bool includeAsDesignerVerb)
            : base( displayName, category, description)
        { 
            this.actionList = actionList;
            this.memberName = memberName; 
            this.includeAsDesignerVerb = includeAsDesignerVerb; 
        }
 
        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.DesignerActionMethodItem2"]/*' />
        /// <devdoc>
        ///     [to be provvided]
        /// </devdoc> 
        public DesignerActionMethodItem(DesignerActionList actionList, string memberName, string displayName)
            : this(actionList, memberName, displayName, null, null, false) { 
        } 

        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.DesignerActionMethodItem3"]/*' /> 
        /// <devdoc>
        ///     [to be provvided]
        /// </devdoc>
        public DesignerActionMethodItem(DesignerActionList actionList, string memberName, string displayName, bool includeAsDesignerVerb) 
            : this(actionList, memberName, displayName, null, null, includeAsDesignerVerb) {
        } 
 
        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.DesignerActionMethodItem3"]/*' />
        /// <devdoc> 
        ///     [to be provvided]
        /// </devdoc>
        public DesignerActionMethodItem(DesignerActionList actionList, string memberName, string displayName, string category)
            : this(actionList, memberName, displayName, category, null, false) { 
        }
 
        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.DesignerActionMethodItem3"]/*' /> 
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc>
        public DesignerActionMethodItem(DesignerActionList actionList, string memberName, string displayName, string category, bool includeAsDesignerVerb)
            : this(actionList, memberName, displayName, category, null, includeAsDesignerVerb) {
        } 

        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.DesignerActionMethodItem3"]/*' /> 
        /// <devdoc> 
        ///     [to be provvided]
        /// </devdoc> 
        public DesignerActionMethodItem(DesignerActionList actionList, string memberName, string displayName, string category, string description)
            : this(actionList, memberName, displayName, category, description, false) {
        }
 

        internal DesignerActionMethodItem() { 
        } 

        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.MemberName"]/*' /> 
        /// <devdoc>
        ///     [to be provvided]
        /// </devdoc>
        public virtual string MemberName { 
            get {
                return memberName; 
            } 
        }
 
        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.RelatedComponent"]/*' />
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
 
        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.MemberName"]/*' />
        /// <devdoc> 
        ///     [to be provvided] 
        /// </devdoc>
        public virtual bool IncludeAsDesignerVerb { 
            get {
                return includeAsDesignerVerb;
            }
        } 

        // this is only use for verbs so that a designer action method item can 
        // be converted to a verb. Verbs use an EventHandler to call their invoke 
        // so we need a way to translate the EventHandler Invoke into ou own Invoke
        internal void Invoke(object sender, EventArgs args) { 
            Invoke();
        }

 
        public virtual void Invoke() {
            if (methodInfo == null) { 
                // we look public AND private or protected methods 
                methodInfo = actionList.GetType().GetMethod(memberName, BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            } 

            if (methodInfo != null) {
                methodInfo.Invoke(actionList, null);
            } 
            else {
                throw new InvalidOperationException(SR.GetString(SR.DesignerActionPanel_CouldNotFindMethod, MemberName)); 
            } 
        }
    } 
}



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionMethodItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.ComponentModel; 
    using System.Design;
    using System.Reflection;

    /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem"]/*' /> 
    /// <devdoc>
    ///     [to be provided] 
    /// </devdoc> 
    public class DesignerActionMethodItem  : DesignerActionItem {
 
        private string                       memberName;
        private bool                         includeAsDesignerVerb;
        private DesignerActionList           actionList;
        private MethodInfo                   methodInfo; 
        private IComponent                   relatedComponent;
 
        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.DesignerActionMethodItem"]/*' /> 
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc>
        public DesignerActionMethodItem(DesignerActionList actionList, string memberName, string displayName, string category, string description, bool includeAsDesignerVerb)
            : base( displayName, category, description)
        { 
            this.actionList = actionList;
            this.memberName = memberName; 
            this.includeAsDesignerVerb = includeAsDesignerVerb; 
        }
 
        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.DesignerActionMethodItem2"]/*' />
        /// <devdoc>
        ///     [to be provvided]
        /// </devdoc> 
        public DesignerActionMethodItem(DesignerActionList actionList, string memberName, string displayName)
            : this(actionList, memberName, displayName, null, null, false) { 
        } 

        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.DesignerActionMethodItem3"]/*' /> 
        /// <devdoc>
        ///     [to be provvided]
        /// </devdoc>
        public DesignerActionMethodItem(DesignerActionList actionList, string memberName, string displayName, bool includeAsDesignerVerb) 
            : this(actionList, memberName, displayName, null, null, includeAsDesignerVerb) {
        } 
 
        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.DesignerActionMethodItem3"]/*' />
        /// <devdoc> 
        ///     [to be provvided]
        /// </devdoc>
        public DesignerActionMethodItem(DesignerActionList actionList, string memberName, string displayName, string category)
            : this(actionList, memberName, displayName, category, null, false) { 
        }
 
        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.DesignerActionMethodItem3"]/*' /> 
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc>
        public DesignerActionMethodItem(DesignerActionList actionList, string memberName, string displayName, string category, bool includeAsDesignerVerb)
            : this(actionList, memberName, displayName, category, null, includeAsDesignerVerb) {
        } 

        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.DesignerActionMethodItem3"]/*' /> 
        /// <devdoc> 
        ///     [to be provvided]
        /// </devdoc> 
        public DesignerActionMethodItem(DesignerActionList actionList, string memberName, string displayName, string category, string description)
            : this(actionList, memberName, displayName, category, description, false) {
        }
 

        internal DesignerActionMethodItem() { 
        } 

        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.MemberName"]/*' /> 
        /// <devdoc>
        ///     [to be provvided]
        /// </devdoc>
        public virtual string MemberName { 
            get {
                return memberName; 
            } 
        }
 
        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.RelatedComponent"]/*' />
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
 
        /// <include file='doc\DesignerActionMethodItem.uex' path='docs/doc[@for="DesignerActionMethodItem.MemberName"]/*' />
        /// <devdoc> 
        ///     [to be provvided] 
        /// </devdoc>
        public virtual bool IncludeAsDesignerVerb { 
            get {
                return includeAsDesignerVerb;
            }
        } 

        // this is only use for verbs so that a designer action method item can 
        // be converted to a verb. Verbs use an EventHandler to call their invoke 
        // so we need a way to translate the EventHandler Invoke into ou own Invoke
        internal void Invoke(object sender, EventArgs args) { 
            Invoke();
        }

 
        public virtual void Invoke() {
            if (methodInfo == null) { 
                // we look public AND private or protected methods 
                methodInfo = actionList.GetType().GetMethod(memberName, BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            } 

            if (methodInfo != null) {
                methodInfo.Invoke(actionList, null);
            } 
            else {
                throw new InvalidOperationException(SR.GetString(SR.DesignerActionPanel_CouldNotFindMethod, MemberName)); 
            } 
        }
    } 
}



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
