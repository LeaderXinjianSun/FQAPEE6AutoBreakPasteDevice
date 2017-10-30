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
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using BingLibrary.hjb;
using Leader.DeltaAS300ModbusTCP;

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
        DataAxisCoor CoorPar = new DataAxisCoor();
        //TcpIpClient tcpClient = new TcpIpClient();
        AS300ModbusTCP aS300ModbusTCP;
        HTuple RowCheck, ColumnCheck, AngleCheck, ScaleCheck, Score;
        HTuple homMat2D;
        public MainWindow()
        {
            InitializeComponent();
            hdev_export = new HDevelopExport();
            drawing_objects = new List<HTuple>();
            Init();
            
        }

        private void GrapButton_Click(object sender, RoutedEventArgs e)
        {
            grapAction();
        }
        private void grapAction()
        {
            OnClearAllObjects();
            hdev_export.GrapCamera();
            background_image = hdev_export.ho_Image;
            image.Dispose();
            image = new HImage(background_image);
            hSmartWindowControlWPF1.HalconWindow.DispObj(image);
        }
        private void OnClearAllObjects()
        {
            lock (image_lock)
            {
                foreach (HTuple dobj in drawing_objects)
                {
                    HOperatorSet.ClearDrawingObject(dobj);
                }
                drawing_objects.Clear();
            }
            hSmartWindowControlWPF1.HalconWindow.ClearWindow();
        }
        private void DrawButton_Click(object sender, RoutedEventArgs e)
        {
            HTuple draw_id;
            OnClearAllObjects();
            hdev_export.GrapCamera();
            background_image = hdev_export.ho_Image;
            hSmartWindowControlWPF1.HalconWindow.AttachBackgroundToWindow(new HImage(background_image));
            hdev_export.add_new_drawing_object("rectangle1", hSmartWindowControlWPF1.HalconID, out draw_id);
            SetCallbacks(draw_id);
        }
        private void SetCallbacks(HTuple draw_id)
        {
            // Set callbacks for all relevant interactions
            drawing_objects.Add(draw_id);
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(cb);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_resize", ptr);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_drag", ptr);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_attach", ptr);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_select", ptr);
            lock (image_lock)
            {
                HOperatorSet.AttachDrawingObjectToWindow(hSmartWindowControlWPF1.HalconID, draw_id);
            }
        }
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            HTuple hv_ParamValues;
            HImage ImgReduced;
            if (drawing_objects.Count > 0)
            {
                HOperatorSet.GetDrawingObjectParams(drawing_objects[0], (new HTuple("row1")).TupleConcat(new HTuple("column1")
                    ).TupleConcat(new HTuple("row2")).TupleConcat(new HTuple("column2")), out hv_ParamValues);

                CoorPar.RectangleRow1 = hv_ParamValues.IArr[0];
                CoorPar.RectangleColumn1 = hv_ParamValues.IArr[1];
                CoorPar.RectangleRow2 = hv_ParamValues.IArr[2];
                CoorPar.RectangleColumn2 = hv_ParamValues.IArr[3];

                Rectangle = new HRegion(CoorPar.RectangleRow1, CoorPar.RectangleColumn1, CoorPar.RectangleRow2, CoorPar.RectangleColumn2);
                Rectangle.AreaCenter(out Row, out Column);
                //hdev_export.GrapCamera();
                image.Dispose();
                image = new HImage(hdev_export.ho_Image);
                image.DispObj(Window);
                ImgReduced = image.ReduceDomain(Rectangle);
                ImgReduced.InspectShapeModel(out ModelRegion, 1, 20);
                ShapeModel = new HShapeModel(ImgReduced, 4, 0, new HTuple(360.0).TupleRad().D,
new HTuple(1.0).TupleRad().D, "none", "use_polarity", 20, 10);
                Window.SetColor("green");
                Window.SetDraw("margin");
                ModelRegion.DispObj(Window);
                image.WriteImage("tiff", 0, System.Environment.CurrentDirectory + "\\ModelImage.tiff");
            }
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            FileStream fileStream = new FileStream(System.Environment.CurrentDirectory + "\\CoorPar.dat", FileMode.Create);
            BinaryFormatter b = new BinaryFormatter();
            b.Serialize(fileStream, CoorPar);
            fileStream.Close();
            hdev_export.CloseCamera();
        }

        private void Calib1Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Calib2Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CalcButton_Click(object sender, RoutedEventArgs e)
        {

        }

        void Init()
        {
            HImage ImgReduced;
            hdev_export.OpenCamera();

            FileStream fileStream = new FileStream(System.Environment.CurrentDirectory + "\\CoorPar.dat", FileMode.Open, FileAccess.Read, FileShare.Read);
            BinaryFormatter b = new BinaryFormatter();
            CoorPar = b.Deserialize(fileStream) as DataAxisCoor;
            fileStream.Close();
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
            try
            {
                aS300ModbusTCP = new AS300ModbusTCP();
            }
            catch (Exception ex)
            {
                MsgTextBox.Text = AddMessage(ex.Message);
            }
            
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
    [Serializable]
    public class DataAxisCoor
    {
        public double RectangleRow1;
        public double RectangleColumn1;
        public double RectangleRow2;
        public double RectangleColumn2;
        public double[,] Coor;
        public double[,] CoorU;
        public double deltaD = 5;
        public double deltaU = 15;
        public double[,] deltaCoor;
        public double[] deltaCoorU;
        public double Pos_x;
        public double Pos_y;
        public double Center_x;
        public double Center_y;
        public double Center_r;
        public void CalcDeltaCoor()
        {
            deltaCoor = new double[9, 2];
            deltaCoor[0, 0] = deltaD * (-1); deltaCoor[0, 1] = deltaD * (-1);
            deltaCoor[1, 0] = deltaD * (0); deltaCoor[1, 1] = deltaD * (-1);
            deltaCoor[2, 0] = deltaD * (1); deltaCoor[2, 1] = deltaD * (-1);
            deltaCoor[3, 0] = deltaD * (1); deltaCoor[3, 1] = deltaD * (0);
            deltaCoor[4, 0] = deltaD * (0); deltaCoor[4, 1] = deltaD * (0);
            deltaCoor[5, 0] = deltaD * (-1); deltaCoor[5, 1] = deltaD * (0);
            deltaCoor[6, 0] = deltaD * (-1); deltaCoor[6, 1] = deltaD * (1);
            deltaCoor[7, 0] = deltaD * (0); deltaCoor[7, 1] = deltaD * (1);
            deltaCoor[8, 0] = deltaD * (1); deltaCoor[8, 1] = deltaD * (1);
        }
        public void CalcDelyaCoorU()
        {
            deltaCoorU = new double[3];
            deltaCoorU[0] = deltaU * (-1);
            deltaCoorU[1] = deltaU * (0);
            deltaCoorU[2] = deltaU * (1);
        }
    }
}
