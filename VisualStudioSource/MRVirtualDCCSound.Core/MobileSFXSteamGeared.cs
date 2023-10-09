using Microsoft.Extensions.Logging;
using System.ComponentModel;


namespace MRVirtualDCCSound.Core
{
    /// <summary>
    /// Sentinel Shunters and such.
    /// </summary>
    public class MobileDecoderSteamGeared : MobileSFXBase, IMobileSFXBase
    {
        #region Constructors
        public MobileDecoderSteamGeared(MobileSFXBase item, ILogger logger) : base(item, logger)
        {
        }

        #endregion



        public override void SetSpeed(byte speed)
        {

            if (_soundMotion == null) return;
            var newSpeed = new SpeedState(speed, DirectionReversed, _lastSpeed);
            if (newSpeed.State == SpeedState.Mode.NoChange) return;
            if (semaphoreLock.WaitOne(50))
            {
                try
                {
                    float RPM = Math.Max(0.5F, HalfSpeedSteps ? speed * _speedConversionFactor / 2 : speed * _speedConversionFactor);
                    //RPM = RPM * 2;
                    //float RPM = (speed * 30 -(speed * 28.75F)+10) * _speedConversionFactor;
                    //if (HalfSpeedSteps) RPM = RPM / 2;
                    //Some real-world examples of RPMs
                    //RPM LNER A4, 6'8" wheel @ 286RPM for 134MPH.
                    //RPM LMS 3F 4'8" drivers @ 180RPM for 60MPH and 90RPM for 30MPH.
                    //RPM LYR Pug with 3'drivers @ 140RPM for 30MPH.
                    //In conclusion 280RPM is speed record territory, 120RPM is quite fast; 50RPM is comfortable.
                    var speedStepFraction = Math.Min(30, newSpeed.SpeedStepsPerSecond) / 30;//30 speedsteps per second is rapid decel. rapid accel would be 1; slow would be zero.
                                                                                            //double tempo = Convert.ToInt32(RPM * 10);//this should be in range of 0 to 1000? certainly 600///eventually we can let soundfile specify exhaustbeats per wav file and RPM and Beats per RPM. Also need support for CV6 
                                                                                            //int pitch = Convert.ToInt32((Math.Min(100, RPM) / 100 * -9) + 5);//this should be in the range of -12 to 12. negative numbers are higher pitch.-4 to +5 is reasonable range.
                    var duration = _soundMotion.Length.TotalSeconds;
                    var halfRPMdurationConversion = duration / 30;
                    double tempo = Convert.ToDouble(RPM * halfRPMdurationConversion);
                    var RPMstep = Math.Min(30, RPM) / 30;//one when going fast; 0 when slow.
                                                         //4 beats per RPM. example duration of 4 beat wav file is 2000ms.
                                                         //2000ms is per RPM...in other words 60,000ms/2000ms is how much we need to stretch.
                                                         //15rpm with a 4'9" driver is 2mph. 15rpm in 2cyl loco is 60bpm.
                    tempo = Math.Max(0.6F, Math.Min(tempo, 9));
                    _soundMotion.Tempo = tempo;
                    if (newSpeed.State == SpeedState.Mode.Idle && _lastSpeed.State != SpeedState.Mode.Idle)
                    {
                        #region idleStart
                        //turn off Chuff
                        _soundMotion.Stop();
                        //turn on idle with timer.
                        _soundIdle.Tempo = 1;
                        _soundIdle.Volume = 0.7F;
                        _soundIdle.Play();
                        _idletimer.Enabled = true;
                        _idletimer.Start();
                        #endregion
                    }
                    else if (newSpeed.State == SpeedState.Mode.Accelerating)
                    {
                        #region accelerating
                        if (_lastSpeed.State != SpeedState.Mode.Accelerating)
                        {
                            //turn on Chuff but quietly for now.
                            /// _soundMotion.PitchSemitTones = 1;
                            _soundMotion.Volume = 0F;
                            _soundMotion.Play();
                            //Turn on Idle too                       
                            _soundIdle.Tempo = 1;
                            _soundIdle.Volume = 0.7F;
                            _soundIdle.Play();
                            //reset
                            _idletimer.Stop();
                            _clankedRecently = false;
                        }
                        //Chuff depending on speed step and rate
                        if (Coupled)
                        {
                            if (newSpeed.SpeedStepsPerSecond > 20 && !_clankedRecently)
                            {
                                BufferClank();
                            }
                            if (RPM > 15)
                            {
                                _soundMotion.Volume = 0.6F + (speedStepFraction * 0.2F);
                                _soundIdle.Stop();
                            }
                            else
                            {
                                _soundIdle.Volume = 0.7F - (0.5F * RPMstep);
                                if (RPM > 2)
                                {
                                    _soundMotion.Volume = 0.4F + (speedStepFraction * 0.4F);
                                }
                                else
                                {
                                    _soundMotion.Volume = 0;
                                }
                            }
                        }
                        else
                        {
                            if (RPM > 30)
                            {
                                _soundIdle.Stop();
                                _soundMotion.Volume = 0.6F + (speedStepFraction * 0.2F);
                            }
                            else if (RPM > 3)
                            {
                                _soundIdle.Volume = 0.7F - (0.5F * RPMstep);
                                _soundMotion.Volume = 0.4F + (speedStepFraction * 0.4F);
                                //play chuffs loudly
                            }
                            else
                            {
                                _soundMotion.Volume = 0;
                                //RPM<3 just play idle sound.
                            }
                        }
                        #endregion
                    }
                    else if (newSpeed.State == SpeedState.Mode.Deaccelerating)
                    {
                        #region decel

                        _soundIdle.Volume = 0.7F - (RPMstep * 0.5F);
                        // _soundMotion.Volume = Math.Max(0.05F, _soundMotion.Volume - (speedStepFraction * 0.4F));
                        // _soundMotion.PitchSemitTones = RPMstep * 4;
                        if (_lastSpeed.State == SpeedState.Mode.Accelerating)
                        {
                            //turn on idle                         
                            _soundIdle.Play();
                            _clankedRecently = false;
                        }
                        //  if (RPM < 15)
                        //{
                        _soundMotion.Volume = Math.Min(_soundMotion.Volume, (RPMstep * 0.2F) - (speedStepFraction * 0.15F));
                        //}
                        // else
                        //{
                        //    _soundMotion.Volume = Math.Min(_soundMotion.Volume, (RPMstep * 0.3F) - (speedStepFraction * 0.3F));
                        // }
                        if (Coupled && newSpeed.SpeedStepsPerSecond > 20 && !_clankedRecently)
                        {
                            //if deacclerating rapidly play a buffer sound.
                            BufferClank();
                        }
                    }
                    _lastSpeed = newSpeed;
                    #endregion
                }
                finally
                {
                    semaphoreLock.Release();
                }
            }
        }
    }
}