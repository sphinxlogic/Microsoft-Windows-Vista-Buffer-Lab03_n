//------------------------------------------------------------------------------ 
// <copyright file="TabControlToolboxItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Drawing.Design;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 
    using System.Runtime.Serialization;
 
 
    [Serializable]
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")] 
    internal class TabControlToolboxItem : ToolboxItem{

        public TabControlToolboxItem() : base (typeof(TabControl)) {
        } 

 
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")] 
        private TabControlToolboxItem(SerializationInfo info, StreamingContext context) {
            Deserialize(info, context); 
        }

        protected override IComponent[] CreateComponentsCore(IDesignerHost host) {
            IComponent[] components = base.CreateComponentsCore(host); 

            Debug.Assert(components != null && components.Length > 0, "TabControlToolboxItem failed to create component."); 
            Debug.Assert(components.Length == 1, "TabControlToolboxItem did not create the correct number of components."); 
            Debug.Assert(components[0] is TabControl, "TabControlToolboxItem did not create a control.");
 
            if (components != null && components.Length > 0 && components[0] is TabControl) {
                TabControl tabControl = (TabControl) components[0];
                tabControl.ShowToolTips = true;
            } 
            return components;
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TabControlToolboxItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Drawing.Design;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis; 
    using System.Runtime.Serialization;
 
 
    [Serializable]
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")] 
    internal class TabControlToolboxItem : ToolboxItem{

        public TabControlToolboxItem() : base (typeof(TabControl)) {
        } 

 
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")] 
        private TabControlToolboxItem(SerializationInfo info, StreamingContext context) {
            Deserialize(info, context); 
        }

        protected override IComponent[] CreateComponentsCore(IDesignerHost host) {
            IComponent[] components = base.CreateComponentsCore(host); 

            Debug.Assert(components != null && components.Length > 0, "TabControlToolboxItem failed to create component."); 
            Debug.Assert(components.Length == 1, "TabControlToolboxItem did not create the correct number of components."); 
            Debug.Assert(components[0] is TabControl, "TabControlToolboxItem did not create a control.");
 
            if (components != null && components.Length > 0 && components[0] is TabControl) {
                TabControl tabControl = (TabControl) components[0];
                tabControl.ShowToolTips = true;
            } 
            return components;
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
