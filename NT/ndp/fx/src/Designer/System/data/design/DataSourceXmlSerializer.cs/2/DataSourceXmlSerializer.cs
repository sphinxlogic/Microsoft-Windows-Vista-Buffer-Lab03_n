 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Design {
 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
 	using System.Design;
	using System.Xml; 
 
	using System.Diagnostics;
 

    internal class DataSourceXmlSerializer {
        private static Hashtable    nameToType;
        private static Hashtable    propertySerializationInfoHash; 
        private string nameSpace = SchemaName.DataSourceNamespace;
 
        private Queue   objectNeedBeInitialized; 

        internal DataSourceXmlSerializer() { 
            objectNeedBeInitialized = new Queue();
        }

        private Hashtable NameToType { 
            get {
                if (nameToType == null) { 
                    nameToType = new Hashtable(); 
                    // consider to move this code to SchemaName?
                    // 
                    nameToType.Add(SchemaName.DbSource, typeof(DbSource));
                    nameToType.Add(SchemaName.Connection, typeof(DesignConnection));
                    nameToType.Add(SchemaName.RadTable, typeof(DesignTable));
                    nameToType.Add(SchemaName.DbCommand, typeof(DbSourceCommand)); 
                    nameToType.Add(SchemaName.Parameter, typeof(DesignParameter));
                } 
                return nameToType; 
            }
        } 

        /// <summary>
        ///  Create DataSource object from XmlElement's tag
        /// </summary> 
        private object CreateObject(string tagName) {
            if (tagName == SchemaName.OldRadTable){ 
                tagName = SchemaName.RadTable; 
            }
            if (!NameToType.Contains(tagName)) { 
                Debug.Fail("We don't know how to deserialize " + tagName);
                throw new DataSourceSerializationException(SR.GetString(SR.DTDS_CouldNotDeserializeXmlElement, tagName));
            }
 
            Type objectType = (Type)NameToType[tagName];
 
            return Activator.CreateInstance(objectType); 
        }
 
        /// <summary>
        /// </summary>
        internal object Deserialize(XmlElement xmlElement) {
            object resultObject = CreateObject(xmlElement.LocalName); 

            if (resultObject is IDataSourceXmlSerializable) { 
                ((IDataSourceXmlSerializable)resultObject).ReadXml(xmlElement, this); 
            }
            else { 
                DeserializeBody(xmlElement, resultObject);
            }

            if (resultObject is IDataSourceInitAfterLoading) { 
                objectNeedBeInitialized.Enqueue(resultObject);
            } 
 
            return resultObject;
        } 

        /// <summary>
        /// deserialize the content of an object
        /// </summary> 
        internal void DeserializeBody(XmlElement xmlElement, object obj) {
            PropertySerializationInfo serializtionInfo = GetSerializationInfo(obj.GetType()); 
            IDataSourceXmlSpecialOwner specialOwner = obj as IDataSourceXmlSpecialOwner; 

            object value; 

            foreach (XmlSerializableProperty serializableProperty in serializtionInfo.AttributeProperties) {
                DataSourceXmlAttributeAttribute attributeAttribute = serializableProperty.SerializationAttribute as DataSourceXmlAttributeAttribute;
 
                Debug.Assert(attributeAttribute != null);
 
                if (attributeAttribute != null) { 
                    XmlAttribute xmlAttribute = xmlElement.Attributes[serializableProperty.Name];
 
                    if (xmlAttribute != null) {
                        PropertyDescriptor propertyDescriptor = serializableProperty.PropertyDescriptor;

                        if (attributeAttribute.SpecialWay) { 
                            specialOwner.ReadSpecialItem(propertyDescriptor.Name, xmlAttribute, this);
                        } 
                        else { 
                            Type propertyType = serializableProperty.PropertyType;
 
                            if (propertyType == typeof(string)) {
                                value = xmlAttribute.InnerText;
                            }
                            else { 
                                value = TypeDescriptor.GetConverter(propertyType).ConvertFromString(xmlAttribute.InnerText);
                            } 
 
                            if (value != null) {
                                propertyDescriptor.SetValue(obj, value); 
                            }
                        }
                    }
                } 
            }
 
            foreach (XmlNode itemNode in xmlElement.ChildNodes) { 
                XmlElement itemElement = itemNode as XmlElement;
 
                if (itemElement != null) {
                    XmlSerializableProperty serializableItem = serializtionInfo.GetSerializablePropertyWithElementName(itemElement.LocalName);
                    if (serializableItem != null) {
                        PropertyDescriptor elementProperty = serializableItem.PropertyDescriptor; 

                        DataSourceXmlSerializationAttribute serializationAttribute = serializableItem.SerializationAttribute; 
 
                        if (serializationAttribute is DataSourceXmlElementAttribute) {
                            DataSourceXmlElementAttribute elementAttribute = (DataSourceXmlElementAttribute)serializationAttribute; 

                            bool isSpecial = serializationAttribute.SpecialWay;

                            if (isSpecial) { 
                                specialOwner.ReadSpecialItem(elementProperty.Name, itemElement, this);
                            } 
                            else if (NameToType.Contains(itemElement.LocalName)){ 
                                //  Can create object
                                object item = Deserialize(itemElement); 
                                elementProperty.SetValue(obj, item);
                            }
                            else {
                                Type propertyType = serializableItem.PropertyType; 
                                try {
                                    if (propertyType == typeof(string)) { 
                                        value = itemElement.InnerText; 
                                    }
                                    else { 
                                        value = TypeDescriptor.GetConverter(propertyType).ConvertFromString(itemElement.InnerText);
                                    }

                                    elementProperty.SetValue(obj, value); 
                                }
                                catch (Exception e) { 
                                    Debug.Fail(e.Message); 
                                }
                            } 
                        }
                        else {
                            DataSourceXmlSubItemAttribute itemAttribute = (DataSourceXmlSubItemAttribute)serializationAttribute;
 
                            if (typeof(IList).IsAssignableFrom(elementProperty.PropertyType)) {
 
                                IList list = elementProperty.GetValue(obj) as IList; 

                                foreach (XmlNode subNode in itemElement.ChildNodes) { 
                                    XmlElement subElement = subNode as XmlElement;
                                    if (subElement != null) {
                                        object item = Deserialize(subElement);
                                        list.Add(item); 
                                    }
                                } 
                            } 
                            else {
                                for (XmlNode subNode = itemElement.FirstChild; subNode!=null; subNode=subNode.NextSibling) { 
                                    if (subNode is XmlElement) {
                                        object item = Deserialize((XmlElement)subNode);
                                        elementProperty.SetValue(obj, item);
                                        break; 
                                    }
                                } 
                            } 
                        }
                    } 
                }
            }
        }
 
        /// <summary>
        ///  Get Serialization information, which is generated from the class meta data 
        /// </summary> 
        private PropertySerializationInfo GetSerializationInfo(Type type) {
            if (propertySerializationInfoHash == null) { 
                propertySerializationInfoHash = new Hashtable();
            }

            if (propertySerializationInfoHash.Contains(type)) { 
                return (PropertySerializationInfo) propertySerializationInfoHash[type];
            } 
 
            PropertySerializationInfo info = new PropertySerializationInfo(type);
            propertySerializationInfoHash.Add(type, info); 

            return info;
        }
 
        /// <summary>
        /// </summary> 
        internal void InitializeObjects() { 
            int count = objectNeedBeInitialized.Count;
 
            while (count-- > 0) {
                IDataSourceInitAfterLoading obj = (IDataSourceInitAfterLoading)objectNeedBeInitialized.Dequeue();
                obj.InitializeAfterLoading();
            } 
        }
 
        /// <summary> 
        /// </summary>
        internal void Serialize(XmlWriter xmlWriter, object obj) { 
            if (obj is IDataSourceXmlSerializable) {
                ((IDataSourceXmlSerializable)obj).WriteXml(xmlWriter, this);
            }
            else { 
                Type    componentClass = obj.GetType();
                string  elementName = null; 
 
                AttributeCollection attributeCollection = TypeDescriptor.GetAttributes(componentClass);
 
                DataSourceXmlClassAttribute classAttribute = attributeCollection[typeof(DataSourceXmlClassAttribute)] as DataSourceXmlClassAttribute;
                Debug.Assert(classAttribute != null, "We should define an element name here");

                if (classAttribute != null) { 
                    elementName = classAttribute.Name;
                } 
 
                if (elementName == null) {
                    elementName = componentClass.Name; 
                }

                xmlWriter.WriteStartElement(String.Empty, elementName, nameSpace);
                SerializeBody(xmlWriter, obj); 
                xmlWriter.WriteFullEndElement();
            } 
        } 

        /// <summary> 
        ///  Serialize the content of an object
        /// </summary>
        internal void SerializeBody(XmlWriter xmlWriter, object obj) {
            PropertyDescriptorCollection propertyCollection; 

            if (obj is ICustomTypeDescriptor) { 
                propertyCollection = ((ICustomTypeDescriptor)obj).GetProperties(); 
            }
            else { 
                propertyCollection = TypeDescriptor.GetProperties(obj);
            }

            // for whatever reason, the order that we persist elements in seems to change 
            // every once in a while, causing suites to fail b/c the baseline is
            // different than the daily log of the suite. to fix this & make it consistent, 
            // we sort the collection. 
            //
            propertyCollection = propertyCollection.Sort(); 
            //

            ArrayList elementProperties = new ArrayList();
            IDataSourceXmlSpecialOwner specialOwner = obj as IDataSourceXmlSpecialOwner; 

            foreach (PropertyDescriptor propertyDescriptor in propertyCollection) { 
                DataSourceXmlSerializationAttribute serializationAttribute = 
                    (DataSourceXmlSerializationAttribute)propertyDescriptor.Attributes[typeof(DataSourceXmlSerializationAttribute)];
 
                if (serializationAttribute != null) {
                    if (serializationAttribute is DataSourceXmlAttributeAttribute) {
                        DataSourceXmlAttributeAttribute attributeAttribute = (DataSourceXmlAttributeAttribute)serializationAttribute;
 
                        object attributeValue = propertyDescriptor.GetValue(obj);
 
                        if (attributeValue != null) { 
                            string name = attributeAttribute.Name;
                            if (name == null) { 
                                name = propertyDescriptor.Name;
                            }

                            if (attributeAttribute.SpecialWay) { 
                                xmlWriter.WriteStartAttribute(String.Empty, name, String.Empty);
                                specialOwner.WriteSpecialItem(propertyDescriptor.Name, xmlWriter, this); 
                                xmlWriter.WriteEndAttribute(); 
                            }
                            else { 

                                xmlWriter.WriteAttributeString(name, attributeValue.ToString());
                            }
                        } 
                    }
                    else { 
                        elementProperties.Add(propertyDescriptor); 
                    }
                } 
            }

            foreach (PropertyDescriptor elementProperty in elementProperties) {
                object attributeValue = elementProperty.GetValue(obj); 

                if (attributeValue != null) { 
                    DataSourceXmlSerializationAttribute serializationAttribute = 
                        (DataSourceXmlSerializationAttribute)elementProperty.Attributes[typeof(DataSourceXmlSerializationAttribute)];
 
                    string name = serializationAttribute.Name;
                    if (name == null) {
                        name = elementProperty.Name;
                    } 

                    if (serializationAttribute is DataSourceXmlElementAttribute) { 
                        DataSourceXmlElementAttribute elementAttribute = (DataSourceXmlElementAttribute)serializationAttribute; 

                        bool isSpecial = serializationAttribute.SpecialWay; 

                        if (isSpecial) {
                            xmlWriter.WriteStartElement(String.Empty, name, nameSpace);
                            specialOwner.WriteSpecialItem(elementProperty.Name, xmlWriter, this); 
                            xmlWriter.WriteFullEndElement();
                        } 
                        else if (NameToType.Contains(name)) { 
                            // Can create object
                            Serialize(xmlWriter, attributeValue); 
                        }
                        else {
                            xmlWriter.WriteElementString(name, attributeValue.ToString());
                        } 
                    }
                    else { 
                        DataSourceXmlSubItemAttribute itemAttribute = (DataSourceXmlSubItemAttribute)serializationAttribute; 

                        xmlWriter.WriteStartElement(String.Empty, name, nameSpace); 

                        if (attributeValue is ICollection) {
                            foreach (object subObject in (ICollection)attributeValue) {
                                Serialize(xmlWriter, subObject); 
                            }
                        } 
                        else { 
                            Serialize(xmlWriter, attributeValue);
                        } 

                        xmlWriter.WriteFullEndElement();
                    }
                } 
            }
        } 
 

        private class PropertySerializationInfo { 
            internal XmlSerializableProperty[]  AttributeProperties;
            private Hashtable                   elementProperties;

            internal PropertySerializationInfo(Type type) { 
                ArrayList attributeProperties = new ArrayList();
                elementProperties = new Hashtable(); 
 
                PropertyDescriptorCollection propertyCollection = TypeDescriptor.GetProperties(type);
                foreach (PropertyDescriptor propertyDescriptor in propertyCollection) { 
                    DataSourceXmlSerializationAttribute serializationAttribute =
                        (DataSourceXmlSerializationAttribute)propertyDescriptor.Attributes[typeof(DataSourceXmlSerializationAttribute)];

                    if (serializationAttribute != null) { 
                        XmlSerializableProperty serializableProperty = new XmlSerializableProperty(serializationAttribute, propertyDescriptor);
 
                        if (serializationAttribute is DataSourceXmlAttributeAttribute) { 
                            attributeProperties.Add(serializableProperty);
                        } 
                        else {
                            elementProperties.Add(serializableProperty.Name, serializableProperty);
                        }
                    } 
                }
 
                AttributeProperties = (XmlSerializableProperty[]) attributeProperties.ToArray(typeof(XmlSerializableProperty)); 
            }
 
            internal XmlSerializableProperty GetSerializablePropertyWithElementName(string name) {
                if (elementProperties.Contains(name)) {
                    return (XmlSerializableProperty)elementProperties[name];
                } 
                return null;
            } 
        } 

        private class XmlSerializableProperty { 
            internal string                                 Name;
            internal DataSourceXmlSerializationAttribute    SerializationAttribute;
            internal Type                                   PropertyType;
            internal PropertyDescriptor                     PropertyDescriptor; 

            internal XmlSerializableProperty(DataSourceXmlSerializationAttribute serializationAttribute, PropertyDescriptor propertyDescriptor) { 
                Name = serializationAttribute.Name; 
                if (Name == null) {
                    Name = propertyDescriptor.Name; 
                }

                SerializationAttribute = serializationAttribute;
                PropertyDescriptor = propertyDescriptor; 

                PropertyType = serializationAttribute.ItemType; 
                if (PropertyType == null) { 
                    PropertyType = propertyDescriptor.PropertyType;
                } 
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
    using System.ComponentModel;
    using System.ComponentModel.Design; 
 	using System.Design;
	using System.Xml; 
 
	using System.Diagnostics;
 

    internal class DataSourceXmlSerializer {
        private static Hashtable    nameToType;
        private static Hashtable    propertySerializationInfoHash; 
        private string nameSpace = SchemaName.DataSourceNamespace;
 
        private Queue   objectNeedBeInitialized; 

        internal DataSourceXmlSerializer() { 
            objectNeedBeInitialized = new Queue();
        }

        private Hashtable NameToType { 
            get {
                if (nameToType == null) { 
                    nameToType = new Hashtable(); 
                    // consider to move this code to SchemaName?
                    // 
                    nameToType.Add(SchemaName.DbSource, typeof(DbSource));
                    nameToType.Add(SchemaName.Connection, typeof(DesignConnection));
                    nameToType.Add(SchemaName.RadTable, typeof(DesignTable));
                    nameToType.Add(SchemaName.DbCommand, typeof(DbSourceCommand)); 
                    nameToType.Add(SchemaName.Parameter, typeof(DesignParameter));
                } 
                return nameToType; 
            }
        } 

        /// <summary>
        ///  Create DataSource object from XmlElement's tag
        /// </summary> 
        private object CreateObject(string tagName) {
            if (tagName == SchemaName.OldRadTable){ 
                tagName = SchemaName.RadTable; 
            }
            if (!NameToType.Contains(tagName)) { 
                Debug.Fail("We don't know how to deserialize " + tagName);
                throw new DataSourceSerializationException(SR.GetString(SR.DTDS_CouldNotDeserializeXmlElement, tagName));
            }
 
            Type objectType = (Type)NameToType[tagName];
 
            return Activator.CreateInstance(objectType); 
        }
 
        /// <summary>
        /// </summary>
        internal object Deserialize(XmlElement xmlElement) {
            object resultObject = CreateObject(xmlElement.LocalName); 

            if (resultObject is IDataSourceXmlSerializable) { 
                ((IDataSourceXmlSerializable)resultObject).ReadXml(xmlElement, this); 
            }
            else { 
                DeserializeBody(xmlElement, resultObject);
            }

            if (resultObject is IDataSourceInitAfterLoading) { 
                objectNeedBeInitialized.Enqueue(resultObject);
            } 
 
            return resultObject;
        } 

        /// <summary>
        /// deserialize the content of an object
        /// </summary> 
        internal void DeserializeBody(XmlElement xmlElement, object obj) {
            PropertySerializationInfo serializtionInfo = GetSerializationInfo(obj.GetType()); 
            IDataSourceXmlSpecialOwner specialOwner = obj as IDataSourceXmlSpecialOwner; 

            object value; 

            foreach (XmlSerializableProperty serializableProperty in serializtionInfo.AttributeProperties) {
                DataSourceXmlAttributeAttribute attributeAttribute = serializableProperty.SerializationAttribute as DataSourceXmlAttributeAttribute;
 
                Debug.Assert(attributeAttribute != null);
 
                if (attributeAttribute != null) { 
                    XmlAttribute xmlAttribute = xmlElement.Attributes[serializableProperty.Name];
 
                    if (xmlAttribute != null) {
                        PropertyDescriptor propertyDescriptor = serializableProperty.PropertyDescriptor;

                        if (attributeAttribute.SpecialWay) { 
                            specialOwner.ReadSpecialItem(propertyDescriptor.Name, xmlAttribute, this);
                        } 
                        else { 
                            Type propertyType = serializableProperty.PropertyType;
 
                            if (propertyType == typeof(string)) {
                                value = xmlAttribute.InnerText;
                            }
                            else { 
                                value = TypeDescriptor.GetConverter(propertyType).ConvertFromString(xmlAttribute.InnerText);
                            } 
 
                            if (value != null) {
                                propertyDescriptor.SetValue(obj, value); 
                            }
                        }
                    }
                } 
            }
 
            foreach (XmlNode itemNode in xmlElement.ChildNodes) { 
                XmlElement itemElement = itemNode as XmlElement;
 
                if (itemElement != null) {
                    XmlSerializableProperty serializableItem = serializtionInfo.GetSerializablePropertyWithElementName(itemElement.LocalName);
                    if (serializableItem != null) {
                        PropertyDescriptor elementProperty = serializableItem.PropertyDescriptor; 

                        DataSourceXmlSerializationAttribute serializationAttribute = serializableItem.SerializationAttribute; 
 
                        if (serializationAttribute is DataSourceXmlElementAttribute) {
                            DataSourceXmlElementAttribute elementAttribute = (DataSourceXmlElementAttribute)serializationAttribute; 

                            bool isSpecial = serializationAttribute.SpecialWay;

                            if (isSpecial) { 
                                specialOwner.ReadSpecialItem(elementProperty.Name, itemElement, this);
                            } 
                            else if (NameToType.Contains(itemElement.LocalName)){ 
                                //  Can create object
                                object item = Deserialize(itemElement); 
                                elementProperty.SetValue(obj, item);
                            }
                            else {
                                Type propertyType = serializableItem.PropertyType; 
                                try {
                                    if (propertyType == typeof(string)) { 
                                        value = itemElement.InnerText; 
                                    }
                                    else { 
                                        value = TypeDescriptor.GetConverter(propertyType).ConvertFromString(itemElement.InnerText);
                                    }

                                    elementProperty.SetValue(obj, value); 
                                }
                                catch (Exception e) { 
                                    Debug.Fail(e.Message); 
                                }
                            } 
                        }
                        else {
                            DataSourceXmlSubItemAttribute itemAttribute = (DataSourceXmlSubItemAttribute)serializationAttribute;
 
                            if (typeof(IList).IsAssignableFrom(elementProperty.PropertyType)) {
 
                                IList list = elementProperty.GetValue(obj) as IList; 

                                foreach (XmlNode subNode in itemElement.ChildNodes) { 
                                    XmlElement subElement = subNode as XmlElement;
                                    if (subElement != null) {
                                        object item = Deserialize(subElement);
                                        list.Add(item); 
                                    }
                                } 
                            } 
                            else {
                                for (XmlNode subNode = itemElement.FirstChild; subNode!=null; subNode=subNode.NextSibling) { 
                                    if (subNode is XmlElement) {
                                        object item = Deserialize((XmlElement)subNode);
                                        elementProperty.SetValue(obj, item);
                                        break; 
                                    }
                                } 
                            } 
                        }
                    } 
                }
            }
        }
 
        /// <summary>
        ///  Get Serialization information, which is generated from the class meta data 
        /// </summary> 
        private PropertySerializationInfo GetSerializationInfo(Type type) {
            if (propertySerializationInfoHash == null) { 
                propertySerializationInfoHash = new Hashtable();
            }

            if (propertySerializationInfoHash.Contains(type)) { 
                return (PropertySerializationInfo) propertySerializationInfoHash[type];
            } 
 
            PropertySerializationInfo info = new PropertySerializationInfo(type);
            propertySerializationInfoHash.Add(type, info); 

            return info;
        }
 
        /// <summary>
        /// </summary> 
        internal void InitializeObjects() { 
            int count = objectNeedBeInitialized.Count;
 
            while (count-- > 0) {
                IDataSourceInitAfterLoading obj = (IDataSourceInitAfterLoading)objectNeedBeInitialized.Dequeue();
                obj.InitializeAfterLoading();
            } 
        }
 
        /// <summary> 
        /// </summary>
        internal void Serialize(XmlWriter xmlWriter, object obj) { 
            if (obj is IDataSourceXmlSerializable) {
                ((IDataSourceXmlSerializable)obj).WriteXml(xmlWriter, this);
            }
            else { 
                Type    componentClass = obj.GetType();
                string  elementName = null; 
 
                AttributeCollection attributeCollection = TypeDescriptor.GetAttributes(componentClass);
 
                DataSourceXmlClassAttribute classAttribute = attributeCollection[typeof(DataSourceXmlClassAttribute)] as DataSourceXmlClassAttribute;
                Debug.Assert(classAttribute != null, "We should define an element name here");

                if (classAttribute != null) { 
                    elementName = classAttribute.Name;
                } 
 
                if (elementName == null) {
                    elementName = componentClass.Name; 
                }

                xmlWriter.WriteStartElement(String.Empty, elementName, nameSpace);
                SerializeBody(xmlWriter, obj); 
                xmlWriter.WriteFullEndElement();
            } 
        } 

        /// <summary> 
        ///  Serialize the content of an object
        /// </summary>
        internal void SerializeBody(XmlWriter xmlWriter, object obj) {
            PropertyDescriptorCollection propertyCollection; 

            if (obj is ICustomTypeDescriptor) { 
                propertyCollection = ((ICustomTypeDescriptor)obj).GetProperties(); 
            }
            else { 
                propertyCollection = TypeDescriptor.GetProperties(obj);
            }

            // for whatever reason, the order that we persist elements in seems to change 
            // every once in a while, causing suites to fail b/c the baseline is
            // different than the daily log of the suite. to fix this & make it consistent, 
            // we sort the collection. 
            //
            propertyCollection = propertyCollection.Sort(); 
            //

            ArrayList elementProperties = new ArrayList();
            IDataSourceXmlSpecialOwner specialOwner = obj as IDataSourceXmlSpecialOwner; 

            foreach (PropertyDescriptor propertyDescriptor in propertyCollection) { 
                DataSourceXmlSerializationAttribute serializationAttribute = 
                    (DataSourceXmlSerializationAttribute)propertyDescriptor.Attributes[typeof(DataSourceXmlSerializationAttribute)];
 
                if (serializationAttribute != null) {
                    if (serializationAttribute is DataSourceXmlAttributeAttribute) {
                        DataSourceXmlAttributeAttribute attributeAttribute = (DataSourceXmlAttributeAttribute)serializationAttribute;
 
                        object attributeValue = propertyDescriptor.GetValue(obj);
 
                        if (attributeValue != null) { 
                            string name = attributeAttribute.Name;
                            if (name == null) { 
                                name = propertyDescriptor.Name;
                            }

                            if (attributeAttribute.SpecialWay) { 
                                xmlWriter.WriteStartAttribute(String.Empty, name, String.Empty);
                                specialOwner.WriteSpecialItem(propertyDescriptor.Name, xmlWriter, this); 
                                xmlWriter.WriteEndAttribute(); 
                            }
                            else { 

                                xmlWriter.WriteAttributeString(name, attributeValue.ToString());
                            }
                        } 
                    }
                    else { 
                        elementProperties.Add(propertyDescriptor); 
                    }
                } 
            }

            foreach (PropertyDescriptor elementProperty in elementProperties) {
                object attributeValue = elementProperty.GetValue(obj); 

                if (attributeValue != null) { 
                    DataSourceXmlSerializationAttribute serializationAttribute = 
                        (DataSourceXmlSerializationAttribute)elementProperty.Attributes[typeof(DataSourceXmlSerializationAttribute)];
 
                    string name = serializationAttribute.Name;
                    if (name == null) {
                        name = elementProperty.Name;
                    } 

                    if (serializationAttribute is DataSourceXmlElementAttribute) { 
                        DataSourceXmlElementAttribute elementAttribute = (DataSourceXmlElementAttribute)serializationAttribute; 

                        bool isSpecial = serializationAttribute.SpecialWay; 

                        if (isSpecial) {
                            xmlWriter.WriteStartElement(String.Empty, name, nameSpace);
                            specialOwner.WriteSpecialItem(elementProperty.Name, xmlWriter, this); 
                            xmlWriter.WriteFullEndElement();
                        } 
                        else if (NameToType.Contains(name)) { 
                            // Can create object
                            Serialize(xmlWriter, attributeValue); 
                        }
                        else {
                            xmlWriter.WriteElementString(name, attributeValue.ToString());
                        } 
                    }
                    else { 
                        DataSourceXmlSubItemAttribute itemAttribute = (DataSourceXmlSubItemAttribute)serializationAttribute; 

                        xmlWriter.WriteStartElement(String.Empty, name, nameSpace); 

                        if (attributeValue is ICollection) {
                            foreach (object subObject in (ICollection)attributeValue) {
                                Serialize(xmlWriter, subObject); 
                            }
                        } 
                        else { 
                            Serialize(xmlWriter, attributeValue);
                        } 

                        xmlWriter.WriteFullEndElement();
                    }
                } 
            }
        } 
 

        private class PropertySerializationInfo { 
            internal XmlSerializableProperty[]  AttributeProperties;
            private Hashtable                   elementProperties;

            internal PropertySerializationInfo(Type type) { 
                ArrayList attributeProperties = new ArrayList();
                elementProperties = new Hashtable(); 
 
                PropertyDescriptorCollection propertyCollection = TypeDescriptor.GetProperties(type);
                foreach (PropertyDescriptor propertyDescriptor in propertyCollection) { 
                    DataSourceXmlSerializationAttribute serializationAttribute =
                        (DataSourceXmlSerializationAttribute)propertyDescriptor.Attributes[typeof(DataSourceXmlSerializationAttribute)];

                    if (serializationAttribute != null) { 
                        XmlSerializableProperty serializableProperty = new XmlSerializableProperty(serializationAttribute, propertyDescriptor);
 
                        if (serializationAttribute is DataSourceXmlAttributeAttribute) { 
                            attributeProperties.Add(serializableProperty);
                        } 
                        else {
                            elementProperties.Add(serializableProperty.Name, serializableProperty);
                        }
                    } 
                }
 
                AttributeProperties = (XmlSerializableProperty[]) attributeProperties.ToArray(typeof(XmlSerializableProperty)); 
            }
 
            internal XmlSerializableProperty GetSerializablePropertyWithElementName(string name) {
                if (elementProperties.Contains(name)) {
                    return (XmlSerializableProperty)elementProperties[name];
                } 
                return null;
            } 
        } 

        private class XmlSerializableProperty { 
            internal string                                 Name;
            internal DataSourceXmlSerializationAttribute    SerializationAttribute;
            internal Type                                   PropertyType;
            internal PropertyDescriptor                     PropertyDescriptor; 

            internal XmlSerializableProperty(DataSourceXmlSerializationAttribute serializationAttribute, PropertyDescriptor propertyDescriptor) { 
                Name = serializationAttribute.Name; 
                if (Name == null) {
                    Name = propertyDescriptor.Name; 
                }

                SerializationAttribute = serializationAttribute;
                PropertyDescriptor = propertyDescriptor; 

                PropertyType = serializationAttribute.ItemType; 
                if (PropertyType == null) { 
                    PropertyType = propertyDescriptor.PropertyType;
                } 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
