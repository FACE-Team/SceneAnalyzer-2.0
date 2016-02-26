using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Drawing;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.Kinect;
using KinectONE;

namespace SceneAnalyzerONE
{
    /// <summary>
    /// Interaction logic for KienctView.xaml
    /// </summary>
    public partial class KienctView : Window
    {
        private MainWindow Main;

        private KinectOne kinect;
       
        /// <summary>
        /// Object for locking energy buffer to synchronize threads.
        /// </summary>
        private readonly object energyLock = new object();

        /// <summary>
        /// Last time energy visualization was rendered to screen.
        /// </summary>
        private DateTime? lastEnergyRefreshTime;

        /// <summary>
        /// Default CTOR
        /// </summary>
        public KienctView(MainWindow _main)
        {
            Main = _main;

            InitializeComponent();

            kinect = new KinectOne();
            kinect.InitializeCamera(CameraImage);
            kinect.OnColorFrameArrived();

            kinect.InitializeBody();
            kinect.OnBodyFrameArrived(Main.SkeletonRead);


            kinect.InitializeSound((System.Windows.Media.Color)this.Resources["KinectPurpleColor"]);
            kinect.OnAudioFrameArrived(OnAudioFrameArrived);

            this.waveDisplay.Source = kinect.Audio.EnergyBitmap;
            CompositionTarget.Rendering += this.UpdateEnergy;


            // Close Kinect when closing app
            Closing += OnClosing;


        }

        #region FRAME PROCESSING

   


        #endregion FRAME PROCESSING

        #region UI Methods
    
        private void OnToggleCamera(object sender, RoutedEventArgs e)
        {
            ChangeVisualMode("Camera");
        }

        private void OnToggleDepth(object sender, RoutedEventArgs e)
        {
            ChangeVisualMode("Depth");
        }

        private void OnToggleInfrared(object sender, RoutedEventArgs e)
        {
            ChangeVisualMode("Infrared");
        }

        /// <summary>
        /// Close Kinect & Kinect Service
        /// </summary>
        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            KinectOne.StopSound();
            CompositionTarget.Rendering -= this.UpdateEnergy;

            KinectOne.StopBody();
            KinectOne.StopCamera();

            KinectOne.StopInfrared();
            KinectOne.StopDepth();
            KinectOne.StopKinect();



            //StopBody();
            //StopCamera();
            //StopDepth();
            //StopInfrared();
            //StopSound();
            //StopKinect();

        }

