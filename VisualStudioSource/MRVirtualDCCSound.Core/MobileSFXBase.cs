using Microsoft.Extensions.Logging;
using System.ComponentModel;


namespace MRVirtualDCCSound.Core
{
    public class MobileSFXBase : MobileSFXAbstract, IMobileSFXBase
    { 
       // public ILogger Logger { get; set; } = null;
        //internal readonly bool IsDebug = false;
       // internal ILogger Logger;


        #region Constructors
        public MobileSFXBase():base()
        {
        }
        public MobileSFXBase(uint address, uint maxRPM) : base(address,maxRPM)
        {
        }

        public MobileSFXBase(MobileSFXBase item,ILogger logger):base(item,logger)
        {
            //this.Id = item.Id;
            //this.MaxRPM = item.MaxRPM;
            //this.Notes = item.Notes;
            //this.SoundsFolder = item.SoundsFolder;
            //this.Logger = item.Logger;
            //IsDebug = this.Logger!=null&&this.Logger.IsEnabled(logLevel: LogLevel.Debug);
        }

        #endregion

        public override void BlowWhistle()//F@?
        {
            if (IsDebug) Logger.LogDebug("Loco{0} Whistle!", this.Id);
            PlaySoundEffect(_wavWhistles, ref _soundWhistle, false);
        }
        public override void BufferClank()
        {
            if (IsDebug) Logger.LogDebug("Loco{0} BufferClank!", this.Id);
            _clankedRecently = true;
            PlaySoundEffect(_wavBuffer, ref _soundBuffer, false);
        }

        public override void CoupleUp()
        {
            if(IsDebug) Logger.LogDebug("Loco{0} Couple!", this.Id);
            _clankedRecently = true;
            PlaySoundEffect(_wavCouple, ref _soundCouple, false);
        }

        public override void FlangeSqueal()
        {
            if (IsDebug) Logger.LogDebug("Loco{0} FlangeSqueal!", this.Id);
            PlaySoundEffect(_wavWhistles, ref _soundWhistle, false);
        }


        public override void SetSpeed(byte speed)
        {
            if (IsDebug) Logger.LogWarning("Loco{0} SetSpeed Method Was Not Overridden", this.Id);
        }

        public override async Task TestLoopAsync()
        {
            for (int i = 0; i < 5; i++)
            {
                if (IsDebug) Logger.LogDebug("Loco{0} TestIdle", this.Id);
                SetSpeed(0);
                await Task.Delay(500);
            }
            for (int i = 0; i < 255; i = i + 5)
            {
                SetSpeed((byte)(i));

                if (i == 15 | i == 50 | i == 75 | i == 125 | i == 200)
                {
                    BlowWhistle();
                }
                if (i == 3 | i == 30 | i == 100 | i == 175 | i == 220)
                {
                    BufferClank();
                }
                await Task.Delay(100 + (i));
            }

            if (IsDebug) Logger.LogDebug("Loco{0} Loop1 of 4", this.Id);
            for (int i = 255; i > -1; i = i - 15)
            {
                //await Task.Delay(10 * i + 100);
                await Task.Delay(30 + (i * 5));
                SetSpeed((byte)(i));
            }
            if (IsDebug) Logger.LogDebug("Loco{0} Loop2 of 4", this.Id);
            for (int i = 0; i < 5; i++)
            {
                if (IsDebug) Logger.LogDebug("Loco{0} Idling", this.Id);
                await Task.Delay(500);
                SetSpeed(0);
            }
            if (IsDebug) Logger.LogDebug("Loco{0} waiting...", this.Id);
            await Task.Delay(10000);
            for (int i = 1; i < 120; i++)
            {
                SetSpeed((byte)(i * 2));
                if (i == 15 | i == 50 | i == 75 | i == 125 | i == 200)
                {
                    BlowWhistle();
                }
                await Task.Delay(1000 - (i * 2));
            }
            if (IsDebug) Logger.LogDebug("Loco{0} Loop 3 of 4...", this.Id);
            for (int i = 60; i > -1; i--)
            {
                SetSpeed((byte)(i * 4));
                await Task.Delay(17 * i + 100);
            }
            if (IsDebug) Logger.LogDebug("Loco{0} loop 4 of 4...complete!", this.Id);
        }
    }
}



