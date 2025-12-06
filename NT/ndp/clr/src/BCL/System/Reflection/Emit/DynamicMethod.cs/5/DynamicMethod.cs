// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

namespace System.Reflection.Emit 
{ 
    using System;
    using CultureInfo = System.Globalization.CultureInfo; 
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading; 
    using System.Runtime.CompilerServices;
 
[System.Runtime.InteropServices.ComVisible(true)] 
    public sealed class DynamicMethod : MethodInfo
    { 
        RuntimeType[] m_parameterTypes;
        RuntimeType m_returnType;
        DynamicILGenerator m_ilGenerator;
        DynamicILInfo m_DynamicILInfo; 
        bool m_fInitLocals;
        internal RuntimeMethodHandle m_method; 
        internal ModuleHandle m_module; 
        internal bool m_skipVisibility;
        internal RuntimeType m_typeOwner; // can be null 

        // We want the creator of the DynamicMethod to control who has access to the
        // DynamicMethod (just like we do for delegates). However, a user can get to
        // the corresponding RTDynamicMethod using Exception.TargetSite, StackFrame.GetMethod, etc. 
        // If we allowed use of RTDynamicMethod, the creator of the DynamicMethod would
        // not be able to bound access to the DynamicMethod. Hence, we need to ensure that 
        // we do not allow direct use of RTDynamicMethod. 
        RTDynamicMethod m_dynMethod;
 
        // needed to keep the object alive during jitting
        // assigned by the DynamicResolver ctor
        internal DynamicResolver m_resolver;
 
        internal bool m_restrictedSkipVisibility;
        // The context when the method was created. We use this to do the RestrictedMemberAccess checks. 
        // These checks are done when the method is compiled. This can happen at an arbitrary time, 
        // when CreateDelegate or Invoke is called, or when another DynamicMethod executes OpCodes.Call.
        // We capture the creation context so that we can do the checks against the same context, 
        // irrespective of when the method gets compiled. Note that the DynamicMethod does not know when
        // it is ready for use since there is not API which indictates that IL generation has completed.
        internal CompressedStack m_creationContext;
        private static Module s_anonymouslyHostedDynamicMethodsModule; 
        private static readonly object s_anonymouslyHostedDynamicMethodsModuleLock = new object();
 
        // 
        // class initialization (ctor and init)
        // 

        private DynamicMethod() { }

        public DynamicMethod(string name, 
                             Type returnType,
                             Type[] parameterTypes) 
        { 
            Init(name,
                MethodAttributes.Public | MethodAttributes.Static, 
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                null,   // owner 
                null,   // m
                false,  // skipVisibility 
                true);  // transparentMethod 
        }
 
        public DynamicMethod(string name,
                             Type returnType,
                             Type[] parameterTypes,
                             bool restrictedSkipVisibility) 
        {
            Init(name, 
                MethodAttributes.Public | MethodAttributes.Static, 
                CallingConventions.Standard,
                returnType, 
                parameterTypes,
                null,   // owner
                null,   // m
                restrictedSkipVisibility, 
                true);  // transparentMethod
        } 
 
        public DynamicMethod(string name,
                             Type returnType, 
                             Type[] parameterTypes,
                             Module m) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            DynamicMethod.PerformSecurityCheck(m, ref stackMark, false); 
            Init(name,
                MethodAttributes.Public | MethodAttributes.Static, 
                CallingConventions.Standard, 
                returnType,
                parameterTypes, 
                null,   // owner
                m,      // m
                false,  // skipVisibility
                false);  // transparentMethod 
        }
 
