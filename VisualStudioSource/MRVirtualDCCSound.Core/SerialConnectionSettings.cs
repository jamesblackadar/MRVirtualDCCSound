using System.IO.Ports;

namespace MRVirtualDCCSound.Core
{
    public class SerialConnectionSettings
    {
        public string? PortName { get; set; }
        public Parity Parity { get; set; }
        public Handshake Handshake { get; set; }
        public bool RtsEnabled { get; set; }
        public int Baud { get; set; }
        public int Databits { get; set; }
    }
}
