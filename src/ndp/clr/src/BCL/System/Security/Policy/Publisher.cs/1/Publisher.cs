// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
//  Publisher.cs 
//
//  Publisher is an IIdentity representing internet sites. 
//

namespace System.Security.Policy {
    using System.Runtime.Remoting; 
    using System;
    using System.IO; 
    using System.Security.Util; 
    using System.Collections;
    using PublisherIdentityPermission = System.Security.Permissions.PublisherIdentityPermission; 
    using System.Security.Cryptography.X509Certificates;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)] 
    sealed public class Publisher : IIdentityPermissionFactory, IBuiltInEvidence
    { 
        private X509Certificate m_cert; 

        internal Publisher() { } 

        public Publisher(X509Certificate cert)
        {
            if (cert == null) 
                throw new ArgumentNullException("cert");
 
            m_cert = cert; 
        }
 
        public IPermission CreateIdentityPermission( Evidence evidence )
        {
            return new PublisherIdentityPermission( m_cert );
        } 

        // Two Publisher objects are equal if the public keys contained within their certificates 
        // are equal.  The certs themselves may be different... 

        public override bool Equals(Object o) 
        {
            Publisher that = (o as Publisher);

            return (that != null && PublicKeyEquals( this.m_cert, that.m_cert )); 
        }
 
        // Checks if two certificates have the same public key, keyalg, and keyparam. 
        internal static bool PublicKeyEquals( X509Certificate cert1, X509Certificate cert2 )
        { 
            if (cert1 == null)
            {
                return (cert2 == null);
            } 
            else if (cert2 == null)
            { 
                return false; 
            }
 
            byte[] publicKey1 = cert1.GetPublicKey();
            String keyAlg1 = cert1.GetKeyAlgorithm();
            byte[] keyAlgParam1 = cert1.GetKeyAlgorithmParameters();
            byte[] publicKey2 = cert2.GetPublicKey(); 
            String keyAlg2 = cert2.GetKeyAlgorithm();
            byte[] keyAlgParam2 = cert2.GetKeyAlgorithmParameters(); 
 
            // Keys are most likely to be different of the three components,
            // so check them first 

            int len = publicKey1.Length;
            if (len != publicKey2.Length) return(false);
            for (int i = 0; i < len; i++) { 
                if (publicKey1[i] != publicKey2[i]) return(false);
            } 
            if (!(keyAlg1.Equals(keyAlg2))) return(false); 
            len = keyAlgParam1.Length;
            if (keyAlgParam2.Length != len) return(false); 
            for (int i = 0; i < len; i++) {
                if (keyAlgParam1[i] != keyAlgParam2[i]) return(false);
            }
 
            return true;
        } 
 
        public override int GetHashCode()
        { 
            return m_cert.GetHashCode();
        }

        public X509Certificate Certificate 
        {
            get 
            { 
                if (m_cert == null)
                    return null; 
                else
                    return new X509Certificate( m_cert );
            }
        } 

        public Object Copy() 
        { 
            Publisher p = new Publisher();
 
            if (m_cert != null)
                p.m_cert = new X509Certificate(m_cert);

            return p; 
        }
 
        internal SecurityElement ToXml() 
        {
            SecurityElement elem = new SecurityElement( "System.Security.Policy.Publisher" ); 
            // If you hit this assert then most likely you are trying to change the name of this class.
            // This is ok as long as you change the hard coded string above and change the assert below.
            BCLDebug.Assert( this.GetType().FullName.Equals( "System.Security.Policy.Publisher" ), "Class name changed!" );
 
            elem.AddAttribute( "version", "1" );
            elem.AddChild( new SecurityElement( "X509v3Certificate", m_cert != null ? m_cert.GetRawCertDataString() : "" ) ); 
            return elem; 
        }
 
        /// <internalonly/>
        int IBuiltInEvidence.OutputToBuffer( char[] buffer, int position, bool verbose )
        {
            buffer[position++] = BuiltInEvidenceHelper.idPublisher; 
            byte[] certData = this.Certificate.GetRawCertData();
            int length = certData.Length; 
 
            if (verbose)
            { 
                BuiltInEvidenceHelper.CopyIntToCharArray(length, buffer, position);
                position += 2;
            }
 
            Buffer.InternalBlockCopy(certData, 0, buffer, position * 2, length);
            return ((length - 1) / 2) + 1 + position; 
        } 

        /// <internalonly/> 
        int IBuiltInEvidence.GetRequiredSize(bool verbose)
        {
            // We need to return the size of the byte array converted to chars
            // (which can be one byte larger than is actually needed) plus 
            // one char for the identifier.
 
            // Note: the formula (NumBytes - 1) / 2 + 1 will always calculate 
            // the proper number of chars needed.
 
            int length = ((this.Certificate.GetRawCertData().Length - 1) / 2) + 1;
            if (verbose)
                return length + 3;
            else 
                return length + 1;
        } 
 
        /// <internalonly/>
        int IBuiltInEvidence.InitFromBuffer( char[] buffer, int position ) 
        {
            int length = BuiltInEvidenceHelper.GetIntFromCharArray(buffer, position);
            position += 2;
            byte[] certData = new byte[length]; 

            int lengthInChars = ((length - 1) / 2) + 1; 
            Buffer.InternalBlockCopy(buffer, position * 2, certData, 0, length); 
            m_cert = new X509Certificate( certData );
 
            return position + lengthInChars;
        }

        public override String ToString() 
        {
            return ToXml().ToString(); 
        } 

        // INormalizeForIsolatedStorage is not implemented for startup perf 
        // equivalent to INormalizeForIsolatedStorage.Normalize()
        internal Object Normalize()
        {
            MemoryStream ms = new MemoryStream(m_cert.GetRawCertData()); 
            ms.Position = 0;
            return ms; 
        } 
    }
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
//  Publisher.cs 
//
//  Publisher is an IIdentity representing internet sites. 
//

