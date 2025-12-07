// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// GacInstalled is an IIdentity representing whether or not an assembly is installed in the Gac 
//
 
namespace System.Security.Policy {
    using System.Runtime.Remoting;
    using System;
    using System.Security; 
    using System.Security.Util;
    using System.IO; 
    using System.Collections; 
    using GacIdentityPermission = System.Security.Permissions.GacIdentityPermission;
    using System.Runtime.CompilerServices; 

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    sealed public class GacInstalled : IIdentityPermissionFactory, IBuiltInEvidence 
    {
        public GacInstalled() 
        { 
        }
 
        public IPermission CreateIdentityPermission( Evidence evidence )
        {
            return new GacIdentityPermission();
        } 

        public override bool Equals(Object o) 
        { 
            if (o is GacInstalled)
                return true; 
            return false;
        }

        public override int GetHashCode() 
        {
            return 0; 
        } 

        public Object Copy() 
        {
            return new GacInstalled();
        }
 
        internal SecurityElement ToXml()
        { 
            SecurityElement elem = new SecurityElement( this.GetType().FullName ); 
            elem.AddAttribute( "version", "1" );
            return elem; 
        }

        /// <internalonly/>
        int IBuiltInEvidence.OutputToBuffer( char[] buffer, int position, bool verbose ) 
        {
            buffer[position] = BuiltInEvidenceHelper.idGac; 
            return position + 1; 
        }
 
        /// <internalonly/>
        int IBuiltInEvidence.GetRequiredSize(bool verbose)
        {
            return 1; 
        }
 
        /// <internalonly/> 
        int IBuiltInEvidence.InitFromBuffer( char[] buffer, int position )
        { 
            return position;
        }

        public override String ToString() 
        {
            return ToXml().ToString(); 
        } 
    }
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
// GacInstalled is an IIdentity representing whether or not an assembly is installed in the Gac 
//
 
namespace System.Security.Policy {
    using System.Runtime.Remoting;
    using System;
    using System.Security; 
    using System.Security.Util;
    using System.IO; 
    using System.Collections; 
    using GacIdentityPermission = System.Security.Permissions.GacIdentityPermission;
    using System.Runtime.CompilerServices; 

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    sealed public class GacInstalled : IIdentityPermissionFactory, IBuiltInEvidence 
    {
        public GacInstalled() 
        { 
        }
 
        public IPermission CreateIdentityPermission( Evidence evidence )
        {
            return new GacIdentityPermission();
        } 

        public override bool Equals(Object o) 
        { 
            if (o is GacInstalled)
                return true; 
            return false;
        }

        public override int GetHashCode() 
        {
            return 0; 
        } 

        public Object Copy() 
        {
            return new GacInstalled();
        }
 
        internal SecurityElement ToXml()
        { 
            SecurityElement elem = new SecurityElement( this.GetType().FullName ); 
            elem.AddAttribute( "version", "1" );
            return elem; 
        }

        /// <internalonly/>
        int IBuiltInEvidence.OutputToBuffer( char[] buffer, int position, bool verbose ) 
        {
            buffer[position] = BuiltInEvidenceHelper.idGac; 
            return position + 1; 
        }
 
        /// <internalonly/>
        int IBuiltInEvidence.GetRequiredSize(bool verbose)
        {
            return 1; 
        }
 
        /// <internalonly/> 
        int IBuiltInEvidence.InitFromBuffer( char[] buffer, int position )
        { 
            return position;
        }

        public override String ToString() 
        {
            return ToXml().ToString(); 
        } 
    }
} 
