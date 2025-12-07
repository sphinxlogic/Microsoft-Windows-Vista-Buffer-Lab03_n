//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2001' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Windows.Forms.Design { 
    using System;
    using System.ComponentModel; 
    using System.Design;

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Class | AttributeTargets.Method)]
    internal sealed class SRDisplayNameAttribute : DisplayNameAttribute { 

        private bool replaced = false; 
 
        /// <summary>
        ///     Constructs a new sys display name. 
        /// </summary>
        /// <param name='displayName'>
        ///     description text.
        /// </param> 
        public SRDisplayNameAttribute(string displayName) : base(displayName) {
        } 
 
        /// <summary>
        ///     Retrieves the description text. 
        /// </summary>
        /// <returns>
        ///     description
        /// </returns> 
        public override string DisplayName {
            get { 
                if (!replaced) { 
                    replaced = true;
                    DisplayNameValue = SR.GetString(base.DisplayName); 
                }
                return base.DisplayName;
            }
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2001' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Windows.Forms.Design { 
    using System;
    using System.ComponentModel; 
    using System.Design;

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Class | AttributeTargets.Method)]
    internal sealed class SRDisplayNameAttribute : DisplayNameAttribute { 

        private bool replaced = false; 
 
        /// <summary>
        ///     Constructs a new sys display name. 
        /// </summary>
        /// <param name='displayName'>
        ///     description text.
        /// </param> 
        public SRDisplayNameAttribute(string displayName) : base(displayName) {
        } 
 
        /// <summary>
        ///     Retrieves the description text. 
        /// </summary>
        /// <returns>
        ///     description
        /// </returns> 
        public override string DisplayName {
            get { 
                if (!replaced) { 
                    replaced = true;
                    DisplayNameValue = SR.GetString(base.DisplayName); 
                }
                return base.DisplayName;
            }
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
