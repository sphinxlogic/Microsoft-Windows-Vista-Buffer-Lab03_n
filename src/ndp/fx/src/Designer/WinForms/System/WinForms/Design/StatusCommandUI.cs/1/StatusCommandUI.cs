//------------------------------------------------------------------------------ 
// <copyright file="StatusCommandUI.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design
{ 
    using System.Design;
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Diagnostics; 
    using System;
    using System.Collections; 
    using Microsoft.Win32; 
    using System.ComponentModel.Design;
    using System.Drawing; 
    using System.Windows.Forms;
    using System.Windows.Forms.ComponentModel;
    using System.Windows.Forms.Design;
    using System.Windows.Forms.Design.Behavior; 

 
 
    /// <include file='doc\StatusCommandUI.uex' path='docs/doc[@for="StatusCommandUI"]/*' />
    /// <devdoc> 
    ///      This class provides a single entrypoint used by the Behaviors, KeySize and KeyMoves (in CommandSets) and SelectionService to update
    ///      the StatusBar Information.
    /// </devdoc>
    internal class StatusCommandUI 
    {
        MenuCommand statusRectCommand = null; 
        IMenuCommandService menuService = null; 
        IServiceProvider serviceProvider;
 
        public StatusCommandUI(IServiceProvider provider)
        {
            this.serviceProvider = provider;
        } 

        /// <include file='doc\StatusCommandUI.uex' path='docs/doc[@for="StatusCommandUI.MenuService"]/*' /> 
        /// <devdoc> 
        ///     Retrieves the menu editor service, which we cache for speed.
        /// </devdoc> 
        private IMenuCommandService MenuService
        {
            get
            { 
                if (menuService == null)
                { 
                    menuService = (IMenuCommandService)serviceProvider.GetService(typeof(IMenuCommandService)); 
                }
                return menuService; 
            }
        }

        /// <include file='doc\StatusCommandUI.uex' path='docs/doc[@for="StatusCommandUI.StatusRectCommand"]/*' /> 
        /// <devdoc>
        ///     Retrieves the actual StatusRectCommand, which we cache for speed. 
        /// </devdoc> 
        private MenuCommand StatusRectCommand
        { 
            get
            {
                if (statusRectCommand == null)
                { 
                    if (MenuService != null)
                    { 
                        statusRectCommand = MenuService.FindCommand(MenuCommands.SetStatusRectangle); 
                    }
                } 
                return statusRectCommand;
            }
        }
 
        /// <include file='doc\StatusCommandUI.uex' path='docs/doc[@for="StatusCommandUI.SetStatusInformation"]/*' />
        /// <devdoc> 
        ///     Actual Function which invokes the command. 
        /// </devdoc>
        public void SetStatusInformation(Component selectedComponent, Point location) 
        {
            if (selectedComponent == null)
            {
 		return; 
	    }
            Rectangle bounds = Rectangle.Empty; 
            Control c = selectedComponent as Control; 

            if (c != null) { 
                bounds = c.Bounds;
            }
            else{
                PropertyDescriptor BoundsProp = TypeDescriptor.GetProperties(selectedComponent)["Bounds"]; 
                if (BoundsProp != null && typeof(Rectangle).IsAssignableFrom(BoundsProp.PropertyType)) {
                    bounds = (Rectangle)BoundsProp.GetValue(selectedComponent); 
                } 
            }
            if (location != Point.Empty) 
            {
              bounds.X = location.X;
              bounds.Y = location.Y;
            } 
			if (StatusRectCommand != null)
			{ 
 				StatusRectCommand.Invoke(bounds); 
			}
 		} 

        /// <include file='doc\StatusCommandUI.uex' path='docs/doc[@for="StatusCommandUI.SetStatusInformation"]/*' />
        /// <devdoc>
        ///     Actual Function which invokes the command. 
        /// </devdoc>
        public void SetStatusInformation(Component selectedComponent) 
        { 
            if (selectedComponent == null)
            { 
 		return;
	    }
            Rectangle bounds = Rectangle.Empty;
            Control c = selectedComponent as Control; 

            if (c != null) { 
                bounds = c.Bounds; 
            }
            else{ 
                PropertyDescriptor BoundsProp = TypeDescriptor.GetProperties(selectedComponent)["Bounds"];
                if (BoundsProp != null && typeof(Rectangle).IsAssignableFrom(BoundsProp.PropertyType)) {
                    bounds = (Rectangle)BoundsProp.GetValue(selectedComponent);
                } 
            }
            if (StatusRectCommand != null) 
            { 
                StatusRectCommand.Invoke(bounds);
            } 
        }

        /// <include file='doc\StatusCommandUI.uex' path='docs/doc[@for="StatusCommandUI.SetStatusInformation"]/*' />
        /// <devdoc> 
        ///     Actual Function which invokes the command.
        /// </devdoc> 
        public void SetStatusInformation(Rectangle bounds) 
        {
            if (StatusRectCommand != null) 
            {
                StatusRectCommand.Invoke(bounds);
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="StatusCommandUI.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design
{ 
    using System.Design;
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Diagnostics; 
    using System;
    using System.Collections; 
    using Microsoft.Win32; 
    using System.ComponentModel.Design;
    using System.Drawing; 
    using System.Windows.Forms;
    using System.Windows.Forms.ComponentModel;
    using System.Windows.Forms.Design;
    using System.Windows.Forms.Design.Behavior; 

 
 
    /// <include file='doc\StatusCommandUI.uex' path='docs/doc[@for="StatusCommandUI"]/*' />
    /// <devdoc> 
    ///      This class provides a single entrypoint used by the Behaviors, KeySize and KeyMoves (in CommandSets) and SelectionService to update
    ///      the StatusBar Information.
    /// </devdoc>
    internal class StatusCommandUI 
    {
        MenuCommand statusRectCommand = null; 
        IMenuCommandService menuService = null; 
        IServiceProvider serviceProvider;
 
        public StatusCommandUI(IServiceProvider provider)
        {
            this.serviceProvider = provider;
        } 

        /// <include file='doc\StatusCommandUI.uex' path='docs/doc[@for="StatusCommandUI.MenuService"]/*' /> 
        /// <devdoc> 
        ///     Retrieves the menu editor service, which we cache for speed.
        /// </devdoc> 
        private IMenuCommandService MenuService
        {
            get
            { 
                if (menuService == null)
                { 
                    menuService = (IMenuCommandService)serviceProvider.GetService(typeof(IMenuCommandService)); 
                }
                return menuService; 
            }
        }

        /// <include file='doc\StatusCommandUI.uex' path='docs/doc[@for="StatusCommandUI.StatusRectCommand"]/*' /> 
        /// <devdoc>
        ///     Retrieves the actual StatusRectCommand, which we cache for speed. 
        /// </devdoc> 
        private MenuCommand StatusRectCommand
        { 
            get
            {
                if (statusRectCommand == null)
                { 
                    if (MenuService != null)
                    { 
                        statusRectCommand = MenuService.FindCommand(MenuCommands.SetStatusRectangle); 
                    }
                } 
                return statusRectCommand;
            }
        }
 
        /// <include file='doc\StatusCommandUI.uex' path='docs/doc[@for="StatusCommandUI.SetStatusInformation"]/*' />
        /// <devdoc> 
        ///     Actual Function which invokes the command. 
        /// </devdoc>
        public void SetStatusInformation(Component selectedComponent, Point location) 
        {
            if (selectedComponent == null)
            {
 		return; 
	    }
            Rectangle bounds = Rectangle.Empty; 
            Control c = selectedComponent as Control; 

            if (c != null) { 
                bounds = c.Bounds;
            }
            else{
                PropertyDescriptor BoundsProp = TypeDescriptor.GetProperties(selectedComponent)["Bounds"]; 
                if (BoundsProp != null && typeof(Rectangle).IsAssignableFrom(BoundsProp.PropertyType)) {
                    bounds = (Rectangle)BoundsProp.GetValue(selectedComponent); 
                } 
            }
            if (location != Point.Empty) 
            {
              bounds.X = location.X;
              bounds.Y = location.Y;
            } 
			if (StatusRectCommand != null)
			{ 
 				StatusRectCommand.Invoke(bounds); 
			}
 		} 

        /// <include file='doc\StatusCommandUI.uex' path='docs/doc[@for="StatusCommandUI.SetStatusInformation"]/*' />
        /// <devdoc>
        ///     Actual Function which invokes the command. 
        /// </devdoc>
        public void SetStatusInformation(Component selectedComponent) 
        { 
            if (selectedComponent == null)
            { 
 		return;
	    }
            Rectangle bounds = Rectangle.Empty;
            Control c = selectedComponent as Control; 

            if (c != null) { 
                bounds = c.Bounds; 
            }
            else{ 
                PropertyDescriptor BoundsProp = TypeDescriptor.GetProperties(selectedComponent)["Bounds"];
                if (BoundsProp != null && typeof(Rectangle).IsAssignableFrom(BoundsProp.PropertyType)) {
                    bounds = (Rectangle)BoundsProp.GetValue(selectedComponent);
                } 
            }
            if (StatusRectCommand != null) 
            { 
                StatusRectCommand.Invoke(bounds);
            } 
        }

        /// <include file='doc\StatusCommandUI.uex' path='docs/doc[@for="StatusCommandUI.SetStatusInformation"]/*' />
        /// <devdoc> 
        ///     Actual Function which invokes the command.
        /// </devdoc> 
        public void SetStatusInformation(Rectangle bounds) 
        {
            if (StatusRectCommand != null) 
            {
                StatusRectCommand.Invoke(bounds);
            }
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
