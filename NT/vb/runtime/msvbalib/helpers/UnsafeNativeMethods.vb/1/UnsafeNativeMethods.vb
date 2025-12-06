'*****************************************************************************/ 
'* UnsafeNativeMethods.vb
'*
'*  Copyright (c) Microsoft Corporation.  All rights reserved.
'*  Information Contained Herein Is Proprietary and Confidential. 
'*
'* Purpose: Methods defined in this file require you to do the appropriate demand before allowing 
'* a call to them.  We surpress the UnmanagedCodeSecurity for these methods so it is up to 
'* you to protect access to them.  These methods are not considered to be benign so you must
'* protect access to any of them that you call into. 
'*****************************************************************************/

Imports System
Imports System.Security 
Imports System.Security.Permissions
Imports System.Text 
Imports System.Runtime.InteropServices 
Imports System.Runtime.ConstrainedExecution
 
Namespace Microsoft.VisualBasic.CompilerServices

    <System.Runtime.InteropServices.ComVisible(False), _
     System.Security.SuppressUnmanagedCodeSecurityAttribute()> _ 
    Friend NotInheritable Class UnsafeNativeMethods
 
        <PreserveSig()> Friend Declare Ansi Function LCMapStringA _ 
                Lib "kernel32" Alias "LCMapStringA" (ByVal Locale As Integer, ByVal dwMapFlags As Integer, _
                    <MarshalAs(UnmanagedType.LPArray)> ByVal lpSrcStr As Byte(), ByVal cchSrc As Integer, <MarshalAs(UnmanagedType.LPArray)> ByVal lpDestStr As Byte(), ByVal cchDest As Integer) As Integer 

        <PreserveSig()> Friend Declare Auto Function LCMapString _
                Lib "kernel32" (ByVal Locale As Integer, ByVal dwMapFlags As Integer, _
                    ByVal lpSrcStr As String, ByVal cchSrc As Integer, ByVal lpDestStr As String, ByVal cchDest As Integer) As Integer 

        <DllImport("oleaut32", PreserveSig:=True, CharSet:=CharSet.Unicode, EntryPoint:="VarParseNumFromStr")> _ 
               Friend Shared Function VarParseNumFromStr( _ 
                <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal str As String, _
                ByVal lcid As Integer, _ 
                ByVal dwFlags As Integer, _
                <MarshalAs(UnmanagedType.LPArray)> ByVal numprsPtr As Byte(), _
                <MarshalAs(UnmanagedType.LPArray)> ByVal digits As Byte()) As Integer
        End Function 

        <DllImport("oleaut32", PreserveSig:=False, CharSet:=CharSet.Unicode, EntryPoint:="VarNumFromParseNum")> _ 
               Friend Shared Function VarNumFromParseNum( _ 
                <MarshalAs(UnmanagedType.LPArray)> ByVal numprsPtr As Byte(), _
                <MarshalAs(UnmanagedType.LPArray)> ByVal DigitArray As Byte(), _ 
                ByVal dwVtBits As Int32) As Object
        End Function

        <DllImport("oleaut32", PreserveSig:=False, CharSet:=CharSet.Unicode, EntryPoint:="VariantChangeType")> _ 
               Friend Shared Sub VariantChangeType( _
            <Out()> ByRef dest As Object, _ 
            <[In]()> ByRef Src As Object, _ 
            ByVal wFlags As Int16, _
            ByVal vt As Int16) 
        End Sub

        <DllImport("user32", PreserveSig:=True, CharSet:=CharSet.Unicode, EntryPoint:="MessageBeep")> _
               Friend Shared Function MessageBeep(ByVal uType As Integer) As Integer 
        End Function
 
        <DllImport("kernel32", PreserveSig:=True, CharSet:=CharSet.Unicode, EntryPoint:="SetLocalTime", SetLastError:=True)> _ 
               Friend Shared Function SetLocalTime(ByVal systime As NativeTypes.SystemTime) As Integer
        End Function 

        <DllImport("kernel32", PreserveSig:=True, CharSet:=CharSet.Auto, EntryPoint:="MoveFile", BestFitMapping:=False, ThrowOnUnmappableChar:=True, SetLastError:=True)> _
               Friend Shared Function MoveFile(<[In](), MarshalAs(UnmanagedType.LPTStr)> ByVal lpExistingFileName As String, _
                <[In](), MarshalAs(UnmanagedType.LPTStr)> ByVal lpNewFileName As String) As Integer 
        End Function
 
        <DllImport("kernel32", PreserveSig:=True, CharSet:=CharSet.Unicode, EntryPoint:="GetLogicalDrives")> _ 
               Friend Shared Function GetLogicalDrives() As Integer
        End Function 

        <DllImport("Kernel32", EntryPoint:="CreateFileMapping", CharSet:=CharSet.Auto, BestFitMapping:=False, SetLastError:=True)> _
        Friend Shared Function CreateFileMapping(ByVal hFile As HandleRef, <MarshalAs(UnmanagedType.LPStruct)> ByVal lpAttributes As NativeTypes.SECURITY_ATTRIBUTES, ByVal flProtect As Integer, ByVal dwMaxSizeHi As Integer, ByVal dwMaxSizeLow As Integer, ByVal lpName As String) As IntPtr
        End Function 

        <DllImport("Kernel32", EntryPoint:="OpenFileMapping", CharSet:=CharSet.Auto, SetLastError:=True)> _ 
                Friend Shared Function OpenFileMapping(ByVal dwDesiredAccess As Integer, <MarshalAs(UnmanagedType.Bool)> ByVal bInheritHandle As Boolean, ByVal lpName As String) As IntPtr 
        End Function
 
        <DllImport("Kernel32", EntryPoint:="MapViewOfFile", CharSet:=CharSet.Auto, SetLastError:=True)> _
        Friend Shared Function MapViewOfFile(ByVal hFileMapping As HandleRef, ByVal dwDesiredAccess As Integer, ByVal dwFileOffsetHigh As Integer, ByVal dwFileOffsetLow As Integer, ByVal dwNumberOfBytesToMap As Integer) As IntPtr
        End Function
 
        <ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)> _
        <DllImport("Kernel32", EntryPoint:="UnmapViewOfFile", CharSet:=CharSet.Auto, SetLastError:=True)> _ 
        Friend Shared Function UnmapViewOfFile(ByVal pvBaseAddress As HandleRef) As <MarshalAsAttribute(UnmanagedType.Bool)> Boolean 
        End Function
 
        Public Const MEMBERID_NIL As Integer = 0
        Public Const LCID_US_ENGLISH As Integer = &H409

 
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)> _
        Public Enum tagSYSKIND 
            SYS_WIN16 = 0 
            SYS_MAC = 2
        End Enum 


        '    [StructLayout(LayoutKind.Sequential)]
        '    Public class  tagTLIBATTR { 
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)> _
        Public Structure tagTLIBATTR 
            Public guid As Guid 
            Public lcid As Integer
            Public syskind As tagSYSKIND 
            <MarshalAs(UnmanagedType.U2)> Public wMajorVerNum As Short
            <MarshalAs(UnmanagedType.U2)> Public wMinorVerNum As Short
            <MarshalAs(UnmanagedType.U2)> Public wLibFlags As Short
        End Structure 

        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never), _ 
         ComImport(), _ 
         Guid("00020403-0000-0000-C000-000000000046"), _
         InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _ 
        Public Interface ITypeComp

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _
            Sub RemoteBind( _ 
                   <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal szName As String, _
                   <[In](), MarshalAs(UnmanagedType.U4)> ByVal lHashVal As Integer, _ 
                   <[In](), MarshalAs(UnmanagedType.U2)> ByVal wFlags As Short, _ 
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTInfo As ITypeInfo(), _
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pDescKind As ComTypes.DESCKIND(), _ 
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppFuncDesc As ComTypes.FUNCDESC(), _
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppVarDesc As ComTypes.VARDESC(), _
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTypeComp As ITypeComp(), _
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pDummy As Integer()) 

            Sub RemoteBindType( _ 
                   <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal szName As String, _ 
                   <[In](), MarshalAs(UnmanagedType.U4)> ByVal lHashVal As Integer, _
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTInfo As ITypeInfo()) 
        End Interface


 
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never), _
         ComImport(), _ 
         Guid("00020400-0000-0000-C000-000000000046"), _ 
         InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _
        Public Interface IDispatch 

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _
            <PreserveSig()> Function GetTypeInfoCount() As Integer
 
            <PreserveSig()> Function GetTypeInfo( _
                    <[In]()> ByVal index As Integer, _ 
                    <[In]()> ByVal lcid As Integer, _ 
                    <[Out](), MarshalAs(UnmanagedType.Interface)> ByRef pTypeInfo As ITypeInfo) As Integer
 

            <PreserveSig()> Function GetIDsOfNames() As Integer

 
            <PreserveSig()> Function Invoke() As Integer
        End Interface 
 

 
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never), _
         ComImport(), _
         Guid("00020401-0000-0000-C000-000000000046"), _
         InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _ 
        Public Interface ITypeInfo
            <PreserveSig()> Function GetTypeAttr( _ 
                    <Out()> ByRef pTypeAttr As IntPtr) As Integer 

            <PreserveSig()> Function GetTypeComp( _ 
                    <Out()> ByRef pTComp As ITypeComp) As Integer

            <PreserveSig()> Function GetFuncDesc( _
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer, _ 
                    <Out()> ByRef pFuncDesc As IntPtr) As Integer
 
            <PreserveSig()> Function GetVarDesc( _ 
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer, _
                    <Out()> ByRef pVarDesc As IntPtr) As Integer 

            <PreserveSig()> Function GetNames( _
                    <[In]()> ByVal memid As Integer, _
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal rgBstrNames As String(), _ 
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal cMaxNames As Integer, _
                    <Out(), MarshalAs(UnmanagedType.U4)> ByRef cNames As Integer) As Integer 
 
            <Obsolete("Bad signature, second param type should be Byref. Fix and verify signature before use.", True)> _
            <PreserveSig()> Function GetRefTypeOfImplType( _ 
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer, _
                    <Out()> ByRef pRefType As Integer) As Integer

            <Obsolete("Bad signature, second param type should be Byref. Fix and verify signature before use.", True)> _ 
            <PreserveSig()> Function GetImplTypeFlags( _
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer, _ 
                    <Out()> ByVal pImplTypeFlags As Integer) As Integer 

            <PreserveSig()> Function GetIDsOfNames( _ 
                    <[In]()> ByVal rgszNames As IntPtr, _
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal cNames As Integer, _
                    <Out()> ByRef pMemId As IntPtr) As Integer
 
            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _
            <PreserveSig()> Function Invoke() As Integer 
 
            <PreserveSig()> Function GetDocumentation( _
                     <[In]()> ByVal memid As Integer, _ 
                     <Out(), MarshalAs(UnmanagedType.BStr)> ByRef pBstrName As String, _
                     <Out(), MarshalAs(UnmanagedType.BStr)> ByRef pBstrDocString As String, _
                     <Out(), MarshalAs(UnmanagedType.U4)> ByRef pdwHelpContext As Integer, _
                     <Out(), MarshalAs(UnmanagedType.BStr)> ByRef pBstrHelpFile As String) As Integer 

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _ 
            <PreserveSig()> Function GetDllEntry( _ 
                    <[In]()> ByVal memid As Integer, _
                    <[In]()> ByVal invkind As ComTypes.INVOKEKIND, _ 
                    <Out(), MarshalAs(UnmanagedType.BStr)> ByVal pBstrDllName As String, _
                    <Out(), MarshalAs(UnmanagedType.BStr)> ByVal pBstrName As String, _
                    <Out(), MarshalAs(UnmanagedType.U2)> ByVal pwOrdinal As Short) As Integer
 
            <PreserveSig()> Function GetRefTypeInfo( _
                     <[In]()> ByVal hreftype As IntPtr, _ 
                     <Out()> ByRef pTypeInfo As ITypeInfo) As Integer 

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _ 
            <PreserveSig()> Function AddressOfMember() As Integer

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _
            <PreserveSig()> Function CreateInstance( _ 
                    <[In]()> ByRef pUnkOuter As IntPtr, _
                    <[In]()> ByRef riid As Guid, _ 
                    <Out(), MarshalAs(UnmanagedType.IUnknown)> ByVal ppvObj As Object) As Integer 

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _ 
            <PreserveSig()> Function GetMops( _
                    <[In]()> ByVal memid As Integer, _
                    <Out(), MarshalAs(UnmanagedType.BStr)> ByVal pBstrMops As String) As Integer
 
            <PreserveSig()> Function GetContainingTypeLib( _
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTLib As ITypeLib(), _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pIndex As Integer()) As Integer 

            <PreserveSig()> Sub ReleaseTypeAttr(ByVal typeAttr As IntPtr) 

            <PreserveSig()> Sub ReleaseFuncDesc(ByVal funcDesc As IntPtr)

            <PreserveSig()> Sub ReleaseVarDesc(ByVal varDesc As IntPtr) 
        End Interface
 
 

        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never), _ 
         ComImport(), _
         Guid("B196B283-BAB4-101A-B69C-00AA00341D07"), _
         InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _
        Public Interface IProvideClassInfo 
            Function GetClassInfo() As <MarshalAs(UnmanagedType.Interface)> ITypeInfo
        End Interface 
 

 
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never), _
         ComImport(), _
         Guid("00020402-0000-0000-C000-000000000046"), _
         InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _ 
        Public Interface ITypeLib
            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _ 
            Sub RemoteGetTypeInfoCount( _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pcTInfo As Integer())
 
            Sub GetTypeInfo( _
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer, _
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTInfo As ITypeInfo())
 
            Sub GetTypeInfoType( _
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer, _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pTKind As ComTypes.TYPEKIND()) 

            Sub GetTypeInfoOfGuid( _ 
                    <[In]()> ByRef guid As Guid, _
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTInfo As ITypeInfo())

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _ 
            Sub RemoteGetLibAttr( _
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTLibAttr As tagTLIBATTR(), _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pDummy As Integer()) 

            Sub GetTypeComp( _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTComp As ITypeComp())

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _
            Sub RemoteGetDocumentation( _ 
            ByVal index As Integer, _
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal refPtrFlags As Integer, _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pBstrName As String(), _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pBstrDocString As String(), _
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pdwHelpContext As Integer(), _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pBstrHelpFile As String())

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _
            Sub RemoteIsName( _ 
                    <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal szNameBuf As String, _
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal lHashVal As Integer, _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pfName As IntPtr(), _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pBstrLibName As String())
 
            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _
            Sub RemoteFindName( _
                    <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal szNameBuf As String, _
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal lHashVal As Integer, _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTInfo As ITypeInfo(), _
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal rgMemId As Integer(), _ 
                    <[In](), Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pcFound As Short(), _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pBstrLibName As String())
 
            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _
            Sub LocalReleaseTLibAttr()
        End Interface
 
        '*****************************************************************************
        ';GetKeyState 
        ' 
        'Summary:
        '   Gets the state of the specified key on the keyboard when the function 
        '   is called.
        'Params:
        '   KeyCode - Integer representing the key in question.
        'Returns: 
        '   The high order byte is 1 if the key is down. The low order byte is one
        '   if the key is toggled on (i.e. for keys like CapsLock) 
        '***************************************************************************** 
        <DllImport("User32.dll", ExactSpelling:=True, CharSet:=CharSet.Auto)> _
        Friend Shared Function GetKeyState(ByVal KeyCode As Integer) As Short 
        End Function

        '''*************************************************************************
        ''';LocalFree 
        ''' <summary>
        ''' Frees memory allocated from the local heap. i.e. frees memory allocated 
        ''' by LocalAlloc or LocalReAlloc.n 
        ''' </summary>
        ''' <param name="LocalHandle"></param> 
        ''' <returns></returns>
        ''' <remarks></remarks>
        <DllImport("kernel32", ExactSpelling:=True, setlasterror:=True)> _
        Friend Shared Function LocalFree(ByVal LocalHandle As IntPtr) As IntPtr 
        End Function
 
        ''' ************************************************************************** 
        ''' ;GetDiskFreeSpaceEx
        ''' <summary> 
        ''' Used to determine how much free space is on a disk
        ''' </summary>
        ''' <param name="Directory">Path including drive we're getting information about</param>
        ''' <param name="UserSpaceFree">The amount of free sapce available to the current user</param> 
        ''' <param name="TotalUserSpace">The total amount of space on the disk relative to the current user</param>
        ''' <param name="TotalFreeSpace">The amount of free spave on the disk.</param> 
        ''' <returns>True if function succeeds in getting info otherwise False</returns> 
        <DllImport("Kernel32.dll", CharSet:=CharSet.Auto, BestFitMapping:=False, SetLastError:=True)> _
        Friend Shared Function GetDiskFreeSpaceEx(ByVal Directory As String, ByRef UserSpaceFree As Long, ByRef TotalUserSpace As Long, ByRef TotalFreeSpace As Long) As <MarshalAs(UnmanagedType.Bool)> Boolean 
        End Function

        '''*************************************************************************
        ''' ;New 
        ''' <summary>
        ''' Avoid uninstantiated internal class. 
        ''' Adding a private constructor to prevent the compiler from generating a default constructor. 
        ''' </summary>
        Private Sub New() 
        End Sub
    End Class

End Namespace 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
'*****************************************************************************/ 
'* UnsafeNativeMethods.vb
'*
'*  Copyright (c) Microsoft Corporation.  All rights reserved.
'*  Information Contained Herein Is Proprietary and Confidential. 
'*
'* Purpose: Methods defined in this file require you to do the appropriate demand before allowing 
'* a call to them.  We surpress the UnmanagedCodeSecurity for these methods so it is up to 
'* you to protect access to them.  These methods are not considered to be benign so you must
'* protect access to any of them that you call into. 
'*****************************************************************************/

Imports System
Imports System.Security 
Imports System.Security.Permissions
Imports System.Text 
Imports System.Runtime.InteropServices 
Imports System.Runtime.ConstrainedExecution
 
Namespace Microsoft.VisualBasic.CompilerServices

    <System.Runtime.InteropServices.ComVisible(False), _
     System.Security.SuppressUnmanagedCodeSecurityAttribute()> _ 
    Friend NotInheritable Class UnsafeNativeMethods
 
        <PreserveSig()> Friend Declare Ansi Function LCMapStringA _ 
                Lib "kernel32" Alias "LCMapStringA" (ByVal Locale As Integer, ByVal dwMapFlags As Integer, _
                    <MarshalAs(UnmanagedType.LPArray)> ByVal lpSrcStr As Byte(), ByVal cchSrc As Integer, <MarshalAs(UnmanagedType.LPArray)> ByVal lpDestStr As Byte(), ByVal cchDest As Integer) As Integer 

        <PreserveSig()> Friend Declare Auto Function LCMapString _
                Lib "kernel32" (ByVal Locale As Integer, ByVal dwMapFlags As Integer, _
                    ByVal lpSrcStr As String, ByVal cchSrc As Integer, ByVal lpDestStr As String, ByVal cchDest As Integer) As Integer 

        <DllImport("oleaut32", PreserveSig:=True, CharSet:=CharSet.Unicode, EntryPoint:="VarParseNumFromStr")> _ 
               Friend Shared Function VarParseNumFromStr( _ 
                <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal str As String, _
                ByVal lcid As Integer, _ 
                ByVal dwFlags As Integer, _
                <MarshalAs(UnmanagedType.LPArray)> ByVal numprsPtr As Byte(), _
                <MarshalAs(UnmanagedType.LPArray)> ByVal digits As Byte()) As Integer
        End Function 

        <DllImport("oleaut32", PreserveSig:=False, CharSet:=CharSet.Unicode, EntryPoint:="VarNumFromParseNum")> _ 
               Friend Shared Function VarNumFromParseNum( _ 
                <MarshalAs(UnmanagedType.LPArray)> ByVal numprsPtr As Byte(), _
                <MarshalAs(UnmanagedType.LPArray)> ByVal DigitArray As Byte(), _ 
                ByVal dwVtBits As Int32) As Object
        End Function

        <DllImport("oleaut32", PreserveSig:=False, CharSet:=CharSet.Unicode, EntryPoint:="VariantChangeType")> _ 
               Friend Shared Sub VariantChangeType( _
            <Out()> ByRef dest As Object, _ 
            <[In]()> ByRef Src As Object, _ 
            ByVal wFlags As Int16, _
            ByVal vt As Int16) 
        End Sub

        <DllImport("user32", PreserveSig:=True, CharSet:=CharSet.Unicode, EntryPoint:="MessageBeep")> _
               Friend Shared Function MessageBeep(ByVal uType As Integer) As Integer 
        End Function
 
        <DllImport("kernel32", PreserveSig:=True, CharSet:=CharSet.Unicode, EntryPoint:="SetLocalTime", SetLastError:=True)> _ 
               Friend Shared Function SetLocalTime(ByVal systime As NativeTypes.SystemTime) As Integer
        End Function 

        <DllImport("kernel32", PreserveSig:=True, CharSet:=CharSet.Auto, EntryPoint:="MoveFile", BestFitMapping:=False, ThrowOnUnmappableChar:=True, SetLastError:=True)> _
               Friend Shared Function MoveFile(<[In](), MarshalAs(UnmanagedType.LPTStr)> ByVal lpExistingFileName As String, _
                <[In](), MarshalAs(UnmanagedType.LPTStr)> ByVal lpNewFileName As String) As Integer 
        End Function
 
        <DllImport("kernel32", PreserveSig:=True, CharSet:=CharSet.Unicode, EntryPoint:="GetLogicalDrives")> _ 
               Friend Shared Function GetLogicalDrives() As Integer
        End Function 

        <DllImport("Kernel32", EntryPoint:="CreateFileMapping", CharSet:=CharSet.Auto, BestFitMapping:=False, SetLastError:=True)> _
        Friend Shared Function CreateFileMapping(ByVal hFile As HandleRef, <MarshalAs(UnmanagedType.LPStruct)> ByVal lpAttributes As NativeTypes.SECURITY_ATTRIBUTES, ByVal flProtect As Integer, ByVal dwMaxSizeHi As Integer, ByVal dwMaxSizeLow As Integer, ByVal lpName As String) As IntPtr
        End Function 

        <DllImport("Kernel32", EntryPoint:="OpenFileMapping", CharSet:=CharSet.Auto, SetLastError:=True)> _ 
                Friend Shared Function OpenFileMapping(ByVal dwDesiredAccess As Integer, <MarshalAs(UnmanagedType.Bool)> ByVal bInheritHandle As Boolean, ByVal lpName As String) As IntPtr 
        End Function
 
        <DllImport("Kernel32", EntryPoint:="MapViewOfFile", CharSet:=CharSet.Auto, SetLastError:=True)> _
        Friend Shared Function MapViewOfFile(ByVal hFileMapping As HandleRef, ByVal dwDesiredAccess As Integer, ByVal dwFileOffsetHigh As Integer, ByVal dwFileOffsetLow As Integer, ByVal dwNumberOfBytesToMap As Integer) As IntPtr
        End Function
 
        <ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)> _
        <DllImport("Kernel32", EntryPoint:="UnmapViewOfFile", CharSet:=CharSet.Auto, SetLastError:=True)> _ 
        Friend Shared Function UnmapViewOfFile(ByVal pvBaseAddress As HandleRef) As <MarshalAsAttribute(UnmanagedType.Bool)> Boolean 
        End Function
 
        Public Const MEMBERID_NIL As Integer = 0
        Public Const LCID_US_ENGLISH As Integer = &H409

 
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)> _
        Public Enum tagSYSKIND 
            SYS_WIN16 = 0 
            SYS_MAC = 2
        End Enum 


        '    [StructLayout(LayoutKind.Sequential)]
        '    Public class  tagTLIBATTR { 
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)> _
        Public Structure tagTLIBATTR 
            Public guid As Guid 
            Public lcid As Integer
            Public syskind As tagSYSKIND 
            <MarshalAs(UnmanagedType.U2)> Public wMajorVerNum As Short
            <MarshalAs(UnmanagedType.U2)> Public wMinorVerNum As Short
            <MarshalAs(UnmanagedType.U2)> Public wLibFlags As Short
        End Structure 

        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never), _ 
         ComImport(), _ 
         Guid("00020403-0000-0000-C000-000000000046"), _
         InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _ 
        Public Interface ITypeComp

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _
            Sub RemoteBind( _ 
                   <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal szName As String, _
                   <[In](), MarshalAs(UnmanagedType.U4)> ByVal lHashVal As Integer, _ 
                   <[In](), MarshalAs(UnmanagedType.U2)> ByVal wFlags As Short, _ 
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTInfo As ITypeInfo(), _
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pDescKind As ComTypes.DESCKIND(), _ 
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppFuncDesc As ComTypes.FUNCDESC(), _
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppVarDesc As ComTypes.VARDESC(), _
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTypeComp As ITypeComp(), _
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pDummy As Integer()) 

            Sub RemoteBindType( _ 
                   <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal szName As String, _ 
                   <[In](), MarshalAs(UnmanagedType.U4)> ByVal lHashVal As Integer, _
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTInfo As ITypeInfo()) 
        End Interface


 
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never), _
         ComImport(), _ 
         Guid("00020400-0000-0000-C000-000000000046"), _ 
         InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _
        Public Interface IDispatch 

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _
            <PreserveSig()> Function GetTypeInfoCount() As Integer
 
            <PreserveSig()> Function GetTypeInfo( _
                    <[In]()> ByVal index As Integer, _ 
                    <[In]()> ByVal lcid As Integer, _ 
                    <[Out](), MarshalAs(UnmanagedType.Interface)> ByRef pTypeInfo As ITypeInfo) As Integer
 

            <PreserveSig()> Function GetIDsOfNames() As Integer

 
            <PreserveSig()> Function Invoke() As Integer
        End Interface 
 

 
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never), _
         ComImport(), _
         Guid("00020401-0000-0000-C000-000000000046"), _
         InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _ 
        Public Interface ITypeInfo
            <PreserveSig()> Function GetTypeAttr( _ 
                    <Out()> ByRef pTypeAttr As IntPtr) As Integer 

            <PreserveSig()> Function GetTypeComp( _ 
                    <Out()> ByRef pTComp As ITypeComp) As Integer

            <PreserveSig()> Function GetFuncDesc( _
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer, _ 
                    <Out()> ByRef pFuncDesc As IntPtr) As Integer
 
            <PreserveSig()> Function GetVarDesc( _ 
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer, _
                    <Out()> ByRef pVarDesc As IntPtr) As Integer 

            <PreserveSig()> Function GetNames( _
                    <[In]()> ByVal memid As Integer, _
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal rgBstrNames As String(), _ 
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal cMaxNames As Integer, _
                    <Out(), MarshalAs(UnmanagedType.U4)> ByRef cNames As Integer) As Integer 
 
            <Obsolete("Bad signature, second param type should be Byref. Fix and verify signature before use.", True)> _
            <PreserveSig()> Function GetRefTypeOfImplType( _ 
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer, _
                    <Out()> ByRef pRefType As Integer) As Integer

            <Obsolete("Bad signature, second param type should be Byref. Fix and verify signature before use.", True)> _ 
            <PreserveSig()> Function GetImplTypeFlags( _
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer, _ 
                    <Out()> ByVal pImplTypeFlags As Integer) As Integer 

            <PreserveSig()> Function GetIDsOfNames( _ 
                    <[In]()> ByVal rgszNames As IntPtr, _
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal cNames As Integer, _
                    <Out()> ByRef pMemId As IntPtr) As Integer
 
            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _
            <PreserveSig()> Function Invoke() As Integer 
 
            <PreserveSig()> Function GetDocumentation( _
                     <[In]()> ByVal memid As Integer, _ 
                     <Out(), MarshalAs(UnmanagedType.BStr)> ByRef pBstrName As String, _
                     <Out(), MarshalAs(UnmanagedType.BStr)> ByRef pBstrDocString As String, _
                     <Out(), MarshalAs(UnmanagedType.U4)> ByRef pdwHelpContext As Integer, _
                     <Out(), MarshalAs(UnmanagedType.BStr)> ByRef pBstrHelpFile As String) As Integer 

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _ 
            <PreserveSig()> Function GetDllEntry( _ 
                    <[In]()> ByVal memid As Integer, _
                    <[In]()> ByVal invkind As ComTypes.INVOKEKIND, _ 
                    <Out(), MarshalAs(UnmanagedType.BStr)> ByVal pBstrDllName As String, _
                    <Out(), MarshalAs(UnmanagedType.BStr)> ByVal pBstrName As String, _
                    <Out(), MarshalAs(UnmanagedType.U2)> ByVal pwOrdinal As Short) As Integer
 
            <PreserveSig()> Function GetRefTypeInfo( _
                     <[In]()> ByVal hreftype As IntPtr, _ 
                     <Out()> ByRef pTypeInfo As ITypeInfo) As Integer 

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _ 
            <PreserveSig()> Function AddressOfMember() As Integer

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _
            <PreserveSig()> Function CreateInstance( _ 
                    <[In]()> ByRef pUnkOuter As IntPtr, _
                    <[In]()> ByRef riid As Guid, _ 
                    <Out(), MarshalAs(UnmanagedType.IUnknown)> ByVal ppvObj As Object) As Integer 

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _ 
            <PreserveSig()> Function GetMops( _
                    <[In]()> ByVal memid As Integer, _
                    <Out(), MarshalAs(UnmanagedType.BStr)> ByVal pBstrMops As String) As Integer
 
            <PreserveSig()> Function GetContainingTypeLib( _
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTLib As ITypeLib(), _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pIndex As Integer()) As Integer 

            <PreserveSig()> Sub ReleaseTypeAttr(ByVal typeAttr As IntPtr) 

            <PreserveSig()> Sub ReleaseFuncDesc(ByVal funcDesc As IntPtr)

            <PreserveSig()> Sub ReleaseVarDesc(ByVal varDesc As IntPtr) 
        End Interface
 
 

        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never), _ 
         ComImport(), _
         Guid("B196B283-BAB4-101A-B69C-00AA00341D07"), _
         InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _
        Public Interface IProvideClassInfo 
            Function GetClassInfo() As <MarshalAs(UnmanagedType.Interface)> ITypeInfo
        End Interface 
 

 
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never), _
         ComImport(), _
         Guid("00020402-0000-0000-C000-000000000046"), _
         InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _ 
        Public Interface ITypeLib
            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _ 
            Sub RemoteGetTypeInfoCount( _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pcTInfo As Integer())
 
            Sub GetTypeInfo( _
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer, _
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTInfo As ITypeInfo())
 
            Sub GetTypeInfoType( _
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer, _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pTKind As ComTypes.TYPEKIND()) 

            Sub GetTypeInfoOfGuid( _ 
                    <[In]()> ByRef guid As Guid, _
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTInfo As ITypeInfo())

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _ 
            Sub RemoteGetLibAttr( _
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTLibAttr As tagTLIBATTR(), _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pDummy As Integer()) 

            Sub GetTypeComp( _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTComp As ITypeComp())

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _
            Sub RemoteGetDocumentation( _ 
            ByVal index As Integer, _
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal refPtrFlags As Integer, _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pBstrName As String(), _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pBstrDocString As String(), _
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pdwHelpContext As Integer(), _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pBstrHelpFile As String())

            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _
            Sub RemoteIsName( _ 
                    <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal szNameBuf As String, _
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal lHashVal As Integer, _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pfName As IntPtr(), _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pBstrLibName As String())
 
            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _
            Sub RemoteFindName( _
                    <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal szNameBuf As String, _
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal lHashVal As Integer, _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTInfo As ITypeInfo(), _
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal rgMemId As Integer(), _ 
                    <[In](), Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pcFound As Short(), _ 
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pBstrLibName As String())
 
            <Obsolete("Bad signature. Fix and verify signature before use.", True)> _
            Sub LocalReleaseTLibAttr()
        End Interface
 
        '*****************************************************************************
        ';GetKeyState 
        ' 
        'Summary:
        '   Gets the state of the specified key on the keyboard when the function 
        '   is called.
        'Params:
        '   KeyCode - Integer representing the key in question.
        'Returns: 
        '   The high order byte is 1 if the key is down. The low order byte is one
        '   if the key is toggled on (i.e. for keys like CapsLock) 
        '***************************************************************************** 
        <DllImport("User32.dll", ExactSpelling:=True, CharSet:=CharSet.Auto)> _
        Friend Shared Function GetKeyState(ByVal KeyCode As Integer) As Short 
        End Function

        '''*************************************************************************
        ''';LocalFree 
        ''' <summary>
        ''' Frees memory allocated from the local heap. i.e. frees memory allocated 
        ''' by LocalAlloc or LocalReAlloc.n 
        ''' </summary>
        ''' <param name="LocalHandle"></param> 
        ''' <returns></returns>
        ''' <remarks></remarks>
        <DllImport("kernel32", ExactSpelling:=True, setlasterror:=True)> _
        Friend Shared Function LocalFree(ByVal LocalHandle As IntPtr) As IntPtr 
        End Function
 
        ''' ************************************************************************** 
        ''' ;GetDiskFreeSpaceEx
        ''' <summary> 
        ''' Used to determine how much free space is on a disk
        ''' </summary>
        ''' <param name="Directory">Path including drive we're getting information about</param>
        ''' <param name="UserSpaceFree">The amount of free sapce available to the current user</param> 
        ''' <param name="TotalUserSpace">The total amount of space on the disk relative to the current user</param>
        ''' <param name="TotalFreeSpace">The amount of free spave on the disk.</param> 
        ''' <returns>True if function succeeds in getting info otherwise False</returns> 
        <DllImport("Kernel32.dll", CharSet:=CharSet.Auto, BestFitMapping:=False, SetLastError:=True)> _
        Friend Shared Function GetDiskFreeSpaceEx(ByVal Directory As String, ByRef UserSpaceFree As Long, ByRef TotalUserSpace As Long, ByRef TotalFreeSpace As Long) As <MarshalAs(UnmanagedType.Bool)> Boolean 
        End Function

        '''*************************************************************************
        ''' ;New 
        ''' <summary>
        ''' Avoid uninstantiated internal class. 
        ''' Adding a private constructor to prevent the compiler from generating a default constructor. 
        ''' </summary>
        Private Sub New() 
        End Sub
    End Class

End Namespace 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
