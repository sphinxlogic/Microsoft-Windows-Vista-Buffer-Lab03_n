'****************************************************************************** 
'* WebUser.vb
'*
'* Copyright (c) Microsoft Corporation.  All rights reserved.
'* Information Contained Herein Is Proprietary and Confidential. 
'******************************************************************************
Option Explicit On 
Option Strict On 

Imports System 
Imports System.Security.Permissions
Imports System.Security.Principal
Imports System.Web
 
Namespace Microsoft.VisualBasic.ApplicationServices
 
    '''************************************************************************ 
    ''';WebUser
    ''' <summary> 
    ''' Class abstracting a web application user
    ''' </summary>
    ''' <remarks></remarks>
    <HostProtection(Resources:=HostProtectionResource.ExternalProcessMgmt)> _ 
    Public Class WebUser
        Inherits User 
 
        '==PUBLIC**************************************************************
 
        '''********************************************************************
        ''';New
        ''' <summary>
        ''' Creates an instance of a WebUser 
        ''' </summary>
        ''' <remarks></remarks> 
        Public Sub New() 
        End Sub
 
        '==PROTECTED************************************************************

        '''*********************************************************************
        ''';InternalPrincipal 
        ''' <summary>
        ''' Gets the current user from the HTTPContext 
        ''' </summary> 
        ''' <value>An IPrincipal representing the current user</value>
        ''' <remarks></remarks> 
        Protected Overrides Property InternalPrincipal() As IPrincipal
            Get
                Return HttpContext.Current.User
            End Get 
            Set(ByVal value As IPrincipal)
                HttpContext.Current.User = value 
            End Set 
        End Property
 
    End Class
End Namespace

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
﻿'****************************************************************************** 
'* WebUser.vb
'*
'* Copyright (c) Microsoft Corporation.  All rights reserved.
'* Information Contained Herein Is Proprietary and Confidential. 
'******************************************************************************
Option Explicit On 
Option Strict On 

Imports System 
Imports System.Security.Permissions
Imports System.Security.Principal
Imports System.Web
 
Namespace Microsoft.VisualBasic.ApplicationServices
 
    '''************************************************************************ 
    ''';WebUser
    ''' <summary> 
    ''' Class abstracting a web application user
    ''' </summary>
    ''' <remarks></remarks>
    <HostProtection(Resources:=HostProtectionResource.ExternalProcessMgmt)> _ 
    Public Class WebUser
        Inherits User 
 
        '==PUBLIC**************************************************************
 
        '''********************************************************************
        ''';New
        ''' <summary>
        ''' Creates an instance of a WebUser 
        ''' </summary>
        ''' <remarks></remarks> 
        Public Sub New() 
        End Sub
 
        '==PROTECTED************************************************************

        '''*********************************************************************
        ''';InternalPrincipal 
        ''' <summary>
        ''' Gets the current user from the HTTPContext 
        ''' </summary> 
        ''' <value>An IPrincipal representing the current user</value>
        ''' <remarks></remarks> 
        Protected Overrides Property InternalPrincipal() As IPrincipal
            Get
                Return HttpContext.Current.User
            End Get 
            Set(ByVal value As IPrincipal)
                HttpContext.Current.User = value 
            End Set 
        End Property
 
    End Class
End Namespace

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
