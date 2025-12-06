//------------------------------------------------------------------------------ 
// <copyright file="DBDataPermissionAttribute.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
#if ORACLE
namespace System.Data.OracleClient { 
#else
namespace System.Data.Common {
#endif
 
    using System.ComponentModel;
    using System.Data.Common; 
    using System.Diagnostics; 
    using System.Security;
    using System.Security.Permissions; 

    /* derived class pattern
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )]
    [Serializable] sealed public class XPermissionAttribute : DBDataPermissionAttribute { 
        public XPermissionAttribute(SecurityAction action) : base(action) {
        } 
        override public IPermission CreatePermission() { 
            return new XPermission(this);
        } 
    }
    */

#if ORACLE 
    sealed public partial class OraclePermissionAttribute :  CodeAccessSecurityAttribute {
#else 
    [Serializable(), AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
#if WINFSInternalOnly
    internal 
#else
    public
#endif
    abstract class DBDataPermissionAttribute : CodeAccessSecurityAttribute { // V1.0.3300 
#endif
        private bool _allowBlankPassword;// = false; 
        private string _connectionString;// = ADP.StrEmpty; 
        private string _restrictions;// = ADP.StrEmpty;
        private KeyRestrictionBehavior _behavior;// = KeyRestrictionBehavior.AllowOnly; 

#if !ORACLE
        protected DBDataPermissionAttribute(SecurityAction action) : base(action) {
        } 
#endif
 
        public bool AllowBlankPassword { // V1.0.3300 
            get {
                return _allowBlankPassword; 
            }
            set {
                _allowBlankPassword = value;
            } 
        }
 
        public string ConnectionString { // V1.0.5000 
            get {
                string value = _connectionString; 
                return ((null != value) ? value : String.Empty);
            }
            set {
                _connectionString = value; 
            }
        } 
 
        public KeyRestrictionBehavior KeyRestrictionBehavior { // V1.0.5000, default AllowOnly
            get { 
                return _behavior;
            }
            set {
                switch(value) { 
                case KeyRestrictionBehavior.PreventUsage:
                case KeyRestrictionBehavior.AllowOnly: 
                    _behavior = value; 
                    break;
                default: 
                    throw ADP.InvalidKeyRestrictionBehavior(value);
                }
            }
        } 

        public string KeyRestrictions { // V1.0.5000 
            get { 
                string value = _restrictions;
                return (null != value) ? value : ADP.StrEmpty; 
            }
            set {
                _restrictions = value;
            } 
        }
 
        [ EditorBrowsableAttribute(EditorBrowsableState.Never) ] 
        public bool ShouldSerializeConnectionString() { // V1.2.3300
            return (null != _connectionString); 
        }

        [ EditorBrowsableAttribute(EditorBrowsableState.Never) ]
        public bool ShouldSerializeKeyRestrictions() { // V1.2.3300 
            return (null != _restrictions);
        } 
    } 
}
 
#if !ORACLE
namespace System.Data { // MDAC 83087

[Serializable] 

#if WINFSInternalOnly 
    internal 
#else
    public 
#endif
    enum KeyRestrictionBehavior { // V1.0.5000
        AllowOnly    = 0,
        PreventUsage = 1, 
    }
} 
#endif 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DBDataPermissionAttribute.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
#if ORACLE
namespace System.Data.OracleClient { 
#else
namespace System.Data.Common {
#endif
 
    using System.ComponentModel;
    using System.Data.Common; 
    using System.Diagnostics; 
    using System.Security;
    using System.Security.Permissions; 

    /* derived class pattern
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )]
    [Serializable] sealed public class XPermissionAttribute : DBDataPermissionAttribute { 
        public XPermissionAttribute(SecurityAction action) : base(action) {
        } 
        override public IPermission CreatePermission() { 
            return new XPermission(this);
        } 
    }
    */

#if ORACLE 
    sealed public partial class OraclePermissionAttribute :  CodeAccessSecurityAttribute {
#else 
    [Serializable(), AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
#if WINFSInternalOnly
    internal 
#else
    public
#endif
    abstract class DBDataPermissionAttribute : CodeAccessSecurityAttribute { // V1.0.3300 
#endif
        private bool _allowBlankPassword;// = false; 
        private string _connectionString;// = ADP.StrEmpty; 
        private string _restrictions;// = ADP.StrEmpty;
        private KeyRestrictionBehavior _behavior;// = KeyRestrictionBehavior.AllowOnly; 

#if !ORACLE
        protected DBDataPermissionAttribute(SecurityAction action) : base(action) {
        } 
#endif
 
        public bool AllowBlankPassword { // V1.0.3300 
            get {
                return _allowBlankPassword; 
            }
            set {
                _allowBlankPassword = value;
            } 
        }
 
        public string ConnectionString { // V1.0.5000 
            get {
                string value = _connectionString; 
                return ((null != value) ? value : String.Empty);
            }
            set {
                _connectionString = value; 
            }
        } 
 
        public KeyRestrictionBehavior KeyRestrictionBehavior { // V1.0.5000, default AllowOnly
            get { 
                return _behavior;
            }
            set {
                switch(value) { 
                case KeyRestrictionBehavior.PreventUsage:
                case KeyRestrictionBehavior.AllowOnly: 
                    _behavior = value; 
                    break;
                default: 
                    throw ADP.InvalidKeyRestrictionBehavior(value);
                }
            }
        } 

        public string KeyRestrictions { // V1.0.5000 
            get { 
                string value = _restrictions;
                return (null != value) ? value : ADP.StrEmpty; 
            }
            set {
                _restrictions = value;
            } 
        }
 
        [ EditorBrowsableAttribute(EditorBrowsableState.Never) ] 
        public bool ShouldSerializeConnectionString() { // V1.2.3300
            return (null != _connectionString); 
        }

        [ EditorBrowsableAttribute(EditorBrowsableState.Never) ]
        public bool ShouldSerializeKeyRestrictions() { // V1.2.3300 
            return (null != _restrictions);
        } 
    } 
}
 
#if !ORACLE
namespace System.Data { // MDAC 83087

[Serializable] 

#if WINFSInternalOnly 
    internal 
#else
    public 
#endif
    enum KeyRestrictionBehavior { // V1.0.5000
        AllowOnly    = 0,
        PreventUsage = 1, 
    }
} 
#endif 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
