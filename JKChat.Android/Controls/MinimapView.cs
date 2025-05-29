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
using JKChat.Core.ValueCombiners;

namespace JKChat.Android.Controls;

[Register("JKChat.Android.Controls.MinimapView")]
public class MinimapView : FrameLayout {
	private ImageView minimapImageView;

	private EntityData []entities;
	public EntityData []Entities {
		get => entities;
		set { entities = value; Invalidate(); }
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
					using var stream = MapData.Assembly.GetManifestResourceStream(MapData.Path);
					var drawable = Drawable.CreateFromStream(stream, null);
					minimapImageView.SetImageDrawable(drawable);
				} catch (Exception exception) {
					System.Diagnostics.Debug.WriteLine(exception);
				}
			} else {
				minimapImageView.SetImageDrawable(null);
			}
			Invalidate();
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
		};
		minimapImageView.SetScaleType(ImageView.ScaleType.FitStart);
		minimapImageView.SetAdjustViewBounds(true);
		AddView(minimapImageView);
		SetWillNotDraw(false);
	}
	
	private static readonly float flagHeight = 10.0f.DpToPxF();
	private static readonly float flagWidth = 7.0f.DpToPxF();
	private static readonly float flagLineWidth = 2.0f.DpToPxF();
	private static readonly float viewTriMedian = 30.0f.DpToPxF();
	private static readonly float viewTriAngle = 30.0f;
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

		if (MapData == null || minimapImageView.Drawable == null || Entities.IsNullOrEmpty())
			return;
		
		var now = DateTime.UtcNow;
		Path path;
		foreach (var entity in Entities) {
			var pos = entity.Origin.ToViewPosition(MapData.Min,MapData.Max,MeasuredWidth,MeasuredHeight,true);
			var pos2 = entity.Origin2.ToViewPosition(mapData.Min,mapData.Max,MeasuredWidth,MeasuredHeight,true);
			var color = new Color(entity.Color.ToArgb());
			var lifeDiff = entity.Life - now;
			var lifeLeft = lifeDiff.TotalMilliseconds;
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
					trianglePaint.SetShader(new LinearGradient(0.0f, 0.0f, viewTriMedian, 0.0f, color, new Color(ColorUtils.SetAlphaComponent(color, 0)), Shader.TileMode.Clamp));
					canvas.DrawPath(path, trianglePaint);
					canvas.Restore();
//player point
					pointPaint.Color = color;
					pointPaint.SetStyle(Paint.Style.Stroke);
					canvas.DrawPoint(pos.X, pos.Y, pointPaint);
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
					break;
				case EntityType.Shot:
					const int shotFadeTime = 200;
					if (lifeLeft <= 0.0) {
						break;
					} else if (lifeLeft <= shotFadeTime) {
						color.A = unchecked((byte)(255*((float)(lifeLeft / shotFadeTime))));
					}
					path = new Path();
					path.MoveTo(pos.X, pos.Y);
					path.LineTo(pos2.X, pos2.Y);
					shotPaint.Color = color;
					shotPaint.SetStyle(Paint.Style.Stroke);
					canvas.DrawPath(path, shotPaint);
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
					}
					break;
				case EntityType.Impact:
					if (lifeLeft <= 0.0) {
						break;
					} else {
						float dl = (float)(lifeLeft / entity.LifeLength);
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
		foreach (var entity in Entities) {
			var pos = entity.Origin.ToViewPosition(MapData.Min,MapData.Max,MeasuredWidth,MeasuredHeight,true);
			switch (entity.Type) {
				case EntityType.Player:
//player name
					var ss = ColourTextValueConverter.Convert(entity.Name, new ColourTextParameter() { ParseShadow = MapData.HasShadow, AddShadow = true });
					StaticLayout l;
					if (Build.VERSION.SdkInt >= BuildVersionCodes.M) {
						var b = StaticLayout.Builder.Obtain(ss, 0, ss.Length(), textPaint, MeasuredWidth);
						l = b.Build();
					} else {
						l = new StaticLayout(ss, textPaint, MeasuredWidth, global::Android.Text.Layout.Alignment.AlignNormal, 1.0f, 0.0f, false);
					}
					canvas.Save();
					canvas.Translate(pos.X-l.GetLineWidth(0)*0.5f, pos.Y);
					l.Draw(canvas);
					canvas.Restore();
					break;
			}
		}
	}
}