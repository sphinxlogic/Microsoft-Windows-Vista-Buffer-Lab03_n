 
    /// <summary><para>
    ///    Provides support for ip configuation information and statistics.
    ///</para></summary>
    /// 
namespace System.Net.NetworkInformation {
 
    using System.Net; 
    using System.Net.Sockets;
    using System; 
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.ComponentModel;
    using System.Security.Permissions; 
    using Microsoft.Win32;
 
 

    /// <summary> 
    /// Provides information specific to a network
    /// interface.
    /// </summary>
    /// <remarks> 
    /// <para>Provides information specific to a network interface. A network interface can have more than one IPAddress associated with it. If the machine is &gt;= XP, we call the native GetAdaptersAddresses api to
    /// prepopulate all of the interface instances and most of their associated information. Otherwise, 
    /// GetAdaptersInfo is called.</para> 
    /// </remarks>
    internal class SystemIPInterfaceProperties:IPInterfaceProperties { 

        //common properties
        uint mtu;
 
        //Unfortunately, any interface can
        //have two completely different valid indexes for ipv4 and ipv6 
        internal uint index = 0; 
        internal uint ipv6Index = 0;
        internal IPVersion versionSupported = IPVersion.None; 

        //these are valid for all interfaces
        bool dnsEnabled = false;
        bool dynamicDnsEnabled = false; //os>=xp only 
        IPAddressCollection dnsAddresses = null;
        UnicastIPAddressInformationCollection unicastAddresses = null; 
 
        //OS >= XP only
        MulticastIPAddressInformationCollection multicastAddresses = null; 
        IPAddressInformationCollection anycastAddresses = null;
        AdapterFlags adapterFlags;
        string dnsSuffix;
        string name; 

        //ipv4 only 
        // 
        SystemIPv4InterfaceProperties ipv4Properties;
        SystemIPv6InterfaceProperties ipv6Properties; 


        private SystemIPInterfaceProperties(){}
 
        //This constructor is used only in the OS>=XP case
        //and uses the GetAdapterAddresses api 
        internal SystemIPInterfaceProperties(FixedInfo fixedInfo, IpAdapterAddresses ipAdapterAddresses) { 

            //network params info 
            dnsEnabled = fixedInfo.EnableDns;

            //store the common api information
            index = ipAdapterAddresses.index; 
            name = ipAdapterAddresses.AdapterName;
 
 
            //api specific info
            ipv6Index = ipAdapterAddresses.ipv6Index; 

            if (index > 0)
                versionSupported |= IPVersion.IPv4;
            if (ipv6Index > 0) 
                versionSupported |= IPVersion.IPv6;
 
            mtu = ipAdapterAddresses.mtu; 
            adapterFlags = ipAdapterAddresses.flags;
            dnsSuffix = ipAdapterAddresses.dnsSuffix; 
            dynamicDnsEnabled = ((ipAdapterAddresses.flags & AdapterFlags.DnsEnabled) > 0);
            multicastAddresses = SystemMulticastIPAddressInformation.ToAddressInformationCollection(ipAdapterAddresses.FirstMulticastAddress);
            dnsAddresses = SystemIPAddressInformation.ToAddressCollection(ipAdapterAddresses.FirstDnsServerAddress,versionSupported);
            anycastAddresses = SystemIPAddressInformation.ToAddressInformationCollection(ipAdapterAddresses.FirstAnycastAddress,versionSupported); 
            unicastAddresses = SystemUnicastIPAddressInformation.ToAddressInformationCollection(ipAdapterAddresses.FirstUnicastAddress);
 
            if (ipv6Index > 0){ 
                ipv6Properties = new SystemIPv6InterfaceProperties(ipv6Index,mtu);
            } 
        }


