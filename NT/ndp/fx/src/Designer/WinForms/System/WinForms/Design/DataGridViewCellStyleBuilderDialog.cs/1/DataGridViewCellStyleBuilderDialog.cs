using System; 
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms; 
using System.Design;
using System.ComponentModel.Design; 
using System.Diagnostics; 

namespace System.Windows.Forms.Design 
{
    /// <summary>
    /// Summary description for CellStyleBuilder.
    /// </summary> 
    internal class DataGridViewCellStyleBuilder : System.Windows.Forms.Form {
        /// <summary> 
        /// Required designer variable. 
        /// </summary>
        private System.Windows.Forms.PropertyGrid cellStyleProperties; 
        private System.Windows.Forms.GroupBox previewGroupBox;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label label1; 
        private System.Windows.Forms.DataGridView listenerDataGridView;
        private System.Windows.Forms.DataGridView sampleDataGridView; 
        private System.Windows.Forms.DataGridView sampleDataGridViewSelected; 
        private TableLayoutPanel sampleViewTableLayoutPanel;
        private TableLayoutPanel okCancelTableLayoutPanel; 
        private TableLayoutPanel overarchingTableLayoutPanel;
        private TableLayoutPanel sampleViewGridsTableLayoutPanel;
        private System.ComponentModel.Container components = null;
        private System.Windows.Forms.Label normalLabel = null; 
        private System.Windows.Forms.Label selectedLabel = null;
        private IHelpService helpService = null; 
        private IComponent comp = null; 
        private IServiceProvider serviceProvider = null;
 
        private DataGridViewCellStyle cellStyle;
        private ITypeDescriptorContext context = null;

        public DataGridViewCellStyleBuilder(IServiceProvider serviceProvider, IComponent comp) 
        {
            // 
            // Required for Windows Form Designer support 
            //
            InitializeComponent(); 
            //
            // Adds columns and rows to the grid, also resizes them
            //
            InitializeGrids(); 

            this.listenerDataGridView = new System.Windows.Forms.DataGridView(); 
            this.serviceProvider = serviceProvider; 
            this.comp = comp;
 
            if (this.serviceProvider != null)
            {
                this.helpService = (IHelpService) serviceProvider.GetService(typeof(IHelpService));
            } 

            this.cellStyleProperties.Site = new DataGridViewComponentPropertyGridSite(serviceProvider, comp); 
        } 

        private void InitializeGrids() 
        {
            this.sampleDataGridViewSelected.Size = new System.Drawing.Size(100, this.Font.Height + 9);
            this.sampleDataGridView.Size = new System.Drawing.Size(100, this.Font.Height + 9);
            this.sampleDataGridView.AccessibilityObject.Name = SR.GetString(SR.CellStyleBuilderNormalPreviewAccName); 

            DataGridViewRow row = new DataGridViewRow(); 
            row.Cells.Add(new DialogDataGridViewCell()); 
            row.Cells[0].Value = "####";
            row.Cells[0].AccessibilityObject.Name = SR.GetString(SR.CellStyleBuilderSelectedPreviewAccName); 

            this.sampleDataGridViewSelected.Columns.Add(new DataGridViewTextBoxColumn());
            this.sampleDataGridViewSelected.Rows.Add(row);
            this.sampleDataGridViewSelected.Rows[0].Selected = true; 
            this.sampleDataGridViewSelected.AccessibilityObject.Name = SR.GetString(SR.CellStyleBuilderSelectedPreviewAccName);
 
 
            row = new DataGridViewRow();
            row.Cells.Add(new DialogDataGridViewCell()); 
            row.Cells[0].Value = "####";
            row.Cells[0].AccessibilityObject.Name = SR.GetString(SR.CellStyleBuilderNormalPreviewAccName);

            this.sampleDataGridView.Columns.Add(new DataGridViewTextBoxColumn()); 
            this.sampleDataGridView.Rows.Add(row);
        } 
 
        public DataGridViewCellStyle CellStyle
        { 
            get
            {
                return cellStyle;
            } 
            set
            { 
                cellStyle = new DataGridViewCellStyle(value); 
                this.cellStyleProperties.SelectedObject = cellStyle;
                ListenerDataGridViewDefaultCellStyleChanged(null, EventArgs.Empty); 
                this.listenerDataGridView.DefaultCellStyle = cellStyle;
                this.listenerDataGridView.DefaultCellStyleChanged += new EventHandler(this.ListenerDataGridViewDefaultCellStyleChanged);
            }
        } 

