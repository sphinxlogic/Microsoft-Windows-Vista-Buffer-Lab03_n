using System; 
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.ComponentModel; 
using System.ComponentModel.Design;
using System.Windows.Forms; 
using System.Data; 
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis; 
using System.Globalization;
using System.Design;

namespace System.Windows.Forms.Design 
{
    internal class DataGridViewColumnCollectionDialog : System.Windows.Forms.Form 
    { 
        private System.Windows.Forms.Label selectedColumnsLabel;
 
        private System.Windows.Forms.ListBox selectedColumns;
        private System.Windows.Forms.Button moveUp;
        private System.Windows.Forms.Button moveDown;
        private System.Windows.Forms.Button deleteButton; 
        private System.Windows.Forms.Button addButton;
 
        private System.Windows.Forms.Label propertyGridLabel; 

        private System.Windows.Forms.PropertyGrid propertyGrid1; 
        private System.Windows.Forms.TableLayoutPanel okCancelTableLayoutPanel;

        private System.Windows.Forms.Button okButton;
 
        private System.Windows.Forms.Button cancelButton;
 
 
        private System.Windows.Forms.DataGridView liveDataGridView;
 
        private IComponentChangeService compChangeService = null;

        private System.Windows.Forms.DataGridView dataGridViewPrivateCopy;
        private System.Windows.Forms.DataGridViewColumnCollection columnsPrivateCopy; 
        private System.Collections.Hashtable columnsNames;
        private DataGridViewAddColumnDialog addColumnDialog = null; 
 
        private const int LISTBOXITEMHEIGHT = 17;
        private const int OWNERDRAWHORIZONTALBUFFER = 3; 
        private const int OWNERDRAWVERTICALBUFFER = 4;
        private const int OWNERDRAWITEMIMAGEBUFFER = 2;

        // static because we can only have one instance of the DataGridViewColumnCollectionDialog running at a time 
        private static Bitmap selectedColumnsItemBitmap;
        private static ColorMap[] colorMap = new ColorMap[] { new ColorMap()}; 
 
        private static Type iTypeResolutionServiceType = typeof(System.ComponentModel.Design.ITypeResolutionService);
        private static Type iTypeDiscoveryServiceType = typeof(System.ComponentModel.Design.ITypeDiscoveryService); 
        private static Type iComponentChangeServiceType = typeof(System.ComponentModel.Design.IComponentChangeService);
        private static Type iHelpServiceType = typeof(System.ComponentModel.Design.IHelpService);
        private static Type iUIServiceType = typeof(System.Windows.Forms.Design.IUIService);
        private static Type toolboxBitmapAttributeType = typeof(System.Drawing.ToolboxBitmapAttribute); 

        private bool columnCollectionChanging = false; 
 
        private bool formIsDirty = false;
        private TableLayoutPanel overarchingTableLayoutPanel; 
        private TableLayoutPanel addRemoveTableLayoutPanel;
        private Hashtable userAddedColumns;

        /// <summary> 
        /// Required designer variable.
        /// </summary> 
        private System.ComponentModel.IContainer components = null; 

        internal DataGridViewColumnCollectionDialog () 
        {
            //
            // Required for Windows Form Designer support
            // 
            InitializeComponent();
 
            this.dataGridViewPrivateCopy = new DataGridView(); 
            this.columnsPrivateCopy = this.dataGridViewPrivateCopy.Columns;
            this.columnsPrivateCopy.CollectionChanged += new CollectionChangeEventHandler(columnsPrivateCopy_CollectionChanged); 
        }

        private Bitmap SelectedColumnsItemBitmap
        { 
            get
            { 
                if (selectedColumnsItemBitmap == null) 
                {
                    selectedColumnsItemBitmap = new Bitmap(typeof(DataGridViewColumnCollectionDialog), "DataGridViewColumnsDialog.selectedColumns.bmp"); 
                    selectedColumnsItemBitmap.MakeTransparent(System.Drawing.Color.Red);
                }

                return selectedColumnsItemBitmap; 
            }
        } 
 
        private void columnsPrivateCopy_CollectionChanged(object sender, CollectionChangeEventArgs e)
        { 
            if (this.columnCollectionChanging)
            {
                return;
            } 

            PopulateSelectedColumns(); 
 
            if (e.Action == CollectionChangeAction.Add)
            { 
                this.selectedColumns.SelectedIndex = this.columnsPrivateCopy.IndexOf((DataGridViewColumn) e.Element);
                ListBoxItem lbi = this.selectedColumns.SelectedItem as ListBoxItem;
                this.userAddedColumns[lbi.DataGridViewColumn] = true;
                this.columnsNames[lbi.DataGridViewColumn] = lbi.DataGridViewColumn.Name; 
            }
 
            this.formIsDirty = true; 
        }
 
        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        private void ColumnTypeChanged(ListBoxItem item, Type newType)
        {
            DataGridViewColumn currentColumn = item.DataGridViewColumn; 
            Debug.Assert(typeof(DataGridViewColumn).IsAssignableFrom(newType), "we should only have types that can be assigned to a DataGridViewColumn");
            Debug.Assert(this.selectedColumns.SelectedItem == item, "we must have lost track of what item is in the property grid"); 
 
            DataGridViewColumn newColumn = System.Activator.CreateInstance(newType) as DataGridViewColumn;
 
            ITypeResolutionService tr = this.liveDataGridView.Site.GetService(iTypeResolutionServiceType) as ITypeResolutionService;
            ComponentDesigner newColumnDesigner = System.Windows.Forms.Design.DataGridViewAddColumnDialog.GetComponentDesignerForType(tr, newType);

            CopyDataGridViewColumnProperties(currentColumn /*srcColumn*/, newColumn /*destColumn*/); 
            CopyDataGridViewColumnState(currentColumn /*srcColumn*/, newColumn /*destColumn*/);
 
 
            this.columnCollectionChanging = true;
            int selectedIndex = this.selectedColumns.SelectedIndex; 

            // steal the focus away from the PropertyGrid
            this.selectedColumns.Focus();
            this.ActiveControl = this.selectedColumns; 

            try 
            { 
                // scrub the TypeDescriptor associations
                ListBoxItem lbi = (ListBoxItem) this.selectedColumns.SelectedItem; 

                bool userAddedColumn = (bool) this.userAddedColumns[lbi.DataGridViewColumn];

                string columnSiteName = String.Empty; 
                if (this.columnsNames.Contains(lbi.DataGridViewColumn))
                { 
                    columnSiteName = (string) this.columnsNames[lbi.DataGridViewColumn]; 
                    this.columnsNames.Remove(lbi.DataGridViewColumn);
                } 

                if (this.userAddedColumns.Contains(lbi.DataGridViewColumn))
                {
                    this.userAddedColumns.Remove(lbi.DataGridViewColumn); 
                }
 
                if (lbi.DataGridViewColumnDesigner != null) 
                {
                    TypeDescriptor.RemoveAssociation(lbi.DataGridViewColumn, lbi.DataGridViewColumnDesigner); 
                }

                this.selectedColumns.Items.RemoveAt(selectedIndex);
                this.selectedColumns.Items.Insert(selectedIndex, new ListBoxItem(newColumn, this, newColumnDesigner)); 

                this.columnsPrivateCopy.RemoveAt(selectedIndex); 
                // wipe out the display index 
                newColumn.DisplayIndex = -1;
                this.columnsPrivateCopy.Insert(selectedIndex, newColumn); 

                if (!String.IsNullOrEmpty(columnSiteName))
                {
                    this.columnsNames[newColumn] = columnSiteName; 
                }
 
                this.userAddedColumns[newColumn] = userAddedColumn; 

                // properties like DataGridViewColumn::Frozen are dependent on the DisplayIndex 
                FixColumnCollectionDisplayIndices();

                this.selectedColumns.SelectedIndex = selectedIndex;
                this.propertyGrid1.SelectedObject = this.selectedColumns.SelectedItem; 
            }
            finally 
            { 
                this.columnCollectionChanging = false;
            } 
        }

        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        private void CommitChanges() 
        {
            if (this.formIsDirty) 
            { 
                try
                { 
                        IComponentChangeService changeService = (IComponentChangeService) this.liveDataGridView.Site.GetService(iComponentChangeServiceType);
                        PropertyDescriptor prop = TypeDescriptor.GetProperties(this.liveDataGridView)["Columns"];
                        IContainer currentContainer = this.liveDataGridView.Site != null ? this.liveDataGridView.Site.Container : null;
 
                        // Here is the order in which we should do the ComponentChanging/ComponentChanged
                        // Container.RemoveComponent, Container.AddComponent 
                        // 
                        // 1. OnComponentChanging DataGridView.Columns
                        // 2. DataGridView.Columns.Clear(); 
                        // 3. OnComponentChanged DataGridView.Columns
                        // 4. IContainer.Remove(dataGridView.Columns)
                        // 5. IContainer.Add(new dataGridView.Columns)
                        // 6. OnComponentChanging DataGridView.Columns 
                        // 7. DataGridView.Columns.Add( new DataGridViewColumns)
                        // 8. OnComponentChanged DataGridView.Columns 
 
                        DataGridViewColumn[] oldColumns = new DataGridViewColumn[this.liveDataGridView.Columns.Count];
                        this.liveDataGridView.Columns.CopyTo(oldColumns, 0); 

                        // 1. OnComponentChanging DataGridView.Columns
                        changeService.OnComponentChanging(this.liveDataGridView, prop);
 
                        // 2. DataGridView.Columns.Clear();
                        this.liveDataGridView.Columns.Clear(); 
 
                        // 3. OnComponentChanged DataGridView.Columns
                        changeService.OnComponentChanged(this.liveDataGridView, prop, null, null); 

                        // 4. IContainer.Remove(dataGridView.Columns)
                        if (currentContainer != null)
                        { 
                            for (int i = 0; i < oldColumns.Length; i ++)
                            { 
                                currentContainer.Remove(oldColumns[i]); 
                            }
                        } 

                        DataGridViewColumn[] newColumns = new DataGridViewColumn[this.columnsPrivateCopy.Count];
                        bool[] userAddedColumnsInfo = new bool[this.columnsPrivateCopy.Count];
                        string[] compNames = new string[this.columnsPrivateCopy.Count]; 
                        for (int i = 0; i < this.columnsPrivateCopy.Count; i ++)
                        { 
                            DataGridViewColumn newColumn = (DataGridViewColumn) this.columnsPrivateCopy[i].Clone(); 
                            // at design time we need to do a shallow copy for ContextMenuStrip property
                            newColumn.ContextMenuStrip = this.columnsPrivateCopy[i].ContextMenuStrip; 

                            newColumns[i] = newColumn;
                            userAddedColumnsInfo[i] = (bool) this.userAddedColumns[this.columnsPrivateCopy[i]];
                            compNames[i] = (string) this.columnsNames[this.columnsPrivateCopy[i]]; 
                        }
 
                        // 5. IContainer.Add(new dataGridView.Columns) 
                        if (currentContainer != null)
                        { 
                            for (int i = 0; i < newColumns.Length; i ++)
                            {
                                if (!String.IsNullOrEmpty(compNames[i]) && ValidateName(currentContainer, compNames[i], newColumns[i]))
                                { 
                                    currentContainer.Add(newColumns[i], compNames[i]);
                                } 
                                else 
                                {
                                    currentContainer.Add(newColumns[i]); 
                                }
                            }
                        }
 
                        // 6. OnComponentChanging DataGridView.Columns
                        changeService.OnComponentChanging(this.liveDataGridView, prop); 
 
                        // 7. DataGridView.Columns.Add( new DataGridViewColumns)
                        for (int i = 0; i < newColumns.Length; i ++) 
                        {
                            // wipe out the DisplayIndex
                            PropertyDescriptor pd = TypeDescriptor.GetProperties(newColumns[i])["DisplayIndex"];
                            if (pd != null) { 
                                pd.SetValue(newColumns[i], -1);
                            } 
 
                            this.liveDataGridView.Columns.Add(newColumns[i]);
                        } 

                        // 8. OnComponentChanged DataGridView.Columns
                        changeService.OnComponentChanged(this.liveDataGridView, prop, null, null);
                        for (int i = 0; i < userAddedColumnsInfo.Length; i ++) 
                        {
                            PropertyDescriptor pd = TypeDescriptor.GetProperties(newColumns[i])["UserAddedColumn"]; 
                            if (pd != null) 
                            {
                                pd.SetValue(newColumns[i], userAddedColumnsInfo[i]); 
                            }
                        }
                }
                catch (System.InvalidOperationException ex) 
                {
                    IUIService uiService = (IUIService) this.liveDataGridView.Site.GetService(typeof(IUIService)); 
                    DataGridViewDesigner.ShowErrorDialog(uiService, ex, this.liveDataGridView); 
                    this.DialogResult = DialogResult.Cancel;
                } 
            }
        }

        private void componentChanged(object sender, ComponentChangedEventArgs e) 
        {
            if (e.Component is ListBoxItem && this.selectedColumns.Items.Contains(e.Component)) 
            { 
                this.formIsDirty = true;
            } 
        }

        private static void CopyDataGridViewColumnProperties(DataGridViewColumn srcColumn, DataGridViewColumn destColumn)
        { 
            destColumn.AutoSizeMode = srcColumn.AutoSizeMode;
            destColumn.ContextMenuStrip = srcColumn.ContextMenuStrip; 
            destColumn.DataPropertyName = srcColumn.DataPropertyName; 
            if (srcColumn.HasDefaultCellStyle)
            { 
                CopyDefaultCellStyle(srcColumn, destColumn);
            }
            destColumn.DividerWidth = srcColumn.DividerWidth;
            destColumn.HeaderText = srcColumn.HeaderText; 
            destColumn.MinimumWidth = srcColumn.MinimumWidth;
            destColumn.Name = srcColumn.Name; 
            destColumn.SortMode = srcColumn.SortMode; 
            destColumn.Tag = srcColumn.Tag;
            destColumn.ToolTipText = srcColumn.ToolTipText; 
            destColumn.Width = srcColumn.Width;
            destColumn.FillWeight = srcColumn.FillWeight;
        }
 
        private static void CopyDataGridViewColumnState(DataGridViewColumn srcColumn, DataGridViewColumn destColumn)
        { 
            destColumn.Frozen = srcColumn.Frozen; 
            destColumn.Visible = srcColumn.Visible;
            destColumn.ReadOnly = srcColumn.ReadOnly; 
            destColumn.Resizable = srcColumn.Resizable;
        }

