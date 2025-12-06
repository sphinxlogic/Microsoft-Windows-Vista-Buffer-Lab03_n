//------------------------------------------------------------------------------ 
// <copyright file="XmlWriterSettings.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
using System.Collections.Generic; 
using System.Diagnostics;
using System.IO; 
using System.Text;
using System.Xml.Xsl.Runtime;

namespace System.Xml { 

    public enum XmlOutputMethod { 
        Xml         = 0,    // Use Xml 1.0 rules to serialize 
        Html        = 1,    // Use Html rules specified by Xslt specification to serialize
        Text        = 2,    // Only serialize text blocks 
        AutoDetect  = 3,    // Choose between Xml and Html output methods at runtime (using Xslt rules to do so)
    }

    internal enum XmlStandalone { 
        // Do not change the constants - XmlBinaryWriter depends in it
        Omit    = 0, 
        Yes     = 1, 
        No      = 2,
    } 

    /// <summary>
    /// Three-state logic enumeration.
    /// </summary> 
    internal enum TriState {
        Unknown = -1, 
        False = 0, 
        True = 1,
    }; 


    // XmlReaderSettings class specifies features of an XmlWriter.
    public sealed class XmlWriterSettings { 
//
// Fields 
// 
        // Text settings
        Encoding            encoding; 
        bool                omitXmlDecl;
        NewLineHandling     newLineHandling;
        string              newLineChars;
        TriState            indent; 
        string              indentChars;
        bool                newLineOnAttributes; 
        bool                closeOutput; 

        // Conformance settings 
        ConformanceLevel conformanceLevel;
        bool             checkCharacters;

        // Xslt settings 
        XmlOutputMethod outputMethod;
        List<XmlQualifiedName> cdataSections = new List<XmlQualifiedName>(); 
        bool            mergeCDataSections; 
        string          mediaType;
        string          docTypeSystem; 
        string          docTypePublic;
        XmlStandalone   standalone;
        bool            autoXmlDecl;
 
        // read-only flag
        bool    isReadOnly; 
 
//
// Constructor 
//
        public XmlWriterSettings() {
            Reset();
        } 

// 
// Properties 
//
    // Text 
        public Encoding Encoding {
            get {
                return encoding;
            } 
            set {
                CheckReadOnly( "Encoding" ); 
                encoding = value; 
            }
        } 

        // True if an xml declaration should *not* be written.
        public bool OmitXmlDeclaration {
            get { 
                return omitXmlDecl;
            } 
            set { 
                CheckReadOnly( "OmitXmlDeclaration" );
                omitXmlDecl = value; 
            }
        }

        // See NewLineHandling enum for details. 
        public NewLineHandling NewLineHandling {
            get { 
                return newLineHandling; 
            }
            set { 
                CheckReadOnly("NewLineHandling");

                if ( (uint)value > (uint)NewLineHandling.None ) {
                    throw new ArgumentOutOfRangeException( "value" ); 
                }
                newLineHandling = value; 
            } 
        }
 
        // Line terminator string. By default, this is a carriage return followed by a line feed ("\r\n").
        public string NewLineChars {
            get {
                return newLineChars; 
            }
            set { 
                CheckReadOnly( "NewLineChars" ); 

                if ( value == null ) { 
                    throw new ArgumentNullException( "value" );
                }
                newLineChars = value;
            } 
        }
 
        // True if output should be indented using rules that are appropriate to the output rules (i.e. Xml, Html, etc). 
        public bool Indent {
            get { 
                return indent == TriState.True;
            }
            set {
                CheckReadOnly( "Indent" ); 
                indent = value ? TriState.True : TriState.False;
            } 
        } 

        // Characters to use when indenting. This is usually tab or some spaces, but can be anything. 
        public string IndentChars {
            get {
                return indentChars;
            } 
            set {
                CheckReadOnly( "IndentChars" ); 
 
                if ( value == null ) {
                    throw new ArgumentNullException("value"); 
                }
                indentChars = value;
            }
        } 

