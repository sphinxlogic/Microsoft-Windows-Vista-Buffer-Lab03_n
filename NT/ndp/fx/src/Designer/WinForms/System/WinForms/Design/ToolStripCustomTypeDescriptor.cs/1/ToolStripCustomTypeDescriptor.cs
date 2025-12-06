//------------------------------------------------------------------------------ 
// <copyright file="ToolStripCustomTypeDescriptor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Design; 
    using System.ComponentModel;
    using System.Diagnostics;
    using System;
    using System.Collections; 
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Serialization; 
    using System.Windows.Forms; 

    /// <summary> 
    ///  ToolStripCustomTypeDescriptor class.
    /// </summary>
    internal class ToolStripCustomTypeDescriptor : CustomTypeDescriptor
    { 

        ToolStrip instance = null; 
        PropertyDescriptor propItems = null; 
        PropertyDescriptorCollection collection = null;
 
        public ToolStripCustomTypeDescriptor(ToolStrip instance) : base()
        {
            this.instance = instance;
        } 

        /// <include file='doc\ToolStripCustomTypeDescriptor.uex' path='docs/doc[@for="ToolStripCustomTypeDescriptor.GetPropertyOwner"]/*' /> 
        /// <devdoc> 
        ///     The GetPropertyOwner method returns an instance of an object that
        ///     owns the given property for the object this type descriptor is representing. 
        ///     An optional attribute array may be provided to filter the collection that is
        ///     returned.  Returning null from this method causes the TypeDescriptor object
        ///     to use its default type description services.
        /// </devdoc> 
        public override object GetPropertyOwner(PropertyDescriptor pd)
        { 
            return instance; 
        }
 
        /// <include file='doc\ToolStripCustomTypeDescriptor.uex' path='docs/doc[@for="ToolStripCustomTypeDescriptor.GetProperties"]/*' />
        /// <devdoc>
        ///     The GetProperties method returns a collection of property descriptors
        ///     for the object this type descriptor is representing.  An optional 
        ///     attribute array may be provided to filter the collection that is returned.
        ///     If no parent is provided,this will return an empty 
        ///     property collection. 
        /// </devdoc>
        public override PropertyDescriptorCollection GetProperties() 
        {
            if (instance!= null && collection == null)
            {
                PropertyDescriptorCollection retColl = TypeDescriptor.GetProperties(instance); 
                PropertyDescriptor[] propArray = new PropertyDescriptor[retColl.Count];
 
                retColl.CopyTo(propArray, 0); 

                collection = new PropertyDescriptorCollection(propArray, false); 
            }
            if (collection.Count > 0)
            {
                propItems = collection["Items"]; 
                if (propItems != null)
                { 
                    collection.Remove(propItems); 
                }
            } 
            return collection;
        }

        /// <include file='doc\ToolStripCustomTypeDescriptor.uex' path='docs/doc[@for="ToolStripCustomTypeDescriptor.GetProperties"]/*' /> 
        /// <devdoc>
        ///     The GetProperties method returns a collection of property descriptors 
        ///     for the object this type descriptor is representing.  An optional 
        ///     attribute array may be provided to filter the collection that is returned.
        ///     If no parent is provided,this will return an empty 
        ///     property collection.
        ///     Here we will pass the "collection without the "items" property.
        /// </devdoc>
        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes) 
        {
            if (instance!= null && collection == null) 
            { 
                PropertyDescriptorCollection retColl = TypeDescriptor.GetProperties(instance);
                PropertyDescriptor[] propArray = new PropertyDescriptor[retColl.Count]; 

                retColl.CopyTo(propArray, 0);

                collection = new PropertyDescriptorCollection(propArray, false); 
            }
            if (collection.Count > 0) 
            { 
                propItems = collection["Items"];
                if (propItems != null) 
                {
                    collection.Remove(propItems);
                }
            } 
            return collection;
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ToolStripCustomTypeDescriptor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Design; 
    using System.ComponentModel;
    using System.Diagnostics;
    using System;
    using System.Collections; 
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Serialization; 
    using System.Windows.Forms; 

    /// <summary> 
    ///  ToolStripCustomTypeDescriptor class.
    /// </summary>
    internal class ToolStripCustomTypeDescriptor : CustomTypeDescriptor
    { 

        ToolStrip instance = null; 
        PropertyDescriptor propItems = null; 
        PropertyDescriptorCollection collection = null;
 
        public ToolStripCustomTypeDescriptor(ToolStrip instance) : base()
        {
            this.instance = instance;
        } 

        /// <include file='doc\ToolStripCustomTypeDescriptor.uex' path='docs/doc[@for="ToolStripCustomTypeDescriptor.GetPropertyOwner"]/*' /> 
        /// <devdoc> 
        ///     The GetPropertyOwner method returns an instance of an object that
        ///     owns the given property for the object this type descriptor is representing. 
        ///     An optional attribute array may be provided to filter the collection that is
        ///     returned.  Returning null from this method causes the TypeDescriptor object
        ///     to use its default type description services.
        /// </devdoc> 
        public override object GetPropertyOwner(PropertyDescriptor pd)
        { 
            return instance; 
        }
 
        /// <include file='doc\ToolStripCustomTypeDescriptor.uex' path='docs/doc[@for="ToolStripCustomTypeDescriptor.GetProperties"]/*' />
        /// <devdoc>
        ///     The GetProperties method returns a collection of property descriptors
        ///     for the object this type descriptor is representing.  An optional 
        ///     attribute array may be provided to filter the collection that is returned.
        ///     If no parent is provided,this will return an empty 
        ///     property collection. 
        /// </devdoc>
        public override PropertyDescriptorCollection GetProperties() 
        {
            if (instance!= null && collection == null)
            {
                PropertyDescriptorCollection retColl = TypeDescriptor.GetProperties(instance); 
                PropertyDescriptor[] propArray = new PropertyDescriptor[retColl.Count];
 
                retColl.CopyTo(propArray, 0); 

                collection = new PropertyDescriptorCollection(propArray, false); 
            }
            if (collection.Count > 0)
            {
                propItems = collection["Items"]; 
                if (propItems != null)
                { 
                    collection.Remove(propItems); 
                }
            } 
            return collection;
        }

        /// <include file='doc\ToolStripCustomTypeDescriptor.uex' path='docs/doc[@for="ToolStripCustomTypeDescriptor.GetProperties"]/*' /> 
        /// <devdoc>
        ///     The GetProperties method returns a collection of property descriptors 
        ///     for the object this type descriptor is representing.  An optional 
        ///     attribute array may be provided to filter the collection that is returned.
        ///     If no parent is provided,this will return an empty 
        ///     property collection.
        ///     Here we will pass the "collection without the "items" property.
        /// </devdoc>
        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes) 
        {
            if (instance!= null && collection == null) 
            { 
                PropertyDescriptorCollection retColl = TypeDescriptor.GetProperties(instance);
                PropertyDescriptor[] propArray = new PropertyDescriptor[retColl.Count]; 

                retColl.CopyTo(propArray, 0);

                collection = new PropertyDescriptorCollection(propArray, false); 
            }
            if (collection.Count > 0) 
            { 
                propItems = collection["Items"];
                if (propItems != null) 
                {
                    collection.Remove(propItems);
                }
            } 
            return collection;
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
