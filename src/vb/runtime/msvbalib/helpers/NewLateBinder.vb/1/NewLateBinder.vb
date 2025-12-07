'*****************************************************************************/ 
'* NewLateBinder.vb
'*
'*  Copyright (C), Microsoft Corporation.  All Rights Reserved.
'*  Information Contained Herein Is Proprietary and Confidential. 
'*
'* Purpose: 
'*  Implements VB late binder. 
'*
'*****************************************************************************/ 

Imports System
Imports System.Reflection
Imports System.Diagnostics 
Imports System.Runtime.InteropServices
Imports System.Collections 
Imports System.Collections.Generic 
Imports System.Security.Permissions
Imports System.Globalization 

Imports Microsoft.VisualBasic.CompilerServices.Symbols
Imports Microsoft.VisualBasic.CompilerServices.OverloadResolution
 
#Const NEW_BINDER = True
#Const BINDING_LOG = False 
 

 



Namespace Microsoft.VisualBasic.CompilerServices 

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)> _ 
    Public NotInheritable Class NewLateBinding 
        ' Prevent creation.
        Private Sub New() 
        End Sub

        <DebuggerHiddenAttribute()> <DebuggerStepThroughAttribute()> _
        Public Shared Function LateCanEvaluate( _ 
            ByVal instance As Object, _
            ByVal type As System.Type, _ 
            ByVal memberName As String, _ 
            ByVal arguments As Object(), _
            ByVal allowFunctionEvaluation As Boolean, _ 
            ByVal allowPropertyEvaluation As Boolean) As Boolean

            Dim BaseReference As Container
            If Type IsNot Nothing Then 
                BaseReference = New Container(type)
            Else 
                BaseReference = New Container(instance) 
            End If
 
            Dim Members As MemberInfo() = BaseReference.GetMembers(memberName, False)

            If Members.Length = 0 Then
                Return True 
            End If
 
            ' This is a field access 
            If Members(0).MemberType = MemberTypes.Field Then
                If arguments.Length = 0 Then 
                    Return True
                Else
                    Dim FieldValue As Object = BaseReference.GetFieldValue(DirectCast(Members(0), FieldInfo))
                    BaseReference = New Container(FieldValue) 
                    If BaseReference.IsArray Then
                        Return True 
                    End If 
                    Return allowPropertyEvaluation
                End If 
            End If

            ' This is a method invocation
            If Members(0).MemberType = MemberTypes.Method Then 
                Return allowFunctionEvaluation
            End If 
 
            ' This is a property access
            If Members(0).MemberType = MemberTypes.Property Then 
                Return allowPropertyEvaluation
            End If

            Return True 
        End Function
 
 

        <DebuggerHiddenAttribute()> <DebuggerStepThroughAttribute()> _ 
        Public Shared Function LateCall( _
            ByVal Instance As Object, _
            ByVal Type As System.Type, _
            ByVal MemberName As String, _ 
            ByVal Arguments As Object(), _
            ByVal ArgumentNames As String(), _ 
            ByVal TypeArguments As System.Type(), _ 
            ByVal CopyBack As Boolean(), _
            ByVal IgnoreReturn As Boolean) As Object 

#If Not NEW_BINDER Then
            Return LateBinding.InternalLateCall(Instance, Type, MemberName, Arguments, ArgumentNames, CopyBack, False)
#End If 
            If Arguments Is Nothing Then Arguments = NoArguments
            If ArgumentNames Is Nothing Then ArgumentNames = NoArgumentNames 
            If TypeArguments Is Nothing Then TypeArguments = NoTypeArguments 

            Dim BaseReference As Container 
            If Type IsNot Nothing Then
                BaseReference = New Container(Type)
            Else
                BaseReference = New Container(Instance) 
            End If
 
            If BaseReference.IsCOMObject Then 
                ' call the old binder.
                Return LateBinding.InternalLateCall(Instance, Type, MemberName, Arguments, ArgumentNames, CopyBack, IgnoreReturn) 
            End If

            Dim InvocationFlags As BindingFlags = BindingFlags.InvokeMethod Or BindingFlags.GetProperty
            If IgnoreReturn Then InvocationFlags = InvocationFlags Or BindingFlags.IgnoreReturn 

            Dim Failure As ResolutionFailure 
 
            Return _
                CallMethod( _ 
                    BaseReference, _
                    MemberName, _
                    Arguments, _
                    ArgumentNames, _ 
                    TypeArguments, _
                    CopyBack, _ 
                    InvocationFlags, _ 
                    True, _
                    Failure) 

        End Function

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()> _ 
        Public Shared Function LateIndexGet( _
            ByVal Instance As Object, _ 
            ByVal Arguments() As Object, _ 
            ByVal ArgumentNames() As String) As Object
 
            Dim Failure As ResolutionFailure
            Return InternalLateIndexGet( _
                Instance, _
                Arguments, _ 
                ArgumentNames, _
                True, _ 
                Failure) 
        End Function
 
        Friend Shared Function InternalLateIndexGet( _
            ByVal Instance As Object, _
            ByVal Arguments() As Object, _
            ByVal ArgumentNames() As String, _ 
            ByVal ReportErrors As Boolean, _
            ByRef Failure As ResolutionFailure) As Object 
 
            Failure = ResolutionFailure.None
 
#If Not NEW_BINDER Then
            Return LateBinding.LateIndexGet(Instance, Arguments, ArgumentNames)
#End If
            If Arguments Is Nothing Then Arguments = NoArguments 
            If ArgumentNames Is Nothing Then ArgumentNames = NoArgumentNames
 
            Dim BaseReference As Container = New Container(Instance) 

            If BaseReference.IsCOMObject Then 
                ' call the old binder.
                Return LateBinding.LateIndexGet(Instance, Arguments, ArgumentNames)
            End If
 
            'An r-value expression o(a) has two possible forms:
            '    1: o(a)    array lookup--where o is an array object and a is a set of indices 
            '    2: o.d(a)  default member access--where o has default method/property d 

            If BaseReference.IsArray Then 
                'This is an array lookup o(a).

                If ArgumentNames.Length > 0 Then
                    Failure = ResolutionFailure.InvalidArgument 

                    If ReportErrors Then 
                        Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidNamedArgs)) 
                    End If
 
                    Return Nothing
                End If

                Return BaseReference.GetArrayValue(Arguments) 
            End If
 
            'This is a default member access o.d(a), which is a call to method "". 

            Return _ 
                CallMethod( _
                    BaseReference, _
                    "", _
                    Arguments, _ 
                    ArgumentNames, _
                    NoTypeArguments, _ 
                    Nothing, _ 
                    BindingFlags.InvokeMethod Or BindingFlags.GetProperty, _
                    ReportErrors, _ 
                    Failure)
        End Function

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()> _ 
        Public Shared Function LateGet( _
            ByVal Instance As Object, _ 
            ByVal Type As System.Type, _ 
            ByVal MemberName As String, _
            ByVal Arguments As Object(), _ 
            ByVal ArgumentNames As String(), _
            ByVal TypeArguments As Type(), _
            ByVal CopyBack As Boolean()) As Object
 
#If Not NEW_BINDER Then
            Return LateBinding.LateGet(Instance, Type, MemberName, Arguments, ArgumentNames, CopyBack) 
#End If 

            If Arguments Is Nothing Then Arguments = NoArguments 
            If ArgumentNames Is Nothing Then ArgumentNames = NoArgumentNames
            If TypeArguments Is Nothing Then TypeArguments = NoTypeArguments

            Dim BaseReference As Container 
            If Type IsNot Nothing Then
                BaseReference = New Container(Type) 
            Else 
                BaseReference = New Container(Instance)
            End If 

            Dim InvocationFlags As BindingFlags = BindingFlags.InvokeMethod Or BindingFlags.GetProperty

 
            If BaseReference.IsCOMObject Then
                ' call the old binder. 
                Return LateBinding.LateGet(Instance, Type, MemberName, Arguments, ArgumentNames, CopyBack) 

            End If 

            Dim Members As MemberInfo() = BaseReference.GetMembers(MemberName, True)

            If Members(0).MemberType = MemberTypes.Field Then 
                If TypeArguments.Length > 0 Then
                    Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidValue)) 
                End If 

                Dim FieldValue As Object = BaseReference.GetFieldValue(DirectCast(Members(0), FieldInfo)) 
                If Arguments.Length = 0 Then
                    'This is a simple field access.
                    Return FieldValue
                Else 
                    'This is an indexed field access.
                    Return LateIndexGet(FieldValue, Arguments, ArgumentNames) 
                End If 
            End If
 
            If ArgumentNames.Length > Arguments.Length OrElse _
               (CopyBack IsNot Nothing AndAlso CopyBack.Length <> Arguments.Length) Then
                Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidValue))
            End If 

            Dim Failure As OverloadResolution.ResolutionFailure 
            Dim TargetProcedure As Method = _ 
                ResolveCall( _
                    BaseReference, _ 
                    MemberName, _
                    Members, _
                    Arguments, _
                    ArgumentNames, _ 
                    TypeArguments, _
                    InvocationFlags, _ 
                    False, _ 
                    Failure)
 
            If Failure = OverloadResolution.ResolutionFailure.None Then
                Return BaseReference.InvokeMethod(TargetProcedure, Arguments, CopyBack, InvocationFlags)

            ElseIf Arguments.Length > 0 Then 

                TargetProcedure = _ 
                    ResolveCall( _ 
                        BaseReference, _
                        MemberName, _ 
                        Members, _
                        NoArguments, _
                        NoArgumentNames, _
                        TypeArguments, _ 
                        InvocationFlags, _
                        False, _ 
                        Failure) 

                If Failure = OverloadResolution.ResolutionFailure.None Then 
                    Dim Result As Object = BaseReference.InvokeMethod(TargetProcedure, NoArguments, Nothing, InvocationFlags)

                    'For backwards compatibility, throw a missing member exception if the intermediate result is Nothing.
                    If Result Is Nothing Then 
                        Throw New _
                            MissingMemberException( _ 
                                GetResourceString( _ 
                                    ResID.IntermediateLateBoundNothingResult1, _
                                    TargetProcedure.ToString, _ 
                                    BaseReference.VBFriendlyName))
                    End If

                    Result = InternalLateIndexGet( _ 
                                Result, _
                                Arguments, _ 
                                ArgumentNames, _ 
                                False, _
                                Failure) 

                    If Failure = ResolutionFailure.None Then
                        Return Result
                    End If 
                End If
 
            End If 

            'Every attempt to make this work failed.  Redo the original call resolution to generate errors. 
            ResolveCall( _
                BaseReference, _
                MemberName, _
                Members, _ 
                Arguments, _
                ArgumentNames, _ 
                TypeArguments, _ 
                InvocationFlags, _
                True, _ 
                Failure)

            Debug.Fail("the resolution should have thrown an exception")
            Throw New InternalErrorException() 
        End Function
 
 
        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()> _
        Public Shared Sub LateIndexSetComplex( _ 
            ByVal Instance As Object, _
            ByVal Arguments As Object(), _
            ByVal ArgumentNames As String(), _
            ByVal OptimisticSet As Boolean, _ 
            ByVal RValueBase As Boolean)
 
