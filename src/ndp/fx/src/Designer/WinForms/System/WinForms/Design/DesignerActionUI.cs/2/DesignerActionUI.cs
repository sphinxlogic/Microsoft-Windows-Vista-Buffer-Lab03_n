namespace System.Windows.Forms.Design { 

    using System;
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing;
    using System.Runtime.InteropServices; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design.Behavior;
    using System.Text;
 
    /// <include file='doc\DesignerActionUI.uex' path='docs/doc[@for="DesignerActionUI"]/*' />
    /// <devdoc> 
    ///     The DesignerActionUI is the designer/UI-specific implementation of the 
    ////    DesignerActions feature.  This class instantiates the DesignerActionService
    ///     and hooks to its DesignerActionsChanged event.  Responding to this single 
    ///     event will enable the DesignerActionUI to perform all neceessary UI-related
    ///     operations.
    ///     Note that the DesignerActionUI uses the BehaviorService to manage all UI
    ///     interaction.  For every component containing a DesignerAction (determined 
    ///     by the DesignerActionsChagned event) there will be an associated
    ///     DesignerActionGlyph and DesignerActionBehavior. 
    ///     Finally, the DesignerActionUI is also responsible for showing and managing 
    ///     the Action's context menus.  Note that every DesignerAction context menu has
    ///     an item that will bring up the DesignerActions option pane in the options 
    ///     dialog.
    /// </devdoc>
    internal class DesignerActionUI : IDisposable {
 
        private static TraceSwitch DesigneActionPanelTraceSwitch     = new TraceSwitch("DesigneActionPanelTrace", "DesignerActionPanel tracing");
 
        private Adorner                 designerActionAdorner;//used to add designeraction-related glyphs 
        private IServiceProvider        serviceProvider;//standard service provider
        private ISelectionService       selSvc;//used to determine if comps have selection or not 
        private DesignerActionService   designerActionService;//this is how all designeractions will be managed
        private DesignerActionUIService   designerActionUIService;//this is how all designeractions UI elements will be managed
        private BehaviorService         behaviorService;//this is how all of our UI is implemented (glyphs, behaviors, etc...)
        private IMenuCommandService      menuCommandService; 
        private DesignerActionKeyboardBehavior dapkb;   //out keyboard behavior
        private Hashtable               componentToGlyph;//used for quick reference between compoments and our glyphs 
        private Control                 marshalingControl;//used to invoke events on our main gui thread 
        private IComponent              lastPanelComponent;
 
        private IUIService              uiService;
        private IWin32Window            mainParentWindow;
        internal DesignerActionToolStripDropDown      designerActionHost;
 
        private MenuCommand             cmdShowDesignerActions;//used to respond to the Alt+Shft+F10 command
        private bool                    inTransaction = false; 
        private IComponent              relatedComponentTransaction; 
        private DesignerActionGlyph     relatedGlyphTransaction;
        private bool                    disposeActionService; 
        private bool                    disposeActionUIService;


        private delegate void ActionChangedEventHandler(object sender, DesignerActionListsChangedEventArgs e); 

#if DEBUG 
        internal static readonly TraceSwitch DropDownVisibilityDebug = new TraceSwitch("DropDownVisibilityDebug", "Debug ToolStrip Selection code"); 
#else
        internal static readonly TraceSwitch DropDownVisibilityDebug; 
#endif

        /// <include file='doc\DesignerActionUI.uex' path='docs/doc[@for="DesignerActionUI.DesignerActionUI"]/*' />
        /// <devdoc> 
        ///     Constructor that takes a service provider.  This is needed to establish
        ///     references to the BehaviorService and SelecteionService, as well as 
        ///     spin-up the DesignerActionService. 
        /// </devdoc>
        public DesignerActionUI(IServiceProvider serviceProvider, Adorner containerAdorner) { 

            this.serviceProvider = serviceProvider;
            this.designerActionAdorner = containerAdorner;
 
            behaviorService = (BehaviorService)serviceProvider.GetService(typeof(BehaviorService));
            menuCommandService = (IMenuCommandService)serviceProvider.GetService(typeof(IMenuCommandService)); 
            selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService)); 

            if (behaviorService == null || selSvc == null) { 
                Debug.Fail("Either BehaviorService or ISelectionService is null, cannot continue.");
                return;
            }
 
            //query for our DesignerActionService
            designerActionService = (DesignerActionService)serviceProvider.GetService(typeof(DesignerActionService)); 
            if (designerActionService == null) { 
                //start the service
                designerActionService = new DesignerActionService(serviceProvider); 
                disposeActionService = true;
            }
            designerActionUIService = (DesignerActionUIService)serviceProvider.GetService(typeof(DesignerActionUIService));
            if (designerActionUIService == null) { 
                designerActionUIService = new DesignerActionUIService(serviceProvider);
                disposeActionUIService = true; 
            } 
            designerActionUIService.DesignerActionUIStateChange += new DesignerActionUIStateChangeEventHandler(this.OnDesignerActionUIStateChange);
            designerActionService.DesignerActionListsChanged += new DesignerActionListsChangedEventHandler(this.OnDesignerActionsChanged); 
            lastPanelComponent = null;

            IComponentChangeService cs = (IComponentChangeService)serviceProvider.GetService(typeof(IComponentChangeService));
            if (cs != null) { 
                cs.ComponentChanged += new ComponentChangedEventHandler(this.OnComponentChanged);
            } 
 

            if (menuCommandService != null) { 
                cmdShowDesignerActions = new MenuCommand(new EventHandler(OnKeyShowDesignerActions), MenuCommands.KeyInvokeSmartTag);
                menuCommandService.AddCommand(cmdShowDesignerActions);
            }
 
            uiService = (IUIService)serviceProvider.GetService(typeof(IUIService));
            if(uiService != null) 
                mainParentWindow = uiService.GetDialogOwnerWindow(); 

            componentToGlyph = new Hashtable(); 

            marshalingControl = new Control();
            marshalingControl.CreateControl();
        } 

 
        /// <include file='doc\DesignerActionUI.uex' path='docs/doc[@for="DesignerActionUI.Dispose"]/*' /> 
        /// <devdoc>
        ///     Disposes all UI-related objects and unhooks services. 
        /// </devdoc>

        // Don't need to dispose of designerActionUIService.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")] 
        public void Dispose() {
 
            if (marshalingControl != null) { 
                marshalingControl.Dispose();
                marshalingControl = null; 
            }


            if (serviceProvider != null) { 
                IComponentChangeService cs = (IComponentChangeService)serviceProvider.GetService(typeof(IComponentChangeService));
                if (cs != null) { 
                    cs.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged); 
                }
 
                if (cmdShowDesignerActions != null) {
                    IMenuCommandService mcs = (IMenuCommandService)serviceProvider.GetService(typeof(IMenuCommandService));
                    if (mcs != null) {
                        mcs.RemoveCommand(cmdShowDesignerActions); 
                    }
                } 
            } 

            serviceProvider = null; 
            behaviorService = null;
            selSvc = null;

            if (designerActionService != null) { 
                designerActionService.DesignerActionListsChanged -= new DesignerActionListsChangedEventHandler(this.OnDesignerActionsChanged);
                if (disposeActionService) { 
                    designerActionService.Dispose(); 
                }
            } 
            designerActionService = null;

            if  (designerActionUIService != null) {
                designerActionUIService.DesignerActionUIStateChange -= new DesignerActionUIStateChangeEventHandler(this.OnDesignerActionUIStateChange); 
                if (disposeActionUIService) {
                    designerActionUIService.Dispose(); 
                } 
            }
            designerActionUIService = null; 

            designerActionAdorner = null;

        } 

 
        public DesignerActionGlyph GetDesignerActionGlyph(IComponent comp) { 
            return GetDesignerActionGlyph(comp, null);
        } 

        internal DesignerActionGlyph GetDesignerActionGlyph(IComponent comp, DesignerActionListCollection dalColl) {
            // check this component origin, this class or is it readyonly because inherited...
            InheritanceAttribute attribute = (InheritanceAttribute)TypeDescriptor.GetAttributes(comp)[typeof(InheritanceAttribute)]; 
            if(attribute == InheritanceAttribute.InheritedReadOnly) { // only do it if we can change the control...
                return null; 
            } 

            // we didnt get on, fetch it 
            if(dalColl == null) {
                dalColl = designerActionService.GetComponentActions(comp);
            }
 
            if(dalColl!= null && dalColl.Count > 0) {
                DesignerActionGlyph dag = null; 
                if(componentToGlyph[comp] == null) { 
                    DesignerActionBehavior dab = new DesignerActionBehavior(serviceProvider, comp, dalColl, this);
 
                    //if comp is a component then try to find a traycontrol associated with it...
                    // this should really be in ComponentTray but there is no behaviorService for the CT
                    if (!(comp is Control) || comp is ToolStripDropDown) {
                        //Here, we'll try to get the traycontrol associated with 
                        //the comp and supply the glyph with an alternative bounds
                        ComponentTray compTray = serviceProvider.GetService(typeof(ComponentTray)) as ComponentTray; 
                        if (compTray != null) { 
                            ComponentTray.TrayControl trayControl = compTray.GetTrayControlFromComponent(comp);
                            if (trayControl != null) { 
                                Rectangle trayBounds = trayControl.Bounds;
                                dag = new DesignerActionGlyph(dab,  trayBounds, compTray);
                            }
                        } 
                    }
 
                    //either comp is a control or we failed to find a traycontrol (which could be the case 
                    //for toolstripitem components) - in this case just create a standard glyoh.
                    if (dag == null) { 
                        //if the related comp is a control, then this shortcut will just hang off its bounds
                        dag = new DesignerActionGlyph(dab, designerActionAdorner);
                    }
 
                    if (dag != null) {
                        //store off this relationship 
                        componentToGlyph.Add(comp, dag); 
                    }
                } 
                else {
                    dag = componentToGlyph[comp] as DesignerActionGlyph;
                    if (dag != null) {
                        DesignerActionBehavior behavior = dag.Behavior as DesignerActionBehavior; 
                        if (behavior != null) {
                            behavior.ActionLists = dalColl; 
                        } 
                        dag.Invalidate(); // need to invalidate here too, someone could have called refresh too soon,
                                            //causing the glyph to get created in the wrong place 
                    }
                }
                return dag;
            } else { 
                // the list is now empty... remove the panel and glyph for this control
                RemoveActionGlyph(comp); 
                return null; 
            }
 
        }

        /// <include file='doc\DesignerActionUI.uex' path='docs/doc[@for="DesignerActionUI.OnComponentChanged"]/*' />
        /// <devdoc> 
        ///     We monitor this event so we can update smart tag locations when
        ///     controls move. 
        /// </devdoc> 
        private void OnComponentChanged(object source, ComponentChangedEventArgs ce) {
            //validate event args 
            if (ce.Component == null || ce.Member == null || !IsDesignerActionPanelVisible) {
                return;
            }
 
            // VSWhidbey 497545.
            // If the smart tag is showing, we only move the smart tag if the changing 
            // component is the component for the currently showing smart tag. 
            if (lastPanelComponent != null && !lastPanelComponent.Equals(ce.Component)) {
                return; 
            }

            //if something changed on a component we have actions associated with
            //then invalidate all (repaint & reposition) 
            DesignerActionGlyph glyph = componentToGlyph[ce.Component] as DesignerActionGlyph;
            if (glyph != null) { 
                glyph.Invalidate(); 

                if(ce.Member.Name.Equals("Dock")) { // this is the only case were we don't require an explicit refresh 
                    RecreatePanel(ce.Component as IComponent); // because 99% of the time the action is name "dock in parent container" and get replaced by "undock"
                }

                if (ce.Member.Name.Equals("Location") || 
                     ce.Member.Name.Equals("Width") ||
                     ce.Member.Name.Equals("Height")) { 
                    // we don't need to regen, we just need to update location 
                    // calculate the position of the form hosting the panel
                    UpdateDAPLocation(ce.Component as IComponent, glyph); 
                }
            }
        }
 
        private void RecreatePanel(IComponent comp) {
            if(inTransaction || comp != selSvc.PrimarySelection) { //we only ever need to do that when the comp is the primary selection 
                return; 
            }
            // we check wether or not we're in a transaction, if we are, we only the refresh at the 
            // end of the transaction to avoid flicker.
            IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;
            if(host!=null) {
                bool hostIsClosingTransaction = false; 
                IDesignerHostTransactionState hostTransactionState = host as IDesignerHostTransactionState;
                if (hostTransactionState != null) 
                { 
                    hostIsClosingTransaction = hostTransactionState.IsClosingTransaction;
                } 
                if (host.InTransaction && !hostIsClosingTransaction)
                {
                    //Debug.WriteLine("In transaction, bail, but first hookup to the end of the transaction...");
                    host.TransactionClosed += new DesignerTransactionCloseEventHandler(this.DesignerTransactionClosed); 
                    inTransaction = true;
                    relatedComponentTransaction = comp; 
                    return; 
                }
            } 
            RecreateInternal(comp);
        }

         private void DesignerTransactionClosed(object sender, DesignerTransactionCloseEventArgs e) { 
            if(e.LastTransaction && relatedComponentTransaction != null) {
                // surprise surprise we can get multiple even with e.LastTransaction set to true, even though we unhook here 
                // this is because the list on which we enumerate (the event handler list) is copied before it's enumerated on 
                // which means that if the undo engine for example creates and commit a transaction during the OnCancel of another
                // completed transaction we will get this twice. So we have to check also for relatedComponentTransaction != null 
                inTransaction = false;
                //Debug.WriteLine("End of the transaction, refresh...");
                IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;
                host.TransactionClosed -= new DesignerTransactionCloseEventHandler(this.DesignerTransactionClosed); 
                //Debug.WriteLine("End of the transaction, refresh... on component");
                RecreateInternal(relatedComponentTransaction); 
                relatedComponentTransaction = null; 
            }
        } 

        private void RecreateInternal(IComponent comp) {
            //Debug.WriteLine("not in a transaction, do it now!");
            DesignerActionGlyph glyph = GetDesignerActionGlyph(comp); 
            if (glyph != null) {
                //Debug.WriteLine("Recreating panel for component " + comp.Site.Name); 
                VerifyGlyphIsInAdorner(glyph); // this could happen when a verb change state or suddendly a control gets 
                                            // a new action in the panel and we are the primary selection
                                            // in that case there would not be a glyph active in the adorner to be shown 
                                            // because we update that on selection change. We have to do that here too. Sad really...
                RecreatePanel(glyph); // recreate the DAP itself
                UpdateDAPLocation(comp, glyph); // reposition the thing
            } 
        }
        private void RecreatePanel(Glyph glyphWithPanelToRegen) { 
            // we don't want to do anything if the panel is not visible 
            if(!IsDesignerActionPanelVisible) {
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionUI.RecreatePanel] panel is not visible, bail"); 
                return;
            }
            //recreate a designeraction panel
            if(glyphWithPanelToRegen != null) { 
                DesignerActionBehavior behaviorWithPanelToRegen = glyphWithPanelToRegen.Behavior as DesignerActionBehavior;
                if(behaviorWithPanelToRegen!= null) { 
                    //DesignerActionPanel dap = behaviorWithPanelToRegen.CreateDesignerActionPanel(behaviorWithPanelToRegen.RelatedComponent); 
                    //designerActionHost.SetDesignerActionPanel(dap, glyphWithPanelToRegen);
                    Debug.Assert(behaviorWithPanelToRegen.RelatedComponent != null, "could not find related component for this refresh"); 
                    DesignerActionPanel dap = designerActionHost.CurrentPanel; // WE DO NOT RECREATE THE WHOLE THING / WE UPDATE THE TASKS - should flicker less
                    dap.UpdateTasks(behaviorWithPanelToRegen.ActionLists, new DesignerActionListCollection(), SR.GetString(SR.DesignerActionPanel_DefaultPanelTitle,
                        behaviorWithPanelToRegen.RelatedComponent.GetType().Name), null);
                    designerActionHost.UpdateContainerSize(); 
                }
            } 
        } 

        private void VerifyGlyphIsInAdorner(DesignerActionGlyph glyph) { 
            if (glyph.IsInComponentTray) {
                ComponentTray compTray = serviceProvider.GetService(typeof(ComponentTray)) as ComponentTray;
                if(compTray.SelectionGlyphs != null && !compTray.SelectionGlyphs.Contains(glyph)) {
                    compTray.SelectionGlyphs.Insert(0, glyph); 
                }
            } else { 
                if(designerActionAdorner != null && designerActionAdorner.Glyphs != null && !designerActionAdorner.Glyphs.Contains(glyph)) { 
                    designerActionAdorner.Glyphs.Insert(0,glyph);
                } 
            }
            glyph.InvalidateOwnerLocation();
        }
 
        /// <devdoc>
        ///     This event is fired by the DesignerActionService in response 
        ///     to a DesignerActionCollection changing.  The event args contains 
        ///     information about the related object, the type of change (added
        ///     or removed) and the remaining DesignerActionCollection for the 
        ///     object.
        ///     Note that when new DesignerActions are added, if the related control
        ///     is not yet parented - we add these actions to a "delay" list and they
        ///     are later created when the control is finally parented. 
        /// </devdoc>
        private void OnDesignerActionsChanged(object sender, DesignerActionListsChangedEventArgs e) { 
            // We need to invoke this async because the designer action service will 
            // raise this event from the thread pool.
            if (marshalingControl != null && marshalingControl.IsHandleCreated) { 
                marshalingControl.BeginInvoke(new ActionChangedEventHandler(OnInvokedDesignerActionChanged), new object[] {sender, e});
            }
        }
 
        private void OnDesignerActionUIStateChange(object sender, DesignerActionUIStateChangeEventArgs e) {
            IComponent comp = e.RelatedObject as IComponent; 
            Debug.Assert(comp!=null || e.ChangeType == DesignerActionUIStateChangeType.Hide, "related object is not an IComponent, something is wrong here..."); 
            if(comp!=null) {
                DesignerActionGlyph relatedGlyph = GetDesignerActionGlyph(comp); 
                if(relatedGlyph != null) {
                    if(e.ChangeType == DesignerActionUIStateChangeType.Show) {
                        DesignerActionBehavior behavior = relatedGlyph.Behavior as DesignerActionBehavior;
                        if(behavior != null) { 
                            behavior.ShowUI(relatedGlyph);
                        } 
                    } else if (e.ChangeType == DesignerActionUIStateChangeType.Hide){ 
                        DesignerActionBehavior behavior = relatedGlyph.Behavior as DesignerActionBehavior;
                        if(behavior != null) { 
                            behavior.HideUI();
                        }
                    } else if (e.ChangeType == DesignerActionUIStateChangeType.Refresh) {
                        relatedGlyph.Invalidate(); 
                        RecreatePanel((IComponent)e.RelatedObject);
                        /*BehaviorService.MenuCommandHandler mch = menuCommandService as BehaviorService.MenuCommandHandler; 
                        if(mch != null && mch.MenuService != null) { 
                            MenuCommandService mcs = mch.MenuService as MenuCommandService;
                            if(mcs != null) { 
                                mcs.InvalidateVerbsCollection();
                            } else {
                                Debug.Fail("Could not find our way to the real MenuCommandService");
                            } 
                        } else {
                            Debug.Fail("Could not find our way to the real MenuCommandService"); 
                        }*/ 
                    }
                } 
            } else {
                if (e.ChangeType == DesignerActionUIStateChangeType.Hide){
                    HideDesignerActionPanel();
                } 
            }
        } 
 
        /// <devdoc>
        ///     This is the same as DesignerActionChanged, but it is invoked on our control's thread 
        /// </devdoc>
        private void OnInvokedDesignerActionChanged(object sender, DesignerActionListsChangedEventArgs e) {

            IComponent relatedComponent = e.RelatedObject as IComponent; 
            DesignerActionGlyph g = null;
            if (e.ChangeType == DesignerActionListsChangedType.ActionListsAdded) { 
                if (relatedComponent == null) { 
                    Debug.Fail("How can we add a DesignerAction glyphs when it's related object is not  an IComponent?");
                    return; 
                }

                IComponent primSel = selSvc.PrimarySelection as IComponent;
                if (primSel == e.RelatedObject) { 
                    g = GetDesignerActionGlyph(relatedComponent , e.ActionLists);
                    if(g != null) { 
                        VerifyGlyphIsInAdorner(g); 
                    } else {
                        RemoveActionGlyph(e.RelatedObject); 
                    }
                }
            }
 
            if (e.ChangeType == DesignerActionListsChangedType.ActionListsRemoved && e.ActionLists.Count == 0) {
                //only remove our glyph if there are no more DesignerActions 
                //associated with it. 
                RemoveActionGlyph(e.RelatedObject);
            } else if(g!=null) { 
                // we need to recreate the panel here, since it's content has changed...
                RecreatePanel(relatedComponent);
            }
        } 

        /// <devdoc> 
        ///     Called when our KeyShowDesignerActions menu command is fired 
        ///     (a.k.a. Alt+Shift+F10) - we will find the primary selection,
        ///     see if it has designer actions, and if so - show the menu. 
        /// </devdoc>
        private void OnKeyShowDesignerActions(object sender, EventArgs e) {
            ShowDesignerActionPanelForPrimarySelection();
        } 

 
 
        // we cannot attach several menu command to the same command id, we need
        // a single entry point, we put it in designershortcutui. but we need a way to call the show ui on the related behavior 
        // hence this internal function to hack it together
        //we return false if we have nothing to display, we hide it and return true if we're already displaying
        internal bool ShowDesignerActionPanelForPrimarySelection() {
            //can't do anythign w/o selection service 
            if (selSvc == null) {
                return false; 
            } 

            object primarySelection = selSvc.PrimarySelection; 

            //verfiy that we have obtained a valid component with designer actions
            if (primarySelection == null || !componentToGlyph.Contains(primarySelection)) {
                return false; 
            }
 
            DesignerActionGlyph glyph = (DesignerActionGlyph)componentToGlyph[primarySelection]; 

            if (glyph != null && glyph.Behavior is DesignerActionBehavior) { 
                // show the menu
                DesignerActionBehavior behavior = glyph.Behavior as DesignerActionBehavior;
                if(behavior != null) {
                    if(!IsDesignerActionPanelVisible) { 
                        behavior.ShowUI(glyph);
                        return true; 
                    } else { 
                        behavior.HideUI();
                        return false; 
                    }
                }
            }
            return false; 
        }
 
 
        /// <devdoc>
        ///     When all the DesignerActions have been removed for a particular 
        ///     object, we remove any UI (glyphs) that we may have been managing.
        /// </devdoc>
        internal void RemoveActionGlyph(object relatedObject) {
            if (relatedObject == null) { 
                return;
            } 
 
            if(IsDesignerActionPanelVisible && relatedObject == lastPanelComponent) {
                HideDesignerActionPanel(); 
            }

            DesignerActionGlyph glyph = (DesignerActionGlyph)componentToGlyph[relatedObject];
 
            if (glyph != null) {
 
               // Check ComponentTray first 
               ComponentTray compTray = serviceProvider.GetService(typeof(ComponentTray)) as ComponentTray;
               if(compTray != null && compTray.SelectionGlyphs != null) { 
                   if (compTray != null && compTray.SelectionGlyphs.Contains(glyph)) {
                      compTray.SelectionGlyphs.Remove(glyph);
                   }
               } 

               if(designerActionAdorner.Glyphs.Contains(glyph)) { 
                    designerActionAdorner.Glyphs.Remove(glyph); 
               }
               componentToGlyph.Remove(relatedObject); 

               // we only do this when we're in a transaction, see bug VSWHIDBEY 418709. This is for compat reason - infragistic. if we're not in a transaction, too bad, we don't update the screen
               IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;
               if(host!=null && host.InTransaction) { 
                    host.TransactionClosed += new DesignerTransactionCloseEventHandler(this.InvalidateGlyphOnLastTransaction);
                    relatedGlyphTransaction = glyph; 
               } 
            }
 
        }

        private void InvalidateGlyphOnLastTransaction(object sender, DesignerTransactionCloseEventArgs e) {
            if(e.LastTransaction) { 
                IDesignerHost host = (serviceProvider != null) ? serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost : null;
                if (host != null) { 
                    host.TransactionClosed -= new DesignerTransactionCloseEventHandler(this.InvalidateGlyphOnLastTransaction); 
                }
 
                if(relatedGlyphTransaction != null) {
                    relatedGlyphTransaction.InvalidateOwnerLocation();
                }
                relatedGlyphTransaction = null; 
            }
        } 
 
        internal void HideDesignerActionPanel() {
            if(IsDesignerActionPanelVisible) { 
                designerActionHost.Close();
            }
        }
 
        internal bool IsDesignerActionPanelVisible {
            get { 
                return (designerActionHost != null && designerActionHost.Visible); 
            }
        } 

        internal IComponent LastPanelComponent {
            get {
                return (IsDesignerActionPanelVisible ? this.lastPanelComponent : null); 
            }
        } 
 
        private void toolStripDropDown_Closing(object sender, ToolStripDropDownClosingEventArgs e) {
            if (cancelClose || e.Cancel) { 
                e.Cancel = true;
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionUI.toolStripDropDown_Closing] cancelClose true, bail");
                return;
            } 
            if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked) {
                e.Cancel = true; 
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionUI.toolStripDropDown_Closing] ItemClicked: e.Cancel set to: " + e.Cancel.ToString()); 
            }
            if(e.CloseReason == ToolStripDropDownCloseReason.Keyboard) { 
                e.Cancel = false;
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionUI.toolStripDropDown_Closing] Keyboard: e.Cancel set to: " + e.Cancel.ToString());
            }
 
            if(e.Cancel == false) { // we WILL disappear
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionUI.toolStripDropDown_Closing] Closing..."); 
                Debug.Assert(lastPanelComponent!=null, "last panel component should not be null here... "+ 
                    "(except if you're currently debugging VS where deactivation messages in the middle of the pump can mess up everything...)");
                if(lastPanelComponent == null) 
                    return;
                // if we're actually closing
                // get the coordinate of the last message, the one causing us to close, is it within the glyph coordinate
                // if it is that mean that someone just clicked back from the panel, on VS, but ON THE GLYPH, that means that he 
                // actually wants to close it. The activation change is going to do that for us but we should NOT reopen right away
                // because he clicked on the glyph... this code is here to prevent this... 
                Point point = DesignerUtils.LastCursorPoint; 
                DesignerActionGlyph currentGlyph = componentToGlyph[lastPanelComponent] as DesignerActionGlyph;
                if(currentGlyph != null) { 
                    Point glyphCoord = GetGlyphLocationScreenCoord(lastPanelComponent, currentGlyph);
                    if((new Rectangle(glyphCoord, new Size(currentGlyph.Bounds.Width, currentGlyph.Bounds.Height))).Contains(point)) {
                        DesignerActionBehavior behavior = currentGlyph.Behavior as DesignerActionBehavior;
                        behavior.IgnoreNextMouseUp = true; 
                    }
                    currentGlyph.InvalidateOwnerLocation(); 
                } 

                // unset the ownership relationship 
               /* UnsafeNativeMethods.SetWindowLong(new HandleRef(designerActionHost, designerActionHost.Handle),
                                  NativeMethods.GWL_HWNDPARENT,
                                  new HandleRef(null, IntPtr.Zero));
                                 */ 
                lastPanelComponent = null;
 
                // panel is going away, pop the behavior that's on the stack... 
                Debug.Assert(dapkb != null, "why is dapkb null?");
                System.Windows.Forms.Design.Behavior.Behavior popBehavior = behaviorService.PopBehavior(dapkb); 
                Debug.Assert(popBehavior is DesignerActionKeyboardBehavior, "behavior returned is of the wrong kind?");
            }

 
        }
 
 
        internal Point UpdateDAPLocation(IComponent component, DesignerActionGlyph glyph) {
            if(component == null) { // in case of a resize... 
                component = lastPanelComponent;
            }

            if(designerActionHost == null) { 
                return Point.Empty;
            } 
 
            if (component == null || glyph == null ) {
                return designerActionHost.Location; 
            }

            // check that the glyph is still visible in the adorner window
            if(behaviorService != null && behaviorService.AdornerWindowGraphics != null && 
                !behaviorService.AdornerWindowControl.DisplayRectangle.IntersectsWith(glyph.Bounds)) {
                HideDesignerActionPanel(); 
                return designerActionHost.Location; 
            }
 
            Point glyphLocationScreenCoord = GetGlyphLocationScreenCoord(component, glyph);
            Rectangle rectGlyph = new Rectangle(glyphLocationScreenCoord, glyph.Bounds.Size);
            DockStyle edgeToDock;
            Point pt = DesignerActionPanel.ComputePreferredDesktopLocation(rectGlyph, designerActionHost.Size, out edgeToDock); 
            glyph.DockEdge = edgeToDock;
            designerActionHost.Location = pt; 
            return pt; 
        }
 
        private Point GetGlyphLocationScreenCoord(IComponent relatedComponent, Glyph glyph) {
            Point glyphLocationScreenCoord = new Point(0,0);
            if(relatedComponent is Control && !(relatedComponent is ToolStripDropDown)) {
                Control relatedControl = relatedComponent as Control; 
                glyphLocationScreenCoord =behaviorService.AdornerWindowPointToScreen(glyph.Bounds.Location);
            } 
            //ISSUE: we can't have this special cased here - we should find a more 
            //generic approach to solving this problem
            else if (relatedComponent is ToolStripItem) { 
                ToolStripItem item = relatedComponent as ToolStripItem;
                if (item != null && item.Owner != null) {
                    glyphLocationScreenCoord = behaviorService.AdornerWindowPointToScreen(glyph.Bounds.Location);
                } 
            }
            else if (relatedComponent is IComponent) { 
                ComponentTray compTray = serviceProvider.GetService(typeof(ComponentTray)) as ComponentTray; 
                if (compTray != null) {
                    glyphLocationScreenCoord = compTray.PointToScreen(glyph.Bounds.Location); 
                }
            }
            return glyphLocationScreenCoord;
        } 

        bool cancelClose = false; 
 

        /// <devdoc> 
        ///     This shows the actual chrome paenl that is created by the
        ///     DesignerActionBehavior object.
        /// </devdoc>
        internal void ShowDesignerActionPanel(IComponent relatedComponent, DesignerActionPanel panel, DesignerActionGlyph glyph) { 
            if(designerActionHost ==null) {
                designerActionHost = new DesignerActionToolStripDropDown(this, mainParentWindow); 
                designerActionHost.AutoSize = false; 
                designerActionHost.Padding = Padding.Empty;
                designerActionHost.Renderer = new NoBorderRenderer(); 
                designerActionHost.Text = "DesignerActionTopLevelForm";

                designerActionHost.Closing +=new ToolStripDropDownClosingEventHandler(toolStripDropDown_Closing);
 

            } 
            // set the accessible name of the panel to the same title as the panel header. do that every time 
            designerActionHost.AccessibleName = SR.GetString(SR.DesignerActionPanel_DefaultPanelTitle, relatedComponent.GetType().Name);
            panel.AccessibleName = SR.GetString(SR.DesignerActionPanel_DefaultPanelTitle, relatedComponent.GetType().Name); 


            //GetDesignerActionGlyph(relatedComponent); // only here to update the ActionList collection on the behavior
            designerActionHost.SetDesignerActionPanel(panel, glyph); 
            Point location = UpdateDAPLocation(relatedComponent, glyph);
 
            // check that the panel will have at least it's parent glyph visible on the adorner window 

            if(behaviorService != null && behaviorService.AdornerWindowGraphics != null && 
                behaviorService.AdornerWindowControl.DisplayRectangle.IntersectsWith(glyph.Bounds)) {
                //behaviorService.AdornerWindowGraphics.IsVisible(glyph.Bounds)) {
                if (mainParentWindow != null && mainParentWindow.Handle != IntPtr.Zero) {
                    Debug.WriteLineIf(DesigneActionPanelTraceSwitch.TraceVerbose, "Assigning owner to mainParentWindow"); 
                    Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "Assigning owner to mainParentWindow");
                    UnsafeNativeMethods.SetWindowLong(new HandleRef(designerActionHost, designerActionHost.Handle), 
                                                      NativeMethods.GWL_HWNDPARENT, 
                                                      new HandleRef(mainParentWindow, mainParentWindow.Handle));
                } 

                //  designerActionHost.AutoClose = false;
                cancelClose = true;
 
                designerActionHost.Show(location);
                designerActionHost.Focus(); 
                // when a control is drag and dropped and authoshow is set to true 
                // the vs designer is going to get activated as soon as the control is dropped
                // we don't want to close the panel then, so we post a message (using the trick to 
                // call begin invoke) and once everything is settled re-activate the autoclose logic
                designerActionHost.BeginInvoke(new EventHandler(OnShowComplete));

                // invalidate the glyph to have it point the other way 
                glyph.InvalidateOwnerLocation();
                lastPanelComponent = relatedComponent; 
 
                // push new behavior for keyboard handling on the behavior stack
                dapkb = new DesignerActionKeyboardBehavior(designerActionHost.CurrentPanel, serviceProvider, behaviorService); 
                behaviorService.PushBehavior(dapkb);
            }
        }
 
        private void OnShowComplete(object sender, EventArgs e) {
      //      designerActionHost.AutoClose = true; 
            cancelClose = false; 

            // force the panel to be the active window - for some reason someone else could have forced VS to become 
            // active for real while we were ignoring close. This might be bad cause we'd be in a bad state.
            if(designerActionHost != null && designerActionHost.Handle != IntPtr.Zero && designerActionHost.Visible) {
                  UnsafeNativeMethods.SetActiveWindow(new HandleRef(this, designerActionHost.Handle));
                  designerActionHost.CheckFocusIsRight(); 
            }
        } 
    } 

 

    internal class DesignerActionToolStripDropDown : ToolStripDropDown {
        private IWin32Window _mainParentWindow;
        private ToolStripControlHost _panel; 
        private DesignerActionUI _designerActionUI;
        private bool _cancelClose = false; 
 
        private Glyph relatedGlyph;
 
        public DesignerActionToolStripDropDown(DesignerActionUI designerActionUI, IWin32Window mainParentWindow) {
            _mainParentWindow = mainParentWindow;
            _designerActionUI = designerActionUI;
        } 

 
        public DesignerActionPanel CurrentPanel { 
            get {
                if(this._panel != null) { 
                    return _panel.Control as DesignerActionPanel;
                } else {
                    return null;
                } 
            }
        } 
 
        // we're not topmost because we can show modal editors above us.
        protected override bool TopMost { 
            get { return false; }
        }

        public void UpdateContainerSize() { 
            if (CurrentPanel != null) {
                Size panelSize = CurrentPanel.GetPreferredSize(new Size(150, Int32.MaxValue)); 
                if (CurrentPanel.Size == panelSize) { 
                    // If the panel size didn't actually change, we still have to force
                    // a call to PerformLayout to make sure that controls get repositioned 
                    // properly within the panel. The issue arises because we did a
                    // measure-only Layout that determined some sizes, and then we end up
                    // painting with those values even though there wasn't an actual Layout
                    // performed. 
                    CurrentPanel.PerformLayout();
                } 
                else { 
                    CurrentPanel.Size = panelSize;
                } 
                ClientSize = panelSize;
            }
        }
 
        public void CheckFocusIsRight() { // hack to get the focus to NOT stay on ContainerControl
            Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "Checking focus..."); 
            IntPtr focusedControl = UnsafeNativeMethods.GetFocus(); 
            if(focusedControl == this.Handle) {
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "    putting focus on the panel..."); 
                _panel.Focus();
            }
            focusedControl = UnsafeNativeMethods.GetFocus();
            if(CurrentPanel != null && CurrentPanel.Handle == focusedControl) { 
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "    selecting next available control on the panel...");
                CurrentPanel.SelectNextControl(null, true, true, true, true); 
            } 
            focusedControl = UnsafeNativeMethods.GetFocus();
        } 

        protected override void OnLayout(LayoutEventArgs levent) {
            base.OnLayout(levent);
 
            UpdateContainerSize();
        } 
 
        protected override void OnClosing(ToolStripDropDownClosingEventArgs e) {
 
            Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "_____________________________Begin OnClose " + e.CloseReason.ToString());
            Debug.Indent();
            if (e.CloseReason == ToolStripDropDownCloseReason.AppFocusChange && _cancelClose) {
                _cancelClose = false; 
                e.Cancel = true;
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "cancel close prepopulated"); 
            } 
            // when we get closing event as a result of an activation change,
            // pre-populate e.Cancel based on why we're exiting. 
            //
            // - if it's a modal window that's owned by VS dont exit
            // - if it's a window that's owned by the toolstrip dropdown dont exit
 
            else if (e.CloseReason == ToolStripDropDownCloseReason.AppFocusChange || e.CloseReason == ToolStripDropDownCloseReason.AppClicked) {
 
                IntPtr hwndActivating = UnsafeNativeMethods.GetActiveWindow(); 
                if (this.Handle == hwndActivating && e.CloseReason == ToolStripDropDownCloseReason.AppClicked) {
                    e.Cancel = false; 
                    Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionToolStripDropDown.OnClosing] activation hasnt changed, but we've certainly clicked somewhere else.");
                }
                else if(WindowOwnsWindow(this.Handle, hwndActivating)) {
                   // we're being de-activated for someone owned by the panel 
                   e.Cancel = true;
                   Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionToolStripDropDown.OnClosing] Cancel close - the window activating is owned by this window"); 
                } 
                else if(_mainParentWindow != null && !WindowOwnsWindow(_mainParentWindow.Handle, hwndActivating)) {
                    if (IsWindowEnabled(_mainParentWindow.Handle)) { 
                       // the activated windows is not a child/owned windows of the main top level windows
                       // let toolstripdropdown handle this
                       e.Cancel = false;
                       Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionToolStripDropDown.OnClosing] Call close: the activated windows is not a child/owned windows of the main top level windows "); 
                    }
                    else { 
                        e.Cancel = true; 
                        Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionToolStripDropDown.OnClosing] we're being deactivated by a foreign window, but the main window is not enabled - we should stay up");
                    } 

                    base.OnClosing(e);
                    Debug.Unindent();
                    Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "_____________________________End OnClose e.Cancel: " + e.Cancel.ToString() ); 
                    return;
                } 
                else { 
                   Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionToolStripDropDown.OnClosing] since the designer action panel dropdown doesnt own the activating window " + hwndActivating.ToString("x") + ", calling close. ");
                } 


                // what's the owner of the windows being activated?
                IntPtr parent = UnsafeNativeMethods.GetWindowLong(new HandleRef(this, hwndActivating), 
                                                  NativeMethods.GWL_HWNDPARENT);
                // is it currently disabled (ie, the activating windows is in modal mode) 
                if(!IsWindowEnabled(parent)) { 
                   Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionToolStripDropDown.OnClosing] modal window activated - cancelling close");
                   // we are in a modal case 
                   e.Cancel = true;
                }
              } else {
           } 
           Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionToolStripDropDown.OnClosing] calling base.OnClosing with e.Cancel: " + e.Cancel.ToString());
 
           base.OnClosing(e); 
           Debug.Unindent();
           Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "_____________________________End OnClose e.Cancel: " + e.Cancel.ToString()); 

        }

        public void SetDesignerActionPanel(DesignerActionPanel panel, Glyph relatedGlyph) { 
            if(_panel != null && panel == (DesignerActionPanel)_panel.Control)
                return; 
 
            Debug.Assert(relatedGlyph != null, "related glyph cannot be null");
 
            this.relatedGlyph = relatedGlyph;

            panel.SizeChanged += new EventHandler(PanelResized);
            // hook up the event 
            if( _panel != null) {
                Items.Remove(_panel); 
                _panel.Dispose(); 
                _panel = null;
            } 
            _panel = new ToolStripControlHost(panel);
            // we don't want no margin
            _panel.Margin = Padding.Empty;
            _panel.Size = panel.Size; 

            this.SuspendLayout(); 
            this.Size = panel.Size; 
            this.Items.Add(_panel);
            this.ResumeLayout(); 

            if(this.Visible) {
                CheckFocusIsRight();
            } 

        } 
 
        private void PanelResized(object sender, System.EventArgs e) {
            Control ctrl = sender as Control; 
            if(this.Size.Width != ctrl.Size.Width || this.Size.Height != ctrl.Size.Height) {
                this.SuspendLayout();
                this.Size = ctrl.Size;
                if(_panel != null) { 
                    _panel.Size = ctrl.Size;
                } 
                _designerActionUI.UpdateDAPLocation(null, relatedGlyph as DesignerActionGlyph); 
                this.ResumeLayout();
            } 
        }


        protected override void SetVisibleCore(bool visible) { 
            Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionToolStripDropDown.SetVisibleCore] setting dropdown visible=" + visible.ToString());
            base.SetVisibleCore(visible); 
            if(visible) { 
                CheckFocusIsRight();
            } 
        }

        /// <devdoc>
        ///    General purpose method, based on Control.Contains()... 
        ///
        ///    Determines whether a given window (specified using native window handle) 
        ///    is a descendant of this control. This catches both contained descendants 
        ///    and 'owned' windows such as modal dialogs. Using window handles rather
        ///    than Control objects allows it to catch un-managed windows as well. 
        /// </devdoc>
        private static bool WindowOwnsWindow(IntPtr hWndOwner, IntPtr hWndDescendant) {
            Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[WindowOwnsWindow] Testing if " + hWndOwner.ToString("x")+ " is a owned by " + hWndDescendant.ToString("x") + "... ");
#if DEBUG 
            if (DesignerActionUI.DropDownVisibilityDebug.TraceVerbose) {
                Debug.WriteLine("\t\tOWNER: " + GetControlInformation(hWndOwner)); 
                Debug.WriteLine("\t\tOWNEE: " + GetControlInformation(hWndDescendant)); 

                IntPtr claimedOwnerHwnd = UnsafeNativeMethods.GetWindowLong(new HandleRef(null, hWndDescendant), NativeMethods.GWL_HWNDPARENT); 
                Debug.WriteLine("OWNEE's CLAIMED OWNER: "+ GetControlInformation(claimedOwnerHwnd));
            }

#endif 
            if (hWndDescendant == hWndOwner) {
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "they match, YES."); 
                return true; 
            }
 
            while (hWndDescendant != IntPtr.Zero) {
                hWndDescendant = UnsafeNativeMethods.GetWindowLong(new HandleRef(null, hWndDescendant), NativeMethods.GWL_HWNDPARENT);
                if (hWndDescendant == IntPtr.Zero) {
                    Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "NOPE."); 
                    return false;
                } 
                if (hWndDescendant == hWndOwner) { 
                    Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "YES.");
                    return true; 
                }
            }

            Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "NO."); 
            return false;
        } 
 

 

        // helper function for generating infomation about a particular control
        // use AssertControlInformation if sticking in an assert - then the work
        // to figure out the control info will only be done when the assertion is false. 
        internal static string GetControlInformation(IntPtr hwnd) {
             if (hwnd == IntPtr.Zero) { 
                return "Handle is IntPtr.Zero"; 
             }
#if DEBUG 
 	     if (!DesignerActionUI.DropDownVisibilityDebug.TraceVerbose) {
                return String.Empty;
             }
 
             int textLen = SafeNativeMethods.GetWindowTextLength(new HandleRef(null, hwnd));
             StringBuilder sb = new StringBuilder(textLen+1); 
             UnsafeNativeMethods.GetWindowText(new HandleRef(null, hwnd), sb, sb.Capacity); 

             string typeOfControl = "Unknown"; 
             string nameOfControl = "";
             Control c = Control.FromHandle(hwnd);
             if (c != null) {
                typeOfControl = c.GetType().Name; 
                if (!string.IsNullOrEmpty(c.Name)) {
                    nameOfControl += c.Name; 
                } 
                else {
                    nameOfControl += "Unknown"; 

                    ToolStripDropDown dd = c as ToolStripDropDown;
                    // some extra debug info for toolstripdropdowns...
                    if (dd != null) { 

                        if (dd.OwnerItem != null) { 
                            nameOfControl += "OwnerItem: [" + dd.OwnerItem.ToString()+ "]"; 
                        }
                    } 
                }
             }

             return sb.ToString() + "\r\n\t\t\tType: [" + typeOfControl + "] Name: [" + nameOfControl + "]"; 
#else
	     return String.Empty; 
#endif 

        } 
        private bool IsWindowEnabled(IntPtr handle) {
            int style = (int) UnsafeNativeMethods.GetWindowLong(new HandleRef(this, handle), NativeMethods.GWL_STYLE);
            return (style & NativeMethods.WS_DISABLED) == 0;
        } 

        private void WmActivate(ref Message m) { 
 
            if((int)m.WParam == NativeMethods.WA_INACTIVE) {
                IntPtr hwndActivating = m.LParam; 
                if(WindowOwnsWindow(this.Handle, hwndActivating)) {
                   Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionUI WmActivate] setting cancel close true because WindowsOwnWindow");

                   Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionUI WmActivate] checking the focus... " + GetControlInformation(UnsafeNativeMethods.GetFocus())); 

                   _cancelClose = true; 
                } 
                else {
                    _cancelClose = false; 
                }
            }
            else {
                _cancelClose = false; 
            }
 
            base.WndProc(ref m); 
        }
 
        protected override void WndProc(ref Message m) {
            switch (m.Msg) {
                case NativeMethods.WM_ACTIVATE:
                    WmActivate(ref m); 
                    return;
            } 
            base.WndProc(ref m); 

        } 

        protected override bool ProcessDialogKey(Keys keyData) {
            // since we're not hosted in a form we need to do the same logic
            // as Form.cs. If we get an enter key we need to find the current focused control 
            // if it's a button, we click it and return that we handled the message
            if(keyData == Keys.Enter) { 
                IntPtr focusedControlPtr = UnsafeNativeMethods.GetFocus(); 
                Control focusedControl = Control.FromChildHandle(focusedControlPtr);
                IButtonControl button = focusedControl as IButtonControl; 
                if (button != null && button is Control) {
                    button.PerformClick();
                    return true;
                } 
            }
            /* should not need that anymore... *//* 
            if (   keyData == (Keys.Menu | Keys.Alt) || 
                    keyData == Keys.F4 ||
                    keyData == (Keys.Alt | Keys.Down) || 
                    keyData == (Keys.Alt | Keys.Up)) { //HACK HACK HACK  DesignerActionPanel should handle message routing properly
            // I don't think that's the case now. checking this in to get the suite to pass. Here we prevent VS from getting the F4
                IntPtr focusedControlPtr = UnsafeNativeMethods.GetFocus();
                if(WindowOwnsWindow(this.Handle, focusedControlPtr)) { 
                    // we don't want VS to even get the message, but we want to
                    // make sure it'll cause an OnKeyDown on the panel (who has focus) 
                    return false; 
                }
            }*/ 
            return base.ProcessDialogKey(keyData);
        }
    }
 

 
    internal class NoBorderRenderer : ToolStripProfessionalRenderer { 
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) {
        } 
    }
}

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Windows.Forms.Design { 

    using System;
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing;
    using System.Runtime.InteropServices; 
    using System.Windows.Forms;
    using System.Windows.Forms.Design.Behavior;
    using System.Text;
 
    /// <include file='doc\DesignerActionUI.uex' path='docs/doc[@for="DesignerActionUI"]/*' />
    /// <devdoc> 
    ///     The DesignerActionUI is the designer/UI-specific implementation of the 
    ////    DesignerActions feature.  This class instantiates the DesignerActionService
    ///     and hooks to its DesignerActionsChanged event.  Responding to this single 
    ///     event will enable the DesignerActionUI to perform all neceessary UI-related
    ///     operations.
    ///     Note that the DesignerActionUI uses the BehaviorService to manage all UI
    ///     interaction.  For every component containing a DesignerAction (determined 
    ///     by the DesignerActionsChagned event) there will be an associated
    ///     DesignerActionGlyph and DesignerActionBehavior. 
    ///     Finally, the DesignerActionUI is also responsible for showing and managing 
    ///     the Action's context menus.  Note that every DesignerAction context menu has
    ///     an item that will bring up the DesignerActions option pane in the options 
    ///     dialog.
    /// </devdoc>
    internal class DesignerActionUI : IDisposable {
 
        private static TraceSwitch DesigneActionPanelTraceSwitch     = new TraceSwitch("DesigneActionPanelTrace", "DesignerActionPanel tracing");
 
        private Adorner                 designerActionAdorner;//used to add designeraction-related glyphs 
        private IServiceProvider        serviceProvider;//standard service provider
        private ISelectionService       selSvc;//used to determine if comps have selection or not 
        private DesignerActionService   designerActionService;//this is how all designeractions will be managed
        private DesignerActionUIService   designerActionUIService;//this is how all designeractions UI elements will be managed
        private BehaviorService         behaviorService;//this is how all of our UI is implemented (glyphs, behaviors, etc...)
        private IMenuCommandService      menuCommandService; 
        private DesignerActionKeyboardBehavior dapkb;   //out keyboard behavior
        private Hashtable               componentToGlyph;//used for quick reference between compoments and our glyphs 
        private Control                 marshalingControl;//used to invoke events on our main gui thread 
        private IComponent              lastPanelComponent;
 
        private IUIService              uiService;
        private IWin32Window            mainParentWindow;
        internal DesignerActionToolStripDropDown      designerActionHost;
 
        private MenuCommand             cmdShowDesignerActions;//used to respond to the Alt+Shft+F10 command
        private bool                    inTransaction = false; 
        private IComponent              relatedComponentTransaction; 
        private DesignerActionGlyph     relatedGlyphTransaction;
        private bool                    disposeActionService; 
        private bool                    disposeActionUIService;


        private delegate void ActionChangedEventHandler(object sender, DesignerActionListsChangedEventArgs e); 

#if DEBUG 
        internal static readonly TraceSwitch DropDownVisibilityDebug = new TraceSwitch("DropDownVisibilityDebug", "Debug ToolStrip Selection code"); 
#else
        internal static readonly TraceSwitch DropDownVisibilityDebug; 
#endif

        /// <include file='doc\DesignerActionUI.uex' path='docs/doc[@for="DesignerActionUI.DesignerActionUI"]/*' />
        /// <devdoc> 
        ///     Constructor that takes a service provider.  This is needed to establish
        ///     references to the BehaviorService and SelecteionService, as well as 
        ///     spin-up the DesignerActionService. 
        /// </devdoc>
        public DesignerActionUI(IServiceProvider serviceProvider, Adorner containerAdorner) { 

            this.serviceProvider = serviceProvider;
            this.designerActionAdorner = containerAdorner;
 
            behaviorService = (BehaviorService)serviceProvider.GetService(typeof(BehaviorService));
            menuCommandService = (IMenuCommandService)serviceProvider.GetService(typeof(IMenuCommandService)); 
            selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService)); 

            if (behaviorService == null || selSvc == null) { 
                Debug.Fail("Either BehaviorService or ISelectionService is null, cannot continue.");
                return;
            }
 
            //query for our DesignerActionService
            designerActionService = (DesignerActionService)serviceProvider.GetService(typeof(DesignerActionService)); 
            if (designerActionService == null) { 
                //start the service
                designerActionService = new DesignerActionService(serviceProvider); 
                disposeActionService = true;
            }
            designerActionUIService = (DesignerActionUIService)serviceProvider.GetService(typeof(DesignerActionUIService));
            if (designerActionUIService == null) { 
                designerActionUIService = new DesignerActionUIService(serviceProvider);
                disposeActionUIService = true; 
            } 
            designerActionUIService.DesignerActionUIStateChange += new DesignerActionUIStateChangeEventHandler(this.OnDesignerActionUIStateChange);
            designerActionService.DesignerActionListsChanged += new DesignerActionListsChangedEventHandler(this.OnDesignerActionsChanged); 
            lastPanelComponent = null;

            IComponentChangeService cs = (IComponentChangeService)serviceProvider.GetService(typeof(IComponentChangeService));
            if (cs != null) { 
                cs.ComponentChanged += new ComponentChangedEventHandler(this.OnComponentChanged);
            } 
 

            if (menuCommandService != null) { 
                cmdShowDesignerActions = new MenuCommand(new EventHandler(OnKeyShowDesignerActions), MenuCommands.KeyInvokeSmartTag);
                menuCommandService.AddCommand(cmdShowDesignerActions);
            }
 
            uiService = (IUIService)serviceProvider.GetService(typeof(IUIService));
            if(uiService != null) 
                mainParentWindow = uiService.GetDialogOwnerWindow(); 

            componentToGlyph = new Hashtable(); 

            marshalingControl = new Control();
            marshalingControl.CreateControl();
        } 

 
        /// <include file='doc\DesignerActionUI.uex' path='docs/doc[@for="DesignerActionUI.Dispose"]/*' /> 
        /// <devdoc>
        ///     Disposes all UI-related objects and unhooks services. 
        /// </devdoc>

        // Don't need to dispose of designerActionUIService.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")] 
        public void Dispose() {
 
            if (marshalingControl != null) { 
                marshalingControl.Dispose();
                marshalingControl = null; 
            }


            if (serviceProvider != null) { 
                IComponentChangeService cs = (IComponentChangeService)serviceProvider.GetService(typeof(IComponentChangeService));
                if (cs != null) { 
                    cs.ComponentChanged -= new ComponentChangedEventHandler(this.OnComponentChanged); 
                }
 
                if (cmdShowDesignerActions != null) {
                    IMenuCommandService mcs = (IMenuCommandService)serviceProvider.GetService(typeof(IMenuCommandService));
                    if (mcs != null) {
                        mcs.RemoveCommand(cmdShowDesignerActions); 
                    }
                } 
            } 

            serviceProvider = null; 
            behaviorService = null;
            selSvc = null;

            if (designerActionService != null) { 
                designerActionService.DesignerActionListsChanged -= new DesignerActionListsChangedEventHandler(this.OnDesignerActionsChanged);
                if (disposeActionService) { 
                    designerActionService.Dispose(); 
                }
            } 
            designerActionService = null;

            if  (designerActionUIService != null) {
                designerActionUIService.DesignerActionUIStateChange -= new DesignerActionUIStateChangeEventHandler(this.OnDesignerActionUIStateChange); 
                if (disposeActionUIService) {
                    designerActionUIService.Dispose(); 
                } 
            }
            designerActionUIService = null; 

            designerActionAdorner = null;

        } 

 
        public DesignerActionGlyph GetDesignerActionGlyph(IComponent comp) { 
            return GetDesignerActionGlyph(comp, null);
        } 

        internal DesignerActionGlyph GetDesignerActionGlyph(IComponent comp, DesignerActionListCollection dalColl) {
            // check this component origin, this class or is it readyonly because inherited...
            InheritanceAttribute attribute = (InheritanceAttribute)TypeDescriptor.GetAttributes(comp)[typeof(InheritanceAttribute)]; 
            if(attribute == InheritanceAttribute.InheritedReadOnly) { // only do it if we can change the control...
                return null; 
            } 

            // we didnt get on, fetch it 
            if(dalColl == null) {
                dalColl = designerActionService.GetComponentActions(comp);
            }
 
            if(dalColl!= null && dalColl.Count > 0) {
                DesignerActionGlyph dag = null; 
                if(componentToGlyph[comp] == null) { 
                    DesignerActionBehavior dab = new DesignerActionBehavior(serviceProvider, comp, dalColl, this);
 
                    //if comp is a component then try to find a traycontrol associated with it...
                    // this should really be in ComponentTray but there is no behaviorService for the CT
                    if (!(comp is Control) || comp is ToolStripDropDown) {
                        //Here, we'll try to get the traycontrol associated with 
                        //the comp and supply the glyph with an alternative bounds
                        ComponentTray compTray = serviceProvider.GetService(typeof(ComponentTray)) as ComponentTray; 
                        if (compTray != null) { 
                            ComponentTray.TrayControl trayControl = compTray.GetTrayControlFromComponent(comp);
                            if (trayControl != null) { 
                                Rectangle trayBounds = trayControl.Bounds;
                                dag = new DesignerActionGlyph(dab,  trayBounds, compTray);
                            }
                        } 
                    }
 
                    //either comp is a control or we failed to find a traycontrol (which could be the case 
                    //for toolstripitem components) - in this case just create a standard glyoh.
                    if (dag == null) { 
                        //if the related comp is a control, then this shortcut will just hang off its bounds
                        dag = new DesignerActionGlyph(dab, designerActionAdorner);
                    }
 
                    if (dag != null) {
                        //store off this relationship 
                        componentToGlyph.Add(comp, dag); 
                    }
                } 
                else {
                    dag = componentToGlyph[comp] as DesignerActionGlyph;
                    if (dag != null) {
                        DesignerActionBehavior behavior = dag.Behavior as DesignerActionBehavior; 
                        if (behavior != null) {
                            behavior.ActionLists = dalColl; 
                        } 
                        dag.Invalidate(); // need to invalidate here too, someone could have called refresh too soon,
                                            //causing the glyph to get created in the wrong place 
                    }
                }
                return dag;
            } else { 
                // the list is now empty... remove the panel and glyph for this control
                RemoveActionGlyph(comp); 
                return null; 
            }
 
        }

        /// <include file='doc\DesignerActionUI.uex' path='docs/doc[@for="DesignerActionUI.OnComponentChanged"]/*' />
        /// <devdoc> 
        ///     We monitor this event so we can update smart tag locations when
        ///     controls move. 
        /// </devdoc> 
        private void OnComponentChanged(object source, ComponentChangedEventArgs ce) {
            //validate event args 
            if (ce.Component == null || ce.Member == null || !IsDesignerActionPanelVisible) {
                return;
            }
 
            // VSWhidbey 497545.
            // If the smart tag is showing, we only move the smart tag if the changing 
            // component is the component for the currently showing smart tag. 
            if (lastPanelComponent != null && !lastPanelComponent.Equals(ce.Component)) {
                return; 
            }

            //if something changed on a component we have actions associated with
            //then invalidate all (repaint & reposition) 
            DesignerActionGlyph glyph = componentToGlyph[ce.Component] as DesignerActionGlyph;
            if (glyph != null) { 
                glyph.Invalidate(); 

                if(ce.Member.Name.Equals("Dock")) { // this is the only case were we don't require an explicit refresh 
                    RecreatePanel(ce.Component as IComponent); // because 99% of the time the action is name "dock in parent container" and get replaced by "undock"
                }

                if (ce.Member.Name.Equals("Location") || 
                     ce.Member.Name.Equals("Width") ||
                     ce.Member.Name.Equals("Height")) { 
                    // we don't need to regen, we just need to update location 
                    // calculate the position of the form hosting the panel
                    UpdateDAPLocation(ce.Component as IComponent, glyph); 
                }
            }
        }
 
        private void RecreatePanel(IComponent comp) {
            if(inTransaction || comp != selSvc.PrimarySelection) { //we only ever need to do that when the comp is the primary selection 
                return; 
            }
            // we check wether or not we're in a transaction, if we are, we only the refresh at the 
            // end of the transaction to avoid flicker.
            IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;
            if(host!=null) {
                bool hostIsClosingTransaction = false; 
                IDesignerHostTransactionState hostTransactionState = host as IDesignerHostTransactionState;
                if (hostTransactionState != null) 
                { 
                    hostIsClosingTransaction = hostTransactionState.IsClosingTransaction;
                } 
                if (host.InTransaction && !hostIsClosingTransaction)
                {
                    //Debug.WriteLine("In transaction, bail, but first hookup to the end of the transaction...");
                    host.TransactionClosed += new DesignerTransactionCloseEventHandler(this.DesignerTransactionClosed); 
                    inTransaction = true;
                    relatedComponentTransaction = comp; 
                    return; 
                }
            } 
            RecreateInternal(comp);
        }

         private void DesignerTransactionClosed(object sender, DesignerTransactionCloseEventArgs e) { 
            if(e.LastTransaction && relatedComponentTransaction != null) {
                // surprise surprise we can get multiple even with e.LastTransaction set to true, even though we unhook here 
                // this is because the list on which we enumerate (the event handler list) is copied before it's enumerated on 
                // which means that if the undo engine for example creates and commit a transaction during the OnCancel of another
                // completed transaction we will get this twice. So we have to check also for relatedComponentTransaction != null 
                inTransaction = false;
                //Debug.WriteLine("End of the transaction, refresh...");
                IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;
                host.TransactionClosed -= new DesignerTransactionCloseEventHandler(this.DesignerTransactionClosed); 
                //Debug.WriteLine("End of the transaction, refresh... on component");
                RecreateInternal(relatedComponentTransaction); 
                relatedComponentTransaction = null; 
            }
        } 

        private void RecreateInternal(IComponent comp) {
            //Debug.WriteLine("not in a transaction, do it now!");
            DesignerActionGlyph glyph = GetDesignerActionGlyph(comp); 
            if (glyph != null) {
                //Debug.WriteLine("Recreating panel for component " + comp.Site.Name); 
                VerifyGlyphIsInAdorner(glyph); // this could happen when a verb change state or suddendly a control gets 
                                            // a new action in the panel and we are the primary selection
                                            // in that case there would not be a glyph active in the adorner to be shown 
                                            // because we update that on selection change. We have to do that here too. Sad really...
                RecreatePanel(glyph); // recreate the DAP itself
                UpdateDAPLocation(comp, glyph); // reposition the thing
            } 
        }
        private void RecreatePanel(Glyph glyphWithPanelToRegen) { 
            // we don't want to do anything if the panel is not visible 
            if(!IsDesignerActionPanelVisible) {
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionUI.RecreatePanel] panel is not visible, bail"); 
                return;
            }
            //recreate a designeraction panel
            if(glyphWithPanelToRegen != null) { 
                DesignerActionBehavior behaviorWithPanelToRegen = glyphWithPanelToRegen.Behavior as DesignerActionBehavior;
                if(behaviorWithPanelToRegen!= null) { 
                    //DesignerActionPanel dap = behaviorWithPanelToRegen.CreateDesignerActionPanel(behaviorWithPanelToRegen.RelatedComponent); 
                    //designerActionHost.SetDesignerActionPanel(dap, glyphWithPanelToRegen);
                    Debug.Assert(behaviorWithPanelToRegen.RelatedComponent != null, "could not find related component for this refresh"); 
                    DesignerActionPanel dap = designerActionHost.CurrentPanel; // WE DO NOT RECREATE THE WHOLE THING / WE UPDATE THE TASKS - should flicker less
                    dap.UpdateTasks(behaviorWithPanelToRegen.ActionLists, new DesignerActionListCollection(), SR.GetString(SR.DesignerActionPanel_DefaultPanelTitle,
                        behaviorWithPanelToRegen.RelatedComponent.GetType().Name), null);
                    designerActionHost.UpdateContainerSize(); 
                }
            } 
        } 

        private void VerifyGlyphIsInAdorner(DesignerActionGlyph glyph) { 
            if (glyph.IsInComponentTray) {
                ComponentTray compTray = serviceProvider.GetService(typeof(ComponentTray)) as ComponentTray;
                if(compTray.SelectionGlyphs != null && !compTray.SelectionGlyphs.Contains(glyph)) {
                    compTray.SelectionGlyphs.Insert(0, glyph); 
                }
            } else { 
                if(designerActionAdorner != null && designerActionAdorner.Glyphs != null && !designerActionAdorner.Glyphs.Contains(glyph)) { 
                    designerActionAdorner.Glyphs.Insert(0,glyph);
                } 
            }
            glyph.InvalidateOwnerLocation();
        }
 
        /// <devdoc>
        ///     This event is fired by the DesignerActionService in response 
        ///     to a DesignerActionCollection changing.  The event args contains 
        ///     information about the related object, the type of change (added
        ///     or removed) and the remaining DesignerActionCollection for the 
        ///     object.
        ///     Note that when new DesignerActions are added, if the related control
        ///     is not yet parented - we add these actions to a "delay" list and they
        ///     are later created when the control is finally parented. 
        /// </devdoc>
        private void OnDesignerActionsChanged(object sender, DesignerActionListsChangedEventArgs e) { 
            // We need to invoke this async because the designer action service will 
            // raise this event from the thread pool.
            if (marshalingControl != null && marshalingControl.IsHandleCreated) { 
                marshalingControl.BeginInvoke(new ActionChangedEventHandler(OnInvokedDesignerActionChanged), new object[] {sender, e});
            }
        }
 
        private void OnDesignerActionUIStateChange(object sender, DesignerActionUIStateChangeEventArgs e) {
            IComponent comp = e.RelatedObject as IComponent; 
            Debug.Assert(comp!=null || e.ChangeType == DesignerActionUIStateChangeType.Hide, "related object is not an IComponent, something is wrong here..."); 
            if(comp!=null) {
                DesignerActionGlyph relatedGlyph = GetDesignerActionGlyph(comp); 
                if(relatedGlyph != null) {
                    if(e.ChangeType == DesignerActionUIStateChangeType.Show) {
                        DesignerActionBehavior behavior = relatedGlyph.Behavior as DesignerActionBehavior;
                        if(behavior != null) { 
                            behavior.ShowUI(relatedGlyph);
                        } 
                    } else if (e.ChangeType == DesignerActionUIStateChangeType.Hide){ 
                        DesignerActionBehavior behavior = relatedGlyph.Behavior as DesignerActionBehavior;
                        if(behavior != null) { 
                            behavior.HideUI();
                        }
                    } else if (e.ChangeType == DesignerActionUIStateChangeType.Refresh) {
                        relatedGlyph.Invalidate(); 
                        RecreatePanel((IComponent)e.RelatedObject);
                        /*BehaviorService.MenuCommandHandler mch = menuCommandService as BehaviorService.MenuCommandHandler; 
                        if(mch != null && mch.MenuService != null) { 
                            MenuCommandService mcs = mch.MenuService as MenuCommandService;
                            if(mcs != null) { 
                                mcs.InvalidateVerbsCollection();
                            } else {
                                Debug.Fail("Could not find our way to the real MenuCommandService");
                            } 
                        } else {
                            Debug.Fail("Could not find our way to the real MenuCommandService"); 
                        }*/ 
                    }
                } 
            } else {
                if (e.ChangeType == DesignerActionUIStateChangeType.Hide){
                    HideDesignerActionPanel();
                } 
            }
        } 
 
        /// <devdoc>
        ///     This is the same as DesignerActionChanged, but it is invoked on our control's thread 
        /// </devdoc>
        private void OnInvokedDesignerActionChanged(object sender, DesignerActionListsChangedEventArgs e) {

            IComponent relatedComponent = e.RelatedObject as IComponent; 
            DesignerActionGlyph g = null;
            if (e.ChangeType == DesignerActionListsChangedType.ActionListsAdded) { 
                if (relatedComponent == null) { 
                    Debug.Fail("How can we add a DesignerAction glyphs when it's related object is not  an IComponent?");
                    return; 
                }

                IComponent primSel = selSvc.PrimarySelection as IComponent;
                if (primSel == e.RelatedObject) { 
                    g = GetDesignerActionGlyph(relatedComponent , e.ActionLists);
                    if(g != null) { 
                        VerifyGlyphIsInAdorner(g); 
                    } else {
                        RemoveActionGlyph(e.RelatedObject); 
                    }
                }
            }
 
            if (e.ChangeType == DesignerActionListsChangedType.ActionListsRemoved && e.ActionLists.Count == 0) {
                //only remove our glyph if there are no more DesignerActions 
                //associated with it. 
                RemoveActionGlyph(e.RelatedObject);
            } else if(g!=null) { 
                // we need to recreate the panel here, since it's content has changed...
                RecreatePanel(relatedComponent);
            }
        } 

        /// <devdoc> 
        ///     Called when our KeyShowDesignerActions menu command is fired 
        ///     (a.k.a. Alt+Shift+F10) - we will find the primary selection,
        ///     see if it has designer actions, and if so - show the menu. 
        /// </devdoc>
        private void OnKeyShowDesignerActions(object sender, EventArgs e) {
            ShowDesignerActionPanelForPrimarySelection();
        } 

 
 
        // we cannot attach several menu command to the same command id, we need
        // a single entry point, we put it in designershortcutui. but we need a way to call the show ui on the related behavior 
        // hence this internal function to hack it together
        //we return false if we have nothing to display, we hide it and return true if we're already displaying
        internal bool ShowDesignerActionPanelForPrimarySelection() {
            //can't do anythign w/o selection service 
            if (selSvc == null) {
                return false; 
            } 

            object primarySelection = selSvc.PrimarySelection; 

            //verfiy that we have obtained a valid component with designer actions
            if (primarySelection == null || !componentToGlyph.Contains(primarySelection)) {
                return false; 
            }
 
            DesignerActionGlyph glyph = (DesignerActionGlyph)componentToGlyph[primarySelection]; 

            if (glyph != null && glyph.Behavior is DesignerActionBehavior) { 
                // show the menu
                DesignerActionBehavior behavior = glyph.Behavior as DesignerActionBehavior;
                if(behavior != null) {
                    if(!IsDesignerActionPanelVisible) { 
                        behavior.ShowUI(glyph);
                        return true; 
                    } else { 
                        behavior.HideUI();
                        return false; 
                    }
                }
            }
            return false; 
        }
 
 
        /// <devdoc>
        ///     When all the DesignerActions have been removed for a particular 
        ///     object, we remove any UI (glyphs) that we may have been managing.
        /// </devdoc>
        internal void RemoveActionGlyph(object relatedObject) {
            if (relatedObject == null) { 
                return;
            } 
 
            if(IsDesignerActionPanelVisible && relatedObject == lastPanelComponent) {
                HideDesignerActionPanel(); 
            }

            DesignerActionGlyph glyph = (DesignerActionGlyph)componentToGlyph[relatedObject];
 
            if (glyph != null) {
 
               // Check ComponentTray first 
               ComponentTray compTray = serviceProvider.GetService(typeof(ComponentTray)) as ComponentTray;
               if(compTray != null && compTray.SelectionGlyphs != null) { 
                   if (compTray != null && compTray.SelectionGlyphs.Contains(glyph)) {
                      compTray.SelectionGlyphs.Remove(glyph);
                   }
               } 

               if(designerActionAdorner.Glyphs.Contains(glyph)) { 
                    designerActionAdorner.Glyphs.Remove(glyph); 
               }
               componentToGlyph.Remove(relatedObject); 

               // we only do this when we're in a transaction, see bug VSWHIDBEY 418709. This is for compat reason - infragistic. if we're not in a transaction, too bad, we don't update the screen
               IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;
               if(host!=null && host.InTransaction) { 
                    host.TransactionClosed += new DesignerTransactionCloseEventHandler(this.InvalidateGlyphOnLastTransaction);
                    relatedGlyphTransaction = glyph; 
               } 
            }
 
        }

        private void InvalidateGlyphOnLastTransaction(object sender, DesignerTransactionCloseEventArgs e) {
            if(e.LastTransaction) { 
                IDesignerHost host = (serviceProvider != null) ? serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost : null;
                if (host != null) { 
                    host.TransactionClosed -= new DesignerTransactionCloseEventHandler(this.InvalidateGlyphOnLastTransaction); 
                }
 
                if(relatedGlyphTransaction != null) {
                    relatedGlyphTransaction.InvalidateOwnerLocation();
                }
                relatedGlyphTransaction = null; 
            }
        } 
 
        internal void HideDesignerActionPanel() {
            if(IsDesignerActionPanelVisible) { 
                designerActionHost.Close();
            }
        }
 
        internal bool IsDesignerActionPanelVisible {
            get { 
                return (designerActionHost != null && designerActionHost.Visible); 
            }
        } 

        internal IComponent LastPanelComponent {
            get {
                return (IsDesignerActionPanelVisible ? this.lastPanelComponent : null); 
            }
        } 
 
        private void toolStripDropDown_Closing(object sender, ToolStripDropDownClosingEventArgs e) {
            if (cancelClose || e.Cancel) { 
                e.Cancel = true;
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionUI.toolStripDropDown_Closing] cancelClose true, bail");
                return;
            } 
            if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked) {
                e.Cancel = true; 
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionUI.toolStripDropDown_Closing] ItemClicked: e.Cancel set to: " + e.Cancel.ToString()); 
            }
            if(e.CloseReason == ToolStripDropDownCloseReason.Keyboard) { 
                e.Cancel = false;
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionUI.toolStripDropDown_Closing] Keyboard: e.Cancel set to: " + e.Cancel.ToString());
            }
 
            if(e.Cancel == false) { // we WILL disappear
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionUI.toolStripDropDown_Closing] Closing..."); 
                Debug.Assert(lastPanelComponent!=null, "last panel component should not be null here... "+ 
                    "(except if you're currently debugging VS where deactivation messages in the middle of the pump can mess up everything...)");
                if(lastPanelComponent == null) 
                    return;
                // if we're actually closing
                // get the coordinate of the last message, the one causing us to close, is it within the glyph coordinate
                // if it is that mean that someone just clicked back from the panel, on VS, but ON THE GLYPH, that means that he 
                // actually wants to close it. The activation change is going to do that for us but we should NOT reopen right away
                // because he clicked on the glyph... this code is here to prevent this... 
                Point point = DesignerUtils.LastCursorPoint; 
                DesignerActionGlyph currentGlyph = componentToGlyph[lastPanelComponent] as DesignerActionGlyph;
                if(currentGlyph != null) { 
                    Point glyphCoord = GetGlyphLocationScreenCoord(lastPanelComponent, currentGlyph);
                    if((new Rectangle(glyphCoord, new Size(currentGlyph.Bounds.Width, currentGlyph.Bounds.Height))).Contains(point)) {
                        DesignerActionBehavior behavior = currentGlyph.Behavior as DesignerActionBehavior;
                        behavior.IgnoreNextMouseUp = true; 
                    }
                    currentGlyph.InvalidateOwnerLocation(); 
                } 

                // unset the ownership relationship 
               /* UnsafeNativeMethods.SetWindowLong(new HandleRef(designerActionHost, designerActionHost.Handle),
                                  NativeMethods.GWL_HWNDPARENT,
                                  new HandleRef(null, IntPtr.Zero));
                                 */ 
                lastPanelComponent = null;
 
                // panel is going away, pop the behavior that's on the stack... 
                Debug.Assert(dapkb != null, "why is dapkb null?");
                System.Windows.Forms.Design.Behavior.Behavior popBehavior = behaviorService.PopBehavior(dapkb); 
                Debug.Assert(popBehavior is DesignerActionKeyboardBehavior, "behavior returned is of the wrong kind?");
            }

 
        }
 
 
        internal Point UpdateDAPLocation(IComponent component, DesignerActionGlyph glyph) {
            if(component == null) { // in case of a resize... 
                component = lastPanelComponent;
            }

            if(designerActionHost == null) { 
                return Point.Empty;
            } 
 
            if (component == null || glyph == null ) {
                return designerActionHost.Location; 
            }

            // check that the glyph is still visible in the adorner window
            if(behaviorService != null && behaviorService.AdornerWindowGraphics != null && 
                !behaviorService.AdornerWindowControl.DisplayRectangle.IntersectsWith(glyph.Bounds)) {
                HideDesignerActionPanel(); 
                return designerActionHost.Location; 
            }
 
            Point glyphLocationScreenCoord = GetGlyphLocationScreenCoord(component, glyph);
            Rectangle rectGlyph = new Rectangle(glyphLocationScreenCoord, glyph.Bounds.Size);
            DockStyle edgeToDock;
            Point pt = DesignerActionPanel.ComputePreferredDesktopLocation(rectGlyph, designerActionHost.Size, out edgeToDock); 
            glyph.DockEdge = edgeToDock;
            designerActionHost.Location = pt; 
            return pt; 
        }
 
        private Point GetGlyphLocationScreenCoord(IComponent relatedComponent, Glyph glyph) {
            Point glyphLocationScreenCoord = new Point(0,0);
            if(relatedComponent is Control && !(relatedComponent is ToolStripDropDown)) {
                Control relatedControl = relatedComponent as Control; 
                glyphLocationScreenCoord =behaviorService.AdornerWindowPointToScreen(glyph.Bounds.Location);
            } 
            //ISSUE: we can't have this special cased here - we should find a more 
            //generic approach to solving this problem
            else if (relatedComponent is ToolStripItem) { 
                ToolStripItem item = relatedComponent as ToolStripItem;
                if (item != null && item.Owner != null) {
                    glyphLocationScreenCoord = behaviorService.AdornerWindowPointToScreen(glyph.Bounds.Location);
                } 
            }
            else if (relatedComponent is IComponent) { 
                ComponentTray compTray = serviceProvider.GetService(typeof(ComponentTray)) as ComponentTray; 
                if (compTray != null) {
                    glyphLocationScreenCoord = compTray.PointToScreen(glyph.Bounds.Location); 
                }
            }
            return glyphLocationScreenCoord;
        } 

        bool cancelClose = false; 
 

        /// <devdoc> 
        ///     This shows the actual chrome paenl that is created by the
        ///     DesignerActionBehavior object.
        /// </devdoc>
        internal void ShowDesignerActionPanel(IComponent relatedComponent, DesignerActionPanel panel, DesignerActionGlyph glyph) { 
            if(designerActionHost ==null) {
                designerActionHost = new DesignerActionToolStripDropDown(this, mainParentWindow); 
                designerActionHost.AutoSize = false; 
                designerActionHost.Padding = Padding.Empty;
                designerActionHost.Renderer = new NoBorderRenderer(); 
                designerActionHost.Text = "DesignerActionTopLevelForm";

                designerActionHost.Closing +=new ToolStripDropDownClosingEventHandler(toolStripDropDown_Closing);
 

            } 
            // set the accessible name of the panel to the same title as the panel header. do that every time 
            designerActionHost.AccessibleName = SR.GetString(SR.DesignerActionPanel_DefaultPanelTitle, relatedComponent.GetType().Name);
            panel.AccessibleName = SR.GetString(SR.DesignerActionPanel_DefaultPanelTitle, relatedComponent.GetType().Name); 


            //GetDesignerActionGlyph(relatedComponent); // only here to update the ActionList collection on the behavior
            designerActionHost.SetDesignerActionPanel(panel, glyph); 
            Point location = UpdateDAPLocation(relatedComponent, glyph);
 
            // check that the panel will have at least it's parent glyph visible on the adorner window 

            if(behaviorService != null && behaviorService.AdornerWindowGraphics != null && 
                behaviorService.AdornerWindowControl.DisplayRectangle.IntersectsWith(glyph.Bounds)) {
                //behaviorService.AdornerWindowGraphics.IsVisible(glyph.Bounds)) {
                if (mainParentWindow != null && mainParentWindow.Handle != IntPtr.Zero) {
                    Debug.WriteLineIf(DesigneActionPanelTraceSwitch.TraceVerbose, "Assigning owner to mainParentWindow"); 
                    Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "Assigning owner to mainParentWindow");
                    UnsafeNativeMethods.SetWindowLong(new HandleRef(designerActionHost, designerActionHost.Handle), 
                                                      NativeMethods.GWL_HWNDPARENT, 
                                                      new HandleRef(mainParentWindow, mainParentWindow.Handle));
                } 

                //  designerActionHost.AutoClose = false;
                cancelClose = true;
 
                designerActionHost.Show(location);
                designerActionHost.Focus(); 
                // when a control is drag and dropped and authoshow is set to true 
                // the vs designer is going to get activated as soon as the control is dropped
                // we don't want to close the panel then, so we post a message (using the trick to 
                // call begin invoke) and once everything is settled re-activate the autoclose logic
                designerActionHost.BeginInvoke(new EventHandler(OnShowComplete));

                // invalidate the glyph to have it point the other way 
                glyph.InvalidateOwnerLocation();
                lastPanelComponent = relatedComponent; 
 
                // push new behavior for keyboard handling on the behavior stack
                dapkb = new DesignerActionKeyboardBehavior(designerActionHost.CurrentPanel, serviceProvider, behaviorService); 
                behaviorService.PushBehavior(dapkb);
            }
        }
 
        private void OnShowComplete(object sender, EventArgs e) {
      //      designerActionHost.AutoClose = true; 
            cancelClose = false; 

            // force the panel to be the active window - for some reason someone else could have forced VS to become 
            // active for real while we were ignoring close. This might be bad cause we'd be in a bad state.
            if(designerActionHost != null && designerActionHost.Handle != IntPtr.Zero && designerActionHost.Visible) {
                  UnsafeNativeMethods.SetActiveWindow(new HandleRef(this, designerActionHost.Handle));
                  designerActionHost.CheckFocusIsRight(); 
            }
        } 
    } 

 

    internal class DesignerActionToolStripDropDown : ToolStripDropDown {
        private IWin32Window _mainParentWindow;
        private ToolStripControlHost _panel; 
        private DesignerActionUI _designerActionUI;
        private bool _cancelClose = false; 
 
        private Glyph relatedGlyph;
 
        public DesignerActionToolStripDropDown(DesignerActionUI designerActionUI, IWin32Window mainParentWindow) {
            _mainParentWindow = mainParentWindow;
            _designerActionUI = designerActionUI;
        } 

 
        public DesignerActionPanel CurrentPanel { 
            get {
                if(this._panel != null) { 
                    return _panel.Control as DesignerActionPanel;
                } else {
                    return null;
                } 
            }
        } 
 
        // we're not topmost because we can show modal editors above us.
        protected override bool TopMost { 
            get { return false; }
        }

        public void UpdateContainerSize() { 
            if (CurrentPanel != null) {
                Size panelSize = CurrentPanel.GetPreferredSize(new Size(150, Int32.MaxValue)); 
                if (CurrentPanel.Size == panelSize) { 
                    // If the panel size didn't actually change, we still have to force
                    // a call to PerformLayout to make sure that controls get repositioned 
                    // properly within the panel. The issue arises because we did a
                    // measure-only Layout that determined some sizes, and then we end up
                    // painting with those values even though there wasn't an actual Layout
                    // performed. 
                    CurrentPanel.PerformLayout();
                } 
                else { 
                    CurrentPanel.Size = panelSize;
                } 
                ClientSize = panelSize;
            }
        }
 
        public void CheckFocusIsRight() { // hack to get the focus to NOT stay on ContainerControl
            Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "Checking focus..."); 
            IntPtr focusedControl = UnsafeNativeMethods.GetFocus(); 
            if(focusedControl == this.Handle) {
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "    putting focus on the panel..."); 
                _panel.Focus();
            }
            focusedControl = UnsafeNativeMethods.GetFocus();
            if(CurrentPanel != null && CurrentPanel.Handle == focusedControl) { 
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "    selecting next available control on the panel...");
                CurrentPanel.SelectNextControl(null, true, true, true, true); 
            } 
            focusedControl = UnsafeNativeMethods.GetFocus();
        } 

        protected override void OnLayout(LayoutEventArgs levent) {
            base.OnLayout(levent);
 
            UpdateContainerSize();
        } 
 
        protected override void OnClosing(ToolStripDropDownClosingEventArgs e) {
 
            Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "_____________________________Begin OnClose " + e.CloseReason.ToString());
            Debug.Indent();
            if (e.CloseReason == ToolStripDropDownCloseReason.AppFocusChange && _cancelClose) {
                _cancelClose = false; 
                e.Cancel = true;
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "cancel close prepopulated"); 
            } 
            // when we get closing event as a result of an activation change,
            // pre-populate e.Cancel based on why we're exiting. 
            //
            // - if it's a modal window that's owned by VS dont exit
            // - if it's a window that's owned by the toolstrip dropdown dont exit
 
            else if (e.CloseReason == ToolStripDropDownCloseReason.AppFocusChange || e.CloseReason == ToolStripDropDownCloseReason.AppClicked) {
 
                IntPtr hwndActivating = UnsafeNativeMethods.GetActiveWindow(); 
                if (this.Handle == hwndActivating && e.CloseReason == ToolStripDropDownCloseReason.AppClicked) {
                    e.Cancel = false; 
                    Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionToolStripDropDown.OnClosing] activation hasnt changed, but we've certainly clicked somewhere else.");
                }
                else if(WindowOwnsWindow(this.Handle, hwndActivating)) {
                   // we're being de-activated for someone owned by the panel 
                   e.Cancel = true;
                   Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionToolStripDropDown.OnClosing] Cancel close - the window activating is owned by this window"); 
                } 
                else if(_mainParentWindow != null && !WindowOwnsWindow(_mainParentWindow.Handle, hwndActivating)) {
                    if (IsWindowEnabled(_mainParentWindow.Handle)) { 
                       // the activated windows is not a child/owned windows of the main top level windows
                       // let toolstripdropdown handle this
                       e.Cancel = false;
                       Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionToolStripDropDown.OnClosing] Call close: the activated windows is not a child/owned windows of the main top level windows "); 
                    }
                    else { 
                        e.Cancel = true; 
                        Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionToolStripDropDown.OnClosing] we're being deactivated by a foreign window, but the main window is not enabled - we should stay up");
                    } 

                    base.OnClosing(e);
                    Debug.Unindent();
                    Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "_____________________________End OnClose e.Cancel: " + e.Cancel.ToString() ); 
                    return;
                } 
                else { 
                   Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionToolStripDropDown.OnClosing] since the designer action panel dropdown doesnt own the activating window " + hwndActivating.ToString("x") + ", calling close. ");
                } 


                // what's the owner of the windows being activated?
                IntPtr parent = UnsafeNativeMethods.GetWindowLong(new HandleRef(this, hwndActivating), 
                                                  NativeMethods.GWL_HWNDPARENT);
                // is it currently disabled (ie, the activating windows is in modal mode) 
                if(!IsWindowEnabled(parent)) { 
                   Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionToolStripDropDown.OnClosing] modal window activated - cancelling close");
                   // we are in a modal case 
                   e.Cancel = true;
                }
              } else {
           } 
           Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionToolStripDropDown.OnClosing] calling base.OnClosing with e.Cancel: " + e.Cancel.ToString());
 
           base.OnClosing(e); 
           Debug.Unindent();
           Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "_____________________________End OnClose e.Cancel: " + e.Cancel.ToString()); 

        }

        public void SetDesignerActionPanel(DesignerActionPanel panel, Glyph relatedGlyph) { 
            if(_panel != null && panel == (DesignerActionPanel)_panel.Control)
                return; 
 
            Debug.Assert(relatedGlyph != null, "related glyph cannot be null");
 
            this.relatedGlyph = relatedGlyph;

            panel.SizeChanged += new EventHandler(PanelResized);
            // hook up the event 
            if( _panel != null) {
                Items.Remove(_panel); 
                _panel.Dispose(); 
                _panel = null;
            } 
            _panel = new ToolStripControlHost(panel);
            // we don't want no margin
            _panel.Margin = Padding.Empty;
            _panel.Size = panel.Size; 

            this.SuspendLayout(); 
            this.Size = panel.Size; 
            this.Items.Add(_panel);
            this.ResumeLayout(); 

            if(this.Visible) {
                CheckFocusIsRight();
            } 

        } 
 
        private void PanelResized(object sender, System.EventArgs e) {
            Control ctrl = sender as Control; 
            if(this.Size.Width != ctrl.Size.Width || this.Size.Height != ctrl.Size.Height) {
                this.SuspendLayout();
                this.Size = ctrl.Size;
                if(_panel != null) { 
                    _panel.Size = ctrl.Size;
                } 
                _designerActionUI.UpdateDAPLocation(null, relatedGlyph as DesignerActionGlyph); 
                this.ResumeLayout();
            } 
        }


        protected override void SetVisibleCore(bool visible) { 
            Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionToolStripDropDown.SetVisibleCore] setting dropdown visible=" + visible.ToString());
            base.SetVisibleCore(visible); 
            if(visible) { 
                CheckFocusIsRight();
            } 
        }

        /// <devdoc>
        ///    General purpose method, based on Control.Contains()... 
        ///
        ///    Determines whether a given window (specified using native window handle) 
        ///    is a descendant of this control. This catches both contained descendants 
        ///    and 'owned' windows such as modal dialogs. Using window handles rather
        ///    than Control objects allows it to catch un-managed windows as well. 
        /// </devdoc>
        private static bool WindowOwnsWindow(IntPtr hWndOwner, IntPtr hWndDescendant) {
            Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[WindowOwnsWindow] Testing if " + hWndOwner.ToString("x")+ " is a owned by " + hWndDescendant.ToString("x") + "... ");
#if DEBUG 
            if (DesignerActionUI.DropDownVisibilityDebug.TraceVerbose) {
                Debug.WriteLine("\t\tOWNER: " + GetControlInformation(hWndOwner)); 
                Debug.WriteLine("\t\tOWNEE: " + GetControlInformation(hWndDescendant)); 

                IntPtr claimedOwnerHwnd = UnsafeNativeMethods.GetWindowLong(new HandleRef(null, hWndDescendant), NativeMethods.GWL_HWNDPARENT); 
                Debug.WriteLine("OWNEE's CLAIMED OWNER: "+ GetControlInformation(claimedOwnerHwnd));
            }

#endif 
            if (hWndDescendant == hWndOwner) {
                Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "they match, YES."); 
                return true; 
            }
 
            while (hWndDescendant != IntPtr.Zero) {
                hWndDescendant = UnsafeNativeMethods.GetWindowLong(new HandleRef(null, hWndDescendant), NativeMethods.GWL_HWNDPARENT);
                if (hWndDescendant == IntPtr.Zero) {
                    Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "NOPE."); 
                    return false;
                } 
                if (hWndDescendant == hWndOwner) { 
                    Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "YES.");
                    return true; 
                }
            }

            Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "NO."); 
            return false;
        } 
 

 

        // helper function for generating infomation about a particular control
        // use AssertControlInformation if sticking in an assert - then the work
        // to figure out the control info will only be done when the assertion is false. 
        internal static string GetControlInformation(IntPtr hwnd) {
             if (hwnd == IntPtr.Zero) { 
                return "Handle is IntPtr.Zero"; 
             }
#if DEBUG 
 	     if (!DesignerActionUI.DropDownVisibilityDebug.TraceVerbose) {
                return String.Empty;
             }
 
             int textLen = SafeNativeMethods.GetWindowTextLength(new HandleRef(null, hwnd));
             StringBuilder sb = new StringBuilder(textLen+1); 
             UnsafeNativeMethods.GetWindowText(new HandleRef(null, hwnd), sb, sb.Capacity); 

             string typeOfControl = "Unknown"; 
             string nameOfControl = "";
             Control c = Control.FromHandle(hwnd);
             if (c != null) {
                typeOfControl = c.GetType().Name; 
                if (!string.IsNullOrEmpty(c.Name)) {
                    nameOfControl += c.Name; 
                } 
                else {
                    nameOfControl += "Unknown"; 

                    ToolStripDropDown dd = c as ToolStripDropDown;
                    // some extra debug info for toolstripdropdowns...
                    if (dd != null) { 

                        if (dd.OwnerItem != null) { 
                            nameOfControl += "OwnerItem: [" + dd.OwnerItem.ToString()+ "]"; 
                        }
                    } 
                }
             }

             return sb.ToString() + "\r\n\t\t\tType: [" + typeOfControl + "] Name: [" + nameOfControl + "]"; 
#else
	     return String.Empty; 
#endif 

        } 
        private bool IsWindowEnabled(IntPtr handle) {
            int style = (int) UnsafeNativeMethods.GetWindowLong(new HandleRef(this, handle), NativeMethods.GWL_STYLE);
            return (style & NativeMethods.WS_DISABLED) == 0;
        } 

        private void WmActivate(ref Message m) { 
 
            if((int)m.WParam == NativeMethods.WA_INACTIVE) {
                IntPtr hwndActivating = m.LParam; 
                if(WindowOwnsWindow(this.Handle, hwndActivating)) {
                   Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionUI WmActivate] setting cancel close true because WindowsOwnWindow");

                   Debug.WriteLineIf(DesignerActionUI.DropDownVisibilityDebug.TraceVerbose, "[DesignerActionUI WmActivate] checking the focus... " + GetControlInformation(UnsafeNativeMethods.GetFocus())); 

                   _cancelClose = true; 
                } 
                else {
                    _cancelClose = false; 
                }
            }
            else {
                _cancelClose = false; 
            }
 
            base.WndProc(ref m); 
        }
 
        protected override void WndProc(ref Message m) {
            switch (m.Msg) {
                case NativeMethods.WM_ACTIVATE:
                    WmActivate(ref m); 
                    return;
            } 
            base.WndProc(ref m); 

        } 

        protected override bool ProcessDialogKey(Keys keyData) {
            // since we're not hosted in a form we need to do the same logic
            // as Form.cs. If we get an enter key we need to find the current focused control 
            // if it's a button, we click it and return that we handled the message
            if(keyData == Keys.Enter) { 
                IntPtr focusedControlPtr = UnsafeNativeMethods.GetFocus(); 
                Control focusedControl = Control.FromChildHandle(focusedControlPtr);
                IButtonControl button = focusedControl as IButtonControl; 
                if (button != null && button is Control) {
                    button.PerformClick();
                    return true;
                } 
            }
            /* should not need that anymore... *//* 
            if (   keyData == (Keys.Menu | Keys.Alt) || 
                    keyData == Keys.F4 ||
                    keyData == (Keys.Alt | Keys.Down) || 
                    keyData == (Keys.Alt | Keys.Up)) { //HACK HACK HACK  DesignerActionPanel should handle message routing properly
            // I don't think that's the case now. checking this in to get the suite to pass. Here we prevent VS from getting the F4
                IntPtr focusedControlPtr = UnsafeNativeMethods.GetFocus();
                if(WindowOwnsWindow(this.Handle, focusedControlPtr)) { 
                    // we don't want VS to even get the message, but we want to
                    // make sure it'll cause an OnKeyDown on the panel (who has focus) 
                    return false; 
                }
            }*/ 
            return base.ProcessDialogKey(keyData);
        }
    }
 

 
    internal class NoBorderRenderer : ToolStripProfessionalRenderer { 
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) {
        } 
    }
}

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
