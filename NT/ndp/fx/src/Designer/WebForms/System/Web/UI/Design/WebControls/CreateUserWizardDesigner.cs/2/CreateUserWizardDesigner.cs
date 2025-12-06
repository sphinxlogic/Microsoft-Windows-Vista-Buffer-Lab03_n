//------------------------------------------------------------------------------ 
/// <copyright from='1997' to='2001' company='Microsoft Corporation'>
///    Copyright (c) Microsoft Corporation. All Rights Reserved.
///    Information Contained Herein is Proprietary and Confidential.
/// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Web.UI.Design.WebControls { 

    using System; 
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Data;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing;
    using System.Globalization; 
    using System.Reflection;
    using System.Text;
    using System.Web;
    using System.Web.UI; 
    using System.Web.UI.Design;
    using System.Web.UI.WebControls; 
    using System.IO; 
    using Cursor = System.Windows.Forms.Cursor;
    using Cursors = System.Windows.Forms.Cursors; 


    /// <include file='doc\CreateUserWizardDesigner.uex' path='docs/doc[@for="CreateUserWizardDesigner"]/*' />
    /// <devdoc> 
    /// <para>
    /// Provides design-time support for the <see cref='System.Web.UI.WebControls.Wizard'/> web control. 
    /// </para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, 
        Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class CreateUserWizardDesigner : WizardDesigner {

        private CreateUserWizard _createUserWizard; 

        private const string _userNameID = "UserName"; 
        private const string _passwordID = "Password"; 
        private const string _confirmPasswordID = "ConfirmPassword";
        private const string _unknownErrorMessageID = "ErrorMessage"; 
        private const string _emailID = "Email";
        private const string _questionID = "Question";
        private const string _answerID = "Answer";
        private const string _userNameRequiredID = "UserNameRequired"; 
        private const string _passwordRequiredID = "PasswordRequired";
        private const string _confirmPasswordRequiredID = "ConfirmPasswordRequired"; 
        private const string _passwordRegExpID = "PasswordRegExp"; 
        private const string _emailRequiredID = "EmailRequired";
        private const string _emailRegExpID = "EmailRegExp"; 
        private const string _questionRequiredID = "QuestionRequired";
        private const string _answerRequiredID = "AnswerRequired";
        private const string _passwordCompareID = "PasswordCompare";
        private const string _cancelButtonID = "CancelButton"; 
        private const string _cancelButtonButtonID = "CancelButtonButton";
        private const string _cancelButtonImageButtonID = "CancelButtonImageButton"; 
        private const string _cancelButtonLinkButtonID = "CancelButtonLinkButton"; 
        private const string _continueButtonID = "ContinueButton";
        private const string _continueButtonButtonID = "ContinueButtonButton"; 
        private const string _continueButtonImageButtonID = "ContinueButtonImageButton";
        private const string _continueButtonLinkButtonID = "ContinueButtonLinkButton";
        private const string _helpLinkID = "HelpLink";
        private const string _editProfileLinkID = "EditProfileLink"; 
        private const string _createUserButtonID = "StepNextButton";
        private const string _createUserButtonButtonID = "StepNextButtonButton"; 
        private const string _createUserButtonImageButtonID = "StepNextButtonImageButton"; 
        private const string _createUserButtonLinkButtonID = "StepNextButtonLinkButton";
        private const string _createUserNavigationTemplateName = "CreateUserNavigationTemplate"; 
        private const string _previousButtonID = "StepNextButton";
        private const string _previousButtonButtonID = "StepPreviousButton";
        private const string _previousButtonImageButtonID = "StepPreviousButtonImageButton";
        private const string _previousButtonLinkButtonID = "StepPreviousButtonLinkButton"; 

        private static DesignerAutoFormatCollection _autoFormats; 
 
        private static readonly Hashtable _persistedIDConverter;
        private static readonly Hashtable _completeStepConverter; 
        static CreateUserWizardDesigner() {
            _persistedIDConverter = new Hashtable();
            _persistedIDConverter.Add(_cancelButtonImageButtonID, _cancelButtonID);
            _persistedIDConverter.Add(_cancelButtonButtonID, _cancelButtonID); 
            _persistedIDConverter.Add(_cancelButtonLinkButtonID, _cancelButtonID);
            _persistedIDConverter.Add(_createUserButtonImageButtonID, _createUserButtonID); 
            _persistedIDConverter.Add(_createUserButtonButtonID, _createUserButtonID); 
            _persistedIDConverter.Add(_createUserButtonLinkButtonID, _createUserButtonID);
            _persistedIDConverter.Add(_previousButtonImageButtonID, _previousButtonID); 
            _persistedIDConverter.Add(_previousButtonButtonID, _previousButtonID);
            _persistedIDConverter.Add(_previousButtonLinkButtonID, _previousButtonID);

            _completeStepConverter = new Hashtable(); 
            _completeStepConverter.Add(_continueButtonImageButtonID, _continueButtonID);
            _completeStepConverter.Add(_continueButtonButtonID, _continueButtonID); 
            _completeStepConverter.Add(_continueButtonLinkButtonID, _continueButtonID); 
        }
 
        // Controls that are persisted when converting to template
        private static readonly string[] _persistedControlIDs = new string[] {
            _userNameID,
            _userNameRequiredID, 
            _passwordID,
            _passwordRequiredID, 
            _confirmPasswordID, 
            _emailID,
            _questionID, 
            _answerID,
            _confirmPasswordRequiredID,
            _passwordRegExpID,
            _emailRegExpID, 
            _emailRequiredID,
            _questionRequiredID, 
            _answerRequiredID, 
            _passwordCompareID,
            _cancelButtonID, 
            _continueButtonID,
            _createUserButtonID,
            _unknownErrorMessageID,
            _helpLinkID, 
            _editProfileLinkID,
        }; 
 
        // Controls that are persisted even if they are not visible when the control is rendered
        // They are not visible at design-time because the values are computed at runtime 
        private static readonly string[] _persistedIfNotVisibleControlIDs = new string[] {
            _unknownErrorMessageID,
        };
 
        // Removed from the property grid when there is a user template for the create step
        private static readonly string[] _defaultCreateStepProperties = new string[] { 
            "AnswerLabelText", 
            "ConfirmPasswordLabelText",
            "ConfirmPasswordCompareErrorMessage", 
            "ConfirmPasswordRequiredErrorMessage",
            "EmailLabelText",
            "ErrorMessageStyle",
            "HelpPageIconUrl", 
            "HelpPageText",
            "HelpPageUrl", 
            "HyperLinkStyle", 
            "InstructionText",
            "InstructionTextStyle", 
            "LabelStyle",
            "PasswordHintText",
            "PasswordHintStyle",
            "PasswordLabelText", 
            "PasswordRequiredErrorMessage",
            "QuestionLabelText", 
            "TextBoxStyle", 
            "UserNameLabelText",
            "UserNameRequiredErrorMessage", 
            "AnswerRequiredErrorMessage",
            "EmailRegularExpression",
            "EmailRegularExpressionErrorMessage",
            "EmailRequiredErrorMessage", 
            "PasswordRegularExpression",
            "PasswordRegularExpressionErrorMessage", 
            "QuestionRequiredErrorMessage", 
            "ValidatorTextStyle",
        }; 

        private static readonly string[] _defaultCreateUserNavProperties = new string[] {
            "CancelButtonImageUrl",
            "CancelButtonType", 
            "CancelButtonStyle",
            "CancelButtonText", 
            "CreateUserButtonImageUrl", 
            "CreateUserButtonType",
            "CreateUserButtonStyle", 
            "CreateUserButtonText",
        };

        // Removed from the property grid when there is a user template for the create step 
        private static readonly string[] _defaultCompleteStepProperties = new string[] {
            "CompleteSuccessText", 
            "CompleteSuccessTextStyle", 
            "ContinueButtonStyle",
            "ContinueButtonText", 
            "ContinueButtonType",
            "ContinueButtonImageUrl",
            "EditProfileText",
            "EditProfileIconUrl", 
            "EditProfileUrl",
        }; 
 
        private static bool IsStepEmpty(WizardStepBase step) {
            if (!(step is CreateUserWizardStep) && !(step is CompleteWizardStep)) { 
                return false;
            }

            TemplatedWizardStep templatedStep = (TemplatedWizardStep)step; 
            return templatedStep.ContentTemplate == null;
        } 
 
        internal override bool InRegionEditingMode(Wizard viewControl) {
            if (!SupportsDesignerRegions) { 
                return true;
            }

            // If the activestep's content is empty, renders them since they are not using RegionEditing 
            if (IsStepEmpty(_createUserWizard.ActiveStep)) {
                return true; 
            } 

            return false; 
        }

        /// <include file='doc\CreateUserWizardDesigner.uex' path='docs/doc[@for="CreateUserWizardDesigner.ActionLists"]/*' />
        public override DesignerActionListCollection ActionLists { 
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection(); 
                actionLists.AddRange(base.ActionLists); 
                actionLists.Add(new CreateUserWizardDesignerActionList(this));
 
                return actionLists;
            }
        }
 
        public override DesignerAutoFormatCollection AutoFormats {
            get { 
                if (_autoFormats == null) { 
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.CREATEUSERWIZARD_SCHEMES,
                        delegate(DataRow schemeData) { return new CreateUserWizardAutoFormat(schemeData); }); 
                }
                return _autoFormats;
            }
        } 

        protected override bool UsePreviewControl { 
            get { 
                return true;
            } 
        }

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.AddDesignerRegions1"]/*' />
        protected override void AddDesignerRegions(DesignerRegionCollection regions) { 
            if (!SupportsDesignerRegions) {
                return; 
            } 

            // Recreate the controls if we have no create user step, this will retrigger any exceptions as well 
            if (_createUserWizard.CreateUserStep == null) {
                CreateChildControls();

                // Give up if its still not created 
                if (_createUserWizard.CreateUserStep == null) {
                    return; 
                } 
            }
 
            // Default steps are not editable
            bool defaultCreateStep = _createUserWizard.CreateUserStep.ContentTemplate == null;
            bool defaultCompleteStep = _createUserWizard.CompleteStep.ContentTemplate == null;
            foreach (WizardStepBase step in _createUserWizard.WizardSteps) { 
                bool defaultStep = ((defaultCreateStep && step is CreateUserWizardStep) ||
                                    (defaultCompleteStep && step is CompleteWizardStep)); 
                DesignerRegion reg = null; 
                if (!defaultStep) {
                    if (step is TemplatedWizardStep) { 
                        TemplateDefinition definition = new TemplateDefinition(
                            this, _contentTemplateName, _createUserWizard, _contentTemplateName, TemplateStyleArray[_navigationStyleLength - 1]);
                        reg = new WizardStepTemplatedEditableRegion(definition, step);
                        reg.EnsureSize = false; 
                    }
                    else { 
                        reg = new WizardStepEditableRegion(this, step); 
                    }
                    reg.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark); 
                }
                else reg = new WizardSelectableRegion(this, GetRegionName(step), step);
                regions.Add(reg);
            } 
            foreach (WizardStepBase step in _createUserWizard.WizardSteps) {
                WizardSelectableRegion reg = new WizardSelectableRegion(this, "Move to " + GetRegionName(step), step); 
                if (_createUserWizard.ActiveStep == step) { 
                    reg.Selected = true;
                } 
                regions.Add(reg);
            }
        }
 
        /// <include file='doc\CreateUserWizardDesigner.uex' path='docs/doc[@for="CreateUserWizardDesigner.OnConvertToCustomNavigationTemplate"]/*' />
        protected override void ConvertToCustomNavigationTemplate() { 
            try { 
                if (_createUserWizard.ActiveStep == _createUserWizard.CreateUserStep) {
                    IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                    Debug.Assert(designerHost != null);

                    ITemplate template = ((CreateUserWizard)ViewControl).CreateUserStep.CustomNavigationTemplate;
                    if (template == null) { 
                        IControlDesignerAccessor accessor = (IControlDesignerAccessor)_createUserWizard;
                        IDictionary dictionary = accessor.GetDesignModeState(); 
 
                        ControlCollection controls = dictionary[_customNavigationControls] as ControlCollection;
                        if (controls != null) { 
                            string persistedText = String.Empty;
                            foreach (Control ctrl in controls) {
                                if (ctrl != null && ctrl.Visible) {
                                    // Convert to the right control ID 
                                    foreach (string ID in _persistedIDConverter.Keys) {
                                        Control control = ctrl.FindControl(ID); 
                                        if (control != null && control.Visible) { 
                                            control.ID = (String)_persistedIDConverter[ID];
                                        } 
                                    }

                                    // We just want the buttons to remain in persistance format
                                    if (ctrl is Table) { 
                                        persistedText += ConvertNavigationTableToHtmlTable((Table)ctrl);
                                        // Everything else should look like html 
                                    } 
                                    else {
                                        StringWriter stringWriter = new StringWriter(CultureInfo.CurrentCulture); 
                                        HtmlTextWriter writer = new HtmlTextWriter(stringWriter);
                                        ctrl.RenderControl(writer);
                                        persistedText += stringWriter.ToString();
                                    } 
                                }
                            } 
                            template = ControlParser.ParseTemplate(designerHost, persistedText); 
                        }
                    } 

                    InvokeTransactedChange(Component, new TransactedChangeCallback(base.ConvertToCustomNavigationTemplateCallBack),
                        template, SR.GetString(SR.Wizard_ConvertToCustomNavigationTemplate));
 
                    UpdateDesignTimeHtml();
                } 
                else { 
                    base.ConvertToCustomNavigationTemplate();
                } 
            }
            catch (Exception ex) {
                Debug.Fail(ex.ToString());
            } 
        }
 
        private string ConvertTableToHtmlTable(Table originalTable, Control container) { 
            return ConvertTableToHtmlTable(originalTable, container, null);
        } 

        private string ConvertTableToHtmlTable(Table originalTable, Control container, IDictionary persistMap) {
            // Original table has style, ID, and base attributes copied from PasswordRecovery control.  We don't want these on
            // the template, so create a new table and move rows from old to new table.  Need to create temporary 
            // ArrayList because the original ControlCollection can't be modified while being enumerated.
            IList controls = new ArrayList(); 
            foreach (Control c in originalTable.Controls) { 
                controls.Add(c);
            } 
            Table table = new Table();
            foreach (Control c in controls) {
                table.Controls.Add(c);
            } 

            // The table inside the template will not inherit the font properties 
            if (originalTable.ControlStyleCreated) { 
                table.ApplyStyle(originalTable.ControlStyle);
            } 
            table.Width = ((WebControl)ViewControl).Width;
            table.Height = ((WebControl)ViewControl).Height;

            // Add the new table to the controls collection of the login container, and remove the old table 
            // Necessary to set the Site, Page, Parent, etc. of the new table
            if (container != null) { 
                container.Controls.Add(table); 
                container.Controls.Remove(originalTable);
            } 

            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));

            if (persistMap != null) { 
                // Convert to the right control ID
                foreach (string id in persistMap.Keys) { 
                    Control c = table.FindControl(id); 
                    if (c != null && c.Visible) {
                        c.ID = (string)persistMap[id]; 
                        string persisted = ControlPersister.PersistControl(c, host);
                        LiteralControl literal = new LiteralControl(persisted);
                        c.Parent.Controls.Add(literal);
                        c.Parent.Controls.Remove(c); 
                    }
                } 
            } 

            // Replace textboxes, validators, button, and errorMessage with LiteralControls 
            // that contain the persistence view of that control
            foreach (string ID in _persistedControlIDs) {
                Control control = table.FindControl(ID);
                if (control != null) { 
                    if (Array.IndexOf(_persistedIfNotVisibleControlIDs, ID) >= 0) {
                        control.Visible = true; 
                        // Set the parent table cell and table row to visible so they will get rendered 
                        control.Parent.Visible = true;
                        control.Parent.Parent.Visible = true; 
                    }
                    // Special case to apply style to failure text table cell, since the failure text is empty at design time
                    if (ID == _unknownErrorMessageID) {
                        TableCell errorMessagCell = (TableCell)(control.Parent); 
                        errorMessagCell.ForeColor = Color.Red;
                        errorMessagCell.ApplyStyle(_createUserWizard.ErrorMessageStyle); 
 
                        // Turn off viewstate for the failure text so postbacks don't retain the failure text
                        // VSWhidbey 195651 
                        control.EnableViewState = false;
                    }
                    if (control.Visible) {
                        string persisted = ControlPersister.PersistControl(control, host); 
                        LiteralControl literal = new LiteralControl(persisted);
                        control.Parent.Controls.Add(literal); 
                        control.Parent.Controls.Remove(control); 
                    }
                } 
            }

            // Render table to HTML
            StringWriter stringWriter = new StringWriter(CultureInfo.CurrentCulture); 
            HtmlTextWriter writer = new HtmlTextWriter(stringWriter);
            table.RenderControl(writer); 
 
            return stringWriter.ToString();
        } 

        private string ConvertNavigationTableToHtmlTable(Table table) {
            // Needed to make the failure text visible
            IControlDesignerAccessor accessor = (IControlDesignerAccessor)_createUserWizard; 
            IDictionary dictionary = accessor.GetDesignModeState();
 
            StringWriter strWriter = new StringWriter(CultureInfo.CurrentCulture); 
            HtmlTextWriter writer = new HtmlTextWriter(strWriter);
            if (table.Width != Unit.Empty) writer.AddStyleAttribute(HtmlTextWriterStyle.Width, table.Width.ToString(CultureInfo.CurrentCulture)); 
            if (table.Height != Unit.Empty) writer.AddStyleAttribute(HtmlTextWriterStyle.Height, table.Height.ToString(CultureInfo.CurrentCulture));
            if (table.CellSpacing != 0) writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, table.CellSpacing.ToString(CultureInfo.CurrentCulture));
            string border = "0";
            if (table.BorderWidth != Unit.Empty) { 
                border = table.BorderWidth.ToString(CultureInfo.CurrentCulture);
            } 
            writer.AddAttribute(HtmlTextWriterAttribute.Border, border); 
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
 
            ArrayList rowCells = new ArrayList(table.Rows.Count);
            foreach (TableRow row in table.Rows) {
                if (!row.Visible) continue;
                ArrayList cells = new ArrayList(row.Cells.Count); 
                foreach (TableCell cell in row.Cells) {
                    if (!cell.Visible || !cell.HasControls()) continue; 
                    ArrayList controls = new ArrayList(cell.Controls.Count); 
                    foreach (Control control in cell.Controls) {
                        if (!control.Visible) continue; 
                        if (control is Literal && control.ID != _unknownErrorMessageID && ((Literal)control).Text.Length == 0) continue;
                        if (control is HyperLink && ((HyperLink)control).Text.Length == 0) continue;
                        if (control is System.Web.UI.WebControls.Image &&
                            ((System.Web.UI.WebControls.Image)control).ImageUrl.Length == 0) continue; 
                        controls.Add(control);
                    } 
                    if (controls.Count > 0) cells.Add(new CellControls(cell, controls)); 
                }
                if (cells.Count > 0) rowCells.Add(new RowCells(row, cells)); 
            }

            foreach (RowCells rc in rowCells) {
                switch (rc._row.HorizontalAlign) { 
                case HorizontalAlign.Center:
                    writer.AddAttribute(HtmlTextWriterAttribute.Align, "center"); 
                    break; 
                case HorizontalAlign.Right:
                    writer.AddAttribute(HtmlTextWriterAttribute.Align, "right"); 
                    break;
                }
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
 
                foreach (CellControls cell in rc._cells) {
                    switch (cell._cell.HorizontalAlign) { 
                    case HorizontalAlign.Center: 
                        writer.AddAttribute(HtmlTextWriterAttribute.Align, "center");
                        break; 
                    case HorizontalAlign.Right:
                        writer.AddAttribute(HtmlTextWriterAttribute.Align, "right");
                        break;
                    } 

                    writer.AddAttribute(HtmlTextWriterAttribute.Colspan, cell._cell.ColumnSpan.ToString(CultureInfo.CurrentCulture)); 
 
                    StringBuilder cellContents = new StringBuilder();
                    foreach (Control control in cell._controls) { 
                        bool failureLiteral = control.ID == _unknownErrorMessageID;
                        // Don't show the empty controls unless its required
                        if (control is Literal && !failureLiteral) {
                            // Write out literals as text 
                            cellContents.Append(((Literal)control).Text);
                            continue; 
                        } 

                        // Special case to apply style to failure text table cell, since the failure text is empty at design time 
                        if (failureLiteral) {
                            writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "Red");

                            // Turn off viewstate for the failure text so postbacks don't retain the failure text 
                            // VSWhidbey 195651
                            control.EnableViewState = false; 
                        } 

                        cellContents.Append(ControlPersister.PersistControl(control)); 
                    }

                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(cellContents.ToString()); 

                    writer.RenderEndTag(); 
                } 
                writer.RenderEndTag();
            } 

            writer.RenderEndTag();
            return strWriter.ToString();
        } 

        internal override string GetEditableDesignerRegionContent(IWizardStepEditableRegion region) { 
            if (region == null) 
                throw new ArgumentNullException("region");
 
            StringBuilder sb = new StringBuilder();
            if (region.Step == _createUserWizard.CreateUserStep &&
                ((CreateUserWizardStep)region.Step).ContentTemplate == null &&
                region.Step.Controls[0] is Table) { 
                //
                Table table = (Table)(((Table)region.Step.Controls[0]).Rows[0].Cells[0].Controls[0]); 
                sb.Append(ConvertTableToHtmlTable(table, ((TemplatedWizardStep)region.Step).ContentTemplateContainer)); 
                return sb.ToString();
            } 
            else if (region.Step == _createUserWizard.CompleteStep &&
                     ((CompleteWizardStep)region.Step).ContentTemplate == null &&
                     region.Step.Controls[0] is Table) {
                // 
                Table table = (Table)(((Table)region.Step.Controls[0]).Rows[0].Cells[0].Controls[0]);
                sb.Append(ConvertTableToHtmlTable(table, ((TemplatedWizardStep)region.Step).ContentTemplateContainer)); 
                return sb.ToString(); 
            }
 
            return base.GetEditableDesignerRegionContent(region);
        }

        /// <include file='doc\LoginDesigner.uex' path='docs/doc[@for="LoginDesigner.GetErrorDesignTimeHtml"]/*' /> 
        protected override string GetErrorDesignTimeHtml(Exception e) {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRenderingShort) + "<br />" + e.Message); 
        } 

        /// <include file='doc\CreateUserWizardDesigner.uex' path='docs/doc[@for="CreateUserWizardDesigner.Initialize"]/*' /> 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(CreateUserWizard));

            // DevDiv Bugs 71165: Must set _createUserWizard before calling base 
            _createUserWizard = (CreateUserWizard)component;
 
            base.Initialize(component); 

        } 

        private void LaunchWebAdmin() {
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (designerHost != null) { 
                IWebAdministrationService webadmin = (IWebAdministrationService)designerHost.GetService(typeof(IWebAdministrationService));
                if (webadmin != null) { 
                    webadmin.Start(null); 
                }
            } 
        }

        /// <devdoc>
        /// Creates a template that looks identical to the current rendering of the control, and tells the control 
        /// to use this template.  Allows a page developer to customize the control using its style properties, then
        /// convert to a template and modify the template for further customization.  The template contains the 
        /// necessary server controls with the correct IDs. 
        /// </devdoc>
        private void CustomizeCompleteStep() { 
            IComponent component = (IComponent)_createUserWizard.CompleteStep;

            PropertyDescriptor activeStepIndexDescriptor = TypeDescriptor.GetProperties(Component)["ActiveStepIndex"];
            int index = _createUserWizard.WizardSteps.IndexOf(_createUserWizard.CompleteStep); 
            InvokeTransactedChange(Component, new TransactedChangeCallback(NavigateToStep), index,
                                   SR.GetString(SR.CreateUserWizard_NavigateToStep, index), activeStepIndexDescriptor); 
 
            PropertyDescriptor templateDescriptor = TypeDescriptor.GetProperties(component)["ContentTemplate"];
            InvokeTransactedChange(Component.Site, component, new TransactedChangeCallback(CustomizeCompleteStepCallback), null, 
                                   SR.GetString(SR.CreateUserWizard_CustomizeCompleteStep), templateDescriptor);
        }

        private bool CustomizeCompleteStepCallback(object context) { 
            Debug.Assert(_createUserWizard.CompleteStep.ContentTemplate == null);
 
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null);
 
            CreateUserWizard createUserWizard = (CreateUserWizard)ViewControl;

            // Skin with template scenario
            ITemplate contentTemplate = createUserWizard.CompleteStep.ContentTemplate; 
            if (contentTemplate == null) {
                try { 
                    Hashtable convertOn = new Hashtable(1); 
                    convertOn.Add("ConvertToTemplate", true);
                    ((IControlDesignerAccessor)ViewControl).SetDesignModeState(convertOn); 

                    // Causes the control to set its child properties
                    ViewControlCreated = false;
                    GetDesignTimeHtml(); 

                    createUserWizard = (CreateUserWizard)ViewControl; 
                    IControlDesignerAccessor accessor = (IControlDesignerAccessor)createUserWizard; 
                    IDictionary dictionary = accessor.GetDesignModeState();
 
                    StringBuilder sb = new StringBuilder();
                    // Only control in step is the container who has a table first
                    TemplatedWizardStep step = createUserWizard.CompleteStep;
                    Table table = (Table)(((Table)step.Controls[0].Controls[0]).Rows[0].Cells[0].Controls[0]); 

                    // Apply control style to the table before converting 
                    if (createUserWizard.ControlStyleCreated) { 
                        Style style = createUserWizard.ControlStyle;
                        table.ForeColor = style.ForeColor; 
                        table.BackColor = style.BackColor;
                        table.Font.CopyFrom(style.Font);
                        table.Font.Size = new FontUnit(Unit.Percentage(100));
                    } 

                    // Apply Step style to the table before converting 
                    Style stepStyle = createUserWizard.StepStyle; 
                    if (!stepStyle.IsEmpty) {
                        table.ForeColor = stepStyle.ForeColor; 
                        table.BackColor = stepStyle.BackColor;
                        table.Font.CopyFrom(stepStyle.Font);
                        table.Font.Size = new FontUnit(Unit.Percentage(100));
                    } 

                    sb.Append(ConvertTableToHtmlTable(table, step.ContentTemplateContainer, _completeStepConverter)); 
 
                    contentTemplate = ControlParser.ParseTemplate(designerHost, sb.ToString());
 
                    Hashtable convertOff = new Hashtable(1);
                    convertOff.Add("ConvertToTemplate", false);
                    ((IControlDesignerAccessor)ViewControl).SetDesignModeState(convertOff);
                } 
                catch (Exception e) {
                    Debug.Fail(e.Message); 
                    return false; 
                }
            } 

            IComponent component = (IComponent)_createUserWizard.CompleteStep;
            PropertyDescriptor templateDescriptor = TypeDescriptor.GetProperties(component)["ContentTemplate"];
            templateDescriptor.SetValue(component, contentTemplate); 

            UpdateDesignTimeHtml(); 
 
            return true;
        } 

        /// <devdoc>
        /// Creates a template that looks identical to the current rendering of the control, and tells the control
        /// to use this template.  Allows a page developer to customize the control using its style properties, then 
        /// convert to a template and modify the template for further customization.  The template contains the
        /// necessary server controls with the correct IDs. 
        /// </devdoc> 
        private void CustomizeCreateUserStep() {
            IComponent component = (IComponent)_createUserWizard.CreateUserStep; 

            PropertyDescriptor activeStepIndexDescriptor = TypeDescriptor.GetProperties(Component)["ActiveStepIndex"];
            int index = _createUserWizard.WizardSteps.IndexOf(_createUserWizard.CreateUserStep);
            InvokeTransactedChange(Component, new TransactedChangeCallback(NavigateToStep), index, 
                                   SR.GetString(SR.CreateUserWizard_NavigateToStep, index), activeStepIndexDescriptor);
 
            PropertyDescriptor templateDescriptor = TypeDescriptor.GetProperties(component)["ContentTemplate"]; 
            InvokeTransactedChange(Component.Site, component, new TransactedChangeCallback(CustomizeCreateUserStepCallback), null,
                                   SR.GetString(SR.CreateUserWizard_CustomizeCreateUserStep), templateDescriptor); 
        }

        private bool NavigateToStep(object context) {
            try { 
                int stepIndex = (int)context;
                _createUserWizard.ActiveStepIndex = stepIndex; 
 
                return true;
            } 
            catch (Exception ex) {
                Debug.Fail(ex.Message);
                return false;
            } 
        }
 
        private bool CustomizeCreateUserStepCallback(object context) { 
            try {
                IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                Debug.Assert(designerHost != null);

                CreateUserWizard createUserWizard = (CreateUserWizard)ViewControl;
 
                // contentTemplate is defined when the skin has a template
                ITemplate contentTemplate = createUserWizard.CreateUserStep.ContentTemplate; 
                if (contentTemplate == null) { 

                    // Causes the control to set its child properties 
                    ViewControlCreated = false;

                    Hashtable convertOn = new Hashtable(1);
                    convertOn.Add("ConvertToTemplate", true); 
                    ((IControlDesignerAccessor)ViewControl).SetDesignModeState(convertOn);
 
                    GetDesignTimeHtml(); 

                    // Get the new view control 
                    createUserWizard = (CreateUserWizard)ViewControl;
                    Debug.Assert(createUserWizard.CreateUserStep.ContentTemplate == null);

                    IControlDesignerAccessor accessor = (IControlDesignerAccessor)createUserWizard; 
                    IDictionary dictionary = accessor.GetDesignModeState();
 
                    StringBuilder sb = new StringBuilder(); 
                    // Only control in step is the container who has a table first
                    TemplatedWizardStep step = createUserWizard.CreateUserStep; 
                    Table table = (Table)(((Table)step.Controls[0].Controls[0]).Rows[0].Cells[0].Controls[0]);

                    // Apply control style to the table before converting
                    if (createUserWizard.ControlStyleCreated)  { 
                        Style style = createUserWizard.ControlStyle;
                        table.ForeColor = style.ForeColor; 
                        table.BackColor = style.BackColor; 
                        table.Font.CopyFrom(style.Font);
                        table.Font.Size = new FontUnit(Unit.Percentage(100)); 
                    }

                    // Apply Step style to the table before converting
                    Style stepStyle = createUserWizard.StepStyle; 
                    if (!stepStyle.IsEmpty) {
                        table.ForeColor = stepStyle.ForeColor; 
                        table.BackColor = stepStyle.BackColor; 
                        table.Font.CopyFrom(stepStyle.Font);
                        table.Font.Size = new FontUnit(Unit.Percentage(100)); 
                    }

                    sb.Append(ConvertTableToHtmlTable(table, step.ContentTemplateContainer));
 
                    contentTemplate = ControlParser.ParseTemplate(designerHost, sb.ToString());
 
                    Hashtable convertOff = new Hashtable(1); 
                    convertOff.Add("ConvertToTemplate", false);
                    ((IControlDesignerAccessor)createUserWizard).SetDesignModeState(convertOff); 
                }

                IComponent component = (IComponent)_createUserWizard.CreateUserStep;
                PropertyDescriptor templateDescriptor = TypeDescriptor.GetProperties(component)["ContentTemplate"]; 
                templateDescriptor.SetValue(_createUserWizard.CreateUserStep, contentTemplate);
 
                UpdateDesignTimeHtml(); 

                return true; 
            } catch (Exception e) {
                Debug.Fail(e.Message);
                return false;
            } 
        }
 
        /// <include file='doc\CreateUserWizardDesigner.uex' path='docs/doc[@for="CreateUserWizardDesigner.PreFilterProperties"]/*' /> 
        /// <devdoc>
        /// If the default steps are not used, remove properties that do not apply. 
        /// </devdoc>
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties);
            TemplatedWizardStep createStep = _createUserWizard.CreateUserStep; 
            bool defaultCreateStep = (createStep != null && createStep.ContentTemplate != null);
            if (defaultCreateStep) { 
                foreach (string propertyName in _defaultCreateStepProperties) { 
                    PropertyDescriptor property = (PropertyDescriptor) properties[propertyName];
                    Debug.Assert(property != null, "Property is null: " + propertyName); 
                    if (property != null) {
                        properties[propertyName] = TypeDescriptor.CreateProperty(property.ComponentType, property, BrowsableAttribute.No);
                    }
                } 
            }
 
            TemplatedWizardStep completeStep = _createUserWizard.CompleteStep; 
            bool defaultCompleteStep = (completeStep != null && completeStep.ContentTemplate != null);
            if (defaultCompleteStep) { 
                foreach (string propertyName in _defaultCompleteStepProperties) {
                    PropertyDescriptor property = (PropertyDescriptor) properties[propertyName];
                    Debug.Assert(property != null, "Property is null: " + propertyName);
                    if (property != null) { 
                        properties[propertyName] = TypeDescriptor.CreateProperty(property.ComponentType, property, BrowsableAttribute.No);
                    } 
                } 
            }
            if (createStep != null && createStep.CustomNavigationTemplate != null) { 
                foreach (string propertyName in _defaultCreateUserNavProperties) {
                    PropertyDescriptor property = (PropertyDescriptor) properties[propertyName];
                    Debug.Assert(property != null, "Property is null: " + propertyName);
                    if (property != null) { 
                        properties[propertyName] = TypeDescriptor.CreateProperty(property.ComponentType, property, BrowsableAttribute.No);
                    } 
                } 
            }
 
            if (defaultCompleteStep && defaultCreateStep) {
                // Only TitleTextStyle should remain unless both are templated
                PropertyDescriptor property = (PropertyDescriptor) properties["TitleTextStyle"];
                if (property != null) { 
                    properties["TitleTextStyle"] = TypeDescriptor.CreateProperty(property.ComponentType, property, BrowsableAttribute.No);
                } 
 
            }
        } 

        private bool ResetCallback(object context) {
            try {
                IComponent component = (IComponent)context; 
                PropertyDescriptor templateDescriptor = TypeDescriptor.GetProperties(component)["ContentTemplate"];
                templateDescriptor.SetValue(component, null); 
 
                return true;
            } 
            catch (Exception e) {
                Debug.Fail(e.Message);
                return false;
            } 
        }
 
        /// <devdoc> 
        /// Restores the default finish step
        /// </devdoc> 
        private void ResetCompleteStep() {

            //
 
            UpdateDesignTimeHtml();
 
            PropertyDescriptor templateDescriptor = TypeDescriptor.GetProperties(Component)["WizardSteps"]; 
            InvokeTransactedChange(Component, new TransactedChangeCallback(ResetCallback), _createUserWizard.CompleteStep,
                                   SR.GetString(SR.CreateUserWizard_ResetCompleteStepVerb), templateDescriptor); 
        }

        /// <devdoc>
        /// Restores the default create user step 
        /// </devdoc>
        private void ResetCreateUserStep() { 
            // 

            UpdateDesignTimeHtml(); 

            PropertyDescriptor templateDescriptor = TypeDescriptor.GetProperties(Component)["WizardSteps"];
            InvokeTransactedChange(Component, new TransactedChangeCallback(ResetCallback), _createUserWizard.CreateUserStep,
                                   SR.GetString(SR.CreateUserWizard_ResetCreateUserStepVerb), templateDescriptor); 
        }
 
        // Helper class for ConvertTableToHtmlTable 
        private class RowCells {
            internal TableRow _row; 
            internal ArrayList _cells;

            internal RowCells(TableRow row, ArrayList cells) {
                _row = row; 
                _cells = cells;
            } 
        } 

        // Helper class for ConvertTableToHtmlTable 
        private class CellControls {
            internal TableCell _cell;
            internal ArrayList _controls;
 
            internal CellControls(TableCell cell, ArrayList controls) {
                _cell = cell; 
                _controls = controls; 
            }
        } 

        private class CreateUserWizardDesignerActionList : DesignerActionList {
            private CreateUserWizardDesigner _parent;
 
            public CreateUserWizardDesignerActionList(CreateUserWizardDesigner parent) : base(parent.Component) {
                _parent = parent; 
            } 

            public override bool AutoShow { 
                get {
                    return true;
                }
                set { 
                }
            } 
 
            public void CustomizeCreateUserStep() {
                Cursor originalCursor = Cursor.Current; 
                try {
                    Cursor.Current = Cursors.WaitCursor;
                    _parent.CustomizeCreateUserStep();
                } 
                finally {
                    Cursor.Current = originalCursor; 
                } 
            }
 
            public void CustomizeCompleteStep() {
                Cursor originalCursor = Cursor.Current;
                try {
                    Cursor.Current = Cursors.WaitCursor; 
                    _parent.CustomizeCompleteStep();
                } 
                finally { 
                    Cursor.Current = originalCursor;
                } 
            }

            public void LaunchWebAdmin() {
                _parent.LaunchWebAdmin(); 
            }
 
            public void ResetCreateUserStep() { 
                _parent.ResetCreateUserStep();
            } 

            public void ResetCompleteStep() {
                _parent.ResetCompleteStep();
            } 

            public override DesignerActionItemCollection GetSortedActionItems() { 
                if (_parent.InTemplateMode) { 
                    return new DesignerActionItemCollection();
                } 

                DesignerActionItemCollection items = new DesignerActionItemCollection();
                if (_parent._createUserWizard.CreateUserStep.ContentTemplate == null) {
                    items.Add(new DesignerActionMethodItem(this, "CustomizeCreateUserStep", 
                        SR.GetString(SR.CreateUserWizard_CustomizeCreateUserStep), String.Empty,
                        SR.GetString(SR.CreateUserWizard_CustomizeCreateUserStepDescription), true)); 
                } 
                else {
                    items.Add(new DesignerActionMethodItem(this, "ResetCreateUserStep", 
                        SR.GetString(SR.CreateUserWizard_ResetCreateUserStepVerb), String.Empty,
                        SR.GetString(SR.CreateUserWizard_ResetCreateUserStepVerbDescription), true));
                }
 
                if (_parent._createUserWizard.CompleteStep.ContentTemplate == null) {
                    items.Add(new DesignerActionMethodItem(this, "CustomizeCompleteStep", 
                        SR.GetString(SR.CreateUserWizard_CustomizeCompleteStep), String.Empty, 
                        SR.GetString(SR.CreateUserWizard_CustomizeCompleteStepDescription), true));
                } 
                else {
                    items.Add(new DesignerActionMethodItem(this, "ResetCompleteStep",
                        SR.GetString(SR.CreateUserWizard_ResetCompleteStepVerb), String.Empty,
                        SR.GetString(SR.CreateUserWizard_ResetCompleteStepVerbDescription), true)); 
                }
 
                items.Add(new DesignerActionMethodItem(this, "LaunchWebAdmin", SR.GetString(SR.Login_LaunchWebAdmin), 
                    String.Empty, SR.GetString(SR.Login_LaunchWebAdminDescription), true));
 
                return items;
            }
        }
 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