#If Not NEW_BINDER Then 
            LateBinding.LateIndexSetComplex(Instance, Arguments, ArgumentNames, OptimisticSet, RValueBase)
            Return 
#End If


 

 
            If Arguments Is Nothing Then Arguments = NoArguments 
            If ArgumentNames Is Nothing Then ArgumentNames = NoArgumentNames
 
            Dim BaseReference As Container = New Container(Instance)

            'An l-value expression o(a) has two possible forms:
            '    1: o(a) = v    array lookup--where o is an array object and a is a set of indices 
            '    2: o.d(a) = v  default member access--where o has default method/property d
 
            If BaseReference.IsArray Then 
                'This is an array lookup and assignment o(a) = v.
 
                If ArgumentNames.Length > 0 Then
                    Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidNamedArgs))
                End If
 
                BaseReference.SetArrayValue(Arguments)
                Return 
            End If 

            If ArgumentNames.Length > Arguments.Length Then 
                Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidValue))
            End If

            If Arguments.Length < 1 Then 
                'We're binding to a Set, we must have at least the Value argument.
                Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidValue)) 
            End If 

            Dim MethodName As String = "" 

            If BaseReference.IsCOMObject Then
                ' call the old binder.
                LateBinding.LateIndexSetComplex(Instance, Arguments, ArgumentNames, OptimisticSet, RValueBase) 
                Return
#If 0 Then 
                Try 
                    BaseReference.InvokeCOMMethod( _
                        MethodName, _ 
                        Arguments, _
                        ArgumentNames, _
                        Nothing, _
                        GetPropertyPutFlags(Arguments(Arguments.Length - 1))) 

                    If RValueBase AndAlso BaseReference.IsValueType Then 
                        Throw New Exception( _ 
                                GetResourceString( _
                                    ResID.RValueBaseForValueType, _ 
                                    BaseReference.VBFriendlyName, _
                                    BaseReference.VBFriendlyName))
                    End If
                Catch ex As System.MissingMemberException When OptimisticSet = True 
                    'A missing member exception means it has no Set member.  Silently handle the exception.
                End Try 
                Return 
#End If
 
            Else
                Dim InvocationFlags As BindingFlags = BindingFlags.SetProperty

                Dim Members As MemberInfo() = BaseReference.GetMembers(MethodName, True) 'MethodName is set during this call. 

                Dim Failure As OverloadResolution.ResolutionFailure 
                Dim TargetProcedure As Method = _ 
                    ResolveCall( _
                        BaseReference, _ 
                        MethodName, _
                        Members, _
                        Arguments, _
                        ArgumentNames, _ 
                        NoTypeArguments, _
                        InvocationFlags, _ 
                        False, _ 
                        Failure)
 
                If Failure = OverloadResolution.ResolutionFailure.None Then

                    If RValueBase AndAlso BaseReference.IsValueType Then
                        Throw New Exception( _ 
                                GetResourceString( _
                                    ResID.RValueBaseForValueType, _ 
                                    BaseReference.VBFriendlyName, _ 
                                    BaseReference.VBFriendlyName))
                    End If 

                    BaseReference.InvokeMethod(TargetProcedure, Arguments, Nothing, InvocationFlags)
                    Return
 
                ElseIf OptimisticSet Then
                    Return 
 
                Else
                    'Redo the resolution to generate errors. 
                    ResolveCall( _
                        BaseReference, _
                        MethodName, _
                        Members, _ 
                        Arguments, _
                        ArgumentNames, _ 
                        NoTypeArguments, _ 
                        InvocationFlags, _
                        True, _ 
                        Failure)
                End If
            End If
 
            Debug.Fail("the resolution should have thrown an exception - should never reach here")
            Throw New InternalErrorException() 
 
        End Sub
 

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()> _
        Public Shared Sub LateIndexSet( _
            ByVal Instance As Object, _ 
            ByVal Arguments() As Object, _
            ByVal ArgumentNames() As String) 
 
#If Not NEW_BINDER Then
            LateBinding.LateIndexSet(Instance, Arguments, ArgumentNames) 
            Return
#End If

            LateIndexSetComplex(Instance, Arguments, ArgumentNames, False, False) 
            Return
        End Sub 
 
        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()> _
        Public Shared Sub LateSetComplex( _ 
            ByVal Instance As Object, _
            ByVal Type As Type, _
            ByVal MemberName As String, _
            ByVal Arguments() As Object, _ 
            ByVal ArgumentNames() As String, _
            ByVal TypeArguments() As Type, _ 
            ByVal OptimisticSet As Boolean, _ 
            ByVal RValueBase As Boolean)
 
#If Not NEW_BINDER Then
            LateBinding.LateSetComplex(Instance, Type, MemberName, Arguments, ArgumentNames, OptimisticSet, RValueBase)
            Return
#End If 
            Const DefaultCallType As CallType = CType(0, CallType)
            LateSet(Instance, Type, MemberName, Arguments, ArgumentNames, TypeArguments, OptimisticSet, RValueBase, DefaultCallType) 
        End Sub 

 
        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()> _
        Public Shared Sub LateSet( _
            ByVal Instance As Object, _
            ByVal Type As Type, _ 
            ByVal MemberName As String, _
            ByVal Arguments() As Object, _ 
            ByVal ArgumentNames() As String, _ 
            ByVal TypeArguments As Type())
 
#If Not NEW_BINDER Then
            LateBinding.LateSet(Instance, Type, MemberName, Arguments, ArgumentNames)
            Return
#End If 

            Const DefaultCallType As CallType = CType(0, CallType) 
            LateSet(Instance, Type, MemberName, Arguments, ArgumentNames, TypeArguments, False, False, DefaultCallType) 
            Return
        End Sub 

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()> _
        Public Shared Sub LateSet( _
            ByVal Instance As Object, _ 
            ByVal Type As Type, _
            ByVal MemberName As String, _ 
            ByVal Arguments As Object(), _ 
            ByVal ArgumentNames As String(), _
            ByVal TypeArguments As Type(), _ 
            ByVal OptimisticSet As Boolean, _
            ByVal RValueBase As Boolean, _
            ByVal CallType As CallType)
 

 
 

 

            If Arguments Is Nothing Then Arguments = NoArguments
            If ArgumentNames Is Nothing Then ArgumentNames = NoArgumentNames
            If TypeArguments Is Nothing Then TypeArguments = NoTypeArguments 

            Dim BaseReference As Container 
            If Type IsNot Nothing Then 
                BaseReference = New Container(Type)
            Else 
                BaseReference = New Container(Instance)
            End If

            Dim InvocationFlags As BindingFlags 

            If BaseReference.IsCOMObject Then 
                '  call the old binder. 
                Try
 



 

                    LateBinding.InternalLateSet(Instance, Type, MemberName, Arguments, ArgumentNames, OptimisticSet, CallType) 
 
                    If RValueBase AndAlso Type.IsValueType Then
                        'note that objType is passed byref to InternalLateSet and that it 
                        'should be valid by the time we get to this point
                        Throw New Exception(GetResourceString(ResID.RValueBaseForValueType, VBFriendlyName(Type, Instance), VBFriendlyName(Type, Instance)))
                    End If
                Catch ex As System.MissingMemberException When OptimisticSet = True 
                    'A missing member exception means it has no Set member.  Silently handle the exception.
                End Try 
 
                Return
