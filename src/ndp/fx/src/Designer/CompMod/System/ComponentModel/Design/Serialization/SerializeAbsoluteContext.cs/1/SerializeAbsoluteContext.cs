//------------------------------------------------------------------------------ 
// <copyright file="SerializeAbsoluteContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Serialization { 
 
    using System;
    using System.CodeDom; 

    /// <include file='doc\SerializeAbsoluteContext.uex' path='docs/doc[@for="SerializeAbsoluteContext"]/*' />
    /// <devdoc>
    ///     The ComponentSerializationService supports "absolute" serialization, where instead of just 
    ///     serializing values that differ from an object's default values, all values are
    ///     serialized in such a way as to be able to reset values to their defaults for 
    ///     objects that may have already been initialized.  When a component serialization service 
    ///     wishes to indicate this to CodeDomSerializer objects, it will place a
    ///     SerializeAbsoluteContext on the context stack.  The member in this context may be null, 
    ///     to indicate that all members are serialized, or a member indicating that only a
    ///     specific member is being serialized at this time.
    /// </devdoc>
    public sealed class SerializeAbsoluteContext { 

        private MemberDescriptor _member; 
 
        /// <include file='doc\SerializeAbsoluteContext.uex' path='docs/doc[@for="SerializeAbsoluteContext.SerializeAbsoluteContext"]/*' />
        /// <devdoc> 
        ///     Creeates a new SerializeAbsoluteContext.  Member can be null or omitted to indicate this context
        ///     should be used for all members.
        /// </devdoc>
        public SerializeAbsoluteContext() { 
        }
 
        /// <include file='doc\SerializeAbsoluteContext.uex' path='docs/doc[@for="SerializeAbsoluteContext.SerializeAbsoluteContext1"]/*' /> 
        /// <devdoc>
        ///     Creeates a new SerializeAbsoluteContext.  Member can be null or omitted to indicate this context 
        ///     should be used for all members.
        /// </devdoc>
        public SerializeAbsoluteContext(MemberDescriptor member) {
            _member = member; 
        }
 
        /// <include file='doc\SerializeAbsoluteContext.uex' path='docs/doc[@for="SerializeAbsoluteContext.Member"]/*' /> 
        /// <devdoc>
        ///     This property returns the member this context is bound to.  It may be null to 
        ///     indicate the context is bound to all members of an object.
        /// </devdoc>
        public MemberDescriptor Member {
            get { 
                return _member;
            } 
        } 

        /// <include file='doc\SerializeAbsoluteContext.uex' path='docs/doc[@for="SerializeAbsoluteContext.ShouldSerialize"]/*' /> 
        /// <devdoc>
        ///     Returns true if the given member should be serialized in this context.
        /// </devdoc>
        public bool ShouldSerialize(MemberDescriptor member) { 
            return (_member == null || _member == member);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SerializeAbsoluteContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design.Serialization { 
 
    using System;
    using System.CodeDom; 

    /// <include file='doc\SerializeAbsoluteContext.uex' path='docs/doc[@for="SerializeAbsoluteContext"]/*' />
    /// <devdoc>
    ///     The ComponentSerializationService supports "absolute" serialization, where instead of just 
    ///     serializing values that differ from an object's default values, all values are
    ///     serialized in such a way as to be able to reset values to their defaults for 
    ///     objects that may have already been initialized.  When a component serialization service 
    ///     wishes to indicate this to CodeDomSerializer objects, it will place a
    ///     SerializeAbsoluteContext on the context stack.  The member in this context may be null, 
    ///     to indicate that all members are serialized, or a member indicating that only a
    ///     specific member is being serialized at this time.
    /// </devdoc>
    public sealed class SerializeAbsoluteContext { 

        private MemberDescriptor _member; 
 
        /// <include file='doc\SerializeAbsoluteContext.uex' path='docs/doc[@for="SerializeAbsoluteContext.SerializeAbsoluteContext"]/*' />
        /// <devdoc> 
        ///     Creeates a new SerializeAbsoluteContext.  Member can be null or omitted to indicate this context
        ///     should be used for all members.
        /// </devdoc>
        public SerializeAbsoluteContext() { 
        }
 
        /// <include file='doc\SerializeAbsoluteContext.uex' path='docs/doc[@for="SerializeAbsoluteContext.SerializeAbsoluteContext1"]/*' /> 
        /// <devdoc>
        ///     Creeates a new SerializeAbsoluteContext.  Member can be null or omitted to indicate this context 
        ///     should be used for all members.
        /// </devdoc>
        public SerializeAbsoluteContext(MemberDescriptor member) {
            _member = member; 
        }
 
        /// <include file='doc\SerializeAbsoluteContext.uex' path='docs/doc[@for="SerializeAbsoluteContext.Member"]/*' /> 
        /// <devdoc>
        ///     This property returns the member this context is bound to.  It may be null to 
        ///     indicate the context is bound to all members of an object.
        /// </devdoc>
        public MemberDescriptor Member {
            get { 
                return _member;
            } 
        } 

        /// <include file='doc\SerializeAbsoluteContext.uex' path='docs/doc[@for="SerializeAbsoluteContext.ShouldSerialize"]/*' /> 
        /// <devdoc>
        ///     Returns true if the given member should be serialized in this context.
        /// </devdoc>
        public bool ShouldSerialize(MemberDescriptor member) { 
            return (_member == null || _member == member);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