/// <copyright from='1997' to='2001' company='Microsoft Corporation'>
///    Copyright (c) Microsoft Corporation. All Rights Reserved.
///    Information Contained Herein is Proprietary and Confidential.
/// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Web.UI.Design.WebControls { 

    using System; 
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Data;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing;
    using System.Globalization; 
    using System.Reflection;
    using System.Text;
    using System.Web;
    using System.Web.UI; 
    using System.Web.UI.Design;
    using System.Web.UI.WebControls; 
    using System.IO; 
    using Cursor = System.Windows.Forms.Cursor;
    using Cursors = System.Windows.Forms.Cursors; 


    /// <include file='doc\CreateUserWizardDesigner.uex' path='docs/doc[@for="CreateUserWizardDesigner"]/*' />
    /// <devdoc> 
    /// <para>
    /// Provides design-time support for the <see cref='System.Web.UI.WebControls.Wizard'/> web control. 
    /// </para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, 
        Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class CreateUserWizardDesigner : WizardDesigner {

        private CreateUserWizard _createUserWizard; 

        private const string _userNameID = "UserName"; 
        private const string _passwordID = "Password"; 
        private const string _confirmPasswordID = "ConfirmPassword";
        private const string _unknownErrorMessageID = "ErrorMessage"; 
        private const string _emailID = "Email";
        private const string _questionID = "Question";
        private const string _answerID = "Answer";
        private const string _userNameRequiredID = "UserNameRequired"; 
        private const string _passwordRequiredID = "PasswordRequired";
        private const string _confirmPasswordRequiredID = "ConfirmPasswordRequired"; 
        private const string _passwordRegExpID = "PasswordRegExp"; 
        private const string _emailRequiredID = "EmailRequired";
        private const string _emailRegExpID = "EmailRegExp"; 
        private const string _questionRequiredID = "QuestionRequired";
        private const string _answerRequiredID = "AnswerRequired";
        private const string _passwordCompareID = "PasswordCompare";
        private const string _cancelButtonID = "CancelButton"; 
        private const string _cancelButtonButtonID = "CancelButtonButton";
        private const string _cancelButtonImageButtonID = "CancelButtonImageButton"; 
        private const string _cancelButtonLinkButtonID = "CancelButtonLinkButton"; 
        private const string _continueButtonID = "ContinueButton";
        private const string _continueButtonButtonID = "ContinueButtonButton"; 
        private const string _continueButtonImageButtonID = "ContinueButtonImageButton";
        private const string _continueButtonLinkButtonID = "ContinueButtonLinkButton";
        private const string _helpLinkID = "HelpLink";
        private const string _editProfileLinkID = "EditProfileLink"; 
        private const string _createUserButtonID = "StepNextButton";
        private const string _createUserButtonButtonID = "StepNextButtonButton"; 
        private const string _createUserButtonImageButtonID = "StepNextButtonImageButton"; 
        private const string _createUserButtonLinkButtonID = "StepNextButtonLinkButton";
        private const string _createUserNavigationTemplateName = "CreateUserNavigationTemplate"; 
        private const string _previousButtonID = "StepNextButton";
        private const string _previousButtonButtonID = "StepPreviousButton";
        private const string _previousButtonImageButtonID = "StepPreviousButtonImageButton";
        private const string _previousButtonLinkButtonID = "StepPreviousButtonLinkButton"; 

        private static DesignerAutoFormatCollection _autoFormats; 
 
        private static readonly Hashtable _persistedIDConverter;
        private static readonly Hashtable _completeStepConverter; 
        static CreateUserWizardDesigner() {
            _persistedIDConverter = new Hashtable();
            _persistedIDConverter.Add(_cancelButtonImageButtonID, _cancelButtonID);
            _persistedIDConverter.Add(_cancelButtonButtonID, _cancelButtonID); 
            _persistedIDConverter.Add(_cancelButtonLinkButtonID, _cancelButtonID);
            _persistedIDConverter.Add(_createUserButtonImageButtonID, _createUserButtonID); 
            _persistedIDConverter.Add(_createUserButtonButtonID, _createUserButtonID); 
            _persistedIDConverter.Add(_createUserButtonLinkButtonID, _createUserButtonID);
            _persistedIDConverter.Add(_previousButtonImageButtonID, _previousButtonID); 
            _persistedIDConverter.Add(_previousButtonButtonID, _previousButtonID);
            _persistedIDConverter.Add(_previousButtonLinkButtonID, _previousButtonID);

            _completeStepConverter = new Hashtable(); 
            _completeStepConverter.Add(_continueButtonImageButtonID, _continueButtonID);
            _completeStepConverter.Add(_continueButtonButtonID, _continueButtonID); 
            _completeStepConverter.Add(_continueButtonLinkButtonID, _continueButtonID); 
        }
 
        // Controls that are persisted when converting to template
        private static readonly string[] _persistedControlIDs = new string[] {
            _userNameID,
            _userNameRequiredID, 
            _passwordID,
            _passwordRequiredID, 
            _confirmPasswordID, 
            _emailID,
            _questionID, 
            _answerID,
            _confirmPasswordRequiredID,
            _passwordRegExpID,
            _emailRegExpID, 
            _emailRequiredID,
            _questionRequiredID, 
            _answerRequiredID, 
            _passwordCompareID,
            _cancelButtonID, 
            _continueButtonID,
            _createUserButtonID,
            _unknownErrorMessageID,
            _helpLinkID, 
            _editProfileLinkID,
        }; 
 
        // Controls that are persisted even if they are not visible when the control is rendered
        // They are not visible at design-time because the values are computed at runtime 
        private static readonly string[] _persistedIfNotVisibleControlIDs = new string[] {
            _unknownErrorMessageID,
        };
 
        // Removed from the property grid when there is a user template for the create step
        private static readonly string[] _defaultCreateStepProperties = new string[] { 
            "AnswerLabelText", 
            "ConfirmPasswordLabelText",
            "ConfirmPasswordCompareErrorMessage", 
            "ConfirmPasswordRequiredErrorMessage",
            "EmailLabelText",
            "ErrorMessageStyle",
            "HelpPageIconUrl", 
            "HelpPageText",
            "HelpPageUrl", 
            "HyperLinkStyle", 
            "InstructionText",
            "InstructionTextStyle", 
            "LabelStyle",
            "PasswordHintText",
            "PasswordHintStyle",
            "PasswordLabelText", 
            "PasswordRequiredErrorMessage",
            "QuestionLabelText", 
            "TextBoxStyle", 
            "UserNameLabelText",
            "UserNameRequiredErrorMessage", 
            "AnswerRequiredErrorMessage",
            "EmailRegularExpression",
            "EmailRegularExpressionErrorMessage",
            "EmailRequiredErrorMessage", 
            "PasswordRegularExpression",
            "PasswordRegularExpressionErrorMessage", 
            "QuestionRequiredErrorMessage", 
            "ValidatorTextStyle",
        }; 

        private static readonly string[] _defaultCreateUserNavProperties = new string[] {
            "CancelButtonImageUrl",
            "CancelButtonType", 
            "CancelButtonStyle",
            "CancelButtonText", 
            "CreateUserButtonImageUrl", 
            "CreateUserButtonType",
            "CreateUserButtonStyle", 
            "CreateUserButtonText",
        };

        // Removed from the property grid when there is a user template for the create step 
        private static readonly string[] _defaultCompleteStepProperties = new string[] {
            "CompleteSuccessText", 
            "CompleteSuccessTextStyle", 
            "ContinueButtonStyle",
            "ContinueButtonText", 
            "ContinueButtonType",
            "ContinueButtonImageUrl",
            "EditProfileText",
            "EditProfileIconUrl", 
            "EditProfileUrl",
        }; 
 
        private static bool IsStepEmpty(WizardStepBase step) {
            if (!(step is CreateUserWizardStep) && !(step is CompleteWizardStep)) { 
                return false;
            }

            TemplatedWizardStep templatedStep = (TemplatedWizardStep)step; 
            return templatedStep.ContentTemplate == null;
        } 
 
        internal override bool InRegionEditingMode(Wizard viewControl) {
            if (!SupportsDesignerRegions) { 
                return true;
            }

            // If the activestep's content is empty, renders them since they are not using RegionEditing 
            if (IsStepEmpty(_createUserWizard.ActiveStep)) {
                return true; 
            } 

            return false; 
        }

        /// <include file='doc\CreateUserWizardDesigner.uex' path='docs/doc[@for="CreateUserWizardDesigner.ActionLists"]/*' />
        public override DesignerActionListCollection ActionLists { 
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection(); 
                actionLists.AddRange(base.ActionLists); 
                actionLists.Add(new CreateUserWizardDesignerActionList(this));
 
                return actionLists;
            }
        }
 
        public override DesignerAutoFormatCollection AutoFormats {
            get { 
                if (_autoFormats == null) { 
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.CREATEUSERWIZARD_SCHEMES,
                        delegate(DataRow schemeData) { return new CreateUserWizardAutoFormat(schemeData); }); 
                }
                return _autoFormats;
            }
        } 

        protected override bool UsePreviewControl { 
            get { 
                return true;
            } 
        }

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.AddDesignerRegions1"]/*' />
        protected override void AddDesignerRegions(DesignerRegionCollection regions) { 
            if (!SupportsDesignerRegions) {
                return; 
            } 

            // Recreate the controls if we have no create user step, this will retrigger any exceptions as well 
            if (_createUserWizard.CreateUserStep == null) {
                CreateChildControls();

                // Give up if its still not created 
                if (_createUserWizard.CreateUserStep == null) {
                    return; 
                } 
            }
 
            // Default steps are not editable
            bool defaultCreateStep = _createUserWizard.CreateUserStep.ContentTemplate == null;
            bool defaultCompleteStep = _createUserWizard.CompleteStep.ContentTemplate == null;
            foreach (WizardStepBase step in _createUserWizard.WizardSteps) { 
                bool defaultStep = ((defaultCreateStep && step is CreateUserWizardStep) ||
                                    (defaultCompleteStep && step is CompleteWizardStep)); 
                DesignerRegion reg = null; 
                if (!defaultStep) {
                    if (step is TemplatedWizardStep) { 
                        TemplateDefinition definition = new TemplateDefinition(
                            this, _contentTemplateName, _createUserWizard, _contentTemplateName, TemplateStyleArray[_navigationStyleLength - 1]);
                        reg = new WizardStepTemplatedEditableRegion(definition, step);
                        reg.EnsureSize = false; 
                    }
                    else { 
                        reg = new WizardStepEditableRegion(this, step); 
                    }
                    reg.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark); 
                }
                else reg = new WizardSelectableRegion(this, GetRegionName(step), step);
                regions.Add(reg);
            } 
            foreach (WizardStepBase step in _createUserWizard.WizardSteps) {
                WizardSelectableRegion reg = new WizardSelectableRegion(this, "Move to " + GetRegionName(step), step); 
                if (_createUserWizard.ActiveStep == step) { 
                    reg.Selected = true;
                } 
                regions.Add(reg);
            }
        }
 
        /// <include file='doc\CreateUserWizardDesigner.uex' path='docs/doc[@for="CreateUserWizardDesigner.OnConvertToCustomNavigationTemplate"]/*' />
        protected override void ConvertToCustomNavigationTemplate() { 
            try { 
                if (_createUserWizard.ActiveStep == _createUserWizard.CreateUserStep) {
                    IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                    Debug.Assert(designerHost != null);

                    ITemplate template = ((CreateUserWizard)ViewControl).CreateUserStep.CustomNavigationTemplate;
                    if (template == null) { 
                        IControlDesignerAccessor accessor = (IControlDesignerAccessor)_createUserWizard;
                        IDictionary dictionary = accessor.GetDesignModeState(); 
 
                        ControlCollection controls = dictionary[_customNavigationControls] as ControlCollection;
                        if (controls != null) { 
                            string persistedText = String.Empty;
                            foreach (Control ctrl in controls) {
                                if (ctrl != null && ctrl.Visible) {
                                    // Convert to the right control ID 
                                    foreach (string ID in _persistedIDConverter.Keys) {
                                        Control control = ctrl.FindControl(ID); 
                                        if (control != null && control.Visible) { 
                                            control.ID = (String)_persistedIDConverter[ID];
                                        } 
                                    }

                                    // We just want the buttons to remain in persistance format
                                    if (ctrl is Table) { 
                                        persistedText += ConvertNavigationTableToHtmlTable((Table)ctrl);
                                        // Everything else should look like html 
                                    } 
                                    else {
                                        StringWriter stringWriter = new StringWriter(CultureInfo.CurrentCulture); 
                                        HtmlTextWriter writer = new HtmlTextWriter(stringWriter);
                                        ctrl.RenderControl(writer);
                                        persistedText += stringWriter.ToString();
                                    } 
                                }
                            } 
                            template = ControlParser.ParseTemplate(designerHost, persistedText); 
                        }
                    } 

                    InvokeTransactedChange(Component, new TransactedChangeCallback(base.ConvertToCustomNavigationTemplateCallBack),
                        template, SR.GetString(SR.Wizard_ConvertToCustomNavigationTemplate));
 
                    UpdateDesignTimeHtml();
                } 
                else { 
                    base.ConvertToCustomNavigationTemplate();
                } 
            }
            catch (Exception ex) {
                Debug.Fail(ex.ToString());
            } 
        }
 
        private string ConvertTableToHtmlTable(Table originalTable, Control container) { 
            return ConvertTableToHtmlTable(originalTable, container, null);
        } 

        private string ConvertTableToHtmlTable(Table originalTable, Control container, IDictionary persistMap) {
            // Original table has style, ID, and base attributes copied from PasswordRecovery control.  We don't want these on
            // the template, so create a new table and move rows from old to new table.  Need to create temporary 
            // ArrayList because the original ControlCollection can't be modified while being enumerated.
            IList controls = new ArrayList(); 
            foreach (Control c in originalTable.Controls) { 
                controls.Add(c);
            } 
            Table table = new Table();
            foreach (Control c in controls) {
                table.Controls.Add(c);
            } 

            // The table inside the template will not inherit the font properties 
            if (originalTable.ControlStyleCreated) { 
                table.ApplyStyle(originalTable.ControlStyle);
            } 
            table.Width = ((WebControl)ViewControl).Width;
            table.Height = ((WebControl)ViewControl).Height;

            // Add the new table to the controls collection of the login container, and remove the old table 
            // Necessary to set the Site, Page, Parent, etc. of the new table
            if (container != null) { 
                container.Controls.Add(table); 
                container.Controls.Remove(originalTable);
            } 

            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));

            if (persistMap != null) { 
                // Convert to the right control ID
                foreach (string id in persistMap.Keys) { 
                    Control c = table.FindControl(id); 
                    if (c != null && c.Visible) {
                        c.ID = (string)persistMap[id]; 
                        string persisted = ControlPersister.PersistControl(c, host);
                        LiteralControl literal = new LiteralControl(persisted);
                        c.Parent.Controls.Add(literal);
                        c.Parent.Controls.Remove(c); 
                    }
                } 
            } 

            // Replace textboxes, validators, button, and errorMessage with LiteralControls 
            // that contain the persistence view of that control
            foreach (string ID in _persistedControlIDs) {
                Control control = table.FindControl(ID);
                if (control != null) { 
                    if (Array.IndexOf(_persistedIfNotVisibleControlIDs, ID) >= 0) {
                        control.Visible = true; 
                        // Set the parent table cell and table row to visible so they will get rendered 
                        control.Parent.Visible = true;
                        control.Parent.Parent.Visible = true; 
                    }
                    // Special case to apply style to failure text table cell, since the failure text is empty at design time
                    if (ID == _unknownErrorMessageID) {
                        TableCell errorMessagCell = (TableCell)(control.Parent); 
                        errorMessagCell.ForeColor = Color.Red;
                        errorMessagCell.ApplyStyle(_createUserWizard.ErrorMessageStyle); 
 
                        // Turn off viewstate for the failure text so postbacks don't retain the failure text
                        // VSWhidbey 195651 
                        control.EnableViewState = false;
                    }
                    if (control.Visible) {
                        string persisted = ControlPersister.PersistControl(control, host); 
                        LiteralControl literal = new LiteralControl(persisted);
                        control.Parent.Controls.Add(literal); 
                        control.Parent.Controls.Remove(control); 
                    }
                } 
            }

            // Render table to HTML
            StringWriter stringWriter = new StringWriter(CultureInfo.CurrentCulture); 
            HtmlTextWriter writer = new HtmlTextWriter(stringWriter);
            table.RenderControl(writer); 
 
            return stringWriter.ToString();
        } 

        private string ConvertNavigationTableToHtmlTable(Table table) {
            // Needed to make the failure text visible
            IControlDesignerAccessor accessor = (IControlDesignerAccessor)_createUserWizard; 
            IDictionary dictionary = accessor.GetDesignModeState();
 
            StringWriter strWriter = new StringWriter(CultureInfo.CurrentCulture); 
            HtmlTextWriter writer = new HtmlTextWriter(strWriter);
            if (table.Width != Unit.Empty) writer.AddStyleAttribute(HtmlTextWriterStyle.Width, table.Width.ToString(CultureInfo.CurrentCulture)); 
            if (table.Height != Unit.Empty) writer.AddStyleAttribute(HtmlTextWriterStyle.Height, table.Height.ToString(CultureInfo.CurrentCulture));
            if (table.CellSpacing != 0) writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, table.CellSpacing.ToString(CultureInfo.CurrentCulture));
            string border = "0";
            if (table.BorderWidth != Unit.Empty) { 
                border = table.BorderWidth.ToString(CultureInfo.CurrentCulture);
            } 
            writer.AddAttribute(HtmlTextWriterAttribute.Border, border); 
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
 
            ArrayList rowCells = new ArrayList(table.Rows.Count);
            foreach (TableRow row in table.Rows) {
                if (!row.Visible) continue;
                ArrayList cells = new ArrayList(row.Cells.Count); 
                foreach (TableCell cell in row.Cells) {
                    if (!cell.Visible || !cell.HasControls()) continue; 
                    ArrayList controls = new ArrayList(cell.Controls.Count); 
                    foreach (Control control in cell.Controls) {
                        if (!control.Visible) continue; 
                        if (control is Literal && control.ID != _unknownErrorMessageID && ((Literal)control).Text.Length == 0) continue;
                        if (control is HyperLink && ((HyperLink)control).Text.Length == 0) continue;
                        if (control is System.Web.UI.WebControls.Image &&
                            ((System.Web.UI.WebControls.Image)control).ImageUrl.Length == 0) continue; 
                        controls.Add(control);
                    } 
                    if (controls.Count > 0) cells.Add(new CellControls(cell, controls)); 
                }
                if (cells.Count > 0) rowCells.Add(new RowCells(row, cells)); 
            }

            foreach (RowCells rc in rowCells) {
                switch (rc._row.HorizontalAlign) { 
                case HorizontalAlign.Center:
                    writer.AddAttribute(HtmlTextWriterAttribute.Align, "center"); 
                    break; 
                case HorizontalAlign.Right:
                    writer.AddAttribute(HtmlTextWriterAttribute.Align, "right"); 
                    break;
                }
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
 
                foreach (CellControls cell in rc._cells) {
                    switch (cell._cell.HorizontalAlign) { 
                    case HorizontalAlign.Center: 
                        writer.AddAttribute(HtmlTextWriterAttribute.Align, "center");
                        break; 
                    case HorizontalAlign.Right:
                        writer.AddAttribute(HtmlTextWriterAttribute.Align, "right");
                        break;
                    } 

                    writer.AddAttribute(HtmlTextWriterAttribute.Colspan, cell._cell.ColumnSpan.ToString(CultureInfo.CurrentCulture)); 
 
                    StringBuilder cellContents = new StringBuilder();
                    foreach (Control control in cell._controls) { 
                        bool failureLiteral = control.ID == _unknownErrorMessageID;
                        // Don't show the empty controls unless its required
                        if (control is Literal && !failureLiteral) {
                            // Write out literals as text 
                            cellContents.Append(((Literal)control).Text);
                            continue; 
                        } 

                        // Special case to apply style to failure text table cell, since the failure text is empty at design time 
                        if (failureLiteral) {
                            writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "Red");

                            // Turn off viewstate for the failure text so postbacks don't retain the failure text 
                            // VSWhidbey 195651
                            control.EnableViewState = false; 
                        } 

                        cellContents.Append(ControlPersister.PersistControl(control)); 
                    }

                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(cellContents.ToString()); 

                    writer.RenderEndTag(); 
                } 
                writer.RenderEndTag();
            } 

            writer.RenderEndTag();
            return strWriter.ToString();
        } 

        internal override string GetEditableDesignerRegionContent(IWizardStepEditableRegion region) { 
            if (region == null) 
                throw new ArgumentNullException("region");
 
            StringBuilder sb = new StringBuilder();
            if (region.Step == _createUserWizard.CreateUserStep &&
                ((CreateUserWizardStep)region.Step).ContentTemplate == null &&
                region.Step.Controls[0] is Table) { 
                //
                Table table = (Table)(((Table)region.Step.Controls[0]).Rows[0].Cells[0].Controls[0]); 
                sb.Append(ConvertTableToHtmlTable(table, ((TemplatedWizardStep)region.Step).ContentTemplateContainer)); 
                return sb.ToString();
            } 
            else if (region.Step == _createUserWizard.CompleteStep &&
                     ((CompleteWizardStep)region.Step).ContentTemplate == null &&
                     region.Step.Controls[0] is Table) {
                // 
                Table table = (Table)(((Table)region.Step.Controls[0]).Rows[0].Cells[0].Controls[0]);
                sb.Append(ConvertTableToHtmlTable(table, ((TemplatedWizardStep)region.Step).ContentTemplateContainer)); 
                return sb.ToString(); 
            }
 
            return base.GetEditableDesignerRegionContent(region);
        }

        /// <include file='doc\LoginDesigner.uex' path='docs/doc[@for="LoginDesigner.GetErrorDesignTimeHtml"]/*' /> 
        protected override string GetErrorDesignTimeHtml(Exception e) {
            return CreatePlaceHolderDesignTimeHtml(SR.GetString(SR.Control_ErrorRenderingShort) + "<br />" + e.Message); 
        } 

        /// <include file='doc\CreateUserWizardDesigner.uex' path='docs/doc[@for="CreateUserWizardDesigner.Initialize"]/*' /> 
        public override void Initialize(IComponent component) {
            VerifyInitializeArgument(component, typeof(CreateUserWizard));

            // DevDiv Bugs 71165: Must set _createUserWizard before calling base 
            _createUserWizard = (CreateUserWizard)component;
 
            base.Initialize(component); 

        } 

        private void LaunchWebAdmin() {
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (designerHost != null) { 
                IWebAdministrationService webadmin = (IWebAdministrationService)designerHost.GetService(typeof(IWebAdministrationService));
                if (webadmin != null) { 
                    webadmin.Start(null); 
                }
            } 
        }

        /// <devdoc>
        /// Creates a template that looks identical to the current rendering of the control, and tells the control 
        /// to use this template.  Allows a page developer to customize the control using its style properties, then
        /// convert to a template and modify the template for further customization.  The template contains the 
        /// necessary server controls with the correct IDs. 
        /// </devdoc>
        private void CustomizeCompleteStep() { 
            IComponent component = (IComponent)_createUserWizard.CompleteStep;

            PropertyDescriptor activeStepIndexDescriptor = TypeDescriptor.GetProperties(Component)["ActiveStepIndex"];
            int index = _createUserWizard.WizardSteps.IndexOf(_createUserWizard.CompleteStep); 
            InvokeTransactedChange(Component, new TransactedChangeCallback(NavigateToStep), index,
                                   SR.GetString(SR.CreateUserWizard_NavigateToStep, index), activeStepIndexDescriptor); 
 
            PropertyDescriptor templateDescriptor = TypeDescriptor.GetProperties(component)["ContentTemplate"];
            InvokeTransactedChange(Component.Site, component, new TransactedChangeCallback(CustomizeCompleteStepCallback), null, 
                                   SR.GetString(SR.CreateUserWizard_CustomizeCompleteStep), templateDescriptor);
        }

        private bool CustomizeCompleteStepCallback(object context) { 
            Debug.Assert(_createUserWizard.CompleteStep.ContentTemplate == null);
 
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null);
 
            CreateUserWizard createUserWizard = (CreateUserWizard)ViewControl;

            // Skin with template scenario
            ITemplate contentTemplate = createUserWizard.CompleteStep.ContentTemplate; 
            if (contentTemplate == null) {
                try { 
                    Hashtable convertOn = new Hashtable(1); 
                    convertOn.Add("ConvertToTemplate", true);
                    ((IControlDesignerAccessor)ViewControl).SetDesignModeState(convertOn); 

                    // Causes the control to set its child properties
                    ViewControlCreated = false;
                    GetDesignTimeHtml(); 

                    createUserWizard = (CreateUserWizard)ViewControl; 
                    IControlDesignerAccessor accessor = (IControlDesignerAccessor)createUserWizard; 
                    IDictionary dictionary = accessor.GetDesignModeState();
 
                    StringBuilder sb = new StringBuilder();
                    // Only control in step is the container who has a table first
                    TemplatedWizardStep step = createUserWizard.CompleteStep;
                    Table table = (Table)(((Table)step.Controls[0].Controls[0]).Rows[0].Cells[0].Controls[0]); 

                    // Apply control style to the table before converting 
                    if (createUserWizard.ControlStyleCreated) { 
                        Style style = createUserWizard.ControlStyle;
                        table.ForeColor = style.ForeColor; 
                        table.BackColor = style.BackColor;
                        table.Font.CopyFrom(style.Font);
                        table.Font.Size = new FontUnit(Unit.Percentage(100));
                    } 

                    // Apply Step style to the table before converting 
                    Style stepStyle = createUserWizard.StepStyle; 
                    if (!stepStyle.IsEmpty) {
                        table.ForeColor = stepStyle.ForeColor; 
                        table.BackColor = stepStyle.BackColor;
                        table.Font.CopyFrom(stepStyle.Font);
                        table.Font.Size = new FontUnit(Unit.Percentage(100));
                    } 

                    sb.Append(ConvertTableToHtmlTable(table, step.ContentTemplateContainer, _completeStepConverter)); 
 
                    contentTemplate = ControlParser.ParseTemplate(designerHost, sb.ToString());
 
                    Hashtable convertOff = new Hashtable(1);
                    convertOff.Add("ConvertToTemplate", false);
                    ((IControlDesignerAccessor)ViewControl).SetDesignModeState(convertOff);
                } 
                catch (Exception e) {
                    Debug.Fail(e.Message); 
                    return false; 
                }
            } 

            IComponent component = (IComponent)_createUserWizard.CompleteStep;
            PropertyDescriptor templateDescriptor = TypeDescriptor.GetProperties(component)["ContentTemplate"];
            templateDescriptor.SetValue(component, contentTemplate); 

            UpdateDesignTimeHtml(); 
 
            return true;
        } 

        /// <devdoc>
        /// Creates a template that looks identical to the current rendering of the control, and tells the control
        /// to use this template.  Allows a page developer to customize the control using its style properties, then 
        /// convert to a template and modify the template for further customization.  The template contains the
        /// necessary server controls with the correct IDs. 
        /// </devdoc> 
        private void CustomizeCreateUserStep() {
            IComponent component = (IComponent)_createUserWizard.CreateUserStep; 

            PropertyDescriptor activeStepIndexDescriptor = TypeDescriptor.GetProperties(Component)["ActiveStepIndex"];
            int index = _createUserWizard.WizardSteps.IndexOf(_createUserWizard.CreateUserStep);
            InvokeTransactedChange(Component, new TransactedChangeCallback(NavigateToStep), index, 
                                   SR.GetString(SR.CreateUserWizard_NavigateToStep, index), activeStepIndexDescriptor);
 
            PropertyDescriptor templateDescriptor = TypeDescriptor.GetProperties(component)["ContentTemplate"]; 
            InvokeTransactedChange(Component.Site, component, new TransactedChangeCallback(CustomizeCreateUserStepCallback), null,
                                   SR.GetString(SR.CreateUserWizard_CustomizeCreateUserStep), templateDescriptor); 
        }

        private bool NavigateToStep(object context) {
            try { 
                int stepIndex = (int)context;
                _createUserWizard.ActiveStepIndex = stepIndex; 
 
                return true;
            } 
            catch (Exception ex) {
                Debug.Fail(ex.Message);
                return false;
            } 
        }
 
        private bool CustomizeCreateUserStepCallback(object context) { 
            try {
                IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost)); 
                Debug.Assert(designerHost != null);

                CreateUserWizard createUserWizard = (CreateUserWizard)ViewControl;
 
                // contentTemplate is defined when the skin has a template
                ITemplate contentTemplate = createUserWizard.CreateUserStep.ContentTemplate; 
                if (contentTemplate == null) { 

                    // Causes the control to set its child properties 
                    ViewControlCreated = false;

                    Hashtable convertOn = new Hashtable(1);
                    convertOn.Add("ConvertToTemplate", true); 
                    ((IControlDesignerAccessor)ViewControl).SetDesignModeState(convertOn);
 
                    GetDesignTimeHtml(); 

                    // Get the new view control 
                    createUserWizard = (CreateUserWizard)ViewControl;
                    Debug.Assert(createUserWizard.CreateUserStep.ContentTemplate == null);

                    IControlDesignerAccessor accessor = (IControlDesignerAccessor)createUserWizard; 
                    IDictionary dictionary = accessor.GetDesignModeState();
 
                    StringBuilder sb = new StringBuilder(); 
                    // Only control in step is the container who has a table first
                    TemplatedWizardStep step = createUserWizard.CreateUserStep; 
                    Table table = (Table)(((Table)step.Controls[0].Controls[0]).Rows[0].Cells[0].Controls[0]);

                    // Apply control style to the table before converting
                    if (createUserWizard.ControlStyleCreated)  { 
                        Style style = createUserWizard.ControlStyle;
                        table.ForeColor = style.ForeColor; 
                        table.BackColor = style.BackColor; 
                        table.Font.CopyFrom(style.Font);
                        table.Font.Size = new FontUnit(Unit.Percentage(100)); 
                    }

                    // Apply Step style to the table before converting
                    Style stepStyle = createUserWizard.StepStyle; 
                    if (!stepStyle.IsEmpty) {
                        table.ForeColor = stepStyle.ForeColor; 
                        table.BackColor = stepStyle.BackColor; 
                        table.Font.CopyFrom(stepStyle.Font);
                        table.Font.Size = new FontUnit(Unit.Percentage(100)); 
                    }

                    sb.Append(ConvertTableToHtmlTable(table, step.ContentTemplateContainer));
 
                    contentTemplate = ControlParser.ParseTemplate(designerHost, sb.ToString());
 
                    Hashtable convertOff = new Hashtable(1); 
                    convertOff.Add("ConvertToTemplate", false);
                    ((IControlDesignerAccessor)createUserWizard).SetDesignModeState(convertOff); 
                }

                IComponent component = (IComponent)_createUserWizard.CreateUserStep;
                PropertyDescriptor templateDescriptor = TypeDescriptor.GetProperties(component)["ContentTemplate"]; 
                templateDescriptor.SetValue(_createUserWizard.CreateUserStep, contentTemplate);
 
                UpdateDesignTimeHtml(); 

                return true; 
            } catch (Exception e) {
                Debug.Fail(e.Message);
                return false;
            } 
        }
 
        /// <include file='doc\CreateUserWizardDesigner.uex' path='docs/doc[@for="CreateUserWizardDesigner.PreFilterProperties"]/*' /> 
        /// <devdoc>
        /// If the default steps are not used, remove properties that do not apply. 
        /// </devdoc>
        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties);
            TemplatedWizardStep createStep = _createUserWizard.CreateUserStep; 
            bool defaultCreateStep = (createStep != null && createStep.ContentTemplate != null);
            if (defaultCreateStep) { 
                foreach (string propertyName in _defaultCreateStepProperties) { 
                    PropertyDescriptor property = (PropertyDescriptor) properties[propertyName];
                    Debug.Assert(property != null, "Property is null: " + propertyName); 
                    if (property != null) {
                        properties[propertyName] = TypeDescriptor.CreateProperty(property.ComponentType, property, BrowsableAttribute.No);
                    }
                } 
            }
 
            TemplatedWizardStep completeStep = _createUserWizard.CompleteStep; 
            bool defaultCompleteStep = (completeStep != null && completeStep.ContentTemplate != null);
            if (defaultCompleteStep) { 
                foreach (string propertyName in _defaultCompleteStepProperties) {
                    PropertyDescriptor property = (PropertyDescriptor) properties[propertyName];
                    Debug.Assert(property != null, "Property is null: " + propertyName);
                    if (property != null) { 
                        properties[propertyName] = TypeDescriptor.CreateProperty(property.ComponentType, property, BrowsableAttribute.No);
                    } 
                } 
            }
            if (createStep != null && createStep.CustomNavigationTemplate != null) { 
                foreach (string propertyName in _defaultCreateUserNavProperties) {
                    PropertyDescriptor property = (PropertyDescriptor) properties[propertyName];
                    Debug.Assert(property != null, "Property is null: " + propertyName);
                    if (property != null) { 
                        properties[propertyName] = TypeDescriptor.CreateProperty(property.ComponentType, property, BrowsableAttribute.No);
                    } 
                } 
            }
 
            if (defaultCompleteStep && defaultCreateStep) {
                // Only TitleTextStyle should remain unless both are templated
                PropertyDescriptor property = (PropertyDescriptor) properties["TitleTextStyle"];
                if (property != null) { 
                    properties["TitleTextStyle"] = TypeDescriptor.CreateProperty(property.ComponentType, property, BrowsableAttribute.No);
                } 
 
            }
        } 

        private bool ResetCallback(object context) {
            try {
                IComponent component = (IComponent)context; 
                PropertyDescriptor templateDescriptor = TypeDescriptor.GetProperties(component)["ContentTemplate"];
                templateDescriptor.SetValue(component, null); 
 
                return true;
            } 
            catch (Exception e) {
                Debug.Fail(e.Message);
                return false;
            } 
        }
 
        /// <devdoc> 
        /// Restores the default finish step
        /// </devdoc> 
        private void ResetCompleteStep() {

            //
 
            UpdateDesignTimeHtml();
 
            PropertyDescriptor templateDescriptor = TypeDescriptor.GetProperties(Component)["WizardSteps"]; 
            InvokeTransactedChange(Component, new TransactedChangeCallback(ResetCallback), _createUserWizard.CompleteStep,
                                   SR.GetString(SR.CreateUserWizard_ResetCompleteStepVerb), templateDescriptor); 
        }

        /// <devdoc>
        /// Restores the default create user step 
        /// </devdoc>
        private void ResetCreateUserStep() { 
            // 

            UpdateDesignTimeHtml(); 

            PropertyDescriptor templateDescriptor = TypeDescriptor.GetProperties(Component)["WizardSteps"];
            InvokeTransactedChange(Component, new TransactedChangeCallback(ResetCallback), _createUserWizard.CreateUserStep,
                                   SR.GetString(SR.CreateUserWizard_ResetCreateUserStepVerb), templateDescriptor); 
        }
 
        // Helper class for ConvertTableToHtmlTable 
        private class RowCells {
            internal TableRow _row; 
            internal ArrayList _cells;

            internal RowCells(TableRow row, ArrayList cells) {
                _row = row; 
                _cells = cells;
            } 
        } 

        // Helper class for ConvertTableToHtmlTable 
        private class CellControls {
            internal TableCell _cell;
            internal ArrayList _controls;
 
            internal CellControls(TableCell cell, ArrayList controls) {
                _cell = cell; 
                _controls = controls; 
            }
        } 

        private class CreateUserWizardDesignerActionList : DesignerActionList {
            private CreateUserWizardDesigner _parent;
 
            public CreateUserWizardDesignerActionList(CreateUserWizardDesigner parent) : base(parent.Component) {
                _parent = parent; 
            } 

            public override bool AutoShow { 
                get {
                    return true;
                }
                set { 
                }
            } 
 
            public void CustomizeCreateUserStep() {
                Cursor originalCursor = Cursor.Current; 
                try {
                    Cursor.Current = Cursors.WaitCursor;
                    _parent.CustomizeCreateUserStep();
                } 
                finally {
                    Cursor.Current = originalCursor; 
                } 
            }
 
            public void CustomizeCompleteStep() {
                Cursor originalCursor = Cursor.Current;
                try {
                    Cursor.Current = Cursors.WaitCursor; 
                    _parent.CustomizeCompleteStep();
                } 
                finally { 
                    Cursor.Current = originalCursor;
                } 
            }

            public void LaunchWebAdmin() {
                _parent.LaunchWebAdmin(); 
            }
 
            public void ResetCreateUserStep() { 
                _parent.ResetCreateUserStep();
            } 

            public void ResetCompleteStep() {
                _parent.ResetCompleteStep();
            } 

            public override DesignerActionItemCollection GetSortedActionItems() { 
                if (_parent.InTemplateMode) { 
                    return new DesignerActionItemCollection();
                } 

                DesignerActionItemCollection items = new DesignerActionItemCollection();
                if (_parent._createUserWizard.CreateUserStep.ContentTemplate == null) {
                    items.Add(new DesignerActionMethodItem(this, "CustomizeCreateUserStep", 
                        SR.GetString(SR.CreateUserWizard_CustomizeCreateUserStep), String.Empty,
                        SR.GetString(SR.CreateUserWizard_CustomizeCreateUserStepDescription), true)); 
                } 
                else {
                    items.Add(new DesignerActionMethodItem(this, "ResetCreateUserStep", 
                        SR.GetString(SR.CreateUserWizard_ResetCreateUserStepVerb), String.Empty,
                        SR.GetString(SR.CreateUserWizard_ResetCreateUserStepVerbDescription), true));
                }
 
                if (_parent._createUserWizard.CompleteStep.ContentTemplate == null) {
                    items.Add(new DesignerActionMethodItem(this, "CustomizeCompleteStep", 
                        SR.GetString(SR.CreateUserWizard_CustomizeCompleteStep), String.Empty, 
                        SR.GetString(SR.CreateUserWizard_CustomizeCompleteStepDescription), true));
                } 
                else {
                    items.Add(new DesignerActionMethodItem(this, "ResetCompleteStep",
                        SR.GetString(SR.CreateUserWizard_ResetCompleteStepVerb), String.Empty,
                        SR.GetString(SR.CreateUserWizard_ResetCompleteStepVerbDescription), true)); 
                }
 
                items.Add(new DesignerActionMethodItem(this, "LaunchWebAdmin", SR.GetString(SR.Login_LaunchWebAdmin), 
                    String.Empty, SR.GetString(SR.Login_LaunchWebAdminDescription), true));
 
                return items;
            }
        }
 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
