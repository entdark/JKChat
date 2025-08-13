using System;

using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;

using AndroidX.AppCompat.Widget;
using AndroidX.Core.OS;
using AndroidX.Core.View;
using Orientation = Android.Content.Res.Orientation;

namespace JKChat.Android.Controls.TouchImageView;

//source: https://github.com/MikeOrtiz/TouchImageView/blob/master/touchview/src/main/java/com/ortiz/touchview/TouchImageView.kt
[Register("JKChat.Android.Controls.TouchImageView")]
public class TouchImageView : AppCompatImageView
{
	private const float DefaultSuperMinMultiplier = 0.75f;
	private const float DefaultSuperMaxMultiplier = 1.25f;

	private const int DefaultZoomTime = 200;

	public const float AutomaticMinZoom = -1.0f;

	private Matrix? touchMatrix, prevMatrix;
	private bool orientationJustChanged = false;

	private ImageActionState imageActionState;
	private float userSpecifiedMinScale = 0.0f;
	private float minScale = 0.0f;
	private bool maxScaleIsSetByMultiplier = false;
	private float maxScaleMultiplier = 0.0f;
	private float maxScale = 0.0f;
	private float[]? floatMatrix;

	private Orientation orientation = Orientation.Undefined;
	private ScaleType? touchScaleType;
	private bool imageRenderedAtLeastOnce = false;
	private bool onDrawReady = false;

	private int viewWidth, viewHeight, prevViewWidth, prevViewHeight;
	private float matchViewWidth, matchViewHeight, prevMatchViewWidth, prevMatchViewHeight;

	private ZoomVariables? delayedZoomVariables;

	private Fling? fling;
	private ScaleGestureDetector? scaleDetector;
	private GestureDetector? gestureDetector;

	private IOnTouchCoordinatesListener? touchCoordinatesListener = null;
	private GestureDetector.IOnDoubleTapListener? doubleTapListener = null;
	private IOnTouchListener? userTouchListener = null;
	private IOnTouchImageViewListener? touchImageViewListener = null;

	private float superMinScale => SuperMinMultiplier * minScale;
	private float superMaxScale => SuperMaxMultiplier * maxScale;

	private float imageWidth => matchViewWidth * CurrentZoom;
/*!*/	private float imageHeight => /*matchViewHeight*/viewHeight * CurrentZoom;

	public float ScaledImageWidth => matchViewWidth * CurrentZoom;
	public float ScaledImageHeight => matchViewHeight * CurrentZoom;

	public float SuperMinMultiplier { get; set; } = DefaultSuperMinMultiplier;
	public float SuperMaxMultiplier { get; set; } = DefaultSuperMaxMultiplier;
	public float CurrentZoom { get; private set; }
	public bool IsZoomEnabled { get; set; } = true;
	public bool IsSuperZoomEnabled { get; set; } = true;
	public bool IsRotateImageToFitScreen { get; set; } = false;
	public float DoubleTapScale { get; set; } = 0.0f;

	public FixedPixel OrientationChangeFixedPixel { get; set; } = FixedPixel.Center;
	public FixedPixel ViewSizeChangeFixedPixel { get; set; } = FixedPixel.Center;

	public bool IsZoomed => CurrentZoom != 1.0f;

	private int zoomTime = DefaultZoomTime;
	public int ZoomTime
	{
		get => zoomTime;
		set => zoomTime = Math.Clamp(value, 1, int.MaxValue);
	}

	public RectF ZoomedRect
	{
		get
		{
			if (touchScaleType == ScaleType.FitXy)
				throw new Java.Lang.UnsupportedOperationException("ZoomedRect is not supported with FitXy ScaleType");

			var topLeft = TransformCoordTouchToBitmap(0.0f, 0.0f, true);
			var bottomRight = TransformCoordTouchToBitmap(viewWidth, viewHeight, true);
			var drawable = Drawable;
			int w = GetDrawableWidth(drawable!);
			int h = GetDrawableHeight(drawable!);
			return new RectF(topLeft.X / w, topLeft.Y / h, bottomRight.X / w, bottomRight.Y / h);
		}
	}

	public float MinZoom
	{
		get => minScale;
		set
		{
			userSpecifiedMinScale = value;
			if (value == AutomaticMinZoom)
			{
				if (touchScaleType == ScaleType.Center || touchScaleType == ScaleType.CenterCrop)
				{
					var drawable = Drawable;
					int drawableWidth = GetDrawableWidth(drawable!);
					int drawableHeight = GetDrawableHeight(drawable!);
					if (drawable != null && drawableWidth > 0 && drawableHeight > 0)
					{
						float widthRatio = (float)viewWidth / drawableWidth;
						float heightRatio = (float)viewHeight / drawableHeight;
						if (touchScaleType == ScaleType.Center)
							minScale = Math.Min(widthRatio, heightRatio);
						else
							minScale = Math.Min(widthRatio, heightRatio) / Math.Max(widthRatio, heightRatio);
					}
				}
				else
				{
					minScale = 1.0f;
				}
			}
			else
			{
				minScale = userSpecifiedMinScale;
			}

			if (maxScaleIsSetByMultiplier)
				SetMaxZoomRatio(maxScaleMultiplier);
		}
	}

