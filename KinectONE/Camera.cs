using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

using System.Windows.Controls;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;

namespace KinectONE
{
    public class Camera 
    {
        #region def 
      
        private KinectSensor _kinect = null;
        private readonly int _bytePerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;   
        private ColorFrameReader _colorReader = null;
        private DepthFrameReader _depthReader = null;   
        private InfraredFrameReader _infraReader = null;
        private BodyFrameReader _bodyReader = null;    
        private byte[] _colorPixels = null;

        /// <summary>
        /// Array of depth pixels used for the output
        /// </summary>
        private byte[] _depthPixels = null;

        /// <summary>
        /// Array of infrared pixels used for the output
        /// </summary>
        private byte[] _infraPixels = null;

        /// <summary>
        /// Array of depth values
        /// </summary>
        private ushort[] _depthData = null;

        /// <summary>
        /// Array of infrared data
        /// </summary>
        private ushort[] _infraData = null;

      
        private Body[] _bodies = null;
        private WriteableBitmap _colorBitmap = null;

        /// <summary>
        /// Color WriteableBitmap linked to our UI
        /// </summary>
        private WriteableBitmap _depthBitmap = null;

        /// <summary>
        /// Infrared WriteableBitmap linked to our UI
        /// </summary>
        private WriteableBitmap _infraBitmap = null;
        
        private EventWaitHandle frameReady = null;
        private IntPtr _pinnedImageBuffer;
        private int bodyCount;
        private FaceFrameSource[] faceFrameSources = null;
        private FaceFrameReader[] faceFrameReaders = null;
        private FaceFrameResult[] faceFrameResults = null;

        #endregion

        #region Public Methods

        /// <summary>
        /// Representation of the Kinect Sensor
        /// </summary>
        public KinectSensor Sensor
        {
            get { return _kinect; }
            set { }
        }
   
        /// <summary>
        /// Color WriteableBitmap linked to our UI
        /// </summary>
        public WriteableBitmap ColorImageBitmap
        {
            get { return _colorBitmap; }
            set { _colorBitmap = value; }
        }

        /// <summary>
        ///  An event for synchronizing threads
        /// </summary>
        public EventWaitHandle FrameReady
        {
            get { return frameReady; }
            set { frameReady = value; }
        }
 
        /// <summary>
        /// 
        /// </summary>
        public IntPtr PinnedImageBuffer
        {
            get { return _pinnedImageBuffer; }
            set { _pinnedImageBuffer = value; }
        }

        /// <summary>
        /// All tracked bodies
        /// </summary>
        public Body[] Bodies
        {
            get { return _bodies; }
            set { }
        }

        /// <summary>
        /// FrameReader for our depth output
        /// </summary>
        public DepthFrameReader DepthReader 
        {
            get { return _depthReader; }
            set { _depthReader = value; }
        }

        /// <summary>
        /// FrameReader for our coloroutput
        /// </summary>
        public ColorFrameReader ColorReader
        {
            get { return _colorReader; }
            set { _colorReader = value; }
        }

        /// <summary>
        /// FrameReader for our infrared output
        /// </summary>
        public InfraredFrameReader InfraReader
        {
            get { return _infraReader; }
            set { _infraReader = value; }
        }


        /// <summary>
        /// FrameReader for our body output
        /// </summary>
        public BodyFrameReader BodyReader
        {
            get { return _bodyReader; }
            set { _bodyReader = value; }
        }

        /// <summary>
        /// Face frame readers
        /// </summary>
        public FaceFrameReader[] FaceFrameReaders
        {
            get { return faceFrameReaders; }
            set { faceFrameReaders = value; }
        }

        /// <summary>
        /// Face frame sources
        /// </summary>
        public FaceFrameSource[] FaceFrameSources 
        {
            get { return faceFrameSources; }
            set { faceFrameSources = value; }
        }

        /// <summary>
        /// Storage for face frame results
        /// </summary>
        public FaceFrameResult[] FaceFrameResults 
        {
            get { return faceFrameResults; }
            set { faceFrameResults = value; }
        }

        /// <summary>
        /// Array of color pixels
        /// </summary>
        public byte[] ColorPixels
        {
            get { return _colorPixels; }
            set { _colorPixels = value; }
        }

        /// <summary>
        /// Size fo the RGB pixel in bitmap
        /// </summary>
        public int BytePerPixel 
        {
            get { return _bytePerPixel; }
            set { }
        }

        /// <summary>
        /// Number of bodies tracked
        /// </summary>
        public int BodyCount 
        {
            get { return bodyCount; }
            set { }
        }
        #endregion