        public DynamicMethod(string name, 
                             Type returnType,
                             Type[] parameterTypes, 
                             Module m,
                             bool skipVisibility) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            DynamicMethod.PerformSecurityCheck(m, ref stackMark, skipVisibility); 
            Init(name,
                MethodAttributes.Public | MethodAttributes.Static, 
                CallingConventions.Standard, 
                returnType,
                parameterTypes, 
                null,   // owner
                m,      // m
                skipVisibility,
                false); // transparentMethod 
        }
 
        public DynamicMethod(string name, 
                             MethodAttributes attributes,
                             CallingConventions callingConvention, 
                             Type returnType,
                             Type[] parameterTypes,
                             Module m,
                             bool skipVisibility) { 
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            DynamicMethod.PerformSecurityCheck(m, ref stackMark, skipVisibility); 
            Init(name, 
                attributes,
                callingConvention, 
                returnType,
                parameterTypes,
                null,   // owner
                m,      // m 
                skipVisibility,
                false); // transparentMethod 
        } 

        public DynamicMethod(string name, 
                             Type returnType,
                             Type[] parameterTypes,
                             Type owner) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller; 
            DynamicMethod.PerformSecurityCheck(owner, ref stackMark, false);
            Init(name, 
                MethodAttributes.Public | MethodAttributes.Static, 
                CallingConventions.Standard,
                returnType, 
                parameterTypes,
                owner,  // owner
                null,   // m
                false,  // skipVisibility 
                false); // transparentMethod
        } 
 
        public DynamicMethod(string name,
                             Type returnType, 
                             Type[] parameterTypes,
                             Type owner,
                             bool skipVisibility) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller; 
            DynamicMethod.PerformSecurityCheck(owner, ref stackMark, skipVisibility);
            Init(name, 
                MethodAttributes.Public | MethodAttributes.Static, 
                CallingConventions.Standard,
                returnType, 
                parameterTypes,
                owner,  // owner
                null,   // m
                skipVisibility, 
                false); // transparentMethod
        } 
 
        public DynamicMethod(string name,
                             MethodAttributes attributes, 
                             CallingConventions callingConvention,
                             Type returnType,
                             Type[] parameterTypes,
                             Type owner, 
                             bool skipVisibility) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller; 
            DynamicMethod.PerformSecurityCheck(owner, ref stackMark, skipVisibility); 
            Init(name,
                attributes, 
                callingConvention,
                returnType,
                parameterTypes,
                owner,  // owner 
                null,   // m
                skipVisibility, 
                false); // transparentMethod 
        }
 
        // helpers for intialization

        static private void CheckConsistency(MethodAttributes attributes, CallingConventions callingConvention) {
            // only static public for method attributes 
            if ((attributes & ~MethodAttributes.MemberAccessMask) != MethodAttributes.Static)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags")); 
            if ((attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public) 
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags"));
 
            // only standard or varargs supported
            if (callingConvention != CallingConventions.Standard && callingConvention != CallingConventions.VarArgs)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags"));
 
            // vararg is not supported at the moment
            if (callingConvention == CallingConventions.VarArgs) 
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags")); 
        }
 
        // We create a transparent assembly to host DynamicMethods. Since the assembly does not have any
        // non-public fields (or any fields at all), it is a safe anonymous assembly to host DynamicMethods
        static Module GetDynamicMethodsModule()
        { 
            if (s_anonymouslyHostedDynamicMethodsModule != null)
                return s_anonymouslyHostedDynamicMethodsModule; 
 
            lock (s_anonymouslyHostedDynamicMethodsModuleLock)
            { 
                if (s_anonymouslyHostedDynamicMethodsModule != null)
                    return s_anonymouslyHostedDynamicMethodsModule;

                ConstructorInfo ctor = typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes); 
                CustomAttributeBuilder transparencyAttribute = new CustomAttributeBuilder(ctor, new object[0]);
                CustomAttributeBuilder[] assemblyAttributes = new CustomAttributeBuilder[1]; 
                assemblyAttributes[0] = transparencyAttribute; 
                AssemblyName assemblyName = new AssemblyName("Anonymously Hosted DynamicMethods Assembly");
                AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run, assemblyAttributes); 
                AppDomain.CurrentDomain.PublishAnonymouslyHostedDynamicMethodsAssembly(assembly.InternalAssembly);

                s_anonymouslyHostedDynamicMethodsModule = assembly.ManifestModule;
            } 

            return s_anonymouslyHostedDynamicMethodsModule; 
        } 

        private unsafe void Init(String name, 
                                 MethodAttributes attributes,
                                 CallingConventions callingConvention,
                                 Type returnType,
                                 Type[] signature, 
                                 Type owner,
                                 Module m, 
                                 bool skipVisibility, 
                                 bool transparentMethod) {
            DynamicMethod.CheckConsistency(attributes, callingConvention); 

            // check and store the signature
            if (signature != null) {
                m_parameterTypes = new RuntimeType[signature.Length]; 
                for (int i = 0; i < signature.Length; i++) {
                    if (signature[i] == null) 
                        throw new ArgumentException(Environment.GetResourceString("Arg_InvalidTypeInSignature")); 
                    m_parameterTypes[i] = signature[i].UnderlyingSystemType as RuntimeType;
                    if (m_parameterTypes[i] == null || m_parameterTypes[i] == typeof(void)) 
                        throw new ArgumentException(Environment.GetResourceString("Arg_InvalidTypeInSignature"));
                }
            }
            else { 
                m_parameterTypes = new RuntimeType[0];
            } 
 
            // check and store the return value
            m_returnType = (returnType == null) ? ((RuntimeType)typeof(void)) : (returnType.UnderlyingSystemType as RuntimeType); 
            if (m_returnType == null || m_returnType.IsByRef)
                throw new NotSupportedException(Environment.GetResourceString("Arg_InvalidTypeInRetType"));

            if (transparentMethod) 
            {
                BCLDebug.Assert(owner == null && m == null, "owner and m cannot be set for transparent methods"); 
                m_module = GetDynamicMethodsModule().ModuleHandle; 
                if (skipVisibility)
                { 
                    m_restrictedSkipVisibility = true;
                    m_creationContext = CompressedStack.Capture();
                }
            } 
            else
            { 
                BCLDebug.Assert(m != null || owner != null, "PerformSecurityCheck should ensure that either m or owner is set"); 
                BCLDebug.Assert(m == null || m != s_anonymouslyHostedDynamicMethodsModule, "The user cannot explicitly use this assembly");
 
                m_typeOwner = (owner != null) ? owner.UnderlyingSystemType as RuntimeType : null;
                if (m_typeOwner != null)
                    if (m_typeOwner.HasElementType || m_typeOwner.ContainsGenericParameters
                        || m_typeOwner.IsGenericParameter || m_typeOwner.IsInterface) 
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidTypeForDynamicMethod"));
 
                m_module = (m != null) ? m.ModuleHandle : m_typeOwner.Module.ModuleHandle; 

                m_skipVisibility = skipVisibility; 
            }

            // initialize remaining fields
            m_ilGenerator = null; 
            m_fInitLocals = true;
            m_method = new RuntimeMethodHandle(null); 
 
            if (name == null)
                throw new ArgumentNullException("name"); 
            m_dynMethod = new RTDynamicMethod(this, name, attributes, callingConvention);
        }

        static private void PerformSecurityCheck(Module m, ref StackCrawlMark stackMark, bool skipVisibility) 
        {
            if (m == null) 
                throw new ArgumentNullException("m"); 

            // The user cannot explicitly use this assembly 
            if (m.Equals(s_anonymouslyHostedDynamicMethodsModule))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"), "m");

            // ask for member access if skip visibility 
            if (skipVisibility)
                new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand(); 
 
            // ask for control evidence if outside of the caller assembly
            RuntimeTypeHandle callingType = ModuleHandle.GetCallerType(ref stackMark); 
            if (!m.Assembly.AssemblyHandle.Equals(callingType.GetAssemblyHandle()))
            {
                // Demand the permissions of the assembly where the DynamicMethod will live
                PermissionSet granted, refused; 
                m.Assembly.nGetGrantSet( out granted, out refused );
 
                if (granted == null) 
                    granted = new PermissionSet(PermissionState.Unrestricted);
 
                CodeAccessSecurityEngine.ReflectionTargetDemandHelper(PermissionType.SecurityControlEvidence, granted);
            }
        }
 
        static private void PerformSecurityCheck(Type owner, ref StackCrawlMark stackMark, bool skipVisibility)
        { 
                if (owner == null || ((owner = owner.UnderlyingSystemType as RuntimeType) == null)) 
                    throw new ArgumentNullException("owner");
 
                // get the type the call is coming from
                RuntimeTypeHandle callingType = ModuleHandle.GetCallerType(ref stackMark);

                // ask for member access if skip visibility 
                if (skipVisibility)
                    new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand(); 
                else { 
                    // if the call is not coming from the same class ask for member access
                    if (!callingType.Equals(owner.TypeHandle)) 
                        new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
                }

                // ask for control evidence if outside of the caller module 
                if (!owner.Assembly.AssemblyHandle.Equals(callingType.GetAssemblyHandle()))
                { 
                    // Demand the permissions of the assembly where the DynamicMethod will live 
                    PermissionSet granted, refused;
                    owner.Assembly.nGetGrantSet(out granted, out refused); 

                    if (granted == null)
                        granted = new PermissionSet(PermissionState.Unrestricted);
 
                    CodeAccessSecurityEngine.ReflectionTargetDemandHelper(PermissionType.SecurityControlEvidence, granted);
                } 
        } 

        // 
        // Delegate and method creation
        //

        [System.Runtime.InteropServices.ComVisible(true)] 
        public Delegate CreateDelegate(Type delegateType) {
            if (m_restrictedSkipVisibility) 
            { 
                // Compile the method since accessibility checks are done as part of compilation.
                System.Runtime.CompilerServices.RuntimeHelpers._CompileMethod(GetMethodDescriptor().Value); 
            }

            MulticastDelegate d = (MulticastDelegate)Delegate.CreateDelegate(delegateType, null, GetMethodDescriptor());
            // stash this MethodInfo by brute force. 
            d.StoreDynamicMethod(GetMethodInfo());
            return d; 
        } 

        [System.Runtime.InteropServices.ComVisible(true)] 
        public Delegate CreateDelegate(Type delegateType, Object target) {
            if (m_restrictedSkipVisibility)
            {
                // Compile the method since accessibility checks are done as part of compilation 
                System.Runtime.CompilerServices.RuntimeHelpers._CompileMethod(GetMethodDescriptor().Value);
            } 
 
            MulticastDelegate d = (MulticastDelegate)Delegate.CreateDelegate(delegateType, target, GetMethodDescriptor());
            // stash this MethodInfo by brute force. 
            d.StoreDynamicMethod(GetMethodInfo());
            return d;
        }
 
        internal unsafe RuntimeMethodHandle GetMethodDescriptor() {
            if (m_method.IsNullHandle()) { 
                lock (this) { 
                    if (m_method.IsNullHandle()) {
                        if (m_DynamicILInfo != null) 
                            m_method = m_DynamicILInfo.GetCallableMethod(m_module.Value);
                        else {
                            if (m_ilGenerator == null || m_ilGenerator.m_length == 0)
                                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("InvalidOperation_BadEmptyMethodBody"), Name)); 

                            m_method = m_ilGenerator.GetCallableMethod(m_module.Value); 
                        } 
                    }
                } 
            }
            return m_method;
        }
 
        //
        // MethodInfo api. They mostly forward to RTDynamicMethod 
        // 

        public override String ToString() { return m_dynMethod.ToString(); } 

        public override String Name { get { return m_dynMethod.Name; } }

        public override Type DeclaringType { get { return m_dynMethod.DeclaringType; } } 

        public override Type ReflectedType { get { return m_dynMethod.ReflectedType; } } 
 
        internal override int MetadataTokenInternal { get { return m_dynMethod.MetadataTokenInternal; } }
 
        public override Module Module { get { return m_dynMethod.Module; } }

        // we cannot return a MethodHandle because we cannot track it via GC so this method is off limits
        public override RuntimeMethodHandle MethodHandle { get { throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInDynamicMethod")); } } 

        public override MethodAttributes Attributes { get { return m_dynMethod.Attributes; } } 
 
        public override CallingConventions CallingConvention { get { return m_dynMethod.CallingConvention; } }
 
        public override MethodInfo GetBaseDefinition() { return this; }

        public override ParameterInfo[] GetParameters() { return m_dynMethod.GetParameters(); }
 
        public override MethodImplAttributes GetMethodImplementationFlags() { return m_dynMethod.GetMethodImplementationFlags(); }
 
        public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture) { 
            //
            // We do not demand any permission here because the caller already has access 
            // to the current DynamicMethod object, and it could just as easily emit another
            // Transparent DynamicMethod to call the current DynamicMethod.
            //
 
            RuntimeMethodHandle method = GetMethodDescriptor();
            // ignore obj since it's a static method 
 
            if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_CallToVarArg")); 

            // create a signature object
            RuntimeTypeHandle[] argumentHandles = new RuntimeTypeHandle[m_parameterTypes.Length];
            for (int i = 0; i < argumentHandles.Length; i++) 
                argumentHandles[i] = m_parameterTypes[i].TypeHandle;
            Signature sig = new Signature( 
                method, argumentHandles, m_returnType.TypeHandle, CallingConvention); 

 
            // verify arguments
            int formalCount = sig.Arguments.Length;
            int actualCount = (parameters != null) ? parameters.Length : 0;
            if (formalCount != actualCount) 
                throw new TargetParameterCountException(Environment.GetResourceString("Arg_ParmCnt"));
 
            // if we are here we passed all the previous checks. Time to look at the arguments 
            Object retValue = null;
            if (actualCount > 0) 
            {
                Object[] arguments = CheckArguments(parameters, binder, invokeAttr, culture, sig);
                retValue = method.InvokeMethodFast(null, arguments, sig, Attributes, RuntimeTypeHandle.EmptyHandle);
                // copy out. This should be made only if ByRef are present. 
                for (int index = 0; index < actualCount; index++)
                    parameters[index] = arguments[index]; 
            } 
            else
            { 
                retValue = method.InvokeMethodFast(null, null, sig, Attributes, RuntimeTypeHandle.EmptyHandle);
            }

            GC.KeepAlive(this); 
            return retValue;
        } 
 
        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        { 
            return m_dynMethod.GetCustomAttributes(attributeType, inherit);
        }

        public override Object[] GetCustomAttributes(bool inherit) { return m_dynMethod.GetCustomAttributes(inherit); } 

        public override bool IsDefined(Type attributeType, bool inherit) { return m_dynMethod.IsDefined(attributeType, inherit); } 
 
        public override Type ReturnType { get { return m_dynMethod.ReturnType; } }
 
        public override ParameterInfo ReturnParameter { get { return m_dynMethod.ReturnParameter; } }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes { get { return m_dynMethod.ReturnTypeCustomAttributes; } }
 
        internal override bool IsOverloaded { get { return m_dynMethod.IsOverloaded; } }
 
        // 
        // DynamicMethod specific methods
        // 

        public ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, String parameterName) {
            if (position < 0 || position > m_parameterTypes.Length)
                throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_ParamSequence")); 
            position--; // it's 1 based. 0 is the return value
 
            if (position >= 0) { 
                ParameterInfo[] parameters = m_dynMethod.LoadParameters();
                parameters[position].SetName(parameterName); 
                parameters[position].SetAttributes(attributes);
            }
            return null;
        } 

        public DynamicILInfo GetDynamicILInfo() 
        { 
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
 
            if (m_DynamicILInfo != null)
                return m_DynamicILInfo;

            return GetDynamicILInfo(new DynamicScope()); 
        }
 
        internal DynamicILInfo GetDynamicILInfo(DynamicScope scope) 
        {
            if (m_DynamicILInfo == null) 
            {
                byte[] methodSignature = SignatureHelper.GetMethodSigHelper(
                        null, CallingConvention, ReturnType, null, null, m_parameterTypes, null, null).GetSignature(true);
                m_DynamicILInfo = new DynamicILInfo(scope, this, methodSignature); 
            }
 
            return m_DynamicILInfo; 
        }
 
    	public ILGenerator GetILGenerator() {
            return GetILGenerator(64);
        }
 
       public ILGenerator GetILGenerator(int streamSize)
        { 
            if (m_ilGenerator == null) 
            {
                byte[] methodSignature = SignatureHelper.GetMethodSigHelper( 
                    null, CallingConvention, ReturnType, null, null, m_parameterTypes, null, null).GetSignature(true);
                m_ilGenerator = new DynamicILGenerator(this, methodSignature, streamSize);
            }
    		return m_ilGenerator; 
    	}
 
    	public bool InitLocals { 
    		get {return m_fInitLocals;}
    		set {m_fInitLocals = value;} 
    	}

        //
        // Internal API 
        //
 
        internal MethodInfo GetMethodInfo() { 
            return m_dynMethod;
        } 

        //////////////////////////////////////////////////////////////////////////////////////////////
        // RTDynamicMethod
        // 
        // this is actually the real runtime instance of a method info that gets used for invocation
        // We need this so we never leak the DynamicMethod out via an exception. 
        // This way the DynamicMethod creator is the only one responsible for DynamicMethod access, 
        // and can control exactly who gets access to it.
        // 
        internal class RTDynamicMethod : MethodInfo {

            internal DynamicMethod m_owner;
            ParameterInfo[] m_parameters; 
            String m_name;
            MethodAttributes m_attributes; 
            CallingConventions m_callingConvention; 

            // 
            // ctors
            //
            private RTDynamicMethod() {}
 
            internal RTDynamicMethod(DynamicMethod owner, String name, MethodAttributes attributes, CallingConventions callingConvention) {
                m_owner = owner; 
                m_name = name; 
                m_attributes = attributes;
                m_callingConvention = callingConvention; 
            }

            //
            // MethodInfo api 
            //
            public override String ToString() { 
                return ReturnType.SigToString() + " " + RuntimeMethodInfo.ConstructName(this); 
            }
 
            public override String Name {
                get { return m_name; }
            }
 
            public override Type DeclaringType {
                get { return null; } 
            } 

            public override Type ReflectedType { 
                get { return null; }
            }

            internal override int MetadataTokenInternal { 
                get { return 0; }
            } 
 
            public override Module Module {
                get { return m_owner.m_module.GetModule(); } 
            }

            public override RuntimeMethodHandle MethodHandle {
                get { throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInDynamicMethod")); } 
            }
 
            public override MethodAttributes Attributes { 
                get { return m_attributes; }
            } 

            public override CallingConventions CallingConvention {
                get { return m_callingConvention; }
            } 

            public override MethodInfo GetBaseDefinition() { 
                return this; 
            }
 
            public override ParameterInfo[] GetParameters() {
                ParameterInfo[] privateParameters = LoadParameters();
                ParameterInfo[] parameters = new ParameterInfo[privateParameters.Length];
                Array.Copy(privateParameters, parameters, privateParameters.Length); 
                return parameters;
            } 
 
            public override MethodImplAttributes GetMethodImplementationFlags() {
                return MethodImplAttributes.IL | MethodImplAttributes.NoInlining; 
            }

            public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture) {
                // We want the creator of the DynamicMethod to control who has access to the 
                // DynamicMethod (just like we do for delegates). However, a user can get to
                // the corresponding RTDynamicMethod using Exception.TargetSite, StackFrame.GetMethod, etc. 
                // If we allowed use of RTDynamicMethod, the creator of the DynamicMethod would 
                // not be able to bound access to the DynamicMethod. Hence, we do not allow
                // direct use of RTDynamicMethod. 
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"), "this");
            }

            public override Object[] GetCustomAttributes(Type attributeType, bool inherit) { 
                if (attributeType == null)
                    throw new ArgumentNullException("attributeType"); 
 
                if (attributeType.IsAssignableFrom(typeof(MethodImplAttribute)))
                    return new Object[] { new MethodImplAttribute(GetMethodImplementationFlags()) }; 
                else
                    return new Object[0];
            }
 
            public override Object[] GetCustomAttributes(bool inherit) {
                // support for MethodImplAttribute PCA 
                return new Object[] { new MethodImplAttribute(GetMethodImplementationFlags()) }; 
            }
 
            public override bool IsDefined(Type attributeType, bool inherit) {
                if (attributeType == null)
                    throw new ArgumentNullException("attributeType");
 
                if (attributeType.IsAssignableFrom(typeof(MethodImplAttribute)))
                    return true; 
                else 
                    return false;
            } 

            internal override Type GetReturnType() {
                return m_owner.m_returnType;
            } 

            public override ParameterInfo ReturnParameter { 
                get { return null; } 
            }
 
            public override ICustomAttributeProvider ReturnTypeCustomAttributes {
                get { return GetEmptyCAHolder(); }
            }
 
            internal override bool IsOverloaded {
                get { return false; } 
            } 

            // 
            // private implementation
            //

            internal ParameterInfo[] LoadParameters() { 
                if (m_parameters == null) {
                    RuntimeType[] parameterTypes = m_owner.m_parameterTypes; 
                    ParameterInfo[] parameters = new ParameterInfo[parameterTypes.Length]; 
                    for (int i = 0; i < parameterTypes.Length; i++)
                        parameters[i] = new ParameterInfo(this, null, parameterTypes[i], i); 
                    if (m_parameters == null)
                        // should we interlockexchange?
                        m_parameters = parameters;
                } 
                return m_parameters;
            } 
 
            // private implementation of CA for the return type
            private ICustomAttributeProvider GetEmptyCAHolder() { 
                return new EmptyCAHolder();
            }

            /////////////////////////////////////////////////// 
            // EmptyCAHolder
            private class EmptyCAHolder : ICustomAttributeProvider { 
                internal EmptyCAHolder() {} 

                Object[] ICustomAttributeProvider.GetCustomAttributes(Type attributeType, bool inherit) { 
                    return new Object[0];
                }

                Object[] ICustomAttributeProvider.GetCustomAttributes(bool inherit) { 
                    return new Object[0];
                } 
 
                bool ICustomAttributeProvider.IsDefined (Type attributeType, bool inherit) {
                    return false; 
                }
            }

        } 

    } 
 
}
 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