	public float MaxZoom
	{
		get => maxScale;
		set
		{
			maxScale = value;
			maxScaleIsSetByMultiplier = false;
		}
	}

	public PointF ScrollPosition
	{
		get
		{
			var drawable = Drawable;
			if (drawable == null)
				return new PointF(0.5f, 0.5f);

			int drawableWidth = GetDrawableWidth(drawable!);
			int drawableHeight = GetDrawableHeight(drawable!);

			var point = TransformCoordTouchToBitmap(viewWidth * 0.5f, viewHeight * 0.5f, true);
			point.X /= drawableWidth;
			point.Y /= drawableHeight;
			return point;
		}
	}

	public override Matrix ImageMatrix {
		get => base.ImageMatrix;
		set {
			base.ImageMatrix = value;
			ImageMatrixChanged?.Invoke(this, EventArgs.Empty);
		}
	}

/*!*/	public event EventHandler ImageMatrixChanged;

	public TouchImageView(Context context) : base(context)
	{
		Initialize();
	}

	public TouchImageView(Context context, IAttributeSet attrs) : base(context, attrs)
	{
		Initialize();
	}

	public TouchImageView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
	{
		Initialize();
	}

	private void Initialize()
	{
		base.Clickable = true;
		orientation = Resources!.Configuration!.Orientation;
		scaleDetector = new ScaleGestureDetector(Context!, new ScaleListener(this));
		gestureDetector = new GestureDetector(Context, new GestureListener(this));
		touchMatrix = new Matrix();
		prevMatrix = new Matrix();
		floatMatrix = new float[9];
		touchScaleType = ScaleType.FitCenter;
		CurrentZoom = 1.0f;
		minScale = 1.0f;
		maxScale = 3.0f;
		ApplyTouchMatrix();
		SetScaleType(ScaleType.Matrix!);
		SetState(ImageActionState.None);
		onDrawReady = false;
		base.SetOnTouchListener(new PrivateOnTouchListener(this));
	}

	public override void SetOnTouchListener(IOnTouchListener? onTouchListener)
	{
		userTouchListener = onTouchListener;
	}

	public void SetOnTouchImageViewListener(IOnTouchImageViewListener? onTouchImageViewListener)
	{
		touchImageViewListener = onTouchImageViewListener;
	}

	public void SetOnDoubleTapListener(GestureDetector.IOnDoubleTapListener? onDoubleTapListener)
	{
		doubleTapListener = onDoubleTapListener;
	}

	public void SetOnTouchImageViewListener(IOnTouchCoordinatesListener? onTouchCoordinatesListener)
	{
		touchCoordinatesListener = onTouchCoordinatesListener;
	}

	public override void SetImageResource(int resId)
	{
		imageRenderedAtLeastOnce = false;
		base.SetImageResource(resId);
		SavePreviousImageValues();
		FitImageToView();
	}

	public override void SetImageBitmap(Bitmap? bm)
	{
		imageRenderedAtLeastOnce = false;
		base.SetImageBitmap(bm);
		SavePreviousImageValues();
		FitImageToView();
	}

	public override void SetImageDrawable(Drawable? drawable)
	{
		imageRenderedAtLeastOnce = false;
		base.SetImageDrawable(drawable);
		SavePreviousImageValues();
		FitImageToView();
	}

	public override void SetImageURI(global::Android.Net.Uri? uri)
	{
		imageRenderedAtLeastOnce = false;
		base.SetImageURI(uri);
		SavePreviousImageValues();
		FitImageToView();
	}

	public override void SetScaleType(ScaleType? type)
	{
		if (type == ScaleType.Matrix)
		{
			base.SetScaleType(ScaleType.Matrix);
		}
		else
		{
			touchScaleType = type;
			if (onDrawReady)
				SetZoom(this);
		}
	}

	public override ScaleType? GetScaleType()
	{
		return touchScaleType;
	}

	private void SavePreviousImageValues()
	{
		if (viewHeight != 0 && viewWidth != 0)
		{
			touchMatrix!.GetValues(floatMatrix);
			prevMatrix!.SetValues(floatMatrix);
			prevMatchViewWidth = matchViewWidth;
			prevMatchViewHeight = matchViewHeight;
			prevViewWidth = viewWidth;
			prevViewHeight = viewHeight;
		}
	}

	protected override IParcelable OnSaveInstanceState()
	{
		touchMatrix!.GetValues(floatMatrix);
		var bundle = new Bundle();
		bundle.PutParcelable("parent", base.OnSaveInstanceState());
		bundle.PutInt("orientation", (int)orientation);
		bundle.PutFloat("saveScale", CurrentZoom);
		bundle.PutFloat("matchViewWidth", matchViewWidth);
		bundle.PutFloat("matchViewHeight", matchViewHeight);
		bundle.PutInt("viewWidth", viewWidth);
		bundle.PutInt("viewHeight", viewHeight);
		bundle.PutFloatArray("matrix", floatMatrix);
		bundle.PutBoolean("imageRendered", imageRenderedAtLeastOnce);
		bundle.PutInt("viewSizeChangeFixedPixel", (int)ViewSizeChangeFixedPixel);
		bundle.PutInt("orientationChangeFixedPixel", (int)OrientationChangeFixedPixel);
		return bundle;
	}

