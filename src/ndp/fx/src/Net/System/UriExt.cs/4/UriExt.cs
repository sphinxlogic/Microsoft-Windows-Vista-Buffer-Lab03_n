/*++ 
Copyright (c) 2003 Microsoft Corporation

Module Name:
 
    UriExt.cs
 
Abstract: 

    Uri extensibility model Implementation. 
    This file utilizes partial class feature.
    Uri.cs file contains core System.Uri functionality.

Author: 
    Alexei Vopilov    Nov 21 2003
 
Revision History: 

--*/ 

namespace System {
    using System.Collections.Generic;
    using System.Configuration; 
    using System.Globalization;
    using System.Net.Configuration; 
    using System.Security.Permissions; 
    using System.Text;
    using System.Runtime.InteropServices; 

    public partial class Uri {
        //
        // All public ctors go through here 
        //
        private void CreateThis(string uri, bool dontEscape, UriKind uriKind) 
        { 
            // if (!Enum.IsDefined(typeof(UriKind), uriKind)) -- We currently believe that Enum.IsDefined() is too slow to be used here.
            if ((int)uriKind < (int)UriKind.RelativeOrAbsolute || (int)uriKind > (int)UriKind.Relative) { 
                throw new ArgumentException(SR.GetString(SR.net_uri_InvalidUriKind, uriKind));
            }

            m_String = uri == null? string.Empty: uri; 

            if (dontEscape) 
                m_Flags |= Flags.UserEscaped; 

            ParsingError err = ParseScheme(m_String, ref m_Flags, ref m_Syntax); 
            UriFormatException e;

            InitializeUri(err, uriKind, out e);
            if (e != null) 
                throw e;
        } 
        // 
        private void InitializeUri(ParsingError err, UriKind uriKind, out UriFormatException e)
        { 
            if (err == ParsingError.None)
            {
                if (IsImplicitFile)
                { 
                    // V1 compat VsWhidbey#252282
                    // A relative Uri wins over implicit UNC path unless the UNC path is of the form "\\something" and uriKind != Absolute 
                    if ( 
#if !PLATFORM_UNIX
                        NotAny(Flags.DosPath) && 
#endif // !PLATFORM_UNIX
                        uriKind != UriKind.Absolute &&
                       (uriKind == UriKind.Relative || (m_String.Length >= 2 && (m_String[0] != '\\' || m_String[1] != '\\'))))
 
                    {
                        m_Syntax = null; //make it be relative Uri 
                        m_Flags &= Flags.UserEscaped; // the only flag that makes sense for a relative uri 
                        e = null;
                        return; 
                        // Otheriwse an absolute file Uri wins when it's of the form "\\something"
                    }
                    //
                    // VsWhidbey#423805 and V1 compat issue 
                    // We should support relative Uris of the form c:\bla or c:/bla
                    // 
#if !PLATFORM_UNIX 
                    else if (uriKind == UriKind.Relative && InFact(Flags.DosPath))
                    { 
                        m_Syntax = null; //make it be relative Uri
                        m_Flags &= Flags.UserEscaped; // the only flag that makes sense for a relative uri
                        e = null;
                        return; 
                        // Otheriwse an absolute file Uri wins when it's of the form "c:\something"
                    } 
#endif // !PLATFORM_UNIX 
                }
            } 
            else if (err > ParsingError.LastRelativeUriOkErrIndex)
            {
                //This is a fatal error based solely on scheme name parsing
                m_String = null; // make it be invalid Uri 
                e = GetException(err);
                return; 
            } 

            System.Net.GlobalLog.Assert(err == ParsingError.None || (object)m_Syntax == null, "Uri::.ctor|ParseScheme has found an error:{0}, but m_Syntax is not null.", err); 
            //
            //
            //
            bool hasUnicode = false; 

            // Is there unicode .. 
            if ((!s_ConfigInitialized) && CheckForConfigLoad(m_String)){ 
                InitializeUriConfig();
            } 

            m_iriParsing = (s_IriParsing && ((m_Syntax == null) || m_Syntax.InFact(UriSyntaxFlags.AllowIriParsing)));

            if (m_iriParsing && CheckForUnicode(m_String)){ 
                m_Flags |= Flags.HasUnicode;
                hasUnicode = true; 
                // switch internal strings 
                m_originalUnicodeString = m_String; // original string location changed
            } 

            if (m_Syntax != null)
            {
                if (m_Syntax.IsSimple) 
                {
                    if ((err = PrivateParseMinimal()) != ParsingError.None) 
                    { 
                        if (uriKind != UriKind.Absolute && err <= ParsingError.LastRelativeUriOkErrIndex)
                        { 
                            m_Syntax = null; // convert to relative uri
                            e = null;
                            m_Flags &= Flags.UserEscaped; // the only flag that makes sense for a relative uri
                        } 
                        else
                            e = GetException(err); 
                    } 
                    else if (uriKind == UriKind.Relative)
                    { 
                        // Here we know that we can create an absolute Uri, but the user has requested only a relative one
                        e = GetException(ParsingError.CannotCreateRelative);
                    }
                    else 
                        e = null;
                    // will return from here 
 
                    if (m_iriParsing && hasUnicode){
                        // In this scenario we need to parse the whole string 
                        EnsureParseRemaining();
                    }
                }
                else 
                {
                    // offer custom parser to create a parsing context 
                    m_Syntax = m_Syntax.InternalOnNewUri(); 

                    // incase they won't call us 
                    m_Flags |= Flags.UserDrivenParsing;

                    // Ask a registered type to validate this uri
                    m_Syntax.InternalValidate(this, out e); 

                    if (e != null) 
                    { 
                        // Can we still take it as a relative Uri?
                        if (uriKind != UriKind.Absolute && err != ParsingError.None && err <= ParsingError.LastRelativeUriOkErrIndex) 
                        {
                            m_Syntax = null; // convert it to relative
                            e = null;
                            m_Flags &= Flags.UserEscaped; // the only flag that makes sense for a relative uri 
                        }
                    } 
                    else // e == null 
                    {
                        if (err != ParsingError.None || InFact(Flags.ErrorOrParsingRecursion)) 
                        {
                            // User parser took over on an invalid Uri
                            SetUserDrivenParsing();
                        } 
                        else if (uriKind == UriKind.Relative)
                        { 
                            // Here we know that custom parser can create an absolute Uri, but the user has requested only a relative one 
                            e = GetException(ParsingError.CannotCreateRelative);
                        } 

                        if (m_iriParsing && hasUnicode){
                            // In this scenario we need to parse the whole string
                            EnsureParseRemaining(); 
                        }
 
                    } 
                    // will return from here
                } 
            }
            else if (err != ParsingError.None && uriKind != UriKind.Absolute && err <= ParsingError.LastRelativeUriOkErrIndex)
            {
                e = null; 
                m_Flags &= (Flags.UserEscaped|Flags.HasUnicode); // the only flags that makes sense for a relative uri
                if (m_iriParsing && hasUnicode){ 
                    // Iri'ze and then normalize relative uris 
                    m_String = EscapeUnescapeIri(m_originalUnicodeString, 0, m_originalUnicodeString.Length,
                                                (UriComponents)0); 
                    try{
                        m_String = m_String.Normalize(NormalizationForm.FormC);
                    }
                    catch (ArgumentException){ 
                        e = GetException(ParsingError.BadFormat);
                    } 
                } 
            }
            else 
            {
               m_String = null; // make it be invalid Uri
               e = GetException(err);
            } 
        }
 
 
        //
        // Checks if there are any unicode or escaped chars or ace to determine whether to load 
        // config
        //
        private unsafe bool CheckForConfigLoad(String data)
        { 
            bool initConfig = false;
            int length = data.Length; 
 
            fixed (char* temp = data){
                for (int i = 0; i < length; ++i){ 

                    if ((temp[i] > '\x7f') || (temp[i] == '%') ||
                        ((temp[i] == 'x') && ((i + 3) < length) && (temp[i + 1] == 'n') && (temp[i + 2] == '-') && (temp[i + 3] == '-')))
                    { 

                        // Unicode or maybe ace 
                        initConfig = true; 
                        break;
                    } 
                }


            } 

            return initConfig; 
        } 

        // 
        // Unescapes entire string and checks if it has unicode chars
        //
        private unsafe bool CheckForUnicode(String data)
        { 
            bool hasUnicode = false;
            char[] chars = new char[data.Length]; 
            int count = 0; 

            chars = UnescapeString(data, 0, data.Length, chars, ref count, c_DummyChar, 
                                   c_DummyChar, c_DummyChar, UnescapeMode.Unescape | UnescapeMode.UnescapeAll,
                                   null, false, false);

            String tempStr = new string(chars, 0, count); 
            int length = tempStr.Length;
 
            fixed (char* tempPtr = tempStr){ 
                for (int i = 0; i < length; ++i){
                    if (tempPtr[i] > '\x7f'){ 
                        // Unicode
                        hasUnicode = true;
                        break;
                    } 
                }
            } 
            return hasUnicode; 

        } 

        //
        // Check if the char (potentially surrogate) at given offset is in the iri range
        // Takes in isQuery because because iri restrictions for query are different 
        //
        internal static bool CheckIriUnicodeRange(string uri, int offset, ref bool surrogatePair, bool isQuery) 
        { 
            char invalidLowSurr = '\uFFFF';
            return CheckIriUnicodeRange(uri[offset],(offset + 1 < uri.Length) ? uri[offset + 1] : invalidLowSurr, 
                                        ref surrogatePair, isQuery);
        }

        // 
        // Checks if provided non surrogate char lies in iri range
        // 
        internal static bool CheckIriUnicodeRange(char unicode, bool isQuery) 
        {
            if ((unicode >= '\u00A0' && unicode <= '\uD7FF') || 
               (unicode >= '\uF900' && unicode <= '\uFDCF') ||
               (unicode >= '\uFDF0' && unicode <= '\uFFEF') ||
               (isQuery && unicode >= '\uE000' && unicode <= '\uF8FF')){
                return true; 
            }else{
                return false; 
            } 
        }
 
        //
        // Check if the highSurr is in the iri range or if highSurr and lowSurr are a surr pair then
        // it checks if the combined char is in the range
        // Takes in isQuery because because iri restrictions for query are different 
        //
        internal static bool CheckIriUnicodeRange(char highSurr, char lowSurr, ref bool surrogatePair, bool isQuery) 
        { 
            bool inRange = false;
            surrogatePair = false; 

            if (CheckIriUnicodeRange(highSurr, isQuery)){
                inRange = true;
            } 
            else if (Char.IsHighSurrogate(highSurr)){
                if (Char.IsSurrogatePair(highSurr, lowSurr)){ 
                    surrogatePair = true; 
                    char[] chars = new char[2] { highSurr, lowSurr };
                    string surrPair = new string(chars); 
                    if (((surrPair.CompareTo("\U00010000") >= 0) && (surrPair.CompareTo("\U0001FFFD") <= 0)) ||
                        ((surrPair.CompareTo("\U00020000") >= 0) && (surrPair.CompareTo("\U0002FFFD") <= 0)) ||
                        ((surrPair.CompareTo("\U00030000") >= 0) && (surrPair.CompareTo("\U0003FFFD") <= 0)) ||
                        ((surrPair.CompareTo("\U00040000") >= 0) && (surrPair.CompareTo("\U0004FFFD") <= 0)) || 
                        ((surrPair.CompareTo("\U00050000") >= 0) && (surrPair.CompareTo("\U0005FFFD") <= 0)) ||
                        ((surrPair.CompareTo("\U00060000") >= 0) && (surrPair.CompareTo("\U0006FFFD") <= 0)) || 
                        ((surrPair.CompareTo("\U00070000") >= 0) && (surrPair.CompareTo("\U0007FFFD") <= 0)) || 
                        ((surrPair.CompareTo("\U00080000") >= 0) && (surrPair.CompareTo("\U0008FFFD") <= 0)) ||
                        ((surrPair.CompareTo("\U00090000") >= 0) && (surrPair.CompareTo("\U0009FFFD") <= 0)) || 
                        ((surrPair.CompareTo("\U000A0000") >= 0) && (surrPair.CompareTo("\U000AFFFD") <= 0)) ||
                        ((surrPair.CompareTo("\U000B0000") >= 0) && (surrPair.CompareTo("\U000BFFFD") <= 0)) ||
                        ((surrPair.CompareTo("\U000C0000") >= 0) && (surrPair.CompareTo("\U000CFFFD") <= 0)) ||
                        ((surrPair.CompareTo("\U000D0000") >= 0) && (surrPair.CompareTo("\U000DFFFD") <= 0)) || 
                        ((surrPair.CompareTo("\U000E0000") >= 0) && (surrPair.CompareTo("\U000EFFFD") <= 0)) ||
                        (isQuery && (((surrPair.CompareTo("\U000F0000") >= 0) && (surrPair.CompareTo("\U000FFFFD") <= 0)) || 
                        ((surrPair.CompareTo("\U00100000") >= 0) && (surrPair.CompareTo("\U0010FFFD") <= 0))))) 
                        inRange = true;
                } 
            }

            return inRange;
        } 
        //
        // 
        //  Returns true if the string represents a valid argument to the Uri ctor 
        //  If uriKind != AbsoluteUri then certain parsing erros are ignored but Uri usage is limited
        // 
        public static bool TryCreate(string uriString, UriKind uriKind, out Uri result)
        {
            if ((object)uriString == null)
            { 
                result = null;
                return false; 
            } 
            UriFormatException e = null;
            result = CreateHelper(uriString, false, uriKind, ref e); 
            return (object) e == null && result != null;
        }
        //
        public static bool TryCreate(Uri baseUri, string relativeUri, out Uri result) 
        {
            Uri relativeLink; 
            if (TryCreate(relativeUri, UriKind.RelativeOrAbsolute, out relativeLink)) 
            {
                if (!relativeLink.IsAbsoluteUri) 
                    return TryCreate(baseUri, relativeLink, out result);

                result = relativeLink;
                return true; 
            }
            result = null; 
            return false; 
        }
        // 
        public static bool TryCreate(Uri baseUri, Uri relativeUri, out Uri result)
        {
            result = null;
 
            //Consider: Work out the baseUri==null case
            if ((object)baseUri == null) 
                return false; 

            if (baseUri.IsNotAbsoluteUri) 
                return false;

            UriFormatException e;
            string newUriString = null; 

            bool dontEscape; 
            if (baseUri.Syntax.IsSimple) 
            {
                dontEscape = relativeUri.UserEscaped; 
                result = ResolveHelper(baseUri, relativeUri, ref newUriString, ref dontEscape, out e);
            }
            else
            { 
                dontEscape = false;
                newUriString = baseUri.Syntax.InternalResolve(baseUri, relativeUri, out e); 
            } 

            if (e != null) 
                return false;

            if ((object) result == null)
                result = CreateHelper(newUriString, dontEscape, UriKind.Absolute, ref e); 

            return (object) e == null && result != null && result.IsAbsoluteUri; 
        } 
        //
        // 
        public bool IsBaseOf(Uri uri)
        {
            if (!IsAbsoluteUri)
                return false; 

            if (Syntax.IsSimple) 
                return IsBaseOfHelper(uri); 

            return Syntax.InternalIsBaseOf(this, uri); 
        }
        //
        //
        public string GetComponents(UriComponents components, UriFormat format) 
        {
            if (((components & UriComponents.SerializationInfoString) != 0) && components != UriComponents.SerializationInfoString) 
                throw new ArgumentOutOfRangeException("UriComponents.SerializationInfoString"); 

            if ((format & ~UriFormat.SafeUnescaped) != 0) 
                throw new ArgumentOutOfRangeException("format");

            if (IsNotAbsoluteUri)
            { 
                if (components == UriComponents.SerializationInfoString)
                    return GetRelativeSerializationString(format); 
                else 
                    throw new InvalidOperationException(SR.GetString(SR.net_uri_NotAbsolute));
            } 

            if (Syntax.IsSimple)
                return GetComponentsHelper(components, format);
 
            return Syntax.InternalGetComponents(this, components, format);
        } 
        // 
        public bool IsWellFormedOriginalString()
        { 
            if (IsNotAbsoluteUri || Syntax.IsSimple)
                return InternalIsWellFormedOriginalString();

            return Syntax.InternalIsWellFormedOriginalString(this); 
        }
        // Consider: (perf) Making it to not create a Uri internally 
        public static bool IsWellFormedUriString(string uriString, UriKind uriKind) 
        {
          Uri result; 

          if (!Uri.TryCreate(uriString, uriKind, out result))
              return false;
 
          return result.IsWellFormedOriginalString();
        } 
        // 
        //
        // This is for languages that do not support == != operators overloading 
        //
        // Note that Uri.Equals will get an optimized path but is limited to true/fasle result only
        //
        public static int Compare(Uri uri1, Uri uri2, UriComponents partsToCompare, UriFormat compareFormat, StringComparison comparisonType) 
        {
 
            if ((object) uri1 == null) 
            {
                if (uri2 == null) 
                    return 0; // Equal
                return -1;    // null < non-null
            }
            if ((object) uri2 == null) 
                return 1;     // non-null > null
 
            // a relative uri is always less than an absolute one 
            if (!uri1.IsAbsoluteUri || !uri2.IsAbsoluteUri)
                return uri1.IsAbsoluteUri? 1: uri2.IsAbsoluteUri? -1: string.Compare(uri1.OriginalString, uri2.OriginalString, comparisonType); 

            return string.Compare(
                                    uri1.GetParts(partsToCompare, compareFormat),
                                    uri2.GetParts(partsToCompare, compareFormat), 
                                    comparisonType
                                  ); 
        } 
        //
        // 
        //
        public static string UnescapeDataString(string stringToUnescape)
        {
            if ((object) stringToUnescape == null) 
                throw new ArgumentNullException("stringToUnescape");
 
            if (stringToUnescape.Length == 0) 
                return string.Empty;
 
            unsafe {
                fixed (char* pStr = stringToUnescape)
                {
                    int position; 
                    for (position = 0; position < stringToUnescape.Length; ++position)
                        if (pStr[position] == '%') 
                            break; 

                    if (position == stringToUnescape.Length) 
                        return stringToUnescape;

                    position = 0;
                    char[] dest = new char[stringToUnescape.Length]; 
                    dest = UnescapeString(stringToUnescape, 0, stringToUnescape.Length, dest, ref position, c_DummyChar, c_DummyChar, c_DummyChar,
                        UnescapeMode.Unescape | UnescapeMode.UnescapeAllOrThrow, null, false, true); 
                    return new string(dest, 0, position); 
                }
            } 
        }
        //
        // Where stringToEscape is intented to be a completely unescaped URI string.
        // This method will escape any character that is not a reserved or unreserved character, including percent signs. 
        // Note that EscapeUriString will also do not escape a '#' sign.
        // 
        public static string EscapeUriString(string stringToEscape) 
        {
            if ((object)stringToEscape == null) 
                throw new ArgumentNullException("stringToUnescape");

            if (stringToEscape.Length == 0)
                return string.Empty; 

            int position = 0; 
            char[] dest = EscapeString(stringToEscape, 0, stringToEscape.Length, null, ref position, true, c_DummyChar, c_DummyChar, c_DummyChar); 
            if ((object) dest == null)
                return stringToEscape; 
            return new string(dest, 0, position);
        }
        //
        // Where stringToEscape is intended to be URI data, but not an entire URI. 
        // This method will escape any character that is not an unreserved character, including percent signs.
        // 
        public static string EscapeDataString(string stringToEscape) 
        {
            if ((object) stringToEscape == null) 
                throw new ArgumentNullException("stringToUnescape");

            if (stringToEscape.Length == 0)
                return string.Empty; 

            int position = 0; 
            char[] dest = EscapeString(stringToEscape, 0, stringToEscape.Length, null, ref position, false, c_DummyChar, c_DummyChar, c_DummyChar); 
            if (dest == null)
                return stringToEscape; 
            return new string(dest, 0, position);
        }

