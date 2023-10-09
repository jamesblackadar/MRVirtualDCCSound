using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MRVirtualDCCSound.Core
{
    /// <summary>
    /// Diesel Locomotives with Mechanical Transmissions
    /// </summary>
    public class MobileSFXDieselMech : MobileSFXBase, IMobileSFXBase
    {

        private bool motorRunning = false;
        int selectedGear = 0;
        int[] gearSpread;
        private DateTime _lastGearChange = DateTime.Now;

        public class Config
        {
            public int[] GearSpread { get; set; } = new int[] { 0, 70, 160, 255 };//todo read gears spread config in from config file (to support 2,3,4,5,etc speed diesels)
        }
        public MobileSFXDieselMech(MobileSFXBase item, ILogger logger) : base(item, logger)
        {
            gearSpread = new Config().GearSpread.OrderBy(a => a).ToArray();
        }
        float numerator = 0F;
        float denominator = 255F;
        CancellationTokenSource cts = new CancellationTokenSource();
        public override async void SetSpeed(byte speed)
        {
            if (_soundMotion == null) return;
            var start = DateTime.Now;
            var newSpeed = new SpeedState(speed, DirectionReversed, _lastSpeed);
            var GearChangedElapsedms = (int)(DateTime.Now.Subtract(_lastGearChange).TotalMilliseconds);
            if (newSpeed.State == SpeedState.Mode.NoChange && GearChangedElapsedms > 200)
            {
                var variation = _random.NextSingle() * 0.05F;
                _soundMotion.Volume = _soundMotion.Volume - variation;
                //return;
            }

            if (speed == 0 || newSpeed.SignedSpeedStep - _lastSpeed.SignedSpeedStep > 20)//20 could be the average gear spread.
            {
                cts.Cancel();
            }

            if (semaphoreLock.WaitOne(0, false))
            {
                try
                {
                    if (!motorRunning)
                    {
                        //start the motor!
                        _soundMotion.Play();
                        _soundMotion.Volume = 0.3F;
                        _soundMotion.Rate = 0.7F;
                        motorRunning = true;
                    }
                    if (newSpeed.State == SpeedState.Mode.Idle)
                    {
                        if (_lastSpeed.State != SpeedState.Mode.Idle)
                        {
                            //#region idleStart
                            ////motor on tickover
                            _soundMotion.Volume = 0.5F;
                            _soundMotion.Rate = 0.7F;
                            ////turn on idle with timer.
                            _soundIdle.Rate = 1;
                            _soundIdle.Volume = 0.7F;
                            _soundIdle.Play();
                            _idletimer.Enabled = true;
                            _idletimer.Start();
                            //#endregion
                        }
                    }
                    else
                    {

                        float rpmTempoMultiplier = 0.1F;//0 to 1;

                        #region gearchange
                        var gearIndex = Array.FindIndex(gearSpread, x => speed < x);//given array {0,80,180,255}1st element(aka gear) for 0 thru 80, 2nd for 80 thru 180, 3rd gear for 180 thru 255
                        if (gearIndex != -1)
                        {
                            if (gearIndex != selectedGear)
                            {
                                var token = cts.Token;
                                numerator = ((gearIndex - 1 < 0) ? 0 : gearSpread[gearIndex - 1]);
                                denominator = gearSpread[gearIndex] - ((gearIndex - 1 < 0) ? 0 : gearSpread[gearIndex - 1]);
                                selectedGear = gearIndex;
                                _lastGearChange = DateTime.Now;
                                if (IsDebug) Logger.LogDebug($">>>>Gear Change to:{selectedGear} Numerator:{numerator}Denom{denominator}");
                                _soundMotion.Volume = 0.9F;
                                _soundMotion.Rate = (newSpeed.State == SpeedState.Mode.Accelerating) ? 0.55F : 0.45F;
                                if (IsDebug) Logger.LogDebug($"{speed}clutch1:Gear{selectedGear}Tempo{_soundMotion.Rate}");
                                try
                                {
                                    await Task.Delay(150, token);
                                    if (speed == 0)
                                    {
                                        cts = new CancellationTokenSource(); return;
                                    }
                                    _soundMotion.Volume = 0.55F;
                                    _soundMotion.Rate = 0.48F;
                                    await Task.Delay(200, token);
                                    if (speed == 0)
                                    {
                                        cts = new CancellationTokenSource(); return;
                                    }
                                    _soundMotion.Volume = 0.6F;
                                    _soundMotion.Rate = (newSpeed.State == SpeedState.Mode.Accelerating) ? 0.55F : 0.45F;
                                    if (IsDebug) Logger.LogDebug($"{speed}clutch3:Gear{selectedGear}Tempo{_soundMotion.Rate}");
                                    await Task.Delay(350, token);
                                    if (speed == 0)
                                    {
                                        cts = new CancellationTokenSource(); return;
                                    }
                                }
                                catch (OperationCanceledException)
                                {

                                }
                            }
                        }
                        #endregion

                        #region InGearMotorRPM
                        rpmTempoMultiplier = (float)(speed - numerator) / denominator;//range from 0.3 to 0.9?
                        rpmTempoMultiplier = Math.Clamp(((rpmTempoMultiplier * 0.3F) + 0.85F), 0.85F, 1.3F);
                        //rpmTempoMultiplier = Math.Clamp(((rpmTempoMultiplier * 2.5F) + 0.05F), 0.8F, 2.4F);
                        if (IsDebug) Logger.LogDebug($">>>>TempoRate: {rpmTempoMultiplier}");
                        _soundMotion.Rate = rpmTempoMultiplier;

                        //clutch is out and locomotive is in gear.
                        //play sound at full volume and tempo of wheel RPM * gear ratio
                        //if accelerating play loudly etc?
                        //var RPMstep = Math.Min(30, RPM) / 30;//one when going fast; 0 when slow.
                        if (IsDebug) Logger.LogDebug("speed steps per second:" + newSpeed.SpeedStepsPerSecond);
                        var speedStepFraction = Math.Min(3, newSpeed.SpeedStepsPerSecond) / 4;
                        //_soundMotion.RateChange = (rpmTempoMultiplier * 2) + 0.5F;
                        // _soundMotion.RateChange = (rpmTempoMultiplier * 2) + 0.8F; //tempochange can be upto about 4 before we run into problems
                        // _soundMotion.RateChange = tempochange;
                        if (IsDebug) Logger.LogDebug($"Speed:{speed}\r\nGear:{selectedGear}\r\nRate:{_soundMotion.Rate}\r\nVolume:{_soundMotion.Volume}\r\nSppedSteps:{speedStepFraction}");
                        #endregion

                        #region Accel/Decel
                        if (newSpeed.State == SpeedState.Mode.Accelerating)
                        {
                            if (speed < 10)
                            {
                                _soundMotion.Volume = 0.4F;
                                _soundIdle.Volume = 0.4f;
                            }
                            else if (speed < 19)
                            {
                                _soundMotion.Volume = 0.4F + (speedStepFraction * 0.15F);
                                _soundIdle.Volume = 0.2f;
                            }
                            else if (speed > 18)
                            {
                                _soundMotion.Volume = 0.5F + (speedStepFraction * 0.2F);
                                _soundIdle.Stop();
                            }
                        }
                        else if (newSpeed.State == SpeedState.Mode.Deaccelerating)
                        {
                            _soundMotion.Volume = 0.5F + (speedStepFraction * 0.1F);
                            if (_lastSpeed.State == SpeedState.Mode.Accelerating)
                            {
                                _clankedRecently = false;
                            }
                            if (Coupled && newSpeed.SpeedStepsPerSecond > 20 && !_clankedRecently)
                            {
                                //if deacclerating rapidly play a buffer sound.
                                BufferClank();
                            }
                        }
                        #endregion
                    }
                }
                finally
                {
                    semaphoreLock.Release();
                    _lastSpeed = newSpeed;
                }
            }
        }

        internal override void IdleTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (_soundIdle != null)
            {
                if (_soundIdle.Volume < 0.3F)
                {
                    //once quiet, stop reducing the sound; We could change this to turn off entirely
                    //after some relatively lengthy time period.
                    _idletimer.Enabled = false;
                }
                else
                {
                    _soundIdle.Volume += -0.05F;//lower the volume
                    if (_soundMotion != null)
                    {
                        if (_soundMotion.Volume > 0.2F)
                        {
                            _soundMotion.Volume += -0.05F;//lower the volume
                        }
                    }
                }
            }
        }
    }
}



