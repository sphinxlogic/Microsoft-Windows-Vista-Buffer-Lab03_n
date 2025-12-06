using System; 
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics; 
using System.Drawing;
using System.Globalization; 
using System.IO; 
using System.Text;
using System.Web.UI.WebControls; 

namespace System.Web.UI.Design.WebControls {
    internal static class LoginDesignerUtil {
        internal abstract class GenericConvertToTemplateHelper<ControlType, ControlDesignerType> 
                where ControlType: WebControl, IControlDesignerAccessor
                where ControlDesignerType: ControlDesigner { 
 
            private const string _failureTextID = "FailureText";
            private ControlDesignerType _designer; 
            private IDesignerHost _designerHost;

            public GenericConvertToTemplateHelper(ControlDesignerType designer, IDesignerHost designerHost) {
                Debug.Assert(designer.Component is ControlType, "designer.Component does not match ControlType"); 
                _designer = designer;
                _designerHost = designerHost; 
            } 

            protected ControlDesignerType Designer { 
                get {
                    return _designer;
                }
            } 

            private ControlType ViewControl { 
                get { 
                    return (ControlType)Designer.ViewControl;
                } 
            }

            protected abstract string[] PersistedControlIDs { get; }
 
            protected abstract string[] PersistedIfNotVisibleControlIDs { get; }
 
            // Returns a table that contains the default template contents.  The table should 
            // have CellPadding equal to the BorderPadding property of the Login control.
            protected abstract Control GetDefaultTemplateContents(); 

            protected abstract Style GetFailureTextStyle(ControlType control);

            protected abstract ITemplate GetTemplate(ControlType control); 

            // Replace textboxes, validators, checkbox, button, and errorMessage with LiteralControls 
            // that contain the persistence view of that control 
            private void ConvertPersistedControlsToLiteralControls(Control defaultTemplateContents) {
                foreach (string ID in PersistedControlIDs) { 
                    Control control = defaultTemplateContents.FindControl(ID);
                    if (control != null) {
                        if (Array.IndexOf(PersistedIfNotVisibleControlIDs, ID) >= 0) {
                            control.Visible = true; 
                            // Set the parent table cell and table row to visible so they will get rendered
                            control.Parent.Visible = true; 
                            control.Parent.Parent.Visible = true; 
                        }
                        if (control.Visible) { 
                            String persisted = ControlPersister.PersistControl(control, _designerHost);
                            LiteralControl literal = new LiteralControl(persisted);
                            ControlCollection controls = control.Parent.Controls;
                            int index = controls.IndexOf(control); 
                            controls.Remove(control);
                            controls.AddAt(index, literal); 
                        } 
                    }
                } 
            }

            public ITemplate ConvertToTemplate() {
                ITemplate template = null; 

                // Skin with template scenario 
                ITemplate viewControlTemplate = GetTemplate((ControlType)ViewControl); 
                if (viewControlTemplate != null) {
                    template = viewControlTemplate; 
                }
                else {
                    _designer.ViewControlCreated = false;
 
                    Hashtable convertOn = new Hashtable(1);
                    convertOn.Add("ConvertToTemplate", true); 
                    ((IControlDesignerAccessor)ViewControl).SetDesignModeState(convertOn); 

                    // Causes the control to set its child properties 
                    _designer.GetDesignTimeHtml();

                    // Get the default template contents from the ViewControl
                    Control defaultTemplateContents = GetDefaultTemplateContents(); 
                    SetFailureTextStyle(defaultTemplateContents);
                    ConvertPersistedControlsToLiteralControls(defaultTemplateContents); 
 
                    // Render table to HTML
                    StringWriter stringWriter = new StringWriter(CultureInfo.CurrentCulture); 
                    HtmlTextWriter writer = new HtmlTextWriter(stringWriter);
                    defaultTemplateContents.RenderControl(writer);

                    // Create template from HTML 
                    template = ControlParser.ParseTemplate(_designerHost, stringWriter.ToString());
 
                    Hashtable convertOff = new Hashtable(1); 
                    convertOff.Add("ConvertToTemplate", false);
                    ((IControlDesignerAccessor)ViewControl).SetDesignModeState(convertOff); 
                }

                return template;
            } 

            // Special case to apply style to failure text table cell, since the failure text is empty at design time 
            private void SetFailureTextStyle(Control defaultTemplateContents) { 
                Control failureText = defaultTemplateContents.FindControl(_failureTextID);
                if (failureText != null) { 
                    TableCell failureTextCell = (TableCell)(failureText.Parent);
                    failureTextCell.ForeColor = Color.Red;
                    failureTextCell.ApplyStyle(GetFailureTextStyle((ControlType)ViewControl));
 
                    // Turn off viewstate for the failure text so postbacks don't retain the failure text
                    // VSWhidbey 195651 
                    failureText.EnableViewState = false; 
                }
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
using System; 
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics; 
using System.Drawing;
using System.Globalization; 
using System.IO; 
using System.Text;
using System.Web.UI.WebControls; 

namespace System.Web.UI.Design.WebControls {
    internal static class LoginDesignerUtil {
        internal abstract class GenericConvertToTemplateHelper<ControlType, ControlDesignerType> 
                where ControlType: WebControl, IControlDesignerAccessor
                where ControlDesignerType: ControlDesigner { 
 
            private const string _failureTextID = "FailureText";
            private ControlDesignerType _designer; 
            private IDesignerHost _designerHost;

            public GenericConvertToTemplateHelper(ControlDesignerType designer, IDesignerHost designerHost) {
                Debug.Assert(designer.Component is ControlType, "designer.Component does not match ControlType"); 
                _designer = designer;
                _designerHost = designerHost; 
            } 

            protected ControlDesignerType Designer { 
                get {
                    return _designer;
                }
            } 

            private ControlType ViewControl { 
                get { 
                    return (ControlType)Designer.ViewControl;
                } 
            }

            protected abstract string[] PersistedControlIDs { get; }
 
            protected abstract string[] PersistedIfNotVisibleControlIDs { get; }
 
            // Returns a table that contains the default template contents.  The table should 
            // have CellPadding equal to the BorderPadding property of the Login control.
            protected abstract Control GetDefaultTemplateContents(); 

            protected abstract Style GetFailureTextStyle(ControlType control);

            protected abstract ITemplate GetTemplate(ControlType control); 

            // Replace textboxes, validators, checkbox, button, and errorMessage with LiteralControls 
            // that contain the persistence view of that control 
            private void ConvertPersistedControlsToLiteralControls(Control defaultTemplateContents) {
                foreach (string ID in PersistedControlIDs) { 
                    Control control = defaultTemplateContents.FindControl(ID);
                    if (control != null) {
                        if (Array.IndexOf(PersistedIfNotVisibleControlIDs, ID) >= 0) {
                            control.Visible = true; 
                            // Set the parent table cell and table row to visible so they will get rendered
                            control.Parent.Visible = true; 
                            control.Parent.Parent.Visible = true; 
                        }
                        if (control.Visible) { 
                            String persisted = ControlPersister.PersistControl(control, _designerHost);
                            LiteralControl literal = new LiteralControl(persisted);
                            ControlCollection controls = control.Parent.Controls;
                            int index = controls.IndexOf(control); 
                            controls.Remove(control);
                            controls.AddAt(index, literal); 
                        } 
                    }
                } 
            }

            public ITemplate ConvertToTemplate() {
                ITemplate template = null; 

                // Skin with template scenario 
                ITemplate viewControlTemplate = GetTemplate((ControlType)ViewControl); 
                if (viewControlTemplate != null) {
                    template = viewControlTemplate; 
                }
                else {
                    _designer.ViewControlCreated = false;
 
                    Hashtable convertOn = new Hashtable(1);
                    convertOn.Add("ConvertToTemplate", true); 
                    ((IControlDesignerAccessor)ViewControl).SetDesignModeState(convertOn); 

                    // Causes the control to set its child properties 
                    _designer.GetDesignTimeHtml();

                    // Get the default template contents from the ViewControl
                    Control defaultTemplateContents = GetDefaultTemplateContents(); 
                    SetFailureTextStyle(defaultTemplateContents);
                    ConvertPersistedControlsToLiteralControls(defaultTemplateContents); 
 
                    // Render table to HTML
                    StringWriter stringWriter = new StringWriter(CultureInfo.CurrentCulture); 
                    HtmlTextWriter writer = new HtmlTextWriter(stringWriter);
                    defaultTemplateContents.RenderControl(writer);

                    // Create template from HTML 
                    template = ControlParser.ParseTemplate(_designerHost, stringWriter.ToString());
 
                    Hashtable convertOff = new Hashtable(1); 
                    convertOff.Add("ConvertToTemplate", false);
                    ((IControlDesignerAccessor)ViewControl).SetDesignModeState(convertOff); 
                }

                return template;
            } 

            // Special case to apply style to failure text table cell, since the failure text is empty at design time 
            private void SetFailureTextStyle(Control defaultTemplateContents) { 
                Control failureText = defaultTemplateContents.FindControl(_failureTextID);
                if (failureText != null) { 
                    TableCell failureTextCell = (TableCell)(failureText.Parent);
                    failureTextCell.ForeColor = Color.Red;
                    failureTextCell.ApplyStyle(GetFailureTextStyle((ControlType)ViewControl));
 
                    // Turn off viewstate for the failure text so postbacks don't retain the failure text
                    // VSWhidbey 195651 
                    failureText.EnableViewState = false; 
                }
            } 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
