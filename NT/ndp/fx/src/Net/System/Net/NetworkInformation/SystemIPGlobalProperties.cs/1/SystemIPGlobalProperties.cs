 

namespace System.Net.NetworkInformation {

    using System.Net; 
    using System.Net.Sockets;
    using System.Security.Permissions; 
    using System; 
    using System.Runtime.InteropServices;
    using System.Collections; 
    using System.ComponentModel;
    using System.Threading;

 

    internal class SystemIPGlobalProperties:IPGlobalProperties { 
        private FixedInfo fixedInfo; 
        private bool fixedInfoInitialized = false;
 

        //changing these require a reboot, so we'll cache them instead.
        private static string hostName = null;
        private static string domainName = null; 

        static object syncObject = new object(); 
 
        internal SystemIPGlobalProperties() {
        } 



        internal static FixedInfo GetFixedInfo(){ 
            uint    size = 0;
            SafeLocalFree buffer = null; 
            FixedInfo fixedInfo = new FixedInfo(); 

            //first we need to get the size of the buffer 
            uint result = UnsafeNetInfoNativeMethods.GetNetworkParams(SafeLocalFree.Zero,ref size);

            while (result == IpHelperErrors.ErrorBufferOverflow) {
                try { 
                    //now we allocate the buffer and read the network parameters.
                    buffer = SafeLocalFree.LocalAlloc((int)size); 
                    result = UnsafeNetInfoNativeMethods.GetNetworkParams(buffer,ref size); 
                    if ( result == IpHelperErrors.Success ) {
                        fixedInfo = new FixedInfo( (FIXED_INFO)Marshal.PtrToStructure(buffer.DangerousGetHandle(),typeof(FIXED_INFO))); 
                    }
                }
                finally {
                    if(buffer != null){ 
                        buffer.Close();
                    } 
                } 
            }
 
            //if the result include there being no information, we'll still throw
            if (result != IpHelperErrors.Success) {
                throw new NetworkInformationException((int)result);
            } 
            return fixedInfo;
        } 
 

        internal FixedInfo FixedInfo { 
            get {
                if (!fixedInfoInitialized) {
                    lock(this){
                        if (!fixedInfoInitialized) { 
                            fixedInfo = GetFixedInfo();
                            fixedInfoInitialized = true; 
                        } 
                    }
                } 
                return fixedInfo;
            }
        }
 
        /// <summary>Specifies the host name for the local computer.</summary>
        public override string HostName{ 
            get { 
                if(hostName == null){
                    lock(syncObject){ 
                        if(hostName == null){
                            hostName = FixedInfo.HostName;
                            domainName = FixedInfo.DomainName;
                        } 
                    }
                } 
                return hostName; 
            }
        } 
        /// <summary>Specifies the domain in which the local computer is registered.</summary>
        public override string DomainName{
            get {
                if(domainName == null){ 
                    lock(syncObject){
                        if(domainName == null){ 
                            hostName = FixedInfo.HostName; 
                            domainName = FixedInfo.DomainName;
                        } 
                    }
                }
                return domainName;
            } 
        }
        /// <summary> 
        /// The type of node. 
        /// </summary>
        /// <remarks> 
        /// The exact mechanism by which NetBIOS names are resolved to IP addresses
        /// depends on the node's configured NetBIOS Node Type. Broadcast - uses broadcast
        /// NetBIOS Name Queries for name registration and resolution.
        /// PeerToPeer - uses a NetBIOS name server (NBNS), such as Windows Internet 
        /// Name Service (WINS), to resolve NetBIOS names.
        /// Mixed - uses Broadcast then PeerToPeer. 
        /// Hybrid - uses PeerToPeer then Broadcast. 
        /// </remarks>
        public override NetBiosNodeType NodeType{get { 
            return (NetBiosNodeType)FixedInfo.NodeType;}
        }
        /// <summary>Specifies the DHCP scope name.</summary>
        public override string DhcpScopeName{get { 
            return FixedInfo.ScopeId;}
        } 
        /// <summary>Specifies whether the local computer is acting as an WINS proxy.</summary> 
        public override bool IsWinsProxy{get {
            return (FixedInfo.EnableProxy);} 
        }