namespace System.Reflection.Emit 
{ 
    using System;
    using CultureInfo = System.Globalization.CultureInfo; 
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading; 
    using System.Runtime.CompilerServices;
 
[System.Runtime.InteropServices.ComVisible(true)] 
    public sealed class DynamicMethod : MethodInfo
    { 
        RuntimeType[] m_parameterTypes;
        RuntimeType m_returnType;
        DynamicILGenerator m_ilGenerator;
        DynamicILInfo m_DynamicILInfo; 
        bool m_fInitLocals;
        internal RuntimeMethodHandle m_method; 
        internal ModuleHandle m_module; 
        internal bool m_skipVisibility;
        internal RuntimeType m_typeOwner; // can be null 

        // We want the creator of the DynamicMethod to control who has access to the
        // DynamicMethod (just like we do for delegates). However, a user can get to
        // the corresponding RTDynamicMethod using Exception.TargetSite, StackFrame.GetMethod, etc. 
        // If we allowed use of RTDynamicMethod, the creator of the DynamicMethod would
        // not be able to bound access to the DynamicMethod. Hence, we need to ensure that 
        // we do not allow direct use of RTDynamicMethod. 
        RTDynamicMethod m_dynMethod;
 
        // needed to keep the object alive during jitting
        // assigned by the DynamicResolver ctor
        internal DynamicResolver m_resolver;
 
