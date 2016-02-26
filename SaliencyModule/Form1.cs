using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging; 

using System.Reflection;
using System.IO;
using System.Globalization;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Timers;

using Microsoft.Kinect;

using KinectONE;
using FACELibrary;

using YarpManagerCS;
 
namespace SaliencyModule
{
    public partial class SaliencyModule : Form
    {
        #region Saliency Variables

        [DllImport("VisualSaliency.dll")]
        private static extern void SaliencyMap(ref SalientPoint image, int numtemporal, int numspatial, float firsttau, int firstrad, int wFrameResized);

        [DllImport("VisualSaliency.dll", EntryPoint = "CreateVisualSaliency", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CreateVisualSaliency(int width, int height, int type, int numtemporal, int numspatial, float firsttau, int firstrad);

        [DllImport("VisualSaliency.dll", EntryPoint = "DestroyVisualSaliency", CallingConvention = CallingConvention.StdCall)]
        private static extern void DestroyVisualSaliency(IntPtr vs);

        [DllImport("VisualSaliency.dll", EntryPoint = "UpdateVisualSaliency", CallingConvention = CallingConvention.StdCall)]
        private static extern void UpdateVisualSaliency(IntPtr vs, IntPtr data);

        [DllImport("VisualSaliency.dll", EntryPoint = "GetSaliencyMap", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetSaliencyMap(IntPtr vs);

        [DllImport("VisualSaliency.dll", EntryPoint = "GetSaliencyMapRgb", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetSaliencyMapRgb(IntPtr vs);

        [DllImport("VisualSaliency.dll", EntryPoint = "GetSaliencyMapType", CallingConvention = CallingConvention.StdCall)]
        private static extern int GetSaliencyMapType(IntPtr vs);

        [DllImport("VisualSaliency.dll", EntryPoint = "GetSalientPoint", CallingConvention = CallingConvention.StdCall)]
        private static extern SalientPoint GetSalientPoint(IntPtr vs);

        [StructLayout(LayoutKind.Sequential)]
        public struct SalientPoint
        {
            public UInt32 x;
            public UInt32 y;
            public UInt32 width;
            public UInt32 height;
        };

        //private IntPtr _imageIntPr;
        //private UInt32 _fWidth;
        //private UInt32 _fHeight;
        //private Int32 _lineFeed;
        //private uint stride;

        private IntPtr vs;
        private SalientPoint Spoint;
        private System.Threading.Thread saliencyThread = null;
        private System.Timers.Timer saliencySecondsTimer;        
        
        #endregion
        
        private KinectOne kinect;

        private YarpPort yarpPort;
        private string portName = ConfigurationManager.AppSettings["YarpPort"].ToString();
        private System.Threading.Thread senderYarp = null;

        Saliency sa = new Saliency();
        string sceneData = null;
        private SalientPoint[] saliency = new SalientPoint[10];
        private int currentId = 0;

        private System.Object lockThis = new System.Object();

        private System.Object lockThis2 = new System.Object();

        ColorFrameReference colorRef = null;
        SalientPoint avgPoint = new SalientPoint();

        uint size = 0;

        public SaliencyModule()
        {
            var dllDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + dllDirectory);
           

            InitializeComponent();

            kinect = new KinectOne();
            kinect.InitializeCamera();

            kinect.OnColorFrameArrived(OnColorFrameArrived);

            yarpPort = new YarpPort();
            yarpPort.openSender(portName);

            if (yarpPort != null && yarpPort.NetworkExists())
            {
                lblYarpServer.Text = "YarpServer: runnig...";
                lblYarpPort.Text = "YarpPort : "+portName;

               
            }

            

            InitSaliency();

   

            lblStatus.Text = "running...";
        }

        private void InitSaliency()
        {

            vs = CreateVisualSaliency(kinect.Camera.ColorImageBitmap.PixelWidth, kinect.Camera.ColorImageBitmap.PixelHeight, true ? 24 : 2, 2, 2, 1.0f, 0);

            saliencySecondsTimer = new System.Timers.Timer();
            saliencySecondsTimer.Interval = 200;
            //saliencySecondsTimer.Elapsed += new ElapsedEventHandler(saliencySecondsTimer_Tick);
            //saliencySecondsTimer.Start();
            
        }

         #region Saliency activation


        /// <summary>
        /// Process the infrared frames and update UI
        /// </summary>
        public void OnColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // Get the reference to the color frame
            colorRef = e.FrameReference;

            if (colorRef == null) return;


            ThreadPool.QueueUserWorkItem(saliencyEngine);
       

        }


       

        void saliencyEngine(object state)
        {
            lock (lockThis)
            {
                using (ColorFrame frame = colorRef.AcquireFrame())
                {

                    // It's possible that we skipped a frame or it is already gone
                    if (frame == null) return;

                    size = Convert.ToUInt32(frame.FrameDescription.Height * frame.FrameDescription.Width * 4);
                    frame.CopyConvertedFrameDataToIntPtr(kinect.Camera.PinnedImageBuffer, size, ColorImageFormat.Bgra);

                    frame.Dispose();

                    colorRef = null;

                    UpdateVisualSaliency(vs, kinect.Camera.PinnedImageBuffer);
                    Spoint = GetSalientPoint(vs);

                    saliency[currentId % 10] = Spoint;
                    if (currentId == 10)
                        currentId = 0;
                    else
                        currentId++;

                    saliencySecondsTimer_Tick();
                }
            }
        }

        void saliencySecondsTimer_Tick()//(object sender, EventArgs e)
        {

            uint sumX = 0, sumY = 0;
            for (int i = 0; i < saliency.ToArray().Length; i++)
            {
                sumX += saliency[i].x;
                sumY += saliency[i].y;
            }
            avgPoint.x = (uint)(sumX / saliency.Length);
            avgPoint.y = (uint)(sumY / saliency.Length);
            
               
            //List<float> pos = new List<float>() { avgPoint.x, avgPoint.y };

            lblpoint.Invoke(new Action(() =>
            {
                lblpoint.Text = "X = " + avgPoint.x + " ; Y = " + avgPoint.y;
            }));


            sa.position = new List<float>() { avgPoint.x, avgPoint.y };

            if (sa != null)
            {
                sceneData = ComUtils.XmlUtils.Serialize<Saliency>(sa);
                //Console.WriteLine(sceneData);

                yarpPort.sendData(sceneData);
                sceneData = null;// se non cancello col tempo mi occupa tutta la memoria
            }
           
        }

        

        #endregion
       

        private void StopYarp()
        {
            if (senderYarp != null)
                senderYarp.Abort();

            if (yarpPort != null)
                yarpPort.Close();


        }

        private void StopSaliency()
        {
            if (saliencySecondsTimer != null)
                saliencySecondsTimer.Stop();

            if (saliencyThread != null)
                saliencyThread.Abort();

        }

        private void SaliencyModule_FormClosed(object sender, FormClosedEventArgs e)
        {
            StopYarp();
            StopSaliency();
        }

        private void SaliencyModule_Load(object sender, EventArgs e)
        {
            var secondaryScreen = System.Windows.Forms.Screen.AllScreens.Where(s => !s.Primary).FirstOrDefault();

            if (secondaryScreen != null)
            {

                var workingArea = secondaryScreen.WorkingArea;
                this.Left = workingArea.Left +900;
                this.Top = workingArea.Top+130;
                //this.Width = workingArea.Width;
                //this.Height = workingArea.Height;
                // If window isn't loaded then maxmizing will result in the window displaying on the primary monitor
                //if (this.IsLoaded)
                //    this.WindowState = WindowState.Maximized;
            }
        }

    }
}