        // Whether or not indent attributes on new lines. 
        public bool NewLineOnAttributes { 
            get {
                return newLineOnAttributes; 
            }
            set {
                CheckReadOnly( "NewLineOnAttributes" );
                newLineOnAttributes = value; 
            }
        } 
 
        // Whether or not the XmlWriter should close the underlying stream or TextWriter when Close is called on the XmlWriter.
        public bool CloseOutput { 
            get {
                return closeOutput;
            }
            set { 
                CheckReadOnly( "CloseOutput" );
                closeOutput = value; 
            } 
        }
 

    // Conformance
        // See ConformanceLevel enum for details.
        public ConformanceLevel ConformanceLevel { 
            get {
                return conformanceLevel; 
            } 
            set {
                CheckReadOnly( "ConformanceLevel" ); 

                if ( (uint)value > (uint)ConformanceLevel.Document ) {
                    throw new ArgumentOutOfRangeException( "value" );
                } 
                conformanceLevel = value;
            } 
        } 

        // Whether or not to check content characters that they are valid XML characters. 
        public bool CheckCharacters {
            get {
                return checkCharacters;
            } 
            set {
                CheckReadOnly( "CheckCharacters" ); 
                checkCharacters = value; 
            }
        } 

//
// Public methods
// 
        public void Reset() {
            encoding = Encoding.UTF8; 
            omitXmlDecl = false; 
            newLineHandling = NewLineHandling.Replace;
            newLineChars = "\r\n"; 
            indent = TriState.Unknown;
            indentChars = "  ";
            newLineOnAttributes = false;
            closeOutput = false; 

            conformanceLevel = ConformanceLevel.Document; 
            checkCharacters = true; 

            outputMethod = XmlOutputMethod.Xml; 
            cdataSections.Clear();
            mergeCDataSections = false;
            mediaType = null;
            docTypeSystem = null; 
            docTypePublic = null;
            standalone = XmlStandalone.Omit; 
 
            isReadOnly = false;
        } 

        // Deep clone all settings (except read-only, which is always set to false).  The original and new objects
        // can now be set independently of each other.
        public XmlWriterSettings Clone() { 
            XmlWriterSettings clonedSettings = MemberwiseClone() as XmlWriterSettings;
 
            // Deep clone shared settings that are not immutable 
            clonedSettings.cdataSections = new List<XmlQualifiedName>( cdataSections );
 
            // Read-only setting is always false on clones
            clonedSettings.isReadOnly = false;

            return clonedSettings; 
        }
 
 
//
// Internal and private methods 
//
        internal bool ReadOnly {
            get {
                return isReadOnly; 
            }
            set { 
                isReadOnly = value; 
            }
        } 

        // Specifies the method (Html, Xml, etc.) that will be used to serialize the result tree.
        public XmlOutputMethod OutputMethod {
            get { 
                return outputMethod;
            } 
            internal set { 
                outputMethod = value;
            } 
        }

        // Set of XmlQualifiedNames that identify any elements that need to have text children wrapped in CData sections.
        internal List<XmlQualifiedName> CDataSectionElements { 
            get {
                Debug.Assert(cdataSections != null); 
                return cdataSections; 
            }
        } 

        internal bool MergeCDataSections {
            get {
                return mergeCDataSections; 
            }
            set { 
                CheckReadOnly( "MergeCDataSections" ); 
                mergeCDataSections = value;
            } 
        }

        // Used in Html writer when writing Meta element.  Null denotes the default media type.
        internal string MediaType { 
            get {
                return mediaType; 
            } 
            set {
                CheckReadOnly( "MediaType" ); 
                mediaType = value;
            }
        }
 
        // System Id in doc-type declaration.  Null denotes the absence of the system Id.
        internal string DocTypeSystem { 
            get { 
                return docTypeSystem;
            } 
            set {
                CheckReadOnly( "DocTypeSystem" );
                docTypeSystem = value;
            } 
        }
 
