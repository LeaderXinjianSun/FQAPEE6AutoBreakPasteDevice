//
// File generated by HDevelop for HALCON/.NET (C#) Version 13.0
//
//  This file is intended to be used with the HDevelopTemplate or
//  HDevelopTemplateWPF projects located under %HALCONEXAMPLES%\c#

using System;
using HalconDotNet;

public partial class HDevelopExport
{
    public HTuple hv_ExpDefaultWinHandle;
    public HTuple hv_AcqHandle = null;
    public HObject ho_Image = null;

    public bool OpenCamera()
    {
        try
        {
            if (hv_AcqHandle != null)
            {
                HOperatorSet.CloseFramegrabber(hv_AcqHandle);
            }
            //HOperatorSet.OpenFramegrabber("GenICamTL", 0, 0, 0, 0, 0, 0, "progressive", -1,
            //    "default", -1, "false", "default", "MER-500-14GM(00-21-49-00-5E-50)", 0,
            //    -1, out hv_AcqHandle);
    //        HOperatorSet.OpenFramegrabber("DirectShow", 1, 1, 0, 0, 0, 0, "default", 8, "rgb",
    //-1, "false", "default", "[0] HD USB Camera", 0, -1, out hv_AcqHandle);
        //    HOperatorSet.OpenFramegrabber("DirectShow", 1, 1, 0, 0, 0, 0, "default", 8, "rgb",
        //-1, "false", "default", "[0] DMK 72AUC02", 0, -1, out hv_AcqHandle);
            HOperatorSet.OpenFramegrabber("GigEVision", 0, 0, 0, 0, 0, 0, "default", -1,
    "default", -1, "false", "default", "BottomCamera", 0, -1, out hv_AcqHandle);
            HOperatorSet.SetFramegrabberParam(hv_AcqHandle, "ExposureTime", 650.0);
            return true;
        }
        catch
        {
            return false;
        }
    }
    public void CloseCamera()
    {
        if (hv_AcqHandle != null)
        {
            HOperatorSet.CloseFramegrabber(hv_AcqHandle);
        }
    }
    public bool GrapCamera()
    {
        try
        {
            if (ho_Image != null)
            {
                ho_Image.Dispose();
            }
            HOperatorSet.GrabImage(out ho_Image, hv_AcqHandle);
            //HOperatorSet.WriteImage(ho_Image, "tiff", 0, "d:/image/"+ DateTime.Now.ToString("yyyyMMdd") + DateTime.Now.ToString("HHmmss") + ".bmp");
            
            return true;
        }
        catch
        {

            return false;
        }
    }
    public void SaveImage()
    {
        HOperatorSet.WriteImage(ho_Image, "tiff", 0, "d:/image/" + DateTime.Now.ToString("yyyyMMdd") + DateTime.Now.ToString("HHmmss"));
    }
    public bool ReadImage(string filename)
    {
        //"D:/EE62017111002/FQAPEE6AutoBreakPasteDevice/FQAPEE6AutoBreakPasteDeviceUI/FQAPEE6AutoBreakPasteDeviceUI/bin/Debug/ModelImage.tiff"
        try
        {
            HOperatorSet.ReadImage(out ho_Image, filename);
            return true;
        }
        catch 
        {

            return false;
        }
    }
    public bool GrapCamera(string path)
    {
        try
        {
            if (ho_Image != null)
            {
                ho_Image.Dispose();
            }
            HOperatorSet.ReadImage(out ho_Image, path);
            return true;
        }
        catch
        {

            return false;
        }
    }
    // Procedures 
    // Chapter: Develop
    // Short Description: Switch dev_update_pc, dev_update_var and dev_update_window to 'off'. 
    public void dev_update_off()
    {

        // Initialize local and output iconic variables 
        //This procedure sets different update settings to 'off'.
        //This is useful to get the best performance and reduce overhead.
        //
        // dev_update_pc(...); only in hdevelop
        // dev_update_var(...); only in hdevelop
        // dev_update_window(...); only in hdevelop

        return;
    }

    // Local procedures 
    public void add_new_drawing_object(HTuple hv_Type, HTuple hv_WindowHandle, out HTuple hv_DrawID)
    {


        // Initialize local and output iconic variables 
        hv_DrawID = new HTuple();
        //Create a drawing object DrawID of the specified Type
        //and attach it to the graphics window WindowHandle
        //
        if ((int)(new HTuple(hv_Type.TupleEqual("rectangle1"))) != 0)
        {
            HOperatorSet.CreateDrawingObjectRectangle1(100, 100, 200, 200, out hv_DrawID);
        }
        else if ((int)(new HTuple(hv_Type.TupleEqual("circle"))) != 0)
        {
            HOperatorSet.CreateDrawingObjectCircle(200, 200, 120, out hv_DrawID);
        }
        else if ((int)(new HTuple(hv_Type.TupleEqual("rectangle2"))) != 0)
        {
            HOperatorSet.CreateDrawingObjectRectangle2(200, 200, 0, 100, 100, out hv_DrawID);
        }
        else if ((int)(new HTuple(hv_Type.TupleEqual("ellipse"))) != 0)
        {
            HOperatorSet.CreateDrawingObjectEllipse(200, 200, 0, 100, 60, out hv_DrawID);
        }
        else
        {
            throw new HalconException(
                (new HTuple("Unrecognized drawing object type.")).TupleConcat("Either not a valid type or not supported by this procedure"));
        }

        return;
    }

