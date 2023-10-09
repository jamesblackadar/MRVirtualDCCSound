namespace MRVirtualDCCSound.Core
{
    /// <summary>
    /// Concrete observer of CommandArgs (events that drive sound effects)
    /// </summary>
    public class MobileCollectionObserver : IObserver<MobileEventArgs>

    {
        public MobileCollectionObserver()
        {
            Decoders = new MobileCollection<MobileSFXBase>();
        }

        public MobileCollection<MobileSFXBase> Decoders { get; set; }
        public virtual void Subscribe(IObservable<MobileEventArgs> provider)
        {
            unsubscriber = provider.Subscribe(this);
        }

        public virtual void Unsubscribe()
        {
            unsubscriber.Dispose();
        }

        public void OnCompleted()
        {
            //nothing to do here.
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(MobileEventArgs arg)
        {
            if (arg == null) return;
            var dccDecoder = GetDecoder(arg.Address);
            if (dccDecoder != null)
            {
                //we are listening for this decoder
                switch (arg.Command)
                {
                    case MobileEventArgs.Commands.SPEED:
                        var speed = arg.Value as MobileEventArgs.Speed;
                        if (speed != null)
                        {
                            dccDecoder.DirectionReversed = speed.Reversed;
                            //dccDecoder.LastSpeedStep = speed.Steps;
                            dccDecoder.SetSpeed(speed.Steps);
                        }
                        break;
                    case MobileEventArgs.Commands.FN:
                        var effects = (MobileEventArgs.FN)arg.Value;
                        dccDecoder.Coupled = effects.HasFlag(MobileEventArgs.FN.Coupled);
                        dccDecoder.HalfSpeedSteps = effects.HasFlag(MobileEventArgs.FN.yardModeSwitcherModeHalfSpeed);
                        if (effects.HasFlag(MobileEventArgs.FN.whistle)) dccDecoder.BlowWhistle();
                        if (effects.HasFlag(MobileEventArgs.FN.bufferclank)) dccDecoder.BufferClank();
                        if (effects.HasFlag(MobileEventArgs.FN.flangesqueal)) dccDecoder.FlangeSqueal();
                        {
                            Console.WriteLine("flangesqueal");
                        }
                        break;
                }
            }
        }

        private IMobileSFXBase? GetDecoder(uint address)
        {
            try
            {
                return Decoders[Decoders.GetIndexFor(address)];
            }
            catch
            {
                return null;
            }
        }
        private IDisposable unsubscriber;
    }
}



