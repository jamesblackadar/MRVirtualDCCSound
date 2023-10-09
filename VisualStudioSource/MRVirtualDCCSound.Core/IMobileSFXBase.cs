using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MRVirtualDCCSound.Core
{
    public interface IMobileSFXBase
    {
        bool Coupled { get; set; }
        bool DirectionReversed { get; set; }
        bool HalfSpeedSteps { get; set; }
        uint Id { get; set; }
        byte LastSpeedStep { get; }
        uint MaxRPM { get; set; }
        string Notes { get; set; }
        string SoundsFolder { get; set; }
        //ILogger Logger { get; set; }

        //event PropertyChangedEventHandler PropertyChanged;

        void BlowWhistle();
        void BufferClank();
        void CoupleUp();
        void FlangeSqueal();
        void InitializeSound();
        void OnNext(MobileEventArgs e);
        void SetSpeed(byte speed);
        Task TestLoopAsync();
    }
}