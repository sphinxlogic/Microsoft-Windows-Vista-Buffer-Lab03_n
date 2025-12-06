//------------------------------------------------------------------------------ 
// <copyright file="Parameter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics;
    using System.Globalization; 
    using System.Security.Permissions;
 
 
    /// <devdoc>
    /// Represents a parameter to a DataSourceControl. 
    /// Parameters can be session variables, web request parameters, or of custom types.
    /// </devdoc>
    [
    DefaultProperty("DefaultValue"), 
    ]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class Parameter : ICloneable, IStateManager {
 
        private ParameterCollection _owner;
        private bool _tracking;
        private StateBag _viewState;
 

 
        /// <devdoc> 
        /// Creates an instance of the Parameter class.
        /// </devdoc> 
        public Parameter() {
        }

 
        /// <devdoc>
        /// Creates an instance of the Parameter class with the specified parameter name. 
        /// </devdoc> 
        public Parameter(string name) {
            Name = name; 
        }


        /// <devdoc> 
        /// Creates an instance of the Parameter class with the specified parameter name and type.
        /// </devdoc> 
        public Parameter(string name, TypeCode type) { 
            Name = name;
            Type = type; 
        }


        /// <devdoc> 
        /// Creates an instance of the Parameter class with the specified parameter name, type, and default value.
        /// </devdoc> 
        public Parameter(string name, TypeCode type, string defaultValue) { 
            Name = name;
            Type = type; 
            DefaultValue = defaultValue;
        }

 
        /// <devdoc>
        /// Used to clone a parameter. 
        /// </devdoc> 
        protected Parameter(Parameter original) {
            DefaultValue = original.DefaultValue; 
            Direction = original.Direction;
            Name = original.Name;
            ConvertEmptyStringToNull = original.ConvertEmptyStringToNull;
            Size = original.Size; 
            Type = original.Type;
        } 
 

 
        /// <devdoc>
        /// Indicates whether the Parameter is tracking view state.
        /// </devdoc>
        protected bool IsTrackingViewState { 
            get {
                return _tracking; 
            } 
        }
 

        /// <devdoc>
        /// The default value to use in GetValue() if it cannot obtain a value.
        /// </devdoc> 
        [
        DefaultValue(null), 
        WebCategory("Parameter"), 
        WebSysDescription(SR.Parameter_DefaultValue),
        ] 
        public string DefaultValue {
            get {
                object o = ViewState["DefaultValue"];
                return (o as string); 
            }
            set { 
                if (DefaultValue != value) { 
                    ViewState["DefaultValue"] = value;
                    OnParameterChanged(); 
                }
            }
        }
 

        /// <devdoc> 
        /// Gets/sets the direction of the parameter. 
        /// </devdoc>
        [ 
        DefaultValue(ParameterDirection.Input),
        WebCategory("Parameter"),
        WebSysDescription(SR.Parameter_Direction),
        ] 
        public ParameterDirection Direction {
            get { 
                object o = ViewState["Direction"]; 
                if (o == null)
                    return ParameterDirection.Input; 
                return (ParameterDirection)o;
            }
            set {
                if (Direction != value) { 
                    ViewState["Direction"] = value;
                    OnParameterChanged(); 
                } 
            }
        } 


        /// <devdoc>
        /// Gets/sets the name of the parameter. 
        /// </devdoc>
        [ 
        DefaultValue(""), 
        WebCategory("Parameter"),
        WebSysDescription(SR.Parameter_Name), 
        ]
        public string Name {
            get {
                object o = ViewState["Name"]; 
                if (o == null)
                    return String.Empty; 
                return (string)o; 
            }
            set { 
                if (Name != value) {
                    ViewState["Name"] = value;
                    OnParameterChanged();
                } 
            }
        } 
 
        /// <devdoc>
        /// Returns the value of parameter after converting it to the proper type. 
        /// </devdoc>
        [
        Browsable(false),
        ] 
        internal object ParameterValue {
            get { 
                return GetValue(ViewState["ParameterValue"], DefaultValue, Type, ConvertEmptyStringToNull, false); 
            }
        } 

        internal static object GetValue(object value, string defaultValue, TypeCode type, bool convertEmptyStringToNull, bool ignoreNullableTypeChanges) {
            // Convert.ChangeType() throws if you attempt to convert to DBNull, so we have to special case it.
            if (type == TypeCode.DBNull) { 
                return DBNull.Value;
            } 
 
            // Get the value and convert it to the default value if it is null
            if (convertEmptyStringToNull) { 
                string stringValue = value as string;
                if ((stringValue != null) && (stringValue.Length == 0)) {
                    value = null;
                } 
            }
            if (value == null) { 
                // Attempt to use the default value, but if it is null too, just return null immediately 
                if (convertEmptyStringToNull && String.IsNullOrEmpty(defaultValue)) {
                    defaultValue = null; 
                }
                if (defaultValue == null) {
                    return null;
                } 
                value = defaultValue;
            } 
 
            Debug.Assert(value != null, "Value should not be null at this point.");
 
            if (type == TypeCode.Object || type == TypeCode.Empty) {
                return value;
            }
 
            // For ObjectDataSource we special-case Nullable<T> and do nothing because these
            // types will get converted when we actually call the method. 
            if (ignoreNullableTypeChanges) { 
                Type valueType = value.GetType();
                if (valueType.IsGenericType && (valueType.GetGenericTypeDefinition() == typeof(Nullable<>))) { 
                    return value;
                }
            }
            return value = Convert.ChangeType(value, type, CultureInfo.CurrentCulture);; 
        }
 
        /// <devdoc> 
        /// Gets/sets the size of the parameter.
        /// </devdoc> 
        [
        DefaultValue(0),
        WebCategory("Parameter"),
        WebSysDescription(SR.Parameter_Size), 
        ]
        public int Size { 
            get { 
                object o = ViewState["Size"];
                if (o == null) 
                    return 0;
                return (int)o;
            }
            set { 
                if (Size != value) {
                    ViewState["Size"] = value; 
                    OnParameterChanged(); 
                }
            } 
        }


        /// <devdoc> 
        /// Gets/sets the type of the parameter's value.
        /// </devdoc> 
        [ 
        DefaultValue(TypeCode.Empty),
        WebCategory("Parameter"), 
        WebSysDescription(SR.Parameter_Type),
        ]
        public TypeCode Type {
            get { 
                object o = ViewState["Type"];
                if (o == null) 
                    return TypeCode.Empty; 
                return (TypeCode)o;
            } 
            set {
                if (value < TypeCode.Empty || value > TypeCode.String) {
                    throw new ArgumentOutOfRangeException("value");
                } 

                if (Type != value) { 
                    ViewState["Type"] = value; 
                    OnParameterChanged();
                } 
            }
        }

 
        /// <devdoc>
        /// Gets/sets whether an empty string should be treated as a null value. If this property is set to true 
        /// and the value is an empty string, the default value will be used. 
        /// </devdoc>
        [ 
        DefaultValue(true),
        WebCategory("Parameter"),
        WebSysDescription(SR.Parameter_ConvertEmptyStringToNull),
        ] 
        public bool ConvertEmptyStringToNull {
            get { 
                object o = ViewState["ConvertEmptyStringToNull"]; 
                if (o == null)
                    return true; 
                return (bool)o;
            }
            set {
                if (ConvertEmptyStringToNull != value) { 
                    ViewState["ConvertEmptyStringToNull"] = value;
                    OnParameterChanged(); 
                } 
            }
        } 


        /// <devdoc>
        /// Indicates a dictionary of state information that allows you to save and restore 
        /// the state of a Parameter across multiple requests for the same page.
        /// </devdoc> 
        [ 
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ]
        protected StateBag ViewState {
            get {
                if (_viewState == null) { 
                    _viewState = new StateBag();
                    if (_tracking) 
                        _viewState.TrackViewState(); 
                }
 
                return _viewState;
            }
        }
 

 
        /// <devdoc> 
        /// Creates a new Parameter that is a copy of this Parameter.
        /// </devdoc> 
        protected virtual Parameter Clone() {
            return new Parameter(this);
        }
 

        /// <devdoc> 
        /// Evaluates the parameter and returns the new value. 
        /// The control parameter is used to access the page's framework.
        /// By default it returns the null, implying that the DefaultValue will 
        /// be the value.
        /// </devdoc>
        protected virtual object Evaluate(HttpContext context, Control control) {
            return null; 
        }
 
 
        /// <devdoc>
        /// Loads view state. 
        /// </devdoc>
        protected virtual void LoadViewState(object savedState) {
            if (savedState != null) {
                ViewState.LoadViewState(savedState); 
            }
        } 
 

        /// <devdoc> 
        /// Raises the ParameterChanged event. This notifies a listener that it should re-evaluate the value.
        /// </devdoc>
        protected void OnParameterChanged() {
            if (_owner != null) { 
                _owner.CallOnParametersChanged();
            } 
        } 

 
        /// <devdoc>
        /// Saves view state.
        /// </devdoc>
        protected virtual object SaveViewState() { 
            return (_viewState != null) ? _viewState.SaveViewState() : null;
        } 
 

        /// <devdoc> 
        /// Tells the Parameter to record its entire state into view state.
        /// </devdoc>
        protected internal virtual void SetDirty() {
            ViewState.SetDirty(true); 
        }
 
        /// <devdoc> 
        /// Tells the Parameter the collection it belongs to
        /// </devdoc> 
        internal void SetOwner(ParameterCollection owner) {
            _owner = owner;
        }
 

        /// <devdoc> 
        /// Converts the Parameter to a string value. 
        /// </devdoc>
        public override string ToString() { 
            return this.Name;
        }

 
        /// <devdoc>
        /// Tells the Parameter to start tracking property changes. 
        /// </devdoc> 
        protected virtual void TrackViewState() {
            _tracking = true; 

            if (_viewState != null) {
                _viewState.TrackViewState();
            } 
        }
 
        /// <devdoc> 
        /// Updates the value of parameter.
        /// If the value changed, this will raise the ParametersChanged event of the ParameterCollection it belongs to. 
        /// The control parameter is used to access the page's framework.
        /// </devdoc>
        internal void UpdateValue(HttpContext context, Control control) {
            object oldValue = ViewState["ParameterValue"]; 
            object newValue = Evaluate(context, control);
 
            ViewState["ParameterValue"] = newValue; 

            // If you have chains of dependency, like one control with a control parameter on another, and then a third with a control 
            // parameter on the second, the order in which the evaluations take place is non-deterministic and may create incorrect
            // evaluation of parameters because all our evaluation happens during LoadComplete.  The correct solution is to call DataBind
            // on the third control when the second control's selected value changes.  Hacky, but we don't support specifying dependency
            // chains on data sources. 
            if ((newValue == null && oldValue != null) || (newValue != null && !newValue.Equals(oldValue))) {
                OnParameterChanged(); 
            } 
        }
 

        #region Implementation of ICloneable

        /// <internalonly/> 
        object ICloneable.Clone() {
            return Clone(); 
        } 
        #endregion
 

        #region Implementation of IStateManager

        /// <internalonly/> 
        bool IStateManager.IsTrackingViewState {
            get { 
                return IsTrackingViewState; 
            }
        } 


        /// <internalonly/>
        void IStateManager.LoadViewState(object savedState) { 
            LoadViewState(savedState);
        } 
 

        /// <internalonly/> 
        object IStateManager.SaveViewState() {
            return SaveViewState();
        }
 

        /// <internalonly/> 
        void IStateManager.TrackViewState() { 
            TrackViewState();
        } 
        #endregion
    }
}
 

