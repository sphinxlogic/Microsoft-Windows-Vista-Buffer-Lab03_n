//------------------------------------------------------------------------------ 
// <copyright file="DataBoundControlActionList.cs" company="Microsoft">
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
 
    /// <include file='doc\DataBoundControlActionList.uex' path='docs/doc[@for="DataBoundControlActionList"]/*' />
    internal class DataBoundControlActionList : DesignerActionList { 
        private IDataSourceDesigner _dataSourceDesigner;
        private ControlDesigner _controlDesigner;

        /// <include file='doc\DataBoundControlActionList.uex' path='docs/doc[@for="DataBoundControlActionList.DataBoundControlActionList"]/*' /> 
        public DataBoundControlActionList(ControlDesigner controlDesigner, IDataSourceDesigner dataSourceDesigner) : base(controlDesigner.Component) {
            _controlDesigner = controlDesigner; 
            _dataSourceDesigner = dataSourceDesigner; 
        }
 
        public override bool AutoShow {
            get {
                return true;
            } 
            set {
            } 
        } 

        /// <include file='doc\DataBoundControlActionList.uex' path='docs/doc[@for="DataBoundControlActionList.DataSourceID"]/*' /> 
        [TypeConverterAttribute(typeof(DataSourceIDConverter))]
        public string DataSourceID {
            get {
                string dataSourceID = null; 
                DataBoundControlDesigner dataBoundControlDesigner = _controlDesigner as DataBoundControlDesigner;
                if (dataBoundControlDesigner != null) { 
                    dataSourceID = dataBoundControlDesigner.DataSourceID; 
                }
                else { 
                    BaseDataListDesigner bdlDesigner = _controlDesigner as BaseDataListDesigner;
                    if (bdlDesigner != null) {
                        dataSourceID = bdlDesigner.DataSourceID;
                    } 
                    else {
                        RepeaterDesigner repeaterDesigner = _controlDesigner as RepeaterDesigner; 
                        if (repeaterDesigner != null) { 
                            dataSourceID = repeaterDesigner.DataSourceID;
                        } 
                        else {
                            Debug.Fail("Unknown type called DataBoundControlActionList");
                        }
                    } 
                }
                if (String.IsNullOrEmpty(dataSourceID)) { 
                    return SR.GetString(SR.DataSourceIDChromeConverter_NoDataSource); 
                }
                return dataSourceID; 
            }
            set {
                ControlDesigner.InvokeTransactedChange(_controlDesigner.Component, new TransactedChangeCallback(SetDataSourceIDCallback), value, SR.GetString(SR.DataBoundControlActionList_SetDataSourceIDTransaction));
            } 
        }
 
        /// <include file='doc\DataBoundControlActionList.uex' path='docs/doc[@for="DataBoundControlActionList.GetSortedActionItems"]/*' /> 
        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = new DesignerActionItemCollection(); 
            PropertyDescriptor pd = TypeDescriptor.GetProperties(_controlDesigner.Component)["DataSourceID"];
            if (pd != null && pd.IsBrowsable) {
                items.Add(new DesignerActionPropertyItem("DataSourceID",
                                                          SR.GetString(SR.BaseDataBoundControl_ConfigureDataVerb), 
                                                          SR.GetString(SR.BaseDataBoundControl_DataActionGroup),
                                                          SR.GetString(SR.BaseDataBoundControl_ConfigureDataVerbDesc))); 
            } 

            // add associated tasks 
            ControlDesigner dsDesigner = _dataSourceDesigner as ControlDesigner;
            if (dsDesigner != null) {
                ((DesignerActionPropertyItem)items[0]).RelatedComponent = dsDesigner.Component;
            } 

            return items; 
        } 

        private bool SetDataSourceIDCallback(object context) { 
            string value = (string)context;
            DataBoundControlDesigner dataBoundControlDesigner = _controlDesigner as DataBoundControlDesigner;
            if (dataBoundControlDesigner != null) {
                PropertyDescriptor dataSourceIDDescriptor = TypeDescriptor.GetProperties(dataBoundControlDesigner.Component)["DataSourceID"]; 
                dataSourceIDDescriptor.SetValue(dataBoundControlDesigner.Component, value);
            } 
            else { 
                BaseDataListDesigner bdlDesigner = _controlDesigner as BaseDataListDesigner;
                if (bdlDesigner != null) { 
                    PropertyDescriptor dataSourceIDDescriptor = TypeDescriptor.GetProperties(bdlDesigner.Component)["DataSourceID"];
                    dataSourceIDDescriptor.SetValue(bdlDesigner.Component, value);
                }
                else { 
                    RepeaterDesigner repeaterDesigner = _controlDesigner as RepeaterDesigner;
                    if (repeaterDesigner != null) { 
                        PropertyDescriptor dataSourceIDDescriptor = TypeDescriptor.GetProperties(repeaterDesigner.Component)["DataSourceID"]; 
                        dataSourceIDDescriptor.SetValue(repeaterDesigner.Component, value);
                    } 
                    else {
                        Debug.Fail("Unknown type called DataBoundControlActionList");
                    }
                } 
            }
            return true; 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataBoundControlActionList.cs" company="Microsoft">
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
 
    /// <include file='doc\DataBoundControlActionList.uex' path='docs/doc[@for="DataBoundControlActionList"]/*' />
    internal class DataBoundControlActionList : DesignerActionList { 
        private IDataSourceDesigner _dataSourceDesigner;
        private ControlDesigner _controlDesigner;

        /// <include file='doc\DataBoundControlActionList.uex' path='docs/doc[@for="DataBoundControlActionList.DataBoundControlActionList"]/*' /> 
        public DataBoundControlActionList(ControlDesigner controlDesigner, IDataSourceDesigner dataSourceDesigner) : base(controlDesigner.Component) {
            _controlDesigner = controlDesigner; 
            _dataSourceDesigner = dataSourceDesigner; 
        }
 
        public override bool AutoShow {
            get {
                return true;
            } 
            set {
            } 
        } 

        /// <include file='doc\DataBoundControlActionList.uex' path='docs/doc[@for="DataBoundControlActionList.DataSourceID"]/*' /> 
        [TypeConverterAttribute(typeof(DataSourceIDConverter))]
        public string DataSourceID {
            get {
                string dataSourceID = null; 
                DataBoundControlDesigner dataBoundControlDesigner = _controlDesigner as DataBoundControlDesigner;
                if (dataBoundControlDesigner != null) { 
                    dataSourceID = dataBoundControlDesigner.DataSourceID; 
                }
                else { 
                    BaseDataListDesigner bdlDesigner = _controlDesigner as BaseDataListDesigner;
                    if (bdlDesigner != null) {
                        dataSourceID = bdlDesigner.DataSourceID;
                    } 
                    else {
                        RepeaterDesigner repeaterDesigner = _controlDesigner as RepeaterDesigner; 
                        if (repeaterDesigner != null) { 
                            dataSourceID = repeaterDesigner.DataSourceID;
                        } 
                        else {
                            Debug.Fail("Unknown type called DataBoundControlActionList");
                        }
                    } 
                }
                if (String.IsNullOrEmpty(dataSourceID)) { 
                    return SR.GetString(SR.DataSourceIDChromeConverter_NoDataSource); 
                }
                return dataSourceID; 
            }
            set {
                ControlDesigner.InvokeTransactedChange(_controlDesigner.Component, new TransactedChangeCallback(SetDataSourceIDCallback), value, SR.GetString(SR.DataBoundControlActionList_SetDataSourceIDTransaction));
            } 
        }
 
        /// <include file='doc\DataBoundControlActionList.uex' path='docs/doc[@for="DataBoundControlActionList.GetSortedActionItems"]/*' /> 
        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = new DesignerActionItemCollection(); 
            PropertyDescriptor pd = TypeDescriptor.GetProperties(_controlDesigner.Component)["DataSourceID"];
            if (pd != null && pd.IsBrowsable) {
                items.Add(new DesignerActionPropertyItem("DataSourceID",
                                                          SR.GetString(SR.BaseDataBoundControl_ConfigureDataVerb), 
                                                          SR.GetString(SR.BaseDataBoundControl_DataActionGroup),
                                                          SR.GetString(SR.BaseDataBoundControl_ConfigureDataVerbDesc))); 
            } 

            // add associated tasks 
            ControlDesigner dsDesigner = _dataSourceDesigner as ControlDesigner;
            if (dsDesigner != null) {
                ((DesignerActionPropertyItem)items[0]).RelatedComponent = dsDesigner.Component;
            } 

            return items; 
        } 

        private bool SetDataSourceIDCallback(object context) { 
            string value = (string)context;
            DataBoundControlDesigner dataBoundControlDesigner = _controlDesigner as DataBoundControlDesigner;
            if (dataBoundControlDesigner != null) {
                PropertyDescriptor dataSourceIDDescriptor = TypeDescriptor.GetProperties(dataBoundControlDesigner.Component)["DataSourceID"]; 
                dataSourceIDDescriptor.SetValue(dataBoundControlDesigner.Component, value);
            } 
            else { 
                BaseDataListDesigner bdlDesigner = _controlDesigner as BaseDataListDesigner;
                if (bdlDesigner != null) { 
                    PropertyDescriptor dataSourceIDDescriptor = TypeDescriptor.GetProperties(bdlDesigner.Component)["DataSourceID"];
                    dataSourceIDDescriptor.SetValue(bdlDesigner.Component, value);
                }
                else { 
                    RepeaterDesigner repeaterDesigner = _controlDesigner as RepeaterDesigner;
                    if (repeaterDesigner != null) { 
                        PropertyDescriptor dataSourceIDDescriptor = TypeDescriptor.GetProperties(repeaterDesigner.Component)["DataSourceID"]; 
                        dataSourceIDDescriptor.SetValue(repeaterDesigner.Component, value);
                    } 
                    else {
                        Debug.Fail("Unknown type called DataBoundControlActionList");
                    }
                } 
            }
            return true; 
        } 
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
