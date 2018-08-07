using Android.Content;
using Android.Util;
using Android.Views;
using Java.Lang;

namespace Camera2Sample.Droid.Renderers.Camera
{
	public class CameraTextureView : TextureView
	{
		private int _mRatioWidth;
		private int _mRatioHeight;

		public CameraTextureView(Context context) : base(context, null)
		{
		}

		public CameraTextureView(Context context, IAttributeSet attrs) :
			base(context, attrs, 0)
		{
		}

		public CameraTextureView(Context context, IAttributeSet attrs, int defStyle) :
			base(context, attrs, defStyle)
		{
		}

		/// <summary>
		/// Sets the aspect ratio for this view. The size of the view will be measured based on the ratio
		/// calculated from the parameters. Note that the actual sizes of parameters don't matter, that
		/// is, calling setAspectRatio(2, 3) and setAspectRatio(4, 6) make the same result.
		/// </summary>
		/// <param name="width">Relative horizontal size.</param>
		/// <param name="height">Relative vertical size.</param>
		public void SetAspectRatio(int width, int height)
		{
			if (width < 0 || height < 0)
				throw new IllegalArgumentException("Size cannot be negative.");

			if (_mRatioWidth == width && _mRatioHeight == height)
				return;

			_mRatioWidth = width;
			_mRatioHeight = height;
			RequestLayout();
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

			var width = MeasureSpec.GetSize(widthMeasureSpec);
			var height = MeasureSpec.GetSize(heightMeasureSpec);

			if (_mRatioWidth == 0 || _mRatioHeight == 0)
			{
				SetMeasuredDimension(width, height);
			}
			else
			{
				if (width < height * _mRatioWidth / _mRatioHeight)
				{
					SetMeasuredDimension(width, width * _mRatioHeight / _mRatioWidth);
				}
				else
				{
					SetMeasuredDimension(height * _mRatioWidth / _mRatioHeight, height);
				}
			}
		}
	}
}
