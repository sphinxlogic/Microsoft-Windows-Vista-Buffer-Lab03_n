//------------------------------------------------------------------------------ 
// <copyright file="DesignerAutoFormat.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Web.UI;
    using System.Web.UI.WebControls;
 
    using Control = System.Web.UI.Control;
 
    /// <include file='doc\DesignerAutoFormat.uex' path='docs/doc[@for="DesignerAutoFormat"]/*' /> 
    public abstract class DesignerAutoFormat {
 
        private string _name;
        private DesignerAutoFormatStyle _style;

        protected DesignerAutoFormat(string name) { 
            if ((name == null) || (name.Length == 0)) {
                throw new ArgumentNullException("name"); 
            } 

            _name = name; 
        }

        /// <include file='doc\DesignerAutoFormat.uex' path='docs/doc[@for="DesignerAutoFormat.Name"]/*' />
        public string Name { 
            get {
                return _name; 
            } 
        }
 
        public DesignerAutoFormatStyle Style {
            get {
                if (_style == null) {
                    _style = new DesignerAutoFormatStyle(); 
                }
 
                return _style; 
            }
        } 


        /// <include file='doc\DesignerAutoFormat.uex' path='docs/doc[@for="DesignerAutoFormat.ApplyScheme"]/*' />
        public abstract void Apply(Control control); 

        public virtual Control GetPreviewControl(Control runtimeControl) { 
            IDesignerHost host = (IDesignerHost)runtimeControl.Site.GetService(typeof(IDesignerHost)); 
            ControlDesigner designer = host.GetDesigner(runtimeControl) as ControlDesigner;
 
            if (designer != null) {
                return designer.CreateClonedControl(host, true);
            }
            return null; 
        }
 
        /// <include file='doc\DesignerAutoFormat.uex' path='docs/doc[@for="DesignerAutoFormat.ToString"]/*' /> 
        public override string ToString() {
            return Name; 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerAutoFormat.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Web.UI;
    using System.Web.UI.WebControls;
 
    using Control = System.Web.UI.Control;
 
    /// <include file='doc\DesignerAutoFormat.uex' path='docs/doc[@for="DesignerAutoFormat"]/*' /> 
    public abstract class DesignerAutoFormat {
 
        private string _name;
        private DesignerAutoFormatStyle _style;

        protected DesignerAutoFormat(string name) { 
            if ((name == null) || (name.Length == 0)) {
                throw new ArgumentNullException("name"); 
            } 

            _name = name; 
        }

        /// <include file='doc\DesignerAutoFormat.uex' path='docs/doc[@for="DesignerAutoFormat.Name"]/*' />
        public string Name { 
            get {
                return _name; 
            } 
        }
 
        public DesignerAutoFormatStyle Style {
            get {
                if (_style == null) {
                    _style = new DesignerAutoFormatStyle(); 
                }
 
                return _style; 
            }
        } 


        /// <include file='doc\DesignerAutoFormat.uex' path='docs/doc[@for="DesignerAutoFormat.ApplyScheme"]/*' />
        public abstract void Apply(Control control); 

        public virtual Control GetPreviewControl(Control runtimeControl) { 
            IDesignerHost host = (IDesignerHost)runtimeControl.Site.GetService(typeof(IDesignerHost)); 
            ControlDesigner designer = host.GetDesigner(runtimeControl) as ControlDesigner;
 
            if (designer != null) {
                return designer.CreateClonedControl(host, true);
            }
            return null; 
        }
 
        /// <include file='doc\DesignerAutoFormat.uex' path='docs/doc[@for="DesignerAutoFormat.ToString"]/*' /> 
        public override string ToString() {
            return Name; 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