#If 0 Then 
                If CallType = CallType.Set Then
                    InvocationFlags = InvocationFlags Or BindingFlags.PutRefDispProperty
                    If Arguments(Arguments.GetUpperBound(0)) Is Nothing Then
                        Arguments(Arguments.GetUpperBound(0)) = New DispatchWrapper(Nothing) 
                    End If
                ElseIf CallType = CallType.Let Then 
                    InvocationFlags = InvocationFlags Or BindingFlags.PutDispProperty 
                Else
                    InvocationFlags = InvocationFlags Or GetPropertyPutFlags(Arguments(Arguments.GetUpperBound(0))) 
                End If


                BaseReference.InvokeCOMMethod2(MemberName, Arguments, ArgumentNames, Nothing, InvocationFlags) 
                Return
#End If 
            End If 

            Dim Members As MemberInfo() = BaseReference.GetMembers(MemberName, True) 

            If Members(0).MemberType = MemberTypes.Field Then

                If TypeArguments.Length > 0 Then 
                    Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidValue))
                End If 
 
                If Arguments.Length = 1 Then
                    If RValueBase AndAlso BaseReference.IsValueType Then 
                        Throw New Exception( _
                                GetResourceString( _
                                    ResID.RValueBaseForValueType, _
                                    BaseReference.VBFriendlyName, _ 
                                    BaseReference.VBFriendlyName))
                    End If 
                    'This is a simple field set. 
                    BaseReference.SetFieldValue(DirectCast(Members(0), FieldInfo), Arguments(0))
                    Return 
                Else
                    'This is an indexed field set.
                    Dim FieldValue As Object = BaseReference.GetFieldValue(DirectCast(Members(0), FieldInfo))
                    LateIndexSetComplex(FieldValue, Arguments, ArgumentNames, OptimisticSet, True) 
                    Return
                End If 
            End If 

            InvocationFlags = BindingFlags.SetProperty 

            If ArgumentNames.Length > Arguments.Length Then
                Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidValue))
            End If 

            Dim Failure As OverloadResolution.ResolutionFailure 
            Dim TargetProcedure As Method 

            If TypeArguments.Length = 0 Then 

                TargetProcedure = _
                    ResolveCall( _
                        BaseReference, _ 
                        MemberName, _
                        Members, _ 
                        Arguments, _ 
                        ArgumentNames, _
                        NoTypeArguments, _ 
                        InvocationFlags, _
                        False, _
                        Failure)
 
                If Failure = OverloadResolution.ResolutionFailure.None Then
                    If RValueBase AndAlso BaseReference.IsValueType Then 
                        Throw New Exception( _ 
                                GetResourceString( _
                                    ResID.RValueBaseForValueType, _ 
                                    BaseReference.VBFriendlyName, _
                                    BaseReference.VBFriendlyName))
                    End If
 
                    BaseReference.InvokeMethod(TargetProcedure, Arguments, Nothing, InvocationFlags)
                    Return 
                End If 

            End If 

            Dim SecondaryInvocationFlags As BindingFlags = _
                    BindingFlags.InvokeMethod Or BindingFlags.GetProperty
 
            If Failure = OverloadResolution.ResolutionFailure.None OrElse Failure = OverloadResolution.ResolutionFailure.MissingMember Then
 
                TargetProcedure = _ 
                    ResolveCall( _
                        BaseReference, _ 
                        MemberName, _
                        Members, _
                        NoArguments, _
                        NoArgumentNames, _ 
                        TypeArguments, _
                        SecondaryInvocationFlags, _ 
                        False, _ 
                        Failure)
 
                If Failure = OverloadResolution.ResolutionFailure.None Then
                    Dim Result As Object = _
                        BaseReference.InvokeMethod(TargetProcedure, NoArguments, Nothing, SecondaryInvocationFlags)
 
                    'For backwards compatibility, throw a missing member exception if the intermediate result is Nothing.
                    If Result Is Nothing Then 
                        Throw New _ 
                            MissingMemberException( _
                                GetResourceString( _ 
                                    ResID.IntermediateLateBoundNothingResult1, _
                                    TargetProcedure.ToString, _
                                    BaseReference.VBFriendlyName))
                    End If 

                    LateIndexSetComplex(Result, Arguments, ArgumentNames, OptimisticSet, True) 
                    Return 
                End If
            End If 

            If OptimisticSet Then
                Return
            End If 

            'Everything failed, so give errors. Redo the first attempt to generate the errors. 
            If TypeArguments.Length = 0 Then 
                ResolveCall( _
                    BaseReference, _ 
                    MemberName, _
                    Members, _
                    Arguments, _
                    ArgumentNames, _ 
                    TypeArguments, _
                    InvocationFlags, _ 
                    True, _ 
                    Failure)
 
            Else
                ResolveCall( _
                    BaseReference, _
                    MemberName, _ 
                    Members, _
                    NoArguments, _ 
                    NoArgumentNames, _ 
                    TypeArguments, _
                    SecondaryInvocationFlags, _ 
                    True, _
                    Failure)
            End If
 
            Debug.Fail("the resolution should have thrown an exception")
            Throw New InternalErrorException() 
            Return 
        End Sub
 
        Private Shared Function CallMethod( _
            ByVal BaseReference As Container, _
            ByVal MethodName As String, _
            ByVal Arguments As Object(), _ 
            ByVal ArgumentNames As String(), _
            ByVal TypeArguments As System.Type(), _ 
            ByVal CopyBack As Boolean(), _ 
            ByVal InvocationFlags As BindingFlags, _
            ByVal ReportErrors As Boolean, _ 
            ByRef Failure As ResolutionFailure) As Object

            Debug.Assert(BaseReference IsNot Nothing, "Nothing unexpected")
            Debug.Assert(Arguments IsNot Nothing, "Nothing unexpected") 
            Debug.Assert(ArgumentNames IsNot Nothing, "Nothing unexpected")
            Debug.Assert(TypeArguments IsNot Nothing, "Nothing unexpected") 
 
            Failure = ResolutionFailure.None
 
            If ArgumentNames.Length > Arguments.Length OrElse _
               (CopyBack IsNot Nothing AndAlso CopyBack.Length <> Arguments.Length) Then
                Failure = ResolutionFailure.InvalidArgument
 
                If ReportErrors Then
                    Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidValue)) 
                End If 

                Return Nothing 
            End If

            If HasFlag(InvocationFlags, BindingFlags.SetProperty) AndAlso Arguments.Length < 1 Then
                Failure = ResolutionFailure.InvalidArgument 

                If ReportErrors Then 
                    'If we're binding to a Set, we must have at least the Value argument. 
                    Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidValue))
                End If 

                Return Nothing
            End If
 
#If 0 Then
            If BaseReference.IsCOMObject Then 
                Return _ 
                    BaseReference.InvokeCOMMethod( _
                        MethodName, _ 
                        Arguments, _
                        ArgumentNames, _
                        CopyBack, _
                        InvocationFlags) 
            End If
