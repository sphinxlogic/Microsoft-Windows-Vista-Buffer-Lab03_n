//------------------------------------------------------------------------------ 
// <copyright file="PasswordRecoveryAutoFormat.cs" company="Microsoft">
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

    internal sealed class PasswordRecoveryAutoFormat : DesignerAutoFormat { 

        private string backColor;
        private string borderColor;
        private string borderWidth; 
        private int borderStyle = -1;
        private string fontSize; 
        private string fontName; 
        private string titleTextBackColor;
        private string titleTextForeColor; 
        private int titleTextFont;
        private string titleTextFontSize;
        private int borderPadding = 1;
        private string instructionTextForeColor; 
        private int instructionTextFont;
        private string textboxFontSize; 
        private string submitButtonBackColor; 
        private string submitButtonForeColor;
        private string submitButtonFontSize; 
        private string submitButtonFontName;
        private string submitButtonBorderColor;
        private string submitButtonBorderWidth;
        private int submitButtonBorderStyle = -1; 
        private string successTextForeColor;
        private int successTextFont; 
 
        const int FONT_BOLD = 1;
        const int FONT_ITALIC = 2; 

        public PasswordRecoveryAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) {
            Load(schemeData);
 
            Style.Width = 500;
            Style.Height = 300; 
        } 

        public override void Apply(Control control) { 
            Debug.Assert(control is PasswordRecovery,
                         "PasswordRecoveryAutoFormat:ApplyScheme- control is not PasswordRecovery");
            if (control is PasswordRecovery) {
                Apply(control as PasswordRecovery); 
            }
        } 
 
        private void Apply(PasswordRecovery passwordRecovery) {
 
            passwordRecovery.BackColor = ColorTranslator.FromHtml(backColor);
            passwordRecovery.BorderColor = ColorTranslator.FromHtml(borderColor);
            passwordRecovery.BorderWidth = new Unit(borderWidth, CultureInfo.InvariantCulture);
            if ((borderStyle >= 0) && (borderStyle <= 9)) { 
                passwordRecovery.BorderStyle = (BorderStyle) borderStyle;
            } 
            else { 
                passwordRecovery.BorderStyle = BorderStyle.NotSet;
            } 
            passwordRecovery.Font.Size = new FontUnit(fontSize, CultureInfo.InvariantCulture);
            passwordRecovery.Font.Name = fontName;
            passwordRecovery.Font.ClearDefaults();
            passwordRecovery.TitleTextStyle.BackColor = ColorTranslator.FromHtml(titleTextBackColor); 
            passwordRecovery.TitleTextStyle.ForeColor = ColorTranslator.FromHtml(titleTextForeColor);
            passwordRecovery.TitleTextStyle.Font.Bold = ((titleTextFont & FONT_BOLD) != 0); 
            passwordRecovery.TitleTextStyle.Font.Size = new FontUnit(titleTextFontSize, CultureInfo.InvariantCulture); 
            passwordRecovery.TitleTextStyle.Font.ClearDefaults();
            passwordRecovery.BorderPadding = borderPadding; 
            passwordRecovery.InstructionTextStyle.ForeColor = ColorTranslator.FromHtml(instructionTextForeColor);
            passwordRecovery.InstructionTextStyle.Font.Italic = ((instructionTextFont & FONT_ITALIC) != 0);
            passwordRecovery.InstructionTextStyle.Font.ClearDefaults();
            passwordRecovery.TextBoxStyle.Font.Size = new FontUnit(textboxFontSize, CultureInfo.InvariantCulture); 
            passwordRecovery.TextBoxStyle.Font.ClearDefaults();
            passwordRecovery.SubmitButtonStyle.BackColor = ColorTranslator.FromHtml(submitButtonBackColor); 
            passwordRecovery.SubmitButtonStyle.ForeColor = ColorTranslator.FromHtml(submitButtonForeColor); 
            passwordRecovery.SubmitButtonStyle.Font.Size = new FontUnit(submitButtonFontSize, CultureInfo.InvariantCulture);
            passwordRecovery.SubmitButtonStyle.Font.Name = submitButtonFontName; 
            passwordRecovery.SubmitButtonStyle.BorderColor = ColorTranslator.FromHtml(submitButtonBorderColor);
            passwordRecovery.SubmitButtonStyle.BorderWidth = new Unit(submitButtonBorderWidth, CultureInfo.InvariantCulture);
            if ((submitButtonBorderStyle >= 0) && (submitButtonBorderStyle <= 9)) {
                passwordRecovery.SubmitButtonStyle.BorderStyle = (BorderStyle) submitButtonBorderStyle; 
            }
            else { 
                passwordRecovery.SubmitButtonStyle.BorderStyle = BorderStyle.NotSet; 
            }
            passwordRecovery.SubmitButtonStyle.Font.ClearDefaults(); 
            passwordRecovery.SuccessTextStyle.ForeColor = ColorTranslator.FromHtml(successTextForeColor);
            passwordRecovery.SuccessTextStyle.Font.Bold = ((successTextFont & FONT_BOLD) != 0);
            passwordRecovery.SuccessTextStyle.Font.ClearDefaults();
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
            titleTextFontSize = GetStringProperty("TitleTextFontSize", schemeData); 
            instructionTextForeColor = GetStringProperty("InstructionTextForeColor", schemeData); 
            instructionTextFont = GetIntProperty("InstructionTextFont", schemeData);
            borderPadding = GetIntProperty("BorderPadding", 1, schemeData); 
            textboxFontSize = GetStringProperty("TextboxFontSize", schemeData);
            submitButtonBackColor = GetStringProperty("SubmitButtonBackColor", schemeData);
            submitButtonForeColor = GetStringProperty("SubmitButtonForeColor", schemeData);
            submitButtonFontSize = GetStringProperty("SubmitButtonFontSize", schemeData); 
            submitButtonFontName = GetStringProperty("SubmitButtonFontName", schemeData);
            submitButtonBorderColor = GetStringProperty("SubmitButtonBorderColor", schemeData); 
            submitButtonBorderWidth = GetStringProperty("SubmitButtonBorderWidth", schemeData); 
            submitButtonBorderStyle = GetIntProperty("SubmitButtonBorderStyle", -1, schemeData);
            successTextForeColor = GetStringProperty("SuccessTextForeColor", schemeData); 
            successTextFont = GetIntProperty("SuccessTextFont", schemeData);
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="PasswordRecoveryAutoFormat.cs" company="Microsoft">
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

    internal sealed class PasswordRecoveryAutoFormat : DesignerAutoFormat { 

        private string backColor;
        private string borderColor;
        private string borderWidth; 
        private int borderStyle = -1;
        private string fontSize; 
        private string fontName; 
        private string titleTextBackColor;
        private string titleTextForeColor; 
        private int titleTextFont;
        private string titleTextFontSize;
        private int borderPadding = 1;
        private string instructionTextForeColor; 
        private int instructionTextFont;
        private string textboxFontSize; 
        private string submitButtonBackColor; 
        private string submitButtonForeColor;
        private string submitButtonFontSize; 
        private string submitButtonFontName;
        private string submitButtonBorderColor;
        private string submitButtonBorderWidth;
        private int submitButtonBorderStyle = -1; 
        private string successTextForeColor;
        private int successTextFont; 
 
        const int FONT_BOLD = 1;
        const int FONT_ITALIC = 2; 

        public PasswordRecoveryAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) {
            Load(schemeData);
 
            Style.Width = 500;
            Style.Height = 300; 
        } 

        public override void Apply(Control control) { 
            Debug.Assert(control is PasswordRecovery,
                         "PasswordRecoveryAutoFormat:ApplyScheme- control is not PasswordRecovery");
            if (control is PasswordRecovery) {
                Apply(control as PasswordRecovery); 
            }
        } 
 
        private void Apply(PasswordRecovery passwordRecovery) {
 
            passwordRecovery.BackColor = ColorTranslator.FromHtml(backColor);
            passwordRecovery.BorderColor = ColorTranslator.FromHtml(borderColor);
            passwordRecovery.BorderWidth = new Unit(borderWidth, CultureInfo.InvariantCulture);
            if ((borderStyle >= 0) && (borderStyle <= 9)) { 
                passwordRecovery.BorderStyle = (BorderStyle) borderStyle;
            } 
            else { 
                passwordRecovery.BorderStyle = BorderStyle.NotSet;
            } 
            passwordRecovery.Font.Size = new FontUnit(fontSize, CultureInfo.InvariantCulture);
            passwordRecovery.Font.Name = fontName;
            passwordRecovery.Font.ClearDefaults();
            passwordRecovery.TitleTextStyle.BackColor = ColorTranslator.FromHtml(titleTextBackColor); 
            passwordRecovery.TitleTextStyle.ForeColor = ColorTranslator.FromHtml(titleTextForeColor);
            passwordRecovery.TitleTextStyle.Font.Bold = ((titleTextFont & FONT_BOLD) != 0); 
            passwordRecovery.TitleTextStyle.Font.Size = new FontUnit(titleTextFontSize, CultureInfo.InvariantCulture); 
            passwordRecovery.TitleTextStyle.Font.ClearDefaults();
            passwordRecovery.BorderPadding = borderPadding; 
            passwordRecovery.InstructionTextStyle.ForeColor = ColorTranslator.FromHtml(instructionTextForeColor);
            passwordRecovery.InstructionTextStyle.Font.Italic = ((instructionTextFont & FONT_ITALIC) != 0);
            passwordRecovery.InstructionTextStyle.Font.ClearDefaults();
            passwordRecovery.TextBoxStyle.Font.Size = new FontUnit(textboxFontSize, CultureInfo.InvariantCulture); 
            passwordRecovery.TextBoxStyle.Font.ClearDefaults();
            passwordRecovery.SubmitButtonStyle.BackColor = ColorTranslator.FromHtml(submitButtonBackColor); 
            passwordRecovery.SubmitButtonStyle.ForeColor = ColorTranslator.FromHtml(submitButtonForeColor); 
            passwordRecovery.SubmitButtonStyle.Font.Size = new FontUnit(submitButtonFontSize, CultureInfo.InvariantCulture);
            passwordRecovery.SubmitButtonStyle.Font.Name = submitButtonFontName; 
            passwordRecovery.SubmitButtonStyle.BorderColor = ColorTranslator.FromHtml(submitButtonBorderColor);
            passwordRecovery.SubmitButtonStyle.BorderWidth = new Unit(submitButtonBorderWidth, CultureInfo.InvariantCulture);
            if ((submitButtonBorderStyle >= 0) && (submitButtonBorderStyle <= 9)) {
                passwordRecovery.SubmitButtonStyle.BorderStyle = (BorderStyle) submitButtonBorderStyle; 
            }
            else { 
                passwordRecovery.SubmitButtonStyle.BorderStyle = BorderStyle.NotSet; 
            }
            passwordRecovery.SubmitButtonStyle.Font.ClearDefaults(); 
            passwordRecovery.SuccessTextStyle.ForeColor = ColorTranslator.FromHtml(successTextForeColor);
            passwordRecovery.SuccessTextStyle.Font.Bold = ((successTextFont & FONT_BOLD) != 0);
            passwordRecovery.SuccessTextStyle.Font.ClearDefaults();
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
            titleTextFontSize = GetStringProperty("TitleTextFontSize", schemeData); 
            instructionTextForeColor = GetStringProperty("InstructionTextForeColor", schemeData); 
            instructionTextFont = GetIntProperty("InstructionTextFont", schemeData);
            borderPadding = GetIntProperty("BorderPadding", 1, schemeData); 
            textboxFontSize = GetStringProperty("TextboxFontSize", schemeData);
            submitButtonBackColor = GetStringProperty("SubmitButtonBackColor", schemeData);
            submitButtonForeColor = GetStringProperty("SubmitButtonForeColor", schemeData);
            submitButtonFontSize = GetStringProperty("SubmitButtonFontSize", schemeData); 
            submitButtonFontName = GetStringProperty("SubmitButtonFontName", schemeData);
            submitButtonBorderColor = GetStringProperty("SubmitButtonBorderColor", schemeData); 
            submitButtonBorderWidth = GetStringProperty("SubmitButtonBorderWidth", schemeData); 
            submitButtonBorderStyle = GetIntProperty("SubmitButtonBorderStyle", -1, schemeData);
            successTextForeColor = GetStringProperty("SuccessTextForeColor", schemeData); 
            successTextFont = GetIntProperty("SuccessTextFont", schemeData);
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
