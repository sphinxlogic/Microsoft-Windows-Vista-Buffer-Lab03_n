//------------------------------------------------------------------------------ 
// <copyright file="ChangePasswordAutoFormat.cs" company="Microsoft">
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
 
    internal sealed class ChangePasswordAutoFormat : DesignerAutoFormat {

        private string _backColor;
        private string _borderColor; 
        private string _borderWidth;
        private int _borderStyle = -1; 
        private string _fontSize; 
        private string _fontName;
        private string _titleTextBackColor; 
        private string _titleTextForeColor;
        private int _titleTextFont;
        private string _titleTextFontSize;
        private int _borderPadding = 1; 
        private string _instructionTextForeColor;
        private int _instructionTextFont; 
        private string _textboxFontSize; 
        private string _buttonBackColor;
        private string _buttonForeColor; 
        private string _buttonFontSize;
        private string _buttonFontName;
        private string _buttonBorderColor;
        private string _buttonBorderWidth; 
        private int _buttonBorderStyle = -1;
        private string _passwordHintForeColor; 
        private int _passwordHintFont; 

        const int FONT_BOLD = 1; 
        const int FONT_ITALIC = 2;

        public ChangePasswordAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) {
            Load(schemeData); 

            Style.Width = 400; 
            Style.Height = 250; 
        }
 
        public override void Apply(Control control) {
            Debug.Assert(control is ChangePassword, "ChangePasswordAutoFormat:ApplyScheme- control is not ChangePassword");
            if (control is ChangePassword) {
                Apply(control as ChangePassword); 
            }
        } 
 
        private void Apply(ChangePassword changePassword) {
            changePassword.BackColor = ColorTranslator.FromHtml(_backColor); 
            changePassword.BorderColor = ColorTranslator.FromHtml(_borderColor);
            changePassword.BorderWidth = new Unit(_borderWidth, CultureInfo.InvariantCulture);
            if ((_borderStyle >= 0) && (_borderStyle <= 9)) {
                changePassword.BorderStyle = (BorderStyle) _borderStyle; 
            }
            else { 
                changePassword.BorderStyle = BorderStyle.NotSet; 
            }
            changePassword.Font.Size = new FontUnit(_fontSize, CultureInfo.InvariantCulture); 
            changePassword.Font.Name = _fontName;
            changePassword.Font.ClearDefaults();
            changePassword.TitleTextStyle.BackColor = ColorTranslator.FromHtml(_titleTextBackColor);
            changePassword.TitleTextStyle.ForeColor = ColorTranslator.FromHtml(_titleTextForeColor); 
            changePassword.TitleTextStyle.Font.Bold = ((_titleTextFont & FONT_BOLD) != 0);
            changePassword.TitleTextStyle.Font.Size = new FontUnit(_titleTextFontSize, CultureInfo.InvariantCulture); 
            changePassword.TitleTextStyle.Font.ClearDefaults(); 
            changePassword.BorderPadding = _borderPadding;
            changePassword.InstructionTextStyle.ForeColor = ColorTranslator.FromHtml(_instructionTextForeColor); 
            changePassword.InstructionTextStyle.Font.Italic = ((_instructionTextFont & FONT_ITALIC) != 0);
            changePassword.InstructionTextStyle.Font.ClearDefaults();
            changePassword.TextBoxStyle.Font.Size = new FontUnit(_textboxFontSize, CultureInfo.InvariantCulture);
            changePassword.TextBoxStyle.Font.ClearDefaults(); 
            changePassword.ChangePasswordButtonStyle.BackColor = ColorTranslator.FromHtml(_buttonBackColor);
            changePassword.ChangePasswordButtonStyle.ForeColor = ColorTranslator.FromHtml(_buttonForeColor); 
            changePassword.ChangePasswordButtonStyle.Font.Size = new FontUnit(_buttonFontSize, CultureInfo.InvariantCulture); 
            changePassword.ChangePasswordButtonStyle.Font.Name = _buttonFontName;
            changePassword.ChangePasswordButtonStyle.BorderColor = ColorTranslator.FromHtml(_buttonBorderColor); 
            changePassword.ChangePasswordButtonStyle.BorderWidth = new Unit(_buttonBorderWidth, CultureInfo.InvariantCulture);
            if ((_buttonBorderStyle >= 0) && (_buttonBorderStyle <= 9)) {
                changePassword.ChangePasswordButtonStyle.BorderStyle = (BorderStyle) _buttonBorderStyle;
            } 
            else {
                changePassword.ChangePasswordButtonStyle.BorderStyle = BorderStyle.NotSet; 
            } 
            changePassword.ChangePasswordButtonStyle.Font.ClearDefaults();
            changePassword.ContinueButtonStyle.BackColor = ColorTranslator.FromHtml(_buttonBackColor); 
            changePassword.ContinueButtonStyle.ForeColor = ColorTranslator.FromHtml(_buttonForeColor);
            changePassword.ContinueButtonStyle.Font.Size = new FontUnit(_buttonFontSize, CultureInfo.InvariantCulture);
            changePassword.ContinueButtonStyle.Font.Name = _buttonFontName;
            changePassword.ContinueButtonStyle.BorderColor = ColorTranslator.FromHtml(_buttonBorderColor); 
            changePassword.ContinueButtonStyle.BorderWidth = new Unit(_buttonBorderWidth, CultureInfo.InvariantCulture);
            if ((_buttonBorderStyle >= 0) && (_buttonBorderStyle <= 9)) { 
                changePassword.ContinueButtonStyle.BorderStyle = (BorderStyle) _buttonBorderStyle; 
            }
            else { 
                changePassword.ContinueButtonStyle.BorderStyle = BorderStyle.NotSet;
            }
            changePassword.ContinueButtonStyle.Font.ClearDefaults();
            changePassword.CancelButtonStyle.BackColor = ColorTranslator.FromHtml(_buttonBackColor); 
            changePassword.CancelButtonStyle.ForeColor = ColorTranslator.FromHtml(_buttonForeColor);
            changePassword.CancelButtonStyle.Font.Size = new FontUnit(_buttonFontSize, CultureInfo.InvariantCulture); 
            changePassword.CancelButtonStyle.Font.Name = _buttonFontName; 
            changePassword.CancelButtonStyle.BorderColor = ColorTranslator.FromHtml(_buttonBorderColor);
            changePassword.CancelButtonStyle.BorderWidth = new Unit(_buttonBorderWidth, CultureInfo.InvariantCulture); 
            if ((_buttonBorderStyle >= 0) && (_buttonBorderStyle <= 9)) {
                changePassword.CancelButtonStyle.BorderStyle = (BorderStyle) _buttonBorderStyle;
            }
            else { 
                changePassword.CancelButtonStyle.BorderStyle = BorderStyle.NotSet;
            } 
            changePassword.CancelButtonStyle.Font.ClearDefaults(); 

            changePassword.PasswordHintStyle.ForeColor = ColorTranslator.FromHtml(_passwordHintForeColor); 
            changePassword.PasswordHintStyle.Font.Italic = ((_passwordHintFont & FONT_ITALIC) != 0);
            changePassword.PasswordHintStyle.Font.ClearDefaults();
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
 
            _backColor = GetStringProperty("BackColor", schemeData);
            _borderColor = GetStringProperty("BorderColor", schemeData); 
            _borderWidth = GetStringProperty("BorderWidth", schemeData); 
            _borderStyle = GetIntProperty("BorderStyle", -1, schemeData);
            _fontSize = GetStringProperty("FontSize", schemeData); 
            _fontName = GetStringProperty("FontName", schemeData);
            _titleTextBackColor = GetStringProperty("TitleTextBackColor", schemeData);
            _titleTextForeColor = GetStringProperty("TitleTextForeColor", schemeData);
            _titleTextFont = GetIntProperty("TitleTextFont", schemeData); 
            _titleTextFontSize = GetStringProperty("TitleTextFontSize", schemeData);
            _instructionTextForeColor = GetStringProperty("InstructionTextForeColor", schemeData); 
            _instructionTextFont = GetIntProperty("InstructionTextFont", schemeData); 
            _borderPadding = GetIntProperty("BorderPadding", 1, schemeData);
            _textboxFontSize = GetStringProperty("TextboxFontSize", schemeData); 
            _buttonBackColor = GetStringProperty("ButtonBackColor", schemeData);
            _buttonForeColor = GetStringProperty("ButtonForeColor", schemeData);
            _buttonFontSize = GetStringProperty("ButtonFontSize", schemeData);
            _buttonFontName = GetStringProperty("ButtonFontName", schemeData); 
            _buttonBorderColor = GetStringProperty("ButtonBorderColor", schemeData);
            _buttonBorderWidth = GetStringProperty("ButtonBorderWidth", schemeData); 
            _buttonBorderStyle = GetIntProperty("ButtonBorderStyle", -1, schemeData); 
            _passwordHintForeColor = GetStringProperty("PasswordHintForeColor", schemeData);
            _passwordHintFont = GetIntProperty("PasswordHintFont", schemeData); 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ChangePasswordAutoFormat.cs" company="Microsoft">
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
 
    internal sealed class ChangePasswordAutoFormat : DesignerAutoFormat {

        private string _backColor;
        private string _borderColor; 
        private string _borderWidth;
        private int _borderStyle = -1; 
        private string _fontSize; 
        private string _fontName;
        private string _titleTextBackColor; 
        private string _titleTextForeColor;
        private int _titleTextFont;
        private string _titleTextFontSize;
        private int _borderPadding = 1; 
        private string _instructionTextForeColor;
        private int _instructionTextFont; 
        private string _textboxFontSize; 
        private string _buttonBackColor;
        private string _buttonForeColor; 
        private string _buttonFontSize;
        private string _buttonFontName;
        private string _buttonBorderColor;
        private string _buttonBorderWidth; 
        private int _buttonBorderStyle = -1;
        private string _passwordHintForeColor; 
        private int _passwordHintFont; 

        const int FONT_BOLD = 1; 
        const int FONT_ITALIC = 2;

        public ChangePasswordAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) {
            Load(schemeData); 

            Style.Width = 400; 
            Style.Height = 250; 
        }
 
        public override void Apply(Control control) {
            Debug.Assert(control is ChangePassword, "ChangePasswordAutoFormat:ApplyScheme- control is not ChangePassword");
            if (control is ChangePassword) {
                Apply(control as ChangePassword); 
            }
        } 
 
        private void Apply(ChangePassword changePassword) {
            changePassword.BackColor = ColorTranslator.FromHtml(_backColor); 
            changePassword.BorderColor = ColorTranslator.FromHtml(_borderColor);
            changePassword.BorderWidth = new Unit(_borderWidth, CultureInfo.InvariantCulture);
            if ((_borderStyle >= 0) && (_borderStyle <= 9)) {
                changePassword.BorderStyle = (BorderStyle) _borderStyle; 
            }
            else { 
                changePassword.BorderStyle = BorderStyle.NotSet; 
            }
            changePassword.Font.Size = new FontUnit(_fontSize, CultureInfo.InvariantCulture); 
            changePassword.Font.Name = _fontName;
            changePassword.Font.ClearDefaults();
            changePassword.TitleTextStyle.BackColor = ColorTranslator.FromHtml(_titleTextBackColor);
            changePassword.TitleTextStyle.ForeColor = ColorTranslator.FromHtml(_titleTextForeColor); 
            changePassword.TitleTextStyle.Font.Bold = ((_titleTextFont & FONT_BOLD) != 0);
            changePassword.TitleTextStyle.Font.Size = new FontUnit(_titleTextFontSize, CultureInfo.InvariantCulture); 
            changePassword.TitleTextStyle.Font.ClearDefaults(); 
            changePassword.BorderPadding = _borderPadding;
            changePassword.InstructionTextStyle.ForeColor = ColorTranslator.FromHtml(_instructionTextForeColor); 
            changePassword.InstructionTextStyle.Font.Italic = ((_instructionTextFont & FONT_ITALIC) != 0);
            changePassword.InstructionTextStyle.Font.ClearDefaults();
            changePassword.TextBoxStyle.Font.Size = new FontUnit(_textboxFontSize, CultureInfo.InvariantCulture);
            changePassword.TextBoxStyle.Font.ClearDefaults(); 
            changePassword.ChangePasswordButtonStyle.BackColor = ColorTranslator.FromHtml(_buttonBackColor);
            changePassword.ChangePasswordButtonStyle.ForeColor = ColorTranslator.FromHtml(_buttonForeColor); 
            changePassword.ChangePasswordButtonStyle.Font.Size = new FontUnit(_buttonFontSize, CultureInfo.InvariantCulture); 
            changePassword.ChangePasswordButtonStyle.Font.Name = _buttonFontName;
            changePassword.ChangePasswordButtonStyle.BorderColor = ColorTranslator.FromHtml(_buttonBorderColor); 
            changePassword.ChangePasswordButtonStyle.BorderWidth = new Unit(_buttonBorderWidth, CultureInfo.InvariantCulture);
            if ((_buttonBorderStyle >= 0) && (_buttonBorderStyle <= 9)) {
                changePassword.ChangePasswordButtonStyle.BorderStyle = (BorderStyle) _buttonBorderStyle;
            } 
            else {
                changePassword.ChangePasswordButtonStyle.BorderStyle = BorderStyle.NotSet; 
            } 
            changePassword.ChangePasswordButtonStyle.Font.ClearDefaults();
            changePassword.ContinueButtonStyle.BackColor = ColorTranslator.FromHtml(_buttonBackColor); 
            changePassword.ContinueButtonStyle.ForeColor = ColorTranslator.FromHtml(_buttonForeColor);
            changePassword.ContinueButtonStyle.Font.Size = new FontUnit(_buttonFontSize, CultureInfo.InvariantCulture);
            changePassword.ContinueButtonStyle.Font.Name = _buttonFontName;
            changePassword.ContinueButtonStyle.BorderColor = ColorTranslator.FromHtml(_buttonBorderColor); 
            changePassword.ContinueButtonStyle.BorderWidth = new Unit(_buttonBorderWidth, CultureInfo.InvariantCulture);
            if ((_buttonBorderStyle >= 0) && (_buttonBorderStyle <= 9)) { 
                changePassword.ContinueButtonStyle.BorderStyle = (BorderStyle) _buttonBorderStyle; 
            }
            else { 
                changePassword.ContinueButtonStyle.BorderStyle = BorderStyle.NotSet;
            }
            changePassword.ContinueButtonStyle.Font.ClearDefaults();
            changePassword.CancelButtonStyle.BackColor = ColorTranslator.FromHtml(_buttonBackColor); 
            changePassword.CancelButtonStyle.ForeColor = ColorTranslator.FromHtml(_buttonForeColor);
            changePassword.CancelButtonStyle.Font.Size = new FontUnit(_buttonFontSize, CultureInfo.InvariantCulture); 
            changePassword.CancelButtonStyle.Font.Name = _buttonFontName; 
            changePassword.CancelButtonStyle.BorderColor = ColorTranslator.FromHtml(_buttonBorderColor);
            changePassword.CancelButtonStyle.BorderWidth = new Unit(_buttonBorderWidth, CultureInfo.InvariantCulture); 
            if ((_buttonBorderStyle >= 0) && (_buttonBorderStyle <= 9)) {
                changePassword.CancelButtonStyle.BorderStyle = (BorderStyle) _buttonBorderStyle;
            }
            else { 
                changePassword.CancelButtonStyle.BorderStyle = BorderStyle.NotSet;
            } 
            changePassword.CancelButtonStyle.Font.ClearDefaults(); 

            changePassword.PasswordHintStyle.ForeColor = ColorTranslator.FromHtml(_passwordHintForeColor); 
            changePassword.PasswordHintStyle.Font.Italic = ((_passwordHintFont & FONT_ITALIC) != 0);
            changePassword.PasswordHintStyle.Font.ClearDefaults();
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
 
            _backColor = GetStringProperty("BackColor", schemeData);
            _borderColor = GetStringProperty("BorderColor", schemeData); 
            _borderWidth = GetStringProperty("BorderWidth", schemeData); 
            _borderStyle = GetIntProperty("BorderStyle", -1, schemeData);
            _fontSize = GetStringProperty("FontSize", schemeData); 
            _fontName = GetStringProperty("FontName", schemeData);
            _titleTextBackColor = GetStringProperty("TitleTextBackColor", schemeData);
            _titleTextForeColor = GetStringProperty("TitleTextForeColor", schemeData);
            _titleTextFont = GetIntProperty("TitleTextFont", schemeData); 
            _titleTextFontSize = GetStringProperty("TitleTextFontSize", schemeData);
            _instructionTextForeColor = GetStringProperty("InstructionTextForeColor", schemeData); 
            _instructionTextFont = GetIntProperty("InstructionTextFont", schemeData); 
            _borderPadding = GetIntProperty("BorderPadding", 1, schemeData);
            _textboxFontSize = GetStringProperty("TextboxFontSize", schemeData); 
            _buttonBackColor = GetStringProperty("ButtonBackColor", schemeData);
            _buttonForeColor = GetStringProperty("ButtonForeColor", schemeData);
            _buttonFontSize = GetStringProperty("ButtonFontSize", schemeData);
            _buttonFontName = GetStringProperty("ButtonFontName", schemeData); 
            _buttonBorderColor = GetStringProperty("ButtonBorderColor", schemeData);
            _buttonBorderWidth = GetStringProperty("ButtonBorderWidth", schemeData); 
            _buttonBorderStyle = GetIntProperty("ButtonBorderStyle", -1, schemeData); 
            _passwordHintForeColor = GetStringProperty("PasswordHintForeColor", schemeData);
            _passwordHintFont = GetIntProperty("PasswordHintFont", schemeData); 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
