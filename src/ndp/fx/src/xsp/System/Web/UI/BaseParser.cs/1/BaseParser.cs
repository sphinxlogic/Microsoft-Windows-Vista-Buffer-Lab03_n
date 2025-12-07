//------------------------------------------------------------------------------ 
// <copyright file="BaseParser.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * Implements the ASP.NET template parser 
 *
 * Copyright (c) 1998 Microsoft Corporation 
 */

/*********************************
 
Class hierarchy
 
BaseParser 
    DependencyParser
        TemplateControlDependencyParser 
            PageDependencyParser
            UserControlDependencyParser
            MasterPageDependencyParser
    TemplateParser 
        BaseTemplateParser
            TemplateControlParser 
                PageParser 
                UserControlParser
                    MasterPageParser 
            PageThemeParser
        ApplicationFileParser

**********************************/ 

namespace System.Web.UI { 
using System; 
using System.Collections;
using System.Web.Hosting; 
using System.Web.Util;
using System.Text.RegularExpressions;
using System.Web.RegularExpressions;
using System.Security.Permissions; 

// Internal interface for Parser that have exteranl assembly dependency. 
internal interface IAssemblyDependencyParser { 
    ICollection AssemblyDependencies { get; }
} 


/// <devdoc>
///    <para>[To be supplied.]</para> 
/// </devdoc>
[AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
[AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
public class BaseParser {
 
    // The directory used for relative path calculations
    private VirtualPath _baseVirtualDir;
    internal VirtualPath BaseVirtualDir {
        get { return _baseVirtualDir; } 

    } 
 
    // The virtual path to the file currently being processed
    private VirtualPath _currentVirtualPath; 
    internal VirtualPath CurrentVirtualPath {
        get { return _currentVirtualPath; }
        set {
            _currentVirtualPath = value; 

            // Can happen in the designer 
            if (value == null) return; 

            _baseVirtualDir = value.Parent; 
        }
    }

    internal string CurrentVirtualPathString { 
        get { return System.Web.VirtualPath.GetVirtualPathString(CurrentVirtualPath); }
    } 
 
    internal readonly static Regex tagRegex = new TagRegex();
    internal readonly static Regex directiveRegex = new DirectiveRegex(); 
    internal readonly static Regex endtagRegex = new EndTagRegex();
    internal readonly static Regex aspCodeRegex = new AspCodeRegex();
    internal readonly static Regex aspExprRegex = new AspExprRegex();
    internal readonly static Regex databindExprRegex = new DatabindExprRegex(); 
    internal readonly static Regex commentRegex = new CommentRegex();
    internal readonly static Regex includeRegex = new IncludeRegex(); 
    internal readonly static Regex textRegex = new TextRegex(); 

    // Regexes used in DetectSpecialServerTagError 
    internal readonly static Regex gtRegex = new GTRegex();
    internal readonly static Regex ltRegex = new LTRegex();
    internal readonly static Regex serverTagsRegex = new ServerTagsRegex();
    internal readonly static Regex runatServerRegex = new RunatServerRegex(); 

    /* 
     * Turns relative virtual path into absolute ones 
     */
    internal VirtualPath ResolveVirtualPath(VirtualPath virtualPath) { 
        return VirtualPathProvider.CombineVirtualPathsInternal(CurrentVirtualPath, virtualPath);
    }
}
 

} 
//------------------------------------------------------------------------------ 
// <copyright file="BaseParser.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * Implements the ASP.NET template parser 
 *
 * Copyright (c) 1998 Microsoft Corporation 
 */

/*********************************
 
Class hierarchy
 
BaseParser 
    DependencyParser
        TemplateControlDependencyParser 
            PageDependencyParser
            UserControlDependencyParser
            MasterPageDependencyParser
    TemplateParser 
        BaseTemplateParser
            TemplateControlParser 
                PageParser 
                UserControlParser
                    MasterPageParser 
            PageThemeParser
        ApplicationFileParser

**********************************/ 

namespace System.Web.UI { 
using System; 
using System.Collections;
using System.Web.Hosting; 
using System.Web.Util;
using System.Text.RegularExpressions;
using System.Web.RegularExpressions;
using System.Security.Permissions; 

// Internal interface for Parser that have exteranl assembly dependency. 
internal interface IAssemblyDependencyParser { 
    ICollection AssemblyDependencies { get; }
} 


/// <devdoc>
///    <para>[To be supplied.]</para> 
/// </devdoc>
[AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
[AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
public class BaseParser {
 
    // The directory used for relative path calculations
    private VirtualPath _baseVirtualDir;
    internal VirtualPath BaseVirtualDir {
        get { return _baseVirtualDir; } 

    } 
 
    // The virtual path to the file currently being processed
    private VirtualPath _currentVirtualPath; 
    internal VirtualPath CurrentVirtualPath {
        get { return _currentVirtualPath; }
        set {
            _currentVirtualPath = value; 

            // Can happen in the designer 
            if (value == null) return; 

            _baseVirtualDir = value.Parent; 
        }
    }

    internal string CurrentVirtualPathString { 
        get { return System.Web.VirtualPath.GetVirtualPathString(CurrentVirtualPath); }
    } 
 
    internal readonly static Regex tagRegex = new TagRegex();
    internal readonly static Regex directiveRegex = new DirectiveRegex(); 
    internal readonly static Regex endtagRegex = new EndTagRegex();
    internal readonly static Regex aspCodeRegex = new AspCodeRegex();
    internal readonly static Regex aspExprRegex = new AspExprRegex();
    internal readonly static Regex databindExprRegex = new DatabindExprRegex(); 
    internal readonly static Regex commentRegex = new CommentRegex();
    internal readonly static Regex includeRegex = new IncludeRegex(); 
    internal readonly static Regex textRegex = new TextRegex(); 

    // Regexes used in DetectSpecialServerTagError 
    internal readonly static Regex gtRegex = new GTRegex();
    internal readonly static Regex ltRegex = new LTRegex();
    internal readonly static Regex serverTagsRegex = new ServerTagsRegex();
    internal readonly static Regex runatServerRegex = new RunatServerRegex(); 

    /* 
     * Turns relative virtual path into absolute ones 
     */
    internal VirtualPath ResolveVirtualPath(VirtualPath virtualPath) { 
        return VirtualPathProvider.CombineVirtualPathsInternal(CurrentVirtualPath, virtualPath);
    }
}
 

} 
