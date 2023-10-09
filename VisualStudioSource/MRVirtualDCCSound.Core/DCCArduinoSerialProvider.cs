using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.IO.Ports;

namespace MRVirtualDCCSound.Core
{
    public class DCCArduinoSerialProvider : IMobileEventProvider
    {
        private SerialConnectionHelper _serialInterface;
        public SerialConnectionHelper SerialConnectionHelper { 
            get=>_serialInterface; 
            set{ _serialInterface = value;
                if (_serialInterface.SerialPort != null)
                {
                    _serialInterface.SerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                    _serialInterface.SerialPort.ErrorReceived += new SerialErrorReceivedEventHandler(ErrorRecievedHandler);
                }
            } }
        ILogger _il;
        bool ildebug = false;
        bool ilinfo = false;
        bool ilerror = false;

        public event EventHandler<MobileEventArgs> MobileEventArgEmit;
        public DCCArduinoSerialProvider(SerialConnectionHelper serialInterface, ILogger<DCCArduinoSerialProvider> logger)
        {
            _il = logger;
            if (_il != null)
            {
                ildebug = _il.IsEnabled(LogLevel.Debug);
                ilinfo = _il.IsEnabled(LogLevel.Information);
                ilerror = _il.IsEnabled(LogLevel.Error);
                if (ilinfo) _il.LogInformation("Starting");
            }
            this.SerialConnectionHelper = serialInterface;
        }

        private string _lastCommand;

        private void ErrorRecievedHandler(object sender, SerialErrorReceivedEventArgs e)
        {
            if (ilerror) _il.LogError($"Serial error:{e.EventType}");
        }
        private async void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            byte[] array = new byte[sp.BytesToRead];
            int i = 0;
            while (sp.BytesToRead > 0 && i < array.Length)
            {
                var line = sp.ReadLine();
                if (line.StartsWith("Loc"))//only listen to interesting 'loc' aka DCCdecoder events.
                {
                    _lastCommand = line;// + "stuffthatwon'tmatch";
                    BuildMobileEventArgs(line);
                }
                i++;
            }
        }
        //Loc 3 Rev 19 Data 11 1001011
        //Loc 3 Forw 18 Data 11 1111010
        //Loc 6 F8-F5 101 Data 110 10110101
        //Acc Ext 117 Asp 11011001 Data 10000011 1011010
        private void BuildMobileEventArgs(string serialInput)
        {
            var tokens = serialInput.Split(' ');
            if (tokens.Length > 5)
            {
                if (tokens[0] == "Loc")
                {
                    var DCCAddress = Convert.ToUInt32(tokens[1]);
                    if (tokens[2] == "Forw")
                    {
                        OnMobileEvent(new MobileEventArgs(DCCAddress) { Command = MobileEventArgs.Commands.SPEED, Value = new MobileEventArgs.Speed((byte)(Convert.ToByte(tokens[3]) * 9), false) });
                    }
                    else if (tokens[2] == "Forw128")
                    {
                        OnMobileEvent(new MobileEventArgs(DCCAddress) { Command = MobileEventArgs.Commands.SPEED, Value = new MobileEventArgs.Speed((byte)(Convert.ToByte(tokens[3]) * 2), false) });
                    }
                    else if (tokens[2] == "Rev")
                    {
                        OnMobileEvent(new MobileEventArgs(DCCAddress) { Command = MobileEventArgs.Commands.SPEED, Value = new MobileEventArgs.Speed((byte)(Convert.ToByte(tokens[3]) * 9), true) });
                    }
                    else if (tokens[2] == "Rev128")
                    {
                        OnMobileEvent(new MobileEventArgs(DCCAddress) { Command = MobileEventArgs.Commands.SPEED, Value = new MobileEventArgs.Speed((byte)(Convert.ToByte(tokens[3]) * 2), true) });
                    }
                    else if (tokens[2] == "Stop")
                    {
                        OnMobileEvent(new MobileEventArgs(DCCAddress) { Command = MobileEventArgs.Commands.SPEED, Value = new MobileEventArgs.Speed(0, false) });
                    }
                    else if (tokens[3] == "F4-F1")
                    {
                        MobileEventArgs.FN effects = new MobileEventArgs.FN();
                        var fourthToken = tokens[4].PadLeft(8, '0');

                        if (fourthToken[7] == '1')//1st bit aka F1 aka bell
                        {
                            effects |= MobileEventArgs.FN.bell;
                        }
                        if (fourthToken[6] == '1')//2nd bit aka F2 aka horn
                        {
                            effects |= MobileEventArgs.FN.whistle;
                        }
                        if (fourthToken[5] == '1')//3rd bit aka F3
                        {
                            effects |= MobileEventArgs.FN.yardModeSwitcherModeHalfSpeed;
                        }
                        if (fourthToken[4] == '1')//4th bit aka F4 aka coupling?
                        {
                            effects |= MobileEventArgs.FN.Coupled;
                        }
                        if (fourthToken[3] == '1')//5th bit aka FL
                        {
                            effects |= MobileEventArgs.FN.bufferclank;
                        }
                        OnMobileEvent(new MobileEventArgs(DCCAddress) { Command = MobileEventArgs.Commands.FN, Value = effects });
                    }
                }
            }
        }

        // Wrap event invocations inside a protected virtual method
        // to allow derived classes to override the event invocation behavior
        protected virtual void OnMobileEvent(MobileEventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<MobileEventArgs> raiseEvent = MobileEventArgEmit;

            // Event will be null if there are no subscribers
            if (raiseEvent != null)
            {
                // Call to raise the event.
                raiseEvent(this, e);
            }
        }

        public async Task TestLoop()
        {

        }

        public Task Test(MobileEventArgs arg)
        {
            return Task.CompletedTask;
        }
    }
}



