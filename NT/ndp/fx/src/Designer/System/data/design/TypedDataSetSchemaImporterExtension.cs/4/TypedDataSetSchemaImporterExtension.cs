//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2004' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

using System; 
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Specialized; 
using System.IO;
using System.Reflection; 
using System.Xml; 
using System.Xml.Schema;
using System.Xml.Serialization; 
using System.Xml.Serialization.Advanced;

    public class TypedDataSetSchemaImporterExtension : SchemaImporterExtension {
 	Hashtable importedTypes = new Hashtable(); 
        TypedDataSetGenerator.GenerateOption dataSetGenerateOptions;
 
        public TypedDataSetSchemaImporterExtension () 
            : this(TypedDataSetGenerator.GenerateOption.None)
        { 
        }


        protected TypedDataSetSchemaImporterExtension (TypedDataSetGenerator.GenerateOption dataSetGenerateOptions) 
        {
            this.dataSetGenerateOptions = dataSetGenerateOptions; 
        } 

        public override string ImportSchemaType(string name, string namespaceName, XmlSchemaObject context, XmlSchemas schemas, XmlSchemaImporter importer, 
            CodeCompileUnit compileUnit, CodeNamespace mainNamespace, CodeGenerationOptions options, CodeDomProvider codeProvider) {
            IList values = schemas.GetSchemas(namespaceName);
            if (values.Count != 1) {
                return null; 
            }
            XmlSchema schema = values[0] as XmlSchema; 
            if (schema == null) 
                return null;
            XmlSchemaType type = (XmlSchemaType)schema.SchemaTypes[new XmlQualifiedName(name, namespaceName)]; 
            return ImportSchemaType(type, context, schemas, importer, compileUnit, mainNamespace, options, codeProvider);
        }

        public override string ImportSchemaType(XmlSchemaType type, XmlSchemaObject context, XmlSchemas schemas, 
            XmlSchemaImporter importer, CodeCompileUnit compileUnit, CodeNamespace mainNamespace, CodeGenerationOptions options, CodeDomProvider codeProvider) {
            if (type == null) { 
                return null; 
            }
 
            if (!(context is XmlSchemaElement)) {
                return null;
            }
            XmlSchemaElement e = (XmlSchemaElement)context; 

            if (IsDataSet(e)) { 
                if (importedTypes[type] != null) { 
                    // if we have the type in our internal cache already, we assume we already generated the code for it in
                    // compileUnit/mainNamespace and we also added referenced assemblies to the CompileUnit; so we can just 
                    // return the type name.
                    return (string)importedTypes[type];
                }
 
                return GenerateTypedDataSet(e, schemas, compileUnit, mainNamespace, codeProvider);
            } 
 
            if (type is XmlSchemaComplexType) {
                XmlSchemaComplexType ct = (XmlSchemaComplexType)type; 

                if (ct.Particle is XmlSchemaSequence) {
                    XmlSchemaObjectCollection items = ((XmlSchemaSequence)ct.Particle).Items;
 
                    if (items.Count == 2 && items[0] is XmlSchemaAny && items[1] is XmlSchemaAny) {
                        XmlSchemaAny any0 = (XmlSchemaAny)items[0]; 
                        XmlSchemaAny any1 = (XmlSchemaAny)items[1]; 

                        if (any0.Namespace == XmlSchema.Namespace && any1.Namespace == "urn:schemas-microsoft-com:xml-diffgram-v1") { 
                            // new diffgramm format
                            string ns = null;
                            string tableName = null;
                            foreach (XmlSchemaAttribute a in ct.Attributes) { 
                                if (a.Name == "namespace") {
                                    ns = a.FixedValue.Trim(); 
                                } 
                                else if (a.Name == "tableTypeName") {
                                    // typed table XmlSerialization support; we generate a typed dataset, 
                                    // but return the typed table class name to the proxy generator
                                    tableName = a.FixedValue.Trim();
                                }
 
                                if (ns != null && tableName != null) {
                                    break; 
                                } 
                            }
 
                            if (ns == null) {
                                return null;
                            }
                            else { 
                                IList values = schemas.GetSchemas(ns);
                                if (values.Count != 1) { 
                                    return null; 
                                }
                                XmlSchema schema = values[0] as XmlSchema; 
                                if (schema == null || schema.Id == null) {
                                    return null;
                                }
 
                                XmlSchemaElement ds = FindDataSetElement(schema, schemas);
                                if (ds != null) { 
                                    string datasetName = ImportSchemaType(ds.SchemaType, ds, schemas, importer, compileUnit, mainNamespace, options, codeProvider); 

                                    if (tableName == null) { 
                                        return datasetName;
                                    }
                                    else {
                                        return CodeGenHelper.GetTypeName(codeProvider, datasetName, tableName); 
                                    }
                                } 
                                else { 
                                    return null;
                                } 
                            }
                        }
                    }
                } 
                if (ct.Particle is XmlSchemaSequence || ct.Particle is XmlSchemaAll) {
                    XmlSchemaObjectCollection items = ((XmlSchemaGroupBase)ct.Particle).Items; 
 
                    if (items.Count == 1) {
                        if (!(items[0] is XmlSchemaAny)) { 
                            return null;
                        }

                        XmlSchemaAny any = (XmlSchemaAny)items[0]; 
                        if (any.Namespace == null) {
                            return null; 
                        } 
                        if (any.Namespace.IndexOf('#') >= 0) {
                            return null; // special syntax (##any, ##other, ...) 
                        }
                        if (any.Namespace.IndexOf(' ') >= 0) {
                            return null; // more than one Uri present
                        } 

                        IList values = schemas.GetSchemas(any.Namespace); 
                        if (values.Count != 1) { 
                            return null;
                        } 
                        XmlSchema schema = values[0] as XmlSchema;
                        if (schema == null) {
                            return null;
                        } 
                        if (schema.Id == null) {
                            return null; 
                        } 

                        XmlSchemaElement ds = FindDataSetElement(schema, schemas); 
                        if (ds != null) {
                            return ImportSchemaType(ds.SchemaType, ds, schemas, importer, compileUnit, mainNamespace, options, codeProvider);
                        }
                        else { 
                            return null;
                        } 
                    } 
                }
            } 

            return null;
        }
 
        internal string GenerateTypedDataSet(XmlSchemaElement element, XmlSchemas schemas, CodeCompileUnit compileUnit, CodeNamespace mainNamespace, CodeDomProvider codeProvider) {
            if (element == null) { 
                return null; 
            }
 
            if (importedTypes[element.SchemaType] != null) {
                // if we have the type in our internal cache already, we assume we already generated the code for it in
                // compileUnit/mainNamespace and we also added referenced assemblies to the CompileUnit; so we can just
                // return the type name. 
                return (string)importedTypes[element.SchemaType];
            } 
 
            IList values = schemas.GetSchemas(element.QualifiedName.Namespace);
            if (values.Count != 1) { 
                return null;
            }
            XmlSchema schema = values[0] as XmlSchema;
            if (schema == null) { 
                return null;
            } 
 
            MemoryStream stream = new MemoryStream();
            schema.Write(stream); 
            stream.Position = 0;
            DesignDataSource ds = new DesignDataSource();
            ds.ReadXmlSchema(stream);
            stream.Close(); 

            string typeName = TypedDataSetGenerator.GenerateInternal(ds, compileUnit, mainNamespace, codeProvider, this.dataSetGenerateOptions, null); 
            importedTypes.Add(element.SchemaType, typeName); 

            return typeName; 
        }


        internal static bool IsDataSet(XmlSchemaElement e) { 
			if (e.UnhandledAttributes != null) {
				foreach (XmlAttribute a in e.UnhandledAttributes) { 
					if (a.LocalName == "IsDataSet" && a.NamespaceURI == Keywords.MSDNS) { 
 						// currently the msdata:IsDataSet uses its own format for the boolean values
						if (a.Value == "True" || a.Value == "true" || a.Value == "1") { 
                            return true;
                        }
 					}
 				} 
			}
 
 			return false; 
		}
 
        internal XmlSchemaElement FindDataSetElement(XmlSchema schema, XmlSchemas schemas) {
            foreach(XmlSchemaObject item in schema.Items) {
                if (item is XmlSchemaElement && IsDataSet((XmlSchemaElement)item)) {
                    XmlSchemaElement ds = (XmlSchemaElement)item; 
                    // return cached element in the case if we doing typesharing
                    return (XmlSchemaElement)schemas.Find(ds.QualifiedName, typeof(XmlSchemaElement)); 
                } 
            }
            return null; 
        }

	}
 
    internal sealed class Keywords {
 
        private Keywords() { /* prevent utility class from being insantiated*/ } 

        // Keywords for DataSet Namespace 
        internal const string MSDNS                 = "urn:schemas-microsoft-com:xml-msdata";
        internal const string DFFNS                 = "urn:schemas-microsoft-com:xml-diffgram-v1";
        internal const string WS_DATASETFULLQNAME   = "system.data.dataset";
        internal const string WS_VERSION            = "WSDL_VERSION"; 
    }
 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2004' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