	protected override void OnRestoreInstanceState(IParcelable? state)
	{
		if (state is not Bundle bundle)
		{
			base.OnRestoreInstanceState(state);
			return;
		}

		base.OnRestoreInstanceState(BundleCompat.GetParcelable(bundle, "parent", Java.Lang.Class.FromType(typeof(IParcelable))) as IParcelable);
		CurrentZoom = bundle.GetFloat("saveScale");
		floatMatrix = bundle.GetFloatArray("matrix");
		prevMatrix!.SetValues(floatMatrix);
		prevMatchViewWidth = bundle.GetFloat("matchViewWidth");
		prevMatchViewHeight = bundle.GetFloat("matchViewHeight");
		prevViewWidth = bundle.GetInt("viewWidth");
		prevViewHeight = bundle.GetInt("viewHeight");
		imageRenderedAtLeastOnce = bundle.GetBoolean("imageRendered");
		ViewSizeChangeFixedPixel = (FixedPixel)bundle.GetInt("viewSizeChangeFixedPixel");
		OrientationChangeFixedPixel = (FixedPixel)bundle.GetInt("orientationChangeFixedPixel");
		var oldOrientation = (Orientation)bundle.GetInt("orientation");
		if (orientation != oldOrientation)
			orientationJustChanged = true;
	}

	protected override void OnDraw(Canvas? canvas)
	{
		onDrawReady = true;
		imageRenderedAtLeastOnce = true;
		if (delayedZoomVariables != null)
		{
			SetZoom(delayedZoomVariables.Scale, delayedZoomVariables.FocusX, delayedZoomVariables.FocusY, delayedZoomVariables.ScaleType);
			delayedZoomVariables = null;
		}
		base.OnDraw(canvas);
	}

	protected override void OnConfigurationChanged(Configuration? newConfig)
	{
		base.OnConfigurationChanged(newConfig);
		var newOrientation = Resources!.Configuration!.Orientation;
		if (newOrientation != orientation)
		{
			orientationJustChanged = true;
			orientation = newOrientation;
		}
		SavePreviousImageValues();
	}

	private void ApplyTouchMatrix()
	{
		ImageMatrix = touchMatrix;
	}

	public void SetMaxZoomRatio(float max)
	{
		maxScaleMultiplier = max;
		maxScale = minScale * maxScaleMultiplier;
		maxScaleIsSetByMultiplier = true;
	}

	public void ResetZoom()
	{
		CurrentZoom = 1.0f;
		FitImageToView();
	}

	public void ResetZoomAnimated()
	{
		SetZoomAnimated(1.0f, 0.5f, 0.5f);
	}

	public void SetZoom(float scale)
	{
		SetZoom(scale, 0.5f, 0.5f);
	}

	public void SetZoom(float scale, float focusX, float focusY)
	{
		SetZoom(scale, focusX, focusY, touchScaleType);
	}

	public void SetZoom(float scale, float focusX, float focusY, ScaleType? scaleType)
	{
		if (!onDrawReady)
		{
			delayedZoomVariables = new ZoomVariables(scale, focusX, focusY, scaleType);
			return;
		}

		if (userSpecifiedMinScale == AutomaticMinZoom)
		{
			MinZoom = AutomaticMinZoom;
			if (CurrentZoom < minScale)
				CurrentZoom = minScale;
		}

		if (scaleType != touchScaleType)
			SetScaleType(scaleType);

		ResetZoom();
		ScaleImage(scale, viewWidth * 0.5f, viewHeight * 0.5f, IsSuperZoomEnabled);
		touchMatrix!.GetValues(floatMatrix);
		floatMatrix![Matrix.MtransX] = -((focusX * imageWidth) - (viewWidth * 0.5f));
		floatMatrix[Matrix.MtransY] = -((focusY * imageHeight) - (viewHeight * 0.5f));
		touchMatrix.SetValues(floatMatrix);
		FixTrans();
		SavePreviousImageValues();
		ApplyTouchMatrix();
	}

	public void SetZoom(TouchImageView imageSource)
	{
		var center = imageSource.ScrollPosition;
		SetZoom(imageSource.CurrentZoom, center.X, center.Y, imageSource.GetScaleType());
	}

	private bool OrientationMismatch(Drawable drawable)
	{
		return (viewWidth > viewHeight) != (drawable.IntrinsicWidth > drawable.IntrinsicHeight);
	}

	private int GetDrawableWidth(Drawable drawable)
	{
		if (OrientationMismatch(drawable) && IsRotateImageToFitScreen)
			return drawable.IntrinsicHeight;
		else
			return drawable.IntrinsicWidth;
	}

	private int GetDrawableHeight(Drawable drawable)
	{
		if (OrientationMismatch(drawable) && IsRotateImageToFitScreen)
			return drawable.IntrinsicWidth;
		else
			return drawable.IntrinsicHeight;
	}

	public void SetScrollPosition(float focusX, float focusY)
	{
		SetZoom(CurrentZoom, focusX, focusY);
	}