        //This constructor is used only in the OS<XP case 
        //and uses the GetAdapterInfo api
        internal SystemIPInterfaceProperties(FixedInfo fixedInfo, IpAdapterInfo ipAdapterInfo) { 
 
            //network params info
            dnsEnabled = fixedInfo.EnableDns; 

            //store the common api information
            name = ipAdapterInfo.adapterName;
            index = ipAdapterInfo.index; 
            multicastAddresses = new MulticastIPAddressInformationCollection();
            anycastAddresses = new IPAddressInformationCollection(); 
 
            if (index > 0)
                versionSupported |= IPVersion.IPv4; 


            //have to get the suffix from the registry if 2k
            if (ComNetOS.IsWin2K) 
                ReadRegDnsSuffix();
 
 
            //get ipaddresses
            unicastAddresses = new UnicastIPAddressInformationCollection(); 
            ArrayList ipAddresses = ipAdapterInfo.ipAddressList.ToIPExtendedAddressArrayList();
            foreach (IPExtendedAddress address in ipAddresses) {
                unicastAddresses.InternalAdd(new SystemUnicastIPAddressInformation(ipAdapterInfo,address));
            } 

            try { 
                ipv4Properties = new SystemIPv4InterfaceProperties(fixedInfo,ipAdapterInfo); 
                if (dnsAddresses == null || dnsAddresses.Count == 0) {
                    dnsAddresses = ((SystemIPv4InterfaceProperties)ipv4Properties).DnsAddresses; 
                }
            }
            catch (NetworkInformationException exception) {
                if (exception.ErrorCode != IpHelperErrors.ErrorInvalidParameter) 
                    throw;
            } 
        } 

 


        public override bool IsDnsEnabled{get { return dnsEnabled;}}
 
        //only valid for OS>=XP
        public override bool IsDynamicDnsEnabled{get {return dynamicDnsEnabled;}} 
 

        // Stats specific for Ipv4.  Ipv6 will be added in Longhorn 
        public override IPv4InterfaceProperties GetIPv4Properties(){
            if (index == 0) {
                throw new NetworkInformationException(SocketError.ProtocolNotSupported);
            } 
            return ipv4Properties;
        } 
 
        // Stats specific for Ipv4.  Ipv6 will be added in Longhorn
        public override IPv6InterfaceProperties GetIPv6Properties(){ 
            if (ipv6Index == 0) {
                throw new NetworkInformationException(SocketError.ProtocolNotSupported);
            }
            return ipv6Properties; 
        }
 
 
        public override string DnsSuffix {
            get { 
                if (! ComNetOS.IsWin2K)
                    throw new PlatformNotSupportedException(SR.GetString(SR.Win2000Required));
                return dnsSuffix;
            } 
        }
 
 
        //returns the addresses specified by the address type.
        public override IPAddressInformationCollection AnycastAddresses{ 
            get{
                return anycastAddresses;
            }
        } 

        //returns the addresses specified by the address type. 
        public override UnicastIPAddressInformationCollection UnicastAddresses{ 
            get{
                return unicastAddresses; 
            }
        }

        //returns the addresses specified by the address type. 
        public override MulticastIPAddressInformationCollection MulticastAddresses{
            get{ 
                return multicastAddresses; 
            }
        } 

        //returns the addresses specified by the address type.
        public override IPAddressCollection DnsAddresses{
            get{ 
                return dnsAddresses;
            } 
        } 

 
        /// <summary>IP Address of the default gateway.</summary>
        public override GatewayIPAddressInformationCollection GatewayAddresses{
            get{
                if (ipv4Properties != null) { 
                    return ipv4Properties.GetGatewayAddresses();
                } 
                return new GatewayIPAddressInformationCollection(); 
            }
        } 


        public override IPAddressCollection DhcpServerAddresses{
            get{ 
                if (ipv4Properties != null) {
                    return ipv4Properties.GetDhcpServerAddresses(); 
                } 
                return new IPAddressCollection();
            } 
        }


        public override IPAddressCollection WinsServersAddresses{ 
            get{
                if (ipv4Properties != null) { 
                    return ipv4Properties.GetWinsServersAddresses(); 
                }
                return new IPAddressCollection(); 
            }
        }

 