        // Public Id in doc-type declaration.  Null denotes the absence of the public Id. 
        internal string DocTypePublic {
            get { 
                return docTypePublic;
            }
            set {
                CheckReadOnly( "DocTypePublic" ); 
                docTypePublic = value;
            } 
        } 

        // Yes for standalone="yes", No for standalone="no", and Omit for no standalone. 
        internal XmlStandalone Standalone {
            get {
                return standalone;
            } 
            set {
                CheckReadOnly( "Standalone" ); 
                standalone = value; 
            }
        } 

        // True if an xml declaration should automatically be output (no need to call WriteStartDocument)
        internal bool AutoXmlDeclaration {
            get { 
                return autoXmlDecl;
            } 
            set { 
                CheckReadOnly( "AutoXmlDeclaration" );
                autoXmlDecl = value; 
            }
        }

        // If TriState.Unknown, then Indent property was not explicitly set.  In this case, the AutoDetect output 
        // method will default to Indent=true for Html and Indent=false for Xml.
        internal TriState InternalIndent { 
            get { 
                return indent;
            } 
        }

        internal bool IsQuerySpecific {
            get { 
                return cdataSections.Count != 0 || docTypePublic != null ||
                       docTypeSystem != null || standalone == XmlStandalone.Yes; 
            } 
        }
 
        private void CheckReadOnly( string propertyName ) {
            if ( isReadOnly ) {
                throw new XmlException( Res.Xml_ReadOnlyProperty, "XmlWriterSettings." + propertyName );
            } 
        }
 
#if !HIDE_XSL 
        /// <summary>
        /// Serialize the object to BinaryWriter. 
        /// </summary>
        internal void GetObjectData(XmlQueryDataWriter writer) {
            // Encoding encoding;
            // NOTE: For Encoding we serialize only CodePage, and ignore EncoderFallback/DecoderFallback. 
            // It suffices for XSLT purposes, but not in the general case.
            Debug.Assert(encoding.Equals(Encoding.GetEncoding(encoding.CodePage)), "Cannot serialize encoding correctly"); 
            writer.Write(encoding.CodePage); 
            // bool omitXmlDecl;
            writer.Write(omitXmlDecl); 
            // NewLineHandling newLineHandling;
            writer.Write((sbyte)newLineHandling);
            // string newLineChars;
            writer.WriteStringQ(newLineChars); 
            // TriState indent;
            writer.Write((sbyte)indent); 
            // string indentChars; 
            writer.WriteStringQ(indentChars);
            // bool newLineOnAttributes; 
            writer.Write(newLineOnAttributes);
            // bool closeOutput;
            writer.Write(closeOutput);
            // ConformanceLevel conformanceLevel; 
            writer.Write((sbyte)conformanceLevel);
            // bool checkCharacters; 
            writer.Write(checkCharacters); 
            // XmlOutputMethod outputMethod;
            writer.Write((sbyte)outputMethod); 
            // List<XmlQualifiedName> cdataSections;
            writer.Write(cdataSections.Count);
            foreach (XmlQualifiedName qname in cdataSections) {
                writer.Write(qname.Name); 
                writer.Write(qname.Namespace);
            } 
            // bool mergeCDataSections; 
            writer.Write(mergeCDataSections);
            // string mediaType; 
            writer.WriteStringQ(mediaType);
            // string docTypeSystem;
            writer.WriteStringQ(docTypeSystem);
            // string docTypePublic; 
            writer.WriteStringQ(docTypePublic);
            // XmlStandalone standalone; 
            writer.Write((sbyte)standalone); 
            // bool autoXmlDecl;
            writer.Write(autoXmlDecl); 
            // bool isReadOnly;
            writer.Write(isReadOnly);
        }
 
