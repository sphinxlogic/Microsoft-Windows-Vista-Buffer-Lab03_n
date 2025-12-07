namespace System.Windows.Forms.Design { 
    using System.Design;
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System; 
    using System.IO;
    using Microsoft.Win32; 
    using System.CodeDom; 
    using System.CodeDom.Compiler;
    using System.Collections; 
    using System.Collections.Generic;
    using System.Reflection;
    using System.ComponentModel;
    using System.Windows.Forms; 
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis; 
 
    /// <include file='doc\AxWrapperGen.uex' path='docs/doc[@for="AxWrapperGen"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       Generates a wrapper for ActiveX controls for use in the design-time
    ///       environment.
    ///    </para> 
    /// </devdoc>
    /// <internalonly/> 
    public class AxWrapperGen { 
        private String axctlIface;
        private Type   axctlType; 
        private Guid   clsidAx;

        private String axctlEvents;
        private Type   axctlEventsType; 

        private String axctl; 
        private static String axctlNS; 

        private string memIface = null; 
        private string multicaster = null;
        private string cookie = null;

        private bool dispInterface = false; 
        private bool enumerableInterface = false;
 
        private string defMember = null; // Default Member (used for DefaultProperty attribute) 

        private string aboutBoxMethod = null; // Method name that handles the AboutBox(). 

        private CodeFieldReferenceExpression memIfaceRef = null;
        private CodeFieldReferenceExpression multicasterRef = null;
        private CodeFieldReferenceExpression cookieRef = null; 

        private ArrayList events = null; 
        /// <include file='doc\AxWrapperGen.uex' path='docs/doc[@for="AxWrapperGen.GeneratedSources"]/*' /> 
        public static ArrayList GeneratedSources = new ArrayList();
 
        private static Guid Guid_DataSource = new Guid("{7C0FFAB3-CD84-11D0-949A-00A0C91110ED}");

        internal static BooleanSwitch AxWrapper = new BooleanSwitch("AxWrapper", "ActiveX WFW wrapper generation.");
        internal static BooleanSwitch AxCodeGen = new BooleanSwitch("AxCodeGen", "ActiveX WFW property generation."); 

        // Attributes to add the NoBrowse/NoPersis attributes to selected properties. 
        private static CodeAttributeDeclaration nobrowse    = null; 
        private static CodeAttributeDeclaration browse      = null;
        private static CodeAttributeDeclaration nopersist   = null; 
        private static CodeAttributeDeclaration bindable    = null;
        private static CodeAttributeDeclaration defaultBind = null;

        // Optimization caches. 
        //
        private Hashtable axctlTypeMembers; 
        private Hashtable axHostMembers; 
        private Hashtable conflictableThings;
        private static Hashtable classesInNamespace; 
        private static Hashtable axHostPropDescs;

        private ArrayList dataSourceProps = new ArrayList();
 
        /// <include file='doc\AxWrapperGen.uex' path='docs/doc[@for="AxWrapperGen.AxWrapperGen"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public AxWrapperGen(Type axType) { 
            axctl = axType.Name;
            axctl = axctl.TrimStart(new char[]{'_', '1'});
            axctl = "Ax" + axctl;
            clsidAx = axType.GUID; 

            Debug.WriteLineIf(AxWrapper.Enabled, "Found " + axctl + " as the ActiveX control. Guid: " + clsidAx.ToString() + " on : " + axType.FullName); 
 
            object[] custom = axType.GetCustomAttributes(typeof(ComSourceInterfacesAttribute), false);
 
            // If we didn't find the attribute on the class itself, let's see if we can find it on
            // the base type, if the base type happens to be an internal tlbimp helper class.
            //
            if (custom.Length == 0 && axType.BaseType.GUID.Equals(axType.GUID)) { 
                custom = axType.BaseType.GetCustomAttributes(typeof(ComSourceInterfacesAttribute), false);
                Debug.WriteLineIf(custom.Length > 0 && AxWrapper.Enabled, "Found ComSourceInterfacesAttribute in baseType: " + axType.BaseType); 
            } 

            if (custom.Length > 0) { 
                ComSourceInterfacesAttribute coms = (ComSourceInterfacesAttribute)custom[0];

                // The string is a \0 delimited string containing all interfaces implemented on this
                // COM object. The first one is the default events interface. 
                int indexIntf = coms.Value.IndexOfAny(new char[]{(char)0});
                Debug.Assert(indexIntf != -1, "Did not find delimiter in events name string: " + coms.Value); 
 
                string eventName = coms.Value.Substring(0, indexIntf);
                axctlEventsType = axType.Module.Assembly.GetType(eventName); 
                if (axctlEventsType == null) {
                    axctlEventsType = Type.GetType(eventName, false);
                }
                if (axctlEventsType != null) { 
                    axctlEvents = axctlEventsType.FullName;
                } 
 
                Debug.Assert(axctlEventsType != null, "Could not get event interface: " + coms.Value);
                Debug.WriteLineIf(AxWrapper.Enabled, "Assigned: " + axctlEvents + " as events interface"); 
            }
            else
                Debug.WriteLineIf(AxWrapper.Enabled, "No Events Interface defined for: " + axType.Name);
 
            Type[] interfaces = axType.GetInterfaces();
            axctlType = interfaces[0]; 
 
            // Look to see if this interface has a CoClassAttribute. If it does this
            // means that this is a helper interface that in turn derives from the 
            // default OCX interface.
            //
            foreach(Type iface in interfaces) {
                custom = iface.GetCustomAttributes(typeof(CoClassAttribute), false); 
                if (custom.Length > 0) {
                    Type[] ifaces = iface.GetInterfaces(); 
                    Debug.Assert(ifaces != null && ifaces.Length > 0, "No interfaces implemented on the CoClass"); 

                    if (ifaces != null && ifaces.Length > 0) { 
                        axctl = "Ax" + iface.Name;
                        axctlType = ifaces[0];
                        break;
                    } 
                }
            } 
 
            axctlIface = axctlType.Name;
            Debug.WriteLineIf(AxWrapper.Enabled, "Assigned: " + axctlIface + " as default interface"); 

            // Check to see if we want to implement IEnumerable on the ActiveX wrapper.
            //
            foreach(Type t in interfaces) { 
                if (t == typeof(System.Collections.IEnumerable)) {
                    Debug.WriteLineIf(AxWrapper.Enabled, "ActiveX control " + axctlType.FullName + " implements IEnumerable"); 
                    enumerableInterface = true; 
                    break;
                } 
            }

            try {
                // Check to see if the default interface is disp-only. 
                custom = axctlType.GetCustomAttributes(typeof(InterfaceTypeAttribute), false);
                if (custom.Length > 0) { 
                    InterfaceTypeAttribute intfType = (InterfaceTypeAttribute)custom[0]; 
                    dispInterface = (intfType.Value == ComInterfaceType.InterfaceIsIDispatch);
                } 
            }
            catch(MissingMethodException) {
                Debug.WriteLineIf(AxWrapper.Enabled, "The EE is not able to find the right ctor for InterfaceTypeAttribute");
            } 
        }
 
        private Hashtable AxHostMembers { 
            get {
                if (axHostMembers == null) 
                    FillAxHostMembers();
                return axHostMembers;
            }
        } 

        private Hashtable ConflictableThings { 
            get { 
                if (conflictableThings == null)
                    FillConflicatableThings(); 
                return conflictableThings;
            }
        }
 
        private void AddClassToNamespace(CodeNamespace ns, CodeTypeDeclaration cls) {
            if (classesInNamespace == null) { 
                classesInNamespace = new Hashtable(); 
            }
 
            try {
                ns.Types.Add(cls);
                classesInNamespace.Add(cls.Name, cls);
            } 
            catch (Exception e) {
                Debug.Fail("Failed to add " + cls.Name + " to types in Namespace. " + e); 
            } 
            catch {
                Debug.Fail("Failed to add " + cls.Name + " to types in Namespace. non-clscompliant exception encountered"); 
            }
        }

        private EventEntry AddEvent(string name, string eventCls, string eventHandlerCls, Type retType, AxParameterData[] parameters) { 
            if (events == null)
                events = new ArrayList(); 
 
            if (axctlTypeMembers == null) {
                axctlTypeMembers = new Hashtable(); 

                Type t = axctlType;

                MemberInfo[] members = t.GetMembers(); 
                foreach(MemberInfo member in members) {
                    string memberName = member.Name; 
                    if (!axctlTypeMembers.Contains(memberName)) { 
                        axctlTypeMembers.Add(memberName, member);
                    } 
                }
            }

            bool contain = axctlTypeMembers.Contains(name) || AxHostMembers.Contains(name) || ConflictableThings.Contains(name); 
            EventEntry entry = new EventEntry(name, eventCls, eventHandlerCls, retType, parameters, contain);
            events.Add(entry); 
            return entry; 
        }
 
        private bool ClassAlreadyExistsInNamespace(CodeNamespace ns, string clsName) {
            return classesInNamespace.Contains(clsName);
        }
 
        private static string Compile(AxImporter importer, CodeNamespace ns, string[] refAssemblies, DateTime tlbTimeStamp, Version version) {
            CodeDomProvider codeProvider = new Microsoft.CSharp.CSharpCodeProvider(); 
#pragma warning disable 618 
            ICodeGenerator codegen = codeProvider.CreateGenerator();
#pragma warning restore 618 

            // Build up the name of the output dll and the command line for the compiler.
            //
            string outputFileName = importer.options.outputName; 
            Debug.Assert(outputFileName != null, "No output filename!!!");
 
            string outputName = Path.Combine(importer.options.outputDirectory, outputFileName); 
            string fileName = Path.ChangeExtension(outputName, ".cs");
 
            CompilerParameters cparams = new CompilerParameters(refAssemblies, outputName);
            cparams.IncludeDebugInformation = importer.options.genSources;
            CodeCompileUnit cu = new CodeCompileUnit();
            cu.Namespaces.Add(ns); 

            CodeAttributeDeclarationCollection assemblyAttributes = cu.AssemblyCustomAttributes; 
            assemblyAttributes.Add(new CodeAttributeDeclaration("System.Reflection.AssemblyVersion", new CodeAttributeArgument(new CodePrimitiveExpression(version.ToString())))); 
            assemblyAttributes.Add(new CodeAttributeDeclaration("System.Windows.Forms.AxHost.TypeLibraryTimeStamp", new CodeAttributeArgument(new CodePrimitiveExpression(tlbTimeStamp.ToString()))));
            if (importer.options.delaySign) { 
                assemblyAttributes.Add(new CodeAttributeDeclaration("System.Reflection.AssemblyDelaySign", new CodeAttributeArgument(new CodePrimitiveExpression(true))));
            }
            if (importer.options.keyFile != null && importer.options.keyFile.Length > 0) {
                assemblyAttributes.Add(new CodeAttributeDeclaration("System.Reflection.AssemblyKeyFile", new CodeAttributeArgument(new CodePrimitiveExpression(importer.options.keyFile)))); 
            }
            if (importer.options.keyContainer != null && importer.options.keyContainer.Length > 0) { 
                assemblyAttributes.Add(new CodeAttributeDeclaration("System.Reflection.AssemblyKeyName", new CodeAttributeArgument(new CodePrimitiveExpression(importer.options.keyContainer)))); 
            }
 
            // Compile the file into a DLL.
            //
            CompilerResults results;
 
            if (importer.options.genSources) {
                Debug.WriteLineIf(AxWrapperGen.AxWrapper.Enabled, "Compiling " + ns.Name + ".cs" + " to " + outputName); 
                SaveCompileUnit(codegen, cu, fileName); 
                results = ((ICodeCompiler)codegen).CompileAssemblyFromFile(cparams, fileName);
            } 
            else {
                results = ((ICodeCompiler)codegen).CompileAssemblyFromDom(cparams, cu);
            }
 
            // Walk through any errors and warnings and build up the correct exception string if needed.
            // 
            if (results.Errors != null && results.Errors.Count > 0) { 
                string errorText = null;
                CompilerErrorCollection errors = results.Errors; 

                Debug.WriteLineIf(AxWrapperGen.AxWrapper.Enabled, "Errors#: " + errors.Count);
                foreach(CompilerError err in errors) {
                    Debug.WriteLineIf(AxWrapperGen.AxWrapper.Enabled, err.ToString()); 

                    // Skip warnings... 
                    // 
                    if (!err.IsWarning)
                        errorText = errorText + err.ToString() + "\r\n"; 
                }

                if (errorText != null) {
                    SaveCompileUnit(codegen, cu, fileName); 
                    errorText = SR.GetString(SR.AXCompilerError, ns.Name, fileName) + "\r\n" + errorText;
                    throw new Exception(errorText); 
                } 
            }
 
            return outputName;
        }

        private string CreateDataSourceFieldName(string propName) { 
            return "ax" + propName;
        } 
 
        private CodeParameterDeclarationExpression CreateParamDecl(string type, string name, bool isOptional) {
            CodeParameterDeclarationExpression paramDecl = new CodeParameterDeclarationExpression(type, name); 

            if (!isOptional)
                return paramDecl;
 
            CodeAttributeDeclarationCollection paramAttrs = new CodeAttributeDeclarationCollection();
            paramAttrs.Add(new CodeAttributeDeclaration("System.Runtime.InteropServices.Optional", new CodeAttributeArgument[0])); 
            paramDecl.CustomAttributes = paramAttrs; 
            return paramDecl;
        } 

        private CodeConditionStatement CreateValidStateCheck() {
            CodeConditionStatement ifstat = new CodeConditionStatement();
            CodeBinaryOperatorExpression cond1; 
            CodeBinaryOperatorExpression cond2;
            CodeBinaryOperatorExpression condAnd; 
 
            cond1 = new CodeBinaryOperatorExpression(memIfaceRef, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null));
            cond2 = new CodeBinaryOperatorExpression(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "PropsValid"), 
                                                     CodeBinaryOperatorType.IdentityEquality,
                                                     new CodePrimitiveExpression(true));
            condAnd = new CodeBinaryOperatorExpression(cond1, CodeBinaryOperatorType.BooleanAnd, cond2);
 
            ifstat = new CodeConditionStatement();
            ifstat.Condition = condAnd; 
            return ifstat; 
        }
 
        private CodeStatement CreateInvalidStateException(string name, string kind) {
            CodeBinaryOperatorExpression cond = new CodeBinaryOperatorExpression(memIfaceRef,
                                                                                 CodeBinaryOperatorType.IdentityEquality,
                                                                                 new CodePrimitiveExpression(null)); 
            CodeConditionStatement ifstat = new CodeConditionStatement();
            ifstat.Condition = cond; 
 
            CodeExpression[] createParams = new CodeExpression[] {
                new CodePrimitiveExpression(name), 
                new CodeFieldReferenceExpression(new CodeFieldReferenceExpression(null, typeof(AxHost).FullName + ".ActiveXInvokeKind"), kind)
            };

            CodeObjectCreateExpression invalidState = new CodeObjectCreateExpression(typeof(AxHost.InvalidActiveXStateException).FullName, createParams); 

            ifstat.TrueStatements.Add(new CodeThrowExceptionStatement(invalidState)); 
            return ifstat; 
        }
 
        private void FillAxHostMembers() {
            if (axHostMembers == null) {
                axHostMembers = new Hashtable(StringComparer.CurrentCultureIgnoreCase);
 
                Type t = typeof(AxHost);
 
                MemberInfo[] members = t.GetMembers(); 
                foreach(MemberInfo member in members) {
                    string memberName = member.Name; 

                    if (!axHostMembers.Contains(memberName)) {
                        // Check to see if this is a field
                        // 
                        FieldInfo fi = member as FieldInfo;
                        if (fi != null && !fi.IsPrivate) { 
                            axHostMembers.Add(memberName, member); 
                            continue;
                        } 

                        // Check to see if this is a property
                        //
                        PropertyInfo pi = member as PropertyInfo; 
                        if (pi != null) {
                            axHostMembers.Add(memberName, member); 
                            continue; 
                        }
 
                        // Check to see if this is a ctor or method.
                        //
                        MethodBase mb = member as MethodBase;
                        if (mb != null && !mb.IsPrivate) { 
                            axHostMembers.Add(memberName, member);
                            continue; 
                        } 

                        // Check to see if this is a ctor or method. 
                        //
                        EventInfo ei = member as EventInfo;
                        if (ei != null) {
                            axHostMembers.Add(memberName, member); 
                            continue;
                        } 
 
                        // Check to see if this is a ctor or method.
                        // 
                        Type type = member as Type;
                        if (type != null && (type.IsPublic || type.IsNestedPublic)) {
                            axHostMembers.Add(memberName, member);
                            continue; 
                        }
 
                        Debug.Fail("Failed to process AxHost member " + member.ToString() + " " + member.GetType().FullName); 
                        axHostMembers.Add(memberName, member);
                    } 
                }
            }
        }
 
        private void FillConflicatableThings() {
            if (conflictableThings == null) { 
                conflictableThings = new Hashtable(); 
                conflictableThings.Add("System", "System");
            } 
        }

        private static void SaveCompileUnit(ICodeGenerator codegen, CodeCompileUnit cu, string fileName) {
            // Persist to file. 
            Debug.WriteLineIf(AxWrapperGen.AxWrapper.Enabled, "Generating source file: " + fileName);
            try { 
                try { 
                    if (File.Exists(fileName))
                        File.Delete(fileName); 
                }
                catch {
                    Debug.WriteLineIf(AxWrapperGen.AxWrapper.Enabled, "Could not delete: " + fileName);
                } 

                FileStream file = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite); 
                StreamWriter stream = new StreamWriter(file, new System.Text.UTF8Encoding(false)); 
                codegen.GenerateCodeFromCompileUnit(cu, stream, null);
                stream.Flush(); 
                stream.Close();
                file.Close();
                GeneratedSources.Add( fileName );
                Debug.WriteLineIf(AxWrapperGen.AxWrapper.Enabled, "Generated source file: " + fileName); 
            }
            catch (Exception e) { 
                Debug.WriteLineIf(AxWrapperGen.AxWrapper.Enabled, "Exception generating source file: " + e.ToString()); 
            }
            catch { 
                Debug.WriteLineIf(AxWrapperGen.AxWrapper.Enabled, "Non CLSCompliant Exception generating source file: ");
            }
        }
 
        internal static string MapTypeName(Type type) {
            bool isArray = type.IsArray; 
 
            Type baseType = type.GetElementType();
            if (baseType != null) 
                type = baseType;

            string typeName = type.FullName;
            return (isArray) ? (typeName + "[]") : typeName; 
        }
 
        private static bool IsTypeActiveXControl(Type type) { 
            if (type.IsClass && type.IsCOMObject && type.IsPublic && !type.GUID.Equals(Guid.Empty)) {
 
                // Check to see if the type is ComVisible. Otherwise, this is a internal helper type from tlbimp.
                //
                try {
                    object[] attrs = type.GetCustomAttributes(typeof(ComVisibleAttribute), false); 
                    if (attrs.Length != 0 && ((ComVisibleAttribute)attrs[0]).Value == false) {
                        return false; 
                    } 
                }
                catch { 
                    return false;
                }

                // Look for the Control key under the Classes_Root\CLSID to see if this is the ActiveX control. 
                //
                Guid clsid = type.GUID; 
                string controlKey = "CLSID\\{" + clsid.ToString() + "}\\Control"; 
                RegistryKey k = Registry.ClassesRoot.OpenSubKey(controlKey);
                if (k == null) 
                    return false;

                k.Close();
                Debug.WriteLineIf(AxWrapper.Enabled, "Found key: " + controlKey); 

                // Make sure this type implements atleast the default interface. 
                // 
                Type[] ifaces = type.GetInterfaces();
                Debug.WriteLineIf(ifaces.Length < 1 && AxWrapper.Enabled, "Not even one interface implemented on: " + type.FullName); 

                if (ifaces != null && ifaces.Length >= 1)
                    return true;
            } 

            return false; 
        } 

        /// <include file='doc\AxWrapperGen.uex' path='docs/doc[@for="AxWrapperGen.GenerateWrappers"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal static string GenerateWrappers(AxImporter importer, Guid axClsid, Assembly rcwAssem, string[] refAssemblies, DateTime tlbTimeStamp, out string assem) { 
            assem = null;
 
            bool fFoundClass = false; 
            CodeNamespace ns = null;
            string retAxCtlName = null; 

            try {
                Type[] types = rcwAssem.GetTypes();
                for (int i = 0; i < types.Length; ++i) { 
                    if (IsTypeActiveXControl(types[i])) {
                        fFoundClass = true; 
 
                        // Create a namespace for the AxWrappers.
                        // 
                        if (ns == null) {
                            // Determine the namespace for the ActiveX control wrappers.
                            //
                            axctlNS = "Ax" + types[i].Namespace; 
                            ns = new CodeNamespace(axctlNS);
                        } 
 
                        // Generate code for the ActiveX control wrapper.
                        // 
                        AxWrapperGen axwrapper = new AxWrapperGen(types[i]);
                        axwrapper.GenerateAxHost(ns, refAssemblies);

                        // If we are given a specific GUID, then we should return the type for that control, 
                        // otherwise, we will return the type of the first ActiveX control that we generate
                        // wrapper for. 
                        // 
                        if (!axClsid.Equals(Guid.Empty) && axClsid.Equals(types[i].GUID)) {
                            Debug.Assert(retAxCtlName == null, "Two controls match the same GUID... " + retAxCtlName + " and " + types[i].FullName); 
                            retAxCtlName = axwrapper.axctl;
                        }
                        else if (axClsid.Equals(Guid.Empty) && retAxCtlName == null) {
                            retAxCtlName = axwrapper.axctl; 
                        }
                    } 
                } 
            }
            finally { 
                if (classesInNamespace != null) {
                    classesInNamespace.Clear();
                    classesInNamespace = null;
                } 
            }
 
            AssemblyName an = rcwAssem.GetName(); 

            if (fFoundClass) { 
                // Now that we found atleast one ActiveX control, we should compile the namespace into
                // an assembly.
                //
                Debug.Assert(ns != null, "ActiveX control found but no code generated!!!!"); 

                Version version = an.Version; 
                assem = Compile(importer, ns, refAssemblies, tlbTimeStamp, version); 

                // Return the type of the ActiveX control. 
                //
                if (assem != null) {
                    if (retAxCtlName == null)
                        throw new Exception(SR.GetString(SR.AXNotValidControl, "{" + axClsid + "}")); 

                    return axctlNS + "." + retAxCtlName + "," + axctlNS; 
                } 
            }
#if DEBUG 
            else {
                Debug.WriteLineIf(AxWrapper.Enabled, "Did not find any ActiveX control in: " + an.Name);
            }