        //This method is invoked to fill in the rest of the values 
        //for ipv4 interfaces that aren't available via GetAdapterAddresses 
        internal bool Update(FixedInfo fixedInfo,IpAdapterInfo ipAdapterInfo) {
            try { 
                ArrayList ipAddresses = ipAdapterInfo.ipAddressList.ToIPExtendedAddressArrayList();
                foreach (IPExtendedAddress extendedAddress in ipAddresses) {
                    foreach(SystemUnicastIPAddressInformation unicastAddress in unicastAddresses){
                        if(extendedAddress.address.Equals(unicastAddress.Address)){ 
                            unicastAddress.ipv4Mask = extendedAddress.mask;
                        } 
                    } 
                }
 
                ipv4Properties = new SystemIPv4InterfaceProperties(fixedInfo,ipAdapterInfo);
                if (dnsAddresses == null || dnsAddresses.Count == 0) {
                    dnsAddresses = ((SystemIPv4InterfaceProperties)ipv4Properties).DnsAddresses;
                } 
            }
            catch (NetworkInformationException exception) { 
                if (exception.ErrorCode == IpHelperErrors.ErrorInvalidParameter || 
                    exception.ErrorCode == IpHelperErrors.ErrorInvalidData ||
                    exception.ErrorCode == IpHelperErrors.ErrorNoData || 
                    exception.ErrorCode == IpHelperErrors.ErrorInvalidFunction ||
                    exception.ErrorCode == IpHelperErrors.ErrorNoSuchDevice)
                    return false;
                throw; 
            }
            return true; 
        } 

 
        [RegistryPermission(SecurityAction.Assert, Read="HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces")]
        private void ReadRegDnsSuffix() {
            RegistryKey key = null;
            try { 
                string subKey = "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\" + name;
                key = Registry.LocalMachine.OpenSubKey(subKey); 
                if (null != key) { 
                    dnsSuffix = (string) key.GetValue("DhcpDomain");
                    if (dnsSuffix == null ) { 
                       dnsSuffix = (string) key.GetValue("Domain");
                       if (dnsSuffix == null) {
                           dnsSuffix = String.Empty;
                       } 
                    }
                } 
            } 
            finally{
                if (null != key){ 
                    key.Close();
                }
            }
        } 
       /*
        [RegistryPermission(SecurityAction.Assert, Read="HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces")] 
        private uint ReadRegMetric(bool ipv6) { 
            RegistryKey key = null;
            uint metric = 0; 
            try {
                string subKey;
                if(ipv6){
                    "SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters\\Interfaces\\" + name; 
                }
                else{ 
                    "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\" + name; 
                }
 
                key = Registry.LocalMachine.OpenSubKey(subKey);
                if (null != key) {
                    metric = (uint) key.GetValue("InterfaceMetric");
                } 
            }
            finally{ 
                if (null != key){ 
                    key.Close();
                } 
            }
            return metric;
        }
        */ 

    } 
} 
 
    /// <summary><para>
    ///    Provides support for ip configuation information and statistics.
    ///</para></summary>
    /// 
namespace System.Net.NetworkInformation {
 
    using System.Net; 
    using System.Net.Sockets;
    using System; 
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.ComponentModel;
    using System.Security.Permissions; 
    using Microsoft.Win32;
 
 

    /// <summary> 
    /// Provides information specific to a network
    /// interface.
    /// </summary>
    /// <remarks> 
    /// <para>Provides information specific to a network interface. A network interface can have more than one IPAddress associated with it. If the machine is &gt;= XP, we call the native GetAdaptersAddresses api to
    /// prepopulate all of the interface instances and most of their associated information. Otherwise, 
    /// GetAdaptersInfo is called.</para> 
    /// </remarks>
    internal class SystemIPInterfaceProperties:IPInterfaceProperties { 

        //common properties
        uint mtu;
 
        //Unfortunately, any interface can
        //have two completely different valid indexes for ipv4 and ipv6 
        internal uint index = 0; 
        internal uint ipv6Index = 0;
        internal IPVersion versionSupported = IPVersion.None; 

        //these are valid for all interfaces
        bool dnsEnabled = false;
        bool dynamicDnsEnabled = false; //os>=xp only 
        IPAddressCollection dnsAddresses = null;
        UnicastIPAddressInformationCollection unicastAddresses = null; 
 
        //OS >= XP only
        MulticastIPAddressInformationCollection multicastAddresses = null; 
        IPAddressInformationCollection anycastAddresses = null;
        AdapterFlags adapterFlags;
        string dnsSuffix;
        string name; 

        //ipv4 only 
        // 
        SystemIPv4InterfaceProperties ipv4Properties;
        SystemIPv6InterfaceProperties ipv6Properties; 


        private SystemIPInterfaceProperties(){}
 