	private void FixTrans()
	{
		touchMatrix!.GetValues(floatMatrix);
		float transX = floatMatrix![Matrix.MtransX];
		float transY = floatMatrix[Matrix.MtransY];

		float offset = 0.0f;
		if (IsRotateImageToFitScreen && OrientationMismatch(Drawable!))
			offset = imageWidth;

		float fixTransX = GetFixTrans(transX, viewWidth, imageWidth, offset);
		float fixTransY = GetFixTrans(transY, viewHeight, imageHeight, 0.0f);
		touchMatrix.PostTranslate(fixTransX, fixTransY);
	}

	private void FixScaleTrans()
	{
		FixTrans();
		touchMatrix!.GetValues(floatMatrix);
		if (imageWidth < viewWidth)
		{
			float xOffset = (viewWidth - imageWidth) * 0.5f;
			if (IsRotateImageToFitScreen && OrientationMismatch(Drawable!))
				xOffset += imageWidth;

			floatMatrix![Matrix.MtransX] = xOffset;
		}

		if (imageHeight < viewHeight)
			floatMatrix![Matrix.MtransY] = (viewHeight - imageHeight) * 0.5f;

		touchMatrix.SetValues(floatMatrix);
	}

	private static float GetFixTrans(float trans, float viewSize, float contentSize, float offset)
	{
		float minTrans, maxTrans;

		if (contentSize <= viewSize)
		{
			minTrans = offset;
			maxTrans = offset + viewSize - contentSize;
		}
		else
		{
			minTrans = offset + viewSize - contentSize;
			maxTrans = offset;
		}

		if (trans < minTrans)
			return -trans + minTrans;
		else if (trans > maxTrans)
			return -trans + maxTrans;

		return 0.0f;
	}

	private static float GetFixDragTrans(float delta, float viewSize, float contentSize)
	{
		if (contentSize <= viewSize)
			return 0.0f;
		else
			return delta;
	}

	protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
	{
		var drawable = Drawable;

		if (drawable == null || drawable.IntrinsicWidth == 0 || drawable.IntrinsicHeight == 0)
		{
			SetMeasuredDimension(0, 0);
			return;
		}

		int drawableWidth = GetDrawableWidth(drawable);
		int drawableHeight = GetDrawableHeight(drawable);
		int widthSize = MeasureSpec.GetSize(widthMeasureSpec);
		var widthMode = MeasureSpec.GetMode(widthMeasureSpec);
		int heightSize = MeasureSpec.GetSize(heightMeasureSpec);
		var heightMode = MeasureSpec.GetMode(heightMeasureSpec);
		int totalViewWidth = SetViewSize(widthMode, widthSize, drawableWidth);
		int totalViewHeight = SetViewSize(heightMode, heightSize, drawableHeight);

		if (!orientationJustChanged)
			SavePreviousImageValues();

		SetMeasuredDimension(totalViewWidth, totalViewHeight);
	}

	protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
	{
		base.OnSizeChanged(w, h, oldw, oldh);
		viewWidth = w - PaddingLeft - PaddingRight;
		viewHeight = h - PaddingTop - PaddingBottom;
		FitImageToView();
	}

	private static int SetViewSize(MeasureSpecMode mode, int size, int drawableSize)
	{
		return mode switch
		{
			MeasureSpecMode.Exactly => size,
			MeasureSpecMode.AtMost => Math.Min(drawableSize, size),
			MeasureSpecMode.Unspecified => drawableSize,
			_ => size,
		};
	}

