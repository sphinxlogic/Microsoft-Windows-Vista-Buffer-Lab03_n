//------------------------------------------------------------------------------ 
// <copyright file="Membership.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Security { 
    using  System.Web; 
    using  System.Web.Configuration;
    using  System.Security.Principal; 
    using  System.Security.Permissions;
    using  System.Globalization;
    using  System.Runtime.Serialization;
    using  System.Collections; 
    using  System.Security.Cryptography;
    using  System.Configuration.Provider; 
    using  System.Text; 
    using  System.Configuration;
    using  System.Web.Management; 
    using  System.Web.Hosting;
    using  System.Threading;
    using  System.Web.Util;
    using  System.Collections.Specialized; 

 
    /// <devdoc> 
    ///    <para>[To be supplied.]</para>
    /// </devdoc> 
    // [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    // This has no hosting permission demands because of DevDiv Bugs 31461: ClientAppSvcs: ASP.net Provider support
    public static class Membership
    { 

        public static bool   EnablePasswordRetrieval   { get { Initialize(); return Provider.EnablePasswordRetrieval;}} 
 
        public static bool   EnablePasswordReset       { get { Initialize(); return Provider.EnablePasswordReset;}}
 
        public static bool   RequiresQuestionAndAnswer   { get { Initialize(); return Provider.RequiresQuestionAndAnswer;}}

        public static int    UserIsOnlineTimeWindow      { get { Initialize(); return s_UserIsOnlineTimeWindow; }}
 

        public static MembershipProviderCollection    Providers    { get { Initialize(); return s_Providers; }} 
 
        public static MembershipProvider             Provider     { get { Initialize(); return s_Provider;  }}
 
        public static string HashAlgorithmType { get { Initialize(); return s_HashAlgorithmType; }}
        internal static bool   IsHashAlgorithmFromMembershipConfig { get { Initialize(); return s_HashAlgorithmFromConfig; }}

        public static int MaxInvalidPasswordAttempts 
        {
            get 
            { 
                Initialize();
 
                return Provider.MaxInvalidPasswordAttempts;
            }
        }
 
        public static int PasswordAttemptWindow
        { 
            get 
            {
                Initialize(); 

                return Provider.PasswordAttemptWindow;
            }
        } 

        public static int MinRequiredPasswordLength 
        { 
            get
            { 
                Initialize();

                return Provider.MinRequiredPasswordLength;
            } 
        }
 
        public static int MinRequiredNonAlphanumericCharacters 
        {
            get 
            {
                Initialize();

                return Provider.MinRequiredNonAlphanumericCharacters; 
            }
        } 
 
        public static string PasswordStrengthRegularExpression
        { 
            get
            {
                Initialize();
 
                return Provider.PasswordStrengthRegularExpression;
            } 
        } 

 
        public static string ApplicationName
        {
            get { return Provider.ApplicationName; }
            set { Provider.ApplicationName = value; } 
        }
 
 
        public static MembershipUser CreateUser(string username, string password)
        { 
            return CreateUser(username, password, null);
        }

 
        public static MembershipUser CreateUser(string username, string password, string email)
        { 
            MembershipCreateStatus status; 
            MembershipUser u = CreateUser(username, password, email,null,null,true, out status);
            if (u == null) 
                throw new MembershipCreateUserException(status);
            return u;
        }
 

        public static MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, out MembershipCreateStatus status) 
        { 
            return CreateUser(username, password, email, passwordQuestion, passwordAnswer, isApproved, null, out status);
        } 

        public static MembershipUser CreateUser( string username, string password,  string email, string passwordQuestion,string passwordAnswer,
                                                 bool   isApproved, object providerUserKey, out MembershipCreateStatus status )
        { 
            if( !SecUtility.ValidateParameter(ref username,  true,  true, true, 0))
            { 
                status = MembershipCreateStatus.InvalidUserName; 
                return null;
            } 

            if( !SecUtility.ValidatePasswordParameter(ref password, 0))
            {
                status = MembershipCreateStatus.InvalidPassword; 
                return null;
            } 
 

            if( !SecUtility.ValidateParameter( ref email, false, false, false, 0)) 
            {
                status = MembershipCreateStatus.InvalidEmail;
                return null;
            } 

            if( !SecUtility.ValidateParameter(ref passwordQuestion, false, true, false, 0)) 
            { 
                status = MembershipCreateStatus.InvalidQuestion;
                return null; 
            }

            if( !SecUtility.ValidateParameter(ref passwordAnswer, false, true, false, 0))
            { 
                status = MembershipCreateStatus.InvalidAnswer;
                return null; 
            } 

            return Provider.CreateUser( username, password, email, passwordQuestion, passwordAnswer, isApproved, providerUserKey, out status); 
        }


        public static bool ValidateUser(string username, string password) 
        {
            return Provider.ValidateUser(username, password); 
            /* 
            if (retVal) {
                PerfCounters.IncrementCounter(AppPerfCounter.MEMBER_SUCCESS); 
                WebBaseEvent.RaiseSystemEvent(null, WebEventCodes.AuditMembershipAuthenticationSuccess, username);
            }
            else {
                PerfCounters.IncrementCounter(AppPerfCounter.MEMBER_FAIL); 
                WebBaseEvent.RaiseSystemEvent(null, WebEventCodes.AuditMembershipAuthenticationFailure, username);
            } 
 
            return retVal;
             */ 
        }


        public static MembershipUser GetUser() 
        {
            return GetUser(GetCurrentUserName(), true); 
        } 

 
        public static MembershipUser GetUser(bool userIsOnline)
        {
            return GetUser(GetCurrentUserName(), userIsOnline);
        } 

 
        public static MembershipUser GetUser(string username) 
        {
            return GetUser(username, false); 
        }


        public static MembershipUser GetUser(string username, bool userIsOnline) 
        {
            SecUtility.CheckParameter( ref username, true, false, true, 0, "username" ); 
 
            return Provider.GetUser(username, userIsOnline);
        } 

        public static MembershipUser GetUser( object providerUserKey )
        {
            return GetUser( providerUserKey, false); 
        }
 
        public static MembershipUser GetUser( object providerUserKey, bool userIsOnline ) 
        {
            if( providerUserKey == null ) 
            {
                throw new ArgumentNullException( "providerUserKey" );
            }
 

            return Provider.GetUser( providerUserKey, userIsOnline); 
        } 

 
        public static string GetUserNameByEmail( string emailToMatch )
        {
            SecUtility.CheckParameter( ref emailToMatch,
                                       false, 
                                       false,
                                       false, 
                                       0, 
                                       "emailToMatch" );
 
            return Provider.GetUserNameByEmail( emailToMatch );
        }

 
        public static bool DeleteUser(string username)
        { 
            SecUtility.CheckParameter( ref username, true, true, true, 0, "username" ); 
            return Provider.DeleteUser( username, true );
        } 


        public static bool DeleteUser(string username, bool deleteAllRelatedData)
        { 
            SecUtility.CheckParameter( ref username, true, true, true, 0, "username" );
            return Provider.DeleteUser( username, deleteAllRelatedData ); 
        } 

 
        public static void UpdateUser( MembershipUser user )
        {
            if( user == null )
            { 
                throw new ArgumentNullException( "user" );
            } 
 
            user.Update();
        } 


        public static MembershipUserCollection GetAllUsers()
        { 
            int totalRecords = 0;
            return GetAllUsers( 0, Int32.MaxValue, out totalRecords); 
        } 

        public static MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords) 
        {
            if ( pageIndex < 0 )
            {
                throw new ArgumentException(SR.GetString(SR.PageIndex_bad), "pageIndex"); 
            }
 
            if ( pageSize < 1 ) 
            {
                throw new ArgumentException(SR.GetString(SR.PageSize_bad), "pageSize"); 
            }

            return Provider.GetAllUsers(pageIndex, pageSize, out totalRecords);
        } 

 
        public static int GetNumberOfUsersOnline() { 
            return Provider.GetNumberOfUsersOnline();
        } 

        private static char [] punctuations = "!@#$%^&*()_-+=[{]};:>|./?".ToCharArray();

 
        public static string GeneratePassword(int length, int numberOfNonAlphanumericCharacters) {
            if (length < 1 || length > 128) 
            { 
                throw new ArgumentException(SR.GetString(SR.Membership_password_length_incorrect));
            } 

            if( numberOfNonAlphanumericCharacters > length || numberOfNonAlphanumericCharacters < 0 )
            {
                throw new ArgumentException(SR.GetString(SR.Membership_min_required_non_alphanumeric_characters_incorrect, 
                                                         "numberOfNonAlphanumericCharacters"));
            } 
 
            string password;
            int    index; 
            byte[] buf;
            char[] cBuf;
            int count;
 
            do {
                buf = new byte[length]; 
                cBuf = new char[length]; 
                count = 0;
 
                (new RNGCryptoServiceProvider()).GetBytes(buf);

                for(int iter=0; iter<length; iter++)
                { 
                    int i = (int) (buf[iter] % 87);
                    if (i < 10) 
                        cBuf[iter] = (char) ('0' + i); 
                    else if (i < 36)
                        cBuf[iter] = (char) ('A' + i - 10); 
                    else if (i < 62)
                        cBuf[iter] = (char) ('a' + i - 36);
                    else
                    { 
                        cBuf[iter] = punctuations[i-62];
                        count++; 
                    } 
                }
 
                if( count < numberOfNonAlphanumericCharacters )
                {
                    int j, k;
                    Random rand = new Random(); 

                    for( j = 0; j < numberOfNonAlphanumericCharacters - count; j++ ) 
                    { 
                        do
                        { 
                            k = rand.Next( 0, length );
                        }
                        while( !Char.IsLetterOrDigit( cBuf[k] ) );
 
                        cBuf[k] = punctuations[rand.Next(0, punctuations.Length)];
                    } 
                } 

                password = new string(cBuf); 
            }
            while(CrossSiteScriptingValidation.IsDangerousString(password, out index));

            return password; 
        }
 
        private static void Initialize() 
        {
            if (s_Initialized) { 
                if (s_InitializeException != null)
                    throw s_InitializeException;
                return;
            } 
            if (s_InitializeException != null)
                throw s_InitializeException; 
 
            if (HostingEnvironment.IsHosted)
                HttpRuntime.CheckAspNetHostingPermission(AspNetHostingPermissionLevel.Low, SR.Feature_not_supported_at_this_level); 

            lock (s_lock) {
                if (s_Initialized) {
                    if (s_InitializeException != null) 
                        throw s_InitializeException;
                    return; 
                } 

                try { 
                    RuntimeConfig appConfig = RuntimeConfig.GetAppConfig();
                    MembershipSection settings = appConfig.Membership;
                    MachineKeySection keySection;
                    MachineKeyValidation validation; 
                    TimeSpan timeWindow;
                    string hashAlgorithm; 
 
                    if (settings.DefaultProvider == null || settings.Providers == null || settings.Providers.Count < 1)
                        throw new ProviderException(SR.GetString(SR.Def_membership_provider_not_specified)); 

                    hashAlgorithm = settings.HashAlgorithmType;
                    if (String.IsNullOrEmpty(hashAlgorithm)) {
                        // If no hash algorithm is specified, use the same as the "validation" in "<machineKey>". 
                        // If the validation is "3DES", switch it to use "SHA1" instead.
                        s_HashAlgorithmFromConfig = false; 
 
                        s_HashAlgorithmType = "SHA1";
                        keySection = appConfig.MachineKey; 
                        if (keySection != null) {
                            validation = keySection.Validation;

                            // MachineKeyValidation enum has only 3 values, so if it's MD5, switch the 
                            // algorithm type to it.  Else, leave it as SHA1 (specified above).
                            if (validation == MachineKeyValidation.MD5) { 
                                s_HashAlgorithmType = "MD5"; 
                            }
                        } 
                    }
                    else {
                        s_HashAlgorithmType = hashAlgorithm;
                        s_HashAlgorithmFromConfig = true; 
                    }
 
                    s_Providers = new MembershipProviderCollection(); 
                    if (HostingEnvironment.IsHosted) {
                        ProvidersHelper.InstantiateProviders(settings.Providers, s_Providers, typeof(MembershipProvider)); 
                    } else {
                        foreach (ProviderSettings ps in settings.Providers) {
                            Type t = Type.GetType(ps.Type, true, true);
                            if (!typeof(MembershipProvider).IsAssignableFrom(t)) 
                                throw new ArgumentException(SR.GetString(SR.Provider_must_implement_type, typeof(MembershipProvider).ToString()));
                            MembershipProvider provider = (MembershipProvider)Activator.CreateInstance(t); 
                            NameValueCollection pars = ps.Parameters; 
                            NameValueCollection cloneParams = new NameValueCollection(pars.Count, StringComparer.Ordinal);
                            foreach (string key in pars) 
                                cloneParams[key] = pars[key];
                            provider.Initialize(ps.Name, cloneParams);
                            s_Providers.Add(provider);
                        } 
                    }
                    s_Provider = s_Providers[settings.DefaultProvider]; 
                    if (s_Provider == null) 
                    {
                        throw new ConfigurationErrorsException(SR.GetString(SR.Def_membership_provider_not_found), settings.ElementInformation.Properties["defaultProvider"].Source, settings.ElementInformation.Properties["defaultProvider"].LineNumber); 
                    }
                    timeWindow = settings.UserIsOnlineTimeWindow;
                    s_UserIsOnlineTimeWindow = (int) timeWindow.TotalMinutes;
                    s_Providers.SetReadOnly(); 
                } catch (Exception e) {
                    s_InitializeException = e; 
                    throw; 
                }
                s_Initialized = true; 
            }
        }

 
        public static MembershipUserCollection FindUsersByName( string usernameToMatch,
                                                                int pageIndex, 
                                                                int pageSize, 
                                                                out int totalRecords )
        { 
            SecUtility.CheckParameter( ref usernameToMatch,
                                       true,
                                       true,
                                       false, 
                                       0,
                                       "usernameToMatch" ); 
 
            if ( pageIndex < 0 )
            { 
                throw new ArgumentException(SR.GetString(SR.PageIndex_bad), "pageIndex");
            }

            if ( pageSize < 1 ) 
            {
                throw new ArgumentException(SR.GetString(SR.PageSize_bad), "pageSize"); 
            } 

            return Provider.FindUsersByName( usernameToMatch, 
                                             pageIndex,
                                             pageSize,
                                             out totalRecords);
        } 

 
        public static MembershipUserCollection FindUsersByName( string usernameToMatch ) 
        {
            SecUtility.CheckParameter( ref usernameToMatch, 
                                       true,
                                       true,
                                       false,
                                       0, 
                                       "usernameToMatch" );
 
            int totalRecords = 0; 
            return Provider.FindUsersByName( usernameToMatch,
                                             0, 
                                             Int32.MaxValue,
                                             out totalRecords );
        }
 
        public static MembershipUserCollection FindUsersByEmail( string  emailToMatch,
                                                                 int     pageIndex, 
                                                                 int     pageSize, 
                                                                 out int totalRecords )
        { 
            SecUtility.CheckParameter( ref emailToMatch,
                                       false,
                                       false,
                                       false, 
                                       0,
                                       "emailToMatch" ); 
 
            if ( pageIndex < 0 )
            { 
                throw new ArgumentException(SR.GetString(SR.PageIndex_bad), "pageIndex");
            }

            if ( pageSize < 1 ) 
            {
                throw new ArgumentException(SR.GetString(SR.PageSize_bad), "pageSize"); 
            } 

            return Provider.FindUsersByEmail( emailToMatch, 
                                              pageIndex,
                                              pageSize,
                                              out totalRecords );
        } 

        public static MembershipUserCollection FindUsersByEmail(string emailToMatch) 
        { 
            SecUtility.CheckParameter( ref emailToMatch,
                                       false, 
                                       false,
                                       false,
                                       0,
                                       "emailToMatch" ); 

            int totalRecords = 0; 
            return FindUsersByEmail(emailToMatch, 0, Int32.MaxValue, out totalRecords); 
        }
 
        private static string GetCurrentUserName()
        {
            if (HostingEnvironment.IsHosted) {
                HttpContext cur = HttpContext.Current; 
                if (cur != null)
                    return cur.User.Identity.Name; 
            } 
            IPrincipal user = Thread.CurrentPrincipal;
            if (user == null || user.Identity == null) 
                return String.Empty;
            else
                return user.Identity.Name;
        } 

        public static event MembershipValidatePasswordEventHandler ValidatingPassword 
        { 
            add
            { 
                Provider.ValidatingPassword += value;
            }
            remove
            { 
                Provider.ValidatingPassword -= value;
            } 
        } 

        private static MembershipProviderCollection   s_Providers; 
        private static MembershipProvider             s_Provider;
        private static int                            s_UserIsOnlineTimeWindow = 15;
        private static object                         s_lock = new object();
        private static bool                           s_Initialized = false; 
        private static Exception                      s_InitializeException = null;
        private static string                         s_HashAlgorithmType; 
        private static bool                           s_HashAlgorithmFromConfig; 
    }
 
    ////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////// 

 
    public enum MembershipCreateStatus { 

        Success                  = 0, 

        InvalidUserName          = 1,  // invalid user name

        InvalidPassword          = 2, // new password was not accepted (invalid format) 

        InvalidQuestion          = 3, // new question was not accepted (invalid format) 
 
        InvalidAnswer            = 4, // new passwordAnswer was not acceppted (invalid format)
 
        InvalidEmail             = 5, // new email was not accepted (invalid format)

        DuplicateUserName        = 6, // username already exists
 
        DuplicateEmail           = 7, // email already exists
 
        UserRejected             = 8, // provider rejected user (for some user-specific reason) 

        InvalidProviderUserKey   = 9,  // new provider user key was not accepted (invalid format) 
        DuplicateProviderUserKey = 10, // provider user key already exists
        ProviderError            = 11  // provider-specific error (couldn't map onto this enumeration)
    }
 

 
   // [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
   // This has no hosting permission demands because of DevDiv Bugs 31461: ClientAppSvcs: ASP.net Provider support
   public sealed class MembershipProviderCollection : ProviderCollection 
   {

        public override void Add(ProviderBase provider) {
            if( provider == null ) 
            {
                throw new ArgumentNullException( "provider" ); 
            } 

            if( !( provider is MembershipProvider ) ) 
            {
                throw new ArgumentException(SR.GetString(SR.Provider_must_implement_type, typeof(MembershipProvider).ToString()), "provider");
            }
 
            base.Add( provider );
        } 
 
       new public MembershipProvider this[string name] {
           get { 
               return (MembershipProvider) base[name];
           }
       }
 
       public void CopyTo(MembershipProvider[] array, int index)
       { 
           base.CopyTo(array, index); 
       }
   } 


   [Serializable]
   [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
   public sealed class MembershipUserCollection : IEnumerable, ICollection
    { 
        private Hashtable _Indices  = null; 
        private ArrayList _Values   = null;
        private bool      _ReadOnly = false; 



        public MembershipUserCollection() { 
            _Indices = new Hashtable(10, StringComparer.CurrentCultureIgnoreCase);
            _Values  = new ArrayList(); 
        } 
        private MembershipUserCollection(Hashtable indices, ArrayList values){
            _Indices = (Hashtable) indices.Clone(); 
            _Values = (ArrayList) values.Clone();
        }

 
        public void Add(MembershipUser user)
        { 
            if( user == null ) 
            {
                throw new ArgumentNullException( "user" ); 
            }

            if (_ReadOnly)
                throw new NotSupportedException(); 

            int pos = _Values.Add(user); 
            try { 
                _Indices.Add(user.UserName, pos);
            } 
            catch {
                _Values.RemoveAt(pos);
                throw;
            } 
        }
 
        public void Remove(string name) 
        {
            if (_ReadOnly) 
                throw new NotSupportedException();

            object pos = _Indices[name];
            if (pos == null || !(pos is int)) 
                return;
            int ipos = (int) pos; 
            if (ipos >= _Values.Count) 
                return;
            _Values.RemoveAt(ipos); 
            _Indices.Remove(name);
            ArrayList al = new ArrayList();
            foreach(DictionaryEntry de in _Indices)
                if ((int)de.Value > ipos) 
                    al.Add(de.Key);
            foreach(string key in al) 
                _Indices[key] = ((int) _Indices[key]) - 1; 
        }
 
        public MembershipUser this[string name]
        {
            get {
                object pos = _Indices[name]; 
                if (pos == null || !(pos is int))
                    return null; 
                int ipos = (int) pos; 
                if (ipos >= _Values.Count)
                    return null; 
                return (MembershipUser) _Values[ipos];
            }
        }
 
        public IEnumerator GetEnumerator() {
            return _Values.GetEnumerator(); 
        } 

        public void SetReadOnly() { 
            if (_ReadOnly)
                return;
            _ReadOnly = true;
            _Values = ArrayList.ReadOnly(_Values); 
        }
 
        public void Clear() { 
            _Values.Clear();
            _Indices.Clear(); 
        }

        public int Count { get { return _Values.Count; }}
 
        public bool IsSynchronized {get { return false;}}
 
        public object SyncRoot {get { return this;}} 

 
        void ICollection.CopyTo(Array array, int index)
        {
            _Values.CopyTo(array, index);
        } 

        public void CopyTo(MembershipUser [] array, int index) 
        { 
            _Values.CopyTo(array, index);
        } 
    }


    [Serializable] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class MembershipCreateUserException : Exception 
    {
 
        public MembershipCreateUserException(MembershipCreateStatus statusCode)
            : base(GetMessageFromStatusCode(statusCode))
        {
            _StatusCode = statusCode; 
        }
 
        public MembershipCreateUserException(String message) : base(message) 
        { }
 

        protected MembershipCreateUserException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _StatusCode = (MembershipCreateStatus)info.GetInt32("_StatusCode"); 
        }
 
 
        public MembershipCreateUserException()
        { } 

        public MembershipCreateUserException(String message, Exception innerException) : base(message, innerException)
        { }
 
        private MembershipCreateStatus _StatusCode = MembershipCreateStatus.ProviderError;
 
 
        public  MembershipCreateStatus StatusCode { get { return _StatusCode; }}
 
        [SecurityPermission(SecurityAction.Demand, Flags=SecurityPermissionFlag.SerializationFormatter, SerializationFormatter=true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);
            info.AddValue("_StatusCode", _StatusCode); 
        }
 
        internal static string GetMessageFromStatusCode(MembershipCreateStatus statusCode) { 
            string msgKey = SR.Provider_Error;
            switch(statusCode) 
            {
            case MembershipCreateStatus.Success:
                 msgKey = SR.Membership_No_error;
                 break; 
            case MembershipCreateStatus.InvalidUserName:
                msgKey = SR.Membership_InvalidUserName; 
                break; 
            case MembershipCreateStatus.InvalidPassword:
                msgKey = SR.Membership_InvalidPassword; 
                break;
            case MembershipCreateStatus.InvalidQuestion:
                msgKey = SR.Membership_InvalidQuestion;
                break; 
            case MembershipCreateStatus.InvalidAnswer:
                msgKey = SR.Membership_InvalidAnswer; 
                break; 
            case MembershipCreateStatus.InvalidEmail:
                msgKey = SR.Membership_InvalidEmail; 
                break;
            case MembershipCreateStatus.InvalidProviderUserKey:
                msgKey = SR.Membership_InvalidProviderUserKey;
                break; 
            case MembershipCreateStatus.DuplicateUserName:
                msgKey = SR.Membership_DuplicateUserName; 
                break; 
            case MembershipCreateStatus.DuplicateEmail:
                msgKey = SR.Membership_DuplicateEmail; 
                break;
            case MembershipCreateStatus.DuplicateProviderUserKey:
                msgKey = SR.Membership_DuplicateProviderUserKey;
                break; 
            case MembershipCreateStatus.UserRejected:
                msgKey = SR.Membership_UserRejected; 
                break; 
            }
            return SR.GetString(msgKey); 
        }

    }
 

    public enum MembershipPasswordFormat { 
 
        Clear = 0, Hashed = 1, Encrypted = 2
    } 

}