namespace System.Security.Policy {
    using System.Runtime.Remoting; 
    using System;
    using System.IO; 
    using System.Security.Util; 
    using System.Collections;
    using PublisherIdentityPermission = System.Security.Permissions.PublisherIdentityPermission; 
    using System.Security.Cryptography.X509Certificates;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)] 
    sealed public class Publisher : IIdentityPermissionFactory, IBuiltInEvidence
    { 
        private X509Certificate m_cert; 

        internal Publisher() { } 

        public Publisher(X509Certificate cert)
        {
            if (cert == null) 
                throw new ArgumentNullException("cert");
 
            m_cert = cert; 
        }
 
        public IPermission CreateIdentityPermission( Evidence evidence )
        {
            return new PublisherIdentityPermission( m_cert );
        } 

        // Two Publisher objects are equal if the public keys contained within their certificates 
        // are equal.  The certs themselves may be different... 

        public override bool Equals(Object o) 
        {
            Publisher that = (o as Publisher);

            return (that != null && PublicKeyEquals( this.m_cert, that.m_cert )); 
        }
 
        // Checks if two certificates have the same public key, keyalg, and keyparam. 
        internal static bool PublicKeyEquals( X509Certificate cert1, X509Certificate cert2 )
        { 
            if (cert1 == null)
            {
                return (cert2 == null);
            } 
            else if (cert2 == null)
            { 
                return false; 
            }
 
            byte[] publicKey1 = cert1.GetPublicKey();
            String keyAlg1 = cert1.GetKeyAlgorithm();
            byte[] keyAlgParam1 = cert1.GetKeyAlgorithmParameters();
            byte[] publicKey2 = cert2.GetPublicKey(); 
            String keyAlg2 = cert2.GetKeyAlgorithm();
            byte[] keyAlgParam2 = cert2.GetKeyAlgorithmParameters(); 
 
            // Keys are most likely to be different of the three components,
            // so check them first 

            int len = publicKey1.Length;
            if (len != publicKey2.Length) return(false);
            for (int i = 0; i < len; i++) { 
                if (publicKey1[i] != publicKey2[i]) return(false);
            } 
            if (!(keyAlg1.Equals(keyAlg2))) return(false); 
            len = keyAlgParam1.Length;
            if (keyAlgParam2.Length != len) return(false); 
            for (int i = 0; i < len; i++) {
                if (keyAlgParam1[i] != keyAlgParam2[i]) return(false);
            }
 
            return true;
        } 
 
        public override int GetHashCode()
        { 
            return m_cert.GetHashCode();
        }

        public X509Certificate Certificate 
        {
            get 
            { 
                if (m_cert == null)
                    return null; 
                else
                    return new X509Certificate( m_cert );
            }
        } 

        public Object Copy() 
        { 
            Publisher p = new Publisher();
 
            if (m_cert != null)
                p.m_cert = new X509Certificate(m_cert);

            return p; 
        }
 
        internal SecurityElement ToXml() 
        {
            SecurityElement elem = new SecurityElement( "System.Security.Policy.Publisher" ); 
            // If you hit this assert then most likely you are trying to change the name of this class.
            // This is ok as long as you change the hard coded string above and change the assert below.
            BCLDebug.Assert( this.GetType().FullName.Equals( "System.Security.Policy.Publisher" ), "Class name changed!" );
 
            elem.AddAttribute( "version", "1" );
            elem.AddChild( new SecurityElement( "X509v3Certificate", m_cert != null ? m_cert.GetRawCertDataString() : "" ) ); 
            return elem; 
        }
 
        /// <internalonly/>
        int IBuiltInEvidence.OutputToBuffer( char[] buffer, int position, bool verbose )
        {
            buffer[position++] = BuiltInEvidenceHelper.idPublisher; 
            byte[] certData = this.Certificate.GetRawCertData();
            int length = certData.Length; 
 
            if (verbose)
            { 
                BuiltInEvidenceHelper.CopyIntToCharArray(length, buffer, position);
                position += 2;
            }
 
            Buffer.InternalBlockCopy(certData, 0, buffer, position * 2, length);
            return ((length - 1) / 2) + 1 + position; 
        } 

        /// <internalonly/> 
        int IBuiltInEvidence.GetRequiredSize(bool verbose)
        {
            // We need to return the size of the byte array converted to chars
            // (which can be one byte larger than is actually needed) plus 
            // one char for the identifier.
 
            // Note: the formula (NumBytes - 1) / 2 + 1 will always calculate 
            // the proper number of chars needed.
 
            int length = ((this.Certificate.GetRawCertData().Length - 1) / 2) + 1;
            if (verbose)
                return length + 3;
            else 
                return length + 1;
        } 
 
        /// <internalonly/>
        int IBuiltInEvidence.InitFromBuffer( char[] buffer, int position ) 
        {
            int length = BuiltInEvidenceHelper.GetIntFromCharArray(buffer, position);
            position += 2;
            byte[] certData = new byte[length]; 

            int lengthInChars = ((length - 1) / 2) + 1; 
            Buffer.InternalBlockCopy(buffer, position * 2, certData, 0, length); 
            m_cert = new X509Certificate( certData );
 
            return position + lengthInChars;
        }

        public override String ToString() 
        {
            return ToXml().ToString(); 
        } 

        // INormalizeForIsolatedStorage is not implemented for startup perf 
        // equivalent to INormalizeForIsolatedStorage.Normalize()
        internal Object Normalize()
        {
            MemoryStream ms = new MemoryStream(m_cert.GetRawCertData()); 
            ms.Position = 0;
            return ms; 
        } 
    }
} 
