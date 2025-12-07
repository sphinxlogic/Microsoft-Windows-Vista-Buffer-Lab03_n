// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** File:    ContextProperty.cs 
**
** A contextProperty is a name-value pair holding the property 
** name and the object representing the property in a context.
** An array of these is returned by Context::GetContextProperties()
**
** 
**
===========================================================*/ 
 
namespace System.Runtime.Remoting.Contexts {
 
    using System;
    using System.Threading;
    using System.Reflection;
    using System.Runtime.InteropServices; 
    using System.Runtime.Remoting.Activation;
    using System.Security.Permissions; 
 
    /// <internalonly/>
    [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
    [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.Infrastructure)]
[System.Runtime.InteropServices.ComVisible(true)]
    public class ContextProperty {
        internal String _name;           // property name 
        internal Object _property;       // property object
 
    /// <internalonly/> 
        public virtual String Name {
            get { 
                return _name;
            }
        }
 
    /// <internalonly/>
        public virtual Object Property { 
            get { 
                return _property;
            } 
        }

        /* can't create outside the package */
        internal ContextProperty(String name, Object prop) 
        {
            _name = name; 
            _property = prop; 
        }
    } 

    //  The IContextAttribute interface is implemented by attribute classes.
    //  The attributes contribute a property which resides in a context and
    //  enforces a specific policy for the objects created in that context. 
    /// <internalonly/>
[System.Runtime.InteropServices.ComVisible(true)] 
    public interface IContextAttribute 
    {
    /// <internalonly/> 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
        bool IsContextOK(Context ctx, IConstructionCallMessage msg);
    /// <internalonly/>
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
        void GetPropertiesForNewContext(IConstructionCallMessage msg);
    } 
 
   //   This interface is exposed by the property contributed to a context
   //   by an attribute. By default, it is also implemented by the ContextAttribute 
   //   base class which every attribute class must extend from.
    /// <internalonly/>
[System.Runtime.InteropServices.ComVisible(true)]
    public interface IContextProperty 
    {
    /// <internalonly/> 
       //   This is the name under which the property will be added 
       //   to the {name,property} table in a context.
        String Name 
        {
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
        get;
    } 
    /// <internalonly/>
       //   After forming the newCtx, we ask each property if it is happy 
       //   with the context. We expect most implementations to say yes. 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
        bool IsNewContextOK(Context newCtx); 
    /// <internalonly/>

        // New method. All properties are notified when the context
        // they are in is frozen. 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
        void Freeze(Context newContext); 
    } 

    /// <internalonly/> 
[System.Runtime.InteropServices.ComVisible(true)]
    public interface IContextPropertyActivator
    {
    /// <internalonly/> 
        // This method lets properties in the current context have a say in
        // whether an activation may be done 'here' or not. 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
        bool IsOKToActivate(IConstructionCallMessage msg);
    /// <internalonly/> 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
        void CollectFromClientContext(IConstructionCallMessage msg);
    /// <internalonly/>
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
        bool DeliverClientContextToServerContext(IConstructionCallMessage msg);
    /// <internalonly/> 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
        void CollectFromServerContext(IConstructionReturnMessage msg);
    /// <internalonly/> 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
        bool DeliverServerContextToClientContext(IConstructionReturnMessage msg);
    }
 

   //   All context attribute classes must extend from this base class. 
   //   This class provides the base implementations which the derived 
   //   classes are free to over-ride. The base implementations provide
   //   the default answers to various questions. 