#endif // DEBUG 

            return null; 
        } 

        private void GenerateAxHost(CodeNamespace ns, string[] refAssemblies) { 
            CodeTypeDeclaration cls = new CodeTypeDeclaration();
            cls.Name = axctl;
            cls.BaseTypes.Add(typeof(AxHost).FullName);
 
            if (enumerableInterface) {
                cls.BaseTypes.Add(typeof(System.Collections.IEnumerable)); 
            } 

            CodeAttributeDeclarationCollection clsAttrs = new CodeAttributeDeclarationCollection(); 

            CodeAttributeDeclaration guidAttr = new CodeAttributeDeclaration(typeof(System.Windows.Forms.AxHost.ClsidAttribute).FullName,
                                                         new CodeAttributeArgument[] {new CodeAttributeArgument(new CodeSnippetExpression("\"{" + clsidAx.ToString() + "}\""))});
 
            clsAttrs.Add(guidAttr);
 
            // Generate the DesignTimeVisible attribute so that the control shows up in the toolbox. 
            //
 
            CodeAttributeDeclaration designAttr = new CodeAttributeDeclaration(typeof(System.ComponentModel.DesignTimeVisibleAttribute).FullName,
                                                         new CodeAttributeArgument[] {new CodeAttributeArgument(new CodePrimitiveExpression(true))});

            clsAttrs.Add(designAttr); 

            cls.CustomAttributes = clsAttrs; 
 
            // See if there is a DefaultAttribute on the interface. If so, convert it to a DefaultPropertyAttribute.
            object[] attr = axctlType.GetCustomAttributes(typeof(System.Reflection.DefaultMemberAttribute), true); 
            if (attr != null && attr.Length > 0) {
                defMember = ((DefaultMemberAttribute)attr[0]).MemberName;
            }
 
            AddClassToNamespace(ns, cls);
 
            WriteMembersDecl(cls); 

            if (axctlEventsType != null) { 
                WriteEventMembersDecl(ns, cls);
            }

            CodeConstructor ctor = WriteConstructor(cls); 

            WriteProperties(cls); 
            WriteMethods(cls); 

            WriteHookupMethods(cls); 

            // Hookup the AboutBox delegate if one exists for this control.
            //
            if (aboutBoxMethod != null) { 
                CodeObjectCreateExpression aboutDelegate = new CodeObjectCreateExpression("AboutBoxDelegate");
                aboutDelegate.Parameters.Add(new CodeFieldReferenceExpression(null, aboutBoxMethod)); 
 
                CodeMethodInvokeExpression aboutAdd = new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetAboutBoxDelegate");
                aboutAdd.Parameters.Add(aboutDelegate); 

                ctor.Statements.Add(new CodeExpressionStatement(aboutAdd));
            }
 
            if (axctlEventsType != null)
                WriteEvents(ns, cls); 
 
            // If there is a need to generate a override for OnInPlaceActive()
            // do it now. 
            //
            if (dataSourceProps.Count > 0) {
                WriteOnInPlaceActive(cls);
            } 
        }
 
        private CodeExpression GetInitializer(Type type) { 
            if (type == null)
                return new CodePrimitiveExpression(null); 
            else if (type == typeof(Int32) || type == typeof(short) || type == typeof(Int64) || type == typeof(Single) || type == typeof(Double) || typeof(Enum).IsAssignableFrom(type))
                return new CodePrimitiveExpression(0);
            else if (type == typeof(char))
                return new CodeCastExpression("System.Character", new CodePrimitiveExpression(0)); 
            else if (type == typeof(bool))
                return new CodePrimitiveExpression(false); 
            else 
                return new CodePrimitiveExpression(null);
        } 

        private bool IsDispidKnown(int dp, string propName) {
            return dp == NativeMethods.ActiveX.DISPID_FORECOLOR ||
                   dp == NativeMethods.ActiveX.DISPID_BACKCOLOR || 
                   dp == NativeMethods.ActiveX.DISPID_FONT ||
                   dp == NativeMethods.ActiveX.DISPID_ENABLED || 
                   dp == NativeMethods.ActiveX.DISPID_TABSTOP || 
                   dp == NativeMethods.ActiveX.DISPID_RIGHTTOLEFT ||
                   dp == NativeMethods.ActiveX.DISPID_TEXT || 
                   dp == NativeMethods.ActiveX.DISPID_HWND ||
                   (dp == NativeMethods.ActiveX.DISPID_VALUE && propName.Equals(defMember));
        }
 
        private bool IsEventPresent(MethodInfo mievent) {
            //return TypeDescriptor.GetEvent(typeof(AxHost), eventsRef[i].Name) != null; 
 
            return false;
 
            /*
            Type axHostType = typeof(AxHost);
            ParameterInfo[] parameters = mievent.GetParameters();
 
            Type[] paramList = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; ++i) 
                paramList[i] = parameters[i].ParameterType; 

            try { 
                MethodInfo mi = axHostType.GetMethod("RaiseOn" + mievent.Name, paramList);
                bool f = (mi != null) && (mi.ReturnType == mievent.ReturnType);
                return f;
            } 
            catch (AmbiguousMatchException) {
                return true; 
            } 
            */
        } 

        private bool IsPropertyBindable(PropertyInfo pinfo, out bool isDefaultBind) {
            isDefaultBind = false;
 
            MethodInfo getter = pinfo.GetGetMethod();
            if (getter == null) 
                return false; 

            object[] attr = getter.GetCustomAttributes(typeof(TypeLibFuncAttribute), false); 
            if (attr != null && attr.Length > 0) {
                TypeLibFuncFlags flags = ((TypeLibFuncAttribute)attr[0]).Value;

                isDefaultBind = ((int)flags & (int)TypeLibFuncFlags.FDefaultBind) != 0; 

                if (isDefaultBind || ((int)flags & (int)TypeLibFuncFlags.FBindable) != 0) { 
                    return true; 
                }
            } 

            return false;
        }
 
        private bool IsPropertyBrowsable(PropertyInfo pinfo, ComAliasEnum alias) {
            MethodInfo getter = pinfo.GetGetMethod(); 
            if (getter == null) 
                return false;
 
            object[] attr = getter.GetCustomAttributes(typeof(TypeLibFuncAttribute), false);
            if (attr != null && attr.Length > 0) {
                TypeLibFuncFlags flags = ((TypeLibFuncAttribute)attr[0]).Value;
                if (((int)flags & (int)TypeLibFuncFlags.FNonBrowsable) != 0 || ((int)flags & (int)TypeLibFuncFlags.FHidden) != 0) { 
                    return false;
                } 
            } 

            // Hide all properties that have COM objects that are not of the DataSource type 
            // and do not have their type converted to a Windows Forms type.
            //
            Type t = pinfo.PropertyType;
            if (alias == ComAliasEnum.None && t.IsInterface && !t.GUID.Equals(Guid_DataSource)) { 
                return false;
            } 
 
            return true;
        } 

        private bool IsPropertySignature(PropertyInfo pinfo, out bool useLet) {
            int nParams = 0;
            bool isProperty = true; 

            useLet = false; 
 
            // Handle Indexed properties.
            string defProp = ((defMember == null) ? "Item" : defMember); 
            if (pinfo.Name.Equals(defProp))
                nParams = pinfo.GetIndexParameters().Length;

            if (pinfo.GetGetMethod() != null) 
                isProperty = IsPropertySignature(pinfo.GetGetMethod(), pinfo.PropertyType, true, nParams);
            if (pinfo.GetSetMethod() != null) { 
                isProperty = isProperty && IsPropertySignature(pinfo.GetSetMethod(), pinfo.PropertyType, false, nParams + 1); 

                if (!isProperty) { 
                    MethodInfo letMethod = pinfo.DeclaringType.GetMethod("let_" + pinfo.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    if (letMethod != null) {
                        isProperty = IsPropertySignature(letMethod, pinfo.PropertyType, false, nParams + 1);
                        useLet = true; 
                    }
                } 
            } 

            return isProperty; 
        }

        private bool IsPropertySignature(MethodInfo method, out bool hasPropInfo, out bool useLet) {
            useLet = false; 
            hasPropInfo = false;
 
            bool getter = method.Name.StartsWith("get_"); 
            if (!getter && !method.Name.StartsWith("set_") && !method.Name.StartsWith("let_"))
                return false; 

            string propName = method.Name.Substring(4, method.Name.Length - 4);
            PropertyInfo pinfo = axctlType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
 
            Debug.Assert(pinfo != null, "No property found for:" + propName);
            if (pinfo == null) 
                return false; 

            return IsPropertySignature(pinfo, out useLet); 
        }

        private bool IsPropertySignature(MethodInfo method, Type returnType, bool getter, int nParams) {
            if (method.IsConstructor) return false; 

            // If there is a property of the same name, we handle it differently. 
            if (getter) { 
                Debug.Assert(method.Name.StartsWith("get_"), "Property get: " + method.Name + " does not start with get_!!!");
                String name = method.Name.Substring(4); 
                if (axctlType.GetProperty(name) != null && method.GetParameters().Length == nParams)
                    return method.ReturnType == returnType;
            }
            else { 
                Debug.Assert(method.Name.StartsWith("set_") || method.Name.StartsWith("let_"), "Property set: " + method.Name + " does not start with set_ or a let_!!!");
                String name = method.Name.Substring(4); 
                ParameterInfo[] parameters = method.GetParameters(); 
                if (axctlType.GetProperty(name) != null && parameters.Length == nParams) {
                    if (parameters.Length > 0) 
                        return parameters[parameters.Length-1].ParameterType == returnType ||
                            (method.Name.StartsWith("let_") && parameters[parameters.Length-1].ParameterType == typeof(object));
                    return true;
                } 
            }
 
            return false; 
        }
 

        //VSQFE #830 addition
        private bool OptionalsPresent(MethodInfo method) {
            AxParameterData[] parameters = AxParameterData.Convert(method.GetParameters()); 

            if (parameters != null && parameters.Length > 0) { 
                for (int iparam = 0; iparam < parameters.Length; ++iparam) { 
                    if (parameters[iparam].IsOptional) {
                        return true; 
                    }
                }
            }
 
            return false;
        } 
 

        private string ResolveConflict(string name, Type returnType, out bool fOverride, out bool fUseNew) { 
            fOverride = false;
            fUseNew = false;

            string prefix = ""; 
            try {
                if (axHostPropDescs == null) { 
                    axHostPropDescs = new Hashtable(); 

                    PropertyInfo[] props = typeof(AxHost).GetProperties(); 
                    foreach(PropertyInfo prop in props) {
                        axHostPropDescs.Add(prop.Name + prop.PropertyType.GetHashCode(), prop);
                    }
                } 

                PropertyInfo pinfo = (PropertyInfo)axHostPropDescs[name + returnType.GetHashCode()]; 
                if (pinfo != null) { 
                    if (returnType.Equals(pinfo.PropertyType)) {
                        bool isVirtual = false; 
                        isVirtual = (pinfo.CanRead) ? pinfo.GetGetMethod().IsVirtual : false;

                        if (isVirtual)
                            fOverride = true; 
                        else
                            fUseNew = true; 
                    } 
                    else {
                        prefix = "Ctl"; 
                    }
                }
                else {
                    if (AxHostMembers.Contains(name) || ConflictableThings.Contains(name)) { 
                        prefix = "Ctl";
                    } 
                    else { 
                        if (name.StartsWith("get_") || name.StartsWith("set_")) {
                            if (TypeDescriptor.GetProperties(typeof(AxHost))[name.Substring(4)] != null) 
                               prefix = "Ctl";
                        }
                    }
                } 
            }
            catch (AmbiguousMatchException) { 
                prefix = "Ctl"; 
            }
 
    #if DEBUG
            if (fOverride)
                Debug.Assert(prefix.Length == 0, "Have override and Ctl prefix for: " + name);
            if (fUseNew) 
                Debug.Assert(prefix.Length == 0, "Have new and Ctl prefix for: " + name);
            if (AxCodeGen.Enabled && prefix.Length != 0) Debug.WriteLine("Resolved conflict for: " + name); 
            if (AxCodeGen.Enabled && fOverride) Debug.WriteLine("Resolved conflict for: " + name + " through override"); 
            if (AxCodeGen.Enabled && fUseNew) Debug.WriteLine("Resolved conflict for: " + name + " through new");
    #endif 
            return prefix;
        }

        private CodeConstructor WriteConstructor(CodeTypeDeclaration cls) { 
            CodeConstructor ctor = new CodeConstructor();
            ctor.Attributes = MemberAttributes.Public; 
 
            ctor.BaseConstructorArgs.Add(new CodeSnippetExpression("\"" + clsidAx.ToString() + "\""));
            cls.Members.Add(ctor); 
            return ctor;
        }

        private void WriteOnInPlaceActive(CodeTypeDeclaration cls) { 
            CodeMemberMethod oipMeth = new CodeMemberMethod();
            oipMeth.Name = "OnInPlaceActive"; 
            oipMeth.Attributes = MemberAttributes.Family | MemberAttributes.Override; 

            CodeMethodInvokeExpression baseOip = new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), "OnInPlaceActive"); 
            oipMeth.Statements.Add(new CodeExpressionStatement(baseOip));

            foreach(PropertyInfo pinfo in dataSourceProps) {
                string fieldName = CreateDataSourceFieldName(pinfo.Name); 

                CodeBinaryOperatorExpression cond = new CodeBinaryOperatorExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), 
                                                                                     CodeBinaryOperatorType.IdentityInequality, 
                                                                                     new CodePrimitiveExpression(null));
                CodeConditionStatement ifstat = new CodeConditionStatement(); 
                ifstat.Condition = cond;

                CodeExpression left  = new CodeFieldReferenceExpression(memIfaceRef, pinfo.Name);
                CodeExpression right = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName); 
                ifstat.TrueStatements.Add(new CodeAssignStatement(left, right));
 
                oipMeth.Statements.Add(ifstat); 
            }
 
            cls.Members.Add(oipMeth);
        }

        private String WriteEventClass(CodeNamespace ns, MethodInfo mi, ParameterInfo[] pinfos) { 
            String evntCls = axctlEventsType.Name + "_" + mi.Name + "Event";
            if (ClassAlreadyExistsInNamespace(ns, evntCls)) { 
                return evntCls; 
            }
 
            CodeTypeDeclaration cls = new CodeTypeDeclaration();
            cls.Name = evntCls;

            AxParameterData[] parameters = AxParameterData.Convert(pinfos); 
            for (int i = 0; i < parameters.Length; ++i) {
                CodeMemberField field = new CodeMemberField(parameters[i].TypeName, parameters[i].Name); 
                field.Attributes = MemberAttributes.Public | MemberAttributes.Final; 
                cls.Members.Add(field);
            } 

            CodeConstructor ctor = new CodeConstructor();
            ctor.Attributes = MemberAttributes.Public;
 
            for (int i = 0; i < parameters.Length; ++i) {
                if (parameters[i].Direction != FieldDirection.Out) { 
                    ctor.Parameters.Add(CreateParamDecl(parameters[i].TypeName, parameters[i].Name, false)); 

                    CodeFieldReferenceExpression left = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), parameters[i].Name); 
                    CodeFieldReferenceExpression right = new CodeFieldReferenceExpression(null, parameters[i].Name);
                    CodeAssignStatement assign = new CodeAssignStatement(left, right);
                    ctor.Statements.Add(assign);
                } 
            }
            cls.Members.Add(ctor); 
 
            AddClassToNamespace(ns, cls);
            return evntCls; 
        }

        private String WriteEventHandlerClass(CodeNamespace ns, MethodInfo mi) {
            String evntCls = axctlEventsType.Name + "_" + mi.Name + "EventHandler"; 
            if (ClassAlreadyExistsInNamespace(ns, evntCls)) {
                return evntCls; 
            } 

            CodeTypeDelegate cls = new CodeTypeDelegate(); 
            cls.Name = evntCls;
            cls.Parameters.Add(CreateParamDecl(typeof(object).FullName, "sender", false));
            cls.Parameters.Add(CreateParamDecl(axctlEventsType.Name + "_" + mi.Name + "Event", "e", false));
            cls.ReturnType = new CodeTypeReference(mi.ReturnType); 

            AddClassToNamespace(ns, cls); 
            return evntCls; 
        }
 
        private void WriteEventMembersDecl(CodeNamespace ns, CodeTypeDeclaration cls) {
            bool eventAttr = false;

            MethodInfo[] events = axctlEventsType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly); 

            for (int i = 0; i < events.Length; ++i) { 
                EventEntry entry = null; 

                if (!IsEventPresent(events[i])) { 
                    ParameterInfo[] parameters = events[i].GetParameters();

                    if (parameters.Length > 0 || events[i].ReturnType != typeof(void)) {
                        String eventHCls = WriteEventHandlerClass(ns, events[i]); 
                        String eventCls = WriteEventClass(ns, events[i], parameters);
                        entry = AddEvent(events[i].Name, eventCls, eventHCls, events[i].ReturnType, AxParameterData.Convert(parameters)); 
                    } 
                    else {
                        entry = AddEvent(events[i].Name, "System.EventArgs", "System.EventHandler", typeof(void), new AxParameterData[0]); 
                    }
                }

                if (!eventAttr) { 
                    object[] attrs = events[i].GetCustomAttributes(typeof(DispIdAttribute), false);
                    if (attrs == null || attrs.Length == 0) { 
                        continue; 
                    }
 
                    DispIdAttribute dispid = (DispIdAttribute)attrs[0];
                    if (dispid.Value == 1) {
                        string eventName = (entry != null) ? entry.resovledEventName : events[i].Name;
 
                        CodeAttributeDeclaration defEventAttr = new CodeAttributeDeclaration("System.ComponentModel.DefaultEvent",
                                                                     new CodeAttributeArgument[] {new CodeAttributeArgument(new CodePrimitiveExpression(eventName))}); 
                        cls.CustomAttributes.Add(defEventAttr); 
                        eventAttr = true;
                    } 
                }
            }

            Debug.WriteLineIf(AxCodeGen.Enabled && !eventAttr, "No default event found for: " + axctlEventsType.FullName); 
        }
 
        private string WriteEventMulticaster(CodeNamespace ns) { 
            String evntCls = axctl + "EventMulticaster";
            if (ClassAlreadyExistsInNamespace(ns, evntCls)) { 
                return evntCls;
            }

            CodeTypeDeclaration cls = new CodeTypeDeclaration(); 
            cls.Name = evntCls;
            cls.BaseTypes.Add(axctlEvents); 
 
            // Set the ClassInterface attribute
            CodeAttributeDeclarationCollection clsAttrs = new CodeAttributeDeclarationCollection(); 
            CodeAttributeDeclaration clsIfaceType = new CodeAttributeDeclaration("System.Runtime.InteropServices.ClassInterface", new CodeAttributeArgument[] {
                                                                                                                                                              new CodeAttributeArgument(new CodeFieldReferenceExpression(
                                                                                                                                                              new CodeFieldReferenceExpression(null, "System.Runtime.InteropServices.ClassInterfaceType"), "None"))
                                                                                                                                                          }); 
            clsAttrs.Add(clsIfaceType);
            cls.CustomAttributes = clsAttrs; 
 
            CodeMemberField field = new CodeMemberField(axctl, "parent");
            field.Attributes = MemberAttributes.Private | MemberAttributes.Final; 
            cls.Members.Add(field);

            CodeConstructor ctor = new CodeConstructor();
            ctor.Attributes = MemberAttributes.Public; 
            ctor.Parameters.Add(CreateParamDecl(axctl, "parent", false));
 
            CodeFieldReferenceExpression right = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "parent"); 
            CodeFieldReferenceExpression left  = new CodeFieldReferenceExpression(null, "parent");
            ctor.Statements.Add(new CodeAssignStatement(right, left)); 
            cls.Members.Add(ctor);

            MethodInfo[] eventsRef = axctlEventsType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            int iEvent = 0; 
            for (int i = 0; i < eventsRef.Length; ++i) {
                AxParameterData[] parameters = AxParameterData.Convert(eventsRef[i].GetParameters()); 
 
                CodeMemberMethod method = new CodeMemberMethod();
                method.Name = eventsRef[i].Name; 
                method.Attributes = MemberAttributes.Public;
                method.ReturnType = new CodeTypeReference(MapTypeName(eventsRef[i].ReturnType));

                for (int iparam = 0; iparam < parameters.Length; ++iparam) { 
                    CodeParameterDeclarationExpression param = CreateParamDecl(MapTypeName(parameters[iparam].ParameterType), parameters[iparam].Name, parameters[iparam].IsOptional);
                    param.Direction = parameters[iparam].Direction; 
                    method.Parameters.Add(param); 
                }
 
                CodeFieldReferenceExpression parent = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "parent");
                if (!IsEventPresent(eventsRef[i])) {
                    EventEntry ee = (EventEntry)events[iEvent++];
 
                    Debug.Assert(eventsRef[i].Name.Equals(ee.eventName), "Not hadling the right event!!!");
 
                    CodeExpressionCollection paramstr = new CodeExpressionCollection(); 
                    paramstr.Add(parent);
                    if (ee.eventCls.Equals("EventArgs")) { 
                        paramstr.Add(new CodeFieldReferenceExpression(new CodeFieldReferenceExpression(null, "EventArgs"), "Empty"));

                        CodeExpression[] temp = new CodeExpression[paramstr.Count];
                        ((IList)paramstr).CopyTo(temp, 0); 
                        CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(parent, ee.invokeMethodName, temp);
                        if (eventsRef[i].ReturnType == typeof(void)) { 
                            method.Statements.Add(new CodeExpressionStatement(methodInvoke)); 
                        }
                        else { 
                            method.Statements.Add(new CodeMethodReturnStatement(methodInvoke));
                        }
                    }
                    else { 
                        CodeObjectCreateExpression create = new CodeObjectCreateExpression(ee.eventCls);
                        for (int iparam = 0; iparam < ee.parameters.Length; ++iparam) { 
                            if (!ee.parameters[iparam].IsOut) 
                                create.Parameters.Add(new CodeFieldReferenceExpression(null, ee.parameters[iparam].Name));
                        } 

                        CodeVariableDeclarationStatement evtfield = new CodeVariableDeclarationStatement(ee.eventCls, ee.eventParam);
                        evtfield.InitExpression = create;
                        method.Statements.Add(evtfield); 

                        paramstr.Add(new CodeFieldReferenceExpression(null, ee.eventParam)); 
 
                        CodeExpression[] temp = new CodeExpression[paramstr.Count];
                        ((IList)paramstr).CopyTo(temp, 0); 
                        CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(parent, ee.invokeMethodName, temp);

                        if (eventsRef[i].ReturnType == typeof(void)) {
                            method.Statements.Add(new CodeExpressionStatement(methodInvoke)); 
                        }
                        else { 
                            CodeVariableDeclarationStatement tempVar = new CodeVariableDeclarationStatement(ee.retType, ee.invokeMethodName); 
                            method.Statements.Add(tempVar);
                            method.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(null, tempVar.Name), methodInvoke)); 
                        }

                        for (int j = 0; j < parameters.Length; ++j) {
                            if (parameters[j].IsByRef) { 
                                method.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(null, parameters[j].Name),
                                                                                  new CodeFieldReferenceExpression( 
                                                                                    new CodeFieldReferenceExpression(null, evtfield.Name), 
                                                                                    parameters[j].Name)));
                            } 
                        }

                        if (eventsRef[i].ReturnType != typeof(void)) {
                            method.Statements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(null, ee.invokeMethodName))); 
                        }
                    } 
                } 
                else {
                    CodeExpressionCollection paramstr = new CodeExpressionCollection(); 
                    for (int iparam = 0; iparam < parameters.Length; ++iparam)
                        paramstr.Add(new CodeFieldReferenceExpression(null, parameters[iparam].Name));

                    CodeExpression[] temp = new CodeExpression[paramstr.Count]; 
                    ((IList)paramstr).CopyTo(temp, 0);
                    CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(parent, "RaiseOn" + eventsRef[i].Name, temp); 
 
                    if (eventsRef[i].ReturnType == typeof(void)) {
                        method.Statements.Add(new CodeExpressionStatement(methodInvoke)); 
                    }
                    else {
                        method.Statements.Add(new CodeMethodReturnStatement(methodInvoke));
                    } 
                }
 
                cls.Members.Add(method); 
            }
 
            AddClassToNamespace(ns, cls);
            return evntCls;
        }
 
        private void WriteEvents(CodeNamespace ns, CodeTypeDeclaration cls) {
            for (int i = 0; events != null && i < events.Count; ++i) { 
                EventEntry evententry = (EventEntry)events[i]; 

                Debug.WriteLineIf(AxCodeGen.Enabled, "Processing event: " + evententry.eventName); 

                CodeMemberEvent e = new CodeMemberEvent();
                e.Name = evententry.resovledEventName;
                e.Attributes = evententry.eventFlags; 

                e.Type = new CodeTypeReference(evententry.eventHandlerCls); 
                cls.Members.Add(e); 

                //Generate the "RaiseXXX" method. 
                CodeMemberMethod cmm = new CodeMemberMethod();
                cmm.Name = evententry.invokeMethodName;
                cmm.ReturnType = new CodeTypeReference(evententry.retType);
                cmm.Attributes = MemberAttributes.Assembly | MemberAttributes.Final; 
                cmm.Parameters.Add(CreateParamDecl(MapTypeName(typeof(object)), "sender", false));
                cmm.Parameters.Add(CreateParamDecl(evententry.eventCls, "e", false)); 
 
                CodeFieldReferenceExpression eventExpr = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), evententry.resovledEventName);
                CodeBinaryOperatorExpression cond = new CodeBinaryOperatorExpression(eventExpr, 
                                                                                     CodeBinaryOperatorType.IdentityInequality,
                                                                                     new CodePrimitiveExpression(null));
                CodeConditionStatement ifstat = new CodeConditionStatement();
                ifstat.Condition = cond; 

                CodeExpressionCollection paramstr = new CodeExpressionCollection(); 
                paramstr.Add(new CodeFieldReferenceExpression(null, "sender")); 
                paramstr.Add(new CodeFieldReferenceExpression(null, "e"));
 
                CodeExpression[] temp = new CodeExpression[paramstr.Count];
                ((IList)paramstr).CopyTo(temp, 0);

                CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), evententry.resovledEventName, temp); 

                if (evententry.retType == typeof(void)) { 
                    ifstat.TrueStatements.Add(new CodeExpressionStatement(methodInvoke)); 
                }
                else { 
                    ifstat.TrueStatements.Add(new CodeMethodReturnStatement(methodInvoke));
                    ifstat.FalseStatements.Add(new CodeMethodReturnStatement(GetInitializer(evententry.retType)));
                }
 
                cmm.Statements.Add(ifstat);
 
                cls.Members.Add(cmm); 
            }
 
            WriteEventMulticaster(ns);
        }

        private void WriteHookupMethods(CodeTypeDeclaration cls) { 
            if (axctlEventsType != null) {
                // Generate the CreateSink() override. 
                // 
                CodeMemberMethod sinkMeth = new CodeMemberMethod();
                sinkMeth.Name = "CreateSink"; 
                sinkMeth.Attributes = MemberAttributes.Family | MemberAttributes.Override;

                CodeObjectCreateExpression newMultiCaster = new CodeObjectCreateExpression(axctl + "EventMulticaster");
                newMultiCaster.Parameters.Add(new CodeThisReferenceExpression()); 

                CodeAssignStatement assignMultiCaster = new CodeAssignStatement(multicasterRef, newMultiCaster); 
 
                CodeObjectCreateExpression coce = new CodeObjectCreateExpression(typeof(AxHost.ConnectionPointCookie).FullName);
                coce.Parameters.Add(memIfaceRef); 
                coce.Parameters.Add(multicasterRef);
                coce.Parameters.Add(new CodeTypeOfExpression(axctlEvents));

                CodeAssignStatement cas = new CodeAssignStatement(cookieRef, coce); 

                CodeTryCatchFinallyStatement ctcf = new CodeTryCatchFinallyStatement(); 
                ctcf.TryStatements.Add(assignMultiCaster); 
                ctcf.TryStatements.Add(cas);
                ctcf.CatchClauses.Add(new CodeCatchClause("", new CodeTypeReference(typeof(Exception)))); 

                // Add the CreateSink() method to the class.
                //
                sinkMeth.Statements.Add(ctcf); 
                cls.Members.Add(sinkMeth);
 
                // Generate the DetachSink() override. 
                //
                CodeMemberMethod detachMeth = new CodeMemberMethod(); 
                detachMeth.Name = "DetachSink";
                detachMeth.Attributes = MemberAttributes.Family | MemberAttributes.Override;

                CodeFieldReferenceExpression invokee = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), cookie); 
                CodeMethodInvokeExpression cmis = new CodeMethodInvokeExpression(invokee, "Disconnect");
 
                ctcf = new CodeTryCatchFinallyStatement(); 
                ctcf.TryStatements.Add(cmis);
                ctcf.CatchClauses.Add(new CodeCatchClause("", new CodeTypeReference(typeof(Exception)))); 
                detachMeth.Statements.Add(ctcf);
                cls.Members.Add(detachMeth);
            }
 
            // Generate the AttachInterfaces() override.
            CodeMemberMethod attachMeth = new CodeMemberMethod(); 
            attachMeth.Name = "AttachInterfaces"; 
            attachMeth.Attributes = MemberAttributes.Family | MemberAttributes.Override;
 
            CodeCastExpression cce = new CodeCastExpression(axctlType.FullName, new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "GetOcx"));
            CodeAssignStatement assign = new CodeAssignStatement(memIfaceRef, cce);

            CodeTryCatchFinallyStatement trycatch = new CodeTryCatchFinallyStatement(); 
            trycatch.TryStatements.Add(assign);
            trycatch.CatchClauses.Add(new CodeCatchClause("", new CodeTypeReference(typeof(Exception)))); 
 
            attachMeth.Statements.Add(trycatch);
            cls.Members.Add(attachMeth); 
        }

        private void WriteMembersDecl(CodeTypeDeclaration cls) {
            memIface = "ocx"; 
            memIfaceRef = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), memIface);
 
            cls.Members.Add(new CodeMemberField(MapTypeName(axctlType), memIface)); 

            if (axctlEventsType != null) { 
                multicaster = "eventMulticaster";
                multicasterRef = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), multicaster);
                cls.Members.Add(new CodeMemberField(axctl + "EventMulticaster", multicaster));
 
                cookie = "cookie";
                cookieRef = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), cookie); 
                cls.Members.Add(new CodeMemberField(typeof(AxHost.ConnectionPointCookie).FullName, cookie)); 
            }
        } 

        //VSQFE #830 change: This method now provides an option of stripping optional parameters.
        private void WriteMethod(CodeTypeDeclaration cls, MethodInfo method, bool hasPropInfo, bool removeOptionals) {
            Debug.WriteLineIf(AxCodeGen.Enabled, "Processing method: " + method.Name); 

 
            AxMethodGenerator generator = AxMethodGenerator.Create(method, removeOptionals); 
            generator.ControlType = axctlType;
 
            String methodName = method.Name;

            bool fOverride = false;
            bool fUseNew = false; 

            string methodPrefix = ResolveConflict(method.Name, method.ReturnType, out fOverride, out fUseNew); 
            if (fOverride) { 
                methodName = "Ctl" + methodName;
            } 

            // create the method body.
            //
            CodeMemberMethod cmm = generator.CreateMethod(methodName); 

 
            // Add the check for null this.ocx. 
            //
            cmm.Statements.Add(CreateInvalidStateException(cmm.Name, "MethodInvoke")); 


            // Marshal parameters in, convert if necessary, push onto parameter list
            // 
            List<CodeExpression> parameters = generator.GenerateAndMarshalParameters(cmm);
 
            // do method call 
            //
            CodeExpression returnExpression = generator.DoMethodInvoke(cmm, method.Name, memIfaceRef, parameters); 

            // marshal back parameters, do conversion.
            //
            generator.UnmarshalParameters(cmm, parameters); 

            // convert return value. 
            // 
            generator.GenerateReturn(cmm, returnExpression);
 

            cls.Members.Add(cmm);

            // Get the DISPID Attribute of the method to see if it handles the About Box. 
            // If it does, we will have to add a delegate to this in the WriteHookupMethods() code.
            // 
            object[] attrs = method.GetCustomAttributes(typeof(DispIdAttribute), false); 
            if (attrs != null && attrs.Length > 0) {
                DispIdAttribute dispid = (DispIdAttribute)attrs[0]; 
                if (dispid.Value == NativeMethods.ActiveX.DISPID_ABOUTBOX && method.GetParameters().Length == 0) {
                    aboutBoxMethod = cmm.Name;
                }
            } 
        }
 
 
        private void WriteMethods(CodeTypeDeclaration cls) {
            MethodInfo[] methods = axctlType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly); 
            for (int imeth = 0; imeth < methods.Length; ++imeth) {
                bool hasPropInfo;
                bool useLet;
                bool f = IsPropertySignature(methods[imeth], out hasPropInfo, out useLet); 

                Debug.WriteLineIf(AxCodeGen.Enabled, "Processing method: " + methods[imeth].Name + " IsProperty: " + f); 
 
                //VSQFE #830 change: Check if there are optional parameters. If so, generate an overload
                //                   with all the optionals removed. 
                if (!f) {
                    if (OptionalsPresent(methods[imeth])) {
                        WriteMethod(cls, methods[imeth], hasPropInfo, true);
                    } 

                    WriteMethod(cls, methods[imeth], hasPropInfo, false); 
                } 
            }
        } 

        private void WriteProperty(CodeTypeDeclaration cls, PropertyInfo pinfo, bool useLet) {
            CodeAttributeDeclarationCollection customAttrs;
            CodeAttributeDeclaration dispidAttr = null; 
            DispIdAttribute dispid = null;
 
            Debug.WriteLineIf(AxCodeGen.Enabled, "Processing property " + pinfo.Name); 

            if (nopersist == null) { 
                nopersist = new CodeAttributeDeclaration("System.ComponentModel.DesignerSerializationVisibility", new CodeAttributeArgument[] {
                                                                            new CodeAttributeArgument(new CodeFieldReferenceExpression(
                                                                            new CodeFieldReferenceExpression(null, "System.ComponentModel.DesignerSerializationVisibility"), "Hidden"))
                                                                            }); 
                nobrowse = new CodeAttributeDeclaration("System.ComponentModel.Browsable", new CodeAttributeArgument[] {new CodeAttributeArgument(new CodePrimitiveExpression(false))});
                browse   = new CodeAttributeDeclaration("System.ComponentModel.Browsable", new CodeAttributeArgument[] {new CodeAttributeArgument(new CodePrimitiveExpression(true))}); 
                bindable = new CodeAttributeDeclaration("System.ComponentModel.Bindable", new CodeAttributeArgument[] { 
                                                                            new CodeAttributeArgument(new CodeFieldReferenceExpression(
                                                                                new CodeFieldReferenceExpression(null, "System.ComponentModel.BindableSupport"), "Yes")) 
                                                                            });
                defaultBind = new CodeAttributeDeclaration("System.ComponentModel.Bindable", new CodeAttributeArgument[] {
                                                                            new CodeAttributeArgument(new CodeFieldReferenceExpression(
                                                                                new CodeFieldReferenceExpression(null, "System.ComponentModel.BindableSupport"), "Default")) 
                                                                            });
            } 
 
            object[] comaliasAttrs = pinfo.GetCustomAttributes(typeof(ComAliasNameAttribute), false);
            ComAliasEnum alias = ComAliasConverter.GetComAliasEnum(pinfo, pinfo.PropertyType, pinfo); 

            Type propType = pinfo.PropertyType;
            if (alias != ComAliasEnum.None) {
                propType = ComAliasConverter.GetWFTypeFromComType(propType, alias); 
            }
 
            // Is this a DataSource property? If so, add a member variable to 
            // cache the value of the property.
            // 
            bool dataSourceProp = (propType.GUID.Equals(Guid_DataSource));
            if (dataSourceProp) {
                CodeMemberField field = new CodeMemberField(propType.FullName, CreateDataSourceFieldName(pinfo.Name));
                field.Attributes = MemberAttributes.Private | MemberAttributes.Final; 
                cls.Members.Add(field);
                dataSourceProps.Add(pinfo); 
            } 

            // Get the DISPID Attribute of the property and store it in the newly generated wrapper property. 
            // We use this later to determine the property category to be used in the properties window.
            //
            object[] attrs = pinfo.GetCustomAttributes(typeof(DispIdAttribute), false);
            if (attrs != null && attrs.Length > 0) { 
                dispid = (DispIdAttribute)attrs[0];
                dispidAttr = new CodeAttributeDeclaration(typeof(DispIdAttribute).FullName, new CodeAttributeArgument[] {new CodeAttributeArgument(new CodePrimitiveExpression(dispid.Value))}); 
            } 
#if DEBUG
            else { 
                Debug.Fail("Property: " + pinfo.Name + " does not have any DISPID attribute, or has multiple DISPID attributes!!!" + ((attrs != null) ? attrs.Length : -1));
            }
#endif // DEBUG
 
            bool fOverride = false;
            bool fUseNew = false; 
 
            string propPrefix = ResolveConflict(pinfo.Name, propType, out fOverride, out fUseNew);
 
            if (fOverride || fUseNew) {
                if (dispid == null)
                    return;
                else { 
                    if (!IsDispidKnown(dispid.Value, pinfo.Name)) {
                        propPrefix = "Ctl"; 
                        fOverride = false; 
                        fUseNew = false;
                    } 
                }
            }

            CodeMemberProperty prop = new CodeMemberProperty(); 

            prop.Type = new CodeTypeReference(MapTypeName(propType)); 
            prop.Name = propPrefix + pinfo.Name; 
            prop.Attributes = MemberAttributes.Public;
 
            if (fOverride) {
                prop.Attributes |= MemberAttributes.Override;
            }
            else if (fUseNew) { 
                prop.Attributes |= MemberAttributes.New;
            } 
 
            bool isDefaultBind = false;
            bool browsable = IsPropertyBrowsable(pinfo, alias); 
            bool isbind    = IsPropertyBindable(pinfo, out isDefaultBind);

            // Generate custom attributes for these properties.
            // 
            if (!browsable || alias == ComAliasEnum.Handle) {
                // These properties are NonBrowsable, NonPersistable ones. 
                // 
                customAttrs = new CodeAttributeDeclarationCollection(new CodeAttributeDeclaration[] {nobrowse, nopersist, dispidAttr});
            } 
            else if (dataSourceProp) {
                // DataSource properties are persitable in code
                //
                customAttrs = new CodeAttributeDeclarationCollection(new CodeAttributeDeclaration[] {dispidAttr}); 
            }
            else { 
                // The rest of the properties are to be Persistable.None, as they get persisted to the ActiveX control's persist stream. 
                //
                if (fOverride || fUseNew) { 
                    customAttrs = new CodeAttributeDeclarationCollection(new CodeAttributeDeclaration[] {browse, nopersist, dispidAttr});
                }
                else {
                    customAttrs = new CodeAttributeDeclarationCollection(new CodeAttributeDeclaration[] {nopersist, dispidAttr}); 
                }
            } 
 
            if (alias != ComAliasEnum.None) {
                CodeAttributeDeclaration attr = new CodeAttributeDeclaration(typeof(ComAliasNameAttribute).FullName, new CodeAttributeArgument[] { 
                                                                                new CodeAttributeArgument(new CodePrimitiveExpression(pinfo.PropertyType.FullName))});
                customAttrs.Add(attr);
            }
 
            if (isDefaultBind)
                customAttrs.Add(defaultBind); 
            else if (isbind) 
                customAttrs.Add(bindable);
 
            prop.CustomAttributes = customAttrs;

            // Handle Indexed properties...
            AxParameterData[] parameters = AxParameterData.Convert(pinfo.GetIndexParameters()); 
            if (parameters != null && parameters.Length > 0) {
                for (int iparam = 0; iparam < parameters.Length; ++iparam) { 
                    CodeParameterDeclarationExpression param = CreateParamDecl(parameters[iparam].TypeName, parameters[iparam].Name, false); 
                    param.Direction = parameters[iparam].Direction;
                    prop.Parameters.Add(param); 
                }
            }

            // Visual Basic generates properties where the setter takes a BYREF parameter for the set_XXX(). 
            // This causes the C# code gen to not work correctly, since the compiler cannot convert
            // the parameter fro, type 'Foo' to type 'ref Foo'. The workaround is to recognize these 
            // properties and generate the property invokes on the OCX to be of the get_XXX() and 
            // set_XXX(ref value) instead of the regular property invoke syntax.
            // 
            bool fConvertPropCallsToMethodInvokes = useLet;
            if (pinfo.CanWrite) {
                MethodInfo setter;
                if (useLet) 
                    setter = pinfo.DeclaringType.GetMethod("let_" + pinfo.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                else 
                    setter = pinfo.GetSetMethod(); 

                Debug.Assert(setter != null, "No set/let method found for : " + pinfo.Name); 

                Type paramType = setter.GetParameters()[0].ParameterType;
                Type baseType = paramType.GetElementType();
                if (baseType != null && paramType != baseType) { 
                    Debug.WriteLineIf(AxCodeGen.Enabled, "Writing property in method invoke syntax " + pinfo.Name);
                    fConvertPropCallsToMethodInvokes = true; 
                } 
            }
 
            if (pinfo.CanRead)
                WritePropertyGetter(prop, pinfo, alias, parameters, fConvertPropCallsToMethodInvokes, fOverride, dataSourceProp);
            if (pinfo.CanWrite)
                WritePropertySetter(prop, pinfo, alias, parameters, fConvertPropCallsToMethodInvokes, fOverride, useLet, dataSourceProp); 

            // If the default property happens to be different from "Item", we have to 
            // generate the name("foo") attribute on the default property so we can 
            // rename it.
            // 
            if (parameters.Length > 0 && prop.Name != "Item") {
                CodeAttributeDeclaration nameAttr = new CodeAttributeDeclaration("System.Runtime.CompilerServices.IndexerName",
                                                             new CodeAttributeArgument[] {new CodeAttributeArgument(new CodePrimitiveExpression(prop.Name))});
 
                // Calling a property "Item" tells the codedom that this is the default indexed property.
                // 
                prop.Name = "Item"; 
                prop.CustomAttributes.Add(nameAttr);
            } 

            // Add DefaultProperty attribute for the class if needed...
            if (defMember != null && defMember.Equals(pinfo.Name)) {
                CodeAttributeDeclaration defMemberAttr = new CodeAttributeDeclaration("System.ComponentModel.DefaultProperty", 
                                                             new CodeAttributeArgument[] {new CodeAttributeArgument(new CodePrimitiveExpression(prop.Name))});
                cls.CustomAttributes.Add(defMemberAttr); 
            } 

            cls.Members.Add(prop); 
        }

        private void WritePropertyGetter(CodeMemberProperty prop, PropertyInfo pinfo, ComAliasEnum alias, AxParameterData[] parameters, bool fMethodSyntax, bool fOverride, bool dataSourceProp) {
            if (dataSourceProp) { 
                Debug.Assert(!fOverride, "Cannot have a overridden DataSource property.");
                Debug.Assert(parameters.Length <= 0, "Cannot have a parameterized DataSource property."); 
 
                string dataSourceName = CreateDataSourceFieldName(pinfo.Name);
                CodeMethodReturnStatement ret = new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), dataSourceName)); 
                prop.GetStatements.Add(ret);
            }
            else if (fOverride) {
                CodeConditionStatement ifstat = CreateValidStateCheck(); 
                ifstat.TrueStatements.Add(GetPropertyGetRValue(pinfo, memIfaceRef, alias, parameters, fMethodSyntax));
 
                ifstat.FalseStatements.Add(GetPropertyGetRValue(pinfo, new CodeBaseReferenceExpression(), ComAliasEnum.None, parameters, false)); 

                prop.GetStatements.Add(ifstat); 
            }
            else {
                prop.GetStatements.Add(CreateInvalidStateException(prop.Name, "PropertyGet"));
                prop.GetStatements.Add(GetPropertyGetRValue(pinfo, memIfaceRef, alias, parameters, fMethodSyntax)); 
            }
        } 
 
        private void WritePropertySetter(CodeMemberProperty prop, PropertyInfo pinfo, ComAliasEnum alias, AxParameterData[] parameters, bool fMethodSyntax, bool fOverride, bool useLet, bool dataSourceProp) {
            if (!fOverride && !dataSourceProp) { 
                prop.SetStatements.Add(CreateInvalidStateException(prop.Name, "PropertySet"));
            }

            if (dataSourceProp) { 
                Debug.Assert(!fOverride, "Cannot have a overridden DataSource property.");
                Debug.Assert(parameters.Length <= 0, "Cannot have a parameterized DataSource property."); 
 
                string dataSourceName = CreateDataSourceFieldName(pinfo.Name);
                WriteDataSourcePropertySetter(prop, pinfo, dataSourceName); 
            }
            else if (!fMethodSyntax) {
                WritePropertySetterProp(prop, pinfo, alias, parameters, fOverride, useLet);
            } 
            else {
                WritePropertySetterMethod(prop, pinfo, alias, parameters, fOverride, useLet); 
            } 
        }
 
        private void WriteDataSourcePropertySetter(CodeMemberProperty prop, PropertyInfo pinfo, string dataSourceName) {
            CodeExpression left = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), dataSourceName);
            CodeExpression right = new CodeFieldReferenceExpression(null, "value");
            CodeAssignStatement assign = new CodeAssignStatement(left, right); 

            prop.SetStatements.Add(assign); 
 
            CodeConditionStatement ifstat = CreateValidStateCheck();
            left = new CodeFieldReferenceExpression(memIfaceRef, pinfo.Name); 
            ifstat.TrueStatements.Add(new CodeAssignStatement(left, right));

            prop.SetStatements.Add(ifstat);
        } 

        private void WritePropertySetterMethod(CodeMemberProperty prop, PropertyInfo pinfo, ComAliasEnum alias, AxParameterData[] parameters, bool fOverride, bool useLet) { 
            CodeExpression baseCall = null; 
            CodeBinaryOperatorExpression cond = null;
            CodeConditionStatement ifstat = null; 

            if (fOverride) {
                if (parameters.Length > 0) {
                    baseCall = new CodeIndexerExpression(memIfaceRef); 
                }
                else { 
                    baseCall = new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), pinfo.Name); 
                }
                cond = new CodeBinaryOperatorExpression(memIfaceRef, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null)); 
                ifstat = new CodeConditionStatement();
                ifstat.Condition = cond;
            }
 
            CodeFieldReferenceExpression propCallParam;
            string setterName = (useLet) ? "let_" + pinfo.Name : pinfo.GetSetMethod().Name; 
            CodeMethodInvokeExpression propCall = new CodeMethodInvokeExpression(memIfaceRef, setterName); 

            for (int iparam = 0; iparam < parameters.Length; ++iparam) { 
                if (fOverride) {
                    ((CodeIndexerExpression)baseCall).Indices.Add(new CodeFieldReferenceExpression(null, parameters[iparam].Name));
                }
                propCall.Parameters.Add(new CodeFieldReferenceExpression(null, parameters[iparam].Name)); 
            }
 
            CodeFieldReferenceExpression valueExpr = new CodeFieldReferenceExpression(null, "value"); 
            CodeExpression rval = GetPropertySetRValue(alias, pinfo.PropertyType);
 
            if (alias != ComAliasEnum.None) {
                string paramConverter = ComAliasConverter.GetWFToComParamConverter(alias, pinfo.PropertyType);

                CodeParameterDeclarationExpression propCallParamDecl; 
                if (paramConverter.Length == 0) {
                    propCallParamDecl = CreateParamDecl(MapTypeName(pinfo.PropertyType), "paramTemp", false); 
                } 
                else {
                    propCallParamDecl = CreateParamDecl(paramConverter, "paramTemp", false); 
                }
                prop.SetStatements.Add(new CodeAssignStatement(propCallParamDecl, rval));

                propCallParam = new CodeFieldReferenceExpression(null, "paramTemp"); 
            }
            else { 
                propCallParam = valueExpr; 
            }
 
            propCall.Parameters.Add(new CodeDirectionExpression((useLet) ? FieldDirection.In : FieldDirection.Ref, propCallParam));

            if (fOverride) {
                prop.SetStatements.Add(new CodeAssignStatement(baseCall, valueExpr)); 
                ifstat.TrueStatements.Add(new CodeExpressionStatement(propCall));
                prop.SetStatements.Add(ifstat); 
            } 
            else {
                prop.SetStatements.Add(new CodeExpressionStatement(propCall)); 
            }
        }

        private void WritePropertySetterProp(CodeMemberProperty prop, PropertyInfo pinfo, ComAliasEnum alias, AxParameterData[] parameters, bool fOverride, bool useLet) { 
            CodeExpression baseCall = null;
            CodeBinaryOperatorExpression cond = null; 
            CodeConditionStatement ifstat = null; 

            if (fOverride) { 
                if (parameters.Length > 0) {
                    baseCall = new CodeIndexerExpression(memIfaceRef);
                }
                else { 
                    baseCall = new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), pinfo.Name);
                } 
 
                cond = new CodeBinaryOperatorExpression(memIfaceRef, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null));
                ifstat = new CodeConditionStatement(); 
                ifstat.Condition = cond;
            }

            CodeExpression propCall; 

            if (parameters.Length > 0) { 
                propCall = new CodeIndexerExpression(memIfaceRef); 
            }
            else { 
                propCall = new CodePropertyReferenceExpression(memIfaceRef, pinfo.Name);
            }

            for (int iparam = 0; iparam < parameters.Length; ++iparam) { 
                if (fOverride) {
                    ((CodeIndexerExpression)baseCall).Indices.Add(new CodeFieldReferenceExpression(null, parameters[iparam].Name)); 
                } 
                ((CodeIndexerExpression)propCall).Indices.Add(new CodeFieldReferenceExpression(null, parameters[iparam].Name));
            } 

            CodeFieldReferenceExpression valueExpr = new CodeFieldReferenceExpression(null, "value");
            CodeExpression rval = GetPropertySetRValue(alias, pinfo.PropertyType);
 
            if (fOverride) {
                prop.SetStatements.Add(new CodeAssignStatement(baseCall, valueExpr)); 
                ifstat.TrueStatements.Add(new CodeAssignStatement(propCall, rval)); 
                prop.SetStatements.Add(ifstat);
            } 
            else {
                prop.SetStatements.Add(new CodeAssignStatement(propCall, rval));
            }
        } 

        private CodeMethodReturnStatement GetPropertyGetRValue(PropertyInfo pinfo, CodeExpression reference, ComAliasEnum alias, AxParameterData[] parameters, bool fMethodSyntax) { 
            CodeExpression propCall = null; 

            if (fMethodSyntax) { 
                propCall = new CodeMethodInvokeExpression(reference, pinfo.GetGetMethod().Name);
                for (int iparam = 0; iparam < parameters.Length; ++iparam)
                    ((CodeMethodInvokeExpression)propCall).Parameters.Add(new CodeFieldReferenceExpression(null, parameters[iparam].Name));
            } 
            else {
                if (parameters.Length > 0) { 
                    propCall = new CodeIndexerExpression(reference); 

                    for (int iparam = 0; iparam < parameters.Length; ++iparam) 
                        ((CodeIndexerExpression)propCall).Indices.Add(new CodeFieldReferenceExpression(null, parameters[iparam].Name));
                }
                else {
                    propCall = new CodePropertyReferenceExpression(reference, ((parameters.Length == 0) ? pinfo.Name : "")); 
                }
            } 
 
            if (alias != ComAliasEnum.None) {
                string converter = ComAliasConverter.GetComToManagedConverter(alias); 
                string paramConverter = ComAliasConverter.GetComToWFParamConverter(alias);

                CodeExpression[] expr = null;
                if (paramConverter.Length == 0) { 
                    expr = new CodeExpression[] {propCall};
                } 
                else { 
                    CodeCastExpression paramCast = new CodeCastExpression(paramConverter, propCall);
                    expr = new CodeExpression[] {paramCast}; 
                }
                CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(null, converter, expr);
                return new CodeMethodReturnStatement(methodInvoke);
            } 
            else {
                return new CodeMethodReturnStatement(propCall); 
            } 
        }
 
        private CodeExpression GetPropertySetRValue(ComAliasEnum alias, Type propertyType) {
            CodeExpression valueExpr = new CodePropertySetValueReferenceExpression();

            if (alias != ComAliasEnum.None) { 
                string converter = ComAliasConverter.GetWFToComConverter(alias);
                string paramConverter = ComAliasConverter.GetWFToComParamConverter(alias, propertyType); 
 
                CodeExpression[] expr = new CodeExpression[] {valueExpr};
                CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(null, converter, expr); 

                if (paramConverter.Length == 0) {
                    return methodInvoke;
                } 
                else {
                    return new CodeCastExpression(paramConverter, methodInvoke); 
                } 
            }
            else { 
                return valueExpr;
            }
        }
 
        private void WriteProperties(CodeTypeDeclaration cls) {
            bool useLet; 
 
            PropertyInfo[] props = axctlType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
 
            for (int iprop = 0; iprop < props.Length; ++iprop) {
                if (IsPropertySignature(props[iprop], out useLet)) {
                    WriteProperty(cls, props[iprop], useLet);
                } 
            }
        } 
 
        private enum ComAliasEnum {
            None, 
            Color,
            Font,
            FontDisp,
            Handle, 
            Picture,
            PictureDisp 
        } 

        private static class ComAliasConverter { 
            private static Guid Guid_IPicture     = new Guid("{7BF80980-BF32-101A-8BBB-00AA00300CAB}");
            private static Guid Guid_IPictureDisp = new Guid("{7BF80981-BF32-101A-8BBB-00AA00300CAB}");
            private static Guid Guid_IFont        = new Guid("{BEF6E002-A874-101A-8BBA-00AA00300CAB}");
            private static Guid Guid_IFontDisp    = new Guid("{BEF6E003-A874-101A-8BBA-00AA00300CAB}"); 

            // Optimization caches. 
            // 
            private static Hashtable typeGuids;
 
            public static string GetComToManagedConverter(ComAliasEnum alias) {
                Debug.Assert(alias != ComAliasEnum.None, "Cannot find converter for ComAliasEnum.None");
                if (alias == ComAliasEnum.Color)
                    return "GetColorFromOleColor"; 

                if (IsFont(alias)) 
                    return "GetFontFromIFont"; 

                if (IsPicture(alias)) 
                    return "GetPictureFromIPicture";

                return "";
            } 

            public static string GetComToWFParamConverter(ComAliasEnum alias) { 
                Debug.Assert(alias != ComAliasEnum.None, "Cannot find converter for ComAliasEnum.None"); 
                if (alias == ComAliasEnum.Color)
                    return typeof(uint).FullName; 

                return "";
            }
 
            private static Guid GetGuid(Type t) {
                Guid g = Guid.Empty; 
 
                if (typeGuids == null) {
                    typeGuids = new Hashtable(); 
                }
                else if (typeGuids.Contains(t)) {
                    g = (Guid)typeGuids[t];
                    return g; 
                }
 
                g = t.GUID; 
                typeGuids.Add(t, g);
                return g; 
            }
            public static Type GetWFTypeFromComType(Type t, ComAliasEnum alias) {
                Debug.Assert(alias != ComAliasEnum.None, "Cannot find converter for ComAliasEnum.None");
 
                if (!IsValidType(alias, t))
                    return t; 
 
                if (alias == ComAliasEnum.Color)
                    return typeof(System.Drawing.Color); 

                if (IsFont(alias))
                    return typeof(System.Drawing.Font);
 
                if (IsPicture(alias))
                    return typeof(System.Drawing.Image); 
 
                return t;
            } 

            public static string GetWFToComConverter(ComAliasEnum alias) {
                Debug.Assert(alias != ComAliasEnum.None, "Cannot find converter for ComAliasEnum.None");
                if (alias == ComAliasEnum.Color) 
                    return "GetOleColorFromColor";
 
                if (IsFont(alias)) 
                    return "GetIFontFromFont";
 
                if (IsPicture(alias))
                    return "GetIPictureFromPicture";

                return ""; 
            }
 
            public static string GetWFToComParamConverter(ComAliasEnum alias, Type t) { 
                Debug.Assert(alias != ComAliasEnum.None, "Cannot find converter for ComAliasEnum.None");
                return t.FullName; 
            }

            public static ComAliasEnum GetComAliasEnum(MemberInfo memberInfo, Type type, ICustomAttributeProvider attrProvider) {
                string aliasName = null; 
                int dispid = -1;
 
                Debug.Assert(type != null, "No type for ComAliasEnum!!!"); 

                object[] attrs = new object[0]; 

                if (attrProvider != null) {
                    attrs = attrProvider.GetCustomAttributes(typeof(ComAliasNameAttribute), false);
                } 

                if (attrs != null && attrs.Length > 0) { 
                    Debug.Assert(attrs.Length == 1, "Multiple ComAliasNameAttributes found o: " + memberInfo.Name); 
                    ComAliasNameAttribute alias = (ComAliasNameAttribute)attrs[0];
                    aliasName = alias.Value; 
                }

                if (aliasName != null && aliasName.Length != 0) {
                    if (aliasName.EndsWith(".OLE_COLOR") && IsValidType(ComAliasEnum.Color, type)) 
                        return ComAliasEnum.Color;
 
                    if (aliasName.EndsWith(".OLE_HANDLE") && IsValidType(ComAliasEnum.Handle, type)) 
                        return ComAliasEnum.Handle;
 
#if DEBUG
                    //if (!aliasName.Equals("stdole.OLE_HANDLE"))
                    //    Debug.Fail("Did not handle ComAliasNameAttribute of: " + aliasName + " on property: " + memberInfo.Name);
#endif //DEBUG 
                }
 
                if (memberInfo is PropertyInfo && String.Equals(memberInfo.Name, "hWnd",  StringComparison.OrdinalIgnoreCase) && IsValidType(ComAliasEnum.Handle, type)) { 
                    Debug.WriteLineIf(AxCodeGen.Enabled && (aliasName == null || aliasName.EndsWith(".OLE_HANDLE")), "hWnd property is not marked as OLE_HANDLE");
                    return ComAliasEnum.Handle; 
                }

                // Get the dispid so we can use standard dispid values to compare.
                // 
                if (attrProvider != null) {
                    attrs = attrProvider.GetCustomAttributes(typeof(DispIdAttribute), false); 
                    if (attrs != null && attrs.Length > 0) { 
                        Debug.Assert(attrs.Length == 1, "Multiple ComAliasNameAttributes found o: " + memberInfo.Name);
                        DispIdAttribute alias = (DispIdAttribute)attrs[0]; 
                        dispid = alias.Value;
                    }
                }
 
                if ((dispid == NativeMethods.ActiveX.DISPID_BACKCOLOR || dispid == NativeMethods.ActiveX.DISPID_FORECOLOR ||
                    dispid == NativeMethods.ActiveX.DISPID_FILLCOLOR ||  dispid == NativeMethods.ActiveX.DISPID_BORDERCOLOR) && 
                    IsValidType(ComAliasEnum.Color, type)) { 
                        return ComAliasEnum.Color;
                } 

                if (dispid == NativeMethods.ActiveX.DISPID_FONT && IsValidType(ComAliasEnum.Font, type)) {
                        return ComAliasEnum.Font;
                } 

                if (dispid == NativeMethods.ActiveX.DISPID_PICTURE && IsValidType(ComAliasEnum.Picture, type)) { 
                        return ComAliasEnum.Picture; 
                }
 
                if (dispid == NativeMethods.ActiveX.DISPID_HWND && IsValidType(ComAliasEnum.Handle, type)) {
                        return ComAliasEnum.Handle;
                }
 
                if (IsValidType(ComAliasEnum.Font, type))
                    return ComAliasEnum.Font; 
 
                if (IsValidType(ComAliasEnum.FontDisp, type))
                    return ComAliasEnum.FontDisp; 

                if (IsValidType(ComAliasEnum.Picture, type))
                    return ComAliasEnum.Picture;
 
                if (IsValidType(ComAliasEnum.PictureDisp, type))
                    return ComAliasEnum.PictureDisp; 
 
                return ComAliasEnum.None;
            } 

            public static bool IsFont(ComAliasEnum e) {
                return e == ComAliasEnum.Font || e == ComAliasEnum.FontDisp;
            } 

            public static bool IsPicture(ComAliasEnum e) { 
                return e == ComAliasEnum.Picture || e == ComAliasEnum.PictureDisp; 
            }
 
            private static bool IsValidType(ComAliasEnum e, Type t) {
                switch (e) {
                    case ComAliasEnum.Color:
                        return t == typeof(UInt16) || t == typeof(uint) || t == typeof(int) || t == typeof(short); 

                    case ComAliasEnum.Handle: 
                        return t == typeof(uint) || t == typeof(int) || t == typeof(IntPtr) || t == typeof(UIntPtr); 

                    case ComAliasEnum.Font: 
                        return GetGuid(t).Equals(Guid_IFont);

                    case ComAliasEnum.FontDisp:
                        return GetGuid(t).Equals(Guid_IFontDisp); 

                    case ComAliasEnum.Picture: 
                        return GetGuid(t).Equals(Guid_IPicture); 

                    case ComAliasEnum.PictureDisp: 
                        return GetGuid(t).Equals(Guid_IPictureDisp);

                    default:
                        Debug.Fail("Invalid verify call for " + e.ToString()); 
                        return false;
                } 
            } 
        }
 
        private class EventEntry {
            public string eventName;
            public string resovledEventName;
            public string eventCls; 
            public string eventHandlerCls;
            public Type   retType; 
            public AxParameterData[] parameters; 
            public string eventParam;
            public string invokeMethodName; 
            public MemberAttributes eventFlags;

            public EventEntry(string eventName, string eventCls, string eventHandlerCls, Type retType, AxParameterData[] parameters, bool conflict) {
                this.eventName = eventName; 
                this.eventCls = eventCls;
                this.eventHandlerCls = eventHandlerCls; 
                this.retType = retType; 
                this.parameters = parameters;
                this.eventParam = eventName.ToLower(CultureInfo.InvariantCulture) + "Event"; 
                this.resovledEventName = (conflict) ? eventName + "Event" : eventName;
                this.invokeMethodName = "RaiseOn" + resovledEventName;
                this.eventFlags = MemberAttributes.Public | MemberAttributes.Final;
            } 
        }
 
 
        /// A helper class we use to generate method bodies.  We need this
        /// because we either generate a "normal" call or a call through reflection using MethodINfo.Invoke. 
        /// By factoring out the calls like this, we can override this call and change the output for the
        /// Inovke case.  See the AxReflectionInvokeMethodGenerator class.
        ///
        /// Here's a complex example: 
        ///
        /// public virtual System.Drawing.Image MethodOpt4(bool b, short i, ref System.Drawing.Font f, ref System.Drawing.Image p, ref short x) { 
        ///    if ((this.ocx == null)) { 
        ///        throw new System.Windows.Forms.AxHost.InvalidActiveXStateException("MethodOpt4", System.Windows.Forms.AxHost.ActiveXInvokeKind.MethodInvoke);
        ///    } 
        ///    stdole.StdFont _f = ((stdole.StdFont)(GetIFontFromFont(f)));
        ///    stdole.StdPicture _p = ((stdole.StdPicture)(GetIPictureFromPicture(p)));
        ///    stdole.StdPicture returnValue = ((stdole.StdPicture)(this.ocx.MethodOpt4(b, i, ref _f, ref _p, ref x)));
        ///    f = GetFontFromIFont(((stdole.StdFont)(_f))); 
        ///    p = GetPictureFromIPicture(((stdole.StdPicture)(_p)));
        ///    return GetPictureFromIPicture(returnValue); 
        /// } 
        ///
        private class AxMethodGenerator { 

            private MethodInfo _method;
            private bool       _removeOptionals;
            private AxParameterData[] _params; 
            private Type        _controlType;
 
            protected static object OriginalParamNameKey = new object(); 
            protected static string ReturnValueVariableName = "returnValue";
 
            internal AxMethodGenerator(MethodInfo method, bool removeOpts) {
                _method = method;
                _removeOptionals = removeOpts;
            } 

            /// The type of control we're generating code for.  This is needed 
            /// if we're donig reflection invoke (see the derived class) 
            ///
            public Type ControlType { 

                get {return _controlType;}
                set {_controlType = value;}
            } 

            private AxParameterData[] Parameters { 
 
                get {
                    if (_params == null && _method != null) { 
                        _params = AxParameterData.Convert(_method.GetParameters());

                        if (_params == null) {
                            _params = new AxParameterData[0]; 
                        }
                    } 
                    return _params; 
                }
            } 

            /// Static factory method that gives us the right kind of object so we can use
            /// that fancy polymorphism.
            /// 
            public static AxMethodGenerator Create(MethodInfo method, bool removeOptionals) {
 
                bool useReflectionInvoke = removeOptionals && NonPrimitiveOptionalsOrMissingPresent(method); 
                if (useReflectionInvoke) {
                    return new AxReflectionInvokeMethodGenerator(method, removeOptionals); 
                }
                else {
                    return new AxMethodGenerator(method, removeOptionals);
                } 
            }
 
            /// Create the method body... 
            ///
            public CodeMemberMethod CreateMethod(string methodName) { 


                CodeMemberMethod cmm = new CodeMemberMethod();
 
                cmm.Name = methodName;
                cmm.Attributes = MemberAttributes.Public; 
                cmm.ReturnType = new CodeTypeReference(MapTypeName(_method.ReturnType)); 

                return cmm; 

            }

            /// Build the list of parameters and "marshal" them in.  This means for any that are types 
            /// that we need to convert to managed types, we'll do that work.
            /// 
            /// Here's a code example: 
            ///
            /// public virtual System.Drawing.Font MethodOpt3(ref System.Drawing.Font p, ref short x) { 
            ///    stdole.StdFont _p = ((stdole.StdFont)(GetIFontFromFont(p)));
            ///
            ///
            /// The output from this is the list of parameters to send to the calling function. 
            /// Notice this list contains both references to parameters (normal or like the ones above), but also
            /// the default values of any that are optional. 
            /// 
            public List<CodeExpression> GenerateAndMarshalParameters(CodeMemberMethod method){
 
                List<CodeExpression> paramExpressions = new List<CodeExpression>();

                foreach (AxParameterData param in Parameters) {
                    if (param.IsOptional && _removeOptionals) { 
                        // generate the default value here -- notice we just add this value to the list of
                        // parameters and pass it along. 
                        // 
                        CodeExpression defaultExpression = GetDefaultExpressionForInvoke(_method, param);
                        paramExpressions.Add(defaultExpression); 
                        continue;
                    }
                    // set up our variables.
                    // 
                    Type parameterType = param.ParameterBaseType;
                    ComAliasEnum alias = ComAliasConverter.GetComAliasEnum(_method, parameterType , null); 
                    CodeVariableReferenceExpression paramRef = new CodeVariableReferenceExpression(param.Name); 
                    paramRef.UserData[typeof(AxParameterData)] = param;
 
                    if (alias != ComAliasEnum.None) {


                            // create the parameter itself. 
                            //
                            Type destType = ComAliasConverter.GetWFTypeFromComType(parameterType, alias); 
                            CodeParameterDeclarationExpression paramDecl =  new CodeParameterDeclarationExpression(destType.FullName, param.Name); 
                            paramDecl.Direction = param.Direction;
                            method.Parameters.Add(paramDecl); 

                            // generate the conversion code
                            //
                            string converter = ComAliasConverter.GetWFToComConverter(alias); 
                            CodeMethodInvokeExpression conversionExpression = new CodeMethodInvokeExpression(null, converter);
 
                            // note we create a new one here because the paramRef name will get updated.  We want the original name. 
                            //
                            conversionExpression.Parameters.Add(new CodeVariableReferenceExpression(param.Name)); 

                            // update the name in the param ref -- we'll just prepend an underscore for the new parameter name.
                            //
                            paramRef.UserData[OriginalParamNameKey] = param.Name; 
                            paramRef.VariableName = "_" + param.Name;
 
                            // add the conversion statement. 
                            //
                            CodeVariableDeclarationStatement paramConversion = 
                                    new CodeVariableDeclarationStatement(parameterType.FullName,
                                                                         paramRef.VariableName,
                                                                         new CodeCastExpression(parameterType, conversionExpression));
 
                            method.Statements.Add(paramConversion);
                    } 
                    else { 
                            CodeParameterDeclarationExpression paramExpr = new CodeParameterDeclarationExpression(param.TypeName, param.Name);
                            paramExpr.Direction = param.Direction; 
                            method.Parameters.Add(paramExpr);
                    }

                    paramExpressions.Add(paramRef); 
                }
 
                return paramExpressions; 
            }
 
            public CodeExpression DoMethodInvoke(CodeMemberMethod method, string methodName, CodeExpression targetObject, List<CodeExpression> parameters) {
                return DoMethodInvokeCore(method, methodName, _method.ReturnType, targetObject, parameters);
            }
 
            /// Do the main invoke call.  In this case, we'll just invoke the method we're passed with the parameters
            /// that we're passed.  This method is virtual so reflection invoke things can do their own processing here. 
            /// Code: 
            ///
            /// this.ocx.MethodOpt2(ref _p, x); 
            ///
            /// or (if optionals are present and we have a default value)
            ///
            /// this.ocx.MethodOpt1(((short)(2))); 
            ///
            public virtual CodeExpression DoMethodInvokeCore(CodeMemberMethod method, string methodName, Type returnType, CodeExpression targetObject, List<CodeExpression> parameters) { 
                CodeMethodInvokeExpression mie = new CodeMethodInvokeExpression(targetObject, methodName); 

                // walk the parameters and just add them to the call 
                //
                foreach (CodeExpression paramExpr in parameters) {
                   AxParameterData paramData = (AxParameterData)paramExpr.UserData[typeof(AxParameterData)];
 
                   CodeExpression expr = paramExpr;
 
                   if (paramData != null) { 
                        expr = new CodeDirectionExpression(paramData.Direction, paramExpr);
                   } 
                   mie.Parameters.Add(expr);
                }

                // if there isn't a return type, we're done! 
                //
                if (returnType == typeof(void)) { 
                    method.Statements.Add(new CodeExpressionStatement(mie)); 
                    return null;
                } 

                // assign the method call to the return value and return the expression to get to it.
                //
                CodeVariableDeclarationStatement returnStatement = new CodeVariableDeclarationStatement(returnType, ReturnValueVariableName, new CodeCastExpression(returnType, mie)); 
                method.Statements.Add(returnStatement);
 
                return new CodeVariableReferenceExpression(ReturnValueVariableName); 
            }
 
            /// Take the parameter list and convert them back (if necessary) to the input parameter types.
            /// For example, if a parameter comes in as IPictureDisp, we convert it to a Bitmap.  If it's a ref
            /// parameter, we need to convert it back to an IPictureDisp and copy the value back out.
            /// The idea is that we use a copy-in-copy-out pattern for ref and out parameters. 
            ///
            /// Code: 
            /// 
            /// p = GetPictureFromIPicture(((stdole.StdPicture)(_p)));
            /// 
            ///
            public void UnmarshalParameters(CodeMemberMethod method, List<CodeExpression> parameters) {

                 foreach (CodeExpression paramExpr in parameters) { 

                        // if it's not a variable-based parameter, we're done. 
                        // 
                        if (!(paramExpr is CodeVariableReferenceExpression)) {
 
                            continue;
                        }

                        AxParameterData paramData = (AxParameterData)paramExpr.UserData[typeof(AxParameterData)]; 
                        Debug.Assert(paramData != null, "why don't we have parameter data here?");
 
                        // check the direction to see if this guy needs to be marshaled out. 
                        //
 
                        string originalParameterName = (string)paramExpr.UserData[OriginalParamNameKey];

                        if (paramData.Direction == FieldDirection.In || originalParameterName == null) {
                            continue; 
                        }
 
                        CodeExpression left = new CodeVariableReferenceExpression(originalParameterName); 
                        CodeExpression right = new CodeCastExpression(paramData.ParameterBaseType, paramExpr);
 
                        // check the parameter type to see if it needs to be converted, and then convert it.
                        //
                        ComAliasEnum alias = ComAliasConverter.GetComAliasEnum(_method, paramData.ParameterBaseType, null);
 
                        if (alias != ComAliasEnum.None) {
 
                            string converter = ComAliasConverter.GetComToManagedConverter(alias); 
                            CodeMethodInvokeExpression refConvertExpression = new CodeMethodInvokeExpression(null, converter);
                            refConvertExpression.Parameters.Add(right); 
                            right = refConvertExpression;
                        }

                        // generate the assignment back to the variable. 
                        //
                        CodeAssignStatement cas = new CodeAssignStatement(left, right); 
                        method.Statements.Add(cas); 
                 }
 
            }

            /// Generate the property return statement given the expression.
            /// This will ensure the value is properly converted. 
            ///
            /// code: 
            /// 
            ///    return GetPictureFromIPicture(returnValue);
            /// 
            /// or
            ///
            ///   return returnValue;
            /// 
            public void GenerateReturn(CodeMemberMethod method, CodeExpression returnExpression) {
 
                if (returnExpression == null) { 
                    return;
                } 

                // convert the return type if needed.
                //
                ComAliasEnum alias = ComAliasConverter.GetComAliasEnum(_method, _method.ReturnType, _method.ReturnTypeCustomAttributes); 

                if (alias != ComAliasEnum.None) { 
 
                    string converter = ComAliasConverter.GetComToManagedConverter(alias);
 
                    CodeMethodInvokeExpression mie = new CodeMethodInvokeExpression(null, converter);
                    mie.Parameters.Add(returnExpression);
                    returnExpression = mie;
 
                    method.ReturnType = new CodeTypeReference(ComAliasConverter.GetWFTypeFromComType(_method.ReturnType, alias));
                } 
 
                // add the return statement to the method.
                // 
                method.Statements.Add(new CodeMethodReturnStatement(returnExpression));
            }

            //VSQFE #830 addition 
            private static bool NonPrimitiveOptionalsOrMissingPresent(MethodInfo method) {
                ParameterInfo[] parameters = method.GetParameters(); 
 
                if (parameters != null && parameters.Length > 0) {
                    for (int iparam = 0; iparam < parameters.Length; ++iparam) { 
                        if (parameters[iparam].IsOptional && ((!parameters[iparam].ParameterType.IsPrimitive && !parameters[iparam].ParameterType.IsEnum) || parameters[iparam].DefaultValue == System.Reflection.Missing.Value)) {
                            return true;
                        }
                    } 
                }
 
                return false; 
            }
 
             // CodeDom only handles CLS which doesn't include the unsigned types, so just convert them to their
            // signed equivelent here and let the cast do the right thing.
            //
            private static object GetClsPrimitiveValue(object value) { 
                if (value is UInt32) {
                    return Convert.ChangeType(value, typeof(Int32), CultureInfo.InvariantCulture); 
                } 
                else if (value is UInt16) {
                    return Convert.ChangeType(value, typeof(Int16), CultureInfo.InvariantCulture); 
                }
                else if (value is UInt64) {
                    return Convert.ChangeType(value, typeof(Int64), CultureInfo.InvariantCulture);
                } 
                else if (value is SByte) {
                    return Convert.ChangeType(value, typeof(byte), CultureInfo.InvariantCulture); 
                } 
                return value;
            } 

            [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
            private static object GetDefaultValueForUnsignedType(Type parameterType, object value) {
                if (parameterType == typeof(UInt32)) { 
                    //UNBOX
                    Int32 newVar32 = 0; 
                    if (value is System.Int16) 
                    {
                        newVar32 = (System.Int16)value; 
                    }
                    if (value is System.Int32)
                    {
                        newVar32 = (System.Int32)value; 
                    }
                    if (value is System.Int64) 
                    { 
                        newVar32 = (System.Int32)value;
                    } 
                    return Convert.ToUInt32(Convert.ToString(newVar32, 16), 16);
                }
                else if (parameterType == typeof(UInt16)) {
                    //UNBOX 
                    Int16 newVar16 = (System.Int16)value;
                    return Convert.ToUInt16(Convert.ToString(newVar16, 16), 16); 
                } 
                else if (parameterType == typeof(UInt64)) {
                    //UNBOX 
                    Int64 newVar64 = 0;

                    if (value is System.Int16)
                    { 
                        newVar64 = (System.Int16)value;
                    } 
                    if (value is System.Int32) 
                    {
                        newVar64 = (System.Int32)value; 
                    }
                    if (value is System.Int64)
                    {
                        newVar64 = (System.Int64)value; 
                    }
                    return Convert.ToUInt64(Convert.ToString(newVar64, 16), 16); 
                } 
                return value;
            } 


            // Gets the "default" value for a given primative type.
            // Basically just converts Zero to the appropriate type. 
            //
            private static object GetPrimitiveDefaultValue(Type destType) { 
                if (destType == typeof(IntPtr) || destType == typeof(UIntPtr)) { 
                    // return actual zero, let the cast take care of it.
                    // 
                    return 0;
                }
                else {
                    return GetClsPrimitiveValue(Convert.ChangeType(0, destType, CultureInfo.InvariantCulture)); 
                }
            } 
 
            // Given parameter, come up with correct default expression for it.
            // Below is the table of cases that we need to handle 
            //
            //              Primitive   ValueType   Enum    ReferenceType Object
            //  Missing         (1)         (2*)     (3)        (4)         (9)
            //  Present         (5)         (6*)     (7)        (8)         (10) 
            //
            //  2*, 6* -- we don't have a good way of serializing arbitrary things 
            //        so we special case what we expect to get.  There may be cases we're missing here. 
            //
            // 
            [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
            private static CodeExpression GetDefaultExpressionForInvoke(MethodInfo method, AxParameterData parameterInfo) {
                object defaultValue = parameterInfo.ParameterInfo.DefaultValue;
                Type parameterType = parameterInfo.ParameterBaseType; 

                if (defaultValue == System.Reflection.Missing.Value) { 
                    // we need to come up with a reasonable default. 
                    // 1. For primitives, it's zero
                    // 2. For Enums it's zero if there is one, or it's the first value 
                    //    in the enum.
                    // 3. For others it's null.
                    //
                    if (parameterType.IsPrimitive) { 
                        defaultValue = GetPrimitiveDefaultValue(parameterType); // case (1)
                    } 
                    else if (parameterType.IsEnum) { 
                        // case (3)
                        defaultValue = 0; 
                        if (!Enum.IsDefined(parameterType, 0) && Enum.GetValues(parameterType).Length > 0) {
                            defaultValue = Enum.GetValues(parameterType).GetValue(0);
                        }
                    } 
                    else if (parameterType == typeof(object)) {
                        // case 9 
                        // if we have object, we've actually got a VARIANT here which 
                        // means we do pass System.Reflection.Missing.Value as the value.
                        // 
                        return new CodeFieldReferenceExpression(new CodeFieldReferenceExpression(null, "System.Reflection.Missing"), "Value");
                    }
                    else if (!parameterType.IsValueType) {
                        // case (4) 
                        if (parameterType == typeof(string)) {
                            defaultValue = ""; 
                        } 
                        else {
                            defaultValue = null; 
                        }
                        parameterType = null;
                    }
                    else if (parameterType.GetConstructor(new Type[0]) != null) { 
                        // case (2a)
                        // if there is a default public ctor, use that. 
                        // 
                        return new CodeObjectCreateExpression(parameterType);
                    } 
                    else if (parameterType == typeof(Decimal)) {
                        // case 2b
                        return new CodeObjectCreateExpression(typeof(Decimal), new CodeExpression[]{new CodePrimitiveExpression(0.0d)});
                    } 
                    else if (parameterType == typeof(DateTime)) {
                        // case 2c 
                        return new CodeObjectCreateExpression(typeof(DateTime), new CodeExpression[]{new CodePrimitiveExpression(0L)}); 
                    }
                    else { 

                        // case 2d

                        throw new Exception(SR.GetString(SR.AxImpNoDefaultValue, method.Name, parameterInfo.Name, parameterType.FullName)); 

                    } 
                } 
                else if (parameterType.IsPrimitive) {
                    // case 5 
                    defaultValue = GetClsPrimitiveValue(defaultValue);
                    defaultValue = GetDefaultValueForUnsignedType(parameterType, defaultValue);
                }
                else if (defaultValue != null && parameterType.IsInstanceOfType(defaultValue) && (defaultValue is DateTime || defaultValue is Decimal || defaultValue is bool)) { 
                    // case 6, 8
 
                    // here's where we put our nice little special case hacks since we don't have a good way to serialize things here and then 
                    // deserialize them at runtime.  We can only get a couple of types here so we just handle them specifically.
                    // 
                    if (defaultValue is DateTime) {
                        return new CodeObjectCreateExpression(typeof(DateTime), new CodeCastExpression(typeof(long), new CodePrimitiveExpression(((DateTime)defaultValue).Ticks)));
                    }
                    else if (defaultValue is Decimal) { 
                        return new CodeObjectCreateExpression(typeof(Decimal), new CodeCastExpression(typeof(Double), new CodePrimitiveExpression(Decimal.ToDouble((Decimal)defaultValue))));
                    } 
                    else if (defaultValue is bool) { 
                        return new CodePrimitiveExpression((bool)defaultValue);
                    } 
                    else if (defaultValue is string) {
                        // just fall through, we don't need a cast...we'll just push in the primitive
                        // with the string.
                        // 
                        parameterType = null;
                    } 
                    else { 
                        // we got a real instance here but we don't know hot to serialize it.  Need to add a special case!
                        // 
                        Debug.Fail("Unable to serialize type '" + parameterType.Name + "', with value '" + defaultValue.ToString() + "', do we need to special case this type?");
                        throw new Exception(SR.GetString(SR.AxImpUnrecognizedDefaultValueType, method.Name, parameterInfo.Name, parameterType.FullName));
                    }
                } 
                else if (!parameterType.IsValueType) {
 
                    // case 8 

                    // this is for things like stdole.StdFont, etc. 
                    //
                    if (defaultValue is DispatchWrapper) {
                        defaultValue = null;
                    } 

                    if (defaultValue == null || defaultValue is string) { 
                        // just go with null, no cast needed. 
                        //
                        parameterType = null; 
                        return new CodePrimitiveExpression(defaultValue);

                    }								
                    else { 
                        // case 10
                        // we got a real instance here but have no idea what to do with it; we have no way to serialize 
                        // arbitrary values. 
                        //
                        throw new Exception(SR.GetString(SR.AxImpUnrecognizedDefaultValueType, method.Name, parameterInfo.Name, parameterType.FullName)); 
                    }
                }

                // note case 7 (default specified Enum) is handled automatically here. 
                //
                if (parameterType != null && parameterType.IsEnum) { 
                    defaultValue = (int)defaultValue; 
                }
 
                CodeExpression expr = new CodePrimitiveExpression(defaultValue);
                if (parameterType != null) {
                    expr = new CodeCastExpression(parameterType, expr);
                } 
                return expr;
 
            } 

        } 


        /// The class that generates the relfection invoke calls, something like this:
        /// 
        ///  public virtual decimal MethodE(ref decimal e) {
        ///      if ((this.ocx == null)) { 
        ///          throw new System.Windows.Forms.AxHost.InvalidActiveXStateException("MethodE", System.Windows.Forms.AxHost.ActiveXInvokeKind.MethodInvoke); 
        ///      }
        ///      object[] paramArray = new object[] { 
        ///              e,
        ///              new decimal(0)};
        ///      System.Type typeVar = typeof(AxImpTestProject._UserControl1);
        ///      System.Reflection.MethodInfo methodToInvoke = typeVar.GetMethod("MethodE"); 
        ///      decimal returnValue = ((decimal)(methodToInvoke.Invoke(this.ocx, paramArray)));
        ///      e = ((decimal)(paramArray[0])); 
        ///      return returnValue; 
        /// }
        /// 
        private class AxReflectionInvokeMethodGenerator : AxMethodGenerator {

            internal AxReflectionInvokeMethodGenerator(MethodInfo method, bool removeOpts) : base(method, removeOpts){
            } 

            public override CodeExpression DoMethodInvokeCore(CodeMemberMethod method, string methodName, Type returnType, CodeExpression targetObject, List<CodeExpression> parameters) { 
 
                // Here we're generating something like:
                // 
                //            object[] paramArray = new object[] {
                //                              a,
                //                              null,
                //                              "variant default"}; 
                //                              System.Type typeVar = typeof(AxImpTestProject._UserControl1);
                //                              System.Reflection.MethodInfo methodToInvoke = typeVar.GetMethod("MethodF"); 
                // 
                //  and will invoke a method like:
                // 
                //            object returnVal = ((object)(methodToInvoke.Invoke(this.ocx, paramArray)));
                //
                //  so we'll do the work here to wrap up the parameters and munge the method name before we call down to the base.
                // 

 
                // first, create and initialize argument array 
                //
                CodeExpression[] initializers = parameters.ToArray(); 

                // for any that are out params, replace the value with null.
                //
                for (int iparam = 0; iparam < initializers.Length; ++iparam) { 

                    CodeVariableReferenceExpression paramRef = initializers[iparam] as CodeVariableReferenceExpression; 
 
                    if (paramRef == null) {
                        continue; 
                    }

                    AxParameterData paramData = paramRef.UserData[typeof(AxParameterData)] as AxParameterData;
 
                    // if we have param data, that means this wasn't an optional parameter and
                    // we need to specify null if it's an out param otherwise the compiler will 
                    // complain that we're using an out param without initializing it. 
                    //
                    if (paramData != null && paramData.Direction == FieldDirection.Out) { 
                        initializers[iparam] = new CodePrimitiveExpression(null);
                    }
                }
 

                // push the parameters into a param array 
                // 
                CodeArrayCreateExpression paramArrayExp = new CodeArrayCreateExpression(typeof(object), initializers);
                CodeVariableDeclarationStatement paramArrayDecl = new CodeVariableDeclarationStatement(typeof(object[]), "paramArray", paramArrayExp); 
                method.Statements.Add(paramArrayDecl);

                // generate the reflection code
                // 

                CodeTypeOfExpression toe = new CodeTypeOfExpression(ControlType); 
                CodeVariableDeclarationStatement typeVarDecl = new CodeVariableDeclarationStatement(typeof(Type), "typeVar", toe); 
                method.Statements.Add(typeVarDecl);
 
                CodeMethodInvokeExpression mie = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("typeVar"), "GetMethod", new CodeExpression[]{new CodePrimitiveExpression(methodName)});
                CodeVariableDeclarationStatement methodToInvokeDecl = new CodeVariableDeclarationStatement(typeof(MethodInfo), "methodToInvoke", mie);
                method.Statements.Add(methodToInvokeDecl);
 

                // build the new param list 
                // 
                List<CodeExpression> newParams = new List<CodeExpression>();
 
                // add the 'this.ocx' call
                //
                newParams.Add(targetObject);
                CodeVariableReferenceExpression paramArrayExpression = new CodeVariableReferenceExpression("paramArray"); 
                newParams.Add(paramArrayExpression);
 
                // and just call base... 
                //
                CodeExpression returnExpression = base.DoMethodInvokeCore(method, "Invoke", returnType, new CodeVariableReferenceExpression("methodToInvoke"), newParams); 

                // now we've got to pull any reference parameters out of the array and back into their variable.
                //
 
                //assign back non-optional ref and out parameters after the invoke
                for (int iparam = 0; iparam < parameters.Count; ++iparam) { 
 
                    CodeVariableReferenceExpression paramRef = parameters[iparam] as CodeVariableReferenceExpression;
 
                    if (paramRef == null) {
                        continue;
                    }
 
                    AxParameterData paramData = paramRef.UserData[typeof(AxParameterData)] as AxParameterData;
 
                    if (paramData == null || paramData.Direction == FieldDirection.In){ 
                        continue;
                    } 

                    // pull the value out of the array.
                    //
                    CodeExpression right = new CodeCastExpression(paramData.TypeName, 
                            new CodeArrayIndexerExpression(paramArrayExpression, new CodePrimitiveExpression(iparam)));
 
 
                    // assign it back to the variable.
                    // 
                    CodeAssignStatement cas = new CodeAssignStatement(paramRef, right);
                    method.Statements.Add(cas);
                }
 
                return returnExpression;
            } 
        } 

    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Windows.Forms.Design { 
    using System.Design;
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System; 
    using System.IO;
    using Microsoft.Win32; 
    using System.CodeDom; 
    using System.CodeDom.Compiler;
    using System.Collections; 
    using System.Collections.Generic;
    using System.Reflection;
    using System.ComponentModel;
    using System.Windows.Forms; 
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis; 
 
    /// <include file='doc\AxWrapperGen.uex' path='docs/doc[@for="AxWrapperGen"]/*' />
    /// <devdoc> 
    ///    <para>
    ///       Generates a wrapper for ActiveX controls for use in the design-time
    ///       environment.
    ///    </para> 
    /// </devdoc>
    /// <internalonly/> 
    public class AxWrapperGen { 
        private String axctlIface;
        private Type   axctlType; 
        private Guid   clsidAx;

        private String axctlEvents;
        private Type   axctlEventsType; 

        private String axctl; 
        private static String axctlNS; 

        private string memIface = null; 
        private string multicaster = null;
        private string cookie = null;

        private bool dispInterface = false; 
        private bool enumerableInterface = false;
 
        private string defMember = null; // Default Member (used for DefaultProperty attribute) 

        private string aboutBoxMethod = null; // Method name that handles the AboutBox(). 

        private CodeFieldReferenceExpression memIfaceRef = null;
        private CodeFieldReferenceExpression multicasterRef = null;
        private CodeFieldReferenceExpression cookieRef = null; 

        private ArrayList events = null; 
        /// <include file='doc\AxWrapperGen.uex' path='docs/doc[@for="AxWrapperGen.GeneratedSources"]/*' /> 
        public static ArrayList GeneratedSources = new ArrayList();
 
        private static Guid Guid_DataSource = new Guid("{7C0FFAB3-CD84-11D0-949A-00A0C91110ED}");

        internal static BooleanSwitch AxWrapper = new BooleanSwitch("AxWrapper", "ActiveX WFW wrapper generation.");
        internal static BooleanSwitch AxCodeGen = new BooleanSwitch("AxCodeGen", "ActiveX WFW property generation."); 

        // Attributes to add the NoBrowse/NoPersis attributes to selected properties. 
        private static CodeAttributeDeclaration nobrowse    = null; 
        private static CodeAttributeDeclaration browse      = null;
        private static CodeAttributeDeclaration nopersist   = null; 
        private static CodeAttributeDeclaration bindable    = null;
        private static CodeAttributeDeclaration defaultBind = null;

        // Optimization caches. 
        //
        private Hashtable axctlTypeMembers; 
        private Hashtable axHostMembers; 
        private Hashtable conflictableThings;
        private static Hashtable classesInNamespace; 
        private static Hashtable axHostPropDescs;

        private ArrayList dataSourceProps = new ArrayList();
 
        /// <include file='doc\AxWrapperGen.uex' path='docs/doc[@for="AxWrapperGen.AxWrapperGen"]/*' />
        /// <devdoc> 
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public AxWrapperGen(Type axType) { 
            axctl = axType.Name;
            axctl = axctl.TrimStart(new char[]{'_', '1'});
            axctl = "Ax" + axctl;
            clsidAx = axType.GUID; 

            Debug.WriteLineIf(AxWrapper.Enabled, "Found " + axctl + " as the ActiveX control. Guid: " + clsidAx.ToString() + " on : " + axType.FullName); 
 
            object[] custom = axType.GetCustomAttributes(typeof(ComSourceInterfacesAttribute), false);
 
            // If we didn't find the attribute on the class itself, let's see if we can find it on
            // the base type, if the base type happens to be an internal tlbimp helper class.
            //
            if (custom.Length == 0 && axType.BaseType.GUID.Equals(axType.GUID)) { 
                custom = axType.BaseType.GetCustomAttributes(typeof(ComSourceInterfacesAttribute), false);
                Debug.WriteLineIf(custom.Length > 0 && AxWrapper.Enabled, "Found ComSourceInterfacesAttribute in baseType: " + axType.BaseType); 
            } 

            if (custom.Length > 0) { 
                ComSourceInterfacesAttribute coms = (ComSourceInterfacesAttribute)custom[0];

                // The string is a \0 delimited string containing all interfaces implemented on this
                // COM object. The first one is the default events interface. 
                int indexIntf = coms.Value.IndexOfAny(new char[]{(char)0});
                Debug.Assert(indexIntf != -1, "Did not find delimiter in events name string: " + coms.Value); 
 
                string eventName = coms.Value.Substring(0, indexIntf);
                axctlEventsType = axType.Module.Assembly.GetType(eventName); 
                if (axctlEventsType == null) {
                    axctlEventsType = Type.GetType(eventName, false);
                }
                if (axctlEventsType != null) { 
                    axctlEvents = axctlEventsType.FullName;
                } 
 
                Debug.Assert(axctlEventsType != null, "Could not get event interface: " + coms.Value);
                Debug.WriteLineIf(AxWrapper.Enabled, "Assigned: " + axctlEvents + " as events interface"); 
            }
            else
                Debug.WriteLineIf(AxWrapper.Enabled, "No Events Interface defined for: " + axType.Name);
 
            Type[] interfaces = axType.GetInterfaces();
            axctlType = interfaces[0]; 
 
            // Look to see if this interface has a CoClassAttribute. If it does this
            // means that this is a helper interface that in turn derives from the 
            // default OCX interface.
            //
            foreach(Type iface in interfaces) {
                custom = iface.GetCustomAttributes(typeof(CoClassAttribute), false); 
                if (custom.Length > 0) {
                    Type[] ifaces = iface.GetInterfaces(); 
                    Debug.Assert(ifaces != null && ifaces.Length > 0, "No interfaces implemented on the CoClass"); 

                    if (ifaces != null && ifaces.Length > 0) { 
                        axctl = "Ax" + iface.Name;
                        axctlType = ifaces[0];
                        break;
                    } 
                }
            } 
 
            axctlIface = axctlType.Name;
            Debug.WriteLineIf(AxWrapper.Enabled, "Assigned: " + axctlIface + " as default interface"); 

            // Check to see if we want to implement IEnumerable on the ActiveX wrapper.
            //
            foreach(Type t in interfaces) { 
                if (t == typeof(System.Collections.IEnumerable)) {
                    Debug.WriteLineIf(AxWrapper.Enabled, "ActiveX control " + axctlType.FullName + " implements IEnumerable"); 
                    enumerableInterface = true; 
                    break;
                } 
            }

            try {
                // Check to see if the default interface is disp-only. 
                custom = axctlType.GetCustomAttributes(typeof(InterfaceTypeAttribute), false);
                if (custom.Length > 0) { 
                    InterfaceTypeAttribute intfType = (InterfaceTypeAttribute)custom[0]; 
                    dispInterface = (intfType.Value == ComInterfaceType.InterfaceIsIDispatch);
                } 
            }
            catch(MissingMethodException) {
                Debug.WriteLineIf(AxWrapper.Enabled, "The EE is not able to find the right ctor for InterfaceTypeAttribute");
            } 
        }
 
        private Hashtable AxHostMembers { 
            get {
                if (axHostMembers == null) 
                    FillAxHostMembers();
                return axHostMembers;
            }
        } 

        private Hashtable ConflictableThings { 
            get { 
                if (conflictableThings == null)
                    FillConflicatableThings(); 
                return conflictableThings;
            }
        }
 
        private void AddClassToNamespace(CodeNamespace ns, CodeTypeDeclaration cls) {
            if (classesInNamespace == null) { 
                classesInNamespace = new Hashtable(); 
            }
 
            try {
                ns.Types.Add(cls);
                classesInNamespace.Add(cls.Name, cls);
            } 
            catch (Exception e) {
                Debug.Fail("Failed to add " + cls.Name + " to types in Namespace. " + e); 
            } 
            catch {
                Debug.Fail("Failed to add " + cls.Name + " to types in Namespace. non-clscompliant exception encountered"); 
            }
        }

        private EventEntry AddEvent(string name, string eventCls, string eventHandlerCls, Type retType, AxParameterData[] parameters) { 
            if (events == null)
                events = new ArrayList(); 
 
            if (axctlTypeMembers == null) {
                axctlTypeMembers = new Hashtable(); 

                Type t = axctlType;

                MemberInfo[] members = t.GetMembers(); 
                foreach(MemberInfo member in members) {
                    string memberName = member.Name; 
                    if (!axctlTypeMembers.Contains(memberName)) { 
                        axctlTypeMembers.Add(memberName, member);
                    } 
                }
            }

            bool contain = axctlTypeMembers.Contains(name) || AxHostMembers.Contains(name) || ConflictableThings.Contains(name); 
            EventEntry entry = new EventEntry(name, eventCls, eventHandlerCls, retType, parameters, contain);
            events.Add(entry); 
            return entry; 
        }
 
        private bool ClassAlreadyExistsInNamespace(CodeNamespace ns, string clsName) {
            return classesInNamespace.Contains(clsName);
        }
 
        private static string Compile(AxImporter importer, CodeNamespace ns, string[] refAssemblies, DateTime tlbTimeStamp, Version version) {
            CodeDomProvider codeProvider = new Microsoft.CSharp.CSharpCodeProvider(); 
#pragma warning disable 618 
            ICodeGenerator codegen = codeProvider.CreateGenerator();
#pragma warning restore 618 

            // Build up the name of the output dll and the command line for the compiler.
            //
            string outputFileName = importer.options.outputName; 
            Debug.Assert(outputFileName != null, "No output filename!!!");
 
            string outputName = Path.Combine(importer.options.outputDirectory, outputFileName); 
            string fileName = Path.ChangeExtension(outputName, ".cs");
 
            CompilerParameters cparams = new CompilerParameters(refAssemblies, outputName);
            cparams.IncludeDebugInformation = importer.options.genSources;
            CodeCompileUnit cu = new CodeCompileUnit();
            cu.Namespaces.Add(ns); 

            CodeAttributeDeclarationCollection assemblyAttributes = cu.AssemblyCustomAttributes; 
            assemblyAttributes.Add(new CodeAttributeDeclaration("System.Reflection.AssemblyVersion", new CodeAttributeArgument(new CodePrimitiveExpression(version.ToString())))); 
            assemblyAttributes.Add(new CodeAttributeDeclaration("System.Windows.Forms.AxHost.TypeLibraryTimeStamp", new CodeAttributeArgument(new CodePrimitiveExpression(tlbTimeStamp.ToString()))));
            if (importer.options.delaySign) { 
                assemblyAttributes.Add(new CodeAttributeDeclaration("System.Reflection.AssemblyDelaySign", new CodeAttributeArgument(new CodePrimitiveExpression(true))));
            }
            if (importer.options.keyFile != null && importer.options.keyFile.Length > 0) {
                assemblyAttributes.Add(new CodeAttributeDeclaration("System.Reflection.AssemblyKeyFile", new CodeAttributeArgument(new CodePrimitiveExpression(importer.options.keyFile)))); 
            }
            if (importer.options.keyContainer != null && importer.options.keyContainer.Length > 0) { 
                assemblyAttributes.Add(new CodeAttributeDeclaration("System.Reflection.AssemblyKeyName", new CodeAttributeArgument(new CodePrimitiveExpression(importer.options.keyContainer)))); 
            }
 
            // Compile the file into a DLL.
            //
            CompilerResults results;
 
            if (importer.options.genSources) {
                Debug.WriteLineIf(AxWrapperGen.AxWrapper.Enabled, "Compiling " + ns.Name + ".cs" + " to " + outputName); 
                SaveCompileUnit(codegen, cu, fileName); 
                results = ((ICodeCompiler)codegen).CompileAssemblyFromFile(cparams, fileName);
            } 
            else {
                results = ((ICodeCompiler)codegen).CompileAssemblyFromDom(cparams, cu);
            }
 
            // Walk through any errors and warnings and build up the correct exception string if needed.
            // 
            if (results.Errors != null && results.Errors.Count > 0) { 
                string errorText = null;
                CompilerErrorCollection errors = results.Errors; 

                Debug.WriteLineIf(AxWrapperGen.AxWrapper.Enabled, "Errors#: " + errors.Count);
                foreach(CompilerError err in errors) {
                    Debug.WriteLineIf(AxWrapperGen.AxWrapper.Enabled, err.ToString()); 

                    // Skip warnings... 
                    // 
                    if (!err.IsWarning)
                        errorText = errorText + err.ToString() + "\r\n"; 
                }

                if (errorText != null) {
                    SaveCompileUnit(codegen, cu, fileName); 
                    errorText = SR.GetString(SR.AXCompilerError, ns.Name, fileName) + "\r\n" + errorText;
                    throw new Exception(errorText); 
                } 
            }
 
            return outputName;
        }

        private string CreateDataSourceFieldName(string propName) { 
            return "ax" + propName;
        } 
 
        private CodeParameterDeclarationExpression CreateParamDecl(string type, string name, bool isOptional) {
            CodeParameterDeclarationExpression paramDecl = new CodeParameterDeclarationExpression(type, name); 

            if (!isOptional)
                return paramDecl;
 
            CodeAttributeDeclarationCollection paramAttrs = new CodeAttributeDeclarationCollection();
            paramAttrs.Add(new CodeAttributeDeclaration("System.Runtime.InteropServices.Optional", new CodeAttributeArgument[0])); 
            paramDecl.CustomAttributes = paramAttrs; 
            return paramDecl;
        } 

        private CodeConditionStatement CreateValidStateCheck() {
            CodeConditionStatement ifstat = new CodeConditionStatement();
            CodeBinaryOperatorExpression cond1; 
            CodeBinaryOperatorExpression cond2;
            CodeBinaryOperatorExpression condAnd; 
 
            cond1 = new CodeBinaryOperatorExpression(memIfaceRef, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null));
            cond2 = new CodeBinaryOperatorExpression(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "PropsValid"), 
                                                     CodeBinaryOperatorType.IdentityEquality,
                                                     new CodePrimitiveExpression(true));
            condAnd = new CodeBinaryOperatorExpression(cond1, CodeBinaryOperatorType.BooleanAnd, cond2);
 
            ifstat = new CodeConditionStatement();
            ifstat.Condition = condAnd; 
            return ifstat; 
        }
 
        private CodeStatement CreateInvalidStateException(string name, string kind) {
            CodeBinaryOperatorExpression cond = new CodeBinaryOperatorExpression(memIfaceRef,
                                                                                 CodeBinaryOperatorType.IdentityEquality,
                                                                                 new CodePrimitiveExpression(null)); 
            CodeConditionStatement ifstat = new CodeConditionStatement();
            ifstat.Condition = cond; 
 
            CodeExpression[] createParams = new CodeExpression[] {
                new CodePrimitiveExpression(name), 
                new CodeFieldReferenceExpression(new CodeFieldReferenceExpression(null, typeof(AxHost).FullName + ".ActiveXInvokeKind"), kind)
            };

            CodeObjectCreateExpression invalidState = new CodeObjectCreateExpression(typeof(AxHost.InvalidActiveXStateException).FullName, createParams); 

            ifstat.TrueStatements.Add(new CodeThrowExceptionStatement(invalidState)); 
            return ifstat; 
        }
 
        private void FillAxHostMembers() {
            if (axHostMembers == null) {
                axHostMembers = new Hashtable(StringComparer.CurrentCultureIgnoreCase);
 
                Type t = typeof(AxHost);
 
                MemberInfo[] members = t.GetMembers(); 
                foreach(MemberInfo member in members) {
                    string memberName = member.Name; 

                    if (!axHostMembers.Contains(memberName)) {
                        // Check to see if this is a field
                        // 
                        FieldInfo fi = member as FieldInfo;
                        if (fi != null && !fi.IsPrivate) { 
                            axHostMembers.Add(memberName, member); 
                            continue;
                        } 

                        // Check to see if this is a property
                        //
                        PropertyInfo pi = member as PropertyInfo; 
                        if (pi != null) {
                            axHostMembers.Add(memberName, member); 
                            continue; 
                        }
 
                        // Check to see if this is a ctor or method.
                        //
                        MethodBase mb = member as MethodBase;
                        if (mb != null && !mb.IsPrivate) { 
                            axHostMembers.Add(memberName, member);
                            continue; 
                        } 

                        // Check to see if this is a ctor or method. 
                        //
                        EventInfo ei = member as EventInfo;
                        if (ei != null) {
                            axHostMembers.Add(memberName, member); 
                            continue;
                        } 
 
                        // Check to see if this is a ctor or method.
                        // 
                        Type type = member as Type;
                        if (type != null && (type.IsPublic || type.IsNestedPublic)) {
                            axHostMembers.Add(memberName, member);
                            continue; 
                        }
 
                        Debug.Fail("Failed to process AxHost member " + member.ToString() + " " + member.GetType().FullName); 
                        axHostMembers.Add(memberName, member);
                    } 
                }
            }
        }
 
        private void FillConflicatableThings() {
            if (conflictableThings == null) { 
                conflictableThings = new Hashtable(); 
                conflictableThings.Add("System", "System");
            } 
        }

        private static void SaveCompileUnit(ICodeGenerator codegen, CodeCompileUnit cu, string fileName) {
            // Persist to file. 
            Debug.WriteLineIf(AxWrapperGen.AxWrapper.Enabled, "Generating source file: " + fileName);
            try { 
                try { 
                    if (File.Exists(fileName))
                        File.Delete(fileName); 
                }
                catch {
                    Debug.WriteLineIf(AxWrapperGen.AxWrapper.Enabled, "Could not delete: " + fileName);
                } 

                FileStream file = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite); 
                StreamWriter stream = new StreamWriter(file, new System.Text.UTF8Encoding(false)); 
                codegen.GenerateCodeFromCompileUnit(cu, stream, null);
                stream.Flush(); 
                stream.Close();
                file.Close();
                GeneratedSources.Add( fileName );
                Debug.WriteLineIf(AxWrapperGen.AxWrapper.Enabled, "Generated source file: " + fileName); 
            }
            catch (Exception e) { 
                Debug.WriteLineIf(AxWrapperGen.AxWrapper.Enabled, "Exception generating source file: " + e.ToString()); 
            }
            catch { 
                Debug.WriteLineIf(AxWrapperGen.AxWrapper.Enabled, "Non CLSCompliant Exception generating source file: ");
            }
        }
 
        internal static string MapTypeName(Type type) {
            bool isArray = type.IsArray; 
 
            Type baseType = type.GetElementType();
            if (baseType != null) 
                type = baseType;

            string typeName = type.FullName;
            return (isArray) ? (typeName + "[]") : typeName; 
        }
 
        private static bool IsTypeActiveXControl(Type type) { 
            if (type.IsClass && type.IsCOMObject && type.IsPublic && !type.GUID.Equals(Guid.Empty)) {
 
                // Check to see if the type is ComVisible. Otherwise, this is a internal helper type from tlbimp.
                //
                try {
                    object[] attrs = type.GetCustomAttributes(typeof(ComVisibleAttribute), false); 
                    if (attrs.Length != 0 && ((ComVisibleAttribute)attrs[0]).Value == false) {
                        return false; 
                    } 
                }
                catch { 
                    return false;
                }

                // Look for the Control key under the Classes_Root\CLSID to see if this is the ActiveX control. 
                //
                Guid clsid = type.GUID; 
                string controlKey = "CLSID\\{" + clsid.ToString() + "}\\Control"; 
                RegistryKey k = Registry.ClassesRoot.OpenSubKey(controlKey);
                if (k == null) 
                    return false;

                k.Close();
                Debug.WriteLineIf(AxWrapper.Enabled, "Found key: " + controlKey); 

                // Make sure this type implements atleast the default interface. 
                // 
                Type[] ifaces = type.GetInterfaces();
                Debug.WriteLineIf(ifaces.Length < 1 && AxWrapper.Enabled, "Not even one interface implemented on: " + type.FullName); 

                if (ifaces != null && ifaces.Length >= 1)
                    return true;
            } 

            return false; 
        } 

        /// <include file='doc\AxWrapperGen.uex' path='docs/doc[@for="AxWrapperGen.GenerateWrappers"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal static string GenerateWrappers(AxImporter importer, Guid axClsid, Assembly rcwAssem, string[] refAssemblies, DateTime tlbTimeStamp, out string assem) { 
            assem = null;
 
            bool fFoundClass = false; 
            CodeNamespace ns = null;
            string retAxCtlName = null; 

            try {
                Type[] types = rcwAssem.GetTypes();
                for (int i = 0; i < types.Length; ++i) { 
                    if (IsTypeActiveXControl(types[i])) {
                        fFoundClass = true; 
 
                        // Create a namespace for the AxWrappers.
                        // 
                        if (ns == null) {
                            // Determine the namespace for the ActiveX control wrappers.
                            //
                            axctlNS = "Ax" + types[i].Namespace; 
                            ns = new CodeNamespace(axctlNS);
                        } 
 
                        // Generate code for the ActiveX control wrapper.
                        // 
                        AxWrapperGen axwrapper = new AxWrapperGen(types[i]);
                        axwrapper.GenerateAxHost(ns, refAssemblies);

                        // If we are given a specific GUID, then we should return the type for that control, 
                        // otherwise, we will return the type of the first ActiveX control that we generate
                        // wrapper for. 
                        // 
                        if (!axClsid.Equals(Guid.Empty) && axClsid.Equals(types[i].GUID)) {
                            Debug.Assert(retAxCtlName == null, "Two controls match the same GUID... " + retAxCtlName + " and " + types[i].FullName); 
                            retAxCtlName = axwrapper.axctl;
                        }
                        else if (axClsid.Equals(Guid.Empty) && retAxCtlName == null) {
                            retAxCtlName = axwrapper.axctl; 
                        }
                    } 
                } 
            }
            finally { 
                if (classesInNamespace != null) {
                    classesInNamespace.Clear();
                    classesInNamespace = null;
                } 
            }
 
            AssemblyName an = rcwAssem.GetName(); 

            if (fFoundClass) { 
                // Now that we found atleast one ActiveX control, we should compile the namespace into
                // an assembly.
                //
                Debug.Assert(ns != null, "ActiveX control found but no code generated!!!!"); 

                Version version = an.Version; 
                assem = Compile(importer, ns, refAssemblies, tlbTimeStamp, version); 

                // Return the type of the ActiveX control. 
                //
                if (assem != null) {
                    if (retAxCtlName == null)
                        throw new Exception(SR.GetString(SR.AXNotValidControl, "{" + axClsid + "}")); 

                    return axctlNS + "." + retAxCtlName + "," + axctlNS; 
                } 
            }