        // - forceX characters are always escaped if found 
        // - rsvd character will remain unescaped
        // 
        // start    - starting offset from input 
        // end      - the exclusive ending offset in input
        // destPos  - starting offset in dest for output, on return this will be an exclusive "end" in the output. 
        //
        // In case "dest" has lack of space it will be reallocated by preserving the _whole_ content up to current destPos
        //
        // Returns null if nothing has to be escaped AND passed dest was null, otherwise the resulting array with the updated destPos 
        //
        const short c_MaxAsciiCharsReallocate   = 40; 
        const short c_MaxUnicodeCharsReallocate = 40; 
        const short c_MaxUTF_8BytesPerUnicodeChar  = 4;
        const short c_EncodedCharsPerByte = 3; 
        private unsafe static char[] EscapeString(string input, int start, int end, char[] dest, ref int destPos, bool isUriString, char force1, char force2, char rsvd)
        {
            if (end - start >= c_MaxUriBufferSize)
                throw GetException(ParsingError.SizeLimit); 

            int i = start; 
            int prevInputPos = start; 
            byte *bytes = stackalloc byte[c_MaxUnicodeCharsReallocate*c_MaxUTF_8BytesPerUnicodeChar];   // 40*4=160
 
            fixed (char* pStr = input)
            {
                for(; i < end; ++i)
                { 
                    char ch = pStr[i];
 
                    // a Unicode ? 
                    if (ch  > '\x7F')
                    { 
                        short maxSize = (short)Math.Min(end - i, (int)c_MaxUnicodeCharsReallocate-1);

                        short count = 1;
                        for (; count < maxSize && pStr[i + count] > '\x7f'; ++count) 
                            ;
 
                        // Is the last a high surrogate? 
                        if (pStr[i + count-1] >= 0xD800 && pStr[i + count-1] <= 0xDBFF)
                        { 
                            // Should be a rare case where the app tries to feed an invalid Unicode surrogates pair
                           if (count == 1 || count == end - i)
                                throw new UriFormatException(SR.GetString(SR.net_uri_BadString));
                           // need to grab one more char as a Surrogate except when it's a bogus input 
                           ++count;
                        } 
 
                        dest = EnsureDestinationSize(pStr, dest, i, (short)(count*c_MaxUTF_8BytesPerUnicodeChar*c_EncodedCharsPerByte),
                                                     c_MaxUnicodeCharsReallocate*c_MaxUTF_8BytesPerUnicodeChar*c_EncodedCharsPerByte, 
                                                     ref destPos, prevInputPos);

                        short numberOfBytes = (short)Encoding.UTF8.GetBytes(pStr+i, count, bytes, c_MaxUnicodeCharsReallocate*c_MaxUTF_8BytesPerUnicodeChar);
 
                        // This is the only exception that built in UriParser can throw after a Uri ctor.
                        // Should not happen unless the app tries to feed an invalid Unicode String 
                        if (numberOfBytes == 0) 
                            throw new UriFormatException(SR.GetString(SR.net_uri_BadString));
 
                        i += (count-1);

                        for (count = 0 ; count < numberOfBytes; ++count)
                            EscapeAsciiChar((char)bytes[count], dest, ref destPos); 

                        prevInputPos = i+1; 
                    } 
                    else if (ch == '%' && rsvd == '%')
                    { 
                        // Means we don't reEncode '%' but check for the possible escaped sequence
                        dest = EnsureDestinationSize(pStr, dest, i, c_EncodedCharsPerByte, c_MaxAsciiCharsReallocate*c_EncodedCharsPerByte, ref destPos, prevInputPos);
                        if(i + 2 < end && EscapedAscii(pStr[i+1], pStr[i+2]) != c_DummyChar)
                        { 
                            // leave it escaped
                            dest[destPos++] = '%'; 
                            dest[destPos++] = pStr[i+1]; 
                            dest[destPos++] = pStr[i+2];
                            i += 2; 
                        }
                        else
                        {
                            EscapeAsciiChar('%', dest, ref destPos); 
                        }
                        prevInputPos = i+1; 
                    } 
                    else if (ch == force1 ||  ch == force2)
                    { 
                        dest = EnsureDestinationSize(pStr, dest, i, c_EncodedCharsPerByte, c_MaxAsciiCharsReallocate*c_EncodedCharsPerByte, ref destPos, prevInputPos);
                        EscapeAsciiChar(ch, dest, ref destPos);
                        prevInputPos = i+1;
                    } 
                    else if (ch != rsvd && (isUriString? IsNotReservedNotUnreservedNotHash(ch): IsNotUnreserved(ch)))
                    { 
                        dest = EnsureDestinationSize(pStr, dest, i, c_EncodedCharsPerByte, c_MaxAsciiCharsReallocate*c_EncodedCharsPerByte, ref destPos, prevInputPos); 
                        EscapeAsciiChar(ch, dest, ref destPos);
                        prevInputPos = i+1; 
                    }
                }

                if (prevInputPos != i) 
                {
                    // need to fill up the dest array ? 
                    if (prevInputPos != start || dest != null) 
                        dest = EnsureDestinationSize(pStr, dest, i, 0, 0, ref destPos, prevInputPos);
                } 
            }

            return dest;
        } 

        // 
        // ensure destination array has enough space and contains all the needed input stuff 
        //
        private unsafe static char[] EnsureDestinationSize(char *pStr, char[] dest, int currentInputPos, short charsToAdd, short minReallocateChars, ref int destPos, int prevInputPos) 
        {
            if ((object) dest == null || dest.Length  < destPos + (currentInputPos-prevInputPos) + charsToAdd)
            {
                // allocating or reallocating array by ensuring enough space based on maxCharsToAdd. 
                char[] newresult = new char[destPos + (currentInputPos-prevInputPos) + minReallocateChars];
 
                if ((object) dest != null && destPos != 0) 
                    Buffer.BlockCopy(dest, 0, newresult, 0, destPos<<1);
                dest = newresult; 
            }

            // ensuring we copied everything form the input string left before last escaping
            while (prevInputPos != currentInputPos) 
                dest[destPos++] = pStr[prevInputPos++];
            return dest; 
        } 
        //
        // mark        = "-" | "_" | "." | "!" | "~" | "*" | "'" | "(" | ")" 
        // reserved    = ";" | "/" | "?" | ":" | "@" | "&" | "=" | "+" | "$" | ","
        // excluded = control | space | delims | unwise
        // delims      = "<" | ">" | "#" | "%" | <">
        // unwise      = "{" | "}" | "|" | "\" | "^" | "[" | "]" | "`" 
        //
        private static unsafe bool IsNotReservedNotUnreservedNotHash(char c) 
        { 
            if (c > 'z' && c != '~')
            { 
                return true;
            }
            else if (c > 'Z' && c < 'a' && c != '_')
            { 
                return true;
            } 
            else if (c < '!') 
            {
                return true; 
            }
            else if (c == '>' || c == '<' || c == '%' || c == '"' || c == '`')
            {
                return true; 
            }
            return false; 
        } 
        //
        private static unsafe bool IsNotUnreserved(char c) 
        {
            if (c > 'z' && c != '~')
            {
                return true; 
            }
            else if ((c > '9' && c < 'A') || (c > 'Z' && c < 'a' && c != '_')) 
            { 
                return true;
            } 
            else if (c < '\'' && c != '!')
            {
                return true;
            } 
            else if (c == '+' || c == ',' || c == '/')
            { 
                return true; 
            }
            return false; 
        }

        //
        // This method will assume that any good Escaped Sequence will be unescaped in the output 
        // - Assumes Dest.Length - detPosition >= end-start
        // - UnescapeLevel controls various modes of opearion 
        // - Any "bad" escape sequence will remain as is or '%' will be escaped. 
        // - destPosition tells the starting index in dest for placing the result.
        //   On return destPosition tells the last character + 1 postion in the "dest" array. 
        // - The control chars and chars passed in rsdvX parameters may be re-escaped depending on UnescapeLevel
        // - It is a RARE case when Unescape actually needs escaping some characteres mentioned above.
        //   For this reason it returns a char[] that is usually the same ref as the input "dest" value.
        // 
        [Flags]
        private enum UnescapeMode 
        { 
            CopyOnly                = 0x0,                  // used for V1.0 ToString() compatibility mode only
            Escape                  = 0x1,                  // Only used by ImplicitFile, the string is already fully unescaped 
            Unescape                = 0x2,                  // Only used as V1.0 UserEscaped compatibility mode
            EscapeUnescape          = Unescape|Escape,      // does both escaping control+reserved and unescaping of safe characters
            V1ToStringFlag          = 0x4,                  // Only used as V1.0 ToString() compatibility mode, assumes DontEscape level also
            UnescapeAll             = 0x8,                  // just unescape everything, leave bad escaped sequences as is (should throw but can't due to v1.0 compat) 
            UnescapeAllOrThrow      = 0x10|UnescapeAll,     // just unescape everything plus throw on bad escaped sequences
        } 
        private unsafe static char[] UnescapeString(   string input, int start, int end, char[] dest, ref int destPosition, 
                                                        char rsvd1, char rsvd2, char rsvd3, UnescapeMode unescapeMode,
                                                        UriParser syntax, bool isQuery, bool readOnlyConfig) 
        {
            fixed (char *pStr = input)
            {
                return UnescapeString(  pStr, start, end, dest, ref destPosition, 
                                        rsvd1, rsvd2, rsvd3, unescapeMode, syntax, isQuery, readOnlyConfig);
            } 
        } 
        private unsafe static char[] UnescapeString(char *pStr, int start, int end, char[] dest, ref int destPosition,
            char rsvd1, char rsvd2, char rsvd3, UnescapeMode unescapeMode, UriParser syntax, bool isQuery, bool readOnlyConfig) 
        {
            byte [] bytes = null;
            byte escapedReallocations = 0;
            bool escapeReserved = false; 
            int next = start;
            bool iriParsing = s_IriParsing && ((readOnlyConfig) || (!readOnlyConfig && IriParsingStatic(syntax))) 
                                && ((unescapeMode & UnescapeMode.EscapeUnescape) == UnescapeMode.EscapeUnescape); 

            while (true) 
            {
                // we may need to re-pin dest[]
                fixed (char* pDest = dest)
                { 
                    if ((unescapeMode & UnescapeMode.EscapeUnescape) == UnescapeMode.CopyOnly)
                    { 
                        while (start < end) 
                            pDest[destPosition++] = pStr[start++];
                        return dest; 
                    }

                    while (true)
                    { 
                        char ch = (char)0;
 
                        for (;next < end; ++next) 
                        {
                            if ((ch = pStr[next]) == '%') 
                            {
                                if ((unescapeMode & UnescapeMode.Unescape) == 0)
                                {
                                    // re-escape, don't check anything else 
                                    escapeReserved = true;
                                } 
                                else if (next+2 < end) 
                                {
                                    ch = EscapedAscii(pStr[next+1], pStr[next+2]); 
                                    // Unescape a good sequence if full unescape is requested
                                    if (unescapeMode >= UnescapeMode.UnescapeAll)
                                    {
                                        if (ch == c_DummyChar) 
                                        {
                                            if (unescapeMode >= UnescapeMode.UnescapeAllOrThrow) 
                                            { 
                                                // Should be a rare case where the app tries to feed an invalid escaped sequence
                                                throw new UriFormatException(SR.GetString(SR.net_uri_BadString)); 
                                            }
                                            continue;
                                        }
                                    } 
                                    // re-escape % from an invalid sequence
                                    else if (ch == c_DummyChar) 
                                    { 
                                        if ((unescapeMode & UnescapeMode.Escape) != 0)
                                            escapeReserved = true; 
                                        else
                                            continue;   // we should throw instead but since v1.0 woudl just print '%'
                                    }
                                    // Do not unescape '%' itself unless full unescape is requested 
                                    else if (ch == '%')
                                    { 
                                        next += 2; 
                                        continue;
                                    } 
                                    // Do not unescape a reserved char unless full unescape is requested
                                    else if (ch == rsvd1 || ch == rsvd2 || ch == rsvd3)
                                    {
                                        next += 2; 
                                        continue;
                                    } 
                                    // Do not unescape a dangerous char unless it's V1ToStringFlags mode 
                                    else if ((unescapeMode & UnescapeMode.V1ToStringFlag) == 0 && IsNotSafeForUnescape(ch))
                                    { 
                                        next += 2;
                                        continue;
                                    }
                                    else if (iriParsing && ((ch <='\x9F' && IsNotSafeForUnescape(ch)) || 
                                                            (ch >'\x9F' &&!CheckIriUnicodeRange(ch, isQuery))))
                                    { 
                                        // check if unenscaping gives a char ouside iri range 
                                        // if it does then keep it escaped
                                        next += 2; 
                                        continue;
                                    }
                                    // unescape escaped char or escape %
                                    break; 
                                }
                                else if (unescapeMode >= UnescapeMode.UnescapeAll) 
                                { 
                                    if (unescapeMode >= UnescapeMode.UnescapeAllOrThrow)
                                    { 
                                        // Should be a rare case where the app tries to feed an invalid escaped sequence
                                        throw new UriFormatException(SR.GetString(SR.net_uri_BadString));
                                    }
                                    // keep a '%' as part of a bogus sequence (we should throw but this is how 1.0 has shipped) 
                                    continue;
                                } 
                                else 
                                {
                                    escapeReserved = true; 
                                }
                                // escape (escapeReserved==ture) or otheriwse unescape the sequence
                                break;
                            } 
                            else if ((unescapeMode & (UnescapeMode.Unescape | UnescapeMode.UnescapeAll)) == (UnescapeMode.Unescape | UnescapeMode.UnescapeAll))
                            { 
                                continue; 
                            }
                            else if ((unescapeMode & UnescapeMode.Escape) != 0) 
                            {
                                 // Could actually escape some of the characters
                                 if (ch == rsvd1 || ch == rsvd2 || ch == rsvd3)
                                 { 
                                     // found an unescaped reserved character -> escape it
                                     escapeReserved = true; 
                                     break; 
                                 }
                                 else if ((unescapeMode & UnescapeMode.V1ToStringFlag) == 0 && (ch <= '\x1F' || (ch >= '\x7F' && ch <= '\x9F'))) 
                                 {
                                     // found an unescaped reserved character -> escape it
                                     escapeReserved = true;
                                     break; 
                                 }
                            } 
                        } 

                        //copy off previous characters from input 
                        while (start < next)
                            pDest[destPosition++] = pStr[start++];

                        if (next != end) 
                        {
                            //VsWhidbey#87423 
                            if (escapeReserved) 
                            {
                                //escape that char 
                                // Since this should be _really_ rare case, reallocate with constant size increase of 30 rsvd-type characters.
                                if (escapedReallocations == 0)
                                {
                                    escapedReallocations = 30; 
                                    char[] newDest = new char[dest.Length + escapedReallocations*3];
                                    fixed (char *pNewDest = newDest) 
                                    { 
                                        for (int i = 0; i < destPosition; ++i)
                                            pNewDest[i] = pDest[i]; 
                                    }
                                    dest = newDest;
                                    // re-pin new dest[] array
                                    goto dest_fixed_loop_break; 
                                }
                                else 
                                { 
                                    --escapedReallocations;
                                    EscapeAsciiChar(pStr[next], dest, ref destPosition); 
                                    escapeReserved = false;
                                    start = ++next;
                                    continue;
                                } 
                            }
 
                            // unescaping either one Ascii or possibly multiple Unicode 

                            if (ch <= '\x7F') 
                            {
                                //ASCII
                                dest[destPosition++] = ch;
                                next+=3; 
                                start = next;
                                continue; 
                            } 

                            // Unicode 

                            int byteCount = 1;
                            // lazy initialization of max size, will reuse the array for next sequences
                            if ((object) bytes == null) 
                                bytes = new byte[end - next];
 
                            bytes[0] = (byte)ch; 
                            next+=3;
                            while (next < end) 
                            {
                                // Check on exit criterion
                                if ((ch = pStr[next]) != '%' || next+2 >= end)
                                    break; 

                                // already made sure we have 3 characters in str 
                                ch = EscapedAscii(pStr[next+1], pStr[next+2]); 

                                //invalid hex sequence ? 
                                if (ch == c_DummyChar)
                                    break;
                                // character is not part of a UTF-8 sequence ?
                                else if (ch < '\x80') 
                                    break;
                                else 
                                { 
                                    //a UTF-8 sequence
                                    bytes[byteCount++] = (byte)ch; 
                                    next += 3;
                                }
                            }
                            Encoding noFallbackCharUTF8 = Encoding.GetEncoding("utf-8", new EncoderReplacementFallback(""), new DecoderReplacementFallback("")); 
                            char[] unescapedChars = new char[bytes.Length];
                            int charCount = noFallbackCharUTF8.GetChars(bytes, 0, byteCount, unescapedChars, 0); 
 
                            if (charCount != 0)
                            { 
                                start = next;

                                // match exact bytes
                                // Do not unescape chars not allowed by Iri 
                                // need to check for invalid utf sequences that may not have given any chars
 
                                MatchUTF8Sequence(pDest, dest, ref destPosition, unescapedChars, charCount, bytes, isQuery, iriParsing); 
                            }
                            else 
                            {
                                // the encoded, high-ANSI characters are not UTF-8 encoded
                                // [....] codereview: a bug?
                                // If we expect UTF-8 then we should discard (not copy) anything else. 
                                // The reason is that >0x7F Ansi and UTF-8 cannot be supported at the same time
 
                                if (unescapeMode >= UnescapeMode.UnescapeAllOrThrow) 
                                {
                                    // Should be a rare case where the app tries to feed an invalid escaped sequence 
                                    throw new UriFormatException(SR.GetString(SR.net_uri_BadString));
                                }

                                next = start + 3; 
                                start = next;
                                dest[destPosition++]= (char)bytes[0]; 
                            } 
                        }
 
                        if (next == end)
                            goto done;
                    }
dest_fixed_loop_break: ; 
                }
            } 
 
done:       return dest;
        } 

