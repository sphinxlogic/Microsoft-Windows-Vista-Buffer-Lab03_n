//------------------------------------------------------------------------------ 
// <copyright file="TemplatedControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Design;
    using System.Diagnostics; 

    using System;
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.Web.UI; 
    using System.Web.UI.Design; 
    using System.Web.UI.WebControls;
    using System.ComponentModel.Design; 

    /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner"]/*' />
    /// <devdoc>
    ///    <para>Provides a base class for all server control designers that are template-based.</para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public abstract class TemplatedControlDesigner : ControlDesigner { 

        private bool                    enableTemplateEditing;          // True to enable template editing, and false otherwise. 
        private TemplatedControlDesignerTemplateGroup _currentTemplateGroup;
        private IDictionary _templateGroupTable;

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.TemplatedControlDesigner"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of the <see cref='System.Web.UI.Design.TemplatedControlDesigner'/> 
        ///       class.
        ///    </para> 
        /// </devdoc>
        public TemplatedControlDesigner() {
            enableTemplateEditing = true;
        } 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.ActiveTemplateEditingFrame"]/*' /> 
        /// <devdoc> 
        ///     The currently active template frame object (will be null when not in template mode).
        /// </devdoc> 
        [Obsolete("Use of this property is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        public ITemplateEditingFrame ActiveTemplateEditingFrame {
            get {
                if (_currentTemplateGroup != null) { 
                    return _currentTemplateGroup.Frame;
                } 
                return null; 
            }
        } 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.CanEnterTemplateMode"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Whether or not this designer will allow editing of templates.
        ///    </para> 
        /// </devdoc> 
        public bool CanEnterTemplateMode {
            get { 
                return enableTemplateEditing;
            }
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.DataBindingsEnabled"]/*' />
        /// <internalonly/> 
        protected override bool DataBindingsEnabled { 
            get {
                if (InTemplateModeInternal && HidePropertiesInTemplateMode) { 
                    return false;
                }
                return base.DataBindingsEnabled;
            } 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.InTemplateMode"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Whether or not the designer document is in template mode.
        ///    </para>
        /// </devdoc>
        [Obsolete("The recommended alternative is System.Web.UI.Design.ControlDesigner.InTemplateMode. http://go.microsoft.com/fwlink/?linkid=14202")] 
        public new bool InTemplateMode {
            get { 
                return (_currentTemplateGroup != null); 
            }
        } 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.InTemplateModeInternal"]/*' />
        internal bool InTemplateModeInternal {
            get { 
#pragma warning disable 618
                return InTemplateMode; 
#pragma warning restore 618 
            }
        } 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.TemplateEditingVerbHandler"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Verb execution handler for opening the template frames and entering template mode.
        ///    </para> 
        /// </devdoc> 
        internal EventHandler TemplateEditingVerbHandler {
            get { 
                return new EventHandler(this.OnTemplateEditingVerbInvoked);
            }
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.TemplateGroups"]/*' />
        public override TemplateGroupCollection TemplateGroups { 
            get { 
                TemplateGroupCollection groups = base.TemplateGroups;
 
                TemplateGroupTable.Clear();
#pragma warning disable 618
                TemplateEditingVerbCollection verbs = GetTemplateEditingVerbsInternal();
                foreach (TemplateEditingVerb verb in verbs) { 
#pragma warning restore 618
                    if (!verb.Enabled || !verb.Visible) { 
                        continue; 
                    }
 
#pragma warning disable 618
                    ITemplateEditingFrame frame = CreateTemplateEditingFrame(verb);
#pragma warning restore 618
                    frame.Verb = verb; 

                    TemplateGroup group = new TemplatedControlDesignerTemplateGroup(verb, frame); 
 
                    bool hasStyles = (frame.TemplateStyles != null);
                    for (int j = 0; j < frame.TemplateNames.Length; j++) { 
                        Style templateStyle = hasStyles ? frame.TemplateStyles[j] : null;
                        TemplatedControlDesignerTemplateDefinition template =
                            new TemplatedControlDesignerTemplateDefinition(frame.TemplateNames[j], templateStyle, this, frame);
 
                        // All v1 templates support databinding
                        template.SupportsDataBinding = true; 
 
                        group.AddTemplateDefinition(template);
                    } 

                    groups.Add(group);
                    TemplateGroupTable[frame] = group;
                } 

                return groups; 
            } 
        }
 
        private IDictionary TemplateGroupTable {
            get {
                if (_templateGroupTable == null) {
                    _templateGroupTable = new HybridDictionary(); 
                }
 
                return _templateGroupTable; 
            }
        } 


        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.CreateTemplateEditingFrame"]/*' />
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        protected abstract ITemplateEditingFrame CreateTemplateEditingFrame(TemplateEditingVerb verb);
 
        private void EnableTemplateEditing(bool enable) { 
            enableTemplateEditing = enable;
            // 
        }

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.EnterTemplateMode"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Opens a particular template frame object for editing in the designer. 
        ///    </para> 
        /// </devdoc>
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public void EnterTemplateMode(ITemplateEditingFrame newTemplateEditingFrame) {
            Debug.Assert(newTemplateEditingFrame != null, "New template frame passed in is null!");

            // Return immediately when trying to open (again) the currently active template frame. 
            if (ActiveTemplateEditingFrame == newTemplateEditingFrame) {
                return; 
            } 

            if (BehaviorInternal != null) { 
                Debug.Assert(BehaviorInternal is IControlDesignerBehavior, "Invalid element.");
                IControlDesignerBehavior behavior = (IControlDesignerBehavior)BehaviorInternal;

                try { 
                    bool switchingTemplates = false;
                    if (InTemplateModeInternal) { 
                        // This is the case of switching from template frame to another. 
                        switchingTemplates = true;
                        ExitTemplateModeInternal(switchingTemplates, /*fNested*/ false, /*fSave*/ true); 
                    }
                    else {
                        // Clear the design time HTML when entering template mode from read-only/preview mode.
                        if (behavior != null) { 
                            behavior.DesignTimeHtml = String.Empty;
                        } 
                    } 

                    // The designer is now in template editing mode. 
                    _currentTemplateGroup = (TemplatedControlDesignerTemplateGroup)TemplateGroupTable[newTemplateEditingFrame];
                    if (_currentTemplateGroup == null) {
                        _currentTemplateGroup = new TemplatedControlDesignerTemplateGroup(null, newTemplateEditingFrame);
                    } 

                    if (!switchingTemplates) { 
                        RaiseTemplateModeChanged(); 
                    }
 
                    // Open the new template frame and make it visible.
                    ActiveTemplateEditingFrame.Open();

                    // Mark the designer as dirty when in template mode. 
                    IsDirtyInternal = true;
 
                    // Invalidate the type descriptor so that proper filtering of properties 
                    // is done when entering template mode.
                    TypeDescriptor.Refresh(Component); 
                }
                catch {
                }
 
                IWebFormsDocumentService wfServices = (IWebFormsDocumentService)GetService(typeof(IWebFormsDocumentService));
                if (wfServices != null) { 
                    wfServices.UpdateSelection(); 
                }
            } 
        }

#pragma warning disable 618
        private void EnterTemplateModeInternal(ITemplateEditingFrame newTemplateEditingFrame) { 
            EnterTemplateMode(newTemplateEditingFrame);
        } 
#pragma warning restore 618 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.ExitNestedTemplates"]/*' /> 
        /// <devdoc>
        ///     This method ensures that for a particular templated control designer when exiting
        ///     its template mode handles nested templates (if any). This is done by exiting the
        ///     inner most template frames first before exiting itself. Inside-Out Model. 
        /// </devdoc>
        private void ExitNestedTemplates(bool fSave) { 
            try { 
                IComponent component = ViewControl;
                IDesignerHost host = (IDesignerHost)component.Site.GetService(typeof(IDesignerHost)); 

                ControlCollection children = ((Control)component).Controls;
                for (int i = 0; i < children.Count; i++) {
                    IDesigner designer = host.GetDesigner((IComponent)children[i]); 
                    if (designer is TemplatedControlDesigner) {
                        TemplatedControlDesigner innerDesigner = (TemplatedControlDesigner)designer; 
                        if (innerDesigner.InTemplateModeInternal) { 
                            innerDesigner.ExitTemplateModeInternal(/*fSwitchingTemplates*/ false, /*fNested*/ true, /*fSave*/ fSave);
                        } 
                    }
                }
            }
            catch (Exception ex) { 
                Debug.Fail(ex.ToString());
            } 
        } 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.ExitTemplateMode"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Closes the currently active template editing frame after saving any relevant changes.
        ///    </para> 
        /// </devdoc>
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public void ExitTemplateMode(bool fSwitchingTemplates, bool fNested, bool fSave) { 
            Debug.Assert(ActiveTemplateEditingFrame != null, "Invalid current template frame!");
 
            try {
                // First let the inner/nested designers handle exiting of their template mode.
                // Note: This has to be done inside-out in order to ensure that the changes
                // made in a particular template are saved before its immediate outer level 
                // control designer saves its children.
                ExitNestedTemplates(fSave); 
 
                // Save the current contents of all the templates within the active frame, and
                // close the frame by removing it from the tree. 
                ActiveTemplateEditingFrame.Close(fSave);

                if (!fSwitchingTemplates) {
                    // No longer in template editing mode. 
                    // This will fire the OnTemplateModeChanged notification
                    _currentTemplateGroup = null; 
                    RaiseTemplateModeChanged(); 

                    // When not switching from one template frame to another and it is the 
                    // outer most designer being switched out of template editing, then
                    // update its design-time html:

                    if (!fNested) { 
                        UpdateDesignTimeHtml();
 
                        // Invalidate the type descriptor so that proper filtering of properties 
                        // is done when exiting template mode.
                        TypeDescriptor.Refresh(Component); 

                        IWebFormsDocumentService wfServices = (IWebFormsDocumentService)GetService(typeof(IWebFormsDocumentService));
                        if (wfServices != null) {
                            wfServices.UpdateSelection(); 
                        }
                    } 
                } 
            }
            catch { 
            }
        }

        private void ExitTemplateModeInternal(bool fSwitchingTemplates, bool fNested, bool fSave) { 
#pragma warning disable 618
            ExitTemplateMode(fSwitchingTemplates, fNested, fSave); 
#pragma warning restore 618 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetCachedTemplateEditingVerbs"]/*' />
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        protected abstract TemplateEditingVerb[] GetCachedTemplateEditingVerbs();
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetPersistenceContent"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets the HTML to be persisted for the content present within the associated server control runtime.
        ///    </para> 
        /// </devdoc>
        internal override string GetPersistInnerHtmlInternal() {
            // Save the currently active template editing frame when in template mode.
            if (InTemplateModeInternal) { 
                SaveActiveTemplateEditingFrame();
            } 
 
            // Call the base implementation to do the actual persistence.
            string persistHTML = base.GetPersistInnerHtmlInternal(); 

            //
            if (InTemplateModeInternal) {
                IsDirtyInternal = true; 
            }
 
            return persistHTML; 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetTemplateContainerDataItemProperty"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the template's container's data item property. 
        ///    </para>
        /// </devdoc> 
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public virtual string GetTemplateContainerDataItemProperty(string templateName) {
            return String.Empty; 
        }

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetTemplateContainerDataSource"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the template's container's data source. 
        ///    </para> 
        /// </devdoc>
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public virtual IEnumerable GetTemplateContainerDataSource(string templateName) {
            return null;
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetTemplateContent"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets the template's content.
        ///    </para> 
        /// </devdoc>
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        public abstract string GetTemplateContent(ITemplateEditingFrame editingFrame, string templateName, out bool allowEditing);
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetTemplateEditingVerbs"]/*' />
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public TemplateEditingVerb[] GetTemplateEditingVerbs() { 
            ITemplateEditingService teService =
                (ITemplateEditingService)GetService(typeof(ITemplateEditingService)); 
            Debug.Assert(teService != null, "Host that does not implement ITemplateEditingService is asking for template verbs");
            if (teService == null) {
                return null;
            } 

            TemplateEditingVerbCollection verbs = GetTemplateEditingVerbsInternal(); 
            TemplateEditingVerb[] verbArray = new TemplateEditingVerb[verbs.Count]; 

            ((ICollection)verbs).CopyTo(verbArray, 0); 
            return verbArray;
        }

#pragma warning disable 618 
        private TemplateEditingVerbCollection GetTemplateEditingVerbsInternal() {
            TemplateEditingVerbCollection verbs = new TemplateEditingVerbCollection(); 
            TemplateEditingVerb[] templateVerbs = GetCachedTemplateEditingVerbs(); 

            if ((templateVerbs != null) && (templateVerbs.Length > 0)) { 
                for (int i = 0; i < templateVerbs.Length; i++) {
                    if ((_currentTemplateGroup != null) && (_currentTemplateGroup.Verb == templateVerbs[i])) {
                        templateVerbs[i].Checked = true;
                    } 
                    else {
                        templateVerbs[i].Checked = false; 
                    } 

                    verbs.Add(templateVerbs[i]); 
                }

            }
 
            return verbs;
        } 
#pragma warning restore 618 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetTemplateFromText"]/*' /> 
        protected ITemplate GetTemplateFromText(string text) {
            return GetTemplateFromText(text, null);
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetTemplateFromText1"]/*' />
        /// <internalonly/> 
        internal ITemplate GetTemplateFromText(string text, ITemplate currentTemplate) { 
            if ((text == null) || (text.Length == 0)) {
                throw new ArgumentNullException("text"); 
            }
            IDesignerHost host = (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost));
            Debug.Assert(host != null, "no IDesignerHost!");
 
            try {
                ITemplate newTemplate = ControlParser.ParseTemplate(host, text); 
                if (newTemplate != null) { 
                    return newTemplate;
                } 
            }
            catch {
            }
            return currentTemplate; 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetTemplatePropertyParentType"]/*' /> 
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual Type GetTemplatePropertyParentType(string templateName) { 
            return Component.GetType();
        }

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetTextFromTemplate"]/*' /> 
        protected string GetTextFromTemplate(ITemplate template) {
            if (template == null) { 
                throw new ArgumentNullException("template"); 
            }
 
            Debug.Assert(template is TemplateBuilder, "Unexpected ITemplate implementation");
            if (template is TemplateBuilder) {
                return ((TemplateBuilder)template).Text;
            } 
            return String.Empty;
        } 
 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.Initialize"]/*' /> 
        public override void Initialize(IComponent component) {
            base.Initialize(component);

            if (View != null) { 
                View.ViewEvent += new ViewEventHandler(OnViewEvent);
 
                View.SetFlags(ViewFlags.TemplateEditing, true); 
            }
        } 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.OnBehaviorAttached"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Notification that is called when the behavior is attached to the designer.
        ///    </para> 
        /// </devdoc> 
        [Obsolete("The recommended alternative is ControlDesigner.Tag. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected override void OnBehaviorAttached() { 
            if (InTemplateModeInternal) {
                //
                Debug.Assert(ActiveTemplateEditingFrame != null, "Valid template frame should be present when in template mode!");
                ActiveTemplateEditingFrame.Close(false); 
                ActiveTemplateEditingFrame.Dispose();
                _currentTemplateGroup = null; 
 
                // Refresh the type descriptor so the properties are up to date when switching views.
                TypeDescriptor.Refresh(Component); 
            }

            // Call the base implementation.
            base.OnBehaviorAttached(); 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.OnComponentChanged"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Delegate to handle the component changed event.
        ///    </para>
        /// </devdoc>
        public override void OnComponentChanged(object sender, ComponentChangedEventArgs ce) { 
            // Call the base class implementation first.
            base.OnComponentChanged(sender, ce); 
 
            if (InTemplateModeInternal) {
                if ((ce.Member != null) && (ce.NewValue != null) && ce.Member.Name.Equals("ID")) { 
                    // If the ID property changes when in template mode, update it in the
                    // active template editing frame.

#pragma warning disable 618 
                    Debug.Assert(ActiveTemplateEditingFrame != null, "Valid template frame should be present when in template mode");
                    ActiveTemplateEditingFrame.UpdateControlName(ce.NewValue.ToString()); 
#pragma warning restore 618 
                }
            } 
        }

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.OnSetParent"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Notification that is called when the associated control is parented. 
        ///    </para> 
        /// </devdoc>
        public override void OnSetParent() { 
            Control control = (Control)Component;
            Debug.Assert(control.Parent != null, "Valid parent should be present!");

            bool enable = false; 

            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            Debug.Assert(host != null); 

#pragma warning disable 618 
            ITemplateEditingService teService = (ITemplateEditingService)host.GetService(typeof(ITemplateEditingService));
#pragma warning restore 618
            if (teService != null) {
                enable = true; 

                Control parent = control.Parent; 
                Control page = control.Page; 

                while ((parent != null) && (parent != page)) { 
                    IDesigner designer = host.GetDesigner(parent);
                    TemplatedControlDesigner templatedDesigner = designer as TemplatedControlDesigner;

                    if (templatedDesigner != null) { 
                        enable = teService.SupportsNestedTemplateEditing;
                        break; 
                    } 

                    parent = parent.Parent; 
                }
            }

            EnableTemplateEditing(enable); 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.OnTemplateEditingVerbInvoked"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Delegate to handle template verb invocation.
        ///    </para>
        /// </devdoc>
#pragma warning disable 618 
        private void OnTemplateEditingVerbInvoked(object sender, EventArgs e) {
            Debug.Assert(sender is TemplateEditingVerb, "Template verb execution is not sent by TemplateEditingVerb"); 
            TemplateEditingVerb verb = (TemplateEditingVerb)sender; 

            if (verb.EditingFrame == null) { 
                verb.EditingFrame = CreateTemplateEditingFrame(verb);
                Debug.Assert(verb.EditingFrame != null, "CreateTemplateEditingFrame returned null!");
            }
 
            if (verb.EditingFrame != null) {
                verb.EditingFrame.Verb = verb; 
                EnterTemplateModeInternal(verb.EditingFrame); 
            }
        } 
#pragma warning restore 618

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.OnTemplateModeChanged"]/*' />
        protected virtual void OnTemplateModeChanged() { 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.OnTemplateModeChanged"]/*' /> 
        internal void OnTemplateModeChangedInternal(TemplateModeChangedEventArgs e) {
            TemplateGroup newGroup = e.NewTemplateGroup; 
            if (newGroup != null)  {
                if (_currentTemplateGroup != newGroup) {
                    EnterTemplateModeInternal(((TemplatedControlDesignerTemplateGroup)newGroup).Frame);
                } 
            }
            else { 
                // Exiting template mode since the new group is null 
                ExitTemplateModeInternal(false, false, true);
            } 
        }

        private void OnViewEvent(object sender, ViewEventArgs e) {
            if (e.EventType == ViewEvent.TemplateModeChanged) { 
                OnTemplateModeChangedInternal((TemplateModeChangedEventArgs)e.EventArgs);
            } 
        } 

        private void RaiseTemplateModeChanged() { 
            if (BehaviorInternal != null) {
#pragma warning disable 618
                ((IControlDesignerBehavior)BehaviorInternal).OnTemplateModeChanged();
#pragma warning restore 618 
            }
 
            OnTemplateModeChanged(); 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.SaveActiveTemplateEditingFrame"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Saves the active template frame. 
        ///    </para>
        /// </devdoc> 
        protected void SaveActiveTemplateEditingFrame() { 
#pragma warning disable 618
            Debug.Assert(InTemplateModeInternal, "SaveActiveTemplate should be called only when in template mode"); 
            Debug.Assert(ActiveTemplateEditingFrame != null, "An active template frame should be present in SaveActiveTemplate");

            ActiveTemplateEditingFrame.Save();
#pragma warning restore 618 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.SetTemplateContent"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Sets the template content to the specified content.
        ///    </para>
        /// </devdoc>
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public abstract void SetTemplateContent(ITemplateEditingFrame editingFrame, string templateName, string templateContent);
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.UpdateDesignTimeHtml"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Updates the design-time HTML.
        ///    </para>
        /// </devdoc>
        public override void UpdateDesignTimeHtml() { 
            if (!InTemplateModeInternal) {
                base.UpdateDesignTimeHtml(); 
            } 

            // 
        }

#pragma warning disable 618
        private class TemplatedControlDesignerTemplateDefinition : TemplateDefinition { 
            private TemplatedControlDesigner _parent;
            private ITemplateEditingFrame _frame; 
 
            public TemplatedControlDesignerTemplateDefinition(string name, Style style, TemplatedControlDesigner parent, ITemplateEditingFrame frame) : base(parent, name, parent.Component, name, style) {
                _parent = parent; 
                _frame = frame;

                Properties[typeof(Control)] = (Control)_parent.Component;
            } 

            public override bool AllowEditing { 
                get { 
                    bool allowEditing;
                    _parent.GetTemplateContent(_frame, Name, out allowEditing); 
                    return allowEditing;
                }
            }
 
            public override string Content {
                get { 
                    bool allowEditing; 
                    return _parent.GetTemplateContent(_frame, Name, out allowEditing);
                } 
                set {
                    _parent.SetTemplateContent(_frame, Name, value);
                    _parent.Tag.SetDirty(true);
 
                    _parent.UpdateDesignTimeHtml();
                } 
            } 
        }
#pragma warning restore 618 

#pragma warning disable 618
        private class TemplatedControlDesignerTemplateGroup : TemplateGroup {
            private ITemplateEditingFrame _frame; 
            private TemplateEditingVerb _verb;
 
            public TemplatedControlDesignerTemplateGroup(TemplateEditingVerb verb, ITemplateEditingFrame frame) 
                : base(verb.Text, frame.ControlStyle) {
                _frame = frame; 
                _verb = verb;
            }

            public ITemplateEditingFrame Frame { 
                get {
                    return _frame; 
                } 
            }
 
            public TemplateEditingVerb Verb {
                get {
                    return _verb;
                } 
            }
        } 
#pragma warning restore 618 

#pragma warning disable 618 
        private class TemplateEditingVerbCollection : IList {
            private ArrayList _list;

            public TemplateEditingVerbCollection() { 
            }
 
            internal TemplateEditingVerbCollection(TemplateEditingVerb[] verbs) { 
                for (int i = 0; i < verbs.Length; i++) {
                    Add(verbs[i]); 
                }
            }

            public int Count { 
                get {
                    return InternalList.Count; 
                } 
            }
 
            private ArrayList InternalList {
                get {
                    if (_list == null) {
                        _list = new ArrayList(); 
                    }
                    return _list; 
                } 
            }
 
            public TemplateEditingVerb this[int index] {
                get {
                    return (TemplateEditingVerb)InternalList[index];
                } 
                set {
                    InternalList[index] = value; 
                } 
            }
 
            public int Add(TemplateEditingVerb verb) {
                return InternalList.Add(verb);
            }
 
            public void Clear() {
                InternalList.Clear(); 
            } 

            public bool Contains(TemplateEditingVerb verb) { 
                return InternalList.Contains(verb);
            }

            public int IndexOf(TemplateEditingVerb verb) { 
                return InternalList.IndexOf(verb);
            } 
 
            public void Insert(int index, TemplateEditingVerb verb) {
                InternalList.Insert(index, verb); 
            }

            public void Remove(TemplateEditingVerb verb) {
                InternalList.Remove(verb); 
            }
 
            public void RemoveAt(int index) { 
                InternalList.RemoveAt(index);
            } 

            #region IList implementation
            int ICollection.Count {
                get { 
                    return Count;
                } 
            } 

            bool IList.IsFixedSize { 
                get {
                    return InternalList.IsFixedSize;
                }
            } 

            bool IList.IsReadOnly { 
                get { 
                    return InternalList.IsReadOnly;
                } 
            }

            bool ICollection.IsSynchronized {
                get { 
                    return InternalList.IsSynchronized;
                } 
            } 

            object ICollection.SyncRoot { 
                get {
                    return InternalList.SyncRoot;
                }
            } 

            object IList.this[int index] { 
                get { 
                    return this[index];
                } 
                set {
                    if (!(value is TemplateEditingVerb)) {
                        throw new ArgumentException();
                    } 

                    this[index] = (TemplateEditingVerb)value; 
                } 
            }
 
            int IList.Add(object o) {
                if (!(o is TemplateEditingVerb)) {
                    throw new ArgumentException();
                } 

                return Add((TemplateEditingVerb)o); 
            } 

            void IList.Clear() { 
                Clear();
            }

            bool IList.Contains(object o) { 
                if (!(o is TemplateEditingVerb)) {
                    throw new ArgumentException(); 
                } 

                return Contains((TemplateEditingVerb)o); 
            }

            void ICollection.CopyTo(Array array, int index) {
                InternalList.CopyTo(array, index); 
            }
 
            IEnumerator IEnumerable.GetEnumerator() { 
                return InternalList.GetEnumerator();
            } 

            int IList.IndexOf(object o) {
                if (!(o is TemplateEditingVerb)) {
                    throw new ArgumentException(); 
                }
 
                return IndexOf((TemplateEditingVerb)o); 
            }
 
            void IList.Insert(int index, object o) {
                if (!(o is TemplateEditingVerb)) {
                    throw new ArgumentException();
                } 

                Insert(index, (TemplateEditingVerb)o); 
            } 

            void IList.Remove(object o) { 
                if (!(o is TemplateEditingVerb)) {
                    throw new ArgumentException();
                }
 
                Remove((TemplateEditingVerb)o);
            } 
 
            void IList.RemoveAt(int index) {
                RemoveAt(index); 
            }

            #endregion
        } 
#pragma warning restore 618
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TemplatedControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
 
    using System.Design;
    using System.Diagnostics; 

    using System;
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.Web.UI; 
    using System.Web.UI.Design; 
    using System.Web.UI.WebControls;
    using System.ComponentModel.Design; 

    /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner"]/*' />
    /// <devdoc>
    ///    <para>Provides a base class for all server control designers that are template-based.</para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public abstract class TemplatedControlDesigner : ControlDesigner { 

        private bool                    enableTemplateEditing;          // True to enable template editing, and false otherwise. 
        private TemplatedControlDesignerTemplateGroup _currentTemplateGroup;
        private IDictionary _templateGroupTable;

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.TemplatedControlDesigner"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of the <see cref='System.Web.UI.Design.TemplatedControlDesigner'/> 
        ///       class.
        ///    </para> 
        /// </devdoc>
        public TemplatedControlDesigner() {
            enableTemplateEditing = true;
        } 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.ActiveTemplateEditingFrame"]/*' /> 
        /// <devdoc> 
        ///     The currently active template frame object (will be null when not in template mode).
        /// </devdoc> 
        [Obsolete("Use of this property is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        public ITemplateEditingFrame ActiveTemplateEditingFrame {
            get {
                if (_currentTemplateGroup != null) { 
                    return _currentTemplateGroup.Frame;
                } 
                return null; 
            }
        } 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.CanEnterTemplateMode"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Whether or not this designer will allow editing of templates.
        ///    </para> 
        /// </devdoc> 
        public bool CanEnterTemplateMode {
            get { 
                return enableTemplateEditing;
            }
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.DataBindingsEnabled"]/*' />
        /// <internalonly/> 
        protected override bool DataBindingsEnabled { 
            get {
                if (InTemplateModeInternal && HidePropertiesInTemplateMode) { 
                    return false;
                }
                return base.DataBindingsEnabled;
            } 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.InTemplateMode"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Whether or not the designer document is in template mode.
        ///    </para>
        /// </devdoc>
        [Obsolete("The recommended alternative is System.Web.UI.Design.ControlDesigner.InTemplateMode. http://go.microsoft.com/fwlink/?linkid=14202")] 
        public new bool InTemplateMode {
            get { 
                return (_currentTemplateGroup != null); 
            }
        } 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.InTemplateModeInternal"]/*' />
        internal bool InTemplateModeInternal {
            get { 
#pragma warning disable 618
                return InTemplateMode; 
#pragma warning restore 618 
            }
        } 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.TemplateEditingVerbHandler"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Verb execution handler for opening the template frames and entering template mode.
        ///    </para> 
        /// </devdoc> 
        internal EventHandler TemplateEditingVerbHandler {
            get { 
                return new EventHandler(this.OnTemplateEditingVerbInvoked);
            }
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.TemplateGroups"]/*' />
        public override TemplateGroupCollection TemplateGroups { 
            get { 
                TemplateGroupCollection groups = base.TemplateGroups;
 
                TemplateGroupTable.Clear();
#pragma warning disable 618
                TemplateEditingVerbCollection verbs = GetTemplateEditingVerbsInternal();
                foreach (TemplateEditingVerb verb in verbs) { 
#pragma warning restore 618
                    if (!verb.Enabled || !verb.Visible) { 
                        continue; 
                    }
 
#pragma warning disable 618
                    ITemplateEditingFrame frame = CreateTemplateEditingFrame(verb);
#pragma warning restore 618
                    frame.Verb = verb; 

                    TemplateGroup group = new TemplatedControlDesignerTemplateGroup(verb, frame); 
 
                    bool hasStyles = (frame.TemplateStyles != null);
                    for (int j = 0; j < frame.TemplateNames.Length; j++) { 
                        Style templateStyle = hasStyles ? frame.TemplateStyles[j] : null;
                        TemplatedControlDesignerTemplateDefinition template =
                            new TemplatedControlDesignerTemplateDefinition(frame.TemplateNames[j], templateStyle, this, frame);
 
                        // All v1 templates support databinding
                        template.SupportsDataBinding = true; 
 
                        group.AddTemplateDefinition(template);
                    } 

                    groups.Add(group);
                    TemplateGroupTable[frame] = group;
                } 

                return groups; 
            } 
        }
 
        private IDictionary TemplateGroupTable {
            get {
                if (_templateGroupTable == null) {
                    _templateGroupTable = new HybridDictionary(); 
                }
 
                return _templateGroupTable; 
            }
        } 


        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.CreateTemplateEditingFrame"]/*' />
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        protected abstract ITemplateEditingFrame CreateTemplateEditingFrame(TemplateEditingVerb verb);
 
        private void EnableTemplateEditing(bool enable) { 
            enableTemplateEditing = enable;
            // 
        }

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.EnterTemplateMode"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Opens a particular template frame object for editing in the designer. 
        ///    </para> 
        /// </devdoc>
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public void EnterTemplateMode(ITemplateEditingFrame newTemplateEditingFrame) {
            Debug.Assert(newTemplateEditingFrame != null, "New template frame passed in is null!");

            // Return immediately when trying to open (again) the currently active template frame. 
            if (ActiveTemplateEditingFrame == newTemplateEditingFrame) {
                return; 
            } 

            if (BehaviorInternal != null) { 
                Debug.Assert(BehaviorInternal is IControlDesignerBehavior, "Invalid element.");
                IControlDesignerBehavior behavior = (IControlDesignerBehavior)BehaviorInternal;

                try { 
                    bool switchingTemplates = false;
                    if (InTemplateModeInternal) { 
                        // This is the case of switching from template frame to another. 
                        switchingTemplates = true;
                        ExitTemplateModeInternal(switchingTemplates, /*fNested*/ false, /*fSave*/ true); 
                    }
                    else {
                        // Clear the design time HTML when entering template mode from read-only/preview mode.
                        if (behavior != null) { 
                            behavior.DesignTimeHtml = String.Empty;
                        } 
                    } 

                    // The designer is now in template editing mode. 
                    _currentTemplateGroup = (TemplatedControlDesignerTemplateGroup)TemplateGroupTable[newTemplateEditingFrame];
                    if (_currentTemplateGroup == null) {
                        _currentTemplateGroup = new TemplatedControlDesignerTemplateGroup(null, newTemplateEditingFrame);
                    } 

                    if (!switchingTemplates) { 
                        RaiseTemplateModeChanged(); 
                    }
 
                    // Open the new template frame and make it visible.
                    ActiveTemplateEditingFrame.Open();

                    // Mark the designer as dirty when in template mode. 
                    IsDirtyInternal = true;
 
                    // Invalidate the type descriptor so that proper filtering of properties 
                    // is done when entering template mode.
                    TypeDescriptor.Refresh(Component); 
                }
                catch {
                }
 
                IWebFormsDocumentService wfServices = (IWebFormsDocumentService)GetService(typeof(IWebFormsDocumentService));
                if (wfServices != null) { 
                    wfServices.UpdateSelection(); 
                }
            } 
        }

#pragma warning disable 618
        private void EnterTemplateModeInternal(ITemplateEditingFrame newTemplateEditingFrame) { 
            EnterTemplateMode(newTemplateEditingFrame);
        } 
#pragma warning restore 618 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.ExitNestedTemplates"]/*' /> 
        /// <devdoc>
        ///     This method ensures that for a particular templated control designer when exiting
        ///     its template mode handles nested templates (if any). This is done by exiting the
        ///     inner most template frames first before exiting itself. Inside-Out Model. 
        /// </devdoc>
        private void ExitNestedTemplates(bool fSave) { 
            try { 
                IComponent component = ViewControl;
                IDesignerHost host = (IDesignerHost)component.Site.GetService(typeof(IDesignerHost)); 

                ControlCollection children = ((Control)component).Controls;
                for (int i = 0; i < children.Count; i++) {
                    IDesigner designer = host.GetDesigner((IComponent)children[i]); 
                    if (designer is TemplatedControlDesigner) {
                        TemplatedControlDesigner innerDesigner = (TemplatedControlDesigner)designer; 
                        if (innerDesigner.InTemplateModeInternal) { 
                            innerDesigner.ExitTemplateModeInternal(/*fSwitchingTemplates*/ false, /*fNested*/ true, /*fSave*/ fSave);
                        } 
                    }
                }
            }
            catch (Exception ex) { 
                Debug.Fail(ex.ToString());
            } 
        } 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.ExitTemplateMode"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Closes the currently active template editing frame after saving any relevant changes.
        ///    </para> 
        /// </devdoc>
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public void ExitTemplateMode(bool fSwitchingTemplates, bool fNested, bool fSave) { 
            Debug.Assert(ActiveTemplateEditingFrame != null, "Invalid current template frame!");
 
            try {
                // First let the inner/nested designers handle exiting of their template mode.
                // Note: This has to be done inside-out in order to ensure that the changes
                // made in a particular template are saved before its immediate outer level 
                // control designer saves its children.
                ExitNestedTemplates(fSave); 
 
                // Save the current contents of all the templates within the active frame, and
                // close the frame by removing it from the tree. 
                ActiveTemplateEditingFrame.Close(fSave);

                if (!fSwitchingTemplates) {
                    // No longer in template editing mode. 
                    // This will fire the OnTemplateModeChanged notification
                    _currentTemplateGroup = null; 
                    RaiseTemplateModeChanged(); 

                    // When not switching from one template frame to another and it is the 
                    // outer most designer being switched out of template editing, then
                    // update its design-time html:

                    if (!fNested) { 
                        UpdateDesignTimeHtml();
 
                        // Invalidate the type descriptor so that proper filtering of properties 
                        // is done when exiting template mode.
                        TypeDescriptor.Refresh(Component); 

                        IWebFormsDocumentService wfServices = (IWebFormsDocumentService)GetService(typeof(IWebFormsDocumentService));
                        if (wfServices != null) {
                            wfServices.UpdateSelection(); 
                        }
                    } 
                } 
            }
            catch { 
            }
        }

        private void ExitTemplateModeInternal(bool fSwitchingTemplates, bool fNested, bool fSave) { 
#pragma warning disable 618
            ExitTemplateMode(fSwitchingTemplates, fNested, fSave); 
#pragma warning restore 618 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetCachedTemplateEditingVerbs"]/*' />
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        protected abstract TemplateEditingVerb[] GetCachedTemplateEditingVerbs();
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetPersistenceContent"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets the HTML to be persisted for the content present within the associated server control runtime.
        ///    </para> 
        /// </devdoc>
        internal override string GetPersistInnerHtmlInternal() {
            // Save the currently active template editing frame when in template mode.
            if (InTemplateModeInternal) { 
                SaveActiveTemplateEditingFrame();
            } 
 
            // Call the base implementation to do the actual persistence.
            string persistHTML = base.GetPersistInnerHtmlInternal(); 

            //
            if (InTemplateModeInternal) {
                IsDirtyInternal = true; 
            }
 
            return persistHTML; 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetTemplateContainerDataItemProperty"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the template's container's data item property. 
        ///    </para>
        /// </devdoc> 
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public virtual string GetTemplateContainerDataItemProperty(string templateName) {
            return String.Empty; 
        }

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetTemplateContainerDataSource"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Gets the template's container's data source. 
        ///    </para> 
        /// </devdoc>
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public virtual IEnumerable GetTemplateContainerDataSource(string templateName) {
            return null;
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetTemplateContent"]/*' />
        /// <devdoc> 
        ///    <para> 
        ///       Gets the template's content.
        ///    </para> 
        /// </devdoc>
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        public abstract string GetTemplateContent(ITemplateEditingFrame editingFrame, string templateName, out bool allowEditing);
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetTemplateEditingVerbs"]/*' />
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public TemplateEditingVerb[] GetTemplateEditingVerbs() { 
            ITemplateEditingService teService =
                (ITemplateEditingService)GetService(typeof(ITemplateEditingService)); 
            Debug.Assert(teService != null, "Host that does not implement ITemplateEditingService is asking for template verbs");
            if (teService == null) {
                return null;
            } 

            TemplateEditingVerbCollection verbs = GetTemplateEditingVerbsInternal(); 
            TemplateEditingVerb[] verbArray = new TemplateEditingVerb[verbs.Count]; 

            ((ICollection)verbs).CopyTo(verbArray, 0); 
            return verbArray;
        }

#pragma warning disable 618 
        private TemplateEditingVerbCollection GetTemplateEditingVerbsInternal() {
            TemplateEditingVerbCollection verbs = new TemplateEditingVerbCollection(); 
            TemplateEditingVerb[] templateVerbs = GetCachedTemplateEditingVerbs(); 

            if ((templateVerbs != null) && (templateVerbs.Length > 0)) { 
                for (int i = 0; i < templateVerbs.Length; i++) {
                    if ((_currentTemplateGroup != null) && (_currentTemplateGroup.Verb == templateVerbs[i])) {
                        templateVerbs[i].Checked = true;
                    } 
                    else {
                        templateVerbs[i].Checked = false; 
                    } 

                    verbs.Add(templateVerbs[i]); 
                }

            }
 
            return verbs;
        } 
#pragma warning restore 618 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetTemplateFromText"]/*' /> 
        protected ITemplate GetTemplateFromText(string text) {
            return GetTemplateFromText(text, null);
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetTemplateFromText1"]/*' />
        /// <internalonly/> 
        internal ITemplate GetTemplateFromText(string text, ITemplate currentTemplate) { 
            if ((text == null) || (text.Length == 0)) {
                throw new ArgumentNullException("text"); 
            }
            IDesignerHost host = (IDesignerHost)Component.Site.GetService(typeof(IDesignerHost));
            Debug.Assert(host != null, "no IDesignerHost!");
 
            try {
                ITemplate newTemplate = ControlParser.ParseTemplate(host, text); 
                if (newTemplate != null) { 
                    return newTemplate;
                } 
            }
            catch {
            }
            return currentTemplate; 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetTemplatePropertyParentType"]/*' /> 
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual Type GetTemplatePropertyParentType(string templateName) { 
            return Component.GetType();
        }

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.GetTextFromTemplate"]/*' /> 
        protected string GetTextFromTemplate(ITemplate template) {
            if (template == null) { 
                throw new ArgumentNullException("template"); 
            }
 
            Debug.Assert(template is TemplateBuilder, "Unexpected ITemplate implementation");
            if (template is TemplateBuilder) {
                return ((TemplateBuilder)template).Text;
            } 
            return String.Empty;
        } 
 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.Initialize"]/*' /> 
        public override void Initialize(IComponent component) {
            base.Initialize(component);

            if (View != null) { 
                View.ViewEvent += new ViewEventHandler(OnViewEvent);
 
                View.SetFlags(ViewFlags.TemplateEditing, true); 
            }
        } 

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.OnBehaviorAttached"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Notification that is called when the behavior is attached to the designer.
        ///    </para> 
        /// </devdoc> 
        [Obsolete("The recommended alternative is ControlDesigner.Tag. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected override void OnBehaviorAttached() { 
            if (InTemplateModeInternal) {
                //
                Debug.Assert(ActiveTemplateEditingFrame != null, "Valid template frame should be present when in template mode!");
                ActiveTemplateEditingFrame.Close(false); 
                ActiveTemplateEditingFrame.Dispose();
                _currentTemplateGroup = null; 
 
                // Refresh the type descriptor so the properties are up to date when switching views.
                TypeDescriptor.Refresh(Component); 
            }

            // Call the base implementation.
            base.OnBehaviorAttached(); 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.OnComponentChanged"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Delegate to handle the component changed event.
        ///    </para>
        /// </devdoc>
        public override void OnComponentChanged(object sender, ComponentChangedEventArgs ce) { 
            // Call the base class implementation first.
            base.OnComponentChanged(sender, ce); 
 
            if (InTemplateModeInternal) {
                if ((ce.Member != null) && (ce.NewValue != null) && ce.Member.Name.Equals("ID")) { 
                    // If the ID property changes when in template mode, update it in the
                    // active template editing frame.

#pragma warning disable 618 
                    Debug.Assert(ActiveTemplateEditingFrame != null, "Valid template frame should be present when in template mode");
                    ActiveTemplateEditingFrame.UpdateControlName(ce.NewValue.ToString()); 
#pragma warning restore 618 
                }
            } 
        }

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.OnSetParent"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Notification that is called when the associated control is parented. 
        ///    </para> 
        /// </devdoc>
        public override void OnSetParent() { 
            Control control = (Control)Component;
            Debug.Assert(control.Parent != null, "Valid parent should be present!");

            bool enable = false; 

            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost)); 
            Debug.Assert(host != null); 

#pragma warning disable 618 
            ITemplateEditingService teService = (ITemplateEditingService)host.GetService(typeof(ITemplateEditingService));
#pragma warning restore 618
            if (teService != null) {
                enable = true; 

                Control parent = control.Parent; 
                Control page = control.Page; 

                while ((parent != null) && (parent != page)) { 
                    IDesigner designer = host.GetDesigner(parent);
                    TemplatedControlDesigner templatedDesigner = designer as TemplatedControlDesigner;

                    if (templatedDesigner != null) { 
                        enable = teService.SupportsNestedTemplateEditing;
                        break; 
                    } 

                    parent = parent.Parent; 
                }
            }

            EnableTemplateEditing(enable); 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.OnTemplateEditingVerbInvoked"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Delegate to handle template verb invocation.
        ///    </para>
        /// </devdoc>
#pragma warning disable 618 
        private void OnTemplateEditingVerbInvoked(object sender, EventArgs e) {
            Debug.Assert(sender is TemplateEditingVerb, "Template verb execution is not sent by TemplateEditingVerb"); 
            TemplateEditingVerb verb = (TemplateEditingVerb)sender; 

            if (verb.EditingFrame == null) { 
                verb.EditingFrame = CreateTemplateEditingFrame(verb);
                Debug.Assert(verb.EditingFrame != null, "CreateTemplateEditingFrame returned null!");
            }
 
            if (verb.EditingFrame != null) {
                verb.EditingFrame.Verb = verb; 
                EnterTemplateModeInternal(verb.EditingFrame); 
            }
        } 
#pragma warning restore 618

        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.OnTemplateModeChanged"]/*' />
        protected virtual void OnTemplateModeChanged() { 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.OnTemplateModeChanged"]/*' /> 
        internal void OnTemplateModeChangedInternal(TemplateModeChangedEventArgs e) {
            TemplateGroup newGroup = e.NewTemplateGroup; 
            if (newGroup != null)  {
                if (_currentTemplateGroup != newGroup) {
                    EnterTemplateModeInternal(((TemplatedControlDesignerTemplateGroup)newGroup).Frame);
                } 
            }
            else { 
                // Exiting template mode since the new group is null 
                ExitTemplateModeInternal(false, false, true);
            } 
        }

        private void OnViewEvent(object sender, ViewEventArgs e) {
            if (e.EventType == ViewEvent.TemplateModeChanged) { 
                OnTemplateModeChangedInternal((TemplateModeChangedEventArgs)e.EventArgs);
            } 
        } 

        private void RaiseTemplateModeChanged() { 
            if (BehaviorInternal != null) {
#pragma warning disable 618
                ((IControlDesignerBehavior)BehaviorInternal).OnTemplateModeChanged();
#pragma warning restore 618 
            }
 
            OnTemplateModeChanged(); 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.SaveActiveTemplateEditingFrame"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Saves the active template frame. 
        ///    </para>
        /// </devdoc> 
        protected void SaveActiveTemplateEditingFrame() { 
#pragma warning disable 618
            Debug.Assert(InTemplateModeInternal, "SaveActiveTemplate should be called only when in template mode"); 
            Debug.Assert(ActiveTemplateEditingFrame != null, "An active template frame should be present in SaveActiveTemplate");

            ActiveTemplateEditingFrame.Save();
#pragma warning restore 618 
        }
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.SetTemplateContent"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Sets the template content to the specified content.
        ///    </para>
        /// </devdoc>
        [Obsolete("Use of this method is not recommended because template editing is handled in ControlDesigner. To support template editing expose template data in the TemplateGroups property and call SetViewFlags(ViewFlags.TemplateEditing, true). http://go.microsoft.com/fwlink/?linkid=14202")] 
        public abstract void SetTemplateContent(ITemplateEditingFrame editingFrame, string templateName, string templateContent);
 
        /// <include file='doc\TemplatedControlDesigner.uex' path='docs/doc[@for="TemplatedControlDesigner.UpdateDesignTimeHtml"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Updates the design-time HTML.
        ///    </para>
        /// </devdoc>
        public override void UpdateDesignTimeHtml() { 
            if (!InTemplateModeInternal) {
                base.UpdateDesignTimeHtml(); 
            } 

            // 
        }

#pragma warning disable 618
        private class TemplatedControlDesignerTemplateDefinition : TemplateDefinition { 
            private TemplatedControlDesigner _parent;
            private ITemplateEditingFrame _frame; 
 
            public TemplatedControlDesignerTemplateDefinition(string name, Style style, TemplatedControlDesigner parent, ITemplateEditingFrame frame) : base(parent, name, parent.Component, name, style) {
                _parent = parent; 
                _frame = frame;

                Properties[typeof(Control)] = (Control)_parent.Component;
            } 

            public override bool AllowEditing { 
                get { 
                    bool allowEditing;
                    _parent.GetTemplateContent(_frame, Name, out allowEditing); 
                    return allowEditing;
                }
            }
 
            public override string Content {
                get { 
                    bool allowEditing; 
                    return _parent.GetTemplateContent(_frame, Name, out allowEditing);
                } 
                set {
                    _parent.SetTemplateContent(_frame, Name, value);
                    _parent.Tag.SetDirty(true);
 
                    _parent.UpdateDesignTimeHtml();
                } 
            } 
        }
#pragma warning restore 618 

#pragma warning disable 618
        private class TemplatedControlDesignerTemplateGroup : TemplateGroup {
            private ITemplateEditingFrame _frame; 
            private TemplateEditingVerb _verb;
 
            public TemplatedControlDesignerTemplateGroup(TemplateEditingVerb verb, ITemplateEditingFrame frame) 
                : base(verb.Text, frame.ControlStyle) {
                _frame = frame; 
                _verb = verb;
            }

            public ITemplateEditingFrame Frame { 
                get {
                    return _frame; 
                } 
            }
 
            public TemplateEditingVerb Verb {
                get {
                    return _verb;
                } 
            }
        } 
#pragma warning restore 618 

#pragma warning disable 618 
        private class TemplateEditingVerbCollection : IList {
            private ArrayList _list;

            public TemplateEditingVerbCollection() { 
            }
 
            internal TemplateEditingVerbCollection(TemplateEditingVerb[] verbs) { 
                for (int i = 0; i < verbs.Length; i++) {
                    Add(verbs[i]); 
                }
            }

            public int Count { 
                get {
                    return InternalList.Count; 
                } 
            }
 
            private ArrayList InternalList {
                get {
                    if (_list == null) {
                        _list = new ArrayList(); 
                    }
                    return _list; 
                } 
            }
 
            public TemplateEditingVerb this[int index] {
                get {
                    return (TemplateEditingVerb)InternalList[index];
                } 
                set {
                    InternalList[index] = value; 
                } 
            }
 
            public int Add(TemplateEditingVerb verb) {
                return InternalList.Add(verb);
            }
 
            public void Clear() {
                InternalList.Clear(); 
            } 

            public bool Contains(TemplateEditingVerb verb) { 
                return InternalList.Contains(verb);
            }

            public int IndexOf(TemplateEditingVerb verb) { 
                return InternalList.IndexOf(verb);
            } 
 
            public void Insert(int index, TemplateEditingVerb verb) {
                InternalList.Insert(index, verb); 
            }

            public void Remove(TemplateEditingVerb verb) {
                InternalList.Remove(verb); 
            }
 
            public void RemoveAt(int index) { 
                InternalList.RemoveAt(index);
            } 

            #region IList implementation
            int ICollection.Count {
                get { 
                    return Count;
                } 
            } 

            bool IList.IsFixedSize { 
                get {
                    return InternalList.IsFixedSize;
                }
            } 

            bool IList.IsReadOnly { 
                get { 
                    return InternalList.IsReadOnly;
                } 
            }

            bool ICollection.IsSynchronized {
                get { 
                    return InternalList.IsSynchronized;
                } 
            } 

            object ICollection.SyncRoot { 
                get {
                    return InternalList.SyncRoot;
                }
            } 

            object IList.this[int index] { 
                get { 
                    return this[index];
                } 
                set {
                    if (!(value is TemplateEditingVerb)) {
                        throw new ArgumentException();
                    } 

                    this[index] = (TemplateEditingVerb)value; 
                } 
            }
 
            int IList.Add(object o) {
                if (!(o is TemplateEditingVerb)) {
                    throw new ArgumentException();
                } 

                return Add((TemplateEditingVerb)o); 
            } 

            void IList.Clear() { 
                Clear();
            }

            bool IList.Contains(object o) { 
                if (!(o is TemplateEditingVerb)) {
                    throw new ArgumentException(); 
                } 

                return Contains((TemplateEditingVerb)o); 
            }

            void ICollection.CopyTo(Array array, int index) {
                InternalList.CopyTo(array, index); 
            }
 
            IEnumerator IEnumerable.GetEnumerator() { 
                return InternalList.GetEnumerator();
            } 

            int IList.IndexOf(object o) {
                if (!(o is TemplateEditingVerb)) {
                    throw new ArgumentException(); 
                }
 
                return IndexOf((TemplateEditingVerb)o); 
            }
 
            void IList.Insert(int index, object o) {
                if (!(o is TemplateEditingVerb)) {
                    throw new ArgumentException();
                } 

                Insert(index, (TemplateEditingVerb)o); 
            } 

            void IList.Remove(object o) { 
                if (!(o is TemplateEditingVerb)) {
                    throw new ArgumentException();
                }
 
                Remove((TemplateEditingVerb)o);
            } 
 
            void IList.RemoveAt(int index) {
                RemoveAt(index); 
            }

            #endregion
        } 
#pragma warning restore 618
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