//------------------------------------------------------------------------------ 
// <copyright file="Membership.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Security { 
    using  System.Web; 
    using  System.Web.Configuration;
    using  System.Security.Principal; 
    using  System.Security.Permissions;
    using  System.Globalization;
    using  System.Runtime.Serialization;
    using  System.Collections; 
    using  System.Security.Cryptography;
    using  System.Configuration.Provider; 
    using  System.Text; 
    using  System.Configuration;
    using  System.Web.Management; 
    using  System.Web.Hosting;
    using  System.Threading;
    using  System.Web.Util;
    using  System.Collections.Specialized; 

 
    /// <devdoc> 
    ///    <para>[To be supplied.]</para>
    /// </devdoc> 
    // [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    // This has no hosting permission demands because of DevDiv Bugs 31461: ClientAppSvcs: ASP.net Provider support
    public static class Membership
    { 

        public static bool   EnablePasswordRetrieval   { get { Initialize(); return Provider.EnablePasswordRetrieval;}} 
 
        public static bool   EnablePasswordReset       { get { Initialize(); return Provider.EnablePasswordReset;}}
 
        public static bool   RequiresQuestionAndAnswer   { get { Initialize(); return Provider.RequiresQuestionAndAnswer;}}

        public static int    UserIsOnlineTimeWindow      { get { Initialize(); return s_UserIsOnlineTimeWindow; }}
 

        public static MembershipProviderCollection    Providers    { get { Initialize(); return s_Providers; }} 
 
        public static MembershipProvider             Provider     { get { Initialize(); return s_Provider;  }}
 
        public static string HashAlgorithmType { get { Initialize(); return s_HashAlgorithmType; }}
        internal static bool   IsHashAlgorithmFromMembershipConfig { get { Initialize(); return s_HashAlgorithmFromConfig; }}

        public static int MaxInvalidPasswordAttempts 
        {
            get 
            { 
                Initialize();
 
                return Provider.MaxInvalidPasswordAttempts;
            }
        }
 
        public static int PasswordAttemptWindow
        { 
            get 
            {
                Initialize(); 

                return Provider.PasswordAttemptWindow;
            }
        } 

        public static int MinRequiredPasswordLength 
        { 
            get
            { 
                Initialize();

                return Provider.MinRequiredPasswordLength;
            } 
        }
 
        public static int MinRequiredNonAlphanumericCharacters 
        {
            get 
            {
                Initialize();

                return Provider.MinRequiredNonAlphanumericCharacters; 
            }
        } 
 
        public static string PasswordStrengthRegularExpression
        { 
            get
            {
                Initialize();
 
                return Provider.PasswordStrengthRegularExpression;
            } 
        } 

 
        public static string ApplicationName
        {
            get { return Provider.ApplicationName; }
            set { Provider.ApplicationName = value; } 
        }
 
 
        public static MembershipUser CreateUser(string username, string password)
        { 
            return CreateUser(username, password, null);
        }

 
        public static MembershipUser CreateUser(string username, string password, string email)
        { 
            MembershipCreateStatus status; 
            MembershipUser u = CreateUser(username, password, email,null,null,true, out status);
            if (u == null) 
                throw new MembershipCreateUserException(status);
            return u;
        }
 

        public static MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, out MembershipCreateStatus status) 
        { 
            return CreateUser(username, password, email, passwordQuestion, passwordAnswer, isApproved, null, out status);
        } 

        public static MembershipUser CreateUser( string username, string password,  string email, string passwordQuestion,string passwordAnswer,
                                                 bool   isApproved, object providerUserKey, out MembershipCreateStatus status )
        { 
            if( !SecUtility.ValidateParameter(ref username,  true,  true, true, 0))
            { 
                status = MembershipCreateStatus.InvalidUserName; 
                return null;
            } 

            if( !SecUtility.ValidatePasswordParameter(ref password, 0))
            {
                status = MembershipCreateStatus.InvalidPassword; 
                return null;
            } 
 

            if( !SecUtility.ValidateParameter( ref email, false, false, false, 0)) 
            {
                status = MembershipCreateStatus.InvalidEmail;
                return null;
            } 

            if( !SecUtility.ValidateParameter(ref passwordQuestion, false, true, false, 0)) 
            { 
                status = MembershipCreateStatus.InvalidQuestion;
                return null; 
            }

            if( !SecUtility.ValidateParameter(ref passwordAnswer, false, true, false, 0))
            { 
                status = MembershipCreateStatus.InvalidAnswer;
                return null; 
            } 

            return Provider.CreateUser( username, password, email, passwordQuestion, passwordAnswer, isApproved, providerUserKey, out status); 
        }


        public static bool ValidateUser(string username, string password) 
        {
            return Provider.ValidateUser(username, password); 
            /* 
            if (retVal) {
                PerfCounters.IncrementCounter(AppPerfCounter.MEMBER_SUCCESS); 
                WebBaseEvent.RaiseSystemEvent(null, WebEventCodes.AuditMembershipAuthenticationSuccess, username);
            }
            else {
                PerfCounters.IncrementCounter(AppPerfCounter.MEMBER_FAIL); 
                WebBaseEvent.RaiseSystemEvent(null, WebEventCodes.AuditMembershipAuthenticationFailure, username);
            } 
 
            return retVal;
             */ 
        }


        public static MembershipUser GetUser() 
        {
            return GetUser(GetCurrentUserName(), true); 
        } 

 
        public static MembershipUser GetUser(bool userIsOnline)
        {
            return GetUser(GetCurrentUserName(), userIsOnline);
        } 

 
        public static MembershipUser GetUser(string username) 
        {
            return GetUser(username, false); 
        }


        public static MembershipUser GetUser(string username, bool userIsOnline) 
        {
            SecUtility.CheckParameter( ref username, true, false, true, 0, "username" ); 
 
            return Provider.GetUser(username, userIsOnline);
        } 

        public static MembershipUser GetUser( object providerUserKey )
        {
            return GetUser( providerUserKey, false); 
        }
 
        public static MembershipUser GetUser( object providerUserKey, bool userIsOnline ) 
        {
            if( providerUserKey == null ) 
            {
                throw new ArgumentNullException( "providerUserKey" );
            }
 

            return Provider.GetUser( providerUserKey, userIsOnline); 
        } 

 
        public static string GetUserNameByEmail( string emailToMatch )
        {
            SecUtility.CheckParameter( ref emailToMatch,
                                       false, 
                                       false,
                                       false, 
                                       0, 
                                       "emailToMatch" );
 
            return Provider.GetUserNameByEmail( emailToMatch );
        }

 
        public static bool DeleteUser(string username)
        { 
            SecUtility.CheckParameter( ref username, true, true, true, 0, "username" ); 
            return Provider.DeleteUser( username, true );
        } 


        public static bool DeleteUser(string username, bool deleteAllRelatedData)
        { 
            SecUtility.CheckParameter( ref username, true, true, true, 0, "username" );
            return Provider.DeleteUser( username, deleteAllRelatedData ); 
        } 

 
        public static void UpdateUser( MembershipUser user )
        {
            if( user == null )
            { 
                throw new ArgumentNullException( "user" );
            } 
 
            user.Update();
        } 


        public static MembershipUserCollection GetAllUsers()
        { 
            int totalRecords = 0;
            return GetAllUsers( 0, Int32.MaxValue, out totalRecords); 
        } 

        public static MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords) 
        {
            if ( pageIndex < 0 )
            {
                throw new ArgumentException(SR.GetString(SR.PageIndex_bad), "pageIndex"); 
            }
 
            if ( pageSize < 1 ) 
            {
                throw new ArgumentException(SR.GetString(SR.PageSize_bad), "pageSize"); 
            }

            return Provider.GetAllUsers(pageIndex, pageSize, out totalRecords);
        } 

 
        public static int GetNumberOfUsersOnline() { 
            return Provider.GetNumberOfUsersOnline();
        } 

        private static char [] punctuations = "!@#$%^&*()_-+=[{]};:>|./?".ToCharArray();

 
        public static string GeneratePassword(int length, int numberOfNonAlphanumericCharacters) {
            if (length < 1 || length > 128) 
            { 
                throw new ArgumentException(SR.GetString(SR.Membership_password_length_incorrect));
            } 

            if( numberOfNonAlphanumericCharacters > length || numberOfNonAlphanumericCharacters < 0 )
            {
                throw new ArgumentException(SR.GetString(SR.Membership_min_required_non_alphanumeric_characters_incorrect, 
                                                         "numberOfNonAlphanumericCharacters"));
            } 
 
            string password;
            int    index; 
            byte[] buf;
            char[] cBuf;
            int count;
 
            do {
                buf = new byte[length]; 
                cBuf = new char[length]; 
                count = 0;
 
                (new RNGCryptoServiceProvider()).GetBytes(buf);

                for(int iter=0; iter<length; iter++)
                { 
                    int i = (int) (buf[iter] % 87);
                    if (i < 10) 
                        cBuf[iter] = (char) ('0' + i); 
                    else if (i < 36)
                        cBuf[iter] = (char) ('A' + i - 10); 
                    else if (i < 62)
                        cBuf[iter] = (char) ('a' + i - 36);
                    else
                    { 
                        cBuf[iter] = punctuations[i-62];
                        count++; 
                    } 
                }
 
                if( count < numberOfNonAlphanumericCharacters )
                {
                    int j, k;
                    Random rand = new Random(); 

                    for( j = 0; j < numberOfNonAlphanumericCharacters - count; j++ ) 
                    { 
                        do
                        { 
                            k = rand.Next( 0, length );
                        }
                        while( !Char.IsLetterOrDigit( cBuf[k] ) );
 
                        cBuf[k] = punctuations[rand.Next(0, punctuations.Length)];
                    } 
                } 

                password = new string(cBuf); 
            }
            while(CrossSiteScriptingValidation.IsDangerousString(password, out index));

            return password; 
        }
 
        private static void Initialize() 
        {
            if (s_Initialized) { 
                if (s_InitializeException != null)
                    throw s_InitializeException;
                return;
            } 
            if (s_InitializeException != null)
                throw s_InitializeException; 
 
            if (HostingEnvironment.IsHosted)
                HttpRuntime.CheckAspNetHostingPermission(AspNetHostingPermissionLevel.Low, SR.Feature_not_supported_at_this_level); 

            lock (s_lock) {
                if (s_Initialized) {
                    if (s_InitializeException != null) 
                        throw s_InitializeException;
                    return; 
                } 

                try { 
                    RuntimeConfig appConfig = RuntimeConfig.GetAppConfig();
                    MembershipSection settings = appConfig.Membership;
                    MachineKeySection keySection;
                    MachineKeyValidation validation; 
                    TimeSpan timeWindow;
                    string hashAlgorithm; 
 
                    if (settings.DefaultProvider == null || settings.Providers == null || settings.Providers.Count < 1)
                        throw new ProviderException(SR.GetString(SR.Def_membership_provider_not_specified)); 

                    hashAlgorithm = settings.HashAlgorithmType;
                    if (String.IsNullOrEmpty(hashAlgorithm)) {
                        // If no hash algorithm is specified, use the same as the "validation" in "<machineKey>". 
                        // If the validation is "3DES", switch it to use "SHA1" instead.
                        s_HashAlgorithmFromConfig = false; 
 
                        s_HashAlgorithmType = "SHA1";
                        keySection = appConfig.MachineKey; 
                        if (keySection != null) {
                            validation = keySection.Validation;

                            // MachineKeyValidation enum has only 3 values, so if it's MD5, switch the 
                            // algorithm type to it.  Else, leave it as SHA1 (specified above).
                            if (validation == MachineKeyValidation.MD5) { 
                                s_HashAlgorithmType = "MD5"; 
                            }
                        } 
                    }
                    else {
                        s_HashAlgorithmType = hashAlgorithm;
                        s_HashAlgorithmFromConfig = true; 
                    }
 
                    s_Providers = new MembershipProviderCollection(); 
                    if (HostingEnvironment.IsHosted) {
                        ProvidersHelper.InstantiateProviders(settings.Providers, s_Providers, typeof(MembershipProvider)); 
                    } else {
                        foreach (ProviderSettings ps in settings.Providers) {
                            Type t = Type.GetType(ps.Type, true, true);
                            if (!typeof(MembershipProvider).IsAssignableFrom(t)) 
                                throw new ArgumentException(SR.GetString(SR.Provider_must_implement_type, typeof(MembershipProvider).ToString()));
                            MembershipProvider provider = (MembershipProvider)Activator.CreateInstance(t); 
                            NameValueCollection pars = ps.Parameters; 
                            NameValueCollection cloneParams = new NameValueCollection(pars.Count, StringComparer.Ordinal);
                            foreach (string key in pars) 
                                cloneParams[key] = pars[key];
                            provider.Initialize(ps.Name, cloneParams);
                            s_Providers.Add(provider);
                        } 
                    }
                    s_Provider = s_Providers[settings.DefaultProvider]; 
                    if (s_Provider == null) 
                    {
                        throw new ConfigurationErrorsException(SR.GetString(SR.Def_membership_provider_not_found), settings.ElementInformation.Properties["defaultProvider"].Source, settings.ElementInformation.Properties["defaultProvider"].LineNumber); 
                    }
                    timeWindow = settings.UserIsOnlineTimeWindow;
                    s_UserIsOnlineTimeWindow = (int) timeWindow.TotalMinutes;
                    s_Providers.SetReadOnly(); 
                } catch (Exception e) {
                    s_InitializeException = e; 
                    throw; 
                }
                s_Initialized = true; 
            }
        }

 
        public static MembershipUserCollection FindUsersByName( string usernameToMatch,
                                                                int pageIndex, 
                                                                int pageSize, 
                                                                out int totalRecords )
        { 
            SecUtility.CheckParameter( ref usernameToMatch,
                                       true,
                                       true,
                                       false, 
                                       0,
                                       "usernameToMatch" ); 
 
            if ( pageIndex < 0 )
            { 
                throw new ArgumentException(SR.GetString(SR.PageIndex_bad), "pageIndex");
            }

            if ( pageSize < 1 ) 
            {
                throw new ArgumentException(SR.GetString(SR.PageSize_bad), "pageSize"); 
            } 

            return Provider.FindUsersByName( usernameToMatch, 
                                             pageIndex,
                                             pageSize,
                                             out totalRecords);
        } 

 
        public static MembershipUserCollection FindUsersByName( string usernameToMatch ) 
        {
            SecUtility.CheckParameter( ref usernameToMatch, 
                                       true,
                                       true,
                                       false,
                                       0, 
                                       "usernameToMatch" );
 
            int totalRecords = 0; 
            return Provider.FindUsersByName( usernameToMatch,
                                             0, 
                                             Int32.MaxValue,
                                             out totalRecords );
        }
 
        public static MembershipUserCollection FindUsersByEmail( string  emailToMatch,
                                                                 int     pageIndex, 
                                                                 int     pageSize, 
                                                                 out int totalRecords )
        { 
            SecUtility.CheckParameter( ref emailToMatch,
                                       false,
                                       false,
                                       false, 
                                       0,
                                       "emailToMatch" ); 
 
            if ( pageIndex < 0 )
            { 
                throw new ArgumentException(SR.GetString(SR.PageIndex_bad), "pageIndex");
            }

            if ( pageSize < 1 ) 
            {
                throw new ArgumentException(SR.GetString(SR.PageSize_bad), "pageSize"); 
            } 

            return Provider.FindUsersByEmail( emailToMatch, 
                                              pageIndex,
                                              pageSize,
                                              out totalRecords );
        } 

        public static MembershipUserCollection FindUsersByEmail(string emailToMatch) 
        { 
            SecUtility.CheckParameter( ref emailToMatch,
                                       false, 
                                       false,
                                       false,
                                       0,
                                       "emailToMatch" ); 

            int totalRecords = 0; 
            return FindUsersByEmail(emailToMatch, 0, Int32.MaxValue, out totalRecords); 
        }
 
        private static string GetCurrentUserName()
        {
            if (HostingEnvironment.IsHosted) {
                HttpContext cur = HttpContext.Current; 
                if (cur != null)
                    return cur.User.Identity.Name; 
            } 
            IPrincipal user = Thread.CurrentPrincipal;
            if (user == null || user.Identity == null) 
                return String.Empty;
            else
                return user.Identity.Name;
        } 

        public static event MembershipValidatePasswordEventHandler ValidatingPassword 
        { 
            add
            { 
                Provider.ValidatingPassword += value;
            }
            remove
            { 
                Provider.ValidatingPassword -= value;
            } 
        } 

        private static MembershipProviderCollection   s_Providers; 
        private static MembershipProvider             s_Provider;
        private static int                            s_UserIsOnlineTimeWindow = 15;
        private static object                         s_lock = new object();
        private static bool                           s_Initialized = false; 
        private static Exception                      s_InitializeException = null;
        private static string                         s_HashAlgorithmType; 
        private static bool                           s_HashAlgorithmFromConfig; 
    }
 
    ////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////// 

 
    public enum MembershipCreateStatus { 

        Success                  = 0, 

        InvalidUserName          = 1,  // invalid user name

        InvalidPassword          = 2, // new password was not accepted (invalid format) 

        InvalidQuestion          = 3, // new question was not accepted (invalid format) 
 
        InvalidAnswer            = 4, // new passwordAnswer was not acceppted (invalid format)
 
        InvalidEmail             = 5, // new email was not accepted (invalid format)

        DuplicateUserName        = 6, // username already exists
 
        DuplicateEmail           = 7, // email already exists
 
        UserRejected             = 8, // provider rejected user (for some user-specific reason) 

        InvalidProviderUserKey   = 9,  // new provider user key was not accepted (invalid format) 
        DuplicateProviderUserKey = 10, // provider user key already exists
        ProviderError            = 11  // provider-specific error (couldn't map onto this enumeration)
    }
 

 
   // [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
   // This has no hosting permission demands because of DevDiv Bugs 31461: ClientAppSvcs: ASP.net Provider support
   public sealed class MembershipProviderCollection : ProviderCollection 
   {

        public override void Add(ProviderBase provider) {
            if( provider == null ) 
            {
                throw new ArgumentNullException( "provider" ); 
            } 

            if( !( provider is MembershipProvider ) ) 
            {
                throw new ArgumentException(SR.GetString(SR.Provider_must_implement_type, typeof(MembershipProvider).ToString()), "provider");
            }
 
            base.Add( provider );
        } 
 
       new public MembershipProvider this[string name] {
           get { 
               return (MembershipProvider) base[name];
           }
       }
 
       public void CopyTo(MembershipProvider[] array, int index)
       { 
           base.CopyTo(array, index); 
       }
   } 


   [Serializable]
   [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
   public sealed class MembershipUserCollection : IEnumerable, ICollection
    { 
        private Hashtable _Indices  = null; 
        private ArrayList _Values   = null;
        private bool      _ReadOnly = false; 



        public MembershipUserCollection() { 
            _Indices = new Hashtable(10, StringComparer.CurrentCultureIgnoreCase);
            _Values  = new ArrayList(); 
        } 
        private MembershipUserCollection(Hashtable indices, ArrayList values){
            _Indices = (Hashtable) indices.Clone(); 
            _Values = (ArrayList) values.Clone();
        }

 
        public void Add(MembershipUser user)
        { 
            if( user == null ) 
            {
                throw new ArgumentNullException( "user" ); 
            }

            if (_ReadOnly)
                throw new NotSupportedException(); 

            int pos = _Values.Add(user); 
            try { 
                _Indices.Add(user.UserName, pos);
            } 
            catch {
                _Values.RemoveAt(pos);
                throw;
            } 
        }
 
        public void Remove(string name) 
        {
            if (_ReadOnly) 
                throw new NotSupportedException();

            object pos = _Indices[name];
            if (pos == null || !(pos is int)) 
                return;
            int ipos = (int) pos; 
            if (ipos >= _Values.Count) 
                return;
            _Values.RemoveAt(ipos); 
            _Indices.Remove(name);
            ArrayList al = new ArrayList();
            foreach(DictionaryEntry de in _Indices)
                if ((int)de.Value > ipos) 
                    al.Add(de.Key);
            foreach(string key in al) 
                _Indices[key] = ((int) _Indices[key]) - 1; 
        }
 
        public MembershipUser this[string name]
        {
            get {
                object pos = _Indices[name]; 
                if (pos == null || !(pos is int))
                    return null; 
                int ipos = (int) pos; 
                if (ipos >= _Values.Count)
                    return null; 
                return (MembershipUser) _Values[ipos];
            }
        }
 
        public IEnumerator GetEnumerator() {
            return _Values.GetEnumerator(); 
        } 

        public void SetReadOnly() { 
            if (_ReadOnly)
                return;
            _ReadOnly = true;
            _Values = ArrayList.ReadOnly(_Values); 
        }
 
        public void Clear() { 
            _Values.Clear();
            _Indices.Clear(); 
        }

        public int Count { get { return _Values.Count; }}
 
        public bool IsSynchronized {get { return false;}}
 
        public object SyncRoot {get { return this;}} 

 
        void ICollection.CopyTo(Array array, int index)
        {
            _Values.CopyTo(array, index);
        } 

        public void CopyTo(MembershipUser [] array, int index) 
        { 
            _Values.CopyTo(array, index);
        } 
    }


    [Serializable] 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class MembershipCreateUserException : Exception 
    {
 
        public MembershipCreateUserException(MembershipCreateStatus statusCode)
            : base(GetMessageFromStatusCode(statusCode))
        {
            _StatusCode = statusCode; 
        }
 
        public MembershipCreateUserException(String message) : base(message) 
        { }
 

        protected MembershipCreateUserException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _StatusCode = (MembershipCreateStatus)info.GetInt32("_StatusCode"); 
        }
 
 
        public MembershipCreateUserException()
        { } 

        public MembershipCreateUserException(String message, Exception innerException) : base(message, innerException)
        { }
 
        private MembershipCreateStatus _StatusCode = MembershipCreateStatus.ProviderError;
 
 
        public  MembershipCreateStatus StatusCode { get { return _StatusCode; }}
 
        [SecurityPermission(SecurityAction.Demand, Flags=SecurityPermissionFlag.SerializationFormatter, SerializationFormatter=true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);
            info.AddValue("_StatusCode", _StatusCode); 
        }
 
        internal static string GetMessageFromStatusCode(MembershipCreateStatus statusCode) { 
            string msgKey = SR.Provider_Error;
            switch(statusCode) 
            {
            case MembershipCreateStatus.Success:
                 msgKey = SR.Membership_No_error;
                 break; 
            case MembershipCreateStatus.InvalidUserName:
                msgKey = SR.Membership_InvalidUserName; 
                break; 
            case MembershipCreateStatus.InvalidPassword:
                msgKey = SR.Membership_InvalidPassword; 
                break;
            case MembershipCreateStatus.InvalidQuestion:
                msgKey = SR.Membership_InvalidQuestion;
                break; 
            case MembershipCreateStatus.InvalidAnswer:
                msgKey = SR.Membership_InvalidAnswer; 
                break; 
            case MembershipCreateStatus.InvalidEmail:
                msgKey = SR.Membership_InvalidEmail; 
                break;
            case MembershipCreateStatus.InvalidProviderUserKey:
                msgKey = SR.Membership_InvalidProviderUserKey;
                break; 
            case MembershipCreateStatus.DuplicateUserName:
                msgKey = SR.Membership_DuplicateUserName; 
                break; 
            case MembershipCreateStatus.DuplicateEmail:
                msgKey = SR.Membership_DuplicateEmail; 
                break;
            case MembershipCreateStatus.DuplicateProviderUserKey:
                msgKey = SR.Membership_DuplicateProviderUserKey;
                break; 
            case MembershipCreateStatus.UserRejected:
                msgKey = SR.Membership_UserRejected; 
                break; 
            }
            return SR.GetString(msgKey); 
        }

    }
 

    public enum MembershipPasswordFormat { 
 
        Clear = 0, Hashed = 1, Encrypted = 2
    } 

}