        //
        // Need to check for invalid utf sequences that may not have given any chars.
        // We got the unescaped chars, we then reencode them and match off the bytes 
        // to get the invalid sequence bytes that we just copy off
        // 
        private static unsafe void MatchUTF8Sequence(char* pDest, char[] dest, ref int destOffset, char[] unescapedChars, 
                                                    int charCount, byte[] bytes, bool isQuery, bool iriParsing)
        { 
            int count = 0;
            fixed (char* unescapedCharsPtr = unescapedChars){
                for (int j = 0; j < charCount; ++j){
                    bool isHighSurr = Char.IsHighSurrogate(unescapedCharsPtr[j]); 
                    byte[] encodedBytes = Encoding.UTF8.GetBytes(unescapedChars, j, isHighSurr ? 2 : 1);
                    int encodedBytesLength = encodedBytes.Length; 
 
                    // we have to keep unicode chars outside Iri range escaped
                    bool inIriRange = false; 
                    if (iriParsing){
                        if (!isHighSurr)
                            inIriRange = CheckIriUnicodeRange(unescapedChars[j], isQuery);
                        else{ 
                            bool surrPair = false;
                            inIriRange = CheckIriUnicodeRange(unescapedChars[j], unescapedChars[j + 1], 
                                                                   ref surrPair, isQuery); 
                        }
                    } 

                    while (true){
                        while (bytes[count] != encodedBytes[0]){
                            EscapeAsciiChar((char)bytes[count++], dest, ref destOffset); 
                        }
 
                        // check if all bytes match 
                        bool allBytesMatch = true;
                        int k = 0; 
                        for (; k < encodedBytesLength; ++k){
                            if (bytes[count + k] != encodedBytes[k]){
                                allBytesMatch = false;
                                break; 
                            }
                        } 
 
                        if (allBytesMatch){
                            count += encodedBytesLength; 
                            if (iriParsing){
                                if (!inIriRange){
                                    // need to keep chars not allowed as escaped
                                    for (int l = 0; l < encodedBytes.Length; ++l){ 
                                        EscapeAsciiChar((char)encodedBytes[l], dest, ref destOffset);
                                    } 
                                } 
                                else if (!IsBidiControlCharacter(unescapedCharsPtr[j])){
                                    //copy chars 
                                    pDest[destOffset++] = unescapedCharsPtr[j];
                                }
                                if (isHighSurr)
                                    pDest[destOffset++] = unescapedCharsPtr[j + 1]; 
                            }
                            else{ 
                                //copy chars 
                                pDest[destOffset++] = unescapedCharsPtr[j];
 
                                if (isHighSurr)
                                    pDest[destOffset++] = unescapedCharsPtr[j + 1];
                            }
                                break; // break out of while (true) since we've matched this char bytes 
                        }
                        else{ 
                            // copy bytes till place where bytes dont match 
                            for (int l = 0; l < k; ++l){
                                EscapeAsciiChar((char)bytes[count++], dest, ref destOffset); 
                            }
                        }
                    }
 
                    if (isHighSurr) j++;
 
                } 
            }
        } 

        // for rfc 3987
        internal bool CheckIsReserved(char ch)
        { 
            char[] rsvd = new char[]{':', '/', '?', '#', '[', ']', '@'}; //gen-delims
            for (int i = 0; i < rsvd.Length; ++i){ 
                if (rsvd[i] == ch) return true; 
            }
            return false; 
        }

        //
        // Check reserved chars according to rfc 3987 in a sepecific component 
        //
        internal bool CheckIsReserved(char ch, UriComponents component) 
        { 
            if ((component != UriComponents.Scheme) ||
                    (component != UriComponents.UserInfo) || 
                    (component != UriComponents.Host) ||
                    (component != UriComponents.Port) ||
                    (component != UriComponents.Path) ||
                    (component != UriComponents.Query) || 
                    (component != UriComponents.Fragment)
                ) 
                return (component == (UriComponents)0)? CheckIsReserved(ch): false; 
            else
            { 
                switch(component)
                {
                    // Reserved chars according to rfc 3987
                    case UriComponents.UserInfo: 
                        if( ch == '/' || ch == '?' || ch == '#' || ch == '[' || ch == ']' || ch == '@' )
                            return true; 
                        break; 
                    case UriComponents.Host:
                        if( ch == ':' || ch == '/' || ch == '?' || ch == '#' || ch == '[' || ch == ']' || ch == '@' ) 
                            return true;
                        break;
                    case UriComponents.Path:
                        if( ch == '/' || ch == '?' || ch == '#' || ch == '[' || ch == ']' ) 
                            return true;
                        break; 
                    case UriComponents.Query: 
                        if(ch == '#' || ch == '[' || ch == ']')
                            return true; 
                        break;
                    case UriComponents.Fragment:
                        if(ch == '#' || ch == '[' || ch == ']')
                            return true; 
                        break;
                    default: 
                        break; 
                }
                return false; 
            }
        }

        // 
        // Cleans up the specified component according to Iri rules
        // a) Chars allowed by iri in a component are unescaped if found escaped 
        // b) Bidi chars are stripped 
        //
        // should be called only if IRI parsing is switched on 
        internal unsafe string EscapeUnescapeIri(string input, int start, int end, UriComponents component)
        {
            fixed (char *pInput = input)
            { 
                return EscapeUnescapeIri(pInput, start, end, component);
            } 
        } 

        // 
        // See above explanation
        //
        internal unsafe string EscapeUnescapeIri(char* pInput, int start, int end, UriComponents component)
        { 

            char [] dest = new char[ end - start ]; 
            byte[] bytes = null; 

            // Pin the array to do pointer accesses 
            GCHandle destHandle = GCHandle.Alloc(dest, GCHandleType.Pinned);
            char* pDest = (char*)destHandle.AddrOfPinnedObject();

            byte escapedReallocations = 0; 
            int next = start;
            int destOffset = 0; 
            char ch; 
            bool escape = false;
            bool surrogatePair = false; 
            bool isUnicode = false;

            for (;next < end; ++next)
            { 
                escape = false;
                surrogatePair = false; 
                isUnicode = false; 

                if ((ch = pInput[next]) == '%'){ 
                    if (next + 2 < end){
                        ch = EscapedAscii(pInput[next + 1], pInput[next + 2]);
                        // Do not unescape a reserved char
                        if (ch == c_DummyChar || ch == '%' || CheckIsReserved(ch, component) || IsNotSafeForUnescape(ch)){ 
                            // keep as is
                            pDest[destOffset++] = pInput[next++]; 
                            pDest[destOffset++] = pInput[next++]; 
                            pDest[destOffset++] = pInput[next];
                            continue; 
                        }
                        else if (ch <= '\x7F'){
                            //ASCII
                            pDest[destOffset++] = ch; 
                            next += 2;
                            continue; 
                        }else{ 
                            // possibly utf8 encoded sequence of unicode
 
                            // check if safe to unescape according to Iri rules

                            int startSeq = next;
                            int byteCount = 1; 
                            // lazy initialization of max size, will reuse the array for next sequences
                            if ((object)bytes == null) 
                                bytes = new byte[end - next]; 

                            bytes[0] = (byte)ch; 
                            next += 3;
                            while (next < end)
                            {
                                // Check on exit criterion 
                                if ((ch = pInput[next]) != '%' || next + 2 >= end)
                                    break; 
 
                                // already made sure we have 3 characters in str
                                ch = EscapedAscii(pInput[next + 1], pInput[next + 2]); 

                                //invalid hex sequence ?
                                if (ch == c_DummyChar)
                                    break; 
                                // character is not part of a UTF-8 sequence ?
                                else if (ch < '\x80') 
                                    break; 
                                else
                                { 
                                    //a UTF-8 sequence
                                    bytes[byteCount++] = (byte)ch;
                                    next += 3;
                                } 
                            }
                            next--; // for loop will increment 
 
                            Encoding noFallbackCharUTF8 = Encoding.GetEncoding("utf-8",  new EncoderReplacementFallback(""), new DecoderReplacementFallback(""));
                            char[] unescapedChars = new char[bytes.Length]; 
                            int charCount = noFallbackCharUTF8.GetChars(bytes, 0, byteCount, unescapedChars, 0);


                            if (charCount != 0){ 

                                // need to check for invalid utf sequences that may not have given any chars 
 
                                // check if unicode value is allowed
                                MatchUTF8Sequence(  pDest, dest, ref destOffset, unescapedChars, charCount, bytes, 
                                                    component == UriComponents.Query, true);
                            }
                            else
                            { 
                                // copy escaped sequence as is
                                for (int i = startSeq; i <= next; ++i) 
                                    pDest[destOffset++] = pInput[i]; 
                            }
 
                        }

                    }else{
                        pDest[destOffset++] = pInput[next]; 
                    }
                } 
                else if (ch > '\x7f'){ 
                    // unicode
 
                    char ch2;

                    if ((Char.IsHighSurrogate(ch)) && (next + 1 < end)){
                        ch2 = pInput[next + 1]; 
                        escape = !CheckIriUnicodeRange(ch, ch2, ref surrogatePair, component == UriComponents.Query);
                        if (!escape){ 
                            // copy the two chars 
                            pDest[destOffset++] = pInput[next++];
                            pDest[destOffset++] = pInput[next]; 
                        }else{
                            isUnicode = true;
                        }
                    }else{ 
                        if(CheckIriUnicodeRange(ch, component == UriComponents.Query)){
                            if (!IsBidiControlCharacter(ch)){ 
                                // copy it 
                                pDest[destOffset++] = pInput[next];
                            } 
                        }else{
                            // escape it
                            escape = true;
                            isUnicode = true; 
                        }
                    } 
                }else{ 
                    // just copy the character
                    pDest[destOffset++] = pInput[next]; 
                }

                if (escape){
                    if (escapedReallocations == 0){ 
                        // may need more memory since we didnt anticipate escaping
                        escapedReallocations = 30; 
 
                        char[] newDest = new char[dest.Length + escapedReallocations * 3];
                        fixed (char* pNewDest = newDest){ 
                            for (int i = 0; i < destOffset; ++i)
                                pNewDest[i] = pDest[i];
                        }
                        if (destHandle.IsAllocated) 
                            destHandle.Free();
                        dest = newDest; 
 
                        // re-pin new dest[] array
                        destHandle = GCHandle.Alloc(dest, GCHandleType.Pinned); 
                        pDest = (char*)destHandle.AddrOfPinnedObject();
                    }else{
                        if (isUnicode){
                            if (surrogatePair) 
                                escapedReallocations -= 4;
                            else 
                                escapedReallocations -= 3; 
                        }
                        else 
                            --escapedReallocations;
                    }

                    byte[] encodedBytes = new byte[4]; 
                    fixed (byte* pEncodedBytes = encodedBytes){
                        int encodedBytesCount = Encoding.UTF8.GetBytes(pInput + next, surrogatePair ? 2 : 1, pEncodedBytes, 4); 
 
                        for (int count = 0; count < encodedBytesCount; ++count)
                            EscapeAsciiChar((char)encodedBytes[count], dest, ref destOffset); 
                    }
                }
            }
 
            if (destHandle.IsAllocated)
                destHandle.Free(); 
            return new string(dest, 0 , destOffset ); 
        }
 
        //
        // Do not unescape these in safe mode:
        // 1)  reserved    = ";" | "/" | "?" | ":" | "@" | "&" | "=" | "+" | "$" | ","
        // 2)  excluded = control | "#" | "%" | "\" 
        //
        // That will still give plenty characters unescaped by SafeUnesced mode such as 
        // 1) Unicode characters 
        // 2) Unreserved = alphanum | "-" | "_" | "." | "!" | "~" | "*" | "'" | "(" | ")"
        // 3) DelimitersAndUnwise = "<" | ">" |  <"> | "{" | "}" | "|" | "^" | "[" | "]" | "`" 
        static bool IsNotSafeForUnescape(char ch)
        {
            if (ch <= '\x1F' || (ch >= '\x7F' && ch <= '\x9F'))
                return true; 
            else if ((ch >= ';' && ch <= '@' && (ch | '\x2') != '>') ||
                     (ch >= '#' && ch <= '&') || 
                     ch == '+' || ch == ',' || ch == '/' || ch == '\\') 
                return true;
 
            return false;
        }

 
        //
        // Internal stuff 
        // 