using System; 
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Specialized; 
using System.IO;
using System.Reflection; 
using System.Xml; 
using System.Xml.Schema;
using System.Xml.Serialization; 
using System.Xml.Serialization.Advanced;

    public class TypedDataSetSchemaImporterExtension : SchemaImporterExtension {
 	Hashtable importedTypes = new Hashtable(); 
        TypedDataSetGenerator.GenerateOption dataSetGenerateOptions;
 
        public TypedDataSetSchemaImporterExtension () 
            : this(TypedDataSetGenerator.GenerateOption.None)
        { 
        }


        protected TypedDataSetSchemaImporterExtension (TypedDataSetGenerator.GenerateOption dataSetGenerateOptions) 
        {
            this.dataSetGenerateOptions = dataSetGenerateOptions; 
        } 

        public override string ImportSchemaType(string name, string namespaceName, XmlSchemaObject context, XmlSchemas schemas, XmlSchemaImporter importer, 
            CodeCompileUnit compileUnit, CodeNamespace mainNamespace, CodeGenerationOptions options, CodeDomProvider codeProvider) {
            IList values = schemas.GetSchemas(namespaceName);
            if (values.Count != 1) {
                return null; 
            }
            XmlSchema schema = values[0] as XmlSchema; 
            if (schema == null) 
                return null;
            XmlSchemaType type = (XmlSchemaType)schema.SchemaTypes[new XmlQualifiedName(name, namespaceName)]; 
            return ImportSchemaType(type, context, schemas, importer, compileUnit, mainNamespace, options, codeProvider);
        }

        public override string ImportSchemaType(XmlSchemaType type, XmlSchemaObject context, XmlSchemas schemas, 
            XmlSchemaImporter importer, CodeCompileUnit compileUnit, CodeNamespace mainNamespace, CodeGenerationOptions options, CodeDomProvider codeProvider) {
            if (type == null) { 
                return null; 
            }
 
            if (!(context is XmlSchemaElement)) {
                return null;
            }
            XmlSchemaElement e = (XmlSchemaElement)context; 

            if (IsDataSet(e)) { 
                if (importedTypes[type] != null) { 
                    // if we have the type in our internal cache already, we assume we already generated the code for it in
                    // compileUnit/mainNamespace and we also added referenced assemblies to the CompileUnit; so we can just 
                    // return the type name.
                    return (string)importedTypes[type];
                }
 
                return GenerateTypedDataSet(e, schemas, compileUnit, mainNamespace, codeProvider);
            } 
 
            if (type is XmlSchemaComplexType) {
                XmlSchemaComplexType ct = (XmlSchemaComplexType)type; 

                if (ct.Particle is XmlSchemaSequence) {
                    XmlSchemaObjectCollection items = ((XmlSchemaSequence)ct.Particle).Items;
 
                    if (items.Count == 2 && items[0] is XmlSchemaAny && items[1] is XmlSchemaAny) {
                        XmlSchemaAny any0 = (XmlSchemaAny)items[0]; 
                        XmlSchemaAny any1 = (XmlSchemaAny)items[1]; 

                        if (any0.Namespace == XmlSchema.Namespace && any1.Namespace == "urn:schemas-microsoft-com:xml-diffgram-v1") { 
                            // new diffgramm format
                            string ns = null;
                            string tableName = null;
                            foreach (XmlSchemaAttribute a in ct.Attributes) { 
                                if (a.Name == "namespace") {
                                    ns = a.FixedValue.Trim(); 
                                } 
                                else if (a.Name == "tableTypeName") {
                                    // typed table XmlSerialization support; we generate a typed dataset, 
                                    // but return the typed table class name to the proxy generator
                                    tableName = a.FixedValue.Trim();
                                }
 
                                if (ns != null && tableName != null) {
                                    break; 
                                } 
                            }
 
                            if (ns == null) {
                                return null;
                            }
                            else { 
                                IList values = schemas.GetSchemas(ns);
                                if (values.Count != 1) { 
                                    return null; 
                                }
                                XmlSchema schema = values[0] as XmlSchema; 
                                if (schema == null || schema.Id == null) {
                                    return null;
                                }
 
                                XmlSchemaElement ds = FindDataSetElement(schema, schemas);
                                if (ds != null) { 
                                    string datasetName = ImportSchemaType(ds.SchemaType, ds, schemas, importer, compileUnit, mainNamespace, options, codeProvider); 

                                    if (tableName == null) { 
                                        return datasetName;
                                    }
                                    else {
                                        return CodeGenHelper.GetTypeName(codeProvider, datasetName, tableName); 
                                    }
                                } 
                                else { 
                                    return null;
                                } 
                            }
                        }
                    }
                } 
                if (ct.Particle is XmlSchemaSequence || ct.Particle is XmlSchemaAll) {
                    XmlSchemaObjectCollection items = ((XmlSchemaGroupBase)ct.Particle).Items; 
 
                    if (items.Count == 1) {
                        if (!(items[0] is XmlSchemaAny)) { 
                            return null;
                        }

                        XmlSchemaAny any = (XmlSchemaAny)items[0]; 
                        if (any.Namespace == null) {
                            return null; 
                        } 
                        if (any.Namespace.IndexOf('#') >= 0) {
                            return null; // special syntax (##any, ##other, ...) 
                        }
                        if (any.Namespace.IndexOf(' ') >= 0) {
                            return null; // more than one Uri present
                        } 

                        IList values = schemas.GetSchemas(any.Namespace); 
                        if (values.Count != 1) { 
                            return null;
                        } 
                        XmlSchema schema = values[0] as XmlSchema;
                        if (schema == null) {
                            return null;
                        } 
                        if (schema.Id == null) {
                            return null; 
                        } 

                        XmlSchemaElement ds = FindDataSetElement(schema, schemas); 
                        if (ds != null) {
                            return ImportSchemaType(ds.SchemaType, ds, schemas, importer, compileUnit, mainNamespace, options, codeProvider);
                        }
                        else { 
                            return null;
                        } 
                    } 
                }
            } 

            return null;
        }
 
        internal string GenerateTypedDataSet(XmlSchemaElement element, XmlSchemas schemas, CodeCompileUnit compileUnit, CodeNamespace mainNamespace, CodeDomProvider codeProvider) {
            if (element == null) { 
                return null; 
            }
 
            if (importedTypes[element.SchemaType] != null) {
                // if we have the type in our internal cache already, we assume we already generated the code for it in
                // compileUnit/mainNamespace and we also added referenced assemblies to the CompileUnit; so we can just
                // return the type name. 
                return (string)importedTypes[element.SchemaType];
            } 
 
            IList values = schemas.GetSchemas(element.QualifiedName.Namespace);
            if (values.Count != 1) { 
                return null;
            }
            XmlSchema schema = values[0] as XmlSchema;
            if (schema == null) { 
                return null;
            } 
 
            MemoryStream stream = new MemoryStream();
            schema.Write(stream); 
            stream.Position = 0;
            DesignDataSource ds = new DesignDataSource();
            ds.ReadXmlSchema(stream);
            stream.Close(); 

            string typeName = TypedDataSetGenerator.GenerateInternal(ds, compileUnit, mainNamespace, codeProvider, this.dataSetGenerateOptions, null); 
            importedTypes.Add(element.SchemaType, typeName); 

            return typeName; 
        }


        internal static bool IsDataSet(XmlSchemaElement e) { 
			if (e.UnhandledAttributes != null) {
				foreach (XmlAttribute a in e.UnhandledAttributes) { 
					if (a.LocalName == "IsDataSet" && a.NamespaceURI == Keywords.MSDNS) { 
 						// currently the msdata:IsDataSet uses its own format for the boolean values
						if (a.Value == "True" || a.Value == "true" || a.Value == "1") { 
                            return true;
                        }
 					}
 				} 
			}
 
 			return false; 
		}
 
        internal XmlSchemaElement FindDataSetElement(XmlSchema schema, XmlSchemas schemas) {
            foreach(XmlSchemaObject item in schema.Items) {
                if (item is XmlSchemaElement && IsDataSet((XmlSchemaElement)item)) {
                    XmlSchemaElement ds = (XmlSchemaElement)item; 
                    // return cached element in the case if we doing typesharing
                    return (XmlSchemaElement)schemas.Find(ds.QualifiedName, typeof(XmlSchemaElement)); 
                } 
            }
            return null; 
        }

	}
 
    internal sealed class Keywords {
 
        private Keywords() { /* prevent utility class from being insantiated*/ } 

        // Keywords for DataSet Namespace 
        internal const string MSDNS                 = "urn:schemas-microsoft-com:xml-msdata";
        internal const string DFFNS                 = "urn:schemas-microsoft-com:xml-diffgram-v1";
        internal const string WS_DATASETFULLQNAME   = "system.data.dataset";
        internal const string WS_VERSION            = "WSDL_VERSION"; 
    }
 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
