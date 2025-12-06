//------------------------------------------------------------------------------ 
// <copyright file="DataGridAutoFormatDialog.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System;
    using System.Design; 
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Data;
    using System.Windows.Forms; 
    using System.Drawing; 
    using System.Xml;
    using System.IO; 
    using System.Globalization;

    /// <include file='doc\DataGridAutoFormatDialog.uex' path='docs/doc[@for="DataGridAutoFormatDialog"]/*' />
    /// <internalonly/> 
    /// <devdoc>
    /// </devdoc> 
    internal class DataGridAutoFormatDialog : Form { 

        private DataGrid dgrid; 

        private DataTable schemeTable;
        // private PictureBox schemePicture;
        DataSet dataSet = new DataSet(); 
        private AutoFormatDataGrid dataGrid;
        private DataGridTableStyle tableStyle; 
        private Button button2; 
        private Button button1;
        private ListBox schemeName; 
        private Label formats;
        private Label preview;
        private bool IMBusy;
        private TableLayoutPanel okCancelTableLayoutPanel; 
        private TableLayoutPanel overarchingTableLayoutPanel;
 
        private int selectedIndex = -1; 

        internal DataGridAutoFormatDialog(DataGrid dgrid) { 
            this.dgrid = dgrid;

            this.ShowInTaskbar = false;
            dataSet.Locale = CultureInfo.InvariantCulture; 
            dataSet.ReadXmlSchema(new XmlTextReader(new StringReader(scheme)));
            dataSet.ReadXml(new StringReader(data), XmlReadMode.IgnoreSchema); 
            schemeTable = dataSet.Tables["Scheme"]; 

            IMBusy = true; 

            InitializeComponent();

            schemeName.DataSource = schemeTable; 

            AddDataToDataGrid(); 
            AddStyleSheetInformationToDataGrid(); 

            if (dgrid.Site != null) { 
                IUIService uiService = (IUIService)dgrid.Site.GetService(typeof(IUIService));
                if (uiService != null) {
                    Font f = (Font)uiService.Styles["DialogFont"];
                    if (f != null) { 
                        this.Font = f;
                    } 
                } 
            }
 
            //this.Focus(); causes an assertion failure - and is not required.
            IMBusy = false;
        }
 
        private void AddStyleSheetInformationToDataGrid() {
            DataGridTableStyle dGTStyle = new DataGridTableStyle(); 
            dGTStyle.MappingName = "Table1"; 
            DataGridColumnStyle col1 = new DataGridTextBoxColumn();
            col1.MappingName = "First Name"; 
            col1.HeaderText = SR.GetString(SR.DataGridAutoFormatTableFirstColumn);

            DataGridColumnStyle col2 = new DataGridTextBoxColumn();
            col2.MappingName = "Last Name"; 
            col2.HeaderText = SR.GetString(SR.DataGridAutoFormatTableSecondColumn);
 
            dGTStyle.GridColumnStyles.Add(col1); 
            dGTStyle.GridColumnStyles.Add(col2);
 
            DataRowCollection drc = dataSet.Tables["Scheme"].Rows;
            DataRow dr;
            dr = drc[0];
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameDefault); 
            dr = drc[1];
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameProfessional1); 
            dr = drc[2]; 
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameProfessional2);
            dr = drc[3]; 
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameProfessional3);
            dr = drc[4];
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameProfessional4);
            dr = drc[5]; 
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameClassic);
            dr = drc[6]; 
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameSimple); 
            dr = drc[7];
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameColorful1); 
            dr = drc[8];
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameColorful2);
            dr = drc[9];
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameColorful3); 
            dr = drc[10];
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameColorful4); 
            dr = drc[11]; 
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeName256Color1);
            dr = drc[12]; 
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeName256Color2);

            this.dataGrid.TableStyles.Add(dGTStyle);
            this.tableStyle = dGTStyle; 
        }
 
        private void AddDataToDataGrid() { 
            DataTable dTable = new DataTable("Table1");
            dTable.Locale = CultureInfo.InvariantCulture; 
            dTable.Columns.Add(new DataColumn("First Name"));
            dTable.Columns.Add(new DataColumn("Last Name"));

            DataRow dRow = dTable.NewRow(); 
            dRow["First Name"] = "Robert";
            dRow["Last Name"] = "Brown"; 
            dTable.Rows.Add(dRow); 

            dRow = dTable.NewRow(); 
            dRow["First Name"] = "Nate";
            dRow["Last Name"] = "Sun";
            dTable.Rows.Add(dRow);
 
            dRow = dTable.NewRow();
            dRow["First Name"] = "Carole"; 
            dRow["Last Name"] = "Poland"; 
            dTable.Rows.Add(dRow);
 
            this.dataGrid.SetDataBinding(dTable, "");
        }

        private void AutoFormat_HelpRequested(object sender, HelpEventArgs e) { 
            if (dgrid == null || dgrid.Site == null)
                return; 
            IDesignerHost host = dgrid.Site.GetService(typeof(IDesignerHost)) as IDesignerHost; 
            if (host == null) {
                Debug.Fail("Unable to get IDesignerHost."); 
                return;
            }

            IHelpService helpService = host.GetService(typeof(IHelpService)) as IHelpService; 
            if (helpService != null) {
                helpService.ShowHelpFromKeyword("vs.DataGridAutoFormatDialog"); 
            } else { 
                Debug.Fail("Unable to get IHelpService.");
            } 
        }

        private void Button1_Clicked(object sender, EventArgs e) {
            selectedIndex = schemeName.SelectedIndex; 
        }
 
        private void InitializeComponent() { 
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataGridAutoFormatDialog));
            this.formats = new System.Windows.Forms.Label(); 
            this.schemeName = new System.Windows.Forms.ListBox();
            dataGrid = new AutoFormatDataGrid();
            this.preview = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button(); 
            this.button2 = new System.Windows.Forms.Button();
            this.okCancelTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this.overarchingTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            ((System.ComponentModel.ISupportInitialize)(this.dataGrid)).BeginInit();
            this.okCancelTableLayoutPanel.SuspendLayout(); 
            this.overarchingTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // formats 
            //
            resources.ApplyResources(this.formats, "formats"); 
            this.formats.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0); 
            this.formats.Name = "formats";
            // 
            // schemeName
            //
            resources.ApplyResources(this.schemeName, "schemeName");
            this.schemeName.DisplayMember = "SchemeName"; 
            this.schemeName.FormattingEnabled = true;
            this.schemeName.Margin = new System.Windows.Forms.Padding(0, 2, 3, 3); 
            this.schemeName.Name = "schemeName"; 
            this.schemeName.SelectedIndexChanged += new System.EventHandler(this.SchemeName_SelectionChanged);
            // 
            // dataGrid
            //
            resources.ApplyResources(this.dataGrid, "dataGrid");
            this.dataGrid.DataMember = ""; 
            this.dataGrid.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dataGrid.Margin = new System.Windows.Forms.Padding(3, 2, 0, 3); 
            this.dataGrid.Name = "dataGrid"; 
            //
            // preview 
            //
            resources.ApplyResources(this.preview, "preview");
            this.preview.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.preview.Name = "preview"; 
            //
            // button1 
            // 
            resources.ApplyResources(this.button1, "button1");
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK; 
            this.button1.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.button1.MinimumSize = new System.Drawing.Size(75, 23);
            this.button1.Name = "button1";
            this.button1.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0); 
            this.button1.Click += new System.EventHandler(this.Button1_Clicked);
            // 
            // button2 
            //
            resources.ApplyResources(this.button2, "button2"); 
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button2.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.button2.MinimumSize = new System.Drawing.Size(75, 23);
            this.button2.Name = "button2"; 
            this.button2.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            // 
            // okCancelTableLayoutPanel 
            //
            resources.ApplyResources(this.okCancelTableLayoutPanel, "okCancelTableLayoutPanel"); 
            this.overarchingTableLayoutPanel.SetColumnSpan(this.okCancelTableLayoutPanel, 2);
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.okCancelTableLayoutPanel.Controls.Add(this.button1, 0, 0); 
            this.okCancelTableLayoutPanel.Controls.Add(this.button2, 1, 0);
            this.okCancelTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0); 
            this.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel"; 
            this.okCancelTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            // 
            // overarchingTableLayoutPanel
            //
            resources.ApplyResources(this.overarchingTableLayoutPanel, "overarchingTableLayoutPanel");
            this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 146F)); 
            this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 182F));
            this.overarchingTableLayoutPanel.Controls.Add(this.okCancelTableLayoutPanel, 0, 2); 
            this.overarchingTableLayoutPanel.Controls.Add(this.preview, 1, 0); 
            this.overarchingTableLayoutPanel.Controls.Add(this.dataGrid, 1, 1);
            this.overarchingTableLayoutPanel.Controls.Add(this.formats, 0, 0); 
            this.overarchingTableLayoutPanel.Controls.Add(this.schemeName, 0, 1);
            this.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel";
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            // 
            // DataGridAutoFormatDialog 
            //
            this.AcceptButton = this.button1; 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.button2;
            this.Controls.Add(this.overarchingTableLayoutPanel); 
            this.MaximizeBox = false;
            this.MinimizeBox = false; 
            this.Name = "DataGridAutoFormatDialog"; 
            this.ShowIcon = false;
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.AutoFormat_HelpRequested); 
            ((System.ComponentModel.ISupportInitialize)(this.dataGrid)).EndInit();
            this.okCancelTableLayoutPanel.ResumeLayout(false);
            this.okCancelTableLayoutPanel.PerformLayout();
            this.overarchingTableLayoutPanel.ResumeLayout(false); 
            this.overarchingTableLayoutPanel.PerformLayout();
            this.ResumeLayout(false); 
        } 

        private static bool IsTableProperty(string propName) { 
            if (propName.Equals("HeaderColor"))
                return true;
            if (propName.Equals("AlternatingBackColor"))
                return true; 
            if (propName.Equals("BackColor"))
                return true; 
            if (propName.Equals("ForeColor")) 
                return true;
            if (propName.Equals("GridLineColor")) 
                return true;
            if (propName.Equals("GridLineStyle"))
                return true;
            if (propName.Equals("HeaderBackColor")) 
                return true;
            if (propName.Equals("HeaderForeColor")) 
                return true; 
            if (propName.Equals("LinkColor"))
                return true; 
            if (propName.Equals("LinkHoverColor"))
                return true;
            if (propName.Equals("SelectionForeColor"))
                return true; 
            if (propName.Equals("SelectionBackColor"))
                return true; 
            if (propName.Equals("HeaderFont")) 
                return true;
            return false; 
        }

        [
            SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")   // See comment inside the method about ignoring errors. 
        ]
        private void SchemeName_SelectionChanged(object sender, EventArgs e) { 
            if (IMBusy) 
                return;
 
            DataRow row = ((DataRowView)schemeName.SelectedItem).Row;
            if (row != null) {
                PropertyDescriptorCollection gridProperties = TypeDescriptor.GetProperties(typeof(DataGrid));
                PropertyDescriptorCollection gridTableStyleProperties = TypeDescriptor.GetProperties(typeof(DataGridTableStyle)); 

                foreach (DataColumn c in row.Table.Columns) { 
                    object value = row[c]; 
                    PropertyDescriptor prop;
                    object component; 

                    if (IsTableProperty(c.ColumnName)) {
                        prop = gridTableStyleProperties[c.ColumnName];
                        component = this.tableStyle; 
                    } else {
                        prop = gridProperties[c.ColumnName]; 
                        component = this.dataGrid; 
                    }
 
                    if (prop != null) {
                        if (Convert.IsDBNull(value) || value.ToString().Length == 0) {
                            prop.ResetValue(component);
                        } else { 
                            try {
                                // Ignore errors setting up the preview... 
                                // The only one that really needs to be handled is the font property, 
                                // where the font in the scheme may not exist on the machine. (#56516)
 
                                TypeConverter converter = prop.Converter;
                                object convertedValue = converter.ConvertFromString(value.ToString());
                                prop.SetValue(component, convertedValue);
                            } catch { 
                            }
                        } 
                    } 
                }
            } 
            /*
            string pictureName = row["SchemePicture"].ToString();
            Bitmap picture = new Bitmap(typeof(DataGridAutoFormatDialog),pictureName);
            schemePicture.Image = picture; 
            */
        } 
 
        public DataRow SelectedData {
            get { 
                if (schemeName != null) {
                    // ListBox uses Windows.SendMessage(.., win.LB_GETCURSEL,... ) to determine the selection
                    // by the time that DataGridDesigner needs this information
                    // the call to SendMessage will fail. this is why we save 
                    // the selectedIndex
                    return ((DataRowView)schemeName.Items[this.selectedIndex]).Row; 
                } 
                return null;
            } 
        }

        internal const string scheme = "<xsd:schema id=\"pulica\" xmlns=\"\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\">" +
  "<xsd:element name=\"Scheme\">" + 
    "<xsd:complexType>" +
      "<xsd:all>" + 
        "<xsd:element name=\"SchemeName\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"SchemePicture\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"BorderStyle\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"FlatMode\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"Font\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"CaptionFont\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"HeaderFont\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"AlternatingBackColor\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"BackColor\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"BackgroundColor\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"CaptionForeColor\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"CaptionBackColor\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"ForeColor\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"GridLineColor\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"GridLineStyle\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"HeaderBackColor\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"HeaderForeColor\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"LinkColor\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"LinkHoverColor\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"ParentRowsBackColor\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"ParentRowsForeColor\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"SelectionForeColor\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"SelectionBackColor\" minOccurs=\"0\" type=\"xsd:string\"/>" +
      "</xsd:all>" +
    "</xsd:complexType>" + 
  "</xsd:element>" +
