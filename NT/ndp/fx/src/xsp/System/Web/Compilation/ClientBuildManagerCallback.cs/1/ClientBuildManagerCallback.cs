//------------------------------------------------------------------------------ 
// <copyright file="ClientBuildManagerCallback.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/************************************************************************************************************/ 
 

namespace System.Web.Compilation { 

using System;
using System.Security.Permissions;
using System.CodeDom; 
using System.CodeDom.Compiler;
using System.Web.UI; 
 

// 
// This is a callback class implemented by ClientBuildManager callers. It is used
// to receive status information about the build.
//
[PermissionSet(SecurityAction.LinkDemand, Unrestricted = true)] 
[PermissionSet(SecurityAction.InheritanceDemand, Unrestricted = true)]
public class ClientBuildManagerCallback : MarshalByRefObject { 
 
    // This includes both errors and warnings
    public virtual void ReportCompilerError(CompilerError error) {} 

    public virtual void ReportParseError(ParserError error) {}

    public virtual void ReportProgress(string message) {} 
}
 
} 

 
//------------------------------------------------------------------------------ 
// <copyright file="ClientBuildManagerCallback.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/************************************************************************************************************/ 
 

namespace System.Web.Compilation { 

using System;
using System.Security.Permissions;
using System.CodeDom; 
using System.CodeDom.Compiler;
using System.Web.UI; 
 

// 
// This is a callback class implemented by ClientBuildManager callers. It is used
// to receive status information about the build.
//
[PermissionSet(SecurityAction.LinkDemand, Unrestricted = true)] 
[PermissionSet(SecurityAction.InheritanceDemand, Unrestricted = true)]
public class ClientBuildManagerCallback : MarshalByRefObject { 
 
    // This includes both errors and warnings
    public virtual void ReportCompilerError(CompilerError error) {} 

    public virtual void ReportParseError(ParserError error) {}

    public virtual void ReportProgress(string message) {} 
}
 
} 

 
