//------------------------------------------------------------------------------ 
// <copyright file="DbParameter.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Common {
 
    using System;
    using System.ComponentModel;
    using System.Data;
 
#if WINFSInternalOnly
    internal 
#else 
    public
#endif 
    abstract class DbParameter : MarshalByRefObject, IDbDataParameter { // V1.2.3300

        protected DbParameter() : base() {
        } 

        [ 
        Browsable(false), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        RefreshProperties(RefreshProperties.All), 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbParameter_DbType),
        ]
        abstract public DbType DbType { 
            get;
            set; 
        } 

        [ 
        EditorBrowsableAttribute(EditorBrowsableState.Advanced)
        ]
        public abstract void ResetDbType();
 
        [
        DefaultValue(ParameterDirection.Input), 
        RefreshProperties(RefreshProperties.All), 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbParameter_Direction), 
        ]
        abstract public ParameterDirection Direction {
            get;
            set; 
        }
 
        [ 
        Browsable(false),
        DesignOnly(true), 
        EditorBrowsableAttribute(EditorBrowsableState.Never)
        ]
        abstract public Boolean IsNullable {
            get; 
            set;
        } 
 
        [
        DefaultValue(""), 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbParameter_ParameterName),
        ]
        abstract public String ParameterName { 
            get;
            set; 
        } 

        byte IDbDataParameter.Precision { // SqlProjectTracking 17233 
            get {
                return 0;
            }
            set { 
            }
        } 
 
        byte IDbDataParameter.Scale { // SqlProjectTracking 17233
            get { 
                return 0;
            }
            set {
            } 
        }
 
        [ 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbParameter_Size), 
        ]
        abstract public int Size {
            get;
            set; 
        }
 
        [ 
        DefaultValue(""),
        ResCategoryAttribute(Res.DataCategory_Update), 
        ResDescriptionAttribute(Res.DbParameter_SourceColumn),
        ]
        abstract public String SourceColumn {
            get; 
            set;
        } 
 
        [
        DefaultValue(false), 
        EditorBrowsableAttribute(EditorBrowsableState.Advanced),
        RefreshProperties(RefreshProperties.All),
        ResCategoryAttribute(Res.DataCategory_Update),
        ResDescriptionAttribute(Res.DbParameter_SourceColumnNullMapping), 
        ]
        abstract public bool SourceColumnNullMapping { 
            get; 
            set;
        } 

        [
        DefaultValue(DataRowVersion.Current),
        ResCategoryAttribute(Res.DataCategory_Update), 
        ResDescriptionAttribute(Res.DbParameter_SourceVersion),
        ] 
        abstract public DataRowVersion SourceVersion { 
            get;
            set; 
        }

        [
        DefaultValue(null), 
        RefreshProperties(RefreshProperties.All),
        ResCategoryAttribute(Res.DataCategory_Data), 
        ResDescriptionAttribute(Res.DbParameter_Value), 
        ]
        abstract public object Value { 
            get;
            set;
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DbParameter.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Common {
 
    using System;
    using System.ComponentModel;
    using System.Data;
 
#if WINFSInternalOnly
    internal 
#else 
    public
#endif 
    abstract class DbParameter : MarshalByRefObject, IDbDataParameter { // V1.2.3300

        protected DbParameter() : base() {
        } 

        [ 
        Browsable(false), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        RefreshProperties(RefreshProperties.All), 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbParameter_DbType),
        ]
        abstract public DbType DbType { 
            get;
            set; 
        } 

        [ 
        EditorBrowsableAttribute(EditorBrowsableState.Advanced)
        ]
        public abstract void ResetDbType();
 
        [
        DefaultValue(ParameterDirection.Input), 
        RefreshProperties(RefreshProperties.All), 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbParameter_Direction), 
        ]
        abstract public ParameterDirection Direction {
            get;
            set; 
        }
 
        [ 
        Browsable(false),
        DesignOnly(true), 
        EditorBrowsableAttribute(EditorBrowsableState.Never)
        ]
        abstract public Boolean IsNullable {
            get; 
            set;
        } 
 
        [
        DefaultValue(""), 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbParameter_ParameterName),
        ]
        abstract public String ParameterName { 
            get;
            set; 
        } 

        byte IDbDataParameter.Precision { // SqlProjectTracking 17233 
            get {
                return 0;
            }
            set { 
            }
        } 
 
        byte IDbDataParameter.Scale { // SqlProjectTracking 17233
            get { 
                return 0;
            }
            set {
            } 
        }
 
        [ 
        ResCategoryAttribute(Res.DataCategory_Data),
        ResDescriptionAttribute(Res.DbParameter_Size), 
        ]
        abstract public int Size {
            get;
            set; 
        }
 
        [ 
        DefaultValue(""),
        ResCategoryAttribute(Res.DataCategory_Update), 
        ResDescriptionAttribute(Res.DbParameter_SourceColumn),
        ]
        abstract public String SourceColumn {
            get; 
            set;
        } 
 
        [
        DefaultValue(false), 
        EditorBrowsableAttribute(EditorBrowsableState.Advanced),
        RefreshProperties(RefreshProperties.All),
        ResCategoryAttribute(Res.DataCategory_Update),
        ResDescriptionAttribute(Res.DbParameter_SourceColumnNullMapping), 
        ]
        abstract public bool SourceColumnNullMapping { 
            get; 
            set;
        } 

        [
        DefaultValue(DataRowVersion.Current),
        ResCategoryAttribute(Res.DataCategory_Update), 
        ResDescriptionAttribute(Res.DbParameter_SourceVersion),
        ] 
        abstract public DataRowVersion SourceVersion { 
            get;
            set; 
        }

        [
        DefaultValue(null), 
        RefreshProperties(RefreshProperties.All),
        ResCategoryAttribute(Res.DataCategory_Data), 
        ResDescriptionAttribute(Res.DbParameter_Value), 
        ]
        abstract public object Value { 
            get;
            set;
        }
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
