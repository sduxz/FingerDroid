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
using Android.Graphics;
using Android.Speech.Tts;
using System.IO;
using System.Threading;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using SourceAFIS.Simple;
using SourceAFIS.General;

/******************************************************************************************
问题记录：
1.屏幕重新唤醒后会卡死。
2.是否加个框圈定手指的范围。
3.增加语音提醒。
4.清晰度的检测，实现自动抓取。

*******************************************************************************************/
namespace FingerDroid
{
	[Activity(Label = "FingerDroid", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity,Android.Hardware.Camera.IPictureCallback , Java.Lang.IRunnable //, Android.Hardware.Camera.IPreviewCallback
	{
		int DELAY_MILLIS = 1000;

		CameraView mPreview;
		public Android.Hardware.Camera mCamera;
		Handler hdler = new Handler ();

		ImageView iv = null;
		Boolean isIdentify = true;
		Boolean auto = true;
		//Boolean detected = false;
		string xuehao = null;
		Bitmap pics;
		TextView tv = null;
		TextView result = null;
		TextView frameView = null;
		List<MyPerson> database = null;
	
		// Initialize path to images
		static readonly string ImagePath = "/mnt/ext_sdcard/fingerprints/";

		// Shared AfisEngine instance (cannot be shared between different threads though)
		static AfisEngine Afis;


		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Hide the window title and go fullscreen.
			RequestWindowFeature(WindowFeatures.NoTitle);
			Window.AddFlags(WindowManagerFlags.Fullscreen);

			// Create our Preview view and set it as the content of our activity.
			mPreview = new CameraView(this);

			LinearLayout.LayoutParams param = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent,
													ViewGroup.LayoutParams.WrapContent);
			param.BottomMargin = 10;
			FrameLayout.LayoutParams tparams = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent,
													ViewGroup.LayoutParams.WrapContent);//定义显示组件参数 
			LinearLayout.LayoutParams full = new LinearLayout.LayoutParams (ViewGroup.LayoutParams.MatchParent,
				                                 ViewGroup.LayoutParams.MatchParent);
			
			Button btn = new Button(this);
			btn.Text = "              抓取              ";
			Button enrollbtn = new Button (this);
			enrollbtn.Text = "             录入              ";

			tv = new TextView (this);
			tv.Text = "开始指纹识别。。。";
			tv.SetTextColor(Android.Graphics.Color.LightSkyBlue);
			tv.TextSize = 20;
			result = new TextView (this);
			result.SetTextColor (Android.Graphics.Color.IndianRed);
			result.TextSize = 30;

			View relaView = View.Inflate (this, Resource.Layout.Main, null);
			Switch autoSw = relaView.FindViewById<Switch> (Resource.Id.switch1);
			iv = relaView.FindViewById<ImageView> (Resource.Id.imageView1);
			iv.SetImageResource (Resource.Drawable.Icon);
			frameView = relaView.FindViewById<TextView> (Resource.Id.textView1);
			//SeekBar sk = new SeekBar (this);

			FrameLayout fl = new FrameLayout(this);
			LinearLayout ll = new LinearLayout(this);
			ll.Orientation = Android.Widget.Orientation.Vertical;
			LinearLayout ll2 = new LinearLayout (this);
			RelativeLayout rl = new RelativeLayout (this);

			ll2.AddView (btn, param);
			ll2.AddView (enrollbtn, param);
			//ll2.AddView (sk, tparams);

			ll.AddView (ll2, tparams);
			ll.AddView (tv, param);
			ll.AddView (result, param);
			ll.AddView (relaView, tparams);

			fl.AddView(mPreview);
			fl.AddView (ll, tparams);

			//保持屏幕常亮
			Window.SetFlags (WindowManagerFlags.KeepScreenOn,WindowManagerFlags.KeepScreenOn);
			SetContentView(fl, tparams);

			// Initialize SourceAFIS
			Afis = new AfisEngine();
			// Look up the probe using Threshold = 10
			Afis.Threshold = 30;
			// Enroll some people
			database = new List<MyPerson>();

			//判断数据库是否存在
			if (System.IO.File.Exists (ImagePath + "database.dat")) {
				
				tv.Text = "已载入数据库,开始识别。。。";
				BinaryFormatter formatter = new BinaryFormatter();
				Console.WriteLine ("Reloading database...");
				using (FileStream stream = File.OpenRead (ImagePath + "database.dat"))
					database = (List<MyPerson>)formatter.Deserialize (stream); 
			} else {
				tv.Text = "数据库中无数据，请录入指纹。。。";
			}

			btn.Click += delegate {
				try{
					auto = false;
					mCamera.TakePicture(null,null,this);
					//设置、输出相机参数
					Android.Hardware.Camera.Parameters  p = mCamera.GetParameters();
					string s = p.Flatten();
					Console.WriteLine(s);
					//					p.Set("iso",100.ToString());
					//					p.Set("jpeg-quality",100.ToString());
					//					mCamera.SetParameters(p);

					//输出支持的图片分辨率
					//					IList<Android.Hardware.Camera.Size> ss =  p.SupportedPictureSizes;
					//					foreach(Android.Hardware.Camera.Size aa in ss)
					//					{
					//						Console.WriteLine(aa.Height +"," + aa.Width);
					//					}

				}
				catch(Exception e)
				{
					e.ToString();
				}
			};

			btn.LongClick += delegate {
				mCamera.AutoFocus(null);
				//hdler.PostDelayed (this,DELAY_MILLIS);
			};

			enrollbtn.Click += delegate {
				EditText editT = new EditText(this);
				AlertDialog.Builder  alertDialog = new AlertDialog.Builder(this);
				alertDialog.SetTitle("请输入学号后3位：");
				alertDialog.SetView(editT);
				alertDialog.SetPositiveButton("确认",delegate {
					xuehao = editT.Text;
					tv.Text = "学号为"+ xuehao + "，开始录入指纹。。。";
					isIdentify = false;
				});
				alertDialog.SetNegativeButton("取消",delegate {

				});
				alertDialog.Show();
			};

			autoSw.CheckedChange += delegate {
				if(autoSw.Checked)
					hdler.PostDelayed(this,DELAY_MILLIS);
				else
					hdler.RemoveCallbacks(this);
			}; 



			//zoom放大
//			sk.ProgressChanged += delegate {
//				Android.Hardware.Camera.Parameters  p = mCamera.GetParameters();
//				int maxPa = p.MaxZoom;
//				int maxCa = sk.Max;
//				p.Zoom = maxPa * sk.Progress / maxCa;
//				mCamera.SetParameters(p);
//			};

		}

