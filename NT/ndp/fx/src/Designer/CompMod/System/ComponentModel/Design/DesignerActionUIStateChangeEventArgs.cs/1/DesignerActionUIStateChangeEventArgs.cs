//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionListsChangedEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
    using System.ComponentModel; 
    using System;
    using Microsoft.Win32; 

    /// <include file='doc\DesignerActionListsChangedEventArgs.uex' path='docs/doc[@for="DesignerActionListsChangedEventArgs"]/*' />
    /// <devdoc>
    ///     This EventArgs class is used by the DesignerActionService to signify 
    ///     that there has been a change in DesignerActionLists (added or removed)
    ///     on the related object. 
    /// </devdoc> 
    public class DesignerActionUIStateChangeEventArgs : EventArgs {
 
        private object relatedObject;
        private DesignerActionUIStateChangeType changeType;//type of change

        /// <include file='doc\DesignerActionListsChangedEventArgs.uex' path='docs/doc[@for="DesignerActionListsChangedEventArgs.DesignerActionListsChangedEventArgs"]/*' /> 
        /// <devdoc>
        ///     Constructor that requires the object in question, the type of change 
        ///     and the remaining actionlists left for the object. 
        ///     on the related object.
        /// </devdoc> 
        public DesignerActionUIStateChangeEventArgs(object relatedObject, DesignerActionUIStateChangeType changeType) {
            this.relatedObject = relatedObject;
            this.changeType = changeType;
        } 

        /// <include file='doc\DesignerActionListsChangedEventArgs.uex' path='docs/doc[@for="DesignerActionListsChangedEventArgs.ChangeType"]/*' /> 
        /// <devdoc> 
        ///     The type of changed that caused the related event
        ///     to be thrown. 
        /// </devdoc>
        public DesignerActionUIStateChangeType ChangeType {
            get {
                return changeType; 
            }
        } 
 
        /// <include file='doc\DesignerActionListsChangedEventArgs.uex' path='docs/doc[@for="DesignerActionListsChangedEventArgs.RelatedObject"]/*' />
        /// <devdoc> 
        ///     The object this change is related to.
        /// </devdoc>
        public object RelatedObject {
            get { 
                return relatedObject;
            } 
        } 

    } 
}



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionListsChangedEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
    using System.ComponentModel; 
    using System;
    using Microsoft.Win32; 

    /// <include file='doc\DesignerActionListsChangedEventArgs.uex' path='docs/doc[@for="DesignerActionListsChangedEventArgs"]/*' />
    /// <devdoc>
    ///     This EventArgs class is used by the DesignerActionService to signify 
    ///     that there has been a change in DesignerActionLists (added or removed)
    ///     on the related object. 
    /// </devdoc> 
    public class DesignerActionUIStateChangeEventArgs : EventArgs {
 
        private object relatedObject;
        private DesignerActionUIStateChangeType changeType;//type of change

        /// <include file='doc\DesignerActionListsChangedEventArgs.uex' path='docs/doc[@for="DesignerActionListsChangedEventArgs.DesignerActionListsChangedEventArgs"]/*' /> 
        /// <devdoc>
        ///     Constructor that requires the object in question, the type of change 
        ///     and the remaining actionlists left for the object. 
        ///     on the related object.
        /// </devdoc> 
        public DesignerActionUIStateChangeEventArgs(object relatedObject, DesignerActionUIStateChangeType changeType) {
            this.relatedObject = relatedObject;
            this.changeType = changeType;
        } 

        /// <include file='doc\DesignerActionListsChangedEventArgs.uex' path='docs/doc[@for="DesignerActionListsChangedEventArgs.ChangeType"]/*' /> 
        /// <devdoc> 
        ///     The type of changed that caused the related event
        ///     to be thrown. 
        /// </devdoc>
        public DesignerActionUIStateChangeType ChangeType {
            get {
                return changeType; 
            }
        } 
 
        /// <include file='doc\DesignerActionListsChangedEventArgs.uex' path='docs/doc[@for="DesignerActionListsChangedEventArgs.RelatedObject"]/*' />
        /// <devdoc> 
        ///     The object this change is related to.
        /// </devdoc>
        public object RelatedObject {
            get { 
                return relatedObject;
            } 
        } 

    } 
}



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
