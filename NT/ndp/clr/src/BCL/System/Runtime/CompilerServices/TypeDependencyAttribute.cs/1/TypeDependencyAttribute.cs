// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
using System;
 
 
namespace System.Runtime.CompilerServices
{ 

    // We might want to make this inherited someday.  But I suspect it shouldn't
    // be necessary.
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)] 
    internal sealed class TypeDependencyAttribute: Attribute
 	{ 
 
        private string typeName;
 
        public TypeDependencyAttribute (string typeName)
		{
            if(typeName == null) throw new ArgumentNullException("typeName");
            this.typeName = typeName; 
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

    // We might want to make this inherited someday.  But I suspect it shouldn't
    // be necessary.
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)] 
    internal sealed class TypeDependencyAttribute: Attribute
 	{ 
 
        private string typeName;
 
        public TypeDependencyAttribute (string typeName)
		{
            if(typeName == null) throw new ArgumentNullException("typeName");
            this.typeName = typeName; 
        }
    } 
 
}
 