        internal bool m_restrictedSkipVisibility;
        // The context when the method was created. We use this to do the RestrictedMemberAccess checks. 
        // These checks are done when the method is compiled. This can happen at an arbitrary time, 
        // when CreateDelegate or Invoke is called, or when another DynamicMethod executes OpCodes.Call.
        // We capture the creation context so that we can do the checks against the same context, 
        // irrespective of when the method gets compiled. Note that the DynamicMethod does not know when
        // it is ready for use since there is not API which indictates that IL generation has completed.
        internal CompressedStack m_creationContext;
        private static Module s_anonymouslyHostedDynamicMethodsModule; 
        private static readonly object s_anonymouslyHostedDynamicMethodsModuleLock = new object();
 
        // 
        // class initialization (ctor and init)
        // 

        private DynamicMethod() { }

        public DynamicMethod(string name, 
                             Type returnType,
                             Type[] parameterTypes) 
        { 
            Init(name,
                MethodAttributes.Public | MethodAttributes.Static, 
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                null,   // owner 
                null,   // m
                false,  // skipVisibility 
                true);  // transparentMethod 
        }
 
        public DynamicMethod(string name,
                             Type returnType,
                             Type[] parameterTypes,
                             bool restrictedSkipVisibility) 
        {
            Init(name, 
                MethodAttributes.Public | MethodAttributes.Static, 
                CallingConventions.Standard,
                returnType, 
                parameterTypes,
                null,   // owner
                null,   // m
                restrictedSkipVisibility, 
                true);  // transparentMethod
        } 
 
