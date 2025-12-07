//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceCommandParser.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Web.UI.WebControls; 

    // 
 
    /// <devdoc>
    /// Helper class for parsing simple SQL SELECT commands to extract the 
    /// list of selected fields and which table they are being selected
    /// from. This it not a complete parser, it only handles limited scenarios.
    /// </devdoc>
    internal static class SqlDataSourceCommandParser { 

        private static bool ConsumeField(string s, int startIndex, System.Collections.Generic.List<string> parts) { 
            // Skip whitespace 
            while ((startIndex < s.Length) && (Char.IsWhiteSpace(s, startIndex))) {
                startIndex++; 
            }

            string fieldName;
            startIndex = ConsumeIdentifier(s, startIndex, out fieldName); 
            parts.Add(fieldName);
 
            return ExpectField(s, startIndex, parts); 
        }
 
        private static bool ConsumeFrom(string s, int startIndex, System.Collections.Generic.List<string> parts) {
            // Skip whitespace
            while ((startIndex < s.Length) && (Char.IsWhiteSpace(s, startIndex))) {
                startIndex++; 
            }
 
            // If we're at the end of the string, abort 
            if (startIndex + 5 >= s.Length) {
                return false; 
            }

            if (String.Compare(s, startIndex, "from", 0, 4, StringComparison.OrdinalIgnoreCase) == 0) {
                if (Char.IsWhiteSpace(s, startIndex + 4)) { 
                    return ConsumeTable(s, startIndex + 5, parts);
                } 
            } 

            return false; 
        }

        private static int ConsumeIdentifier(string s, int startIndex, out string identifier) {
            bool inBracket = false; 
            identifier = String.Empty;
 
            // 

            while (startIndex < s.Length) { 
                if ((!inBracket) && (s[startIndex] == '[')) {
                    inBracket = true;
                    identifier += s[startIndex];
                    startIndex++; 
                }
                else { 
                    if (inBracket && (s[startIndex] == ']')) { 
                        inBracket = false;
                        identifier += s[startIndex]; 
                        startIndex++;
                    }
                    else {
                        if (inBracket) { 
                            // In a bracket we'll take anything
                            identifier += s[startIndex]; 
                            startIndex++; 
                        }
                        else { 
                            // Not in a bracket, we'll only take certain chars
                            if ((!Char.IsWhiteSpace(s, startIndex)) && (s[startIndex] != ',')) {
                                identifier += s[startIndex];
                                startIndex++; 
                            }
                            else { 
                                break; 
                            }
                        } 
                    }
                }
            }
 
            return startIndex;
        } 
 
        private static bool ConsumeSelect(string s, int startIndex, System.Collections.Generic.List<string> parts) {
            if (s.Length < 7) { 
                return false;
            }

            if (!s.ToLowerInvariant().StartsWith("select", StringComparison.Ordinal)) { 
                return false;
            } 
 
            if (!Char.IsWhiteSpace(s, 6)) {
                return false; 
            }

            return ConsumeField(s, startIndex + 7, parts);
        } 

        private static bool ConsumeTable(string s, int startIndex, System.Collections.Generic.List<string> parts) { 
            // Skip whitespace 
            while ((startIndex < s.Length) && (Char.IsWhiteSpace(s, startIndex))) {
                startIndex++; 
            }

            string fieldName;
            startIndex = ConsumeIdentifier(s, startIndex, out fieldName); 
            parts.Add(fieldName);
 
            // Make sure there is nothing after this 
            if (startIndex == s.Length) {
                return true; 
            }
            else {
                return false;
            } 
        }
 
        private static bool ExpectField(string s, int startIndex, System.Collections.Generic.List<string> parts) { 
            // Skip whitespace
            while ((startIndex < s.Length) && (Char.IsWhiteSpace(s, startIndex))) { 
                startIndex++;
            }

            // If we're at the end of the string, abort 
            if (startIndex >= s.Length - 1) {
                return false; 
            } 

            if (s[startIndex] == ',') { 
                // A comma indicates there are more fields coming
                return ConsumeField(s, startIndex + 1, parts);
            }
            else { 
                // No comma, we are done with fields
                return ConsumeFrom(s, startIndex, parts); 
            } 
        }
 
        /// <devdoc>
        /// Splits a SQL identifier such as [db].[user].[object] into its parts:
        /// db, user, object
        /// </devdoc> 
        private static string[] GetIdentifierParts(string identifier) {
            Debug.Assert(identifier != null); 
            bool inBrackets = false; 
            StringBuilder sb = new StringBuilder();
            ArrayList result = new ArrayList(); 
            for (int i = 0; i < identifier.Length; i++) {
                char c = identifier[i];
                switch (c) {
                    case '[': 
                        if (inBrackets) {
                            Debug.Fail(String.Format(CultureInfo.InvariantCulture, "The identifier {0} contains invalid characters", identifier)); 
                            return null; 
                        }
                        inBrackets = true; 
                        break;

                    case ']':
                        if ((inBrackets == false) || (identifier.Length > i + 2) && (identifier[i + 1] != '.')) { 
                            Debug.Fail(String.Format(CultureInfo.InvariantCulture, "The identifier {0} contains invalid characters", identifier));
                            return null; 
                        } 
                        inBrackets = false;
                        break; 

                    case '.':
                        if (inBrackets) {
                            sb.Append('.'); 
                        }
                        else { 
                            result.Add(sb.ToString()); 
                            sb.Length = 0;
                        } 
                        break;

                    default:
                        if (inBrackets == false) { 
                            switch (c) {
                                case '@': 
                                case '#': 
                                case '_':
                                case '*': // This allows [foo].* to work 
                                    break;

                                default:
                                    if (Char.IsLetter(c)) { 
                                        break;
                                    } 
                                    // First char can't be $ or digit 
                                    if ((sb.Length > 0) && ((c == '$') || Char.IsDigit(c))) {
                                        break; 
                                    }

                                    Debug.Fail(String.Format(CultureInfo.InvariantCulture, "The identifier {0} contains invalid characters", identifier));
                                    return null; 
                            }
                        } 
                        sb.Append(c); 

                        break; 
                }
            }

            result.Add(sb.ToString()); 

            return (string[])result.ToArray(typeof(string)); 
        } 

        /// <devdoc> 
        /// Parses an identifier such as [foo].[bar] and returns just the
        /// "bar" part.
        /// </devdoc>
        public static string GetLastIdentifierPart(string identifier) { 
            string[] parts = GetIdentifierParts(identifier);
            if ((parts == null) || (parts.Length == 0)) { 
                return null; 
            }
            else { 
                return parts[parts.Length - 1];
            }
        }
 
        /// <devdoc>
        /// Parses a simple SQL command string and extracts the list of 
        /// selected fields and the table name. The return value is null if 
        /// there was any problem parsing the string. The last field returned
        /// is really the name of the table being selected from, not a field. 
        /// </devdoc>
        public static string[] ParseSqlString(string sqlString) {
            if (String.IsNullOrEmpty(sqlString)) {
                return null; 
            }
 
            try { 
                sqlString = sqlString.Trim();
 
                System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
                bool success = ConsumeSelect(sqlString, 0, parts);

                return (success ? parts.ToArray() : null); 
            }
            catch (Exception ex) { 
                Debug.Fail("Caught an exception in SqlParser: " + ex.Message); 
                return null;
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SqlDataSourceCommandParser.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
 
    using System;
    using System.Collections; 
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Web.UI.WebControls; 

    // 
 
    /// <devdoc>
    /// Helper class for parsing simple SQL SELECT commands to extract the 
    /// list of selected fields and which table they are being selected
    /// from. This it not a complete parser, it only handles limited scenarios.
    /// </devdoc>
    internal static class SqlDataSourceCommandParser { 

        private static bool ConsumeField(string s, int startIndex, System.Collections.Generic.List<string> parts) { 
            // Skip whitespace 
            while ((startIndex < s.Length) && (Char.IsWhiteSpace(s, startIndex))) {
                startIndex++; 
            }

            string fieldName;
            startIndex = ConsumeIdentifier(s, startIndex, out fieldName); 
            parts.Add(fieldName);
 
            return ExpectField(s, startIndex, parts); 
        }
 
        private static bool ConsumeFrom(string s, int startIndex, System.Collections.Generic.List<string> parts) {
            // Skip whitespace
            while ((startIndex < s.Length) && (Char.IsWhiteSpace(s, startIndex))) {
                startIndex++; 
            }
 
            // If we're at the end of the string, abort 
            if (startIndex + 5 >= s.Length) {
                return false; 
            }

            if (String.Compare(s, startIndex, "from", 0, 4, StringComparison.OrdinalIgnoreCase) == 0) {
                if (Char.IsWhiteSpace(s, startIndex + 4)) { 
                    return ConsumeTable(s, startIndex + 5, parts);
                } 
            } 

            return false; 
        }

        private static int ConsumeIdentifier(string s, int startIndex, out string identifier) {
            bool inBracket = false; 
            identifier = String.Empty;
 
            // 

            while (startIndex < s.Length) { 
                if ((!inBracket) && (s[startIndex] == '[')) {
                    inBracket = true;
                    identifier += s[startIndex];
                    startIndex++; 
                }
                else { 
                    if (inBracket && (s[startIndex] == ']')) { 
                        inBracket = false;
                        identifier += s[startIndex]; 
                        startIndex++;
                    }
                    else {
                        if (inBracket) { 
                            // In a bracket we'll take anything
                            identifier += s[startIndex]; 
                            startIndex++; 
                        }
                        else { 
                            // Not in a bracket, we'll only take certain chars
                            if ((!Char.IsWhiteSpace(s, startIndex)) && (s[startIndex] != ',')) {
                                identifier += s[startIndex];
                                startIndex++; 
                            }
                            else { 
                                break; 
                            }
                        } 
                    }
                }
            }
 
            return startIndex;
        } 
 
        private static bool ConsumeSelect(string s, int startIndex, System.Collections.Generic.List<string> parts) {
            if (s.Length < 7) { 
                return false;
            }

            if (!s.ToLowerInvariant().StartsWith("select", StringComparison.Ordinal)) { 
                return false;
            } 
 
            if (!Char.IsWhiteSpace(s, 6)) {
                return false; 
            }

            return ConsumeField(s, startIndex + 7, parts);
        } 

        private static bool ConsumeTable(string s, int startIndex, System.Collections.Generic.List<string> parts) { 
            // Skip whitespace 
            while ((startIndex < s.Length) && (Char.IsWhiteSpace(s, startIndex))) {
                startIndex++; 
            }

            string fieldName;
            startIndex = ConsumeIdentifier(s, startIndex, out fieldName); 
            parts.Add(fieldName);
 
            // Make sure there is nothing after this 
            if (startIndex == s.Length) {
                return true; 
            }
            else {
                return false;
            } 
        }
 
        private static bool ExpectField(string s, int startIndex, System.Collections.Generic.List<string> parts) { 
            // Skip whitespace
            while ((startIndex < s.Length) && (Char.IsWhiteSpace(s, startIndex))) { 
                startIndex++;
            }

            // If we're at the end of the string, abort 
            if (startIndex >= s.Length - 1) {
                return false; 
            } 

            if (s[startIndex] == ',') { 
                // A comma indicates there are more fields coming
                return ConsumeField(s, startIndex + 1, parts);
            }
            else { 
                // No comma, we are done with fields
                return ConsumeFrom(s, startIndex, parts); 
            } 
        }
 
        /// <devdoc>
        /// Splits a SQL identifier such as [db].[user].[object] into its parts:
        /// db, user, object
        /// </devdoc> 
        private static string[] GetIdentifierParts(string identifier) {
            Debug.Assert(identifier != null); 
            bool inBrackets = false; 
            StringBuilder sb = new StringBuilder();
            ArrayList result = new ArrayList(); 
            for (int i = 0; i < identifier.Length; i++) {
                char c = identifier[i];
                switch (c) {
                    case '[': 
                        if (inBrackets) {
                            Debug.Fail(String.Format(CultureInfo.InvariantCulture, "The identifier {0} contains invalid characters", identifier)); 
                            return null; 
                        }
                        inBrackets = true; 
                        break;

                    case ']':
                        if ((inBrackets == false) || (identifier.Length > i + 2) && (identifier[i + 1] != '.')) { 
                            Debug.Fail(String.Format(CultureInfo.InvariantCulture, "The identifier {0} contains invalid characters", identifier));
                            return null; 
                        } 
                        inBrackets = false;
                        break; 

                    case '.':
                        if (inBrackets) {
                            sb.Append('.'); 
                        }
                        else { 
                            result.Add(sb.ToString()); 
                            sb.Length = 0;
                        } 
                        break;

                    default:
                        if (inBrackets == false) { 
                            switch (c) {
                                case '@': 
                                case '#': 
                                case '_':
                                case '*': // This allows [foo].* to work 
                                    break;

                                default:
                                    if (Char.IsLetter(c)) { 
                                        break;
                                    } 
                                    // First char can't be $ or digit 
                                    if ((sb.Length > 0) && ((c == '$') || Char.IsDigit(c))) {
                                        break; 
                                    }

                                    Debug.Fail(String.Format(CultureInfo.InvariantCulture, "The identifier {0} contains invalid characters", identifier));
                                    return null; 
                            }
                        } 
                        sb.Append(c); 

                        break; 
                }
            }

            result.Add(sb.ToString()); 

            return (string[])result.ToArray(typeof(string)); 
        } 

        /// <devdoc> 
        /// Parses an identifier such as [foo].[bar] and returns just the
        /// "bar" part.
        /// </devdoc>
        public static string GetLastIdentifierPart(string identifier) { 
            string[] parts = GetIdentifierParts(identifier);
            if ((parts == null) || (parts.Length == 0)) { 
                return null; 
            }
            else { 
                return parts[parts.Length - 1];
            }
        }
 
        /// <devdoc>
        /// Parses a simple SQL command string and extracts the list of 
        /// selected fields and the table name. The return value is null if 
        /// there was any problem parsing the string. The last field returned
        /// is really the name of the table being selected from, not a field. 
        /// </devdoc>
        public static string[] ParseSqlString(string sqlString) {
            if (String.IsNullOrEmpty(sqlString)) {
                return null; 
            }
 
            try { 
                sqlString = sqlString.Trim();
 
                System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
                bool success = ConsumeSelect(sqlString, 0, parts);

                return (success ? parts.ToArray() : null); 
            }
            catch (Exception ex) { 
                Debug.Fail("Caught an exception in SqlParser: " + ex.Message); 
                return null;
            } 
        }
    }
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
