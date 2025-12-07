//#define DEBUGDESIGNERTASKS 
//------------------------------------------------------------------------------
// <copyright file="DesignerActionService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.ComponentModel.Design { 

    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.Timers;
    using System.Diagnostics; 

    /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService"]/*' /> 
    /// <devdoc> 
    ///     The DesignerActionService manages DesignerActions.  All DesignerActions are
    ///     associated with an object. DesignerActions can be added or removed at any 
    ///     given time.
    ///
    ///     The DesignerActionService controls the expiration of DesignerActions by monitoring
    ///     three basic events: selection change, component change, and timer expiration. 
    ///
    ///     Designer implementing this service will need to monitor the 
    ///     DesignerActionsChanged event on this class.  This event will fire every 
    ///     time a change is made to any object's DesignerActions.
    /// </devdoc> 
    public class DesignerActionService : IDisposable {

        private Hashtable                                   designerActionLists;//this is how we store 'em.  Syntax: key = object, value = DesignerActionListCollection
        private DesignerActionListsChangedEventHandler      designerActionListsChanged; 
        private IServiceProvider                            serviceProvider;//standard service provider
        private ISelectionService                           selSvc; // selection service 
        private Hashtable                                   componentToVerbsEventHookedUp; //table component true/false 
        // Gaurd against ReEntrant Code.
        // Please refer VSWhidbey 417484. The Infragistics TabControlDesigner, Sets the Commands Status when the Verbs property is accesssed. 
        // This property is used in the OnVerbStatusChanged code here and hence causes recursion leading to Stack Overflow Exception.
        private bool                                        reEntrantCode = false;

 
        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.DesignerActionService"]/*' />
        /// <devdoc> 
        ///     Standard constructor.  A Service Provider is necessary for monitoring 
        ///     selection and component changes.
        /// </devdoc> 
        public DesignerActionService(IServiceProvider serviceProvider) {

            if (serviceProvider != null) {
                this.serviceProvider = serviceProvider; 

                IDesignerHost host = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
                host.AddService(typeof(DesignerActionService), this); 

                IComponentChangeService cs = (IComponentChangeService)serviceProvider.GetService(typeof(IComponentChangeService)); 
                if (cs != null) {
                    cs.ComponentRemoved += new ComponentEventHandler(this.OnComponentRemoved);
                }
                selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService)); 
                if(selSvc == null) {
                    Debug.Fail("Either BehaviorService or ISelectionService is null, cannot continue."); 
                } 
            }
 
            designerActionLists = new Hashtable();
            componentToVerbsEventHookedUp = new Hashtable();

            #if DEBUGDESIGNERTASKS 
                Debug.WriteLine("DesignerActionService - created");
            #endif 
        } 

 
        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.DesignerActionsChanged"]/*' />
        /// <devdoc>
        ///     This event is thrown whenever a DesignerActionList is removed
        ///     or added for any object. 
        /// </devdoc>
        public event DesignerActionListsChangedEventHandler DesignerActionListsChanged { 
            add { 
                designerActionListsChanged += value;
            } 
            remove {
                designerActionListsChanged -= value;
            }
        } 

        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Add"]/*' /> 
        /// <devdoc> 
        ///     Adds a new collection of DesignerActions to be monitored
        ///     with the related comp object. 
        /// </devdoc>
        public void Add(IComponent comp, DesignerActionListCollection designerActionListCollection) {
            if (comp == null) {
                throw new ArgumentNullException("comp"); 
            }
            if (designerActionListCollection== null) { 
                throw new ArgumentNullException("designerActionListCollection"); 
            }
 
            DesignerActionListCollection dhlc = (DesignerActionListCollection)designerActionLists[comp];
            if (dhlc != null) {
                dhlc.AddRange(designerActionListCollection);
            } 
            else {
                designerActionLists.Add(comp, designerActionListCollection); 
            } 

            //fire event 
            OnDesignerActionListsChanged(new DesignerActionListsChangedEventArgs(comp, DesignerActionListsChangedType.ActionListsAdded, GetComponentActions(comp)));
        }

        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Add2"]/*' /> 
        /// <devdoc>
        ///     Adds a new DesignerActionList to be monitored 
        ///     with the related comp object 
        /// </devdoc>
        public void Add(IComponent comp, DesignerActionList actionList) { 
            Add(comp, new DesignerActionListCollection(actionList));
        }

 
        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Clear"]/*' />
        /// <devdoc> 
        ///     Clears all objects and DesignerActions from the DesignerActionService. 
        /// </devdoc>
        public void Clear() { 

            if (designerActionLists.Count == 0) {
                return;
            } 

            //this will represent the list of componets we just cleared 
            ArrayList compsRemoved = new ArrayList(designerActionLists.Count); 

            foreach (DictionaryEntry entry in designerActionLists) { 
                 compsRemoved.Add(entry.Key);
            }

            //actually clear our hashtable 
            designerActionLists.Clear();
 
            //fire our DesignerActionsChanged event for each comp we just removed 
            foreach (Component comp in compsRemoved) {
                OnDesignerActionListsChanged(new DesignerActionListsChangedEventArgs(comp, DesignerActionListsChangedType.ActionListsRemoved, GetComponentActions(comp))); 
            }

        }
 
        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Contains"]/*' />
        /// <devdoc> 
        ///     Returns true if the DesignerActionService is currently 
        ///     managing the comp object.
        /// </devdoc> 
        public bool Contains(IComponent comp) {
            if (comp == null) {
                throw new ArgumentNullException("comp");
            } 
            return designerActionLists.Contains(comp);
        } 
 
        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Dispose"]/*' />
        /// <devdoc> 
        ///     Disposes all resources and unhooks all events.
        /// </devdoc>
        public void Dispose() {
            Dispose(true); 
        }
 
        protected virtual void Dispose(bool disposing) { 
            if (disposing && serviceProvider != null) {
                IDesignerHost host = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
                if (host != null) {
                    host.RemoveService(typeof(DesignerActionService));
                }
 
                IComponentChangeService cs = (IComponentChangeService)serviceProvider.GetService(typeof(IComponentChangeService));
                if (cs != null) { 
                    cs.ComponentRemoved -= new ComponentEventHandler(this.OnComponentRemoved); 
                }
            } 
        }


        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.GetComponentActions"]/*' /> 
        /// <devdoc>
        /// 
        /// </devdoc> 
        public DesignerActionListCollection GetComponentActions(IComponent component) {
            return GetComponentActions(component, ComponentActionsType.All); 
        }


        public virtual DesignerActionListCollection GetComponentActions(IComponent component, ComponentActionsType type) { 
            if (component == null) {
                throw new ArgumentNullException("component"); 
            } 
            DesignerActionListCollection result = new DesignerActionListCollection();
            switch (type) { 
                case ComponentActionsType.All:
                    GetComponentDesignerActions(component, result);
                    GetComponentServiceActions(component, result);
                    break; 
                case ComponentActionsType.Component:
                    GetComponentDesignerActions(component, result); 
                    break; 
                case ComponentActionsType.Service:
                    GetComponentServiceActions(component, result); 
                    break;

            }
            return result; 
        }
 
 

 
        protected virtual void GetComponentDesignerActions(IComponent component, DesignerActionListCollection actionLists) {
            if(component == null) {
                throw new ArgumentNullException("component");
            } 

            if(actionLists== null) { 
                throw new ArgumentNullException("actionLists"); 
            }
 
            IServiceContainer sc = component.Site as IServiceContainer;
            if (sc != null) {
                DesignerCommandSet dcs = (DesignerCommandSet)sc.GetService(typeof(DesignerCommandSet));
                if(dcs != null) { 
                    DesignerActionListCollection pullCollection = dcs.ActionLists;
                    if(pullCollection != null) { 
                        actionLists.AddRange(pullCollection); 
                    }
 
                    // if we don't find any, add the verbs for this component there...
                    if(actionLists.Count == 0) {
                        DesignerVerbCollection verbs = dcs.Verbs;
                        if(verbs != null && verbs.Count != 0) { 
                            ArrayList verbsArray = new ArrayList();
                            bool hookupEvents = componentToVerbsEventHookedUp[component] == null; 
                            if(hookupEvents) { 
                                componentToVerbsEventHookedUp[component] = true;
                            } 
                            foreach(DesignerVerb verb in verbs) {
                                if(hookupEvents) {
                                    //Debug.WriteLine("hooking up change event for verb " + verb.Text);
                                    verb.CommandChanged += new EventHandler(OnVerbStatusChanged); 
                                }
                                if(verb.Enabled && verb.Visible) { 
                                    //Debug.WriteLine("adding verb to collection for panel... " + verb.Text); 
                                    verbsArray.Add(verb);
                                } 
                            }
                            if(verbsArray.Count != 0) {
                                DesignerActionVerbList davl = new DesignerActionVerbList((DesignerVerb[])verbsArray.ToArray(typeof(DesignerVerb)));
                                actionLists.Add(davl); 
                            }
                        } 
                    } 

                    // remove all the ones that are empty... ie GetSortedActionList returns nothing 
                    // we might waste some time doing this twice but don't have much of a choice here... the panel is not yet displayed
                    // and we want to know if a non empty panel is present...
                    // NOTE: We do this AFTER the verb check that way to disable auto verb upgrading you can just return an empty
                    // actionlist collection 
                    if(pullCollection != null) {
                        foreach(DesignerActionList actionList in pullCollection) { 
                            DesignerActionItemCollection collection = actionList.GetSortedActionItems(); 
                            if(collection == null || collection.Count == 0) {
                                actionLists.Remove(actionList); 
                            }
                        }
                    }
                } 
            }
        } 
 
        private void OnVerbStatusChanged(object sender, EventArgs args) {
 
            if (!reEntrantCode)
            {
                try
                { 
                    reEntrantCode = true;
                    IComponent comp = selSvc.PrimarySelection as IComponent; 
                    if(comp != null) { 
                        IServiceContainer sc = comp.Site as IServiceContainer;
                        if(sc!=null) { 
                            DesignerCommandSet dcs = (DesignerCommandSet)sc.GetService(typeof(DesignerCommandSet));
                            foreach(DesignerVerb verb in dcs.Verbs) {
                                if(verb == sender) {
                                    DesignerActionUIService dapUISvc = (DesignerActionUIService)sc.GetService(typeof(DesignerActionUIService)); 
                                    if(dapUISvc != null) {
                                        //Debug.WriteLine("Calling refresh on component  " + comp.Site.Name); 
                                        dapUISvc.Refresh(comp); // we need to refresh, a verb on the current panel has changed its state 
                                    }
                                } 
                            }
                        }
                    }
                } 
                finally
                { 
                    reEntrantCode = false; 
                }
            } 
        }

        protected virtual void GetComponentServiceActions(IComponent component, DesignerActionListCollection actionLists) {
            if(component == null) { 
                throw new ArgumentNullException("component");
            } 
 
            if(actionLists== null) {
                throw new ArgumentNullException("actionLists"); 
            }

            DesignerActionListCollection pushCollection = (DesignerActionListCollection)designerActionLists[component];
            if(pushCollection != null) { 
                actionLists.AddRange(pushCollection);
                // remove all the ones that are empty... ie GetSortedActionList returns nothing 
                // we might waste some time doing this twice but don't have much of a choice here... the panel is not yet displayed 
                // and we want to know if a non empty panel is present...
                foreach(DesignerActionList actionList in pushCollection) { 
                    DesignerActionItemCollection collection = actionList.GetSortedActionItems();
                    if(collection == null || collection.Count == 0) {
                        actionLists.Remove(actionList);
                    } 
                }
            } 
        } 

 



        /// <devdoc> 
        ///     We hook the OnComponentRemoved event so we can clean up
        ///     all associated actions. 
        /// </devdoc> 
        private void OnComponentRemoved(object source, ComponentEventArgs ce) {
            Remove(ce.Component); 
        }

        /// <devdoc>
        ///     This fires our DesignerActionsChanged event. 
        /// </devdoc>
        private void OnDesignerActionListsChanged(DesignerActionListsChangedEventArgs e) { 
            if (designerActionListsChanged != null) { 
                designerActionListsChanged(this, e);
            } 
        }


 

 
        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Remove"]/*' /> 
        /// <devdoc>
        ///     This will remove all DesignerActions associated with 
        ///     the 'comp' object.  All alarms will be unhooked and
        ///     the DesignerActionsChagned event will be fired.
        /// </devdoc>
        public void Remove(IComponent comp) { 
            if (comp == null) {
                throw new ArgumentNullException("comp"); 
            } 

            if (!designerActionLists.Contains(comp)) { 
                return;
            }

            designerActionLists.Remove(comp); 

            OnDesignerActionListsChanged(new DesignerActionListsChangedEventArgs(comp, DesignerActionListsChangedType.ActionListsRemoved, GetComponentActions(comp))); 
        } 

        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Remove2"]/*' /> 
        /// <devdoc>
        ///     This will remove the specified Designeraction from
        ///     the DesignerActionService.  All alarms will be unhooked and
        ///     the DesignerActionsChagned event will be fired. 
        /// </devdoc>
        public void Remove(DesignerActionList actionList) { 
 
            if (actionList == null) {
                throw new ArgumentNullException("actionList"); 
            }

            //find the associated component
            foreach (IComponent comp in designerActionLists.Keys) { 
                if (((DesignerActionListCollection)designerActionLists[comp]).Contains(actionList)) {
                    Remove(comp, actionList); 
                    break; 
                }
            } 
        }

        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Remove3"]/*' />
        /// <devdoc> 
        ///     This will remove the all instances of the DesignerAction from
        ///     the 'comp' object.  If an alarm was set, it will be 
        ///     unhooked.  This will also fire the DesignerActionChanged 
        ///     event.
        /// </devdoc> 
        public void Remove(IComponent comp, DesignerActionList actionList) {
            if (comp == null) {
                throw new ArgumentNullException("comp");
            } 
            if (actionList == null) {
                throw new ArgumentNullException("actionList"); 
            } 
            if (!designerActionLists.Contains(comp)) {
                return; 
            }

            DesignerActionListCollection actionLists = (DesignerActionListCollection)designerActionLists[comp];
 
            if (!actionLists.Contains(actionList)) {
                return; 
            } 

            if (actionLists.Count == 1) { 
                //this is the last action for this object, remove the entire thing
                Remove(comp);
            }
            else { 
                //remove each instance of this action
                ArrayList actionListsToRemove = new ArrayList(1); 
                foreach (DesignerActionList t in actionLists) { 
                    if (actionList.Equals(t)) {
                        //found one to remove 
                        actionListsToRemove.Add(t);
                    }
                }
 
                foreach (DesignerActionList t in actionListsToRemove) {
                    actionLists.Remove(t); 
                } 

                OnDesignerActionListsChanged(new DesignerActionListsChangedEventArgs(comp, DesignerActionListsChangedType.ActionListsRemoved, GetComponentActions(comp))); 
            }
        }

        internal event DesignerActionUIStateChangeEventHandler DesignerActionUIStateChange { 
            add {
                DesignerActionUIService dapUISvc = (DesignerActionUIService)serviceProvider.GetService(typeof(DesignerActionUIService)); 
                if(dapUISvc != null) { 
                    dapUISvc.DesignerActionUIStateChange += value;
                } 
            }
            remove {
                DesignerActionUIService dapUISvc = (DesignerActionUIService)serviceProvider.GetService(typeof(DesignerActionUIService));
                if(dapUISvc != null) { 
                    dapUISvc.DesignerActionUIStateChange -= value;
                } 
            } 
        }
 
    }
    public enum ComponentActionsType {
        All,
        Component, 
        Service
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//#define DEBUGDESIGNERTASKS 
//------------------------------------------------------------------------------
// <copyright file="DesignerActionService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.ComponentModel.Design { 

    using System; 
    using System.Collections;
    using System.ComponentModel;
    using System.Timers;
    using System.Diagnostics; 

    /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService"]/*' /> 
    /// <devdoc> 
    ///     The DesignerActionService manages DesignerActions.  All DesignerActions are
    ///     associated with an object. DesignerActions can be added or removed at any 
    ///     given time.
    ///
    ///     The DesignerActionService controls the expiration of DesignerActions by monitoring
    ///     three basic events: selection change, component change, and timer expiration. 
    ///
    ///     Designer implementing this service will need to monitor the 
    ///     DesignerActionsChanged event on this class.  This event will fire every 
    ///     time a change is made to any object's DesignerActions.
    /// </devdoc> 
    public class DesignerActionService : IDisposable {

        private Hashtable                                   designerActionLists;//this is how we store 'em.  Syntax: key = object, value = DesignerActionListCollection
        private DesignerActionListsChangedEventHandler      designerActionListsChanged; 
        private IServiceProvider                            serviceProvider;//standard service provider
        private ISelectionService                           selSvc; // selection service 
        private Hashtable                                   componentToVerbsEventHookedUp; //table component true/false 
        // Gaurd against ReEntrant Code.
        // Please refer VSWhidbey 417484. The Infragistics TabControlDesigner, Sets the Commands Status when the Verbs property is accesssed. 
        // This property is used in the OnVerbStatusChanged code here and hence causes recursion leading to Stack Overflow Exception.
        private bool                                        reEntrantCode = false;

 
        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.DesignerActionService"]/*' />
        /// <devdoc> 
        ///     Standard constructor.  A Service Provider is necessary for monitoring 
        ///     selection and component changes.
        /// </devdoc> 
        public DesignerActionService(IServiceProvider serviceProvider) {

            if (serviceProvider != null) {
                this.serviceProvider = serviceProvider; 

                IDesignerHost host = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
                host.AddService(typeof(DesignerActionService), this); 

                IComponentChangeService cs = (IComponentChangeService)serviceProvider.GetService(typeof(IComponentChangeService)); 
                if (cs != null) {
                    cs.ComponentRemoved += new ComponentEventHandler(this.OnComponentRemoved);
                }
                selSvc = (ISelectionService)serviceProvider.GetService(typeof(ISelectionService)); 
                if(selSvc == null) {
                    Debug.Fail("Either BehaviorService or ISelectionService is null, cannot continue."); 
                } 
            }
 
            designerActionLists = new Hashtable();
            componentToVerbsEventHookedUp = new Hashtable();

            #if DEBUGDESIGNERTASKS 
                Debug.WriteLine("DesignerActionService - created");
            #endif 
        } 

 
        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.DesignerActionsChanged"]/*' />
        /// <devdoc>
        ///     This event is thrown whenever a DesignerActionList is removed
        ///     or added for any object. 
        /// </devdoc>
        public event DesignerActionListsChangedEventHandler DesignerActionListsChanged { 
            add { 
                designerActionListsChanged += value;
            } 
            remove {
                designerActionListsChanged -= value;
            }
        } 

        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Add"]/*' /> 
        /// <devdoc> 
        ///     Adds a new collection of DesignerActions to be monitored
        ///     with the related comp object. 
        /// </devdoc>
        public void Add(IComponent comp, DesignerActionListCollection designerActionListCollection) {
            if (comp == null) {
                throw new ArgumentNullException("comp"); 
            }
            if (designerActionListCollection== null) { 
                throw new ArgumentNullException("designerActionListCollection"); 
            }
 
            DesignerActionListCollection dhlc = (DesignerActionListCollection)designerActionLists[comp];
            if (dhlc != null) {
                dhlc.AddRange(designerActionListCollection);
            } 
            else {
                designerActionLists.Add(comp, designerActionListCollection); 
            } 

            //fire event 
            OnDesignerActionListsChanged(new DesignerActionListsChangedEventArgs(comp, DesignerActionListsChangedType.ActionListsAdded, GetComponentActions(comp)));
        }

        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Add2"]/*' /> 
        /// <devdoc>
        ///     Adds a new DesignerActionList to be monitored 
        ///     with the related comp object 
        /// </devdoc>
        public void Add(IComponent comp, DesignerActionList actionList) { 
            Add(comp, new DesignerActionListCollection(actionList));
        }

 
        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Clear"]/*' />
        /// <devdoc> 
        ///     Clears all objects and DesignerActions from the DesignerActionService. 
        /// </devdoc>
        public void Clear() { 

            if (designerActionLists.Count == 0) {
                return;
            } 

            //this will represent the list of componets we just cleared 
            ArrayList compsRemoved = new ArrayList(designerActionLists.Count); 

            foreach (DictionaryEntry entry in designerActionLists) { 
                 compsRemoved.Add(entry.Key);
            }

            //actually clear our hashtable 
            designerActionLists.Clear();
 
            //fire our DesignerActionsChanged event for each comp we just removed 
            foreach (Component comp in compsRemoved) {
                OnDesignerActionListsChanged(new DesignerActionListsChangedEventArgs(comp, DesignerActionListsChangedType.ActionListsRemoved, GetComponentActions(comp))); 
            }

        }
 
        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Contains"]/*' />
        /// <devdoc> 
        ///     Returns true if the DesignerActionService is currently 
        ///     managing the comp object.
        /// </devdoc> 
        public bool Contains(IComponent comp) {
            if (comp == null) {
                throw new ArgumentNullException("comp");
            } 
            return designerActionLists.Contains(comp);
        } 
 
        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Dispose"]/*' />
        /// <devdoc> 
        ///     Disposes all resources and unhooks all events.
        /// </devdoc>
        public void Dispose() {
            Dispose(true); 
        }
 
        protected virtual void Dispose(bool disposing) { 
            if (disposing && serviceProvider != null) {
                IDesignerHost host = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost)); 
                if (host != null) {
                    host.RemoveService(typeof(DesignerActionService));
                }
 
                IComponentChangeService cs = (IComponentChangeService)serviceProvider.GetService(typeof(IComponentChangeService));
                if (cs != null) { 
                    cs.ComponentRemoved -= new ComponentEventHandler(this.OnComponentRemoved); 
                }
            } 
        }


        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.GetComponentActions"]/*' /> 
        /// <devdoc>
        /// 
        /// </devdoc> 
        public DesignerActionListCollection GetComponentActions(IComponent component) {
            return GetComponentActions(component, ComponentActionsType.All); 
        }


        public virtual DesignerActionListCollection GetComponentActions(IComponent component, ComponentActionsType type) { 
            if (component == null) {
                throw new ArgumentNullException("component"); 
            } 
            DesignerActionListCollection result = new DesignerActionListCollection();
            switch (type) { 
                case ComponentActionsType.All:
                    GetComponentDesignerActions(component, result);
                    GetComponentServiceActions(component, result);
                    break; 
                case ComponentActionsType.Component:
                    GetComponentDesignerActions(component, result); 
                    break; 
                case ComponentActionsType.Service:
                    GetComponentServiceActions(component, result); 
                    break;

            }
            return result; 
        }
 
 

 
        protected virtual void GetComponentDesignerActions(IComponent component, DesignerActionListCollection actionLists) {
            if(component == null) {
                throw new ArgumentNullException("component");
            } 

            if(actionLists== null) { 
                throw new ArgumentNullException("actionLists"); 
            }
 
            IServiceContainer sc = component.Site as IServiceContainer;
            if (sc != null) {
                DesignerCommandSet dcs = (DesignerCommandSet)sc.GetService(typeof(DesignerCommandSet));
                if(dcs != null) { 
                    DesignerActionListCollection pullCollection = dcs.ActionLists;
                    if(pullCollection != null) { 
                        actionLists.AddRange(pullCollection); 
                    }
 
                    // if we don't find any, add the verbs for this component there...
                    if(actionLists.Count == 0) {
                        DesignerVerbCollection verbs = dcs.Verbs;
                        if(verbs != null && verbs.Count != 0) { 
                            ArrayList verbsArray = new ArrayList();
                            bool hookupEvents = componentToVerbsEventHookedUp[component] == null; 
                            if(hookupEvents) { 
                                componentToVerbsEventHookedUp[component] = true;
                            } 
                            foreach(DesignerVerb verb in verbs) {
                                if(hookupEvents) {
                                    //Debug.WriteLine("hooking up change event for verb " + verb.Text);
                                    verb.CommandChanged += new EventHandler(OnVerbStatusChanged); 
                                }
                                if(verb.Enabled && verb.Visible) { 
                                    //Debug.WriteLine("adding verb to collection for panel... " + verb.Text); 
                                    verbsArray.Add(verb);
                                } 
                            }
                            if(verbsArray.Count != 0) {
                                DesignerActionVerbList davl = new DesignerActionVerbList((DesignerVerb[])verbsArray.ToArray(typeof(DesignerVerb)));
                                actionLists.Add(davl); 
                            }
                        } 
                    } 

                    // remove all the ones that are empty... ie GetSortedActionList returns nothing 
                    // we might waste some time doing this twice but don't have much of a choice here... the panel is not yet displayed
                    // and we want to know if a non empty panel is present...
                    // NOTE: We do this AFTER the verb check that way to disable auto verb upgrading you can just return an empty
                    // actionlist collection 
                    if(pullCollection != null) {
                        foreach(DesignerActionList actionList in pullCollection) { 
                            DesignerActionItemCollection collection = actionList.GetSortedActionItems(); 
                            if(collection == null || collection.Count == 0) {
                                actionLists.Remove(actionList); 
                            }
                        }
                    }
                } 
            }
        } 
 
        private void OnVerbStatusChanged(object sender, EventArgs args) {
 
            if (!reEntrantCode)
            {
                try
                { 
                    reEntrantCode = true;
                    IComponent comp = selSvc.PrimarySelection as IComponent; 
                    if(comp != null) { 
                        IServiceContainer sc = comp.Site as IServiceContainer;
                        if(sc!=null) { 
                            DesignerCommandSet dcs = (DesignerCommandSet)sc.GetService(typeof(DesignerCommandSet));
                            foreach(DesignerVerb verb in dcs.Verbs) {
                                if(verb == sender) {
                                    DesignerActionUIService dapUISvc = (DesignerActionUIService)sc.GetService(typeof(DesignerActionUIService)); 
                                    if(dapUISvc != null) {
                                        //Debug.WriteLine("Calling refresh on component  " + comp.Site.Name); 
                                        dapUISvc.Refresh(comp); // we need to refresh, a verb on the current panel has changed its state 
                                    }
                                } 
                            }
                        }
                    }
                } 
                finally
                { 
                    reEntrantCode = false; 
                }
            } 
        }

        protected virtual void GetComponentServiceActions(IComponent component, DesignerActionListCollection actionLists) {
            if(component == null) { 
                throw new ArgumentNullException("component");
            } 
 
            if(actionLists== null) {
                throw new ArgumentNullException("actionLists"); 
            }

            DesignerActionListCollection pushCollection = (DesignerActionListCollection)designerActionLists[component];
            if(pushCollection != null) { 
                actionLists.AddRange(pushCollection);
                // remove all the ones that are empty... ie GetSortedActionList returns nothing 
                // we might waste some time doing this twice but don't have much of a choice here... the panel is not yet displayed 
                // and we want to know if a non empty panel is present...
                foreach(DesignerActionList actionList in pushCollection) { 
                    DesignerActionItemCollection collection = actionList.GetSortedActionItems();
                    if(collection == null || collection.Count == 0) {
                        actionLists.Remove(actionList);
                    } 
                }
            } 
        } 

 



        /// <devdoc> 
        ///     We hook the OnComponentRemoved event so we can clean up
        ///     all associated actions. 
        /// </devdoc> 
        private void OnComponentRemoved(object source, ComponentEventArgs ce) {
            Remove(ce.Component); 
        }

        /// <devdoc>
        ///     This fires our DesignerActionsChanged event. 
        /// </devdoc>
        private void OnDesignerActionListsChanged(DesignerActionListsChangedEventArgs e) { 
            if (designerActionListsChanged != null) { 
                designerActionListsChanged(this, e);
            } 
        }


 

 
        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Remove"]/*' /> 
        /// <devdoc>
        ///     This will remove all DesignerActions associated with 
        ///     the 'comp' object.  All alarms will be unhooked and
        ///     the DesignerActionsChagned event will be fired.
        /// </devdoc>
        public void Remove(IComponent comp) { 
            if (comp == null) {
                throw new ArgumentNullException("comp"); 
            } 

            if (!designerActionLists.Contains(comp)) { 
                return;
            }

            designerActionLists.Remove(comp); 

            OnDesignerActionListsChanged(new DesignerActionListsChangedEventArgs(comp, DesignerActionListsChangedType.ActionListsRemoved, GetComponentActions(comp))); 
        } 

        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Remove2"]/*' /> 
        /// <devdoc>
        ///     This will remove the specified Designeraction from
        ///     the DesignerActionService.  All alarms will be unhooked and
        ///     the DesignerActionsChagned event will be fired. 
        /// </devdoc>
        public void Remove(DesignerActionList actionList) { 
 
            if (actionList == null) {
                throw new ArgumentNullException("actionList"); 
            }

            //find the associated component
            foreach (IComponent comp in designerActionLists.Keys) { 
                if (((DesignerActionListCollection)designerActionLists[comp]).Contains(actionList)) {
                    Remove(comp, actionList); 
                    break; 
                }
            } 
        }

        /// <include file='doc\DesignerActionService.uex' path='docs/doc[@for="DesignerActionService.Remove3"]/*' />
        /// <devdoc> 
        ///     This will remove the all instances of the DesignerAction from
        ///     the 'comp' object.  If an alarm was set, it will be 
        ///     unhooked.  This will also fire the DesignerActionChanged 
        ///     event.
        /// </devdoc> 
        public void Remove(IComponent comp, DesignerActionList actionList) {
            if (comp == null) {
                throw new ArgumentNullException("comp");
            } 
            if (actionList == null) {
                throw new ArgumentNullException("actionList"); 
            } 
            if (!designerActionLists.Contains(comp)) {
                return; 
            }

            DesignerActionListCollection actionLists = (DesignerActionListCollection)designerActionLists[comp];
 
            if (!actionLists.Contains(actionList)) {
                return; 
            } 

            if (actionLists.Count == 1) { 
                //this is the last action for this object, remove the entire thing
                Remove(comp);
            }
            else { 
                //remove each instance of this action
                ArrayList actionListsToRemove = new ArrayList(1); 
                foreach (DesignerActionList t in actionLists) { 
                    if (actionList.Equals(t)) {
                        //found one to remove 
                        actionListsToRemove.Add(t);
                    }
                }
 
                foreach (DesignerActionList t in actionListsToRemove) {
                    actionLists.Remove(t); 
                } 

                OnDesignerActionListsChanged(new DesignerActionListsChangedEventArgs(comp, DesignerActionListsChangedType.ActionListsRemoved, GetComponentActions(comp))); 
            }
        }

        internal event DesignerActionUIStateChangeEventHandler DesignerActionUIStateChange { 
            add {
                DesignerActionUIService dapUISvc = (DesignerActionUIService)serviceProvider.GetService(typeof(DesignerActionUIService)); 
                if(dapUISvc != null) { 
                    dapUISvc.DesignerActionUIStateChange += value;
                } 
            }
            remove {
                DesignerActionUIService dapUISvc = (DesignerActionUIService)serviceProvider.GetService(typeof(DesignerActionUIService));
                if(dapUISvc != null) { 
                    dapUISvc.DesignerActionUIStateChange -= value;
                } 
            } 
        }
 
    }
    public enum ComponentActionsType {
        All,
        Component, 
        Service
    } 
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
