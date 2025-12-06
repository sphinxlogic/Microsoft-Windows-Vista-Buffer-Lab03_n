//------------------------------------------------------------------------------ 
// <copyright file="TemplateDefinition.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics;
    using System.Web.UI.WebControls;
 
    /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition"]/*' />
    public class TemplateDefinition : DesignerObject { 
        private Style _style; 

        private string _templatePropertyName; 
        private object _templatedObject;
        private PropertyDescriptor _templateProperty;

        private bool _serverControlsOnly; 
        private bool _supportsDataBinding;
 
        /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition.TemplateDefinition"]/*' /> 
        public TemplateDefinition(ControlDesigner designer, string name, object templatedObject, string templatePropertyName) : this(designer, name, templatedObject, templatePropertyName, false) {
        } 

        /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition.TemplateDefinition"]/*' />
        public TemplateDefinition(ControlDesigner designer, string name, object templatedObject, string templatePropertyName, Style style) : this(designer, name, templatedObject, templatePropertyName, style, false) {
        } 

        /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition.TemplateDefinition"]/*' /> 
        public TemplateDefinition(ControlDesigner designer, string name, object templatedObject, string templatePropertyName, bool serverControlsOnly) : this(designer, name, templatedObject, templatePropertyName, null, serverControlsOnly) { 
        }
 
        /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition.TemplateDefinition1"]/*' />
        public TemplateDefinition(ControlDesigner designer, string name, object templatedObject, string templatePropertyName, Style style, bool serverControlsOnly) : base(designer, name) {
            if ((templatePropertyName == null) || (templatePropertyName.Length == 0)) {
                throw new ArgumentNullException("templatePropertyName"); 
            }
 
            if (templatedObject == null) { 
                throw new ArgumentNullException("templatedObject");
            } 

            _serverControlsOnly = serverControlsOnly;
            _style = style;
            _templatedObject = templatedObject; 
            _templatePropertyName = templatePropertyName;
        } 
 
        public virtual bool AllowEditing {
            get { 
                return true;
            }
        }
 
        /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition.Content"]/*' />
        public virtual string Content { 
            get { 
                ITemplate template = (ITemplate)TemplateProperty.GetValue(TemplatedObject);
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                return ControlPersister.PersistTemplate(template, host);
            }
            set {
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                ITemplate template = ControlParser.ParseTemplate(host, value);
                TemplateProperty.SetValue(TemplatedObject, template); 
            } 
        }
 
        /// <devdoc>
        /// </devdoc>
        public bool ServerControlsOnly {
            get { 
                return _serverControlsOnly;
            } 
        } 

        /// <devdoc> 
        /// </devdoc>
        public bool SupportsDataBinding {
            get {
                return _supportsDataBinding; 
            }
            set { 
                _supportsDataBinding = value; 
            }
        } 

        /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition.Style"]/*' />
        /// <devdoc>
        /// </devdoc> 
        public Style Style {
            get { 
                return _style; 
            }
        } 

        /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition.TemplatedObject"]/*' />
        public object TemplatedObject {
            get { 
                return _templatedObject;
            } 
        } 

        private PropertyDescriptor TemplateProperty { 
            get {
                if (_templateProperty == null) {
                    _templateProperty = TypeDescriptor.GetProperties(TemplatedObject)[TemplatePropertyName];
 
                    if ((_templateProperty == null) || !typeof(ITemplate).IsAssignableFrom(_templateProperty.PropertyType)) {
                        throw new InvalidOperationException(SR.GetString(SR.TemplateDefinition_InvalidTemplateProperty, TemplatePropertyName)); 
                    } 
                }
 
                return _templateProperty;
            }
        }
 
        /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition.TemplatePropertyName"]/*' />
        public string TemplatePropertyName { 
            get { 
                return _templatePropertyName;
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TemplateDefinition.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics;
    using System.Web.UI.WebControls;
 
    /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition"]/*' />
    public class TemplateDefinition : DesignerObject { 
        private Style _style; 

        private string _templatePropertyName; 
        private object _templatedObject;
        private PropertyDescriptor _templateProperty;

        private bool _serverControlsOnly; 
        private bool _supportsDataBinding;
 
        /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition.TemplateDefinition"]/*' /> 
        public TemplateDefinition(ControlDesigner designer, string name, object templatedObject, string templatePropertyName) : this(designer, name, templatedObject, templatePropertyName, false) {
        } 

        /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition.TemplateDefinition"]/*' />
        public TemplateDefinition(ControlDesigner designer, string name, object templatedObject, string templatePropertyName, Style style) : this(designer, name, templatedObject, templatePropertyName, style, false) {
        } 

        /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition.TemplateDefinition"]/*' /> 
        public TemplateDefinition(ControlDesigner designer, string name, object templatedObject, string templatePropertyName, bool serverControlsOnly) : this(designer, name, templatedObject, templatePropertyName, null, serverControlsOnly) { 
        }
 
        /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition.TemplateDefinition1"]/*' />
        public TemplateDefinition(ControlDesigner designer, string name, object templatedObject, string templatePropertyName, Style style, bool serverControlsOnly) : base(designer, name) {
            if ((templatePropertyName == null) || (templatePropertyName.Length == 0)) {
                throw new ArgumentNullException("templatePropertyName"); 
            }
 
            if (templatedObject == null) { 
                throw new ArgumentNullException("templatedObject");
            } 

            _serverControlsOnly = serverControlsOnly;
            _style = style;
            _templatedObject = templatedObject; 
            _templatePropertyName = templatePropertyName;
        } 
 
        public virtual bool AllowEditing {
            get { 
                return true;
            }
        }
 
        /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition.Content"]/*' />
        public virtual string Content { 
            get { 
                ITemplate template = (ITemplate)TemplateProperty.GetValue(TemplatedObject);
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                return ControlPersister.PersistTemplate(template, host);
            }
            set {
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                ITemplate template = ControlParser.ParseTemplate(host, value);
                TemplateProperty.SetValue(TemplatedObject, template); 
            } 
        }
 
        /// <devdoc>
        /// </devdoc>
        public bool ServerControlsOnly {
            get { 
                return _serverControlsOnly;
            } 
        } 

        /// <devdoc> 
        /// </devdoc>
        public bool SupportsDataBinding {
            get {
                return _supportsDataBinding; 
            }
            set { 
                _supportsDataBinding = value; 
            }
        } 

        /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition.Style"]/*' />
        /// <devdoc>
        /// </devdoc> 
        public Style Style {
            get { 
                return _style; 
            }
        } 

        /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition.TemplatedObject"]/*' />
        public object TemplatedObject {
            get { 
                return _templatedObject;
            } 
        } 

        private PropertyDescriptor TemplateProperty { 
            get {
                if (_templateProperty == null) {
                    _templateProperty = TypeDescriptor.GetProperties(TemplatedObject)[TemplatePropertyName];
 
                    if ((_templateProperty == null) || !typeof(ITemplate).IsAssignableFrom(_templateProperty.PropertyType)) {
                        throw new InvalidOperationException(SR.GetString(SR.TemplateDefinition_InvalidTemplateProperty, TemplatePropertyName)); 
                    } 
                }
 
                return _templateProperty;
            }
        }
 
        /// <include file='doc\TemplateDefinition.uex' path='docs/doc[@for="TemplateDefinition.TemplatePropertyName"]/*' />
        public string TemplatePropertyName { 
            get { 
                return _templatePropertyName;
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
