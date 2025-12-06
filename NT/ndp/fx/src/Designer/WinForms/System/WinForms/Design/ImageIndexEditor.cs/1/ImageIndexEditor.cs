//------------------------------------------------------------------------------ 
// <copyright file="ImageIndexEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ImageIndexEditor..ctor()")] 
 
namespace System.Windows.Forms.Design {
    using System.Runtime.Serialization.Formatters; 
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using System.Windows.Forms;
    using System.Drawing; 
    using System.Drawing.Design; 

    using System.ComponentModel.Design; 
    using Microsoft.Win32;
    using System.Windows.Forms.Design;
    using System.Windows.Forms.ComponentModel;
 
    /// <include file='doc\ImageIndexEditor.uex' path='docs/doc[@for="ImageIndexEditor"]/*' />
    /// <devdoc> 
    ///    <para> Provides an editor for visually picking an image index.</para> 
    /// </devdoc>
    internal class ImageIndexEditor : UITypeEditor { 
        protected ImageList          currentImageList;
        protected PropertyDescriptor currentImageListProp;
        protected object             currentInstance;
        protected UITypeEditor       imageEditor; 
        protected string             parentImageListProperty = "Parent";
        protected string             imageListPropertyName = null; 
 
        /// <include file='doc\ImageIndexEditor.uex' path='docs/doc[@for="ImageIndexEditor.ImageIndexEditor"]/*' />
        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Windows.Forms.Design.ImageIndexEditor'/> class.</para>
        /// </devdoc>
        public ImageIndexEditor() {
            // Get the type editor for images.  We use the properties on 
            // this to determine if we support value painting, etc.
            // 
            imageEditor = (UITypeEditor)TypeDescriptor.GetEditor(typeof(Image), typeof(UITypeEditor)); 
        }
 

        internal UITypeEditor ImageEditor {
            get { return imageEditor; }
        } 

        internal string ParentImageListProperty { 
            get { 
                return parentImageListProperty;
            } 
        }


        /// <include file='doc\ImageIndexEditor.uex' path='docs/doc[@for="ImageIndexEditor.GetImage"]/*' /> 
        /// <devdoc>
        ///      Retrieves an image for the current context at current index. 
        /// </devdoc> 
        protected virtual Image GetImage(ITypeDescriptorContext context, int index, string key, bool useIntIndex) {
            Image image = null; 
            object instance = context.Instance;

            if(instance is object[]) { // we would not know what to do in this case anyway (i.e. multiple selection of objects)
                return null; 
            }
 
            // If the instances are different, then we need to re-aquire our image list. 
            //
            if ((index >= 0) || (key != null)) { 
                if (currentImageList == null ||
                    instance != currentInstance ||
                    (currentImageListProp != null && (ImageList)currentImageListProp.GetValue(currentInstance) != currentImageList)) {
 
                    currentInstance = instance;
                    // first look for an attribute 
                    PropertyDescriptor imageListProp = ImageListUtils.GetImageListProperty(context.PropertyDescriptor, ref instance); 

                    // not found as an attribute, do the old behavior 
                    while(instance != null && imageListProp == null) {
                        PropertyDescriptorCollection props = TypeDescriptor.GetProperties(instance);

                        foreach (PropertyDescriptor prop in props) { 
                            if (typeof(ImageList).IsAssignableFrom(prop.PropertyType)) {
                                imageListProp = prop; 
                                break; 
                            }
                        } 

                        if (imageListProp == null) {

                            // We didn't find the image list in this component.  See if the 
                            // component has a "parent" property.  If so, walk the tree...
                            // 
 
                            PropertyDescriptor parentProp = props[ParentImageListProperty];
                            if (parentProp != null) { 
                                instance = parentProp.GetValue(instance);
                            }
                            else {
                                // Stick a fork in us, we're done. 
                                //
                                instance = null; 
                            } 
                        }
                    } 

                    if (imageListProp != null) {
                        currentImageList = (ImageList)imageListProp.GetValue(instance);
                        currentImageListProp = imageListProp; 
                        currentInstance = instance;
                    } 
                } 

                if (currentImageList != null) { 
                    if (useIntIndex) {
                        if (currentImageList != null && index < currentImageList.Images.Count) {
                            index = (index > 0) ? index : 0;
                            image = currentImageList.Images[index]; 
                        }
                    } 
                    else { 
                        image = currentImageList.Images[key];
                    } 
                }
                else {
                    // no image list, no image
                    image = null; 
                }
            } 
 
            return image;
        } 

        /// <include file='doc\ImageIndexEditor.uex' path='docs/doc[@for="ImageIndexEditor.GetPaintValueSupported"]/*' />
        /// <devdoc>
        ///    <para>Gets a value indicating whether this editor supports the painting of a representation 
        ///       of an object's value.</para>
        /// </devdoc> 
        public override bool GetPaintValueSupported(ITypeDescriptorContext context) { 
            if (imageEditor != null) {
                return imageEditor.GetPaintValueSupported(context); 
            }

            return false;
        } 

