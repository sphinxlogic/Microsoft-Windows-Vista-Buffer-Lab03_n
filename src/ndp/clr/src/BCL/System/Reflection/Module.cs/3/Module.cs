// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
////////////////////////////////////////////////////////////////////////////////
 
using System; 
using System.Diagnostics.SymbolStore;
using System.Runtime.Remoting; 
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Reflection.Emit;
using System.Collections; 
using System.Threading;
using System.Runtime.CompilerServices; 
using System.Security.Permissions; 
using System.IO;
using System.Globalization; 
using System.Runtime.Versioning;

namespace System.Reflection
{ 

    [Serializable, Flags] 
[System.Runtime.InteropServices.ComVisible(true)] 
    public enum PortableExecutableKinds
    { 
        NotAPortableExecutableImage = 0x0,

        ILOnly                      = 0x1,
 
        Required32Bit               = 0x2,
 
        PE32Plus                    = 0x4, 

        Unmanaged32Bit              = 0x8, 
    }

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)] 
    public enum ImageFileMachine
    { 
        I386    = 0x014c, 

        IA64    = 0x0200, 

        AMD64   = 0x8664,
    }
 
    [Serializable()]
    [ClassInterface(ClassInterfaceType.None)] 
    [ComDefaultInterface(typeof(_Module))] 
