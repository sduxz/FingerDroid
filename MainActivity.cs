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
5.未考虑鲜见bug，例如文件读写前不检查是否已经存在文件，可能会导致出错和覆盖原文件
6.每节课每天只能考勤一次
7.输入的时间只能为24小时格式的XXXX;

*******************************************************************************************/
namespace FingerDroid
{
	[Activity(Label = "FingerDroid", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity,Android.Hardware.Camera.IPictureCallback , Java.Lang.IRunnable ,TextToSpeech.IOnInitListener,View.IOnTouchListener//, Android.Hardware.Camera.IPreviewCallback
	{
		int DELAY_MILLIS = 1000;

		CameraView mPreview;
		public Android.Hardware.Camera mCamera;
		Handler hdler = new Handler ();

		ImageView iv = null;
		Boolean isIdentify = true;
		Boolean auto = true;
		Boolean todayisbuld = false;
		//Boolean detected = false;
		string xuehao = null;
		string nowLesson = "database";
		Bitmap pics;
		TextView tv = null;
		TextView result = null;
		TextView frameView = null;
		List<MyPerson> database = null;
		List<MyLesson> lessons = null;

		TextToSpeech tts = null;
	
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

			FrameLayout.LayoutParams tparams = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent,
													ViewGroup.LayoutParams.WrapContent);//定义显示组件参数 

			View mainView = View.Inflate (this, Resource.Layout.Main, null);
			Button btn = mainView.FindViewById<Button> (Resource.Id.takepicb);
			Button enrollbtn = mainView.FindViewById<Button> (Resource.Id.enrollbtn);
			Button kechenbtn = mainView.FindViewById<Button> (Resource.Id.button1);

			Spinner spinner = mainView.FindViewById<Spinner> (Resource.Id.spinner1);

			tv = mainView.FindViewById<TextView> (Resource.Id.statustext);
			tv.Text = "开始指纹识别。。。";
			result = mainView.FindViewById<TextView> (Resource.Id.resulttext);

			Switch autoSw = mainView.FindViewById<Switch> (Resource.Id.switch1);
			iv = mainView.FindViewById<ImageView> (Resource.Id.imageView1);
			frameView = mainView.FindViewById<TextView> (Resource.Id.textView1);

			tts = new TextToSpeech (this, this);

			FrameLayout fl = new FrameLayout(this);

			fl.AddView(mPreview);
			fl.AddView (mainView);

			//保持屏幕常亮
			Window.SetFlags (WindowManagerFlags.KeepScreenOn,WindowManagerFlags.KeepScreenOn);
			SetContentView(fl, tparams);

			// Initialize SourceAFIS
			Afis = new AfisEngine();
			// Look up the probe using Threshold = 10
			Afis.Threshold = 25;
			// Enroll some people
			database = new List<MyPerson>();
			lessons = new List<MyLesson> ();

			if (System.IO.File.Exists (ImagePath + "lessons")) {
				BinaryFormatter formatter = new BinaryFormatter ();
				using (FileStream stream = File.OpenRead (ImagePath + "lessons"))
					lessons = (List<MyLesson>)formatter.Deserialize (stream); 

			} else {
				tv.Text = "无课程，请添加课程。。。";
			}

			List<string> lessonname = new List<string> ();
			foreach (MyLesson ml in lessons)
				lessonname.Add (ml.name);
			ArrayAdapter<string> adapter = new ArrayAdapter<string> (Application.Context, Android.Resource.Layout.SimpleSpinnerItem, lessonname);
			spinner.Adapter = adapter;
			spinner.ItemSelected += delegate(object sender, AdapterView.ItemSelectedEventArgs e) {
				Spinner s = (Spinner) sender;
				nowLesson = s.GetItemAtPosition(e.Position).ToString();

				//判断数据库是否存在
				if (System.IO.File.Exists (ImagePath + nowLesson + ".dat")) {

					tv.Text = "已载入" + nowLesson + "数据库,开始识别。。。";
					BinaryFormatter formatterr = new BinaryFormatter ();
					Console.WriteLine ("Reloading database...");
					using (FileStream stream = File.OpenRead (ImagePath + nowLesson + ".dat"))
						database = (List<MyPerson>)formatterr.Deserialize (stream);
				} else {
					tv.Text = "数据库" + nowLesson + "中无数据，请录入指纹。。。";
					database = new List<MyPerson>();
				}

				//判断本课程今天的考勤是否已经建立
				todayisbuld =false;
				foreach(MyLesson ml in lessons)
				{
					if(ml.name == nowLesson){
						foreach (Attendance at in ml) 
						{
							//获取当前时间
							DateTime dt = DateTime.Now;
							//找到今天的考勤在数据库中的位置
							if ((at.date.Year == dt.Year) && (at.date.Month == dt.Month) && (at.date.Day == dt.Day))
								todayisbuld = true;
						}
					}
				}

				//如果没建立，就新建一个
				if(!todayisbuld)
				{
					foreach(MyLesson ml in lessons)
					{
						if(ml.name == nowLesson){
							//校准上课时间的日期到今天
							DateTime dt = DateTime.Now;
							ml.time.AddYears(dt.Year);
							ml.time.AddMonths(dt.Month);
							ml.time.AddDays(dt.Day);
						}
					}
				}
					
						
			};

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

			btn.SetOnTouchListener (this);
			enrollbtn.SetOnTouchListener (this);
			kechenbtn.SetOnTouchListener (this);

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

			kechenbtn.Click += delegate {
				View base2 = View.Inflate(this,Resource.Layout.kecheng,null);
				Button deletelesson = base2.FindViewById<Button>(Resource.Id.button3);
				Button backbtn = base2.FindViewById<Button>(Resource.Id.button1);
				Button addbtn = base2.FindViewById<Button>(Resource.Id.button2);

				deletelesson.Click += delegate {

				};

				addbtn.Click += delegate {
					EditText editT = new EditText(this);
					EditText edittime = new EditText(this);
					LinearLayout ll = new LinearLayout(this);
					ll.Orientation = Orientation.Vertical;
					ll.AddView(editT);
					ll.AddView(edittime);
					AlertDialog.Builder  alertDialog = new AlertDialog.Builder(this);
					alertDialog.SetTitle("请输入课程名和时间：");
					alertDialog.SetView(ll);
					alertDialog.SetPositiveButton("确认",delegate {
						//lessons.Add(editT.Text);
						MyLesson myl = new MyLesson(editT.Text,Convert.ToDateTime(edittime.Text.Insert(2,":")));
						lessons.Add(myl);
						adapter.Add(editT.Text);
						adapter.NotifyDataSetChanged();
						Console.WriteLine ("添加课程...");
						BinaryFormatter formatters = new BinaryFormatter ();
						using (Stream stream = File.Open (ImagePath + "lessons", FileMode.OpenOrCreate))
					    formatters.Serialize (stream, lessons);
						tv.Text = "课程名为"+ editT.Text + "，请录入指纹。。。";
					});
					alertDialog.SetNegativeButton("取消",delegate {

					});
					alertDialog.Show();

				};

				//返回
				backbtn.Click += delegate {
					SetContentView(fl);
				};

				//如果开了自动，则关闭
				if(autoSw.Checked)
				{
					hdler.RemoveCallbacks(this);
					autoSw.Checked = false;
				}
				SetContentView(base2);

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

		public bool OnTouch (View v, MotionEvent e)
		{
			if(e.Action.Equals(MotionEventActions.Down))
				v.SetBackgroundResource(Resource.Drawable.barcode_local_pic_bg_pressed);
			if(e.Action.Equals(MotionEventActions.Up))
				v.SetBackgroundResource(Resource.Drawable.barcode_local_pic_bg_normal);
			return false;
		}

		public void OnInit (OperationResult status)
		{
			/*如果装载TTS引擎成功*/  
			if(status.Equals(OperationResult.Success)){  
				/*设置使用某种语言朗读*/  
				tts.SetLanguage(Java.Util.Locale.Chinese);    
			}else{/*没有TTS引擎*/  
				tv.Text = "暂不支持TTS";
			}  
		}

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
			tts.Speak ("叮咚", QueueMode.Flush, null);
			if (isIdentify && System.IO.File.Exists (ImagePath + nowLesson + ".dat")) {  //指纹识别
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
					tts.Speak ("不认识你", QueueMode.Flush, null);
				} else {
					// Print out any non-null result
					Console.WriteLine ("Probe {0} matches registered person {1}", probe.Name, match.Name);


					// Compute similarity score
					float score = Afis.Verify (probe, match);
					Console.WriteLine ("Similarity score between {0} and {1} = {2:F3}", probe.Name, match.Name, score);
					tv.Text = "识别完成。。。";
					tts.Speak (match.Name, QueueMode.Flush, null);
					result.Text = "身份：" + match.Name + "\n匹配分数：" + score;
				}
			} else if (xuehao != null) { //指纹录入
				//database.Add(Enroll(ImagePath + "r1.BMP", xuehao));
				database.Add (Enroll (pics, xuehao));
				xuehao = null;

				// Save the database to disk and load it back, just to try out the serialization
				Console.WriteLine ("Saving database...");
				BinaryFormatter formatter = new BinaryFormatter ();
				using (Stream stream = File.Open (ImagePath + nowLesson + ".dat", FileMode.OpenOrCreate))
					formatter.Serialize (stream, database);

				isIdentify = true;
				tv.Text = "指纹已录入，开始识别。。。";
			} else {
				//Toast.MakeText(Application.Context,"请先录入指纹",ToastLength.Short).Show();
			}
		}

		public void judgeTime(string name)
		{
			foreach(MyLesson ml in lessons)
			{
				//找到当前的课程在数据库中的位置
				if (ml.name.Equals (nowLesson)) {
					foreach (Attendance at in ml) 
					{
						//获取当前时间
						DateTime dt = DateTime.Now;
						//找到今天的考勤在数据库中的位置
						if ((at.date.Year == dt.Year) && (at.date.Month == dt.Month) && (at.date.Day == dt.Day)) {
							if (DateTime.Compare (dt, ml.time) > 0)
								at.late.Add (name);
							else
								at.attend.Add (name);
						}
					}
				}
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

	[Serializable]
	class MyLesson
	{
		public string name;
		public DateTime time;
		public List<Attendance> attendance;

		public MyLesson(string _name,DateTime _time)
		{
			name = _name;
			time = _time;
		}
	}

	[Serializable]
	class Attendance
	{
		public DateTime date;
		public List<string> attend;
		public List<string> late;
		public List<string> absent;
	}


}