    /// <internalonly/>
    [AttributeUsage(AttributeTargets.Class), Serializable]
    [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
    [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.Infrastructure)]
[System.Runtime.InteropServices.ComVisible(true)] 
    public class ContextAttribute 
        : Attribute, IContextAttribute, IContextProperty
    { 
    /// <internalonly/>
        protected String AttributeName;

        // The derived class must call: base(name); 
    /// <internalonly/>
        public ContextAttribute(String name) 
        { 
            AttributeName = name;
        } 

        // IContextPropery::Name
        // Default implementation provides AttributeName as the property name.
    /// <internalonly/> 
        public virtual String Name { get {return AttributeName;} }
 
        // IContextProperty::IsNewContextOK 
    /// <internalonly/>
        public virtual bool IsNewContextOK(Context newCtx) 
        {
            // This will be called before entering the newCtx
            // Default implementation says OK.
            return true; 
        }
 
        // IContextProperty::Freeze 
        // Default implementation does nothing
    /// <internalonly/> 
        public virtual void Freeze(Context newContext)
        {
            BCLDebug.Log("ContextAttribute::ContextProperty::Freeze"+
                        " for context " + newContext ); 
        }
 
        // Object::Equals 
        // Default implementation just compares the names
    /// <internalonly/> 
        public override bool Equals(Object o)
        {
            IContextProperty prop = o as IContextProperty;
            return  (null != prop) && AttributeName.Equals(prop.Name); 
        }
 
    /// <internalonly/> 
        public override int GetHashCode()
        { 
            return this.AttributeName.GetHashCode();
        }

        // IContextAttribute::IsContextOK 
        // Default calls Object::Equals on the property and does not
        // bother with the ctorMsg. 
    /// <internalonly/> 
        public virtual bool IsContextOK(
            Context ctx, IConstructionCallMessage ctorMsg) 
        {
            if (ctx == null)
                throw new ArgumentNullException("ctx");
            if (ctorMsg == null) 
                throw new ArgumentNullException("ctorMsg");
 
            BCLDebug.Assert(ctorMsg.ActivationType.IsMarshalByRef, "Activation on a non MarshalByRef object"); 

            if (!ctorMsg.ActivationType.IsContextful) 
            {
                return true;
            }
 
            Object prop = ctx.GetProperty(AttributeName);
            if ((prop!=null) && (Equals(prop))) 
            { 
                return true;
            } 
            return false;
        }

        // IContextAttribute::GetPropertiesForNewContext 
        // Default adds the attribute itself w/o regard to the current
        // list of properties 
    /// <internalonly/> 
        public virtual void GetPropertiesForNewContext(
            IConstructionCallMessage ctorMsg) 
        {
            if (ctorMsg == null)
                throw new ArgumentNullException("ctorMsg");
            ctorMsg.ContextProperties.Add((IContextProperty)this); 
        }
    } 
 
#if SIMPLEXAACTIVATION
    /// <internalonly/> 
        [AttributeUsage(AttributeTargets.Class)]
[System.Runtime.InteropServices.ComVisible(true)]
    public class new_appdomain : ContextAttribute
    { 

        internal static int _domain_no = 0; 
 
    /// <internalonly/>
        public new_appdomain() : base("new_appdomain") {} 

    /// <internalonly/>
[System.Runtime.InteropServices.ComVisible(true)]
        public override bool IsContextOK(Context ctx, IConstructionCallMessage msg) 
        {
            return false; 
        } 

    /// <internalonly/> 
[System.Runtime.InteropServices.ComVisible(true)]
        public override void GetPropertiesForNewContext(IConstructionCallMessage ctorMsg)
        {
            ctorMsg.GetProperties()["__new_appdomain"] = true; 
        }
 
    /// <internalonly/> 
[System.Runtime.InteropServices.ComVisible(true)]
        public static IConstructionReturnMessage DoSimpleXADActivation(IConstructionCallMessage msg) 
        {
            int domain_no = Interlocked.Increment(ref _domain_no);
            AppDomain ad = AppDomain.CreateDomain("AutoDomain #" + domain_no, null, null);
 
            activator a = (activator) (ad.CreateInstance(null, typeof(activator).FullName)).Unwrap();
 
            return a.Activate(msg); 
        }
 
    /// <internalonly/>
[System.Runtime.InteropServices.ComVisible(true)]
        public class activator : MarshalByRefObject, IActivator
        { 
        /// <internalonly/>
[System.Runtime.InteropServices.ComVisible(true)] 
            public IConstructionReturnMessage Activate(IConstructionCallMessage msg) 
            {
                return RemotingServices.DoCrossContextActivation(msg); 
            }

        /// <internalonly/>
            public void LoadAssembly(AssemblyName an) 
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller; 
                Assembly a = Assembly.InternalLoad(an, false, null, ref stackMark); 
                if (a == null)
                { 
                    throw new RemotingException(
                        String.Format(
                            Environment.GetResourceString(
                                "Remoting_AssemblyLoadFailed"), 
                            an));
                } 
            } 
        }
    } 

    /// <internalonly/>
        [new_appdomain]
[System.Runtime.InteropServices.ComVisible(true)] 
        public class MBR : MarshalByRefObject
        { 
        /// <internalonly/> 
            public String MyAppDomain()
            { 
                return Thread.GetDomain().GetFriendlyName();
            }
        }
 
    /// <internalonly/>
        [new_appdomain] 
[System.Runtime.InteropServices.ComVisible(true)] 
        public class CB : ContextBoundObject
        { 
        /// <internalonly/>
            public String MyAppDomain()
            {
                return Thread.GetDomain().GetFriendlyName(); 
            }
        } 
