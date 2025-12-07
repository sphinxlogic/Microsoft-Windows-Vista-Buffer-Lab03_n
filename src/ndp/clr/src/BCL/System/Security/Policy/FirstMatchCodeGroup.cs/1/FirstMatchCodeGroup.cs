// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
//  FirstMatchCodeGroup.cs
// 
//  Representation for code groups used for the policy mechanism 
//
 
namespace System.Security.Policy {

    using System;
    using System.Security.Util; 
    using System.Security;
    using System.Collections; 
 
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)] 
    sealed public class FirstMatchCodeGroup : CodeGroup
    {
        internal FirstMatchCodeGroup()
            : base() 
        {
        } 
 
        public FirstMatchCodeGroup( IMembershipCondition membershipCondition, PolicyStatement policy )
            : base( membershipCondition, policy ) 
        {
        }

 
        public override PolicyStatement Resolve( Evidence evidence )
        { 
            if (evidence == null) 
                throw new ArgumentNullException("evidence");
 
            if (this.MembershipCondition.Check( evidence ))
            {
                PolicyStatement childPolicy = null;
 
                IEnumerator enumerator = this.Children.GetEnumerator();
 
                while (enumerator.MoveNext()) 
                {
                    childPolicy = ((CodeGroup)enumerator.Current).Resolve( evidence ); 

                    // If the child has a policy, we are done.

                    if (childPolicy != null) 
                        break;
                } 
 
                PolicyStatement thisPolicy = this.PolicyStatement;
 
                if (thisPolicy == null)
                {
                    return childPolicy;
                } 
                else if (childPolicy != null)
                { 
                    // Combine the child and this policy and return it. 

                    PolicyStatement combined = new PolicyStatement(); 

                    combined.SetPermissionSetNoCopy( thisPolicy.GetPermissionSetNoCopy().Union( childPolicy.GetPermissionSetNoCopy() ) );

                    // if both this group and matching child group are exclusive we need to throw an exception 

                    if (((thisPolicy.Attributes & childPolicy.Attributes) & PolicyStatementAttribute.Exclusive) == PolicyStatementAttribute.Exclusive) 
                        throw new PolicyException( Environment.GetResourceString( "Policy_MultipleExclusive" ) ); 

                    combined.Attributes = thisPolicy.Attributes | childPolicy.Attributes; 

                    return combined;
                }
                else 
                {
                    // Otherwise we just copy the this policy. 
 
                    return this.PolicyStatement;
                } 
            }
            else
            {
                return null; 
            }
        } 
 
        public override CodeGroup ResolveMatchingCodeGroups( Evidence evidence )
        { 
            if (evidence == null)
                throw new ArgumentNullException("evidence");

            if (this.MembershipCondition.Check( evidence )) 
            {
                CodeGroup retGroup = this.Copy(); 
 
                retGroup.Children = new ArrayList();
 
                IEnumerator enumerator = this.Children.GetEnumerator();

                while (enumerator.MoveNext())
                { 
                    CodeGroup matchingGroups = ((CodeGroup)enumerator.Current).ResolveMatchingCodeGroups( evidence );
 
                    // If the child has a policy, we are done. 

                    if (matchingGroups != null) 
                    {
                        retGroup.AddChild( matchingGroups );
                        break;
                    } 
                }
 
                return retGroup; 

            } 
            else
            {
                return null;
            } 
        }
 
        public override CodeGroup Copy() 
        {
            FirstMatchCodeGroup group = new FirstMatchCodeGroup(); 

            group.MembershipCondition = this.MembershipCondition;
            group.PolicyStatement = this.PolicyStatement;
            group.Name = this.Name; 
            group.Description = this.Description;
 
            IEnumerator enumerator = this.Children.GetEnumerator(); 

            while (enumerator.MoveNext()) 
            {
                group.AddChild( (CodeGroup)enumerator.Current );
            }
 
            return group;
        } 
 

        public override String MergeLogic 
        {
            get
            {
                return Environment.GetResourceString( "MergeLogic_FirstMatch" ); 
            }
        } 
 	 
        internal override String GetTypeName()
        { 
            return "System.Security.Policy.FirstMatchCodeGroup";
        }
	
    } 

 
} 
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
//  FirstMatchCodeGroup.cs
// 
//  Representation for code groups used for the policy mechanism 
//
 
