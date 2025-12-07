//------------------------------------------------------------------------------ 
// <copyright file="TableDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using Microsoft.Win32;
    using System.Diagnostics;
    using System.Web.UI.WebControls; 

    /// <include file='doc\TableDesigner.uex' path='docs/doc[@for="TableDesigner"]/*' /> 
    /// <devdoc> 
    ///    <para>
    ///       The designer for the <see cref='System.Web.UI.WebControls.Table'/> 
    ///       web control.
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    [SupportsPreviewControl(true)]
    public class TableDesigner : ControlDesigner { 
        public override string GetDesignTimeHtml() { 
            Table table = (Table)ViewControl;
            TableRowCollection rows = table.Rows; 
            bool emptyTable = (rows.Count == 0);
            bool emptyRows = false;

            if (emptyTable) { 
                TableRow row = new TableRow();
 
                rows.Add(row); 

                TableCell cell = new TableCell(); 

                cell.Text = "###";
                rows[0].Cells.Add(cell);
            } 
            else {
                emptyRows = true; 
                for (int i = 0; i < rows.Count; i++) { 
                    if (rows[i].Cells.Count != 0) {
                        emptyRows = false; 
                        break;
                    }
                }
 
                if (emptyRows == true) {
                    TableCell cell = new TableCell(); 
 
                    cell.Text = "###";
                    rows[0].Cells.Add(cell); 
                }
            }

            if (emptyTable == false) { 
                // rows and cells were defined by the user, but if the cells are empty
                // then something needs to be done about that, so they are visible 
                foreach (TableRow row in rows) { 
                    foreach (TableCell cell in row.Cells) {
                        if ((cell.Text.Length == 0) && (cell.HasControls() == false)) { 
                            cell.Text = "###";
                        }
                    }
                } 
            }
 
            return base.GetDesignTimeHtml(); 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TableDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using Microsoft.Win32;
    using System.Diagnostics;
    using System.Web.UI.WebControls; 

    /// <include file='doc\TableDesigner.uex' path='docs/doc[@for="TableDesigner"]/*' /> 
    /// <devdoc> 
    ///    <para>
    ///       The designer for the <see cref='System.Web.UI.WebControls.Table'/> 
    ///       web control.
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    [SupportsPreviewControl(true)]
    public class TableDesigner : ControlDesigner { 
        public override string GetDesignTimeHtml() { 
            Table table = (Table)ViewControl;
            TableRowCollection rows = table.Rows; 
            bool emptyTable = (rows.Count == 0);
            bool emptyRows = false;

            if (emptyTable) { 
                TableRow row = new TableRow();
 
                rows.Add(row); 

                TableCell cell = new TableCell(); 

                cell.Text = "###";
                rows[0].Cells.Add(cell);
            } 
            else {
                emptyRows = true; 
                for (int i = 0; i < rows.Count; i++) { 
                    if (rows[i].Cells.Count != 0) {
                        emptyRows = false; 
                        break;
                    }
                }
 
                if (emptyRows == true) {
                    TableCell cell = new TableCell(); 
 
                    cell.Text = "###";
                    rows[0].Cells.Add(cell); 
                }
            }

            if (emptyTable == false) { 
                // rows and cells were defined by the user, but if the cells are empty
                // then something needs to be done about that, so they are visible 
                foreach (TableRow row in rows) { 
                    foreach (TableCell cell in row.Cells) {
                        if ((cell.Text.Length == 0) && (cell.HasControls() == false)) { 
                            cell.Text = "###";
                        }
                    }
                } 
            }
 
            return base.GetDesignTimeHtml(); 
        }
    } 
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