        // We don't have any control over the srcColumn constructor. 
        // So we do a catch all.
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
        private static void CopyDefaultCellStyle(DataGridViewColumn srcColumn, DataGridViewColumn destColumn) 
        {
            // Here is what we want to do ( see vsw 352177 for more details ): 
            // 1. If srcColumn and destColumn have the same type simply copy the default cell style from source to destination
            //  and be done w/ it.
            // 2. Otherwise, determine which properties in the cell style are no longer default and copy those properties.
            //      To do that we need to: 
            //      2.a Create a default srcColumn so we get its default cell style. If we get an exception when we are creating the default cell style
            //      then we copy all the public properties. 
            //      2.b Go thru the public properties in the DataGridViewCellStyle and copy only the property that are changed from the default values; 
            //      2.c We need to special case the DataGridViewCellStyle::NullValue property. This property will be copied only if the NullValue
            //      has the same type in destColumn and in srcColumn. 

            Type srcType = srcColumn.GetType();
            Type destType = destColumn.GetType();
 
            // 1. If srcColumn and destColumn have the same type simply copy the default cell style from source to destination
            //  and be done w/ it. 
            if (srcType.IsAssignableFrom(destType) || destType.IsAssignableFrom(srcType)) 
            {
                destColumn.DefaultCellStyle = srcColumn.DefaultCellStyle; 
                return;
            }

            //      2.a Create a default srcColumn so we get its default cell style. If we get an exception when we are creating the default cell style 
            //      then we copy all the public properties.
            DataGridViewColumn defaultSrcColumn = null; 
            try 
            {
                defaultSrcColumn = System.Activator.CreateInstance(srcType) as DataGridViewColumn; 
            }
            catch(Exception e) {
                if (ClientUtils.IsCriticalException(e)) {
                    throw; 
                }
                defaultSrcColumn = null; 
            } 
            catch
            { 
                defaultSrcColumn = null;
            }

            //      2.b Go thru the public properties in the DataGridViewCellStyle and copy only the property that are changed from the default values; 
            if (defaultSrcColumn == null || defaultSrcColumn.DefaultCellStyle.Alignment != srcColumn.DefaultCellStyle.Alignment)
            { 
                destColumn.DefaultCellStyle.Alignment = srcColumn.DefaultCellStyle.Alignment; 
            }
            if (defaultSrcColumn == null || !defaultSrcColumn.DefaultCellStyle.BackColor.Equals(srcColumn.DefaultCellStyle.BackColor)) 
            {
                destColumn.DefaultCellStyle.BackColor = srcColumn.DefaultCellStyle.BackColor;
            }
            if (defaultSrcColumn != null && srcColumn.DefaultCellStyle.Font != null && !srcColumn.DefaultCellStyle.Font.Equals(defaultSrcColumn.DefaultCellStyle.Font)) 
            {
                destColumn.DefaultCellStyle.Font = srcColumn.DefaultCellStyle.Font ; 
            } 
            if (defaultSrcColumn == null || !defaultSrcColumn.DefaultCellStyle.ForeColor.Equals(srcColumn.DefaultCellStyle.ForeColor))
            { 
                destColumn.DefaultCellStyle.ForeColor = srcColumn.DefaultCellStyle.ForeColor;
            }
            if (defaultSrcColumn == null || !defaultSrcColumn.DefaultCellStyle.Format.Equals(srcColumn.DefaultCellStyle.Format))
            { 
                destColumn.DefaultCellStyle.Format = srcColumn.DefaultCellStyle.Format;
            } 
            if (defaultSrcColumn == null || defaultSrcColumn.DefaultCellStyle.Padding != srcColumn.DefaultCellStyle.Padding) 
            {
                destColumn.DefaultCellStyle.Padding = srcColumn.DefaultCellStyle.Padding; 
            }
            if (defaultSrcColumn == null || !defaultSrcColumn.DefaultCellStyle.SelectionBackColor.Equals(srcColumn.DefaultCellStyle.SelectionBackColor))
            {
                destColumn.DefaultCellStyle.SelectionBackColor = srcColumn.DefaultCellStyle.SelectionBackColor; 
            }
            if (defaultSrcColumn == null || !defaultSrcColumn.DefaultCellStyle.SelectionForeColor.Equals(srcColumn.DefaultCellStyle.SelectionForeColor)) 
            { 
                destColumn.DefaultCellStyle.SelectionForeColor = srcColumn.DefaultCellStyle.SelectionForeColor;
            } 
            if (defaultSrcColumn == null || defaultSrcColumn.DefaultCellStyle.WrapMode != srcColumn.DefaultCellStyle.WrapMode)
            {
                destColumn.DefaultCellStyle.WrapMode = srcColumn.DefaultCellStyle.WrapMode;
            } 
            //      2.c We need to special case the DataGridViewCellStyle::NullValue property. This property will be copied only if the NullValue
            //      has the same type in destColumn and in srcColumn. 
            if (!srcColumn.DefaultCellStyle.IsNullValueDefault) 
            {
                object srcNullValue = srcColumn.DefaultCellStyle.NullValue; 
                object destNullValue = destColumn.DefaultCellStyle.NullValue;

                if (srcNullValue != null && destNullValue != null && srcNullValue.GetType() == destNullValue.GetType())
                { 
                    destColumn.DefaultCellStyle.NullValue = srcNullValue;
                } 
            } 
        }
 
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing ) 
        {
            if( disposing ) 
            { 
                if (components != null)
                { 
                    components.Dispose();
                }
            }
            base.Dispose( disposing ); 
        }
 
        private void FixColumnCollectionDisplayIndices() 
        {
            for (int i = 0; i < this.columnsPrivateCopy.Count; i ++) 
            {
                this.columnsPrivateCopy[i].DisplayIndex = i;
            }
        } 

        private void HookComponentChangedEventHandler(IComponentChangeService componentChangeService) 
        { 
            if (componentChangeService != null)
            { 
                componentChangeService.ComponentChanged += new ComponentChangedEventHandler(this.componentChanged);
            }
        }
 