	private void FitImageToView()
	{
		var fixedPixel = orientationJustChanged ? OrientationChangeFixedPixel : ViewSizeChangeFixedPixel;
		orientationJustChanged = false;

		var drawable = Drawable;

		if (drawable == null || drawable.IntrinsicWidth == 0 || drawable.IntrinsicHeight == 0)
			return;

		if (touchMatrix == null || prevMatrix == null)
			return;

		if (userSpecifiedMinScale == AutomaticMinZoom)
		{
			MinZoom = AutomaticMinZoom;
			if (CurrentZoom < minScale)
				CurrentZoom = minScale;
		}

		int drawableWidth = GetDrawableWidth(drawable),
			drawableHeight = GetDrawableHeight(drawable);

		float scaleX = (float)viewWidth / drawableWidth,
			scaleY = (float)viewHeight / drawableHeight;

		if (touchScaleType == ScaleType.Center)
			scaleX = scaleY = 1.0f;
		else if (touchScaleType == ScaleType.CenterCrop)
			scaleX = scaleY = Math.Max(scaleX, scaleY);
		else if (touchScaleType == ScaleType.CenterInside)
			scaleX = scaleY = Math.Min(1.0f, Math.Min(scaleX, scaleY));
		else if (touchScaleType == ScaleType.FitCenter || touchScaleType == ScaleType.FitStart || touchScaleType == ScaleType.FitEnd)
			scaleX = scaleY = Math.Min(scaleX, scaleY);
		else if (touchScaleType == ScaleType.FitXy)
		{
		}

		float redundantXSpace = viewWidth - (scaleX * drawableWidth),
			redundantYSpace = viewHeight - (scaleY * drawableHeight);

		matchViewWidth = viewWidth - redundantXSpace;
		matchViewHeight = viewHeight - redundantYSpace;
		if (!IsZoomed && !imageRenderedAtLeastOnce)
		{
			if (IsRotateImageToFitScreen && OrientationMismatch(drawable))
			{
				touchMatrix.SetRotate(90.0f);
				touchMatrix.PostTranslate(drawableWidth, 0.0f);
				touchMatrix.PostScale(scaleX, scaleY);
			}
			else
			{
				touchMatrix.SetScale(scaleX, scaleY);
			}
			if (touchScaleType == ScaleType.FitStart)
			{
/*!*/				touchMatrix.PostTranslate(redundantXSpace * 0.5f, 0.0f);
			}
			else if (touchScaleType == ScaleType.FitEnd)
			{
				touchMatrix.PostTranslate(redundantXSpace, redundantYSpace);
			}
			else
			{
				touchMatrix.PostTranslate(redundantXSpace * 0.5f, redundantYSpace * 0.5f);
			}
			CurrentZoom = 1.0f;
		}
		else
		{
			if (prevMatchViewWidth == 0.0f || prevMatchViewHeight == 0.0f)
				SavePreviousImageValues();

			prevMatrix.GetValues(floatMatrix);

			floatMatrix[Matrix.MscaleX] = matchViewWidth / drawableWidth * CurrentZoom;
			floatMatrix[Matrix.MscaleY] = matchViewHeight / drawableHeight * CurrentZoom;

			float transX = floatMatrix[Matrix.MtransX],
				transY = floatMatrix[Matrix.MtransY];

			float prevActualWidth = prevMatchViewWidth * CurrentZoom,
				actualWidth = imageWidth;
			floatMatrix[Matrix.MtransX] = NewTranslationAfterChange(transX, prevActualWidth, actualWidth, prevViewWidth, viewWidth, drawableWidth, fixedPixel);

			float prevActualHeight = prevMatchViewHeight * CurrentZoom,
				actualHeight = imageHeight;
			floatMatrix[Matrix.MtransY] = NewTranslationAfterChange(transY, prevActualHeight, actualHeight, prevViewHeight, viewHeight, drawableHeight, fixedPixel);

			touchMatrix.SetValues(floatMatrix);
		}
		FixTrans();
		ApplyTouchMatrix();
	}

	private float NewTranslationAfterChange(float trans, float prevImageSize, float imageSize, int prevViewSize, int viewSize, int drawableSize, FixedPixel sizeChangeFixedPixel)
	{
		if (imageSize < viewSize)
		{
			return (viewSize - (drawableSize * floatMatrix![Matrix.MscaleX])) * 0.5f;
		}
		else if (trans > 0.0f)
		{
			return -((imageSize - viewSize) * 0.5f);
		}
		else
		{
			float fixedPixelPositionInView = sizeChangeFixedPixel switch
			{
				FixedPixel.BottomRight => 1.0f,
				FixedPixel.TopLeft => 0.0f,
				_ => 0.5f,
			};
			float fixedPixelPositionInImage = (-trans + fixedPixelPositionInView * prevViewSize) / prevImageSize;
			return -(fixedPixelPositionInImage * imageSize - viewSize * fixedPixelPositionInView);
		}
	}

	private void SetState(ImageActionState imageActionState)
	{
		this.imageActionState = imageActionState;
		System.Diagnostics.Debug.WriteLine("this.imageActionState: " + this.imageActionState);
	}

	public override bool CanScrollHorizontally(int direction)
	{
		touchMatrix!.GetValues(floatMatrix);
		var x = floatMatrix![Matrix.MtransX];
		if (imageWidth < viewWidth)
			return false;
		else if (x >= -1 && direction < 0)
			return false;
		else
			return (Math.Abs(x) + viewWidth + 1 < imageWidth) || direction <= 0;
	}

	public override bool CanScrollVertically(int direction)
	{
		touchMatrix!.GetValues(floatMatrix);
		var y = floatMatrix![Matrix.MtransY];
		if (imageHeight < viewHeight)
			return false;
		else if (y >= -1 && direction < 0)
			return false;
		else
			return (Math.Abs(y) + viewHeight + 1 < imageHeight) || direction <= 0;
	}

	private void ScaleImage(double deltaScale, float focusX, float focusY, bool stretchImageToSuper)
	{
		double deltaScaleLocal = deltaScale;
		float lowerScale, upperScale;
		if (stretchImageToSuper)
		{
			lowerScale = superMinScale;
			upperScale = superMaxScale;
		}
		else
		{
			lowerScale = minScale;
			upperScale = maxScale;
		}

		float origScale = CurrentZoom;
		CurrentZoom *= (float)deltaScaleLocal;
		if (CurrentZoom > upperScale)
		{
			CurrentZoom = upperScale;
			deltaScaleLocal = (double)upperScale / origScale;
		}
		else if (CurrentZoom < lowerScale)
		{
			CurrentZoom = lowerScale;
			deltaScaleLocal = (double)lowerScale / origScale;
		}

		touchMatrix!.PostScale((float)deltaScaleLocal, (float)deltaScaleLocal, focusX, focusY);
		FixScaleTrans();
	}

