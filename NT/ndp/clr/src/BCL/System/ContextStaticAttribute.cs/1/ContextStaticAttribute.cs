// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** File:        ContextStaticAttribute.cs 
**
** 
**
** Purpose:     Custom attribute to indicate that the field should be treated
**              as a static relative to a context.
** 
**
** 
===========================================================*/ 
namespace System {
 
    using System;
    using System.Runtime.Remoting;
    [AttributeUsage(AttributeTargets.Field, Inherited = false),Serializable]
[System.Runtime.InteropServices.ComVisible(true)] 
    public class  ContextStaticAttribute : Attribute
    { 
        public ContextStaticAttribute() 
        {
        } 
    }
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** File:        ContextStaticAttribute.cs 
**
** 
**
** Purpose:     Custom attribute to indicate that the field should be treated
**              as a static relative to a context.
** 
**
** 
===========================================================*/ 
namespace System {
 
    using System;
    using System.Runtime.Remoting;
    [AttributeUsage(AttributeTargets.Field, Inherited = false),Serializable]
[System.Runtime.InteropServices.ComVisible(true)] 
    public class  ContextStaticAttribute : Attribute
    { 
        public ContextStaticAttribute() 
        {
        } 
    }
}
