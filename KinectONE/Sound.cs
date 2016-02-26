using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.Kinect;

namespace KinectONE
{
    public class Sound
    {
        #region Method Private
        /// <summary>
        /// Number of samples captured from Kinect audio stream each millisecond.
        /// </summary>
        private const int _SamplesPerMillisecond = 16;

        /// <summary>
        /// Number of bytes in each Kinect audio stream sample (32-bit IEEE float).
        /// </summary>
        private const int _BytesPerSample = sizeof(float);

        /// <summary>
        /// Number of audio samples represented by each column of pixels in wave bitmap.
        /// </summary>
        private const int _SamplesPerColumn = 40;

        /// <summary>
        /// Minimum energy of audio to display (a negative number in dB value, where 0 dB is full scale)
        /// </summary>
        private const int _MinEnergy = -90;

        /// <summary>
        /// Width of bitmap that stores audio stream energy data ready for visualization.
        /// </summary>
        private const int _EnergyBitmapWidth = 780;

        /// <summary>
        /// Height of bitmap that stores audio stream energy data ready for visualization.
        /// </summary>
        private const int _EnergyBitmapHeight = 195;

        /// <summary>
        /// Bitmap that contains constructed visualization for audio stream energy, ready to
        /// be displayed. It is a 2-color bitmap with white as background color and blue as
        /// foreground color.
        /// </summary>
        private WriteableBitmap energyBitmap;
        //private readonly WriteableBitmap energyBitmap;


        /// <summary>
        /// Rectangle representing the entire energy bitmap area. Used when drawing background
        /// for energy visualization.
        /// </summary>
        private readonly Int32Rect fullEnergyRect = new Int32Rect(0, 0, _EnergyBitmapWidth, _EnergyBitmapHeight);

        /// <summary>
        /// Array of background-color pixels corresponding to an area equal to the size of whole energy bitmap.
        /// </summary>
        private readonly byte[] backgroundPixels = new byte[_EnergyBitmapWidth * _EnergyBitmapHeight];

        /// <summary>
        /// Will be allocated a buffer to hold a single sub frame of audio data read from audio stream.
        /// </summary>
        //public byte[] audioBuffer = null;
        private byte[] audioBuffer = null;

        /// <summary>
        /// Buffer used to store audio stream energy data as we read audio.
        /// We store 25% more energy values than we strictly need for visualization to allow for a smoother
        /// stream animation effect, since rendering happens on a different schedule with respect to audio
        /// capture.
        /// </summary>
        private readonly float[] energy = new float[(uint)(_EnergyBitmapWidth * 1.25)];

        /// <summary>
        /// Object for locking energy buffer to synchronize threads.
        /// </summary>
        private readonly object energyLock = new object();
        /// <summary>
        /// Reader for audio frames
        /// </summary>
        private AudioBeamFrameReader _audioReader = null;

        /// <summary>
        /// Last observed audio beam angle in radians, in the range [-pi/2, +pi/2]
        /// </summary>
        private float _beamAngle = 0;

        /// <summary>
        ///  Convert from radians to degrees for display purposes
        /// </summary>
        private float _beamAngleInDeg = 0;

        /// <summary>
        /// Last observed audio beam angle confidence, in the range [0, 1]
        /// </summary>
        private float beamAngleConfidence = 0;

        /// <summary>
        /// Array of foreground-color pixels corresponding to a line as long as the energy bitmap is tall.
        /// This gets re-used while constructing the energy visualization.
        /// </summary>
        private byte[] foregroundPixels;

        /// <summary>
        /// Sum of squares of audio samples being accumulated to compute the next energy value.
        /// </summary>
        private float accumulatedSquareSum;

        /// <summary>
        /// Number of audio samples accumulated so far to compute the next energy value.
        /// </summary>
        private int accumulatedSampleCount;

        /// <summary>
        /// Index of next element available in audio energy buffer.
        /// </summary>
        private int energyIndex;

        /// <summary>
        /// Number of newly calculated audio stream energy values that have not yet been
        /// displayed.
        /// </summary>
        private int newEnergyAvailable;

