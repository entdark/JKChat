using System;
using System.Numerics;

using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;

using AndroidX.Core.Graphics;

using JKChat.Android.Helpers;
using JKChat.Android.ValueConverters;
using JKChat.Core;
using JKChat.Core.Helpers;
using JKChat.Core.Models;

namespace JKChat.Android.Controls;

[Register("JKChat.Android.Controls.MinimapView")]
public class MinimapView : FrameLayout {
	private readonly Handler handler = new Handler(Looper.MainLooper);
	private MinimapTouchImageView minimapImageView;
	private MinimapDrawingView minimapDrawingView;
		
	private EntityData []entities;
	public EntityData []Entities {
		get => entities;
		set { entities = value; handler.RemoveCallbacksAndMessages(null); handler.Post(minimapDrawingView.Invalidate); }
	}

	private MapData mapData;
	public MapData MapData {
		get => mapData;
		set {
			if (mapData == value)
				return;

			mapData = value;

			if (mapData != null) {
				try {
					Drawable drawable;
					if (MapData.Assembly != null) {
						using var stream = MapData.Assembly.GetManifestResourceStream(MapData.Path);
						drawable = Drawable.CreateFromStream(stream, null);
					} else {
						drawable = Drawable.CreateFromPath(MapData.Path);
					}
					minimapImageView.SetImageDrawable(drawable);
					minimapImageView.ResetZoom();
					drawable?.Dispose();
				} catch (Exception exception) {
					System.Diagnostics.Debug.WriteLine(exception);
				}
			} else {
				minimapImageView.SetImageDrawable(null);
			}
		}
	}

	public MinimapView(Context context) : base(context) {
		Initialize();
	}

	public MinimapView(Context context, IAttributeSet attrs) : base(context, attrs) {
		Initialize();
	}

