using System;
using System.Drawing;
using System.Numerics;

using CoreGraphics;

using CoreText;

using Foundation;

using JKChat.Core;
using JKChat.Core.Helpers;
using JKChat.Core.Models;
using JKChat.Core.ValueCombiners;
using JKChat.iOS.Helpers;
using JKChat.iOS.ValueConverters;

using ObjCRuntime;

using UIKit;

namespace JKChat.iOS.Controls;

[Register("MinimapView")]
public class MinimapView : UIView {
	private const float OutOfBoundsOffset = 1333.7f;

	private UIImageView minimapImageView;
	private MinimapDrawingView minimapDrawingView;
	private UIScrollView minimapScrollView;
	private UIView minimapContainerView;
	private NSLayoutConstraint imageRatioConstraint;

	private EntityData []entities;
	public EntityData []Entities {
		get => entities;
		set { entities = value; minimapDrawingView.SetNeedsDisplay(); }
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
					var image = MapData.Assembly != null ? UIImage.FromResource(MapData.Assembly, MapData.Path) : UIImage.FromFile(MapData.Path);
					CGSize size;
					if (image != null) {
//UIImageView fails to ScaleToFill when the image is smaller than the container, so scale it up to fit
						if (image.Size.Width > 0.0f && image.Size.Width < DeviceInfo.ScreenBounds.Width) {
							nfloat scale = DeviceInfo.ScreenBounds.Width / image.Size.Width;
							image = image.Scale(scale);
						}
						size = image.Size;
					} else {
						size = new CGSize(1.0f, 1.0f);
					}
					minimapImageView.Image = image;
					imageRatioConstraint.Active = false;
					(imageRatioConstraint = NSLayoutConstraint.Create(minimapImageView, NSLayoutAttribute.Height, NSLayoutRelation.Equal, minimapImageView, NSLayoutAttribute.Width, size.Height / size.Width, 0.0f)).Active = true;
					minimapScrollView.SetZoomScale(1.0f, false);
					LayoutSubviews();
				} catch (Exception exception) {
					System.Diagnostics.Debug.WriteLine(exception);
					minimapImageView.Image = null;
				}
			} else {
				minimapImageView.Image = null;
			}
			SetNeedsDisplay();
		}
	}

	public MinimapView() {
		Initialize();
	}

	public MinimapView(NSCoder coder) : base(coder) {
		Initialize();
	}

	public MinimapView(CGRect frame) : base(frame) {
		Initialize();
	}

	protected MinimapView(NSObjectFlag t) : base(t) {
		Initialize();
	}

	protected internal MinimapView(NativeHandle handle) : base(handle) {
		Initialize();
	}

	private void Initialize() {
		UserInteractionEnabled = true;
		BackgroundColor = UIColor.Clear;
		minimapImageView = new() {
			ContentMode = UIViewContentMode.ScaleToFill
		};
		minimapDrawingView = new(this) {
			UserInteractionEnabled = false,
			BackgroundColor = UIColor.Clear
		};
		minimapContainerView = new() {
			BackgroundColor = UIColor.Clear
		};
		minimapScrollView = new() {
			UserInteractionEnabled = true,
			MultipleTouchEnabled = true,
			ClipsToBounds = false,
			ContentMode = UIViewContentMode.ScaleToFill,
			MinimumZoomScale = 1.0f,
			MaximumZoomScale = 5.0f,
			ScrollsToTop = false,
			DelaysContentTouches = false,
			ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never,
			ViewForZoomingInScrollView = _ => minimapContainerView
		};
		minimapScrollView.AddWithConstraintsTo(this);
		minimapContainerView.AddSubview(minimapImageView);
		minimapImageView.TranslatesAutoresizingMaskIntoConstraints = false;
		minimapImageView.TopAnchor.ConstraintEqualTo(minimapContainerView.TopAnchor).Active = true;
		minimapImageView.CenterXAnchor.ConstraintEqualTo(minimapContainerView.CenterXAnchor).Active = true;
		minimapImageView.WidthAnchor.ConstraintLessThanOrEqualTo(minimapContainerView.WidthAnchor).Active = true;
		minimapImageView.HeightAnchor.ConstraintLessThanOrEqualTo(minimapContainerView.HeightAnchor).Active = true;
		(imageRatioConstraint = minimapImageView.HeightAnchor.ConstraintEqualTo(minimapImageView.WidthAnchor, 1.0f, 0.0f)).Active = true;
		minimapContainerView.AddWithConstraintsTo(minimapScrollView);
		minimapContainerView.WidthAnchor.ConstraintEqualTo(this.WidthAnchor).Active = true;
		minimapContainerView.HeightAnchor.ConstraintEqualTo(this.HeightAnchor).Active = true;
		minimapDrawingView.AddWithConstraintsTo(this,-OutOfBoundsOffset,0.0f,OutOfBoundsOffset,0.0f);
		var tap = new UITapGestureRecognizer(t => {
			if (minimapScrollView.ZoomScale > 1.0f) {
				minimapScrollView.SetZoomScale(1.0f, true);
			} else {
				ZoomToPoint(t.LocationInView(this), 2.0f, true);
			}
		}) {
			NumberOfTapsRequired = 2,
			CancelsTouchesInView = false
		};
		this.AddGestureRecognizer(tap);
	}

	private void ZoomToPoint(CGPoint point, float scale, bool animated) {
		var size = new CGSize(Bounds.Size.Width / scale, Bounds.Size.Height / scale);
		var rect = new CGRect(new(point.X - size.Width * 0.5f, point.Y - size.Height * 0.5f), size);
		this.minimapScrollView.ZoomToRect(rect, animated);
	}

	private static CGRect MakeAspectScaleToFitAlignToTopRect(CGSize containerSize, CGSize contentSize) {
		nfloat x = 0.0f,
			y = 0.0f,
			width,
			height;

		nfloat widthScaleFactor = containerSize.Width / contentSize.Width,
			heightScaleFactor = containerSize.Height / contentSize.Height;

//AspectScaleToFit
		if (widthScaleFactor < heightScaleFactor) {
			width = containerSize.Width;
			height = contentSize.Height * widthScaleFactor;
		} else {
			width = contentSize.Width * heightScaleFactor;
			height = containerSize.Height;
		}

//horizontal alignment center
		x += (containerSize.Width - width) * 0.5f;

		return new CGRect(x, y, width, height);
	}

	private class MinimapDrawingView(MinimapView minimapView) : UIView {
		private const float fontSize = 12.0f;
		private static readonly CGFont font = CGFont.CreateWithFontName(UIFont.SystemFontOfSize(fontSize, UIFontWeight.Regular).Name);

		public override void Draw(CGRect rect) {
			base.Draw(rect);
			
			var entities = minimapView.Entities;
			var mapData = minimapView.MapData;
			if (mapData == null || minimapView.minimapImageView.Image == null || entities.IsNullOrEmpty())
				return;

			var context = UIGraphics.GetCurrentContext();
			if (context == null)
				return;

			context.ClearRect(rect);
			context.TextMatrix = CGAffineTransform.MakeScale(1.0f, -1.0f);

//PresentationLayer properties for smooth animation
			var contentSize = minimapView.minimapContainerView.Layer.PresentationLayer?.Frame.Size ?? minimapView.minimapScrollView.ContentSize;
			var contentOffset = minimapView.minimapScrollView.Layer.PresentationLayer?.Bounds.Location ?? minimapView.minimapScrollView.ContentOffset;
			var frame = MakeAspectScaleToFitAlignToTopRect(contentSize, minimapView.minimapImageView.Image.Size);
			var size = frame.Size;
			var posOffset = new Vector3(
				(float)(frame.X-contentOffset.X+OutOfBoundsOffset),
				(float)(frame.Y-contentOffset.Y),
				0.0f
			);

			long now = App.Milliseconds;
			CGPath path;

			foreach (var entity in entities) {
				var pos = entity.Origin.ToViewPosition(mapData.Min,mapData.Max,size.Width,size.Height,true);
				pos += posOffset;
				var pos2 = entity.Origin2.ToViewPosition(mapData.Min,mapData.Max,size.Width,size.Height,true);
				pos2 += posOffset;
				var color = entity.Color.ToUIColor();
				long lifeLeft = entity.Life - now;
				switch (entity.Type) {
					case EntityType.Player:
						const float viewTriMedian = 30.0f;
						const float viewTriAngle = 30.0f;
						const float pointSize = 3.0f;
//player triangle
						context.SaveState();
						context.TranslateCTM(pos.X, pos.Y);
						context.RotateCTM((float)-entity.Angles.Y.ToRadians());
						path = new CGPath();
						path.AddLines([new(0.0f, 0.0f), new(viewTriMedian, Math.Tan(viewTriAngle.ToRadians()) * -viewTriMedian), new(viewTriMedian, Math.Tan(viewTriAngle.ToRadians()) * viewTriMedian)]);
						context.AddPath(path);
						context.Clip();
						var cs = CGColorSpace.CreateDeviceRGB();
						var gradient = new CGGradient(cs, [color.CGColor, color.ColorWithAlpha(0.0f).CGColor]);
						context.DrawLinearGradient(gradient, new(0.0f, 0.0f), new(viewTriMedian, 0.0f), CGGradientDrawingOptions.None);
						context.RestoreState();
//player point
						context.SetFillColor(color.CGColor);
						context.AddEllipseInRect(new(pos.X-pointSize*0.5f, pos.Y-pointSize*0.5f, pointSize, pointSize));
						context.DrawPath(CGPathDrawingMode.Fill);
						break;
					case EntityType.Vehicle:
						const float vehRadius = pointSize*3.0f;
						const float vehLineWidth = 2.0f;
						context.SetLineWidth(vehLineWidth);
						context.SetStrokeColor(color.CGColor);
						context.AddEllipseInRect(new(pos.X-vehRadius*0.5f, pos.Y-vehRadius*0.5f, vehRadius, vehRadius));
						context.DrawPath(CGPathDrawingMode.Stroke);
						break;
					case EntityType.Flag:
						const float flagHeight = 10.0f;
						const float flagWidth = 7.0f;
						const float flagLineWidth = 2.0f;
						context.SaveState();
						context.TranslateCTM(pos.X, pos.Y);
						context.SetFillColor(color.CGColor);
						context.SetStrokeColor(color.CGColor);
						context.SetLineWidth(flagLineWidth);
						path = new CGPath();
						path.AddLines([new(0.0f, 0.0f), new(0.0f, -flagHeight), new(flagWidth, -flagHeight*0.75f), new(0.0f, -flagHeight*0.5f)]);
						context.AddPath(path);
						context.DrawPath(CGPathDrawingMode.FillStroke);
						context.RestoreState();
						break;
					case EntityType.Shot:
						const float shotLineWidth = 2.0f;
						const int shotFadeTime = 200;
						if (lifeLeft <= 0) {
							break;
						} else if (lifeLeft <= shotFadeTime) {
							color = color.ColorWithAlpha((lifeLeft / (float)shotFadeTime));
						}
						context.SetStrokeColor(color.CGColor);
						context.SetLineWidth(shotLineWidth);
						path = new CGPath();
						path.AddLines([new(pos.X, pos.Y), new(pos2.X, pos2.Y)]);
						context.AddPath(path);
						context.DrawPath(CGPathDrawingMode.Stroke);
						break;
					case EntityType.Projectile:
						if (entity.Origin2 == Vector3.Zero) {
							const float projectilePointSize = 4.0f;
							context.SetFillColor(color.CGColor);
							context.AddEllipseInRect(new(pos.X-projectilePointSize*0.5f, pos.Y-projectilePointSize*0.5f, projectilePointSize, projectilePointSize));
							context.DrawPath(CGPathDrawingMode.Fill);
						} else {
							const float projectileLineWidth = 2.0f;
							context.SetStrokeColor(color.CGColor);
							context.SetLineWidth(projectileLineWidth);
							path = new CGPath();
							path.AddLines([new(pos.X, pos.Y), new(pos2.X, pos2.Y)]);
							context.AddPath(path);
							context.DrawPath(CGPathDrawingMode.Stroke);
						}
						break;
					case EntityType.Impact:
						const float impactStartRadius = 2.0f;
						const float impactEndRadius = 20.0f;
						const float impactLineWidth = 2.0f;
						if (lifeLeft <= 0) {
							break;
						} else {
							float dl = (lifeLeft / (float)entity.LifeLength);
							float radius = impactStartRadius + (1.0f - dl) * (impactEndRadius - impactStartRadius);
							color = color.ColorWithAlpha(dl);
							context.SetLineWidth(impactLineWidth);
							context.SetStrokeColor(color.CGColor);
							context.AddEllipseInRect(new(pos.X-radius*0.5f, pos.Y-radius*0.5f, radius, radius));
							context.DrawPath(CGPathDrawingMode.Stroke);
						}
						break;
				}
			}
//2nd pass to draw texts above everything
			foreach (var entity in entities) {
				var pos = entity.Origin.ToViewPosition(mapData.Min,mapData.Max,size.Width,size.Height,true);
				pos += posOffset;
				switch (entity.Type) {
					case EntityType.Player:
//player name
						const float nameYOffset = 23.0f;
						var text = ColourTextValueConverter.Convert(entity.Name, new ColourTextParameter() { ParseShadow = mapData.HasShadow, AddShadow = true, DefaultColor = Color.White });
						var line = new CTLine(text);
						context.SetFont(font);
						context.SetFontSize(fontSize);
						var bounds = line.GetImageBounds(context);
						context.TextPosition = new(pos.X-bounds.Width*0.5f, pos.Y+nameYOffset);
						line.Draw(context);
						break;
				}
			}
		}
	}
}