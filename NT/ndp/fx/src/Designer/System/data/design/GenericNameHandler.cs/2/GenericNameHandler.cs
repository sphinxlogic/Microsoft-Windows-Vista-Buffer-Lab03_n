//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.Collections;
    using System.Globalization;
    using System.CodeDom.Compiler;
 

    internal sealed class GenericNameHandler { 
        private MemberNameValidator validator = null; 

        private Hashtable names = null; 

        internal GenericNameHandler(ICollection initialNameSet, CodeDomProvider codeProvider) {
            validator = new MemberNameValidator(initialNameSet, codeProvider, true /*languageCaseInsensitive*/);
            names = new Hashtable(StringComparer.Ordinal); 
        }
 
        internal string AddParameterNameToList(string originalName, string parameterPrefix) { 
            if (originalName == null) {
                throw new ArgumentNullException("originalName"); 
            }

            string noPrefixOriginalName = originalName;
            if (!StringUtil.Empty(parameterPrefix)) { 
                if (originalName.StartsWith(parameterPrefix, StringComparison.Ordinal)) {
                    noPrefixOriginalName = originalName.Substring(parameterPrefix.Length); 
                } 
            }
 
            string validatedName = validator.GetNewMemberName(noPrefixOriginalName);
            names.Add(originalName, validatedName);

            return validatedName; 
        }
 
        internal string AddNameToList(string originalName) { 
            if(originalName == null) {
                throw new InternalException("Parameter originalName should not be null."); 
            }

            string validatedName = validator.GetNewMemberName(originalName);
            names.Add(originalName, validatedName); 

            return validatedName; 
        } 

        internal string GetNameFromList(string originalName) { 
            if(originalName == null) {
                throw new InternalException("Parameter originalName should not be null.");
            }
 
            return (string) names[originalName];
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.Collections;
    using System.Globalization;
    using System.CodeDom.Compiler;
 

    internal sealed class GenericNameHandler { 
        private MemberNameValidator validator = null; 

        private Hashtable names = null; 

        internal GenericNameHandler(ICollection initialNameSet, CodeDomProvider codeProvider) {
            validator = new MemberNameValidator(initialNameSet, codeProvider, true /*languageCaseInsensitive*/);
            names = new Hashtable(StringComparer.Ordinal); 
        }
 
        internal string AddParameterNameToList(string originalName, string parameterPrefix) { 
            if (originalName == null) {
                throw new ArgumentNullException("originalName"); 
            }

            string noPrefixOriginalName = originalName;
            if (!StringUtil.Empty(parameterPrefix)) { 
                if (originalName.StartsWith(parameterPrefix, StringComparison.Ordinal)) {
                    noPrefixOriginalName = originalName.Substring(parameterPrefix.Length); 
                } 
            }
 
            string validatedName = validator.GetNewMemberName(noPrefixOriginalName);
            names.Add(originalName, validatedName);

            return validatedName; 
        }
 
        internal string AddNameToList(string originalName) { 
            if(originalName == null) {
                throw new InternalException("Parameter originalName should not be null."); 
            }

            string validatedName = validator.GetNewMemberName(originalName);
            names.Add(originalName, validatedName); 

            return validatedName; 
        } 

        internal string GetNameFromList(string originalName) { 
            if(originalName == null) {
                throw new InternalException("Parameter originalName should not be null.");
            }
 
            return (string) names[originalName];
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