"</xsd:schema>"; 
        internal const string data = 
"<pulica>" +
  "<Scheme>" + 
    "<SchemeName>Default</SchemeName>" +
    "<SchemePicture>default.bmp</SchemePicture>" +
    "<BorderStyle></BorderStyle>" +
    "<FlatMode></FlatMode>" + 
    "<CaptionFont></CaptionFont>" +
    "<Font></Font>" + 
    "<HeaderFont></HeaderFont>" + 
    "<AlternatingBackColor></AlternatingBackColor>" +
    "<BackColor></BackColor>" + 
    "<CaptionForeColor></CaptionForeColor>" +
    "<CaptionBackColor></CaptionBackColor>" +
    "<ForeColor></ForeColor>" +
    "<GridLineColor></GridLineColor>" + 
    "<GridLineStyle></GridLineStyle>" +
    "<HeaderBackColor></HeaderBackColor>" + 
    "<HeaderForeColor></HeaderForeColor>" + 
    "<LinkColor></LinkColor>" +
    "<LinkHoverColor></LinkHoverColor>" + 
    "<ParentRowsBackColor></ParentRowsBackColor>" +
    "<ParentRowsForeColor></ParentRowsForeColor>" +
    "<SelectionForeColor></SelectionForeColor>" +
    "<SelectionBackColor></SelectionBackColor>" + 
  "</Scheme>" +
  "<Scheme>" + 
    "<SchemeName>Professional 1</SchemeName>" + 
    "<SchemePicture>professional1.bmp</SchemePicture>" +
    "<CaptionFont>Verdana, 10pt</CaptionFont>" + 
    "<AlternatingBackColor>LightGray</AlternatingBackColor>" +
    "<CaptionForeColor>Navy</CaptionForeColor>" +
    "<CaptionBackColor>White</CaptionBackColor>" +
    "<ForeColor>Black</ForeColor>" + 
    "<BackColor>DarkGray</BackColor>" +
    "<GridLineColor>Black</GridLineColor>" + 
    "<GridLineStyle>None</GridLineStyle>" + 
    "<HeaderBackColor>Silver</HeaderBackColor>" +
    "<HeaderForeColor>Black</HeaderForeColor>" + 
    "<LinkColor>Navy</LinkColor>" +
    "<LinkHoverColor>Blue</LinkHoverColor>" +
    "<ParentRowsBackColor>White</ParentRowsBackColor>" +
    "<ParentRowsForeColor>Black</ParentRowsForeColor>" + 
    "<SelectionForeColor>White</SelectionForeColor>" +
    "<SelectionBackColor>Navy</SelectionBackColor>" + 
  "</Scheme>" + 
  "<Scheme>" +
    "<SchemeName>Professional 2</SchemeName>" + 
    "<SchemePicture>professional2.bmp</SchemePicture>" +
    "<BorderStyle>FixedSingle</BorderStyle>" +
    "<FlatMode>True</FlatMode>" +
    "<CaptionFont>Tahoma, 8pt</CaptionFont>" + 
    "<AlternatingBackColor>Gainsboro</AlternatingBackColor>" +
    "<BackColor>Silver</BackColor>" + 
    "<CaptionForeColor>White</CaptionForeColor>" + 
    "<CaptionBackColor>DarkSlateBlue</CaptionBackColor>" +
    "<ForeColor>Black</ForeColor>" + 
    "<GridLineColor>White</GridLineColor>" +
    "<HeaderBackColor>DarkGray</HeaderBackColor>" +
    "<HeaderForeColor>Black</HeaderForeColor>" +
    "<LinkColor>DarkSlateBlue</LinkColor>" + 
    "<LinkHoverColor>RoyalBlue</LinkHoverColor>" +
    "<ParentRowsBackColor>Black</ParentRowsBackColor>" + 
    "<ParentRowsForeColor>White</ParentRowsForeColor>" + 
    "<SelectionForeColor>White</SelectionForeColor>" +
    "<SelectionBackColor>DarkSlateBlue</SelectionBackColor>" + 
  "</Scheme>" +
  "<Scheme>" +
    "<SchemeName>Professional 3</SchemeName>" +
    "<SchemePicture>professional3.bmp</SchemePicture>" + 
    "<BorderStyle>None</BorderStyle>" +
    "<FlatMode>True</FlatMode>" + 
    "<CaptionFont>Tahoma, 8pt, style=1</CaptionFont>" + 
    "<HeaderFont>Tahoma, 8pt, style=1</HeaderFont>" +
    "<Font>Tahoma, 8pt</Font>" + 
    "<AlternatingBackColor>LightGray</AlternatingBackColor>" +
    "<BackColor>Gainsboro</BackColor>" +
    "<BackgroundColor>Silver</BackgroundColor>" +
    "<CaptionForeColor>MidnightBlue</CaptionForeColor>" + 
    "<CaptionBackColor>LightSteelBlue</CaptionBackColor>" +
    "<ForeColor>Black</ForeColor>" + 
    "<GridLineColor>DimGray</GridLineColor>" + 
    "<GridLineStyle>None</GridLineStyle>" +
    "<HeaderBackColor>MidnightBlue</HeaderBackColor>" + 
    "<HeaderForeColor>White</HeaderForeColor>" +
    "<LinkColor>MidnightBlue</LinkColor>" +
    "<LinkHoverColor>RoyalBlue</LinkHoverColor>" +
    "<ParentRowsBackColor>DarkGray</ParentRowsBackColor>" + 
    "<ParentRowsForeColor>Black</ParentRowsForeColor>" +
    "<SelectionForeColor>White</SelectionForeColor>" + 
    "<SelectionBackColor>CadetBlue</SelectionBackColor>" + 
  "</Scheme>" +
  "<Scheme>" + 
    "<SchemeName>Professional 4</SchemeName>" +
    "<SchemePicture>professional4.bmp</SchemePicture>" +
    "<BorderStyle>None</BorderStyle>" +
    "<FlatMode>True</FlatMode>" + 
    "<CaptionFont>Tahoma, 8pt, style=1</CaptionFont>" +
    "<HeaderFont>Tahoma, 8pt, style=1</HeaderFont>" + 
    "<Font>Tahoma, 8pt</Font>" + 
    "<AlternatingBackColor>Lavender</AlternatingBackColor>" +
    "<BackColor>WhiteSmoke</BackColor>" + 
    "<BackgroundColor>LightGray</BackgroundColor>" +
    "<CaptionForeColor>MidnightBlue</CaptionForeColor>" +
    "<CaptionBackColor>LightSteelBlue</CaptionBackColor>" +
    "<ForeColor>MidnightBlue</ForeColor>" + 
    "<GridLineColor>Gainsboro</GridLineColor>" +
    "<GridLineStyle>None</GridLineStyle>" + 
    "<HeaderBackColor>MidnightBlue</HeaderBackColor>" + 
    "<HeaderForeColor>WhiteSmoke</HeaderForeColor>" +
    "<LinkColor>Teal</LinkColor>" + 
    "<LinkHoverColor>DarkMagenta</LinkHoverColor>" +
    "<ParentRowsBackColor>Gainsboro</ParentRowsBackColor>" +
    "<ParentRowsForeColor>MidnightBlue</ParentRowsForeColor>" +
    "<SelectionForeColor>WhiteSmoke</SelectionForeColor>" + 
    "<SelectionBackColor>CadetBlue</SelectionBackColor>" +
  "</Scheme>" + 
  "<Scheme>" + 
    "<SchemeName>Classic</SchemeName>" +
    "<SchemePicture>classic.bmp</SchemePicture>" + 
    "<BorderStyle>FixedSingle</BorderStyle>" +
    "<FlatMode>True</FlatMode>" +
    "<Font>Times New Roman, 9pt</Font>" +
    "<HeaderFont>Tahoma, 8pt, style=1</HeaderFont>" + 
    "<CaptionFont>Tahoma, 8pt, style=1</CaptionFont>" +
    "<AlternatingBackColor>WhiteSmoke</AlternatingBackColor>" + 
    "<BackColor>Gainsboro</BackColor>" + 
    "<BackgroundColor>DarkGray</BackgroundColor>" +
    "<CaptionForeColor>Black</CaptionForeColor>" + 
    "<CaptionBackColor>DarkKhaki</CaptionBackColor>" +
    "<ForeColor>Black</ForeColor>" +
    "<GridLineColor>Silver</GridLineColor>" +
    "<HeaderBackColor>Black</HeaderBackColor>" + 
    "<HeaderForeColor>White</HeaderForeColor>" +
    "<LinkColor>DarkSlateBlue</LinkColor>" + 
    "<LinkHoverColor>Firebrick</LinkHoverColor>" + 
    "<ParentRowsForeColor>Black</ParentRowsForeColor>" +
    "<ParentRowsBackColor>LightGray</ParentRowsBackColor>" + 
    "<SelectionForeColor>White</SelectionForeColor>" +
    "<SelectionBackColor>Firebrick</SelectionBackColor>" +
  "</Scheme>" +
  "<Scheme>" + 
    "<SchemeName>Simple</SchemeName>" +
    "<SchemePicture>Simple.bmp</SchemePicture>" + 
    "<BorderStyle>FixedSingle</BorderStyle>" + 
    "<FlatMode>True</FlatMode>" +
    "<Font>Courier New, 9pt</Font>" + 
    "<HeaderFont>Courier New, 10pt, style=1</HeaderFont>" +
    "<CaptionFont>Courier New, 10pt, style=1</CaptionFont>" +
    "<AlternatingBackColor>White</AlternatingBackColor>" +
    "<BackColor>White</BackColor>" + 
    "<BackgroundColor>Gainsboro</BackgroundColor>" +
    "<CaptionForeColor>Black</CaptionForeColor>" + 
    "<CaptionBackColor>Silver</CaptionBackColor>" + 
    "<ForeColor>DarkSlateGray</ForeColor>" +
    "<GridLineColor>DarkGray</GridLineColor>" + 
    "<HeaderBackColor>DarkGreen</HeaderBackColor>" +
    "<HeaderForeColor>White</HeaderForeColor>" +
    "<LinkColor>DarkGreen</LinkColor>" +
    "<LinkHoverColor>Blue</LinkHoverColor>" + 
    "<ParentRowsForeColor>Black</ParentRowsForeColor>" +
    "<ParentRowsBackColor>Gainsboro</ParentRowsBackColor>" + 
    "<SelectionForeColor>Black</SelectionForeColor>" + 
    "<SelectionBackColor>DarkSeaGreen</SelectionBackColor>" +
  "</Scheme>" + 
  "<Scheme>" +
    "<SchemeName>Colorful 1</SchemeName>" +
    "<SchemePicture>colorful1.bmp</SchemePicture>" +
    "<BorderStyle>FixedSingle</BorderStyle>" + 
    "<FlatMode>True</FlatMode>" +
    "<Font>Tahoma, 8pt</Font>" + 
    "<CaptionFont>Tahoma, 9pt, style=1</CaptionFont>" + 
    "<HeaderFont>Tahoma, 9pt, style=1</HeaderFont>" +
    "<AlternatingBackColor>LightGoldenrodYellow</AlternatingBackColor>" + 
    "<BackColor>White</BackColor>" +
    "<BackgroundColor>LightGoldenrodYellow</BackgroundColor>" +
    "<CaptionForeColor>DarkSlateBlue</CaptionForeColor>" +
    "<CaptionBackColor>LightGoldenrodYellow</CaptionBackColor>" + 
    "<ForeColor>DarkSlateBlue</ForeColor>" +
    "<GridLineColor>Peru</GridLineColor>" + 
    "<GridLineStyle>None</GridLineStyle>" + 
    "<HeaderBackColor>Maroon</HeaderBackColor>" +
    "<HeaderForeColor>LightGoldenrodYellow</HeaderForeColor>" + 
    "<LinkColor>Maroon</LinkColor>" +
    "<LinkHoverColor>SlateBlue</LinkHoverColor>" +
    "<ParentRowsBackColor>BurlyWood</ParentRowsBackColor>" +
    "<ParentRowsForeColor>DarkSlateBlue</ParentRowsForeColor>" + 
    "<SelectionForeColor>GhostWhite</SelectionForeColor>" +
    "<SelectionBackColor>DarkSlateBlue</SelectionBackColor>" + 
  "</Scheme>" + 
  "<Scheme>" +
    "<SchemeName>Colorful 2</SchemeName>" + 
    "<SchemePicture>colorful2.bmp</SchemePicture>" +
    "<BorderStyle>None</BorderStyle>" +
    "<FlatMode>True</FlatMode>" +
    "<Font>Tahoma, 8pt</Font>" + 
    "<CaptionFont>Tahoma, 8pt, style=1</CaptionFont>" +
    "<HeaderFont>Tahoma, 8pt, style=1</HeaderFont>" + 
    "<AlternatingBackColor>GhostWhite</AlternatingBackColor>" + 
    "<BackColor>GhostWhite</BackColor>" +
    "<BackgroundColor>Lavender</BackgroundColor>" + 
    "<CaptionForeColor>White</CaptionForeColor>" +
    "<CaptionBackColor>RoyalBlue</CaptionBackColor>" +
    "<ForeColor>MidnightBlue</ForeColor>" +
    "<GridLineColor>RoyalBlue</GridLineColor>" + 
    "<HeaderBackColor>MidnightBlue</HeaderBackColor>" +
    "<HeaderForeColor>Lavender</HeaderForeColor>" + 
    "<LinkColor>Teal</LinkColor>" + 
    "<LinkHoverColor>DodgerBlue</LinkHoverColor>" +
    "<ParentRowsBackColor>Lavender</ParentRowsBackColor>" + 
    "<ParentRowsForeColor>MidnightBlue</ParentRowsForeColor>" +
    "<SelectionForeColor>PaleGreen</SelectionForeColor>" +
    "<SelectionBackColor>Teal</SelectionBackColor>" +
  "</Scheme>" + 
  "<Scheme>" +
    "<SchemeName>Colorful 3</SchemeName>" + 
    "<SchemePicture>colorful3.bmp</SchemePicture>" + 
    "<BorderStyle>None</BorderStyle>" +
    "<FlatMode>True</FlatMode>" + 
    "<Font>Tahoma, 8pt</Font>" +
    "<CaptionFont>Tahoma, 8pt, style=1</CaptionFont>" +
    "<HeaderFont>Tahoma, 8pt, style=1</HeaderFont>" +
    "<AlternatingBackColor>OldLace</AlternatingBackColor>" + 
    "<BackColor>OldLace</BackColor>" +
    "<BackgroundColor>Tan</BackgroundColor>" + 
    "<CaptionForeColor>OldLace</CaptionForeColor>" + 
    "<CaptionBackColor>SaddleBrown</CaptionBackColor>" +
    "<ForeColor>DarkSlateGray</ForeColor>" + 
    "<GridLineColor>Tan</GridLineColor>" +
    "<GridLineStyle>Solid</GridLineStyle>" +
    "<HeaderBackColor>Wheat</HeaderBackColor>" +
    "<HeaderForeColor>SaddleBrown</HeaderForeColor>" + 
    "<LinkColor>DarkSlateBlue</LinkColor>" +
    "<LinkHoverColor>Teal</LinkHoverColor>" + 
    "<ParentRowsBackColor>OldLace</ParentRowsBackColor>" + 
    "<ParentRowsForeColor>DarkSlateGray</ParentRowsForeColor>" +
    "<SelectionForeColor>White</SelectionForeColor>" + 
    "<SelectionBackColor>SlateGray</SelectionBackColor>" +
  "</Scheme>" +
  "<Scheme>" +
    "<SchemeName>Colorful 4</SchemeName>" + 
    "<SchemePicture>colorful4.bmp</SchemePicture>" +
    "<BorderStyle>FixedSingle</BorderStyle>" + 
    "<FlatMode>True</FlatMode>" + 
    "<Font>Tahoma, 8pt</Font>" +
    "<CaptionFont>Tahoma, 8pt, style=1</CaptionFont>" + 
    "<HeaderFont>Tahoma, 8pt, style=1</HeaderFont>" +
    "<AlternatingBackColor>White</AlternatingBackColor>" +
    "<BackColor>White</BackColor>" +
    "<BackgroundColor>Ivory</BackgroundColor>" + 
    "<CaptionForeColor>Lavender</CaptionForeColor>" +
    "<CaptionBackColor>DarkSlateBlue</CaptionBackColor>" + 
    "<ForeColor>Black</ForeColor>" + 
    "<GridLineColor>Wheat</GridLineColor>" +
    "<HeaderBackColor>CadetBlue</HeaderBackColor>" + 
    "<HeaderForeColor>Black</HeaderForeColor>" +
    "<LinkColor>DarkSlateBlue</LinkColor>" +
    "<LinkHoverColor>LightSeaGreen</LinkHoverColor>" +
    "<ParentRowsBackColor>Ivory</ParentRowsBackColor>" + 
    "<ParentRowsForeColor>Black</ParentRowsForeColor>" +
    "<SelectionForeColor>DarkSlateBlue</SelectionForeColor>" + 
    "<SelectionBackColor>Wheat</SelectionBackColor>" + 
  "</Scheme>" +
  "<Scheme>" + 
    "<SchemeName>256 Color 1</SchemeName>" +
    "<SchemePicture>256_1.bmp</SchemePicture>" +
    "<Font>Tahoma, 8pt</Font>" +
    "<CaptionFont>Tahoma, 8 pt</CaptionFont>" + 
    "<HeaderFont>Tahoma, 8pt</HeaderFont>" +
    "<AlternatingBackColor>Silver</AlternatingBackColor>" + 
    "<BackColor>White</BackColor>" + 
    "<CaptionForeColor>White</CaptionForeColor>" +
    "<CaptionBackColor>Maroon</CaptionBackColor>" + 
    "<ForeColor>Black</ForeColor>" +
    "<GridLineColor>Silver</GridLineColor>" +
    "<HeaderBackColor>Silver</HeaderBackColor>" +
    "<HeaderForeColor>Black</HeaderForeColor>" + 
    "<LinkColor>Maroon</LinkColor>" +
    "<LinkHoverColor>Red</LinkHoverColor>" + 
    "<ParentRowsBackColor>Silver</ParentRowsBackColor>" + 
    "<ParentRowsForeColor>Black</ParentRowsForeColor>" +
    "<SelectionForeColor>White</SelectionForeColor>" + 
    "<SelectionBackColor>Maroon</SelectionBackColor>" +
  "</Scheme>" +
  "<Scheme>" +
    "<SchemeName>256 Color 2</SchemeName>" + 
    "<SchemePicture>256_2.bmp</SchemePicture>" +
    "<BorderStyle>FixedSingle</BorderStyle>" + 
    "<FlatMode>True</FlatMode>" + 
    "<CaptionFont>Microsoft Sans Serif, 10 pt, style=1</CaptionFont>" +
    "<Font>Tahoma, 8pt</Font>" + 
    "<HeaderFont>Tahoma, 8pt</HeaderFont>" +
    "<AlternatingBackColor>White</AlternatingBackColor>" +
    "<BackColor>White</BackColor>" +
    "<CaptionForeColor>White</CaptionForeColor>" + 
    "<CaptionBackColor>Teal</CaptionBackColor>" +
    "<ForeColor>Black</ForeColor>" + 
    "<GridLineColor>Silver</GridLineColor>" + 
    "<HeaderBackColor>Black</HeaderBackColor>" +
    "<HeaderForeColor>White</HeaderForeColor>" + 
    "<LinkColor>Purple</LinkColor>" +
    "<LinkHoverColor>Fuchsia</LinkHoverColor>" +
    "<ParentRowsBackColor>Gray</ParentRowsBackColor>" +
    "<ParentRowsForeColor>White</ParentRowsForeColor>" + 
    "<SelectionForeColor>White</SelectionForeColor>" +
    "<SelectionBackColor>Maroon</SelectionBackColor>" + 
  "</Scheme>" + 
