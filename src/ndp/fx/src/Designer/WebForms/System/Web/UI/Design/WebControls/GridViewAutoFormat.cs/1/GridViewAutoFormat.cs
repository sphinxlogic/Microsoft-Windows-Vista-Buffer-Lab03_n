//------------------------------------------------------------------------------ 
// <copyright file="GridViewAutoFormats.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Data;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.Text; 
    using System.Web.UI.WebControls;
 
    internal sealed class GridViewAutoFormat : DesignerAutoFormat { 

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
        private string pagerForeColor;
        private string pagerBackColor; 
        private int pagerFont; 
        private int pagerAlign;
        private int pagerButtons; 
        private string editItemForeColor;
        private string editItemBackColor;
        private int editItemFont;
 
        const int FONT_BOLD = 1;
        const int FONT_ITALIC = 2; 
 

        public GridViewAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) { 
            Load(schemeData);

            Style.Width = 260;
            Style.Height = 240; 
        }
 
        public override void Apply(Control control) { 
            Debug.Assert(control is GridView, "GridViewAutoFormat:ApplyScheme- control is not GridView");
            if (control is GridView) { 
                Apply(control as GridView);
            }
        }
 
        private void Apply(GridView grid) {
            grid.HeaderStyle.ForeColor = ColorTranslator.FromHtml(headerForeColor); 
            grid.HeaderStyle.BackColor = ColorTranslator.FromHtml(headerBackColor); 
            grid.HeaderStyle.Font.Bold = ((headerFont & FONT_BOLD) != 0);
            grid.HeaderStyle.Font.Italic = ((headerFont & FONT_ITALIC) != 0); 
            grid.HeaderStyle.Font.ClearDefaults();
            grid.FooterStyle.ForeColor = ColorTranslator.FromHtml(footerForeColor);
            grid.FooterStyle.BackColor = ColorTranslator.FromHtml(footerBackColor);
            grid.FooterStyle.Font.Bold = ((footerFont & FONT_BOLD) != 0); 
            grid.FooterStyle.Font.Italic = ((footerFont & FONT_ITALIC) != 0);
            grid.FooterStyle.Font.ClearDefaults(); 
            grid.BorderWidth = new Unit(borderWidth, CultureInfo.InvariantCulture); 
            switch (gridLines) {
                case 0: grid.GridLines = GridLines.None; break; 
                case 1: grid.GridLines = GridLines.Horizontal; break;
                case 2: grid.GridLines = GridLines.Vertical; break;
                case 3:
                default: 
                    grid.GridLines = GridLines.Both; break;
            } 
            if ((borderStyle >= 0) && (borderStyle <= 9)) { 
                grid.BorderStyle = (System.Web.UI.WebControls.BorderStyle)borderStyle;
            } 
            else {
                grid.BorderStyle = System.Web.UI.WebControls.BorderStyle.NotSet;
            }
            grid.BorderColor = ColorTranslator.FromHtml(borderColor); 
            grid.CellPadding = cellPadding;
            grid.CellSpacing = cellSpacing; 
            grid.ForeColor = ColorTranslator.FromHtml(foreColor); 
            grid.BackColor = ColorTranslator.FromHtml(backColor);
            grid.RowStyle.ForeColor = ColorTranslator.FromHtml(itemForeColor); 
            grid.RowStyle.BackColor = ColorTranslator.FromHtml(itemBackColor);
            grid.RowStyle.Font.Bold = ((itemFont & FONT_BOLD) != 0);
            grid.RowStyle.Font.Italic = ((itemFont & FONT_ITALIC) != 0);
            grid.RowStyle.Font.ClearDefaults(); 
            grid.AlternatingRowStyle.ForeColor = ColorTranslator.FromHtml(alternatingItemForeColor);
            grid.AlternatingRowStyle.BackColor = ColorTranslator.FromHtml(alternatingItemBackColor); 
            grid.AlternatingRowStyle.Font.Bold = ((alternatingItemFont & FONT_BOLD) != 0); 
            grid.AlternatingRowStyle.Font.Italic = ((alternatingItemFont & FONT_ITALIC) != 0);
            grid.AlternatingRowStyle.Font.ClearDefaults(); 
            grid.SelectedRowStyle.ForeColor = ColorTranslator.FromHtml(selectedItemForeColor);
            grid.SelectedRowStyle.BackColor = ColorTranslator.FromHtml(selectedItemBackColor);
            grid.SelectedRowStyle.Font.Bold = ((selectedItemFont & FONT_BOLD) != 0);
            grid.SelectedRowStyle.Font.Italic = ((selectedItemFont & FONT_ITALIC) != 0); 
            grid.SelectedRowStyle.Font.ClearDefaults();
            grid.PagerStyle.ForeColor = ColorTranslator.FromHtml(pagerForeColor); 
            grid.PagerStyle.BackColor = ColorTranslator.FromHtml(pagerBackColor); 
            grid.PagerStyle.Font.Bold = ((pagerFont & FONT_BOLD) != 0);
            grid.PagerStyle.Font.Italic = ((pagerFont & FONT_ITALIC) != 0); 
            grid.PagerStyle.HorizontalAlign = (HorizontalAlign)pagerAlign;
            grid.PagerStyle.Font.ClearDefaults();
            grid.PagerSettings.Mode = (PagerButtons)pagerButtons;
            grid.EditRowStyle.ForeColor = ColorTranslator.FromHtml(editItemForeColor); 
            grid.EditRowStyle.BackColor = ColorTranslator.FromHtml(editItemBackColor);
            grid.EditRowStyle.Font.Bold = ((editItemFont & FONT_BOLD) != 0); 
            grid.EditRowStyle.Font.Italic = ((editItemFont & FONT_ITALIC) != 0); 
            grid.EditRowStyle.Font.ClearDefaults();
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
            pagerForeColor = GetStringProperty("PagerForeColor", schemeData); 
            pagerBackColor = GetStringProperty("PagerBackColor", schemeData);
            pagerFont = GetIntProperty("PagerFont", schemeData); 
            pagerAlign = GetIntProperty("PagerAlign", schemeData);
            pagerButtons = GetIntProperty("PagerButtons", 1, schemeData);
            editItemForeColor = GetStringProperty("EditItemForeColor", schemeData);
            editItemBackColor = GetStringProperty("EditItemBackColor", schemeData); 
            editItemFont = GetIntProperty("EditItemFont", schemeData);
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="GridViewAutoFormats.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Data;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.Text; 
    using System.Web.UI.WebControls;
 
    internal sealed class GridViewAutoFormat : DesignerAutoFormat { 

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
        private string pagerForeColor;
        private string pagerBackColor; 
        private int pagerFont; 
        private int pagerAlign;
        private int pagerButtons; 
        private string editItemForeColor;
        private string editItemBackColor;
        private int editItemFont;
 
        const int FONT_BOLD = 1;
        const int FONT_ITALIC = 2; 
 

        public GridViewAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) { 
            Load(schemeData);

            Style.Width = 260;
            Style.Height = 240; 
        }
 
        public override void Apply(Control control) { 
            Debug.Assert(control is GridView, "GridViewAutoFormat:ApplyScheme- control is not GridView");
            if (control is GridView) { 
                Apply(control as GridView);
            }
        }
 
        private void Apply(GridView grid) {
            grid.HeaderStyle.ForeColor = ColorTranslator.FromHtml(headerForeColor); 
            grid.HeaderStyle.BackColor = ColorTranslator.FromHtml(headerBackColor); 
            grid.HeaderStyle.Font.Bold = ((headerFont & FONT_BOLD) != 0);
            grid.HeaderStyle.Font.Italic = ((headerFont & FONT_ITALIC) != 0); 
            grid.HeaderStyle.Font.ClearDefaults();
            grid.FooterStyle.ForeColor = ColorTranslator.FromHtml(footerForeColor);
            grid.FooterStyle.BackColor = ColorTranslator.FromHtml(footerBackColor);
            grid.FooterStyle.Font.Bold = ((footerFont & FONT_BOLD) != 0); 
            grid.FooterStyle.Font.Italic = ((footerFont & FONT_ITALIC) != 0);
            grid.FooterStyle.Font.ClearDefaults(); 
            grid.BorderWidth = new Unit(borderWidth, CultureInfo.InvariantCulture); 
            switch (gridLines) {
                case 0: grid.GridLines = GridLines.None; break; 
                case 1: grid.GridLines = GridLines.Horizontal; break;
                case 2: grid.GridLines = GridLines.Vertical; break;
                case 3:
                default: 
                    grid.GridLines = GridLines.Both; break;
            } 
            if ((borderStyle >= 0) && (borderStyle <= 9)) { 
                grid.BorderStyle = (System.Web.UI.WebControls.BorderStyle)borderStyle;
            } 
            else {
                grid.BorderStyle = System.Web.UI.WebControls.BorderStyle.NotSet;
            }
            grid.BorderColor = ColorTranslator.FromHtml(borderColor); 
            grid.CellPadding = cellPadding;
            grid.CellSpacing = cellSpacing; 
            grid.ForeColor = ColorTranslator.FromHtml(foreColor); 
            grid.BackColor = ColorTranslator.FromHtml(backColor);
            grid.RowStyle.ForeColor = ColorTranslator.FromHtml(itemForeColor); 
            grid.RowStyle.BackColor = ColorTranslator.FromHtml(itemBackColor);
            grid.RowStyle.Font.Bold = ((itemFont & FONT_BOLD) != 0);
            grid.RowStyle.Font.Italic = ((itemFont & FONT_ITALIC) != 0);
            grid.RowStyle.Font.ClearDefaults(); 
            grid.AlternatingRowStyle.ForeColor = ColorTranslator.FromHtml(alternatingItemForeColor);
            grid.AlternatingRowStyle.BackColor = ColorTranslator.FromHtml(alternatingItemBackColor); 
            grid.AlternatingRowStyle.Font.Bold = ((alternatingItemFont & FONT_BOLD) != 0); 
            grid.AlternatingRowStyle.Font.Italic = ((alternatingItemFont & FONT_ITALIC) != 0);
            grid.AlternatingRowStyle.Font.ClearDefaults(); 
            grid.SelectedRowStyle.ForeColor = ColorTranslator.FromHtml(selectedItemForeColor);
            grid.SelectedRowStyle.BackColor = ColorTranslator.FromHtml(selectedItemBackColor);
            grid.SelectedRowStyle.Font.Bold = ((selectedItemFont & FONT_BOLD) != 0);
            grid.SelectedRowStyle.Font.Italic = ((selectedItemFont & FONT_ITALIC) != 0); 
            grid.SelectedRowStyle.Font.ClearDefaults();
            grid.PagerStyle.ForeColor = ColorTranslator.FromHtml(pagerForeColor); 
            grid.PagerStyle.BackColor = ColorTranslator.FromHtml(pagerBackColor); 
            grid.PagerStyle.Font.Bold = ((pagerFont & FONT_BOLD) != 0);
            grid.PagerStyle.Font.Italic = ((pagerFont & FONT_ITALIC) != 0); 
            grid.PagerStyle.HorizontalAlign = (HorizontalAlign)pagerAlign;
            grid.PagerStyle.Font.ClearDefaults();
            grid.PagerSettings.Mode = (PagerButtons)pagerButtons;
            grid.EditRowStyle.ForeColor = ColorTranslator.FromHtml(editItemForeColor); 
            grid.EditRowStyle.BackColor = ColorTranslator.FromHtml(editItemBackColor);
            grid.EditRowStyle.Font.Bold = ((editItemFont & FONT_BOLD) != 0); 
            grid.EditRowStyle.Font.Italic = ((editItemFont & FONT_ITALIC) != 0); 
            grid.EditRowStyle.Font.ClearDefaults();
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
            pagerForeColor = GetStringProperty("PagerForeColor", schemeData); 
            pagerBackColor = GetStringProperty("PagerBackColor", schemeData);
            pagerFont = GetIntProperty("PagerFont", schemeData); 
            pagerAlign = GetIntProperty("PagerAlign", schemeData);
            pagerButtons = GetIntProperty("PagerButtons", 1, schemeData);
            editItemForeColor = GetStringProperty("EditItemForeColor", schemeData);
            editItemBackColor = GetStringProperty("EditItemBackColor", schemeData); 
            editItemFont = GetIntProperty("EditItemFont", schemeData);
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
