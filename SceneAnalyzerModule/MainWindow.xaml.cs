using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using System.ComponentModel;
using System.Timers;
using System.Collections;
using System.Threading;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Diagnostics;

using Microsoft.Kinect;
using Microsoft.Kinect.Face;

using Microsoft.Speech.Recognition;
using Microsoft.Speech.AudioFormat;

using ShoreNet;
using FACELibrary;
using ControllersLibrary;

using YarpManagerCS;

//using Emgu.CV;

using MathNet;
using MathNet.Numerics.LinearAlgebra;

namespace SceneAnalyzerONE
{    
    public partial class MainWindow : Window
    {
 
        #region Kinect parameters

        KienctView kinectView;

        private Object thisLock = new Object();
        private bool colorBitmapLock = false;
        private bool drawSkeleton = false; //checkbox skeleton
       
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private double _beamAngle;
        public double BeamAngle
        {
            get { return _beamAngle; }
            set
            {
                _beamAngle = value;
                OnPropertyChanged("BeamAngle");
            }
        }

        #endregion

        #region Delta per Cross-Ambient (Kinect-Shore)

        private double DeltaErrorX = 100; // 30px
        private double TimeSubj = 15; // 30px
        private double DistanceSubj = 0.1; // 30px


        #endregion

        #region Info

        private Surroundings environment;
        private ObjectScene[] sceneObjects;
        private List<Subject> sceneSubjects;
        private List<Subject> sceneSubjectsShore;

        private Dictionary<DateTime, Subject> tempSubject;
        private List<Subject> sceneSubjectsCopy;
        private List<Subject> SubjectsCopy;
        private Winner winner;
        private List<Shore> shores;
        private Saliency saliency;
      

        #endregion

        #region YARP variables

        private string attentionModuleOut_lookAt = ConfigurationManager.AppSettings["YarpPortLookAt_OUT"].ToString();
        private string FaceRecognitionOut = ConfigurationManager.AppSettings["YarpPortSubject_OUTPUT"].ToString();
        private string ShoreOUT = ConfigurationManager.AppSettings["YarpPortShore_OUT"].ToString();
        private string SaliencyOUT = ConfigurationManager.AppSettings["YarpPortSaliency_OUT"].ToString();


        private const string kinectOut = "/kinectOut";

        private string sceneAnalyserIn_lookAt = ConfigurationManager.AppSettings["YarpPortLookAt_INPUT"].ToString();
        private string sceneAnalyserIn_Recognized =ConfigurationManager.AppSettings["YarpPortSubject_INPUT"].ToString();
        private string sceneAnalyserIn_Shore = ConfigurationManager.AppSettings["YarpPortShore_INPUT"].ToString();
        private string sceneAnalyserIn_Saliency = ConfigurationManager.AppSettings["YarpPortSaliency_INPUT"].ToString();

        private string sceneAnalyserOutOPC = ConfigurationManager.AppSettings["YarpPortMetaSceneOPC_OUT"].ToString();//"/SceneAnalyserOPC/MetaSceneOPC:o";
        private string sceneAnalyserOutXML = ConfigurationManager.AppSettings["YarpPortMetaSceneXML_OUT"].ToString(); //"/SceneAnalyserXML/MetaScene:o";
        private string sceneAnalyserOutSound = ConfigurationManager.AppSettings["YarpPortSound_OUT"].ToString();
        private string sceneAnalyserIn_Sensor = ConfigurationManager.AppSettings["YarpPortSensor_IN"].ToString();
        private string sceneAnalyserOut_Sensor = ConfigurationManager.AppSettings["YarpPortSensor_OUT"].ToString();


        private bool lookAtPortExists = false;
        private bool lookAtConnectionExists = false;
        private bool RecognizedPortExists = false;

        private YarpPort yarpPortSceneXML;
        private YarpPort yarpPortSceneOPC;
        private YarpPort yarpPortLookAt;
        private YarpPort yarpPortRecognized;
        private YarpPort yarpPortSound;
        private YarpPort yarpPortShore;
        private YarpPort yarpPortSaliency;

        private YarpPort yarpPortSensor;



        private System.Threading.Thread senderThread = null;

    
        private string receiveLookAtData = "";
        private string receiveSubjectRecognized = "";
        private string receiveShoreData = "";
        private string receiveSaliencyData = "";
        private string receiveSensorData = "";

        private string senderImgString = string.Empty;

        //private System.Timers.Timer checkPortTimer = new System.Timers.Timer();
        private System.Timers.Timer checkYarpStatusTimer;
        private System.Timers.Timer yarpReceiverLookAt;
        private System.Timers.Timer yarpReceiverShore;
        private System.Timers.Timer yarpReceiverSaliency;
        private System.Timers.Timer yarpReceiverSubject;
        private System.Timers.Timer yarpReceiverSensor;


        private object lockObject = new object();
        private object lockSendImage = new object();
        private object lockSendScene = new object();

        // mi serve per sapere se sto aggiornando i subject
        private bool ReadSubject = false;


        UdpClient client;

        #endregion

        #region External App

        Process saliencyModule = new Process();
        Process shoreModule = new Process();
        Process SubjectRecognitionModule = new Process();



        #endregion
    
        bool ShoreView=false;
        bool SaliencyView = false;
        bool SkeletonView = false;

        System.Threading.Thread receiverUDP;

        string str = "";

        /// <summary>
        /// 
        /// </summary>
        public enum gesture { none, raiseHand, YEAH, think, armsCrossed, pray, hand, OMG };

        /// <summary>
        /// 
        /// </summary>
        public MainWindow()
        {

            var dllDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + dllDirectory);

            InitializeComponent();

            Init();
            InitKinect();
            InitYarp();

           
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var secondaryScreen = System.Windows.Forms.Screen.AllScreens.Where(s => !s.Primary).FirstOrDefault();

            if (secondaryScreen != null)
            {
                if (!this.IsLoaded)
                    this.WindowStartupLocation = WindowStartupLocation.Manual;

                var workingArea = secondaryScreen.WorkingArea;
                this.Left = workingArea.Left;
                this.Top = workingArea.Top;

            }