        public override TcpConnectionInformation[] GetActiveTcpConnections(){ 
            ArrayList list = new ArrayList();
            TcpConnectionInformation[] connections = GetAllTcpConnections(); 
            foreach(TcpConnectionInformation connection in connections){ 
                if(connection.State != TcpState.Listen){
                    list.Add(connection); 
                }
            }
            connections = new TcpConnectionInformation[list.Count];
            for (int i=0; i< list.Count;i++) { 
                connections[i]=(TcpConnectionInformation)list[i];
            } 
            return connections; 
        }
 

        public override IPEndPoint[] GetActiveTcpListeners (){
            ArrayList list = new ArrayList();
            TcpConnectionInformation[] connections = GetAllTcpConnections(); 
            foreach(TcpConnectionInformation connection in connections){
                if(connection.State == TcpState.Listen){ 
                    list.Add(connection.LocalEndPoint); 
                }
            } 
            IPEndPoint[] endPoints = new IPEndPoint[list.Count];
            for (int i=0; i< list.Count;i++) {
                endPoints[i]=(IPEndPoint)list[i];
            } 
            return endPoints;
        } 
 

 
        /// <summary>
        /// Gets the active tcp connections. Uses the native GetTcpTable api.</summary>
        private TcpConnectionInformation[] GetAllTcpConnections(){
            uint    size = 0; 
            SafeLocalFree buffer = null;
            SystemTcpConnectionInformation[] tcpConnections = null; 
 

 
            //get the size of buffer needed
            uint result = UnsafeNetInfoNativeMethods.GetTcpTable(SafeLocalFree.Zero,ref size,true);

            while (result == IpHelperErrors.ErrorInsufficientBuffer) { 
                try {
                    //allocate the buffer and get the tcptable 
                    buffer =  SafeLocalFree.LocalAlloc((int)size); 
                    result = UnsafeNetInfoNativeMethods.GetTcpTable(buffer,ref size,true);
 
                    if ( result == IpHelperErrors.Success ) {
                        //the table info just gives us the number of rows.
                        IntPtr newPtr = buffer.DangerousGetHandle();
                        MibTcpTable tcpTableInfo = (MibTcpTable)Marshal.PtrToStructure(newPtr,typeof(MibTcpTable)); 

                        if (tcpTableInfo.numberOfEntries > 0) { 
                            tcpConnections = new SystemTcpConnectionInformation[tcpTableInfo.numberOfEntries]; 

                            //we need to skip over the tableinfo to get the inline rows 
                            newPtr = (IntPtr)((long)newPtr + Marshal.SizeOf(tcpTableInfo.numberOfEntries));

                            for (int i=0;i<tcpTableInfo.numberOfEntries;i++) {
                                MibTcpRow tcpRow = (MibTcpRow)Marshal.PtrToStructure(newPtr,typeof(MibTcpRow)); 
                                tcpConnections[i] = new SystemTcpConnectionInformation(tcpRow);
 
                                //we increment the pointer to the next row 
                                newPtr = (IntPtr)((long)newPtr + Marshal.SizeOf(tcpRow));
                            } 
                        }
                    }
                }
                finally { 
                    if (buffer != null)
                        buffer.Close(); 
                } 
            }
 
            // if we don't have any ipv4 interfaces detected, just continue
            if (result != IpHelperErrors.Success && result != IpHelperErrors.ErrorNoData) {
                throw new NetworkInformationException((int)result);
            } 

            if (tcpConnections == null) { 
                return new SystemTcpConnectionInformation[0]; 
            }
 
            return tcpConnections;
        }

 

 
        /// <summary>Gets the active udp listeners. Uses the native GetUdpTable api.</summary> 
        public override IPEndPoint[] GetActiveUdpListeners(){
            uint    size = 0; 
            SafeLocalFree buffer = null;
            IPEndPoint[] udpListeners = null;

 

            //get the size of buffer needed 
            uint result = UnsafeNetInfoNativeMethods.GetUdpTable(SafeLocalFree.Zero,ref size,true); 
            while (result == IpHelperErrors.ErrorInsufficientBuffer) {
                try { 

                    //allocate the buffer and get the udptable
                    buffer =  SafeLocalFree.LocalAlloc((int)size);
                    result = UnsafeNetInfoNativeMethods.GetUdpTable(buffer,ref size,true); 

                    if ( result ==  IpHelperErrors.Success) { 
 
                        //the table info just gives us the number of rows.
                        IntPtr newPtr = buffer.DangerousGetHandle(); 
                        MibUdpTable udpTableInfo = (MibUdpTable)Marshal.PtrToStructure(newPtr,typeof(MibUdpTable));

                        if (udpTableInfo.numberOfEntries > 0) {
                            udpListeners = new IPEndPoint[udpTableInfo.numberOfEntries]; 

                            //we need to skip over the tableinfo to get the inline rows 
                            newPtr = (IntPtr)((long)newPtr + Marshal.SizeOf(udpTableInfo.numberOfEntries)); 
                            for (int i=0;i<udpTableInfo.numberOfEntries;i++) {
                                MibUdpRow udpRow = (MibUdpRow)Marshal.PtrToStructure(newPtr,typeof(MibUdpRow)); 
                                int localPort = udpRow.localPort3<<24|udpRow.localPort4<<16|udpRow.localPort1<<8|udpRow.localPort2;

                                //need to fix these. Currently this is incorrect if the high bit is set
                                //    uint localPort = (uint)IPAddress.HostToNetworkOrder((short)row.localPort1); 

                                udpListeners[i] = new IPEndPoint(udpRow.localAddr,(int)localPort); 
 

                                //we increment the pointer to the next row 
                                newPtr = (IntPtr)((long)newPtr + Marshal.SizeOf(udpRow));
                            }
                        }
                    } 
                }
                finally { 
                    if (buffer != null) 
                        buffer.Close();
                } 
            }
            // if we don't have any ipv4 interfaces detected, just continue
            if (result != IpHelperErrors.Success && result != IpHelperErrors.ErrorNoData) {
                throw new NetworkInformationException((int)result); 
            }
 
            if (udpListeners == null) { 
                return new IPEndPoint[0];
            } 

            return udpListeners;
        }
 
