using System.IO.Ports;

namespace MRVirtualDCCSound.Core
{
    public class SerialConnectionHelper : IDisposable
    {
        public SerialPort? SerialPort { get; private set; }
        public SerialConnectionSettings SerialConnectionSettings
        {
            get
            {
                if (SerialPort == null)
                {
                    return new SerialConnectionSettings();
                }
                else
                {
                    return new SerialConnectionSettings()
                    {
                        Baud = SerialPort.BaudRate,
                        Databits = SerialPort.DataBits,
                        Handshake = SerialPort.Handshake,
                        Parity = SerialPort.Parity,
                        PortName = SerialPort.PortName,
                        RtsEnabled = SerialPort.RtsEnable
                    };
                }
            }
        }
        public void Initialize(string portName, int baudrate, Parity parity, int databits, Handshake handshake = Handshake.None, bool rtsEnable = false)
        {
            if (SerialPort != null) SerialPort.Close();
            SerialPort = new SerialPort(portName, baudrate, parity, databits)
            {
                Handshake = handshake,
            };
            SerialPort.Open();
        }

        public void Initialize(SerialConnectionSettings connectionSettings)
        {
            Initialize(connectionSettings.PortName ?? "", connectionSettings.Baud, connectionSettings.Parity, connectionSettings.Databits, connectionSettings.Handshake, connectionSettings.RtsEnabled);
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SerialPort?.Close();//closes and disposes the internal serial port stream
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~SerialInterface()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
