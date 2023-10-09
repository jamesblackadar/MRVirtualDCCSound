using Microsoft.Extensions.Logging;
using System.ComponentModel;


namespace MRVirtualDCCSound.Core
{
    public abstract class MobileSFXAbstract : IMobileSFXBase
    { 
        //internal string _soundsfolder, _notes;
        internal readonly Random _random = new Random(DateTime.Now.Millisecond);
        public uint MaxRPM { get; set; }
        internal readonly List<string> _wavWhistles = new List<string>();
        internal readonly List<string> _wavBuffer = new List<string>();
        internal readonly List<string> _wavCouple = new List<string>();
        internal SoundProcessor _soundWhistle;
        internal SoundTouchWrapper _soundMotion;
        internal SoundTouchWrapper _soundIdle;
        internal SoundProcessor _soundBuffer;
        internal SoundProcessor _soundCouple;
        internal SpeedState _lastSpeed = new SpeedState(SpeedState.Mode.NoChange, 0, 0, false, DateTime.Now.AddSeconds(-5));
        internal Semaphore semaphoreLock = new Semaphore(1, 1);
        internal bool _clankedRecently;
        internal bool _isCoupled = false;
        internal float _speedConversionFactor;
        internal readonly System.Timers.Timer _idletimer;
        internal uint _id;
        public ILogger Logger = null;
        internal bool IsDebug = false;

       
        public uint Id { get; set; }
        public string Notes { get; set; }
        public string SoundsFolder { get; set; }
        public byte LastSpeedStep
        {
            get { return _lastSpeed.SpeedStep; }
            internal set { SetSpeed(value); }
        }
        public bool DirectionReversed { get; set; }
        public bool HalfSpeedSteps { get; set; }
        
        public bool Coupled
        {
            get => _isCoupled;
            set
            {
                _isCoupled = value;
                CoupleUp();
            }
        }

        //internal ILogger _logger;


        #region Constructors
        public MobileSFXAbstract()
        {
            _idletimer = new System.Timers.Timer(5000)
            {
                AutoReset = true
            };
            _idletimer.Elapsed += IdleTimer_Elapsed;
        }
        public MobileSFXAbstract(uint address, uint maxRPM) : this()
        {
            Id = address;
            MaxRPM = maxRPM;
        }

        public MobileSFXAbstract(MobileSFXAbstract item,ILogger logger = null):this()
        {
            this.Id = item.Id;
            this.MaxRPM = item.MaxRPM;
            this.Notes = item.Notes;
            this.SoundsFolder = item.SoundsFolder;
            this.Logger = logger;
            IsDebug = this.Logger != null && this.Logger.IsEnabled(logLevel: LogLevel.Information);
        }

        #endregion
        public void InitializeSound()
        {
            if (!Directory.Exists(SoundsFolder)) return;
            string name;
            foreach (var item in Directory.EnumerateFiles(SoundsFolder, "*.wav"))
            {
                name = Path.GetFileName(item);
                if (name.StartsWith("motion", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (_soundMotion != null) _soundMotion.Stop();
                    _soundMotion = new SoundTouchWrapper(item, true);
                }
                else if (name.StartsWith("whistle", StringComparison.InvariantCultureIgnoreCase))
                {
                    _wavWhistles.Add(item);
                }
                else if (name.StartsWith("idle", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (_soundIdle != null) _soundIdle.Stop();
                    _soundIdle = new SoundTouchWrapper(item, true);
                }
                else if (name.StartsWith("buffer", StringComparison.InvariantCultureIgnoreCase))
                {
                    _wavBuffer.Add(item);
                }
                else if (name.StartsWith("couple", StringComparison.InvariantCultureIgnoreCase))
                {
                    _wavBuffer.Add(item);
                }
            }
            _speedConversionFactor = (float)MaxRPM / byte.MaxValue;
           // if (_soundIdle == null || _soundMotion == null) throw new NullReferenceException($"Idle and Motion Wav files are required for this loco{this.Id}");
        }
        public virtual void BlowWhistle()//F@?
        {
        }
        public virtual void BufferClank()
        {
        }

        public virtual void CoupleUp()
        {}

        public virtual void FlangeSqueal()
        {}


        public virtual void SetSpeed(byte speed)
        {}

        internal void PlaySoundEffect(List<string> wavFiles, ref SoundProcessor soundProcessor, bool Loop)
        {

            if (wavFiles.Any())
            {
                if (soundProcessor != null) soundProcessor.Stop();//stop playing previous whistle sound if still running.
                if (wavFiles.Count > 1)
                {
                    soundProcessor = new SoundProcessor(wavFiles[_random.Next(wavFiles.Count)]);
                }
                else
                {
                    if (soundProcessor == null) soundProcessor = new SoundProcessor(wavFiles[0]);
                }
                soundProcessor.streamProcessor.Loop = Loop;
                int pitch = Convert.ToInt32(LastSpeedStep / 60) + _random.Next(0, 2);///this is last speed...not current speed hmm.
                //higher pitch sound if travelling fast
                soundProcessor.streamProcessor.st.PitchSemiTones = pitch;
                soundProcessor.streamProcessor.st.TempoChange = _random.Next(0, 40) - 20;
                soundProcessor.streamProcessor.Volume = 0.9F;
                soundProcessor.Play();
            }
        }


        public void OnNext(MobileEventArgs arg)
        {

            //we are listening for this decoder
            switch (arg.Command)
            {
                case MobileEventArgs.Commands.SPEED:
                    var speed = arg.Value as MobileEventArgs.Speed;
                    if (speed != null)
                    {
                        DirectionReversed = speed.Reversed;
                        //dccDecoder.LastSpeedStep = speed.Steps;
                        SetSpeed(speed.Steps);
                    }
                    break;
                case MobileEventArgs.Commands.FN:
                    var effects = (MobileEventArgs.FN)arg.Value;
                    Coupled = effects.HasFlag(MobileEventArgs.FN.Coupled);
                    HalfSpeedSteps = effects.HasFlag(MobileEventArgs.FN.yardModeSwitcherModeHalfSpeed);
                    if (effects.HasFlag(MobileEventArgs.FN.whistle)) BlowWhistle();
                    if (effects.HasFlag(MobileEventArgs.FN.bufferclank)) BufferClank();
                    if (effects.HasFlag(MobileEventArgs.FN.flangesqueal)) FlangeSqueal();
                    break;
            }

        }

        /// <summary>
        /// Timer to slowly reduce the volume of idle locomotives. 
        /// In future, maybe we should turnoff/reset/dispose entirely until wanted again (actively used on layout) 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal virtual void IdleTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (_soundIdle != null)
            {
                _soundIdle.Volume += -0.05F;//lower the volume
                if (_soundIdle.Volume < 0.15F)
                {
                    //once quiet, stop reducing the sound; We could change this to turn off entirely
                    //after some relatively lengthy time period.
                    _idletimer.Enabled = false;
                }
            }
        }

        internal float GetRPM(byte speed)
        {
            return Math.Max(0.5F, HalfSpeedSteps ? speed * _speedConversionFactor / 2 : speed * _speedConversionFactor);
        }

        public virtual async Task TestLoopAsync()
        {
            throw new NotImplementedException();
        }       
    }
}



