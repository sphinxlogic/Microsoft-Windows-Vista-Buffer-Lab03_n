//------------------------------------------------------------------------------ 
// <copyright file="ListControlActionList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics;
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
 
    /// <include file='doc\ListControlActionList.uex' path='docs/doc[@for="ListControlActionList"]/*' />
    internal class ListControlActionList : DesignerActionList { 
        private IDataSourceDesigner _dataSourceDesigner;
        private ListControlDesigner _listControlDesigner;

        /// <include file='doc\ListControlActionList.uex' path='docs/doc[@for="ListControlActionList.ListControlActionList"]/*' /> 
        public ListControlActionList(ListControlDesigner listControlDesigner, IDataSourceDesigner dataSourceDesigner) : base(listControlDesigner.Component) {
            _listControlDesigner = listControlDesigner; 
            _dataSourceDesigner = dataSourceDesigner; 
        }
 
        /// <include file='doc\ListControlActionList.uex' path='docs/doc[@for="ListControlActionList.AutoPostBack"]/*' />
        public bool AutoPostBack {
            get {
                return ((ListControl)_listControlDesigner.Component).AutoPostBack; 
            }
            set { 
                PropertyDescriptor autoPostBackDescriptor = TypeDescriptor.GetProperties(_listControlDesigner.Component)["AutoPostBack"]; 
                autoPostBackDescriptor.SetValue(_listControlDesigner.Component, value);
            } 
        }

        public override bool AutoShow {
            get { 
                return true;
            } 
            set { 
            }
        } 

        /// <include file='doc\ListControlActionList.uex' path='docs/doc[@for="ListControlActionList.EditItems"]/*' />
        public void EditItems() {
            _listControlDesigner.EditItems(); 
        }
 
        /// <include file='doc\ListControlActionList.uex' path='docs/doc[@for="ListControlActionList.ConnectToDataSource"]/*' /> 
        public void ConnectToDataSource() {
            _listControlDesigner.ConnectToDataSourceAction(); 
        }

        /// <include file='doc\ListControlActionList.uex' path='docs/doc[@for="ListControlActionList.GetSortedActionItems"]/*' />
        public override DesignerActionItemCollection GetSortedActionItems() { 
            DesignerActionItemCollection items = new DesignerActionItemCollection();
            PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(_listControlDesigner.Component); 
 
            PropertyDescriptor pd = pdc["DataSourceID"];
            if (pd != null && pd.IsBrowsable) { 
                items.Add(new DesignerActionMethodItem(this,
                                                        "ConnectToDataSource",
                                                        SR.GetString(SR.ListControl_ConfigureDataVerb),
                                                        SR.GetString(SR.BaseDataBoundControl_DataActionGroup), 
                                                        SR.GetString(SR.BaseDataBoundControl_ConfigureDataVerbDesc)));
            } 
 
            // add associated tasks
            ControlDesigner dsDesigner = _dataSourceDesigner as ControlDesigner; 
            if (dsDesigner != null) {
                ((DesignerActionMethodItem)items[0]).RelatedComponent = dsDesigner.Component;
            }
 

            pd = pdc["Items"]; 
            if (pd != null && pd.IsBrowsable) { 
                items.Add(new DesignerActionMethodItem(this,
                                                        "EditItems", 
                                                        SR.GetString(SR.ListControl_EditItems),
                                                        "Actions",
                                                        SR.GetString(SR.ListControl_EditItemsDesc)));
            } 

            pd = pdc["AutoPostBack"]; 
            if (pd != null && pd.IsBrowsable) { 
                items.Add(new DesignerActionPropertyItem("AutoPostBack",
                                                        SR.GetString(SR.ListControl_EnableAutoPostBack), 
                                                        "Behavior",
                                                        SR.GetString(SR.ListControl_EnableAutoPostBackDesc)));
            }
            return items; 
        }
 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ListControlActionList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics;
    using System.Web.UI.Design; 
    using System.Web.UI.Design.Util;
    using System.Web.UI.WebControls; 
 
    /// <include file='doc\ListControlActionList.uex' path='docs/doc[@for="ListControlActionList"]/*' />
    internal class ListControlActionList : DesignerActionList { 
        private IDataSourceDesigner _dataSourceDesigner;
        private ListControlDesigner _listControlDesigner;

        /// <include file='doc\ListControlActionList.uex' path='docs/doc[@for="ListControlActionList.ListControlActionList"]/*' /> 
        public ListControlActionList(ListControlDesigner listControlDesigner, IDataSourceDesigner dataSourceDesigner) : base(listControlDesigner.Component) {
            _listControlDesigner = listControlDesigner; 
            _dataSourceDesigner = dataSourceDesigner; 
        }
 
        /// <include file='doc\ListControlActionList.uex' path='docs/doc[@for="ListControlActionList.AutoPostBack"]/*' />
        public bool AutoPostBack {
            get {
                return ((ListControl)_listControlDesigner.Component).AutoPostBack; 
            }
            set { 
                PropertyDescriptor autoPostBackDescriptor = TypeDescriptor.GetProperties(_listControlDesigner.Component)["AutoPostBack"]; 
                autoPostBackDescriptor.SetValue(_listControlDesigner.Component, value);
            } 
        }

        public override bool AutoShow {
            get { 
                return true;
            } 
            set { 
            }
        } 

        /// <include file='doc\ListControlActionList.uex' path='docs/doc[@for="ListControlActionList.EditItems"]/*' />
        public void EditItems() {
            _listControlDesigner.EditItems(); 
        }
 
        /// <include file='doc\ListControlActionList.uex' path='docs/doc[@for="ListControlActionList.ConnectToDataSource"]/*' /> 
        public void ConnectToDataSource() {
            _listControlDesigner.ConnectToDataSourceAction(); 
        }

        /// <include file='doc\ListControlActionList.uex' path='docs/doc[@for="ListControlActionList.GetSortedActionItems"]/*' />
        public override DesignerActionItemCollection GetSortedActionItems() { 
            DesignerActionItemCollection items = new DesignerActionItemCollection();
            PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(_listControlDesigner.Component); 
 
            PropertyDescriptor pd = pdc["DataSourceID"];
            if (pd != null && pd.IsBrowsable) { 
                items.Add(new DesignerActionMethodItem(this,
                                                        "ConnectToDataSource",
                                                        SR.GetString(SR.ListControl_ConfigureDataVerb),
                                                        SR.GetString(SR.BaseDataBoundControl_DataActionGroup), 
                                                        SR.GetString(SR.BaseDataBoundControl_ConfigureDataVerbDesc)));
            } 
 
            // add associated tasks
            ControlDesigner dsDesigner = _dataSourceDesigner as ControlDesigner; 
            if (dsDesigner != null) {
                ((DesignerActionMethodItem)items[0]).RelatedComponent = dsDesigner.Component;
            }
 

            pd = pdc["Items"]; 
            if (pd != null && pd.IsBrowsable) { 
                items.Add(new DesignerActionMethodItem(this,
                                                        "EditItems", 
                                                        SR.GetString(SR.ListControl_EditItems),
                                                        "Actions",
                                                        SR.GetString(SR.ListControl_EditItemsDesc)));
            } 

            pd = pdc["AutoPostBack"]; 
            if (pd != null && pd.IsBrowsable) { 
                items.Add(new DesignerActionPropertyItem("AutoPostBack",
                                                        SR.GetString(SR.ListControl_EnableAutoPostBack), 
                                                        "Behavior",
                                                        SR.GetString(SR.ListControl_EnableAutoPostBackDesc)));
            }
            return items; 
        }
 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
