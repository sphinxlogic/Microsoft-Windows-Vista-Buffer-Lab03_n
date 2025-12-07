//------------------------------------------------------------------------------ 
// <copyright file="ContentDispositionField.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mime 
{ 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.IO;
    using System.Text;
    using System.Globalization; 
    using System.Net.Mail;
 
    public class ContentDisposition 
    {
        string dispositionType; 
        TrackingStringDictionary parameters;
        bool isChanged;
        bool isPersisted;
        string disposition; 

        public ContentDisposition() 
        { 
            isChanged = true;
            disposition = "attachment"; 
            ParseValue();
        }

        /// <summary> 
        /// ctor.
        /// </summary> 
        /// <param name="fieldValue">Unparsed header value.</param> 
        public ContentDisposition(string disposition)
        { 
            if (disposition == null)
                throw new ArgumentNullException("disposition");
            isChanged = true;
            this.disposition = disposition; 
            ParseValue();
        } 
 
        /// <summary>
        /// Gets the disposition type of the content. 
        /// </summary>
        public string DispositionType
        {
            get 
            {
                return dispositionType; 
            } 
            set
            { 
                if (value == null) {
                    throw new ArgumentNullException("value");
                }
 
                if (value == string.Empty) {
                    throw new ArgumentException(SR.GetString(SR.net_emptystringset), "value"); 
                } 

                isChanged = true; 
                dispositionType = value;
            }
        }
 
        public StringDictionary Parameters
        { 
            get 
            {
                if (parameters == null) 
                {
                    parameters = new TrackingStringDictionary();
                }
 
                return parameters;
            } 
        } 

        /// <summary> 
        /// Gets the value of the Filename parameter.
        /// </summary>
        public string FileName
        { 
            get
            { 
                return Parameters["filename"]; 
            }
            set 
            {
                if (value == null || value == string.Empty) {
                    Parameters.Remove("filename");
                } 
                else{
                    Parameters["filename"] = value; 
                } 
            }
        } 

        /// <summary>
        /// Gets the value of the Creation-Date parameter.
        /// </summary> 
        public DateTime CreationDate
        { 
            get 
            {
                string dtValue = Parameters["creation-date"]; 
                if (dtValue == null)
                    return DateTime.MinValue;
                int i = 0;
                return MailBnfHelper.ReadDateTime(dtValue, ref i); 
            }
            set 
            { 
                Parameters["creation-date"] = MailBnfHelper.GetDateTimeString(value, null);
            } 
        }

        /// <summary>
        /// Gets the value of the Modification-Date parameter. 
        /// </summary>
        public DateTime ModificationDate 
        { 
            get
            { 
                string dtValue = Parameters["modification-date"];
                if (dtValue == null)
                    return DateTime.MinValue;
                int i = 0; 
                return MailBnfHelper.ReadDateTime(dtValue, ref i);
            } 
            set 
            {
                Parameters["modification-date"] = MailBnfHelper.GetDateTimeString(value, null); 
            }
        }

        public bool Inline { 
            get {
                return (dispositionType == DispositionTypeNames.Inline); 
            } 
            set {
                isChanged = true; 
                if (value) {
                    dispositionType = DispositionTypeNames.Inline;
                }
                else { 
                    dispositionType = DispositionTypeNames.Attachment;
                } 
            } 
        }
 
        /// <summary>
        /// Gets the value of the Read-Date parameter.
        /// </summary>
        public DateTime ReadDate 
        {
            get 
            { 
                string dtValue = Parameters["read-date"];
                if (dtValue == null) 
                    return DateTime.MinValue;
                int i = 0;
                return MailBnfHelper.ReadDateTime(dtValue, ref i);
            } 
            set
            { 
                Parameters["read-date"] = MailBnfHelper.GetDateTimeString(value, null); 
            }
        } 

        /// <summary>
        /// Gets the value of the Size parameter (-1 if unspecified).
        /// </summary> 
        public long Size
        { 
            get 
            {
                string sizeValue = Parameters["size"]; 
                if (sizeValue == null)
                    return -1;
                else
                    return long.Parse(sizeValue, CultureInfo.InvariantCulture); 
            }
            set 
            { 
                Parameters["size"] = value.ToString(CultureInfo.InvariantCulture);
            } 
        }

        internal void Set(string contentDisposition, HeaderCollection headers) {
            //we don't set ischanged because persistence was already handled 
            //via the headers.
            disposition = contentDisposition; 
            ParseValue(); 
            headers.InternalSet(MailHeaderInfo.GetString(MailHeaderID.ContentDisposition), ToString());
            isPersisted = true; 
        }

        internal void PersistIfNeeded(HeaderCollection headers, bool forcePersist) {
            if (IsChanged || !isPersisted || forcePersist) { 
                headers.InternalSet(MailHeaderInfo.GetString(MailHeaderID.ContentDisposition), ToString());
                isPersisted = true; 
            } 
        }
 
        internal bool IsChanged {
            get {
                return (isChanged || parameters != null && parameters.IsChanged);
            } 
        }
 
 
        public override string ToString()
        { 
            if (disposition == null || isChanged || parameters != null && parameters.IsChanged)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(dispositionType); 
                foreach (string key in Parameters.Keys)
                { 
                    builder.Append("; "); 
                    builder.Append(key);
                    builder.Append('='); 
                    MailBnfHelper.GetTokenOrQuotedString(parameters[key], builder);
                }

                disposition = builder.ToString(); 
                isChanged = false;
                parameters.IsChanged = false; 
                isPersisted = false; 
            }
            return disposition; 
        }


        public override bool Equals(object rparam) { 
            if (rparam == null) {
                return false; 
            } 
            return (String.Compare(ToString(), rparam.ToString(), StringComparison.OrdinalIgnoreCase ) == 0);
        } 

        public override int GetHashCode(){
            return ToString().GetHashCode();
        } 

        void ParseValue() 
        { 
            int offset = 0;
            parameters = new TrackingStringDictionary(); 
            Exception exception = null;

            try{
                dispositionType = MailBnfHelper.ReadToken(disposition, ref offset, null); 

                if(dispositionType == null || dispositionType.Length == 0){ 
                    exception = new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                }
 
                if (exception == null) {
                    while (MailBnfHelper.SkipCFWS(disposition, ref offset))
                    {
                        if (disposition[offset++] != ';') 
                            exception =  new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
 
                        if (!MailBnfHelper.SkipCFWS(disposition, ref offset)) 
                            break;
 
                        string paramAttribute = MailBnfHelper.ReadParameterAttribute(disposition, ref offset, null);
                        string paramValue;
                        if (disposition[offset++] != '='){
                            exception =  new FormatException(SR.GetString(SR.MailHeaderFieldMalformedHeader)); 
                            break;
                        } 
                        if (!MailBnfHelper.SkipCFWS(disposition, ref offset)) 
                            paramValue = string.Empty;
                        else if (disposition[offset] == '"') 
                            paramValue = MailBnfHelper.ReadQuotedString(disposition, ref offset, null);
                        else
                            paramValue = MailBnfHelper.ReadToken(disposition, ref offset, null);
 
                        if(paramAttribute == null || paramValue == null || paramAttribute.Length == 0 || paramValue.Length == 0){
                            exception =  new FormatException(SR.GetString(SR.ContentDispositionInvalid)); 
                            break; 
                        }
 

                        //validate date-time strings

                        if(String.Compare(paramAttribute,"creation-date",StringComparison.OrdinalIgnoreCase) == 0 || 
                           String.Compare(paramAttribute,"modification-date",StringComparison.OrdinalIgnoreCase) == 0 ||
                           String.Compare(paramAttribute,"read-date",StringComparison.OrdinalIgnoreCase) == 0 ){ 
 
                            int i = 0;
                            MailBnfHelper.ReadDateTime(paramValue, ref i); 
                        }
                        parameters.Add(paramAttribute, paramValue);
                    }
                } 
            }
            catch(FormatException){ 
                throw new FormatException(SR.GetString(SR.ContentDispositionInvalid)); 
            }
 
            if (exception != null) {
                throw exception;
            }
            parameters.IsChanged = false; 
        }
    } 
} 
//------------------------------------------------------------------------------ 
// <copyright file="ContentDispositionField.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mime 
{ 
    using System;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.IO;
    using System.Text;
    using System.Globalization; 
    using System.Net.Mail;
 
    public class ContentDisposition 
    {
        string dispositionType; 
        TrackingStringDictionary parameters;
        bool isChanged;
        bool isPersisted;
        string disposition; 

        public ContentDisposition() 
        { 
            isChanged = true;
            disposition = "attachment"; 
            ParseValue();
        }

        /// <summary> 
        /// ctor.
        /// </summary> 
        /// <param name="fieldValue">Unparsed header value.</param> 
        public ContentDisposition(string disposition)
        { 
            if (disposition == null)
                throw new ArgumentNullException("disposition");
            isChanged = true;
            this.disposition = disposition; 
            ParseValue();
        } 
 
        /// <summary>
        /// Gets the disposition type of the content. 
        /// </summary>
        public string DispositionType
        {
            get 
            {
                return dispositionType; 
            } 
            set
            { 
                if (value == null) {
                    throw new ArgumentNullException("value");
                }
 
                if (value == string.Empty) {
                    throw new ArgumentException(SR.GetString(SR.net_emptystringset), "value"); 
                } 

                isChanged = true; 
                dispositionType = value;
            }
        }
 
        public StringDictionary Parameters
        { 
            get 
            {
                if (parameters == null) 
                {
                    parameters = new TrackingStringDictionary();
                }
 
                return parameters;
            } 
        } 

        /// <summary> 
        /// Gets the value of the Filename parameter.
        /// </summary>
        public string FileName
        { 
            get
            { 
                return Parameters["filename"]; 
            }
            set 
            {
                if (value == null || value == string.Empty) {
                    Parameters.Remove("filename");
                } 
                else{
                    Parameters["filename"] = value; 
                } 
            }
        } 

        /// <summary>
        /// Gets the value of the Creation-Date parameter.
        /// </summary> 
        public DateTime CreationDate
        { 
            get 
            {
                string dtValue = Parameters["creation-date"]; 
                if (dtValue == null)
                    return DateTime.MinValue;
                int i = 0;
                return MailBnfHelper.ReadDateTime(dtValue, ref i); 
            }
            set 
            { 
                Parameters["creation-date"] = MailBnfHelper.GetDateTimeString(value, null);
            } 
        }

        /// <summary>
        /// Gets the value of the Modification-Date parameter. 
        /// </summary>
        public DateTime ModificationDate 
        { 
            get
            { 
                string dtValue = Parameters["modification-date"];
                if (dtValue == null)
                    return DateTime.MinValue;
                int i = 0; 
                return MailBnfHelper.ReadDateTime(dtValue, ref i);
            } 
            set 
            {
                Parameters["modification-date"] = MailBnfHelper.GetDateTimeString(value, null); 
            }
        }

        public bool Inline { 
            get {
                return (dispositionType == DispositionTypeNames.Inline); 
            } 
            set {
                isChanged = true; 
                if (value) {
                    dispositionType = DispositionTypeNames.Inline;
                }
                else { 
                    dispositionType = DispositionTypeNames.Attachment;
                } 
            } 
        }
 
        /// <summary>
        /// Gets the value of the Read-Date parameter.
        /// </summary>
        public DateTime ReadDate 
        {
            get 
            { 
                string dtValue = Parameters["read-date"];
                if (dtValue == null) 
                    return DateTime.MinValue;
                int i = 0;
                return MailBnfHelper.ReadDateTime(dtValue, ref i);
            } 
            set
            { 
                Parameters["read-date"] = MailBnfHelper.GetDateTimeString(value, null); 
            }
        } 

        /// <summary>
        /// Gets the value of the Size parameter (-1 if unspecified).
        /// </summary> 
        public long Size
        { 
            get 
            {
                string sizeValue = Parameters["size"]; 
                if (sizeValue == null)
                    return -1;
                else
                    return long.Parse(sizeValue, CultureInfo.InvariantCulture); 
            }
            set 
            { 
                Parameters["size"] = value.ToString(CultureInfo.InvariantCulture);
            } 
        }

        internal void Set(string contentDisposition, HeaderCollection headers) {
            //we don't set ischanged because persistence was already handled 
            //via the headers.
            disposition = contentDisposition; 
            ParseValue(); 
            headers.InternalSet(MailHeaderInfo.GetString(MailHeaderID.ContentDisposition), ToString());
            isPersisted = true; 
        }

        internal void PersistIfNeeded(HeaderCollection headers, bool forcePersist) {
            if (IsChanged || !isPersisted || forcePersist) { 
                headers.InternalSet(MailHeaderInfo.GetString(MailHeaderID.ContentDisposition), ToString());
                isPersisted = true; 
            } 
        }
 
        internal bool IsChanged {
            get {
                return (isChanged || parameters != null && parameters.IsChanged);
            } 
        }
 
 
        public override string ToString()
        { 
            if (disposition == null || isChanged || parameters != null && parameters.IsChanged)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(dispositionType); 
                foreach (string key in Parameters.Keys)
                { 
                    builder.Append("; "); 
                    builder.Append(key);
                    builder.Append('='); 
                    MailBnfHelper.GetTokenOrQuotedString(parameters[key], builder);
                }

                disposition = builder.ToString(); 
                isChanged = false;
                parameters.IsChanged = false; 
                isPersisted = false; 
            }
            return disposition; 
        }


        public override bool Equals(object rparam) { 
            if (rparam == null) {
                return false; 
            } 
            return (String.Compare(ToString(), rparam.ToString(), StringComparison.OrdinalIgnoreCase ) == 0);
        } 

        public override int GetHashCode(){
            return ToString().GetHashCode();
        } 

        void ParseValue() 
        { 
            int offset = 0;
            parameters = new TrackingStringDictionary(); 
            Exception exception = null;

            try{
                dispositionType = MailBnfHelper.ReadToken(disposition, ref offset, null); 

                if(dispositionType == null || dispositionType.Length == 0){ 
                    exception = new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                }
 
                if (exception == null) {
                    while (MailBnfHelper.SkipCFWS(disposition, ref offset))
                    {
                        if (disposition[offset++] != ';') 
                            exception =  new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
 
                        if (!MailBnfHelper.SkipCFWS(disposition, ref offset)) 
                            break;
 
                        string paramAttribute = MailBnfHelper.ReadParameterAttribute(disposition, ref offset, null);
                        string paramValue;
                        if (disposition[offset++] != '='){
                            exception =  new FormatException(SR.GetString(SR.MailHeaderFieldMalformedHeader)); 
                            break;
                        } 
                        if (!MailBnfHelper.SkipCFWS(disposition, ref offset)) 
                            paramValue = string.Empty;
                        else if (disposition[offset] == '"') 
                            paramValue = MailBnfHelper.ReadQuotedString(disposition, ref offset, null);
                        else
                            paramValue = MailBnfHelper.ReadToken(disposition, ref offset, null);
 
                        if(paramAttribute == null || paramValue == null || paramAttribute.Length == 0 || paramValue.Length == 0){
                            exception =  new FormatException(SR.GetString(SR.ContentDispositionInvalid)); 
                            break; 
                        }
 

                        //validate date-time strings

                        if(String.Compare(paramAttribute,"creation-date",StringComparison.OrdinalIgnoreCase) == 0 || 
                           String.Compare(paramAttribute,"modification-date",StringComparison.OrdinalIgnoreCase) == 0 ||
                           String.Compare(paramAttribute,"read-date",StringComparison.OrdinalIgnoreCase) == 0 ){ 
 
                            int i = 0;
                            MailBnfHelper.ReadDateTime(paramValue, ref i); 
                        }
                        parameters.Add(paramAttribute, paramValue);
                    }
                } 
            }
            catch(FormatException){ 
                throw new FormatException(SR.GetString(SR.ContentDispositionInvalid)); 
            }
 
            if (exception != null) {
                throw exception;
            }
            parameters.IsChanged = false; 
        }
    } 
} 
