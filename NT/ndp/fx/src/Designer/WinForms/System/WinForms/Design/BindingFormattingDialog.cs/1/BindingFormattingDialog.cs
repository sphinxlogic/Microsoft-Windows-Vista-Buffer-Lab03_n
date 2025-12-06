using System; 
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using System.ComponentModel.Design; 
using System.Globalization;
using System.Windows.Forms; 
using System.Drawing; 
using System.Collections;
using System.Design; 

namespace System.Windows.Forms.Design
{
    internal class BindingFormattingDialog : System.Windows.Forms.Form 
    {
        // we need the context for the HELP service provider 
        private ITypeDescriptorContext context = null; 

        private ControlBindingsCollection bindings; 

        private BindingFormattingWindowsFormsEditorService dataSourcePicker;
        private System.Windows.Forms.Label explanationLabel;
        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel; 
        private System.Windows.Forms.Label propertyLabel;
        private System.Windows.Forms.TreeView propertiesTreeView; 
        private System.Windows.Forms.Label bindingLabel; 
        private System.Windows.Forms.ComboBox bindingUpdateDropDown;
        private System.Windows.Forms.Label updateModeLabel; 
        private FormatControl formatControl1;
        private System.Windows.Forms.TableLayoutPanel okCancelTableLayoutPanel;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton; 
        private bool inLoad = false;
 
        private bool dirty = false; 

        private const int BOUNDIMAGEINDEX = 0; 
        private const int UNBOUNDIMAGEINDEX = 1;

        // static because there will be only one instance of this dialog shown at any time
        private static Bitmap boundBitmap; 
        private static Bitmap unboundBitmap;
 
        // We have to cache the current tree node because the WinForms TreeView control 
        // doesn't tell use what the previous node is when we receive the BeforeSelect event
        private BindingTreeNode currentBindingTreeNode = null; 
        private IDesignerHost host = null;

        public BindingFormattingDialog()
        { 
            InitializeComponent();
        } 
 
        public ControlBindingsCollection Bindings {
            set { 
                this.bindings = value;
            }
        }
 
        private static Bitmap BoundBitmap
        { 
            get 
            {
                if (boundBitmap == null) 
                {
                    boundBitmap = new Bitmap(typeof(BindingFormattingDialog), "BindingFormattingDialog.Bound.bmp");
                    boundBitmap.MakeTransparent(System.Drawing.Color.Red);
                } 
                return boundBitmap;
            } 
        } 

        public ITypeDescriptorContext Context 
        {
            get
            {
                return this.context; 
            }
 
            set 
            {
                this.context = value; 
                dataSourcePicker.Context = value;
            }
        }
 
        public bool Dirty
        { 
            get 
            {
                return this.dirty || this.formatControl1.Dirty; 
            }
        }

        public IDesignerHost Host 
        {
            set 
            { 
                this.host = value;
            } 
        }

        private static Bitmap UnboundBitmap
        { 
            get
            { 
                if (unboundBitmap == null) 
                {
                    unboundBitmap = new Bitmap(typeof(BindingFormattingDialog), "BindingFormattingDialog.Unbound.bmp"); 
                    unboundBitmap.MakeTransparent(System.Drawing.Color.Red);
                }
                return unboundBitmap;
            } 
        }
 
        private void BindingFormattingDialog_Closing(object sender, CancelEventArgs e) { 
            this.currentBindingTreeNode = null;
            this.dataSourcePicker.OwnerComponent = null; 

            this.formatControl1.ResetFormattingInfo();
        }
 
        private void BindingFormattingDialog_HelpRequested(object sender, HelpEventArgs e)
        { 
            BindingFormattingDialog_HelpRequestHandled(); 
            e.Handled = true;
        } 

        private void BindingFormattingDialog_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            BindingFormattingDialog_HelpRequestHandled(); 
            e.Cancel = true;
        } 
 
        private void BindingFormattingDialog_HelpRequestHandled()
        { 
            IHelpService helpService = this.context.GetService(typeof(IHelpService)) as IHelpService;
            if (helpService != null)
            {
                helpService.ShowHelpFromKeyword("vs.BindingFormattingDialog"); 
            }
        } 
 
        private void BindingFormattingDialog_Load(object sender, EventArgs e)
        { 
            this.inLoad = true;

            try
            { 
                //
                // start a new transaction 
                // 
                this.dirty = false;
 
                //
                // get the dialog font
                //
                System.Drawing.Font uiFont = Control.DefaultFont; 
                IUIService uiService = null;
                if (this.bindings.BindableComponent.Site != null) 
                { 
                    uiService = (IUIService) this.bindings.BindableComponent.Site.GetService(typeof(IUIService));
                } 

                if (uiService != null)
                {
                    uiFont = (System.Drawing.Font) uiService.Styles["DialogFont"]; 
                }
 
                this.Font = uiFont; 

                // 
                // push the image list in the tree view
                //
                if (this.propertiesTreeView.ImageList == null)
                { 
                    ImageList il = new ImageList();
                    il.Images.Add(BoundBitmap); 
                    il.Images.Add(UnboundBitmap); 
                    this.propertiesTreeView.ImageList = il;
                } 

                //
                // get the defaultBindingProperty and / or defaultProperty
                // 
                BindingTreeNode defaultBindingPropertyNode = null;
                BindingTreeNode defaultPropertyNode = null; 
                string defaultBindingPropertyName = null; 
                string defaultPropertyName = null;
                AttributeCollection compAttrs = TypeDescriptor.GetAttributes(bindings.BindableComponent); 
                foreach (Attribute attr in compAttrs)
                {
                    if (attr is DefaultBindingPropertyAttribute)
                    { 
                        defaultBindingPropertyName = ((DefaultBindingPropertyAttribute) attr).Name;
                        break; 
                    } 
                    else if (attr is DefaultPropertyAttribute)
                    { 
                        defaultPropertyName = ((DefaultPropertyAttribute) attr).Name;
                    }
                }
 
                //
                // populate the control bindings tree view 
                // 
                this.propertiesTreeView.Nodes.Clear();
                TreeNode commonNode = new TreeNode(SR.GetString(SR.BindingFormattingDialogCommonTreeNode)); 
                TreeNode allNode = new TreeNode(SR.GetString(SR.BindingFormattingDialogAllTreeNode));

                this.propertiesTreeView.Nodes.Add(commonNode);
                this.propertiesTreeView.Nodes.Add(allNode); 

                IBindableComponent bindableComp = bindings.BindableComponent; 
 
                PropertyDescriptorCollection props = TypeDescriptor.GetProperties(bindableComp);
                for (int i = 0; i < props.Count; i ++) 
                {
                    if (props[i].IsReadOnly)
                    {
                        continue; 
                    }
 
                    BindableAttribute bindableAttr = (BindableAttribute) props[i].Attributes[typeof(BindableAttribute)]; 
                    BrowsableAttribute browsable = (BrowsableAttribute) props[i].Attributes[typeof(BrowsableAttribute)];
 
                    // Filter the non Browsable properties but only if they are non Bindable, too.
                    // vsWhidbey 371995
                    if (browsable != null && !browsable.Browsable && (bindableAttr == null || !bindableAttr.Bindable))
                    { 
                        continue;
                    } 
 
                    BindingTreeNode treeNode = new BindingTreeNode(props[i].Name);
 
                    treeNode.Binding = this.FindBinding(props[i].Name);

                    // Make a reasonable guess as to what the FormatType is
                    if (treeNode.Binding != null) 
                    {
                        treeNode.FormatType = FormatControl.FormatTypeStringFromFormatString(treeNode.Binding.FormatString); 
                    } 
                    else
                    { 
                        treeNode.FormatType = SR.GetString(SR.BindingFormattingDialogFormatTypeNoFormatting);
                    }

                    if (bindableAttr != null && bindableAttr.Bindable) 
                    {
                        commonNode.Nodes.Add(treeNode); 
                    } 
                    else
                    { 
                        allNode.Nodes.Add(treeNode);
                    }

                    if (defaultBindingPropertyNode == null && 
                        !String.IsNullOrEmpty(defaultBindingPropertyName) &&
                        String.Compare(props[i].Name, defaultBindingPropertyName, false /*caseInsensitive*/, CultureInfo.CurrentCulture) == 0) 
                    { 
                        defaultBindingPropertyNode = treeNode;
                    } 
                    else if (defaultPropertyNode == null &&
                             !String.IsNullOrEmpty(defaultPropertyName) &&
                             String.Compare(props[i].Name, defaultPropertyName, false /*caseInsensitive*/, CultureInfo.CurrentCulture) == 0)
                    { 
                        defaultPropertyNode = treeNode;
                    } 
                } 

                commonNode.Expand(); 
                allNode.Expand();

                this.propertiesTreeView.Sort();
 
                // set the default node
                // 1. if we have a DefaultBindingProperty then select it; else 
                // 2. if we have a DefaultProperty then select it 
                // 3. select the first node in "All" nodes
                // 4. select the first node in "Common" nodes 
                BindingTreeNode selectedNode;
                if (defaultBindingPropertyNode != null)
                {
                    selectedNode = defaultBindingPropertyNode; 
                }
                else if (defaultPropertyNode != null) 
                { 
                    selectedNode = defaultPropertyNode;
                } 
                else if (commonNode.Nodes.Count > 0)
                {
                    selectedNode = FirstNodeInAlphabeticalOrder(commonNode.Nodes) as BindingTreeNode;
                } 
                else if (allNode.Nodes.Count > 0)
                { 
                    selectedNode = FirstNodeInAlphabeticalOrder(allNode.Nodes) as BindingTreeNode; 
                }
                else 
                {
                    // [....]: so there are no properties for this component.  should we throw an exception?
                    //
                    selectedNode = null; 
                }
 
                this.propertiesTreeView.SelectedNode = selectedNode; 
                if (selectedNode != null)
                { 
                    selectedNode.EnsureVisible();
                }

                this.dataSourcePicker.PropertyName = selectedNode.Text; 
                this.dataSourcePicker.Binding = selectedNode != null ? selectedNode.Binding : null;
                this.dataSourcePicker.Enabled = true; 
                this.dataSourcePicker.OwnerComponent = this.bindings.BindableComponent; 
                this.dataSourcePicker.DefaultDataSourceUpdateMode = bindings.DefaultDataSourceUpdateMode;
 
                if (selectedNode != null && selectedNode.Binding != null)
                {
                    bindingUpdateDropDown.Enabled = true;
                    this.bindingUpdateDropDown.SelectedItem = selectedNode.Binding.DataSourceUpdateMode; 
                    this.updateModeLabel.Enabled = true;
                    this.formatControl1.Enabled = true; 
 
                    // setup the format control
                    this.formatControl1.FormatType = selectedNode.FormatType; 
                    FormatControl.FormatTypeClass formatTypeItem = this.formatControl1.FormatTypeItem;
                    Debug.Assert(formatTypeItem != null, "The FormatString and FormatProvider was not persisted corectly for this binding");

                    formatTypeItem.PushFormatStringIntoFormatType(selectedNode.Binding.FormatString); 
                    if (selectedNode.Binding.NullValue != null)
                    { 
                        this.formatControl1.NullValue = selectedNode.Binding.NullValue.ToString(); 
                    }
                    else 
                    {
                        this.formatControl1.NullValue = String.Empty;
                    }
                } 
                else
                { 
                    this.bindingUpdateDropDown.Enabled = false; 
                    this.bindingUpdateDropDown.SelectedItem = bindings.DefaultDataSourceUpdateMode;
                    this.updateModeLabel.Enabled = false; 
                    this.formatControl1.Enabled = false;
                    this.formatControl1.FormatType = String.Empty;
                }
 
                // tell the format control that we start a new transaction
                // we have to do this after we set the formatControl 
                this.formatControl1.Dirty = false; 

                // set the currentBindingTreeNode 
                this.currentBindingTreeNode = this.propertiesTreeView.SelectedNode as BindingTreeNode;

            }
            finally 
            {
                this.inLoad = false; 
            } 

            // 
            // Done
            //
        }
 
