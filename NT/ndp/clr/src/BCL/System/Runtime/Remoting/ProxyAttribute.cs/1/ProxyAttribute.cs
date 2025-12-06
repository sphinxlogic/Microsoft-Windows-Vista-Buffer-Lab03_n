// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** File:    ProxyAttribute.cs 
**
** 
** Purpose: Defines the attribute that is used on types which
**          need custom proxies.
**
** 
===========================================================*/
namespace System.Runtime.Remoting.Proxies { 
 
    using System.Reflection;
    using System.Runtime.Remoting.Activation; 
    using System.Runtime.Remoting.Contexts;
    using System.Security.Permissions;

    // Attribute for types that need custom proxies 
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]	 
    [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
[System.Runtime.InteropServices.ComVisible(true)]
    public class ProxyAttribute : Attribute , IContextAttribute 
    {
        public ProxyAttribute()
        {
            // Default constructor 
        }
 
        // Default implementation of CreateInstance uses our activation services to create an instance 
        // of the transparent proxy or an uninitialized marshalbyrefobject and returns it.
 
        public virtual MarshalByRefObject CreateInstance(Type serverType)
        {
            if (!serverType.IsContextful)
            { 
                throw new RemotingException(
                    Environment.GetResourceString( 
                        "Remoting_Activation_MBR_ProxyAttribute")); 
            }
            if (serverType.IsAbstract) 
            {
                throw new RemotingException(
                    Environment.GetResourceString(
                        "Acc_CreateAbst")); 
            }
            return CreateInstanceInternal(serverType); 
        } 

        internal MarshalByRefObject CreateInstanceInternal(Type serverType) 
        {
            return ActivationServices.CreateInstance(serverType);
        }
 
        // Default implementation of CreateProxy creates an instance of our
        // remoting proxy 
 
        public virtual RealProxy CreateProxy(ObjRef objRef,
                                             Type serverType, 
                                             Object serverObject,
                                             Context serverContext)
        {
            RemotingProxy rp =  new RemotingProxy(serverType); 

            // If this is a serverID, set the native context field in the TP 
            if (null != serverContext) 
            {
                RealProxy.SetStubData(rp, serverContext.InternalContextID); 
            }

            if (objRef != null && objRef.GetServerIdentity().IsAllocated)
            { 
                rp.SetSrvInfo(objRef.GetServerIdentity(), objRef.GetDomainID());
            } 
 
            // Set the flag indicating that the fields of the proxy
            // have been initialized 
            rp.Initialized = true;

            // Sanity check
            Type t = serverType; 
            if (!t.IsContextful &&
                !t.IsMarshalByRef && 
                (null != serverContext)) 
            {
                throw new RemotingException( 
                    Environment.GetResourceString(
                        "Remoting_Activation_MBR_ProxyAttribute"));
            }
 
            return rp;
        } 
 
        // implementation of interface IContextAttribute
[System.Runtime.InteropServices.ComVisible(true)] 
        public bool IsContextOK(Context ctx, IConstructionCallMessage msg)
        {
            // always happy...
            return true; 
        }
 
[System.Runtime.InteropServices.ComVisible(true)] 
        public void GetPropertiesForNewContext(IConstructionCallMessage msg)
        { 
            // chill.. do nothing.
            return;
        }
    } 
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** File:    ProxyAttribute.cs 
**
** 
** Purpose: Defines the attribute that is used on types which
**          need custom proxies.
**
** 
===========================================================*/
namespace System.Runtime.Remoting.Proxies { 
 
    using System.Reflection;
    using System.Runtime.Remoting.Activation; 
    using System.Runtime.Remoting.Contexts;
    using System.Security.Permissions;

    // Attribute for types that need custom proxies 
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]	 
    [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
[System.Runtime.InteropServices.ComVisible(true)]
    public class ProxyAttribute : Attribute , IContextAttribute 
    {
        public ProxyAttribute()
        {
            // Default constructor 
        }
 
        // Default implementation of CreateInstance uses our activation services to create an instance 
        // of the transparent proxy or an uninitialized marshalbyrefobject and returns it.
 
        public virtual MarshalByRefObject CreateInstance(Type serverType)
        {
            if (!serverType.IsContextful)
            { 
                throw new RemotingException(
                    Environment.GetResourceString( 
                        "Remoting_Activation_MBR_ProxyAttribute")); 
            }
            if (serverType.IsAbstract) 
            {
                throw new RemotingException(
                    Environment.GetResourceString(
                        "Acc_CreateAbst")); 
            }
            return CreateInstanceInternal(serverType); 
        } 

        internal MarshalByRefObject CreateInstanceInternal(Type serverType) 
        {
            return ActivationServices.CreateInstance(serverType);
        }
 
        // Default implementation of CreateProxy creates an instance of our
        // remoting proxy 
 
        public virtual RealProxy CreateProxy(ObjRef objRef,
                                             Type serverType, 
                                             Object serverObject,
                                             Context serverContext)
        {
            RemotingProxy rp =  new RemotingProxy(serverType); 

            // If this is a serverID, set the native context field in the TP 
            if (null != serverContext) 
            {
                RealProxy.SetStubData(rp, serverContext.InternalContextID); 
            }

            if (objRef != null && objRef.GetServerIdentity().IsAllocated)
            { 
                rp.SetSrvInfo(objRef.GetServerIdentity(), objRef.GetDomainID());
            } 
 
            // Set the flag indicating that the fields of the proxy
            // have been initialized 
            rp.Initialized = true;

            // Sanity check
            Type t = serverType; 
            if (!t.IsContextful &&
                !t.IsMarshalByRef && 
                (null != serverContext)) 
            {
                throw new RemotingException( 
                    Environment.GetResourceString(
                        "Remoting_Activation_MBR_ProxyAttribute"));
            }
 
            return rp;
        } 
 
        // implementation of interface IContextAttribute
[System.Runtime.InteropServices.ComVisible(true)] 
        public bool IsContextOK(Context ctx, IConstructionCallMessage msg)
        {
            // always happy...
            return true; 
        }
 
[System.Runtime.InteropServices.ComVisible(true)] 
        public void GetPropertiesForNewContext(IConstructionCallMessage msg)
        { 
            // chill.. do nothing.
            return;
        }
    } 
}