#endif 

} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** File:    ContextProperty.cs 
**
** A contextProperty is a name-value pair holding the property 
** name and the object representing the property in a context.
** An array of these is returned by Context::GetContextProperties()
**
** 
**
===========================================================*/ 
 
namespace System.Runtime.Remoting.Contexts {
 
    using System;
    using System.Threading;
    using System.Reflection;
    using System.Runtime.InteropServices; 
    using System.Runtime.Remoting.Activation;
    using System.Security.Permissions; 
 
    /// <internalonly/>
    [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
    [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.Infrastructure)]
[System.Runtime.InteropServices.ComVisible(true)]
    public class ContextProperty {
        internal String _name;           // property name 
        internal Object _property;       // property object
 
    /// <internalonly/> 
        public virtual String Name {
            get { 
                return _name;
            }
        }
 
    /// <internalonly/>
        public virtual Object Property { 
            get { 
                return _property;
            } 
        }

        /* can't create outside the package */
        internal ContextProperty(String name, Object prop) 
        {
            _name = name; 
            _property = prop; 
        }
    } 

    //  The IContextAttribute interface is implemented by attribute classes.
    //  The attributes contribute a property which resides in a context and
    //  enforces a specific policy for the objects created in that context. 
    /// <internalonly/>
[System.Runtime.InteropServices.ComVisible(true)] 
    public interface IContextAttribute 
    {
    /// <internalonly/> 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
        bool IsContextOK(Context ctx, IConstructionCallMessage msg);
    /// <internalonly/>
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
        void GetPropertiesForNewContext(IConstructionCallMessage msg);
    } 
 
   //   This interface is exposed by the property contributed to a context
   //   by an attribute. By default, it is also implemented by the ContextAttribute 
   //   base class which every attribute class must extend from.
    /// <internalonly/>
[System.Runtime.InteropServices.ComVisible(true)]
    public interface IContextProperty 
    {
    /// <internalonly/> 
       //   This is the name under which the property will be added 
       //   to the {name,property} table in a context.
        String Name 
        {
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
        get;
    } 
    /// <internalonly/>
       //   After forming the newCtx, we ask each property if it is happy 
       //   with the context. We expect most implementations to say yes. 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
        bool IsNewContextOK(Context newCtx); 
    /// <internalonly/>

        // New method. All properties are notified when the context
        // they are in is frozen. 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
        void Freeze(Context newContext); 
    } 

    /// <internalonly/> 
[System.Runtime.InteropServices.ComVisible(true)]
    public interface IContextPropertyActivator
    {
    /// <internalonly/> 
        // This method lets properties in the current context have a say in
        // whether an activation may be done 'here' or not. 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
        bool IsOKToActivate(IConstructionCallMessage msg);
    /// <internalonly/> 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
        void CollectFromClientContext(IConstructionCallMessage msg);
    /// <internalonly/>
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
        bool DeliverClientContextToServerContext(IConstructionCallMessage msg);
    /// <internalonly/> 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
        void CollectFromServerContext(IConstructionReturnMessage msg);
    /// <internalonly/> 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
        bool DeliverServerContextToClientContext(IConstructionReturnMessage msg);
    }
 

   //   All context attribute classes must extend from this base class. 
   //   This class provides the base implementations which the derived 
   //   classes are free to over-ride. The base implementations provide
   //   the default answers to various questions. 

    /// <internalonly/>
    [AttributeUsage(AttributeTargets.Class), Serializable]
    [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)] 
    [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.Infrastructure)]
