 
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
 
 

    //this is the main addressinformation class that contains the ipaddress 
    //and other properties
    internal class SystemIPAddressInformation:IPAddressInformation{

        IPAddress address; 
        internal bool transient = false;
        internal bool dnsEligible = true; 
 
        /*
        // Consider removing. 
        internal SystemIPAddressInformation(){}
        */

        internal SystemIPAddressInformation(IPAddress address) { 
            this.address = address;
            if (address.AddressFamily == AddressFamily.InterNetwork) { 
                //DngEligible is true except for 169.254.x.x addresses (the APIPA addresses ) 
                dnsEligible = !((address.m_Address & 0x0000FEA9)>0);
            } 

        }

        internal SystemIPAddressInformation(IpAdapterUnicastAddress adapterAddress,IPAddress address) { 
            this.address = address;
            transient = (adapterAddress.flags & AdapterAddressFlags.Transient)>0; 
            dnsEligible = (adapterAddress.flags & AdapterAddressFlags.DnsEligible)>0; 

        } 

        internal SystemIPAddressInformation(IpAdapterAddress adapterAddress,IPAddress address) {
            this.address = address;
            transient = (adapterAddress.flags & AdapterAddressFlags.Transient)>0; 
            dnsEligible = (adapterAddress.flags & AdapterAddressFlags.DnsEligible)>0;
 
        } 

        public override IPAddress Address{get {return address;}} 

        /// <summary>The address is a cluster address and shouldn't be used by most applications.</summary>
        public override bool IsTransient{
            get { 
                return (transient);
            } 
        } 

        /// <summary>This address can be used for DNS.</summary> 
        public override bool IsDnsEligible{
            get {
                return (dnsEligible);
            } 
        }
 
 
        //helper method that marshals the addressinformation into the classes
        internal static IPAddressCollection ToAddressCollection(IntPtr ptr,IPVersion versionSupported) { 

            //we don't know the number of addresses up front, so we create an arraylist
            //to temporarily store them.
 

            IPAddressCollection      addressList = new IPAddressCollection(); 
 
            //if there is no address, just return;
            if (ptr == IntPtr.Zero) 
                return addressList;


            //get the first address 
            IpAdapterAddress addr = (IpAdapterAddress)Marshal.PtrToStructure(ptr,typeof(IpAdapterAddress));
 
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
            addressList.InternalAdd(ep.Address); 

 
            //repeat for all of the addresses 
            while ( addr.next != IntPtr.Zero ) {
                addr = (IpAdapterAddress)Marshal.PtrToStructure(addr.next,typeof(IpAdapterAddress)); 

                //determine the address family used to create the IPAddress
                family = (addr.address.addressLength > 16)?AddressFamily.InterNetworkV6:AddressFamily.InterNetwork;
 

                //only add addresses that are the same type as what is supported by the interface 
                //this fixes a bug in iphelper regarding dns addresses 
                if (((family == AddressFamily.InterNetwork) && ((versionSupported & IPVersion.IPv4) > 0))
                    || ((family == AddressFamily.InterNetworkV6) && ((versionSupported & IPVersion.IPv6) > 0))) { 

                    sockAddress = new SocketAddress(family,(int)addr.address.addressLength);
                    Marshal.Copy(addr.address.address,sockAddress.m_Buffer,0,addr.address.addressLength);
 
                    //use the endpoint to create the ipaddress
                    if (family == AddressFamily.InterNetwork ) 
                      ep = (IPEndPoint)IPEndPoint.Any.Create(sockAddress); 
                    else
                      ep = (IPEndPoint)IPEndPoint.IPv6Any.Create(sockAddress); 

                    addressList.InternalAdd(ep.Address);
                }
            } 

            return addressList; 
        } 

 
        //helper method that marshals the addressinformation into the classes
        internal static IPAddressInformationCollection ToAddressInformationCollection(IntPtr ptr,IPVersion versionSupported) {

            //we don't know the number of addresses up front, so we create an arraylist 
            //to temporarily store them.
 
 
            IPAddressInformationCollection      addressList = new IPAddressInformationCollection();
 
            //if there is no address, just return;
            if (ptr == IntPtr.Zero)
                return addressList;
 

            //get the first address 
            IpAdapterAddress addr = (IpAdapterAddress)Marshal.PtrToStructure(ptr,typeof(IpAdapterAddress)); 

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
            addressList.InternalAdd(new SystemIPAddressInformation(addr,ep.Address)); 
 

            //repeat for all of the addresses 
            while ( addr.next != IntPtr.Zero ) {
                addr = (IpAdapterAddress)Marshal.PtrToStructure(addr.next,typeof(IpAdapterAddress));

                //determine the address family used to create the IPAddress 
                family = (addr.address.addressLength > 16)?AddressFamily.InterNetworkV6:AddressFamily.InterNetwork;
 
 
                //only add addresses that are the same type as what is supported by the interface
                //this fixes a bug in iphelper regarding dns addresses 
                if (((family == AddressFamily.InterNetwork) && ((versionSupported & IPVersion.IPv4) > 0))
                    || ((family == AddressFamily.InterNetworkV6) && ((versionSupported & IPVersion.IPv6) > 0))) {

                    sockAddress = new SocketAddress(family,(int)addr.address.addressLength); 
                    Marshal.Copy(addr.address.address,sockAddress.m_Buffer,0,addr.address.addressLength);
 
                    //use the endpoint to create the ipaddress 
                    if (family == AddressFamily.InterNetwork )
                      ep = (IPEndPoint)IPEndPoint.Any.Create(sockAddress); 
                    else
                      ep = (IPEndPoint)IPEndPoint.IPv6Any.Create(sockAddress);

                    addressList.InternalAdd(new SystemIPAddressInformation(addr,ep.Address)); 
                }
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
 
 

    //this is the main addressinformation class that contains the ipaddress 
    //and other properties
    internal class SystemIPAddressInformation:IPAddressInformation{

        IPAddress address; 
        internal bool transient = false;
        internal bool dnsEligible = true; 
 
        /*
        // Consider removing. 
        internal SystemIPAddressInformation(){}
        */

        internal SystemIPAddressInformation(IPAddress address) { 
            this.address = address;
            if (address.AddressFamily == AddressFamily.InterNetwork) { 
                //DngEligible is true except for 169.254.x.x addresses (the APIPA addresses ) 
                dnsEligible = !((address.m_Address & 0x0000FEA9)>0);
            } 

        }

        internal SystemIPAddressInformation(IpAdapterUnicastAddress adapterAddress,IPAddress address) { 
            this.address = address;
            transient = (adapterAddress.flags & AdapterAddressFlags.Transient)>0; 
            dnsEligible = (adapterAddress.flags & AdapterAddressFlags.DnsEligible)>0; 

        } 

        internal SystemIPAddressInformation(IpAdapterAddress adapterAddress,IPAddress address) {
            this.address = address;
            transient = (adapterAddress.flags & AdapterAddressFlags.Transient)>0; 
            dnsEligible = (adapterAddress.flags & AdapterAddressFlags.DnsEligible)>0;
 
        } 

        public override IPAddress Address{get {return address;}} 

        /// <summary>The address is a cluster address and shouldn't be used by most applications.</summary>
        public override bool IsTransient{
            get { 
                return (transient);
            } 
        } 

        /// <summary>This address can be used for DNS.</summary> 
        public override bool IsDnsEligible{
            get {
                return (dnsEligible);
            } 
        }
 
 
        //helper method that marshals the addressinformation into the classes
        internal static IPAddressCollection ToAddressCollection(IntPtr ptr,IPVersion versionSupported) { 

            //we don't know the number of addresses up front, so we create an arraylist
            //to temporarily store them.
 

            IPAddressCollection      addressList = new IPAddressCollection(); 
 
            //if there is no address, just return;
            if (ptr == IntPtr.Zero) 
                return addressList;


            //get the first address 
            IpAdapterAddress addr = (IpAdapterAddress)Marshal.PtrToStructure(ptr,typeof(IpAdapterAddress));
 
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
            addressList.InternalAdd(ep.Address); 

 
            //repeat for all of the addresses 
            while ( addr.next != IntPtr.Zero ) {
                addr = (IpAdapterAddress)Marshal.PtrToStructure(addr.next,typeof(IpAdapterAddress)); 

                //determine the address family used to create the IPAddress
                family = (addr.address.addressLength > 16)?AddressFamily.InterNetworkV6:AddressFamily.InterNetwork;
 

                //only add addresses that are the same type as what is supported by the interface 
                //this fixes a bug in iphelper regarding dns addresses 
                if (((family == AddressFamily.InterNetwork) && ((versionSupported & IPVersion.IPv4) > 0))
                    || ((family == AddressFamily.InterNetworkV6) && ((versionSupported & IPVersion.IPv6) > 0))) { 

                    sockAddress = new SocketAddress(family,(int)addr.address.addressLength);
                    Marshal.Copy(addr.address.address,sockAddress.m_Buffer,0,addr.address.addressLength);
 
                    //use the endpoint to create the ipaddress
                    if (family == AddressFamily.InterNetwork ) 
                      ep = (IPEndPoint)IPEndPoint.Any.Create(sockAddress); 
                    else
                      ep = (IPEndPoint)IPEndPoint.IPv6Any.Create(sockAddress); 

                    addressList.InternalAdd(ep.Address);
                }
            } 

            return addressList; 
        } 

 
        //helper method that marshals the addressinformation into the classes
        internal static IPAddressInformationCollection ToAddressInformationCollection(IntPtr ptr,IPVersion versionSupported) {

            //we don't know the number of addresses up front, so we create an arraylist 
            //to temporarily store them.
 
 
            IPAddressInformationCollection      addressList = new IPAddressInformationCollection();
 
            //if there is no address, just return;
            if (ptr == IntPtr.Zero)
                return addressList;
 

            //get the first address 
            IpAdapterAddress addr = (IpAdapterAddress)Marshal.PtrToStructure(ptr,typeof(IpAdapterAddress)); 

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
            addressList.InternalAdd(new SystemIPAddressInformation(addr,ep.Address)); 
 

            //repeat for all of the addresses 
            while ( addr.next != IntPtr.Zero ) {
                addr = (IpAdapterAddress)Marshal.PtrToStructure(addr.next,typeof(IpAdapterAddress));

                //determine the address family used to create the IPAddress 
                family = (addr.address.addressLength > 16)?AddressFamily.InterNetworkV6:AddressFamily.InterNetwork;
 
 
                //only add addresses that are the same type as what is supported by the interface
                //this fixes a bug in iphelper regarding dns addresses 
                if (((family == AddressFamily.InterNetwork) && ((versionSupported & IPVersion.IPv4) > 0))
                    || ((family == AddressFamily.InterNetworkV6) && ((versionSupported & IPVersion.IPv6) > 0))) {

                    sockAddress = new SocketAddress(family,(int)addr.address.addressLength); 
                    Marshal.Copy(addr.address.address,sockAddress.m_Buffer,0,addr.address.addressLength);
 
                    //use the endpoint to create the ipaddress 
                    if (family == AddressFamily.InterNetwork )
                      ep = (IPEndPoint)IPEndPoint.Any.Create(sockAddress); 
                    else
                      ep = (IPEndPoint)IPEndPoint.IPv6Any.Create(sockAddress);

                    addressList.InternalAdd(new SystemIPAddressInformation(addr,ep.Address)); 
                }
            } 
 
            return addressList;
        } 
    }
}

