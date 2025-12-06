//------------------------------------------------------------------------------ 
// <copyright file="BulletedListDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

 
namespace System.Web.UI.Design.WebControls { 

    using System; 
    using System.Design;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Web.UI;
    using System.Web.UI.WebControls; 
    using System.Web.UI.Design; 

    /// <include file='doc\BulletedListDesigner.uex' path='docs/doc[@for="BulletedListDesigner"]/*' /> 
    /// <devdoc>
    /// <para>The designer for the BulletedList web control.</para>
    /// </devdoc>
    public class BulletedListDesigner : System.Web.UI.Design.WebControls.ListControlDesigner { 

        protected override bool UsePreviewControl { 
            get { 
                return true;
            } 
        }

        /// <include file='doc\BulletedListDesigner.uex' path='docs/doc[@for="BulletedListDesigner.PostFilterEvents"]/*' />
        protected override void PostFilterEvents(IDictionary events) { 
            base.PostFilterEvents(events);
            events.Remove("SelectedIndexChanged"); 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="BulletedListDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

 
namespace System.Web.UI.Design.WebControls { 

    using System; 
    using System.Design;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Web.UI;
    using System.Web.UI.WebControls; 
    using System.Web.UI.Design; 

    /// <include file='doc\BulletedListDesigner.uex' path='docs/doc[@for="BulletedListDesigner"]/*' /> 
    /// <devdoc>
    /// <para>The designer for the BulletedList web control.</para>
    /// </devdoc>
    public class BulletedListDesigner : System.Web.UI.Design.WebControls.ListControlDesigner { 

        protected override bool UsePreviewControl { 
            get { 
                return true;
            } 
        }

        /// <include file='doc\BulletedListDesigner.uex' path='docs/doc[@for="BulletedListDesigner.PostFilterEvents"]/*' />
        protected override void PostFilterEvents(IDictionary events) { 
            base.PostFilterEvents(events);
            events.Remove("SelectedIndexChanged"); 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
