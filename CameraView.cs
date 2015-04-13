using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Views;
using System.Drawing;
using Android.Util;
using Android.Widget;

namespace FingerDroid
{
    class CameraView : FrameLayout, ISurfaceHolderCallback
    {

        SurfaceView mSurfaceView;

        ISurfaceHolder mHolder;
        Camera _Camera;

        public CameraView(Context context)
            : base(context)
        {
            mSurfaceView = new SurfaceView(context);

            AddView(mSurfaceView);

            // Install a SurfaceHolder.Callback so we get notified when the
            // underlying surface is created and destroyed.
            mHolder = mSurfaceView.Holder;
            mHolder.AddCallback(this);
            mHolder.SetType(SurfaceType.PushBuffers);

        }

        public Camera PreviewCamera
        {
            get { return _Camera; }
            set
            {
                _Camera = value;
            }
        }
			
        public void SurfaceChanged(ISurfaceHolder holder, Android.Graphics.Format format, int width, int height)
        {
            // Now that the size is known, set up the camera parameters and begin
            // the preview.
            Camera.Parameters parameters = PreviewCamera.GetParameters();
            parameters.SetPreviewSize(1280, 720);
            RequestLayout();

            PreviewCamera.SetParameters(parameters);
            PreviewCamera.StartPreview();
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            try
            {
                if (PreviewCamera != null)
                {
                    PreviewCamera.SetPreviewDisplay(holder);
                }
            }
            catch (Java.IO.IOException e)
            {
                Log.Error("SurfaceChanged", "IOException caused by setPreviewDisplay()", e);
            }
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            if (PreviewCamera != null)
            {
                PreviewCamera.StopPreview();
            }
        }


    }
}