"</pulica>";
 
        private class AutoFormatDataGrid : DataGrid {
            protected override void OnKeyDown(KeyEventArgs e) {
            }
            protected override bool ProcessDialogKey(Keys keyData) { 
                return false;
            } 
            protected override bool ProcessKeyPreview(ref Message m) { 
                return false;
            } 
            protected override void OnMouseDown(MouseEventArgs e) { }
            protected override void OnMouseUp(MouseEventArgs e) { }
            protected override void OnMouseMove(MouseEventArgs e) { }
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataGridAutoFormatDialog.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System;
    using System.Design; 
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Data;
    using System.Windows.Forms; 
    using System.Drawing; 
    using System.Xml;
    using System.IO; 
    using System.Globalization;

    /// <include file='doc\DataGridAutoFormatDialog.uex' path='docs/doc[@for="DataGridAutoFormatDialog"]/*' />
    /// <internalonly/> 
    /// <devdoc>
    /// </devdoc> 
    internal class DataGridAutoFormatDialog : Form { 

        private DataGrid dgrid; 

        private DataTable schemeTable;
        // private PictureBox schemePicture;
        DataSet dataSet = new DataSet(); 
        private AutoFormatDataGrid dataGrid;
        private DataGridTableStyle tableStyle; 
        private Button button2; 
        private Button button1;
        private ListBox schemeName; 
        private Label formats;
        private Label preview;
        private bool IMBusy;
        private TableLayoutPanel okCancelTableLayoutPanel; 
        private TableLayoutPanel overarchingTableLayoutPanel;
 
        private int selectedIndex = -1; 

        internal DataGridAutoFormatDialog(DataGrid dgrid) { 
            this.dgrid = dgrid;

            this.ShowInTaskbar = false;
            dataSet.Locale = CultureInfo.InvariantCulture; 
            dataSet.ReadXmlSchema(new XmlTextReader(new StringReader(scheme)));
            dataSet.ReadXml(new StringReader(data), XmlReadMode.IgnoreSchema); 
            schemeTable = dataSet.Tables["Scheme"]; 

            IMBusy = true; 

            InitializeComponent();

            schemeName.DataSource = schemeTable; 

            AddDataToDataGrid(); 
            AddStyleSheetInformationToDataGrid(); 

            if (dgrid.Site != null) { 
                IUIService uiService = (IUIService)dgrid.Site.GetService(typeof(IUIService));
                if (uiService != null) {
                    Font f = (Font)uiService.Styles["DialogFont"];
                    if (f != null) { 
                        this.Font = f;
                    } 
                } 
            }
 
            //this.Focus(); causes an assertion failure - and is not required.
            IMBusy = false;
        }
 
        private void AddStyleSheetInformationToDataGrid() {
            DataGridTableStyle dGTStyle = new DataGridTableStyle(); 
            dGTStyle.MappingName = "Table1"; 
            DataGridColumnStyle col1 = new DataGridTextBoxColumn();
            col1.MappingName = "First Name"; 
            col1.HeaderText = SR.GetString(SR.DataGridAutoFormatTableFirstColumn);

            DataGridColumnStyle col2 = new DataGridTextBoxColumn();
            col2.MappingName = "Last Name"; 
            col2.HeaderText = SR.GetString(SR.DataGridAutoFormatTableSecondColumn);
 
            dGTStyle.GridColumnStyles.Add(col1); 
            dGTStyle.GridColumnStyles.Add(col2);
 
            DataRowCollection drc = dataSet.Tables["Scheme"].Rows;
            DataRow dr;
            dr = drc[0];
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameDefault); 
            dr = drc[1];
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameProfessional1); 
            dr = drc[2]; 
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameProfessional2);
            dr = drc[3]; 
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameProfessional3);
            dr = drc[4];
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameProfessional4);
            dr = drc[5]; 
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameClassic);
            dr = drc[6]; 
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameSimple); 
            dr = drc[7];
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameColorful1); 
            dr = drc[8];
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameColorful2);
            dr = drc[9];
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameColorful3); 
            dr = drc[10];
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeNameColorful4); 
            dr = drc[11]; 
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeName256Color1);
            dr = drc[12]; 
            dr["SchemeName"] = SR.GetString(SR.DataGridAutoFormatSchemeName256Color2);

            this.dataGrid.TableStyles.Add(dGTStyle);
            this.tableStyle = dGTStyle; 
        }
 
        private void AddDataToDataGrid() { 
            DataTable dTable = new DataTable("Table1");
            dTable.Locale = CultureInfo.InvariantCulture; 
            dTable.Columns.Add(new DataColumn("First Name"));
            dTable.Columns.Add(new DataColumn("Last Name"));

            DataRow dRow = dTable.NewRow(); 
            dRow["First Name"] = "Robert";
            dRow["Last Name"] = "Brown"; 
            dTable.Rows.Add(dRow); 

            dRow = dTable.NewRow(); 
            dRow["First Name"] = "Nate";
            dRow["Last Name"] = "Sun";
            dTable.Rows.Add(dRow);
 
            dRow = dTable.NewRow();
            dRow["First Name"] = "Carole"; 
            dRow["Last Name"] = "Poland"; 
            dTable.Rows.Add(dRow);
 
            this.dataGrid.SetDataBinding(dTable, "");
        }

        private void AutoFormat_HelpRequested(object sender, HelpEventArgs e) { 
            if (dgrid == null || dgrid.Site == null)
                return; 
            IDesignerHost host = dgrid.Site.GetService(typeof(IDesignerHost)) as IDesignerHost; 
            if (host == null) {
                Debug.Fail("Unable to get IDesignerHost."); 
                return;
            }

            IHelpService helpService = host.GetService(typeof(IHelpService)) as IHelpService; 
            if (helpService != null) {
                helpService.ShowHelpFromKeyword("vs.DataGridAutoFormatDialog"); 
            } else { 
                Debug.Fail("Unable to get IHelpService.");
            } 
        }

        private void Button1_Clicked(object sender, EventArgs e) {
            selectedIndex = schemeName.SelectedIndex; 
        }
 
        private void InitializeComponent() { 
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataGridAutoFormatDialog));
            this.formats = new System.Windows.Forms.Label(); 
            this.schemeName = new System.Windows.Forms.ListBox();
            dataGrid = new AutoFormatDataGrid();
            this.preview = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button(); 
            this.button2 = new System.Windows.Forms.Button();
            this.okCancelTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            this.overarchingTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel(); 
            ((System.ComponentModel.ISupportInitialize)(this.dataGrid)).BeginInit();
            this.okCancelTableLayoutPanel.SuspendLayout(); 
            this.overarchingTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // formats 
            //
            resources.ApplyResources(this.formats, "formats"); 
            this.formats.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0); 
            this.formats.Name = "formats";
            // 
            // schemeName
            //
            resources.ApplyResources(this.schemeName, "schemeName");
            this.schemeName.DisplayMember = "SchemeName"; 
            this.schemeName.FormattingEnabled = true;
            this.schemeName.Margin = new System.Windows.Forms.Padding(0, 2, 3, 3); 
            this.schemeName.Name = "schemeName"; 
            this.schemeName.SelectedIndexChanged += new System.EventHandler(this.SchemeName_SelectionChanged);
            // 
            // dataGrid
            //
            resources.ApplyResources(this.dataGrid, "dataGrid");
            this.dataGrid.DataMember = ""; 
            this.dataGrid.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dataGrid.Margin = new System.Windows.Forms.Padding(3, 2, 0, 3); 
            this.dataGrid.Name = "dataGrid"; 
            //
            // preview 
            //
            resources.ApplyResources(this.preview, "preview");
            this.preview.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.preview.Name = "preview"; 
            //
            // button1 
            // 
            resources.ApplyResources(this.button1, "button1");
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK; 
            this.button1.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.button1.MinimumSize = new System.Drawing.Size(75, 23);
            this.button1.Name = "button1";
            this.button1.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0); 
            this.button1.Click += new System.EventHandler(this.Button1_Clicked);
            // 
            // button2 
            //
            resources.ApplyResources(this.button2, "button2"); 
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button2.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.button2.MinimumSize = new System.Drawing.Size(75, 23);
            this.button2.Name = "button2"; 
            this.button2.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            // 
            // okCancelTableLayoutPanel 
            //
            resources.ApplyResources(this.okCancelTableLayoutPanel, "okCancelTableLayoutPanel"); 
            this.overarchingTableLayoutPanel.SetColumnSpan(this.okCancelTableLayoutPanel, 2);
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.okCancelTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.okCancelTableLayoutPanel.Controls.Add(this.button1, 0, 0); 
            this.okCancelTableLayoutPanel.Controls.Add(this.button2, 1, 0);
            this.okCancelTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0); 
            this.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel"; 
            this.okCancelTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            // 
            // overarchingTableLayoutPanel
            //
            resources.ApplyResources(this.overarchingTableLayoutPanel, "overarchingTableLayoutPanel");
            this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 146F)); 
            this.overarchingTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 182F));
            this.overarchingTableLayoutPanel.Controls.Add(this.okCancelTableLayoutPanel, 0, 2); 
            this.overarchingTableLayoutPanel.Controls.Add(this.preview, 1, 0); 
            this.overarchingTableLayoutPanel.Controls.Add(this.dataGrid, 1, 1);
            this.overarchingTableLayoutPanel.Controls.Add(this.formats, 0, 0); 
            this.overarchingTableLayoutPanel.Controls.Add(this.schemeName, 0, 1);
            this.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel";
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F)); 
            this.overarchingTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            // 
            // DataGridAutoFormatDialog 
            //
            this.AcceptButton = this.button1; 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.button2;
            this.Controls.Add(this.overarchingTableLayoutPanel); 
            this.MaximizeBox = false;
            this.MinimizeBox = false; 
            this.Name = "DataGridAutoFormatDialog"; 
            this.ShowIcon = false;
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.AutoFormat_HelpRequested); 
            ((System.ComponentModel.ISupportInitialize)(this.dataGrid)).EndInit();
            this.okCancelTableLayoutPanel.ResumeLayout(false);
            this.okCancelTableLayoutPanel.PerformLayout();
            this.overarchingTableLayoutPanel.ResumeLayout(false); 
            this.overarchingTableLayoutPanel.PerformLayout();
            this.ResumeLayout(false); 
        } 

        private static bool IsTableProperty(string propName) { 
            if (propName.Equals("HeaderColor"))
                return true;
            if (propName.Equals("AlternatingBackColor"))
                return true; 
            if (propName.Equals("BackColor"))
                return true; 
            if (propName.Equals("ForeColor")) 
                return true;
            if (propName.Equals("GridLineColor")) 
                return true;
            if (propName.Equals("GridLineStyle"))
                return true;
            if (propName.Equals("HeaderBackColor")) 
                return true;
            if (propName.Equals("HeaderForeColor")) 
                return true; 
            if (propName.Equals("LinkColor"))
                return true; 
            if (propName.Equals("LinkHoverColor"))
                return true;
            if (propName.Equals("SelectionForeColor"))
                return true; 
            if (propName.Equals("SelectionBackColor"))
                return true; 
            if (propName.Equals("HeaderFont")) 
                return true;
            return false; 
        }

        [
            SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")   // See comment inside the method about ignoring errors. 
        ]
        private void SchemeName_SelectionChanged(object sender, EventArgs e) { 
            if (IMBusy) 
                return;
 
            DataRow row = ((DataRowView)schemeName.SelectedItem).Row;
            if (row != null) {
                PropertyDescriptorCollection gridProperties = TypeDescriptor.GetProperties(typeof(DataGrid));
                PropertyDescriptorCollection gridTableStyleProperties = TypeDescriptor.GetProperties(typeof(DataGridTableStyle)); 

                foreach (DataColumn c in row.Table.Columns) { 
                    object value = row[c]; 
                    PropertyDescriptor prop;
                    object component; 

                    if (IsTableProperty(c.ColumnName)) {
                        prop = gridTableStyleProperties[c.ColumnName];
                        component = this.tableStyle; 
                    } else {
                        prop = gridProperties[c.ColumnName]; 
                        component = this.dataGrid; 
                    }
 
                    if (prop != null) {
                        if (Convert.IsDBNull(value) || value.ToString().Length == 0) {
                            prop.ResetValue(component);
                        } else { 
                            try {
                                // Ignore errors setting up the preview... 
                                // The only one that really needs to be handled is the font property, 
                                // where the font in the scheme may not exist on the machine. (#56516)
 
                                TypeConverter converter = prop.Converter;
                                object convertedValue = converter.ConvertFromString(value.ToString());
                                prop.SetValue(component, convertedValue);
                            } catch { 
                            }
                        } 
                    } 
                }
            } 
            /*
            string pictureName = row["SchemePicture"].ToString();
            Bitmap picture = new Bitmap(typeof(DataGridAutoFormatDialog),pictureName);
            schemePicture.Image = picture; 
            */
        } 
 
        public DataRow SelectedData {
            get { 
                if (schemeName != null) {
                    // ListBox uses Windows.SendMessage(.., win.LB_GETCURSEL,... ) to determine the selection
                    // by the time that DataGridDesigner needs this information
                    // the call to SendMessage will fail. this is why we save 
                    // the selectedIndex
                    return ((DataRowView)schemeName.Items[this.selectedIndex]).Row; 
                } 
                return null;
            } 
        }

        internal const string scheme = "<xsd:schema id=\"pulica\" xmlns=\"\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\">" +
  "<xsd:element name=\"Scheme\">" + 
    "<xsd:complexType>" +
      "<xsd:all>" + 
        "<xsd:element name=\"SchemeName\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"SchemePicture\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"BorderStyle\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"FlatMode\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"Font\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"CaptionFont\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"HeaderFont\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"AlternatingBackColor\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"BackColor\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"BackgroundColor\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"CaptionForeColor\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"CaptionBackColor\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"ForeColor\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"GridLineColor\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"GridLineStyle\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"HeaderBackColor\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"HeaderForeColor\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"LinkColor\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"LinkHoverColor\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"ParentRowsBackColor\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"ParentRowsForeColor\" minOccurs=\"0\" type=\"xsd:string\"/>" + 
        "<xsd:element name=\"SelectionForeColor\" minOccurs=\"0\" type=\"xsd:string\"/>" +
        "<xsd:element name=\"SelectionBackColor\" minOccurs=\"0\" type=\"xsd:string\"/>" +
      "</xsd:all>" +
    "</xsd:complexType>" + 
  "</xsd:element>" +
