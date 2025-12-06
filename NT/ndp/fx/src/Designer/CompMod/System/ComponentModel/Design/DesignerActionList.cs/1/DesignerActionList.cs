//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Generic;
    using System.Reflection;

    /// <include file='doc\DesignerActionList.uex' path='docs/doc[@for="DesignerActionList"]/*' /> 
    /// <devdoc>
    ///     DesignerActionList is the abstract base class which control authors inherit from to create a task sheet. 
    ///      Typical usage is to add properties and methods and then implement the abstract 
    ///      GetSortedActionItems method to return an array of DesignerActionItems in the order they are to be displayed.
    /// </devdoc> 
    public class DesignerActionList {

        private bool _autoShow = false;
        private IComponent _component; 

        /// <include file='doc\DesignerActionList.uex' path='docs/doc[@for="DesignerActionList.DesignerActionList"]/*' /> 
        /// <devdoc> 
        ///     takes the related component as a parameter
        /// </devdoc> 
        public DesignerActionList(IComponent component)   {
            _component = component;
        }
 
        /// <include file='doc\DesignerActionList.uex' path='docs/doc[@for="DesignerActionList.AutoShow"]/*' />
        /// <devdoc> 
        ///     [to be provvided] 
        /// </devdoc>
        public virtual bool AutoShow { 
            get {
                return _autoShow;
            }
            set { 
                if(_autoShow != value) {
                    _autoShow = value; 
                } 
            }
        } 

        /// <include file='doc\DesignerActionList.uex' path='docs/doc[@for="DesignerActionList.Component"]/*' />
        /// <devdoc>
        ///     this will be null for list created from upgraded verbs collection... 
        /// </devdoc>
        public IComponent Component { 
            get { 
                return _component;
            } 
        }


        /// <include file='doc\DesignerActionList.uex' path='docs/doc[@for="DesignerActionList.GetService"]/*' /> 
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc> 
        public object GetService(Type serviceType) {
            if (_component != null && _component.Site != null) { 
                return _component.Site.GetService(serviceType);
            } else {
                return null;
            } 
        }
 
        private object   GetCustomAttribute(MemberInfo info, Type attributeType) { 
            object[] attributes = info.GetCustomAttributes(attributeType, true);
            if (attributes.Length > 0) { 
                return attributes[0];
            } else {
                return null;
            } 
        }
 
        private void GetMemberDisplayProperties(MemberInfo info, out string displayName, out string description, out string category) { 
            displayName = description = category = "";
            DescriptionAttribute descAttr = GetCustomAttribute(info, typeof(DescriptionAttribute)) as DescriptionAttribute; 
            if(descAttr != null) {
                description = descAttr.Description;
            }
            DisplayNameAttribute dispNameAttr = GetCustomAttribute(info, typeof(DisplayNameAttribute)) as DisplayNameAttribute; 
            if(dispNameAttr != null) {
                displayName = dispNameAttr.DisplayName; 
            } 
            CategoryAttribute catAttr = GetCustomAttribute(info, typeof(CategoryAttribute)) as CategoryAttribute;
            if(dispNameAttr != null) { 
                category = catAttr.Category;
            }

            if (string.IsNullOrEmpty(displayName)) { 
                displayName = info.Name;
            } 
        } 

        /// <include file='doc\DesignerActionList.uex' path='docs/doc[@for="DesignerActionList.GetSortedTasks"]/*' /> 
        /// <devdoc>
        ///     [to be provvided]
        /// </devdoc>
        public virtual DesignerActionItemCollection GetSortedActionItems() { 
            string dispName, desc, cat;
            SortedList<string, DesignerActionItem> items = new SortedList<string, DesignerActionItem>(); 
 
            // we want to ignore the public methods and properties for THIS class (only take the inherited ones)
            IList<MethodInfo> originalMethods = Array.AsReadOnly(typeof(DesignerActionList).GetMethods(BindingFlags.InvokeMethod | 
                                                BindingFlags.Instance |
                                                BindingFlags.DeclaredOnly |
                                                BindingFlags.Public));
            IList<PropertyInfo> originalProperties = Array.AsReadOnly(typeof(DesignerActionList).GetProperties(BindingFlags.InvokeMethod | 
                                                BindingFlags.Instance |
                                                BindingFlags.DeclaredOnly | 
                                                BindingFlags.Public)); 

            // Do methods 
            MethodInfo[] methods = this.GetType().GetMethods(BindingFlags.InvokeMethod |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly |
                BindingFlags.Public); 
            foreach (MethodInfo info in methods) {
                if(originalMethods.Contains(info)) 
                    continue; 
                // Make sure there are only methods that take no parameters
                if (info.GetParameters().Length == 0 && !info.IsSpecialName ) { 
                    GetMemberDisplayProperties(info, out dispName, out desc, out cat);
                    items.Add(info.Name, new DesignerActionMethodItem(this, info.Name, dispName, cat, desc));
                }
            } 

            // Do properties 
            PropertyInfo [] properties = this.GetType().GetProperties(BindingFlags.InvokeMethod | 
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly | 
                BindingFlags.Public);
            foreach (PropertyInfo info in properties) {
                if(originalProperties.Contains(info))
                    continue; 
                GetMemberDisplayProperties(info, out dispName, out desc, out cat);
                items.Add(dispName, new DesignerActionPropertyItem(info.Name, dispName, cat, desc)); 
            } 

            DesignerActionItemCollection returnValue = new DesignerActionItemCollection(); 
            foreach (DesignerActionItem dai in items.Values) {
                returnValue.Add(dai);
            }
            return returnValue; 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Generic;
    using System.Reflection;

    /// <include file='doc\DesignerActionList.uex' path='docs/doc[@for="DesignerActionList"]/*' /> 
    /// <devdoc>
    ///     DesignerActionList is the abstract base class which control authors inherit from to create a task sheet. 
    ///      Typical usage is to add properties and methods and then implement the abstract 
    ///      GetSortedActionItems method to return an array of DesignerActionItems in the order they are to be displayed.
    /// </devdoc> 
    public class DesignerActionList {

        private bool _autoShow = false;
        private IComponent _component; 

        /// <include file='doc\DesignerActionList.uex' path='docs/doc[@for="DesignerActionList.DesignerActionList"]/*' /> 
        /// <devdoc> 
        ///     takes the related component as a parameter
        /// </devdoc> 
        public DesignerActionList(IComponent component)   {
            _component = component;
        }
 
        /// <include file='doc\DesignerActionList.uex' path='docs/doc[@for="DesignerActionList.AutoShow"]/*' />
        /// <devdoc> 
        ///     [to be provvided] 
        /// </devdoc>
        public virtual bool AutoShow { 
            get {
                return _autoShow;
            }
            set { 
                if(_autoShow != value) {
                    _autoShow = value; 
                } 
            }
        } 

        /// <include file='doc\DesignerActionList.uex' path='docs/doc[@for="DesignerActionList.Component"]/*' />
        /// <devdoc>
        ///     this will be null for list created from upgraded verbs collection... 
        /// </devdoc>
        public IComponent Component { 
            get { 
                return _component;
            } 
        }


        /// <include file='doc\DesignerActionList.uex' path='docs/doc[@for="DesignerActionList.GetService"]/*' /> 
        /// <devdoc>
        ///     [to be provvided] 
        /// </devdoc> 
        public object GetService(Type serviceType) {
            if (_component != null && _component.Site != null) { 
                return _component.Site.GetService(serviceType);
            } else {
                return null;
            } 
        }
 
        private object   GetCustomAttribute(MemberInfo info, Type attributeType) { 
            object[] attributes = info.GetCustomAttributes(attributeType, true);
            if (attributes.Length > 0) { 
                return attributes[0];
            } else {
                return null;
            } 
        }
 
        private void GetMemberDisplayProperties(MemberInfo info, out string displayName, out string description, out string category) { 
            displayName = description = category = "";
            DescriptionAttribute descAttr = GetCustomAttribute(info, typeof(DescriptionAttribute)) as DescriptionAttribute; 
            if(descAttr != null) {
                description = descAttr.Description;
            }
            DisplayNameAttribute dispNameAttr = GetCustomAttribute(info, typeof(DisplayNameAttribute)) as DisplayNameAttribute; 
            if(dispNameAttr != null) {
                displayName = dispNameAttr.DisplayName; 
            } 
            CategoryAttribute catAttr = GetCustomAttribute(info, typeof(CategoryAttribute)) as CategoryAttribute;
            if(dispNameAttr != null) { 
                category = catAttr.Category;
            }

            if (string.IsNullOrEmpty(displayName)) { 
                displayName = info.Name;
            } 
        } 

        /// <include file='doc\DesignerActionList.uex' path='docs/doc[@for="DesignerActionList.GetSortedTasks"]/*' /> 
        /// <devdoc>
        ///     [to be provvided]
        /// </devdoc>
        public virtual DesignerActionItemCollection GetSortedActionItems() { 
            string dispName, desc, cat;
            SortedList<string, DesignerActionItem> items = new SortedList<string, DesignerActionItem>(); 
 
            // we want to ignore the public methods and properties for THIS class (only take the inherited ones)
            IList<MethodInfo> originalMethods = Array.AsReadOnly(typeof(DesignerActionList).GetMethods(BindingFlags.InvokeMethod | 
                                                BindingFlags.Instance |
                                                BindingFlags.DeclaredOnly |
                                                BindingFlags.Public));
            IList<PropertyInfo> originalProperties = Array.AsReadOnly(typeof(DesignerActionList).GetProperties(BindingFlags.InvokeMethod | 
                                                BindingFlags.Instance |
                                                BindingFlags.DeclaredOnly | 
                                                BindingFlags.Public)); 

            // Do methods 
            MethodInfo[] methods = this.GetType().GetMethods(BindingFlags.InvokeMethod |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly |
                BindingFlags.Public); 
            foreach (MethodInfo info in methods) {
                if(originalMethods.Contains(info)) 
                    continue; 
                // Make sure there are only methods that take no parameters
                if (info.GetParameters().Length == 0 && !info.IsSpecialName ) { 
                    GetMemberDisplayProperties(info, out dispName, out desc, out cat);
                    items.Add(info.Name, new DesignerActionMethodItem(this, info.Name, dispName, cat, desc));
                }
            } 

            // Do properties 
            PropertyInfo [] properties = this.GetType().GetProperties(BindingFlags.InvokeMethod | 
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly | 
                BindingFlags.Public);
            foreach (PropertyInfo info in properties) {
                if(originalProperties.Contains(info))
                    continue; 
                GetMemberDisplayProperties(info, out dispName, out desc, out cat);
                items.Add(dispName, new DesignerActionPropertyItem(info.Name, dispName, cat, desc)); 
            } 

            DesignerActionItemCollection returnValue = new DesignerActionItemCollection(); 
            foreach (DesignerActionItem dai in items.Values) {
                returnValue.Add(dai);
            }
            return returnValue; 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