namespace System.Security.Policy {

    using System;
    using System.Security.Util; 
    using System.Security;
    using System.Collections; 
 
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)] 
    sealed public class FirstMatchCodeGroup : CodeGroup
    {
        internal FirstMatchCodeGroup()
            : base() 
        {
        } 
 
        public FirstMatchCodeGroup( IMembershipCondition membershipCondition, PolicyStatement policy )
            : base( membershipCondition, policy ) 
        {
        }

 
        public override PolicyStatement Resolve( Evidence evidence )
        { 
            if (evidence == null) 
                throw new ArgumentNullException("evidence");
 
            if (this.MembershipCondition.Check( evidence ))
            {
                PolicyStatement childPolicy = null;
 
                IEnumerator enumerator = this.Children.GetEnumerator();
 
                while (enumerator.MoveNext()) 
                {
                    childPolicy = ((CodeGroup)enumerator.Current).Resolve( evidence ); 

                    // If the child has a policy, we are done.

                    if (childPolicy != null) 
                        break;
                } 
 
                PolicyStatement thisPolicy = this.PolicyStatement;
 
                if (thisPolicy == null)
                {
                    return childPolicy;
                } 
                else if (childPolicy != null)
                { 
                    // Combine the child and this policy and return it. 

                    PolicyStatement combined = new PolicyStatement(); 

                    combined.SetPermissionSetNoCopy( thisPolicy.GetPermissionSetNoCopy().Union( childPolicy.GetPermissionSetNoCopy() ) );

                    // if both this group and matching child group are exclusive we need to throw an exception 

                    if (((thisPolicy.Attributes & childPolicy.Attributes) & PolicyStatementAttribute.Exclusive) == PolicyStatementAttribute.Exclusive) 
                        throw new PolicyException( Environment.GetResourceString( "Policy_MultipleExclusive" ) ); 

                    combined.Attributes = thisPolicy.Attributes | childPolicy.Attributes; 

                    return combined;
                }
                else 
                {
                    // Otherwise we just copy the this policy. 
 
                    return this.PolicyStatement;
                } 
            }
            else
            {
                return null; 
            }
        } 
 
        public override CodeGroup ResolveMatchingCodeGroups( Evidence evidence )
        { 
            if (evidence == null)
                throw new ArgumentNullException("evidence");

            if (this.MembershipCondition.Check( evidence )) 
            {
                CodeGroup retGroup = this.Copy(); 
 
                retGroup.Children = new ArrayList();
 
                IEnumerator enumerator = this.Children.GetEnumerator();

                while (enumerator.MoveNext())
                { 
                    CodeGroup matchingGroups = ((CodeGroup)enumerator.Current).ResolveMatchingCodeGroups( evidence );
 
                    // If the child has a policy, we are done. 

                    if (matchingGroups != null) 
                    {
                        retGroup.AddChild( matchingGroups );
                        break;
                    } 
                }
 
                return retGroup; 

            } 
            else
            {
                return null;
            } 
        }
 
        public override CodeGroup Copy() 
        {
            FirstMatchCodeGroup group = new FirstMatchCodeGroup(); 

            group.MembershipCondition = this.MembershipCondition;
            group.PolicyStatement = this.PolicyStatement;
            group.Name = this.Name; 
            group.Description = this.Description;
 
            IEnumerator enumerator = this.Children.GetEnumerator(); 

            while (enumerator.MoveNext()) 
            {
                group.AddChild( (CodeGroup)enumerator.Current );
            }
 
            return group;
        } 
 

        public override String MergeLogic 
        {
            get
            {
                return Environment.GetResourceString( "MergeLogic_FirstMatch" ); 
            }
        } 
 	 
        internal override String GetTypeName()
        { 
            return "System.Security.Policy.FirstMatchCodeGroup";
        }
	
    } 

 
} 