        public ITypeDescriptorContext Context 
        { 
            set
            { 
                this.context = value;
            }
        }
 
        private void ListenerDataGridViewDefaultCellStyleChanged(object sender, EventArgs e)
        { 
            DataGridViewCellStyle cellStyleTmp = new DataGridViewCellStyle(cellStyle); 
            this.sampleDataGridView.DefaultCellStyle = cellStyleTmp;
            this.sampleDataGridViewSelected.DefaultCellStyle = cellStyleTmp; 
        }

        /// <summary>
        /// Clean up any resources being used. 
        /// </summary>
        protected override void Dispose( bool disposing ) 
        { 
            if ( disposing ) {
                if (components != null) { 
                    components.Dispose();
                }
            }
            base.Dispose( disposing ); 
        }
 
#region Windows Form Designer generated code 
        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        { 
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataGridViewCellStyleBuilder));
            this.cellStyleProperties = new System.Windows.Forms.PropertyGrid(); 
            this.sampleViewTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this.sampleViewGridsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.normalLabel = new System.Windows.Forms.Label(); 
            this.sampleDataGridView = new System.Windows.Forms.DataGridView();
            this.selectedLabel = new System.Windows.Forms.Label();
            this.sampleDataGridViewSelected = new System.Windows.Forms.DataGridView();
            this.label1 = new System.Windows.Forms.Label(); 
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button(); 
            this.okCancelTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this.previewGroupBox = new System.Windows.Forms.GroupBox();
            this.overarchingTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this.sampleViewTableLayoutPanel.SuspendLayout();
            this.sampleViewGridsTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sampleDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sampleDataGridViewSelected)).BeginInit(); 
            this.okCancelTableLayoutPanel.SuspendLayout();
            this.previewGroupBox.SuspendLayout(); 
            this.overarchingTableLayoutPanel.SuspendLayout(); 
            this.SuspendLayout();
            // 
            // cellStyleProperties
            //
            resources.ApplyResources(this.cellStyleProperties, "cellStyleProperties");
            this.cellStyleProperties.LineColor = System.Drawing.SystemColors.ScrollBar; 
            this.cellStyleProperties.Margin = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.cellStyleProperties.Name = "cellStyleProperties"; 
            this.cellStyleProperties.ToolbarVisible = false; 
            //
            // sampleViewTableLayoutPanel 
            //
            resources.ApplyResources(this.sampleViewTableLayoutPanel, "sampleViewTableLayoutPanel");
            this.sampleViewTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 423F));
            this.sampleViewTableLayoutPanel.Controls.Add(this.sampleViewGridsTableLayoutPanel, 0, 1); 
            this.sampleViewTableLayoutPanel.Controls.Add(this.label1, 0, 0);
            this.sampleViewTableLayoutPanel.Name = "sampleViewTableLayoutPanel"; 
            this.sampleViewTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.sampleViewTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            // 
            // sampleViewGridsTableLayoutPanel
            //
            resources.ApplyResources(this.sampleViewGridsTableLayoutPanel, "sampleViewGridsTableLayoutPanel");
            this.sampleViewGridsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F)); 
            this.sampleViewGridsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.sampleViewGridsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F)); 
            this.sampleViewGridsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F)); 
            this.sampleViewGridsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.sampleViewGridsTableLayoutPanel.Controls.Add(this.normalLabel, 1, 0); 
            this.sampleViewGridsTableLayoutPanel.Controls.Add(this.sampleDataGridView, 1, 1);
            this.sampleViewGridsTableLayoutPanel.Controls.Add(this.selectedLabel, 3, 0);
            this.sampleViewGridsTableLayoutPanel.Controls.Add(this.sampleDataGridViewSelected, 3, 1);
            this.sampleViewGridsTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0); 
            this.sampleViewGridsTableLayoutPanel.Name = "sampleViewGridsTableLayoutPanel";
            this.sampleViewGridsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.sampleViewGridsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            //
            // normalLabel 
            //
            resources.ApplyResources(this.normalLabel, "normalLabel");
            this.normalLabel.Margin = new System.Windows.Forms.Padding(0);
            this.normalLabel.Name = "normalLabel"; 
            //
            // sampleDataGridView 
            // 
            this.sampleDataGridView.AllowUserToAddRows = false;
            resources.ApplyResources(this.sampleDataGridView, "sampleDataGridView"); 
            this.sampleDataGridView.ColumnHeadersVisible = false;
            this.sampleDataGridView.Margin = new System.Windows.Forms.Padding(0);
            this.sampleDataGridView.Name = "sampleDataGridView";
            this.sampleDataGridView.ReadOnly = true; 
            this.sampleDataGridView.RowHeadersVisible = false;
            this.sampleDataGridView.CellStateChanged += new DataGridViewCellStateChangedEventHandler(sampleDataGridView_CellStateChanged); 
            // 
            // selectedLabel
            // 
            resources.ApplyResources(this.selectedLabel, "selectedLabel");
            this.selectedLabel.Margin = new System.Windows.Forms.Padding(0);
            this.selectedLabel.Name = "selectedLabel";
            // 
            // sampleDataGridViewSelected
            // 
            this.sampleDataGridViewSelected.AllowUserToAddRows = false; 
            resources.ApplyResources(this.sampleDataGridViewSelected, "sampleDataGridViewSelected");
            this.sampleDataGridViewSelected.ColumnHeadersVisible = false; 
            this.sampleDataGridViewSelected.Margin = new System.Windows.Forms.Padding(0);
            this.sampleDataGridViewSelected.Name = "sampleDataGridViewSelected";
            this.sampleDataGridViewSelected.ReadOnly = true;
            this.sampleDataGridViewSelected.RowHeadersVisible = false; 
            //
            // label1 
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Margin = new System.Windows.Forms.Padding(0, 0, 0, 3); 
            this.label1.Name = "label1";
            //
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK; 
            this.okButton.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0); 
            this.okButton.Name = "okButton";
            // 
            // cancelButton
            //
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel; 
            this.cancelButton.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.cancelButton.Name = "cancelButton"; 
            // 
            // okCancelTableLayoutPanel
            // 
            resources.ApplyResources(this.okCancelTableLayoutPanel, "okCancelTableLayoutPanel");
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.okCancelTableLayoutPanel.Controls.Add(this.okButton, 0, 0); 
            this.okCancelTableLayoutPanel.Controls.Add(this.cancelButton, 1, 0);
            this.okCancelTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0); 
            this.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel"; 
            this.okCancelTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            // 
            // previewGroupBox
            //
            resources.ApplyResources(this.previewGroupBox, "previewGroupBox");
            this.previewGroupBox.Controls.Add(this.sampleViewTableLayoutPanel); 
            this.previewGroupBox.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.previewGroupBox.Name = "previewGroupBox"; 
            this.previewGroupBox.TabStop = false; 
            //
            // overarchingTableLayoutPanel 
            //
            resources.ApplyResources(this.overarchingTableLayoutPanel, "overarchingTableLayoutPanel");
            this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.overarchingTableLayoutPanel.Controls.Add(this.cellStyleProperties, 0, 0); 
            this.overarchingTableLayoutPanel.Controls.Add(this.okCancelTableLayoutPanel, 0, 2);
            this.overarchingTableLayoutPanel.Controls.Add(this.previewGroupBox, 0, 1); 
            this.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"; 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            //
            // DataGridViewCellStyleBuilder
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F); 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font; 
            this.Controls.Add(this.overarchingTableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog; 
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DataGridViewCellStyleBuilder"; 
            this.ShowIcon = false;
            this.ShowInTaskbar = false; 
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.DataGridViewCellStyleBuilder_HelpButtonClicked); 
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.DataGridViewCellStyleBuilder_HelpRequested);
            this.Load += new System.EventHandler(this.DataGridViewCellStyleBuilder_Load); 
            this.sampleViewTableLayoutPanel.ResumeLayout(false);
            this.sampleViewTableLayoutPanel.PerformLayout();
            this.sampleViewGridsTableLayoutPanel.ResumeLayout(false);
            this.sampleViewGridsTableLayoutPanel.PerformLayout(); 
            ((System.ComponentModel.ISupportInitialize)(this.sampleDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sampleDataGridViewSelected)).EndInit(); 
            this.okCancelTableLayoutPanel.ResumeLayout(false); 
            this.okCancelTableLayoutPanel.PerformLayout();
            this.previewGroupBox.ResumeLayout(false); 
            this.previewGroupBox.PerformLayout();
            this.overarchingTableLayoutPanel.ResumeLayout(false);
            this.overarchingTableLayoutPanel.PerformLayout();
            this.ResumeLayout(false); 
        }
