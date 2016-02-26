using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

using System.Windows.Controls;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;

namespace KinectONE
{
    public class KinectOne
    {

        private Camera _camera = new Camera();

        private Sound _audio = new Sound();

        private System.Drawing.Bitmap _colorImageBitmap = null;

        private System.Windows.Media.Brush[] skeletonBrushes;

        #region Initialize
        /// <summary>
        /// Initialize Kinect Sensor
        /// </summary>
        private void InitializeKinect()
        {
            // Get first Kinect
            _camera.InitializeKinect();

            skeletonBrushes = new System.Windows.Media.Brush[] {    System.Windows.Media.Brushes.Red,
                                                                    System.Windows.Media.Brushes.Pink,
                                                                    System.Windows.Media.Brushes.Crimson,
                                                                    System.Windows.Media.Brushes.Indigo,
                                                                    System.Windows.Media.Brushes.DodgerBlue, 
                                                                    System.Windows.Media.Brushes.Purple
                                                                        };

        }
        /// <summary>
        /// 
        /// </summary>
        public void InitializeCamera() 
        {
            if (_camera.Sensor == null)
                InitializeKinect();

            _camera.InitializeCamera(null);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        public void InitializeCamera(Image source)
        {
            if (_camera.Sensor == null)
                InitializeKinect();

            _camera.InitializeCamera(source);
        }
        /// <summary>
        /// 
        /// </summary>
        public void InitializeDepth() 
        {
            _camera.InitializeDepth(null);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        public void InitializeDepth(Image source)
        {
            _camera.InitializeDepth(source);
        }
        /// <summary>
        /// 
        /// </summary>
        public void InitializeInfrared() 
        {
            _camera.InitializeInfrared(null);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        public void InitializeInfrared(Image source)
        {
            _camera.InitializeInfrared(source);
        }
        /// <summary>
        /// 
        /// </summary>
        public void InitializeBody() 
        {
            _camera.InitializeBody();
        }
        /// <summary>
        /// 
        /// </summary>
        public void InitializeFace()
        {
            _camera.InitializeFace();
        }
        /// <summary>
        /// 
        /// </summary>
        public void InitializeSound(System.Windows.Media.Color color, WaitCallback cb) 
        {
            _audio.InitializeSound(_camera.Sensor, color);
            _audio.callback = cb;
            
        }

        public void InitializeSound(System.Windows.Media.Color color)
        {
            _audio.InitializeSound(_camera.Sensor, color);

        }

        #endregion

        #region Public Methods

        public System.Drawing.Bitmap ColorImageBitmap
        {
            get { return BitmapFromWriteableBitmap(_camera.ColorImageBitmap); }
            set { }
        }

        public Camera Camera 
        {
            get { return _camera; }
            set { }
        }

        public Sound Audio
        {
            get { return _audio; }
            set { }
        }

        public System.Windows.Media.Brush[] SkeletonBrushes
        {
            get { return skeletonBrushes; }
            set { skeletonBrushes = value; }
        }
        #endregion
    
        #region EventHandler

        public void OnColorFrameArrived()
        {
            _camera.ColorReader.FrameArrived += _camera.OnColorFrameArrived;
        }

        public void OnColorFrameArrived(EventHandler<ColorFrameArrivedEventArgs> EventArgs)
        {

            _camera.ColorReader.FrameArrived += EventArgs;

        }

        public void OnDepthFrameArrived()
        {
             _camera.DepthReader.FrameArrived += _camera.OnDepthFrameArrived;   
        }

        public void OnDepthFrameArrived(EventHandler<DepthFrameArrivedEventArgs> EventArgs)
        {
           
            _camera.DepthReader.FrameArrived += EventArgs;
           
        }

        public void OnInfraredFrameArrived()
        {
           _camera.InfraReader.FrameArrived += _camera.OnInfraredFrameArrived;
        }

        public void OnInfraredFrameArrived(EventHandler<InfraredFrameArrivedEventArgs> EventArgs)
        {
            _camera.InfraReader.FrameArrived += EventArgs;
        }

        public void OnBodyFrameArrived()
        {
             _camera.BodyReader.FrameArrived += _camera.OnBodyFrameArrived;
        }

        public void OnBodyFrameArrived(EventHandler<BodyFrameArrivedEventArgs> EventArgs)
        {
            _camera.BodyReader.FrameArrived += EventArgs;
        }

        public void OnFaceFrameArrived()
        {
            for (int i = 0; i < _camera.BodyCount; i++)
            {
                if (_camera.FaceFrameReaders[i] != null)
                {
                    // wire handler for face frame arrival
                    _camera.FaceFrameReaders[i].FrameArrived += _camera.OnFaceFrameArrived;
                }
            }
           
        }

        public void OnFaceFrameArrived(EventHandler<FaceFrameArrivedEventArgs> EventArgs)
        {
            for (int i = 0; i < _camera.BodyCount; i++)
            {
                if (_camera.FaceFrameReaders[i] != null)
                {
                    // wire handler for face frame arrival
                    _camera.FaceFrameReaders[i].FrameArrived += EventArgs;
                }
            }
        
        } 

        public void OnAudioFrameArrived(EventHandler<AudioBeamFrameArrivedEventArgs> EventArgs)
        {
            if (EventArgs != null)
            {
                 _audio.AudioReader.FrameArrived += EventArgs;
            }
           
        }

        public void OnAudioFrameArrived()
        {

            _audio.AudioReader.FrameArrived += _audio.OnAudioFrameArrived;
           
        }

        public void OffFaceFrameArrived()
        {

            for (int i = 0; i < _camera.BodyCount; i++)
            {
                if (_camera.FaceFrameReaders[i] != null)
                {
                    // wire handler for face frame arrival
                    _camera.FaceFrameReaders[i].FrameArrived -= _camera.OnFaceFrameArrived;
                }
            }
        }

        
        #endregion

        #region Close
        public void StopKinect()
        {
            if (Camera.Sensor != null)
                Camera.Sensor.Close();
        }

        public void StopCamera()
        {
            if (Camera.ColorReader != null)
            {
                Camera.ColorReader.Dispose();
                Camera.ColorReader = null;
            }
        }

        public void StopDepth()
        {
            if (Camera.DepthReader != null)
            {
                Camera.DepthReader.Dispose();
                Camera.DepthReader = null;
            }
        }
        public void StopInfrared()
        {
            if (Camera.InfraReader != null)
            {
                Camera.InfraReader.Dispose();
                Camera.InfraReader = null;
            }
        }
        public void StopBody()
        {
            if (Camera.BodyReader != null)
            {
                Camera.BodyReader.Dispose();
                Camera.BodyReader = null;
            }
        }
        public void StopSound()
        {
            if (Audio.AudioReader != null)
            {
                // AudioBeamFrameReader is IDisposable
                Audio.AudioReader.Dispose();
                Audio.AudioReader= null;
            }
        }

        #endregion

        #region Tools

        private System.Drawing.Bitmap BitmapFromWriteableBitmap(WriteableBitmap writeBmp)
        {
            if (writeBmp != null)
            {
                System.Drawing.Bitmap bmp;
                using (MemoryStream outStream = new MemoryStream())
                {
                    BitmapEncoder enc = new BmpBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create((BitmapSource)writeBmp));
                    enc.Save(outStream);
                    bmp = new System.Drawing.Bitmap(outStream);
                }
                return bmp;
            }
            else
                return null;
        }
        #endregion
    }
}