//------------------------------------------------------------------------------ 
// <copyright file="Control.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Adapters { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.Globalization;
    using System.IO;
    using System.Security.Permissions;
    using System.Web.UI.WebControls; 
    using System.Web.Util;
 
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public abstract class PageAdapter : ControlAdapter {
        private IDictionary                 _radioButtonGroups = null;

 
        public virtual StringCollection CacheVaryByHeaders {
            get { 
                return null; 
            }
        } 


        public virtual StringCollection CacheVaryByParams {
            get { 
                return null;
            } 
        } 

 
        /// <devdoc>
        /// <para>Exposes the page ClientState string to the adapters</para>
        /// </devdoc>
        protected string ClientState { 
            get {
                if (Page != null) { 
                    return Page.ClientState; 
                }
                return null; 
            }
        }

 
        /// <devdoc>
        /// The id separator used for control UniqueID/ClientID. 
        /// </devdoc> 
        internal virtual char IdSeparator {
            get { 
                return Control.ID_SEPARATOR;
            }
        }
 
        internal String QueryString {
            get { 
                string queryString = Page.ClientQueryString; 
                if (Page.Request.Browser.RequiresUniqueFilePathSuffix) {
                    if (!String.IsNullOrEmpty(queryString)) { 
                        queryString = String.Concat(queryString, "&");
                    }
                    queryString = String.Concat(queryString, Page.UniqueFilePathSuffix);
                } 

                return queryString; 
            } 
        }
 

        /// <devdoc>
        /// <para>[To be supplied.]</para>
        /// </devdoc> 
        public virtual NameValueCollection DeterminePostBackMode() {
            Debug.Assert(Control != null); 
            if(Control != null) { 
                return Control.Page.DeterminePostBackMode();
            } 
            return null;
        }

 
        /// <devdoc>
        /// <para>[To be supplied.]</para> 
        /// </devdoc> 
        public virtual ICollection GetRadioButtonsByGroup(string groupName)  {
            if (_radioButtonGroups == null) { 
                return null;
            }

            return (ICollection)_radioButtonGroups[groupName]; 
        }
 
 
        protected internal virtual string GetPostBackFormReference(string formId) {
            return "document.forms['" + formId + "']"; 
        }


        public virtual PageStatePersister GetStatePersister() { 
            return new HiddenFieldPageStatePersister(Page);
        } 
 

        /// <devdoc> 
        /// <para>[To be supplied.]</para>
        /// </devdoc>
        public virtual void RegisterRadioButton(RadioButton radioButton)  {
 
            string groupName = radioButton.GroupName;
            if (String.IsNullOrEmpty(groupName)) 
                return; 

            ArrayList group = null; 

            if (_radioButtonGroups == null) {
                _radioButtonGroups = new ListDictionary();
            } 

            if (_radioButtonGroups.Contains(groupName))  { 
                group = (RadioButtonGroupList) _radioButtonGroups[groupName]; 
            }
            else  { 
                group = new RadioButtonGroupList();
                _radioButtonGroups[groupName] = group;
            }
 
            group.Add(radioButton);
        } 
 

        /// <devdoc> 
        /// <para>[To be supplied.]</para>
        /// </devdoc>
        public virtual void RenderBeginHyperlink(HtmlTextWriter writer, string targetUrl, bool encodeUrl, string softkeyLabel) {
            RenderBeginHyperlink(writer, targetUrl, encodeUrl, softkeyLabel, null /* accessKey */); 
        }
 
 
        /// <devdoc>
        /// <para>[To be supplied.]</para> 
        /// </devdoc>
        public virtual void RenderBeginHyperlink(HtmlTextWriter writer, string targetUrl, bool encodeUrl, string softkeyLabel, string accessKey) {
            String url;
 
            // Valid values are null, String.Empty, and single character strings
            if ((accessKey != null) && (accessKey.Length > 1)) { 
                throw new ArgumentOutOfRangeException("accessKey"); 
            }
 
            if (encodeUrl) {
                url = HttpUtility.HtmlAttributeEncode(targetUrl);
            }
            else { 
                url = targetUrl;
            } 
            writer.AddAttribute("href", url); 
            if (!String.IsNullOrEmpty(accessKey)) {
                writer.AddAttribute("accessKey", accessKey); 
            }
            writer.RenderBeginTag("a");
        }
 

        /// <devdoc> 
        /// <para>[To be supplied.]</para> 
        /// </devdoc>
        public virtual void RenderEndHyperlink(HtmlTextWriter writer) { 
            writer.WriteEndTag("a");
        }

 
        public virtual void RenderPostBackEvent(HtmlTextWriter writer, string target, string argument, string softkeyLabel, string text) {
            RenderPostBackEvent(writer, target, argument, softkeyLabel, text, null /*postUrl */, null /* accesskey */); 
        } 

 
        /// <devdoc>
        ///    <para>Renders a client widget corresponding to a postback event, for example a wml do or a post link.   Note that this
        ///     widget may not submit the form data, e.g. scriptless html where this renders a link. </para>
        /// </devdoc> 
        public virtual void RenderPostBackEvent(HtmlTextWriter writer, string target, string argument, string softkeyLabel, string text, string postUrl, string accessKey) {
            RenderPostBackEvent(writer, target, argument, softkeyLabel, text, postUrl, accessKey, false /* encode */); 
        } 

 
        /// <devdoc>
        ///    <para>Renders a client widget corresponding to a postback event, for example a wml do or a post link.   Note that this
        ///     widget may not submit the form data, e.g. scriptless html where this renders a link. </para>
        /// </devdoc> 
        protected void RenderPostBackEvent(HtmlTextWriter writer, string target, string argument, string softkeyLabel, string text, string postUrl, string accessKey, bool encode) {
            // Default: render postback event as scriptless anchor (works for all markups).  Override for specific markups. 
            string amp = encode ? "&amp;" : "&"; 

            bool isCrossPagePostBack = !String.IsNullOrEmpty(postUrl); 
            writer.WriteBeginTag("a");
            writer.Write(" href=\"");
            string url = null;
            if (!isCrossPagePostBack) { 
                if ((String)Browser["requiresAbsolutePostbackUrl"] == "true") {
                    url = Page.Response.ApplyAppPathModifier(Page.Request.CurrentExecutionFilePath); 
                } 
                else {
                    url = Page.RelativeFilePath; 
                }
            }
            else {
                url = postUrl; 
                Page.ContainsCrossPagePost = true;
            } 
 
            writer.WriteEncodedUrl(url);
            writer.Write("?"); 

            string clientState = ClientState;
            if (clientState != null)
            { 
                ICollection chunks = Page.DecomposeViewStateIntoChunks();
                // Default chunk count is 1 
                if (chunks.Count > 1) { 
                    writer.Write(Page.ViewStateFieldCountID + "=" + chunks.Count + amp);
                } 
                int count = 0;
                foreach (String state in chunks) {
                    writer.Write(Page.ViewStateFieldPrefixID);
                    if (count > 0) writer.Write(count.ToString(CultureInfo.CurrentCulture)); 
                    writer.Write("=" + HttpUtility.UrlEncode(state));
                    writer.Write(amp); 
                    ++count; 
                }
 
            }

            if (isCrossPagePostBack) {
                writer.Write(Page.previousPageID); 
                writer.Write("=" + Page.EncryptString(Page.Request.CurrentExecutionFilePath));
                writer.Write(amp); 
            } 

            writer.Write("__EVENTTARGET=" + HttpUtility.UrlEncode(target)); 
            writer.Write(amp);
            writer.Write("__EVENTARGUMENT=" + HttpUtility.UrlEncode(argument));
            //
 
            string queryStringText = QueryString;
            if (!String.IsNullOrEmpty(queryStringText)) { 
                writer.Write(amp); 
                writer.Write(queryStringText);
            } 

            writer.Write("\"");
            if (!String.IsNullOrEmpty(accessKey)) {
                writer.WriteAttribute("accessKey", accessKey); 
            }
            writer.Write(">"); 
            writer.Write(text); 
            writer.WriteEndTag("a");
        } 


        /// <devdoc>
        ///     Transforms text for the target device.  The default transformation is the identity transformation, 
        ///     which does not change the text.
        /// </devdoc> 
        public virtual string TransformText(string text) { 
            return text;
        } 
    }


    internal class RadioButtonGroupList : ArrayList { 
#if SHIPPINGADAPTERS
        private bool _autoPostBackRadioButtonsChecked; 
        private bool _containsAutoPostBackRadioButtons; 

        internal bool ContainsAutoPostBackRadioButtons { 
            get {
                if (_autoPostBackRadioButtonsChecked)
                    return _containsAutoPostBackRadioButtons;
 
                for (IEnumerator e = this.GetEnumerator(); e.MoveNext();) {
                    RadioButton radioButton = (RadioButton)e.Current; 
                    if (radioButton.AutoPostBack) { 
                        _containsAutoPostBackRadioButtons = true;
                        break; 
                    }
                }

                _autoPostBackRadioButtonsChecked = true; 
                return _containsAutoPostBackRadioButtons;
            } 
        } 
#endif
    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="Control.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Adapters { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.Globalization;
    using System.IO;
    using System.Security.Permissions;
    using System.Web.UI.WebControls; 
    using System.Web.Util;
 
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public abstract class PageAdapter : ControlAdapter {
        private IDictionary                 _radioButtonGroups = null;

 
        public virtual StringCollection CacheVaryByHeaders {
            get { 
                return null; 
            }
        } 


        public virtual StringCollection CacheVaryByParams {
            get { 
                return null;
            } 
        } 

 
        /// <devdoc>
        /// <para>Exposes the page ClientState string to the adapters</para>
        /// </devdoc>
        protected string ClientState { 
            get {
                if (Page != null) { 
                    return Page.ClientState; 
                }
                return null; 
            }
        }

 
        /// <devdoc>
        /// The id separator used for control UniqueID/ClientID. 
        /// </devdoc> 
        internal virtual char IdSeparator {
            get { 
                return Control.ID_SEPARATOR;
            }
        }
 
        internal String QueryString {
            get { 
                string queryString = Page.ClientQueryString; 
                if (Page.Request.Browser.RequiresUniqueFilePathSuffix) {
                    if (!String.IsNullOrEmpty(queryString)) { 
                        queryString = String.Concat(queryString, "&");
                    }
                    queryString = String.Concat(queryString, Page.UniqueFilePathSuffix);
                } 

                return queryString; 
            } 
        }
 

        /// <devdoc>
        /// <para>[To be supplied.]</para>
        /// </devdoc> 
        public virtual NameValueCollection DeterminePostBackMode() {
            Debug.Assert(Control != null); 
            if(Control != null) { 
                return Control.Page.DeterminePostBackMode();
            } 
            return null;
        }

 
        /// <devdoc>
        /// <para>[To be supplied.]</para> 
        /// </devdoc> 
        public virtual ICollection GetRadioButtonsByGroup(string groupName)  {
            if (_radioButtonGroups == null) { 
                return null;
            }

            return (ICollection)_radioButtonGroups[groupName]; 
        }
 
 
        protected internal virtual string GetPostBackFormReference(string formId) {
            return "document.forms['" + formId + "']"; 
        }


        public virtual PageStatePersister GetStatePersister() { 
            return new HiddenFieldPageStatePersister(Page);
        } 
 

        /// <devdoc> 
        /// <para>[To be supplied.]</para>
        /// </devdoc>
        public virtual void RegisterRadioButton(RadioButton radioButton)  {
 
            string groupName = radioButton.GroupName;
            if (String.IsNullOrEmpty(groupName)) 
                return; 

            ArrayList group = null; 

            if (_radioButtonGroups == null) {
                _radioButtonGroups = new ListDictionary();
            } 

            if (_radioButtonGroups.Contains(groupName))  { 
                group = (RadioButtonGroupList) _radioButtonGroups[groupName]; 
            }
            else  { 
                group = new RadioButtonGroupList();
                _radioButtonGroups[groupName] = group;
            }
 
            group.Add(radioButton);
        } 
 

        /// <devdoc> 
        /// <para>[To be supplied.]</para>
        /// </devdoc>
        public virtual void RenderBeginHyperlink(HtmlTextWriter writer, string targetUrl, bool encodeUrl, string softkeyLabel) {
            RenderBeginHyperlink(writer, targetUrl, encodeUrl, softkeyLabel, null /* accessKey */); 
        }
 
 
        /// <devdoc>
        /// <para>[To be supplied.]</para> 
        /// </devdoc>
        public virtual void RenderBeginHyperlink(HtmlTextWriter writer, string targetUrl, bool encodeUrl, string softkeyLabel, string accessKey) {
            String url;
 
            // Valid values are null, String.Empty, and single character strings
            if ((accessKey != null) && (accessKey.Length > 1)) { 
                throw new ArgumentOutOfRangeException("accessKey"); 
            }
 
            if (encodeUrl) {
                url = HttpUtility.HtmlAttributeEncode(targetUrl);
            }
            else { 
                url = targetUrl;
            } 
            writer.AddAttribute("href", url); 
            if (!String.IsNullOrEmpty(accessKey)) {
                writer.AddAttribute("accessKey", accessKey); 
            }
            writer.RenderBeginTag("a");
        }
 

        /// <devdoc> 
        /// <para>[To be supplied.]</para> 
        /// </devdoc>
        public virtual void RenderEndHyperlink(HtmlTextWriter writer) { 
            writer.WriteEndTag("a");
        }

 
        public virtual void RenderPostBackEvent(HtmlTextWriter writer, string target, string argument, string softkeyLabel, string text) {
            RenderPostBackEvent(writer, target, argument, softkeyLabel, text, null /*postUrl */, null /* accesskey */); 
        } 

 
        /// <devdoc>
        ///    <para>Renders a client widget corresponding to a postback event, for example a wml do or a post link.   Note that this
        ///     widget may not submit the form data, e.g. scriptless html where this renders a link. </para>
        /// </devdoc> 
        public virtual void RenderPostBackEvent(HtmlTextWriter writer, string target, string argument, string softkeyLabel, string text, string postUrl, string accessKey) {
            RenderPostBackEvent(writer, target, argument, softkeyLabel, text, postUrl, accessKey, false /* encode */); 
        } 

 
        /// <devdoc>
        ///    <para>Renders a client widget corresponding to a postback event, for example a wml do or a post link.   Note that this
        ///     widget may not submit the form data, e.g. scriptless html where this renders a link. </para>
        /// </devdoc> 
        protected void RenderPostBackEvent(HtmlTextWriter writer, string target, string argument, string softkeyLabel, string text, string postUrl, string accessKey, bool encode) {
            // Default: render postback event as scriptless anchor (works for all markups).  Override for specific markups. 
            string amp = encode ? "&amp;" : "&"; 

            bool isCrossPagePostBack = !String.IsNullOrEmpty(postUrl); 
            writer.WriteBeginTag("a");
            writer.Write(" href=\"");
            string url = null;
            if (!isCrossPagePostBack) { 
                if ((String)Browser["requiresAbsolutePostbackUrl"] == "true") {
                    url = Page.Response.ApplyAppPathModifier(Page.Request.CurrentExecutionFilePath); 
                } 
                else {
                    url = Page.RelativeFilePath; 
                }
            }
            else {
                url = postUrl; 
                Page.ContainsCrossPagePost = true;
            } 
 
            writer.WriteEncodedUrl(url);
            writer.Write("?"); 

            string clientState = ClientState;
            if (clientState != null)
            { 
                ICollection chunks = Page.DecomposeViewStateIntoChunks();
                // Default chunk count is 1 
                if (chunks.Count > 1) { 
                    writer.Write(Page.ViewStateFieldCountID + "=" + chunks.Count + amp);
                } 
                int count = 0;
                foreach (String state in chunks) {
                    writer.Write(Page.ViewStateFieldPrefixID);
                    if (count > 0) writer.Write(count.ToString(CultureInfo.CurrentCulture)); 
                    writer.Write("=" + HttpUtility.UrlEncode(state));
                    writer.Write(amp); 
                    ++count; 
                }
 
            }

            if (isCrossPagePostBack) {
                writer.Write(Page.previousPageID); 
                writer.Write("=" + Page.EncryptString(Page.Request.CurrentExecutionFilePath));
                writer.Write(amp); 
            } 

            writer.Write("__EVENTTARGET=" + HttpUtility.UrlEncode(target)); 
            writer.Write(amp);
            writer.Write("__EVENTARGUMENT=" + HttpUtility.UrlEncode(argument));
            //
 
            string queryStringText = QueryString;
            if (!String.IsNullOrEmpty(queryStringText)) { 
                writer.Write(amp); 
                writer.Write(queryStringText);
            } 

            writer.Write("\"");
            if (!String.IsNullOrEmpty(accessKey)) {
                writer.WriteAttribute("accessKey", accessKey); 
            }
            writer.Write(">"); 
            writer.Write(text); 
            writer.WriteEndTag("a");
        } 


        /// <devdoc>
        ///     Transforms text for the target device.  The default transformation is the identity transformation, 
        ///     which does not change the text.
        /// </devdoc> 
        public virtual string TransformText(string text) { 
            return text;
        } 
    }


    internal class RadioButtonGroupList : ArrayList { 
#if SHIPPINGADAPTERS
        private bool _autoPostBackRadioButtonsChecked; 
        private bool _containsAutoPostBackRadioButtons; 

        internal bool ContainsAutoPostBackRadioButtons { 
            get {
                if (_autoPostBackRadioButtonsChecked)
                    return _containsAutoPostBackRadioButtons;
 
                for (IEnumerator e = this.GetEnumerator(); e.MoveNext();) {
                    RadioButton radioButton = (RadioButton)e.Current; 
                    if (radioButton.AutoPostBack) { 
                        _containsAutoPostBackRadioButtons = true;
                        break; 
                    }
                }

                _autoPostBackRadioButtonsChecked = true; 
                return _containsAutoPostBackRadioButtons;
            } 
        } 
#endif
    } 
}
