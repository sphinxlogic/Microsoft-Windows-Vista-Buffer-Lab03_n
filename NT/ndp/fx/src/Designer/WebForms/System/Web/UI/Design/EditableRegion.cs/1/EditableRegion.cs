//------------------------------------------------------------------------------ 
// <copyright file="EditableDesignerRegion.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Drawing.Design; 

    /// <include file='doc\EditableDesignerRegion.uex' path='docs/doc[@for="EditableDesignerRegion"]/*' />
    /// <devdoc>
    ///   Provides the necessary functionality for an editable region in a designer 
    /// </devdoc>
    public class EditableDesignerRegion : DesignerRegion { 
        private bool _serverControlsOnly; 
        private bool _supportsDataBinding;
 
        /// <include file='doc\EditableRegion.uex' path='docs/doc[@for="EditableDesignerRegion.EditableDesignerRegion"]/*' />
        public EditableDesignerRegion(ControlDesigner owner, string name) : this(owner, name, false) {
        }
 
        /// <include file='doc\EditableRegion.uex' path='docs/doc[@for="EditableDesignerRegion.EditableDesignerRegion3"]/*' />
        public EditableDesignerRegion(ControlDesigner owner, string name, bool serverControlsOnly) : base(owner, name) { 
            _serverControlsOnly = serverControlsOnly; 
        }
 
        /// <include file='doc\EditableRegion.uex' path='docs/doc[@for="EditableDesignerRegion.Content"]/*' />
        /// <devdoc>
        /// </devdoc>
        public virtual string Content { 
            get {
                return Designer.GetEditableDesignerRegionContent(this); 
            } 
            set {
                Designer.SetEditableDesignerRegionContent(this, value); 
            }
        }

        /// <include file='doc\EditableRegion.uex' path='docs/doc[@for="EditableDesignerRegion.ServerControlsOnly"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public bool ServerControlsOnly { 
            get {
                return _serverControlsOnly; 
            }
            set {
                _serverControlsOnly = value;
            } 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public virtual bool SupportsDataBinding { 
            get {
                return _supportsDataBinding;
            }
            set { 
                _supportsDataBinding = value;
            } 
        } 

        /// <include file='doc\EditableRegion.uex' path='docs/doc[@for="EditableDesignerRegion.GetChildViewRendering"]/*' /> 
        public virtual ViewRendering GetChildViewRendering(Control control) {
            return ControlDesigner.GetViewRendering(control);
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="EditableDesignerRegion.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Drawing.Design; 

    /// <include file='doc\EditableDesignerRegion.uex' path='docs/doc[@for="EditableDesignerRegion"]/*' />
    /// <devdoc>
    ///   Provides the necessary functionality for an editable region in a designer 
    /// </devdoc>
    public class EditableDesignerRegion : DesignerRegion { 
        private bool _serverControlsOnly; 
        private bool _supportsDataBinding;
 
        /// <include file='doc\EditableRegion.uex' path='docs/doc[@for="EditableDesignerRegion.EditableDesignerRegion"]/*' />
        public EditableDesignerRegion(ControlDesigner owner, string name) : this(owner, name, false) {
        }
 
        /// <include file='doc\EditableRegion.uex' path='docs/doc[@for="EditableDesignerRegion.EditableDesignerRegion3"]/*' />
        public EditableDesignerRegion(ControlDesigner owner, string name, bool serverControlsOnly) : base(owner, name) { 
            _serverControlsOnly = serverControlsOnly; 
        }
 
        /// <include file='doc\EditableRegion.uex' path='docs/doc[@for="EditableDesignerRegion.Content"]/*' />
        /// <devdoc>
        /// </devdoc>
        public virtual string Content { 
            get {
                return Designer.GetEditableDesignerRegionContent(this); 
            } 
            set {
                Designer.SetEditableDesignerRegionContent(this, value); 
            }
        }

        /// <include file='doc\EditableRegion.uex' path='docs/doc[@for="EditableDesignerRegion.ServerControlsOnly"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public bool ServerControlsOnly { 
            get {
                return _serverControlsOnly; 
            }
            set {
                _serverControlsOnly = value;
            } 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public virtual bool SupportsDataBinding { 
            get {
                return _supportsDataBinding;
            }
            set { 
                _supportsDataBinding = value;
            } 
        } 

        /// <include file='doc\EditableRegion.uex' path='docs/doc[@for="EditableDesignerRegion.GetChildViewRendering"]/*' /> 
        public virtual ViewRendering GetChildViewRendering(Control control) {
            return ControlDesigner.GetViewRendering(control);
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
