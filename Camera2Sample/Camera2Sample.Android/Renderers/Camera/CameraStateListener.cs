using Android.Hardware.Camera2;

namespace Camera2Sample.Droid.Renderers.Camera
{
	public class CameraStateListener : CameraDevice.StateCallback
	{
		public CameraDroid Camera;

		public override void OnOpened(CameraDevice camera)
		{
			if (Camera == null) return;

			Camera.CameraDevice = camera;
			Camera.StartPreview();
			Camera.OpeningCamera = false;
		}

		public override void OnDisconnected(CameraDevice camera)
		{
			if (Camera == null) return;

			camera.Close();
			Camera.CameraDevice = null;
			Camera.OpeningCamera = false;
		}

		public override void OnError(CameraDevice camera, CameraError error)
		{
			camera.Close();

			if (Camera == null) return;

			Camera.CameraDevice = null;
			Camera.OpeningCamera = false;
		}
	}
}
