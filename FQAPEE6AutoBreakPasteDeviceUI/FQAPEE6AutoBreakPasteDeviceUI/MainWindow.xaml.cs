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
using System.ComponentModel;
using HalconDotNet;

namespace FQAPEE6AutoBreakPasteDeviceUI
{
    public delegate void DisplayResultsDelegate();
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        string MessageStr = "";
        HDevelopExport hdev_export;
        List<HTuple> drawing_objects;
        DisplayResultsDelegate display_results_delegate;
        HDrawingObject.HDrawingObjectCallback cb;
        HObject ho_EdgeAmplitude;
        HObject background_image = null;
        object image_lock = new object();
        private HImage image;
        private HRegion Rectangle, ModelRegion;
        HWindow Window = null;
        private HShapeModel ShapeModel;
        private double Row, Column;
        //DataAxisCoor CoorPar = new DataAxisCoor();
        //TcpIpClient tcpClient = new TcpIpClient();
        HTuple RowCheck, ColumnCheck, AngleCheck, ScaleCheck, Score;
        HTuple homMat2D;
        public MainWindow()
        {
            InitializeComponent();
            hdev_export = new HDevelopExport();
            drawing_objects = new List<HTuple>();
            Init();
        }
        void Init()
        {
            HImage ImgReduced;
            hdev_export.OpenCamera();

          //  FileStream fileStream = new FileStream(System.Environment.CurrentDirectory + "\\CoorPar.dat", FileMode.Open, FileAccess.Read, FileShare.Read);
          //  BinaryFormatter b = new BinaryFormatter();
          //  CoorPar = b.Deserialize(fileStream) as DataAxisCoor;
          //  fileStream.Close();
          //  GethomMat2D();

          //  CalcRolCenter();
          //  GetNewhomMat2D();
          //  HImage img1 = new HImage(System.Environment.CurrentDirectory + "\\ModelImage.tiff");
          //  Rectangle = new HRegion(CoorPar.RectangleRow1, CoorPar.RectangleColumn1, CoorPar.RectangleRow2, CoorPar.RectangleColumn2);

          //  Rectangle.AreaCenter(out Row, out Column);
          //  ImgReduced = img1.ReduceDomain(Rectangle);
          //  ImgReduced.InspectShapeModel(out ModelRegion, 1, 20);//Constract(20)可设置，类似于阀值，值月底黑色像素越明显
          //  ShapeModel = new HShapeModel(ImgReduced, 4, 0, new HTuple(360.0).TupleRad().D,
          //new HTuple(1.0).TupleRad().D, "none", "use_polarity", 20, 10);
          //  img1.Dispose();
          //  ImgReduced.Dispose();
        }
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            hdev_export.GrapCamera();
            background_image = hdev_export.ho_Image;
            image = new HImage(background_image);
            hSmartWindowControlWPF1.HalconWindow.DispObj(image);
            MsgTextBox.Text = AddMessage("WindowLoaded");
        }

        private void MsgTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            MsgTextBox.ScrollToEnd();
        }
        /// <summary>
        /// 打印窗口字符处理函数
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string AddMessage(string str)
        {
            string[] s = MessageStr.Split('\n');
            if (s.Length > 1000)
            {
                MessageStr = "";
            }
            MessageStr += "\n" + System.DateTime.Now.ToString() + " " + str;
            return MessageStr;
        }
        protected void DisplayCallback(IntPtr draw_id, IntPtr window_handle, string type)
        {
            // On callback, process and display image
            lock (image_lock)
            {
                hdev_export.process_image(background_image, out ho_EdgeAmplitude, hSmartWindowControlWPF1.HalconID, draw_id);
            }
            // You need to switch to the UI thread to display the results
            Dispatcher.BeginInvoke(display_results_delegate);
        }
        private void hSmartWindowControlWPF1_HInitWindow(object sender, EventArgs e)
        {
            HTuple width, height;
            Window = hSmartWindowControlWPF1.HalconWindow;
            hdev_export.hv_ExpDefaultWinHandle = hSmartWindowControlWPF1.HalconID;
            hdev_export.GrapCamera();
            background_image = hdev_export.ho_Image;
            HOperatorSet.GetImageSize(background_image, out width, out height);
            hSmartWindowControlWPF1.HalconWindow.SetPart(0.0, 0.0, height - 1, width - 1);
            hSmartWindowControlWPF1.HalconWindow.AttachBackgroundToWindow(new HImage(background_image));
            display_results_delegate = new DisplayResultsDelegate(() =>
            {
                lock (image_lock)
                {
                    if (ho_EdgeAmplitude != null)
                        hdev_export.display_results(ho_EdgeAmplitude);
                }
            });
            cb = new HDrawingObject.HDrawingObjectCallback(DisplayCallback);
        }
    }
   
}
