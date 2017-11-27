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
//using BingLibrary.hjb;
using Leader.DeltaAS300ModbusTCP;
using System.Windows.Threading;
using System.Drawing;
using OfficeOpenXml;
using Microsoft.Win32;
using System.Windows.Forms;
using System.IO.Ports;
using SxjLibrary;
using 臻鼎科技OraDB;
using System.Data;

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
        //HObject background_image2 = null;
        object image_lock = new object();
        private HImage image;
        private HRegion Rectangle, ModelRegion;
        HWindow Window = null;
        //HWindow Window2 = null;
        private HShapeModel ShapeModel;
        private double Row, Column;
        DataAxisCoor CoorPar = new DataAxisCoor();
        AS300ModbusTCP aS300ModbusTCP;
        HTuple RowCheck, ColumnCheck, AngleCheck, ScaleCheck, Score;
        HTuple homMat2D;
        Bitmap ImgBitmap;
        MySQLClass mySQLClass;
        int CX, CY;
        //bool Window2Init = false;
        double Line1Angle, Line1Dist, Line2Angle, Line2Dist;

        object modbustcp = new object();
        bool[] PLC_In;
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        Double[] CrossPoint;
        string[] ImageFiles;
        int ImageIndex;
        private dialog mydialog = new dialog();
        bool ShutdownFlag = false;
        bool Loadin = false;

        //delegate void DeviceLostRouteEventHandler(object sender, DeviceLostEventArgs e);
        //public class DeviceLostEventArgs : RoutedEventArgs
        //{
        //    public DeviceLostEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source) { }

        //}

        private void ICImagingControl_DeviceLost(object sender, TIS.Imaging.ICImagingControl.DeviceLostEventArgs e)
        {
            //throw new NotImplementedException();

            //DeviceLostEventArgs args = new DeviceLostEventArgs(DeviceLostEvent, this);
            //this.RaiseEvent(args);
            System.Windows.Forms.MessageBox.Show("Device Lost");
        }
        public MainWindow()
        {
            InitializeComponent();
            #region 判断系统是否已启动

            System.Diagnostics.Process[] myProcesses = System.Diagnostics.Process.GetProcessesByName("FQAPEE6AutoBreakPasteDeviceUI");//获取指定的进程名   
            if (myProcesses.Length > 1) //如果可以获取到知道的进程名则说明已经启动
            {
                System.Windows.MessageBox.Show("不允许重复打开软件");
                System.Windows.Application.Current.Shutdown();
                ShutdownFlag = true;
            }
            else
            {
                hdev_export = new HDevelopExport();
                drawing_objects = new List<HTuple>();
                mySQLClass = new MySQLClass();
                //mySQLClass.test();
                //iCImagingControl.DeviceLost += ICImagingControl_DeviceLost;
            }


            #endregion



        }
        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var validComNames = SerialPort.GetPortNames();
            foreach (var comName in validComNames)
            {
                if (!Com.Items.Contains(comName))
                    Com.Items.Add(comName);
            }
            List<string> toRemove = new List<string>();
            foreach (string addedName in Com.Items)
            {
                if (!validComNames.Contains(addedName))
                    toRemove.Add(addedName);
            }
            foreach (string remove in toRemove)
            {
                Com.Items.Remove(remove);
            }
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
            if (image != null)
            {
                image.Dispose();
            }
            
            image = new HImage(hdev_export.ho_Image);
            hSmartWindowControlWPF1.HalconWindow.DispObj(image);
            
        }
        private void grapAction1()
        {
            OnClearAllObjects();
            //hdev_export.GrapCamera();
            hdev_export.ReadImage(System.Environment.CurrentDirectory + "\\ModelImage.tiff");
            if (image != null)
            {
                image.Dispose();
            }

            image = new HImage(hdev_export.ho_Image);
            hSmartWindowControlWPF1.HalconWindow.DispObj(image);

        }
        private void grapAction2()
        {
            OnClearAllObjects();
            //hdev_export.GrapCamera();
            hdev_export.ReadImage(ImageFiles[ImageIndex]);
            ImageIndex++;
            if (ImageIndex >= ImageFiles.Length)
            {
                ImageIndex = 0;
            }
            ImageIndexNum.Text = ImageIndex.ToString();
            if (image != null)
            {
                image.Dispose();
            }

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
            
            
            if ((bool)ImageCheckBox.IsChecked)
            {
                hdev_export.ReadImage(System.Environment.CurrentDirectory + "\\ModelImage.tiff");
            }
            else
            {
                hdev_export.GrapCamera();
            }
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
                if ((bool)ImageCheckBox.IsChecked)
                {
                    grapAction1();
                }
                image.Dispose();
                image = new HImage(hdev_export.ho_Image);
                image.DispObj(Window);
                ImgReduced = image.ReduceDomain(Rectangle);
                ImgReduced.InspectShapeModel(out ModelRegion, 1, 25);
                ShapeModel = new HShapeModel(ImgReduced, 4, 0, new HTuple(360.0).TupleRad().D,
new HTuple(1.0).TupleRad().D, "none", "use_polarity", 25, 10);
                Window.SetColor("green");
                Window.SetDraw("margin");
                ModelRegion.DispObj(Window);
                if (!(bool)ImageCheckBox.IsChecked)
                {
                    image.WriteImage("tiff", 0, System.Environment.CurrentDirectory + "\\ModelImage.tiff");
                }
                
                ShapeModel.WriteShapeModel(System.Environment.CurrentDirectory + "\\ShapeModel.shm");
            }
            else
            {
                MsgTextBox.Text = AddMessage("少于2个Region，无法创建");
            }
                
            
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            if (!ShutdownFlag)
            {
                FileStream fileStream = new FileStream(System.Environment.CurrentDirectory + "\\CoorPar.dat", FileMode.Create);
                BinaryFormatter b = new BinaryFormatter();
                b.Serialize(fileStream, CoorPar);
                fileStream.Close();
                hdev_export.CloseCamera();
            }


           
        }

        private void Calib1Button_Click(object sender, RoutedEventArgs e)
        {



            grapAction();
            Action();
            if (RowCheck.Length == 1)
            {
                CoorPar.Row1 = RowCheck.D;
                CoorPar.DRow1 = CX;
                MsgTextBox.Text = AddMessage("CX1: " + CoorPar.Row1.ToString() + "; " + CoorPar.DRow1.ToString());
            }
        }
        private bool FindLines()
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
            HOperatorSet.Threshold(EdgeAmplitude, out region1, CoorPar.ShuXianThreshold, 255);
            region1.DispObj(Window);
            HTuple angle, dist;
            HOperatorSet.HoughLines(region1, 8, CoorPar.ShuXianPixNum, 30, 30, out angle, out dist);
            HObject LinesHNF;
            if (dist.Length > 0)
            {
                HOperatorSet.GenRegionHline(out LinesHNF, angle, dist);
                LinesHNF.DispObj(Window);
                Line1Angle = angle.DArr[0];
                Line1Dist = dist.DArr[0];
            }
            else
            {
                return false;
            }

            ImgReduced = image.ReduceDomain(Rec2Region);
            HOperatorSet.SobelDir(ImgReduced, out EdgeAmplitude, out EdgeDirection, "sum_abs", 3);
            HOperatorSet.Threshold(EdgeAmplitude, out region1, CoorPar.HengXianThreshold, 255);
            region1.DispObj(Window);
            HOperatorSet.HoughLines(region1, 8, CoorPar.HengXianPixNum, 30, 30, out angle, out dist);
            HObject LinesHNF1;

            if (dist.Length > 0)
            {
                HOperatorSet.GenRegionHline(out LinesHNF1, angle, dist);

                LinesHNF1.DispObj(Window);
                Line2Angle = angle.DArr[0];
                Line2Dist = dist.DArr[0];
            }
            else
            {
                return false;
            }
            CrossPoint = GetCrostPoint(Line1Angle, Line1Dist, Line2Angle, Line2Dist);
            //MsgTextBox.Text = AddMessage(CrossPoint[0].ToString() + ","+ CrossPoint[1].ToString());
            Window.SetColor("green");
            Window.SetDraw("fill");
            Window.DispCross(CrossPoint[1], CrossPoint[0], 60, 0);
            return true;
        }
        private void CalcButton_Click(object sender, RoutedEventArgs e)
        {
            if (CoorPar.DRow1 != CoorPar.DRow2)
            {

                CoorPar.Calc();
                MsgTextBox.Text = AddMessage("像素计算完成: " + CoorPar.DisT.ToString());
            }
            else
            {
                MsgTextBox.Text = AddMessage("点1、点2重合，无法计算");
            }
        }
        struct DWORDStruct
        {
            public String RigisterName;
            public int Value;
        }
        private async void WriteCoorData()
        {
            FileStream stream;
            WriteCoor.IsEnabled = false;
            System.Windows.Forms.OpenFileDialog ofdialog = new System.Windows.Forms.OpenFileDialog();
            ofdialog.InitialDirectory = "D:\\";
            ofdialog.Filter = "Microsoft Excel 2013|*.xlsx";
            ofdialog.RestoreDirectory = true;
            if (ofdialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                
                try
                {
                    stream = new FileStream(ofdialog.FileName, FileMode.Open);

                }
                catch (IOException ex)
                {
                    MsgTextBox.Text = AddMessage(ex.Message);
                    WriteCoor.IsEnabled = true;
                    return;
                }
                using (stream)
                {
                    ExcelPackage package = new ExcelPackage(stream);
                    ExcelWorksheet sheet = package.Workbook.Worksheets[1];
                    if (sheet == null)
                    {
                        MsgTextBox.Text = AddMessage("Excel format error!");
                        WriteCoor.IsEnabled = true;
                        return;
                    }
                    if (!sheet.Cells[1,1].Value.Equals("NAME"))
                    {
                        MsgTextBox.Text = AddMessage("Excel format error!");
                        WriteCoor.IsEnabled = true;
                        return;
                    }
                    int lastRow = sheet.Dimension.End.Row;
                    for (int i = 2; i < lastRow; i++)
                    {
                        if (sheet.Cells[i,1].Value != null && sheet.Cells[i, 2].Value != null)
                        {
                            await Task.Delay(10);
                            lock (modbustcp)
                            {
                                aS300ModbusTCP.WriteDWORD(sheet.Cells[i,1].Value.ToString(), int.Parse(sheet.Cells[i, 2].Value.ToString()));
                            }
                        }
                    }
                    MsgTextBox.Text = AddMessage("写入坐标数据完成");
                    WriteCoor.IsEnabled = true;
                }
            }
            else
            {
                WriteCoor.IsEnabled = true;
                return;
            }

        }
        private async void ReadCoorData()
        {
            ReadCoor.IsEnabled = false;
            List<DWORDStruct> DD20000 = new List<DWORDStruct>();


            System.Windows.Forms.SaveFileDialog sfdialog = new System.Windows.Forms.SaveFileDialog();
            sfdialog.Filter = "Microsoft Excel 2013|*.xlsx";
            sfdialog.DefaultExt = "xlsx";
            sfdialog.AddExtension = true;
            sfdialog.Title = "Save Excel";
            sfdialog.InitialDirectory = "D:\\";
            sfdialog.FileName = DateTime.Now.ToString("yyyyMMdd") + DateTime.Now.ToString("HHmmss");
            DialogResult? result = sfdialog.ShowDialog();
            if (result == null || result.Value != System.Windows.Forms.DialogResult.OK)
            {
                ReadCoor.IsEnabled = true;
                return;
                
            }
            else
            {
                for (int i = 0; i < 200; i++)
                {
                    await Task.Delay(10);
                    DWORDStruct dw = new DWORDStruct();
                    dw.RigisterName = "D" + (20000 + 2 * i).ToString();
                    lock (modbustcp)
                    {
                        dw.Value = aS300ModbusTCP.ReadDWORD(dw.RigisterName);
                    }
                    DD20000.Add(dw);
                }
                FileStream stream;
                try
                {
                    stream = new FileStream(sfdialog.FileName,FileMode.Create);
                }
                catch (IOException ex)
                {

                    ReadCoor.IsEnabled = true;
                    MsgTextBox.Text = AddMessage(ex.Message);
                    return;
                }
                using (stream)
                {
                    ExcelPackage package = new ExcelPackage(stream);
                    package.Workbook.Worksheets.Add("PLC坐标数据");
                    ExcelWorksheet sheet = package.Workbook.Worksheets[1];
                    sheet.Cells[1, 1].Value = "NAME";
                    sheet.Cells[1, 2].Value = "VALUE";
                    using (ExcelRange range = sheet.Cells[1,1,1,2])
                    {
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Gray);
                        range.AutoFitColumns(4);
                    }
                    int pos = 2;
                    foreach (DWORDStruct item in DD20000)
                    {
                        sheet.Cells[pos, 1].Value = item.RigisterName;
                        sheet.Cells[pos, 2].Value = item.Value;
                        pos++;
                    }
                    package.Save();
                    
                    ReadCoor.IsEnabled = true;
                    MsgTextBox.Text = AddMessage("导出坐标数据完成");
                }
                
            }

     
        }
        private void PLCScanAction(string str)
        {
            string[] strs = str.Split('\r');
            MsgTextBox.Text = AddMessage(strs[0]);
            BarcodeString.Text = strs[0];
            string NewStr = strs[0];
            if (NewStr != "Error")
            {
                string FindStr = mySQLClass.FindResult(NewStr);
                MsgTextBox.Text = AddMessage(FindStr);
                if (FindStr.Length == 36)
                {
                    bool[] Rb = new bool[36];
                    for (int i = 0; i < 6; i++)
                    {
                        if (FindStr[0 + 6 * i] == '1')
                        {
                            Rb[0 + i] = true;
                        }
                        else
                        {
                            Rb[0 + i] = false;
                        }
                        if (FindStr[1 + 6 * i] == '1')
                        {
                            Rb[6 + i] = true;
                        }
                        else
                        {
                            Rb[6 + i] = false;
                        }
                        if (FindStr[2 + 6 * i] == '1')
                        {
                            Rb[12 + i] = true;
                        }
                        else
                        {
                            Rb[12 + i] = false;
                        }
                        if (FindStr[3 + 6 * i] == '1')
                        {
                            Rb[35 - i] = true;
                        }
                        else
                        {
                            Rb[35 - i] = false;
                        }
                        if (FindStr[4 + 6 * i] == '1')
                        {
                            Rb[29 - i] = true;
                        }
                        else
                        {
                            Rb[29 - i] = false;
                        }
                        if (FindStr[5 + 6 * i] == '1')
                        {
                            Rb[23 - i] = true;
                        }
                        else
                        {
                            Rb[23 - i] = false;
                        }
                    }
                    bool[] _AllowBarRecord;
                    lock (modbustcp)
                    {
                        aS300ModbusTCP.WriteMultCoils("M5103", Rb);
                        _AllowBarRecord = aS300ModbusTCP.ReadCoils("M6000", 5);
                        //aS300ModbusTCP.WriteSigleCoil("M5100", true);
                    }
                    if (UpdateRecode(NewStr, _AllowBarRecord[4]))
                    {
                        lock (modbustcp)
                        {
                            aS300ModbusTCP.WriteSigleCoil("M5100", true);
                        }
                    }
                    else
                    {
                        lock (modbustcp)
                        {
                            aS300ModbusTCP.WriteSigleCoil("M5101", true);
                        }
                    }
                }
                else
                {
                    lock (modbustcp)
                    {
                        aS300ModbusTCP.WriteSigleCoil("M5101", true);
                    }
                }
            }
            else
            {
                lock (modbustcp)
                {
                    aS300ModbusTCP.WriteSigleCoil("M5101", true);
                }
            }
        }
        private bool UpdateRecode(string barcode , bool mode)
        {
            string[] arrField = new string[1];
            string[] arrValue = new string[1];
            try
            {
                OraDB oraDB = new OraDB("fpcsfcdb", "sfcdar", "sfcdardata");
                string tablename = "sfcdata.barautbind";
                if (oraDB.isConnect())
                {
                    arrField[0] = "SCPNLBAR";
                    arrValue[0] = barcode;
                    DataSet s1 = oraDB.selectSQL(tablename.ToUpper(), arrField, arrValue);
                    DataTable PanelDt = s1.Tables[0];
                    if (PanelDt.Rows.Count == 0)
                    {
                        MsgTextBox.Text = AddMessage(barcode + "数据库无记录");
                        oraDB.disconnect();
                        return false;
                    }
                    else
                    {
                        if (PanelDt.Rows[0]["BLID"].ToString() == "" || mode)
                        {
                            string[,] arrFieldAndNewValue = { { "BLDATE", "to_date('" + DateTime.Now.ToString() + "', 'yyyy/mm/dd hh24:mi:ss')" }, { "BLID", CoorPar.ZhiJuBianHao }, { "BLNAME", CoorPar.ZhiJuMingChen }, { "BLUID", CoorPar.ZheXianRenYuan }, { "BLMID", CoorPar.JiTaiBianHao } };

                            string[,] arrFieldAndOldValue = { { "SCPNLBAR", barcode } };
                            oraDB.updateSQL2(tablename.ToUpper(), arrFieldAndNewValue, arrFieldAndOldValue);
                            MsgTextBox.Text = AddMessage(barcode + "数据更新完成");
                            return true;

                        }
                        else
                        {
                            MsgTextBox.Text = AddMessage(barcode + " 条码重复");
                            oraDB.disconnect();
                            return false;
                        }
                    }
                    


                }
                else
                {
                    MsgTextBox.Text = AddMessage("数据库连接失败");
                    oraDB.disconnect();
                    return false;
                    
                }
            }
            catch (Exception ex)
            {
                MsgTextBox.Text = AddMessage(ex.Message);
                return false;
            }

            
        }
        private async void PLCRun()
        {
            bool ScanCMD = false, GigECMD = false;
            while (true)
            {
                await Task.Delay(100);
                try
                {
                    lock (modbustcp)
                    {
                        PLC_In = aS300ModbusTCP.ReadCoils("M5000", 96);
                        CX = aS300ModbusTCP.ReadDWORD("D6");
                        TextX1.Text = ((double)CX / 100).ToString();
                        CY = aS300ModbusTCP.ReadDWORD("D10");
                        TextY1.Text = ((double)CY / 100).ToString();
                    }
                    if (ScanCMD != PLC_In[0])
                    {
                        ScanCMD = PLC_In[0];
                        if (ScanCMD)
                        {
                            ScanCMD = false;
                            lock (modbustcp)
                            {
                                aS300ModbusTCP.WriteSigleCoil("M5000", false);
                            }
                            Scan.GetBarCode(PLCScanAction);

                        }
                    }
                    
                    if (GigECMD != PLC_In[2])
                    {
                        GigECMD = PLC_In[2];
                        if (GigECMD)
                        {
                            GigECMD = false;
                            
                            dispatcherTimer.Stop();
                            grapAction();
                            //hdev_export.SaveImage();
                            Action();
                            if (RowCheck.Length == 1)
                            {
                                if (FindLines())
                                {
                                    TextMaxX1.Text = CrossPoint[0].ToString("F2");
                                    TextMinX1.Text = CrossPoint[1].ToString("F2");
                                    int DD12, DD14, DD16;
                                    DD12 = (int)((CrossPoint[1] - CoorPar.Y0) * -1 / CoorPar.DisT);
                                    DD14 = (int)(((CrossPoint[0] - CoorPar.X0) * -1) / CoorPar.DisT);
                                    //DD16 = Convert.ToInt32(((CoorPar.Line1Angle + CoorPar.Line2Angle - Line1Angle - Line2Angle) / 2 / Math.PI * 180) * 100);
                                    if (AngleCheck.D > Math.PI)
                                    {
                                        DD16 = Convert.ToInt32(((AngleCheck.D - 2 * Math.PI) / Math.PI * 180) * 100);

                                        //MsgTextBox.Text = AddMessage(( - Line1Angle + CoorPar.Line1Angle).ToString() + "," + (Line2Angle - CoorPar.Line2Angle).ToString() +"," + (AngleCheck.D - 2 * Math.PI).ToString());
                                    }
                                    else
                                    {
                                        DD16 = Convert.ToInt32((AngleCheck.D / Math.PI * 180) * 100);
                                        //MsgTextBox.Text = AddMessage(( - Line1Angle + CoorPar.Line1Angle).ToString() + "," + (Line2Angle - CoorPar.Line2Angle).ToString() + "," + AngleCheck.D .ToString());
                                    }
                                    if (DD12 > 1000 || DD12 < -1000 || DD14 > 1000 || DD14 < -1000)
                                    {
                                        lock (modbustcp)
                                        {
                                            aS300ModbusTCP.WriteDWORD("D12", 0);
                                            aS300ModbusTCP.WriteDWORD("D14", 0);
                                            aS300ModbusTCP.WriteDWORD("D16", 0);
                                            MsgTextBox.Text = AddMessage("数值异常");
                                            hdev_export.SaveImage();
                                            aS300ModbusTCP.WriteSigleCoil("M5143", true);
                                        }

                                    }
                                    else
                                    {
                                        lock (modbustcp)
                                        {
                                            aS300ModbusTCP.WriteDWORD("D12", DD12);
                                            aS300ModbusTCP.WriteDWORD("D14", DD14);
                                            aS300ModbusTCP.WriteDWORD("D16", DD16);


                                            MsgTextBox.Text = AddMessage("X:" + DD12.ToString() + ",Y:" + DD14.ToString() + ",U:" + DD16.ToString());
                                            aS300ModbusTCP.WriteSigleCoil("M5140", true);
                                        }

                                    }
                                    

                                }
                                else
                                {

                                    lock (modbustcp)
                                    {
                                        aS300ModbusTCP.WriteDWORD("D12", 0);
                                        aS300ModbusTCP.WriteDWORD("D14", 0);
                                        aS300ModbusTCP.WriteDWORD("D16", 0);
                                        MsgTextBox.Text = AddMessage("直线未找到");
                                        hdev_export.SaveImage();
                                        aS300ModbusTCP.WriteSigleCoil("M5143", true);
                                    }
                                }
                                
                            }
                            else
                            {
                                lock (modbustcp)
                                {
                                    aS300ModbusTCP.WriteDWORD("D12", 0);
                                    aS300ModbusTCP.WriteDWORD("D14", 0);
                                    aS300ModbusTCP.WriteDWORD("D16", 0);
                                    MsgTextBox.Text = AddMessage("未找到模板");
                                    hdev_export.SaveImage();
                                    aS300ModbusTCP.WriteSigleCoil("M5143", true);
                                }

                            }
                            
                            

                            //await Task.Delay(100);
                        }
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
        private double[] GetCrostPoint(double Angle1, double Dist1, double Angle2, double Dist2)
        {
            double[] xy0 = new double[2];
            double a1, b1, a2, b2;
            a2 = Math.Tan(Angle2 - Math.PI / 2);
            b2 = Dist2 / Math.Sin(Angle2);
            if (Angle1 == 0)
            {
                xy0[0] = Dist1;
            }
            else
            {
                a1 = Math.Tan(Angle1 - Math.PI / 2);
                b1 = Dist1 / Math.Sin(Angle1);
                xy0[0] = (b2 - b1) / (a1 - a2);
            }
            xy0[1] = a2 * xy0[0] + b2;
            return xy0;
        }
        public void PrintBarcode(string str)
        {
            string[] strs = str.Split('\r');
            MsgTextBox.Text = AddMessage(strs[0]);
            BarcodeString.Text = strs[0];
        }
        void Init()
        {

            
            hdev_export.OpenCamera();
            MsgTextBox.Text = AddMessage("相机开启");
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
            HObject Rec1;
            HOperatorSet.GenRectangle1(out Rec1,50,300,1700,1400);
            HRegion Region1 = new HRegion(Rec1);
            HImage imgreduced = image.ReduceDomain(Region1);
            ShapeModel.FindShapeModel(imgreduced, 0,
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
                if (AngleCheck.DArr[0] > Math.PI)
                {
                    TextAngle1.Text = ((AngleCheck.DArr[0] - 2 * Math.PI) / Math.PI * 180).ToString("F2");
                }
                else
                {
                    TextAngle1.Text = (AngleCheck.DArr[0] / Math.PI * 180).ToString("F2");
                }
                
                TextScore1.Text = Score.DArr[0].ToString("F2");

            }
        }
        private void Action2()
        {

        }
        private void Calib2Button_Click(object sender, RoutedEventArgs e)
        {
            grapAction();
            Action();
            if (RowCheck.Length == 1)
            {
                CoorPar.Row2 = RowCheck.D;
                CoorPar.DRow2 = CX;
                MsgTextBox.Text = AddMessage("CX2: " + CoorPar.Row2.ToString() + "; " + CoorPar.DRow2.ToString());
            }
        }

        //private void ImgLive_Click(object sender, RoutedEventArgs e)
        //{
            
        //    if (iCImagingControl.DeviceValid)
        //    {
        //        ImgLive.IsEnabled = false;
        //        ImgStop.IsEnabled = true;
        //        iCImagingControl.LiveStart();
        //    }
        //}

        //private void ImgStop_Click(object sender, RoutedEventArgs e)
        //{
        //    if (iCImagingControl.DeviceValid)
        //    {
        //        iCImagingControl.LiveStop();
        //        ImgLive.IsEnabled = true;
        //        ImgStop.IsEnabled = false;
        //    }
        //}
        //private void ImgSnap()
        //{
        //    if (iCImagingControl.DeviceValid)
        //    {
        //        if (iCImagingControl.LiveVideoRunning)
        //        {
        //            iCImagingControl.LiveStop();
        //            ImgLive.IsEnabled = true;
        //            ImgStop.IsEnabled = false;
        //        }
        //        iCImagingControl.MemorySnapImage();
        //        if (ImgBitmap != null)
        //        {
        //            ImgBitmap.Dispose();
        //        }
        //        ImgBitmap = new Bitmap(iCImagingControl.ImageActiveBuffer.Bitmap);
        //    }
        //}

        //private void HWindowControlWPF2_HInitWindow(object sender, EventArgs e)
        //{
        //    Window2 = HWindowControlWPF2.HalconWindow;
        //    HWindowControlWPF2.HalconWindow.SetPart(0.0, 0.0, new HTuple(ImgBitmap.Height - 1), new HTuple(ImgBitmap.Width - 1));
        //    HWindowControlWPF2.HalconWindow.AttachBackgroundToWindow(new HImage(BitmaptoHImage(ImgBitmap)));
        //    Window2Init = true;

        //}
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

        //private void USBCameraAction_Click(object sender, RoutedEventArgs e)
        //{
        //    ImgSnap();
        //    HWindowControlWPF2.HalconWindow.DispObj(new HImage(BitmaptoHImage(ImgBitmap)));
        //}
        /// <summary>
        /// 找竖线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawButton_Click1(object sender, RoutedEventArgs e)
        {
            HTuple draw_id;

            //hdev_export.GrapCamera();
            hdev_export.ReadImage(System.Environment.CurrentDirectory + "\\ModelImage.tiff");
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
            grapAction1();
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

        private void ReadCoor_Click(object sender, RoutedEventArgs e)
        {
            ReadCoorData();
        }

        private void FunctionTest_Click(object sender, RoutedEventArgs e)
        {
            grapAction1();
            Action();
            if (RowCheck.Length > 0)
            {
                if (FindLines())
                {
                    CoorPar.Line1Angle = Line1Angle;
                    CoorPar.Line1Dist = Line1Dist;
                    CoorPar.Line2Angle = Line2Angle;
                    CoorPar.Line2Dist = Line2Dist;
                    CoorPar.X0 = CrossPoint[0];
                    CoorPar.Y0 = CrossPoint[1];
                    MsgTextBox.Text = AddMessage("直线保存成功");
                }
                else
                {
                    MsgTextBox.Text = AddMessage("找直线失败");
                }
            }
            else
            {
                MsgTextBox.Text = AddMessage("未找到模板");
            }
           
        }

        private void Com_DropDownClosed(object sender, EventArgs e)
        {
            CoorPar.ScanCom = Com.Text;
        }

        private void FunctionButton_Click(object sender, RoutedEventArgs e)
        {
            Scan.GetBarCode(PrintBarcode);
        }

        private void ImageCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ImageFiles = Directory.GetFiles(@"D:\image");
            ImageIndex = 0;
        }

        private void MuBanContrast_TextChanged(object sender, TextChangedEventArgs e)
        {
            CoorPar.MuBanContrast = new HTuple(double.Parse(MuBanContrast.Text));
        }

        private void ShuXianThreshold_TextChanged(object sender, TextChangedEventArgs e)
        {
            CoorPar.ShuXianThreshold = new HTuple(double.Parse(ShuXianThreshold.Text));
        }

        private void ShuXianPixNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            CoorPar.ShuXianPixNum = new HTuple(double.Parse(ShuXianPixNum.Text));
        }

        private void HengXianThreshold_TextChanged(object sender, TextChangedEventArgs e)
        {
            CoorPar.HengXianThreshold = new HTuple(double.Parse(HengXianThreshold.Text));
        }

        private void HengXianPixNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            CoorPar.HengXianPixNum = new HTuple(double.Parse(HengXianPixNum.Text));
        }

        private void ImageIndexNumAction(object sender, RoutedEventArgs e)
        {
            if ((bool)ImageCheckBox.IsChecked)
            {
                ImageIndex--;
                if (ImageIndex < 0)
                {
                    ImageIndex = ImageFiles.Length - 1;
                }
                ImageIndexNum.Text = ImageIndex.ToString();
            }
        }

        private async void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            //ActionMessages.ExecuteAction("winclose");
            mydialog.changeaccent("Red");
            var r = await mydialog.showconfirm("确定要关闭程序吗？");
            if (r)
            {
                System.Windows.Application.Current.Shutdown();
            }
            else
            {
                mydialog.changeaccent("Cobalt");
            }
        }

        private async void LoadinButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> r;
            if (!Loadin)
            {
                r = await mydialog.showlogin();
                if (r[1] == "543337")
                {
                    Loadin = true;
                    LoadinButton.Content = "登出";
                    ChuangJianTabItem.IsEnabled = true;
                    BiaoDingTabItem.IsEnabled = true;
                    SaoMaTabItem.IsEnabled = true;
                    QiTaTabItem.IsEnabled = true;
                }
                if (r[1] == "123456")
                {
                    Loadin = true;
                    LoadinButton.Content = "登出";
                    ZhiJuBianHao.IsReadOnly = false;
                    ZhiJuMingChen.IsReadOnly = false;
                    ZheXianRenYuan.IsReadOnly = false;
                    JiTaiBianHao.IsReadOnly = false;
                }

            }
            else
            {
                Loadin = false;
                LoadinButton.Content = "登录";
                ChuangJianTabItem.IsEnabled = false;
                BiaoDingTabItem.IsEnabled = false;
                SaoMaTabItem.IsEnabled = false;
                QiTaTabItem.IsEnabled = false;
                ZhiJuBianHao.IsReadOnly = true;
                ZhiJuMingChen.IsReadOnly = true;
                ZheXianRenYuan.IsReadOnly = true;
                JiTaiBianHao.IsReadOnly = true;
                FileStream fileStream = new FileStream(System.Environment.CurrentDirectory + "\\CoorPar.dat", FileMode.Create);
                BinaryFormatter b = new BinaryFormatter();
                b.Serialize(fileStream, CoorPar);
                fileStream.Close();
            }
        }

        private void ZhiJuBianHao_LostFocus(object sender, RoutedEventArgs e)
        {
            CoorPar.ZhiJuBianHao = ZhiJuBianHao.Text;
        }

        private void ZhiJuMingChen_LostFocus(object sender, RoutedEventArgs e)
        {
            CoorPar.ZhiJuMingChen = ZhiJuMingChen.Text;
        }

        private void ZheXianRenYuan_LostFocus(object sender, RoutedEventArgs e)
        {
            CoorPar.ZheXianRenYuan = ZheXianRenYuan.Text;
        }

        private void JiTaiBianHao_LostFocus(object sender, RoutedEventArgs e)
        {
            CoorPar.JiTaiBianHao = JiTaiBianHao.Text;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UpdateRecode("EE6-WLB-HB0015-0105", false);
        }

        private void FunctionButton1_Click(object sender, RoutedEventArgs e)
        {
            if (BarcodeString.Text != "Error" && BarcodeString.Text.Length > 5)
            {
                MsgTextBox.Text = AddMessage(mySQLClass.FindResult(BarcodeString.Text));
            }
        }

        private void WriteCoor_Click(object sender, RoutedEventArgs e)
        {
            WriteCoorData();
        }

        private void FindModelButton_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)ImageCheckBox.IsChecked)
            {
                grapAction2();
            }
            else
            {
                grapAction();
            }
            
            Action();
        }

        private void FindLineButton_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)ImageCheckBox.IsChecked)
            {
                grapAction2();
            }
            else
            {
                grapAction();
            }
            Action();
            if (RowCheck.Length == 1)
            {
                if (FindLines())
                {
                    TextMaxX1.Text = CrossPoint[0].ToString("F2");
                    TextMinX1.Text = CrossPoint[1].ToString("F2");
                }  

                //if (Score.D >= 0.7)
                //{


                //    FindLines();
                //    MsgTextBox.Text = AddMessage("查找模板完成");
                //}
                //else
                //{
                //    MsgTextBox.Text = AddMessage("模板质量低");
                //}

            }
            else
            {
                MsgTextBox.Text = AddMessage("未找到模板");
            }










        }

        /// <summary>
        /// 找横线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawButton_Click2(object sender, RoutedEventArgs e)
        {
            HTuple draw_id;

            //hdev_export.GrapCamera();
            hdev_export.ReadImage(System.Environment.CurrentDirectory + "\\ModelImage.tiff");
            background_image = hdev_export.ho_Image;
            hSmartWindowControlWPF1.HalconWindow.AttachBackgroundToWindow(new HImage(background_image));
            hdev_export.add_new_drawing_object("rectangle2", hSmartWindowControlWPF1.HalconID, out draw_id);
            SetCallbacks(draw_id, 2);
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
            Init();
            Com.Text = CoorPar.ScanCom;
            LoadinButton.Content = "登录";
            ChuangJianTabItem.IsEnabled = false;
            BiaoDingTabItem.IsEnabled = false;
            SaoMaTabItem.IsEnabled = false;
            QiTaTabItem.IsEnabled = false;
            ZhiJuBianHao.IsReadOnly = true;
            ZhiJuMingChen.IsReadOnly = true;
            ZheXianRenYuan.IsReadOnly = true;
            JiTaiBianHao.IsReadOnly = true;
            ZhiJuBianHao.Text = CoorPar.ZhiJuBianHao;
            ZhiJuMingChen.Text = CoorPar.ZhiJuMingChen;
            ZheXianRenYuan.Text = CoorPar.ZheXianRenYuan;
            JiTaiBianHao.Text = CoorPar.JiTaiBianHao;
            if (CoorPar.MuBanContrast != null)
            {
                MuBanContrast.Text = CoorPar.MuBanContrast.D.ToString();
            }
            if (CoorPar.ShuXianPixNum != null)
            {
                ShuXianPixNum.Text = CoorPar.ShuXianPixNum.D.ToString();
            }
            if (CoorPar.ShuXianThreshold != null)
            {
                ShuXianThreshold.Text = CoorPar.ShuXianThreshold.D.ToString();
            }
            if (CoorPar.HengXianPixNum != null)
            {
                HengXianPixNum.Text = CoorPar.HengXianPixNum.D.ToString();
            }
            if (CoorPar.HengXianThreshold != null)
            {
                HengXianThreshold.Text = CoorPar.HengXianThreshold.D.ToString();
            }
            Scan.ini(CoorPar.ScanCom);
            Scan.Connect();
            MsgTextBox.Text = AddMessage("扫码枪连接");
            //hdev_export.GrapCamera();
            hdev_export.ReadImage(System.Environment.CurrentDirectory + "\\ModelImage.tiff");
            image = new HImage(hdev_export.ho_Image);
            hSmartWindowControlWPF1.HalconWindow.DispObj(image);
            dispatcherTimer.Tick += new EventHandler(GrapContinue);
            dispatcherTimer.Interval = new TimeSpan(1000000);//100毫微秒为单位
            if (!Directory.Exists(@"D:\image"))
            {
                Directory.CreateDirectory(@"D:\image");
            }
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
            //datagrid.ItemsSource = (System.Collections.IEnumerable)mySQLClass.test().Tables[0];
            //var aaaa = mySQLClass.test().Tables[0];
            //try
            //{
            //    iCImagingControl.LoadDeviceStateFromFile("device.xml", true);
            //}
            //catch (Exception ex)
            //{

            //    MsgTextBox.Text = AddMessage(ex.Message);
            //}

            //if (!iCImagingControl.DeviceValid)
            //{
            //    iCImagingControl.ShowDeviceSettingsDialog();
            //}
            ////imageViewer.viewController.repaint();

            //if (iCImagingControl.DeviceValid)
            //{
            //    iCImagingControl.SaveDeviceStateToFile("device.xml");
            //    iCImagingControl.Size = new System.Drawing.Size(600, 400);
            //    iCImagingControl.LiveDisplayDefault = false;
            //    iCImagingControl.LiveDisplayHeight = iCImagingControl.Height;
            //    iCImagingControl.LiveDisplayWidth = iCImagingControl.Width;
            //    ImgSnap();
            //    //SmartWindowControlWPF2Init();
            //}
            //ImgStop.IsEnabled = false;
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
            //MessageStr += "\n" + System.DateTime.Now.ToString() + " " + str;
            MessageStr += "\n" + " " + str;
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
            //hdev_export.GrapCamera();
            hdev_export.ReadImage(System.Environment.CurrentDirectory + "\\ModelImage.tiff");
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
        public double Row1;
        public double Row2;
        public int DRow1;
        public int DRow2;
        public double DisT;
        public double Line1Angle;
        public double Line1Dist;
        public double Line2Angle;
        public double Line2Dist;
        public double X0;
        public double Y0;
        public string ScanCom;
        public HTuple ShuXianThreshold;
        public HTuple ShuXianPixNum;
        public HTuple HengXianThreshold;
        public HTuple HengXianPixNum;
        public HTuple MuBanContrast;
        public string ZhiJuBianHao;
        public string ZhiJuMingChen;
        public string ZheXianRenYuan;
        public string JiTaiBianHao;

        public void Calc()
        {
            DisT = (Row1 - Row2) / (DRow1 - DRow2);
        }
    }

}
