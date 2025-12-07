 
    /// <summary><para>
    ///    Provides support for ip configuation information and statistics.
    ///</para></summary>
    /// 
namespace System.Net.NetworkInformation {
    using System.Threading; 
    using System.Net; 
    using System.Net.Sockets;
    using System; 
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.ComponentModel;
    using System.Security.Permissions; 
    using Microsoft.Win32;
 
 
    [Flags]
    internal enum IPVersion { 
        None = 0,
        IPv4 = 1,
        IPv6 = 2
        } 

 
    internal class SystemNetworkInterface:NetworkInterface { 

        //common properties 
        string name;
        string id;
        string description;
        byte[] physicalAddress; 
        uint addressLength;
        NetworkInterfaceType type; 
        OperationalStatus operStatus; 
        long speed;
 

        //Unfortunately, any interface can
        //have two completely different valid indexes for ipv4 and ipv6
        internal uint index = 0; 
        internal uint ipv6Index = 0;
 
        AdapterFlags adapterFlags; 

        //ipv4 only 
        //
        SystemIPInterfaceProperties interfaceProperties = null;

 
        private SystemNetworkInterface(){}
 
 
        //methods
        /// <summary> 
        /// Gets the network interfaces local to the machine.
        /// If the machine is >= XP, we call the native GetAdaptersAddresses api to prepopulate all of the interface
        /// instances and most of their associated information.  Otherwise, GetAdaptersInfo is called.
        /// </summary> 

        //this returns both ipv4 and ipv6 interfaces 
        internal static NetworkInterface[] GetNetworkInterfaces(){ 
            return GetNetworkInterfaces(0);
        } 


        internal static int InternalLoopbackInterfaceIndex{
            get{ 
                int index;
                int error = (int)UnsafeNetInfoNativeMethods.GetBestInterface(0x100007F,out index); 
                if (error != 0) { 
                    throw new NetworkInformationException(error);
                } 

                return index;
            }
        } 

        internal static bool InternalGetIsNetworkAvailable(){ 
 
            if(ComNetOS.IsWinNt){
                NetworkInterface[] networkInterfaces = GetNetworkInterfaces(); 
                foreach (NetworkInterface netInterface in networkInterfaces) {
                    if(netInterface.OperationalStatus == OperationalStatus.Up && netInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                       && netInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback){
                        return true; 
                    }
                } 
            } 
            else{
                uint flags=0; 
                return UnsafeWinINetNativeMethods.InternetGetConnectedState (ref flags, 0);
            }
            return false;
 
        }
 
 
        //address family specific
        private static NetworkInterface[] GetNetworkInterfaces(AddressFamily family){ 


            IpHelperErrors.CheckFamilyUnspecified(family);
 
            //uses GetAdapterAddresses if os >= winxp
            if (ComNetOS.IsPostWin2K) { 
                return PostWin2KGetNetworkInterfaces(family); 
            }
 
            // os < winxp
            //make sure we are only looking for ipv4

            FixedInfo fixedInfo = SystemIPGlobalProperties.GetFixedInfo(); 

            if (family != 0 && family!= AddressFamily.InterNetwork) 
                throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired)); 

 
            SafeLocalFree buffer = null;
            uint    size = 0;
            IpAdapterInfo info;
            ArrayList interfaceList = new ArrayList(); 

            //figure out the size of the buffer we need 
            uint result = UnsafeNetInfoNativeMethods.GetAdaptersInfo(SafeLocalFree.Zero,ref size); 

            while (result == IpHelperErrors.ErrorBufferOverflow) { 
                try {
                    //now get the adapter info and populate the network interface objects.
                    buffer =  SafeLocalFree.LocalAlloc((int)size);
                    result = UnsafeNetInfoNativeMethods.GetAdaptersInfo(buffer,ref size); 

                    if ( result == IpHelperErrors.Success ) { 
                        info = (IpAdapterInfo)Marshal.PtrToStructure(buffer.DangerousGetHandle(),typeof(IpAdapterInfo)); 

                        interfaceList.Add( new SystemNetworkInterface(fixedInfo,info)); 
                        while(info.Next != IntPtr.Zero) {
                            info = (IpAdapterInfo)Marshal.PtrToStructure(info.Next,typeof(IpAdapterInfo));
                            interfaceList.Add( new SystemNetworkInterface(fixedInfo,info));
                        } 
                    }
                } 
                finally { 
                    if (buffer != null)
                        buffer.Close(); 
                }
            }

