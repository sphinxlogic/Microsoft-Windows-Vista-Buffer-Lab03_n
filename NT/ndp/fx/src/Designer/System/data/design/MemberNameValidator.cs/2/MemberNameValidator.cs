//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.Data;
    using System.CodeDom;
    using System.Collections;
    using System.Collections.Generic; 
    using System.Globalization;
    using System.Reflection; 
    using System.CodeDom.Compiler; 

 
    internal sealed class MemberNameValidator {
        private const int maxGenerationAttempts = 200;
        private const int additionalTruncationChars = 100;
 
        private ArrayList bookedMemberNames = null;
        private CodeDomProvider codeProvider = null; 
        private bool languageCaseInsensitive = false; 
        private bool useSuffix = false;
 
        private static string[] invalidEverettIdentifiersVb = { "region", "externalsource" };
        private static Dictionary<string, string[]> invalidEverettIdentifiers = null;

 
        internal bool UseSuffix {
            get { 
                return useSuffix; 
            }
            set { 
                useSuffix = value;
            }
        }
 
        private static Dictionary<string, string[]> InvalidEverettIdentifiers {
            get { 
                if (invalidEverettIdentifiers == null) { 
                    invalidEverettIdentifiers = new Dictionary<string, string[]>();
                    invalidEverettIdentifiers.Add(".vb", invalidEverettIdentifiersVb); 
                }

                return invalidEverettIdentifiers;
            } 
        }
 
        internal MemberNameValidator(ICollection initialNameSet, CodeDomProvider codeProvider, bool languageCaseInsensitive) { 
            this.codeProvider = codeProvider;
            this.languageCaseInsensitive = languageCaseInsensitive; 

            if(initialNameSet != null) {
                bookedMemberNames = new ArrayList(initialNameSet.Count);
                foreach (string name in initialNameSet) { 
                    this.AddNameToList(name);
                } 
            } 
            else {
                bookedMemberNames = new ArrayList(); 
            }
        }

        internal string GetCandidateMemberName(string originalName) { 
            if(originalName == null) {
                throw new InternalException("Member name cannot be null."); 
            } 

            // generate valid identifier name 
            string validName = GenerateIdName(originalName);
            string baseName = validName;

            // generate unique name: if the name already exists, prefix an underscore to it 
            int attempt = 0;
            while(this.ListContains(validName)) { 
                attempt++; 
                validName = baseName + attempt.ToString(System.Globalization.CultureInfo.CurrentCulture);
                if(!codeProvider.IsValidIdentifier(validName)) { 
                    throw new InternalException(String.Format(System.Globalization.CultureInfo.CurrentCulture, "Unable to generate valid identifier from name: {0}.", originalName));
                }
                if (attempt > maxGenerationAttempts) {
                    throw new InternalException(String.Format(System.Globalization.CultureInfo.CurrentCulture, "Unable to generate unique identifier from name: {0}. Too many attempts.", originalName)); 
                }
            } 
 
            return validName;
        } 

        internal string GetNewMemberName(string originalName) {
            // generate valid and unique identifier name
            string validName = GetCandidateMemberName(originalName); 

            // add the new name to the already booked names 
            this.AddNameToList(validName); 

            return validName; 
        }

        // given a variable name, this method will check to see if the
        // name is a valid identifier name. if this is not the case, then 
        // it will replace all the invalid characters with underscores.
        internal string GenerateIdName(string name) { 
            return GenerateIdName(name, this.codeProvider, this.UseSuffix); 
        }
 
        internal static string GenerateIdName(string name, CodeDomProvider codeProvider, bool useSuffix) {
            return GenerateIdName(name, codeProvider, useSuffix, additionalTruncationChars);
        }
 
        internal static string GenerateIdName(string name, CodeDomProvider codeProvider, bool useSuffix, int additionalCharsToTruncate) {
            if (!useSuffix) { 
                // if we're generating an identifier that needs to be backward compatible with Everett, we need to take into 
                // account some changes made to the CodeDomProviders; some names used to be invalid, but are not any more in Whidbey.
                // For these names, we still need to generate the fixed up version, like in Everett. 
                name = GetBackwardCompatibleIdentifier(name, codeProvider);
            }

            if (codeProvider.IsValidIdentifier(name)) { 
                return name;
            } 
 
            string ret = name.Replace(' ', '_');
            if ( !codeProvider.IsValidIdentifier(ret) ) { 
                if (!useSuffix) {
                    ret = "_" + ret;
                }
                UnicodeCategory unc; 
                for (int i = 0; i < ret.Length; i++) {
                    unc = Char.GetUnicodeCategory(ret[i]); 
                    if ( 
                        UnicodeCategory.UppercaseLetter      != unc &&
                        UnicodeCategory.LowercaseLetter      != unc && 
                        UnicodeCategory.TitlecaseLetter      != unc &&
                        UnicodeCategory.ModifierLetter       != unc &&
                        UnicodeCategory.OtherLetter          != unc &&
                        UnicodeCategory.NonSpacingMark       != unc && 
                        UnicodeCategory.SpacingCombiningMark != unc &&
                        UnicodeCategory.DecimalDigitNumber   != unc && 
                        UnicodeCategory.ConnectorPunctuation != unc 
                        ) {
                        ret = ret.Replace(ret[i], '_'); 
                    } // if
                } // for
            }
 
            // let's make sure that what we generated is really valid
            int generationAttempt = 0; 
            string originalRet = ret; 
            while (!codeProvider.IsValidIdentifier(ret) && generationAttempt < maxGenerationAttempts) {
                generationAttempt++; 
                ret = "_" + ret;
            }
            if(generationAttempt >= maxGenerationAttempts) {
                ret = originalRet; 
                while (!codeProvider.IsValidIdentifier(ret) && ret.Length > 0) {
                    // try to truncate the identifier, maybe it's just too long. 
                    ret = ret.Remove(ret.Length - 1); 
                }
 
                if (ret.Length == 0) {
                    // if we get here we weren't able to generate a valid identifier according to the CodeDomProvider
                    // since sometimes the CodeDomProvider is wrong, let's return the fixed up identifier it may work.
                    return originalRet; 
                }
                else { 
                    if (additionalCharsToTruncate > 0 && ret.Length > additionalCharsToTruncate) { 
                        if (codeProvider.IsValidIdentifier(ret.Remove(ret.Length - additionalCharsToTruncate))) {
                            // truncate a bit more, because we may have to add chars to fix it up. 
                            ret = ret.Remove(ret.Length - additionalCharsToTruncate);
                        }
                    }
                } 
            }
 
            return ret; 
        }
 
        private void AddNameToList(string name) {
            if(this.languageCaseInsensitive) {
                bookedMemberNames.Add(name.ToUpperInvariant());
            } 
            else {
                bookedMemberNames.Add(name); 
            } 
        }
 
        private bool ListContains(string name) {
            if(this.languageCaseInsensitive) {
                return bookedMemberNames.Contains(name.ToUpperInvariant());
            } 
            else {
                return bookedMemberNames.Contains(name); 
            } 
        }
 
        private static string GetBackwardCompatibleIdentifier(string identifier, CodeDomProvider provider) {
            string languageExtension = "." + provider.FileExtension;
            if (languageExtension.StartsWith("..", StringComparison.Ordinal)) {
                languageExtension = languageExtension.Substring(1); 
            }
            if (InvalidEverettIdentifiers.ContainsKey(languageExtension)) { 
                string[] invalidIdentifiers = InvalidEverettIdentifiers[languageExtension]; 
                if (invalidIdentifiers != null) {
                    bool languageCaseInsensitive = (provider.LanguageOptions & LanguageOptions.CaseInsensitive) > 0; 
                    for (int i = 0; i < invalidIdentifiers.Length; i++) {
                        if (StringUtil.EqualValue(identifier, invalidIdentifiers[i], languageCaseInsensitive)) {
                            return "_" + identifier;
                        } 
                    }
                } 
            } 

            return identifier; 
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
    using System.Data;
    using System.CodeDom;
    using System.Collections;
    using System.Collections.Generic; 
    using System.Globalization;
    using System.Reflection; 
    using System.CodeDom.Compiler; 

 
    internal sealed class MemberNameValidator {
        private const int maxGenerationAttempts = 200;
        private const int additionalTruncationChars = 100;
 
        private ArrayList bookedMemberNames = null;
        private CodeDomProvider codeProvider = null; 
        private bool languageCaseInsensitive = false; 
        private bool useSuffix = false;
 
        private static string[] invalidEverettIdentifiersVb = { "region", "externalsource" };
        private static Dictionary<string, string[]> invalidEverettIdentifiers = null;

 
        internal bool UseSuffix {
            get { 
                return useSuffix; 
            }
            set { 
                useSuffix = value;
            }
        }
 
        private static Dictionary<string, string[]> InvalidEverettIdentifiers {
            get { 
                if (invalidEverettIdentifiers == null) { 
                    invalidEverettIdentifiers = new Dictionary<string, string[]>();
                    invalidEverettIdentifiers.Add(".vb", invalidEverettIdentifiersVb); 
                }

                return invalidEverettIdentifiers;
            } 
        }
 
        internal MemberNameValidator(ICollection initialNameSet, CodeDomProvider codeProvider, bool languageCaseInsensitive) { 
            this.codeProvider = codeProvider;
            this.languageCaseInsensitive = languageCaseInsensitive; 

            if(initialNameSet != null) {
                bookedMemberNames = new ArrayList(initialNameSet.Count);
                foreach (string name in initialNameSet) { 
                    this.AddNameToList(name);
                } 
            } 
            else {
                bookedMemberNames = new ArrayList(); 
            }
        }

        internal string GetCandidateMemberName(string originalName) { 
            if(originalName == null) {
                throw new InternalException("Member name cannot be null."); 
            } 

            // generate valid identifier name 
            string validName = GenerateIdName(originalName);
            string baseName = validName;

            // generate unique name: if the name already exists, prefix an underscore to it 
            int attempt = 0;
            while(this.ListContains(validName)) { 
                attempt++; 
                validName = baseName + attempt.ToString(System.Globalization.CultureInfo.CurrentCulture);
                if(!codeProvider.IsValidIdentifier(validName)) { 
                    throw new InternalException(String.Format(System.Globalization.CultureInfo.CurrentCulture, "Unable to generate valid identifier from name: {0}.", originalName));
                }
                if (attempt > maxGenerationAttempts) {
                    throw new InternalException(String.Format(System.Globalization.CultureInfo.CurrentCulture, "Unable to generate unique identifier from name: {0}. Too many attempts.", originalName)); 
                }
            } 
 
            return validName;
        } 

        internal string GetNewMemberName(string originalName) {
            // generate valid and unique identifier name
            string validName = GetCandidateMemberName(originalName); 

            // add the new name to the already booked names 
            this.AddNameToList(validName); 

            return validName; 
        }

        // given a variable name, this method will check to see if the
        // name is a valid identifier name. if this is not the case, then 
        // it will replace all the invalid characters with underscores.
        internal string GenerateIdName(string name) { 
            return GenerateIdName(name, this.codeProvider, this.UseSuffix); 
        }
 
        internal static string GenerateIdName(string name, CodeDomProvider codeProvider, bool useSuffix) {
            return GenerateIdName(name, codeProvider, useSuffix, additionalTruncationChars);
        }
 
        internal static string GenerateIdName(string name, CodeDomProvider codeProvider, bool useSuffix, int additionalCharsToTruncate) {
            if (!useSuffix) { 
                // if we're generating an identifier that needs to be backward compatible with Everett, we need to take into 
                // account some changes made to the CodeDomProviders; some names used to be invalid, but are not any more in Whidbey.
                // For these names, we still need to generate the fixed up version, like in Everett. 
                name = GetBackwardCompatibleIdentifier(name, codeProvider);
            }

            if (codeProvider.IsValidIdentifier(name)) { 
                return name;
            } 
 
            string ret = name.Replace(' ', '_');
            if ( !codeProvider.IsValidIdentifier(ret) ) { 
                if (!useSuffix) {
                    ret = "_" + ret;
                }
                UnicodeCategory unc; 
                for (int i = 0; i < ret.Length; i++) {
                    unc = Char.GetUnicodeCategory(ret[i]); 
                    if ( 
                        UnicodeCategory.UppercaseLetter      != unc &&
                        UnicodeCategory.LowercaseLetter      != unc && 
                        UnicodeCategory.TitlecaseLetter      != unc &&
                        UnicodeCategory.ModifierLetter       != unc &&
                        UnicodeCategory.OtherLetter          != unc &&
                        UnicodeCategory.NonSpacingMark       != unc && 
                        UnicodeCategory.SpacingCombiningMark != unc &&
                        UnicodeCategory.DecimalDigitNumber   != unc && 
                        UnicodeCategory.ConnectorPunctuation != unc 
                        ) {
                        ret = ret.Replace(ret[i], '_'); 
                    } // if
                } // for
            }
 
            // let's make sure that what we generated is really valid
            int generationAttempt = 0; 
            string originalRet = ret; 
            while (!codeProvider.IsValidIdentifier(ret) && generationAttempt < maxGenerationAttempts) {
                generationAttempt++; 
                ret = "_" + ret;
            }
            if(generationAttempt >= maxGenerationAttempts) {
                ret = originalRet; 
                while (!codeProvider.IsValidIdentifier(ret) && ret.Length > 0) {
                    // try to truncate the identifier, maybe it's just too long. 
                    ret = ret.Remove(ret.Length - 1); 
                }
 
                if (ret.Length == 0) {
                    // if we get here we weren't able to generate a valid identifier according to the CodeDomProvider
                    // since sometimes the CodeDomProvider is wrong, let's return the fixed up identifier it may work.
                    return originalRet; 
                }
                else { 
                    if (additionalCharsToTruncate > 0 && ret.Length > additionalCharsToTruncate) { 
                        if (codeProvider.IsValidIdentifier(ret.Remove(ret.Length - additionalCharsToTruncate))) {
                            // truncate a bit more, because we may have to add chars to fix it up. 
                            ret = ret.Remove(ret.Length - additionalCharsToTruncate);
                        }
                    }
                } 
            }
 
            return ret; 
        }
 
        private void AddNameToList(string name) {
            if(this.languageCaseInsensitive) {
                bookedMemberNames.Add(name.ToUpperInvariant());
            } 
            else {
                bookedMemberNames.Add(name); 
            } 
        }
 
        private bool ListContains(string name) {
            if(this.languageCaseInsensitive) {
                return bookedMemberNames.Contains(name.ToUpperInvariant());
            } 
            else {
                return bookedMemberNames.Contains(name); 
            } 
        }
 
        private static string GetBackwardCompatibleIdentifier(string identifier, CodeDomProvider provider) {
            string languageExtension = "." + provider.FileExtension;
            if (languageExtension.StartsWith("..", StringComparison.Ordinal)) {
                languageExtension = languageExtension.Substring(1); 
            }
            if (InvalidEverettIdentifiers.ContainsKey(languageExtension)) { 
                string[] invalidIdentifiers = InvalidEverettIdentifiers[languageExtension]; 
                if (invalidIdentifiers != null) {
                    bool languageCaseInsensitive = (provider.LanguageOptions & LanguageOptions.CaseInsensitive) > 0; 
                    for (int i = 0; i < invalidIdentifiers.Length; i++) {
                        if (StringUtil.EqualValue(identifier, invalidIdentifiers[i], languageCaseInsensitive)) {
                            return "_" + identifier;
                        } 
                    }
                } 
            } 

            return identifier; 
        }

    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
