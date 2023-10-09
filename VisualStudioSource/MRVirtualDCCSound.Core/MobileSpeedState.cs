namespace MRVirtualDCCSound.Core
{
    public class SpeedState
    {
        public SpeedState(Mode state, byte speedStep, float speedStepsperSecond, bool reversed, DateTime timestamp)
        {
            State = state;
            SpeedStep = speedStep;
            Timestamp = timestamp;
            SpeedStepsPerSecond = speedStepsperSecond;
            Reversed = reversed;
        }
        public SpeedState(byte speedStep, bool reversed, SpeedState lastSpeed)
        {
            Reversed = reversed;
            Timestamp = DateTime.Now;
            SpeedStep = speedStep;


            if (speedStep == 0)
            {
                State = Mode.Idle;
            }
            else if (lastSpeed.SignedSpeedStep == this.SignedSpeedStep)
            {
                State = Mode.NoChange;
            }
            else if (lastSpeed.Reversed != this.Reversed)
            {
                State = Mode.Accelerating;
            }
            else if (this.SpeedStep > lastSpeed.SpeedStep)
            {
                State = Mode.Accelerating;
            }
            else
            {
                State = Mode.Deaccelerating;
            }
            SecondsSinceLast = (this.Timestamp - lastSpeed.Timestamp).TotalSeconds;
            SpeedStepsPerSecond = (float)(Math.Abs((this.SignedSpeedStep - lastSpeed.SignedSpeedStep)) / SecondsSinceLast);
        }
        public bool Reversed { get; internal set; }
        public Mode State { get; internal set; }
        public byte SpeedStep { get; internal set; }
        public int SignedSpeedStep => Reversed ? -SpeedStep : SpeedStep;
        public float SpeedStepsPerSecond { get; internal set; }
        public DateTime Timestamp { get; internal set; }
        public double SecondsSinceLast { get; internal set; }
        public enum Mode
        {
            NoChange,
            Idle,
            Accelerating,
            Deaccelerating
        }
    }
}



