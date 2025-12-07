//------------------------------------------------------------------------------ 
// <copyright file="XmlResolver.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Xml 
{
    using System; 
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Security; 
    using System.Security.Policy;
    using System.Security.Permissions; 
 
    /// <include file='doc\XmlResolver.uex' path='docs/doc[@for="XmlResolver"]/*' />
    /// <devdoc> 
    ///    <para>Resolves external XML resources named by a Uniform
    ///       Resource Identifier (URI). This class is <see langword='abstract'/>
    ///       .</para>
    /// </devdoc> 
    public abstract class XmlResolver {
        /// <include file='doc\XmlResolver.uex' path='docs/doc[@for="XmlResolver.GetEntity1"]/*' /> 
        /// <devdoc> 
        ///    <para>Maps a
        ///       URI to an Object containing the actual resource.</para> 
        /// </devdoc>

        public abstract Object GetEntity(Uri absoluteUri,
                                         string role, 
                                         Type ofObjectToReturn);
 
        /// <include file='doc\XmlResolver.uex' path='docs/doc[@for="XmlResolver.ResolveUri"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public virtual Uri ResolveUri(Uri baseUri, string relativeUri) {
            if ( baseUri == null || ( !baseUri.IsAbsoluteUri && baseUri.OriginalString.Length == 0 ) ) {
                Uri uri = new Uri( relativeUri, UriKind.RelativeOrAbsolute ); 
                if ( !uri.IsAbsoluteUri && uri.OriginalString.Length > 0 ) {
                    uri = new Uri( Path.GetFullPath( relativeUri ) ); 
                } 
                return uri;
            } 
            else {
                if ( relativeUri == null || relativeUri.Length == 0 ) {
                    return baseUri;
                } 
                return new Uri( baseUri, relativeUri );
            } 
        } 

        //UE attension 
        /// <include file='doc\XmlResolver.uex' path='docs/doc[@for="XmlResolver.Credentials"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public abstract ICredentials Credentials {
            set; 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="XmlResolver.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
//-----------------------------------------------------------------------------
 
namespace System.Xml 
{
    using System; 
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Security; 
    using System.Security.Policy;
    using System.Security.Permissions; 
 
    /// <include file='doc\XmlResolver.uex' path='docs/doc[@for="XmlResolver"]/*' />
    /// <devdoc> 
    ///    <para>Resolves external XML resources named by a Uniform
    ///       Resource Identifier (URI). This class is <see langword='abstract'/>
    ///       .</para>
    /// </devdoc> 
    public abstract class XmlResolver {
        /// <include file='doc\XmlResolver.uex' path='docs/doc[@for="XmlResolver.GetEntity1"]/*' /> 
        /// <devdoc> 
        ///    <para>Maps a
        ///       URI to an Object containing the actual resource.</para> 
        /// </devdoc>

        public abstract Object GetEntity(Uri absoluteUri,
                                         string role, 
                                         Type ofObjectToReturn);
 
        /// <include file='doc\XmlResolver.uex' path='docs/doc[@for="XmlResolver.ResolveUri"]/*' /> 
        /// <devdoc>
        ///    <para>[To be supplied.]</para> 
        /// </devdoc>
        public virtual Uri ResolveUri(Uri baseUri, string relativeUri) {
            if ( baseUri == null || ( !baseUri.IsAbsoluteUri && baseUri.OriginalString.Length == 0 ) ) {
                Uri uri = new Uri( relativeUri, UriKind.RelativeOrAbsolute ); 
                if ( !uri.IsAbsoluteUri && uri.OriginalString.Length > 0 ) {
                    uri = new Uri( Path.GetFullPath( relativeUri ) ); 
                } 
                return uri;
            } 
            else {
                if ( relativeUri == null || relativeUri.Length == 0 ) {
                    return baseUri;
                } 
                return new Uri( baseUri, relativeUri );
            } 
        } 

        //UE attension 
        /// <include file='doc\XmlResolver.uex' path='docs/doc[@for="XmlResolver.Credentials"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc> 
        public abstract ICredentials Credentials {
            set; 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
