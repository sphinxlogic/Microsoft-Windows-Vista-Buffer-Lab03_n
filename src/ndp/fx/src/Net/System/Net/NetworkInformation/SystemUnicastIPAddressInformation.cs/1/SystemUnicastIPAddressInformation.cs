 
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
 
 

    /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPUnicastAddressInformation"]/*' /> 
    /// <summary>Specifies the unicast addresses for an interface.</summary>
    /// <exception cref='platform not supported'>OS &lt; XP</exception>
    /// <platnote platform='OS &gt;= XP'>
    /// </platnote> 
    internal class SystemUnicastIPAddressInformation:UnicastIPAddressInformation {
        private IpAdapterUnicastAddress adapterAddress; 
        private long dhcpLeaseLifetime; 
        private SystemIPAddressInformation innerInfo;
        internal IPAddress ipv4Mask; 

        private SystemUnicastIPAddressInformation() {
        }
 

        internal SystemUnicastIPAddressInformation(IpAdapterInfo ipAdapterInfo, IPExtendedAddress address){ 
            innerInfo = new SystemIPAddressInformation(address.address); 
            DateTime tempdate = new DateTime(1970,1,1);
            tempdate = tempdate.AddSeconds(ipAdapterInfo.leaseExpires); 
            dhcpLeaseLifetime = (long)((tempdate - DateTime.UtcNow).TotalSeconds);
            ipv4Mask = address.mask;
        }
 
        internal SystemUnicastIPAddressInformation(IpAdapterUnicastAddress adapterAddress, IPAddress ipAddress){
            innerInfo = new SystemIPAddressInformation(adapterAddress,ipAddress); 
            this.adapterAddress = adapterAddress; 
            dhcpLeaseLifetime = adapterAddress.leaseLifetime;
        } 

       /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPAddressInformation.Address"]/*' />
        public override IPAddress Address{get {return innerInfo.Address;}}
 

        public override IPAddress IPv4Mask{ 
            get { 
                if(Address.AddressFamily != AddressFamily.InterNetwork){
                    return new IPAddress(0); 
                }

                return ipv4Mask;
            } 
        }
 
        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPAddressInformation.Transient"]/*' /> 
        /// <summary>The address is a cluster address and shouldn't be used by most applications.</summary>
        public override bool IsTransient{ 
            get {
                return (innerInfo.IsTransient);
            }
        } 

        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPAddressInformation.DnsEligible"]/*' /> 
        /// <summary>This address can be used for DNS.</summary> 
        public override bool IsDnsEligible{
            get { 
                return (innerInfo.IsDnsEligible);
            }
        }
 

        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPUnicastAddressInformation.PrefixOrigin"]/*' /> 
        public override PrefixOrigin PrefixOrigin{ 
            get {
                if (! ComNetOS.IsPostWin2K) 
                    throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired));

                return adapterAddress.prefixOrigin;
            } 
        }
 
        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPUnicastAddressInformation.SuffixOrigin"]/*' /> 
        public override SuffixOrigin SuffixOrigin{
            get { 

                if (! ComNetOS.IsPostWin2K)
                    throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired));
 
                return adapterAddress.suffixOrigin;
            } 
        } 
        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPUnicastAddressInformation.DuplicateAddressDetectionState"]/*' />
        /// <summary>IPv6 only.  Specifies the duplicate address detection state. Only supported 
        /// for IPv6. If called on an IPv4 address, will throw a "not supported" exception.</summary>
        public override DuplicateAddressDetectionState DuplicateAddressDetectionState{
            get {
                if (! ComNetOS.IsPostWin2K) 
                    throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired));
 
                return adapterAddress.dadState; 
            }
        } 


        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPUnicastAddressInformation.ValidLifetime"]/*' />
        /// <summary>Specifies the valid lifetime of the address in seconds.</summary> 
        public override long AddressValidLifetime{
            get { 
                if (! ComNetOS.IsPostWin2K) 
                    throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired));
 
                return adapterAddress.validLifetime;
                }
            }
        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPUnicastAddressInformation.PreferredLifetime"]/*' /> 
        /// <summary>Specifies the prefered lifetime of the address in seconds.</summary>
 
        public override long AddressPreferredLifetime{ 
            get {
                if (! ComNetOS.IsPostWin2K) 
                    throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired));

                return adapterAddress.preferredLifetime;
                } 
            }
        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPUnicastAddressInformation.PreferredLifetime"]/*' /> 
 
        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPUnicastAddressInformation.DhcpLeaseLifetime"]/*' />
        /// <summary>Specifies the prefered lifetime of the address in seconds.</summary> 
        public override long DhcpLeaseLifetime{
            get {
                return dhcpLeaseLifetime;
                } 
            }
 
 
        //helper method that marshals the addressinformation into the classes
        internal static UnicastIPAddressInformationCollection ToAddressInformationCollection(IntPtr ptr) { 

            //we don't know the number of addresses up front, so we create an arraylist
            //to temporarily store them.
            UnicastIPAddressInformationCollection addressList = new UnicastIPAddressInformationCollection(); 

            //if there is no address, just return; 
            if (ptr == IntPtr.Zero) 
                return addressList;
 
            //get the first address
            IpAdapterUnicastAddress addr = (IpAdapterUnicastAddress)Marshal.PtrToStructure(ptr,typeof(IpAdapterUnicastAddress));

 
            //determine the address family used to create the IPAddress
            AddressFamily family = (addr.address.addressLength > 16)?AddressFamily.InterNetworkV6:AddressFamily.InterNetwork; 
            SocketAddress sockAddress = new SocketAddress(family,(int)addr.address.addressLength); 
            Marshal.Copy(addr.address.address,sockAddress.m_Buffer,0,addr.address.addressLength);
 

            //unfortunately, the only way to currently create an ipaddress is through IPEndPoint
            IPEndPoint ep;
 
            if (family == AddressFamily.InterNetwork )
              ep = (IPEndPoint)IPEndPoint.Any.Create(sockAddress); 
            else 
              ep = (IPEndPoint)IPEndPoint.IPv6Any.Create(sockAddress);
 

            //add the ipaddress to the arraylist
            addressList.InternalAdd(new SystemUnicastIPAddressInformation(addr,ep.Address));
 
            //repeat for all of the addresses
            while ( addr.next != IntPtr.Zero ) { 
                addr = (IpAdapterUnicastAddress)Marshal.PtrToStructure(addr.next,typeof(IpAdapterUnicastAddress)); 

                //determine the address family used to create the IPAddress 
                family = (addr.address.addressLength > 16)?AddressFamily.InterNetworkV6:AddressFamily.InterNetwork;
                sockAddress = new SocketAddress(family,(int)addr.address.addressLength);
                Marshal.Copy(addr.address.address,sockAddress.m_Buffer,0,addr.address.addressLength);
 
                //use the endpoint to create the ipaddress
                if (family == AddressFamily.InterNetwork ) 
                  ep = (IPEndPoint)IPEndPoint.Any.Create(sockAddress); 
                else
                  ep = (IPEndPoint)IPEndPoint.IPv6Any.Create(sockAddress); 

                addressList.InternalAdd(new SystemUnicastIPAddressInformation(addr,ep.Address));
            }
 
            return addressList;
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
 
 

    /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPUnicastAddressInformation"]/*' /> 
    /// <summary>Specifies the unicast addresses for an interface.</summary>
    /// <exception cref='platform not supported'>OS &lt; XP</exception>
    /// <platnote platform='OS &gt;= XP'>
    /// </platnote> 
    internal class SystemUnicastIPAddressInformation:UnicastIPAddressInformation {
        private IpAdapterUnicastAddress adapterAddress; 
        private long dhcpLeaseLifetime; 
        private SystemIPAddressInformation innerInfo;
        internal IPAddress ipv4Mask; 

        private SystemUnicastIPAddressInformation() {
        }
 

        internal SystemUnicastIPAddressInformation(IpAdapterInfo ipAdapterInfo, IPExtendedAddress address){ 
            innerInfo = new SystemIPAddressInformation(address.address); 
            DateTime tempdate = new DateTime(1970,1,1);
            tempdate = tempdate.AddSeconds(ipAdapterInfo.leaseExpires); 
            dhcpLeaseLifetime = (long)((tempdate - DateTime.UtcNow).TotalSeconds);
            ipv4Mask = address.mask;
        }
 
        internal SystemUnicastIPAddressInformation(IpAdapterUnicastAddress adapterAddress, IPAddress ipAddress){
            innerInfo = new SystemIPAddressInformation(adapterAddress,ipAddress); 
            this.adapterAddress = adapterAddress; 
            dhcpLeaseLifetime = adapterAddress.leaseLifetime;
        } 

       /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPAddressInformation.Address"]/*' />
        public override IPAddress Address{get {return innerInfo.Address;}}
 

        public override IPAddress IPv4Mask{ 
            get { 
                if(Address.AddressFamily != AddressFamily.InterNetwork){
                    return new IPAddress(0); 
                }

                return ipv4Mask;
            } 
        }
 
        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPAddressInformation.Transient"]/*' /> 
        /// <summary>The address is a cluster address and shouldn't be used by most applications.</summary>
        public override bool IsTransient{ 
            get {
                return (innerInfo.IsTransient);
            }
        } 

        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPAddressInformation.DnsEligible"]/*' /> 
        /// <summary>This address can be used for DNS.</summary> 
        public override bool IsDnsEligible{
            get { 
                return (innerInfo.IsDnsEligible);
            }
        }
 

        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPUnicastAddressInformation.PrefixOrigin"]/*' /> 
        public override PrefixOrigin PrefixOrigin{ 
            get {
                if (! ComNetOS.IsPostWin2K) 
                    throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired));

                return adapterAddress.prefixOrigin;
            } 
        }
 
        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPUnicastAddressInformation.SuffixOrigin"]/*' /> 
        public override SuffixOrigin SuffixOrigin{
            get { 

                if (! ComNetOS.IsPostWin2K)
                    throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired));
 
                return adapterAddress.suffixOrigin;
            } 
        } 
        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPUnicastAddressInformation.DuplicateAddressDetectionState"]/*' />
        /// <summary>IPv6 only.  Specifies the duplicate address detection state. Only supported 
        /// for IPv6. If called on an IPv4 address, will throw a "not supported" exception.</summary>
        public override DuplicateAddressDetectionState DuplicateAddressDetectionState{
            get {
                if (! ComNetOS.IsPostWin2K) 
                    throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired));
 
                return adapterAddress.dadState; 
            }
        } 


        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPUnicastAddressInformation.ValidLifetime"]/*' />
        /// <summary>Specifies the valid lifetime of the address in seconds.</summary> 
        public override long AddressValidLifetime{
            get { 
                if (! ComNetOS.IsPostWin2K) 
                    throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired));
 
                return adapterAddress.validLifetime;
                }
            }
        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPUnicastAddressInformation.PreferredLifetime"]/*' /> 
        /// <summary>Specifies the prefered lifetime of the address in seconds.</summary>
 
        public override long AddressPreferredLifetime{ 
            get {
                if (! ComNetOS.IsPostWin2K) 
                    throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired));

                return adapterAddress.preferredLifetime;
                } 
            }
        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPUnicastAddressInformation.PreferredLifetime"]/*' /> 
 
        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="IPUnicastAddressInformation.DhcpLeaseLifetime"]/*' />
        /// <summary>Specifies the prefered lifetime of the address in seconds.</summary> 
        public override long DhcpLeaseLifetime{
            get {
                return dhcpLeaseLifetime;
                } 
            }
 
 
        //helper method that marshals the addressinformation into the classes
        internal static UnicastIPAddressInformationCollection ToAddressInformationCollection(IntPtr ptr) { 

            //we don't know the number of addresses up front, so we create an arraylist
            //to temporarily store them.
            UnicastIPAddressInformationCollection addressList = new UnicastIPAddressInformationCollection(); 

            //if there is no address, just return; 
            if (ptr == IntPtr.Zero) 
                return addressList;
 
            //get the first address
            IpAdapterUnicastAddress addr = (IpAdapterUnicastAddress)Marshal.PtrToStructure(ptr,typeof(IpAdapterUnicastAddress));

 
            //determine the address family used to create the IPAddress
            AddressFamily family = (addr.address.addressLength > 16)?AddressFamily.InterNetworkV6:AddressFamily.InterNetwork; 
            SocketAddress sockAddress = new SocketAddress(family,(int)addr.address.addressLength); 
            Marshal.Copy(addr.address.address,sockAddress.m_Buffer,0,addr.address.addressLength);
 

            //unfortunately, the only way to currently create an ipaddress is through IPEndPoint
            IPEndPoint ep;
 
            if (family == AddressFamily.InterNetwork )
              ep = (IPEndPoint)IPEndPoint.Any.Create(sockAddress); 
            else 
              ep = (IPEndPoint)IPEndPoint.IPv6Any.Create(sockAddress);
 

            //add the ipaddress to the arraylist
            addressList.InternalAdd(new SystemUnicastIPAddressInformation(addr,ep.Address));
 
            //repeat for all of the addresses
            while ( addr.next != IntPtr.Zero ) { 
                addr = (IpAdapterUnicastAddress)Marshal.PtrToStructure(addr.next,typeof(IpAdapterUnicastAddress)); 

                //determine the address family used to create the IPAddress 
                family = (addr.address.addressLength > 16)?AddressFamily.InterNetworkV6:AddressFamily.InterNetwork;
                sockAddress = new SocketAddress(family,(int)addr.address.addressLength);
                Marshal.Copy(addr.address.address,sockAddress.m_Buffer,0,addr.address.addressLength);
 
                //use the endpoint to create the ipaddress
                if (family == AddressFamily.InterNetwork ) 
                  ep = (IPEndPoint)IPEndPoint.Any.Create(sockAddress); 
                else
                  ep = (IPEndPoint)IPEndPoint.IPv6Any.Create(sockAddress); 

                addressList.InternalAdd(new SystemUnicastIPAddressInformation(addr,ep.Address));
            }
 
            return addressList;
        } 
    } 
}