        public override IPGlobalStatistics GetIPv4GlobalStatistics(){
            return new SystemIPGlobalStatistics(AddressFamily.InterNetwork); 
        } 
        public override IPGlobalStatistics GetIPv6GlobalStatistics(){
            return new SystemIPGlobalStatistics(AddressFamily.InterNetworkV6); 
        }

       public override TcpStatistics GetTcpIPv4Statistics(){
            return new SystemTcpStatistics(AddressFamily.InterNetwork); 
        }
        public override TcpStatistics GetTcpIPv6Statistics(){ 
            return new SystemTcpStatistics(AddressFamily.InterNetworkV6); 
        }
 
        public override UdpStatistics GetUdpIPv4Statistics(){
            return new SystemUdpStatistics(AddressFamily.InterNetwork);
        }
        public override UdpStatistics GetUdpIPv6Statistics(){ 
            return new SystemUdpStatistics(AddressFamily.InterNetworkV6);
        } 
 
        public override IcmpV4Statistics GetIcmpV4Statistics(){
            return new SystemIcmpV4Statistics(); 
        }

        public override IcmpV6Statistics GetIcmpV6Statistics(){
            return new SystemIcmpV6Statistics(); 
        }
 
    }   //ends networkinformation class 

 

    internal struct FixedInfo{
        internal FIXED_INFO info;
        internal IPAddressCollection dnsAddresses; 

 
        internal FixedInfo(FIXED_INFO info){ 
            this.info = info;
            dnsAddresses = info.DnsServerList.ToIPAddressCollection(); 
        }

        internal IPAddressCollection DnsAddresses{
            get{ 
                return dnsAddresses;
            } 
        } 

        internal string HostName{ 
            get{
                return info.hostName;
            }
        } 

        internal string DomainName{ 
            get{ 
                return info.domainName;
            } 
        }

        internal NetBiosNodeType NodeType{
            get{ 
                return info.nodeType;
            } 
        } 
        internal string ScopeId{
            get{ 
                return info.scopeId;
            }
        }
 
        internal bool EnableRouting{
            get{ 
                return info.enableRouting; 
            }
        } 