        /// <summary>
        /// Initialize Kinect Sensor
        /// </summary>
        public void InitializeKinect()
        {
            // Get first Kinect
            _kinect = KinectSensor.GetDefault();

            if (_kinect == null) return;

            // Open connection
            _kinect.Open();

        }

        /// <summary>
        /// Initialize Kinect Camera
        /// </summary>
        ///  <param name="source"></param>
        public void InitializeCamera(Image source)
        {
            if (_kinect == null) return;

            if (_colorReader != null) return;

            // Get frame description for the color output
            FrameDescription desc = _kinect.ColorFrameSource.FrameDescription;

            // Get the framereader for Color
            _colorReader = _kinect.ColorFrameSource.OpenReader();

            // Allocate pixel array
            _colorPixels = new byte[desc.Width * desc.Height * _bytePerPixel];

            // Create new WriteableBitmap
            _colorBitmap = new WriteableBitmap(desc.Width, desc.Height, 96, 96, PixelFormats.Bgr32, null);

            // Link WBMP to UI
           if(source!=null)
                source.Source = _colorBitmap;

            frameReady = new EventWaitHandle(false, EventResetMode.ManualReset);

            int size = 4 * ColorImageBitmap.PixelWidth * ColorImageBitmap.PixelHeight;
            PinnedImageBuffer = Marshal.AllocHGlobal(size);



        }

        /// <summary>
        /// Initialize Kinect Depth
        /// </summary>
        /// <param name="source"></param>
        public void InitializeDepth(Image source)
        {
            if (_kinect == null) return;

            if (_depthReader != null) return;
            // Get frame description for the color output
            FrameDescription desc = _kinect.DepthFrameSource.FrameDescription;

            // Get the framereader for Color
            _depthReader = _kinect.DepthFrameSource.OpenReader();

            // Allocate pixel array
            _depthData = new ushort[desc.Width * desc.Height];
            _depthPixels = new byte[desc.Width * desc.Height * _bytePerPixel];

            // Create new WriteableBitmap
            _depthBitmap = new WriteableBitmap(desc.Width, desc.Height, 96, 96, PixelFormats.Bgr32, null);

            if (source != null)
                source.Source = _depthBitmap;


        }

        /// <summary>
        /// Initialize Kinect Infrared
        /// </summary>
        /// <param name="source"></param>
        public void InitializeInfrared(Image source)
        {
            if (_kinect == null) return;

            if (_infraReader != null) return;

            // Get frame description for the color output
            FrameDescription desc = _kinect.InfraredFrameSource.FrameDescription;

            // Get the framereader for Color
            _infraReader = _kinect.InfraredFrameSource.OpenReader();

            // Allocate pixel array
            _infraData = new ushort[desc.Width * desc.Height];
            _infraPixels = new byte[desc.Width * desc.Height * _bytePerPixel];

            // Create new WriteableBitmap
            _infraBitmap = new WriteableBitmap(desc.Width, desc.Height, 96, 96, PixelFormats.Bgr32, null);

            if (source != null)
                source.Source = _infraBitmap;

        }

        /// <summary>
        /// Initialize Body Tracking
        /// </summary>
        public void InitializeBody()
        {
            if (_kinect == null) return;

            if (_bodyReader != null) return;

            // Allocate Bodies array
            _bodies = new Body[_kinect.BodyFrameSource.BodyCount];

            // Open reader
            _bodyReader = _kinect.BodyFrameSource.OpenReader();

            this.bodyCount = this._kinect.BodyFrameSource.BodyCount;


        }

        public void InitializeFace() 
        {
            if (_kinect == null) return;

            //if (_bodyReader != null) return;

            if (faceFrameReaders != null) return;

            // set the maximum number of bodies that would be tracked by Kinect
            this.bodyCount = _kinect.BodyFrameSource.BodyCount;

            // specify the required face frame results
            FaceFrameFeatures faceFrameFeatures =
                FaceFrameFeatures.BoundingBoxInColorSpace
                | FaceFrameFeatures.PointsInColorSpace
                | FaceFrameFeatures.RotationOrientation
                | FaceFrameFeatures.FaceEngagement
                | FaceFrameFeatures.Glasses
                | FaceFrameFeatures.Happy
                | FaceFrameFeatures.LeftEyeClosed
                | FaceFrameFeatures.RightEyeClosed
                | FaceFrameFeatures.LookingAway
                | FaceFrameFeatures.MouthMoved
                | FaceFrameFeatures.MouthOpen;


            // create a face frame source + reader to track each face in the FOV
            this.faceFrameSources = new FaceFrameSource[this.bodyCount];
            this.faceFrameReaders = new FaceFrameReader[this.bodyCount];
            for (int i = 0; i < this.bodyCount; i++)
            {
                // create the face frame source with the required face frame features and an initial tracking Id of 0
                this.faceFrameSources[i] = new FaceFrameSource(this._kinect, 0, faceFrameFeatures);

                // open the corresponding reader
                this.faceFrameReaders[i] = this.faceFrameSources[i].OpenReader();
            }

            // allocate storage to store face frame results for each face in the FOV
            this.faceFrameResults = new FaceFrameResult[this.bodyCount];
        }
      