        /// <summary>
        /// Error between time slice we wanted to display and time slice that we ended up
        /// displaying, given that we have to display in integer pixels.
        /// </summary>
        private float energyError;

        /// <summary>
        /// Last time energy visualization was rendered to screen.
        /// </summary>
        private DateTime? lastEnergyRefreshTime;

        /// <summary>
        /// Index of first energy element that has never (yet) been displayed to screen.
        /// </summary>
        private int energyRefreshIndex;

        #endregion

        #region Public Methods
        public float BeamAngle
        {
            get { return _beamAngle; }
            set { _beamAngle = value; }
        }

        public float BeamAngleInDeg
        {
            get { return _beamAngleInDeg; }
            set { _beamAngleInDeg = value; }
        }

        public AudioBeamFrameReader AudioReader
        {
            get {return _audioReader;}
            set { _audioReader= value;}
        }

        public float BeamAngleConfidence
        {
            get { return beamAngleConfidence; }
            set { beamAngleConfidence = value; }
        }

        public byte[] AudioBuffer 
        {
            get { return audioBuffer; }
            set { }
        }

        public int BytesPerSample 
        {
            get { return _BytesPerSample; }
            set { }
        }
        public float AccumulatedSquareSum 
        {
            get { return accumulatedSquareSum; }
            set { accumulatedSquareSum = value; }
        }

        public int AccumulatedSampleCount
        {
            get { return accumulatedSampleCount; }
            set { accumulatedSampleCount = value; }
        }

        public int SamplesPerColumn 
        {
            get { return _SamplesPerColumn; }
            set { }
        }

        public float MinEnergy 
        {
            get { return _MinEnergy;}
            set { MinEnergy = value; }
        }

        public float[] Energy
        {
            get { return energy; }
            set { }
        }

        public int EnergyIndex 
        {
            get { return energyIndex; }
            set { energyIndex = value; }
        }

        public int NewEnergyAvailable
        {
            get { return newEnergyAvailable; }
            set { newEnergyAvailable = value; }
        }

        public byte[] ForegroundPixels 
        {
            get { return foregroundPixels; }
            set { foregroundPixels = value; }
        }

        public Int32Rect FullEnergyRect 
        {
            get { return fullEnergyRect;}
            set { }
        }
        public byte[] BackgroundPixels
        {
            get { return backgroundPixels;}
            set { }
        }

        public int EnergyBitmapWidth 
        {
            get { return _EnergyBitmapWidth; }
            set { }
        }

        public int EnergyBitmapHeight
        {
            get { return _EnergyBitmapHeight; }
            set { }
        }

        public WriteableBitmap EnergyBitmap
        {
            get {return energyBitmap; }
            set { energyBitmap = value; }
        }

        public float EnergyError 
        {
            get { return energyError; }
            set { energyError = value; }
        }

        public int EnergyRefreshIndex 
        {
            get { return energyRefreshIndex; }
            set { energyRefreshIndex = value; }
        }

        public int SamplesPerMillisecond 
        {
            get { return _SamplesPerMillisecond; }
            set { }
        }
        #endregion

        public WaitCallback callback = null;
        