        /// <summary>
        /// Deserialize the object from BinaryReader. 
        /// </summary> 
        internal XmlWriterSettings(XmlQueryDataReader reader) {
            // Encoding encoding; 
            encoding = Encoding.GetEncoding(reader.ReadInt32());
            // bool omitXmlDecl;
            omitXmlDecl = reader.ReadBoolean();
            // NewLineHandling newLineHandling; 
            newLineHandling = (NewLineHandling)reader.ReadSByte(0, (sbyte)NewLineHandling.None);
            // string newLineChars; 
            newLineChars = reader.ReadStringQ(); 
            // TriState indent;
            indent = (TriState)reader.ReadSByte((sbyte)TriState.Unknown, (sbyte)TriState.True); 
            // string indentChars;
            indentChars = reader.ReadStringQ();
            // bool newLineOnAttributes;
            newLineOnAttributes = reader.ReadBoolean(); 
            // bool closeOutput;
            closeOutput = reader.ReadBoolean(); 
            // ConformanceLevel conformanceLevel; 
            conformanceLevel = (ConformanceLevel)reader.ReadSByte(0, (sbyte)ConformanceLevel.Document);
            // bool checkCharacters; 
            checkCharacters = reader.ReadBoolean();
            // XmlOutputMethod outputMethod;
            outputMethod = (XmlOutputMethod)reader.ReadSByte(0, (sbyte)XmlOutputMethod.AutoDetect);
            // List<XmlQualifiedName> cdataSections; 
            int length = reader.ReadInt32();
            cdataSections = new List<XmlQualifiedName>(length); 
            for (int idx = 0; idx < length; idx++) { 
                cdataSections.Add(new XmlQualifiedName(reader.ReadString(), reader.ReadString()));
            } 
            // bool mergeCDataSections;
            mergeCDataSections = reader.ReadBoolean();
            // string mediaType;
            mediaType = reader.ReadStringQ(); 
            // string docTypeSystem;
            docTypeSystem = reader.ReadStringQ(); 
            // string docTypePublic; 
            docTypePublic = reader.ReadStringQ();
            // XmlStandalone standalone; 
            Standalone = (XmlStandalone)reader.ReadSByte(0, (sbyte)XmlStandalone.No);
            // bool autoXmlDecl;
            autoXmlDecl = reader.ReadBoolean();
            // bool isReadOnly; 
            isReadOnly = reader.ReadBoolean();
        } 
#else 
        internal void GetObjectData(object writer) { }
        internal XmlWriterSettings(object reader) { } 
#endif
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="XmlWriterSettings.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
using System.Collections.Generic; 
using System.Diagnostics;
using System.IO; 
using System.Text;
using System.Xml.Xsl.Runtime;

namespace System.Xml { 

    public enum XmlOutputMethod { 
        Xml         = 0,    // Use Xml 1.0 rules to serialize 
        Html        = 1,    // Use Html rules specified by Xslt specification to serialize
        Text        = 2,    // Only serialize text blocks 
        AutoDetect  = 3,    // Choose between Xml and Html output methods at runtime (using Xslt rules to do so)
    }

    internal enum XmlStandalone { 
        // Do not change the constants - XmlBinaryWriter depends in it
        Omit    = 0, 
        Yes     = 1, 
        No      = 2,
    } 

    /// <summary>
    /// Three-state logic enumeration.
    /// </summary> 
    internal enum TriState {
        Unknown = -1, 
        False = 0, 
        True = 1,
    }; 


    // XmlReaderSettings class specifies features of an XmlWriter.
    public sealed class XmlWriterSettings { 
//
// Fields 
// 
        // Text settings
        Encoding            encoding; 
        bool                omitXmlDecl;
        NewLineHandling     newLineHandling;
        string              newLineChars;
        TriState            indent; 
        string              indentChars;
        bool                newLineOnAttributes; 
        bool                closeOutput; 

        // Conformance settings 
        ConformanceLevel conformanceLevel;
        bool             checkCharacters;

        // Xslt settings 
        XmlOutputMethod outputMethod;
        List<XmlQualifiedName> cdataSections = new List<XmlQualifiedName>(); 
        bool            mergeCDataSections; 
        string          mediaType;
        string          docTypeSystem; 
        string          docTypePublic;
        XmlStandalone   standalone;
        bool            autoXmlDecl;
 
