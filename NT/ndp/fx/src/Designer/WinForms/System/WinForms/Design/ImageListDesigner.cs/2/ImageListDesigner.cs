//------------------------------------------------------------------------------ 
// <copyright file="ImageListDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Runtime.InteropServices; 
    using System.ComponentModel;
    using System.Diagnostics;
    using System;
    using System.Design; 
    using System.Collections;
    using System.Drawing.Design; 
    using System.Collections.Specialized; 
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Drawing;
    using Microsoft.Win32;
    using Timer = System.Windows.Forms.Timer;
    using System.Globalization; 

    /// <include file='doc\ImageListDesigner.uex' path='docs/doc[@for="ImageListDesigner"]/*' /> 
    /// <devdoc> 
    /// <para>Provides design-time functionality for <see cref='System.Windows.Forms.ImageList'/>.</para>
    /// </devdoc> 
    internal class ImageListDesigner : ComponentDesigner {
        // The designer keeps a backup copy of all the images in the image list.  Unlike the image list,
        // we don't lose any information about size and color depth.
        private OriginalImageCollection originalImageCollection; 
        private DesignerActionListCollection _actionLists;
 
 
        /// <include file='doc\ImageListDesigner.uex' path='docs/doc[@for="ImageListDesigner.ColorDepth"]/*' />
        /// <devdoc> 
        ///    <para>Accessor method for the ColorDepth property on ImageList. We shadow
        ///       this property at design time.</para>
        /// </devdoc>
        private ColorDepth ColorDepth { 
            get {
                return ImageList.ColorDepth; 
            } 
            set {
                ImageList.Images.Clear(); 
                ImageList.ColorDepth = value;
                Images.PopulateHandle();
            }
        } 

        private bool ShouldSerializeColorDepth() { 
            return (Images.Count == 0); 
        }
 
        /// <include file='doc\ImageListDesigner.uex' path='docs/doc[@for="ImageListDesigner.Images"]/*' />
        /// <devdoc>
        ///    <para>Accessor method for the Images property on ImageList. We shadow
        ///       this property at design time.</para> 
        /// </devdoc>
        private OriginalImageCollection Images { 
            get { 
                if (originalImageCollection == null)
                    originalImageCollection = new OriginalImageCollection(this); 
                return originalImageCollection;
            }
        }
 
        internal ImageList ImageList {
            get { 
                return(ImageList) Component; 
            }
        } 

        /// <include file='doc\ImageListDesigner.uex' path='docs/doc[@for="ImageListDesigner.ImageSize"]/*' />
        /// <devdoc>
        ///    <para>Accessor method for the ImageSize property on ImageList. We shadow 
        ///       this property at design time.</para>
        /// </devdoc> 
        private Size ImageSize { 
            get {
                return ImageList.ImageSize; 
            }
            set {
                ImageList.Images.Clear();
                ImageList.ImageSize = value; 
                Images.PopulateHandle();
            } 
        } 

        private bool ShouldSerializeImageSize() { 
            return (Images.Count == 0);
        }

 
        private Color TransparentColor {
            get { 
                return ImageList.TransparentColor; 
            }
            set { 
                ImageList.Images.Clear();
                ImageList.TransparentColor = value;
                Images.PopulateHandle();
            } 
        }
 
        private bool ShouldSerializeTransparentColor() { 
            return !TransparentColor.Equals(Color.LightGray);
        } 


        /// <include file='doc\ImageListDesigner.uex' path='docs/doc[@for="ImageListDesigner.ImageStream"]/*' />
        /// <devdoc> 
        ///    <para>Accessor method for the ImageStream property on ImageList. We shadow
        ///       this property at design time.</para> 
        /// </devdoc> 
        private ImageListStreamer ImageStream {
            get { 
                return ImageList.ImageStream;
            }
            set {
                ImageList.ImageStream = value; 
                Images.ReloadFromImageList();
            } 
        } 

 
        /// <include file='doc\ImageListDesigner.uex' path='docs/doc[@for="ImageListDesigner.PreFilterProperties"]/*' />
        /// <devdoc>
        ///    <para>Provides an opportunity for the designer to filter the properties.</para>
        /// </devdoc> 
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties); 
 
            // Handle shadowed properties
            // 
            string[] shadowProps = new string[] {
                "ColorDepth",
                "ImageSize",
                "ImageStream", 
                "TransparentColor"
            }; 
 
            Attribute[] empty = new Attribute[0];
 
            for (int i = 0; i < shadowProps.Length; i++) {
                PropertyDescriptor prop = (PropertyDescriptor)properties[shadowProps[i]];
                if (prop != null) {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(ImageListDesigner), prop, empty); 
                }
            } 
 
            // replace this one seperately because it is of a different type (OriginalImageCollection) than
            // the original property (ImageCollection) 
            //
            PropertyDescriptor imageProp = (PropertyDescriptor)properties["Images"];

            if (imageProp != null) { 
                Attribute[] attrs = new Attribute[imageProp.Attributes.Count];
                imageProp.Attributes.CopyTo(attrs, 0); 
                properties["Images"] = TypeDescriptor.CreateProperty(typeof(ImageListDesigner), "Images", typeof(OriginalImageCollection), attrs); 
            }
 


        }
 

        public override DesignerActionListCollection ActionLists { 
            get { 
                if (_actionLists == null) {
                    _actionLists = new DesignerActionListCollection(); 
                    _actionLists.Add(new ImageListActionList(this));
                }
                return _actionLists;
            } 
        }
 
        //  Shadow ImageList.Images to allow arbitrary handle recreation. 
        [
           Editor("System.Windows.Forms.Design.ImageCollectionEditor, " + AssemblyRef.SystemDesign, typeof(System.Drawing.Design.UITypeEditor)) 
        ]
        internal class OriginalImageCollection : IList {
            private ImageListDesigner owner;
            private IList list = new ArrayList(); 

            internal OriginalImageCollection(ImageListDesigner owner) { 
                this.owner = owner; 
                // just in case it's got images
                ReloadFromImageList(); 
            }

            private void AssertInvariant() {
                Debug.Assert(owner != null, "OriginalImageCollection has no owner (ImageListDesigner)"); 
                Debug.Assert(list != null, "OriginalImageCollection has no list (ImageListDesigner)");
            } 
 
            public int Count {
                get { 
                    AssertInvariant();
                    return list.Count;
                }
            } 

            public bool IsReadOnly { 
                get { 
                    return false;
                } 
            }

            bool IList.IsFixedSize {
                get { 
                    return false;
                } 
            } 

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] 
            public ImageListImage this[int index] {
                get {
                    if (index < 0 || index >= Count)
                        throw new ArgumentOutOfRangeException(SR.GetString(SR.InvalidArgument, 
                                                                  "index",
                                                                  index.ToString(CultureInfo.CurrentCulture))); 
                    return(ImageListImage) list[index]; 
                }
                set { 
                    if (index < 0 || index >= Count)
                        throw new ArgumentOutOfRangeException(SR.GetString(SR.InvalidArgument,
                                                                  "index",
                                                                  index.ToString(CultureInfo.CurrentCulture))); 

                    if (value == null) 
                        throw new ArgumentException(SR.GetString(SR.InvalidArgument, 
                                                                  "value",
                                                                  "null")); 

                    AssertInvariant();
                    list[index] = value;
                    RecreateHandle(); 
                }
 
            } 

            object IList.this[int index] { 
                get {
                    return this[index];
                }
                set { 
                    if (value is ImageListImage) {
                        this[index] = (ImageListImage)value; 
                    } 
                    else {
                        throw new ArgumentException(SR.GetString(SR.ImageListDesignerBadImageListImage,"value")); 
                    }

                }
            } 

            public void SetKeyName(int index, string name) { 
                this[index].Name = name; 
                owner.ImageList.Images.SetKeyName(index, name);
            } 
            /// <include file='doc\ImageListDesigner.uex' path='docs/doc[@for="ImageListDesigner.OriginalImageCollection.Add"]/*' />
            /// <devdoc>
            ///     Add the given image to the ImageList.
            /// </devdoc> 
            public int Add(ImageListImage value) {
 
                int index = list.Add(value); 
                if (value.Name != null) {
                    owner.ImageList.Images.Add(value.Name, value.Image); 
                }
                else {
                     owner.ImageList.Images.Add(value.Image);
                } 
                return index;
            } 
 
            public void AddRange(ImageListImage[] values) {
                if (values == null) { 
                    throw new ArgumentNullException("values");
                }
                foreach(ImageListImage value in values) {
                    if (value != null) { 
                        Add(value);
                    } 
                } 
            }
 
            int IList.Add(object value) {
               if (value is ImageListImage) {
                    return Add((ImageListImage)value);
                } 
                else {
                    throw new ArgumentException(SR.GetString(SR.ImageListDesignerBadImageListImage,"value")); 
                } 
            }
 
            // Called when reloading the form.  In this case, we have no "originals" list,
            // so we make one out of the image list.
            internal void ReloadFromImageList() {
                list.Clear(); 
                StringCollection imageKeys = owner.ImageList.Images.Keys;
                for(int i = 0; i < owner.ImageList.Images.Count; i++) { 
                    list.Add(new ImageListImage((Bitmap)owner.ImageList.Images[i], imageKeys[i])); 
                }
            } 

            /// <include file='doc\ImageListDesigner.uex' path='docs/doc[@for="ImageListDesigner.OriginalImageCollection.Clear"]/*' />
            /// <devdoc>
            ///     Remove all images and masks from the ImageList. 
            /// </devdoc>
            public void Clear() { 
                AssertInvariant(); 
                list.Clear();
                owner.ImageList.Images.Clear(); 
            }
            public bool Contains(ImageListImage value) {
                return list.Contains(value.Image);
            } 

            bool IList.Contains(object value) { 
                if (value is ImageListImage) { 
                    return Contains((ImageListImage)value);
                } 
                else {
                    return false;
                }
            } 

            public IEnumerator GetEnumerator() { 
                return list.GetEnumerator(); 
            }
 
            public int IndexOf(Image value) {
                return list.IndexOf(value);
            }
 
            int IList.IndexOf(object value) {
                if (value is Image) { 
                    return IndexOf((Image)value); 
                }
                else { 
                    return -1;
                }
            }
 
            void IList.Insert(int index, object value) {
                throw new NotSupportedException(); 
            } 

            internal void PopulateHandle() { 
                for (int i = 0; i < list.Count; i++) {
                    ImageListImage imageListImage = (ImageListImage) list[i];
                    owner.ImageList.Images.Add(imageListImage.Name, imageListImage.Image);
                } 
            }
 
            private void RecreateHandle() { 
                owner.ImageList.Images.Clear();
                PopulateHandle(); 
            }

            public void Remove(Image value) {
                AssertInvariant(); 
                list.Remove(value);
                RecreateHandle(); 
            } 

            void IList.Remove(object value) { 
                if (value is Image) {
                    Remove((Image)value);
                }
            } 

            public void RemoveAt(int index) { 
                if (index < 0 || index >= Count) 
                    throw new ArgumentOutOfRangeException(SR.GetString(SR.InvalidArgument,
                                                              "index", 
                                                              index.ToString(CultureInfo.CurrentCulture)));

                AssertInvariant();
                list.RemoveAt(index); 
                RecreateHandle();
            } 
 
            int ICollection.Count {
                get { 
                    return Count;
                }
            }
 
            bool ICollection.IsSynchronized {
                get { 
                    return false; 
                }
            } 

            object ICollection.SyncRoot {
                get {
                    return null; 
                }
            } 
 
            void ICollection.CopyTo(Array array, int index) {
                list.CopyTo(array, index); 
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator(); 
            }
        } // end class OriginalImageCollection 
    } 

    internal class ImageListActionList : DesignerActionList { 
        private ImageListDesigner _designer;

        public ImageListActionList(ImageListDesigner designer) : base(designer.Component) {
            _designer = designer; 
        }
 
        public void ChooseImages() { 
            EditorServiceContext.EditValue(_designer, Component, "Images");
        } 

        public ColorDepth ColorDepth {
            get {
                return ((ImageList)Component).ColorDepth; 
            }
            set { 
                TypeDescriptor.GetProperties(Component)["ColorDepth"].SetValue(Component, value); 
            }
        } 

        public Size ImageSize {
            get {
                return ((ImageList)Component).ImageSize; 
            }
            set { 
                TypeDescriptor.GetProperties(Component)["ImageSize"].SetValue(Component, value); 
            }
        } 

        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = new DesignerActionItemCollection();
            items.Add(new DesignerActionPropertyItem("ImageSize", 
                SR.GetString(SR.ImageListActionListImageSizeDisplayName),
                SR.GetString(SR.PropertiesCategoryName), 
                SR.GetString(SR.ImageListActionListImageSizeDescription))); 
            items.Add(new DesignerActionPropertyItem("ColorDepth",
                SR.GetString(SR.ImageListActionListColorDepthDisplayName), 
                SR.GetString(SR.PropertiesCategoryName),
                SR.GetString(SR.ImageListActionListColorDepthDescription)));

            items.Add(new DesignerActionMethodItem(this, 
                "ChooseImages",
                SR.GetString(SR.ImageListActionListChooseImagesDisplayName), 
                SR.GetString(SR.LinksCategoryName), 
                SR.GetString(SR.ImageListActionListChooseImagesDescription), true));
            return items; 
        }
 	}
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ImageListDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Runtime.InteropServices; 
    using System.ComponentModel;
    using System.Diagnostics;
    using System;
    using System.Design; 
    using System.Collections;
    using System.Drawing.Design; 
    using System.Collections.Specialized; 
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Drawing;
    using Microsoft.Win32;
    using Timer = System.Windows.Forms.Timer;
    using System.Globalization; 

    /// <include file='doc\ImageListDesigner.uex' path='docs/doc[@for="ImageListDesigner"]/*' /> 
    /// <devdoc> 
    /// <para>Provides design-time functionality for <see cref='System.Windows.Forms.ImageList'/>.</para>
    /// </devdoc> 
    internal class ImageListDesigner : ComponentDesigner {
        // The designer keeps a backup copy of all the images in the image list.  Unlike the image list,
        // we don't lose any information about size and color depth.
        private OriginalImageCollection originalImageCollection; 
        private DesignerActionListCollection _actionLists;
 
 
        /// <include file='doc\ImageListDesigner.uex' path='docs/doc[@for="ImageListDesigner.ColorDepth"]/*' />
        /// <devdoc> 
        ///    <para>Accessor method for the ColorDepth property on ImageList. We shadow
        ///       this property at design time.</para>
        /// </devdoc>
        private ColorDepth ColorDepth { 
            get {
                return ImageList.ColorDepth; 
            } 
            set {
                ImageList.Images.Clear(); 
                ImageList.ColorDepth = value;
                Images.PopulateHandle();
            }
        } 

        private bool ShouldSerializeColorDepth() { 
            return (Images.Count == 0); 
        }
 
        /// <include file='doc\ImageListDesigner.uex' path='docs/doc[@for="ImageListDesigner.Images"]/*' />
        /// <devdoc>
        ///    <para>Accessor method for the Images property on ImageList. We shadow
        ///       this property at design time.</para> 
        /// </devdoc>
        private OriginalImageCollection Images { 
            get { 
                if (originalImageCollection == null)
                    originalImageCollection = new OriginalImageCollection(this); 
                return originalImageCollection;
            }
        }
 
        internal ImageList ImageList {
            get { 
                return(ImageList) Component; 
            }
        } 

        /// <include file='doc\ImageListDesigner.uex' path='docs/doc[@for="ImageListDesigner.ImageSize"]/*' />
        /// <devdoc>
        ///    <para>Accessor method for the ImageSize property on ImageList. We shadow 
        ///       this property at design time.</para>
        /// </devdoc> 
        private Size ImageSize { 
            get {
                return ImageList.ImageSize; 
            }
            set {
                ImageList.Images.Clear();
                ImageList.ImageSize = value; 
                Images.PopulateHandle();
            } 
        } 

        private bool ShouldSerializeImageSize() { 
            return (Images.Count == 0);
        }

 
        private Color TransparentColor {
            get { 
                return ImageList.TransparentColor; 
            }
            set { 
                ImageList.Images.Clear();
                ImageList.TransparentColor = value;
                Images.PopulateHandle();
            } 
        }
 
        private bool ShouldSerializeTransparentColor() { 
            return !TransparentColor.Equals(Color.LightGray);
        } 


        /// <include file='doc\ImageListDesigner.uex' path='docs/doc[@for="ImageListDesigner.ImageStream"]/*' />
        /// <devdoc> 
        ///    <para>Accessor method for the ImageStream property on ImageList. We shadow
        ///       this property at design time.</para> 
        /// </devdoc> 
        private ImageListStreamer ImageStream {
            get { 
                return ImageList.ImageStream;
            }
            set {
                ImageList.ImageStream = value; 
                Images.ReloadFromImageList();
            } 
        } 

 
        /// <include file='doc\ImageListDesigner.uex' path='docs/doc[@for="ImageListDesigner.PreFilterProperties"]/*' />
        /// <devdoc>
        ///    <para>Provides an opportunity for the designer to filter the properties.</para>
        /// </devdoc> 
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties); 
 
            // Handle shadowed properties
            // 
            string[] shadowProps = new string[] {
                "ColorDepth",
                "ImageSize",
                "ImageStream", 
                "TransparentColor"
            }; 
 
            Attribute[] empty = new Attribute[0];
 
            for (int i = 0; i < shadowProps.Length; i++) {
                PropertyDescriptor prop = (PropertyDescriptor)properties[shadowProps[i]];
                if (prop != null) {
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(ImageListDesigner), prop, empty); 
                }
            } 
 
            // replace this one seperately because it is of a different type (OriginalImageCollection) than
            // the original property (ImageCollection) 
            //
            PropertyDescriptor imageProp = (PropertyDescriptor)properties["Images"];

            if (imageProp != null) { 
                Attribute[] attrs = new Attribute[imageProp.Attributes.Count];
                imageProp.Attributes.CopyTo(attrs, 0); 
                properties["Images"] = TypeDescriptor.CreateProperty(typeof(ImageListDesigner), "Images", typeof(OriginalImageCollection), attrs); 
            }
 


        }
 

        public override DesignerActionListCollection ActionLists { 
            get { 
                if (_actionLists == null) {
                    _actionLists = new DesignerActionListCollection(); 
                    _actionLists.Add(new ImageListActionList(this));
                }
                return _actionLists;
            } 
        }
 
        //  Shadow ImageList.Images to allow arbitrary handle recreation. 
        [
           Editor("System.Windows.Forms.Design.ImageCollectionEditor, " + AssemblyRef.SystemDesign, typeof(System.Drawing.Design.UITypeEditor)) 
        ]
        internal class OriginalImageCollection : IList {
            private ImageListDesigner owner;
            private IList list = new ArrayList(); 

            internal OriginalImageCollection(ImageListDesigner owner) { 
                this.owner = owner; 
                // just in case it's got images
                ReloadFromImageList(); 
            }

            private void AssertInvariant() {
                Debug.Assert(owner != null, "OriginalImageCollection has no owner (ImageListDesigner)"); 
                Debug.Assert(list != null, "OriginalImageCollection has no list (ImageListDesigner)");
            } 
 
            public int Count {
                get { 
                    AssertInvariant();
                    return list.Count;
                }
            } 

            public bool IsReadOnly { 
                get { 
                    return false;
                } 
            }

            bool IList.IsFixedSize {
                get { 
                    return false;
                } 
            } 

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] 
            public ImageListImage this[int index] {
                get {
                    if (index < 0 || index >= Count)
                        throw new ArgumentOutOfRangeException(SR.GetString(SR.InvalidArgument, 
                                                                  "index",
                                                                  index.ToString(CultureInfo.CurrentCulture))); 
                    return(ImageListImage) list[index]; 
                }
                set { 
                    if (index < 0 || index >= Count)
                        throw new ArgumentOutOfRangeException(SR.GetString(SR.InvalidArgument,
                                                                  "index",
                                                                  index.ToString(CultureInfo.CurrentCulture))); 

                    if (value == null) 
                        throw new ArgumentException(SR.GetString(SR.InvalidArgument, 
                                                                  "value",
                                                                  "null")); 

                    AssertInvariant();
                    list[index] = value;
                    RecreateHandle(); 
                }
 
            } 

            object IList.this[int index] { 
                get {
                    return this[index];
                }
                set { 
                    if (value is ImageListImage) {
                        this[index] = (ImageListImage)value; 
                    } 
                    else {
                        throw new ArgumentException(SR.GetString(SR.ImageListDesignerBadImageListImage,"value")); 
                    }

                }
            } 

            public void SetKeyName(int index, string name) { 
                this[index].Name = name; 
                owner.ImageList.Images.SetKeyName(index, name);
            } 
            /// <include file='doc\ImageListDesigner.uex' path='docs/doc[@for="ImageListDesigner.OriginalImageCollection.Add"]/*' />
            /// <devdoc>
            ///     Add the given image to the ImageList.
            /// </devdoc> 
            public int Add(ImageListImage value) {
 
                int index = list.Add(value); 
                if (value.Name != null) {
                    owner.ImageList.Images.Add(value.Name, value.Image); 
                }
                else {
                     owner.ImageList.Images.Add(value.Image);
                } 
                return index;
            } 
 
            public void AddRange(ImageListImage[] values) {
                if (values == null) { 
                    throw new ArgumentNullException("values");
                }
                foreach(ImageListImage value in values) {
                    if (value != null) { 
                        Add(value);
                    } 
                } 
            }
 
            int IList.Add(object value) {
               if (value is ImageListImage) {
                    return Add((ImageListImage)value);
                } 
                else {
                    throw new ArgumentException(SR.GetString(SR.ImageListDesignerBadImageListImage,"value")); 
                } 
            }
 
            // Called when reloading the form.  In this case, we have no "originals" list,
            // so we make one out of the image list.
            internal void ReloadFromImageList() {
                list.Clear(); 
                StringCollection imageKeys = owner.ImageList.Images.Keys;
                for(int i = 0; i < owner.ImageList.Images.Count; i++) { 
                    list.Add(new ImageListImage((Bitmap)owner.ImageList.Images[i], imageKeys[i])); 
                }
            } 

            /// <include file='doc\ImageListDesigner.uex' path='docs/doc[@for="ImageListDesigner.OriginalImageCollection.Clear"]/*' />
            /// <devdoc>
            ///     Remove all images and masks from the ImageList. 
            /// </devdoc>
            public void Clear() { 
                AssertInvariant(); 
                list.Clear();
                owner.ImageList.Images.Clear(); 
            }
            public bool Contains(ImageListImage value) {
                return list.Contains(value.Image);
            } 

            bool IList.Contains(object value) { 
                if (value is ImageListImage) { 
                    return Contains((ImageListImage)value);
                } 
                else {
                    return false;
                }
            } 

            public IEnumerator GetEnumerator() { 
                return list.GetEnumerator(); 
            }
 
            public int IndexOf(Image value) {
                return list.IndexOf(value);
            }
 
            int IList.IndexOf(object value) {
                if (value is Image) { 
                    return IndexOf((Image)value); 
                }
                else { 
                    return -1;
                }
            }
 
            void IList.Insert(int index, object value) {
                throw new NotSupportedException(); 
            } 

            internal void PopulateHandle() { 
                for (int i = 0; i < list.Count; i++) {
                    ImageListImage imageListImage = (ImageListImage) list[i];
                    owner.ImageList.Images.Add(imageListImage.Name, imageListImage.Image);
                } 
            }
 
            private void RecreateHandle() { 
                owner.ImageList.Images.Clear();
                PopulateHandle(); 
            }

            public void Remove(Image value) {
                AssertInvariant(); 
                list.Remove(value);
                RecreateHandle(); 
            } 

            void IList.Remove(object value) { 
                if (value is Image) {
                    Remove((Image)value);
                }
            } 

            public void RemoveAt(int index) { 
                if (index < 0 || index >= Count) 
                    throw new ArgumentOutOfRangeException(SR.GetString(SR.InvalidArgument,
                                                              "index", 
                                                              index.ToString(CultureInfo.CurrentCulture)));

                AssertInvariant();
                list.RemoveAt(index); 
                RecreateHandle();
            } 
 
            int ICollection.Count {
                get { 
                    return Count;
                }
            }
 
            bool ICollection.IsSynchronized {
                get { 
                    return false; 
                }
            } 

            object ICollection.SyncRoot {
                get {
                    return null; 
                }
            } 
 
            void ICollection.CopyTo(Array array, int index) {
                list.CopyTo(array, index); 
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator(); 
            }
        } // end class OriginalImageCollection 
    } 

    internal class ImageListActionList : DesignerActionList { 
        private ImageListDesigner _designer;

        public ImageListActionList(ImageListDesigner designer) : base(designer.Component) {
            _designer = designer; 
        }
 
        public void ChooseImages() { 
            EditorServiceContext.EditValue(_designer, Component, "Images");
        } 

        public ColorDepth ColorDepth {
            get {
                return ((ImageList)Component).ColorDepth; 
            }
            set { 
                TypeDescriptor.GetProperties(Component)["ColorDepth"].SetValue(Component, value); 
            }
        } 

        public Size ImageSize {
            get {
                return ((ImageList)Component).ImageSize; 
            }
            set { 
                TypeDescriptor.GetProperties(Component)["ImageSize"].SetValue(Component, value); 
            }
        } 

        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = new DesignerActionItemCollection();
            items.Add(new DesignerActionPropertyItem("ImageSize", 
                SR.GetString(SR.ImageListActionListImageSizeDisplayName),
                SR.GetString(SR.PropertiesCategoryName), 
                SR.GetString(SR.ImageListActionListImageSizeDescription))); 
            items.Add(new DesignerActionPropertyItem("ColorDepth",
                SR.GetString(SR.ImageListActionListColorDepthDisplayName), 
                SR.GetString(SR.PropertiesCategoryName),
                SR.GetString(SR.ImageListActionListColorDepthDescription)));

            items.Add(new DesignerActionMethodItem(this, 
                "ChooseImages",
                SR.GetString(SR.ImageListActionListChooseImagesDisplayName), 
                SR.GetString(SR.LinksCategoryName), 
                SR.GetString(SR.ImageListActionListChooseImagesDescription), true));
            return items; 
        }
 	}
}
 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
