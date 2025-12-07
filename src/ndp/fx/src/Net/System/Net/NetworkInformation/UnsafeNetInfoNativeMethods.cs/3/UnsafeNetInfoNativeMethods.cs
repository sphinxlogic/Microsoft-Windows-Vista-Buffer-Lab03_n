 
namespace System.Net.NetworkInformation {
    using System;
    using System.Collections;
    using System.Net; 
    using System.Net.Sockets;
    using System.Runtime.InteropServices; 
    using System.Threading; 

 
    using System.Security;
    using System.Runtime.CompilerServices;
    using System.Security.Permissions;
    using System.ComponentModel; 
    using System.Text;
    using Microsoft.Win32.SafeHandles; 
 

    internal class IpHelperErrors { 

        internal const uint Success = 0;
        internal const uint ErrorInvalidFunction = 1;
        internal const uint ErrorNoSuchDevice = 2; 
        internal const uint ErrorInvalidData= 13;
        internal const uint ErrorInvalidParameter = 87; 
        internal const uint ErrorBufferOverflow = 111; 
        internal const uint ErrorInsufficientBuffer = 122;
        internal const uint ErrorNoData= 232; 
        internal const uint Pending = 997;
        internal const uint ErrorNotFound = 1168;

        internal static void CheckFamilyUnspecified(AddressFamily family) 
        {
            if (family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6 
                && family != AddressFamily.Unspecified) { 
                throw new ArgumentException(SR.GetString(SR.net_invalidversion), "family");
            } 
        }
    }

 

 
    internal enum OldInterfaceType{Unknown=0,Ethernet=6,TokenRing=9,Fddi=15,Ppp=23,Loopback=24,Slip=28}; 

 
    //
    // Per-adapter Flags
    //
 
    [Flags]
    internal enum AdapterFlags { 
        DnsEnabled=               0x01, 
        RegisterAdapterSuffix=    0x02,
        DhcpEnabled =                0x04, 
        ReceiveOnly =               0x08,
        NoMulticast=               0x10,
        Ipv6OtherStatefulConfig= 0x20
    }; 

    [Flags] 
    internal enum AdapterAddressFlags{ 
        DnsEligible = 0x1,
        Transient = 0x2 
    }
    internal enum OldOperationalStatus{
        NonOperational      =0,
        Unreachable          =1, 
        Disconnected         =2,
        Connecting           =3, 
        Connected            =4, 
        Operational          =5
    } 

    [Flags]
    internal enum GetAdaptersAddressesFlags
    { 
        SkipUnicast       = 0x0001,
        SkipAnycast       = 0x0002, 
        SkipMulticast     = 0x0004, 
        SkipDnsServer     = 0x0008,
        IncludePrefix     = 0x0010, 
        SkipFriendlyName  = 0x0020,
    }

 
    internal struct IPExtendedAddress{
        internal IPAddress mask; 
        internal IPAddress address; 
        internal IPExtendedAddress(IPAddress address, IPAddress mask){
            this.address = address; 
            this.mask = mask;
        }
    }
 

 
 
