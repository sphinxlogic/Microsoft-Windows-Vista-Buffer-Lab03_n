//------------------------------------------------------------------------------ 
// <copyright file="HTMLTagNameToTypeMapper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * Mapper of html tags to control types. 
 *
 * Copyright (c) 1998 Microsoft Corporation 
 */

namespace System.Web.UI {
    using System.ComponentModel; 

    using System; 
    using System.Collections; 
    using System.Collections.Specialized;
    using System.Reflection; 
    using System.Web.UI.HtmlControls;
    using System.Web.Util;
    using System.Globalization;
 
    internal class HtmlTagNameToTypeMapper : ITagNameToTypeMapper {
        static Hashtable _tagMap; 
        static Hashtable _inputTypes; 

        internal HtmlTagNameToTypeMapper() { 
        }

        /*public*/ Type ITagNameToTypeMapper.GetControlType(string tagName, IDictionary attributeBag) {
            Type controlType; 

            if (_tagMap == null) { 
                Hashtable t = new Hashtable(10, StringComparer.OrdinalIgnoreCase); 
                t.Add("a", typeof(HtmlAnchor));
                t.Add("button", typeof(HtmlButton)); 
                t.Add("form", typeof(HtmlForm));
                t.Add("head", typeof(HtmlHead));
                t.Add("img", typeof(HtmlImage));
                t.Add("textarea", typeof(HtmlTextArea)); 
                t.Add("select", typeof(HtmlSelect));
                t.Add("table", typeof(HtmlTable)); 
                t.Add("tr", typeof(HtmlTableRow)); 
                t.Add("td", typeof(HtmlTableCell));
                t.Add("th", typeof(HtmlTableCell)); 
                _tagMap = t;
            }

            if (_inputTypes == null) { 
                Hashtable t = new Hashtable(10, StringComparer.OrdinalIgnoreCase);
                t.Add("text", typeof(HtmlInputText)); 
                t.Add("password", typeof(HtmlInputPassword)); 
                t.Add("button", typeof(HtmlInputButton));
                t.Add("submit", typeof(HtmlInputSubmit)); 
                t.Add("reset", typeof(HtmlInputReset));
                t.Add("image", typeof(HtmlInputImage));
                t.Add("checkbox", typeof(HtmlInputCheckBox));
                t.Add("radio", typeof(HtmlInputRadioButton)); 
                t.Add("hidden", typeof(HtmlInputHidden));
                t.Add("file", typeof(HtmlInputFile)); 
                _inputTypes = t; 
            }
 
            if (StringUtil.EqualsIgnoreCase("input", tagName)) {
                string type = (string)attributeBag["type"];
                if (type == null)
                    type = "text"; 

                controlType = (Type)_inputTypes[type]; 
 
                if (controlType == null)
                    throw new HttpException( 
                        SR.GetString(SR.Invalid_type_for_input_tag, type));
            }
            else {
                controlType = (Type)_tagMap[tagName]; 
                if (controlType == null)
                    controlType = typeof(HtmlGenericControl); 
            } 

            return controlType; 
        }
    }
}
 
//------------------------------------------------------------------------------ 
// <copyright file="HTMLTagNameToTypeMapper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * Mapper of html tags to control types. 
 *
 * Copyright (c) 1998 Microsoft Corporation 
 */

namespace System.Web.UI {
    using System.ComponentModel; 

    using System; 
    using System.Collections; 
    using System.Collections.Specialized;
    using System.Reflection; 
    using System.Web.UI.HtmlControls;
    using System.Web.Util;
    using System.Globalization;
 
    internal class HtmlTagNameToTypeMapper : ITagNameToTypeMapper {
        static Hashtable _tagMap; 
        static Hashtable _inputTypes; 

        internal HtmlTagNameToTypeMapper() { 
        }

        /*public*/ Type ITagNameToTypeMapper.GetControlType(string tagName, IDictionary attributeBag) {
            Type controlType; 

            if (_tagMap == null) { 
                Hashtable t = new Hashtable(10, StringComparer.OrdinalIgnoreCase); 
                t.Add("a", typeof(HtmlAnchor));
                t.Add("button", typeof(HtmlButton)); 
                t.Add("form", typeof(HtmlForm));
                t.Add("head", typeof(HtmlHead));
                t.Add("img", typeof(HtmlImage));
                t.Add("textarea", typeof(HtmlTextArea)); 
                t.Add("select", typeof(HtmlSelect));
                t.Add("table", typeof(HtmlTable)); 
                t.Add("tr", typeof(HtmlTableRow)); 
                t.Add("td", typeof(HtmlTableCell));
                t.Add("th", typeof(HtmlTableCell)); 
                _tagMap = t;
            }

            if (_inputTypes == null) { 
                Hashtable t = new Hashtable(10, StringComparer.OrdinalIgnoreCase);
                t.Add("text", typeof(HtmlInputText)); 
                t.Add("password", typeof(HtmlInputPassword)); 
                t.Add("button", typeof(HtmlInputButton));
                t.Add("submit", typeof(HtmlInputSubmit)); 
                t.Add("reset", typeof(HtmlInputReset));
                t.Add("image", typeof(HtmlInputImage));
                t.Add("checkbox", typeof(HtmlInputCheckBox));
                t.Add("radio", typeof(HtmlInputRadioButton)); 
                t.Add("hidden", typeof(HtmlInputHidden));
                t.Add("file", typeof(HtmlInputFile)); 
                _inputTypes = t; 
            }
 
            if (StringUtil.EqualsIgnoreCase("input", tagName)) {
                string type = (string)attributeBag["type"];
                if (type == null)
                    type = "text"; 

                controlType = (Type)_inputTypes[type]; 
 
                if (controlType == null)
                    throw new HttpException( 
                        SR.GetString(SR.Invalid_type_for_input_tag, type));
            }
            else {
                controlType = (Type)_tagMap[tagName]; 
                if (controlType == null)
                    controlType = typeof(HtmlGenericControl); 
            } 

            return controlType; 
        }
    }
}
 