        internal bool EnableProxy{
            get{
                return info.enableProxy; 
            }
        } 
 
        internal bool EnableDns{
            get{ 
                return info.enableDns;
            }
        }
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

 

    internal class SystemIPGlobalProperties:IPGlobalProperties { 
        private FixedInfo fixedInfo; 
        private bool fixedInfoInitialized = false;
 

        //changing these require a reboot, so we'll cache them instead.
        private static string hostName = null;
        private static string domainName = null; 

        static object syncObject = new object(); 
 
        internal SystemIPGlobalProperties() {
        } 



        internal static FixedInfo GetFixedInfo(){ 
            uint    size = 0;
            SafeLocalFree buffer = null; 
            FixedInfo fixedInfo = new FixedInfo(); 

            //first we need to get the size of the buffer 
            uint result = UnsafeNetInfoNativeMethods.GetNetworkParams(SafeLocalFree.Zero,ref size);

            while (result == IpHelperErrors.ErrorBufferOverflow) {
                try { 
                    //now we allocate the buffer and read the network parameters.
                    buffer = SafeLocalFree.LocalAlloc((int)size); 
                    result = UnsafeNetInfoNativeMethods.GetNetworkParams(buffer,ref size); 
                    if ( result == IpHelperErrors.Success ) {
                        fixedInfo = new FixedInfo( (FIXED_INFO)Marshal.PtrToStructure(buffer.DangerousGetHandle(),typeof(FIXED_INFO))); 
                    }
                }
                finally {
                    if(buffer != null){ 
                        buffer.Close();
                    } 
                } 
            }
 
            //if the result include there being no information, we'll still throw
            if (result != IpHelperErrors.Success) {
                throw new NetworkInformationException((int)result);
            } 
            return fixedInfo;
        } 
 

        internal FixedInfo FixedInfo { 
            get {
                if (!fixedInfoInitialized) {
                    lock(this){
                        if (!fixedInfoInitialized) { 
                            fixedInfo = GetFixedInfo();
                            fixedInfoInitialized = true; 
                        } 
                    }
                } 
                return fixedInfo;
            }
        }
 
        /// <summary>Specifies the host name for the local computer.</summary>
        public override string HostName{ 
            get { 
                if(hostName == null){
                    lock(syncObject){ 
                        if(hostName == null){
                            hostName = FixedInfo.HostName;
                            domainName = FixedInfo.DomainName;
                        } 
                    }
                } 
                return hostName; 
            }
        } 
        /// <summary>Specifies the domain in which the local computer is registered.</summary>
        public override string DomainName{
            get {
                if(domainName == null){ 
                    lock(syncObject){
                        if(domainName == null){ 
                            hostName = FixedInfo.HostName; 
                            domainName = FixedInfo.DomainName;
                        } 
                    }
                }
                return domainName;
            } 
        }
        /// <summary> 
        /// The type of node. 
        /// </summary>
        /// <remarks> 
        /// The exact mechanism by which NetBIOS names are resolved to IP addresses
        /// depends on the node's configured NetBIOS Node Type. Broadcast - uses broadcast
        /// NetBIOS Name Queries for name registration and resolution.
        /// PeerToPeer - uses a NetBIOS name server (NBNS), such as Windows Internet 
        /// Name Service (WINS), to resolve NetBIOS names.
        /// Mixed - uses Broadcast then PeerToPeer. 
        /// Hybrid - uses PeerToPeer then Broadcast. 
        /// </remarks>
        public override NetBiosNodeType NodeType{get { 
            return (NetBiosNodeType)FixedInfo.NodeType;}
        }
        /// <summary>Specifies the DHCP scope name.</summary>
        public override string DhcpScopeName{get { 
            return FixedInfo.ScopeId;}
        } 
        /// <summary>Specifies whether the local computer is acting as an WINS proxy.</summary> 
        public override bool IsWinsProxy{get {
            return (FixedInfo.EnableProxy);} 
        }


