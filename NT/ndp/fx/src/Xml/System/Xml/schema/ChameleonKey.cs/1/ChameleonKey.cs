//------------------------------------------------------------------------------ 
// <copyright file="XmlSchemaExternal.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Xml.Schema { 

    using System.Collections; 
    using System.ComponentModel;
    using System.Xml.Serialization;

    // Case insensitive file name key for use in a hashtable. 

    internal class ChameleonKey { 
        internal string targetNS; 
        internal Uri chameleonLocation;
        int hashCode; 

        public ChameleonKey(string ns, Uri location) {
            targetNS = ns;
            chameleonLocation = location; 
        }
 
        public override int GetHashCode() { 
            if (hashCode == 0) {
                hashCode = targetNS.GetHashCode() + chameleonLocation.GetHashCode(); 
            }
            return hashCode;
        }
 
        public override bool Equals(object obj) {
            if (Ref.ReferenceEquals(this,obj)) { 
                return true; 
            }
            ChameleonKey cKey = obj as ChameleonKey; 
            if (cKey != null) {
                return this.targetNS.Equals(cKey.targetNS) && this.chameleonLocation.Equals(cKey.chameleonLocation);
            }
            return false; 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="XmlSchemaExternal.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Xml.Schema { 

    using System.Collections; 
    using System.ComponentModel;
    using System.Xml.Serialization;

    // Case insensitive file name key for use in a hashtable. 

    internal class ChameleonKey { 
        internal string targetNS; 
        internal Uri chameleonLocation;
        int hashCode; 

        public ChameleonKey(string ns, Uri location) {
            targetNS = ns;
            chameleonLocation = location; 
        }
 
        public override int GetHashCode() { 
            if (hashCode == 0) {
                hashCode = targetNS.GetHashCode() + chameleonLocation.GetHashCode(); 
            }
            return hashCode;
        }
 
        public override bool Equals(object obj) {
            if (Ref.ReferenceEquals(this,obj)) { 
                return true; 
            }
            ChameleonKey cKey = obj as ChameleonKey; 
            if (cKey != null) {
                return this.targetNS.Equals(cKey.targetNS) && this.chameleonLocation.Equals(cKey.chameleonLocation);
            }
            return false; 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
