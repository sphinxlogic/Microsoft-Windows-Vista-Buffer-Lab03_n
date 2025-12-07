//------------------------------------------------------------------------------ 
// <copyright file="MailAddress.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mail 
{ 
    using System;
    using System.Text; 
    using System.Net.Mime;

    public class MailAddress
    { 
        string displayName;
        Encoding displayNameEncoding; 
        string encodedDisplayName; 
        string address;
        string fullAddress; 
        string userName;
        string host;

 

        //for internal use only for the bnfhelper. 
        internal MailAddress(string address, string encodedDisplayName, uint bogusParam){ 
            this.encodedDisplayName = encodedDisplayName;
            GetParts(address);  // the address was already validated before this is called. 
        }

        public MailAddress(string address):this(address,null,null){
        } 

 
        public MailAddress(string address, string displayName):this(address,displayName,null) { 
        }
 

        //we shouldn’t encourage the use of invalid email address in our apis.
        //This is important, not only for our own sanity when parsing strings,
        //but to prevent propagating RFC violations.  We are still experiencing 
        //the pain introduced by the Uri class for not being more strict.
 
        //the only way we can reasonably encode the displayName is if is is given to us 
        //seperately.  Otherwise, if its passed in as a full mail address, we can only assume
        //its in the proper format 

        public MailAddress(string address, string displayName, Encoding displayNameEncoding) {
            if (address == null){
                throw new ArgumentNullException("address"); 
            }
            if (address == String.Empty){ 
                throw new ArgumentException(SR.GetString(SR.net_emptystringcall,"address"), "address"); 
            }
 
            this.displayNameEncoding = displayNameEncoding;
            this.displayName = displayName;

 
            ParseValue(address);
 
            if(this.displayName != null && this.displayName != string.Empty){ 
                if(this.displayName[0] == '\"' && this.displayName[this.displayName.Length - 1] == '\"'){
                    this.displayName = this.displayName.Substring(1,this.displayName.Length -2); 
                }
                this.displayName = this.displayName.Trim();
            }
 
            //if a different display name was provided, then override the existing one
            if(this.displayName != null && this.displayName.Length > 0){ 
 
                //encode if necessary, or if an encoder was given.
                if (!MimeBasePart.IsAscii(this.displayName,false) || this.displayNameEncoding != null) { 
                    if (this.displayNameEncoding == null){
                        this.displayNameEncoding = Encoding.GetEncoding(MimeBasePart.defaultCharSet);
                    }
                    encodedDisplayName = MimeBasePart.EncodeHeaderValue(this.displayName, this.displayNameEncoding, MimeBasePart.ShouldUseBase64Encoding(displayNameEncoding)); 
                    //okay, time to validate it
                    StringBuilder builder = new StringBuilder(); 
                    int offset = 0; 

                    //this means the displayname was given seperately, so we don't need to look 
                    //for quotes, though we should treat it as a quoted string.
                    MailBnfHelper.ReadQuotedString(encodedDisplayName,ref offset,builder,true);
                    encodedDisplayName = builder.ToString();  //set to remove any comments
                } 
                else{
                    encodedDisplayName = this.displayName; 
                } 
            }
        } 


        public string DisplayName
        { 
            get
            { 
                if (displayName == null) { 
                    if(encodedDisplayName != null && encodedDisplayName.Length > 0){
                        displayName = MimeBasePart.DecodeHeaderValue(encodedDisplayName); 
                    }
                    else{
                        displayName = String.Empty;
                    } 
                }
 
                return displayName; 
            }
        } 


        public string User
        { 
            get
            { 
                return this.userName; 
            }
        } 

        public string Host
        {
            get 
            {
                return this.host; 
            } 
        }
 
        public string Address
        {
            get
            { 
                if (this.address == null)
                    CombineParts(); 
                return this.address; 
            }
        } 



        internal string SmtpAddress{ 
            get{
                StringBuilder builder = new StringBuilder(); 
                builder.Append('<'); 
                builder.Append(Address);
                builder.Append('>'); 
                return builder.ToString();
            }
        }
 
        internal string ToEncodedString(){
            if (fullAddress == null){ 
                if (encodedDisplayName != null && encodedDisplayName!=string.Empty) { 
                    StringBuilder builder = new StringBuilder();
                    MailBnfHelper.GetDotAtomOrQuotedString(encodedDisplayName, builder); 
                    builder.Append(" <");
                    builder.Append(Address);
                    builder.Append('>');
                    fullAddress = builder.ToString(); 
                }
                else{ 
                    fullAddress = Address; 
                }
            } 
            return fullAddress;
        }

        public override string ToString() { 

            if (fullAddress == null){ 
                if (encodedDisplayName != null && encodedDisplayName!=string.Empty) { 
                    StringBuilder builder = new StringBuilder();
                    builder.Append('"'); 
                    builder.Append(DisplayName);
                    builder.Append("\" <");
                    builder.Append(Address);
                    builder.Append('>'); 
                    fullAddress = builder.ToString();
                } 
                else{ 
                    fullAddress = Address;
                } 
            }
            return fullAddress;
        }
 

        public override bool Equals(object value) { 
            if (value == null) { 
                return false;
            } 
            return ToString().Equals(value.ToString(),StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode(){ 
            return ToString().GetHashCode();
        } 
 
        void GetParts(string address)
        { 
            if (address == null)
                return;
            int atIndex = address.IndexOf('@');
            if (atIndex < 0) 
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
            userName = address.Substring(0, atIndex); 
            host = address.Substring(atIndex+1); 
        }
 
        void ParseValue(string address)
        {

            //first, split out the display string if it exists 
            string displayPart = null;
            int index = address.IndexOf('\"'); 
            if(index > 0){ 
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
            } 
            else if (index == 0){
                index = address.IndexOf('\"',1);
                if(index < 0){
                    throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
                }
                displayPart = address.Substring(1,index-1); 
                if(address.Length == index+1){ 
                    throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
                } 
                address = address.Substring(index+1);
            }

            if (displayPart == null) { 
                index = address.IndexOf('<');
                if(index > 0){ 
                    displayPart = address.Substring(0,index); 
                    address = address.Substring(index);
                } 
            }

            if(displayName == null){
                displayName = displayPart; 
            }
 
            index = 0; 
            address = MailBnfHelper.ReadMailAddress(address, ref index, out encodedDisplayName);
            GetParts(address); 
        }


        void CombineParts() 
        {
            if (userName == null || host == null) 
                return; 

            StringBuilder builder = new StringBuilder(); 
            MailBnfHelper.GetDotAtomOrQuotedString(User, builder);
            builder.Append('@');
            MailBnfHelper.GetDotAtomOrDomainLiteral(Host, builder);
            address = builder.ToString(); 
        }
    } 
} 
//------------------------------------------------------------------------------ 
// <copyright file="MailAddress.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mail 
{ 
    using System;
    using System.Text; 
    using System.Net.Mime;

    public class MailAddress
    { 
        string displayName;
        Encoding displayNameEncoding; 
        string encodedDisplayName; 
        string address;
        string fullAddress; 
        string userName;
        string host;

 

        //for internal use only for the bnfhelper. 
        internal MailAddress(string address, string encodedDisplayName, uint bogusParam){ 
            this.encodedDisplayName = encodedDisplayName;
            GetParts(address);  // the address was already validated before this is called. 
        }

        public MailAddress(string address):this(address,null,null){
        } 

 
        public MailAddress(string address, string displayName):this(address,displayName,null) { 
        }
 

        //we shouldn’t encourage the use of invalid email address in our apis.
        //This is important, not only for our own sanity when parsing strings,
        //but to prevent propagating RFC violations.  We are still experiencing 
        //the pain introduced by the Uri class for not being more strict.
 
        //the only way we can reasonably encode the displayName is if is is given to us 
        //seperately.  Otherwise, if its passed in as a full mail address, we can only assume
        //its in the proper format 

        public MailAddress(string address, string displayName, Encoding displayNameEncoding) {
            if (address == null){
                throw new ArgumentNullException("address"); 
            }
            if (address == String.Empty){ 
                throw new ArgumentException(SR.GetString(SR.net_emptystringcall,"address"), "address"); 
            }
 
            this.displayNameEncoding = displayNameEncoding;
            this.displayName = displayName;

 
            ParseValue(address);
 
            if(this.displayName != null && this.displayName != string.Empty){ 
                if(this.displayName[0] == '\"' && this.displayName[this.displayName.Length - 1] == '\"'){
                    this.displayName = this.displayName.Substring(1,this.displayName.Length -2); 
                }
                this.displayName = this.displayName.Trim();
            }
 
            //if a different display name was provided, then override the existing one
            if(this.displayName != null && this.displayName.Length > 0){ 
 
                //encode if necessary, or if an encoder was given.
                if (!MimeBasePart.IsAscii(this.displayName,false) || this.displayNameEncoding != null) { 
                    if (this.displayNameEncoding == null){
                        this.displayNameEncoding = Encoding.GetEncoding(MimeBasePart.defaultCharSet);
                    }
                    encodedDisplayName = MimeBasePart.EncodeHeaderValue(this.displayName, this.displayNameEncoding, MimeBasePart.ShouldUseBase64Encoding(displayNameEncoding)); 
                    //okay, time to validate it
                    StringBuilder builder = new StringBuilder(); 
                    int offset = 0; 

                    //this means the displayname was given seperately, so we don't need to look 
                    //for quotes, though we should treat it as a quoted string.
                    MailBnfHelper.ReadQuotedString(encodedDisplayName,ref offset,builder,true);
                    encodedDisplayName = builder.ToString();  //set to remove any comments
                } 
                else{
                    encodedDisplayName = this.displayName; 
                } 
            }
        } 


        public string DisplayName
        { 
            get
            { 
                if (displayName == null) { 
                    if(encodedDisplayName != null && encodedDisplayName.Length > 0){
                        displayName = MimeBasePart.DecodeHeaderValue(encodedDisplayName); 
                    }
                    else{
                        displayName = String.Empty;
                    } 
                }
 
                return displayName; 
            }
        } 


        public string User
        { 
            get
            { 
                return this.userName; 
            }
        } 

        public string Host
        {
            get 
            {
                return this.host; 
            } 
        }
 
        public string Address
        {
            get
            { 
                if (this.address == null)
                    CombineParts(); 
                return this.address; 
            }
        } 



        internal string SmtpAddress{ 
            get{
                StringBuilder builder = new StringBuilder(); 
                builder.Append('<'); 
                builder.Append(Address);
                builder.Append('>'); 
                return builder.ToString();
            }
        }
 
        internal string ToEncodedString(){
            if (fullAddress == null){ 
                if (encodedDisplayName != null && encodedDisplayName!=string.Empty) { 
                    StringBuilder builder = new StringBuilder();
                    MailBnfHelper.GetDotAtomOrQuotedString(encodedDisplayName, builder); 
                    builder.Append(" <");
                    builder.Append(Address);
                    builder.Append('>');
                    fullAddress = builder.ToString(); 
                }
                else{ 
                    fullAddress = Address; 
                }
            } 
            return fullAddress;
        }

        public override string ToString() { 

            if (fullAddress == null){ 
                if (encodedDisplayName != null && encodedDisplayName!=string.Empty) { 
                    StringBuilder builder = new StringBuilder();
                    builder.Append('"'); 
                    builder.Append(DisplayName);
                    builder.Append("\" <");
                    builder.Append(Address);
                    builder.Append('>'); 
                    fullAddress = builder.ToString();
                } 
                else{ 
                    fullAddress = Address;
                } 
            }
            return fullAddress;
        }
 

        public override bool Equals(object value) { 
            if (value == null) { 
                return false;
            } 
            return ToString().Equals(value.ToString(),StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode(){ 
            return ToString().GetHashCode();
        } 
 
        void GetParts(string address)
        { 
            if (address == null)
                return;
            int atIndex = address.IndexOf('@');
            if (atIndex < 0) 
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
            userName = address.Substring(0, atIndex); 
            host = address.Substring(atIndex+1); 
        }
 
        void ParseValue(string address)
        {

            //first, split out the display string if it exists 
            string displayPart = null;
            int index = address.IndexOf('\"'); 
            if(index > 0){ 
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
            } 
            else if (index == 0){
                index = address.IndexOf('\"',1);
                if(index < 0){
                    throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
                }
                displayPart = address.Substring(1,index-1); 
                if(address.Length == index+1){ 
                    throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
                } 
                address = address.Substring(index+1);
            }

            if (displayPart == null) { 
                index = address.IndexOf('<');
                if(index > 0){ 
                    displayPart = address.Substring(0,index); 
                    address = address.Substring(index);
                } 
            }

            if(displayName == null){
                displayName = displayPart; 
            }
 
            index = 0; 
            address = MailBnfHelper.ReadMailAddress(address, ref index, out encodedDisplayName);
            GetParts(address); 
        }


        void CombineParts() 
        {
            if (userName == null || host == null) 
                return; 

            StringBuilder builder = new StringBuilder(); 
            MailBnfHelper.GetDotAtomOrQuotedString(User, builder);
            builder.Append('@');
            MailBnfHelper.GetDotAtomOrDomainLiteral(Host, builder);
            address = builder.ToString(); 
        }
    } 
} 