[System.Runtime.InteropServices.ComVisible(true)]
    public class Module : _Module, ISerializable, ICustomAttributeProvider 
    {
        #region FCalls
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern Type _GetTypeInternal(String className, bool ignoreCase, bool throwOnError); 
        internal Type GetTypeInternal(String className, bool ignoreCase, bool throwOnError)
        { 
            return InternalModule._GetTypeInternal(className, ignoreCase, throwOnError); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern IntPtr _GetHINSTANCE();
        internal IntPtr GetHINSTANCE()
        { 
            return InternalModule._GetHINSTANCE();
        } 
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern String _InternalGetName(); 
        private String InternalGetName()
        {
            return InternalModule._InternalGetName();
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern String _InternalGetFullyQualifiedName(); 
        internal String InternalGetFullyQualifiedName()
        { 
            return InternalModule._InternalGetFullyQualifiedName();
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern Type[] _GetTypesInternal(ref StackCrawlMark stackMark);
        internal Type[] GetTypesInternal(ref StackCrawlMark stackMark) 
        { 
            return InternalModule._GetTypesInternal(ref stackMark);
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern Assembly _GetAssemblyInternal();
        internal virtual Assembly GetAssemblyInternal() 
        {
            return InternalModule._GetAssemblyInternal(); 
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern int _InternalGetTypeToken(String strFullName, Module refedModule, String strRefedModuleFileName, int tkResolution);
        internal int InternalGetTypeToken(String strFullName, Module refedModule, String strRefedModuleFileName, int tkResolution)
        {
            return InternalModule._InternalGetTypeToken(strFullName, refedModule.InternalModule, strRefedModuleFileName, tkResolution); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern Type _InternalLoadInMemoryTypeByName(String className);
        internal Type InternalLoadInMemoryTypeByName(String className) 
        {
            return InternalModule._InternalLoadInMemoryTypeByName(className);
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern int _InternalGetMemberRef(Module refedModule, int tr, int defToken); 
        internal int InternalGetMemberRef(Module refedModule, int tr, int defToken) 
        {
            return InternalModule._InternalGetMemberRef(refedModule.InternalModule, tr, defToken); 
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern int _InternalGetMemberRefFromSignature(int tr, String methodName, byte[] signature, int length); 
        internal int InternalGetMemberRefFromSignature(int tr, String methodName, byte[] signature, int length)
        { 
            return InternalModule._InternalGetMemberRefFromSignature(tr, methodName, signature, length); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern int _InternalGetMemberRefOfMethodInfo(int tr, IntPtr method);
        internal int InternalGetMemberRefOfMethodInfo(int tr, RuntimeMethodHandle method)
        { 
            return InternalModule._InternalGetMemberRefOfMethodInfo(tr, method.Value);
        } 
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern int _InternalGetMemberRefOfFieldInfo(int tkType, IntPtr interfaceHandle, int tkField); 
        internal int InternalGetMemberRefOfFieldInfo(int tkType, RuntimeTypeHandle declaringType, int tkField)
        {
            return InternalModule._InternalGetMemberRefOfFieldInfo(tkType, declaringType.Value, tkField);
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern int _InternalGetTypeSpecTokenWithBytes(byte[] signature, int length); 
        internal int InternalGetTypeSpecTokenWithBytes(byte[] signature, int length)
        { 
            return InternalModule._InternalGetTypeSpecTokenWithBytes(signature, length);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern int _nativeGetArrayMethodToken(int tkTypeSpec, String methodName, byte[] signature, int sigLength, int baseToken);
        internal int nativeGetArrayMethodToken(int tkTypeSpec, String methodName, byte[] signature, int sigLength, int baseToken) 
        { 
            return InternalModule._nativeGetArrayMethodToken(tkTypeSpec, methodName, signature, sigLength, baseToken);
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void _InternalSetFieldRVAContent(int fdToken, byte[] data, int length);
        internal void InternalSetFieldRVAContent(int fdToken, byte[] data, int length) 
        {
            InternalModule._InternalSetFieldRVAContent(fdToken, data, length); 
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern int _InternalGetStringConstant(String str);
        internal int InternalGetStringConstant(String str)
        {
            return InternalModule._InternalGetStringConstant(str); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern void _InternalPreSavePEFile(int portableExecutableKind, int imageFileMachine);
        internal void InternalPreSavePEFile(int portableExecutableKind, int imageFileMachine) 
        {
            InternalModule._InternalPreSavePEFile(portableExecutableKind, imageFileMachine);
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void _InternalSavePEFile(String fileName, int entryPoint, int isExe, bool isManifestFile); 
        internal void InternalSavePEFile(String fileName, MethodToken entryPoint, int isExe, bool isManifestFile) 
        {
            InternalModule._InternalSavePEFile(fileName, entryPoint.Token, isExe, isManifestFile); 
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void _InternalSetResourceCounts(int resCount); 
        internal void InternalSetResourceCounts(int resCount)
        { 
            InternalModule._InternalSetResourceCounts(resCount); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void _InternalAddResource(
            String strName, byte[] resBytes,
            int resByteCount, int tkFile, int attribute, 
            int portableExecutableKind, int imageFileMachine);
        internal void InternalAddResource( 
            String strName, byte[] resBytes, 
            int resByteCount, int tkFile, int attribute,
            int portableExecutableKind, int imageFileMachine) 
        {
            InternalModule._InternalAddResource(strName, resBytes,
            resByteCount, tkFile, attribute,
            portableExecutableKind, imageFileMachine); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern void _InternalSetModuleProps(String strModuleName);
        internal void InternalSetModuleProps(String strModuleName) 
        {
            InternalModule._InternalSetModuleProps(strModuleName);
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern bool _IsResourceInternal(); 
        internal bool IsResourceInternal() 
        {
            return InternalModule._IsResourceInternal(); 
        }

#if !FEATURE_PAL
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern System.Security.Cryptography.X509Certificates.X509Certificate _GetSignerCertificateInternal();
        internal System.Security.Cryptography.X509Certificates.X509Certificate GetSignerCertificateInternal() 
        { 
            return InternalModule._GetSignerCertificateInternal();
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void _InternalDefineNativeResourceFile(String strFilename, int portableExecutableKind, int ImageFileMachine);
        internal void InternalDefineNativeResourceFile(String strFilename, int portableExecutableKind, int ImageFileMachine) 
        {
            InternalModule._InternalDefineNativeResourceFile(strFilename, portableExecutableKind, ImageFileMachine); 
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern void _InternalDefineNativeResourceBytes(byte[] resource, int portableExecutableKind, int imageFileMachine);
        internal void InternalDefineNativeResourceBytes(byte[] resource, int portableExecutableKind, int imageFileMachine)
        {
            InternalModule._InternalDefineNativeResourceBytes(resource, portableExecutableKind, imageFileMachine); 
        }
#endif 
        #endregion 

        #region Static Constructor 
        static Module()
        {
            __Filters _fltObj;
            _fltObj = new __Filters(); 
            FilterTypeName = new TypeFilter(_fltObj.FilterTypeName);
            FilterTypeNameIgnoreCase = new TypeFilter(_fltObj.FilterTypeNameIgnoreCase); 
        } 
        #endregion
 
        #region Public Statics
        public static readonly TypeFilter FilterTypeName;

        public static readonly TypeFilter FilterTypeNameIgnoreCase; 

        public MethodBase ResolveMethod(int metadataToken) 
        { 
            return ResolveMethod(metadataToken, null, null);
        } 

        private static RuntimeTypeHandle[] ConvertToTypeHandleArray(Type[] genericArguments)
        {
            if (genericArguments == null) 
                return null;
 
            int size = genericArguments.Length; 
            RuntimeTypeHandle[] typeHandleArgs = new RuntimeTypeHandle[size];
            for (int i = 0; i < size; i++) 
            {
                Type typeArg = genericArguments[i];
                if (typeArg == null)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidGenericInstArray")); 
                typeArg = typeArg.UnderlyingSystemType;
                if (typeArg == null) 
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidGenericInstArray")); 
                if (!(typeArg is RuntimeType))
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidGenericInstArray")); 
                typeHandleArgs[i] = typeArg.GetTypeHandleInternal();
            }
            return typeHandleArgs;
        } 

        public byte[] ResolveSignature(int metadataToken) 
        { 
            MetadataToken tk = new MetadataToken(metadataToken);
 
            if (!MetadataImport.IsValidToken(tk))
                throw new ArgumentOutOfRangeException("metadataToken",
                    String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidToken", tk, this)));
 
            if (!tk.IsMemberRef && !tk.IsMethodDef && !tk.IsTypeSpec && !tk.IsSignature && !tk.IsFieldDef)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidToken", tk, this)), 
                                            "metadataToken"); 

            ConstArray signature; 
            if (tk.IsMemberRef)
                signature = MetadataImport.GetMemberRefProps(metadataToken);
            else
                signature = MetadataImport.GetSignatureFromToken(metadataToken); 

            byte[] sig = new byte[signature.Length]; 
 
            for (int i = 0; i < signature.Length; i++)
                sig[i] = signature[i]; 

            return sig;
        }
 
        public MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        { 
            MetadataToken tk = new MetadataToken(metadataToken); 

            if (!MetadataImport.IsValidToken(tk)) 
                throw new ArgumentOutOfRangeException("metadataToken",
                    String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidToken", tk, this)));

            RuntimeTypeHandle[] typeArgs = ConvertToTypeHandleArray(genericTypeArguments); 
            RuntimeTypeHandle[] methodArgs = ConvertToTypeHandleArray(genericMethodArguments);
 
            try 
            {
                if (!tk.IsMethodDef && !tk.IsMethodSpec) 
                {
                    if (!tk.IsMemberRef)
                        throw new ArgumentException("metadataToken",
                            String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveMethod", tk, this))); 

                    unsafe 
                    { 
                        ConstArray sig = MetadataImport.GetMemberRefProps(tk);
 
                        if (*(CorCallingConvention*)sig.Signature.ToPointer() == CorCallingConvention.Field)
                            throw new ArgumentException("metadataToken",
                                String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveMethod"), tk, this));
                    } 
                }
 
                RuntimeMethodHandle methodHandle = GetModuleHandle().ResolveMethodHandle(tk, typeArgs, methodArgs); 
                Type declaringType = methodHandle.GetDeclaringType().GetRuntimeType();
 
                if (declaringType.IsGenericType || declaringType.IsArray)
                {
                    MetadataToken tkDeclaringType = new MetadataToken(MetadataImport.GetParentToken(tk));
 
                    if (tk.IsMethodSpec)
                        tkDeclaringType = new MetadataToken(MetadataImport.GetParentToken(tkDeclaringType)); 
 
                    declaringType = ResolveType(tkDeclaringType, genericTypeArguments, genericMethodArguments);
                } 

                return System.RuntimeType.GetMethodBase(declaringType.GetTypeHandleInternal(), methodHandle);
            }
            catch (BadImageFormatException e) 
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_BadImageFormatExceptionResolve"), e); 
            } 
        }
 
        internal FieldInfo ResolveLiteralField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            MetadataToken tk = new MetadataToken(metadataToken);
 
            if (!MetadataImport.IsValidToken(tk))
                throw new ArgumentOutOfRangeException("metadataToken", 
                    String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Argument_InvalidToken", tk, this))); 

            int tkDeclaringType; 
            string fieldName;

            fieldName = MetadataImport.GetName(tk).ToString();
            tkDeclaringType = MetadataImport.GetParentToken(tk); 

            Type declaringType = ResolveType(tkDeclaringType, genericTypeArguments, genericMethodArguments); 
 
            declaringType.GetFields();
 
            try
            {
                return declaringType.GetField(fieldName,
                    BindingFlags.Static | BindingFlags.Instance | 
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly); 
            } 
            catch
            { 
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveField"), tk, this), "metadataToken");
            }
        }
 
        public FieldInfo ResolveField(int metadataToken)
        { 
            return ResolveField(metadataToken, null, null); 
        }
 
        public FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            MetadataToken tk = new MetadataToken(metadataToken);
 
            if (!MetadataImport.IsValidToken(tk))
                throw new ArgumentOutOfRangeException("metadataToken", 
                    String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidToken", tk, this))); 

            RuntimeTypeHandle[] typeArgs = ConvertToTypeHandleArray(genericTypeArguments); 
            RuntimeTypeHandle[] methodArgs = ConvertToTypeHandleArray(genericMethodArguments);

            try
            { 
                RuntimeFieldHandle fieldHandle = new RuntimeFieldHandle();
 
                if (!tk.IsFieldDef) 
                {
                    if (!tk.IsMemberRef) 
                        throw new ArgumentException("metadataToken",
                            String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveField"), tk, this));

                    unsafe 
                    {
                        ConstArray sig = MetadataImport.GetMemberRefProps(tk); 
 
                        if (*(CorCallingConvention*)sig.Signature.ToPointer() != CorCallingConvention.Field)
                            throw new ArgumentException("metadataToken", 
                                String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveField"), tk, this));
                    }

                    fieldHandle = GetModuleHandle().ResolveFieldHandle(tk, typeArgs, methodArgs); 
                }
 
                fieldHandle = GetModuleHandle().ResolveFieldHandle(metadataToken, typeArgs, methodArgs); 
                Type declaringType = fieldHandle.GetApproxDeclaringType().GetRuntimeType();
 
                if (declaringType.IsGenericType || declaringType.IsArray)
                {
                    int tkDeclaringType = GetModuleHandle().GetMetadataImport().GetParentToken(metadataToken);
                    declaringType = ResolveType(tkDeclaringType, genericTypeArguments, genericMethodArguments); 
                }
 
                return System.RuntimeType.GetFieldInfo(declaringType.GetTypeHandleInternal(), fieldHandle); 
            }
            catch(MissingFieldException) 
            {
                return ResolveLiteralField(tk, genericTypeArguments, genericMethodArguments);
            }
            catch (BadImageFormatException e) 
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_BadImageFormatExceptionResolve"), e); 
            } 
        }
 
        public Type ResolveType(int metadataToken)
        {
            return ResolveType(metadataToken, null, null);
        } 

        public Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments) 
        { 
            MetadataToken tk = new MetadataToken(metadataToken);
 
            if (tk.IsGlobalTypeDefToken)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveModuleType"), tk), "metadataToken");

            if (!MetadataImport.IsValidToken(tk)) 
                throw new ArgumentOutOfRangeException("metadataToken",
                    String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidToken", tk, this))); 
 
            if (!tk.IsTypeDef && !tk.IsTypeSpec && !tk.IsTypeRef)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveType"), tk, this), "metadataToken"); 

            RuntimeTypeHandle[] typeArgs = ConvertToTypeHandleArray(genericTypeArguments);
            RuntimeTypeHandle[] methodArgs = ConvertToTypeHandleArray(genericMethodArguments);
 
            try
            { 
                Type t = GetModuleHandle().ResolveTypeHandle(metadataToken, typeArgs, methodArgs).GetRuntimeType(); 

                if (t == null) 
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveType"), tk, this), "metadataToken");

                return t;
            } 
            catch (BadImageFormatException e)
            { 
                throw new ArgumentException(Environment.GetResourceString("Argument_BadImageFormatExceptionResolve"), e); 
            }
        } 

        public MemberInfo ResolveMember(int metadataToken)
        {
            return ResolveMember(metadataToken, null, null); 
        }
 
        public MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments) 
        {
            MetadataToken tk = new MetadataToken(metadataToken); 

            if (tk.IsProperty)
                throw new ArgumentException(Environment.GetResourceString("InvalidOperation_PropertyInfoNotAvailable"));
 
            if (tk.IsEvent)
                throw new ArgumentException(Environment.GetResourceString("InvalidOperation_EventInfoNotAvailable")); 
 
            if (tk.IsMethodSpec || tk.IsMethodDef)
                return ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments); 

            if (tk.IsFieldDef)
                return ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
 
            if (tk.IsTypeRef || tk.IsTypeDef || tk.IsTypeSpec)
                return ResolveType(metadataToken, genericTypeArguments, genericMethodArguments); 
 
            if (tk.IsMemberRef)
            { 
                if (!MetadataImport.IsValidToken(tk))
                    throw new ArgumentOutOfRangeException("metadataToken",
                        String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidToken", tk, this)));
 
                ConstArray sig = MetadataImport.GetMemberRefProps(tk);
 
                unsafe 
                {
                    if (*(CorCallingConvention*)sig.Signature.ToPointer() == CorCallingConvention.Field) 
                    {
                        return ResolveField(tk, genericTypeArguments, genericMethodArguments);
                    }
                    else 
                    {
                        return ResolveMethod(tk, genericTypeArguments, genericMethodArguments); 
                    } 
                }
            } 

            throw new ArgumentException("metadataToken",
                String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveMember", tk, this)));
        } 

        public string ResolveString(int metadataToken) 
        { 
            MetadataToken tk = new MetadataToken(metadataToken);
            if (!tk.IsString) 
                throw new ArgumentException(
                    String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Argument_ResolveString"), metadataToken, ToString()));

            if (!MetadataImport.IsValidToken(tk)) 
                throw new ArgumentOutOfRangeException("metadataToken",
                    String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Argument_InvalidToken", tk, this))); 
 
            string str = MetadataImport.GetUserString(metadataToken);
 
            if (str == null)
                throw new ArgumentException(
                    String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Argument_ResolveString"), metadataToken, ToString()));
 
            return str;
        } 
 
        public void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
        { 
            GetModuleHandle().GetPEKind(out peKind, out machine);
        }

 	public int MDStreamVersion 
	{
	    get {  return GetModuleHandle().MDStreamVersion; } 
	} 

        #endregion 

        #region Literals
        private const BindingFlags DefaultLookup = BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public;
        #endregion 

        #region Data Members 
        #pragma warning disable 169 
        // If you add any data members, you need to update the native declaration ReflectModuleBaseObject.
        internal ArrayList          m__TypeBuilderList; 
        internal ISymbolWriter      m__iSymWriter;
        internal ModuleBuilderData  m__moduleData;
        private RuntimeType         m__runtimeType;
        private IntPtr              m__pRefClass; 
        internal IntPtr             m__pData;
        internal IntPtr             m__pInternalSymWriter; 
        private IntPtr              m__pGlobals; 
        private IntPtr              m__pFields;
        internal MethodToken        m__EntryPoint; 
        #pragma warning restore 169
        public override bool Equals(object o)
        {
            if (o == null) 
                return false;
 
            if (!(o is Module)) 
                return false;
 
            Module rhs = o as Module;
            rhs = rhs.InternalModule;
            return (object)InternalModule == (object)rhs;
        } 
        public override int GetHashCode() { return base.GetHashCode(); }
        internal virtual Module InternalModule 
        { 
            get
            { 
                return this;
            }
        }
        internal ArrayList m_TypeBuilderList { get { return InternalModule.m__TypeBuilderList; } set { InternalModule.m__TypeBuilderList = value; } } 
        internal ISymbolWriter m_iSymWriter { get { return InternalModule.m__iSymWriter; } set { InternalModule.m__iSymWriter = value; } }
        internal ModuleBuilderData m_moduleData { get { return InternalModule.m__moduleData; } set { InternalModule.m__moduleData = value; } } 
        private RuntimeType m_runtimeType { get { return InternalModule.m__runtimeType; } set { InternalModule.m__runtimeType = value; } } 
        private IntPtr m_pRefClass { get { return InternalModule.m__pRefClass; } }
        internal IntPtr m_pData { get { return InternalModule.m__pData; } } 
        internal IntPtr m_pInternalSymWriter { get { return InternalModule.m__pInternalSymWriter; } }
        private IntPtr m_pGlobals { get { return InternalModule.m__pGlobals; } }
        private IntPtr m_pFields { get { return InternalModule.m__pFields; } }
        internal MethodToken m_EntryPoint { get { return InternalModule.m__EntryPoint; } set { InternalModule.m__EntryPoint = value; } } 
        #endregion
 
        #region Constructor 
        internal Module()
        { 
            // Construct a new module.  This returns the default dynamic module.
            // 0 is defined as being a module without an entry point (ie a DLL);
            // This must throw because this dies in ToString() when constructed here...
            // throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_Constructor)); 
            //m_EntryPoint=new MethodToken(0);
        } 
        #endregion 

        #region Private Members 
        private FieldInfo InternalGetField(String name, BindingFlags bindingAttr)
        {
            if (RuntimeType == null)
                return null; 

            return RuntimeType.GetField(name, bindingAttr); 
        } 
        #endregion
 
        #region Internal Members
        internal virtual bool IsDynamic()
        {
            return false; 
        }
 
        internal RuntimeType RuntimeType 
        {
            get 
            {
                unsafe
                {
                    if (m_runtimeType == null) 
                        m_runtimeType = GetModuleHandle().GetModuleTypeHandle().GetRuntimeType() as RuntimeType;
 
                    return m_runtimeType; 
                }
            } 
        }
        #endregion

        #region Protected Virtuals 
        protected virtual MethodInfo GetMethodImpl(String name,BindingFlags bindingAttr,Binder binder,
            CallingConventions callConvention, Type[] types,ParameterModifier[] modifiers) 
        { 
            if (RuntimeType == null)
                return null; 

            if (types == null)
            {
                return RuntimeType.GetMethod(name, bindingAttr); 
            }
            else 
            { 
                return RuntimeType.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
            } 
        }

        internal MetadataImport MetadataImport
        { 
            get
            { 
                unsafe 
                {
                    return ModuleHandle.GetMetadataImport(); 
                }
            }
        }
        #endregion 

        #region ICustomAttributeProvider Members 
        public virtual Object[] GetCustomAttributes(bool inherit) 
        {
            return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType); 
        }

        public virtual Object[] GetCustomAttributes(Type attributeType, bool inherit)
        { 
            if (attributeType == null)
                throw new ArgumentNullException("attributeType"); 
 
            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
 
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"attributeType");

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType); 
        }
 
        public virtual bool IsDefined (Type attributeType, bool inherit) 
        {
            if (attributeType == null) 
                throw new ArgumentNullException("attributeType");

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
 
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"caType"); 
 
            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        } 

        #endregion

        #region Public Virtuals 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) 
        { 
            if (info == null)
            { 
                throw new ArgumentNullException("info");
            }
            UnitySerializationHolder.GetUnitySerializationInfo(info, UnitySerializationHolder.ModuleUnity, this.ScopeName, GetAssemblyInternal());
        } 

[System.Runtime.InteropServices.ComVisible(true)] 
        public virtual Type GetType(String className, bool ignoreCase) 
        {
            return GetType(className, false, ignoreCase); 
        }

[System.Runtime.InteropServices.ComVisible(true)]
        public virtual Type GetType(String className) { 
            return GetType(className, false, false);
        } 
 
[System.Runtime.InteropServices.ComVisible(true)]
        public virtual Type GetType(String className, bool throwOnError, bool ignoreCase) 
        {
            return GetTypeInternal(className, throwOnError, ignoreCase);
        }
 
        public virtual String FullyQualifiedName
        { 
            [ResourceExposure(ResourceScope.Machine)] 
            [ResourceConsumption(ResourceScope.Machine)]
            get 
            {
                String fullyQualifiedName = InternalGetFullyQualifiedName();

                if (fullyQualifiedName != null) { 
                    bool checkPermission = true;
                    try { 
                        Path.GetFullPathInternal(fullyQualifiedName); 
                    }
                    catch(ArgumentException) { 
                        checkPermission = false;
                    }
                    if (checkPermission) {
                        new FileIOPermission( FileIOPermissionAccess.PathDiscovery, fullyQualifiedName ).Demand(); 
                    }
                } 
 
                return fullyQualifiedName;
            } 
        }

        public virtual Type[] FindTypes(TypeFilter filter,Object filterCriteria)
        { 
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Type[] c = GetTypesInternal(ref stackMark); 
            int cnt = 0; 
            for (int i = 0;i<c.Length;i++) {
                if (filter!=null && !filter(c[i],filterCriteria)) 
                    c[i] = null;
                else
                    cnt++;
            } 
            if (cnt == c.Length)
                return c; 
 
            Type[] ret = new Type[cnt];
            cnt=0; 
            for (int i=0;i<c.Length;i++) {
                if (c[i] != null)
                    ret[cnt++] = c[i];
            } 
            return ret;
        } 
 
        public virtual Type[] GetTypes()
        { 
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return GetTypesInternal(ref stackMark);
        }
 
        #endregion
 
        #region Public Members 

        public Guid ModuleVersionId 
        {
            get
            {
                unsafe 
                {
                    Guid mvid; 
                    MetadataImport.GetScopeProps(out mvid); 
                    return mvid;
                } 
            }
        }

        public int MetadataToken 
        {
            get 
            { 
                return GetModuleHandle().GetToken();
            } 
        }

        public bool IsResource()
        { 
            return IsResourceInternal();
        } 
 
        public FieldInfo[] GetFields()
        { 
            if (RuntimeType == null)
                return new FieldInfo[0];

            return RuntimeType.GetFields(); 
        }
 
        public FieldInfo[] GetFields(BindingFlags bindingFlags) 
        {
            if (RuntimeType == null) 
                return new FieldInfo[0];

            return RuntimeType.GetFields(bindingFlags);
        } 

        public FieldInfo GetField(String name) 
        { 
            if (name == null)
                throw new ArgumentNullException("name"); 

            return GetField(name,Module.DefaultLookup);
        }
 
        public FieldInfo GetField(String name, BindingFlags bindingAttr)
        { 
            if (name == null) 
                throw new ArgumentNullException("name");
 
            return InternalGetField(name, bindingAttr);
        }

        public MethodInfo[] GetMethods() 
        {
            if (RuntimeType == null) 
                return new MethodInfo[0]; 

            return RuntimeType.GetMethods(); 
        }

        public MethodInfo[] GetMethods(BindingFlags bindingFlags)
        { 
            if (RuntimeType == null)
                return new MethodInfo[0]; 
 
            return RuntimeType.GetMethods(bindingFlags);
        } 

        public MethodInfo GetMethod(
            String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        { 
            if (name == null)
                throw new ArgumentNullException("name"); 
 
            if (types == null)
                throw new ArgumentNullException("types"); 

            for (int i =0;i < types.Length;i++)
            {
                if (types[i] == null) 
                    throw new ArgumentNullException("types");
            } 
 
            return GetMethodImpl(name,bindingAttr,binder,callConvention,types,modifiers);
 
        }

        public MethodInfo GetMethod(String name,Type[] types)
        { 
            if (name == null)
                throw new ArgumentNullException("name"); 
 
            if (types == null)
                throw new ArgumentNullException("types"); 

            for (int i =0;i < types.Length;i++)
            {
                if (types[i] == null) 
                    throw new ArgumentNullException("types");
            } 
 
            return GetMethodImpl(name, Module.DefaultLookup, null, CallingConventions.Any, types, null);
        } 

        public MethodInfo GetMethod(String name)
        {
            if (name == null) 
                throw new ArgumentNullException("name");
            return GetMethodImpl(name,Module.DefaultLookup,null,CallingConventions.Any, 
                null,null); 
        }
 
        public String ScopeName
        {
            get
            { 
                return InternalGetName();
            } 
        } 

        public String Name 
        {
            [ResourceExposure(ResourceScope.None)]
            [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
            get 
            {
                String s = InternalGetFullyQualifiedName(); 
#if !FEATURE_PAL 
                int i = s.LastIndexOf('\\');
#else 
                int i = s.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
#endif
                if (i == -1)
                    return s; 

                return new String(s.ToCharArray(),i+1,s.Length-i-1); 
            } 
        }
 
        public override String ToString()
        {
            return ScopeName;
        } 

        public Assembly Assembly 
        { 
            get
            { 
                return GetAssemblyInternal();
            }
        }
 
        public unsafe ModuleHandle ModuleHandle
        { 
            get 
            {
                // 


                return new ModuleHandle((void*)m_pData);
            } 
        }
 
        internal unsafe ModuleHandle GetModuleHandle() 
        {
            return new ModuleHandle((void*)m_pData); 
        }
#if !FEATURE_PAL
        public System.Security.Cryptography.X509Certificates.X509Certificate GetSignerCertificate()
        { 
            return GetSignerCertificateInternal();
        } 
#endif 
        #endregion
 
        void _Module.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        } 

        void _Module.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo) 
        { 
            throw new NotImplementedException();
        } 

        void _Module.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException(); 
        }
 
        void _Module.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr) 
        {
            throw new NotImplementedException(); 
        }
   }
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
////////////////////////////////////////////////////////////////////////////////
 
using System; 
using System.Diagnostics.SymbolStore;
using System.Runtime.Remoting; 
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Reflection.Emit;
using System.Collections; 
using System.Threading;
using System.Runtime.CompilerServices; 
using System.Security.Permissions; 
using System.IO;
using System.Globalization; 
using System.Runtime.Versioning;

namespace System.Reflection
{ 

    [Serializable, Flags] 
[System.Runtime.InteropServices.ComVisible(true)] 
    public enum PortableExecutableKinds
    { 
        NotAPortableExecutableImage = 0x0,

        ILOnly                      = 0x1,
 
        Required32Bit               = 0x2,
 
        PE32Plus                    = 0x4, 

        Unmanaged32Bit              = 0x8, 
    }

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)] 
    public enum ImageFileMachine
    { 
        I386    = 0x014c, 

        IA64    = 0x0200, 

        AMD64   = 0x8664,
    }
 
    [Serializable()]
    [ClassInterface(ClassInterfaceType.None)] 
    [ComDefaultInterface(typeof(_Module))] 
[System.Runtime.InteropServices.ComVisible(true)]
    public class Module : _Module, ISerializable, ICustomAttributeProvider 
    {
        #region FCalls
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern Type _GetTypeInternal(String className, bool ignoreCase, bool throwOnError); 
        internal Type GetTypeInternal(String className, bool ignoreCase, bool throwOnError)
        { 
            return InternalModule._GetTypeInternal(className, ignoreCase, throwOnError); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern IntPtr _GetHINSTANCE();
        internal IntPtr GetHINSTANCE()
        { 
            return InternalModule._GetHINSTANCE();
        } 
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern String _InternalGetName(); 
        private String InternalGetName()
        {
            return InternalModule._InternalGetName();
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern String _InternalGetFullyQualifiedName(); 
        internal String InternalGetFullyQualifiedName()
        { 
            return InternalModule._InternalGetFullyQualifiedName();
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern Type[] _GetTypesInternal(ref StackCrawlMark stackMark);
        internal Type[] GetTypesInternal(ref StackCrawlMark stackMark) 
        { 
            return InternalModule._GetTypesInternal(ref stackMark);
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern Assembly _GetAssemblyInternal();
        internal virtual Assembly GetAssemblyInternal() 
        {
            return InternalModule._GetAssemblyInternal(); 
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern int _InternalGetTypeToken(String strFullName, Module refedModule, String strRefedModuleFileName, int tkResolution);
        internal int InternalGetTypeToken(String strFullName, Module refedModule, String strRefedModuleFileName, int tkResolution)
        {
            return InternalModule._InternalGetTypeToken(strFullName, refedModule.InternalModule, strRefedModuleFileName, tkResolution); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern Type _InternalLoadInMemoryTypeByName(String className);
        internal Type InternalLoadInMemoryTypeByName(String className) 
        {
            return InternalModule._InternalLoadInMemoryTypeByName(className);
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern int _InternalGetMemberRef(Module refedModule, int tr, int defToken); 
        internal int InternalGetMemberRef(Module refedModule, int tr, int defToken) 
        {
            return InternalModule._InternalGetMemberRef(refedModule.InternalModule, tr, defToken); 
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern int _InternalGetMemberRefFromSignature(int tr, String methodName, byte[] signature, int length); 
        internal int InternalGetMemberRefFromSignature(int tr, String methodName, byte[] signature, int length)
        { 
            return InternalModule._InternalGetMemberRefFromSignature(tr, methodName, signature, length); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern int _InternalGetMemberRefOfMethodInfo(int tr, IntPtr method);
        internal int InternalGetMemberRefOfMethodInfo(int tr, RuntimeMethodHandle method)
        { 
            return InternalModule._InternalGetMemberRefOfMethodInfo(tr, method.Value);
        } 
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern int _InternalGetMemberRefOfFieldInfo(int tkType, IntPtr interfaceHandle, int tkField); 
        internal int InternalGetMemberRefOfFieldInfo(int tkType, RuntimeTypeHandle declaringType, int tkField)
        {
            return InternalModule._InternalGetMemberRefOfFieldInfo(tkType, declaringType.Value, tkField);
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern int _InternalGetTypeSpecTokenWithBytes(byte[] signature, int length); 
        internal int InternalGetTypeSpecTokenWithBytes(byte[] signature, int length)
        { 
            return InternalModule._InternalGetTypeSpecTokenWithBytes(signature, length);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern int _nativeGetArrayMethodToken(int tkTypeSpec, String methodName, byte[] signature, int sigLength, int baseToken);
        internal int nativeGetArrayMethodToken(int tkTypeSpec, String methodName, byte[] signature, int sigLength, int baseToken) 
        { 
            return InternalModule._nativeGetArrayMethodToken(tkTypeSpec, methodName, signature, sigLength, baseToken);
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void _InternalSetFieldRVAContent(int fdToken, byte[] data, int length);
        internal void InternalSetFieldRVAContent(int fdToken, byte[] data, int length) 
        {
            InternalModule._InternalSetFieldRVAContent(fdToken, data, length); 
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern int _InternalGetStringConstant(String str);
        internal int InternalGetStringConstant(String str)
        {
            return InternalModule._InternalGetStringConstant(str); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern void _InternalPreSavePEFile(int portableExecutableKind, int imageFileMachine);
        internal void InternalPreSavePEFile(int portableExecutableKind, int imageFileMachine) 
        {
            InternalModule._InternalPreSavePEFile(portableExecutableKind, imageFileMachine);
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void _InternalSavePEFile(String fileName, int entryPoint, int isExe, bool isManifestFile); 
        internal void InternalSavePEFile(String fileName, MethodToken entryPoint, int isExe, bool isManifestFile) 
        {
            InternalModule._InternalSavePEFile(fileName, entryPoint.Token, isExe, isManifestFile); 
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void _InternalSetResourceCounts(int resCount); 
        internal void InternalSetResourceCounts(int resCount)
        { 
            InternalModule._InternalSetResourceCounts(resCount); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void _InternalAddResource(
            String strName, byte[] resBytes,
            int resByteCount, int tkFile, int attribute, 
            int portableExecutableKind, int imageFileMachine);
        internal void InternalAddResource( 
            String strName, byte[] resBytes, 
            int resByteCount, int tkFile, int attribute,
            int portableExecutableKind, int imageFileMachine) 
        {
            InternalModule._InternalAddResource(strName, resBytes,
            resByteCount, tkFile, attribute,
            portableExecutableKind, imageFileMachine); 
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern void _InternalSetModuleProps(String strModuleName);
        internal void InternalSetModuleProps(String strModuleName) 
        {
            InternalModule._InternalSetModuleProps(strModuleName);
        }
 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern bool _IsResourceInternal(); 
        internal bool IsResourceInternal() 
        {
            return InternalModule._IsResourceInternal(); 
        }

#if !FEATURE_PAL
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern System.Security.Cryptography.X509Certificates.X509Certificate _GetSignerCertificateInternal();
        internal System.Security.Cryptography.X509Certificates.X509Certificate GetSignerCertificateInternal() 
        { 
            return InternalModule._GetSignerCertificateInternal();
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void _InternalDefineNativeResourceFile(String strFilename, int portableExecutableKind, int ImageFileMachine);
        internal void InternalDefineNativeResourceFile(String strFilename, int portableExecutableKind, int ImageFileMachine) 
        {
            InternalModule._InternalDefineNativeResourceFile(strFilename, portableExecutableKind, ImageFileMachine); 
        } 

        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private extern void _InternalDefineNativeResourceBytes(byte[] resource, int portableExecutableKind, int imageFileMachine);
        internal void InternalDefineNativeResourceBytes(byte[] resource, int portableExecutableKind, int imageFileMachine)
        {
            InternalModule._InternalDefineNativeResourceBytes(resource, portableExecutableKind, imageFileMachine); 
        }
#endif 
        #endregion 

        #region Static Constructor 
        static Module()
        {
            __Filters _fltObj;
            _fltObj = new __Filters(); 
            FilterTypeName = new TypeFilter(_fltObj.FilterTypeName);
            FilterTypeNameIgnoreCase = new TypeFilter(_fltObj.FilterTypeNameIgnoreCase); 
        } 
        #endregion
 
        #region Public Statics
        public static readonly TypeFilter FilterTypeName;

        public static readonly TypeFilter FilterTypeNameIgnoreCase; 

        public MethodBase ResolveMethod(int metadataToken) 
        { 
            return ResolveMethod(metadataToken, null, null);
        } 

        private static RuntimeTypeHandle[] ConvertToTypeHandleArray(Type[] genericArguments)
        {
            if (genericArguments == null) 
                return null;
 
            int size = genericArguments.Length; 
            RuntimeTypeHandle[] typeHandleArgs = new RuntimeTypeHandle[size];
            for (int i = 0; i < size; i++) 
            {
                Type typeArg = genericArguments[i];
                if (typeArg == null)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidGenericInstArray")); 
                typeArg = typeArg.UnderlyingSystemType;
                if (typeArg == null) 
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidGenericInstArray")); 
                if (!(typeArg is RuntimeType))
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidGenericInstArray")); 
                typeHandleArgs[i] = typeArg.GetTypeHandleInternal();
            }
            return typeHandleArgs;
        } 

        public byte[] ResolveSignature(int metadataToken) 
        { 
            MetadataToken tk = new MetadataToken(metadataToken);
 
            if (!MetadataImport.IsValidToken(tk))
                throw new ArgumentOutOfRangeException("metadataToken",
                    String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidToken", tk, this)));
 
            if (!tk.IsMemberRef && !tk.IsMethodDef && !tk.IsTypeSpec && !tk.IsSignature && !tk.IsFieldDef)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidToken", tk, this)), 
                                            "metadataToken"); 

            ConstArray signature; 
            if (tk.IsMemberRef)
                signature = MetadataImport.GetMemberRefProps(metadataToken);
            else
                signature = MetadataImport.GetSignatureFromToken(metadataToken); 

            byte[] sig = new byte[signature.Length]; 
 
            for (int i = 0; i < signature.Length; i++)
                sig[i] = signature[i]; 

            return sig;
        }
 
        public MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        { 
            MetadataToken tk = new MetadataToken(metadataToken); 

            if (!MetadataImport.IsValidToken(tk)) 
                throw new ArgumentOutOfRangeException("metadataToken",
                    String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidToken", tk, this)));

            RuntimeTypeHandle[] typeArgs = ConvertToTypeHandleArray(genericTypeArguments); 
            RuntimeTypeHandle[] methodArgs = ConvertToTypeHandleArray(genericMethodArguments);
 
            try 
            {
                if (!tk.IsMethodDef && !tk.IsMethodSpec) 
                {
                    if (!tk.IsMemberRef)
                        throw new ArgumentException("metadataToken",
                            String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveMethod", tk, this))); 

                    unsafe 
                    { 
                        ConstArray sig = MetadataImport.GetMemberRefProps(tk);
 
                        if (*(CorCallingConvention*)sig.Signature.ToPointer() == CorCallingConvention.Field)
                            throw new ArgumentException("metadataToken",
                                String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveMethod"), tk, this));
                    } 
                }
 
                RuntimeMethodHandle methodHandle = GetModuleHandle().ResolveMethodHandle(tk, typeArgs, methodArgs); 
                Type declaringType = methodHandle.GetDeclaringType().GetRuntimeType();
 
                if (declaringType.IsGenericType || declaringType.IsArray)
                {
                    MetadataToken tkDeclaringType = new MetadataToken(MetadataImport.GetParentToken(tk));
 
                    if (tk.IsMethodSpec)
                        tkDeclaringType = new MetadataToken(MetadataImport.GetParentToken(tkDeclaringType)); 
 
                    declaringType = ResolveType(tkDeclaringType, genericTypeArguments, genericMethodArguments);
                } 

                return System.RuntimeType.GetMethodBase(declaringType.GetTypeHandleInternal(), methodHandle);
            }
            catch (BadImageFormatException e) 
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_BadImageFormatExceptionResolve"), e); 
            } 
        }
 
        internal FieldInfo ResolveLiteralField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            MetadataToken tk = new MetadataToken(metadataToken);
 
            if (!MetadataImport.IsValidToken(tk))
                throw new ArgumentOutOfRangeException("metadataToken", 
                    String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Argument_InvalidToken", tk, this))); 

            int tkDeclaringType; 
            string fieldName;

            fieldName = MetadataImport.GetName(tk).ToString();
            tkDeclaringType = MetadataImport.GetParentToken(tk); 

            Type declaringType = ResolveType(tkDeclaringType, genericTypeArguments, genericMethodArguments); 
 
            declaringType.GetFields();
 
            try
            {
                return declaringType.GetField(fieldName,
                    BindingFlags.Static | BindingFlags.Instance | 
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly); 
            } 
            catch
            { 
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveField"), tk, this), "metadataToken");
            }
        }
 
        public FieldInfo ResolveField(int metadataToken)
        { 
            return ResolveField(metadataToken, null, null); 
        }
 
        public FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            MetadataToken tk = new MetadataToken(metadataToken);
 
            if (!MetadataImport.IsValidToken(tk))
                throw new ArgumentOutOfRangeException("metadataToken", 
                    String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidToken", tk, this))); 

            RuntimeTypeHandle[] typeArgs = ConvertToTypeHandleArray(genericTypeArguments); 
            RuntimeTypeHandle[] methodArgs = ConvertToTypeHandleArray(genericMethodArguments);

            try
            { 
                RuntimeFieldHandle fieldHandle = new RuntimeFieldHandle();
 
                if (!tk.IsFieldDef) 
                {
                    if (!tk.IsMemberRef) 
                        throw new ArgumentException("metadataToken",
                            String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveField"), tk, this));

                    unsafe 
                    {
                        ConstArray sig = MetadataImport.GetMemberRefProps(tk); 
 
                        if (*(CorCallingConvention*)sig.Signature.ToPointer() != CorCallingConvention.Field)
                            throw new ArgumentException("metadataToken", 
                                String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveField"), tk, this));
                    }

                    fieldHandle = GetModuleHandle().ResolveFieldHandle(tk, typeArgs, methodArgs); 
                }
 
                fieldHandle = GetModuleHandle().ResolveFieldHandle(metadataToken, typeArgs, methodArgs); 
                Type declaringType = fieldHandle.GetApproxDeclaringType().GetRuntimeType();
 
                if (declaringType.IsGenericType || declaringType.IsArray)
                {
                    int tkDeclaringType = GetModuleHandle().GetMetadataImport().GetParentToken(metadataToken);
                    declaringType = ResolveType(tkDeclaringType, genericTypeArguments, genericMethodArguments); 
                }
 
                return System.RuntimeType.GetFieldInfo(declaringType.GetTypeHandleInternal(), fieldHandle); 
            }
            catch(MissingFieldException) 
            {
                return ResolveLiteralField(tk, genericTypeArguments, genericMethodArguments);
            }
            catch (BadImageFormatException e) 
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_BadImageFormatExceptionResolve"), e); 
            } 
        }
 
        public Type ResolveType(int metadataToken)
        {
            return ResolveType(metadataToken, null, null);
        } 

        public Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments) 
        { 
            MetadataToken tk = new MetadataToken(metadataToken);
 
            if (tk.IsGlobalTypeDefToken)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveModuleType"), tk), "metadataToken");

            if (!MetadataImport.IsValidToken(tk)) 
                throw new ArgumentOutOfRangeException("metadataToken",
                    String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidToken", tk, this))); 
 
            if (!tk.IsTypeDef && !tk.IsTypeSpec && !tk.IsTypeRef)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveType"), tk, this), "metadataToken"); 

            RuntimeTypeHandle[] typeArgs = ConvertToTypeHandleArray(genericTypeArguments);
            RuntimeTypeHandle[] methodArgs = ConvertToTypeHandleArray(genericMethodArguments);
 
            try
            { 
                Type t = GetModuleHandle().ResolveTypeHandle(metadataToken, typeArgs, methodArgs).GetRuntimeType(); 

                if (t == null) 
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveType"), tk, this), "metadataToken");

                return t;
            } 
            catch (BadImageFormatException e)
            { 
                throw new ArgumentException(Environment.GetResourceString("Argument_BadImageFormatExceptionResolve"), e); 
            }
        } 

        public MemberInfo ResolveMember(int metadataToken)
        {
            return ResolveMember(metadataToken, null, null); 
        }
 
        public MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments) 
        {
            MetadataToken tk = new MetadataToken(metadataToken); 

            if (tk.IsProperty)
                throw new ArgumentException(Environment.GetResourceString("InvalidOperation_PropertyInfoNotAvailable"));
 
            if (tk.IsEvent)
                throw new ArgumentException(Environment.GetResourceString("InvalidOperation_EventInfoNotAvailable")); 
 
            if (tk.IsMethodSpec || tk.IsMethodDef)
                return ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments); 

            if (tk.IsFieldDef)
                return ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
 
            if (tk.IsTypeRef || tk.IsTypeDef || tk.IsTypeSpec)
                return ResolveType(metadataToken, genericTypeArguments, genericMethodArguments); 
 
            if (tk.IsMemberRef)
            { 
                if (!MetadataImport.IsValidToken(tk))
                    throw new ArgumentOutOfRangeException("metadataToken",
                        String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidToken", tk, this)));
 
                ConstArray sig = MetadataImport.GetMemberRefProps(tk);
 
                unsafe 
                {
                    if (*(CorCallingConvention*)sig.Signature.ToPointer() == CorCallingConvention.Field) 
                    {
                        return ResolveField(tk, genericTypeArguments, genericMethodArguments);
                    }
                    else 
                    {
                        return ResolveMethod(tk, genericTypeArguments, genericMethodArguments); 
                    } 
                }
            } 

            throw new ArgumentException("metadataToken",
                String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveMember", tk, this)));
        } 

        public string ResolveString(int metadataToken) 
        { 
            MetadataToken tk = new MetadataToken(metadataToken);
            if (!tk.IsString) 
                throw new ArgumentException(
                    String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Argument_ResolveString"), metadataToken, ToString()));

            if (!MetadataImport.IsValidToken(tk)) 
                throw new ArgumentOutOfRangeException("metadataToken",
                    String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Argument_InvalidToken", tk, this))); 
 
            string str = MetadataImport.GetUserString(metadataToken);
 
            if (str == null)
                throw new ArgumentException(
                    String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Argument_ResolveString"), metadataToken, ToString()));
 
            return str;
        } 
 
        public void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
        { 
            GetModuleHandle().GetPEKind(out peKind, out machine);
        }

 	public int MDStreamVersion 
	{
	    get {  return GetModuleHandle().MDStreamVersion; } 
	} 

        #endregion 

        #region Literals
        private const BindingFlags DefaultLookup = BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public;
        #endregion 

        #region Data Members 
        #pragma warning disable 169 
        // If you add any data members, you need to update the native declaration ReflectModuleBaseObject.
        internal ArrayList          m__TypeBuilderList; 
        internal ISymbolWriter      m__iSymWriter;
        internal ModuleBuilderData  m__moduleData;
        private RuntimeType         m__runtimeType;
        private IntPtr              m__pRefClass; 
        internal IntPtr             m__pData;
        internal IntPtr             m__pInternalSymWriter; 
        private IntPtr              m__pGlobals; 
        private IntPtr              m__pFields;
        internal MethodToken        m__EntryPoint; 
        #pragma warning restore 169
        public override bool Equals(object o)
        {
            if (o == null) 
                return false;
 
            if (!(o is Module)) 
                return false;
 
            Module rhs = o as Module;
            rhs = rhs.InternalModule;
            return (object)InternalModule == (object)rhs;
        } 
        public override int GetHashCode() { return base.GetHashCode(); }
        internal virtual Module InternalModule 
        { 
            get
            { 
                return this;
            }
        }
        internal ArrayList m_TypeBuilderList { get { return InternalModule.m__TypeBuilderList; } set { InternalModule.m__TypeBuilderList = value; } } 
        internal ISymbolWriter m_iSymWriter { get { return InternalModule.m__iSymWriter; } set { InternalModule.m__iSymWriter = value; } }
        internal ModuleBuilderData m_moduleData { get { return InternalModule.m__moduleData; } set { InternalModule.m__moduleData = value; } } 
        private RuntimeType m_runtimeType { get { return InternalModule.m__runtimeType; } set { InternalModule.m__runtimeType = value; } } 
        private IntPtr m_pRefClass { get { return InternalModule.m__pRefClass; } }
        internal IntPtr m_pData { get { return InternalModule.m__pData; } } 
        internal IntPtr m_pInternalSymWriter { get { return InternalModule.m__pInternalSymWriter; } }
        private IntPtr m_pGlobals { get { return InternalModule.m__pGlobals; } }
        private IntPtr m_pFields { get { return InternalModule.m__pFields; } }
        internal MethodToken m_EntryPoint { get { return InternalModule.m__EntryPoint; } set { InternalModule.m__EntryPoint = value; } } 
        #endregion
 
        #region Constructor 
        internal Module()
        { 
            // Construct a new module.  This returns the default dynamic module.
            // 0 is defined as being a module without an entry point (ie a DLL);
            // This must throw because this dies in ToString() when constructed here...
            // throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_Constructor)); 
            //m_EntryPoint=new MethodToken(0);
        } 
        #endregion 

        #region Private Members 
        private FieldInfo InternalGetField(String name, BindingFlags bindingAttr)
        {
            if (RuntimeType == null)
                return null; 

            return RuntimeType.GetField(name, bindingAttr); 
        } 
        #endregion
 
        #region Internal Members
        internal virtual bool IsDynamic()
        {
            return false; 
        }
 
        internal RuntimeType RuntimeType 
        {
            get 
            {
                unsafe
                {
                    if (m_runtimeType == null) 
                        m_runtimeType = GetModuleHandle().GetModuleTypeHandle().GetRuntimeType() as RuntimeType;
 
                    return m_runtimeType; 
                }
            } 
        }
        #endregion

        #region Protected Virtuals 
        protected virtual MethodInfo GetMethodImpl(String name,BindingFlags bindingAttr,Binder binder,
            CallingConventions callConvention, Type[] types,ParameterModifier[] modifiers) 
        { 
            if (RuntimeType == null)
                return null; 

            if (types == null)
            {
                return RuntimeType.GetMethod(name, bindingAttr); 
            }
            else 
            { 
                return RuntimeType.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
            } 
        }

        internal MetadataImport MetadataImport
        { 
            get
            { 
                unsafe 
                {
                    return ModuleHandle.GetMetadataImport(); 
                }
            }
        }
        #endregion 

        #region ICustomAttributeProvider Members 
        public virtual Object[] GetCustomAttributes(bool inherit) 
        {
            return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType); 
        }

        public virtual Object[] GetCustomAttributes(Type attributeType, bool inherit)
        { 
            if (attributeType == null)
                throw new ArgumentNullException("attributeType"); 
 
            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
 
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"attributeType");

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType); 
        }
 
        public virtual bool IsDefined (Type attributeType, bool inherit) 
        {
            if (attributeType == null) 
                throw new ArgumentNullException("attributeType");

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
 
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"caType"); 
 
            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        } 

        #endregion

        #region Public Virtuals 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) 
        { 
            if (info == null)
            { 
                throw new ArgumentNullException("info");
            }
            UnitySerializationHolder.GetUnitySerializationInfo(info, UnitySerializationHolder.ModuleUnity, this.ScopeName, GetAssemblyInternal());
        } 

[System.Runtime.InteropServices.ComVisible(true)] 
        public virtual Type GetType(String className, bool ignoreCase) 
        {
            return GetType(className, false, ignoreCase); 
        }

[System.Runtime.InteropServices.ComVisible(true)]
        public virtual Type GetType(String className) { 
            return GetType(className, false, false);
        } 
 
[System.Runtime.InteropServices.ComVisible(true)]
        public virtual Type GetType(String className, bool throwOnError, bool ignoreCase) 
        {
            return GetTypeInternal(className, throwOnError, ignoreCase);
        }
 
        public virtual String FullyQualifiedName
        { 
            [ResourceExposure(ResourceScope.Machine)] 
            [ResourceConsumption(ResourceScope.Machine)]
            get 
            {
                String fullyQualifiedName = InternalGetFullyQualifiedName();

                if (fullyQualifiedName != null) { 
                    bool checkPermission = true;
                    try { 
                        Path.GetFullPathInternal(fullyQualifiedName); 
                    }
                    catch(ArgumentException) { 
                        checkPermission = false;
                    }
                    if (checkPermission) {
                        new FileIOPermission( FileIOPermissionAccess.PathDiscovery, fullyQualifiedName ).Demand(); 
                    }
                } 
 
                return fullyQualifiedName;
            } 
        }

        public virtual Type[] FindTypes(TypeFilter filter,Object filterCriteria)
        { 
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Type[] c = GetTypesInternal(ref stackMark); 
            int cnt = 0; 
            for (int i = 0;i<c.Length;i++) {
                if (filter!=null && !filter(c[i],filterCriteria)) 
                    c[i] = null;
                else
                    cnt++;
            } 
            if (cnt == c.Length)
                return c; 
 
            Type[] ret = new Type[cnt];
            cnt=0; 
            for (int i=0;i<c.Length;i++) {
                if (c[i] != null)
                    ret[cnt++] = c[i];
            } 
            return ret;
        } 
 
        public virtual Type[] GetTypes()
        { 
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return GetTypesInternal(ref stackMark);
        }
 
        #endregion
 
        #region Public Members 

        public Guid ModuleVersionId 
        {
            get
            {
                unsafe 
                {
                    Guid mvid; 
                    MetadataImport.GetScopeProps(out mvid); 
                    return mvid;
                } 
            }
        }

        public int MetadataToken 
        {
            get 
            { 
                return GetModuleHandle().GetToken();
            } 
        }

        public bool IsResource()
        { 
            return IsResourceInternal();
        } 
 
        public FieldInfo[] GetFields()
        { 
            if (RuntimeType == null)
                return new FieldInfo[0];

            return RuntimeType.GetFields(); 
        }
 
        public FieldInfo[] GetFields(BindingFlags bindingFlags) 
        {
            if (RuntimeType == null) 
                return new FieldInfo[0];

            return RuntimeType.GetFields(bindingFlags);
        } 

        public FieldInfo GetField(String name) 
        { 
            if (name == null)
                throw new ArgumentNullException("name"); 

            return GetField(name,Module.DefaultLookup);
        }
 
        public FieldInfo GetField(String name, BindingFlags bindingAttr)
        { 
            if (name == null) 
                throw new ArgumentNullException("name");
 
            return InternalGetField(name, bindingAttr);
        }

        public MethodInfo[] GetMethods() 
        {
            if (RuntimeType == null) 
                return new MethodInfo[0]; 

            return RuntimeType.GetMethods(); 
        }

        public MethodInfo[] GetMethods(BindingFlags bindingFlags)
        { 
            if (RuntimeType == null)
                return new MethodInfo[0]; 
 
            return RuntimeType.GetMethods(bindingFlags);
        } 

        public MethodInfo GetMethod(
            String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        { 
            if (name == null)
                throw new ArgumentNullException("name"); 
 
            if (types == null)
                throw new ArgumentNullException("types"); 

            for (int i =0;i < types.Length;i++)
            {
                if (types[i] == null) 
                    throw new ArgumentNullException("types");
            } 
 
            return GetMethodImpl(name,bindingAttr,binder,callConvention,types,modifiers);
 
        }

        public MethodInfo GetMethod(String name,Type[] types)
        { 
            if (name == null)
                throw new ArgumentNullException("name"); 
 
            if (types == null)
                throw new ArgumentNullException("types"); 

            for (int i =0;i < types.Length;i++)
            {
                if (types[i] == null) 
                    throw new ArgumentNullException("types");
            } 
 
            return GetMethodImpl(name, Module.DefaultLookup, null, CallingConventions.Any, types, null);
        } 

        public MethodInfo GetMethod(String name)
        {
            if (name == null) 
                throw new ArgumentNullException("name");
            return GetMethodImpl(name,Module.DefaultLookup,null,CallingConventions.Any, 
                null,null); 
        }
 
        public String ScopeName
        {
            get
            { 
                return InternalGetName();
            } 
        } 

        public String Name 
        {
            [ResourceExposure(ResourceScope.None)]
            [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
            get 
            {
                String s = InternalGetFullyQualifiedName(); 
#if !FEATURE_PAL 
                int i = s.LastIndexOf('\\');
#else 
                int i = s.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
#endif
                if (i == -1)
                    return s; 

                return new String(s.ToCharArray(),i+1,s.Length-i-1); 
            } 
        }
 
        public override String ToString()
        {
            return ScopeName;
        } 

        public Assembly Assembly 
        { 
            get
            { 
                return GetAssemblyInternal();
            }
        }
 
        public unsafe ModuleHandle ModuleHandle
        { 
            get 
            {
                // 


                return new ModuleHandle((void*)m_pData);
            } 
        }
 
        internal unsafe ModuleHandle GetModuleHandle() 
        {
            return new ModuleHandle((void*)m_pData); 
        }
#if !FEATURE_PAL
        public System.Security.Cryptography.X509Certificates.X509Certificate GetSignerCertificate()
        { 
            return GetSignerCertificateInternal();
        } 
#endif 
        #endregion
 
        void _Module.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        } 

        void _Module.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo) 
        { 
            throw new NotImplementedException();
        } 

        void _Module.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException(); 
        }
 
        void _Module.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr) 
        {
            throw new NotImplementedException(); 
        }
   }
}