    public void process_image(HObject ho_Image, out HObject ho_EdgeAmplitude, HTuple hv_WindowHandle,
        HTuple hv_DrawID)
    {




        // Local iconic variables 

        HObject ho_Region, ho_ImageReduced;
        // Initialize local and output iconic variables 
        HOperatorSet.GenEmptyObj(out ho_EdgeAmplitude);
        HOperatorSet.GenEmptyObj(out ho_Region);
        HOperatorSet.GenEmptyObj(out ho_ImageReduced);
        try
        {
            //Apply an Sobel edge filter on the background
            //image within the region of interest defined
            //by the drawing object.
            ho_Region.Dispose();
            HOperatorSet.GetDrawingObjectIconic(out ho_Region, hv_DrawID);
            ho_ImageReduced.Dispose();
            HOperatorSet.ReduceDomain(ho_Image, ho_Region, out ho_ImageReduced);
            ho_EdgeAmplitude.Dispose();
            HOperatorSet.SobelAmp(ho_ImageReduced, out ho_EdgeAmplitude, "sum_abs", 3);
            ho_Region.Dispose();
            ho_ImageReduced.Dispose();

            return;
        }
        catch (HalconException HDevExpDefaultException)
        {
            ho_Region.Dispose();
            ho_ImageReduced.Dispose();

            throw HDevExpDefaultException;
        }
    }

    public void display_results(HObject ho_EdgeAmplitude)
    {



        // Local control variables 

        HTuple hv_WindowHandle = new HTuple();
        // Initialize local and output iconic variables 
        //Display the filtered image
        //dev_get_window(...);
        HOperatorSet.SetWindowParam(hv_ExpDefaultWinHandle, "flush", "false");
        HOperatorSet.ClearWindow(hv_ExpDefaultWinHandle);
        HOperatorSet.DispObj(ho_EdgeAmplitude, hv_ExpDefaultWinHandle);
        HOperatorSet.SetWindowParam(hv_ExpDefaultWinHandle, "flush", "true");
        HOperatorSet.FlushBuffer(hv_ExpDefaultWinHandle);

        return;
    }

    // Main procedure 
    private void action()
    {


        // Local iconic variables 

        HObject ho_Image, ho_EdgeAmplitude = null;

        // Local control variables 

        HTuple hv_WindowHandle = new HTuple(), hv_DrawID = null;
        // Initialize local and output iconic variables 
        HOperatorSet.GenEmptyObj(out ho_Image);
        HOperatorSet.GenEmptyObj(out ho_EdgeAmplitude);
        try
        {
            //Initialize visualization
            dev_update_off();
            //dev_close_window(...);
            //dev_open_window(...);
            HOperatorSet.SetPart(hv_ExpDefaultWinHandle, 0, 0, 511, 511);
            //
            //Read background image and attach it to the window
            ho_Image.Dispose();
            HOperatorSet.ReadImage(out ho_Image, "fabrik");
            HOperatorSet.AttachBackgroundToWindow(ho_Image, hv_ExpDefaultWinHandle);
            //
            //Add a drawing object and start the processing loop
            add_new_drawing_object("rectangle1", hv_ExpDefaultWinHandle, out hv_DrawID);
            while ((int)(1) != 0)
            {
                ho_EdgeAmplitude.Dispose();
                process_image(ho_Image, out ho_EdgeAmplitude, hv_ExpDefaultWinHandle, hv_DrawID);
                display_results(ho_EdgeAmplitude);
            }
        }
        catch (HalconException HDevExpDefaultException)
        {
            ho_Image.Dispose();
            ho_EdgeAmplitude.Dispose();

            throw HDevExpDefaultException;
        }
        ho_Image.Dispose();
        ho_EdgeAmplitude.Dispose();

    }

    public void InitHalcon()
    {
        // Default settings used in HDevelop 
        HOperatorSet.SetSystem("width", 512);
        HOperatorSet.SetSystem("height", 512);
    }

    public void RunHalcon(HTuple Window)
    {
        hv_ExpDefaultWinHandle = Window;
        action();
    }

}

