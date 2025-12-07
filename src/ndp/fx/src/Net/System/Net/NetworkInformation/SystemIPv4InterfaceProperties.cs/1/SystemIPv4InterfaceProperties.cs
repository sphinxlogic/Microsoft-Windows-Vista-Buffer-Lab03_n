 
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
 
 

 
    internal class SystemIPv4InterfaceProperties:IPv4InterfaceProperties{

        //these are only valid for ipv4 interfaces
        bool haveWins = false; 
        bool dhcpEnabled = false;
        bool routingEnabled = false; 
        bool autoConfigEnabled = false; 
        bool autoConfigActive = false;
        uint index = 0; 
        uint mtu = 0;

        //ipv4 addresses only
        GatewayIPAddressInformationCollection gatewayAddresses = null; 
        IPAddressCollection dhcpAddresses = null;
        IPAddressCollection winsServerAddresses = null; 
        internal IPAddressCollection dnsAddresses = null; 

        /* 
        // Consider removing.
        internal SystemIPv4InterfaceProperties(){
        }
        */ 

        internal SystemIPv4InterfaceProperties(FixedInfo fixedInfo, IpAdapterInfo ipAdapterInfo) 
        { 
            index = ipAdapterInfo.index;
            routingEnabled = fixedInfo.EnableRouting; 
            dhcpEnabled = ipAdapterInfo.dhcpEnabled;
            haveWins = ipAdapterInfo.haveWins;
            gatewayAddresses = ipAdapterInfo.gatewayList.ToIPGatewayAddressCollection();
            dhcpAddresses = ipAdapterInfo.dhcpServer.ToIPAddressCollection(); 
            IPAddressCollection primaryWinsServerAddresses = ipAdapterInfo.primaryWinsServer.ToIPAddressCollection();
            IPAddressCollection secondaryWinsServerAddresses = ipAdapterInfo.secondaryWinsServer.ToIPAddressCollection(); 
 
            //concat the winsserver addresses
            winsServerAddresses = new IPAddressCollection(); 

            foreach (IPAddress address in primaryWinsServerAddresses){
                winsServerAddresses.InternalAdd(address);
            } 
            foreach (IPAddress address in secondaryWinsServerAddresses){
                winsServerAddresses.InternalAdd(address); 
            } 

            SystemIPv4InterfaceStatistics s = new SystemIPv4InterfaceStatistics(index); 
            mtu = (uint)s.Mtu;

            if(ComNetOS.IsWin2K){
                GetPerAdapterInfo(ipAdapterInfo.index); 
            }
            else{ 
                dnsAddresses = fixedInfo.DnsAddresses; 
            }
        } 

        internal IPAddressCollection DnsAddresses{
            get { return dnsAddresses; }
        } 

 
        /// <summary>Only valid for Ipv4 Uses WINS for name resolution.</summary> 
        public override bool UsesWins{get {return haveWins;}}
 

        public override bool IsDhcpEnabled{
            get { return dhcpEnabled; }
        } 

        public override bool IsForwardingEnabled{get {return routingEnabled;}}      //proto 
 

 
        /// <summary>Auto configuration of an ipv4 address for a client
        /// on a network where a DHCP server
        /// isn't available.</summary>
        public override bool IsAutomaticPrivateAddressingEnabled{ 
            get{
                return autoConfigEnabled; 
            } 
        } // proto
 
        public override bool IsAutomaticPrivateAddressingActive{
            get{
                return autoConfigActive;
            } 
        }
 
 
        /// <summary>Specifies the Maximum transmission unit in bytes. Uses GetIFEntry.</summary>
        //We cache this to be consistent across all platforms 
        public override int Mtu{
            get {
                return (int) mtu;
            } 
        }
 
        public override int Index{ 
            get {
                return (int) index; 
            }
        }

        /// <summary>IP Address of the default gateway.</summary> 
        internal GatewayIPAddressInformationCollection GetGatewayAddresses(){
            return gatewayAddresses; 
        } 

        /// <summary>IP address of the DHCP sever.</summary> 

        internal IPAddressCollection GetDhcpServerAddresses(){
            return dhcpAddresses;
        } 

        /// <summary>IP addresses of the WINS servers.</summary> 
        internal IPAddressCollection GetWinsServersAddresses(){ 
            return winsServerAddresses;
        } 



        private void GetPerAdapterInfo(uint index) { 

            if (index != 0){ 
                uint size = 0; 
                SafeLocalFree buffer = null;
 
                uint result = UnsafeNetInfoNativeMethods.GetPerAdapterInfo(index,SafeLocalFree.Zero,ref size);
                while (result == IpHelperErrors.ErrorBufferOverflow) {
                    try {
                        //now we allocate the buffer and read the network parameters. 
                        buffer =  SafeLocalFree.LocalAlloc((int)size);
                        result = UnsafeNetInfoNativeMethods.GetPerAdapterInfo(index,buffer,ref size); 
                        if ( result == IpHelperErrors.Success ) { 
                            IpPerAdapterInfo ipPerAdapterInfo  = (IpPerAdapterInfo)Marshal.PtrToStructure(buffer.DangerousGetHandle(),typeof(IpPerAdapterInfo));
                            autoConfigEnabled = ipPerAdapterInfo.autoconfigEnabled; 
                            autoConfigActive = ipPerAdapterInfo.autoconfigActive;

                            //get dnsAddresses
                            dnsAddresses = ipPerAdapterInfo.dnsServerList.ToIPAddressCollection(); 
                        }
                    } 
                    finally { 
                        if(dnsAddresses == null){
                            dnsAddresses = new IPAddressCollection(); 
                        }

                        if (buffer != null)
                            buffer.Close(); 
                    }
                } 
 
                if(dnsAddresses == null){
                    dnsAddresses = new IPAddressCollection(); 
                }

                if (result != IpHelperErrors.Success) {
                    throw new NetworkInformationException((int)result); 
                }
            } 
        } 
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
 
 

 
    internal class SystemIPv4InterfaceProperties:IPv4InterfaceProperties{

        //these are only valid for ipv4 interfaces
        bool haveWins = false; 
        bool dhcpEnabled = false;
        bool routingEnabled = false; 
        bool autoConfigEnabled = false; 
        bool autoConfigActive = false;
        uint index = 0; 
        uint mtu = 0;

        //ipv4 addresses only
        GatewayIPAddressInformationCollection gatewayAddresses = null; 
        IPAddressCollection dhcpAddresses = null;
        IPAddressCollection winsServerAddresses = null; 
        internal IPAddressCollection dnsAddresses = null; 

        /* 
        // Consider removing.
        internal SystemIPv4InterfaceProperties(){
        }
        */ 

        internal SystemIPv4InterfaceProperties(FixedInfo fixedInfo, IpAdapterInfo ipAdapterInfo) 
        { 
            index = ipAdapterInfo.index;
            routingEnabled = fixedInfo.EnableRouting; 
            dhcpEnabled = ipAdapterInfo.dhcpEnabled;
            haveWins = ipAdapterInfo.haveWins;
            gatewayAddresses = ipAdapterInfo.gatewayList.ToIPGatewayAddressCollection();
            dhcpAddresses = ipAdapterInfo.dhcpServer.ToIPAddressCollection(); 
            IPAddressCollection primaryWinsServerAddresses = ipAdapterInfo.primaryWinsServer.ToIPAddressCollection();
            IPAddressCollection secondaryWinsServerAddresses = ipAdapterInfo.secondaryWinsServer.ToIPAddressCollection(); 
 
            //concat the winsserver addresses
            winsServerAddresses = new IPAddressCollection(); 

            foreach (IPAddress address in primaryWinsServerAddresses){
                winsServerAddresses.InternalAdd(address);
            } 
            foreach (IPAddress address in secondaryWinsServerAddresses){
                winsServerAddresses.InternalAdd(address); 
            } 

            SystemIPv4InterfaceStatistics s = new SystemIPv4InterfaceStatistics(index); 
            mtu = (uint)s.Mtu;

            if(ComNetOS.IsWin2K){
                GetPerAdapterInfo(ipAdapterInfo.index); 
            }
            else{ 
                dnsAddresses = fixedInfo.DnsAddresses; 
            }
        } 

        internal IPAddressCollection DnsAddresses{
            get { return dnsAddresses; }
        } 

 
        /// <summary>Only valid for Ipv4 Uses WINS for name resolution.</summary> 
        public override bool UsesWins{get {return haveWins;}}
 

        public override bool IsDhcpEnabled{
            get { return dhcpEnabled; }
        } 

        public override bool IsForwardingEnabled{get {return routingEnabled;}}      //proto 
 

 
        /// <summary>Auto configuration of an ipv4 address for a client
        /// on a network where a DHCP server
        /// isn't available.</summary>
        public override bool IsAutomaticPrivateAddressingEnabled{ 
            get{
                return autoConfigEnabled; 
            } 
        } // proto
 
        public override bool IsAutomaticPrivateAddressingActive{
            get{
                return autoConfigActive;
            } 
        }
 
 
        /// <summary>Specifies the Maximum transmission unit in bytes. Uses GetIFEntry.</summary>
        //We cache this to be consistent across all platforms 
        public override int Mtu{
            get {
                return (int) mtu;
            } 
        }
 
        public override int Index{ 
            get {
                return (int) index; 
            }
        }

        /// <summary>IP Address of the default gateway.</summary> 
        internal GatewayIPAddressInformationCollection GetGatewayAddresses(){
            return gatewayAddresses; 
        } 

        /// <summary>IP address of the DHCP sever.</summary> 

        internal IPAddressCollection GetDhcpServerAddresses(){
            return dhcpAddresses;
        } 

        /// <summary>IP addresses of the WINS servers.</summary> 
        internal IPAddressCollection GetWinsServersAddresses(){ 
            return winsServerAddresses;
        } 



        private void GetPerAdapterInfo(uint index) { 

            if (index != 0){ 
                uint size = 0; 
                SafeLocalFree buffer = null;
 
                uint result = UnsafeNetInfoNativeMethods.GetPerAdapterInfo(index,SafeLocalFree.Zero,ref size);
                while (result == IpHelperErrors.ErrorBufferOverflow) {
                    try {
                        //now we allocate the buffer and read the network parameters. 
                        buffer =  SafeLocalFree.LocalAlloc((int)size);
                        result = UnsafeNetInfoNativeMethods.GetPerAdapterInfo(index,buffer,ref size); 
                        if ( result == IpHelperErrors.Success ) { 
                            IpPerAdapterInfo ipPerAdapterInfo  = (IpPerAdapterInfo)Marshal.PtrToStructure(buffer.DangerousGetHandle(),typeof(IpPerAdapterInfo));
                            autoConfigEnabled = ipPerAdapterInfo.autoconfigEnabled; 
                            autoConfigActive = ipPerAdapterInfo.autoconfigActive;

                            //get dnsAddresses
                            dnsAddresses = ipPerAdapterInfo.dnsServerList.ToIPAddressCollection(); 
                        }
                    } 
                    finally { 
                        if(dnsAddresses == null){
                            dnsAddresses = new IPAddressCollection(); 
                        }

                        if (buffer != null)
                            buffer.Close(); 
                    }
                } 
 
                if(dnsAddresses == null){
                    dnsAddresses = new IPAddressCollection(); 
                }

                if (result != IpHelperErrors.Success) {
                    throw new NetworkInformationException((int)result); 
                }
            } 
        } 
    }
} 
