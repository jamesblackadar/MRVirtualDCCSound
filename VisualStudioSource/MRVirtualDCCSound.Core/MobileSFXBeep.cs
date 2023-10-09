using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Linq.Expressions;

namespace MRVirtualDCCSound.Core
{
    /// <summary>
    /// Test Class that emits console beeps...no wav sounds required.
    /// </summary>
    public class MobileSFXBeep : MobileSFXBase, IMobileSFXBase
    {
        CancellationTokenSource tokenSource = new CancellationTokenSource();
        #region Constructors
        public MobileSFXBeep(MobileSFXBase item, ILogger logger) : base(item, logger)
        {
        }
        #endregion
        public override void SetSpeed(byte speed)
        {

            tokenSource.Cancel();
            var tempTokenSource = new CancellationTokenSource();
            var ct = tempTokenSource.Token;

            int freq;
            //37 to 32k
            freq = Convert.ToInt32(speed) * 40 + 1000;
            try
            {
                Task.Run(() => { Console.Beep(freq, 5000); tokenSource = tempTokenSource; }, ct);
            }
            catch (OperationCanceledException)
            {
            }
        }
        public override void BlowWhistle()
        {
            Console.Beep(8000, 500);
        }

        public override void CoupleUp()
        {
            Console.Beep(5000, 100);
        }
    }
}



