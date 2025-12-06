 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design { 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics; 
    using System.Xml;
    using System.Xml.Schema; 
    using System.Xml.Serialization; 

    internal abstract class DataSourceComponent : Component, ICustomTypeDescriptor, IObjectWithParent, IDataSourceCollectionMember, IDataSourceRenamableObject { 
        private DataSourceCollectionBase collectionParent;

        internal protected virtual DataSourceCollectionBase CollectionParent {
            get { 
                return collectionParent;
            } 
            set { 
                collectionParent = value;
            } 
        }


        /// <summary> 
        ///  return the object supports external properties
        /// </summary> 
        protected virtual object ExternalPropertyHost { 
            get {
                return null; 
            }
        }

        /// <summary> 
        /// for IObjectWithParent
        /// </summary> 
        [Browsable(false)] 
        public virtual object Parent {
            get { 
                return collectionParent;
            }
        }
 
        /// <summary>
        ///     Retrieves an array of member attributes for the given object. 
        /// </summary> 
        /// <returns>
        ///     the array of attributes on the class.  This will never be null. 
        /// </returns>
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/>
        AttributeCollection ICustomTypeDescriptor.GetAttributes() {
            return TypeDescriptor.GetAttributes(GetType()); 
        }
 
        /// <summary> 
        ///     Retrieves the class name for this object.  If null is returned,
        ///     the type name is used. 
        /// </summary>
        /// <returns>
        ///     The class name for the object, or null if the default will be used.
        /// </returns> 
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/>
        string ICustomTypeDescriptor.GetClassName() { 
            if (this is IDataSourceNamedObject) { 
                return ((IDataSourceNamedObject)this).PublicTypeName;
            } 

            return null;
        }
 
        /// <summary>
        ///     Retrieves the name for this object.  If null is returned, 
        ///     the default is used. 
        /// </summary>
        /// <returns> 
        ///     The name for the object, or null if the default will be used.
        /// </returns>
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/>
        string ICustomTypeDescriptor.GetComponentName() { 
            INamedObject namedObject = this as INamedObject;
 
            return namedObject != null ? namedObject.Name : null; 
        }
 
        /// <summary>
        ///      Retrieves the type converter for this object.
        /// </summary>
        /// <returns> 
        ///     A TypeConverter.  If null is returned, the default is used.
        /// </returns> 
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/> 
        TypeConverter ICustomTypeDescriptor.GetConverter() {
            return TypeDescriptor.GetConverter(GetType()); 
        }

        /// <summary>
        ///     Retrieves the default event. 
        /// </summary>
        /// <returns> 
        ///     the default event, or null if there are no 
        ///     events
        /// </returns> 
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/>
        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() {
            return TypeDescriptor.GetDefaultEvent(GetType());
        } 

        /// <summary> 
        ///     Retrieves the default property. 
        /// </summary>
        /// <returns> 
        ///     the default property, or null if there are no
        ///     properties
        /// </returns>
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/> 
        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() {
            return TypeDescriptor.GetDefaultProperty(GetType()); 
        } 

        /// <summary> 
        ///      Retrieves the an editor for this object.
        /// </summary>
        /// <returns>
        ///     An editor of the requested type, or null. 
        /// </returns>
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/> 
        object ICustomTypeDescriptor.GetEditor(Type editorBaseType) { 
            return TypeDescriptor.GetEditor(GetType(), editorBaseType);
        } 

        /// <summary>
        ///     Retrieves an array of events that the given component instance
        ///     provides.  This may differ from the set of events the class 
        ///     provides.  If the component is sited, the site may add or remove
        ///     additional events. 
        /// </summary> 
        /// <returns>
        ///     an array of events this component surfaces. 
        /// </returns>
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/>
        EventDescriptorCollection ICustomTypeDescriptor.GetEvents() {
            return TypeDescriptor.GetEvents(GetType()); 
        }
 
        /// <summary> 
        ///     Retrieves an array of events that the given component instance
        ///     provides.  This may differ from the set of events the class 
        ///     provides.  If the component is sited, the site may add or remove
        ///     additional events.  The returned array of events will be
        ///     filtered by the given set of attributes.
        /// </summary> 
        /// <param name='attributes'>
        ///     A set of attributes to use as a filter. 
        /// 
        ///     If a MemberAttribute instance is specified and
        ///     the event does not have an instance of that attribute's 
        ///     class, this will still include the event if the
        ///     MemberAttribute is the same as it's Default property.
        /// </param>
        /// <returns> 
        ///     an array of events this component surfaces that match
        ///     the given set of attributes.. 
        /// </returns> 
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/>
        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) { 
            return TypeDescriptor.GetEvents(GetType(), attributes);
        }

        /// <summary> 
        ///     Retrieves an array of properties that the given component instance
        ///     provides.  This may differ from the set of properties the class 
        ///     provides.  If the component is sited, the site may add or remove 
        ///     additional properties.
        /// </summary> 
        /// <returns>
        ///     an array of properties this component surfaces.
        /// </returns>
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/> 
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() {
            return GetProperties(null); 
        } 

        /// <summary> 
        ///     Retrieves an array of properties that the given component instance
        ///     provides.  This may differ from the set of properties the class
        ///     provides.  If the component is sited, the site may add or remove
        ///     additional properties.  The returned array of properties will be 
        ///     filtered by the given set of attributes.
        /// </summary> 
        /// <param name='attributes'> 
        ///     A set of attributes to use as a filter.
        /// 
        ///     If a MemberAttribute instance is specified and
        ///     the property does not have an instance of that attribute's
        ///     class, this will still include the property if the
        ///     MemberAttribute is the same as it's Default property. 
        /// </param>
        /// <returns> 
        ///     an array of properties this component surfaces that match 
        ///     the given set of attributes..
        /// </returns> 
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/>
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) {
            return GetProperties(attributes);
        } 

        private PropertyDescriptorCollection GetProperties(Attribute[] attributes) { 
            return TypeDescriptor.GetProperties(GetType(), attributes); 
        }
 
        /// <summary>
        ///     Retrieves the object that directly depends on this value being edited.  This is
        ///     generally the object that is required for the PropertyDescriptor's GetValue and SetValue
        ///     methods.  If 'null' is passed for the PropertyDescriptor, the ICustomComponent 
        ///     descripotor implemementation should return the default object, that is the main
        ///     object that exposes the properties and attributes, 
        /// </summary> 
        /// <param name='pd'>
        ///    The PropertyDescriptor to find the owner for.  This call should return an object 
        ///    such that the call "pd.GetValue(GetPropertyOwner(pd));" will generally succeed.
        ///    If 'null' is passed for pd, the main object that owns the properties and attributes
        ///    should be returned.
        /// </param> 
        /// <returns>
        ///     valueOwner 
        /// </returns> 
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/>
        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) { 
            return this;
        }

        /// <summary> 
        /// Returns an object representing a service provided by the component.
        /// </summary> 
        /// <param name="service">type of the service</param> 
        /// <returns></returns>
        protected override object GetService(Type service) { 
            // Query service from its parent node if the object is not sited.
            //
            DataSourceComponent component = this;
 
            while (component != null && component.Site == null) {
                if (component.CollectionParent != null) { 
                    component = component.CollectionParent.CollectionHost; 
                }
                else if (component.Parent != null && component.Parent is DataSourceComponent) { 
                    component = component.Parent as DataSourceComponent;
                }
                else {
                    component = null; 
                }
            } 
 
            if (component != null && component.Site != null) {
                return component.Site.GetService(service); 
            }

            return null;
        } 

        /// <summary> 
        /// IDataSourceCollectionMember implementation 
        /// </summary>
        public virtual void SetCollection(DataSourceCollectionBase collection) { 
            CollectionParent = collection;
        }

        /// <summary> 
        ///  call this function to set value, so there will be componentChanging/Changed events
 
        /// <summary> 
        ///  call this function to set value, so there will be componentChanging/Changed events
        internal void SetPropertyValue(string propertyName, object value) { 
 		//bugbug removed
        }

        internal virtual StringCollection NamingPropertyNames { 
            get {
                return null; 
            } 
        }
 
        // IDataSourceRenamableObject implementation
        [Browsable(false)]
        public virtual string GeneratorName {
            get { 
                return null;
            } 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design { 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics; 
    using System.Xml;
    using System.Xml.Schema; 
    using System.Xml.Serialization; 

    internal abstract class DataSourceComponent : Component, ICustomTypeDescriptor, IObjectWithParent, IDataSourceCollectionMember, IDataSourceRenamableObject { 
        private DataSourceCollectionBase collectionParent;

        internal protected virtual DataSourceCollectionBase CollectionParent {
            get { 
                return collectionParent;
            } 
            set { 
                collectionParent = value;
            } 
        }


        /// <summary> 
        ///  return the object supports external properties
        /// </summary> 
        protected virtual object ExternalPropertyHost { 
            get {
                return null; 
            }
        }

        /// <summary> 
        /// for IObjectWithParent
        /// </summary> 
        [Browsable(false)] 
        public virtual object Parent {
            get { 
                return collectionParent;
            }
        }
 
        /// <summary>
        ///     Retrieves an array of member attributes for the given object. 
        /// </summary> 
        /// <returns>
        ///     the array of attributes on the class.  This will never be null. 
        /// </returns>
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/>
        AttributeCollection ICustomTypeDescriptor.GetAttributes() {
            return TypeDescriptor.GetAttributes(GetType()); 
        }
 
        /// <summary> 
        ///     Retrieves the class name for this object.  If null is returned,
        ///     the type name is used. 
        /// </summary>
        /// <returns>
        ///     The class name for the object, or null if the default will be used.
        /// </returns> 
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/>
        string ICustomTypeDescriptor.GetClassName() { 
            if (this is IDataSourceNamedObject) { 
                return ((IDataSourceNamedObject)this).PublicTypeName;
            } 

            return null;
        }
 
        /// <summary>
        ///     Retrieves the name for this object.  If null is returned, 
        ///     the default is used. 
        /// </summary>
        /// <returns> 
        ///     The name for the object, or null if the default will be used.
        /// </returns>
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/>
        string ICustomTypeDescriptor.GetComponentName() { 
            INamedObject namedObject = this as INamedObject;
 
            return namedObject != null ? namedObject.Name : null; 
        }
 
        /// <summary>
        ///      Retrieves the type converter for this object.
        /// </summary>
        /// <returns> 
        ///     A TypeConverter.  If null is returned, the default is used.
        /// </returns> 
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/> 
        TypeConverter ICustomTypeDescriptor.GetConverter() {
            return TypeDescriptor.GetConverter(GetType()); 
        }

        /// <summary>
        ///     Retrieves the default event. 
        /// </summary>
        /// <returns> 
        ///     the default event, or null if there are no 
        ///     events
        /// </returns> 
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/>
        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() {
            return TypeDescriptor.GetDefaultEvent(GetType());
        } 

        /// <summary> 
        ///     Retrieves the default property. 
        /// </summary>
        /// <returns> 
        ///     the default property, or null if there are no
        ///     properties
        /// </returns>
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/> 
        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() {
            return TypeDescriptor.GetDefaultProperty(GetType()); 
        } 

        /// <summary> 
        ///      Retrieves the an editor for this object.
        /// </summary>
        /// <returns>
        ///     An editor of the requested type, or null. 
        /// </returns>
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/> 
        object ICustomTypeDescriptor.GetEditor(Type editorBaseType) { 
            return TypeDescriptor.GetEditor(GetType(), editorBaseType);
        } 

        /// <summary>
        ///     Retrieves an array of events that the given component instance
        ///     provides.  This may differ from the set of events the class 
        ///     provides.  If the component is sited, the site may add or remove
        ///     additional events. 
        /// </summary> 
        /// <returns>
        ///     an array of events this component surfaces. 
        /// </returns>
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/>
        EventDescriptorCollection ICustomTypeDescriptor.GetEvents() {
            return TypeDescriptor.GetEvents(GetType()); 
        }
 
        /// <summary> 
        ///     Retrieves an array of events that the given component instance
        ///     provides.  This may differ from the set of events the class 
        ///     provides.  If the component is sited, the site may add or remove
        ///     additional events.  The returned array of events will be
        ///     filtered by the given set of attributes.
        /// </summary> 
        /// <param name='attributes'>
        ///     A set of attributes to use as a filter. 
        /// 
        ///     If a MemberAttribute instance is specified and
        ///     the event does not have an instance of that attribute's 
        ///     class, this will still include the event if the
        ///     MemberAttribute is the same as it's Default property.
        /// </param>
        /// <returns> 
        ///     an array of events this component surfaces that match
        ///     the given set of attributes.. 
        /// </returns> 
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/>
        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) { 
            return TypeDescriptor.GetEvents(GetType(), attributes);
        }

        /// <summary> 
        ///     Retrieves an array of properties that the given component instance
        ///     provides.  This may differ from the set of properties the class 
        ///     provides.  If the component is sited, the site may add or remove 
        ///     additional properties.
        /// </summary> 
        /// <returns>
        ///     an array of properties this component surfaces.
        /// </returns>
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/> 
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() {
            return GetProperties(null); 
        } 

        /// <summary> 
        ///     Retrieves an array of properties that the given component instance
        ///     provides.  This may differ from the set of properties the class
        ///     provides.  If the component is sited, the site may add or remove
        ///     additional properties.  The returned array of properties will be 
        ///     filtered by the given set of attributes.
        /// </summary> 
        /// <param name='attributes'> 
        ///     A set of attributes to use as a filter.
        /// 
        ///     If a MemberAttribute instance is specified and
        ///     the property does not have an instance of that attribute's
        ///     class, this will still include the property if the
        ///     MemberAttribute is the same as it's Default property. 
        /// </param>
        /// <returns> 
        ///     an array of properties this component surfaces that match 
        ///     the given set of attributes..
        /// </returns> 
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/>
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) {
            return GetProperties(attributes);
        } 

        private PropertyDescriptorCollection GetProperties(Attribute[] attributes) { 
            return TypeDescriptor.GetProperties(GetType(), attributes); 
        }
 
        /// <summary>
        ///     Retrieves the object that directly depends on this value being edited.  This is
        ///     generally the object that is required for the PropertyDescriptor's GetValue and SetValue
        ///     methods.  If 'null' is passed for the PropertyDescriptor, the ICustomComponent 
        ///     descripotor implemementation should return the default object, that is the main
        ///     object that exposes the properties and attributes, 
        /// </summary> 
        /// <param name='pd'>
        ///    The PropertyDescriptor to find the owner for.  This call should return an object 
        ///    such that the call "pd.GetValue(GetPropertyOwner(pd));" will generally succeed.
        ///    If 'null' is passed for pd, the main object that owns the properties and attributes
        ///    should be returned.
        /// </param> 
        /// <returns>
        ///     valueOwner 
        /// </returns> 
        /// <seealso cref='System.ComponentModel.ICustomTypeDescriptor'/>
        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) { 
            return this;
        }

        /// <summary> 
        /// Returns an object representing a service provided by the component.
        /// </summary> 
        /// <param name="service">type of the service</param> 
        /// <returns></returns>
        protected override object GetService(Type service) { 
            // Query service from its parent node if the object is not sited.
            //
            DataSourceComponent component = this;
 
            while (component != null && component.Site == null) {
                if (component.CollectionParent != null) { 
                    component = component.CollectionParent.CollectionHost; 
                }
                else if (component.Parent != null && component.Parent is DataSourceComponent) { 
                    component = component.Parent as DataSourceComponent;
                }
                else {
                    component = null; 
                }
            } 
 
            if (component != null && component.Site != null) {
                return component.Site.GetService(service); 
            }

            return null;
        } 

        /// <summary> 
        /// IDataSourceCollectionMember implementation 
        /// </summary>
        public virtual void SetCollection(DataSourceCollectionBase collection) { 
            CollectionParent = collection;
        }

        /// <summary> 
        ///  call this function to set value, so there will be componentChanging/Changed events
 
        /// <summary> 
        ///  call this function to set value, so there will be componentChanging/Changed events
        internal void SetPropertyValue(string propertyName, object value) { 
 		//bugbug removed
        }

        internal virtual StringCollection NamingPropertyNames { 
            get {
                return null; 
            } 
        }
 
        // IDataSourceRenamableObject implementation
        [Browsable(false)]
        public virtual string GeneratorName {
            get { 
                return null;
            } 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
