//------------------------------------------------------------------------------ 
// <copyright file="DiagnosticsSwitches.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Xml { 
    using System.Diagnostics;
 
#if DEBUG
    public static class DiagnosticsSwitches {
#else
    internal static class DiagnosticsSwitches { 
#endif
        private static BooleanSwitch xmlSchemaContentModel; 
        private static TraceSwitch xmlSchema; 
        private static BooleanSwitch keepTempFiles;
        private static BooleanSwitch pregenEventLog; 
        private static TraceSwitch xmlSerialization;
        private static TraceSwitch xslTypeInference;

        public static BooleanSwitch XmlSchemaContentModel { 
            get {
                if (xmlSchemaContentModel == null) { 
                    xmlSchemaContentModel = new BooleanSwitch("XmlSchemaContentModel", "Enable tracing for the XmlSchema content model."); 
                }
                return xmlSchemaContentModel; 
            }
        }

        public static TraceSwitch XmlSchema { 
            get {
                if (xmlSchema == null) { 
                    xmlSchema = new TraceSwitch("XmlSchema", "Enable tracing for the XmlSchema class."); 
                }
                return xmlSchema; 
            }
        }

        public static BooleanSwitch KeepTempFiles { 
            get {
                if (keepTempFiles == null) { 
                    keepTempFiles = new BooleanSwitch("XmlSerialization.Compilation", "Keep XmlSerialization generated (temp) files."); 
                }
                return keepTempFiles; 
            }
        }

        public static BooleanSwitch PregenEventLog { 
            get {
                if (pregenEventLog == null) { 
                    pregenEventLog = new BooleanSwitch("XmlSerialization.PregenEventLog", "Log failures while loading pre-generated XmlSerialization assembly."); 
                }
                return pregenEventLog; 
            }
        }

        public static TraceSwitch XmlSerialization { 
            get {
                if (xmlSerialization == null) { 
                    xmlSerialization = new TraceSwitch("XmlSerialization", "Enable tracing for the System.Xml.Serialization component."); 
                }
                return xmlSerialization; 
            }
        }

        public static TraceSwitch XslTypeInference { 
            get {
                if (xslTypeInference == null) { 
                    xslTypeInference = new TraceSwitch("XslTypeInference", "Enable tracing for the XSLT type inference algorithm."); 
                }
                return xslTypeInference; 
            }
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DiagnosticsSwitches.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Xml { 
    using System.Diagnostics;
 
#if DEBUG
    public static class DiagnosticsSwitches {
#else
    internal static class DiagnosticsSwitches { 
#endif
        private static BooleanSwitch xmlSchemaContentModel; 
        private static TraceSwitch xmlSchema; 
        private static BooleanSwitch keepTempFiles;
        private static BooleanSwitch pregenEventLog; 
        private static TraceSwitch xmlSerialization;
        private static TraceSwitch xslTypeInference;

        public static BooleanSwitch XmlSchemaContentModel { 
            get {
                if (xmlSchemaContentModel == null) { 
                    xmlSchemaContentModel = new BooleanSwitch("XmlSchemaContentModel", "Enable tracing for the XmlSchema content model."); 
                }
                return xmlSchemaContentModel; 
            }
        }

        public static TraceSwitch XmlSchema { 
            get {
                if (xmlSchema == null) { 
                    xmlSchema = new TraceSwitch("XmlSchema", "Enable tracing for the XmlSchema class."); 
                }
                return xmlSchema; 
            }
        }

        public static BooleanSwitch KeepTempFiles { 
            get {
                if (keepTempFiles == null) { 
                    keepTempFiles = new BooleanSwitch("XmlSerialization.Compilation", "Keep XmlSerialization generated (temp) files."); 
                }
                return keepTempFiles; 
            }
        }

        public static BooleanSwitch PregenEventLog { 
            get {
                if (pregenEventLog == null) { 
                    pregenEventLog = new BooleanSwitch("XmlSerialization.PregenEventLog", "Log failures while loading pre-generated XmlSerialization assembly."); 
                }
                return pregenEventLog; 
            }
        }

        public static TraceSwitch XmlSerialization { 
            get {
                if (xmlSerialization == null) { 
                    xmlSerialization = new TraceSwitch("XmlSerialization", "Enable tracing for the System.Xml.Serialization component."); 
                }
                return xmlSerialization; 
            }
        }

        public static TraceSwitch XslTypeInference { 
            get {
                if (xslTypeInference == null) { 
                    xslTypeInference = new TraceSwitch("XslTypeInference", "Enable tracing for the XSLT type inference algorithm."); 
                }
                return xslTypeInference; 
            }
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
