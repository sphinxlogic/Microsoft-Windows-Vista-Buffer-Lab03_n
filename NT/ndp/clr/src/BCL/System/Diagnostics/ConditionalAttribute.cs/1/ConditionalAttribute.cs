// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
using System;
 
namespace System.Diagnostics { 
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple=true), Serializable]
[System.Runtime.InteropServices.ComVisible(true)] 
    public sealed class ConditionalAttribute : Attribute
    {
 	public ConditionalAttribute(String conditionString)
	{ 
		m_conditionString = conditionString;
	} 
 
 	public String ConditionString {
		get { 
 			return m_conditionString;
 		}
	}
 
 	private String m_conditionString;
    } 
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
using System;
 
namespace System.Diagnostics { 
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple=true), Serializable]
[System.Runtime.InteropServices.ComVisible(true)] 
    public sealed class ConditionalAttribute : Attribute
    {
 	public ConditionalAttribute(String conditionString)
	{ 
		m_conditionString = conditionString;
	} 
 
 	public String ConditionString {
		get { 
 			return m_conditionString;
 		}
	}
 
 	private String m_conditionString;
    } 
} 
