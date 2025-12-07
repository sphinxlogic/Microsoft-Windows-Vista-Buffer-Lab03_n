//------------------------------------------------------------------------------ 
// <copyright file="DesignerObject.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design;
 
    /// <devdoc>
    /// </devdoc> 
    public abstract class DesignerObject : IServiceProvider { 

        private ControlDesigner _designer; 

        private string _name;
        private IDictionary _properties;
 
        protected DesignerObject(ControlDesigner designer, string name) {
            if (designer == null) { 
                throw new ArgumentNullException("designer"); 
            }
            if ((name == null) || (name.Length == 0)) { 
                throw new ArgumentNullException("name");
            }

            _designer = designer; 
            _name = name;
        } 
 
        public ControlDesigner Designer {
            get { 
                return _designer;
            }
        }
 
        public string Name {
            get { 
                return _name; 
            }
        } 

        public IDictionary Properties {
            get {
                if (_properties == null) { 
                    _properties = new HybridDictionary();
                } 
                return _properties; 
            }
        } 

        protected object GetService(Type serviceType) {
            IServiceProvider serviceProvider = _designer.Component.Site;
            if (serviceProvider != null) { 
                return serviceProvider.GetService(serviceType);
            } 
            return null; 
        }
 
        #region Implementation of IServiceProvider
        object IServiceProvider.GetService(Type serviceType) {
            return GetService(serviceType);
        } 
        #endregion
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerObject.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design;
 
    /// <devdoc>
    /// </devdoc> 
    public abstract class DesignerObject : IServiceProvider { 

        private ControlDesigner _designer; 

        private string _name;
        private IDictionary _properties;
 
        protected DesignerObject(ControlDesigner designer, string name) {
            if (designer == null) { 
                throw new ArgumentNullException("designer"); 
            }
            if ((name == null) || (name.Length == 0)) { 
                throw new ArgumentNullException("name");
            }

            _designer = designer; 
            _name = name;
        } 
 
        public ControlDesigner Designer {
            get { 
                return _designer;
            }
        }
 
        public string Name {
            get { 
                return _name; 
            }
        } 

        public IDictionary Properties {
            get {
                if (_properties == null) { 
                    _properties = new HybridDictionary();
                } 
                return _properties; 
            }
        } 

        protected object GetService(Type serviceType) {
            IServiceProvider serviceProvider = _designer.Component.Site;
            if (serviceProvider != null) { 
                return serviceProvider.GetService(serviceType);
            } 
            return null; 
        }
 
        #region Implementation of IServiceProvider
        object IServiceProvider.GetService(Type serviceType) {
            return GetService(serviceType);
        } 
        #endregion
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
