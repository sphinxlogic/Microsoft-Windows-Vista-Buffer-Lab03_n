//------------------------------------------------------------------------------ 
// <copyright file="AutoSizeToolBoxItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System.Runtime.InteropServices;
 
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System;
    using System.IO; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design; 
    using System.Windows.Forms;
    using System.Drawing; 
    using System.Drawing.Design;
    using System.Design;
    using System.Windows.Forms.ComponentModel;
    using System.Runtime.Serialization; 

    // For Whidbey, we want to turn on AutoSize, AutoRelocate, and change the DefaultPadding 
    // from the values we shipped in RTM/Everett.  To avoid a breaking change, we use a 
    // custom toolbox item to apply these changes when the control is dropped.
    // 
    [Serializable]
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")] // this class is instantiated.
    internal class AutoSizeToolboxItem : ToolboxItem {
        public AutoSizeToolboxItem(){} 

        public AutoSizeToolboxItem(Type toolType) : base (toolType) { 
        } 

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")] // it's internal and nobody derives from this. 
        private AutoSizeToolboxItem(SerializationInfo info, StreamingContext context) {
            Deserialize(info, context);
        }
 
        protected override IComponent[] CreateComponentsCore(IDesignerHost host) {
            IComponent[] components = base.CreateComponentsCore(host); 
 
            Debug.Assert(components != null && components.Length > 0, "ControlToolboxItem failed to create component.");
            Debug.Assert(components.Length == 1, "ControlToolboxItem did not create the correct number of components."); 
            Debug.Assert(components.Length > 0 && components[0] is Control, "ControlToolboxItem did not create a control.");

            if (components != null && components.Length > 0 && components[0] is Control) {
                Control control = components[0] as Control; 
                control.AutoSize = true;
            } 
            return components; 
        }
 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="AutoSizeToolBoxItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System.Runtime.InteropServices;
 
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System;
    using System.IO; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design; 
    using System.Windows.Forms;
    using System.Drawing; 
    using System.Drawing.Design;
    using System.Design;
    using System.Windows.Forms.ComponentModel;
    using System.Runtime.Serialization; 

    // For Whidbey, we want to turn on AutoSize, AutoRelocate, and change the DefaultPadding 
    // from the values we shipped in RTM/Everett.  To avoid a breaking change, we use a 
    // custom toolbox item to apply these changes when the control is dropped.
    // 
    [Serializable]
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")] // this class is instantiated.
    internal class AutoSizeToolboxItem : ToolboxItem {
        public AutoSizeToolboxItem(){} 

        public AutoSizeToolboxItem(Type toolType) : base (toolType) { 
        } 

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")] // it's internal and nobody derives from this. 
        private AutoSizeToolboxItem(SerializationInfo info, StreamingContext context) {
            Deserialize(info, context);
        }
 
        protected override IComponent[] CreateComponentsCore(IDesignerHost host) {
            IComponent[] components = base.CreateComponentsCore(host); 
 
            Debug.Assert(components != null && components.Length > 0, "ControlToolboxItem failed to create component.");
            Debug.Assert(components.Length == 1, "ControlToolboxItem did not create the correct number of components."); 
            Debug.Assert(components.Length > 0 && components[0] is Control, "ControlToolboxItem did not create a control.");

            if (components != null && components.Length > 0 && components[0] is Control) {
                Control control = components[0] as Control; 
                control.AutoSize = true;
            } 
            return components; 
        }
 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
