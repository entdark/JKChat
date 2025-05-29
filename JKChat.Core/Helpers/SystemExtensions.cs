using System;
using System.Numerics;

namespace JKChat.Core.Helpers {
	public static class SystemExtensions {
		public static Vector3 ToViewPosition(this Vector3 gamePosition, Vector3 min, Vector3 max, double width, double height, bool flipVertically) {
			var bounds = max-min;
			var normalizedPosition = (gamePosition-min)/bounds;
			float boundsRatio = bounds.X / bounds.Y;
			double viewRatio = width / height;
			bool boundsLandscape = boundsRatio > 1.0f;
			bool viewLandscape = viewRatio > 1.0f;
			double boundsViewWidth, boundsViewHeight;
			bool landscapeFit;
			if (viewLandscape) {
				if (boundsLandscape) {
					landscapeFit = boundsRatio > viewRatio;
				} else {
					landscapeFit = false;
				}
			} else {
				if (boundsLandscape) {
					landscapeFit = true;
				} else {
					landscapeFit = boundsRatio > viewRatio;
				}
			}
			if (landscapeFit) {
				boundsViewWidth = width;
				boundsViewHeight = boundsViewWidth / boundsRatio;
			} else {
				boundsViewHeight = height;
				boundsViewWidth = boundsViewHeight * boundsRatio;
			}
			double xOffset = (width - boundsViewWidth) * 0.5,
				yOffset = (height - boundsViewHeight) * 0.5;
			double x = normalizedPosition.X*boundsViewWidth,
				y = (flipVertically ? (1.0f-normalizedPosition.Y) : normalizedPosition.Y)*boundsViewHeight,
				z = normalizedPosition.Z;
			return new Vector3((float)(xOffset+x), (float)(yOffset+y), (float)z);
		}
		public static double ToRadians(this float angle) {
			return (double)(angle * Math.PI / 180.0f);
		}
	}
}