        // Returns false if OriginalString value 
        // (1) is not correctly escaped as per URI spec excluding intl UNC name case
        // (2) or is an absolute Uri that represents implicit file Uri "c:\dir\file"
        // (3) or is an absolute Uri that misses a slash before path "file://c:/dir/file"
        // (4) or contains unescaped backslashes even if they will be treated 
        //     as forward slashes like http:\\host/path\file or file:\\\c:\path
        // 
        internal unsafe bool InternalIsWellFormedOriginalString() 
        {
            if (UserDrivenParsing) 
                throw new InvalidOperationException(SR.GetString(SR.net_uri_UserDrivenParsing, this.GetType().FullName));

            fixed (char* str = m_String)
            { 
                ushort idx = 0;
                // 
                // For a relative Uri we only care about escaping and backslashes 
                //
                if (!IsAbsoluteUri) 
                    return (CheckCanonical(str, ref idx, (ushort)m_String.Length, c_EOL) & (Check.BackslashInPath | Check.EscapedCanonical)) == Check.EscapedCanonical;

                //
                // (2) or is an absolute Uri that represents implicit file Uri "c:\dir\file" 
                //
                if (IsImplicitFile) 
                    return false; 

                //This will get all the offsets, a Host name will be checked separatelly below 
                EnsureParseRemaining();

                Flags nonCanonical = (m_Flags & (Flags.E_CannotDisplayCanonical | Flags.IriCanonical));
                // User, Path, Query or Fragment may have some non escaped characters 
                if (((nonCanonical & Flags.E_CannotDisplayCanonical & (Flags.E_UserNotCanonical | Flags.E_PathNotCanonical |
                                        Flags.E_QueryNotCanonical | Flags.E_FragmentNotCanonical)) != Flags.Zero) && 
                    (!m_iriParsing || (m_iriParsing && 
                    (((nonCanonical & Flags.E_UserNotCanonical) == 0) || ((nonCanonical & Flags.UserIriCanonical) == 0)) &&
                    (((nonCanonical & Flags.E_PathNotCanonical) == 0) || ((nonCanonical & Flags.PathIriCanonical) == 0)) && 
                    (((nonCanonical & Flags.E_QueryNotCanonical) == 0) || ((nonCanonical & Flags.QueryIriCanonical) == 0)) &&
                    (((nonCanonical & Flags.E_FragmentNotCanonical) == 0) || ((nonCanonical & Flags.FragmentIriCanonical) == 0)))))
                    return false;
 
                // checking on scheme:\\ or file:////
                if (InFact(Flags.AuthorityFound)) 
                { 
                    idx = (ushort)(m_Info.Offset.Scheme + m_Syntax.SchemeName.Length + 2);
                    if (idx >= m_Info.Offset.User || m_String[idx - 1] == '\\' || m_String[idx] == '\\') 
                        return false;

#if !PLATFORM_UNIX
                    if (InFact(Flags.UncPath | Flags.DosPath)) 
                    {
                        while (++idx < m_Info.Offset.User && (m_String[idx] == '/' || m_String[idx] == '\\')) 
                            return false; 
                    }
#endif // !PLATFORM_UNIX 
                }


                // (3) or is an absolute Uri that misses a slash before path "file://c:/dir/file" 
                // Note that for this check to be more general we assert that if Path is non empty and if it requires a first slash
                // (which looks absent) then the method has to fail. 
                // Today it's only possible for a Dos like path, i.e. file://c:/bla would fail below check. 
                if (InFact(Flags.FirstSlashAbsent) && m_Info.Offset.Query > m_Info.Offset.Path)
                    return false; 

                // (4) or contains unescaped backslashes even if they will be treated
                //     as forward slashes like http:\\host/path\file or file:\\\c:\path
                // Note we do not check for Flags.ShouldBeCompressed i.e. allow // /./ and alike as valid 
                if (InFact(Flags.BackslashInPath))
                    return false; 
 
                // Capturing a rare case like file:///c|/dir
                if (IsDosPath && m_String[m_Info.Offset.Path + SecuredPathIndex - 1] == '|') 
                    return false;

                //
                // May need some real CPU processing to anwser the request 
                //
                // 
                // Check escaping for authority 
                //
                if ((m_Flags & Flags.CanonicalDnsHost) == 0) 
                {
                    idx = m_Info.Offset.User;
                    if (!(m_iriParsing && (HostType == Flags.IPv6HostType))){
                        Check result = CheckCanonical(str, ref idx, (ushort)m_Info.Offset.Path, '/'); 
                        if (((result & (Check.ReservedFound | Check.BackslashInPath | Check.EscapedCanonical)) != Check.EscapedCanonical) &&
                            (!m_iriParsing || (m_iriParsing && ((result & (Check.DisplayCanonical | Check.FoundNonAscii | Check.NotIriCanonical)) 
                                                            != (Check.DisplayCanonical | Check.FoundNonAscii))))) 
                            return false;
                    } 
                }

                // Want to ensure there are slashes after the scheme
                if ((m_Flags & (Flags.SchemeNotCanonical | Flags.AuthorityFound)) == (Flags.SchemeNotCanonical | Flags.AuthorityFound)) 
                {
                    idx = (ushort)m_Syntax.SchemeName.Length; 
                    while (str[idx++] != ':') ; 
                    if (idx + 1 >= m_String.Length || str[idx] != '/' || str[idx + 1] != '/')
                        return false; 
                }
            }
            //
            // May be scheme, host, port or path need some canonicalization but still the uri string is found to be a "well formed" one 
            //
            return true; 
        } 

 
        // Should never be used except by the below method
        private Uri(Flags flags, UriParser uriParser, string uri)
        {
            m_Flags = flags; 
            m_Syntax = uriParser;
            m_String = uri; 
        } 
        //
        // a Uri.TryCreate() method goes through here. 
        //
        internal static Uri CreateHelper(string uriString, bool dontEscape, UriKind uriKind, ref UriFormatException e)
        {
            // if (!Enum.IsDefined(typeof(UriKind), uriKind)) -- We currently believe that Enum.IsDefined() is too slow to be used here. 
            if ((int)uriKind < (int)UriKind.RelativeOrAbsolute || (int)uriKind > (int)UriKind.Relative){
                throw new ArgumentException(SR.GetString(SR.net_uri_InvalidUriKind, uriKind)); 
            } 

            UriParser syntax = null; 
            Flags flags = Flags.Zero;
            ParsingError err = ParseScheme(uriString, ref flags, ref syntax);

            if (dontEscape) 
                flags |= Flags.UserEscaped;
 
            // We won't use User factory for these errors 
            if (err != ParsingError.None)
            { 
                // If it looks as a relative Uri, custom factory is ignored
                if (uriKind != UriKind.Absolute && err <= ParsingError.LastRelativeUriOkErrIndex)
                    return new Uri((flags & Flags.UserEscaped), null, uriString);
 
                return null;
            } 
 
            // Cannot be relative Uri if came here
            Uri result = new Uri(flags, syntax, uriString); 

            // Validate instance using ether built in or a user Parser
            try
            { 
                result.InitializeUri(err, uriKind, out e);
 
                if (e == null) 
                    return result;
 
                return null;
            }
            catch (UriFormatException ee)
            { 
                System.Net.GlobalLog.Assert(!syntax.IsSimple, "A UriPraser threw on InitializeAndValidate.");
                e = ee; 
                // A precaution since custom Parser should never throw in this case. 
                return null;
            } 
        }
        //
        // Resolves into either baseUri or relativeUri according to conditions OR if not possible it uses newUriString
        // to  return combined URI strings from both Uris 
        // otherwise if e != null on output the operation has failed
        // 
 
        internal static Uri ResolveHelper(Uri baseUri, Uri relativeUri, ref string newUriString, ref bool userEscaped, out UriFormatException e)
        { 
            System.Net.GlobalLog.Assert(!baseUri.IsNotAbsoluteUri && !baseUri.UserDrivenParsing, "Uri::ResolveHelper()|baseUri is not Absolute or is controlled by User Parser.");

            e = null;
            string relativeStr = string.Empty; 

            if ((object)relativeUri != null) 
            { 
                if (relativeUri.IsAbsoluteUri)
                    return relativeUri; 

                relativeStr = relativeUri.OriginalString;
                userEscaped = relativeUri.UserEscaped;
            } 
            else
                relativeStr = string.Empty; 
 
            // Here we can assert that passed "relativeUri" is indeed a relative one
 
            if (relativeStr.Length > 0 && (IsLWS(relativeStr[0]) || IsLWS(relativeStr[relativeStr.Length - 1])))
                relativeStr = relativeStr.Trim(_WSchars);

            if (relativeStr.Length == 0) 
            {
                newUriString = baseUri.GetParts(UriComponents.AbsoluteUri, baseUri.UserEscaped ? UriFormat.UriEscaped : UriFormat.SafeUnescaped); 
                return null; 
            }
 
            // Check for a simple fragment in relative part
            if (relativeStr[0] == '#' && !baseUri.IsImplicitFile && baseUri.Syntax.InFact(UriSyntaxFlags.MayHaveFragment))
            {
                newUriString = baseUri.GetParts(UriComponents.AbsoluteUri & ~UriComponents.Fragment, UriFormat.UriEscaped) + relativeStr; 
                return null;
            } 
 
            // Check on the DOS path in the relative Uri (a special case)
            if (relativeStr.Length >= 3 
                && (relativeStr[1] == ':' || relativeStr[1] == '|')
                && IsAsciiLetter(relativeStr[0])
                && (relativeStr[2] == '\\' || relativeStr[2] == '/'))
            { 

                if (baseUri.IsImplicitFile) 
                { 
                    // It could have file:/// prepended to the result but we want to keep it as *Implicit* File Uri
                    newUriString = relativeStr; 
                    return null;
                }
                else if (baseUri.Syntax.InFact(UriSyntaxFlags.AllowDOSPath))
                { 
                    // The scheme is not changed just the path gets replaced
                    string prefix; 
                    if (baseUri.InFact(Flags.AuthorityFound)) 
                        prefix = baseUri.Syntax.InFact(UriSyntaxFlags.PathIsRooted) ? ":///" : "://";
                    else 
                        prefix = baseUri.Syntax.InFact(UriSyntaxFlags.PathIsRooted) ? ":/" : ":";

                    newUriString = baseUri.Scheme + prefix + relativeStr;
                    return null; 
                }
                // If we are here then input like "http://host/path/" + "C:\x" will produce the result  http://host/path/c:/x 
            } 

 
            ParsingError err = GetCombinedString(baseUri, relativeStr, userEscaped, ref newUriString);

            if (err != ParsingError.None)
            { 
                e = GetException(err);
                return null; 
            } 

            if ((object)newUriString == (object)baseUri.m_String) 
                return baseUri;

            return null;
        } 

        private unsafe string GetRelativeSerializationString(UriFormat format) 
        { 
            if (format == UriFormat.UriEscaped)
            { 
                if (m_String.Length == 0)
                    return string.Empty;
                int position = 0;
                char[] dest = EscapeString(m_String, 0, m_String.Length, null, ref position, true, c_DummyChar, c_DummyChar, '%'); 
                if ((object)dest == null)
                    return m_String; 
                return new string(dest, 0, position); 
            }
 
            else if (format == UriFormat.Unescaped)
                return UnescapeDataString(m_String);

            else if (format == UriFormat.SafeUnescaped) 
            {
                if (m_String.Length == 0) 
                    return string.Empty; 

                char[] dest = new char[m_String.Length]; 
                int position = 0;
                dest = UnescapeString(m_String, 0, m_String.Length, dest, ref position, c_DummyChar, c_DummyChar, c_DummyChar,
                    UnescapeMode.EscapeUnescape, null, false, true);
                return new string(dest, 0, position); 
            }
            else 
                throw new ArgumentOutOfRangeException("format"); 

        } 

        //
        // UriParser helpers methods
        // 
        internal string GetComponentsHelper(UriComponents uriComponents, UriFormat uriFormat)
        { 
            if (uriComponents == UriComponents.Scheme) 
                return m_Syntax.SchemeName;
 
            // A serialzation info is "almost" the same as AbsoluteUri except for IPv6 + ScopeID hostname case
            if ((uriComponents & UriComponents.SerializationInfoString) != 0)
                uriComponents |= UriComponents.AbsoluteUri;
 
            //This will get all the offsets, HostString will be created below if needed
            EnsureParseRemaining(); 
 
            //Check to see if we need the host/authotity string
            if ((uriComponents & UriComponents.Host) != 0) 
                EnsureHostString(true);

            //This, single Port request is always processed here
            if (uriComponents == UriComponents.Port || uriComponents == UriComponents.StrongPort) 
            {
                if (((m_Flags & Flags.NotDefaultPort) != 0) || (uriComponents == UriComponents.StrongPort && m_Syntax.DefaultPort != UriParser.NoDefaultPort)) 
                { 
                    // recreate string from the port value
                    return m_Info.Offset.PortValue.ToString(CultureInfo.InvariantCulture); 
                }
                return string.Empty;
            }
 
            if ((uriComponents & UriComponents.StrongPort) != 0)
            { 
                // Down the path we rely on Port to be ON for StrongPort 
                uriComponents |= UriComponents.Port;
            } 

            //This request sometime is faster to process here
            if (uriComponents == UriComponents.Host && (uriFormat == UriFormat.UriEscaped
                || (( m_Flags & (Flags.HostNotCanonical | Flags.E_HostNotCanonical)) == 0))) 
            {
                EnsureHostString(false); 
                return m_Info.Host; 
            }
 
            switch (uriFormat)
            {
                case UriFormat.UriEscaped:
                    return GetEscapedParts(uriComponents); 

                case V1ToStringUnescape: 
                case UriFormat.SafeUnescaped: 
                case UriFormat.Unescaped:
                    return GetUnescapedParts(uriComponents, uriFormat); 

                default:
                    throw new ArgumentOutOfRangeException("uriFormat");
            } 
        }
        // 
        // 
        //
        internal bool IsBaseOfHelper(Uri uriLink) 
        {
            //TO
            if (!IsAbsoluteUri || UserDrivenParsing)
                return false; 

            if (!uriLink.IsAbsoluteUri) 
            { 
                //a relative uri could have quite tricky form, it's better to fix it now.
                string newUriString = null; 
                UriFormatException e;
                bool dontEscape = false;

                uriLink = ResolveHelper(this, uriLink, ref newUriString, ref dontEscape, out e); 
                if (e != null)
                    return false; 
 
                if ((object)uriLink == null)
                    uriLink = CreateHelper(newUriString, dontEscape, UriKind.Absolute, ref e); 

                if (e != null)
                    return false;
            } 

            if (Syntax.SchemeName != uriLink.Syntax.SchemeName) 
                return false; 

            // Canonicalize and test for substring match up to the last path slash 
            string me = GetParts(UriComponents.AbsoluteUri & ~UriComponents.Fragment, UriFormat.SafeUnescaped);
            string she = uriLink.GetParts(UriComponents.AbsoluteUri & ~UriComponents.Fragment, UriFormat.SafeUnescaped);

            unsafe 
            {
                fixed (char* pMe = me) 
                { 
                    fixed (char* pShe = she)
                    { 
                        return TestForSubPath(pMe, (ushort)me.Length, pShe, (ushort)she.Length, IsUncOrDosPath || uriLink.IsUncOrDosPath);
                    }
                }
            } 
        }
        // 
        // Only a ctor time call 
        //
        private void CreateThisFromUri(Uri otherUri) 
        {
            // Clone the other guy but develop own UriInfo member
            m_Info = null;
 
            m_Flags = otherUri.m_Flags;
            if (InFact(Flags.MinimalUriInfoSet)) 
            { 
                m_Flags &= ~(Flags.MinimalUriInfoSet | Flags.AllUriInfoSet | Flags.IndexMask);
                m_Flags |= (Flags)otherUri.m_Info.Offset.Path; 

            }

            m_Syntax = otherUri.m_Syntax; 
            m_String = otherUri.m_String;
            m_iriParsing = otherUri.m_iriParsing; 
            if (otherUri.OriginalStringSwitched){ 
                m_originalUnicodeString = otherUri.m_originalUnicodeString;
            } 
            if (otherUri.AllowIdn && (otherUri.InFact(Flags.IdnHost) || otherUri.InFact(Flags.UnicodeHost))){
                m_DnsSafeHost = otherUri.m_DnsSafeHost;
            }
        } 
    }
} 
/*++ 
Copyright (c) 2003 Microsoft Corporation

Module Name:
 
    UriExt.cs
 
Abstract: 

    Uri extensibility model Implementation. 
    This file utilizes partial class feature.
    Uri.cs file contains core System.Uri functionality.

Author: 
    Alexei Vopilov    Nov 21 2003
 
Revision History: 

--*/ 