	public MinimapView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) {
		Initialize();
	}

	public MinimapView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes) {
		Initialize();
	}

	protected MinimapView(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
		Initialize();
	}

	private void Initialize() {
		minimapImageView = new(Context) {
			LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
			MinZoom = 1.0f,
			MaxZoom = 5.0f,
			SuperMinMultiplier = 0.002f,
			SuperMaxMultiplier = 10.0f,
			DoubleTapScale = 2.0f
		};
		minimapImageView.SetScaleType(ImageView.ScaleType.FitStart);
		minimapImageView.ImageMatrixChanged += ImageMatrixChanged;
		minimapImageView.SetAdjustViewBounds(false);
		AddView(minimapImageView);
		minimapDrawingView = new(Context, this) {
			LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
		};
		minimapDrawingView.SetLayerType(LayerType.Hardware, null);
		AddView(minimapDrawingView);
	}

	private void ImageMatrixChanged(object sender, EventArgs ev) {
		minimapDrawingView.Invalidate();
	}

	private Java.Lang.Thread t;
	protected override void OnAttachedToWindow() {
		base.OnAttachedToWindow();
		if (AppSettings.MinimapOptions.HasFlag(MinimapOptions.HighPerformance)) {
			try {
				t = new(() => {
					while (t is { IsInterrupted: false });
				});
				t.Start();
			} catch (Exception exception) {
				System.Diagnostics.Debug.WriteLine(exception);
			}
		}
	}

	protected override void OnDetachedFromWindow() {
		try {
			t?.Interrupt();
			t?.Dispose();
			t = null;
		} catch (Exception exception) {
			System.Diagnostics.Debug.WriteLine(exception);
		}
		base.OnDetachedFromWindow();
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			minimapImageView.ImageMatrixChanged -= ImageMatrixChanged;
		}
		base.Dispose(disposing);
	}

	private class MinimapTouchImageView(Context context) : Com.Vlbor.TouchImageView.TouchImageView(context) {
		protected override float ImageHeight => viewHeight * CurrentZoom;
		public override float ScaledImageHeight => base.ImageHeight;

		protected override void FitImageToView() {
			if (touchScaleType != ScaleType.FitStart || IsZoomed || imageRenderedAtLeastOnce) {
				base.FitImageToView();
				return;
			}
			orientationJustChanged = false;
			var drawable = Drawable;

			if (drawable == null || drawable.IntrinsicWidth == 0 || drawable.IntrinsicHeight == 0)
				return;

			if (touchMatrix == null || prevMatrix == null)
				return;

			if (userSpecifiedMinScale == AutomaticMinZoom) {
				MinZoom = AutomaticMinZoom;
				if (CurrentZoom < minScale)
					CurrentZoom = minScale;
			}

			int drawableWidth = GetDrawableWidth(drawable),
				drawableHeight = GetDrawableHeight(drawable);

			float scaleX = (float)viewWidth / drawableWidth,
				scaleY = (float)viewHeight / drawableHeight;
			scaleX = scaleY = Math.Min(scaleX, scaleY);

			float redundantXSpace = viewWidth - (scaleX * drawableWidth),
				redundantYSpace = viewHeight - (scaleY * drawableHeight);

			matchViewWidth = viewWidth - redundantXSpace;
			matchViewHeight = viewHeight - redundantYSpace;
			
			if (IsRotateImageToFitScreen && OrientationMismatch(drawable)) {
				touchMatrix.SetRotate(90.0f);
				touchMatrix.PostTranslate(drawableWidth, 0.0f);
				touchMatrix.PostScale(scaleX, scaleY);
			} else {
				touchMatrix.SetScale(scaleX, scaleY);
			}
			touchMatrix.PostTranslate(redundantXSpace * 0.5f, 0.0f);
			CurrentZoom = 1.0f;
			
			FixTrans();
			ApplyTouchMatrix();
		}
	}

	private class MinimapDrawingView(Context context, MinimapView minimapView) : View(context) {
		private const float viewTriAngle = 30.0f;
		private static readonly float flagHeight = 10.0f.DpToPxF();
		private static readonly float flagWidth = 7.0f.DpToPxF();
		private static readonly float flagLineWidth = 2.0f.DpToPxF();
		private static readonly float viewTriMedian = 30.0f.DpToPxF();
		private static readonly float pointSize = 2.0f.DpToPxF();
		private static readonly float textSize = 12.0f.SpToPxF();
		private static readonly float vehRadius = pointSize*2.0f;
		private static readonly float shotLineWidth = 2.0f.DpToPxF();
		private static readonly float projectilePointSize = 4.0f.DpToPxF();
		private static readonly float projectileLineWidth = 2.0f.DpToPxF();
		private static readonly float impactStartRadius = 2.0f.DpToPxF();
		private static readonly float impactEndRadius = 10.0f.DpToPxF();
		private static readonly float impactLineWidth = 2.0f.DpToPxF();
		
		private readonly TextPaint textPaint = new() {
			TextSize = textSize,
			Color = Color.White
		};
		private readonly TextPaint textShadowPaint = new() {
			TextSize = textSize,
			Color = ColourTextValueConverter.ShadowColor
		};
		private readonly Paint pointPaint = new() {
			StrokeCap = Paint.Cap.Round,
			StrokeWidth = pointSize,
			AntiAlias = true
		};
		private readonly Paint trianglePaint = new() {
			AntiAlias = true
		};
		private readonly Paint flagPaint = new() {
			StrokeCap = Paint.Cap.Round,
			StrokeWidth = flagLineWidth,
			AntiAlias = true
		};
		private readonly Paint shotPaint = new() {
			StrokeCap = Paint.Cap.Round,
			StrokeWidth = shotLineWidth,
			AntiAlias = true
		};
		private readonly Paint projectilePaint = new() {
			StrokeCap = Paint.Cap.Round,
			StrokeWidth = projectilePointSize,
			AntiAlias = true
		};
		private readonly Paint impactPaint = new() {
			StrokeCap = Paint.Cap.Round,
			StrokeWidth = impactLineWidth,
			AntiAlias = true
		};

		public override void Draw(Canvas canvas) {
			base.Draw(canvas);

			if (minimapView.Entities == null)
				return;

			lock (minimapView.Entities) {
				var entities = minimapView.Entities;
				var mapData = minimapView.MapData;

				if (mapData == null || minimapView.minimapImageView.Drawable == null || entities.IsNullOrEmpty())
					return;

				float []matrixValues = new float[9];
				minimapView.minimapImageView.ImageMatrix.GetValues(matrixValues);
				float width = (int)minimapView.minimapImageView.ScaledImageWidth,
					height = (int)minimapView.minimapImageView.ScaledImageHeight;
				var posOffset = new Vector3(
					matrixValues[Matrix.MtransX],
					matrixValues[Matrix.MtransY],
					0.0f
				);

				bool isHW = canvas.IsHardwareAccelerated;

				long now = App.Milliseconds;
				Path path;

				foreach (var entity in entities) {
					var pos = entity.Origin.ToViewPosition(mapData.Min,mapData.Max,width,height,true);
					pos += posOffset;
					var pos2 = entity.Origin2.ToViewPosition(mapData.Min,mapData.Max,width,height,true);
					pos2 += posOffset;
					var color = new Color(entity.Color.ToArgb());
					long lifeLeft = entity.Life - now;
					switch (entity.Type) {
						case EntityType.Player:
//player triangle
							canvas.Save();
							canvas.Translate(pos.X, pos.Y);
							canvas.Rotate(-entity.Angles.Y);
							path = new Path();
							path.MoveTo(0.0f, 0.0f);
							path.LineTo(viewTriMedian, (float)Math.Tan(viewTriAngle.ToRadians()) * viewTriMedian);
							path.LineTo(viewTriMedian, (float)Math.Tan(viewTriAngle.ToRadians()) * -viewTriMedian);
							path.Close();
							var gradient = new LinearGradient(0.0f, 0.0f, viewTriMedian, 0.0f, color, new Color(ColorUtils.SetAlphaComponent(color, 0)), Shader.TileMode.Clamp);
							trianglePaint.SetShader(gradient);
							canvas.DrawPath(path, trianglePaint);
							canvas.Restore();
//player point
							pointPaint.Color = color;
							pointPaint.SetStyle(Paint.Style.Stroke);
							canvas.DrawPoint(pos.X, pos.Y, pointPaint);
							gradient.Dispose();
							path.Dispose();
							break;
						case EntityType.Vehicle:
							pointPaint.Color = color;
							pointPaint.SetStyle(Paint.Style.Stroke);
							canvas.DrawCircle(pos.X, pos.Y, vehRadius, pointPaint);
							break;
						case EntityType.Flag:
							canvas.Save();
							canvas.Translate(pos.X, pos.Y);
							path = new Path();
							path.MoveTo(0.0f, 0.0f);
							path.LineTo(0.0f, -flagHeight);
							path.LineTo(flagWidth, -flagHeight*0.75f);
							path.LineTo(0.0f, -flagHeight*0.5f);
							path.Close();
							flagPaint.Color = color;
							flagPaint.SetStyle(Paint.Style.FillAndStroke);
							canvas.DrawPath(path, flagPaint);
							canvas.Restore();
							path.Dispose();
							break;
						case EntityType.Shot:
							const int shotFadeTime = 200;
							if (lifeLeft <= 0) {
								break;
							} else if (lifeLeft <= shotFadeTime) {
								color.A = unchecked((byte)(255*(lifeLeft / (float)shotFadeTime)));
							}
							path = new Path();
							path.MoveTo(pos.X, pos.Y);
							path.LineTo(pos2.X, pos2.Y);
							shotPaint.Color = color;
							shotPaint.SetStyle(Paint.Style.Stroke);
							canvas.DrawPath(path, shotPaint);
							path.Dispose();
							break;
						case EntityType.Projectile:
							if (entity.Origin2 == Vector3.Zero) {
								projectilePaint.StrokeWidth = projectilePointSize;
								projectilePaint.Color = color;
								canvas.DrawPoint(pos.X, pos.Y, projectilePaint);
							} else {
								path = new Path();
								path.MoveTo(pos.X, pos.Y);
								path.LineTo(pos2.X, pos2.Y);
								projectilePaint.StrokeWidth = projectileLineWidth;
								projectilePaint.Color = color;
								projectilePaint.SetStyle(Paint.Style.Stroke);
								canvas.DrawPath(path, projectilePaint);
								path.Dispose();
							}
							break;
						case EntityType.Impact:
							if (lifeLeft <= 0) {
								break;
							} else {
								float dl = (lifeLeft / (float)entity.LifeLength);
								float radius = impactStartRadius + (1.0f - dl) * (impactEndRadius - impactStartRadius);
								color.A = unchecked((byte)(255*dl));
								impactPaint.Color = color;
								impactPaint.SetStyle(Paint.Style.Stroke);
								canvas.DrawCircle(pos.X, pos.Y, radius, impactPaint);
							}
							break;
					}
				}
//2nd pass to draw texts above everything
				foreach (var entity in entities) {
					switch (entity.Type) {
						case EntityType.Player: {
							var pos = entity.Origin.ToViewPosition(mapData.Min,mapData.Max,width,height,true);
							pos += posOffset;
//player name
							using var ss = ColourTextValueConverter.Convert(entity.Name);
							if (ss == null)
								continue;
							using var l = MakeTextLayout(ss, textPaint);
							canvas.Save();
							canvas.Translate(pos.X-l.GetLineWidth(0)*0.5f, pos.Y);
//shadow
							var shadowColor = ColourTextValueConverter.ShadowColor;
							var shadowOffset = ColourTextValueConverter.ShadowOffset;
							using var colorMatrix = new ColorMatrix();
							colorMatrix.SetScale(shadowColor.R/255.0f, shadowColor.G/255.0f, shadowColor.B/255.0f, 1.0f);
							using var colorFilter = new ColorMatrixColorFilter(colorMatrix);
							textShadowPaint.SetColorFilter(colorFilter);
							if (mapData.HasShadow) {
//coloured shadow
								string shadowName = entity.Name.CleanString(shadow: true);
								using var ss2 = ColourTextValueConverter.Convert(shadowName);
								using var l2 = MakeTextLayout(ss2, textShadowPaint);
								canvas.Save();
								canvas.Translate(shadowOffset.X.DpToPxF(), shadowOffset.Y.DpToPxF());
								l2.Draw(canvas);
								canvas.Restore();
							} else {
//regular shadow
								canvas.DrawText(ss, 0, ss.Length(), shadowOffset.X.DpToPxF(), l.GetLineTop(1)-l.GetLineDescent(0)+shadowOffset.Y.DpToPxF(), textShadowPaint);
							}
							l.Draw(canvas);
							canvas.Restore();
							break;
						}
					}
				}
				StaticLayout MakeTextLayout(ISpannable source, TextPaint tp) {
					int width = 10000.DpToPx();
					if (Build.VERSION.SdkInt >= BuildVersionCodes.M) {
						using var b = StaticLayout.Builder.Obtain(source, 0, source.Length(), tp, width);
						return b.Build();
					} else {
						return new StaticLayout(source, tp, width, global::Android.Text.Layout.Alignment.AlignNormal, 1.0f, 0.0f, false);
					}
				}
			}
		}
	}
}