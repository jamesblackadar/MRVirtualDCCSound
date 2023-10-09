namespace MRVirtualDCCSound.Core
{
    public class MobileEventArgs:EventArgs
    {
        public MobileEventArgs(uint address)
        {
            Address = address;
        }
        public uint Address { get; set; }
        public Commands Command { get; set; }
        public object Value { get; set; } = 0;
        public enum Commands
        {
            SPEED,
            FN
        }
        [Flags]
        public enum FN
        {
            none = 0,
            bell = 1,
            whistle = 2,
            bufferclank = 4,
            flangesqueal = 8,
            yardModeSwitcherModeHalfSpeed =16,
            Coupled = 32
        }
        public class Speed
        {
            public Speed(byte steps, bool reversed)
            {
                Steps = steps; Reversed = reversed;
            }
            public byte Steps { get; internal set; }
            public bool Reversed { get; internal set; }
        }
    }
}



