 

    /// <summary><para>
    ///    Provides support for ip configuation information and statistics.
    ///</para></summary> 
    ///
namespace System.Net.NetworkInformation { 
    using System.Net.Sockets; 
    using System;
    using System.ComponentModel; 


    /// <summary>IP statistics</summary>
    internal class SystemIPv4InterfaceStatistics:IPv4InterfaceStatistics { 

        MibIfRow ifRow = new MibIfRow(); 
 
        private SystemIPv4InterfaceStatistics(){ }
        internal SystemIPv4InterfaceStatistics(long index){ 
            GetIfEntry(index);
        }

        public override long OutputQueueLength{get {return ifRow.dwOutQLen;}} 
        public override long BytesSent{get {return ifRow.dwOutOctets;}}
        public override long BytesReceived{get {return ifRow.dwInOctets;}} 
        public override long UnicastPacketsSent{get {return ifRow.dwOutUcastPkts;}} 
        public override long UnicastPacketsReceived{get { return ifRow.dwInUcastPkts;}}
        public override long NonUnicastPacketsSent{get { return ifRow.dwOutNUcastPkts;}} 
        public override long NonUnicastPacketsReceived{get { return ifRow.dwInNUcastPkts;}}
        public override long IncomingPacketsDiscarded{get { return ifRow.dwInDiscards;}}
        public override long OutgoingPacketsDiscarded{get { return ifRow.dwOutDiscards;}}
        public override long IncomingPacketsWithErrors{get { return ifRow.dwInErrors;}} 
        public override long OutgoingPacketsWithErrors{get { return ifRow.dwOutErrors;}}
        public override long IncomingUnknownProtocolPackets{get { return ifRow.dwInUnknownProtos;}} 
        internal long Mtu{get { return ifRow.dwMtu;}} 
        internal OperationalStatus OperationalStatus{
            get{ 
                switch (ifRow.operStatus) {
                case OldOperationalStatus.NonOperational:
                    return OperationalStatus.Down;
                case OldOperationalStatus.Unreachable: 
                    return OperationalStatus.Down;
                case OldOperationalStatus.Disconnected: 
                    return OperationalStatus.Dormant; 
                case OldOperationalStatus.Connecting:
                    return OperationalStatus.Dormant; 
                case OldOperationalStatus.Connected:
                    return OperationalStatus.Up;
                case OldOperationalStatus.Operational:
                    return OperationalStatus.Up; 
                }
                //state unknow 
                return OperationalStatus.Unknown; 
            }
        } 
        internal long Speed{get { return ifRow.dwSpeed;}}

        //This method is used to get information for ipv4 specific interfaces
        //we should only call this the first time one of the properties 
        //are accessed.
        void GetIfEntry(long index) { 
            if (index == 0 ) 
                return;
 
            ifRow.dwIndex = (uint)index;
            uint result = UnsafeNetInfoNativeMethods.GetIfEntry(ref ifRow);
            if (result != IpHelperErrors.Success) {
                throw new NetworkInformationException((int)result); 
            }
        } 
    } 
}
 

 

    /// <summary><para>
    ///    Provides support for ip configuation information and statistics.
    ///</para></summary> 
    ///
namespace System.Net.NetworkInformation { 
    using System.Net.Sockets; 
    using System;
    using System.ComponentModel; 


    /// <summary>IP statistics</summary>
    internal class SystemIPv4InterfaceStatistics:IPv4InterfaceStatistics { 

        MibIfRow ifRow = new MibIfRow(); 
 
        private SystemIPv4InterfaceStatistics(){ }
        internal SystemIPv4InterfaceStatistics(long index){ 
            GetIfEntry(index);
        }

        public override long OutputQueueLength{get {return ifRow.dwOutQLen;}} 
        public override long BytesSent{get {return ifRow.dwOutOctets;}}
        public override long BytesReceived{get {return ifRow.dwInOctets;}} 
        public override long UnicastPacketsSent{get {return ifRow.dwOutUcastPkts;}} 
        public override long UnicastPacketsReceived{get { return ifRow.dwInUcastPkts;}}
        public override long NonUnicastPacketsSent{get { return ifRow.dwOutNUcastPkts;}} 
        public override long NonUnicastPacketsReceived{get { return ifRow.dwInNUcastPkts;}}
        public override long IncomingPacketsDiscarded{get { return ifRow.dwInDiscards;}}
        public override long OutgoingPacketsDiscarded{get { return ifRow.dwOutDiscards;}}
        public override long IncomingPacketsWithErrors{get { return ifRow.dwInErrors;}} 
        public override long OutgoingPacketsWithErrors{get { return ifRow.dwOutErrors;}}
        public override long IncomingUnknownProtocolPackets{get { return ifRow.dwInUnknownProtos;}} 
        internal long Mtu{get { return ifRow.dwMtu;}} 
        internal OperationalStatus OperationalStatus{
            get{ 
                switch (ifRow.operStatus) {
                case OldOperationalStatus.NonOperational:
                    return OperationalStatus.Down;
                case OldOperationalStatus.Unreachable: 
                    return OperationalStatus.Down;
                case OldOperationalStatus.Disconnected: 
                    return OperationalStatus.Dormant; 
                case OldOperationalStatus.Connecting:
                    return OperationalStatus.Dormant; 
                case OldOperationalStatus.Connected:
                    return OperationalStatus.Up;
                case OldOperationalStatus.Operational:
                    return OperationalStatus.Up; 
                }
                //state unknow 
                return OperationalStatus.Unknown; 
            }
        } 
        internal long Speed{get { return ifRow.dwSpeed;}}

        //This method is used to get information for ipv4 specific interfaces
        //we should only call this the first time one of the properties 
        //are accessed.
        void GetIfEntry(long index) { 
            if (index == 0 ) 
                return;
 
            ifRow.dwIndex = (uint)index;
            uint result = UnsafeNetInfoNativeMethods.GetIfEntry(ref ifRow);
            if (result != IpHelperErrors.Success) {
                throw new NetworkInformationException((int)result); 
            }
        } 
    } 
}
 

