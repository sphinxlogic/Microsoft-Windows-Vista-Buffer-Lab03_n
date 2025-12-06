//------------------------------------------------------------------------------ 
// <copyright file="UIServiceHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.Util { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Design;
    using System.Drawing;
    using System.Web.UI.Design; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design; 
 
    /// <devdoc>
    /// Helper class to assist control designers with UI services. 
    /// </devdoc>
    internal static class UIServiceHelper {

        public static Font GetDialogFont(IServiceProvider serviceProvider) { 
            if (serviceProvider != null) {
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService)); 
                if (uiService != null) { 
                    IDictionary uiStyles = uiService.Styles;
                    if (uiStyles != null) { 
                        return (Font)uiStyles["DialogFont"];
                    }
                }
            } 
            return null;
        } 
 
        public static IWin32Window GetDialogOwnerWindow(IServiceProvider serviceProvider) {
            if (serviceProvider != null) { 
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService));
                if (uiService != null) {
                    return uiService.GetDialogOwnerWindow();
                } 
            }
            return null; 
        } 

        public static ToolStripRenderer GetToolStripRenderer(IServiceProvider serviceProvider) { 
            if (serviceProvider != null) {
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService));
                if (uiService != null) {
                    IDictionary uiStyles = uiService.Styles; 
                    if (uiStyles != null) {
                        return (ToolStripRenderer)uiStyles["VsRenderer"]; 
                    } 
                }
            } 
            return null;
        }

        public static DialogResult ShowDialog(IServiceProvider serviceProvider, Form form) { 
            if (serviceProvider != null) {
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService)); 
                if (uiService != null) { 
                    return uiService.ShowDialog(form);
                } 
            }

            return form.ShowDialog();
        } 

        public static void ShowError(IServiceProvider serviceProvider, string message) { 
            if (serviceProvider != null) { 
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService));
                if (uiService != null) { 
                    uiService.ShowError(message);
                    return;
                }
            } 

            RTLAwareMessageBox.Show(null, message, SR.GetString(SR.UIServiceHelper_ErrorCaption), MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0); 
        } 

        /* 
        This method is not currently used. Uncomment it if you need it.
        public static void ShowError(IServiceProvider serviceProvider, Exception ex) {
            if (serviceProvider != null) {
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService)); 
                if (uiService != null) {
                    uiService.ShowError(ex); 
                    return; 
                }
            } 

            string message = String.Empty;
            if (ex != null) {
                message = ex.Message; 
            }
            RTLAwareMessageBox.Show(null, message, SR.GetString(SR.UIServiceHelper_ErrorCaption), MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0); 
        } 
        */
 
        public static void ShowError(IServiceProvider serviceProvider, Exception ex, string message) {
            if (ex != null) {
                message += Environment.NewLine + Environment.NewLine + ex.Message;
            } 

            if (serviceProvider != null) { 
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService)); 
                if (uiService != null) {
                    // We specifically don't call ShowError(ex, message) because the IUIService 
                    // implementation in VS ignores the Exception parameter when the message
                    // parameter is set, and we'd like to show the user both messages.
                    uiService.ShowError(message);
                    return; 
                }
            } 
 
            RTLAwareMessageBox.Show(null, message, SR.GetString(SR.UIServiceHelper_ErrorCaption), MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0);
        } 

        public static void ShowMessage(IServiceProvider serviceProvider, string message) {
            if (serviceProvider != null) {
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService)); 
                if (uiService != null) {
                    uiService.ShowMessage(message); 
                    return; 
                }
            } 

            RTLAwareMessageBox.Show(null, message, String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, 0);
        }
 
        /*
        This method is not currently used. Uncomment it if you need it. 
        public static void ShowMessage(IServiceProvider serviceProvider, string message, string caption) { 
            if (serviceProvider != null) {
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService)); 
                if (uiService != null) {
                    uiService.ShowMessage(message, caption);
                    return;
                } 
            }
 
            RTLAwareMessageBox.Show(null, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, 0); 
        }
        */ 

        public static DialogResult ShowMessage(IServiceProvider serviceProvider, string message, string caption, MessageBoxButtons buttons) {
            if (serviceProvider != null) {
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService)); 
                if (uiService != null) {
                    return uiService.ShowMessage(message, caption, buttons); 
                } 
            }
 
            return RTLAwareMessageBox.Show(null, message, caption, buttons, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, 0);
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="UIServiceHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.Util { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Design;
    using System.Drawing;
    using System.Web.UI.Design; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design; 
 
    /// <devdoc>
    /// Helper class to assist control designers with UI services. 
    /// </devdoc>
    internal static class UIServiceHelper {

        public static Font GetDialogFont(IServiceProvider serviceProvider) { 
            if (serviceProvider != null) {
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService)); 
                if (uiService != null) { 
                    IDictionary uiStyles = uiService.Styles;
                    if (uiStyles != null) { 
                        return (Font)uiStyles["DialogFont"];
                    }
                }
            } 
            return null;
        } 
 
        public static IWin32Window GetDialogOwnerWindow(IServiceProvider serviceProvider) {
            if (serviceProvider != null) { 
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService));
                if (uiService != null) {
                    return uiService.GetDialogOwnerWindow();
                } 
            }
            return null; 
        } 

        public static ToolStripRenderer GetToolStripRenderer(IServiceProvider serviceProvider) { 
            if (serviceProvider != null) {
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService));
                if (uiService != null) {
                    IDictionary uiStyles = uiService.Styles; 
                    if (uiStyles != null) {
                        return (ToolStripRenderer)uiStyles["VsRenderer"]; 
                    } 
                }
            } 
            return null;
        }

        public static DialogResult ShowDialog(IServiceProvider serviceProvider, Form form) { 
            if (serviceProvider != null) {
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService)); 
                if (uiService != null) { 
                    return uiService.ShowDialog(form);
                } 
            }

            return form.ShowDialog();
        } 

        public static void ShowError(IServiceProvider serviceProvider, string message) { 
            if (serviceProvider != null) { 
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService));
                if (uiService != null) { 
                    uiService.ShowError(message);
                    return;
                }
            } 

            RTLAwareMessageBox.Show(null, message, SR.GetString(SR.UIServiceHelper_ErrorCaption), MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0); 
        } 

        /* 
        This method is not currently used. Uncomment it if you need it.
        public static void ShowError(IServiceProvider serviceProvider, Exception ex) {
            if (serviceProvider != null) {
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService)); 
                if (uiService != null) {
                    uiService.ShowError(ex); 
                    return; 
                }
            } 

            string message = String.Empty;
            if (ex != null) {
                message = ex.Message; 
            }
            RTLAwareMessageBox.Show(null, message, SR.GetString(SR.UIServiceHelper_ErrorCaption), MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0); 
        } 
        */
 
        public static void ShowError(IServiceProvider serviceProvider, Exception ex, string message) {
            if (ex != null) {
                message += Environment.NewLine + Environment.NewLine + ex.Message;
            } 

            if (serviceProvider != null) { 
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService)); 
                if (uiService != null) {
                    // We specifically don't call ShowError(ex, message) because the IUIService 
                    // implementation in VS ignores the Exception parameter when the message
                    // parameter is set, and we'd like to show the user both messages.
                    uiService.ShowError(message);
                    return; 
                }
            } 
 
            RTLAwareMessageBox.Show(null, message, SR.GetString(SR.UIServiceHelper_ErrorCaption), MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0);
        } 

        public static void ShowMessage(IServiceProvider serviceProvider, string message) {
            if (serviceProvider != null) {
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService)); 
                if (uiService != null) {
                    uiService.ShowMessage(message); 
                    return; 
                }
            } 

            RTLAwareMessageBox.Show(null, message, String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, 0);
        }
 
        /*
        This method is not currently used. Uncomment it if you need it. 
        public static void ShowMessage(IServiceProvider serviceProvider, string message, string caption) { 
            if (serviceProvider != null) {
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService)); 
                if (uiService != null) {
                    uiService.ShowMessage(message, caption);
                    return;
                } 
            }
 
            RTLAwareMessageBox.Show(null, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, 0); 
        }
        */ 

        public static DialogResult ShowMessage(IServiceProvider serviceProvider, string message, string caption, MessageBoxButtons buttons) {
            if (serviceProvider != null) {
                IUIService uiService = (IUIService)serviceProvider.GetService(typeof(IUIService)); 
                if (uiService != null) {
                    return uiService.ShowMessage(message, caption, buttons); 
                } 
            }
 
            return RTLAwareMessageBox.Show(null, message, caption, buttons, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, 0);
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
