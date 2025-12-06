//------------------------------------------------------------------------------ 
// <copyright file="EmptyEnumerator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
using System; 
using System.Collections;
 
namespace System.Xml {

    internal sealed class EmptyEnumerator : IEnumerator {
 
        bool IEnumerator.MoveNext() {
            return false; 
        } 

        void IEnumerator.Reset() { 
        }

        object IEnumerator.Current {
            get { 
                throw new InvalidOperationException( Res.GetString( Res.Xml_InvalidOperation ) );
            } 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="EmptyEnumerator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
using System; 
using System.Collections;
 
namespace System.Xml {

    internal sealed class EmptyEnumerator : IEnumerator {
 
        bool IEnumerator.MoveNext() {
            return false; 
        } 

        void IEnumerator.Reset() { 
        }

        object IEnumerator.Current {
            get { 
                throw new InvalidOperationException( Res.GetString( Res.Xml_InvalidOperation ) );
            } 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
