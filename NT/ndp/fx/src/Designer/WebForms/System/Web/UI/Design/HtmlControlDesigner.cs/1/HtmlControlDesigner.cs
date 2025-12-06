//------------------------------------------------------------------------------ 
// <copyright file="HtmlControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System.Design; 
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Diagnostics;
    using System;
    using System.Collections;
    using Microsoft.Win32; 
    using System.Web.UI;
    using System.Web.UI.WebControls; 
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Drawing.Design; 
    using System.Windows.Forms;
    using WebUIControl = System.Web.UI.Control;
    using PropertyDescriptor = System.ComponentModel.PropertyDescriptor;
 
    /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner"]/*' />
    /// <devdoc> 
    ///    <para>Provides a base designer class for all server/ASP controls.</para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class HtmlControlDesigner : ComponentDesigner {

#pragma warning disable 618
        private IHtmlControlDesignerBehavior behavior = null;           // the DHTML/Attached Behavior associated to this designer 
#pragma warning restore 618
 
        private bool shouldCodeSerialize; 

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.HtmlControlDesigner"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Initiailizes a new instance of <see cref='System.Web.UI.Design.HtmlControlDesigner'/>.
        ///    </para> 
        /// </devdoc>
        public HtmlControlDesigner() { 
            shouldCodeSerialize = true; 
        }
 
        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.DesignTimeElement"]/*' />
        /// <devdoc>
        ///   <para>The design-time object representing the control associated with this designer on the design surface.</para>
        /// </devdoc> 
        [Obsolete("Error: This property can no longer be referenced, and is included to support existing compiled applications. The design-time element may not always provide access to the element in the markup. There are alternate methods on WebFormsRootDesigner for handling client script and controls. http://go.microsoft.com/fwlink/?linkid=14202", true)]
        protected object DesignTimeElement { 
            get { 
                return DesignTimeElementInternal;
            } 
        }

        internal object DesignTimeElementInternal {
            get { 
                return behavior != null ? behavior.DesignTimeElement : null;
            } 
        } 

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.Behavior"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Points to the DHTML Behavior that is associated to this designer instance.
        ///    </para> 
        /// </devdoc>
        [Obsolete("The recommended alternative is ControlDesigner.Tag. http://go.microsoft.com/fwlink/?linkid=14202")] 
        public IHtmlControlDesignerBehavior Behavior { 
            get {
                return BehaviorInternal; 
            }
            set {
                BehaviorInternal = value;
            } 
        }
 
#pragma warning disable 618 
        internal virtual IHtmlControlDesignerBehavior BehaviorInternal {
            get { 
                return behavior;
            }
            set {
                if (behavior != value) { 

                    if (behavior != null) { 
                        OnBehaviorDetaching(); 

                        // A different behavior might get attached in some cases. So, make sure to 
                        // reset the back pointer from the currently associated behavior to this designer.
                        behavior.Designer = null;
                        behavior = null;
                    } 

                    if (value != null) { 
                        behavior = value; 
                        OnBehaviorAttached();
                    } 
                }
            }
        }