        //This constructor is used only in the OS>=XP case
        //and uses the GetAdapterAddresses api 
        internal SystemIPInterfaceProperties(FixedInfo fixedInfo, IpAdapterAddresses ipAdapterAddresses) { 

            //network params info 
            dnsEnabled = fixedInfo.EnableDns;

            //store the common api information
            index = ipAdapterAddresses.index; 
            name = ipAdapterAddresses.AdapterName;
 
 
            //api specific info
            ipv6Index = ipAdapterAddresses.ipv6Index; 

            if (index > 0)
                versionSupported |= IPVersion.IPv4;
            if (ipv6Index > 0) 
                versionSupported |= IPVersion.IPv6;
 
            mtu = ipAdapterAddresses.mtu; 
            adapterFlags = ipAdapterAddresses.flags;
            dnsSuffix = ipAdapterAddresses.dnsSuffix; 
            dynamicDnsEnabled = ((ipAdapterAddresses.flags & AdapterFlags.DnsEnabled) > 0);
            multicastAddresses = SystemMulticastIPAddressInformation.ToAddressInformationCollection(ipAdapterAddresses.FirstMulticastAddress);
            dnsAddresses = SystemIPAddressInformation.ToAddressCollection(ipAdapterAddresses.FirstDnsServerAddress,versionSupported);
            anycastAddresses = SystemIPAddressInformation.ToAddressInformationCollection(ipAdapterAddresses.FirstAnycastAddress,versionSupported); 
            unicastAddresses = SystemUnicastIPAddressInformation.ToAddressInformationCollection(ipAdapterAddresses.FirstUnicastAddress);
 
            if (ipv6Index > 0){ 
                ipv6Properties = new SystemIPv6InterfaceProperties(ipv6Index,mtu);
            } 
        }


        //This constructor is used only in the OS<XP case 
        //and uses the GetAdapterInfo api
        internal SystemIPInterfaceProperties(FixedInfo fixedInfo, IpAdapterInfo ipAdapterInfo) { 
 
            //network params info
            dnsEnabled = fixedInfo.EnableDns; 

            //store the common api information
            name = ipAdapterInfo.adapterName;
            index = ipAdapterInfo.index; 
            multicastAddresses = new MulticastIPAddressInformationCollection();
            anycastAddresses = new IPAddressInformationCollection(); 
 
            if (index > 0)
                versionSupported |= IPVersion.IPv4; 


            //have to get the suffix from the registry if 2k
            if (ComNetOS.IsWin2K) 
                ReadRegDnsSuffix();
 
 
            //get ipaddresses
            unicastAddresses = new UnicastIPAddressInformationCollection(); 
            ArrayList ipAddresses = ipAdapterInfo.ipAddressList.ToIPExtendedAddressArrayList();
            foreach (IPExtendedAddress address in ipAddresses) {
                unicastAddresses.InternalAdd(new SystemUnicastIPAddressInformation(ipAdapterInfo,address));
            } 

            try { 
                ipv4Properties = new SystemIPv4InterfaceProperties(fixedInfo,ipAdapterInfo); 
                if (dnsAddresses == null || dnsAddresses.Count == 0) {
                    dnsAddresses = ((SystemIPv4InterfaceProperties)ipv4Properties).DnsAddresses; 
                }
            }
            catch (NetworkInformationException exception) {
                if (exception.ErrorCode != IpHelperErrors.ErrorInvalidParameter) 
                    throw;
            } 
        } 

 


        public override bool IsDnsEnabled{get { return dnsEnabled;}}
 
        //only valid for OS>=XP
        public override bool IsDynamicDnsEnabled{get {return dynamicDnsEnabled;}} 
 

        // Stats specific for Ipv4.  Ipv6 will be added in Longhorn 
        public override IPv4InterfaceProperties GetIPv4Properties(){
            if (index == 0) {
                throw new NetworkInformationException(SocketError.ProtocolNotSupported);
            } 
            return ipv4Properties;
        } 
 
        // Stats specific for Ipv4.  Ipv6 will be added in Longhorn
        public override IPv6InterfaceProperties GetIPv6Properties(){ 
            if (ipv6Index == 0) {
                throw new NetworkInformationException(SocketError.ProtocolNotSupported);
            }
            return ipv6Properties; 
        }
 
 
        public override string DnsSuffix {
            get { 
                if (! ComNetOS.IsWin2K)
                    throw new PlatformNotSupportedException(SR.GetString(SR.Win2000Required));
                return dnsSuffix;
            } 
        }
 
 
        //returns the addresses specified by the address type.
        public override IPAddressInformationCollection AnycastAddresses{ 
            get{
                return anycastAddresses;
            }
        } 

