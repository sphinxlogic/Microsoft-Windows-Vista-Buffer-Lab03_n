 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design { 
    using System;
    using System.Diagnostics; 
    using System.Globalization;

    /// <summary>
    /// This class stores some common used string utility functions 
    /// used by any class in this dll.
    /// </summary> 
    internal sealed class StringUtil { 

        /// <summary> 
        /// Private contstructor to avoid class being instantiated.
        /// </summary>
        private StringUtil() {
        } 

        /// <summary> 
        ///  Checks to see if the string is empty or null 
        /// </summary>
        /// <returns> 
        ///  true if the string is empty and not null
        /// </returns>
        internal static bool Empty(string str) {
            return ((null == str) || (0 >= str.Length)); 
        }
 
        /// <summary> 
        ///  Checks to see if the string is empty or null or only contains spaces
        /// </summary> 
        /// <returns>
        ///   true if the string is not empty and not null
        /// </returns>
        internal static bool EmptyOrSpace(string str) { 
            return ((null == str) || (0 >= (str.Trim()).Length));
        } 
 
        /// <summary>
        ///  Compare two strings with invariant culture and case sensitive 
        ///  Also consider the null cases
        /// </summary>
        internal static bool EqualValue(string str1, string str2){
            return EqualValue(str1, str2, false); 
        }
 
         /// <summary> 
        ///  Compare two strings with invariant culture and specified case sensitivity
        ///  Also consider the null cases 
        /// </summary>
        internal static bool EqualValue(string str1, string str2, bool caseInsensitive){
            if((str1 != null) && (str2 != null)) {
                StringComparison compararison = caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal; 
                return String.Equals(str1, str2, compararison);
            } 
            return str1 == str2; 
        }
 
        /// <summary>
        /// We need this function as the VSDesigner.Data.DesignUtil has this one
        /// We want the change ove to be seamless.
        /// </summary> 
        internal static bool NotEmpty(string str) {
            return !Empty(str); 
        } 

        /// <summary> 
        ///      Check the string is empty or null
        /// </summary>
        /// <returns>
        ///       true if the string is not empty and not null 
        /// </returns>
        public static bool NotEmptyAfterTrim(string str) { 
            return !EmptyOrSpace(str); 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design { 
    using System;
    using System.Diagnostics; 
    using System.Globalization;

    /// <summary>
    /// This class stores some common used string utility functions 
    /// used by any class in this dll.
    /// </summary> 
    internal sealed class StringUtil { 

        /// <summary> 
        /// Private contstructor to avoid class being instantiated.
        /// </summary>
        private StringUtil() {
        } 

        /// <summary> 
        ///  Checks to see if the string is empty or null 
        /// </summary>
        /// <returns> 
        ///  true if the string is empty and not null
        /// </returns>
        internal static bool Empty(string str) {
            return ((null == str) || (0 >= str.Length)); 
        }
 
        /// <summary> 
        ///  Checks to see if the string is empty or null or only contains spaces
        /// </summary> 
        /// <returns>
        ///   true if the string is not empty and not null
        /// </returns>
        internal static bool EmptyOrSpace(string str) { 
            return ((null == str) || (0 >= (str.Trim()).Length));
        } 
 
        /// <summary>
        ///  Compare two strings with invariant culture and case sensitive 
        ///  Also consider the null cases
        /// </summary>
        internal static bool EqualValue(string str1, string str2){
            return EqualValue(str1, str2, false); 
        }
 
         /// <summary> 
        ///  Compare two strings with invariant culture and specified case sensitivity
        ///  Also consider the null cases 
        /// </summary>
        internal static bool EqualValue(string str1, string str2, bool caseInsensitive){
            if((str1 != null) && (str2 != null)) {
                StringComparison compararison = caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal; 
                return String.Equals(str1, str2, compararison);
            } 
            return str1 == str2; 
        }
 
        /// <summary>
        /// We need this function as the VSDesigner.Data.DesignUtil has this one
        /// We want the change ove to be seamless.
        /// </summary> 
        internal static bool NotEmpty(string str) {
            return !Empty(str); 
        } 

        /// <summary> 
        ///      Check the string is empty or null
        /// </summary>
        /// <returns>
        ///       true if the string is not empty and not null 
        /// </returns>
        public static bool NotEmptyAfterTrim(string str) { 
            return !EmptyOrSpace(str); 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