    /// <summary>
    ///   IpAddressList - store an IP address with its corresponding subnet mask, 
    ///   both as dotted decimal strings
    /// </summary>
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Ansi)]
    internal struct IpAddrString { 
        internal IntPtr               Next;      /* struct _IpAddressList* */
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=16)] 
        internal string IpAddress; 
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=16)]
        internal string    IpMask; 
        internal uint              Context;

        //helper method to parse the ipaddresses
        internal IPAddressCollection ToIPAddressCollection() { 
            IpAddrString addr        = this;
            IPAddressCollection addresslist = new IPAddressCollection(); 
 
            if (addr.IpAddress.Length != 0)
                addresslist.InternalAdd(IPAddress.Parse(addr.IpAddress)); 

            while ( addr.Next != IntPtr.Zero ) {
                addr = (IpAddrString)Marshal.PtrToStructure(addr.Next,typeof(IpAddrString));
                if (addr.IpAddress.Length != 0) 
                    addresslist.InternalAdd(IPAddress.Parse(addr.IpAddress));
            } 
            return addresslist; 
        }
 
        internal ArrayList ToIPExtendedAddressArrayList() {
            IpAddrString addr        = this;
            ArrayList addresslist = new ArrayList();
 
            if (addr.IpAddress.Length != 0)
                addresslist.Add(new IPExtendedAddress(IPAddress.Parse(addr.IpAddress),IPAddress.Parse(addr.IpMask))); 
 
            while ( addr.Next != IntPtr.Zero ) {
                addr = (IpAddrString)Marshal.PtrToStructure(addr.Next,typeof(IpAddrString)); 
                if (addr.IpAddress.Length != 0)
                    addresslist.Add(new IPExtendedAddress(IPAddress.Parse(addr.IpAddress),IPAddress.Parse(addr.IpMask)));
            }
            return addresslist; 
        }
 
 
        internal GatewayIPAddressInformationCollection ToIPGatewayAddressCollection() {
            IpAddrString addr        = this; 
            GatewayIPAddressInformationCollection addresslist = new GatewayIPAddressInformationCollection();

            if (addr.IpAddress.Length != 0)
                addresslist.InternalAdd(new SystemGatewayIPAddressInformation(IPAddress.Parse(addr.IpAddress))); 

            while ( addr.Next != IntPtr.Zero ) { 
                addr = (IpAddrString)Marshal.PtrToStructure(addr.Next,typeof(IpAddrString)); 
                if (addr.IpAddress.Length != 0)
                    addresslist.InternalAdd(new SystemGatewayIPAddressInformation(IPAddress.Parse(addr.IpAddress))); 
            }
            return addresslist;
        }
 
    };
 
    internal struct IPAddressInfo{ 
        /* Consider Removing
        internal IPAddressInfo(IPAddress addr, IPAddress mask, uint context){ 
            this.addr = addr;
            this.mask = mask;
            this.context = context;
        } 
        */
        internal IPAddress addr; 
        internal IPAddress mask; 
        internal uint context;
    } 


    /// <summary>
    ///   Core network information. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Ansi)] 
    internal struct FIXED_INFO { 
        internal const int MAX_HOSTNAME_LEN               = 128;
        internal const int MAX_DOMAIN_NAME_LEN            = 128; 
        internal const int MAX_SCOPE_ID_LEN               = 256;

        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=MAX_HOSTNAME_LEN + 4)]
        internal string         hostName; 
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=MAX_DOMAIN_NAME_LEN + 4)]
        internal string         domainName; 
        internal uint           currentDnsServer; /* IpAddressList* */ 
        internal IpAddrString DnsServerList;
        internal NetBiosNodeType           nodeType; 
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=MAX_SCOPE_ID_LEN + 4)]
        internal string         scopeId;
        internal bool           enableRouting;
        internal bool           enableProxy; 
        internal bool           enableDns;
    }; 
 

 

    /// <summary>
    ///   ADAPTER_INFO - per-adapter information. All IP addresses are stored as
    ///   strings 
    /// </summary>
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Ansi)] 
    internal struct IpAdapterInfo { 
        internal const int MAX_ADAPTER_DESCRIPTION_LENGTH = 128;
        internal const int MAX_ADAPTER_NAME_LENGTH        = 256; 
        internal const int MAX_ADAPTER_ADDRESS_LENGTH     = 8;

        internal IntPtr /* struct _IP_ADAPTER_INFO* */ Next;
        internal uint           comboIndex; 
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=MAX_ADAPTER_NAME_LENGTH + 4)]
        internal String         adapterName; 
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=MAX_ADAPTER_DESCRIPTION_LENGTH + 4)] 
        internal String         description;
        internal uint           addressLength; 
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=MAX_ADAPTER_ADDRESS_LENGTH)]
        internal byte[]         address;
        internal uint           index;
        internal OldInterfaceType           type; 
        internal bool           dhcpEnabled;
        internal IntPtr           currentIpAddress; /* IpAddressList* */ 
        internal IpAddrString ipAddressList; 
        internal IpAddrString gatewayList;
        internal IpAddrString dhcpServer; 
        [MarshalAs(UnmanagedType.Bool)]
        internal bool           haveWins;
        internal IpAddrString primaryWinsServer;
        internal IpAddrString secondaryWinsServer; 
        internal uint/*time_t*/ leaseObtained;
        internal uint/*time_t*/ leaseExpires; 
    }; 

    [StructLayout(LayoutKind.Sequential)] 
    internal struct IpSocketAddress {
        internal IntPtr address;
        internal int addressLength;
    } 

    // IP_ADAPTER_ANYCAST_ADDRESS 
    // IP_ADAPTER_MULTICAST_ADDRESS 
    // IP_ADAPTER_DNS_SERVER_ADDRESS
    [StructLayout(LayoutKind.Sequential)] 
    internal struct IpAdapterAddress {
        internal uint length;
        internal AdapterAddressFlags flags;
        internal IntPtr next; 
        internal IpSocketAddress address;
    } 
 
    [StructLayout(LayoutKind.Sequential)]
    internal struct IpAdapterUnicastAddress { 
        internal uint length;
        internal AdapterAddressFlags flags;
        internal IntPtr next;
        internal IpSocketAddress address; 
        internal PrefixOrigin prefixOrigin;
        internal SuffixOrigin suffixOrigin; 
        internal DuplicateAddressDetectionState dadState; 
        internal uint validLifetime;
        internal uint preferredLifetime; 
        internal uint leaseLifetime;
    }

    [StructLayout(LayoutKind.Sequential)] 
    internal struct IpAdapterPrefix {
        internal uint length; 
        internal uint ifIndex; 
        internal IntPtr next;
        internal IpSocketAddress address; 
        internal uint prefixLength;
    }

 
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    internal struct IpAdapterAddresses{ 
        internal const int MAX_ADAPTER_ADDRESS_LENGTH     = 8; 

        internal uint length; 
        internal uint index;
        internal IntPtr next;

        //needs to be ansi 
        [MarshalAs(UnmanagedType.LPStr)]
        internal string AdapterName; 
 
        internal IntPtr FirstUnicastAddress;
        internal IntPtr FirstAnycastAddress; 
        internal IntPtr FirstMulticastAddress;
        internal IntPtr FirstDnsServerAddress;

        internal string dnsSuffix; 
        internal string description;
        internal string friendlyName; 
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=MAX_ADAPTER_ADDRESS_LENGTH)] 
        internal byte[]         address;
        internal uint           addressLength; 
        internal AdapterFlags flags;
        internal uint mtu;
        internal NetworkInterfaceType type;
        internal OperationalStatus operStatus; 
        internal uint ipv6Index;
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=16)] 
        internal uint[] zoneIndices; //16 
        internal IntPtr firstPrefix;
    } 


    /// <summary>
    ///   IP_PER_ADAPTER_INFO - per-adapter IP information such as DNS server list. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Ansi)] 
    internal struct IpPerAdapterInfo { 
        internal bool           autoconfigEnabled;
        internal bool           autoconfigActive; 
        internal IntPtr         currentDnsServer; /* IpAddressList* */
        internal IpAddrString dnsServerList;
    };
 

    /// <summary> 
    ///   Network Interface information. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] 
    internal struct MibIfRow {

        //
        // Definitions and structures used by getnetworkparams and getadaptersinfo apis 
        //
        //  internal const uint DEFAULT_MINIMUM_ENTITIES       = 32; 
 
        internal const int MAX_INTERFACE_NAME_LEN         = 256;
        internal const int MAXLEN_IFDESCR                 = 256; 
        internal const int MAXLEN_PHYSADDR                = 8;

        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=MAX_INTERFACE_NAME_LEN)]
        internal string wszName; 
        internal uint   dwIndex;
        internal uint   dwType; 
        internal uint   dwMtu; 
        internal uint   dwSpeed;
        internal uint   dwPhysAddrLen; 
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=MAXLEN_PHYSADDR)]
        internal byte[] bPhysAddr;
        internal uint   dwAdminStatus;
        internal OldOperationalStatus   operStatus; 
        internal uint   dwLastChange;
        internal uint   dwInOctets; 
        internal uint   dwInUcastPkts; 
        internal uint   dwInNUcastPkts;
        internal uint   dwInDiscards; 
        internal uint   dwInErrors;
        internal uint   dwInUnknownProtos;
        internal uint   dwOutOctets;
        internal uint   dwOutUcastPkts; 
        internal uint   dwOutNUcastPkts;
        internal uint   dwOutDiscards; 
        internal uint   dwOutErrors; 
        internal uint   dwOutQLen;
        internal uint   dwDescrLen; 
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=MAXLEN_IFDESCR)]
        internal byte[] bDescr;
    }
 

    [StructLayout(LayoutKind.Sequential)] 
    internal struct MibUdpStats { 
        internal uint datagramsReceived;
        internal uint incomingDatagramsDiscarded; 
        internal uint incomingDatagramsWithErrors;
        internal uint datagramsSent;
        internal uint udpListeners;
    } 

    [StructLayout(LayoutKind.Sequential)] 
    internal struct MibTcpStats { 
        internal uint reTransmissionAlgorithm;
        internal uint minimumRetransmissionTimeOut; 
        internal uint maximumRetransmissionTimeOut;
        internal uint maximumConnections;
        internal uint activeOpens;
        internal uint passiveOpens; 
        internal uint failedConnectionAttempts;
        internal uint resetConnections; 
        internal uint currentConnections; 
        internal uint segmentsReceived;
        internal uint segmentsSent; 
        internal uint segmentsResent;
        internal uint errorsReceived;
        internal uint segmentsSentWithReset;
        internal uint cumulativeConnections; 
    }
 
 
    [StructLayout(LayoutKind.Sequential)]
    internal struct MibIpStats { 
        internal bool forwardingEnabled;
        internal uint defaultTtl;
        internal uint packetsReceived;
        internal uint receivedPacketsWithHeaderErrors; 
        internal uint receivedPacketsWithAddressErrors;
        internal uint packetsForwarded; 
        internal uint receivedPacketsWithUnknownProtocols; 
        internal uint receivedPacketsDiscarded;
        internal uint receivedPacketsDelivered; 
        internal uint packetOutputRequests;
        internal uint outputPacketRoutingDiscards;
        internal uint outputPacketsDiscarded;
        internal uint outputPacketsWithNoRoute; 
        internal uint packetReassemblyTimeout;
        internal uint packetsReassemblyRequired; 
        internal uint packetsReassembled; 
        internal uint packetsReassemblyFailed;
        internal uint packetsFragmented; 
        internal uint packetsFragmentFailed;
        internal uint packetsFragmentCreated;
        internal uint interfaces;
        internal uint ipAddresses; 
        internal uint routes;
    } 
 
    [StructLayout(LayoutKind.Sequential)]
    internal struct MibIcmpInfo { 
        internal MibIcmpStats inStats;
        internal MibIcmpStats outStats;
    }
 
    [StructLayout(LayoutKind.Sequential)]
    internal struct MibIcmpStats { 
        internal uint messages; 
        internal uint errors;
        internal uint destinationUnreachables; 
        internal uint timeExceeds;
        internal uint parameterProblems;
        internal uint sourceQuenches;
        internal uint redirects; 
        internal uint echoRequests;
        internal uint echoReplies; 
        internal uint timestampRequests; 
        internal uint timestampReplies;
        internal uint addressMaskRequests; 
        internal uint addressMaskReplies;
    }

    [StructLayout(LayoutKind.Sequential)] 
    internal struct MibIcmpInfoEx {
        internal MibIcmpStatsEx inStats; 
        internal MibIcmpStatsEx outStats; 
    }
 
    [StructLayout(LayoutKind.Sequential)]
    internal struct MibIcmpStatsEx {
        internal uint       dwMsgs;
        internal uint       dwErrors; 
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=256)]
        internal uint[]      rgdwTypeCount; 
    } 

    [StructLayout(LayoutKind.Sequential)] 
    internal struct MibTcpTable {
        internal uint numberOfEntries;
    }
 
    [StructLayout(LayoutKind.Sequential)]
    internal struct MibTcpRow { 
        internal TcpState  state; 
        internal uint  localAddr;
        internal byte  localPort1; 
        internal byte  localPort2;
        internal byte  localPort3;
        internal byte  localPort4;
        internal uint  remoteAddr; 
        internal byte  remotePort1;
        internal byte  remotePort2; 
        internal byte  remotePort3; 
        internal byte  remotePort4;
    } 

    [StructLayout(LayoutKind.Sequential)]
    internal struct MibUdpTable {
        internal uint numberOfEntries; 
    }
 
    [StructLayout(LayoutKind.Sequential)] 
    internal struct MibUdpRow {
        internal uint  localAddr; 
        internal byte  localPort1;
        internal byte  localPort2;
        internal byte  localPort3;
        internal byte  localPort4; 
    }
 
 
    [StructLayout(LayoutKind.Sequential)]
    internal struct IPOptions { 
        internal byte  ttl;
        internal byte  tos;
        internal byte  flags;
        internal byte  optionsSize; 
        internal IntPtr optionsData;
 
        internal IPOptions (PingOptions options) 
        {
            ttl = 128; 
            tos = 0;
            flags = 0;
            optionsSize = 0;
            optionsData = IntPtr.Zero; 

            if (options != null) { 
                this.ttl = (byte)options.Ttl; 

                if (options.DontFragment){ 
                    flags = 2;
                }
            }
        } 
    }
 
 
    [StructLayout(LayoutKind.Sequential)]
    internal struct IcmpEchoReply { 
        internal uint address;
        internal uint status;
        internal uint  roundTripTime;
        internal ushort dataSize; 
        internal ushort reserved;
        internal IntPtr data; 
        internal IPOptions options; 
        }
       /* 

    [StructLayout(LayoutKind.Sequential)]
    internal struct Ipv6Address {
        ushort sin6_port; 
        uint  sin6_flowinfo;
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=16)] 
        byte[] sin6_addr; 
        uint  sin6_scope_id;
    }    */ 

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    internal struct Ipv6Address {
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=6)] 
        internal byte[] Goo;
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=16)] 
        internal byte[] Address;    // Replying address. 
        internal uint ScopeID;
    } 
 		
    [StructLayout(LayoutKind.Sequential)]
     internal struct Icmp6EchoReply {
        internal Ipv6Address Address; 
        internal uint Status;               // Reply IP_STATUS.
        internal uint RoundTripTime; // RTT in milliseconds. 
        internal IntPtr data; 
        // internal IPOptions options;
        // internal IntPtr data; data os after tjos 
     }


       /* 

    [StructLayout(LayoutKind.Sequential)] 
    internal struct Ipv6Address { 
        ushort sin6_port;
        uint  sin6_flowinfo; 
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=16)]
        byte[] sin6_addr;
        uint  sin6_scope_id;
    }    */ 

    // Reply data follows this structure in memory. 
 
    /// <summary>
    ///   Wrapper for API's in iphlpapi.dll 
    /// </summary>

    [
    System.Security.SuppressUnmanagedCodeSecurityAttribute() 
    ]
    internal static class UnsafeNetInfoNativeMethods { 
 
        private const string IPHLPAPI = "iphlpapi.dll";
 
        [DllImport(IPHLPAPI)]
        internal extern static uint GetAdaptersInfo(SafeLocalFree pAdapterInfo,ref uint pOutBufLen);

        [DllImport(IPHLPAPI)] 
        internal extern static uint GetAdaptersAddresses(
            AddressFamily family, 
            uint flags, 
            IntPtr pReserved,
            SafeLocalFree adapterAddresses, 
            ref uint outBufLen);


        [DllImport(IPHLPAPI)] 
        internal extern static uint GetBestInterface(int ipAddress, out int index);
 
        /* 
        // Consider removing.
        [DllImport(IPHLPAPI)] 
        internal extern static uint GetBestInterfaceEx(byte[] ipAddress, out int index);
        */

        [DllImport(IPHLPAPI)] 
        internal extern static uint GetIfEntry(ref MibIfRow pIfRow);
 
        [DllImport(IPHLPAPI)] 
        internal extern static uint GetIpStatistics(out MibIpStats statistics);
 
        [DllImport(IPHLPAPI)]
        internal extern static uint GetIpStatisticsEx(out MibIpStats statistics, AddressFamily family);

        [DllImport(IPHLPAPI)] 
        internal extern static uint GetTcpStatistics(out MibTcpStats statistics);
 
        [DllImport(IPHLPAPI)] 
        internal extern static uint GetTcpStatisticsEx(out MibTcpStats statistics, AddressFamily family);
 
        [DllImport(IPHLPAPI)]
        internal extern static uint GetUdpStatistics(out MibUdpStats statistics);

        [DllImport(IPHLPAPI)] 
        internal extern static uint GetUdpStatisticsEx(out MibUdpStats statistics, AddressFamily family);
 
        [DllImport(IPHLPAPI)] 
        internal extern static uint GetIcmpStatistics(out MibIcmpInfo statistics);
 
        [DllImport(IPHLPAPI)]
        internal extern static uint GetIcmpStatisticsEx(out MibIcmpInfoEx statistics,AddressFamily family);

        [DllImport(IPHLPAPI)] 
        internal extern static uint GetTcpTable(SafeLocalFree pTcpTable,ref uint dwOutBufLen, bool order);
 
        [DllImport(IPHLPAPI)] 
        internal extern static uint GetUdpTable(SafeLocalFree pUdpTable,ref uint dwOutBufLen, bool order);
 
        // [DllImport(IPHLPAPI)]
        // internal extern static uint GetInterfaceInfo(IntPtr pIfTable,ref uint dwOutBufLen);

        // [DllImport(IPHLPAPI)] 
        // internal extern static uint GetIpAddrTable(SafeLocalFree pIpAddrTable, ref uint dwOutBufLen, bool order);
 
        [DllImport(IPHLPAPI)] 
        internal extern static uint GetNetworkParams(SafeLocalFree pFixedInfo,ref uint pOutBufLen);
 
        // [DllImport(IPHLPAPI)]
        // internal extern static uint GetNumberOfInterfaces(out uint pdwNumIf);

        [DllImport(IPHLPAPI)] 
        internal extern static uint GetPerAdapterInfo(uint IfIndex,SafeLocalFree pPerAdapterInfo,ref uint pOutBufLen);
 
        /* Consider Removing 
        [DllImport(IPHLPAPI)]
        unsafe internal extern static uint NotifyAddrChange(out IntPtr waithandle, [In] NativeOverlapped *overlapped); 
        */

        [DllImport(IPHLPAPI, SetLastError=true)]
        internal extern static SafeCloseIcmpHandle IcmpCreateFile(); 

        [DllImport (IPHLPAPI, SetLastError=true)] 
        internal extern static SafeCloseIcmpHandle Icmp6CreateFile (); 

        [DllImport (IPHLPAPI, SetLastError=true)] 
        internal extern static bool IcmpCloseHandle(IntPtr handle);

        // [DllImport(IPHLPAPI)]
        // internal extern static uint GetBestInterface(uint address4, out uint bestIfIndex); 

        [DllImport (IPHLPAPI, SetLastError=true)] 
        internal extern static uint IcmpSendEcho2 (SafeCloseIcmpHandle icmpHandle, SafeWaitHandle Event, IntPtr apcRoutine, IntPtr apcContext, 
            uint ipAddress, [In] SafeLocalFree data, ushort dataSize, ref IPOptions options, SafeLocalFree replyBuffer, uint replySize, uint timeout);
 
        [DllImport (IPHLPAPI, SetLastError=true)]
        internal extern static uint IcmpSendEcho2 (SafeCloseIcmpHandle icmpHandle, IntPtr Event, IntPtr apcRoutine, IntPtr apcContext,
            uint ipAddress, [In] SafeLocalFree data, ushort dataSize, ref IPOptions options, SafeLocalFree replyBuffer, uint replySize, uint timeout);
 
        [DllImport (IPHLPAPI, SetLastError=true)]
        internal extern static uint Icmp6SendEcho2 (SafeCloseIcmpHandle icmpHandle, SafeWaitHandle Event, IntPtr apcRoutine, IntPtr apcContext, 
            byte[] sourceSocketAddress, byte[] destSocketAddress, [In] SafeLocalFree data, ushort dataSize, ref IPOptions options, SafeLocalFree replyBuffer, uint replySize, uint timeout); 

        [DllImport (IPHLPAPI, SetLastError=true)] 
        internal extern static uint Icmp6SendEcho2 (SafeCloseIcmpHandle icmpHandle, IntPtr Event, IntPtr apcRoutine, IntPtr apcContext,
            byte[] sourceSocketAddress, byte[] destSocketAddress, [In] SafeLocalFree data, ushort dataSize, ref IPOptions options, SafeLocalFree replyBuffer, uint replySize, uint timeout);

        [DllImport (IPHLPAPI, SetLastError=true)] 
        internal unsafe extern static uint IcmpParseReplies (IntPtr replyBuffer, uint replySize);
 
        [DllImport (IPHLPAPI, SetLastError=true)] 
        internal unsafe extern static uint Icmp6ParseReplies (IntPtr replyBuffer, uint replySize);
 



//        [DllImport(IPHLPAPI)] 
//        internal extern static uint NotifyAddrChange(uint nullhandle,uint nulloverlapped);
 
//        [DllImport(IPHLPAPI)] 
//        internal extern static uint NotifyRouteChange(uint nullhandle,uint nulloverlapped);
    } 

    [
    System.Security.SuppressUnmanagedCodeSecurityAttribute()
    ] 
    internal static class UnsafeIcmpNativeMethods {
 
       private const string ICMP = "icmp.dll"; 

       [DllImport(ICMP, SetLastError=true)] 
       internal extern static SafeCloseIcmpHandle IcmpCreateFile();

       [DllImport (ICMP, SetLastError=true)]
       internal extern static bool IcmpCloseHandle(IntPtr icmpHandle); 

       [DllImport (ICMP, SetLastError=true)] 
       internal extern static uint IcmpSendEcho2 (SafeCloseIcmpHandle icmpHandle, SafeWaitHandle Event, IntPtr apcRoutine, IntPtr apcContext, 
       uint ipAddress, [In] SafeLocalFree data, ushort dataSize, ref IPOptions options, SafeLocalFree replyBuffer, uint replySize, uint timeout);
 
       [DllImport (ICMP, SetLastError=true)]
       internal extern static uint IcmpSendEcho2 (SafeCloseIcmpHandle icmpHandle, IntPtr Event, IntPtr apcRoutine, IntPtr apcContext,
       uint ipAddress, [In] SafeLocalFree data, ushort dataSize, ref IPOptions options, SafeLocalFree replyBuffer, uint replySize, uint timeout);
 
       [DllImport (ICMP, SetLastError=true)]
       internal unsafe extern static uint IcmpParseReplies (IntPtr replyBuffer, uint replySize); 
    } 

 
    [
    System.Security.SuppressUnmanagedCodeSecurityAttribute()
    ]
    internal static class UnsafeWinINetNativeMethods { 

       private const string WININET = "wininet.dll"; 
 
       [DllImport (WININET)]
       internal extern static bool InternetGetConnectedState(ref uint flags, uint dwReserved); 
    }
}

 

 
namespace System.Net.NetworkInformation {
    using System;
    using System.Collections;
    using System.Net; 
    using System.Net.Sockets;
    using System.Runtime.InteropServices; 
    using System.Threading; 

 
    using System.Security;
    using System.Runtime.CompilerServices;
    using System.Security.Permissions;
    using System.ComponentModel; 
    using System.Text;
    using Microsoft.Win32.SafeHandles; 
 

