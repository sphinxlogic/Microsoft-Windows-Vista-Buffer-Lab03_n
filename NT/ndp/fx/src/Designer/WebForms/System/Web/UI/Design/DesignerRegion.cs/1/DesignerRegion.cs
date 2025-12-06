//------------------------------------------------------------------------------ 
// <copyright file="DesignerRegion.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.Drawing;

    /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion"]/*' />
    /// <devdoc> 
    ///   Provides the necessary functionality for a region in a designer
    /// </devdoc> 
    public class DesignerRegion : DesignerObject { 
        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.DesignerRegionAttributeName"]/*' />
        public static readonly string DesignerRegionAttributeName = "_designerRegion"; 

        private string _displayName;
        private string _description;
        private object _userData; 
        private bool _selectable;
        private bool _selected; 
        private bool _highlight; 
        private bool _ensureSize = false;
 
        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.DesignerRegion"]/*' />
        public DesignerRegion(ControlDesigner designer, string name) : this(designer, name, false) {
        }
 
        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.DesignerRegion1"]/*' />
        public DesignerRegion(ControlDesigner designer, string name, bool selectable) : base(designer, name) { 
            _selectable = selectable; 
        }
 
        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.Description"]/*' />
        /// <devdoc>
        /// </devdoc>
        public virtual string Description { 
            get {
                if (_description == null) { 
                    return String.Empty; 
                }
                return _description; 
            }
            set {
                _description = value;
            } 
        }
 
        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.DisplayName"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public virtual string DisplayName {
            get {
                if (_displayName == null) {
                    return String.Empty; 
                }
                return _displayName; 
            } 
            set {
                _displayName = value; 
            }
        }

        public bool EnsureSize { 
            get {
                return _ensureSize; 
            } 
            set {
                _ensureSize = value; 
            }
        }

        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.Highlight"]/*' /> 
        public virtual bool Highlight {
            get { 
                return _highlight; 
            }
            set { 
                _highlight = value;
            }
        }
 
        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.Selectable"]/*' />
        public virtual bool Selectable { 
            get { 
                return _selectable;
            } 
            set {
                _selectable = value;
            }
        } 

        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.Selected"]/*' /> 
        public virtual bool Selected { 
            get {
                return _selected; 
            }
            set {
                _selected = value;
            } 
        }
 
        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.UserData"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public object UserData {
            get {
                return _userData;
            } 
            set {
                _userData = value; 
            } 
        }
 
        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.GetBounds"]/*' />
        public Rectangle GetBounds() {
            return Designer.View.GetBounds(this);
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerRegion.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.Drawing;

    /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion"]/*' />
    /// <devdoc> 
    ///   Provides the necessary functionality for a region in a designer
    /// </devdoc> 
    public class DesignerRegion : DesignerObject { 
        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.DesignerRegionAttributeName"]/*' />
        public static readonly string DesignerRegionAttributeName = "_designerRegion"; 

        private string _displayName;
        private string _description;
        private object _userData; 
        private bool _selectable;
        private bool _selected; 
        private bool _highlight; 
        private bool _ensureSize = false;
 
        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.DesignerRegion"]/*' />
        public DesignerRegion(ControlDesigner designer, string name) : this(designer, name, false) {
        }
 
        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.DesignerRegion1"]/*' />
        public DesignerRegion(ControlDesigner designer, string name, bool selectable) : base(designer, name) { 
            _selectable = selectable; 
        }
 
        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.Description"]/*' />
        /// <devdoc>
        /// </devdoc>
        public virtual string Description { 
            get {
                if (_description == null) { 
                    return String.Empty; 
                }
                return _description; 
            }
            set {
                _description = value;
            } 
        }
 
        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.DisplayName"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public virtual string DisplayName {
            get {
                if (_displayName == null) {
                    return String.Empty; 
                }
                return _displayName; 
            } 
            set {
                _displayName = value; 
            }
        }

        public bool EnsureSize { 
            get {
                return _ensureSize; 
            } 
            set {
                _ensureSize = value; 
            }
        }

        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.Highlight"]/*' /> 
        public virtual bool Highlight {
            get { 
                return _highlight; 
            }
            set { 
                _highlight = value;
            }
        }
 
        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.Selectable"]/*' />
        public virtual bool Selectable { 
            get { 
                return _selectable;
            } 
            set {
                _selectable = value;
            }
        } 

        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.Selected"]/*' /> 
        public virtual bool Selected { 
            get {
                return _selected; 
            }
            set {
                _selected = value;
            } 
        }
 
        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.UserData"]/*' /> 
        /// <devdoc>
        /// </devdoc> 
        public object UserData {
            get {
                return _userData;
            } 
            set {
                _userData = value; 
            } 
        }
 
        /// <include file='doc\DesignerRegion.uex' path='docs/doc[@for="DesignerRegion.GetBounds"]/*' />
        public Rectangle GetBounds() {
            return Designer.View.GetBounds(this);
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