//------------------------------------------------------------------------------ 
// <copyright file="Parameter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics;
    using System.Globalization; 
    using System.Security.Permissions;
 
 
    /// <devdoc>
    /// Represents a parameter to a DataSourceControl. 
    /// Parameters can be session variables, web request parameters, or of custom types.
    /// </devdoc>
    [
    DefaultProperty("DefaultValue"), 
    ]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class Parameter : ICloneable, IStateManager {
 
        private ParameterCollection _owner;
        private bool _tracking;
        private StateBag _viewState;
 

 
        /// <devdoc> 
        /// Creates an instance of the Parameter class.
        /// </devdoc> 
        public Parameter() {
        }

 
        /// <devdoc>
        /// Creates an instance of the Parameter class with the specified parameter name. 
        /// </devdoc> 
        public Parameter(string name) {
            Name = name; 
        }


        /// <devdoc> 
        /// Creates an instance of the Parameter class with the specified parameter name and type.
        /// </devdoc> 
        public Parameter(string name, TypeCode type) { 
            Name = name;
            Type = type; 
        }


        /// <devdoc> 
        /// Creates an instance of the Parameter class with the specified parameter name, type, and default value.
        /// </devdoc> 
        public Parameter(string name, TypeCode type, string defaultValue) { 
            Name = name;
            Type = type; 
            DefaultValue = defaultValue;
        }

 
        /// <devdoc>
        /// Used to clone a parameter. 
        /// </devdoc> 
        protected Parameter(Parameter original) {
            DefaultValue = original.DefaultValue; 
            Direction = original.Direction;
            Name = original.Name;
            ConvertEmptyStringToNull = original.ConvertEmptyStringToNull;
            Size = original.Size; 
            Type = original.Type;
        } 
 

 
        /// <devdoc>
        /// Indicates whether the Parameter is tracking view state.
        /// </devdoc>
        protected bool IsTrackingViewState { 
            get {
                return _tracking; 
            } 
        }
 

        /// <devdoc>
        /// The default value to use in GetValue() if it cannot obtain a value.
        /// </devdoc> 
        [
        DefaultValue(null), 
        WebCategory("Parameter"), 
        WebSysDescription(SR.Parameter_DefaultValue),
        ] 
        public string DefaultValue {
            get {
                object o = ViewState["DefaultValue"];
                return (o as string); 
            }
            set { 
                if (DefaultValue != value) { 
                    ViewState["DefaultValue"] = value;
                    OnParameterChanged(); 
                }
            }
        }
 

        /// <devdoc> 
        /// Gets/sets the direction of the parameter. 
        /// </devdoc>
        [ 
        DefaultValue(ParameterDirection.Input),
        WebCategory("Parameter"),
        WebSysDescription(SR.Parameter_Direction),
        ] 
        public ParameterDirection Direction {
            get { 
                object o = ViewState["Direction"]; 
                if (o == null)
                    return ParameterDirection.Input; 
                return (ParameterDirection)o;
            }
            set {
                if (Direction != value) { 
                    ViewState["Direction"] = value;
                    OnParameterChanged(); 
                } 
            }
        } 


        /// <devdoc>
        /// Gets/sets the name of the parameter. 
        /// </devdoc>
        [ 
        DefaultValue(""), 
        WebCategory("Parameter"),
        WebSysDescription(SR.Parameter_Name), 
        ]
        public string Name {
            get {
                object o = ViewState["Name"]; 
                if (o == null)
                    return String.Empty; 
                return (string)o; 
            }
            set { 
                if (Name != value) {
                    ViewState["Name"] = value;
                    OnParameterChanged();
                } 
            }
        } 
 
        /// <devdoc>
        /// Returns the value of parameter after converting it to the proper type. 
        /// </devdoc>
        [
        Browsable(false),
        ] 
        internal object ParameterValue {
            get { 
                return GetValue(ViewState["ParameterValue"], DefaultValue, Type, ConvertEmptyStringToNull, false); 
            }
        } 

        internal static object GetValue(object value, string defaultValue, TypeCode type, bool convertEmptyStringToNull, bool ignoreNullableTypeChanges) {
            // Convert.ChangeType() throws if you attempt to convert to DBNull, so we have to special case it.
            if (type == TypeCode.DBNull) { 
                return DBNull.Value;
            } 
 
            // Get the value and convert it to the default value if it is null
            if (convertEmptyStringToNull) { 
                string stringValue = value as string;
                if ((stringValue != null) && (stringValue.Length == 0)) {
                    value = null;
                } 
            }
            if (value == null) { 
                // Attempt to use the default value, but if it is null too, just return null immediately 
                if (convertEmptyStringToNull && String.IsNullOrEmpty(defaultValue)) {
                    defaultValue = null; 
                }
                if (defaultValue == null) {
                    return null;
                } 
                value = defaultValue;
            } 
 
            Debug.Assert(value != null, "Value should not be null at this point.");
 
            if (type == TypeCode.Object || type == TypeCode.Empty) {
                return value;
            }
 
            // For ObjectDataSource we special-case Nullable<T> and do nothing because these
            // types will get converted when we actually call the method. 
            if (ignoreNullableTypeChanges) { 
                Type valueType = value.GetType();
                if (valueType.IsGenericType && (valueType.GetGenericTypeDefinition() == typeof(Nullable<>))) { 
                    return value;
                }
            }
            return value = Convert.ChangeType(value, type, CultureInfo.CurrentCulture);; 
        }
 
        /// <devdoc> 
        /// Gets/sets the size of the parameter.
        /// </devdoc> 
        [
        DefaultValue(0),
        WebCategory("Parameter"),
        WebSysDescription(SR.Parameter_Size), 
        ]
        public int Size { 
            get { 
                object o = ViewState["Size"];
                if (o == null) 
                    return 0;
                return (int)o;
            }
            set { 
                if (Size != value) {
                    ViewState["Size"] = value; 
                    OnParameterChanged(); 
                }
            } 
        }


        /// <devdoc> 
        /// Gets/sets the type of the parameter's value.
        /// </devdoc> 
        [ 
        DefaultValue(TypeCode.Empty),
        WebCategory("Parameter"), 
        WebSysDescription(SR.Parameter_Type),
        ]
        public TypeCode Type {
            get { 
                object o = ViewState["Type"];
                if (o == null) 
                    return TypeCode.Empty; 
                return (TypeCode)o;
            } 
            set {
                if (value < TypeCode.Empty || value > TypeCode.String) {
                    throw new ArgumentOutOfRangeException("value");
                } 

                if (Type != value) { 
                    ViewState["Type"] = value; 
                    OnParameterChanged();
                } 
            }
        }

 
        /// <devdoc>
        /// Gets/sets whether an empty string should be treated as a null value. If this property is set to true 
        /// and the value is an empty string, the default value will be used. 
        /// </devdoc>
        [ 
        DefaultValue(true),
        WebCategory("Parameter"),
        WebSysDescription(SR.Parameter_ConvertEmptyStringToNull),
        ] 
        public bool ConvertEmptyStringToNull {
            get { 
                object o = ViewState["ConvertEmptyStringToNull"]; 
                if (o == null)
                    return true; 
                return (bool)o;
            }
            set {
                if (ConvertEmptyStringToNull != value) { 
                    ViewState["ConvertEmptyStringToNull"] = value;
                    OnParameterChanged(); 
                } 
            }
        } 


        /// <devdoc>
        /// Indicates a dictionary of state information that allows you to save and restore 
        /// the state of a Parameter across multiple requests for the same page.
        /// </devdoc> 
        [ 
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), 
        ]
        protected StateBag ViewState {
            get {
                if (_viewState == null) { 
                    _viewState = new StateBag();
                    if (_tracking) 
                        _viewState.TrackViewState(); 
                }
 
                return _viewState;
            }
        }
 

 
        /// <devdoc> 
        /// Creates a new Parameter that is a copy of this Parameter.
        /// </devdoc> 
        protected virtual Parameter Clone() {
            return new Parameter(this);
        }
 

        /// <devdoc> 
        /// Evaluates the parameter and returns the new value. 
        /// The control parameter is used to access the page's framework.
        /// By default it returns the null, implying that the DefaultValue will 
        /// be the value.
        /// </devdoc>
        protected virtual object Evaluate(HttpContext context, Control control) {
            return null; 
        }
 
 
        /// <devdoc>
        /// Loads view state. 
        /// </devdoc>
        protected virtual void LoadViewState(object savedState) {
            if (savedState != null) {
                ViewState.LoadViewState(savedState); 
            }
        } 
 

        /// <devdoc> 
        /// Raises the ParameterChanged event. This notifies a listener that it should re-evaluate the value.
        /// </devdoc>
        protected void OnParameterChanged() {
            if (_owner != null) { 
                _owner.CallOnParametersChanged();
            } 
        } 

 
        /// <devdoc>
        /// Saves view state.
        /// </devdoc>
        protected virtual object SaveViewState() { 
            return (_viewState != null) ? _viewState.SaveViewState() : null;
        } 
 

        /// <devdoc> 
        /// Tells the Parameter to record its entire state into view state.
        /// </devdoc>
        protected internal virtual void SetDirty() {
            ViewState.SetDirty(true); 
        }
 
        /// <devdoc> 
        /// Tells the Parameter the collection it belongs to
        /// </devdoc> 
        internal void SetOwner(ParameterCollection owner) {
            _owner = owner;
        }
 

        /// <devdoc> 
        /// Converts the Parameter to a string value. 
        /// </devdoc>
        public override string ToString() { 
            return this.Name;
        }

 
        /// <devdoc>
        /// Tells the Parameter to start tracking property changes. 
        /// </devdoc> 
        protected virtual void TrackViewState() {
            _tracking = true; 

            if (_viewState != null) {
                _viewState.TrackViewState();
            } 
        }
 
        /// <devdoc> 
        /// Updates the value of parameter.
        /// If the value changed, this will raise the ParametersChanged event of the ParameterCollection it belongs to. 
        /// The control parameter is used to access the page's framework.
        /// </devdoc>
        internal void UpdateValue(HttpContext context, Control control) {
            object oldValue = ViewState["ParameterValue"]; 
            object newValue = Evaluate(context, control);
 
            ViewState["ParameterValue"] = newValue; 

            // If you have chains of dependency, like one control with a control parameter on another, and then a third with a control 
            // parameter on the second, the order in which the evaluations take place is non-deterministic and may create incorrect
            // evaluation of parameters because all our evaluation happens during LoadComplete.  The correct solution is to call DataBind
            // on the third control when the second control's selected value changes.  Hacky, but we don't support specifying dependency
            // chains on data sources. 
            if ((newValue == null && oldValue != null) || (newValue != null && !newValue.Equals(oldValue))) {
                OnParameterChanged(); 
            } 
        }
 

        #region Implementation of ICloneable

        /// <internalonly/> 
        object ICloneable.Clone() {
            return Clone(); 
        } 
        #endregion
 

        #region Implementation of IStateManager

        /// <internalonly/> 
        bool IStateManager.IsTrackingViewState {
            get { 
                return IsTrackingViewState; 
            }
        } 


        /// <internalonly/>
        void IStateManager.LoadViewState(object savedState) { 
            LoadViewState(savedState);
        } 
 

        /// <internalonly/> 
        object IStateManager.SaveViewState() {
            return SaveViewState();
        }
 

        /// <internalonly/> 
        void IStateManager.TrackViewState() { 
            TrackViewState();
        } 
        #endregion
    }
}
 

