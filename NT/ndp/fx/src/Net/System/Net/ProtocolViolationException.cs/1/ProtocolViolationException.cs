//------------------------------------------------------------------------------ 
// <copyright file="ProtocolViolationException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net { 
    using System.Runtime.Serialization; 
    using System.Security.Permissions;
 
    /// <devdoc>
    ///    <para>
    ///       An exception class used when an attempt is made to use an invalid
    ///       protocol. 
    ///    </para>
    /// </devdoc> 
    [Serializable] 
    public class ProtocolViolationException : InvalidOperationException, ISerializable {
        /// <devdoc> 
        ///    <para>
        ///       Creates a new instance of the <see cref='System.Net.ProtocolViolationException'/>class.
        ///    </para>
        /// </devdoc> 
        public ProtocolViolationException() : base() {
        } 
 
        /// <devdoc>
        ///    <para> 
        ///       Creates a new instance of the <see cref='System.Net.ProtocolViolationException'/>
        ///       class with the specified message.
        ///    </para>
        /// </devdoc> 
        public ProtocolViolationException(string message) : base(message) {
        } 
 
        protected ProtocolViolationException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) { 
        }

        /// <internalonly/>
 
        [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter, SerializationFormatter=true)]
        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext) { 
            base.GetObjectData(serializationInfo, streamingContext); 
        }
 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)] 		
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        { 
            base.GetObjectData(serializationInfo, streamingContext);
        } 
    }; // class ProtocolViolationException 

 
} // namespace System.Net
//------------------------------------------------------------------------------ 
// <copyright file="ProtocolViolationException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net { 
    using System.Runtime.Serialization; 
    using System.Security.Permissions;
 
    /// <devdoc>
    ///    <para>
    ///       An exception class used when an attempt is made to use an invalid
    ///       protocol. 
    ///    </para>
    /// </devdoc> 
    [Serializable] 
    public class ProtocolViolationException : InvalidOperationException, ISerializable {
        /// <devdoc> 
        ///    <para>
        ///       Creates a new instance of the <see cref='System.Net.ProtocolViolationException'/>class.
        ///    </para>
        /// </devdoc> 
        public ProtocolViolationException() : base() {
        } 
 
        /// <devdoc>
        ///    <para> 
        ///       Creates a new instance of the <see cref='System.Net.ProtocolViolationException'/>
        ///       class with the specified message.
        ///    </para>
        /// </devdoc> 
        public ProtocolViolationException(string message) : base(message) {
        } 
 
        protected ProtocolViolationException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) { 
        }

        /// <internalonly/>
 
        [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter, SerializationFormatter=true)]
        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext) { 
            base.GetObjectData(serializationInfo, streamingContext); 
        }
 

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)] 		
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        { 
            base.GetObjectData(serializationInfo, streamingContext);
        } 
    }; // class ProtocolViolationException 

 
} // namespace System.Net