        // read-only flag
        bool    isReadOnly; 
 
//
// Constructor 
//
        public XmlWriterSettings() {
            Reset();
        } 

// 
// Properties 
//
    // Text 
        public Encoding Encoding {
            get {
                return encoding;
            } 
            set {
                CheckReadOnly( "Encoding" ); 
                encoding = value; 
            }
        } 

        // True if an xml declaration should *not* be written.
        public bool OmitXmlDeclaration {
            get { 
                return omitXmlDecl;
            } 
            set { 
                CheckReadOnly( "OmitXmlDeclaration" );
                omitXmlDecl = value; 
            }
        }

        // See NewLineHandling enum for details. 
        public NewLineHandling NewLineHandling {
            get { 
                return newLineHandling; 
            }
            set { 
                CheckReadOnly("NewLineHandling");

                if ( (uint)value > (uint)NewLineHandling.None ) {
                    throw new ArgumentOutOfRangeException( "value" ); 
                }
                newLineHandling = value; 
            } 
        }
 
        // Line terminator string. By default, this is a carriage return followed by a line feed ("\r\n").
        public string NewLineChars {
            get {
                return newLineChars; 
            }
            set { 
                CheckReadOnly( "NewLineChars" ); 

                if ( value == null ) { 
                    throw new ArgumentNullException( "value" );
                }
                newLineChars = value;
            } 
        }
 
        // True if output should be indented using rules that are appropriate to the output rules (i.e. Xml, Html, etc). 
        public bool Indent {
            get { 
                return indent == TriState.True;
            }
            set {
                CheckReadOnly( "Indent" ); 
                indent = value ? TriState.True : TriState.False;
            } 
        } 

        // Characters to use when indenting. This is usually tab or some spaces, but can be anything. 
        public string IndentChars {
            get {
                return indentChars;
            } 
            set {
                CheckReadOnly( "IndentChars" ); 
 
                if ( value == null ) {
                    throw new ArgumentNullException("value"); 
                }
                indentChars = value;
            }
        } 

        // Whether or not indent attributes on new lines. 
        public bool NewLineOnAttributes { 
            get {
                return newLineOnAttributes; 
            }
            set {
                CheckReadOnly( "NewLineOnAttributes" );
                newLineOnAttributes = value; 
            }
        } 
 
        // Whether or not the XmlWriter should close the underlying stream or TextWriter when Close is called on the XmlWriter.
        public bool CloseOutput { 
            get {
                return closeOutput;
            }
            set { 
                CheckReadOnly( "CloseOutput" );
                closeOutput = value; 
            } 
        }
 

    // Conformance
        // See ConformanceLevel enum for details.
        public ConformanceLevel ConformanceLevel { 
            get {
                return conformanceLevel; 
            } 
            set {
                CheckReadOnly( "ConformanceLevel" ); 

                if ( (uint)value > (uint)ConformanceLevel.Document ) {
                    throw new ArgumentOutOfRangeException( "value" );
                } 
                conformanceLevel = value;
            } 
        } 

        // Whether or not to check content characters that they are valid XML characters. 
        public bool CheckCharacters {
            get {
                return checkCharacters;
            } 
            set {
                CheckReadOnly( "CheckCharacters" ); 
                checkCharacters = value; 
            }
        } 

//
// Public methods
// 
        public void Reset() {
            encoding = Encoding.UTF8; 
            omitXmlDecl = false; 
            newLineHandling = NewLineHandling.Replace;
            newLineChars = "\r\n"; 
            indent = TriState.Unknown;
            indentChars = "  ";
            newLineOnAttributes = false;
            closeOutput = false; 

            conformanceLevel = ConformanceLevel.Document; 
            checkCharacters = true; 

            outputMethod = XmlOutputMethod.Xml; 
            cdataSections.Clear();
            mergeCDataSections = false;
            mediaType = null;
            docTypeSystem = null; 
            docTypePublic = null;
            standalone = XmlStandalone.Omit; 
 
            isReadOnly = false;
        } 

