 
using System;

namespace System.Net.NetworkInformation
{ 

    /// Provides information about a network interface address. 
    internal class SystemGatewayIPAddressInformation:GatewayIPAddressInformation 
    {
        IPAddress address; 

        internal SystemGatewayIPAddressInformation(IPAddress address){
            this.address = address;
        } 

        /// Gets the Internet Protocol (IP) address. 
        public override IPAddress Address { 
            get{
                return address; 
            }
        }
    }
} 

 
using System;

namespace System.Net.NetworkInformation
{ 

    /// Provides information about a network interface address. 
    internal class SystemGatewayIPAddressInformation:GatewayIPAddressInformation 
    {
        IPAddress address; 

        internal SystemGatewayIPAddressInformation(IPAddress address){
            this.address = address;
        } 

        /// Gets the Internet Protocol (IP) address. 
        public override IPAddress Address { 
            get{
                return address; 
            }
        }
    }
} 

