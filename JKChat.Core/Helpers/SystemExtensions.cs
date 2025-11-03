using System;
using System.Numerics;

namespace JKChat.Core.Helpers {
	public static class SystemExtensions {
		public static Vector3 ToViewPosition(this Vector3 gamePosition, Vector3 min, Vector3 max, float width, float height, bool flipVertically) {
			var bounds = max-min;
			var normalizedPosition = (gamePosition-min)/bounds;
			float boundsRatio = bounds.X / bounds.Y;
			float viewRatio = width / height;
			bool boundsLandscape = boundsRatio > 1.0f;
			bool viewLandscape = viewRatio > 1.0f;
			float boundsViewWidth, boundsViewHeight;
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
			float xOffset = (width - boundsViewWidth) * 0.5f,
				yOffset = (height - boundsViewHeight) * 0.5f;
			float x = normalizedPosition.X*boundsViewWidth,
				y = (flipVertically ? (1.0f-normalizedPosition.Y) : normalizedPosition.Y)*boundsViewHeight,
				z = normalizedPosition.Z;
			return new Vector3((xOffset+x), (yOffset+y), z);
		}

		public static Vector3 ToViewPosition(this Vector3 gamePosition, Vector3 min, Vector3 max, double width, double height, bool flipVertically) {
			return gamePosition.ToViewPosition(min,max,(float)width,(float)height,flipVertically);
		}
		public static double ToRadians(this float angle) {
			return (double)(angle * Math.PI / 180.0f);
		}
		public static bool IsProgressActive(this float p) {
			return p > 0.0f && p < 1.0f;
		}
		public static int ToPercent(this float p) {
			return (int)(p * 100.0f);
		}
		public static string ToPercentString(this float p) {
			return p.ToPercent() + "%";
		}
	}
}