#if DEBUG 
            else {
                Debug.WriteLineIf(AxWrapper.Enabled, "Did not find any ActiveX control in: " + an.Name);
            }
#endif // DEBUG 

            return null; 
        } 

        private void GenerateAxHost(CodeNamespace ns, string[] refAssemblies) { 
            CodeTypeDeclaration cls = new CodeTypeDeclaration();
            cls.Name = axctl;
            cls.BaseTypes.Add(typeof(AxHost).FullName);
 
            if (enumerableInterface) {
                cls.BaseTypes.Add(typeof(System.Collections.IEnumerable)); 
            } 

            CodeAttributeDeclarationCollection clsAttrs = new CodeAttributeDeclarationCollection(); 

            CodeAttributeDeclaration guidAttr = new CodeAttributeDeclaration(typeof(System.Windows.Forms.AxHost.ClsidAttribute).FullName,
                                                         new CodeAttributeArgument[] {new CodeAttributeArgument(new CodeSnippetExpression("\"{" + clsidAx.ToString() + "}\""))});
 
            clsAttrs.Add(guidAttr);
 
            // Generate the DesignTimeVisible attribute so that the control shows up in the toolbox. 
            //
 
            CodeAttributeDeclaration designAttr = new CodeAttributeDeclaration(typeof(System.ComponentModel.DesignTimeVisibleAttribute).FullName,
                                                         new CodeAttributeArgument[] {new CodeAttributeArgument(new CodePrimitiveExpression(true))});

            clsAttrs.Add(designAttr); 

            cls.CustomAttributes = clsAttrs; 
 
            // See if there is a DefaultAttribute on the interface. If so, convert it to a DefaultPropertyAttribute.
            object[] attr = axctlType.GetCustomAttributes(typeof(System.Reflection.DefaultMemberAttribute), true); 
            if (attr != null && attr.Length > 0) {
                defMember = ((DefaultMemberAttribute)attr[0]).MemberName;
            }
 
            AddClassToNamespace(ns, cls);
 
            WriteMembersDecl(cls); 

            if (axctlEventsType != null) { 
                WriteEventMembersDecl(ns, cls);
            }

            CodeConstructor ctor = WriteConstructor(cls); 

            WriteProperties(cls); 
            WriteMethods(cls); 

            WriteHookupMethods(cls); 

            // Hookup the AboutBox delegate if one exists for this control.
            //
            if (aboutBoxMethod != null) { 
                CodeObjectCreateExpression aboutDelegate = new CodeObjectCreateExpression("AboutBoxDelegate");
                aboutDelegate.Parameters.Add(new CodeFieldReferenceExpression(null, aboutBoxMethod)); 
 
                CodeMethodInvokeExpression aboutAdd = new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetAboutBoxDelegate");
                aboutAdd.Parameters.Add(aboutDelegate); 

                ctor.Statements.Add(new CodeExpressionStatement(aboutAdd));
            }
 
            if (axctlEventsType != null)
                WriteEvents(ns, cls); 
 
            // If there is a need to generate a override for OnInPlaceActive()
            // do it now. 
            //
            if (dataSourceProps.Count > 0) {
                WriteOnInPlaceActive(cls);
            } 
        }
 
        private CodeExpression GetInitializer(Type type) { 
            if (type == null)
                return new CodePrimitiveExpression(null); 
            else if (type == typeof(Int32) || type == typeof(short) || type == typeof(Int64) || type == typeof(Single) || type == typeof(Double) || typeof(Enum).IsAssignableFrom(type))
                return new CodePrimitiveExpression(0);
            else if (type == typeof(char))
                return new CodeCastExpression("System.Character", new CodePrimitiveExpression(0)); 
            else if (type == typeof(bool))
                return new CodePrimitiveExpression(false); 
            else 
                return new CodePrimitiveExpression(null);
        } 

        private bool IsDispidKnown(int dp, string propName) {
            return dp == NativeMethods.ActiveX.DISPID_FORECOLOR ||
                   dp == NativeMethods.ActiveX.DISPID_BACKCOLOR || 
                   dp == NativeMethods.ActiveX.DISPID_FONT ||
                   dp == NativeMethods.ActiveX.DISPID_ENABLED || 
                   dp == NativeMethods.ActiveX.DISPID_TABSTOP || 
                   dp == NativeMethods.ActiveX.DISPID_RIGHTTOLEFT ||
                   dp == NativeMethods.ActiveX.DISPID_TEXT || 
                   dp == NativeMethods.ActiveX.DISPID_HWND ||
                   (dp == NativeMethods.ActiveX.DISPID_VALUE && propName.Equals(defMember));
        }
 
        private bool IsEventPresent(MethodInfo mievent) {
            //return TypeDescriptor.GetEvent(typeof(AxHost), eventsRef[i].Name) != null; 
 
            return false;
 
            /*
            Type axHostType = typeof(AxHost);
            ParameterInfo[] parameters = mievent.GetParameters();
 
            Type[] paramList = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; ++i) 
                paramList[i] = parameters[i].ParameterType; 

            try { 
                MethodInfo mi = axHostType.GetMethod("RaiseOn" + mievent.Name, paramList);
                bool f = (mi != null) && (mi.ReturnType == mievent.ReturnType);
                return f;
            } 
            catch (AmbiguousMatchException) {
                return true; 
            } 
            */
        } 

        private bool IsPropertyBindable(PropertyInfo pinfo, out bool isDefaultBind) {
            isDefaultBind = false;
 
            MethodInfo getter = pinfo.GetGetMethod();
            if (getter == null) 
                return false; 

            object[] attr = getter.GetCustomAttributes(typeof(TypeLibFuncAttribute), false); 
            if (attr != null && attr.Length > 0) {
                TypeLibFuncFlags flags = ((TypeLibFuncAttribute)attr[0]).Value;

                isDefaultBind = ((int)flags & (int)TypeLibFuncFlags.FDefaultBind) != 0; 

                if (isDefaultBind || ((int)flags & (int)TypeLibFuncFlags.FBindable) != 0) { 
                    return true; 
                }
            } 

            return false;
        }
 
        private bool IsPropertyBrowsable(PropertyInfo pinfo, ComAliasEnum alias) {
            MethodInfo getter = pinfo.GetGetMethod(); 
            if (getter == null) 
                return false;
 
            object[] attr = getter.GetCustomAttributes(typeof(TypeLibFuncAttribute), false);
            if (attr != null && attr.Length > 0) {
                TypeLibFuncFlags flags = ((TypeLibFuncAttribute)attr[0]).Value;
                if (((int)flags & (int)TypeLibFuncFlags.FNonBrowsable) != 0 || ((int)flags & (int)TypeLibFuncFlags.FHidden) != 0) { 
                    return false;
                } 
            } 

            // Hide all properties that have COM objects that are not of the DataSource type 
            // and do not have their type converted to a Windows Forms type.
            //
            Type t = pinfo.PropertyType;
            if (alias == ComAliasEnum.None && t.IsInterface && !t.GUID.Equals(Guid_DataSource)) { 
                return false;
            } 
 
            return true;
        } 

        private bool IsPropertySignature(PropertyInfo pinfo, out bool useLet) {
            int nParams = 0;
            bool isProperty = true; 

            useLet = false; 
 
            // Handle Indexed properties.
            string defProp = ((defMember == null) ? "Item" : defMember); 
            if (pinfo.Name.Equals(defProp))
                nParams = pinfo.GetIndexParameters().Length;

            if (pinfo.GetGetMethod() != null) 
                isProperty = IsPropertySignature(pinfo.GetGetMethod(), pinfo.PropertyType, true, nParams);
            if (pinfo.GetSetMethod() != null) { 
                isProperty = isProperty && IsPropertySignature(pinfo.GetSetMethod(), pinfo.PropertyType, false, nParams + 1); 

                if (!isProperty) { 
                    MethodInfo letMethod = pinfo.DeclaringType.GetMethod("let_" + pinfo.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    if (letMethod != null) {
                        isProperty = IsPropertySignature(letMethod, pinfo.PropertyType, false, nParams + 1);
                        useLet = true; 
                    }
                } 
            } 

            return isProperty; 
        }

        private bool IsPropertySignature(MethodInfo method, out bool hasPropInfo, out bool useLet) {
            useLet = false; 
            hasPropInfo = false;
 
            bool getter = method.Name.StartsWith("get_"); 
            if (!getter && !method.Name.StartsWith("set_") && !method.Name.StartsWith("let_"))
                return false; 

            string propName = method.Name.Substring(4, method.Name.Length - 4);
            PropertyInfo pinfo = axctlType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
 
            Debug.Assert(pinfo != null, "No property found for:" + propName);
            if (pinfo == null) 
                return false; 

            return IsPropertySignature(pinfo, out useLet); 
        }

        private bool IsPropertySignature(MethodInfo method, Type returnType, bool getter, int nParams) {
            if (method.IsConstructor) return false; 

            // If there is a property of the same name, we handle it differently. 
            if (getter) { 
                Debug.Assert(method.Name.StartsWith("get_"), "Property get: " + method.Name + " does not start with get_!!!");
                String name = method.Name.Substring(4); 
                if (axctlType.GetProperty(name) != null && method.GetParameters().Length == nParams)
                    return method.ReturnType == returnType;
            }
            else { 
                Debug.Assert(method.Name.StartsWith("set_") || method.Name.StartsWith("let_"), "Property set: " + method.Name + " does not start with set_ or a let_!!!");
                String name = method.Name.Substring(4); 
                ParameterInfo[] parameters = method.GetParameters(); 
                if (axctlType.GetProperty(name) != null && parameters.Length == nParams) {
                    if (parameters.Length > 0) 
                        return parameters[parameters.Length-1].ParameterType == returnType ||
                            (method.Name.StartsWith("let_") && parameters[parameters.Length-1].ParameterType == typeof(object));
                    return true;
                } 
            }
 
            return false; 
        }
 

        //VSQFE #830 addition
        private bool OptionalsPresent(MethodInfo method) {
            AxParameterData[] parameters = AxParameterData.Convert(method.GetParameters()); 

            if (parameters != null && parameters.Length > 0) { 
                for (int iparam = 0; iparam < parameters.Length; ++iparam) { 
                    if (parameters[iparam].IsOptional) {
                        return true; 
                    }
                }
            }
 
            return false;
        } 
 

        private string ResolveConflict(string name, Type returnType, out bool fOverride, out bool fUseNew) { 
            fOverride = false;
            fUseNew = false;

            string prefix = ""; 
            try {
                if (axHostPropDescs == null) { 
                    axHostPropDescs = new Hashtable(); 

                    PropertyInfo[] props = typeof(AxHost).GetProperties(); 
                    foreach(PropertyInfo prop in props) {
                        axHostPropDescs.Add(prop.Name + prop.PropertyType.GetHashCode(), prop);
                    }
                } 

                PropertyInfo pinfo = (PropertyInfo)axHostPropDescs[name + returnType.GetHashCode()]; 
                if (pinfo != null) { 
                    if (returnType.Equals(pinfo.PropertyType)) {
                        bool isVirtual = false; 
                        isVirtual = (pinfo.CanRead) ? pinfo.GetGetMethod().IsVirtual : false;

                        if (isVirtual)
                            fOverride = true; 
                        else
                            fUseNew = true; 
                    } 
                    else {
                        prefix = "Ctl"; 
                    }
                }
                else {
                    if (AxHostMembers.Contains(name) || ConflictableThings.Contains(name)) { 
                        prefix = "Ctl";
                    } 
                    else { 
                        if (name.StartsWith("get_") || name.StartsWith("set_")) {
                            if (TypeDescriptor.GetProperties(typeof(AxHost))[name.Substring(4)] != null) 
                               prefix = "Ctl";
                        }
                    }
                } 
            }
            catch (AmbiguousMatchException) { 
                prefix = "Ctl"; 
            }
 
    #if DEBUG
            if (fOverride)
                Debug.Assert(prefix.Length == 0, "Have override and Ctl prefix for: " + name);
            if (fUseNew) 
                Debug.Assert(prefix.Length == 0, "Have new and Ctl prefix for: " + name);
            if (AxCodeGen.Enabled && prefix.Length != 0) Debug.WriteLine("Resolved conflict for: " + name); 
            if (AxCodeGen.Enabled && fOverride) Debug.WriteLine("Resolved conflict for: " + name + " through override"); 
            if (AxCodeGen.Enabled && fUseNew) Debug.WriteLine("Resolved conflict for: " + name + " through new");
    #endif 
            return prefix;
        }

        private CodeConstructor WriteConstructor(CodeTypeDeclaration cls) { 
            CodeConstructor ctor = new CodeConstructor();
            ctor.Attributes = MemberAttributes.Public; 
 
            ctor.BaseConstructorArgs.Add(new CodeSnippetExpression("\"" + clsidAx.ToString() + "\""));
            cls.Members.Add(ctor); 
            return ctor;
        }

        private void WriteOnInPlaceActive(CodeTypeDeclaration cls) { 
            CodeMemberMethod oipMeth = new CodeMemberMethod();
            oipMeth.Name = "OnInPlaceActive"; 
            oipMeth.Attributes = MemberAttributes.Family | MemberAttributes.Override; 

            CodeMethodInvokeExpression baseOip = new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), "OnInPlaceActive"); 
            oipMeth.Statements.Add(new CodeExpressionStatement(baseOip));

            foreach(PropertyInfo pinfo in dataSourceProps) {
                string fieldName = CreateDataSourceFieldName(pinfo.Name); 

                CodeBinaryOperatorExpression cond = new CodeBinaryOperatorExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), 
                                                                                     CodeBinaryOperatorType.IdentityInequality, 
                                                                                     new CodePrimitiveExpression(null));
                CodeConditionStatement ifstat = new CodeConditionStatement(); 
                ifstat.Condition = cond;

                CodeExpression left  = new CodeFieldReferenceExpression(memIfaceRef, pinfo.Name);
                CodeExpression right = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName); 
                ifstat.TrueStatements.Add(new CodeAssignStatement(left, right));
 
                oipMeth.Statements.Add(ifstat); 
            }
 
            cls.Members.Add(oipMeth);
        }

        private String WriteEventClass(CodeNamespace ns, MethodInfo mi, ParameterInfo[] pinfos) { 
            String evntCls = axctlEventsType.Name + "_" + mi.Name + "Event";
            if (ClassAlreadyExistsInNamespace(ns, evntCls)) { 
                return evntCls; 
            }
 
            CodeTypeDeclaration cls = new CodeTypeDeclaration();
            cls.Name = evntCls;

            AxParameterData[] parameters = AxParameterData.Convert(pinfos); 
            for (int i = 0; i < parameters.Length; ++i) {
                CodeMemberField field = new CodeMemberField(parameters[i].TypeName, parameters[i].Name); 
                field.Attributes = MemberAttributes.Public | MemberAttributes.Final; 
                cls.Members.Add(field);
            } 

            CodeConstructor ctor = new CodeConstructor();
            ctor.Attributes = MemberAttributes.Public;
 
            for (int i = 0; i < parameters.Length; ++i) {
                if (parameters[i].Direction != FieldDirection.Out) { 
                    ctor.Parameters.Add(CreateParamDecl(parameters[i].TypeName, parameters[i].Name, false)); 

                    CodeFieldReferenceExpression left = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), parameters[i].Name); 
                    CodeFieldReferenceExpression right = new CodeFieldReferenceExpression(null, parameters[i].Name);
                    CodeAssignStatement assign = new CodeAssignStatement(left, right);
                    ctor.Statements.Add(assign);
                } 
            }
            cls.Members.Add(ctor); 
 
            AddClassToNamespace(ns, cls);
            return evntCls; 
        }

        private String WriteEventHandlerClass(CodeNamespace ns, MethodInfo mi) {
            String evntCls = axctlEventsType.Name + "_" + mi.Name + "EventHandler"; 
            if (ClassAlreadyExistsInNamespace(ns, evntCls)) {
                return evntCls; 
            } 

            CodeTypeDelegate cls = new CodeTypeDelegate(); 
            cls.Name = evntCls;
            cls.Parameters.Add(CreateParamDecl(typeof(object).FullName, "sender", false));
            cls.Parameters.Add(CreateParamDecl(axctlEventsType.Name + "_" + mi.Name + "Event", "e", false));
            cls.ReturnType = new CodeTypeReference(mi.ReturnType); 

            AddClassToNamespace(ns, cls); 
            return evntCls; 
        }
 
        private void WriteEventMembersDecl(CodeNamespace ns, CodeTypeDeclaration cls) {
            bool eventAttr = false;

            MethodInfo[] events = axctlEventsType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly); 

            for (int i = 0; i < events.Length; ++i) { 
                EventEntry entry = null; 

                if (!IsEventPresent(events[i])) { 
                    ParameterInfo[] parameters = events[i].GetParameters();

                    if (parameters.Length > 0 || events[i].ReturnType != typeof(void)) {
                        String eventHCls = WriteEventHandlerClass(ns, events[i]); 
                        String eventCls = WriteEventClass(ns, events[i], parameters);
                        entry = AddEvent(events[i].Name, eventCls, eventHCls, events[i].ReturnType, AxParameterData.Convert(parameters)); 
                    } 
                    else {
                        entry = AddEvent(events[i].Name, "System.EventArgs", "System.EventHandler", typeof(void), new AxParameterData[0]); 
                    }
                }

                if (!eventAttr) { 
                    object[] attrs = events[i].GetCustomAttributes(typeof(DispIdAttribute), false);
                    if (attrs == null || attrs.Length == 0) { 
                        continue; 
                    }
 
                    DispIdAttribute dispid = (DispIdAttribute)attrs[0];
                    if (dispid.Value == 1) {
                        string eventName = (entry != null) ? entry.resovledEventName : events[i].Name;
 
                        CodeAttributeDeclaration defEventAttr = new CodeAttributeDeclaration("System.ComponentModel.DefaultEvent",
                                                                     new CodeAttributeArgument[] {new CodeAttributeArgument(new CodePrimitiveExpression(eventName))}); 
                        cls.CustomAttributes.Add(defEventAttr); 
                        eventAttr = true;
                    } 
                }
            }

            Debug.WriteLineIf(AxCodeGen.Enabled && !eventAttr, "No default event found for: " + axctlEventsType.FullName); 
        }
 
        private string WriteEventMulticaster(CodeNamespace ns) { 
            String evntCls = axctl + "EventMulticaster";
            if (ClassAlreadyExistsInNamespace(ns, evntCls)) { 
                return evntCls;
            }

            CodeTypeDeclaration cls = new CodeTypeDeclaration(); 
            cls.Name = evntCls;
            cls.BaseTypes.Add(axctlEvents); 
 
            // Set the ClassInterface attribute
            CodeAttributeDeclarationCollection clsAttrs = new CodeAttributeDeclarationCollection(); 
            CodeAttributeDeclaration clsIfaceType = new CodeAttributeDeclaration("System.Runtime.InteropServices.ClassInterface", new CodeAttributeArgument[] {
                                                                                                                                                              new CodeAttributeArgument(new CodeFieldReferenceExpression(
                                                                                                                                                              new CodeFieldReferenceExpression(null, "System.Runtime.InteropServices.ClassInterfaceType"), "None"))
                                                                                                                                                          }); 
            clsAttrs.Add(clsIfaceType);
            cls.CustomAttributes = clsAttrs; 
 
            CodeMemberField field = new CodeMemberField(axctl, "parent");
            field.Attributes = MemberAttributes.Private | MemberAttributes.Final; 
            cls.Members.Add(field);

            CodeConstructor ctor = new CodeConstructor();
            ctor.Attributes = MemberAttributes.Public; 
            ctor.Parameters.Add(CreateParamDecl(axctl, "parent", false));
 
            CodeFieldReferenceExpression right = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "parent"); 
            CodeFieldReferenceExpression left  = new CodeFieldReferenceExpression(null, "parent");
            ctor.Statements.Add(new CodeAssignStatement(right, left)); 
            cls.Members.Add(ctor);

            MethodInfo[] eventsRef = axctlEventsType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            int iEvent = 0; 
            for (int i = 0; i < eventsRef.Length; ++i) {
                AxParameterData[] parameters = AxParameterData.Convert(eventsRef[i].GetParameters()); 
 
                CodeMemberMethod method = new CodeMemberMethod();
                method.Name = eventsRef[i].Name; 
                method.Attributes = MemberAttributes.Public;
                method.ReturnType = new CodeTypeReference(MapTypeName(eventsRef[i].ReturnType));

                for (int iparam = 0; iparam < parameters.Length; ++iparam) { 
                    CodeParameterDeclarationExpression param = CreateParamDecl(MapTypeName(parameters[iparam].ParameterType), parameters[iparam].Name, parameters[iparam].IsOptional);
                    param.Direction = parameters[iparam].Direction; 
                    method.Parameters.Add(param); 
                }
 
                CodeFieldReferenceExpression parent = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "parent");
                if (!IsEventPresent(eventsRef[i])) {
                    EventEntry ee = (EventEntry)events[iEvent++];
 
                    Debug.Assert(eventsRef[i].Name.Equals(ee.eventName), "Not hadling the right event!!!");
 
                    CodeExpressionCollection paramstr = new CodeExpressionCollection(); 
                    paramstr.Add(parent);
                    if (ee.eventCls.Equals("EventArgs")) { 
                        paramstr.Add(new CodeFieldReferenceExpression(new CodeFieldReferenceExpression(null, "EventArgs"), "Empty"));

                        CodeExpression[] temp = new CodeExpression[paramstr.Count];
                        ((IList)paramstr).CopyTo(temp, 0); 
                        CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(parent, ee.invokeMethodName, temp);
                        if (eventsRef[i].ReturnType == typeof(void)) { 
                            method.Statements.Add(new CodeExpressionStatement(methodInvoke)); 
                        }
                        else { 
                            method.Statements.Add(new CodeMethodReturnStatement(methodInvoke));
                        }
                    }
                    else { 
                        CodeObjectCreateExpression create = new CodeObjectCreateExpression(ee.eventCls);
                        for (int iparam = 0; iparam < ee.parameters.Length; ++iparam) { 
                            if (!ee.parameters[iparam].IsOut) 
                                create.Parameters.Add(new CodeFieldReferenceExpression(null, ee.parameters[iparam].Name));
                        } 

                        CodeVariableDeclarationStatement evtfield = new CodeVariableDeclarationStatement(ee.eventCls, ee.eventParam);
                        evtfield.InitExpression = create;
                        method.Statements.Add(evtfield); 

                        paramstr.Add(new CodeFieldReferenceExpression(null, ee.eventParam)); 
 
                        CodeExpression[] temp = new CodeExpression[paramstr.Count];
                        ((IList)paramstr).CopyTo(temp, 0); 
                        CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(parent, ee.invokeMethodName, temp);

                        if (eventsRef[i].ReturnType == typeof(void)) {
                            method.Statements.Add(new CodeExpressionStatement(methodInvoke)); 
                        }
                        else { 
                            CodeVariableDeclarationStatement tempVar = new CodeVariableDeclarationStatement(ee.retType, ee.invokeMethodName); 
                            method.Statements.Add(tempVar);
                            method.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(null, tempVar.Name), methodInvoke)); 
                        }

                        for (int j = 0; j < parameters.Length; ++j) {
                            if (parameters[j].IsByRef) { 
                                method.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(null, parameters[j].Name),
                                                                                  new CodeFieldReferenceExpression( 
                                                                                    new CodeFieldReferenceExpression(null, evtfield.Name), 
                                                                                    parameters[j].Name)));
                            } 
                        }

                        if (eventsRef[i].ReturnType != typeof(void)) {
                            method.Statements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(null, ee.invokeMethodName))); 
                        }
                    } 
                } 
                else {
                    CodeExpressionCollection paramstr = new CodeExpressionCollection(); 
                    for (int iparam = 0; iparam < parameters.Length; ++iparam)
                        paramstr.Add(new CodeFieldReferenceExpression(null, parameters[iparam].Name));

                    CodeExpression[] temp = new CodeExpression[paramstr.Count]; 
                    ((IList)paramstr).CopyTo(temp, 0);
                    CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(parent, "RaiseOn" + eventsRef[i].Name, temp); 
 
                    if (eventsRef[i].ReturnType == typeof(void)) {
                        method.Statements.Add(new CodeExpressionStatement(methodInvoke)); 
                    }
                    else {
                        method.Statements.Add(new CodeMethodReturnStatement(methodInvoke));
                    } 
                }
 
                cls.Members.Add(method); 
            }
 
            AddClassToNamespace(ns, cls);
            return evntCls;
        }
 
        private void WriteEvents(CodeNamespace ns, CodeTypeDeclaration cls) {
            for (int i = 0; events != null && i < events.Count; ++i) { 
                EventEntry evententry = (EventEntry)events[i]; 

                Debug.WriteLineIf(AxCodeGen.Enabled, "Processing event: " + evententry.eventName); 

                CodeMemberEvent e = new CodeMemberEvent();
                e.Name = evententry.resovledEventName;
                e.Attributes = evententry.eventFlags; 

                e.Type = new CodeTypeReference(evententry.eventHandlerCls); 
                cls.Members.Add(e); 

                //Generate the "RaiseXXX" method. 
                CodeMemberMethod cmm = new CodeMemberMethod();
                cmm.Name = evententry.invokeMethodName;
                cmm.ReturnType = new CodeTypeReference(evententry.retType);
                cmm.Attributes = MemberAttributes.Assembly | MemberAttributes.Final; 
                cmm.Parameters.Add(CreateParamDecl(MapTypeName(typeof(object)), "sender", false));
                cmm.Parameters.Add(CreateParamDecl(evententry.eventCls, "e", false)); 
 
                CodeFieldReferenceExpression eventExpr = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), evententry.resovledEventName);
                CodeBinaryOperatorExpression cond = new CodeBinaryOperatorExpression(eventExpr, 
                                                                                     CodeBinaryOperatorType.IdentityInequality,
                                                                                     new CodePrimitiveExpression(null));
                CodeConditionStatement ifstat = new CodeConditionStatement();
                ifstat.Condition = cond; 

                CodeExpressionCollection paramstr = new CodeExpressionCollection(); 
                paramstr.Add(new CodeFieldReferenceExpression(null, "sender")); 
                paramstr.Add(new CodeFieldReferenceExpression(null, "e"));
 
                CodeExpression[] temp = new CodeExpression[paramstr.Count];
                ((IList)paramstr).CopyTo(temp, 0);

                CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), evententry.resovledEventName, temp); 

                if (evententry.retType == typeof(void)) { 
                    ifstat.TrueStatements.Add(new CodeExpressionStatement(methodInvoke)); 
                }
                else { 
                    ifstat.TrueStatements.Add(new CodeMethodReturnStatement(methodInvoke));
                    ifstat.FalseStatements.Add(new CodeMethodReturnStatement(GetInitializer(evententry.retType)));
                }
 
                cmm.Statements.Add(ifstat);
 
                cls.Members.Add(cmm); 
            }
 
            WriteEventMulticaster(ns);
        }

        private void WriteHookupMethods(CodeTypeDeclaration cls) { 
            if (axctlEventsType != null) {
                // Generate the CreateSink() override. 
                // 
                CodeMemberMethod sinkMeth = new CodeMemberMethod();
                sinkMeth.Name = "CreateSink"; 
                sinkMeth.Attributes = MemberAttributes.Family | MemberAttributes.Override;

                CodeObjectCreateExpression newMultiCaster = new CodeObjectCreateExpression(axctl + "EventMulticaster");
                newMultiCaster.Parameters.Add(new CodeThisReferenceExpression()); 

                CodeAssignStatement assignMultiCaster = new CodeAssignStatement(multicasterRef, newMultiCaster); 
 
                CodeObjectCreateExpression coce = new CodeObjectCreateExpression(typeof(AxHost.ConnectionPointCookie).FullName);
                coce.Parameters.Add(memIfaceRef); 
                coce.Parameters.Add(multicasterRef);
                coce.Parameters.Add(new CodeTypeOfExpression(axctlEvents));

                CodeAssignStatement cas = new CodeAssignStatement(cookieRef, coce); 

                CodeTryCatchFinallyStatement ctcf = new CodeTryCatchFinallyStatement(); 
                ctcf.TryStatements.Add(assignMultiCaster); 
                ctcf.TryStatements.Add(cas);
                ctcf.CatchClauses.Add(new CodeCatchClause("", new CodeTypeReference(typeof(Exception)))); 

                // Add the CreateSink() method to the class.
                //
                sinkMeth.Statements.Add(ctcf); 
                cls.Members.Add(sinkMeth);
 
                // Generate the DetachSink() override. 
                //
                CodeMemberMethod detachMeth = new CodeMemberMethod(); 
                detachMeth.Name = "DetachSink";
                detachMeth.Attributes = MemberAttributes.Family | MemberAttributes.Override;

                CodeFieldReferenceExpression invokee = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), cookie); 
                CodeMethodInvokeExpression cmis = new CodeMethodInvokeExpression(invokee, "Disconnect");
 
                ctcf = new CodeTryCatchFinallyStatement(); 
                ctcf.TryStatements.Add(cmis);
                ctcf.CatchClauses.Add(new CodeCatchClause("", new CodeTypeReference(typeof(Exception)))); 
                detachMeth.Statements.Add(ctcf);
                cls.Members.Add(detachMeth);
            }
 
            // Generate the AttachInterfaces() override.
            CodeMemberMethod attachMeth = new CodeMemberMethod(); 
            attachMeth.Name = "AttachInterfaces"; 
            attachMeth.Attributes = MemberAttributes.Family | MemberAttributes.Override;
 
            CodeCastExpression cce = new CodeCastExpression(axctlType.FullName, new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "GetOcx"));
            CodeAssignStatement assign = new CodeAssignStatement(memIfaceRef, cce);

            CodeTryCatchFinallyStatement trycatch = new CodeTryCatchFinallyStatement(); 
            trycatch.TryStatements.Add(assign);
            trycatch.CatchClauses.Add(new CodeCatchClause("", new CodeTypeReference(typeof(Exception)))); 
 
            attachMeth.Statements.Add(trycatch);
            cls.Members.Add(attachMeth); 
        }

        private void WriteMembersDecl(CodeTypeDeclaration cls) {
            memIface = "ocx"; 
            memIfaceRef = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), memIface);
 
            cls.Members.Add(new CodeMemberField(MapTypeName(axctlType), memIface)); 

            if (axctlEventsType != null) { 
                multicaster = "eventMulticaster";
                multicasterRef = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), multicaster);
                cls.Members.Add(new CodeMemberField(axctl + "EventMulticaster", multicaster));
 
                cookie = "cookie";
                cookieRef = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), cookie); 
                cls.Members.Add(new CodeMemberField(typeof(AxHost.ConnectionPointCookie).FullName, cookie)); 
            }
        } 

        //VSQFE #830 change: This method now provides an option of stripping optional parameters.
        private void WriteMethod(CodeTypeDeclaration cls, MethodInfo method, bool hasPropInfo, bool removeOptionals) {
            Debug.WriteLineIf(AxCodeGen.Enabled, "Processing method: " + method.Name); 

 
            AxMethodGenerator generator = AxMethodGenerator.Create(method, removeOptionals); 
            generator.ControlType = axctlType;
 
            String methodName = method.Name;

            bool fOverride = false;
            bool fUseNew = false; 

            string methodPrefix = ResolveConflict(method.Name, method.ReturnType, out fOverride, out fUseNew); 
            if (fOverride) { 
                methodName = "Ctl" + methodName;
            } 

            // create the method body.
            //
            CodeMemberMethod cmm = generator.CreateMethod(methodName); 

 
            // Add the check for null this.ocx. 
            //
            cmm.Statements.Add(CreateInvalidStateException(cmm.Name, "MethodInvoke")); 


            // Marshal parameters in, convert if necessary, push onto parameter list
            // 
            List<CodeExpression> parameters = generator.GenerateAndMarshalParameters(cmm);
 
            // do method call 
            //
            CodeExpression returnExpression = generator.DoMethodInvoke(cmm, method.Name, memIfaceRef, parameters); 

            // marshal back parameters, do conversion.
            //
            generator.UnmarshalParameters(cmm, parameters); 

            // convert return value. 
            // 
            generator.GenerateReturn(cmm, returnExpression);
 

            cls.Members.Add(cmm);

            // Get the DISPID Attribute of the method to see if it handles the About Box. 
            // If it does, we will have to add a delegate to this in the WriteHookupMethods() code.
            // 
            object[] attrs = method.GetCustomAttributes(typeof(DispIdAttribute), false); 
            if (attrs != null && attrs.Length > 0) {
                DispIdAttribute dispid = (DispIdAttribute)attrs[0]; 
                if (dispid.Value == NativeMethods.ActiveX.DISPID_ABOUTBOX && method.GetParameters().Length == 0) {
                    aboutBoxMethod = cmm.Name;
                }
            } 
        }
 
 
        private void WriteMethods(CodeTypeDeclaration cls) {
            MethodInfo[] methods = axctlType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly); 
            for (int imeth = 0; imeth < methods.Length; ++imeth) {
                bool hasPropInfo;
                bool useLet;
                bool f = IsPropertySignature(methods[imeth], out hasPropInfo, out useLet); 

                Debug.WriteLineIf(AxCodeGen.Enabled, "Processing method: " + methods[imeth].Name + " IsProperty: " + f); 
 
                //VSQFE #830 change: Check if there are optional parameters. If so, generate an overload
                //                   with all the optionals removed. 
                if (!f) {
                    if (OptionalsPresent(methods[imeth])) {
                        WriteMethod(cls, methods[imeth], hasPropInfo, true);
                    } 

                    WriteMethod(cls, methods[imeth], hasPropInfo, false); 
                } 
            }
        } 

        private void WriteProperty(CodeTypeDeclaration cls, PropertyInfo pinfo, bool useLet) {
            CodeAttributeDeclarationCollection customAttrs;
            CodeAttributeDeclaration dispidAttr = null; 
            DispIdAttribute dispid = null;
 
            Debug.WriteLineIf(AxCodeGen.Enabled, "Processing property " + pinfo.Name); 

            if (nopersist == null) { 
                nopersist = new CodeAttributeDeclaration("System.ComponentModel.DesignerSerializationVisibility", new CodeAttributeArgument[] {
                                                                            new CodeAttributeArgument(new CodeFieldReferenceExpression(
                                                                            new CodeFieldReferenceExpression(null, "System.ComponentModel.DesignerSerializationVisibility"), "Hidden"))
                                                                            }); 
                nobrowse = new CodeAttributeDeclaration("System.ComponentModel.Browsable", new CodeAttributeArgument[] {new CodeAttributeArgument(new CodePrimitiveExpression(false))});
                browse   = new CodeAttributeDeclaration("System.ComponentModel.Browsable", new CodeAttributeArgument[] {new CodeAttributeArgument(new CodePrimitiveExpression(true))}); 
                bindable = new CodeAttributeDeclaration("System.ComponentModel.Bindable", new CodeAttributeArgument[] { 
                                                                            new CodeAttributeArgument(new CodeFieldReferenceExpression(
                                                                                new CodeFieldReferenceExpression(null, "System.ComponentModel.BindableSupport"), "Yes")) 
                                                                            });
                defaultBind = new CodeAttributeDeclaration("System.ComponentModel.Bindable", new CodeAttributeArgument[] {
                                                                            new CodeAttributeArgument(new CodeFieldReferenceExpression(
                                                                                new CodeFieldReferenceExpression(null, "System.ComponentModel.BindableSupport"), "Default")) 
                                                                            });
            } 
 
            object[] comaliasAttrs = pinfo.GetCustomAttributes(typeof(ComAliasNameAttribute), false);
            ComAliasEnum alias = ComAliasConverter.GetComAliasEnum(pinfo, pinfo.PropertyType, pinfo); 

            Type propType = pinfo.PropertyType;
            if (alias != ComAliasEnum.None) {
                propType = ComAliasConverter.GetWFTypeFromComType(propType, alias); 
            }
 
            // Is this a DataSource property? If so, add a member variable to 
            // cache the value of the property.
            // 
            bool dataSourceProp = (propType.GUID.Equals(Guid_DataSource));
            if (dataSourceProp) {
                CodeMemberField field = new CodeMemberField(propType.FullName, CreateDataSourceFieldName(pinfo.Name));
                field.Attributes = MemberAttributes.Private | MemberAttributes.Final; 
                cls.Members.Add(field);
                dataSourceProps.Add(pinfo); 
            } 

            // Get the DISPID Attribute of the property and store it in the newly generated wrapper property. 
            // We use this later to determine the property category to be used in the properties window.
            //
            object[] attrs = pinfo.GetCustomAttributes(typeof(DispIdAttribute), false);
            if (attrs != null && attrs.Length > 0) { 
                dispid = (DispIdAttribute)attrs[0];
                dispidAttr = new CodeAttributeDeclaration(typeof(DispIdAttribute).FullName, new CodeAttributeArgument[] {new CodeAttributeArgument(new CodePrimitiveExpression(dispid.Value))}); 
            } 
#if DEBUG
            else { 
                Debug.Fail("Property: " + pinfo.Name + " does not have any DISPID attribute, or has multiple DISPID attributes!!!" + ((attrs != null) ? attrs.Length : -1));
            }
#endif // DEBUG
 
            bool fOverride = false;
            bool fUseNew = false; 
 
            string propPrefix = ResolveConflict(pinfo.Name, propType, out fOverride, out fUseNew);
 
            if (fOverride || fUseNew) {
                if (dispid == null)
                    return;
                else { 
                    if (!IsDispidKnown(dispid.Value, pinfo.Name)) {
                        propPrefix = "Ctl"; 
                        fOverride = false; 
                        fUseNew = false;
                    } 
                }
            }

            CodeMemberProperty prop = new CodeMemberProperty(); 

            prop.Type = new CodeTypeReference(MapTypeName(propType)); 
            prop.Name = propPrefix + pinfo.Name; 
            prop.Attributes = MemberAttributes.Public;
 
            if (fOverride) {
                prop.Attributes |= MemberAttributes.Override;
            }
            else if (fUseNew) { 
                prop.Attributes |= MemberAttributes.New;
            } 
 
            bool isDefaultBind = false;
            bool browsable = IsPropertyBrowsable(pinfo, alias); 
            bool isbind    = IsPropertyBindable(pinfo, out isDefaultBind);

            // Generate custom attributes for these properties.
            // 
            if (!browsable || alias == ComAliasEnum.Handle) {
                // These properties are NonBrowsable, NonPersistable ones. 
                // 
                customAttrs = new CodeAttributeDeclarationCollection(new CodeAttributeDeclaration[] {nobrowse, nopersist, dispidAttr});
            } 
            else if (dataSourceProp) {
                // DataSource properties are persitable in code
                //
                customAttrs = new CodeAttributeDeclarationCollection(new CodeAttributeDeclaration[] {dispidAttr}); 
            }
            else { 
                // The rest of the properties are to be Persistable.None, as they get persisted to the ActiveX control's persist stream. 
                //
                if (fOverride || fUseNew) { 
                    customAttrs = new CodeAttributeDeclarationCollection(new CodeAttributeDeclaration[] {browse, nopersist, dispidAttr});
                }
                else {
                    customAttrs = new CodeAttributeDeclarationCollection(new CodeAttributeDeclaration[] {nopersist, dispidAttr}); 
                }
            } 
 
            if (alias != ComAliasEnum.None) {
                CodeAttributeDeclaration attr = new CodeAttributeDeclaration(typeof(ComAliasNameAttribute).FullName, new CodeAttributeArgument[] { 
                                                                                new CodeAttributeArgument(new CodePrimitiveExpression(pinfo.PropertyType.FullName))});
                customAttrs.Add(attr);
            }
 
            if (isDefaultBind)
                customAttrs.Add(defaultBind); 
            else if (isbind) 
                customAttrs.Add(bindable);
 
            prop.CustomAttributes = customAttrs;

            // Handle Indexed properties...
            AxParameterData[] parameters = AxParameterData.Convert(pinfo.GetIndexParameters()); 
            if (parameters != null && parameters.Length > 0) {
                for (int iparam = 0; iparam < parameters.Length; ++iparam) { 
                    CodeParameterDeclarationExpression param = CreateParamDecl(parameters[iparam].TypeName, parameters[iparam].Name, false); 
                    param.Direction = parameters[iparam].Direction;
                    prop.Parameters.Add(param); 
                }
            }

            // Visual Basic generates properties where the setter takes a BYREF parameter for the set_XXX(). 
            // This causes the C# code gen to not work correctly, since the compiler cannot convert
            // the parameter fro, type 'Foo' to type 'ref Foo'. The workaround is to recognize these 
            // properties and generate the property invokes on the OCX to be of the get_XXX() and 
            // set_XXX(ref value) instead of the regular property invoke syntax.
            // 
            bool fConvertPropCallsToMethodInvokes = useLet;
            if (pinfo.CanWrite) {
                MethodInfo setter;
                if (useLet) 
                    setter = pinfo.DeclaringType.GetMethod("let_" + pinfo.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                else 
                    setter = pinfo.GetSetMethod(); 

                Debug.Assert(setter != null, "No set/let method found for : " + pinfo.Name); 

                Type paramType = setter.GetParameters()[0].ParameterType;
                Type baseType = paramType.GetElementType();
                if (baseType != null && paramType != baseType) { 
                    Debug.WriteLineIf(AxCodeGen.Enabled, "Writing property in method invoke syntax " + pinfo.Name);
                    fConvertPropCallsToMethodInvokes = true; 
                } 
            }
 
            if (pinfo.CanRead)
                WritePropertyGetter(prop, pinfo, alias, parameters, fConvertPropCallsToMethodInvokes, fOverride, dataSourceProp);
            if (pinfo.CanWrite)
                WritePropertySetter(prop, pinfo, alias, parameters, fConvertPropCallsToMethodInvokes, fOverride, useLet, dataSourceProp); 

            // If the default property happens to be different from "Item", we have to 
            // generate the name("foo") attribute on the default property so we can 
            // rename it.
            // 
            if (parameters.Length > 0 && prop.Name != "Item") {
                CodeAttributeDeclaration nameAttr = new CodeAttributeDeclaration("System.Runtime.CompilerServices.IndexerName",
                                                             new CodeAttributeArgument[] {new CodeAttributeArgument(new CodePrimitiveExpression(prop.Name))});
 
                // Calling a property "Item" tells the codedom that this is the default indexed property.
                // 
                prop.Name = "Item"; 
                prop.CustomAttributes.Add(nameAttr);
            } 

            // Add DefaultProperty attribute for the class if needed...
            if (defMember != null && defMember.Equals(pinfo.Name)) {
                CodeAttributeDeclaration defMemberAttr = new CodeAttributeDeclaration("System.ComponentModel.DefaultProperty", 
                                                             new CodeAttributeArgument[] {new CodeAttributeArgument(new CodePrimitiveExpression(prop.Name))});
                cls.CustomAttributes.Add(defMemberAttr); 
            } 

            cls.Members.Add(prop); 
        }

        private void WritePropertyGetter(CodeMemberProperty prop, PropertyInfo pinfo, ComAliasEnum alias, AxParameterData[] parameters, bool fMethodSyntax, bool fOverride, bool dataSourceProp) {
            if (dataSourceProp) { 
                Debug.Assert(!fOverride, "Cannot have a overridden DataSource property.");
                Debug.Assert(parameters.Length <= 0, "Cannot have a parameterized DataSource property."); 
 
                string dataSourceName = CreateDataSourceFieldName(pinfo.Name);
                CodeMethodReturnStatement ret = new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), dataSourceName)); 
                prop.GetStatements.Add(ret);
            }
            else if (fOverride) {
                CodeConditionStatement ifstat = CreateValidStateCheck(); 
                ifstat.TrueStatements.Add(GetPropertyGetRValue(pinfo, memIfaceRef, alias, parameters, fMethodSyntax));
 
                ifstat.FalseStatements.Add(GetPropertyGetRValue(pinfo, new CodeBaseReferenceExpression(), ComAliasEnum.None, parameters, false)); 

                prop.GetStatements.Add(ifstat); 
            }
            else {
                prop.GetStatements.Add(CreateInvalidStateException(prop.Name, "PropertyGet"));
                prop.GetStatements.Add(GetPropertyGetRValue(pinfo, memIfaceRef, alias, parameters, fMethodSyntax)); 
            }
        } 
 
        private void WritePropertySetter(CodeMemberProperty prop, PropertyInfo pinfo, ComAliasEnum alias, AxParameterData[] parameters, bool fMethodSyntax, bool fOverride, bool useLet, bool dataSourceProp) {
            if (!fOverride && !dataSourceProp) { 
                prop.SetStatements.Add(CreateInvalidStateException(prop.Name, "PropertySet"));
            }

            if (dataSourceProp) { 
                Debug.Assert(!fOverride, "Cannot have a overridden DataSource property.");
                Debug.Assert(parameters.Length <= 0, "Cannot have a parameterized DataSource property."); 
 
                string dataSourceName = CreateDataSourceFieldName(pinfo.Name);
                WriteDataSourcePropertySetter(prop, pinfo, dataSourceName); 
            }
            else if (!fMethodSyntax) {
                WritePropertySetterProp(prop, pinfo, alias, parameters, fOverride, useLet);
            } 
            else {
                WritePropertySetterMethod(prop, pinfo, alias, parameters, fOverride, useLet); 
            } 
        }
 
        private void WriteDataSourcePropertySetter(CodeMemberProperty prop, PropertyInfo pinfo, string dataSourceName) {
            CodeExpression left = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), dataSourceName);
            CodeExpression right = new CodeFieldReferenceExpression(null, "value");
            CodeAssignStatement assign = new CodeAssignStatement(left, right); 

            prop.SetStatements.Add(assign); 
 
            CodeConditionStatement ifstat = CreateValidStateCheck();
            left = new CodeFieldReferenceExpression(memIfaceRef, pinfo.Name); 
            ifstat.TrueStatements.Add(new CodeAssignStatement(left, right));

            prop.SetStatements.Add(ifstat);
        } 

        private void WritePropertySetterMethod(CodeMemberProperty prop, PropertyInfo pinfo, ComAliasEnum alias, AxParameterData[] parameters, bool fOverride, bool useLet) { 
            CodeExpression baseCall = null; 
            CodeBinaryOperatorExpression cond = null;
            CodeConditionStatement ifstat = null; 

            if (fOverride) {
                if (parameters.Length > 0) {
                    baseCall = new CodeIndexerExpression(memIfaceRef); 
                }
                else { 
                    baseCall = new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), pinfo.Name); 
                }
                cond = new CodeBinaryOperatorExpression(memIfaceRef, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null)); 
                ifstat = new CodeConditionStatement();
                ifstat.Condition = cond;
            }
 
            CodeFieldReferenceExpression propCallParam;
            string setterName = (useLet) ? "let_" + pinfo.Name : pinfo.GetSetMethod().Name; 
            CodeMethodInvokeExpression propCall = new CodeMethodInvokeExpression(memIfaceRef, setterName); 

            for (int iparam = 0; iparam < parameters.Length; ++iparam) { 
                if (fOverride) {
                    ((CodeIndexerExpression)baseCall).Indices.Add(new CodeFieldReferenceExpression(null, parameters[iparam].Name));
                }
                propCall.Parameters.Add(new CodeFieldReferenceExpression(null, parameters[iparam].Name)); 
            }
 
            CodeFieldReferenceExpression valueExpr = new CodeFieldReferenceExpression(null, "value"); 
            CodeExpression rval = GetPropertySetRValue(alias, pinfo.PropertyType);
 
            if (alias != ComAliasEnum.None) {
                string paramConverter = ComAliasConverter.GetWFToComParamConverter(alias, pinfo.PropertyType);

                CodeParameterDeclarationExpression propCallParamDecl; 
                if (paramConverter.Length == 0) {
                    propCallParamDecl = CreateParamDecl(MapTypeName(pinfo.PropertyType), "paramTemp", false); 
                } 
                else {
                    propCallParamDecl = CreateParamDecl(paramConverter, "paramTemp", false); 
                }
                prop.SetStatements.Add(new CodeAssignStatement(propCallParamDecl, rval));

                propCallParam = new CodeFieldReferenceExpression(null, "paramTemp"); 
            }
            else { 
                propCallParam = valueExpr; 
            }
 
            propCall.Parameters.Add(new CodeDirectionExpression((useLet) ? FieldDirection.In : FieldDirection.Ref, propCallParam));

            if (fOverride) {
                prop.SetStatements.Add(new CodeAssignStatement(baseCall, valueExpr)); 
                ifstat.TrueStatements.Add(new CodeExpressionStatement(propCall));
                prop.SetStatements.Add(ifstat); 
            } 
            else {
                prop.SetStatements.Add(new CodeExpressionStatement(propCall)); 
            }
        }

        private void WritePropertySetterProp(CodeMemberProperty prop, PropertyInfo pinfo, ComAliasEnum alias, AxParameterData[] parameters, bool fOverride, bool useLet) { 
            CodeExpression baseCall = null;
            CodeBinaryOperatorExpression cond = null; 
            CodeConditionStatement ifstat = null; 

            if (fOverride) { 
                if (parameters.Length > 0) {
                    baseCall = new CodeIndexerExpression(memIfaceRef);
                }
                else { 
                    baseCall = new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), pinfo.Name);
                } 
 
                cond = new CodeBinaryOperatorExpression(memIfaceRef, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null));
                ifstat = new CodeConditionStatement(); 
                ifstat.Condition = cond;
            }

            CodeExpression propCall; 

            if (parameters.Length > 0) { 
                propCall = new CodeIndexerExpression(memIfaceRef); 
            }
            else { 
                propCall = new CodePropertyReferenceExpression(memIfaceRef, pinfo.Name);
            }

            for (int iparam = 0; iparam < parameters.Length; ++iparam) { 
                if (fOverride) {
                    ((CodeIndexerExpression)baseCall).Indices.Add(new CodeFieldReferenceExpression(null, parameters[iparam].Name)); 
                } 
                ((CodeIndexerExpression)propCall).Indices.Add(new CodeFieldReferenceExpression(null, parameters[iparam].Name));
            } 

            CodeFieldReferenceExpression valueExpr = new CodeFieldReferenceExpression(null, "value");
            CodeExpression rval = GetPropertySetRValue(alias, pinfo.PropertyType);
 
            if (fOverride) {
                prop.SetStatements.Add(new CodeAssignStatement(baseCall, valueExpr)); 
                ifstat.TrueStatements.Add(new CodeAssignStatement(propCall, rval)); 
                prop.SetStatements.Add(ifstat);
            } 
            else {
                prop.SetStatements.Add(new CodeAssignStatement(propCall, rval));
            }
        } 

        private CodeMethodReturnStatement GetPropertyGetRValue(PropertyInfo pinfo, CodeExpression reference, ComAliasEnum alias, AxParameterData[] parameters, bool fMethodSyntax) { 
            CodeExpression propCall = null; 

            if (fMethodSyntax) { 
                propCall = new CodeMethodInvokeExpression(reference, pinfo.GetGetMethod().Name);
                for (int iparam = 0; iparam < parameters.Length; ++iparam)
                    ((CodeMethodInvokeExpression)propCall).Parameters.Add(new CodeFieldReferenceExpression(null, parameters[iparam].Name));
            } 
            else {
                if (parameters.Length > 0) { 
                    propCall = new CodeIndexerExpression(reference); 

                    for (int iparam = 0; iparam < parameters.Length; ++iparam) 
                        ((CodeIndexerExpression)propCall).Indices.Add(new CodeFieldReferenceExpression(null, parameters[iparam].Name));
                }
                else {
                    propCall = new CodePropertyReferenceExpression(reference, ((parameters.Length == 0) ? pinfo.Name : "")); 
                }
            } 
 
            if (alias != ComAliasEnum.None) {
                string converter = ComAliasConverter.GetComToManagedConverter(alias); 
                string paramConverter = ComAliasConverter.GetComToWFParamConverter(alias);

                CodeExpression[] expr = null;
                if (paramConverter.Length == 0) { 
                    expr = new CodeExpression[] {propCall};
                } 
                else { 
                    CodeCastExpression paramCast = new CodeCastExpression(paramConverter, propCall);
                    expr = new CodeExpression[] {paramCast}; 
                }
                CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(null, converter, expr);
                return new CodeMethodReturnStatement(methodInvoke);
            } 
            else {
                return new CodeMethodReturnStatement(propCall); 
            } 
        }
 
        private CodeExpression GetPropertySetRValue(ComAliasEnum alias, Type propertyType) {
            CodeExpression valueExpr = new CodePropertySetValueReferenceExpression();

            if (alias != ComAliasEnum.None) { 
                string converter = ComAliasConverter.GetWFToComConverter(alias);
                string paramConverter = ComAliasConverter.GetWFToComParamConverter(alias, propertyType); 
 
                CodeExpression[] expr = new CodeExpression[] {valueExpr};
                CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(null, converter, expr); 

                if (paramConverter.Length == 0) {
                    return methodInvoke;
                } 
                else {
                    return new CodeCastExpression(paramConverter, methodInvoke); 
                } 
            }
            else { 
                return valueExpr;
            }
        }
 
        private void WriteProperties(CodeTypeDeclaration cls) {
            bool useLet; 
 
            PropertyInfo[] props = axctlType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
 
            for (int iprop = 0; iprop < props.Length; ++iprop) {
                if (IsPropertySignature(props[iprop], out useLet)) {
                    WriteProperty(cls, props[iprop], useLet);
                } 
            }
        } 
 
        private enum ComAliasEnum {
            None, 
            Color,
            Font,
            FontDisp,
            Handle, 
            Picture,
            PictureDisp 
        } 

        private static class ComAliasConverter { 
            private static Guid Guid_IPicture     = new Guid("{7BF80980-BF32-101A-8BBB-00AA00300CAB}");
            private static Guid Guid_IPictureDisp = new Guid("{7BF80981-BF32-101A-8BBB-00AA00300CAB}");
            private static Guid Guid_IFont        = new Guid("{BEF6E002-A874-101A-8BBA-00AA00300CAB}");
            private static Guid Guid_IFontDisp    = new Guid("{BEF6E003-A874-101A-8BBA-00AA00300CAB}"); 

            // Optimization caches. 
            // 
            private static Hashtable typeGuids;
 
            public static string GetComToManagedConverter(ComAliasEnum alias) {
                Debug.Assert(alias != ComAliasEnum.None, "Cannot find converter for ComAliasEnum.None");
                if (alias == ComAliasEnum.Color)
                    return "GetColorFromOleColor"; 

                if (IsFont(alias)) 
                    return "GetFontFromIFont"; 

                if (IsPicture(alias)) 
                    return "GetPictureFromIPicture";

                return "";
            } 

            public static string GetComToWFParamConverter(ComAliasEnum alias) { 
                Debug.Assert(alias != ComAliasEnum.None, "Cannot find converter for ComAliasEnum.None"); 
                if (alias == ComAliasEnum.Color)
                    return typeof(uint).FullName; 

                return "";
            }
 
            private static Guid GetGuid(Type t) {
                Guid g = Guid.Empty; 
 
                if (typeGuids == null) {
                    typeGuids = new Hashtable(); 
                }
                else if (typeGuids.Contains(t)) {
                    g = (Guid)typeGuids[t];
                    return g; 
                }
 
                g = t.GUID; 
                typeGuids.Add(t, g);
                return g; 
            }
            public static Type GetWFTypeFromComType(Type t, ComAliasEnum alias) {
                Debug.Assert(alias != ComAliasEnum.None, "Cannot find converter for ComAliasEnum.None");
 
                if (!IsValidType(alias, t))
                    return t; 
 
                if (alias == ComAliasEnum.Color)
                    return typeof(System.Drawing.Color); 

                if (IsFont(alias))
                    return typeof(System.Drawing.Font);
 
                if (IsPicture(alias))
                    return typeof(System.Drawing.Image); 
 
                return t;
            } 

            public static string GetWFToComConverter(ComAliasEnum alias) {
                Debug.Assert(alias != ComAliasEnum.None, "Cannot find converter for ComAliasEnum.None");
                if (alias == ComAliasEnum.Color) 
                    return "GetOleColorFromColor";
 
                if (IsFont(alias)) 
                    return "GetIFontFromFont";
 
                if (IsPicture(alias))
                    return "GetIPictureFromPicture";

                return ""; 
            }
 
            public static string GetWFToComParamConverter(ComAliasEnum alias, Type t) { 
                Debug.Assert(alias != ComAliasEnum.None, "Cannot find converter for ComAliasEnum.None");
                return t.FullName; 
            }

            public static ComAliasEnum GetComAliasEnum(MemberInfo memberInfo, Type type, ICustomAttributeProvider attrProvider) {
                string aliasName = null; 
                int dispid = -1;
 
                Debug.Assert(type != null, "No type for ComAliasEnum!!!"); 

                object[] attrs = new object[0]; 

                if (attrProvider != null) {
                    attrs = attrProvider.GetCustomAttributes(typeof(ComAliasNameAttribute), false);
                } 

                if (attrs != null && attrs.Length > 0) { 
                    Debug.Assert(attrs.Length == 1, "Multiple ComAliasNameAttributes found o: " + memberInfo.Name); 
                    ComAliasNameAttribute alias = (ComAliasNameAttribute)attrs[0];
                    aliasName = alias.Value; 
                }

                if (aliasName != null && aliasName.Length != 0) {
                    if (aliasName.EndsWith(".OLE_COLOR") && IsValidType(ComAliasEnum.Color, type)) 
                        return ComAliasEnum.Color;
 
                    if (aliasName.EndsWith(".OLE_HANDLE") && IsValidType(ComAliasEnum.Handle, type)) 
                        return ComAliasEnum.Handle;
 
#if DEBUG
                    //if (!aliasName.Equals("stdole.OLE_HANDLE"))
                    //    Debug.Fail("Did not handle ComAliasNameAttribute of: " + aliasName + " on property: " + memberInfo.Name);
#endif //DEBUG 
                }
 
                if (memberInfo is PropertyInfo && String.Equals(memberInfo.Name, "hWnd",  StringComparison.OrdinalIgnoreCase) && IsValidType(ComAliasEnum.Handle, type)) { 
                    Debug.WriteLineIf(AxCodeGen.Enabled && (aliasName == null || aliasName.EndsWith(".OLE_HANDLE")), "hWnd property is not marked as OLE_HANDLE");
                    return ComAliasEnum.Handle; 
                }

                // Get the dispid so we can use standard dispid values to compare.
                // 
                if (attrProvider != null) {
                    attrs = attrProvider.GetCustomAttributes(typeof(DispIdAttribute), false); 
                    if (attrs != null && attrs.Length > 0) { 
                        Debug.Assert(attrs.Length == 1, "Multiple ComAliasNameAttributes found o: " + memberInfo.Name);
                        DispIdAttribute alias = (DispIdAttribute)attrs[0]; 
                        dispid = alias.Value;
                    }
                }
 
                if ((dispid == NativeMethods.ActiveX.DISPID_BACKCOLOR || dispid == NativeMethods.ActiveX.DISPID_FORECOLOR ||
                    dispid == NativeMethods.ActiveX.DISPID_FILLCOLOR ||  dispid == NativeMethods.ActiveX.DISPID_BORDERCOLOR) && 
                    IsValidType(ComAliasEnum.Color, type)) { 
                        return ComAliasEnum.Color;
                } 

                if (dispid == NativeMethods.ActiveX.DISPID_FONT && IsValidType(ComAliasEnum.Font, type)) {
                        return ComAliasEnum.Font;
                } 

                if (dispid == NativeMethods.ActiveX.DISPID_PICTURE && IsValidType(ComAliasEnum.Picture, type)) { 
                        return ComAliasEnum.Picture; 
                }
 
                if (dispid == NativeMethods.ActiveX.DISPID_HWND && IsValidType(ComAliasEnum.Handle, type)) {
                        return ComAliasEnum.Handle;
                }
 
                if (IsValidType(ComAliasEnum.Font, type))
                    return ComAliasEnum.Font; 
 
                if (IsValidType(ComAliasEnum.FontDisp, type))
                    return ComAliasEnum.FontDisp; 

                if (IsValidType(ComAliasEnum.Picture, type))
                    return ComAliasEnum.Picture;
 
                if (IsValidType(ComAliasEnum.PictureDisp, type))
                    return ComAliasEnum.PictureDisp; 
 
                return ComAliasEnum.None;
            } 

            public static bool IsFont(ComAliasEnum e) {
                return e == ComAliasEnum.Font || e == ComAliasEnum.FontDisp;
            } 

            public static bool IsPicture(ComAliasEnum e) { 
                return e == ComAliasEnum.Picture || e == ComAliasEnum.PictureDisp; 
            }
 
            private static bool IsValidType(ComAliasEnum e, Type t) {
                switch (e) {
                    case ComAliasEnum.Color:
                        return t == typeof(UInt16) || t == typeof(uint) || t == typeof(int) || t == typeof(short); 

                    case ComAliasEnum.Handle: 
                        return t == typeof(uint) || t == typeof(int) || t == typeof(IntPtr) || t == typeof(UIntPtr); 

                    case ComAliasEnum.Font: 
                        return GetGuid(t).Equals(Guid_IFont);

                    case ComAliasEnum.FontDisp:
                        return GetGuid(t).Equals(Guid_IFontDisp); 

                    case ComAliasEnum.Picture: 
                        return GetGuid(t).Equals(Guid_IPicture); 

                    case ComAliasEnum.PictureDisp: 
                        return GetGuid(t).Equals(Guid_IPictureDisp);

                    default:
                        Debug.Fail("Invalid verify call for " + e.ToString()); 
                        return false;
                } 
            } 
        }
 
        private class EventEntry {
            public string eventName;
            public string resovledEventName;
            public string eventCls; 
            public string eventHandlerCls;
            public Type   retType; 
            public AxParameterData[] parameters; 
            public string eventParam;
            public string invokeMethodName; 
            public MemberAttributes eventFlags;

            public EventEntry(string eventName, string eventCls, string eventHandlerCls, Type retType, AxParameterData[] parameters, bool conflict) {
                this.eventName = eventName; 
                this.eventCls = eventCls;
                this.eventHandlerCls = eventHandlerCls; 
                this.retType = retType; 
                this.parameters = parameters;
                this.eventParam = eventName.ToLower(CultureInfo.InvariantCulture) + "Event"; 
                this.resovledEventName = (conflict) ? eventName + "Event" : eventName;
                this.invokeMethodName = "RaiseOn" + resovledEventName;
                this.eventFlags = MemberAttributes.Public | MemberAttributes.Final;
            } 
        }
 
 
        /// A helper class we use to generate method bodies.  We need this
        /// because we either generate a "normal" call or a call through reflection using MethodINfo.Invoke. 
        /// By factoring out the calls like this, we can override this call and change the output for the
        /// Inovke case.  See the AxReflectionInvokeMethodGenerator class.
        ///
        /// Here's a complex example: 
        ///
        /// public virtual System.Drawing.Image MethodOpt4(bool b, short i, ref System.Drawing.Font f, ref System.Drawing.Image p, ref short x) { 
        ///    if ((this.ocx == null)) { 
        ///        throw new System.Windows.Forms.AxHost.InvalidActiveXStateException("MethodOpt4", System.Windows.Forms.AxHost.ActiveXInvokeKind.MethodInvoke);
        ///    } 
        ///    stdole.StdFont _f = ((stdole.StdFont)(GetIFontFromFont(f)));
        ///    stdole.StdPicture _p = ((stdole.StdPicture)(GetIPictureFromPicture(p)));
        ///    stdole.StdPicture returnValue = ((stdole.StdPicture)(this.ocx.MethodOpt4(b, i, ref _f, ref _p, ref x)));
        ///    f = GetFontFromIFont(((stdole.StdFont)(_f))); 
        ///    p = GetPictureFromIPicture(((stdole.StdPicture)(_p)));
        ///    return GetPictureFromIPicture(returnValue); 
        /// } 
        ///
        private class AxMethodGenerator { 

            private MethodInfo _method;
            private bool       _removeOptionals;
            private AxParameterData[] _params; 
            private Type        _controlType;
 
            protected static object OriginalParamNameKey = new object(); 
            protected static string ReturnValueVariableName = "returnValue";
 
            internal AxMethodGenerator(MethodInfo method, bool removeOpts) {
                _method = method;
                _removeOptionals = removeOpts;
            } 

            /// The type of control we're generating code for.  This is needed 
            /// if we're donig reflection invoke (see the derived class) 
            ///
            public Type ControlType { 

                get {return _controlType;}
                set {_controlType = value;}
            } 

            private AxParameterData[] Parameters { 
 
                get {
                    if (_params == null && _method != null) { 
                        _params = AxParameterData.Convert(_method.GetParameters());

                        if (_params == null) {
                            _params = new AxParameterData[0]; 
                        }
                    } 
                    return _params; 
                }
            } 

            /// Static factory method that gives us the right kind of object so we can use
            /// that fancy polymorphism.
            /// 
            public static AxMethodGenerator Create(MethodInfo method, bool removeOptionals) {
 
                bool useReflectionInvoke = removeOptionals && NonPrimitiveOptionalsOrMissingPresent(method); 
                if (useReflectionInvoke) {
                    return new AxReflectionInvokeMethodGenerator(method, removeOptionals); 
                }
                else {
                    return new AxMethodGenerator(method, removeOptionals);
                } 
            }
 
            /// Create the method body... 
            ///
            public CodeMemberMethod CreateMethod(string methodName) { 


                CodeMemberMethod cmm = new CodeMemberMethod();
 
                cmm.Name = methodName;
                cmm.Attributes = MemberAttributes.Public; 
                cmm.ReturnType = new CodeTypeReference(MapTypeName(_method.ReturnType)); 

                return cmm; 

            }

            /// Build the list of parameters and "marshal" them in.  This means for any that are types 
            /// that we need to convert to managed types, we'll do that work.
            /// 
            /// Here's a code example: 
            ///
            /// public virtual System.Drawing.Font MethodOpt3(ref System.Drawing.Font p, ref short x) { 
            ///    stdole.StdFont _p = ((stdole.StdFont)(GetIFontFromFont(p)));
            ///
            ///
            /// The output from this is the list of parameters to send to the calling function. 
            /// Notice this list contains both references to parameters (normal or like the ones above), but also
            /// the default values of any that are optional. 
            /// 
            public List<CodeExpression> GenerateAndMarshalParameters(CodeMemberMethod method){
 
                List<CodeExpression> paramExpressions = new List<CodeExpression>();

                foreach (AxParameterData param in Parameters) {
                    if (param.IsOptional && _removeOptionals) { 
                        // generate the default value here -- notice we just add this value to the list of
                        // parameters and pass it along. 
                        // 
                        CodeExpression defaultExpression = GetDefaultExpressionForInvoke(_method, param);
                        paramExpressions.Add(defaultExpression); 
                        continue;
                    }
                    // set up our variables.
                    // 
                    Type parameterType = param.ParameterBaseType;
                    ComAliasEnum alias = ComAliasConverter.GetComAliasEnum(_method, parameterType , null); 
                    CodeVariableReferenceExpression paramRef = new CodeVariableReferenceExpression(param.Name); 
                    paramRef.UserData[typeof(AxParameterData)] = param;
 
                    if (alias != ComAliasEnum.None) {


                            // create the parameter itself. 
                            //
                            Type destType = ComAliasConverter.GetWFTypeFromComType(parameterType, alias); 
                            CodeParameterDeclarationExpression paramDecl =  new CodeParameterDeclarationExpression(destType.FullName, param.Name); 
                            paramDecl.Direction = param.Direction;
                            method.Parameters.Add(paramDecl); 

                            // generate the conversion code
                            //
                            string converter = ComAliasConverter.GetWFToComConverter(alias); 
                            CodeMethodInvokeExpression conversionExpression = new CodeMethodInvokeExpression(null, converter);
 
                            // note we create a new one here because the paramRef name will get updated.  We want the original name. 
                            //
                            conversionExpression.Parameters.Add(new CodeVariableReferenceExpression(param.Name)); 

                            // update the name in the param ref -- we'll just prepend an underscore for the new parameter name.
                            //
                            paramRef.UserData[OriginalParamNameKey] = param.Name; 
                            paramRef.VariableName = "_" + param.Name;
 
                            // add the conversion statement. 
                            //
                            CodeVariableDeclarationStatement paramConversion = 
                                    new CodeVariableDeclarationStatement(parameterType.FullName,
                                                                         paramRef.VariableName,
                                                                         new CodeCastExpression(parameterType, conversionExpression));
 
                            method.Statements.Add(paramConversion);
                    } 
                    else { 
                            CodeParameterDeclarationExpression paramExpr = new CodeParameterDeclarationExpression(param.TypeName, param.Name);
                            paramExpr.Direction = param.Direction; 
                            method.Parameters.Add(paramExpr);
                    }

                    paramExpressions.Add(paramRef); 
                }
 
                return paramExpressions; 
            }
 
            public CodeExpression DoMethodInvoke(CodeMemberMethod method, string methodName, CodeExpression targetObject, List<CodeExpression> parameters) {
                return DoMethodInvokeCore(method, methodName, _method.ReturnType, targetObject, parameters);
            }
 
            /// Do the main invoke call.  In this case, we'll just invoke the method we're passed with the parameters
            /// that we're passed.  This method is virtual so reflection invoke things can do their own processing here. 
            /// Code: 
            ///
            /// this.ocx.MethodOpt2(ref _p, x); 
            ///
            /// or (if optionals are present and we have a default value)
            ///
            /// this.ocx.MethodOpt1(((short)(2))); 
            ///
            public virtual CodeExpression DoMethodInvokeCore(CodeMemberMethod method, string methodName, Type returnType, CodeExpression targetObject, List<CodeExpression> parameters) { 
                CodeMethodInvokeExpression mie = new CodeMethodInvokeExpression(targetObject, methodName); 

                // walk the parameters and just add them to the call 
                //
                foreach (CodeExpression paramExpr in parameters) {
                   AxParameterData paramData = (AxParameterData)paramExpr.UserData[typeof(AxParameterData)];
 
                   CodeExpression expr = paramExpr;
 
                   if (paramData != null) { 
                        expr = new CodeDirectionExpression(paramData.Direction, paramExpr);
                   } 
                   mie.Parameters.Add(expr);
                }

                // if there isn't a return type, we're done! 
                //
                if (returnType == typeof(void)) { 
                    method.Statements.Add(new CodeExpressionStatement(mie)); 
                    return null;
                } 

                // assign the method call to the return value and return the expression to get to it.
                //
                CodeVariableDeclarationStatement returnStatement = new CodeVariableDeclarationStatement(returnType, ReturnValueVariableName, new CodeCastExpression(returnType, mie)); 
                method.Statements.Add(returnStatement);
 
                return new CodeVariableReferenceExpression(ReturnValueVariableName); 
            }
 
            /// Take the parameter list and convert them back (if necessary) to the input parameter types.
            /// For example, if a parameter comes in as IPictureDisp, we convert it to a Bitmap.  If it's a ref
            /// parameter, we need to convert it back to an IPictureDisp and copy the value back out.
            /// The idea is that we use a copy-in-copy-out pattern for ref and out parameters. 
            ///
            /// Code: 
            /// 
            /// p = GetPictureFromIPicture(((stdole.StdPicture)(_p)));
            /// 
            ///
            public void UnmarshalParameters(CodeMemberMethod method, List<CodeExpression> parameters) {

                 foreach (CodeExpression paramExpr in parameters) { 

                        // if it's not a variable-based parameter, we're done. 
                        // 
                        if (!(paramExpr is CodeVariableReferenceExpression)) {
 
                            continue;
                        }

                        AxParameterData paramData = (AxParameterData)paramExpr.UserData[typeof(AxParameterData)]; 
                        Debug.Assert(paramData != null, "why don't we have parameter data here?");
 
                        // check the direction to see if this guy needs to be marshaled out. 
                        //
 
                        string originalParameterName = (string)paramExpr.UserData[OriginalParamNameKey];

                        if (paramData.Direction == FieldDirection.In || originalParameterName == null) {
                            continue; 
                        }
 
                        CodeExpression left = new CodeVariableReferenceExpression(originalParameterName); 
                        CodeExpression right = new CodeCastExpression(paramData.ParameterBaseType, paramExpr);
 
                        // check the parameter type to see if it needs to be converted, and then convert it.
                        //
                        ComAliasEnum alias = ComAliasConverter.GetComAliasEnum(_method, paramData.ParameterBaseType, null);
 
                        if (alias != ComAliasEnum.None) {
 
                            string converter = ComAliasConverter.GetComToManagedConverter(alias); 
                            CodeMethodInvokeExpression refConvertExpression = new CodeMethodInvokeExpression(null, converter);
                            refConvertExpression.Parameters.Add(right); 
                            right = refConvertExpression;
                        }

                        // generate the assignment back to the variable. 
                        //
                        CodeAssignStatement cas = new CodeAssignStatement(left, right); 
                        method.Statements.Add(cas); 
                 }
 
            }

            /// Generate the property return statement given the expression.
            /// This will ensure the value is properly converted. 
            ///
            /// code: 
            /// 
            ///    return GetPictureFromIPicture(returnValue);
            /// 
            /// or
            ///
            ///   return returnValue;
            /// 
            public void GenerateReturn(CodeMemberMethod method, CodeExpression returnExpression) {
 
                if (returnExpression == null) { 
                    return;
                } 

                // convert the return type if needed.
                //
                ComAliasEnum alias = ComAliasConverter.GetComAliasEnum(_method, _method.ReturnType, _method.ReturnTypeCustomAttributes); 

                if (alias != ComAliasEnum.None) { 
 
                    string converter = ComAliasConverter.GetComToManagedConverter(alias);
 
                    CodeMethodInvokeExpression mie = new CodeMethodInvokeExpression(null, converter);
                    mie.Parameters.Add(returnExpression);
                    returnExpression = mie;
 
                    method.ReturnType = new CodeTypeReference(ComAliasConverter.GetWFTypeFromComType(_method.ReturnType, alias));
                } 
 
                // add the return statement to the method.
                // 
                method.Statements.Add(new CodeMethodReturnStatement(returnExpression));
            }

            //VSQFE #830 addition 
            private static bool NonPrimitiveOptionalsOrMissingPresent(MethodInfo method) {
                ParameterInfo[] parameters = method.GetParameters(); 
 
                if (parameters != null && parameters.Length > 0) {
                    for (int iparam = 0; iparam < parameters.Length; ++iparam) { 
                        if (parameters[iparam].IsOptional && ((!parameters[iparam].ParameterType.IsPrimitive && !parameters[iparam].ParameterType.IsEnum) || parameters[iparam].DefaultValue == System.Reflection.Missing.Value)) {
                            return true;
                        }
                    } 
                }
 
                return false; 
            }
 
             // CodeDom only handles CLS which doesn't include the unsigned types, so just convert them to their
            // signed equivelent here and let the cast do the right thing.
            //
            private static object GetClsPrimitiveValue(object value) { 
                if (value is UInt32) {
                    return Convert.ChangeType(value, typeof(Int32), CultureInfo.InvariantCulture); 
                } 
                else if (value is UInt16) {
                    return Convert.ChangeType(value, typeof(Int16), CultureInfo.InvariantCulture); 
                }
                else if (value is UInt64) {
                    return Convert.ChangeType(value, typeof(Int64), CultureInfo.InvariantCulture);
                } 
                else if (value is SByte) {
                    return Convert.ChangeType(value, typeof(byte), CultureInfo.InvariantCulture); 
                } 
                return value;
            } 

            [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
            private static object GetDefaultValueForUnsignedType(Type parameterType, object value) {
                if (parameterType == typeof(UInt32)) { 
                    //UNBOX
                    Int32 newVar32 = 0; 
                    if (value is System.Int16) 
                    {
                        newVar32 = (System.Int16)value; 
                    }
                    if (value is System.Int32)
                    {
                        newVar32 = (System.Int32)value; 
                    }
                    if (value is System.Int64) 
                    { 
                        newVar32 = (System.Int32)value;
                    } 
                    return Convert.ToUInt32(Convert.ToString(newVar32, 16), 16);
                }
                else if (parameterType == typeof(UInt16)) {
                    //UNBOX 
                    Int16 newVar16 = (System.Int16)value;
                    return Convert.ToUInt16(Convert.ToString(newVar16, 16), 16); 
                } 
                else if (parameterType == typeof(UInt64)) {
                    //UNBOX 
                    Int64 newVar64 = 0;

                    if (value is System.Int16)
                    { 
                        newVar64 = (System.Int16)value;
                    } 
                    if (value is System.Int32) 
                    {
                        newVar64 = (System.Int32)value; 
                    }
                    if (value is System.Int64)
                    {
                        newVar64 = (System.Int64)value; 
                    }
                    return Convert.ToUInt64(Convert.ToString(newVar64, 16), 16); 
                } 
                return value;
            } 


            // Gets the "default" value for a given primative type.
            // Basically just converts Zero to the appropriate type. 
            //
            private static object GetPrimitiveDefaultValue(Type destType) { 
                if (destType == typeof(IntPtr) || destType == typeof(UIntPtr)) { 
                    // return actual zero, let the cast take care of it.
                    // 
                    return 0;
                }
                else {
                    return GetClsPrimitiveValue(Convert.ChangeType(0, destType, CultureInfo.InvariantCulture)); 
                }
            } 
 
            // Given parameter, come up with correct default expression for it.
            // Below is the table of cases that we need to handle 
            //
            //              Primitive   ValueType   Enum    ReferenceType Object
            //  Missing         (1)         (2*)     (3)        (4)         (9)
            //  Present         (5)         (6*)     (7)        (8)         (10) 
            //
            //  2*, 6* -- we don't have a good way of serializing arbitrary things 
            //        so we special case what we expect to get.  There may be cases we're missing here. 
            //
            // 
            [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
            private static CodeExpression GetDefaultExpressionForInvoke(MethodInfo method, AxParameterData parameterInfo) {
                object defaultValue = parameterInfo.ParameterInfo.DefaultValue;
                Type parameterType = parameterInfo.ParameterBaseType; 

                if (defaultValue == System.Reflection.Missing.Value) { 
                    // we need to come up with a reasonable default. 
                    // 1. For primitives, it's zero
                    // 2. For Enums it's zero if there is one, or it's the first value 
                    //    in the enum.
                    // 3. For others it's null.
                    //
                    if (parameterType.IsPrimitive) { 
                        defaultValue = GetPrimitiveDefaultValue(parameterType); // case (1)
                    } 
                    else if (parameterType.IsEnum) { 
                        // case (3)
                        defaultValue = 0; 
                        if (!Enum.IsDefined(parameterType, 0) && Enum.GetValues(parameterType).Length > 0) {
                            defaultValue = Enum.GetValues(parameterType).GetValue(0);
                        }
                    } 
                    else if (parameterType == typeof(object)) {
                        // case 9 
                        // if we have object, we've actually got a VARIANT here which 
                        // means we do pass System.Reflection.Missing.Value as the value.
                        // 
                        return new CodeFieldReferenceExpression(new CodeFieldReferenceExpression(null, "System.Reflection.Missing"), "Value");
                    }
                    else if (!parameterType.IsValueType) {
                        // case (4) 
                        if (parameterType == typeof(string)) {
                            defaultValue = ""; 
                        } 
                        else {
                            defaultValue = null; 
                        }
                        parameterType = null;
                    }
                    else if (parameterType.GetConstructor(new Type[0]) != null) { 
                        // case (2a)
                        // if there is a default public ctor, use that. 
                        // 
                        return new CodeObjectCreateExpression(parameterType);
                    } 
                    else if (parameterType == typeof(Decimal)) {
                        // case 2b
                        return new CodeObjectCreateExpression(typeof(Decimal), new CodeExpression[]{new CodePrimitiveExpression(0.0d)});
                    } 
                    else if (parameterType == typeof(DateTime)) {
                        // case 2c 
                        return new CodeObjectCreateExpression(typeof(DateTime), new CodeExpression[]{new CodePrimitiveExpression(0L)}); 
                    }
                    else { 

                        // case 2d

                        throw new Exception(SR.GetString(SR.AxImpNoDefaultValue, method.Name, parameterInfo.Name, parameterType.FullName)); 

                    } 
                } 
                else if (parameterType.IsPrimitive) {
                    // case 5 
                    defaultValue = GetClsPrimitiveValue(defaultValue);
                    defaultValue = GetDefaultValueForUnsignedType(parameterType, defaultValue);
                }
                else if (defaultValue != null && parameterType.IsInstanceOfType(defaultValue) && (defaultValue is DateTime || defaultValue is Decimal || defaultValue is bool)) { 
                    // case 6, 8
 
                    // here's where we put our nice little special case hacks since we don't have a good way to serialize things here and then 
                    // deserialize them at runtime.  We can only get a couple of types here so we just handle them specifically.
                    // 
                    if (defaultValue is DateTime) {
                        return new CodeObjectCreateExpression(typeof(DateTime), new CodeCastExpression(typeof(long), new CodePrimitiveExpression(((DateTime)defaultValue).Ticks)));
                    }
                    else if (defaultValue is Decimal) { 
                        return new CodeObjectCreateExpression(typeof(Decimal), new CodeCastExpression(typeof(Double), new CodePrimitiveExpression(Decimal.ToDouble((Decimal)defaultValue))));
                    } 
                    else if (defaultValue is bool) { 
                        return new CodePrimitiveExpression((bool)defaultValue);
                    } 
                    else if (defaultValue is string) {
                        // just fall through, we don't need a cast...we'll just push in the primitive
                        // with the string.
                        // 
                        parameterType = null;
                    } 
                    else { 
                        // we got a real instance here but we don't know hot to serialize it.  Need to add a special case!
                        // 
                        Debug.Fail("Unable to serialize type '" + parameterType.Name + "', with value '" + defaultValue.ToString() + "', do we need to special case this type?");
                        throw new Exception(SR.GetString(SR.AxImpUnrecognizedDefaultValueType, method.Name, parameterInfo.Name, parameterType.FullName));
                    }
                } 
                else if (!parameterType.IsValueType) {
 
                    // case 8 

                    // this is for things like stdole.StdFont, etc. 
                    //
                    if (defaultValue is DispatchWrapper) {
                        defaultValue = null;
                    } 

                    if (defaultValue == null || defaultValue is string) { 
                        // just go with null, no cast needed. 
                        //
                        parameterType = null; 
                        return new CodePrimitiveExpression(defaultValue);

                    }								
                    else { 
                        // case 10
                        // we got a real instance here but have no idea what to do with it; we have no way to serialize 
                        // arbitrary values. 
                        //
                        throw new Exception(SR.GetString(SR.AxImpUnrecognizedDefaultValueType, method.Name, parameterInfo.Name, parameterType.FullName)); 
                    }
                }

                // note case 7 (default specified Enum) is handled automatically here. 
                //
                if (parameterType != null && parameterType.IsEnum) { 
                    defaultValue = (int)defaultValue; 
                }
 
                CodeExpression expr = new CodePrimitiveExpression(defaultValue);
                if (parameterType != null) {
                    expr = new CodeCastExpression(parameterType, expr);
                } 
                return expr;
 
            } 

        } 


        /// The class that generates the relfection invoke calls, something like this:
        /// 
        ///  public virtual decimal MethodE(ref decimal e) {
        ///      if ((this.ocx == null)) { 
        ///          throw new System.Windows.Forms.AxHost.InvalidActiveXStateException("MethodE", System.Windows.Forms.AxHost.ActiveXInvokeKind.MethodInvoke); 
        ///      }
        ///      object[] paramArray = new object[] { 
        ///              e,
        ///              new decimal(0)};
        ///      System.Type typeVar = typeof(AxImpTestProject._UserControl1);
        ///      System.Reflection.MethodInfo methodToInvoke = typeVar.GetMethod("MethodE"); 
        ///      decimal returnValue = ((decimal)(methodToInvoke.Invoke(this.ocx, paramArray)));
        ///      e = ((decimal)(paramArray[0])); 
        ///      return returnValue; 
        /// }
        /// 
        private class AxReflectionInvokeMethodGenerator : AxMethodGenerator {

            internal AxReflectionInvokeMethodGenerator(MethodInfo method, bool removeOpts) : base(method, removeOpts){
            } 

            public override CodeExpression DoMethodInvokeCore(CodeMemberMethod method, string methodName, Type returnType, CodeExpression targetObject, List<CodeExpression> parameters) { 
 
                // Here we're generating something like:
                // 
                //            object[] paramArray = new object[] {
                //                              a,
                //                              null,
                //                              "variant default"}; 
                //                              System.Type typeVar = typeof(AxImpTestProject._UserControl1);
                //                              System.Reflection.MethodInfo methodToInvoke = typeVar.GetMethod("MethodF"); 
                // 
                //  and will invoke a method like:
                // 
                //            object returnVal = ((object)(methodToInvoke.Invoke(this.ocx, paramArray)));
                //
                //  so we'll do the work here to wrap up the parameters and munge the method name before we call down to the base.
                // 

 
                // first, create and initialize argument array 
                //
                CodeExpression[] initializers = parameters.ToArray(); 

                // for any that are out params, replace the value with null.
                //
                for (int iparam = 0; iparam < initializers.Length; ++iparam) { 

                    CodeVariableReferenceExpression paramRef = initializers[iparam] as CodeVariableReferenceExpression; 
 
                    if (paramRef == null) {
                        continue; 
                    }

                    AxParameterData paramData = paramRef.UserData[typeof(AxParameterData)] as AxParameterData;
 
                    // if we have param data, that means this wasn't an optional parameter and
                    // we need to specify null if it's an out param otherwise the compiler will 
                    // complain that we're using an out param without initializing it. 
                    //
                    if (paramData != null && paramData.Direction == FieldDirection.Out) { 
                        initializers[iparam] = new CodePrimitiveExpression(null);
                    }
                }
 

                // push the parameters into a param array 
                // 
                CodeArrayCreateExpression paramArrayExp = new CodeArrayCreateExpression(typeof(object), initializers);
                CodeVariableDeclarationStatement paramArrayDecl = new CodeVariableDeclarationStatement(typeof(object[]), "paramArray", paramArrayExp); 
                method.Statements.Add(paramArrayDecl);

                // generate the reflection code
                // 

                CodeTypeOfExpression toe = new CodeTypeOfExpression(ControlType); 
                CodeVariableDeclarationStatement typeVarDecl = new CodeVariableDeclarationStatement(typeof(Type), "typeVar", toe); 
                method.Statements.Add(typeVarDecl);
 
                CodeMethodInvokeExpression mie = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("typeVar"), "GetMethod", new CodeExpression[]{new CodePrimitiveExpression(methodName)});
                CodeVariableDeclarationStatement methodToInvokeDecl = new CodeVariableDeclarationStatement(typeof(MethodInfo), "methodToInvoke", mie);
                method.Statements.Add(methodToInvokeDecl);
 

                // build the new param list 
                // 
                List<CodeExpression> newParams = new List<CodeExpression>();
 
                // add the 'this.ocx' call
                //
                newParams.Add(targetObject);
                CodeVariableReferenceExpression paramArrayExpression = new CodeVariableReferenceExpression("paramArray"); 
                newParams.Add(paramArrayExpression);
 
                // and just call base... 
                //
                CodeExpression returnExpression = base.DoMethodInvokeCore(method, "Invoke", returnType, new CodeVariableReferenceExpression("methodToInvoke"), newParams); 

                // now we've got to pull any reference parameters out of the array and back into their variable.
                //
 
                //assign back non-optional ref and out parameters after the invoke
                for (int iparam = 0; iparam < parameters.Count; ++iparam) { 
 
                    CodeVariableReferenceExpression paramRef = parameters[iparam] as CodeVariableReferenceExpression;
 
                    if (paramRef == null) {
                        continue;
                    }
 
                    AxParameterData paramData = paramRef.UserData[typeof(AxParameterData)] as AxParameterData;
 
                    if (paramData == null || paramData.Direction == FieldDirection.In){ 
                        continue;
                    } 

                    // pull the value out of the array.
                    //
                    CodeExpression right = new CodeCastExpression(paramData.TypeName, 
                            new CodeArrayIndexerExpression(paramArrayExpression, new CodePrimitiveExpression(iparam)));
 
 
                    // assign it back to the variable.
                    // 
                    CodeAssignStatement cas = new CodeAssignStatement(paramRef, right);
                    method.Statements.Add(cas);
                }
 
                return returnExpression;
            } 
        } 

    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
