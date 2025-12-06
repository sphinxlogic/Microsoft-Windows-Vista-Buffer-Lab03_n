//------------------------------------------------------------------------------ 
// <copyright file="ControlPersister.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Web; 
    using System.Web.UI;
    using System.Web.UI.HtmlControls;
    using System.Web.UI.WebControls;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Diagnostics; 
    using System.IO;
    using System.Reflection; 
    using System.Text;
    using AttributeCollection = System.Web.UI.AttributeCollection;

    /// <include file='doc\WebControlPersister.uex' path='docs/doc[@for="ControlPersister"]/*' /> 
    /// <devdoc>
    ///    <para> 
    ///       Provides helper functions used in persisting Controls. 
    ///    </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public sealed class ControlPersister {

        /// <include file='doc\WebControlPersister.uex' path='docs/doc[@for="ControlPersister.ControlPersister"]/*' /> 
        /// <devdoc>
        ///    We don't want instances of this class to be created, so mark 
        ///    the constructor as private. 
        /// </devdoc>
        private ControlPersister() { 
        }

        /// <include file='doc\WebControlPersister.uex' path='docs/doc[@for="ControlPersister.PersistInnerProperties"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets a string that can persist the inner properties of a control. 
        ///    </para> 
        /// </devdoc>
        public static string PersistInnerProperties(object component, IDesignerHost host) { 
            return ControlSerializer.SerializeInnerProperties(component, host);
        }

        /// <include file='doc\WebControlPersister.uex' path='docs/doc[@for="ControlPersister.PersistInnerProperties1"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Persists the inner properties of the control. 
        ///    </para>
        /// </devdoc> 
        public static void PersistInnerProperties(TextWriter sw, object component, IDesignerHost host) {
            ControlSerializer.SerializeInnerProperties(component, host, sw);
        }
 
        /// <include file='doc\WebControlPersister.uex' path='docs/doc[@for="ControlPersister.PersistControl"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets a string that can
        ///       persist a control. 
        ///    </para>
        /// </devdoc>
        public static string PersistControl(Control control) {
            return ControlSerializer.SerializeControl(control); 
        }
 
        /// <include file='doc\WebControlPersister.uex' path='docs/doc[@for="ControlPersister.PersistControl1"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Returns a string that can
        ///       persist a control.
        ///    </para>
        /// </devdoc> 
        public static string PersistControl(Control control, IDesignerHost host) {
            return ControlSerializer.SerializeControl(control, host); 
        } 

        /// <include file='doc\WebControlPersister.uex' path='docs/doc[@for="ControlPersister.PersistControl2"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Persists a control using the
        ///       specified string writer. 
        ///    </para>
        /// </devdoc> 
        public static void PersistControl(TextWriter sw, Control control) { 
            ControlSerializer.SerializeControl(control, sw);
        } 

        /// <include file='doc\WebControlPersister.uex' path='docs/doc[@for="ControlPersister.PersistControl3"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Persists a control using the
        ///       specified string writer. 
        ///    </para> 
        /// </devdoc>
        public static void PersistControl(TextWriter sw, Control control, IDesignerHost host) { 
            ControlSerializer.SerializeControl(control, host, sw);
        }

        public static string PersistTemplate(ITemplate template, IDesignerHost host) { 
            return ControlSerializer.SerializeTemplate(template, host);
        } 
 
        public static void PersistTemplate(TextWriter writer, ITemplate template, IDesignerHost host) {
            ControlSerializer.SerializeTemplate(template, writer, host); 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ControlPersister.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System;
    using System.Web; 
    using System.Web.UI;
    using System.Web.UI.HtmlControls;
    using System.Web.UI.WebControls;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Diagnostics; 
    using System.IO;
    using System.Reflection; 
    using System.Text;
    using AttributeCollection = System.Web.UI.AttributeCollection;

    /// <include file='doc\WebControlPersister.uex' path='docs/doc[@for="ControlPersister"]/*' /> 
    /// <devdoc>
    ///    <para> 
    ///       Provides helper functions used in persisting Controls. 
    ///    </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public sealed class ControlPersister {

        /// <include file='doc\WebControlPersister.uex' path='docs/doc[@for="ControlPersister.ControlPersister"]/*' /> 
        /// <devdoc>
        ///    We don't want instances of this class to be created, so mark 
        ///    the constructor as private. 
        /// </devdoc>
        private ControlPersister() { 
        }

        /// <include file='doc\WebControlPersister.uex' path='docs/doc[@for="ControlPersister.PersistInnerProperties"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets a string that can persist the inner properties of a control. 
        ///    </para> 
        /// </devdoc>
        public static string PersistInnerProperties(object component, IDesignerHost host) { 
            return ControlSerializer.SerializeInnerProperties(component, host);
        }

        /// <include file='doc\WebControlPersister.uex' path='docs/doc[@for="ControlPersister.PersistInnerProperties1"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Persists the inner properties of the control. 
        ///    </para>
        /// </devdoc> 
        public static void PersistInnerProperties(TextWriter sw, object component, IDesignerHost host) {
            ControlSerializer.SerializeInnerProperties(component, host, sw);
        }
 
        /// <include file='doc\WebControlPersister.uex' path='docs/doc[@for="ControlPersister.PersistControl"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets a string that can
        ///       persist a control. 
        ///    </para>
        /// </devdoc>
        public static string PersistControl(Control control) {
            return ControlSerializer.SerializeControl(control); 
        }
 
        /// <include file='doc\WebControlPersister.uex' path='docs/doc[@for="ControlPersister.PersistControl1"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Returns a string that can
        ///       persist a control.
        ///    </para>
        /// </devdoc> 
        public static string PersistControl(Control control, IDesignerHost host) {
            return ControlSerializer.SerializeControl(control, host); 
        } 

        /// <include file='doc\WebControlPersister.uex' path='docs/doc[@for="ControlPersister.PersistControl2"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Persists a control using the
        ///       specified string writer. 
        ///    </para>
        /// </devdoc> 
        public static void PersistControl(TextWriter sw, Control control) { 
            ControlSerializer.SerializeControl(control, sw);
        } 

        /// <include file='doc\WebControlPersister.uex' path='docs/doc[@for="ControlPersister.PersistControl3"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Persists a control using the
        ///       specified string writer. 
        ///    </para> 
        /// </devdoc>
        public static void PersistControl(TextWriter sw, Control control, IDesignerHost host) { 
            ControlSerializer.SerializeControl(control, host, sw);
        }

        public static string PersistTemplate(ITemplate template, IDesignerHost host) { 
            return ControlSerializer.SerializeTemplate(template, host);
        } 
 
        public static void PersistTemplate(TextWriter writer, ITemplate template, IDesignerHost host) {
            ControlSerializer.SerializeTemplate(template, writer, host); 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
