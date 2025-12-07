//------------------------------------------------------------------------------ 
/// <copyright from='1997' to='2001' company='Microsoft Corporation'>
///    Copyright (c) Microsoft Corporation. All Rights Reserved.
///    Information Contained Herein is Proprietary and Confidential.
/// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Web.UI.Design.WebControls { 

    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Drawing.Design;
    using System.Globalization; 
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Web; 
    using System.Web.UI.Design;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls; 
    using System.Windows.Forms.Design;
 
    /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner"]/*' />
    /// <devdoc>
    /// <para>
    /// Provides design-time support for the <see cref='System.Web.UI.WebControls.Wizard'/> web control. 
    /// </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, 
        Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class WizardDesigner : CompositeControlDesigner { 

        private Wizard _wizard;
        private DesignerAutoFormatCollection _autoFormats;
 
        private bool _supportsDesignerRegion;
        private bool _supportsDesignerRegionQueried; 
 
        private const string _headerTemplateName = "HeaderTemplate";
        internal const string _customNavigationTemplateName = "CustomNavigationTemplate"; 
        private const string _startNavigationTemplateName = "StartNavigationTemplate";
        private const string _stepNavigationTemplateName = "StepNavigationTemplate";
        private const string _finishNavigationTemplateName = "FinishNavigationTemplate";
        private const string _sideBarTemplateName = "SideBarTemplate"; 
        private const string _activeStepIndexPropName = "ActiveStepIndex";
        private const string _activeStepIndexTransactionDescription = "Update ActiveStepIndex"; 
        private const string _startNextButtonID = "StartNextButton"; 
        private const string _cancelButtonID = "CancelButton";
        private const string _stepTableCellID = "StepTableCell"; 
        private const string _displaySideBarPropName = "DisplaySideBar";

        private const string _stepPreviousButtonID = "StepPreviousButton";
        private const string _stepNextButtonID = "StepNextButton"; 
        private const string _finishButtonID = "FinishButton";
        private const string _finishPreviousButtonID = "FinishPreviousButton"; 
        private const string _dataListID = "SideBarList"; 
        private const string _sideBarButtonID = "SideBarButton";
        internal const string _customNavigationControls = "CustomNavigationControls"; 
        private const string _wizardStepsPropertyName = "WizardSteps";

        internal const string _contentTemplateName = "ContentTemplate";
        private const string _navigationTemplateName = "CustomNavigationTemplate"; 
        private static string[] _stepTemplateNames = new string[] { _contentTemplateName, _navigationTemplateName };
 
        internal const int _navigationStyleLength = 6; 

        private static string[] _controlTemplateNames = new string[] { 
            _headerTemplateName,
            _sideBarTemplateName,
            _startNavigationTemplateName,
            _stepNavigationTemplateName, 
            _finishNavigationTemplateName,
        }; 
 
        private static readonly string[] _startNavigationTemplateProperties = new string[] {
            "StartNextButtonText", "StartNextButtonType", "StartNextButtonImageUrl", 
            "StartNextButtonStyle",
        };

        private static readonly string[] _stepNavigationTemplateProperties = new string[] { 
            "StepNextButtonText", "StepNextButtonType", "StepNextButtonImageUrl",
            "StepPreviousButtonText", "StepPreviousButtonType", "StepPreviousButtonImageUrl", 
            "StepPreviousButtonStyle", "StepNextButtonStyle", 
        };
 
        private static readonly string[] _finishNavigationTemplateProperties = new string[] {
            "FinishCompleteButtonText", "FinishCompleteButtonType", "FinishCompleteButtonImageUrl",
            "FinishPreviousButtonText", "FinishPreviousButtonType", "FinishPreviousButtonImageUrl",
            "FinishCompleteButtonStyle", "FinishPreviousButtonStyle", 
        };
 
        private static readonly string[] _generalNavigationButtonProperties = new string[] { 
            "CancelButtonImageUrl", "CancelButtonText", "CancelButtonType", "DisplayCancelButton",
            "CancelButtonStyle", "NavigationButtonStyle", 
        };

        private static readonly string[] _headerProperties = new string[] {
            "HeaderText", 
        };
 
        private static readonly string[] _sideBarProperties = new string[] { 
            "SideBarButtonStyle",
        }; 

        private static string[] _startButtonIDs = new string[] {_startNextButtonID, _cancelButtonID};
        private static string[] _stepButtonIDs = new string[] {_stepPreviousButtonID, _stepNextButtonID, _cancelButtonID};
        private static string[] _finishButtonIDs = new string[] {_finishPreviousButtonID, _finishButtonID, _cancelButtonID}; 

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.ActionLists"]/*' /> 
        public override DesignerActionListCollection ActionLists { 
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection(); 
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new WizardDesignerActionList(this));

                return actionLists; 
            }
        } 
 
        internal WizardStepBase ActiveStep {
            get { 
                if (ActiveStepIndex != -1) {
                    return _wizard.WizardSteps[ActiveStepIndex];
                }
 
                return null;
            } 
        } 

        internal int ActiveStepIndex { 
            get {
                int index = _wizard.ActiveStepIndex;
                if (index == -1 && _wizard.WizardSteps.Count > 0) {
                    return 0; 
                }
 
                return index; 
            }
        } 

        public override DesignerAutoFormatCollection AutoFormats {
            get {
                if (_autoFormats == null) { 
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.WIZARD_SCHEMES,
                        delegate(DataRow schemeData) { return new WizardAutoFormat(schemeData); }); 
                } 
                return _autoFormats;
            } 
        }

        protected bool DisplaySideBar {
            get { 
                return ((Wizard)Component).DisplaySideBar;
            } 
            set { 
                // VSWhidbey 402538. Need to invalidate verb visibility when DisplaySideBar property changes.
                TypeDescriptor.Refresh(Component); 

                ((Wizard)Component).DisplaySideBar = value;

                TypeDescriptor.Refresh(Component); 
            }
        } 
 
        internal bool SupportsDesignerRegions {
            get { 
                if (_supportsDesignerRegionQueried) {
                    return _supportsDesignerRegion;
                }
 
                if (View != null) {
                    _supportsDesignerRegion = View.SupportsRegions; 
                } 

                _supportsDesignerRegionQueried = true; 

                return _supportsDesignerRegion;
            }
        } 

        internal virtual bool InRegionEditingMode(Wizard viewControl) { 
            if (!SupportsDesignerRegions) { 
                return true;
            } 

            // Return true if the ContentTemplate is defined through a skin file.
            TemplatedWizardStep activeStepFromWizard = ActiveStep as TemplatedWizardStep;
            if (activeStepFromWizard != null && activeStepFromWizard.ContentTemplate == null) { 
                TemplatedWizardStep mergedStep = viewControl.WizardSteps[ActiveStepIndex] as TemplatedWizardStep;
                if (mergedStep != null && mergedStep.ContentTemplate != null) { 
                    return true; 
                }
            } 

            return false;
        }
 
        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.TemplateGroups"]/*' />
        public override TemplateGroupCollection TemplateGroups { 
            get { 
                TemplateGroupCollection groups = base.TemplateGroups;
                for (int i = 0; i < _controlTemplateNames.Length; i++) { 
                    string templateName = _controlTemplateNames[i];
                    TemplateGroup templateGroup = new TemplateGroup(templateName);
                    templateGroup.AddTemplateDefinition(new TemplateDefinition(this, templateName, _wizard, templateName, TemplateStyleArray[i]));
 
                    groups.Add(templateGroup);
                } 
 
                foreach(WizardStepBase step in _wizard.WizardSteps) {
                    string templateName = GetRegionName(step); 
                    TemplateGroup templateGroup = new TemplateGroup(templateName);
                    if (step is TemplatedWizardStep) {
                        for (int i = 0; i < _stepTemplateNames.Length; i++) {
                            templateGroup.AddTemplateDefinition(new TemplateDefinition(this, _stepTemplateNames[i], step, _stepTemplateNames[i], StepTemplateStyleArray[i])); 
                        }
                    } 
                    else if (!SupportsDesignerRegions) { 
                        templateGroup.AddTemplateDefinition(new WizardStepBaseTemplateDefinition(this, step, templateName, StepTemplateStyleArray[0]));
                    } 

                    if (!templateGroup.IsEmpty) {
                        groups.Add(templateGroup);
                    } 
                }
 
                return groups; 
            }
        } 

        internal Style[] TemplateStyleArray {
            get {
                Style headerStyle = new Style(); 
                Wizard control = ((Wizard)ViewControl);
                headerStyle.CopyFrom(control.ControlStyle); 
                headerStyle.CopyFrom(control.HeaderStyle); 

                Style sideBarStyle = new Style(); 
                sideBarStyle.CopyFrom(control.ControlStyle);
                sideBarStyle.CopyFrom(control.SideBarStyle);

                Style navigationStyle = new Style(); 
                navigationStyle.CopyFrom(control.ControlStyle);
                navigationStyle.CopyFrom(control.NavigationStyle); 
 
                Style[] styleArray = new Style[] {
                    headerStyle, 
                    sideBarStyle,
                    navigationStyle,
                    navigationStyle,
                    navigationStyle, 
                    navigationStyle
                }; 
 
                Debug.Assert(styleArray.Length == _navigationStyleLength);
 
                return styleArray;
            }
        }
 
        private Style[] StepTemplateStyleArray {
            get { 
                Style stepStyle = new Style(); 
                Wizard control = ((Wizard)ViewControl);
                stepStyle.CopyFrom(control.ControlStyle); 
                stepStyle.CopyFrom(control.StepStyle);

                Style navigationStyle = new Style();
                navigationStyle.CopyFrom(control.ControlStyle); 
                navigationStyle.CopyFrom(control.NavigationStyle);
 
                return new Style[] { 
                        stepStyle,
                        navigationStyle }; 
            }
        }

        protected override bool UsePreviewControl { 
            get {
                return true; 
            } 
        }
 
        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.AddDesignerRegions"]/*' />
        protected virtual void AddDesignerRegions(DesignerRegionCollection regions) {
            if (!SupportsDesignerRegions) {
                return; 
            }
 
            foreach (WizardStepBase step in _wizard.WizardSteps) { 
                if (step is TemplatedWizardStep) {
                    TemplateDefinition definition = new TemplateDefinition( 
                        this, _contentTemplateName, _wizard, _contentTemplateName, TemplateStyleArray[_navigationStyleLength - 1]);
                    DesignerRegion region = new WizardStepTemplatedEditableRegion(definition, step);
                    region.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark);
                    regions.Add(region); 
                }
                else { 
                    DesignerRegion region = new WizardStepEditableRegion(this, step); 
                    region.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark);
                    regions.Add(region); 
                }
            }

            foreach (WizardStepBase step in _wizard.WizardSteps) { 
                regions.Add(new WizardSelectableRegion(this, "Move to " + GetRegionName(step), step));
            } 
        } 

        private ITemplate GetTemplateFromDesignModeState(string[] keys) { 
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null);

            IControlDesignerAccessor accessor = (IControlDesignerAccessor)_wizard; 
            IDictionary dictionary = accessor.GetDesignModeState();
            ResetInternalControls(dictionary); 
 
            string persistedText = String.Empty;
            foreach (string key in keys) { 
                Control ctrl = dictionary[key] as Control;
                if (ctrl != null && ctrl.Visible) {
                    // Fix the control ID to match magic IDs.
                    ctrl.ID = key; 
                    persistedText += ControlPersister.PersistControl(ctrl, designerHost);
                } 
            } 

            return ControlParser.ParseTemplate(designerHost, persistedText); 
        }

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.ConvertToTemplate"]/*' />
        protected void ConvertToTemplate(string description, IComponent component, 
                                         string templateName, string[] keys) {
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null); 

            InvokeTransactedChange(Component, new TransactedChangeCallback(ConvertToTemplateCallBack), 
                new Triplet(component, templateName, keys), description);

            UpdateDesignTimeHtml();
        } 

        private bool ConvertToTemplateCallBack(object context) { 
            Triplet triplet = (Triplet)context; 

            IComponent component = (IComponent)triplet.First; 
            String templateName = (String)triplet.Second;
            String[] keys = (String[])triplet.Third;

            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(component)[templateName]; 
            descriptor.SetValue(component, GetTemplateFromDesignModeState(keys));
 
            return true; 
        }
 
        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.OnConvertToCustomNavigationTemplate"]/*' />
        protected virtual void ConvertToCustomNavigationTemplate() {
            try {
                Debug.Assert(ActiveStep is TemplatedWizardStep); 
                ITemplate navigationTemplate = null;
                string description = SR.GetString(SR.Wizard_ConvertToCustomNavigationTemplate); 
 
                TemplatedWizardStep activeStep = ActiveStep as TemplatedWizardStep;
                if (activeStep != null) { 
                    // Check skin case first, if the skin has a custom navigation template, just use that.
                    TemplatedWizardStep viewActiveStep = ((Wizard)ViewControl).ActiveStep as TemplatedWizardStep;
                    if (viewActiveStep != null && viewActiveStep.CustomNavigationTemplate != null) {
                        navigationTemplate = viewActiveStep.CustomNavigationTemplate; 
                    }
                    else { 
                        // Convert the nav template they are using to a custom template 
                        WizardStepType stepType = _wizard.GetStepType(activeStep, ActiveStepIndex);
                        switch (stepType) { 
                            case WizardStepType.Start:
                                navigationTemplate = GetTemplateFromDesignModeState(_startButtonIDs);
                                break;
                            case WizardStepType.Step: 
                                navigationTemplate = GetTemplateFromDesignModeState(_stepButtonIDs);
                                break; 
                            case WizardStepType.Finish: 
                                navigationTemplate = GetTemplateFromDesignModeState(_finishButtonIDs);
                                break; 
                        }
                    }

                    InvokeTransactedChange(Component, new TransactedChangeCallback(ConvertToCustomNavigationTemplateCallBack), 
                        navigationTemplate, description);
                } 
            } 
            catch (Exception ex) {
                Debug.Fail(ex.ToString()); 
            }
        }

        internal bool ConvertToCustomNavigationTemplateCallBack(object context) { 
            ITemplate template = (ITemplate)context;
            TemplatedWizardStep activeStep = ActiveStep as TemplatedWizardStep; 
            Debug.Assert(activeStep != null); 

            activeStep.CustomNavigationTemplate = template; 

            return true;
        }
 
        private void ConvertToStartNavigationTemplate() {
            Debug.Assert(_wizard.StartNavigationTemplate == null); 
            ConvertToTemplate(SR.GetString(SR.Wizard_ConvertToStartNavigationTemplate), Component, 
                _startNavigationTemplateName, _startButtonIDs);
        } 

        private void ConvertToStepNavigationTemplate() {
            Debug.Assert(_wizard.StepNavigationTemplate == null);
            ConvertToTemplate(SR.GetString(SR.Wizard_ConvertToStepNavigationTemplate), Component, 
                _stepNavigationTemplateName, _stepButtonIDs);
        } 
 
        private void ConvertToFinishNavigationTemplate() {
            Debug.Assert(_wizard.FinishNavigationTemplate == null); 
            ConvertToTemplate(SR.GetString(SR.Wizard_ConvertToFinishNavigationTemplate), Component,
                _finishNavigationTemplateName, _finishButtonIDs);
        }
 
        private void ConvertToSideBarTemplate() {
            Debug.Assert(_wizard.SideBarTemplate == null); 
            ConvertToTemplate(SR.GetString(SR.Wizard_ConvertToSideBarTemplate), Component, 
                _sideBarTemplateName, new string[] {_dataListID});
        } 

        protected override void CreateChildControls() {
            base.CreateChildControls();
 
            Wizard wizard = (Wizard)ViewControl;
 
            // Set the first step as the active step to mimic runtime behavior. 
            if (wizard.ActiveStepIndex == -1 && wizard.WizardSteps.Count > 0) {
                wizard.ActiveStepIndex = 0; 
            }

            IControlDesignerAccessor accessor = (IControlDesignerAccessor)wizard;
            IDictionary dictionary = accessor.GetDesignModeState(); 

            // If we have a templated step content template coming from a stylesheet theme, we must turn off region editing 
            // for the template from the skin to show through. 
            TemplatedWizardStep tsw = wizard.ActiveStep as TemplatedWizardStep;
            if (tsw != null && tsw.ContentTemplate != null && ((TemplatedWizardStep)_wizard.WizardSteps[wizard.ActiveStepIndex]).ContentTemplate == null) { 
                return;
            }

            TableCell stepTableCell = dictionary[_stepTableCellID] as TableCell; 
            if (stepTableCell != null && wizard.ActiveStepIndex != -1) {
                stepTableCell.Attributes["_designerRegion"] = wizard.ActiveStepIndex.ToString(NumberFormatInfo.InvariantInfo); 
            } 
        }
 
        private void DataListItemDataBound(object sender, DataListItemEventArgs e) {
            DataListItem  dataListItem = e.Item;
            WebControl button = dataListItem.FindControl(_sideBarButtonID) as WebControl;
 
            if (button != null) {
                int index = dataListItem.ItemIndex + ((Wizard)ViewControl).WizardSteps.Count; 
                button.Attributes["_designerRegion"] = index.ToString(NumberFormatInfo.InvariantInfo); 
            }
        } 

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.GetDesignTimeHtml"]/*' />
        public override string GetDesignTimeHtml() {
            string designTimeHTML = null; 

            // Nothing to do if the Wizard is empty; 
            if (ActiveStepIndex == -1) { 
                return GetEmptyDesignTimeHtml();
            } 

            Wizard wizard = (Wizard)ViewControl;
            IControlDesignerAccessor viewControlAccessor = (IControlDesignerAccessor)wizard;
            IDictionary dictionary = viewControlAccessor.GetDesignModeState(); 

            DataList sideBarDataList = dictionary[_dataListID] as DataList; 
 
            if (sideBarDataList != null) {
                sideBarDataList.ItemDataBound += new DataListItemEventHandler(this.DataListItemDataBound); 

                ICompositeControlDesignerAccessor ccda = (ICompositeControlDesignerAccessor)wizard;
                ccda.RecreateChildControls();
            } 

            ArrayList titleList = new ArrayList(wizard.WizardSteps.Count); 
            foreach (WizardStepBase step in wizard.WizardSteps) { 
                titleList.Add(step.Title);
 
                if ((step.Title == null || step.Title.Length == 0) && (step.ID == null || step.ID.Length == 0)) {
                    step.Title = GetRegionName(step);
                }
            } 

            //  Make sure the viewcontrol is enabled in region editing mode, otherwise the region editing 
            //  will not function properly. 
            if (!InRegionEditingMode(wizard)) {
                wizard.Enabled = true; 
            }

            designTimeHTML = base.GetDesignTimeHtml();
 
            if ((designTimeHTML == null) || (designTimeHTML.Length == 0)) {
                designTimeHTML = GetEmptyDesignTimeHtml(); 
            } 

            return designTimeHTML; 
        }

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.GetDesignTimeHtml1"]/*' />
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) { 
            AddDesignerRegions(regions);
 
            IControlDesignerAccessor accessor = (IControlDesignerAccessor)_wizard; 
            IDictionary dictionary = null;
 
            try {
                dictionary = accessor.GetDesignModeState();
            }
            catch (Exception ex) { 
                return GetErrorDesignTimeHtml(ex);
            } 
 
            DataList sideBarDataList = dictionary[_dataListID] as DataList;
 
            if (sideBarDataList != null) {
                sideBarDataList.ItemDataBound += new DataListItemEventHandler(this.DataListItemDataBound);
            }
 
            Wizard wizard = (Wizard)ViewControl;
 
            IControlDesignerAccessor viewControlAccessor = (IControlDesignerAccessor)wizard; 
            IDictionary viewControlDictionary = viewControlAccessor.GetDesignModeState();
 
            if (viewControlDictionary != null) {
                viewControlDictionary["ShouldRenderWizardSteps"] = InRegionEditingMode(wizard);
            }
 
            return GetDesignTimeHtml();
        } 
 
        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.GetEditableDesignerRegionContent"]/*' />
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) { 
            if (region == null)
                throw new ArgumentNullException("region");

            IWizardStepEditableRegion wizardRegion = region as IWizardStepEditableRegion; 
            if (wizardRegion == null) {
                throw new ArgumentException(SR.GetString(SR.Wizard_InvalidRegion)); 
            } 

            return GetEditableDesignerRegionContent(wizardRegion); 
        }

        internal virtual string GetEditableDesignerRegionContent(IWizardStepEditableRegion region) {
            StringBuilder sb = new StringBuilder(); 
            ControlCollection controls = region.Step.Controls;
 
            IDesignerHost host = (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost)); 

            if (region.Step is TemplatedWizardStep) { 
                TemplatedWizardStep templatedStep = (TemplatedWizardStep)region.Step;
                return ControlPersister.PersistTemplate(templatedStep.ContentTemplate, host);
            }
 
            // Ignore white space only content
            if (controls.Count == 1 && controls[0] is LiteralControl) { 
                string literal = ((LiteralControl)controls[0]).Text; 
                if (literal == null || literal.Trim().Length == 0) {
                    return String.Empty; 
                }
            }

            foreach(Control control in controls) { 
                sb.Append(ControlPersister.PersistControl(control, host));
            } 
 
            return sb.ToString();
        } 

        internal string GetRegionName(WizardStepBase step) {
            if (step.Title != null && step.Title.Length > 0) {
                return step.Title; 
            }
 
            if (step.ID != null && step.ID.Length > 0) { 
                return step.ID;
            } 

            int index = step.Wizard.WizardSteps.IndexOf(step) + 1;
            return "[step ("+index+")]";
        } 

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.Initialize"]/*' /> 
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(Wizard));
 
            _wizard = (Wizard)component;
            base.Initialize(component);

            SetViewFlags(ViewFlags.TemplateEditing, true); 
        }
 
        private void MarkPropertyNonBrowsable(IDictionary properties, String propName) { 
            PropertyDescriptor property = (PropertyDescriptor) properties[propName];
            Debug.Assert(property != null, "Property is null: " + propName); 
            if (property != null) {
                properties[propName] = TypeDescriptor.CreateProperty(property.ComponentType, property, BrowsableAttribute.No);
            }
        } 

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.OnClick"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        protected override void OnClick(DesignerRegionMouseEventArgs e) { 
            base.OnClick(e);

            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null); 

            WizardSelectableRegion region = e.Region as WizardSelectableRegion; 
            if (region != null) { 
                PropertyDescriptor descriptor = TypeDescriptor.GetProperties(_wizard)[_activeStepIndexPropName];
                int index = _wizard.WizardSteps.IndexOf(region.Step); 
                Debug.Assert(index != -1);

                if (ActiveStepIndex != index) {
                    using (DesignerTransaction transaction = designerHost.CreateTransaction(_activeStepIndexTransactionDescription)) { 
                        descriptor.SetValue(Component, index);
                        transaction.Commit(); 
                    } 
                }
            } 
        }

        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties); 

            // Handle shadowed properties 
            // VSWhidbey 402538. Need to invalidate verb visibility when DisplaySideBar property changes. 
            PropertyDescriptor prop = (PropertyDescriptor)properties[_displaySideBarPropName];
            if (prop != null) { 
                properties[_displaySideBarPropName] =
                    TypeDescriptor.CreateProperty(GetType(), prop, null);
            }
 
            if (InTemplateMode) {
                MarkPropertyNonBrowsable(properties, _wizardStepsPropertyName); 
            } 

            if (_wizard.StartNavigationTemplate != null) { 
                foreach (String startNavigationTemplatePropName in _startNavigationTemplateProperties) {
                    MarkPropertyNonBrowsable(properties, startNavigationTemplatePropName);
                }
            } 

            if (_wizard.StepNavigationTemplate != null) { 
                foreach (String stepNavigationTemplatePropName in _stepNavigationTemplateProperties) { 
                    MarkPropertyNonBrowsable(properties, stepNavigationTemplatePropName);
                } 
            }

            if (_wizard.FinishNavigationTemplate != null) {
                foreach (String finishNavigationTemplatePropName in _finishNavigationTemplateProperties) { 
                    MarkPropertyNonBrowsable(properties, finishNavigationTemplatePropName);
                } 
            } 

            // Hide cancel button properties if every navigation template is specified. 
            if (_wizard.StartNavigationTemplate != null && _wizard.StepNavigationTemplate != null &&
                _wizard.FinishNavigationTemplate != null) {
                foreach (String generalNavigationButtonPropName in _generalNavigationButtonProperties) {
                    MarkPropertyNonBrowsable(properties, generalNavigationButtonPropName); 
                }
            } 
 
            if (_wizard.HeaderTemplate != null) {
                foreach (String headerPropName in _headerProperties) { 
                    MarkPropertyNonBrowsable(properties, headerPropName);
                }
            }
 
            if (_wizard.SideBarTemplate != null) {
                foreach (String sideBarPropName in _sideBarProperties) { 
                    MarkPropertyNonBrowsable(properties, sideBarPropName); 
                }
            } 
        }

        private void ResetInternalControls(IDictionary dictionary) {
            DataList sideBarDataList = (DataList)dictionary[_dataListID]; 
            if (sideBarDataList != null) {
                sideBarDataList.SelectedIndex = -1; 
            } 
        }
 
        private void ResetCustomNavigationTemplate() {
            WizardStepBase activeStep = ActiveStep;
            Debug.Assert(activeStep is TemplatedWizardStep && ((TemplatedWizardStep)activeStep).CustomNavigationTemplate != null);
 
            InvokeTransactedChange(Component, new TransactedChangeCallback(ResetCustomNavigationTemplateCallBack),
                null, SR.GetString(SR.Wizard_ResetCustomNavigationTemplate)); 
        } 

        private bool ResetCustomNavigationTemplateCallBack(object context) { 
            WizardStepBase activeStep = ActiveStep;
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(activeStep)[_customNavigationTemplateName];
            descriptor.ResetValue(activeStep);
 
            return true;
        } 
 
        private void ResetStartNavigationTemplate() {
            Debug.Assert(_wizard.StartNavigationTemplate != null); 
            ResetTemplate(SR.GetString(SR.Wizard_ResetStartNavigationTemplate), Component, _startNavigationTemplateName);
        }

        private void ResetStepNavigationTemplate() { 
            Debug.Assert(_wizard.StepNavigationTemplate != null);
            ResetTemplate(SR.GetString(SR.Wizard_ResetStepNavigationTemplate), Component, _stepNavigationTemplateName); 
        } 

        private void ResetFinishNavigationTemplate() { 
            Debug.Assert(_wizard.FinishNavigationTemplate != null);
            ResetTemplate(SR.GetString(SR.Wizard_ResetFinishNavigationTemplate), Component, _finishNavigationTemplateName);
        }
 
        private void ResetSideBarTemplate() {
            Debug.Assert(_wizard.SideBarTemplate != null); 
            ResetTemplate(SR.GetString(SR.Wizard_ResetSideBarTemplate), Component, _sideBarTemplateName); 
        }
 
        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.ResetTemplate"]/*' />
        protected void ResetTemplate(string description, IComponent component, string templateName) {
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null); 

            InvokeTransactedChange(Component, new TransactedChangeCallback(ResetTemplateCallBack), 
                new Pair(component, templateName), description); 

            UpdateDesignTimeHtml(); 
        }

        private bool ResetTemplateCallBack(object context) {
            Pair pair = (Pair)context; 

            IComponent component = (IComponent)pair.First; 
            String templateName = (String)pair.Second; 

            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(component)[templateName]; 
            descriptor.ResetValue(component);

            return true;
        } 

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.SetEditableDesignerRegionContent"]/*' /> 
        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) { 
            if (region == null) {
                throw new ArgumentNullException("region"); 
            }

            IWizardStepEditableRegion wizardRegion = region as IWizardStepEditableRegion;
            if (wizardRegion == null) { 
                throw new ArgumentException(SR.GetString(SR.Wizard_InvalidRegion));
            } 
 
            IDesignerHost host = (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost));
            Debug.Assert(host != null, "IDesignerHost is null."); 

            if (wizardRegion.Step is TemplatedWizardStep) {
                IComponent component = (IComponent)wizardRegion.Step;
                ITemplate template = ControlParser.ParseTemplate(host, content); 
                PropertyDescriptor descriptor = TypeDescriptor.GetProperties(component)[_contentTemplateName];
                using (DesignerTransaction transaction = host.CreateTransaction("SetEditableDesignerRegionContent")) { 
                    descriptor.SetValue(component, template); 
                    transaction.Commit();
                } 

                ViewControlCreated = false;
            }
            else { 
                SetWizardStepContent(wizardRegion.Step, content, host);
            } 
        } 

        private void SetWizardStepContent(WizardStepBase step, string content, IDesignerHost host) { 
            Control[] controls = null;
            if (content != null && content.Length > 0) {
                controls = ControlParser.ParseControls(host, content);
            } 

            step.Controls.Clear(); 
            if (controls == null) 
                return;
 
            foreach(Control control in controls) {
                step.Controls.Add(control);
            }
        } 

        private void StartWizardStepCollectionEditor() { 
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null);
 
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Component)[_wizardStepsPropertyName];

            using (DesignerTransaction transaction = designerHost.CreateTransaction(SR.GetString(SR.Wizard_StartWizardStepCollectionEditor))) {
                UITypeEditor editor = (UITypeEditor)descriptor.GetEditor(typeof(UITypeEditor)); 
                object newValue = editor.EditValue(new TypeDescriptorContext(designerHost, descriptor, Component),
                                                   new WindowsFormsEditorServiceHelper(this), descriptor.GetValue(Component)); 
                if (newValue != null) { 
                    transaction.Commit();
                } 
            }

            // Recreate child controls only if activestepindex is valid.
            if (_wizard.ActiveStepIndex >= -1 && _wizard.ActiveStepIndex < _wizard.WizardSteps.Count) { 

                // Ignore any exception that might happen during child control creation, 
                // these errors will eventually show up during GetDesignTimeHtml() 
                try {
                    ViewControlCreated = false; 
                    CreateChildControls();
                }
                catch { }
            } 
        }
 
        private class WizardDesignerActionList : DesignerActionList { 
            private WizardDesigner _designer;
 
            public WizardDesignerActionList(WizardDesigner designer) : base(designer.Component) {
                _designer = designer;
            }
 
            public override bool AutoShow {
                get { 
                    return true; 
                }
                set { 
                }
            }

            [TypeConverter(typeof(WizardStepTypeConverter))] 
            public int View {
                get { 
                    return _designer.ActiveStepIndex; 
                }
                set { 
                    // Do nothing if the value is unchanged.
                    if (value == _designer.ActiveStepIndex) {
                        return;
                    } 

                    IDesignerHost designerHost = (IDesignerHost)_designer.GetService(typeof(IDesignerHost)); 
                    Debug.Assert(designerHost != null); 

                    PropertyDescriptor descriptor = TypeDescriptor.GetProperties(_designer.Component)[WizardDesigner._activeStepIndexPropName]; 

                    using (DesignerTransaction transaction = designerHost.CreateTransaction(SR.GetString(SR.Wizard_OnViewChanged))) {
                        descriptor.SetValue(_designer.Component, value);
                        transaction.Commit(); 
                    }
 
                    _designer.UpdateDesignTimeHtml(); 
                    TypeDescriptor.Refresh(_designer.Component);
                } 
            }

            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection(); 

                if (!_designer.InTemplateMode) { 
                    if (_designer._wizard.WizardSteps.Count > 0) { 
                        items.Add(new DesignerActionPropertyItem("View",
                                                                  SR.GetString(SR.Wizard_StepsView), 
                                                                  String.Empty,
                                                                  SR.GetString(SR.Wizard_StepsViewDescription)));
                    }
 
                    items.Add(new DesignerActionMethodItem(this, "StartWizardStepCollectionEditor",
                        SR.GetString(SR.Wizard_StartWizardStepCollectionEditor), String.Empty, 
                        SR.GetString(SR.Wizard_StartWizardStepCollectionEditorDescription), true)); 
                    Wizard wizard = _designer._wizard;
 
                    int index = _designer.ActiveStepIndex;

                    if (index >= 0 && index < wizard.WizardSteps.Count) {
                        if (wizard.StartNavigationTemplate != null) { 
                            items.Add(new DesignerActionMethodItem(this, "ResetStartNavigationTemplate",
                                SR.GetString(SR.Wizard_ResetStartNavigationTemplate), String.Empty, 
                                SR.GetString(SR.Wizard_ResetDescription, "StartNavigation"), true)); 
                        } else {
                            items.Add(new DesignerActionMethodItem(this, "ConvertToStartNavigationTemplate", 
                                SR.GetString(SR.Wizard_ConvertToStartNavigationTemplate), String.Empty,
                                SR.GetString(SR.Wizard_ConvertToTemplateDescription, "StartNavigation"), true));
                        }
 
                        if (wizard.StepNavigationTemplate != null) {
                            items.Add(new DesignerActionMethodItem(this, "ResetStepNavigationTemplate", 
                                SR.GetString(SR.Wizard_ResetStepNavigationTemplate), String.Empty, 
                                SR.GetString(SR.Wizard_ResetDescription, "StepNavigation"), true));
                        } else { 
                            items.Add(new DesignerActionMethodItem(this, "ConvertToStepNavigationTemplate",
                                SR.GetString(SR.Wizard_ConvertToStepNavigationTemplate), String.Empty,
                                SR.GetString(SR.Wizard_ConvertToTemplateDescription, "StepNavigation"), true));
                        } 

                        if (wizard.FinishNavigationTemplate != null) { 
                            items.Add(new DesignerActionMethodItem(this, "ResetFinishNavigationTemplate", 
                                SR.GetString(SR.Wizard_ResetFinishNavigationTemplate), String.Empty,
                                SR.GetString(SR.Wizard_ResetDescription, "FinishNavigation"), true)); 
                        } else {
                            items.Add(new DesignerActionMethodItem(this, "ConvertToFinishNavigationTemplate",
                                SR.GetString(SR.Wizard_ConvertToFinishNavigationTemplate), String.Empty,
                                SR.GetString(SR.Wizard_ConvertToTemplateDescription, "FinishNavigation"), true)); 
                        }
 
                        if (wizard.DisplaySideBar) { 
                            if (wizard.SideBarTemplate != null) {
                                items.Add(new DesignerActionMethodItem(this, "ResetSideBarTemplate", 
                                    SR.GetString(SR.Wizard_ResetSideBarTemplate), String.Empty,
                                    SR.GetString(SR.Wizard_ResetDescription, "SideBar"), true));
                            } else {
                                items.Add(new DesignerActionMethodItem(this, "ConvertToSideBarTemplate", 
                                    SR.GetString(SR.Wizard_ConvertToSideBarTemplate), String.Empty,
                                    SR.GetString(SR.Wizard_ConvertToTemplateDescription, "SideBar"), true)); 
                            } 
                        }
 
                        TemplatedWizardStep templatedActiveStep = _designer.ActiveStep as TemplatedWizardStep;
                        // Do not display the "ConvertToCustomNavigationTemplate" if it's a complete step
                        if (templatedActiveStep != null &&
                            templatedActiveStep.StepType != WizardStepType.Complete) { 
                            if (templatedActiveStep.CustomNavigationTemplate != null) {
                                items.Add(new DesignerActionMethodItem(this, "ResetCustomNavigationTemplate", 
                                    SR.GetString(SR.Wizard_ResetCustomNavigationTemplate), String.Empty, 
                                    SR.GetString(SR.Wizard_ResetDescription, "CustomNavigation"), true));
                            } else { 
                                items.Add(new DesignerActionMethodItem(this, "ConvertToCustomNavigationTemplate",
                                    SR.GetString(SR.Wizard_ConvertToCustomNavigationTemplate), String.Empty,
                                    SR.GetString(SR.Wizard_ConvertToTemplateDescription, "CustomNavigation"), true));
                            } 
                        }
                    } 
                } 

                return items; 
            }

            public void ConvertToCustomNavigationTemplate() {
                _designer.ConvertToCustomNavigationTemplate(); 
            }
 
            public void ConvertToFinishNavigationTemplate() { 
                _designer.ConvertToFinishNavigationTemplate();
            } 

            public void ConvertToSideBarTemplate() {
                _designer.ConvertToSideBarTemplate();
            } 

            public void ConvertToStartNavigationTemplate() { 
                _designer.ConvertToStartNavigationTemplate(); 
            }
 
            public void ConvertToStepNavigationTemplate() {
                _designer.ConvertToStepNavigationTemplate();
            }
 
            public void ResetCustomNavigationTemplate() {
                _designer.ResetCustomNavigationTemplate(); 
            } 

            public void ResetFinishNavigationTemplate() { 
                _designer.ResetFinishNavigationTemplate();
            }

            public void ResetSideBarTemplate() { 
                _designer.ResetSideBarTemplate();
            } 
 
            public void ResetStartNavigationTemplate() {
                _designer.ResetStartNavigationTemplate(); 
            }

            public void ResetStepNavigationTemplate() {
                _designer.ResetStepNavigationTemplate(); 
            }
 
            public void StartWizardStepCollectionEditor() { 
                _designer.StartWizardStepCollectionEditor();
            } 

            private class WizardStepTypeConverter : TypeConverter {
                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
                    int[] stepValues = null; 
                    if (context != null) {
                        WizardDesignerActionList list = (WizardDesignerActionList)context.Instance; 
                        WizardDesigner designer = list._designer; 
                        WizardStepCollection steps = designer._wizard.WizardSteps;
                        stepValues = new int[steps.Count]; 
                        for (int i = 0; i < steps.Count; i++) {
                            stepValues[i] = i;
                        }
                    } 
                    return new StandardValuesCollection(stepValues);
                } 
 
                public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
                    return true; 
                }

                public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
                    return true; 
                }
 
                public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) { 
                    if (destinationType == typeof(string)) {
                        if (value is string) return value; 
                        WizardDesignerActionList list = (WizardDesignerActionList)context.Instance;
                        WizardDesigner designer = list._designer;
                        WizardStepCollection steps = designer._wizard.WizardSteps;
                        if (value is int) { 
                            int intValue = (int)value;
                            if (intValue == -1 && steps.Count > 0) { 
                                intValue = 0; 
                            }
 
                            if (intValue >= steps.Count) {
                                return null;
                            }
 
                            return designer.GetRegionName(steps[intValue]);
                        } 
                    } 
                    return base.ConvertTo(context, culture, value, destinationType);
                } 

                public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
                    if (value is string) {
                        WizardDesignerActionList list = (WizardDesignerActionList)context.Instance; 
                        WizardDesigner designer = list._designer;
                        WizardStepCollection steps = designer._wizard.WizardSteps; 
                        for (int i = 0; i < steps.Count; i++) { 
                            if (String.Compare(designer.GetRegionName(steps[i]), (string)value, StringComparison.Ordinal) == 0) {
                                return i; 
                            }
                        }
                    }
                    return base.ConvertFrom (context, culture, value); 
                }
 
                public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) { 
                    if (sourceType == typeof(string)) {
                        return true; 
                    }
                    return base.CanConvertFrom(context, sourceType);
                }
 
                public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
                    if (destinationType == typeof(string)) { 
                        return true; 
                    }
                    return base.CanConvertTo(context, destinationType); 
                }
            }
        }
    } 

    internal interface IWizardStepEditableRegion { 
        WizardStepBase Step { get; } 
    }
 
    /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardStepEditableRegion"]/*' />
    public class WizardStepEditableRegion : EditableDesignerRegion, IWizardStepEditableRegion {
        private WizardStepBase _wizardStep;
 
        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardStepEditableRegion.WizardStepEditableRegion"]/*' />
        public WizardStepEditableRegion(WizardDesigner designer, WizardStepBase wizardStep) : 
            base(designer, designer.GetRegionName(wizardStep), false) { 
            _wizardStep = wizardStep;
            EnsureSize = true; 
        }

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardStepEditableRegion.Step"]/*' />
        public WizardStepBase Step { 
            get { return _wizardStep; }
        } 
    } 

    /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardStepTemplatedEditableRegion"]/*' /> 
    public class WizardStepTemplatedEditableRegion : TemplatedEditableDesignerRegion, IWizardStepEditableRegion {
        private WizardStepBase _wizardStep;

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardStepTemplatedEditableRegion.WizardStepEditableRegion"]/*' /> 
        public WizardStepTemplatedEditableRegion(TemplateDefinition templateDefinition, WizardStepBase wizardStep) :
            base(templateDefinition) { 
            _wizardStep = wizardStep; 
            EnsureSize = true;
        } 

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardStepTemplatedEditableRegion.Step"]/*' />
        public WizardStepBase Step {
            get { return _wizardStep; } 
        }
    } 
 
    internal class WizardStepBaseTemplateDefinition : TemplateDefinition {
        private WizardStepBase _step; 

        public WizardStepBaseTemplateDefinition(WizardDesigner designer, WizardStepBase step, string name, Style style) : base(designer, name, step, name, style) {
            _step = step;
        } 

        public override string Content { 
            get { 
                StringBuilder sb = new StringBuilder();
                foreach(Control control in _step.Controls) { 
                    sb.Append(ControlPersister.PersistControl(control));
                }

                return sb.ToString(); 
            }
            set { 
                _step.Controls.Clear(); 
                if (value == null)
                    return; 

                IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
                Debug.Assert(designerHost != null);
 
                Control[] controls = null;
                controls = ControlParser.ParseControls(designerHost, value); 
 
                foreach(Control control in controls) {
                    _step.Controls.Add(control); 
                }
            }
        }
 
    }
 
    internal class WizardSelectableRegion : DesignerRegion { 
        private WizardStepBase _wizardStep;
 
        internal WizardSelectableRegion(WizardDesigner designer, string name, WizardStepBase wizardStep) : base(designer, name, true) {
            _wizardStep = wizardStep;
        }
 
        internal WizardStepBase Step {
            get { return _wizardStep; } 
        } 
    }
 
    internal sealed class WizardAutoFormat : DesignerAutoFormat {

        private string FontName;
        private FontUnit FontSize; 
        private Color BackColor;
        private Color BorderColor; 
        private Unit BorderWidth; 
        private BorderStyle BorderStyle;
        private Unit NavigationButtonStyleBorderWidth; 
        private string NavigationButtonStyleFontName;
        private FontUnit NavigationButtonStyleFontSize;
        private BorderStyle NavigationButtonStyleBorderStyle;
        private Color NavigationButtonStyleBorderColor; 
        private Color NavigationButtonStyleForeColor;
        private Color NavigationButtonStyleBackColor; 
        private Unit StepStyleBorderWidth; 
        private BorderStyle StepStyleBorderStyle;
        private Color StepStyleBorderColor; 
        private Color StepStyleForeColor;
        private Color StepStyleBackColor;
        private FontUnit StepStyleFontSize;
        private bool SideBarButtonStyleFontUnderline; 
        private string SideBarButtonStyleFontName;
        private Color SideBarButtonStyleForeColor; 
        private Unit SideBarButtonStyleBorderWidth; 
        private Color SideBarButtonStyleBackColor;
        private Color HeaderStyleForeColor; 
        private Color HeaderStyleBorderColor;
        private Color HeaderStyleBackColor;
        private FontUnit HeaderStyleFontSize;
        private bool HeaderStyleFontBold; 
        private Unit HeaderStyleBorderWidth;
        private HorizontalAlign HeaderStyleHorizontalAlign; 
        private BorderStyle HeaderStyleBorderStyle; 
        private Color SideBarStyleBackColor;
        private VerticalAlign SideBarStyleVerticalAlign; 
        private FontUnit SideBarStyleFontSize;
        private bool SideBarStyleFontUnderline;
        private bool SideBarStyleFontStrikeout;
        private Unit SideBarStyleBorderWidth; 

        public WizardAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) { 
            Load(schemeData); 

            Style.Width = 350; 
            Style.Height = 200;
        }

        public override void Apply(Control control) { 
            Debug.Assert(control is Wizard, "WizardAutoFormat:ApplyScheme- control is not Wizard");
            if (control is Wizard) { 
                Apply(control as Wizard); 
            }
        } 

        private void Apply(Wizard wizard) {
            wizard.Font.Name = FontName;
            wizard.Font.Size = FontSize; 
            wizard.BackColor = BackColor;
            wizard.BorderColor = BorderColor; 
            wizard.BorderWidth = BorderWidth; 
            wizard.BorderStyle = BorderStyle;
            wizard.Font.ClearDefaults(); 

            wizard.NavigationButtonStyle.BorderWidth = NavigationButtonStyleBorderWidth;
            wizard.NavigationButtonStyle.Font.Name = NavigationButtonStyleFontName;
            wizard.NavigationButtonStyle.Font.Size = NavigationButtonStyleFontSize; 
            wizard.NavigationButtonStyle.BorderStyle = NavigationButtonStyleBorderStyle;
            wizard.NavigationButtonStyle.BorderColor = NavigationButtonStyleBorderColor; 
            wizard.NavigationButtonStyle.ForeColor = NavigationButtonStyleForeColor; 
            wizard.NavigationButtonStyle.BackColor = NavigationButtonStyleBackColor;
            wizard.NavigationButtonStyle.Font.ClearDefaults(); 

            wizard.StepStyle.BorderWidth = StepStyleBorderWidth;
            wizard.StepStyle.BorderStyle = StepStyleBorderStyle;
            wizard.StepStyle.BorderColor = StepStyleBorderColor; 
            wizard.StepStyle.ForeColor = StepStyleForeColor;
            wizard.StepStyle.BackColor = StepStyleBackColor; 
            wizard.StepStyle.Font.Size = StepStyleFontSize; 
            wizard.StepStyle.Font.ClearDefaults();
 
            wizard.SideBarButtonStyle.Font.Underline = SideBarButtonStyleFontUnderline;
            wizard.SideBarButtonStyle.Font.Name = SideBarButtonStyleFontName;
            wizard.SideBarButtonStyle.ForeColor = SideBarButtonStyleForeColor;
            wizard.SideBarButtonStyle.BorderWidth = SideBarButtonStyleBorderWidth; 
            wizard.SideBarButtonStyle.BackColor = SideBarButtonStyleBackColor;
            wizard.SideBarButtonStyle.Font.ClearDefaults(); 
 
            wizard.HeaderStyle.ForeColor = HeaderStyleForeColor;
            wizard.HeaderStyle.BorderColor = HeaderStyleBorderColor; 
            wizard.HeaderStyle.BackColor = HeaderStyleBackColor;
            wizard.HeaderStyle.Font.Size = HeaderStyleFontSize;
            wizard.HeaderStyle.Font.Bold = HeaderStyleFontBold;
            wizard.HeaderStyle.BorderWidth = HeaderStyleBorderWidth; 
            wizard.HeaderStyle.HorizontalAlign = HeaderStyleHorizontalAlign;
            wizard.HeaderStyle.BorderStyle = HeaderStyleBorderStyle; 
            wizard.HeaderStyle.Font.ClearDefaults(); 

            wizard.SideBarStyle.BackColor = SideBarStyleBackColor; 
            wizard.SideBarStyle.VerticalAlign = SideBarStyleVerticalAlign;
            wizard.SideBarStyle.Font.Size = SideBarStyleFontSize;
            wizard.SideBarStyle.Font.Underline = SideBarStyleFontUnderline;
            wizard.SideBarStyle.Font.Strikeout = SideBarStyleFontStrikeout; 
            wizard.SideBarStyle.BorderWidth = SideBarStyleBorderWidth;
            wizard.SideBarStyle.Font.ClearDefaults(); 
        } 

        private bool GetBooleanProperty(string propertyTag, DataRow schemeData) { 
            object data = schemeData[propertyTag];
            if ((data != null) && !data.Equals(DBNull.Value))
                return bool.Parse(data.ToString());
            else 
                return false;
        } 
 
        private int GetIntProperty(string propertyTag, DataRow schemeData) {
            object data = schemeData[propertyTag]; 
            if ((data != null) && !data.Equals(DBNull.Value))
                return Int32.Parse(data.ToString(), CultureInfo.InvariantCulture);
            else
                return 0; 
        }
 
        private string GetStringProperty(string propertyTag, DataRow schemeData) { 
            object data = schemeData[propertyTag];
            if ((data != null) && !data.Equals(DBNull.Value)) 
                return data.ToString();
            else
                return String.Empty;
        } 

        private void Load(DataRow schemeData) { 
            if (schemeData == null) { 
                Debug.Write("CalendarAutoFormatUtil:LoadScheme- scheme not found");
                return; 
            }

            FontName = GetStringProperty("FontName", schemeData);
            FontSize = new FontUnit(GetStringProperty("FontSize", schemeData), CultureInfo.InvariantCulture); 
            BackColor = ColorTranslator.FromHtml(GetStringProperty("BackColor", schemeData));
            BorderColor = ColorTranslator.FromHtml(GetStringProperty("BorderColor", schemeData)); 
            BorderWidth = new Unit(GetStringProperty("BorderWidth", schemeData), CultureInfo.InvariantCulture); 
            SideBarStyleBackColor = ColorTranslator.FromHtml(GetStringProperty("SideBarStyleBackColor", schemeData));
            SideBarStyleVerticalAlign = (VerticalAlign)GetIntProperty("SideBarStyleVerticalAlign", schemeData); 
            BorderStyle = (BorderStyle)GetIntProperty("BorderStyle", schemeData);
            NavigationButtonStyleBorderWidth = new Unit(GetStringProperty("NavigationButtonStyleBorderWidth", schemeData), CultureInfo.InvariantCulture);
            NavigationButtonStyleFontName = GetStringProperty("NavigationButtonStyleFontName", schemeData);
            NavigationButtonStyleFontSize = new FontUnit(GetStringProperty("NavigationButtonStyleFontSize", schemeData), CultureInfo.InvariantCulture); 
            NavigationButtonStyleBorderStyle = (BorderStyle)GetIntProperty("NavigationButtonStyleBorderStyle", schemeData);
            NavigationButtonStyleBorderColor = ColorTranslator.FromHtml(GetStringProperty("NavigationButtonStyleBorderColor", schemeData)); 
            NavigationButtonStyleForeColor = ColorTranslator.FromHtml(GetStringProperty("NavigationButtonStyleForeColor", schemeData)); 
            NavigationButtonStyleBackColor = ColorTranslator.FromHtml(GetStringProperty("NavigationButtonStyleBackColor", schemeData));
            StepStyleBorderWidth = new Unit(GetStringProperty("StepStyleBorderWidth", schemeData), CultureInfo.InvariantCulture); 
            StepStyleBorderStyle = (BorderStyle)GetIntProperty("StepStyleBorderStyle", schemeData);
            StepStyleBorderColor = ColorTranslator.FromHtml(GetStringProperty("StepStyleBorderColor", schemeData));
            StepStyleForeColor = ColorTranslator.FromHtml(GetStringProperty("StepStyleForeColor", schemeData));
            StepStyleBackColor = ColorTranslator.FromHtml(GetStringProperty("StepStyleBackColor", schemeData)); 
            StepStyleFontSize = new FontUnit(GetStringProperty("StepStyleFontSize", schemeData), CultureInfo.InvariantCulture);
            SideBarButtonStyleFontUnderline = GetBooleanProperty("SideBarButtonStyleFontUnderline", schemeData); 
            SideBarButtonStyleFontName = GetStringProperty("SideBarButtonStyleFontName", schemeData); 
            SideBarButtonStyleForeColor = ColorTranslator.FromHtml(GetStringProperty("SideBarButtonStyleForeColor", schemeData));
            SideBarButtonStyleBorderWidth = new Unit(GetStringProperty("SideBarButtonStyleBorderWidth", schemeData), CultureInfo.InvariantCulture); 
            SideBarButtonStyleBackColor = ColorTranslator.FromHtml(GetStringProperty("SideBarButtonStyleBackColor", schemeData));
            HeaderStyleForeColor = ColorTranslator.FromHtml(GetStringProperty("HeaderStyleForeColor", schemeData));
            HeaderStyleBorderColor = ColorTranslator.FromHtml(GetStringProperty("HeaderStyleBorderColor", schemeData));
            HeaderStyleBackColor = ColorTranslator.FromHtml(GetStringProperty("HeaderStyleBackColor", schemeData)); 
            HeaderStyleFontSize = new FontUnit(GetStringProperty("HeaderStyleFontSize", schemeData), CultureInfo.InvariantCulture);
            HeaderStyleFontBold = GetBooleanProperty("HeaderStyleFontBold", schemeData); 
            HeaderStyleBorderWidth = new Unit(GetStringProperty("HeaderStyleBorderWidth", schemeData), CultureInfo.InvariantCulture); 
            HeaderStyleHorizontalAlign = (HorizontalAlign)GetIntProperty("HeaderStyleHorizontalAlign", schemeData);
            HeaderStyleBorderStyle = (BorderStyle)GetIntProperty("HeaderStyleBorderStyle", schemeData); 
            SideBarStyleBackColor = ColorTranslator.FromHtml(GetStringProperty("SideBarStyleBackColor", schemeData));
            SideBarStyleVerticalAlign = (VerticalAlign)GetIntProperty("SideBarStyleVerticalAlign", schemeData);
            SideBarStyleFontSize = new FontUnit(GetStringProperty("SideBarStyleFontSize", schemeData), CultureInfo.InvariantCulture);
            SideBarStyleFontUnderline = GetBooleanProperty("SideBarStyleFontUnderline", schemeData); 
            SideBarStyleFontStrikeout = GetBooleanProperty("SideBarStyleFontStrikeout", schemeData);
            SideBarStyleBorderWidth = new Unit(GetStringProperty("SideBarStyleBorderWidth", schemeData), CultureInfo.InvariantCulture); 
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
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Drawing.Design;
    using System.Globalization; 
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Web; 
    using System.Web.UI.Design;
    using System.Web.UI.Design.Util; 
    using System.Web.UI.WebControls; 
    using System.Windows.Forms.Design;
 
    /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner"]/*' />
    /// <devdoc>
    /// <para>
    /// Provides design-time support for the <see cref='System.Web.UI.WebControls.Wizard'/> web control. 
    /// </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, 
        Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    public class WizardDesigner : CompositeControlDesigner { 

        private Wizard _wizard;
        private DesignerAutoFormatCollection _autoFormats;
 
        private bool _supportsDesignerRegion;
        private bool _supportsDesignerRegionQueried; 
 
        private const string _headerTemplateName = "HeaderTemplate";
        internal const string _customNavigationTemplateName = "CustomNavigationTemplate"; 
        private const string _startNavigationTemplateName = "StartNavigationTemplate";
        private const string _stepNavigationTemplateName = "StepNavigationTemplate";
        private const string _finishNavigationTemplateName = "FinishNavigationTemplate";
        private const string _sideBarTemplateName = "SideBarTemplate"; 
        private const string _activeStepIndexPropName = "ActiveStepIndex";
        private const string _activeStepIndexTransactionDescription = "Update ActiveStepIndex"; 
        private const string _startNextButtonID = "StartNextButton"; 
        private const string _cancelButtonID = "CancelButton";
        private const string _stepTableCellID = "StepTableCell"; 
        private const string _displaySideBarPropName = "DisplaySideBar";

        private const string _stepPreviousButtonID = "StepPreviousButton";
        private const string _stepNextButtonID = "StepNextButton"; 
        private const string _finishButtonID = "FinishButton";
        private const string _finishPreviousButtonID = "FinishPreviousButton"; 
        private const string _dataListID = "SideBarList"; 
        private const string _sideBarButtonID = "SideBarButton";
        internal const string _customNavigationControls = "CustomNavigationControls"; 
        private const string _wizardStepsPropertyName = "WizardSteps";

        internal const string _contentTemplateName = "ContentTemplate";
        private const string _navigationTemplateName = "CustomNavigationTemplate"; 
        private static string[] _stepTemplateNames = new string[] { _contentTemplateName, _navigationTemplateName };
 
        internal const int _navigationStyleLength = 6; 

        private static string[] _controlTemplateNames = new string[] { 
            _headerTemplateName,
            _sideBarTemplateName,
            _startNavigationTemplateName,
            _stepNavigationTemplateName, 
            _finishNavigationTemplateName,
        }; 
 
        private static readonly string[] _startNavigationTemplateProperties = new string[] {
            "StartNextButtonText", "StartNextButtonType", "StartNextButtonImageUrl", 
            "StartNextButtonStyle",
        };

        private static readonly string[] _stepNavigationTemplateProperties = new string[] { 
            "StepNextButtonText", "StepNextButtonType", "StepNextButtonImageUrl",
            "StepPreviousButtonText", "StepPreviousButtonType", "StepPreviousButtonImageUrl", 
            "StepPreviousButtonStyle", "StepNextButtonStyle", 
        };
 
        private static readonly string[] _finishNavigationTemplateProperties = new string[] {
            "FinishCompleteButtonText", "FinishCompleteButtonType", "FinishCompleteButtonImageUrl",
            "FinishPreviousButtonText", "FinishPreviousButtonType", "FinishPreviousButtonImageUrl",
            "FinishCompleteButtonStyle", "FinishPreviousButtonStyle", 
        };
 
        private static readonly string[] _generalNavigationButtonProperties = new string[] { 
            "CancelButtonImageUrl", "CancelButtonText", "CancelButtonType", "DisplayCancelButton",
            "CancelButtonStyle", "NavigationButtonStyle", 
        };

        private static readonly string[] _headerProperties = new string[] {
            "HeaderText", 
        };
 
        private static readonly string[] _sideBarProperties = new string[] { 
            "SideBarButtonStyle",
        }; 

        private static string[] _startButtonIDs = new string[] {_startNextButtonID, _cancelButtonID};
        private static string[] _stepButtonIDs = new string[] {_stepPreviousButtonID, _stepNextButtonID, _cancelButtonID};
        private static string[] _finishButtonIDs = new string[] {_finishPreviousButtonID, _finishButtonID, _cancelButtonID}; 

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.ActionLists"]/*' /> 
        public override DesignerActionListCollection ActionLists { 
            get {
                DesignerActionListCollection actionLists = new DesignerActionListCollection(); 
                actionLists.AddRange(base.ActionLists);
                actionLists.Add(new WizardDesignerActionList(this));

                return actionLists; 
            }
        } 
 
        internal WizardStepBase ActiveStep {
            get { 
                if (ActiveStepIndex != -1) {
                    return _wizard.WizardSteps[ActiveStepIndex];
                }
 
                return null;
            } 
        } 

        internal int ActiveStepIndex { 
            get {
                int index = _wizard.ActiveStepIndex;
                if (index == -1 && _wizard.WizardSteps.Count > 0) {
                    return 0; 
                }
 
                return index; 
            }
        } 

        public override DesignerAutoFormatCollection AutoFormats {
            get {
                if (_autoFormats == null) { 
                    _autoFormats = CreateAutoFormats(AutoFormatSchemes.WIZARD_SCHEMES,
                        delegate(DataRow schemeData) { return new WizardAutoFormat(schemeData); }); 
                } 
                return _autoFormats;
            } 
        }

        protected bool DisplaySideBar {
            get { 
                return ((Wizard)Component).DisplaySideBar;
            } 
            set { 
                // VSWhidbey 402538. Need to invalidate verb visibility when DisplaySideBar property changes.
                TypeDescriptor.Refresh(Component); 

                ((Wizard)Component).DisplaySideBar = value;

                TypeDescriptor.Refresh(Component); 
            }
        } 
 
        internal bool SupportsDesignerRegions {
            get { 
                if (_supportsDesignerRegionQueried) {
                    return _supportsDesignerRegion;
                }
 
                if (View != null) {
                    _supportsDesignerRegion = View.SupportsRegions; 
                } 

                _supportsDesignerRegionQueried = true; 

                return _supportsDesignerRegion;
            }
        } 

        internal virtual bool InRegionEditingMode(Wizard viewControl) { 
            if (!SupportsDesignerRegions) { 
                return true;
            } 

            // Return true if the ContentTemplate is defined through a skin file.
            TemplatedWizardStep activeStepFromWizard = ActiveStep as TemplatedWizardStep;
            if (activeStepFromWizard != null && activeStepFromWizard.ContentTemplate == null) { 
                TemplatedWizardStep mergedStep = viewControl.WizardSteps[ActiveStepIndex] as TemplatedWizardStep;
                if (mergedStep != null && mergedStep.ContentTemplate != null) { 
                    return true; 
                }
            } 

            return false;
        }
 
        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.TemplateGroups"]/*' />
        public override TemplateGroupCollection TemplateGroups { 
            get { 
                TemplateGroupCollection groups = base.TemplateGroups;
                for (int i = 0; i < _controlTemplateNames.Length; i++) { 
                    string templateName = _controlTemplateNames[i];
                    TemplateGroup templateGroup = new TemplateGroup(templateName);
                    templateGroup.AddTemplateDefinition(new TemplateDefinition(this, templateName, _wizard, templateName, TemplateStyleArray[i]));
 
                    groups.Add(templateGroup);
                } 
 
                foreach(WizardStepBase step in _wizard.WizardSteps) {
                    string templateName = GetRegionName(step); 
                    TemplateGroup templateGroup = new TemplateGroup(templateName);
                    if (step is TemplatedWizardStep) {
                        for (int i = 0; i < _stepTemplateNames.Length; i++) {
                            templateGroup.AddTemplateDefinition(new TemplateDefinition(this, _stepTemplateNames[i], step, _stepTemplateNames[i], StepTemplateStyleArray[i])); 
                        }
                    } 
                    else if (!SupportsDesignerRegions) { 
                        templateGroup.AddTemplateDefinition(new WizardStepBaseTemplateDefinition(this, step, templateName, StepTemplateStyleArray[0]));
                    } 

                    if (!templateGroup.IsEmpty) {
                        groups.Add(templateGroup);
                    } 
                }
 
                return groups; 
            }
        } 

        internal Style[] TemplateStyleArray {
            get {
                Style headerStyle = new Style(); 
                Wizard control = ((Wizard)ViewControl);
                headerStyle.CopyFrom(control.ControlStyle); 
                headerStyle.CopyFrom(control.HeaderStyle); 

                Style sideBarStyle = new Style(); 
                sideBarStyle.CopyFrom(control.ControlStyle);
                sideBarStyle.CopyFrom(control.SideBarStyle);

                Style navigationStyle = new Style(); 
                navigationStyle.CopyFrom(control.ControlStyle);
                navigationStyle.CopyFrom(control.NavigationStyle); 
 
                Style[] styleArray = new Style[] {
                    headerStyle, 
                    sideBarStyle,
                    navigationStyle,
                    navigationStyle,
                    navigationStyle, 
                    navigationStyle
                }; 
 
                Debug.Assert(styleArray.Length == _navigationStyleLength);
 
                return styleArray;
            }
        }
 
        private Style[] StepTemplateStyleArray {
            get { 
                Style stepStyle = new Style(); 
                Wizard control = ((Wizard)ViewControl);
                stepStyle.CopyFrom(control.ControlStyle); 
                stepStyle.CopyFrom(control.StepStyle);

                Style navigationStyle = new Style();
                navigationStyle.CopyFrom(control.ControlStyle); 
                navigationStyle.CopyFrom(control.NavigationStyle);
 
                return new Style[] { 
                        stepStyle,
                        navigationStyle }; 
            }
        }

        protected override bool UsePreviewControl { 
            get {
                return true; 
            } 
        }
 
        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.AddDesignerRegions"]/*' />
        protected virtual void AddDesignerRegions(DesignerRegionCollection regions) {
            if (!SupportsDesignerRegions) {
                return; 
            }
 
            foreach (WizardStepBase step in _wizard.WizardSteps) { 
                if (step is TemplatedWizardStep) {
                    TemplateDefinition definition = new TemplateDefinition( 
                        this, _contentTemplateName, _wizard, _contentTemplateName, TemplateStyleArray[_navigationStyleLength - 1]);
                    DesignerRegion region = new WizardStepTemplatedEditableRegion(definition, step);
                    region.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark);
                    regions.Add(region); 
                }
                else { 
                    DesignerRegion region = new WizardStepEditableRegion(this, step); 
                    region.Description = SR.GetString(SR.ContainerControlDesigner_RegionWatermark);
                    regions.Add(region); 
                }
            }

            foreach (WizardStepBase step in _wizard.WizardSteps) { 
                regions.Add(new WizardSelectableRegion(this, "Move to " + GetRegionName(step), step));
            } 
        } 

        private ITemplate GetTemplateFromDesignModeState(string[] keys) { 
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null);

            IControlDesignerAccessor accessor = (IControlDesignerAccessor)_wizard; 
            IDictionary dictionary = accessor.GetDesignModeState();
            ResetInternalControls(dictionary); 
 
            string persistedText = String.Empty;
            foreach (string key in keys) { 
                Control ctrl = dictionary[key] as Control;
                if (ctrl != null && ctrl.Visible) {
                    // Fix the control ID to match magic IDs.
                    ctrl.ID = key; 
                    persistedText += ControlPersister.PersistControl(ctrl, designerHost);
                } 
            } 

            return ControlParser.ParseTemplate(designerHost, persistedText); 
        }

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.ConvertToTemplate"]/*' />
        protected void ConvertToTemplate(string description, IComponent component, 
                                         string templateName, string[] keys) {
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null); 

            InvokeTransactedChange(Component, new TransactedChangeCallback(ConvertToTemplateCallBack), 
                new Triplet(component, templateName, keys), description);

            UpdateDesignTimeHtml();
        } 

        private bool ConvertToTemplateCallBack(object context) { 
            Triplet triplet = (Triplet)context; 

            IComponent component = (IComponent)triplet.First; 
            String templateName = (String)triplet.Second;
            String[] keys = (String[])triplet.Third;

            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(component)[templateName]; 
            descriptor.SetValue(component, GetTemplateFromDesignModeState(keys));
 
            return true; 
        }
 
        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.OnConvertToCustomNavigationTemplate"]/*' />
        protected virtual void ConvertToCustomNavigationTemplate() {
            try {
                Debug.Assert(ActiveStep is TemplatedWizardStep); 
                ITemplate navigationTemplate = null;
                string description = SR.GetString(SR.Wizard_ConvertToCustomNavigationTemplate); 
 
                TemplatedWizardStep activeStep = ActiveStep as TemplatedWizardStep;
                if (activeStep != null) { 
                    // Check skin case first, if the skin has a custom navigation template, just use that.
                    TemplatedWizardStep viewActiveStep = ((Wizard)ViewControl).ActiveStep as TemplatedWizardStep;
                    if (viewActiveStep != null && viewActiveStep.CustomNavigationTemplate != null) {
                        navigationTemplate = viewActiveStep.CustomNavigationTemplate; 
                    }
                    else { 
                        // Convert the nav template they are using to a custom template 
                        WizardStepType stepType = _wizard.GetStepType(activeStep, ActiveStepIndex);
                        switch (stepType) { 
                            case WizardStepType.Start:
                                navigationTemplate = GetTemplateFromDesignModeState(_startButtonIDs);
                                break;
                            case WizardStepType.Step: 
                                navigationTemplate = GetTemplateFromDesignModeState(_stepButtonIDs);
                                break; 
                            case WizardStepType.Finish: 
                                navigationTemplate = GetTemplateFromDesignModeState(_finishButtonIDs);
                                break; 
                        }
                    }

                    InvokeTransactedChange(Component, new TransactedChangeCallback(ConvertToCustomNavigationTemplateCallBack), 
                        navigationTemplate, description);
                } 
            } 
            catch (Exception ex) {
                Debug.Fail(ex.ToString()); 
            }
        }

        internal bool ConvertToCustomNavigationTemplateCallBack(object context) { 
            ITemplate template = (ITemplate)context;
            TemplatedWizardStep activeStep = ActiveStep as TemplatedWizardStep; 
            Debug.Assert(activeStep != null); 

            activeStep.CustomNavigationTemplate = template; 

            return true;
        }
 
        private void ConvertToStartNavigationTemplate() {
            Debug.Assert(_wizard.StartNavigationTemplate == null); 
            ConvertToTemplate(SR.GetString(SR.Wizard_ConvertToStartNavigationTemplate), Component, 
                _startNavigationTemplateName, _startButtonIDs);
        } 

        private void ConvertToStepNavigationTemplate() {
            Debug.Assert(_wizard.StepNavigationTemplate == null);
            ConvertToTemplate(SR.GetString(SR.Wizard_ConvertToStepNavigationTemplate), Component, 
                _stepNavigationTemplateName, _stepButtonIDs);
        } 
 
        private void ConvertToFinishNavigationTemplate() {
            Debug.Assert(_wizard.FinishNavigationTemplate == null); 
            ConvertToTemplate(SR.GetString(SR.Wizard_ConvertToFinishNavigationTemplate), Component,
                _finishNavigationTemplateName, _finishButtonIDs);
        }
 
        private void ConvertToSideBarTemplate() {
            Debug.Assert(_wizard.SideBarTemplate == null); 
            ConvertToTemplate(SR.GetString(SR.Wizard_ConvertToSideBarTemplate), Component, 
                _sideBarTemplateName, new string[] {_dataListID});
        } 

        protected override void CreateChildControls() {
            base.CreateChildControls();
 
            Wizard wizard = (Wizard)ViewControl;
 
            // Set the first step as the active step to mimic runtime behavior. 
            if (wizard.ActiveStepIndex == -1 && wizard.WizardSteps.Count > 0) {
                wizard.ActiveStepIndex = 0; 
            }

            IControlDesignerAccessor accessor = (IControlDesignerAccessor)wizard;
            IDictionary dictionary = accessor.GetDesignModeState(); 

            // If we have a templated step content template coming from a stylesheet theme, we must turn off region editing 
            // for the template from the skin to show through. 
            TemplatedWizardStep tsw = wizard.ActiveStep as TemplatedWizardStep;
            if (tsw != null && tsw.ContentTemplate != null && ((TemplatedWizardStep)_wizard.WizardSteps[wizard.ActiveStepIndex]).ContentTemplate == null) { 
                return;
            }

            TableCell stepTableCell = dictionary[_stepTableCellID] as TableCell; 
            if (stepTableCell != null && wizard.ActiveStepIndex != -1) {
                stepTableCell.Attributes["_designerRegion"] = wizard.ActiveStepIndex.ToString(NumberFormatInfo.InvariantInfo); 
            } 
        }
 
        private void DataListItemDataBound(object sender, DataListItemEventArgs e) {
            DataListItem  dataListItem = e.Item;
            WebControl button = dataListItem.FindControl(_sideBarButtonID) as WebControl;
 
            if (button != null) {
                int index = dataListItem.ItemIndex + ((Wizard)ViewControl).WizardSteps.Count; 
                button.Attributes["_designerRegion"] = index.ToString(NumberFormatInfo.InvariantInfo); 
            }
        } 

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.GetDesignTimeHtml"]/*' />
        public override string GetDesignTimeHtml() {
            string designTimeHTML = null; 

            // Nothing to do if the Wizard is empty; 
            if (ActiveStepIndex == -1) { 
                return GetEmptyDesignTimeHtml();
            } 

            Wizard wizard = (Wizard)ViewControl;
            IControlDesignerAccessor viewControlAccessor = (IControlDesignerAccessor)wizard;
            IDictionary dictionary = viewControlAccessor.GetDesignModeState(); 

            DataList sideBarDataList = dictionary[_dataListID] as DataList; 
 
            if (sideBarDataList != null) {
                sideBarDataList.ItemDataBound += new DataListItemEventHandler(this.DataListItemDataBound); 

                ICompositeControlDesignerAccessor ccda = (ICompositeControlDesignerAccessor)wizard;
                ccda.RecreateChildControls();
            } 

            ArrayList titleList = new ArrayList(wizard.WizardSteps.Count); 
            foreach (WizardStepBase step in wizard.WizardSteps) { 
                titleList.Add(step.Title);
 
                if ((step.Title == null || step.Title.Length == 0) && (step.ID == null || step.ID.Length == 0)) {
                    step.Title = GetRegionName(step);
                }
            } 

            //  Make sure the viewcontrol is enabled in region editing mode, otherwise the region editing 
            //  will not function properly. 
            if (!InRegionEditingMode(wizard)) {
                wizard.Enabled = true; 
            }

            designTimeHTML = base.GetDesignTimeHtml();
 
            if ((designTimeHTML == null) || (designTimeHTML.Length == 0)) {
                designTimeHTML = GetEmptyDesignTimeHtml(); 
            } 

            return designTimeHTML; 
        }

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.GetDesignTimeHtml1"]/*' />
        public override string GetDesignTimeHtml(DesignerRegionCollection regions) { 
            AddDesignerRegions(regions);
 
            IControlDesignerAccessor accessor = (IControlDesignerAccessor)_wizard; 
            IDictionary dictionary = null;
 
            try {
                dictionary = accessor.GetDesignModeState();
            }
            catch (Exception ex) { 
                return GetErrorDesignTimeHtml(ex);
            } 
 
            DataList sideBarDataList = dictionary[_dataListID] as DataList;
 
            if (sideBarDataList != null) {
                sideBarDataList.ItemDataBound += new DataListItemEventHandler(this.DataListItemDataBound);
            }
 
            Wizard wizard = (Wizard)ViewControl;
 
            IControlDesignerAccessor viewControlAccessor = (IControlDesignerAccessor)wizard; 
            IDictionary viewControlDictionary = viewControlAccessor.GetDesignModeState();
 
            if (viewControlDictionary != null) {
                viewControlDictionary["ShouldRenderWizardSteps"] = InRegionEditingMode(wizard);
            }
 
            return GetDesignTimeHtml();
        } 
 
        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.GetEditableDesignerRegionContent"]/*' />
        public override string GetEditableDesignerRegionContent(EditableDesignerRegion region) { 
            if (region == null)
                throw new ArgumentNullException("region");

            IWizardStepEditableRegion wizardRegion = region as IWizardStepEditableRegion; 
            if (wizardRegion == null) {
                throw new ArgumentException(SR.GetString(SR.Wizard_InvalidRegion)); 
            } 

            return GetEditableDesignerRegionContent(wizardRegion); 
        }

        internal virtual string GetEditableDesignerRegionContent(IWizardStepEditableRegion region) {
            StringBuilder sb = new StringBuilder(); 
            ControlCollection controls = region.Step.Controls;
 
            IDesignerHost host = (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost)); 

            if (region.Step is TemplatedWizardStep) { 
                TemplatedWizardStep templatedStep = (TemplatedWizardStep)region.Step;
                return ControlPersister.PersistTemplate(templatedStep.ContentTemplate, host);
            }
 
            // Ignore white space only content
            if (controls.Count == 1 && controls[0] is LiteralControl) { 
                string literal = ((LiteralControl)controls[0]).Text; 
                if (literal == null || literal.Trim().Length == 0) {
                    return String.Empty; 
                }
            }

            foreach(Control control in controls) { 
                sb.Append(ControlPersister.PersistControl(control, host));
            } 
 
            return sb.ToString();
        } 

        internal string GetRegionName(WizardStepBase step) {
            if (step.Title != null && step.Title.Length > 0) {
                return step.Title; 
            }
 
            if (step.ID != null && step.ID.Length > 0) { 
                return step.ID;
            } 

            int index = step.Wizard.WizardSteps.IndexOf(step) + 1;
            return "[step ("+index+")]";
        } 

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.Initialize"]/*' /> 
        public override void Initialize(IComponent component) { 
            VerifyInitializeArgument(component, typeof(Wizard));
 
            _wizard = (Wizard)component;
            base.Initialize(component);

            SetViewFlags(ViewFlags.TemplateEditing, true); 
        }
 
        private void MarkPropertyNonBrowsable(IDictionary properties, String propName) { 
            PropertyDescriptor property = (PropertyDescriptor) properties[propName];
            Debug.Assert(property != null, "Property is null: " + propName); 
            if (property != null) {
                properties[propName] = TypeDescriptor.CreateProperty(property.ComponentType, property, BrowsableAttribute.No);
            }
        } 

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.OnClick"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        protected override void OnClick(DesignerRegionMouseEventArgs e) { 
            base.OnClick(e);

            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null); 

            WizardSelectableRegion region = e.Region as WizardSelectableRegion; 
            if (region != null) { 
                PropertyDescriptor descriptor = TypeDescriptor.GetProperties(_wizard)[_activeStepIndexPropName];
                int index = _wizard.WizardSteps.IndexOf(region.Step); 
                Debug.Assert(index != -1);

                if (ActiveStepIndex != index) {
                    using (DesignerTransaction transaction = designerHost.CreateTransaction(_activeStepIndexTransactionDescription)) { 
                        descriptor.SetValue(Component, index);
                        transaction.Commit(); 
                    } 
                }
            } 
        }

        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties); 

            // Handle shadowed properties 
            // VSWhidbey 402538. Need to invalidate verb visibility when DisplaySideBar property changes. 
            PropertyDescriptor prop = (PropertyDescriptor)properties[_displaySideBarPropName];
            if (prop != null) { 
                properties[_displaySideBarPropName] =
                    TypeDescriptor.CreateProperty(GetType(), prop, null);
            }
 
            if (InTemplateMode) {
                MarkPropertyNonBrowsable(properties, _wizardStepsPropertyName); 
            } 

            if (_wizard.StartNavigationTemplate != null) { 
                foreach (String startNavigationTemplatePropName in _startNavigationTemplateProperties) {
                    MarkPropertyNonBrowsable(properties, startNavigationTemplatePropName);
                }
            } 

            if (_wizard.StepNavigationTemplate != null) { 
                foreach (String stepNavigationTemplatePropName in _stepNavigationTemplateProperties) { 
                    MarkPropertyNonBrowsable(properties, stepNavigationTemplatePropName);
                } 
            }

            if (_wizard.FinishNavigationTemplate != null) {
                foreach (String finishNavigationTemplatePropName in _finishNavigationTemplateProperties) { 
                    MarkPropertyNonBrowsable(properties, finishNavigationTemplatePropName);
                } 
            } 

            // Hide cancel button properties if every navigation template is specified. 
            if (_wizard.StartNavigationTemplate != null && _wizard.StepNavigationTemplate != null &&
                _wizard.FinishNavigationTemplate != null) {
                foreach (String generalNavigationButtonPropName in _generalNavigationButtonProperties) {
                    MarkPropertyNonBrowsable(properties, generalNavigationButtonPropName); 
                }
            } 
 
            if (_wizard.HeaderTemplate != null) {
                foreach (String headerPropName in _headerProperties) { 
                    MarkPropertyNonBrowsable(properties, headerPropName);
                }
            }
 
            if (_wizard.SideBarTemplate != null) {
                foreach (String sideBarPropName in _sideBarProperties) { 
                    MarkPropertyNonBrowsable(properties, sideBarPropName); 
                }
            } 
        }

        private void ResetInternalControls(IDictionary dictionary) {
            DataList sideBarDataList = (DataList)dictionary[_dataListID]; 
            if (sideBarDataList != null) {
                sideBarDataList.SelectedIndex = -1; 
            } 
        }
 
        private void ResetCustomNavigationTemplate() {
            WizardStepBase activeStep = ActiveStep;
            Debug.Assert(activeStep is TemplatedWizardStep && ((TemplatedWizardStep)activeStep).CustomNavigationTemplate != null);
 
            InvokeTransactedChange(Component, new TransactedChangeCallback(ResetCustomNavigationTemplateCallBack),
                null, SR.GetString(SR.Wizard_ResetCustomNavigationTemplate)); 
        } 

        private bool ResetCustomNavigationTemplateCallBack(object context) { 
            WizardStepBase activeStep = ActiveStep;
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(activeStep)[_customNavigationTemplateName];
            descriptor.ResetValue(activeStep);
 
            return true;
        } 
 
        private void ResetStartNavigationTemplate() {
            Debug.Assert(_wizard.StartNavigationTemplate != null); 
            ResetTemplate(SR.GetString(SR.Wizard_ResetStartNavigationTemplate), Component, _startNavigationTemplateName);
        }

        private void ResetStepNavigationTemplate() { 
            Debug.Assert(_wizard.StepNavigationTemplate != null);
            ResetTemplate(SR.GetString(SR.Wizard_ResetStepNavigationTemplate), Component, _stepNavigationTemplateName); 
        } 

        private void ResetFinishNavigationTemplate() { 
            Debug.Assert(_wizard.FinishNavigationTemplate != null);
            ResetTemplate(SR.GetString(SR.Wizard_ResetFinishNavigationTemplate), Component, _finishNavigationTemplateName);
        }
 
        private void ResetSideBarTemplate() {
            Debug.Assert(_wizard.SideBarTemplate != null); 
            ResetTemplate(SR.GetString(SR.Wizard_ResetSideBarTemplate), Component, _sideBarTemplateName); 
        }
 
        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.ResetTemplate"]/*' />
        protected void ResetTemplate(string description, IComponent component, string templateName) {
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
            Debug.Assert(designerHost != null); 

            InvokeTransactedChange(Component, new TransactedChangeCallback(ResetTemplateCallBack), 
                new Pair(component, templateName), description); 

            UpdateDesignTimeHtml(); 
        }

        private bool ResetTemplateCallBack(object context) {
            Pair pair = (Pair)context; 

            IComponent component = (IComponent)pair.First; 
            String templateName = (String)pair.Second; 

            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(component)[templateName]; 
            descriptor.ResetValue(component);

            return true;
        } 

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardDesigner.SetEditableDesignerRegionContent"]/*' /> 
        public override void SetEditableDesignerRegionContent(EditableDesignerRegion region, string content) { 
            if (region == null) {
                throw new ArgumentNullException("region"); 
            }

            IWizardStepEditableRegion wizardRegion = region as IWizardStepEditableRegion;
            if (wizardRegion == null) { 
                throw new ArgumentException(SR.GetString(SR.Wizard_InvalidRegion));
            } 
 
            IDesignerHost host = (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost));
            Debug.Assert(host != null, "IDesignerHost is null."); 

            if (wizardRegion.Step is TemplatedWizardStep) {
                IComponent component = (IComponent)wizardRegion.Step;
                ITemplate template = ControlParser.ParseTemplate(host, content); 
                PropertyDescriptor descriptor = TypeDescriptor.GetProperties(component)[_contentTemplateName];
                using (DesignerTransaction transaction = host.CreateTransaction("SetEditableDesignerRegionContent")) { 
                    descriptor.SetValue(component, template); 
                    transaction.Commit();
                } 

                ViewControlCreated = false;
            }
            else { 
                SetWizardStepContent(wizardRegion.Step, content, host);
            } 
        } 

        private void SetWizardStepContent(WizardStepBase step, string content, IDesignerHost host) { 
            Control[] controls = null;
            if (content != null && content.Length > 0) {
                controls = ControlParser.ParseControls(host, content);
            } 

            step.Controls.Clear(); 
            if (controls == null) 
                return;
 
            foreach(Control control in controls) {
                step.Controls.Add(control);
            }
        } 

        private void StartWizardStepCollectionEditor() { 
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            Debug.Assert(designerHost != null);
 
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Component)[_wizardStepsPropertyName];

            using (DesignerTransaction transaction = designerHost.CreateTransaction(SR.GetString(SR.Wizard_StartWizardStepCollectionEditor))) {
                UITypeEditor editor = (UITypeEditor)descriptor.GetEditor(typeof(UITypeEditor)); 
                object newValue = editor.EditValue(new TypeDescriptorContext(designerHost, descriptor, Component),
                                                   new WindowsFormsEditorServiceHelper(this), descriptor.GetValue(Component)); 
                if (newValue != null) { 
                    transaction.Commit();
                } 
            }

            // Recreate child controls only if activestepindex is valid.
            if (_wizard.ActiveStepIndex >= -1 && _wizard.ActiveStepIndex < _wizard.WizardSteps.Count) { 

                // Ignore any exception that might happen during child control creation, 
                // these errors will eventually show up during GetDesignTimeHtml() 
                try {
                    ViewControlCreated = false; 
                    CreateChildControls();
                }
                catch { }
            } 
        }
 
        private class WizardDesignerActionList : DesignerActionList { 
            private WizardDesigner _designer;
 
            public WizardDesignerActionList(WizardDesigner designer) : base(designer.Component) {
                _designer = designer;
            }
 
            public override bool AutoShow {
                get { 
                    return true; 
                }
                set { 
                }
            }

            [TypeConverter(typeof(WizardStepTypeConverter))] 
            public int View {
                get { 
                    return _designer.ActiveStepIndex; 
                }
                set { 
                    // Do nothing if the value is unchanged.
                    if (value == _designer.ActiveStepIndex) {
                        return;
                    } 

                    IDesignerHost designerHost = (IDesignerHost)_designer.GetService(typeof(IDesignerHost)); 
                    Debug.Assert(designerHost != null); 

                    PropertyDescriptor descriptor = TypeDescriptor.GetProperties(_designer.Component)[WizardDesigner._activeStepIndexPropName]; 

                    using (DesignerTransaction transaction = designerHost.CreateTransaction(SR.GetString(SR.Wizard_OnViewChanged))) {
                        descriptor.SetValue(_designer.Component, value);
                        transaction.Commit(); 
                    }
 
                    _designer.UpdateDesignTimeHtml(); 
                    TypeDescriptor.Refresh(_designer.Component);
                } 
            }

            public override DesignerActionItemCollection GetSortedActionItems() {
                DesignerActionItemCollection items = new DesignerActionItemCollection(); 

                if (!_designer.InTemplateMode) { 
                    if (_designer._wizard.WizardSteps.Count > 0) { 
                        items.Add(new DesignerActionPropertyItem("View",
                                                                  SR.GetString(SR.Wizard_StepsView), 
                                                                  String.Empty,
                                                                  SR.GetString(SR.Wizard_StepsViewDescription)));
                    }
 
                    items.Add(new DesignerActionMethodItem(this, "StartWizardStepCollectionEditor",
                        SR.GetString(SR.Wizard_StartWizardStepCollectionEditor), String.Empty, 
                        SR.GetString(SR.Wizard_StartWizardStepCollectionEditorDescription), true)); 
                    Wizard wizard = _designer._wizard;
 
                    int index = _designer.ActiveStepIndex;

                    if (index >= 0 && index < wizard.WizardSteps.Count) {
                        if (wizard.StartNavigationTemplate != null) { 
                            items.Add(new DesignerActionMethodItem(this, "ResetStartNavigationTemplate",
                                SR.GetString(SR.Wizard_ResetStartNavigationTemplate), String.Empty, 
                                SR.GetString(SR.Wizard_ResetDescription, "StartNavigation"), true)); 
                        } else {
                            items.Add(new DesignerActionMethodItem(this, "ConvertToStartNavigationTemplate", 
                                SR.GetString(SR.Wizard_ConvertToStartNavigationTemplate), String.Empty,
                                SR.GetString(SR.Wizard_ConvertToTemplateDescription, "StartNavigation"), true));
                        }
 
                        if (wizard.StepNavigationTemplate != null) {
                            items.Add(new DesignerActionMethodItem(this, "ResetStepNavigationTemplate", 
                                SR.GetString(SR.Wizard_ResetStepNavigationTemplate), String.Empty, 
                                SR.GetString(SR.Wizard_ResetDescription, "StepNavigation"), true));
                        } else { 
                            items.Add(new DesignerActionMethodItem(this, "ConvertToStepNavigationTemplate",
                                SR.GetString(SR.Wizard_ConvertToStepNavigationTemplate), String.Empty,
                                SR.GetString(SR.Wizard_ConvertToTemplateDescription, "StepNavigation"), true));
                        } 

                        if (wizard.FinishNavigationTemplate != null) { 
                            items.Add(new DesignerActionMethodItem(this, "ResetFinishNavigationTemplate", 
                                SR.GetString(SR.Wizard_ResetFinishNavigationTemplate), String.Empty,
                                SR.GetString(SR.Wizard_ResetDescription, "FinishNavigation"), true)); 
                        } else {
                            items.Add(new DesignerActionMethodItem(this, "ConvertToFinishNavigationTemplate",
                                SR.GetString(SR.Wizard_ConvertToFinishNavigationTemplate), String.Empty,
                                SR.GetString(SR.Wizard_ConvertToTemplateDescription, "FinishNavigation"), true)); 
                        }
 
                        if (wizard.DisplaySideBar) { 
                            if (wizard.SideBarTemplate != null) {
                                items.Add(new DesignerActionMethodItem(this, "ResetSideBarTemplate", 
                                    SR.GetString(SR.Wizard_ResetSideBarTemplate), String.Empty,
                                    SR.GetString(SR.Wizard_ResetDescription, "SideBar"), true));
                            } else {
                                items.Add(new DesignerActionMethodItem(this, "ConvertToSideBarTemplate", 
                                    SR.GetString(SR.Wizard_ConvertToSideBarTemplate), String.Empty,
                                    SR.GetString(SR.Wizard_ConvertToTemplateDescription, "SideBar"), true)); 
                            } 
                        }
 
                        TemplatedWizardStep templatedActiveStep = _designer.ActiveStep as TemplatedWizardStep;
                        // Do not display the "ConvertToCustomNavigationTemplate" if it's a complete step
                        if (templatedActiveStep != null &&
                            templatedActiveStep.StepType != WizardStepType.Complete) { 
                            if (templatedActiveStep.CustomNavigationTemplate != null) {
                                items.Add(new DesignerActionMethodItem(this, "ResetCustomNavigationTemplate", 
                                    SR.GetString(SR.Wizard_ResetCustomNavigationTemplate), String.Empty, 
                                    SR.GetString(SR.Wizard_ResetDescription, "CustomNavigation"), true));
                            } else { 
                                items.Add(new DesignerActionMethodItem(this, "ConvertToCustomNavigationTemplate",
                                    SR.GetString(SR.Wizard_ConvertToCustomNavigationTemplate), String.Empty,
                                    SR.GetString(SR.Wizard_ConvertToTemplateDescription, "CustomNavigation"), true));
                            } 
                        }
                    } 
                } 

                return items; 
            }

            public void ConvertToCustomNavigationTemplate() {
                _designer.ConvertToCustomNavigationTemplate(); 
            }
 
            public void ConvertToFinishNavigationTemplate() { 
                _designer.ConvertToFinishNavigationTemplate();
            } 

            public void ConvertToSideBarTemplate() {
                _designer.ConvertToSideBarTemplate();
            } 

            public void ConvertToStartNavigationTemplate() { 
                _designer.ConvertToStartNavigationTemplate(); 
            }
 
            public void ConvertToStepNavigationTemplate() {
                _designer.ConvertToStepNavigationTemplate();
            }
 
            public void ResetCustomNavigationTemplate() {
                _designer.ResetCustomNavigationTemplate(); 
            } 

            public void ResetFinishNavigationTemplate() { 
                _designer.ResetFinishNavigationTemplate();
            }

            public void ResetSideBarTemplate() { 
                _designer.ResetSideBarTemplate();
            } 
 
            public void ResetStartNavigationTemplate() {
                _designer.ResetStartNavigationTemplate(); 
            }

            public void ResetStepNavigationTemplate() {
                _designer.ResetStepNavigationTemplate(); 
            }
 
            public void StartWizardStepCollectionEditor() { 
                _designer.StartWizardStepCollectionEditor();
            } 

            private class WizardStepTypeConverter : TypeConverter {
                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
                    int[] stepValues = null; 
                    if (context != null) {
                        WizardDesignerActionList list = (WizardDesignerActionList)context.Instance; 
                        WizardDesigner designer = list._designer; 
                        WizardStepCollection steps = designer._wizard.WizardSteps;
                        stepValues = new int[steps.Count]; 
                        for (int i = 0; i < steps.Count; i++) {
                            stepValues[i] = i;
                        }
                    } 
                    return new StandardValuesCollection(stepValues);
                } 
 
                public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
                    return true; 
                }

                public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
                    return true; 
                }
 
                public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) { 
                    if (destinationType == typeof(string)) {
                        if (value is string) return value; 
                        WizardDesignerActionList list = (WizardDesignerActionList)context.Instance;
                        WizardDesigner designer = list._designer;
                        WizardStepCollection steps = designer._wizard.WizardSteps;
                        if (value is int) { 
                            int intValue = (int)value;
                            if (intValue == -1 && steps.Count > 0) { 
                                intValue = 0; 
                            }
 
                            if (intValue >= steps.Count) {
                                return null;
                            }
 
                            return designer.GetRegionName(steps[intValue]);
                        } 
                    } 
                    return base.ConvertTo(context, culture, value, destinationType);
                } 

                public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
                    if (value is string) {
                        WizardDesignerActionList list = (WizardDesignerActionList)context.Instance; 
                        WizardDesigner designer = list._designer;
                        WizardStepCollection steps = designer._wizard.WizardSteps; 
                        for (int i = 0; i < steps.Count; i++) { 
                            if (String.Compare(designer.GetRegionName(steps[i]), (string)value, StringComparison.Ordinal) == 0) {
                                return i; 
                            }
                        }
                    }
                    return base.ConvertFrom (context, culture, value); 
                }
 
                public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) { 
                    if (sourceType == typeof(string)) {
                        return true; 
                    }
                    return base.CanConvertFrom(context, sourceType);
                }
 
                public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
                    if (destinationType == typeof(string)) { 
                        return true; 
                    }
                    return base.CanConvertTo(context, destinationType); 
                }
            }
        }
    } 

    internal interface IWizardStepEditableRegion { 
        WizardStepBase Step { get; } 
    }
 
    /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardStepEditableRegion"]/*' />
    public class WizardStepEditableRegion : EditableDesignerRegion, IWizardStepEditableRegion {
        private WizardStepBase _wizardStep;
 
        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardStepEditableRegion.WizardStepEditableRegion"]/*' />
        public WizardStepEditableRegion(WizardDesigner designer, WizardStepBase wizardStep) : 
            base(designer, designer.GetRegionName(wizardStep), false) { 
            _wizardStep = wizardStep;
            EnsureSize = true; 
        }

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardStepEditableRegion.Step"]/*' />
        public WizardStepBase Step { 
            get { return _wizardStep; }
        } 
    } 

    /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardStepTemplatedEditableRegion"]/*' /> 
    public class WizardStepTemplatedEditableRegion : TemplatedEditableDesignerRegion, IWizardStepEditableRegion {
        private WizardStepBase _wizardStep;

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardStepTemplatedEditableRegion.WizardStepEditableRegion"]/*' /> 
        public WizardStepTemplatedEditableRegion(TemplateDefinition templateDefinition, WizardStepBase wizardStep) :
            base(templateDefinition) { 
            _wizardStep = wizardStep; 
            EnsureSize = true;
        } 

        /// <include file='doc\WizardDesigner.uex' path='docs/doc[@for="WizardStepTemplatedEditableRegion.Step"]/*' />
        public WizardStepBase Step {
            get { return _wizardStep; } 
        }
    } 
 
    internal class WizardStepBaseTemplateDefinition : TemplateDefinition {
        private WizardStepBase _step; 

        public WizardStepBaseTemplateDefinition(WizardDesigner designer, WizardStepBase step, string name, Style style) : base(designer, name, step, name, style) {
            _step = step;
        } 

        public override string Content { 
            get { 
                StringBuilder sb = new StringBuilder();
                foreach(Control control in _step.Controls) { 
                    sb.Append(ControlPersister.PersistControl(control));
                }

                return sb.ToString(); 
            }
            set { 
                _step.Controls.Clear(); 
                if (value == null)
                    return; 

                IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
                Debug.Assert(designerHost != null);
 
                Control[] controls = null;
                controls = ControlParser.ParseControls(designerHost, value); 
 
                foreach(Control control in controls) {
                    _step.Controls.Add(control); 
                }
            }
        }
 
    }
 
    internal class WizardSelectableRegion : DesignerRegion { 
        private WizardStepBase _wizardStep;
 
        internal WizardSelectableRegion(WizardDesigner designer, string name, WizardStepBase wizardStep) : base(designer, name, true) {
            _wizardStep = wizardStep;
        }
 
        internal WizardStepBase Step {
            get { return _wizardStep; } 
        } 
    }
 
    internal sealed class WizardAutoFormat : DesignerAutoFormat {

        private string FontName;
        private FontUnit FontSize; 
        private Color BackColor;
        private Color BorderColor; 
        private Unit BorderWidth; 
        private BorderStyle BorderStyle;
        private Unit NavigationButtonStyleBorderWidth; 
        private string NavigationButtonStyleFontName;
        private FontUnit NavigationButtonStyleFontSize;
        private BorderStyle NavigationButtonStyleBorderStyle;
        private Color NavigationButtonStyleBorderColor; 
        private Color NavigationButtonStyleForeColor;
        private Color NavigationButtonStyleBackColor; 
        private Unit StepStyleBorderWidth; 
        private BorderStyle StepStyleBorderStyle;
        private Color StepStyleBorderColor; 
        private Color StepStyleForeColor;
        private Color StepStyleBackColor;
        private FontUnit StepStyleFontSize;
        private bool SideBarButtonStyleFontUnderline; 
        private string SideBarButtonStyleFontName;
        private Color SideBarButtonStyleForeColor; 
        private Unit SideBarButtonStyleBorderWidth; 
        private Color SideBarButtonStyleBackColor;
        private Color HeaderStyleForeColor; 
        private Color HeaderStyleBorderColor;
        private Color HeaderStyleBackColor;
        private FontUnit HeaderStyleFontSize;
        private bool HeaderStyleFontBold; 
        private Unit HeaderStyleBorderWidth;
        private HorizontalAlign HeaderStyleHorizontalAlign; 
        private BorderStyle HeaderStyleBorderStyle; 
        private Color SideBarStyleBackColor;
        private VerticalAlign SideBarStyleVerticalAlign; 
        private FontUnit SideBarStyleFontSize;
        private bool SideBarStyleFontUnderline;
        private bool SideBarStyleFontStrikeout;
        private Unit SideBarStyleBorderWidth; 

        public WizardAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) { 
            Load(schemeData); 

            Style.Width = 350; 
            Style.Height = 200;
        }

        public override void Apply(Control control) { 
            Debug.Assert(control is Wizard, "WizardAutoFormat:ApplyScheme- control is not Wizard");
            if (control is Wizard) { 
                Apply(control as Wizard); 
            }
        } 

        private void Apply(Wizard wizard) {
            wizard.Font.Name = FontName;
            wizard.Font.Size = FontSize; 
            wizard.BackColor = BackColor;
            wizard.BorderColor = BorderColor; 
            wizard.BorderWidth = BorderWidth; 
            wizard.BorderStyle = BorderStyle;
            wizard.Font.ClearDefaults(); 

            wizard.NavigationButtonStyle.BorderWidth = NavigationButtonStyleBorderWidth;
            wizard.NavigationButtonStyle.Font.Name = NavigationButtonStyleFontName;
            wizard.NavigationButtonStyle.Font.Size = NavigationButtonStyleFontSize; 
            wizard.NavigationButtonStyle.BorderStyle = NavigationButtonStyleBorderStyle;
            wizard.NavigationButtonStyle.BorderColor = NavigationButtonStyleBorderColor; 
            wizard.NavigationButtonStyle.ForeColor = NavigationButtonStyleForeColor; 
            wizard.NavigationButtonStyle.BackColor = NavigationButtonStyleBackColor;
            wizard.NavigationButtonStyle.Font.ClearDefaults(); 

            wizard.StepStyle.BorderWidth = StepStyleBorderWidth;
            wizard.StepStyle.BorderStyle = StepStyleBorderStyle;
            wizard.StepStyle.BorderColor = StepStyleBorderColor; 
            wizard.StepStyle.ForeColor = StepStyleForeColor;
            wizard.StepStyle.BackColor = StepStyleBackColor; 
            wizard.StepStyle.Font.Size = StepStyleFontSize; 
            wizard.StepStyle.Font.ClearDefaults();
 
            wizard.SideBarButtonStyle.Font.Underline = SideBarButtonStyleFontUnderline;
            wizard.SideBarButtonStyle.Font.Name = SideBarButtonStyleFontName;
            wizard.SideBarButtonStyle.ForeColor = SideBarButtonStyleForeColor;
            wizard.SideBarButtonStyle.BorderWidth = SideBarButtonStyleBorderWidth; 
            wizard.SideBarButtonStyle.BackColor = SideBarButtonStyleBackColor;
            wizard.SideBarButtonStyle.Font.ClearDefaults(); 
 
            wizard.HeaderStyle.ForeColor = HeaderStyleForeColor;
            wizard.HeaderStyle.BorderColor = HeaderStyleBorderColor; 
            wizard.HeaderStyle.BackColor = HeaderStyleBackColor;
            wizard.HeaderStyle.Font.Size = HeaderStyleFontSize;
            wizard.HeaderStyle.Font.Bold = HeaderStyleFontBold;
            wizard.HeaderStyle.BorderWidth = HeaderStyleBorderWidth; 
            wizard.HeaderStyle.HorizontalAlign = HeaderStyleHorizontalAlign;
            wizard.HeaderStyle.BorderStyle = HeaderStyleBorderStyle; 
            wizard.HeaderStyle.Font.ClearDefaults(); 

            wizard.SideBarStyle.BackColor = SideBarStyleBackColor; 
            wizard.SideBarStyle.VerticalAlign = SideBarStyleVerticalAlign;
            wizard.SideBarStyle.Font.Size = SideBarStyleFontSize;
            wizard.SideBarStyle.Font.Underline = SideBarStyleFontUnderline;
            wizard.SideBarStyle.Font.Strikeout = SideBarStyleFontStrikeout; 
            wizard.SideBarStyle.BorderWidth = SideBarStyleBorderWidth;
            wizard.SideBarStyle.Font.ClearDefaults(); 
        } 

        private bool GetBooleanProperty(string propertyTag, DataRow schemeData) { 
            object data = schemeData[propertyTag];
            if ((data != null) && !data.Equals(DBNull.Value))
                return bool.Parse(data.ToString());
            else 
                return false;
        } 
 
        private int GetIntProperty(string propertyTag, DataRow schemeData) {
            object data = schemeData[propertyTag]; 
            if ((data != null) && !data.Equals(DBNull.Value))
                return Int32.Parse(data.ToString(), CultureInfo.InvariantCulture);
            else
                return 0; 
        }
 
        private string GetStringProperty(string propertyTag, DataRow schemeData) { 
            object data = schemeData[propertyTag];
            if ((data != null) && !data.Equals(DBNull.Value)) 
                return data.ToString();
            else
                return String.Empty;
        } 

        private void Load(DataRow schemeData) { 
            if (schemeData == null) { 
                Debug.Write("CalendarAutoFormatUtil:LoadScheme- scheme not found");
                return; 
            }

            FontName = GetStringProperty("FontName", schemeData);
            FontSize = new FontUnit(GetStringProperty("FontSize", schemeData), CultureInfo.InvariantCulture); 
            BackColor = ColorTranslator.FromHtml(GetStringProperty("BackColor", schemeData));
            BorderColor = ColorTranslator.FromHtml(GetStringProperty("BorderColor", schemeData)); 
            BorderWidth = new Unit(GetStringProperty("BorderWidth", schemeData), CultureInfo.InvariantCulture); 
            SideBarStyleBackColor = ColorTranslator.FromHtml(GetStringProperty("SideBarStyleBackColor", schemeData));
            SideBarStyleVerticalAlign = (VerticalAlign)GetIntProperty("SideBarStyleVerticalAlign", schemeData); 
            BorderStyle = (BorderStyle)GetIntProperty("BorderStyle", schemeData);
            NavigationButtonStyleBorderWidth = new Unit(GetStringProperty("NavigationButtonStyleBorderWidth", schemeData), CultureInfo.InvariantCulture);
            NavigationButtonStyleFontName = GetStringProperty("NavigationButtonStyleFontName", schemeData);
            NavigationButtonStyleFontSize = new FontUnit(GetStringProperty("NavigationButtonStyleFontSize", schemeData), CultureInfo.InvariantCulture); 
            NavigationButtonStyleBorderStyle = (BorderStyle)GetIntProperty("NavigationButtonStyleBorderStyle", schemeData);
            NavigationButtonStyleBorderColor = ColorTranslator.FromHtml(GetStringProperty("NavigationButtonStyleBorderColor", schemeData)); 
            NavigationButtonStyleForeColor = ColorTranslator.FromHtml(GetStringProperty("NavigationButtonStyleForeColor", schemeData)); 
            NavigationButtonStyleBackColor = ColorTranslator.FromHtml(GetStringProperty("NavigationButtonStyleBackColor", schemeData));
            StepStyleBorderWidth = new Unit(GetStringProperty("StepStyleBorderWidth", schemeData), CultureInfo.InvariantCulture); 
            StepStyleBorderStyle = (BorderStyle)GetIntProperty("StepStyleBorderStyle", schemeData);
            StepStyleBorderColor = ColorTranslator.FromHtml(GetStringProperty("StepStyleBorderColor", schemeData));
            StepStyleForeColor = ColorTranslator.FromHtml(GetStringProperty("StepStyleForeColor", schemeData));
            StepStyleBackColor = ColorTranslator.FromHtml(GetStringProperty("StepStyleBackColor", schemeData)); 
            StepStyleFontSize = new FontUnit(GetStringProperty("StepStyleFontSize", schemeData), CultureInfo.InvariantCulture);
            SideBarButtonStyleFontUnderline = GetBooleanProperty("SideBarButtonStyleFontUnderline", schemeData); 
            SideBarButtonStyleFontName = GetStringProperty("SideBarButtonStyleFontName", schemeData); 
            SideBarButtonStyleForeColor = ColorTranslator.FromHtml(GetStringProperty("SideBarButtonStyleForeColor", schemeData));
            SideBarButtonStyleBorderWidth = new Unit(GetStringProperty("SideBarButtonStyleBorderWidth", schemeData), CultureInfo.InvariantCulture); 
            SideBarButtonStyleBackColor = ColorTranslator.FromHtml(GetStringProperty("SideBarButtonStyleBackColor", schemeData));
            HeaderStyleForeColor = ColorTranslator.FromHtml(GetStringProperty("HeaderStyleForeColor", schemeData));
            HeaderStyleBorderColor = ColorTranslator.FromHtml(GetStringProperty("HeaderStyleBorderColor", schemeData));
            HeaderStyleBackColor = ColorTranslator.FromHtml(GetStringProperty("HeaderStyleBackColor", schemeData)); 
            HeaderStyleFontSize = new FontUnit(GetStringProperty("HeaderStyleFontSize", schemeData), CultureInfo.InvariantCulture);
            HeaderStyleFontBold = GetBooleanProperty("HeaderStyleFontBold", schemeData); 
            HeaderStyleBorderWidth = new Unit(GetStringProperty("HeaderStyleBorderWidth", schemeData), CultureInfo.InvariantCulture); 
            HeaderStyleHorizontalAlign = (HorizontalAlign)GetIntProperty("HeaderStyleHorizontalAlign", schemeData);
            HeaderStyleBorderStyle = (BorderStyle)GetIntProperty("HeaderStyleBorderStyle", schemeData); 
            SideBarStyleBackColor = ColorTranslator.FromHtml(GetStringProperty("SideBarStyleBackColor", schemeData));
            SideBarStyleVerticalAlign = (VerticalAlign)GetIntProperty("SideBarStyleVerticalAlign", schemeData);
            SideBarStyleFontSize = new FontUnit(GetStringProperty("SideBarStyleFontSize", schemeData), CultureInfo.InvariantCulture);
            SideBarStyleFontUnderline = GetBooleanProperty("SideBarStyleFontUnderline", schemeData); 
            SideBarStyleFontStrikeout = GetBooleanProperty("SideBarStyleFontStrikeout", schemeData);
            SideBarStyleBorderWidth = new Unit(GetStringProperty("SideBarStyleBorderWidth", schemeData), CultureInfo.InvariantCulture); 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
