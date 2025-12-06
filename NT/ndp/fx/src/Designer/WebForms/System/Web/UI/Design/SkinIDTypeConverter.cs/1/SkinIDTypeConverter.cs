namespace System.Web.UI.Design { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Globalization;
 
    public class SkinIDTypeConverter : TypeConverter { 
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType == typeof(string)) { 
                return true;
            }

            return base.CanConvertFrom(context, sourceType); 
        }
 
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) { 
            if (value is string) {
               return value; 
            }

            return base.ConvertFrom(context, culture, value);
        } 

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType) { 
            if (destType == typeof(string)) { 
                return true;
            } 

            return base.CanConvertTo(context, destType);
        }
 
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (value is string) { 
                return value; 
            }
 
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 
            if (context == null) return new StandardValuesCollection(new ArrayList());
 
            Control control = (Control)context.Instance; 

            ArrayList skins = new ArrayList(); 
            if (control.Site != null) {
                IThemeResolutionService themeService = (IThemeResolutionService)control.Site.GetService(typeof(IThemeResolutionService));
                ThemeProvider stylesheetThemeProvider = themeService.GetStylesheetThemeProvider();
                ThemeProvider themeProvider = themeService.GetThemeProvider(); 

                if (stylesheetThemeProvider != null) { 
                    skins.AddRange(stylesheetThemeProvider.GetSkinsForControl(control.GetType())); 
                    skins.Remove(String.Empty);
                } 

                if (themeProvider != null) {
                    ICollection themeSkins = themeProvider.GetSkinsForControl(control.GetType());
                    foreach (string skinID in themeSkins) { 
                        if (!skins.Contains(skinID)) {
                            skins.Add(skinID); 
                        } 
                    }
                    skins.Remove(String.Empty); 
                }
                skins.Sort();
            }
 
            return new StandardValuesCollection(skins);
        } 
 
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
            ThemeProvider themeProvider = null; 
            if (context != null) {
                Control control = (Control)context.Instance;

                if (control.Site != null) { 
                    IThemeResolutionService themeService = (IThemeResolutionService)control.Site.GetService(typeof(IThemeResolutionService));
                    if (themeService != null) { 
                        themeProvider = themeService.GetThemeProvider(); 

                        if (themeProvider == null) { 
                            themeProvider = themeService.GetStylesheetThemeProvider();
                        }
                    }
                } 
            }
 
            return (themeProvider != null); 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Web.UI.Design { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Globalization;
 
    public class SkinIDTypeConverter : TypeConverter { 
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType == typeof(string)) { 
                return true;
            }

            return base.CanConvertFrom(context, sourceType); 
        }
 
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) { 
            if (value is string) {
               return value; 
            }

            return base.ConvertFrom(context, culture, value);
        } 

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType) { 
            if (destType == typeof(string)) { 
                return true;
            } 

            return base.CanConvertTo(context, destType);
        }
 
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (value is string) { 
                return value; 
            }
 
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 
            if (context == null) return new StandardValuesCollection(new ArrayList());
 
            Control control = (Control)context.Instance; 

            ArrayList skins = new ArrayList(); 
            if (control.Site != null) {
                IThemeResolutionService themeService = (IThemeResolutionService)control.Site.GetService(typeof(IThemeResolutionService));
                ThemeProvider stylesheetThemeProvider = themeService.GetStylesheetThemeProvider();
                ThemeProvider themeProvider = themeService.GetThemeProvider(); 

                if (stylesheetThemeProvider != null) { 
                    skins.AddRange(stylesheetThemeProvider.GetSkinsForControl(control.GetType())); 
                    skins.Remove(String.Empty);
                } 

                if (themeProvider != null) {
                    ICollection themeSkins = themeProvider.GetSkinsForControl(control.GetType());
                    foreach (string skinID in themeSkins) { 
                        if (!skins.Contains(skinID)) {
                            skins.Add(skinID); 
                        } 
                    }
                    skins.Remove(String.Empty); 
                }
                skins.Sort();
            }
 
            return new StandardValuesCollection(skins);
        } 
 
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
            ThemeProvider themeProvider = null; 
            if (context != null) {
                Control control = (Control)context.Instance;

                if (control.Site != null) { 
                    IThemeResolutionService themeService = (IThemeResolutionService)control.Site.GetService(typeof(IThemeResolutionService));
                    if (themeService != null) { 
                        themeProvider = themeService.GetThemeProvider(); 

                        if (themeProvider == null) { 
                            themeProvider = themeService.GetStylesheetThemeProvider();
                        }
                    }
                } 
            }
 
            return (themeProvider != null); 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
