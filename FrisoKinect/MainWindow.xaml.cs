#define debug

using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Point = FindPointNS.Point;
using FindPointNS;
using System.Runtime.InteropServices;
using WindowsFormsApplication1;
using PointDB = ConsoleApplication1.Point;
using ConsoleApplication1;
//using Emgu.CV;
//using Emgu.CV.Structure;



namespace FrisoKinect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public enum DisplayFrameType
    {
        Infrared,
        Color,
        Depth
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const DisplayFrameType DEFAULT_DISPLAYFRAMETYPE = DisplayFrameType.Infrared;
        private FrameDescription currentFrameDescription;
        private DisplayFrameType currentDisplayFrameType;
        private MultiSourceFrameReader multiSourceFrameReader = null;

        // Size of the RGB pixel in the bitmap
        private const int BytesPerPixel = 4;

        private KinectSensor kinectSensor = null;
        private CoordinateMapper mapper = null;
        private string statusText = null;
        private WriteableBitmap bitmap = null;

        //Infrared Frame
        private ushort[] infraredFrameData = null;
        private byte[] infraredPixels = null;

        //Depth Frame
        private ushort[] depthFrameData = null;
        private byte[] depthPixels = null;
        private float depthMin = 0;
        private float depthMax = 8;
        private float thickness = 30;
        private CameraSpacePoint[] cameraPt = null;
        private int[] validCameraPtIndex = null;
        private float minX = -0.1f;
        private float maxX = 0.1f;
        private float minY = -0.1f;
        private float maxY = 0.1f;
        private CameraSpacePoint[] cameraCornerPt = null;
        private int cameraSPIndex;
        bool isRun = false;

        private List<Point> ValidDepthPoints = null;
        double eps = 0.01;
        int minPts = 300;

        //4 Corners
        int x = -1;
        int y = -1;
        ushort z = ushort.MaxValue;        

        private Point pointOnKinect, pointOnProjector;
        private Point centerOnProjecter, centerOnKinect;
        private Point[] kinectP = new Point[4];
        private Point[] projP = new Point[4];
        private const int PROJ_WIDTH = /*828;*/ /*1920;*/ /*25;*/ 27;
        private const int PROJ_HEIGHT = /*612;*/ /*1080;*/ /*25;*/ 27;
        private int[] corner = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
        private int[] proj = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
        private int[] center = new int[] { 0, 0 };
        FindPoint findPoint;
        bool OK_P1 = false;
        bool OK_P2 = false;
        bool OK_P3 = false;
        bool OK_P4 = false;
        bool OK_PC = false;

        //UDP
        UdpSender udp;

        public event PropertyChangedEventHandler PropertyChanged;

        public string StatusText
        {
            get { return this.statusText; }
            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        public FrameDescription CurrentFrameDescription
        {
            get { return this.currentFrameDescription; }
            set
            {
                if (this.currentFrameDescription != value)
                {
                    this.currentFrameDescription = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("CurrentFrameDescription"));
                    }
                }
            }
        }

        public DisplayFrameType CurrentDisplayFrameType
        {
            get { return this.currentDisplayFrameType; }
            set
            {
                if (this.currentDisplayFrameType != value)
                {
                    this.currentDisplayFrameType = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("CurrentDisplayFrameType"));
                    }
                }
            }
        }

        public MainWindow()
        {
            this.kinectSensor = KinectSensor.GetDefault();

            this.multiSourceFrameReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Infrared | FrameSourceTypes.Color | FrameSourceTypes.Depth);

            this.multiSourceFrameReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;

            this.mapper = kinectSensor.CoordinateMapper;

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // use the window object as the view model in this example
            this.DataContext = this;

            // open the sensor
            this.kinectSensor.Open();

            this.InitializeComponent();

            findPoint = new FindPoint();
            findPoint.Reset();
            udp = new UdpSender();

            SetupCurrentDisplay(DEFAULT_DISPLAYFRAMETYPE);

            projP[0].X = 0;
            projP[0].Y = 0;
            projP[1].X = 0;
            projP[1].Y = PROJ_HEIGHT;
            projP[2].X = PROJ_WIDTH;
            projP[2].Y = 0;
            projP[3].X = PROJ_WIDTH;
            projP[3].Y = PROJ_HEIGHT;
        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            this.StatusText = this.kinectSensor.IsAvailable ? "Running" : "Not Available";
        }

        private void SetupCurrentDisplay(DisplayFrameType newDisplayFrameType)
        {
            CurrentDisplayFrameType = newDisplayFrameType;
            // Frames used by more than one type are declared outside the switch
            FrameDescription colorFrameDescription = null;
            FrameDescription infraredFrameDescription = null;
            FrameDescription depthFrameDescription = null;

            // reset the display methods
            if (this.FrameDisplayImage != null)
            {
                this.FrameDisplayImage.Source = null;
            }

            switch (CurrentDisplayFrameType)
            {
                case DisplayFrameType.Infrared:
                    infraredFrameDescription = this.kinectSensor.InfraredFrameSource.FrameDescription;
                    depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
                    this.CurrentFrameDescription = infraredFrameDescription;
                    // allocate space to put the pixels being received and converted
                    this.infraredFrameData = new ushort[infraredFrameDescription.Width * infraredFrameDescription.Height];
                    this.infraredPixels = new byte[infraredFrameDescription.Width * infraredFrameDescription.Height * BytesPerPixel];
                    this.depthFrameData = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];
                    this.cameraPt = new CameraSpacePoint[depthFrameDescription.Width * depthFrameDescription.Height];
                    this.cameraCornerPt = new CameraSpacePoint[4];
                    this.bitmap = new WriteableBitmap(infraredFrameDescription.Width, infraredFrameDescription.Height, 96, 96, PixelFormats.Bgra32, null);
                    break;

                case DisplayFrameType.Color:
                    colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;
                    depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
                    this.CurrentFrameDescription = colorFrameDescription;
                    this.depthFrameData = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];
                    this.cameraPt = new CameraSpacePoint[depthFrameDescription.Width * depthFrameDescription.Height];
                    this.cameraCornerPt = new CameraSpacePoint[4];
                    // create the bitmap to display
                    this.bitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96, 96, PixelFormats.Bgra32, null);
                    break;

                case DisplayFrameType.Depth:
                    colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;
                    depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
                    // Actual current frame is going to be a map of depth and color, choosing the larger to display(color)	
                    this.CurrentFrameDescription = depthFrameDescription;
                    // allocate space to put the pixels being received and converted	
                    this.depthFrameData = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];
                    this.depthPixels = new byte[depthFrameDescription.Width * depthFrameDescription.Height * 4];
                    this.cameraPt = new CameraSpacePoint[depthFrameDescription.Width * depthFrameDescription.Height];
                    this.validCameraPtIndex = new int[depthFrameDescription.Width * depthFrameDescription.Height];
                    this.bitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96, 96, PixelFormats.Bgra32, null);
                    break;

                default:
                    break;
            }
        }

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            // If the Frame has expired by the time we process this event, return.
            if (multiSourceFrame == null)
            {
                return;
            }

            ColorFrame colorFrame = null;
            InfraredFrame infraredFrame = null;
            DepthFrame depthFrame = null;

            switch (CurrentDisplayFrameType)
            {
                case DisplayFrameType.Infrared:
                    using (infraredFrame = multiSourceFrame.InfraredFrameReference.AcquireFrame())
                    {
                        using (depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                        {
                            ShowInfraredFrame(infraredFrame, depthFrame);
                        }
                    }
                    break;

                case DisplayFrameType.Color:
                    using (colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                    {
                        using (depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                        {
                            ShowColorFrame(colorFrame, depthFrame);
                        }
                    }
                    break;

                case DisplayFrameType.Depth:
                    using (depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                    {
                        ShowDepthFrame(depthFrame);
                    }
                    break;

                default:
                    break;
            }
        }

        private void ShowColorFrame(ColorFrame colorFrame, DepthFrame depthFrame)
        {
            bool colorFrameProcessed = false;

            if (colorFrame != null)
            {
                FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                // verify data and write the new color frame data to the Writeable bitmap
                if ((colorFrameDescription.Width == this.bitmap.PixelWidth) && (colorFrameDescription.Height == this.bitmap.PixelHeight))
                {
                    this.bitmap.Lock();

                    colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.bitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                    
                    depthFrame.CopyFrameDataToArray(depthFrameData);
                    mapper.MapDepthFrameToCameraSpace(depthFrameData, cameraPt);

                    this.bitmap.AddDirtyRect(new Int32Rect(0, 0, this.bitmap.PixelWidth, this.bitmap.PixelHeight));

                    this.bitmap.Unlock();

                    colorFrameProcessed = true;
                }
            }

            if (colorFrameProcessed)
            {
                FrameDisplayImage.Source = this.bitmap;
            }
        }

        private void ShowInfraredFrame(InfraredFrame infraredFrame, DepthFrame depthFrame)
        {
            bool infraredFrameProcessed = false;


            if (infraredFrame != null)
            {
                this.bitmap.Lock();

                FrameDescription infraredFrameDescription = infraredFrame.FrameDescription;

                // verify data and write the new infrared frame data to the display bitmap
                if (((infraredFrameDescription.Width * infraredFrameDescription.Height) == this.infraredFrameData.Length) &&
                    (infraredFrameDescription.Width == this.bitmap.PixelWidth) &&
                    (infraredFrameDescription.Height == this.bitmap.PixelHeight))
                {
                    // Copy the pixel data from the image to a temporary array
                    infraredFrame.CopyFrameDataToArray(this.infraredFrameData);
                    depthFrame.CopyFrameDataToArray(this.depthFrameData);
                    mapper.MapDepthFrameToCameraSpace(depthFrameData, cameraPt);

                    infraredFrameProcessed = true;

                    ConvertInfraredDataToPixels_ShowLight();
                    //this.bitmap.Unlock();
                }
            }

            // we got a frame, convert and render
            if (infraredFrameProcessed)
            {
                //ConvertInfraredDataToPixels();
                //ConvertInfraredDataToPixels_ShowLight();
                RenderPixelArray(this.infraredPixels);
                //FrameDisplayImage.Source = this.bitmap;
            }
        }

        private void ShowDepthFrame(DepthFrame depthFrame)
        {
            bool depthFrameProcessed = false;

            if (depthFrame != null)
            {
                this.bitmap.Lock();

                FrameDescription depthFrameDescription = depthFrame.FrameDescription;

                // verify data and write the new infrared frame data
                // to the display bitmap
                if (((depthFrameDescription.Width * depthFrameDescription.Height) == this.depthFrameData.Length) &&
                    (depthFrameDescription.Width == this.bitmap.PixelWidth) &&
                    (depthFrameDescription.Height == this.bitmap.PixelHeight))
                {
                    // Copy the pixel data from the image to a temporary array
                    depthFrame.CopyFrameDataToArray(this.depthFrameData);
                    mapper.MapDepthFrameToCameraSpace(depthFrameData, cameraPt);

                    depthFrameProcessed = true;
                }
            }

            // we got a frame, convert and render
            if (depthFrameProcessed)
            {
                ConvertDepthDataToPixels();
                RenderPixelArray(this.depthPixels);                
            }
        }

        private void ConvertDepthDataToPixels()
        {
            int colorPixelIndex = 0;
            //ValidDepthPoints = new List<Point>();

            for (int i = 0; i < this.cameraPt.Length; ++i)
            {
                //byte intensity = (byte)(this.depthFrameData[i] >> 8);
                if (isRun)
                {
                    if (cameraPt[i].X >= minX && cameraPt[i].X <= maxX && cameraPt[i].Y >= minY && cameraPt[i].Y <= maxY && cameraPt[i].Z >= depthMin && cameraPt[i].Z <= depthMax
                        && !float.IsInfinity(cameraPt[i].X) && !float.IsInfinity(cameraPt[i].Y) && !float.IsInfinity(cameraPt[i].Z))
                    {
                        // Get the depth for this pixel
                        //byte depth = (byte)this.depthFrameData[i];

                        this.depthPixels[colorPixelIndex++] = (byte)(depthFrameData[i] >> 6); //Blue
                        this.depthPixels[colorPixelIndex++] = (byte)(depthFrameData[i] >> 4); //Green
                        this.depthPixels[colorPixelIndex++] = (byte)(depthFrameData[i] >> 2); //Red
                        this.depthPixels[colorPixelIndex++] = 255; //Alpha

                        //Point p = new Point();
                        //p.X = cameraPt[i].X;
                        //p.Y = cameraPt[i].Y;
                        //ValidDepthPoints.Add(p);
                        validCameraPtIndex[i] = 0; //valid point and unlabel
                    }
                    else
                    {
                        this.depthPixels[colorPixelIndex++] = 0; //Blue
                        this.depthPixels[colorPixelIndex++] = 0; //Green
                        this.depthPixels[colorPixelIndex++] = 0; //Red
                        this.depthPixels[colorPixelIndex++] = 255; //Alpha

                        validCameraPtIndex[i] = -1; //invalid point
                    }
                }
                else
                {
                    this.depthPixels[colorPixelIndex++] = (byte)(depthFrameData[i] >> 6); //Blue
                    this.depthPixels[colorPixelIndex++] = (byte)(depthFrameData[i] >> 4); //Green
                    this.depthPixels[colorPixelIndex++] = (byte)(depthFrameData[i] >> 2); //Red
                    this.depthPixels[colorPixelIndex++] = 255; //Alpha

                }


                //if (!float.IsInfinity(cameraPt[i].X) && !float.IsInfinity(cameraPt[i].Y) && !float.IsInfinity(cameraPt[i].Z))
                //{
                    
                //}
                
            }

            //Send data
            //Connected-component_labeling
            Dictionary<int, List<CameraSpacePoint>> clusters = new Dictionary<int, List<CameraSpacePoint>>();
            int newLab = 1;

            //Find all clusters
            for (int i = 0; i < validCameraPtIndex.Length; i++)
            {
                if (validCameraPtIndex[i] == 0)
                {
                    //Check neighbors
                    int lab = 0;
                    //top left
                    if (validCameraPtIndex[i - 513] > 0)
                    {
                        lab = validCameraPtIndex[i - 513];
                    }

                    //top
                    if (validCameraPtIndex[i - 512] < lab && validCameraPtIndex[i - 512] > 0)
                    {
                        lab = validCameraPtIndex[i - 512];
                    }

                    //top right
                    if (validCameraPtIndex[i - 511] < lab && validCameraPtIndex[i - 511] > 0)
                    {
                        lab = validCameraPtIndex[i - 511];
                    }

                    //left
                    if (validCameraPtIndex[i - 1] < lab && validCameraPtIndex[i - 1] > 0)
                    {
                        lab = validCameraPtIndex[i - 1];
                    }

                    //assign label
                    if (lab != 0)
                    {
                        //join existed cluster
                        validCameraPtIndex[i] = lab;
                        clusters[lab].Add(cameraPt[i]);
                    }
                    else
                    {
                        //new clusters
                        List<CameraSpacePoint> list = new List<CameraSpacePoint>();
                        list.Add(cameraPt[i]);
                        validCameraPtIndex[i] = newLab;
                        clusters.Add(newLab++, list);
                    }
                }
            }

            //Eliminate too small clusters
            foreach (var cluster in clusters)
            {
                if (cluster.Value.Count < 1000)
                {
                    clusters.Remove(cluster.Key);
                }
            }

            //get center points and send udp
            foreach (var cluster in clusters)
            {
                float sumX = 0;
                float sumY = 0;
                foreach (CameraSpacePoint p in cluster.Value)
                {
                    sumX += p.X;
                    sumY += p.Y;
                }
                pointOnKinect.X = sumX / cluster.Value.Count;
                pointOnKinect.Y = sumY / cluster.Value.Count;
                Convert();                
            }

            //if (OK_P1 && OK_P2 && OK_P3 && OK_P4 && OK_PC)
            //{
                //if (ValidDepthPoints.Count > 0)
                //{
                    //P5Text.Text = "";

                    //List<List<PointDB>> clusters = DBScanAlgo.GetClusters(ValidDepthPoints, eps, minPts);
                    //int sumX = 0;
                    //int sumY = 0;
                    //for (int i = 0; i < clusters.Count; i++)
                    //{
                    //    foreach (PointDB p in clusters[i])
                    //    {
                    //        sumX += p.X;
                    //        sumY += p.Y;
                    //    }
                    //    pointOnKinect.X = sumX / clusters[i].Count;
                    //    pointOnKinect.Y = sumY / clusters[i].Count;
                    //    //Convert();
                    //    P5Text.Text += pointOnKinect.X + " : " + pointOnKinect.Y + ";\n";
                    //}
                //}
            //}
        }

        private void ConvertInfraredDataToPixels_ShowLight()
        {
            // Convert the infrared to RGB
            int colorPixelIndex = 0;
            float brightest = 0f;
            int j = -1;

            for (int i = 0; i < this.infraredFrameData.Length; ++i)
            {
                byte intensity = (byte)(this.infraredFrameData[i] >> 8);

                this.infraredPixels[colorPixelIndex++] = intensity; //Blue
                this.infraredPixels[colorPixelIndex++] = intensity; //Green
                this.infraredPixels[colorPixelIndex++] = intensity; //Red
                this.infraredPixels[colorPixelIndex++] = 255;       //Alpha 

                if (intensity > brightest && i != 106)
                {
                    brightest = intensity;
                    j = i;
                }
            }

            //Show coordinates
            /*if (j != -1)
            {
                y = j / CurrentFrameDescription.Width;
                x = j - y * CurrentFrameDescription.Width;
                //if (0 <= x && x < PROJ_WIDTH && y >= 0 && y < PROJ_HEIGHT)
                //if (true)
                //{
                    pointOnKinect.X = x;
                    pointOnKinect.Y = y;
                    z = (ushort)(depthFrameData[j] + 20); //20cm above floor;
                    cameraSPIndex = j;
                    
                    LightPoint.Text = string.Format("\n{0} , {1}, {2}", x, y, z);
                    //if (OK_P1 && OK_P2 && OK_P3 && OK_P4 && OK_PC)
                    //{
                    //    Convert();
                    //}
                //}
                //else LightPoint.Text = "Out of range";
            }
            else
            {
                LightPoint.Text = "No IR detected";
            }*/

            //Render            
            //Marshal.Copy(infraredPixels, 0, this.bitmap.BackBuffer, infraredPixels.Length);
            //this.bitmap.AddDirtyRect(new Int32Rect(0, 0, this.bitmap.PixelWidth, this.bitmap.PixelHeight));
        }

        private void RenderPixelArray(byte[] pixels)
        {
            Marshal.Copy(pixels, 0, this.bitmap.BackBuffer, pixels.Length);
            this.bitmap.AddDirtyRect(new Int32Rect(0, 0, this.bitmap.PixelWidth, this.bitmap.PixelHeight));
            FrameDisplayImage.Source = this.bitmap;
            this.bitmap.Unlock();
        }

        //private void RenderPixelArray(byte[] pixels)
        //{
        //    pixels.CopyTo(this.bitmap.PixelBuffer);
        //    this.bitmap.Invalidate();
        //    FrameDisplayImage.Source = this.bitmap;
        //}

        private void InfraredButton_Click(object sender, RoutedEventArgs e)
        {
            SetupCurrentDisplay(DisplayFrameType.Infrared);
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            SetupCurrentDisplay(DisplayFrameType.Color);
        }

        private void DepthButton_Click(object sender, RoutedEventArgs e)
        {
            SetupCurrentDisplay(DisplayFrameType.Depth);
        }

        private void P1_Click(object sender, RoutedEventArgs e)
        {
            int a, b;
            if (CheckValidCorner(x, y, out a, out b))
            {
                corner[0] = a;
                corner[1] = b;
                kinectP[0].X = a;
                kinectP[0].Y = b;

                cameraCornerPt[0] = cameraPt[cameraSPIndex];

                findPoint.CalHomoMatrix(kinectP, projP);
                OK_P1 = true;
            }
            P1Text.Text = string.Format("P1: {0} , {1}", a, b);
        }

        private void P2_Click(object sender, RoutedEventArgs e)
        {
            int a, b;
            if (CheckValidCorner(x, y, out a, out b))
            {
                corner[2] = a;
                corner[3] = b;
                kinectP[1].X = a;
                kinectP[1].Y = b;

                cameraCornerPt[1] = cameraPt[cameraSPIndex];

                findPoint.CalHomoMatrix(kinectP, projP);
                OK_P2 = true;
            }
            P2Text.Text = string.Format("P2: {0} , {1}", a, b);
        }

        private void P3_Click(object sender, RoutedEventArgs e)
        {
            int a, b;
            if (CheckValidCorner(x, y, out a, out b))
            {
                corner[4] = a;
                corner[5] = b;
                kinectP[2].X = a;
                kinectP[2].Y = b;

                cameraCornerPt[2] = cameraPt[cameraSPIndex];

                findPoint.CalHomoMatrix(kinectP, projP);
                OK_P3 = true;
            }
            P3Text.Text = string.Format("P3: {0} , {1}", a, b);
        }

        private void P4_Click(object sender, RoutedEventArgs e)
        {
            int a, b;
            if (CheckValidCorner(x, y, out a, out b))
            {
                corner[6] = a;
                corner[7] = b;
                kinectP[3].X = a;
                kinectP[3].Y = b;

                cameraCornerPt[3] = cameraPt[cameraSPIndex];

                findPoint.CalHomoMatrix(kinectP, projP);
                OK_P4 = true;
            }
            P4Text.Text = string.Format("P4: {0} , {1}", a, b);
        }

        private void P5_Click(object sender, RoutedEventArgs e)
        {
            int a, b;
            if (CheckValidCorner(x, y, out a, out b))
            {
                pointOnKinect.X = a;
                pointOnKinect.Y = b;
            }
            P5Text.Text = string.Format("P5: {0} , {1}", a, b);
            Convert();
        }

        private void PC_Click(object sender, RoutedEventArgs e)
        {
            if (OK_P1 && OK_P2 && OK_P3 && OK_P4)
            {
                int a, b;
                if (CheckValidCorner(x, y, out a, out b))
                {
                    center[0] = a;
                    center[1] = b;
                    centerOnKinect.X = a;
                    centerOnKinect.Y = b;

                    //depthMax = z;
                    //depthMin = (ushort)(z - thickness);

                    centerOnProjecter = findPoint.Calculate(centerOnKinect);

                    DefineBoxLimit();

                    OK_PC = true;
                }
                PCText.Text = string.Format("PC: {0} , {1}", a, b);
            }
            else
            {
                PCText.Text = "PC: Get P1, P2, P3, P4 first";
            }

        }

        private void DefineBoxLimit()
        {
            //kinectP[0].X = 150;
            //kinectP[0].Y = 105;

            //kinectP[1].X = 140;
            //kinectP[1].Y = 260;

            //kinectP[2].X = 456;
            //kinectP[2].Y = 263;

            //kinectP[3].X = 327;
            //kinectP[3].Y = 199;

            cameraCornerPt[0] = cameraPt[163488];
            cameraCornerPt[1] = cameraPt[84108];
            cameraCornerPt[2] = cameraPt[159138];
            cameraCornerPt[3] = cameraPt[82888];

            ////Min X
            //if (cameraCornerPt[0].X < cameraCornerPt[1].X)
            //{
            //    minX = cameraCornerPt[0].X;
            //}
            //else
            //{
            //    minX = cameraCornerPt[1].X;
            //}

            ////Max X
            //if (cameraCornerPt[2].X > cameraCornerPt[3].X)
            //{
            //    maxX = cameraCornerPt[2].X;
            //}
            //else
            //{
            //    maxX = cameraCornerPt[3].X;
            //}

            ////Min Y
            //if (cameraCornerPt[0].Y < cameraCornerPt[2].Y)
            //{
            //    minY = cameraCornerPt[0].Y;
            //}
            //else
            //{
            //    minY = cameraCornerPt[2].Y;
            //}

            ////Max Y
            //if (cameraCornerPt[1].Y > cameraCornerPt[3].Y)
            //{
            //    maxY = cameraCornerPt[1].Y;
            //}
            //else
            //{
            //    maxY = cameraCornerPt[3].Y;
            //}

            minX = Min_4P(cameraCornerPt[0].X, cameraCornerPt[1].X, cameraCornerPt[2].X, cameraCornerPt[3].X);
            maxX = Max_4P(cameraCornerPt[0].X, cameraCornerPt[1].X, cameraCornerPt[2].X, cameraCornerPt[3].X);
            minY = Min_4P(cameraCornerPt[0].Y, cameraCornerPt[1].Y, cameraCornerPt[2].Y, cameraCornerPt[3].Y);
            maxY = Max_4P(cameraCornerPt[0].Y, cameraCornerPt[1].Y, cameraCornerPt[2].Y, cameraCornerPt[3].Y);

            depthMax = (cameraCornerPt[0].Z + cameraCornerPt[1].Z + cameraCornerPt[2].Z + cameraCornerPt[3].Z) / 4 - 0.5f;
            //depthMin = depthMax - 0.15f;
        }

        float Min_4P(float a, float b, float c, float d)
        {
            float min = a;
            if (b < min) min = b;
            if (c < min) min = c;
            if (d < min) min = d;
            return min;
        }

        float Max_4P(float a, float b, float c, float d)
        {
            float max = a;
            if (b > max) max = b;
            if (c > max) max = c;
            if (d > max) max = d;
            return max;
        }

        private void Setup_Click(object sender, RoutedEventArgs e)
        {
            OK_P1 = false;
            OK_P2 = false;
            OK_P3 = false;
            OK_P4 = false;
            OK_PC = false;
        }

        private bool CheckValidCorner(int x, int y, out int a, out int b)
        {
            a = -1;
            b = -1;
            if (x == -1 || y == -1 ||
                x > 500 || x < 3 || y > 400 || y < 3)
            {
                return false;
            }
            a = x;
            b = y;
            return true;
        }

        private void Convert()
        {
            //kinectP[0].X = 144;
            //kinectP[0].Y = 370;

            //kinectP[1].X = 178;
            //kinectP[1].Y = 334;

            //kinectP[2].X = 289;
            //kinectP[2].Y = 369;

            //kinectP[3].X = 279;
            //kinectP[3].Y = 333;

            //center[0] = 222;
            //center[1] = 163;

            //findPoint.CalHomoMatrix(kinectP, projP);

            //Convert
            pointOnProjector = findPoint.Calculate(pointOnKinect);
            //convert to unity axis
            pointOnProjector.X -= centerOnProjecter.X;
            pointOnProjector.Y -= centerOnProjecter.Y;
            //pointOnProjector.X *= -1;
            //pointOnProjector.Y *= -1;
            //P5Text.Text = string.Format("P: {0} , {1}", pointOnProjector.X, pointOnProjector.Y);

            //Send UDP
            String s = String.Format("{0},{1},{2}", (int)pointOnProjector.X, (int)pointOnProjector.Y, 1);
            udp.SenMessage(s);

        }

        private void FrameDisplayImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point mouse = e.GetPosition(FrameDisplayImage);

            pointOnKinect.X = (float)(mouse.X * 512 / FrameDisplayImage.ActualWidth);
            pointOnKinect.Y = (float)(mouse.Y * 424 / FrameDisplayImage.ActualHeight);

            int i = 0;
            for (; i < depthFrameData.Length; i++)
            {
                //y = i / 512;
                //x = i - 512 * y;
                y = (int) (424 - i/512);
                x = i - 512*(int) (i/512);
                //z = (ushort)(depthFrameData[i] + 20);
                if (y == (int)pointOnKinect.Y && x == (int)pointOnKinect.X)
                {
                    cameraSPIndex = i;
                    break;
                }
            }
            
            LightPoint.Text = string.Format("\n{0} , {1}", x, y);
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            isRun = !isRun;
            DefineBoxLimit();
        }
    }
}
