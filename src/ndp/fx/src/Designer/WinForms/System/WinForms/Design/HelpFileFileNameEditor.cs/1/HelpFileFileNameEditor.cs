//------------------------------------------------------------------------------ 
// <copyright file="HelpFileFileNameEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.HelpNamespaceEditor..ctor()")]
 

namespace System.Windows.Forms.Design {
    using System.Design;
    using System.Runtime.Serialization.Formatters; 

    using System.Diagnostics; 
 
    using System;
    using System.Drawing; 
    using System.ComponentModel;
    using Microsoft.Win32;

    /// <include file='doc\HelpFileFileNameEditor.uex' path='docs/doc[@for="HelpNamespaceEditor"]/*' /> 
    /// <devdoc>
    ///     This is a filename editor for choosing help files. 
    /// </devdoc> 
    /// <internalonly/>
    internal class HelpNamespaceEditor : FileNameEditor { 

        /// <include file='doc\HelpFileFileNameEditor.uex' path='docs/doc[@for="HelpNamespaceEditor.InitializeDialog"]/*' />
        /// <devdoc>
        ///      Initializes the open file dialog when it is created.  This gives you 
        ///      an opportunity to configure the dialog as you please.  The default
        ///      implementation provides a generic file filter and title. 
        /// </devdoc> 
        protected override void InitializeDialog(OpenFileDialog openFileDialog) {
            openFileDialog.Filter = SR.GetString(SR.HelpProviderEditorFilter); 
            openFileDialog.Title = SR.GetString(SR.HelpProviderEditorTitle);
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="HelpFileFileNameEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.HelpNamespaceEditor..ctor()")]
 

namespace System.Windows.Forms.Design {
    using System.Design;
    using System.Runtime.Serialization.Formatters; 

    using System.Diagnostics; 
 
    using System;
    using System.Drawing; 
    using System.ComponentModel;
    using Microsoft.Win32;

    /// <include file='doc\HelpFileFileNameEditor.uex' path='docs/doc[@for="HelpNamespaceEditor"]/*' /> 
    /// <devdoc>
    ///     This is a filename editor for choosing help files. 
    /// </devdoc> 
    /// <internalonly/>
    internal class HelpNamespaceEditor : FileNameEditor { 

        /// <include file='doc\HelpFileFileNameEditor.uex' path='docs/doc[@for="HelpNamespaceEditor.InitializeDialog"]/*' />
        /// <devdoc>
        ///      Initializes the open file dialog when it is created.  This gives you 
        ///      an opportunity to configure the dialog as you please.  The default
        ///      implementation provides a generic file filter and title. 
        /// </devdoc> 
        protected override void InitializeDialog(OpenFileDialog openFileDialog) {
            openFileDialog.Filter = SR.GetString(SR.HelpProviderEditorFilter); 
            openFileDialog.Title = SR.GetString(SR.HelpProviderEditorTitle);
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