#endregion 
 
        protected override bool ProcessDialogKey(Keys keyData) {
            if ((keyData & Keys.Modifiers) == 0 && (keyData & Keys.KeyCode) == Keys.Escape) { 
                this.Close();
                return true;
            } else {
                return base.ProcessDialogKey(keyData); 
            }
        } 
 
        private void DataGridViewCellStyleBuilder_HelpButtonClicked(object sender, CancelEventArgs e)
        { 
            e.Cancel = true;
            DataGridViewCellStyleBuilder_HelpRequestHandled();        }

        private void DataGridViewCellStyleBuilder_HelpRequested(object sender, System.Windows.Forms.HelpEventArgs e) 
        {
            e.Handled = true; 
            DataGridViewCellStyleBuilder_HelpRequestHandled(); 
        }
 
        private void DataGridViewCellStyleBuilder_HelpRequestHandled()
        {
            IHelpService helpService = this.context.GetService(typeof(IHelpService)) as IHelpService;
            if (helpService != null) 
            {
                helpService.ShowHelpFromKeyword("vs.CellStyleDialog"); 
            } 
        }
 
        private void DataGridViewCellStyleBuilder_Load(object sender, System.EventArgs e)
        {
            // The cell inside the sampleDataGridView should not be selected.
            this.sampleDataGridView.ClearSelection(); 

            // make sure that the cell inside the sampleDataGridView and sampleDataGridViewSelected fill their 
            // respective dataGridView's 
            this.sampleDataGridView.Rows[0].Height = this.sampleDataGridView.Height;
            this.sampleDataGridView.Columns[0].Width = this.sampleDataGridView.Width; 

            this.sampleDataGridViewSelected.Rows[0].Height = this.sampleDataGridViewSelected.Height;
            this.sampleDataGridViewSelected.Columns[0].Width = this.sampleDataGridViewSelected.Width;
 
            // sync the Layout event for both sample DataGridView's
            // so that when the sample DataGridView's are laid out we know to change the size of their cells 
            this.sampleDataGridView.Layout += new System.Windows.Forms.LayoutEventHandler(this.sampleDataGridView_Layout); 
            this.sampleDataGridViewSelected.Layout += new System.Windows.Forms.LayoutEventHandler(this.sampleDataGridView_Layout);
        } 

        private void sampleDataGridView_CellStateChanged(object sender, System.Windows.Forms.DataGridViewCellStateChangedEventArgs e)
        {
            Debug.Assert(e.Cell == this.sampleDataGridView.Rows[0].Cells[0], "the sample data grid view has only one cell"); 
            Debug.Assert(sender == this.sampleDataGridView, "did we forget to unhook notification");
            if ((e.StateChanged & DataGridViewElementStates.Selected) != 0 && (e.Cell.State & DataGridViewElementStates.Selected) != 0) 
            { 
                // The cell inside the sample data grid view became selected.
                // We don't want that to happen 
                this.sampleDataGridView.ClearSelection();
            }
        }
 
        private void sampleDataGridView_Layout(object sender, System.Windows.Forms.LayoutEventArgs e)
        { 
            DataGridView dataGridView = (DataGridView) sender; 
            dataGridView.Rows[0].Height = dataGridView.Height;
            dataGridView.Columns[0].Width = dataGridView.Width; 
        }

        private class DialogDataGridViewCell : DataGridViewTextBoxCell
        { 
            DialogDataGridViewCellAccessibleObject accObj = null;
            protected override AccessibleObject CreateAccessibilityInstance() 
            { 
                if (this.accObj == null)
                { 
                    this.accObj = new DialogDataGridViewCellAccessibleObject(this);
                }

                return accObj; 
            }
 
            private class DialogDataGridViewCellAccessibleObject : DataGridViewCell.DataGridViewCellAccessibleObject 
            {
                public DialogDataGridViewCellAccessibleObject(DataGridViewCell owner) : base (owner) 
                {
                }

                string name = ""; 
                public override string Name
                { 
                    get { 
                        return this.name;
                    } 
                    set
                    {
                        this.name = value;
                    } 

                } 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
using System; 
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms; 
using System.Design;
using System.ComponentModel.Design; 
using System.Diagnostics; 

namespace System.Windows.Forms.Design 
{
    /// <summary>
    /// Summary description for CellStyleBuilder.
    /// </summary> 
    internal class DataGridViewCellStyleBuilder : System.Windows.Forms.Form {
        /// <summary> 
        /// Required designer variable. 
        /// </summary>
        private System.Windows.Forms.PropertyGrid cellStyleProperties; 
        private System.Windows.Forms.GroupBox previewGroupBox;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label label1; 
        private System.Windows.Forms.DataGridView listenerDataGridView;
        private System.Windows.Forms.DataGridView sampleDataGridView; 
        private System.Windows.Forms.DataGridView sampleDataGridViewSelected; 
        private TableLayoutPanel sampleViewTableLayoutPanel;
        private TableLayoutPanel okCancelTableLayoutPanel; 
        private TableLayoutPanel overarchingTableLayoutPanel;
        private TableLayoutPanel sampleViewGridsTableLayoutPanel;
        private System.ComponentModel.Container components = null;
        private System.Windows.Forms.Label normalLabel = null; 
        private System.Windows.Forms.Label selectedLabel = null;
        private IHelpService helpService = null; 
        private IComponent comp = null; 
        private IServiceProvider serviceProvider = null;
 
        private DataGridViewCellStyle cellStyle;
        private ITypeDescriptorContext context = null;

        public DataGridViewCellStyleBuilder(IServiceProvider serviceProvider, IComponent comp) 
        {
            // 
            // Required for Windows Form Designer support 
            //
            InitializeComponent(); 
            //
            // Adds columns and rows to the grid, also resizes them
            //
            InitializeGrids(); 

            this.listenerDataGridView = new System.Windows.Forms.DataGridView(); 
            this.serviceProvider = serviceProvider; 
            this.comp = comp;
 
            if (this.serviceProvider != null)
            {
                this.helpService = (IHelpService) serviceProvider.GetService(typeof(IHelpService));
            } 

            this.cellStyleProperties.Site = new DataGridViewComponentPropertyGridSite(serviceProvider, comp); 
        } 

        private void InitializeGrids() 
        {
            this.sampleDataGridViewSelected.Size = new System.Drawing.Size(100, this.Font.Height + 9);
            this.sampleDataGridView.Size = new System.Drawing.Size(100, this.Font.Height + 9);
            this.sampleDataGridView.AccessibilityObject.Name = SR.GetString(SR.CellStyleBuilderNormalPreviewAccName); 

            DataGridViewRow row = new DataGridViewRow(); 
            row.Cells.Add(new DialogDataGridViewCell()); 
            row.Cells[0].Value = "####";
            row.Cells[0].AccessibilityObject.Name = SR.GetString(SR.CellStyleBuilderSelectedPreviewAccName); 

            this.sampleDataGridViewSelected.Columns.Add(new DataGridViewTextBoxColumn());
            this.sampleDataGridViewSelected.Rows.Add(row);
            this.sampleDataGridViewSelected.Rows[0].Selected = true; 
            this.sampleDataGridViewSelected.AccessibilityObject.Name = SR.GetString(SR.CellStyleBuilderSelectedPreviewAccName);
 
 
            row = new DataGridViewRow();
            row.Cells.Add(new DialogDataGridViewCell()); 
            row.Cells[0].Value = "####";
            row.Cells[0].AccessibilityObject.Name = SR.GetString(SR.CellStyleBuilderNormalPreviewAccName);

            this.sampleDataGridView.Columns.Add(new DataGridViewTextBoxColumn()); 
            this.sampleDataGridView.Rows.Add(row);
        } 
 
        public DataGridViewCellStyle CellStyle
        { 
            get
            {
                return cellStyle;
            } 
            set
            { 
                cellStyle = new DataGridViewCellStyle(value); 
                this.cellStyleProperties.SelectedObject = cellStyle;
                ListenerDataGridViewDefaultCellStyleChanged(null, EventArgs.Empty); 
                this.listenerDataGridView.DefaultCellStyle = cellStyle;
                this.listenerDataGridView.DefaultCellStyleChanged += new EventHandler(this.ListenerDataGridViewDefaultCellStyleChanged);
            }
        } 

        public ITypeDescriptorContext Context 
        { 
            set
            { 
                this.context = value;
            }
        }
 
        private void ListenerDataGridViewDefaultCellStyleChanged(object sender, EventArgs e)
        { 
            DataGridViewCellStyle cellStyleTmp = new DataGridViewCellStyle(cellStyle); 
            this.sampleDataGridView.DefaultCellStyle = cellStyleTmp;
            this.sampleDataGridViewSelected.DefaultCellStyle = cellStyleTmp; 
        }

        /// <summary>
        /// Clean up any resources being used. 
        /// </summary>
        protected override void Dispose( bool disposing ) 
        { 
            if ( disposing ) {
                if (components != null) { 
                    components.Dispose();
                }
            }
            base.Dispose( disposing ); 
        }
 
#region Windows Form Designer generated code 
        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        { 
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataGridViewCellStyleBuilder));
            this.cellStyleProperties = new System.Windows.Forms.PropertyGrid(); 
            this.sampleViewTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this.sampleViewGridsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.normalLabel = new System.Windows.Forms.Label(); 
            this.sampleDataGridView = new System.Windows.Forms.DataGridView();
            this.selectedLabel = new System.Windows.Forms.Label();
            this.sampleDataGridViewSelected = new System.Windows.Forms.DataGridView();
            this.label1 = new System.Windows.Forms.Label(); 
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button(); 
            this.okCancelTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this.previewGroupBox = new System.Windows.Forms.GroupBox();
            this.overarchingTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this.sampleViewTableLayoutPanel.SuspendLayout();
            this.sampleViewGridsTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sampleDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sampleDataGridViewSelected)).BeginInit(); 
            this.okCancelTableLayoutPanel.SuspendLayout();
            this.previewGroupBox.SuspendLayout(); 
            this.overarchingTableLayoutPanel.SuspendLayout(); 
            this.SuspendLayout();
            // 
            // cellStyleProperties
            //
            resources.ApplyResources(this.cellStyleProperties, "cellStyleProperties");
            this.cellStyleProperties.LineColor = System.Drawing.SystemColors.ScrollBar; 
            this.cellStyleProperties.Margin = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.cellStyleProperties.Name = "cellStyleProperties"; 
            this.cellStyleProperties.ToolbarVisible = false; 
            //
            // sampleViewTableLayoutPanel 
            //
            resources.ApplyResources(this.sampleViewTableLayoutPanel, "sampleViewTableLayoutPanel");
            this.sampleViewTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 423F));
            this.sampleViewTableLayoutPanel.Controls.Add(this.sampleViewGridsTableLayoutPanel, 0, 1); 
            this.sampleViewTableLayoutPanel.Controls.Add(this.label1, 0, 0);
            this.sampleViewTableLayoutPanel.Name = "sampleViewTableLayoutPanel"; 
            this.sampleViewTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.sampleViewTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            // 
            // sampleViewGridsTableLayoutPanel
            //
            resources.ApplyResources(this.sampleViewGridsTableLayoutPanel, "sampleViewGridsTableLayoutPanel");
            this.sampleViewGridsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F)); 
            this.sampleViewGridsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.sampleViewGridsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F)); 
            this.sampleViewGridsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F)); 
            this.sampleViewGridsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.sampleViewGridsTableLayoutPanel.Controls.Add(this.normalLabel, 1, 0); 
            this.sampleViewGridsTableLayoutPanel.Controls.Add(this.sampleDataGridView, 1, 1);
            this.sampleViewGridsTableLayoutPanel.Controls.Add(this.selectedLabel, 3, 0);
            this.sampleViewGridsTableLayoutPanel.Controls.Add(this.sampleDataGridViewSelected, 3, 1);
            this.sampleViewGridsTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0); 
            this.sampleViewGridsTableLayoutPanel.Name = "sampleViewGridsTableLayoutPanel";
            this.sampleViewGridsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.sampleViewGridsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            //
            // normalLabel 
            //
            resources.ApplyResources(this.normalLabel, "normalLabel");
            this.normalLabel.Margin = new System.Windows.Forms.Padding(0);
            this.normalLabel.Name = "normalLabel"; 
            //
            // sampleDataGridView 
            // 
            this.sampleDataGridView.AllowUserToAddRows = false;
            resources.ApplyResources(this.sampleDataGridView, "sampleDataGridView"); 
            this.sampleDataGridView.ColumnHeadersVisible = false;
            this.sampleDataGridView.Margin = new System.Windows.Forms.Padding(0);
            this.sampleDataGridView.Name = "sampleDataGridView";
            this.sampleDataGridView.ReadOnly = true; 
            this.sampleDataGridView.RowHeadersVisible = false;
            this.sampleDataGridView.CellStateChanged += new DataGridViewCellStateChangedEventHandler(sampleDataGridView_CellStateChanged); 
            // 
            // selectedLabel
            // 
            resources.ApplyResources(this.selectedLabel, "selectedLabel");
            this.selectedLabel.Margin = new System.Windows.Forms.Padding(0);
            this.selectedLabel.Name = "selectedLabel";
            // 
            // sampleDataGridViewSelected
            // 
            this.sampleDataGridViewSelected.AllowUserToAddRows = false; 
            resources.ApplyResources(this.sampleDataGridViewSelected, "sampleDataGridViewSelected");
            this.sampleDataGridViewSelected.ColumnHeadersVisible = false; 
            this.sampleDataGridViewSelected.Margin = new System.Windows.Forms.Padding(0);
            this.sampleDataGridViewSelected.Name = "sampleDataGridViewSelected";
            this.sampleDataGridViewSelected.ReadOnly = true;
            this.sampleDataGridViewSelected.RowHeadersVisible = false; 
            //
            // label1 
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Margin = new System.Windows.Forms.Padding(0, 0, 0, 3); 
            this.label1.Name = "label1";
            //
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK; 
            this.okButton.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0); 
            this.okButton.Name = "okButton";
            // 
            // cancelButton
            //
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel; 
            this.cancelButton.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.cancelButton.Name = "cancelButton"; 
            // 
            // okCancelTableLayoutPanel
            // 
            resources.ApplyResources(this.okCancelTableLayoutPanel, "okCancelTableLayoutPanel");
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.okCancelTableLayoutPanel.Controls.Add(this.okButton, 0, 0); 
            this.okCancelTableLayoutPanel.Controls.Add(this.cancelButton, 1, 0);
            this.okCancelTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0); 
            this.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel"; 
            this.okCancelTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            // 
            // previewGroupBox
            //
            resources.ApplyResources(this.previewGroupBox, "previewGroupBox");
            this.previewGroupBox.Controls.Add(this.sampleViewTableLayoutPanel); 
            this.previewGroupBox.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.previewGroupBox.Name = "previewGroupBox"; 
            this.previewGroupBox.TabStop = false; 
            //
            // overarchingTableLayoutPanel 
            //
            resources.ApplyResources(this.overarchingTableLayoutPanel, "overarchingTableLayoutPanel");
            this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.overarchingTableLayoutPanel.Controls.Add(this.cellStyleProperties, 0, 0); 
            this.overarchingTableLayoutPanel.Controls.Add(this.okCancelTableLayoutPanel, 0, 2);
            this.overarchingTableLayoutPanel.Controls.Add(this.previewGroupBox, 0, 1); 
            this.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"; 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            //
            // DataGridViewCellStyleBuilder
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F); 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font; 
            this.Controls.Add(this.overarchingTableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog; 
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DataGridViewCellStyleBuilder"; 
            this.ShowIcon = false;
            this.ShowInTaskbar = false; 
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.DataGridViewCellStyleBuilder_HelpButtonClicked); 
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.DataGridViewCellStyleBuilder_HelpRequested);
            this.Load += new System.EventHandler(this.DataGridViewCellStyleBuilder_Load); 
            this.sampleViewTableLayoutPanel.ResumeLayout(false);
            this.sampleViewTableLayoutPanel.PerformLayout();
            this.sampleViewGridsTableLayoutPanel.ResumeLayout(false);
            this.sampleViewGridsTableLayoutPanel.PerformLayout(); 
            ((System.ComponentModel.ISupportInitialize)(this.sampleDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sampleDataGridViewSelected)).EndInit(); 
            this.okCancelTableLayoutPanel.ResumeLayout(false); 
            this.okCancelTableLayoutPanel.PerformLayout();
            this.previewGroupBox.ResumeLayout(false); 
            this.previewGroupBox.PerformLayout();
            this.overarchingTableLayoutPanel.ResumeLayout(false);
            this.overarchingTableLayoutPanel.PerformLayout();
            this.ResumeLayout(false); 
        }