        public void InitializeSound(KinectSensor _kinect, System.Windows.Media.Color color)
        {
            if (_kinect == null) return;

            if (_audioReader != null) return;

            // Get its audio source
            AudioSource audioSource = _kinect.AudioSource;

            // Allocate 1024 bytes to hold a single audio sub frame. Duration sub frame 
            // is 16 msec, the sample rate is 16khz, which means 256 samples per sub frame. 
            // With 4 bytes per sample, that gives us 1024 bytes.
            audioBuffer = new byte[audioSource.SubFrameLengthInBytes];

            // Open the reader for the audio frames
            _audioReader = audioSource.OpenReader();

            // Uncomment these two lines to overwrite the automatic mode of the audio beam.
            // It will change the beam mode to manual and set the desired beam angle.
            // In this example, point it straight forward.
            // Note that setting beam mode and beam angle will only work if the
            // application window is in the foreground.
            // Furthermore, setting these values is an asynchronous operation --
            // it may take a short period of time for the beam to adjust.
            /*
            audioSource.AudioBeams[0].AudioBeamMode = AudioBeamMode.Manual;
            audioSource.AudioBeams[0].BeamAngle = 0;
                */

            this.energyBitmap = new WriteableBitmap(EnergyBitmapWidth, EnergyBitmapHeight, 96, 96, PixelFormats.Indexed1, new BitmapPalette(new List<System.Windows.Media.Color> { Colors.Transparent, color }));

            // Initialize foreground pixels
            this.foregroundPixels = new byte[_EnergyBitmapHeight];
            for (int i = 0; i < this.foregroundPixels.Length; ++i)
            {
                this.foregroundPixels[i] = 0xff;
            }

            //this.waveDisplay.Source = this.energyBitmap;

            //CompositionTarget.Rendering += this.UpdateEnergy;


          


        }

        /// <summary>
        /// Handles the audio frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void OnAudioFrameArrived(object sender, AudioBeamFrameArrivedEventArgs e)
        {
            AudioBeamFrameReference frameReference = e.FrameReference;

            try
            {
                AudioBeamFrameList frameList = frameReference.AcquireBeamFrames();

                if (frameList != null)
                {
                    // AudioBeamFrameList is IDisposable
                    using (frameList)
                    {
                        // Only one audio beam is supported. Get the sub frame list for this beam
                        IReadOnlyList<AudioBeamSubFrame> subFrameList = frameList[0].SubFrames;

                        // Loop over all sub frames, extract audio buffer and beam information
                        foreach (AudioBeamSubFrame subFrame in subFrameList)
                        {
                            // Check if beam angle and/or confidence have changed
                            bool updateBeam = false;

                            if (subFrame.BeamAngle != BeamAngle)
                            {
                                BeamAngle = subFrame.BeamAngle;
                                updateBeam = true;
                            }

                            if (subFrame.BeamAngleConfidence != BeamAngleConfidence)
                            {
                                BeamAngleConfidence = subFrame.BeamAngleConfidence;
                                updateBeam = true;
                            }

                            if (updateBeam)
                            {
                                // Refresh display of audio beam
                                //this.AudioBeamChanged();

                                ThreadPool.QueueUserWorkItem(callback);
                            }

                            // Process audio buffer
                            subFrame.CopyFrameDataToArray(AudioBuffer);

                            for (int i = 0; i < AudioBuffer.Length; i += BytesPerSample)
                            {
                                // Extract the 32-bit IEEE float sample from the byte array
                                float audioSample = BitConverter.ToSingle(AudioBuffer, i);

                                AccumulatedSquareSum += audioSample * audioSample;
                                ++AccumulatedSampleCount;

                                if (AccumulatedSampleCount < SamplesPerColumn)
                                {
                                    continue;
                                }

                                float meanSquare = AccumulatedSquareSum / SamplesPerColumn;

                                if (meanSquare > 1.0f)
                                {
                                    // A loud audio source right next to the sensor may result in mean square values
                                    // greater than 1.0. Cap it at 1.0f for display purposes.
                                    meanSquare = 1.0f;
                                }

                                // Calculate energy in dB, in the range [MinEnergy, 0], where MinEnergy < 0
                                float energy = MinEnergy;

                                if (meanSquare > 0)
                                {
                                    energy = (float)(10.0 * Math.Log10(meanSquare));
                                }

                                lock (this.energyLock)
                                {
                                    // Normalize values to the range [0, 1] for display
                                    Energy[EnergyIndex] = (MinEnergy - energy) / MinEnergy;
                                    EnergyIndex = (EnergyIndex + 1) % Energy.Length;
                                    ++NewEnergyAvailable;
                                }

                                AccumulatedSquareSum = 0;
                                AccumulatedSampleCount = 0;

                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore if the frame is no longer available
            }
        }


  
    }
}
