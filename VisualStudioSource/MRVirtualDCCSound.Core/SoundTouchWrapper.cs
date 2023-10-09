namespace MRVirtualDCCSound.Core
{
    public class SoundTouchWrapper : IDisposable
    {
        private SoundProcessor _sp;
        private float _vol=1;
        private double _pitch=1;
        private double _tempo = 1;
        private double _rate = 1;
        private TimeSpan _length;
        private bool disposedValue;

        public SoundTouchWrapper(string wavFile, bool loop)
        {
            _sp = new SoundProcessor(wavFile);
            _sp.streamProcessor.Loop = loop;
            _length = _sp.streamProcessor.TotalTime;
        }
        public TimeSpan Length
        {
            get { return _length; }
        }

        public void Play()
        {
            _sp.Play();
        }
        public void Stop()
        {
            _sp.Stop();
        }
        public float Volume
        {
            get => _vol; set
            {
                _sp.streamProcessor.Volume = _vol = value < 0 ? 0 : value;
            }
        }
        //public double PitchSemitTones
        //{
        //    get => _pitch; set
        //    {
        //        _sp.streamProcessor.st.PitchSemiTones = _pitch = value;
        //    }
        //}
        /// <summary>
        /// 1 = normal speed.
        /// </summary>
        public double Tempo
        {
            get => _tempo; set
            {
                _sp.streamProcessor.st.Tempo = _tempo = value;
            }
        }

        /// <summary>
        /// 1 = normal speed.
        /// </summary>
        public double Rate
        {
            get => _rate; set
            {
                _sp.streamProcessor.st.Rate = _rate = value;
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _sp.streamProcessor.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DCCSoundProcessor()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}



