//------------------------------------------------------------------------------ 
// <copyright file="BaseDataListActionList.cs" company="Microsoft">
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
 
    /// <include file='doc\BaseDataListActionList.uex' path='docs/doc[@for="BaseDataListActionList"]/*' />
    internal class BaseDataListActionList : DataBoundControlActionList { 
        private IDataSourceDesigner _dataSourceDesigner;
        private ControlDesigner _controlDesigner;

        /// <include file='doc\BaseDataListActionList.uex' path='docs/doc[@for="BaseDataListActionList.BaseDataListActionList"]/*' /> 
        public BaseDataListActionList(ControlDesigner controlDesigner, IDataSourceDesigner dataSourceDesigner) : base(controlDesigner, dataSourceDesigner) {
            _controlDesigner = controlDesigner; 
            _dataSourceDesigner = dataSourceDesigner; 
        }
 
        /// <include file='doc\BaseDataListActionList.uex' path='docs/doc[@for="BaseDataListActionList.InvokePropertyBuilder"]/*' />
        public void InvokePropertyBuilder() {
            Debug.Assert(_controlDesigner is BaseDataListDesigner, "Called by wrong designer type");
            ((BaseDataListDesigner)_controlDesigner).InvokePropertyBuilder(0); 
        }
 
        /// <include file='doc\BaseDataListActionList.uex' path='docs/doc[@for="BaseDataListActionList.GetSortedActionItems"]/*' /> 
        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = base.GetSortedActionItems(); 
            if(items== null) {
                items = new DesignerActionItemCollection();
            }
            items.Add(new DesignerActionMethodItem(this, 
                                                    "InvokePropertyBuilder",
                                                    SR.GetString(SR.BDL_PropertyBuilderVerb), 
                                                    SR.GetString(SR.BDL_BehaviorGroup), 
                                                    SR.GetString(SR.BDL_PropertyBuilderDesc)));
            return items; 
        }

    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="BaseDataListActionList.cs" company="Microsoft">
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
 
    /// <include file='doc\BaseDataListActionList.uex' path='docs/doc[@for="BaseDataListActionList"]/*' />
    internal class BaseDataListActionList : DataBoundControlActionList { 
        private IDataSourceDesigner _dataSourceDesigner;
        private ControlDesigner _controlDesigner;

        /// <include file='doc\BaseDataListActionList.uex' path='docs/doc[@for="BaseDataListActionList.BaseDataListActionList"]/*' /> 
        public BaseDataListActionList(ControlDesigner controlDesigner, IDataSourceDesigner dataSourceDesigner) : base(controlDesigner, dataSourceDesigner) {
            _controlDesigner = controlDesigner; 
            _dataSourceDesigner = dataSourceDesigner; 
        }
 
        /// <include file='doc\BaseDataListActionList.uex' path='docs/doc[@for="BaseDataListActionList.InvokePropertyBuilder"]/*' />
        public void InvokePropertyBuilder() {
            Debug.Assert(_controlDesigner is BaseDataListDesigner, "Called by wrong designer type");
            ((BaseDataListDesigner)_controlDesigner).InvokePropertyBuilder(0); 
        }
 
        /// <include file='doc\BaseDataListActionList.uex' path='docs/doc[@for="BaseDataListActionList.GetSortedActionItems"]/*' /> 
        public override DesignerActionItemCollection GetSortedActionItems() {
            DesignerActionItemCollection items = base.GetSortedActionItems(); 
            if(items== null) {
                items = new DesignerActionItemCollection();
            }
            items.Add(new DesignerActionMethodItem(this, 
                                                    "InvokePropertyBuilder",
                                                    SR.GetString(SR.BDL_PropertyBuilderVerb), 
                                                    SR.GetString(SR.BDL_BehaviorGroup), 
                                                    SR.GetString(SR.BDL_PropertyBuilderDesc)));
            return items; 
        }

    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