#End If 
 
            Dim Members As MemberInfo() = BaseReference.GetMembers(MethodName, ReportErrors)
 
            If Members Is Nothing OrElse Members.Length = 0 Then
                Failure = ResolutionFailure.MissingMember

                If ReportErrors Then 
                    Debug.Fail("If ReportErrors is True, GetMembers should have thrown above")
                    Members = BaseReference.GetMembers(MethodName, True) 
                End If 

                Return Nothing 
            End If

            Dim TargetProcedure As Method = _
                ResolveCall( _ 
                    BaseReference, _
                    MethodName, _ 
                    Members, _ 
                    Arguments, _
                    ArgumentNames, _ 
                    TypeArguments, _
                    InvocationFlags, _
                    ReportErrors, _
                    Failure) 

            If Failure = ResolutionFailure.None Then 
                Return BaseReference.InvokeMethod(TargetProcedure, Arguments, CopyBack, InvocationFlags) 
            End If
 
            Return Nothing
        End Function

        Friend Shared Function MatchesPropertyRequirements(ByVal TargetProcedure As Method, ByVal Flags As BindingFlags) As MethodInfo 
            Debug.Assert(TargetProcedure.IsProperty, "advertised property method isn't.")
 
            If HasFlag(Flags, BindingFlags.SetProperty) Then 
                Return TargetProcedure.AsProperty.GetSetMethod
            Else 
                Return TargetProcedure.AsProperty.GetGetMethod
            End If
        End Function
 
        Friend Shared Function ReportPropertyMismatch(ByVal TargetProcedure As Method, ByVal Flags As BindingFlags) As Exception
            Debug.Assert(TargetProcedure.IsProperty, "advertised property method isn't.") 
 
            If HasFlag(Flags, BindingFlags.SetProperty) Then
                Debug.Assert(TargetProcedure.AsProperty.GetSetMethod Is Nothing, "expected error condition") 

                Return New MissingMemberException( _
                    GetResourceString(ResID.NoSetProperty1, TargetProcedure.AsProperty.Name))
            Else 
                Debug.Assert(TargetProcedure.AsProperty.GetGetMethod Is Nothing, "expected error condition")
 
                Return New MissingMemberException( _ 
                    GetResourceString(ResID.NoGetProperty1, TargetProcedure.AsProperty.Name))
            End If 
        End Function

        Friend Shared Function ResolveCall( _
            ByVal BaseReference As Container, _ 
            ByVal MethodName As String, _
            ByVal Members As MemberInfo(), _ 
            ByVal Arguments As Object(), _ 
            ByVal ArgumentNames As String(), _
            ByVal TypeArguments As Type(), _ 
            ByVal LookupFlags As BindingFlags, _
            ByVal ReportErrors As Boolean, _
            ByRef Failure As OverloadResolution.ResolutionFailure) As Method
 

            Debug.Assert(BaseReference IsNot Nothing, "expected a base reference") 
            Debug.Assert(MethodName IsNot Nothing, "expected method name") 
            Debug.Assert(Members IsNot Nothing AndAlso Members.Length > 0, "expected members")
            Debug.Assert(Arguments IsNot Nothing AndAlso _ 
                         ArgumentNames IsNot Nothing AndAlso _
                         TypeArguments IsNot Nothing AndAlso _
                         ArgumentNames.Length <= Arguments.Length, _
                         "expected valid argument arrays") 

            Failure = OverloadResolution.ResolutionFailure.None 
 
            If Members(0).MemberType <> MemberTypes.Method AndAlso _
               Members(0).MemberType <> MemberTypes.Property Then 

                Failure = OverloadResolution.ResolutionFailure.InvalidTarget
                If ReportErrors Then
                    'This expression is not a procedure, but occurs as the target of a procedure call. 
                    Throw New ArgumentException( _
                        GetResourceString(ResID.ExpressionNotProcedure, MethodName, BaseReference.VBFriendlyName)) 
                End If 
                Return Nothing
            End If 

            'When binding to Property Set accessors, strip off the last Value argument
            'because it does not participate in overload resolution.
 
            Dim SavedArguments As Object()
            Dim ArgumentCount As Integer = Arguments.Length 
            Dim LastArgument As Object = Nothing 

            If HasFlag(LookupFlags, BindingFlags.SetProperty) Then 
                If Arguments.Length = 0 Then
                    Failure = OverloadResolution.ResolutionFailure.InvalidArgument

                    If ReportErrors Then 
                        Throw New InvalidCastException( _
                            GetResourceString(ResID.PropertySetMissingArgument1, MethodName)) 
                    End If 

                    Return Nothing 
                End If

                SavedArguments = Arguments
                Arguments = New Object(ArgumentCount - 2) {} 
                System.Array.Copy(SavedArguments, Arguments, Arguments.Length)
                LastArgument = SavedArguments(ArgumentCount - 1) 
            End If 

            Dim ResolutionResult As Method = _ 
                ResolveOverloadedCall( _
                    MethodName, _
                    Members, _
                    Arguments, _ 
                    ArgumentNames, _
                    TypeArguments, _ 
                    LookupFlags, _ 
                    ReportErrors, _
                    Failure) 

            Debug.Assert(Failure = OverloadResolution.ResolutionFailure.None OrElse Not ReportErrors, _
                         "if resolution failed, an exception should have been thrown")
 
            If Failure <> OverloadResolution.ResolutionFailure.None Then
                Debug.Assert(ResolutionResult Is Nothing, "resolution failed so should have no result") 
                Return Nothing 
            End If
 
            Debug.Assert(ResolutionResult IsNot Nothing, "resolution didn't fail, so should have result")

#If BINDING_LOG Then
            Console.WriteLine("== RESULT ==") 
            Console.WriteLine(ResolutionResult.DeclaringType.Name & "::" & ResolutionResult.ToString)
            Console.WriteLine() 
#End If 

            'Overload resolution will potentially select one method before validating arguments. 
            'Validate those arguments now.

            If Not ResolutionResult.ArgumentsValidated Then
 
                If Not CanMatchArguments(ResolutionResult, Arguments, ArgumentNames, TypeArguments, False, Nothing) Then
 
                    Failure = OverloadResolution.ResolutionFailure.InvalidArgument 

                    If ReportErrors Then 
                        Dim ErrorMessage As String = ""
                        Dim Errors As New List(Of String)

                        Dim Result As Boolean = _ 
                            CanMatchArguments(ResolutionResult, Arguments, ArgumentNames, TypeArguments, False, Errors)
 
                        Debug.Assert(Result = False AndAlso Errors.Count > 0, "expected this candidate to fail") 

                        For Each ErrorString As String In Errors 
                            ErrorMessage &= vbCrLf & "    " & ErrorString
                        Next

                        ErrorMessage = GetResourceString(ResID.MatchArgumentFailure2, ResolutionResult.ToString, ErrorMessage) 
                        'We are missing a member which can match the arguments, so throw a missing member exception.
 
 
                        Throw New InvalidCastException(ErrorMessage)
                    End If 

                    Return Nothing
                End If
 
            End If
 
            'Once we've gotten this far, we've selected a member. From this point on, we determine 
            'if the member can be called given the context.
 
            'Check that the resulting binding makes sense in the current context.
            If ResolutionResult.IsProperty Then
                If MatchesPropertyRequirements(ResolutionResult, LookupFlags) Is Nothing Then
                    Failure = OverloadResolution.ResolutionFailure.InvalidTarget 
                    If ReportErrors Then
                        Throw ReportPropertyMismatch(ResolutionResult, LookupFlags) 
                    End If 
                    Return Nothing
                End If 
            Else
                Debug.Assert(ResolutionResult.IsMethod, "must be a method")
                If HasFlag(LookupFlags, BindingFlags.SetProperty) Then
                    Failure = OverloadResolution.ResolutionFailure.InvalidTarget 
                    If ReportErrors Then
                        'Methods can't be targets of assignments. 
 
                        Throw New MissingMemberException( _
                            GetResourceString(ResID.MethodAssignment1, ResolutionResult.AsMethod.Name)) 
                    End If
                    Return Nothing
                End If
            End If 

            If HasFlag(LookupFlags, BindingFlags.SetProperty) Then 
                'Need to match the Value argument for the property set call. 
                Debug.Assert(GetCallTarget(ResolutionResult, LookupFlags).Name.StartsWith("set_"), "expected set accessor")
 
                Dim Parameters As ParameterInfo() = GetCallTarget(ResolutionResult, LookupFlags).GetParameters
                Dim LastParameter As ParameterInfo = Parameters(Parameters.Length - 1)
                If Not CanPassToParameter( _
                            ResolutionResult, _ 
                            LastArgument, _
                            LastParameter, _ 
                            False, _ 
                            False, _
                            Nothing, _ 
                            Nothing, _
                            Nothing) Then

                    Failure = OverloadResolution.ResolutionFailure.InvalidArgument 

                    If ReportErrors Then 
                        Dim ErrorMessage As String = "" 
                        Dim Errors As New List(Of String)
 
                        Dim Result As Boolean = _
                            CanPassToParameter( _
                                ResolutionResult, _
                                LastArgument, _ 
                                LastParameter, _
                                False, _ 
                                False, _ 
                                Errors, _
                                Nothing, _ 
                                Nothing)

                        Debug.Assert(Result = False AndAlso Errors.Count > 0, "expected this candidate to fail")
 
                        For Each ErrorString As String In Errors
                            ErrorMessage &= vbCrLf & "    " & ErrorString 
                        Next 

                        ErrorMessage = GetResourceString(ResID.MatchArgumentFailure2, ResolutionResult.ToString, ErrorMessage) 
                        'The selected member can't handle the type of the Value argument, so this is an argument exception.


                        Throw New InvalidCastException(ErrorMessage) 
                    End If
 
                    Return Nothing 
                End If
            End If 

            Return ResolutionResult
        End Function
 
        Friend Shared Function GetCallTarget(ByVal TargetProcedure As Method, ByVal Flags As BindingFlags) As MethodBase
            If TargetProcedure.IsMethod Then Return TargetProcedure.AsMethod 
            If TargetProcedure.IsProperty Then Return MatchesPropertyRequirements(TargetProcedure, Flags) 
            Debug.Fail("not a method or property??")
            Return Nothing 
        End Function

        Friend Shared Function ConstructCallArguments( _
            ByVal TargetProcedure As Method, _ 
            ByVal Arguments As Object(), _
            ByVal LookupFlags As BindingFlags) As Object() 
 
            Debug.Assert(TargetProcedure IsNot Nothing AndAlso Arguments IsNot Nothing, "expected arguments")
 

            Dim Parameters As ParameterInfo() = GetCallTarget(TargetProcedure, LookupFlags).GetParameters
            Dim CallArguments As Object() = New Object(Parameters.Length - 1) {}
 
            Dim SavedArguments As Object()
            Dim ArgumentCount As Integer = Arguments.Length 
            Dim LastArgument As Object = Nothing 

            If HasFlag(LookupFlags, BindingFlags.SetProperty) Then 
                Debug.Assert(Arguments.Length > 0, "must have an argument for property set Value")
                SavedArguments = Arguments
                Arguments = New Object(ArgumentCount - 2) {}
                System.Array.Copy(SavedArguments, Arguments, Arguments.Length) 
                LastArgument = SavedArguments(ArgumentCount - 1)
            End If 
 
            MatchArguments(TargetProcedure, Arguments, CallArguments)
 
            If HasFlag(LookupFlags, BindingFlags.SetProperty) Then
                'Need to match the Value argument for the property set call.
                Debug.Assert(GetCallTarget(TargetProcedure, LookupFlags).Name.StartsWith("set_"), "expected set accessor")
 
                Dim LastParameter As ParameterInfo = Parameters(Parameters.Length - 1)
                CallArguments(Parameters.Length - 1) = _ 
                    PassToParameter(LastArgument, LastParameter, LastParameter.ParameterType) 
            End If
 
            Return CallArguments
        End Function

    End Class 