        /// <summary>
        /// Process the depth frames and update UI
        /// </summary>
        public void OnDepthFrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            DepthFrameReference refer = e.FrameReference;

            if (refer == null) return;

            DepthFrame frame = refer.AcquireFrame();

            if (frame == null) return;

            using (frame)
            {
                FrameDescription frameDesc = frame.FrameDescription;

                if (((frameDesc.Width * frameDesc.Height) == _depthData.Length) && (frameDesc.Width == _depthBitmap.PixelWidth) && (frameDesc.Height == _depthBitmap.PixelHeight))
                {
                    // Copy depth frames
                    frame.CopyFrameDataToArray(_depthData);

                    // Get min & max depth
                    ushort minDepth = frame.DepthMinReliableDistance;
                    ushort maxDepth = frame.DepthMaxReliableDistance;

                    // Adjust visualisation
                    int colorPixelIndex = 0;
                    for (int i = 0; i < _depthData.Length; ++i)
                    {
                        // Get depth value
                        ushort depth = _depthData[i];

                        if (depth == 0)
                        {
                            _depthPixels[colorPixelIndex++] = 41;
                            _depthPixels[colorPixelIndex++] = 239;
                            _depthPixels[colorPixelIndex++] = 242;
                        }
                        else if (depth < minDepth || depth > maxDepth)
                        {
                            _depthPixels[colorPixelIndex++] = 25;
                            _depthPixels[colorPixelIndex++] = 0;
                            _depthPixels[colorPixelIndex++] = 255;
                        }
                        else
                        {
                            double gray = (Math.Floor((double)depth / 250) * 12.75);

                            _depthPixels[colorPixelIndex++] = (byte)gray;
                            _depthPixels[colorPixelIndex++] = (byte)gray;
                            _depthPixels[colorPixelIndex++] = (byte)gray;
                        }

                        // Increment
                        ++colorPixelIndex;
                    }

                    // Copy output to bitmap
                    _depthBitmap.WritePixels(
                            new Int32Rect(0, 0, frameDesc.Width, frameDesc.Height),
                            _depthPixels,
                            frameDesc.Width * _bytePerPixel,
                            0);
                }
            }
        }

        /// <summary>
        /// Process the infrared frames and update UI
        /// </summary>
        public void OnInfraredFrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            // Reference to infrared frame
            InfraredFrameReference refer = e.FrameReference;

            if (refer == null) return;

            // Get infrared frame
            InfraredFrame frame = refer.AcquireFrame();

            if (frame == null) return;

