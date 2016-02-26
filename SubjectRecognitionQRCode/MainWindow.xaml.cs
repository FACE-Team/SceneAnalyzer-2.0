using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Media;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.IO;
using System.Timers;
using System.Configuration;

using System.Runtime.InteropServices;
using System.Windows.Interop;

using AForge.Video.DirectShow;
using AForge.Video;

using YarpManagerCS;
using FACELibrary;

namespace SubjectRecognitionQRCode
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region CAM

        FilterInfoCollection videoSources;
        VideoCaptureDevice videoStream;

        // Bitmap buffers
        Bitmap streamBitmap;
        Bitmap snapShotBitmap;

        System.Threading.Thread decodingThread;
        // The QR Decoder variable from ZXing
        Decoder decoder;

        double w;
        double h;

        private System.Threading.Thread senderThread = null;

        #endregion

        #region Yarp
        private string subjectRecognized_OUT = ConfigurationManager.AppSettings["YarpPorSubjectRecognized_OUT"].ToString();
       
        private string dataBaseCommand_IN = ConfigurationManager.AppSettings["YarpPortDataBaseCommand_IN"].ToString();
        private string dataBaseCommand_OUT = ConfigurationManager.AppSettings["YarpPortDataBaseCommand_OUT"].ToString();
        
        private string dataBaseReply_IN = ConfigurationManager.AppSettings["YarpPortDataBaseReply_IN"].ToString();
        private string dataBaseReply_OUT = ConfigurationManager.AppSettings["YarpPortDataBaseReply_OUT"].ToString();
        
        private string dataBaseStatus_IN = ConfigurationManager.AppSettings["YarpPortDataBaseStatus_IN"].ToString();
        private string dataBaseStatus_OUT = ConfigurationManager.AppSettings["YarpPortDataBaseStatus_OUT"].ToString(); 

        private YarpPort yarpPortScene;
        private YarpPort yarpPortCommandDB;
        private YarpPort yarpPortReplyDB;
        private YarpPort yarpPortStatusDB;

        private System.Timers.Timer checkYarpStatusTimer;


        private string StringYarp=null;

        #endregion
        RecognizedQRCode SubjRecognition = new RecognizedQRCode();


        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public MainWindow()
        {
            var dllDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + dllDirectory);

            InitializeComponent();

            InitCamera();
            InitYarp();


        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var secondaryScreen = System.Windows.Forms.Screen.AllScreens.Where(s => !s.Primary).FirstOrDefault();

            if (secondaryScreen != null)
            {

                var workingArea = secondaryScreen.WorkingArea;
                this.Left = workingArea.Left + 900;
                this.Top = workingArea.Top+290;
                //this.Width = workingArea.Width;
                //this.Height = workingArea.Height;
                // If window isn't loaded then maxmizing will result in the window displaying on the primary monitor
                //if (this.IsLoaded)
                //    this.WindowState = WindowState.Maximized;
            }

            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }


        private void InitYarp()
        {
            yarpPortScene = new YarpPort();
            yarpPortScene.openSender(subjectRecognized_OUT);

            yarpPortCommandDB = new YarpPort();
           // yarpPortCommandDB.openSender("/RecognizedModule/Command:o");

            yarpPortReplyDB = new YarpPort();
            //yarpPortReplyDB.openInputPort("/RecognizedModule/Reply:i");

            yarpPortStatusDB = new YarpPort();
            yarpPortStatusDB.openReceiver(dataBaseStatus_OUT, dataBaseStatus_IN);

            senderThread = new System.Threading.Thread(SendData);
            senderThread.Start();

            checkYarpStatusTimer = new System.Timers.Timer();
            checkYarpStatusTimer.Elapsed += new ElapsedEventHandler(CheckYarpConnections);
            checkYarpStatusTimer.Interval = (1000) * 5;
            checkYarpStatusTimer.Start();
        }

        private void InitCamera() 
        {
            // Initialize sound variable

            decoder = new Decoder();


            try
            {
                // enumerate video devices
                videoSources = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                // create video source
                if (videoSources.Count >= 1)
                    videoStream = new VideoCaptureDevice(videoSources[0].MonikerString);
                else
                {
                    MessageBox.Show("camera disconnected");
                    this.Close();
                    return;
                }

                //foreach (VideoCapabilities vc in videoStream.VideoCapabilities)
                //    cmbFrameSize.Items.Add(vc.FrameSize);

                videoStream.DesiredFrameSize = new System.Drawing.Size(1280, 720); 
 
                // set NewFrame event handler
                videoStream.NewFrame += new NewFrameEventHandler(videoSource_NewFrame);

                // start the video source
                videoStream.Start();

            }
            catch(VideoException exp)
            {
                Console.Write(exp.Message);
            }
            
        }

        // This event will be triggered whenever a new image is being captured by the webcam, minimum 25 frame per minute
        // Depending in the webcam capability.
        void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {

            try
            {
                System.Drawing.Image img = (Bitmap)eventArgs.Frame.Clone();

                using (Graphics g = Graphics.FromImage(img))
                {
                    using (System.Drawing.Brush brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(200, 128, 128, 128)))
                    {
                        g.FillRectangle(brush, 0, 0, 500, 720);
                        g.FillRectangle(brush, 780, 0, 500, 720);
                    }
                }

                img.RotateFlip(RotateFlipType.Rotate180FlipY);

                w = img.Width;
                h= img.Height;

                MemoryStream ms = new MemoryStream();
                img.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);
                
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.EndInit();
                bi.Freeze();

              

                streamBitmap = (Bitmap)img;

                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    CameraImage.Source = bi;
                    MarkerQRCode.Height = CameraImage.ActualHeight;
                    MarkerQRCode.Width = CameraImage.ActualWidth;
                }));
            }
            catch (Exception exp)
            {
                Console.Write(exp.Message);
            }
        }

        // Decoding endless thread process
        public void decodeLoop()
        {
            while (true)
            {
                // 1 second pause for the thread. This could be changed manually to a prefereable decoding interval.
                System.Threading.Thread.Sleep(1000);
                if (streamBitmap != null)
                    snapShotBitmap = (Bitmap)streamBitmap.Clone();
                else
                    return;

                MarkerQRCode.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                     new Action(delegate()
                     {
                         MarkerQRCode.Children.Clear();
                         Sub.Content = "";
                         drawEllipse(MarkerQRCode, 1280 / 2, 720 / 2, 10, 10, (float)2.0, System.Windows.Media.Brushes.Red);

                     }));


                StringYarp = "";

                // Decode the snapshot.
                Dictionary<string, ZXing.ResultPoint[]> decodeStr = new Dictionary<string, ZXing.ResultPoint[]>();
                decodeStr = decoder.MDecode(snapShotBitmap);

                if (decodeStr != null)
                {
                    if (decodeStr.Count > 0)
                    {


                        Console.WriteLine(StringYarp);
                        float x=0;
                        float y=0;


                        drawEllipse(MarkerQRCode, 1280 / 2, 720 / 2, 10, 10, (float)2.0, System.Windows.Media.Brushes.Red);

                        foreach (var point in decodeStr)
                        {
                           
                            for (int i = 0; i < point.Value.Length; i++)
                            {
                                x += point.Value[i].X;
                                y += point.Value[i].Y;
                                drawEllipse(MarkerQRCode, Convert.ToInt32(point.Value[i].X), Convert.ToInt32(point.Value[i].Y), 10, 10, (float)2.0);
                            }
                                
                            drawEllipse(MarkerQRCode, Convert.ToInt32((point.Value[0].X+point.Value[1].X)/2), Convert.ToInt32((point.Value[1].Y+point.Value[2].Y)/2), 10, 10, (float)2.0);

                        }

                        StringYarp = ComUtils.JsonUtils.Serialize<RecognizedQRCode>(getInformationDB(decodeStr));

                    }
                }
            }
        }

        private void SendData()
        {

            while (true)
            {
                if (StringYarp==null)
                    continue;

                yarpPortScene.sendData(StringYarp);
                
            }
        }

        void CheckYarpConnections(object source, ElapsedEventArgs e)
        {
            #region PortExists-> dataBaseStatus_OUT
            if (yarpPortStatusDB != null && yarpPortStatusDB.PortExists(dataBaseStatus_OUT))
            {

                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate()
                    {
                        if (DBModStatus.Fill == System.Windows.Media.Brushes.Red)
                        {
                            DBModStatus.Fill = System.Windows.Media.Brushes.Green;
                                        decodingThread = new System.Threading.Thread(new ThreadStart(decodeLoop));

                            decodingThread.Start();

                        }
                    }));
            }
            else if (yarpPortStatusDB != null && !yarpPortStatusDB.PortExists(dataBaseStatus_OUT))
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate()
                    {
                        if (DBModStatus.Fill == System.Windows.Media.Brushes.Green)
                        {
                            DBModStatus.Fill = System.Windows.Media.Brushes.Red;
                            decodingThread.Abort();

                        }

                    }));
            }
            #endregion


            #region NetworkExists
            if (yarpPortStatusDB != null && yarpPortStatusDB.NetworkExists())
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate()
                    {
                        if (YarpServerStatus.Fill == System.Windows.Media.Brushes.Red)
                            YarpServerStatus.Fill = System.Windows.Media.Brushes.Green;
                    }));
            }
            else if (yarpPortStatusDB != null && !yarpPortStatusDB.NetworkExists())
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate()
                    {
                        if (YarpServerStatus.Fill == System.Windows.Media.Brushes.Green)
                            YarpServerStatus.Fill = System.Windows.Media.Brushes.Red;
                    }));
            }
            #endregion
        }


        RecognizedQRCode getInformationDB(Dictionary<string, ZXing.ResultPoint[]> dict)
        {

            RecognizedQRCode rec = new RecognizedQRCode();
            rec.ResolutionCam = new ResolutionCam();
            rec.ResolutionCam.Width = w.ToString();
            rec.ResolutionCam.Height = h.ToString();


            foreach (var s in dict)
            {
                //preparo l'informazione del soggetto con la posizione centrale del QRCode
                InfoQRCode inf = new InfoQRCode();
                inf.Positions = new List<Pos>();
            
                foreach (var v in s.Value)
                {
                    Pos p = new Pos();
                    p.X = v.X.ToString();
                    p.Y = v.Y.ToString();

                    inf.Positions.Add(p);
                }

                // verifico se già conosco il soggetto altrimenti interogo il database
                List<InfoQRCode> sub = SubjRecognition.InfoQRCode.FindAll(a => a.Message.Split('|')[1].Trim() == s.Key);
                if (sub.Count() == 0)
                {
                    //leggo lo status del database se è occupato attendo
                    string state = "";

                    do
                    {
                        yarpPortStatusDB.receivedData(out state);
                    }
                    while (state != "Activo");

                    //apro le connessioni con il db
                    yarpPortCommandDB.openConnectionToDb(dataBaseCommand_OUT, dataBaseCommand_IN);
                    yarpPortReplyDB.openReceiverReplyDb(dataBaseReply_OUT, dataBaseReply_IN);

                    yarpPortCommandDB.sendData("SELECT * FROM subjects WHERE IdSubject=" + s.Key);
                    System.Threading.Thread.Sleep(400);

                    //attendo risposta
                    string reply = "";
                    do
                    {
                        yarpPortReplyDB.receivedData(out reply);
                    }
                    while (reply == "");

                    //se non trovo nulla parte evento che gestire l'inserimento del nuovo soggetto 
                    if (reply.Replace("\"", "") == "|") 
                    {
                        InputDialog.InputDialog dialog2 = new InputDialog.InputDialog("Assign ID and Name to ", "", "");
                        if (dialog2.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            if (dialog2.ResultText != "unknown")
                            {

                                yarpPortCommandDB.sendData("INSERT INTO subjects (IdSubject,Name,FirstTime) VALUE ('" + s.Key + "','" + dialog2.ResultText + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')");

                                System.Threading.Thread.Sleep(400);

                                reply = "";
                                do
                                {
                                    yarpPortReplyDB.receivedData(out reply);
                                }
                                while (reply == "" || reply == "\"|\"");

                                inf.Message = "unknown";
                            }
                            else
                                inf.Message = dialog2.ResultText;

                        }
                        else
                            inf.Message = dialog2.ResultText;
                    }
                    else
                        inf.Message = reply.Replace("\"", "");



                    yarpPortCommandDB.Close();
                    yarpPortReplyDB.Close();

                    if(inf.Message!="unknown")
                        SubjRecognition.InfoQRCode.Add(inf);


                }
                else
                {

                    foreach (InfoQRCode info in sub)
                        if (info.Message[1] == s.Key[0])
                            inf.Message = info.Message;
                }

                Sub.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                  new Action(delegate()
                  {
                      Sub.Content = inf.Message;
                  }));

              
                rec.InfoQRCode.Add(inf);


            }


            return rec;
        }


       #region Tools
        public void drawEllipse(System.Windows.Controls.Canvas pb, int x, int y, int w, int h, float Bwidth)
        {
            pb.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                     new Action(delegate()
                     {


                         SolidBrush brush = new SolidBrush(System.Drawing.Color.Red);
                         System.Drawing.Point dPoint = new System.Drawing.Point((x * Convert.ToInt32(pb.ActualWidth) / 1280), Convert.ToInt32(y * pb.ActualHeight) / 720);
                         dPoint.X = dPoint.X - (h / 2);
                         dPoint.Y = dPoint.Y - (h / 2);

                         System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
                         rect.Width = w;
                         rect.Height = h;
                         rect.Stroke = System.Windows.Media.Brushes.Cyan;
                         rect.Margin = new Thickness(dPoint.X, dPoint.Y, 0, 0); //draw the rectangle
                      


                         pb.Children.Add(rect);
                     }
             ));
        }


        public void drawEllipse(System.Windows.Controls.Canvas pb, double x, double y, int w, int h, float Bwidth, System.Windows.Media.Brush brush)
        {
            pb.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                     new Action(delegate()
                     {



                         double pointX = (x * pb.ActualWidth) / 1280.00;
                         double pointY = (y * pb.ActualHeight) / 720.00;
                         pointX = pointX - (w / 2);// +Convert.ToInt32(ErrorXc.Text);
                         pointY = pointY - (h / 2);// +Convert.ToInt32(ErrorYc.Text);

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
        string MyDictionaryToJson(Dictionary<string, ZXing.ResultPoint[]> dict)
        {
            bool sFirst = true;
            bool vFirst = true;

            string str = "{ \"res\": {\"Width\" :\"" + w + "\", \"Height\" :\"" + h + "\"}, \"subjets\":[";

            foreach (var s in dict) 
            {
                if (sFirst)
                {
                    str += "{";
                    sFirst = false;
                }
                else
                    str += ",{";

                str+=string.Format("\"string\": \"{0}\",",  s.Key);
                str+="\"pos\" :[";
                foreach (var v in s.Value) 
                {
                    if (vFirst)
                    {
                        str += "{\"x\":\"" + v.X + "\",\"y\": \""+v.Y+"\"}";
                        vFirst = false;
                    }
                    else
                        str += ",{\"x\":\"" + v.X + "\",\"y\": \"" + v.Y + "\"}";

                }
                str += "]}";

            }
            str += "]}";
            return str;
        }

        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            #region Thred or timer
            if (decodingThread != null)
                decodingThread.Abort();

            if (senderThread != null)
                senderThread.Abort();

            if (checkYarpStatusTimer != null)
            {
                checkYarpStatusTimer.Elapsed -= new ElapsedEventHandler(CheckYarpConnections);
                checkYarpStatusTimer.Stop();
            }

            if (videoStream != null)
            {
                videoStream.NewFrame -= new NewFrameEventHandler(videoSource_NewFrame);
                videoStream.Stop();
            }



            #endregion

            #region Port

            if (yarpPortScene != null)
                yarpPortScene.Close();

            if (yarpPortCommandDB != null)
                yarpPortCommandDB.Close();

            if (yarpPortReplyDB != null)
                yarpPortReplyDB.Close();

            if (yarpPortStatusDB != null)
                yarpPortStatusDB.Close();

            #endregion
        }

     
    }

    
}