        public override TcpConnectionInformation[] GetActiveTcpConnections(){ 
            ArrayList list = new ArrayList();
            TcpConnectionInformation[] connections = GetAllTcpConnections(); 
            foreach(TcpConnectionInformation connection in connections){ 
                if(connection.State != TcpState.Listen){
                    list.Add(connection); 
                }
            }
            connections = new TcpConnectionInformation[list.Count];
            for (int i=0; i< list.Count;i++) { 
                connections[i]=(TcpConnectionInformation)list[i];
            } 
            return connections; 
        }
 

        public override IPEndPoint[] GetActiveTcpListeners (){
            ArrayList list = new ArrayList();
            TcpConnectionInformation[] connections = GetAllTcpConnections(); 
            foreach(TcpConnectionInformation connection in connections){
                if(connection.State == TcpState.Listen){ 
                    list.Add(connection.LocalEndPoint); 
                }
            } 
            IPEndPoint[] endPoints = new IPEndPoint[list.Count];
            for (int i=0; i< list.Count;i++) {
                endPoints[i]=(IPEndPoint)list[i];
            } 
            return endPoints;
        } 
 

 
        /// <summary>
        /// Gets the active tcp connections. Uses the native GetTcpTable api.</summary>
        private TcpConnectionInformation[] GetAllTcpConnections(){
            uint    size = 0; 
            SafeLocalFree buffer = null;
            SystemTcpConnectionInformation[] tcpConnections = null; 
 

 
            //get the size of buffer needed
            uint result = UnsafeNetInfoNativeMethods.GetTcpTable(SafeLocalFree.Zero,ref size,true);

            while (result == IpHelperErrors.ErrorInsufficientBuffer) { 
                try {
                    //allocate the buffer and get the tcptable 
                    buffer =  SafeLocalFree.LocalAlloc((int)size); 
                    result = UnsafeNetInfoNativeMethods.GetTcpTable(buffer,ref size,true);
 
                    if ( result == IpHelperErrors.Success ) {
                        //the table info just gives us the number of rows.
                        IntPtr newPtr = buffer.DangerousGetHandle();
                        MibTcpTable tcpTableInfo = (MibTcpTable)Marshal.PtrToStructure(newPtr,typeof(MibTcpTable)); 

                        if (tcpTableInfo.numberOfEntries > 0) { 
                            tcpConnections = new SystemTcpConnectionInformation[tcpTableInfo.numberOfEntries]; 

                            //we need to skip over the tableinfo to get the inline rows 
                            newPtr = (IntPtr)((long)newPtr + Marshal.SizeOf(tcpTableInfo.numberOfEntries));

                            for (int i=0;i<tcpTableInfo.numberOfEntries;i++) {
                                MibTcpRow tcpRow = (MibTcpRow)Marshal.PtrToStructure(newPtr,typeof(MibTcpRow)); 
                                tcpConnections[i] = new SystemTcpConnectionInformation(tcpRow);
 
                                //we increment the pointer to the next row 
                                newPtr = (IntPtr)((long)newPtr + Marshal.SizeOf(tcpRow));
                            } 
                        }
                    }
                }
                finally { 
                    if (buffer != null)
                        buffer.Close(); 
                } 
            }
 
            // if we don't have any ipv4 interfaces detected, just continue
            if (result != IpHelperErrors.Success && result != IpHelperErrors.ErrorNoData) {
                throw new NetworkInformationException((int)result);
            } 

            if (tcpConnections == null) { 
                return new SystemTcpConnectionInformation[0]; 
            }
 
            return tcpConnections;
        }

 

 
        /// <summary>Gets the active udp listeners. Uses the native GetUdpTable api.</summary> 
        public override IPEndPoint[] GetActiveUdpListeners(){
            uint    size = 0; 
            SafeLocalFree buffer = null;
            IPEndPoint[] udpListeners = null;

 

            //get the size of buffer needed 
            uint result = UnsafeNetInfoNativeMethods.GetUdpTable(SafeLocalFree.Zero,ref size,true); 
            while (result == IpHelperErrors.ErrorInsufficientBuffer) {
                try { 

                    //allocate the buffer and get the udptable
                    buffer =  SafeLocalFree.LocalAlloc((int)size);
                    result = UnsafeNetInfoNativeMethods.GetUdpTable(buffer,ref size,true); 

                    if ( result ==  IpHelperErrors.Success) { 
 
                        //the table info just gives us the number of rows.
                        IntPtr newPtr = buffer.DangerousGetHandle(); 
                        MibUdpTable udpTableInfo = (MibUdpTable)Marshal.PtrToStructure(newPtr,typeof(MibUdpTable));

                        if (udpTableInfo.numberOfEntries > 0) {
                            udpListeners = new IPEndPoint[udpTableInfo.numberOfEntries]; 

                            //we need to skip over the tableinfo to get the inline rows 
                            newPtr = (IntPtr)((long)newPtr + Marshal.SizeOf(udpTableInfo.numberOfEntries)); 
                            for (int i=0;i<udpTableInfo.numberOfEntries;i++) {
                                MibUdpRow udpRow = (MibUdpRow)Marshal.PtrToStructure(newPtr,typeof(MibUdpRow)); 
                                int localPort = udpRow.localPort3<<24|udpRow.localPort4<<16|udpRow.localPort1<<8|udpRow.localPort2;

                                //need to fix these. Currently this is incorrect if the high bit is set
                                //    uint localPort = (uint)IPAddress.HostToNetworkOrder((short)row.localPort1); 

                                udpListeners[i] = new IPEndPoint(udpRow.localAddr,(int)localPort); 
 

                                //we increment the pointer to the next row 
                                newPtr = (IntPtr)((long)newPtr + Marshal.SizeOf(udpRow));
                            }
                        }
                    } 
                }
                finally { 
                    if (buffer != null) 
                        buffer.Close();
                } 
            }
            // if we don't have any ipv4 interfaces detected, just continue
            if (result != IpHelperErrors.Success && result != IpHelperErrors.ErrorNoData) {
                throw new NetworkInformationException((int)result); 
            }
 
            if (udpListeners == null) { 
                return new IPEndPoint[0];
            } 

            return udpListeners;
        }
 