namespace System {
    using System.Collections.Generic;
    using System.Configuration; 
    using System.Globalization;
    using System.Net.Configuration; 
    using System.Security.Permissions; 
    using System.Text;
    using System.Runtime.InteropServices; 

    public partial class Uri {
        //
        // All public ctors go through here 
        //
        private void CreateThis(string uri, bool dontEscape, UriKind uriKind) 
        { 
            // if (!Enum.IsDefined(typeof(UriKind), uriKind)) -- We currently believe that Enum.IsDefined() is too slow to be used here.
            if ((int)uriKind < (int)UriKind.RelativeOrAbsolute || (int)uriKind > (int)UriKind.Relative) { 
                throw new ArgumentException(SR.GetString(SR.net_uri_InvalidUriKind, uriKind));
            }

            m_String = uri == null? string.Empty: uri; 

            if (dontEscape) 
                m_Flags |= Flags.UserEscaped; 

            ParsingError err = ParseScheme(m_String, ref m_Flags, ref m_Syntax); 
            UriFormatException e;

            InitializeUri(err, uriKind, out e);
            if (e != null) 
                throw e;
        } 
        // 
        private void InitializeUri(ParsingError err, UriKind uriKind, out UriFormatException e)
        { 
            if (err == ParsingError.None)
            {
                if (IsImplicitFile)
                { 
                    // V1 compat VsWhidbey#252282
                    // A relative Uri wins over implicit UNC path unless the UNC path is of the form "\\something" and uriKind != Absolute 
                    if ( 
#if !PLATFORM_UNIX
                        NotAny(Flags.DosPath) && 
#endif // !PLATFORM_UNIX
                        uriKind != UriKind.Absolute &&
                       (uriKind == UriKind.Relative || (m_String.Length >= 2 && (m_String[0] != '\\' || m_String[1] != '\\'))))
 
                    {
                        m_Syntax = null; //make it be relative Uri 
                        m_Flags &= Flags.UserEscaped; // the only flag that makes sense for a relative uri 
                        e = null;
                        return; 
                        // Otheriwse an absolute file Uri wins when it's of the form "\\something"
                    }
                    //
                    // VsWhidbey#423805 and V1 compat issue 
                    // We should support relative Uris of the form c:\bla or c:/bla
                    // 
#if !PLATFORM_UNIX 
                    else if (uriKind == UriKind.Relative && InFact(Flags.DosPath))
                    { 
                        m_Syntax = null; //make it be relative Uri
                        m_Flags &= Flags.UserEscaped; // the only flag that makes sense for a relative uri
                        e = null;
                        return; 
                        // Otheriwse an absolute file Uri wins when it's of the form "c:\something"
                    } 
#endif // !PLATFORM_UNIX 
                }
            } 
            else if (err > ParsingError.LastRelativeUriOkErrIndex)
            {
                //This is a fatal error based solely on scheme name parsing
                m_String = null; // make it be invalid Uri 
                e = GetException(err);
                return; 
            } 

            System.Net.GlobalLog.Assert(err == ParsingError.None || (object)m_Syntax == null, "Uri::.ctor|ParseScheme has found an error:{0}, but m_Syntax is not null.", err); 
            //
            //
            //
            bool hasUnicode = false; 

            // Is there unicode .. 
            if ((!s_ConfigInitialized) && CheckForConfigLoad(m_String)){ 
                InitializeUriConfig();
            } 

            m_iriParsing = (s_IriParsing && ((m_Syntax == null) || m_Syntax.InFact(UriSyntaxFlags.AllowIriParsing)));

            if (m_iriParsing && CheckForUnicode(m_String)){ 
                m_Flags |= Flags.HasUnicode;
                hasUnicode = true; 
                // switch internal strings 
                m_originalUnicodeString = m_String; // original string location changed
            } 

            if (m_Syntax != null)
            {
                if (m_Syntax.IsSimple) 
                {
                    if ((err = PrivateParseMinimal()) != ParsingError.None) 
                    { 
                        if (uriKind != UriKind.Absolute && err <= ParsingError.LastRelativeUriOkErrIndex)
                        { 
                            m_Syntax = null; // convert to relative uri
                            e = null;
                            m_Flags &= Flags.UserEscaped; // the only flag that makes sense for a relative uri
                        } 
                        else
                            e = GetException(err); 
                    } 
                    else if (uriKind == UriKind.Relative)
                    { 
                        // Here we know that we can create an absolute Uri, but the user has requested only a relative one
                        e = GetException(ParsingError.CannotCreateRelative);
                    }
                    else 
                        e = null;
                    // will return from here 
 
                    if (m_iriParsing && hasUnicode){
                        // In this scenario we need to parse the whole string 
                        EnsureParseRemaining();
                    }
                }
                else 
                {
                    // offer custom parser to create a parsing context 
                    m_Syntax = m_Syntax.InternalOnNewUri(); 

                    // incase they won't call us 
                    m_Flags |= Flags.UserDrivenParsing;

                    // Ask a registered type to validate this uri
                    m_Syntax.InternalValidate(this, out e); 

                    if (e != null) 
                    { 
                        // Can we still take it as a relative Uri?
                        if (uriKind != UriKind.Absolute && err != ParsingError.None && err <= ParsingError.LastRelativeUriOkErrIndex) 
                        {
                            m_Syntax = null; // convert it to relative
                            e = null;
                            m_Flags &= Flags.UserEscaped; // the only flag that makes sense for a relative uri 
                        }
                    } 
                    else // e == null 
                    {
                        if (err != ParsingError.None || InFact(Flags.ErrorOrParsingRecursion)) 
                        {
                            // User parser took over on an invalid Uri
                            SetUserDrivenParsing();
                        } 
                        else if (uriKind == UriKind.Relative)
                        { 
                            // Here we know that custom parser can create an absolute Uri, but the user has requested only a relative one 
                            e = GetException(ParsingError.CannotCreateRelative);
                        } 

                        if (m_iriParsing && hasUnicode){
                            // In this scenario we need to parse the whole string
                            EnsureParseRemaining(); 
                        }
 
                    } 
                    // will return from here
                } 
            }
            else if (err != ParsingError.None && uriKind != UriKind.Absolute && err <= ParsingError.LastRelativeUriOkErrIndex)
            {
                e = null; 
                m_Flags &= (Flags.UserEscaped|Flags.HasUnicode); // the only flags that makes sense for a relative uri
                if (m_iriParsing && hasUnicode){ 
                    // Iri'ze and then normalize relative uris 
                    m_String = EscapeUnescapeIri(m_originalUnicodeString, 0, m_originalUnicodeString.Length,
                                                (UriComponents)0); 
                    try{
                        m_String = m_String.Normalize(NormalizationForm.FormC);
                    }
                    catch (ArgumentException){ 
                        e = GetException(ParsingError.BadFormat);
                    } 
                } 
            }
            else 
            {
               m_String = null; // make it be invalid Uri
               e = GetException(err);
            } 
        }
 
 
        //
        // Checks if there are any unicode or escaped chars or ace to determine whether to load 
        // config
        //
        private unsafe bool CheckForConfigLoad(String data)
        { 
            bool initConfig = false;
            int length = data.Length; 
 
            fixed (char* temp = data){
                for (int i = 0; i < length; ++i){ 

                    if ((temp[i] > '\x7f') || (temp[i] == '%') ||
                        ((temp[i] == 'x') && ((i + 3) < length) && (temp[i + 1] == 'n') && (temp[i + 2] == '-') && (temp[i + 3] == '-')))
                    { 

                        // Unicode or maybe ace 
                        initConfig = true; 
                        break;
                    } 
                }


            } 

            return initConfig; 
        } 

        // 
        // Unescapes entire string and checks if it has unicode chars
        //
        private unsafe bool CheckForUnicode(String data)
        { 
            bool hasUnicode = false;
            char[] chars = new char[data.Length]; 
            int count = 0; 

            chars = UnescapeString(data, 0, data.Length, chars, ref count, c_DummyChar, 
                                   c_DummyChar, c_DummyChar, UnescapeMode.Unescape | UnescapeMode.UnescapeAll,
                                   null, false, false);

            String tempStr = new string(chars, 0, count); 
            int length = tempStr.Length;
 
            fixed (char* tempPtr = tempStr){ 
                for (int i = 0; i < length; ++i){
                    if (tempPtr[i] > '\x7f'){ 
                        // Unicode
                        hasUnicode = true;
                        break;
                    } 
                }
            } 
            return hasUnicode; 

        } 

        //
        // Check if the char (potentially surrogate) at given offset is in the iri range
        // Takes in isQuery because because iri restrictions for query are different 
        //
        internal static bool CheckIriUnicodeRange(string uri, int offset, ref bool surrogatePair, bool isQuery) 
        { 
            char invalidLowSurr = '\uFFFF';
            return CheckIriUnicodeRange(uri[offset],(offset + 1 < uri.Length) ? uri[offset + 1] : invalidLowSurr, 
                                        ref surrogatePair, isQuery);
        }

        // 
        // Checks if provided non surrogate char lies in iri range
        // 
        internal static bool CheckIriUnicodeRange(char unicode, bool isQuery) 
        {
            if ((unicode >= '\u00A0' && unicode <= '\uD7FF') || 
               (unicode >= '\uF900' && unicode <= '\uFDCF') ||
               (unicode >= '\uFDF0' && unicode <= '\uFFEF') ||
               (isQuery && unicode >= '\uE000' && unicode <= '\uF8FF')){
                return true; 
            }else{
                return false; 
            } 
        }
 
        //
        // Check if the highSurr is in the iri range or if highSurr and lowSurr are a surr pair then
        // it checks if the combined char is in the range
        // Takes in isQuery because because iri restrictions for query are different 
        //
        internal static bool CheckIriUnicodeRange(char highSurr, char lowSurr, ref bool surrogatePair, bool isQuery) 
        { 
            bool inRange = false;
            surrogatePair = false; 

            if (CheckIriUnicodeRange(highSurr, isQuery)){
                inRange = true;
            } 
            else if (Char.IsHighSurrogate(highSurr)){
                if (Char.IsSurrogatePair(highSurr, lowSurr)){ 
                    surrogatePair = true; 
                    char[] chars = new char[2] { highSurr, lowSurr };
                    string surrPair = new string(chars); 
                    if (((surrPair.CompareTo("\U00010000") >= 0) && (surrPair.CompareTo("\U0001FFFD") <= 0)) ||
                        ((surrPair.CompareTo("\U00020000") >= 0) && (surrPair.CompareTo("\U0002FFFD") <= 0)) ||
                        ((surrPair.CompareTo("\U00030000") >= 0) && (surrPair.CompareTo("\U0003FFFD") <= 0)) ||
                        ((surrPair.CompareTo("\U00040000") >= 0) && (surrPair.CompareTo("\U0004FFFD") <= 0)) || 
                        ((surrPair.CompareTo("\U00050000") >= 0) && (surrPair.CompareTo("\U0005FFFD") <= 0)) ||
                        ((surrPair.CompareTo("\U00060000") >= 0) && (surrPair.CompareTo("\U0006FFFD") <= 0)) || 
                        ((surrPair.CompareTo("\U00070000") >= 0) && (surrPair.CompareTo("\U0007FFFD") <= 0)) || 
                        ((surrPair.CompareTo("\U00080000") >= 0) && (surrPair.CompareTo("\U0008FFFD") <= 0)) ||
                        ((surrPair.CompareTo("\U00090000") >= 0) && (surrPair.CompareTo("\U0009FFFD") <= 0)) || 
                        ((surrPair.CompareTo("\U000A0000") >= 0) && (surrPair.CompareTo("\U000AFFFD") <= 0)) ||
                        ((surrPair.CompareTo("\U000B0000") >= 0) && (surrPair.CompareTo("\U000BFFFD") <= 0)) ||
                        ((surrPair.CompareTo("\U000C0000") >= 0) && (surrPair.CompareTo("\U000CFFFD") <= 0)) ||
                        ((surrPair.CompareTo("\U000D0000") >= 0) && (surrPair.CompareTo("\U000DFFFD") <= 0)) || 
                        ((surrPair.CompareTo("\U000E0000") >= 0) && (surrPair.CompareTo("\U000EFFFD") <= 0)) ||
                        (isQuery && (((surrPair.CompareTo("\U000F0000") >= 0) && (surrPair.CompareTo("\U000FFFFD") <= 0)) || 
                        ((surrPair.CompareTo("\U00100000") >= 0) && (surrPair.CompareTo("\U0010FFFD") <= 0))))) 
                        inRange = true;
                } 
            }

            return inRange;
        } 
        //
        // 
        //  Returns true if the string represents a valid argument to the Uri ctor 
        //  If uriKind != AbsoluteUri then certain parsing erros are ignored but Uri usage is limited
        // 
        public static bool TryCreate(string uriString, UriKind uriKind, out Uri result)
        {
            if ((object)uriString == null)
            { 
                result = null;
                return false; 
            } 
            UriFormatException e = null;
            result = CreateHelper(uriString, false, uriKind, ref e); 
            return (object) e == null && result != null;
        }
        //
        public static bool TryCreate(Uri baseUri, string relativeUri, out Uri result) 
        {
            Uri relativeLink; 
            if (TryCreate(relativeUri, UriKind.RelativeOrAbsolute, out relativeLink)) 
            {
                if (!relativeLink.IsAbsoluteUri) 
                    return TryCreate(baseUri, relativeLink, out result);

                result = relativeLink;
                return true; 
            }
            result = null; 
            return false; 
        }
        // 
        public static bool TryCreate(Uri baseUri, Uri relativeUri, out Uri result)
        {
            result = null;
 
            //Consider: Work out the baseUri==null case
            if ((object)baseUri == null) 
                return false; 

            if (baseUri.IsNotAbsoluteUri) 
                return false;

            UriFormatException e;
            string newUriString = null; 

            bool dontEscape; 
            if (baseUri.Syntax.IsSimple) 
            {
                dontEscape = relativeUri.UserEscaped; 
                result = ResolveHelper(baseUri, relativeUri, ref newUriString, ref dontEscape, out e);
            }
            else
            { 
                dontEscape = false;
                newUriString = baseUri.Syntax.InternalResolve(baseUri, relativeUri, out e); 
            } 

            if (e != null) 
                return false;

            if ((object) result == null)
                result = CreateHelper(newUriString, dontEscape, UriKind.Absolute, ref e); 

            return (object) e == null && result != null && result.IsAbsoluteUri; 
        } 
        //
        // 
        public bool IsBaseOf(Uri uri)
        {
            if (!IsAbsoluteUri)
                return false; 

            if (Syntax.IsSimple) 
                return IsBaseOfHelper(uri); 

            return Syntax.InternalIsBaseOf(this, uri); 
        }
        //
        //
        public string GetComponents(UriComponents components, UriFormat format) 
        {
            if (((components & UriComponents.SerializationInfoString) != 0) && components != UriComponents.SerializationInfoString) 
                throw new ArgumentOutOfRangeException("UriComponents.SerializationInfoString"); 

            if ((format & ~UriFormat.SafeUnescaped) != 0) 
                throw new ArgumentOutOfRangeException("format");

            if (IsNotAbsoluteUri)
            { 
                if (components == UriComponents.SerializationInfoString)
                    return GetRelativeSerializationString(format); 
                else 
                    throw new InvalidOperationException(SR.GetString(SR.net_uri_NotAbsolute));
            } 

            if (Syntax.IsSimple)
                return GetComponentsHelper(components, format);
 
            return Syntax.InternalGetComponents(this, components, format);
        } 
        // 
        public bool IsWellFormedOriginalString()
        { 
            if (IsNotAbsoluteUri || Syntax.IsSimple)
                return InternalIsWellFormedOriginalString();

            return Syntax.InternalIsWellFormedOriginalString(this); 
        }
        // Consider: (perf) Making it to not create a Uri internally 
        public static bool IsWellFormedUriString(string uriString, UriKind uriKind) 
        {
          Uri result; 

          if (!Uri.TryCreate(uriString, uriKind, out result))
              return false;
 
          return result.IsWellFormedOriginalString();
        } 
        // 
        //
        // This is for languages that do not support == != operators overloading 
        //
        // Note that Uri.Equals will get an optimized path but is limited to true/fasle result only
        //
        public static int Compare(Uri uri1, Uri uri2, UriComponents partsToCompare, UriFormat compareFormat, StringComparison comparisonType) 
        {
 
            if ((object) uri1 == null) 
            {
                if (uri2 == null) 
                    return 0; // Equal
                return -1;    // null < non-null
            }
            if ((object) uri2 == null) 
                return 1;     // non-null > null
 
            // a relative uri is always less than an absolute one 
            if (!uri1.IsAbsoluteUri || !uri2.IsAbsoluteUri)
                return uri1.IsAbsoluteUri? 1: uri2.IsAbsoluteUri? -1: string.Compare(uri1.OriginalString, uri2.OriginalString, comparisonType); 

            return string.Compare(
                                    uri1.GetParts(partsToCompare, compareFormat),
                                    uri2.GetParts(partsToCompare, compareFormat), 
                                    comparisonType
                                  ); 
        } 
        //
        // 
        //
        public static string UnescapeDataString(string stringToUnescape)
        {
            if ((object) stringToUnescape == null) 
                throw new ArgumentNullException("stringToUnescape");
 
            if (stringToUnescape.Length == 0) 
                return string.Empty;
 
            unsafe {
                fixed (char* pStr = stringToUnescape)
                {
                    int position; 
                    for (position = 0; position < stringToUnescape.Length; ++position)
                        if (pStr[position] == '%') 
                            break; 

                    if (position == stringToUnescape.Length) 
                        return stringToUnescape;

                    position = 0;
                    char[] dest = new char[stringToUnescape.Length]; 
                    dest = UnescapeString(stringToUnescape, 0, stringToUnescape.Length, dest, ref position, c_DummyChar, c_DummyChar, c_DummyChar,
                        UnescapeMode.Unescape | UnescapeMode.UnescapeAllOrThrow, null, false, true); 
                    return new string(dest, 0, position); 
                }
            } 
        }
        //
        // Where stringToEscape is intented to be a completely unescaped URI string.
        // This method will escape any character that is not a reserved or unreserved character, including percent signs. 
        // Note that EscapeUriString will also do not escape a '#' sign.
        // 
        public static string EscapeUriString(string stringToEscape) 
        {
            if ((object)stringToEscape == null) 
                throw new ArgumentNullException("stringToUnescape");

            if (stringToEscape.Length == 0)
                return string.Empty; 

            int position = 0; 
            char[] dest = EscapeString(stringToEscape, 0, stringToEscape.Length, null, ref position, true, c_DummyChar, c_DummyChar, c_DummyChar); 
            if ((object) dest == null)
                return stringToEscape; 
            return new string(dest, 0, position);
        }
        //
        // Where stringToEscape is intended to be URI data, but not an entire URI. 
        // This method will escape any character that is not an unreserved character, including percent signs.
        // 
        public static string EscapeDataString(string stringToEscape) 
        {
            if ((object) stringToEscape == null) 
                throw new ArgumentNullException("stringToUnescape");

            if (stringToEscape.Length == 0)
                return string.Empty; 

            int position = 0; 
            char[] dest = EscapeString(stringToEscape, 0, stringToEscape.Length, null, ref position, false, c_DummyChar, c_DummyChar, c_DummyChar); 
            if (dest == null)
                return stringToEscape; 
            return new string(dest, 0, position);
        }