	private PointF TransformCoordTouchToBitmap(float x, float y, bool clipToBitmap)
	{
		touchMatrix!.GetValues(floatMatrix);

		float origW = Drawable!.IntrinsicWidth,
			origH = Drawable.IntrinsicHeight,
			transX = floatMatrix![Matrix.MtransX],
			transY = floatMatrix[Matrix.MtransY],
			finalX = ((x - transX) * origW) / imageWidth,
			finalY = ((y - transY) * origH) / imageHeight;

		if (clipToBitmap)
		{
			finalX = Math.Min(Math.Max(finalX, 0.0f), origW);
			finalY = Math.Min(Math.Max(finalY, 0.0f), origH);
		}

		return new PointF(finalX, finalY);
	}

	private PointF TransformCoordBitmapToTouch(float bx, float by)
	{
		touchMatrix!.GetValues(floatMatrix);
		float origW = Drawable!.IntrinsicWidth,
			origH = Drawable.IntrinsicHeight,
			px = bx / origW,
			py = by / origH,
			finalX = floatMatrix![Matrix.MtransX] + imageWidth * px,
			finalY = floatMatrix[Matrix.MtransY] + imageHeight * py;
		return new PointF(finalX, finalY);
	}

	private void CompatPostOnAnimation(Java.Lang.IRunnable runnable)
	{
		PostOnAnimation(runnable);
	}

	public void SetZoomAnimated(float scale, float focusX, float focusY)
	{
		var animation = new AnimatedZoom(this, scale, new PointF(focusX, focusY), ZoomTime);
		CompatPostOnAnimation(animation);
	}

	public void SetZoomAnimated(float scale, float focusX, float focusY, int zoomTimeMs)
	{
		var animation = new AnimatedZoom(this, scale, new PointF(focusX, focusY), zoomTimeMs);
		CompatPostOnAnimation(animation);
	}

	public void SetZoomAnimated(float scale, float focusX, float focusY, int zoomTimeMs, IOnZoomFinishedListener listener)
	{
		var animation = new AnimatedZoom(this, scale, new PointF(focusX, focusY), zoomTimeMs, listener);
		CompatPostOnAnimation(animation);
	}

	public void SetZoomAnimated(float scale, float focusX, float focusY, IOnZoomFinishedListener listener)
	{
		var animation = new AnimatedZoom(this, scale, new PointF(focusX, focusY), listener: listener);
		CompatPostOnAnimation(animation);
	}

	private class GestureListener : GestureDetector.SimpleOnGestureListener
	{
		private readonly TouchImageView imageView;

		public GestureListener(TouchImageView imageView)
		{
			this.imageView = imageView;
		}

		public override bool OnSingleTapConfirmed(MotionEvent ev)
		{
			return imageView.doubleTapListener?.OnSingleTapConfirmed(ev) ?? imageView.PerformClick();
		}

		public override void OnLongPress(MotionEvent ev)
		{
			imageView.PerformLongClick();
		}

		public override bool OnFling(MotionEvent ev1, MotionEvent ev2, float velocityX, float velocityY)
		{
			imageView.fling?.CancelFling();
			imageView.fling = new Fling(imageView, (int)velocityX, (int)velocityY);
			imageView.CompatPostOnAnimation(imageView.fling);
			return base.OnFling(ev1, ev2, velocityX, velocityY);
		}

		public override bool OnDoubleTap(MotionEvent ev)
		{
			if (!imageView.IsZoomEnabled)
				return false;

			bool consumed = imageView.doubleTapListener?.OnDoubleTap(ev) ?? false;
			if (imageView.imageActionState == ImageActionState.None)
			{
				float maxZoomScale = imageView.DoubleTapScale == 0.0f ? imageView.maxScale : imageView.DoubleTapScale;
				float targetZoom = (imageView.CurrentZoom == imageView.minScale) ? maxZoomScale : imageView.minScale;
				var doubleTap = new DoubleTapZoom(imageView, targetZoom, ev.GetX(), ev.GetY(), false);
				imageView.CompatPostOnAnimation(doubleTap);
				consumed = true;
			}
			return consumed;
		}

		public override bool OnDoubleTapEvent(MotionEvent ev)
		{
			return imageView.doubleTapListener?.OnDoubleTapEvent(ev) ?? false;
		}
	}

	private class PrivateOnTouchListener : Java.Lang.Object, IOnTouchListener
	{
		private readonly TouchImageView imageView;

		private readonly PointF last = new PointF();

		public PrivateOnTouchListener(TouchImageView imageView)
		{
			this.imageView = imageView;
		}

