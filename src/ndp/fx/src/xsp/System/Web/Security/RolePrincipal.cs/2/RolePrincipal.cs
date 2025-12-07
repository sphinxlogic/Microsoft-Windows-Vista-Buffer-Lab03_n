//------------------------------------------------------------------------------ 
// <copyright file="RolePrincipal.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * RolePrincipal 
 *
 * Copyright (c) 2002 Microsoft Corporation 
 */

namespace System.Web.Security {
    using  System.Security.Principal; 
    using System.Security.Permissions;
    using System.Collections; 
    using System.Collections.Specialized; 
    using System.Web;
    using System.Web.Hosting; 
    using System.Text;
    using System.Web.Configuration;
    using System.Web.Util;
    using System.Globalization; 
    using System.Runtime.Serialization;
    using System.IO; 
    using System.Configuration.Provider; 
    using System.Runtime.Serialization.Formatters.Binary;
 
    [Serializable]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class RolePrincipal : IPrincipal, ISerializable
    { 
        public RolePrincipal(IIdentity identity, string encryptedTicket)
        { 
            if (identity == null) 
                throw new ArgumentNullException( "identity" );
 
            if (encryptedTicket == null)
                throw new ArgumentNullException( "encryptedTicket" );
            _Identity = identity;
            _ProviderName = Roles.Provider.Name; 
            if (identity.IsAuthenticated)
                InitFromEncryptedTicket(encryptedTicket); 
            else 
                Init();
        } 

        public RolePrincipal(IIdentity identity)
        {
            if (identity == null) 
                throw new ArgumentNullException( "identity" );
            _Identity = identity; 
            Init(); 
        }
 
        public RolePrincipal(string providerName, IIdentity identity )
        {
            if (identity == null)
                throw new ArgumentNullException( "identity" ); 

            if( providerName == null) 
                throw new ArgumentException( SR.GetString( SR.Role_provider_name_invalid ), "providerName" ); 

            _ProviderName = providerName; 
            if (Roles.Providers[providerName] == null)
                throw new ArgumentException(SR.GetString(SR.Role_provider_name_invalid), "providerName");

            _Identity = identity; 
            Init();
        } 
 
        public RolePrincipal(string providerName, IIdentity identity, string encryptedTicket )
        { 
            if (identity == null)
                throw new ArgumentNullException( "identity" );

            if (encryptedTicket == null) 
                throw new ArgumentNullException( "encryptedTicket" );
 
            if( providerName == null) 
                throw new ArgumentException( SR.GetString( SR.Role_provider_name_invalid ), "providerName" );
 
            _ProviderName = providerName;
            if (Roles.Providers[_ProviderName] == null)
                throw new ArgumentException(SR.GetString(SR.Role_provider_name_invalid), "providerName");
            _Identity = identity; 
            if (identity.IsAuthenticated)
                InitFromEncryptedTicket(encryptedTicket); 
            else 
                Init();
        } 


        private void InitFromEncryptedTicket( string encryptedTicket )
        { 
            if (HostingEnvironment.IsHosted && EtwTrace.IsTraceEnabled(EtwTraceLevel.Information, EtwTraceFlags.AppSvc))
                EtwTrace.Trace(EtwTraceType.ETW_TYPE_ROLE_BEGIN, HttpContext.Current.WorkerRequest); 
 
            if (string.IsNullOrEmpty(encryptedTicket))
                goto Exit; 

            byte[] bTicket = CookieProtectionHelper.Decode(Roles.CookieProtectionValue, encryptedTicket);
            if (bTicket == null)
                goto Exit; 

            RolePrincipal   rp = null; 
            MemoryStream    ms = null; 
            try{
                ms = new System.IO.MemoryStream(bTicket); 
                rp = (new BinaryFormatter()).Deserialize(ms) as RolePrincipal;
            } catch {
            } finally {
                ms.Close(); 
            }
            if (rp == null) 
                goto Exit; 
            if (!StringUtil.EqualsIgnoreCase(rp._Username, _Identity.Name))
                goto Exit; 
            if (!StringUtil.EqualsIgnoreCase(rp._ProviderName, _ProviderName))
                goto Exit;
            if (DateTime.UtcNow > rp._ExpireDate)
                goto Exit; 

            _Version = rp._Version; 
            _ExpireDate = rp._ExpireDate; 
            _IssueDate = rp._IssueDate;
            _IsRoleListCached = rp._IsRoleListCached; 
            _CachedListChanged = false;
            _Username = rp._Username;
            _Roles = rp._Roles;
 

            RenewIfOld(); 
 
            if (HostingEnvironment.IsHosted && EtwTrace.IsTraceEnabled(EtwTraceLevel.Information, EtwTraceFlags.AppSvc))
                EtwTrace.Trace( EtwTraceType.ETW_TYPE_ROLE_END, HttpContext.Current.WorkerRequest, "RolePrincipal", _Identity.Name); 

            return;
        Exit:
            Init(); 
            _CachedListChanged = true;
            if (HostingEnvironment.IsHosted && EtwTrace.IsTraceEnabled(EtwTraceLevel.Information, EtwTraceFlags.AppSvc)) 
                EtwTrace.Trace(EtwTraceType.ETW_TYPE_ROLE_END, HttpContext.Current.WorkerRequest, "RolePrincipal", _Identity.Name); 
            return;
        } 

        ////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////// 

        private void Init() { 
            _Version = 1; 
            _IssueDate = DateTime.UtcNow;
            _ExpireDate = DateTime.UtcNow.AddMinutes(Roles.CookieTimeout); 
            //_CookiePath = Roles.CookiePath;
            _IsRoleListCached = false;
            _CachedListChanged = false;
            if (_ProviderName == null) 
                _ProviderName = Roles.Provider.Name;
            if (_Roles == null) 
                _Roles = new HybridDictionary(true); 
            if (_Identity != null)
                _Username = _Identity.Name; 
        }

        ////////////////////////////////////////////////////////////
        // Public properties 

        public int       Version           { get { return _Version;}} 
        public DateTime  ExpireDate        { get { return _ExpireDate.ToLocalTime();}} 
        public DateTime  IssueDate         { get { return _IssueDate.ToLocalTime();}}
        public bool      Expired           { get { return _ExpireDate < DateTime.Now;}} 
        public String    CookiePath        { get { return Roles.CookiePath;}} //
        public IIdentity Identity          { get { return _Identity; }}
        public bool      IsRoleListCached  { get { return _IsRoleListCached; }}
        public bool      CachedListChanged { get { return _CachedListChanged; }} 
        public string    ProviderName      { get { return _ProviderName; } }
 
        //////////////////////////////////////////////////////////// 
        ////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////// 
        ////////////////////////////////////////////////////////////
        // Public functions

 
        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public string ToEncryptedTicket() 
        { 
            if (!Roles.Enabled)
                return null; 
            if (_Identity != null && !_Identity.IsAuthenticated)
                return null;
            if (_Identity == null && string.IsNullOrEmpty(_Username))
                return null; 
            if (_Roles.Count > Roles.MaxCachedResults)
                return null; 
 
            MemoryStream ms = new System.IO.MemoryStream();
            byte[] buf = null; 
            IIdentity id = _Identity;
            try {
                _Identity = null;
                BinaryFormatter bf = new BinaryFormatter(); 
                bf.Serialize(ms, this);
                buf = ms.ToArray(); 
            } finally { 
                ms.Close();
                _Identity = id; 
            }

            return CookieProtectionHelper.Encode(Roles.CookieProtectionValue, buf, buf.Length);
        } 

        //////////////////////////////////////////////////////////// 
        //////////////////////////////////////////////////////////// 
        ////////////////////////////////////////////////////////////
        private void RenewIfOld() { 
            if (!Roles.CookieSlidingExpiration)
                return;
            DateTime dtN = DateTime.UtcNow;
            TimeSpan t1  = dtN - _IssueDate; 
            TimeSpan t2  = _ExpireDate - dtN;
 
            if (t2 > t1) 
                return;
            _ExpireDate = dtN + (_ExpireDate - _IssueDate); 
            _IssueDate = dtN;
            _CachedListChanged = true;
        }
 
        ////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////// 
        //////////////////////////////////////////////////////////// 
        public string[] GetRoles()
        { 
            if (_Identity == null)
                throw new ProviderException(SR.GetString(SR.Role_Principal_not_fully_constructed));

            if (!_Identity.IsAuthenticated) 
                return new string[0];
            string[] roles; 
 
            if (!_IsRoleListCached || !_GetRolesCalled) {
                _Roles.Clear(); 
                roles = Roles.Providers[_ProviderName].GetRolesForUser(Identity.Name);
                foreach (string role in roles)
                    if (_Roles[role] == null)
                        _Roles.Add(role, String.Empty); 
                _IsRoleListCached = true;
                _CachedListChanged = true; 
                _GetRolesCalled = true; 
                return roles;
            } else { 
                roles = new string[_Roles.Count];
                int index = 0;
                foreach (string role in _Roles.Keys)
                    roles[index++] = role; 
                return roles;
            } 
        } 

        //////////////////////////////////////////////////////////// 
        ////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////

        public bool IsInRole(string role) 
        {
            if (_Identity == null) 
                throw new ProviderException(SR.GetString(SR.Role_Principal_not_fully_constructed)); 

            if (!_Identity.IsAuthenticated || role == null) 
                return false;
            role = role.Trim();
            if (!IsRoleListCached) {
                _Roles.Clear(); 
                string[] roles = Roles.Providers[_ProviderName].GetRolesForUser(Identity.Name);
                foreach(string roleTemp in roles) 
                    if (_Roles[roleTemp] == null) 
                        _Roles.Add(roleTemp, String.Empty);
 
                _IsRoleListCached = true;
                _CachedListChanged = true;
            }
            return _Roles[role] != null; 
        }
 
        public void SetDirty() 
        {
            _IsRoleListCached = false; 
            _CachedListChanged = true;
        }

        private RolePrincipal(SerializationInfo info, StreamingContext context) 
        {
            _Version = info.GetInt32("_Version"); 
            _ExpireDate = info.GetDateTime("_ExpireDate"); 
            _IssueDate = info.GetDateTime("_IssueDate");
            try { 
                _Identity = info.GetValue("_Identity", typeof(IIdentity)) as IIdentity;
            } catch { } // Ignore Exceptions
            _ProviderName = info.GetString("_ProviderName");
            _Username = info.GetString("_Username"); 
            _IsRoleListCached = info.GetBoolean("_IsRoleListCached");
            _Roles = new HybridDictionary(true); 
            string allRoles = info.GetString("_AllRoles"); 
            if (allRoles != null) {
                foreach(string role in allRoles.Split(new char[] {','})) 
                    if (_Roles[role] == null)
                        _Roles.Add(role, String.Empty);
            }
        } 

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) 
        { 
            info.AddValue("_Version", _Version);
 
            info.AddValue("_ExpireDate", _ExpireDate);
            info.AddValue("_IssueDate", _IssueDate);
            if (_Identity != null) {
                try { 
                    info.AddValue("_Identity", _Identity);
                } catch { } // Ignore Exceptions 
            } 
            info.AddValue("_ProviderName", _ProviderName);
            info.AddValue("_Username", _Identity == null ? _Username : _Identity.Name); 
            info.AddValue("_IsRoleListCached", _IsRoleListCached);
            if (_Roles.Count > 0) {
                StringBuilder sb = new StringBuilder(_Roles.Count * 10);
                foreach(object role in _Roles.Keys) 
                    sb.Append(((string)role) + ",");
                string allRoles = sb.ToString(); 
                info.AddValue("_AllRoles", allRoles.Substring(0, allRoles.Length - 1)); 
            } else {
                info.AddValue("_AllRoles", String.Empty); 
            }
        }