#endregion 
 
        protected override bool ProcessDialogKey(Keys keyData) {
            if ((keyData & Keys.Modifiers) == 0 && (keyData & Keys.KeyCode) == Keys.Escape) { 
                this.Close();
                return true;
            } else {
                return base.ProcessDialogKey(keyData); 
            }
        } 
 
        private void DataGridViewCellStyleBuilder_HelpButtonClicked(object sender, CancelEventArgs e)
        { 
            e.Cancel = true;
            DataGridViewCellStyleBuilder_HelpRequestHandled();        }

        private void DataGridViewCellStyleBuilder_HelpRequested(object sender, System.Windows.Forms.HelpEventArgs e) 
        {
            e.Handled = true; 
            DataGridViewCellStyleBuilder_HelpRequestHandled(); 
        }
 
        private void DataGridViewCellStyleBuilder_HelpRequestHandled()
        {
            IHelpService helpService = this.context.GetService(typeof(IHelpService)) as IHelpService;
            if (helpService != null) 
            {
                helpService.ShowHelpFromKeyword("vs.CellStyleDialog"); 
            } 
        }
 
        private void DataGridViewCellStyleBuilder_Load(object sender, System.EventArgs e)
        {
            // The cell inside the sampleDataGridView should not be selected.
            this.sampleDataGridView.ClearSelection(); 

            // make sure that the cell inside the sampleDataGridView and sampleDataGridViewSelected fill their 
            // respective dataGridView's 
            this.sampleDataGridView.Rows[0].Height = this.sampleDataGridView.Height;
            this.sampleDataGridView.Columns[0].Width = this.sampleDataGridView.Width; 

            this.sampleDataGridViewSelected.Rows[0].Height = this.sampleDataGridViewSelected.Height;
            this.sampleDataGridViewSelected.Columns[0].Width = this.sampleDataGridViewSelected.Width;
 
            // sync the Layout event for both sample DataGridView's
            // so that when the sample DataGridView's are laid out we know to change the size of their cells 
            this.sampleDataGridView.Layout += new System.Windows.Forms.LayoutEventHandler(this.sampleDataGridView_Layout); 
            this.sampleDataGridViewSelected.Layout += new System.Windows.Forms.LayoutEventHandler(this.sampleDataGridView_Layout);
        } 

        private void sampleDataGridView_CellStateChanged(object sender, System.Windows.Forms.DataGridViewCellStateChangedEventArgs e)
        {
            Debug.Assert(e.Cell == this.sampleDataGridView.Rows[0].Cells[0], "the sample data grid view has only one cell"); 
            Debug.Assert(sender == this.sampleDataGridView, "did we forget to unhook notification");
            if ((e.StateChanged & DataGridViewElementStates.Selected) != 0 && (e.Cell.State & DataGridViewElementStates.Selected) != 0) 
            { 
                // The cell inside the sample data grid view became selected.
                // We don't want that to happen 
                this.sampleDataGridView.ClearSelection();
            }
        }
 
        private void sampleDataGridView_Layout(object sender, System.Windows.Forms.LayoutEventArgs e)
        { 
            DataGridView dataGridView = (DataGridView) sender; 
            dataGridView.Rows[0].Height = dataGridView.Height;
            dataGridView.Columns[0].Width = dataGridView.Width; 
        }

        private class DialogDataGridViewCell : DataGridViewTextBoxCell
        { 
            DialogDataGridViewCellAccessibleObject accObj = null;
            protected override AccessibleObject CreateAccessibilityInstance() 
            { 
                if (this.accObj == null)
                { 
                    this.accObj = new DialogDataGridViewCellAccessibleObject(this);
                }

                return accObj; 
            }
 
            private class DialogDataGridViewCellAccessibleObject : DataGridViewCell.DataGridViewCellAccessibleObject 
            {
                public DialogDataGridViewCellAccessibleObject(DataGridViewCell owner) : base (owner) 
                {
                }

                string name = ""; 
                public override string Name
                { 
                    get { 
                        return this.name;
                    } 
                    set
                    {
                        this.name = value;
                    } 

                } 
            } 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
