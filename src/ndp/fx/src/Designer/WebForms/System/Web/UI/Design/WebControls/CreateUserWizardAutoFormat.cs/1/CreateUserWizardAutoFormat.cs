//------------------------------------------------------------------------------ 
// <copyright file="CreateUserWizardAutoFormat.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Data;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.IO; 
    using System.Text;
    using System.Web.UI; 
    using System.Web.UI.WebControls; 
    using System.Xml;
    using System.ComponentModel; 

    internal sealed class CreateUserWizardAutoFormat : DesignerAutoFormat {

        private string backColor; 
        private string borderColor;
        private string borderWidth; 
        private int borderStyle = -1; 
        private string fontSize;
        private string fontName; 
        private string titleTextBackColor;
        private string titleTextForeColor;
        private int titleTextFont;
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
 
        const int FONT_BOLD = 1;

        public CreateUserWizardAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) {
            Load(schemeData); 

            Style.Width = 500; 
            Style.Height = 400; 
        }
 
        public override void Apply(Control control) {
            Debug.Assert(control is CreateUserWizard, "CreateUserWizardAutoFormat:ApplyScheme- control is not CreateUserWizard");
            if (control is CreateUserWizard) {
                Apply(control as CreateUserWizard); 
            }
        } 
 
        private void Apply(CreateUserWizard createUserWizard) {
            createUserWizard.StepStyle.Reset(); 
            createUserWizard.BackColor = ColorTranslator.FromHtml(backColor);
            createUserWizard.BorderColor = ColorTranslator.FromHtml(borderColor);
            createUserWizard.BorderWidth = new Unit(borderWidth, CultureInfo.InvariantCulture);
            if ((borderStyle >= 0) && (borderStyle <= 9)) { 
                createUserWizard.BorderStyle = (BorderStyle) borderStyle;
            } 
            else { 
                createUserWizard.BorderStyle = BorderStyle.NotSet;
            } 
            createUserWizard.Font.Size = new FontUnit(fontSize, CultureInfo.InvariantCulture);
            createUserWizard.Font.Name = fontName;
            createUserWizard.Font.ClearDefaults();
            createUserWizard.TitleTextStyle.BackColor = ColorTranslator.FromHtml(titleTextBackColor); 
            createUserWizard.TitleTextStyle.ForeColor = ColorTranslator.FromHtml(titleTextForeColor);
            createUserWizard.TitleTextStyle.Font.Bold = ((titleTextFont & FONT_BOLD) != 0); 
            createUserWizard.TitleTextStyle.Font.ClearDefaults(); 

            createUserWizard.StepStyle.BorderWidth = StepStyleBorderWidth; 
            createUserWizard.StepStyle.BorderStyle = StepStyleBorderStyle;
            createUserWizard.StepStyle.BorderColor = StepStyleBorderColor;
            createUserWizard.StepStyle.ForeColor = StepStyleForeColor;
            createUserWizard.StepStyle.BackColor = StepStyleBackColor; 
            createUserWizard.StepStyle.Font.Size = StepStyleFontSize;
            createUserWizard.StepStyle.Font.ClearDefaults(); 
 
            createUserWizard.SideBarButtonStyle.Font.Underline = SideBarButtonStyleFontUnderline;
            createUserWizard.SideBarButtonStyle.Font.Name = SideBarButtonStyleFontName; 
            createUserWizard.SideBarButtonStyle.ForeColor = SideBarButtonStyleForeColor;
            createUserWizard.SideBarButtonStyle.BorderWidth = SideBarButtonStyleBorderWidth;
            createUserWizard.SideBarButtonStyle.BackColor = SideBarButtonStyleBackColor;
            createUserWizard.SideBarButtonStyle.Font.ClearDefaults(); 

            createUserWizard.NavigationButtonStyle.BorderWidth = NavigationButtonStyleBorderWidth; 
            createUserWizard.NavigationButtonStyle.Font.Name = NavigationButtonStyleFontName; 
            createUserWizard.NavigationButtonStyle.Font.Size = NavigationButtonStyleFontSize;
            createUserWizard.NavigationButtonStyle.BorderStyle = NavigationButtonStyleBorderStyle; 
            createUserWizard.NavigationButtonStyle.BorderColor = NavigationButtonStyleBorderColor;
            createUserWizard.NavigationButtonStyle.ForeColor = NavigationButtonStyleForeColor;
            createUserWizard.NavigationButtonStyle.BackColor = NavigationButtonStyleBackColor;
            createUserWizard.NavigationButtonStyle.Font.ClearDefaults(); 

            createUserWizard.ContinueButtonStyle.BorderWidth = NavigationButtonStyleBorderWidth; 
            createUserWizard.ContinueButtonStyle.Font.Name = NavigationButtonStyleFontName; 
            createUserWizard.ContinueButtonStyle.Font.Size = NavigationButtonStyleFontSize;
            createUserWizard.ContinueButtonStyle.BorderStyle = NavigationButtonStyleBorderStyle; 
            createUserWizard.ContinueButtonStyle.BorderColor = NavigationButtonStyleBorderColor;
            createUserWizard.ContinueButtonStyle.ForeColor = NavigationButtonStyleForeColor;
            createUserWizard.ContinueButtonStyle.BackColor = NavigationButtonStyleBackColor;
            createUserWizard.ContinueButtonStyle.Font.ClearDefaults(); 

            createUserWizard.CreateUserButtonStyle.BorderWidth = NavigationButtonStyleBorderWidth; 
            createUserWizard.CreateUserButtonStyle.Font.Name = NavigationButtonStyleFontName; 
            createUserWizard.CreateUserButtonStyle.Font.Size = NavigationButtonStyleFontSize;
            createUserWizard.CreateUserButtonStyle.BorderStyle = NavigationButtonStyleBorderStyle; 
            createUserWizard.CreateUserButtonStyle.BorderColor = NavigationButtonStyleBorderColor;
            createUserWizard.CreateUserButtonStyle.ForeColor = NavigationButtonStyleForeColor;
            createUserWizard.CreateUserButtonStyle.BackColor = NavigationButtonStyleBackColor;
            createUserWizard.CreateUserButtonStyle.Font.ClearDefaults(); 

            createUserWizard.HeaderStyle.ForeColor = HeaderStyleForeColor; 
            createUserWizard.HeaderStyle.BorderColor = HeaderStyleBorderColor; 
            createUserWizard.HeaderStyle.BackColor = HeaderStyleBackColor;
            createUserWizard.HeaderStyle.Font.Size = HeaderStyleFontSize; 
            createUserWizard.HeaderStyle.Font.Bold = HeaderStyleFontBold;
            createUserWizard.HeaderStyle.BorderWidth = HeaderStyleBorderWidth;
            createUserWizard.HeaderStyle.HorizontalAlign = HeaderStyleHorizontalAlign;
            createUserWizard.HeaderStyle.BorderStyle = HeaderStyleBorderStyle; 
            createUserWizard.HeaderStyle.Font.ClearDefaults();
 
            createUserWizard.SideBarStyle.BackColor = SideBarStyleBackColor; 
            createUserWizard.SideBarStyle.VerticalAlign = SideBarStyleVerticalAlign;
            createUserWizard.SideBarStyle.Font.Size = SideBarStyleFontSize; 
            createUserWizard.SideBarStyle.Font.Underline = SideBarStyleFontUnderline;
            createUserWizard.SideBarStyle.Font.Strikeout = SideBarStyleFontStrikeout;
            createUserWizard.SideBarStyle.BorderWidth = SideBarStyleBorderWidth;
            createUserWizard.SideBarStyle.Font.ClearDefaults(); 
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

            backColor = GetStringProperty("BackColor", schemeData); 
            borderColor = GetStringProperty("BorderColor", schemeData); 
            borderWidth = GetStringProperty("BorderWidth", schemeData);
            borderStyle = GetIntProperty("BorderStyle", -1, schemeData); 
            fontSize = GetStringProperty("FontSize", schemeData);
            fontName = GetStringProperty("FontName", schemeData);
            titleTextBackColor = GetStringProperty("TitleTextBackColor", schemeData);
            titleTextForeColor = GetStringProperty("TitleTextForeColor", schemeData); 
            titleTextFont = GetIntProperty("TitleTextFont", schemeData);
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
// <copyright file="CreateUserWizardAutoFormat.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Data;
    using System.Design; 
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.IO; 
    using System.Text;
    using System.Web.UI; 
    using System.Web.UI.WebControls; 
    using System.Xml;
    using System.ComponentModel; 

    internal sealed class CreateUserWizardAutoFormat : DesignerAutoFormat {

        private string backColor; 
        private string borderColor;
        private string borderWidth; 
        private int borderStyle = -1; 
        private string fontSize;
        private string fontName; 
        private string titleTextBackColor;
        private string titleTextForeColor;
        private int titleTextFont;
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
 
        const int FONT_BOLD = 1;

        public CreateUserWizardAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) {
            Load(schemeData); 

            Style.Width = 500; 
            Style.Height = 400; 
        }
 
        public override void Apply(Control control) {
            Debug.Assert(control is CreateUserWizard, "CreateUserWizardAutoFormat:ApplyScheme- control is not CreateUserWizard");
            if (control is CreateUserWizard) {
                Apply(control as CreateUserWizard); 
            }
        } 
 
        private void Apply(CreateUserWizard createUserWizard) {
            createUserWizard.StepStyle.Reset(); 
            createUserWizard.BackColor = ColorTranslator.FromHtml(backColor);
            createUserWizard.BorderColor = ColorTranslator.FromHtml(borderColor);
            createUserWizard.BorderWidth = new Unit(borderWidth, CultureInfo.InvariantCulture);
            if ((borderStyle >= 0) && (borderStyle <= 9)) { 
                createUserWizard.BorderStyle = (BorderStyle) borderStyle;
            } 
            else { 
                createUserWizard.BorderStyle = BorderStyle.NotSet;
            } 
            createUserWizard.Font.Size = new FontUnit(fontSize, CultureInfo.InvariantCulture);
            createUserWizard.Font.Name = fontName;
            createUserWizard.Font.ClearDefaults();
            createUserWizard.TitleTextStyle.BackColor = ColorTranslator.FromHtml(titleTextBackColor); 
            createUserWizard.TitleTextStyle.ForeColor = ColorTranslator.FromHtml(titleTextForeColor);
            createUserWizard.TitleTextStyle.Font.Bold = ((titleTextFont & FONT_BOLD) != 0); 
            createUserWizard.TitleTextStyle.Font.ClearDefaults(); 

            createUserWizard.StepStyle.BorderWidth = StepStyleBorderWidth; 
            createUserWizard.StepStyle.BorderStyle = StepStyleBorderStyle;
            createUserWizard.StepStyle.BorderColor = StepStyleBorderColor;
            createUserWizard.StepStyle.ForeColor = StepStyleForeColor;
            createUserWizard.StepStyle.BackColor = StepStyleBackColor; 
            createUserWizard.StepStyle.Font.Size = StepStyleFontSize;
            createUserWizard.StepStyle.Font.ClearDefaults(); 
 
            createUserWizard.SideBarButtonStyle.Font.Underline = SideBarButtonStyleFontUnderline;
            createUserWizard.SideBarButtonStyle.Font.Name = SideBarButtonStyleFontName; 
            createUserWizard.SideBarButtonStyle.ForeColor = SideBarButtonStyleForeColor;
            createUserWizard.SideBarButtonStyle.BorderWidth = SideBarButtonStyleBorderWidth;
            createUserWizard.SideBarButtonStyle.BackColor = SideBarButtonStyleBackColor;
            createUserWizard.SideBarButtonStyle.Font.ClearDefaults(); 

            createUserWizard.NavigationButtonStyle.BorderWidth = NavigationButtonStyleBorderWidth; 
            createUserWizard.NavigationButtonStyle.Font.Name = NavigationButtonStyleFontName; 
            createUserWizard.NavigationButtonStyle.Font.Size = NavigationButtonStyleFontSize;
            createUserWizard.NavigationButtonStyle.BorderStyle = NavigationButtonStyleBorderStyle; 
            createUserWizard.NavigationButtonStyle.BorderColor = NavigationButtonStyleBorderColor;
            createUserWizard.NavigationButtonStyle.ForeColor = NavigationButtonStyleForeColor;
            createUserWizard.NavigationButtonStyle.BackColor = NavigationButtonStyleBackColor;
            createUserWizard.NavigationButtonStyle.Font.ClearDefaults(); 

            createUserWizard.ContinueButtonStyle.BorderWidth = NavigationButtonStyleBorderWidth; 
            createUserWizard.ContinueButtonStyle.Font.Name = NavigationButtonStyleFontName; 
            createUserWizard.ContinueButtonStyle.Font.Size = NavigationButtonStyleFontSize;
            createUserWizard.ContinueButtonStyle.BorderStyle = NavigationButtonStyleBorderStyle; 
            createUserWizard.ContinueButtonStyle.BorderColor = NavigationButtonStyleBorderColor;
            createUserWizard.ContinueButtonStyle.ForeColor = NavigationButtonStyleForeColor;
            createUserWizard.ContinueButtonStyle.BackColor = NavigationButtonStyleBackColor;
            createUserWizard.ContinueButtonStyle.Font.ClearDefaults(); 

            createUserWizard.CreateUserButtonStyle.BorderWidth = NavigationButtonStyleBorderWidth; 
            createUserWizard.CreateUserButtonStyle.Font.Name = NavigationButtonStyleFontName; 
            createUserWizard.CreateUserButtonStyle.Font.Size = NavigationButtonStyleFontSize;
            createUserWizard.CreateUserButtonStyle.BorderStyle = NavigationButtonStyleBorderStyle; 
            createUserWizard.CreateUserButtonStyle.BorderColor = NavigationButtonStyleBorderColor;
            createUserWizard.CreateUserButtonStyle.ForeColor = NavigationButtonStyleForeColor;
            createUserWizard.CreateUserButtonStyle.BackColor = NavigationButtonStyleBackColor;
            createUserWizard.CreateUserButtonStyle.Font.ClearDefaults(); 

            createUserWizard.HeaderStyle.ForeColor = HeaderStyleForeColor; 
            createUserWizard.HeaderStyle.BorderColor = HeaderStyleBorderColor; 
            createUserWizard.HeaderStyle.BackColor = HeaderStyleBackColor;
            createUserWizard.HeaderStyle.Font.Size = HeaderStyleFontSize; 
            createUserWizard.HeaderStyle.Font.Bold = HeaderStyleFontBold;
            createUserWizard.HeaderStyle.BorderWidth = HeaderStyleBorderWidth;
            createUserWizard.HeaderStyle.HorizontalAlign = HeaderStyleHorizontalAlign;
            createUserWizard.HeaderStyle.BorderStyle = HeaderStyleBorderStyle; 
            createUserWizard.HeaderStyle.Font.ClearDefaults();
 
            createUserWizard.SideBarStyle.BackColor = SideBarStyleBackColor; 
            createUserWizard.SideBarStyle.VerticalAlign = SideBarStyleVerticalAlign;
            createUserWizard.SideBarStyle.Font.Size = SideBarStyleFontSize; 
            createUserWizard.SideBarStyle.Font.Underline = SideBarStyleFontUnderline;
            createUserWizard.SideBarStyle.Font.Strikeout = SideBarStyleFontStrikeout;
            createUserWizard.SideBarStyle.BorderWidth = SideBarStyleBorderWidth;
            createUserWizard.SideBarStyle.Font.ClearDefaults(); 
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

            backColor = GetStringProperty("BackColor", schemeData); 
            borderColor = GetStringProperty("BorderColor", schemeData); 
            borderWidth = GetStringProperty("BorderWidth", schemeData);
            borderStyle = GetIntProperty("BorderStyle", -1, schemeData); 
            fontSize = GetStringProperty("FontSize", schemeData);
            fontName = GetStringProperty("FontName", schemeData);
            titleTextBackColor = GetStringProperty("TitleTextBackColor", schemeData);
            titleTextForeColor = GetStringProperty("TitleTextForeColor", schemeData); 
            titleTextFont = GetIntProperty("TitleTextFont", schemeData);
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
