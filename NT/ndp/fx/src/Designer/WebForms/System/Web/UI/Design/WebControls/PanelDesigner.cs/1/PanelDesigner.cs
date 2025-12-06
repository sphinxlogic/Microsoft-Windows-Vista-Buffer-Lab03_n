//------------------------------------------------------------------------------ 
// <copyright file="PanelDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Diagnostics;
 
    using System;
    using System.ComponentModel;
    using Microsoft.Win32;
    using System.Web.UI.WebControls; 
    using System.Globalization;
 
    /// <include file='doc\PanelDesigner.uex' path='docs/doc[@for="PanelDesigner"]/*' /> 
    /// <devdoc>
    ///    <para> 
    ///       Provides design-time support for the <see cref='System.Web.UI.WebControls.Panel'/>
    ///       web control.
    ///    </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    [Obsolete("The recommended alternative is PanelContainerDesigner because it uses an EditableDesignerRegion for editing the content. Designer regions allow for better control of the content being edited. http://go.microsoft.com/fwlink/?linkid=14202")] 
    public class PanelDesigner : ReadWriteControlDesigner { 

        /// <include file='doc\PanelDesigner.uex' path='docs/doc[@for="PanelDesigner.MapPropertyToStyle"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Maps a specified property and value to a specified HTML style.
        ///    </para> 
        /// </devdoc>
        protected override void MapPropertyToStyle(string propName, Object varPropValue) { 
            Debug.Assert(propName != null && propName.Length != 0, "Invalid property name passed in!"); 
            Debug.Assert(varPropValue != null, "Invalid property value passed in!");
            if (propName == null || varPropValue == null) { 
                return;
            }

            if (varPropValue != null) { 
                try {
                    // 
 
                    if (propName.Equals("BackImageUrl")) {
                        string strPropValue = Convert.ToString(varPropValue, CultureInfo.InvariantCulture); 
                        if (strPropValue != null) {
                            if (strPropValue.Length != 0) {
                                strPropValue = "url(" + strPropValue + ")";
                                BehaviorInternal.SetStyleAttribute("backgroundImage", true, strPropValue, true); 
                            }
                        } 
                    } 
                    else if (propName.Equals("HorizontalAlign")) {
                        string strHAlign = String.Empty; 

                        if ((HorizontalAlign)varPropValue != HorizontalAlign.NotSet) {
                            strHAlign = Enum.Format(typeof(HorizontalAlign), varPropValue, "G");
                        } 
                        BehaviorInternal.SetStyleAttribute("textAlign", true, strHAlign, true);
                    } 
                    else { 
                        base.MapPropertyToStyle(propName, varPropValue);
                    } 
                }
                catch (Exception ex) {
                    Debug.Fail(ex.ToString());
                } 
            }
        } 
 
        /// <include file='doc\PanelDesigner.uex' path='docs/doc[@for="PanelDesigner.OnBehaviorAttached"]/*' />
        /// <devdoc> 
        ///     Notification that is fired upon the designer being attached to the behavior.
        /// </devdoc>
        [Obsolete("The recommended alternative is ControlDesigner.Tag. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected override void OnBehaviorAttached() { 
            base.OnBehaviorAttached();
 
            Panel panel = (Panel)Component; 
            string backImageUrl = panel.BackImageUrl;
            if (backImageUrl != null) { 
                MapPropertyToStyle("BackImageUrl", backImageUrl);
            }

            HorizontalAlign hAlign = panel.HorizontalAlign; 
            if (HorizontalAlign.NotSet != hAlign) {
                MapPropertyToStyle("HorizontalAlign", hAlign); 
            } 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="PanelDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System.Diagnostics;
 
    using System;
    using System.ComponentModel;
    using Microsoft.Win32;
    using System.Web.UI.WebControls; 
    using System.Globalization;
 
    /// <include file='doc\PanelDesigner.uex' path='docs/doc[@for="PanelDesigner"]/*' /> 
    /// <devdoc>
    ///    <para> 
    ///       Provides design-time support for the <see cref='System.Web.UI.WebControls.Panel'/>
    ///       web control.
    ///    </para>
    /// </devdoc> 
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    [Obsolete("The recommended alternative is PanelContainerDesigner because it uses an EditableDesignerRegion for editing the content. Designer regions allow for better control of the content being edited. http://go.microsoft.com/fwlink/?linkid=14202")] 
    public class PanelDesigner : ReadWriteControlDesigner { 

        /// <include file='doc\PanelDesigner.uex' path='docs/doc[@for="PanelDesigner.MapPropertyToStyle"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Maps a specified property and value to a specified HTML style.
        ///    </para> 
        /// </devdoc>
        protected override void MapPropertyToStyle(string propName, Object varPropValue) { 
            Debug.Assert(propName != null && propName.Length != 0, "Invalid property name passed in!"); 
            Debug.Assert(varPropValue != null, "Invalid property value passed in!");
            if (propName == null || varPropValue == null) { 
                return;
            }

            if (varPropValue != null) { 
                try {
                    // 
 
                    if (propName.Equals("BackImageUrl")) {
                        string strPropValue = Convert.ToString(varPropValue, CultureInfo.InvariantCulture); 
                        if (strPropValue != null) {
                            if (strPropValue.Length != 0) {
                                strPropValue = "url(" + strPropValue + ")";
                                BehaviorInternal.SetStyleAttribute("backgroundImage", true, strPropValue, true); 
                            }
                        } 
                    } 
                    else if (propName.Equals("HorizontalAlign")) {
                        string strHAlign = String.Empty; 

                        if ((HorizontalAlign)varPropValue != HorizontalAlign.NotSet) {
                            strHAlign = Enum.Format(typeof(HorizontalAlign), varPropValue, "G");
                        } 
                        BehaviorInternal.SetStyleAttribute("textAlign", true, strHAlign, true);
                    } 
                    else { 
                        base.MapPropertyToStyle(propName, varPropValue);
                    } 
                }
                catch (Exception ex) {
                    Debug.Fail(ex.ToString());
                } 
            }
        } 
 
        /// <include file='doc\PanelDesigner.uex' path='docs/doc[@for="PanelDesigner.OnBehaviorAttached"]/*' />
        /// <devdoc> 
        ///     Notification that is fired upon the designer being attached to the behavior.
        /// </devdoc>
        [Obsolete("The recommended alternative is ControlDesigner.Tag. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected override void OnBehaviorAttached() { 
            base.OnBehaviorAttached();
 
            Panel panel = (Panel)Component; 
            string backImageUrl = panel.BackImageUrl;
            if (backImageUrl != null) { 
                MapPropertyToStyle("BackImageUrl", backImageUrl);
            }

            HorizontalAlign hAlign = panel.HorizontalAlign; 
            if (HorizontalAlign.NotSet != hAlign) {
                MapPropertyToStyle("HorizontalAlign", hAlign); 
            } 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
