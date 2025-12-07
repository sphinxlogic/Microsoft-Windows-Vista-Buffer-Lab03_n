 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design { 

    using System; 
    using System.Collections;
    using System.Globalization;

    /// <summary> 
    /// </summary>
    internal interface INamedObject { 
        string Name { get; set; } 
    }
 
    /// <summary>
    /// Indicates a collection of objects that support INamedObject.
    /// </summary>
    internal interface INamedObjectCollection: ICollection { 
        INameService GetNameService();  // Might return null.
    } 
 
    /// <summary>
    /// A name service can be used for many INamedObjectCollection 
    /// </summary>
    internal interface INameService {
        // Create UniqueName will always return a valid and unique name
        // 
        string CreateUniqueName( INamedObjectCollection container, Type type );
        string CreateUniqueName( INamedObjectCollection container, string proposed ); 
        string CreateUniqueName( INamedObjectCollection container, string proposedNameRoot, int startSuffix); 

        // ValidateName does not check if the name is unque 
        void ValidateName( string name );   // Should throw NameValidationException when invalid name passed.

        // Check if the name is unique and valid
        void ValidateUniqueName( INamedObjectCollection container, string name );   // Should throw NameValidationException when invalid name passed. 

        // Check if the name is unique and valid 
        // This function is useful when renaming an existing object. 
        void ValidateUniqueName(INamedObjectCollection container, INamedObject namedObject, string proposedName);   // Should throw NameValidationException when invalid name passed.
    } 


    /// <summary>
    /// </summary> 
    internal class NamedObjectUtil {
 
        /// <summary> 
        /// Private contstructor to avoid class being instantiated.
        /// </summary> 
        private NamedObjectUtil() {
        }

        public static INamedObject Find( INamedObjectCollection coll, string name ) { 
            return NamedObjectUtil.Find( (ICollection) coll, name, false);
        } 
 
        private static INamedObject Find( ICollection coll, string name, bool ignoreCase) {
            IEnumerator e = coll.GetEnumerator(); 

            while( e.MoveNext() ) {
                INamedObject n = e.Current as INamedObject;
 
                if( n == null ) {
                    throw new InternalException( VSDExceptions.COMMON.NOT_A_NAMED_OBJECT_MSG, VSDExceptions.COMMON.NOT_A_NAMED_OBJECT_CODE ); 
                } 

                if( StringUtil.EqualValue(n.Name, name, ignoreCase)) { 
                    return n;
                }
            }
 
            return null;
        } 
    } 

 
    /// <summary>
    /// </summary>
    [Serializable]
    internal sealed class NameValidationException: ApplicationException { 

        public NameValidationException( string message ) : base( message ) {} 
 
        // No additional fields defined so we do not have to override default ISerializable implementation
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
    using System.Collections;
    using System.Globalization;

    /// <summary> 
    /// </summary>
    internal interface INamedObject { 
        string Name { get; set; } 
    }
 
    /// <summary>
    /// Indicates a collection of objects that support INamedObject.
    /// </summary>
    internal interface INamedObjectCollection: ICollection { 
        INameService GetNameService();  // Might return null.
    } 
 
    /// <summary>
    /// A name service can be used for many INamedObjectCollection 
    /// </summary>
    internal interface INameService {
        // Create UniqueName will always return a valid and unique name
        // 
        string CreateUniqueName( INamedObjectCollection container, Type type );
        string CreateUniqueName( INamedObjectCollection container, string proposed ); 
        string CreateUniqueName( INamedObjectCollection container, string proposedNameRoot, int startSuffix); 

        // ValidateName does not check if the name is unque 
        void ValidateName( string name );   // Should throw NameValidationException when invalid name passed.

        // Check if the name is unique and valid
        void ValidateUniqueName( INamedObjectCollection container, string name );   // Should throw NameValidationException when invalid name passed. 

        // Check if the name is unique and valid 
        // This function is useful when renaming an existing object. 
        void ValidateUniqueName(INamedObjectCollection container, INamedObject namedObject, string proposedName);   // Should throw NameValidationException when invalid name passed.
    } 


    /// <summary>
    /// </summary> 
    internal class NamedObjectUtil {
 
        /// <summary> 
        /// Private contstructor to avoid class being instantiated.
        /// </summary> 
        private NamedObjectUtil() {
        }

        public static INamedObject Find( INamedObjectCollection coll, string name ) { 
            return NamedObjectUtil.Find( (ICollection) coll, name, false);
        } 
 
        private static INamedObject Find( ICollection coll, string name, bool ignoreCase) {
            IEnumerator e = coll.GetEnumerator(); 

            while( e.MoveNext() ) {
                INamedObject n = e.Current as INamedObject;
 
                if( n == null ) {
                    throw new InternalException( VSDExceptions.COMMON.NOT_A_NAMED_OBJECT_MSG, VSDExceptions.COMMON.NOT_A_NAMED_OBJECT_CODE ); 
                } 

                if( StringUtil.EqualValue(n.Name, name, ignoreCase)) { 
                    return n;
                }
            }
 
            return null;
        } 
    } 

 
    /// <summary>
    /// </summary>
    [Serializable]
    internal sealed class NameValidationException: ApplicationException { 

        public NameValidationException( string message ) : base( message ) {} 
 
        // No additional fields defined so we do not have to override default ISerializable implementation
    } 

}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
