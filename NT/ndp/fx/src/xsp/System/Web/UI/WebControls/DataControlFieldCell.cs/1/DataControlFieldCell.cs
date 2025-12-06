//------------------------------------------------------------------------------ 
// <copyright file="DataControlFieldCell.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Security.Permissions; 
    using System.Web.UI.WebControls;


    /// <devdoc> 
    /// <para>Creates a special cell that is contained within a DataControlField.</para>
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class DataControlFieldCell : TableCell { 
        DataControlField _containingField;


        public DataControlFieldCell(DataControlField containingField) { 
            _containingField = containingField;
        } 
 

        protected DataControlFieldCell(HtmlTextWriterTag tagKey, DataControlField containingField) : base(tagKey) { 
            _containingField = containingField;
        }

 
        public DataControlField ContainingField {
            get { 
                return _containingField; 
            }
        } 
    }
}

 
//------------------------------------------------------------------------------ 
// <copyright file="DataControlFieldCell.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Security.Permissions; 
    using System.Web.UI.WebControls;


    /// <devdoc> 
    /// <para>Creates a special cell that is contained within a DataControlField.</para>
    /// </devdoc> 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class DataControlFieldCell : TableCell { 
        DataControlField _containingField;


        public DataControlFieldCell(DataControlField containingField) { 
            _containingField = containingField;
        } 
 

        protected DataControlFieldCell(HtmlTextWriterTag tagKey, DataControlField containingField) : base(tagKey) { 
            _containingField = containingField;
        }

 
        public DataControlField ContainingField {
            get { 
                return _containingField; 
            }
        } 
    }
}

 
