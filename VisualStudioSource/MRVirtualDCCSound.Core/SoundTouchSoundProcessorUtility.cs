//////////////////////////////////////////////////////////////////////////////
using NAudio.Wave;
///
/// WaveStream processor class for manipulating audio stream in C# with 
/// SoundTouch library.
/// 
/// This module uses NAudio library for C# audio file input / output
/// 
/// Author        : Copyright (c) Olli Parviainen
/// Author e-mail : oparviai 'at' iki.fi
/// SoundTouch WWW: http://www.surina.net/soundtouch
///
////////////////////////////////////////////////////////////////////////////////
//
// License for this source code file: Microsoft Public License(Ms-PL)
//
////////////////////////////////////////////////////////////////////////////////

using SoundTouch;

namespace MRVirtualDCCSound.Core
{
    /// <summary>
    /// Helper class that allow writing status texts to the host application
    /// </summary>
    public class StatusMessage
    {
        /// <summary>
        /// Handler for status message events. Subscribe this from the host application
        /// </summary>
        public static event EventHandler<string> StatusEvent;

        /// <summary>
        /// Pass a status message to the host application
        /// </summary>
        public static void Write(string msg)
        {
            if (StatusEvent != null)
            {
                StatusEvent(null, msg);
            }
        }
    }

    /// <summary>
    /// NAudio WaveStream class for processing audio stream with SoundTouch effects
    /// </summary>
    public class LoopingWaveStreamProcessor : WaveStream
    {
        private WaveChannel32 inputStr;
        public SoundTouchProcessor st;

        private byte[] bytebuffer = new byte[4096];
        private float[] floatbuffer = new float[1024];
        bool endReached = false;

        //public float Volume { set { inputStr.Volume = value; } }
        public float Volume { get => inputStr.Volume; set { inputStr.Volume = value; } }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="input">WaveChannel32 stream used for processor stream input</param>
        public LoopingWaveStreamProcessor(WaveChannel32 input)
        {
            inputStr = input;
            st = new SoundTouchProcessor();
            st.Channels = (int)input.WaveFormat.Channels;
            st.SampleRate = (int)input.WaveFormat.SampleRate;
        }

        public bool Loop { get; set; }

        /// <summary>
        /// True if end of stream reached
        /// </summary>
        public bool EndReached
        {
            get { return endReached; }
        }


        public override long Length
        {
            get
            {
                return inputStr.Length;
            }
        }


        public override long Position
        {
            get
            {
                return inputStr.Position;
            }

            set
            {
                inputStr.Position = value;
            }
        }


        public override WaveFormat WaveFormat
        {
            get
            {
                return inputStr.WaveFormat;
            }
        }

        /// <summary>
        /// Overridden Read function that returns samples processed with SoundTouch. Returns data in same format as
        /// WaveChannel32 i.e. stereo float samples.
        /// </summary>
        /// <param name="buffer">Buffer where to return sample data</param>
        /// <param name="offset">Offset from beginning of the buffer</param>
        /// <param name="count">Number of bytes to return</param>
        /// <returns>Number of bytes copied to buffer</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                // Iterate until enough samples available for output:
                // - read samples from input stream
                // - put samples to SoundStretch processor
                while (st.AvailableSamples < count)
                {
                    int nbytes = inputStr.Read(bytebuffer, 0, bytebuffer.Length);
                    if (nbytes == 0)
                    {
                        // end of stream. flush final samples from SoundTouch buffers to output
                        if (endReached == false && !Loop)
                        {
                            endReached = true;  // do only once to avoid continuous flushing
                            st.Flush();
                            //add this for looping:
                            // break;
                        }
                        //remove for looping:break;
                        if (!Loop)
                        {
                            break;
                        }
                        else
                        {
                            //add
                            inputStr.Position = 0;
                        }

                    }

                    // binary copy data from "byte[]" to "float[]" buffer
                    Buffer.BlockCopy(bytebuffer, 0, floatbuffer, 0, nbytes);
                    st.PutSamples(floatbuffer, (int)(nbytes / 8));
                }

