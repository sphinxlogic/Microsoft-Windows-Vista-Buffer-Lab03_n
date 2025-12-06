//------------------------------------------------------------------------------ 
// <copyright file="ValidatorCompatibilityHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System.Globalization;
    using System.Reflection; 
    using System.Web.Util;

    // Needed to support Validators in AJAX 1.0 (Windows OS Bugs 2015831)
    internal static class ValidatorCompatibilityHelper { 
        public static void RegisterArrayDeclaration(Control control, string arrayName, string arrayValue) {
            Type scriptManagerType = control.Page.ScriptManagerType; 
            Debug.Assert(scriptManagerType != null); 

            scriptManagerType.InvokeMember("RegisterArrayDeclaration", 
                                           BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
                                           null, /*binder*/
                                           null, /*target*/
                                           new object[] {control, arrayName, arrayValue}); 
        }
 
        public static void RegisterClientScriptResource(Control control, Type type, string resourceName) { 
            Type scriptManagerType = control.Page.ScriptManagerType;
            Debug.Assert(scriptManagerType != null); 

            scriptManagerType.InvokeMember("RegisterClientScriptResource",
                                           BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
                                           null, /*binder*/ 
                                           null, /*target*/
                                           new object[] {control, type, resourceName}); 
        } 

        public static void RegisterExpandoAttribute(Control control, string controlId, string attributeName, string attributeValue, bool encode) { 
            Type scriptManagerType = control.Page.ScriptManagerType;
            Debug.Assert(scriptManagerType != null);

            scriptManagerType.InvokeMember("RegisterExpandoAttribute", 
                                           BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
                                           null, /*binder*/ 
                                           null, /*target*/ 
                                           new object[] {control, controlId, attributeName, attributeValue, encode});
        } 

        public static void RegisterOnSubmitStatement(Control control, Type type, string key, string script) {
            Type scriptManagerType = control.Page.ScriptManagerType;
            Debug.Assert(scriptManagerType != null); 

            scriptManagerType.InvokeMember("RegisterOnSubmitStatement", 
                                           BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, 
                                           null, /*binder*/
                                           null, /*target*/ 
                                           new object[] {control, type, key, script});
        }

        public static void RegisterStartupScript(Control control, Type type, string key, string script, bool addScriptTags) { 
            Type scriptManagerType = control.Page.ScriptManagerType;
            Debug.Assert(scriptManagerType != null); 
 
            scriptManagerType.InvokeMember("RegisterStartupScript",
                                           BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, 
                                           null, /*binder*/
                                           null, /*target*/
                                           new object[] {control, type, key, script, addScriptTags});
        } 
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="ValidatorCompatibilityHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System.Globalization;
    using System.Reflection; 
    using System.Web.Util;

    // Needed to support Validators in AJAX 1.0 (Windows OS Bugs 2015831)
    internal static class ValidatorCompatibilityHelper { 
        public static void RegisterArrayDeclaration(Control control, string arrayName, string arrayValue) {
            Type scriptManagerType = control.Page.ScriptManagerType; 
            Debug.Assert(scriptManagerType != null); 

            scriptManagerType.InvokeMember("RegisterArrayDeclaration", 
                                           BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
                                           null, /*binder*/
                                           null, /*target*/
                                           new object[] {control, arrayName, arrayValue}); 
        }
 
        public static void RegisterClientScriptResource(Control control, Type type, string resourceName) { 
            Type scriptManagerType = control.Page.ScriptManagerType;
            Debug.Assert(scriptManagerType != null); 

            scriptManagerType.InvokeMember("RegisterClientScriptResource",
                                           BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
                                           null, /*binder*/ 
                                           null, /*target*/
                                           new object[] {control, type, resourceName}); 
        } 

        public static void RegisterExpandoAttribute(Control control, string controlId, string attributeName, string attributeValue, bool encode) { 
            Type scriptManagerType = control.Page.ScriptManagerType;
            Debug.Assert(scriptManagerType != null);

            scriptManagerType.InvokeMember("RegisterExpandoAttribute", 
                                           BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
                                           null, /*binder*/ 
                                           null, /*target*/ 
                                           new object[] {control, controlId, attributeName, attributeValue, encode});
        } 

        public static void RegisterOnSubmitStatement(Control control, Type type, string key, string script) {
            Type scriptManagerType = control.Page.ScriptManagerType;
            Debug.Assert(scriptManagerType != null); 

            scriptManagerType.InvokeMember("RegisterOnSubmitStatement", 
                                           BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, 
                                           null, /*binder*/
                                           null, /*target*/ 
                                           new object[] {control, type, key, script});
        }

        public static void RegisterStartupScript(Control control, Type type, string key, string script, bool addScriptTags) { 
            Type scriptManagerType = control.Page.ScriptManagerType;
            Debug.Assert(scriptManagerType != null); 
 
            scriptManagerType.InvokeMember("RegisterStartupScript",
                                           BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, 
                                           null, /*binder*/
                                           null, /*target*/
                                           new object[] {control, type, key, script, addScriptTags});
        } 
    }
} 