        // Deep clone all settings (except read-only, which is always set to false).  The original and new objects
        // can now be set independently of each other.
        public XmlWriterSettings Clone() { 
            XmlWriterSettings clonedSettings = MemberwiseClone() as XmlWriterSettings;
 
            // Deep clone shared settings that are not immutable 
            clonedSettings.cdataSections = new List<XmlQualifiedName>( cdataSections );
 
            // Read-only setting is always false on clones
            clonedSettings.isReadOnly = false;

            return clonedSettings; 
        }
 
 
//
// Internal and private methods 
//
        internal bool ReadOnly {
            get {
                return isReadOnly; 
            }
            set { 
                isReadOnly = value; 
            }
        } 

        // Specifies the method (Html, Xml, etc.) that will be used to serialize the result tree.
        public XmlOutputMethod OutputMethod {
            get { 
                return outputMethod;
            } 
            internal set { 
                outputMethod = value;
            } 
        }

        // Set of XmlQualifiedNames that identify any elements that need to have text children wrapped in CData sections.
        internal List<XmlQualifiedName> CDataSectionElements { 
            get {
                Debug.Assert(cdataSections != null); 
                return cdataSections; 
            }
        } 

        internal bool MergeCDataSections {
            get {
                return mergeCDataSections; 
            }
            set { 
                CheckReadOnly( "MergeCDataSections" ); 
                mergeCDataSections = value;
            } 
        }

        // Used in Html writer when writing Meta element.  Null denotes the default media type.
        internal string MediaType { 
            get {
                return mediaType; 
            } 
            set {
                CheckReadOnly( "MediaType" ); 
                mediaType = value;
            }
        }
 
        // System Id in doc-type declaration.  Null denotes the absence of the system Id.
        internal string DocTypeSystem { 
            get { 
                return docTypeSystem;
            } 
            set {
                CheckReadOnly( "DocTypeSystem" );
                docTypeSystem = value;
            } 
        }
 
        // Public Id in doc-type declaration.  Null denotes the absence of the public Id. 
        internal string DocTypePublic {
            get { 
                return docTypePublic;
            }
            set {
                CheckReadOnly( "DocTypePublic" ); 
                docTypePublic = value;
            } 
        } 

        // Yes for standalone="yes", No for standalone="no", and Omit for no standalone. 
        internal XmlStandalone Standalone {
            get {
                return standalone;
            } 
            set {
                CheckReadOnly( "Standalone" ); 
                standalone = value; 
            }
        } 

        // True if an xml declaration should automatically be output (no need to call WriteStartDocument)
        internal bool AutoXmlDeclaration {
            get { 
                return autoXmlDecl;
            } 
            set { 
                CheckReadOnly( "AutoXmlDeclaration" );
                autoXmlDecl = value; 
            }
        }

        // If TriState.Unknown, then Indent property was not explicitly set.  In this case, the AutoDetect output 
        // method will default to Indent=true for Html and Indent=false for Xml.
        internal TriState InternalIndent { 
            get { 
                return indent;
            } 
        }

        internal bool IsQuerySpecific {
            get { 
                return cdataSections.Count != 0 || docTypePublic != null ||
                       docTypeSystem != null || standalone == XmlStandalone.Yes; 
            } 
        }
 