            // if we don't have any interfaces detected, return empty. 
            if (result == IpHelperErrors.ErrorNoData)
                return new SystemNetworkInterface[0]; 
 
            //otherwise we throw on an error
            if (result != IpHelperErrors.Success) { 
                throw new NetworkInformationException((int)result);
            }

            // create the array of interfaces to return 
            SystemNetworkInterface[] networkInterfaces = new SystemNetworkInterface[interfaceList.Count];
            for ( int i = 0; i < interfaceList.Count; ++i ) { 
                networkInterfaces[i] = (SystemNetworkInterface)interfaceList[i]; 
            }
 
            return networkInterfaces;
        }

 
        private static SystemNetworkInterface[] GetAdaptersAddresses(AddressFamily family, FixedInfo fixedInfo) {
 
            uint size = 0; 
            SafeLocalFree buffer = null;
            ArrayList interfaceList =  new ArrayList(); 
            SystemNetworkInterface[] networkInterfaces = null;

            //get each adapter's info
            //get the size of the buffer required 
            uint result = UnsafeNetInfoNativeMethods.GetAdaptersAddresses(family,0,IntPtr.Zero,SafeLocalFree.Zero,ref size);
            while (result == IpHelperErrors.ErrorBufferOverflow) { 
                try { 
                    //allocate the buffer and get the adapter info
                    buffer =  SafeLocalFree.LocalAlloc((int)size); 
                    result = UnsafeNetInfoNativeMethods.GetAdaptersAddresses(family,0,IntPtr.Zero,buffer,ref size);

                    //if succeeded, we're going to add each new interface
                    if ( result == IpHelperErrors.Success) { 

                        //get the first adapter 
                        // we don't know the number of interfaces until we've 
                        // actually marshalled all of them
                        IpAdapterAddresses adapterAddresses = (IpAdapterAddresses)Marshal.PtrToStructure(buffer.DangerousGetHandle(),typeof(IpAdapterAddresses)); 
                        interfaceList.Add(new SystemNetworkInterface(fixedInfo,adapterAddresses));

                        //get the rest
                        while(adapterAddresses.next != IntPtr.Zero) { 
                            adapterAddresses = (IpAdapterAddresses)Marshal.PtrToStructure(adapterAddresses.next,typeof(IpAdapterAddresses));
                            interfaceList.Add(new SystemNetworkInterface(fixedInfo,adapterAddresses)); 
                        } 
                    }
                } 
                finally {
                    if (buffer != null)
                        buffer.Close();
                    buffer = null; 
                }
            } 
 
            // if we don't have any interfaces detected, return empty.
            if (result == IpHelperErrors.ErrorNoData || result == IpHelperErrors.ErrorInvalidParameter) 
                return new SystemNetworkInterface[0];

            //otherwise we throw on an error
            if (result != IpHelperErrors.Success) { 
                throw new NetworkInformationException((int)result);
            } 
 

            // create the array of interfaces to return 
            networkInterfaces = new SystemNetworkInterface[interfaceList.Count];
            for ( int i = 0; i < interfaceList.Count; ++i ) {
                networkInterfaces[i] = (SystemNetworkInterface)interfaceList[i];
            } 
            return networkInterfaces;
        } 
 
        private static SystemNetworkInterface[] PostWin2KGetNetworkInterfaces(AddressFamily family) {
 
            FixedInfo fixedInfo = SystemIPGlobalProperties.GetFixedInfo();

            SystemNetworkInterface[] networkInterfaces = null;
 
            while (true) {
                try { 
                    networkInterfaces = GetAdaptersAddresses(family, fixedInfo); 
                    break;
                } catch (NetworkInformationException exception) { 
                    if (exception.ErrorCode != IpHelperErrors.ErrorInvalidFunction)
                        throw;
                }
            } 

            if (!Socket.SupportsIPv4) // Only if IPv4 is supported continue below 
            { 
                return networkInterfaces;
            } 

            //GetAdapterAddresses doesn't currently include all of the possible
            //ipv4 information, so we need to call GetAdaptersInfo to populate
            // the rest of the fields. 

            uint size = 0; 
            uint result = 0; 
            SafeLocalFree buffer = null;
 
            if (family == 0 || family == AddressFamily.InterNetwork) {

                //figure out the size of the buffer we need
                result = UnsafeNetInfoNativeMethods.GetAdaptersInfo(SafeLocalFree.Zero,ref size); 

                int failedUpdateCount = 0; 
                //now use GetAdaptersInfo and update the network interface objects w/ the information 
                // missing in GetAdaptersAddresses.
                while (result == IpHelperErrors.ErrorBufferOverflow) { 
                    try {
                        buffer =  SafeLocalFree.LocalAlloc((int)size);
                        result = UnsafeNetInfoNativeMethods.GetAdaptersInfo(buffer,ref size);
 
                        //basically, we get the additional adapterinfo for each ipv4 adapter and
                        //call update on the interface 
                        if ( result == IpHelperErrors.Success ){ 
                            IntPtr myPtr = buffer.DangerousGetHandle();
                            bool success = false; 

                            while (myPtr != IntPtr.Zero)
                            {
                                IpAdapterInfo info = (IpAdapterInfo)Marshal.PtrToStructure(myPtr, typeof(IpAdapterInfo)); 

                                for (int i = 0; i < networkInterfaces.Length; i++) 
                                { 
                                    if (networkInterfaces[i] != null)
                                    { 
                                        if (info.index == networkInterfaces[i].index)
                                        {
                                            success = networkInterfaces[i].interfaceProperties.Update(fixedInfo, info);
                                            if (!success) 
                                            {
                                                networkInterfaces[i] = null; 
                                                failedUpdateCount++; 
                                            }
                                            break; 
                                        }
                                    }
                                }
                                myPtr = info.Next; 
                            }
                        } 
                    } 
                    finally {
                        if (buffer != null) 
                            buffer.Close();
                    }
                }
                if (failedUpdateCount != 0) { 
                    SystemNetworkInterface[] newNetworkInterfaces =
                        new SystemNetworkInterface[networkInterfaces.Length-failedUpdateCount]; 
                    int newArrayIndex = 0; 
                    for (int i=0; i < networkInterfaces.Length; i++) {
                        if (networkInterfaces[i] != null) 
                            newNetworkInterfaces[newArrayIndex++] = networkInterfaces[i];
                    }
                    networkInterfaces = newNetworkInterfaces;
                } 
            }
            // if we don't have any ipv4 interfaces detected, just continue 
            if (result != IpHelperErrors.Success && 
                result != IpHelperErrors.ErrorNoData)
            { 
                throw new NetworkInformationException((int)result);
            }
            return networkInterfaces;
        } 

 
        //This constructor is used only in the OS>=XP case 
        //and uses the GetAdapterAddresses api
        internal SystemNetworkInterface(FixedInfo fixedInfo, IpAdapterAddresses ipAdapterAddresses) { 

            //store the common api information
            id = ipAdapterAddresses.AdapterName;
            name = ipAdapterAddresses.friendlyName; 
            description = ipAdapterAddresses.description;
            index = ipAdapterAddresses.index; 
 
            physicalAddress = ipAdapterAddresses.address;
            addressLength = ipAdapterAddresses.addressLength; 

            type = ipAdapterAddresses.type;
            operStatus = ipAdapterAddresses.operStatus;
 
            //api specific info
            ipv6Index = ipAdapterAddresses.ipv6Index; 
 
            adapterFlags = ipAdapterAddresses.flags;
            interfaceProperties = new SystemIPInterfaceProperties(fixedInfo,ipAdapterAddresses); 
        }


        //This constructor is used only in the OS<XP case 
        //and uses the GetAdapterInfo api
        internal SystemNetworkInterface(FixedInfo fixedInfo, IpAdapterInfo ipAdapterInfo) { 
 
            //store the common api information
            id = ipAdapterInfo.adapterName; 
            name = String.Empty;
            description = ipAdapterInfo.description;
            index = ipAdapterInfo.index;
 
            physicalAddress = ipAdapterInfo.address;
            addressLength = ipAdapterInfo.addressLength; 
 
            //try to discover the adapter name
            if(ComNetOS.IsWin2K && ! ComNetOS.IsPostWin2K){ 
                name = ReadAdapterName(id);
            }

            //otherwise, just use the name returned in description. 
            if(name.Length == 0){
                name = description; 
            } 

 
            SystemIPv4InterfaceStatistics s = new SystemIPv4InterfaceStatistics(index);
            operStatus = s.OperationalStatus;

 
            //we need to convert the old adapterinfo types
            //to the new ones used in AdapterAddresses 
            switch (ipAdapterInfo.type){ 
                case OldInterfaceType.Ethernet:
                    type = NetworkInterfaceType.Ethernet; 
                    break;
                case OldInterfaceType.Fddi:
                    type = NetworkInterfaceType.Fddi;
                    break; 
                case OldInterfaceType.Loopback:
                    type = NetworkInterfaceType.Loopback; 
                    break; 
                case OldInterfaceType.Ppp:
                    type = NetworkInterfaceType.Ppp; 
                    break;
                case OldInterfaceType.Slip:
                    type = NetworkInterfaceType.Slip;
                    break; 
                case OldInterfaceType.TokenRing:
                    type = NetworkInterfaceType.TokenRing; 
                    break; 
                default:
                    type = NetworkInterfaceType.Unknown; 
                    break;
            }
            interfaceProperties = new SystemIPInterfaceProperties(fixedInfo,ipAdapterInfo);
        } 

 
 

        /// Basic Properties 

        public override string Id{get {return id;}}
        public override string Name{get {return name;}}
        public override string Description{get {return description;}} 

 
        public override PhysicalAddress GetPhysicalAddress(){ 
            byte[] newAddr = new byte[addressLength];
            Array.Copy(physicalAddress,newAddr,addressLength); 
            return new PhysicalAddress(newAddr);
        }
        public override NetworkInterfaceType NetworkInterfaceType{get {return type;}}
 
        // Stats specific for Ipv4.  Ipv6 will be added in Longhorn
        public override IPInterfaceProperties GetIPProperties(){ 
            return interfaceProperties; 
        }
 


        // Stats specific for Ipv4.  Ipv6 will be added in Longhorn
        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="NetworkInterface.GetInterfaceStatistics"]/*' /> 
        public override IPv4InterfaceStatistics GetIPv4Statistics(){
            return new SystemIPv4InterfaceStatistics(index); 
        } 

 
        public override bool Supports(NetworkInterfaceComponent networkInterfaceComponent){
            if (networkInterfaceComponent ==  NetworkInterfaceComponent.IPv6 && ipv6Index >0){
                return true;
            } 
            if (networkInterfaceComponent ==  NetworkInterfaceComponent.IPv4 && index >0){
                return true; 
            } 
            return false;
        } 

        /// <summary>Is the interface administratively enabled.</summary>
        //public override bool AdministrationEnabled{get {return false;}}           //interface specific property
 

        //We cache this to be consistent across all platforms 
        public override OperationalStatus OperationalStatus{ 
            get {
                return operStatus; 
            }
        }

 
        public override long Speed{
            get { 
 
                if (speed == 0) {
                    SystemIPv4InterfaceStatistics s = new SystemIPv4InterfaceStatistics(index); 
                    speed = s.Speed;
                }
                return speed;
            } 
        }
 
        /// <exception cref='platform not supported'>OS &lt; XP</exception> 
        /// <platnote platform='OS &gt;= XP'>
        /// </platnote> 
        public override bool IsReceiveOnly {
            get {
                if (! ComNetOS.IsPostWin2K)
                    throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired)); 
                return ((adapterFlags & AdapterFlags.ReceiveOnly) > 0);
            } 
        } //throw if <winxp 
        /// <summary>The interface doesn't allow multicast.</summary>
        /// <exception cref='platform not supported'>OS &lt; XP</exception> 
        /// <platnote platform='OS &gt;= XP'>
        /// </platnote>
        public override bool SupportsMulticast {
            get { 
                if (! ComNetOS.IsPostWin2K)
                    throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired)); 
                return ((adapterFlags & AdapterFlags.NoMulticast) == 0); 
            }
        } 

        [RegistryPermission(SecurityAction.Assert, Read="HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Network\\{4D36E972-E325-11CE-BFC1-08002BE10318}")]
        private string ReadAdapterName(string id) {
            RegistryKey key = null; 
            string name = String.Empty;
            try { 
                string subKey = "SYSTEM\\CurrentControlSet\\Control\\Network\\{4D36E972-E325-11CE-BFC1-08002BE10318}\\"+id+"\\Connection"; 
                key = Registry.LocalMachine.OpenSubKey(subKey);
                if (null != key) { 
                    name = (string) key.GetValue("Name");
                    if (name == null ) {
                       name = String.Empty;
                    } 
                }
            } 
            finally{ 
                if (null != key){
                    key.Close(); 
                }
            }
            return name;
        } 
    }
} 
 

 
 
    /// <summary><para>
    ///    Provides support for ip configuation information and statistics.
    ///</para></summary>
    /// 
namespace System.Net.NetworkInformation {
    using System.Threading; 
    using System.Net; 
    using System.Net.Sockets;
    using System; 
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.ComponentModel;
    using System.Security.Permissions; 
    using Microsoft.Win32;
 
 
    [Flags]
    internal enum IPVersion { 
        None = 0,
        IPv4 = 1,
        IPv6 = 2
        } 

 
    internal class SystemNetworkInterface:NetworkInterface { 

        //common properties 
        string name;
        string id;
        string description;
        byte[] physicalAddress; 
        uint addressLength;
        NetworkInterfaceType type; 
        OperationalStatus operStatus; 
        long speed;
 

        //Unfortunately, any interface can
        //have two completely different valid indexes for ipv4 and ipv6
        internal uint index = 0; 
        internal uint ipv6Index = 0;
 
        AdapterFlags adapterFlags; 

        //ipv4 only 
        //
        SystemIPInterfaceProperties interfaceProperties = null;

 
        private SystemNetworkInterface(){}
 
 
        //methods
        /// <summary> 
        /// Gets the network interfaces local to the machine.
        /// If the machine is >= XP, we call the native GetAdaptersAddresses api to prepopulate all of the interface
        /// instances and most of their associated information.  Otherwise, GetAdaptersInfo is called.
        /// </summary> 

        //this returns both ipv4 and ipv6 interfaces 
        internal static NetworkInterface[] GetNetworkInterfaces(){ 
            return GetNetworkInterfaces(0);
        } 


        internal static int InternalLoopbackInterfaceIndex{
            get{ 
                int index;
                int error = (int)UnsafeNetInfoNativeMethods.GetBestInterface(0x100007F,out index); 
                if (error != 0) { 
                    throw new NetworkInformationException(error);
                } 

                return index;
            }
        } 

        internal static bool InternalGetIsNetworkAvailable(){ 
 
            if(ComNetOS.IsWinNt){
                NetworkInterface[] networkInterfaces = GetNetworkInterfaces(); 
                foreach (NetworkInterface netInterface in networkInterfaces) {
                    if(netInterface.OperationalStatus == OperationalStatus.Up && netInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                       && netInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback){
                        return true; 
                    }
                } 
            } 
            else{
                uint flags=0; 
                return UnsafeWinINetNativeMethods.InternetGetConnectedState (ref flags, 0);
            }
            return false;
 
        }
 
 
        //address family specific
        private static NetworkInterface[] GetNetworkInterfaces(AddressFamily family){ 


            IpHelperErrors.CheckFamilyUnspecified(family);
 
            //uses GetAdapterAddresses if os >= winxp
            if (ComNetOS.IsPostWin2K) { 
                return PostWin2KGetNetworkInterfaces(family); 
            }
 
            // os < winxp
            //make sure we are only looking for ipv4

            FixedInfo fixedInfo = SystemIPGlobalProperties.GetFixedInfo(); 

            if (family != 0 && family!= AddressFamily.InterNetwork) 
                throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired)); 

 
            SafeLocalFree buffer = null;
            uint    size = 0;
            IpAdapterInfo info;
            ArrayList interfaceList = new ArrayList(); 

            //figure out the size of the buffer we need 
            uint result = UnsafeNetInfoNativeMethods.GetAdaptersInfo(SafeLocalFree.Zero,ref size); 

            while (result == IpHelperErrors.ErrorBufferOverflow) { 
                try {
                    //now get the adapter info and populate the network interface objects.
                    buffer =  SafeLocalFree.LocalAlloc((int)size);
                    result = UnsafeNetInfoNativeMethods.GetAdaptersInfo(buffer,ref size); 

                    if ( result == IpHelperErrors.Success ) { 
                        info = (IpAdapterInfo)Marshal.PtrToStructure(buffer.DangerousGetHandle(),typeof(IpAdapterInfo)); 

                        interfaceList.Add( new SystemNetworkInterface(fixedInfo,info)); 
                        while(info.Next != IntPtr.Zero) {
                            info = (IpAdapterInfo)Marshal.PtrToStructure(info.Next,typeof(IpAdapterInfo));
                            interfaceList.Add( new SystemNetworkInterface(fixedInfo,info));
                        } 
                    }
                } 
                finally { 
                    if (buffer != null)
                        buffer.Close(); 
                }
            }

            // if we don't have any interfaces detected, return empty. 
            if (result == IpHelperErrors.ErrorNoData)
                return new SystemNetworkInterface[0]; 
 
            //otherwise we throw on an error
            if (result != IpHelperErrors.Success) { 
                throw new NetworkInformationException((int)result);
            }

            // create the array of interfaces to return 
            SystemNetworkInterface[] networkInterfaces = new SystemNetworkInterface[interfaceList.Count];
            for ( int i = 0; i < interfaceList.Count; ++i ) { 
                networkInterfaces[i] = (SystemNetworkInterface)interfaceList[i]; 
            }
 
            return networkInterfaces;
        }

 
        private static SystemNetworkInterface[] GetAdaptersAddresses(AddressFamily family, FixedInfo fixedInfo) {
 
            uint size = 0; 
            SafeLocalFree buffer = null;
            ArrayList interfaceList =  new ArrayList(); 
            SystemNetworkInterface[] networkInterfaces = null;

            //get each adapter's info
            //get the size of the buffer required 
            uint result = UnsafeNetInfoNativeMethods.GetAdaptersAddresses(family,0,IntPtr.Zero,SafeLocalFree.Zero,ref size);
            while (result == IpHelperErrors.ErrorBufferOverflow) { 
                try { 
                    //allocate the buffer and get the adapter info
                    buffer =  SafeLocalFree.LocalAlloc((int)size); 
                    result = UnsafeNetInfoNativeMethods.GetAdaptersAddresses(family,0,IntPtr.Zero,buffer,ref size);

                    //if succeeded, we're going to add each new interface
                    if ( result == IpHelperErrors.Success) { 

                        //get the first adapter 
                        // we don't know the number of interfaces until we've 
                        // actually marshalled all of them
                        IpAdapterAddresses adapterAddresses = (IpAdapterAddresses)Marshal.PtrToStructure(buffer.DangerousGetHandle(),typeof(IpAdapterAddresses)); 
                        interfaceList.Add(new SystemNetworkInterface(fixedInfo,adapterAddresses));

                        //get the rest
                        while(adapterAddresses.next != IntPtr.Zero) { 
                            adapterAddresses = (IpAdapterAddresses)Marshal.PtrToStructure(adapterAddresses.next,typeof(IpAdapterAddresses));
                            interfaceList.Add(new SystemNetworkInterface(fixedInfo,adapterAddresses)); 
                        } 
                    }
                } 
                finally {
                    if (buffer != null)
                        buffer.Close();
                    buffer = null; 
                }
            } 
 
            // if we don't have any interfaces detected, return empty.
            if (result == IpHelperErrors.ErrorNoData || result == IpHelperErrors.ErrorInvalidParameter) 
                return new SystemNetworkInterface[0];

            //otherwise we throw on an error
            if (result != IpHelperErrors.Success) { 
                throw new NetworkInformationException((int)result);
            } 
 

            // create the array of interfaces to return 
            networkInterfaces = new SystemNetworkInterface[interfaceList.Count];
            for ( int i = 0; i < interfaceList.Count; ++i ) {
                networkInterfaces[i] = (SystemNetworkInterface)interfaceList[i];
            } 
            return networkInterfaces;
        } 
 
        private static SystemNetworkInterface[] PostWin2KGetNetworkInterfaces(AddressFamily family) {
 
            FixedInfo fixedInfo = SystemIPGlobalProperties.GetFixedInfo();

            SystemNetworkInterface[] networkInterfaces = null;
 
            while (true) {
                try { 
                    networkInterfaces = GetAdaptersAddresses(family, fixedInfo); 
                    break;
                } catch (NetworkInformationException exception) { 
                    if (exception.ErrorCode != IpHelperErrors.ErrorInvalidFunction)
                        throw;
                }
            } 

            if (!Socket.SupportsIPv4) // Only if IPv4 is supported continue below 
            { 
                return networkInterfaces;
            } 

            //GetAdapterAddresses doesn't currently include all of the possible
            //ipv4 information, so we need to call GetAdaptersInfo to populate
            // the rest of the fields. 

            uint size = 0; 
            uint result = 0; 
            SafeLocalFree buffer = null;
 
            if (family == 0 || family == AddressFamily.InterNetwork) {

                //figure out the size of the buffer we need
                result = UnsafeNetInfoNativeMethods.GetAdaptersInfo(SafeLocalFree.Zero,ref size); 

                int failedUpdateCount = 0; 
                //now use GetAdaptersInfo and update the network interface objects w/ the information 
                // missing in GetAdaptersAddresses.
                while (result == IpHelperErrors.ErrorBufferOverflow) { 
                    try {
                        buffer =  SafeLocalFree.LocalAlloc((int)size);
                        result = UnsafeNetInfoNativeMethods.GetAdaptersInfo(buffer,ref size);
 
                        //basically, we get the additional adapterinfo for each ipv4 adapter and
                        //call update on the interface 
                        if ( result == IpHelperErrors.Success ){ 
                            IntPtr myPtr = buffer.DangerousGetHandle();
                            bool success = false; 

                            while (myPtr != IntPtr.Zero)
                            {
                                IpAdapterInfo info = (IpAdapterInfo)Marshal.PtrToStructure(myPtr, typeof(IpAdapterInfo)); 

                                for (int i = 0; i < networkInterfaces.Length; i++) 
                                { 
                                    if (networkInterfaces[i] != null)
                                    { 
                                        if (info.index == networkInterfaces[i].index)
                                        {
                                            success = networkInterfaces[i].interfaceProperties.Update(fixedInfo, info);
                                            if (!success) 
                                            {
                                                networkInterfaces[i] = null; 
                                                failedUpdateCount++; 
                                            }
                                            break; 
                                        }
                                    }
                                }
                                myPtr = info.Next; 
                            }
                        } 
                    } 
                    finally {
                        if (buffer != null) 
                            buffer.Close();
                    }
                }
                if (failedUpdateCount != 0) { 
                    SystemNetworkInterface[] newNetworkInterfaces =
                        new SystemNetworkInterface[networkInterfaces.Length-failedUpdateCount]; 
                    int newArrayIndex = 0; 
                    for (int i=0; i < networkInterfaces.Length; i++) {
                        if (networkInterfaces[i] != null) 
                            newNetworkInterfaces[newArrayIndex++] = networkInterfaces[i];
                    }
                    networkInterfaces = newNetworkInterfaces;
                } 
            }
            // if we don't have any ipv4 interfaces detected, just continue 
            if (result != IpHelperErrors.Success && 
                result != IpHelperErrors.ErrorNoData)
            { 
                throw new NetworkInformationException((int)result);
            }
            return networkInterfaces;
        } 

 
        //This constructor is used only in the OS>=XP case 
        //and uses the GetAdapterAddresses api
        internal SystemNetworkInterface(FixedInfo fixedInfo, IpAdapterAddresses ipAdapterAddresses) { 

            //store the common api information
            id = ipAdapterAddresses.AdapterName;
            name = ipAdapterAddresses.friendlyName; 
            description = ipAdapterAddresses.description;
            index = ipAdapterAddresses.index; 
 
            physicalAddress = ipAdapterAddresses.address;
            addressLength = ipAdapterAddresses.addressLength; 

            type = ipAdapterAddresses.type;
            operStatus = ipAdapterAddresses.operStatus;
 
            //api specific info
            ipv6Index = ipAdapterAddresses.ipv6Index; 
 
            adapterFlags = ipAdapterAddresses.flags;
            interfaceProperties = new SystemIPInterfaceProperties(fixedInfo,ipAdapterAddresses); 
        }


        //This constructor is used only in the OS<XP case 
        //and uses the GetAdapterInfo api
        internal SystemNetworkInterface(FixedInfo fixedInfo, IpAdapterInfo ipAdapterInfo) { 
 
            //store the common api information
            id = ipAdapterInfo.adapterName; 
            name = String.Empty;
            description = ipAdapterInfo.description;
            index = ipAdapterInfo.index;
 
            physicalAddress = ipAdapterInfo.address;
            addressLength = ipAdapterInfo.addressLength; 
 
            //try to discover the adapter name
            if(ComNetOS.IsWin2K && ! ComNetOS.IsPostWin2K){ 
                name = ReadAdapterName(id);
            }

            //otherwise, just use the name returned in description. 
            if(name.Length == 0){
                name = description; 
            } 

 
            SystemIPv4InterfaceStatistics s = new SystemIPv4InterfaceStatistics(index);
            operStatus = s.OperationalStatus;

 
            //we need to convert the old adapterinfo types
            //to the new ones used in AdapterAddresses 
            switch (ipAdapterInfo.type){ 
                case OldInterfaceType.Ethernet:
                    type = NetworkInterfaceType.Ethernet; 
                    break;
                case OldInterfaceType.Fddi:
                    type = NetworkInterfaceType.Fddi;
                    break; 
                case OldInterfaceType.Loopback:
                    type = NetworkInterfaceType.Loopback; 
                    break; 
                case OldInterfaceType.Ppp:
                    type = NetworkInterfaceType.Ppp; 
                    break;
                case OldInterfaceType.Slip:
                    type = NetworkInterfaceType.Slip;
                    break; 
                case OldInterfaceType.TokenRing:
                    type = NetworkInterfaceType.TokenRing; 
                    break; 
                default:
                    type = NetworkInterfaceType.Unknown; 
                    break;
            }
            interfaceProperties = new SystemIPInterfaceProperties(fixedInfo,ipAdapterInfo);
        } 

 
 

        /// Basic Properties 

        public override string Id{get {return id;}}
        public override string Name{get {return name;}}
        public override string Description{get {return description;}} 

 
        public override PhysicalAddress GetPhysicalAddress(){ 
            byte[] newAddr = new byte[addressLength];
            Array.Copy(physicalAddress,newAddr,addressLength); 
            return new PhysicalAddress(newAddr);
        }
        public override NetworkInterfaceType NetworkInterfaceType{get {return type;}}
 
        // Stats specific for Ipv4.  Ipv6 will be added in Longhorn
        public override IPInterfaceProperties GetIPProperties(){ 
            return interfaceProperties; 
        }
 


        // Stats specific for Ipv4.  Ipv6 will be added in Longhorn
        /// <include file='doc\NetworkInterface.uex' path='docs/doc[@for="NetworkInterface.GetInterfaceStatistics"]/*' /> 
        public override IPv4InterfaceStatistics GetIPv4Statistics(){
            return new SystemIPv4InterfaceStatistics(index); 
        } 

 
        public override bool Supports(NetworkInterfaceComponent networkInterfaceComponent){
            if (networkInterfaceComponent ==  NetworkInterfaceComponent.IPv6 && ipv6Index >0){
                return true;
            } 
            if (networkInterfaceComponent ==  NetworkInterfaceComponent.IPv4 && index >0){
                return true; 
            } 
            return false;
        } 

        /// <summary>Is the interface administratively enabled.</summary>
        //public override bool AdministrationEnabled{get {return false;}}           //interface specific property
 

        //We cache this to be consistent across all platforms 
        public override OperationalStatus OperationalStatus{ 
            get {
                return operStatus; 
            }
        }

 
        public override long Speed{
            get { 
 
                if (speed == 0) {
                    SystemIPv4InterfaceStatistics s = new SystemIPv4InterfaceStatistics(index); 
                    speed = s.Speed;
                }
                return speed;
            } 
        }
 
        /// <exception cref='platform not supported'>OS &lt; XP</exception> 
        /// <platnote platform='OS &gt;= XP'>
        /// </platnote> 
        public override bool IsReceiveOnly {
            get {
                if (! ComNetOS.IsPostWin2K)
                    throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired)); 
                return ((adapterFlags & AdapterFlags.ReceiveOnly) > 0);
            } 
        } //throw if <winxp 
        /// <summary>The interface doesn't allow multicast.</summary>
        /// <exception cref='platform not supported'>OS &lt; XP</exception> 
        /// <platnote platform='OS &gt;= XP'>
        /// </platnote>
        public override bool SupportsMulticast {
            get { 
                if (! ComNetOS.IsPostWin2K)
                    throw new PlatformNotSupportedException(SR.GetString(SR.WinXPRequired)); 
                return ((adapterFlags & AdapterFlags.NoMulticast) == 0); 
            }
        } 

        [RegistryPermission(SecurityAction.Assert, Read="HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Network\\{4D36E972-E325-11CE-BFC1-08002BE10318}")]
        private string ReadAdapterName(string id) {
            RegistryKey key = null; 
            string name = String.Empty;
            try { 
                string subKey = "SYSTEM\\CurrentControlSet\\Control\\Network\\{4D36E972-E325-11CE-BFC1-08002BE10318}\\"+id+"\\Connection"; 
                key = Registry.LocalMachine.OpenSubKey(subKey);
                if (null != key) { 
                    name = (string) key.GetValue("Name");
                    if (name == null ) {
                       name = String.Empty;
                    } 
                }
            } 
            finally{ 
                if (null != key){
                    key.Close(); 
                }
            }
            return name;
        } 
    }
} 
 

 
