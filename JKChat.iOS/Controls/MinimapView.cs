using System;
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
	private UIImageView minimapImageView;
	private MinimapDrawingView minimapDrawingView;

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
					var image = UIImage.FromResource(MapData.Assembly, MapData.Path);
					minimapImageView.Image = image;
					LayoutSubviews();
				} catch (Exception exception) {
					System.Diagnostics.Debug.WriteLine(exception);
					minimapImageView.Image = null;
				}
			} else {
				minimapImageView.Image = null;
			}
			minimapDrawingView.SetNeedsDisplay();
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
		UserInteractionEnabled = false;
		BackgroundColor = UIColor.Clear;
		minimapImageView = new() {
			ContentMode = UIViewContentMode.ScaleToFill
		};
		AddSubview(minimapImageView);
		minimapDrawingView = new(this) {
			BackgroundColor = UIColor.Clear
		};
		AddSubview(minimapDrawingView);
	}

	public override void LayoutSubviews() {
		base.LayoutSubviews();

		if (minimapImageView?.Image is not { } image)
			return;

		nfloat imageViewXOrigin = 0.0f,
			imageViewYOrigin = 0.0f,
			imageViewWidth,
			imageViewHeight;

		nfloat widthScaleFactor = Frame.Width / image.Size.Width,
			heightScaleFactor = Frame.Height / image.Size.Height;

//AspectScaleToFit
		if (widthScaleFactor < heightScaleFactor) {
			imageViewWidth = Bounds.Size.Width;
			imageViewHeight = Bounds.Size.Width * image.Size.Height / image.Size.Width;
		} else {
			imageViewWidth = Bounds.Size.Height * image.Size.Width / image.Size.Height;
			imageViewHeight = Bounds.Size.Height;
		}

//horizontal alignment center
		imageViewXOrigin += (Frame.Width - imageViewWidth) * 0.5f;

		this.minimapDrawingView.Frame = this.minimapImageView.Frame = new CGRect(imageViewXOrigin, imageViewYOrigin, imageViewWidth, imageViewHeight);
	}

	private class MinimapDrawingView : UIView {
		private const float fontSize = 12.0f;
		private static readonly CGFont font = CGFont.CreateWithFontName(UIFont.SystemFontOfSize(fontSize, UIFontWeight.Regular).Name);
		private static readonly CTLine constantCapLine = new(new NSAttributedString("JKchat"));
		private MinimapView minimapView;
		public MinimapDrawingView(MinimapView minimapView) {
			this.minimapView = minimapView;
		}
		public override void Draw(CGRect rect) {
			base.Draw(rect);
			
			var entities = minimapView.Entities;
			var mapData = minimapView.MapData;
			if (mapData == null || minimapView.minimapImageView.Image == null || entities.IsNullOrEmpty())
				return;

			var context = UIGraphics.GetCurrentContext();
			if (context == null)
				return;

			var now = DateTime.UtcNow;
			context.ClearRect(rect);
			context.TextMatrix = CGAffineTransform.MakeIdentity();
			context.TranslateCTM(0.0f, rect.Height);
			context.ScaleCTM(1.0f, -1.0f);
			CGPath path;
			foreach (var entity in entities) {
				var pos = entity.Origin.ToViewPosition(mapData.Min,mapData.Max,rect.Width,rect.Height,false);
				var pos2 = entity.Origin2.ToViewPosition(mapData.Min,mapData.Max,rect.Width,rect.Height,false);
				var color = entity.Color.ToUIColor();
				var lifeDiff = entity.Life - now;
				var lifeLeft = lifeDiff.TotalMilliseconds;
				switch (entity.Type) {
					case EntityType.Player:
						const float viewTriMedian = 30.0f;
						const float viewTriAngle = 30.0f;
						const float pointSize = 3.0f;
						const float nameYOffset = 13.37f;
//player triangle
						context.SaveState();
						context.TranslateCTM(pos.X, pos.Y);
						context.RotateCTM((float)entity.Angles.Y.ToRadians());
						path = new CGPath();
						path.AddLines(new CGPoint[]{ new(0.0f, 0.0f), new(viewTriMedian, Math.Tan(viewTriAngle.ToRadians()) * -viewTriMedian), new(viewTriMedian, Math.Tan(viewTriAngle.ToRadians()) * viewTriMedian) });
						context.AddPath(path);
						context.Clip();
						var cs = CGColorSpace.CreateDeviceRGB();
						var gradient = new CGGradient(cs, new []{ color.CGColor, color.ColorWithAlpha(0.0f).CGColor });
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
						path.AddLines(new CGPoint[] { new(0.0f, 0.0f), new(0.0f, flagHeight), new(flagWidth, flagHeight*0.75f), new(0.0f, flagHeight*0.5f) });
						context.AddPath(path);
						context.DrawPath(CGPathDrawingMode.FillStroke);
						context.RestoreState();
						break;
					case EntityType.Shot:
						const float shotLineWidth = 2.0f;
						const int shotFadeTime = 200;
						if (lifeLeft <= 0.0) {
							break;
						} else if (lifeLeft <= shotFadeTime) {
							color = color.ColorWithAlpha((float)(lifeLeft / shotFadeTime));
						}
						context.SetStrokeColor(color.CGColor);
						context.SetLineWidth(shotLineWidth);
						path = new CGPath();
						path.AddLines(new CGPoint[] { new(pos.X, pos.Y), new(pos2.X, pos2.Y) });
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
							path.AddLines(new CGPoint[] { new(pos.X, pos.Y), new(pos2.X, pos2.Y) });
							context.AddPath(path);
							context.DrawPath(CGPathDrawingMode.Stroke);
						}
						break;
					case EntityType.Impact:
						const float impactStartRadius = 2.0f;
						const float impactEndRadius = 20.0f;
						const float impactLineWidth = 2.0f;
						if (lifeLeft <= 0.0) {
							break;
						} else {
							float dl = (float)(lifeLeft / entity.LifeLength);
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
				var pos = entity.Origin.ToViewPosition(mapData.Min,mapData.Max,rect.Width,rect.Height,false);
				switch (entity.Type) {
					case EntityType.Player:
//player name
						const float nameYOffset = 23.0f;
						var text = ColourTextValueConverter.Convert(entity.Name, new ColourTextParameter() { ParseShadow = mapData.HasShadow, AddShadow = true });
						var line = new CTLine(text);
						context.SetFont(font);
						context.SetFontSize(fontSize);
						var horizontalBounds = line.GetImageBounds(context);
						var verticalBounds = constantCapLine.GetImageBounds(context);
						context.TextPosition = new(pos.X-horizontalBounds.Width*0.5f, pos.Y-nameYOffset+verticalBounds.Height*0.5f);
						line.Draw(context);
						break;
				}
			}
		}
	}
}