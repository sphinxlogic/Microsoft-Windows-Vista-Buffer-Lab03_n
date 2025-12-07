using System.Web.UI.Design.Util; 
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.ComponentModel;
using System.ComponentModel.Design; 

namespace System.Web.UI.Design.WebControls { 
    internal sealed class WindowsFormsEditorServiceHelper : IWindowsFormsEditorService, IServiceProvider { 
        private ComponentDesigner _componentDesigner;
 
        public WindowsFormsEditorServiceHelper(ComponentDesigner componentDesigner) {
            _componentDesigner = componentDesigner;
        }
 
        #region IWindowsFormsEditorService Members
        void IWindowsFormsEditorService.CloseDropDown() { 
        } 

        void IWindowsFormsEditorService.DropDownControl(System.Windows.Forms.Control control) { 
        }

        DialogResult IWindowsFormsEditorService.ShowDialog(System.Windows.Forms.Form dialog) {
            return UIServiceHelper.ShowDialog(this, dialog); 
        }
        #endregion 
 
        #region ComponentDesigner Members
        object IServiceProvider.GetService(Type serviceType) { 
            if (serviceType == typeof(IWindowsFormsEditorService)) {
                return this;
            }
            else { 
                IComponent component = _componentDesigner.Component;
                if (component != null) { 
                    ISite site = _componentDesigner.Component.Site; 
                    if (site != null) {
                        return site.GetService(serviceType); 
                    }
                }
            }
            return null; 
        }
        #endregion 
} 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
using System.Web.UI.Design.Util; 
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.ComponentModel;
using System.ComponentModel.Design; 

namespace System.Web.UI.Design.WebControls { 
    internal sealed class WindowsFormsEditorServiceHelper : IWindowsFormsEditorService, IServiceProvider { 
        private ComponentDesigner _componentDesigner;
 
        public WindowsFormsEditorServiceHelper(ComponentDesigner componentDesigner) {
            _componentDesigner = componentDesigner;
        }
 
        #region IWindowsFormsEditorService Members
        void IWindowsFormsEditorService.CloseDropDown() { 
        } 

        void IWindowsFormsEditorService.DropDownControl(System.Windows.Forms.Control control) { 
        }

        DialogResult IWindowsFormsEditorService.ShowDialog(System.Windows.Forms.Form dialog) {
            return UIServiceHelper.ShowDialog(this, dialog); 
        }
        #endregion 
 
        #region ComponentDesigner Members
        object IServiceProvider.GetService(Type serviceType) { 
            if (serviceType == typeof(IWindowsFormsEditorService)) {
                return this;
            }
            else { 
                IComponent component = _componentDesigner.Component;
                if (component != null) { 
                    ISite site = _componentDesigner.Component.Site; 
                    if (site != null) {
                        return site.GetService(serviceType); 
                    }
                }
            }
            return null; 
        }
        #endregion 
} 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
