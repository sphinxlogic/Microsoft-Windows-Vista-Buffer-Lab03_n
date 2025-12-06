//------------------------------------------------------------------------------ 
// <copyright file="HttpResponseHeader.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * Single http header representation 
 *
 * Copyright (c) 1998 Microsoft Corporation 
 */

namespace System.Web {
 
    using System.Collections;
 
    /* 
     * Response header (either known or unknown)
     */ 
    internal class HttpResponseHeader {
        private String _unknownHeader;
        private int _knownHeaderIndex;
        private String _value; 
        private static readonly char [] s_BadChars = new char[] {'\n', '\r', '\0'};
 
        internal HttpResponseHeader(int knownHeaderIndex, String value) { 
            _unknownHeader = null;
            _knownHeaderIndex = knownHeaderIndex; 

            // encode header value if
            if(HttpRuntime.EnableHeaderChecking) {
                _value = MaybeEncodeHeader(value); 
            }
            else { 
                _value = value; 
            }
        } 

        internal HttpResponseHeader(String unknownHeader, String value) {
            if(HttpRuntime.EnableHeaderChecking) {
                _unknownHeader = MaybeEncodeHeader(unknownHeader); 
                _knownHeaderIndex = HttpWorkerRequest.GetKnownResponseHeaderIndex(_unknownHeader);
                _value = MaybeEncodeHeader(value); 
            } 
            else {
                _unknownHeader = unknownHeader; 
                _knownHeaderIndex = HttpWorkerRequest.GetKnownResponseHeaderIndex(_unknownHeader);
                _value = value;
            }
        } 

        internal virtual String Name { 
            get { 
                if (_unknownHeader != null)
                    return _unknownHeader; 
                else
                    return HttpWorkerRequest.GetKnownResponseHeaderName(_knownHeaderIndex);
            }
        } 

        internal String Value { 
            get { return _value;} 
        }
 
        internal void Send(HttpWorkerRequest wr) {
            if (_knownHeaderIndex >= 0)
                wr.SendKnownResponseHeader(_knownHeaderIndex, _value);
            else 
                wr.SendUnknownResponseHeader(_unknownHeader, _value);
        } 
 
        // Encode the header if it contains a CRLF pair
        // VSWhidbey 257154 
        internal static string MaybeEncodeHeader(string value) {
            string sanitizedHeader = value;
            if (value.IndexOfAny(s_BadChars) >= 0) {
                // if we found a CRLF pair or NULL in the header, replace it 
                // this is slow but isn't expected to occur often
                // review: will any clients try to decode this? 
                // should it just be a space? 
                sanitizedHeader = value.Replace("\n", "%0a");
                sanitizedHeader = sanitizedHeader.Replace("\r", "%0d"); 
                sanitizedHeader = sanitizedHeader.Replace("\0", "%00");
            }

            return sanitizedHeader; 
        }
    } 
} 
//------------------------------------------------------------------------------ 
// <copyright file="HttpResponseHeader.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * Single http header representation 
 *
 * Copyright (c) 1998 Microsoft Corporation 
 */

namespace System.Web {
 
    using System.Collections;
 
    /* 
     * Response header (either known or unknown)
     */ 
    internal class HttpResponseHeader {
        private String _unknownHeader;
        private int _knownHeaderIndex;
        private String _value; 
        private static readonly char [] s_BadChars = new char[] {'\n', '\r', '\0'};
 
        internal HttpResponseHeader(int knownHeaderIndex, String value) { 
            _unknownHeader = null;
            _knownHeaderIndex = knownHeaderIndex; 

            // encode header value if
            if(HttpRuntime.EnableHeaderChecking) {
                _value = MaybeEncodeHeader(value); 
            }
            else { 
                _value = value; 
            }
        } 

        internal HttpResponseHeader(String unknownHeader, String value) {
            if(HttpRuntime.EnableHeaderChecking) {
                _unknownHeader = MaybeEncodeHeader(unknownHeader); 
                _knownHeaderIndex = HttpWorkerRequest.GetKnownResponseHeaderIndex(_unknownHeader);
                _value = MaybeEncodeHeader(value); 
            } 
            else {
                _unknownHeader = unknownHeader; 
                _knownHeaderIndex = HttpWorkerRequest.GetKnownResponseHeaderIndex(_unknownHeader);
                _value = value;
            }
        } 

        internal virtual String Name { 
            get { 
                if (_unknownHeader != null)
                    return _unknownHeader; 
                else
                    return HttpWorkerRequest.GetKnownResponseHeaderName(_knownHeaderIndex);
            }
        } 

        internal String Value { 
            get { return _value;} 
        }
 
        internal void Send(HttpWorkerRequest wr) {
            if (_knownHeaderIndex >= 0)
                wr.SendKnownResponseHeader(_knownHeaderIndex, _value);
            else 
                wr.SendUnknownResponseHeader(_unknownHeader, _value);
        } 
 
        // Encode the header if it contains a CRLF pair
        // VSWhidbey 257154 
        internal static string MaybeEncodeHeader(string value) {
            string sanitizedHeader = value;
            if (value.IndexOfAny(s_BadChars) >= 0) {
                // if we found a CRLF pair or NULL in the header, replace it 
                // this is slow but isn't expected to occur often
                // review: will any clients try to decode this? 
                // should it just be a space? 
                sanitizedHeader = value.Replace("\n", "%0a");
                sanitizedHeader = sanitizedHeader.Replace("\r", "%0d"); 
                sanitizedHeader = sanitizedHeader.Replace("\0", "%00");
            }

            return sanitizedHeader; 
        }
    } 
} 
