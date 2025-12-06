//------------------------------------------------------------------------------ 
// <copyright file="ToolboxService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Drawing.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Drawing; 
    using System.Globalization;
    using System.IO; 
    using System.Reflection; 
    using System.Runtime.Remoting.Lifetime;
    using System.Runtime.Serialization; 
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Security;
    using System.Security.Permissions;
    using System.Windows.Forms; 

    /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService"]/*' /> 
    /// <devdoc> 
    ///     This is a partial implementation of the IToolboxService
    ///     interface.  To use this implementation you must 
    ///     derive from this class and implement the abstract
    ///     methods.  Once implemented, you may add this class
    ///     to your designer application's service container.
    ///     There should be one toolbox service for each 
    ///     designer application.
    /// </devdoc> 
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")] 
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")]
    public abstract class ToolboxService : IToolboxService, IComponentDiscoveryService { 

        private IDesignerEventService   _designerEventService;

        private ArrayList       _globalCreators; 
        private Hashtable       _designerCreators;    // key: designer host, value: ArrayList of ToolboxItemCreators
 
        // A cache of the last merge we did to merge designer creators and global creators. 
        private IDesignerHost   _lastMergedHost;
        private ICollection     _lastMergedCreators; 

        // DesignerToolboxInfo stores filter and toolbox user information
        // on a per-designer basis.  This is the last one we queried.
        private DesignerToolboxInfo     _lastState; 

        // We maintain a separate app domain to enumerate assemblies without 
        // locking them. 
        //
        private static DomainProxyObject _domainObject; 
        private static AppDomain         _domain;
        private static ClientSponsor     _domainObjectSponsor;

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.CategoryNames"]/*' /> 
        /// <devdoc>
        ///     Retrieves a collection of category name strings. 
        ///     These category names correspond to various toolbox 
        ///     categories.
        /// </devdoc> 
        protected abstract CategoryNameCollection CategoryNames { get; }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.SelectedCategory"]/*' />
        /// <devdoc> 
        ///     Gets or sets the selected category for the toolbox.
        ///     Toolbox items are generally grouped into categories. 
        /// </devdoc> 
        protected abstract string SelectedCategory { get; set; }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.SelectedItemContainer"]/*' />
        /// <devdoc>
        ///     Gets or sets the selected toolbox item.
        /// </devdoc> 
        protected abstract ToolboxItemContainer SelectedItemContainer {get; set; }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.CreateItemContainer"]/*' /> 
        /// <devdoc>
        ///     Creates a new toolbox item container from a toolbox 
        ///     item.  This allows the implementor the chance to
        ///     provide a derived version of ToolboxItemContainer.
        ///     If the provided IDesignerHost link parameter is
        ///     non-null it indicates that this is a linked toolbox 
        ///     item.  By default, ToolboxService does not support
        ///     linked items so it will return null for non-null 
        ///     link parameters.  To provide link support, you should 
        ///     override this method to create a derived
        ///     ToolboxItemContainer object that knows how to handle 
        ///     links.  A "linked" toolbox item is one whose lifetime
        ///     is related to the storage of a particular designer
        ///     host.  So, in a typical project system, a designer
        ///     host is associated with a particular file.  A 
        ///     toobox item linked to a designer host would automatically
        ///     be deleted from the toolbox when the designer host's 
        ///     source file is deleted or removed from the project. 
        /// </devdoc>
        protected virtual ToolboxItemContainer CreateItemContainer(ToolboxItem item, IDesignerHost link) { 

            if (item == null) {
                throw new ArgumentNullException("item");
            } 

            // Default implementation does not support links. 
            if (link != null) { 
                return null;
            } 

            return new ToolboxItemContainer(item);
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.CreateItemContainer1"]/*' />
        /// <devdoc> 
        ///     Creates a new toolbox item container from a data object.  The 
        ///     data object passed in should contain data obtained from
        ///     a prior call to the ToolboxData property on a toolbox item 
        ///     container.
        /// </devdoc>
        protected virtual ToolboxItemContainer CreateItemContainer(IDataObject dataObject) {
 
            if (dataObject == null) {
                throw new ArgumentNullException("dataObject"); 
            } 

            return new ToolboxItemContainer(dataObject); 
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.FilterChanged"]/*' />
        /// <devdoc> 
        ///     This event is raised when the toolbox service detects that
        ///     the toolbox item filter for the actvie designer 
        ///     has changed. 
        /// </devdoc>
        protected virtual void FilterChanged() { 
        }

        /// <devdoc>
        ///     Returns an ICollection of toolbox item creators, or null if there 
        ///     is no active creator collection.  This will merge global creators
        ///     in with the provided host, if not null.  This also caches the 
        ///     last provided merged collection because merging takes time. 
        /// </devdoc>
        private ICollection GetCreatorCollection(IDesignerHost host) { 

            // If not provided a host, we just return the global
            // creator collection.
            // 
            if (host == null) {
                return _globalCreators; 
            } 

            // If we are provided a host, and that host matches the 
            // last request, we returned the last merged set.  Otherwise,
            //  we build a new merged set.
            //
            if (host != _lastMergedHost) { 

                ICollection creators = _globalCreators; 
                ICollection hostCreators = null; 

                if (_designerCreators != null) { 

                    hostCreators = _designerCreators[host] as ICollection;

                    if (hostCreators != null) { 

                        int cnt = hostCreators.Count; 
 
                        if (creators != null) {
                            cnt += creators.Count; 
                        }

                        ToolboxItemCreator[] newCreators = new ToolboxItemCreator[cnt];
 
                        hostCreators.CopyTo(newCreators, 0);
                        if (creators != null) { 
                            creators.CopyTo(newCreators, hostCreators.Count); 
                        }
                        creators = newCreators; 
                    }
                }

                _lastMergedCreators = creators; 
                _lastMergedHost = host;
            } 
            #if DEBUG 

            // For debug builds verify that our caching algorithm didn't miss. 
            //
            else if (_lastMergedCreators != null) {
                int debugCount = 0;
 
                if (_globalCreators != null) {
                    debugCount += _globalCreators.Count; 
                } 

                if (_designerCreators != null) { 
                    ICollection debugCreators = _designerCreators[host] as ICollection;
                    if (debugCreators != null) {
                        debugCount += debugCreators.Count;
                    } 
                }
 
                Debug.Assert(_lastMergedCreators.Count == debugCount, "ToolboxItemCreator cache algorithm is broken."); 
            }
            #endif 

            return _lastMergedCreators;
        }
 
        /// <devdoc>
        ///     Determines the type of filter support given two filter collections. 
        /// 
        ///     Truth Table:
        /// 
        ///                  Root Designer
        ///     Class        Mismatch   Allow         Prevent   Require       Custom
        ///     Mismatch     Y          Y             Y         N             Y
        ///     Allow        Y          Y             N         Y             IsSupported 
        ///     Prevent      Y          N             N         N             N
        ///     Require      N          Y             N         Y             IsSupported 
        ///     Custom       Y          IsSupported   N         IsSupported   IsSupported 
        ///
        ///     Legend: 
        ///
        ///     Y : The toolbox item will be enabled
        ///     N : The toolbox item will be disabled
        ///     IsSupported: The toolbox item will be enabled only if the method IToolboxUser.IsSupported returns true. 
        ///
        /// </devdoc> 
        private static FilterSupport GetFilterSupport(ICollection itemFilter, ICollection targetFilter) { 

            FilterSupport support = FilterSupport.Supported; 

            int requireCount = 0;
            int requireMatch = 0;
 
            // If Custom is specified on the designer, then we check to see if the
            // filter name matches an attribute, or if the filter name is empty. 
            // If either is the case, then we will invoke the designer for custom 
            // support.
            // 
            foreach(ToolboxItemFilterAttribute attr in itemFilter) {

                if (support == FilterSupport.NotSupported) {
                    break; 
                }
 
                if (attr.FilterType == ToolboxItemFilterType.Require) { 

                    // This filter is required.  Check that it exists.  Require filters 
                    // are or-matches.  If any one requirement is satisified, you're fine.
                    //
                    requireCount++;
 
                    foreach(object attrObject in targetFilter) {
                        ToolboxItemFilterAttribute attr2 = attrObject as ToolboxItemFilterAttribute; 
                        if (attr2 == null) { 
                            continue;
                        } 

                        if (attr.Match(attr2)) {
                            requireMatch++;
                            break; 
                        }
                    } 
                } 
                else if (attr.FilterType == ToolboxItemFilterType.Prevent) {
 
                    // This filter should be prevented.  Check that it fails.
                    //
                    foreach(object attrObject in targetFilter) {
                        ToolboxItemFilterAttribute attr2 = attrObject as ToolboxItemFilterAttribute; 
                        if (attr2 == null) {
                            continue; 
                        } 

                        if (attr.Match(attr2)) { 
                            support = FilterSupport.NotSupported;
                            break;
                        }
                    } 
                }
                else if (support != FilterSupport.Custom && attr.FilterType == ToolboxItemFilterType.Custom) { 
                    if (attr.FilterString.Length == 0) { 
                        support = FilterSupport.Custom;
                    } 
                    else {
                        foreach(ToolboxItemFilterAttribute attr2 in targetFilter) {
                            if (attr.FilterString.Equals(attr2.FilterString)) {
                                support = FilterSupport.Custom; 
                                break;
                            } 
                        } 
                    }
                } 
            }


            // Now, configure Supported based on matching require counts 
            //
            if (support != FilterSupport.NotSupported && requireCount > 0 && requireMatch == 0) { 
                support = FilterSupport.NotSupported; 
            }
 
            // Now, do the same thing for the designer side.  Identical check, but from
            // a different perspective.  We also check for the presence of a custom filter
            // here.
            // 
            if (support != FilterSupport.NotSupported) {
 
                requireCount = 0; 
                requireMatch = 0;
 
                foreach(ToolboxItemFilterAttribute attr in targetFilter) {

                    if (support == FilterSupport.NotSupported) {
                        break; 
                    }
 
                    if (attr.FilterType == ToolboxItemFilterType.Require) { 

                        // This filter is required.  Check that it exists.  Require filters 
                        // are or-matches.  If any one requirement is satisified, you're fine.
                        //
                        requireCount++;
 
                        foreach(ToolboxItemFilterAttribute attr2 in itemFilter) {
                            if (attr.Match(attr2)) { 
                                requireMatch++; 
                                break;
                            } 
                        }
                    }
                    else if (attr.FilterType == ToolboxItemFilterType.Prevent) {
 
                        // This filter should be prevented.  Check that it fails.
                        // 
                        foreach(ToolboxItemFilterAttribute attr2 in itemFilter) { 
                            if (attr.Match(attr2)) {
                                support = FilterSupport.NotSupported; 
                                break;
                            }
                        }
                    } 
                    else if (support != FilterSupport.Custom && attr.FilterType == ToolboxItemFilterType.Custom) {
                        if (attr.FilterString.Length == 0) { 
                            support = FilterSupport.Custom; 
                        }
                        else { 
                            foreach(ToolboxItemFilterAttribute attr2 in itemFilter) {
                                if (attr.FilterString.Equals(attr2.FilterString)) {
                                    support = FilterSupport.Custom;
                                    break; 
                                }
                            } 
                        } 
                    }
                } 

                // Now, configure Supported based on matching require counts
                //
                if (support != FilterSupport.NotSupported && requireCount > 0 && requireMatch == 0) { 
                    support = FilterSupport.NotSupported;
                } 
            } 

            return support; 
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.GetItemContainers"]/*' />
        /// <devdoc> 
        ///     Retrieves an IList containing all items on the toolbox.
        ///     The items in the list must be ToolboxItemContainer objects. 
        ///     If the toolbox implementation is organlized in 
        ///     categories, this retrieves a combined list of all
        ///     categories.  The list must be read-write.  New items 
        ///     will be created by calling CreateItem, and then passing
        ///     this newly created item to the Add method of the list.
        /// </devdoc>
        protected abstract IList GetItemContainers(); 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.GetItemContainers1"]/*' /> 
        /// <devdoc> 
        ///     Retrieves an IList containing items on the toolbox
        ///     associated with a particular category.  If the category does 
        ///     not exist this should throw a meaningful exception.
        ///     The items in the list must be ToolboxItemContainer objects.
        ///     The list must be read-write.  New items will be
        ///     created by calling CreateItem, and then passing 
        ///     this newly created item to the Add method of the list.
        /// </devdoc> 
        protected abstract IList GetItemContainers(string categoryName); 

 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.GetToolboxItem"]/*' />
        /// <devdoc>
        ///     Returns a toolbox item associated with the given type, or
        ///     null if the type has no corresponding toolbox item. 
        /// </devdoc>
        public static ToolboxItem GetToolboxItem(Type toolType) { 
            return GetToolboxItem(toolType, false); 
        }
 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.GetToolboxItem1"]/*' />
        /// <devdoc>
        ///     Returns a toolbox item associated with the given type, or 
        ///     null if the type has no corresponding toolbox item.  If nonPublic is true this will search
        ///     for non-public constructors on the type.  If false, constructurs need to be public. 
        /// </devdoc> 
        public static ToolboxItem GetToolboxItem(Type toolType, bool nonPublic) {
 
            ToolboxItem item = null;

            if (toolType == null) {
                throw new ArgumentNullException("toolType"); 
            }
 
            if (((nonPublic || toolType.IsPublic) || toolType.IsNestedPublic) && typeof(IComponent).IsAssignableFrom(toolType) && !toolType.IsAbstract) { 

                // Create a toolbox item for this type, if it is supported 
                //
                ToolboxItemAttribute tba = (ToolboxItemAttribute)TypeDescriptor.GetAttributes(toolType)[typeof(ToolboxItemAttribute)];

                if (!tba.IsDefaultAttribute()) { 
                    Type itemType = tba.ToolboxItemType;
 
                    if (itemType != null) { 

                        // First, try to find a constructor with Type as a parameter.  If that 
                        // fails, try the default constructor.
                        //
                        ConstructorInfo ctor = itemType.GetConstructor(new Type[] {typeof(Type)});
                        if (ctor != null && toolType != null) { 
                            item = (ToolboxItem)ctor.Invoke(new object[] {toolType});
                        } 
                        else { 
                            ctor = itemType.GetConstructor(new Type[0]);
                            if (ctor != null) { 
                                item = (ToolboxItem)ctor.Invoke(new object[0]);
                                item.Initialize(toolType);
                            }
                        } 
                    }
                } 
                else if (!tba.Equals(ToolboxItemAttribute.None) && !toolType.ContainsGenericParameters) { 
                    //the default toolboxitem class does not support generics, but we do not stop anyone from specifying thier own
                    //toolboxitem if they really want to. 
                    //however, most tools in VS will be filtering generics, so this would be an advanced scenario.
                    item = new ToolboxItem(toolType);
                }
            } 
            else if (typeof(ToolboxItem).IsAssignableFrom(toolType)) {
                // if the type *is* a toolboxitem, just create it.. 
                // 
                item = (ToolboxItem)Activator.CreateInstance(toolType, true);
            } 

            return item;
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.GetToolboxItems"]/*' />
        /// <devdoc> 
        ///     Returns a collection containing all the toolbox items in the 
        ///     given assembly.
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")] // Would be a breaking change.
        public static ICollection GetToolboxItems(Assembly a, string newCodeBase)
        {
            return GetToolboxItems(a, newCodeBase, false); 
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.GetToolboxItems"]/*' /> 
        /// <devdoc>
        ///     Returns a collection containing all the toolbox items in the 
        ///     given assembly.
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")] // Would be a breaking change.
        public static ICollection GetToolboxItems(Assembly a, string newCodeBase, bool throwOnError) 
        {
 
            if (a == null) { 
                throw new ArgumentNullException("a");
            } 

            ArrayList items = new ArrayList();

            // For GAC installed assemblies, we want to replace the current 
            // assembly name with the SDK path for the assembly, so we are not
            // relying on specific places within the GAC. 
            // 
            AssemblyName newAssemblyName;
 
            if (a.GlobalAssemblyCache) {
                newAssemblyName = a.GetName();
                newAssemblyName.CodeBase = newCodeBase;
            } 
            else {
                newAssemblyName = null; 
            } 

            try { 
                foreach(Type type in a.GetTypes()) {

                    // only do IComponent things from here...
                    // 
                    if (!typeof(IComponent).IsAssignableFrom(type)) {
                        continue; 
                    } 

                    // Look for compatible constructors 
                    //
                    ConstructorInfo ctor = type.GetConstructor(new Type[0]);
                    if (ctor == null) {
                        ctor = type.GetConstructor(new Type[] {typeof(IContainer)}); 
                    }
 
                    if (ctor == null) { 
                        continue;
                    } 

                    try {
                        ToolboxItem item = GetToolboxItem(type);
 
                        if (item != null) {
 
                            // Now that we have the item, we may need to replace the 
                            // assembly name.
                            // 
                            if (newAssemblyName != null) {
                                item.AssemblyName = newAssemblyName;
                            }
 
                            // Finally, this item needs to go in our list.
                            // 
                            items.Add(item); 
                        }
                    } 
 					catch
					{
                        if (throwOnError) {
                            throw; 
                        }
                        // Nothing here.  If a toolbox item failed we want to continue searching 
                        // the rest of the types. 
                    }
                } 
            }
            catch {
                if (throwOnError) {
                    throw; 
                }
                // Nothing here.  If an assembly is missing dependencies it could throw while 
                // we evaluate.  Eat it and move on. 
            }
 
            return items;
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.GetToolboxItems1"]/*' /> 
        /// <devdoc>
        ///     Returns a collection containing all the toolbox items in 
        ///     the assembly represented by the given assembly name.  This 
        ///     will only momentarially lock the assembly file.
        /// </devdoc> 
        public static ICollection GetToolboxItems(AssemblyName an) {
            return GetToolboxItems(an, false);
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.GetToolboxItems1"]/*' />
        /// <devdoc> 
        ///     Returns a collection containing all the toolbox items in 
        ///     the assembly represented by the given assembly name.  This
        ///     will only momentarially lock the assembly file. 
        /// </devdoc>
        public static ICollection GetToolboxItems(AssemblyName an, bool throwOnError) {

            if (_domainObject == null) { 
                _domain = AppDomain.CreateDomain("Assembly Enumeration Domain");
                _domainObject = (DomainProxyObject)_domain.CreateInstanceAndUnwrap(typeof(DomainProxyObject).Assembly.FullName, typeof(DomainProxyObject).FullName); 
                _domainObjectSponsor = new ClientSponsor(new TimeSpan(0 /* hours */, 5 /* minutes */, 0 /* seconds */)); 
                _domainObjectSponsor.Register(_domainObject);
            } 

            byte[] bytes = _domainObject.GetToolboxItems(an, throwOnError);

            BinaryFormatter f = new BinaryFormatter(); 
            ICollection items = (ICollection)f.Deserialize(new MemoryStream(bytes));
 
            return items; 

        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IsItemContainer"]/*' />
        /// <devdoc>
        ///     Called to perform a quick check to see if the given data object represents 
        ///     a toolbox item.  You may pass in an instance of a designer host if
        ///     you want to include custom toolbox item creators the host provides. 
        ///     Otherwise, this parameter can be null. 
        /// </devdoc>
        protected virtual bool IsItemContainer(IDataObject dataObject, IDesignerHost host) { 

            if (dataObject == null) {
                throw new ArgumentNullException("dataObject");
            } 

            if (ToolboxItemContainer.ContainsFormat(dataObject)) { 
                return true; 
            }
 
            ICollection creators = GetCreatorCollection(host);
            if (creators != null) {
                foreach(ToolboxItemCreator creator in creators) {
                    if (dataObject.GetDataPresent(creator.Format)) { 
                        return true;
                    } 
                } 
            }
 
            return false;
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IsItemContainerSupported"]/*' /> 
        /// <devdoc>
        ///     This is a helper method that can be used if a toolbox item container 
        ///     is supported by the given designer host. 
        /// </devdoc>
        protected bool IsItemContainerSupported(ToolboxItemContainer container, IDesignerHost host) { 

            if (container == null) {
                throw new ArgumentNullException("container");
            } 

            if (host == null) { 
                throw new ArgumentNullException("host"); 
            }
 
            ICollection creators = GetCreatorCollection(host);

            _lastState = host.GetService(typeof(DesignerToolboxInfo)) as DesignerToolboxInfo;
            if (_lastState == null) { 
                _lastState = new DesignerToolboxInfo(this, host);
                host.AddService(typeof(DesignerToolboxInfo), _lastState); 
            } 

            switch(GetFilterSupport(container.GetFilter(creators), _lastState.Filter)) { 
                case FilterSupport.NotSupported:
                    return false;
                case FilterSupport.Supported:
                    return true; 
                case FilterSupport.Custom:
                    if (_lastState.ToolboxUser != null) { 
                        return _lastState.ToolboxUser.GetToolSupported(container.GetToolboxItem(creators)); 
                    }
                    break; 
            }

            return false;
        } 

        /// <devdoc> 
        ///     Called by our DesignerToolboxInfo object to 
        ///     notify us when the external designer state
        ///     (filtering, etc) has changed. 
        /// </devdoc>
        internal void OnDesignerInfoChanged(DesignerToolboxInfo state) {

            // One of the toolbox item state objects changed.  If 
            // it is tied to the currently active designer then
            // we need to raise our FilterChanged event. 
            // 
            if (_designerEventService == null) {
                _designerEventService = state.DesignerHost.GetService(typeof(IDesignerEventService)) as IDesignerEventService; 
            }

            if (_designerEventService != null && _designerEventService.ActiveDesigner == state.DesignerHost) {
                FilterChanged(); 
            }
        } 
 
        /*
 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 
*/
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.Refresh"]/*' /> 
        /// <devdoc>
        ///     Called by the toolbox service when an outside ouser 
        ///     has requested that the collection of items should
        ///     be refreshed.  If the collection is always live,
        ///     there is no need to provide any implementation here.
        ///     If the collection returned from GetItemContainers 
        ///     represents a snapshot of the toolbox items, however,
        ///     this method provides a opportunity to update them. 
        /// </devdoc> 
        protected abstract void Refresh();
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.SelectedItemContainerUsed"]/*' />
        /// <devdoc>
        ///     Called by the toolbox service when an outside
        ///     user has reported that he/she has used the 
        ///     selected toolbox item.  The default behavior is
        ///     to set the selected item to null. 
        /// </devdoc> 
    	protected virtual void SelectedItemContainerUsed() {
            SelectedItemContainer = null; 
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.SetCursor"]/*' />
        /// <devdoc> 
        ///     Called by the toolbox service when an outside
        ///     user has asked to set the cursor for the currently 
        ///     selected toolbox item.  The default implementation 
        ///     sets the cursor to a crosshair and returns true
        ///     if there is a toolbox item selected.  It returns 
        ///     false if no item is selected.
        /// </devdoc>
        protected virtual bool SetCursor() {
            if (SelectedItemContainer != null) { 
                Cursor.Current = Cursors.Cross;
                return true; 
            } 

            return false; 
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.UnloadToolboxItems"]/*' />
        /// <devdoc> 
        ///     This unloads any assemblies that were locked
        ///     as a result of calling GetToolboxItems. 
        /// </devdoc> 
        public static void UnloadToolboxItems() {
 
            // We are now done with the domain, so release it.
            //
            if (_domain != null) {
                AppDomain deadDomain = _domain; 
                _domainObjectSponsor.Close();
                _domainObjectSponsor = null; 
                _domainObject = null; 
                _domain = null;
                AppDomain.Unload(deadDomain); 
            }
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.CategoryNames"]/*' /> 
        /// <devdoc>
        ///     Gets the names of all the tool categories currently on the toolbox. 
        /// </devdoc> 
        CategoryNameCollection IToolboxService.CategoryNames {
            get { 
                return CategoryNames;
            }
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.SelectedCategory"]/*' />
        /// <devdoc> 
        ///     Gets the name of the currently selected tool category from the toolbox. 
        /// </devdoc>
        string IToolboxService.SelectedCategory { 
            get {
                return SelectedCategory;
            }
            set { 
                SelectedCategory = value;
            } 
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.AddCreator"]/*' /> 
        /// <devdoc>
        ///     Adds a new toolbox item creator.
        /// </devdoc>
        void IToolboxService.AddCreator(ToolboxItemCreatorCallback creator, string format) { 

            if (creator == null) { 
                throw new ArgumentNullException("creator"); 
            }
 
            if (format == null) {
                throw new ArgumentNullException("format");
            }
 
            if (_globalCreators == null) {
                _globalCreators = new ArrayList(); 
            } 

            _globalCreators.Add(new ToolboxItemCreator(creator, format)); 

            // We now need to re-query because the list has changed.
            //
            _lastMergedHost = null; 
            _lastMergedCreators = null;
        } 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.AddCreator1"]/*' />
        /// <devdoc> 
        ///     Adds a new toolbox item creator.
        /// </devdoc>
        void IToolboxService.AddCreator(ToolboxItemCreatorCallback creator, string format, IDesignerHost host) {
 
            if (creator == null) {
                throw new ArgumentNullException("creator"); 
            } 

            if (format == null) { 
                throw new ArgumentNullException("format");
            }

            if (host == null) { 
                throw new ArgumentNullException("host");
            } 
 
            if (_designerCreators == null) {
                _designerCreators = new Hashtable(); 
            }

            ArrayList list = _designerCreators[host] as ArrayList;
            if (list == null) { 
                list = new ArrayList(4);
                _designerCreators[host] = list; 
            } 

            list.Add(new ToolboxItemCreator(creator, format)); 

            // We now need to re-query because the list has changed.
            //
            _lastMergedHost = null; 
            _lastMergedCreators = null;
        } 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.AddLinkedToolboxItem"]/*' />
        /// <devdoc> 
        ///     Adds a new tool to the toolbox under the default category.
        /// </devdoc>
        void IToolboxService.AddLinkedToolboxItem(ToolboxItem toolboxItem, IDesignerHost host) {
 
            if (toolboxItem == null) {
                throw new ArgumentNullException("toolboxItem"); 
            } 

            if (host == null) { 
                throw new ArgumentNullException("host");
            }

            ToolboxItemContainer item = CreateItemContainer(toolboxItem, host); 

            // Item can be null if this service doesn't support linking. 
            // 
            if (item != null) {
                GetItemContainers(SelectedCategory).Add(item); 
            }
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.AddLinkedToolboxItem1"]/*' /> 
        /// <devdoc>
        ///     Adds a new tool to the toolbox under the specified category. 
        /// </devdoc> 
        void IToolboxService.AddLinkedToolboxItem(ToolboxItem toolboxItem, string category, IDesignerHost host) {
 
            if (toolboxItem == null) {
                throw new ArgumentNullException("toolboxItem");
            }
 
            if (category == null) {
                throw new ArgumentNullException("category"); 
            } 

            if (host == null) { 
                throw new ArgumentNullException("host");
            }

            ToolboxItemContainer item = CreateItemContainer(toolboxItem, host); 

            // Item can be null if this service doesn't support linking. 
            // 
            if (item != null) {
                GetItemContainers(category).Add(item); 
            }
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.AddLinkedToolboxItem2"]/*' /> 
        /// <devdoc>
        ///     Adds a new tool to the toolbox under the default category. 
        /// </devdoc> 
        void IToolboxService.AddToolboxItem(ToolboxItem toolboxItem) {
 
            if (toolboxItem == null) {
                throw new ArgumentNullException("toolboxItem");
            }
 
            ToolboxItemContainer item = CreateItemContainer(toolboxItem, null);
 
            if (item != null) { 
                GetItemContainers(SelectedCategory).Add(item);
            } 
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.AddLinkedToolboxItem3"]/*' />
        /// <devdoc> 
        ///     Adds a new tool to the toolbox under the specified category.
        /// </devdoc> 
        void IToolboxService.AddToolboxItem(ToolboxItem toolboxItem, string category) { 

            if (toolboxItem == null) { 
                throw new ArgumentNullException("toolboxItem");
            }

            if (category == null) { 
                throw new ArgumentNullException("category");
            } 
 
            ToolboxItemContainer item = CreateItemContainer(toolboxItem, null);
 
            if (item != null) {
                GetItemContainers(category).Add(item);
            }
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.DeserializeToolboxItem"]/*' /> 
        /// <devdoc> 
        ///     Gets a toolbox item from a previously serialized object.
        /// </devdoc> 
        ToolboxItem IToolboxService.DeserializeToolboxItem(object serializedObject) {

            if (serializedObject == null) {
                throw new ArgumentNullException("serializedObject"); 
            }
 
            IDataObject dataObject = serializedObject as IDataObject; 
            if (dataObject == null) {
                dataObject = new DataObject(serializedObject); 
            }

            ToolboxItemContainer container = CreateItemContainer(dataObject);
            if (container != null) { 
                return container.GetToolboxItem(GetCreatorCollection(null));
            } 
 
            return null;
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.DeserializeToolboxItem1"]/*' />
        /// <devdoc>
        ///     Gets a toolbox item from a previously serialized object. 
        /// </devdoc>
        ToolboxItem IToolboxService.DeserializeToolboxItem(object serializedObject, IDesignerHost host) { 
 
            if (serializedObject == null) {
                throw new ArgumentNullException("serializedObject"); 
            }

            if (host == null) {
                throw new ArgumentNullException("host"); 
            }
 
            IDataObject dataObject = serializedObject as IDataObject; 
            if (dataObject == null) {
                dataObject = new DataObject(serializedObject); 
            }

            ToolboxItemContainer container = CreateItemContainer(dataObject);
            if (container != null) { 
                return container.GetToolboxItem(GetCreatorCollection(host));
            } 
 
            return null;
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.GetSelectedToolboxItem"]/*' />
        /// <devdoc>
        ///     Gets the currently selected tool. 
        /// </devdoc>
        ToolboxItem IToolboxService.GetSelectedToolboxItem() { 
            ToolboxItemContainer container = SelectedItemContainer; 
            if (container != null) {
                return container.GetToolboxItem(GetCreatorCollection(null)); 
            }
            return null;
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.GetSelectedToolboxItem1"]/*' />
        /// <devdoc> 
        ///     Gets the currently selected tool. 
        /// </devdoc>
        ToolboxItem IToolboxService.GetSelectedToolboxItem(IDesignerHost host) { 

            if (host == null) {
                throw new ArgumentNullException("host");
            } 

            ToolboxItemContainer container = SelectedItemContainer; 
            if (container != null) { 
                return container.GetToolboxItem(GetCreatorCollection(host));
            } 
            return null;
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.GetToolboxItems"]/*' /> 
        /// <devdoc>
        ///     Gets all .NET Framework tools on the toolbox. 
        /// </devdoc> 
        ToolboxItemCollection IToolboxService.GetToolboxItems() {
            IList itemContainers = GetItemContainers(); 
            ArrayList items = new ArrayList(itemContainers.Count);
            ICollection creators = GetCreatorCollection(null);
            foreach(ToolboxItemContainer container in itemContainers) {
                ToolboxItem item = container.GetToolboxItem(creators); 
                if (item != null) {
                    items.Add(item); 
                } 
            }
            ToolboxItem[] itemArray = new ToolboxItem[items.Count]; 
            items.CopyTo(itemArray, 0);
            return new ToolboxItemCollection(itemArray);
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.GetToolboxItems1"]/*' />
        /// <devdoc> 
        ///     Gets all .NET Framework tools on the toolbox. 
        /// </devdoc>
        ToolboxItemCollection IToolboxService.GetToolboxItems(IDesignerHost host) { 

            if (host == null) {
                throw new ArgumentNullException("host");
            } 

            IList itemContainers = GetItemContainers(); 
            ArrayList items = new ArrayList(itemContainers.Count); 
            ICollection creators = GetCreatorCollection(host);
            foreach(ToolboxItemContainer container in itemContainers) { 
                ToolboxItem item = container.GetToolboxItem(creators);
                if (item != null) {
                    items.Add(item);
                } 
            }
            ToolboxItem[] itemArray = new ToolboxItem[items.Count]; 
            items.CopyTo(itemArray, 0); 
            return new ToolboxItemCollection(itemArray);
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.GetToolboxItems2"]/*' />
        /// <devdoc>
        ///     Gets all .NET Framework tools on the specified toolbox category. 
        /// </devdoc>
        ToolboxItemCollection IToolboxService.GetToolboxItems(String category) { 
 
            if (category == null) {
                throw new ArgumentNullException("category"); 
            }

            IList itemContainers = GetItemContainers(category);
            ArrayList items = new ArrayList(itemContainers.Count); 
            ICollection creators = GetCreatorCollection(null);
            foreach(ToolboxItemContainer container in itemContainers) { 
                ToolboxItem item = container.GetToolboxItem(creators); 
                if (item != null) {
                    items.Add(item); 
                }
            }
            ToolboxItem[] itemArray = new ToolboxItem[items.Count];
            items.CopyTo(itemArray, 0); 
            return new ToolboxItemCollection(itemArray);
        } 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.GetToolboxItems3"]/*' />
        /// <devdoc> 
        ///     Gets all .NET Framework tools on the specified toolbox category.
        /// </devdoc>
        ToolboxItemCollection IToolboxService.GetToolboxItems(String category, IDesignerHost host) {
 
            if (category == null) {
                throw new ArgumentNullException("category"); 
            } 

            if (host == null) { 
                throw new ArgumentNullException("host");
            }

            IList itemContainers = GetItemContainers(category); 
            ArrayList items = new ArrayList(itemContainers.Count);
            ICollection creators = GetCreatorCollection(host); 
            foreach(ToolboxItemContainer container in itemContainers) { 
                ToolboxItem item = container.GetToolboxItem(creators);
                if (item != null) { 
                    items.Add(item);
                }
            }
            ToolboxItem[] itemArray = new ToolboxItem[items.Count]; 
            items.CopyTo(itemArray, 0);
            return new ToolboxItemCollection(itemArray); 
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.IsSupported"]/*' /> 
        /// <devdoc>
        ///     Determines if the given designer host contains a designer that supports the serialized
        ///     toolbox item.  This will return false if the designer doesn't support the item, or if the
        ///     serializedObject parameter does not contain a toolbox item. 
        /// </devdoc>
        bool IToolboxService.IsSupported(object serializedObject, IDesignerHost host) { 
 
            if (serializedObject == null) {
                throw new ArgumentNullException("serializedObject"); 
            }

            if (host == null) {
                throw new ArgumentNullException("host"); 
            }
 
            IDataObject dataObject = serializedObject as IDataObject; 
            if (dataObject == null) {
                dataObject = new DataObject(serializedObject); 
            }

            // First, is this even a valid serialized object?
            // 
            if (!IsItemContainer(dataObject, host)) {
                return false; 
            } 

            ToolboxItemContainer container = CreateItemContainer(dataObject); 


            // Second, identify the filter that the host is using.  If
            // the filter matches, then we are OK. 
            //
            return IsItemContainerSupported(container, host); 
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.IsSupported1"]/*' /> 
        /// <devdoc>
        ///     Determines if the serialized toolbox item contains a matching collection of filter attributes.
        ///     This will return false if the serializedObject parameter doesn't contain a toolbox item,
        ///     or if the collection of filter attributes does not match. 
        /// </devdoc>
        bool IToolboxService.IsSupported(object serializedObject, ICollection filterAttributes) { 
 
            if (serializedObject == null) {
                throw new ArgumentNullException("serializedObject"); 
            }

            if (filterAttributes == null) {
                throw new ArgumentNullException("filterAttributes"); 
            }
 
            IDataObject dataObject = serializedObject as IDataObject; 
            if (dataObject == null) {
                dataObject = new DataObject(serializedObject); 
            }

            // First, is this even a valid serialized object?
            // 
            if (!IsItemContainer(dataObject, null)) {
                return false; 
            } 

            ToolboxItemContainer container = CreateItemContainer(dataObject); 

            return GetFilterSupport(container.GetFilter(GetCreatorCollection(null)), filterAttributes) == FilterSupport.Supported;
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.IsToolboxItem"]/*' />
        /// <devdoc> 
        ///     Gets a value indicating whether the specified object contains a serialized toolbox item. 
        /// </devdoc>
        bool IToolboxService.IsToolboxItem(object serializedObject) { 

            if (serializedObject == null) {
                throw new ArgumentNullException("serializedObject");
            } 

            IDataObject dataObject = serializedObject as IDataObject; 
            if (dataObject == null) { 
                dataObject = new DataObject(serializedObject);
            } 

            return IsItemContainer(dataObject, null);
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.IsToolboxItem1"]/*' />
        /// <devdoc> 
        ///     Gets a value indicating whether the specified object contains a serialized toolbox item. 
        /// </devdoc>
        bool IToolboxService.IsToolboxItem(object serializedObject, IDesignerHost host) { 

            if (serializedObject == null) {
                throw new ArgumentNullException("serializedObject");
            } 

            if (host == null) { 
                throw new ArgumentNullException("host"); 
            }
 
            IDataObject dataObject = serializedObject as IDataObject;
            if (dataObject == null) {
                dataObject = new DataObject(serializedObject);
            } 

            return IsItemContainer(dataObject, host); 
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.Refresh"]/*' /> 
        /// <devdoc>
        ///     Refreshes the state of the toolbox items.
        /// </devdoc>
        void IToolboxService.Refresh() { 
            Refresh();
        } 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.RemoveCreator"]/*' />
        /// <devdoc> 
        ///     Removes a previously added toolbox creator.
        /// </devdoc>
        void IToolboxService.RemoveCreator(string format) {
 
            if (format == null) {
                throw new ArgumentNullException("format"); 
            } 

            if (_globalCreators != null) { 
                for (int i = 0; i < _globalCreators.Count; i++) {
                    ToolboxItemCreator creator = _globalCreators[i] as ToolboxItemCreator;
                    if (creator.Format.Equals(format)) {
                        _globalCreators.RemoveAt(i); 

                        // We now need to re-query because the list has changed. 
                        // 
                        _lastMergedHost = null;
                        _lastMergedCreators = null; 

                        return;
                    }
                } 
            }
        } 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.RemoveCreator1"]/*' />
        /// <devdoc> 
        ///     Removes a previously added toolbox creator.
        /// </devdoc>
        void IToolboxService.RemoveCreator(string format, IDesignerHost host) {
 
            if (format == null) {
                throw new ArgumentNullException("format"); 
            } 

            if (host == null) { 
                throw new ArgumentNullException("host");
            }

            if (_designerCreators != null) { 
                ArrayList list = _designerCreators[host] as ArrayList;
                if (list != null) { 
                    for (int i = 0; i < list.Count; i++) { 
                        ToolboxItemCreator creator = list[i] as ToolboxItemCreator;
                        if (creator.Format.Equals(format)) { 
                            list.RemoveAt(i);

                            // We now need to re-query because the list has changed.
                            // 
                            _lastMergedHost = null;
                            _lastMergedCreators = null; 
 
                            return;
                        } 
                    }
                }
            }
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.RemoveToolboxItem"]/*' /> 
        /// <devdoc> 
        ///     Removes the specified tool from the toolbox.
        /// </devdoc> 
        void IToolboxService.RemoveToolboxItem(ToolboxItem toolboxItem) {

            if (toolboxItem == null) {
                throw new ArgumentNullException("toolboxItem"); 
            }
 
            GetItemContainers().Remove(CreateItemContainer(toolboxItem, null)); 
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.RemoveToolboxItem1"]/*' />
        /// <devdoc>
        ///     Removes the specified tool from the toolbox.
        /// </devdoc> 
        void IToolboxService.RemoveToolboxItem(ToolboxItem toolboxItem, string category) {
 
            if (toolboxItem == null) { 
                throw new ArgumentNullException("toolboxItem");
            } 

            if (category == null) {
                throw new ArgumentNullException("category");
            } 

            GetItemContainers(category).Remove(CreateItemContainer(toolboxItem, null)); 
 
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.SelectedToolboxItemUsed"]/*' />
        /// <devdoc>
        ///     Notifies the toolbox that the selected tool has been used.
        /// </devdoc> 
        void IToolboxService.SelectedToolboxItemUsed() {
            SelectedItemContainerUsed(); 
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.SerializeToolboxItem"]/*' /> 
        /// <devdoc>
        ///     Takes the given toolbox item and serializes it to a persistent object.  This object can then
        ///     be stored in a stream or passed around in a drag and drop or clipboard operation.
        /// </devdoc> 
        object IToolboxService.SerializeToolboxItem(ToolboxItem toolboxItem) {
 
            if (toolboxItem == null) { 
                throw new ArgumentNullException("toolboxItem");
            } 

            return CreateItemContainer(toolboxItem, null).ToolboxData;
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.SetCursor"]/*' />
        /// <devdoc> 
        ///     Sets the current application's cursor to a cursor that represents the 
        ///     currently selected tool.
        /// </devdoc> 
        bool IToolboxService.SetCursor() {
            return SetCursor();
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.SetSelectedToolboxItem"]/*' />
        /// <devdoc> 
        ///     Sets the currently selected tool in the toolbox. 
        /// </devdoc>
        void IToolboxService.SetSelectedToolboxItem(ToolboxItem toolboxItem) { 
            if (toolboxItem != null) {
                SelectedItemContainer = CreateItemContainer(toolboxItem, null);
            }
            else { 
                SelectedItemContainer = null;
            } 
        } 

 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IComponentDiscoveryService.GetComponentTypes"]/*' />
        ICollection IComponentDiscoveryService.GetComponentTypes(IDesignerHost designerHost, Type baseType) {
            Hashtable types = new Hashtable();
 
            ToolboxItemCollection items = ((IToolboxService)this).GetToolboxItems();
            if (items != null) { 
                Type componentType = typeof(IComponent); 
                foreach (ToolboxItem item in items) {
                    Type t = item.GetType(designerHost); 
                    if (t != null) {
                        if (componentType.IsAssignableFrom(t) == false) {
                            continue;
                        } 
                        if ((baseType != null) && (baseType.IsAssignableFrom(t) == false)) {
                            continue; 
                        } 
                        types[t] = t;
                    } 
                }
            }

            return types.Values; 
        }
 
        /// <devdoc> 
        ///     Proxy object to allow us to do cross-domain calls.
        /// </devdoc> 
        private class DomainProxyObject : MarshalByRefObject {

            // [....] : Changed this from a stream to a byte[].  Remoting bug
            // VSWhidbey 90430 causes streams to be marshaled incorrectly across the boundary. 
            //
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods")] 
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:AvoidUnusedParameters")] 
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
            internal byte[] GetToolboxItems(AssemblyName an, bool throwOnError) { 

                // Load the assembly here.  We are running in a different
                // app domain, so we can load.  When we're finished and
                // we return, the caller will unload the domain and free 
                // the file.
                // 
                Assembly assembly = null; 

                try { 
                    assembly = Assembly.Load(an);
                }
                catch (FileNotFoundException) {
                } 
                catch (BadImageFormatException) {
                } 
                catch (IOException) { 
                }
 
                if (assembly == null && an.CodeBase != null) {
                    assembly = Assembly.LoadFrom(new System.Uri(an.CodeBase).LocalPath);
                }
 
                if (assembly == null) {
                    throw new ArgumentException(SR.GetString(SR.ToolboxServiceAssemblyNotFound, an.FullName)); 
                } 

                ICollection items = null; 

                try {
                    items = ToolboxService.GetToolboxItems(assembly, null, throwOnError);
                } 
                catch (Exception e) {
                    //we have to convert the exception if its a ReflectionTypeLoadException 
                    ReflectionTypeLoadException typeloadex = e as ReflectionTypeLoadException; 
                    if (typeloadex != null) {
                        //remove the types so we don't try to load them when going to the main domain. 
                        throw new ReflectionTypeLoadException(null, typeloadex.LoaderExceptions, typeloadex.Message);
                    }
                    //otherwise, we can just throw the original exception.
                    throw; 
                }
 
                BinaryFormatter formatter = new BinaryFormatter(); 
                MemoryStream stream = new MemoryStream();
 
                formatter.Serialize(stream, items);
                stream.Close();
                return stream.GetBuffer();
            } 
        }
 
        /// <devdoc> 
        ///     Private enum that identifies the level of filter
        ///     support. 
        /// </devdoc>
        private enum FilterSupport {
            NotSupported,
            Supported, 
            Custom
        } 
 
    }
 
    /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.ToolboxItemCreator"]/*' />
    /// <devdoc>
    ///     The ToolboxItemCreator class encapsulates toolbox
    ///     item crator callbacks.  These callbacks are used by 
    ///     designers to provide ways to create toolbox items
    ///     for custom data objects. 
    /// </devdoc> 
    public sealed class ToolboxItemCreator {
 
        private ToolboxItemCreatorCallback _callback;
        private string _format;

        internal ToolboxItemCreator(ToolboxItemCreatorCallback callback, string format) { 
            _callback = callback;
            _format = format; 
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.ToolboxItemCreator.Create"]/*' /> 
        /// <devdoc>
        ///     Creates a new toolbox item given a
        ///     data object.  This may raise an exception
        ///     if the data object does not contain 
        ///     data for the supported format.
        /// </devdoc> 
        public ToolboxItem Create(IDataObject data) { 
            return _callback(data, _format);
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.ToolboxItemCreator.Format"]/*' />
        /// <devdoc>
        ///     The data format that this toolbox item 
        ///     creator supports.
        /// </devdoc> 
        public string Format { 
            get {
                return _format; 
            }
        }
    }
 
    /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer"]/*' />
    /// <devdoc> 
    ///     The ToolboxItemContainer class contains a toolbox 
    ///     item and can convert a toolbox item to a data object
    ///     and back. 
    /// </devdoc>
    [Serializable]
    public class ToolboxItemContainer : ISerializable {
 
        private const string _localClipboardFormat = "CF_TOOLBOXITEMCONTAINER";
        private const string _itemClipboardFormat = "CF_TOOLBOXITEMCONTAINER_CONTENTS"; 
        private const string _hashClipboardFormat = "CF_TOOLBOXITEMCONTAINER_HASH"; 
        private const string _serializationFormats = "TbxIC_DataObjectFormats";
        private const string _serializationValues = "TbxIC_DataObjectValues"; 
        private const short _clipboardVersion = 1;

        private int _hashCode;
        private ToolboxItem _toolboxItem; 
        private IDataObject _dataObject;
        private ICollection _filter; 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.ToolboxItemContainer"]/*' />
        /// <devdoc> 
        ///     Serialization constructor.  ToolboxItemContainers
        ///     can be serialized.  Note that generally it is not
        ///     necessary to override the serialization mechanism
        ///     for a toolbox item container.  Toolbox item 
        ///     containers implement serialization by saving the
        ///     IDataObject returned from ToolboxData, so when 
        ///     overriding ToolboxData and providing your own 
        ///     custom data this data will be included with
        ///     the default ISerializable implementation.  You 
        ///     would want to override the default serialization
        ///     implementation only if you intend to store
        ///     private details about this toolbox item
        ///     container that should not be exposed through 
        ///     the public data object.
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:AvoidUnusedParameters")] 
        protected ToolboxItemContainer(SerializationInfo info, StreamingContext context) {
            string[] formats = (string[])info.GetValue(_serializationFormats, typeof(string[])); 
            object[] data = (object[])info.GetValue(_serializationValues, typeof(object[]));

            Debug.Assert(formats.Length == data.Length, "Array mismatch in serialization");
 
            DataObject d = new DataObject();
 
            for (int i = 0; i < formats.Length; i++) { 
                d.SetData(formats[i], data[i]);
            } 

            _dataObject = d;
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.ToolboxItemContainer1"]/*' />
        /// <devdoc> 
        ///     Creates a new ToolboxItemContainer object from 
        ///     a toolbox item.
        /// </devdoc> 
        public ToolboxItemContainer(ToolboxItem item) {
            if (item == null) {
                throw new ArgumentNullException("item");
            } 
            _toolboxItem = item;
            UpdateFilter(item); 
 
            _hashCode = item.DisplayName.GetHashCode();
            if (item.AssemblyName != null) { 
                _hashCode ^= item.AssemblyName.GetHashCode();
            }
            if (item.TypeName != null) {
                _hashCode ^= item.TypeName.GetHashCode(); 
            }
            if (_hashCode == 0) { 
                _hashCode = item.DisplayName.GetHashCode(); 
            }
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.ToolboxItemContainer2"]/*' />
        /// <devdoc>
        ///     Creates a new ToolboxItemContainer object from an 
        ///     IDataObject.  The data object can contain
        ///     data provided by the ToolboxItemContainer class, or it 
        ///     can contain data that can be read by one 
        ///     of the toolbox item creators that have
        ///     been supplied by the user. 
        /// </devdoc>
        public ToolboxItemContainer(IDataObject data) {
            if (data == null) {
                throw new ArgumentNullException("data"); 
            }
            _dataObject = data; 
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.IsCreated"]/*' /> 
        /// <devdoc>
        ///     Returns true if the underlying toolbox item has been deserialized.
        /// </devdoc>
        public bool IsCreated { 
            get {
                return (_toolboxItem != null); 
            } 
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.IsTransient"]/*' />
        /// <devdoc>
        ///     Returns true if the toolbox item contained in this
        ///     container has been marked as transient. 
        /// </devdoc>
        public bool IsTransient { 
            get { 
                if (_toolboxItem != null) {
                    return _toolboxItem.IsTransient; 
                }

                // If the toolbox item is already persisted, then
                // it must not have been transient. 
                return false;
            } 
        } 

 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.ToolboxData"]/*' />
        /// <devdoc>
        ///     The serialized version of the toolbox item.
        ///     This data object can be used by an application 
        ///     to store this toolbox item.  This data object
        ///     will be fabricated from the toolbox item if 
        ///     necessary.  Implementors may override this to 
        ///     provide additional storage information in the
        ///     data object. 
        /// </devdoc>
        public virtual IDataObject ToolboxData {
            get {
                if (_dataObject == null) { 
                    MemoryStream stream = new MemoryStream();
                    DataObject d = new DataObject(); 
 
                    BinaryWriter writer = new BinaryWriter(stream);
                    writer.Write(_clipboardVersion); 
                    writer.Write((short)_filter.Count);
                    foreach (ToolboxItemFilterAttribute attr in _filter) {
                        writer.Write(attr.FilterString);
                        writer.Write((short)attr.FilterType); 
                    }
                    writer.Flush(); 
                    stream.Close(); 
                    d.SetData(_localClipboardFormat, stream.GetBuffer());
                    d.SetData(_hashClipboardFormat, _hashCode); 

                    // Now it's time for the toolbox item itself.  We want
                    // to defer actually serializing the toolbox item until
                    // we need to save this to disk, but we want to ensure that 
                    // when we load it up at a later date we can recover the item
                    // if it in a non-gac assembly.  So we have a wrapper object 
                    // that implements ISerializable to handle this for us. 
                    //
                    d.SetData(_itemClipboardFormat, new ToolboxItemSerializer(_toolboxItem)); 
                    _dataObject = d;
                }

                return _dataObject; 
            }
        } 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.UpdateFilter"]/*' />
        /// <devdoc> 
        /// This method is called to update the containers filter with the filter from the item.
        /// This should be called when the toolbox item was modified (or configured) or if a new TypeDescriptionProvider was
        /// added such that the filter will be changed.
        /// </devdoc> 
        public void UpdateFilter(ToolboxItem item) {
            _filter = MergeFilter(item); 
        } 

        /// <devdoc> 
        ///     This will determine if the data object
        ///     contains our clipboard format.
        /// </devdoc>
        internal static bool ContainsFormat(IDataObject dataObject) { 
            return dataObject.GetDataPresent(_localClipboardFormat);
        } 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.Equals"]/*' />
        /// <devdoc> 
        ///     Equals override for this class
        /// </devdoc>
        public override bool Equals(object obj) {
            ToolboxItemContainer them = obj as ToolboxItemContainer; 

            if (them == this) { 
                return true; 
            }
 
            if (them == null) {
                return false;
            }
 
            if (this._toolboxItem != null && them._toolboxItem != null && this._toolboxItem.Equals(them._toolboxItem)) {
                return true; 
            } 

            if (this._dataObject != null && them._dataObject != null && this._dataObject.Equals(them._dataObject)) { 
                return true;
            }

            ToolboxItem ourItem = GetToolboxItem(null); 
            ToolboxItem theirItem = them.GetToolboxItem(null);
 
            if (ourItem != null && theirItem != null) { 
                return ourItem.Equals(theirItem);
            } 

            return false;
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.GetFilter"]/*' />
        /// <devdoc> 
        ///     The types stored in a toolbox item may have 
        ///     a filter associated with them.  Filters can be
        ///     used to restrict the tools that can be placed 
        ///     on designers. The objects in this collection are
        ///     all instances of ToolboxItemFilterAttribute.
        /// </devdoc>
        public virtual ICollection GetFilter(ICollection creators) { 

            ICollection filter = _filter; 
 
            if (_filter == null) {
 
                if (_dataObject.GetDataPresent(_localClipboardFormat)) {

                    // Our own private format is in the data object.
                    // This format contains filter data, so we just 
                    // need to pull it out.
 
                    byte[] bytes = (byte[])_dataObject.GetData(_localClipboardFormat); 
                    if (bytes != null) {
                        // If the stream is null, that could mean that we could not extract it.  This is essentially 
                        // a "dead" toolbox item
                        BinaryReader reader = new BinaryReader(new MemoryStream(bytes));
                        short version = reader.ReadInt16();
 
                        if (version != _clipboardVersion) {
                            Debug.Fail("Toolbox item version mismatch.  Toolbox database contains version " + version.ToString(CultureInfo.InvariantCulture) + " and current codebase expects version " + _clipboardVersion.ToString(CultureInfo.InvariantCulture)); 
                            _filter = new ToolboxItemFilterAttribute[0]; 
                        }
                        else { 
                            short filterCount = reader.ReadInt16();
                            ToolboxItemFilterAttribute[] filterArray = new ToolboxItemFilterAttribute[filterCount];

                            for (short i = 0; i < filterCount; i++) { 
                                string filterName = reader.ReadString();
                                short filterValue = reader.ReadInt16(); 
 
                                filterArray[i] = new ToolboxItemFilterAttribute(filterName, (ToolboxItemFilterType)filterValue);
                            } 

                            _filter = filterArray;
                        }
                    } 
                    else {
                        _filter = new ToolboxItemFilterAttribute[0]; 
                    } 

                    filter = _filter; 
                }
                else {

                    // We don't recognize the format of this data object. 
                    // Ask one or more creators if it does.
 
                    if (creators != null) { 
                        foreach (ToolboxItemCreator creator in creators) {
                            if (_dataObject.GetDataPresent(creator.Format)) { 
                                ToolboxItem item = creator.Create(_dataObject);
                                if (item != null) {
                                    filter = MergeFilter(item);
                                    break; 
                                }
                            } 
                        } 
                    }
                } 
            }

            return filter;
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.GetHashCode"]/*' /> 
        /// <devdoc> 
        ///     Override of hash code
        /// </devdoc> 
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
		public override int GetHashCode() {

            // Recover the hash code we saved into the data object. 
            if (_hashCode == 0) {
                if (_dataObject != null && _dataObject.GetDataPresent(_hashClipboardFormat)) { 
                    _hashCode = (int)_dataObject.GetData(_hashClipboardFormat); 
                }
            } 

            // If we failed, hash against our object identity.
            if (_hashCode == 0) {
                _hashCode = base.GetHashCode(); 
            }
 
            return _hashCode; 
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.GetObjectData"]/*' />
        /// <devdoc>
        ///     Protected implementation of ISerializable.
        ///     ToolboxItemContainers can be serialized. 
        ///     Note that generally it is not necessary to
        ///     override the serialization mechanism 
        ///     for a toolbox item container.  Toolbox item 
        ///     containers implement serialization by saving the
        ///     IDataObject returned from ToolboxData, so when 
        ///     overriding ToolboxData and providing your own
        ///     custom data this data will be included with
        ///     the default ISerializable implementation.  You
        ///     would want to override the default serialization 
        ///     implementation only if you intend to store
        ///     private details about this toolbox item 
        ///     container that should not be exposed through 
        ///     the public data object.
        /// </devdoc> 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)]
        protected virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
            IDataObject d = ToolboxData;
 
            string[] formats = d.GetFormats();
            object[] data = new object[formats.Length]; 
 
            for (int i = 0; i < data.Length; i++) {
                data[i] = d.GetData(formats[i]); 
                Debug.Assert(data[i] != null, "Data format contained within toolbox " + formats[i] + " does not implement serializable data?");
            }

            info.AddValue(_serializationFormats, formats); 
            info.AddValue(_serializationValues, data);
        } 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.GetToolboxItem"]/*' />
        /// <devdoc> 
        ///     This will retrieve the toolbox item stored in
        ///     this ToolboxItemContainer class.  The creators
        ///     argument supplies a collection of toolbox item
        ///     creators that can be used in case the container 
        ///     is unable to create the toolbox item itself.
        ///     Iimplementors may override this to provide custom 
        ///     deserialization of the data object. 
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
        public virtual ToolboxItem GetToolboxItem(ICollection creators) {

            ToolboxItem item = _toolboxItem;
 
            if (_toolboxItem == null) {
                Debug.Assert(_dataObject != null, "Should always have a data object"); 
 
                if (_dataObject.GetDataPresent(_itemClipboardFormat)) {
 

                    // Our own private data format is in the data object.
                    // This format corresponds directly to a toolbox item,
                    // so all we need to do is deserialize it. 

                    string exceptionString = null; 
 
                    try {
                        ToolboxItemSerializer s = (ToolboxItemSerializer)_dataObject.GetData(_itemClipboardFormat); 
                        _toolboxItem = s.ToolboxItem;
                    }
                    catch (Exception ex) {
                        exceptionString = ex.Message; 
                    }
                    catch { 
                        Debug.Fail("What is this non CLS exception doing here?!?"); 
                    }
 
                    if (_toolboxItem == null) {
                        Debug.Fail("Toolbox item is either out of date or bogus.  We were unable to convert the data object to an item.  Does your toolbox need to be reset?");
                        _toolboxItem = new BrokenToolboxItem(exceptionString);
                    } 

                    item = _toolboxItem; 
                } 
                else {
 
                    // This is an unknown format.  Ask our creator collection if
                    // any of them support the format.  If they do, ask them to
                    // create a toolbox item.  Because the list of creators
                    // depends on the active designer, we do not stash the 
                    // toolbox item in our member variable here.  It is
                    // considered to be transient. 
 
                    if (creators != null) {
                        foreach (ToolboxItemCreator creator in creators) { 
                            if (_dataObject.GetDataPresent(creator.Format)) {
                                item = creator.Create(_dataObject);
                                if (item != null) {
                                    break; 
                                }
                            } 
                        } 
                    }
                } 
            }

            return item;
        } 

        /// <devdoc> 
        ///     This is a helper method that merges the filter from the 
        ///     toolbox item along with filter attributes on the item itself.
        /// </devdoc> 
        private static ICollection MergeFilter(ToolboxItem item) {

            ICollection existingFilter = item.Filter;
            ArrayList itemFilter = new ArrayList(); 

            foreach (Attribute a in TypeDescriptor.GetAttributes(item)) { 
                if (a is ToolboxItemFilterAttribute) { 
                    itemFilter.Add(a);
                } 
            }

            ICollection finalFilter;
 
            if (existingFilter == null || existingFilter.Count == 0) {
                finalFilter = itemFilter; 
            } 
            else {
                if (itemFilter.Count > 0) { 
                    Hashtable hash = new Hashtable(itemFilter.Count + existingFilter.Count);
                    foreach (Attribute a in itemFilter) {
                        hash[a.TypeId] = a;
                    } 
                    foreach (Attribute a in existingFilter) {
                        hash[a.TypeId] = a; 
                    } 
                    ToolboxItemFilterAttribute[] filter = new ToolboxItemFilterAttribute[hash.Values.Count];
                    hash.Values.CopyTo(filter, 0); 
                    finalFilter = filter;
                }
                else {
                    finalFilter = existingFilter; 
                }
            } 
 
            return finalFilter;
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.ISerializable.GetObjectData"]/*' />
        /// <devdoc>
        ///     Serialization method.  We make this private because the only time 
        ///     we serialize this class is when enumerating assemblies, and there
        ///     we never create instances of derived classes. 
        /// </devdoc> 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) { 
            GetObjectData(info, context);
        }

        /// <devdoc> 
        ///     BrokenToolboxItem is a placeholder for a toolbox item we failed to create.
        ///     It sits silently as a simple component until someone asks to create an instance 
        ///     of the objects within it, and then it displays an error to the user. 
        /// </devdoc>
        private class BrokenToolboxItem : ToolboxItem { 

            private string _exceptionString;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")] 
            public BrokenToolboxItem(string exceptionString)
                : base(typeof(Component)) { 
                _exceptionString = exceptionString; 
                Lock();
            } 

            protected override IComponent[] CreateComponentsCore(IDesignerHost host) {
                if (_exceptionString != null) {
                    throw new InvalidOperationException(SR.GetString(SR.ToolboxServiceBadToolboxItemWithException, _exceptionString)); 
                }
                else { 
                    throw new InvalidOperationException(SR.GetString(SR.ToolboxServiceBadToolboxItem)); 
                }
            } 
        }

        /// <devdoc>
        ///     This class serializes a toolbox item.  It prefixes the toolbox item with 
        ///     assembly information so that when the item is deseralized it can be loaded
        ///     from the assembly, even if that assembly is not in the GAC. 
        /// </devdoc> 
        [Serializable]
        private sealed class ToolboxItemSerializer : ISerializable { 

            private const string _assemblyNameKey = "AssemblyName";
            private const string _streamKey = "Stream";
 
            private static BinaryFormatter _formatter;
 
            private ToolboxItem _toolboxItem; 

            /// <devdoc> 
            /// </devdoc>
            internal ToolboxItemSerializer(ToolboxItem toolboxItem) {
                _toolboxItem = toolboxItem;
            } 

            /// <devdoc> 
            /// </devdoc> 
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:AvoidUnusedParameters")]
            private ToolboxItemSerializer(SerializationInfo info, StreamingContext context) { 
                AssemblyName name = (AssemblyName)info.GetValue(_assemblyNameKey, typeof(AssemblyName));
                byte[] bytes = (byte[])info.GetValue(_streamKey, typeof(byte[]));

                if (_formatter == null) { 
                    _formatter = new BinaryFormatter();
                } 
 
                SerializationBinder oldBinder = _formatter.Binder;
                _formatter.Binder = new ToolboxSerializationBinder(name); 
                try {
                    _toolboxItem = (ToolboxItem)_formatter.Deserialize(new MemoryStream(bytes));
                }
                finally { 
                    _formatter.Binder = oldBinder;
                } 
            } 

            /// <devdoc> 
            /// </devdoc>
            internal ToolboxItem ToolboxItem {
                get {
                    return _toolboxItem; 
                }
            } 
 
            /// <devdoc>
            /// </devdoc> 
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {

                if (_formatter == null) {
                    _formatter = new BinaryFormatter(); 
                }
 
                MemoryStream stream = new MemoryStream(); 
                _formatter.Serialize(stream, _toolboxItem);
                stream.Close(); 

                info.AddValue(_assemblyNameKey, _toolboxItem.GetType().Assembly.GetName());
                info.AddValue(_streamKey, stream.GetBuffer());
            } 
        }
 
        /// <devdoc> 
        ///     Simple serialization binder that is used to load up toolbox items from
        ///     assemblies that are not stored in the GAC. 
        /// </devdoc>
        private class ToolboxSerializationBinder : SerializationBinder {

            private Hashtable _assemblies; 
            private AssemblyName _name;
            private string _namePart; 
 
            /// <devdoc>
            ///     Create a new toolbox serialization binder. 
            /// </devdoc>
            public ToolboxSerializationBinder(AssemblyName name) {
                _assemblies = new Hashtable();
                _name = name; 
                _namePart = name.Name + ",";
            } 
 
            /// <devdoc>
            ///     Takes a type name and creates a type.  If it cannot match 
            ///     the type it returns null.
            /// </devdoc>
            [
                System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods"), 
                System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1307:SpecifyStringComparison")
            ] 
            public override Type BindToType(string assemblyName, string typeName) { 

                Assembly assembly = (Assembly)_assemblies[assemblyName]; 

                if (assembly == null) {

                    // Try the normal assembly load first. 
                    //
                    try { 
                        assembly = Assembly.Load(assemblyName); 
                    }
                    catch (FileNotFoundException) { 
                    }
                    catch (BadImageFormatException) {
                    }
                    catch (IOException) { 
                    }
 
                    if (assembly == null) { 

                        AssemblyName an; 

                        // Try our stashed assembly name.
                        if (assemblyName.StartsWith(_namePart)) {
                            an = _name; 

                            try { 
                                assembly = Assembly.Load(an); 
                            }
                            catch (FileNotFoundException) { 
                            }
                            catch (BadImageFormatException) {
                            }
                            catch (IOException) { 
                            }
                        } 
                        else { 
                            an = new AssemblyName(assemblyName);
                        } 

                        // Finally, load via codebase.
                        //
                        if (assembly == null) { 
                            string codeBase = an.CodeBase;
                            if (codeBase != null && codeBase.Length > 0 && File.Exists(codeBase)) { 
                                assembly = Assembly.LoadFrom(codeBase); 
                            }
                        } 
                    }

                    if (assembly != null) {
                        _assemblies[assemblyName] = assembly; 
                    }
                } 
 
                if (assembly != null) {
                    return assembly.GetType(typeName); 
                }

                // Binder couldn't handle it, let the default loader take over.
                return null; 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ToolboxService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Drawing.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Drawing; 
    using System.Globalization;
    using System.IO; 
    using System.Reflection; 
    using System.Runtime.Remoting.Lifetime;
    using System.Runtime.Serialization; 
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Security;
    using System.Security.Permissions;
    using System.Windows.Forms; 

    /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService"]/*' /> 
    /// <devdoc> 
    ///     This is a partial implementation of the IToolboxService
    ///     interface.  To use this implementation you must 
    ///     derive from this class and implement the abstract
    ///     methods.  Once implemented, you may add this class
    ///     to your designer application's service container.
    ///     There should be one toolbox service for each 
    ///     designer application.
    /// </devdoc> 
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")] 
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")]
    public abstract class ToolboxService : IToolboxService, IComponentDiscoveryService { 

        private IDesignerEventService   _designerEventService;

        private ArrayList       _globalCreators; 
        private Hashtable       _designerCreators;    // key: designer host, value: ArrayList of ToolboxItemCreators
 
        // A cache of the last merge we did to merge designer creators and global creators. 
        private IDesignerHost   _lastMergedHost;
        private ICollection     _lastMergedCreators; 

        // DesignerToolboxInfo stores filter and toolbox user information
        // on a per-designer basis.  This is the last one we queried.
        private DesignerToolboxInfo     _lastState; 

        // We maintain a separate app domain to enumerate assemblies without 
        // locking them. 
        //
        private static DomainProxyObject _domainObject; 
        private static AppDomain         _domain;
        private static ClientSponsor     _domainObjectSponsor;

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.CategoryNames"]/*' /> 
        /// <devdoc>
        ///     Retrieves a collection of category name strings. 
        ///     These category names correspond to various toolbox 
        ///     categories.
        /// </devdoc> 
        protected abstract CategoryNameCollection CategoryNames { get; }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.SelectedCategory"]/*' />
        /// <devdoc> 
        ///     Gets or sets the selected category for the toolbox.
        ///     Toolbox items are generally grouped into categories. 
        /// </devdoc> 
        protected abstract string SelectedCategory { get; set; }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.SelectedItemContainer"]/*' />
        /// <devdoc>
        ///     Gets or sets the selected toolbox item.
        /// </devdoc> 
        protected abstract ToolboxItemContainer SelectedItemContainer {get; set; }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.CreateItemContainer"]/*' /> 
        /// <devdoc>
        ///     Creates a new toolbox item container from a toolbox 
        ///     item.  This allows the implementor the chance to
        ///     provide a derived version of ToolboxItemContainer.
        ///     If the provided IDesignerHost link parameter is
        ///     non-null it indicates that this is a linked toolbox 
        ///     item.  By default, ToolboxService does not support
        ///     linked items so it will return null for non-null 
        ///     link parameters.  To provide link support, you should 
        ///     override this method to create a derived
        ///     ToolboxItemContainer object that knows how to handle 
        ///     links.  A "linked" toolbox item is one whose lifetime
        ///     is related to the storage of a particular designer
        ///     host.  So, in a typical project system, a designer
        ///     host is associated with a particular file.  A 
        ///     toobox item linked to a designer host would automatically
        ///     be deleted from the toolbox when the designer host's 
        ///     source file is deleted or removed from the project. 
        /// </devdoc>
        protected virtual ToolboxItemContainer CreateItemContainer(ToolboxItem item, IDesignerHost link) { 

            if (item == null) {
                throw new ArgumentNullException("item");
            } 

            // Default implementation does not support links. 
            if (link != null) { 
                return null;
            } 

            return new ToolboxItemContainer(item);
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.CreateItemContainer1"]/*' />
        /// <devdoc> 
        ///     Creates a new toolbox item container from a data object.  The 
        ///     data object passed in should contain data obtained from
        ///     a prior call to the ToolboxData property on a toolbox item 
        ///     container.
        /// </devdoc>
        protected virtual ToolboxItemContainer CreateItemContainer(IDataObject dataObject) {
 
            if (dataObject == null) {
                throw new ArgumentNullException("dataObject"); 
            } 

            return new ToolboxItemContainer(dataObject); 
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.FilterChanged"]/*' />
        /// <devdoc> 
        ///     This event is raised when the toolbox service detects that
        ///     the toolbox item filter for the actvie designer 
        ///     has changed. 
        /// </devdoc>
        protected virtual void FilterChanged() { 
        }

        /// <devdoc>
        ///     Returns an ICollection of toolbox item creators, or null if there 
        ///     is no active creator collection.  This will merge global creators
        ///     in with the provided host, if not null.  This also caches the 
        ///     last provided merged collection because merging takes time. 
        /// </devdoc>
        private ICollection GetCreatorCollection(IDesignerHost host) { 

            // If not provided a host, we just return the global
            // creator collection.
            // 
            if (host == null) {
                return _globalCreators; 
            } 

            // If we are provided a host, and that host matches the 
            // last request, we returned the last merged set.  Otherwise,
            //  we build a new merged set.
            //
            if (host != _lastMergedHost) { 

                ICollection creators = _globalCreators; 
                ICollection hostCreators = null; 

                if (_designerCreators != null) { 

                    hostCreators = _designerCreators[host] as ICollection;

                    if (hostCreators != null) { 

                        int cnt = hostCreators.Count; 
 
                        if (creators != null) {
                            cnt += creators.Count; 
                        }

                        ToolboxItemCreator[] newCreators = new ToolboxItemCreator[cnt];
 
                        hostCreators.CopyTo(newCreators, 0);
                        if (creators != null) { 
                            creators.CopyTo(newCreators, hostCreators.Count); 
                        }
                        creators = newCreators; 
                    }
                }

                _lastMergedCreators = creators; 
                _lastMergedHost = host;
            } 
            #if DEBUG 

            // For debug builds verify that our caching algorithm didn't miss. 
            //
            else if (_lastMergedCreators != null) {
                int debugCount = 0;
 
                if (_globalCreators != null) {
                    debugCount += _globalCreators.Count; 
                } 

                if (_designerCreators != null) { 
                    ICollection debugCreators = _designerCreators[host] as ICollection;
                    if (debugCreators != null) {
                        debugCount += debugCreators.Count;
                    } 
                }
 
                Debug.Assert(_lastMergedCreators.Count == debugCount, "ToolboxItemCreator cache algorithm is broken."); 
            }
            #endif 

            return _lastMergedCreators;
        }
 
        /// <devdoc>
        ///     Determines the type of filter support given two filter collections. 
        /// 
        ///     Truth Table:
        /// 
        ///                  Root Designer
        ///     Class        Mismatch   Allow         Prevent   Require       Custom
        ///     Mismatch     Y          Y             Y         N             Y
        ///     Allow        Y          Y             N         Y             IsSupported 
        ///     Prevent      Y          N             N         N             N
        ///     Require      N          Y             N         Y             IsSupported 
        ///     Custom       Y          IsSupported   N         IsSupported   IsSupported 
        ///
        ///     Legend: 
        ///
        ///     Y : The toolbox item will be enabled
        ///     N : The toolbox item will be disabled
        ///     IsSupported: The toolbox item will be enabled only if the method IToolboxUser.IsSupported returns true. 
        ///
        /// </devdoc> 
        private static FilterSupport GetFilterSupport(ICollection itemFilter, ICollection targetFilter) { 

            FilterSupport support = FilterSupport.Supported; 

            int requireCount = 0;
            int requireMatch = 0;
 
            // If Custom is specified on the designer, then we check to see if the
            // filter name matches an attribute, or if the filter name is empty. 
            // If either is the case, then we will invoke the designer for custom 
            // support.
            // 
            foreach(ToolboxItemFilterAttribute attr in itemFilter) {

                if (support == FilterSupport.NotSupported) {
                    break; 
                }
 
                if (attr.FilterType == ToolboxItemFilterType.Require) { 

                    // This filter is required.  Check that it exists.  Require filters 
                    // are or-matches.  If any one requirement is satisified, you're fine.
                    //
                    requireCount++;
 
                    foreach(object attrObject in targetFilter) {
                        ToolboxItemFilterAttribute attr2 = attrObject as ToolboxItemFilterAttribute; 
                        if (attr2 == null) { 
                            continue;
                        } 

                        if (attr.Match(attr2)) {
                            requireMatch++;
                            break; 
                        }
                    } 
                } 
                else if (attr.FilterType == ToolboxItemFilterType.Prevent) {
 
                    // This filter should be prevented.  Check that it fails.
                    //
                    foreach(object attrObject in targetFilter) {
                        ToolboxItemFilterAttribute attr2 = attrObject as ToolboxItemFilterAttribute; 
                        if (attr2 == null) {
                            continue; 
                        } 

                        if (attr.Match(attr2)) { 
                            support = FilterSupport.NotSupported;
                            break;
                        }
                    } 
                }
                else if (support != FilterSupport.Custom && attr.FilterType == ToolboxItemFilterType.Custom) { 
                    if (attr.FilterString.Length == 0) { 
                        support = FilterSupport.Custom;
                    } 
                    else {
                        foreach(ToolboxItemFilterAttribute attr2 in targetFilter) {
                            if (attr.FilterString.Equals(attr2.FilterString)) {
                                support = FilterSupport.Custom; 
                                break;
                            } 
                        } 
                    }
                } 
            }


            // Now, configure Supported based on matching require counts 
            //
            if (support != FilterSupport.NotSupported && requireCount > 0 && requireMatch == 0) { 
                support = FilterSupport.NotSupported; 
            }
 
            // Now, do the same thing for the designer side.  Identical check, but from
            // a different perspective.  We also check for the presence of a custom filter
            // here.
            // 
            if (support != FilterSupport.NotSupported) {
 
                requireCount = 0; 
                requireMatch = 0;
 
                foreach(ToolboxItemFilterAttribute attr in targetFilter) {

                    if (support == FilterSupport.NotSupported) {
                        break; 
                    }
 
                    if (attr.FilterType == ToolboxItemFilterType.Require) { 

                        // This filter is required.  Check that it exists.  Require filters 
                        // are or-matches.  If any one requirement is satisified, you're fine.
                        //
                        requireCount++;
 
                        foreach(ToolboxItemFilterAttribute attr2 in itemFilter) {
                            if (attr.Match(attr2)) { 
                                requireMatch++; 
                                break;
                            } 
                        }
                    }
                    else if (attr.FilterType == ToolboxItemFilterType.Prevent) {
 
                        // This filter should be prevented.  Check that it fails.
                        // 
                        foreach(ToolboxItemFilterAttribute attr2 in itemFilter) { 
                            if (attr.Match(attr2)) {
                                support = FilterSupport.NotSupported; 
                                break;
                            }
                        }
                    } 
                    else if (support != FilterSupport.Custom && attr.FilterType == ToolboxItemFilterType.Custom) {
                        if (attr.FilterString.Length == 0) { 
                            support = FilterSupport.Custom; 
                        }
                        else { 
                            foreach(ToolboxItemFilterAttribute attr2 in itemFilter) {
                                if (attr.FilterString.Equals(attr2.FilterString)) {
                                    support = FilterSupport.Custom;
                                    break; 
                                }
                            } 
                        } 
                    }
                } 

                // Now, configure Supported based on matching require counts
                //
                if (support != FilterSupport.NotSupported && requireCount > 0 && requireMatch == 0) { 
                    support = FilterSupport.NotSupported;
                } 
            } 

            return support; 
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.GetItemContainers"]/*' />
        /// <devdoc> 
        ///     Retrieves an IList containing all items on the toolbox.
        ///     The items in the list must be ToolboxItemContainer objects. 
        ///     If the toolbox implementation is organlized in 
        ///     categories, this retrieves a combined list of all
        ///     categories.  The list must be read-write.  New items 
        ///     will be created by calling CreateItem, and then passing
        ///     this newly created item to the Add method of the list.
        /// </devdoc>
        protected abstract IList GetItemContainers(); 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.GetItemContainers1"]/*' /> 
        /// <devdoc> 
        ///     Retrieves an IList containing items on the toolbox
        ///     associated with a particular category.  If the category does 
        ///     not exist this should throw a meaningful exception.
        ///     The items in the list must be ToolboxItemContainer objects.
        ///     The list must be read-write.  New items will be
        ///     created by calling CreateItem, and then passing 
        ///     this newly created item to the Add method of the list.
        /// </devdoc> 
        protected abstract IList GetItemContainers(string categoryName); 

 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.GetToolboxItem"]/*' />
        /// <devdoc>
        ///     Returns a toolbox item associated with the given type, or
        ///     null if the type has no corresponding toolbox item. 
        /// </devdoc>
        public static ToolboxItem GetToolboxItem(Type toolType) { 
            return GetToolboxItem(toolType, false); 
        }
 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.GetToolboxItem1"]/*' />
        /// <devdoc>
        ///     Returns a toolbox item associated with the given type, or 
        ///     null if the type has no corresponding toolbox item.  If nonPublic is true this will search
        ///     for non-public constructors on the type.  If false, constructurs need to be public. 
        /// </devdoc> 
        public static ToolboxItem GetToolboxItem(Type toolType, bool nonPublic) {
 
            ToolboxItem item = null;

            if (toolType == null) {
                throw new ArgumentNullException("toolType"); 
            }
 
            if (((nonPublic || toolType.IsPublic) || toolType.IsNestedPublic) && typeof(IComponent).IsAssignableFrom(toolType) && !toolType.IsAbstract) { 

                // Create a toolbox item for this type, if it is supported 
                //
                ToolboxItemAttribute tba = (ToolboxItemAttribute)TypeDescriptor.GetAttributes(toolType)[typeof(ToolboxItemAttribute)];

                if (!tba.IsDefaultAttribute()) { 
                    Type itemType = tba.ToolboxItemType;
 
                    if (itemType != null) { 

                        // First, try to find a constructor with Type as a parameter.  If that 
                        // fails, try the default constructor.
                        //
                        ConstructorInfo ctor = itemType.GetConstructor(new Type[] {typeof(Type)});
                        if (ctor != null && toolType != null) { 
                            item = (ToolboxItem)ctor.Invoke(new object[] {toolType});
                        } 
                        else { 
                            ctor = itemType.GetConstructor(new Type[0]);
                            if (ctor != null) { 
                                item = (ToolboxItem)ctor.Invoke(new object[0]);
                                item.Initialize(toolType);
                            }
                        } 
                    }
                } 
                else if (!tba.Equals(ToolboxItemAttribute.None) && !toolType.ContainsGenericParameters) { 
                    //the default toolboxitem class does not support generics, but we do not stop anyone from specifying thier own
                    //toolboxitem if they really want to. 
                    //however, most tools in VS will be filtering generics, so this would be an advanced scenario.
                    item = new ToolboxItem(toolType);
                }
            } 
            else if (typeof(ToolboxItem).IsAssignableFrom(toolType)) {
                // if the type *is* a toolboxitem, just create it.. 
                // 
                item = (ToolboxItem)Activator.CreateInstance(toolType, true);
            } 

            return item;
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.GetToolboxItems"]/*' />
        /// <devdoc> 
        ///     Returns a collection containing all the toolbox items in the 
        ///     given assembly.
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")] // Would be a breaking change.
        public static ICollection GetToolboxItems(Assembly a, string newCodeBase)
        {
            return GetToolboxItems(a, newCodeBase, false); 
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.GetToolboxItems"]/*' /> 
        /// <devdoc>
        ///     Returns a collection containing all the toolbox items in the 
        ///     given assembly.
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")] // Would be a breaking change.
        public static ICollection GetToolboxItems(Assembly a, string newCodeBase, bool throwOnError) 
        {
 
            if (a == null) { 
                throw new ArgumentNullException("a");
            } 

            ArrayList items = new ArrayList();

            // For GAC installed assemblies, we want to replace the current 
            // assembly name with the SDK path for the assembly, so we are not
            // relying on specific places within the GAC. 
            // 
            AssemblyName newAssemblyName;
 
            if (a.GlobalAssemblyCache) {
                newAssemblyName = a.GetName();
                newAssemblyName.CodeBase = newCodeBase;
            } 
            else {
                newAssemblyName = null; 
            } 

            try { 
                foreach(Type type in a.GetTypes()) {

                    // only do IComponent things from here...
                    // 
                    if (!typeof(IComponent).IsAssignableFrom(type)) {
                        continue; 
                    } 

                    // Look for compatible constructors 
                    //
                    ConstructorInfo ctor = type.GetConstructor(new Type[0]);
                    if (ctor == null) {
                        ctor = type.GetConstructor(new Type[] {typeof(IContainer)}); 
                    }
 
                    if (ctor == null) { 
                        continue;
                    } 

                    try {
                        ToolboxItem item = GetToolboxItem(type);
 
                        if (item != null) {
 
                            // Now that we have the item, we may need to replace the 
                            // assembly name.
                            // 
                            if (newAssemblyName != null) {
                                item.AssemblyName = newAssemblyName;
                            }
 
                            // Finally, this item needs to go in our list.
                            // 
                            items.Add(item); 
                        }
                    } 
 					catch
					{
                        if (throwOnError) {
                            throw; 
                        }
                        // Nothing here.  If a toolbox item failed we want to continue searching 
                        // the rest of the types. 
                    }
                } 
            }
            catch {
                if (throwOnError) {
                    throw; 
                }
                // Nothing here.  If an assembly is missing dependencies it could throw while 
                // we evaluate.  Eat it and move on. 
            }
 
            return items;
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.GetToolboxItems1"]/*' /> 
        /// <devdoc>
        ///     Returns a collection containing all the toolbox items in 
        ///     the assembly represented by the given assembly name.  This 
        ///     will only momentarially lock the assembly file.
        /// </devdoc> 
        public static ICollection GetToolboxItems(AssemblyName an) {
            return GetToolboxItems(an, false);
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.GetToolboxItems1"]/*' />
        /// <devdoc> 
        ///     Returns a collection containing all the toolbox items in 
        ///     the assembly represented by the given assembly name.  This
        ///     will only momentarially lock the assembly file. 
        /// </devdoc>
        public static ICollection GetToolboxItems(AssemblyName an, bool throwOnError) {

            if (_domainObject == null) { 
                _domain = AppDomain.CreateDomain("Assembly Enumeration Domain");
                _domainObject = (DomainProxyObject)_domain.CreateInstanceAndUnwrap(typeof(DomainProxyObject).Assembly.FullName, typeof(DomainProxyObject).FullName); 
                _domainObjectSponsor = new ClientSponsor(new TimeSpan(0 /* hours */, 5 /* minutes */, 0 /* seconds */)); 
                _domainObjectSponsor.Register(_domainObject);
            } 

            byte[] bytes = _domainObject.GetToolboxItems(an, throwOnError);

            BinaryFormatter f = new BinaryFormatter(); 
            ICollection items = (ICollection)f.Deserialize(new MemoryStream(bytes));
 
            return items; 

        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IsItemContainer"]/*' />
        /// <devdoc>
        ///     Called to perform a quick check to see if the given data object represents 
        ///     a toolbox item.  You may pass in an instance of a designer host if
        ///     you want to include custom toolbox item creators the host provides. 
        ///     Otherwise, this parameter can be null. 
        /// </devdoc>
        protected virtual bool IsItemContainer(IDataObject dataObject, IDesignerHost host) { 

            if (dataObject == null) {
                throw new ArgumentNullException("dataObject");
            } 

            if (ToolboxItemContainer.ContainsFormat(dataObject)) { 
                return true; 
            }
 
            ICollection creators = GetCreatorCollection(host);
            if (creators != null) {
                foreach(ToolboxItemCreator creator in creators) {
                    if (dataObject.GetDataPresent(creator.Format)) { 
                        return true;
                    } 
                } 
            }
 
            return false;
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IsItemContainerSupported"]/*' /> 
        /// <devdoc>
        ///     This is a helper method that can be used if a toolbox item container 
        ///     is supported by the given designer host. 
        /// </devdoc>
        protected bool IsItemContainerSupported(ToolboxItemContainer container, IDesignerHost host) { 

            if (container == null) {
                throw new ArgumentNullException("container");
            } 

            if (host == null) { 
                throw new ArgumentNullException("host"); 
            }
 
            ICollection creators = GetCreatorCollection(host);

            _lastState = host.GetService(typeof(DesignerToolboxInfo)) as DesignerToolboxInfo;
            if (_lastState == null) { 
                _lastState = new DesignerToolboxInfo(this, host);
                host.AddService(typeof(DesignerToolboxInfo), _lastState); 
            } 

            switch(GetFilterSupport(container.GetFilter(creators), _lastState.Filter)) { 
                case FilterSupport.NotSupported:
                    return false;
                case FilterSupport.Supported:
                    return true; 
                case FilterSupport.Custom:
                    if (_lastState.ToolboxUser != null) { 
                        return _lastState.ToolboxUser.GetToolSupported(container.GetToolboxItem(creators)); 
                    }
                    break; 
            }

            return false;
        } 

        /// <devdoc> 
        ///     Called by our DesignerToolboxInfo object to 
        ///     notify us when the external designer state
        ///     (filtering, etc) has changed. 
        /// </devdoc>
        internal void OnDesignerInfoChanged(DesignerToolboxInfo state) {

            // One of the toolbox item state objects changed.  If 
            // it is tied to the currently active designer then
            // we need to raise our FilterChanged event. 
            // 
            if (_designerEventService == null) {
                _designerEventService = state.DesignerHost.GetService(typeof(IDesignerEventService)) as IDesignerEventService; 
            }

            if (_designerEventService != null && _designerEventService.ActiveDesigner == state.DesignerHost) {
                FilterChanged(); 
            }
        } 
 
        /*
 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 

 
 

 



 
*/
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.Refresh"]/*' /> 
        /// <devdoc>
        ///     Called by the toolbox service when an outside ouser 
        ///     has requested that the collection of items should
        ///     be refreshed.  If the collection is always live,
        ///     there is no need to provide any implementation here.
        ///     If the collection returned from GetItemContainers 
        ///     represents a snapshot of the toolbox items, however,
        ///     this method provides a opportunity to update them. 
        /// </devdoc> 
        protected abstract void Refresh();
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.SelectedItemContainerUsed"]/*' />
        /// <devdoc>
        ///     Called by the toolbox service when an outside
        ///     user has reported that he/she has used the 
        ///     selected toolbox item.  The default behavior is
        ///     to set the selected item to null. 
        /// </devdoc> 
    	protected virtual void SelectedItemContainerUsed() {
            SelectedItemContainer = null; 
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.SetCursor"]/*' />
        /// <devdoc> 
        ///     Called by the toolbox service when an outside
        ///     user has asked to set the cursor for the currently 
        ///     selected toolbox item.  The default implementation 
        ///     sets the cursor to a crosshair and returns true
        ///     if there is a toolbox item selected.  It returns 
        ///     false if no item is selected.
        /// </devdoc>
        protected virtual bool SetCursor() {
            if (SelectedItemContainer != null) { 
                Cursor.Current = Cursors.Cross;
                return true; 
            } 

            return false; 
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.UnloadToolboxItems"]/*' />
        /// <devdoc> 
        ///     This unloads any assemblies that were locked
        ///     as a result of calling GetToolboxItems. 
        /// </devdoc> 
        public static void UnloadToolboxItems() {
 
            // We are now done with the domain, so release it.
            //
            if (_domain != null) {
                AppDomain deadDomain = _domain; 
                _domainObjectSponsor.Close();
                _domainObjectSponsor = null; 
                _domainObject = null; 
                _domain = null;
                AppDomain.Unload(deadDomain); 
            }
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.CategoryNames"]/*' /> 
        /// <devdoc>
        ///     Gets the names of all the tool categories currently on the toolbox. 
        /// </devdoc> 
        CategoryNameCollection IToolboxService.CategoryNames {
            get { 
                return CategoryNames;
            }
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.SelectedCategory"]/*' />
        /// <devdoc> 
        ///     Gets the name of the currently selected tool category from the toolbox. 
        /// </devdoc>
        string IToolboxService.SelectedCategory { 
            get {
                return SelectedCategory;
            }
            set { 
                SelectedCategory = value;
            } 
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.AddCreator"]/*' /> 
        /// <devdoc>
        ///     Adds a new toolbox item creator.
        /// </devdoc>
        void IToolboxService.AddCreator(ToolboxItemCreatorCallback creator, string format) { 

            if (creator == null) { 
                throw new ArgumentNullException("creator"); 
            }
 
            if (format == null) {
                throw new ArgumentNullException("format");
            }
 
            if (_globalCreators == null) {
                _globalCreators = new ArrayList(); 
            } 

            _globalCreators.Add(new ToolboxItemCreator(creator, format)); 

            // We now need to re-query because the list has changed.
            //
            _lastMergedHost = null; 
            _lastMergedCreators = null;
        } 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.AddCreator1"]/*' />
        /// <devdoc> 
        ///     Adds a new toolbox item creator.
        /// </devdoc>
        void IToolboxService.AddCreator(ToolboxItemCreatorCallback creator, string format, IDesignerHost host) {
 
            if (creator == null) {
                throw new ArgumentNullException("creator"); 
            } 

            if (format == null) { 
                throw new ArgumentNullException("format");
            }

            if (host == null) { 
                throw new ArgumentNullException("host");
            } 
 
            if (_designerCreators == null) {
                _designerCreators = new Hashtable(); 
            }

            ArrayList list = _designerCreators[host] as ArrayList;
            if (list == null) { 
                list = new ArrayList(4);
                _designerCreators[host] = list; 
            } 

            list.Add(new ToolboxItemCreator(creator, format)); 

            // We now need to re-query because the list has changed.
            //
            _lastMergedHost = null; 
            _lastMergedCreators = null;
        } 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.AddLinkedToolboxItem"]/*' />
        /// <devdoc> 
        ///     Adds a new tool to the toolbox under the default category.
        /// </devdoc>
        void IToolboxService.AddLinkedToolboxItem(ToolboxItem toolboxItem, IDesignerHost host) {
 
            if (toolboxItem == null) {
                throw new ArgumentNullException("toolboxItem"); 
            } 

            if (host == null) { 
                throw new ArgumentNullException("host");
            }

            ToolboxItemContainer item = CreateItemContainer(toolboxItem, host); 

            // Item can be null if this service doesn't support linking. 
            // 
            if (item != null) {
                GetItemContainers(SelectedCategory).Add(item); 
            }
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.AddLinkedToolboxItem1"]/*' /> 
        /// <devdoc>
        ///     Adds a new tool to the toolbox under the specified category. 
        /// </devdoc> 
        void IToolboxService.AddLinkedToolboxItem(ToolboxItem toolboxItem, string category, IDesignerHost host) {
 
            if (toolboxItem == null) {
                throw new ArgumentNullException("toolboxItem");
            }
 
            if (category == null) {
                throw new ArgumentNullException("category"); 
            } 

            if (host == null) { 
                throw new ArgumentNullException("host");
            }

            ToolboxItemContainer item = CreateItemContainer(toolboxItem, host); 

            // Item can be null if this service doesn't support linking. 
            // 
            if (item != null) {
                GetItemContainers(category).Add(item); 
            }
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.AddLinkedToolboxItem2"]/*' /> 
        /// <devdoc>
        ///     Adds a new tool to the toolbox under the default category. 
        /// </devdoc> 
        void IToolboxService.AddToolboxItem(ToolboxItem toolboxItem) {
 
            if (toolboxItem == null) {
                throw new ArgumentNullException("toolboxItem");
            }
 
            ToolboxItemContainer item = CreateItemContainer(toolboxItem, null);
 
            if (item != null) { 
                GetItemContainers(SelectedCategory).Add(item);
            } 
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.AddLinkedToolboxItem3"]/*' />
        /// <devdoc> 
        ///     Adds a new tool to the toolbox under the specified category.
        /// </devdoc> 
        void IToolboxService.AddToolboxItem(ToolboxItem toolboxItem, string category) { 

            if (toolboxItem == null) { 
                throw new ArgumentNullException("toolboxItem");
            }

            if (category == null) { 
                throw new ArgumentNullException("category");
            } 
 
            ToolboxItemContainer item = CreateItemContainer(toolboxItem, null);
 
            if (item != null) {
                GetItemContainers(category).Add(item);
            }
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.DeserializeToolboxItem"]/*' /> 
        /// <devdoc> 
        ///     Gets a toolbox item from a previously serialized object.
        /// </devdoc> 
        ToolboxItem IToolboxService.DeserializeToolboxItem(object serializedObject) {

            if (serializedObject == null) {
                throw new ArgumentNullException("serializedObject"); 
            }
 
            IDataObject dataObject = serializedObject as IDataObject; 
            if (dataObject == null) {
                dataObject = new DataObject(serializedObject); 
            }

            ToolboxItemContainer container = CreateItemContainer(dataObject);
            if (container != null) { 
                return container.GetToolboxItem(GetCreatorCollection(null));
            } 
 
            return null;
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.DeserializeToolboxItem1"]/*' />
        /// <devdoc>
        ///     Gets a toolbox item from a previously serialized object. 
        /// </devdoc>
        ToolboxItem IToolboxService.DeserializeToolboxItem(object serializedObject, IDesignerHost host) { 
 
            if (serializedObject == null) {
                throw new ArgumentNullException("serializedObject"); 
            }

            if (host == null) {
                throw new ArgumentNullException("host"); 
            }
 
            IDataObject dataObject = serializedObject as IDataObject; 
            if (dataObject == null) {
                dataObject = new DataObject(serializedObject); 
            }

            ToolboxItemContainer container = CreateItemContainer(dataObject);
            if (container != null) { 
                return container.GetToolboxItem(GetCreatorCollection(host));
            } 
 
            return null;
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.GetSelectedToolboxItem"]/*' />
        /// <devdoc>
        ///     Gets the currently selected tool. 
        /// </devdoc>
        ToolboxItem IToolboxService.GetSelectedToolboxItem() { 
            ToolboxItemContainer container = SelectedItemContainer; 
            if (container != null) {
                return container.GetToolboxItem(GetCreatorCollection(null)); 
            }
            return null;
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.GetSelectedToolboxItem1"]/*' />
        /// <devdoc> 
        ///     Gets the currently selected tool. 
        /// </devdoc>
        ToolboxItem IToolboxService.GetSelectedToolboxItem(IDesignerHost host) { 

            if (host == null) {
                throw new ArgumentNullException("host");
            } 

            ToolboxItemContainer container = SelectedItemContainer; 
            if (container != null) { 
                return container.GetToolboxItem(GetCreatorCollection(host));
            } 
            return null;
        }

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.GetToolboxItems"]/*' /> 
        /// <devdoc>
        ///     Gets all .NET Framework tools on the toolbox. 
        /// </devdoc> 
        ToolboxItemCollection IToolboxService.GetToolboxItems() {
            IList itemContainers = GetItemContainers(); 
            ArrayList items = new ArrayList(itemContainers.Count);
            ICollection creators = GetCreatorCollection(null);
            foreach(ToolboxItemContainer container in itemContainers) {
                ToolboxItem item = container.GetToolboxItem(creators); 
                if (item != null) {
                    items.Add(item); 
                } 
            }
            ToolboxItem[] itemArray = new ToolboxItem[items.Count]; 
            items.CopyTo(itemArray, 0);
            return new ToolboxItemCollection(itemArray);
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.GetToolboxItems1"]/*' />
        /// <devdoc> 
        ///     Gets all .NET Framework tools on the toolbox. 
        /// </devdoc>
        ToolboxItemCollection IToolboxService.GetToolboxItems(IDesignerHost host) { 

            if (host == null) {
                throw new ArgumentNullException("host");
            } 

            IList itemContainers = GetItemContainers(); 
            ArrayList items = new ArrayList(itemContainers.Count); 
            ICollection creators = GetCreatorCollection(host);
            foreach(ToolboxItemContainer container in itemContainers) { 
                ToolboxItem item = container.GetToolboxItem(creators);
                if (item != null) {
                    items.Add(item);
                } 
            }
            ToolboxItem[] itemArray = new ToolboxItem[items.Count]; 
            items.CopyTo(itemArray, 0); 
            return new ToolboxItemCollection(itemArray);
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.GetToolboxItems2"]/*' />
        /// <devdoc>
        ///     Gets all .NET Framework tools on the specified toolbox category. 
        /// </devdoc>
        ToolboxItemCollection IToolboxService.GetToolboxItems(String category) { 
 
            if (category == null) {
                throw new ArgumentNullException("category"); 
            }

            IList itemContainers = GetItemContainers(category);
            ArrayList items = new ArrayList(itemContainers.Count); 
            ICollection creators = GetCreatorCollection(null);
            foreach(ToolboxItemContainer container in itemContainers) { 
                ToolboxItem item = container.GetToolboxItem(creators); 
                if (item != null) {
                    items.Add(item); 
                }
            }
            ToolboxItem[] itemArray = new ToolboxItem[items.Count];
            items.CopyTo(itemArray, 0); 
            return new ToolboxItemCollection(itemArray);
        } 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.GetToolboxItems3"]/*' />
        /// <devdoc> 
        ///     Gets all .NET Framework tools on the specified toolbox category.
        /// </devdoc>
        ToolboxItemCollection IToolboxService.GetToolboxItems(String category, IDesignerHost host) {
 
            if (category == null) {
                throw new ArgumentNullException("category"); 
            } 

            if (host == null) { 
                throw new ArgumentNullException("host");
            }

            IList itemContainers = GetItemContainers(category); 
            ArrayList items = new ArrayList(itemContainers.Count);
            ICollection creators = GetCreatorCollection(host); 
            foreach(ToolboxItemContainer container in itemContainers) { 
                ToolboxItem item = container.GetToolboxItem(creators);
                if (item != null) { 
                    items.Add(item);
                }
            }
            ToolboxItem[] itemArray = new ToolboxItem[items.Count]; 
            items.CopyTo(itemArray, 0);
            return new ToolboxItemCollection(itemArray); 
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.IsSupported"]/*' /> 
        /// <devdoc>
        ///     Determines if the given designer host contains a designer that supports the serialized
        ///     toolbox item.  This will return false if the designer doesn't support the item, or if the
        ///     serializedObject parameter does not contain a toolbox item. 
        /// </devdoc>
        bool IToolboxService.IsSupported(object serializedObject, IDesignerHost host) { 
 
            if (serializedObject == null) {
                throw new ArgumentNullException("serializedObject"); 
            }

            if (host == null) {
                throw new ArgumentNullException("host"); 
            }
 
            IDataObject dataObject = serializedObject as IDataObject; 
            if (dataObject == null) {
                dataObject = new DataObject(serializedObject); 
            }

            // First, is this even a valid serialized object?
            // 
            if (!IsItemContainer(dataObject, host)) {
                return false; 
            } 

            ToolboxItemContainer container = CreateItemContainer(dataObject); 


            // Second, identify the filter that the host is using.  If
            // the filter matches, then we are OK. 
            //
            return IsItemContainerSupported(container, host); 
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.IsSupported1"]/*' /> 
        /// <devdoc>
        ///     Determines if the serialized toolbox item contains a matching collection of filter attributes.
        ///     This will return false if the serializedObject parameter doesn't contain a toolbox item,
        ///     or if the collection of filter attributes does not match. 
        /// </devdoc>
        bool IToolboxService.IsSupported(object serializedObject, ICollection filterAttributes) { 
 
            if (serializedObject == null) {
                throw new ArgumentNullException("serializedObject"); 
            }

            if (filterAttributes == null) {
                throw new ArgumentNullException("filterAttributes"); 
            }
 
            IDataObject dataObject = serializedObject as IDataObject; 
            if (dataObject == null) {
                dataObject = new DataObject(serializedObject); 
            }

            // First, is this even a valid serialized object?
            // 
            if (!IsItemContainer(dataObject, null)) {
                return false; 
            } 

            ToolboxItemContainer container = CreateItemContainer(dataObject); 

            return GetFilterSupport(container.GetFilter(GetCreatorCollection(null)), filterAttributes) == FilterSupport.Supported;
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.IsToolboxItem"]/*' />
        /// <devdoc> 
        ///     Gets a value indicating whether the specified object contains a serialized toolbox item. 
        /// </devdoc>
        bool IToolboxService.IsToolboxItem(object serializedObject) { 

            if (serializedObject == null) {
                throw new ArgumentNullException("serializedObject");
            } 

            IDataObject dataObject = serializedObject as IDataObject; 
            if (dataObject == null) { 
                dataObject = new DataObject(serializedObject);
            } 

            return IsItemContainer(dataObject, null);
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.IsToolboxItem1"]/*' />
        /// <devdoc> 
        ///     Gets a value indicating whether the specified object contains a serialized toolbox item. 
        /// </devdoc>
        bool IToolboxService.IsToolboxItem(object serializedObject, IDesignerHost host) { 

            if (serializedObject == null) {
                throw new ArgumentNullException("serializedObject");
            } 

            if (host == null) { 
                throw new ArgumentNullException("host"); 
            }
 
            IDataObject dataObject = serializedObject as IDataObject;
            if (dataObject == null) {
                dataObject = new DataObject(serializedObject);
            } 

            return IsItemContainer(dataObject, host); 
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.Refresh"]/*' /> 
        /// <devdoc>
        ///     Refreshes the state of the toolbox items.
        /// </devdoc>
        void IToolboxService.Refresh() { 
            Refresh();
        } 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.RemoveCreator"]/*' />
        /// <devdoc> 
        ///     Removes a previously added toolbox creator.
        /// </devdoc>
        void IToolboxService.RemoveCreator(string format) {
 
            if (format == null) {
                throw new ArgumentNullException("format"); 
            } 

            if (_globalCreators != null) { 
                for (int i = 0; i < _globalCreators.Count; i++) {
                    ToolboxItemCreator creator = _globalCreators[i] as ToolboxItemCreator;
                    if (creator.Format.Equals(format)) {
                        _globalCreators.RemoveAt(i); 

                        // We now need to re-query because the list has changed. 
                        // 
                        _lastMergedHost = null;
                        _lastMergedCreators = null; 

                        return;
                    }
                } 
            }
        } 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.RemoveCreator1"]/*' />
        /// <devdoc> 
        ///     Removes a previously added toolbox creator.
        /// </devdoc>
        void IToolboxService.RemoveCreator(string format, IDesignerHost host) {
 
            if (format == null) {
                throw new ArgumentNullException("format"); 
            } 

            if (host == null) { 
                throw new ArgumentNullException("host");
            }

            if (_designerCreators != null) { 
                ArrayList list = _designerCreators[host] as ArrayList;
                if (list != null) { 
                    for (int i = 0; i < list.Count; i++) { 
                        ToolboxItemCreator creator = list[i] as ToolboxItemCreator;
                        if (creator.Format.Equals(format)) { 
                            list.RemoveAt(i);

                            // We now need to re-query because the list has changed.
                            // 
                            _lastMergedHost = null;
                            _lastMergedCreators = null; 
 
                            return;
                        } 
                    }
                }
            }
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.RemoveToolboxItem"]/*' /> 
        /// <devdoc> 
        ///     Removes the specified tool from the toolbox.
        /// </devdoc> 
        void IToolboxService.RemoveToolboxItem(ToolboxItem toolboxItem) {

            if (toolboxItem == null) {
                throw new ArgumentNullException("toolboxItem"); 
            }
 
            GetItemContainers().Remove(CreateItemContainer(toolboxItem, null)); 
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.RemoveToolboxItem1"]/*' />
        /// <devdoc>
        ///     Removes the specified tool from the toolbox.
        /// </devdoc> 
        void IToolboxService.RemoveToolboxItem(ToolboxItem toolboxItem, string category) {
 
            if (toolboxItem == null) { 
                throw new ArgumentNullException("toolboxItem");
            } 

            if (category == null) {
                throw new ArgumentNullException("category");
            } 

            GetItemContainers(category).Remove(CreateItemContainer(toolboxItem, null)); 
 
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.SelectedToolboxItemUsed"]/*' />
        /// <devdoc>
        ///     Notifies the toolbox that the selected tool has been used.
        /// </devdoc> 
        void IToolboxService.SelectedToolboxItemUsed() {
            SelectedItemContainerUsed(); 
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.SerializeToolboxItem"]/*' /> 
        /// <devdoc>
        ///     Takes the given toolbox item and serializes it to a persistent object.  This object can then
        ///     be stored in a stream or passed around in a drag and drop or clipboard operation.
        /// </devdoc> 
        object IToolboxService.SerializeToolboxItem(ToolboxItem toolboxItem) {
 
            if (toolboxItem == null) { 
                throw new ArgumentNullException("toolboxItem");
            } 

            return CreateItemContainer(toolboxItem, null).ToolboxData;
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.SetCursor"]/*' />
        /// <devdoc> 
        ///     Sets the current application's cursor to a cursor that represents the 
        ///     currently selected tool.
        /// </devdoc> 
        bool IToolboxService.SetCursor() {
            return SetCursor();
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IToolboxService.SetSelectedToolboxItem"]/*' />
        /// <devdoc> 
        ///     Sets the currently selected tool in the toolbox. 
        /// </devdoc>
        void IToolboxService.SetSelectedToolboxItem(ToolboxItem toolboxItem) { 
            if (toolboxItem != null) {
                SelectedItemContainer = CreateItemContainer(toolboxItem, null);
            }
            else { 
                SelectedItemContainer = null;
            } 
        } 

 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.IComponentDiscoveryService.GetComponentTypes"]/*' />
        ICollection IComponentDiscoveryService.GetComponentTypes(IDesignerHost designerHost, Type baseType) {
            Hashtable types = new Hashtable();
 
            ToolboxItemCollection items = ((IToolboxService)this).GetToolboxItems();
            if (items != null) { 
                Type componentType = typeof(IComponent); 
                foreach (ToolboxItem item in items) {
                    Type t = item.GetType(designerHost); 
                    if (t != null) {
                        if (componentType.IsAssignableFrom(t) == false) {
                            continue;
                        } 
                        if ((baseType != null) && (baseType.IsAssignableFrom(t) == false)) {
                            continue; 
                        } 
                        types[t] = t;
                    } 
                }
            }

            return types.Values; 
        }
 
        /// <devdoc> 
        ///     Proxy object to allow us to do cross-domain calls.
        /// </devdoc> 
        private class DomainProxyObject : MarshalByRefObject {

            // [....] : Changed this from a stream to a byte[].  Remoting bug
            // VSWhidbey 90430 causes streams to be marshaled incorrectly across the boundary. 
            //
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods")] 
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:AvoidUnusedParameters")] 
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
            internal byte[] GetToolboxItems(AssemblyName an, bool throwOnError) { 

                // Load the assembly here.  We are running in a different
                // app domain, so we can load.  When we're finished and
                // we return, the caller will unload the domain and free 
                // the file.
                // 
                Assembly assembly = null; 

                try { 
                    assembly = Assembly.Load(an);
                }
                catch (FileNotFoundException) {
                } 
                catch (BadImageFormatException) {
                } 
                catch (IOException) { 
                }
 
                if (assembly == null && an.CodeBase != null) {
                    assembly = Assembly.LoadFrom(new System.Uri(an.CodeBase).LocalPath);
                }
 
                if (assembly == null) {
                    throw new ArgumentException(SR.GetString(SR.ToolboxServiceAssemblyNotFound, an.FullName)); 
                } 

                ICollection items = null; 

                try {
                    items = ToolboxService.GetToolboxItems(assembly, null, throwOnError);
                } 
                catch (Exception e) {
                    //we have to convert the exception if its a ReflectionTypeLoadException 
                    ReflectionTypeLoadException typeloadex = e as ReflectionTypeLoadException; 
                    if (typeloadex != null) {
                        //remove the types so we don't try to load them when going to the main domain. 
                        throw new ReflectionTypeLoadException(null, typeloadex.LoaderExceptions, typeloadex.Message);
                    }
                    //otherwise, we can just throw the original exception.
                    throw; 
                }
 
                BinaryFormatter formatter = new BinaryFormatter(); 
                MemoryStream stream = new MemoryStream();
 
                formatter.Serialize(stream, items);
                stream.Close();
                return stream.GetBuffer();
            } 
        }
 
        /// <devdoc> 
        ///     Private enum that identifies the level of filter
        ///     support. 
        /// </devdoc>
        private enum FilterSupport {
            NotSupported,
            Supported, 
            Custom
        } 
 
    }
 
    /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.ToolboxItemCreator"]/*' />
    /// <devdoc>
    ///     The ToolboxItemCreator class encapsulates toolbox
    ///     item crator callbacks.  These callbacks are used by 
    ///     designers to provide ways to create toolbox items
    ///     for custom data objects. 
    /// </devdoc> 
    public sealed class ToolboxItemCreator {
 
        private ToolboxItemCreatorCallback _callback;
        private string _format;

        internal ToolboxItemCreator(ToolboxItemCreatorCallback callback, string format) { 
            _callback = callback;
            _format = format; 
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.ToolboxItemCreator.Create"]/*' /> 
        /// <devdoc>
        ///     Creates a new toolbox item given a
        ///     data object.  This may raise an exception
        ///     if the data object does not contain 
        ///     data for the supported format.
        /// </devdoc> 
        public ToolboxItem Create(IDataObject data) { 
            return _callback(data, _format);
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxService.ToolboxItemCreator.Format"]/*' />
        /// <devdoc>
        ///     The data format that this toolbox item 
        ///     creator supports.
        /// </devdoc> 
        public string Format { 
            get {
                return _format; 
            }
        }
    }
 
    /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer"]/*' />
    /// <devdoc> 
    ///     The ToolboxItemContainer class contains a toolbox 
    ///     item and can convert a toolbox item to a data object
    ///     and back. 
    /// </devdoc>
    [Serializable]
    public class ToolboxItemContainer : ISerializable {
 
        private const string _localClipboardFormat = "CF_TOOLBOXITEMCONTAINER";
        private const string _itemClipboardFormat = "CF_TOOLBOXITEMCONTAINER_CONTENTS"; 
        private const string _hashClipboardFormat = "CF_TOOLBOXITEMCONTAINER_HASH"; 
        private const string _serializationFormats = "TbxIC_DataObjectFormats";
        private const string _serializationValues = "TbxIC_DataObjectValues"; 
        private const short _clipboardVersion = 1;

        private int _hashCode;
        private ToolboxItem _toolboxItem; 
        private IDataObject _dataObject;
        private ICollection _filter; 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.ToolboxItemContainer"]/*' />
        /// <devdoc> 
        ///     Serialization constructor.  ToolboxItemContainers
        ///     can be serialized.  Note that generally it is not
        ///     necessary to override the serialization mechanism
        ///     for a toolbox item container.  Toolbox item 
        ///     containers implement serialization by saving the
        ///     IDataObject returned from ToolboxData, so when 
        ///     overriding ToolboxData and providing your own 
        ///     custom data this data will be included with
        ///     the default ISerializable implementation.  You 
        ///     would want to override the default serialization
        ///     implementation only if you intend to store
        ///     private details about this toolbox item
        ///     container that should not be exposed through 
        ///     the public data object.
        /// </devdoc> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:AvoidUnusedParameters")] 
        protected ToolboxItemContainer(SerializationInfo info, StreamingContext context) {
            string[] formats = (string[])info.GetValue(_serializationFormats, typeof(string[])); 
            object[] data = (object[])info.GetValue(_serializationValues, typeof(object[]));

            Debug.Assert(formats.Length == data.Length, "Array mismatch in serialization");
 
            DataObject d = new DataObject();
 
            for (int i = 0; i < formats.Length; i++) { 
                d.SetData(formats[i], data[i]);
            } 

            _dataObject = d;
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.ToolboxItemContainer1"]/*' />
        /// <devdoc> 
        ///     Creates a new ToolboxItemContainer object from 
        ///     a toolbox item.
        /// </devdoc> 
        public ToolboxItemContainer(ToolboxItem item) {
            if (item == null) {
                throw new ArgumentNullException("item");
            } 
            _toolboxItem = item;
            UpdateFilter(item); 
 
            _hashCode = item.DisplayName.GetHashCode();
            if (item.AssemblyName != null) { 
                _hashCode ^= item.AssemblyName.GetHashCode();
            }
            if (item.TypeName != null) {
                _hashCode ^= item.TypeName.GetHashCode(); 
            }
            if (_hashCode == 0) { 
                _hashCode = item.DisplayName.GetHashCode(); 
            }
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.ToolboxItemContainer2"]/*' />
        /// <devdoc>
        ///     Creates a new ToolboxItemContainer object from an 
        ///     IDataObject.  The data object can contain
        ///     data provided by the ToolboxItemContainer class, or it 
        ///     can contain data that can be read by one 
        ///     of the toolbox item creators that have
        ///     been supplied by the user. 
        /// </devdoc>
        public ToolboxItemContainer(IDataObject data) {
            if (data == null) {
                throw new ArgumentNullException("data"); 
            }
            _dataObject = data; 
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.IsCreated"]/*' /> 
        /// <devdoc>
        ///     Returns true if the underlying toolbox item has been deserialized.
        /// </devdoc>
        public bool IsCreated { 
            get {
                return (_toolboxItem != null); 
            } 
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.IsTransient"]/*' />
        /// <devdoc>
        ///     Returns true if the toolbox item contained in this
        ///     container has been marked as transient. 
        /// </devdoc>
        public bool IsTransient { 
            get { 
                if (_toolboxItem != null) {
                    return _toolboxItem.IsTransient; 
                }

                // If the toolbox item is already persisted, then
                // it must not have been transient. 
                return false;
            } 
        } 

 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.ToolboxData"]/*' />
        /// <devdoc>
        ///     The serialized version of the toolbox item.
        ///     This data object can be used by an application 
        ///     to store this toolbox item.  This data object
        ///     will be fabricated from the toolbox item if 
        ///     necessary.  Implementors may override this to 
        ///     provide additional storage information in the
        ///     data object. 
        /// </devdoc>
        public virtual IDataObject ToolboxData {
            get {
                if (_dataObject == null) { 
                    MemoryStream stream = new MemoryStream();
                    DataObject d = new DataObject(); 
 
                    BinaryWriter writer = new BinaryWriter(stream);
                    writer.Write(_clipboardVersion); 
                    writer.Write((short)_filter.Count);
                    foreach (ToolboxItemFilterAttribute attr in _filter) {
                        writer.Write(attr.FilterString);
                        writer.Write((short)attr.FilterType); 
                    }
                    writer.Flush(); 
                    stream.Close(); 
                    d.SetData(_localClipboardFormat, stream.GetBuffer());
                    d.SetData(_hashClipboardFormat, _hashCode); 

                    // Now it's time for the toolbox item itself.  We want
                    // to defer actually serializing the toolbox item until
                    // we need to save this to disk, but we want to ensure that 
                    // when we load it up at a later date we can recover the item
                    // if it in a non-gac assembly.  So we have a wrapper object 
                    // that implements ISerializable to handle this for us. 
                    //
                    d.SetData(_itemClipboardFormat, new ToolboxItemSerializer(_toolboxItem)); 
                    _dataObject = d;
                }

                return _dataObject; 
            }
        } 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.UpdateFilter"]/*' />
        /// <devdoc> 
        /// This method is called to update the containers filter with the filter from the item.
        /// This should be called when the toolbox item was modified (or configured) or if a new TypeDescriptionProvider was
        /// added such that the filter will be changed.
        /// </devdoc> 
        public void UpdateFilter(ToolboxItem item) {
            _filter = MergeFilter(item); 
        } 

        /// <devdoc> 
        ///     This will determine if the data object
        ///     contains our clipboard format.
        /// </devdoc>
        internal static bool ContainsFormat(IDataObject dataObject) { 
            return dataObject.GetDataPresent(_localClipboardFormat);
        } 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.Equals"]/*' />
        /// <devdoc> 
        ///     Equals override for this class
        /// </devdoc>
        public override bool Equals(object obj) {
            ToolboxItemContainer them = obj as ToolboxItemContainer; 

            if (them == this) { 
                return true; 
            }
 
            if (them == null) {
                return false;
            }
 
            if (this._toolboxItem != null && them._toolboxItem != null && this._toolboxItem.Equals(them._toolboxItem)) {
                return true; 
            } 

            if (this._dataObject != null && them._dataObject != null && this._dataObject.Equals(them._dataObject)) { 
                return true;
            }

            ToolboxItem ourItem = GetToolboxItem(null); 
            ToolboxItem theirItem = them.GetToolboxItem(null);
 
            if (ourItem != null && theirItem != null) { 
                return ourItem.Equals(theirItem);
            } 

            return false;
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.GetFilter"]/*' />
        /// <devdoc> 
        ///     The types stored in a toolbox item may have 
        ///     a filter associated with them.  Filters can be
        ///     used to restrict the tools that can be placed 
        ///     on designers. The objects in this collection are
        ///     all instances of ToolboxItemFilterAttribute.
        /// </devdoc>
        public virtual ICollection GetFilter(ICollection creators) { 

            ICollection filter = _filter; 
 
            if (_filter == null) {
 
                if (_dataObject.GetDataPresent(_localClipboardFormat)) {

                    // Our own private format is in the data object.
                    // This format contains filter data, so we just 
                    // need to pull it out.
 
                    byte[] bytes = (byte[])_dataObject.GetData(_localClipboardFormat); 
                    if (bytes != null) {
                        // If the stream is null, that could mean that we could not extract it.  This is essentially 
                        // a "dead" toolbox item
                        BinaryReader reader = new BinaryReader(new MemoryStream(bytes));
                        short version = reader.ReadInt16();
 
                        if (version != _clipboardVersion) {
                            Debug.Fail("Toolbox item version mismatch.  Toolbox database contains version " + version.ToString(CultureInfo.InvariantCulture) + " and current codebase expects version " + _clipboardVersion.ToString(CultureInfo.InvariantCulture)); 
                            _filter = new ToolboxItemFilterAttribute[0]; 
                        }
                        else { 
                            short filterCount = reader.ReadInt16();
                            ToolboxItemFilterAttribute[] filterArray = new ToolboxItemFilterAttribute[filterCount];

                            for (short i = 0; i < filterCount; i++) { 
                                string filterName = reader.ReadString();
                                short filterValue = reader.ReadInt16(); 
 
                                filterArray[i] = new ToolboxItemFilterAttribute(filterName, (ToolboxItemFilterType)filterValue);
                            } 

                            _filter = filterArray;
                        }
                    } 
                    else {
                        _filter = new ToolboxItemFilterAttribute[0]; 
                    } 

                    filter = _filter; 
                }
                else {

                    // We don't recognize the format of this data object. 
                    // Ask one or more creators if it does.
 
                    if (creators != null) { 
                        foreach (ToolboxItemCreator creator in creators) {
                            if (_dataObject.GetDataPresent(creator.Format)) { 
                                ToolboxItem item = creator.Create(_dataObject);
                                if (item != null) {
                                    filter = MergeFilter(item);
                                    break; 
                                }
                            } 
                        } 
                    }
                } 
            }

            return filter;
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.GetHashCode"]/*' /> 
        /// <devdoc> 
        ///     Override of hash code
        /// </devdoc> 
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
		public override int GetHashCode() {

            // Recover the hash code we saved into the data object. 
            if (_hashCode == 0) {
                if (_dataObject != null && _dataObject.GetDataPresent(_hashClipboardFormat)) { 
                    _hashCode = (int)_dataObject.GetData(_hashClipboardFormat); 
                }
            } 

            // If we failed, hash against our object identity.
            if (_hashCode == 0) {
                _hashCode = base.GetHashCode(); 
            }
 
            return _hashCode; 
        }
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.GetObjectData"]/*' />
        /// <devdoc>
        ///     Protected implementation of ISerializable.
        ///     ToolboxItemContainers can be serialized. 
        ///     Note that generally it is not necessary to
        ///     override the serialization mechanism 
        ///     for a toolbox item container.  Toolbox item 
        ///     containers implement serialization by saving the
        ///     IDataObject returned from ToolboxData, so when 
        ///     overriding ToolboxData and providing your own
        ///     custom data this data will be included with
        ///     the default ISerializable implementation.  You
        ///     would want to override the default serialization 
        ///     implementation only if you intend to store
        ///     private details about this toolbox item 
        ///     container that should not be exposed through 
        ///     the public data object.
        /// </devdoc> 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)]
        protected virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
            IDataObject d = ToolboxData;
 
            string[] formats = d.GetFormats();
            object[] data = new object[formats.Length]; 
 
            for (int i = 0; i < data.Length; i++) {
                data[i] = d.GetData(formats[i]); 
                Debug.Assert(data[i] != null, "Data format contained within toolbox " + formats[i] + " does not implement serializable data?");
            }

            info.AddValue(_serializationFormats, formats); 
            info.AddValue(_serializationValues, data);
        } 
 
        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.GetToolboxItem"]/*' />
        /// <devdoc> 
        ///     This will retrieve the toolbox item stored in
        ///     this ToolboxItemContainer class.  The creators
        ///     argument supplies a collection of toolbox item
        ///     creators that can be used in case the container 
        ///     is unable to create the toolbox item itself.
        ///     Iimplementors may override this to provide custom 
        ///     deserialization of the data object. 
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")] 
        public virtual ToolboxItem GetToolboxItem(ICollection creators) {

            ToolboxItem item = _toolboxItem;
 
            if (_toolboxItem == null) {
                Debug.Assert(_dataObject != null, "Should always have a data object"); 
 
                if (_dataObject.GetDataPresent(_itemClipboardFormat)) {
 

                    // Our own private data format is in the data object.
                    // This format corresponds directly to a toolbox item,
                    // so all we need to do is deserialize it. 

                    string exceptionString = null; 
 
                    try {
                        ToolboxItemSerializer s = (ToolboxItemSerializer)_dataObject.GetData(_itemClipboardFormat); 
                        _toolboxItem = s.ToolboxItem;
                    }
                    catch (Exception ex) {
                        exceptionString = ex.Message; 
                    }
                    catch { 
                        Debug.Fail("What is this non CLS exception doing here?!?"); 
                    }
 
                    if (_toolboxItem == null) {
                        Debug.Fail("Toolbox item is either out of date or bogus.  We were unable to convert the data object to an item.  Does your toolbox need to be reset?");
                        _toolboxItem = new BrokenToolboxItem(exceptionString);
                    } 

                    item = _toolboxItem; 
                } 
                else {
 
                    // This is an unknown format.  Ask our creator collection if
                    // any of them support the format.  If they do, ask them to
                    // create a toolbox item.  Because the list of creators
                    // depends on the active designer, we do not stash the 
                    // toolbox item in our member variable here.  It is
                    // considered to be transient. 
 
                    if (creators != null) {
                        foreach (ToolboxItemCreator creator in creators) { 
                            if (_dataObject.GetDataPresent(creator.Format)) {
                                item = creator.Create(_dataObject);
                                if (item != null) {
                                    break; 
                                }
                            } 
                        } 
                    }
                } 
            }

            return item;
        } 

        /// <devdoc> 
        ///     This is a helper method that merges the filter from the 
        ///     toolbox item along with filter attributes on the item itself.
        /// </devdoc> 
        private static ICollection MergeFilter(ToolboxItem item) {

            ICollection existingFilter = item.Filter;
            ArrayList itemFilter = new ArrayList(); 

            foreach (Attribute a in TypeDescriptor.GetAttributes(item)) { 
                if (a is ToolboxItemFilterAttribute) { 
                    itemFilter.Add(a);
                } 
            }

            ICollection finalFilter;
 
            if (existingFilter == null || existingFilter.Count == 0) {
                finalFilter = itemFilter; 
            } 
            else {
                if (itemFilter.Count > 0) { 
                    Hashtable hash = new Hashtable(itemFilter.Count + existingFilter.Count);
                    foreach (Attribute a in itemFilter) {
                        hash[a.TypeId] = a;
                    } 
                    foreach (Attribute a in existingFilter) {
                        hash[a.TypeId] = a; 
                    } 
                    ToolboxItemFilterAttribute[] filter = new ToolboxItemFilterAttribute[hash.Values.Count];
                    hash.Values.CopyTo(filter, 0); 
                    finalFilter = filter;
                }
                else {
                    finalFilter = existingFilter; 
                }
            } 
 
            return finalFilter;
        } 

        /// <include file='doc\ToolboxService.uex' path='docs/doc[@for="ToolboxItemContainer.ISerializable.GetObjectData"]/*' />
        /// <devdoc>
        ///     Serialization method.  We make this private because the only time 
        ///     we serialize this class is when enumerating assemblies, and there
        ///     we never create instances of derived classes. 
        /// </devdoc> 
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.SerializationFormatter)]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) { 
            GetObjectData(info, context);
        }

        /// <devdoc> 
        ///     BrokenToolboxItem is a placeholder for a toolbox item we failed to create.
        ///     It sits silently as a simple component until someone asks to create an instance 
        ///     of the objects within it, and then it displays an error to the user. 
        /// </devdoc>
        private class BrokenToolboxItem : ToolboxItem { 

            private string _exceptionString;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")] 
            public BrokenToolboxItem(string exceptionString)
                : base(typeof(Component)) { 
                _exceptionString = exceptionString; 
                Lock();
            } 

            protected override IComponent[] CreateComponentsCore(IDesignerHost host) {
                if (_exceptionString != null) {
                    throw new InvalidOperationException(SR.GetString(SR.ToolboxServiceBadToolboxItemWithException, _exceptionString)); 
                }
                else { 
                    throw new InvalidOperationException(SR.GetString(SR.ToolboxServiceBadToolboxItem)); 
                }
            } 
        }

        /// <devdoc>
        ///     This class serializes a toolbox item.  It prefixes the toolbox item with 
        ///     assembly information so that when the item is deseralized it can be loaded
        ///     from the assembly, even if that assembly is not in the GAC. 
        /// </devdoc> 
        [Serializable]
        private sealed class ToolboxItemSerializer : ISerializable { 

            private const string _assemblyNameKey = "AssemblyName";
            private const string _streamKey = "Stream";
 
            private static BinaryFormatter _formatter;
 
            private ToolboxItem _toolboxItem; 

            /// <devdoc> 
            /// </devdoc>
            internal ToolboxItemSerializer(ToolboxItem toolboxItem) {
                _toolboxItem = toolboxItem;
            } 

            /// <devdoc> 
            /// </devdoc> 
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:AvoidUnusedParameters")]
            private ToolboxItemSerializer(SerializationInfo info, StreamingContext context) { 
                AssemblyName name = (AssemblyName)info.GetValue(_assemblyNameKey, typeof(AssemblyName));
                byte[] bytes = (byte[])info.GetValue(_streamKey, typeof(byte[]));

                if (_formatter == null) { 
                    _formatter = new BinaryFormatter();
                } 
 
                SerializationBinder oldBinder = _formatter.Binder;
                _formatter.Binder = new ToolboxSerializationBinder(name); 
                try {
                    _toolboxItem = (ToolboxItem)_formatter.Deserialize(new MemoryStream(bytes));
                }
                finally { 
                    _formatter.Binder = oldBinder;
                } 
            } 

            /// <devdoc> 
            /// </devdoc>
            internal ToolboxItem ToolboxItem {
                get {
                    return _toolboxItem; 
                }
            } 
 
            /// <devdoc>
            /// </devdoc> 
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {

                if (_formatter == null) {
                    _formatter = new BinaryFormatter(); 
                }
 
                MemoryStream stream = new MemoryStream(); 
                _formatter.Serialize(stream, _toolboxItem);
                stream.Close(); 

                info.AddValue(_assemblyNameKey, _toolboxItem.GetType().Assembly.GetName());
                info.AddValue(_streamKey, stream.GetBuffer());
            } 
        }
 
        /// <devdoc> 
        ///     Simple serialization binder that is used to load up toolbox items from
        ///     assemblies that are not stored in the GAC. 
        /// </devdoc>
        private class ToolboxSerializationBinder : SerializationBinder {

            private Hashtable _assemblies; 
            private AssemblyName _name;
            private string _namePart; 
 
            /// <devdoc>
            ///     Create a new toolbox serialization binder. 
            /// </devdoc>
            public ToolboxSerializationBinder(AssemblyName name) {
                _assemblies = new Hashtable();
                _name = name; 
                _namePart = name.Name + ",";
            } 
 
            /// <devdoc>
            ///     Takes a type name and creates a type.  If it cannot match 
            ///     the type it returns null.
            /// </devdoc>
            [
                System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods"), 
                System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1307:SpecifyStringComparison")
            ] 
            public override Type BindToType(string assemblyName, string typeName) { 

                Assembly assembly = (Assembly)_assemblies[assemblyName]; 

                if (assembly == null) {

                    // Try the normal assembly load first. 
                    //
                    try { 
                        assembly = Assembly.Load(assemblyName); 
                    }
                    catch (FileNotFoundException) { 
                    }
                    catch (BadImageFormatException) {
                    }
                    catch (IOException) { 
                    }
 
                    if (assembly == null) { 

                        AssemblyName an; 

                        // Try our stashed assembly name.
                        if (assemblyName.StartsWith(_namePart)) {
                            an = _name; 

                            try { 
                                assembly = Assembly.Load(an); 
                            }
                            catch (FileNotFoundException) { 
                            }
                            catch (BadImageFormatException) {
                            }
                            catch (IOException) { 
                            }
                        } 
                        else { 
                            an = new AssemblyName(assemblyName);
                        } 

                        // Finally, load via codebase.
                        //
                        if (assembly == null) { 
                            string codeBase = an.CodeBase;
                            if (codeBase != null && codeBase.Length > 0 && File.Exists(codeBase)) { 
                                assembly = Assembly.LoadFrom(codeBase); 
                            }
                        } 
                    }

                    if (assembly != null) {
                        _assemblies[assemblyName] = assembly; 
                    }
                } 
 
                if (assembly != null) {
                    return assembly.GetType(typeName); 
                }

                // Binder couldn't handle it, let the default loader take over.
                return null; 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