                // ensure that buffer is large enough to receive desired amount of data out
                if (floatbuffer.Length < count / 4)
                {
                    floatbuffer = new float[count / 4];
                }
                // get processed output samples from SoundTouch
                int numsamples = (int)st.ReceiveSamples(floatbuffer, (int)(count / 8));
                // binary copy data from "float[]" to "byte[]" buffer
                Buffer.BlockCopy(floatbuffer, 0, buffer, offset, numsamples * 8);
                return numsamples * 8;  // number of bytes
            }
            catch (Exception exp)
            {
                StatusMessage.Write("exception in WaveStreamProcessor.Read: " + exp.Message);
                return 0;
            }
        }

        /// <summary>
        /// Clear the internal processor buffers. Call this if seeking or rewinding to new position within the stream.
        /// </summary>
        public void Clear()
        {
            st.Clear();
            endReached = false;
        }
    }



    /// <summary>
    /// Class that opens & plays MP3 file and allows real-time audio processing with SoundTouch
    /// while playing
    /// </summary>
    public class SoundProcessor
    {
        MediaFoundationReader mediaFile;
        NAudio.Wave.DirectSoundOut waveOut;
        //WaveOut waveOut;
        public LoopingWaveStreamProcessor streamProcessor;

        public SoundProcessor(string wavFile)
        {
            OpenMp3File(wavFile);
        }

        /// <summary>
        /// Start / resume playback
        /// </summary>
        /// <returns>true if succesful, false if audio file not open</returns>
        public bool Play()
        {
            if (waveOut == null) return false;


            if (waveOut.PlaybackState != PlaybackState.Playing)
            {
                waveOut.Play();
            }
            return true;
        }


        /// <summary>
        /// Pause playback
        /// </summary>
        /// <returns>true if succesful, false if audio not playing</returns>
        public bool Pause()
        {
            if (waveOut == null) return false;

            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Stop();
                return true;
            }
            return false;
        }


        /// <summary>
        /// Stop playback
        /// </summary>
        /// <returns>true if succesful, false if audio file not open</returns>
        public bool Stop()
        {
            if (waveOut == null) return false;

            waveOut.Stop();//todo thread abort is not supported on this platform

            mediaFile.Position = 0;

            streamProcessor.Clear();
            return true;
        }



        /// <summary>
        /// Event for "playback stopped" event. 'bool' argument is true if playback has reached end of stream.
        /// </summary>
        public event EventHandler<bool> PlaybackStopped;


        /// <summary>
        /// Proxy event handler for receiving playback stoped event from WaveOut
        /// </summary>
        protected void EventHandler_stopped(object sender, StoppedEventArgs args)
        {
            bool isEnd = streamProcessor.EndReached;
            if (isEnd)
            {
                Stop();
            }
            if (PlaybackStopped != null)
            {
                PlaybackStopped(sender, isEnd);
            }
        }


        /// <summary>
        /// Open MP3 file
        /// </summary>
        /// <param name="filePath">Path to file to open</param>
        /// <returns>true if successful</returns>
        private bool OpenMp3File(string filePath)
        {
            try
            {
                //mp3File = new Mp3FileReader(filePath);
                mediaFile = new MediaFoundationReader(filePath);
                WaveChannel32 inputStream = new WaveChannel32(mediaFile);
                inputStream.PadWithZeroes = false;  // don't pad, otherwise the stream never ends
                streamProcessor = new LoopingWaveStreamProcessor(inputStream);

                waveOut = new NAudio.Wave.DirectSoundOut(100);

                waveOut.Init(streamProcessor);  // inputStream);
                waveOut.PlaybackStopped += EventHandler_stopped;

                StatusMessage.Write("Opened file " + filePath);
                return true;
            }
            catch (Exception exp)
            {
                // Error in opening file
                waveOut = null;
                StatusMessage.Write("Can't open file: " + exp.Message);
                return false;
            }
            return false;

        }
    }
}
