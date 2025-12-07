 

namespace System.Net.NetworkInformation {

    using System.Net; 
    using System.Net.Sockets;
    using System.Security.Permissions; 
    using System; 
    using System.Runtime.InteropServices;
    using System.Collections; 
    using System.ComponentModel;
    using System.Threading;

 

 
    /// <summary> 
    /// Represents an active Tcp connection.</summary>
    internal class SystemTcpConnectionInformation:TcpConnectionInformation { 
        IPEndPoint localEndPoint;
        IPEndPoint remoteEndPoint;
        TcpState state;
 
        internal SystemTcpConnectionInformation(MibTcpRow row) {
            state = row.state; 
 
            //port is returned in Big-Endian - most significant bit on left
            //unfortunately, its done at the word level and not the dword level. 

            int localPort = row.localPort3<<24|row.localPort4<<16|row.localPort1<<8|row.localPort2;
            int remotePort = ((state == TcpState.Listen)?0:row.remotePort3<<24|row.remotePort4<<16|row.remotePort1<<8|row.remotePort2);
 

            //need to fix these. Currently they are incorrect if high order bit is set. 
            //    uint localPort = (uint)IPAddress.HostToNetworkOrder((short)row.localPort1); 
            //  uint remotePort = (uint)IPAddress.HostToNetworkOrder((short)row.remotePort1);
 
            localEndPoint = new IPEndPoint(row.localAddr,(int)localPort);
            remoteEndPoint= new IPEndPoint(row.remoteAddr,(int)remotePort);
        }
 

        public override TcpState State{get {return state;}} 
        public override IPEndPoint LocalEndPoint{get {return localEndPoint;}} 
        public override IPEndPoint RemoteEndPoint{get {return remoteEndPoint;}}
    } 
 }

 

namespace System.Net.NetworkInformation {

    using System.Net; 
    using System.Net.Sockets;
    using System.Security.Permissions; 
    using System; 
    using System.Runtime.InteropServices;
    using System.Collections; 
    using System.ComponentModel;
    using System.Threading;

 

 
    /// <summary> 
    /// Represents an active Tcp connection.</summary>
    internal class SystemTcpConnectionInformation:TcpConnectionInformation { 
        IPEndPoint localEndPoint;
        IPEndPoint remoteEndPoint;
        TcpState state;
 
        internal SystemTcpConnectionInformation(MibTcpRow row) {
            state = row.state; 
 
            //port is returned in Big-Endian - most significant bit on left
            //unfortunately, its done at the word level and not the dword level. 

            int localPort = row.localPort3<<24|row.localPort4<<16|row.localPort1<<8|row.localPort2;
            int remotePort = ((state == TcpState.Listen)?0:row.remotePort3<<24|row.remotePort4<<16|row.remotePort1<<8|row.remotePort2);
 

            //need to fix these. Currently they are incorrect if high order bit is set. 
            //    uint localPort = (uint)IPAddress.HostToNetworkOrder((short)row.localPort1); 
            //  uint remotePort = (uint)IPAddress.HostToNetworkOrder((short)row.remotePort1);
 
            localEndPoint = new IPEndPoint(row.localAddr,(int)localPort);
            remoteEndPoint= new IPEndPoint(row.remoteAddr,(int)remotePort);
        }
 

        public override TcpState State{get {return state;}} 
        public override IPEndPoint LocalEndPoint{get {return localEndPoint;}} 
        public override IPEndPoint RemoteEndPoint{get {return remoteEndPoint;}}
    } 
 }

