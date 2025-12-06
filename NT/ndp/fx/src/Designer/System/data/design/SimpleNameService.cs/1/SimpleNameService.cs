 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design { 

    using System; 
    using System.Diagnostics;
    using System.Collections;
    using System.Globalization;
    using System.Text.RegularExpressions; 
 	using System.Design;
 
	internal  class SimpleNamedObject:INamedObject{ 
        object _obj;
 
        public SimpleNamedObject(object obj){
            _obj = obj;
        }
 
        public string Name {
            get{ 
                if (_obj is INamedObject){ 
                    return (_obj as INamedObject).Name;
                } 
                else if (_obj is string){
                    return _obj as string;
                }
                else { 
                    return _obj.ToString();
                } 
            } 
            set {
                if (_obj is INamedObject){ 
                    (_obj as INamedObject).Name = value;
                }
                else if (_obj is string){
                    _obj = value; 
                }
                else { 
                    // do nothing 
                }
            } 
        }
    }

    internal class SimpleNamedObjectCollection:ArrayList, INamedObjectCollection{ 
        private static SimpleNameService myNameService;
 
        protected virtual INameService NameService { 
            get {
                if( SimpleNamedObjectCollection.myNameService == null ) { 
                    SimpleNamedObjectCollection.myNameService = new SimpleNameService();
                }

                return SimpleNamedObjectCollection.myNameService; 
            }
        } 
 
        public INameService GetNameService() {
            return NameService; 
        }
    }

    /// <summary> 
    /// SimpleNameService is intended to have some basic naming rule as following
    /// 1. Use the identifier regular expression 
    /// 2. Name is case insensitive 
    /// 3. Limit length to 1024
    /// 4. Undone: Use CodeGen's NameHandler Bug 45100 
    /// </summary>
    internal  class SimpleNameService:INameService {
        internal const int DEFAULT_MAX_TRIALS = 100000;
        private const int MAX_LENGTH = 1024; 
        private int maxNumberOfTrials = DEFAULT_MAX_TRIALS;
 
        private static readonly string regexAlphaCharacter = @"[\p{L}\p{Nl}]"; 
        private static readonly string regexUnderscoreCharacter = @"\p{Pc}";
        private static readonly string regexIdentifierCharacter = @"[\p{L}\p{Nl}\p{Nd}\p{Mn}\p{Mc}\p{Cf}]"; 
        private static readonly string regexIdentifierStart = "(" + regexAlphaCharacter + "|(" + regexUnderscoreCharacter + regexIdentifierCharacter + "))";
        private static readonly string regexIdentifier = regexIdentifierStart + regexIdentifierCharacter + "*";
        private static SimpleNameService defaultInstance;
 
        private bool caseSensitive = true;
 
        internal SimpleNameService() { 
        }
 
        internal static SimpleNameService DefaultInstance {
            get {
                if (defaultInstance == null) {
                    defaultInstance = new SimpleNameService(); 
                }
                return defaultInstance; 
            } 
        }
 
        /// <summary>
        /// return proposed if it is not exist
        /// </summary>
        /// <param name="container"></param> 
        /// <param name="proposed"></param>
        /// <returns></returns> 
        public string CreateUniqueName(INamedObjectCollection container, string proposed){ 
            if( !NameExist(container, proposed) ) {
                this.ValidateName(proposed); 
                return proposed;
            }
            return this.CreateUniqueName(container, proposed, 1);
        } 

        /// <summary> 
        /// Return typeName1 or typeName2, etc. 
        /// </summary>
        /// <param name="connectionCollection"></param> 
        /// <param name="type"></param>
        /// <returns></returns>
        public string CreateUniqueName(INamedObjectCollection container, Type type) {
            return this.CreateUniqueName(container, type.Name, 1); 
        }
 
        /// <summary> 
        /// try proposedNameRoot + startSuffix first then ...
        /// </summary> 
        /// <param name="namedObjectCollection"></param>
        /// <param name="proposed"></param>
        /// <returns></returns>
        public string CreateUniqueName(INamedObjectCollection container, string proposedNameRoot, int startSuffix){ 
            return CreateUniqueNameOnCollection(container, proposedNameRoot, startSuffix);
        } 
 
        public string CreateUniqueNameOnCollection(ICollection container, string proposedNameRoot, int startSuffix) {
            int suffix = startSuffix; 
            if (suffix < 0){
                Debug.Fail("startSuffix should >= 0");
                suffix = 0;
            } 

            // It will throw if proposedNameRoot is bad! 
            ValidateName(proposedNameRoot); 

            string current = proposedNameRoot + suffix.ToString(System.Globalization.CultureInfo.CurrentCulture); 
            while (NameExist(container, current)){
                suffix++;
                if (suffix >= maxNumberOfTrials){
                    throw new InternalException(VSDExceptions.COMMON.SIMPLENAMESERVICE_NAMEOVERFLOW_MSG, 
                                                VSDExceptions.COMMON.SIMPLENAMESERVICE_NAMEOVERFLOW_CODE,true);
                } 
                current = proposedNameRoot + suffix.ToString(System.Globalization.CultureInfo.CurrentCulture); 
            }
            ValidateName(current); 

            return current;
        }
 
        /// <summary>
        /// Check to see if the name is exist in the container 
        /// </summary> 
        /// <param name="container"></param>
        /// <param name="nameTobeChecked"></param> 
        /// <returns></returns>
        private bool NameExist(ICollection container, string nameTobeChecked){
            return NameExist(container, null, nameTobeChecked);
        } 

        /// <summary> 
        /// Check to see if the name is exist in the container 
        /// </summary>
        /// <param name="container"></param> 
        /// <param name="objTobeChecked"></param>
        /// <returns></returns>
        private bool NameExist(ICollection container, INamedObject objTobeChecked, string nameTobeChecked) {
            if (StringUtil.Empty(nameTobeChecked) && objTobeChecked != null) { 
                nameTobeChecked = objTobeChecked.Name;
            } 
            foreach (INamedObject obj in container) { 
                if (obj != objTobeChecked && StringUtil.EqualValue(obj.Name, nameTobeChecked, !caseSensitive)){
                    return true; 
                }
            }
            return false;
        } 

        /// <summary> 
        /// Should throw NameValidationException when invalid name passed. 
        /// valid name syntx only
        /// </summary> 
        /// <param name="name"></param>
        public virtual void ValidateName(string name) {
            if (StringUtil.EmptyOrSpace(name)){
                throw new NameValidationException(SR.GetString(SR.CM_NameNotEmptyExcption)); 
            }
            if (name.Length > MAX_LENGTH){ 
                throw new NameValidationException(SR.GetString(SR.CM_NameTooLongExcption)); 
            }
            Match m = Regex.Match(name, regexIdentifier); 
            if(!m.Success) {
                throw new NameValidationException(SR.GetString(SR.CM_NameInvalid, name));
            }
        } 

        /// <summary> 
        /// Should throw NameValidationException when invalid name passed. 
        /// Valid syntax + dup
        /// This function is used when adding a new object to the collection 
        /// </summary>
        /// <param name="name"></param>
        public void ValidateUniqueName(INamedObjectCollection container, string proposedName){
            ValidateUniqueName(container, null, proposedName); 
        }
        // Check if the name is unique and valid 
        // This function is useful when renaming an existing object. 
        public void ValidateUniqueName(INamedObjectCollection container, INamedObject namedObject, string proposedName) {
            ValidateName(proposedName); 
            if (NameExist(container, namedObject, proposedName)) {
                throw new NameValidationException(SR.GetString(SR.CM_NameExist, proposedName));
            }
        } 

    } 
    /// <summary> 
    /// Used to validate the DataSet objects: tables, columns, etc.
    /// </summary> 
    internal class DataSetNameService : SimpleNameService {
        private static DataSetNameService defaultInstance;

        internal static new DataSetNameService DefaultInstance { 
            get {
                if (defaultInstance == null) { 
                    defaultInstance = new DataSetNameService(); 
                }
 
                return defaultInstance;
            }
        }
 
        /// <summary>
        /// Do nothing. Leave the name validation to DataSet 
        /// </summary> 
        /// <param name="name"></param>
        public override void ValidateName(string name) { 
        }
    }

    /// <summary> 
    /// Used to validate the sources
    /// </summary> 
    internal class SourceNameService : SimpleNameService { 
        private static SourceNameService defaultInstance;
 
        internal static new SourceNameService DefaultInstance {
            get {
                if (defaultInstance == null) {
                    defaultInstance = new SourceNameService(); 
                }
                return defaultInstance; 
            } 
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
    using System.Collections;
    using System.Globalization;
    using System.Text.RegularExpressions; 
 	using System.Design;
 
	internal  class SimpleNamedObject:INamedObject{ 
        object _obj;
 
        public SimpleNamedObject(object obj){
            _obj = obj;
        }
 
        public string Name {
            get{ 
                if (_obj is INamedObject){ 
                    return (_obj as INamedObject).Name;
                } 
                else if (_obj is string){
                    return _obj as string;
                }
                else { 
                    return _obj.ToString();
                } 
            } 
            set {
                if (_obj is INamedObject){ 
                    (_obj as INamedObject).Name = value;
                }
                else if (_obj is string){
                    _obj = value; 
                }
                else { 
                    // do nothing 
                }
            } 
        }
    }

    internal class SimpleNamedObjectCollection:ArrayList, INamedObjectCollection{ 
        private static SimpleNameService myNameService;
 
        protected virtual INameService NameService { 
            get {
                if( SimpleNamedObjectCollection.myNameService == null ) { 
                    SimpleNamedObjectCollection.myNameService = new SimpleNameService();
                }

                return SimpleNamedObjectCollection.myNameService; 
            }
        } 
 
        public INameService GetNameService() {
            return NameService; 
        }
    }

    /// <summary> 
    /// SimpleNameService is intended to have some basic naming rule as following
    /// 1. Use the identifier regular expression 
    /// 2. Name is case insensitive 
    /// 3. Limit length to 1024
    /// 4. Undone: Use CodeGen's NameHandler Bug 45100 
    /// </summary>
    internal  class SimpleNameService:INameService {
        internal const int DEFAULT_MAX_TRIALS = 100000;
        private const int MAX_LENGTH = 1024; 
        private int maxNumberOfTrials = DEFAULT_MAX_TRIALS;
 
        private static readonly string regexAlphaCharacter = @"[\p{L}\p{Nl}]"; 
        private static readonly string regexUnderscoreCharacter = @"\p{Pc}";
        private static readonly string regexIdentifierCharacter = @"[\p{L}\p{Nl}\p{Nd}\p{Mn}\p{Mc}\p{Cf}]"; 
        private static readonly string regexIdentifierStart = "(" + regexAlphaCharacter + "|(" + regexUnderscoreCharacter + regexIdentifierCharacter + "))";
        private static readonly string regexIdentifier = regexIdentifierStart + regexIdentifierCharacter + "*";
        private static SimpleNameService defaultInstance;
 
        private bool caseSensitive = true;
 
        internal SimpleNameService() { 
        }
 
        internal static SimpleNameService DefaultInstance {
            get {
                if (defaultInstance == null) {
                    defaultInstance = new SimpleNameService(); 
                }
                return defaultInstance; 
            } 
        }
 
        /// <summary>
        /// return proposed if it is not exist
        /// </summary>
        /// <param name="container"></param> 
        /// <param name="proposed"></param>
        /// <returns></returns> 
        public string CreateUniqueName(INamedObjectCollection container, string proposed){ 
            if( !NameExist(container, proposed) ) {
                this.ValidateName(proposed); 
                return proposed;
            }
            return this.CreateUniqueName(container, proposed, 1);
        } 

        /// <summary> 
        /// Return typeName1 or typeName2, etc. 
        /// </summary>
        /// <param name="connectionCollection"></param> 
        /// <param name="type"></param>
        /// <returns></returns>
        public string CreateUniqueName(INamedObjectCollection container, Type type) {
            return this.CreateUniqueName(container, type.Name, 1); 
        }
 
        /// <summary> 
        /// try proposedNameRoot + startSuffix first then ...
        /// </summary> 
        /// <param name="namedObjectCollection"></param>
        /// <param name="proposed"></param>
        /// <returns></returns>
        public string CreateUniqueName(INamedObjectCollection container, string proposedNameRoot, int startSuffix){ 
            return CreateUniqueNameOnCollection(container, proposedNameRoot, startSuffix);
        } 
 
        public string CreateUniqueNameOnCollection(ICollection container, string proposedNameRoot, int startSuffix) {
            int suffix = startSuffix; 
            if (suffix < 0){
                Debug.Fail("startSuffix should >= 0");
                suffix = 0;
            } 

            // It will throw if proposedNameRoot is bad! 
            ValidateName(proposedNameRoot); 

            string current = proposedNameRoot + suffix.ToString(System.Globalization.CultureInfo.CurrentCulture); 
            while (NameExist(container, current)){
                suffix++;
                if (suffix >= maxNumberOfTrials){
                    throw new InternalException(VSDExceptions.COMMON.SIMPLENAMESERVICE_NAMEOVERFLOW_MSG, 
                                                VSDExceptions.COMMON.SIMPLENAMESERVICE_NAMEOVERFLOW_CODE,true);
                } 
                current = proposedNameRoot + suffix.ToString(System.Globalization.CultureInfo.CurrentCulture); 
            }
            ValidateName(current); 

            return current;
        }
 
        /// <summary>
        /// Check to see if the name is exist in the container 
        /// </summary> 
        /// <param name="container"></param>
        /// <param name="nameTobeChecked"></param> 
        /// <returns></returns>
        private bool NameExist(ICollection container, string nameTobeChecked){
            return NameExist(container, null, nameTobeChecked);
        } 

        /// <summary> 
        /// Check to see if the name is exist in the container 
        /// </summary>
        /// <param name="container"></param> 
        /// <param name="objTobeChecked"></param>
        /// <returns></returns>
        private bool NameExist(ICollection container, INamedObject objTobeChecked, string nameTobeChecked) {
            if (StringUtil.Empty(nameTobeChecked) && objTobeChecked != null) { 
                nameTobeChecked = objTobeChecked.Name;
            } 
            foreach (INamedObject obj in container) { 
                if (obj != objTobeChecked && StringUtil.EqualValue(obj.Name, nameTobeChecked, !caseSensitive)){
                    return true; 
                }
            }
            return false;
        } 

        /// <summary> 
        /// Should throw NameValidationException when invalid name passed. 
        /// valid name syntx only
        /// </summary> 
        /// <param name="name"></param>
        public virtual void ValidateName(string name) {
            if (StringUtil.EmptyOrSpace(name)){
                throw new NameValidationException(SR.GetString(SR.CM_NameNotEmptyExcption)); 
            }
            if (name.Length > MAX_LENGTH){ 
                throw new NameValidationException(SR.GetString(SR.CM_NameTooLongExcption)); 
            }
            Match m = Regex.Match(name, regexIdentifier); 
            if(!m.Success) {
                throw new NameValidationException(SR.GetString(SR.CM_NameInvalid, name));
            }
        } 

        /// <summary> 
        /// Should throw NameValidationException when invalid name passed. 
        /// Valid syntax + dup
        /// This function is used when adding a new object to the collection 
        /// </summary>
        /// <param name="name"></param>
        public void ValidateUniqueName(INamedObjectCollection container, string proposedName){
            ValidateUniqueName(container, null, proposedName); 
        }
        // Check if the name is unique and valid 
        // This function is useful when renaming an existing object. 
        public void ValidateUniqueName(INamedObjectCollection container, INamedObject namedObject, string proposedName) {
            ValidateName(proposedName); 
            if (NameExist(container, namedObject, proposedName)) {
                throw new NameValidationException(SR.GetString(SR.CM_NameExist, proposedName));
            }
        } 

    } 
    /// <summary> 
    /// Used to validate the DataSet objects: tables, columns, etc.
    /// </summary> 
    internal class DataSetNameService : SimpleNameService {
        private static DataSetNameService defaultInstance;

        internal static new DataSetNameService DefaultInstance { 
            get {
                if (defaultInstance == null) { 
                    defaultInstance = new DataSetNameService(); 
                }
 
                return defaultInstance;
            }
        }
 
        /// <summary>
        /// Do nothing. Leave the name validation to DataSet 
        /// </summary> 
        /// <param name="name"></param>
        public override void ValidateName(string name) { 
        }
    }

    /// <summary> 
    /// Used to validate the sources
    /// </summary> 
    internal class SourceNameService : SimpleNameService { 
        private static SourceNameService defaultInstance;
 
        internal static new SourceNameService DefaultInstance {
            get {
                if (defaultInstance == null) {
                    defaultInstance = new SourceNameService(); 
                }
                return defaultInstance; 
            } 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
