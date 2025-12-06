//------------------------------------------------------------------------------ 
// <copyright file="ClientScriptManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI { 
    using System; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Collections;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Text; 
    using System.Web.Handlers;
    using System.Web.UI.WebControls; 
    using System.Web.Util; 
    using ExceptionUtil=System.Web.Util.ExceptionUtil;
    using WebUtil = System.Web.Util; 
    using System.Security.Permissions;

    // The various types of client API's that can be registered
    internal enum ClientAPIRegisterType { 
        WebFormsScript,
        PostBackScript, 
        FocusScript, 
        ClientScriptBlocks,
        ClientScriptBlocksWithoutTags, 
        ClientStartupScripts,
        ClientStartupScriptsWithoutTags,
        OnSubmitStatement,
        ArrayDeclaration, 
        HiddenField,
        ExpandoAttribute, 
        EventValidation, 
    }
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class ClientScriptManager {
        private const string IncludeScriptBegin = @"
<script src="""; 
        private const string IncludeScriptEnd = @""" type=""text/javascript""></script>";
        internal const string ClientScriptStart = "\r\n<script type=\"text/javascript\">\r\n//<![CDATA[\r\n"; 
        internal const string ClientScriptStartLegacy = "\r\n<script type=\"text/javascript\">\r\n<!--\r\n"; 
        internal const string ClientScriptEnd = "//]]>\r\n</script>\r\n";
        internal const string ClientScriptEndLegacy = "// -->\r\n</script>\r\n"; 
        internal const string JscriptPrefix = "javascript:";

        private const string _callbackFunctionName = "WebForm_DoCallback";
        private const string _postbackOptionsFunctionName = "WebForm_DoPostBackWithOptions"; 
        private const string _postBackFunctionName = "__doPostBack";
        private const string PageCallbackScriptKey = "PageCallbackScript"; 
 
        private ListDictionary _registeredClientScriptBlocks;
        private ArrayList _clientScriptBlocks; 
        private bool _clientScriptBlocksInScriptTag;

        private ListDictionary _registeredClientStartupScripts;
        private ArrayList _clientStartupScripts; 
        private bool _clientStartupScriptInScriptTag;
        private bool _eventValidationFieldLoaded; 
 
        private ListDictionary _registeredOnSubmitStatements;
 
        private IDictionary _registeredArrayDeclares;
        private IDictionary _registeredHiddenFields;
        private ListDictionary _registeredControlsWithExpandoAttributes;
 
        private ArrayList _validEventReferences;
        private HybridDictionary _clientPostBackValidatedEventTable; 
 
        private Page _owner;
 
        internal ClientScriptManager(Page owner) {
            _owner = owner;
        }
 
        internal bool HasRegisteredHiddenFields {
            get { 
                return (_registeredHiddenFields != null && _registeredHiddenFields.Count > 0); 
            }
        } 

        internal bool HasSubmitStatements {
            get {
                return (_registeredOnSubmitStatements != null && _registeredOnSubmitStatements.Count > 0); 
            }
        } 
 
        private static int ComputeHashKey(String uniqueId, String argument) {
            if (String.IsNullOrEmpty(argument)) { 
                return StringUtil.GetStringHashCode(uniqueId);
            }

            return StringUtil.GetStringHashCode(uniqueId) ^ StringUtil.GetStringHashCode(argument); 
        }
 
        internal string GetEventValidationFieldValue() { 
            if ((_validEventReferences == null) || (_validEventReferences.Count == 0)) {
                return String.Empty; 
            }
            IStateFormatter formatter = _owner.CreateStateFormatter();
            return formatter.Serialize(_validEventReferences);
        } 

        public void RegisterForEventValidation(PostBackOptions options) { 
            RegisterForEventValidation(options.TargetControl.UniqueID, options.Argument); 
        }
 
        public void RegisterForEventValidation(string uniqueId) {
            RegisterForEventValidation(uniqueId, String.Empty);
        }
 
        public void RegisterForEventValidation(string uniqueId, string argument) {
            if (!_owner.EnableEventValidation || _owner.DesignMode) { 
                return; 
            }
 
            // VSWhidbey 497632. Ignore if uniqueID is empty since the postback won't be valid anyway.
            if (String.IsNullOrEmpty(uniqueId)) {
                return;
            } 

            if ((_owner.ControlState < ControlState.PreRendered) && (!_owner.IsCallback)) { 
                throw new InvalidOperationException( 
                    SR.GetString(SR.ClientScriptManager_RegisterForEventValidation_Too_Early));
            } 

#if DEBUGEVENTVALIDATION
            string key = uniqueId + "@" + argument;
#else 
            int key = ComputeHashKey(uniqueId, argument);
#endif //DEBUGEVENTVALIDATION 
 
            string stateString = _owner.ClientState;
            if (stateString == null) { 
                stateString = String.Empty;
            }

            if (_validEventReferences == null) { 
                if (_owner.IsCallback) {
                    EnsureEventValidationFieldLoaded(); 
                    if (_validEventReferences == null) { 
                        _validEventReferences = new ArrayList();
                    } 
                }
                else {
                    _validEventReferences = new ArrayList();
                    _validEventReferences.Add( 
                        StringUtil.GetStringHashCode(stateString));
                } 
            } 

#if DEBUGEVENTVALIDATION 
            Debug.Assert(!_validEventReferences.Contains(key));
#endif //DEBUGEVENTVALIDATION

            _validEventReferences.Add(key); 

            // If there are any partial caching controls on the stack, forward the call to them 
            if (_owner.PartialCachingControlStack != null) { 
                foreach (BasePartialCachingControl c in _owner.PartialCachingControlStack) {
                    c.RegisterForEventValidation(uniqueId, argument); 
                }
            }
        }
 
        internal void SaveEventValidationField() {
            string fieldValue = GetEventValidationFieldValue(); 
            if (!String.IsNullOrEmpty(fieldValue)) { 
                RegisterHiddenField(Page.EventValidationPrefixID, fieldValue);
            } 
        }

        private void EnsureEventValidationFieldLoaded() {
            if (_eventValidationFieldLoaded) { 
                return;
            } 
 
            _eventValidationFieldLoaded = true;
 
            string unsafeField = null;
            if (_owner.RequestValueCollection != null) {
                unsafeField = _owner.RequestValueCollection[Page.EventValidationPrefixID];
            } 

            if (String.IsNullOrEmpty(unsafeField)) { 
                return; 
            }
 
            IStateFormatter formatter = _owner.CreateStateFormatter();
            ArrayList validatedClientEvents = null;

            try { 
                validatedClientEvents = formatter.Deserialize(unsafeField) as ArrayList;
            } 
            catch(Exception ex) { 
                ViewStateException.ThrowViewStateError(ex, unsafeField);
            } 

            if (validatedClientEvents == null || validatedClientEvents.Count < 1) {
                return;
            } 

            Debug.Assert(_clientPostBackValidatedEventTable == null); 
            int viewStateHashCode = (int)validatedClientEvents[0]; 

            string viewStateString = _owner.RequestViewStateString; 

            if (viewStateHashCode != StringUtil.GetStringHashCode(viewStateString)) {
                ViewStateException.ThrowViewStateError(null, unsafeField);
            } 

            _clientPostBackValidatedEventTable = new HybridDictionary(validatedClientEvents.Count - 1, true); 
 
            // Ignore the first item in the arrayList, which is the controlstate
            for (int index = 1; index < validatedClientEvents.Count; index++) { 
#if DEBUGEVENTVALIDATION
                string hashKey = (string)validatedClientEvents[index];
#else
                int hashKey = (int)validatedClientEvents[index]; 
#endif //DEBUGEVENTVALIDATION
                _clientPostBackValidatedEventTable[hashKey] = null; 
            } 

            if (_owner.IsCallback) { 
                _validEventReferences = validatedClientEvents;
            }
        }
 
        public void ValidateEvent(string uniqueId) {
            ValidateEvent(uniqueId, String.Empty); 
        } 

        public void ValidateEvent(string uniqueId, string argument) { 
            if (!_owner.EnableEventValidation) {
                return;
            }
 
            if (String.IsNullOrEmpty(uniqueId)) {
                throw new ArgumentException(SR.GetString(SR.Parameter_NullOrEmpty, "uniqueId"), "uniqueId"); 
            } 

            EnsureEventValidationFieldLoaded(); 

            if (_clientPostBackValidatedEventTable == null) {
                throw new ArgumentException(SR.GetString(SR.ClientScriptManager_InvalidPostBackArgument));
            } 

#if DEBUGEVENTVALIDATION 
            String hashCode = uniqueId + "@" + argument; 
#else
            int hashCode = ComputeHashKey(uniqueId, argument); 
#endif //DEBUGEVENTVALIDATION

            if (!_clientPostBackValidatedEventTable.Contains(hashCode)) {
                throw new ArgumentException(SR.GetString(SR.ClientScriptManager_InvalidPostBackArgument)); 
            }
        } 
 
        internal void ClearHiddenFields() {
            _registeredHiddenFields = null; 
        }

        internal static ScriptKey CreateScriptKey(Type type, string key) {
            return new ScriptKey(type, key); 
        }
 
        internal static ScriptKey CreateScriptIncludeKey(Type type, string key) { 
            return new ScriptKey(type, key, true);
        } 

        /// <devdoc>
        ///   Enables controls to obtain client-side script function that will cause
        ///   (when invoked) an out-of-band callback to the server 
        /// </devdoc>
        public string GetCallbackEventReference(Control control, string argument, string clientCallback, string context) { 
            return GetCallbackEventReference(control, argument, clientCallback, context, false); 
        }
 
        public string GetCallbackEventReference(Control control, string argument, string clientCallback, string context, bool useAsync) {
            return GetCallbackEventReference(control, argument, clientCallback, context, null, useAsync);
        }
 
        /// <devdoc>
        ///   Enables controls to obtain client-side script function that will cause 
        ///   (when invoked) an out-of-band callback to the server and allows the user to specify a client-side error callback 
        /// </devdoc>
        public string GetCallbackEventReference(Control control, string argument, string clientCallback, string context, string clientErrorCallback, bool useAsync) { 
            if (control == null) {
                throw new ArgumentNullException("control");
            }
            if (!(control is ICallbackEventHandler)) { 
                throw new InvalidOperationException(SR.GetString(SR.Page_CallBackTargetInvalid, control.UniqueID));
            } 
            return GetCallbackEventReference("'" + control.UniqueID + "'", argument, clientCallback, context, clientErrorCallback, useAsync); 
        }
 
        /// <devdoc>
        ///   Enables controls to obtain client-side script function that will cause
        ///   (when invoked) an out-of-band callback to the server and allows the user to specify a client-side error callback
        /// </devdoc> 
        public string GetCallbackEventReference(string target, string argument, string clientCallback, string context, string clientErrorCallback, bool useAsync) {
            _owner.RegisterWebFormsScript(); 
            if (_owner.ClientSupportsJavaScript && (_owner.RequestInternal != null) && _owner.RequestInternal.Browser.SupportsCallback) { 
                RegisterStartupScript(typeof(Page), PageCallbackScriptKey, (((_owner.RequestInternal != null) &&
                        (String.Equals(_owner.RequestInternal.Url.Scheme, "https", StringComparison.OrdinalIgnoreCase))) ? 
                            @"
var callBackFrameUrl='" + Util.QuoteJScriptString(GetWebResourceUrl(typeof(Page), "SmartNav.htm"), false) + @"';
WebForm_InitCallback();" :
                            @" 
WebForm_InitCallback();"), true);
            } 
            if (argument == null) { 
                argument = "null";
            } 
            else if (argument.Length == 0) {
                argument = "\"\"";
            }
            if (context == null) { 
                context = "null";
            } 
            else if (context.Length == 0) { 
                context = "\"\"";
            } 
            return _callbackFunctionName +
                   "(" +
                   target +
                   "," + 
                   argument +
                   "," + 
                   clientCallback + 
                   "," +
                   context + 
                   "," +
                   ((clientErrorCallback == null) ? "null" : clientErrorCallback) +
                   "," +
                   (useAsync ? "true" : "false") + 
                   ")";
        } 
 
        public string GetPostBackClientHyperlink(Control control, string argument) {
            // We're using escapePercent=true here and false in Page 
            // because true in Page would be a breaking change:
            // People may already be encoding percent characters before calling this,
            // and we may double encode it.
            // Our own classes and new code should almost always use the override with escapePercent=true. 
            return GetPostBackClientHyperlink(control, argument, true, false);
        } 
 
        public string GetPostBackClientHyperlink(Control control, string argument, bool registerForEventValidation) {
            // We're using escapePercent=true here and false in Page 
            // because true in Page would be a breaking change:
            // People may already be encoding percent characters before calling this,
            // and we may double encode it.
            // Our own classes and new code should almost always use the override with escapePercent=true. 
            return GetPostBackClientHyperlink(control, argument, true, registerForEventValidation);
        } 
 
        /// <devdoc>
        ///    <para>This returs a string that can be put in client event to post back to the named control</para> 
        /// </devdoc>
        internal string GetPostBackClientHyperlink(Control control, string argument, bool escapePercent, bool registerForEventValidation) {
            // Hyperlinks always need the language prefix
            // If used in a hyperlink, the event argument needs to be escaped for % characters 
            // which will otherwise be interpreted as escape sequences (VSWhidbey 421874)
            return JscriptPrefix + GetPostBackEventReference(control, argument, escapePercent, registerForEventValidation); 
        } 

        public string GetPostBackEventReference(Control control, string argument) { 
            return GetPostBackEventReference(control, argument, false, false);
        }

        public string GetPostBackEventReference(Control control, string argument, bool registerForEventValidation) { 
            return GetPostBackEventReference(control, argument, false, registerForEventValidation);
        } 
 
        /*
         * Enables controls to obtain client-side script function that will cause 
         * (when invoked) a server post-back to the form.
         * argument: Parameter that will be passed to control on server
         */
        /// <devdoc> 
        ///    <para>Passes a parameter to the control that will do the postback processing on the
        ///       server.</para> 
        /// </devdoc> 
        private string GetPostBackEventReference(Control control, string argument, bool forUrl, bool registerForEventValidation) {
            if (control == null) { 
                throw new ArgumentNullException("control");
            }

            _owner.RegisterPostBackScript(); 

            string controlID = control.UniqueID; 
 
            if (registerForEventValidation) {
                RegisterForEventValidation(controlID, argument); 
            }

            // VSWhidbey 475945
            if (control.EnableLegacyRendering && _owner.IsInOnFormRender && 
                controlID != null && controlID.IndexOf(Control.LEGACY_ID_SEPARATOR) >= 0) {
 
                controlID = controlID.Replace(Control.LEGACY_ID_SEPARATOR, Control.ID_SEPARATOR); 
            }
 
            // Split into 2 calls to String.Concat to improve performance.
            
            string postBackEventReference = _postBackFunctionName + "('" + controlID + "','";
            // The argument needs to be quoted, in case in contains characters that 
            // can't be used in JScript strings (ASURT 71818).
            postBackEventReference += Util.QuoteJScriptString(argument, forUrl) + "')"; 
 
            return postBackEventReference;
        } 

        /// <devdoc>
        ///    <para>Passes a parameter to the control that will do the postback processing on the
        ///       server.</para> 
        /// </devdoc>
        public string GetPostBackEventReference(PostBackOptions options) { 
            return GetPostBackEventReference(options, false); 
        }
 
        public string GetPostBackEventReference(PostBackOptions options, bool registerForEventValidation) {
            if (options == null) {
                throw new ArgumentNullException("options");
            } 

            if (registerForEventValidation) { 
                RegisterForEventValidation(options); 
            }
 
            StringBuilder builder = new StringBuilder();
            bool shouldRenderPostBackReferenceString = false;

            if (options.RequiresJavaScriptProtocol) { 
                builder.Append(JscriptPrefix);
            } 
 
            if (options.AutoPostBack) {
                builder.Append("setTimeout('"); 
            }

            // Use the old __doPostBack method if not using other postback features.
            if (!options.PerformValidation && !options.TrackFocus && options.ClientSubmit && 
                string.IsNullOrEmpty(options.ActionUrl)) {
                string postbackRef = GetPostBackEventReference(options.TargetControl, options.Argument); 
 
                // Need to quote the string if auto posting back
                if (options.AutoPostBack) { 
                    builder.Append(Util.QuoteJScriptString(postbackRef));
                    builder.Append("', 0)");
                }
                else { 
                    builder.Append(postbackRef);
                } 
 
                return builder.ToString();
            } 

            builder.Append(_postbackOptionsFunctionName);
            builder.Append("(new WebForm_PostBackOptions(\"");
            builder.Append(options.TargetControl.UniqueID); 
            builder.Append("\", ");
 
            if (String.IsNullOrEmpty(options.Argument)) { 
                builder.Append("\"\", ");
            } 
            else {
                builder.Append("\"");
                builder.Append(Util.QuoteJScriptString(options.Argument));
                builder.Append("\", "); 
            }
 
            if (options.PerformValidation) { 
                shouldRenderPostBackReferenceString = true;
                builder.Append("true, "); 
            }
            else {
                builder.Append("false, ");
            } 

            if (options.ValidationGroup!= null && options.ValidationGroup.Length > 0) { 
                shouldRenderPostBackReferenceString = true; 

                builder.Append("\""); 
                builder.Append(options.ValidationGroup);
                builder.Append("\", ");
            }
            else { 
                builder.Append("\"\", ");
            } 
 
            if (options.ActionUrl != null && options.ActionUrl.Length > 0) {
                shouldRenderPostBackReferenceString = true; 
                _owner.ContainsCrossPagePost = true;

                builder.Append("\"");
                builder.Append(Util.QuoteJScriptString(options.ActionUrl)); 
                builder.Append("\", ");
            } 
            else { 
                builder.Append("\"\", ");
            } 

            if (options.TrackFocus) {
                _owner.RegisterFocusScript();
                shouldRenderPostBackReferenceString = true; 
                builder.Append("true, ");
            } 
            else { 
                builder.Append("false, ");
            } 

            if (options.ClientSubmit) {
                shouldRenderPostBackReferenceString = true;
                _owner.RegisterPostBackScript(); 

                builder.Append("true))"); 
            } 
            else {
                builder.Append("false))"); 
            }

            if (options.AutoPostBack) {
                builder.Append("', 0)"); 
            }
 
            string reference =  null; 
            if (shouldRenderPostBackReferenceString) {
                reference = builder.ToString(); 
                _owner.RegisterWebFormsScript();
            }

            return reference; 
        }
 
        /// <devdoc> 
        /// Gets a URL resource reference to a client-side resource
        /// </devdoc> 
        public string GetWebResourceUrl(Type type, string resourceName) {
            return GetWebResourceUrl(_owner, type, resourceName, false);
        }
 
        internal static string GetWebResourceUrl(Page owner, Type type, string resourceName, bool htmlEncoded) {
            if (type == null) { 
                throw new ArgumentNullException("type"); 
            }
 
            if (String.IsNullOrEmpty(resourceName)) {
                throw new ArgumentNullException("resourceName");
            }
 
            if (owner != null && owner.DesignMode) {
                ISite site = ((IComponent)owner).Site; 
                if (site != null) { 
                    IResourceUrlGenerator urlGenerator = site.GetService(typeof(IResourceUrlGenerator)) as IResourceUrlGenerator;
                    if (urlGenerator != null) { 
                        return urlGenerator.GetResourceUrl(type, resourceName);
                    }
                }
 
                return resourceName;
            } 
            else { 
                return AssemblyResourceLoader.GetWebResourceUrl(type, resourceName, htmlEncoded);
            } 
        }

        /// <devdoc>
        ///    <para>Determines if the client script block is registered with the page.</para> 
        /// </devdoc>
        public bool IsClientScriptBlockRegistered(string key) { 
            return IsClientScriptBlockRegistered(typeof(Page), key); 
        }
 
        /// <devdoc>
        ///    <para>Determines if the client script block is registered with the page.</para>
        /// </devdoc>
        public bool IsClientScriptBlockRegistered(Type type, string key) { 
            if (type == null) {
                throw new ArgumentNullException("type"); 
            } 

            ScriptKey scriptKey = CreateScriptKey(type, key); 
            return (_registeredClientScriptBlocks != null
                   && (_registeredClientScriptBlocks[scriptKey] != null));
        }
 
        /// <devdoc>
        ///    <para>Determines if the onsubmit script is registered with the page.</para> 
        /// </devdoc> 
        public bool IsClientScriptIncludeRegistered(string key) {
            return IsClientScriptIncludeRegistered(typeof(Page), key); 
        }

        /// <devdoc>
        ///    <para>Determines if the onsubmit script  is registered with the page.</para> 
        /// </devdoc>
        public bool IsClientScriptIncludeRegistered(Type type, string key) { 
            if (type == null) { 
                throw new ArgumentNullException("type");
            } 

            return(_registeredClientScriptBlocks != null
                   && (_registeredClientScriptBlocks[CreateScriptIncludeKey(type, key)] != null));
        } 

        /// <devdoc> 
        ///    <para>Determines if the client startup script is registered with the 
        ///       page.</para>
        /// </devdoc> 
        public bool IsStartupScriptRegistered(string key) {
            return IsStartupScriptRegistered(typeof(Page), key);
        }
 
        /// <devdoc>
        ///    <para>Determines if the client startup script is registered with the 
        ///       page.</para> 
        /// </devdoc>
        public bool IsStartupScriptRegistered(Type type, string key) { 
            if (type == null) {
                throw new ArgumentNullException("type");
            }
 
            return(_registeredClientStartupScripts != null
                   && (_registeredClientStartupScripts[CreateScriptKey(type, key)] != null)); 
        } 

        /// <devdoc> 
        ///    <para>Determines if the onsubmit script is registered with the page.</para>
        /// </devdoc>
        public bool IsOnSubmitStatementRegistered(string key) {
            return IsOnSubmitStatementRegistered(typeof(Page), key); 
        }
 
        /// <devdoc> 
        ///    <para>Determines if the onsubmit script  is registered with the page.</para>
        /// </devdoc> 
        public bool IsOnSubmitStatementRegistered(Type type, string key) {
            if (type == null) {
                throw new ArgumentNullException("type");
            } 

            return(_registeredOnSubmitStatements != null 
                   && (_registeredOnSubmitStatements[CreateScriptKey(type, key)] != null)); 
        }
 
        /// <devdoc>
        ///    <para>Declares a value that will be declared as a JavaScript array declaration
        ///       when the page renders. This can be used by script-based controls to declare
        ///       themselves within an array so that a client script library can work with 
        ///       all the controls of the same type.</para>
        /// </devdoc> 
        public void RegisterArrayDeclaration(string arrayName, string arrayValue) { 
            if (arrayName == null) {
                throw new ArgumentNullException("arrayName"); 
            }
            if (_registeredArrayDeclares == null) {
                _registeredArrayDeclares = new ListDictionary();
            } 
            if (!_registeredArrayDeclares.Contains(arrayName)) {
                _registeredArrayDeclares[arrayName] = new ArrayList(); 
            } 

            ArrayList elements = (ArrayList) _registeredArrayDeclares[arrayName]; 
            elements.Add(arrayValue);

            // If there are any partial caching controls on the stack, forward the call to them
            if (_owner.PartialCachingControlStack != null) { 
                foreach(BasePartialCachingControl c in _owner.PartialCachingControlStack) {
                    c.RegisterArrayDeclaration(arrayName, arrayValue); 
                } 
            }
        } 

        // RegisterArrayDeclaration implementation that supports partial rendering.
        internal void RegisterArrayDeclaration(Control control, string arrayName, string arrayValue) {
            IScriptManager scriptManager = _owner.ScriptManager; 
            if ((scriptManager != null) && scriptManager.SupportsPartialRendering) {
                scriptManager.RegisterArrayDeclaration(control, arrayName, arrayValue); 
            } 
            else {
                RegisterArrayDeclaration(arrayName, arrayValue); 
            }
        }

        public void RegisterExpandoAttribute(string controlId, string attributeName, string attributeValue) { 
            RegisterExpandoAttribute(controlId, attributeName, attributeValue, true);
        } 
 
        public void RegisterExpandoAttribute(string controlId, string attributeName, string attributeValue, bool encode) {
            // check paramters 
            WebUtil.StringUtil.CheckAndTrimString(controlId, "controlId");
            WebUtil.StringUtil.CheckAndTrimString(attributeName, "attributeName");

            ListDictionary expandoAttributes = null; 
            if (_registeredControlsWithExpandoAttributes == null) {
                _registeredControlsWithExpandoAttributes = new ListDictionary(StringComparer.Ordinal); 
            } 
            else {
                expandoAttributes = (ListDictionary) _registeredControlsWithExpandoAttributes[controlId]; 
            }

            if (expandoAttributes == null) {
                expandoAttributes = new ListDictionary(StringComparer.Ordinal); 
                _registeredControlsWithExpandoAttributes.Add(controlId, expandoAttributes);
            } 
 
            if (encode) {
                attributeValue = Util.QuoteJScriptString(attributeValue); 
            }

            expandoAttributes.Add(attributeName, attributeValue);
 
            // If there are any partial caching controls on the stack, forward the call to them
            if (_owner.PartialCachingControlStack != null) { 
                foreach(BasePartialCachingControl c in _owner.PartialCachingControlStack) { 
                    c.RegisterExpandoAttribute(controlId, attributeName, attributeValue);
                } 
            }
        }

        // RegisterExpandoAttribute implementation that supports partial rendering. 
        internal void RegisterExpandoAttribute(Control control, string controlId, string attributeName, string attributeValue, bool encode) {
            IScriptManager scriptManager = _owner.ScriptManager; 
            if ((scriptManager != null) && scriptManager.SupportsPartialRendering) { 
                scriptManager.RegisterExpandoAttribute(control, controlId, attributeName, attributeValue, encode);
            } 
            else {
                RegisterExpandoAttribute(controlId, attributeName, attributeValue, encode);
            }
        } 

        /// <devdoc> 
        ///    <para> 
        ///       Allows controls to automatically register a hidden field on the form. The
        ///       field will be emitted when the form control renders itself. 
        ///    </para>
        /// </devdoc>
        public void RegisterHiddenField(string hiddenFieldName,
                                        string hiddenFieldInitialValue) { 
            if (hiddenFieldName == null) {
                throw new ArgumentNullException("hiddenFieldName"); 
            } 
            if (_registeredHiddenFields == null)
                _registeredHiddenFields = new ListDictionary(); 

            if (!_registeredHiddenFields.Contains(hiddenFieldName))
                _registeredHiddenFields.Add(hiddenFieldName, hiddenFieldInitialValue);
 
            // If there are any partial caching controls on the stack, forward the call to them
            if (_owner.PartialCachingControlStack != null) { 
                foreach(BasePartialCachingControl c in _owner.PartialCachingControlStack) { 
                    c.RegisterHiddenField(hiddenFieldName, hiddenFieldInitialValue);
                } 
            }
        }

        // RegisterHiddenField implementation that supports partial rendering. 
        internal void RegisterHiddenField(Control control, string hiddenFieldName, string hiddenFieldValue) {
            IScriptManager scriptManager = _owner.ScriptManager; 
            if ((scriptManager != null) && scriptManager.SupportsPartialRendering) { 
                scriptManager.RegisterHiddenField(control, hiddenFieldName, hiddenFieldValue);
            } 
            else {
                RegisterHiddenField(hiddenFieldName, hiddenFieldValue);
            }
        } 

 
        /// <devdoc> 
        ///    Prevents controls from sending duplicate blocks of
        ///    client-side script to the client. Any script blocks with the same type and key 
        ///    values are considered duplicates.</para>
        /// </devdoc>
        public void RegisterClientScriptBlock(Type type, string key, string script) {
            RegisterClientScriptBlock(type, key, script, false); 
        }
 
        /// <devdoc> 
        ///    Prevents controls from sending duplicate blocks of
        ///    client-side script to the client. Any script blocks with the same type and key 
        ///    values are considered duplicates.</para>
        /// </devdoc>
        public void RegisterClientScriptBlock(Type type, string key, string script, bool addScriptTags) {
            if (type == null) { 
                throw new ArgumentNullException("type");
            } 
 
            if (addScriptTags) {
                RegisterScriptBlock(CreateScriptKey(type, key), script, ClientAPIRegisterType.ClientScriptBlocksWithoutTags); 
            }
            else {
                RegisterScriptBlock(CreateScriptKey(type, key), script, ClientAPIRegisterType.ClientScriptBlocks);
            } 
        }
 
        // RegisterClientScriptBlock implementation that supports partial rendering. 
        internal void RegisterClientScriptBlock(Control control, Type type, string key, string script, bool addScriptTags) {
            IScriptManager scriptManager = _owner.ScriptManager; 
            if ((scriptManager != null) && scriptManager.SupportsPartialRendering) {
                scriptManager.RegisterClientScriptBlock(control, type, key, script, addScriptTags);
            }
            else { 
                RegisterClientScriptBlock(type, key, script, addScriptTags);
            } 
        } 

 
        /// <devdoc>
        ///    <para> Prevents controls from sending duplicate blocks of
        ///       client-side script to the client. Any script blocks with the same <paramref name="key"/> parameter
        ///       values are considered duplicates.</para> 
        /// </devdoc>
        public void RegisterClientScriptInclude(string key, string url) { 
            RegisterClientScriptInclude(typeof(Page), key, url); 
        }
 
        /// <devdoc>
        ///    Prevents controls from sending duplicate blocks of
        ///    client-side script to the client. Any script blocks with the same type and key
        ///    values are considered duplicates.</para> 
        /// </devdoc>
        public void RegisterClientScriptInclude(Type type, string key, string url) { 
            if (type == null) { 
                throw new ArgumentNullException("type");
            } 
            if (String.IsNullOrEmpty(url)) {
                throw ExceptionUtil.ParameterNullOrEmpty("url");
            }
 
            // VSWhidbey 499036: encode the url
            string script = IncludeScriptBegin + HttpUtility.HtmlAttributeEncode(url) + IncludeScriptEnd; 
            RegisterScriptBlock(CreateScriptIncludeKey(type, key), script, ClientAPIRegisterType.ClientScriptBlocks); 
        }
 
        // RegisterClientScriptInclude implementation that supports partial rendering.
        internal void RegisterClientScriptInclude(Control control, Type type, string key, string url) {
            IScriptManager scriptManager = _owner.ScriptManager;
            if ((scriptManager != null) && scriptManager.SupportsPartialRendering) { 
                scriptManager.RegisterClientScriptInclude(control, type, key, url);
            } 
            else { 
                RegisterClientScriptInclude(type, key, url);
            } 
        }


        /// <devdoc> 
        /// </devdoc>
        public void RegisterClientScriptResource(Type type, string resourceName) { 
            if (type == null) { 
                throw new ArgumentNullException("type");
            } 

            RegisterClientScriptInclude(type, resourceName, GetWebResourceUrl(type, resourceName));
        }
 
        // RegisterClientScriptResource implementation that supports partial rendering.
        internal void RegisterClientScriptResource(Control control, Type type, string resourceName) { 
            IScriptManager scriptManager = _owner.ScriptManager; 
            if ((scriptManager != null) && scriptManager.SupportsPartialRendering) {
                scriptManager.RegisterClientScriptResource(control, type, resourceName); 
            }
            else {
                RegisterClientScriptResource(type, resourceName);
            } 
        }
 
 
        internal void RegisterDefaultButtonScript(Control button, HtmlTextWriter writer, bool useAddAttribute) {
            _owner.RegisterWebFormsScript(); 
            if (_owner.EnableLegacyRendering) {
                if (useAddAttribute) {
                    writer.AddAttribute("language", "javascript", false);
                } 
                else {
                    writer.WriteAttribute("language", "javascript", false); 
                } 
            }
            string keyPress = "javascript:return WebForm_FireDefaultButton(event, '" + button.ClientID + "')"; 
            if (useAddAttribute) {
                writer.AddAttribute("onkeypress", keyPress);
            }
            else { 
                writer.WriteAttribute("onkeypress", keyPress);
            } 
        } 

        /// <devdoc> 
        ///    <para>Allows a control to access a the client
        ///    <see langword='onsubmit'/> event.
        ///       The script should be a function call to client code registered elsewhere.</para>
        /// </devdoc> 
        public void RegisterOnSubmitStatement(Type type, string key, string script) {
            if (type == null) { 
                throw new ArgumentNullException("type"); 
            }
 
            RegisterOnSubmitStatementInternal(CreateScriptKey(type, key), script);
        }

 
        // RegisterOnSubmitStatement implementation that supports partial rendering.
        internal void RegisterOnSubmitStatement(Control control, Type type, string key, string script) { 
            IScriptManager scriptManager = _owner.ScriptManager; 
            if ((scriptManager != null) && scriptManager.SupportsPartialRendering) {
                scriptManager.RegisterOnSubmitStatement(control, type, key, script); 
            }
            else {
                RegisterOnSubmitStatement(type, key, script);
            } 
        }
 
 
        internal void RegisterOnSubmitStatementInternal(ScriptKey key, string script) {
            if (String.IsNullOrEmpty(script)) { 
                throw ExceptionUtil.ParameterNullOrEmpty("script");
            }
            if (_registeredOnSubmitStatements == null)
                _registeredOnSubmitStatements = new ListDictionary(); 

            // Make sure the script block ends in a semicolon 
            int index = script.Length - 1; 
            while ((index >= 0) && Char.IsWhiteSpace(script, index)) {
                index--; 
            }

            if ((index >= 0) && (script[index] != ';')) {
                script = script.Substring(0, index + 1) + ";" + script.Substring(index + 1); 
            }
 
            if (_registeredOnSubmitStatements[key] == null) 
                _registeredOnSubmitStatements.Add(key, script);
 
            // If there are any partial caching controls on the stack, forward the call to them
            if (_owner.PartialCachingControlStack != null) {
                foreach(BasePartialCachingControl c in _owner.PartialCachingControlStack) {
                    c.RegisterOnSubmitStatement(key, script); 
                }
            } 
        } 

        internal void RegisterScriptBlock(ScriptKey key, string script, ClientAPIRegisterType type) { 

            // Call RegisterScriptBlock with the correct collection based on the blockType
            switch (type) {
                case ClientAPIRegisterType.ClientScriptBlocks: 
                    RegisterScriptBlock(key, script, ref _registeredClientScriptBlocks, ref _clientScriptBlocks, false, ref _clientScriptBlocksInScriptTag);
                    break; 
                case ClientAPIRegisterType.ClientScriptBlocksWithoutTags: 
                    RegisterScriptBlock(key, script, ref _registeredClientScriptBlocks, ref _clientScriptBlocks, true, ref _clientScriptBlocksInScriptTag);
                    break; 
                case ClientAPIRegisterType.ClientStartupScripts:
                    RegisterScriptBlock(key, script, ref _registeredClientStartupScripts, ref _clientStartupScripts, false, ref _clientStartupScriptInScriptTag);
                    break;
                case ClientAPIRegisterType.ClientStartupScriptsWithoutTags: 
                    RegisterScriptBlock(key, script, ref _registeredClientStartupScripts, ref _clientStartupScripts, true, ref _clientStartupScriptInScriptTag);
                    break; 
                default: 
                    Debug.Assert(false);
                    break; 
            }

            // If there are any partial caching controls on the stack, forward the call to them
            if (_owner.PartialCachingControlStack != null) { 
                foreach(BasePartialCachingControl c in _owner.PartialCachingControlStack) {
                    c.RegisterScriptBlock(type, key, script); 
                } 
            }
        } 

        private void RegisterScriptBlock(ScriptKey key, string script, ref ListDictionary scriptBlocks, ref ArrayList scriptList, bool needsScriptTags, ref bool inScriptBlock) {
            if (scriptBlocks == null)
                scriptBlocks = new ListDictionary(); 

            if (scriptBlocks[key] == null) { 
                scriptBlocks.Add(key, script); 

                // Now build up the script string 
                if (scriptList == null) {
                    scriptList = new ArrayList();

                    // If the the first script needs script tags, emit a start script tag 
                    if (needsScriptTags) {
                        scriptList.Add(_owner.EnableLegacyRendering ? ClientScriptStartLegacy : ClientScriptStart); 
                    } 
                }
                else { 
                    // If we already have some script
                    if (needsScriptTags) {
                        // If we need script tags and we're not in a script tag, emit a start script tag
                        if (!inScriptBlock) { 
                            scriptList.Add(_owner.EnableLegacyRendering ? ClientScriptStartLegacy : ClientScriptStart);
                        } 
                    } 
                    else {
                        // If we don't need script tags, and we're in a script tag, emit an end script tag 
                        if (inScriptBlock) {
                            scriptList.Add(_owner.EnableLegacyRendering ? ClientScriptEndLegacy : ClientScriptEnd);
                        }
                    } 
                }
 
                scriptList.Add(script); 
                inScriptBlock = needsScriptTags;
            } 
        }

        /// <devdoc>
        ///    <para> 
        ///       Allows controls to keep duplicate blocks of client-side script code from
        ///       being sent to the client. Any script blocks with the same type and key 
        ///       value are considered duplicates. 
        ///    </para>
        /// </devdoc> 
        public void RegisterStartupScript(Type type, string key, string script) {
            RegisterStartupScript(type, key, script, false);
        }
 
        /// <devdoc>
        ///    <para> 
        ///       Allows controls to keep duplicate blocks of client-side script code from 
        ///       being sent to the client. Any script blocks with the same type and key
        ///       value are considered duplicates. 
        ///    </para>
        /// </devdoc>
        public void RegisterStartupScript(Type type, string key, string script, bool addScriptTags) {
            if (type == null) { 
                throw new ArgumentNullException("type");
            } 
 
            if (addScriptTags) {
                RegisterScriptBlock(CreateScriptKey(type, key), script, ClientAPIRegisterType.ClientStartupScriptsWithoutTags); 
            }
            else {
                RegisterScriptBlock(CreateScriptKey(type, key), script, ClientAPIRegisterType.ClientStartupScripts);
            } 
        }
 
        // RegisterStartupScript implementation that supports partial rendering. 
        internal void RegisterStartupScript(Control control, Type type, string key, string script, bool addScriptTags) {
            IScriptManager scriptManager = _owner.ScriptManager; 
            if ((scriptManager != null) && scriptManager.SupportsPartialRendering) {
                scriptManager.RegisterStartupScript(control, type, key, script, addScriptTags);
            }
            else { 
                RegisterStartupScript(type, key, script, addScriptTags);
            } 
        } 

 
        internal void RenderArrayDeclares(HtmlTextWriter writer) {
            if (_registeredArrayDeclares == null || _registeredArrayDeclares.Count == 0) {
                return;
            } 

            writer.Write(_owner.EnableLegacyRendering ? ClientScriptStartLegacy : ClientScriptStart); 
 
            // Write out each array
            IDictionaryEnumerator arrays = _registeredArrayDeclares.GetEnumerator(); 
            while (arrays.MoveNext()) {
                // Write the declaration
                writer.Write("var ");
                writer.Write(arrays.Key); 
                writer.Write(" =  new Array(");
 
                // Write each element 
                IEnumerator elements = ((ArrayList)arrays.Value).GetEnumerator();
                bool first = true; 
                while (elements.MoveNext()) {
                    if (first) {
                        first = false;
                    } 
                    else {
                        writer.Write(", "); 
                    } 
                    writer.Write(elements.Current);
                } 

                // Close the declaration
                writer.WriteLine(");");
            } 

            writer.Write(_owner.EnableLegacyRendering ? ClientScriptEndLegacy : ClientScriptEnd); 
        } 

        internal void RenderExpandoAttribute(HtmlTextWriter writer) { 
            if (_registeredControlsWithExpandoAttributes == null ||
                _registeredControlsWithExpandoAttributes.Count == 0) {
                return;
            } 

            writer.Write(_owner.EnableLegacyRendering ? ClientScriptStartLegacy : ClientScriptStart); 
 
            foreach (DictionaryEntry controlEntry in _registeredControlsWithExpandoAttributes) {
                string controlId = (string) controlEntry.Key; 
                writer.Write("var ");
                writer.Write(controlId);
                writer.Write(" = document.all ? document.all[\"");
                writer.Write(controlId); 
                writer.Write("\"] : document.getElementById(\"");
                writer.Write(controlId); 
                writer.WriteLine("\");"); 

                ListDictionary expandoAttributes = (ListDictionary) controlEntry.Value; 
                Debug.Assert(expandoAttributes != null && expandoAttributes.Count > 0);
                foreach (DictionaryEntry expandoAttribute in expandoAttributes) {
                    writer.Write(controlId);
                    writer.Write("."); 
                    writer.Write(expandoAttribute.Key);
                    if (expandoAttribute.Value == null) { 
                        // VSWhidbey 382151 Render out null string for nulls 
                        writer.WriteLine(" = null;");
                    } 
                    else {
                        writer.Write(" = \"");
                        writer.Write(expandoAttribute.Value);
                        writer.WriteLine("\";"); 
                    }
                } 
            } 

            writer.Write(_owner.EnableLegacyRendering ? ClientScriptEndLegacy : ClientScriptEnd); 
        }

        internal void RenderHiddenFields(HtmlTextWriter writer) {
            if (_registeredHiddenFields == null || _registeredHiddenFields.Count == 0) { 
                return;
            } 
 
            foreach (DictionaryEntry entry in _registeredHiddenFields) {
                string entryKey = (string)entry.Key; 
                if (entryKey == null) {
                    entryKey = String.Empty;
                }
                writer.WriteLine(); 
                writer.Write("<input type=\"hidden\" name=\"");
                writer.Write(entryKey); 
                writer.Write("\" id=\""); 
                writer.Write(entryKey);
                writer.Write("\" value=\""); 
                HttpUtility.HtmlEncode((string)entry.Value, writer);
                writer.Write("\" />");
            }
 
            ClearHiddenFields();
        } 
 
        internal void RenderClientScriptBlocks(HtmlTextWriter writer) {
            if (_clientScriptBlocks != null) { 
                writer.WriteLine();
                // Write out each registered script block
                foreach (string s in _clientScriptBlocks) {
                    writer.Write(s); 
                }
            } 
 
            // Emit the onSubmit function, in necessary
            if (!String.IsNullOrEmpty(_owner.ClientOnSubmitEvent) && _owner.ClientSupportsJavaScript) { 
                // If we were already inside a script tag, don't emit a new open script tag
                if (!_clientScriptBlocksInScriptTag) {
                    writer.Write(_owner.EnableLegacyRendering ? ClientScriptStartLegacy : ClientScriptStart);
                } 

                writer.Write(@"function WebForm_OnSubmit() { 
"); 
                if (_registeredOnSubmitStatements != null) {
                    foreach (string s in _registeredOnSubmitStatements.Values) { 
                        writer.Write(s);
                    }
                }
                writer.WriteLine(@" 
return true;
}"); 
                // We always need to close the script tag 
                writer.Write(_owner.EnableLegacyRendering ? ClientScriptEndLegacy : ClientScriptEnd);
            } 
            // If there was no onSubmit function, close the script tag if needed
            else if (_clientScriptBlocksInScriptTag) {
                writer.Write(_owner.EnableLegacyRendering ? ClientScriptEndLegacy : ClientScriptEnd);
            } 
        }
 
        internal void RenderClientStartupScripts(HtmlTextWriter writer) { 
            if (_clientStartupScripts != null) {
                writer.WriteLine(); 
                // Write out each startup script
                foreach (string s in _clientStartupScripts) {
                    writer.Write(s);
                } 

                // Close the script tag if needed 
                if (_clientStartupScriptInScriptTag) { 
                    writer.Write(_owner.EnableLegacyRendering ? ClientScriptEndLegacy : ClientScriptEnd);
                } 
            }
        }

        internal void RenderWebFormsScript(HtmlTextWriter writer) { 
            writer.Write(IncludeScriptBegin);
            writer.Write(GetWebResourceUrl(_owner, typeof(Page), "WebForms.js", true)); 
            writer.WriteLine(IncludeScriptEnd); 
        }
    } 

    internal class ScriptKey {
        private Type _type;
        private string _key; 
        private bool _isInclude;
 
        internal ScriptKey(Type type, string key) : this(type, key, false) { 
        }
 
        internal ScriptKey(Type type, string key, bool isInclude) {
            _type = type;

            // To treat empty strings the same as nulls, make them null 
            if (String.IsNullOrEmpty(key)) {
                key = null; 
            } 
            _key = key;
            _isInclude = isInclude; 
        }

        public override int GetHashCode() {
            return WebUtil.HashCodeCombiner.CombineHashCodes(_type.GetHashCode(), _key.GetHashCode(), 
                                                             _isInclude.GetHashCode());
        } 
 
        public override bool Equals(object o) {
            ScriptKey key = (ScriptKey)o; 
            return (key._type == _type) && (key._key == _key) && (key._isInclude == _isInclude);
        }
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="ClientScriptManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI { 
    using System; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Collections;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Text; 
    using System.Web.Handlers;
    using System.Web.UI.WebControls; 
    using System.Web.Util; 
    using ExceptionUtil=System.Web.Util.ExceptionUtil;
    using WebUtil = System.Web.Util; 
    using System.Security.Permissions;

    // The various types of client API's that can be registered
    internal enum ClientAPIRegisterType { 
        WebFormsScript,
        PostBackScript, 
        FocusScript, 
        ClientScriptBlocks,
        ClientScriptBlocksWithoutTags, 
        ClientStartupScripts,
        ClientStartupScriptsWithoutTags,
        OnSubmitStatement,
        ArrayDeclaration, 
        HiddenField,
        ExpandoAttribute, 
        EventValidation, 
    }
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class ClientScriptManager {
        private const string IncludeScriptBegin = @"
<script src="""; 
        private const string IncludeScriptEnd = @""" type=""text/javascript""></script>";
        internal const string ClientScriptStart = "\r\n<script type=\"text/javascript\">\r\n//<![CDATA[\r\n"; 
        internal const string ClientScriptStartLegacy = "\r\n<script type=\"text/javascript\">\r\n<!--\r\n"; 
        internal const string ClientScriptEnd = "//]]>\r\n</script>\r\n";
        internal const string ClientScriptEndLegacy = "// -->\r\n</script>\r\n"; 
        internal const string JscriptPrefix = "javascript:";

        private const string _callbackFunctionName = "WebForm_DoCallback";
        private const string _postbackOptionsFunctionName = "WebForm_DoPostBackWithOptions"; 
        private const string _postBackFunctionName = "__doPostBack";
        private const string PageCallbackScriptKey = "PageCallbackScript"; 
 
        private ListDictionary _registeredClientScriptBlocks;
        private ArrayList _clientScriptBlocks; 
        private bool _clientScriptBlocksInScriptTag;

        private ListDictionary _registeredClientStartupScripts;
        private ArrayList _clientStartupScripts; 
        private bool _clientStartupScriptInScriptTag;
        private bool _eventValidationFieldLoaded; 
 
        private ListDictionary _registeredOnSubmitStatements;
 
        private IDictionary _registeredArrayDeclares;
        private IDictionary _registeredHiddenFields;
        private ListDictionary _registeredControlsWithExpandoAttributes;
 
        private ArrayList _validEventReferences;
        private HybridDictionary _clientPostBackValidatedEventTable; 
 
        private Page _owner;
 
        internal ClientScriptManager(Page owner) {
            _owner = owner;
        }
 
        internal bool HasRegisteredHiddenFields {
            get { 
                return (_registeredHiddenFields != null && _registeredHiddenFields.Count > 0); 
            }
        } 

        internal bool HasSubmitStatements {
            get {
                return (_registeredOnSubmitStatements != null && _registeredOnSubmitStatements.Count > 0); 
            }
        } 
 
        private static int ComputeHashKey(String uniqueId, String argument) {
            if (String.IsNullOrEmpty(argument)) { 
                return StringUtil.GetStringHashCode(uniqueId);
            }

            return StringUtil.GetStringHashCode(uniqueId) ^ StringUtil.GetStringHashCode(argument); 
        }
 
        internal string GetEventValidationFieldValue() { 
            if ((_validEventReferences == null) || (_validEventReferences.Count == 0)) {
                return String.Empty; 
            }
            IStateFormatter formatter = _owner.CreateStateFormatter();
            return formatter.Serialize(_validEventReferences);
        } 

        public void RegisterForEventValidation(PostBackOptions options) { 
            RegisterForEventValidation(options.TargetControl.UniqueID, options.Argument); 
        }
 
        public void RegisterForEventValidation(string uniqueId) {
            RegisterForEventValidation(uniqueId, String.Empty);
        }
 
        public void RegisterForEventValidation(string uniqueId, string argument) {
            if (!_owner.EnableEventValidation || _owner.DesignMode) { 
                return; 
            }
 
            // VSWhidbey 497632. Ignore if uniqueID is empty since the postback won't be valid anyway.
            if (String.IsNullOrEmpty(uniqueId)) {
                return;
            } 

            if ((_owner.ControlState < ControlState.PreRendered) && (!_owner.IsCallback)) { 
                throw new InvalidOperationException( 
                    SR.GetString(SR.ClientScriptManager_RegisterForEventValidation_Too_Early));
            } 

#if DEBUGEVENTVALIDATION
            string key = uniqueId + "@" + argument;
#else 
            int key = ComputeHashKey(uniqueId, argument);
#endif //DEBUGEVENTVALIDATION 
 
            string stateString = _owner.ClientState;
            if (stateString == null) { 
                stateString = String.Empty;
            }

            if (_validEventReferences == null) { 
                if (_owner.IsCallback) {
                    EnsureEventValidationFieldLoaded(); 
                    if (_validEventReferences == null) { 
                        _validEventReferences = new ArrayList();
                    } 
                }
                else {
                    _validEventReferences = new ArrayList();
                    _validEventReferences.Add( 
                        StringUtil.GetStringHashCode(stateString));
                } 
            } 

#if DEBUGEVENTVALIDATION 
            Debug.Assert(!_validEventReferences.Contains(key));
#endif //DEBUGEVENTVALIDATION

            _validEventReferences.Add(key); 

            // If there are any partial caching controls on the stack, forward the call to them 
            if (_owner.PartialCachingControlStack != null) { 
                foreach (BasePartialCachingControl c in _owner.PartialCachingControlStack) {
                    c.RegisterForEventValidation(uniqueId, argument); 
                }
            }
        }
 
        internal void SaveEventValidationField() {
            string fieldValue = GetEventValidationFieldValue(); 
            if (!String.IsNullOrEmpty(fieldValue)) { 
                RegisterHiddenField(Page.EventValidationPrefixID, fieldValue);
            } 
        }

        private void EnsureEventValidationFieldLoaded() {
            if (_eventValidationFieldLoaded) { 
                return;
            } 
 
            _eventValidationFieldLoaded = true;
 
            string unsafeField = null;
            if (_owner.RequestValueCollection != null) {
                unsafeField = _owner.RequestValueCollection[Page.EventValidationPrefixID];
            } 

            if (String.IsNullOrEmpty(unsafeField)) { 
                return; 
            }
 
            IStateFormatter formatter = _owner.CreateStateFormatter();
            ArrayList validatedClientEvents = null;

            try { 
                validatedClientEvents = formatter.Deserialize(unsafeField) as ArrayList;
            } 
            catch(Exception ex) { 
                ViewStateException.ThrowViewStateError(ex, unsafeField);
            } 

            if (validatedClientEvents == null || validatedClientEvents.Count < 1) {
                return;
            } 

            Debug.Assert(_clientPostBackValidatedEventTable == null); 
            int viewStateHashCode = (int)validatedClientEvents[0]; 

            string viewStateString = _owner.RequestViewStateString; 

            if (viewStateHashCode != StringUtil.GetStringHashCode(viewStateString)) {
                ViewStateException.ThrowViewStateError(null, unsafeField);
            } 

            _clientPostBackValidatedEventTable = new HybridDictionary(validatedClientEvents.Count - 1, true); 
 
            // Ignore the first item in the arrayList, which is the controlstate
            for (int index = 1; index < validatedClientEvents.Count; index++) { 
#if DEBUGEVENTVALIDATION
                string hashKey = (string)validatedClientEvents[index];
#else
                int hashKey = (int)validatedClientEvents[index]; 
#endif //DEBUGEVENTVALIDATION
                _clientPostBackValidatedEventTable[hashKey] = null; 
            } 

            if (_owner.IsCallback) { 
                _validEventReferences = validatedClientEvents;
            }
        }
 
        public void ValidateEvent(string uniqueId) {
            ValidateEvent(uniqueId, String.Empty); 
        } 

        public void ValidateEvent(string uniqueId, string argument) { 
            if (!_owner.EnableEventValidation) {
                return;
            }
 
            if (String.IsNullOrEmpty(uniqueId)) {
                throw new ArgumentException(SR.GetString(SR.Parameter_NullOrEmpty, "uniqueId"), "uniqueId"); 
            } 

            EnsureEventValidationFieldLoaded(); 

            if (_clientPostBackValidatedEventTable == null) {
                throw new ArgumentException(SR.GetString(SR.ClientScriptManager_InvalidPostBackArgument));
            } 

#if DEBUGEVENTVALIDATION 
            String hashCode = uniqueId + "@" + argument; 
#else
            int hashCode = ComputeHashKey(uniqueId, argument); 
#endif //DEBUGEVENTVALIDATION

            if (!_clientPostBackValidatedEventTable.Contains(hashCode)) {
                throw new ArgumentException(SR.GetString(SR.ClientScriptManager_InvalidPostBackArgument)); 
            }
        } 
 
        internal void ClearHiddenFields() {
            _registeredHiddenFields = null; 
        }

        internal static ScriptKey CreateScriptKey(Type type, string key) {
            return new ScriptKey(type, key); 
        }
 
        internal static ScriptKey CreateScriptIncludeKey(Type type, string key) { 
            return new ScriptKey(type, key, true);
        } 

        /// <devdoc>
        ///   Enables controls to obtain client-side script function that will cause
        ///   (when invoked) an out-of-band callback to the server 
        /// </devdoc>
        public string GetCallbackEventReference(Control control, string argument, string clientCallback, string context) { 
            return GetCallbackEventReference(control, argument, clientCallback, context, false); 
        }
 
        public string GetCallbackEventReference(Control control, string argument, string clientCallback, string context, bool useAsync) {
            return GetCallbackEventReference(control, argument, clientCallback, context, null, useAsync);
        }
 
        /// <devdoc>
        ///   Enables controls to obtain client-side script function that will cause 
        ///   (when invoked) an out-of-band callback to the server and allows the user to specify a client-side error callback 
        /// </devdoc>
        public string GetCallbackEventReference(Control control, string argument, string clientCallback, string context, string clientErrorCallback, bool useAsync) { 
            if (control == null) {
                throw new ArgumentNullException("control");
            }
            if (!(control is ICallbackEventHandler)) { 
                throw new InvalidOperationException(SR.GetString(SR.Page_CallBackTargetInvalid, control.UniqueID));
            } 
            return GetCallbackEventReference("'" + control.UniqueID + "'", argument, clientCallback, context, clientErrorCallback, useAsync); 
        }
 
        /// <devdoc>
        ///   Enables controls to obtain client-side script function that will cause
        ///   (when invoked) an out-of-band callback to the server and allows the user to specify a client-side error callback
        /// </devdoc> 
        public string GetCallbackEventReference(string target, string argument, string clientCallback, string context, string clientErrorCallback, bool useAsync) {
            _owner.RegisterWebFormsScript(); 
            if (_owner.ClientSupportsJavaScript && (_owner.RequestInternal != null) && _owner.RequestInternal.Browser.SupportsCallback) { 
                RegisterStartupScript(typeof(Page), PageCallbackScriptKey, (((_owner.RequestInternal != null) &&
                        (String.Equals(_owner.RequestInternal.Url.Scheme, "https", StringComparison.OrdinalIgnoreCase))) ? 
                            @"
var callBackFrameUrl='" + Util.QuoteJScriptString(GetWebResourceUrl(typeof(Page), "SmartNav.htm"), false) + @"';
WebForm_InitCallback();" :
                            @" 
WebForm_InitCallback();"), true);
            } 
            if (argument == null) { 
                argument = "null";
            } 
            else if (argument.Length == 0) {
                argument = "\"\"";
            }
            if (context == null) { 
                context = "null";
            } 
            else if (context.Length == 0) { 
                context = "\"\"";
            } 
            return _callbackFunctionName +
                   "(" +
                   target +
                   "," + 
                   argument +
                   "," + 
                   clientCallback + 
                   "," +
                   context + 
                   "," +
                   ((clientErrorCallback == null) ? "null" : clientErrorCallback) +
                   "," +
                   (useAsync ? "true" : "false") + 
                   ")";
        } 
 
        public string GetPostBackClientHyperlink(Control control, string argument) {
            // We're using escapePercent=true here and false in Page 
            // because true in Page would be a breaking change:
            // People may already be encoding percent characters before calling this,
            // and we may double encode it.
            // Our own classes and new code should almost always use the override with escapePercent=true. 
            return GetPostBackClientHyperlink(control, argument, true, false);
        } 
 
        public string GetPostBackClientHyperlink(Control control, string argument, bool registerForEventValidation) {
            // We're using escapePercent=true here and false in Page 
            // because true in Page would be a breaking change:
            // People may already be encoding percent characters before calling this,
            // and we may double encode it.
            // Our own classes and new code should almost always use the override with escapePercent=true. 
            return GetPostBackClientHyperlink(control, argument, true, registerForEventValidation);
        } 
 
        /// <devdoc>
        ///    <para>This returs a string that can be put in client event to post back to the named control</para> 
        /// </devdoc>
        internal string GetPostBackClientHyperlink(Control control, string argument, bool escapePercent, bool registerForEventValidation) {
            // Hyperlinks always need the language prefix
            // If used in a hyperlink, the event argument needs to be escaped for % characters 
            // which will otherwise be interpreted as escape sequences (VSWhidbey 421874)
            return JscriptPrefix + GetPostBackEventReference(control, argument, escapePercent, registerForEventValidation); 
        } 

        public string GetPostBackEventReference(Control control, string argument) { 
            return GetPostBackEventReference(control, argument, false, false);
        }

        public string GetPostBackEventReference(Control control, string argument, bool registerForEventValidation) { 
            return GetPostBackEventReference(control, argument, false, registerForEventValidation);
        } 
 
        /*
         * Enables controls to obtain client-side script function that will cause 
         * (when invoked) a server post-back to the form.
         * argument: Parameter that will be passed to control on server
         */
        /// <devdoc> 
        ///    <para>Passes a parameter to the control that will do the postback processing on the
        ///       server.</para> 
        /// </devdoc> 
        private string GetPostBackEventReference(Control control, string argument, bool forUrl, bool registerForEventValidation) {
            if (control == null) { 
                throw new ArgumentNullException("control");
            }

            _owner.RegisterPostBackScript(); 

            string controlID = control.UniqueID; 
 
            if (registerForEventValidation) {
                RegisterForEventValidation(controlID, argument); 
            }

            // VSWhidbey 475945
            if (control.EnableLegacyRendering && _owner.IsInOnFormRender && 
                controlID != null && controlID.IndexOf(Control.LEGACY_ID_SEPARATOR) >= 0) {
 
                controlID = controlID.Replace(Control.LEGACY_ID_SEPARATOR, Control.ID_SEPARATOR); 
            }
 
            // Split into 2 calls to String.Concat to improve performance.
            
            string postBackEventReference = _postBackFunctionName + "('" + controlID + "','";
            // The argument needs to be quoted, in case in contains characters that 
            // can't be used in JScript strings (ASURT 71818).
            postBackEventReference += Util.QuoteJScriptString(argument, forUrl) + "')"; 
 
            return postBackEventReference;
        } 

        /// <devdoc>
        ///    <para>Passes a parameter to the control that will do the postback processing on the
        ///       server.</para> 
        /// </devdoc>
        public string GetPostBackEventReference(PostBackOptions options) { 
            return GetPostBackEventReference(options, false); 
        }
 
        public string GetPostBackEventReference(PostBackOptions options, bool registerForEventValidation) {
            if (options == null) {
                throw new ArgumentNullException("options");
            } 

            if (registerForEventValidation) { 
                RegisterForEventValidation(options); 
            }
 
            StringBuilder builder = new StringBuilder();
            bool shouldRenderPostBackReferenceString = false;

            if (options.RequiresJavaScriptProtocol) { 
                builder.Append(JscriptPrefix);
            } 
 
            if (options.AutoPostBack) {
                builder.Append("setTimeout('"); 
            }

            // Use the old __doPostBack method if not using other postback features.
            if (!options.PerformValidation && !options.TrackFocus && options.ClientSubmit && 
                string.IsNullOrEmpty(options.ActionUrl)) {
                string postbackRef = GetPostBackEventReference(options.TargetControl, options.Argument); 
 
                // Need to quote the string if auto posting back
                if (options.AutoPostBack) { 
                    builder.Append(Util.QuoteJScriptString(postbackRef));
                    builder.Append("', 0)");
                }
                else { 
                    builder.Append(postbackRef);
                } 
 
                return builder.ToString();
            } 

            builder.Append(_postbackOptionsFunctionName);
            builder.Append("(new WebForm_PostBackOptions(\"");
            builder.Append(options.TargetControl.UniqueID); 
            builder.Append("\", ");
 
            if (String.IsNullOrEmpty(options.Argument)) { 
                builder.Append("\"\", ");
            } 
            else {
                builder.Append("\"");
                builder.Append(Util.QuoteJScriptString(options.Argument));
                builder.Append("\", "); 
            }
 
            if (options.PerformValidation) { 
                shouldRenderPostBackReferenceString = true;
                builder.Append("true, "); 
            }
            else {
                builder.Append("false, ");
            } 

            if (options.ValidationGroup!= null && options.ValidationGroup.Length > 0) { 
                shouldRenderPostBackReferenceString = true; 

                builder.Append("\""); 
                builder.Append(options.ValidationGroup);
                builder.Append("\", ");
            }
            else { 
                builder.Append("\"\", ");
            } 
 
            if (options.ActionUrl != null && options.ActionUrl.Length > 0) {
                shouldRenderPostBackReferenceString = true; 
                _owner.ContainsCrossPagePost = true;

                builder.Append("\"");
                builder.Append(Util.QuoteJScriptString(options.ActionUrl)); 
                builder.Append("\", ");
            } 
            else { 
                builder.Append("\"\", ");
            } 

            if (options.TrackFocus) {
                _owner.RegisterFocusScript();
                shouldRenderPostBackReferenceString = true; 
                builder.Append("true, ");
            } 
            else { 
                builder.Append("false, ");
            } 

            if (options.ClientSubmit) {
                shouldRenderPostBackReferenceString = true;
                _owner.RegisterPostBackScript(); 

                builder.Append("true))"); 
            } 
            else {
                builder.Append("false))"); 
            }

            if (options.AutoPostBack) {
                builder.Append("', 0)"); 
            }
 
            string reference =  null; 
            if (shouldRenderPostBackReferenceString) {
                reference = builder.ToString(); 
                _owner.RegisterWebFormsScript();
            }

            return reference; 
        }
 
        /// <devdoc> 
        /// Gets a URL resource reference to a client-side resource
        /// </devdoc> 
        public string GetWebResourceUrl(Type type, string resourceName) {
            return GetWebResourceUrl(_owner, type, resourceName, false);
        }
 
        internal static string GetWebResourceUrl(Page owner, Type type, string resourceName, bool htmlEncoded) {
            if (type == null) { 
                throw new ArgumentNullException("type"); 
            }
 
            if (String.IsNullOrEmpty(resourceName)) {
                throw new ArgumentNullException("resourceName");
            }
 
            if (owner != null && owner.DesignMode) {
                ISite site = ((IComponent)owner).Site; 
                if (site != null) { 
                    IResourceUrlGenerator urlGenerator = site.GetService(typeof(IResourceUrlGenerator)) as IResourceUrlGenerator;
                    if (urlGenerator != null) { 
                        return urlGenerator.GetResourceUrl(type, resourceName);
                    }
                }
 
                return resourceName;
            } 
            else { 
                return AssemblyResourceLoader.GetWebResourceUrl(type, resourceName, htmlEncoded);
            } 
        }

        /// <devdoc>
        ///    <para>Determines if the client script block is registered with the page.</para> 
        /// </devdoc>
        public bool IsClientScriptBlockRegistered(string key) { 
            return IsClientScriptBlockRegistered(typeof(Page), key); 
        }
 
        /// <devdoc>
        ///    <para>Determines if the client script block is registered with the page.</para>
        /// </devdoc>
        public bool IsClientScriptBlockRegistered(Type type, string key) { 
            if (type == null) {
                throw new ArgumentNullException("type"); 
            } 

            ScriptKey scriptKey = CreateScriptKey(type, key); 
            return (_registeredClientScriptBlocks != null
                   && (_registeredClientScriptBlocks[scriptKey] != null));
        }
 
        /// <devdoc>
        ///    <para>Determines if the onsubmit script is registered with the page.</para> 
        /// </devdoc> 
        public bool IsClientScriptIncludeRegistered(string key) {
            return IsClientScriptIncludeRegistered(typeof(Page), key); 
        }

        /// <devdoc>
        ///    <para>Determines if the onsubmit script  is registered with the page.</para> 
        /// </devdoc>
        public bool IsClientScriptIncludeRegistered(Type type, string key) { 
            if (type == null) { 
                throw new ArgumentNullException("type");
            } 

            return(_registeredClientScriptBlocks != null
                   && (_registeredClientScriptBlocks[CreateScriptIncludeKey(type, key)] != null));
        } 

        /// <devdoc> 
        ///    <para>Determines if the client startup script is registered with the 
        ///       page.</para>
        /// </devdoc> 
        public bool IsStartupScriptRegistered(string key) {
            return IsStartupScriptRegistered(typeof(Page), key);
        }
 
        /// <devdoc>
        ///    <para>Determines if the client startup script is registered with the 
        ///       page.</para> 
        /// </devdoc>
        public bool IsStartupScriptRegistered(Type type, string key) { 
            if (type == null) {
                throw new ArgumentNullException("type");
            }
 
            return(_registeredClientStartupScripts != null
                   && (_registeredClientStartupScripts[CreateScriptKey(type, key)] != null)); 
        } 

        /// <devdoc> 
        ///    <para>Determines if the onsubmit script is registered with the page.</para>
        /// </devdoc>
        public bool IsOnSubmitStatementRegistered(string key) {
            return IsOnSubmitStatementRegistered(typeof(Page), key); 
        }
 
        /// <devdoc> 
        ///    <para>Determines if the onsubmit script  is registered with the page.</para>
        /// </devdoc> 
        public bool IsOnSubmitStatementRegistered(Type type, string key) {
            if (type == null) {
                throw new ArgumentNullException("type");
            } 

            return(_registeredOnSubmitStatements != null 
                   && (_registeredOnSubmitStatements[CreateScriptKey(type, key)] != null)); 
        }
 
        /// <devdoc>
        ///    <para>Declares a value that will be declared as a JavaScript array declaration
        ///       when the page renders. This can be used by script-based controls to declare
        ///       themselves within an array so that a client script library can work with 
        ///       all the controls of the same type.</para>
        /// </devdoc> 
        public void RegisterArrayDeclaration(string arrayName, string arrayValue) { 
            if (arrayName == null) {
                throw new ArgumentNullException("arrayName"); 
            }
            if (_registeredArrayDeclares == null) {
                _registeredArrayDeclares = new ListDictionary();
            } 
            if (!_registeredArrayDeclares.Contains(arrayName)) {
                _registeredArrayDeclares[arrayName] = new ArrayList(); 
            } 

            ArrayList elements = (ArrayList) _registeredArrayDeclares[arrayName]; 
            elements.Add(arrayValue);

            // If there are any partial caching controls on the stack, forward the call to them
            if (_owner.PartialCachingControlStack != null) { 
                foreach(BasePartialCachingControl c in _owner.PartialCachingControlStack) {
                    c.RegisterArrayDeclaration(arrayName, arrayValue); 
                } 
            }
        } 

        // RegisterArrayDeclaration implementation that supports partial rendering.
        internal void RegisterArrayDeclaration(Control control, string arrayName, string arrayValue) {
            IScriptManager scriptManager = _owner.ScriptManager; 
            if ((scriptManager != null) && scriptManager.SupportsPartialRendering) {
                scriptManager.RegisterArrayDeclaration(control, arrayName, arrayValue); 
            } 
            else {
                RegisterArrayDeclaration(arrayName, arrayValue); 
            }
        }

        public void RegisterExpandoAttribute(string controlId, string attributeName, string attributeValue) { 
            RegisterExpandoAttribute(controlId, attributeName, attributeValue, true);
        } 
 
        public void RegisterExpandoAttribute(string controlId, string attributeName, string attributeValue, bool encode) {
            // check paramters 
            WebUtil.StringUtil.CheckAndTrimString(controlId, "controlId");
            WebUtil.StringUtil.CheckAndTrimString(attributeName, "attributeName");

            ListDictionary expandoAttributes = null; 
            if (_registeredControlsWithExpandoAttributes == null) {
                _registeredControlsWithExpandoAttributes = new ListDictionary(StringComparer.Ordinal); 
            } 
            else {
                expandoAttributes = (ListDictionary) _registeredControlsWithExpandoAttributes[controlId]; 
            }

            if (expandoAttributes == null) {
                expandoAttributes = new ListDictionary(StringComparer.Ordinal); 
                _registeredControlsWithExpandoAttributes.Add(controlId, expandoAttributes);
            } 
 
            if (encode) {
                attributeValue = Util.QuoteJScriptString(attributeValue); 
            }

            expandoAttributes.Add(attributeName, attributeValue);
 
            // If there are any partial caching controls on the stack, forward the call to them
            if (_owner.PartialCachingControlStack != null) { 
                foreach(BasePartialCachingControl c in _owner.PartialCachingControlStack) { 
                    c.RegisterExpandoAttribute(controlId, attributeName, attributeValue);
                } 
            }
        }

        // RegisterExpandoAttribute implementation that supports partial rendering. 
        internal void RegisterExpandoAttribute(Control control, string controlId, string attributeName, string attributeValue, bool encode) {
            IScriptManager scriptManager = _owner.ScriptManager; 
            if ((scriptManager != null) && scriptManager.SupportsPartialRendering) { 
                scriptManager.RegisterExpandoAttribute(control, controlId, attributeName, attributeValue, encode);
            } 
            else {
                RegisterExpandoAttribute(controlId, attributeName, attributeValue, encode);
            }
        } 

        /// <devdoc> 
        ///    <para> 
        ///       Allows controls to automatically register a hidden field on the form. The
        ///       field will be emitted when the form control renders itself. 
        ///    </para>
        /// </devdoc>
        public void RegisterHiddenField(string hiddenFieldName,
                                        string hiddenFieldInitialValue) { 
            if (hiddenFieldName == null) {
                throw new ArgumentNullException("hiddenFieldName"); 
            } 
            if (_registeredHiddenFields == null)
                _registeredHiddenFields = new ListDictionary(); 

            if (!_registeredHiddenFields.Contains(hiddenFieldName))
                _registeredHiddenFields.Add(hiddenFieldName, hiddenFieldInitialValue);
 
            // If there are any partial caching controls on the stack, forward the call to them
            if (_owner.PartialCachingControlStack != null) { 
                foreach(BasePartialCachingControl c in _owner.PartialCachingControlStack) { 
                    c.RegisterHiddenField(hiddenFieldName, hiddenFieldInitialValue);
                } 
            }
        }

        // RegisterHiddenField implementation that supports partial rendering. 
        internal void RegisterHiddenField(Control control, string hiddenFieldName, string hiddenFieldValue) {
            IScriptManager scriptManager = _owner.ScriptManager; 
            if ((scriptManager != null) && scriptManager.SupportsPartialRendering) { 
                scriptManager.RegisterHiddenField(control, hiddenFieldName, hiddenFieldValue);
            } 
            else {
                RegisterHiddenField(hiddenFieldName, hiddenFieldValue);
            }
        } 

 
        /// <devdoc> 
        ///    Prevents controls from sending duplicate blocks of
        ///    client-side script to the client. Any script blocks with the same type and key 
        ///    values are considered duplicates.</para>
        /// </devdoc>
        public void RegisterClientScriptBlock(Type type, string key, string script) {
            RegisterClientScriptBlock(type, key, script, false); 
        }
 
        /// <devdoc> 
        ///    Prevents controls from sending duplicate blocks of
        ///    client-side script to the client. Any script blocks with the same type and key 
        ///    values are considered duplicates.</para>
        /// </devdoc>
        public void RegisterClientScriptBlock(Type type, string key, string script, bool addScriptTags) {
            if (type == null) { 
                throw new ArgumentNullException("type");
            } 
 
            if (addScriptTags) {
                RegisterScriptBlock(CreateScriptKey(type, key), script, ClientAPIRegisterType.ClientScriptBlocksWithoutTags); 
            }
            else {
                RegisterScriptBlock(CreateScriptKey(type, key), script, ClientAPIRegisterType.ClientScriptBlocks);
            } 
        }
 
        // RegisterClientScriptBlock implementation that supports partial rendering. 
        internal void RegisterClientScriptBlock(Control control, Type type, string key, string script, bool addScriptTags) {
            IScriptManager scriptManager = _owner.ScriptManager; 
            if ((scriptManager != null) && scriptManager.SupportsPartialRendering) {
                scriptManager.RegisterClientScriptBlock(control, type, key, script, addScriptTags);
            }
            else { 
                RegisterClientScriptBlock(type, key, script, addScriptTags);
            } 
        } 

 
        /// <devdoc>
        ///    <para> Prevents controls from sending duplicate blocks of
        ///       client-side script to the client. Any script blocks with the same <paramref name="key"/> parameter
        ///       values are considered duplicates.</para> 
        /// </devdoc>
        public void RegisterClientScriptInclude(string key, string url) { 
            RegisterClientScriptInclude(typeof(Page), key, url); 
        }
 
        /// <devdoc>
        ///    Prevents controls from sending duplicate blocks of
        ///    client-side script to the client. Any script blocks with the same type and key
        ///    values are considered duplicates.</para> 
        /// </devdoc>
        public void RegisterClientScriptInclude(Type type, string key, string url) { 
            if (type == null) { 
                throw new ArgumentNullException("type");
            } 
            if (String.IsNullOrEmpty(url)) {
                throw ExceptionUtil.ParameterNullOrEmpty("url");
            }
 
            // VSWhidbey 499036: encode the url
            string script = IncludeScriptBegin + HttpUtility.HtmlAttributeEncode(url) + IncludeScriptEnd; 
            RegisterScriptBlock(CreateScriptIncludeKey(type, key), script, ClientAPIRegisterType.ClientScriptBlocks); 
        }
 
        // RegisterClientScriptInclude implementation that supports partial rendering.
        internal void RegisterClientScriptInclude(Control control, Type type, string key, string url) {
            IScriptManager scriptManager = _owner.ScriptManager;
            if ((scriptManager != null) && scriptManager.SupportsPartialRendering) { 
                scriptManager.RegisterClientScriptInclude(control, type, key, url);
            } 
            else { 
                RegisterClientScriptInclude(type, key, url);
            } 
        }


        /// <devdoc> 
        /// </devdoc>
        public void RegisterClientScriptResource(Type type, string resourceName) { 
            if (type == null) { 
                throw new ArgumentNullException("type");
            } 

            RegisterClientScriptInclude(type, resourceName, GetWebResourceUrl(type, resourceName));
        }
 
        // RegisterClientScriptResource implementation that supports partial rendering.
        internal void RegisterClientScriptResource(Control control, Type type, string resourceName) { 
            IScriptManager scriptManager = _owner.ScriptManager; 
            if ((scriptManager != null) && scriptManager.SupportsPartialRendering) {
                scriptManager.RegisterClientScriptResource(control, type, resourceName); 
            }
            else {
                RegisterClientScriptResource(type, resourceName);
            } 
        }
 
 
        internal void RegisterDefaultButtonScript(Control button, HtmlTextWriter writer, bool useAddAttribute) {
            _owner.RegisterWebFormsScript(); 
            if (_owner.EnableLegacyRendering) {
                if (useAddAttribute) {
                    writer.AddAttribute("language", "javascript", false);
                } 
                else {
                    writer.WriteAttribute("language", "javascript", false); 
                } 
            }
            string keyPress = "javascript:return WebForm_FireDefaultButton(event, '" + button.ClientID + "')"; 
            if (useAddAttribute) {
                writer.AddAttribute("onkeypress", keyPress);
            }
            else { 
                writer.WriteAttribute("onkeypress", keyPress);
            } 
        } 

        /// <devdoc> 
        ///    <para>Allows a control to access a the client
        ///    <see langword='onsubmit'/> event.
        ///       The script should be a function call to client code registered elsewhere.</para>
        /// </devdoc> 
        public void RegisterOnSubmitStatement(Type type, string key, string script) {
            if (type == null) { 
                throw new ArgumentNullException("type"); 
            }
 
            RegisterOnSubmitStatementInternal(CreateScriptKey(type, key), script);
        }

 
        // RegisterOnSubmitStatement implementation that supports partial rendering.
        internal void RegisterOnSubmitStatement(Control control, Type type, string key, string script) { 
            IScriptManager scriptManager = _owner.ScriptManager; 
            if ((scriptManager != null) && scriptManager.SupportsPartialRendering) {
                scriptManager.RegisterOnSubmitStatement(control, type, key, script); 
            }
            else {
                RegisterOnSubmitStatement(type, key, script);
            } 
        }
 
 
        internal void RegisterOnSubmitStatementInternal(ScriptKey key, string script) {
            if (String.IsNullOrEmpty(script)) { 
                throw ExceptionUtil.ParameterNullOrEmpty("script");
            }
            if (_registeredOnSubmitStatements == null)
                _registeredOnSubmitStatements = new ListDictionary(); 

            // Make sure the script block ends in a semicolon 
            int index = script.Length - 1; 
            while ((index >= 0) && Char.IsWhiteSpace(script, index)) {
                index--; 
            }

            if ((index >= 0) && (script[index] != ';')) {
                script = script.Substring(0, index + 1) + ";" + script.Substring(index + 1); 
            }
 
            if (_registeredOnSubmitStatements[key] == null) 
                _registeredOnSubmitStatements.Add(key, script);
 
            // If there are any partial caching controls on the stack, forward the call to them
            if (_owner.PartialCachingControlStack != null) {
                foreach(BasePartialCachingControl c in _owner.PartialCachingControlStack) {
                    c.RegisterOnSubmitStatement(key, script); 
                }
            } 
        } 

        internal void RegisterScriptBlock(ScriptKey key, string script, ClientAPIRegisterType type) { 

            // Call RegisterScriptBlock with the correct collection based on the blockType
            switch (type) {
                case ClientAPIRegisterType.ClientScriptBlocks: 
                    RegisterScriptBlock(key, script, ref _registeredClientScriptBlocks, ref _clientScriptBlocks, false, ref _clientScriptBlocksInScriptTag);
                    break; 
                case ClientAPIRegisterType.ClientScriptBlocksWithoutTags: 
                    RegisterScriptBlock(key, script, ref _registeredClientScriptBlocks, ref _clientScriptBlocks, true, ref _clientScriptBlocksInScriptTag);
                    break; 
                case ClientAPIRegisterType.ClientStartupScripts:
                    RegisterScriptBlock(key, script, ref _registeredClientStartupScripts, ref _clientStartupScripts, false, ref _clientStartupScriptInScriptTag);
                    break;
                case ClientAPIRegisterType.ClientStartupScriptsWithoutTags: 
                    RegisterScriptBlock(key, script, ref _registeredClientStartupScripts, ref _clientStartupScripts, true, ref _clientStartupScriptInScriptTag);
                    break; 
                default: 
                    Debug.Assert(false);
                    break; 
            }

            // If there are any partial caching controls on the stack, forward the call to them
            if (_owner.PartialCachingControlStack != null) { 
                foreach(BasePartialCachingControl c in _owner.PartialCachingControlStack) {
                    c.RegisterScriptBlock(type, key, script); 
                } 
            }
        } 

        private void RegisterScriptBlock(ScriptKey key, string script, ref ListDictionary scriptBlocks, ref ArrayList scriptList, bool needsScriptTags, ref bool inScriptBlock) {
            if (scriptBlocks == null)
                scriptBlocks = new ListDictionary(); 

            if (scriptBlocks[key] == null) { 
                scriptBlocks.Add(key, script); 

                // Now build up the script string 
                if (scriptList == null) {
                    scriptList = new ArrayList();

                    // If the the first script needs script tags, emit a start script tag 
                    if (needsScriptTags) {
                        scriptList.Add(_owner.EnableLegacyRendering ? ClientScriptStartLegacy : ClientScriptStart); 
                    } 
                }
                else { 
                    // If we already have some script
                    if (needsScriptTags) {
                        // If we need script tags and we're not in a script tag, emit a start script tag
                        if (!inScriptBlock) { 
                            scriptList.Add(_owner.EnableLegacyRendering ? ClientScriptStartLegacy : ClientScriptStart);
                        } 
                    } 
                    else {
                        // If we don't need script tags, and we're in a script tag, emit an end script tag 
                        if (inScriptBlock) {
                            scriptList.Add(_owner.EnableLegacyRendering ? ClientScriptEndLegacy : ClientScriptEnd);
                        }
                    } 
                }
 
                scriptList.Add(script); 
                inScriptBlock = needsScriptTags;
            } 
        }

        /// <devdoc>
        ///    <para> 
        ///       Allows controls to keep duplicate blocks of client-side script code from
        ///       being sent to the client. Any script blocks with the same type and key 
        ///       value are considered duplicates. 
        ///    </para>
        /// </devdoc> 
        public void RegisterStartupScript(Type type, string key, string script) {
            RegisterStartupScript(type, key, script, false);
        }
 
        /// <devdoc>
        ///    <para> 
        ///       Allows controls to keep duplicate blocks of client-side script code from 
        ///       being sent to the client. Any script blocks with the same type and key
        ///       value are considered duplicates. 
        ///    </para>
        /// </devdoc>
        public void RegisterStartupScript(Type type, string key, string script, bool addScriptTags) {
            if (type == null) { 
                throw new ArgumentNullException("type");
            } 
 
            if (addScriptTags) {
                RegisterScriptBlock(CreateScriptKey(type, key), script, ClientAPIRegisterType.ClientStartupScriptsWithoutTags); 
            }
            else {
                RegisterScriptBlock(CreateScriptKey(type, key), script, ClientAPIRegisterType.ClientStartupScripts);
            } 
        }
 
        // RegisterStartupScript implementation that supports partial rendering. 
        internal void RegisterStartupScript(Control control, Type type, string key, string script, bool addScriptTags) {
            IScriptManager scriptManager = _owner.ScriptManager; 
            if ((scriptManager != null) && scriptManager.SupportsPartialRendering) {
                scriptManager.RegisterStartupScript(control, type, key, script, addScriptTags);
            }
            else { 
                RegisterStartupScript(type, key, script, addScriptTags);
            } 
        } 

 
        internal void RenderArrayDeclares(HtmlTextWriter writer) {
            if (_registeredArrayDeclares == null || _registeredArrayDeclares.Count == 0) {
                return;
            } 

            writer.Write(_owner.EnableLegacyRendering ? ClientScriptStartLegacy : ClientScriptStart); 
 
            // Write out each array
            IDictionaryEnumerator arrays = _registeredArrayDeclares.GetEnumerator(); 
            while (arrays.MoveNext()) {
                // Write the declaration
                writer.Write("var ");
                writer.Write(arrays.Key); 
                writer.Write(" =  new Array(");
 
                // Write each element 
                IEnumerator elements = ((ArrayList)arrays.Value).GetEnumerator();
                bool first = true; 
                while (elements.MoveNext()) {
                    if (first) {
                        first = false;
                    } 
                    else {
                        writer.Write(", "); 
                    } 
                    writer.Write(elements.Current);
                } 

                // Close the declaration
                writer.WriteLine(");");
            } 

            writer.Write(_owner.EnableLegacyRendering ? ClientScriptEndLegacy : ClientScriptEnd); 
        } 

        internal void RenderExpandoAttribute(HtmlTextWriter writer) { 
            if (_registeredControlsWithExpandoAttributes == null ||
                _registeredControlsWithExpandoAttributes.Count == 0) {
                return;
            } 

            writer.Write(_owner.EnableLegacyRendering ? ClientScriptStartLegacy : ClientScriptStart); 
 
            foreach (DictionaryEntry controlEntry in _registeredControlsWithExpandoAttributes) {
                string controlId = (string) controlEntry.Key; 
                writer.Write("var ");
                writer.Write(controlId);
                writer.Write(" = document.all ? document.all[\"");
                writer.Write(controlId); 
                writer.Write("\"] : document.getElementById(\"");
                writer.Write(controlId); 
                writer.WriteLine("\");"); 

                ListDictionary expandoAttributes = (ListDictionary) controlEntry.Value; 
                Debug.Assert(expandoAttributes != null && expandoAttributes.Count > 0);
                foreach (DictionaryEntry expandoAttribute in expandoAttributes) {
                    writer.Write(controlId);
                    writer.Write("."); 
                    writer.Write(expandoAttribute.Key);
                    if (expandoAttribute.Value == null) { 
                        // VSWhidbey 382151 Render out null string for nulls 
                        writer.WriteLine(" = null;");
                    } 
                    else {
                        writer.Write(" = \"");
                        writer.Write(expandoAttribute.Value);
                        writer.WriteLine("\";"); 
                    }
                } 
            } 

            writer.Write(_owner.EnableLegacyRendering ? ClientScriptEndLegacy : ClientScriptEnd); 
        }

        internal void RenderHiddenFields(HtmlTextWriter writer) {
            if (_registeredHiddenFields == null || _registeredHiddenFields.Count == 0) { 
                return;
            } 
 
            foreach (DictionaryEntry entry in _registeredHiddenFields) {
                string entryKey = (string)entry.Key; 
                if (entryKey == null) {
                    entryKey = String.Empty;
                }
                writer.WriteLine(); 
                writer.Write("<input type=\"hidden\" name=\"");
                writer.Write(entryKey); 
                writer.Write("\" id=\""); 
                writer.Write(entryKey);
                writer.Write("\" value=\""); 
                HttpUtility.HtmlEncode((string)entry.Value, writer);
                writer.Write("\" />");
            }
 
            ClearHiddenFields();
        } 
 
        internal void RenderClientScriptBlocks(HtmlTextWriter writer) {
            if (_clientScriptBlocks != null) { 
                writer.WriteLine();
                // Write out each registered script block
                foreach (string s in _clientScriptBlocks) {
                    writer.Write(s); 
                }
            } 
 
            // Emit the onSubmit function, in necessary
            if (!String.IsNullOrEmpty(_owner.ClientOnSubmitEvent) && _owner.ClientSupportsJavaScript) { 
                // If we were already inside a script tag, don't emit a new open script tag
                if (!_clientScriptBlocksInScriptTag) {
                    writer.Write(_owner.EnableLegacyRendering ? ClientScriptStartLegacy : ClientScriptStart);
                } 

                writer.Write(@"function WebForm_OnSubmit() { 
"); 
                if (_registeredOnSubmitStatements != null) {
                    foreach (string s in _registeredOnSubmitStatements.Values) { 
                        writer.Write(s);
                    }
                }
                writer.WriteLine(@" 
return true;
}"); 
                // We always need to close the script tag 
                writer.Write(_owner.EnableLegacyRendering ? ClientScriptEndLegacy : ClientScriptEnd);
            } 
            // If there was no onSubmit function, close the script tag if needed
            else if (_clientScriptBlocksInScriptTag) {
                writer.Write(_owner.EnableLegacyRendering ? ClientScriptEndLegacy : ClientScriptEnd);
            } 
        }
 
        internal void RenderClientStartupScripts(HtmlTextWriter writer) { 
            if (_clientStartupScripts != null) {
                writer.WriteLine(); 
                // Write out each startup script
                foreach (string s in _clientStartupScripts) {
                    writer.Write(s);
                } 

                // Close the script tag if needed 
                if (_clientStartupScriptInScriptTag) { 
                    writer.Write(_owner.EnableLegacyRendering ? ClientScriptEndLegacy : ClientScriptEnd);
                } 
            }
        }

        internal void RenderWebFormsScript(HtmlTextWriter writer) { 
            writer.Write(IncludeScriptBegin);
            writer.Write(GetWebResourceUrl(_owner, typeof(Page), "WebForms.js", true)); 
            writer.WriteLine(IncludeScriptEnd); 
        }
    } 

    internal class ScriptKey {
        private Type _type;
        private string _key; 
        private bool _isInclude;
 
        internal ScriptKey(Type type, string key) : this(type, key, false) { 
        }
 
        internal ScriptKey(Type type, string key, bool isInclude) {
            _type = type;

            // To treat empty strings the same as nulls, make them null 
            if (String.IsNullOrEmpty(key)) {
                key = null; 
            } 
            _key = key;
            _isInclude = isInclude; 
        }

        public override int GetHashCode() {
            return WebUtil.HashCodeCombiner.CombineHashCodes(_type.GetHashCode(), _key.GetHashCode(), 
                                                             _isInclude.GetHashCode());
        } 
 
        public override bool Equals(object o) {
            ScriptKey key = (ScriptKey)o; 
            return (key._type == _type) && (key._key == _key) && (key._isInclude == _isInclude);
        }
    }
} 
