using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views.Animations;

using JKChat.Android.Helpers;

namespace JKChat.Android.Controls.Toolbar {
	public class BackDrawable : Drawable {
		private const float DefaultAnimationTime = 300.0f;

		private readonly Paint paint = new Paint(PaintFlags.AntiAlias) {
			StrokeWidth = 2.0f.DpToPxF()
		};
		private readonly DecelerateInterpolator interpolator = new DecelerateInterpolator();

		private bool reverseAngle;
		private long lastFrameTime;
		private float finalRotation;
		private float currentRotation;
		private long currentAnimationTime;

		public bool AlwaysClose { get; set; }

		private Color color = Color.White;
		public Color Color {
			get => color;
			set { color = value; InvalidateSelf(); }
		}

		private Color rotatedColor = Color.Gray;
		public Color RotatedColor {
			get => rotatedColor;
			set { rotatedColor = value; InvalidateSelf(); }
		}

		private int arrowRotation;
		public int ArrowRotation {
			get => arrowRotation;
			set { arrowRotation = value; InvalidateSelf(); }
		}

		public float StrokeWidth {
			get => paint.StrokeWidth.PxToDp();
			set => paint.StrokeWidth = value.DpToPxF();
		}

		public float AnimationTime { get; set; } = DefaultAnimationTime;

		public bool Rotated { get; set; } = true;

		public BackDrawable() {}

		public void SetRotation(float rotation, bool animated) {
			lastFrameTime = 0;
			if (currentRotation == 1) {
				reverseAngle = true;
			} else if (currentRotation == 0) {
				reverseAngle = false;
			}
			lastFrameTime = 0;
			if (animated) {
				if (currentRotation < rotation) {
					currentAnimationTime = (long)(currentRotation * AnimationTime);
				} else {
					currentAnimationTime = (long)((1.0f - currentRotation) * AnimationTime);
				}
				lastFrameTime = Java.Lang.JavaSystem.CurrentTimeMillis();
				finalRotation = rotation;
			} else {
				finalRotation = currentRotation = rotation;
			}
			InvalidateSelf();
		}

		public override void Draw(Canvas canvas) {
			if (currentRotation != finalRotation) {
				if (lastFrameTime != 0) {
					long dt = Java.Lang.JavaSystem.CurrentTimeMillis() - lastFrameTime;

					currentAnimationTime += dt;
					if (currentAnimationTime >= AnimationTime) {
						currentRotation = finalRotation;
					} else {
						if (currentRotation < finalRotation) {
							currentRotation = interpolator.GetInterpolation(currentAnimationTime / AnimationTime) * finalRotation;
						} else {
							currentRotation = 1.0f - interpolator.GetInterpolation(currentAnimationTime / AnimationTime);
						}
					}
				}
				lastFrameTime = Java.Lang.JavaSystem.CurrentTimeMillis();
				InvalidateSelf();
			}

			int rD = Rotated ? (int)((rotatedColor.R - color.R) * currentRotation) : 0;
			int rG = Rotated ? (int)((rotatedColor.G - color.G) * currentRotation) : 0;
			int rB = Rotated ? (int)((rotatedColor.B - color.B) * currentRotation) : 0;
			paint.Color = Color.Rgb(color.R + rD, color.G + rG, color.B + rB);

			canvas.Save();
			canvas.Translate(IntrinsicWidth / 2, IntrinsicHeight / 2);
			if (arrowRotation != 0) {
				canvas.Rotate(arrowRotation);
			}
			float rotation = currentRotation;
			if (!AlwaysClose) {
				canvas.Rotate(currentRotation * (reverseAngle ? -225 : 135));
			} else {
				canvas.Rotate(135 + currentRotation * (reverseAngle ? -180 : 180));
				rotation = 1.0f;
			}
			canvas.DrawLine(-7.0f.DpToPx() - 2.0f.DpToPx() * rotation, 0, 8.0f.DpToPx() + 1.0f.DpToPx() * rotation, 0, paint);
			float startYDiff = -0.5f.DpToPx();
			float endYDiff = 7.0f.DpToPx() + 2.0f.DpToPx() * rotation;
			float startXDiff = -7.0f.DpToPx() + 7.0f.DpToPx() * rotation;
			float endXDiff = 0.5f.DpToPx() - 0.5f.DpToPx() * rotation;
			canvas.DrawLine(startXDiff, -startYDiff, endXDiff, -endYDiff, paint);
			canvas.DrawLine(startXDiff, startYDiff, endXDiff, endYDiff, paint);
			canvas.Restore();
		}

		public override void SetAlpha(int alpha) {}

		public override void SetColorFilter(ColorFilter cf) {}

		public override int Opacity => (int)Format.Transparent;

		public override int IntrinsicWidth => 24.0f.DpToPx();

		public override int IntrinsicHeight => 24.0f.DpToPx();
	}
}