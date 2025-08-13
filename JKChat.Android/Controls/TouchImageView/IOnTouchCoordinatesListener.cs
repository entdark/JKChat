using Android.Graphics;
using Android.Views;

namespace JKChat.Android.Controls.TouchImageView;

public interface IOnTouchCoordinatesListener {
	void OnTouchCoordinate(View view, MotionEvent ev, PointF bitmapPoint);
}