//------------------------------------------------------------------------------ 
// <copyright file="MailAddressCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mail 
{ 
    using System;
    using System.Collections; 
    using System.Collections.ObjectModel;
    using System.Text;
    using System.Net.Mime;
 

    public class MailAddressCollection: Collection<MailAddress> { 
        public MailAddressCollection(){ 
        }
 
        public void Add(string addresses) {
            if (addresses == null) {
                throw new ArgumentNullException("addresses");
            } 
            if (addresses == string.Empty) {
                throw new ArgumentException(SR.GetString(SR.net_emptystringcall, "addresses"), "addresses"); 
            } 

            ParseValue(addresses); 
        }

        /*
        // Consider removing. 
        internal void Populate(string[] addresses) {
            if (addresses == null) { 
                throw new ArgumentNullException("addresses"); 
            }
            if (addresses.Length == 0) { 
                throw new ArgumentException(SR.GetString(SR.net_emptystringcall, "addresses"), "addresses");
            }

            ParseValue(addresses); 
        }
        */ 
 
        protected override void SetItem(int index, MailAddress item){
              if(item==null) { 
                  throw new ArgumentNullException("item");
              }

              base.SetItem(index,item); 
        }
 
        protected override void InsertItem(int index, MailAddress item){ 
              if(item==null){
                   throw new ArgumentNullException("item"); 
              }

              base.InsertItem(index,item);
        } 

        /* 
        // Consider removing. 
        internal bool IsChanged {
            get { 
                return this.isChanged;
            }
            set {
                this.isChanged = value; 
            }
        } 
        */ 

        /* 
        // Consider removing.
        internal void ParseValue(string[] addresses) {
            for (int i = 0; i < addresses.Length; i++) {
                int offset = 0; 
                MailAddress address = MailBnfHelper.ReadMailAddress(addresses[i],ref offset);
 
                if (address == null) 
                    break;
 
                this.Add(address);
            }
        }
        */ 

        internal void ParseValue(string addresses) { 
            for (int offset = 0; offset < addresses.Length; offset++) { 
                MailAddress address = MailBnfHelper.ReadMailAddress(addresses, ref offset);
 
                if (address == null)
                    break;

                this.Add(address); 
                if (!MailBnfHelper.SkipCFWS(addresses, ref offset) || addresses[offset] != ',')
                    break; 
            } 
        }
 
        internal string ToEncodedString() {
            bool first = true;
            StringBuilder builder = new StringBuilder();
 
            foreach (MailAddress address in this) {
                if (!first) { 
                    builder.Append(", "); 
                }
 
                builder.Append(address.ToEncodedString());
                first = false;
            }
 
            return builder.ToString();;
        } 
 

        public override string ToString() { 
            bool first = true;
            StringBuilder builder = new StringBuilder();

            foreach (MailAddress address in this) { 
                if (!first) {
                    builder.Append(", "); 
                } 

                builder.Append(address.ToString()); 
                first = false;
            }

            return builder.ToString();; 
        }
 
        /* 
        // Consider removing.
        internal string ToAddressString() { 
            bool first = true;
            StringBuilder builder = new StringBuilder();

            foreach (MailAddress address in this) { 
                if (!first) {
                    builder.Append(", "); 
                } 

                builder.Append(address.Address); 
                first = false;
            }

            return builder.ToString();; 
        }
        */ 
    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="MailAddressCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mail 
{ 
    using System;
    using System.Collections; 
    using System.Collections.ObjectModel;
    using System.Text;
    using System.Net.Mime;
 

    public class MailAddressCollection: Collection<MailAddress> { 
        public MailAddressCollection(){ 
        }
 
        public void Add(string addresses) {
            if (addresses == null) {
                throw new ArgumentNullException("addresses");
            } 
            if (addresses == string.Empty) {
                throw new ArgumentException(SR.GetString(SR.net_emptystringcall, "addresses"), "addresses"); 
            } 

            ParseValue(addresses); 
        }

        /*
        // Consider removing. 
        internal void Populate(string[] addresses) {
            if (addresses == null) { 
                throw new ArgumentNullException("addresses"); 
            }
            if (addresses.Length == 0) { 
                throw new ArgumentException(SR.GetString(SR.net_emptystringcall, "addresses"), "addresses");
            }

            ParseValue(addresses); 
        }
        */ 
 
        protected override void SetItem(int index, MailAddress item){
              if(item==null) { 
                  throw new ArgumentNullException("item");
              }

              base.SetItem(index,item); 
        }
 
        protected override void InsertItem(int index, MailAddress item){ 
              if(item==null){
                   throw new ArgumentNullException("item"); 
              }

              base.InsertItem(index,item);
        } 

        /* 
        // Consider removing. 
        internal bool IsChanged {
            get { 
                return this.isChanged;
            }
            set {
                this.isChanged = value; 
            }
        } 
        */ 

        /* 
        // Consider removing.
        internal void ParseValue(string[] addresses) {
            for (int i = 0; i < addresses.Length; i++) {
                int offset = 0; 
                MailAddress address = MailBnfHelper.ReadMailAddress(addresses[i],ref offset);
 
                if (address == null) 
                    break;
 
                this.Add(address);
            }
        }
        */ 

        internal void ParseValue(string addresses) { 
            for (int offset = 0; offset < addresses.Length; offset++) { 
                MailAddress address = MailBnfHelper.ReadMailAddress(addresses, ref offset);
 
                if (address == null)
                    break;

                this.Add(address); 
                if (!MailBnfHelper.SkipCFWS(addresses, ref offset) || addresses[offset] != ',')
                    break; 
            } 
        }
 
        internal string ToEncodedString() {
            bool first = true;
            StringBuilder builder = new StringBuilder();
 
            foreach (MailAddress address in this) {
                if (!first) { 
                    builder.Append(", "); 
                }
 
                builder.Append(address.ToEncodedString());
                first = false;
            }
 
            return builder.ToString();;
        } 
 

        public override string ToString() { 
            bool first = true;
            StringBuilder builder = new StringBuilder();

            foreach (MailAddress address in this) { 
                if (!first) {
                    builder.Append(", "); 
                } 

                builder.Append(address.ToString()); 
                first = false;
            }

            return builder.ToString();; 
        }
 
        /* 
        // Consider removing.
        internal string ToAddressString() { 
            bool first = true;
            StringBuilder builder = new StringBuilder();

            foreach (MailAddress address in this) { 
                if (!first) {
                    builder.Append(", "); 
                } 

                builder.Append(address.Address); 
                first = false;
            }

            return builder.ToString();; 
        }
        */ 
    } 
}
