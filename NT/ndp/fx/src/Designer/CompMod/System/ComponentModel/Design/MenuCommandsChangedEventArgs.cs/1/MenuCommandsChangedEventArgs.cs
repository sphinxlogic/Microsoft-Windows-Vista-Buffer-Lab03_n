//------------------------------------------------------------------------------ 
// <copyright file="MenuCommandsChangedEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
    using System.ComponentModel; 
    using System;
    using Microsoft.Win32; 

    /// <include file='doc\MenuCommandsChangedEventArgs.uex' path='docs/doc[@for="MenuCommandsChangedEventArgs"]/*' />
    /// <devdoc>
    ///     This EventArgs class is used by the MenuCommandService to signify 
    ///     that there has been a change in MenuCommands (added or removed)
    ///     on the related object. 
    /// </devdoc> 
    [System.Runtime.InteropServices.ComVisible(true)]
    public class MenuCommandsChangedEventArgs : EventArgs { 

        private MenuCommand command;
        private MenuCommandsChangedType changeType;//type of change
 
        /// <include file='doc\MenuCommandsChangedEventArgs.uex' path='docs/doc[@for="MenuCommandsChangedEventArgs.MenuCommandsChangedEventArgs"]/*' />
        /// <devdoc> 
        ///     Constructor that requires the object in question, the type of change 
        ///     and the remaining commands left for the object.  "command" can be null
        ///     to signify multiple commands changed at once. 
        /// </devdoc>
        public MenuCommandsChangedEventArgs(MenuCommandsChangedType changeType, MenuCommand command) {
            this.changeType = changeType;
            this.command = command; 
        }
 
        /// <include file='doc\MenuCommandsChangedEventArgs.uex' path='docs/doc[@for="MenuCommandsChangedEventArgs.ChangeType"]/*' /> 
        /// <devdoc>
        ///     The type of changed that caused the related event 
        ///     to be thrown.
        /// </devdoc>
        public MenuCommandsChangedType ChangeType {
            get { 
                return changeType;
            } 
        } 

        /// <include file='doc\MenuCommandsChangedEventArgs.uex' path='docs/doc[@for="MenuCommandsChangedEventArgs.Commands"]/*' /> 
        /// <devdoc>
        ///     The command that was added/removed/changed.  This can be null if more than one command changed at once.
        /// </devdoc>
        public MenuCommand Command { 
            get {
                return command; 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="MenuCommandsChangedEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
    using System.ComponentModel; 
    using System;
    using Microsoft.Win32; 

    /// <include file='doc\MenuCommandsChangedEventArgs.uex' path='docs/doc[@for="MenuCommandsChangedEventArgs"]/*' />
    /// <devdoc>
    ///     This EventArgs class is used by the MenuCommandService to signify 
    ///     that there has been a change in MenuCommands (added or removed)
    ///     on the related object. 
    /// </devdoc> 
    [System.Runtime.InteropServices.ComVisible(true)]
    public class MenuCommandsChangedEventArgs : EventArgs { 

        private MenuCommand command;
        private MenuCommandsChangedType changeType;//type of change
 
        /// <include file='doc\MenuCommandsChangedEventArgs.uex' path='docs/doc[@for="MenuCommandsChangedEventArgs.MenuCommandsChangedEventArgs"]/*' />
        /// <devdoc> 
        ///     Constructor that requires the object in question, the type of change 
        ///     and the remaining commands left for the object.  "command" can be null
        ///     to signify multiple commands changed at once. 
        /// </devdoc>
        public MenuCommandsChangedEventArgs(MenuCommandsChangedType changeType, MenuCommand command) {
            this.changeType = changeType;
            this.command = command; 
        }
 
        /// <include file='doc\MenuCommandsChangedEventArgs.uex' path='docs/doc[@for="MenuCommandsChangedEventArgs.ChangeType"]/*' /> 
        /// <devdoc>
        ///     The type of changed that caused the related event 
        ///     to be thrown.
        /// </devdoc>
        public MenuCommandsChangedType ChangeType {
            get { 
                return changeType;
            } 
        } 

        /// <include file='doc\MenuCommandsChangedEventArgs.uex' path='docs/doc[@for="MenuCommandsChangedEventArgs.Commands"]/*' /> 
        /// <devdoc>
        ///     The command that was added/removed/changed.  This can be null if more than one command changed at once.
        /// </devdoc>
        public MenuCommand Command { 
            get {
                return command; 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