            // Process it
            using (frame)
            {
                // Get the description
                FrameDescription frameDesc = frame.FrameDescription;

                if (((frameDesc.Width * frameDesc.Height) == _infraData.Length) && (frameDesc.Width == _infraBitmap.PixelWidth) && (frameDesc.Height == _infraBitmap.PixelHeight))
                {
                    // Copy data
                    frame.CopyFrameDataToArray(_infraData);

                    int colorPixelIndex = 0;

                    for (int i = 0; i < _infraData.Length; ++i)
                    {
                        // Get infrared value
                        ushort ir = _infraData[i];

                        // Bitshift
                        byte intensity = (byte)(ir >> 8);

                        // Assign infrared intensity
                        _infraPixels[colorPixelIndex++] = intensity;
                        _infraPixels[colorPixelIndex++] = intensity;
                        _infraPixels[colorPixelIndex++] = intensity;

                        ++colorPixelIndex;
                    }

                    // Copy output to bitmap
                    _infraBitmap.WritePixels(
                            new Int32Rect(0, 0, frameDesc.Width, frameDesc.Height),
                            _infraPixels,
                            frameDesc.Width * _bytePerPixel,
                            0);
                }
            }
        }

        /// <summary>
        /// Process the infrared frames and update UI
        /// </summary>
        public void OnColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // Get the reference to the color frame
            ColorFrameReference colorRef = e.FrameReference;

            if (colorRef == null) return;

            // Acquire frame for specific reference
            ColorFrame frame = colorRef.AcquireFrame();

            // It's possible that we skipped a frame or it is already gone
            if (frame == null) return;


            //SALIENCY
            uint size = Convert.ToUInt32(frame.FrameDescription.Height * frame.FrameDescription.Width * 4);
            frame.CopyConvertedFrameDataToIntPtr(PinnedImageBuffer, size, ColorImageFormat.Bgra);

            using (frame)
            {


                // Get frame description
                FrameDescription frameDesc = frame.FrameDescription;

                // Check if width/height matches
                if (frameDesc.Width == _colorBitmap.PixelWidth && frameDesc.Height == _colorBitmap.PixelHeight)
                {
                   

                    // Copy data to array based on image format
                    if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
                        frame.CopyRawFrameDataToArray(_colorPixels);
                    else 
                        frame.CopyConvertedFrameDataToArray(_colorPixels, ColorImageFormat.Bgra);

                    // Copy output to bitmap
                    _colorBitmap.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate()
                    {
                        _colorBitmap.WritePixels(
                               new Int32Rect(0, 0, frameDesc.Width, frameDesc.Height),
                               _colorPixels,
                               frameDesc.Width * _bytePerPixel,
                               0);
                   
                    }));
                   
                }

                frameReady.Set();

            }
        }

        /// <summary>
        /// Process the body-frames and draw joints
        /// </summary>
        public void OnBodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {

            // Get frame reference
            BodyFrameReference refer = e.FrameReference;

            if (refer == null) return;

            // Get body frame
            BodyFrame frame = refer.AcquireFrame();

            if (frame == null) return;

            using (frame)
            {
                // Aquire body data
                frame.GetAndRefreshBodyData(_bodies);

   
                // Loop all bodies
                foreach (Body body in _bodies)
                {
                    // Only process tracked bodies
                    if (body.IsTracked)
                    {
                       
                    }
                }


            }
        }

        /// <summary>
        /// Handles the face frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void OnFaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame != null)
                {
                    // get the index of the face source from the face source array
                    int index = this.GetFaceSourceIndex(faceFrame.FaceFrameSource);

                    // check if this face frame has valid face frame results
                    if (this.ValidateFaceBoxAndPoints(faceFrame.FaceFrameResult))
                    {
                        // store this face frame result to draw later
                        this.faceFrameResults[index] = faceFrame.FaceFrameResult;
                    }
                    else
                    {
                        // indicates that the latest face frame result from this reader is invalid
                        this.faceFrameResults[index] = null;
                    }
                }
            }
        }


        /// <summary>
        /// Returns the index of the face frame source
        /// </summary>
        /// <param name="faceFrameSource">the face frame source</param>
        /// <returns>the index of the face source in the face source array</returns>
        private int GetFaceSourceIndex(FaceFrameSource faceFrameSource)
        {
            int index = -1;

            for (int i = 0; i < this.bodyCount; i++)
            {
                if (this.faceFrameSources[i] == faceFrameSource)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        /// <summary>
        /// Validates face bounding box and face points to be within screen space
        /// </summary>
        /// <param name="faceResult">the face frame result containing face box and points</param>
        /// <returns>success or failure</returns>
        private bool ValidateFaceBoxAndPoints(FaceFrameResult faceResult)
        {
            bool isFaceValid = faceResult != null;

            if (isFaceValid)
            {
                var faceBox = faceResult.FaceBoundingBoxInColorSpace;
                if (faceBox != null)
                {
                    // check if we have a valid rectangle within the bounds of the screen space
                    isFaceValid = (faceBox.Right - faceBox.Left) > 0 &&
                                  (faceBox.Bottom - faceBox.Top) > 0 &&
                                  faceBox.Right <= this.ColorImageBitmap.PixelWidth &&
                                  faceBox.Bottom <= this.ColorImageBitmap.PixelHeight;

                    if (isFaceValid)
                    {
                        var facePoints = faceResult.FacePointsInColorSpace;
                        if (facePoints != null)
                        {
                            foreach (PointF pointF in facePoints.Values)
                            {
                                // check if we have a valid face point within the bounds of the screen space
                                bool isFacePointValid = pointF.X > 0.0f &&
                                                        pointF.Y > 0.0f &&
                                                        pointF.X < this.ColorImageBitmap.PixelWidth &&
                                                        pointF.Y < this.ColorImageBitmap.PixelHeight;

                                if (!isFacePointValid)
                                {
                                    isFaceValid = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return isFaceValid;
        }

        public Boolean WaitOne()
        {
            return frameReady.WaitOne();
        }
    }
}