		public bool OnTouch(View? v, MotionEvent? ev)
		{
			if (imageView.Drawable == null)
			{
				imageView.SetState(ImageActionState.None);
				return false;
			}

			if (imageView.IsZoomEnabled)
				imageView.scaleDetector!.OnTouchEvent(ev!);
			imageView.gestureDetector!.OnTouchEvent(ev!);

			var curr = new PointF(ev!.GetX(), ev.GetY());

			if (imageView.imageActionState == ImageActionState.None || imageView.imageActionState == ImageActionState.Drag || imageView.imageActionState == ImageActionState.Fling)
			{
				switch (ev.Action)
				{
					case MotionEventActions.Down:
						last.Set(curr);
						imageView.fling?.CancelFling();
						imageView.SetState(ImageActionState.Drag);
						break;

					case MotionEventActions.Move:
						if (imageView.imageActionState == ImageActionState.Drag)
						{
							float dx = curr.X - last.X,
								dy = curr.Y - last.Y,
								fixTransX = GetFixDragTrans(dx, imageView.viewWidth, imageView.imageWidth),
								fixTransY = GetFixDragTrans(dy, imageView.viewHeight, imageView.imageHeight);
							imageView.touchMatrix!.PostTranslate(fixTransX, fixTransY);
							imageView.FixTrans();
							last.Set(curr.X, curr.Y);
						}
						break;

					case MotionEventActions.Up:
					case MotionEventActions.PointerUp:
						imageView.SetState(ImageActionState.None);
						break;
				}
			}

			imageView.touchCoordinatesListener?.OnTouchCoordinate(v, ev, imageView.TransformCoordTouchToBitmap(ev.GetX(), ev.GetY(), true));

			imageView.ApplyTouchMatrix();

			imageView.userTouchListener?.OnTouch(v, ev);
			imageView.touchImageViewListener?.OnMove();

			return true;
		}
	}

	private class ScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
	{
		private readonly TouchImageView imageView;

		public ScaleListener(TouchImageView imageView)
		{
			this.imageView = imageView;
		}

		public override bool OnScaleBegin(ScaleGestureDetector detector)
		{
			imageView.SetState(ImageActionState.Zoom);
			return true;
		}

		public override bool OnScale(ScaleGestureDetector detector)
		{
			imageView.ScaleImage(detector.ScaleFactor, detector.FocusX, detector.FocusY, imageView.IsSuperZoomEnabled);
			imageView.touchImageViewListener?.OnMove();
			return true;
		}

		public override void OnScaleEnd(ScaleGestureDetector detector)
		{
			base.OnScaleEnd(detector);

			imageView.SetState(ImageActionState.None);

			bool animateToZoomBoundary = false;
			float targetZoom = imageView.CurrentZoom;

			if (imageView.CurrentZoom > imageView.maxScale)
			{
				targetZoom = imageView.maxScale;
				animateToZoomBoundary = true;

			}
			else if (imageView.CurrentZoom < imageView.minScale)
			{
				targetZoom = imageView.minScale;
				animateToZoomBoundary = true;
			}

			if (animateToZoomBoundary)
			{
				var doubleTap = new DoubleTapZoom(imageView, targetZoom, imageView.viewWidth * 0.5f, imageView.viewHeight * 0.5f, imageView.IsSuperZoomEnabled);
				imageView.CompatPostOnAnimation(doubleTap);
			}
		}
	}

	protected class DoubleTapZoom : Java.Lang.Object, Java.Lang.IRunnable
	{
		private readonly TouchImageView imageView;
		private readonly long startTime;
		private readonly float startZoom, targetZoom;
		private readonly float bitmapX, bitmapY;
		private readonly bool stretchImageToSuper;
		private readonly IInterpolator interpolator = new AccelerateDecelerateInterpolator();
		private readonly PointF startTouch;
		private readonly PointF endTouch;

		public DoubleTapZoom(TouchImageView imageView, float targetZoom, float focusX, float focusY, bool stretchImageToSuper)
		{
			this.imageView = imageView;
			this.imageView.SetState(ImageActionState.AnimateZoom);
			startTime = Java.Lang.JavaSystem.CurrentTimeMillis();
			startZoom = this.imageView.CurrentZoom;
			this.targetZoom = targetZoom;
			this.stretchImageToSuper = stretchImageToSuper;
			var bitmapPoint = this.imageView.TransformCoordTouchToBitmap(focusX, focusY, false);
			bitmapX = bitmapPoint.X;
			bitmapY = bitmapPoint.Y;

			startTouch = this.imageView.TransformCoordBitmapToTouch(bitmapX, bitmapY);
			endTouch = new PointF(this.imageView.viewWidth * 0.5f, this.imageView.viewHeight * 0.5f);
		}

		public void Run()
		{
			if (imageView.Drawable == null)
			{
				imageView.SetState(ImageActionState.None);
				return;
			}

			float t = Interpolate();
			double deltaScale = CalculateDeltaScale(t);
			imageView.ScaleImage(deltaScale, bitmapX, bitmapY, stretchImageToSuper);
			TranslateImageToCenterTouchPosition(t);
			imageView.FixScaleTrans();
			imageView.ApplyTouchMatrix();

			imageView.touchImageViewListener?.OnMove();

			if (t < 1.0f)
				imageView.CompatPostOnAnimation(this);
			else
				imageView.SetState(ImageActionState.None);
		}

		private void TranslateImageToCenterTouchPosition(float t)
		{
			float targetX = startTouch.X + t * (endTouch.X - startTouch.X),
				targetY = startTouch.Y + t * (endTouch.Y - startTouch.Y);
			var curr = imageView.TransformCoordBitmapToTouch(bitmapX, bitmapY);
			imageView.touchMatrix!.PostTranslate(targetX - curr.X, targetY - curr.Y);
		}

