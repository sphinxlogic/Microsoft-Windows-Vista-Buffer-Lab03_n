//------------------------------------------------------------------------------ 
// <copyright file="User.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Security { 
    using  System.Web; 
    using  System.Web.Configuration;
    using  System.Configuration.Provider; 
    using  System.Security.Principal;
    using  System.Security.Permissions;
    using  System.Globalization;
    using  System.Runtime.Serialization; 
    using  System.Web.Util;
 
 
    /// <devdoc>
    ///    <para>[To be supplied.]</para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [Serializable]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class MembershipUser
    { 
        //////////////////////////////////////////////////////////// 
        // Public methods
 
        public virtual string UserName{
            get { return _UserName;}}

        public virtual object ProviderUserKey{ 
            get { return _ProviderUserKey;}}
 
 
        public virtual string Email{
            get { return _Email;} 
            set { _Email = value; }}


        public virtual string PasswordQuestion{ 
            get { return _PasswordQuestion;}}
 
 
        public virtual string Comment{
            get { return _Comment;} 
            set { _Comment = value;}}


        public virtual bool IsApproved{ 
            get { return _IsApproved;}
            set { _IsApproved = value; } } 
 
        public virtual bool IsLockedOut
        { 
            get { return _IsLockedOut; }
        }

        public virtual DateTime LastLockoutDate 
        {
            get { return _LastLockoutDate.ToLocalTime(); } 
        } 

 
        public virtual DateTime CreationDate {
            get { return _CreationDate.ToLocalTime(); }
        }
 

        public virtual DateTime LastLoginDate { 
            get { return _LastLoginDate.ToLocalTime(); } 
            set { _LastLoginDate = value.ToUniversalTime(); } }
 

        public virtual DateTime LastActivityDate {
            get { return _LastActivityDate.ToLocalTime(); }
            set { _LastActivityDate = value.ToUniversalTime(); } } 

 
        public virtual DateTime LastPasswordChangedDate { 
            get { return _LastPasswordChangedDate.ToLocalTime(); }
        } 


        public bool IsOnline {
            get { 
                TimeSpan   ts  = new TimeSpan(0, Membership.UserIsOnlineTimeWindow, 0);
                DateTime   dt  = DateTime.UtcNow.Subtract(ts); 
                return LastActivityDate.ToUniversalTime() > dt; 
            }
        } 

        public override string ToString()
        {
            return UserName; 
        }
 
 
        public virtual string ProviderName
        { 
            get { return _ProviderName; }
        }

        //////////////////////////////////////////////////////////// 
        // CTor
 
        public MembershipUser( 
            string        providerName,
            string        name, 
            object        providerUserKey,
            string        email,
            string        passwordQuestion,
            string        comment, 
            bool          isApproved,
            bool          isLockedOut, 
            DateTime      creationDate, 
            DateTime      lastLoginDate,
            DateTime      lastActivityDate, 
            DateTime      lastPasswordChangedDate,
            DateTime      lastLockoutDate )
        {
            if ( providerName == null || Membership.Providers[providerName] == null ) 
            {
                throw new ArgumentException( SR.GetString( SR.Membership_provider_name_invalid ), "providerName" ); 
            } 

            if( name != null ) 
            {
                name = name.Trim();
            }
 
            if( email != null )
            { 
                email = email.Trim(); 
            }
 
            if( passwordQuestion != null )
            {
                passwordQuestion = passwordQuestion.Trim();
            } 

            _ProviderName = providerName; 
            _UserName = name; 
            _ProviderUserKey = providerUserKey;
            _Email = email; 
            _PasswordQuestion = passwordQuestion;
            _Comment = comment;
            _IsApproved = isApproved;
            _IsLockedOut = isLockedOut; 

            // VSWhidbey 451539: We should use UTC internally for all dates, and return local for public apis 
            _CreationDate = creationDate.ToUniversalTime(); 
            _LastLoginDate = lastLoginDate.ToUniversalTime();
            _LastActivityDate = lastActivityDate.ToUniversalTime(); 
            _LastPasswordChangedDate = lastPasswordChangedDate.ToUniversalTime();
            _LastLockoutDate = lastLockoutDate.ToUniversalTime();
        }
 

        protected MembershipUser() { } // Default CTor: Callable by derived class only. 
 
        internal virtual void Update()
        { 
            Membership.Providers[ProviderName].UpdateUser(this);
            UpdateSelf();
        }
 
        public virtual string GetPassword()
        { 
            return Membership.Providers[ProviderName].GetPassword(UserName, null); 
        }
 
        public virtual string GetPassword(string passwordAnswer)
        {
            return Membership.Providers[ ProviderName ].GetPassword(UserName, passwordAnswer);
        } 

        internal string GetPassword(bool throwOnError) { 
            return GetPassword(null, /* useAnswer */ false, throwOnError); 
        }
 
        internal string GetPassword(string answer, bool throwOnError) {
            return GetPassword(answer, /* useAnswer */ true, throwOnError);
        }
 
        // GetPassword() can throw 3 types of exception:
        // 1. ArgumentException is thrown if: 
        //    A. Answer is null, empty, or longer than 128 characters 
        // 2. ProviderException is thrown if the user does not exist when the stored procedure
        //    is run.  The only way this could happen is in a race condition, where the user 
        //    is deleted in the middle of the MembershipProvider.ChangePassword() method.
        // 3. MembershipPasswordException is thrown if the user is locked out, or the answer
        //    is incorrect.
        private string GetPassword(string answer, bool useAnswer, bool throwOnError) { 
            string password = null;
 
            try { 
                if (useAnswer) {
                    password = GetPassword(answer); 
                }
                else {
                    password = GetPassword();
                } 
            }
            catch (ArgumentException) { 
                if (throwOnError) throw; 
            }
            catch (MembershipPasswordException) { 
                if (throwOnError) throw;
            }
            catch (ProviderException) {
                if (throwOnError) throw; 
            }
 
            return password; 
        }
 
        public virtual bool ChangePassword(string oldPassword, string newPassword)
        {
            SecUtility.CheckPasswordParameter(ref oldPassword, 0, "oldPassword");
            SecUtility.CheckPasswordParameter(ref newPassword, 0, "newPassword"); 

            if (!Membership.Providers[ ProviderName ].ChangePassword(UserName, oldPassword, newPassword)) 
                return false; 
            UpdateSelf();
            //_LastPasswordChangedDate = Membership.Providers[ ProviderName ].GetUser( UserName, false ).LastPasswordChangedDate; 
            return true;
        }

        // ChangePassword() can throw 3 types of exception: 
        // 1. ArgumentException is thrown if:
        //    A. OldPassword or NewPassword is null, empty, or longer than 128 characters 
        //    B. NewPassword shorter than MinRequiredPasswordLength, or NewPassword contains 
        //       less non-alphanumeric characters than MinRequiredNonAlphanumericCharacters,
        //       or NewPassword does not match PasswordStrengthRegularExpression. 
        //    C. A developer adds a listener to the MembershipProvider.ValidatingPassword event,
        //       and sets e.Cancel to true, and e.FailureInformation is null.
        // 2. ProviderException is thrown if the user does not exist when the stored procedure
        //    is run.  The only way this could happen is in a race condition, where the user 
        //    is deleted in the middle of the MembershipProvider.ChangePassword() method.
        // 3. It appears that MembershipProviderException currently cannot be thrown, but 
        //    there is a codepath that throws this exception, so we should catch it here anyway. 
        internal bool ChangePassword(string oldPassword, string newPassword, bool throwOnError) {
            bool passwordChanged = false; 

            try {
                passwordChanged = ChangePassword(oldPassword, newPassword);
            } 
            catch (ArgumentException) {
                if (throwOnError) throw; 
            } 
            catch (MembershipPasswordException) {
                if (throwOnError) throw; 
            }
            catch (ProviderException) {
                if (throwOnError) throw;
            } 

            return passwordChanged; 
        } 

        public virtual bool ChangePasswordQuestionAndAnswer(string password, string newPasswordQuestion, string newPasswordAnswer) 
        {
            SecUtility.CheckPasswordParameter(ref password, 0, "password");
            SecUtility.CheckParameter(ref newPasswordQuestion, false, true, false, 0, "newPasswordQuestion" );
            SecUtility.CheckParameter(ref newPasswordAnswer, false, true, false, 0, "newPasswordAnswer" ); 

            if (!Membership.Providers[ ProviderName ].ChangePasswordQuestionAndAnswer(UserName, password, newPasswordQuestion, newPasswordAnswer)) 
                return false; 
            UpdateSelf();
            return true; 
        }

        public virtual string ResetPassword(string passwordAnswer)
        { 
            string pass = Membership.Providers[ ProviderName ].ResetPassword(UserName, passwordAnswer);
            if (!String.IsNullOrEmpty(pass)) { 
                UpdateSelf(); 
                //_LastPasswordChangedDate = Membership.Providers[ProviderName].GetUser(UserName, false).LastPasswordChangedDate;
            } 
            return pass;
        }

        public virtual string ResetPassword() 
        {
            return ResetPassword(null); 
        } 

        internal string ResetPassword(bool throwOnError) { 
            return ResetPassword(null, /* useAnswer */ false, throwOnError);
        }

        internal string ResetPassword(string passwordAnswer, bool throwOnError) { 
            return ResetPassword(passwordAnswer, /* useAnswer */ true, throwOnError);
        } 
 
        // MembershipProvider.ResetPassword() can throw 3 types of exception:
        // 1. ArgumentException is thrown if: 
        //    A. Answer is null, empty, or longer than 128 characters
        // 2. ProviderException is thrown if:
        //    A. The user does not exist when the stored procedure is run.  The only way
        //       this could happen is in a race condition, where the user is deleted in 
        //       the middle of the MembershipProvider.ChangePassword() method.
        //    B. A developer adds a listener to the MembershipProvider.ValidatingPassword event, 
        //       and sets e.Cancel to true, and e.FailureInformation is null. 
        // 3. MembershipPasswordException is thrown if the user is locked out, or the answer
        //    is incorrect. 
        private string ResetPassword(string passwordAnswer, bool useAnswer, bool throwOnError) {
            string password = null;

            try { 
                if (useAnswer) {
                    password = ResetPassword(passwordAnswer); 
                } 
                else {
                    password = ResetPassword(); 
                }
            }
            catch (ArgumentException) {
                if (throwOnError) throw; 
            }
            catch (MembershipPasswordException) { 
                if (throwOnError) throw; 
            }
            catch (ProviderException) { 
                if (throwOnError) throw;
            }

            return password; 
        }
 
        public virtual bool UnlockUser() 
        {
            if( Membership.Providers[ProviderName].UnlockUser(UserName)) 
            {
                UpdateSelf();
                return !IsLockedOut;
            } 

            return false; 
        } 
        private void UpdateSelf()
        { 
            MembershipUser mu = Membership.Providers[ProviderName].GetUser(UserName, false);
            if (mu != null) {
                try {
                    _LastPasswordChangedDate = mu.LastPasswordChangedDate.ToUniversalTime(); 
                } catch (NotSupportedException) {}
                try { 
                    LastActivityDate = mu.LastActivityDate; 
                } catch (NotSupportedException) {}
                try { 
                    LastLoginDate = mu.LastLoginDate;
                } catch (NotSupportedException) {}
                try {
                    _CreationDate = mu.CreationDate.ToUniversalTime(); 
                } catch (NotSupportedException) { }
                try { 
                    _LastLockoutDate = mu.LastLockoutDate.ToUniversalTime(); 
                } catch (NotSupportedException) { }
                try { 
                    _IsLockedOut = mu.IsLockedOut;
                } catch (NotSupportedException) { }
                try {
                    IsApproved = mu.IsApproved; 
                } catch (NotSupportedException) { }
                try { 
                    Comment = mu.Comment; 
                } catch (NotSupportedException) { }
                try { 
                    _PasswordQuestion = mu.PasswordQuestion;
                } catch (NotSupportedException) { }
                try {
                    Email = mu.Email; 
                } catch (NotSupportedException) { }
                try { 
                    _ProviderUserKey = mu.ProviderUserKey; 
                } catch (NotSupportedException) { }
            } 
        }
        ////////////////////////////////////////////////////////////
        // private Data
        private  string      _UserName; 
        private  object      _ProviderUserKey;
        private  string      _Email; 
        private  string      _PasswordQuestion; 
        private  string      _Comment;
        private  bool        _IsApproved; 
        private  bool        _IsLockedOut;
        private  DateTime    _LastLockoutDate;
        private  DateTime    _CreationDate;
        private  DateTime    _LastLoginDate; 
        private  DateTime    _LastActivityDate;
        private  DateTime    _LastPasswordChangedDate; 
        private  string      _ProviderName; 
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="User.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Security { 
    using  System.Web; 
    using  System.Web.Configuration;
    using  System.Configuration.Provider; 
    using  System.Security.Principal;
    using  System.Security.Permissions;
    using  System.Globalization;
    using  System.Runtime.Serialization; 
    using  System.Web.Util;
 
 
    /// <devdoc>
    ///    <para>[To be supplied.]</para> 
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [Serializable]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class MembershipUser
    { 
        //////////////////////////////////////////////////////////// 
        // Public methods
 
        public virtual string UserName{
            get { return _UserName;}}

        public virtual object ProviderUserKey{ 
            get { return _ProviderUserKey;}}
 
 
        public virtual string Email{
            get { return _Email;} 
            set { _Email = value; }}


        public virtual string PasswordQuestion{ 
            get { return _PasswordQuestion;}}
 
 
        public virtual string Comment{
            get { return _Comment;} 
            set { _Comment = value;}}


        public virtual bool IsApproved{ 
            get { return _IsApproved;}
            set { _IsApproved = value; } } 
 
        public virtual bool IsLockedOut
        { 
            get { return _IsLockedOut; }
        }

        public virtual DateTime LastLockoutDate 
        {
            get { return _LastLockoutDate.ToLocalTime(); } 
        } 

 
        public virtual DateTime CreationDate {
            get { return _CreationDate.ToLocalTime(); }
        }
 

        public virtual DateTime LastLoginDate { 
            get { return _LastLoginDate.ToLocalTime(); } 
            set { _LastLoginDate = value.ToUniversalTime(); } }
 

        public virtual DateTime LastActivityDate {
            get { return _LastActivityDate.ToLocalTime(); }
            set { _LastActivityDate = value.ToUniversalTime(); } } 

 
        public virtual DateTime LastPasswordChangedDate { 
            get { return _LastPasswordChangedDate.ToLocalTime(); }
        } 


        public bool IsOnline {
            get { 
                TimeSpan   ts  = new TimeSpan(0, Membership.UserIsOnlineTimeWindow, 0);
                DateTime   dt  = DateTime.UtcNow.Subtract(ts); 
                return LastActivityDate.ToUniversalTime() > dt; 
            }
        } 

        public override string ToString()
        {
            return UserName; 
        }
 
 
        public virtual string ProviderName
        { 
            get { return _ProviderName; }
        }

        //////////////////////////////////////////////////////////// 
        // CTor
 
        public MembershipUser( 
            string        providerName,
            string        name, 
            object        providerUserKey,
            string        email,
            string        passwordQuestion,
            string        comment, 
            bool          isApproved,
            bool          isLockedOut, 
            DateTime      creationDate, 
            DateTime      lastLoginDate,
            DateTime      lastActivityDate, 
            DateTime      lastPasswordChangedDate,
            DateTime      lastLockoutDate )
        {
            if ( providerName == null || Membership.Providers[providerName] == null ) 
            {
                throw new ArgumentException( SR.GetString( SR.Membership_provider_name_invalid ), "providerName" ); 
            } 

            if( name != null ) 
            {
                name = name.Trim();
            }
 
            if( email != null )
            { 
                email = email.Trim(); 
            }
 
            if( passwordQuestion != null )
            {
                passwordQuestion = passwordQuestion.Trim();
            } 

            _ProviderName = providerName; 
            _UserName = name; 
            _ProviderUserKey = providerUserKey;
            _Email = email; 
            _PasswordQuestion = passwordQuestion;
            _Comment = comment;
            _IsApproved = isApproved;
            _IsLockedOut = isLockedOut; 

            // VSWhidbey 451539: We should use UTC internally for all dates, and return local for public apis 
            _CreationDate = creationDate.ToUniversalTime(); 
            _LastLoginDate = lastLoginDate.ToUniversalTime();
            _LastActivityDate = lastActivityDate.ToUniversalTime(); 
            _LastPasswordChangedDate = lastPasswordChangedDate.ToUniversalTime();
            _LastLockoutDate = lastLockoutDate.ToUniversalTime();
        }
 

        protected MembershipUser() { } // Default CTor: Callable by derived class only. 
 
        internal virtual void Update()
        { 
            Membership.Providers[ProviderName].UpdateUser(this);
            UpdateSelf();
        }
 
        public virtual string GetPassword()
        { 
            return Membership.Providers[ProviderName].GetPassword(UserName, null); 
        }
 
        public virtual string GetPassword(string passwordAnswer)
        {
            return Membership.Providers[ ProviderName ].GetPassword(UserName, passwordAnswer);
        } 

        internal string GetPassword(bool throwOnError) { 
            return GetPassword(null, /* useAnswer */ false, throwOnError); 
        }
 
        internal string GetPassword(string answer, bool throwOnError) {
            return GetPassword(answer, /* useAnswer */ true, throwOnError);
        }
 
        // GetPassword() can throw 3 types of exception:
        // 1. ArgumentException is thrown if: 
        //    A. Answer is null, empty, or longer than 128 characters 
        // 2. ProviderException is thrown if the user does not exist when the stored procedure
        //    is run.  The only way this could happen is in a race condition, where the user 
        //    is deleted in the middle of the MembershipProvider.ChangePassword() method.
        // 3. MembershipPasswordException is thrown if the user is locked out, or the answer
        //    is incorrect.
        private string GetPassword(string answer, bool useAnswer, bool throwOnError) { 
            string password = null;
 
            try { 
                if (useAnswer) {
                    password = GetPassword(answer); 
                }
                else {
                    password = GetPassword();
                } 
            }
            catch (ArgumentException) { 
                if (throwOnError) throw; 
            }
            catch (MembershipPasswordException) { 
                if (throwOnError) throw;
            }
            catch (ProviderException) {
                if (throwOnError) throw; 
            }
 
            return password; 
        }
 
        public virtual bool ChangePassword(string oldPassword, string newPassword)
        {
            SecUtility.CheckPasswordParameter(ref oldPassword, 0, "oldPassword");
            SecUtility.CheckPasswordParameter(ref newPassword, 0, "newPassword"); 

            if (!Membership.Providers[ ProviderName ].ChangePassword(UserName, oldPassword, newPassword)) 
                return false; 
            UpdateSelf();
            //_LastPasswordChangedDate = Membership.Providers[ ProviderName ].GetUser( UserName, false ).LastPasswordChangedDate; 
            return true;
        }

        // ChangePassword() can throw 3 types of exception: 
        // 1. ArgumentException is thrown if:
        //    A. OldPassword or NewPassword is null, empty, or longer than 128 characters 
        //    B. NewPassword shorter than MinRequiredPasswordLength, or NewPassword contains 
        //       less non-alphanumeric characters than MinRequiredNonAlphanumericCharacters,
        //       or NewPassword does not match PasswordStrengthRegularExpression. 
        //    C. A developer adds a listener to the MembershipProvider.ValidatingPassword event,
        //       and sets e.Cancel to true, and e.FailureInformation is null.
        // 2. ProviderException is thrown if the user does not exist when the stored procedure
        //    is run.  The only way this could happen is in a race condition, where the user 
        //    is deleted in the middle of the MembershipProvider.ChangePassword() method.
        // 3. It appears that MembershipProviderException currently cannot be thrown, but 
        //    there is a codepath that throws this exception, so we should catch it here anyway. 
        internal bool ChangePassword(string oldPassword, string newPassword, bool throwOnError) {
            bool passwordChanged = false; 

            try {
                passwordChanged = ChangePassword(oldPassword, newPassword);
            } 
            catch (ArgumentException) {
                if (throwOnError) throw; 
            } 
            catch (MembershipPasswordException) {
                if (throwOnError) throw; 
            }
            catch (ProviderException) {
                if (throwOnError) throw;
            } 

            return passwordChanged; 
        } 

        public virtual bool ChangePasswordQuestionAndAnswer(string password, string newPasswordQuestion, string newPasswordAnswer) 
        {
            SecUtility.CheckPasswordParameter(ref password, 0, "password");
            SecUtility.CheckParameter(ref newPasswordQuestion, false, true, false, 0, "newPasswordQuestion" );
            SecUtility.CheckParameter(ref newPasswordAnswer, false, true, false, 0, "newPasswordAnswer" ); 

            if (!Membership.Providers[ ProviderName ].ChangePasswordQuestionAndAnswer(UserName, password, newPasswordQuestion, newPasswordAnswer)) 
                return false; 
            UpdateSelf();
            return true; 
        }

        public virtual string ResetPassword(string passwordAnswer)
        { 
            string pass = Membership.Providers[ ProviderName ].ResetPassword(UserName, passwordAnswer);
            if (!String.IsNullOrEmpty(pass)) { 
                UpdateSelf(); 
                //_LastPasswordChangedDate = Membership.Providers[ProviderName].GetUser(UserName, false).LastPasswordChangedDate;
            } 
            return pass;
        }

        public virtual string ResetPassword() 
        {
            return ResetPassword(null); 
        } 

        internal string ResetPassword(bool throwOnError) { 
            return ResetPassword(null, /* useAnswer */ false, throwOnError);
        }

        internal string ResetPassword(string passwordAnswer, bool throwOnError) { 
            return ResetPassword(passwordAnswer, /* useAnswer */ true, throwOnError);
        } 
 
        // MembershipProvider.ResetPassword() can throw 3 types of exception:
        // 1. ArgumentException is thrown if: 
        //    A. Answer is null, empty, or longer than 128 characters
        // 2. ProviderException is thrown if:
        //    A. The user does not exist when the stored procedure is run.  The only way
        //       this could happen is in a race condition, where the user is deleted in 
        //       the middle of the MembershipProvider.ChangePassword() method.
        //    B. A developer adds a listener to the MembershipProvider.ValidatingPassword event, 
        //       and sets e.Cancel to true, and e.FailureInformation is null. 
        // 3. MembershipPasswordException is thrown if the user is locked out, or the answer
        //    is incorrect. 
        private string ResetPassword(string passwordAnswer, bool useAnswer, bool throwOnError) {
            string password = null;

            try { 
                if (useAnswer) {
                    password = ResetPassword(passwordAnswer); 
                } 
                else {
                    password = ResetPassword(); 
                }
            }
            catch (ArgumentException) {
                if (throwOnError) throw; 
            }
            catch (MembershipPasswordException) { 
                if (throwOnError) throw; 
            }
            catch (ProviderException) { 
                if (throwOnError) throw;
            }

            return password; 
        }
 
        public virtual bool UnlockUser() 
        {
            if( Membership.Providers[ProviderName].UnlockUser(UserName)) 
            {
                UpdateSelf();
                return !IsLockedOut;
            } 

            return false; 
        } 
        private void UpdateSelf()
        { 
            MembershipUser mu = Membership.Providers[ProviderName].GetUser(UserName, false);
            if (mu != null) {
                try {
                    _LastPasswordChangedDate = mu.LastPasswordChangedDate.ToUniversalTime(); 
                } catch (NotSupportedException) {}
                try { 
                    LastActivityDate = mu.LastActivityDate; 
                } catch (NotSupportedException) {}
                try { 
                    LastLoginDate = mu.LastLoginDate;
                } catch (NotSupportedException) {}
                try {
                    _CreationDate = mu.CreationDate.ToUniversalTime(); 
                } catch (NotSupportedException) { }
                try { 
                    _LastLockoutDate = mu.LastLockoutDate.ToUniversalTime(); 
                } catch (NotSupportedException) { }
                try { 
                    _IsLockedOut = mu.IsLockedOut;
                } catch (NotSupportedException) { }
                try {
                    IsApproved = mu.IsApproved; 
                } catch (NotSupportedException) { }
                try { 
                    Comment = mu.Comment; 
                } catch (NotSupportedException) { }
                try { 
                    _PasswordQuestion = mu.PasswordQuestion;
                } catch (NotSupportedException) { }
                try {
                    Email = mu.Email; 
                } catch (NotSupportedException) { }
                try { 
                    _ProviderUserKey = mu.ProviderUserKey; 
                } catch (NotSupportedException) { }
            } 
        }
        ////////////////////////////////////////////////////////////
        // private Data
        private  string      _UserName; 
        private  object      _ProviderUserKey;
        private  string      _Email; 
        private  string      _PasswordQuestion; 
        private  string      _Comment;
        private  bool        _IsApproved; 
        private  bool        _IsLockedOut;
        private  DateTime    _LastLockoutDate;
        private  DateTime    _CreationDate;
        private  DateTime    _LastLoginDate; 
        private  DateTime    _LastActivityDate;
        private  DateTime    _LastPasswordChangedDate; 
        private  string      _ProviderName; 
    }
} 