End Namespace

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
'*****************************************************************************/ 
'* NewLateBinder.vb
'*
'*  Copyright (C), Microsoft Corporation.  All Rights Reserved.
'*  Information Contained Herein Is Proprietary and Confidential. 
'*
'* Purpose: 
'*  Implements VB late binder. 
'*
'*****************************************************************************/ 

Imports System
Imports System.Reflection
Imports System.Diagnostics 
Imports System.Runtime.InteropServices
Imports System.Collections 
Imports System.Collections.Generic 
Imports System.Security.Permissions
Imports System.Globalization 

Imports Microsoft.VisualBasic.CompilerServices.Symbols
Imports Microsoft.VisualBasic.CompilerServices.OverloadResolution
 
#Const NEW_BINDER = True
#Const BINDING_LOG = False 
 

 



Namespace Microsoft.VisualBasic.CompilerServices 

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)> _ 
    Public NotInheritable Class NewLateBinding 
        ' Prevent creation.
        Private Sub New() 
        End Sub

        <DebuggerHiddenAttribute()> <DebuggerStepThroughAttribute()> _
        Public Shared Function LateCanEvaluate( _ 
            ByVal instance As Object, _
            ByVal type As System.Type, _ 
            ByVal memberName As String, _ 
            ByVal arguments As Object(), _
            ByVal allowFunctionEvaluation As Boolean, _ 
            ByVal allowPropertyEvaluation As Boolean) As Boolean

            Dim BaseReference As Container
            If Type IsNot Nothing Then 
                BaseReference = New Container(type)
            Else 
                BaseReference = New Container(instance) 
            End If
 
            Dim Members As MemberInfo() = BaseReference.GetMembers(memberName, False)

            If Members.Length = 0 Then
                Return True 
            End If
 
            ' This is a field access 
            If Members(0).MemberType = MemberTypes.Field Then
                If arguments.Length = 0 Then 
                    Return True
                Else
                    Dim FieldValue As Object = BaseReference.GetFieldValue(DirectCast(Members(0), FieldInfo))
                    BaseReference = New Container(FieldValue) 
                    If BaseReference.IsArray Then
                        Return True 
                    End If 
                    Return allowPropertyEvaluation
                End If 
            End If

            ' This is a method invocation
            If Members(0).MemberType = MemberTypes.Method Then 
                Return allowFunctionEvaluation
            End If 
 
            ' This is a property access
            If Members(0).MemberType = MemberTypes.Property Then 
                Return allowPropertyEvaluation
            End If

            Return True 
        End Function
 
 

        <DebuggerHiddenAttribute()> <DebuggerStepThroughAttribute()> _ 
        Public Shared Function LateCall( _
            ByVal Instance As Object, _
            ByVal Type As System.Type, _
            ByVal MemberName As String, _ 
            ByVal Arguments As Object(), _
            ByVal ArgumentNames As String(), _ 
            ByVal TypeArguments As System.Type(), _ 
            ByVal CopyBack As Boolean(), _
            ByVal IgnoreReturn As Boolean) As Object 

#If Not NEW_BINDER Then
            Return LateBinding.InternalLateCall(Instance, Type, MemberName, Arguments, ArgumentNames, CopyBack, False)
#End If 
            If Arguments Is Nothing Then Arguments = NoArguments
            If ArgumentNames Is Nothing Then ArgumentNames = NoArgumentNames 
            If TypeArguments Is Nothing Then TypeArguments = NoTypeArguments 

            Dim BaseReference As Container 
            If Type IsNot Nothing Then
                BaseReference = New Container(Type)
            Else
                BaseReference = New Container(Instance) 
            End If
 
            If BaseReference.IsCOMObject Then 
                ' call the old binder.
                Return LateBinding.InternalLateCall(Instance, Type, MemberName, Arguments, ArgumentNames, CopyBack, IgnoreReturn) 
            End If

            Dim InvocationFlags As BindingFlags = BindingFlags.InvokeMethod Or BindingFlags.GetProperty
            If IgnoreReturn Then InvocationFlags = InvocationFlags Or BindingFlags.IgnoreReturn 

            Dim Failure As ResolutionFailure 
 
            Return _
                CallMethod( _ 
                    BaseReference, _
                    MemberName, _
                    Arguments, _
                    ArgumentNames, _ 
                    TypeArguments, _
                    CopyBack, _ 
                    InvocationFlags, _ 
                    True, _
                    Failure) 

        End Function

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()> _ 
        Public Shared Function LateIndexGet( _
            ByVal Instance As Object, _ 
            ByVal Arguments() As Object, _ 
            ByVal ArgumentNames() As String) As Object
 
            Dim Failure As ResolutionFailure
            Return InternalLateIndexGet( _
                Instance, _
                Arguments, _ 
                ArgumentNames, _
                True, _ 
                Failure) 
        End Function
 
        Friend Shared Function InternalLateIndexGet( _
            ByVal Instance As Object, _
            ByVal Arguments() As Object, _
            ByVal ArgumentNames() As String, _ 
            ByVal ReportErrors As Boolean, _
            ByRef Failure As ResolutionFailure) As Object 
 
            Failure = ResolutionFailure.None
 
#If Not NEW_BINDER Then
            Return LateBinding.LateIndexGet(Instance, Arguments, ArgumentNames)
#End If
            If Arguments Is Nothing Then Arguments = NoArguments 
            If ArgumentNames Is Nothing Then ArgumentNames = NoArgumentNames
 
            Dim BaseReference As Container = New Container(Instance) 

            If BaseReference.IsCOMObject Then 
                ' call the old binder.
                Return LateBinding.LateIndexGet(Instance, Arguments, ArgumentNames)
            End If
 
            'An r-value expression o(a) has two possible forms:
            '    1: o(a)    array lookup--where o is an array object and a is a set of indices 
            '    2: o.d(a)  default member access--where o has default method/property d 

            If BaseReference.IsArray Then 
                'This is an array lookup o(a).

                If ArgumentNames.Length > 0 Then
                    Failure = ResolutionFailure.InvalidArgument 

                    If ReportErrors Then 
                        Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidNamedArgs)) 
                    End If
 
                    Return Nothing
                End If

                Return BaseReference.GetArrayValue(Arguments) 
            End If
 
            'This is a default member access o.d(a), which is a call to method "". 

            Return _ 
                CallMethod( _
                    BaseReference, _
                    "", _
                    Arguments, _ 
                    ArgumentNames, _
                    NoTypeArguments, _ 
                    Nothing, _ 
                    BindingFlags.InvokeMethod Or BindingFlags.GetProperty, _
                    ReportErrors, _ 
                    Failure)
        End Function

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()> _ 
        Public Shared Function LateGet( _
            ByVal Instance As Object, _ 
            ByVal Type As System.Type, _ 
            ByVal MemberName As String, _
            ByVal Arguments As Object(), _ 
            ByVal ArgumentNames As String(), _
            ByVal TypeArguments As Type(), _
            ByVal CopyBack As Boolean()) As Object
 
#If Not NEW_BINDER Then
            Return LateBinding.LateGet(Instance, Type, MemberName, Arguments, ArgumentNames, CopyBack) 
#End If 

            If Arguments Is Nothing Then Arguments = NoArguments 
            If ArgumentNames Is Nothing Then ArgumentNames = NoArgumentNames
            If TypeArguments Is Nothing Then TypeArguments = NoTypeArguments

            Dim BaseReference As Container 
            If Type IsNot Nothing Then
                BaseReference = New Container(Type) 
            Else 
                BaseReference = New Container(Instance)
            End If 

            Dim InvocationFlags As BindingFlags = BindingFlags.InvokeMethod Or BindingFlags.GetProperty

 
            If BaseReference.IsCOMObject Then
                ' call the old binder. 
                Return LateBinding.LateGet(Instance, Type, MemberName, Arguments, ArgumentNames, CopyBack) 

            End If 

            Dim Members As MemberInfo() = BaseReference.GetMembers(MemberName, True)

            If Members(0).MemberType = MemberTypes.Field Then 
                If TypeArguments.Length > 0 Then
                    Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidValue)) 
                End If 

                Dim FieldValue As Object = BaseReference.GetFieldValue(DirectCast(Members(0), FieldInfo)) 
                If Arguments.Length = 0 Then
                    'This is a simple field access.
                    Return FieldValue
                Else 
                    'This is an indexed field access.
                    Return LateIndexGet(FieldValue, Arguments, ArgumentNames) 
                End If 
            End If
 
            If ArgumentNames.Length > Arguments.Length OrElse _
               (CopyBack IsNot Nothing AndAlso CopyBack.Length <> Arguments.Length) Then
                Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidValue))
            End If 

            Dim Failure As OverloadResolution.ResolutionFailure 
            Dim TargetProcedure As Method = _ 
                ResolveCall( _
                    BaseReference, _ 
                    MemberName, _
                    Members, _
                    Arguments, _
                    ArgumentNames, _ 
                    TypeArguments, _
                    InvocationFlags, _ 
                    False, _ 
                    Failure)
 
            If Failure = OverloadResolution.ResolutionFailure.None Then
                Return BaseReference.InvokeMethod(TargetProcedure, Arguments, CopyBack, InvocationFlags)

            ElseIf Arguments.Length > 0 Then 

                TargetProcedure = _ 
                    ResolveCall( _ 
                        BaseReference, _
                        MemberName, _ 
                        Members, _
                        NoArguments, _
                        NoArgumentNames, _
                        TypeArguments, _ 
                        InvocationFlags, _
                        False, _ 
                        Failure) 

                If Failure = OverloadResolution.ResolutionFailure.None Then 
                    Dim Result As Object = BaseReference.InvokeMethod(TargetProcedure, NoArguments, Nothing, InvocationFlags)

                    'For backwards compatibility, throw a missing member exception if the intermediate result is Nothing.
                    If Result Is Nothing Then 
                        Throw New _
                            MissingMemberException( _ 
                                GetResourceString( _ 
                                    ResID.IntermediateLateBoundNothingResult1, _
                                    TargetProcedure.ToString, _ 
                                    BaseReference.VBFriendlyName))
                    End If

                    Result = InternalLateIndexGet( _ 
                                Result, _
                                Arguments, _ 
                                ArgumentNames, _ 
                                False, _
                                Failure) 

                    If Failure = ResolutionFailure.None Then
                        Return Result
                    End If 
                End If
 
            End If 

            'Every attempt to make this work failed.  Redo the original call resolution to generate errors. 
            ResolveCall( _
                BaseReference, _
                MemberName, _
                Members, _ 
                Arguments, _
                ArgumentNames, _ 
                TypeArguments, _ 
                InvocationFlags, _
                True, _ 
                Failure)

            Debug.Fail("the resolution should have thrown an exception")
            Throw New InternalErrorException() 
        End Function
 
 
        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()> _
        Public Shared Sub LateIndexSetComplex( _ 
            ByVal Instance As Object, _
            ByVal Arguments As Object(), _
            ByVal ArgumentNames As String(), _
            ByVal OptimisticSet As Boolean, _ 
            ByVal RValueBase As Boolean)
 