"</xsd:schema>"; 
        internal const string data = 
"<pulica>" +
  "<Scheme>" + 
    "<SchemeName>Default</SchemeName>" +
    "<SchemePicture>default.bmp</SchemePicture>" +
    "<BorderStyle></BorderStyle>" +
    "<FlatMode></FlatMode>" + 
    "<CaptionFont></CaptionFont>" +
    "<Font></Font>" + 
    "<HeaderFont></HeaderFont>" + 
    "<AlternatingBackColor></AlternatingBackColor>" +
    "<BackColor></BackColor>" + 
    "<CaptionForeColor></CaptionForeColor>" +
    "<CaptionBackColor></CaptionBackColor>" +
    "<ForeColor></ForeColor>" +
    "<GridLineColor></GridLineColor>" + 
    "<GridLineStyle></GridLineStyle>" +
    "<HeaderBackColor></HeaderBackColor>" + 
    "<HeaderForeColor></HeaderForeColor>" + 
    "<LinkColor></LinkColor>" +
    "<LinkHoverColor></LinkHoverColor>" + 
    "<ParentRowsBackColor></ParentRowsBackColor>" +
    "<ParentRowsForeColor></ParentRowsForeColor>" +
    "<SelectionForeColor></SelectionForeColor>" +
    "<SelectionBackColor></SelectionBackColor>" + 
  "</Scheme>" +
  "<Scheme>" + 
    "<SchemeName>Professional 1</SchemeName>" + 
    "<SchemePicture>professional1.bmp</SchemePicture>" +
    "<CaptionFont>Verdana, 10pt</CaptionFont>" + 
    "<AlternatingBackColor>LightGray</AlternatingBackColor>" +
    "<CaptionForeColor>Navy</CaptionForeColor>" +
    "<CaptionBackColor>White</CaptionBackColor>" +
    "<ForeColor>Black</ForeColor>" + 
    "<BackColor>DarkGray</BackColor>" +
    "<GridLineColor>Black</GridLineColor>" + 
    "<GridLineStyle>None</GridLineStyle>" + 
    "<HeaderBackColor>Silver</HeaderBackColor>" +
    "<HeaderForeColor>Black</HeaderForeColor>" + 
    "<LinkColor>Navy</LinkColor>" +
    "<LinkHoverColor>Blue</LinkHoverColor>" +
    "<ParentRowsBackColor>White</ParentRowsBackColor>" +
    "<ParentRowsForeColor>Black</ParentRowsForeColor>" + 
    "<SelectionForeColor>White</SelectionForeColor>" +
    "<SelectionBackColor>Navy</SelectionBackColor>" + 
  "</Scheme>" + 
  "<Scheme>" +
    "<SchemeName>Professional 2</SchemeName>" + 
    "<SchemePicture>professional2.bmp</SchemePicture>" +
    "<BorderStyle>FixedSingle</BorderStyle>" +
    "<FlatMode>True</FlatMode>" +
    "<CaptionFont>Tahoma, 8pt</CaptionFont>" + 
    "<AlternatingBackColor>Gainsboro</AlternatingBackColor>" +
    "<BackColor>Silver</BackColor>" + 
    "<CaptionForeColor>White</CaptionForeColor>" + 
    "<CaptionBackColor>DarkSlateBlue</CaptionBackColor>" +
    "<ForeColor>Black</ForeColor>" + 
    "<GridLineColor>White</GridLineColor>" +
    "<HeaderBackColor>DarkGray</HeaderBackColor>" +
    "<HeaderForeColor>Black</HeaderForeColor>" +
    "<LinkColor>DarkSlateBlue</LinkColor>" + 
    "<LinkHoverColor>RoyalBlue</LinkHoverColor>" +
    "<ParentRowsBackColor>Black</ParentRowsBackColor>" + 
    "<ParentRowsForeColor>White</ParentRowsForeColor>" + 
    "<SelectionForeColor>White</SelectionForeColor>" +
    "<SelectionBackColor>DarkSlateBlue</SelectionBackColor>" + 
  "</Scheme>" +
  "<Scheme>" +
    "<SchemeName>Professional 3</SchemeName>" +
    "<SchemePicture>professional3.bmp</SchemePicture>" + 
    "<BorderStyle>None</BorderStyle>" +
    "<FlatMode>True</FlatMode>" + 
    "<CaptionFont>Tahoma, 8pt, style=1</CaptionFont>" + 
    "<HeaderFont>Tahoma, 8pt, style=1</HeaderFont>" +
    "<Font>Tahoma, 8pt</Font>" + 
    "<AlternatingBackColor>LightGray</AlternatingBackColor>" +
    "<BackColor>Gainsboro</BackColor>" +
    "<BackgroundColor>Silver</BackgroundColor>" +
    "<CaptionForeColor>MidnightBlue</CaptionForeColor>" + 
    "<CaptionBackColor>LightSteelBlue</CaptionBackColor>" +
    "<ForeColor>Black</ForeColor>" + 
    "<GridLineColor>DimGray</GridLineColor>" + 
    "<GridLineStyle>None</GridLineStyle>" +
    "<HeaderBackColor>MidnightBlue</HeaderBackColor>" + 
    "<HeaderForeColor>White</HeaderForeColor>" +
    "<LinkColor>MidnightBlue</LinkColor>" +
    "<LinkHoverColor>RoyalBlue</LinkHoverColor>" +
    "<ParentRowsBackColor>DarkGray</ParentRowsBackColor>" + 
    "<ParentRowsForeColor>Black</ParentRowsForeColor>" +
    "<SelectionForeColor>White</SelectionForeColor>" + 
    "<SelectionBackColor>CadetBlue</SelectionBackColor>" + 
  "</Scheme>" +
  "<Scheme>" + 
    "<SchemeName>Professional 4</SchemeName>" +
    "<SchemePicture>professional4.bmp</SchemePicture>" +
    "<BorderStyle>None</BorderStyle>" +
    "<FlatMode>True</FlatMode>" + 
    "<CaptionFont>Tahoma, 8pt, style=1</CaptionFont>" +
    "<HeaderFont>Tahoma, 8pt, style=1</HeaderFont>" + 
    "<Font>Tahoma, 8pt</Font>" + 
    "<AlternatingBackColor>Lavender</AlternatingBackColor>" +
    "<BackColor>WhiteSmoke</BackColor>" + 
    "<BackgroundColor>LightGray</BackgroundColor>" +
    "<CaptionForeColor>MidnightBlue</CaptionForeColor>" +
    "<CaptionBackColor>LightSteelBlue</CaptionBackColor>" +
    "<ForeColor>MidnightBlue</ForeColor>" + 
    "<GridLineColor>Gainsboro</GridLineColor>" +
    "<GridLineStyle>None</GridLineStyle>" + 
    "<HeaderBackColor>MidnightBlue</HeaderBackColor>" + 
    "<HeaderForeColor>WhiteSmoke</HeaderForeColor>" +
    "<LinkColor>Teal</LinkColor>" + 
    "<LinkHoverColor>DarkMagenta</LinkHoverColor>" +
    "<ParentRowsBackColor>Gainsboro</ParentRowsBackColor>" +
    "<ParentRowsForeColor>MidnightBlue</ParentRowsForeColor>" +
    "<SelectionForeColor>WhiteSmoke</SelectionForeColor>" + 
    "<SelectionBackColor>CadetBlue</SelectionBackColor>" +
  "</Scheme>" + 
  "<Scheme>" + 
    "<SchemeName>Classic</SchemeName>" +
    "<SchemePicture>classic.bmp</SchemePicture>" + 
    "<BorderStyle>FixedSingle</BorderStyle>" +
    "<FlatMode>True</FlatMode>" +
    "<Font>Times New Roman, 9pt</Font>" +
    "<HeaderFont>Tahoma, 8pt, style=1</HeaderFont>" + 
    "<CaptionFont>Tahoma, 8pt, style=1</CaptionFont>" +
    "<AlternatingBackColor>WhiteSmoke</AlternatingBackColor>" + 
    "<BackColor>Gainsboro</BackColor>" + 
    "<BackgroundColor>DarkGray</BackgroundColor>" +
    "<CaptionForeColor>Black</CaptionForeColor>" + 
    "<CaptionBackColor>DarkKhaki</CaptionBackColor>" +
    "<ForeColor>Black</ForeColor>" +
    "<GridLineColor>Silver</GridLineColor>" +
    "<HeaderBackColor>Black</HeaderBackColor>" + 
    "<HeaderForeColor>White</HeaderForeColor>" +
    "<LinkColor>DarkSlateBlue</LinkColor>" + 
    "<LinkHoverColor>Firebrick</LinkHoverColor>" + 
    "<ParentRowsForeColor>Black</ParentRowsForeColor>" +
    "<ParentRowsBackColor>LightGray</ParentRowsBackColor>" + 
    "<SelectionForeColor>White</SelectionForeColor>" +
    "<SelectionBackColor>Firebrick</SelectionBackColor>" +
  "</Scheme>" +
  "<Scheme>" + 
    "<SchemeName>Simple</SchemeName>" +
    "<SchemePicture>Simple.bmp</SchemePicture>" + 
    "<BorderStyle>FixedSingle</BorderStyle>" + 
    "<FlatMode>True</FlatMode>" +
    "<Font>Courier New, 9pt</Font>" + 
    "<HeaderFont>Courier New, 10pt, style=1</HeaderFont>" +
    "<CaptionFont>Courier New, 10pt, style=1</CaptionFont>" +
    "<AlternatingBackColor>White</AlternatingBackColor>" +
    "<BackColor>White</BackColor>" + 
    "<BackgroundColor>Gainsboro</BackgroundColor>" +
    "<CaptionForeColor>Black</CaptionForeColor>" + 
    "<CaptionBackColor>Silver</CaptionBackColor>" + 
    "<ForeColor>DarkSlateGray</ForeColor>" +
    "<GridLineColor>DarkGray</GridLineColor>" + 
    "<HeaderBackColor>DarkGreen</HeaderBackColor>" +
    "<HeaderForeColor>White</HeaderForeColor>" +
    "<LinkColor>DarkGreen</LinkColor>" +
    "<LinkHoverColor>Blue</LinkHoverColor>" + 
    "<ParentRowsForeColor>Black</ParentRowsForeColor>" +
    "<ParentRowsBackColor>Gainsboro</ParentRowsBackColor>" + 
    "<SelectionForeColor>Black</SelectionForeColor>" + 
    "<SelectionBackColor>DarkSeaGreen</SelectionBackColor>" +
  "</Scheme>" + 
  "<Scheme>" +
    "<SchemeName>Colorful 1</SchemeName>" +
    "<SchemePicture>colorful1.bmp</SchemePicture>" +
    "<BorderStyle>FixedSingle</BorderStyle>" + 
    "<FlatMode>True</FlatMode>" +
    "<Font>Tahoma, 8pt</Font>" + 
    "<CaptionFont>Tahoma, 9pt, style=1</CaptionFont>" + 
    "<HeaderFont>Tahoma, 9pt, style=1</HeaderFont>" +
    "<AlternatingBackColor>LightGoldenrodYellow</AlternatingBackColor>" + 
    "<BackColor>White</BackColor>" +
    "<BackgroundColor>LightGoldenrodYellow</BackgroundColor>" +
    "<CaptionForeColor>DarkSlateBlue</CaptionForeColor>" +
    "<CaptionBackColor>LightGoldenrodYellow</CaptionBackColor>" + 
    "<ForeColor>DarkSlateBlue</ForeColor>" +
    "<GridLineColor>Peru</GridLineColor>" + 
    "<GridLineStyle>None</GridLineStyle>" + 
    "<HeaderBackColor>Maroon</HeaderBackColor>" +
    "<HeaderForeColor>LightGoldenrodYellow</HeaderForeColor>" + 
    "<LinkColor>Maroon</LinkColor>" +
    "<LinkHoverColor>SlateBlue</LinkHoverColor>" +
    "<ParentRowsBackColor>BurlyWood</ParentRowsBackColor>" +
    "<ParentRowsForeColor>DarkSlateBlue</ParentRowsForeColor>" + 
    "<SelectionForeColor>GhostWhite</SelectionForeColor>" +
    "<SelectionBackColor>DarkSlateBlue</SelectionBackColor>" + 
  "</Scheme>" + 
  "<Scheme>" +
    "<SchemeName>Colorful 2</SchemeName>" + 
    "<SchemePicture>colorful2.bmp</SchemePicture>" +
    "<BorderStyle>None</BorderStyle>" +
    "<FlatMode>True</FlatMode>" +
    "<Font>Tahoma, 8pt</Font>" + 
    "<CaptionFont>Tahoma, 8pt, style=1</CaptionFont>" +
    "<HeaderFont>Tahoma, 8pt, style=1</HeaderFont>" + 
    "<AlternatingBackColor>GhostWhite</AlternatingBackColor>" + 
    "<BackColor>GhostWhite</BackColor>" +
    "<BackgroundColor>Lavender</BackgroundColor>" + 
    "<CaptionForeColor>White</CaptionForeColor>" +
    "<CaptionBackColor>RoyalBlue</CaptionBackColor>" +
    "<ForeColor>MidnightBlue</ForeColor>" +
    "<GridLineColor>RoyalBlue</GridLineColor>" + 
    "<HeaderBackColor>MidnightBlue</HeaderBackColor>" +
    "<HeaderForeColor>Lavender</HeaderForeColor>" + 
    "<LinkColor>Teal</LinkColor>" + 
    "<LinkHoverColor>DodgerBlue</LinkHoverColor>" +
    "<ParentRowsBackColor>Lavender</ParentRowsBackColor>" + 
    "<ParentRowsForeColor>MidnightBlue</ParentRowsForeColor>" +
    "<SelectionForeColor>PaleGreen</SelectionForeColor>" +
    "<SelectionBackColor>Teal</SelectionBackColor>" +
  "</Scheme>" + 
  "<Scheme>" +
    "<SchemeName>Colorful 3</SchemeName>" + 
    "<SchemePicture>colorful3.bmp</SchemePicture>" + 
    "<BorderStyle>None</BorderStyle>" +
    "<FlatMode>True</FlatMode>" + 
    "<Font>Tahoma, 8pt</Font>" +
    "<CaptionFont>Tahoma, 8pt, style=1</CaptionFont>" +
    "<HeaderFont>Tahoma, 8pt, style=1</HeaderFont>" +
    "<AlternatingBackColor>OldLace</AlternatingBackColor>" + 
    "<BackColor>OldLace</BackColor>" +
    "<BackgroundColor>Tan</BackgroundColor>" + 
    "<CaptionForeColor>OldLace</CaptionForeColor>" + 
    "<CaptionBackColor>SaddleBrown</CaptionBackColor>" +
    "<ForeColor>DarkSlateGray</ForeColor>" + 
    "<GridLineColor>Tan</GridLineColor>" +
    "<GridLineStyle>Solid</GridLineStyle>" +
    "<HeaderBackColor>Wheat</HeaderBackColor>" +
    "<HeaderForeColor>SaddleBrown</HeaderForeColor>" + 
    "<LinkColor>DarkSlateBlue</LinkColor>" +
    "<LinkHoverColor>Teal</LinkHoverColor>" + 
    "<ParentRowsBackColor>OldLace</ParentRowsBackColor>" + 
    "<ParentRowsForeColor>DarkSlateGray</ParentRowsForeColor>" +
    "<SelectionForeColor>White</SelectionForeColor>" + 
    "<SelectionBackColor>SlateGray</SelectionBackColor>" +
  "</Scheme>" +
  "<Scheme>" +
    "<SchemeName>Colorful 4</SchemeName>" + 
    "<SchemePicture>colorful4.bmp</SchemePicture>" +
    "<BorderStyle>FixedSingle</BorderStyle>" + 
    "<FlatMode>True</FlatMode>" + 
    "<Font>Tahoma, 8pt</Font>" +
    "<CaptionFont>Tahoma, 8pt, style=1</CaptionFont>" + 
    "<HeaderFont>Tahoma, 8pt, style=1</HeaderFont>" +
    "<AlternatingBackColor>White</AlternatingBackColor>" +
    "<BackColor>White</BackColor>" +
    "<BackgroundColor>Ivory</BackgroundColor>" + 
    "<CaptionForeColor>Lavender</CaptionForeColor>" +
    "<CaptionBackColor>DarkSlateBlue</CaptionBackColor>" + 
    "<ForeColor>Black</ForeColor>" + 
    "<GridLineColor>Wheat</GridLineColor>" +
    "<HeaderBackColor>CadetBlue</HeaderBackColor>" + 
    "<HeaderForeColor>Black</HeaderForeColor>" +
    "<LinkColor>DarkSlateBlue</LinkColor>" +
    "<LinkHoverColor>LightSeaGreen</LinkHoverColor>" +
    "<ParentRowsBackColor>Ivory</ParentRowsBackColor>" + 
    "<ParentRowsForeColor>Black</ParentRowsForeColor>" +
    "<SelectionForeColor>DarkSlateBlue</SelectionForeColor>" + 
    "<SelectionBackColor>Wheat</SelectionBackColor>" + 
  "</Scheme>" +
  "<Scheme>" + 
    "<SchemeName>256 Color 1</SchemeName>" +
    "<SchemePicture>256_1.bmp</SchemePicture>" +
    "<Font>Tahoma, 8pt</Font>" +
    "<CaptionFont>Tahoma, 8 pt</CaptionFont>" + 
    "<HeaderFont>Tahoma, 8pt</HeaderFont>" +
    "<AlternatingBackColor>Silver</AlternatingBackColor>" + 
    "<BackColor>White</BackColor>" + 
    "<CaptionForeColor>White</CaptionForeColor>" +
    "<CaptionBackColor>Maroon</CaptionBackColor>" + 
    "<ForeColor>Black</ForeColor>" +
    "<GridLineColor>Silver</GridLineColor>" +
    "<HeaderBackColor>Silver</HeaderBackColor>" +
    "<HeaderForeColor>Black</HeaderForeColor>" + 
    "<LinkColor>Maroon</LinkColor>" +
    "<LinkHoverColor>Red</LinkHoverColor>" + 
    "<ParentRowsBackColor>Silver</ParentRowsBackColor>" + 
    "<ParentRowsForeColor>Black</ParentRowsForeColor>" +
    "<SelectionForeColor>White</SelectionForeColor>" + 
    "<SelectionBackColor>Maroon</SelectionBackColor>" +
  "</Scheme>" +
  "<Scheme>" +
    "<SchemeName>256 Color 2</SchemeName>" + 
    "<SchemePicture>256_2.bmp</SchemePicture>" +
    "<BorderStyle>FixedSingle</BorderStyle>" + 
    "<FlatMode>True</FlatMode>" + 
    "<CaptionFont>Microsoft Sans Serif, 10 pt, style=1</CaptionFont>" +
    "<Font>Tahoma, 8pt</Font>" + 
    "<HeaderFont>Tahoma, 8pt</HeaderFont>" +
    "<AlternatingBackColor>White</AlternatingBackColor>" +
    "<BackColor>White</BackColor>" +
    "<CaptionForeColor>White</CaptionForeColor>" + 
    "<CaptionBackColor>Teal</CaptionBackColor>" +
    "<ForeColor>Black</ForeColor>" + 
    "<GridLineColor>Silver</GridLineColor>" + 
    "<HeaderBackColor>Black</HeaderBackColor>" +
    "<HeaderForeColor>White</HeaderForeColor>" + 
    "<LinkColor>Purple</LinkColor>" +
    "<LinkHoverColor>Fuchsia</LinkHoverColor>" +
    "<ParentRowsBackColor>Gray</ParentRowsBackColor>" +
    "<ParentRowsForeColor>White</ParentRowsForeColor>" + 
    "<SelectionForeColor>White</SelectionForeColor>" +
    "<SelectionBackColor>Maroon</SelectionBackColor>" + 
  "</Scheme>" + 
"</pulica>";
 
        private class AutoFormatDataGrid : DataGrid {
            protected override void OnKeyDown(KeyEventArgs e) {
            }
            protected override bool ProcessDialogKey(Keys keyData) { 
                return false;
            } 
            protected override bool ProcessKeyPreview(ref Message m) { 
                return false;
            } 
            protected override void OnMouseDown(MouseEventArgs e) { }
            protected override void OnMouseUp(MouseEventArgs e) { }
            protected override void OnMouseMove(MouseEventArgs e) { }
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
