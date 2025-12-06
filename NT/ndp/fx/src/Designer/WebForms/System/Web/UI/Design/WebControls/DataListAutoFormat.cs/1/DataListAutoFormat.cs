//------------------------------------------------------------------------------ 
// <copyright file="DataListAutoFormats.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.ComponentModel.Design;
    using System.Data; 
    using System.Design;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization; 
    using System.Text;
    using System.Web.UI.WebControls; 
 
    internal sealed class DataListAutoFormat : DesignerAutoFormat {
 
        private string headerForeColor;
        private string headerBackColor;
        private int headerFont;
        private string footerForeColor; 
        private string footerBackColor;
        private int footerFont; 
        private string borderColor; 
        private string borderWidth;
        private int borderStyle = -1; 
        private int gridLines = -1;
        private int cellSpacing;
        private int cellPadding = -1;
        private string foreColor; 
        private string backColor;
        private string itemForeColor; 
        private string itemBackColor; 
        private int itemFont;
        private string alternatingItemForeColor; 
        private string alternatingItemBackColor;
        private int alternatingItemFont;
        private string selectedItemForeColor;
        private string selectedItemBackColor; 
        private int selectedItemFont;
 
        const int FONT_BOLD = 1; 
        const int FONT_ITALIC = 2;
 

        public DataListAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) {
            Load(schemeData);
        } 

        public override void Apply(Control control) { 
            Debug.Assert(control is DataList, "DataListAutoFormat:ApplyScheme- control is not DataList"); 
            if (control is DataList) {
                Apply(control as DataList); 
            }
        }

        private void Apply(DataList list) { 
            list.HeaderStyle.ForeColor = ColorTranslator.FromHtml(headerForeColor);
            list.HeaderStyle.BackColor = ColorTranslator.FromHtml(headerBackColor); 
            list.HeaderStyle.Font.Bold = ((headerFont & FONT_BOLD) != 0); 
            list.HeaderStyle.Font.Italic = ((headerFont & FONT_ITALIC) != 0);
            list.HeaderStyle.Font.ClearDefaults(); 
            list.FooterStyle.ForeColor = ColorTranslator.FromHtml(footerForeColor);
            list.FooterStyle.BackColor = ColorTranslator.FromHtml(footerBackColor);
            list.FooterStyle.Font.Bold = ((footerFont & FONT_BOLD) != 0);
            list.FooterStyle.Font.Italic = ((footerFont & FONT_ITALIC) != 0); 
            list.FooterStyle.Font.ClearDefaults();
            list.BorderWidth = new Unit(borderWidth, CultureInfo.InvariantCulture); 
            switch (gridLines) { 
                case 0: list.GridLines = GridLines.None; break;
                case 1: list.GridLines = GridLines.Horizontal; break; 
                case 2: list.GridLines = GridLines.Vertical; break;
                case 3: list.GridLines = GridLines.Both; break;
                default:
                    list.GridLines = GridLines.None; break; 
            }
            if ((borderStyle >= 0) && (borderStyle <= 9)) { 
                list.BorderStyle = (System.Web.UI.WebControls.BorderStyle)borderStyle; 
            }
            else { 
                list.BorderStyle = System.Web.UI.WebControls.BorderStyle.NotSet;
            }
            list.BorderColor = ColorTranslator.FromHtml(borderColor);
            list.CellPadding = cellPadding; 
            list.CellSpacing = cellSpacing;
            list.ForeColor = ColorTranslator.FromHtml(foreColor); 
            list.BackColor = ColorTranslator.FromHtml(backColor); 
            list.ItemStyle.ForeColor = ColorTranslator.FromHtml(itemForeColor);
            list.ItemStyle.BackColor = ColorTranslator.FromHtml(itemBackColor); 
            list.ItemStyle.Font.Bold = ((itemFont & FONT_BOLD) != 0);
            list.ItemStyle.Font.Italic = ((itemFont & FONT_ITALIC) != 0);
            list.ItemStyle.Font.ClearDefaults();
            list.AlternatingItemStyle.ForeColor = ColorTranslator.FromHtml(alternatingItemForeColor); 
            list.AlternatingItemStyle.BackColor = ColorTranslator.FromHtml(alternatingItemBackColor);
            list.AlternatingItemStyle.Font.Bold = ((alternatingItemFont & FONT_BOLD) != 0); 
            list.AlternatingItemStyle.Font.Italic = ((alternatingItemFont & FONT_ITALIC) != 0); 
            list.AlternatingItemStyle.Font.ClearDefaults();
            list.SelectedItemStyle.ForeColor = ColorTranslator.FromHtml(selectedItemForeColor); 
            list.SelectedItemStyle.BackColor = ColorTranslator.FromHtml(selectedItemBackColor);
            list.SelectedItemStyle.Font.Bold = ((selectedItemFont & FONT_BOLD) != 0);
            list.SelectedItemStyle.Font.Italic = ((selectedItemFont & FONT_ITALIC) != 0);
            list.SelectedItemStyle.Font.ClearDefaults(); 
        }
 
        private int GetIntProperty(string propertyTag, DataRow schemeData) { 
            object data = schemeData[propertyTag];
            if ((data != null) && !data.Equals(DBNull.Value)) 
                return Int32.Parse(data.ToString(), CultureInfo.InvariantCulture);
            else
                return 0;
        } 

        private int GetIntProperty(string propertyTag, int defaultValue, DataRow schemeData) { 
            object data = schemeData[propertyTag]; 
            if ((data != null) && !data.Equals(DBNull.Value))
                return Int32.Parse(data.ToString(), CultureInfo.InvariantCulture); 
            else
                return defaultValue;
        }
 
        public override Control GetPreviewControl(Control runtimeControl) {
            Control control = base.GetPreviewControl(runtimeControl); 
            if (control != null) { 
                IDesignerHost host = (IDesignerHost)runtimeControl.Site.GetService(typeof(IDesignerHost));
                DataList dataList = control as DataList; 

                if (dataList != null && host != null) {
                    TemplateBuilder itemTemplate = dataList.ItemTemplate as TemplateBuilder;
                    if ((itemTemplate != null && itemTemplate.Text.Length == 0) || dataList.ItemTemplate == null) { 
                        string text = "####";
                        dataList.ItemTemplate = ControlParser.ParseTemplate(host, text); 
                        dataList.ItemStyle.HorizontalAlign = HorizontalAlign.Center; 
                    }
                    dataList.HorizontalAlign = HorizontalAlign.Center; 
                    dataList.Width = new Unit(80, UnitType.Percentage);
                }
            }
            return control; 
        }
 
        private string GetStringProperty(string propertyTag, DataRow schemeData) { 
            object data = schemeData[propertyTag];
            if ((data != null) && !data.Equals(DBNull.Value)) 
                return data.ToString();
            else
                return String.Empty;
        } 

        private void Load(DataRow schemeData) { 
            Debug.Assert(schemeData != null); 

            foreColor = GetStringProperty("ForeColor", schemeData); 
            backColor = GetStringProperty("BackColor", schemeData);
            borderColor = GetStringProperty("BorderColor", schemeData);
            borderWidth = GetStringProperty("BorderWidth", schemeData);
            borderStyle = GetIntProperty("BorderStyle", -1, schemeData); 
            cellSpacing = GetIntProperty("CellSpacing", schemeData);
            cellPadding = GetIntProperty("CellPadding", -1, schemeData); 
            gridLines = GetIntProperty("GridLines", -1, schemeData); 
            itemForeColor = GetStringProperty("ItemForeColor", schemeData);
            itemBackColor = GetStringProperty("ItemBackColor", schemeData); 
            itemFont = GetIntProperty("ItemFont", schemeData);
            alternatingItemForeColor = GetStringProperty("AltItemForeColor", schemeData);
            alternatingItemBackColor = GetStringProperty("AltItemBackColor", schemeData);
            alternatingItemFont = GetIntProperty("AltItemFont", schemeData); 
            selectedItemForeColor = GetStringProperty("SelItemForeColor", schemeData);
            selectedItemBackColor = GetStringProperty("SelItemBackColor", schemeData); 
            selectedItemFont = GetIntProperty("SelItemFont", schemeData); 
            headerForeColor = GetStringProperty("HeaderForeColor", schemeData);
            headerBackColor = GetStringProperty("HeaderBackColor", schemeData); 
            headerFont = GetIntProperty("HeaderFont", schemeData);
            footerForeColor = GetStringProperty("FooterForeColor", schemeData);
            footerBackColor = GetStringProperty("FooterBackColor", schemeData);
            footerFont = GetIntProperty("FooterFont", schemeData); 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataListAutoFormats.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.ComponentModel.Design;
    using System.Data; 
    using System.Design;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization; 
    using System.Text;
    using System.Web.UI.WebControls; 
 
    internal sealed class DataListAutoFormat : DesignerAutoFormat {
 
        private string headerForeColor;
        private string headerBackColor;
        private int headerFont;
        private string footerForeColor; 
        private string footerBackColor;
        private int footerFont; 
        private string borderColor; 
        private string borderWidth;
        private int borderStyle = -1; 
        private int gridLines = -1;
        private int cellSpacing;
        private int cellPadding = -1;
        private string foreColor; 
        private string backColor;
        private string itemForeColor; 
        private string itemBackColor; 
        private int itemFont;
        private string alternatingItemForeColor; 
        private string alternatingItemBackColor;
        private int alternatingItemFont;
        private string selectedItemForeColor;
        private string selectedItemBackColor; 
        private int selectedItemFont;
 
        const int FONT_BOLD = 1; 
        const int FONT_ITALIC = 2;
 

        public DataListAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) {
            Load(schemeData);
        } 

        public override void Apply(Control control) { 
            Debug.Assert(control is DataList, "DataListAutoFormat:ApplyScheme- control is not DataList"); 
            if (control is DataList) {
                Apply(control as DataList); 
            }
        }

        private void Apply(DataList list) { 
            list.HeaderStyle.ForeColor = ColorTranslator.FromHtml(headerForeColor);
            list.HeaderStyle.BackColor = ColorTranslator.FromHtml(headerBackColor); 
            list.HeaderStyle.Font.Bold = ((headerFont & FONT_BOLD) != 0); 
            list.HeaderStyle.Font.Italic = ((headerFont & FONT_ITALIC) != 0);
            list.HeaderStyle.Font.ClearDefaults(); 
            list.FooterStyle.ForeColor = ColorTranslator.FromHtml(footerForeColor);
            list.FooterStyle.BackColor = ColorTranslator.FromHtml(footerBackColor);
            list.FooterStyle.Font.Bold = ((footerFont & FONT_BOLD) != 0);
            list.FooterStyle.Font.Italic = ((footerFont & FONT_ITALIC) != 0); 
            list.FooterStyle.Font.ClearDefaults();
            list.BorderWidth = new Unit(borderWidth, CultureInfo.InvariantCulture); 
            switch (gridLines) { 
                case 0: list.GridLines = GridLines.None; break;
                case 1: list.GridLines = GridLines.Horizontal; break; 
                case 2: list.GridLines = GridLines.Vertical; break;
                case 3: list.GridLines = GridLines.Both; break;
                default:
                    list.GridLines = GridLines.None; break; 
            }
            if ((borderStyle >= 0) && (borderStyle <= 9)) { 
                list.BorderStyle = (System.Web.UI.WebControls.BorderStyle)borderStyle; 
            }
            else { 
                list.BorderStyle = System.Web.UI.WebControls.BorderStyle.NotSet;
            }
            list.BorderColor = ColorTranslator.FromHtml(borderColor);
            list.CellPadding = cellPadding; 
            list.CellSpacing = cellSpacing;
            list.ForeColor = ColorTranslator.FromHtml(foreColor); 
            list.BackColor = ColorTranslator.FromHtml(backColor); 
            list.ItemStyle.ForeColor = ColorTranslator.FromHtml(itemForeColor);
            list.ItemStyle.BackColor = ColorTranslator.FromHtml(itemBackColor); 
            list.ItemStyle.Font.Bold = ((itemFont & FONT_BOLD) != 0);
            list.ItemStyle.Font.Italic = ((itemFont & FONT_ITALIC) != 0);
            list.ItemStyle.Font.ClearDefaults();
            list.AlternatingItemStyle.ForeColor = ColorTranslator.FromHtml(alternatingItemForeColor); 
            list.AlternatingItemStyle.BackColor = ColorTranslator.FromHtml(alternatingItemBackColor);
            list.AlternatingItemStyle.Font.Bold = ((alternatingItemFont & FONT_BOLD) != 0); 
            list.AlternatingItemStyle.Font.Italic = ((alternatingItemFont & FONT_ITALIC) != 0); 
            list.AlternatingItemStyle.Font.ClearDefaults();
            list.SelectedItemStyle.ForeColor = ColorTranslator.FromHtml(selectedItemForeColor); 
            list.SelectedItemStyle.BackColor = ColorTranslator.FromHtml(selectedItemBackColor);
            list.SelectedItemStyle.Font.Bold = ((selectedItemFont & FONT_BOLD) != 0);
            list.SelectedItemStyle.Font.Italic = ((selectedItemFont & FONT_ITALIC) != 0);
            list.SelectedItemStyle.Font.ClearDefaults(); 
        }
 
        private int GetIntProperty(string propertyTag, DataRow schemeData) { 
            object data = schemeData[propertyTag];
            if ((data != null) && !data.Equals(DBNull.Value)) 
                return Int32.Parse(data.ToString(), CultureInfo.InvariantCulture);
            else
                return 0;
        } 

        private int GetIntProperty(string propertyTag, int defaultValue, DataRow schemeData) { 
            object data = schemeData[propertyTag]; 
            if ((data != null) && !data.Equals(DBNull.Value))
                return Int32.Parse(data.ToString(), CultureInfo.InvariantCulture); 
            else
                return defaultValue;
        }
 
        public override Control GetPreviewControl(Control runtimeControl) {
            Control control = base.GetPreviewControl(runtimeControl); 
            if (control != null) { 
                IDesignerHost host = (IDesignerHost)runtimeControl.Site.GetService(typeof(IDesignerHost));
                DataList dataList = control as DataList; 

                if (dataList != null && host != null) {
                    TemplateBuilder itemTemplate = dataList.ItemTemplate as TemplateBuilder;
                    if ((itemTemplate != null && itemTemplate.Text.Length == 0) || dataList.ItemTemplate == null) { 
                        string text = "####";
                        dataList.ItemTemplate = ControlParser.ParseTemplate(host, text); 
                        dataList.ItemStyle.HorizontalAlign = HorizontalAlign.Center; 
                    }
                    dataList.HorizontalAlign = HorizontalAlign.Center; 
                    dataList.Width = new Unit(80, UnitType.Percentage);
                }
            }
            return control; 
        }
 
        private string GetStringProperty(string propertyTag, DataRow schemeData) { 
            object data = schemeData[propertyTag];
            if ((data != null) && !data.Equals(DBNull.Value)) 
                return data.ToString();
            else
                return String.Empty;
        } 

        private void Load(DataRow schemeData) { 
            Debug.Assert(schemeData != null); 

            foreColor = GetStringProperty("ForeColor", schemeData); 
            backColor = GetStringProperty("BackColor", schemeData);
            borderColor = GetStringProperty("BorderColor", schemeData);
            borderWidth = GetStringProperty("BorderWidth", schemeData);
            borderStyle = GetIntProperty("BorderStyle", -1, schemeData); 
            cellSpacing = GetIntProperty("CellSpacing", schemeData);
            cellPadding = GetIntProperty("CellPadding", -1, schemeData); 
            gridLines = GetIntProperty("GridLines", -1, schemeData); 
            itemForeColor = GetStringProperty("ItemForeColor", schemeData);
            itemBackColor = GetStringProperty("ItemBackColor", schemeData); 
            itemFont = GetIntProperty("ItemFont", schemeData);
            alternatingItemForeColor = GetStringProperty("AltItemForeColor", schemeData);
            alternatingItemBackColor = GetStringProperty("AltItemBackColor", schemeData);
            alternatingItemFont = GetIntProperty("AltItemFont", schemeData); 
            selectedItemForeColor = GetStringProperty("SelItemForeColor", schemeData);
            selectedItemBackColor = GetStringProperty("SelItemBackColor", schemeData); 
            selectedItemFont = GetIntProperty("SelItemFont", schemeData); 
            headerForeColor = GetStringProperty("HeaderForeColor", schemeData);
            headerBackColor = GetStringProperty("HeaderBackColor", schemeData); 
            headerFont = GetIntProperty("HeaderFont", schemeData);
            footerForeColor = GetStringProperty("FooterForeColor", schemeData);
            footerBackColor = GetStringProperty("FooterBackColor", schemeData);
            footerFont = GetIntProperty("FooterFont", schemeData); 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