        //////////////////////////////////////////////////////////// 
        ////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////// 
        private int         _Version; 
        private DateTime    _ExpireDate;
        private DateTime    _IssueDate; 
        private IIdentity   _Identity;
        private string      _ProviderName;
        private string      _Username;
        private bool        _IsRoleListCached; 
        private bool        _CachedListChanged;
 
        [NonSerialized] 
        private HybridDictionary _Roles = null;
        [NonSerialized] 
        private bool _GetRolesCalled;
        ////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////// 
     }
} 
//------------------------------------------------------------------------------ 
// <copyright file="RolePrincipal.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 * RolePrincipal 
 *
 * Copyright (c) 2002 Microsoft Corporation 
 */

namespace System.Web.Security {
    using  System.Security.Principal; 
    using System.Security.Permissions;
    using System.Collections; 
    using System.Collections.Specialized; 
    using System.Web;
    using System.Web.Hosting; 
    using System.Text;
    using System.Web.Configuration;
    using System.Web.Util;
    using System.Globalization; 
    using System.Runtime.Serialization;
    using System.IO; 
    using System.Configuration.Provider; 
    using System.Runtime.Serialization.Formatters.Binary;
 
    [Serializable]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public sealed class RolePrincipal : IPrincipal, ISerializable
    { 
        public RolePrincipal(IIdentity identity, string encryptedTicket)
        { 
            if (identity == null) 
                throw new ArgumentNullException( "identity" );
 
            if (encryptedTicket == null)
                throw new ArgumentNullException( "encryptedTicket" );
            _Identity = identity;
            _ProviderName = Roles.Provider.Name; 
            if (identity.IsAuthenticated)
                InitFromEncryptedTicket(encryptedTicket); 
            else 
                Init();
        } 

        public RolePrincipal(IIdentity identity)
        {
            if (identity == null) 
                throw new ArgumentNullException( "identity" );
            _Identity = identity; 
            Init(); 
        }
 
        public RolePrincipal(string providerName, IIdentity identity )
        {
            if (identity == null)
                throw new ArgumentNullException( "identity" ); 

            if( providerName == null) 
                throw new ArgumentException( SR.GetString( SR.Role_provider_name_invalid ), "providerName" ); 

            _ProviderName = providerName; 
            if (Roles.Providers[providerName] == null)
                throw new ArgumentException(SR.GetString(SR.Role_provider_name_invalid), "providerName");

            _Identity = identity; 
            Init();
        } 
 
        public RolePrincipal(string providerName, IIdentity identity, string encryptedTicket )
        { 
            if (identity == null)
                throw new ArgumentNullException( "identity" );

            if (encryptedTicket == null) 
                throw new ArgumentNullException( "encryptedTicket" );
 
            if( providerName == null) 
                throw new ArgumentException( SR.GetString( SR.Role_provider_name_invalid ), "providerName" );
 
            _ProviderName = providerName;
            if (Roles.Providers[_ProviderName] == null)
                throw new ArgumentException(SR.GetString(SR.Role_provider_name_invalid), "providerName");
            _Identity = identity; 
            if (identity.IsAuthenticated)
                InitFromEncryptedTicket(encryptedTicket); 
            else 
                Init();
        } 


        private void InitFromEncryptedTicket( string encryptedTicket )
        { 
            if (HostingEnvironment.IsHosted && EtwTrace.IsTraceEnabled(EtwTraceLevel.Information, EtwTraceFlags.AppSvc))
                EtwTrace.Trace(EtwTraceType.ETW_TYPE_ROLE_BEGIN, HttpContext.Current.WorkerRequest); 
 
            if (string.IsNullOrEmpty(encryptedTicket))
                goto Exit; 

            byte[] bTicket = CookieProtectionHelper.Decode(Roles.CookieProtectionValue, encryptedTicket);
            if (bTicket == null)
                goto Exit; 

            RolePrincipal   rp = null; 
            MemoryStream    ms = null; 
            try{
                ms = new System.IO.MemoryStream(bTicket); 
                rp = (new BinaryFormatter()).Deserialize(ms) as RolePrincipal;
            } catch {
            } finally {
                ms.Close(); 
            }
            if (rp == null) 
                goto Exit; 
            if (!StringUtil.EqualsIgnoreCase(rp._Username, _Identity.Name))
                goto Exit; 
            if (!StringUtil.EqualsIgnoreCase(rp._ProviderName, _ProviderName))
                goto Exit;
            if (DateTime.UtcNow > rp._ExpireDate)
                goto Exit; 

            _Version = rp._Version; 
            _ExpireDate = rp._ExpireDate; 
            _IssueDate = rp._IssueDate;
            _IsRoleListCached = rp._IsRoleListCached; 
            _CachedListChanged = false;
            _Username = rp._Username;
            _Roles = rp._Roles;
 

            RenewIfOld(); 
 
            if (HostingEnvironment.IsHosted && EtwTrace.IsTraceEnabled(EtwTraceLevel.Information, EtwTraceFlags.AppSvc))
                EtwTrace.Trace( EtwTraceType.ETW_TYPE_ROLE_END, HttpContext.Current.WorkerRequest, "RolePrincipal", _Identity.Name); 

            return;
        Exit:
            Init(); 
            _CachedListChanged = true;
            if (HostingEnvironment.IsHosted && EtwTrace.IsTraceEnabled(EtwTraceLevel.Information, EtwTraceFlags.AppSvc)) 
                EtwTrace.Trace(EtwTraceType.ETW_TYPE_ROLE_END, HttpContext.Current.WorkerRequest, "RolePrincipal", _Identity.Name); 
            return;
        } 

        ////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////// 

        private void Init() { 
            _Version = 1; 
            _IssueDate = DateTime.UtcNow;
            _ExpireDate = DateTime.UtcNow.AddMinutes(Roles.CookieTimeout); 
            //_CookiePath = Roles.CookiePath;
            _IsRoleListCached = false;
            _CachedListChanged = false;
            if (_ProviderName == null) 
                _ProviderName = Roles.Provider.Name;
            if (_Roles == null) 
                _Roles = new HybridDictionary(true); 
            if (_Identity != null)
                _Username = _Identity.Name; 
        }

        ////////////////////////////////////////////////////////////
        // Public properties 

        public int       Version           { get { return _Version;}} 
        public DateTime  ExpireDate        { get { return _ExpireDate.ToLocalTime();}} 
        public DateTime  IssueDate         { get { return _IssueDate.ToLocalTime();}}
        public bool      Expired           { get { return _ExpireDate < DateTime.Now;}} 
        public String    CookiePath        { get { return Roles.CookiePath;}} //
        public IIdentity Identity          { get { return _Identity; }}
        public bool      IsRoleListCached  { get { return _IsRoleListCached; }}
        public bool      CachedListChanged { get { return _CachedListChanged; }} 
        public string    ProviderName      { get { return _ProviderName; } }
 
        //////////////////////////////////////////////////////////// 
        ////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////// 
        ////////////////////////////////////////////////////////////
        // Public functions

 
        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public string ToEncryptedTicket() 
        { 
            if (!Roles.Enabled)
                return null; 
            if (_Identity != null && !_Identity.IsAuthenticated)
                return null;
            if (_Identity == null && string.IsNullOrEmpty(_Username))
                return null; 
            if (_Roles.Count > Roles.MaxCachedResults)
                return null; 
 
            MemoryStream ms = new System.IO.MemoryStream();
            byte[] buf = null; 
            IIdentity id = _Identity;
            try {
                _Identity = null;
                BinaryFormatter bf = new BinaryFormatter(); 
                bf.Serialize(ms, this);
                buf = ms.ToArray(); 
            } finally { 
                ms.Close();
                _Identity = id; 
            }

            return CookieProtectionHelper.Encode(Roles.CookieProtectionValue, buf, buf.Length);
        } 

        //////////////////////////////////////////////////////////// 
        //////////////////////////////////////////////////////////// 
        ////////////////////////////////////////////////////////////
        private void RenewIfOld() { 
            if (!Roles.CookieSlidingExpiration)
                return;
            DateTime dtN = DateTime.UtcNow;
            TimeSpan t1  = dtN - _IssueDate; 
            TimeSpan t2  = _ExpireDate - dtN;
 
            if (t2 > t1) 
                return;
            _ExpireDate = dtN + (_ExpireDate - _IssueDate); 
            _IssueDate = dtN;
            _CachedListChanged = true;
        }
 
        ////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////// 
        //////////////////////////////////////////////////////////// 
        public string[] GetRoles()
        { 
            if (_Identity == null)
                throw new ProviderException(SR.GetString(SR.Role_Principal_not_fully_constructed));

            if (!_Identity.IsAuthenticated) 
                return new string[0];
            string[] roles; 
 
            if (!_IsRoleListCached || !_GetRolesCalled) {
                _Roles.Clear(); 
                roles = Roles.Providers[_ProviderName].GetRolesForUser(Identity.Name);
                foreach (string role in roles)
                    if (_Roles[role] == null)
                        _Roles.Add(role, String.Empty); 
                _IsRoleListCached = true;
                _CachedListChanged = true; 
                _GetRolesCalled = true; 
                return roles;
            } else { 
                roles = new string[_Roles.Count];
                int index = 0;
                foreach (string role in _Roles.Keys)
                    roles[index++] = role; 
                return roles;
            } 
        } 

        //////////////////////////////////////////////////////////// 
        ////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////

        public bool IsInRole(string role) 
        {
            if (_Identity == null) 
                throw new ProviderException(SR.GetString(SR.Role_Principal_not_fully_constructed)); 

            if (!_Identity.IsAuthenticated || role == null) 
                return false;
            role = role.Trim();
            if (!IsRoleListCached) {
                _Roles.Clear(); 
                string[] roles = Roles.Providers[_ProviderName].GetRolesForUser(Identity.Name);
                foreach(string roleTemp in roles) 
                    if (_Roles[roleTemp] == null) 
                        _Roles.Add(roleTemp, String.Empty);
 
                _IsRoleListCached = true;
                _CachedListChanged = true;
            }
            return _Roles[role] != null; 
        }
 
        public void SetDirty() 
        {
            _IsRoleListCached = false; 
            _CachedListChanged = true;
        }

        private RolePrincipal(SerializationInfo info, StreamingContext context) 
        {
            _Version = info.GetInt32("_Version"); 
            _ExpireDate = info.GetDateTime("_ExpireDate"); 
            _IssueDate = info.GetDateTime("_IssueDate");
            try { 
                _Identity = info.GetValue("_Identity", typeof(IIdentity)) as IIdentity;
            } catch { } // Ignore Exceptions
            _ProviderName = info.GetString("_ProviderName");
            _Username = info.GetString("_Username"); 
            _IsRoleListCached = info.GetBoolean("_IsRoleListCached");
            _Roles = new HybridDictionary(true); 
            string allRoles = info.GetString("_AllRoles"); 
            if (allRoles != null) {
                foreach(string role in allRoles.Split(new char[] {','})) 
                    if (_Roles[role] == null)
                        _Roles.Add(role, String.Empty);
            }
        } 

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) 
        { 
            info.AddValue("_Version", _Version);
 
            info.AddValue("_ExpireDate", _ExpireDate);
            info.AddValue("_IssueDate", _IssueDate);
            if (_Identity != null) {
                try { 
                    info.AddValue("_Identity", _Identity);
                } catch { } // Ignore Exceptions 
            } 
            info.AddValue("_ProviderName", _ProviderName);
            info.AddValue("_Username", _Identity == null ? _Username : _Identity.Name); 
            info.AddValue("_IsRoleListCached", _IsRoleListCached);
            if (_Roles.Count > 0) {
                StringBuilder sb = new StringBuilder(_Roles.Count * 10);
                foreach(object role in _Roles.Keys) 
                    sb.Append(((string)role) + ",");
                string allRoles = sb.ToString(); 
                info.AddValue("_AllRoles", allRoles.Substring(0, allRoles.Length - 1)); 
            } else {
                info.AddValue("_AllRoles", String.Empty); 
            }
        }

        //////////////////////////////////////////////////////////// 
        ////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////// 
        private int         _Version; 
        private DateTime    _ExpireDate;
        private DateTime    _IssueDate; 
        private IIdentity   _Identity;
        private string      _ProviderName;
        private string      _Username;
        private bool        _IsRoleListCached; 
        private bool        _CachedListChanged;
 
        [NonSerialized] 
        private HybridDictionary _Roles = null;
        [NonSerialized] 
        private bool _GetRolesCalled;
        ////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////// 
     }
} 
