//------------------------------------------------------------------------------ 
// <copyright file="StringArrayEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
 
    using System;
    using System.Collections;
    using Microsoft.Win32;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis; 
    using System.Drawing;
    using System.Windows.Forms; 

    /// <include file='doc\StringArrayEditor.uex' path='docs/doc[@for="StringArrayEditor"]/*' />
    /// <devdoc>
    ///      The StringArrayEditor is a collection editor that is specifically 
    ///      designed to edit arrays containing strings.
    /// </devdoc> 
    internal class StringArrayEditor : StringCollectionEditor { 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public StringArrayEditor(Type type) 
            : base(type)
        {
        }
 
        /// <include file='doc\StringArrayEditor.uex' path='docs/doc[@for="StringArrayEditor.CreateCollectionItemType"]/*' />
        /// <devdoc> 
        ///      Retrieves the data type this collection contains.  The default 
        ///      implementation looks inside of the collection for the Item property
        ///      and returns the returning datatype of the item.  Do not call this 
        ///      method directly.  Instead, use the CollectionItemType property.  Use this
        ///      method to override the default implementation.
        /// </devdoc>
        protected override Type CreateCollectionItemType() { 
            return CollectionType.GetElementType();
        } 
 
        /// <include file='doc\StringArrayEditor.uex' path='docs/doc[@for="StringArrayEditor.GetItems"]/*' />
        /// <devdoc> 
        ///      We implement the getting and setting of items on this collection.
        /// </devdoc>
        protected override object[] GetItems(object editValue) {
            Array valueArray = editValue as Array; 
            if (valueArray == null)
            { 
                return new object[0]; 
            }
            else 
            {
                object[] items = new object[valueArray.GetLength(0)];
                Array.Copy(valueArray, items, items.Length);
                return items; 
            }
        } 
 
        /// <include file='doc\StringArrayEditor.uex' path='docs/doc[@for="StringArrayEditor.SetItems"]/*' />
        /// <devdoc> 
        ///      We implement the getting and setting of items on this collection.
        ///      It should return an instance to replace editValue with, or editValue
        ///      if there is no need to replace the instance.
        /// </devdoc> 
        protected override object SetItems(object editValue, object[] value) {
            if (editValue is Array || editValue == null) { 
                Array newArray = Array.CreateInstance(CollectionItemType, value.Length); 
                Array.Copy(value, newArray, value.Length);
                return newArray; 
            }
            return editValue;
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="StringArrayEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
 
    using System;
    using System.Collections;
    using Microsoft.Win32;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis; 
    using System.Drawing;
    using System.Windows.Forms; 

    /// <include file='doc\StringArrayEditor.uex' path='docs/doc[@for="StringArrayEditor"]/*' />
    /// <devdoc>
    ///      The StringArrayEditor is a collection editor that is specifically 
    ///      designed to edit arrays containing strings.
    /// </devdoc> 
    internal class StringArrayEditor : StringCollectionEditor { 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public StringArrayEditor(Type type) 
            : base(type)
        {
        }
 
        /// <include file='doc\StringArrayEditor.uex' path='docs/doc[@for="StringArrayEditor.CreateCollectionItemType"]/*' />
        /// <devdoc> 
        ///      Retrieves the data type this collection contains.  The default 
        ///      implementation looks inside of the collection for the Item property
        ///      and returns the returning datatype of the item.  Do not call this 
        ///      method directly.  Instead, use the CollectionItemType property.  Use this
        ///      method to override the default implementation.
        /// </devdoc>
        protected override Type CreateCollectionItemType() { 
            return CollectionType.GetElementType();
        } 
 
        /// <include file='doc\StringArrayEditor.uex' path='docs/doc[@for="StringArrayEditor.GetItems"]/*' />
        /// <devdoc> 
        ///      We implement the getting and setting of items on this collection.
        /// </devdoc>
        protected override object[] GetItems(object editValue) {
            Array valueArray = editValue as Array; 
            if (valueArray == null)
            { 
                return new object[0]; 
            }
            else 
            {
                object[] items = new object[valueArray.GetLength(0)];
                Array.Copy(valueArray, items, items.Length);
                return items; 
            }
        } 
 
        /// <include file='doc\StringArrayEditor.uex' path='docs/doc[@for="StringArrayEditor.SetItems"]/*' />
        /// <devdoc> 
        ///      We implement the getting and setting of items on this collection.
        ///      It should return an instance to replace editValue with, or editValue
        ///      if there is no need to replace the instance.
        /// </devdoc> 
        protected override object SetItems(object editValue, object[] value) {
            if (editValue is Array || editValue == null) { 
                Array newArray = Array.CreateInstance(CollectionItemType, value.Length); 
                Array.Copy(value, newArray, value.Length);
                return newArray; 
            }
            return editValue;
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