#If Not NEW_BINDER Then 
            LateBinding.LateIndexSetComplex(Instance, Arguments, ArgumentNames, OptimisticSet, RValueBase)
            Return 
#End If


 

 
            If Arguments Is Nothing Then Arguments = NoArguments 
            If ArgumentNames Is Nothing Then ArgumentNames = NoArgumentNames
 
            Dim BaseReference As Container = New Container(Instance)

            'An l-value expression o(a) has two possible forms:
            '    1: o(a) = v    array lookup--where o is an array object and a is a set of indices 
            '    2: o.d(a) = v  default member access--where o has default method/property d
 
            If BaseReference.IsArray Then 
                'This is an array lookup and assignment o(a) = v.
 
                If ArgumentNames.Length > 0 Then
                    Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidNamedArgs))
                End If
 
                BaseReference.SetArrayValue(Arguments)
                Return 
            End If 

            If ArgumentNames.Length > Arguments.Length Then 
                Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidValue))
            End If

            If Arguments.Length < 1 Then 
                'We're binding to a Set, we must have at least the Value argument.
                Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidValue)) 
            End If 

            Dim MethodName As String = "" 

            If BaseReference.IsCOMObject Then
                ' call the old binder.
                LateBinding.LateIndexSetComplex(Instance, Arguments, ArgumentNames, OptimisticSet, RValueBase) 
                Return
#If 0 Then 
                Try 
                    BaseReference.InvokeCOMMethod( _
                        MethodName, _ 
                        Arguments, _
                        ArgumentNames, _
                        Nothing, _
                        GetPropertyPutFlags(Arguments(Arguments.Length - 1))) 

                    If RValueBase AndAlso BaseReference.IsValueType Then 
                        Throw New Exception( _ 
                                GetResourceString( _
                                    ResID.RValueBaseForValueType, _ 
                                    BaseReference.VBFriendlyName, _
                                    BaseReference.VBFriendlyName))
                    End If
                Catch ex As System.MissingMemberException When OptimisticSet = True 
                    'A missing member exception means it has no Set member.  Silently handle the exception.
                End Try 
                Return 
#End If
 
            Else
                Dim InvocationFlags As BindingFlags = BindingFlags.SetProperty

                Dim Members As MemberInfo() = BaseReference.GetMembers(MethodName, True) 'MethodName is set during this call. 

                Dim Failure As OverloadResolution.ResolutionFailure 
                Dim TargetProcedure As Method = _ 
                    ResolveCall( _
                        BaseReference, _ 
                        MethodName, _
                        Members, _
                        Arguments, _
                        ArgumentNames, _ 
                        NoTypeArguments, _
                        InvocationFlags, _ 
                        False, _ 
                        Failure)
 
                If Failure = OverloadResolution.ResolutionFailure.None Then

                    If RValueBase AndAlso BaseReference.IsValueType Then
                        Throw New Exception( _ 
                                GetResourceString( _
                                    ResID.RValueBaseForValueType, _ 
                                    BaseReference.VBFriendlyName, _ 
                                    BaseReference.VBFriendlyName))
                    End If 

                    BaseReference.InvokeMethod(TargetProcedure, Arguments, Nothing, InvocationFlags)
                    Return
 
                ElseIf OptimisticSet Then
                    Return 
 
                Else
                    'Redo the resolution to generate errors. 
                    ResolveCall( _
                        BaseReference, _
                        MethodName, _
                        Members, _ 
                        Arguments, _
                        ArgumentNames, _ 
                        NoTypeArguments, _ 
                        InvocationFlags, _
                        True, _ 
                        Failure)
                End If
            End If
 
            Debug.Fail("the resolution should have thrown an exception - should never reach here")
            Throw New InternalErrorException() 
 
        End Sub
 

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()> _
        Public Shared Sub LateIndexSet( _
            ByVal Instance As Object, _ 
            ByVal Arguments() As Object, _
            ByVal ArgumentNames() As String) 
 
#If Not NEW_BINDER Then
            LateBinding.LateIndexSet(Instance, Arguments, ArgumentNames) 
            Return
#End If

            LateIndexSetComplex(Instance, Arguments, ArgumentNames, False, False) 
            Return
        End Sub 
 
        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()> _
        Public Shared Sub LateSetComplex( _ 
            ByVal Instance As Object, _
            ByVal Type As Type, _
            ByVal MemberName As String, _
            ByVal Arguments() As Object, _ 
            ByVal ArgumentNames() As String, _
            ByVal TypeArguments() As Type, _ 
            ByVal OptimisticSet As Boolean, _ 
            ByVal RValueBase As Boolean)
 
#If Not NEW_BINDER Then
            LateBinding.LateSetComplex(Instance, Type, MemberName, Arguments, ArgumentNames, OptimisticSet, RValueBase)
            Return
#End If 
            Const DefaultCallType As CallType = CType(0, CallType)
            LateSet(Instance, Type, MemberName, Arguments, ArgumentNames, TypeArguments, OptimisticSet, RValueBase, DefaultCallType) 
        End Sub 

 
        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()> _
        Public Shared Sub LateSet( _
            ByVal Instance As Object, _
            ByVal Type As Type, _ 
            ByVal MemberName As String, _
            ByVal Arguments() As Object, _ 
            ByVal ArgumentNames() As String, _ 
            ByVal TypeArguments As Type())
 
#If Not NEW_BINDER Then
            LateBinding.LateSet(Instance, Type, MemberName, Arguments, ArgumentNames)
            Return
#End If 

            Const DefaultCallType As CallType = CType(0, CallType) 
            LateSet(Instance, Type, MemberName, Arguments, ArgumentNames, TypeArguments, False, False, DefaultCallType) 
            Return
        End Sub 

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()> _
        Public Shared Sub LateSet( _
            ByVal Instance As Object, _ 
            ByVal Type As Type, _
            ByVal MemberName As String, _ 
            ByVal Arguments As Object(), _ 
            ByVal ArgumentNames As String(), _
            ByVal TypeArguments As Type(), _ 
            ByVal OptimisticSet As Boolean, _
            ByVal RValueBase As Boolean, _
            ByVal CallType As CallType)
 

 
 

 

            If Arguments Is Nothing Then Arguments = NoArguments
            If ArgumentNames Is Nothing Then ArgumentNames = NoArgumentNames
            If TypeArguments Is Nothing Then TypeArguments = NoTypeArguments 

            Dim BaseReference As Container 
            If Type IsNot Nothing Then 
                BaseReference = New Container(Type)
            Else 
                BaseReference = New Container(Instance)
            End If

            Dim InvocationFlags As BindingFlags 

            If BaseReference.IsCOMObject Then 
                '  call the old binder. 
                Try
 



 

                    LateBinding.InternalLateSet(Instance, Type, MemberName, Arguments, ArgumentNames, OptimisticSet, CallType) 
 
                    If RValueBase AndAlso Type.IsValueType Then
                        'note that objType is passed byref to InternalLateSet and that it 
                        'should be valid by the time we get to this point
                        Throw New Exception(GetResourceString(ResID.RValueBaseForValueType, VBFriendlyName(Type, Instance), VBFriendlyName(Type, Instance)))
                    End If
                Catch ex As System.MissingMemberException When OptimisticSet = True 
                    'A missing member exception means it has no Set member.  Silently handle the exception.
                End Try 
 
                Return