    internal class IpHelperErrors { 

        internal const uint Success = 0;
        internal const uint ErrorInvalidFunction = 1;
        internal const uint ErrorNoSuchDevice = 2; 
        internal const uint ErrorInvalidData= 13;
        internal const uint ErrorInvalidParameter = 87; 
        internal const uint ErrorBufferOverflow = 111; 
        internal const uint ErrorInsufficientBuffer = 122;
        internal const uint ErrorNoData= 232; 
        internal const uint Pending = 997;
        internal const uint ErrorNotFound = 1168;

        internal static void CheckFamilyUnspecified(AddressFamily family) 
        {
            if (family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6 
                && family != AddressFamily.Unspecified) { 
                throw new ArgumentException(SR.GetString(SR.net_invalidversion), "family");
            } 
        }
    }

 

 
    internal enum OldInterfaceType{Unknown=0,Ethernet=6,TokenRing=9,Fddi=15,Ppp=23,Loopback=24,Slip=28}; 

 
    //
    // Per-adapter Flags
    //
 
    [Flags]
    internal enum AdapterFlags { 
        DnsEnabled=               0x01, 
        RegisterAdapterSuffix=    0x02,
        DhcpEnabled =                0x04, 
        ReceiveOnly =               0x08,
        NoMulticast=               0x10,
        Ipv6OtherStatefulConfig= 0x20
    }; 

