//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceParameterParser.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Diagnostics;
    using System.IO;
    using System.Web.UI.WebControls;
 
    //
 
    /// <devdoc> 
    /// Helper class to get list of parameters from an arbitrary SQL query.
    /// </devdoc> 
    internal static class SqlDataSourceParameterParser {
        /// <devdoc>
        /// Parses a command and returns an array of parameter names.
        /// </devdoc> 
        public static Parameter[] ParseCommandText(string providerName, string commandText) {
            if (String.IsNullOrEmpty(providerName)) { 
                providerName = SqlDataSourceDesigner.DefaultProviderName; 
            }
            if (String.IsNullOrEmpty(commandText)) { 
                commandText = String.Empty;
            }

            ParameterParser parser = null; 

            switch (providerName.ToLowerInvariant()) { 
                case "system.data.sqlclient": 
                    parser = new SqlClientParameterParser();
                    break; 
                case "system.data.odbc":
                case "system.data.oledb":
                    parser = new MiscParameterParser();
                    break; 
                case "system.data.oracleclient":
                    parser = new OracleClientParameterParser(); 
                    break; 
            }
 
            if (parser == null) {
                Debug.Fail("Trying to parse parameters for unknown provider: " + providerName);
                return new Parameter[0];
            } 
            return parser.ParseCommandText(commandText);
        } 
 

        /// <devdoc> 
        /// Base class for parameter parsing classes.
        /// </devdoc>
        private abstract class ParameterParser {
            public abstract Parameter[] ParseCommandText(string commandText); 
        }
 
        /// <devdoc> 
        /// Parses SqlClient parameters of the form:
        /// select * from authors where col1 = @param1 
        /// </devdoc>
        private sealed class SqlClientParameterParser : ParameterParser {
            /// <devdoc>
            /// List of states for parser. 
            /// </devdoc>
            private enum State { 
                InText, 
                InQuote,
                InDoubleQuote, 
                InBracket,
                InParameter,
            }
 
            /// <devdoc>
            /// Returns true is a character is valid for a named parameter. 
            /// </devdoc> 
            private static bool IsValidParamNameChar(char c) {
                return (Char.IsLetterOrDigit(c)) || 
                    (c == '@') ||
                    (c == '$') ||
                    (c == '#') ||
                    (c == '_'); 
            }
 
            public override Parameter[] ParseCommandText(string commandText) { 
                int index = 0;
                int length = commandText.Length; 
                State state = State.InText;
                System.Collections.Generic.List<Parameter> parameters = new System.Collections.Generic.List<Parameter>();
                System.Collections.Specialized.StringCollection parameterNames = new System.Collections.Specialized.StringCollection();
 
                while (index < length) {
                    switch (state) { 
                        case State.InText: 
                            if (commandText[index] == '\'') {
                                state = State.InQuote; 
                            }
                            else if (commandText[index] == '"') {
                                state = State.InDoubleQuote;
                            } 
                            else if (commandText[index] == '[') {
                                state = State.InBracket; 
                            } 
                            else if (commandText[index] == '@') {
                                state = State.InParameter; 
                            }
                            else {
                                index++;
                            } 
                            break;
 
                        case State.InQuote: 
                            index++;
                            while (index < length && commandText[index] != '\'') { 
                                index++;
                            }
                            index++;
                            state = State.InText; 
                            break;
 
                        case State.InDoubleQuote: 
                            index++;
                            while (index < length && commandText[index] != '"') { 
                                index++;
                            }
                            index++;
                            state = State.InText; 
                            break;
 
                        case State.InBracket: 
                            index++;
                            while (index < length && commandText[index] != ']') { 
                                index++;
                            }
                            index++;
                            state = State.InText; 
                            break;
 
                        case State.InParameter: 
                            index++;
                            string paramName = String.Empty; 
                            while ((index < length) && (IsValidParamNameChar(commandText[index]))) {
                                paramName += commandText[index];
                                index++;
                            } 

                            // Ignore @@functions since they aren't parameters 
                            if (!paramName.StartsWith("@", StringComparison.Ordinal)) { 
                                Parameter p = new Parameter(paramName);
                                // Ignore duplicate parameters 
                                if (!parameterNames.Contains(paramName)) {
                                    parameters.Add(p);
                                    parameterNames.Add(paramName);
                                } 
                            }
 
                            state = State.InText; 
                            break;
                    } 
                }

                return parameters.ToArray();
            } 
        }
 
        /// <devdoc> 
        /// Parses ODBC and OLEDB parameters of the form:
        /// select * from authors where col1 = ? 
        /// </devdoc>
        private sealed class MiscParameterParser : ParameterParser {
            /// <devdoc>
            /// List of states for parser. 
            /// </devdoc>
            private enum State { 
                InText, 
                InQuote,
                InDoubleQuote, 
                InBracket,
                InQuestion,
            }
 
            public override Parameter[] ParseCommandText(string commandText) {
                int index = 0; 
                int length = commandText.Length; 
                State state = State.InText;
                System.Collections.Generic.List<Parameter> parameters = new System.Collections.Generic.List<Parameter>(); 

                while (index < length) {
                    switch (state) {
                        case State.InText: 
                            if (commandText[index] == '\'') {
                                state = State.InQuote; 
                            } 
                            else if (commandText[index] == '"') {
                                state = State.InDoubleQuote; 
                            }
                            else if (commandText[index] == '[') {
                                state = State.InBracket;
                            } 
                            else if (commandText[index] == '?') {
                                state = State.InQuestion; 
                            } 
                            else {
                                index++; 
                            }
                            break;

                        case State.InQuote: 
                            index++;
                            while (index < length && commandText[index] != '\'') { 
                                index++; 
                            }
                            index++; 
                            state = State.InText;
                            break;

                        case State.InDoubleQuote: 
                            index++;
                            while (index < length && commandText[index] != '"') { 
                                index++; 
                            }
                            index++; 
                            state = State.InText;
                            break;

                        case State.InBracket: 
                            index++;
                            while (index < length && commandText[index] != ']') { 
                                index++; 
                            }
                            index++; 
                            state = State.InText;
                            break;

                        case State.InQuestion: 
                            index++;
                            parameters.Add(new Parameter("?")); 
                            state = State.InText; 
                            break;
                    } 
                }

                return parameters.ToArray();
            } 
        }
 
        /// <devdoc> 
        /// Parses OracleClient parameters of the form:
        /// select * from authors where col1 = :param1 
        /// </devdoc>
        private sealed class OracleClientParameterParser : ParameterParser {
            /// <devdoc>
            /// List of states for parser. 
            /// </devdoc>
            private enum State { 
                InText, 
                InQuote,
                InDoubleQuote, 
                InBracket,
                InParameter,
            }
 
            /// <devdoc>
            /// Returns true is a character is valid for a named parameter. 
            /// </devdoc> 
            private static bool IsValidParamNameChar(char c) {
                // 
                return (Char.IsLetterOrDigit(c)) ||
                    (c == '_');
            }
 
            public override Parameter[] ParseCommandText(string commandText) {
                int index = 0; 
                int length = commandText.Length; 
                State state = State.InText;
                System.Collections.Generic.List<Parameter> parameters = new System.Collections.Generic.List<Parameter>(); 
                System.Collections.Specialized.StringCollection parameterNames = new System.Collections.Specialized.StringCollection();

                while (index < length) {
                    switch (state) { 
                        case State.InText:
                            if (commandText[index] == '\'') { 
                                state = State.InQuote; 
                            }
                            else if (commandText[index] == '"') { 
                                state = State.InDoubleQuote;
                            }
                            else if (commandText[index] == '[') {
                                state = State.InBracket; 
                            }
                            else if (commandText[index] == ':') { 
                                state = State.InParameter; 
                            }
                            else { 
                                index++;
                            }
                            break;
 
                        case State.InQuote:
                            index++; 
                            while (index < length && commandText[index] != '\'') { 
                                index++;
                            } 
                            index++;
                            state = State.InText;
                            break;
 
                        case State.InDoubleQuote:
                            index++; 
                            while (index < length && commandText[index] != '"') { 
                                index++;
                            } 
                            index++;
                            state = State.InText;
                            break;
 
                        case State.InBracket:
                            index++; 
                            while (index < length && commandText[index] != ']') { 
                                index++;
                            } 
                            index++;
                            state = State.InText;
                            break;
 
                        case State.InParameter:
                            index++; 
                            string paramName = String.Empty; 
                            while ((index < length) && (IsValidParamNameChar(commandText[index]))) {
                                paramName += commandText[index]; 
                                index++;
                            }

                            Parameter p = new Parameter(paramName); 
                            // Ignore duplicate parameters
                            if (!parameterNames.Contains(paramName)) { 
                                parameters.Add(p); 
                                parameterNames.Add(paramName);
                            } 

                            state = State.InText;
                            break;
                    } 
                }
 
                return parameters.ToArray(); 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceParameterParser.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Diagnostics;
    using System.IO;
    using System.Web.UI.WebControls;
 
    //
 
    /// <devdoc> 
    /// Helper class to get list of parameters from an arbitrary SQL query.
    /// </devdoc> 
    internal static class SqlDataSourceParameterParser {
        /// <devdoc>
        /// Parses a command and returns an array of parameter names.
        /// </devdoc> 
        public static Parameter[] ParseCommandText(string providerName, string commandText) {
            if (String.IsNullOrEmpty(providerName)) { 
                providerName = SqlDataSourceDesigner.DefaultProviderName; 
            }
            if (String.IsNullOrEmpty(commandText)) { 
                commandText = String.Empty;
            }

            ParameterParser parser = null; 

            switch (providerName.ToLowerInvariant()) { 
                case "system.data.sqlclient": 
                    parser = new SqlClientParameterParser();
                    break; 
                case "system.data.odbc":
                case "system.data.oledb":
                    parser = new MiscParameterParser();
                    break; 
                case "system.data.oracleclient":
                    parser = new OracleClientParameterParser(); 
                    break; 
            }
 
            if (parser == null) {
                Debug.Fail("Trying to parse parameters for unknown provider: " + providerName);
                return new Parameter[0];
            } 
            return parser.ParseCommandText(commandText);
        } 
 

        /// <devdoc> 
        /// Base class for parameter parsing classes.
        /// </devdoc>
        private abstract class ParameterParser {
            public abstract Parameter[] ParseCommandText(string commandText); 
        }
 
        /// <devdoc> 
        /// Parses SqlClient parameters of the form:
        /// select * from authors where col1 = @param1 
        /// </devdoc>
        private sealed class SqlClientParameterParser : ParameterParser {
            /// <devdoc>
            /// List of states for parser. 
            /// </devdoc>
            private enum State { 
                InText, 
                InQuote,
                InDoubleQuote, 
                InBracket,
                InParameter,
            }
 
            /// <devdoc>
            /// Returns true is a character is valid for a named parameter. 
            /// </devdoc> 
            private static bool IsValidParamNameChar(char c) {
                return (Char.IsLetterOrDigit(c)) || 
                    (c == '@') ||
                    (c == '$') ||
                    (c == '#') ||
                    (c == '_'); 
            }
 
            public override Parameter[] ParseCommandText(string commandText) { 
                int index = 0;
                int length = commandText.Length; 
                State state = State.InText;
                System.Collections.Generic.List<Parameter> parameters = new System.Collections.Generic.List<Parameter>();
                System.Collections.Specialized.StringCollection parameterNames = new System.Collections.Specialized.StringCollection();
 
                while (index < length) {
                    switch (state) { 
                        case State.InText: 
                            if (commandText[index] == '\'') {
                                state = State.InQuote; 
                            }
                            else if (commandText[index] == '"') {
                                state = State.InDoubleQuote;
                            } 
                            else if (commandText[index] == '[') {
                                state = State.InBracket; 
                            } 
                            else if (commandText[index] == '@') {
                                state = State.InParameter; 
                            }
                            else {
                                index++;
                            } 
                            break;
 
                        case State.InQuote: 
                            index++;
                            while (index < length && commandText[index] != '\'') { 
                                index++;
                            }
                            index++;
                            state = State.InText; 
                            break;
 
                        case State.InDoubleQuote: 
                            index++;
                            while (index < length && commandText[index] != '"') { 
                                index++;
                            }
                            index++;
                            state = State.InText; 
                            break;
 
                        case State.InBracket: 
                            index++;
                            while (index < length && commandText[index] != ']') { 
                                index++;
                            }
                            index++;
                            state = State.InText; 
                            break;
 
                        case State.InParameter: 
                            index++;
                            string paramName = String.Empty; 
                            while ((index < length) && (IsValidParamNameChar(commandText[index]))) {
                                paramName += commandText[index];
                                index++;
                            } 

                            // Ignore @@functions since they aren't parameters 
                            if (!paramName.StartsWith("@", StringComparison.Ordinal)) { 
                                Parameter p = new Parameter(paramName);
                                // Ignore duplicate parameters 
                                if (!parameterNames.Contains(paramName)) {
                                    parameters.Add(p);
                                    parameterNames.Add(paramName);
                                } 
                            }
 
                            state = State.InText; 
                            break;
                    } 
                }

                return parameters.ToArray();
            } 
        }
 
        /// <devdoc> 
        /// Parses ODBC and OLEDB parameters of the form:
        /// select * from authors where col1 = ? 
        /// </devdoc>
        private sealed class MiscParameterParser : ParameterParser {
            /// <devdoc>
            /// List of states for parser. 
            /// </devdoc>
            private enum State { 
                InText, 
                InQuote,
                InDoubleQuote, 
                InBracket,
                InQuestion,
            }
 
            public override Parameter[] ParseCommandText(string commandText) {
                int index = 0; 
                int length = commandText.Length; 
                State state = State.InText;
                System.Collections.Generic.List<Parameter> parameters = new System.Collections.Generic.List<Parameter>(); 

                while (index < length) {
                    switch (state) {
                        case State.InText: 
                            if (commandText[index] == '\'') {
                                state = State.InQuote; 
                            } 
                            else if (commandText[index] == '"') {
                                state = State.InDoubleQuote; 
                            }
                            else if (commandText[index] == '[') {
                                state = State.InBracket;
                            } 
                            else if (commandText[index] == '?') {
                                state = State.InQuestion; 
                            } 
                            else {
                                index++; 
                            }
                            break;

                        case State.InQuote: 
                            index++;
                            while (index < length && commandText[index] != '\'') { 
                                index++; 
                            }
                            index++; 
                            state = State.InText;
                            break;

                        case State.InDoubleQuote: 
                            index++;
                            while (index < length && commandText[index] != '"') { 
                                index++; 
                            }
                            index++; 
                            state = State.InText;
                            break;

                        case State.InBracket: 
                            index++;
                            while (index < length && commandText[index] != ']') { 
                                index++; 
                            }
                            index++; 
                            state = State.InText;
                            break;

                        case State.InQuestion: 
                            index++;
                            parameters.Add(new Parameter("?")); 
                            state = State.InText; 
                            break;
                    } 
                }

                return parameters.ToArray();
            } 
        }
 
        /// <devdoc> 
        /// Parses OracleClient parameters of the form:
        /// select * from authors where col1 = :param1 
        /// </devdoc>
        private sealed class OracleClientParameterParser : ParameterParser {
            /// <devdoc>
            /// List of states for parser. 
            /// </devdoc>
            private enum State { 
                InText, 
                InQuote,
                InDoubleQuote, 
                InBracket,
                InParameter,
            }
 
            /// <devdoc>
            /// Returns true is a character is valid for a named parameter. 
            /// </devdoc> 
            private static bool IsValidParamNameChar(char c) {
                // 
                return (Char.IsLetterOrDigit(c)) ||
                    (c == '_');
            }
 
            public override Parameter[] ParseCommandText(string commandText) {
                int index = 0; 
                int length = commandText.Length; 
                State state = State.InText;
                System.Collections.Generic.List<Parameter> parameters = new System.Collections.Generic.List<Parameter>(); 
                System.Collections.Specialized.StringCollection parameterNames = new System.Collections.Specialized.StringCollection();

                while (index < length) {
                    switch (state) { 
                        case State.InText:
                            if (commandText[index] == '\'') { 
                                state = State.InQuote; 
                            }
                            else if (commandText[index] == '"') { 
                                state = State.InDoubleQuote;
                            }
                            else if (commandText[index] == '[') {
                                state = State.InBracket; 
                            }
                            else if (commandText[index] == ':') { 
                                state = State.InParameter; 
                            }
                            else { 
                                index++;
                            }
                            break;
 
                        case State.InQuote:
                            index++; 
                            while (index < length && commandText[index] != '\'') { 
                                index++;
                            } 
                            index++;
                            state = State.InText;
                            break;
 
                        case State.InDoubleQuote:
                            index++; 
                            while (index < length && commandText[index] != '"') { 
                                index++;
                            } 
                            index++;
                            state = State.InText;
                            break;
 
                        case State.InBracket:
                            index++; 
                            while (index < length && commandText[index] != ']') { 
                                index++;
                            } 
                            index++;
                            state = State.InText;
                            break;
 
                        case State.InParameter:
                            index++; 
                            string paramName = String.Empty; 
                            while ((index < length) && (IsValidParamNameChar(commandText[index]))) {
                                paramName += commandText[index]; 
                                index++;
                            }

                            Parameter p = new Parameter(paramName); 
                            // Ignore duplicate parameters
                            if (!parameterNames.Contains(paramName)) { 
                                parameters.Add(p); 
                                parameterNames.Add(paramName);
                            } 

                            state = State.InText;
                            break;
                    } 
                }
 
                return parameters.ToArray(); 
            }
        } 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