#If 0 Then 
                If CallType = CallType.Set Then
                    InvocationFlags = InvocationFlags Or BindingFlags.PutRefDispProperty
                    If Arguments(Arguments.GetUpperBound(0)) Is Nothing Then
                        Arguments(Arguments.GetUpperBound(0)) = New DispatchWrapper(Nothing) 
                    End If
                ElseIf CallType = CallType.Let Then 
                    InvocationFlags = InvocationFlags Or BindingFlags.PutDispProperty 
                Else
                    InvocationFlags = InvocationFlags Or GetPropertyPutFlags(Arguments(Arguments.GetUpperBound(0))) 
                End If


                BaseReference.InvokeCOMMethod2(MemberName, Arguments, ArgumentNames, Nothing, InvocationFlags) 
                Return
#End If 
            End If 

            Dim Members As MemberInfo() = BaseReference.GetMembers(MemberName, True) 

            If Members(0).MemberType = MemberTypes.Field Then

                If TypeArguments.Length > 0 Then 
                    Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidValue))
                End If 
 
                If Arguments.Length = 1 Then
                    If RValueBase AndAlso BaseReference.IsValueType Then 
                        Throw New Exception( _
                                GetResourceString( _
                                    ResID.RValueBaseForValueType, _
                                    BaseReference.VBFriendlyName, _ 
                                    BaseReference.VBFriendlyName))
                    End If 
                    'This is a simple field set. 
                    BaseReference.SetFieldValue(DirectCast(Members(0), FieldInfo), Arguments(0))
                    Return 
                Else
                    'This is an indexed field set.
                    Dim FieldValue As Object = BaseReference.GetFieldValue(DirectCast(Members(0), FieldInfo))
                    LateIndexSetComplex(FieldValue, Arguments, ArgumentNames, OptimisticSet, True) 
                    Return
                End If 
            End If 

            InvocationFlags = BindingFlags.SetProperty 

            If ArgumentNames.Length > Arguments.Length Then
                Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidValue))
            End If 

            Dim Failure As OverloadResolution.ResolutionFailure 
            Dim TargetProcedure As Method 

            If TypeArguments.Length = 0 Then 

                TargetProcedure = _
                    ResolveCall( _
                        BaseReference, _ 
                        MemberName, _
                        Members, _ 
                        Arguments, _ 
                        ArgumentNames, _
                        NoTypeArguments, _ 
                        InvocationFlags, _
                        False, _
                        Failure)
 
                If Failure = OverloadResolution.ResolutionFailure.None Then
                    If RValueBase AndAlso BaseReference.IsValueType Then 
                        Throw New Exception( _ 
                                GetResourceString( _
                                    ResID.RValueBaseForValueType, _ 
                                    BaseReference.VBFriendlyName, _
                                    BaseReference.VBFriendlyName))
                    End If
 
                    BaseReference.InvokeMethod(TargetProcedure, Arguments, Nothing, InvocationFlags)
                    Return 
                End If 

            End If 

            Dim SecondaryInvocationFlags As BindingFlags = _
                    BindingFlags.InvokeMethod Or BindingFlags.GetProperty
 
            If Failure = OverloadResolution.ResolutionFailure.None OrElse Failure = OverloadResolution.ResolutionFailure.MissingMember Then
 
                TargetProcedure = _ 
                    ResolveCall( _
                        BaseReference, _ 
                        MemberName, _
                        Members, _
                        NoArguments, _
                        NoArgumentNames, _ 
                        TypeArguments, _
                        SecondaryInvocationFlags, _ 
                        False, _ 
                        Failure)
 
                If Failure = OverloadResolution.ResolutionFailure.None Then
                    Dim Result As Object = _
                        BaseReference.InvokeMethod(TargetProcedure, NoArguments, Nothing, SecondaryInvocationFlags)
 
                    'For backwards compatibility, throw a missing member exception if the intermediate result is Nothing.
                    If Result Is Nothing Then 
                        Throw New _ 
                            MissingMemberException( _
                                GetResourceString( _ 
                                    ResID.IntermediateLateBoundNothingResult1, _
                                    TargetProcedure.ToString, _
                                    BaseReference.VBFriendlyName))
                    End If 

                    LateIndexSetComplex(Result, Arguments, ArgumentNames, OptimisticSet, True) 
                    Return 
                End If
            End If 

            If OptimisticSet Then
                Return
            End If 

            'Everything failed, so give errors. Redo the first attempt to generate the errors. 
            If TypeArguments.Length = 0 Then 
                ResolveCall( _
                    BaseReference, _ 
                    MemberName, _
                    Members, _
                    Arguments, _
                    ArgumentNames, _ 
                    TypeArguments, _
                    InvocationFlags, _ 
                    True, _ 
                    Failure)
 
            Else
                ResolveCall( _
                    BaseReference, _
                    MemberName, _ 
                    Members, _
                    NoArguments, _ 
                    NoArgumentNames, _ 
                    TypeArguments, _
                    SecondaryInvocationFlags, _ 
                    True, _
                    Failure)
            End If
 
            Debug.Fail("the resolution should have thrown an exception")
            Throw New InternalErrorException() 
            Return 
        End Sub
 
        Private Shared Function CallMethod( _
            ByVal BaseReference As Container, _
            ByVal MethodName As String, _
            ByVal Arguments As Object(), _ 
            ByVal ArgumentNames As String(), _
            ByVal TypeArguments As System.Type(), _ 
            ByVal CopyBack As Boolean(), _ 
            ByVal InvocationFlags As BindingFlags, _
            ByVal ReportErrors As Boolean, _ 
            ByRef Failure As ResolutionFailure) As Object

            Debug.Assert(BaseReference IsNot Nothing, "Nothing unexpected")
            Debug.Assert(Arguments IsNot Nothing, "Nothing unexpected") 
            Debug.Assert(ArgumentNames IsNot Nothing, "Nothing unexpected")
            Debug.Assert(TypeArguments IsNot Nothing, "Nothing unexpected") 
 
            Failure = ResolutionFailure.None
 
            If ArgumentNames.Length > Arguments.Length OrElse _
               (CopyBack IsNot Nothing AndAlso CopyBack.Length <> Arguments.Length) Then
                Failure = ResolutionFailure.InvalidArgument
 
                If ReportErrors Then
                    Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidValue)) 
                End If 

                Return Nothing 
            End If

            If HasFlag(InvocationFlags, BindingFlags.SetProperty) AndAlso Arguments.Length < 1 Then
                Failure = ResolutionFailure.InvalidArgument 

                If ReportErrors Then 
                    'If we're binding to a Set, we must have at least the Value argument. 
                    Throw New ArgumentException(GetResourceString(ResID.Argument_InvalidValue))
                End If 

                Return Nothing
            End If
 
#If 0 Then
            If BaseReference.IsCOMObject Then 
                Return _ 
                    BaseReference.InvokeCOMMethod( _
                        MethodName, _ 
                        Arguments, _
                        ArgumentNames, _
                        CopyBack, _
                        InvocationFlags) 
            End If
