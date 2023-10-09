using System.IO.Ports;

namespace MRVirtualDCCSound.Core
{
    public class GlobalSettings
    {
        public string SoundsFolder { get; set; } = @"c:\dcc\sounds";
        public IEnumerable<string> SoundsFolders { get {return System.IO.Directory.EnumerateDirectories(SoundsFolder).ToList(); } }
        public MRVirtualDCCSound.Core.SerialConnectionSettings ConnectionSettings { get; set; }

        public static SerialConnectionSettings GetDefaultSerialConnectionSettings()
        {
            return new SerialConnectionSettings()
            {
                PortName = @"COM1",
                Baud = 38400,
                Parity = Parity.None,
                Handshake = Handshake.None,
                Databits = 8,
                RtsEnabled = true
            };
        }
    }
}