#pragma warning restore 618 

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.DataBindings"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        public DataBindingCollection DataBindings { 
            get {
                return ((IDataBindingsAccessor)Component).DataBindings;
            }
        } 

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.Expressions"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        public ExpressionBindingCollection Expressions { 
            get {
                return ((IExpressionsAccessor)Component).Expressions;
            }
        } 

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.ShouldSerialize"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        [Obsolete("Use of this property is not recommended because code serialization is not supported. http://go.microsoft.com/fwlink/?linkid=14202")] 
        public virtual bool ShouldCodeSerialize {
            get {
                return ShouldCodeSerializeInternal;
            } 
            set {
                ShouldCodeSerializeInternal = value; 
            } 
        }
 
        internal virtual bool ShouldCodeSerializeInternal {
            get {
                return shouldCodeSerialize;
            } 
            set {
                shouldCodeSerialize = value; 
            } 
        }
 
        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.Dispose"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Disposes of the resources (other than memory) used by 
        ///       the <see cref='System.Web.UI.Design.HtmlControlDesigner'/>.
        ///    </para> 
        /// </devdoc> 
        protected override void Dispose(bool disposing) {
            if (disposing) { 
                if (BehaviorInternal != null) {
                    BehaviorInternal.Designer = null;
                    BehaviorInternal = null;
                } 
            }
 
            base.Dispose(disposing); 
        }
 
        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.Initialize"]/*' />
        /// <devdoc>
        ///    <para>Initializes
        ///       the designer and sets the component for design.</para> 
        /// </devdoc>
        public override void Initialize(IComponent component) { 
            ControlDesigner.VerifyInitializeArgument(component, typeof(WebUIControl)); 
            base.Initialize(component);
        } 

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.OnBehaviorAttached"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Notification that is called when the designer is attached to the behavior.
        ///    </para> 
        /// </devdoc> 
        [Obsolete("The recommended alternative is ControlDesigner.Tag. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected virtual void OnBehaviorAttached() { 
        }

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.OnBehaviorDetaching"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Notification that is called when the designer is detached from the behavior. 
        ///    </para> 
        /// </devdoc>
        [Obsolete("The recommended alternative is ControlDesigner.Tag. http://go.microsoft.com/fwlink/?linkid=14202")] 
        protected virtual void OnBehaviorDetaching() {
        }

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.OnSetParent"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Notification that is called when the associated control is parented. 
        ///    </para>
        /// </devdoc> 
        public virtual void OnSetParent() {
        }

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.PreFilterEvents"]/*' /> 
        protected override void PreFilterEvents(IDictionary events) {
            base.PreFilterEvents(events); 
 
            if (ShouldCodeSerializeInternal == false) {
                // hide all the events, if this control isn't going to be serialized to code behind, 

                ICollection eventCollection = events.Values;
                if ((eventCollection != null) && (eventCollection.Count != 0)) {
                    object[] eventDescriptors = new object[eventCollection.Count]; 
                    eventCollection.CopyTo(eventDescriptors, 0);
 
                    for (int i = 0; i < eventDescriptors.Length; i++) { 
                        EventDescriptor eventDesc = (EventDescriptor)eventDescriptors[i];
 
                        eventDesc = TypeDescriptor.CreateEvent(eventDesc.ComponentType, eventDesc, BrowsableAttribute.No);
                        events[eventDesc.Name] = eventDesc;
                    }
                } 
            }
        } 
 
        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.PreFilterProperties"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Allows a designer to filter the set of member attributes
        ///       that the component it is designing will expose through the <see cref='System.ComponentModel.TypeDescriptor'/>
        ///       object. 
        ///    </para>
        /// </devdoc> 
        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties);
 
            PropertyDescriptor prop = (PropertyDescriptor)properties["Modifiers"];
            if (prop != null) {
                properties["Modifiers"] = TypeDescriptor.CreateProperty(prop.ComponentType, prop, BrowsableAttribute.No);
            } 

            properties["Expressions"] = 
                TypeDescriptor.CreateProperty(this.GetType(), "Expressions", typeof(ExpressionBindingCollection), 
                                              new Attribute[] {
                                                  DesignerSerializationVisibilityAttribute.Hidden, 
                                                  CategoryAttribute.Data,
                                                  new EditorAttribute(typeof(ExpressionsCollectionEditor), typeof(UITypeEditor)),
                                                  new TypeConverterAttribute(typeof(ExpressionsCollectionConverter)),
                                                  new ParenthesizePropertyNameAttribute(true), 
                                                  MergablePropertyAttribute.No,
                                                  new DescriptionAttribute(SR.GetString(SR.Control_Expressions)) 
                                              }); 

        } 

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.OnBindingsCollectionChanged"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Delegate to handle bindings collection changed event.
        ///    </para> 
        /// </devdoc> 
        [Obsolete("The recommended alternative is to handle the Changed event on the DataBindings collection. The DataBindings collection allows more control of the databindings associated with the control. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected virtual void OnBindingsCollectionChanged(string propName) { 
        }

        internal void OnBindingsCollectionChangedInternal(string propName) {
#pragma warning disable 618 
            OnBindingsCollectionChanged(propName);
#pragma warning restore 618 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="HtmlControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System.Design; 
    using System.Runtime.InteropServices;
    using System.ComponentModel; 
    using System.Diagnostics;
    using System;
    using System.Collections;
    using Microsoft.Win32; 
    using System.Web.UI;
    using System.Web.UI.WebControls; 
    using System.ComponentModel.Design; 
    using System.Drawing;
    using System.Drawing.Design; 
    using System.Windows.Forms;
    using WebUIControl = System.Web.UI.Control;
    using PropertyDescriptor = System.ComponentModel.PropertyDescriptor;
 
    /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner"]/*' />
    /// <devdoc> 
    ///    <para>Provides a base designer class for all server/ASP controls.</para> 
    /// </devdoc>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)] 
    public class HtmlControlDesigner : ComponentDesigner {

#pragma warning disable 618
        private IHtmlControlDesignerBehavior behavior = null;           // the DHTML/Attached Behavior associated to this designer 
#pragma warning restore 618
 
        private bool shouldCodeSerialize; 

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.HtmlControlDesigner"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Initiailizes a new instance of <see cref='System.Web.UI.Design.HtmlControlDesigner'/>.
        ///    </para> 
        /// </devdoc>
        public HtmlControlDesigner() { 
            shouldCodeSerialize = true; 
        }
 
        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.DesignTimeElement"]/*' />
        /// <devdoc>
        ///   <para>The design-time object representing the control associated with this designer on the design surface.</para>
        /// </devdoc> 
        [Obsolete("Error: This property can no longer be referenced, and is included to support existing compiled applications. The design-time element may not always provide access to the element in the markup. There are alternate methods on WebFormsRootDesigner for handling client script and controls. http://go.microsoft.com/fwlink/?linkid=14202", true)]
        protected object DesignTimeElement { 
            get { 
                return DesignTimeElementInternal;
            } 
        }

        internal object DesignTimeElementInternal {
            get { 
                return behavior != null ? behavior.DesignTimeElement : null;
            } 
        } 

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.Behavior"]/*' /> 
        /// <devdoc>
        ///    <para>
        ///       Points to the DHTML Behavior that is associated to this designer instance.
        ///    </para> 
        /// </devdoc>
        [Obsolete("The recommended alternative is ControlDesigner.Tag. http://go.microsoft.com/fwlink/?linkid=14202")] 
        public IHtmlControlDesignerBehavior Behavior { 
            get {
                return BehaviorInternal; 
            }
            set {
                BehaviorInternal = value;
            } 
        }
 
#pragma warning disable 618 
        internal virtual IHtmlControlDesignerBehavior BehaviorInternal {
            get { 
                return behavior;
            }
            set {
                if (behavior != value) { 

                    if (behavior != null) { 
                        OnBehaviorDetaching(); 

                        // A different behavior might get attached in some cases. So, make sure to 
                        // reset the back pointer from the currently associated behavior to this designer.
                        behavior.Designer = null;
                        behavior = null;
                    } 

                    if (value != null) { 
                        behavior = value; 
                        OnBehaviorAttached();
                    } 
                }
            }
        }
#pragma warning restore 618 

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.DataBindings"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        public DataBindingCollection DataBindings { 
            get {
                return ((IDataBindingsAccessor)Component).DataBindings;
            }
        } 

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.Expressions"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        public ExpressionBindingCollection Expressions { 
            get {
                return ((IExpressionsAccessor)Component).Expressions;
            }
        } 

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.ShouldSerialize"]/*' /> 
        /// <devdoc> 
        /// </devdoc>
        [Obsolete("Use of this property is not recommended because code serialization is not supported. http://go.microsoft.com/fwlink/?linkid=14202")] 
        public virtual bool ShouldCodeSerialize {
            get {
                return ShouldCodeSerializeInternal;
            } 
            set {
                ShouldCodeSerializeInternal = value; 
            } 
        }
 
        internal virtual bool ShouldCodeSerializeInternal {
            get {
                return shouldCodeSerialize;
            } 
            set {
                shouldCodeSerialize = value; 
            } 
        }
 
        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.Dispose"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Disposes of the resources (other than memory) used by 
        ///       the <see cref='System.Web.UI.Design.HtmlControlDesigner'/>.
        ///    </para> 
        /// </devdoc> 
        protected override void Dispose(bool disposing) {
            if (disposing) { 
                if (BehaviorInternal != null) {
                    BehaviorInternal.Designer = null;
                    BehaviorInternal = null;
                } 
            }
 
            base.Dispose(disposing); 
        }
 
        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.Initialize"]/*' />
        /// <devdoc>
        ///    <para>Initializes
        ///       the designer and sets the component for design.</para> 
        /// </devdoc>
        public override void Initialize(IComponent component) { 
            ControlDesigner.VerifyInitializeArgument(component, typeof(WebUIControl)); 
            base.Initialize(component);
        } 

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.OnBehaviorAttached"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Notification that is called when the designer is attached to the behavior.
        ///    </para> 
        /// </devdoc> 
        [Obsolete("The recommended alternative is ControlDesigner.Tag. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected virtual void OnBehaviorAttached() { 
        }

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.OnBehaviorDetaching"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Notification that is called when the designer is detached from the behavior. 
        ///    </para> 
        /// </devdoc>
        [Obsolete("The recommended alternative is ControlDesigner.Tag. http://go.microsoft.com/fwlink/?linkid=14202")] 
        protected virtual void OnBehaviorDetaching() {
        }

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.OnSetParent"]/*' /> 
        /// <devdoc>
        ///    <para> 
        ///       Notification that is called when the associated control is parented. 
        ///    </para>
        /// </devdoc> 
        public virtual void OnSetParent() {
        }

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.PreFilterEvents"]/*' /> 
        protected override void PreFilterEvents(IDictionary events) {
            base.PreFilterEvents(events); 
 
            if (ShouldCodeSerializeInternal == false) {
                // hide all the events, if this control isn't going to be serialized to code behind, 

                ICollection eventCollection = events.Values;
                if ((eventCollection != null) && (eventCollection.Count != 0)) {
                    object[] eventDescriptors = new object[eventCollection.Count]; 
                    eventCollection.CopyTo(eventDescriptors, 0);
 
                    for (int i = 0; i < eventDescriptors.Length; i++) { 
                        EventDescriptor eventDesc = (EventDescriptor)eventDescriptors[i];
 
                        eventDesc = TypeDescriptor.CreateEvent(eventDesc.ComponentType, eventDesc, BrowsableAttribute.No);
                        events[eventDesc.Name] = eventDesc;
                    }
                } 
            }
        } 
 
        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.PreFilterProperties"]/*' />
        /// <devdoc> 
        ///    <para>
        ///       Allows a designer to filter the set of member attributes
        ///       that the component it is designing will expose through the <see cref='System.ComponentModel.TypeDescriptor'/>
        ///       object. 
        ///    </para>
        /// </devdoc> 
        protected override void PreFilterProperties(IDictionary properties) { 
            base.PreFilterProperties(properties);
 
            PropertyDescriptor prop = (PropertyDescriptor)properties["Modifiers"];
            if (prop != null) {
                properties["Modifiers"] = TypeDescriptor.CreateProperty(prop.ComponentType, prop, BrowsableAttribute.No);
            } 

            properties["Expressions"] = 
                TypeDescriptor.CreateProperty(this.GetType(), "Expressions", typeof(ExpressionBindingCollection), 
                                              new Attribute[] {
                                                  DesignerSerializationVisibilityAttribute.Hidden, 
                                                  CategoryAttribute.Data,
                                                  new EditorAttribute(typeof(ExpressionsCollectionEditor), typeof(UITypeEditor)),
                                                  new TypeConverterAttribute(typeof(ExpressionsCollectionConverter)),
                                                  new ParenthesizePropertyNameAttribute(true), 
                                                  MergablePropertyAttribute.No,
                                                  new DescriptionAttribute(SR.GetString(SR.Control_Expressions)) 
                                              }); 

        } 

        /// <include file='doc\HtmlControlDesigner.uex' path='docs/doc[@for="HtmlControlDesigner.OnBindingsCollectionChanged"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Delegate to handle bindings collection changed event.
        ///    </para> 
        /// </devdoc> 
        [Obsolete("The recommended alternative is to handle the Changed event on the DataBindings collection. The DataBindings collection allows more control of the databindings associated with the control. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected virtual void OnBindingsCollectionChanged(string propName) { 
        }

        internal void OnBindingsCollectionChangedInternal(string propName) {
#pragma warning disable 618 
            OnBindingsCollectionChanged(propName);
#pragma warning restore 618 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
