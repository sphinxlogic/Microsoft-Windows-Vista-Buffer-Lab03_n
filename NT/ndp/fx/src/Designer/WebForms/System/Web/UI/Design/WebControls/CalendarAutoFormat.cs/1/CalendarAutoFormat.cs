//------------------------------------------------------------------------------ 
// <copyright file="CalendarAutoFormats.cs" company="Microsoft">
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

    using Calendar = System.Web.UI.WebControls.Calendar; 

    internal sealed class CalendarAutoFormat : DesignerAutoFormat {

        private Unit Width; 
        private Unit Height;
        private string FontName; 
        private FontUnit FontSize; 
        private Color ForeColor;
        private Color BackColor; 
        private Color BorderColor;
        private Unit BorderWidth;
        private BorderStyle BorderStyle;
        private bool ShowGridLines; 
        private int CellPadding;
        private int CellSpacing; 
        private DayNameFormat DayNameFormat; 
        private Color NextPrevBackColor;
        private int NextPrevFont; 
        private FontUnit NextPrevFontSize;
        private Color NextPrevForeColor;
        private NextPrevFormat NextPrevFormat;
        private VerticalAlign NextPrevVerticalAlign; 
        private TitleFormat TitleFormat;
        private Color TitleBackColor; 
        private Color TitleBorderColor; 
        private BorderStyle TitleBorderStyle;
        private Unit TitleBorderWidth; 
        private int TitleFont;
        private FontUnit TitleFontSize;
        private Color TitleForeColor;
        private Unit TitleHeight; 
        private Color DayBackColor;
        private int DayFont; 
        private FontUnit DayFontSize; 
        private Color DayForeColor;
        private Unit DayWidth; 
        private Color DayHeaderBackColor;
        private int DayHeaderFont;
        private FontUnit DayHeaderFontSize;
        private Color DayHeaderForeColor; 
        private Unit DayHeaderHeight;
        private Color TodayDayBackColor; 
        private int TodayDayFont; 
        private FontUnit TodayDayFontSize;
        private Color TodayDayForeColor; 
        private Color SelectedDayBackColor;
        private int SelectedDayFont;
        private FontUnit SelectedDayFontSize;
        private Color SelectedDayForeColor; 
        private Color OtherMonthDayBackColor;
        private int OtherMonthDayFont; 
        private FontUnit OtherMonthDayFontSize; 
        private Color OtherMonthDayForeColor;
        private Color WeekendDayBackColor; 
        private int WeekendDayFont;
        private FontUnit WeekendDayFontSize;
        private Color WeekendDayForeColor;
        private Color SelectorBackColor; 
        private int SelectorFont;
        private string SelectorFontName; 
        private FontUnit SelectorFontSize; 
        private Color SelectorForeColor;
        private Unit SelectorWidth; 

        const int FONT_BOLD = 1;
        const int FONT_ITALIC = 2;
        const int FONT_UNDERLINE = 4; 

        public CalendarAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) { 
            Load(schemeData); 

            Style.Width = 430; 
            Style.Height = 280;
        }

        public override void Apply(Control control) { 
            Debug.Assert(control is Calendar, "CalendarAutoFormat:ApplyScheme- control is not Calendar");
            if (control is Calendar) { 
                Apply(control as Calendar); 
            }
        } 

        private void Apply(Calendar calendar) {
            calendar.Width = Width;
            calendar.Height = Height; 
            calendar.Font.Name = FontName;
            calendar.Font.Size = FontSize; 
            calendar.ForeColor = ForeColor; 
            calendar.BackColor = BackColor;
            calendar.BorderColor = BorderColor; 
            calendar.BorderWidth = BorderWidth;
            calendar.BorderStyle = BorderStyle;
            calendar.ShowGridLines = ShowGridLines;
            calendar.CellPadding = CellPadding; 
            calendar.CellSpacing = CellSpacing;
            calendar.DayNameFormat = DayNameFormat; 
            calendar.TitleFormat = TitleFormat; 
            calendar.NextPrevFormat = NextPrevFormat;
            calendar.Font.ClearDefaults(); 

            calendar.NextPrevStyle.BackColor = NextPrevBackColor;
            calendar.NextPrevStyle.Font.Bold = ((NextPrevFont & FONT_BOLD) != 0);
            calendar.NextPrevStyle.Font.Italic = ((NextPrevFont & FONT_ITALIC) != 0); 
            calendar.NextPrevStyle.Font.Underline = ((NextPrevFont & FONT_UNDERLINE) != 0);
            calendar.NextPrevStyle.Font.Size = NextPrevFontSize; 
            calendar.NextPrevStyle.ForeColor = NextPrevForeColor; 
            calendar.NextPrevStyle.VerticalAlign = NextPrevVerticalAlign;
            calendar.NextPrevStyle.Font.ClearDefaults(); 

            calendar.TitleStyle.BackColor = TitleBackColor;
            calendar.TitleStyle.BorderColor = TitleBorderColor;
            calendar.TitleStyle.BorderStyle = TitleBorderStyle; 
            calendar.TitleStyle.BorderWidth = TitleBorderWidth;
            calendar.TitleStyle.Font.Bold = ((TitleFont & FONT_BOLD) != 0); 
            calendar.TitleStyle.Font.Italic = ((TitleFont & FONT_ITALIC) != 0); 
            calendar.TitleStyle.Font.Underline = ((TitleFont & FONT_UNDERLINE) != 0);
            calendar.TitleStyle.Font.Size = TitleFontSize; 
            calendar.TitleStyle.ForeColor = TitleForeColor;
            calendar.TitleStyle.Height = TitleHeight;
            calendar.TitleStyle.Font.ClearDefaults();
 
            calendar.DayStyle.BackColor = DayBackColor;
            calendar.DayStyle.Font.Bold = ((DayFont & FONT_BOLD) != 0); 
            calendar.DayStyle.Font.Italic = ((DayFont & FONT_ITALIC) != 0); 
            calendar.DayStyle.Font.Underline = ((DayFont & FONT_UNDERLINE) != 0);
            calendar.DayStyle.Font.Size = DayFontSize; 
            calendar.DayStyle.ForeColor = DayForeColor;
            calendar.DayStyle.Width = DayWidth;
            calendar.DayStyle.Font.ClearDefaults();
 
            calendar.DayHeaderStyle.BackColor = DayHeaderBackColor;
            calendar.DayHeaderStyle.Font.Bold = ((DayHeaderFont & FONT_BOLD) != 0); 
            calendar.DayHeaderStyle.Font.Italic = ((DayHeaderFont & FONT_ITALIC) != 0); 
            calendar.DayHeaderStyle.Font.Underline = ((DayHeaderFont & FONT_UNDERLINE) != 0);
            calendar.DayHeaderStyle.Font.Size = DayHeaderFontSize; 
            calendar.DayHeaderStyle.ForeColor = DayHeaderForeColor;
            calendar.DayHeaderStyle.Height = DayHeaderHeight;
            calendar.DayHeaderStyle.Font.ClearDefaults();
 
            calendar.TodayDayStyle.BackColor = TodayDayBackColor;
            calendar.TodayDayStyle.Font.Bold = ((TodayDayFont & FONT_BOLD) != 0); 
            calendar.TodayDayStyle.Font.Italic = ((TodayDayFont & FONT_ITALIC) != 0); 
            calendar.TodayDayStyle.Font.Underline = ((TodayDayFont & FONT_UNDERLINE) != 0);
            calendar.TodayDayStyle.Font.Size = TodayDayFontSize; 
            calendar.TodayDayStyle.ForeColor = TodayDayForeColor;
            calendar.TodayDayStyle.Font.ClearDefaults();

            calendar.SelectedDayStyle.BackColor = SelectedDayBackColor; 
            calendar.SelectedDayStyle.Font.Bold = ((SelectedDayFont & FONT_BOLD) != 0);
            calendar.SelectedDayStyle.Font.Italic = ((SelectedDayFont & FONT_ITALIC) != 0); 
            calendar.SelectedDayStyle.Font.Underline = ((SelectedDayFont & FONT_UNDERLINE) != 0); 
            calendar.SelectedDayStyle.Font.Size = SelectedDayFontSize;
            calendar.SelectedDayStyle.ForeColor = SelectedDayForeColor; 
            calendar.SelectedDayStyle.Font.ClearDefaults();

            calendar.OtherMonthDayStyle.BackColor = OtherMonthDayBackColor;
            calendar.OtherMonthDayStyle.Font.Bold = ((OtherMonthDayFont & FONT_BOLD) != 0); 
            calendar.OtherMonthDayStyle.Font.Italic = ((OtherMonthDayFont & FONT_ITALIC) != 0);
            calendar.OtherMonthDayStyle.Font.Underline = ((OtherMonthDayFont & FONT_UNDERLINE) != 0); 
            calendar.OtherMonthDayStyle.Font.Size = OtherMonthDayFontSize; 
            calendar.OtherMonthDayStyle.ForeColor = OtherMonthDayForeColor;
            calendar.OtherMonthDayStyle.Font.ClearDefaults(); 

            calendar.WeekendDayStyle.BackColor = WeekendDayBackColor;
            calendar.WeekendDayStyle.Font.Bold = ((WeekendDayFont & FONT_BOLD) != 0);
            calendar.WeekendDayStyle.Font.Italic = ((WeekendDayFont & FONT_ITALIC) != 0); 
            calendar.WeekendDayStyle.Font.Underline = ((WeekendDayFont & FONT_UNDERLINE) != 0);
            calendar.WeekendDayStyle.Font.Size = WeekendDayFontSize; 
            calendar.WeekendDayStyle.ForeColor = WeekendDayForeColor; 
            calendar.WeekendDayStyle.Font.ClearDefaults();
 
            calendar.SelectorStyle.BackColor = SelectorBackColor;
            calendar.SelectorStyle.Font.Bold = ((SelectorFont & FONT_BOLD) != 0);
            calendar.SelectorStyle.Font.Italic = ((SelectorFont & FONT_ITALIC) != 0);
            calendar.SelectorStyle.Font.Underline = ((SelectorFont & FONT_UNDERLINE) != 0); 
            calendar.SelectorStyle.Font.Name = SelectorFontName;
            calendar.SelectorStyle.Font.Size = SelectorFontSize; 
            calendar.SelectorStyle.ForeColor = SelectorForeColor; 
            calendar.SelectorStyle.Width = SelectorWidth;
            calendar.SelectorStyle.Font.ClearDefaults(); 
        }

        private int GetIntProperty(string propertyTag, DataRow schemeData) {
            object data = schemeData[propertyTag]; 
            if ((data != null) && !data.Equals(DBNull.Value))
                return Int32.Parse(data.ToString(), CultureInfo.InvariantCulture); 
            else 
                return 0;
        } 

        private int GetIntProperty(string propertyTag, DataRow schemeData, int defaultValue) {
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

        private string GetStringProperty(string propertyTag, DataRow schemeData, string defaultValue) { 
            object data = schemeData[propertyTag];
            if ((data != null) && !data.Equals(DBNull.Value))
                return data.ToString();
            else 
                return defaultValue;
        } 
 
        private void Load(DataRow schemeData) {
            if (schemeData == null) { 
                Debug.Write("CalendarAutoFormatUtil:LoadScheme- scheme not found");
                return;
            }
 
            Width = new Unit(GetStringProperty("Width", schemeData), CultureInfo.InvariantCulture);
            Height = new Unit(GetStringProperty("Height", schemeData), CultureInfo.InvariantCulture); 
            FontName = GetStringProperty("FontName", schemeData); 
            FontSize = new FontUnit(GetStringProperty("FontSize", schemeData), CultureInfo.InvariantCulture);
            ForeColor = ColorTranslator.FromHtml(GetStringProperty("ForeColor", schemeData)); 
            BackColor = ColorTranslator.FromHtml(GetStringProperty("BackColor", schemeData));
            BorderColor = ColorTranslator.FromHtml(GetStringProperty("BorderColor", schemeData));
            BorderWidth = new Unit(GetStringProperty("BorderWidth", schemeData), CultureInfo.InvariantCulture);
            BorderStyle = (BorderStyle)Enum.Parse(typeof(BorderStyle), GetStringProperty("BorderStyle", schemeData, "NotSet")); 
            ShowGridLines = Boolean.Parse(GetStringProperty("ShowGridLines", schemeData, "false"));
            CellPadding = GetIntProperty("CellPadding", schemeData, 2); 
            CellSpacing = GetIntProperty("CellSpacing", schemeData); 
            DayNameFormat = (DayNameFormat)Enum.Parse(typeof(DayNameFormat), GetStringProperty("DayNameFormat", schemeData, "Short"));
            NextPrevBackColor = ColorTranslator.FromHtml(GetStringProperty("NextPrevBackColor", schemeData)); 
            NextPrevFont = GetIntProperty("NextPrevFont", schemeData);
            NextPrevFontSize = new FontUnit(GetStringProperty("NextPrevFontSize", schemeData), CultureInfo.InvariantCulture);
            NextPrevForeColor = ColorTranslator.FromHtml(GetStringProperty("NextPrevForeColor", schemeData));
            NextPrevFormat = (NextPrevFormat)Enum.Parse(typeof(NextPrevFormat), GetStringProperty("NextPrevFormat", schemeData, "CustomText")); 
            NextPrevVerticalAlign = (VerticalAlign)Enum.Parse(typeof(VerticalAlign), GetStringProperty("NextPrevVerticalAlign", schemeData, "NotSet"));
            TitleFormat = (TitleFormat)Enum.Parse(typeof(TitleFormat), GetStringProperty("TitleFormat", schemeData, "MonthYear")); 
            TitleBackColor = ColorTranslator.FromHtml(GetStringProperty("TitleBackColor", schemeData)); 
            TitleBorderColor = ColorTranslator.FromHtml(GetStringProperty("TitleBorderColor", schemeData));
            TitleBorderStyle = (BorderStyle)Enum.Parse(typeof(BorderStyle), GetStringProperty("BorderStyle", schemeData, "NotSet")); 
            TitleBorderWidth = new Unit(GetStringProperty("TitleBorderWidth", schemeData), CultureInfo.InvariantCulture);
            TitleFont = GetIntProperty("TitleFont", schemeData);
            TitleFontSize = new FontUnit(GetStringProperty("TitleFontSize", schemeData), CultureInfo.InvariantCulture);
            TitleForeColor = ColorTranslator.FromHtml(GetStringProperty("TitleForeColor", schemeData)); 
            TitleHeight = new Unit(GetStringProperty("TitleHeight", schemeData), CultureInfo.InvariantCulture);
            DayBackColor = ColorTranslator.FromHtml(GetStringProperty("DayBackColor", schemeData)); 
            DayFont = GetIntProperty("DayFont", schemeData); 
            DayFontSize = new FontUnit(GetStringProperty("DayFontSize", schemeData), CultureInfo.InvariantCulture);
            DayForeColor = ColorTranslator.FromHtml(GetStringProperty("DayForeColor", schemeData)); 
            DayWidth = new Unit(GetStringProperty("DayWidth", schemeData), CultureInfo.InvariantCulture);
            DayHeaderBackColor = ColorTranslator.FromHtml(GetStringProperty("DayHeaderBackColor", schemeData));
            DayHeaderFont = GetIntProperty("DayHeaderFont", schemeData);
            DayHeaderFontSize = new FontUnit(GetStringProperty("DayHeaderFontSize", schemeData), CultureInfo.InvariantCulture); 
            DayHeaderForeColor = ColorTranslator.FromHtml(GetStringProperty("DayHeaderForeColor", schemeData));
            DayHeaderHeight = new Unit(GetStringProperty("DayHeaderHeight", schemeData), CultureInfo.InvariantCulture); 
            TodayDayBackColor = ColorTranslator.FromHtml(GetStringProperty("TodayDayBackColor", schemeData)); 
            TodayDayFont = GetIntProperty("TodayDayFont", schemeData);
            TodayDayFontSize = new FontUnit(GetStringProperty("TodayDayFontSize", schemeData), CultureInfo.InvariantCulture); 
            TodayDayForeColor = ColorTranslator.FromHtml(GetStringProperty("TodayDayForeColor", schemeData));
            SelectedDayBackColor = ColorTranslator.FromHtml(GetStringProperty("SelectedDayBackColor", schemeData));
            SelectedDayFont = GetIntProperty("SelectedDayFont", schemeData);
            SelectedDayFontSize = new FontUnit(GetStringProperty("SelectedDayFontSize", schemeData), CultureInfo.InvariantCulture); 
            SelectedDayForeColor = ColorTranslator.FromHtml(GetStringProperty("SelectedDayForeColor", schemeData));
            OtherMonthDayBackColor = ColorTranslator.FromHtml(GetStringProperty("OtherMonthDayBackColor", schemeData)); 
            OtherMonthDayFont = GetIntProperty("OtherMonthDayFont", schemeData); 
            OtherMonthDayFontSize = new FontUnit(GetStringProperty("OtherMonthDayFontSize", schemeData), CultureInfo.InvariantCulture);
            OtherMonthDayForeColor = ColorTranslator.FromHtml(GetStringProperty("OtherMonthDayForeColor", schemeData)); 
            WeekendDayBackColor = ColorTranslator.FromHtml(GetStringProperty("WeekendDayBackColor", schemeData));
            WeekendDayFont = GetIntProperty("WeekendDayFont", schemeData);
            WeekendDayFontSize = new FontUnit(GetStringProperty("WeekendDayFontSize", schemeData), CultureInfo.InvariantCulture);
            WeekendDayForeColor = ColorTranslator.FromHtml(GetStringProperty("WeekendDayForeColor", schemeData)); 
            SelectorBackColor = ColorTranslator.FromHtml(GetStringProperty("SelectorBackColor", schemeData));
            SelectorFont = GetIntProperty("SelectorFont", schemeData); 
            SelectorFontName = GetStringProperty("SelectorFontName", schemeData); 
            SelectorFontSize = new FontUnit(GetStringProperty("SelectorFontSize", schemeData), CultureInfo.InvariantCulture);
            SelectorForeColor = ColorTranslator.FromHtml(GetStringProperty("SelectorForeColor", schemeData)); 
            SelectorWidth = new Unit(GetStringProperty("SelectorWidth", schemeData), CultureInfo.InvariantCulture);

        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="CalendarAutoFormats.cs" company="Microsoft">
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

    using Calendar = System.Web.UI.WebControls.Calendar; 

    internal sealed class CalendarAutoFormat : DesignerAutoFormat {

        private Unit Width; 
        private Unit Height;
        private string FontName; 
        private FontUnit FontSize; 
        private Color ForeColor;
        private Color BackColor; 
        private Color BorderColor;
        private Unit BorderWidth;
        private BorderStyle BorderStyle;
        private bool ShowGridLines; 
        private int CellPadding;
        private int CellSpacing; 
        private DayNameFormat DayNameFormat; 
        private Color NextPrevBackColor;
        private int NextPrevFont; 
        private FontUnit NextPrevFontSize;
        private Color NextPrevForeColor;
        private NextPrevFormat NextPrevFormat;
        private VerticalAlign NextPrevVerticalAlign; 
        private TitleFormat TitleFormat;
        private Color TitleBackColor; 
        private Color TitleBorderColor; 
        private BorderStyle TitleBorderStyle;
        private Unit TitleBorderWidth; 
        private int TitleFont;
        private FontUnit TitleFontSize;
        private Color TitleForeColor;
        private Unit TitleHeight; 
        private Color DayBackColor;
        private int DayFont; 
        private FontUnit DayFontSize; 
        private Color DayForeColor;
        private Unit DayWidth; 
        private Color DayHeaderBackColor;
        private int DayHeaderFont;
        private FontUnit DayHeaderFontSize;
        private Color DayHeaderForeColor; 
        private Unit DayHeaderHeight;
        private Color TodayDayBackColor; 
        private int TodayDayFont; 
        private FontUnit TodayDayFontSize;
        private Color TodayDayForeColor; 
        private Color SelectedDayBackColor;
        private int SelectedDayFont;
        private FontUnit SelectedDayFontSize;
        private Color SelectedDayForeColor; 
        private Color OtherMonthDayBackColor;
        private int OtherMonthDayFont; 
        private FontUnit OtherMonthDayFontSize; 
        private Color OtherMonthDayForeColor;
        private Color WeekendDayBackColor; 
        private int WeekendDayFont;
        private FontUnit WeekendDayFontSize;
        private Color WeekendDayForeColor;
        private Color SelectorBackColor; 
        private int SelectorFont;
        private string SelectorFontName; 
        private FontUnit SelectorFontSize; 
        private Color SelectorForeColor;
        private Unit SelectorWidth; 

        const int FONT_BOLD = 1;
        const int FONT_ITALIC = 2;
        const int FONT_UNDERLINE = 4; 

        public CalendarAutoFormat(DataRow schemeData) : base(SR.GetString(schemeData["SchemeName"].ToString())) { 
            Load(schemeData); 

            Style.Width = 430; 
            Style.Height = 280;
        }

        public override void Apply(Control control) { 
            Debug.Assert(control is Calendar, "CalendarAutoFormat:ApplyScheme- control is not Calendar");
            if (control is Calendar) { 
                Apply(control as Calendar); 
            }
        } 

        private void Apply(Calendar calendar) {
            calendar.Width = Width;
            calendar.Height = Height; 
            calendar.Font.Name = FontName;
            calendar.Font.Size = FontSize; 
            calendar.ForeColor = ForeColor; 
            calendar.BackColor = BackColor;
            calendar.BorderColor = BorderColor; 
            calendar.BorderWidth = BorderWidth;
            calendar.BorderStyle = BorderStyle;
            calendar.ShowGridLines = ShowGridLines;
            calendar.CellPadding = CellPadding; 
            calendar.CellSpacing = CellSpacing;
            calendar.DayNameFormat = DayNameFormat; 
            calendar.TitleFormat = TitleFormat; 
            calendar.NextPrevFormat = NextPrevFormat;
            calendar.Font.ClearDefaults(); 

            calendar.NextPrevStyle.BackColor = NextPrevBackColor;
            calendar.NextPrevStyle.Font.Bold = ((NextPrevFont & FONT_BOLD) != 0);
            calendar.NextPrevStyle.Font.Italic = ((NextPrevFont & FONT_ITALIC) != 0); 
            calendar.NextPrevStyle.Font.Underline = ((NextPrevFont & FONT_UNDERLINE) != 0);
            calendar.NextPrevStyle.Font.Size = NextPrevFontSize; 
            calendar.NextPrevStyle.ForeColor = NextPrevForeColor; 
            calendar.NextPrevStyle.VerticalAlign = NextPrevVerticalAlign;
            calendar.NextPrevStyle.Font.ClearDefaults(); 

            calendar.TitleStyle.BackColor = TitleBackColor;
            calendar.TitleStyle.BorderColor = TitleBorderColor;
            calendar.TitleStyle.BorderStyle = TitleBorderStyle; 
            calendar.TitleStyle.BorderWidth = TitleBorderWidth;
            calendar.TitleStyle.Font.Bold = ((TitleFont & FONT_BOLD) != 0); 
            calendar.TitleStyle.Font.Italic = ((TitleFont & FONT_ITALIC) != 0); 
            calendar.TitleStyle.Font.Underline = ((TitleFont & FONT_UNDERLINE) != 0);
            calendar.TitleStyle.Font.Size = TitleFontSize; 
            calendar.TitleStyle.ForeColor = TitleForeColor;
            calendar.TitleStyle.Height = TitleHeight;
            calendar.TitleStyle.Font.ClearDefaults();
 
            calendar.DayStyle.BackColor = DayBackColor;
            calendar.DayStyle.Font.Bold = ((DayFont & FONT_BOLD) != 0); 
            calendar.DayStyle.Font.Italic = ((DayFont & FONT_ITALIC) != 0); 
            calendar.DayStyle.Font.Underline = ((DayFont & FONT_UNDERLINE) != 0);
            calendar.DayStyle.Font.Size = DayFontSize; 
            calendar.DayStyle.ForeColor = DayForeColor;
            calendar.DayStyle.Width = DayWidth;
            calendar.DayStyle.Font.ClearDefaults();
 
            calendar.DayHeaderStyle.BackColor = DayHeaderBackColor;
            calendar.DayHeaderStyle.Font.Bold = ((DayHeaderFont & FONT_BOLD) != 0); 
            calendar.DayHeaderStyle.Font.Italic = ((DayHeaderFont & FONT_ITALIC) != 0); 
            calendar.DayHeaderStyle.Font.Underline = ((DayHeaderFont & FONT_UNDERLINE) != 0);
            calendar.DayHeaderStyle.Font.Size = DayHeaderFontSize; 
            calendar.DayHeaderStyle.ForeColor = DayHeaderForeColor;
            calendar.DayHeaderStyle.Height = DayHeaderHeight;
            calendar.DayHeaderStyle.Font.ClearDefaults();
 
            calendar.TodayDayStyle.BackColor = TodayDayBackColor;
            calendar.TodayDayStyle.Font.Bold = ((TodayDayFont & FONT_BOLD) != 0); 
            calendar.TodayDayStyle.Font.Italic = ((TodayDayFont & FONT_ITALIC) != 0); 
            calendar.TodayDayStyle.Font.Underline = ((TodayDayFont & FONT_UNDERLINE) != 0);
            calendar.TodayDayStyle.Font.Size = TodayDayFontSize; 
            calendar.TodayDayStyle.ForeColor = TodayDayForeColor;
            calendar.TodayDayStyle.Font.ClearDefaults();

            calendar.SelectedDayStyle.BackColor = SelectedDayBackColor; 
            calendar.SelectedDayStyle.Font.Bold = ((SelectedDayFont & FONT_BOLD) != 0);
            calendar.SelectedDayStyle.Font.Italic = ((SelectedDayFont & FONT_ITALIC) != 0); 
            calendar.SelectedDayStyle.Font.Underline = ((SelectedDayFont & FONT_UNDERLINE) != 0); 
            calendar.SelectedDayStyle.Font.Size = SelectedDayFontSize;
            calendar.SelectedDayStyle.ForeColor = SelectedDayForeColor; 
            calendar.SelectedDayStyle.Font.ClearDefaults();

            calendar.OtherMonthDayStyle.BackColor = OtherMonthDayBackColor;
            calendar.OtherMonthDayStyle.Font.Bold = ((OtherMonthDayFont & FONT_BOLD) != 0); 
            calendar.OtherMonthDayStyle.Font.Italic = ((OtherMonthDayFont & FONT_ITALIC) != 0);
            calendar.OtherMonthDayStyle.Font.Underline = ((OtherMonthDayFont & FONT_UNDERLINE) != 0); 
            calendar.OtherMonthDayStyle.Font.Size = OtherMonthDayFontSize; 
            calendar.OtherMonthDayStyle.ForeColor = OtherMonthDayForeColor;
            calendar.OtherMonthDayStyle.Font.ClearDefaults(); 

            calendar.WeekendDayStyle.BackColor = WeekendDayBackColor;
            calendar.WeekendDayStyle.Font.Bold = ((WeekendDayFont & FONT_BOLD) != 0);
            calendar.WeekendDayStyle.Font.Italic = ((WeekendDayFont & FONT_ITALIC) != 0); 
            calendar.WeekendDayStyle.Font.Underline = ((WeekendDayFont & FONT_UNDERLINE) != 0);
            calendar.WeekendDayStyle.Font.Size = WeekendDayFontSize; 
            calendar.WeekendDayStyle.ForeColor = WeekendDayForeColor; 
            calendar.WeekendDayStyle.Font.ClearDefaults();
 
            calendar.SelectorStyle.BackColor = SelectorBackColor;
            calendar.SelectorStyle.Font.Bold = ((SelectorFont & FONT_BOLD) != 0);
            calendar.SelectorStyle.Font.Italic = ((SelectorFont & FONT_ITALIC) != 0);
            calendar.SelectorStyle.Font.Underline = ((SelectorFont & FONT_UNDERLINE) != 0); 
            calendar.SelectorStyle.Font.Name = SelectorFontName;
            calendar.SelectorStyle.Font.Size = SelectorFontSize; 
            calendar.SelectorStyle.ForeColor = SelectorForeColor; 
            calendar.SelectorStyle.Width = SelectorWidth;
            calendar.SelectorStyle.Font.ClearDefaults(); 
        }

        private int GetIntProperty(string propertyTag, DataRow schemeData) {
            object data = schemeData[propertyTag]; 
            if ((data != null) && !data.Equals(DBNull.Value))
                return Int32.Parse(data.ToString(), CultureInfo.InvariantCulture); 
            else 
                return 0;
        } 

        private int GetIntProperty(string propertyTag, DataRow schemeData, int defaultValue) {
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

        private string GetStringProperty(string propertyTag, DataRow schemeData, string defaultValue) { 
            object data = schemeData[propertyTag];
            if ((data != null) && !data.Equals(DBNull.Value))
                return data.ToString();
            else 
                return defaultValue;
        } 
 
        private void Load(DataRow schemeData) {
            if (schemeData == null) { 
                Debug.Write("CalendarAutoFormatUtil:LoadScheme- scheme not found");
                return;
            }
 
            Width = new Unit(GetStringProperty("Width", schemeData), CultureInfo.InvariantCulture);
            Height = new Unit(GetStringProperty("Height", schemeData), CultureInfo.InvariantCulture); 
            FontName = GetStringProperty("FontName", schemeData); 
            FontSize = new FontUnit(GetStringProperty("FontSize", schemeData), CultureInfo.InvariantCulture);
            ForeColor = ColorTranslator.FromHtml(GetStringProperty("ForeColor", schemeData)); 
            BackColor = ColorTranslator.FromHtml(GetStringProperty("BackColor", schemeData));
            BorderColor = ColorTranslator.FromHtml(GetStringProperty("BorderColor", schemeData));
            BorderWidth = new Unit(GetStringProperty("BorderWidth", schemeData), CultureInfo.InvariantCulture);
            BorderStyle = (BorderStyle)Enum.Parse(typeof(BorderStyle), GetStringProperty("BorderStyle", schemeData, "NotSet")); 
            ShowGridLines = Boolean.Parse(GetStringProperty("ShowGridLines", schemeData, "false"));
            CellPadding = GetIntProperty("CellPadding", schemeData, 2); 
            CellSpacing = GetIntProperty("CellSpacing", schemeData); 
            DayNameFormat = (DayNameFormat)Enum.Parse(typeof(DayNameFormat), GetStringProperty("DayNameFormat", schemeData, "Short"));
            NextPrevBackColor = ColorTranslator.FromHtml(GetStringProperty("NextPrevBackColor", schemeData)); 
            NextPrevFont = GetIntProperty("NextPrevFont", schemeData);
            NextPrevFontSize = new FontUnit(GetStringProperty("NextPrevFontSize", schemeData), CultureInfo.InvariantCulture);
            NextPrevForeColor = ColorTranslator.FromHtml(GetStringProperty("NextPrevForeColor", schemeData));
            NextPrevFormat = (NextPrevFormat)Enum.Parse(typeof(NextPrevFormat), GetStringProperty("NextPrevFormat", schemeData, "CustomText")); 
            NextPrevVerticalAlign = (VerticalAlign)Enum.Parse(typeof(VerticalAlign), GetStringProperty("NextPrevVerticalAlign", schemeData, "NotSet"));
            TitleFormat = (TitleFormat)Enum.Parse(typeof(TitleFormat), GetStringProperty("TitleFormat", schemeData, "MonthYear")); 
            TitleBackColor = ColorTranslator.FromHtml(GetStringProperty("TitleBackColor", schemeData)); 
            TitleBorderColor = ColorTranslator.FromHtml(GetStringProperty("TitleBorderColor", schemeData));
            TitleBorderStyle = (BorderStyle)Enum.Parse(typeof(BorderStyle), GetStringProperty("BorderStyle", schemeData, "NotSet")); 
            TitleBorderWidth = new Unit(GetStringProperty("TitleBorderWidth", schemeData), CultureInfo.InvariantCulture);
            TitleFont = GetIntProperty("TitleFont", schemeData);
            TitleFontSize = new FontUnit(GetStringProperty("TitleFontSize", schemeData), CultureInfo.InvariantCulture);
            TitleForeColor = ColorTranslator.FromHtml(GetStringProperty("TitleForeColor", schemeData)); 
            TitleHeight = new Unit(GetStringProperty("TitleHeight", schemeData), CultureInfo.InvariantCulture);
            DayBackColor = ColorTranslator.FromHtml(GetStringProperty("DayBackColor", schemeData)); 
            DayFont = GetIntProperty("DayFont", schemeData); 
            DayFontSize = new FontUnit(GetStringProperty("DayFontSize", schemeData), CultureInfo.InvariantCulture);
            DayForeColor = ColorTranslator.FromHtml(GetStringProperty("DayForeColor", schemeData)); 
            DayWidth = new Unit(GetStringProperty("DayWidth", schemeData), CultureInfo.InvariantCulture);
            DayHeaderBackColor = ColorTranslator.FromHtml(GetStringProperty("DayHeaderBackColor", schemeData));
            DayHeaderFont = GetIntProperty("DayHeaderFont", schemeData);
            DayHeaderFontSize = new FontUnit(GetStringProperty("DayHeaderFontSize", schemeData), CultureInfo.InvariantCulture); 
            DayHeaderForeColor = ColorTranslator.FromHtml(GetStringProperty("DayHeaderForeColor", schemeData));
            DayHeaderHeight = new Unit(GetStringProperty("DayHeaderHeight", schemeData), CultureInfo.InvariantCulture); 
            TodayDayBackColor = ColorTranslator.FromHtml(GetStringProperty("TodayDayBackColor", schemeData)); 
            TodayDayFont = GetIntProperty("TodayDayFont", schemeData);
            TodayDayFontSize = new FontUnit(GetStringProperty("TodayDayFontSize", schemeData), CultureInfo.InvariantCulture); 
            TodayDayForeColor = ColorTranslator.FromHtml(GetStringProperty("TodayDayForeColor", schemeData));
            SelectedDayBackColor = ColorTranslator.FromHtml(GetStringProperty("SelectedDayBackColor", schemeData));
            SelectedDayFont = GetIntProperty("SelectedDayFont", schemeData);
            SelectedDayFontSize = new FontUnit(GetStringProperty("SelectedDayFontSize", schemeData), CultureInfo.InvariantCulture); 
            SelectedDayForeColor = ColorTranslator.FromHtml(GetStringProperty("SelectedDayForeColor", schemeData));
            OtherMonthDayBackColor = ColorTranslator.FromHtml(GetStringProperty("OtherMonthDayBackColor", schemeData)); 
            OtherMonthDayFont = GetIntProperty("OtherMonthDayFont", schemeData); 
            OtherMonthDayFontSize = new FontUnit(GetStringProperty("OtherMonthDayFontSize", schemeData), CultureInfo.InvariantCulture);
            OtherMonthDayForeColor = ColorTranslator.FromHtml(GetStringProperty("OtherMonthDayForeColor", schemeData)); 
            WeekendDayBackColor = ColorTranslator.FromHtml(GetStringProperty("WeekendDayBackColor", schemeData));
            WeekendDayFont = GetIntProperty("WeekendDayFont", schemeData);
            WeekendDayFontSize = new FontUnit(GetStringProperty("WeekendDayFontSize", schemeData), CultureInfo.InvariantCulture);
            WeekendDayForeColor = ColorTranslator.FromHtml(GetStringProperty("WeekendDayForeColor", schemeData)); 
            SelectorBackColor = ColorTranslator.FromHtml(GetStringProperty("SelectorBackColor", schemeData));
            SelectorFont = GetIntProperty("SelectorFont", schemeData); 
            SelectorFontName = GetStringProperty("SelectorFontName", schemeData); 
            SelectorFontSize = new FontUnit(GetStringProperty("SelectorFontSize", schemeData), CultureInfo.InvariantCulture);
            SelectorForeColor = ColorTranslator.FromHtml(GetStringProperty("SelectorForeColor", schemeData)); 
            SelectorWidth = new Unit(GetStringProperty("SelectorWidth", schemeData), CultureInfo.InvariantCulture);

        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
