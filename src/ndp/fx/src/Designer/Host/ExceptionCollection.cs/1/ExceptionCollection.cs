 
//------------------------------------------------------------------------------
// <copyright file="ExceptionCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.ComponentModel.Design { 

    using System; 
    using System.Collections;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
 
    [Serializable]
    public sealed class ExceptionCollection : Exception { 
        ArrayList exceptions; 

        public ExceptionCollection(ArrayList exceptions) { 
            this.exceptions = exceptions;
        }

        /// <devdoc> 
        ///     Need this constructor since Exception implements ISerializable.
        /// </devdoc> 
        private ExceptionCollection(SerializationInfo info, StreamingContext context) : base (info, context) { 
            exceptions = (ArrayList) info.GetValue("exceptions", typeof(ArrayList));
        } 

        public ArrayList Exceptions {
            get {
                if (exceptions != null) { 
                    return (ArrayList) exceptions.Clone();
                } 
 
                return null;
            } 
        }

        /// <devdoc>
        ///     Need this since Exception implements ISerializable and we have fields to save out. 
        /// </devdoc>
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)] 
        public override void GetObjectData(SerializationInfo info, StreamingContext context) { 
            if (info == null) {
                throw new ArgumentNullException("info"); 
            }

            info.AddValue("exceptions", exceptions);
 
            base.GetObjectData(info, context);
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright file="ExceptionCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.ComponentModel.Design { 

    using System; 
    using System.Collections;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
 
    [Serializable]
    public sealed class ExceptionCollection : Exception { 
        ArrayList exceptions; 

        public ExceptionCollection(ArrayList exceptions) { 
            this.exceptions = exceptions;
        }

        /// <devdoc> 
        ///     Need this constructor since Exception implements ISerializable.
        /// </devdoc> 
        private ExceptionCollection(SerializationInfo info, StreamingContext context) : base (info, context) { 
            exceptions = (ArrayList) info.GetValue("exceptions", typeof(ArrayList));
        } 

        public ArrayList Exceptions {
            get {
                if (exceptions != null) { 
                    return (ArrayList) exceptions.Clone();
                } 
 
                return null;
            } 
        }

        /// <devdoc>
        ///     Need this since Exception implements ISerializable and we have fields to save out. 
        /// </devdoc>
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)] 
        public override void GetObjectData(SerializationInfo info, StreamingContext context) { 
            if (info == null) {
                throw new ArgumentNullException("info"); 
            }

            info.AddValue("exceptions", exceptions);
 
            base.GetObjectData(info, context);
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