        #region Windows Form Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary> 
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataGridViewColumnCollectionDialog));
            this.addButton = new System.Windows.Forms.Button(); 
            this.deleteButton = new System.Windows.Forms.Button();
            this.moveDown = new System.Windows.Forms.Button(); 
            this.moveUp = new System.Windows.Forms.Button(); 
            this.selectedColumns = new System.Windows.Forms.ListBox();
            this.overarchingTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this.addRemoveTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.selectedColumnsLabel = new System.Windows.Forms.Label();
            this.propertyGridLabel = new System.Windows.Forms.Label();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid(); 
            this.okCancelTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.cancelButton = new System.Windows.Forms.Button(); 
            this.okButton = new System.Windows.Forms.Button(); 
            this.overarchingTableLayoutPanel.SuspendLayout();
            this.addRemoveTableLayoutPanel.SuspendLayout(); 
            this.okCancelTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // addButton 
            //
            resources.ApplyResources(this.addButton, "addButton"); 
            this.addButton.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0); 
            this.addButton.Name = "addButton";
            this.addButton.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0); 
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            //
            // deleteButton
            // 
            resources.ApplyResources(this.deleteButton, "deleteButton");
            this.deleteButton.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0); 
            this.deleteButton.Name = "deleteButton"; 
            this.deleteButton.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click); 
            //
            // moveDown
            //
            resources.ApplyResources(this.moveDown, "moveDown"); 
            this.moveDown.Margin = new System.Windows.Forms.Padding(0, 1, 18, 0);
            this.moveDown.Name = "moveDown"; 
            this.moveDown.Click += new System.EventHandler(this.moveDown_Click); 
            //
            // moveUp 
            //
            resources.ApplyResources(this.moveUp, "moveUp");
            this.moveUp.Margin = new System.Windows.Forms.Padding(0, 0, 18, 1);
            this.moveUp.Name = "moveUp"; 
            this.moveUp.Click += new System.EventHandler(this.moveUp_Click);
            // 
            // selectedColumns 
            //
            resources.ApplyResources(this.selectedColumns, "selectedColumns"); 
            this.selectedColumns.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.selectedColumns.Margin = new System.Windows.Forms.Padding(0, 2, 3, 3);
            this.selectedColumns.Name = "selectedColumns";
            this.overarchingTableLayoutPanel.SetRowSpan(this.selectedColumns, 2); 
            this.selectedColumns.SelectedIndexChanged += new System.EventHandler(this.selectedColumns_SelectedIndexChanged);
            this.selectedColumns.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.selectedColumns_KeyPress); 
            this.selectedColumns.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.selectedColumns_DrawItem); 
            this.selectedColumns.KeyUp += new System.Windows.Forms.KeyEventHandler(this.selectedColumns_KeyUp);
            // 
            // overarchingTableLayoutPanel
            //
            resources.ApplyResources(this.overarchingTableLayoutPanel, "overarchingTableLayoutPanel");
            this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle()); 
            this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(SizeType.Percent)); 
            this.overarchingTableLayoutPanel.Controls.Add(this.addRemoveTableLayoutPanel, 0, 3); 
            this.overarchingTableLayoutPanel.Controls.Add(this.moveUp, 1, 1);
            this.overarchingTableLayoutPanel.Controls.Add(this.selectedColumnsLabel, 0, 0); 
            this.overarchingTableLayoutPanel.Controls.Add(this.moveDown, 1, 2);
            this.overarchingTableLayoutPanel.Controls.Add(this.propertyGridLabel, 2, 0);
            this.overarchingTableLayoutPanel.Controls.Add(this.selectedColumns, 0, 1);
            this.overarchingTableLayoutPanel.Controls.Add(this.propertyGrid1, 2, 1); 
            this.overarchingTableLayoutPanel.Controls.Add(this.okCancelTableLayoutPanel, 0, 4);
            this.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"; 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            //
            // addRemoveTableLayoutPanel
            // 
            resources.ApplyResources(this.addRemoveTableLayoutPanel, "addRemoveTableLayoutPanel");
            this.addRemoveTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this.addRemoveTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this.addRemoveTableLayoutPanel.Controls.Add(this.addButton, 0, 0);
            this.addRemoveTableLayoutPanel.Controls.Add(this.deleteButton, 1, 0); 
            this.addRemoveTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
            this.addRemoveTableLayoutPanel.Name = "addRemoveTableLayoutPanel";
            this.addRemoveTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            // 
            // selectedColumnsLabel
            // 
            resources.ApplyResources(this.selectedColumnsLabel, "selectedColumnsLabel"); 
            this.selectedColumnsLabel.Margin = new System.Windows.Forms.Padding(0);
            this.selectedColumnsLabel.Name = "selectedColumnsLabel"; 
            //
            // propertyGridLabel
            //
            resources.ApplyResources(this.propertyGridLabel, "propertyGridLabel"); 
            this.propertyGridLabel.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.propertyGridLabel.Name = "propertyGridLabel"; 
            // 
            // propertyGrid1
            // 
            resources.ApplyResources(this.propertyGrid1, "propertyGrid1");
            this.propertyGrid1.LineColor = System.Drawing.SystemColors.ScrollBar;
            this.propertyGrid1.Margin = new System.Windows.Forms.Padding(3, 2, 0, 3);
            this.propertyGrid1.Name = "propertyGrid1"; 
            this.overarchingTableLayoutPanel.SetRowSpan(this.propertyGrid1, 3);
            this.propertyGrid1.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGrid1_PropertyValueChanged); 
            // 
            // okCancelTableLayoutPanel
            // 
            resources.ApplyResources(this.okCancelTableLayoutPanel, "okCancelTableLayoutPanel");
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F)); 
            this.okCancelTableLayoutPanel.Controls.Add(this.cancelButton, 1, 0);
            this.okCancelTableLayoutPanel.Controls.Add(this.okButton, 0, 0); 
            this.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel"; 
            this.overarchingTableLayoutPanel.SetColumnSpan(this.okCancelTableLayoutPanel, 3);
            this.okCancelTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            //
            // cancelButton
            //
            resources.ApplyResources(this.cancelButton, "cancelButton"); 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0); 
            this.cancelButton.Name = "cancelButton"; 
            this.cancelButton.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            // 
            // okButton
            //
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK; 
            this.okButton.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.okButton.Name = "okButton"; 
            this.okButton.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0); 
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // DataGridViewColumnCollectionDialog
            //
            this.AcceptButton = this.okButton;
            resources.ApplyResources(this, "$this"); 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton; 
            this.Controls.Add(this.overarchingTableLayoutPanel); 
            this.HelpButton = true;
            this.MaximizeBox = false; 
            this.MinimizeBox = false;
            this.Name = "DataGridViewColumnCollectionDialog";
            this.Padding = new System.Windows.Forms.Padding(12);
            this.ShowIcon = false; 
            this.ShowInTaskbar = false;
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.DataGridViewColumnCollectionDialog_HelpButtonClicked); 
            this.Closed += new System.EventHandler(this.DataGridViewColumnCollectionDialog_Closed); 
            this.Load += new System.EventHandler(this.DataGridViewColumnCollectionDialog_Load);
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.DataGridViewColumnCollectionDialog_HelpRequested); 
            this.overarchingTableLayoutPanel.ResumeLayout(false);
            this.overarchingTableLayoutPanel.PerformLayout();
            this.addRemoveTableLayoutPanel.ResumeLayout(false);
            this.addRemoveTableLayoutPanel.PerformLayout(); 
            this.okCancelTableLayoutPanel.ResumeLayout(false);
            this.okCancelTableLayoutPanel.PerformLayout(); 
            this.ResumeLayout(false); 

        } 
        #endregion

        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        private static bool IsColumnAddedByUser(DataGridViewColumn col) 
        {
            PropertyDescriptor pd = TypeDescriptor.GetProperties(col)["UserAddedColumn"]; 
            if (pd != null) 
            {
                return (bool) pd.GetValue(col); 
            }
            else
            {
                return false; 
            }
        } 
 
        private void okButton_Click(object sender, System.EventArgs e)
        { 
            CommitChanges();
        }

        private void moveDown_Click(object sender, System.EventArgs e) 
        {
            int selectedIndex = this.selectedColumns.SelectedIndex; 
            Debug.Assert(selectedIndex > -1 && selectedIndex < this.selectedColumns.Items.Count - 1); 

            this.columnCollectionChanging = true; 
            try
            {
                ListBoxItem item = (ListBoxItem) this.selectedColumns.SelectedItem;
                this.selectedColumns.Items.RemoveAt(selectedIndex); 
                this.selectedColumns.Items.Insert(selectedIndex + 1, item);
 
                // now do the same thing to the column collection 
                this.columnsPrivateCopy.RemoveAt(selectedIndex);
 
                // if the column we moved was frozen, make sure the column below is frozen too
                if (item.DataGridViewColumn.Frozen)
                {
                    this.columnsPrivateCopy[selectedIndex].Frozen = true; 
                    #if DEBUG
                    // sanity check 
                    for (int i = 0; i < selectedIndex; i ++) 
                    {
                        Debug.Assert(this.columnsPrivateCopy[i].Frozen, "MOVE_DOWN : all the columns up to the one we moved should be frozen"); 
                    }
                    #endif // DEBUG
                }
 
                // wipe out the DisplayIndex
                item.DataGridViewColumn.DisplayIndex = -1; 
                this.columnsPrivateCopy.Insert(selectedIndex + 1, item.DataGridViewColumn); 

                // properties like DataGridViewColumn::Frozen are dependent on the DisplayIndex 
                FixColumnCollectionDisplayIndices();
            }
            finally
            { 
                this.columnCollectionChanging = false;
            } 
 
            this.formIsDirty = true;
            this.selectedColumns.SelectedIndex = selectedIndex + 1; 
            this.moveUp.Enabled = this.selectedColumns.SelectedIndex > 0;
            this.moveDown.Enabled = this.selectedColumns.SelectedIndex < this.selectedColumns.Items.Count - 1;
        }
 
        private void moveUp_Click(object sender, System.EventArgs e)
        { 
            int selectedIndex = this.selectedColumns.SelectedIndex; 
            Debug.Assert(selectedIndex > 0);
 
            this.columnCollectionChanging = true;
            try
            {
                ListBoxItem item = (ListBoxItem) this.selectedColumns.Items[selectedIndex - 1]; 
                this.selectedColumns.Items.RemoveAt(selectedIndex - 1);
                this.selectedColumns.Items.Insert(selectedIndex, item); 
 
                // now do the same thing to the column collection
                this.columnsPrivateCopy.RemoveAt(selectedIndex - 1); 

                // we want to keep the Frozen value of the column we move intact
                // if we move up an UnFrozen column and the column above the one we move is Frozen
                // then we need to make the column above the one we move UnFrozen, too 
                //
                // columnsPrivateCopy[selectedIndex - 1] points to the column we just moved 
                // 
                if (item.DataGridViewColumn.Frozen && !this.columnsPrivateCopy[selectedIndex - 1].Frozen)
                { 
                    item.DataGridViewColumn.Frozen = false;
                }

                // wipe out the display index. 
                item.DataGridViewColumn.DisplayIndex = -1;
                this.columnsPrivateCopy.Insert(selectedIndex, item.DataGridViewColumn); 
 
                // properties like DataGridViewColumn::Frozen are dependent on the DisplayIndex
                FixColumnCollectionDisplayIndices(); 
            }
            finally
            {
                this.columnCollectionChanging = false; 
            }
 
            this.formIsDirty = true; 
            this.selectedColumns.SelectedIndex = selectedIndex - 1;
            this.moveUp.Enabled = this.selectedColumns.SelectedIndex > 0; 
            this.moveDown.Enabled = this.selectedColumns.SelectedIndex < this.selectedColumns.Items.Count - 1;

            // vsw 495403: keep the selected item visible.
            // For some reason, we only have to do this when we move a column up. 
            // When we move a column down or when we delete a column, the selected item remains visible.
            if (this.selectedColumns.SelectedIndex != -1 && this.selectedColumns.TopIndex > this.selectedColumns.SelectedIndex) { 
                this.selectedColumns.TopIndex = this.selectedColumns.SelectedIndex; 
            }
        } 

        private void DataGridViewColumnCollectionDialog_Closed(object sender, System.EventArgs e)
        {
            // scrub the TypeDescriptor association between DataGridViewColumns and their designers 
            for (int i = 0; i < this.selectedColumns.Items.Count; i ++)
            { 
                ListBoxItem lbi = this.selectedColumns.Items[i] as ListBoxItem; 
                if (lbi.DataGridViewColumnDesigner != null)
                { 
                    TypeDescriptor.RemoveAssociation(lbi.DataGridViewColumn, lbi.DataGridViewColumnDesigner);
                }
            }
            this.columnsNames = null; 
            this.userAddedColumns = null;
        } 
 
        private void DataGridViewColumnCollectionDialog_HelpButtonClicked(object sender, CancelEventArgs e)
        { 
            e.Cancel = true;
            DataGridViewColumnCollectionDialog_HelpRequestHandled();
        }
 
        private void DataGridViewColumnCollectionDialog_HelpRequested(object sender, System.Windows.Forms.HelpEventArgs e)
        { 
            DataGridViewColumnCollectionDialog_HelpRequestHandled(); 
            e.Handled = true;
        } 

        private void DataGridViewColumnCollectionDialog_HelpRequestHandled()
        {
            IHelpService helpService = this.liveDataGridView.Site.GetService(iHelpServiceType) as IHelpService; 
            if (helpService != null)
            { 
                helpService.ShowHelpFromKeyword("vs.DataGridViewColumnCollectionDialog"); 
            }
        } 

        private void DataGridViewColumnCollectionDialog_Load(object sender, EventArgs e)
        {
            // get the Dialog Font 
            //
            Font uiFont = Control.DefaultFont; 
            IUIService uiService = (IUIService) this.liveDataGridView.Site.GetService(iUIServiceType); 
            if (uiService != null) {
                uiFont = (Font) uiService.Styles["DialogFont"]; 
            }
            this.Font = uiFont;

            // keep the selected index to 0 or -1 if there are no selected columns 
            this.selectedColumns.SelectedIndex = Math.Min(0, this.selectedColumns.Items.Count - 1);
 
            this.moveUp.Enabled = this.selectedColumns.SelectedIndex > 0; 
            this.moveDown.Enabled = this.selectedColumns.SelectedIndex < this.selectedColumns.Items.Count - 1;
            this.deleteButton.Enabled = this.selectedColumns.Items.Count > 0 && this.selectedColumns.SelectedIndex != -1; 
            this.propertyGrid1.SelectedObject = this.selectedColumns.SelectedItem;

            this.selectedColumns.ItemHeight = this.Font.Height + OWNERDRAWVERTICALBUFFER;
 
            this.ActiveControl = this.selectedColumns;
 
            this.SetSelectedColumnsHorizontalExtent(); 

            this.selectedColumns.Focus(); 

            formIsDirty = false;
        }
 
        private void deleteButton_Click(object sender, System.EventArgs e)
        { 
            Debug.Assert(this.selectedColumns.SelectedIndex != -1); 
            int selectedIndex = this.selectedColumns.SelectedIndex;
 
            this.columnsNames.Remove(this.columnsPrivateCopy[selectedIndex]);
            this.userAddedColumns.Remove(this.columnsPrivateCopy[selectedIndex]);

            this.columnsPrivateCopy.RemoveAt(selectedIndex); 

            // try to keep the same selected index 
            this.selectedColumns.SelectedIndex = Math.Min(this.selectedColumns.Items.Count - 1, selectedIndex); 

            this.moveUp.Enabled = this.selectedColumns.SelectedIndex > 0; 
            this.moveDown.Enabled = this.selectedColumns.SelectedIndex < this.selectedColumns.Items.Count - 1;
            this.deleteButton.Enabled = this.selectedColumns.Items.Count > 0 && this.selectedColumns.SelectedIndex != -1;
            this.propertyGrid1.SelectedObject = this.selectedColumns.SelectedItem;
        } 

        private void addButton_Click(object sender, System.EventArgs e) 
        { 
            int insertIndex;
            if (this.selectedColumns.SelectedIndex == -1) 
            {
                insertIndex = this.selectedColumns.Items.Count;
            }
            else 
            {
                insertIndex = this.selectedColumns.SelectedIndex + 1; 
            } 

            if (this.addColumnDialog == null) 
            {
                this.addColumnDialog = new DataGridViewAddColumnDialog(this.columnsPrivateCopy, this.liveDataGridView);
                this.addColumnDialog.StartPosition = FormStartPosition.CenterParent;
            } 

            this.addColumnDialog.Start(insertIndex, false /*persistChangesToDesigner*/); 
 
            this.addColumnDialog.ShowDialog(this);
        } 

        private void PopulateSelectedColumns()
        {
            int selectedIndex = this.selectedColumns.SelectedIndex; 

            // scrub the TypeDescriptor association between DataGridViewColumns and their designers 
            for (int i = 0; i < this.selectedColumns.Items.Count; i ++) 
            {
                ListBoxItem lbi = this.selectedColumns.Items[i] as ListBoxItem; 
                if (lbi.DataGridViewColumnDesigner != null)
                {
                    TypeDescriptor.RemoveAssociation(lbi.DataGridViewColumn, lbi.DataGridViewColumnDesigner);
                } 
            }
 
            this.selectedColumns.Items.Clear(); 
            ITypeResolutionService tr = (ITypeResolutionService) this.liveDataGridView.Site.GetService(iTypeResolutionServiceType);
 
            for (int i = 0; i < this.columnsPrivateCopy.Count; i ++)
            {
                ComponentDesigner columnDesigner = System.Windows.Forms.Design.DataGridViewAddColumnDialog.GetComponentDesignerForType(tr, this.columnsPrivateCopy[i].GetType());
                this.selectedColumns.Items.Add(new ListBoxItem(this.columnsPrivateCopy[i], this, columnDesigner)); 
            }
 
            this.selectedColumns.SelectedIndex = Math.Min(selectedIndex, this.selectedColumns.Items.Count - 1); 

            SetSelectedColumnsHorizontalExtent(); 

            if (this.selectedColumns.Items.Count == 0)
            {
                this.propertyGridLabel.Text = SR.GetString(SR.DataGridViewProperties); 
            }
        } 
 
        private void propertyGrid1_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        { 
            if (!this.columnCollectionChanging)
            {
                this.formIsDirty = true;
                // refresh the selected columns when the user changed the HeaderText property 
                if (e.ChangedItem.PropertyDescriptor.Name.Equals("HeaderText"))
                { 
                    // invalidate the selected index only 
                    int selectedIndex = this.selectedColumns.SelectedIndex;
                    Debug.Assert(selectedIndex != -1, "we forgot to take away the selected object from the property grid"); 
                    Rectangle bounds = new Rectangle(0, selectedIndex * this.selectedColumns.ItemHeight, this.selectedColumns.Width, this.selectedColumns.ItemHeight);
                    this.columnCollectionChanging = true;
                    try
                    { 
                        // for accessibility reasons, we need to reset the item in the selected columns collection.
                        this.selectedColumns.Items[selectedIndex] = this.selectedColumns.Items[selectedIndex]; 
                    } 
                    finally
                    { 
                        this.columnCollectionChanging = false;
                    }

                    this.selectedColumns.Invalidate(bounds); 

                    // if the header text changed make sure that we update the selected columns HorizontalExtent 
                    this.SetSelectedColumnsHorizontalExtent(); 
                }
                else if (e.ChangedItem.PropertyDescriptor.Name.Equals("DataPropertyName")) 
                {
                    DataGridViewColumn col = (DataGridViewColumn) ((ListBoxItem) this.selectedColumns.SelectedItem).DataGridViewColumn;

                    if (String.IsNullOrEmpty(col.DataPropertyName)) 
                    {
                        this.propertyGridLabel.Text = SR.GetString(SR.DataGridViewUnboundColumnProperties); 
                    } 
                    else
                    { 
                        this.propertyGridLabel.Text = SR.GetString(SR.DataGridViewBoundColumnProperties);
                    }
                }
                else if (e.ChangedItem.PropertyDescriptor.Name.Equals("Name")) 
                {
                    DataGridViewColumn col = (DataGridViewColumn) ((ListBoxItem) this.selectedColumns.SelectedItem).DataGridViewColumn; 
                    this.columnsNames[col] = col.Name; 
                }
            } 
        }

        private void selectedColumns_DrawItem(object sender, DrawItemEventArgs e)
        { 
            if (e.Index < 0)
            { 
                return; 
            }
 
            ListBoxItem lbi = this.selectedColumns.Items[e.Index] as ListBoxItem;

#if DGV_DITHERING
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected) 
            {
                ImageAttributes attr = new ImageAttributes(); 
 
                colorMap[0].OldColor = Color.White;
                colorMap[0].NewColor = e.BackColor; 

                //
                // TO DO : DITHER
                // 
                attr.SetRemapTable(colorMap, ColorAdjustType.Bitmap);
 
                Rectangle imgRectangle = new Rectangle(e.Bounds.X + OWNERDRAWITEMIMAGEBUFFER, e.Bounds.Y + OWNERDRAWITEMIMAGEBUFFER, lbi.ToolboxBitmap.Width, lbi.ToolboxBitmap.Height); 
                e.Graphics.DrawImage(lbi.ToolboxBitmap,
                                     imgRectangle, 
                                     0,
                                     0,
                                     imgRectangle.Width,
                                     imgRectangle.Height, 
                                     GraphicsUnit.Pixel,
                                     attr); 
                attr.Dispose(); 
            }
            else 
            {
#endif // DGV_DITHERING
            e.Graphics.DrawImage(lbi.ToolboxBitmap,
                                 e.Bounds.X + OWNERDRAWITEMIMAGEBUFFER, 
                                 e.Bounds.Y + OWNERDRAWITEMIMAGEBUFFER,
                                 lbi.ToolboxBitmap.Width, 
                                 lbi.ToolboxBitmap.Height); 

            Rectangle bounds = e.Bounds; 
            bounds.Width -= lbi.ToolboxBitmap.Width + 2*OWNERDRAWITEMIMAGEBUFFER;
            bounds.X += lbi.ToolboxBitmap.Width + 2*OWNERDRAWITEMIMAGEBUFFER;
            bounds.Y += OWNERDRAWITEMIMAGEBUFFER;
            bounds.Height -= 2 * OWNERDRAWITEMIMAGEBUFFER; 

            Brush selectedBrush = new System.Drawing.SolidBrush(e.BackColor); 
            Brush foreBrush = new System.Drawing.SolidBrush(e.ForeColor); 
            Brush backBrush = new System.Drawing.SolidBrush(this.selectedColumns.BackColor);
 
            string columnName = ((ListBoxItem) this.selectedColumns.Items[e.Index]).ToString();

            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            { 
                // first get the text rectangle
                int textWidth = Size.Ceiling(e.Graphics.MeasureString(columnName, e.Font, new SizeF(bounds.Width, bounds.Height))).Width; 
                // [....]: the spec calls for + 7 but I think that + 3 does the trick better 
                Rectangle focusRectangle = new Rectangle(bounds.X, e.Bounds.Y + 1, textWidth + OWNERDRAWHORIZONTALBUFFER, e.Bounds.Height - 2);
 
                e.Graphics.FillRectangle(selectedBrush, focusRectangle);
                focusRectangle.Inflate(-1, -1);

                e.Graphics.DrawString(columnName, e.Font, foreBrush, focusRectangle); 

                focusRectangle.Inflate(1, 1); 
 
                // only paint the focus rectangle when the list box is focused
                if (this.selectedColumns.Focused) { 
                    ControlPaint.DrawFocusRectangle(e.Graphics, focusRectangle, e.ForeColor, e.BackColor);
                }

                e.Graphics.FillRectangle(backBrush, new Rectangle(focusRectangle.Right + 1, e.Bounds.Y, e.Bounds.Width - focusRectangle.Right - 1, e.Bounds.Height)); 

            } 
            else 
            {
                e.Graphics.FillRectangle(backBrush, new Rectangle(bounds.X, e.Bounds.Y, e.Bounds.Width - bounds.X, e.Bounds.Height)); 

                e.Graphics.DrawString(columnName, e.Font, foreBrush, bounds);
            }
 
            selectedBrush.Dispose();
            backBrush.Dispose(); 
            foreBrush.Dispose(); 
        }
 
        private void selectedColumns_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Modifiers) == 0 && e.KeyCode == Keys.F4)
            { 
                this.propertyGrid1.Focus();
                e.Handled = true; 
            } 
        }
 
        private void selectedColumns_KeyPress(object sender, KeyPressEventArgs e)
        {
            Keys modifierKeys = System.Windows.Forms.Control.ModifierKeys;
 
            // vsw 479960.
            // Don't let Ctrl-* propagate to the selected columns list box. 
            if ((modifierKeys & Keys.Control) != 0) 
            {
                e.Handled = true; 
            }
        }

        private void selectedColumns_SelectedIndexChanged(object sender, System.EventArgs e) 
        {
            if (this.columnCollectionChanging) 
            { 
                return;
            } 

            this.propertyGrid1.SelectedObject = this.selectedColumns.SelectedItem;

            // enable/disable up/down/delete buttons 
            this.moveDown.Enabled = this.selectedColumns.Items.Count > 0 && this.selectedColumns.SelectedIndex != this.selectedColumns.Items.Count - 1;
            this.moveUp.Enabled = this.selectedColumns.Items.Count > 0 && this.selectedColumns.SelectedIndex > 0; 
            this.deleteButton.Enabled = this.selectedColumns.Items.Count > 0 && this.selectedColumns.SelectedIndex != -1; 

            if (this.selectedColumns.SelectedItem != null) 
            {
                DataGridViewColumn column = ((ListBoxItem) this.selectedColumns.SelectedItem).DataGridViewColumn;
                if (String.IsNullOrEmpty(column.DataPropertyName))
                { 
                    this.propertyGridLabel.Text = SR.GetString(SR.DataGridViewUnboundColumnProperties);
                } 
                else 
                {
                    this.propertyGridLabel.Text = SR.GetString(SR.DataGridViewBoundColumnProperties); 
                }

            } else {
                this.propertyGridLabel.Text = SR.GetString(SR.DataGridViewProperties); 
            }
        } 
 
        internal void SetLiveDataGridView(DataGridView dataGridView)
        { 

            IComponentChangeService newComponentChangeService = null;
            if (dataGridView.Site != null)
            { 
                newComponentChangeService = (IComponentChangeService) dataGridView.Site.GetService(iComponentChangeServiceType);
            } 
 
            if (newComponentChangeService != this.compChangeService)
            { 
                UnhookComponentChangedEventHandler(this.compChangeService);

                this.compChangeService = newComponentChangeService;
 
                HookComponentChangedEventHandler(this.compChangeService);
            } 
 
            this.liveDataGridView = dataGridView;
 
            this.dataGridViewPrivateCopy.Site = dataGridView.Site;
            this.dataGridViewPrivateCopy.AutoSizeColumnsMode = dataGridView.AutoSizeColumnsMode;
            this.dataGridViewPrivateCopy.DataSource = dataGridView.DataSource;
            this.dataGridViewPrivateCopy.DataMember = dataGridView.DataMember; 
            this.columnsNames = new System.Collections.Hashtable(this.columnsPrivateCopy.Count);
            this.columnsPrivateCopy.Clear(); 
 
            this.userAddedColumns = new System.Collections.Hashtable(this.liveDataGridView.Columns.Count);
 
            // Set ColumnCollectionChanging to true so:
            // 1. the column collection changed event handler does not execute PopulateSelectedColumns over and over again.
            // 2. the collection changed event handler does not add each live column to its userAddedColumns hash table.
            // 
            this.columnCollectionChanging = true;
            try 
            { 
                for (int i = 0; i < this.liveDataGridView.Columns.Count; i ++)
                { 
                    DataGridViewColumn liveCol = this.liveDataGridView.Columns[i];
                    DataGridViewColumn col = (DataGridViewColumn) liveCol.Clone();
                    // at design time we need to do a shallow copy for the ContextMenuStrip property
                    // 
                    col.ContextMenuStrip = this.liveDataGridView.Columns[i].ContextMenuStrip;
                    // wipe out the display index before adding the new column. 
                    col.DisplayIndex = -1; 
                    this.columnsPrivateCopy.Add(col);
 
                    if (liveCol.Site != null)
                    {
                        this.columnsNames[col] = liveCol.Site.Name;
                    } 
                    this.userAddedColumns[col] = IsColumnAddedByUser(this.liveDataGridView.Columns[i]);
                } 
            } 
            finally
            { 
                this.columnCollectionChanging = false;
            }

            PopulateSelectedColumns(); 

            this.propertyGrid1.Site = new DataGridViewComponentPropertyGridSite(this.liveDataGridView.Site, this.liveDataGridView); 
 
            this.propertyGrid1.SelectedObject = this.selectedColumns.SelectedItem;
        } 

        private void SetSelectedColumnsHorizontalExtent() {
            int maxItemWidth = 0;
            for (int i = 0; i < this.selectedColumns.Items.Count; i ++) { 
                int itemWidth = TextRenderer.MeasureText(this.selectedColumns.Items[i].ToString(), this.selectedColumns.Font).Width;
                maxItemWidth = Math.Max(maxItemWidth, itemWidth); 
            } 

            this.selectedColumns.HorizontalExtent = this.SelectedColumnsItemBitmap.Width + 2 * OWNERDRAWITEMIMAGEBUFFER + maxItemWidth + OWNERDRAWHORIZONTALBUFFER; 
        }

        private void UnhookComponentChangedEventHandler(IComponentChangeService componentChangeService)
        { 
            if (componentChangeService != null)
            { 
                componentChangeService.ComponentChanged -= new ComponentChangedEventHandler(this.componentChanged); 
            }
        } 

        private static bool ValidateName(IContainer container, string siteName, IComponent component)
        {
            ComponentCollection comps = container.Components; 
            if (comps == null)
            { 
                return true; 
            }
 
            for (int i = 0; i < comps.Count; i ++)
            {
                IComponent comp = comps[i];
                if (comp == null || comp.Site == null) 
                {
                    continue; 
                } 

                ISite s = comp.Site; 

                if (s != null && s.Name != null && string.Equals(s.Name, siteName, StringComparison.OrdinalIgnoreCase) && s.Component != component) {
                    return false;
                } 
            }
 
            return true; 
        }
 
        // internal because the DataGridViewColumnDataPropertyNameEditor needs to get at the ListBoxItem
        // IComponent because some editors for some dataGridViewColumn properties - DataGridViewComboBox::DataSource editor -
        // need the site
        internal class ListBoxItem : ICustomTypeDescriptor, IComponent 
        {
            private DataGridViewColumn column; 
            private DataGridViewColumnCollectionDialog owner; 
            private ComponentDesigner compDesigner;
            private Image toolboxBitmap; 
            public ListBoxItem(DataGridViewColumn column, DataGridViewColumnCollectionDialog owner, ComponentDesigner compDesigner)
            {
                this.column = column;
                this.owner = owner; 
                this.compDesigner = compDesigner;
 
                if (this.compDesigner != null) 
                {
                    this.compDesigner.Initialize(column); 
                    TypeDescriptor.CreateAssociation(this.column, this.compDesigner);
                }

                ToolboxBitmapAttribute attr = TypeDescriptor.GetAttributes(column)[toolboxBitmapAttributeType] as ToolboxBitmapAttribute; 
                if (attr != null)
                { 
                    this.toolboxBitmap = attr.GetImage(column, false /*large*/); 
                }
                else 
                {
                    this.toolboxBitmap = this.owner.SelectedColumnsItemBitmap;
                }
 
                DataGridViewColumnDesigner dgvColumnDesigner = compDesigner as DataGridViewColumnDesigner;
                if (dgvColumnDesigner != null) 
                { 
                    dgvColumnDesigner.LiveDataGridView = this.owner.liveDataGridView;
                } 
            }

            public DataGridViewColumn DataGridViewColumn
            { 
                get
                { 
                    return this.column; 
                }
            } 

            public ComponentDesigner DataGridViewColumnDesigner
            {
                get 
                {
                    return this.compDesigner; 
                } 
            }
 
            public DataGridViewColumnCollectionDialog Owner
            {
                get
                { 
                    return this.owner;
                } 
            } 

            public Image ToolboxBitmap 
            {
                get
                {
                    return this.toolboxBitmap; 
                }
            } 
 
            public override string ToString()
            { 
                return this.column.HeaderText;
            }

            // ICustomTypeDescriptor implementation 
            AttributeCollection ICustomTypeDescriptor.GetAttributes() {
                return TypeDescriptor.GetAttributes(this.column); 
            } 

            string ICustomTypeDescriptor.GetClassName() { 
                return TypeDescriptor.GetClassName(this.column);
            }

            string ICustomTypeDescriptor.GetComponentName() { 
                return TypeDescriptor.GetComponentName(this.column);
            } 
 
            TypeConverter ICustomTypeDescriptor.GetConverter() {
                return TypeDescriptor.GetConverter(this.column); 
            }

            EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() {
                return TypeDescriptor.GetDefaultEvent(this.column); 
            }
 
            PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() { 
                return TypeDescriptor.GetDefaultProperty(this.column);
            } 

            object ICustomTypeDescriptor.GetEditor(Type type) {
                return TypeDescriptor.GetEditor(this.column, type);
            } 

            EventDescriptorCollection ICustomTypeDescriptor.GetEvents() { 
                return TypeDescriptor.GetEvents(this.column); 
            }
 
            EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attrs) {
                return TypeDescriptor.GetEvents(this.column, attrs);
            }
 
            PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() {
                return (((ICustomTypeDescriptor) this).GetProperties(null)); 
            } 

            PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attrs) { 
                PropertyDescriptorCollection props = TypeDescriptor.GetProperties(this.column);

                PropertyDescriptor[] propArray = null;
                if (this.compDesigner != null) 
                {
                    // PropertyDescriptorCollection does not let us change properties. 
                    // So we have to create a hash table that we pass to PreFilterProperties 
                    // and then copy back the result from PreFilterProperties
 
                    // We should look into speeding this up w/ our own DataGridViewColumnTypes...
                    //
                    Hashtable hash = new Hashtable();
                    for (int i = 0; i < props.Count; i ++) 
                    {
                        hash.Add(props[i].Name, props[i]); 
                    } 

                    ((IDesignerFilter) compDesigner).PreFilterProperties(hash); 

                    // PreFilterProperties can add / remove properties.
                    // Use the hashtable's Count, not the old property descriptor collection's count.
                    propArray = new PropertyDescriptor[hash.Count + 1]; 
                    hash.Values.CopyTo(propArray, 0);
                } 
                else 
                {
                    propArray = new PropertyDescriptor[props.Count + 1]; 
                    props.CopyTo(propArray, 0);
                }

                propArray[propArray.Length - 1] = new ColumnTypePropertyDescriptor(); 

                return new PropertyDescriptorCollection(propArray); 
            } 

            object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) { 
                if (pd == null)
                {
                    return this.column;
                } 
                else if (pd is ColumnTypePropertyDescriptor)
                { 
                    return this; 
                }
                else 
                {
                    return this.column;
                }
 
            }
 
            ISite IComponent.Site 
            {
                get 
                {
                    return this.owner.liveDataGridView.Site;
                }
                set 
                {
                } 
            } 

            event EventHandler IComponent.Disposed 
            {
                add
                {
                } 
                remove
                { 
                } 
            }
 
            [
                SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")      // The ListBoxItem does not own the ToolBoxBitmap
                                                                                                   // so it can't dispose it.
            ] 
            void IDisposable.Dispose()
            { 
            } 
        }
 
        private class ColumnTypePropertyDescriptor : PropertyDescriptor
        {
            public ColumnTypePropertyDescriptor() : base("ColumnType", null)
            { 
            }
 
            public override AttributeCollection Attributes 
            {
                get 
                {
                    EditorAttribute editorAttr = new EditorAttribute("System.Windows.Forms.Design.DataGridViewColumnTypeEditor, " + AssemblyRef.SystemDesign, typeof(System.Drawing.Design.UITypeEditor));
                    DescriptionAttribute descriptionAttr = new DescriptionAttribute(SR.GetString(SR.DataGridViewColumnTypePropertyDescription));
                    CategoryAttribute categoryAttr = CategoryAttribute.Design; 
                    // add the description attribute and the categories attribute
                    Attribute[] attrs = new Attribute[] {editorAttr, descriptionAttr, categoryAttr}; 
                    return new AttributeCollection(attrs); 
                }
            } 

            public override Type ComponentType
            {
                get 
                {
                    return typeof(ListBoxItem); 
                } 
            }
 
            public override bool IsReadOnly
            {
                get
                { 
                    return false;
                } 
            } 

            public override Type PropertyType 
            {
                get
                {
                    return typeof(Type); 
                }
            } 
 
            public override bool CanResetValue(object component)
            { 
                Debug.Assert(component is ListBoxItem, "this property descriptor only relates to the data grid view column class");
                return false;
            }
 
            public override object GetValue(object component)
            { 
                if (component == null) 
                {
                    return null; 
                }

                ListBoxItem item = (ListBoxItem) component;
                return item.DataGridViewColumn.GetType().Name; 
            }
 
            public override void ResetValue(object component) 
            {
            } 

            public override void SetValue(object component, object value)
            {
                ListBoxItem item = (ListBoxItem) component; 
                Type type = value as Type;
                if (item.DataGridViewColumn.GetType() != type) 
                { 
                    item.Owner.ColumnTypeChanged(item, type);
                    OnValueChanged(component, EventArgs.Empty); 
                }
            }

            public override bool ShouldSerializeValue(object component) 
            {
                return false; 
            } 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
using System; 
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.ComponentModel; 
using System.ComponentModel.Design;
using System.Windows.Forms; 
using System.Data; 
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis; 
using System.Globalization;
using System.Design;

namespace System.Windows.Forms.Design 
{
    internal class DataGridViewColumnCollectionDialog : System.Windows.Forms.Form 
    { 
        private System.Windows.Forms.Label selectedColumnsLabel;
 
        private System.Windows.Forms.ListBox selectedColumns;
        private System.Windows.Forms.Button moveUp;
        private System.Windows.Forms.Button moveDown;
        private System.Windows.Forms.Button deleteButton; 
        private System.Windows.Forms.Button addButton;
 
        private System.Windows.Forms.Label propertyGridLabel; 

        private System.Windows.Forms.PropertyGrid propertyGrid1; 
        private System.Windows.Forms.TableLayoutPanel okCancelTableLayoutPanel;

        private System.Windows.Forms.Button okButton;
 
        private System.Windows.Forms.Button cancelButton;
 
 
        private System.Windows.Forms.DataGridView liveDataGridView;
 
        private IComponentChangeService compChangeService = null;

        private System.Windows.Forms.DataGridView dataGridViewPrivateCopy;
        private System.Windows.Forms.DataGridViewColumnCollection columnsPrivateCopy; 
        private System.Collections.Hashtable columnsNames;
        private DataGridViewAddColumnDialog addColumnDialog = null; 
 
        private const int LISTBOXITEMHEIGHT = 17;
        private const int OWNERDRAWHORIZONTALBUFFER = 3; 
        private const int OWNERDRAWVERTICALBUFFER = 4;
        private const int OWNERDRAWITEMIMAGEBUFFER = 2;

        // static because we can only have one instance of the DataGridViewColumnCollectionDialog running at a time 
        private static Bitmap selectedColumnsItemBitmap;
        private static ColorMap[] colorMap = new ColorMap[] { new ColorMap()}; 
 
        private static Type iTypeResolutionServiceType = typeof(System.ComponentModel.Design.ITypeResolutionService);
        private static Type iTypeDiscoveryServiceType = typeof(System.ComponentModel.Design.ITypeDiscoveryService); 
        private static Type iComponentChangeServiceType = typeof(System.ComponentModel.Design.IComponentChangeService);
        private static Type iHelpServiceType = typeof(System.ComponentModel.Design.IHelpService);
        private static Type iUIServiceType = typeof(System.Windows.Forms.Design.IUIService);
        private static Type toolboxBitmapAttributeType = typeof(System.Drawing.ToolboxBitmapAttribute); 

        private bool columnCollectionChanging = false; 
 
        private bool formIsDirty = false;
        private TableLayoutPanel overarchingTableLayoutPanel; 
        private TableLayoutPanel addRemoveTableLayoutPanel;
        private Hashtable userAddedColumns;

        /// <summary> 
        /// Required designer variable.
        /// </summary> 
        private System.ComponentModel.IContainer components = null; 

        internal DataGridViewColumnCollectionDialog () 
        {
            //
            // Required for Windows Form Designer support
            // 
            InitializeComponent();
 
            this.dataGridViewPrivateCopy = new DataGridView(); 
            this.columnsPrivateCopy = this.dataGridViewPrivateCopy.Columns;
            this.columnsPrivateCopy.CollectionChanged += new CollectionChangeEventHandler(columnsPrivateCopy_CollectionChanged); 
        }

        private Bitmap SelectedColumnsItemBitmap
        { 
            get
            { 
                if (selectedColumnsItemBitmap == null) 
                {
                    selectedColumnsItemBitmap = new Bitmap(typeof(DataGridViewColumnCollectionDialog), "DataGridViewColumnsDialog.selectedColumns.bmp"); 
                    selectedColumnsItemBitmap.MakeTransparent(System.Drawing.Color.Red);
                }

                return selectedColumnsItemBitmap; 
            }
        } 
 
        private void columnsPrivateCopy_CollectionChanged(object sender, CollectionChangeEventArgs e)
        { 
            if (this.columnCollectionChanging)
            {
                return;
            } 

            PopulateSelectedColumns(); 
 
            if (e.Action == CollectionChangeAction.Add)
            { 
                this.selectedColumns.SelectedIndex = this.columnsPrivateCopy.IndexOf((DataGridViewColumn) e.Element);
                ListBoxItem lbi = this.selectedColumns.SelectedItem as ListBoxItem;
                this.userAddedColumns[lbi.DataGridViewColumn] = true;
                this.columnsNames[lbi.DataGridViewColumn] = lbi.DataGridViewColumn.Name; 
            }
 
            this.formIsDirty = true; 
        }
 
        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        private void ColumnTypeChanged(ListBoxItem item, Type newType)
        {
            DataGridViewColumn currentColumn = item.DataGridViewColumn; 
            Debug.Assert(typeof(DataGridViewColumn).IsAssignableFrom(newType), "we should only have types that can be assigned to a DataGridViewColumn");
            Debug.Assert(this.selectedColumns.SelectedItem == item, "we must have lost track of what item is in the property grid"); 
 
            DataGridViewColumn newColumn = System.Activator.CreateInstance(newType) as DataGridViewColumn;
 
            ITypeResolutionService tr = this.liveDataGridView.Site.GetService(iTypeResolutionServiceType) as ITypeResolutionService;
            ComponentDesigner newColumnDesigner = System.Windows.Forms.Design.DataGridViewAddColumnDialog.GetComponentDesignerForType(tr, newType);

            CopyDataGridViewColumnProperties(currentColumn /*srcColumn*/, newColumn /*destColumn*/); 
            CopyDataGridViewColumnState(currentColumn /*srcColumn*/, newColumn /*destColumn*/);
 
 
            this.columnCollectionChanging = true;
            int selectedIndex = this.selectedColumns.SelectedIndex; 

            // steal the focus away from the PropertyGrid
            this.selectedColumns.Focus();
            this.ActiveControl = this.selectedColumns; 

            try 
            { 
                // scrub the TypeDescriptor associations
                ListBoxItem lbi = (ListBoxItem) this.selectedColumns.SelectedItem; 

                bool userAddedColumn = (bool) this.userAddedColumns[lbi.DataGridViewColumn];

                string columnSiteName = String.Empty; 
                if (this.columnsNames.Contains(lbi.DataGridViewColumn))
                { 
                    columnSiteName = (string) this.columnsNames[lbi.DataGridViewColumn]; 
                    this.columnsNames.Remove(lbi.DataGridViewColumn);
                } 

                if (this.userAddedColumns.Contains(lbi.DataGridViewColumn))
                {
                    this.userAddedColumns.Remove(lbi.DataGridViewColumn); 
                }
 
                if (lbi.DataGridViewColumnDesigner != null) 
                {
                    TypeDescriptor.RemoveAssociation(lbi.DataGridViewColumn, lbi.DataGridViewColumnDesigner); 
                }

                this.selectedColumns.Items.RemoveAt(selectedIndex);
                this.selectedColumns.Items.Insert(selectedIndex, new ListBoxItem(newColumn, this, newColumnDesigner)); 

                this.columnsPrivateCopy.RemoveAt(selectedIndex); 
                // wipe out the display index 
                newColumn.DisplayIndex = -1;
                this.columnsPrivateCopy.Insert(selectedIndex, newColumn); 

                if (!String.IsNullOrEmpty(columnSiteName))
                {
                    this.columnsNames[newColumn] = columnSiteName; 
                }
 
                this.userAddedColumns[newColumn] = userAddedColumn; 

                // properties like DataGridViewColumn::Frozen are dependent on the DisplayIndex 
                FixColumnCollectionDisplayIndices();

                this.selectedColumns.SelectedIndex = selectedIndex;
                this.propertyGrid1.SelectedObject = this.selectedColumns.SelectedItem; 
            }
            finally 
            { 
                this.columnCollectionChanging = false;
            } 
        }

        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        private void CommitChanges() 
        {
            if (this.formIsDirty) 
            { 
                try
                { 
                        IComponentChangeService changeService = (IComponentChangeService) this.liveDataGridView.Site.GetService(iComponentChangeServiceType);
                        PropertyDescriptor prop = TypeDescriptor.GetProperties(this.liveDataGridView)["Columns"];
                        IContainer currentContainer = this.liveDataGridView.Site != null ? this.liveDataGridView.Site.Container : null;
 
                        // Here is the order in which we should do the ComponentChanging/ComponentChanged
                        // Container.RemoveComponent, Container.AddComponent 
                        // 
                        // 1. OnComponentChanging DataGridView.Columns
                        // 2. DataGridView.Columns.Clear(); 
                        // 3. OnComponentChanged DataGridView.Columns
                        // 4. IContainer.Remove(dataGridView.Columns)
                        // 5. IContainer.Add(new dataGridView.Columns)
                        // 6. OnComponentChanging DataGridView.Columns 
                        // 7. DataGridView.Columns.Add( new DataGridViewColumns)
                        // 8. OnComponentChanged DataGridView.Columns 
 
                        DataGridViewColumn[] oldColumns = new DataGridViewColumn[this.liveDataGridView.Columns.Count];
                        this.liveDataGridView.Columns.CopyTo(oldColumns, 0); 

                        // 1. OnComponentChanging DataGridView.Columns
                        changeService.OnComponentChanging(this.liveDataGridView, prop);
 
                        // 2. DataGridView.Columns.Clear();
                        this.liveDataGridView.Columns.Clear(); 
 
                        // 3. OnComponentChanged DataGridView.Columns
                        changeService.OnComponentChanged(this.liveDataGridView, prop, null, null); 

                        // 4. IContainer.Remove(dataGridView.Columns)
                        if (currentContainer != null)
                        { 
                            for (int i = 0; i < oldColumns.Length; i ++)
                            { 
                                currentContainer.Remove(oldColumns[i]); 
                            }
                        } 

                        DataGridViewColumn[] newColumns = new DataGridViewColumn[this.columnsPrivateCopy.Count];
                        bool[] userAddedColumnsInfo = new bool[this.columnsPrivateCopy.Count];
                        string[] compNames = new string[this.columnsPrivateCopy.Count]; 
                        for (int i = 0; i < this.columnsPrivateCopy.Count; i ++)
                        { 
                            DataGridViewColumn newColumn = (DataGridViewColumn) this.columnsPrivateCopy[i].Clone(); 
                            // at design time we need to do a shallow copy for ContextMenuStrip property
                            newColumn.ContextMenuStrip = this.columnsPrivateCopy[i].ContextMenuStrip; 

                            newColumns[i] = newColumn;
                            userAddedColumnsInfo[i] = (bool) this.userAddedColumns[this.columnsPrivateCopy[i]];
                            compNames[i] = (string) this.columnsNames[this.columnsPrivateCopy[i]]; 
                        }
 
                        // 5. IContainer.Add(new dataGridView.Columns) 
                        if (currentContainer != null)
                        { 
                            for (int i = 0; i < newColumns.Length; i ++)
                            {
                                if (!String.IsNullOrEmpty(compNames[i]) && ValidateName(currentContainer, compNames[i], newColumns[i]))
                                { 
                                    currentContainer.Add(newColumns[i], compNames[i]);
                                } 
                                else 
                                {
                                    currentContainer.Add(newColumns[i]); 
                                }
                            }
                        }
 
                        // 6. OnComponentChanging DataGridView.Columns
                        changeService.OnComponentChanging(this.liveDataGridView, prop); 
 
                        // 7. DataGridView.Columns.Add( new DataGridViewColumns)
                        for (int i = 0; i < newColumns.Length; i ++) 
                        {
                            // wipe out the DisplayIndex
                            PropertyDescriptor pd = TypeDescriptor.GetProperties(newColumns[i])["DisplayIndex"];
                            if (pd != null) { 
                                pd.SetValue(newColumns[i], -1);
                            } 
 
                            this.liveDataGridView.Columns.Add(newColumns[i]);
                        } 

                        // 8. OnComponentChanged DataGridView.Columns
                        changeService.OnComponentChanged(this.liveDataGridView, prop, null, null);
                        for (int i = 0; i < userAddedColumnsInfo.Length; i ++) 
                        {
                            PropertyDescriptor pd = TypeDescriptor.GetProperties(newColumns[i])["UserAddedColumn"]; 
                            if (pd != null) 
                            {
                                pd.SetValue(newColumns[i], userAddedColumnsInfo[i]); 
                            }
                        }
                }
                catch (System.InvalidOperationException ex) 
                {
                    IUIService uiService = (IUIService) this.liveDataGridView.Site.GetService(typeof(IUIService)); 
                    DataGridViewDesigner.ShowErrorDialog(uiService, ex, this.liveDataGridView); 
                    this.DialogResult = DialogResult.Cancel;
                } 
            }
        }

        private void componentChanged(object sender, ComponentChangedEventArgs e) 
        {
            if (e.Component is ListBoxItem && this.selectedColumns.Items.Contains(e.Component)) 
            { 
                this.formIsDirty = true;
            } 
        }

        private static void CopyDataGridViewColumnProperties(DataGridViewColumn srcColumn, DataGridViewColumn destColumn)
        { 
            destColumn.AutoSizeMode = srcColumn.AutoSizeMode;
            destColumn.ContextMenuStrip = srcColumn.ContextMenuStrip; 
            destColumn.DataPropertyName = srcColumn.DataPropertyName; 
            if (srcColumn.HasDefaultCellStyle)
            { 
                CopyDefaultCellStyle(srcColumn, destColumn);
            }
            destColumn.DividerWidth = srcColumn.DividerWidth;
            destColumn.HeaderText = srcColumn.HeaderText; 
            destColumn.MinimumWidth = srcColumn.MinimumWidth;
            destColumn.Name = srcColumn.Name; 
            destColumn.SortMode = srcColumn.SortMode; 
            destColumn.Tag = srcColumn.Tag;
            destColumn.ToolTipText = srcColumn.ToolTipText; 
            destColumn.Width = srcColumn.Width;
            destColumn.FillWeight = srcColumn.FillWeight;
        }
 
        private static void CopyDataGridViewColumnState(DataGridViewColumn srcColumn, DataGridViewColumn destColumn)
        { 
            destColumn.Frozen = srcColumn.Frozen; 
            destColumn.Visible = srcColumn.Visible;
            destColumn.ReadOnly = srcColumn.ReadOnly; 
            destColumn.Resizable = srcColumn.Resizable;
        }

        // We don't have any control over the srcColumn constructor. 
        // So we do a catch all.
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
        private static void CopyDefaultCellStyle(DataGridViewColumn srcColumn, DataGridViewColumn destColumn) 
        {
            // Here is what we want to do ( see vsw 352177 for more details ): 
            // 1. If srcColumn and destColumn have the same type simply copy the default cell style from source to destination
            //  and be done w/ it.
            // 2. Otherwise, determine which properties in the cell style are no longer default and copy those properties.
            //      To do that we need to: 
            //      2.a Create a default srcColumn so we get its default cell style. If we get an exception when we are creating the default cell style
            //      then we copy all the public properties. 
            //      2.b Go thru the public properties in the DataGridViewCellStyle and copy only the property that are changed from the default values; 
            //      2.c We need to special case the DataGridViewCellStyle::NullValue property. This property will be copied only if the NullValue
            //      has the same type in destColumn and in srcColumn. 

            Type srcType = srcColumn.GetType();
            Type destType = destColumn.GetType();
 
            // 1. If srcColumn and destColumn have the same type simply copy the default cell style from source to destination
            //  and be done w/ it. 
            if (srcType.IsAssignableFrom(destType) || destType.IsAssignableFrom(srcType)) 
            {
                destColumn.DefaultCellStyle = srcColumn.DefaultCellStyle; 
                return;
            }

            //      2.a Create a default srcColumn so we get its default cell style. If we get an exception when we are creating the default cell style 
            //      then we copy all the public properties.
            DataGridViewColumn defaultSrcColumn = null; 
            try 
            {
                defaultSrcColumn = System.Activator.CreateInstance(srcType) as DataGridViewColumn; 
            }
            catch(Exception e) {
                if (ClientUtils.IsCriticalException(e)) {
                    throw; 
                }
                defaultSrcColumn = null; 
            } 
            catch
            { 
                defaultSrcColumn = null;
            }

            //      2.b Go thru the public properties in the DataGridViewCellStyle and copy only the property that are changed from the default values; 
            if (defaultSrcColumn == null || defaultSrcColumn.DefaultCellStyle.Alignment != srcColumn.DefaultCellStyle.Alignment)
            { 
                destColumn.DefaultCellStyle.Alignment = srcColumn.DefaultCellStyle.Alignment; 
            }
            if (defaultSrcColumn == null || !defaultSrcColumn.DefaultCellStyle.BackColor.Equals(srcColumn.DefaultCellStyle.BackColor)) 
            {
                destColumn.DefaultCellStyle.BackColor = srcColumn.DefaultCellStyle.BackColor;
            }
            if (defaultSrcColumn != null && srcColumn.DefaultCellStyle.Font != null && !srcColumn.DefaultCellStyle.Font.Equals(defaultSrcColumn.DefaultCellStyle.Font)) 
            {
                destColumn.DefaultCellStyle.Font = srcColumn.DefaultCellStyle.Font ; 
            } 
            if (defaultSrcColumn == null || !defaultSrcColumn.DefaultCellStyle.ForeColor.Equals(srcColumn.DefaultCellStyle.ForeColor))
            { 
                destColumn.DefaultCellStyle.ForeColor = srcColumn.DefaultCellStyle.ForeColor;
            }
            if (defaultSrcColumn == null || !defaultSrcColumn.DefaultCellStyle.Format.Equals(srcColumn.DefaultCellStyle.Format))
            { 
                destColumn.DefaultCellStyle.Format = srcColumn.DefaultCellStyle.Format;
            } 
            if (defaultSrcColumn == null || defaultSrcColumn.DefaultCellStyle.Padding != srcColumn.DefaultCellStyle.Padding) 
            {
                destColumn.DefaultCellStyle.Padding = srcColumn.DefaultCellStyle.Padding; 
            }
            if (defaultSrcColumn == null || !defaultSrcColumn.DefaultCellStyle.SelectionBackColor.Equals(srcColumn.DefaultCellStyle.SelectionBackColor))
            {
                destColumn.DefaultCellStyle.SelectionBackColor = srcColumn.DefaultCellStyle.SelectionBackColor; 
            }
            if (defaultSrcColumn == null || !defaultSrcColumn.DefaultCellStyle.SelectionForeColor.Equals(srcColumn.DefaultCellStyle.SelectionForeColor)) 
            { 
                destColumn.DefaultCellStyle.SelectionForeColor = srcColumn.DefaultCellStyle.SelectionForeColor;
            } 
            if (defaultSrcColumn == null || defaultSrcColumn.DefaultCellStyle.WrapMode != srcColumn.DefaultCellStyle.WrapMode)
            {
                destColumn.DefaultCellStyle.WrapMode = srcColumn.DefaultCellStyle.WrapMode;
            } 
            //      2.c We need to special case the DataGridViewCellStyle::NullValue property. This property will be copied only if the NullValue
            //      has the same type in destColumn and in srcColumn. 
            if (!srcColumn.DefaultCellStyle.IsNullValueDefault) 
            {
                object srcNullValue = srcColumn.DefaultCellStyle.NullValue; 
                object destNullValue = destColumn.DefaultCellStyle.NullValue;

                if (srcNullValue != null && destNullValue != null && srcNullValue.GetType() == destNullValue.GetType())
                { 
                    destColumn.DefaultCellStyle.NullValue = srcNullValue;
                } 
            } 
        }
 
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing ) 
        {
            if( disposing ) 
            { 
                if (components != null)
                { 
                    components.Dispose();
                }
            }
            base.Dispose( disposing ); 
        }
 
        private void FixColumnCollectionDisplayIndices() 
        {
            for (int i = 0; i < this.columnsPrivateCopy.Count; i ++) 
            {
                this.columnsPrivateCopy[i].DisplayIndex = i;
            }
        } 

        private void HookComponentChangedEventHandler(IComponentChangeService componentChangeService) 
        { 
            if (componentChangeService != null)
            { 
                componentChangeService.ComponentChanged += new ComponentChangedEventHandler(this.componentChanged);
            }
        }
 
        #region Windows Form Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary> 
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataGridViewColumnCollectionDialog));
            this.addButton = new System.Windows.Forms.Button(); 
            this.deleteButton = new System.Windows.Forms.Button();
            this.moveDown = new System.Windows.Forms.Button(); 
            this.moveUp = new System.Windows.Forms.Button(); 
            this.selectedColumns = new System.Windows.Forms.ListBox();
            this.overarchingTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this.addRemoveTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.selectedColumnsLabel = new System.Windows.Forms.Label();
            this.propertyGridLabel = new System.Windows.Forms.Label();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid(); 
            this.okCancelTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.cancelButton = new System.Windows.Forms.Button(); 
            this.okButton = new System.Windows.Forms.Button(); 
            this.overarchingTableLayoutPanel.SuspendLayout();
            this.addRemoveTableLayoutPanel.SuspendLayout(); 
            this.okCancelTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // addButton 
            //
            resources.ApplyResources(this.addButton, "addButton"); 
            this.addButton.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0); 
            this.addButton.Name = "addButton";
            this.addButton.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0); 
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            //
            // deleteButton
            // 
            resources.ApplyResources(this.deleteButton, "deleteButton");
            this.deleteButton.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0); 
            this.deleteButton.Name = "deleteButton"; 
            this.deleteButton.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click); 
            //
            // moveDown
            //
            resources.ApplyResources(this.moveDown, "moveDown"); 
            this.moveDown.Margin = new System.Windows.Forms.Padding(0, 1, 18, 0);
            this.moveDown.Name = "moveDown"; 
            this.moveDown.Click += new System.EventHandler(this.moveDown_Click); 
            //
            // moveUp 
            //
            resources.ApplyResources(this.moveUp, "moveUp");
            this.moveUp.Margin = new System.Windows.Forms.Padding(0, 0, 18, 1);
            this.moveUp.Name = "moveUp"; 
            this.moveUp.Click += new System.EventHandler(this.moveUp_Click);
            // 
            // selectedColumns 
            //
            resources.ApplyResources(this.selectedColumns, "selectedColumns"); 
            this.selectedColumns.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.selectedColumns.Margin = new System.Windows.Forms.Padding(0, 2, 3, 3);
            this.selectedColumns.Name = "selectedColumns";
            this.overarchingTableLayoutPanel.SetRowSpan(this.selectedColumns, 2); 
            this.selectedColumns.SelectedIndexChanged += new System.EventHandler(this.selectedColumns_SelectedIndexChanged);
            this.selectedColumns.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.selectedColumns_KeyPress); 
            this.selectedColumns.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.selectedColumns_DrawItem); 
            this.selectedColumns.KeyUp += new System.Windows.Forms.KeyEventHandler(this.selectedColumns_KeyUp);
            // 
            // overarchingTableLayoutPanel
            //
            resources.ApplyResources(this.overarchingTableLayoutPanel, "overarchingTableLayoutPanel");
            this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle()); 
            this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(SizeType.Percent)); 
            this.overarchingTableLayoutPanel.Controls.Add(this.addRemoveTableLayoutPanel, 0, 3); 
            this.overarchingTableLayoutPanel.Controls.Add(this.moveUp, 1, 1);
            this.overarchingTableLayoutPanel.Controls.Add(this.selectedColumnsLabel, 0, 0); 
            this.overarchingTableLayoutPanel.Controls.Add(this.moveDown, 1, 2);
            this.overarchingTableLayoutPanel.Controls.Add(this.propertyGridLabel, 2, 0);
            this.overarchingTableLayoutPanel.Controls.Add(this.selectedColumns, 0, 1);
            this.overarchingTableLayoutPanel.Controls.Add(this.propertyGrid1, 2, 1); 
            this.overarchingTableLayoutPanel.Controls.Add(this.okCancelTableLayoutPanel, 0, 4);
            this.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"; 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            //
            // addRemoveTableLayoutPanel
            // 
            resources.ApplyResources(this.addRemoveTableLayoutPanel, "addRemoveTableLayoutPanel");
            this.addRemoveTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this.addRemoveTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this.addRemoveTableLayoutPanel.Controls.Add(this.addButton, 0, 0);
            this.addRemoveTableLayoutPanel.Controls.Add(this.deleteButton, 1, 0); 
            this.addRemoveTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
            this.addRemoveTableLayoutPanel.Name = "addRemoveTableLayoutPanel";
            this.addRemoveTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            // 
            // selectedColumnsLabel
            // 
            resources.ApplyResources(this.selectedColumnsLabel, "selectedColumnsLabel"); 
            this.selectedColumnsLabel.Margin = new System.Windows.Forms.Padding(0);
            this.selectedColumnsLabel.Name = "selectedColumnsLabel"; 
            //
            // propertyGridLabel
            //
            resources.ApplyResources(this.propertyGridLabel, "propertyGridLabel"); 
            this.propertyGridLabel.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.propertyGridLabel.Name = "propertyGridLabel"; 
            // 
            // propertyGrid1
            // 
            resources.ApplyResources(this.propertyGrid1, "propertyGrid1");
            this.propertyGrid1.LineColor = System.Drawing.SystemColors.ScrollBar;
            this.propertyGrid1.Margin = new System.Windows.Forms.Padding(3, 2, 0, 3);
            this.propertyGrid1.Name = "propertyGrid1"; 
            this.overarchingTableLayoutPanel.SetRowSpan(this.propertyGrid1, 3);
            this.propertyGrid1.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGrid1_PropertyValueChanged); 
            // 
            // okCancelTableLayoutPanel
            // 
            resources.ApplyResources(this.okCancelTableLayoutPanel, "okCancelTableLayoutPanel");
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F)); 
            this.okCancelTableLayoutPanel.Controls.Add(this.cancelButton, 1, 0);
            this.okCancelTableLayoutPanel.Controls.Add(this.okButton, 0, 0); 
            this.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel"; 
            this.overarchingTableLayoutPanel.SetColumnSpan(this.okCancelTableLayoutPanel, 3);
            this.okCancelTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            //
            // cancelButton
            //
            resources.ApplyResources(this.cancelButton, "cancelButton"); 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0); 
            this.cancelButton.Name = "cancelButton"; 
            this.cancelButton.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            // 
            // okButton
            //
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK; 
            this.okButton.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.okButton.Name = "okButton"; 
            this.okButton.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0); 
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // DataGridViewColumnCollectionDialog
            //
            this.AcceptButton = this.okButton;
            resources.ApplyResources(this, "$this"); 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton; 
            this.Controls.Add(this.overarchingTableLayoutPanel); 
            this.HelpButton = true;
            this.MaximizeBox = false; 
            this.MinimizeBox = false;
            this.Name = "DataGridViewColumnCollectionDialog";
            this.Padding = new System.Windows.Forms.Padding(12);
            this.ShowIcon = false; 
            this.ShowInTaskbar = false;
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.DataGridViewColumnCollectionDialog_HelpButtonClicked); 
            this.Closed += new System.EventHandler(this.DataGridViewColumnCollectionDialog_Closed); 
            this.Load += new System.EventHandler(this.DataGridViewColumnCollectionDialog_Load);
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.DataGridViewColumnCollectionDialog_HelpRequested); 
            this.overarchingTableLayoutPanel.ResumeLayout(false);
            this.overarchingTableLayoutPanel.PerformLayout();
            this.addRemoveTableLayoutPanel.ResumeLayout(false);
            this.addRemoveTableLayoutPanel.PerformLayout(); 
            this.okCancelTableLayoutPanel.ResumeLayout(false);
            this.okCancelTableLayoutPanel.PerformLayout(); 
            this.ResumeLayout(false); 

        } 
        #endregion

        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        private static bool IsColumnAddedByUser(DataGridViewColumn col) 
        {
            PropertyDescriptor pd = TypeDescriptor.GetProperties(col)["UserAddedColumn"]; 
            if (pd != null) 
            {
                return (bool) pd.GetValue(col); 
            }
            else
            {
                return false; 
            }
        } 
 
        private void okButton_Click(object sender, System.EventArgs e)
        { 
            CommitChanges();
        }

        private void moveDown_Click(object sender, System.EventArgs e) 
        {
            int selectedIndex = this.selectedColumns.SelectedIndex; 
            Debug.Assert(selectedIndex > -1 && selectedIndex < this.selectedColumns.Items.Count - 1); 

            this.columnCollectionChanging = true; 
            try
            {
                ListBoxItem item = (ListBoxItem) this.selectedColumns.SelectedItem;
                this.selectedColumns.Items.RemoveAt(selectedIndex); 
                this.selectedColumns.Items.Insert(selectedIndex + 1, item);
 
                // now do the same thing to the column collection 
                this.columnsPrivateCopy.RemoveAt(selectedIndex);
 
                // if the column we moved was frozen, make sure the column below is frozen too
                if (item.DataGridViewColumn.Frozen)
                {
                    this.columnsPrivateCopy[selectedIndex].Frozen = true; 
                    #if DEBUG
                    // sanity check 
                    for (int i = 0; i < selectedIndex; i ++) 
                    {
                        Debug.Assert(this.columnsPrivateCopy[i].Frozen, "MOVE_DOWN : all the columns up to the one we moved should be frozen"); 
                    }
                    #endif // DEBUG
                }
 
                // wipe out the DisplayIndex
                item.DataGridViewColumn.DisplayIndex = -1; 
                this.columnsPrivateCopy.Insert(selectedIndex + 1, item.DataGridViewColumn); 

                // properties like DataGridViewColumn::Frozen are dependent on the DisplayIndex 
                FixColumnCollectionDisplayIndices();
            }
            finally
            { 
                this.columnCollectionChanging = false;
            } 
 
            this.formIsDirty = true;
            this.selectedColumns.SelectedIndex = selectedIndex + 1; 
            this.moveUp.Enabled = this.selectedColumns.SelectedIndex > 0;
            this.moveDown.Enabled = this.selectedColumns.SelectedIndex < this.selectedColumns.Items.Count - 1;
        }
 
        private void moveUp_Click(object sender, System.EventArgs e)
        { 
            int selectedIndex = this.selectedColumns.SelectedIndex; 
            Debug.Assert(selectedIndex > 0);
 
            this.columnCollectionChanging = true;
            try
            {
                ListBoxItem item = (ListBoxItem) this.selectedColumns.Items[selectedIndex - 1]; 
                this.selectedColumns.Items.RemoveAt(selectedIndex - 1);
                this.selectedColumns.Items.Insert(selectedIndex, item); 
 
                // now do the same thing to the column collection
                this.columnsPrivateCopy.RemoveAt(selectedIndex - 1); 

                // we want to keep the Frozen value of the column we move intact
                // if we move up an UnFrozen column and the column above the one we move is Frozen
                // then we need to make the column above the one we move UnFrozen, too 
                //
                // columnsPrivateCopy[selectedIndex - 1] points to the column we just moved 
                // 
                if (item.DataGridViewColumn.Frozen && !this.columnsPrivateCopy[selectedIndex - 1].Frozen)
                { 
                    item.DataGridViewColumn.Frozen = false;
                }

                // wipe out the display index. 
                item.DataGridViewColumn.DisplayIndex = -1;
                this.columnsPrivateCopy.Insert(selectedIndex, item.DataGridViewColumn); 
 
                // properties like DataGridViewColumn::Frozen are dependent on the DisplayIndex
                FixColumnCollectionDisplayIndices(); 
            }
            finally
            {
                this.columnCollectionChanging = false; 
            }
 
            this.formIsDirty = true; 
            this.selectedColumns.SelectedIndex = selectedIndex - 1;
            this.moveUp.Enabled = this.selectedColumns.SelectedIndex > 0; 
            this.moveDown.Enabled = this.selectedColumns.SelectedIndex < this.selectedColumns.Items.Count - 1;

            // vsw 495403: keep the selected item visible.
            // For some reason, we only have to do this when we move a column up. 
            // When we move a column down or when we delete a column, the selected item remains visible.
            if (this.selectedColumns.SelectedIndex != -1 && this.selectedColumns.TopIndex > this.selectedColumns.SelectedIndex) { 
                this.selectedColumns.TopIndex = this.selectedColumns.SelectedIndex; 
            }
        } 

        private void DataGridViewColumnCollectionDialog_Closed(object sender, System.EventArgs e)
        {
            // scrub the TypeDescriptor association between DataGridViewColumns and their designers 
            for (int i = 0; i < this.selectedColumns.Items.Count; i ++)
            { 
                ListBoxItem lbi = this.selectedColumns.Items[i] as ListBoxItem; 
                if (lbi.DataGridViewColumnDesigner != null)
                { 
                    TypeDescriptor.RemoveAssociation(lbi.DataGridViewColumn, lbi.DataGridViewColumnDesigner);
                }
            }
            this.columnsNames = null; 
            this.userAddedColumns = null;
        } 
 
        private void DataGridViewColumnCollectionDialog_HelpButtonClicked(object sender, CancelEventArgs e)
        { 
            e.Cancel = true;
            DataGridViewColumnCollectionDialog_HelpRequestHandled();
        }
 
        private void DataGridViewColumnCollectionDialog_HelpRequested(object sender, System.Windows.Forms.HelpEventArgs e)
        { 
            DataGridViewColumnCollectionDialog_HelpRequestHandled(); 
            e.Handled = true;
        } 

        private void DataGridViewColumnCollectionDialog_HelpRequestHandled()
        {
            IHelpService helpService = this.liveDataGridView.Site.GetService(iHelpServiceType) as IHelpService; 
            if (helpService != null)
            { 
                helpService.ShowHelpFromKeyword("vs.DataGridViewColumnCollectionDialog"); 
            }
        } 

        private void DataGridViewColumnCollectionDialog_Load(object sender, EventArgs e)
        {
            // get the Dialog Font 
            //
            Font uiFont = Control.DefaultFont; 
            IUIService uiService = (IUIService) this.liveDataGridView.Site.GetService(iUIServiceType); 
            if (uiService != null) {
                uiFont = (Font) uiService.Styles["DialogFont"]; 
            }
            this.Font = uiFont;

            // keep the selected index to 0 or -1 if there are no selected columns 
            this.selectedColumns.SelectedIndex = Math.Min(0, this.selectedColumns.Items.Count - 1);
 
            this.moveUp.Enabled = this.selectedColumns.SelectedIndex > 0; 
            this.moveDown.Enabled = this.selectedColumns.SelectedIndex < this.selectedColumns.Items.Count - 1;
            this.deleteButton.Enabled = this.selectedColumns.Items.Count > 0 && this.selectedColumns.SelectedIndex != -1; 
            this.propertyGrid1.SelectedObject = this.selectedColumns.SelectedItem;

            this.selectedColumns.ItemHeight = this.Font.Height + OWNERDRAWVERTICALBUFFER;
 
            this.ActiveControl = this.selectedColumns;
 
            this.SetSelectedColumnsHorizontalExtent(); 

            this.selectedColumns.Focus(); 

            formIsDirty = false;
        }
 
        private void deleteButton_Click(object sender, System.EventArgs e)
        { 
            Debug.Assert(this.selectedColumns.SelectedIndex != -1); 
            int selectedIndex = this.selectedColumns.SelectedIndex;
 
            this.columnsNames.Remove(this.columnsPrivateCopy[selectedIndex]);
            this.userAddedColumns.Remove(this.columnsPrivateCopy[selectedIndex]);

            this.columnsPrivateCopy.RemoveAt(selectedIndex); 

            // try to keep the same selected index 
            this.selectedColumns.SelectedIndex = Math.Min(this.selectedColumns.Items.Count - 1, selectedIndex); 

            this.moveUp.Enabled = this.selectedColumns.SelectedIndex > 0; 
            this.moveDown.Enabled = this.selectedColumns.SelectedIndex < this.selectedColumns.Items.Count - 1;
            this.deleteButton.Enabled = this.selectedColumns.Items.Count > 0 && this.selectedColumns.SelectedIndex != -1;
            this.propertyGrid1.SelectedObject = this.selectedColumns.SelectedItem;
        } 

        private void addButton_Click(object sender, System.EventArgs e) 
        { 
            int insertIndex;
            if (this.selectedColumns.SelectedIndex == -1) 
            {
                insertIndex = this.selectedColumns.Items.Count;
            }
            else 
            {
                insertIndex = this.selectedColumns.SelectedIndex + 1; 
            } 

            if (this.addColumnDialog == null) 
            {
                this.addColumnDialog = new DataGridViewAddColumnDialog(this.columnsPrivateCopy, this.liveDataGridView);
                this.addColumnDialog.StartPosition = FormStartPosition.CenterParent;
            } 

            this.addColumnDialog.Start(insertIndex, false /*persistChangesToDesigner*/); 
 
            this.addColumnDialog.ShowDialog(this);
        } 

        private void PopulateSelectedColumns()
        {
            int selectedIndex = this.selectedColumns.SelectedIndex; 

            // scrub the TypeDescriptor association between DataGridViewColumns and their designers 
            for (int i = 0; i < this.selectedColumns.Items.Count; i ++) 
            {
                ListBoxItem lbi = this.selectedColumns.Items[i] as ListBoxItem; 
                if (lbi.DataGridViewColumnDesigner != null)
                {
                    TypeDescriptor.RemoveAssociation(lbi.DataGridViewColumn, lbi.DataGridViewColumnDesigner);
                } 
            }
 
            this.selectedColumns.Items.Clear(); 
            ITypeResolutionService tr = (ITypeResolutionService) this.liveDataGridView.Site.GetService(iTypeResolutionServiceType);
 
            for (int i = 0; i < this.columnsPrivateCopy.Count; i ++)
            {
                ComponentDesigner columnDesigner = System.Windows.Forms.Design.DataGridViewAddColumnDialog.GetComponentDesignerForType(tr, this.columnsPrivateCopy[i].GetType());
                this.selectedColumns.Items.Add(new ListBoxItem(this.columnsPrivateCopy[i], this, columnDesigner)); 
            }
 
            this.selectedColumns.SelectedIndex = Math.Min(selectedIndex, this.selectedColumns.Items.Count - 1); 

            SetSelectedColumnsHorizontalExtent(); 

            if (this.selectedColumns.Items.Count == 0)
            {
                this.propertyGridLabel.Text = SR.GetString(SR.DataGridViewProperties); 
            }
        } 
 
        private void propertyGrid1_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        { 
            if (!this.columnCollectionChanging)
            {
                this.formIsDirty = true;
                // refresh the selected columns when the user changed the HeaderText property 
                if (e.ChangedItem.PropertyDescriptor.Name.Equals("HeaderText"))
                { 
                    // invalidate the selected index only 
                    int selectedIndex = this.selectedColumns.SelectedIndex;
                    Debug.Assert(selectedIndex != -1, "we forgot to take away the selected object from the property grid"); 
                    Rectangle bounds = new Rectangle(0, selectedIndex * this.selectedColumns.ItemHeight, this.selectedColumns.Width, this.selectedColumns.ItemHeight);
                    this.columnCollectionChanging = true;
                    try
                    { 
                        // for accessibility reasons, we need to reset the item in the selected columns collection.
                        this.selectedColumns.Items[selectedIndex] = this.selectedColumns.Items[selectedIndex]; 
                    } 
                    finally
                    { 
                        this.columnCollectionChanging = false;
                    }

                    this.selectedColumns.Invalidate(bounds); 

                    // if the header text changed make sure that we update the selected columns HorizontalExtent 
                    this.SetSelectedColumnsHorizontalExtent(); 
                }
                else if (e.ChangedItem.PropertyDescriptor.Name.Equals("DataPropertyName")) 
                {
                    DataGridViewColumn col = (DataGridViewColumn) ((ListBoxItem) this.selectedColumns.SelectedItem).DataGridViewColumn;

                    if (String.IsNullOrEmpty(col.DataPropertyName)) 
                    {
                        this.propertyGridLabel.Text = SR.GetString(SR.DataGridViewUnboundColumnProperties); 
                    } 
                    else
                    { 
                        this.propertyGridLabel.Text = SR.GetString(SR.DataGridViewBoundColumnProperties);
                    }
                }
                else if (e.ChangedItem.PropertyDescriptor.Name.Equals("Name")) 
                {
                    DataGridViewColumn col = (DataGridViewColumn) ((ListBoxItem) this.selectedColumns.SelectedItem).DataGridViewColumn; 
                    this.columnsNames[col] = col.Name; 
                }
            } 
        }

        private void selectedColumns_DrawItem(object sender, DrawItemEventArgs e)
        { 
            if (e.Index < 0)
            { 
                return; 
            }
 
            ListBoxItem lbi = this.selectedColumns.Items[e.Index] as ListBoxItem;

#if DGV_DITHERING
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected) 
            {
                ImageAttributes attr = new ImageAttributes(); 
 
                colorMap[0].OldColor = Color.White;
                colorMap[0].NewColor = e.BackColor; 

                //
                // TO DO : DITHER
                // 
                attr.SetRemapTable(colorMap, ColorAdjustType.Bitmap);
 
                Rectangle imgRectangle = new Rectangle(e.Bounds.X + OWNERDRAWITEMIMAGEBUFFER, e.Bounds.Y + OWNERDRAWITEMIMAGEBUFFER, lbi.ToolboxBitmap.Width, lbi.ToolboxBitmap.Height); 
                e.Graphics.DrawImage(lbi.ToolboxBitmap,
                                     imgRectangle, 
                                     0,
                                     0,
                                     imgRectangle.Width,
                                     imgRectangle.Height, 
                                     GraphicsUnit.Pixel,
                                     attr); 
                attr.Dispose(); 
            }
            else 
            {
#endif // DGV_DITHERING
            e.Graphics.DrawImage(lbi.ToolboxBitmap,
                                 e.Bounds.X + OWNERDRAWITEMIMAGEBUFFER, 
                                 e.Bounds.Y + OWNERDRAWITEMIMAGEBUFFER,
                                 lbi.ToolboxBitmap.Width, 
                                 lbi.ToolboxBitmap.Height); 

            Rectangle bounds = e.Bounds; 
            bounds.Width -= lbi.ToolboxBitmap.Width + 2*OWNERDRAWITEMIMAGEBUFFER;
            bounds.X += lbi.ToolboxBitmap.Width + 2*OWNERDRAWITEMIMAGEBUFFER;
            bounds.Y += OWNERDRAWITEMIMAGEBUFFER;
            bounds.Height -= 2 * OWNERDRAWITEMIMAGEBUFFER; 

            Brush selectedBrush = new System.Drawing.SolidBrush(e.BackColor); 
            Brush foreBrush = new System.Drawing.SolidBrush(e.ForeColor); 
            Brush backBrush = new System.Drawing.SolidBrush(this.selectedColumns.BackColor);
 
            string columnName = ((ListBoxItem) this.selectedColumns.Items[e.Index]).ToString();

            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            { 
                // first get the text rectangle
                int textWidth = Size.Ceiling(e.Graphics.MeasureString(columnName, e.Font, new SizeF(bounds.Width, bounds.Height))).Width; 
                // [....]: the spec calls for + 7 but I think that + 3 does the trick better 
                Rectangle focusRectangle = new Rectangle(bounds.X, e.Bounds.Y + 1, textWidth + OWNERDRAWHORIZONTALBUFFER, e.Bounds.Height - 2);
 
                e.Graphics.FillRectangle(selectedBrush, focusRectangle);
                focusRectangle.Inflate(-1, -1);

                e.Graphics.DrawString(columnName, e.Font, foreBrush, focusRectangle); 

                focusRectangle.Inflate(1, 1); 
 
                // only paint the focus rectangle when the list box is focused
                if (this.selectedColumns.Focused) { 
                    ControlPaint.DrawFocusRectangle(e.Graphics, focusRectangle, e.ForeColor, e.BackColor);
                }

                e.Graphics.FillRectangle(backBrush, new Rectangle(focusRectangle.Right + 1, e.Bounds.Y, e.Bounds.Width - focusRectangle.Right - 1, e.Bounds.Height)); 

            } 
            else 
            {
                e.Graphics.FillRectangle(backBrush, new Rectangle(bounds.X, e.Bounds.Y, e.Bounds.Width - bounds.X, e.Bounds.Height)); 

                e.Graphics.DrawString(columnName, e.Font, foreBrush, bounds);
            }
 
            selectedBrush.Dispose();
            backBrush.Dispose(); 
            foreBrush.Dispose(); 
        }
 
        private void selectedColumns_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Modifiers) == 0 && e.KeyCode == Keys.F4)
            { 
                this.propertyGrid1.Focus();
                e.Handled = true; 
            } 
        }
 
        private void selectedColumns_KeyPress(object sender, KeyPressEventArgs e)
        {
            Keys modifierKeys = System.Windows.Forms.Control.ModifierKeys;
 
            // vsw 479960.
            // Don't let Ctrl-* propagate to the selected columns list box. 
            if ((modifierKeys & Keys.Control) != 0) 
            {
                e.Handled = true; 
            }
        }

        private void selectedColumns_SelectedIndexChanged(object sender, System.EventArgs e) 
        {
            if (this.columnCollectionChanging) 
            { 
                return;
            } 

            this.propertyGrid1.SelectedObject = this.selectedColumns.SelectedItem;

            // enable/disable up/down/delete buttons 
            this.moveDown.Enabled = this.selectedColumns.Items.Count > 0 && this.selectedColumns.SelectedIndex != this.selectedColumns.Items.Count - 1;
            this.moveUp.Enabled = this.selectedColumns.Items.Count > 0 && this.selectedColumns.SelectedIndex > 0; 
            this.deleteButton.Enabled = this.selectedColumns.Items.Count > 0 && this.selectedColumns.SelectedIndex != -1; 

            if (this.selectedColumns.SelectedItem != null) 
            {
                DataGridViewColumn column = ((ListBoxItem) this.selectedColumns.SelectedItem).DataGridViewColumn;
                if (String.IsNullOrEmpty(column.DataPropertyName))
                { 
                    this.propertyGridLabel.Text = SR.GetString(SR.DataGridViewUnboundColumnProperties);
                } 
                else 
                {
                    this.propertyGridLabel.Text = SR.GetString(SR.DataGridViewBoundColumnProperties); 
                }

            } else {
                this.propertyGridLabel.Text = SR.GetString(SR.DataGridViewProperties); 
            }
        } 
 
        internal void SetLiveDataGridView(DataGridView dataGridView)
        { 

            IComponentChangeService newComponentChangeService = null;
            if (dataGridView.Site != null)
            { 
                newComponentChangeService = (IComponentChangeService) dataGridView.Site.GetService(iComponentChangeServiceType);
            } 
 
            if (newComponentChangeService != this.compChangeService)
            { 
                UnhookComponentChangedEventHandler(this.compChangeService);

                this.compChangeService = newComponentChangeService;
 
                HookComponentChangedEventHandler(this.compChangeService);
            } 
 
            this.liveDataGridView = dataGridView;
 
            this.dataGridViewPrivateCopy.Site = dataGridView.Site;
            this.dataGridViewPrivateCopy.AutoSizeColumnsMode = dataGridView.AutoSizeColumnsMode;
            this.dataGridViewPrivateCopy.DataSource = dataGridView.DataSource;
            this.dataGridViewPrivateCopy.DataMember = dataGridView.DataMember; 
            this.columnsNames = new System.Collections.Hashtable(this.columnsPrivateCopy.Count);
            this.columnsPrivateCopy.Clear(); 
 
            this.userAddedColumns = new System.Collections.Hashtable(this.liveDataGridView.Columns.Count);
 
            // Set ColumnCollectionChanging to true so:
            // 1. the column collection changed event handler does not execute PopulateSelectedColumns over and over again.
            // 2. the collection changed event handler does not add each live column to its userAddedColumns hash table.
            // 
            this.columnCollectionChanging = true;
            try 
            { 
                for (int i = 0; i < this.liveDataGridView.Columns.Count; i ++)
                { 
                    DataGridViewColumn liveCol = this.liveDataGridView.Columns[i];
                    DataGridViewColumn col = (DataGridViewColumn) liveCol.Clone();
                    // at design time we need to do a shallow copy for the ContextMenuStrip property
                    // 
                    col.ContextMenuStrip = this.liveDataGridView.Columns[i].ContextMenuStrip;
                    // wipe out the display index before adding the new column. 
                    col.DisplayIndex = -1; 
                    this.columnsPrivateCopy.Add(col);
 
                    if (liveCol.Site != null)
                    {
                        this.columnsNames[col] = liveCol.Site.Name;
                    } 
                    this.userAddedColumns[col] = IsColumnAddedByUser(this.liveDataGridView.Columns[i]);
                } 
            } 
            finally
            { 
                this.columnCollectionChanging = false;
            }

            PopulateSelectedColumns(); 

            this.propertyGrid1.Site = new DataGridViewComponentPropertyGridSite(this.liveDataGridView.Site, this.liveDataGridView); 
 
            this.propertyGrid1.SelectedObject = this.selectedColumns.SelectedItem;
        } 

        private void SetSelectedColumnsHorizontalExtent() {
            int maxItemWidth = 0;
            for (int i = 0; i < this.selectedColumns.Items.Count; i ++) { 
                int itemWidth = TextRenderer.MeasureText(this.selectedColumns.Items[i].ToString(), this.selectedColumns.Font).Width;
                maxItemWidth = Math.Max(maxItemWidth, itemWidth); 
            } 

            this.selectedColumns.HorizontalExtent = this.SelectedColumnsItemBitmap.Width + 2 * OWNERDRAWITEMIMAGEBUFFER + maxItemWidth + OWNERDRAWHORIZONTALBUFFER; 
        }

        private void UnhookComponentChangedEventHandler(IComponentChangeService componentChangeService)
        { 
            if (componentChangeService != null)
            { 
                componentChangeService.ComponentChanged -= new ComponentChangedEventHandler(this.componentChanged); 
            }
        } 

        private static bool ValidateName(IContainer container, string siteName, IComponent component)
        {
            ComponentCollection comps = container.Components; 
            if (comps == null)
            { 
                return true; 
            }
 
            for (int i = 0; i < comps.Count; i ++)
            {
                IComponent comp = comps[i];
                if (comp == null || comp.Site == null) 
                {
                    continue; 
                } 

                ISite s = comp.Site; 

                if (s != null && s.Name != null && string.Equals(s.Name, siteName, StringComparison.OrdinalIgnoreCase) && s.Component != component) {
                    return false;
                } 
            }
 
            return true; 
        }
 
        // internal because the DataGridViewColumnDataPropertyNameEditor needs to get at the ListBoxItem
        // IComponent because some editors for some dataGridViewColumn properties - DataGridViewComboBox::DataSource editor -
        // need the site
        internal class ListBoxItem : ICustomTypeDescriptor, IComponent 
        {
            private DataGridViewColumn column; 
            private DataGridViewColumnCollectionDialog owner; 
            private ComponentDesigner compDesigner;
            private Image toolboxBitmap; 
            public ListBoxItem(DataGridViewColumn column, DataGridViewColumnCollectionDialog owner, ComponentDesigner compDesigner)
            {
                this.column = column;
                this.owner = owner; 
                this.compDesigner = compDesigner;
 
                if (this.compDesigner != null) 
                {
                    this.compDesigner.Initialize(column); 
                    TypeDescriptor.CreateAssociation(this.column, this.compDesigner);
                }

                ToolboxBitmapAttribute attr = TypeDescriptor.GetAttributes(column)[toolboxBitmapAttributeType] as ToolboxBitmapAttribute; 
                if (attr != null)
                { 
                    this.toolboxBitmap = attr.GetImage(column, false /*large*/); 
                }
                else 
                {
                    this.toolboxBitmap = this.owner.SelectedColumnsItemBitmap;
                }
 
                DataGridViewColumnDesigner dgvColumnDesigner = compDesigner as DataGridViewColumnDesigner;
                if (dgvColumnDesigner != null) 
                { 
                    dgvColumnDesigner.LiveDataGridView = this.owner.liveDataGridView;
                } 
            }

            public DataGridViewColumn DataGridViewColumn
            { 
                get
                { 
                    return this.column; 
                }
            } 

            public ComponentDesigner DataGridViewColumnDesigner
            {
                get 
                {
                    return this.compDesigner; 
                } 
            }
 
            public DataGridViewColumnCollectionDialog Owner
            {
                get
                { 
                    return this.owner;
                } 
            } 

            public Image ToolboxBitmap 
            {
                get
                {
                    return this.toolboxBitmap; 
                }
            } 
 
            public override string ToString()
            { 
                return this.column.HeaderText;
            }

            // ICustomTypeDescriptor implementation 
            AttributeCollection ICustomTypeDescriptor.GetAttributes() {
                return TypeDescriptor.GetAttributes(this.column); 
            } 

            string ICustomTypeDescriptor.GetClassName() { 
                return TypeDescriptor.GetClassName(this.column);
            }

            string ICustomTypeDescriptor.GetComponentName() { 
                return TypeDescriptor.GetComponentName(this.column);
            } 
 
            TypeConverter ICustomTypeDescriptor.GetConverter() {
                return TypeDescriptor.GetConverter(this.column); 
            }

            EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() {
                return TypeDescriptor.GetDefaultEvent(this.column); 
            }
 
            PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() { 
                return TypeDescriptor.GetDefaultProperty(this.column);
            } 

            object ICustomTypeDescriptor.GetEditor(Type type) {
                return TypeDescriptor.GetEditor(this.column, type);
            } 

            EventDescriptorCollection ICustomTypeDescriptor.GetEvents() { 
                return TypeDescriptor.GetEvents(this.column); 
            }
 
            EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attrs) {
                return TypeDescriptor.GetEvents(this.column, attrs);
            }
 
            PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() {
                return (((ICustomTypeDescriptor) this).GetProperties(null)); 
            } 

            PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attrs) { 
                PropertyDescriptorCollection props = TypeDescriptor.GetProperties(this.column);

                PropertyDescriptor[] propArray = null;
                if (this.compDesigner != null) 
                {
                    // PropertyDescriptorCollection does not let us change properties. 
                    // So we have to create a hash table that we pass to PreFilterProperties 
                    // and then copy back the result from PreFilterProperties
 
                    // We should look into speeding this up w/ our own DataGridViewColumnTypes...
                    //
                    Hashtable hash = new Hashtable();
                    for (int i = 0; i < props.Count; i ++) 
                    {
                        hash.Add(props[i].Name, props[i]); 
                    } 

                    ((IDesignerFilter) compDesigner).PreFilterProperties(hash); 

                    // PreFilterProperties can add / remove properties.
                    // Use the hashtable's Count, not the old property descriptor collection's count.
                    propArray = new PropertyDescriptor[hash.Count + 1]; 
                    hash.Values.CopyTo(propArray, 0);
                } 
                else 
                {
                    propArray = new PropertyDescriptor[props.Count + 1]; 
                    props.CopyTo(propArray, 0);
                }

                propArray[propArray.Length - 1] = new ColumnTypePropertyDescriptor(); 

                return new PropertyDescriptorCollection(propArray); 
            } 

            object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) { 
                if (pd == null)
                {
                    return this.column;
                } 
                else if (pd is ColumnTypePropertyDescriptor)
                { 
                    return this; 
                }
                else 
                {
                    return this.column;
                }
 
            }
 
            ISite IComponent.Site 
            {
                get 
                {
                    return this.owner.liveDataGridView.Site;
                }
                set 
                {
                } 
            } 

            event EventHandler IComponent.Disposed 
            {
                add
                {
                } 
                remove
                { 
                } 
            }
 
            [
                SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")      // The ListBoxItem does not own the ToolBoxBitmap
                                                                                                   // so it can't dispose it.
            ] 
            void IDisposable.Dispose()
            { 
            } 
        }
 
        private class ColumnTypePropertyDescriptor : PropertyDescriptor
        {
            public ColumnTypePropertyDescriptor() : base("ColumnType", null)
            { 
            }
 
            public override AttributeCollection Attributes 
            {
                get 
                {
                    EditorAttribute editorAttr = new EditorAttribute("System.Windows.Forms.Design.DataGridViewColumnTypeEditor, " + AssemblyRef.SystemDesign, typeof(System.Drawing.Design.UITypeEditor));
                    DescriptionAttribute descriptionAttr = new DescriptionAttribute(SR.GetString(SR.DataGridViewColumnTypePropertyDescription));
                    CategoryAttribute categoryAttr = CategoryAttribute.Design; 
                    // add the description attribute and the categories attribute
                    Attribute[] attrs = new Attribute[] {editorAttr, descriptionAttr, categoryAttr}; 
                    return new AttributeCollection(attrs); 
                }
            } 

            public override Type ComponentType
            {
                get 
                {
                    return typeof(ListBoxItem); 
                } 
            }
 
            public override bool IsReadOnly
            {
                get
                { 
                    return false;
                } 
            } 

            public override Type PropertyType 
            {
                get
                {
                    return typeof(Type); 
                }
            } 
 
            public override bool CanResetValue(object component)
            { 
                Debug.Assert(component is ListBoxItem, "this property descriptor only relates to the data grid view column class");
                return false;
            }
 
            public override object GetValue(object component)
            { 
                if (component == null) 
                {
                    return null; 
                }

                ListBoxItem item = (ListBoxItem) component;
                return item.DataGridViewColumn.GetType().Name; 
            }
 
            public override void ResetValue(object component) 
            {
            } 

            public override void SetValue(object component, object value)
            {
                ListBoxItem item = (ListBoxItem) component; 
                Type type = value as Type;
                if (item.DataGridViewColumn.GetType() != type) 
                { 
                    item.Owner.ColumnTypeChanged(item, type);
                    OnValueChanged(component, EventArgs.Empty); 
                }
            }

            public override bool ShouldSerializeValue(object component) 
            {
                return false; 
            } 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