        // - forceX characters are always escaped if found 
        // - rsvd character will remain unescaped
        // 
        // start    - starting offset from input 
        // end      - the exclusive ending offset in input
        // destPos  - starting offset in dest for output, on return this will be an exclusive "end" in the output. 
        //
        // In case "dest" has lack of space it will be reallocated by preserving the _whole_ content up to current destPos
        //
        // Returns null if nothing has to be escaped AND passed dest was null, otherwise the resulting array with the updated destPos 
        //
        const short c_MaxAsciiCharsReallocate   = 40; 
        const short c_MaxUnicodeCharsReallocate = 40; 
        const short c_MaxUTF_8BytesPerUnicodeChar  = 4;
        const short c_EncodedCharsPerByte = 3; 
        private unsafe static char[] EscapeString(string input, int start, int end, char[] dest, ref int destPos, bool isUriString, char force1, char force2, char rsvd)
        {
            if (end - start >= c_MaxUriBufferSize)
                throw GetException(ParsingError.SizeLimit); 

            int i = start; 
            int prevInputPos = start; 
            byte *bytes = stackalloc byte[c_MaxUnicodeCharsReallocate*c_MaxUTF_8BytesPerUnicodeChar];   // 40*4=160
 
            fixed (char* pStr = input)
            {
                for(; i < end; ++i)
                { 
                    char ch = pStr[i];
 
                    // a Unicode ? 
                    if (ch  > '\x7F')
                    { 
                        short maxSize = (short)Math.Min(end - i, (int)c_MaxUnicodeCharsReallocate-1);

                        short count = 1;
                        for (; count < maxSize && pStr[i + count] > '\x7f'; ++count) 
                            ;
 
                        // Is the last a high surrogate? 
                        if (pStr[i + count-1] >= 0xD800 && pStr[i + count-1] <= 0xDBFF)
                        { 
                            // Should be a rare case where the app tries to feed an invalid Unicode surrogates pair
                           if (count == 1 || count == end - i)
                                throw new UriFormatException(SR.GetString(SR.net_uri_BadString));
                           // need to grab one more char as a Surrogate except when it's a bogus input 
                           ++count;
                        } 
 
                        dest = EnsureDestinationSize(pStr, dest, i, (short)(count*c_MaxUTF_8BytesPerUnicodeChar*c_EncodedCharsPerByte),
                                                     c_MaxUnicodeCharsReallocate*c_MaxUTF_8BytesPerUnicodeChar*c_EncodedCharsPerByte, 
                                                     ref destPos, prevInputPos);

                        short numberOfBytes = (short)Encoding.UTF8.GetBytes(pStr+i, count, bytes, c_MaxUnicodeCharsReallocate*c_MaxUTF_8BytesPerUnicodeChar);
 
                        // This is the only exception that built in UriParser can throw after a Uri ctor.
                        // Should not happen unless the app tries to feed an invalid Unicode String 
                        if (numberOfBytes == 0) 
                            throw new UriFormatException(SR.GetString(SR.net_uri_BadString));
 
                        i += (count-1);

                        for (count = 0 ; count < numberOfBytes; ++count)
                            EscapeAsciiChar((char)bytes[count], dest, ref destPos); 

                        prevInputPos = i+1; 
                    } 
                    else if (ch == '%' && rsvd == '%')
                    { 
                        // Means we don't reEncode '%' but check for the possible escaped sequence
                        dest = EnsureDestinationSize(pStr, dest, i, c_EncodedCharsPerByte, c_MaxAsciiCharsReallocate*c_EncodedCharsPerByte, ref destPos, prevInputPos);
                        if(i + 2 < end && EscapedAscii(pStr[i+1], pStr[i+2]) != c_DummyChar)
                        { 
                            // leave it escaped
                            dest[destPos++] = '%'; 
                            dest[destPos++] = pStr[i+1]; 
                            dest[destPos++] = pStr[i+2];
                            i += 2; 
                        }
                        else
                        {
                            EscapeAsciiChar('%', dest, ref destPos); 
                        }
                        prevInputPos = i+1; 
                    } 
                    else if (ch == force1 ||  ch == force2)
                    { 
                        dest = EnsureDestinationSize(pStr, dest, i, c_EncodedCharsPerByte, c_MaxAsciiCharsReallocate*c_EncodedCharsPerByte, ref destPos, prevInputPos);
                        EscapeAsciiChar(ch, dest, ref destPos);
                        prevInputPos = i+1;
                    } 
                    else if (ch != rsvd && (isUriString? IsNotReservedNotUnreservedNotHash(ch): IsNotUnreserved(ch)))
                    { 
                        dest = EnsureDestinationSize(pStr, dest, i, c_EncodedCharsPerByte, c_MaxAsciiCharsReallocate*c_EncodedCharsPerByte, ref destPos, prevInputPos); 
                        EscapeAsciiChar(ch, dest, ref destPos);
                        prevInputPos = i+1; 
                    }
                }

                if (prevInputPos != i) 
                {
                    // need to fill up the dest array ? 
                    if (prevInputPos != start || dest != null) 
                        dest = EnsureDestinationSize(pStr, dest, i, 0, 0, ref destPos, prevInputPos);
                } 
            }

            return dest;
        } 

        // 
        // ensure destination array has enough space and contains all the needed input stuff 
        //
        private unsafe static char[] EnsureDestinationSize(char *pStr, char[] dest, int currentInputPos, short charsToAdd, short minReallocateChars, ref int destPos, int prevInputPos) 
        {
            if ((object) dest == null || dest.Length  < destPos + (currentInputPos-prevInputPos) + charsToAdd)
            {
                // allocating or reallocating array by ensuring enough space based on maxCharsToAdd. 
                char[] newresult = new char[destPos + (currentInputPos-prevInputPos) + minReallocateChars];
 
                if ((object) dest != null && destPos != 0) 
                    Buffer.BlockCopy(dest, 0, newresult, 0, destPos<<1);
                dest = newresult; 
            }

            // ensuring we copied everything form the input string left before last escaping
            while (prevInputPos != currentInputPos) 
                dest[destPos++] = pStr[prevInputPos++];
            return dest; 
        } 
        //
        // mark        = "-" | "_" | "." | "!" | "~" | "*" | "'" | "(" | ")" 
        // reserved    = ";" | "/" | "?" | ":" | "@" | "&" | "=" | "+" | "$" | ","
        // excluded = control | space | delims | unwise
        // delims      = "<" | ">" | "#" | "%" | <">
        // unwise      = "{" | "}" | "|" | "\" | "^" | "[" | "]" | "`" 
        //
        private static unsafe bool IsNotReservedNotUnreservedNotHash(char c) 
        { 
            if (c > 'z' && c != '~')
            { 
                return true;
            }
            else if (c > 'Z' && c < 'a' && c != '_')
            { 
                return true;
            } 
            else if (c < '!') 
            {
                return true; 
            }
            else if (c == '>' || c == '<' || c == '%' || c == '"' || c == '`')
            {
                return true; 
            }
            return false; 
        } 
        //
        private static unsafe bool IsNotUnreserved(char c) 
        {
            if (c > 'z' && c != '~')
            {
                return true; 
            }
            else if ((c > '9' && c < 'A') || (c > 'Z' && c < 'a' && c != '_')) 
            { 
                return true;
            } 
            else if (c < '\'' && c != '!')
            {
                return true;
            } 
            else if (c == '+' || c == ',' || c == '/')
            { 
                return true; 
            }
            return false; 
        }

        //
        // This method will assume that any good Escaped Sequence will be unescaped in the output 
        // - Assumes Dest.Length - detPosition >= end-start
        // - UnescapeLevel controls various modes of opearion 
        // - Any "bad" escape sequence will remain as is or '%' will be escaped. 
        // - destPosition tells the starting index in dest for placing the result.
        //   On return destPosition tells the last character + 1 postion in the "dest" array. 
        // - The control chars and chars passed in rsdvX parameters may be re-escaped depending on UnescapeLevel
        // - It is a RARE case when Unescape actually needs escaping some characteres mentioned above.
        //   For this reason it returns a char[] that is usually the same ref as the input "dest" value.
        // 
        [Flags]
        private enum UnescapeMode 
        { 
            CopyOnly                = 0x0,                  // used for V1.0 ToString() compatibility mode only
            Escape                  = 0x1,                  // Only used by ImplicitFile, the string is already fully unescaped 
            Unescape                = 0x2,                  // Only used as V1.0 UserEscaped compatibility mode
            EscapeUnescape          = Unescape|Escape,      // does both escaping control+reserved and unescaping of safe characters
            V1ToStringFlag          = 0x4,                  // Only used as V1.0 ToString() compatibility mode, assumes DontEscape level also
            UnescapeAll             = 0x8,                  // just unescape everything, leave bad escaped sequences as is (should throw but can't due to v1.0 compat) 
            UnescapeAllOrThrow      = 0x10|UnescapeAll,     // just unescape everything plus throw on bad escaped sequences
        } 
        private unsafe static char[] UnescapeString(   string input, int start, int end, char[] dest, ref int destPosition, 
                                                        char rsvd1, char rsvd2, char rsvd3, UnescapeMode unescapeMode,
                                                        UriParser syntax, bool isQuery, bool readOnlyConfig) 
        {
            fixed (char *pStr = input)
            {
                return UnescapeString(  pStr, start, end, dest, ref destPosition, 
                                        rsvd1, rsvd2, rsvd3, unescapeMode, syntax, isQuery, readOnlyConfig);
            } 
        } 
        private unsafe static char[] UnescapeString(char *pStr, int start, int end, char[] dest, ref int destPosition,
            char rsvd1, char rsvd2, char rsvd3, UnescapeMode unescapeMode, UriParser syntax, bool isQuery, bool readOnlyConfig) 
        {
            byte [] bytes = null;
            byte escapedReallocations = 0;
            bool escapeReserved = false; 
            int next = start;
            bool iriParsing = s_IriParsing && ((readOnlyConfig) || (!readOnlyConfig && IriParsingStatic(syntax))) 
                                && ((unescapeMode & UnescapeMode.EscapeUnescape) == UnescapeMode.EscapeUnescape); 

            while (true) 
            {
                // we may need to re-pin dest[]
                fixed (char* pDest = dest)
                { 
                    if ((unescapeMode & UnescapeMode.EscapeUnescape) == UnescapeMode.CopyOnly)
                    { 
                        while (start < end) 
                            pDest[destPosition++] = pStr[start++];
                        return dest; 
                    }

                    while (true)
                    { 
                        char ch = (char)0;
 
                        for (;next < end; ++next) 
                        {
                            if ((ch = pStr[next]) == '%') 
                            {
                                if ((unescapeMode & UnescapeMode.Unescape) == 0)
                                {
                                    // re-escape, don't check anything else 
                                    escapeReserved = true;
                                } 
                                else if (next+2 < end) 
                                {
                                    ch = EscapedAscii(pStr[next+1], pStr[next+2]); 
                                    // Unescape a good sequence if full unescape is requested
                                    if (unescapeMode >= UnescapeMode.UnescapeAll)
                                    {
                                        if (ch == c_DummyChar) 
                                        {
                                            if (unescapeMode >= UnescapeMode.UnescapeAllOrThrow) 
                                            { 
                                                // Should be a rare case where the app tries to feed an invalid escaped sequence
                                                throw new UriFormatException(SR.GetString(SR.net_uri_BadString)); 
                                            }
                                            continue;
                                        }
                                    } 
                                    // re-escape % from an invalid sequence
                                    else if (ch == c_DummyChar) 
                                    { 
                                        if ((unescapeMode & UnescapeMode.Escape) != 0)
                                            escapeReserved = true; 
                                        else
                                            continue;   // we should throw instead but since v1.0 woudl just print '%'
                                    }
                                    // Do not unescape '%' itself unless full unescape is requested 
                                    else if (ch == '%')
                                    { 
                                        next += 2; 
                                        continue;
                                    } 
                                    // Do not unescape a reserved char unless full unescape is requested
                                    else if (ch == rsvd1 || ch == rsvd2 || ch == rsvd3)
                                    {
                                        next += 2; 
                                        continue;
                                    } 
                                    // Do not unescape a dangerous char unless it's V1ToStringFlags mode 
                                    else if ((unescapeMode & UnescapeMode.V1ToStringFlag) == 0 && IsNotSafeForUnescape(ch))
                                    { 
                                        next += 2;
                                        continue;
                                    }
                                    else if (iriParsing && ((ch <='\x9F' && IsNotSafeForUnescape(ch)) || 
                                                            (ch >'\x9F' &&!CheckIriUnicodeRange(ch, isQuery))))
                                    { 
                                        // check if unenscaping gives a char ouside iri range 
                                        // if it does then keep it escaped
                                        next += 2; 
                                        continue;
                                    }
                                    // unescape escaped char or escape %
                                    break; 
                                }
                                else if (unescapeMode >= UnescapeMode.UnescapeAll) 
                                { 
                                    if (unescapeMode >= UnescapeMode.UnescapeAllOrThrow)
                                    { 
                                        // Should be a rare case where the app tries to feed an invalid escaped sequence
                                        throw new UriFormatException(SR.GetString(SR.net_uri_BadString));
                                    }
                                    // keep a '%' as part of a bogus sequence (we should throw but this is how 1.0 has shipped) 
                                    continue;
                                } 
                                else 
                                {
                                    escapeReserved = true; 
                                }
                                // escape (escapeReserved==ture) or otheriwse unescape the sequence
                                break;
                            } 
                            else if ((unescapeMode & (UnescapeMode.Unescape | UnescapeMode.UnescapeAll)) == (UnescapeMode.Unescape | UnescapeMode.UnescapeAll))
                            { 
                                continue; 
                            }
                            else if ((unescapeMode & UnescapeMode.Escape) != 0) 
                            {
                                 // Could actually escape some of the characters
                                 if (ch == rsvd1 || ch == rsvd2 || ch == rsvd3)
                                 { 
                                     // found an unescaped reserved character -> escape it
                                     escapeReserved = true; 
                                     break; 
                                 }
                                 else if ((unescapeMode & UnescapeMode.V1ToStringFlag) == 0 && (ch <= '\x1F' || (ch >= '\x7F' && ch <= '\x9F'))) 
                                 {
                                     // found an unescaped reserved character -> escape it
                                     escapeReserved = true;
                                     break; 
                                 }
                            } 
                        } 

                        //copy off previous characters from input 
                        while (start < next)
                            pDest[destPosition++] = pStr[start++];

                        if (next != end) 
                        {
                            //VsWhidbey#87423 
                            if (escapeReserved) 
                            {
                                //escape that char 
                                // Since this should be _really_ rare case, reallocate with constant size increase of 30 rsvd-type characters.
                                if (escapedReallocations == 0)
                                {
                                    escapedReallocations = 30; 
                                    char[] newDest = new char[dest.Length + escapedReallocations*3];
                                    fixed (char *pNewDest = newDest) 
                                    { 
                                        for (int i = 0; i < destPosition; ++i)
                                            pNewDest[i] = pDest[i]; 
                                    }
                                    dest = newDest;
                                    // re-pin new dest[] array
                                    goto dest_fixed_loop_break; 
                                }
                                else 
                                { 
                                    --escapedReallocations;
                                    EscapeAsciiChar(pStr[next], dest, ref destPosition); 
                                    escapeReserved = false;
                                    start = ++next;
                                    continue;
                                } 
                            }
 
                            // unescaping either one Ascii or possibly multiple Unicode 

                            if (ch <= '\x7F') 
                            {
                                //ASCII
                                dest[destPosition++] = ch;
                                next+=3; 
                                start = next;
                                continue; 
                            } 

                            // Unicode 

                            int byteCount = 1;
                            // lazy initialization of max size, will reuse the array for next sequences
                            if ((object) bytes == null) 
                                bytes = new byte[end - next];
 
                            bytes[0] = (byte)ch; 
                            next+=3;
                            while (next < end) 
                            {
                                // Check on exit criterion
                                if ((ch = pStr[next]) != '%' || next+2 >= end)
                                    break; 

                                // already made sure we have 3 characters in str 
                                ch = EscapedAscii(pStr[next+1], pStr[next+2]); 

                                //invalid hex sequence ? 
                                if (ch == c_DummyChar)
                                    break;
                                // character is not part of a UTF-8 sequence ?
                                else if (ch < '\x80') 
                                    break;
                                else 
                                { 
                                    //a UTF-8 sequence
                                    bytes[byteCount++] = (byte)ch; 
                                    next += 3;
                                }
                            }
                            Encoding noFallbackCharUTF8 = Encoding.GetEncoding("utf-8", new EncoderReplacementFallback(""), new DecoderReplacementFallback("")); 
                            char[] unescapedChars = new char[bytes.Length];
                            int charCount = noFallbackCharUTF8.GetChars(bytes, 0, byteCount, unescapedChars, 0); 
 
                            if (charCount != 0)
                            { 
                                start = next;

                                // match exact bytes
                                // Do not unescape chars not allowed by Iri 
                                // need to check for invalid utf sequences that may not have given any chars
 
                                MatchUTF8Sequence(pDest, dest, ref destPosition, unescapedChars, charCount, bytes, isQuery, iriParsing); 
                            }
                            else 
                            {
                                // the encoded, high-ANSI characters are not UTF-8 encoded
                                // [....] codereview: a bug?
                                // If we expect UTF-8 then we should discard (not copy) anything else. 
                                // The reason is that >0x7F Ansi and UTF-8 cannot be supported at the same time
 
                                if (unescapeMode >= UnescapeMode.UnescapeAllOrThrow) 
                                {
                                    // Should be a rare case where the app tries to feed an invalid escaped sequence 
                                    throw new UriFormatException(SR.GetString(SR.net_uri_BadString));
                                }

                                next = start + 3; 
                                start = next;
                                dest[destPosition++]= (char)bytes[0]; 
                            } 
                        }
 
                        if (next == end)
                            goto done;
                    }
dest_fixed_loop_break: ; 
                }
            } 
 
done:       return dest;
        } 

