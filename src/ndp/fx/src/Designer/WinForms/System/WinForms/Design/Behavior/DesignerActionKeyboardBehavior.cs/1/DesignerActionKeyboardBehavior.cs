namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Windows.Forms.Design;
    using System.Diagnostics.CodeAnalysis; 

    /// <devdoc>
    ///
    /// </devdoc> 
    internal sealed class DesignerActionKeyboardBehavior : Behavior {
        private DesignerActionPanel panel; 
        private IMenuCommandService menuService; 
        private DesignerActionUIService daUISvc;
        private static readonly Guid VSStandardCommandSet97 = new Guid("{5efc7975-14bc-11cf-9b2b-00aa00573819}"); 


        public DesignerActionKeyboardBehavior(DesignerActionPanel panel, IServiceProvider serviceProvider, BehaviorService behaviorService) :
            base(true, behaviorService) { 
            this.panel = panel;
            if(serviceProvider != null) { 
                this.menuService = serviceProvider.GetService(typeof(IMenuCommandService)) as IMenuCommandService; 
                Debug.Assert(menuService != null, "we should have found a menu service here...");
                this.daUISvc = serviceProvider.GetService(typeof(DesignerActionUIService)) as DesignerActionUIService; 
            }
        }
        // THIS shoudl not stay here, creation of a custom command or of the real thing should be handled in the
        // designeractionpanel itself 
        public override MenuCommand FindCommand(CommandID commandId) {
            if(panel != null && menuService != null) { 
                // if the command we're looking for is handled by the panel, just tell VS that this command is 
                // disabled. otherwise let it through as usual...
                foreach(CommandID candidateCommandId in panel.FilteredCommandIDs) { 
                    if(candidateCommandId.Equals(commandId)) {
                        MenuCommand dummyMC = new MenuCommand(delegate{}, commandId);
                        dummyMC.Enabled = false;
                        //Debug.WriteLine("Found command id in DesignerActionPAnel supported commands"); 
                        return dummyMC;
                    } 
                } 
                // in case of a ctrl-tab we need to close the DAP
                if (daUISvc != null && commandId.Guid == DesignerActionKeyboardBehavior.VSStandardCommandSet97 && commandId.ID == 1124) { 
                    daUISvc.HideUI(null);
                }
            }
            //Debug.WriteLine("NOT Found command id in DesignerActionPAnel supported commands. ASking base..."); 
            return base.FindCommand(commandId); // this will route the request to the parent behavior
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Windows.Forms.Design;
    using System.Diagnostics.CodeAnalysis; 

    /// <devdoc>
    ///
    /// </devdoc> 
    internal sealed class DesignerActionKeyboardBehavior : Behavior {
        private DesignerActionPanel panel; 
        private IMenuCommandService menuService; 
        private DesignerActionUIService daUISvc;
        private static readonly Guid VSStandardCommandSet97 = new Guid("{5efc7975-14bc-11cf-9b2b-00aa00573819}"); 


        public DesignerActionKeyboardBehavior(DesignerActionPanel panel, IServiceProvider serviceProvider, BehaviorService behaviorService) :
            base(true, behaviorService) { 
            this.panel = panel;
            if(serviceProvider != null) { 
                this.menuService = serviceProvider.GetService(typeof(IMenuCommandService)) as IMenuCommandService; 
                Debug.Assert(menuService != null, "we should have found a menu service here...");
                this.daUISvc = serviceProvider.GetService(typeof(DesignerActionUIService)) as DesignerActionUIService; 
            }
        }
        // THIS shoudl not stay here, creation of a custom command or of the real thing should be handled in the
        // designeractionpanel itself 
        public override MenuCommand FindCommand(CommandID commandId) {
            if(panel != null && menuService != null) { 
                // if the command we're looking for is handled by the panel, just tell VS that this command is 
                // disabled. otherwise let it through as usual...
                foreach(CommandID candidateCommandId in panel.FilteredCommandIDs) { 
                    if(candidateCommandId.Equals(commandId)) {
                        MenuCommand dummyMC = new MenuCommand(delegate{}, commandId);
                        dummyMC.Enabled = false;
                        //Debug.WriteLine("Found command id in DesignerActionPAnel supported commands"); 
                        return dummyMC;
                    } 
                } 
                // in case of a ctrl-tab we need to close the DAP
                if (daUISvc != null && commandId.Guid == DesignerActionKeyboardBehavior.VSStandardCommandSet97 && commandId.ID == 1124) { 
                    daUISvc.HideUI(null);
                }
            }
            //Debug.WriteLine("NOT Found command id in DesignerActionPAnel supported commands. ASking base..."); 
            return base.FindCommand(commandId); // this will route the request to the parent behavior
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
