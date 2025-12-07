//------------------------------------------------------------------------------ 
// <copyright file="MailBnfHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mime 
{ 
    using System;
    using System.Text; 
    using System.Net.Mail;
    using System.Globalization;

    internal static class MailBnfHelper 
    {
        static bool[] s_atext = new bool[128]; 
        static bool[] s_qtext = new bool[128]; 
        static bool[] s_fqtext = new bool[128];
        static bool[] s_dtext = new bool[128]; 
        static bool[] s_fdtext = new bool[128];
        static bool[] s_ftext = new bool[128];
        static bool[] s_ttext = new bool[128];
        static bool[] s_digits = new bool[128]; 

        static MailBnfHelper() 
        { 
            // atext = ALPHA / DIGIT / "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" / "-" / "/" / "=" / "?" / "^" / "_" / "`" / "{" / "|" / "}" / "~"
            for (int i = '0'; i <= '9'; i++) { s_atext[i] = true; } 
            for (int i = 'A'; i <= 'Z'; i++) { s_atext[i] = true; }
            for (int i = 'a'; i <= 'z'; i++) { s_atext[i] = true; }
            s_atext['!'] = true;
            s_atext['#'] = true; 
            s_atext['$'] = true;
            s_atext['%'] = true; 
            s_atext['&'] = true; 
            s_atext['\''] = true;
            s_atext['*'] = true; 
            s_atext['+'] = true;
            s_atext['-'] = true;
            s_atext['/'] = true;
            s_atext['='] = true; 
            s_atext['?'] = true;
            s_atext['^'] = true; 
            s_atext['_'] = true; 
            s_atext['`'] = true;
            s_atext['{'] = true; 
            s_atext['|'] = true;
            s_atext['}'] = true;
            s_atext['~'] = true;
 
            // qtext = %d1-8 / %d11 / %d12 / %d14-31 / %d33 / %d35-91 / %d93-127
            for (int i = 1; i <= 8; i++) { s_qtext[i] = true; } 
            s_qtext[11] = true; 
            s_qtext[12] = true;
            for (int i = 14; i <= 31; i++) { s_qtext[i] = true; } 
            s_qtext[33] = true;
            for (int i = 35; i <= 91; i++) { s_qtext[i] = true; }
            for (int i = 93; i <= 127; i++) { s_qtext[i] = true; }
 
            // fqtext = %d1-9 / %d11 / %d12 / %d14-33 / %d35-91 / %d93-127
            for (int i = 1; i <= 9; i++) { s_fqtext[i] = true; } 
            s_fqtext[11] = true; 
            s_fqtext[12] = true;
            for (int i = 14; i <= 33; i++) { s_fqtext[i] = true; } 
            for (int i = 35; i <= 91; i++) { s_fqtext[i] = true; }
            for (int i = 93; i <= 127; i++) { s_fqtext[i] = true; }

            // dtext = %d1-8 / %d11 / %d12 / %d14-31 / %d33-90 / %d94-127 
            for (int i = 1; i <= 8; i++) { s_dtext[i] = true; }
            s_dtext[11] = true; 
            s_dtext[12] = true; 
            for (int i = 14; i <= 31; i++) { s_dtext[i] = true; }
            for (int i = 33; i <= 90; i++) { s_dtext[i] = true; } 
            for (int i = 94; i <= 127; i++) { s_dtext[i] = true; }

            // fdtext = %d1-9 / %d11 / %d12 / %d14-90 / %d94-127
            for (int i = 1; i <= 9; i++) { s_fdtext[i] = true; } 
            s_fdtext[11] = true;
            s_fdtext[12] = true; 
            for (int i = 14; i <= 90; i++) { s_fdtext[i] = true; } 
            for (int i = 94; i <= 127; i++) { s_fdtext[i] = true; }
 
            // ftext = %d33-57 / %d59-126
            for (int i = 33; i <= 57; i++) { s_ftext[i] = true; }
            for (int i = 59; i <= 126; i++) { s_ftext[i] = true; }
 
            // ttext = %d33-126 except '()<>@,;:\"/[]?='
            for (int i = 33; i <= 126; i++) { s_ttext[i] = true; } 
            s_ttext['('] = false; 
            s_ttext[')'] = false;
            s_ttext['<'] = false; 
            s_ttext['>'] = false;
            s_ttext['@'] = false;
            s_ttext[','] = false;
            s_ttext[';'] = false; 
            s_ttext[':'] = false;
            s_ttext['\\'] = false; 
            s_ttext['"'] = false; 
            s_ttext['/'] = false;
            s_ttext['['] = false; 
            s_ttext[']'] = false;
            s_ttext['?'] = false;
            s_ttext['='] = false;
 
            // digits = %d48-57
            for (int i = 48; i <= 57; i++) 
                s_digits[i] = true; 
        }
 



 
        internal static bool SkipCFWS(string data, ref int offset)
        { 
            int comments = 0; 
            for (;offset < data.Length ;offset++)
            { 
                if (data[offset] > 127)
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
                else if (data[offset] == '\\' && comments > 0)
                    offset += 2; 
                else if (data[offset] == '(')
                    comments++; 
                else if (data[offset] == ')') 
                    comments--;
                else if (data[offset] != ' ' && data[offset] != '\t' && comments == 0) 
                    return true;

                if (comments < 0) {
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                }
            } 
 
            //returns false if end of string
            return false; 
        }


        internal static bool SkipFWS(string data, ref int offset) 
        {
            for (;offset < data.Length ;offset++) 
            { 
                if (data[offset] != ' ' && data[offset] != '\t')
                    return true; 
            }

            //returns false if end of string
            return false; 
        }
 
 
        internal static void ValidateHeaderName(string data){
            int offset = 0; 
            for (; offset < data.Length; offset++)
            {
                if (data[offset] > s_ftext.Length || !s_ftext[data[offset]])
                    throw new FormatException(SR.GetString(SR.InvalidHeaderName)); 
            }
            if (offset == 0) 
                throw new FormatException(SR.GetString(SR.InvalidHeaderName)); 
        }
 

        /*
        // Consider removing.
        internal static string ReadFieldName(string data, ref int offset, StringBuilder builder) 
        {
            int start = offset; 
            for (; offset < data.Length; offset++) 
            {
                if (data[offset] == ':') 
                    break;
                if (data[offset] > s_ftext.Length || !s_ftext[data[offset]])
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
            } 
            if (start == 0 && offset == data.Length)
                return data; 
            else 
                return data.Substring(start, offset - start);
        } 
        */

        internal static string ReadQuotedString(string data, ref int offset, StringBuilder builder){
            return ReadQuotedString(data, ref offset, builder,false); 
        }
 
        internal static string ReadQuotedString(string data, ref int offset, StringBuilder builder,bool doesntRequireQuotes) 
        {
            // assume first char is the opening quote 
            if(!doesntRequireQuotes){
                ++offset;
            }
            int start = offset; 
            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder());
            for (; offset < data.Length; offset++) 
            { 
                if (data[offset] == '\\')
                { 
                    localBuilder.Append(data, start, offset - start);
                    start = ++offset;
                    continue;
                } 
                else if (data[offset] == '"')
                { 
                    localBuilder.Append(data, start, offset - start); 
                    offset++;
                    return (builder != null ? null : localBuilder.ToString()); 
                }
                else if (!s_fqtext[data[offset]])
                {
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                }
            } 
            if(doesntRequireQuotes){ 
                localBuilder.Append(data, start, offset - start);
                return (builder != null ? null : localBuilder.ToString()); 
            }
            throw new FormatException(SR.GetString(SR.MailHeaderFieldMalformedHeader));
        }
 
        internal static string ReadPhrase(string data, ref int offset, StringBuilder builder)
        { 
            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 
            bool addSP = false;
            SkipCFWS(data, ref offset); 
            int start = offset;

            while (SkipCFWS(data, ref offset))
            { 
                if (data[offset] == '"')
                { 
                    if (addSP) 
                        localBuilder.Append(' ');
                    ReadQuotedString(data, ref offset, localBuilder); 
                    addSP = true;
                }
                else if (s_atext[data[offset]])
                { 
                    if (addSP)
                        localBuilder.Append(' '); 
                    ReadAtom(data, ref offset, localBuilder); 
                    addSP = true;
                } 
                else
                {
                    break;
                } 
            }
            if (start == offset) { 
                //we didn't get any text, which is a violation 
                throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
            } 

            return (builder != null ? null : localBuilder.ToString());
        }
 
        internal static string ReadAtom(string data, ref int offset, StringBuilder builder)
        { 
            int start = offset; 
            string ret;
            for (; offset < data.Length; offset++) 
            {
                if (data[offset] > s_atext.Length)
                {
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                }
                else if (!s_atext[data[offset]]) 
                { 

                    //if we didn't find any data, then there was an error. 
                    if(offset == start){
                        throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
                    }
 
                    ret = data.Substring(start, offset - start);
                    if (builder != null) 
                    { 
                        builder.Append(ret);
                        return null; 
                    }
                    else
                    {
                        return ret; 
                    }
                } 
            } 

 
            //if we didn't find any data, then there was an error.
            if(offset == start){
                throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
            } 

            ret = (start == 0 ? data : data.Substring(start)); 
            if (builder != null) 
            {
                builder.Append(ret); 
                return null;
            }
            else
            { 
                return ret;
            } 
        } 

 
        internal static string ReadDotAtom(string data, ref int offset, StringBuilder builder)
        {
            int start = offset;
            bool validBuilder = true; 
            if (builder == null){
                validBuilder = false; 
                builder = new StringBuilder(); 
            }
 
            if(data[offset]!='.'){
                ReadAtom(data,ref offset, builder);
            }
 
            while(offset < data.Length && data[offset]=='.') {
                builder.Append(data[offset++]); 
                ReadAtom(data,ref offset, builder); 
            }
 
            if(validBuilder){
                return null;
            }
            else{ 
                return builder.ToString();
            } 
        } 

 
        /*
        // Consider removing.
        internal static string ReadAddress(string data, ref int offset, StringBuilder builder)
        { 
            int start = offset;
            if (!SkipCFWS(data, ref offset)) 
                return null; 

            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 

            if (data[offset] == '"')
                ReadQuotedString(data, ref offset, localBuilder);
            else 
                ReadDotAtom(data, ref offset, localBuilder);
 
            if (!SkipCFWS(data, ref offset)) 
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
 
            if (data[offset++] != '@')
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat));

            if (!SkipCFWS(data, ref offset)) 
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
 
            localBuilder.Append('@'); 

            if (data[offset] == '[') 
                ReadDomainLiteral(data, ref offset, localBuilder);
            else
                ReadDotAtom(data, ref offset, localBuilder);
 
            return (builder != null ? null : localBuilder.ToString());
        } 
        */ 

 
        internal static string ReadDomainLiteral(string data, ref int offset, StringBuilder builder)
        {
            // assume first char is the opening bracket
            int start = ++offset; 
            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder());
            for (; offset < data.Length; offset++) 
            { 
                if (data[offset] == '\\')
                { 
                    localBuilder.Append(data, start, offset - start);
                    start = ++offset;
                    continue;
                } 
                else if (data[offset] == ']')
                { 
                    localBuilder.Append(data, start, offset - start); 
                    offset++;
                    return (builder != null ? null : localBuilder.ToString()); 
                }
                else if (data[offset] > s_fdtext.Length || !s_fdtext[data[offset]])
                {
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                }
            } 
            throw new FormatException(SR.GetString(SR.MailHeaderFieldMalformedHeader)); 
        }
 
        internal static string ReadParameterAttribute(string data, ref int offset, StringBuilder builder)
        {
            if (!SkipCFWS(data, ref offset))
                return null; // 

            return ReadToken(data, ref offset, null); 
        } 

        /* 
        // Consider removing.
        internal static string ReadParameterValue(string data, ref int offset, StringBuilder builder)
        {
            if (!SkipCFWS(data, ref offset)) 
                return string.Empty;
 
            if (data[offset] == '"') 
                return ReadQuotedString(data, ref offset, builder);
            else 
                return ReadToken(data, ref offset, builder);
        }
        */
 
        internal static string ReadToken(string data, ref int offset, StringBuilder builder)
        { 
            int start = offset; 
            for (; offset < data.Length; offset++)
            { 
                if (data[offset] > s_ttext.Length)
                {
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
                } 
                else if (!s_ttext[data[offset]])
                { 
                    break; 
                }
            } 

            if (start == offset) {
                throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
            } 

            return data.Substring(start, offset - start); 
        } 

        /* 
        // Consider removing.
        internal static string ReadNoFoldQuotedString(string data, ref int offset, StringBuilder builder)
        {
            // assume first char is the opening quote 
            int start = ++offset;
            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 
            for (; offset < data.Length; offset++) 
            {
                if (data[offset] == '\\') 
                {
                    localBuilder.Append(data, start, offset - start);
                    start = ++offset;
                    continue; 
                }
                else if (data[offset] == '"') 
                { 
                    localBuilder.Append(data, start, offset - start);
                    offset++; 
                    return (builder != null ? null : localBuilder.ToString());
                }
                else if (data[offset] > s_qtext.Length || !s_qtext[data[offset]])
                { 
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
                } 
            } 
            throw new FormatException(SR.GetString(SR.MailHeaderFieldMalformedHeader));
        } 

        /*
        // Consider removing.
        internal static string ReadNoFoldLiteral(string data, ref int offset, StringBuilder builder) 
        {
            // assume first char is the opening bracket 
            int start = ++offset; 
            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder());
            for (; offset < data.Length; offset++) 
            {
                if (data[offset] == '\\')
                {
                    localBuilder.Append(data, start, offset - start); 
                    start = ++offset;
                    continue; 
                } 
                else if (data[offset] == ']')
                { 
                    localBuilder.Append(data, start, offset - start);
                    offset++;
                    return (builder != null ? null : localBuilder.ToString());
                } 
                else if (data[offset] > s_dtext.Length || !s_dtext[data[offset]])
                { 
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                }
            } 
            throw new FormatException(SR.GetString(SR.MailHeaderFieldMalformedHeader));
        }
        */
 

        internal static string ReadAngleAddress(string data, ref int offset, StringBuilder builder) 
        { 
            if (offset >= data.Length) {
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
            }

            //first, need to get local part
            SkipCFWS(data, ref offset); 
            if (data[offset] == '\"') {
                ReadQuotedString(data,ref offset,builder); 
            } 
            else{
                ReadDotAtom(data, ref offset, builder); 
            }

            SkipCFWS(data,ref offset);
 
            if (offset >= data.Length || data[offset] != '@') {
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
            } 
            offset++;
 
            SkipCFWS(data,ref offset);

            string address = ReadAddressSpecDomain(data, ref offset, builder);
            if (!SkipCFWS(data, ref offset) || data[offset++] != '>') 
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
            return address; 
        } 

 
        internal static string ReadAddressSpecDomain(string data,ref int offset, StringBuilder builder){
            if (offset >= data.Length) {
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
            } 

            builder.Append('@'); 
 
            SkipCFWS(data, ref offset);
 
            if (data[offset] == '['){
                ReadDomainLiteral(data, ref offset, builder);
            }
            else{ 
                ReadDotAtom(data, ref offset, builder);
            } 
 
            SkipCFWS(data, ref offset);
 
            return builder.ToString();
        }

 
        internal static string ReadMailAddress(string data, ref int offset, out string displayName){
            string address = null; 
            Exception exception = null; 
            displayName = String.Empty;
            StringBuilder builder = new StringBuilder(); 

            try{
                SkipCFWS(data, ref offset);
 
                if (offset >= data.Length) {
                    exception = new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
                    goto done; 
                }
 
                //angle address
                if (data[offset] == '<')
                {
                    offset++; 
                    address = ReadAngleAddress(data, ref offset, builder);
                    return address; 
                } 

 
                //otherwise, its a group, mailbox-name-addr or mailbox-addr-spec
                //get the first part
                //this should skip CFWS internally before and after the phrase
                ReadPhrase(data, ref offset, builder); 

                if (offset >= data.Length) { 
                    exception = new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
                    goto done;
                } 

            // ReadOneOrMoreQuotedStrings() and ReadOneOrMoreDotAtoms() both
            // call SkipCFWS() before they return so the next character should
            // not be a whitespace character. 

                switch (data[offset]) 
                { 
                    case '@':{
                        //the phrase was the local part 
                        offset++;
                        address = ReadAddressSpecDomain(data,ref offset, builder);
                        break;
                    } 
                    case '.': {
                        // the phrase was a dotatom, therefore this must be 
                        // the localpart of a mail address or the display name. 
                        ReadDotAtom(data, ref offset, builder);
                        SkipCFWS(data, ref offset); 

                        if (offset >= data.Length) {
                            exception = new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
                            goto done; 
                        }
 
                        if (data[offset] == '@') { 
                            offset++;
                            address = ReadAddressSpecDomain(data,ref offset,builder); 
                        }
                        else if (data[offset] == '<') {
                            displayName = builder.ToString();
                            builder = new StringBuilder(); 
                            offset++;
                            address = ReadAngleAddress(data, ref offset, builder); 
                        } 
                        else{
                            exception = new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
                            goto done;
                        }

                        break; 
                    }
                    case '\"':{ 
                        // the phrase was a quoted string, therefore this must be 
                        // the localpart of a mail address, or the display name.
 
                        //skip whitespace and look at next character
                        offset++;

                        if (offset >= data.Length) { 
                            exception = new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
                            goto done; 
                        } 

                        SkipCFWS(data, ref offset); 

                        if (offset >= data.Length) {
                            exception = new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
                            goto done; 
                        }
 
                        //it was the display 
                        if (data[offset] == '<') {
                            offset++; 
                            address = ReadAngleAddress(data, ref offset, builder);
                        }
                        else{
                            //otherwise, it was the local part 
                            address = ReadAddressSpecDomain(data,ref offset,builder);
                        } 
                        break; 
                    }
                    case '<':{ 
                        //the phrase was a display name
                        displayName = builder.ToString();
                        builder = new StringBuilder();
                        offset++; 
                        address = ReadAngleAddress(data, ref offset, builder);
                        break; 
                    } 

                    case ':':{ 
                        exception = new FormatException(SR.GetString(SR.MailAddressUnsupportedFormat));
                        goto done;
                    }
 
                    default:
                    { 
                        exception = new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
                        goto done;
                    } 
                }

                //next character better be whitespace, end of the data, or a comma.  Otherwise, there was an error
                if(offset < data.Length){ 
                    SkipCFWS(data, ref offset);
                    if(offset < data.Length && data[offset]!=','){ 
                        exception = new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
                        goto done;
                    } 
                }
            }
            catch(FormatException){
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
            }
 
            done: 
            if(exception != null){
                throw exception; 
            }
            return address;
        }
 

        internal static MailAddress ReadMailAddress(string data, ref int offset) 
        { 
            string displayName = null;
            string address = ReadMailAddress(data, ref offset,out displayName); 
            return new MailAddress(address,displayName,0);
        }

 
        /*
        // Consider removing. 
        // DATE 
        internal static string ReadDigits(string data, ref int offset, StringBuilder builder)
        { 
            int start = offset;
            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder());
            for (; offset < data.Length && s_digits[data[offset]]; offset++);
            localBuilder.Append(data, start, offset - start); 
            return (builder != null ? null : localBuilder.ToString());
        } 
        */ 

        internal static DateTime ReadDateTime(string data, ref int offset) 
        {
            if (!SkipCFWS(data, ref offset))
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));
 
            //day of week is optional
            if (IsValidDOW(data, ref offset)){ 
                if(offset >= data.Length || data[offset] != ',') 
                    throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));
 
                offset++;
            }

            if (!MailBnfHelper.SkipFWS(data, ref offset)) 
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));
 
            int day = ReadDateNumber(data, ref offset, 2); 

            if (offset >= data.Length || (data[offset] != ' ' && data[offset] != '\t')) 
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));

            if (!MailBnfHelper.SkipFWS(data, ref offset))
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat)); 

            int month = ReadMonth(data, ref offset); 
 
            if (offset >= data.Length || (data[offset] != ' ' && data[offset] != '\t'))
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat)); 

            if (!MailBnfHelper.SkipFWS(data, ref offset))
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));
 
            int year = ReadDateNumber(data, ref offset, 4);
 
            if (offset >= data.Length || (data[offset] != ' ' && data[offset] != '\t')) 
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));
 
            if (!MailBnfHelper.SkipFWS(data, ref offset))
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));

            int hour = ReadDateNumber(data, ref offset,2); 

            if (offset >= data.Length || data[offset] != ':') 
                throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 

            offset++; 
            int minute = ReadDateNumber(data, ref offset,2);

            int second = 0;
            if (offset < data.Length && data[offset] == ':') 
            {
                offset++; 
                second = ReadDateNumber(data, ref offset,2); 
            }
 
            if (!MailBnfHelper.SkipFWS(data, ref offset))
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));

            if (offset >= data.Length || (data[offset] != '-' && data[offset] != '+')) 
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));
 
            offset++; 

            int zone = ReadDateNumber(data, ref offset,4); 
            //

            return new DateTime(year, month, day, hour, minute, second);
        } 

        //  date-time       =       [ day-of-week "," ] date FWS time [CFWS] 
        //  day-of-week     =       ([FWS] day-name) / obs-day-of-week 
        //  day-name        =       "Mon" / "Tue" / "Wed" / "Thu" / "Fri" / "Sat" / "Sun"
        //  date            =       day month year 
        //  year            =       4*DIGIT / obs-year
        //  month           =       (FWS month-name FWS) / obs-month
        //  month-name      =       "Jan" / "Feb" / "Mar" / "Apr" / "May" / "Jun" / "Jul" / "Aug" / "Sep" / "Oct" / "Nov" / "Dec"
        //  day             =       ([FWS] 1*2DIGIT) / obs-day 
        //  time            =       time-of-day FWS zone
        //  time-of-day     =       hour ":" minute [ ":" second ] 
        //  hour            =       2DIGIT / obs-hour 
        //  minute          =       2DIGIT / obs-minute
        //  second          =       2DIGIT / obs-second 
        //  zone            =       (( "+" / "-" ) 4DIGIT) / obs-zone

        static string[] s_months = new string[] { null, "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        static string[] s_days = new string[] {"Mon","Tue","Wed","Thu","Fri","Sat","Sun"}; 

        static bool IsValidDOW(string data, ref int offset) 
        { 
            if(offset + 3 >= data.Length){
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat)); 
            }
            for(int i = 0; i<s_days.Length;i++){
                if(String.Compare(s_days[i],0,data,offset,3,true, CultureInfo.InvariantCulture) == 0){
                    offset+=3; 
                    return true;
                } 
            } 
            return false;
        } 


        static int ReadDateNumber(string data, ref int offset, int maxSize)
        { 
            int res = 0;
            int maxLength = offset + maxSize; 
 
            if (offset >= data.Length)
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat)); 

            for (; offset < data.Length && offset < maxLength; offset++)
            {
                if (data[offset] < '0' || data[offset] > '9') 
                    break;
                res = (res * 10) + (data[offset] - '0'); 
            } 

            if(res == 0) 
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));

            return res;
        } 

        static int ReadMonth(string data, ref int offset) 
        { 
            if (offset >= data.Length - 3)
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat)); 

            switch (data[offset++])
            {
                case 'J': case 'j': // jan / jun / jul 
                switch (data[offset++])
                { 
                    case 'A': case 'a': 
                    switch (data[offset++])
                    { 
                        case 'N': case 'n': return 1;
                    }
                        break;
                    case 'U': case 'u': 
                    switch (data[offset++])
                    { 
                        case 'N': case 'n': return 6; 
                        case 'L': case 'l': return 7;
                    } 
                        break;
                }
                    goto default;
                case 'F': case 'f': // feb 
                switch (data[offset++])
                { 
                    case 'E': case 'e': 
                    switch (data[offset++])
                    { 
                        case 'B': case 'b': return 2;
                    }
                        break;
                } 
                    goto default;
                case 'M': case 'm': // mar / may 
                switch (data[offset++]) 
                {
                    case 'A': case 'a': 
                    switch (data[offset++])
                    {
                        case 'Y': case 'y': return 5;
                        case 'R': case 'r': return 3; 
                    }
                        break; 
                } 
                    goto default;
                case 'A': case 'a': // apr / aug 
                switch (data[offset++])
                {
                    case 'P': case 'p':
                    switch (data[offset++]) 
                    {
                        case 'R': case 'r': return 4; 
                    } 
                        break;
                    case 'U': case 'u': 
                    switch (data[offset++])
                    {
                        case 'G': case 'g': return 8;
                    } 
                        break;
                } 
                    goto default; 
                case 'S': case 's': // sep
                switch (data[offset++]) 
                {
                    case 'E': case 'e':
                    switch (data[offset++])
                    { 
                        case 'P': case 'p': return 9;
                    } 
                        break; 
                }
                    goto default; 
                case 'O': case 'o': // Oct
                switch (data[offset++])
                {
                    case 'C': case 'c': 
                    switch (data[offset++])
                    { 
                        case 'T': case 't': return 10; 
                    }
                        break; 
                }
                    goto default;
                case 'N': case 'n': // Nov
                switch (data[offset++]) 
                {
                    case 'O': case 'o': 
                    switch (data[offset++]) 
                    {
                        case 'V': case 'v': return 11; 
                    }
                        break;
                }
                    goto default; 
                case 'D': case 'd': // dec
                switch (data[offset++]) 
                { 
                    case 'E':
                    case 'e': 
                    switch (data[offset++])
                    {
                        case 'C': case 'c': return 12;
                    } 
                        break;
                } 
                    goto default; 
                default:
                    throw new FormatException(SR.GetString(SR.MailDateInvalidFormat)); 
            }
        }

        internal static string GetDateTimeString(DateTime value, StringBuilder builder) 
        {
            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 
            localBuilder.Append(value.Day); 
            localBuilder.Append(' ');
            localBuilder.Append(s_months[value.Month]); 
            localBuilder.Append(' ');
            localBuilder.Append(value.Year);
            localBuilder.Append(' ');
            if(value.Hour <= 9){ 
                localBuilder.Append('0');
            } 
            localBuilder.Append(value.Hour); 
            localBuilder.Append(':');
            if(value.Minute <= 9){ 
                localBuilder.Append('0');
            }
            localBuilder.Append(value.Minute);
            localBuilder.Append(':'); 
            if(value.Second <= 9){
                localBuilder.Append('0'); 
            } 
            localBuilder.Append(value.Second);
 
            string offset = TimeZone.CurrentTimeZone.GetUtcOffset(value).ToString();
            if (offset[0] != '-') {
                localBuilder.Append(" +");
            } 
            else{
                localBuilder.Append(" "); 
            } 

            string[] offsetFields = offset.Split(':'); 
            localBuilder.Append(offsetFields[0]);
            localBuilder.Append(offsetFields[1]);
            return (builder != null ? null : localBuilder.ToString());
        } 

        internal static string GetTokenOrQuotedString(string data, StringBuilder builder) 
        { 
            int offset = 0, start = 0;
            for (; offset < data.Length; offset++) 
            {
                if (data[offset] > s_ttext.Length)
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
 
                if (!s_ttext[data[offset]] || data[offset] == ' ')
                { 
                    StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 
                    builder.Append('"');
                    for (; offset < data.Length; offset++) 
                    {
                        if (data[offset] > s_fqtext.Length)
                        {
                            throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                        }
                        else if (!s_fqtext[data[offset]]) 
                        { 
                            builder.Append(data, start, offset - start);
                            builder.Append('\\'); 
                            start = offset;
                        }
                    }
                    builder.Append(data, start, offset - start); 
                    builder.Append('"');
                    return (builder != null ? null : localBuilder.ToString()); 
                } 
            }
 
            //always a quoted string if it was empty.
            if(data.Length == 0){
                if (builder != null) {
                    builder.Append("\"\""); 
                }
                else{ 
                    return "\"\""; 
                }
            } 

            if (builder != null)
            {
                builder.Append(data); 
                return null;
            } 
            return data; 
        }
 
        /*
        // Consider removing.
        internal static string GetAtomOrQuotedString(string data, StringBuilder builder)
        { 
            int offset = 0, start = 0;
            for (; offset < data.Length; offset++) 
            { 
                if (data[offset] > s_atext.Length)
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 

                if (!s_atext[data[offset]] || data[offset] == ' ')
                {
                    StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 
                    builder.Append('"');
                    for (; offset < data.Length; offset++) 
                    { 
                        if (data[offset] > s_fqtext.Length)
                        { 
                            throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
                        }
                        else if (!s_fqtext[data[offset]])
                        { 
                            builder.Append(data, start, offset - start);
                            builder.Append('\\'); 
                            start = offset; 
                        }
                    } 
                    builder.Append(data, start, offset - start);
                    builder.Append('"');
                    return (builder != null ? null : localBuilder.ToString());
                } 
            }
            if (builder != null) 
            { 
                builder.Append(data);
                return null; 
            }
            return data;
        }
        */ 

        internal static string GetDotAtomOrQuotedString(string data, StringBuilder builder) 
        { 
            int offset = 0, start = 0;
            for (; offset < data.Length; offset++) 
            {
                if (data[offset] > s_atext.Length)
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
 
                if ((data[offset] != '.' && !s_atext[data[offset]]) || data[offset] == ' ')
                { 
                    StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 
                    builder.Append('"');
                    for (; offset < data.Length; offset++) 
                    {
                        if (data[offset] > s_fqtext.Length)
                        {
                            throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                        }
                        else if (!s_fqtext[data[offset]]) 
                        { 
                            builder.Append(data, start, offset - start);
                            builder.Append('\\'); 
                            start = offset;
                        }
                    }
                    builder.Append(data, start, offset - start); 
                    builder.Append('"');
                    return (builder != null ? null : localBuilder.ToString()); 
                } 
            }
            if (builder != null) 
            {
                builder.Append(data);
                return null;
            } 
            return data;
        } 
 
        /*
        internal static string GetDotAtomOrNoFoldQuotedString(string data, StringBuilder builder) 
        {
            int offset = 0, start = 0;
            for (; offset < data.Length; offset++)
            { 
                if (data[offset] > s_atext.Length)
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
 
                if (data[offset] != '.' && !s_atext[data[offset]])
                { 
                    StringBuilder localBuilder = (builder != null ? builder : new StringBuilder());
                    builder.Append('"');
                    for (; offset < data.Length; offset++)
                    { 
                        if (data[offset] > s_qtext.Length)
                        { 
                            throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                        }
                        else if (!s_qtext[data[offset]]) 
                        {
                            builder.Append(data, start, offset - start);
                            builder.Append('\\');
                            start = offset; 
                        }
                    } 
                    builder.Append(data, start, offset - start); 
                    builder.Append('"');
                    return (builder != null ? null : localBuilder.ToString()); 
                }
            }
            if (builder != null)
            { 
                builder.Append(data);
                return null; 
            } 
            return data;
        } 
        */

        /*
        // Consider removing. 
        internal static string GetDotAtomOrNoFoldLiteral(string data, StringBuilder builder)
        { 
            int offset = 0, start = 0; 
            for (; offset < data.Length; offset++)
            { 
                if (data[offset] > s_atext.Length)
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));

                if (data[offset] != '.' && !s_atext[data[offset]]) 
                {
                    StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 
                    builder.Append('['); 
                    for (; offset < data.Length; offset++)
                    { 
                        if (data[offset] > s_dtext.Length)
                        {
                            throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
                        } 
                        else if (!s_dtext[data[offset]])
                        { 
                            builder.Append(data, start, offset - start); 
                            builder.Append('\\');
                            start = offset; 
                        }
                    }
                    builder.Append(data, start, offset - start);
                    builder.Append(']'); 
                    return (builder != null ? null : localBuilder.ToString());
                } 
            } 
            if (builder != null)
            { 
                builder.Append(data);
                return null;
            }
            return data; 
        }
        */ 
 
        internal static string GetDotAtomOrDomainLiteral(string data, StringBuilder builder)
        { 
            int offset = 0, start = 0;
            for (; offset < data.Length; offset++)
            {
                if (data[offset] > s_atext.Length) 
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
 
                if (data[offset] != '.' && !s_atext[data[offset]]) 
                {
                    StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 
                    builder.Append('[');
                    for (; offset < data.Length; offset++)
                    {
                        if (data[offset] > s_fdtext.Length) 
                        {
                            throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                        } 
                        else if (!s_fdtext[data[offset]])
                        { 
                            builder.Append(data, start, offset - start);
                            builder.Append('\\');
                            start = offset;
                        } 
                    }
                    builder.Append(data, start, offset - start); 
                    builder.Append(']'); 
                    return (builder != null ? null : localBuilder.ToString());
                } 
            }
            if (builder != null)
            {
                builder.Append(data); 
                return null;
            } 
            return data; 
        }
 
        internal static bool HasCROrLF(string data){
            for (int i=0;i<data.Length;i++) {
                if(data[i] == '\r' || data[i] == '\n'){
                    return true; 
                }
            } 
            return false; 
        }
    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="MailBnfHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net.Mime 
{ 
    using System;
    using System.Text; 
    using System.Net.Mail;
    using System.Globalization;

    internal static class MailBnfHelper 
    {
        static bool[] s_atext = new bool[128]; 
        static bool[] s_qtext = new bool[128]; 
        static bool[] s_fqtext = new bool[128];
        static bool[] s_dtext = new bool[128]; 
        static bool[] s_fdtext = new bool[128];
        static bool[] s_ftext = new bool[128];
        static bool[] s_ttext = new bool[128];
        static bool[] s_digits = new bool[128]; 

        static MailBnfHelper() 
        { 
            // atext = ALPHA / DIGIT / "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" / "-" / "/" / "=" / "?" / "^" / "_" / "`" / "{" / "|" / "}" / "~"
            for (int i = '0'; i <= '9'; i++) { s_atext[i] = true; } 
            for (int i = 'A'; i <= 'Z'; i++) { s_atext[i] = true; }
            for (int i = 'a'; i <= 'z'; i++) { s_atext[i] = true; }
            s_atext['!'] = true;
            s_atext['#'] = true; 
            s_atext['$'] = true;
            s_atext['%'] = true; 
            s_atext['&'] = true; 
            s_atext['\''] = true;
            s_atext['*'] = true; 
            s_atext['+'] = true;
            s_atext['-'] = true;
            s_atext['/'] = true;
            s_atext['='] = true; 
            s_atext['?'] = true;
            s_atext['^'] = true; 
            s_atext['_'] = true; 
            s_atext['`'] = true;
            s_atext['{'] = true; 
            s_atext['|'] = true;
            s_atext['}'] = true;
            s_atext['~'] = true;
 
            // qtext = %d1-8 / %d11 / %d12 / %d14-31 / %d33 / %d35-91 / %d93-127
            for (int i = 1; i <= 8; i++) { s_qtext[i] = true; } 
            s_qtext[11] = true; 
            s_qtext[12] = true;
            for (int i = 14; i <= 31; i++) { s_qtext[i] = true; } 
            s_qtext[33] = true;
            for (int i = 35; i <= 91; i++) { s_qtext[i] = true; }
            for (int i = 93; i <= 127; i++) { s_qtext[i] = true; }
 
            // fqtext = %d1-9 / %d11 / %d12 / %d14-33 / %d35-91 / %d93-127
            for (int i = 1; i <= 9; i++) { s_fqtext[i] = true; } 
            s_fqtext[11] = true; 
            s_fqtext[12] = true;
            for (int i = 14; i <= 33; i++) { s_fqtext[i] = true; } 
            for (int i = 35; i <= 91; i++) { s_fqtext[i] = true; }
            for (int i = 93; i <= 127; i++) { s_fqtext[i] = true; }

            // dtext = %d1-8 / %d11 / %d12 / %d14-31 / %d33-90 / %d94-127 
            for (int i = 1; i <= 8; i++) { s_dtext[i] = true; }
            s_dtext[11] = true; 
            s_dtext[12] = true; 
            for (int i = 14; i <= 31; i++) { s_dtext[i] = true; }
            for (int i = 33; i <= 90; i++) { s_dtext[i] = true; } 
            for (int i = 94; i <= 127; i++) { s_dtext[i] = true; }

            // fdtext = %d1-9 / %d11 / %d12 / %d14-90 / %d94-127
            for (int i = 1; i <= 9; i++) { s_fdtext[i] = true; } 
            s_fdtext[11] = true;
            s_fdtext[12] = true; 
            for (int i = 14; i <= 90; i++) { s_fdtext[i] = true; } 
            for (int i = 94; i <= 127; i++) { s_fdtext[i] = true; }
 
            // ftext = %d33-57 / %d59-126
            for (int i = 33; i <= 57; i++) { s_ftext[i] = true; }
            for (int i = 59; i <= 126; i++) { s_ftext[i] = true; }
 
            // ttext = %d33-126 except '()<>@,;:\"/[]?='
            for (int i = 33; i <= 126; i++) { s_ttext[i] = true; } 
            s_ttext['('] = false; 
            s_ttext[')'] = false;
            s_ttext['<'] = false; 
            s_ttext['>'] = false;
            s_ttext['@'] = false;
            s_ttext[','] = false;
            s_ttext[';'] = false; 
            s_ttext[':'] = false;
            s_ttext['\\'] = false; 
            s_ttext['"'] = false; 
            s_ttext['/'] = false;
            s_ttext['['] = false; 
            s_ttext[']'] = false;
            s_ttext['?'] = false;
            s_ttext['='] = false;
 
            // digits = %d48-57
            for (int i = 48; i <= 57; i++) 
                s_digits[i] = true; 
        }
 



 
        internal static bool SkipCFWS(string data, ref int offset)
        { 
            int comments = 0; 
            for (;offset < data.Length ;offset++)
            { 
                if (data[offset] > 127)
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
                else if (data[offset] == '\\' && comments > 0)
                    offset += 2; 
                else if (data[offset] == '(')
                    comments++; 
                else if (data[offset] == ')') 
                    comments--;
                else if (data[offset] != ' ' && data[offset] != '\t' && comments == 0) 
                    return true;

                if (comments < 0) {
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                }
            } 
 
            //returns false if end of string
            return false; 
        }


        internal static bool SkipFWS(string data, ref int offset) 
        {
            for (;offset < data.Length ;offset++) 
            { 
                if (data[offset] != ' ' && data[offset] != '\t')
                    return true; 
            }

            //returns false if end of string
            return false; 
        }
 
 
        internal static void ValidateHeaderName(string data){
            int offset = 0; 
            for (; offset < data.Length; offset++)
            {
                if (data[offset] > s_ftext.Length || !s_ftext[data[offset]])
                    throw new FormatException(SR.GetString(SR.InvalidHeaderName)); 
            }
            if (offset == 0) 
                throw new FormatException(SR.GetString(SR.InvalidHeaderName)); 
        }
 

        /*
        // Consider removing.
        internal static string ReadFieldName(string data, ref int offset, StringBuilder builder) 
        {
            int start = offset; 
            for (; offset < data.Length; offset++) 
            {
                if (data[offset] == ':') 
                    break;
                if (data[offset] > s_ftext.Length || !s_ftext[data[offset]])
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
            } 
            if (start == 0 && offset == data.Length)
                return data; 
            else 
                return data.Substring(start, offset - start);
        } 
        */

        internal static string ReadQuotedString(string data, ref int offset, StringBuilder builder){
            return ReadQuotedString(data, ref offset, builder,false); 
        }
 
        internal static string ReadQuotedString(string data, ref int offset, StringBuilder builder,bool doesntRequireQuotes) 
        {
            // assume first char is the opening quote 
            if(!doesntRequireQuotes){
                ++offset;
            }
            int start = offset; 
            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder());
            for (; offset < data.Length; offset++) 
            { 
                if (data[offset] == '\\')
                { 
                    localBuilder.Append(data, start, offset - start);
                    start = ++offset;
                    continue;
                } 
                else if (data[offset] == '"')
                { 
                    localBuilder.Append(data, start, offset - start); 
                    offset++;
                    return (builder != null ? null : localBuilder.ToString()); 
                }
                else if (!s_fqtext[data[offset]])
                {
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                }
            } 
            if(doesntRequireQuotes){ 
                localBuilder.Append(data, start, offset - start);
                return (builder != null ? null : localBuilder.ToString()); 
            }
            throw new FormatException(SR.GetString(SR.MailHeaderFieldMalformedHeader));
        }
 
        internal static string ReadPhrase(string data, ref int offset, StringBuilder builder)
        { 
            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 
            bool addSP = false;
            SkipCFWS(data, ref offset); 
            int start = offset;

            while (SkipCFWS(data, ref offset))
            { 
                if (data[offset] == '"')
                { 
                    if (addSP) 
                        localBuilder.Append(' ');
                    ReadQuotedString(data, ref offset, localBuilder); 
                    addSP = true;
                }
                else if (s_atext[data[offset]])
                { 
                    if (addSP)
                        localBuilder.Append(' '); 
                    ReadAtom(data, ref offset, localBuilder); 
                    addSP = true;
                } 
                else
                {
                    break;
                } 
            }
            if (start == offset) { 
                //we didn't get any text, which is a violation 
                throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
            } 

            return (builder != null ? null : localBuilder.ToString());
        }
 
        internal static string ReadAtom(string data, ref int offset, StringBuilder builder)
        { 
            int start = offset; 
            string ret;
            for (; offset < data.Length; offset++) 
            {
                if (data[offset] > s_atext.Length)
                {
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                }
                else if (!s_atext[data[offset]]) 
                { 

                    //if we didn't find any data, then there was an error. 
                    if(offset == start){
                        throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
                    }
 
                    ret = data.Substring(start, offset - start);
                    if (builder != null) 
                    { 
                        builder.Append(ret);
                        return null; 
                    }
                    else
                    {
                        return ret; 
                    }
                } 
            } 

 
            //if we didn't find any data, then there was an error.
            if(offset == start){
                throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
            } 

            ret = (start == 0 ? data : data.Substring(start)); 
            if (builder != null) 
            {
                builder.Append(ret); 
                return null;
            }
            else
            { 
                return ret;
            } 
        } 

 
        internal static string ReadDotAtom(string data, ref int offset, StringBuilder builder)
        {
            int start = offset;
            bool validBuilder = true; 
            if (builder == null){
                validBuilder = false; 
                builder = new StringBuilder(); 
            }
 
            if(data[offset]!='.'){
                ReadAtom(data,ref offset, builder);
            }
 
            while(offset < data.Length && data[offset]=='.') {
                builder.Append(data[offset++]); 
                ReadAtom(data,ref offset, builder); 
            }
 
            if(validBuilder){
                return null;
            }
            else{ 
                return builder.ToString();
            } 
        } 

 
        /*
        // Consider removing.
        internal static string ReadAddress(string data, ref int offset, StringBuilder builder)
        { 
            int start = offset;
            if (!SkipCFWS(data, ref offset)) 
                return null; 

            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 

            if (data[offset] == '"')
                ReadQuotedString(data, ref offset, localBuilder);
            else 
                ReadDotAtom(data, ref offset, localBuilder);
 
            if (!SkipCFWS(data, ref offset)) 
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
 
            if (data[offset++] != '@')
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat));

            if (!SkipCFWS(data, ref offset)) 
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
 
            localBuilder.Append('@'); 

            if (data[offset] == '[') 
                ReadDomainLiteral(data, ref offset, localBuilder);
            else
                ReadDotAtom(data, ref offset, localBuilder);
 
            return (builder != null ? null : localBuilder.ToString());
        } 
        */ 

 
        internal static string ReadDomainLiteral(string data, ref int offset, StringBuilder builder)
        {
            // assume first char is the opening bracket
            int start = ++offset; 
            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder());
            for (; offset < data.Length; offset++) 
            { 
                if (data[offset] == '\\')
                { 
                    localBuilder.Append(data, start, offset - start);
                    start = ++offset;
                    continue;
                } 
                else if (data[offset] == ']')
                { 
                    localBuilder.Append(data, start, offset - start); 
                    offset++;
                    return (builder != null ? null : localBuilder.ToString()); 
                }
                else if (data[offset] > s_fdtext.Length || !s_fdtext[data[offset]])
                {
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                }
            } 
            throw new FormatException(SR.GetString(SR.MailHeaderFieldMalformedHeader)); 
        }
 
        internal static string ReadParameterAttribute(string data, ref int offset, StringBuilder builder)
        {
            if (!SkipCFWS(data, ref offset))
                return null; // 

            return ReadToken(data, ref offset, null); 
        } 

        /* 
        // Consider removing.
        internal static string ReadParameterValue(string data, ref int offset, StringBuilder builder)
        {
            if (!SkipCFWS(data, ref offset)) 
                return string.Empty;
 
            if (data[offset] == '"') 
                return ReadQuotedString(data, ref offset, builder);
            else 
                return ReadToken(data, ref offset, builder);
        }
        */
 
        internal static string ReadToken(string data, ref int offset, StringBuilder builder)
        { 
            int start = offset; 
            for (; offset < data.Length; offset++)
            { 
                if (data[offset] > s_ttext.Length)
                {
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
                } 
                else if (!s_ttext[data[offset]])
                { 
                    break; 
                }
            } 

            if (start == offset) {
                throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
            } 

            return data.Substring(start, offset - start); 
        } 

        /* 
        // Consider removing.
        internal static string ReadNoFoldQuotedString(string data, ref int offset, StringBuilder builder)
        {
            // assume first char is the opening quote 
            int start = ++offset;
            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 
            for (; offset < data.Length; offset++) 
            {
                if (data[offset] == '\\') 
                {
                    localBuilder.Append(data, start, offset - start);
                    start = ++offset;
                    continue; 
                }
                else if (data[offset] == '"') 
                { 
                    localBuilder.Append(data, start, offset - start);
                    offset++; 
                    return (builder != null ? null : localBuilder.ToString());
                }
                else if (data[offset] > s_qtext.Length || !s_qtext[data[offset]])
                { 
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
                } 
            } 
            throw new FormatException(SR.GetString(SR.MailHeaderFieldMalformedHeader));
        } 

        /*
        // Consider removing.
        internal static string ReadNoFoldLiteral(string data, ref int offset, StringBuilder builder) 
        {
            // assume first char is the opening bracket 
            int start = ++offset; 
            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder());
            for (; offset < data.Length; offset++) 
            {
                if (data[offset] == '\\')
                {
                    localBuilder.Append(data, start, offset - start); 
                    start = ++offset;
                    continue; 
                } 
                else if (data[offset] == ']')
                { 
                    localBuilder.Append(data, start, offset - start);
                    offset++;
                    return (builder != null ? null : localBuilder.ToString());
                } 
                else if (data[offset] > s_dtext.Length || !s_dtext[data[offset]])
                { 
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                }
            } 
            throw new FormatException(SR.GetString(SR.MailHeaderFieldMalformedHeader));
        }
        */
 

        internal static string ReadAngleAddress(string data, ref int offset, StringBuilder builder) 
        { 
            if (offset >= data.Length) {
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
            }

            //first, need to get local part
            SkipCFWS(data, ref offset); 
            if (data[offset] == '\"') {
                ReadQuotedString(data,ref offset,builder); 
            } 
            else{
                ReadDotAtom(data, ref offset, builder); 
            }

            SkipCFWS(data,ref offset);
 
            if (offset >= data.Length || data[offset] != '@') {
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
            } 
            offset++;
 
            SkipCFWS(data,ref offset);

            string address = ReadAddressSpecDomain(data, ref offset, builder);
            if (!SkipCFWS(data, ref offset) || data[offset++] != '>') 
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
            return address; 
        } 

 
        internal static string ReadAddressSpecDomain(string data,ref int offset, StringBuilder builder){
            if (offset >= data.Length) {
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
            } 

            builder.Append('@'); 
 
            SkipCFWS(data, ref offset);
 
            if (data[offset] == '['){
                ReadDomainLiteral(data, ref offset, builder);
            }
            else{ 
                ReadDotAtom(data, ref offset, builder);
            } 
 
            SkipCFWS(data, ref offset);
 
            return builder.ToString();
        }

 
        internal static string ReadMailAddress(string data, ref int offset, out string displayName){
            string address = null; 
            Exception exception = null; 
            displayName = String.Empty;
            StringBuilder builder = new StringBuilder(); 

            try{
                SkipCFWS(data, ref offset);
 
                if (offset >= data.Length) {
                    exception = new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
                    goto done; 
                }
 
                //angle address
                if (data[offset] == '<')
                {
                    offset++; 
                    address = ReadAngleAddress(data, ref offset, builder);
                    return address; 
                } 

 
                //otherwise, its a group, mailbox-name-addr or mailbox-addr-spec
                //get the first part
                //this should skip CFWS internally before and after the phrase
                ReadPhrase(data, ref offset, builder); 

                if (offset >= data.Length) { 
                    exception = new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
                    goto done;
                } 

            // ReadOneOrMoreQuotedStrings() and ReadOneOrMoreDotAtoms() both
            // call SkipCFWS() before they return so the next character should
            // not be a whitespace character. 

                switch (data[offset]) 
                { 
                    case '@':{
                        //the phrase was the local part 
                        offset++;
                        address = ReadAddressSpecDomain(data,ref offset, builder);
                        break;
                    } 
                    case '.': {
                        // the phrase was a dotatom, therefore this must be 
                        // the localpart of a mail address or the display name. 
                        ReadDotAtom(data, ref offset, builder);
                        SkipCFWS(data, ref offset); 

                        if (offset >= data.Length) {
                            exception = new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
                            goto done; 
                        }
 
                        if (data[offset] == '@') { 
                            offset++;
                            address = ReadAddressSpecDomain(data,ref offset,builder); 
                        }
                        else if (data[offset] == '<') {
                            displayName = builder.ToString();
                            builder = new StringBuilder(); 
                            offset++;
                            address = ReadAngleAddress(data, ref offset, builder); 
                        } 
                        else{
                            exception = new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
                            goto done;
                        }

                        break; 
                    }
                    case '\"':{ 
                        // the phrase was a quoted string, therefore this must be 
                        // the localpart of a mail address, or the display name.
 
                        //skip whitespace and look at next character
                        offset++;

                        if (offset >= data.Length) { 
                            exception = new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
                            goto done; 
                        } 

                        SkipCFWS(data, ref offset); 

                        if (offset >= data.Length) {
                            exception = new FormatException(SR.GetString(SR.MailAddressInvalidFormat));
                            goto done; 
                        }
 
                        //it was the display 
                        if (data[offset] == '<') {
                            offset++; 
                            address = ReadAngleAddress(data, ref offset, builder);
                        }
                        else{
                            //otherwise, it was the local part 
                            address = ReadAddressSpecDomain(data,ref offset,builder);
                        } 
                        break; 
                    }
                    case '<':{ 
                        //the phrase was a display name
                        displayName = builder.ToString();
                        builder = new StringBuilder();
                        offset++; 
                        address = ReadAngleAddress(data, ref offset, builder);
                        break; 
                    } 

                    case ':':{ 
                        exception = new FormatException(SR.GetString(SR.MailAddressUnsupportedFormat));
                        goto done;
                    }
 
                    default:
                    { 
                        exception = new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
                        goto done;
                    } 
                }

                //next character better be whitespace, end of the data, or a comma.  Otherwise, there was an error
                if(offset < data.Length){ 
                    SkipCFWS(data, ref offset);
                    if(offset < data.Length && data[offset]!=','){ 
                        exception = new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
                        goto done;
                    } 
                }
            }
            catch(FormatException){
                throw new FormatException(SR.GetString(SR.MailAddressInvalidFormat)); 
            }
 
            done: 
            if(exception != null){
                throw exception; 
            }
            return address;
        }
 

        internal static MailAddress ReadMailAddress(string data, ref int offset) 
        { 
            string displayName = null;
            string address = ReadMailAddress(data, ref offset,out displayName); 
            return new MailAddress(address,displayName,0);
        }

 
        /*
        // Consider removing. 
        // DATE 
        internal static string ReadDigits(string data, ref int offset, StringBuilder builder)
        { 
            int start = offset;
            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder());
            for (; offset < data.Length && s_digits[data[offset]]; offset++);
            localBuilder.Append(data, start, offset - start); 
            return (builder != null ? null : localBuilder.ToString());
        } 
        */ 

        internal static DateTime ReadDateTime(string data, ref int offset) 
        {
            if (!SkipCFWS(data, ref offset))
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));
 
            //day of week is optional
            if (IsValidDOW(data, ref offset)){ 
                if(offset >= data.Length || data[offset] != ',') 
                    throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));
 
                offset++;
            }

            if (!MailBnfHelper.SkipFWS(data, ref offset)) 
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));
 
            int day = ReadDateNumber(data, ref offset, 2); 

            if (offset >= data.Length || (data[offset] != ' ' && data[offset] != '\t')) 
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));

            if (!MailBnfHelper.SkipFWS(data, ref offset))
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat)); 

            int month = ReadMonth(data, ref offset); 
 
            if (offset >= data.Length || (data[offset] != ' ' && data[offset] != '\t'))
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat)); 

            if (!MailBnfHelper.SkipFWS(data, ref offset))
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));
 
            int year = ReadDateNumber(data, ref offset, 4);
 
            if (offset >= data.Length || (data[offset] != ' ' && data[offset] != '\t')) 
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));
 
            if (!MailBnfHelper.SkipFWS(data, ref offset))
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));

            int hour = ReadDateNumber(data, ref offset,2); 

            if (offset >= data.Length || data[offset] != ':') 
                throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 

            offset++; 
            int minute = ReadDateNumber(data, ref offset,2);

            int second = 0;
            if (offset < data.Length && data[offset] == ':') 
            {
                offset++; 
                second = ReadDateNumber(data, ref offset,2); 
            }
 
            if (!MailBnfHelper.SkipFWS(data, ref offset))
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));

            if (offset >= data.Length || (data[offset] != '-' && data[offset] != '+')) 
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));
 
            offset++; 

            int zone = ReadDateNumber(data, ref offset,4); 
            //

            return new DateTime(year, month, day, hour, minute, second);
        } 

        //  date-time       =       [ day-of-week "," ] date FWS time [CFWS] 
        //  day-of-week     =       ([FWS] day-name) / obs-day-of-week 
        //  day-name        =       "Mon" / "Tue" / "Wed" / "Thu" / "Fri" / "Sat" / "Sun"
        //  date            =       day month year 
        //  year            =       4*DIGIT / obs-year
        //  month           =       (FWS month-name FWS) / obs-month
        //  month-name      =       "Jan" / "Feb" / "Mar" / "Apr" / "May" / "Jun" / "Jul" / "Aug" / "Sep" / "Oct" / "Nov" / "Dec"
        //  day             =       ([FWS] 1*2DIGIT) / obs-day 
        //  time            =       time-of-day FWS zone
        //  time-of-day     =       hour ":" minute [ ":" second ] 
        //  hour            =       2DIGIT / obs-hour 
        //  minute          =       2DIGIT / obs-minute
        //  second          =       2DIGIT / obs-second 
        //  zone            =       (( "+" / "-" ) 4DIGIT) / obs-zone

        static string[] s_months = new string[] { null, "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        static string[] s_days = new string[] {"Mon","Tue","Wed","Thu","Fri","Sat","Sun"}; 

        static bool IsValidDOW(string data, ref int offset) 
        { 
            if(offset + 3 >= data.Length){
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat)); 
            }
            for(int i = 0; i<s_days.Length;i++){
                if(String.Compare(s_days[i],0,data,offset,3,true, CultureInfo.InvariantCulture) == 0){
                    offset+=3; 
                    return true;
                } 
            } 
            return false;
        } 


        static int ReadDateNumber(string data, ref int offset, int maxSize)
        { 
            int res = 0;
            int maxLength = offset + maxSize; 
 
            if (offset >= data.Length)
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat)); 

            for (; offset < data.Length && offset < maxLength; offset++)
            {
                if (data[offset] < '0' || data[offset] > '9') 
                    break;
                res = (res * 10) + (data[offset] - '0'); 
            } 

            if(res == 0) 
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat));

            return res;
        } 

        static int ReadMonth(string data, ref int offset) 
        { 
            if (offset >= data.Length - 3)
                throw new FormatException(SR.GetString(SR.MailDateInvalidFormat)); 

            switch (data[offset++])
            {
                case 'J': case 'j': // jan / jun / jul 
                switch (data[offset++])
                { 
                    case 'A': case 'a': 
                    switch (data[offset++])
                    { 
                        case 'N': case 'n': return 1;
                    }
                        break;
                    case 'U': case 'u': 
                    switch (data[offset++])
                    { 
                        case 'N': case 'n': return 6; 
                        case 'L': case 'l': return 7;
                    } 
                        break;
                }
                    goto default;
                case 'F': case 'f': // feb 
                switch (data[offset++])
                { 
                    case 'E': case 'e': 
                    switch (data[offset++])
                    { 
                        case 'B': case 'b': return 2;
                    }
                        break;
                } 
                    goto default;
                case 'M': case 'm': // mar / may 
                switch (data[offset++]) 
                {
                    case 'A': case 'a': 
                    switch (data[offset++])
                    {
                        case 'Y': case 'y': return 5;
                        case 'R': case 'r': return 3; 
                    }
                        break; 
                } 
                    goto default;
                case 'A': case 'a': // apr / aug 
                switch (data[offset++])
                {
                    case 'P': case 'p':
                    switch (data[offset++]) 
                    {
                        case 'R': case 'r': return 4; 
                    } 
                        break;
                    case 'U': case 'u': 
                    switch (data[offset++])
                    {
                        case 'G': case 'g': return 8;
                    } 
                        break;
                } 
                    goto default; 
                case 'S': case 's': // sep
                switch (data[offset++]) 
                {
                    case 'E': case 'e':
                    switch (data[offset++])
                    { 
                        case 'P': case 'p': return 9;
                    } 
                        break; 
                }
                    goto default; 
                case 'O': case 'o': // Oct
                switch (data[offset++])
                {
                    case 'C': case 'c': 
                    switch (data[offset++])
                    { 
                        case 'T': case 't': return 10; 
                    }
                        break; 
                }
                    goto default;
                case 'N': case 'n': // Nov
                switch (data[offset++]) 
                {
                    case 'O': case 'o': 
                    switch (data[offset++]) 
                    {
                        case 'V': case 'v': return 11; 
                    }
                        break;
                }
                    goto default; 
                case 'D': case 'd': // dec
                switch (data[offset++]) 
                { 
                    case 'E':
                    case 'e': 
                    switch (data[offset++])
                    {
                        case 'C': case 'c': return 12;
                    } 
                        break;
                } 
                    goto default; 
                default:
                    throw new FormatException(SR.GetString(SR.MailDateInvalidFormat)); 
            }
        }

        internal static string GetDateTimeString(DateTime value, StringBuilder builder) 
        {
            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 
            localBuilder.Append(value.Day); 
            localBuilder.Append(' ');
            localBuilder.Append(s_months[value.Month]); 
            localBuilder.Append(' ');
            localBuilder.Append(value.Year);
            localBuilder.Append(' ');
            if(value.Hour <= 9){ 
                localBuilder.Append('0');
            } 
            localBuilder.Append(value.Hour); 
            localBuilder.Append(':');
            if(value.Minute <= 9){ 
                localBuilder.Append('0');
            }
            localBuilder.Append(value.Minute);
            localBuilder.Append(':'); 
            if(value.Second <= 9){
                localBuilder.Append('0'); 
            } 
            localBuilder.Append(value.Second);
 
            string offset = TimeZone.CurrentTimeZone.GetUtcOffset(value).ToString();
            if (offset[0] != '-') {
                localBuilder.Append(" +");
            } 
            else{
                localBuilder.Append(" "); 
            } 

            string[] offsetFields = offset.Split(':'); 
            localBuilder.Append(offsetFields[0]);
            localBuilder.Append(offsetFields[1]);
            return (builder != null ? null : localBuilder.ToString());
        } 

        internal static string GetTokenOrQuotedString(string data, StringBuilder builder) 
        { 
            int offset = 0, start = 0;
            for (; offset < data.Length; offset++) 
            {
                if (data[offset] > s_ttext.Length)
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
 
                if (!s_ttext[data[offset]] || data[offset] == ' ')
                { 
                    StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 
                    builder.Append('"');
                    for (; offset < data.Length; offset++) 
                    {
                        if (data[offset] > s_fqtext.Length)
                        {
                            throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                        }
                        else if (!s_fqtext[data[offset]]) 
                        { 
                            builder.Append(data, start, offset - start);
                            builder.Append('\\'); 
                            start = offset;
                        }
                    }
                    builder.Append(data, start, offset - start); 
                    builder.Append('"');
                    return (builder != null ? null : localBuilder.ToString()); 
                } 
            }
 
            //always a quoted string if it was empty.
            if(data.Length == 0){
                if (builder != null) {
                    builder.Append("\"\""); 
                }
                else{ 
                    return "\"\""; 
                }
            } 

            if (builder != null)
            {
                builder.Append(data); 
                return null;
            } 
            return data; 
        }
 
        /*
        // Consider removing.
        internal static string GetAtomOrQuotedString(string data, StringBuilder builder)
        { 
            int offset = 0, start = 0;
            for (; offset < data.Length; offset++) 
            { 
                if (data[offset] > s_atext.Length)
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 

                if (!s_atext[data[offset]] || data[offset] == ' ')
                {
                    StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 
                    builder.Append('"');
                    for (; offset < data.Length; offset++) 
                    { 
                        if (data[offset] > s_fqtext.Length)
                        { 
                            throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
                        }
                        else if (!s_fqtext[data[offset]])
                        { 
                            builder.Append(data, start, offset - start);
                            builder.Append('\\'); 
                            start = offset; 
                        }
                    } 
                    builder.Append(data, start, offset - start);
                    builder.Append('"');
                    return (builder != null ? null : localBuilder.ToString());
                } 
            }
            if (builder != null) 
            { 
                builder.Append(data);
                return null; 
            }
            return data;
        }
        */ 

        internal static string GetDotAtomOrQuotedString(string data, StringBuilder builder) 
        { 
            int offset = 0, start = 0;
            for (; offset < data.Length; offset++) 
            {
                if (data[offset] > s_atext.Length)
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
 
                if ((data[offset] != '.' && !s_atext[data[offset]]) || data[offset] == ' ')
                { 
                    StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 
                    builder.Append('"');
                    for (; offset < data.Length; offset++) 
                    {
                        if (data[offset] > s_fqtext.Length)
                        {
                            throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                        }
                        else if (!s_fqtext[data[offset]]) 
                        { 
                            builder.Append(data, start, offset - start);
                            builder.Append('\\'); 
                            start = offset;
                        }
                    }
                    builder.Append(data, start, offset - start); 
                    builder.Append('"');
                    return (builder != null ? null : localBuilder.ToString()); 
                } 
            }
            if (builder != null) 
            {
                builder.Append(data);
                return null;
            } 
            return data;
        } 
 
        /*
        internal static string GetDotAtomOrNoFoldQuotedString(string data, StringBuilder builder) 
        {
            int offset = 0, start = 0;
            for (; offset < data.Length; offset++)
            { 
                if (data[offset] > s_atext.Length)
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
 
                if (data[offset] != '.' && !s_atext[data[offset]])
                { 
                    StringBuilder localBuilder = (builder != null ? builder : new StringBuilder());
                    builder.Append('"');
                    for (; offset < data.Length; offset++)
                    { 
                        if (data[offset] > s_qtext.Length)
                        { 
                            throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                        }
                        else if (!s_qtext[data[offset]]) 
                        {
                            builder.Append(data, start, offset - start);
                            builder.Append('\\');
                            start = offset; 
                        }
                    } 
                    builder.Append(data, start, offset - start); 
                    builder.Append('"');
                    return (builder != null ? null : localBuilder.ToString()); 
                }
            }
            if (builder != null)
            { 
                builder.Append(data);
                return null; 
            } 
            return data;
        } 
        */

        /*
        // Consider removing. 
        internal static string GetDotAtomOrNoFoldLiteral(string data, StringBuilder builder)
        { 
            int offset = 0, start = 0; 
            for (; offset < data.Length; offset++)
            { 
                if (data[offset] > s_atext.Length)
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));

                if (data[offset] != '.' && !s_atext[data[offset]]) 
                {
                    StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 
                    builder.Append('['); 
                    for (; offset < data.Length; offset++)
                    { 
                        if (data[offset] > s_dtext.Length)
                        {
                            throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
                        } 
                        else if (!s_dtext[data[offset]])
                        { 
                            builder.Append(data, start, offset - start); 
                            builder.Append('\\');
                            start = offset; 
                        }
                    }
                    builder.Append(data, start, offset - start);
                    builder.Append(']'); 
                    return (builder != null ? null : localBuilder.ToString());
                } 
            } 
            if (builder != null)
            { 
                builder.Append(data);
                return null;
            }
            return data; 
        }
        */ 
 
        internal static string GetDotAtomOrDomainLiteral(string data, StringBuilder builder)
        { 
            int offset = 0, start = 0;
            for (; offset < data.Length; offset++)
            {
                if (data[offset] > s_atext.Length) 
                    throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter));
 
                if (data[offset] != '.' && !s_atext[data[offset]]) 
                {
                    StringBuilder localBuilder = (builder != null ? builder : new StringBuilder()); 
                    builder.Append('[');
                    for (; offset < data.Length; offset++)
                    {
                        if (data[offset] > s_fdtext.Length) 
                        {
                            throw new FormatException(SR.GetString(SR.MailHeaderFieldInvalidCharacter)); 
                        } 
                        else if (!s_fdtext[data[offset]])
                        { 
                            builder.Append(data, start, offset - start);
                            builder.Append('\\');
                            start = offset;
                        } 
                    }
                    builder.Append(data, start, offset - start); 
                    builder.Append(']'); 
                    return (builder != null ? null : localBuilder.ToString());
                } 
            }
            if (builder != null)
            {
                builder.Append(data); 
                return null;
            } 
            return data; 
        }
 
        internal static bool HasCROrLF(string data){
            for (int i=0;i<data.Length;i++) {
                if(data[i] == '\r' || data[i] == '\n'){
                    return true; 
                }
            } 
            return false; 
        }
    } 
}