        //
        // Need to check for invalid utf sequences that may not have given any chars.
        // We got the unescaped chars, we then reencode them and match off the bytes 
        // to get the invalid sequence bytes that we just copy off
        // 
        private static unsafe void MatchUTF8Sequence(char* pDest, char[] dest, ref int destOffset, char[] unescapedChars, 
                                                    int charCount, byte[] bytes, bool isQuery, bool iriParsing)
        { 
            int count = 0;
            fixed (char* unescapedCharsPtr = unescapedChars){
                for (int j = 0; j < charCount; ++j){
                    bool isHighSurr = Char.IsHighSurrogate(unescapedCharsPtr[j]); 
                    byte[] encodedBytes = Encoding.UTF8.GetBytes(unescapedChars, j, isHighSurr ? 2 : 1);
                    int encodedBytesLength = encodedBytes.Length; 
 
                    // we have to keep unicode chars outside Iri range escaped
                    bool inIriRange = false; 
                    if (iriParsing){
                        if (!isHighSurr)
                            inIriRange = CheckIriUnicodeRange(unescapedChars[j], isQuery);
                        else{ 
                            bool surrPair = false;
                            inIriRange = CheckIriUnicodeRange(unescapedChars[j], unescapedChars[j + 1], 
                                                                   ref surrPair, isQuery); 
                        }
                    } 

                    while (true){
                        while (bytes[count] != encodedBytes[0]){
                            EscapeAsciiChar((char)bytes[count++], dest, ref destOffset); 
                        }
 
                        // check if all bytes match 
                        bool allBytesMatch = true;
                        int k = 0; 
                        for (; k < encodedBytesLength; ++k){
                            if (bytes[count + k] != encodedBytes[k]){
                                allBytesMatch = false;
                                break; 
                            }
                        } 
 
                        if (allBytesMatch){
                            count += encodedBytesLength; 
                            if (iriParsing){
                                if (!inIriRange){
                                    // need to keep chars not allowed as escaped
                                    for (int l = 0; l < encodedBytes.Length; ++l){ 
                                        EscapeAsciiChar((char)encodedBytes[l], dest, ref destOffset);
                                    } 
                                } 
                                else if (!IsBidiControlCharacter(unescapedCharsPtr[j])){
                                    //copy chars 
                                    pDest[destOffset++] = unescapedCharsPtr[j];
                                }
                                if (isHighSurr)
                                    pDest[destOffset++] = unescapedCharsPtr[j + 1]; 
                            }
                            else{ 
                                //copy chars 
                                pDest[destOffset++] = unescapedCharsPtr[j];
 
                                if (isHighSurr)
                                    pDest[destOffset++] = unescapedCharsPtr[j + 1];
                            }
                                break; // break out of while (true) since we've matched this char bytes 
                        }
                        else{ 
                            // copy bytes till place where bytes dont match 
                            for (int l = 0; l < k; ++l){
                                EscapeAsciiChar((char)bytes[count++], dest, ref destOffset); 
                            }
                        }
                    }
 
                    if (isHighSurr) j++;
 
                } 
            }
        } 

        // for rfc 3987
        internal bool CheckIsReserved(char ch)
        { 
            char[] rsvd = new char[]{':', '/', '?', '#', '[', ']', '@'}; //gen-delims
            for (int i = 0; i < rsvd.Length; ++i){ 
                if (rsvd[i] == ch) return true; 
            }
            return false; 
        }

        //
        // Check reserved chars according to rfc 3987 in a sepecific component 
        //
        internal bool CheckIsReserved(char ch, UriComponents component) 
        { 
            if ((component != UriComponents.Scheme) ||
                    (component != UriComponents.UserInfo) || 
                    (component != UriComponents.Host) ||
                    (component != UriComponents.Port) ||
                    (component != UriComponents.Path) ||
                    (component != UriComponents.Query) || 
                    (component != UriComponents.Fragment)
                ) 
                return (component == (UriComponents)0)? CheckIsReserved(ch): false; 
            else
            { 
                switch(component)
                {
                    // Reserved chars according to rfc 3987
                    case UriComponents.UserInfo: 
                        if( ch == '/' || ch == '?' || ch == '#' || ch == '[' || ch == ']' || ch == '@' )
                            return true; 
                        break; 
                    case UriComponents.Host:
                        if( ch == ':' || ch == '/' || ch == '?' || ch == '#' || ch == '[' || ch == ']' || ch == '@' ) 
                            return true;
                        break;
                    case UriComponents.Path:
                        if( ch == '/' || ch == '?' || ch == '#' || ch == '[' || ch == ']' ) 
                            return true;
                        break; 
                    case UriComponents.Query: 
                        if(ch == '#' || ch == '[' || ch == ']')
                            return true; 
                        break;
                    case UriComponents.Fragment:
                        if(ch == '#' || ch == '[' || ch == ']')
                            return true; 
                        break;
                    default: 
                        break; 
                }
                return false; 
            }
        }

        // 
        // Cleans up the specified component according to Iri rules
        // a) Chars allowed by iri in a component are unescaped if found escaped 
        // b) Bidi chars are stripped 
        //
        // should be called only if IRI parsing is switched on 
        internal unsafe string EscapeUnescapeIri(string input, int start, int end, UriComponents component)
        {
            fixed (char *pInput = input)
            { 
                return EscapeUnescapeIri(pInput, start, end, component);
            } 
        } 

        // 
        // See above explanation
        //
        internal unsafe string EscapeUnescapeIri(char* pInput, int start, int end, UriComponents component)
        { 

            char [] dest = new char[ end - start ]; 
            byte[] bytes = null; 

            // Pin the array to do pointer accesses 
            GCHandle destHandle = GCHandle.Alloc(dest, GCHandleType.Pinned);
            char* pDest = (char*)destHandle.AddrOfPinnedObject();

            byte escapedReallocations = 0; 
            int next = start;
            int destOffset = 0; 
            char ch; 
            bool escape = false;
            bool surrogatePair = false; 
            bool isUnicode = false;

            for (;next < end; ++next)
            { 
                escape = false;
                surrogatePair = false; 
                isUnicode = false; 

                if ((ch = pInput[next]) == '%'){ 
                    if (next + 2 < end){
                        ch = EscapedAscii(pInput[next + 1], pInput[next + 2]);
                        // Do not unescape a reserved char
                        if (ch == c_DummyChar || ch == '%' || CheckIsReserved(ch, component) || IsNotSafeForUnescape(ch)){ 
                            // keep as is
                            pDest[destOffset++] = pInput[next++]; 
                            pDest[destOffset++] = pInput[next++]; 
                            pDest[destOffset++] = pInput[next];
                            continue; 
                        }
                        else if (ch <= '\x7F'){
                            //ASCII
                            pDest[destOffset++] = ch; 
                            next += 2;
                            continue; 
                        }else{ 
                            // possibly utf8 encoded sequence of unicode
 
                            // check if safe to unescape according to Iri rules

                            int startSeq = next;
                            int byteCount = 1; 
                            // lazy initialization of max size, will reuse the array for next sequences
                            if ((object)bytes == null) 
                                bytes = new byte[end - next]; 

                            bytes[0] = (byte)ch; 
                            next += 3;
                            while (next < end)
                            {
                                // Check on exit criterion 
                                if ((ch = pInput[next]) != '%' || next + 2 >= end)
                                    break; 
 
                                // already made sure we have 3 characters in str
                                ch = EscapedAscii(pInput[next + 1], pInput[next + 2]); 

                                //invalid hex sequence ?
                                if (ch == c_DummyChar)
                                    break; 
                                // character is not part of a UTF-8 sequence ?
                                else if (ch < '\x80') 
                                    break; 
                                else
                                { 
                                    //a UTF-8 sequence
                                    bytes[byteCount++] = (byte)ch;
                                    next += 3;
                                } 
                            }
                            next--; // for loop will increment 
 
                            Encoding noFallbackCharUTF8 = Encoding.GetEncoding("utf-8",  new EncoderReplacementFallback(""), new DecoderReplacementFallback(""));
                            char[] unescapedChars = new char[bytes.Length]; 
                            int charCount = noFallbackCharUTF8.GetChars(bytes, 0, byteCount, unescapedChars, 0);


                            if (charCount != 0){ 

                                // need to check for invalid utf sequences that may not have given any chars 
 
                                // check if unicode value is allowed
                                MatchUTF8Sequence(  pDest, dest, ref destOffset, unescapedChars, charCount, bytes, 
                                                    component == UriComponents.Query, true);
                            }
                            else
                            { 
                                // copy escaped sequence as is
                                for (int i = startSeq; i <= next; ++i) 
                                    pDest[destOffset++] = pInput[i]; 
                            }
 
                        }

                    }else{
                        pDest[destOffset++] = pInput[next]; 
                    }
                } 
                else if (ch > '\x7f'){ 
                    // unicode
 
                    char ch2;

                    if ((Char.IsHighSurrogate(ch)) && (next + 1 < end)){
                        ch2 = pInput[next + 1]; 
                        escape = !CheckIriUnicodeRange(ch, ch2, ref surrogatePair, component == UriComponents.Query);
                        if (!escape){ 
                            // copy the two chars 
                            pDest[destOffset++] = pInput[next++];
                            pDest[destOffset++] = pInput[next]; 
                        }else{
                            isUnicode = true;
                        }
                    }else{ 
                        if(CheckIriUnicodeRange(ch, component == UriComponents.Query)){
                            if (!IsBidiControlCharacter(ch)){ 
                                // copy it 
                                pDest[destOffset++] = pInput[next];
                            } 
                        }else{
                            // escape it
                            escape = true;
                            isUnicode = true; 
                        }
                    } 
                }else{ 
                    // just copy the character
                    pDest[destOffset++] = pInput[next]; 
                }

                if (escape){
                    if (escapedReallocations == 0){ 
                        // may need more memory since we didnt anticipate escaping
                        escapedReallocations = 30; 
 
                        char[] newDest = new char[dest.Length + escapedReallocations * 3];
                        fixed (char* pNewDest = newDest){ 
                            for (int i = 0; i < destOffset; ++i)
                                pNewDest[i] = pDest[i];
                        }
                        if (destHandle.IsAllocated) 
                            destHandle.Free();
                        dest = newDest; 
 
                        // re-pin new dest[] array
                        destHandle = GCHandle.Alloc(dest, GCHandleType.Pinned); 
                        pDest = (char*)destHandle.AddrOfPinnedObject();
                    }else{
                        if (isUnicode){
                            if (surrogatePair) 
                                escapedReallocations -= 4;
                            else 
                                escapedReallocations -= 3; 
                        }
                        else 
                            --escapedReallocations;
                    }

                    byte[] encodedBytes = new byte[4]; 
                    fixed (byte* pEncodedBytes = encodedBytes){
                        int encodedBytesCount = Encoding.UTF8.GetBytes(pInput + next, surrogatePair ? 2 : 1, pEncodedBytes, 4); 
 
                        for (int count = 0; count < encodedBytesCount; ++count)
                            EscapeAsciiChar((char)encodedBytes[count], dest, ref destOffset); 
                    }
                }
            }
 
            if (destHandle.IsAllocated)
                destHandle.Free(); 
            return new string(dest, 0 , destOffset ); 
        }
 
        //
        // Do not unescape these in safe mode:
        // 1)  reserved    = ";" | "/" | "?" | ":" | "@" | "&" | "=" | "+" | "$" | ","
        // 2)  excluded = control | "#" | "%" | "\" 
        //
        // That will still give plenty characters unescaped by SafeUnesced mode such as 
        // 1) Unicode characters 
        // 2) Unreserved = alphanum | "-" | "_" | "." | "!" | "~" | "*" | "'" | "(" | ")"
        // 3) DelimitersAndUnwise = "<" | ">" |  <"> | "{" | "}" | "|" | "^" | "[" | "]" | "`" 
        static bool IsNotSafeForUnescape(char ch)
        {
            if (ch <= '\x1F' || (ch >= '\x7F' && ch <= '\x9F'))
                return true; 
            else if ((ch >= ';' && ch <= '@' && (ch | '\x2') != '>') ||
                     (ch >= '#' && ch <= '&') || 
                     ch == '+' || ch == ',' || ch == '/' || ch == '\\') 
                return true;
 
            return false;
        }

 
        //
        // Internal stuff 
        // 

