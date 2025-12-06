//------------------------------------------------------------------------------ 
// <copyright file="TemplateEditingVerb.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Diagnostics; 
    using System.ComponentModel;
    using System.ComponentModel.Design;

    /// <include file='doc\TemplateEditingVerb.uex' path='docs/doc[@for="TemplateEditingVerb"]/*' /> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    [Obsolete("Use of this type is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
    public class TemplateEditingVerb : DesignerVerb, IDisposable { 

        private static readonly EventHandler dummyEventHandler = new EventHandler(OnDummyEventHandler); 

        private ITemplateEditingFrame editingFrame;
        private int index;
 
        /// <include file='doc\TemplateEditingVerb.uex' path='docs/doc[@for="TemplateEditingVerb.TemplateEditingVerb"]/*' />
        public TemplateEditingVerb(string text, int index, TemplatedControlDesigner designer) : this(text, index, designer.TemplateEditingVerbHandler) { 
        } 

        public TemplateEditingVerb(string text, int index) : this(text, index, dummyEventHandler) { 
        }

        private TemplateEditingVerb(string text, int index, EventHandler handler) : base(text, handler) {
            this.index = index; 
        }
 
        internal ITemplateEditingFrame EditingFrame { 
            get {
                return editingFrame; 
            }
            set {
                editingFrame = value;
            } 
        }
 
        /// <include file='doc\TemplateEditingVerb.uex' path='docs/doc[@for="TemplateEditingVerb.Index"]/*' /> 
        public int Index {
            get { 
                return index;
            }
        }
 
        /// <include file='doc\TemplateEditingVerb.uex' path='docs/doc[@for="TemplateEditingVerb.Dispose"]/*' />
        public void Dispose() { 
            Dispose(true); 
            GC.SuppressFinalize(this);
        } 

        /// <include file='doc\TemplateEditingVerb.uex' path='docs/doc[@for="TemplateEditingVerb.Finalize"]/*' />
        ~TemplateEditingVerb() {
            Dispose(false); 
        }
 
        /// <include file='doc\TemplateEditingVerb.uex' path='docs/doc[@for="TemplateEditingVerb.Dispose2"]/*' /> 
        protected virtual void Dispose(bool disposing) {
            if (disposing) { 
                if (editingFrame != null) {
                    editingFrame.Dispose();
                    editingFrame = null;
                } 
            }
        } 
 
        private static void OnDummyEventHandler(object sender, EventArgs e) {
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TemplateEditingVerb.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Diagnostics; 
    using System.ComponentModel;
    using System.ComponentModel.Design;

    /// <include file='doc\TemplateEditingVerb.uex' path='docs/doc[@for="TemplateEditingVerb"]/*' /> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    [Obsolete("Use of this type is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
    public class TemplateEditingVerb : DesignerVerb, IDisposable { 

        private static readonly EventHandler dummyEventHandler = new EventHandler(OnDummyEventHandler); 

        private ITemplateEditingFrame editingFrame;
        private int index;
 
        /// <include file='doc\TemplateEditingVerb.uex' path='docs/doc[@for="TemplateEditingVerb.TemplateEditingVerb"]/*' />
        public TemplateEditingVerb(string text, int index, TemplatedControlDesigner designer) : this(text, index, designer.TemplateEditingVerbHandler) { 
        } 

        public TemplateEditingVerb(string text, int index) : this(text, index, dummyEventHandler) { 
        }

        private TemplateEditingVerb(string text, int index, EventHandler handler) : base(text, handler) {
            this.index = index; 
        }
 
        internal ITemplateEditingFrame EditingFrame { 
            get {
                return editingFrame; 
            }
            set {
                editingFrame = value;
            } 
        }
 
        /// <include file='doc\TemplateEditingVerb.uex' path='docs/doc[@for="TemplateEditingVerb.Index"]/*' /> 
        public int Index {
            get { 
                return index;
            }
        }
 
        /// <include file='doc\TemplateEditingVerb.uex' path='docs/doc[@for="TemplateEditingVerb.Dispose"]/*' />
        public void Dispose() { 
            Dispose(true); 
            GC.SuppressFinalize(this);
        } 

        /// <include file='doc\TemplateEditingVerb.uex' path='docs/doc[@for="TemplateEditingVerb.Finalize"]/*' />
        ~TemplateEditingVerb() {
            Dispose(false); 
        }
 
        /// <include file='doc\TemplateEditingVerb.uex' path='docs/doc[@for="TemplateEditingVerb.Dispose2"]/*' /> 
        protected virtual void Dispose(bool disposing) {
            if (disposing) { 
                if (editingFrame != null) {
                    editingFrame.Dispose();
                    editingFrame = null;
                } 
            }
        } 
 
        private static void OnDummyEventHandler(object sender, EventArgs e) {
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
