// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

using System; 
 
namespace System.Runtime.CompilerServices
{ 
    using System;
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=true, Inherited=false)]
    public sealed class TypeForwardedToAttribute : Attribute
    { 
        private Type _destination;
 
        public TypeForwardedToAttribute(Type destination) 
        {
            _destination = destination; 
        }

        public Type Destination
        { 
            get {
                return _destination; 
            } 
        }
    } 
}


 

// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

using System; 
 
namespace System.Runtime.CompilerServices
{ 
    using System;
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=true, Inherited=false)]
    public sealed class TypeForwardedToAttribute : Attribute
    { 
        private Type _destination;
 
        public TypeForwardedToAttribute(Type destination) 
        {
            _destination = destination; 
        }

        public Type Destination
        { 
            get {
                return _destination; 
            } 
        }
    } 
}


 

