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
using System.Windows.Threading;
using System.Drawing;

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
        HTuple ShuXian, HengXian;
        DisplayResultsDelegate display_results_delegate;
        HDrawingObject.HDrawingObjectCallback cb;
        HObject ho_EdgeAmplitude;
        HObject background_image = null;
        HObject background_image2 = null;
        object image_lock = new object();
        private HImage image;
        private HRegion Rectangle, ModelRegion;
        HWindow Window = null;
        HWindow Window2 = null;
        private HShapeModel ShapeModel;
        private double Row, Column;
        DataAxisCoor CoorPar = new DataAxisCoor();
        AS300ModbusTCP aS300ModbusTCP;
        HTuple RowCheck, ColumnCheck, AngleCheck, ScaleCheck, Score;
        HTuple homMat2D;
        Bitmap ImgBitmap;

        object modbustcp = new object();
        bool[] PLC_In;
        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        delegate void DeviceLostRouteEventHandler(object sender, DeviceLostEventArgs e);
        public class DeviceLostEventArgs : RoutedEventArgs
        {
            public DeviceLostEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source) { }

        }

        private void ICImagingControl_DeviceLost(object sender, TIS.Imaging.ICImagingControl.DeviceLostEventArgs e)
        {
            //throw new NotImplementedException();

            //DeviceLostEventArgs args = new DeviceLostEventArgs(DeviceLostEvent, this);
            //this.RaiseEvent(args);
            MessageBox.Show("Device Lost");
        }
        public MainWindow()
        {
            InitializeComponent();
            hdev_export = new HDevelopExport();
            drawing_objects = new List<HTuple>();
            iCImagingControl.DeviceLost += ICImagingControl_DeviceLost;
            Init();
            
        }

        private void GrapButton_Click(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
            grapAction();
        }
        private void grapAction()
        {
            OnClearAllObjects();
            hdev_export.GrapCamera();
            image.Dispose();
            image = new HImage(hdev_export.ho_Image);
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
            if (drawing_objects.Count >= 2)
            {
                OnClearAllObjects();
            }
            
            hdev_export.GrapCamera();
            background_image = hdev_export.ho_Image;
            hSmartWindowControlWPF1.HalconWindow.AttachBackgroundToWindow(new HImage(background_image));
            hdev_export.add_new_drawing_object("rectangle2", hSmartWindowControlWPF1.HalconID, out draw_id);
            SetCallbacks(draw_id, 0);
        }
        private void SetCallbacks(HTuple draw_id,int option)
        {
            // Set callbacks for all relevant interactions
            switch (option)
            {
                case 0:
                    drawing_objects.Add(draw_id);
                    break;
                case 1:
                    ShuXian = draw_id;
                    break;
                case 2:
                    HengXian = draw_id;
                    break;
                default:
                    break;
            }
            
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
            HTuple hv_ParamValues1, hv_ParamValues2;
            HObject Rec1, Rec2, Rec3;
            HImage ImgReduced;

            if (drawing_objects.Count >= 2)
            {
                HOperatorSet.GetDrawingObjectParams(drawing_objects[0], (new HTuple("row")).TupleConcat(new HTuple("column")
                ).TupleConcat(new HTuple("phi")).TupleConcat(new HTuple("length1")).TupleConcat(new HTuple("length2")), out hv_ParamValues1);
                HOperatorSet.GetDrawingObjectParams(drawing_objects[1], (new HTuple("row")).TupleConcat(new HTuple("column")
                ).TupleConcat(new HTuple("phi")).TupleConcat(new HTuple("length1")).TupleConcat(new HTuple("length2")), out hv_ParamValues2);
                HOperatorSet.GenEmptyObj(out Rec1);
                HOperatorSet.GenEmptyObj(out Rec2);
                HOperatorSet.GenEmptyObj(out Rec3);
                HOperatorSet.GenRectangle2(out Rec1, hv_ParamValues1.DArr[0], hv_ParamValues1.DArr[1], hv_ParamValues1.DArr[2], hv_ParamValues1.DArr[3], hv_ParamValues1.DArr[4]);
                HOperatorSet.GenRectangle2(out Rec2, hv_ParamValues2.DArr[0], hv_ParamValues2.DArr[1], hv_ParamValues2.DArr[2], hv_ParamValues2.DArr[3], hv_ParamValues2.DArr[4]);
                HOperatorSet.SymmDifference(Rec1, Rec2,out Rec3);
                Rectangle = new HRegion(Rec3);

                Rectangle.AreaCenter(out Row, out Column);
                //                //hdev_export.GrapCamera();
                image.Dispose();
                image = new HImage(hdev_export.ho_Image);
                image.DispObj(Window);
                ImgReduced = image.ReduceDomain(Rectangle);
                ImgReduced.InspectShapeModel(out ModelRegion, 1, 10);
                ShapeModel = new HShapeModel(ImgReduced, 4, 0, new HTuple(360.0).TupleRad().D,
new HTuple(1.0).TupleRad().D, "none", "use_polarity", 15, 5);
                Window.SetColor("green");
                Window.SetDraw("margin");
                ModelRegion.DispObj(Window);
                image.WriteImage("tiff", 0, System.Environment.CurrentDirectory + "\\ModelImage.tiff");
                ShapeModel.WriteShapeModel(System.Environment.CurrentDirectory + "\\ShapeModel.shm");
            }
            else
            {
                MsgTextBox.Text = AddMessage("少于2个Region，无法创建");
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



            grapAction();
            Action();
            if (RowCheck.Length == 1)
            {
                if (Score.D >= 0.9)
                {
                    double shuxian_x, shuxian_y;
                    RolConvert(CoorPar.ShuXiam.row, CoorPar.ShuXiam.column, CoorPar.MoBan.row, CoorPar.MoBan.column, AngleCheck.D, out shuxian_x, out shuxian_y);
                    shuxian_x += RowCheck.D - CoorPar.MoBan.row;
                    shuxian_y += ColumnCheck.D - CoorPar.MoBan.column;
                    HObject Rec1;
                    HOperatorSet.GenRectangle2(out Rec1, shuxian_x, shuxian_y, CoorPar.ShuXiam.phi + AngleCheck.D, CoorPar.ShuXiam.length1, CoorPar.ShuXiam.length2);
                    HRegion Rec1Region = new HRegion(Rec1);
                    

                    double hengxian_x, hengxian_y;
                    RolConvert(CoorPar.HengXiam.row, CoorPar.HengXiam.column, CoorPar.MoBan.row, CoorPar.MoBan.column, AngleCheck.D, out hengxian_x, out hengxian_y);
                    hengxian_x += RowCheck.D - CoorPar.MoBan.row;
                    hengxian_y += ColumnCheck.D - CoorPar.MoBan.column;
                    HObject Rec2;
                    HOperatorSet.GenRectangle2(out Rec2, hengxian_x, hengxian_y, CoorPar.HengXiam.phi + AngleCheck.D, CoorPar.HengXiam.length1, CoorPar.HengXiam.length2);
                    HRegion Rec2Region = new HRegion(Rec2);

                    Window.SetColor("red");
                    Window.SetDraw("fill");

                    HImage ImgReduced = image.ReduceDomain(Rec1Region);
                    HObject EdgeAmplitude, EdgeDirection;
                    HOperatorSet.SobelDir(ImgReduced, out EdgeAmplitude, out EdgeDirection, "sum_abs", 3);
                    HObject region1;
                    HOperatorSet.Threshold(EdgeAmplitude, out region1, 10, 20);
                    //region1.DispObj(Window);
                    HTuple angle, dist;
                    HOperatorSet.HoughLines(region1, 8, 400, 30, 30, out angle, out dist);
                    HObject LinesHNF;
                    if (dist.Length > 0)
                    {
                        HOperatorSet.GenRegionHline(out LinesHNF, angle, dist);
                        LinesHNF.DispObj(Window);
                    }

                    ImgReduced = image.ReduceDomain(Rec2Region);
                    HOperatorSet.SobelDir(ImgReduced, out EdgeAmplitude, out EdgeDirection, "sum_abs", 3);
                    HOperatorSet.Threshold(EdgeAmplitude, out region1, 50, 255);
                    //region1.DispObj(Window);
                    HOperatorSet.HoughLines(region1, 8, 300, 30, 30, out angle, out dist);
                    HObject LinesHNF1;
                    
                    if (dist.Length > 0)
                    {
                        HOperatorSet.GenRegionHline(out LinesHNF1, angle, dist);
                        LinesHNF1.DispObj(Window);
                    }


                    
                    MsgTextBox.Text = AddMessage("查找模板完成");
                }
                else
                {
                    MsgTextBox.Text = AddMessage("模板质量低");
                }
                //DataAxisCoor.MRectangle2 rec2 = new DataAxisCoor.MRectangle2();
                //rec2.row = RowCheck.DArr[0];
                //rec2.column = ColumnCheck.DArr[0];
                //rec2.phi = AngleCheck.DArr[0];
                //rec2.length1 = 0;
                //rec2.length2 = 0;
                //CoorPar.MoBan = rec2;
                //FileStream fileStream = new FileStream(System.Environment.CurrentDirectory + "\\CoorPar.dat", FileMode.Create);
                //BinaryFormatter b = new BinaryFormatter();
                //b.Serialize(fileStream, CoorPar);
                //fileStream.Close();
            }
            else
            {
                MsgTextBox.Text = AddMessage("未找到模板");
            }













        }

        private void CalcButton_Click(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
        }
        private async void PLCRun()
        {
            while (true)
            {
                await Task.Delay(200);
                try
                {
                    lock (modbustcp)
                    {
                        PLC_In = aS300ModbusTCP.ReadCoils("M5000", 96);
                        TextX1.Text = aS300ModbusTCP.ReadDWORD("D0").ToString();
                        //aS300ModbusTCP.WriteDWORD("D2", -99999999);
                    }
                    //throw new Exception(PLC_In[0].ToString());
                }
                catch (Exception ex)
                {
                    MsgTextBox.Text = AddMessage(ex.Message);
                }
            }

        }

        private void AdjustButton_Click(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Start();
        }

        void Init()
        {
            hdev_export.OpenCamera();

            FileStream fileStream = new FileStream(System.Environment.CurrentDirectory + "\\CoorPar.dat", FileMode.Open, FileAccess.Read, FileShare.Read);
            BinaryFormatter b = new BinaryFormatter();
            CoorPar = b.Deserialize(fileStream) as DataAxisCoor;
            fileStream.Close();
            //GethomMat2D();

            //CalcRolCenter();
            //GetNewhomMat2D();
            //  HImage img1 = new HImage(System.Environment.CurrentDirectory + "\\ModelImage.tiff");
            //  Rectangle = new HRegion(CoorPar.RectangleRow1, CoorPar.RectangleColumn1, CoorPar.RectangleRow2, CoorPar.RectangleColumn2);

            //  Rectangle.AreaCenter(out Row, out Column);
            //  ImgReduced = img1.ReduceDomain(Rectangle);
            //  ImgReduced.InspectShapeModel(out ModelRegion, 1, 20);//Constract(20)可设置，类似于阀值，值月底黑色像素越明显
            //  ShapeModel = new HShapeModel(ImgReduced, 4, 0, new HTuple(360.0).TupleRad().D,
            //new HTuple(1.0).TupleRad().D, "none", "use_polarity", 20, 10);

            //  img1.Dispose();
            //  ImgReduced.Dispose();
            ShapeModel = new HShapeModel(System.Environment.CurrentDirectory + "\\ShapeModel.shm");
        }
        private void Action()
        {

            //ShapeModel.FindScaledShapeModel(image, 0,
            //        new HTuple(360).TupleRad().D, 0.5, 2,
            //        0.4, 1, 0.5, "least_squares",
            //        4, 0.9, out RowCheck, out ColumnCheck,
            //        out AngleCheck, out ScaleCheck, out Score);
            ShapeModel.FindShapeModel(image, 0,
                    new HTuple(360).TupleRad().D, 0.5, 1,
                    0.4, "least_squares",
                    4, 0.9, out RowCheck, out ColumnCheck,
                    out AngleCheck, out Score);



            if (RowCheck.Length == 1)
            {
                Window.SetColor("green");
                Window.SetDraw("fill");
                Window.DispCross(RowCheck, ColumnCheck, 60, 0);
                TextRow1.Text = RowCheck.DArr[0].ToString("F2");
                TextColumn1.Text = ColumnCheck.DArr[0].ToString("F2");
                TextAngle1.Text = AngleCheck.DArr[0].ToString("F2");
                TextScore1.Text = Score.DArr[0].ToString("F2");

            }
        }
        private void Action2()
        {

        }
        private void Calib2Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ImgLive_Click(object sender, RoutedEventArgs e)
        {
            
            if (iCImagingControl.DeviceValid)
            {
                ImgLive.IsEnabled = false;
                ImgStop.IsEnabled = true;
                iCImagingControl.LiveStart();
            }
        }

        private void ImgStop_Click(object sender, RoutedEventArgs e)
        {
            if (iCImagingControl.DeviceValid)
            {
                iCImagingControl.LiveStop();
                ImgLive.IsEnabled = true;
                ImgStop.IsEnabled = false;
            }
        }
        private void ImgSnap()
        {
            if (iCImagingControl.DeviceValid)
            {
                if (iCImagingControl.LiveVideoRunning)
                {
                    iCImagingControl.LiveStop();
                    ImgLive.IsEnabled = true;
                    ImgStop.IsEnabled = false;
                }
                iCImagingControl.MemorySnapImage();
                if (ImgBitmap != null)
                {
                    ImgBitmap.Dispose();
                }
                ImgBitmap = new Bitmap(iCImagingControl.ImageActiveBuffer.Bitmap);
            }
        }

        private void HWindowControlWPF2_HInitWindow(object sender, EventArgs e)
        {
            Window2 = HWindowControlWPF2.HalconWindow;
            HWindowControlWPF2.HalconWindow.SetPart(0.0, 0.0, new HTuple(ImgBitmap.Height - 1), new HTuple(ImgBitmap.Width - 1));
            HWindowControlWPF2.HalconWindow.AttachBackgroundToWindow(new HImage(BitmaptoHImage(ImgBitmap)));
        }
        private HObject BitmaptoHImage(Bitmap bmp)
        {
            HObject ho_Image;
            HOperatorSet.GenEmptyObj(out ho_Image);
            // Lock the bitmap's bits.    
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                bmp.PixelFormat);
            HOperatorSet.GenImageInterleaved(out ho_Image, bmpData.Scan0, "bgrx", bmp.Width, bmp.Height, -1, "byte", bmp.Width, bmp.Height, 0, 0, -1, 0);
            return ho_Image;
        }

        private void USBCameraAction_Click(object sender, RoutedEventArgs e)
        {
            ImgSnap();
            HWindowControlWPF2.HalconWindow.DispObj(new HImage(BitmaptoHImage(ImgBitmap)));
        }
        /// <summary>
        /// 找竖线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawButton_Click1(object sender, RoutedEventArgs e)
        {
            HTuple draw_id;

            hdev_export.GrapCamera();
            background_image = hdev_export.ho_Image;
            hSmartWindowControlWPF1.HalconWindow.AttachBackgroundToWindow(new HImage(background_image));
            hdev_export.add_new_drawing_object("rectangle2", hSmartWindowControlWPF1.HalconID, out draw_id);
            SetCallbacks(draw_id, 1);

        }
        /// <summary>
        /// 创建直线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateButton_Click1(object sender, RoutedEventArgs e)
        {
            //if (ShuXian != null && HengXian != null)
            //{

            //}
            HTuple hv_ParamValues;
            if (ShuXian != null)
            {
                HOperatorSet.GetDrawingObjectParams(ShuXian, (new HTuple("row")).TupleConcat(new HTuple("column")
).TupleConcat(new HTuple("phi")).TupleConcat(new HTuple("length1")).TupleConcat(new HTuple("length2")), out hv_ParamValues);
                DataAxisCoor.MRectangle2 rec2 = new DataAxisCoor.MRectangle2();
                rec2.row = hv_ParamValues.DArr[0];
                rec2.column = hv_ParamValues.DArr[1];
                rec2.phi = hv_ParamValues.DArr[2];
                rec2.length1 = hv_ParamValues.DArr[3];
                rec2.length2 = hv_ParamValues.DArr[4];
                CoorPar.ShuXiam = rec2;
                FileStream fileStream = new FileStream(System.Environment.CurrentDirectory + "\\CoorPar.dat", FileMode.Create);
                BinaryFormatter b = new BinaryFormatter();
                b.Serialize(fileStream, CoorPar);
                fileStream.Close();
            }
            else
            {
                MsgTextBox.Text = AddMessage("竖线区域不存在");
            }
            if (HengXian != null)
            {
                HOperatorSet.GetDrawingObjectParams(HengXian, (new HTuple("row")).TupleConcat(new HTuple("column")
).TupleConcat(new HTuple("phi")).TupleConcat(new HTuple("length1")).TupleConcat(new HTuple("length2")), out hv_ParamValues);
                DataAxisCoor.MRectangle2 rec2 = new DataAxisCoor.MRectangle2();
                rec2.row = hv_ParamValues.DArr[0];
                rec2.column = hv_ParamValues.DArr[1];
                rec2.phi = hv_ParamValues.DArr[2];
                rec2.length1 = hv_ParamValues.DArr[3];
                rec2.length2 = hv_ParamValues.DArr[4];
                CoorPar.HengXiam = rec2;
                FileStream fileStream = new FileStream(System.Environment.CurrentDirectory + "\\CoorPar.dat", FileMode.Create);
                BinaryFormatter b = new BinaryFormatter();
                b.Serialize(fileStream, CoorPar);
                fileStream.Close();
            }
            else
            {
                MsgTextBox.Text = AddMessage("横线区域不存在");
            }
            grapAction();
            Action();
            if (RowCheck.Length == 1)
            {
                DataAxisCoor.MRectangle2 rec2 = new DataAxisCoor.MRectangle2();
                rec2.row = RowCheck.DArr[0];
                rec2.column = ColumnCheck.DArr[0];
                rec2.phi = AngleCheck.DArr[0];
                rec2.length1 = 0;
                rec2.length2 = 0;
                CoorPar.MoBan = rec2;
                FileStream fileStream = new FileStream(System.Environment.CurrentDirectory + "\\CoorPar.dat", FileMode.Create);
                BinaryFormatter b = new BinaryFormatter();
                b.Serialize(fileStream, CoorPar);
                fileStream.Close();
            }
            else
            {
                MsgTextBox.Text = AddMessage("模板匹配失败");
            }


        }
        public void RolConvert(double x,double y,double rx0,double ry0,double a,out double x0,out double y0)
        {
            //http://jingyan.baidu.com/article/2c8c281dfbf3dd0009252a7b.html
            x0 = (x - rx0) * Math.Cos(a) - (y - ry0) * Math.Sin(a) + rx0;
            y0 = (x - rx0) * Math.Sin(a) + (y - ry0) * Math.Cos(a) + ry0;
        }
       
        /// <summary>
        /// 找横线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawButton_Click2(object sender, RoutedEventArgs e)
        {
            HTuple draw_id;

            hdev_export.GrapCamera();
            background_image = hdev_export.ho_Image;
            hSmartWindowControlWPF1.HalconWindow.AttachBackgroundToWindow(new HImage(background_image));
            hdev_export.add_new_drawing_object("rectangle2", hSmartWindowControlWPF1.HalconID, out draw_id);
            SetCallbacks(draw_id, 2);
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            hdev_export.GrapCamera();
            image = new HImage(hdev_export.ho_Image);
            hSmartWindowControlWPF1.HalconWindow.DispObj(image);
            dispatcherTimer.Tick += new EventHandler(GrapContinue);
            dispatcherTimer.Interval = new TimeSpan(1000000);//100毫微秒为单位
            try
            {
                aS300ModbusTCP = new AS300ModbusTCP();
                MsgTextBox.Text = AddMessage("PLC连接成功");
                
                PLCRun();
            }
            catch (Exception ex)
            {
                MsgTextBox.Text = AddMessage(ex.Message);
            }


            try
            {
                iCImagingControl.LoadDeviceStateFromFile("device.xml", true);
            }
            catch (Exception ex)
            {

                MsgTextBox.Text = AddMessage(ex.Message);
            }

            if (!iCImagingControl.DeviceValid)
            {
                iCImagingControl.ShowDeviceSettingsDialog();
            }
            //imageViewer.viewController.repaint();

            if (iCImagingControl.DeviceValid)
            {
                iCImagingControl.SaveDeviceStateToFile("device.xml");
                iCImagingControl.Size = new System.Drawing.Size(600, 400);
                iCImagingControl.LiveDisplayDefault = false;
                iCImagingControl.LiveDisplayHeight = iCImagingControl.Height;
                iCImagingControl.LiveDisplayWidth = iCImagingControl.Width;
                ImgSnap();
                //SmartWindowControlWPF2Init();
            }
            ImgStop.IsEnabled = false;
        }
        private void GrapContinue(Object sender, EventArgs e)
        {
            grapAction();
            Action();
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
        [Serializable]
        public class MRectangle2
        {
            public double row;
            public double column;
            public double phi;
            public double length1;
            public double length2;
        }
        public MRectangle2 ShuXiam;
        public MRectangle2 HengXiam;
        public MRectangle2 MoBan;
    }

}
