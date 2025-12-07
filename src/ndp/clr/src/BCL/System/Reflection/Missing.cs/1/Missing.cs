// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

using System; 
using System.Runtime.Remoting; 
using System.Runtime.Serialization;
using System.Security.Permissions; 

namespace System.Reflection
{
    // This is not serializable because it is a reflection command. 
    [Serializable()]
[System.Runtime.InteropServices.ComVisible(true)] 
    public sealed class Missing: ISerializable 
    {
    	public static readonly Missing Value = new Missing(); 

        #region Constructor
        private Missing() { }
        #endregion 

        #region ISerializable 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)] 
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        { 
            if (info == null)
                throw new ArgumentNullException("info");

            UnitySerializationHolder.GetUnitySerializationInfo(info, this); 
        }
        #endregion 
    } 
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

using System; 
using System.Runtime.Remoting; 
using System.Runtime.Serialization;
using System.Security.Permissions; 

namespace System.Reflection
{
    // This is not serializable because it is a reflection command. 
    [Serializable()]
[System.Runtime.InteropServices.ComVisible(true)] 
    public sealed class Missing: ISerializable 
    {
    	public static readonly Missing Value = new Missing(); 

        #region Constructor
        private Missing() { }
        #endregion 

        #region ISerializable 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)] 
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        { 
            if (info == null)
                throw new ArgumentNullException("info");

            UnitySerializationHolder.GetUnitySerializationInfo(info, this); 
        }
        #endregion 
    } 
}