#End If 
 
            Dim Members As MemberInfo() = BaseReference.GetMembers(MethodName, ReportErrors)
 
            If Members Is Nothing OrElse Members.Length = 0 Then
                Failure = ResolutionFailure.MissingMember

                If ReportErrors Then 
                    Debug.Fail("If ReportErrors is True, GetMembers should have thrown above")
                    Members = BaseReference.GetMembers(MethodName, True) 
                End If 

                Return Nothing 
            End If

            Dim TargetProcedure As Method = _
                ResolveCall( _ 
                    BaseReference, _
                    MethodName, _ 
                    Members, _ 
                    Arguments, _
                    ArgumentNames, _ 
                    TypeArguments, _
                    InvocationFlags, _
                    ReportErrors, _
                    Failure) 

            If Failure = ResolutionFailure.None Then 
                Return BaseReference.InvokeMethod(TargetProcedure, Arguments, CopyBack, InvocationFlags) 
            End If
 
            Return Nothing
        End Function

        Friend Shared Function MatchesPropertyRequirements(ByVal TargetProcedure As Method, ByVal Flags As BindingFlags) As MethodInfo 
            Debug.Assert(TargetProcedure.IsProperty, "advertised property method isn't.")
 
            If HasFlag(Flags, BindingFlags.SetProperty) Then 
                Return TargetProcedure.AsProperty.GetSetMethod
            Else 
                Return TargetProcedure.AsProperty.GetGetMethod
            End If
        End Function
 
        Friend Shared Function ReportPropertyMismatch(ByVal TargetProcedure As Method, ByVal Flags As BindingFlags) As Exception
            Debug.Assert(TargetProcedure.IsProperty, "advertised property method isn't.") 
 
            If HasFlag(Flags, BindingFlags.SetProperty) Then
                Debug.Assert(TargetProcedure.AsProperty.GetSetMethod Is Nothing, "expected error condition") 

                Return New MissingMemberException( _
                    GetResourceString(ResID.NoSetProperty1, TargetProcedure.AsProperty.Name))
            Else 
                Debug.Assert(TargetProcedure.AsProperty.GetGetMethod Is Nothing, "expected error condition")
 
                Return New MissingMemberException( _ 
                    GetResourceString(ResID.NoGetProperty1, TargetProcedure.AsProperty.Name))
            End If 
        End Function

        Friend Shared Function ResolveCall( _
            ByVal BaseReference As Container, _ 
            ByVal MethodName As String, _
            ByVal Members As MemberInfo(), _ 
            ByVal Arguments As Object(), _ 
            ByVal ArgumentNames As String(), _
            ByVal TypeArguments As Type(), _ 
            ByVal LookupFlags As BindingFlags, _
            ByVal ReportErrors As Boolean, _
            ByRef Failure As OverloadResolution.ResolutionFailure) As Method
 

            Debug.Assert(BaseReference IsNot Nothing, "expected a base reference") 
            Debug.Assert(MethodName IsNot Nothing, "expected method name") 
            Debug.Assert(Members IsNot Nothing AndAlso Members.Length > 0, "expected members")
            Debug.Assert(Arguments IsNot Nothing AndAlso _ 
                         ArgumentNames IsNot Nothing AndAlso _
                         TypeArguments IsNot Nothing AndAlso _
                         ArgumentNames.Length <= Arguments.Length, _
                         "expected valid argument arrays") 

            Failure = OverloadResolution.ResolutionFailure.None 
 
            If Members(0).MemberType <> MemberTypes.Method AndAlso _
               Members(0).MemberType <> MemberTypes.Property Then 

                Failure = OverloadResolution.ResolutionFailure.InvalidTarget
                If ReportErrors Then
                    'This expression is not a procedure, but occurs as the target of a procedure call. 
                    Throw New ArgumentException( _
                        GetResourceString(ResID.ExpressionNotProcedure, MethodName, BaseReference.VBFriendlyName)) 
                End If 
                Return Nothing
            End If 

            'When binding to Property Set accessors, strip off the last Value argument
            'because it does not participate in overload resolution.
 
            Dim SavedArguments As Object()
            Dim ArgumentCount As Integer = Arguments.Length 
            Dim LastArgument As Object = Nothing 

            If HasFlag(LookupFlags, BindingFlags.SetProperty) Then 
                If Arguments.Length = 0 Then
                    Failure = OverloadResolution.ResolutionFailure.InvalidArgument

                    If ReportErrors Then 
                        Throw New InvalidCastException( _
                            GetResourceString(ResID.PropertySetMissingArgument1, MethodName)) 
                    End If 

                    Return Nothing 
                End If

                SavedArguments = Arguments
                Arguments = New Object(ArgumentCount - 2) {} 
                System.Array.Copy(SavedArguments, Arguments, Arguments.Length)
                LastArgument = SavedArguments(ArgumentCount - 1) 
            End If 

            Dim ResolutionResult As Method = _ 
                ResolveOverloadedCall( _
                    MethodName, _
                    Members, _
                    Arguments, _ 
                    ArgumentNames, _
                    TypeArguments, _ 
                    LookupFlags, _ 
                    ReportErrors, _
                    Failure) 

            Debug.Assert(Failure = OverloadResolution.ResolutionFailure.None OrElse Not ReportErrors, _
                         "if resolution failed, an exception should have been thrown")
 
            If Failure <> OverloadResolution.ResolutionFailure.None Then
                Debug.Assert(ResolutionResult Is Nothing, "resolution failed so should have no result") 
                Return Nothing 
            End If
 
            Debug.Assert(ResolutionResult IsNot Nothing, "resolution didn't fail, so should have result")

#If BINDING_LOG Then
            Console.WriteLine("== RESULT ==") 
            Console.WriteLine(ResolutionResult.DeclaringType.Name & "::" & ResolutionResult.ToString)
            Console.WriteLine() 
#End If 

            'Overload resolution will potentially select one method before validating arguments. 
            'Validate those arguments now.

            If Not ResolutionResult.ArgumentsValidated Then
 
                If Not CanMatchArguments(ResolutionResult, Arguments, ArgumentNames, TypeArguments, False, Nothing) Then
 
                    Failure = OverloadResolution.ResolutionFailure.InvalidArgument 

                    If ReportErrors Then 
                        Dim ErrorMessage As String = ""
                        Dim Errors As New List(Of String)

                        Dim Result As Boolean = _ 
                            CanMatchArguments(ResolutionResult, Arguments, ArgumentNames, TypeArguments, False, Errors)
 
                        Debug.Assert(Result = False AndAlso Errors.Count > 0, "expected this candidate to fail") 

                        For Each ErrorString As String In Errors 
                            ErrorMessage &= vbCrLf & "    " & ErrorString
                        Next

                        ErrorMessage = GetResourceString(ResID.MatchArgumentFailure2, ResolutionResult.ToString, ErrorMessage) 
                        'We are missing a member which can match the arguments, so throw a missing member exception.
 
 
                        Throw New InvalidCastException(ErrorMessage)
                    End If 

                    Return Nothing
                End If
 
            End If
 
            'Once we've gotten this far, we've selected a member. From this point on, we determine 
            'if the member can be called given the context.
 
            'Check that the resulting binding makes sense in the current context.
            If ResolutionResult.IsProperty Then
                If MatchesPropertyRequirements(ResolutionResult, LookupFlags) Is Nothing Then
                    Failure = OverloadResolution.ResolutionFailure.InvalidTarget 
                    If ReportErrors Then
                        Throw ReportPropertyMismatch(ResolutionResult, LookupFlags) 
                    End If 
                    Return Nothing
                End If 
            Else
                Debug.Assert(ResolutionResult.IsMethod, "must be a method")
                If HasFlag(LookupFlags, BindingFlags.SetProperty) Then
                    Failure = OverloadResolution.ResolutionFailure.InvalidTarget 
                    If ReportErrors Then
                        'Methods can't be targets of assignments. 
 
                        Throw New MissingMemberException( _
                            GetResourceString(ResID.MethodAssignment1, ResolutionResult.AsMethod.Name)) 
                    End If
                    Return Nothing
                End If
            End If 

            If HasFlag(LookupFlags, BindingFlags.SetProperty) Then 
                'Need to match the Value argument for the property set call. 
                Debug.Assert(GetCallTarget(ResolutionResult, LookupFlags).Name.StartsWith("set_"), "expected set accessor")
 
                Dim Parameters As ParameterInfo() = GetCallTarget(ResolutionResult, LookupFlags).GetParameters
                Dim LastParameter As ParameterInfo = Parameters(Parameters.Length - 1)
                If Not CanPassToParameter( _
                            ResolutionResult, _ 
                            LastArgument, _
                            LastParameter, _ 
                            False, _ 
                            False, _
                            Nothing, _ 
                            Nothing, _
                            Nothing) Then

                    Failure = OverloadResolution.ResolutionFailure.InvalidArgument 

                    If ReportErrors Then 
                        Dim ErrorMessage As String = "" 
                        Dim Errors As New List(Of String)
 
                        Dim Result As Boolean = _
                            CanPassToParameter( _
                                ResolutionResult, _
                                LastArgument, _ 
                                LastParameter, _
                                False, _ 
                                False, _ 
                                Errors, _
                                Nothing, _ 
                                Nothing)

                        Debug.Assert(Result = False AndAlso Errors.Count > 0, "expected this candidate to fail")
 
                        For Each ErrorString As String In Errors
                            ErrorMessage &= vbCrLf & "    " & ErrorString 
                        Next 

                        ErrorMessage = GetResourceString(ResID.MatchArgumentFailure2, ResolutionResult.ToString, ErrorMessage) 
                        'The selected member can't handle the type of the Value argument, so this is an argument exception.


                        Throw New InvalidCastException(ErrorMessage) 
                    End If
 
                    Return Nothing 
                End If
            End If 

            Return ResolutionResult
        End Function
 
        Friend Shared Function GetCallTarget(ByVal TargetProcedure As Method, ByVal Flags As BindingFlags) As MethodBase
            If TargetProcedure.IsMethod Then Return TargetProcedure.AsMethod 
            If TargetProcedure.IsProperty Then Return MatchesPropertyRequirements(TargetProcedure, Flags) 
            Debug.Fail("not a method or property??")
            Return Nothing 
        End Function

        Friend Shared Function ConstructCallArguments( _
            ByVal TargetProcedure As Method, _ 
            ByVal Arguments As Object(), _
            ByVal LookupFlags As BindingFlags) As Object() 
 
            Debug.Assert(TargetProcedure IsNot Nothing AndAlso Arguments IsNot Nothing, "expected arguments")
 

            Dim Parameters As ParameterInfo() = GetCallTarget(TargetProcedure, LookupFlags).GetParameters
            Dim CallArguments As Object() = New Object(Parameters.Length - 1) {}
 
            Dim SavedArguments As Object()
            Dim ArgumentCount As Integer = Arguments.Length 
            Dim LastArgument As Object = Nothing 

            If HasFlag(LookupFlags, BindingFlags.SetProperty) Then 
                Debug.Assert(Arguments.Length > 0, "must have an argument for property set Value")
                SavedArguments = Arguments
                Arguments = New Object(ArgumentCount - 2) {}
                System.Array.Copy(SavedArguments, Arguments, Arguments.Length) 
                LastArgument = SavedArguments(ArgumentCount - 1)
            End If 
 
            MatchArguments(TargetProcedure, Arguments, CallArguments)
 
            If HasFlag(LookupFlags, BindingFlags.SetProperty) Then
                'Need to match the Value argument for the property set call.
                Debug.Assert(GetCallTarget(TargetProcedure, LookupFlags).Name.StartsWith("set_"), "expected set accessor")
 
                Dim LastParameter As ParameterInfo = Parameters(Parameters.Length - 1)
                CallArguments(Parameters.Length - 1) = _ 
                    PassToParameter(LastArgument, LastParameter, LastParameter.ParameterType) 
            End If
 
            Return CallArguments
        End Function

    End Class 
End Namespace

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
