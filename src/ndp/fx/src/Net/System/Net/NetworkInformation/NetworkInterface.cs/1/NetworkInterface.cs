 
using System;

namespace System.Net.NetworkInformation
{ 

    public abstract class NetworkInterface 
    { 
        /// Returns objects that describe the network interfaces on the local computer.
        public static NetworkInterface[] GetAllNetworkInterfaces(){ 
            (new NetworkInformationPermission(NetworkInformationAccess.Read)).Demand();
            return SystemNetworkInterface.GetNetworkInterfaces();
        }
 
        public static bool GetIsNetworkAvailable(){
            return SystemNetworkInterface.InternalGetIsNetworkAvailable(); 
        } 

        public static int LoopbackInterfaceIndex{ 
            get{
                return SystemNetworkInterface.InternalLoopbackInterfaceIndex;
            }
        } 

        public abstract string Id{get;} 
 
        /// Gets the name of the network interface.
        public abstract string Name{get;} 

        /// Gets the description of the network interface
        public abstract string Description{get;}
 
        /// Gets the IP properties for this network interface.
        public abstract IPInterfaceProperties GetIPProperties(); 
 
        /// Provides Internet Protocol (IP) statistical data for thisnetwork interface.
        public abstract IPv4InterfaceStatistics GetIPv4Statistics(); 

        /// Gets the current operational state of the network connection.
        public abstract OperationalStatus OperationalStatus{get;}
 
        /// Gets the speed of the interface in bits per second as reported by the interface.
        public abstract long Speed{get;} 
 
        /// Gets a bool value that indicates whether the network interface is set to only receive data packets.
        public abstract bool IsReceiveOnly{get;} 

        /// Gets a bool value that indicates whether this network interface is enabled to receive multicast packets.
        public abstract bool SupportsMulticast{get;}
 
        /// Gets the physical address of this network interface
        /// <b>deonb. This is okay if you don't support this in Whidbey. This actually belongs in the NetworkAdapter derived class</b> 
        public abstract PhysicalAddress GetPhysicalAddress(); 

        /// Gets the interface type. 
        public abstract NetworkInterfaceType NetworkInterfaceType{get;}

        public abstract bool Supports(NetworkInterfaceComponent networkInterfaceComponent);
    } 
}
 
 
using System;

namespace System.Net.NetworkInformation
{ 

    public abstract class NetworkInterface 
    { 
        /// Returns objects that describe the network interfaces on the local computer.
        public static NetworkInterface[] GetAllNetworkInterfaces(){ 
            (new NetworkInformationPermission(NetworkInformationAccess.Read)).Demand();
            return SystemNetworkInterface.GetNetworkInterfaces();
        }
 
        public static bool GetIsNetworkAvailable(){
            return SystemNetworkInterface.InternalGetIsNetworkAvailable(); 
        } 

        public static int LoopbackInterfaceIndex{ 
            get{
                return SystemNetworkInterface.InternalLoopbackInterfaceIndex;
            }
        } 

        public abstract string Id{get;} 
 
        /// Gets the name of the network interface.
        public abstract string Name{get;} 

        /// Gets the description of the network interface
        public abstract string Description{get;}
 
        /// Gets the IP properties for this network interface.
        public abstract IPInterfaceProperties GetIPProperties(); 
 
        /// Provides Internet Protocol (IP) statistical data for thisnetwork interface.
        public abstract IPv4InterfaceStatistics GetIPv4Statistics(); 

        /// Gets the current operational state of the network connection.
        public abstract OperationalStatus OperationalStatus{get;}
 
        /// Gets the speed of the interface in bits per second as reported by the interface.
        public abstract long Speed{get;} 
 
        /// Gets a bool value that indicates whether the network interface is set to only receive data packets.
        public abstract bool IsReceiveOnly{get;} 

        /// Gets a bool value that indicates whether this network interface is enabled to receive multicast packets.
        public abstract bool SupportsMulticast{get;}
 
        /// Gets the physical address of this network interface
        /// <b>deonb. This is okay if you don't support this in Whidbey. This actually belongs in the NetworkAdapter derived class</b> 
        public abstract PhysicalAddress GetPhysicalAddress(); 

        /// Gets the interface type. 
        public abstract NetworkInterfaceType NetworkInterfaceType{get;}

        public abstract bool Supports(NetworkInterfaceComponent networkInterfaceComponent);
    } 
}
 
