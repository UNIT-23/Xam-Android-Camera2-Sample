using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.IO;
using Xamarin.Forms;
using Size = Android.Util.Size;

namespace Camera2Sample.Droid.Renderers.Camera
{
	public class CameraDroid : FrameLayout, TextureView.ISurfaceTextureListener
	{
		private static readonly SparseIntArray Orientations = new SparseIntArray();

		public event EventHandler<ImageSource> Photo;

		public bool OpeningCamera { private get; set; }

		public CameraDevice CameraDevice;

		private readonly CameraStateListener _mStateListener;
		private CaptureRequest.Builder _previewBuilder;
		private CameraCaptureSession _previewSession;
		private SurfaceTexture _viewSurface;
		private readonly TextureView _cameraTexture;
		private Size _previewSize;
		private readonly Context _context;
		private CameraManager _manager;

		public CameraDroid(Context context) : base(context)
		{
			_context = context;

			var inflater = LayoutInflater.FromContext(context);

			if (inflater == null) return;
			var view = inflater.Inflate(Resource.Layout.CameraLayout, this);

			_cameraTexture = view.FindViewById<TextureView>(Resource.Id.cameraTexture);

			_cameraTexture.Click += (sender, args) => { TakePhoto(); };

			_cameraTexture.SurfaceTextureListener = this;

			_mStateListener = new CameraStateListener { Camera = this };

			Orientations.Append((int)SurfaceOrientation.Rotation0, 0);
			Orientations.Append((int)SurfaceOrientation.Rotation90, 90);
			Orientations.Append((int)SurfaceOrientation.Rotation180, 180);
			Orientations.Append((int)SurfaceOrientation.Rotation270, 270);
		}

		public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
		{
			_viewSurface = surface;

			ConfigureTransform(width, height);
			StartPreview();
		}

		public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
		{
			return true;
		}

		public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
		{
		}

		public void OnSurfaceTextureUpdated(SurfaceTexture surface)
		{
		}

		public void OpenCamera()
		{
			if (_context == null || OpeningCamera)
			{
				return;
			}

			OpeningCamera = true;

			_manager = (CameraManager)_context.GetSystemService(Context.CameraService);

			var cameraId = _manager.GetCameraIdList()[1];

			var characteristics = _manager.GetCameraCharacteristics(cameraId);
			var map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);

			_previewSize = map.GetOutputSizes(Class.FromType(typeof(SurfaceTexture)))[0];

			_manager.OpenCamera(cameraId, _mStateListener, null);

		}


		private void TakePhoto()
		{
			if (_context == null || CameraDevice == null) return;

			var characteristics = _manager.GetCameraCharacteristics(CameraDevice.Id);
			Size[] jpegSizes = null;
			if (characteristics != null)
			{
				jpegSizes = ((StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap)).GetOutputSizes((int)ImageFormatType.Jpeg);
			}
			var width = 480;
			var height = 640;

			if (jpegSizes != null && jpegSizes.Length > 0)
			{
				width = jpegSizes[0].Width;
				height = jpegSizes[0].Height;
			}

			var reader = ImageReader.NewInstance(width, height, ImageFormatType.Jpeg, 1);
			var outputSurfaces = new List<Surface>(2) { reader.Surface, new Surface(_viewSurface) };

			var captureBuilder = CameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
			captureBuilder.AddTarget(reader.Surface);
			captureBuilder.Set(CaptureRequest.ControlMode, new Integer((int)ControlMode.Auto));

			var windowManager = _context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
			var rotation = windowManager.DefaultDisplay.Rotation;
			captureBuilder.Set(CaptureRequest.JpegOrientation, new Integer(Orientations.Get((int)rotation)));

			var readerListener = new ImageAvailableListener();

			readerListener.Photo += (sender, buffer) =>
			{
				Photo?.Invoke(this, ImageSource.FromStream(() => new MemoryStream(buffer)));
			};

			var thread = new HandlerThread("CameraPicture");
			thread.Start();
			var backgroundHandler = new Handler(thread.Looper);
			reader.SetOnImageAvailableListener(readerListener, backgroundHandler);

			var captureListener = new CameraCaptureListener();

			captureListener.PhotoComplete += (sender, e) =>
			{
				StartPreview();
			};

			CameraDevice.CreateCaptureSession(outputSurfaces, new CameraCaptureStateListener
			{
				OnConfiguredAction = session =>
				{
					try
					{
						_previewSession = session;
						session.Capture(captureBuilder.Build(), captureListener, backgroundHandler);
					}
					catch (CameraAccessException ex)
					{
						Log.WriteLine(LogPriority.Info, "Capture Session error: ", ex.ToString());
					}
				}
			}, backgroundHandler);
		}


		public void StartPreview()
		{
			if (CameraDevice == null || !_cameraTexture.IsAvailable || _previewSize == null) return;

			var texture = _cameraTexture.SurfaceTexture;

			texture.SetDefaultBufferSize(_previewSize.Width, _previewSize.Height);
			var surface = new Surface(texture);

			_previewBuilder = CameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
			_previewBuilder.AddTarget(surface);

			CameraDevice.CreateCaptureSession(new List<Surface> { surface },
				new CameraCaptureStateListener
				{
					OnConfigureFailedAction = session =>
					{
					},
					OnConfiguredAction = session =>
					{
						_previewSession = session;
						UpdatePreview();
					}
				},
				null);


		}

		private void ConfigureTransform(int viewWidth, int viewHeight)
		{
			if (_viewSurface == null || _previewSize == null || _context == null) return;

			var windowManager = _context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

			var rotation = windowManager.DefaultDisplay.Rotation;
			var matrix = new Matrix();
			var viewRect = new RectF(0, 0, viewWidth, viewHeight);
			var bufferRect = new RectF(0, 0, _previewSize.Width, _previewSize.Height);

			var centerX = viewRect.CenterX();
			var centerY = viewRect.CenterY();

			if (rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270)
			{
				bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
				matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);

				matrix.PostRotate(90 * ((int)rotation - 2), centerX, centerY);
			}

			_cameraTexture.SetTransform(matrix);
		}

		private void UpdatePreview()
		{
			if (CameraDevice == null || _previewSession == null) return;

			_previewBuilder.Set(CaptureRequest.ControlMode, new Integer((int)ControlMode.Auto));
			var thread = new HandlerThread("CameraPreview");
			thread.Start();
			var backgroundHandler = new Handler(thread.Looper);

			_previewSession.SetRepeatingRequest(_previewBuilder.Build(), null, backgroundHandler);
		}
	}
}