            try
            {
                foreach (Process proc in Process.GetProcessesByName("ShoreModule"))
                {
                    proc.CloseMainWindow();
                }

                foreach (Process proc in Process.GetProcessesByName("SaliencyModule"))
                {
                    proc.CloseMainWindow();
                }

                foreach (Process proc in Process.GetProcessesByName("SubjectRecognitionQRCode"))
                {
                    proc.CloseMainWindow();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


        }


        #region Initialization

        private void Init()
        {
            environment = new Surroundings();
            sceneObjects = new ObjectScene[] { new ObjectScene() };
            sceneSubjects = new List<Subject>();
            sceneSubjectsShore = new List<Subject>();

            tempSubject = new Dictionary<DateTime, Subject>();
            sceneSubjectsCopy = new List<Subject>();
            SubjectsCopy = new List<Subject>();
            winner = new Winner();

        }

        private void InitKinect()
        {
                 
            RecognitionEnginePanel.IsEnabled = true;

            kinectView = new KienctView(this);
            kinectView.Show();

            environment.resolution = new Resolution(kinectView.KinectOne.Camera.Sensor.ColorFrameSource.FrameDescription.Height, kinectView.KinectOne.Camera.Sensor.ColorFrameSource.FrameDescription.Width);

          
        }

        private void InitUDP()
        {
            string ip;
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress addr in localIPs)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    ip = addr.ToString();
                    client = new UdpClient(ip, Convert.ToInt32(ConfigurationManager.AppSettings["UDPPortMetaScene"]));
                    break;
                }
            }




        }

        private void InitYarp()
        {
            #region define port

           
            yarpPortSceneXML = new YarpPort();
            yarpPortSceneXML.openSender(sceneAnalyserOutXML);


            yarpPortSceneOPC = new YarpPort();
            yarpPortSceneOPC.openSender(sceneAnalyserOutOPC);

            yarpPortSound = new YarpPort();
            yarpPortSound.openSender(sceneAnalyserOutSound);
            
            
            yarpPortLookAt = new YarpPort();
            yarpPortLookAt.openReceiver(attentionModuleOut_lookAt, sceneAnalyserIn_lookAt);


            yarpPortShore = new YarpPort();
            yarpPortShore.openReceiver(ShoreOUT, sceneAnalyserIn_Shore);


            yarpPortSaliency = new YarpPort();
            yarpPortSaliency.openReceiver(SaliencyOUT, sceneAnalyserIn_Saliency);


            yarpPortRecognized = new YarpPort();
            yarpPortRecognized.openReceiver(FaceRecognitionOut, sceneAnalyserIn_Recognized);

            yarpPortSensor = new YarpPort();
            yarpPortSensor.openReceiver(sceneAnalyserOut_Sensor, sceneAnalyserIn_Sensor);

            #endregion

            #region define timer or thread

            senderThread = new System.Threading.Thread(SendData);
            senderThread.Start();

                       
            yarpReceiverLookAt = new System.Timers.Timer();
            yarpReceiverLookAt.Interval = 200;
            yarpReceiverLookAt.Elapsed += new ElapsedEventHandler(ReceiveDataLookAt);

            yarpReceiverShore = new System.Timers.Timer();
            yarpReceiverShore.Interval = 200;
            yarpReceiverShore.Elapsed += new ElapsedEventHandler(ReceiveDataShore);
           

            yarpReceiverSaliency = new System.Timers.Timer();
            yarpReceiverSaliency.Interval = 200;
            yarpReceiverSaliency.Elapsed += new ElapsedEventHandler(ReceiveDataSaliency);

            yarpReceiverSubject = new System.Timers.Timer();
            yarpReceiverSubject.Interval = 200;
            yarpReceiverSubject.Elapsed += new ElapsedEventHandler(ReceiveDataRecognized);


            yarpReceiverSensor = new System.Timers.Timer();
            yarpReceiverSensor.Interval = 200;
            yarpReceiverSensor.Elapsed += new ElapsedEventHandler(ReceiveDataSensor);

            #endregion

            // controllo se la connessione con le porte sono attive
            checkYarpStatusTimer = new System.Timers.Timer();
            checkYarpStatusTimer.Elapsed += new ElapsedEventHandler(CheckYarpConnections);
            checkYarpStatusTimer.Interval = (1000)*5;
            checkYarpStatusTimer.Start();
          
            
        }

        #endregion

        #region Kinect

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SkeletonRead(object sender, BodyFrameArrivedEventArgs e)
        {
            //timerS.Start();
            ReadSubject = true;

            environment.soundAngle = kinectView.KinectOne.Audio.BeamAngleInDeg;
            environment.soundEstimatedX = (float)Math.Tan(kinectView.KinectOne.Audio.BeamAngle);
            environment.soundAverageNorm = kinectView.KinectOne.Audio.Energy.Average();
            environment.soundDecibelNorm = kinectView.KinectOne.Audio.Energy[kinectView.KinectOne.Audio.EnergyIndex];
            // Get frame reference
            BodyFrameReference refer = e.FrameReference;

            if (refer == null) return;

            // Get body frame
            BodyFrame frame = refer.AcquireFrame();

            if (frame == null) return;

            using (frame)
            {
                // Aquire body data
                frame.GetAndRefreshBodyData(kinectView.KinectOne.Camera.Bodies);
                
                kinectView.SkeletonCanvas.Children.Clear();
                kinectView.SkeletonCanvas2.Children.Clear();
                kinectView.Canvas_Robot.Children.Clear();

                #region Remove Subject from list
                foreach (Subject checkSub in sceneSubjects.ToList())
                {
                    bool remove = true;
                    foreach (Body ske in kinectView.KinectOne.Camera.Bodies)
                    {
                        if (checkSub.idKinect == (int)ske.TrackingId)
                        {
                            remove = false;
                            break;
                        }
                    }

                    if (remove)
                    {
                        try
                        {
                            tempSubject.Add(DateTime.Now, checkSub);
                            sceneSubjects.Remove(checkSub);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error " + ex.Message);
                        }
                    }
                }
                #endregion

                for(int i=0 ; i<kinectView.KinectOne.Camera.BodyCount;i++)
                {
                    Body body = kinectView.KinectOne.Camera.Bodies[i];
                    // Only process tracked bodies
                    if (body.IsTracked)
                    {
                        int countSubject = 0;

                        if (SkeletonView)
                        {
                            kinectView.DrawBody(body, kinectView.KinectOne.SkeletonBrushes[countSubject]);
                        }
                       
                        #region check subject present
                        bool present = false;
                        foreach (Subject checkSub in sceneSubjects)
                        {
                            if (checkSub.idKinect == (int)body.TrackingId)
                            {
                                present = true;
                                break;
                            }
                            countSubject++;
                        }

                        if (!present)
                        {
                            bool continuo = true;

                            if (tempSubject.Count > 0)
                            {
                                Position spine = new Position(body.Joints[JointType.SpineBase].Position.X, body.Joints[JointType.SpineBase].Position.Y, body.Joints[JointType.SpineBase].Position.Z);

                                foreach (var kvp in tempSubject)
                                {

                                    if (DateTime.Compare(DateTime.Now, kvp.Key.AddSeconds(TimeSubj)) < 0)
                                    {
                                        Console.WriteLine((Math.Round(kvp.Value.spineBase.X, 1) + "  " + Math.Round(spine.X, 1)));

                                        if (Math.Abs(kvp.Value.spineBase.X - spine.X) < DistanceSubj)
                                        {
                                            tempSubject[kvp.Key].idKinect = (int)body.TrackingId;
                                            sceneSubjects.Add(tempSubject[kvp.Key]);
                                            countSubject = sceneSubjects.Count - 1;
                                            continuo = false;
                                            break;
                                        }
                                        else
                                        {
                                            Console.WriteLine("Troppo distante: " + Math.Abs(kvp.Value.spineBase.X - spine.X));
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Troppo tempo: " + DateTime.Now + "  " + kvp.Key.AddSeconds(TimeSubj));
                                        tempSubject.Remove(kvp.Key);
                                        break;
                                    }

                                }
                            }


                            if (continuo)
                            {
                                Subject newSub = new Subject();
                                newSub.idKinect = (int)body.TrackingId;
                                newSub.angle = 0;
                                newSub.name = new List<string>() { "unknown" };
                                sceneSubjects.Add(newSub);
                                countSubject = sceneSubjects.Count - 1;
                            }
                        }

                        #endregion

                        #region face kinect
                        if(FaceTrackingCheckbox.IsChecked==true)
                            if (kinectView.KinectOne.Camera.FaceFrameSources[i].IsTrackingIdValid)   // check if a valid face is tracked in this face source
                            {
                                // check if we have valid face frame results
                                if (kinectView.KinectOne.Camera.FaceFrameResults[i] != null)
                                {
                                    // check if we have valid face frame results
                                    if (kinectView.KinectOne.Camera.FaceFrameResults[i].FaceProperties != null)
                                    {
                                   
                                        sceneSubjects[countSubject].Engaged = kinectView.KinectOne.Camera.FaceFrameResults[i].FaceProperties[FaceProperty.Engaged].ToString();        
                                        sceneSubjects[countSubject].WearingGlasses = kinectView.KinectOne.Camera.FaceFrameResults[i].FaceProperties[FaceProperty.WearingGlasses].ToString();
                                        sceneSubjects[countSubject].LeftEyeClosed = kinectView.KinectOne.Camera.FaceFrameResults[i].FaceProperties[FaceProperty.LeftEyeClosed].ToString();
                                        sceneSubjects[countSubject].RightEyeClosed = kinectView.KinectOne.Camera.FaceFrameResults[i].FaceProperties[FaceProperty.RightEyeClosed].ToString();
                                        sceneSubjects[countSubject].MouthOpen = kinectView.KinectOne.Camera.FaceFrameResults[i].FaceProperties[FaceProperty.MouthOpen].ToString();                                 
                                        sceneSubjects[countSubject].MouthMoved = kinectView.KinectOne.Camera.FaceFrameResults[i].FaceProperties[FaceProperty.MouthMoved].ToString();
                                        sceneSubjects[countSubject].LookingAway = kinectView.KinectOne.Camera.FaceFrameResults[i].FaceProperties[FaceProperty.LookingAway].ToString();

                                    }
                                }

                            }
                            else
                            {
                                // check if the corresponding body is tracked 
                                if (kinectView.KinectOne.Camera.Bodies[i].IsTracked)
                                {
                                    // update the face frame source to track this body
                                    kinectView.KinectOne.Camera.FaceFrameSources[i].TrackingId = kinectView.KinectOne.Camera.Bodies[i].TrackingId;
                                }
                            }

                        #endregion

                        #region Tracking subject

                        sceneSubjects[countSubject].trackedState = true;

                        //sceneSubjects[i].ankle = new Limb();
                        sceneSubjects[countSubject].ankle.left = new Position(body.Joints[JointType.AnkleLeft].Position.X, body.Joints[JointType.AnkleLeft].Position.Y, body.Joints[JointType.AnkleLeft].Position.Z);

                        sceneSubjects[countSubject].ankle.right = new Position(body.Joints[JointType.AnkleRight].Position.X, body.Joints[JointType.AnkleRight].Position.Y, body.Joints[JointType.AnkleRight].Position.Z);

                        sceneSubjects[countSubject].elbow.left = new Position(body.Joints[JointType.ElbowLeft].Position.X, body.Joints[JointType.ElbowLeft].Position.Y, body.Joints[JointType.ElbowLeft].Position.Z);

                        sceneSubjects[countSubject].elbow.right = new Position(body.Joints[JointType.ElbowRight].Position.X, body.Joints[JointType.ElbowRight].Position.Y, body.Joints[JointType.ElbowRight].Position.Z);
 
                        sceneSubjects[countSubject].foot.left = new Position(body.Joints[JointType.FootLeft].Position.X, body.Joints[JointType.FootLeft].Position.Y, body.Joints[JointType.FootLeft].Position.Z);
   
                        sceneSubjects[countSubject].foot.right = new Position(body.Joints[JointType.FootRight].Position.X, body.Joints[JointType.FootRight].Position.Y, body.Joints[JointType.FootRight].Position.Z);

                        sceneSubjects[countSubject].hand.left= new Position(body.Joints[JointType.HandLeft].Position.X, body.Joints[JointType.HandLeft].Position.Y, body.Joints[JointType.HandLeft].Position.Z);

                        sceneSubjects[countSubject].hand.right = new Position(body.Joints[JointType.HandRight].Position.X, body.Joints[JointType.HandRight].Position.Y, body.Joints[JointType.HandRight].Position.Z);

                        sceneSubjects[countSubject].handTip.left = new Position(body.Joints[JointType.HandTipLeft].Position.X, body.Joints[JointType.HandTipLeft].Position.Y, body.Joints[JointType.HandTipLeft].Position.Z);
                          
                        sceneSubjects[countSubject].handTip.right = new Position(body.Joints[JointType.HandTipRight].Position.X, body.Joints[JointType.HandTipRight].Position.Y, body.Joints[JointType.HandTipRight].Position.Z);
                        
                        sceneSubjects[countSubject].head = new Position(body.Joints[JointType.Head].Position.X, body.Joints[JointType.Head].Position.Y, body.Joints[JointType.Head].Position.Z);
                                   
                        sceneSubjects[countSubject].hip.left = new Position(body.Joints[JointType.HipLeft].Position.X, body.Joints[JointType.HipLeft].Position.Y, body.Joints[JointType.HipLeft].Position.Z);
                         
                        sceneSubjects[countSubject].hip.right = new Position(body.Joints[JointType.HipRight].Position.X, body.Joints[JointType.HipRight].Position.Y, body.Joints[JointType.HipRight].Position.Z);
                                   
                        sceneSubjects[countSubject].knee.left = new Position(body.Joints[JointType.KneeLeft].Position.X, body.Joints[JointType.KneeLeft].Position.Y, body.Joints[JointType.KneeLeft].Position.Z);

                        sceneSubjects[countSubject].knee.right = new Position(body.Joints[JointType.KneeRight].Position.X, body.Joints[JointType.KneeRight].Position.Y, body.Joints[JointType.KneeRight].Position.Z);

                        sceneSubjects[countSubject].neck= new Position(body.Joints[JointType.Neck].Position.X, body.Joints[JointType.Neck].Position.Y, body.Joints[JointType.Neck].Position.Z);

                        sceneSubjects[countSubject].shoulder.left = new Position(body.Joints[JointType.ShoulderLeft].Position.X, body.Joints[JointType.ShoulderLeft].Position.Y, body.Joints[JointType.Neck].Position.Z);

                        sceneSubjects[countSubject].shoulder.right = new Position(body.Joints[JointType.ShoulderRight].Position.X, body.Joints[JointType.ShoulderRight].Position.Y, body.Joints[JointType.ShoulderRight].Position.Z);
                                    
                        sceneSubjects[countSubject].spineBase=new Position(body.Joints[JointType.SpineBase].Position.X, body.Joints[JointType.SpineBase].Position.Y, body.Joints[JointType.SpineBase].Position.Z);
                            
                        sceneSubjects[countSubject].spineMid = new Position(body.Joints[JointType.SpineMid].Position.X, body.Joints[JointType.SpineMid].Position.Y, body.Joints[JointType.SpineMid].Position.Z);

                        sceneSubjects[countSubject].spineShoulder = new Position(body.Joints[JointType.SpineShoulder].Position.X, body.Joints[JointType.SpineShoulder].Position.Y, body.Joints[JointType.SpineShoulder].Position.Z);

                        sceneSubjects[countSubject].thumb.left = new Position(body.Joints[JointType.ThumbLeft].Position.X, body.Joints[JointType.ThumbLeft].Position.Y, body.Joints[JointType.ThumbLeft].Position.Z);

                        sceneSubjects[countSubject].thumb.right = new Position(body.Joints[JointType.ThumbRight].Position.X, body.Joints[JointType.ThumbRight].Position.Y, body.Joints[JointType.ThumbRight].Position.Z);

                        sceneSubjects[countSubject].wrist.left = new Position(body.Joints[JointType.WristLeft].Position.X, body.Joints[JointType.WristLeft].Position.Y, body.Joints[JointType.WristLeft].Position.Z);

                        sceneSubjects[countSubject].wrist.right = new Position(body.Joints[JointType.WristRight].Position.X, body.Joints[JointType.WristRight].Position.Y, body.Joints[JointType.WristRight].Position.Z);


                        sceneSubjects[countSubject].angle = (float)Math.Round((Math.Atan((body.Joints[JointType.SpineMid].Position.X / body.Joints[JointType.SpineMid].Position.Z)) * (180 / (Math.PI))), 2);
                        sceneSubjects[countSubject].speak_prob = Math.Abs(Math.Abs(sceneSubjects[countSubject].angle - environment.soundAngle) - 70) / 70;


                        //SDK del kinect prende come(0,0) il punto in alto a destra della depth e il punto (640,480) in basso a sinistra
                        //il mondo di FACE invece va da 0 a 1 in entrambi gli assi (X,Y) per questo si normalizza in base alle info del SDK
                        //spinPoints.Insert(i, GetJointPoint(skeleton.Joints[JointType.Spine], Canvas_Skeleton));
                        //sceneSubjects[i].normalizedspincenter_xy = new List<float> { 
                        //    (float)Math.Round((decimal)(sceneSubjects[i].head_xyz[0]) / (kinectView.Sensor.ColorFrameSource.FrameDescription.Width), 2, MidpointRounding.ToEven), 
                        //    (float)Math.Round((decimal)((kinectView.Sensor.ColorFrameSource.FrameDescription.Height -  sceneSubjects[i].head_xyz[1]) / (kinectView.Sensor.ColorFrameSource.FrameDescription.Height)), 2, MidpointRounding.ToEven)
                        //};

                        //scrivo l'id sulla testa
                        ColorSpacePoint xyhead0 = kinectView.KinectOne.Camera.Sensor.CoordinateMapper.MapCameraPointToColorSpace(body.Joints[JointType.Head].Position);
                        xyhead0.X *= (float)kinectView.CameraImage.ActualWidth / kinectView.KinectOne.Camera.Sensor.ColorFrameSource.FrameDescription.Width;
                        xyhead0.Y *= (float)kinectView.CameraImage.ActualHeight / kinectView.KinectOne.Camera.Sensor.ColorFrameSource.FrameDescription.Height;

                        // sistemo il punto perchè le dimensione della camera e diverso da SkeletonCanvas
                        xyhead0.X += (float)(kinectView.SkeletonCanvas.ActualWidth - kinectView.CameraImage.ActualWidth) / 2;
                        xyhead0.Y +=(float)(kinectView.SkeletonCanvas.ActualHeight - kinectView.CameraImage.ActualHeight) / 2;

                        //// Avoid exceptions based on bad tracking
                        if (float.IsInfinity(xyhead0.X) || float.IsInfinity(xyhead0.Y)) return;

                        if (sceneSubjects[countSubject].id != 0)
                        {
                            var dr = new Label
                            {
                                Foreground = Brushes.Blue,
                                FontSize = 20,
                                Content = sceneSubjects[countSubject].id, //+"------"+ ((float)Math.Round((Math.Atan((body.Joints[JointType.HandRight].Position.X / body.Joints[JointType.HandRight].Position.Z)) * (180 / (Math.PI))), 2)).ToString(),
                                Opacity = 1,
                                Margin = new Thickness(xyhead0.X, (float)(xyhead0.Y - 150), 0, 0)
                            };

                            kinectView.Canvas_Robot.Children.Add(dr);

                            var dr1 = new Label
                            {
                                Foreground = Brushes.Blue,
                                FontSize = 20,
                                Content = sceneSubjects[countSubject].name[0], //+"------"+ ((float)Math.Round((Math.Atan((body.Joints[JointType.HandRight].Position.X / body.Joints[JointType.HandRight].Position.Z)) * (180 / (Math.PI))), 2)).ToString(),
                                Opacity = 1,
                                Margin = new Thickness(xyhead0.X, (float)(xyhead0.Y - 120), 0, 0)
                            };

                            kinectView.Canvas_Robot.Children.Add(dr1);

                        }
                        else
                        {
                            var dr = new Label
                            {
                                Foreground = Brushes.Red,
                                FontSize = 25,
                                FontWeight= FontWeights.Bold,
                                Content = sceneSubjects[countSubject].idKinect,//  +"------"+ sceneSubjects[i].angle.ToString() +"------"+sceneSubjects[i].head.Z.ToString("F2"),
                                Opacity = 1,
                                Margin = new Thickness(xyhead0.X, (float)(xyhead0.Y - 150), 0, 0)
                            };

                            kinectView.Canvas_Robot.Children.Add(dr);


                        }

                       // Console.WriteLine(sceneSubjects[i].head.X+";"+ sceneSubjects[i].head.Y+";" +sceneSubjects[i].head.Z);
                        

                        #endregion




                       //Console.WriteLine(DifferencePoints(sceneSubjects[i].handTip.left,sceneSubjects[i].handTip.right));
                       //Console.WriteLine(DifferencePoints(sceneSubjects[i].thumb.left,sceneSubjects[i].thumb.right));
                        //Console.WriteLine("Distance> " + sceneSubjects[i].head.Z + " UserAngle> " + sceneSubjects[i].angle+ " Delta> " + ((sceneSubjects[i].angle) - (environment.soundAngle)));
                        #region Gesture


                        //public enum gesture {none, raiseHand, YEAH, think, armsCrossed,pray, hand };

                        if (sceneSubjects[countSubject].head != null && sceneSubjects[countSubject].spineMid != null)
                        {
                            sceneSubjects[countSubject].gesture = 0;

                            // TRUE if the right or left hand is over the treshold
                            // FALSE if the subject is not trackable (i.e. the head joint is equals to the spin joint), See Tracked Passive Subjects
                            //double treshold_old = sceneSubjects[i].head.Y - ((sceneSubjects[i].head.Y - sceneSubjects[i].spineMid.Y) / 3);

                            double treshold = sceneSubjects[countSubject].head.Y + (sceneSubjects[countSubject].head.Y - sceneSubjects[countSubject].spineShoulder.Y) * 0.75;

                            if ((DifferencePoints(sceneSubjects[countSubject].head, sceneSubjects[countSubject].handTip.right) < 0.20) ^ (DifferencePoints(sceneSubjects[countSubject].head, sceneSubjects[countSubject].handTip.left) < 0.20))
                            {
                                sceneSubjects[countSubject].gesture = 3;
                            }
                            else if (((sceneSubjects[countSubject].hand.right.Y > treshold) ^ (sceneSubjects[countSubject].hand.left.Y > treshold)) && (sceneSubjects[countSubject].head.Y - sceneSubjects[countSubject].spineMid.Y) > 0.05)
                            {
                                sceneSubjects[countSubject].gesture = 1;
                            }
                            else if (DifferencePoints(sceneSubjects[countSubject].wrist.left, sceneSubjects[countSubject].elbow.right) < 0.20 && DifferencePoints(sceneSubjects[countSubject].wrist.right, sceneSubjects[countSubject].elbow.left) < 0.20)
                            {
                                sceneSubjects[countSubject].gesture = 4;
                            }
                            else if (((sceneSubjects[countSubject].hand.right.Y > treshold) && (sceneSubjects[countSubject].hand.left.Y > treshold) && (sceneSubjects[countSubject].hand.right.Y - sceneSubjects[countSubject].hand.left.Y) < 0.15) && (sceneSubjects[countSubject].head.Y - sceneSubjects[countSubject].spineMid.Y) > 0.05)
                            {
                                sceneSubjects[countSubject].gesture = 2;
                            }
                            else if (DifferencePoints(sceneSubjects[countSubject].wrist.left, sceneSubjects[countSubject].wrist.right) < 0.20)
                            {
                                //sceneSubjects[countSubject].gesture = 5;
                            }
                            else if (DifferencePoints(sceneSubjects[countSubject].handTip.right, sceneSubjects[countSubject].thumb.right) < 0.05)
                            {
                                sceneSubjects[countSubject].gesture = 6;
                            }
                            else if (DifferencePoints(sceneSubjects[countSubject].hand.right, sceneSubjects[countSubject].hand.left) > 1.03) 
                            {
                                sceneSubjects[countSubject].gesture = 7;

                            }

                   
                            
                        }

                        #endregion

                        //if (sceneSubjects[i].head != null && sceneSubjects[i].spineMid != null)
                        //{
                        //    sceneSubjects[i].pose = 0;

                                               
                        //    if(AngleBetween3D(sceneSubjects[i].shoulder.right, sceneSubjects[i].hip.right, sceneSubjects[i].knee.right)<130 && AngleBetween3D(sceneSubjects[i].shoulder.left, sceneSubjects[i].hip.left, sceneSubjects[i].knee.left)<130)
                        //        sceneSubjects[i].pose = 1;

                        //}

                        //countSubject++;
                    }
                }
                
                environment.numberSubject = sceneSubjects.Count;

                WriteStack();


                ReadSubject = false;

                
            }
        }

        
        private void WriteStack()
        {
            #region View subject

            int idx = 0;
            StringBuilder[] sbGeneric = new StringBuilder[6];
            foreach (Subject subj in sceneSubjects.ToList())
            {
                #region
                if (idx >= 6)
                    break;

                sbGeneric[idx] = new StringBuilder("");
                foreach (System.Reflection.PropertyInfo prop in typeof(Subject).GetProperties())
                {
                    object val = typeof(Subject).GetProperty(prop.Name).GetValue(subj, null);
                    if (val != null)
                    {

                        if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            System.Collections.IList l = (System.Collections.IList)val;
                            sbGeneric[idx].AppendFormat(" {0}:", prop.Name);
                            sbGeneric[idx].AppendFormat("\n     (");
                            foreach (object elem in l)
                            {
                                if (elem.ToString() != null)
                                {
                                    if (elem.ToString().Length > 4)
                                        sbGeneric[idx].AppendFormat(" {0},", elem.ToString().Substring(0, 4));
                                    else
                                        sbGeneric[idx].AppendFormat(" {0},", elem.ToString());
                                }
                            }
                            sbGeneric[idx].AppendFormat(")\n");
                        }
                        else if (prop.PropertyType.IsClass && prop.PropertyType.Name == "Limb")
                        {


                            Limb li = (Limb)val;

                            sbGeneric[idx].AppendFormat(" {0}:", prop.Name);
                            sbGeneric[idx].AppendFormat(" (Left :");
                            sbGeneric[idx].AppendFormat(" {0},", li.left.X.ToString("F3"));
                            sbGeneric[idx].AppendFormat(" {0},", li.left.Y.ToString("F3"));
                            sbGeneric[idx].AppendFormat(" {0}", li.left.Z.ToString("F3"));
                            sbGeneric[idx].AppendFormat(")\n         (Right :");
                            sbGeneric[idx].AppendFormat(" {0},", li.right.X.ToString("F3"));
                            sbGeneric[idx].AppendFormat(" {0},", li.right.Y.ToString("F3"));
                            sbGeneric[idx].AppendFormat(" {0}", li.right.Z.ToString("F3"));
                            sbGeneric[idx].AppendFormat(")\n");

                        }
                        else if (prop.PropertyType.IsClass && prop.PropertyType.Name == "Position")
                        {
                            Position pos = (Position)val;
                            sbGeneric[idx].AppendFormat(" {0}:", prop.Name);
                            sbGeneric[idx].AppendFormat(" (");
                            sbGeneric[idx].AppendFormat(" {0},", pos.X.ToString("F3"));
                            sbGeneric[idx].AppendFormat(" {0},", pos.Y.ToString("F3"));
                            sbGeneric[idx].AppendFormat(" {0}", pos.Z.ToString("F3"));
                            sbGeneric[idx].AppendFormat(")\n");
                        }
                        else
                        {
                            if (prop.Name == "gesture")
                                sbGeneric[idx].AppendFormat(" {0} : {1} \n", prop.Name, GetGestureName((int)val));
                            else
                                sbGeneric[idx].AppendFormat(" {0} : {1} \n", prop.Name, val.ToString());
                        }
                    }
                }
                idx++;
                #endregion
            }

            if (idx >= 6)
                return;

            List<Subject> sceneSubjectsShorecopy = new List<Subject>() { };
            sceneSubjectsShorecopy = sceneSubjectsShore.ToList();
            foreach (Subject subjShore in sceneSubjectsShorecopy.ToList())
            {
                #region
                if (idx >= 6)
                    break;

                sbGeneric[idx] = new StringBuilder("");
                foreach (System.Reflection.PropertyInfo prop in typeof(Subject).GetProperties())
                {
                    object val = typeof(Subject).GetProperty(prop.Name).GetValue(subjShore, null);
                    if (val != null)
                    {

                        if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            System.Collections.IList l = (System.Collections.IList)val;
                            sbGeneric[idx].AppendFormat(" {0}:", prop.Name);
                            sbGeneric[idx].AppendFormat("\n     (");           
                            foreach (object elem in l)
                            {
                                if (elem.ToString() != null)
                                {
                                    if (elem.ToString().Length > 4)
                                        sbGeneric[idx].AppendFormat(" {0},", elem.ToString().Substring(0, 4));
                                    else
                                        sbGeneric[idx].AppendFormat(" {0},", elem.ToString());
                                }
                            }
                            sbGeneric[idx].AppendFormat(")\n");
                        }
                        else if (prop.PropertyType.IsClass && prop.PropertyType.Name =="Limb")
                        {
                           
                           
                            Limb li = (Limb)val;

                            sbGeneric[idx].AppendFormat(" {0}:", prop.Name);
                            sbGeneric[idx].AppendFormat(" (Left :");
                            sbGeneric[idx].AppendFormat(" {0},", li.left.X.ToString("F3"));
                            sbGeneric[idx].AppendFormat(" {0},", li.left.Y.ToString("F3"));
                            sbGeneric[idx].AppendFormat(" {0}", li.left.Z.ToString("F3"));
                            sbGeneric[idx].AppendFormat(")\n         (Right :");
                            sbGeneric[idx].AppendFormat(" {0},", li.right.X.ToString("F3"));
                            sbGeneric[idx].AppendFormat(" {0},", li.right.Y.ToString("F3"));
                            sbGeneric[idx].AppendFormat(" {0}", li.right.Z.ToString("F3"));
                            sbGeneric[idx].AppendFormat(")\n");
                          
                        }
                        else if (prop.PropertyType.IsClass && prop.PropertyType.Name == "Position")
                        {
                            Position pos = (Position)val;
                            sbGeneric[idx].AppendFormat(" {0}:", prop.Name);
                            sbGeneric[idx].AppendFormat(" (");
                            sbGeneric[idx].AppendFormat(" {0},", pos.X.ToString("F3"));
                            sbGeneric[idx].AppendFormat(" {0},", pos.Y.ToString("F3"));
                            sbGeneric[idx].AppendFormat(" {0}", pos.Z.ToString("F3"));
                            sbGeneric[idx].AppendFormat(")\n");
                        }
                        else
                        {
                            if(prop.Name=="gesture")
                                sbGeneric[idx].AppendFormat(" {0} : {1} \n", prop.Name, GetGestureName((int)val));
                            else
                                sbGeneric[idx].AppendFormat(" {0} : {1} \n", prop.Name, val.ToString());
                        }
                    }
                }
                idx++;
                #endregion
            }

            #endregion

            SubjParamsPanel.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                new Action(delegate()
                {
                    Subj1.Content = sbGeneric[0];
                    Subj2.Content = sbGeneric[1];
                    Subj3.Content = sbGeneric[2];
                    Subj4.Content = sbGeneric[3];
                    Subj5.Content = sbGeneric[4];
                    Subj6.Content = sbGeneric[5];
                }
            ));

            #region View enviroment

            StringBuilder sbGen = new StringBuilder();
            foreach (System.Reflection.PropertyInfo prop in typeof(Surroundings).GetProperties())
            {
                object val = typeof(Surroundings).GetProperty(prop.Name).GetValue(environment, null);
                if (val != null)
                {
                    if (prop.PropertyType.IsGenericType)
                    {
                        System.Collections.IList l = (System.Collections.IList)val;
                        sbGen.AppendFormat(" {0}:( ", prop.Name);
                        foreach (object elem in l)
                        {
                            if (elem.ToString() != null)
                            {
                                if (elem.ToString().Length > 4)
                                    sbGen.AppendFormat(" {0},", elem.ToString().Substring(0, 4));
                                else
                                    sbGen.AppendFormat(" {0},", elem.ToString());
                            }
                        }
                        sbGen.AppendFormat(")\n");
                    }
                    else if (prop.PropertyType.IsClass && prop.PropertyType.Name == "SOME")
                    {
                        SOME some = (SOME)val;
                        sbGen.AppendFormat(" {0}:", prop.Name);
                        sbGen.AppendFormat(" (Mic: ");
                        sbGen.AppendFormat(" {0}\n", some.mic.ToString());
                        sbGen.AppendFormat("        Lux: ");
                        sbGen.AppendFormat(" {0}\n", some.lux.ToString());
                        sbGen.AppendFormat("        Temp :");
                        sbGen.AppendFormat(" {0}\n", some.temp.ToString());
                        sbGen.AppendFormat("        IR :");
                        sbGen.AppendFormat(" {0}\n", some.IR.ToString());
                        sbGen.AppendFormat("        Touch :");
                        sbGen.AppendFormat(" {0}", some.touch.ToString());
                        sbGen.AppendFormat(")\n ");

                    }
                    else if (prop.PropertyType.IsClass && prop.PropertyType.Name == "Saliency")
                    {
                        Saliency sal = (Saliency)val;
                        if (sal.position != null)
                        {
                            if (sal.position.Count != 0)
                            {
                                sbGen.AppendFormat(" {0}:", prop.Name);
                                sbGen.AppendFormat(" (Pos: ");
                                sbGen.AppendFormat(" {0},", sal.position[0].ToString("F2"));
                                sbGen.AppendFormat(" {0}\n", sal.position[1].ToString("F2"));
                                sbGen.AppendFormat("        Weight: ");
                                sbGen.AppendFormat(" {0})\n", sal.saliencyWeight.ToString("F1"));
                            }
                        }
                    }
                    else if (prop.PropertyType.IsClass && prop.PropertyType.Name == "Ambience")
                    {
                        sbGen.AppendFormat(" {0}:", prop.Name);
                        sbGen.AppendFormat("\n ");
                    }
                    else if (prop.PropertyType.IsClass && prop.PropertyType.Name == "Resolution")
                    {
                        Resolution res = (Resolution)val;
                        sbGen.AppendFormat(" {0}:", prop.Name);
                        sbGen.AppendFormat("({0},", res.Width.ToString("F1"));
                        sbGen.AppendFormat(" {0},", res.Height.ToString("F1"));
                        sbGen.AppendFormat(")\n ");
                    }
                    else
                    {
                        sbGen.AppendFormat(" {0} : {1} \n", prop.Name, val.ToString());
                    }
                }
            }

            EnvirParamsPanel.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                new Action(delegate()
                {
                    Envir0.Content = "";
                    Envir0.Content = sbGen;
                }
            ));

            #endregion
        }

        #endregion

        #region Shore


        private void ShoreEngine(object state)
        {
            try
            {

                kinectView.Canvas_Shore.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                new Action(delegate()
                {
                    kinectView.Canvas_Shore.Children.Clear();
                }));
            

                sceneSubjectsCopy = sceneSubjects.ToList();
                sceneSubjectsShore.Clear();
                foreach (Shore shore in shores)
                {
                    #region check corrispondenza subject
                    bool present = false;

                    for (int j = 0; j < sceneSubjectsCopy.Count; j++)
                    {

                        if (sceneSubjectsCopy[j].spineMid != null)
                        {

                            // calcolo il tunto medio degli occhi
                            Point middleEyes = new Point((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y);


                            double shorePointX = MetersToPoint(sceneSubjectsCopy[j].head).X;
                            double ErrorX = shorePointX - middleEyes.X; //spinInCanvas.X - middleEyes.X; //Errore tra la posizione di shore e del kinect

                            Console.WriteLine("Shore" + shorePointX + " -" + middleEyes.X + "=" + ErrorX);

                            if (Math.Abs(ErrorX) < DeltaErrorX)
                            {
                                try
                                {
                                    sceneSubjects[j].gender = (shore.Gender == "Female") ? "Female" : "Male";
                                    sceneSubjects[j].age = shore.Age;
                                    sceneSubjects[j].happiness_ratio = shore.Happiness_ratio;
                                    sceneSubjects[j].surprise_ratio = shore.Surprise_ratio;
                                    sceneSubjects[j].anger_ratio = shore.Anger_ratio;
                                    sceneSubjects[j].sadness_ratio = shore.Sadness_ratio;
                                    sceneSubjects[j].uptime = shore.Uptime;

                                    present = true;

                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Shore Error" + e.Message);
                                }

                                //UpdateSubjectInfoShore(sObj, j);
                                break;
                            }
                            else
                            {
                                //Console.WriteLine(ErrorX);
                            }

                        }
                    }


                    //if (!present)
                    //{

                    //    Subject newSub = new Subject();
                    //    newSub.idKinect = 0;
                    //    newSub.angle = 0;
                    //    newSub.gender = (shore.Gender == "Female") ? "Female" : "Male";
                    //    newSub.age = shore.Age;
                    //    newSub.happiness_ratio = shore.Happiness_ratio;
                    //    newSub.surprise_ratio = shore.Surprise_ratio;
                    //    newSub.anger_ratio = shore.Anger_ratio;
                    //    newSub.sadness_ratio = shore.Sadness_ratio;
                    //    newSub.uptime = shore.Uptime;

                    //    newSub.trackedState = false;
                    //    newSub.ankle.left = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y,0.0f);
                    //    newSub.ankle.right = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.elbow.left = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.elbow.right = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.foot.left = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.foot.right = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.hand.left = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.hand.right = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.handTip.left = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.handTip.right = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.head = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.hip.left = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.hip.right = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.knee.left = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.knee.right = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.neck = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.shoulder.left = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.shoulder.right = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.spineBase = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.spineMid = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.spineShoulder = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.thumb.left = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.thumb.right = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.wrist.left = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.wrist.right = new Position((shore.Eyes.Left.X + shore.Eyes.Right.X) / 2, shore.Eyes.Left.Y, 0.0f);
                    //    newSub.angle = 0.0f;
                    //    newSub.speak_prob = 0.0f;


                    //    sceneSubjectsShore.Add(newSub);


                    //}
                    #endregion



                    if (ShoreView == true)
                    {
                        Dictionary<string, float> expRatio = new Dictionary<string, float>() 
                        {
                            { "Angry", shore.Anger_ratio },
                            { "Happy", shore.Happiness_ratio },
                            { "Sad", shore.Sadness_ratio },
                            { "Surprised", shore.Surprise_ratio }
                        };


                        float ratioVal = (float)Math.Round((decimal)expRatio.Values.Max(), 1);
                        string ratioName = expRatio.OrderByDescending(kvp => kvp.Value).First().Key;

                        // Draw subject information: Gender, age +/- deviation, expression, expression rate
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine(shore.Gender);
                        sb.AppendLine("Age: " + shore.Age + " +/- " + shore.Age_deviation);
                        sb.AppendLine(ratioName + ": " + ratioVal + "%");

                        double width = Math.Abs(shore.Region_face.Left - shore.Region_face.Right);
                        double height = Math.Abs(shore.Region_face.Top - shore.Region_face.Bottom);

                        double left = shore.Region_face.Left;
                        double top = shore.Region_face.Top;
                        double bottom = shore.Region_face.Bottom;
                        string gender = shore.Gender;



                        //scala per mantenere le proporzioni del punto rispetto alla dimensione della cameraImage
                        width *= (float)(kinectView.CameraImage.ActualWidth / (float)environment.resolution.Width);
                        height *= (float)(kinectView.CameraImage.ActualHeight / (float)environment.resolution.Height);

                        left *= (float)(kinectView.CameraImage.ActualWidth / (float)environment.resolution.Width);
                        top *= (float)(kinectView.CameraImage.ActualHeight / (float)environment.resolution.Height);

                        left += (float)(kinectView.Canvas_Shore.ActualWidth - kinectView.CameraImage.ActualWidth) / 2;
                        top += (float)(kinectView.Canvas_Shore.ActualHeight - kinectView.CameraImage.ActualHeight) / 2;

                        kinectView.Canvas_Shore.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                            new Action(delegate()
                            {
                                Rectangle rect = new Rectangle();
                                //rect.Name = sceneSubjectsCopy[j].id.ToString();
                                rect.Width = width;
                                rect.Height = height;
                                rect.StrokeThickness = 2;
                                rect.Stroke = (gender == "Female") ? Brushes.Fuchsia : Brushes.Cyan;
                                rect.Margin = new Thickness(left, top, 0, 0); //draw the rectangle

                                Label lab = new Label
                                {
                                    Foreground = (gender == "Female") ? Brushes.Fuchsia : Brushes.Cyan,
                                    FontSize = 12,
                                    FontWeight = FontWeights.Bold,
                                    Content = sb.ToString(),
                                    Opacity = 1,
                                    Margin = new Thickness(left, top + height, 0, 0) //draw under the rectangle
                                };


                                kinectView.Canvas_Shore.Children.Add(rect);
                                kinectView.Canvas_Shore.Children.Add(lab);


                            }));
                    }
                }


            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error ShoreEngine");
            }
            shores.Clear();
        }

        #endregion

        #region SubjectRecognition

        void SubjectRecognition(object state) 
        {
           

            
            RecognizedQRCode subjectsRecognized = ComUtils.JsonUtils.Deserialize<RecognizedQRCode>(receiveSubjectRecognized);


            kinectView.MarkerQRCode.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                   new Action(delegate()
                   {
                       kinectView.MarkerQRCode.Children.Clear();
                      

                   }));

            if (subjectsRecognized == null)
                return;
            

            double middleQRcodeX = 0;
            double middleQRcodeY = 0;

            foreach (InfoQRCode subRec in subjectsRecognized.InfoQRCode)
            {
              
              


                middleQRcodeX = (Convert.ToDouble(subRec.Positions[0].X) + Convert.ToDouble(subRec.Positions[1].X)) / 2;
                middleQRcodeY = (Convert.ToDouble(subRec.Positions[1].Y) + Convert.ToDouble(subRec.Positions[2].Y)) / 2;

                Pos middleQRcodeIntoPos = new Pos() { };
                middleQRcodeIntoPos.X = middleQRcodeX.ToString();
                middleQRcodeIntoPos.Y = middleQRcodeY.ToString();

                Pos middleQRcodeIntoPosScale = scale(middleQRcodeIntoPos, subjectsRecognized.ResolutionCam);
                drawEllipse(kinectView.MarkerQRCode,Convert.ToDouble(middleQRcodeIntoPosScale.X) ,Convert.ToDouble( middleQRcodeIntoPosScale.Y) , 10, 10, (float)2.0);

             
                lock (lockObject)
                {
                    for (int j = 0; j < sceneSubjects.Count; j++)
                    {
                        if (sceneSubjects[j].name[0] == "unknown")
                        {

                            double ErrorX = MetersToPoint(sceneSubjects[j].spineBase).X - Convert.ToDouble(middleQRcodeIntoPosScale.X); //Errore tra la posizione di shore e del kinect
                            if (Math.Abs(ErrorX) < 50)
                            {
                                string[] msg = subRec.Message.Substring(1, subRec.Message.Length - 2).Split('|');
                                sceneSubjects[j].id = Convert.ToInt32(msg[0]);
                                sceneSubjects[j].name = new List<string> { msg [1]};
                                break;
                            }

                        }
                    }
                }


                 middleQRcodeX = 0;
                 middleQRcodeY = 0;
            }
        }

        #endregion

        void SensorRecognized(object state) 
        {
            string[] sensor = receiveSensorData.Split(' ');
            if (sensor.Length == 0)
                return;
        }


        #region UDP
        private void ListenUDPButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(UDP_ip.Text.Trim()) && !String.IsNullOrEmpty(UDP_port.Text.Trim()))
            {
                try
                {
                    IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(UDP_ip.Text.Trim()), Convert.ToInt32(UDP_port.Text.Trim()));
                    IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    ManualResetEvent wait = new ManualResetEvent(false);

                    UdpClient listener = new UdpClient(localEndPoint);

                    receiverUDP = new System.Threading.Thread(() =>
                    {
                        while (true)
                        {
                            while (listener.Available != 0)
                            {
                                byte[] received = listener.Receive(ref remoteIpEndPoint);
                                // int code = Int32.Parse(Encoding.ASCII.GetString(received));

                                //System.Diagnostics.Debug.WriteLine("[" + count + "]" + Encoding.ASCII.GetString(received));
                                //count++;

                                string SOMEinfo = Encoding.ASCII.GetString(received);
                                string[] TOI = SOMEinfo.Split(',');

                                if (TOI.Length == 5)
                                {
                                    environment.toi.mic = Int32.Parse(TOI[0]);
                                    environment.toi.lux = Int32.Parse(TOI[1]);
                                    environment.toi.IR = Int32.Parse(TOI[2]);
                                    environment.toi.temp = Int32.Parse(TOI[3]);
                                    if (Int32.Parse(TOI[4]) == 1)
                                        environment.toi.touch = true;
                                    else
                                        environment.toi.touch = false;
                                }
                                else
                                    Console.WriteLine("Errore String SOME: " + SOMEinfo);
                            }

                        }
                    });
                    receiverUDP.Start();

                    ListenUDPButton.Visibility = Visibility.Collapsed;
                    StopUDPButton.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Listen UDP Port");
                    System.Console.WriteLine(ex.Message);
                }
            }
            else
                MessageBox.Show("Error SOME Address");
        }

        private void StopUDPButton_Click(object sender, RoutedEventArgs e)
        {
            receiverUDP.Abort();
            ListenUDPButton.Visibility = Visibility.Visible;
            StopUDPButton.Visibility =  Visibility.Collapsed;
        }
        
        #endregion

        #region Yarp

        private void SendData()
        {
            string sceneData = null;

                   
            List<ObjectScene> objects = new List<ObjectScene> { sceneObjects[0] };
            while (true)
            {
               

                //se sto aggiornando i dati (SKeletonRead)non faccio la lettura e non invio nulla
                if (!ReadSubject)
                    continue;

                using (Scene scene = new Scene())
                {
                    try
                    {
                        scene.Subjects = sceneSubjects.ToList();
                        scene.Objects = objects;
                        scene.Environment = environment;
                    }
                    catch { }

                    sceneData = ComUtils.XmlUtils.Serialize<Scene>(scene);


                    //string time = string.Format("{0:mm\\:ss\\:fff}", stopwatch.Elapsed);
                    //sceneData = "(" + time + ")" + sceneData;

                    //str += sceneData + "\r\n";
                    Console.WriteLine(sceneData);
                    yarpPortSceneXML.sendData(sceneData);
                    //using (StreamWriter w = File.AppendText("log.txt"))
                    //{
                    //    w.WriteLine(sceneData);
                    //}

                    yarpPortSceneOPC.sendComandOPC("add", scene.getstringOPC());

                    if (client != null)
                    {
                        byte[] data = Encoding.UTF8.GetBytes(sceneData);
                        client.Send(data, data.Length);
                    }

                    sceneData = null;// se non cancello col tempo mi occupa tutta la memoria


                }

               
            }

        }

        void ReceiveDataLookAt(object sender, ElapsedEventArgs e)
        {
            yarpPortLookAt.receivedData(out receiveLookAtData);

            if (receiveLookAtData != null && receiveLookAtData != "")
            {
                try
                {
                    winner = ComUtils.XmlUtils.Deserialize<Winner>(receiveLookAtData);
                    //Console.WriteLine(receiveLookAtData);//check winner data
                }
                catch (Exception exc)
                {
                    Console.WriteLine("Error XML Winner: " + exc.Message);
                }

                System.Threading.Thread t1 = new System.Threading.Thread(delegate()
                {
                  
                    this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                        new Action(delegate()
                        {
                         
                            winner.spinX *= (float)kinectView.SkeletonCanvas.ActualWidth;
                            winner.spinY = (float)kinectView.SkeletonCanvas.ActualHeight * (1 - winner.spinY);


                            winner.spinX += ((float)kinectView.SkeletonCanvas.ActualWidth - (float)kinectView.CameraImage.ActualWidth) / 2;
                            winner.spinY += ((float)kinectView.SkeletonCanvas.ActualHeight - (float)kinectView.CameraImage.ActualHeight) / 2;
                       
                            Canvas.SetLeft(kinectView.ViewPoint, winner.spinX );
                            Canvas.SetTop(kinectView.ViewPoint, winner.spinY);
                         
                        }
                    ));

                });
                t1.Priority = ThreadPriority.Lowest;
                t1.Start();
            }
        }

        void ReceiveDataSaliency(object sender, ElapsedEventArgs e)
        {
            yarpPortSaliency.receivedData(out receiveSaliencyData);

            if (receiveSaliencyData != null && receiveSaliencyData != "")
            {
                try
                {
                    saliency = ComUtils.XmlUtils.Deserialize<Saliency>(receiveSaliencyData);
                    //Console.WriteLine(receiveSaliencyData);//check winner data
                }
                catch (Exception exc)
                {
                    Console.WriteLine("Error XML Saliency: " + exc.Message);
                }

                Point point = new Point();
                point.X = saliency.position[0];
                point.Y = saliency.position[1];

                environment.saliency = new Saliency((uint)point.X, (uint)point.Y);

                if (SaliencyView)
                {

                    point.X *= (float)(kinectView.CameraImage.ActualWidth / (float)environment.resolution.Width);
                    point.Y *= (float)(kinectView.CameraImage.ActualHeight / (float)environment.resolution.Height);

                    point.X += (float)(kinectView.Point.ActualWidth - kinectView.CameraImage.ActualWidth) / 2;
                    point.Y += (float)(kinectView.Point.ActualHeight - kinectView.CameraImage.ActualHeight) / 2;



                    Dispatcher.BeginInvoke((System.Threading.ThreadStart)delegate
                    {
                        Canvas.SetLeft(kinectView.salientPoint, point.X);
                        Canvas.SetTop(kinectView.salientPoint, point.Y);
                    });

                }
            }
        }

        void ReceiveDataShore(object sender, ElapsedEventArgs e)
        {

            kinectView.Canvas_Shore.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
            new Action(delegate()
            {
                kinectView.Canvas_Shore.Children.Clear();
            }));

            yarpPortShore.receivedData(out receiveShoreData);

            if (receiveShoreData != null && receiveShoreData != "")
            {
                try
                {
                    shores = ComUtils.XmlUtils.Deserialize<List<Shore>>(receiveShoreData);

               
                    ThreadPool.QueueUserWorkItem(ShoreEngine);
                    //Console.WriteLine(receiveLookAtData);//check winner data
                }
                catch (Exception exc)
                {
                    Console.WriteLine("Error XML Shore: " + exc.Message);
                }

               
            }
        }
  
        void ReceiveDataRecognized(object sender, ElapsedEventArgs e)
        {
            if (sceneSubjects.FindAll(a => a.id == 0).Count > 0)
            {
                yarpPortRecognized.receivedData(out receiveSubjectRecognized);

                if (!String.IsNullOrEmpty(receiveSubjectRecognized))
                    ThreadPool.QueueUserWorkItem(SubjectRecognition);
            }
        }

        void ReceiveDataSensor(object sender, ElapsedEventArgs e)
        {
            if (sceneSubjects.Count > 0)
            {
                yarpPortSensor.receivedData(out receiveSensorData);

                if (!String.IsNullOrEmpty(receiveSensorData))
                    ThreadPool.QueueUserWorkItem(SensorRecognized);
            }
        }

        void CheckYarpConnections(object source, ElapsedEventArgs e)
        {
            #region PortExists-> attentionModuleOut_lookAt
            if (yarpPortSceneXML != null && yarpPortSceneXML.PortExists(attentionModuleOut_lookAt))
            {
                
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate() {
                        if (AttentionModStatus.Fill == Brushes.Red)
                        {
                            AttentionModStatus.Fill = Brushes.Green;
                            kinectView.ViewPoint.Visibility = Visibility.Visible;
                            yarpReceiverLookAt.Start();
                        }
                    }));
            }
            else if (yarpPortSceneXML != null && !yarpPortSceneXML.PortExists(attentionModuleOut_lookAt))
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate() 
                        { 
                            if(AttentionModStatus.Fill == Brushes.Green)
                            {
                                AttentionModStatus.Fill = Brushes.Red;
                                kinectView.ViewPoint.Visibility = Visibility.Hidden;
                                if (yarpReceiverLookAt != null)
                                {
                                    yarpReceiverLookAt.Elapsed -= new ElapsedEventHandler(ReceiveDataLookAt);
                                    yarpReceiverLookAt.Stop();
                                }
                            }
                    }));
            }
            #endregion

            #region PortExists->FaceRecognitionOut
            if (yarpPortSceneXML != null && yarpPortSceneXML.PortExists(FaceRecognitionOut))
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate()
                    {
                        if (FaceRecognitionStatus.Fill == Brushes.Red)
                        {
                            FaceRecognitionStatus.Fill = Brushes.Green;
                            if(SubjectCheckbox.IsChecked==true)
                                yarpReceiverSubject.Start();

                            //ViewPoint.Visibility = Visibility.Visible;
                        }
                    }));
            }
            else if (yarpPortSceneXML != null && !yarpPortSceneXML.PortExists(FaceRecognitionOut))
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate()
                    {
                        if (FaceRecognitionStatus.Fill == Brushes.Green)
                        {
                            FaceRecognitionStatus.Fill = Brushes.Red;
                            yarpReceiverSubject.Stop();
                            //ViewPoint.Visibility = Visibility.Hidden;
                        }
                    }));
            }
            #endregion

            #region PortExists->ShoreOUT
            if (yarpPortSceneXML != null && yarpPortSceneXML.PortExists(ShoreOUT))
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate()
                    {
                        if (ShoreStatus.Fill == Brushes.Red)
                        {
                            ShoreStatus.Fill = Brushes.Green;
                            if(FacialExpCheckbox.IsChecked==true)
                                yarpReceiverShore.Start();

                            //ViewPoint.Visibility = Visibility.Visible;
                        }
                    }));
            }
            else if (yarpPortSceneXML != null && !yarpPortSceneXML.PortExists(ShoreOUT))
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate()
                    {
                        if (ShoreStatus.Fill == Brushes.Green)
                        {
                            ShoreStatus.Fill = Brushes.Red;
                            yarpReceiverShore.Stop();
                            //ViewPoint.Visibility = Visibility.Hidden;
                        }
                    }));
            }
            #endregion


            #region PortExists->SaliencyOUT
            if (yarpPortSceneXML != null && yarpPortSceneXML.PortExists(SaliencyOUT))
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate()
                    {
                        if (SaliencyStatus.Fill == Brushes.Red)
                        {
                            SaliencyStatus.Fill = Brushes.Green;
                            if(SaliencyCheckbox.IsChecked==true)
                                yarpReceiverSaliency.Start();
                        }
                    }));
            }
            else if (yarpPortSceneXML != null && !yarpPortSceneXML.PortExists(SaliencyOUT))
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate()
                    {
                        if (SaliencyStatus.Fill == Brushes.Green)
                        {
                            SaliencyStatus.Fill = Brushes.Red;
                            yarpReceiverSaliency.Stop();
                            //ViewPoint.Visibility = Visibility.Hidden;
                        }
                    }));
            }
            #endregion

            #region NetworkExists
            if (yarpPortSceneXML != null && yarpPortSceneXML.NetworkExists())
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate() 
                        { 
                            if(YarpServerStatus.Fill == Brushes.Red)
                                YarpServerStatus.Fill = Brushes.Green; 
                        }));
            }
            else if (yarpPortSceneXML != null && !yarpPortSceneXML.NetworkExists())
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate() 
                        {
                            if (YarpServerStatus.Fill == Brushes.Green)
                                YarpServerStatus.Fill = Brushes.Red; 
                        }));
            }
            #endregion
        }

        #endregion

        #region Texboxes Delta

        private void ErrorX_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render,
                new Action(delegate()
            {
                DeltaErrorX = (ErrorX.Text.Trim().Length != 0) ? double.Parse(ErrorX.Text.Trim()) : 100;
            }));
        }

        private void TimeSubject_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render,
             new Action(delegate()
             {
                 TimeSubj = (TimeSubject.Text.Trim().Length != 0) ? double.Parse(TimeSubject.Text.Trim()) : 15;
             }));
        }

        private void DistanceSub_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render,
             new Action(delegate()
             {
                 DistanceSubj = (DistanceSub.Text.Trim().Length != 0) ? double.Parse(DistanceSub.Text.Trim()) : 0.1;
             }));
        }

        #endregion

        #region Checkboxes

        private void CheckboxFacialexp_Checked(object sender, RoutedEventArgs e)
        {
            kinectView.Canvas_Shore.Children.Clear();

            if (Process.GetProcessesByName("ShoreModule").Length == 0)
            {
                shoreModule.StartInfo.FileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/ShoreModule.exe";
                shoreModule.Start();
            }
            else
                shoreModule = Process.GetProcessesByName("ShoreModule")[0];

            ErrorX.IsEnabled = true;
            viewShore.IsEnabled = true;
          
           
        }

        private void CheckboxFacialexp_UnChecked(object sender, RoutedEventArgs e)
        {
            kinectView.Canvas_Shore.Children.Clear();
          
            shoreModule.CloseMainWindow();
            PanelShore.IsEnabled = false;
            viewShore.IsEnabled = false;
            viewShore.IsChecked = false;

            sceneSubjectsShore.Clear();
         
        }

        private void Saliency_Checked(object sender, RoutedEventArgs e)
        {
            
            kinectView.salientPoint.Visibility = Visibility.Visible;

            //Process[] pp = Process.GetProcesses();

            //Process[] p =Process.GetProcessesByName("SaliencyModule");

            if (Process.GetProcessesByName("SaliencyModule").Length == 0)
            {
                saliencyModule.StartInfo.FileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/SaliencyModule.exe";
                saliencyModule.Start();
            }
            else
                saliencyModule = Process.GetProcessesByName("SaliencyModule")[0];

            viewSaliency.IsEnabled = true;
        }
   
        private void Saliency_Unchecked(object sender, RoutedEventArgs e)
        {
            kinectView.salientPoint.Visibility = Visibility.Hidden;
            yarpReceiverSaliency.Stop();
           // environment.saliency.position.Clear();

            saliencyModule.CloseMainWindow();
            viewSaliency.IsChecked = false;
            viewSaliency.IsEnabled = false;
           
        }


        private void cbCalibLeft_Checked(object sender, RoutedEventArgs e)
        {
            StackCab.IsEnabled = true;
            kinectView.CalibrationPoint1.Visibility = Visibility.Visible;
          


            Dispatcher.BeginInvoke((System.Threading.ThreadStart)delegate
            {
                double pointX = (kinectView.Point.ActualWidth) / 1920.00;
                double pointY = (kinectView.Point.ActualHeight) / 1080.00;

                double alfaX = (1920.0 - 1280.0) / 2.0;
                double alfaY = (1080.0 - 720.0) / 2.0;


                Canvas.SetLeft(kinectView.CalibrationPoint1, ((200 + alfaX) * pointX) - 7.5);
                Canvas.SetTop(kinectView.CalibrationPoint1, ((100 + alfaY) * pointY) - 7.5);



            });
        }

        private void cbCalibLeft_Unchecked(object sender, RoutedEventArgs e)
        {
            StackCab.IsEnabled = false;
            kinectView.CalibrationPoint1.Visibility = Visibility.Hidden;
        }

        private void cbCalibCenter_Checked(object sender, RoutedEventArgs e)
        {

            StackCabCenter.IsEnabled = true;
            kinectView.CalibrationPoint2.Visibility = Visibility.Visible;


            Dispatcher.BeginInvoke((System.Threading.ThreadStart)delegate
            {
                double pointX = (kinectView.Point.ActualWidth) / 1920.00;
                double pointY = (kinectView.Point.ActualHeight) / 1080.00;

                double alfaX = (1920.0 - 1280.0) / 2.0;
                double alfaY = (1080.0 - 720.0) / 2.0;


              
                Canvas.SetLeft(kinectView.CalibrationPoint2, ((640 + alfaX) * pointX) - 7.5);
                Canvas.SetTop(kinectView.CalibrationPoint2, ((360 + alfaY) * pointY) - 7.5);



                

            });
        }

        private void cbCalibCenter_Unchecked(object sender, RoutedEventArgs e)
        {
            StackCabCenter.IsEnabled = false;
            kinectView.CalibrationPoint2.Visibility = Visibility.Hidden;
        }

        private void cbCalibRight_Checked(object sender, RoutedEventArgs e)
        {
            StackCabRight.IsEnabled = true;
          
            kinectView.CalibrationPoint3.Visibility = Visibility.Visible;


            Dispatcher.BeginInvoke((System.Threading.ThreadStart)delegate
            {
                double pointX = (kinectView.Point.ActualWidth) / 1920.00;
                double pointY = (kinectView.Point.ActualHeight) / 1080.00;

                double alfaX = (1920.0 - 1280.0) / 2.0;
                double alfaY = (1080.0 - 720.0) / 2.0;


             
                Canvas.SetLeft(kinectView.CalibrationPoint3, ((1080 + alfaX) * pointX) - 7.5);
                Canvas.SetTop(kinectView.CalibrationPoint3, ((620 + alfaY) * pointY) - 7.5);




            });
        }

        private void cbCalibRight_Unchecked(object sender, RoutedEventArgs e)
        {
            StackCabRight.IsEnabled = false;

            kinectView.CalibrationPoint3.Visibility = Visibility.Hidden;

        }

        private void SubjectCheckbox_Checked(object sender, RoutedEventArgs e)
        {

            if (Process.GetProcessesByName("SubjectRecognitionQRCode").Length == 0)
            {
                SubjectRecognitionModule.StartInfo.FileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/SubjectRecognitionQRCode.exe";
                SubjectRecognitionModule.Start();
            }
            else
                SubjectRecognitionModule = Process.GetProcessesByName("SubjectRecognitionQRCode")[0];

        }

        private void SubjectCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                SubjectRecognitionModule.CloseMainWindow();
            }
            catch 
            {
            }
        }

        private void viewShore_Checked(object sender, RoutedEventArgs e)
        {
            ShoreView = true;
        }

        private void viewShore_Unchecked(object sender, RoutedEventArgs e)
        {
            ShoreView = false;
        }

        private void viewSkeleton_Checked(object sender, RoutedEventArgs e)
        {
            SkeletonView = true;
        }

        private void viewSkeleton_Unchecked(object sender, RoutedEventArgs e)
        {
            SkeletonView = false;
        }

        private void viewSaliency_Checked(object sender, RoutedEventArgs e)
        {
            SaliencyView = true;
        }

        private void viewSaliency_Unchecked(object sender, RoutedEventArgs e)
        {
            SaliencyView = false;
        }

        private void FaceTrackingCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            kinectView.KinectOne.InitializeFace();
            kinectView.KinectOne.OnFaceFrameArrived();
        }

        private void FaceTrackingCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            kinectView.KinectOne.OffFaceFrameArrived();
        }

        #endregion

        #region Draw

        public void DrawPointString(string str, double offX, double offY)
        {
            var dr = new Label
            {
                Foreground = Brushes.Red,
                FontSize = 20,
                Content = str,
                Opacity = 1,
                Margin = new Thickness(offX, offY, 0, 0)
            };

            kinectView.Canvas_Robot.Children.Add(dr);
        }

        public void DrawPointString(string str, double offX, double offY, Brush br)
        {
            var dr = new Label
            {
                Foreground = br,
                FontSize = 20,
                Content = str,
                Opacity = 1,
                Margin = new Thickness(offX, offY, 0, 0)
            };

            kinectView.Canvas_Robot.Children.Add(dr);
        }

        public void drawEllipse(System.Windows.Controls.Canvas pb, double x, double y, int w, int h, float Bwidth)
        {
            pb.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                     new Action(delegate()
                     {


                        System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.Red);

                        double pointX = (x * pb.ActualWidth) / 1920.00;
                        double pointY= (y * pb.ActualHeight) / 1080.00;
                        //pointX = pointX - (w / 2);// +Convert.ToInt32(ErrorXc.Text);
                        //pointY = pointY - (h / 2);// +Convert.ToInt32(ErrorYc.Text);

                        System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
                        rect.Width = w;
                        rect.Height = h;
                        rect.Stroke = System.Windows.Media.Brushes.Cyan;
                        rect.Margin = new Thickness(pointX, pointY, 0, 0); //draw the rectangle

                        //Console.WriteLine(dPoint.X + ";" + dPoint.Y);

                        pb.Children.Add(rect);
                     }
             ));
        }

        public void drawEllipse(System.Windows.Controls.Canvas pb, double x, double y, int w, int h, float Bwidth, System.Windows.Media.Brush brush)
        {
            pb.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                     new Action(delegate()
                     {



                         double pointX = (x * pb.ActualWidth) / 1920.00;
                         double pointY = (y * pb.ActualHeight) / 1080.00;
                         //pointX = pointX - (w / 2);// +Convert.ToInt32(ErrorXc.Text);
                         //pointY = pointY - (h / 2);// +Convert.ToInt32(ErrorYc.Text);

                         System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
                         rect.Width = w;
                         rect.Height = h;
                         rect.Stroke = brush;
                         rect.Margin = new Thickness(pointX, pointY, 0, 0); //draw the rectangle

                         //Console.WriteLine(dPoint.X + ";" + dPoint.Y);

                         pb.Children.Add(rect);
                     }
             ));
        }
        #endregion

        #region Close app

        private void StopKinect()
        {
            if (kinectView.KinectOne.Camera.Sensor != null)
            {
                //kinect.sensor.AudioSource.Stop();
                kinectView.Close();
            }
        }

        private void StopYarp()
        {
            #region Thred or timer  
            if (senderThread != null)
                senderThread.Abort();

          
            if (checkYarpStatusTimer != null)
            {
                checkYarpStatusTimer.Elapsed -= new ElapsedEventHandler(CheckYarpConnections);
                checkYarpStatusTimer.Stop();
            }
            //if (checkPortTimer != null)
            //    checkPortTimer.Stop();

            if (yarpReceiverLookAt != null)
            {
                yarpReceiverLookAt.Elapsed -= new ElapsedEventHandler(ReceiveDataLookAt);
                yarpReceiverLookAt.Stop();
            }


            if (yarpReceiverShore != null)
            {
                yarpReceiverShore.Elapsed -= new ElapsedEventHandler(ReceiveDataShore);
                yarpReceiverShore.Stop();
            }

            if (yarpReceiverSaliency != null)
            {
                yarpReceiverSaliency.Elapsed -= new ElapsedEventHandler(ReceiveDataSaliency);
                yarpReceiverSaliency.Stop();
            }
       

            if (yarpReceiverSubject != null)
            {
                yarpReceiverSubject.Elapsed -= new ElapsedEventHandler(ReceiveDataRecognized);
                yarpReceiverSubject.Stop();
            }

            #endregion

            #region Port

            if (yarpPortRecognized != null)
                yarpPortRecognized.Close();

            if (yarpPortSceneXML != null)
                yarpPortSceneXML.Close();

            if (yarpPortSceneOPC != null)
                yarpPortSceneOPC.Close();

            if (yarpPortSound != null)
                yarpPortSound.Close();

            if (yarpPortLookAt != null)
                yarpPortLookAt.Close();

            //if (yarpPortShore != null)
            //    yarpPortShore.Close();

            //if (yarpPortSaliency != null)
            //    yarpPortSound.Close();

             //if (yarpPortImage != null)
             //    yarpPortImage.Close();
            #endregion


        }

        private void StopUDP()
        {
            if (receiverUDP != null)
                receiverUDP.Abort();
        }

        private void StopShore()
        {



            if (yarpReceiverShore != null)
                yarpReceiverShore.Stop();


            try
            {
	            foreach (Process proc in Process.GetProcessesByName("ShoreModule"))
                {
                    proc.CloseMainWindow();
                }
            }
            catch(Exception ex)
            {
	            MessageBox.Show(ex.Message);
            }


        }

        private void StopSaliency()
        {
            if (yarpReceiverSaliency != null)
                yarpReceiverSaliency.Stop();

            try
            {
                foreach (Process proc in Process.GetProcessesByName("SaliencyModule"))
                {
                    proc.CloseMainWindow();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Window_Closing(object sender, EventArgs e)
        {

            foreach (Process proc in Process.GetProcessesByName("ShoreModule"))
            {
                proc.CloseMainWindow();
            }

            foreach (Process proc in Process.GetProcessesByName("SaliencyModule"))
            {
                proc.CloseMainWindow();
            }

            foreach (Process proc in Process.GetProcessesByName("SubjectRecognitionQRCode"))
            {
                proc.CloseMainWindow();
            }



            StopYarp();
            StopUDP();
            StopShore();
            StopSaliency();
            StopKinect();

        }

        #endregion

        #region Tools

        public System.Drawing.Bitmap MakeGrayscale3(System.Drawing.Bitmap original)
        {
            //create a blank bitmap the same size as original
           
            System.Drawing.Bitmap newBitmap;
            lock (this)
            {
                if (original == null)
                    return null;
                newBitmap = new System.Drawing.Bitmap(original.Width, original.Height);

                //get a graphics object from the new image
                System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(newBitmap);

                //create the grayscale ColorMatrix
                ColorMatrix colorMatrix = new ColorMatrix(
                   new float[][] 
              {
                 new float[] {.3f, .3f, .3f, 0, 0},
                 new float[] {.59f, .59f, .59f, 0, 0},
                 new float[] {.11f, .11f, .11f, 0, 0},
                 new float[] {0, 0, 0, 1, 0},
                 new float[] {0, 0, 0, 0, 1}
              });

                //create some image attributes
                ImageAttributes attributes = new ImageAttributes();

                //set the color matrix attribute
                attributes.SetColorMatrix(colorMatrix);

                //draw the original image on the new image
                //using the grayscale color matrix
                g.DrawImage(original, new System.Drawing.Rectangle(0, 0, original.Width, original.Height), 0, 0, original.Width, original.Height, System.Drawing.GraphicsUnit.Pixel, attributes);

                //dispose the Graphics object
                g.Dispose();
            }
            return newBitmap;
        }

        private Point MetersToPoint(Position point)
        {
            float Xmax = point.Z * (float)Math.Tan((57.00 / 180.00) * Math.PI);
            float X = ((point.X / Xmax) / 2) + (float)0.5;

            float Ymax = point.Z * (float)Math.Tan((43.00 / 180.00) * Math.PI);
            float Y = ((point.Y / Ymax) / 2) + (float)0.5;

            Point p = new Point();
            p.X = X * environment.resolution.Width;
            p.Y = Y * environment.resolution.Height;

            return p;
        }

        public gesture GetGestureName(int i)
        {
            string name = Enum.GetName(typeof(gesture), i);
            if (null == name) throw new Exception();
            return (gesture)Enum.Parse(typeof(gesture), name);
        }

        public double DifferencePoints(Position pos1, Position pos2) 
        {
            double deltaX = pos1.X - pos2.X;
            double deltaY = pos1.Y - pos2.Y;
            double deltaZ = pos1.Z - pos2.Z;

            return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY) + (deltaZ*deltaZ));

        }

        private double AngleBetween3D(Position pos1, Position center, Position pos2)
        {
            Vector3D vector1 = new Vector3D(pos1.X - center.X, pos1.Y - center.Y, pos1.Z - center.Z);
            Vector3D vector2 = new Vector3D(pos2.X - center.X, pos2.Y - center.Y, pos2.Z - center.Z);

            return Vector3D.AngleBetween(vector1, vector2);
        }

        private Pos scale(Pos p, ResolutionCam r) 
        {
            Pos pNew = new Pos();

            pNew.X = (Convert.ToDouble(p.X) + ((1920.00 - Convert.ToDouble(r.Width))/2)).ToString();
            pNew.Y = (Convert.ToDouble(p.Y) + ((1080.00 - Convert.ToDouble(r.Height))/2)).ToString();

            return pNew;
        }

        private Pos scale(double x , double y , double w , double h)
        {
            Pos pNew = new Pos();

            pNew.X = (x + ((1920.00 - w) / 2)).ToString();
            pNew.Y = (y + ((1080.00 - h) / 2)).ToString();

            return pNew;
        }

   
        #endregion

        private void SendUDPButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void StopSendUDPButton_Click(object sender, RoutedEventArgs e)
        {

        }



     
   

     

 
    }
}
