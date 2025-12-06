//------------------------------------------------------------------------------ 
// <copyright file="DetailsViewAutoFormats.cs" company="Microsoft">
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
 
    internal sealed class DetailsViewAutoFormat : DesignerAutoFormat { 

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
        private string rowForeColor;
        private string rowBackColor; 
        private int itemFont; 
        private string alternatingRowForeColor;
        private string alternatingRowBackColor; 
        private int alternatingRowFont;
        private string commandRowForeColor;
        private string commandRowBackColor;
        private int commandRowFont; 
        private string fieldHeaderForeColor;
        private string fieldHeaderBackColor; 
        private int fieldHeaderFont; 
        private string editRowForeColor;
        private string editRowBackColor; 
        private int editRowFont;
        private string pagerForeColor;
        private string pagerBackColor;
        private int pagerFont; 
        private int pagerAlign;
        private int pagerButtons; 
 
        const int FONT_BOLD = 1;
        const int FONT_ITALIC = 2; 


        public DetailsViewAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) {
            Load(schemeData); 
        }
 
        public override void Apply(Control control) { 
            Debug.Assert(control is DetailsView, "DetailsViewAutoFormat:ApplyScheme- control is not DetailsView");
            if (control is DetailsView) { 
                Apply(control as DetailsView);
            }
        }
 
        private void Apply(DetailsView view) {
            view.HeaderStyle.ForeColor = ColorTranslator.FromHtml(headerForeColor); 
            view.HeaderStyle.BackColor = ColorTranslator.FromHtml(headerBackColor); 
            view.HeaderStyle.Font.Bold = ((headerFont & FONT_BOLD) != 0);
            view.HeaderStyle.Font.Italic = ((headerFont & FONT_ITALIC) != 0); 
            view.HeaderStyle.Font.ClearDefaults();
            view.FooterStyle.ForeColor = ColorTranslator.FromHtml(footerForeColor);
            view.FooterStyle.BackColor = ColorTranslator.FromHtml(footerBackColor);
            view.FooterStyle.Font.Bold = ((footerFont & FONT_BOLD) != 0); 
            view.FooterStyle.Font.Italic = ((footerFont & FONT_ITALIC) != 0);
            view.FooterStyle.Font.ClearDefaults(); 
            view.BorderWidth = new Unit(borderWidth, CultureInfo.InvariantCulture); 
            switch (gridLines) {
                case 0: view.GridLines = GridLines.None; break; 
                case 1: view.GridLines = GridLines.Horizontal; break;
                case 2: view.GridLines = GridLines.Vertical; break;
                case 3: view.GridLines = GridLines.Both; break;
                default: 
                    view.GridLines = GridLines.Both; break;
            } 
            if ((borderStyle >= 0) && (borderStyle <= 9)) { 
                view.BorderStyle = (System.Web.UI.WebControls.BorderStyle)borderStyle;
            } 
            else {
                view.BorderStyle = System.Web.UI.WebControls.BorderStyle.NotSet;
            }
            view.BorderColor = ColorTranslator.FromHtml(borderColor); 
            view.CellPadding = cellPadding;
            view.CellSpacing = cellSpacing; 
            view.ForeColor = ColorTranslator.FromHtml(foreColor); 
            view.BackColor = ColorTranslator.FromHtml(backColor);
            view.RowStyle.ForeColor = ColorTranslator.FromHtml(rowForeColor); 
            view.RowStyle.BackColor = ColorTranslator.FromHtml(rowBackColor);
            view.RowStyle.Font.Bold = ((itemFont & FONT_BOLD) != 0);
            view.RowStyle.Font.Italic = ((itemFont & FONT_ITALIC) != 0);
            view.RowStyle.Font.ClearDefaults(); 
            view.AlternatingRowStyle.ForeColor = ColorTranslator.FromHtml(alternatingRowForeColor);
            view.AlternatingRowStyle.BackColor = ColorTranslator.FromHtml(alternatingRowBackColor); 
            view.AlternatingRowStyle.Font.Bold = ((alternatingRowFont & FONT_BOLD) != 0); 
            view.AlternatingRowStyle.Font.Italic = ((alternatingRowFont & FONT_ITALIC) != 0);
            view.AlternatingRowStyle.Font.ClearDefaults(); 
            view.CommandRowStyle.ForeColor = ColorTranslator.FromHtml(commandRowForeColor);
            view.CommandRowStyle.BackColor = ColorTranslator.FromHtml(commandRowBackColor);
            view.CommandRowStyle.Font.Bold = ((commandRowFont & FONT_BOLD) != 0);
            view.CommandRowStyle.Font.Italic = ((commandRowFont & FONT_ITALIC) != 0); 
            view.CommandRowStyle.Font.ClearDefaults();
            view.FieldHeaderStyle.ForeColor = ColorTranslator.FromHtml(fieldHeaderForeColor); 
            view.FieldHeaderStyle.BackColor = ColorTranslator.FromHtml(fieldHeaderBackColor); 
            view.FieldHeaderStyle.Font.Bold = ((fieldHeaderFont & FONT_BOLD) != 0);
            view.FieldHeaderStyle.Font.Italic = ((fieldHeaderFont & FONT_ITALIC) != 0); 
            view.FieldHeaderStyle.Font.ClearDefaults();
            view.EditRowStyle.ForeColor = ColorTranslator.FromHtml(editRowForeColor);
            view.EditRowStyle.BackColor = ColorTranslator.FromHtml(editRowBackColor);
            view.EditRowStyle.Font.Bold = ((editRowFont & FONT_BOLD) != 0); 
            view.EditRowStyle.Font.Italic = ((editRowFont & FONT_ITALIC) != 0);
            view.EditRowStyle.Font.ClearDefaults(); 
            view.PagerStyle.ForeColor = ColorTranslator.FromHtml(pagerForeColor); 
            view.PagerStyle.BackColor = ColorTranslator.FromHtml(pagerBackColor);
            view.PagerStyle.Font.Bold = ((pagerFont & FONT_BOLD) != 0); 
            view.PagerStyle.Font.Italic = ((pagerFont & FONT_ITALIC) != 0);
            view.PagerStyle.HorizontalAlign = (HorizontalAlign)pagerAlign;
            view.PagerStyle.Font.ClearDefaults();
            view.PagerSettings.Mode = (PagerButtons)pagerButtons; 
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
            rowForeColor = GetStringProperty("RowForeColor", schemeData); 
            rowBackColor = GetStringProperty("RowBackColor", schemeData); 
            itemFont = GetIntProperty("RowFont", schemeData);
            alternatingRowForeColor = GetStringProperty("AltRowForeColor", schemeData); 
            alternatingRowBackColor = GetStringProperty("AltRowBackColor", schemeData);
            alternatingRowFont = GetIntProperty("AltRowFont", schemeData);
            commandRowForeColor = GetStringProperty("CommandRowForeColor", schemeData);
            commandRowBackColor = GetStringProperty("CommandRowBackColor", schemeData); 
            commandRowFont = GetIntProperty("CommandRowFont", schemeData);
            fieldHeaderForeColor = GetStringProperty("FieldHeaderForeColor", schemeData); 
            fieldHeaderBackColor = GetStringProperty("FieldHeaderBackColor", schemeData); 
            fieldHeaderFont = GetIntProperty("FieldHeaderFont", schemeData);
            editRowForeColor = GetStringProperty("EditRowForeColor", schemeData); 
            editRowBackColor = GetStringProperty("EditRowBackColor", schemeData);
            editRowFont = GetIntProperty("EditRowFont", schemeData);
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
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DetailsViewAutoFormats.cs" company="Microsoft">
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
 
    internal sealed class DetailsViewAutoFormat : DesignerAutoFormat { 

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
        private string rowForeColor;
        private string rowBackColor; 
        private int itemFont; 
        private string alternatingRowForeColor;
        private string alternatingRowBackColor; 
        private int alternatingRowFont;
        private string commandRowForeColor;
        private string commandRowBackColor;
        private int commandRowFont; 
        private string fieldHeaderForeColor;
        private string fieldHeaderBackColor; 
        private int fieldHeaderFont; 
        private string editRowForeColor;
        private string editRowBackColor; 
        private int editRowFont;
        private string pagerForeColor;
        private string pagerBackColor;
        private int pagerFont; 
        private int pagerAlign;
        private int pagerButtons; 
 
        const int FONT_BOLD = 1;
        const int FONT_ITALIC = 2; 


        public DetailsViewAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) {
            Load(schemeData); 
        }
 
        public override void Apply(Control control) { 
            Debug.Assert(control is DetailsView, "DetailsViewAutoFormat:ApplyScheme- control is not DetailsView");
            if (control is DetailsView) { 
                Apply(control as DetailsView);
            }
        }
 
        private void Apply(DetailsView view) {
            view.HeaderStyle.ForeColor = ColorTranslator.FromHtml(headerForeColor); 
            view.HeaderStyle.BackColor = ColorTranslator.FromHtml(headerBackColor); 
            view.HeaderStyle.Font.Bold = ((headerFont & FONT_BOLD) != 0);
            view.HeaderStyle.Font.Italic = ((headerFont & FONT_ITALIC) != 0); 
            view.HeaderStyle.Font.ClearDefaults();
            view.FooterStyle.ForeColor = ColorTranslator.FromHtml(footerForeColor);
            view.FooterStyle.BackColor = ColorTranslator.FromHtml(footerBackColor);
            view.FooterStyle.Font.Bold = ((footerFont & FONT_BOLD) != 0); 
            view.FooterStyle.Font.Italic = ((footerFont & FONT_ITALIC) != 0);
            view.FooterStyle.Font.ClearDefaults(); 
            view.BorderWidth = new Unit(borderWidth, CultureInfo.InvariantCulture); 
            switch (gridLines) {
                case 0: view.GridLines = GridLines.None; break; 
                case 1: view.GridLines = GridLines.Horizontal; break;
                case 2: view.GridLines = GridLines.Vertical; break;
                case 3: view.GridLines = GridLines.Both; break;
                default: 
                    view.GridLines = GridLines.Both; break;
            } 
            if ((borderStyle >= 0) && (borderStyle <= 9)) { 
                view.BorderStyle = (System.Web.UI.WebControls.BorderStyle)borderStyle;
            } 
            else {
                view.BorderStyle = System.Web.UI.WebControls.BorderStyle.NotSet;
            }
            view.BorderColor = ColorTranslator.FromHtml(borderColor); 
            view.CellPadding = cellPadding;
            view.CellSpacing = cellSpacing; 
            view.ForeColor = ColorTranslator.FromHtml(foreColor); 
            view.BackColor = ColorTranslator.FromHtml(backColor);
            view.RowStyle.ForeColor = ColorTranslator.FromHtml(rowForeColor); 
            view.RowStyle.BackColor = ColorTranslator.FromHtml(rowBackColor);
            view.RowStyle.Font.Bold = ((itemFont & FONT_BOLD) != 0);
            view.RowStyle.Font.Italic = ((itemFont & FONT_ITALIC) != 0);
            view.RowStyle.Font.ClearDefaults(); 
            view.AlternatingRowStyle.ForeColor = ColorTranslator.FromHtml(alternatingRowForeColor);
            view.AlternatingRowStyle.BackColor = ColorTranslator.FromHtml(alternatingRowBackColor); 
            view.AlternatingRowStyle.Font.Bold = ((alternatingRowFont & FONT_BOLD) != 0); 
            view.AlternatingRowStyle.Font.Italic = ((alternatingRowFont & FONT_ITALIC) != 0);
            view.AlternatingRowStyle.Font.ClearDefaults(); 
            view.CommandRowStyle.ForeColor = ColorTranslator.FromHtml(commandRowForeColor);
            view.CommandRowStyle.BackColor = ColorTranslator.FromHtml(commandRowBackColor);
            view.CommandRowStyle.Font.Bold = ((commandRowFont & FONT_BOLD) != 0);
            view.CommandRowStyle.Font.Italic = ((commandRowFont & FONT_ITALIC) != 0); 
            view.CommandRowStyle.Font.ClearDefaults();
            view.FieldHeaderStyle.ForeColor = ColorTranslator.FromHtml(fieldHeaderForeColor); 
            view.FieldHeaderStyle.BackColor = ColorTranslator.FromHtml(fieldHeaderBackColor); 
            view.FieldHeaderStyle.Font.Bold = ((fieldHeaderFont & FONT_BOLD) != 0);
            view.FieldHeaderStyle.Font.Italic = ((fieldHeaderFont & FONT_ITALIC) != 0); 
            view.FieldHeaderStyle.Font.ClearDefaults();
            view.EditRowStyle.ForeColor = ColorTranslator.FromHtml(editRowForeColor);
            view.EditRowStyle.BackColor = ColorTranslator.FromHtml(editRowBackColor);
            view.EditRowStyle.Font.Bold = ((editRowFont & FONT_BOLD) != 0); 
            view.EditRowStyle.Font.Italic = ((editRowFont & FONT_ITALIC) != 0);
            view.EditRowStyle.Font.ClearDefaults(); 
            view.PagerStyle.ForeColor = ColorTranslator.FromHtml(pagerForeColor); 
            view.PagerStyle.BackColor = ColorTranslator.FromHtml(pagerBackColor);
            view.PagerStyle.Font.Bold = ((pagerFont & FONT_BOLD) != 0); 
            view.PagerStyle.Font.Italic = ((pagerFont & FONT_ITALIC) != 0);
            view.PagerStyle.HorizontalAlign = (HorizontalAlign)pagerAlign;
            view.PagerStyle.Font.ClearDefaults();
            view.PagerSettings.Mode = (PagerButtons)pagerButtons; 
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
            rowForeColor = GetStringProperty("RowForeColor", schemeData); 
            rowBackColor = GetStringProperty("RowBackColor", schemeData); 
            itemFont = GetIntProperty("RowFont", schemeData);
            alternatingRowForeColor = GetStringProperty("AltRowForeColor", schemeData); 
            alternatingRowBackColor = GetStringProperty("AltRowBackColor", schemeData);
            alternatingRowFont = GetIntProperty("AltRowFont", schemeData);
            commandRowForeColor = GetStringProperty("CommandRowForeColor", schemeData);
            commandRowBackColor = GetStringProperty("CommandRowBackColor", schemeData); 
            commandRowFont = GetIntProperty("CommandRowFont", schemeData);
            fieldHeaderForeColor = GetStringProperty("FieldHeaderForeColor", schemeData); 
            fieldHeaderBackColor = GetStringProperty("FieldHeaderBackColor", schemeData); 
            fieldHeaderFont = GetIntProperty("FieldHeaderFont", schemeData);
            editRowForeColor = GetStringProperty("EditRowForeColor", schemeData); 
            editRowBackColor = GetStringProperty("EditRowBackColor", schemeData);
            editRowFont = GetIntProperty("EditRowFont", schemeData);
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
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
