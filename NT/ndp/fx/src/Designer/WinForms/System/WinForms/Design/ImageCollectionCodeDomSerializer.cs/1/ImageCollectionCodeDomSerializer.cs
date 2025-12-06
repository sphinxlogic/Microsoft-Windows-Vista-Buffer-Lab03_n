namespace  System.Windows.Forms.Design { 

    using System.Design;
    using System.CodeDom;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Diagnostics; 
    using System.Reflection;
    using System.ComponentModel.Design.Serialization; 
    using System.Collections.Specialized;
    using System.Windows.Forms;

 
    /// <include file='doc\StringDictionaryCodeDomSerializer.uex' path='docs/doc[@for="StringDictionaryDomSerializer"]/*' />
    /// <devdoc> 
    ///     This serializer serializes string dictionaries. 
    /// </devdoc>
 
    //
    public class ImageListCodeDomSerializer : CodeDomSerializer {

        /// <include file='doc\ImageCollectionCodeDomSerializer.uex' path='docs/doc[@for="StringDictionaryCodeDomSerializer.Deserialize"]/*' /> 
        /// <devdoc>
        ///     This method takes a CodeDomObject and deserializes into a real object. 
        ///     We don't do anything here. 
        /// </devdoc>
        public override object Deserialize(IDesignerSerializationManager manager, object codeObject) { 

            //

            if (manager == null || codeObject == null) { 
                throw new ArgumentNullException( manager == null ? "manager" : "codeObject");
            } 
 
            // Find our base class's serializer.
            // 
            CodeDomSerializer serializer = (CodeDomSerializer)manager.GetSerializer(typeof(Component), typeof(CodeDomSerializer));
            if (serializer == null) {
                Debug.Fail("Unable to find a CodeDom serializer for 'Component'.  Has someone tampered with the serialization providers?");
                return null; 
            }
 
            return serializer.Deserialize(manager, codeObject); 
        }
 
        /// <include file='doc\ImageCollectionCodeDomSerializer.uex' path='docs/doc[@for="StringDictionaryCodeDomSerializer.Serialize"]/*' />
        /// <devdoc>
        ///     Serializes the given object into a CodeDom object.
        /// </devdoc> 
        public override object Serialize(IDesignerSerializationManager manager, object value) {
 
            CodeDomSerializer baseSerializer = (CodeDomSerializer)manager.GetSerializer(typeof(ImageList).BaseType, typeof(CodeDomSerializer)); 

            object codeObject = baseSerializer.Serialize(manager, value); 

            ImageList imageList = value as ImageList;
            if (imageList != null) {
                StringCollection imageKeys = imageList.Images.Keys; 

                if (codeObject is CodeStatementCollection) { 
                    CodeExpression imageListObject = GetExpression(manager, value); 
                    if (imageListObject != null) {
                        CodeExpression imageListImagesProperty = new CodePropertyReferenceExpression(imageListObject, "Images"); 

                        if (imageListImagesProperty != null) {
                            for (int i = 0; i < imageKeys.Count; i++) {
                                if ((imageKeys[i] != null) || (imageKeys[i].Length != 0)){ 
                                    CodeMethodInvokeExpression setNameMethodCall = new CodeMethodInvokeExpression(imageListImagesProperty, "SetKeyName",
                                                                                   new CodeExpression [] { 
                                                                                            new CodePrimitiveExpression(i),         // SetKeyName(int, 
                                                                                            new CodePrimitiveExpression(imageKeys[i])        // string);
                                                                                            }); 

                                    ((CodeStatementCollection)codeObject).Add(setNameMethodCall);
                                }
                             } 
                        }
                    } 
                } 
            }
            return codeObject; 

        }
    } // Class ImageListCodeDomSerializer
 } // Namespace System.Windows.Forms.Design 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace  System.Windows.Forms.Design { 

    using System.Design;
    using System.CodeDom;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Diagnostics; 
    using System.Reflection;
    using System.ComponentModel.Design.Serialization; 
    using System.Collections.Specialized;
    using System.Windows.Forms;

 
    /// <include file='doc\StringDictionaryCodeDomSerializer.uex' path='docs/doc[@for="StringDictionaryDomSerializer"]/*' />
    /// <devdoc> 
    ///     This serializer serializes string dictionaries. 
    /// </devdoc>
 
    //
    public class ImageListCodeDomSerializer : CodeDomSerializer {

        /// <include file='doc\ImageCollectionCodeDomSerializer.uex' path='docs/doc[@for="StringDictionaryCodeDomSerializer.Deserialize"]/*' /> 
        /// <devdoc>
        ///     This method takes a CodeDomObject and deserializes into a real object. 
        ///     We don't do anything here. 
        /// </devdoc>
        public override object Deserialize(IDesignerSerializationManager manager, object codeObject) { 

            //

            if (manager == null || codeObject == null) { 
                throw new ArgumentNullException( manager == null ? "manager" : "codeObject");
            } 
 
            // Find our base class's serializer.
            // 
            CodeDomSerializer serializer = (CodeDomSerializer)manager.GetSerializer(typeof(Component), typeof(CodeDomSerializer));
            if (serializer == null) {
                Debug.Fail("Unable to find a CodeDom serializer for 'Component'.  Has someone tampered with the serialization providers?");
                return null; 
            }
 
            return serializer.Deserialize(manager, codeObject); 
        }
 
        /// <include file='doc\ImageCollectionCodeDomSerializer.uex' path='docs/doc[@for="StringDictionaryCodeDomSerializer.Serialize"]/*' />
        /// <devdoc>
        ///     Serializes the given object into a CodeDom object.
        /// </devdoc> 
        public override object Serialize(IDesignerSerializationManager manager, object value) {
 
            CodeDomSerializer baseSerializer = (CodeDomSerializer)manager.GetSerializer(typeof(ImageList).BaseType, typeof(CodeDomSerializer)); 

            object codeObject = baseSerializer.Serialize(manager, value); 

            ImageList imageList = value as ImageList;
            if (imageList != null) {
                StringCollection imageKeys = imageList.Images.Keys; 

                if (codeObject is CodeStatementCollection) { 
                    CodeExpression imageListObject = GetExpression(manager, value); 
                    if (imageListObject != null) {
                        CodeExpression imageListImagesProperty = new CodePropertyReferenceExpression(imageListObject, "Images"); 

                        if (imageListImagesProperty != null) {
                            for (int i = 0; i < imageKeys.Count; i++) {
                                if ((imageKeys[i] != null) || (imageKeys[i].Length != 0)){ 
                                    CodeMethodInvokeExpression setNameMethodCall = new CodeMethodInvokeExpression(imageListImagesProperty, "SetKeyName",
                                                                                   new CodeExpression [] { 
                                                                                            new CodePrimitiveExpression(i),         // SetKeyName(int, 
                                                                                            new CodePrimitiveExpression(imageKeys[i])        // string);
                                                                                            }); 

                                    ((CodeStatementCollection)codeObject).Add(setNameMethodCall);
                                }
                             } 
                        }
                    } 
                } 
            }
            return codeObject; 

        }
    } // Class ImageListCodeDomSerializer
 } // Namespace System.Windows.Forms.Design 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
