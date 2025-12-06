//------------------------------------------------------------------------------ 
// <copyright file="TimeSpanMinutesConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

using System; 
using System.Collections; 
using System.IO;
using System.Reflection; 
using System.Security.Permissions;
using System.Xml;
using System.Collections.Specialized;
using System.Globalization; 
using System.ComponentModel;
using System.Security; 
using System.Text; 
using System.Configuration;
 
namespace System.Web.Configuration {

    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public sealed class MachineKeyValidationConverter : ConfigurationConverterBase {
 
        public override object ConvertTo(ITypeDescriptorContext ctx, CultureInfo ci, object value, Type type) { 
            if ((value != null) && (value.GetType() != typeof(MachineKeyValidation))) {
                throw new ArgumentException(SR.GetString(SR.Invalid_enum_value, "SHA1, MD5, 3DES, AES")); 
            }

            switch ((MachineKeyValidation)value) {
                case MachineKeyValidation.SHA1: 
                    return (string) "SHA1";
                case MachineKeyValidation.MD5: 
                    return (string)"MD5"; 
                case MachineKeyValidation.TripleDES:
                    return (string)"3DES"; 
                case MachineKeyValidation.AES:
                    return (string)"AES";
                default:
                    throw new ArgumentOutOfRangeException("value"); 
            }
        } 
 
        public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data) {
            //            Debug.Assert( data is string ); 

            string s = (string)data;
            switch (s) {
                case "SHA1": 
                    return MachineKeyValidation.SHA1;
                case "MD5": 
                    return MachineKeyValidation.MD5; 
                case "3DES":
                    return MachineKeyValidation.TripleDES; 
                case "AES":
                    return MachineKeyValidation.AES;
                default:
                    throw new ArgumentException(SR.GetString(SR.Config_Invalid_enum_value, "SHA1, MD5, 3DES, AES")); 
            }
        } 
    } 
}
//------------------------------------------------------------------------------ 
// <copyright file="TimeSpanMinutesConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

using System; 
using System.Collections; 
using System.IO;
using System.Reflection; 
using System.Security.Permissions;
using System.Xml;
using System.Collections.Specialized;
using System.Globalization; 
using System.ComponentModel;
using System.Security; 
using System.Text; 
using System.Configuration;
 
namespace System.Web.Configuration {

    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public sealed class MachineKeyValidationConverter : ConfigurationConverterBase {
 
        public override object ConvertTo(ITypeDescriptorContext ctx, CultureInfo ci, object value, Type type) { 
            if ((value != null) && (value.GetType() != typeof(MachineKeyValidation))) {
                throw new ArgumentException(SR.GetString(SR.Invalid_enum_value, "SHA1, MD5, 3DES, AES")); 
            }

            switch ((MachineKeyValidation)value) {
                case MachineKeyValidation.SHA1: 
                    return (string) "SHA1";
                case MachineKeyValidation.MD5: 
                    return (string)"MD5"; 
                case MachineKeyValidation.TripleDES:
                    return (string)"3DES"; 
                case MachineKeyValidation.AES:
                    return (string)"AES";
                default:
                    throw new ArgumentOutOfRangeException("value"); 
            }
        } 
 
        public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data) {
            //            Debug.Assert( data is string ); 

            string s = (string)data;
            switch (s) {
                case "SHA1": 
                    return MachineKeyValidation.SHA1;
                case "MD5": 
                    return MachineKeyValidation.MD5; 
                case "3DES":
                    return MachineKeyValidation.TripleDES; 
                case "AES":
                    return MachineKeyValidation.AES;
                default:
                    throw new ArgumentException(SR.GetString(SR.Config_Invalid_enum_value, "SHA1, MD5, 3DES, AES")); 
            }
        } 
    } 
}
