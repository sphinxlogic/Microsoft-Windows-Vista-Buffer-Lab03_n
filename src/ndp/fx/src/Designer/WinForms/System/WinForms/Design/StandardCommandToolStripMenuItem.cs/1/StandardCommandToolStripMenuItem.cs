//------------------------------------------------------------------------------ 
// <copyright file="StandardCommandToolStripMenuItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design
{ 
    using System.Design;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 
    using System;
    using System.Security; 
    using System.Security.Permissions; 
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Drawing;
    using System.Drawing.Design;
    using System.Windows.Forms.Design.Behavior;
    using System.Runtime.InteropServices; 
    using System.Drawing.Drawing2D;
 
 
    /// <include file='doc\StandardCommandToolStripMenuItem.uex' path='docs/doc[@for="StandardCommandToolStripMenuItem"]/*' />
    /// <devdoc> 
    ///      Associates standard command with ToolStripMenuItem.
    /// </devdoc>
    /// <internalonly/>
    internal class StandardCommandToolStripMenuItem : ToolStripMenuItem 
    {
        private bool _cachedImage = false; 
        private Image _image = null; 
        private CommandID menuID;
        private IMenuCommandService menuCommandService; 
        private IServiceProvider serviceProvider;
        private string name;

        private MenuCommand menuCommand; 

 
        // Ok to call MenuService.FindComand to find the menuCommand mapping to the appropriated menuID. 
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        public StandardCommandToolStripMenuItem(CommandID menuID, string text, string imageName, IServiceProvider serviceProvider)
        {
            this.menuID = menuID; 
            this.serviceProvider = serviceProvider;
            // Findcommand can throw; so we need to catch and disable the command. 
            try 
            {
                menuCommand = MenuService.FindCommand(menuID); 
            }
            catch
            {
                this.Enabled = false; 
            }
 
            this.Text = text; 
            this.name = imageName;
 
            RefreshItem();
        }

        public void RefreshItem() 
        {
            if (menuCommand != null) 
            { 
                this.Visible = menuCommand.Visible;
                this.Enabled = menuCommand.Enabled; 
                this.Checked = menuCommand.Checked;
            }
        }
 
        /// <include file='doc\ToolStripKeyboardHandlingService.uex' path='docs/doc[@for="ToolStripKeyboardHandlingService.MenuService"]/*' />
        /// <devdoc> 
        ///     Retrieves the menu editor service, which we cache for speed. 
        /// </devdoc>
        public IMenuCommandService MenuService 
        {
            get
            {
                if (menuCommandService == null) 
                {
                    menuCommandService = (IMenuCommandService)serviceProvider.GetService(typeof(IMenuCommandService)); 
                } 
                return menuCommandService;
            } 
        }

        public override Image Image
        { 
            // Standard 'catch all - rethrow critical' exception pattern
            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
            get 
            {
                // Defer loading the image until we're sure we need it 
                if (!_cachedImage)
                {
                    _cachedImage = true;
                    try 
                    {
                        if (name != null) 
                        { 
                            _image = new Bitmap(typeof(ToolStripMenuItem), name + ".bmp");
                        } 
                        this.ImageTransparentColor = Color.Magenta;
                    }
                    catch (Exception ex)
                    { 
                        if (ClientUtils.IsCriticalException(ex))
                        { 
                            throw; 
                        }
                    } 
                    catch
                    {
                    }
                } 
                return _image;
            } 
            set 
            {
                _image = value; 
                _cachedImage = true;
            }
        }
 
        protected override void OnClick(System.EventArgs e)
        { 
            if (menuCommand != null) 
            {
                menuCommand.Invoke(); 
            }
            else if (MenuService != null)
            {
                if (MenuService.GlobalInvoke(menuID)) 
                {
                    return; 
                } 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="StandardCommandToolStripMenuItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design
{ 
    using System.Design;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 
    using System;
    using System.Security; 
    using System.Security.Permissions; 
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Drawing;
    using System.Drawing.Design;
    using System.Windows.Forms.Design.Behavior;
    using System.Runtime.InteropServices; 
    using System.Drawing.Drawing2D;
 
 
    /// <include file='doc\StandardCommandToolStripMenuItem.uex' path='docs/doc[@for="StandardCommandToolStripMenuItem"]/*' />
    /// <devdoc> 
    ///      Associates standard command with ToolStripMenuItem.
    /// </devdoc>
    /// <internalonly/>
    internal class StandardCommandToolStripMenuItem : ToolStripMenuItem 
    {
        private bool _cachedImage = false; 
        private Image _image = null; 
        private CommandID menuID;
        private IMenuCommandService menuCommandService; 
        private IServiceProvider serviceProvider;
        private string name;

        private MenuCommand menuCommand; 

 
        // Ok to call MenuService.FindComand to find the menuCommand mapping to the appropriated menuID. 
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        public StandardCommandToolStripMenuItem(CommandID menuID, string text, string imageName, IServiceProvider serviceProvider)
        {
            this.menuID = menuID; 
            this.serviceProvider = serviceProvider;
            // Findcommand can throw; so we need to catch and disable the command. 
            try 
            {
                menuCommand = MenuService.FindCommand(menuID); 
            }
            catch
            {
                this.Enabled = false; 
            }
 
            this.Text = text; 
            this.name = imageName;
 
            RefreshItem();
        }

        public void RefreshItem() 
        {
            if (menuCommand != null) 
            { 
                this.Visible = menuCommand.Visible;
                this.Enabled = menuCommand.Enabled; 
                this.Checked = menuCommand.Checked;
            }
        }
 
        /// <include file='doc\ToolStripKeyboardHandlingService.uex' path='docs/doc[@for="ToolStripKeyboardHandlingService.MenuService"]/*' />
        /// <devdoc> 
        ///     Retrieves the menu editor service, which we cache for speed. 
        /// </devdoc>
        public IMenuCommandService MenuService 
        {
            get
            {
                if (menuCommandService == null) 
                {
                    menuCommandService = (IMenuCommandService)serviceProvider.GetService(typeof(IMenuCommandService)); 
                } 
                return menuCommandService;
            } 
        }

        public override Image Image
        { 
            // Standard 'catch all - rethrow critical' exception pattern
            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
            get 
            {
                // Defer loading the image until we're sure we need it 
                if (!_cachedImage)
                {
                    _cachedImage = true;
                    try 
                    {
                        if (name != null) 
                        { 
                            _image = new Bitmap(typeof(ToolStripMenuItem), name + ".bmp");
                        } 
                        this.ImageTransparentColor = Color.Magenta;
                    }
                    catch (Exception ex)
                    { 
                        if (ClientUtils.IsCriticalException(ex))
                        { 
                            throw; 
                        }
                    } 
                    catch
                    {
                    }
                } 
                return _image;
            } 
            set 
            {
                _image = value; 
                _cachedImage = true;
            }
        }
 
        protected override void OnClick(System.EventArgs e)
        { 
            if (menuCommand != null) 
            {
                menuCommand.Invoke(); 
            }
            else if (MenuService != null)
            {
                if (MenuService.GlobalInvoke(menuID)) 
                {
                    return; 
                } 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
