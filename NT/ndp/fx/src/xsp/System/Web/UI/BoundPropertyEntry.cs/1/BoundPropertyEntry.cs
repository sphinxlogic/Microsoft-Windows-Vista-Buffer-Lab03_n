//------------------------------------------------------------------------------ 
// <copyright file="BoundPropertyEntry.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI { 
 
    using System.Reflection;
    using System.Web.Util; 
    using System.Web.Compilation;
    using System.ComponentModel.Design;
    using System.Security.Permissions;
 

    /// <devdoc> 
    /// PropertyEntry for any bound properties 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class BoundPropertyEntry : PropertyEntry {
        private string _expression;
        private ExpressionBuilder _expressionBuilder; 
        private string _expressionPrefix;
        private bool _useSetAttribute; 
        private object _parsedExpressionData; 
        private bool _generated;
        private string _fieldName; 
        private string _formatString;
        private string _controlID;
        private Type _controlType;
        private bool _readOnlyProperty; 
        private bool _twoWayBound;
 
        internal BoundPropertyEntry() { 
        }
 
        /// <devdoc>
        /// The id of the control that contains this binding.
        /// </devdoc>
        public string ControlID { 
            get {
                return _controlID; 
            } 
            set {
                _controlID = value; 
            }
        }

        /// <devdoc> 
        /// The type of the control which is being bound to a runtime value.
        /// </devdoc> 
        public Type ControlType { 
            get {
                return _controlType; 
            }
            set {
                _controlType = value;
            } 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public string Expression { 
            get {
                return _expression;
            }
            set { 
                _expression = value;
            } 
        } 

        public ExpressionBuilder ExpressionBuilder { 
            get {
                return _expressionBuilder;
            }
            set { 
                _expressionBuilder = value;
            } 
        } 

        public string ExpressionPrefix { 
            get {
                return _expressionPrefix;
            }
            set { 
                _expressionPrefix = value;
            } 
        } 

        /// <devdoc> 
        /// The name of the data field that is being bound to.
        /// </devdoc>
        public string FieldName {
            get { 
                return _fieldName;
            } 
            set { 
                _fieldName = value;
            } 
        }

        /// <devdoc>
        /// The format string applied to the field for display. 
        /// </devdoc>
        public string FormatString { 
            get { 
                return _formatString;
            } 
            set {
                _formatString = value;
            }
        } 

        internal bool IsDataBindingEntry { 
            // Empty prefix means it's a databinding expression (i.e. <%# ... %>) 
            get { return String.IsNullOrEmpty(ExpressionPrefix); }
        } 

        public bool Generated {
            get {
                return _generated; 
            }
            set { 
                _generated = value; 
            }
        } 

        public object ParsedExpressionData {
            get {
                return _parsedExpressionData; 
            }
            set { 
                _parsedExpressionData = value; 
            }
        } 

        /// <devdoc>
        /// Indicates whether the two way statement is set and get, or just get but not set.
        /// </devdoc> 
        public bool ReadOnlyProperty {
            get { 
                return _readOnlyProperty; 
            }
            set { 
                _readOnlyProperty = value;
            }
        }
 
        public bool TwoWayBound {
            get { 
                return _twoWayBound; 
            }
            set { 
                _twoWayBound = value;
            }
        }
 
        /// <devdoc>
        /// </devdoc> 
        public bool UseSetAttribute { 
            get {
                return _useSetAttribute; 
            }
            set {
                _useSetAttribute = value;
            } 
        }
 
        // Parse the expression, and store the resulting object 
        internal void ParseExpression(ExpressionBuilderContext context) {
            if (Expression == null || ExpressionPrefix == null || ExpressionBuilder == null) 
                return;

            _parsedExpressionData = ExpressionBuilder.ParseExpression(Expression, Type, context);
        } 
    }
} 
 

//------------------------------------------------------------------------------ 
// <copyright file="BoundPropertyEntry.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI { 
 
    using System.Reflection;
    using System.Web.Util; 
    using System.Web.Compilation;
    using System.ComponentModel.Design;
    using System.Security.Permissions;
 

    /// <devdoc> 
    /// PropertyEntry for any bound properties 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class BoundPropertyEntry : PropertyEntry {
        private string _expression;
        private ExpressionBuilder _expressionBuilder; 
        private string _expressionPrefix;
        private bool _useSetAttribute; 
        private object _parsedExpressionData; 
        private bool _generated;
        private string _fieldName; 
        private string _formatString;
        private string _controlID;
        private Type _controlType;
        private bool _readOnlyProperty; 
        private bool _twoWayBound;
 
        internal BoundPropertyEntry() { 
        }
 
        /// <devdoc>
        /// The id of the control that contains this binding.
        /// </devdoc>
        public string ControlID { 
            get {
                return _controlID; 
            } 
            set {
                _controlID = value; 
            }
        }

        /// <devdoc> 
        /// The type of the control which is being bound to a runtime value.
        /// </devdoc> 
        public Type ControlType { 
            get {
                return _controlType; 
            }
            set {
                _controlType = value;
            } 
        }
 
        /// <devdoc> 
        /// </devdoc>
        public string Expression { 
            get {
                return _expression;
            }
            set { 
                _expression = value;
            } 
        } 

        public ExpressionBuilder ExpressionBuilder { 
            get {
                return _expressionBuilder;
            }
            set { 
                _expressionBuilder = value;
            } 
        } 

        public string ExpressionPrefix { 
            get {
                return _expressionPrefix;
            }
            set { 
                _expressionPrefix = value;
            } 
        } 

        /// <devdoc> 
        /// The name of the data field that is being bound to.
        /// </devdoc>
        public string FieldName {
            get { 
                return _fieldName;
            } 
            set { 
                _fieldName = value;
            } 
        }

        /// <devdoc>
        /// The format string applied to the field for display. 
        /// </devdoc>
        public string FormatString { 
            get { 
                return _formatString;
            } 
            set {
                _formatString = value;
            }
        } 

        internal bool IsDataBindingEntry { 
            // Empty prefix means it's a databinding expression (i.e. <%# ... %>) 
            get { return String.IsNullOrEmpty(ExpressionPrefix); }
        } 

        public bool Generated {
            get {
                return _generated; 
            }
            set { 
                _generated = value; 
            }
        } 

        public object ParsedExpressionData {
            get {
                return _parsedExpressionData; 
            }
            set { 
                _parsedExpressionData = value; 
            }
        } 

        /// <devdoc>
        /// Indicates whether the two way statement is set and get, or just get but not set.
        /// </devdoc> 
        public bool ReadOnlyProperty {
            get { 
                return _readOnlyProperty; 
            }
            set { 
                _readOnlyProperty = value;
            }
        }
 
        public bool TwoWayBound {
            get { 
                return _twoWayBound; 
            }
            set { 
                _twoWayBound = value;
            }
        }
 
        /// <devdoc>
        /// </devdoc> 
        public bool UseSetAttribute { 
            get {
                return _useSetAttribute; 
            }
            set {
                _useSetAttribute = value;
            } 
        }
 
        // Parse the expression, and store the resulting object 
        internal void ParseExpression(ExpressionBuilderContext context) {
            if (Expression == null || ExpressionPrefix == null || ExpressionBuilder == null) 
                return;

            _parsedExpressionData = ExpressionBuilder.ParseExpression(Expression, Type, context);
        } 
    }
} 
 

