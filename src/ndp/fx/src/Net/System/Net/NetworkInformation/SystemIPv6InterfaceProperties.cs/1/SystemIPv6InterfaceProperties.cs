 
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
 
 

 
    internal class SystemIPv6InterfaceProperties:IPv6InterfaceProperties{


        uint index = 0; 
        uint mtu = 0;
 
        internal SystemIPv6InterfaceProperties(uint index, uint mtu) 
        {
           this.index = index; 
           this.mtu = mtu;
        }
        /// <summary>Specifies the Maximum transmission unit in bytes. Uses GetIFEntry.</summary>
        //We cache this to be consistent across all platforms 
        public override int Index{
            get { 
                return (int)index; 
            }
        } 
        /// <summary>Specifies the Maximum transmission unit in bytes. Uses GetIFEntry.</summary>
        //We cache this to be consistent across all platforms
        public override int Mtu{
            get { 
                return (int) mtu;
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
 
 

 
    internal class SystemIPv6InterfaceProperties:IPv6InterfaceProperties{


        uint index = 0; 
        uint mtu = 0;
 
        internal SystemIPv6InterfaceProperties(uint index, uint mtu) 
        {
           this.index = index; 
           this.mtu = mtu;
        }
        /// <summary>Specifies the Maximum transmission unit in bytes. Uses GetIFEntry.</summary>
        //We cache this to be consistent across all platforms 
        public override int Index{
            get { 
                return (int)index; 
            }
        } 
        /// <summary>Specifies the Maximum transmission unit in bytes. Uses GetIFEntry.</summary>
        //We cache this to be consistent across all platforms
        public override int Mtu{
            get { 
                return (int) mtu;
            } 
        } 
    }
} 