        // Returns false if OriginalString value 
        // (1) is not correctly escaped as per URI spec excluding intl UNC name case
        // (2) or is an absolute Uri that represents implicit file Uri "c:\dir\file"
        // (3) or is an absolute Uri that misses a slash before path "file://c:/dir/file"
        // (4) or contains unescaped backslashes even if they will be treated 
        //     as forward slashes like http:\\host/path\file or file:\\\c:\path
        // 
        internal unsafe bool InternalIsWellFormedOriginalString() 
        {
            if (UserDrivenParsing) 
                throw new InvalidOperationException(SR.GetString(SR.net_uri_UserDrivenParsing, this.GetType().FullName));

            fixed (char* str = m_String)
            { 
                ushort idx = 0;
                // 
                // For a relative Uri we only care about escaping and backslashes 
                //
                if (!IsAbsoluteUri) 
                    return (CheckCanonical(str, ref idx, (ushort)m_String.Length, c_EOL) & (Check.BackslashInPath | Check.EscapedCanonical)) == Check.EscapedCanonical;

                //
                // (2) or is an absolute Uri that represents implicit file Uri "c:\dir\file" 
                //
                if (IsImplicitFile) 
                    return false; 

                //This will get all the offsets, a Host name will be checked separatelly below 
                EnsureParseRemaining();

                Flags nonCanonical = (m_Flags & (Flags.E_CannotDisplayCanonical | Flags.IriCanonical));
                // User, Path, Query or Fragment may have some non escaped characters 
                if (((nonCanonical & Flags.E_CannotDisplayCanonical & (Flags.E_UserNotCanonical | Flags.E_PathNotCanonical |
                                        Flags.E_QueryNotCanonical | Flags.E_FragmentNotCanonical)) != Flags.Zero) && 
                    (!m_iriParsing || (m_iriParsing && 
                    (((nonCanonical & Flags.E_UserNotCanonical) == 0) || ((nonCanonical & Flags.UserIriCanonical) == 0)) &&
                    (((nonCanonical & Flags.E_PathNotCanonical) == 0) || ((nonCanonical & Flags.PathIriCanonical) == 0)) && 
                    (((nonCanonical & Flags.E_QueryNotCanonical) == 0) || ((nonCanonical & Flags.QueryIriCanonical) == 0)) &&
                    (((nonCanonical & Flags.E_FragmentNotCanonical) == 0) || ((nonCanonical & Flags.FragmentIriCanonical) == 0)))))
                    return false;
 
                // checking on scheme:\\ or file:////
                if (InFact(Flags.AuthorityFound)) 
                { 
                    idx = (ushort)(m_Info.Offset.Scheme + m_Syntax.SchemeName.Length + 2);
                    if (idx >= m_Info.Offset.User || m_String[idx - 1] == '\\' || m_String[idx] == '\\') 
                        return false;

#if !PLATFORM_UNIX
                    if (InFact(Flags.UncPath | Flags.DosPath)) 
                    {
                        while (++idx < m_Info.Offset.User && (m_String[idx] == '/' || m_String[idx] == '\\')) 
                            return false; 
                    }
#endif // !PLATFORM_UNIX 
                }


                // (3) or is an absolute Uri that misses a slash before path "file://c:/dir/file" 
                // Note that for this check to be more general we assert that if Path is non empty and if it requires a first slash
                // (which looks absent) then the method has to fail. 
                // Today it's only possible for a Dos like path, i.e. file://c:/bla would fail below check. 
                if (InFact(Flags.FirstSlashAbsent) && m_Info.Offset.Query > m_Info.Offset.Path)
                    return false; 

                // (4) or contains unescaped backslashes even if they will be treated
                //     as forward slashes like http:\\host/path\file or file:\\\c:\path
                // Note we do not check for Flags.ShouldBeCompressed i.e. allow // /./ and alike as valid 
                if (InFact(Flags.BackslashInPath))
                    return false; 
 
                // Capturing a rare case like file:///c|/dir
                if (IsDosPath && m_String[m_Info.Offset.Path + SecuredPathIndex - 1] == '|') 
                    return false;

                //
                // May need some real CPU processing to anwser the request 
                //
                // 
                // Check escaping for authority 
                //
                if ((m_Flags & Flags.CanonicalDnsHost) == 0) 
                {
                    idx = m_Info.Offset.User;
                    if (!(m_iriParsing && (HostType == Flags.IPv6HostType))){
                        Check result = CheckCanonical(str, ref idx, (ushort)m_Info.Offset.Path, '/'); 
                        if (((result & (Check.ReservedFound | Check.BackslashInPath | Check.EscapedCanonical)) != Check.EscapedCanonical) &&
                            (!m_iriParsing || (m_iriParsing && ((result & (Check.DisplayCanonical | Check.FoundNonAscii | Check.NotIriCanonical)) 
                                                            != (Check.DisplayCanonical | Check.FoundNonAscii))))) 
                            return false;
                    } 
                }

                // Want to ensure there are slashes after the scheme
                if ((m_Flags & (Flags.SchemeNotCanonical | Flags.AuthorityFound)) == (Flags.SchemeNotCanonical | Flags.AuthorityFound)) 
                {
                    idx = (ushort)m_Syntax.SchemeName.Length; 
                    while (str[idx++] != ':') ; 
                    if (idx + 1 >= m_String.Length || str[idx] != '/' || str[idx + 1] != '/')
                        return false; 
                }
            }
            //
            // May be scheme, host, port or path need some canonicalization but still the uri string is found to be a "well formed" one 
            //
            return true; 
        } 

 
        // Should never be used except by the below method
        private Uri(Flags flags, UriParser uriParser, string uri)
        {
            m_Flags = flags; 
            m_Syntax = uriParser;
            m_String = uri; 
        } 
        //
        // a Uri.TryCreate() method goes through here. 
        //
        internal static Uri CreateHelper(string uriString, bool dontEscape, UriKind uriKind, ref UriFormatException e)
        {
            // if (!Enum.IsDefined(typeof(UriKind), uriKind)) -- We currently believe that Enum.IsDefined() is too slow to be used here. 
            if ((int)uriKind < (int)UriKind.RelativeOrAbsolute || (int)uriKind > (int)UriKind.Relative){
                throw new ArgumentException(SR.GetString(SR.net_uri_InvalidUriKind, uriKind)); 
            } 

            UriParser syntax = null; 
            Flags flags = Flags.Zero;
            ParsingError err = ParseScheme(uriString, ref flags, ref syntax);

            if (dontEscape) 
                flags |= Flags.UserEscaped;
 
            // We won't use User factory for these errors 
            if (err != ParsingError.None)
            { 
                // If it looks as a relative Uri, custom factory is ignored
                if (uriKind != UriKind.Absolute && err <= ParsingError.LastRelativeUriOkErrIndex)
                    return new Uri((flags & Flags.UserEscaped), null, uriString);
 
                return null;
            } 
 
            // Cannot be relative Uri if came here
            Uri result = new Uri(flags, syntax, uriString); 

            // Validate instance using ether built in or a user Parser
            try
            { 
                result.InitializeUri(err, uriKind, out e);
 
                if (e == null) 
                    return result;
 
                return null;
            }
            catch (UriFormatException ee)
            { 
                System.Net.GlobalLog.Assert(!syntax.IsSimple, "A UriPraser threw on InitializeAndValidate.");
                e = ee; 
                // A precaution since custom Parser should never throw in this case. 
                return null;
            } 
        }
        //
        // Resolves into either baseUri or relativeUri according to conditions OR if not possible it uses newUriString
        // to  return combined URI strings from both Uris 
        // otherwise if e != null on output the operation has failed
        // 
 
        internal static Uri ResolveHelper(Uri baseUri, Uri relativeUri, ref string newUriString, ref bool userEscaped, out UriFormatException e)
        { 
            System.Net.GlobalLog.Assert(!baseUri.IsNotAbsoluteUri && !baseUri.UserDrivenParsing, "Uri::ResolveHelper()|baseUri is not Absolute or is controlled by User Parser.");

            e = null;
            string relativeStr = string.Empty; 

            if ((object)relativeUri != null) 
            { 
                if (relativeUri.IsAbsoluteUri)
                    return relativeUri; 

                relativeStr = relativeUri.OriginalString;
                userEscaped = relativeUri.UserEscaped;
            } 
            else
                relativeStr = string.Empty; 
 
            // Here we can assert that passed "relativeUri" is indeed a relative one
 
            if (relativeStr.Length > 0 && (IsLWS(relativeStr[0]) || IsLWS(relativeStr[relativeStr.Length - 1])))
                relativeStr = relativeStr.Trim(_WSchars);

            if (relativeStr.Length == 0) 
            {
                newUriString = baseUri.GetParts(UriComponents.AbsoluteUri, baseUri.UserEscaped ? UriFormat.UriEscaped : UriFormat.SafeUnescaped); 
                return null; 
            }
 
            // Check for a simple fragment in relative part
            if (relativeStr[0] == '#' && !baseUri.IsImplicitFile && baseUri.Syntax.InFact(UriSyntaxFlags.MayHaveFragment))
            {
                newUriString = baseUri.GetParts(UriComponents.AbsoluteUri & ~UriComponents.Fragment, UriFormat.UriEscaped) + relativeStr; 
                return null;
            } 
 
            // Check on the DOS path in the relative Uri (a special case)
            if (relativeStr.Length >= 3 
                && (relativeStr[1] == ':' || relativeStr[1] == '|')
                && IsAsciiLetter(relativeStr[0])
                && (relativeStr[2] == '\\' || relativeStr[2] == '/'))
            { 

                if (baseUri.IsImplicitFile) 
                { 
                    // It could have file:/// prepended to the result but we want to keep it as *Implicit* File Uri
                    newUriString = relativeStr; 
                    return null;
                }
                else if (baseUri.Syntax.InFact(UriSyntaxFlags.AllowDOSPath))
                { 
                    // The scheme is not changed just the path gets replaced
                    string prefix; 
                    if (baseUri.InFact(Flags.AuthorityFound)) 
                        prefix = baseUri.Syntax.InFact(UriSyntaxFlags.PathIsRooted) ? ":///" : "://";
                    else 
                        prefix = baseUri.Syntax.InFact(UriSyntaxFlags.PathIsRooted) ? ":/" : ":";

                    newUriString = baseUri.Scheme + prefix + relativeStr;
                    return null; 
                }
                // If we are here then input like "http://host/path/" + "C:\x" will produce the result  http://host/path/c:/x 
            } 

 
            ParsingError err = GetCombinedString(baseUri, relativeStr, userEscaped, ref newUriString);

            if (err != ParsingError.None)
            { 
                e = GetException(err);
                return null; 
            } 

            if ((object)newUriString == (object)baseUri.m_String) 
                return baseUri;

            return null;
        } 

        private unsafe string GetRelativeSerializationString(UriFormat format) 
        { 
            if (format == UriFormat.UriEscaped)
            { 
                if (m_String.Length == 0)
                    return string.Empty;
                int position = 0;
                char[] dest = EscapeString(m_String, 0, m_String.Length, null, ref position, true, c_DummyChar, c_DummyChar, '%'); 
                if ((object)dest == null)
                    return m_String; 
                return new string(dest, 0, position); 
            }
 
            else if (format == UriFormat.Unescaped)
                return UnescapeDataString(m_String);

            else if (format == UriFormat.SafeUnescaped) 
            {
                if (m_String.Length == 0) 
                    return string.Empty; 

                char[] dest = new char[m_String.Length]; 
                int position = 0;
                dest = UnescapeString(m_String, 0, m_String.Length, dest, ref position, c_DummyChar, c_DummyChar, c_DummyChar,
                    UnescapeMode.EscapeUnescape, null, false, true);
                return new string(dest, 0, position); 
            }
            else 
                throw new ArgumentOutOfRangeException("format"); 

        } 

        //
        // UriParser helpers methods
        // 
        internal string GetComponentsHelper(UriComponents uriComponents, UriFormat uriFormat)
        { 
            if (uriComponents == UriComponents.Scheme) 
                return m_Syntax.SchemeName;
 
            // A serialzation info is "almost" the same as AbsoluteUri except for IPv6 + ScopeID hostname case
            if ((uriComponents & UriComponents.SerializationInfoString) != 0)
                uriComponents |= UriComponents.AbsoluteUri;
 
            //This will get all the offsets, HostString will be created below if needed
            EnsureParseRemaining(); 
 
            //Check to see if we need the host/authotity string
            if ((uriComponents & UriComponents.Host) != 0) 
                EnsureHostString(true);

            //This, single Port request is always processed here
            if (uriComponents == UriComponents.Port || uriComponents == UriComponents.StrongPort) 
            {
                if (((m_Flags & Flags.NotDefaultPort) != 0) || (uriComponents == UriComponents.StrongPort && m_Syntax.DefaultPort != UriParser.NoDefaultPort)) 
                { 
                    // recreate string from the port value
                    return m_Info.Offset.PortValue.ToString(CultureInfo.InvariantCulture); 
                }
                return string.Empty;
            }
 
            if ((uriComponents & UriComponents.StrongPort) != 0)
            { 
                // Down the path we rely on Port to be ON for StrongPort 
                uriComponents |= UriComponents.Port;
            } 

            //This request sometime is faster to process here
            if (uriComponents == UriComponents.Host && (uriFormat == UriFormat.UriEscaped
                || (( m_Flags & (Flags.HostNotCanonical | Flags.E_HostNotCanonical)) == 0))) 
            {
                EnsureHostString(false); 
                return m_Info.Host; 
            }
 
            switch (uriFormat)
            {
                case UriFormat.UriEscaped:
                    return GetEscapedParts(uriComponents); 

                case V1ToStringUnescape: 
                case UriFormat.SafeUnescaped: 
                case UriFormat.Unescaped:
                    return GetUnescapedParts(uriComponents, uriFormat); 

                default:
                    throw new ArgumentOutOfRangeException("uriFormat");
            } 
        }
        // 
        // 
        //
        internal bool IsBaseOfHelper(Uri uriLink) 
        {
            //TO
            if (!IsAbsoluteUri || UserDrivenParsing)
                return false; 

            if (!uriLink.IsAbsoluteUri) 
            { 
                //a relative uri could have quite tricky form, it's better to fix it now.
                string newUriString = null; 
                UriFormatException e;
                bool dontEscape = false;

                uriLink = ResolveHelper(this, uriLink, ref newUriString, ref dontEscape, out e); 
                if (e != null)
                    return false; 
 
                if ((object)uriLink == null)
                    uriLink = CreateHelper(newUriString, dontEscape, UriKind.Absolute, ref e); 

                if (e != null)
                    return false;
            } 

            if (Syntax.SchemeName != uriLink.Syntax.SchemeName) 
                return false; 

            // Canonicalize and test for substring match up to the last path slash 
            string me = GetParts(UriComponents.AbsoluteUri & ~UriComponents.Fragment, UriFormat.SafeUnescaped);
            string she = uriLink.GetParts(UriComponents.AbsoluteUri & ~UriComponents.Fragment, UriFormat.SafeUnescaped);

            unsafe 
            {
                fixed (char* pMe = me) 
                { 
                    fixed (char* pShe = she)
                    { 
                        return TestForSubPath(pMe, (ushort)me.Length, pShe, (ushort)she.Length, IsUncOrDosPath || uriLink.IsUncOrDosPath);
                    }
                }
            } 
        }
        // 
        // Only a ctor time call 
        //
        private void CreateThisFromUri(Uri otherUri) 
        {
            // Clone the other guy but develop own UriInfo member
            m_Info = null;
 
            m_Flags = otherUri.m_Flags;
            if (InFact(Flags.MinimalUriInfoSet)) 
            { 
                m_Flags &= ~(Flags.MinimalUriInfoSet | Flags.AllUriInfoSet | Flags.IndexMask);
                m_Flags |= (Flags)otherUri.m_Info.Offset.Path; 

            }

            m_Syntax = otherUri.m_Syntax; 
            m_String = otherUri.m_String;
            m_iriParsing = otherUri.m_iriParsing; 
            if (otherUri.OriginalStringSwitched){ 
                m_originalUnicodeString = otherUri.m_originalUnicodeString;
            } 
            if (otherUri.AllowIdn && (otherUri.InFact(Flags.IdnHost) || otherUri.InFact(Flags.UnicodeHost))){
                m_DnsSafeHost = otherUri.m_DnsSafeHost;
            }
        } 
    }
} 