        public override IPGlobalStatistics GetIPv4GlobalStatistics(){
            return new SystemIPGlobalStatistics(AddressFamily.InterNetwork); 
        } 
        public override IPGlobalStatistics GetIPv6GlobalStatistics(){
            return new SystemIPGlobalStatistics(AddressFamily.InterNetworkV6); 
        }

       public override TcpStatistics GetTcpIPv4Statistics(){
            return new SystemTcpStatistics(AddressFamily.InterNetwork); 
        }
        public override TcpStatistics GetTcpIPv6Statistics(){ 
            return new SystemTcpStatistics(AddressFamily.InterNetworkV6); 
        }
 
        public override UdpStatistics GetUdpIPv4Statistics(){
            return new SystemUdpStatistics(AddressFamily.InterNetwork);
        }
        public override UdpStatistics GetUdpIPv6Statistics(){ 
            return new SystemUdpStatistics(AddressFamily.InterNetworkV6);
        } 
 
        public override IcmpV4Statistics GetIcmpV4Statistics(){
            return new SystemIcmpV4Statistics(); 
        }

        public override IcmpV6Statistics GetIcmpV6Statistics(){
            return new SystemIcmpV6Statistics(); 
        }
 
    }   //ends networkinformation class 

 

    internal struct FixedInfo{
        internal FIXED_INFO info;
        internal IPAddressCollection dnsAddresses; 

 
        internal FixedInfo(FIXED_INFO info){ 
            this.info = info;
            dnsAddresses = info.DnsServerList.ToIPAddressCollection(); 
        }

        internal IPAddressCollection DnsAddresses{
            get{ 
                return dnsAddresses;
            } 
        } 

        internal string HostName{ 
            get{
                return info.hostName;
            }
        } 

        internal string DomainName{ 
            get{ 
                return info.domainName;
            } 
        }

        internal NetBiosNodeType NodeType{
            get{ 
                return info.nodeType;
            } 
        } 
        internal string ScopeId{
            get{ 
                return info.scopeId;
            }
        }
 
        internal bool EnableRouting{
            get{ 
                return info.enableRouting; 
            }
        } 

        internal bool EnableProxy{
            get{
                return info.enableProxy; 
            }
        } 
 
        internal bool EnableDns{
            get{ 
                return info.enableDns;
            }
        }
    } 
}
 
