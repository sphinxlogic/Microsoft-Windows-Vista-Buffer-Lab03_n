 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2001' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Design {
 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Data;
 	using System.Design; 
	using System.Diagnostics; 
	using System.Drawing;
    using System.Runtime.InteropServices; 
    using System.Runtime.Serialization.Formatters;
    using System.Windows.Forms;
    using System.Windows.Forms.Design;
    using System.Globalization; 

    /// <summary> 
    /// </summary> 
    /// <internalonly/>
    internal sealed class DesignUtil { 

        /// <summary>
        /// Private contstructor to avoid class being instantiated.
        /// </summary> 
        private DesignUtil() {
        } 
 
        internal static IDictionary CloneDictionary( IDictionary source ) {
            Debug.Assert( source != null ); 
            if( source == null ) {
                return null;
            }
 
            if( source is ICloneable ) {
                return (IDictionary) ((ICloneable) source).Clone(); 
            } 

            IDictionary clone = (IDictionary) Activator.CreateInstance( source.GetType() ); 

            IDictionaryEnumerator e = source.GetEnumerator();
            while( e.MoveNext() ) {
                ICloneable key = e.Key as ICloneable; 
                ICloneable val = e.Value as ICloneable;
 
                if( (key != null) && (val != null) ) { 
                    clone.Add( key.Clone(), val.Clone() );
                } 
            }

            return clone;
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2001' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Design {
 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Data;
 	using System.Design; 
	using System.Diagnostics; 
	using System.Drawing;
    using System.Runtime.InteropServices; 
    using System.Runtime.Serialization.Formatters;
    using System.Windows.Forms;
    using System.Windows.Forms.Design;
    using System.Globalization; 

    /// <summary> 
    /// </summary> 
    /// <internalonly/>
    internal sealed class DesignUtil { 

        /// <summary>
        /// Private contstructor to avoid class being instantiated.
        /// </summary> 
        private DesignUtil() {
        } 
 
        internal static IDictionary CloneDictionary( IDictionary source ) {
            Debug.Assert( source != null ); 
            if( source == null ) {
                return null;
            }
 
            if( source is ICloneable ) {
                return (IDictionary) ((ICloneable) source).Clone(); 
            } 

            IDictionary clone = (IDictionary) Activator.CreateInstance( source.GetType() ); 

            IDictionaryEnumerator e = source.GetEnumerator();
            while( e.MoveNext() ) {
                ICloneable key = e.Key as ICloneable; 
                ICloneable val = e.Value as ICloneable;
 
                if( (key != null) && (val != null) ) { 
                    clone.Add( key.Clone(), val.Clone() );
                } 
            }

            return clone;
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