        //returns the addresses specified by the address type. 
        public override UnicastIPAddressInformationCollection UnicastAddresses{ 
            get{
                return unicastAddresses; 
            }
        }

        //returns the addresses specified by the address type. 
        public override MulticastIPAddressInformationCollection MulticastAddresses{
            get{ 
                return multicastAddresses; 
            }
        } 

        //returns the addresses specified by the address type.
        public override IPAddressCollection DnsAddresses{
            get{ 
                return dnsAddresses;
            } 
        } 

 
        /// <summary>IP Address of the default gateway.</summary>
        public override GatewayIPAddressInformationCollection GatewayAddresses{
            get{
                if (ipv4Properties != null) { 
                    return ipv4Properties.GetGatewayAddresses();
                } 
                return new GatewayIPAddressInformationCollection(); 
            }
        } 


        public override IPAddressCollection DhcpServerAddresses{
            get{ 
                if (ipv4Properties != null) {
                    return ipv4Properties.GetDhcpServerAddresses(); 
                } 
                return new IPAddressCollection();
            } 
        }


        public override IPAddressCollection WinsServersAddresses{ 
            get{
                if (ipv4Properties != null) { 
                    return ipv4Properties.GetWinsServersAddresses(); 
                }
                return new IPAddressCollection(); 
            }
        }

 

        //This method is invoked to fill in the rest of the values 
        //for ipv4 interfaces that aren't available via GetAdapterAddresses 
        internal bool Update(FixedInfo fixedInfo,IpAdapterInfo ipAdapterInfo) {
            try { 
                ArrayList ipAddresses = ipAdapterInfo.ipAddressList.ToIPExtendedAddressArrayList();
                foreach (IPExtendedAddress extendedAddress in ipAddresses) {
                    foreach(SystemUnicastIPAddressInformation unicastAddress in unicastAddresses){
                        if(extendedAddress.address.Equals(unicastAddress.Address)){ 
                            unicastAddress.ipv4Mask = extendedAddress.mask;
                        } 
                    } 
                }
 
                ipv4Properties = new SystemIPv4InterfaceProperties(fixedInfo,ipAdapterInfo);
                if (dnsAddresses == null || dnsAddresses.Count == 0) {
                    dnsAddresses = ((SystemIPv4InterfaceProperties)ipv4Properties).DnsAddresses;
                } 
            }
            catch (NetworkInformationException exception) { 
                if (exception.ErrorCode == IpHelperErrors.ErrorInvalidParameter || 
                    exception.ErrorCode == IpHelperErrors.ErrorInvalidData ||
                    exception.ErrorCode == IpHelperErrors.ErrorNoData || 
                    exception.ErrorCode == IpHelperErrors.ErrorInvalidFunction ||
                    exception.ErrorCode == IpHelperErrors.ErrorNoSuchDevice)
                    return false;
                throw; 
            }
            return true; 
        } 

 
        [RegistryPermission(SecurityAction.Assert, Read="HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces")]
        private void ReadRegDnsSuffix() {
            RegistryKey key = null;
            try { 
                string subKey = "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\" + name;
                key = Registry.LocalMachine.OpenSubKey(subKey); 
                if (null != key) { 
                    dnsSuffix = (string) key.GetValue("DhcpDomain");
                    if (dnsSuffix == null ) { 
                       dnsSuffix = (string) key.GetValue("Domain");
                       if (dnsSuffix == null) {
                           dnsSuffix = String.Empty;
                       } 
                    }
                } 
            } 
            finally{
                if (null != key){ 
                    key.Close();
                }
            }
        } 
       /*
        [RegistryPermission(SecurityAction.Assert, Read="HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces")] 
        private uint ReadRegMetric(bool ipv6) { 
            RegistryKey key = null;
            uint metric = 0; 
            try {
                string subKey;
                if(ipv6){
                    "SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters\\Interfaces\\" + name; 
                }
                else{ 
                    "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\" + name; 
                }
 
                key = Registry.LocalMachine.OpenSubKey(subKey);
                if (null != key) {
                    metric = (uint) key.GetValue("InterfaceMetric");
                } 
            }
            finally{ 
                if (null != key){ 
                    key.Close();
                } 
            }
            return metric;
        }
        */ 

    } 
} 
