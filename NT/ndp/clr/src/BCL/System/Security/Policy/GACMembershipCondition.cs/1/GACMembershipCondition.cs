// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
//  GacMembershipCondition.cs 
//
//  Implementation of membership condition for being in the Gac 
//

namespace System.Security.Policy {
    using System; 
    using System.Collections;
    using System.Globalization; 
 
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)] 
    sealed public class GacMembershipCondition : IMembershipCondition, IConstantMembershipCondition
    {
        //------------------------------------------------------
        // 
        // PUBLIC CONSTRUCTORS
        // 
        //----------------------------------------------------- 

        public GacMembershipCondition() 
        {
        }

        //----------------------------------------------------- 
        //
        // IMEMBERSHIPCONDITION IMPLEMENTATION 
        // 
        //-----------------------------------------------------
 
        public bool Check( Evidence evidence )
        {
            if (evidence == null)
                return false; 

            IEnumerator enumerator = evidence.GetHostEnumerator(); 
            while (enumerator.MoveNext()) 
            {
                Object obj = enumerator.Current; 
                if (obj is GacInstalled)
                    return true;
            }
            return false; 
        }
 
        public IMembershipCondition Copy() 
        {
            return new GacMembershipCondition(); 
        }

        public SecurityElement ToXml()
        { 
            return ToXml( null );
        } 
 
        public void FromXml( SecurityElement e )
        { 
            FromXml( e, null );
        }

        public SecurityElement ToXml( PolicyLevel level ) 
        {
            SecurityElement root = new SecurityElement( "IMembershipCondition" ); 
            System.Security.Util.XMLUtil.AddClassAttribute( root, this.GetType(), this.GetType().FullName ); 
            root.AddAttribute( "version", "1" );
            return root; 
        }

        public void FromXml( SecurityElement e, PolicyLevel level )
        { 
            if (e == null)
                throw new ArgumentNullException("e"); 
 
            if (!e.Tag.Equals( "IMembershipCondition" ))
                throw new ArgumentException( Environment.GetResourceString( "Argument_MembershipConditionElement" ) ); 
        }

        public override bool Equals( Object o )
        { 
            GacMembershipCondition that = (o as GacMembershipCondition);
            if (that != null) 
                return true; 
            return false;
        } 

        public override int GetHashCode()
        {
            return 0; 
        }
 
        public override String ToString() 
        {
            return Environment.GetResourceString( "GAC_ToString" ); 
        }
    }
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 

// 
//  GacMembershipCondition.cs 
//
//  Implementation of membership condition for being in the Gac 
//

namespace System.Security.Policy {
    using System; 
    using System.Collections;
    using System.Globalization; 
 
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)] 
    sealed public class GacMembershipCondition : IMembershipCondition, IConstantMembershipCondition
    {
        //------------------------------------------------------
        // 
        // PUBLIC CONSTRUCTORS
        // 
        //----------------------------------------------------- 

        public GacMembershipCondition() 
        {
        }

        //----------------------------------------------------- 
        //
        // IMEMBERSHIPCONDITION IMPLEMENTATION 
        // 
        //-----------------------------------------------------
 
        public bool Check( Evidence evidence )
        {
            if (evidence == null)
                return false; 

            IEnumerator enumerator = evidence.GetHostEnumerator(); 
            while (enumerator.MoveNext()) 
            {
                Object obj = enumerator.Current; 
                if (obj is GacInstalled)
                    return true;
            }
            return false; 
        }
 
        public IMembershipCondition Copy() 
        {
            return new GacMembershipCondition(); 
        }

        public SecurityElement ToXml()
        { 
            return ToXml( null );
        } 
 
        public void FromXml( SecurityElement e )
        { 
            FromXml( e, null );
        }

        public SecurityElement ToXml( PolicyLevel level ) 
        {
            SecurityElement root = new SecurityElement( "IMembershipCondition" ); 
            System.Security.Util.XMLUtil.AddClassAttribute( root, this.GetType(), this.GetType().FullName ); 
            root.AddAttribute( "version", "1" );
            return root; 
        }

        public void FromXml( SecurityElement e, PolicyLevel level )
        { 
            if (e == null)
                throw new ArgumentNullException("e"); 
 
            if (!e.Tag.Equals( "IMembershipCondition" ))
                throw new ArgumentException( Environment.GetResourceString( "Argument_MembershipConditionElement" ) ); 
        }

        public override bool Equals( Object o )
        { 
            GacMembershipCondition that = (o as GacMembershipCondition);
            if (that != null) 
                return true; 
            return false;
        } 

        public override int GetHashCode()
        {
            return 0; 
        }
 
        public override String ToString() 
        {
            return Environment.GetResourceString( "GAC_ToString" ); 
        }
    }
}
