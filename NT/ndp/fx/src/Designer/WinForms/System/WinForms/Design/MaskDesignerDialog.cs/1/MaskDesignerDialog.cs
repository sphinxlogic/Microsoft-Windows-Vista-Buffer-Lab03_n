//------------------------------------------------------------------------------ 
// <copyright file="TextBoxBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.Design; 
    using System.Drawing;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Diagnostics; 
    using Microsoft.Win32;
    using System.Globalization; 

    /// <devdoc>
    ///     UI for the MaskTypeEditor (Design time).
    /// </devdoc> 
    internal class MaskDesignerDialog : System.Windows.Forms.Form
    { 
#if DEBUG 
        // Used by test suite to disable popping-up Assert dlg using private reflection in debug builds only.
        private static bool DisableAssertDlg; 
#endif
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.ListView listViewCannedMasks;
        private System.Windows.Forms.CheckBox checkBoxUseValidatingType; 
        private System.Windows.Forms.ColumnHeader maskDescriptionHeader;
        private System.Windows.Forms.ColumnHeader dataFormatHeader; 
        private System.Windows.Forms.ColumnHeader validatingTypeHeader; 
        private System.Windows.Forms.TableLayoutPanel maskTryItTable;
        private System.Windows.Forms.Label lblMask; 
        private System.Windows.Forms.TextBox txtBoxMask;
        private System.Windows.Forms.Label lblTryIt;
        private System.Windows.Forms.MaskedTextBox maskedTextBox;
        private System.Windows.Forms.TableLayoutPanel okCancelTableLayoutPanel; 
        private System.Windows.Forms.TableLayoutPanel overarchingTableLayoutPanel;
        private System.Windows.Forms.Button btnOK; 
        private System.Windows.Forms.Button btnCancel; 
        private System.Windows.Forms.ErrorProvider errorProvider;
 

        private List<MaskDescriptor> maskDescriptors = new List<MaskDescriptor>();
        private MaskDescriptor customMaskDescriptor;
        private SortOrder listViewSortOrder = SortOrder.Ascending; 
        private Type mtpValidatingType;
        private IContainer components; 
        private IHelpService helpService = null; 

 
        /// <devdoc>
        ///     Constructor receiving a clone of the MaskedTextBox control under design.
        /// </devdoc>
        public MaskDesignerDialog(MaskedTextBox instance, IHelpService helpService) { 
            if( instance == null )
            { 
                Debug.Fail( "Null masked text box, creating default." ); 
                this.maskedTextBox = new System.Windows.Forms.MaskedTextBox();
            } 
            else
            {
                this.maskedTextBox = MaskedTextBoxDesigner.GetDesignMaskedTextBox(instance);
            } 

            this.helpService = helpService; 
 
            InitializeComponent();
 
            // Non-designer-handled stuff.
            this.SuspendLayout();

            this.txtBoxMask.Text = this.maskedTextBox.Mask; 

            // Add default mask descriptors to the mask description list. 
            this.AddDefaultMaskDescriptors(this.maskedTextBox.Culture); 

            // 
            // maskDescriptionHeader
            //
            this.maskDescriptionHeader.Text = SR.GetString(SR.MaskDesignerDialogMaskDescription);
            this.maskDescriptionHeader.Width = this.listViewCannedMasks.Width / 3; 
            //
            // dataFormatHeader 
            // 
            this.dataFormatHeader.Text = SR.GetString(SR.MaskDesignerDialogDataFormat);
            this.dataFormatHeader.Width = this.listViewCannedMasks.Width / 3; 
            //
            // validatingTypeHeader
            //
            this.validatingTypeHeader.Text = SR.GetString(SR.MaskDesignerDialogValidatingType); 
            this.validatingTypeHeader.Width = (this.listViewCannedMasks.Width / 3) - SystemInformation.VerticalScrollBarWidth - 4;	// so no h-scrollbar.
            this.ResumeLayout(false); 
 
            HookEvents();
        } 

        private void HookEvents()
        {
            this.listViewCannedMasks.SelectedIndexChanged += new System.EventHandler(this.listViewCannedMasks_SelectedIndexChanged); 
            this.listViewCannedMasks.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listViewCannedMasks_ColumnClick);
            this.listViewCannedMasks.Enter += new EventHandler(listViewCannedMasks_Enter); 
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click); 
            this.txtBoxMask.TextChanged += new System.EventHandler(this.txtBoxMask_TextChanged);
            this.txtBoxMask.Validating += new System.ComponentModel.CancelEventHandler(this.txtBoxMask_Validating); 
            this.maskedTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.maskedTextBox_KeyDown);
            this.maskedTextBox.MaskInputRejected += new System.Windows.Forms.MaskInputRejectedEventHandler(this.maskedTextBox_MaskInputRejected);
            this.Load += new System.EventHandler(this.MaskDesignerDialog_Load);
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.MaskDesignerDialog_HelpButtonClicked); 
        }
 
 
        private void InitializeComponent()
        { 
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MaskDesignerDialog));
            this.lblHeader = new System.Windows.Forms.Label();
            this.listViewCannedMasks = new System.Windows.Forms.ListView(); 
            this.maskDescriptionHeader = new System.Windows.Forms.ColumnHeader(resources.GetString("listViewCannedMasks.Columns"));
            this.dataFormatHeader = new System.Windows.Forms.ColumnHeader(resources.GetString("listViewCannedMasks.Columns1")); 
            this.validatingTypeHeader = new System.Windows.Forms.ColumnHeader(resources.GetString("listViewCannedMasks.Columns2")); 
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button(); 
            this.checkBoxUseValidatingType = new System.Windows.Forms.CheckBox();
            this.maskTryItTable = new System.Windows.Forms.TableLayoutPanel();
            this.lblMask = new System.Windows.Forms.Label();
            this.txtBoxMask = new System.Windows.Forms.TextBox(); 
            this.lblTryIt = new System.Windows.Forms.Label();
            this.overarchingTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this.okCancelTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.maskTryItTable.SuspendLayout(); 
            this.overarchingTableLayoutPanel.SuspendLayout();
            this.okCancelTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout(); 
            //
            // maskedTextBox 
            // 
            resources.ApplyResources(this.maskedTextBox, "maskedTextBox");
            this.maskedTextBox.Margin = new System.Windows.Forms.Padding(3, 3, 18, 0); 
            this.maskedTextBox.Name = "maskedTextBox";
            //
            // lblHeader
            // 
            resources.ApplyResources(this.lblHeader, "lblHeader");
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 3); 
            this.lblHeader.Name = "lblHeader"; 
            //
            // listViewCannedMasks 
            //
            resources.ApplyResources(this.listViewCannedMasks, "listViewCannedMasks");
            this.listViewCannedMasks.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.maskDescriptionHeader, 
            this.dataFormatHeader,
            this.validatingTypeHeader}); 
            this.listViewCannedMasks.FullRowSelect = true; 
            this.listViewCannedMasks.HideSelection = false;
            this.listViewCannedMasks.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3); 
            this.listViewCannedMasks.MultiSelect = false;
            this.listViewCannedMasks.Name = "listViewCannedMasks";
            this.listViewCannedMasks.Sorting = SortOrder.None; // We'll do the sorting ourselves.
            this.listViewCannedMasks.View = System.Windows.Forms.View.Details; 
            //
            // maskDescriptionHeader 
            // 
            resources.ApplyResources(this.maskDescriptionHeader, "maskDescriptionHeader");
            // 
            // dataFormatHeader
            //
            resources.ApplyResources(this.dataFormatHeader, "dataFormatHeader");
            // 
            // validatingTypeHeader
            // 
            resources.ApplyResources(this.validatingTypeHeader, "validatingTypeHeader"); 
            //
            // btnOK 
            //
            resources.ApplyResources(this.btnOK, "btnOK");
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0); 
            this.btnOK.MinimumSize = new System.Drawing.Size(75, 23);
            this.btnOK.Name = "btnOK"; 
            this.btnOK.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0); 
            //
            // btnCancel 
            //
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0); 
            this.btnCancel.MinimumSize = new System.Drawing.Size(75, 23);
            this.btnCancel.Name = "btnCancel"; 
            this.btnCancel.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0); 
            //
            // checkBoxUseValidatingType 
            //
            resources.ApplyResources(this.checkBoxUseValidatingType, "checkBoxUseValidatingType");
            this.checkBoxUseValidatingType.Checked = true;
            this.checkBoxUseValidatingType.CheckState = System.Windows.Forms.CheckState.Checked; 
            this.checkBoxUseValidatingType.Margin = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.checkBoxUseValidatingType.Name = "checkBoxUseValidatingType"; 
            // 
            // maskTryItTable
            // 
            resources.ApplyResources(this.maskTryItTable, "maskTryItTable");
            this.maskTryItTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.maskTryItTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.maskTryItTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle()); 
            this.maskTryItTable.Controls.Add(this.checkBoxUseValidatingType, 2, 0);
            this.maskTryItTable.Controls.Add(this.lblMask, 0, 0); 
            this.maskTryItTable.Controls.Add(this.txtBoxMask, 1, 0); 
            this.maskTryItTable.Controls.Add(this.lblTryIt, 0, 1);
            this.maskTryItTable.Controls.Add(this.maskedTextBox, 1, 1); 
            this.maskTryItTable.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.maskTryItTable.Name = "maskTryItTable";
            this.maskTryItTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.maskTryItTable.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            //
            // lblMask 
            // 
            resources.ApplyResources(this.lblMask, "lblMask");
            this.lblMask.Margin = new System.Windows.Forms.Padding(0, 0, 3, 3); 
            this.lblMask.Name = "lblMask";
            //
            // txtBoxMask
            // 
            resources.ApplyResources(this.txtBoxMask, "txtBoxMask");
            this.txtBoxMask.Margin = new System.Windows.Forms.Padding(3, 0, 18, 3); 
            this.txtBoxMask.Name = "txtBoxMask"; 
            //
            // lblTryIt 
            //
            resources.ApplyResources(this.lblTryIt, "lblTryIt");
            this.lblTryIt.Margin = new System.Windows.Forms.Padding(0, 3, 3, 0);
            this.lblTryIt.Name = "lblTryIt"; 
            //
            // overarchingTableLayoutPanel 
            // 
            resources.ApplyResources(this.overarchingTableLayoutPanel, "overarchingTableLayoutPanel");
            this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this.overarchingTableLayoutPanel.Controls.Add(this.maskTryItTable, 0, 3);
            this.overarchingTableLayoutPanel.Controls.Add(this.okCancelTableLayoutPanel, 0, 4);
            this.overarchingTableLayoutPanel.Controls.Add(this.lblHeader, 0, 1);
            this.overarchingTableLayoutPanel.Controls.Add(this.listViewCannedMasks, 0, 2); 
            this.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel";
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            //
            // okCancelTableLayoutPanel
            // 
            resources.ApplyResources(this.okCancelTableLayoutPanel, "okCancelTableLayoutPanel");
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this.okCancelTableLayoutPanel.Controls.Add(this.btnCancel, 1, 0);
            this.okCancelTableLayoutPanel.Controls.Add(this.btnOK, 0, 0); 
            this.okCancelTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel";
            this.okCancelTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            // 
            // errorProvider
            // 
            this.errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink; 
            this.errorProvider.ContainerControl = this;
            // 
            // MaskDesignerDialog
            //
            resources.ApplyResources(this, "$this");
            this.AcceptButton = this.btnOK; 
            this.CancelButton = this.btnCancel;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font; 
            this.Controls.Add(this.overarchingTableLayoutPanel); 
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.HelpButton = true; 
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MaskDesignerDialog";
            this.ShowInTaskbar = false; 
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.maskTryItTable.ResumeLayout(false); 
            this.maskTryItTable.PerformLayout(); 
            this.overarchingTableLayoutPanel.ResumeLayout(false);
            this.overarchingTableLayoutPanel.PerformLayout(); 
            this.okCancelTableLayoutPanel.ResumeLayout(false);
            this.okCancelTableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.ResumeLayout(false); 

        } 
 
        /// <devdoc>
        ///     The current text (mask) in the txtBoxMask control. 
        /// </devdoc>
        public string Mask
        {
            get 
            {
 				return this.maskedTextBox.Mask; 
			} 
        }
 

		/// <devdoc>
		///     The current text (mask) in the txtBoxMask control.
 		/// </devdoc> 
		public Type ValidatingType
 		{ 
 			get 
			{
 				return this.mtpValidatingType; 
			}
		}

 

		////////// Properties. 
        /// 

        /// <devdoc> 
        ///     A collection of MaskDescriptor objects represented in the ListView with the canned mask
        ///     descriptions.
        /// </devdoc>
        public System.Collections.IEnumerator MaskDescriptors 
        {
            get 
            { 
                return this.maskDescriptors.GetEnumerator();
            } 
        }


        ////////// Methods. 

        /// <devdoc> 
        ///     Adds the default mask descriptors to the mask description list. 
        ///     We need to add the deafult descriptors explicitly because the DiscoverMaskDescriptors method only adds
        ///     public descriptors and these are internal. 
        /// </devdoc>
        private void AddDefaultMaskDescriptors(CultureInfo culture)
        {
            this.customMaskDescriptor = new MaskDescriptorTemplate(null, SR.GetString(SR.MaskDesignerDialogCustomEntry), null, null, null, true); 

            List<MaskDescriptor> maskDescriptors = MaskDescriptorTemplate.GetLocalizedMaskDescriptors(culture); 
 
            // Need to pass false for validateDescriptor param since the custom mask will fail validation
            // because the mask is empty. 
            InsertMaskDescriptor(0, this.customMaskDescriptor, /*validate*/ false);

            foreach( MaskDescriptor maskDescriptor in maskDescriptors )
            { 
                InsertMaskDescriptor(0, maskDescriptor);
            } 
        } 

        /// <devdoc> 
        ///     Determines whether the specified MaskDescriptor object is in the MaskDescriptor collection or not.
        /// </devdoc>
        private bool ContainsMaskDescriptor( MaskDescriptor maskDescriptor )
        { 
            Debug.Assert( maskDescriptor != null, "Null mask descriptor." );
 
            foreach( MaskDescriptor descriptor in this.maskDescriptors ) 
            {
                Debug.Assert( descriptor != null, "Null mask descriptor in the collection." ); 

                if( maskDescriptor.Equals(descriptor) || maskDescriptor.Name.Trim() == descriptor.Name.Trim() )
                {
                    return true; 
                }
            } 
 
            return false;
        } 

        /// <devdoc>
        ///     Uses the specified ITypeDiscoveryService service provider to discover MaskDescriptor objects from
        ///     the referenced assemblies. 
        /// </devdoc>
        public void DiscoverMaskDescriptors( ITypeDiscoveryService discoveryService ) 
        { 
            if (discoveryService != null)
            { 
                ICollection descriptors = DesignerUtils.FilterGenericTypes(discoveryService.GetTypes(typeof(MaskDescriptor), false /* excludeGlobalTypes */));

                // Note: This code assumes DesignerUtils.FilterGenericTypes return a valid ICollection (collection of MaskDescriptor types).
                foreach( Type t in descriptors ) 
                {
                    if (t.IsAbstract || !t.IsPublic) 
                    { 
                        continue;
                    } 

                    // Since mask descriptors can be provided from external sources, we need to guard against
                    // possible exceptions when accessing an external descriptor.
                    try 
                    {
                        MaskDescriptor maskDescriptor = (MaskDescriptor) Activator.CreateInstance( t ); 
                        InsertMaskDescriptor(0, maskDescriptor); 
                    }
                    catch( Exception ex ) 
                    {
                        if( ClientUtils.IsCriticalException( ex ))
                        {
                            throw; 
                        }
#if DEBUG 
                        Debug.Assert(DisableAssertDlg, ex.ToString()); 
#endif
                    } 
                    catch
                    {
                        Debug.Fail("non-CLS compliant exception");
                    } 
                }
            } 
        } 

        /// <devdoc> 
        ///     Gets the index of a mask descriptor in the mask descriptor table.
        /// </devdoc>
        private int GetMaskDescriptorIndex(MaskDescriptor maskDescriptor)
        { 
            for( int index = 0; index < this.maskDescriptors.Count; index++ )
            { 
                MaskDescriptor descriptor = this.maskDescriptors[index]; 

                if( descriptor == maskDescriptor ) 
                {
                    return index;
                }
            } 

            Debug.Fail( "Could not find mask descriptor." ); 
            return -1; 
        }
 
        /// <devdoc>
        ///     Selects the mask descriptor corresponding to the current MaskedTextBox.Mask if any, otherwise the custom entry.
        /// </devdoc>
        private void SelectMtbMaskDescriptor() 
        {
            int selectedItemIdx = -1; 
 
            if( !string.IsNullOrEmpty( this.maskedTextBox.Mask ) )
            { 
                for( int selectedIndex = 0; selectedIndex < this.maskDescriptors.Count; selectedIndex++ )
                {
                    MaskDescriptor descriptor = this.maskDescriptors[selectedIndex];
 
                    if( descriptor.Mask == this.maskedTextBox.Mask && descriptor.ValidatingType == this.maskedTextBox.ValidatingType)
                    { 
                        selectedItemIdx = selectedIndex; 
                        break;
                    } 
                }
            }

            if( selectedItemIdx == -1 ) // select custom mask. 
            {
                selectedItemIdx = GetMaskDescriptorIndex( this.customMaskDescriptor ); 
 
                if( selectedItemIdx == -1 )
                { 
                    Debug.Fail("Could not find custom mask descriptor.");
                }
            }
 
            if( selectedItemIdx != -1 )
            { 
                SetSelectedMaskDescriptor( selectedItemIdx ); 
            }
 
            //if( this.listViewCannedMasks.FocusedItem == null )
            //{
            //    this.listViewCannedMasks.Items[0].Focused = true;  // it is assumed we have the default items in the list.
            //} 
        }
 
        /// <devdoc> 
        ///     Selects the specified item in the ListView.
        /// </devdoc> 
        private void SetSelectedMaskDescriptor( MaskDescriptor maskDex )
        {
            int maskDexIndex = GetMaskDescriptorIndex( maskDex );
            SetSelectedMaskDescriptor( maskDexIndex ); 
        }
        private void SetSelectedMaskDescriptor( int maskDexIndex ) 
        { 
            Debug.Assert( maskDexIndex >= 0, "Invalid index." );
            if( maskDexIndex >= 0 && this.listViewCannedMasks.Items.Count > maskDexIndex ) 
            {
                this.listViewCannedMasks.Items[maskDexIndex].Selected = true;
                this.listViewCannedMasks.FocusedItem = this.listViewCannedMasks.Items[maskDexIndex];
                this.listViewCannedMasks.EnsureVisible( maskDexIndex ); 
            }
        } 
 
        /// <devdoc>
        ///     Sorts the maskDescriptors and the list view items. 
        /// </devdoc>
        private void UpdateSortedListView(MaskDescriptorComparer.SortType sortType )
        {
            if( !this.listViewCannedMasks.IsHandleCreated ) 
            {
                return; 
            } 

            MaskDescriptor selectedMaskDex = null; 

            // Save current selected entry to restore it after sorting.
            if( this.listViewCannedMasks.SelectedItems.Count > 0 )
            { 
                int selectedIndex = this.listViewCannedMasks.SelectedIndices[0];
                selectedMaskDex = this.maskDescriptors[selectedIndex]; 
            } 

            // Custom mask descriptor should always be the last entry - remove it before sorting array. 
            this.maskDescriptors.RemoveAt( this.maskDescriptors.Count - 1 );

            // Sort MaskDescriptor collection.
            this.maskDescriptors.Sort( new MaskDescriptorComparer( sortType, this.listViewSortOrder ) ); 

            // Sorting the ListView items forces handle recreation, since we have the items sorted and know what item to select 
            // it is better for us to replace the items ourselves.  This way also avoids problems with the selected item  and 
            // the custom entry not getting properly added.
            // this.listViewCannedMasks.Sort(); 

            // Since we need to pre-process each item before inserting it in the ListView, it is better to remove all items
            // from it first and then add the sorted ones back (no replace).  Stop redrawing while we change the list.
 
            UnsafeNativeMethods.SendMessage( this.listViewCannedMasks.Handle, NativeMethods.WM_SETREDRAW, false, /* unused = */ 0 );
 
            try 
            {
                this.listViewCannedMasks.Items.Clear(); 

                string nullEntry = SR.GetString( SR.MaskDescriptorValidatingTypeNone );

                foreach( MaskDescriptor maskDescriptor in maskDescriptors ) 
                {
                    string validatingType = maskDescriptor.ValidatingType != null ? maskDescriptor.ValidatingType.Name : nullEntry; 
 
                    // Make sure the sample displays literals.
                    MaskedTextProvider mtp = new MaskedTextProvider( maskDescriptor.Mask, maskDescriptor.Culture ); 
                    bool success = mtp.Add( maskDescriptor.Sample );
                    Debug.Assert( success, "BadBad: Could not add MaskDescriptor.Sample even it was validated, something is wrong!" );
                    // Don't include prompt.
                    string sample = mtp.ToString( false, true ); 

                    this.listViewCannedMasks.Items.Add( new ListViewItem( new string[] { maskDescriptor.Name, sample, validatingType } ) ); 
                } 

                // Add the custom mask descriptor as the last entry. 
                this.maskDescriptors.Add( this.customMaskDescriptor );
                this.listViewCannedMasks.Items.Add( new ListViewItem( new string[] { this.customMaskDescriptor.Name, "", nullEntry } ) );

                if( selectedMaskDex != null ) 
                {
                    SetSelectedMaskDescriptor( selectedMaskDex ); 
                } 
            }
            finally 
            {
                // Resume redraw.
                UnsafeNativeMethods.SendMessage( this.listViewCannedMasks.Handle, NativeMethods.WM_SETREDRAW, true, /* unused = */ 0 );
                this.listViewCannedMasks.Invalidate(); 
            }
        } 
 
        /// <devdoc>
        ///     Inserts a MaskDescriptor object in the specified position in the internal MaskDescriptor collection. 
        /// </devdoc>
        private void InsertMaskDescriptor( int index, MaskDescriptor maskDescriptor )
        {
            InsertMaskDescriptor( index, maskDescriptor, true ); 
        }
        private void InsertMaskDescriptor( int index, MaskDescriptor maskDescriptor, bool validateDescriptor ) 
        { 
            string errorMessage;
 
            if( validateDescriptor && !MaskDescriptor.IsValidMaskDescriptor(maskDescriptor, out errorMessage) )
            {
#if DEBUG
                Debug.Assert(DisableAssertDlg, string.Format(CultureInfo.CurrentCulture, "Invalid mask descriptor - Error: {0}\r\n{1}", errorMessage, maskDescriptor)); 
#endif
                return; 
            } 

            if( !ContainsMaskDescriptor(maskDescriptor) ) 
            {
                this.maskDescriptors.Insert(index, maskDescriptor );
            }
            else 
            {
#if DEBUG 
                Debug.Assert(DisableAssertDlg, "MaskedDescriptor could not be added to the list of canned masks because it is already added: " + maskDescriptor); 
#endif
            } 
        }

        /// <devdoc>
        ///     Removes a MaskDescriptor object from teh MaskDescriptor collection. 
        /// </devdoc>
        private void RemoveMaskDescriptor( MaskDescriptor maskDescriptor ) 
        { 
            int index = GetMaskDescriptorIndex( maskDescriptor );
 
            if( index >= 0 )
            {
                this.maskDescriptors.RemoveAt(index);
                return; 
            }
 
            Debug.Fail("Did not find mask descriptor: " + maskDescriptor); 
        }
 

        /// <devdoc>
        ///     Canned masks list view Column click event handler.  Sorts the items.
        /// </devdoc> 
        private void listViewCannedMasks_ColumnClick(object sender, ColumnClickEventArgs e)
        { 
            // Switch sorting order. 
            switch( this.listViewSortOrder )
            { 
                case SortOrder.None:
                case SortOrder.Descending:
                    this.listViewSortOrder = SortOrder.Ascending;
                    break; 
                case SortOrder.Ascending:
                    this.listViewSortOrder = SortOrder.Descending; 
                    break; 
            }
 
            //this.listViewCannedMasks.ListViewItemSorter = new ListViewItemComparer( e.Column, this.listViewCannedMasks.Sorting );

            UpdateSortedListView( (MaskDescriptorComparer.SortType) e.Column );
        } 

        /// <devdoc> 
        ///     OK button Click event handler.  Updates the validating type. 
        /// </devdoc>
        private void btnOK_Click(object sender, EventArgs e) 
        {
            if (this.checkBoxUseValidatingType.Checked)
            {
                this.mtpValidatingType = this.maskedTextBox.ValidatingType; 
            }
            else 
            { 
                this.mtpValidatingType = null;
            } 
        }

        /// <devdoc>
        ///     Canned masks list view Enter event handler.  Sets focus in the first item if none has it. 
        /// </devdoc>
        private void listViewCannedMasks_Enter(object sender, EventArgs e) 
        { 
            if( this.listViewCannedMasks.FocusedItem == null && this.listViewCannedMasks.Items.Count > 0)
            { 
                this.listViewCannedMasks.Items[0].Focused = true;
            }
        }
 
        /// <devdoc>
        ///     Canned masks list view SelectedIndexChanged event handler.  Gets the selected canned mask 
        ///     information. 
        /// </devdoc>
        private void listViewCannedMasks_SelectedIndexChanged(object sender, EventArgs e) 
        {
            if (this.listViewCannedMasks.SelectedItems.Count != 0)
            {
                int selectedIndex = this.listViewCannedMasks.SelectedIndices[0]; 
                MaskDescriptor maskDescriptor = (MaskDescriptor) this.maskDescriptors[selectedIndex];
 
                // If one of the canned mask descriptors chosen, update test control. 
                if( maskDescriptor != this.customMaskDescriptor )
                { 
                    this.txtBoxMask.Text              = maskDescriptor.Mask;
                    this.maskedTextBox.Mask           = maskDescriptor.Mask;
                    this.maskedTextBox.ValidatingType = maskDescriptor.ValidatingType;
                } 
                else
                { 
                    this.maskedTextBox.ValidatingType = null; 
                }
            } 
        }

        private void MaskDesignerDialog_Load(object sender, EventArgs e)
        { 
            UpdateSortedListView( MaskDescriptorComparer.SortType.ByName );
            SelectMtbMaskDescriptor(); 
            this.btnCancel.Select(); 
        }
 
        private void maskedTextBox_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {
            this.errorProvider.SetError(this.maskedTextBox, MaskedTextBoxDesigner.GetMaskInputRejectedErrorMessage(e));
        } 

        private string HelpTopic { 
            get { 
                return "net.ComponentModel.MaskPropertyEditor";
            } 
        }

        /// <devdoc>
        ///    <para> 
        ///       Called when the help button is clicked.
        ///    </para> 
        /// </devdoc> 
        private void ShowHelp() {
            if (helpService != null) { 
                helpService.ShowHelpFromKeyword(HelpTopic);
            }
            else {
                Debug.Fail("Unable to get IHelpService."); 
            }
        } 
 
        private void MaskDesignerDialog_HelpButtonClicked(object sender, CancelEventArgs e)
        { 
            e.Cancel = true;
            ShowHelp();
        }
 
        private void maskedTextBox_KeyDown(object sender, KeyEventArgs e)
        { 
            this.errorProvider.Clear(); 
        }
 
        /// <devdoc>
        ///     Mask text box Leave event handler.
        /// </devdoc>
        private void txtBoxMask_Validating(object sender, CancelEventArgs e) 
        {
            try 
            { 
                this.maskedTextBox.Mask = this.txtBoxMask.Text;
            } 
            catch(ArgumentException)
            {
                // The text in the TextBox may contain invalid characters so we just ignore the exception.
            } 
        }
 
        /// <devdoc> 
        ///     Mask text box TextChanged event handler.
        /// </devdoc> 
        private void txtBoxMask_TextChanged(object sender, EventArgs e)
        {
            // If the change in the text box is performed by the user, we need to select the 'Custom' item in
            // the list view, which is the last item. 

            MaskDescriptor selectedMaskDex = null; 
 
            if( this.listViewCannedMasks.SelectedItems.Count != 0 )
            { 
                int selectedIndex = this.listViewCannedMasks.SelectedIndices[0];
                selectedMaskDex = this.maskDescriptors[selectedIndex];
            }
 
            if( selectedMaskDex == null || (selectedMaskDex != this.customMaskDescriptor && selectedMaskDex.Mask != this.txtBoxMask.Text))
            { 
                SetSelectedMaskDescriptor(this.customMaskDescriptor); 
            }
        } 
    }

    /// <devdoc>
    ///     Implements the manual sorting of items by columns in the list view. 
    /// </devdoc>
    /* Note: Leaving this code here for ref - This was needed when sorting the listview elements automatically. 
    internal class ListViewItemComparer : System.Collections.IComparer 
    {
        private int column; 
        private SortOrder sortOrder;

        public ListViewItemComparer(int column, SortOrder sortOrder)
        { 
            this.column    = column;
            this.sortOrder = sortOrder; 
        } 

        public int Compare(object itemA, object itemB) 
        {
            ListViewItem listViewItemA = itemA as ListViewItem;
            ListViewItem listViewItemB = itemB as ListViewItem;
 
            if( listViewItemA == null || listViewItemB == null )
            { 
                Debug.Fail( "object type is not ListViewItem" ); 
                return 1;  // don't fail, however.
            } 

            string textA = listViewItemA.SubItems[this.column].Text;
            string textB = listViewItemB.SubItems[this.column].Text;
 
            int retVal = String.Compare(textA, textB);
 
            return sortOrder == SortOrder.Descending ? -retVal : retVal; 
        }
    } 
    */
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TextBoxBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.Design; 
    using System.Drawing;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Windows.Forms; 
    using System.Diagnostics; 
    using Microsoft.Win32;
    using System.Globalization; 

    /// <devdoc>
    ///     UI for the MaskTypeEditor (Design time).
    /// </devdoc> 
    internal class MaskDesignerDialog : System.Windows.Forms.Form
    { 
#if DEBUG 
        // Used by test suite to disable popping-up Assert dlg using private reflection in debug builds only.
        private static bool DisableAssertDlg; 
#endif
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.ListView listViewCannedMasks;
        private System.Windows.Forms.CheckBox checkBoxUseValidatingType; 
        private System.Windows.Forms.ColumnHeader maskDescriptionHeader;
        private System.Windows.Forms.ColumnHeader dataFormatHeader; 
        private System.Windows.Forms.ColumnHeader validatingTypeHeader; 
        private System.Windows.Forms.TableLayoutPanel maskTryItTable;
        private System.Windows.Forms.Label lblMask; 
        private System.Windows.Forms.TextBox txtBoxMask;
        private System.Windows.Forms.Label lblTryIt;
        private System.Windows.Forms.MaskedTextBox maskedTextBox;
        private System.Windows.Forms.TableLayoutPanel okCancelTableLayoutPanel; 
        private System.Windows.Forms.TableLayoutPanel overarchingTableLayoutPanel;
        private System.Windows.Forms.Button btnOK; 
        private System.Windows.Forms.Button btnCancel; 
        private System.Windows.Forms.ErrorProvider errorProvider;
 

        private List<MaskDescriptor> maskDescriptors = new List<MaskDescriptor>();
        private MaskDescriptor customMaskDescriptor;
        private SortOrder listViewSortOrder = SortOrder.Ascending; 
        private Type mtpValidatingType;
        private IContainer components; 
        private IHelpService helpService = null; 

 
        /// <devdoc>
        ///     Constructor receiving a clone of the MaskedTextBox control under design.
        /// </devdoc>
        public MaskDesignerDialog(MaskedTextBox instance, IHelpService helpService) { 
            if( instance == null )
            { 
                Debug.Fail( "Null masked text box, creating default." ); 
                this.maskedTextBox = new System.Windows.Forms.MaskedTextBox();
            } 
            else
            {
                this.maskedTextBox = MaskedTextBoxDesigner.GetDesignMaskedTextBox(instance);
            } 

            this.helpService = helpService; 
 
            InitializeComponent();
 
            // Non-designer-handled stuff.
            this.SuspendLayout();

            this.txtBoxMask.Text = this.maskedTextBox.Mask; 

            // Add default mask descriptors to the mask description list. 
            this.AddDefaultMaskDescriptors(this.maskedTextBox.Culture); 

            // 
            // maskDescriptionHeader
            //
            this.maskDescriptionHeader.Text = SR.GetString(SR.MaskDesignerDialogMaskDescription);
            this.maskDescriptionHeader.Width = this.listViewCannedMasks.Width / 3; 
            //
            // dataFormatHeader 
            // 
            this.dataFormatHeader.Text = SR.GetString(SR.MaskDesignerDialogDataFormat);
            this.dataFormatHeader.Width = this.listViewCannedMasks.Width / 3; 
            //
            // validatingTypeHeader
            //
            this.validatingTypeHeader.Text = SR.GetString(SR.MaskDesignerDialogValidatingType); 
            this.validatingTypeHeader.Width = (this.listViewCannedMasks.Width / 3) - SystemInformation.VerticalScrollBarWidth - 4;	// so no h-scrollbar.
            this.ResumeLayout(false); 
 
            HookEvents();
        } 

        private void HookEvents()
        {
            this.listViewCannedMasks.SelectedIndexChanged += new System.EventHandler(this.listViewCannedMasks_SelectedIndexChanged); 
            this.listViewCannedMasks.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listViewCannedMasks_ColumnClick);
            this.listViewCannedMasks.Enter += new EventHandler(listViewCannedMasks_Enter); 
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click); 
            this.txtBoxMask.TextChanged += new System.EventHandler(this.txtBoxMask_TextChanged);
            this.txtBoxMask.Validating += new System.ComponentModel.CancelEventHandler(this.txtBoxMask_Validating); 
            this.maskedTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.maskedTextBox_KeyDown);
            this.maskedTextBox.MaskInputRejected += new System.Windows.Forms.MaskInputRejectedEventHandler(this.maskedTextBox_MaskInputRejected);
            this.Load += new System.EventHandler(this.MaskDesignerDialog_Load);
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.MaskDesignerDialog_HelpButtonClicked); 
        }
 
 
        private void InitializeComponent()
        { 
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MaskDesignerDialog));
            this.lblHeader = new System.Windows.Forms.Label();
            this.listViewCannedMasks = new System.Windows.Forms.ListView(); 
            this.maskDescriptionHeader = new System.Windows.Forms.ColumnHeader(resources.GetString("listViewCannedMasks.Columns"));
            this.dataFormatHeader = new System.Windows.Forms.ColumnHeader(resources.GetString("listViewCannedMasks.Columns1")); 
            this.validatingTypeHeader = new System.Windows.Forms.ColumnHeader(resources.GetString("listViewCannedMasks.Columns2")); 
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button(); 
            this.checkBoxUseValidatingType = new System.Windows.Forms.CheckBox();
            this.maskTryItTable = new System.Windows.Forms.TableLayoutPanel();
            this.lblMask = new System.Windows.Forms.Label();
            this.txtBoxMask = new System.Windows.Forms.TextBox(); 
            this.lblTryIt = new System.Windows.Forms.Label();
            this.overarchingTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this.okCancelTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.maskTryItTable.SuspendLayout(); 
            this.overarchingTableLayoutPanel.SuspendLayout();
            this.okCancelTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout(); 
            //
            // maskedTextBox 
            // 
            resources.ApplyResources(this.maskedTextBox, "maskedTextBox");
            this.maskedTextBox.Margin = new System.Windows.Forms.Padding(3, 3, 18, 0); 
            this.maskedTextBox.Name = "maskedTextBox";
            //
            // lblHeader
            // 
            resources.ApplyResources(this.lblHeader, "lblHeader");
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 3); 
            this.lblHeader.Name = "lblHeader"; 
            //
            // listViewCannedMasks 
            //
            resources.ApplyResources(this.listViewCannedMasks, "listViewCannedMasks");
            this.listViewCannedMasks.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.maskDescriptionHeader, 
            this.dataFormatHeader,
            this.validatingTypeHeader}); 
            this.listViewCannedMasks.FullRowSelect = true; 
            this.listViewCannedMasks.HideSelection = false;
            this.listViewCannedMasks.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3); 
            this.listViewCannedMasks.MultiSelect = false;
            this.listViewCannedMasks.Name = "listViewCannedMasks";
            this.listViewCannedMasks.Sorting = SortOrder.None; // We'll do the sorting ourselves.
            this.listViewCannedMasks.View = System.Windows.Forms.View.Details; 
            //
            // maskDescriptionHeader 
            // 
            resources.ApplyResources(this.maskDescriptionHeader, "maskDescriptionHeader");
            // 
            // dataFormatHeader
            //
            resources.ApplyResources(this.dataFormatHeader, "dataFormatHeader");
            // 
            // validatingTypeHeader
            // 
            resources.ApplyResources(this.validatingTypeHeader, "validatingTypeHeader"); 
            //
            // btnOK 
            //
            resources.ApplyResources(this.btnOK, "btnOK");
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0); 
            this.btnOK.MinimumSize = new System.Drawing.Size(75, 23);
            this.btnOK.Name = "btnOK"; 
            this.btnOK.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0); 
            //
            // btnCancel 
            //
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0); 
            this.btnCancel.MinimumSize = new System.Drawing.Size(75, 23);
            this.btnCancel.Name = "btnCancel"; 
            this.btnCancel.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0); 
            //
            // checkBoxUseValidatingType 
            //
            resources.ApplyResources(this.checkBoxUseValidatingType, "checkBoxUseValidatingType");
            this.checkBoxUseValidatingType.Checked = true;
            this.checkBoxUseValidatingType.CheckState = System.Windows.Forms.CheckState.Checked; 
            this.checkBoxUseValidatingType.Margin = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.checkBoxUseValidatingType.Name = "checkBoxUseValidatingType"; 
            // 
            // maskTryItTable
            // 
            resources.ApplyResources(this.maskTryItTable, "maskTryItTable");
            this.maskTryItTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.maskTryItTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.maskTryItTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle()); 
            this.maskTryItTable.Controls.Add(this.checkBoxUseValidatingType, 2, 0);
            this.maskTryItTable.Controls.Add(this.lblMask, 0, 0); 
            this.maskTryItTable.Controls.Add(this.txtBoxMask, 1, 0); 
            this.maskTryItTable.Controls.Add(this.lblTryIt, 0, 1);
            this.maskTryItTable.Controls.Add(this.maskedTextBox, 1, 1); 
            this.maskTryItTable.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.maskTryItTable.Name = "maskTryItTable";
            this.maskTryItTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.maskTryItTable.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            //
            // lblMask 
            // 
            resources.ApplyResources(this.lblMask, "lblMask");
            this.lblMask.Margin = new System.Windows.Forms.Padding(0, 0, 3, 3); 
            this.lblMask.Name = "lblMask";
            //
            // txtBoxMask
            // 
            resources.ApplyResources(this.txtBoxMask, "txtBoxMask");
            this.txtBoxMask.Margin = new System.Windows.Forms.Padding(3, 0, 18, 3); 
            this.txtBoxMask.Name = "txtBoxMask"; 
            //
            // lblTryIt 
            //
            resources.ApplyResources(this.lblTryIt, "lblTryIt");
            this.lblTryIt.Margin = new System.Windows.Forms.Padding(0, 3, 3, 0);
            this.lblTryIt.Name = "lblTryIt"; 
            //
            // overarchingTableLayoutPanel 
            // 
            resources.ApplyResources(this.overarchingTableLayoutPanel, "overarchingTableLayoutPanel");
            this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this.overarchingTableLayoutPanel.Controls.Add(this.maskTryItTable, 0, 3);
            this.overarchingTableLayoutPanel.Controls.Add(this.okCancelTableLayoutPanel, 0, 4);
            this.overarchingTableLayoutPanel.Controls.Add(this.lblHeader, 0, 1);
            this.overarchingTableLayoutPanel.Controls.Add(this.listViewCannedMasks, 0, 2); 
            this.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel";
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle()); 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            //
            // okCancelTableLayoutPanel
            // 
            resources.ApplyResources(this.okCancelTableLayoutPanel, "okCancelTableLayoutPanel");
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this.okCancelTableLayoutPanel.Controls.Add(this.btnCancel, 1, 0);
            this.okCancelTableLayoutPanel.Controls.Add(this.btnOK, 0, 0); 
            this.okCancelTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel";
            this.okCancelTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            // 
            // errorProvider
            // 
            this.errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink; 
            this.errorProvider.ContainerControl = this;
            // 
            // MaskDesignerDialog
            //
            resources.ApplyResources(this, "$this");
            this.AcceptButton = this.btnOK; 
            this.CancelButton = this.btnCancel;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font; 
            this.Controls.Add(this.overarchingTableLayoutPanel); 
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.HelpButton = true; 
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MaskDesignerDialog";
            this.ShowInTaskbar = false; 
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.maskTryItTable.ResumeLayout(false); 
            this.maskTryItTable.PerformLayout(); 
            this.overarchingTableLayoutPanel.ResumeLayout(false);
            this.overarchingTableLayoutPanel.PerformLayout(); 
            this.okCancelTableLayoutPanel.ResumeLayout(false);
            this.okCancelTableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.ResumeLayout(false); 

        } 
 
        /// <devdoc>
        ///     The current text (mask) in the txtBoxMask control. 
        /// </devdoc>
        public string Mask
        {
            get 
            {
 				return this.maskedTextBox.Mask; 
			} 
        }
 

		/// <devdoc>
		///     The current text (mask) in the txtBoxMask control.
 		/// </devdoc> 
		public Type ValidatingType
 		{ 
 			get 
			{
 				return this.mtpValidatingType; 
			}
		}

 

		////////// Properties. 
        /// 

        /// <devdoc> 
        ///     A collection of MaskDescriptor objects represented in the ListView with the canned mask
        ///     descriptions.
        /// </devdoc>
        public System.Collections.IEnumerator MaskDescriptors 
        {
            get 
            { 
                return this.maskDescriptors.GetEnumerator();
            } 
        }


        ////////// Methods. 

        /// <devdoc> 
        ///     Adds the default mask descriptors to the mask description list. 
        ///     We need to add the deafult descriptors explicitly because the DiscoverMaskDescriptors method only adds
        ///     public descriptors and these are internal. 
        /// </devdoc>
        private void AddDefaultMaskDescriptors(CultureInfo culture)
        {
            this.customMaskDescriptor = new MaskDescriptorTemplate(null, SR.GetString(SR.MaskDesignerDialogCustomEntry), null, null, null, true); 

            List<MaskDescriptor> maskDescriptors = MaskDescriptorTemplate.GetLocalizedMaskDescriptors(culture); 
 
            // Need to pass false for validateDescriptor param since the custom mask will fail validation
            // because the mask is empty. 
            InsertMaskDescriptor(0, this.customMaskDescriptor, /*validate*/ false);

            foreach( MaskDescriptor maskDescriptor in maskDescriptors )
            { 
                InsertMaskDescriptor(0, maskDescriptor);
            } 
        } 

        /// <devdoc> 
        ///     Determines whether the specified MaskDescriptor object is in the MaskDescriptor collection or not.
        /// </devdoc>
        private bool ContainsMaskDescriptor( MaskDescriptor maskDescriptor )
        { 
            Debug.Assert( maskDescriptor != null, "Null mask descriptor." );
 
            foreach( MaskDescriptor descriptor in this.maskDescriptors ) 
            {
                Debug.Assert( descriptor != null, "Null mask descriptor in the collection." ); 

                if( maskDescriptor.Equals(descriptor) || maskDescriptor.Name.Trim() == descriptor.Name.Trim() )
                {
                    return true; 
                }
            } 
 
            return false;
        } 

        /// <devdoc>
        ///     Uses the specified ITypeDiscoveryService service provider to discover MaskDescriptor objects from
        ///     the referenced assemblies. 
        /// </devdoc>
        public void DiscoverMaskDescriptors( ITypeDiscoveryService discoveryService ) 
        { 
            if (discoveryService != null)
            { 
                ICollection descriptors = DesignerUtils.FilterGenericTypes(discoveryService.GetTypes(typeof(MaskDescriptor), false /* excludeGlobalTypes */));

                // Note: This code assumes DesignerUtils.FilterGenericTypes return a valid ICollection (collection of MaskDescriptor types).
                foreach( Type t in descriptors ) 
                {
                    if (t.IsAbstract || !t.IsPublic) 
                    { 
                        continue;
                    } 

                    // Since mask descriptors can be provided from external sources, we need to guard against
                    // possible exceptions when accessing an external descriptor.
                    try 
                    {
                        MaskDescriptor maskDescriptor = (MaskDescriptor) Activator.CreateInstance( t ); 
                        InsertMaskDescriptor(0, maskDescriptor); 
                    }
                    catch( Exception ex ) 
                    {
                        if( ClientUtils.IsCriticalException( ex ))
                        {
                            throw; 
                        }
#if DEBUG 
                        Debug.Assert(DisableAssertDlg, ex.ToString()); 
#endif
                    } 
                    catch
                    {
                        Debug.Fail("non-CLS compliant exception");
                    } 
                }
            } 
        } 

        /// <devdoc> 
        ///     Gets the index of a mask descriptor in the mask descriptor table.
        /// </devdoc>
        private int GetMaskDescriptorIndex(MaskDescriptor maskDescriptor)
        { 
            for( int index = 0; index < this.maskDescriptors.Count; index++ )
            { 
                MaskDescriptor descriptor = this.maskDescriptors[index]; 

                if( descriptor == maskDescriptor ) 
                {
                    return index;
                }
            } 

            Debug.Fail( "Could not find mask descriptor." ); 
            return -1; 
        }
 
        /// <devdoc>
        ///     Selects the mask descriptor corresponding to the current MaskedTextBox.Mask if any, otherwise the custom entry.
        /// </devdoc>
        private void SelectMtbMaskDescriptor() 
        {
            int selectedItemIdx = -1; 
 
            if( !string.IsNullOrEmpty( this.maskedTextBox.Mask ) )
            { 
                for( int selectedIndex = 0; selectedIndex < this.maskDescriptors.Count; selectedIndex++ )
                {
                    MaskDescriptor descriptor = this.maskDescriptors[selectedIndex];
 
                    if( descriptor.Mask == this.maskedTextBox.Mask && descriptor.ValidatingType == this.maskedTextBox.ValidatingType)
                    { 
                        selectedItemIdx = selectedIndex; 
                        break;
                    } 
                }
            }

            if( selectedItemIdx == -1 ) // select custom mask. 
            {
                selectedItemIdx = GetMaskDescriptorIndex( this.customMaskDescriptor ); 
 
                if( selectedItemIdx == -1 )
                { 
                    Debug.Fail("Could not find custom mask descriptor.");
                }
            }
 
            if( selectedItemIdx != -1 )
            { 
                SetSelectedMaskDescriptor( selectedItemIdx ); 
            }
 
            //if( this.listViewCannedMasks.FocusedItem == null )
            //{
            //    this.listViewCannedMasks.Items[0].Focused = true;  // it is assumed we have the default items in the list.
            //} 
        }
 
        /// <devdoc> 
        ///     Selects the specified item in the ListView.
        /// </devdoc> 
        private void SetSelectedMaskDescriptor( MaskDescriptor maskDex )
        {
            int maskDexIndex = GetMaskDescriptorIndex( maskDex );
            SetSelectedMaskDescriptor( maskDexIndex ); 
        }
        private void SetSelectedMaskDescriptor( int maskDexIndex ) 
        { 
            Debug.Assert( maskDexIndex >= 0, "Invalid index." );
            if( maskDexIndex >= 0 && this.listViewCannedMasks.Items.Count > maskDexIndex ) 
            {
                this.listViewCannedMasks.Items[maskDexIndex].Selected = true;
                this.listViewCannedMasks.FocusedItem = this.listViewCannedMasks.Items[maskDexIndex];
                this.listViewCannedMasks.EnsureVisible( maskDexIndex ); 
            }
        } 
 
        /// <devdoc>
        ///     Sorts the maskDescriptors and the list view items. 
        /// </devdoc>
        private void UpdateSortedListView(MaskDescriptorComparer.SortType sortType )
        {
            if( !this.listViewCannedMasks.IsHandleCreated ) 
            {
                return; 
            } 

            MaskDescriptor selectedMaskDex = null; 

            // Save current selected entry to restore it after sorting.
            if( this.listViewCannedMasks.SelectedItems.Count > 0 )
            { 
                int selectedIndex = this.listViewCannedMasks.SelectedIndices[0];
                selectedMaskDex = this.maskDescriptors[selectedIndex]; 
            } 

            // Custom mask descriptor should always be the last entry - remove it before sorting array. 
            this.maskDescriptors.RemoveAt( this.maskDescriptors.Count - 1 );

            // Sort MaskDescriptor collection.
            this.maskDescriptors.Sort( new MaskDescriptorComparer( sortType, this.listViewSortOrder ) ); 

            // Sorting the ListView items forces handle recreation, since we have the items sorted and know what item to select 
            // it is better for us to replace the items ourselves.  This way also avoids problems with the selected item  and 
            // the custom entry not getting properly added.
            // this.listViewCannedMasks.Sort(); 

            // Since we need to pre-process each item before inserting it in the ListView, it is better to remove all items
            // from it first and then add the sorted ones back (no replace).  Stop redrawing while we change the list.
 
            UnsafeNativeMethods.SendMessage( this.listViewCannedMasks.Handle, NativeMethods.WM_SETREDRAW, false, /* unused = */ 0 );
 
            try 
            {
                this.listViewCannedMasks.Items.Clear(); 

                string nullEntry = SR.GetString( SR.MaskDescriptorValidatingTypeNone );

                foreach( MaskDescriptor maskDescriptor in maskDescriptors ) 
                {
                    string validatingType = maskDescriptor.ValidatingType != null ? maskDescriptor.ValidatingType.Name : nullEntry; 
 
                    // Make sure the sample displays literals.
                    MaskedTextProvider mtp = new MaskedTextProvider( maskDescriptor.Mask, maskDescriptor.Culture ); 
                    bool success = mtp.Add( maskDescriptor.Sample );
                    Debug.Assert( success, "BadBad: Could not add MaskDescriptor.Sample even it was validated, something is wrong!" );
                    // Don't include prompt.
                    string sample = mtp.ToString( false, true ); 

                    this.listViewCannedMasks.Items.Add( new ListViewItem( new string[] { maskDescriptor.Name, sample, validatingType } ) ); 
                } 

                // Add the custom mask descriptor as the last entry. 
                this.maskDescriptors.Add( this.customMaskDescriptor );
                this.listViewCannedMasks.Items.Add( new ListViewItem( new string[] { this.customMaskDescriptor.Name, "", nullEntry } ) );

                if( selectedMaskDex != null ) 
                {
                    SetSelectedMaskDescriptor( selectedMaskDex ); 
                } 
            }
            finally 
            {
                // Resume redraw.
                UnsafeNativeMethods.SendMessage( this.listViewCannedMasks.Handle, NativeMethods.WM_SETREDRAW, true, /* unused = */ 0 );
                this.listViewCannedMasks.Invalidate(); 
            }
        } 
 
        /// <devdoc>
        ///     Inserts a MaskDescriptor object in the specified position in the internal MaskDescriptor collection. 
        /// </devdoc>
        private void InsertMaskDescriptor( int index, MaskDescriptor maskDescriptor )
        {
            InsertMaskDescriptor( index, maskDescriptor, true ); 
        }
        private void InsertMaskDescriptor( int index, MaskDescriptor maskDescriptor, bool validateDescriptor ) 
        { 
            string errorMessage;
 
            if( validateDescriptor && !MaskDescriptor.IsValidMaskDescriptor(maskDescriptor, out errorMessage) )
            {
#if DEBUG
                Debug.Assert(DisableAssertDlg, string.Format(CultureInfo.CurrentCulture, "Invalid mask descriptor - Error: {0}\r\n{1}", errorMessage, maskDescriptor)); 
#endif
                return; 
            } 

            if( !ContainsMaskDescriptor(maskDescriptor) ) 
            {
                this.maskDescriptors.Insert(index, maskDescriptor );
            }
            else 
            {
#if DEBUG 
                Debug.Assert(DisableAssertDlg, "MaskedDescriptor could not be added to the list of canned masks because it is already added: " + maskDescriptor); 
#endif
            } 
        }

        /// <devdoc>
        ///     Removes a MaskDescriptor object from teh MaskDescriptor collection. 
        /// </devdoc>
        private void RemoveMaskDescriptor( MaskDescriptor maskDescriptor ) 
        { 
            int index = GetMaskDescriptorIndex( maskDescriptor );
 
            if( index >= 0 )
            {
                this.maskDescriptors.RemoveAt(index);
                return; 
            }
 
            Debug.Fail("Did not find mask descriptor: " + maskDescriptor); 
        }
 

        /// <devdoc>
        ///     Canned masks list view Column click event handler.  Sorts the items.
        /// </devdoc> 
        private void listViewCannedMasks_ColumnClick(object sender, ColumnClickEventArgs e)
        { 
            // Switch sorting order. 
            switch( this.listViewSortOrder )
            { 
                case SortOrder.None:
                case SortOrder.Descending:
                    this.listViewSortOrder = SortOrder.Ascending;
                    break; 
                case SortOrder.Ascending:
                    this.listViewSortOrder = SortOrder.Descending; 
                    break; 
            }
 
            //this.listViewCannedMasks.ListViewItemSorter = new ListViewItemComparer( e.Column, this.listViewCannedMasks.Sorting );

            UpdateSortedListView( (MaskDescriptorComparer.SortType) e.Column );
        } 

        /// <devdoc> 
        ///     OK button Click event handler.  Updates the validating type. 
        /// </devdoc>
        private void btnOK_Click(object sender, EventArgs e) 
        {
            if (this.checkBoxUseValidatingType.Checked)
            {
                this.mtpValidatingType = this.maskedTextBox.ValidatingType; 
            }
            else 
            { 
                this.mtpValidatingType = null;
            } 
        }

        /// <devdoc>
        ///     Canned masks list view Enter event handler.  Sets focus in the first item if none has it. 
        /// </devdoc>
        private void listViewCannedMasks_Enter(object sender, EventArgs e) 
        { 
            if( this.listViewCannedMasks.FocusedItem == null && this.listViewCannedMasks.Items.Count > 0)
            { 
                this.listViewCannedMasks.Items[0].Focused = true;
            }
        }
 
        /// <devdoc>
        ///     Canned masks list view SelectedIndexChanged event handler.  Gets the selected canned mask 
        ///     information. 
        /// </devdoc>
        private void listViewCannedMasks_SelectedIndexChanged(object sender, EventArgs e) 
        {
            if (this.listViewCannedMasks.SelectedItems.Count != 0)
            {
                int selectedIndex = this.listViewCannedMasks.SelectedIndices[0]; 
                MaskDescriptor maskDescriptor = (MaskDescriptor) this.maskDescriptors[selectedIndex];
 
                // If one of the canned mask descriptors chosen, update test control. 
                if( maskDescriptor != this.customMaskDescriptor )
                { 
                    this.txtBoxMask.Text              = maskDescriptor.Mask;
                    this.maskedTextBox.Mask           = maskDescriptor.Mask;
                    this.maskedTextBox.ValidatingType = maskDescriptor.ValidatingType;
                } 
                else
                { 
                    this.maskedTextBox.ValidatingType = null; 
                }
            } 
        }

        private void MaskDesignerDialog_Load(object sender, EventArgs e)
        { 
            UpdateSortedListView( MaskDescriptorComparer.SortType.ByName );
            SelectMtbMaskDescriptor(); 
            this.btnCancel.Select(); 
        }
 
        private void maskedTextBox_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {
            this.errorProvider.SetError(this.maskedTextBox, MaskedTextBoxDesigner.GetMaskInputRejectedErrorMessage(e));
        } 

        private string HelpTopic { 
            get { 
                return "net.ComponentModel.MaskPropertyEditor";
            } 
        }

        /// <devdoc>
        ///    <para> 
        ///       Called when the help button is clicked.
        ///    </para> 
        /// </devdoc> 
        private void ShowHelp() {
            if (helpService != null) { 
                helpService.ShowHelpFromKeyword(HelpTopic);
            }
            else {
                Debug.Fail("Unable to get IHelpService."); 
            }
        } 
 
        private void MaskDesignerDialog_HelpButtonClicked(object sender, CancelEventArgs e)
        { 
            e.Cancel = true;
            ShowHelp();
        }
 
        private void maskedTextBox_KeyDown(object sender, KeyEventArgs e)
        { 
            this.errorProvider.Clear(); 
        }
 
        /// <devdoc>
        ///     Mask text box Leave event handler.
        /// </devdoc>
        private void txtBoxMask_Validating(object sender, CancelEventArgs e) 
        {
            try 
            { 
                this.maskedTextBox.Mask = this.txtBoxMask.Text;
            } 
            catch(ArgumentException)
            {
                // The text in the TextBox may contain invalid characters so we just ignore the exception.
            } 
        }
 
        /// <devdoc> 
        ///     Mask text box TextChanged event handler.
        /// </devdoc> 
        private void txtBoxMask_TextChanged(object sender, EventArgs e)
        {
            // If the change in the text box is performed by the user, we need to select the 'Custom' item in
            // the list view, which is the last item. 

            MaskDescriptor selectedMaskDex = null; 
 
            if( this.listViewCannedMasks.SelectedItems.Count != 0 )
            { 
                int selectedIndex = this.listViewCannedMasks.SelectedIndices[0];
                selectedMaskDex = this.maskDescriptors[selectedIndex];
            }
 
            if( selectedMaskDex == null || (selectedMaskDex != this.customMaskDescriptor && selectedMaskDex.Mask != this.txtBoxMask.Text))
            { 
                SetSelectedMaskDescriptor(this.customMaskDescriptor); 
            }
        } 
    }

    /// <devdoc>
    ///     Implements the manual sorting of items by columns in the list view. 
    /// </devdoc>
    /* Note: Leaving this code here for ref - This was needed when sorting the listview elements automatically. 
    internal class ListViewItemComparer : System.Collections.IComparer 
    {
        private int column; 
        private SortOrder sortOrder;

        public ListViewItemComparer(int column, SortOrder sortOrder)
        { 
            this.column    = column;
            this.sortOrder = sortOrder; 
        } 

        public int Compare(object itemA, object itemB) 
        {
            ListViewItem listViewItemA = itemA as ListViewItem;
            ListViewItem listViewItemB = itemB as ListViewItem;
 
            if( listViewItemA == null || listViewItemB == null )
            { 
                Debug.Fail( "object type is not ListViewItem" ); 
                return 1;  // don't fail, however.
            } 

            string textA = listViewItemA.SubItems[this.column].Text;
            string textB = listViewItemB.SubItems[this.column].Text;
 
            int retVal = String.Compare(textA, textB);
 
            return sortOrder == SortOrder.Descending ? -retVal : retVal; 
        }
    } 
    */
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