[System.Runtime.InteropServices.ComVisible(true)] 
    public class ContextAttribute 
        : Attribute, IContextAttribute, IContextProperty
    { 
    /// <internalonly/>
        protected String AttributeName;

        // The derived class must call: base(name); 
    /// <internalonly/>
        public ContextAttribute(String name) 
        { 
            AttributeName = name;
        } 

        // IContextPropery::Name
        // Default implementation provides AttributeName as the property name.
    /// <internalonly/> 
        public virtual String Name { get {return AttributeName;} }
 
        // IContextProperty::IsNewContextOK 
    /// <internalonly/>
        public virtual bool IsNewContextOK(Context newCtx) 
        {
            // This will be called before entering the newCtx
            // Default implementation says OK.
            return true; 
        }
 
        // IContextProperty::Freeze 
        // Default implementation does nothing
    /// <internalonly/> 
        public virtual void Freeze(Context newContext)
        {
            BCLDebug.Log("ContextAttribute::ContextProperty::Freeze"+
                        " for context " + newContext ); 
        }
 
        // Object::Equals 
        // Default implementation just compares the names
    /// <internalonly/> 
        public override bool Equals(Object o)
        {
            IContextProperty prop = o as IContextProperty;
            return  (null != prop) && AttributeName.Equals(prop.Name); 
        }
 
    /// <internalonly/> 
        public override int GetHashCode()
        { 
            return this.AttributeName.GetHashCode();
        }

        // IContextAttribute::IsContextOK 
        // Default calls Object::Equals on the property and does not
        // bother with the ctorMsg. 
    /// <internalonly/> 
        public virtual bool IsContextOK(
            Context ctx, IConstructionCallMessage ctorMsg) 
        {
            if (ctx == null)
                throw new ArgumentNullException("ctx");
            if (ctorMsg == null) 
                throw new ArgumentNullException("ctorMsg");
 
            BCLDebug.Assert(ctorMsg.ActivationType.IsMarshalByRef, "Activation on a non MarshalByRef object"); 

            if (!ctorMsg.ActivationType.IsContextful) 
            {
                return true;
            }
 
            Object prop = ctx.GetProperty(AttributeName);
            if ((prop!=null) && (Equals(prop))) 
            { 
                return true;
            } 
            return false;
        }

        // IContextAttribute::GetPropertiesForNewContext 
        // Default adds the attribute itself w/o regard to the current
        // list of properties 
    /// <internalonly/> 
        public virtual void GetPropertiesForNewContext(
            IConstructionCallMessage ctorMsg) 
        {
            if (ctorMsg == null)
                throw new ArgumentNullException("ctorMsg");
            ctorMsg.ContextProperties.Add((IContextProperty)this); 
        }
    } 
 
#if SIMPLEXAACTIVATION
    /// <internalonly/> 
        [AttributeUsage(AttributeTargets.Class)]
[System.Runtime.InteropServices.ComVisible(true)]
    public class new_appdomain : ContextAttribute
    { 

        internal static int _domain_no = 0; 
 
    /// <internalonly/>
        public new_appdomain() : base("new_appdomain") {} 

    /// <internalonly/>
[System.Runtime.InteropServices.ComVisible(true)]
        public override bool IsContextOK(Context ctx, IConstructionCallMessage msg) 
        {
            return false; 
        } 

    /// <internalonly/> 
[System.Runtime.InteropServices.ComVisible(true)]
        public override void GetPropertiesForNewContext(IConstructionCallMessage ctorMsg)
        {
            ctorMsg.GetProperties()["__new_appdomain"] = true; 
        }
 
    /// <internalonly/> 
[System.Runtime.InteropServices.ComVisible(true)]
        public static IConstructionReturnMessage DoSimpleXADActivation(IConstructionCallMessage msg) 
        {
            int domain_no = Interlocked.Increment(ref _domain_no);
            AppDomain ad = AppDomain.CreateDomain("AutoDomain #" + domain_no, null, null);
 
            activator a = (activator) (ad.CreateInstance(null, typeof(activator).FullName)).Unwrap();
 
            return a.Activate(msg); 
        }
 
    /// <internalonly/>
[System.Runtime.InteropServices.ComVisible(true)]
        public class activator : MarshalByRefObject, IActivator
        { 
        /// <internalonly/>
[System.Runtime.InteropServices.ComVisible(true)] 
            public IConstructionReturnMessage Activate(IConstructionCallMessage msg) 
            {
                return RemotingServices.DoCrossContextActivation(msg); 
            }

        /// <internalonly/>
            public void LoadAssembly(AssemblyName an) 
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller; 
                Assembly a = Assembly.InternalLoad(an, false, null, ref stackMark); 
                if (a == null)
                { 
                    throw new RemotingException(
                        String.Format(
                            Environment.GetResourceString(
                                "Remoting_AssemblyLoadFailed"), 
                            an));
                } 
            } 
        }
    } 

    /// <internalonly/>
        [new_appdomain]
[System.Runtime.InteropServices.ComVisible(true)] 
        public class MBR : MarshalByRefObject
        { 
        /// <internalonly/> 
            public String MyAppDomain()
            { 
                return Thread.GetDomain().GetFriendlyName();
            }
        }
 
    /// <internalonly/>
        [new_appdomain] 
[System.Runtime.InteropServices.ComVisible(true)] 
        public class CB : ContextBoundObject
        { 
        /// <internalonly/>
            public String MyAppDomain()
            {
                return Thread.GetDomain().GetFriendlyName(); 
            }
        } 
#endif 

} 