		private float Interpolate()
		{
			float dt = (float)(Java.Lang.JavaSystem.CurrentTimeMillis() - startTime) / this.imageView.ZoomTime;
			dt = Math.Min(1.0f, dt);
			return interpolator.GetInterpolation(dt);
		}

		private double CalculateDeltaScale(float t)
		{
			double zoom = startZoom + t * (double)(targetZoom - startZoom);
			return zoom / imageView.CurrentZoom;
		}
	}

	protected class Fling : Java.Lang.Object, Java.Lang.IRunnable
	{
		private readonly TouchImageView imageView;
		private readonly OverScroller scroller;
		private int currX, currY;

		public Fling(TouchImageView imageView, int velocityX, int velocityY)
		{
			this.imageView = imageView;
			this.imageView.SetState(ImageActionState.Fling);
			scroller = new OverScroller(this.imageView.Context);
			this.imageView.touchMatrix!.GetValues(this.imageView.floatMatrix);

			int startX = (int)this.imageView.floatMatrix![Matrix.MtransX];
			int startY = (int)this.imageView.floatMatrix[Matrix.MtransY];
			int minX, maxX, minY, maxY;

			if (this.imageView.IsRotateImageToFitScreen && this.imageView.OrientationMismatch(this.imageView.Drawable!))
				startX -= (int)this.imageView.imageWidth;

			if (this.imageView.imageWidth > this.imageView.viewWidth)
			{
				minX = this.imageView.viewWidth - (int)this.imageView.imageWidth;
				maxX = 0;
			}
			else
				minX = maxX = startX;

			if (this.imageView.imageHeight > this.imageView.viewHeight)
			{
				minY = this.imageView.viewHeight - (int)this.imageView.imageHeight;
				maxY = 0;
			}
			else
				minY = maxY = startY;

			scroller.Fling(startX, startY, velocityX, velocityY, minX, maxX, minY, maxY);
			currX = startX;
			currY = startY;
		}

		public void CancelFling()
		{
			imageView.SetState(ImageActionState.None);
			scroller.ForceFinished(true);
		}

		public void Run()
		{
			imageView.touchImageViewListener?.OnMove();

			if (scroller.IsFinished)
				return;

			if (scroller.ComputeScrollOffset())
			{
				int newX = scroller.CurrX;
				int newY = scroller.CurrY;
				int transX = newX - currX;
				int transY = newY - currY;
				currX = newX;
				currY = newY;
				imageView.touchMatrix!.PostTranslate(transX, transY);
				imageView.FixTrans();
				imageView.ApplyTouchMatrix();
				imageView.CompatPostOnAnimation(this);
			}
		}
	}

	private class AnimatedZoom : Java.Lang.Object, Java.Lang.IRunnable
	{
		private readonly TouchImageView imageView;
		private readonly int zoomTimeMillis;
		private readonly long startTime;
		private readonly float startZoom;
		private readonly float targetZoom;
		private readonly PointF startFocus;
		private readonly PointF targetFocus;
		private readonly IInterpolator interpolator = new DecelerateInterpolator();
		private readonly IOnZoomFinishedListener? zoomFinishedListener;

		public AnimatedZoom(TouchImageView imageView, float targetZoom, PointF focus, int zoomTimeMillis = DefaultZoomTime, IOnZoomFinishedListener? listener = null)
		{
			this.imageView = imageView;
			this.imageView.SetState(ImageActionState.AnimateZoom);
			startTime = Java.Lang.JavaSystem.CurrentTimeMillis();
			startZoom = this.imageView.CurrentZoom;
			this.targetZoom = targetZoom;
			this.zoomTimeMillis = zoomTimeMillis;
			startFocus = this.imageView.ScrollPosition;
			targetFocus = focus;
			zoomFinishedListener = listener;
		}

		public void Run()
		{
			float t = Interpolate();

			float nextZoom = startZoom + (targetZoom - startZoom) * t,
				nextX = startFocus.X + (targetFocus.X - startFocus.X) * t,
				nextY = startFocus.Y + (targetFocus.Y - startFocus.Y) * t;
			imageView.SetZoom(nextZoom, nextX, nextY);

			if (t < 1.0f)
			{
				imageView.CompatPostOnAnimation(this);
			}
			else
			{
				imageView.SetState(ImageActionState.None);
				zoomFinishedListener?.OnZoomFinished();
			}
		}

		private float Interpolate()
		{
			var dt = (float)(Java.Lang.JavaSystem.CurrentTimeMillis() - startTime) / zoomTimeMillis;
			dt = Math.Min(1.0f, dt);
			return interpolator.GetInterpolation(dt);
		}
	}

	private class ZoomVariables
	{
		public float Scale;
		public float FocusX;
		public float FocusY;
		public ScaleType? ScaleType;

		public ZoomVariables(float scale, float focusX, float focusY, ScaleType? scaleType)
		{
			Scale = scale;
			FocusX = focusX;
			FocusY = focusY;
			ScaleType = scaleType;
		}
	}
}