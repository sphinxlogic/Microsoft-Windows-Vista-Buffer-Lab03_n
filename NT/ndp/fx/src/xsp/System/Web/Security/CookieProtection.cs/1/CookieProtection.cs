//------------------------------------------------------------------------------ 
// <copyright file="CookieProtection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Security 
{ 
    using System.Security.Cryptography;
    using System.Web.Configuration; 
    using System.Web.Management;


    public enum CookieProtection 
    {
 
        None, Validation, Encryption, All 
    }
 
    internal class CookieProtectionHelper
    {
        internal static string Encode (CookieProtection cookieProtection, byte [] buf, int count)
        { 
            if (cookieProtection == CookieProtection.All || cookieProtection == CookieProtection.Validation)
            { 
                byte[] bMac = MachineKeySection.HashData (buf, null, 0, count); 

                if (bMac == null || bMac.Length != 20) 
                    return null;
                if (buf.Length >= count + 20)
                {
                    Buffer.BlockCopy (bMac, 0, buf, count, 20); 
                }
                else 
                { 
                    byte[] bTemp = buf;
                    buf = new byte[count + 20]; 
                    Buffer.BlockCopy (bTemp, 0, buf, 0, count);
                    Buffer.BlockCopy (bMac, 0, buf, count, 20);
                }
                count += 20; 
            }
 
            if (cookieProtection == CookieProtection.All || cookieProtection == CookieProtection.Encryption) 
            {
                buf = MachineKeySection.EncryptOrDecryptData (true, buf, null, 0, count); 
                count = buf.Length;
            }
            if (count < buf.Length)
            { 
                byte[] bTemp = buf;
                buf = new byte[count]; 
                Buffer.BlockCopy (bTemp, 0, buf, 0, count); 
            }
 
            return HttpServerUtility.UrlTokenEncode(buf);
        }

        internal static byte[] Decode (CookieProtection cookieProtection, string data) 
        {
            byte [] buf = HttpServerUtility.UrlTokenDecode(data); 
            if (buf == null || cookieProtection == CookieProtection.None) 
                return buf;
            if (cookieProtection == CookieProtection.All || cookieProtection == CookieProtection.Encryption) 
            {
                buf = MachineKeySection.EncryptOrDecryptData (false, buf, null, 0, buf.Length);
                if (buf == null)
                    return null; 
            }
 
            if (cookieProtection == CookieProtection.All || cookieProtection == CookieProtection.Validation) 
            {
                ////////////////////////////////////////////////////////////////////// 
                // Step 2: Get the MAC: Last 20 bytes
                if (buf.Length <= 20)
                    return null;
 
                byte[] buf2 = new byte[buf.Length - 20];
                Buffer.BlockCopy (buf, 0, buf2, 0, buf2.Length); 
                byte[] bMac = MachineKeySection.HashData (buf2, null, 0, buf2.Length); 

                ////////////////////////////////////////////////////////////////////// 
                // Step 3: Make sure the MAC is correct
                if (bMac == null || bMac.Length != 20)
                    return null;
 
                for (int iter = 0; iter < 20; iter++)
                    if (bMac[iter] != buf[buf2.Length + iter]) 
                        return null; 

                buf = buf2; 
            }
            return buf;
        }
    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="CookieProtection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Security 
{ 
    using System.Security.Cryptography;
    using System.Web.Configuration; 
    using System.Web.Management;


    public enum CookieProtection 
    {
 
        None, Validation, Encryption, All 
    }
 
    internal class CookieProtectionHelper
    {
        internal static string Encode (CookieProtection cookieProtection, byte [] buf, int count)
        { 
            if (cookieProtection == CookieProtection.All || cookieProtection == CookieProtection.Validation)
            { 
                byte[] bMac = MachineKeySection.HashData (buf, null, 0, count); 

                if (bMac == null || bMac.Length != 20) 
                    return null;
                if (buf.Length >= count + 20)
                {
                    Buffer.BlockCopy (bMac, 0, buf, count, 20); 
                }
                else 
                { 
                    byte[] bTemp = buf;
                    buf = new byte[count + 20]; 
                    Buffer.BlockCopy (bTemp, 0, buf, 0, count);
                    Buffer.BlockCopy (bMac, 0, buf, count, 20);
                }
                count += 20; 
            }
 
            if (cookieProtection == CookieProtection.All || cookieProtection == CookieProtection.Encryption) 
            {
                buf = MachineKeySection.EncryptOrDecryptData (true, buf, null, 0, count); 
                count = buf.Length;
            }
            if (count < buf.Length)
            { 
                byte[] bTemp = buf;
                buf = new byte[count]; 
                Buffer.BlockCopy (bTemp, 0, buf, 0, count); 
            }
 
            return HttpServerUtility.UrlTokenEncode(buf);
        }

        internal static byte[] Decode (CookieProtection cookieProtection, string data) 
        {
            byte [] buf = HttpServerUtility.UrlTokenDecode(data); 
            if (buf == null || cookieProtection == CookieProtection.None) 
                return buf;
            if (cookieProtection == CookieProtection.All || cookieProtection == CookieProtection.Encryption) 
            {
                buf = MachineKeySection.EncryptOrDecryptData (false, buf, null, 0, buf.Length);
                if (buf == null)
                    return null; 
            }
 
            if (cookieProtection == CookieProtection.All || cookieProtection == CookieProtection.Validation) 
            {
                ////////////////////////////////////////////////////////////////////// 
                // Step 2: Get the MAC: Last 20 bytes
                if (buf.Length <= 20)
                    return null;
 
                byte[] buf2 = new byte[buf.Length - 20];
                Buffer.BlockCopy (buf, 0, buf2, 0, buf2.Length); 
                byte[] bMac = MachineKeySection.HashData (buf2, null, 0, buf2.Length); 

                ////////////////////////////////////////////////////////////////////// 
                // Step 3: Make sure the MAC is correct
                if (bMac == null || bMac.Length != 20)
                    return null;
 
                for (int iter = 0; iter < 20; iter++)
                    if (bMac[iter] != buf[buf2.Length + iter]) 
                        return null; 

                buf = buf2; 
            }
            return buf;
        }
    } 
}