        public DynamicMethod(string name,
                             Type returnType, 
                             Type[] parameterTypes,
                             Module m) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            DynamicMethod.PerformSecurityCheck(m, ref stackMark, false); 
            Init(name,
                MethodAttributes.Public | MethodAttributes.Static, 
                CallingConventions.Standard, 
                returnType,
                parameterTypes, 
                null,   // owner
                m,      // m
                false,  // skipVisibility
                false);  // transparentMethod 
        }
 
        public DynamicMethod(string name, 
                             Type returnType,
                             Type[] parameterTypes, 
                             Module m,
                             bool skipVisibility) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            DynamicMethod.PerformSecurityCheck(m, ref stackMark, skipVisibility); 
            Init(name,
                MethodAttributes.Public | MethodAttributes.Static, 
                CallingConventions.Standard, 
                returnType,
                parameterTypes, 
                null,   // owner
                m,      // m
                skipVisibility,
                false); // transparentMethod 
        }
 
        public DynamicMethod(string name, 
                             MethodAttributes attributes,
                             CallingConventions callingConvention, 
                             Type returnType,
                             Type[] parameterTypes,
                             Module m,
                             bool skipVisibility) { 
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            DynamicMethod.PerformSecurityCheck(m, ref stackMark, skipVisibility); 
            Init(name, 
                attributes,
                callingConvention, 
                returnType,
                parameterTypes,
                null,   // owner
                m,      // m 
                skipVisibility,
                false); // transparentMethod 
        } 

        public DynamicMethod(string name, 
                             Type returnType,
                             Type[] parameterTypes,
                             Type owner) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller; 
            DynamicMethod.PerformSecurityCheck(owner, ref stackMark, false);
            Init(name, 
                MethodAttributes.Public | MethodAttributes.Static, 
                CallingConventions.Standard,
                returnType, 
                parameterTypes,
                owner,  // owner
                null,   // m
                false,  // skipVisibility 
                false); // transparentMethod
        } 
 
        public DynamicMethod(string name,
                             Type returnType, 
                             Type[] parameterTypes,
                             Type owner,
                             bool skipVisibility) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller; 
            DynamicMethod.PerformSecurityCheck(owner, ref stackMark, skipVisibility);
            Init(name, 
                MethodAttributes.Public | MethodAttributes.Static, 
                CallingConventions.Standard,
                returnType, 
                parameterTypes,
                owner,  // owner
                null,   // m
                skipVisibility, 
                false); // transparentMethod
        } 
 
        public DynamicMethod(string name,
                             MethodAttributes attributes, 
                             CallingConventions callingConvention,
                             Type returnType,
                             Type[] parameterTypes,
                             Type owner, 
                             bool skipVisibility) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller; 
            DynamicMethod.PerformSecurityCheck(owner, ref stackMark, skipVisibility); 
            Init(name,
                attributes, 
                callingConvention,
                returnType,
                parameterTypes,
                owner,  // owner 
                null,   // m
                skipVisibility, 
                false); // transparentMethod 
        }
 
        // helpers for intialization

        static private void CheckConsistency(MethodAttributes attributes, CallingConventions callingConvention) {
            // only static public for method attributes 
            if ((attributes & ~MethodAttributes.MemberAccessMask) != MethodAttributes.Static)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags")); 
            if ((attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public) 
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags"));
 
            // only standard or varargs supported
            if (callingConvention != CallingConventions.Standard && callingConvention != CallingConventions.VarArgs)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags"));
 
            // vararg is not supported at the moment
            if (callingConvention == CallingConventions.VarArgs) 
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags")); 
        }
 
        // We create a transparent assembly to host DynamicMethods. Since the assembly does not have any
        // non-public fields (or any fields at all), it is a safe anonymous assembly to host DynamicMethods
        static Module GetDynamicMethodsModule()
        { 
            if (s_anonymouslyHostedDynamicMethodsModule != null)
                return s_anonymouslyHostedDynamicMethodsModule; 
 
            lock (s_anonymouslyHostedDynamicMethodsModuleLock)
            { 
                if (s_anonymouslyHostedDynamicMethodsModule != null)
                    return s_anonymouslyHostedDynamicMethodsModule;

                ConstructorInfo ctor = typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes); 
                CustomAttributeBuilder transparencyAttribute = new CustomAttributeBuilder(ctor, new object[0]);
                CustomAttributeBuilder[] assemblyAttributes = new CustomAttributeBuilder[1]; 
                assemblyAttributes[0] = transparencyAttribute; 
                AssemblyName assemblyName = new AssemblyName("Anonymously Hosted DynamicMethods Assembly");
                AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run, assemblyAttributes); 
                AppDomain.CurrentDomain.PublishAnonymouslyHostedDynamicMethodsAssembly(assembly.InternalAssembly);

                s_anonymouslyHostedDynamicMethodsModule = assembly.ManifestModule;
            } 

            return s_anonymouslyHostedDynamicMethodsModule; 
        } 

        private unsafe void Init(String name, 
                                 MethodAttributes attributes,
                                 CallingConventions callingConvention,
                                 Type returnType,
                                 Type[] signature, 
                                 Type owner,
                                 Module m, 
                                 bool skipVisibility, 
                                 bool transparentMethod) {
            DynamicMethod.CheckConsistency(attributes, callingConvention); 

            // check and store the signature
            if (signature != null) {
                m_parameterTypes = new RuntimeType[signature.Length]; 
                for (int i = 0; i < signature.Length; i++) {
                    if (signature[i] == null) 
                        throw new ArgumentException(Environment.GetResourceString("Arg_InvalidTypeInSignature")); 
                    m_parameterTypes[i] = signature[i].UnderlyingSystemType as RuntimeType;
                    if (m_parameterTypes[i] == null || m_parameterTypes[i] == typeof(void)) 
                        throw new ArgumentException(Environment.GetResourceString("Arg_InvalidTypeInSignature"));
                }
            }
            else { 
                m_parameterTypes = new RuntimeType[0];
            } 
 
            // check and store the return value
            m_returnType = (returnType == null) ? ((RuntimeType)typeof(void)) : (returnType.UnderlyingSystemType as RuntimeType); 
            if (m_returnType == null || m_returnType.IsByRef)
                throw new NotSupportedException(Environment.GetResourceString("Arg_InvalidTypeInRetType"));

            if (transparentMethod) 
            {
                BCLDebug.Assert(owner == null && m == null, "owner and m cannot be set for transparent methods"); 
                m_module = GetDynamicMethodsModule().ModuleHandle; 
                if (skipVisibility)
                { 
                    m_restrictedSkipVisibility = true;
                    m_creationContext = CompressedStack.Capture();
                }
            } 
            else
            { 
                BCLDebug.Assert(m != null || owner != null, "PerformSecurityCheck should ensure that either m or owner is set"); 
                BCLDebug.Assert(m == null || m != s_anonymouslyHostedDynamicMethodsModule, "The user cannot explicitly use this assembly");
 
                m_typeOwner = (owner != null) ? owner.UnderlyingSystemType as RuntimeType : null;
                if (m_typeOwner != null)
                    if (m_typeOwner.HasElementType || m_typeOwner.ContainsGenericParameters
                        || m_typeOwner.IsGenericParameter || m_typeOwner.IsInterface) 
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidTypeForDynamicMethod"));
 
                m_module = (m != null) ? m.ModuleHandle : m_typeOwner.Module.ModuleHandle; 

                m_skipVisibility = skipVisibility; 
            }

            // initialize remaining fields
            m_ilGenerator = null; 
            m_fInitLocals = true;
            m_method = new RuntimeMethodHandle(null); 
 
            if (name == null)
                throw new ArgumentNullException("name"); 
            m_dynMethod = new RTDynamicMethod(this, name, attributes, callingConvention);
        }

        static private void PerformSecurityCheck(Module m, ref StackCrawlMark stackMark, bool skipVisibility) 
        {
            if (m == null) 
                throw new ArgumentNullException("m"); 

            // The user cannot explicitly use this assembly 
            if (m.Equals(s_anonymouslyHostedDynamicMethodsModule))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"), "m");

            // ask for member access if skip visibility 
            if (skipVisibility)
                new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand(); 
 
            // ask for control evidence if outside of the caller assembly
            RuntimeTypeHandle callingType = ModuleHandle.GetCallerType(ref stackMark); 
            if (!m.Assembly.AssemblyHandle.Equals(callingType.GetAssemblyHandle()))
            {
                // Demand the permissions of the assembly where the DynamicMethod will live
                PermissionSet granted, refused; 
                m.Assembly.nGetGrantSet( out granted, out refused );
 
                if (granted == null) 
                    granted = new PermissionSet(PermissionState.Unrestricted);
 
                CodeAccessSecurityEngine.ReflectionTargetDemandHelper(PermissionType.SecurityControlEvidence, granted);
            }
        }
 
        static private void PerformSecurityCheck(Type owner, ref StackCrawlMark stackMark, bool skipVisibility)
        { 
                if (owner == null || ((owner = owner.UnderlyingSystemType as RuntimeType) == null)) 
                    throw new ArgumentNullException("owner");
 
                // get the type the call is coming from
                RuntimeTypeHandle callingType = ModuleHandle.GetCallerType(ref stackMark);

                // ask for member access if skip visibility 
                if (skipVisibility)
                    new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand(); 
                else { 
                    // if the call is not coming from the same class ask for member access
                    if (!callingType.Equals(owner.TypeHandle)) 
                        new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
                }

                // ask for control evidence if outside of the caller module 
                if (!owner.Assembly.AssemblyHandle.Equals(callingType.GetAssemblyHandle()))
                { 
                    // Demand the permissions of the assembly where the DynamicMethod will live 
                    PermissionSet granted, refused;
                    owner.Assembly.nGetGrantSet(out granted, out refused); 

                    if (granted == null)
                        granted = new PermissionSet(PermissionState.Unrestricted);
 
                    CodeAccessSecurityEngine.ReflectionTargetDemandHelper(PermissionType.SecurityControlEvidence, granted);
                } 
        } 

        // 
        // Delegate and method creation
        //

        [System.Runtime.InteropServices.ComVisible(true)] 
        public Delegate CreateDelegate(Type delegateType) {
            if (m_restrictedSkipVisibility) 
            { 
                // Compile the method since accessibility checks are done as part of compilation.
                System.Runtime.CompilerServices.RuntimeHelpers._CompileMethod(GetMethodDescriptor().Value); 
            }

            MulticastDelegate d = (MulticastDelegate)Delegate.CreateDelegate(delegateType, null, GetMethodDescriptor());
            // stash this MethodInfo by brute force. 
            d.StoreDynamicMethod(GetMethodInfo());
            return d; 
        } 

        [System.Runtime.InteropServices.ComVisible(true)] 
        public Delegate CreateDelegate(Type delegateType, Object target) {
            if (m_restrictedSkipVisibility)
            {
                // Compile the method since accessibility checks are done as part of compilation 
                System.Runtime.CompilerServices.RuntimeHelpers._CompileMethod(GetMethodDescriptor().Value);
            } 
 
            MulticastDelegate d = (MulticastDelegate)Delegate.CreateDelegate(delegateType, target, GetMethodDescriptor());
            // stash this MethodInfo by brute force. 
            d.StoreDynamicMethod(GetMethodInfo());
            return d;
        }
 
        internal unsafe RuntimeMethodHandle GetMethodDescriptor() {
            if (m_method.IsNullHandle()) { 
                lock (this) { 
                    if (m_method.IsNullHandle()) {
                        if (m_DynamicILInfo != null) 
                            m_method = m_DynamicILInfo.GetCallableMethod(m_module.Value);
                        else {
                            if (m_ilGenerator == null || m_ilGenerator.m_length == 0)
                                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("InvalidOperation_BadEmptyMethodBody"), Name)); 

                            m_method = m_ilGenerator.GetCallableMethod(m_module.Value); 
                        } 
                    }
                } 
            }
            return m_method;
        }
 
        //
        // MethodInfo api. They mostly forward to RTDynamicMethod 
        // 

        public override String ToString() { return m_dynMethod.ToString(); } 

        public override String Name { get { return m_dynMethod.Name; } }

        public override Type DeclaringType { get { return m_dynMethod.DeclaringType; } } 

        public override Type ReflectedType { get { return m_dynMethod.ReflectedType; } } 
 
        internal override int MetadataTokenInternal { get { return m_dynMethod.MetadataTokenInternal; } }
 
        public override Module Module { get { return m_dynMethod.Module; } }

        // we cannot return a MethodHandle because we cannot track it via GC so this method is off limits
        public override RuntimeMethodHandle MethodHandle { get { throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInDynamicMethod")); } } 

        public override MethodAttributes Attributes { get { return m_dynMethod.Attributes; } } 
 
        public override CallingConventions CallingConvention { get { return m_dynMethod.CallingConvention; } }
 
        public override MethodInfo GetBaseDefinition() { return this; }

        public override ParameterInfo[] GetParameters() { return m_dynMethod.GetParameters(); }
 
        public override MethodImplAttributes GetMethodImplementationFlags() { return m_dynMethod.GetMethodImplementationFlags(); }
 
        public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture) { 
            //
            // We do not demand any permission here because the caller already has access 
            // to the current DynamicMethod object, and it could just as easily emit another
            // Transparent DynamicMethod to call the current DynamicMethod.
            //
 
            RuntimeMethodHandle method = GetMethodDescriptor();
            // ignore obj since it's a static method 
 
            if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_CallToVarArg")); 

            // create a signature object
            RuntimeTypeHandle[] argumentHandles = new RuntimeTypeHandle[m_parameterTypes.Length];
            for (int i = 0; i < argumentHandles.Length; i++) 
                argumentHandles[i] = m_parameterTypes[i].TypeHandle;
            Signature sig = new Signature( 
                method, argumentHandles, m_returnType.TypeHandle, CallingConvention); 

 
            // verify arguments
            int formalCount = sig.Arguments.Length;
            int actualCount = (parameters != null) ? parameters.Length : 0;
            if (formalCount != actualCount) 
                throw new TargetParameterCountException(Environment.GetResourceString("Arg_ParmCnt"));
 
            // if we are here we passed all the previous checks. Time to look at the arguments 
            Object retValue = null;
            if (actualCount > 0) 
            {
                Object[] arguments = CheckArguments(parameters, binder, invokeAttr, culture, sig);
                retValue = method.InvokeMethodFast(null, arguments, sig, Attributes, RuntimeTypeHandle.EmptyHandle);
                // copy out. This should be made only if ByRef are present. 
                for (int index = 0; index < actualCount; index++)
                    parameters[index] = arguments[index]; 
            } 
            else
            { 
                retValue = method.InvokeMethodFast(null, null, sig, Attributes, RuntimeTypeHandle.EmptyHandle);
            }

            GC.KeepAlive(this); 
            return retValue;
        } 
 
        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        { 
            return m_dynMethod.GetCustomAttributes(attributeType, inherit);
        }

        public override Object[] GetCustomAttributes(bool inherit) { return m_dynMethod.GetCustomAttributes(inherit); } 

        public override bool IsDefined(Type attributeType, bool inherit) { return m_dynMethod.IsDefined(attributeType, inherit); } 
 
        public override Type ReturnType { get { return m_dynMethod.ReturnType; } }
 
        public override ParameterInfo ReturnParameter { get { return m_dynMethod.ReturnParameter; } }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes { get { return m_dynMethod.ReturnTypeCustomAttributes; } }
 
        internal override bool IsOverloaded { get { return m_dynMethod.IsOverloaded; } }
 
        // 
        // DynamicMethod specific methods
        // 

        public ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, String parameterName) {
            if (position < 0 || position > m_parameterTypes.Length)
                throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_ParamSequence")); 
            position--; // it's 1 based. 0 is the return value
 
            if (position >= 0) { 
                ParameterInfo[] parameters = m_dynMethod.LoadParameters();
                parameters[position].SetName(parameterName); 
                parameters[position].SetAttributes(attributes);
            }
            return null;
        } 

        public DynamicILInfo GetDynamicILInfo() 
        { 
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
 
            if (m_DynamicILInfo != null)
                return m_DynamicILInfo;

            return GetDynamicILInfo(new DynamicScope()); 
        }
 
        internal DynamicILInfo GetDynamicILInfo(DynamicScope scope) 
        {
            if (m_DynamicILInfo == null) 
            {
                byte[] methodSignature = SignatureHelper.GetMethodSigHelper(
                        null, CallingConvention, ReturnType, null, null, m_parameterTypes, null, null).GetSignature(true);
                m_DynamicILInfo = new DynamicILInfo(scope, this, methodSignature); 
            }
 
            return m_DynamicILInfo; 
        }
 
    	public ILGenerator GetILGenerator() {
            return GetILGenerator(64);
        }
 
       public ILGenerator GetILGenerator(int streamSize)
        { 
            if (m_ilGenerator == null) 
            {
                byte[] methodSignature = SignatureHelper.GetMethodSigHelper( 
                    null, CallingConvention, ReturnType, null, null, m_parameterTypes, null, null).GetSignature(true);
                m_ilGenerator = new DynamicILGenerator(this, methodSignature, streamSize);
            }
    		return m_ilGenerator; 
    	}
 
    	public bool InitLocals { 
    		get {return m_fInitLocals;}
    		set {m_fInitLocals = value;} 
    	}

        //
        // Internal API 
        //
 
        internal MethodInfo GetMethodInfo() { 
            return m_dynMethod;
        } 

        //////////////////////////////////////////////////////////////////////////////////////////////
        // RTDynamicMethod
        // 
        // this is actually the real runtime instance of a method info that gets used for invocation
        // We need this so we never leak the DynamicMethod out via an exception. 
        // This way the DynamicMethod creator is the only one responsible for DynamicMethod access, 
        // and can control exactly who gets access to it.
        // 
        internal class RTDynamicMethod : MethodInfo {

            internal DynamicMethod m_owner;
            ParameterInfo[] m_parameters; 
            String m_name;
            MethodAttributes m_attributes; 
            CallingConventions m_callingConvention; 

            // 
            // ctors
            //
            private RTDynamicMethod() {}
 
            internal RTDynamicMethod(DynamicMethod owner, String name, MethodAttributes attributes, CallingConventions callingConvention) {
                m_owner = owner; 
                m_name = name; 
                m_attributes = attributes;
                m_callingConvention = callingConvention; 
            }

            //
            // MethodInfo api 
            //
            public override String ToString() { 
                return ReturnType.SigToString() + " " + RuntimeMethodInfo.ConstructName(this); 
            }
 
            public override String Name {
                get { return m_name; }
            }
 
            public override Type DeclaringType {
                get { return null; } 
            } 

            public override Type ReflectedType { 
                get { return null; }
            }

            internal override int MetadataTokenInternal { 
                get { return 0; }
            } 
 
            public override Module Module {
                get { return m_owner.m_module.GetModule(); } 
            }

            public override RuntimeMethodHandle MethodHandle {
                get { throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInDynamicMethod")); } 
            }
 
            public override MethodAttributes Attributes { 
                get { return m_attributes; }
            } 

            public override CallingConventions CallingConvention {
                get { return m_callingConvention; }
            } 

            public override MethodInfo GetBaseDefinition() { 
                return this; 
            }
 
            public override ParameterInfo[] GetParameters() {
                ParameterInfo[] privateParameters = LoadParameters();
                ParameterInfo[] parameters = new ParameterInfo[privateParameters.Length];
                Array.Copy(privateParameters, parameters, privateParameters.Length); 
                return parameters;
            } 
 
            public override MethodImplAttributes GetMethodImplementationFlags() {
                return MethodImplAttributes.IL | MethodImplAttributes.NoInlining; 
            }

            public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture) {
                // We want the creator of the DynamicMethod to control who has access to the 
                // DynamicMethod (just like we do for delegates). However, a user can get to
                // the corresponding RTDynamicMethod using Exception.TargetSite, StackFrame.GetMethod, etc. 
                // If we allowed use of RTDynamicMethod, the creator of the DynamicMethod would 
                // not be able to bound access to the DynamicMethod. Hence, we do not allow
                // direct use of RTDynamicMethod. 
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"), "this");
            }

            public override Object[] GetCustomAttributes(Type attributeType, bool inherit) { 
                if (attributeType == null)
                    throw new ArgumentNullException("attributeType"); 
 
                if (attributeType.IsAssignableFrom(typeof(MethodImplAttribute)))
                    return new Object[] { new MethodImplAttribute(GetMethodImplementationFlags()) }; 
                else
                    return new Object[0];
            }
 
            public override Object[] GetCustomAttributes(bool inherit) {
                // support for MethodImplAttribute PCA 
                return new Object[] { new MethodImplAttribute(GetMethodImplementationFlags()) }; 
            }
 
            public override bool IsDefined(Type attributeType, bool inherit) {
                if (attributeType == null)
                    throw new ArgumentNullException("attributeType");
 
                if (attributeType.IsAssignableFrom(typeof(MethodImplAttribute)))
                    return true; 
                else 
                    return false;
            } 

            internal override Type GetReturnType() {
                return m_owner.m_returnType;
            } 

            public override ParameterInfo ReturnParameter { 
                get { return null; } 
            }
 
            public override ICustomAttributeProvider ReturnTypeCustomAttributes {
                get { return GetEmptyCAHolder(); }
            }
 
            internal override bool IsOverloaded {
                get { return false; } 
            } 

            // 
            // private implementation
            //

            internal ParameterInfo[] LoadParameters() { 
                if (m_parameters == null) {
                    RuntimeType[] parameterTypes = m_owner.m_parameterTypes; 
                    ParameterInfo[] parameters = new ParameterInfo[parameterTypes.Length]; 
                    for (int i = 0; i < parameterTypes.Length; i++)
                        parameters[i] = new ParameterInfo(this, null, parameterTypes[i], i); 
                    if (m_parameters == null)
                        // should we interlockexchange?
                        m_parameters = parameters;
                } 
                return m_parameters;
            } 
 
            // private implementation of CA for the return type
            private ICustomAttributeProvider GetEmptyCAHolder() { 
                return new EmptyCAHolder();
            }

            /////////////////////////////////////////////////// 
            // EmptyCAHolder
            private class EmptyCAHolder : ICustomAttributeProvider { 
                internal EmptyCAHolder() {} 

                Object[] ICustomAttributeProvider.GetCustomAttributes(Type attributeType, bool inherit) { 
                    return new Object[0];
                }

                Object[] ICustomAttributeProvider.GetCustomAttributes(bool inherit) { 
                    return new Object[0];
                } 
 
                bool ICustomAttributeProvider.IsDefined (Type attributeType, bool inherit) {
                    return false; 
                }
            }

        } 

    } 
 
}
 
