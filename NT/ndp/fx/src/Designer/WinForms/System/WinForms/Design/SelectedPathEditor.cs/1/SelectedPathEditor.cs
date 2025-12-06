//------------------------------------------------------------------------------ 
// <copyright file="WorkingDirectoryEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.SelectedPathEditor..ctor()")]
namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.Windows.Forms.Design; 

    /// <devdoc>
    ///     Folder editor for choosing the initial folder of the folder browser dialog.
    /// </devdoc> 
    internal class SelectedPathEditor : FolderNameEditor
    { 
        protected override void InitializeDialog(FolderBrowser folderBrowser) 
        {
            folderBrowser.Description = System.Design.SR.GetString(System.Design.SR.SelectedPathEditorLabel); 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="WorkingDirectoryEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.SelectedPathEditor..ctor()")]
namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.Windows.Forms.Design; 

    /// <devdoc>
    ///     Folder editor for choosing the initial folder of the folder browser dialog.
    /// </devdoc> 
    internal class SelectedPathEditor : FolderNameEditor
    { 
        protected override void InitializeDialog(FolderBrowser folderBrowser) 
        {
            folderBrowser.Description = System.Design.SR.GetString(System.Design.SR.SelectedPathEditorLabel); 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