        // given the property name, this function will return the binding, if there is any
        private Binding FindBinding(string propertyName) 
        { 
            for (int i = 0; i < this.bindings.Count; i ++)
            { 
                if (String.Equals(propertyName, bindings[i].PropertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return bindings[i];
                } 
            }
 
            return null; 
        }
 
        private static TreeNode FirstNodeInAlphabeticalOrder(TreeNodeCollection nodes)
        {
            if (nodes.Count == 0)
            { 
                return null;
            } 
 
            TreeNode result = nodes[0];
 
            for (int i = 1; i < nodes.Count; i ++)
            {
                if (String.Compare(result.Text, nodes[i].Text, false /*ignoreCase*/, CultureInfo.CurrentCulture) > 0)
                { 
                    result = nodes[i];
                } 
            } 

            return result; 
        }

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary> 
        private void InitializeComponent() 
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BindingFormattingDialog)); 
            this.explanationLabel = new System.Windows.Forms.Label();
            this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.propertiesTreeView = new System.Windows.Forms.TreeView();
            this.propertyLabel = new System.Windows.Forms.Label(); 
            this.dataSourcePicker = new BindingFormattingWindowsFormsEditorService();
            this.bindingLabel = new System.Windows.Forms.Label(); 
            this.updateModeLabel = new System.Windows.Forms.Label(); 
            this.bindingUpdateDropDown = new System.Windows.Forms.ComboBox();
            this.formatControl1 = new FormatControl(); 
            this.okCancelTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.mainTableLayoutPanel.SuspendLayout(); 
            this.okCancelTableLayoutPanel.SuspendLayout();
            this.ShowIcon = false; 
            this.SuspendLayout(); 
            //
            // explanationLabel 
            //
            resources.ApplyResources(this.explanationLabel, "explanationLabel");
            this.mainTableLayoutPanel.SetColumnSpan(this.explanationLabel, 3);
            this.explanationLabel.Name = "explanationLabel"; 
            //
            // mainTableLayoutPanel 
            // 
            resources.ApplyResources(this.mainTableLayoutPanel, "mainTableLayoutPanel");
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F)); 
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.mainTableLayoutPanel.Controls.Add(this.okCancelTableLayoutPanel, 2, 4);
            this.mainTableLayoutPanel.Controls.Add(this.formatControl1, 1, 3); 
            this.mainTableLayoutPanel.Controls.Add(this.bindingUpdateDropDown, 2, 2);
            this.mainTableLayoutPanel.Controls.Add(this.propertiesTreeView, 0, 2); 
            this.mainTableLayoutPanel.Controls.Add(this.updateModeLabel, 2, 1); 
            this.mainTableLayoutPanel.Controls.Add(this.dataSourcePicker, 1, 2);
            this.mainTableLayoutPanel.Controls.Add(this.explanationLabel, 0, 0); 
            this.mainTableLayoutPanel.Controls.Add(this.bindingLabel, 1, 1);
            this.mainTableLayoutPanel.Controls.Add(this.propertyLabel, 0, 1);
            this.mainTableLayoutPanel.MinimumSize = new System.Drawing.Size(542, 283);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel"; 
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            //
            // propertiesTreeView
            //
            resources.ApplyResources(this.propertiesTreeView, "propertiesTreeView"); 
            this.propertiesTreeView.Name = "propertiesTreeView";
            this.propertiesTreeView.HideSelection = false; 
            this.propertiesTreeView.TreeViewNodeSorter = new TreeNodeComparer(); 
            this.mainTableLayoutPanel.SetRowSpan(this.propertiesTreeView, 2);
            this.propertiesTreeView.BeforeSelect += new TreeViewCancelEventHandler(this.propertiesTreeView_BeforeSelect); 
            this.propertiesTreeView.AfterSelect += new TreeViewEventHandler(this.propertiesTreeView_AfterSelect);
            //
            // propertyLabel
            // 
            resources.ApplyResources(this.propertyLabel, "propertyLabel");
            this.propertyLabel.Name = "propertyLabel"; 
            // 
            // dataSourcePicker
            // 
            resources.ApplyResources(this.dataSourcePicker, "dataSourcePicker");
            this.dataSourcePicker.Name = "dataSourcePicker";
            this.dataSourcePicker.PropertyValueChanged += new System.EventHandler(dataSourcePicker_PropertyValueChanged);
            // 
            // bindingLabel
            // 
            resources.ApplyResources(this.bindingLabel, "bindingLabel"); 
            this.bindingLabel.Name = "bindingLabel";
            // 
            // updateModeLabel
            //
            resources.ApplyResources(this.updateModeLabel, "updateModeLabel");
            this.updateModeLabel.Name = "updateModeLabel"; 
            //
            // bindingUpdateDropDown 
            // 
            this.bindingUpdateDropDown.FormattingEnabled = true;
            resources.ApplyResources(this.bindingUpdateDropDown, "bindingUpdateDropDown"); 
            this.bindingUpdateDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.bindingUpdateDropDown.Name = "bindingUpdateDropDown";
            this.bindingUpdateDropDown.Items.AddRange(new object[] {DataSourceUpdateMode.Never, DataSourceUpdateMode.OnPropertyChanged, DataSourceUpdateMode.OnValidation});
            this.bindingUpdateDropDown.SelectedIndexChanged += new System.EventHandler(this.bindingUpdateDropDown_SelectedIndexChanged); 
            //
            // formatControl1 
            // 
            this.mainTableLayoutPanel.SetColumnSpan(this.formatControl1, 2);
            resources.ApplyResources(this.formatControl1, "formatControl1"); 
            this.formatControl1.MinimumSize = new System.Drawing.Size(390, 237);
            this.formatControl1.Name = "formatControl1";
            this.formatControl1.NullValueTextBoxEnabled = true;
            // 
            // okCancelTableLayoutPanel
            // 
            resources.ApplyResources(this.okCancelTableLayoutPanel, "okCancelTableLayoutPanel"); 
            this.okCancelTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.okCancelTableLayoutPanel.Controls.Add(this.cancelButton, 1, 0);
            this.okCancelTableLayoutPanel.Controls.Add(this.okButton, 0, 0);
            this.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel"; 
            this.okCancelTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            // 
            // okButton 
            //
            resources.ApplyResources(this.okButton, "okButton"); 
            this.okButton.Name = "okButton";
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Click += new EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton"); 
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel; 
            this.cancelButton.Click += new EventHandler(this.cancelButton_Click);
            //
            // BindingFormattingDialog
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font; 
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent; 
            this.CancelButton = cancelButton;
            this.AcceptButton = okButton; 
            this.Controls.Add(this.mainTableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Name = "BindingFormattingDialog";
            this.mainTableLayoutPanel.ResumeLayout(false); 
            this.mainTableLayoutPanel.PerformLayout();
            this.okCancelTableLayoutPanel.ResumeLayout(false); 
            this.HelpButton = true; 
            this.ShowInTaskbar = false;
            this.MinimizeBox = false; 
            this.MaximizeBox = false;
            this.Load += new EventHandler(BindingFormattingDialog_Load);
            this.Closing += new CancelEventHandler(BindingFormattingDialog_Closing);
            this.HelpButtonClicked += new CancelEventHandler(this.BindingFormattingDialog_HelpButtonClicked); 
            this.HelpRequested += new HelpEventHandler(this.BindingFormattingDialog_HelpRequested);
            this.ResumeLayout(false); 
            this.PerformLayout(); 
        }
 
        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.dirty = false;
        } 

        // this will consolidate the information from the form in the currentBindingTreeNode member variable 
 
        [
        SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")      // We can't avoid casting from a ComboBox. 
        ]
        private void ConsolidateBindingInformation()
        {
            Debug.Assert(this.currentBindingTreeNode != null, "we need a binding tree node to consolidate this information"); 

            Binding binding = this.dataSourcePicker.Binding; 
 
            if (binding == null)
            { 
                return;
            }

            // Whidbey Data Binding will have FormattingEnabled set to true 
            binding.FormattingEnabled = true;
            this.currentBindingTreeNode.Binding = binding; 
            this.currentBindingTreeNode.FormatType = this.formatControl1.FormatType; 

            FormatControl.FormatTypeClass formatTypeItem = this.formatControl1.FormatTypeItem; 

            if (formatTypeItem != null)
            {
                binding.FormatString = formatTypeItem.FormatString; 
                binding.NullValue = this.formatControl1.NullValue;
            } 
 
            binding.DataSourceUpdateMode = (DataSourceUpdateMode) this.bindingUpdateDropDown.SelectedItem;
 
        }

        private void dataSourcePicker_PropertyValueChanged(object sender, System.EventArgs e)
        { 
            if (this.inLoad)
            { 
                return; 
            }
 
            BindingTreeNode bindingTreeNode = this.propertiesTreeView.SelectedNode as BindingTreeNode;
            Debug.Assert(bindingTreeNode != null, " the data source drop down is active only when the user is editing a binding tree node");

            if (this.dataSourcePicker.Binding == bindingTreeNode.Binding) 
            {
                return; 
            } 

            Binding binding = this.dataSourcePicker.Binding; 

            if (binding != null)
            {
                binding.FormattingEnabled = true; 

                Binding currentBinding = bindingTreeNode.Binding; 
                if (currentBinding != null) 
                {
                    binding.FormatString = currentBinding.FormatString; 
                    binding.NullValue = currentBinding.NullValue;
                    binding.FormatInfo = currentBinding.FormatInfo;
                }
            } 

            bindingTreeNode.Binding = binding; 
 
            // enable/disable the format control
            if (binding != null) 
            {
                this.formatControl1.Enabled = true;
                this.updateModeLabel.Enabled = true;
                this.bindingUpdateDropDown.Enabled = true; 
                this.bindingUpdateDropDown.SelectedItem = binding.DataSourceUpdateMode;
 
                if (!String.IsNullOrEmpty(this.formatControl1.FormatType)) 
                {
                    // push the current user control into the format control type 
                    this.formatControl1.FormatType = this.formatControl1.FormatType;
                }
                else
                { 
                    this.formatControl1.FormatType = SR.GetString(SR.BindingFormattingDialogFormatTypeNoFormatting);
                } 
            } 
            else
            { 
                this.formatControl1.Enabled = false;
                this.updateModeLabel.Enabled = false;
                this.bindingUpdateDropDown.Enabled = false;
                this.bindingUpdateDropDown.SelectedItem = bindings.DefaultDataSourceUpdateMode; 

                this.formatControl1.FormatType = SR.GetString(SR.BindingFormattingDialogFormatTypeNoFormatting); 
            } 

            // dirty the form 
            this.dirty = true;
        }

        private void okButton_Click(object sender, EventArgs e) 
        {
            // save the information for the current binding 
            if (this.currentBindingTreeNode != null) 
            {
                this.ConsolidateBindingInformation(); 
            }

            // push the changes
            this.PushChanges(); 
        }
 
        private void propertiesTreeView_AfterSelect(object sender, TreeViewEventArgs e) 
        {
            if (this.inLoad) 
            {
                return;
            }
 
            BindingTreeNode bindingTreeNode = e.Node as BindingTreeNode;
 
            if (bindingTreeNode == null) 
            {
                // disable the data source drop down when the active tree node is not a binding node 
                this.dataSourcePicker.Binding = null;
                this.bindingLabel.Enabled = this.dataSourcePicker.Enabled = false;
                this.updateModeLabel.Enabled = this.bindingUpdateDropDown.Enabled = false;
                this.formatControl1.Enabled = false; 
                return;
            } 
 
            // make sure the the drop down is enabled
            this.bindingLabel.Enabled = this.dataSourcePicker.Enabled = true; 
            this.dataSourcePicker.PropertyName = bindingTreeNode.Text;

            // enable the update mode drop down only if the user is editing a binding;
            this.updateModeLabel.Enabled = this.bindingUpdateDropDown.Enabled = false; 

            // enable the format control only if the user is editing a binding 
            this.formatControl1.Enabled = false; 

            if (bindingTreeNode.Binding != null) 
            {
                // this is not the first time we visit this binding
                // restore the binding information from the last time the user touched this binding
                this.formatControl1.Enabled = true; 
                this.formatControl1.FormatType = bindingTreeNode.FormatType;
                Debug.Assert(this.formatControl1.FormatTypeItem != null, "FormatType did not persist well for this binding"); 
 
                FormatControl.FormatTypeClass formatTypeItem = this.formatControl1.FormatTypeItem;
 
                this.dataSourcePicker.Binding = bindingTreeNode.Binding;

                formatTypeItem.PushFormatStringIntoFormatType(bindingTreeNode.Binding.FormatString);
                if (bindingTreeNode.Binding.NullValue != null) 
                {
                    this.formatControl1.NullValue = bindingTreeNode.Binding.NullValue.ToString(); 
                } 
                else
                { 
                    this.formatControl1.NullValue = String.Empty;
                }

                this.bindingUpdateDropDown.SelectedItem = bindingTreeNode.Binding.DataSourceUpdateMode; 
                Debug.Assert(this.bindingUpdateDropDown.SelectedItem != null, "Binding.UpdateMode was not persisted corectly for this binding");
                this.updateModeLabel.Enabled = this.bindingUpdateDropDown.Enabled = true; 
            } 
            else
            { 
                bool currentDirtyState = this.dirty;
                this.dataSourcePicker.Binding = null;

                this.formatControl1.FormatType = bindingTreeNode.FormatType; 
                this.bindingUpdateDropDown.SelectedItem = bindings.DefaultDataSourceUpdateMode;
 
                this.formatControl1.NullValue = null; 
                this.dirty = currentDirtyState;
            } 

            this.formatControl1.Dirty = false;

            // now save this node so that when we get the BeforeSelect event we know which node was affected 
            this.currentBindingTreeNode = bindingTreeNode;
        } 
 
        private void propertiesTreeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        { 
            if (this.inLoad)
            {
                return;
            } 

            if (this.currentBindingTreeNode == null) 
            { 
                return;
            } 

            // if there is no selected field quit
            if (this.dataSourcePicker.Binding == null)
            { 
                return;
            } 
 
            // if the format control was not touched quit
            if (!this.formatControl1.Enabled) 
            {
                return;
            }
 
            ConsolidateBindingInformation();
 
            // dirty the form 
            this.dirty = this.dirty || this.formatControl1.Dirty;
        } 

        private void PushChanges()
        {
            if (!this.Dirty) 
            {
                return; 
            } 

            IComponentChangeService ccs = host.GetService(typeof(IComponentChangeService)) as IComponentChangeService; 
            PropertyDescriptor prop = null;
            IBindableComponent control = bindings.BindableComponent;
            if (ccs != null && control != null) {
                prop = TypeDescriptor.GetProperties(control)["DataBindings"]; 
                if (prop != null) {
                    ccs.OnComponentChanging(control, prop); 
                } 
            }
 
            // clear the bindings collection and insert the new bindings
            this.bindings.Clear();

            // get the bindings from the "Common" tree nodes 
            TreeNode commonTreeNode = this.propertiesTreeView.Nodes[0];
            Debug.Assert(commonTreeNode.Text.Equals(SR.GetString(SR.BindingFormattingDialogCommonTreeNode)), "the first node in the tree view should be the COMMON node"); 
            for (int i = 0; i < commonTreeNode.Nodes.Count; i ++) 
            {
                BindingTreeNode bindingTreeNode = commonTreeNode.Nodes[i] as BindingTreeNode; 
                Debug.Assert(bindingTreeNode != null, "we only put bindingTreeNodes in the COMMON node");
                if (bindingTreeNode.Binding != null)
                {
                    this.bindings.Add(bindingTreeNode.Binding); 
                }
            } 
 
            // get the bindings from the "All" tree nodes
            TreeNode allTreeNode = this.propertiesTreeView.Nodes[1]; 
            Debug.Assert(allTreeNode.Text.Equals(SR.GetString(SR.BindingFormattingDialogAllTreeNode)), "the second node in the tree view should be the ALL node");
            for (int i = 0; i < allTreeNode.Nodes.Count; i ++)
            {
                BindingTreeNode bindingTreeNode = allTreeNode.Nodes[i] as BindingTreeNode; 
                Debug.Assert(bindingTreeNode != null, "we only put bindingTreeNodes in the ALL node");
                if (bindingTreeNode.Binding != null) 
                { 
                    this.bindings.Add(bindingTreeNode.Binding);
                } 
            }

            if (ccs != null && control != null && prop != null) {
                ccs.OnComponentChanged(control, prop, null, null); 
            }
        } 
 
        private void bindingUpdateDropDown_SelectedIndexChanged(object sender, EventArgs e)
        { 
            if (this.inLoad)
            {
                return;
            } 

            this.dirty = true; 
        } 

        // will hold all the information in the tree node 
        private class BindingTreeNode : TreeNode
        {
            Binding binding;
            // one of the "General", "Numeric", "Currency", "DateTime", "Percentage", "Scientific", "Custom" strings 
            string formatType;
 
            public BindingTreeNode(string name) : base (name) 
            {
            } 

            public Binding Binding
            {
                get 
                {
                    return this.binding; 
                } 
                set
                { 
                    this.binding = value;
                    this.ImageIndex = this.binding != null ? BindingFormattingDialog.BOUNDIMAGEINDEX : BindingFormattingDialog.UNBOUNDIMAGEINDEX;
                    this.SelectedImageIndex = this.binding != null ? BindingFormattingDialog.BOUNDIMAGEINDEX : BindingFormattingDialog.UNBOUNDIMAGEINDEX;
                } 
            }
 
            // one of the "General", "Numeric", "Currency", "DateTime", "Percentage", "Scientific", "Custom" strings 
            public string FormatType
            { 
                get
                {
                    return this.formatType;
                } 
                set
                { 
                    this.formatType = value; 
                }
            } 
        }

        private class TreeNodeComparer : IComparer
        { 
            public TreeNodeComparer() {}
 
            int IComparer.Compare(object o1, object o2) 
            {
                TreeNode treeNode1 = o1 as TreeNode; 
                TreeNode treeNode2 = o2 as TreeNode;

                Debug.Assert(treeNode1 != null && treeNode2 != null, "this method only compares tree nodes");
 
                BindingTreeNode bindingTreeNode1 = treeNode1 as BindingTreeNode;
                BindingTreeNode bindingTreeNode2 = treeNode2 as BindingTreeNode; 
                if (bindingTreeNode1 != null) 
                {
                    Debug.Assert(bindingTreeNode2 != null, "we compare nodes at the same level. and at the BindingTreeNode level are only BindingTreeNodes"); 
                    return String.Compare(bindingTreeNode1.Text, bindingTreeNode2.Text, false /*ignoreCase*/, CultureInfo.CurrentCulture);
                }
                else
                { 
                    Debug.Assert(bindingTreeNode2 == null, "we compare nodes at the same level. and at the BindingTreeNode level are only BindingTreeNodes");
                    if (String.Compare(treeNode1.Text, SR.GetString(SR.BindingFormattingDialogAllTreeNode), false /*ignoreCase*/, CultureInfo.CurrentCulture) == 0) 
                    { 
                        if (String.Compare(treeNode2.Text, SR.GetString(SR.BindingFormattingDialogAllTreeNode), false /*ignoreCase*/, CultureInfo.CurrentCulture) == 0)
                        { 
                            return 0;
                        }
                        else
                        { 
                            // we want to show "Common" before "All"
                            Debug.Assert(String.Compare(treeNode2.Text, SR.GetString(SR.BindingFormattingDialogCommonTreeNode), false /*ignoreCase*/, CultureInfo.CurrentCulture) == 0, " we only have All and Common at this level"); 
                            return 1; 
                        }
                    } 
                    else
                    {
                        Debug.Assert(String.Compare(treeNode1.Text, SR.GetString(SR.BindingFormattingDialogCommonTreeNode), false /*ignoreCase*/, CultureInfo.CurrentCulture) == 0, " we only have All and Common at this level");
 
                        if (String.Compare(treeNode2.Text, SR.GetString(SR.BindingFormattingDialogCommonTreeNode), false /*ignoreCase*/, CultureInfo.CurrentCulture) == 0)
                        { 
                            return 0; 
                        }
                        else 
                        {
                            // we want to show "Common" before "All"
                            Debug.Assert(String.Compare(treeNode2.Text, SR.GetString(SR.BindingFormattingDialogAllTreeNode), false /*ignoreCase*/, CultureInfo.CurrentCulture) == 0, " we only have All and Common at this level");
                            return -1; 
                        }
                    } 
                } 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
using System; 
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using System.ComponentModel.Design; 
using System.Globalization;
using System.Windows.Forms; 
using System.Drawing; 
using System.Collections;
using System.Design; 

namespace System.Windows.Forms.Design
{
    internal class BindingFormattingDialog : System.Windows.Forms.Form 
    {
        // we need the context for the HELP service provider 
        private ITypeDescriptorContext context = null; 

        private ControlBindingsCollection bindings; 

        private BindingFormattingWindowsFormsEditorService dataSourcePicker;
        private System.Windows.Forms.Label explanationLabel;
        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel; 
        private System.Windows.Forms.Label propertyLabel;
        private System.Windows.Forms.TreeView propertiesTreeView; 
        private System.Windows.Forms.Label bindingLabel; 
        private System.Windows.Forms.ComboBox bindingUpdateDropDown;
        private System.Windows.Forms.Label updateModeLabel; 
        private FormatControl formatControl1;
        private System.Windows.Forms.TableLayoutPanel okCancelTableLayoutPanel;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton; 
        private bool inLoad = false;
 
        private bool dirty = false; 

        private const int BOUNDIMAGEINDEX = 0; 
        private const int UNBOUNDIMAGEINDEX = 1;

        // static because there will be only one instance of this dialog shown at any time
        private static Bitmap boundBitmap; 
        private static Bitmap unboundBitmap;
 
        // We have to cache the current tree node because the WinForms TreeView control 
        // doesn't tell use what the previous node is when we receive the BeforeSelect event
        private BindingTreeNode currentBindingTreeNode = null; 
        private IDesignerHost host = null;

        public BindingFormattingDialog()
        { 
            InitializeComponent();
        } 
 
        public ControlBindingsCollection Bindings {
            set { 
                this.bindings = value;
            }
        }
 
        private static Bitmap BoundBitmap
        { 
            get 
            {
                if (boundBitmap == null) 
                {
                    boundBitmap = new Bitmap(typeof(BindingFormattingDialog), "BindingFormattingDialog.Bound.bmp");
                    boundBitmap.MakeTransparent(System.Drawing.Color.Red);
                } 
                return boundBitmap;
            } 
        } 

        public ITypeDescriptorContext Context 
        {
            get
            {
                return this.context; 
            }
 
            set 
            {
                this.context = value; 
                dataSourcePicker.Context = value;
            }
        }
 
        public bool Dirty
        { 
            get 
            {
                return this.dirty || this.formatControl1.Dirty; 
            }
        }

        public IDesignerHost Host 
        {
            set 
            { 
                this.host = value;
            } 
        }

        private static Bitmap UnboundBitmap
        { 
            get
            { 
                if (unboundBitmap == null) 
                {
                    unboundBitmap = new Bitmap(typeof(BindingFormattingDialog), "BindingFormattingDialog.Unbound.bmp"); 
                    unboundBitmap.MakeTransparent(System.Drawing.Color.Red);
                }
                return unboundBitmap;
            } 
        }
 
        private void BindingFormattingDialog_Closing(object sender, CancelEventArgs e) { 
            this.currentBindingTreeNode = null;
            this.dataSourcePicker.OwnerComponent = null; 

            this.formatControl1.ResetFormattingInfo();
        }
 
        private void BindingFormattingDialog_HelpRequested(object sender, HelpEventArgs e)
        { 
            BindingFormattingDialog_HelpRequestHandled(); 
            e.Handled = true;
        } 

        private void BindingFormattingDialog_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            BindingFormattingDialog_HelpRequestHandled(); 
            e.Cancel = true;
        } 
 
        private void BindingFormattingDialog_HelpRequestHandled()
        { 
            IHelpService helpService = this.context.GetService(typeof(IHelpService)) as IHelpService;
            if (helpService != null)
            {
                helpService.ShowHelpFromKeyword("vs.BindingFormattingDialog"); 
            }
        } 
 
        private void BindingFormattingDialog_Load(object sender, EventArgs e)
        { 
            this.inLoad = true;

            try
            { 
                //
                // start a new transaction 
                // 
                this.dirty = false;
 
                //
                // get the dialog font
                //
                System.Drawing.Font uiFont = Control.DefaultFont; 
                IUIService uiService = null;
                if (this.bindings.BindableComponent.Site != null) 
                { 
                    uiService = (IUIService) this.bindings.BindableComponent.Site.GetService(typeof(IUIService));
                } 

                if (uiService != null)
                {
                    uiFont = (System.Drawing.Font) uiService.Styles["DialogFont"]; 
                }
 
                this.Font = uiFont; 

                // 
                // push the image list in the tree view
                //
                if (this.propertiesTreeView.ImageList == null)
                { 
                    ImageList il = new ImageList();
                    il.Images.Add(BoundBitmap); 
                    il.Images.Add(UnboundBitmap); 
                    this.propertiesTreeView.ImageList = il;
                } 

                //
                // get the defaultBindingProperty and / or defaultProperty
                // 
                BindingTreeNode defaultBindingPropertyNode = null;
                BindingTreeNode defaultPropertyNode = null; 
                string defaultBindingPropertyName = null; 
                string defaultPropertyName = null;
                AttributeCollection compAttrs = TypeDescriptor.GetAttributes(bindings.BindableComponent); 
                foreach (Attribute attr in compAttrs)
                {
                    if (attr is DefaultBindingPropertyAttribute)
                    { 
                        defaultBindingPropertyName = ((DefaultBindingPropertyAttribute) attr).Name;
                        break; 
                    } 
                    else if (attr is DefaultPropertyAttribute)
                    { 
                        defaultPropertyName = ((DefaultPropertyAttribute) attr).Name;
                    }
                }
 
                //
                // populate the control bindings tree view 
                // 
                this.propertiesTreeView.Nodes.Clear();
                TreeNode commonNode = new TreeNode(SR.GetString(SR.BindingFormattingDialogCommonTreeNode)); 
                TreeNode allNode = new TreeNode(SR.GetString(SR.BindingFormattingDialogAllTreeNode));

                this.propertiesTreeView.Nodes.Add(commonNode);
                this.propertiesTreeView.Nodes.Add(allNode); 

                IBindableComponent bindableComp = bindings.BindableComponent; 
 
                PropertyDescriptorCollection props = TypeDescriptor.GetProperties(bindableComp);
                for (int i = 0; i < props.Count; i ++) 
                {
                    if (props[i].IsReadOnly)
                    {
                        continue; 
                    }
 
                    BindableAttribute bindableAttr = (BindableAttribute) props[i].Attributes[typeof(BindableAttribute)]; 
                    BrowsableAttribute browsable = (BrowsableAttribute) props[i].Attributes[typeof(BrowsableAttribute)];
 
                    // Filter the non Browsable properties but only if they are non Bindable, too.
                    // vsWhidbey 371995
                    if (browsable != null && !browsable.Browsable && (bindableAttr == null || !bindableAttr.Bindable))
                    { 
                        continue;
                    } 
 
                    BindingTreeNode treeNode = new BindingTreeNode(props[i].Name);
 
                    treeNode.Binding = this.FindBinding(props[i].Name);

                    // Make a reasonable guess as to what the FormatType is
                    if (treeNode.Binding != null) 
                    {
                        treeNode.FormatType = FormatControl.FormatTypeStringFromFormatString(treeNode.Binding.FormatString); 
                    } 
                    else
                    { 
                        treeNode.FormatType = SR.GetString(SR.BindingFormattingDialogFormatTypeNoFormatting);
                    }

                    if (bindableAttr != null && bindableAttr.Bindable) 
                    {
                        commonNode.Nodes.Add(treeNode); 
                    } 
                    else
                    { 
                        allNode.Nodes.Add(treeNode);
                    }

                    if (defaultBindingPropertyNode == null && 
                        !String.IsNullOrEmpty(defaultBindingPropertyName) &&
                        String.Compare(props[i].Name, defaultBindingPropertyName, false /*caseInsensitive*/, CultureInfo.CurrentCulture) == 0) 
                    { 
                        defaultBindingPropertyNode = treeNode;
                    } 
                    else if (defaultPropertyNode == null &&
                             !String.IsNullOrEmpty(defaultPropertyName) &&
                             String.Compare(props[i].Name, defaultPropertyName, false /*caseInsensitive*/, CultureInfo.CurrentCulture) == 0)
                    { 
                        defaultPropertyNode = treeNode;
                    } 
                } 

                commonNode.Expand(); 
                allNode.Expand();

                this.propertiesTreeView.Sort();
 
                // set the default node
                // 1. if we have a DefaultBindingProperty then select it; else 
                // 2. if we have a DefaultProperty then select it 
                // 3. select the first node in "All" nodes
                // 4. select the first node in "Common" nodes 
                BindingTreeNode selectedNode;
                if (defaultBindingPropertyNode != null)
                {
                    selectedNode = defaultBindingPropertyNode; 
                }
                else if (defaultPropertyNode != null) 
                { 
                    selectedNode = defaultPropertyNode;
                } 
                else if (commonNode.Nodes.Count > 0)
                {
                    selectedNode = FirstNodeInAlphabeticalOrder(commonNode.Nodes) as BindingTreeNode;
                } 
                else if (allNode.Nodes.Count > 0)
                { 
                    selectedNode = FirstNodeInAlphabeticalOrder(allNode.Nodes) as BindingTreeNode; 
                }
                else 
                {
                    // [....]: so there are no properties for this component.  should we throw an exception?
                    //
                    selectedNode = null; 
                }
 
                this.propertiesTreeView.SelectedNode = selectedNode; 
                if (selectedNode != null)
                { 
                    selectedNode.EnsureVisible();
                }

                this.dataSourcePicker.PropertyName = selectedNode.Text; 
                this.dataSourcePicker.Binding = selectedNode != null ? selectedNode.Binding : null;
                this.dataSourcePicker.Enabled = true; 
                this.dataSourcePicker.OwnerComponent = this.bindings.BindableComponent; 
                this.dataSourcePicker.DefaultDataSourceUpdateMode = bindings.DefaultDataSourceUpdateMode;
 
                if (selectedNode != null && selectedNode.Binding != null)
                {
                    bindingUpdateDropDown.Enabled = true;
                    this.bindingUpdateDropDown.SelectedItem = selectedNode.Binding.DataSourceUpdateMode; 
                    this.updateModeLabel.Enabled = true;
                    this.formatControl1.Enabled = true; 
 
                    // setup the format control
                    this.formatControl1.FormatType = selectedNode.FormatType; 
                    FormatControl.FormatTypeClass formatTypeItem = this.formatControl1.FormatTypeItem;
                    Debug.Assert(formatTypeItem != null, "The FormatString and FormatProvider was not persisted corectly for this binding");

                    formatTypeItem.PushFormatStringIntoFormatType(selectedNode.Binding.FormatString); 
                    if (selectedNode.Binding.NullValue != null)
                    { 
                        this.formatControl1.NullValue = selectedNode.Binding.NullValue.ToString(); 
                    }
                    else 
                    {
                        this.formatControl1.NullValue = String.Empty;
                    }
                } 
                else
                { 
                    this.bindingUpdateDropDown.Enabled = false; 
                    this.bindingUpdateDropDown.SelectedItem = bindings.DefaultDataSourceUpdateMode;
                    this.updateModeLabel.Enabled = false; 
                    this.formatControl1.Enabled = false;
                    this.formatControl1.FormatType = String.Empty;
                }
 
                // tell the format control that we start a new transaction
                // we have to do this after we set the formatControl 
                this.formatControl1.Dirty = false; 

                // set the currentBindingTreeNode 
                this.currentBindingTreeNode = this.propertiesTreeView.SelectedNode as BindingTreeNode;

            }
            finally 
            {
                this.inLoad = false; 
            } 

            // 
            // Done
            //
        }
 
        // given the property name, this function will return the binding, if there is any
        private Binding FindBinding(string propertyName) 
        { 
            for (int i = 0; i < this.bindings.Count; i ++)
            { 
                if (String.Equals(propertyName, bindings[i].PropertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return bindings[i];
                } 
            }
 
            return null; 
        }
 
        private static TreeNode FirstNodeInAlphabeticalOrder(TreeNodeCollection nodes)
        {
            if (nodes.Count == 0)
            { 
                return null;
            } 
 
            TreeNode result = nodes[0];
 
            for (int i = 1; i < nodes.Count; i ++)
            {
                if (String.Compare(result.Text, nodes[i].Text, false /*ignoreCase*/, CultureInfo.CurrentCulture) > 0)
                { 
                    result = nodes[i];
                } 
            } 

            return result; 
        }

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary> 
        private void InitializeComponent() 
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BindingFormattingDialog)); 
            this.explanationLabel = new System.Windows.Forms.Label();
            this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.propertiesTreeView = new System.Windows.Forms.TreeView();
            this.propertyLabel = new System.Windows.Forms.Label(); 
            this.dataSourcePicker = new BindingFormattingWindowsFormsEditorService();
            this.bindingLabel = new System.Windows.Forms.Label(); 
            this.updateModeLabel = new System.Windows.Forms.Label(); 
            this.bindingUpdateDropDown = new System.Windows.Forms.ComboBox();
            this.formatControl1 = new FormatControl(); 
            this.okCancelTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.mainTableLayoutPanel.SuspendLayout(); 
            this.okCancelTableLayoutPanel.SuspendLayout();
            this.ShowIcon = false; 
            this.SuspendLayout(); 
            //
            // explanationLabel 
            //
            resources.ApplyResources(this.explanationLabel, "explanationLabel");
            this.mainTableLayoutPanel.SetColumnSpan(this.explanationLabel, 3);
            this.explanationLabel.Name = "explanationLabel"; 
            //
            // mainTableLayoutPanel 
            // 
            resources.ApplyResources(this.mainTableLayoutPanel, "mainTableLayoutPanel");
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F)); 
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.mainTableLayoutPanel.Controls.Add(this.okCancelTableLayoutPanel, 2, 4);
            this.mainTableLayoutPanel.Controls.Add(this.formatControl1, 1, 3); 
            this.mainTableLayoutPanel.Controls.Add(this.bindingUpdateDropDown, 2, 2);
            this.mainTableLayoutPanel.Controls.Add(this.propertiesTreeView, 0, 2); 
            this.mainTableLayoutPanel.Controls.Add(this.updateModeLabel, 2, 1); 
            this.mainTableLayoutPanel.Controls.Add(this.dataSourcePicker, 1, 2);
            this.mainTableLayoutPanel.Controls.Add(this.explanationLabel, 0, 0); 
            this.mainTableLayoutPanel.Controls.Add(this.bindingLabel, 1, 1);
            this.mainTableLayoutPanel.Controls.Add(this.propertyLabel, 0, 1);
            this.mainTableLayoutPanel.MinimumSize = new System.Drawing.Size(542, 283);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel"; 
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            //
            // propertiesTreeView
            //
            resources.ApplyResources(this.propertiesTreeView, "propertiesTreeView"); 
            this.propertiesTreeView.Name = "propertiesTreeView";
            this.propertiesTreeView.HideSelection = false; 
            this.propertiesTreeView.TreeViewNodeSorter = new TreeNodeComparer(); 
            this.mainTableLayoutPanel.SetRowSpan(this.propertiesTreeView, 2);
            this.propertiesTreeView.BeforeSelect += new TreeViewCancelEventHandler(this.propertiesTreeView_BeforeSelect); 
            this.propertiesTreeView.AfterSelect += new TreeViewEventHandler(this.propertiesTreeView_AfterSelect);
            //
            // propertyLabel
            // 
            resources.ApplyResources(this.propertyLabel, "propertyLabel");
            this.propertyLabel.Name = "propertyLabel"; 
            // 
            // dataSourcePicker
            // 
            resources.ApplyResources(this.dataSourcePicker, "dataSourcePicker");
            this.dataSourcePicker.Name = "dataSourcePicker";
            this.dataSourcePicker.PropertyValueChanged += new System.EventHandler(dataSourcePicker_PropertyValueChanged);
            // 
            // bindingLabel
            // 
            resources.ApplyResources(this.bindingLabel, "bindingLabel"); 
            this.bindingLabel.Name = "bindingLabel";
            // 
            // updateModeLabel
            //
            resources.ApplyResources(this.updateModeLabel, "updateModeLabel");
            this.updateModeLabel.Name = "updateModeLabel"; 
            //
            // bindingUpdateDropDown 
            // 
            this.bindingUpdateDropDown.FormattingEnabled = true;
            resources.ApplyResources(this.bindingUpdateDropDown, "bindingUpdateDropDown"); 
            this.bindingUpdateDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.bindingUpdateDropDown.Name = "bindingUpdateDropDown";
            this.bindingUpdateDropDown.Items.AddRange(new object[] {DataSourceUpdateMode.Never, DataSourceUpdateMode.OnPropertyChanged, DataSourceUpdateMode.OnValidation});
            this.bindingUpdateDropDown.SelectedIndexChanged += new System.EventHandler(this.bindingUpdateDropDown_SelectedIndexChanged); 
            //
            // formatControl1 
            // 
            this.mainTableLayoutPanel.SetColumnSpan(this.formatControl1, 2);
            resources.ApplyResources(this.formatControl1, "formatControl1"); 
            this.formatControl1.MinimumSize = new System.Drawing.Size(390, 237);
            this.formatControl1.Name = "formatControl1";
            this.formatControl1.NullValueTextBoxEnabled = true;
            // 
            // okCancelTableLayoutPanel
            // 
            resources.ApplyResources(this.okCancelTableLayoutPanel, "okCancelTableLayoutPanel"); 
            this.okCancelTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.okCancelTableLayoutPanel.Controls.Add(this.cancelButton, 1, 0);
            this.okCancelTableLayoutPanel.Controls.Add(this.okButton, 0, 0);
            this.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel"; 
            this.okCancelTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            // 
            // okButton 
            //
            resources.ApplyResources(this.okButton, "okButton"); 
            this.okButton.Name = "okButton";
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Click += new EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton"); 
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel; 
            this.cancelButton.Click += new EventHandler(this.cancelButton_Click);
            //
            // BindingFormattingDialog
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font; 
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent; 
            this.CancelButton = cancelButton;
            this.AcceptButton = okButton; 
            this.Controls.Add(this.mainTableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Name = "BindingFormattingDialog";
            this.mainTableLayoutPanel.ResumeLayout(false); 
            this.mainTableLayoutPanel.PerformLayout();
            this.okCancelTableLayoutPanel.ResumeLayout(false); 
            this.HelpButton = true; 
            this.ShowInTaskbar = false;
            this.MinimizeBox = false; 
            this.MaximizeBox = false;
            this.Load += new EventHandler(BindingFormattingDialog_Load);
            this.Closing += new CancelEventHandler(BindingFormattingDialog_Closing);
            this.HelpButtonClicked += new CancelEventHandler(this.BindingFormattingDialog_HelpButtonClicked); 
            this.HelpRequested += new HelpEventHandler(this.BindingFormattingDialog_HelpRequested);
            this.ResumeLayout(false); 
            this.PerformLayout(); 
        }
 
        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.dirty = false;
        } 

        // this will consolidate the information from the form in the currentBindingTreeNode member variable 
 
        [
        SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")      // We can't avoid casting from a ComboBox. 
        ]
        private void ConsolidateBindingInformation()
        {
            Debug.Assert(this.currentBindingTreeNode != null, "we need a binding tree node to consolidate this information"); 

            Binding binding = this.dataSourcePicker.Binding; 
 
            if (binding == null)
            { 
                return;
            }

            // Whidbey Data Binding will have FormattingEnabled set to true 
            binding.FormattingEnabled = true;
            this.currentBindingTreeNode.Binding = binding; 
            this.currentBindingTreeNode.FormatType = this.formatControl1.FormatType; 

            FormatControl.FormatTypeClass formatTypeItem = this.formatControl1.FormatTypeItem; 

            if (formatTypeItem != null)
            {
                binding.FormatString = formatTypeItem.FormatString; 
                binding.NullValue = this.formatControl1.NullValue;
            } 
 
            binding.DataSourceUpdateMode = (DataSourceUpdateMode) this.bindingUpdateDropDown.SelectedItem;
 
        }

        private void dataSourcePicker_PropertyValueChanged(object sender, System.EventArgs e)
        { 
            if (this.inLoad)
            { 
                return; 
            }
 
            BindingTreeNode bindingTreeNode = this.propertiesTreeView.SelectedNode as BindingTreeNode;
            Debug.Assert(bindingTreeNode != null, " the data source drop down is active only when the user is editing a binding tree node");

            if (this.dataSourcePicker.Binding == bindingTreeNode.Binding) 
            {
                return; 
            } 

            Binding binding = this.dataSourcePicker.Binding; 

            if (binding != null)
            {
                binding.FormattingEnabled = true; 

                Binding currentBinding = bindingTreeNode.Binding; 
                if (currentBinding != null) 
                {
                    binding.FormatString = currentBinding.FormatString; 
                    binding.NullValue = currentBinding.NullValue;
                    binding.FormatInfo = currentBinding.FormatInfo;
                }
            } 

            bindingTreeNode.Binding = binding; 
 
            // enable/disable the format control
            if (binding != null) 
            {
                this.formatControl1.Enabled = true;
                this.updateModeLabel.Enabled = true;
                this.bindingUpdateDropDown.Enabled = true; 
                this.bindingUpdateDropDown.SelectedItem = binding.DataSourceUpdateMode;
 
                if (!String.IsNullOrEmpty(this.formatControl1.FormatType)) 
                {
                    // push the current user control into the format control type 
                    this.formatControl1.FormatType = this.formatControl1.FormatType;
                }
                else
                { 
                    this.formatControl1.FormatType = SR.GetString(SR.BindingFormattingDialogFormatTypeNoFormatting);
                } 
            } 
            else
            { 
                this.formatControl1.Enabled = false;
                this.updateModeLabel.Enabled = false;
                this.bindingUpdateDropDown.Enabled = false;
                this.bindingUpdateDropDown.SelectedItem = bindings.DefaultDataSourceUpdateMode; 

                this.formatControl1.FormatType = SR.GetString(SR.BindingFormattingDialogFormatTypeNoFormatting); 
            } 

            // dirty the form 
            this.dirty = true;
        }

        private void okButton_Click(object sender, EventArgs e) 
        {
            // save the information for the current binding 
            if (this.currentBindingTreeNode != null) 
            {
                this.ConsolidateBindingInformation(); 
            }

            // push the changes
            this.PushChanges(); 
        }
 
        private void propertiesTreeView_AfterSelect(object sender, TreeViewEventArgs e) 
        {
            if (this.inLoad) 
            {
                return;
            }
 
            BindingTreeNode bindingTreeNode = e.Node as BindingTreeNode;
 
            if (bindingTreeNode == null) 
            {
                // disable the data source drop down when the active tree node is not a binding node 
                this.dataSourcePicker.Binding = null;
                this.bindingLabel.Enabled = this.dataSourcePicker.Enabled = false;
                this.updateModeLabel.Enabled = this.bindingUpdateDropDown.Enabled = false;
                this.formatControl1.Enabled = false; 
                return;
            } 
 
            // make sure the the drop down is enabled
            this.bindingLabel.Enabled = this.dataSourcePicker.Enabled = true; 
            this.dataSourcePicker.PropertyName = bindingTreeNode.Text;

            // enable the update mode drop down only if the user is editing a binding;
            this.updateModeLabel.Enabled = this.bindingUpdateDropDown.Enabled = false; 

            // enable the format control only if the user is editing a binding 
            this.formatControl1.Enabled = false; 

            if (bindingTreeNode.Binding != null) 
            {
                // this is not the first time we visit this binding
                // restore the binding information from the last time the user touched this binding
                this.formatControl1.Enabled = true; 
                this.formatControl1.FormatType = bindingTreeNode.FormatType;
                Debug.Assert(this.formatControl1.FormatTypeItem != null, "FormatType did not persist well for this binding"); 
 
                FormatControl.FormatTypeClass formatTypeItem = this.formatControl1.FormatTypeItem;
 
                this.dataSourcePicker.Binding = bindingTreeNode.Binding;

                formatTypeItem.PushFormatStringIntoFormatType(bindingTreeNode.Binding.FormatString);
                if (bindingTreeNode.Binding.NullValue != null) 
                {
                    this.formatControl1.NullValue = bindingTreeNode.Binding.NullValue.ToString(); 
                } 
                else
                { 
                    this.formatControl1.NullValue = String.Empty;
                }

                this.bindingUpdateDropDown.SelectedItem = bindingTreeNode.Binding.DataSourceUpdateMode; 
                Debug.Assert(this.bindingUpdateDropDown.SelectedItem != null, "Binding.UpdateMode was not persisted corectly for this binding");
                this.updateModeLabel.Enabled = this.bindingUpdateDropDown.Enabled = true; 
            } 
            else
            { 
                bool currentDirtyState = this.dirty;
                this.dataSourcePicker.Binding = null;

                this.formatControl1.FormatType = bindingTreeNode.FormatType; 
                this.bindingUpdateDropDown.SelectedItem = bindings.DefaultDataSourceUpdateMode;
 
                this.formatControl1.NullValue = null; 
                this.dirty = currentDirtyState;
            } 

            this.formatControl1.Dirty = false;

            // now save this node so that when we get the BeforeSelect event we know which node was affected 
            this.currentBindingTreeNode = bindingTreeNode;
        } 
 
        private void propertiesTreeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        { 
            if (this.inLoad)
            {
                return;
            } 

            if (this.currentBindingTreeNode == null) 
            { 
                return;
            } 

            // if there is no selected field quit
            if (this.dataSourcePicker.Binding == null)
            { 
                return;
            } 
 
            // if the format control was not touched quit
            if (!this.formatControl1.Enabled) 
            {
                return;
            }
 
            ConsolidateBindingInformation();
 
            // dirty the form 
            this.dirty = this.dirty || this.formatControl1.Dirty;
        } 

        private void PushChanges()
        {
            if (!this.Dirty) 
            {
                return; 
            } 

            IComponentChangeService ccs = host.GetService(typeof(IComponentChangeService)) as IComponentChangeService; 
            PropertyDescriptor prop = null;
            IBindableComponent control = bindings.BindableComponent;
            if (ccs != null && control != null) {
                prop = TypeDescriptor.GetProperties(control)["DataBindings"]; 
                if (prop != null) {
                    ccs.OnComponentChanging(control, prop); 
                } 
            }
 
            // clear the bindings collection and insert the new bindings
            this.bindings.Clear();

            // get the bindings from the "Common" tree nodes 
            TreeNode commonTreeNode = this.propertiesTreeView.Nodes[0];
            Debug.Assert(commonTreeNode.Text.Equals(SR.GetString(SR.BindingFormattingDialogCommonTreeNode)), "the first node in the tree view should be the COMMON node"); 
            for (int i = 0; i < commonTreeNode.Nodes.Count; i ++) 
            {
                BindingTreeNode bindingTreeNode = commonTreeNode.Nodes[i] as BindingTreeNode; 
                Debug.Assert(bindingTreeNode != null, "we only put bindingTreeNodes in the COMMON node");
                if (bindingTreeNode.Binding != null)
                {
                    this.bindings.Add(bindingTreeNode.Binding); 
                }
            } 
 
            // get the bindings from the "All" tree nodes
            TreeNode allTreeNode = this.propertiesTreeView.Nodes[1]; 
            Debug.Assert(allTreeNode.Text.Equals(SR.GetString(SR.BindingFormattingDialogAllTreeNode)), "the second node in the tree view should be the ALL node");
            for (int i = 0; i < allTreeNode.Nodes.Count; i ++)
            {
                BindingTreeNode bindingTreeNode = allTreeNode.Nodes[i] as BindingTreeNode; 
                Debug.Assert(bindingTreeNode != null, "we only put bindingTreeNodes in the ALL node");
                if (bindingTreeNode.Binding != null) 
                { 
                    this.bindings.Add(bindingTreeNode.Binding);
                } 
            }

            if (ccs != null && control != null && prop != null) {
                ccs.OnComponentChanged(control, prop, null, null); 
            }
        } 
 
        private void bindingUpdateDropDown_SelectedIndexChanged(object sender, EventArgs e)
        { 
            if (this.inLoad)
            {
                return;
            } 

            this.dirty = true; 
        } 

        // will hold all the information in the tree node 
        private class BindingTreeNode : TreeNode
        {
            Binding binding;
            // one of the "General", "Numeric", "Currency", "DateTime", "Percentage", "Scientific", "Custom" strings 
            string formatType;
 
            public BindingTreeNode(string name) : base (name) 
            {
            } 

            public Binding Binding
            {
                get 
                {
                    return this.binding; 
                } 
                set
                { 
                    this.binding = value;
                    this.ImageIndex = this.binding != null ? BindingFormattingDialog.BOUNDIMAGEINDEX : BindingFormattingDialog.UNBOUNDIMAGEINDEX;
                    this.SelectedImageIndex = this.binding != null ? BindingFormattingDialog.BOUNDIMAGEINDEX : BindingFormattingDialog.UNBOUNDIMAGEINDEX;
                } 
            }
 
            // one of the "General", "Numeric", "Currency", "DateTime", "Percentage", "Scientific", "Custom" strings 
            public string FormatType
            { 
                get
                {
                    return this.formatType;
                } 
                set
                { 
                    this.formatType = value; 
                }
            } 
        }

        private class TreeNodeComparer : IComparer
        { 
            public TreeNodeComparer() {}
 
            int IComparer.Compare(object o1, object o2) 
            {
                TreeNode treeNode1 = o1 as TreeNode; 
                TreeNode treeNode2 = o2 as TreeNode;

                Debug.Assert(treeNode1 != null && treeNode2 != null, "this method only compares tree nodes");
 
                BindingTreeNode bindingTreeNode1 = treeNode1 as BindingTreeNode;
                BindingTreeNode bindingTreeNode2 = treeNode2 as BindingTreeNode; 
                if (bindingTreeNode1 != null) 
                {
                    Debug.Assert(bindingTreeNode2 != null, "we compare nodes at the same level. and at the BindingTreeNode level are only BindingTreeNodes"); 
                    return String.Compare(bindingTreeNode1.Text, bindingTreeNode2.Text, false /*ignoreCase*/, CultureInfo.CurrentCulture);
                }
                else
                { 
                    Debug.Assert(bindingTreeNode2 == null, "we compare nodes at the same level. and at the BindingTreeNode level are only BindingTreeNodes");
                    if (String.Compare(treeNode1.Text, SR.GetString(SR.BindingFormattingDialogAllTreeNode), false /*ignoreCase*/, CultureInfo.CurrentCulture) == 0) 
                    { 
                        if (String.Compare(treeNode2.Text, SR.GetString(SR.BindingFormattingDialogAllTreeNode), false /*ignoreCase*/, CultureInfo.CurrentCulture) == 0)
                        { 
                            return 0;
                        }
                        else
                        { 
                            // we want to show "Common" before "All"
                            Debug.Assert(String.Compare(treeNode2.Text, SR.GetString(SR.BindingFormattingDialogCommonTreeNode), false /*ignoreCase*/, CultureInfo.CurrentCulture) == 0, " we only have All and Common at this level"); 
                            return 1; 
                        }
                    } 
                    else
                    {
                        Debug.Assert(String.Compare(treeNode1.Text, SR.GetString(SR.BindingFormattingDialogCommonTreeNode), false /*ignoreCase*/, CultureInfo.CurrentCulture) == 0, " we only have All and Common at this level");
 
                        if (String.Compare(treeNode2.Text, SR.GetString(SR.BindingFormattingDialogCommonTreeNode), false /*ignoreCase*/, CultureInfo.CurrentCulture) == 0)
                        { 
                            return 0; 
                        }
                        else 
                        {
                            // we want to show "Common" before "All"
                            Debug.Assert(String.Compare(treeNode2.Text, SR.GetString(SR.BindingFormattingDialogAllTreeNode), false /*ignoreCase*/, CultureInfo.CurrentCulture) == 0, " we only have All and Common at this level");
                            return -1; 
                        }
                    } 
                } 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
