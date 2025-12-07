//------------------------------------------------------------------------------ 
// <copyright file="WizardStepCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Design;
    using System.Reflection;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Web.UI.WebControls;
 
    /// <include file='doc\WizardStepCollectionEditor.uex' path='docs/doc[@for="WizardStepCollectionEditor"]/*' /> 
    /// <devdoc>
    ///    <para>Provides an editor to edit wizardsteps in a Wizard.</para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class WizardStepCollectionEditor : CollectionEditor {
 
        /// <include file='doc\WizardStepCollectionEditor.uex' path='docs/doc[@for="WizardStepCollectionEditor.WizardStepCollectionEditor"]/*' />
        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.Design.WebControls.WizardStepCollectionEditor'/> class.</para> 
        /// </devdoc>
        public WizardStepCollectionEditor(Type type) : base(type) { 
        }

        /// <include file='doc\WizardStepCollectionEditor.uex' path='docs/doc[@for="WizardStepCollectionEditor.CanSelectMultipleInstances"]/*' />
        /// <devdoc> 
        ///    <para>Gets a value indicating whether multiple instances may be selected.</para>
        /// </devdoc> 
        protected override bool CanSelectMultipleInstances() { 
            return false;
        } 

        protected override CollectionForm CreateCollectionForm() {
            CollectionForm form = base.CreateCollectionForm();
            form.Text = SR.GetString(SR.CollectionEditorCaption, "WizardStep"); 

            return form; 
        } 

        /// <include file='doc\WizardStepCollectionEditor.uex' path='docs/doc[@for="WizardStepCollectionEditor.CreateInstance"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override object CreateInstance(Type itemType) { 
            return Activator.CreateInstance(itemType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, null, null);
        } 
 
        /// <include file='doc\WizardStepCollectionEditor.uex' path='docs/doc[@for="WizardStepCollectionEditor.CreateNewItemTypes"]/*' />
        /// <devdoc> 
        ///      Retrieves the data types this collection can contain.  The default
        ///      implementation looks inside of the collection for the Item property
        ///      and returns the returning datatype of the item.  Do not call this
        ///      method directly.  Instead, use the ItemTypes property.  Use this 
        ///      method to override the default implementation.
        /// </devdoc> 
        protected override Type[] CreateNewItemTypes() { 
            return new Type[] {
                typeof(WizardStep), 
                typeof(TemplatedWizardStep)
            };
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="WizardStepCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Design;
    using System.Reflection;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Web.UI.WebControls;
 
    /// <include file='doc\WizardStepCollectionEditor.uex' path='docs/doc[@for="WizardStepCollectionEditor"]/*' /> 
    /// <devdoc>
    ///    <para>Provides an editor to edit wizardsteps in a Wizard.</para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class WizardStepCollectionEditor : CollectionEditor {
 
        /// <include file='doc\WizardStepCollectionEditor.uex' path='docs/doc[@for="WizardStepCollectionEditor.WizardStepCollectionEditor"]/*' />
        /// <devdoc> 
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.Design.WebControls.WizardStepCollectionEditor'/> class.</para> 
        /// </devdoc>
        public WizardStepCollectionEditor(Type type) : base(type) { 
        }

        /// <include file='doc\WizardStepCollectionEditor.uex' path='docs/doc[@for="WizardStepCollectionEditor.CanSelectMultipleInstances"]/*' />
        /// <devdoc> 
        ///    <para>Gets a value indicating whether multiple instances may be selected.</para>
        /// </devdoc> 
        protected override bool CanSelectMultipleInstances() { 
            return false;
        } 

        protected override CollectionForm CreateCollectionForm() {
            CollectionForm form = base.CreateCollectionForm();
            form.Text = SR.GetString(SR.CollectionEditorCaption, "WizardStep"); 

            return form; 
        } 

        /// <include file='doc\WizardStepCollectionEditor.uex' path='docs/doc[@for="WizardStepCollectionEditor.CreateInstance"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override object CreateInstance(Type itemType) { 
            return Activator.CreateInstance(itemType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, null, null);
        } 
 
        /// <include file='doc\WizardStepCollectionEditor.uex' path='docs/doc[@for="WizardStepCollectionEditor.CreateNewItemTypes"]/*' />
        /// <devdoc> 
        ///      Retrieves the data types this collection can contain.  The default
        ///      implementation looks inside of the collection for the Item property
        ///      and returns the returning datatype of the item.  Do not call this
        ///      method directly.  Instead, use the ItemTypes property.  Use this 
        ///      method to override the default implementation.
        /// </devdoc> 
        protected override Type[] CreateNewItemTypes() { 
            return new Type[] {
                typeof(WizardStep), 
                typeof(TemplatedWizardStep)
            };
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