    [Flags] 
    internal enum AdapterAddressFlags{ 
        DnsEligible = 0x1,
        Transient = 0x2 
    }
    internal enum OldOperationalStatus{
        NonOperational      =0,
        Unreachable          =1, 
        Disconnected         =2,
        Connecting           =3, 
        Connected            =4, 
        Operational          =5
    } 

    [Flags]
    internal enum GetAdaptersAddressesFlags
    { 
        SkipUnicast       = 0x0001,
        SkipAnycast       = 0x0002, 
        SkipMulticast     = 0x0004, 
        SkipDnsServer     = 0x0008,
        IncludePrefix     = 0x0010, 
        SkipFriendlyName  = 0x0020,
    }

 
    internal struct IPExtendedAddress{
        internal IPAddress mask; 
        internal IPAddress address; 
        internal IPExtendedAddress(IPAddress address, IPAddress mask){
            this.address = address; 
            this.mask = mask;
        }
    }
 

 
 
    /// <summary>
    ///   IpAddressList - store an IP address with its corresponding subnet mask, 
    ///   both as dotted decimal strings
    /// </summary>
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Ansi)]
    internal struct IpAddrString { 
        internal IntPtr               Next;      /* struct _IpAddressList* */
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=16)] 
        internal string IpAddress; 
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=16)]
        internal string    IpMask; 
        internal uint              Context;

        //helper method to parse the ipaddresses
        internal IPAddressCollection ToIPAddressCollection() { 
            IpAddrString addr        = this;
            IPAddressCollection addresslist = new IPAddressCollection(); 
 
            if (addr.IpAddress.Length != 0)
                addresslist.InternalAdd(IPAddress.Parse(addr.IpAddress)); 

            while ( addr.Next != IntPtr.Zero ) {
                addr = (IpAddrString)Marshal.PtrToStructure(addr.Next,typeof(IpAddrString));
                if (addr.IpAddress.Length != 0) 
                    addresslist.InternalAdd(IPAddress.Parse(addr.IpAddress));
            } 
            return addresslist; 
        }
 
        internal ArrayList ToIPExtendedAddressArrayList() {
            IpAddrString addr        = this;
            ArrayList addresslist = new ArrayList();
 
            if (addr.IpAddress.Length != 0)
                addresslist.Add(new IPExtendedAddress(IPAddress.Parse(addr.IpAddress),IPAddress.Parse(addr.IpMask))); 
 
            while ( addr.Next != IntPtr.Zero ) {
                addr = (IpAddrString)Marshal.PtrToStructure(addr.Next,typeof(IpAddrString)); 
                if (addr.IpAddress.Length != 0)
                    addresslist.Add(new IPExtendedAddress(IPAddress.Parse(addr.IpAddress),IPAddress.Parse(addr.IpMask)));
            }
            return addresslist; 
        }
 
 
        internal GatewayIPAddressInformationCollection ToIPGatewayAddressCollection() {
            IpAddrString addr        = this; 
            GatewayIPAddressInformationCollection addresslist = new GatewayIPAddressInformationCollection();

            if (addr.IpAddress.Length != 0)
                addresslist.InternalAdd(new SystemGatewayIPAddressInformation(IPAddress.Parse(addr.IpAddress))); 

            while ( addr.Next != IntPtr.Zero ) { 
                addr = (IpAddrString)Marshal.PtrToStructure(addr.Next,typeof(IpAddrString)); 
                if (addr.IpAddress.Length != 0)
                    addresslist.InternalAdd(new SystemGatewayIPAddressInformation(IPAddress.Parse(addr.IpAddress))); 
            }
            return addresslist;
        }
 
    };
 
    internal struct IPAddressInfo{ 
        /* Consider Removing
        internal IPAddressInfo(IPAddress addr, IPAddress mask, uint context){ 
            this.addr = addr;
            this.mask = mask;
            this.context = context;
        } 
        */
        internal IPAddress addr; 
        internal IPAddress mask; 
        internal uint context;
    } 


    /// <summary>
    ///   Core network information. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Ansi)] 
    internal struct FIXED_INFO { 
        internal const int MAX_HOSTNAME_LEN               = 128;
        internal const int MAX_DOMAIN_NAME_LEN            = 128; 
        internal const int MAX_SCOPE_ID_LEN               = 256;

        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=MAX_HOSTNAME_LEN + 4)]
        internal string         hostName; 
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=MAX_DOMAIN_NAME_LEN + 4)]
        internal string         domainName; 
        internal uint           currentDnsServer; /* IpAddressList* */ 
        internal IpAddrString DnsServerList;
        internal NetBiosNodeType           nodeType; 
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=MAX_SCOPE_ID_LEN + 4)]
        internal string         scopeId;
        internal bool           enableRouting;
        internal bool           enableProxy; 
        internal bool           enableDns;
    }; 
 

 

    /// <summary>
    ///   ADAPTER_INFO - per-adapter information. All IP addresses are stored as
    ///   strings 
    /// </summary>
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Ansi)] 
    internal struct IpAdapterInfo { 
        internal const int MAX_ADAPTER_DESCRIPTION_LENGTH = 128;
        internal const int MAX_ADAPTER_NAME_LENGTH        = 256; 
        internal const int MAX_ADAPTER_ADDRESS_LENGTH     = 8;

        internal IntPtr /* struct _IP_ADAPTER_INFO* */ Next;
        internal uint           comboIndex; 
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=MAX_ADAPTER_NAME_LENGTH + 4)]
        internal String         adapterName; 
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=MAX_ADAPTER_DESCRIPTION_LENGTH + 4)] 
        internal String         description;
        internal uint           addressLength; 
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=MAX_ADAPTER_ADDRESS_LENGTH)]
        internal byte[]         address;
        internal uint           index;
        internal OldInterfaceType           type; 
        internal bool           dhcpEnabled;
        internal IntPtr           currentIpAddress; /* IpAddressList* */ 
        internal IpAddrString ipAddressList; 
        internal IpAddrString gatewayList;
        internal IpAddrString dhcpServer; 
        [MarshalAs(UnmanagedType.Bool)]
        internal bool           haveWins;
        internal IpAddrString primaryWinsServer;
        internal IpAddrString secondaryWinsServer; 
        internal uint/*time_t*/ leaseObtained;
        internal uint/*time_t*/ leaseExpires; 
    }; 

    [StructLayout(LayoutKind.Sequential)] 
    internal struct IpSocketAddress {
        internal IntPtr address;
        internal int addressLength;
    } 

    // IP_ADAPTER_ANYCAST_ADDRESS 
    // IP_ADAPTER_MULTICAST_ADDRESS 
    // IP_ADAPTER_DNS_SERVER_ADDRESS
    [StructLayout(LayoutKind.Sequential)] 
    internal struct IpAdapterAddress {
        internal uint length;
        internal AdapterAddressFlags flags;
        internal IntPtr next; 
        internal IpSocketAddress address;
    } 
 
    [StructLayout(LayoutKind.Sequential)]
    internal struct IpAdapterUnicastAddress { 
        internal uint length;
        internal AdapterAddressFlags flags;
        internal IntPtr next;
        internal IpSocketAddress address; 
        internal PrefixOrigin prefixOrigin;
        internal SuffixOrigin suffixOrigin; 
        internal DuplicateAddressDetectionState dadState; 
        internal uint validLifetime;
        internal uint preferredLifetime; 
        internal uint leaseLifetime;
    }

    [StructLayout(LayoutKind.Sequential)] 
    internal struct IpAdapterPrefix {
        internal uint length; 
        internal uint ifIndex; 
        internal IntPtr next;
        internal IpSocketAddress address; 
        internal uint prefixLength;
    }

 
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    internal struct IpAdapterAddresses{ 
        internal const int MAX_ADAPTER_ADDRESS_LENGTH     = 8; 

        internal uint length; 
        internal uint index;
        internal IntPtr next;

        //needs to be ansi 
        [MarshalAs(UnmanagedType.LPStr)]
        internal string AdapterName; 
 
        internal IntPtr FirstUnicastAddress;
        internal IntPtr FirstAnycastAddress; 
        internal IntPtr FirstMulticastAddress;
        internal IntPtr FirstDnsServerAddress;

        internal string dnsSuffix; 
        internal string description;
        internal string friendlyName; 
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=MAX_ADAPTER_ADDRESS_LENGTH)] 
        internal byte[]         address;
        internal uint           addressLength; 
        internal AdapterFlags flags;
        internal uint mtu;
        internal NetworkInterfaceType type;
        internal OperationalStatus operStatus; 
        internal uint ipv6Index;
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=16)] 
        internal uint[] zoneIndices; //16 
        internal IntPtr firstPrefix;
    } 


    /// <summary>
    ///   IP_PER_ADAPTER_INFO - per-adapter IP information such as DNS server list. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Ansi)] 
    internal struct IpPerAdapterInfo { 
        internal bool           autoconfigEnabled;
        internal bool           autoconfigActive; 
        internal IntPtr         currentDnsServer; /* IpAddressList* */
        internal IpAddrString dnsServerList;
    };
 

    /// <summary> 
    ///   Network Interface information. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] 
    internal struct MibIfRow {

        //
        // Definitions and structures used by getnetworkparams and getadaptersinfo apis 
        //
        //  internal const uint DEFAULT_MINIMUM_ENTITIES       = 32; 
 
        internal const int MAX_INTERFACE_NAME_LEN         = 256;
        internal const int MAXLEN_IFDESCR                 = 256; 
        internal const int MAXLEN_PHYSADDR                = 8;

        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=MAX_INTERFACE_NAME_LEN)]
        internal string wszName; 
        internal uint   dwIndex;
        internal uint   dwType; 
        internal uint   dwMtu; 
        internal uint   dwSpeed;
        internal uint   dwPhysAddrLen; 
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=MAXLEN_PHYSADDR)]
        internal byte[] bPhysAddr;
        internal uint   dwAdminStatus;
        internal OldOperationalStatus   operStatus; 
        internal uint   dwLastChange;
        internal uint   dwInOctets; 
        internal uint   dwInUcastPkts; 
        internal uint   dwInNUcastPkts;
        internal uint   dwInDiscards; 
        internal uint   dwInErrors;
        internal uint   dwInUnknownProtos;
        internal uint   dwOutOctets;
        internal uint   dwOutUcastPkts; 
        internal uint   dwOutNUcastPkts;
        internal uint   dwOutDiscards; 
        internal uint   dwOutErrors; 
        internal uint   dwOutQLen;
        internal uint   dwDescrLen; 
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=MAXLEN_IFDESCR)]
        internal byte[] bDescr;
    }
 

    [StructLayout(LayoutKind.Sequential)] 
    internal struct MibUdpStats { 
        internal uint datagramsReceived;
        internal uint incomingDatagramsDiscarded; 
        internal uint incomingDatagramsWithErrors;
        internal uint datagramsSent;
        internal uint udpListeners;
    } 

    [StructLayout(LayoutKind.Sequential)] 
    internal struct MibTcpStats { 
        internal uint reTransmissionAlgorithm;
        internal uint minimumRetransmissionTimeOut; 
        internal uint maximumRetransmissionTimeOut;
        internal uint maximumConnections;
        internal uint activeOpens;
        internal uint passiveOpens; 
        internal uint failedConnectionAttempts;
        internal uint resetConnections; 
        internal uint currentConnections; 
        internal uint segmentsReceived;
        internal uint segmentsSent; 
        internal uint segmentsResent;
        internal uint errorsReceived;
        internal uint segmentsSentWithReset;
        internal uint cumulativeConnections; 
    }
 
 
    [StructLayout(LayoutKind.Sequential)]
    internal struct MibIpStats { 
        internal bool forwardingEnabled;
        internal uint defaultTtl;
        internal uint packetsReceived;
        internal uint receivedPacketsWithHeaderErrors; 
        internal uint receivedPacketsWithAddressErrors;
        internal uint packetsForwarded; 
        internal uint receivedPacketsWithUnknownProtocols; 
        internal uint receivedPacketsDiscarded;
        internal uint receivedPacketsDelivered; 
        internal uint packetOutputRequests;
        internal uint outputPacketRoutingDiscards;
        internal uint outputPacketsDiscarded;
        internal uint outputPacketsWithNoRoute; 
        internal uint packetReassemblyTimeout;
        internal uint packetsReassemblyRequired; 
        internal uint packetsReassembled; 
        internal uint packetsReassemblyFailed;
        internal uint packetsFragmented; 
        internal uint packetsFragmentFailed;
        internal uint packetsFragmentCreated;
        internal uint interfaces;
        internal uint ipAddresses; 
        internal uint routes;
    } 
 
    [StructLayout(LayoutKind.Sequential)]
    internal struct MibIcmpInfo { 
        internal MibIcmpStats inStats;
        internal MibIcmpStats outStats;
    }
 
    [StructLayout(LayoutKind.Sequential)]
    internal struct MibIcmpStats { 
        internal uint messages; 
        internal uint errors;
        internal uint destinationUnreachables; 
        internal uint timeExceeds;
        internal uint parameterProblems;
        internal uint sourceQuenches;
        internal uint redirects; 
        internal uint echoRequests;
        internal uint echoReplies; 
        internal uint timestampRequests; 
        internal uint timestampReplies;
        internal uint addressMaskRequests; 
        internal uint addressMaskReplies;
    }

    [StructLayout(LayoutKind.Sequential)] 
    internal struct MibIcmpInfoEx {
        internal MibIcmpStatsEx inStats; 
        internal MibIcmpStatsEx outStats; 
    }
 
    [StructLayout(LayoutKind.Sequential)]
    internal struct MibIcmpStatsEx {
        internal uint       dwMsgs;
        internal uint       dwErrors; 
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=256)]
        internal uint[]      rgdwTypeCount; 
    } 

    [StructLayout(LayoutKind.Sequential)] 
    internal struct MibTcpTable {
        internal uint numberOfEntries;
    }
 
    [StructLayout(LayoutKind.Sequential)]
    internal struct MibTcpRow { 
        internal TcpState  state; 
        internal uint  localAddr;
        internal byte  localPort1; 
        internal byte  localPort2;
        internal byte  localPort3;
        internal byte  localPort4;
        internal uint  remoteAddr; 
        internal byte  remotePort1;
        internal byte  remotePort2; 
        internal byte  remotePort3; 
        internal byte  remotePort4;
    } 

    [StructLayout(LayoutKind.Sequential)]
    internal struct MibUdpTable {
        internal uint numberOfEntries; 
    }
 
    [StructLayout(LayoutKind.Sequential)] 
    internal struct MibUdpRow {
        internal uint  localAddr; 
        internal byte  localPort1;
        internal byte  localPort2;
        internal byte  localPort3;
        internal byte  localPort4; 
    }
 
 
    [StructLayout(LayoutKind.Sequential)]
    internal struct IPOptions { 
        internal byte  ttl;
        internal byte  tos;
        internal byte  flags;
        internal byte  optionsSize; 
        internal IntPtr optionsData;
 
        internal IPOptions (PingOptions options) 
        {
            ttl = 128; 
            tos = 0;
            flags = 0;
            optionsSize = 0;
            optionsData = IntPtr.Zero; 

            if (options != null) { 
                this.ttl = (byte)options.Ttl; 

                if (options.DontFragment){ 
                    flags = 2;
                }
            }
        } 
    }
 
 
    [StructLayout(LayoutKind.Sequential)]
    internal struct IcmpEchoReply { 
        internal uint address;
        internal uint status;
        internal uint  roundTripTime;
        internal ushort dataSize; 
        internal ushort reserved;
        internal IntPtr data; 
        internal IPOptions options; 
        }
       /* 

    [StructLayout(LayoutKind.Sequential)]
    internal struct Ipv6Address {
        ushort sin6_port; 
        uint  sin6_flowinfo;
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=16)] 
        byte[] sin6_addr; 
        uint  sin6_scope_id;
    }    */ 

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    internal struct Ipv6Address {
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=6)] 
        internal byte[] Goo;
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=16)] 
        internal byte[] Address;    // Replying address. 
        internal uint ScopeID;
    } 
 		
    [StructLayout(LayoutKind.Sequential)]
     internal struct Icmp6EchoReply {
        internal Ipv6Address Address; 
        internal uint Status;               // Reply IP_STATUS.
        internal uint RoundTripTime; // RTT in milliseconds. 
        internal IntPtr data; 
        // internal IPOptions options;
        // internal IntPtr data; data os after tjos 
     }


       /* 

    [StructLayout(LayoutKind.Sequential)] 
    internal struct Ipv6Address { 
        ushort sin6_port;
        uint  sin6_flowinfo; 
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=16)]
        byte[] sin6_addr;
        uint  sin6_scope_id;
    }    */ 

    // Reply data follows this structure in memory. 
 
    /// <summary>
    ///   Wrapper for API's in iphlpapi.dll 
    /// </summary>

    [
    System.Security.SuppressUnmanagedCodeSecurityAttribute() 
    ]
    internal static class UnsafeNetInfoNativeMethods { 
 
        private const string IPHLPAPI = "iphlpapi.dll";
 
        [DllImport(IPHLPAPI)]
        internal extern static uint GetAdaptersInfo(SafeLocalFree pAdapterInfo,ref uint pOutBufLen);

        [DllImport(IPHLPAPI)] 
        internal extern static uint GetAdaptersAddresses(
            AddressFamily family, 
            uint flags, 
            IntPtr pReserved,
            SafeLocalFree adapterAddresses, 
            ref uint outBufLen);


        [DllImport(IPHLPAPI)] 
        internal extern static uint GetBestInterface(int ipAddress, out int index);
 
        /* 
        // Consider removing.
        [DllImport(IPHLPAPI)] 
        internal extern static uint GetBestInterfaceEx(byte[] ipAddress, out int index);
        */

        [DllImport(IPHLPAPI)] 
        internal extern static uint GetIfEntry(ref MibIfRow pIfRow);
 
        [DllImport(IPHLPAPI)] 
        internal extern static uint GetIpStatistics(out MibIpStats statistics);
 
        [DllImport(IPHLPAPI)]
        internal extern static uint GetIpStatisticsEx(out MibIpStats statistics, AddressFamily family);

        [DllImport(IPHLPAPI)] 
        internal extern static uint GetTcpStatistics(out MibTcpStats statistics);
 
        [DllImport(IPHLPAPI)] 
        internal extern static uint GetTcpStatisticsEx(out MibTcpStats statistics, AddressFamily family);
 
        [DllImport(IPHLPAPI)]
        internal extern static uint GetUdpStatistics(out MibUdpStats statistics);

        [DllImport(IPHLPAPI)] 
        internal extern static uint GetUdpStatisticsEx(out MibUdpStats statistics, AddressFamily family);
 
        [DllImport(IPHLPAPI)] 
        internal extern static uint GetIcmpStatistics(out MibIcmpInfo statistics);
 
        [DllImport(IPHLPAPI)]
        internal extern static uint GetIcmpStatisticsEx(out MibIcmpInfoEx statistics,AddressFamily family);

        [DllImport(IPHLPAPI)] 
        internal extern static uint GetTcpTable(SafeLocalFree pTcpTable,ref uint dwOutBufLen, bool order);
 
        [DllImport(IPHLPAPI)] 
        internal extern static uint GetUdpTable(SafeLocalFree pUdpTable,ref uint dwOutBufLen, bool order);
 
        // [DllImport(IPHLPAPI)]
        // internal extern static uint GetInterfaceInfo(IntPtr pIfTable,ref uint dwOutBufLen);

        // [DllImport(IPHLPAPI)] 
        // internal extern static uint GetIpAddrTable(SafeLocalFree pIpAddrTable, ref uint dwOutBufLen, bool order);
 
        [DllImport(IPHLPAPI)] 
        internal extern static uint GetNetworkParams(SafeLocalFree pFixedInfo,ref uint pOutBufLen);
 
        // [DllImport(IPHLPAPI)]
        // internal extern static uint GetNumberOfInterfaces(out uint pdwNumIf);

        [DllImport(IPHLPAPI)] 
        internal extern static uint GetPerAdapterInfo(uint IfIndex,SafeLocalFree pPerAdapterInfo,ref uint pOutBufLen);
 
        /* Consider Removing 
        [DllImport(IPHLPAPI)]
        unsafe internal extern static uint NotifyAddrChange(out IntPtr waithandle, [In] NativeOverlapped *overlapped); 
        */

        [DllImport(IPHLPAPI, SetLastError=true)]
        internal extern static SafeCloseIcmpHandle IcmpCreateFile(); 

        [DllImport (IPHLPAPI, SetLastError=true)] 
        internal extern static SafeCloseIcmpHandle Icmp6CreateFile (); 

        [DllImport (IPHLPAPI, SetLastError=true)] 
        internal extern static bool IcmpCloseHandle(IntPtr handle);

        // [DllImport(IPHLPAPI)]
        // internal extern static uint GetBestInterface(uint address4, out uint bestIfIndex); 

        [DllImport (IPHLPAPI, SetLastError=true)] 
        internal extern static uint IcmpSendEcho2 (SafeCloseIcmpHandle icmpHandle, SafeWaitHandle Event, IntPtr apcRoutine, IntPtr apcContext, 
            uint ipAddress, [In] SafeLocalFree data, ushort dataSize, ref IPOptions options, SafeLocalFree replyBuffer, uint replySize, uint timeout);
 
        [DllImport (IPHLPAPI, SetLastError=true)]
        internal extern static uint IcmpSendEcho2 (SafeCloseIcmpHandle icmpHandle, IntPtr Event, IntPtr apcRoutine, IntPtr apcContext,
            uint ipAddress, [In] SafeLocalFree data, ushort dataSize, ref IPOptions options, SafeLocalFree replyBuffer, uint replySize, uint timeout);
 
        [DllImport (IPHLPAPI, SetLastError=true)]
        internal extern static uint Icmp6SendEcho2 (SafeCloseIcmpHandle icmpHandle, SafeWaitHandle Event, IntPtr apcRoutine, IntPtr apcContext, 
            byte[] sourceSocketAddress, byte[] destSocketAddress, [In] SafeLocalFree data, ushort dataSize, ref IPOptions options, SafeLocalFree replyBuffer, uint replySize, uint timeout); 

        [DllImport (IPHLPAPI, SetLastError=true)] 
        internal extern static uint Icmp6SendEcho2 (SafeCloseIcmpHandle icmpHandle, IntPtr Event, IntPtr apcRoutine, IntPtr apcContext,
            byte[] sourceSocketAddress, byte[] destSocketAddress, [In] SafeLocalFree data, ushort dataSize, ref IPOptions options, SafeLocalFree replyBuffer, uint replySize, uint timeout);

        [DllImport (IPHLPAPI, SetLastError=true)] 
        internal unsafe extern static uint IcmpParseReplies (IntPtr replyBuffer, uint replySize);
 
        [DllImport (IPHLPAPI, SetLastError=true)] 
        internal unsafe extern static uint Icmp6ParseReplies (IntPtr replyBuffer, uint replySize);
 



//        [DllImport(IPHLPAPI)] 
//        internal extern static uint NotifyAddrChange(uint nullhandle,uint nulloverlapped);
 
//        [DllImport(IPHLPAPI)] 
//        internal extern static uint NotifyRouteChange(uint nullhandle,uint nulloverlapped);
    } 

    [
    System.Security.SuppressUnmanagedCodeSecurityAttribute()
    ] 
    internal static class UnsafeIcmpNativeMethods {
 
       private const string ICMP = "icmp.dll"; 

       [DllImport(ICMP, SetLastError=true)] 
       internal extern static SafeCloseIcmpHandle IcmpCreateFile();

       [DllImport (ICMP, SetLastError=true)]
       internal extern static bool IcmpCloseHandle(IntPtr icmpHandle); 

       [DllImport (ICMP, SetLastError=true)] 
       internal extern static uint IcmpSendEcho2 (SafeCloseIcmpHandle icmpHandle, SafeWaitHandle Event, IntPtr apcRoutine, IntPtr apcContext, 
       uint ipAddress, [In] SafeLocalFree data, ushort dataSize, ref IPOptions options, SafeLocalFree replyBuffer, uint replySize, uint timeout);
 
       [DllImport (ICMP, SetLastError=true)]
       internal extern static uint IcmpSendEcho2 (SafeCloseIcmpHandle icmpHandle, IntPtr Event, IntPtr apcRoutine, IntPtr apcContext,
       uint ipAddress, [In] SafeLocalFree data, ushort dataSize, ref IPOptions options, SafeLocalFree replyBuffer, uint replySize, uint timeout);
 
       [DllImport (ICMP, SetLastError=true)]
       internal unsafe extern static uint IcmpParseReplies (IntPtr replyBuffer, uint replySize); 
    } 

 
    [
    System.Security.SuppressUnmanagedCodeSecurityAttribute()
    ]
    internal static class UnsafeWinINetNativeMethods { 

       private const string WININET = "wininet.dll"; 
 
       [DllImport (WININET)]
       internal extern static bool InternetGetConnectedState(ref uint flags, uint dwReserved); 
    }
}

 