        /// <include file='doc\ImageIndexEditor.uex' path='docs/doc[@for="ImageIndexEditor.PaintValue"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Paints a representative value of the given object to the provided 
        ///       canvas. Painting should be done within the boundaries of the
        ///       provided rectangle.
        ///    </para>
        /// </devdoc> 
        public override void PaintValue(PaintValueEventArgs e) {
 
            if (ImageEditor != null){ 

                Image image = null; 

                if (e.Value is int) {
                   image = GetImage(e.Context, (int)e.Value, null, true);
                } 
                else if (e.Value is string) {
                   image = GetImage(e.Context, -1, (string)e.Value, false); 
                } 

                if (image != null) { 
                    ImageEditor.PaintValue(new PaintValueEventArgs(e.Context, image, e.Graphics, e.Bounds));
                }
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ImageIndexEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ImageIndexEditor..ctor()")] 
 
namespace System.Windows.Forms.Design {
    using System.Runtime.Serialization.Formatters; 
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using System.Windows.Forms;
    using System.Drawing; 
    using System.Drawing.Design; 

    using System.ComponentModel.Design; 
    using Microsoft.Win32;
    using System.Windows.Forms.Design;
    using System.Windows.Forms.ComponentModel;
 
    /// <include file='doc\ImageIndexEditor.uex' path='docs/doc[@for="ImageIndexEditor"]/*' />
    /// <devdoc> 
    ///    <para> Provides an editor for visually picking an image index.</para> 
    /// </devdoc>
    internal class ImageIndexEditor : UITypeEditor { 
        protected ImageList          currentImageList;
        protected PropertyDescriptor currentImageListProp;
        protected object             currentInstance;
        protected UITypeEditor       imageEditor; 
        protected string             parentImageListProperty = "Parent";
        protected string             imageListPropertyName = null; 
 
        /// <include file='doc\ImageIndexEditor.uex' path='docs/doc[@for="ImageIndexEditor.ImageIndexEditor"]/*' />
        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Windows.Forms.Design.ImageIndexEditor'/> class.</para>
        /// </devdoc>
        public ImageIndexEditor() {
            // Get the type editor for images.  We use the properties on 
            // this to determine if we support value painting, etc.
            // 
            imageEditor = (UITypeEditor)TypeDescriptor.GetEditor(typeof(Image), typeof(UITypeEditor)); 
        }
 

        internal UITypeEditor ImageEditor {
            get { return imageEditor; }
        } 

        internal string ParentImageListProperty { 
            get { 
                return parentImageListProperty;
            } 
        }


        /// <include file='doc\ImageIndexEditor.uex' path='docs/doc[@for="ImageIndexEditor.GetImage"]/*' /> 
        /// <devdoc>
        ///      Retrieves an image for the current context at current index. 
        /// </devdoc> 
        protected virtual Image GetImage(ITypeDescriptorContext context, int index, string key, bool useIntIndex) {
            Image image = null; 
            object instance = context.Instance;

            if(instance is object[]) { // we would not know what to do in this case anyway (i.e. multiple selection of objects)
                return null; 
            }
 
            // If the instances are different, then we need to re-aquire our image list. 
            //
            if ((index >= 0) || (key != null)) { 
                if (currentImageList == null ||
                    instance != currentInstance ||
                    (currentImageListProp != null && (ImageList)currentImageListProp.GetValue(currentInstance) != currentImageList)) {
 
                    currentInstance = instance;
                    // first look for an attribute 
                    PropertyDescriptor imageListProp = ImageListUtils.GetImageListProperty(context.PropertyDescriptor, ref instance); 

                    // not found as an attribute, do the old behavior 
                    while(instance != null && imageListProp == null) {
                        PropertyDescriptorCollection props = TypeDescriptor.GetProperties(instance);

                        foreach (PropertyDescriptor prop in props) { 
                            if (typeof(ImageList).IsAssignableFrom(prop.PropertyType)) {
                                imageListProp = prop; 
                                break; 
                            }
                        } 

                        if (imageListProp == null) {

                            // We didn't find the image list in this component.  See if the 
                            // component has a "parent" property.  If so, walk the tree...
                            // 
 
                            PropertyDescriptor parentProp = props[ParentImageListProperty];
                            if (parentProp != null) { 
                                instance = parentProp.GetValue(instance);
                            }
                            else {
                                // Stick a fork in us, we're done. 
                                //
                                instance = null; 
                            } 
                        }
                    } 

                    if (imageListProp != null) {
                        currentImageList = (ImageList)imageListProp.GetValue(instance);
                        currentImageListProp = imageListProp; 
                        currentInstance = instance;
                    } 
                } 

                if (currentImageList != null) { 
                    if (useIntIndex) {
                        if (currentImageList != null && index < currentImageList.Images.Count) {
                            index = (index > 0) ? index : 0;
                            image = currentImageList.Images[index]; 
                        }
                    } 
                    else { 
                        image = currentImageList.Images[key];
                    } 
                }
                else {
                    // no image list, no image
                    image = null; 
                }
            } 
 
            return image;
        } 

        /// <include file='doc\ImageIndexEditor.uex' path='docs/doc[@for="ImageIndexEditor.GetPaintValueSupported"]/*' />
        /// <devdoc>
        ///    <para>Gets a value indicating whether this editor supports the painting of a representation 
        ///       of an object's value.</para>
        /// </devdoc> 
        public override bool GetPaintValueSupported(ITypeDescriptorContext context) { 
            if (imageEditor != null) {
                return imageEditor.GetPaintValueSupported(context); 
            }

            return false;
        } 

        /// <include file='doc\ImageIndexEditor.uex' path='docs/doc[@for="ImageIndexEditor.PaintValue"]/*' /> 
        /// <devdoc> 
        ///    <para>
        ///       Paints a representative value of the given object to the provided 
        ///       canvas. Painting should be done within the boundaries of the
        ///       provided rectangle.
        ///    </para>
        /// </devdoc> 
        public override void PaintValue(PaintValueEventArgs e) {
 
            if (ImageEditor != null){ 

                Image image = null; 

                if (e.Value is int) {
                   image = GetImage(e.Context, (int)e.Value, null, true);
                } 
                else if (e.Value is string) {
                   image = GetImage(e.Context, -1, (string)e.Value, false); 
                } 

                if (image != null) { 
                    ImageEditor.PaintValue(new PaintValueEventArgs(e.Context, image, e.Graphics, e.Bounds));
                }
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
