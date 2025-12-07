//------------------------------------------------------------------------------ 
// <copyright file="SecureStringHasher.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
using System; 
using System.Collections.Generic;
 
namespace System.Xml {

    // SecureStringHasher is a hash code provider for strings. The hash codes calculation starts with a seed (hasCodeRandomizer) which is usually
    // different for each instance of SecureStringHasher. Since the hash code depend on the seed, the chance of hashtable DoS attack in case when 
    // someone passes in lots of strings that hash to the same hash code is greatly reduced.
    // The SecureStringHasher implements IEqualityComparer for strings and therefore can be used in generic IDictionary. 
    internal class SecureStringHasher : IEqualityComparer<String> { 
        int hashCodeRandomizer;
 
        public SecureStringHasher() {
            this.hashCodeRandomizer = Environment.TickCount;
        }
 
        public SecureStringHasher( int hashCodeRandomizer ) {
            this.hashCodeRandomizer = hashCodeRandomizer; 
        } 

 
        public int Compare( String x, String y ) {
            return String.Compare(x, y, StringComparison.Ordinal);
        }
 
        public bool Equals( String x, String y ) {
            return String.Equals( x, y, StringComparison.Ordinal ); 
        } 

        public int GetHashCode( String key ) { 
            int hashCode = hashCodeRandomizer;
            // use key.Length to eliminate the rangecheck
            for ( int i = 0; i < key.Length; i++ ) {
                hashCode += ( hashCode << 7 ) ^ key[i]; 
            }
            // mix it a bit more 
            hashCode -= hashCode >> 17; 
            hashCode -= hashCode >> 11;
            hashCode -= hashCode >> 5; 
            return hashCode;
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="SecureStringHasher.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
using System; 
using System.Collections.Generic;
 
namespace System.Xml {

    // SecureStringHasher is a hash code provider for strings. The hash codes calculation starts with a seed (hasCodeRandomizer) which is usually
    // different for each instance of SecureStringHasher. Since the hash code depend on the seed, the chance of hashtable DoS attack in case when 
    // someone passes in lots of strings that hash to the same hash code is greatly reduced.
    // The SecureStringHasher implements IEqualityComparer for strings and therefore can be used in generic IDictionary. 
    internal class SecureStringHasher : IEqualityComparer<String> { 
        int hashCodeRandomizer;
 
        public SecureStringHasher() {
            this.hashCodeRandomizer = Environment.TickCount;
        }
 
        public SecureStringHasher( int hashCodeRandomizer ) {
            this.hashCodeRandomizer = hashCodeRandomizer; 
        } 

 
        public int Compare( String x, String y ) {
            return String.Compare(x, y, StringComparison.Ordinal);
        }
 
        public bool Equals( String x, String y ) {
            return String.Equals( x, y, StringComparison.Ordinal ); 
        } 

        public int GetHashCode( String key ) { 
            int hashCode = hashCodeRandomizer;
            // use key.Length to eliminate the rangecheck
            for ( int i = 0; i < key.Length; i++ ) {
                hashCode += ( hashCode << 7 ) ^ key[i]; 
            }
            // mix it a bit more 
            hashCode -= hashCode >> 17; 
            hashCode -= hashCode >> 11;
            hashCode -= hashCode >> 5; 
            return hashCode;
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