        private void CheckReadOnly( string propertyName ) {
            if ( isReadOnly ) {
                throw new XmlException( Res.Xml_ReadOnlyProperty, "XmlWriterSettings." + propertyName );
            } 
        }
 
#if !HIDE_XSL 
        /// <summary>
        /// Serialize the object to BinaryWriter. 
        /// </summary>
        internal void GetObjectData(XmlQueryDataWriter writer) {
            // Encoding encoding;
            // NOTE: For Encoding we serialize only CodePage, and ignore EncoderFallback/DecoderFallback. 
            // It suffices for XSLT purposes, but not in the general case.
            Debug.Assert(encoding.Equals(Encoding.GetEncoding(encoding.CodePage)), "Cannot serialize encoding correctly"); 
            writer.Write(encoding.CodePage); 
            // bool omitXmlDecl;
            writer.Write(omitXmlDecl); 
            // NewLineHandling newLineHandling;
            writer.Write((sbyte)newLineHandling);
            // string newLineChars;
            writer.WriteStringQ(newLineChars); 
            // TriState indent;
            writer.Write((sbyte)indent); 
            // string indentChars; 
            writer.WriteStringQ(indentChars);
            // bool newLineOnAttributes; 
            writer.Write(newLineOnAttributes);
            // bool closeOutput;
            writer.Write(closeOutput);
            // ConformanceLevel conformanceLevel; 
            writer.Write((sbyte)conformanceLevel);
            // bool checkCharacters; 
            writer.Write(checkCharacters); 
            // XmlOutputMethod outputMethod;
            writer.Write((sbyte)outputMethod); 
            // List<XmlQualifiedName> cdataSections;
            writer.Write(cdataSections.Count);
            foreach (XmlQualifiedName qname in cdataSections) {
                writer.Write(qname.Name); 
                writer.Write(qname.Namespace);
            } 
            // bool mergeCDataSections; 
            writer.Write(mergeCDataSections);
            // string mediaType; 
            writer.WriteStringQ(mediaType);
            // string docTypeSystem;
            writer.WriteStringQ(docTypeSystem);
            // string docTypePublic; 
            writer.WriteStringQ(docTypePublic);
            // XmlStandalone standalone; 
            writer.Write((sbyte)standalone); 
            // bool autoXmlDecl;
            writer.Write(autoXmlDecl); 
            // bool isReadOnly;
            writer.Write(isReadOnly);
        }
 
        /// <summary>
        /// Deserialize the object from BinaryReader. 
        /// </summary> 
        internal XmlWriterSettings(XmlQueryDataReader reader) {
            // Encoding encoding; 
            encoding = Encoding.GetEncoding(reader.ReadInt32());
            // bool omitXmlDecl;
            omitXmlDecl = reader.ReadBoolean();
            // NewLineHandling newLineHandling; 
            newLineHandling = (NewLineHandling)reader.ReadSByte(0, (sbyte)NewLineHandling.None);
            // string newLineChars; 
            newLineChars = reader.ReadStringQ(); 
            // TriState indent;
            indent = (TriState)reader.ReadSByte((sbyte)TriState.Unknown, (sbyte)TriState.True); 
            // string indentChars;
            indentChars = reader.ReadStringQ();
            // bool newLineOnAttributes;
            newLineOnAttributes = reader.ReadBoolean(); 
            // bool closeOutput;
            closeOutput = reader.ReadBoolean(); 
            // ConformanceLevel conformanceLevel; 
            conformanceLevel = (ConformanceLevel)reader.ReadSByte(0, (sbyte)ConformanceLevel.Document);
            // bool checkCharacters; 
            checkCharacters = reader.ReadBoolean();
            // XmlOutputMethod outputMethod;
            outputMethod = (XmlOutputMethod)reader.ReadSByte(0, (sbyte)XmlOutputMethod.AutoDetect);
            // List<XmlQualifiedName> cdataSections; 
            int length = reader.ReadInt32();
            cdataSections = new List<XmlQualifiedName>(length); 
            for (int idx = 0; idx < length; idx++) { 
                cdataSections.Add(new XmlQualifiedName(reader.ReadString(), reader.ReadString()));
            } 
            // bool mergeCDataSections;
            mergeCDataSections = reader.ReadBoolean();
            // string mediaType;
            mediaType = reader.ReadStringQ(); 
            // string docTypeSystem;
            docTypeSystem = reader.ReadStringQ(); 
            // string docTypePublic; 
            docTypePublic = reader.ReadStringQ();
            // XmlStandalone standalone; 
            Standalone = (XmlStandalone)reader.ReadSByte(0, (sbyte)XmlStandalone.No);
            // bool autoXmlDecl;
            autoXmlDecl = reader.ReadBoolean();
            // bool isReadOnly; 
            isReadOnly = reader.ReadBoolean();
        } 
#else 
        internal void GetObjectData(object writer) { }
        internal XmlWriterSettings(object reader) { } 
#endif
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
