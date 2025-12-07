//------------------------------------------------------------------------------ 
// <copyright file="DesignBinding.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System;
    using System.ComponentModel; 
    using System.Drawing.Design;
    using System.Globalization;

    [ 
    Editor("System.Windows.Forms.Design.DesignBindingEditor, " + AssemblyRef.SystemDesign,typeof(UITypeEditor))
    ] 
    internal class DesignBinding { 

        private object dataSource; 
        private string dataMember;

        public static DesignBinding Null = new DesignBinding(null, null);
 
        public DesignBinding(object dataSource, string dataMember) {
            this.dataSource = dataSource; 
            this.dataMember = dataMember; 
        }
 
        public bool IsNull {
            get {
                return (dataSource == null);
            } 
        }
 
        public object DataSource { 
            get {
                return dataSource; 
            }
        }

        public string DataMember { 
            get {
                return dataMember; 
            } 
        }
 
        public string DataField {
            get {
                if (String.IsNullOrEmpty(dataMember)) {
                    return String.Empty; 
                }
 
                int lastDot = dataMember.LastIndexOf("."); 
                if (lastDot == -1) {
                    return dataMember; 
                } else {
                    return dataMember.Substring(lastDot+1);
                }
            } 
        }
 
        public bool Equals(object dataSource, string dataMember) { 
            return (dataSource == this.dataSource && String.Equals(dataMember, this.dataMember,  StringComparison.OrdinalIgnoreCase));
        } 
    }

}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignBinding.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System;
    using System.ComponentModel; 
    using System.Drawing.Design;
    using System.Globalization;

    [ 
    Editor("System.Windows.Forms.Design.DesignBindingEditor, " + AssemblyRef.SystemDesign,typeof(UITypeEditor))
    ] 
    internal class DesignBinding { 

        private object dataSource; 
        private string dataMember;

        public static DesignBinding Null = new DesignBinding(null, null);
 
        public DesignBinding(object dataSource, string dataMember) {
            this.dataSource = dataSource; 
            this.dataMember = dataMember; 
        }
 
        public bool IsNull {
            get {
                return (dataSource == null);
            } 
        }
 
        public object DataSource { 
            get {
                return dataSource; 
            }
        }

        public string DataMember { 
            get {
                return dataMember; 
            } 
        }
 
        public string DataField {
            get {
                if (String.IsNullOrEmpty(dataMember)) {
                    return String.Empty; 
                }
 
                int lastDot = dataMember.LastIndexOf("."); 
                if (lastDot == -1) {
                    return dataMember; 
                } else {
                    return dataMember.Substring(lastDot+1);
                }
            } 
        }
 
        public bool Equals(object dataSource, string dataMember) { 
            return (dataSource == this.dataSource && String.Equals(dataMember, this.dataMember,  StringComparison.OrdinalIgnoreCase));
        } 
    }

}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
