'*****************************************************************************/ 
'* StaticLocals.vb
'*
'*  Copyright (C) 2001 Microsoft Corporation.  All Rights Reserved.
'*  Information Contained Herein Is Proprietary and Confidential. 
'*
'* Purpose: 
'* 
'*
'*****************************************************************************/ 

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)> _ 
    <System.Serializable()> _
    Public NotInheritable Class StaticLocalInitFlag 
        Public State As Short 
    End Class
 
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)> _
    <System.Serializable()> _
    Public NotInheritable Class IncompleteInitialization
        Inherits System.Exception 

        ' deserialization constructor must be defined as private. 
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)> _ 
        Private Sub New(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context) 
        End Sub

        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)> _
        Public Sub New(ByVal message As String) 
            MyBase.New(message)
        End Sub 
 
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)> _
        Public Sub New(ByVal message As String, ByVal innerException As System.Exception) 
            MyBase.New(message, innerException)
        End Sub

            ' default constructor 
        Public Sub New()
            MyBase.New() 
        End Sub 

    End Class 

End Namespace

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
'*****************************************************************************/ 
'* StaticLocals.vb
'*
'*  Copyright (C) 2001 Microsoft Corporation.  All Rights Reserved.
'*  Information Contained Herein Is Proprietary and Confidential. 
'*
'* Purpose: 
'* 
'*
'*****************************************************************************/ 

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)> _ 
    <System.Serializable()> _
    Public NotInheritable Class StaticLocalInitFlag 
        Public State As Short 
    End Class
 
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)> _
    <System.Serializable()> _
    Public NotInheritable Class IncompleteInitialization
        Inherits System.Exception 

        ' deserialization constructor must be defined as private. 
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)> _ 
        Private Sub New(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context) 
        End Sub

        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)> _
        Public Sub New(ByVal message As String) 
            MyBase.New(message)
        End Sub 
 
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)> _
        Public Sub New(ByVal message As String, ByVal innerException As System.Exception) 
            MyBase.New(message, innerException)
        End Sub

            ' default constructor 
        Public Sub New()
            MyBase.New() 
        End Sub 

    End Class 

End Namespace

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