		protected override void OnResume()
		{
			base.OnResume();

			mCamera = Android.Hardware.Camera.Open();
			mPreview.PreviewCamera = mCamera;
			initCamera ();
		}

		protected override void OnPause()
		{
			base.OnPause();

			if (mCamera != null)
			{
				mPreview.PreviewCamera = null;
				mCamera.Release();	
				mCamera = null;
			}
		}

		//初始化相机  
		private void initCamera()
		{

			mCamera.StopPreview();
			if (null != mCamera)
			{
				Android.Hardware.Camera.Parameters myParam = mCamera.GetParameters();

				//设置大小和方向等参数  
				myParam.SetPreviewSize(1280, 720);
				myParam.SetPictureSize (640, 480);
				myParam.SetRotation (90);
				myParam.Set("iso",100);
				myParam.Set("jpeg-quality",100);
				myParam.Zoom = 14;
				mCamera.SetDisplayOrientation(90);
				mCamera.SetParameters(myParam);
				mCamera.StartPreview();
				//mCamera.SetPreviewCallback (this);
			}
		}  
			


		public void OnPictureTaken (byte[] data, Android.Hardware.Camera camera)
		{
			Bitmap pic = BitmapFactory.DecodeByteArray (data, 0, data.Length);
			//切割图片
			pics = Bitmap.CreateBitmap (pic, 80, 150, 400, 260);
			//pics = BitmapFactory.DecodeFile("/mnt/ext_sdcard/fingerprints/haha.jpeg");

			//FileStream bos = new FileStream("/mnt/ext_sdcard/fingerprints/haha.jpeg",FileMode.Create);
			//pics.Compress (Bitmap.CompressFormat.Jpeg, 100, bos);
			//bos.Close ();
			//Console.WriteLine (pics.ToString());
			iv.SetImageBitmap (pics);
			pic = null;
			if (auto) {
				int length = pics.Width;
				byte ridge = 0;
				byte valley = 0;
				int count = 0;
				int[] temp = new int[length];
				pics.GetPixels (temp, 0, length, 0, 130, 400, 1);
				byte[] pixelLine = new byte[length];

				for (int i = 0; i < length; i++) 
					pixelLine [i] = SourceAFIS.General.GdiIO.ARGBtoGray8 (temp [i]);
				for (int i = 1; i < length - 1; i++) {
					if ((pixelLine [i] > pixelLine [i - 1]) && (pixelLine [i] > pixelLine [i + 1])) {
						ridge = pixelLine [i];

						if (ridge - valley > 30)
							count++;
					} else if ((pixelLine [i] < pixelLine [i - 1]) && (pixelLine [i] < pixelLine [i + 1])) {
						valley = pixelLine [i];

						if (ridge - valley > 30)
							count++;
					}
				}
				Console.WriteLine (count.ToString());
				if (count > 15) {
					hdler.RemoveCallbacks (this);
					StartCore ();
					hdler.PostDelayed (this, DELAY_MILLIS);
				}
			} else {
				StartCore ();
			}
			//bos = null;
			GC.Collect ();
			camera.StartPreview ();
		}

