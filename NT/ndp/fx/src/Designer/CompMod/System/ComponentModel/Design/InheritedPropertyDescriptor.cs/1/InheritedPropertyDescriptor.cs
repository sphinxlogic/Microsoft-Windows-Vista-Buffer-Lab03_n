//------------------------------------------------------------------------------ 
// <copyright file="InheritedPropertyDescriptor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.ComponentModel.Design {
    using System.Runtime.Serialization.Formatters; 
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using System.Design;
    using System.Collections; 
    using System.Drawing.Design; 
    using System.Globalization;
    using System.Windows.Forms; 
    using System.Reflection;
    using Microsoft.Win32;

    /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor"]/*' /> 
    /// <internalonly/>
    /// <devdoc> 
    ///    <para>Describes and represents inherited properties in an inherited 
    ///       class.</para>
    /// </devdoc> 
    internal class InheritedPropertyDescriptor : PropertyDescriptor {
        private PropertyDescriptor propertyDescriptor;
        private object defaultValue;
        private static object noDefault = new Object(); 
        private bool initShouldSerialize;
 
        private object originalValue; 

        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.InheritedPropertyDescriptor"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.InheritedPropertyDescriptor'/> class.
        ///    </para> 
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")] 
        public InheritedPropertyDescriptor( 
            PropertyDescriptor propertyDescriptor,
            object component, 
            bool rootComponent)
            : base(propertyDescriptor, new Attribute[] {}) {

            Debug.Assert(!(propertyDescriptor is InheritedPropertyDescriptor), "Recursive inheritance propertyDescriptor " + propertyDescriptor.ToString()); 
            this.propertyDescriptor = propertyDescriptor;
 
            InitInheritedDefaultValue(component, rootComponent); 

            // Check to see if this property points to a collection of objects 
            // that are not IComponents.  We cannot serialize the delta between
            // two collections if they do not contain components, so if we
            // detect this case we will make the property invisible to serialization.
            // We only do this if there are already items in the collection.  Otherwise, 
            // it is safe.
 
            bool readOnlyCollection = false; 

            if (typeof(ICollection).IsAssignableFrom(propertyDescriptor.PropertyType) && 
                propertyDescriptor.Attributes.Contains(DesignerSerializationVisibilityAttribute.Content)) {

                ICollection collection = propertyDescriptor.GetValue(component) as ICollection;
                if (collection != null && collection.Count > 0) { 
                    // Trawl Add and AddRange methods looking for the first comopatible
                    // serializable method.  All we need is the data type. 
                    foreach(MethodInfo method in TypeDescriptor.GetReflectionType(collection).GetMethods(BindingFlags.Public | BindingFlags.Instance)) { 

                        ParameterInfo[] parameters = method.GetParameters(); 
                        if (parameters.Length == 1) {

                            string name = method.Name;
                            Type collectionType = null; 

                            if (name.Equals("AddRange") && parameters[0].ParameterType.IsArray) { 
                                collectionType = parameters[0].ParameterType.GetElementType(); 
                            }
                            else if (name.Equals("Add")) { 
                                collectionType = parameters[0].ParameterType;
                            }

                            if (collectionType != null) { 
                                if (!typeof(IComponent).IsAssignableFrom(collectionType)) {
                                    // Must mark this object as read-only 
                                    ArrayList attributes = new ArrayList(AttributeArray); 
                                    attributes.Add(DesignerSerializationVisibilityAttribute.Hidden);
                                    attributes.Add(ReadOnlyAttribute.Yes); 
                                    attributes.Add(new EditorAttribute(typeof(UITypeEditor), typeof(UITypeEditor)));
                                    attributes.Add(new TypeConverterAttribute(typeof(ReadOnlyCollectionConverter)));
                                    Attribute[] attributeArray = (Attribute[])attributes.ToArray(typeof(Attribute));
                                    AttributeArray = attributeArray; 
                                    readOnlyCollection = true;
                                    break; 
                                } 

                            } 
                        }
                    }
                }
            } 

            if (!readOnlyCollection) { 
                if (defaultValue != noDefault) { 
                    ArrayList attributes = new ArrayList(AttributeArray);
 
                    attributes.Add(new DefaultValueAttribute(defaultValue));

                    Attribute[] attributeArray = new Attribute[attributes.Count];
 
                    attributes.CopyTo(attributeArray, 0);
                    AttributeArray = attributeArray; 
                } 
            }
        } 

        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.ComponentType"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Gets or sets the type of the component this property descriptor is bound to.
        ///    </para> 
        /// </devdoc> 
        public override Type ComponentType {
            get { 
                return propertyDescriptor.ComponentType;
            }
        }
 
        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.IsReadOnly"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets or sets a value indicating whether this property is read only.
        ///    </para> 
        /// </devdoc>
        public override bool IsReadOnly {
            get {
                return propertyDescriptor.IsReadOnly || Attributes[typeof(ReadOnlyAttribute)].Equals(ReadOnlyAttribute.Yes); 
            }
        } 
 
        internal object OriginalValue {
            get { 
                return originalValue;
            }
        }
 
        internal PropertyDescriptor PropertyDescriptor {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] //property/reflection. 
            get { 
                return this.propertyDescriptor;
            } 
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] //property/reflection.
            set {
                Debug.Assert(!(value is InheritedPropertyDescriptor), "Recursive inheritance propertyDescriptor " + propertyDescriptor.ToString());
                this.propertyDescriptor = value; 
            }
        } 
 
        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.PropertyType"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets or sets the type of the property.
        ///    </para>
        /// </devdoc> 
        public override Type PropertyType {
            get { 
                return propertyDescriptor.PropertyType; 
            }
        } 

        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.CanResetValue"]/*' />
        /// <devdoc>
        ///    <para>Indicates whether reset will change 
        ///       the value of the component.</para>
        /// </devdoc> 
        public override bool CanResetValue(object component) { 

            // We always have a default value, because we got it from the component 
            // when we were constructed.
            //
            if (defaultValue == noDefault) {
                return propertyDescriptor.CanResetValue(component); 
            }
            else { 
                return !object.Equals(GetValue(component),defaultValue); 
            }
        } 

        private object ClonedDefaultValue(object value) {
            DesignerSerializationVisibilityAttribute dsva = (DesignerSerializationVisibilityAttribute)propertyDescriptor.Attributes[typeof(DesignerSerializationVisibilityAttribute)];
            DesignerSerializationVisibility serializationVisibility; 

            // if we have a persist contents guy, we'll need to try to clone the value because 
            // otherwise we won't be able to tell when it's been modified. 
            //
            if (dsva == null) { 
                serializationVisibility = DesignerSerializationVisibility.Visible;
            }
            else {
                serializationVisibility = dsva.Visibility; 
            }
 
            if (value != null && serializationVisibility == DesignerSerializationVisibility.Content) { 
                if (value is ICloneable) {
                    // if it's clonable, clone it... 
                    //
                    value = ((ICloneable)value).Clone();
                }
                else { 
                    // otherwise, we'll just have to always spit it.
                    // 
                    value = noDefault; 
                }
            } 
            return value;
        }

        /// <devdoc> 
        ///       We need to merge in attributes from the wrapped property descriptor here.
        /// </devdoc> 
        protected override void FillAttributes(IList attributeList) { 

            base.FillAttributes(attributeList); 

            foreach (Attribute attr in propertyDescriptor.Attributes) {
                attributeList.Add(attr);
            } 
        }
 
        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.GetValue"]/*' /> 
        /// <devdoc>
        ///    <para> Gets the current value of the property on the component, 
        ///       invoking the getXXX method.</para>
        /// </devdoc>
        public override object GetValue(object component) {
            return propertyDescriptor.GetValue(component); 
        }
 
        private void InitInheritedDefaultValue(object component, bool rootComponent) { 
            try {
 
                object currentValue;

                // Don't just get the default value.  Check to see if the propertyDescriptor has
                // indicated ShouldSerialize, and if it hasn't try to use the default value. 
                // We need to do this for properties that inherit from their parent.  If we
                // are processing properties on the root component, we always favor the presence 
                // of a default value attribute.  The root component is always inherited 
                // but some values should always be written into code.
                // 
                if (!propertyDescriptor.ShouldSerializeValue(component)) {
                    DefaultValueAttribute defaultAttribute = (DefaultValueAttribute)propertyDescriptor.Attributes[typeof(DefaultValueAttribute)];
                    if (defaultAttribute != null) {
                        defaultValue = defaultAttribute.Value; 
                        currentValue = defaultValue;
                    } 
                    else { 
                        defaultValue = noDefault;
                        currentValue = propertyDescriptor.GetValue(component); 
                    }
                }
                else {
                    defaultValue = propertyDescriptor.GetValue(component); 
                    currentValue = defaultValue;
                    defaultValue = ClonedDefaultValue(defaultValue); 
                } 

                SaveOriginalValue(currentValue); 
            }
            catch  {
                // If the property get blows chunks, then the default value is NoDefault and
                // we resort to the base property descriptor. 
                this.defaultValue = noDefault;
            } 
 
            this.initShouldSerialize = ShouldSerializeValue(component);
        } 

        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.ResetValue"]/*' />
        /// <devdoc>
        ///    <para>Resets the default value for this property 
        ///       on the component.</para>
        /// </devdoc> 
        public override void ResetValue(object component) { 
            if (defaultValue == noDefault) {
                propertyDescriptor.ResetValue(component); 
            }
            else {
                SetValue(component, defaultValue);
            } 
        }
 
        private void SaveOriginalValue(object value) { 
            if (value is ICollection) {
                originalValue = new object[((ICollection)value).Count]; 
                ((ICollection)value).CopyTo((Array)originalValue, 0);
            }
            else {
                originalValue = value; 
            }
        } 
 
        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.SetValue"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Sets the value to be the new value of this property
        ///       on the component by invoking the setXXX method on the component.
        ///    </para> 
        /// </devdoc>
        public override void SetValue(object component, object value) { 
            propertyDescriptor.SetValue(component, value); 
        }
 
        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.ShouldSerializeValue"]/*' />
        /// <devdoc>
        ///    <para>Indicates whether the value of this property needs to be persisted.</para>
        /// </devdoc> 
        public override bool ShouldSerializeValue(object component) {
 
            if (IsReadOnly) { 
                return propertyDescriptor.ShouldSerializeValue(component) && Attributes.Contains(DesignerSerializationVisibilityAttribute.Content);
            } 

            if (defaultValue == noDefault) {
                return propertyDescriptor.ShouldSerializeValue(component);
            } 
            else {
                return !object.Equals(GetValue(component), defaultValue); 
            } 
        }
 
        private class ReadOnlyCollectionConverter : TypeConverter {
            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {

                if (destinationType == typeof(string)) { 
                    return SR.GetString(SR.InheritanceServiceReadOnlyCollection);
                } 
 
                return base.ConvertTo(context, culture, value, destinationType);
 
            }
        }
     }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="InheritedPropertyDescriptor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.ComponentModel.Design {
    using System.Runtime.Serialization.Formatters; 
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using System.Design;
    using System.Collections; 
    using System.Drawing.Design; 
    using System.Globalization;
    using System.Windows.Forms; 
    using System.Reflection;
    using Microsoft.Win32;

    /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor"]/*' /> 
    /// <internalonly/>
    /// <devdoc> 
    ///    <para>Describes and represents inherited properties in an inherited 
    ///       class.</para>
    /// </devdoc> 
    internal class InheritedPropertyDescriptor : PropertyDescriptor {
        private PropertyDescriptor propertyDescriptor;
        private object defaultValue;
        private static object noDefault = new Object(); 
        private bool initShouldSerialize;
 
        private object originalValue; 

        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.InheritedPropertyDescriptor"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.InheritedPropertyDescriptor'/> class.
        ///    </para> 
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")] 
        public InheritedPropertyDescriptor( 
            PropertyDescriptor propertyDescriptor,
            object component, 
            bool rootComponent)
            : base(propertyDescriptor, new Attribute[] {}) {

            Debug.Assert(!(propertyDescriptor is InheritedPropertyDescriptor), "Recursive inheritance propertyDescriptor " + propertyDescriptor.ToString()); 
            this.propertyDescriptor = propertyDescriptor;
 
            InitInheritedDefaultValue(component, rootComponent); 

            // Check to see if this property points to a collection of objects 
            // that are not IComponents.  We cannot serialize the delta between
            // two collections if they do not contain components, so if we
            // detect this case we will make the property invisible to serialization.
            // We only do this if there are already items in the collection.  Otherwise, 
            // it is safe.
 
            bool readOnlyCollection = false; 

            if (typeof(ICollection).IsAssignableFrom(propertyDescriptor.PropertyType) && 
                propertyDescriptor.Attributes.Contains(DesignerSerializationVisibilityAttribute.Content)) {

                ICollection collection = propertyDescriptor.GetValue(component) as ICollection;
                if (collection != null && collection.Count > 0) { 
                    // Trawl Add and AddRange methods looking for the first comopatible
                    // serializable method.  All we need is the data type. 
                    foreach(MethodInfo method in TypeDescriptor.GetReflectionType(collection).GetMethods(BindingFlags.Public | BindingFlags.Instance)) { 

                        ParameterInfo[] parameters = method.GetParameters(); 
                        if (parameters.Length == 1) {

                            string name = method.Name;
                            Type collectionType = null; 

                            if (name.Equals("AddRange") && parameters[0].ParameterType.IsArray) { 
                                collectionType = parameters[0].ParameterType.GetElementType(); 
                            }
                            else if (name.Equals("Add")) { 
                                collectionType = parameters[0].ParameterType;
                            }

                            if (collectionType != null) { 
                                if (!typeof(IComponent).IsAssignableFrom(collectionType)) {
                                    // Must mark this object as read-only 
                                    ArrayList attributes = new ArrayList(AttributeArray); 
                                    attributes.Add(DesignerSerializationVisibilityAttribute.Hidden);
                                    attributes.Add(ReadOnlyAttribute.Yes); 
                                    attributes.Add(new EditorAttribute(typeof(UITypeEditor), typeof(UITypeEditor)));
                                    attributes.Add(new TypeConverterAttribute(typeof(ReadOnlyCollectionConverter)));
                                    Attribute[] attributeArray = (Attribute[])attributes.ToArray(typeof(Attribute));
                                    AttributeArray = attributeArray; 
                                    readOnlyCollection = true;
                                    break; 
                                } 

                            } 
                        }
                    }
                }
            } 

            if (!readOnlyCollection) { 
                if (defaultValue != noDefault) { 
                    ArrayList attributes = new ArrayList(AttributeArray);
 
                    attributes.Add(new DefaultValueAttribute(defaultValue));

                    Attribute[] attributeArray = new Attribute[attributes.Count];
 
                    attributes.CopyTo(attributeArray, 0);
                    AttributeArray = attributeArray; 
                } 
            }
        } 

        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.ComponentType"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Gets or sets the type of the component this property descriptor is bound to.
        ///    </para> 
        /// </devdoc> 
        public override Type ComponentType {
            get { 
                return propertyDescriptor.ComponentType;
            }
        }
 
        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.IsReadOnly"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets or sets a value indicating whether this property is read only.
        ///    </para> 
        /// </devdoc>
        public override bool IsReadOnly {
            get {
                return propertyDescriptor.IsReadOnly || Attributes[typeof(ReadOnlyAttribute)].Equals(ReadOnlyAttribute.Yes); 
            }
        } 
 
        internal object OriginalValue {
            get { 
                return originalValue;
            }
        }
 
        internal PropertyDescriptor PropertyDescriptor {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] //property/reflection. 
            get { 
                return this.propertyDescriptor;
            } 
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] //property/reflection.
            set {
                Debug.Assert(!(value is InheritedPropertyDescriptor), "Recursive inheritance propertyDescriptor " + propertyDescriptor.ToString());
                this.propertyDescriptor = value; 
            }
        } 
 
        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.PropertyType"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets or sets the type of the property.
        ///    </para>
        /// </devdoc> 
        public override Type PropertyType {
            get { 
                return propertyDescriptor.PropertyType; 
            }
        } 

        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.CanResetValue"]/*' />
        /// <devdoc>
        ///    <para>Indicates whether reset will change 
        ///       the value of the component.</para>
        /// </devdoc> 
        public override bool CanResetValue(object component) { 

            // We always have a default value, because we got it from the component 
            // when we were constructed.
            //
            if (defaultValue == noDefault) {
                return propertyDescriptor.CanResetValue(component); 
            }
            else { 
                return !object.Equals(GetValue(component),defaultValue); 
            }
        } 

        private object ClonedDefaultValue(object value) {
            DesignerSerializationVisibilityAttribute dsva = (DesignerSerializationVisibilityAttribute)propertyDescriptor.Attributes[typeof(DesignerSerializationVisibilityAttribute)];
            DesignerSerializationVisibility serializationVisibility; 

            // if we have a persist contents guy, we'll need to try to clone the value because 
            // otherwise we won't be able to tell when it's been modified. 
            //
            if (dsva == null) { 
                serializationVisibility = DesignerSerializationVisibility.Visible;
            }
            else {
                serializationVisibility = dsva.Visibility; 
            }
 
            if (value != null && serializationVisibility == DesignerSerializationVisibility.Content) { 
                if (value is ICloneable) {
                    // if it's clonable, clone it... 
                    //
                    value = ((ICloneable)value).Clone();
                }
                else { 
                    // otherwise, we'll just have to always spit it.
                    // 
                    value = noDefault; 
                }
            } 
            return value;
        }

        /// <devdoc> 
        ///       We need to merge in attributes from the wrapped property descriptor here.
        /// </devdoc> 
        protected override void FillAttributes(IList attributeList) { 

            base.FillAttributes(attributeList); 

            foreach (Attribute attr in propertyDescriptor.Attributes) {
                attributeList.Add(attr);
            } 
        }
 
        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.GetValue"]/*' /> 
        /// <devdoc>
        ///    <para> Gets the current value of the property on the component, 
        ///       invoking the getXXX method.</para>
        /// </devdoc>
        public override object GetValue(object component) {
            return propertyDescriptor.GetValue(component); 
        }
 
        private void InitInheritedDefaultValue(object component, bool rootComponent) { 
            try {
 
                object currentValue;

                // Don't just get the default value.  Check to see if the propertyDescriptor has
                // indicated ShouldSerialize, and if it hasn't try to use the default value. 
                // We need to do this for properties that inherit from their parent.  If we
                // are processing properties on the root component, we always favor the presence 
                // of a default value attribute.  The root component is always inherited 
                // but some values should always be written into code.
                // 
                if (!propertyDescriptor.ShouldSerializeValue(component)) {
                    DefaultValueAttribute defaultAttribute = (DefaultValueAttribute)propertyDescriptor.Attributes[typeof(DefaultValueAttribute)];
                    if (defaultAttribute != null) {
                        defaultValue = defaultAttribute.Value; 
                        currentValue = defaultValue;
                    } 
                    else { 
                        defaultValue = noDefault;
                        currentValue = propertyDescriptor.GetValue(component); 
                    }
                }
                else {
                    defaultValue = propertyDescriptor.GetValue(component); 
                    currentValue = defaultValue;
                    defaultValue = ClonedDefaultValue(defaultValue); 
                } 

                SaveOriginalValue(currentValue); 
            }
            catch  {
                // If the property get blows chunks, then the default value is NoDefault and
                // we resort to the base property descriptor. 
                this.defaultValue = noDefault;
            } 
 
            this.initShouldSerialize = ShouldSerializeValue(component);
        } 

        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.ResetValue"]/*' />
        /// <devdoc>
        ///    <para>Resets the default value for this property 
        ///       on the component.</para>
        /// </devdoc> 
        public override void ResetValue(object component) { 
            if (defaultValue == noDefault) {
                propertyDescriptor.ResetValue(component); 
            }
            else {
                SetValue(component, defaultValue);
            } 
        }
 
        private void SaveOriginalValue(object value) { 
            if (value is ICollection) {
                originalValue = new object[((ICollection)value).Count]; 
                ((ICollection)value).CopyTo((Array)originalValue, 0);
            }
            else {
                originalValue = value; 
            }
        } 
 
        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.SetValue"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Sets the value to be the new value of this property
        ///       on the component by invoking the setXXX method on the component.
        ///    </para> 
        /// </devdoc>
        public override void SetValue(object component, object value) { 
            propertyDescriptor.SetValue(component, value); 
        }
 
        /// <include file='doc\InheritedPropertyDescriptor.uex' path='docs/doc[@for="InheritedPropertyDescriptor.ShouldSerializeValue"]/*' />
        /// <devdoc>
        ///    <para>Indicates whether the value of this property needs to be persisted.</para>
        /// </devdoc> 
        public override bool ShouldSerializeValue(object component) {
 
            if (IsReadOnly) { 
                return propertyDescriptor.ShouldSerializeValue(component) && Attributes.Contains(DesignerSerializationVisibilityAttribute.Content);
            } 

            if (defaultValue == noDefault) {
                return propertyDescriptor.ShouldSerializeValue(component);
            } 
            else {
                return !object.Equals(GetValue(component), defaultValue); 
            } 
        }
 
        private class ReadOnlyCollectionConverter : TypeConverter {
            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {

                if (destinationType == typeof(string)) { 
                    return SR.GetString(SR.InheritanceServiceReadOnlyCollection);
                } 
 
                return base.ConvertTo(context, culture, value, destinationType);
 
            }
        }
     }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
