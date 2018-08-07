using System;
using Xamarin.Forms;

namespace Camera2Sample
{
	public partial class MainPage
	{
		public static event EventHandler<ImageSource> PhotoCapturedEvent;

		public MainPage()
		{
			InitializeComponent();

			PhotoCapturedEvent += (sender, source) =>
			{
				PhotoCaptured.Source = source;
			};
		}

		public static void OnPhotoCaptured(ImageSource src)
		{
			PhotoCapturedEvent?.Invoke(new MainPage(), src);
		}
	}
}