		/*public void OnPreviewFrame (byte[] data, Android.Hardware.Camera camera)
		{
			if (detected) {
				Android.Hardware.Camera.Size size = mCamera.GetParameters ().PreviewSize;
				YuvImage image = new YuvImage (data, ImageFormatType.Nv21, size.Width, size.Height, null);
				MemoryStream st = new MemoryStream ();
				image.CompressToJpeg (new Rect (500, 0, 505, 720), 100, st);
				Bitmap bmp = BitmapFactory.DecodeByteArray (st.ToArray (), 0, (int)st.Length);
				st.Close ();
				st = null;
				//iv.SetImageBitmap (bmp);
				//Console.WriteLine(bmp.Width);
				//Console.WriteLine (bmp.Height);

				int length = bmp.Height;
				byte ridge = 0;
				byte valley = 0;
				int count = 0;
				int[] temp = new int[length];
				bmp.GetPixels (temp, 0, 1, 0, 0, 1, bmp.Height);
				byte[] pixelLine = new byte[length];

				for (int i = 0; i < length; i++)
					pixelLine [i] = SourceAFIS.General.GdiIO.ARGBtoGray8 (temp [i]);
				for (int i = 1; i < length - 1; i++) {
					if ((pixelLine [i] > pixelLine [i - 1]) && (pixelLine [i] > pixelLine [i + 1])) {
						ridge = pixelLine [i];

						if (ridge - valley > 30)
							count++;
					} else if ((pixelLine [i] < pixelLine [i - 1]) && (pixelLine [i] < pixelLine [i + 1])) {
						valley = pixelLine [i];

						if (ridge - valley > 30)
							count++;
					}
				}
				Console.WriteLine (count.ToString ());
				detected = false;
				if (count > 20) {
					hdler.RemoveCallbacks (this);
					auto = false;
					mCamera.TakePicture (null, null, this);

					hdler.PostDelayed (this, DELAY_MILLIS);
				}
			}
		} */

