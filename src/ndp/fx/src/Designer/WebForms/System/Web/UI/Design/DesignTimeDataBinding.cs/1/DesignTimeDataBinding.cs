//------------------------------------------------------------------------------ 
// <copyright file="DesignTimeDataBinding.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System.ComponentModel; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.RegularExpressions;
    using System.Web.UI; 

    /// <summary> 
    /// </summary> 
    internal sealed class DesignTimeDataBinding {
 
        private static readonly Regex EvalRegex = new EvalExpressionRegex();
        private static readonly Regex BindExpressionRegex = new BindExpressionRegex();
        private static readonly Regex BindParametersRegex = new BindParametersRegex();
 
        private DataBinding _runtimeDataBinding;
        private bool _parsed; 
        private bool _twoWayBinding; 
        private string _field;
        private string _format; 
        private string _expression;

        public DesignTimeDataBinding(DataBinding runtimeDataBinding) {
            _runtimeDataBinding = runtimeDataBinding; 
        }
 
        public DesignTimeDataBinding(PropertyDescriptor propDesc, string expression) { 
            _expression = expression;
 
            _runtimeDataBinding = new DataBinding(propDesc.Name, propDesc.PropertyType, expression);
        }

        public DesignTimeDataBinding(PropertyDescriptor propDesc, string field, string format, bool twoWayBinding) { 
            _field = field;
            _format = format; 
            if (twoWayBinding) { 
                _expression = CreateBindExpression(field, format);
            } 
            else {
                _expression = CreateEvalExpression(field, format);
            }
            _parsed = true; 
            _twoWayBinding = twoWayBinding;
 
            _runtimeDataBinding = new DataBinding(propDesc.Name, propDesc.PropertyType, _expression); 
        }
 
        public bool IsCustom {
            get {
                EnsureParsed();
                return (_field == null); 
            }
        } 
 
        public string Expression {
            get { 
                EnsureParsed();
                return _expression;
            }
        } 

        public string Field { 
            get { 
                EnsureParsed();
 
                Debug.Assert(IsCustom == false);
                return _field;
            }
        } 

        public string Format { 
            get { 
                EnsureParsed();
 
                Debug.Assert(IsCustom == false);
                return _format;
            }
        } 

        public bool IsTwoWayBound { 
            get { 
                EnsureParsed();
                return _twoWayBinding; 
            }
        }

        public DataBinding RuntimeDataBinding { 
            get {
                return _runtimeDataBinding; 
            } 
        }
 
        public static string CreateBindExpression(string field, string format) {
            Debug.Assert((field != null) && (field.Length != 0));
            string bindFieldName = field;
            bool hasBrackets = false; 

            for (int i = 0; i < field.Length; i++) { 
                char currentChar = field[i]; 
                if (!Char.IsLetterOrDigit(currentChar) && currentChar != '_') {
                    if (!hasBrackets) { 
                        bindFieldName = "[" + field + "]";
                        hasBrackets = true;
                    }
                } 
            }
 
            if ((format != null) && (format.Length != 0)) { 
                return String.Format(CultureInfo.InvariantCulture, "Bind(\"{0}\", \"{1}\")", bindFieldName, format);
            } 
            else {
                return String.Format(CultureInfo.InvariantCulture, "Bind(\"{0}\")", bindFieldName);
            }
        } 

        public static string CreateEvalExpression(string field, string format) { 
            Debug.Assert((field != null) && (field.Length != 0)); 
            string evalFieldName = field;
            bool hasBrackets = false; 

            for (int i = 0; i < field.Length; i++) {
                char currentChar = field[i];
                if (!Char.IsLetterOrDigit(currentChar) && currentChar != '_') { 
                    if (!hasBrackets) {
                        evalFieldName = "[" + field + "]"; 
                        hasBrackets = true; 
                    }
                } 
            }

            if ((format != null) && (format.Length != 0)) {
                return String.Format(CultureInfo.InvariantCulture, "Eval(\"{0}\", \"{1}\")", evalFieldName, format); 
            }
            else { 
                return String.Format(CultureInfo.InvariantCulture, "Eval(\"{0}\")", evalFieldName); 
            }
        } 

        private void EnsureParsed() {
            if (_parsed == false) {
                _expression = _runtimeDataBinding.Expression.Trim(); 
                if (_expression.Length != 0) {
                    try { 
                        bool evalMatch = false; 

                        Match match = EvalRegex.Match(_expression); 
                        if (match.Success) {
                             evalMatch = true;
                        }
                        else { 
                            match = BindExpressionRegex.Match(_expression);
                        } 
 
                        if (match.Success) {
                            string paramString = match.Groups["params"].Value; 

                            if ((match = BindParametersRegex.Match(paramString, 0)).Success) {
                                _field = match.Groups["fieldName"].Value;
                                Group formatStringGroup = match.Groups["formatString"]; 
                                if (formatStringGroup != null) {
                                    _format = formatStringGroup.Value; 
                                } 

                                if (!evalMatch) { 
                                    _twoWayBinding = true;
                                }
                            }
                        } 
                    }
                    catch (Exception e) { 
                        Debug.Fail(e.ToString()); 
                    }
                } 
            }

            _parsed = true;
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignTimeDataBinding.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System.ComponentModel; 
    using System.Diagnostics;
    using System.Globalization; 
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.RegularExpressions;
    using System.Web.UI; 

    /// <summary> 
    /// </summary> 
    internal sealed class DesignTimeDataBinding {
 
        private static readonly Regex EvalRegex = new EvalExpressionRegex();
        private static readonly Regex BindExpressionRegex = new BindExpressionRegex();
        private static readonly Regex BindParametersRegex = new BindParametersRegex();
 
        private DataBinding _runtimeDataBinding;
        private bool _parsed; 
        private bool _twoWayBinding; 
        private string _field;
        private string _format; 
        private string _expression;

        public DesignTimeDataBinding(DataBinding runtimeDataBinding) {
            _runtimeDataBinding = runtimeDataBinding; 
        }
 
        public DesignTimeDataBinding(PropertyDescriptor propDesc, string expression) { 
            _expression = expression;
 
            _runtimeDataBinding = new DataBinding(propDesc.Name, propDesc.PropertyType, expression);
        }

        public DesignTimeDataBinding(PropertyDescriptor propDesc, string field, string format, bool twoWayBinding) { 
            _field = field;
            _format = format; 
            if (twoWayBinding) { 
                _expression = CreateBindExpression(field, format);
            } 
            else {
                _expression = CreateEvalExpression(field, format);
            }
            _parsed = true; 
            _twoWayBinding = twoWayBinding;
 
            _runtimeDataBinding = new DataBinding(propDesc.Name, propDesc.PropertyType, _expression); 
        }
 
        public bool IsCustom {
            get {
                EnsureParsed();
                return (_field == null); 
            }
        } 
 
        public string Expression {
            get { 
                EnsureParsed();
                return _expression;
            }
        } 

        public string Field { 
            get { 
                EnsureParsed();
 
                Debug.Assert(IsCustom == false);
                return _field;
            }
        } 

        public string Format { 
            get { 
                EnsureParsed();
 
                Debug.Assert(IsCustom == false);
                return _format;
            }
        } 

        public bool IsTwoWayBound { 
            get { 
                EnsureParsed();
                return _twoWayBinding; 
            }
        }

        public DataBinding RuntimeDataBinding { 
            get {
                return _runtimeDataBinding; 
            } 
        }
 
        public static string CreateBindExpression(string field, string format) {
            Debug.Assert((field != null) && (field.Length != 0));
            string bindFieldName = field;
            bool hasBrackets = false; 

            for (int i = 0; i < field.Length; i++) { 
                char currentChar = field[i]; 
                if (!Char.IsLetterOrDigit(currentChar) && currentChar != '_') {
                    if (!hasBrackets) { 
                        bindFieldName = "[" + field + "]";
                        hasBrackets = true;
                    }
                } 
            }
 
            if ((format != null) && (format.Length != 0)) { 
                return String.Format(CultureInfo.InvariantCulture, "Bind(\"{0}\", \"{1}\")", bindFieldName, format);
            } 
            else {
                return String.Format(CultureInfo.InvariantCulture, "Bind(\"{0}\")", bindFieldName);
            }
        } 

        public static string CreateEvalExpression(string field, string format) { 
            Debug.Assert((field != null) && (field.Length != 0)); 
            string evalFieldName = field;
            bool hasBrackets = false; 

            for (int i = 0; i < field.Length; i++) {
                char currentChar = field[i];
                if (!Char.IsLetterOrDigit(currentChar) && currentChar != '_') { 
                    if (!hasBrackets) {
                        evalFieldName = "[" + field + "]"; 
                        hasBrackets = true; 
                    }
                } 
            }

            if ((format != null) && (format.Length != 0)) {
                return String.Format(CultureInfo.InvariantCulture, "Eval(\"{0}\", \"{1}\")", evalFieldName, format); 
            }
            else { 
                return String.Format(CultureInfo.InvariantCulture, "Eval(\"{0}\")", evalFieldName); 
            }
        } 

        private void EnsureParsed() {
            if (_parsed == false) {
                _expression = _runtimeDataBinding.Expression.Trim(); 
                if (_expression.Length != 0) {
                    try { 
                        bool evalMatch = false; 

                        Match match = EvalRegex.Match(_expression); 
                        if (match.Success) {
                             evalMatch = true;
                        }
                        else { 
                            match = BindExpressionRegex.Match(_expression);
                        } 
 
                        if (match.Success) {
                            string paramString = match.Groups["params"].Value; 

                            if ((match = BindParametersRegex.Match(paramString, 0)).Success) {
                                _field = match.Groups["fieldName"].Value;
                                Group formatStringGroup = match.Groups["formatString"]; 
                                if (formatStringGroup != null) {
                                    _format = formatStringGroup.Value; 
                                } 

                                if (!evalMatch) { 
                                    _twoWayBinding = true;
                                }
                            }
                        } 
                    }
                    catch (Exception e) { 
                        Debug.Fail(e.ToString()); 
                    }
                } 
            }

            _parsed = true;
        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
