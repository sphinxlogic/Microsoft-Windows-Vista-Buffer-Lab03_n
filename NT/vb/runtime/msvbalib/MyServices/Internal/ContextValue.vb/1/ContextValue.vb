'****************************************************************************** 
'* ContextValue.vb
'*
'* Copyright (c) Microsoft Corporation.  All rights reserved.
'* Information Contained Herein Is Proprietary and Confidential. 
'******************************************************************************
Option Explicit On 
Option Strict On 

Namespace Microsoft.VisualBasic.MyServices.Internal 

    '''**************************************************************************
    ''' ;ContextValue
    ''' <summary> 
    ''' Stores an object in a context appropriate for the environment we are
    ''' running in (web/windows) 
    ''' </summary> 
    ''' <typeparam name="T"></typeparam>
    ''' <remarks> 
    ''' "Thread appropriate" means that if we are running on ASP.Net the object will be stored in the
    ''' context of the current request (meaning the object is stored per request on the web).  Otherwise,
    ''' the object is stored per CallContext.  Note that an instance of this class can only be associated
    ''' with the one item to be stored/retrieved at a time. 
    ''' </remarks>
    <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _ 
    Public Class ContextValue(Of T) 
        Public Sub New()
            m_ContextKey = System.Guid.NewGuid.ToString 
        End Sub

        '''**************************************************************************
        ''' ;Value 
        ''' <summary>
        ''' Get the object from the correct thread-appropriate location 
        ''' </summary> 
        ''' <value></value>
        Public Property Value() As T 'No Synclocks required because we are operating upon instance data and the object is not shared across threads 
            Get
                Dim Context As System.Web.HttpContext = System.Web.HttpContext.Current
                If Context IsNot Nothing Then 'we are running on the web
                    Return DirectCast(Context.Items(m_ContextKey), T) 'Note, Context.Items() can return Nothing and that's ok 
                Else 'we are running in a DLL
                    Return DirectCast(System.Runtime.Remoting.Messaging.CallContext.GetData(m_ContextKey), T) 'Note, CallContext.GetData() can return Nothing and that's ok 
                End If 
            End Get
            Set(ByVal value As T) 
                Dim Context As System.Web.HttpContext = System.Web.HttpContext.Current
                If Context IsNot Nothing Then 'we are running on the web
                    Context.Items(m_ContextKey) = value
                Else 'we are running in a DLL 
                    System.Runtime.Remoting.Messaging.CallContext.SetData(m_ContextKey, value)
                End If 
            End Set 
        End Property
 
        '= PRIVATE ============================================================

        Private m_ContextKey As String 'An item is stored in the dictionary by a guid which this string maintains
    End Class 'ContextValue 

End Namespace 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
'****************************************************************************** 
'* ContextValue.vb
'*
'* Copyright (c) Microsoft Corporation.  All rights reserved.
'* Information Contained Herein Is Proprietary and Confidential. 
'******************************************************************************
Option Explicit On 
Option Strict On 

Namespace Microsoft.VisualBasic.MyServices.Internal 

    '''**************************************************************************
    ''' ;ContextValue
    ''' <summary> 
    ''' Stores an object in a context appropriate for the environment we are
    ''' running in (web/windows) 
    ''' </summary> 
    ''' <typeparam name="T"></typeparam>
    ''' <remarks> 
    ''' "Thread appropriate" means that if we are running on ASP.Net the object will be stored in the
    ''' context of the current request (meaning the object is stored per request on the web).  Otherwise,
    ''' the object is stored per CallContext.  Note that an instance of this class can only be associated
    ''' with the one item to be stored/retrieved at a time. 
    ''' </remarks>
    <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _ 
    Public Class ContextValue(Of T) 
        Public Sub New()
            m_ContextKey = System.Guid.NewGuid.ToString 
        End Sub

        '''**************************************************************************
        ''' ;Value 
        ''' <summary>
        ''' Get the object from the correct thread-appropriate location 
        ''' </summary> 
        ''' <value></value>
        Public Property Value() As T 'No Synclocks required because we are operating upon instance data and the object is not shared across threads 
            Get
                Dim Context As System.Web.HttpContext = System.Web.HttpContext.Current
                If Context IsNot Nothing Then 'we are running on the web
                    Return DirectCast(Context.Items(m_ContextKey), T) 'Note, Context.Items() can return Nothing and that's ok 
                Else 'we are running in a DLL
                    Return DirectCast(System.Runtime.Remoting.Messaging.CallContext.GetData(m_ContextKey), T) 'Note, CallContext.GetData() can return Nothing and that's ok 
                End If 
            End Get
            Set(ByVal value As T) 
                Dim Context As System.Web.HttpContext = System.Web.HttpContext.Current
                If Context IsNot Nothing Then 'we are running on the web
                    Context.Items(m_ContextKey) = value
                Else 'we are running in a DLL 
                    System.Runtime.Remoting.Messaging.CallContext.SetData(m_ContextKey, value)
                End If 
            End Set 
        End Property
 
        '= PRIVATE ============================================================

        Private m_ContextKey As String 'An item is stored in the dictionary by a guid which this string maintains
    End Class 'ContextValue 

End Namespace 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