		// Take fingerprint image file and create Person object from the image
		static MyPerson Enroll(Bitmap image, string name)
		{
			Console.WriteLine("Enrolling {0}...", name);

			// Initialize empty fingerprint object and set properties
			MyFingerprint fp = new MyFingerprint();
			fp.Filename = name;
			// Load image from the file
			Console.WriteLine(" Loading image from Bitmap...");

			//BitmapImage image = new BitmapImage(new Uri(filename, UriKind.RelativeOrAbsolute));
			//FileInputStream fstream = new FileInputStream (filename);
			//Bitmap image = BitmapFactory.DecodeFile (filename);
			fp.AsBitmap = image;
			// Above update of fp.AsBitmapSource initialized also raw image in fp.Image
			// Check raw image dimensions, Y axis is first, X axis is second
			Console.WriteLine(" Image size = {0} x {1} (width x height)", fp.Image.GetLength(1), fp.Image.GetLength(0));

			// Initialize empty person object and set its properties
			MyPerson person = new MyPerson();
			person.Name = name;
			// Add fingerprint to the person
			person.Fingerprints.Add(fp);

			// Execute extraction in order to initialize fp.Template
			Console.WriteLine(" Extracting template...");
			Afis.Extract(person);
			// Check template size
			Console.WriteLine(" Template size = {0} bytes", fp.Template.Length);

			return person;
		}

		void StartCore()
		{
			if (isIdentify && System.IO.File.Exists (ImagePath + "database.dat")) {  //指纹识别
				// Enroll visitor with unknown identity
				//MyPerson probe = Enroll(ImagePath + "t1.BMP", "##Visitor##");
				tv.Text = "开始识别。。。";
				MyPerson probe = Enroll (pics, "##Visitor##");
				Console.WriteLine ("Identifying {0} in database of {1} persons...", probe.Name, database.Count);
				MyPerson match = Afis.Identify (probe, database).FirstOrDefault () as MyPerson;
				// Null result means that there is no candidate with similarity score above threshold
				if (match == null) {
					Console.WriteLine ("No matching person found.");
					result.Text = "无匹配指纹！";
				} else {
					// Print out any non-null result
					Console.WriteLine ("Probe {0} matches registered person {1}", probe.Name, match.Name);


					// Compute similarity score
					float score = Afis.Verify (probe, match);
					Console.WriteLine ("Similarity score between {0} and {1} = {2:F3}", probe.Name, match.Name, score);
					tv.Text = "识别完成。。。";
					result.Text = "身份：" + match.Name + "\n匹配分数：" + score;
				}
			} else if (xuehao != null) { //指纹录入
				//database.Add(Enroll(ImagePath + "r1.BMP", xuehao));
				database.Add (Enroll (pics, xuehao));

				// Save the database to disk and load it back, just to try out the serialization
				Console.WriteLine ("Saving database...");
				BinaryFormatter formatter = new BinaryFormatter ();
				using (Stream stream = File.Open (ImagePath + "database.dat", FileMode.OpenOrCreate))
					formatter.Serialize (stream, database);

				isIdentify = true;
				tv.Text = "指纹已录入，开始识别。。。";
			} else {
				//Toast.MakeText(Application.Context,"请先录入指纹",ToastLength.Short).Show();
			}
		}

		int i = 0;
		public void Run ()
		{
			frameView.Text = i.ToString();
			i++;
			auto = true;
			mCamera.TakePicture(null,null,this);
			//detected = true;
			hdler.PostDelayed (this, DELAY_MILLIS);
		}

	}

	// Inherit from Fingerprint in order to add Filename field
	[Serializable]
	class MyFingerprint : Fingerprint
	{
		public string Filename;
	}

	// Inherit from Person in order to add Name field
	[Serializable]
	class MyPerson : Person
	{
		public string Name;
	}
		


}

