//------------------------------------------------------------------------------ 
// <copyright file="LoginAutoFormat.cs" company="Microsoft">
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
    using System.Web.UI.WebControls; 
    using System.Xml; 

    internal sealed class LoginAutoFormat : DesignerAutoFormat { 

        private string backColor;
        private string foreColor;
        private string borderColor; 
        private string borderWidth;
        private int borderStyle = -1; 
        private string fontSize; 
        private string fontName;
        private string titleTextBackColor; 
        private string titleTextForeColor;
        private int titleTextFont;
        private string titleTextFontSize;
        private int textLayout; 
        private int borderPadding ;
        private string instructionTextForeColor; 
        private int instructionTextFont; 
        private string textboxFontSize;
        private string _loginButtonBackColor; 
        private string _loginButtonForeColor;
        private string _loginButtonFontSize;
        private string _loginButtonFontName;
        private string _loginButtonBorderColor; 
        private string _loginButtonBorderWidth;
        private int _loginButtonBorderStyle = -1; 
 
        const int FONT_BOLD = 1;
        const int FONT_ITALIC = 2; 

        public LoginAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) {
            Load(schemeData);
 
            Style.Width = 300;
            Style.Height = 200; 
        } 

        public override void Apply(Control control) { 
            Debug.Assert(control is Login, "LoginAutoFormat:ApplyScheme- control is not Login");
            if (control is Login) {
                Apply(control as Login);
            } 
        }
 
        private void Apply(Login login) { 
            login.BackColor = ColorTranslator.FromHtml(backColor);
            login.ForeColor = ColorTranslator.FromHtml(foreColor); 
            login.BorderColor = ColorTranslator.FromHtml(borderColor);
            login.BorderWidth = new Unit(borderWidth, CultureInfo.InvariantCulture);
            if ((borderStyle >= 0) && (borderStyle <= 9)) {
                login.BorderStyle = (BorderStyle) borderStyle; 
            }
            else { 
                login.BorderStyle = BorderStyle.NotSet; 
            }
            login.Font.Size = new FontUnit(fontSize, CultureInfo.InvariantCulture); 
            login.Font.Name = fontName;
            login.Font.ClearDefaults();
            login.TitleTextStyle.BackColor = ColorTranslator.FromHtml(titleTextBackColor);
            login.TitleTextStyle.ForeColor = ColorTranslator.FromHtml(titleTextForeColor); 
            login.TitleTextStyle.Font.Bold = ((titleTextFont & FONT_BOLD) != 0);
            login.TitleTextStyle.Font.Size = new FontUnit(titleTextFontSize, CultureInfo.InvariantCulture); 
            login.TitleTextStyle.Font.ClearDefaults(); 
            login.BorderPadding = borderPadding;
            if (textLayout > 0) { 
                login.TextLayout = LoginTextLayout.TextOnTop;
            } else {
                login.TextLayout = LoginTextLayout.TextOnLeft;
            } 
            login.InstructionTextStyle.ForeColor = ColorTranslator.FromHtml(instructionTextForeColor);
            login.InstructionTextStyle.Font.Italic = ((instructionTextFont & FONT_ITALIC) != 0); 
            login.InstructionTextStyle.Font.ClearDefaults(); 
            login.TextBoxStyle.Font.Size = new FontUnit(textboxFontSize, CultureInfo.InvariantCulture);
            login.TextBoxStyle.Font.ClearDefaults(); 
            login.LoginButtonStyle.BackColor = ColorTranslator.FromHtml(_loginButtonBackColor);
            login.LoginButtonStyle.ForeColor = ColorTranslator.FromHtml(_loginButtonForeColor);
            login.LoginButtonStyle.Font.Size = new FontUnit(_loginButtonFontSize, CultureInfo.InvariantCulture);
            login.LoginButtonStyle.Font.Name = _loginButtonFontName; 
            login.LoginButtonStyle.BorderColor = ColorTranslator.FromHtml(_loginButtonBorderColor);
            login.LoginButtonStyle.BorderWidth = new Unit(_loginButtonBorderWidth, CultureInfo.InvariantCulture); 
            if ((_loginButtonBorderStyle >= 0) && (_loginButtonBorderStyle <= 9)) { 
                login.LoginButtonStyle.BorderStyle = (BorderStyle)_loginButtonBorderStyle;
            } 
            else {
                login.LoginButtonStyle.BorderStyle = BorderStyle.NotSet;
            }
            login.LoginButtonStyle.Font.ClearDefaults(); 
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
            foreColor = GetStringProperty("ForeColor", schemeData);
            borderColor = GetStringProperty("BorderColor", schemeData); 
            borderWidth = GetStringProperty("BorderWidth", schemeData);
            borderStyle = GetIntProperty("BorderStyle", -1, schemeData);
            fontSize = GetStringProperty("FontSize", schemeData);
            fontName = GetStringProperty("FontName", schemeData); 
            instructionTextForeColor = GetStringProperty("InstructionTextForeColor", schemeData);
            instructionTextFont = GetIntProperty("InstructionTextFont", schemeData); 
            titleTextBackColor = GetStringProperty("TitleTextBackColor", schemeData); 
            titleTextForeColor = GetStringProperty("TitleTextForeColor", schemeData);
            titleTextFont = GetIntProperty("TitleTextFont", schemeData); 
            titleTextFontSize = GetStringProperty("TitleTextFontSize", schemeData);
            borderPadding = GetIntProperty("BorderPadding", 1, schemeData);
            textLayout = GetIntProperty("TextLayout", schemeData);
            textboxFontSize = GetStringProperty("TextboxFontSize", schemeData); 
            _loginButtonBackColor = GetStringProperty("SubmitButtonBackColor", schemeData);
            _loginButtonForeColor = GetStringProperty("SubmitButtonForeColor", schemeData); 
            _loginButtonFontSize = GetStringProperty("SubmitButtonFontSize", schemeData); 
            _loginButtonFontName = GetStringProperty("SubmitButtonFontName", schemeData);
            _loginButtonBorderColor = GetStringProperty("SubmitButtonBorderColor", schemeData); 
            _loginButtonBorderWidth = GetStringProperty("SubmitButtonBorderWidth", schemeData);
            _loginButtonBorderStyle = GetIntProperty("SubmitButtonBorderStyle", -1, schemeData);
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="LoginAutoFormat.cs" company="Microsoft">
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
    using System.Web.UI.WebControls; 
    using System.Xml; 

    internal sealed class LoginAutoFormat : DesignerAutoFormat { 

        private string backColor;
        private string foreColor;
        private string borderColor; 
        private string borderWidth;
        private int borderStyle = -1; 
        private string fontSize; 
        private string fontName;
        private string titleTextBackColor; 
        private string titleTextForeColor;
        private int titleTextFont;
        private string titleTextFontSize;
        private int textLayout; 
        private int borderPadding ;
        private string instructionTextForeColor; 
        private int instructionTextFont; 
        private string textboxFontSize;
        private string _loginButtonBackColor; 
        private string _loginButtonForeColor;
        private string _loginButtonFontSize;
        private string _loginButtonFontName;
        private string _loginButtonBorderColor; 
        private string _loginButtonBorderWidth;
        private int _loginButtonBorderStyle = -1; 
 
        const int FONT_BOLD = 1;
        const int FONT_ITALIC = 2; 

        public LoginAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) {
            Load(schemeData);
 
            Style.Width = 300;
            Style.Height = 200; 
        } 

        public override void Apply(Control control) { 
            Debug.Assert(control is Login, "LoginAutoFormat:ApplyScheme- control is not Login");
            if (control is Login) {
                Apply(control as Login);
            } 
        }
 
        private void Apply(Login login) { 
            login.BackColor = ColorTranslator.FromHtml(backColor);
            login.ForeColor = ColorTranslator.FromHtml(foreColor); 
            login.BorderColor = ColorTranslator.FromHtml(borderColor);
            login.BorderWidth = new Unit(borderWidth, CultureInfo.InvariantCulture);
            if ((borderStyle >= 0) && (borderStyle <= 9)) {
                login.BorderStyle = (BorderStyle) borderStyle; 
            }
            else { 
                login.BorderStyle = BorderStyle.NotSet; 
            }
            login.Font.Size = new FontUnit(fontSize, CultureInfo.InvariantCulture); 
            login.Font.Name = fontName;
            login.Font.ClearDefaults();
            login.TitleTextStyle.BackColor = ColorTranslator.FromHtml(titleTextBackColor);
            login.TitleTextStyle.ForeColor = ColorTranslator.FromHtml(titleTextForeColor); 
            login.TitleTextStyle.Font.Bold = ((titleTextFont & FONT_BOLD) != 0);
            login.TitleTextStyle.Font.Size = new FontUnit(titleTextFontSize, CultureInfo.InvariantCulture); 
            login.TitleTextStyle.Font.ClearDefaults(); 
            login.BorderPadding = borderPadding;
            if (textLayout > 0) { 
                login.TextLayout = LoginTextLayout.TextOnTop;
            } else {
                login.TextLayout = LoginTextLayout.TextOnLeft;
            } 
            login.InstructionTextStyle.ForeColor = ColorTranslator.FromHtml(instructionTextForeColor);
            login.InstructionTextStyle.Font.Italic = ((instructionTextFont & FONT_ITALIC) != 0); 
            login.InstructionTextStyle.Font.ClearDefaults(); 
            login.TextBoxStyle.Font.Size = new FontUnit(textboxFontSize, CultureInfo.InvariantCulture);
            login.TextBoxStyle.Font.ClearDefaults(); 
            login.LoginButtonStyle.BackColor = ColorTranslator.FromHtml(_loginButtonBackColor);
            login.LoginButtonStyle.ForeColor = ColorTranslator.FromHtml(_loginButtonForeColor);
            login.LoginButtonStyle.Font.Size = new FontUnit(_loginButtonFontSize, CultureInfo.InvariantCulture);
            login.LoginButtonStyle.Font.Name = _loginButtonFontName; 
            login.LoginButtonStyle.BorderColor = ColorTranslator.FromHtml(_loginButtonBorderColor);
            login.LoginButtonStyle.BorderWidth = new Unit(_loginButtonBorderWidth, CultureInfo.InvariantCulture); 
            if ((_loginButtonBorderStyle >= 0) && (_loginButtonBorderStyle <= 9)) { 
                login.LoginButtonStyle.BorderStyle = (BorderStyle)_loginButtonBorderStyle;
            } 
            else {
                login.LoginButtonStyle.BorderStyle = BorderStyle.NotSet;
            }
            login.LoginButtonStyle.Font.ClearDefaults(); 
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
            foreColor = GetStringProperty("ForeColor", schemeData);
            borderColor = GetStringProperty("BorderColor", schemeData); 
            borderWidth = GetStringProperty("BorderWidth", schemeData);
            borderStyle = GetIntProperty("BorderStyle", -1, schemeData);
            fontSize = GetStringProperty("FontSize", schemeData);
            fontName = GetStringProperty("FontName", schemeData); 
            instructionTextForeColor = GetStringProperty("InstructionTextForeColor", schemeData);
            instructionTextFont = GetIntProperty("InstructionTextFont", schemeData); 
            titleTextBackColor = GetStringProperty("TitleTextBackColor", schemeData); 
            titleTextForeColor = GetStringProperty("TitleTextForeColor", schemeData);
            titleTextFont = GetIntProperty("TitleTextFont", schemeData); 
            titleTextFontSize = GetStringProperty("TitleTextFontSize", schemeData);
            borderPadding = GetIntProperty("BorderPadding", 1, schemeData);
            textLayout = GetIntProperty("TextLayout", schemeData);
            textboxFontSize = GetStringProperty("TextboxFontSize", schemeData); 
            _loginButtonBackColor = GetStringProperty("SubmitButtonBackColor", schemeData);
            _loginButtonForeColor = GetStringProperty("SubmitButtonForeColor", schemeData); 
            _loginButtonFontSize = GetStringProperty("SubmitButtonFontSize", schemeData); 
            _loginButtonFontName = GetStringProperty("SubmitButtonFontName", schemeData);
            _loginButtonBorderColor = GetStringProperty("SubmitButtonBorderColor", schemeData); 
            _loginButtonBorderWidth = GetStringProperty("SubmitButtonBorderWidth", schemeData);
            _loginButtonBorderStyle = GetIntProperty("SubmitButtonBorderStyle", -1, schemeData);
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
