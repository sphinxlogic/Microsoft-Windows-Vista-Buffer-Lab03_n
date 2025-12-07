// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** File:    AssemblyNameProxy 
**
** 
** Purpose: Remotable version the AssemblyName
**
**
===========================================================*/ 
namespace System.Reflection {
    using System; 
 
[System.Runtime.InteropServices.ComVisible(true)]
    public class AssemblyNameProxy : MarshalByRefObject 
    {
        public AssemblyName GetAssemblyName(String assemblyFile)
        {
            return AssemblyName.nGetFileInformation(assemblyFile); 
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
** File:    AssemblyNameProxy 
**
** 
** Purpose: Remotable version the AssemblyName
**
**
===========================================================*/ 
namespace System.Reflection {
    using System; 
 
[System.Runtime.InteropServices.ComVisible(true)]
    public class AssemblyNameProxy : MarshalByRefObject 
    {
        public AssemblyName GetAssemblyName(String assemblyFile)
        {
            return AssemblyName.nGetFileInformation(assemblyFile); 
        }
    } 
 
}
