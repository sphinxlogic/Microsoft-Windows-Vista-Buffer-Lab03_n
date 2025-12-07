//------------------------------------------------------------------------------ 
// <copyright file="FileDialogCustomPlacesCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

using System; 
using System.Collections.ObjectModel; 
using System.IO;
using System.Security; 
using System.Security.Permissions;

namespace System.Windows.Forms
{ 
    public class FileDialogCustomPlacesCollection : Collection<FileDialogCustomPlace>
    { 
        internal void Apply(FileDialogNative.IFileDialog dialog) 
        {
            //Assert FileIOPermission for getting the paths for the favorites 
            new FileIOPermission(PermissionState.Unrestricted).Assert();
            //Walk backwards
            for (int i = this.Items.Count - 1; i >= 0; --i)
            { 
                FileDialogCustomPlace customPlace = this.Items[i];
                try 
                { 
                    FileDialogNative.IShellItem shellItem = customPlace.GetNativePath();
                    if (null != shellItem) 
                    {
                        dialog.AddPlace(shellItem, 0);
                    }
                } 
                catch (FileNotFoundException)
                { 
                } 
                //Silently absorb FileNotFound exceptions (these could be caused by a path that disappeared after the place was added to the dialog).
            } 
        }

        public void Add(string path)
        { 
            Add(new FileDialogCustomPlace(path));
        } 
 
        public void Add(Guid knownFolderGuid)
        { 
            Add(new FileDialogCustomPlace(knownFolderGuid));
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="FileDialogCustomPlacesCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

using System; 
using System.Collections.ObjectModel; 
using System.IO;
using System.Security; 
using System.Security.Permissions;

namespace System.Windows.Forms
{ 
    public class FileDialogCustomPlacesCollection : Collection<FileDialogCustomPlace>
    { 
        internal void Apply(FileDialogNative.IFileDialog dialog) 
        {
            //Assert FileIOPermission for getting the paths for the favorites 
            new FileIOPermission(PermissionState.Unrestricted).Assert();
            //Walk backwards
            for (int i = this.Items.Count - 1; i >= 0; --i)
            { 
                FileDialogCustomPlace customPlace = this.Items[i];
                try 
                { 
                    FileDialogNative.IShellItem shellItem = customPlace.GetNativePath();
                    if (null != shellItem) 
                    {
                        dialog.AddPlace(shellItem, 0);
                    }
                } 
                catch (FileNotFoundException)
                { 
                } 
                //Silently absorb FileNotFound exceptions (these could be caused by a path that disappeared after the place was added to the dialog).
            } 
        }

        public void Add(string path)
        { 
            Add(new FileDialogCustomPlace(path));
        } 
 
        public void Add(Guid knownFolderGuid)
        { 
            Add(new FileDialogCustomPlace(knownFolderGuid));
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