        /// <summary>
        /// Change the UI based on the mode
        /// </summary>
        /// <param name="mode">New UI mode</param>
        private void ChangeVisualMode(string mode)
        {
            // Invis all
            CameraGrid.Visibility = Visibility.Collapsed;
            DepthGrid.Visibility = Visibility.Collapsed;
            InfraredGrid.Visibility = Visibility.Collapsed;

            switch (mode)
            {
                case "Camera":
                    CameraGrid.Visibility = Visibility.Visible;

                    kinect.InitializeCamera(CameraImage);
                    kinect.OnColorFrameArrived();

                    kinect.InitializeBody();
                    kinect.OnBodyFrameArrived(Main.SkeletonRead);

                    kinect.InitializeSound((System.Windows.Media.Color)this.Resources["KinectPurpleColor"]);
                    kinect.OnAudioFrameArrived(OnAudioFrameArrived);
                    CompositionTarget.Rendering += this.UpdateEnergy;

                    kinect.StopDepth();
                    KinectOne.StopDepth();


                    break;
                case "Depth":
                    DepthGrid.Visibility = Visibility.Visible;

                    KinectOne.StopSound();
                    CompositionTarget.Rendering -= this.UpdateEnergy;

                    KinectOne.StopBody();
                    KinectOne.StopCamera();

                    KinectOne.StopInfrared();

                    KinectOne.InitializeDepth(DepthImage);
                    KinectOne.OnDepthFrameArrived();

                    //StopSound();
                    //StopBody();
                    //StopCamera();
                    //InitializeDepth();
                    //StopInfrared();


                    break;

                case "Infrared":
                    InfraredGrid.Visibility = Visibility.Visible;

                    KinectOne.StopSound();
                    CompositionTarget.Rendering -= this.UpdateEnergy;

                    KinectOne.StopBody();
                    KinectOne.StopCamera();
                    KinectOne.StopDepth();

                    KinectOne.InitializeInfrared(InfraredImage);
                    KinectOne.OnInfraredFrameArrived();
                    //StopSound();
                    //StopBody();
                    //StopCamera();
                    //StopDepth();
                    //InitializeInfrared();

                    break;
            }
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (kinect.Camera.ColorImageBitmap != null)
            {
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();

                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create(kinect.Camera.ColorImageBitmap));

                
                //RenderTargetBitmap renderBitmap = new RenderTargetBitmap(1920, 1080, 96d,96d,PixelFormats.Pbgra32);
                //renderBitmap.Render(Canvas_Shore);
                //encoder.Frames.Add(BitmapFrame.Create(renderBitmap));


                //RenderTargetBitmap renderBitmap2 = new RenderTargetBitmap(1920,1080, 96d, 96d, PixelFormats.Pbgra32);
                //renderBitmap.Render(Canvas_Robot);
                //encoder.Frames.Add(BitmapFrame.Create(renderBitmap2));

                //RenderTargetBitmap renderBitmap3 = new RenderTargetBitmap(1920, 1080, 96d, 96d, PixelFormats.Pbgra32);
                //renderBitmap.Render(SkeletonCanvas);
                //encoder.Frames.Add(BitmapFrame.Create(renderBitmap3));

                //RenderTargetBitmap renderBitmap4 = new RenderTargetBitmap(1920, 1080, 96d, 96d, PixelFormats.Pbgra32);
                //renderBitmap.Render(SkeletonCanvas2);
                //encoder.Frames.Add(BitmapFrame.Create(renderBitmap4));

                //RenderTargetBitmap renderBitmap5 = new RenderTargetBitmap(1920, 1080, 96d, 96d, PixelFormats.Pbgra32);
                //renderBitmap.Render(Point);
                //encoder.Frames.Add(BitmapFrame.Create(renderBitmap5));
                
                
                

                string time = System.DateTime.UtcNow.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                string path = System.IO.Path.Combine(myPhotos, "KinectScreenshot-BodyIndex-" + time + ".png");

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }

                       
                }
                catch (IOException)
                {
                       
                }
            }
        }

        /// <summary>
        /// Handles rendering energy visualization into a bitmap.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void UpdateEnergy(object sender, EventArgs e)
        {
            lock (this.energyLock)
            {
                // Calculate how many energy samples we need to advance since the last update in order to
                // have a smooth animation effect
                DateTime now = DateTime.UtcNow;
                DateTime? previousRefreshTime = this.lastEnergyRefreshTime;
                this.lastEnergyRefreshTime = now;

                // No need to refresh if there is no new energy available to render
                if (kinect.Audio.NewEnergyAvailable <= 0)
                {
                    return;
                }

                if (previousRefreshTime != null)
                {
                    float energyToAdvance = kinect.Audio.EnergyError + (((float)(now - previousRefreshTime.Value).TotalMilliseconds * kinect.Audio.SamplesPerMillisecond) / kinect.Audio.SamplesPerColumn);
                    int energySamplesToAdvance = Math.Min(kinect.Audio.NewEnergyAvailable, (int)Math.Round(energyToAdvance));
                    kinect.Audio.EnergyError = energyToAdvance - energySamplesToAdvance;
                    kinect.Audio.EnergyRefreshIndex = (kinect.Audio.EnergyRefreshIndex + energySamplesToAdvance) % kinect.Audio.Energy.Length;
                    kinect.Audio.NewEnergyAvailable -= energySamplesToAdvance;
                }
                // clear background of energy visualization area
                kinect.Audio.EnergyBitmap.WritePixels(kinect.Audio.FullEnergyRect, kinect.Audio.BackgroundPixels,kinect.Audio.EnergyBitmapWidth, 0);

                // Draw each energy sample as a centered vertical bar, where the length of each bar is
                // proportional to the amount of energy it represents.
                // Time advances from left to right, with current time represented by the rightmost bar.
                int baseIndex = (kinect.Audio.EnergyRefreshIndex + kinect.Audio.Energy.Length - kinect.Audio.EnergyBitmapWidth) % kinect.Audio.Energy.Length;
                for (int i = 0; i < kinect.Audio.EnergyBitmapWidth; ++i)
                {
                    int HalfImageHeight = kinect.Audio.EnergyBitmapHeight / 2;

                    // Each bar has a minimum height of 1 (to get a steady signal down the middle) and a maximum height
                    // equal to the bitmap height.
                    int barHeight = (int)Math.Max(1.0, kinect.Audio.Energy[(baseIndex + i) % kinect.Audio.Energy.Length] * kinect.Audio.EnergyBitmapHeight);

                    // Center bar vertically on image
                    var barRect = new Int32Rect(i, HalfImageHeight - (barHeight / 2), 1, barHeight);

                    // Draw bar in foreground color
                    kinect.Audio.EnergyBitmap.WritePixels(barRect, kinect.Audio.ForegroundPixels, 1, 0);
                }
            }
        }
        



        #endregion UI Methods

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

                            if (subFrame.BeamAngle != kinect.Audio.BeamAngle)
                            {
                                kinect.Audio.BeamAngle = subFrame.BeamAngle;
                                updateBeam = true;
                            }

                            if (subFrame.BeamAngleConfidence != kinect.Audio.BeamAngleConfidence)
                            {
                                kinect.Audio.BeamAngleConfidence = subFrame.BeamAngleConfidence;
                                updateBeam = true;
                            }

                            if (updateBeam)
                            {
                                // Refresh display of audio beam
                                this.AudioBeamChanged();

                            }

                            // Process audio buffer
                            subFrame.CopyFrameDataToArray(kinect.Audio.AudioBuffer);

                            for (int i = 0; i < kinect.Audio.AudioBuffer.Length; i += kinect.Audio.BytesPerSample)
                            {
                                // Extract the 32-bit IEEE float sample from the byte array
                                float audioSample = BitConverter.ToSingle(kinect.Audio.AudioBuffer, i);

                                kinect.Audio.AccumulatedSquareSum += audioSample * audioSample;
                                ++kinect.Audio.AccumulatedSampleCount;

                                if (kinect.Audio.AccumulatedSampleCount < kinect.Audio.SamplesPerColumn)
                                {
                                    continue;
                                }

                                float meanSquare = kinect.Audio.AccumulatedSquareSum / kinect.Audio.SamplesPerColumn;

                                if (meanSquare > 1.0f)
                                {
                                    // A loud audio source right next to the sensor may result in mean square values
                                    // greater than 1.0. Cap it at 1.0f for display purposes.
                                    meanSquare = 1.0f;
                                }

                                // Calculate energy in dB, in the range [MinEnergy, 0], where MinEnergy < 0
                                float energy = kinect.Audio.MinEnergy;

                                if (meanSquare > 0)
                                {
                                    energy = (float)(10.0 * Math.Log10(meanSquare));
                                }

                                lock (this.energyLock)
                                {
                                    // Normalize values to the range [0, 1] for display
                                    kinect.Audio.Energy[kinect.Audio.EnergyIndex] = (kinect.Audio.MinEnergy - energy) / kinect.Audio.MinEnergy;
                                    kinect.Audio.EnergyIndex = (kinect.Audio.EnergyIndex + 1) % kinect.Audio.Energy.Length;
                                    ++kinect.Audio.NewEnergyAvailable;
                                }

                                kinect.Audio.AccumulatedSquareSum = 0;
                                kinect.Audio.AccumulatedSampleCount = 0;

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


        
        /// <summary>
        /// Method called when audio beam angle and/or confidence have changed.
        /// </summary>
        //private void AudioBeamChanged(object state)
        private void AudioBeamChanged()
        {
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
            new Action(delegate()
            {
               

         
              
                // Maximum possible confidence corresponds to this gradient width
                const float MinGradientWidth = 0.04f;

                // Set width of mark based on confidence.
                // A confidence of 0 would give us a gradient that fills whole area diffusely.
                // A confidence of 1 would give us the narrowest allowed gradient width.
                float halfWidth = Math.Max(1 - kinect.Audio.BeamAngleConfidence, MinGradientWidth) / 2;

                // Update the gradient representing sound source position to reflect confidence
                this.beamBarGsPre.Offset = Math.Max(this.beamBarGsMain.Offset - halfWidth, 0);
                this.beamBarGsPost.Offset = Math.Min(this.beamBarGsMain.Offset + halfWidth, 1);

                // Convert from radians to degrees for display purposes
                kinect.Audio.BeamAngleInDeg = kinect.Audio.BeamAngle * 180.0f / (float)Math.PI;

                // Rotate gradient to match angle
                this.beamBarRotation.Angle = -kinect.Audio.BeamAngleInDeg;
                beamNeedleRotation.Angle = -kinect.Audio.BeamAngleInDeg;

                // Display new numerical values
                //beamAngleText.Text = string.Format(CultureInfo.CurrentCulture, Properties.Resources.BeamAngle, beamAngleInDeg.ToString("0", CultureInfo.CurrentCulture));
                //beamConfidenceText.Text = string.Format(CultureInfo.CurrentCulture, Properties.Resources.BeamAngleConfidence, this.beamAngleConfidence.ToString("0.00", CultureInfo.CurrentCulture));
           }));

        }


        public KinectOne KinectOne
        {
            get { return kinect; }
        }
        #region Tool

        /// <summary>
        /// Visualize the body
        /// </summary>
        /// <param name="body">Tracked body</param>
        public void DrawBody(Body body, System.Windows.Media.Brush brush)
        {
            // Draw points
            DrawPolyline(body, new[] { JointType.Head, JointType.Neck, JointType.SpineShoulder, JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft }, brush);
            DrawPolyline(body, new[] { JointType.SpineShoulder, JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight }, brush);
            DrawPolyline(body, new[] { JointType.SpineShoulder, JointType.SpineMid, JointType.SpineBase, JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight }, brush);
            DrawPolyline(body, new[] { JointType.SpineBase, JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft }, brush);

            foreach (JointType type in body.Joints.Keys)
            {

                // Draw all the body joints
                switch (type)
                {

                    case JointType.Head:
                    case JointType.FootLeft:
                    case JointType.FootRight:
                        DrawJoint(body.Joints[type], 20, System.Windows.Media.Brushes.Yellow, 2, System.Windows.Media.Brushes.White);
                        break;
                    case JointType.ShoulderLeft:
                    case JointType.ShoulderRight:
                    case JointType.HipLeft:
                    case JointType.HipRight:
                        DrawJoint(body.Joints[type], 20, System.Windows.Media.Brushes.YellowGreen, 2, System.Windows.Media.Brushes.White);
                        break;
                    case JointType.ElbowLeft:
                    case JointType.ElbowRight:
                    case JointType.KneeLeft:
                    case JointType.KneeRight:
                        DrawJoint(body.Joints[type], 15, System.Windows.Media.Brushes.LawnGreen, 2, System.Windows.Media.Brushes.White);
                        break;
                    case JointType.HandLeft:
                        DrawHandJoint(body.Joints[type], body.HandLeftState, 20, 2, System.Windows.Media.Brushes.White);
                        break;
                    case JointType.HandRight:
                        DrawHandJoint(body.Joints[type], body.HandRightState, 20, 2, System.Windows.Media.Brushes.White);
                        break;
                    default:
                        DrawJoint(body.Joints[type], 15, System.Windows.Media.Brushes.RoyalBlue, 2, System.Windows.Media.Brushes.White);
                        break;
                }

            }


        }

        /// <summary>
        /// Draws a body joint
        /// </summary>
        /// <param name="joint">Joint of the body</param>
        /// <param name="radius">Circle radius</param>
        /// <param name="fill">Fill color</param>
        /// <param name="borderWidth">Thickness of the border</param>
        /// <param name="border">Color of the boder</param>
        private void DrawJoint(Joint joint, double radius, SolidColorBrush fill, double borderWidth, SolidColorBrush border)
        {
            if (joint.TrackingState != TrackingState.Tracked) return;

            // Map the CameraPoint to ColorSpace so they match
            ColorSpacePoint colorPoint = kinect.Camera.Sensor.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            colorPoint.X *= (float)(CameraImage.ActualWidth / kinect.Camera.Sensor.ColorFrameSource.FrameDescription.Width);
            colorPoint.Y *= (float)(CameraImage.ActualHeight / kinect.Camera.Sensor.ColorFrameSource.FrameDescription.Height);

            colorPoint.X += (float)(SkeletonCanvas.ActualWidth - CameraImage.ActualWidth) / 2;
            colorPoint.Y += (float)(SkeletonCanvas.ActualHeight - CameraImage.ActualHeight) / 2;


            // Create the UI element based on the parameters
            Ellipse el = new Ellipse();
            el.Fill = fill;
            el.Stroke = border;
            el.StrokeThickness = borderWidth;
            el.Width = el.Height = radius;

            // Add the Ellipse to the canvas
            SkeletonCanvas.Children.Add(el);

            //// Avoid exceptions based on bad tracking
            if (float.IsInfinity(colorPoint.X) || float.IsInfinity(colorPoint.Y)) return;

            //// Allign ellipse on canvas (Divide by 2 because image is only 50% of original size)
            Canvas.SetLeft(el, colorPoint.X - (radius + borderWidth) / 2);
            Canvas.SetTop(el, colorPoint.Y - (radius + borderWidth) / 2);
        }

        /// <summary>
        /// Draw a body joint for a hand and assigns a specific color based on the handstate
        /// </summary>
        /// <param name="joint">Joint representing a hand</param>
        /// <param name="handState">State of the hand</param>
        private void DrawHandJoint(Joint joint, HandState handState, double radius, double borderWidth, SolidColorBrush border)
        {
            switch (handState)
            {
                case HandState.Lasso:
                    DrawJoint(joint, radius, System.Windows.Media.Brushes.Cyan, borderWidth, border);
                    break;
                case HandState.Open:
                    DrawJoint(joint, radius, System.Windows.Media.Brushes.Green, borderWidth, border);
                    break;
                case HandState.Closed:
                    DrawJoint(joint, radius, System.Windows.Media.Brushes.Red, borderWidth, border);
                    break;
                default:
                    break;
            }
        }

        private void DrawPolyline(Body body, IEnumerable<JointType> joints, System.Windows.Media.Brush brush)
        {

            var figure = new Polyline { StrokeThickness = 5, Stroke = brush };

            foreach (var t in joints)
            {

                ColorSpacePoint colorPoint = kinect.Camera.Sensor.CoordinateMapper.MapCameraPointToColorSpace(body.Joints[t].Position);
                colorPoint.X *= (float)(CameraImage.ActualWidth / kinect.Camera.Sensor.ColorFrameSource.FrameDescription.Width);
                colorPoint.Y *= (float)(CameraImage.ActualHeight / kinect.Camera.Sensor.ColorFrameSource.FrameDescription.Height);

                System.Windows.Point point = new System.Windows.Point();
                point.X = colorPoint.X + (SkeletonCanvas.ActualWidth - CameraImage.ActualWidth) / 2;
                point.Y = colorPoint.Y + (SkeletonCanvas.ActualHeight - CameraImage.ActualHeight) / 2;
                figure.Points.Add(point);
            }

            SkeletonCanvas2.Children.Add(figure);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="colorFrame"></param>
        /// <returns></returns>
        public System.Drawing.Bitmap ColorFrameToBitmap(ColorFrame colorFrame)
        {
            FrameDescription frameDesc = colorFrame.FrameDescription;

            byte[] pixelBuffer = new byte[kinect.Camera.ColorPixels.Length];
            if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                colorFrame.CopyRawFrameDataToArray(pixelBuffer);
            }
            else colorFrame.CopyConvertedFrameDataToArray(pixelBuffer, ColorImageFormat.Bgra);



            System.Drawing.Bitmap bitmapFrame = new System.Drawing.Bitmap(frameDesc.Width, frameDesc.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);


            System.Drawing.Imaging.BitmapData bitmapData = bitmapFrame.LockBits(new System.Drawing.Rectangle(0, 0, frameDesc.Width, frameDesc.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bitmapFrame.PixelFormat);


            IntPtr intPointer = bitmapData.Scan0;
            //Marshal.Copy(pixelBuffer, 0, intPointer, bitmapData.Width * bitmapData.Height);
            System.Runtime.InteropServices.Marshal.Copy(pixelBuffer, 0, intPointer, kinect.Camera.ColorPixels.Length);

            bitmapFrame.UnlockBits(bitmapData);

            return bitmapFrame;



        }

        #endregion

     

        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void window_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;

        }

        private void window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

      
          

    }
}
