'*****************************************************************************/ 
'* SafeNativeMethods.vb
'*
'*  Copyright (c) Microsoft Corporation.  All rights reserved.
'*  Information Contained Herein Is Proprietary and Confidential. 
'*
'* Purpose: Methods defined in this file do not require demands because although they are 
'* native methods, they are benign (like time)  So we SuppressUnmanagedCodeSecurity 
'* for all methods defined in here.
'*****************************************************************************/ 

Imports System
Imports System.Security
Imports System.Security.Permissions 
Imports System.Text
Imports System.Runtime.InteropServices 
 
Namespace Microsoft.VisualBasic.CompilerServices
 
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never), _
     System.Runtime.InteropServices.ComVisible(False), _
     System.Security.SuppressUnmanagedCodeSecurityAttribute()> _
    Friend NotInheritable Class _ 
        SafeNativeMethods
 
        <PreserveSig()> Friend Declare Function _ 
            IsWindowEnabled _
                Lib "user32" (ByVal hwnd As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean 

        <PreserveSig()> Friend Declare Function _
            IsWindowVisible _
                Lib "user32" (ByVal hwnd As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean 

        <PreserveSig()> Friend Declare Function _ 
            GetWindowThreadProcessId _ 
                Lib "user32" (ByVal hwnd As IntPtr, ByRef lpdwProcessId As Integer) As Integer
 
        <PreserveSig()> Friend Declare Sub _
            GetLocalTime _
                Lib "kernel32" (ByVal systime As NativeTypes.SystemTime)
 
        '''*************************************************************************
        ''' ;New 
        ''' <summary> 
        '''
        ''' Adding a private constructor to prevent the compiler from generating a default constructor. 
        ''' </summary>
        Private Sub New()
        End Sub
    End Class 

End Namespace 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
'*****************************************************************************/ 
'* SafeNativeMethods.vb
'*
'*  Copyright (c) Microsoft Corporation.  All rights reserved.
'*  Information Contained Herein Is Proprietary and Confidential. 
'*
'* Purpose: Methods defined in this file do not require demands because although they are 
'* native methods, they are benign (like time)  So we SuppressUnmanagedCodeSecurity 
'* for all methods defined in here.
'*****************************************************************************/ 

Imports System
Imports System.Security
Imports System.Security.Permissions 
Imports System.Text
Imports System.Runtime.InteropServices 
 
Namespace Microsoft.VisualBasic.CompilerServices
 
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never), _
     System.Runtime.InteropServices.ComVisible(False), _
     System.Security.SuppressUnmanagedCodeSecurityAttribute()> _
    Friend NotInheritable Class _ 
        SafeNativeMethods
 
        <PreserveSig()> Friend Declare Function _ 
            IsWindowEnabled _
                Lib "user32" (ByVal hwnd As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean 

        <PreserveSig()> Friend Declare Function _
            IsWindowVisible _
                Lib "user32" (ByVal hwnd As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean 

        <PreserveSig()> Friend Declare Function _ 
            GetWindowThreadProcessId _ 
                Lib "user32" (ByVal hwnd As IntPtr, ByRef lpdwProcessId As Integer) As Integer
 
        <PreserveSig()> Friend Declare Sub _
            GetLocalTime _
                Lib "kernel32" (ByVal systime As NativeTypes.SystemTime)
 
        '''*************************************************************************
        ''' ;New 
        ''' <summary> 
        '''
        ''' Adding a private constructor to prevent the compiler from generating a default constructor. 
        ''' </summary>
        Private Sub New()
        End Sub
    End Class 

End Namespace 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
