//------------------------------------------------------------------------------ 
// <copyright file="WebFormsDesignerActionService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Drawing.Design; 
    using System.Globalization; 
    using System.Resources;
    using System.Web.Compilation; 
    using System.Web.UI;

    /// <devdoc>
    /// </devdoc> 
    public class WebFormsDesignerActionService : DesignerActionService {
        public WebFormsDesignerActionService(IServiceProvider serviceProvider) 
            : base(serviceProvider) { 
        }
 
        protected override void GetComponentDesignerActions(IComponent component, DesignerActionListCollection actionLists) {
            if (component == null) {
                throw new ArgumentNullException("component");
            } 

            if (actionLists == null) { 
                throw new ArgumentNullException("actionLists"); 
            }
 
            IServiceContainer sc = component.Site as IServiceContainer;
            if (sc != null) {
                DesignerCommandSet dcs = (DesignerCommandSet)sc.GetService(typeof(DesignerCommandSet));
                if (dcs != null) { 
                    DesignerActionListCollection pullCollection = dcs.ActionLists;
                    if (pullCollection != null) { 
                        actionLists.AddRange(pullCollection); 
                    }
                } 
                // if we don't find any, add the verbs for this component there...
                if ((actionLists.Count == 0) ||
                    ((actionLists.Count == 1) && (actionLists[0] is ControlDesigner.ControlDesignerActionList))) {
                    DesignerVerbCollection verbs = dcs.Verbs; 
                    if (verbs != null && verbs.Count != 0) {
                        DesignerVerb[] verbsArray = new DesignerVerb[verbs.Count]; 
                        verbs.CopyTo(verbsArray, 0); 
                        actionLists.Add(new DesignerActionVerbList(verbsArray));
                    } 
                }
            }
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="WebFormsDesignerActionService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Collections.Specialized; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing;
    using System.Drawing.Design; 
    using System.Globalization; 
    using System.Resources;
    using System.Web.Compilation; 
    using System.Web.UI;

    /// <devdoc>
    /// </devdoc> 
    public class WebFormsDesignerActionService : DesignerActionService {
        public WebFormsDesignerActionService(IServiceProvider serviceProvider) 
            : base(serviceProvider) { 
        }
 
        protected override void GetComponentDesignerActions(IComponent component, DesignerActionListCollection actionLists) {
            if (component == null) {
                throw new ArgumentNullException("component");
            } 

            if (actionLists == null) { 
                throw new ArgumentNullException("actionLists"); 
            }
 
            IServiceContainer sc = component.Site as IServiceContainer;
            if (sc != null) {
                DesignerCommandSet dcs = (DesignerCommandSet)sc.GetService(typeof(DesignerCommandSet));
                if (dcs != null) { 
                    DesignerActionListCollection pullCollection = dcs.ActionLists;
                    if (pullCollection != null) { 
                        actionLists.AddRange(pullCollection); 
                    }
                } 
                // if we don't find any, add the verbs for this component there...
                if ((actionLists.Count == 0) ||
                    ((actionLists.Count == 1) && (actionLists[0] is ControlDesigner.ControlDesignerActionList))) {
                    DesignerVerbCollection verbs = dcs.Verbs; 
                    if (verbs != null && verbs.Count != 0) {
                        DesignerVerb[] verbsArray = new DesignerVerb[verbs.Count]; 
                        verbs.CopyTo(verbsArray, 0); 
                        actionLists.Add(new DesignerActionVerbList(verbsArray));
                    } 
                }
            }
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
