//------------------------------------------------------------------------------ 
// <copyright file="EventBindingService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Text; 
    using Microsoft.Internal.Performance; 
    using System.Windows.Forms;
 
    /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService"]/*' />
    /// <devdoc>
    ///     This class provides a default implementation of the event
    ///     binding service. 
    /// </devdoc>
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")] 
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")] 
    public abstract class EventBindingService : IEventBindingService {
 
        private Hashtable _eventProperties;
        private IServiceProvider _provider;

        private IComponent showCodeComponent; 
        private EventDescriptor showCodeEventDescriptor;
        private string showCodeMethodName; 
        private static CodeMarkers codemarkers = CodeMarkers.Instance; 

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.EventBindingService"]/*' /> 
        /// <devdoc>
        ///     You must provide a service provider to the binding
        ///     service.
        /// </devdoc> 
        protected EventBindingService(IServiceProvider provider) {
            if (provider == null) { 
                throw new ArgumentNullException("provider"); 
            }
            _provider = provider; 
        }

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.CreateUniqueMethodName"]/*' />
        /// <devdoc> 
        ///     Creates a unique method name.  The name must be
        ///     compatible with the script language being used and 
        ///     it must not conflict with any other name in the user's 
        ///     code.
        /// </devdoc> 
        protected abstract string CreateUniqueMethodName(IComponent component, EventDescriptor e);

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.FreeMethod"]/*' />
        /// <devdoc> 
        ///     This provides a notification that a particular method
        ///     is no longer being used by an event handler.  Some implementations 
        ///     may want to remove the event hander when no events are using 
        ///     it.  By overriding UseMethod and FreeMethod, an implementation
        ///     can know when a method is no longer needed. 
        /// </devdoc>
        protected virtual void FreeMethod(IComponent component, EventDescriptor e, string methodName) {
        }
 
        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.GetCompatibleMethods"]/*' />
        /// <devdoc> 
        ///     Returns a collection of strings.  Each string is 
        ///     the method name of a method whose signature is
        ///     compatible with the delegate contained in the 
        ///     event descriptor.  This should return an empty
        ///     collection if no names are compatible.
        /// </devdoc>
        protected abstract ICollection GetCompatibleMethods(EventDescriptor e); 

        /// <devdoc> 
        ///     Generates a key based on a method name and it's parameters by just concatenating the 
        ///     parameters.
        /// </devdoc> 
        private string GetEventDescriptorHashCode(EventDescriptor eventDesc) {

            StringBuilder builder = new StringBuilder(eventDesc.Name);
            builder.Append(eventDesc.EventType.GetHashCode().ToString(CultureInfo.InvariantCulture)); 

            foreach(Attribute a in eventDesc.Attributes) { 
                builder.Append(a.GetHashCode().ToString(CultureInfo.InvariantCulture)); 
            }
 
            return builder.ToString();
        }

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.GetService"]/*' /> 
        /// <devdoc>
        ///     Gets the requested service from our service provider. 
        /// </devdoc> 
        protected object GetService(Type serviceType) {
            if (_provider != null) { 
                return _provider.GetService(serviceType);
            }
            return null;
        } 

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.ShowCode"]/*' /> 
        /// <devdoc> 
        ///     Shows the user code.  This method does not show any
        ///     particular code; generally it shows the last code the 
        ///     user typed.  This returns true if it was possible to
        ///     show the code, or false if not.
        /// </devdoc>
        protected abstract bool ShowCode(); 

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.ShowCode2"]/*' /> 
        /// <devdoc> 
        ///     Shows the user code at the given line number.  Line
        ///     numbers are one-based.  This returns true if it was 
        ///     possible to show the code, or false if not.
        /// </devdoc>
        protected abstract bool ShowCode(int lineNumber);
 
        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.ShowCode3"]/*' />
        /// <devdoc> 
        ///     Shows the body of the user code with the given method 
        ///     name. This returns true if it was possible to show
        ///     the code, or false if not. 
        /// </devdoc>
        protected abstract bool ShowCode(IComponent component, EventDescriptor e, string methodName);

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.UseMethod"]/*' /> 
        /// <devdoc>
        ///     This provides a notification that a particular method 
        ///     is being used by an event handler.  Some implementations 
        ///     may want to remove the event hander when no events are using
        ///     it.  By overriding UseMethod and FreeMethod, an implementation 
        ///     can know when a method is no longer needed.
        /// </devdoc>
        protected virtual void UseMethod(IComponent component, EventDescriptor e, string methodName) {
        } 

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.ValidateMethodName"]/*' /> 
        /// <devdoc> 
        ///     This validates that the provided method name is valid for
        ///     the language / script being used.  The default does nothing. 
        ///     You may override this and throw an exception if the name
        ///     is invalid for your use.
        /// </devdoc>
        protected virtual void ValidateMethodName(string methodName) { 
        }
 
        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.IEventBindingService.CreateUniqueMethodName"]/*' /> 
        /// <devdoc>
        ///     This creates a name for an event handling method for the given component 
        ///     and event.  The name that is created is guaranteed to be unique in the user's source
        ///     code.
        /// </devdoc>
        string IEventBindingService.CreateUniqueMethodName(IComponent component, EventDescriptor e) { 

            if (component == null) { 
                throw new ArgumentNullException("component"); 
            }
 
            if (e == null) {
                throw new ArgumentNullException("e");
            }
 
            return CreateUniqueMethodName(component, e);
        } 
 
        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.IEventBindingService.GetCompatibleMethods"]/*' />
        /// <devdoc> 
        ///     Retrieves a collection of strings.  Each string is the name of a method
        ///     in user code that has a signature that is compatible with the given event.
        /// </devdoc>
        ICollection IEventBindingService.GetCompatibleMethods(EventDescriptor e) { 

            if (e == null) { 
                throw new ArgumentNullException("e"); 
            }
 
            return GetCompatibleMethods(e);
        }

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.IEventBindingService.GetEvent"]/*' /> 
        /// <devdoc>
        ///     For properties that are representing events, this will return the event 
        ///     that the property represents. 
        /// </devdoc>
        EventDescriptor IEventBindingService.GetEvent(PropertyDescriptor property) { 

            if (property is EventPropertyDescriptor) {
                return ((EventPropertyDescriptor)property).Event;
            } 

            return null; 
        } 

        /// <devdoc> 
        ///     returns true if the given event has a generic argument or return value in its raise method.
        /// </devdoc>
        private bool HasGenericArgument(EventDescriptor ed) {
            if (ed == null || ed.ComponentType == null) { 
                return false;
            } 
 
            System.Reflection.EventInfo evInfo = ed.ComponentType.GetEvent(ed.Name);
            if (evInfo == null || !evInfo.EventHandlerType.IsGenericType) { 
                return false;
            }

            Type[] args = evInfo.EventHandlerType.GetGenericArguments(); 
            if (args != null && args.Length > 0) {
                for (int i = 0; i < args.Length; i++) { 
                    if (args[i].IsGenericType) { 
                        return true;
                    } 
                }
            }

            return false; 
        }
 
        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.IEventBindingService.GetEventProperties"]/*' /> 
        /// <devdoc>
        ///     Converts a set of events to a set of properties. 
        /// </devdoc>
        PropertyDescriptorCollection IEventBindingService.GetEventProperties(EventDescriptorCollection events) {

            if (events == null) { 
                throw new ArgumentNullException("events");
            } 
 
            System.Collections.Generic.List<PropertyDescriptor> props = new System.Collections.Generic.List<PropertyDescriptor>(events.Count);
 
            // We cache the property descriptors here for speed.  Create those for
            // events that we don't have yet.
            //
            if (_eventProperties == null) { 
                _eventProperties = new Hashtable();
            } 
 
            for (int i = 0; i < events.Count; i++) {
 
                if (HasGenericArgument(events[i])) {
                    continue;
                }
 
                object eventHashCode = GetEventDescriptorHashCode(events[i]);
 
                PropertyDescriptor prop = (PropertyDescriptor)_eventProperties[eventHashCode]; 
                if (prop == null) {
                    prop = new EventPropertyDescriptor(events[i], this); 
                    _eventProperties[eventHashCode] = prop;
                }
                props.Add(prop);
            } 

            return new PropertyDescriptorCollection(props.ToArray()); 
        } 

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.IEventBindingService.GetEventProperty"]/*' /> 
        /// <devdoc>
        ///     Converts a single event to a property.
        /// </devdoc>
        PropertyDescriptor IEventBindingService.GetEventProperty(EventDescriptor e) { 

            if (e == null) { 
                throw new ArgumentNullException("e"); 
            }
 
            if (_eventProperties == null) {
                _eventProperties = new Hashtable();
            }
 
            object eventHashCode = GetEventDescriptorHashCode(e);
 
            PropertyDescriptor prop = (PropertyDescriptor)_eventProperties[eventHashCode]; 

            if (prop == null) { 
                prop = new EventPropertyDescriptor(e, this);
                _eventProperties[eventHashCode] = prop;
            }
 
            return prop;
        } 
 
        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.IEventBindingService.ShowCode"]/*' />
        /// <devdoc> 
        ///     Displays the user code for this designer.  This will return true if the user
        ///     code could be displayed, or false otherwise.
        /// </devdoc>
        bool IEventBindingService.ShowCode() { 
            return ShowCode();
        } 
 
        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.IEventBindingService.ShowCode1"]/*' />
        /// <devdoc> 
        ///     Displays the user code for the designer.  This will return true if the user
        ///     code could be displayed, or false otherwise.
        /// </devdoc>
        bool IEventBindingService.ShowCode(int lineNumber) { 
            return ShowCode(lineNumber);
        } 
 
        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.IEventBindingService.ShowCode2"]/*' />
        /// <devdoc> 
        ///     Displays the user code for the given event.  This will return true if the user
        ///     code could be displayed, or false otherwise.
        /// </devdoc>
        bool IEventBindingService.ShowCode(IComponent component, EventDescriptor e) { 

            if (component == null) { 
                throw new ArgumentNullException("component"); 
            }
 
            if (e == null) {
                throw new ArgumentNullException("e");
            }
 
            PropertyDescriptor prop = ((IEventBindingService)this).GetEventProperty(e);
 
            string methodName = (string)prop.GetValue(component); 
            if (methodName == null) {
                return false;   // the event is not bound to a method. 
            }

            Debug.Assert(showCodeComponent == null && showCodeEventDescriptor == null && showCodeMethodName == null, "show code already pending");
 
            showCodeComponent = component;
            showCodeEventDescriptor = e; 
            showCodeMethodName = methodName; 
            Application.Idle += new EventHandler(this.ShowCodeIdle);
            return true; 
        }

        /// <include file='doc\CodeDomLoader.uex' path='docs/doc[@for="CodeDomLoader.IEventBindingService.ShowCode2"]/*' />
        /// <devdoc> 
        ///     Displays the user code for the given event.  This will return true if the user
        ///     code could be displayed, or false otherwise. 
        /// </devdoc> 
        private void ShowCodeIdle(object sender, EventArgs e) {
            Application.Idle -= new EventHandler(this.ShowCodeIdle); 

            try {
                ShowCode(showCodeComponent, showCodeEventDescriptor, showCodeMethodName);
            } 
            finally {
                showCodeComponent = null; 
                showCodeEventDescriptor = null; 
                showCodeMethodName = null;
                codemarkers.CodeMarker(CodeMarkerEvent.perfFXDesignShowCode); 
            }
        }

        /// <devdoc> 
        ///     This is an EventDescriptor cleverly wrapped in a PropertyDescriptor
        ///     of type String.  Note that we now handle subobjects by storing their 
        ///     event information in their base component's site's dictionary. 
        ///     Note also that when a value is set for this property we will code-gen
        ///     the event method.  If the property is set to a new value we will 
        ///     remove the old event method ONLY if it is empty.
        /// </devdoc>
        private class EventPropertyDescriptor : PropertyDescriptor {
 
            private EventDescriptor     _eventDesc;
            private EventBindingService _eventSvc; 
            private TypeConverter       _converter; 

            /// <devdoc> 
            ///     Creates a new EventPropertyDescriptor.
            /// </devdoc>
            internal EventPropertyDescriptor(EventDescriptor eventDesc, EventBindingService eventSvc) : base(eventDesc, null) {
                _eventDesc = eventDesc; 
                _eventSvc = eventSvc;
            } 
 
            /// <devdoc>
            ///     Indicates whether reset will change the value of the component.  If there 
            ///     is a DefaultValueAttribute, then this will return true if getValue returns
            ///     something different than the default value.  If there is a reset method and
            ///     a shouldPersist method, this will return what shouldPersist returns.
            ///     If there is just a reset method, this always returns true.  If none of these 
            ///     cases apply, this returns false.
            /// </devdoc> 
            public override bool CanResetValue(object component) { 
                return GetValue(component) != null;
            } 

            /// <devdoc>
            ///     Retrieves the type of the component this PropertyDescriptor is bound to.
            /// </devdoc> 
            public override Type ComponentType {
                get { 
                    return _eventDesc.ComponentType; 
                }
            } 

            /// <devdoc>
            ///      Retrieves the type converter for this property.
            /// </devdoc> 
            public override TypeConverter Converter {
                get { 
                    if (_converter == null) { 
                        _converter = new EventConverter(_eventDesc);
                    } 

                    return _converter;
                }
            } 

            /// <devdoc> 
            ///     Retrieves the event descriptor we are representing. 
            /// </devdoc>
            internal EventDescriptor Event { 
                get {
                    return _eventDesc;
                }
            } 

            /// <devdoc> 
            ///     Indicates whether this property is read only. 
            /// </devdoc>
            public override bool IsReadOnly { 
                get {
                    return Attributes[typeof(ReadOnlyAttribute)].Equals(ReadOnlyAttribute.Yes);
                }
            } 

            /// <devdoc> 
            ///     Retrieves the type of the property. 
            /// </devdoc>
            public override Type PropertyType { 
                get {
                    return _eventDesc.EventType;
                }
            } 

            /// <devdoc> 
            ///     Retrieves the current value of the property on component, 
            ///     invoking the getXXX method.  An exception in the getXXX
            ///     method will pass through. 
            /// </devdoc>
            public override object GetValue(object component) {

                if (component == null) { 
                    throw new ArgumentNullException("component");
                } 
 
                // We must locate the sited component, because we store data on the dictionary
                // service for the component. 
                //
                ISite site = null;

                if (component is IComponent) { 
                    site = ((IComponent)component).Site;
                } 
 
                if (site == null) {
                    IReferenceService rs = _eventSvc._provider.GetService(typeof(IReferenceService)) as IReferenceService; 
                    if (rs != null) {
                        IComponent baseComponent = rs.GetComponent(component);
                        if (baseComponent != null) {
                            site = baseComponent.Site; 
                        }
                    } 
                } 

                if (site == null) { 
                    // Object not sited, so we weren't able to set a value on it.  Setting
                    // a value will fail.
                    return null;
                } 

                IDictionaryService ds = (IDictionaryService)site.GetService(typeof(IDictionaryService)); 
                if (ds == null) { 
                    // No dictionary service, so we weren't able to set a value on it.  Setting
                    // a value will fail. 
                    return null;
                }

                return (string)ds.GetValue(new ReferenceEventClosure(component, this)); 
            }
 
            /// <devdoc> 
            ///     Will reset the default value for this property on the component.  If
            ///     there was a default value passed in as a DefaultValueAttribute, that 
            ///     value will be set as the value of the property on the component.  If
            ///     there was no default value passed in, a ResetXXX method will be looked
            ///     for.  If one is found, it will be invoked.  If one is not found, this
            ///     is a nop. 
            /// </devdoc>
            public override void ResetValue(object component) { 
                SetValue(component, null); 
            }
 
            /// <devdoc>
            ///     This will set value to be the new value of this property on the
            ///     component by invoking the setXXX method on the component.  If the
            ///     value specified is invalid, the component should throw an exception 
            ///     which will be passed up.  The component designer should design the
            ///     property so that getXXX following a setXXX should return the value 
            ///     passed in if no exception was thrown in the setXXX call. 
            /// </devdoc>
            public override void SetValue(object component, object value) { 

                // Argument, state checking.  Is it ok to set this event?
                //
                if (IsReadOnly) { 
                    Exception ex = new InvalidOperationException(SR.GetString(SR.EventBindingServiceEventReadOnly, Name));
                    ex.HelpLink = SR.EventBindingServiceEventReadOnly; 
                    throw ex; 
                }
 
                if (value != null && !(value is string)) {
                    Exception ex = new ArgumentException(SR.GetString(SR.EventBindingServiceBadArgType, Name, typeof(string).Name));
                    ex.HelpLink = SR.EventBindingServiceBadArgType;
                    throw ex; 
                }
 
                string name = (string)value; 
                if (name != null && name.Length == 0) {
                    name = null; 
                }

                // Obtain the site for the component.  Note that this can be a site
                // to a parent component if we can get to the reference service. 
                //
                ISite site = null; 
 
                if (component is IComponent) {
                    site = ((IComponent)component).Site; 
                }

                if (site == null) {
                    IReferenceService rs = _eventSvc._provider.GetService(typeof(IReferenceService)) as IReferenceService; 
                    if (rs != null) {
                        IComponent baseComponent = rs.GetComponent(component); 
                        if (baseComponent != null) { 
                            site = baseComponent.Site;
                        } 
                    }
                }

                if (site == null) { 
                    Exception ex = new InvalidOperationException(SR.GetString(SR.EventBindingServiceNoSite));
                    ex.HelpLink = SR.EventBindingServiceNoSite; 
                    throw ex; 
                }
 
                // The dictionary service is where we store the actual event method name.
                //
                IDictionaryService ds = site.GetService(typeof(IDictionaryService)) as IDictionaryService;
                if (ds == null) { 
                    Exception ex = new InvalidOperationException(SR.GetString(SR.EventBindingServiceMissingService, typeof(IDictionaryService).Name));
                    ex.HelpLink = SR.EventBindingServiceMissingService; 
                    throw ex; 
                }
 
                // Get the old method name, ensure that they are different, and then continue.
                //
                ReferenceEventClosure key = new ReferenceEventClosure(component, this);
                string oldName = (string)ds.GetValue(key); 

                if (object.ReferenceEquals(oldName, name)) { 
                    return; 
                }
 
                if (oldName != null && name != null && oldName.Equals(name)) {
                    return;
                }
 
                // Before we continue our work, ensure that the name is
                // actually valid. 
                // 
                if (name != null) {
                    _eventSvc.ValidateMethodName(name); 
                }

                // If there is a designer host, create a transaction so there is a
                // nice name for this change.  We don't want a name like 
                // "Change property 'Click', because to users, this isn't a property.
                // 
                IDesignerHost host = site.GetService(typeof(IDesignerHost)) as IDesignerHost; 
                DesignerTransaction trans = null;
 
                if (host != null) {
                    trans = host.CreateTransaction(SR.GetString(SR.EventBindingServiceSetValue, site.Name, name));
                }
 
                try {
                    // Ok, the names are different.  Fire a changing event to make 
                    // sure it's OK to perform the change. 
                    //
                    IComponentChangeService change = site.GetService(typeof(IComponentChangeService)) as IComponentChangeService; 
                    if (change != null) {
                        try{
                            change.OnComponentChanging(component, this);
                            change.OnComponentChanging(component, Event); 
                        }
                        catch(CheckoutException coEx){ 
                            if (coEx == CheckoutException.Canceled){ 
                                return;
                            } 
                            throw;
                        }
                    }
 
                    // Less chance of success of adding a new method name, so
                    // don't release the old name until we verify that adding 
                    // the new one actually succeeded. 
                    //
                    if (name != null) { 
                        _eventSvc.UseMethod((IComponent)component, _eventDesc, name);
                    }

                    if (oldName != null) { 
                        _eventSvc.FreeMethod((IComponent)component, _eventDesc, oldName);
                    } 
 
                    ds.SetValue(key, name);
 
                    if (change != null) {
                        change.OnComponentChanged(component, Event, null, null);
                        change.OnComponentChanged(component, this, oldName, name);
                    } 

                    OnValueChanged(component, EventArgs.Empty); 
 
                    if (trans != null) {
                        trans.Commit(); 
                    }
                }
                finally {
                    if (trans != null) { 
                        ((IDisposable)trans).Dispose();
                    } 
                } 
            }
 
            /// <devdoc>
            ///     Indicates whether the value of this property needs to be persisted. In
            ///     other words, it indicates whether the state of the property is distinct
            ///     from when the component is first instantiated. If there is a default 
            ///     value specified in this PropertyDescriptor, it will be compared against the
            ///     property's current value to determine this.  If there is't, the 
            ///     shouldPersistXXX method is looked for and invoked if found.  If both 
            ///     these routes fail, true will be returned.
            /// 
            ///     If this returns false, a tool should not persist this property's value.
            /// </devdoc>
            public override bool ShouldSerializeValue(object component) {
                return CanResetValue(component); 
            }
 
            /// <devdoc> 
            ///     Implements a type converter for event objects.
            /// </devdoc> 
            private class EventConverter : TypeConverter {

                private EventDescriptor _evt;
 
                /// <devdoc>
                ///     Creates a new EventConverter. 
                /// </devdoc> 
                internal EventConverter(EventDescriptor evt) {
                    _evt = evt; 
                }

                /// <devdoc>
                ///     Determines if this converter can convert an object in the given source 
                ///     type to the native type of the converter.
                /// </devdoc> 
                public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) { 
                    if (sourceType == typeof(string)) {
                        return true; 
                    }
                    return base.CanConvertFrom(context, sourceType);
                }
 
                /// <devdoc>
                ///     Determines if this converter can convert an object to the given destination 
                ///     type. 
                /// </devdoc>
                public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) { 
                    if (destinationType == typeof(string)) {
                        return true;
                    }
                    return base.CanConvertTo(context, destinationType); 
                }
 
                /// <devdoc> 
                ///     Converts the given object to the converter's native type.
                /// </devdoc> 
                public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
                    if (value == null) {
                        return value;
                    } 
                    if (value is string) {
                        if (((string)value).Length == 0) { 
                            return null; 
                        }
                        return value; 
                    }
                    return base.ConvertFrom(context, culture, value);
                }
 
                /// <devdoc>
                ///     Converts the given object to another type.  The most common types to convert 
                ///     are to and from a string object.  The default implementation will make a call 
                ///     to ToString on the object if the object is valid and if the destination
                ///     type is string.  If this cannot convert to the desitnation type, this will 
                ///     throw a NotSupportedException.
                /// </devdoc>
                public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
                    if (destinationType == typeof(string)) { 
                        return value == null ? string.Empty : value;
                    } 
                    return base.ConvertTo(context, culture, value, destinationType); 
                }
 
                /// <devdoc>
                ///     Retrieves a collection containing a set of standard values
                ///     for the data type this validator is designed for.  This
                ///     will return null if the data type does not support a 
                ///     standard set of values.
                /// </devdoc> 
                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 

                    // We cannot cache this because it depends on the contents of the source file. 
                    //
                    string[] eventMethods = null;

                    if (context != null) { 
                        IEventBindingService ebs = (IEventBindingService)context.GetService(typeof(IEventBindingService));
                        if (ebs != null) { 
                            ICollection methods = ebs.GetCompatibleMethods(_evt); 
                            eventMethods = new string[methods.Count];
                            int i = 0; 
                            foreach(string s in methods) {
                                eventMethods[i++] = s;
                            }
                        } 
                    }
 
                    return new StandardValuesCollection(eventMethods); 
                }
 
                /// <devdoc>
                ///     Determines if the list of standard values returned from
                ///     GetStandardValues is an exclusive list.  If the list
                ///     is exclusive, then no other values are valid, such as 
                ///     in an enum data type.  If the list is not exclusive,
                ///     then there are other valid values besides the list of 
                ///     standard values GetStandardValues provides. 
                /// </devdoc>
                public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { 
                    return false;
                }

                /// <devdoc> 
                ///     Determines if this object supports a standard set of values
                ///     that can be picked from a list. 
                /// </devdoc> 
                public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
                    return true; 
                }
            }

            /// <devdoc> 
            ///     This is a combination of a reference and a property, so that it can be used
            ///     as the key of a hashtable.  This is because we may have subobjects that share 
            ///     the same property. 
            /// </devdoc>
            private class ReferenceEventClosure { 
                object reference;
                EventPropertyDescriptor propertyDescriptor;

                public ReferenceEventClosure(object reference, EventPropertyDescriptor prop) { 
                    this.reference = reference;
                    this.propertyDescriptor = prop; 
                } 

                public override int GetHashCode() { 
                    return reference.GetHashCode() * propertyDescriptor.GetHashCode();
                }

                public override bool Equals(Object otherClosure) { 
                    if (otherClosure is ReferenceEventClosure) {
                        ReferenceEventClosure typedClosure = (ReferenceEventClosure) otherClosure; 
                        return(typedClosure.reference == reference && 
                               typedClosure.propertyDescriptor.Equals(propertyDescriptor));
                    } 
                    return false;
                }
            }
        } 
    }
} 
 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="EventBindingService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Design;
    using System.Diagnostics; 
    using System.Globalization;
    using System.Text; 
    using Microsoft.Internal.Performance; 
    using System.Windows.Forms;
 
    /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService"]/*' />
    /// <devdoc>
    ///     This class provides a default implementation of the event
    ///     binding service. 
    /// </devdoc>
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")] 
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")] 
    public abstract class EventBindingService : IEventBindingService {
 
        private Hashtable _eventProperties;
        private IServiceProvider _provider;

        private IComponent showCodeComponent; 
        private EventDescriptor showCodeEventDescriptor;
        private string showCodeMethodName; 
        private static CodeMarkers codemarkers = CodeMarkers.Instance; 

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.EventBindingService"]/*' /> 
        /// <devdoc>
        ///     You must provide a service provider to the binding
        ///     service.
        /// </devdoc> 
        protected EventBindingService(IServiceProvider provider) {
            if (provider == null) { 
                throw new ArgumentNullException("provider"); 
            }
            _provider = provider; 
        }

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.CreateUniqueMethodName"]/*' />
        /// <devdoc> 
        ///     Creates a unique method name.  The name must be
        ///     compatible with the script language being used and 
        ///     it must not conflict with any other name in the user's 
        ///     code.
        /// </devdoc> 
        protected abstract string CreateUniqueMethodName(IComponent component, EventDescriptor e);

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.FreeMethod"]/*' />
        /// <devdoc> 
        ///     This provides a notification that a particular method
        ///     is no longer being used by an event handler.  Some implementations 
        ///     may want to remove the event hander when no events are using 
        ///     it.  By overriding UseMethod and FreeMethod, an implementation
        ///     can know when a method is no longer needed. 
        /// </devdoc>
        protected virtual void FreeMethod(IComponent component, EventDescriptor e, string methodName) {
        }
 
        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.GetCompatibleMethods"]/*' />
        /// <devdoc> 
        ///     Returns a collection of strings.  Each string is 
        ///     the method name of a method whose signature is
        ///     compatible with the delegate contained in the 
        ///     event descriptor.  This should return an empty
        ///     collection if no names are compatible.
        /// </devdoc>
        protected abstract ICollection GetCompatibleMethods(EventDescriptor e); 

        /// <devdoc> 
        ///     Generates a key based on a method name and it's parameters by just concatenating the 
        ///     parameters.
        /// </devdoc> 
        private string GetEventDescriptorHashCode(EventDescriptor eventDesc) {

            StringBuilder builder = new StringBuilder(eventDesc.Name);
            builder.Append(eventDesc.EventType.GetHashCode().ToString(CultureInfo.InvariantCulture)); 

            foreach(Attribute a in eventDesc.Attributes) { 
                builder.Append(a.GetHashCode().ToString(CultureInfo.InvariantCulture)); 
            }
 
            return builder.ToString();
        }

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.GetService"]/*' /> 
        /// <devdoc>
        ///     Gets the requested service from our service provider. 
        /// </devdoc> 
        protected object GetService(Type serviceType) {
            if (_provider != null) { 
                return _provider.GetService(serviceType);
            }
            return null;
        } 

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.ShowCode"]/*' /> 
        /// <devdoc> 
        ///     Shows the user code.  This method does not show any
        ///     particular code; generally it shows the last code the 
        ///     user typed.  This returns true if it was possible to
        ///     show the code, or false if not.
        /// </devdoc>
        protected abstract bool ShowCode(); 

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.ShowCode2"]/*' /> 
        /// <devdoc> 
        ///     Shows the user code at the given line number.  Line
        ///     numbers are one-based.  This returns true if it was 
        ///     possible to show the code, or false if not.
        /// </devdoc>
        protected abstract bool ShowCode(int lineNumber);
 
        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.ShowCode3"]/*' />
        /// <devdoc> 
        ///     Shows the body of the user code with the given method 
        ///     name. This returns true if it was possible to show
        ///     the code, or false if not. 
        /// </devdoc>
        protected abstract bool ShowCode(IComponent component, EventDescriptor e, string methodName);

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.UseMethod"]/*' /> 
        /// <devdoc>
        ///     This provides a notification that a particular method 
        ///     is being used by an event handler.  Some implementations 
        ///     may want to remove the event hander when no events are using
        ///     it.  By overriding UseMethod and FreeMethod, an implementation 
        ///     can know when a method is no longer needed.
        /// </devdoc>
        protected virtual void UseMethod(IComponent component, EventDescriptor e, string methodName) {
        } 

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.ValidateMethodName"]/*' /> 
        /// <devdoc> 
        ///     This validates that the provided method name is valid for
        ///     the language / script being used.  The default does nothing. 
        ///     You may override this and throw an exception if the name
        ///     is invalid for your use.
        /// </devdoc>
        protected virtual void ValidateMethodName(string methodName) { 
        }
 
        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.IEventBindingService.CreateUniqueMethodName"]/*' /> 
        /// <devdoc>
        ///     This creates a name for an event handling method for the given component 
        ///     and event.  The name that is created is guaranteed to be unique in the user's source
        ///     code.
        /// </devdoc>
        string IEventBindingService.CreateUniqueMethodName(IComponent component, EventDescriptor e) { 

            if (component == null) { 
                throw new ArgumentNullException("component"); 
            }
 
            if (e == null) {
                throw new ArgumentNullException("e");
            }
 
            return CreateUniqueMethodName(component, e);
        } 
 
        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.IEventBindingService.GetCompatibleMethods"]/*' />
        /// <devdoc> 
        ///     Retrieves a collection of strings.  Each string is the name of a method
        ///     in user code that has a signature that is compatible with the given event.
        /// </devdoc>
        ICollection IEventBindingService.GetCompatibleMethods(EventDescriptor e) { 

            if (e == null) { 
                throw new ArgumentNullException("e"); 
            }
 
            return GetCompatibleMethods(e);
        }

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.IEventBindingService.GetEvent"]/*' /> 
        /// <devdoc>
        ///     For properties that are representing events, this will return the event 
        ///     that the property represents. 
        /// </devdoc>
        EventDescriptor IEventBindingService.GetEvent(PropertyDescriptor property) { 

            if (property is EventPropertyDescriptor) {
                return ((EventPropertyDescriptor)property).Event;
            } 

            return null; 
        } 

        /// <devdoc> 
        ///     returns true if the given event has a generic argument or return value in its raise method.
        /// </devdoc>
        private bool HasGenericArgument(EventDescriptor ed) {
            if (ed == null || ed.ComponentType == null) { 
                return false;
            } 
 
            System.Reflection.EventInfo evInfo = ed.ComponentType.GetEvent(ed.Name);
            if (evInfo == null || !evInfo.EventHandlerType.IsGenericType) { 
                return false;
            }

            Type[] args = evInfo.EventHandlerType.GetGenericArguments(); 
            if (args != null && args.Length > 0) {
                for (int i = 0; i < args.Length; i++) { 
                    if (args[i].IsGenericType) { 
                        return true;
                    } 
                }
            }

            return false; 
        }
 
        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.IEventBindingService.GetEventProperties"]/*' /> 
        /// <devdoc>
        ///     Converts a set of events to a set of properties. 
        /// </devdoc>
        PropertyDescriptorCollection IEventBindingService.GetEventProperties(EventDescriptorCollection events) {

            if (events == null) { 
                throw new ArgumentNullException("events");
            } 
 
            System.Collections.Generic.List<PropertyDescriptor> props = new System.Collections.Generic.List<PropertyDescriptor>(events.Count);
 
            // We cache the property descriptors here for speed.  Create those for
            // events that we don't have yet.
            //
            if (_eventProperties == null) { 
                _eventProperties = new Hashtable();
            } 
 
            for (int i = 0; i < events.Count; i++) {
 
                if (HasGenericArgument(events[i])) {
                    continue;
                }
 
                object eventHashCode = GetEventDescriptorHashCode(events[i]);
 
                PropertyDescriptor prop = (PropertyDescriptor)_eventProperties[eventHashCode]; 
                if (prop == null) {
                    prop = new EventPropertyDescriptor(events[i], this); 
                    _eventProperties[eventHashCode] = prop;
                }
                props.Add(prop);
            } 

            return new PropertyDescriptorCollection(props.ToArray()); 
        } 

        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.IEventBindingService.GetEventProperty"]/*' /> 
        /// <devdoc>
        ///     Converts a single event to a property.
        /// </devdoc>
        PropertyDescriptor IEventBindingService.GetEventProperty(EventDescriptor e) { 

            if (e == null) { 
                throw new ArgumentNullException("e"); 
            }
 
            if (_eventProperties == null) {
                _eventProperties = new Hashtable();
            }
 
            object eventHashCode = GetEventDescriptorHashCode(e);
 
            PropertyDescriptor prop = (PropertyDescriptor)_eventProperties[eventHashCode]; 

            if (prop == null) { 
                prop = new EventPropertyDescriptor(e, this);
                _eventProperties[eventHashCode] = prop;
            }
 
            return prop;
        } 
 
        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.IEventBindingService.ShowCode"]/*' />
        /// <devdoc> 
        ///     Displays the user code for this designer.  This will return true if the user
        ///     code could be displayed, or false otherwise.
        /// </devdoc>
        bool IEventBindingService.ShowCode() { 
            return ShowCode();
        } 
 
        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.IEventBindingService.ShowCode1"]/*' />
        /// <devdoc> 
        ///     Displays the user code for the designer.  This will return true if the user
        ///     code could be displayed, or false otherwise.
        /// </devdoc>
        bool IEventBindingService.ShowCode(int lineNumber) { 
            return ShowCode(lineNumber);
        } 
 
        /// <include file='doc\EventBindingService.uex' path='docs/doc[@for="EventBindingService.IEventBindingService.ShowCode2"]/*' />
        /// <devdoc> 
        ///     Displays the user code for the given event.  This will return true if the user
        ///     code could be displayed, or false otherwise.
        /// </devdoc>
        bool IEventBindingService.ShowCode(IComponent component, EventDescriptor e) { 

            if (component == null) { 
                throw new ArgumentNullException("component"); 
            }
 
            if (e == null) {
                throw new ArgumentNullException("e");
            }
 
            PropertyDescriptor prop = ((IEventBindingService)this).GetEventProperty(e);
 
            string methodName = (string)prop.GetValue(component); 
            if (methodName == null) {
                return false;   // the event is not bound to a method. 
            }

            Debug.Assert(showCodeComponent == null && showCodeEventDescriptor == null && showCodeMethodName == null, "show code already pending");
 
            showCodeComponent = component;
            showCodeEventDescriptor = e; 
            showCodeMethodName = methodName; 
            Application.Idle += new EventHandler(this.ShowCodeIdle);
            return true; 
        }

        /// <include file='doc\CodeDomLoader.uex' path='docs/doc[@for="CodeDomLoader.IEventBindingService.ShowCode2"]/*' />
        /// <devdoc> 
        ///     Displays the user code for the given event.  This will return true if the user
        ///     code could be displayed, or false otherwise. 
        /// </devdoc> 
        private void ShowCodeIdle(object sender, EventArgs e) {
            Application.Idle -= new EventHandler(this.ShowCodeIdle); 

            try {
                ShowCode(showCodeComponent, showCodeEventDescriptor, showCodeMethodName);
            } 
            finally {
                showCodeComponent = null; 
                showCodeEventDescriptor = null; 
                showCodeMethodName = null;
                codemarkers.CodeMarker(CodeMarkerEvent.perfFXDesignShowCode); 
            }
        }

        /// <devdoc> 
        ///     This is an EventDescriptor cleverly wrapped in a PropertyDescriptor
        ///     of type String.  Note that we now handle subobjects by storing their 
        ///     event information in their base component's site's dictionary. 
        ///     Note also that when a value is set for this property we will code-gen
        ///     the event method.  If the property is set to a new value we will 
        ///     remove the old event method ONLY if it is empty.
        /// </devdoc>
        private class EventPropertyDescriptor : PropertyDescriptor {
 
            private EventDescriptor     _eventDesc;
            private EventBindingService _eventSvc; 
            private TypeConverter       _converter; 

            /// <devdoc> 
            ///     Creates a new EventPropertyDescriptor.
            /// </devdoc>
            internal EventPropertyDescriptor(EventDescriptor eventDesc, EventBindingService eventSvc) : base(eventDesc, null) {
                _eventDesc = eventDesc; 
                _eventSvc = eventSvc;
            } 
 
            /// <devdoc>
            ///     Indicates whether reset will change the value of the component.  If there 
            ///     is a DefaultValueAttribute, then this will return true if getValue returns
            ///     something different than the default value.  If there is a reset method and
            ///     a shouldPersist method, this will return what shouldPersist returns.
            ///     If there is just a reset method, this always returns true.  If none of these 
            ///     cases apply, this returns false.
            /// </devdoc> 
            public override bool CanResetValue(object component) { 
                return GetValue(component) != null;
            } 

            /// <devdoc>
            ///     Retrieves the type of the component this PropertyDescriptor is bound to.
            /// </devdoc> 
            public override Type ComponentType {
                get { 
                    return _eventDesc.ComponentType; 
                }
            } 

            /// <devdoc>
            ///      Retrieves the type converter for this property.
            /// </devdoc> 
            public override TypeConverter Converter {
                get { 
                    if (_converter == null) { 
                        _converter = new EventConverter(_eventDesc);
                    } 

                    return _converter;
                }
            } 

            /// <devdoc> 
            ///     Retrieves the event descriptor we are representing. 
            /// </devdoc>
            internal EventDescriptor Event { 
                get {
                    return _eventDesc;
                }
            } 

            /// <devdoc> 
            ///     Indicates whether this property is read only. 
            /// </devdoc>
            public override bool IsReadOnly { 
                get {
                    return Attributes[typeof(ReadOnlyAttribute)].Equals(ReadOnlyAttribute.Yes);
                }
            } 

            /// <devdoc> 
            ///     Retrieves the type of the property. 
            /// </devdoc>
            public override Type PropertyType { 
                get {
                    return _eventDesc.EventType;
                }
            } 

            /// <devdoc> 
            ///     Retrieves the current value of the property on component, 
            ///     invoking the getXXX method.  An exception in the getXXX
            ///     method will pass through. 
            /// </devdoc>
            public override object GetValue(object component) {

                if (component == null) { 
                    throw new ArgumentNullException("component");
                } 
 
                // We must locate the sited component, because we store data on the dictionary
                // service for the component. 
                //
                ISite site = null;

                if (component is IComponent) { 
                    site = ((IComponent)component).Site;
                } 
 
                if (site == null) {
                    IReferenceService rs = _eventSvc._provider.GetService(typeof(IReferenceService)) as IReferenceService; 
                    if (rs != null) {
                        IComponent baseComponent = rs.GetComponent(component);
                        if (baseComponent != null) {
                            site = baseComponent.Site; 
                        }
                    } 
                } 

                if (site == null) { 
                    // Object not sited, so we weren't able to set a value on it.  Setting
                    // a value will fail.
                    return null;
                } 

                IDictionaryService ds = (IDictionaryService)site.GetService(typeof(IDictionaryService)); 
                if (ds == null) { 
                    // No dictionary service, so we weren't able to set a value on it.  Setting
                    // a value will fail. 
                    return null;
                }

                return (string)ds.GetValue(new ReferenceEventClosure(component, this)); 
            }
 
            /// <devdoc> 
            ///     Will reset the default value for this property on the component.  If
            ///     there was a default value passed in as a DefaultValueAttribute, that 
            ///     value will be set as the value of the property on the component.  If
            ///     there was no default value passed in, a ResetXXX method will be looked
            ///     for.  If one is found, it will be invoked.  If one is not found, this
            ///     is a nop. 
            /// </devdoc>
            public override void ResetValue(object component) { 
                SetValue(component, null); 
            }
 
            /// <devdoc>
            ///     This will set value to be the new value of this property on the
            ///     component by invoking the setXXX method on the component.  If the
            ///     value specified is invalid, the component should throw an exception 
            ///     which will be passed up.  The component designer should design the
            ///     property so that getXXX following a setXXX should return the value 
            ///     passed in if no exception was thrown in the setXXX call. 
            /// </devdoc>
            public override void SetValue(object component, object value) { 

                // Argument, state checking.  Is it ok to set this event?
                //
                if (IsReadOnly) { 
                    Exception ex = new InvalidOperationException(SR.GetString(SR.EventBindingServiceEventReadOnly, Name));
                    ex.HelpLink = SR.EventBindingServiceEventReadOnly; 
                    throw ex; 
                }
 
                if (value != null && !(value is string)) {
                    Exception ex = new ArgumentException(SR.GetString(SR.EventBindingServiceBadArgType, Name, typeof(string).Name));
                    ex.HelpLink = SR.EventBindingServiceBadArgType;
                    throw ex; 
                }
 
                string name = (string)value; 
                if (name != null && name.Length == 0) {
                    name = null; 
                }

                // Obtain the site for the component.  Note that this can be a site
                // to a parent component if we can get to the reference service. 
                //
                ISite site = null; 
 
                if (component is IComponent) {
                    site = ((IComponent)component).Site; 
                }

                if (site == null) {
                    IReferenceService rs = _eventSvc._provider.GetService(typeof(IReferenceService)) as IReferenceService; 
                    if (rs != null) {
                        IComponent baseComponent = rs.GetComponent(component); 
                        if (baseComponent != null) { 
                            site = baseComponent.Site;
                        } 
                    }
                }

                if (site == null) { 
                    Exception ex = new InvalidOperationException(SR.GetString(SR.EventBindingServiceNoSite));
                    ex.HelpLink = SR.EventBindingServiceNoSite; 
                    throw ex; 
                }
 
                // The dictionary service is where we store the actual event method name.
                //
                IDictionaryService ds = site.GetService(typeof(IDictionaryService)) as IDictionaryService;
                if (ds == null) { 
                    Exception ex = new InvalidOperationException(SR.GetString(SR.EventBindingServiceMissingService, typeof(IDictionaryService).Name));
                    ex.HelpLink = SR.EventBindingServiceMissingService; 
                    throw ex; 
                }
 
                // Get the old method name, ensure that they are different, and then continue.
                //
                ReferenceEventClosure key = new ReferenceEventClosure(component, this);
                string oldName = (string)ds.GetValue(key); 

                if (object.ReferenceEquals(oldName, name)) { 
                    return; 
                }
 
                if (oldName != null && name != null && oldName.Equals(name)) {
                    return;
                }
 
                // Before we continue our work, ensure that the name is
                // actually valid. 
                // 
                if (name != null) {
                    _eventSvc.ValidateMethodName(name); 
                }

                // If there is a designer host, create a transaction so there is a
                // nice name for this change.  We don't want a name like 
                // "Change property 'Click', because to users, this isn't a property.
                // 
                IDesignerHost host = site.GetService(typeof(IDesignerHost)) as IDesignerHost; 
                DesignerTransaction trans = null;
 
                if (host != null) {
                    trans = host.CreateTransaction(SR.GetString(SR.EventBindingServiceSetValue, site.Name, name));
                }
 
                try {
                    // Ok, the names are different.  Fire a changing event to make 
                    // sure it's OK to perform the change. 
                    //
                    IComponentChangeService change = site.GetService(typeof(IComponentChangeService)) as IComponentChangeService; 
                    if (change != null) {
                        try{
                            change.OnComponentChanging(component, this);
                            change.OnComponentChanging(component, Event); 
                        }
                        catch(CheckoutException coEx){ 
                            if (coEx == CheckoutException.Canceled){ 
                                return;
                            } 
                            throw;
                        }
                    }
 
                    // Less chance of success of adding a new method name, so
                    // don't release the old name until we verify that adding 
                    // the new one actually succeeded. 
                    //
                    if (name != null) { 
                        _eventSvc.UseMethod((IComponent)component, _eventDesc, name);
                    }

                    if (oldName != null) { 
                        _eventSvc.FreeMethod((IComponent)component, _eventDesc, oldName);
                    } 
 
                    ds.SetValue(key, name);
 
                    if (change != null) {
                        change.OnComponentChanged(component, Event, null, null);
                        change.OnComponentChanged(component, this, oldName, name);
                    } 

                    OnValueChanged(component, EventArgs.Empty); 
 
                    if (trans != null) {
                        trans.Commit(); 
                    }
                }
                finally {
                    if (trans != null) { 
                        ((IDisposable)trans).Dispose();
                    } 
                } 
            }
 
            /// <devdoc>
            ///     Indicates whether the value of this property needs to be persisted. In
            ///     other words, it indicates whether the state of the property is distinct
            ///     from when the component is first instantiated. If there is a default 
            ///     value specified in this PropertyDescriptor, it will be compared against the
            ///     property's current value to determine this.  If there is't, the 
            ///     shouldPersistXXX method is looked for and invoked if found.  If both 
            ///     these routes fail, true will be returned.
            /// 
            ///     If this returns false, a tool should not persist this property's value.
            /// </devdoc>
            public override bool ShouldSerializeValue(object component) {
                return CanResetValue(component); 
            }
 
            /// <devdoc> 
            ///     Implements a type converter for event objects.
            /// </devdoc> 
            private class EventConverter : TypeConverter {

                private EventDescriptor _evt;
 
                /// <devdoc>
                ///     Creates a new EventConverter. 
                /// </devdoc> 
                internal EventConverter(EventDescriptor evt) {
                    _evt = evt; 
                }

                /// <devdoc>
                ///     Determines if this converter can convert an object in the given source 
                ///     type to the native type of the converter.
                /// </devdoc> 
                public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) { 
                    if (sourceType == typeof(string)) {
                        return true; 
                    }
                    return base.CanConvertFrom(context, sourceType);
                }
 
                /// <devdoc>
                ///     Determines if this converter can convert an object to the given destination 
                ///     type. 
                /// </devdoc>
                public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) { 
                    if (destinationType == typeof(string)) {
                        return true;
                    }
                    return base.CanConvertTo(context, destinationType); 
                }
 
                /// <devdoc> 
                ///     Converts the given object to the converter's native type.
                /// </devdoc> 
                public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
                    if (value == null) {
                        return value;
                    } 
                    if (value is string) {
                        if (((string)value).Length == 0) { 
                            return null; 
                        }
                        return value; 
                    }
                    return base.ConvertFrom(context, culture, value);
                }
 
                /// <devdoc>
                ///     Converts the given object to another type.  The most common types to convert 
                ///     are to and from a string object.  The default implementation will make a call 
                ///     to ToString on the object if the object is valid and if the destination
                ///     type is string.  If this cannot convert to the desitnation type, this will 
                ///     throw a NotSupportedException.
                /// </devdoc>
                public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
                    if (destinationType == typeof(string)) { 
                        return value == null ? string.Empty : value;
                    } 
                    return base.ConvertTo(context, culture, value, destinationType); 
                }
 
                /// <devdoc>
                ///     Retrieves a collection containing a set of standard values
                ///     for the data type this validator is designed for.  This
                ///     will return null if the data type does not support a 
                ///     standard set of values.
                /// </devdoc> 
                public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { 

                    // We cannot cache this because it depends on the contents of the source file. 
                    //
                    string[] eventMethods = null;

                    if (context != null) { 
                        IEventBindingService ebs = (IEventBindingService)context.GetService(typeof(IEventBindingService));
                        if (ebs != null) { 
                            ICollection methods = ebs.GetCompatibleMethods(_evt); 
                            eventMethods = new string[methods.Count];
                            int i = 0; 
                            foreach(string s in methods) {
                                eventMethods[i++] = s;
                            }
                        } 
                    }
 
                    return new StandardValuesCollection(eventMethods); 
                }
 
                /// <devdoc>
                ///     Determines if the list of standard values returned from
                ///     GetStandardValues is an exclusive list.  If the list
                ///     is exclusive, then no other values are valid, such as 
                ///     in an enum data type.  If the list is not exclusive,
                ///     then there are other valid values besides the list of 
                ///     standard values GetStandardValues provides. 
                /// </devdoc>
                public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { 
                    return false;
                }

                /// <devdoc> 
                ///     Determines if this object supports a standard set of values
                ///     that can be picked from a list. 
                /// </devdoc> 
                public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
                    return true; 
                }
            }

            /// <devdoc> 
            ///     This is a combination of a reference and a property, so that it can be used
            ///     as the key of a hashtable.  This is because we may have subobjects that share 
            ///     the same property. 
            /// </devdoc>
            private class ReferenceEventClosure { 
                object reference;
                EventPropertyDescriptor propertyDescriptor;

                public ReferenceEventClosure(object reference, EventPropertyDescriptor prop) { 
                    this.reference = reference;
                    this.propertyDescriptor = prop; 
                } 

                public override int GetHashCode() { 
                    return reference.GetHashCode() * propertyDescriptor.GetHashCode();
                }

                public override bool Equals(Object otherClosure) { 
                    if (otherClosure is ReferenceEventClosure) {
                        ReferenceEventClosure typedClosure = (ReferenceEventClosure) otherClosure; 
                        return(typedClosure.reference == reference && 
                               typedClosure.propertyDescriptor.Equals(propertyDescriptor));
                    } 
                    return false;
                }
            }
        } 
    }
} 
 

 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
