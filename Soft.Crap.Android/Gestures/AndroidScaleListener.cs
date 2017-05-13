using System;
using Android.Views;

namespace Soft.Crap.Android.Gestures
{
    public class AndroidScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
    {
        private readonly Func<float> _getScaleFactor;
        private readonly Action<float> _setScaleFactor;

        private readonly float _minScaleFactor;
        private readonly float _maxScaleFactor;

        public AndroidScaleListener
        (
            Func<float> getScaleFactor,
            Action<float> setScaleFactor,
            float minScaleFactor,
            float maxScaleFactor
        )
        {
            _getScaleFactor = getScaleFactor;
            _setScaleFactor = setScaleFactor;

            _minScaleFactor = minScaleFactor;
            _maxScaleFactor = maxScaleFactor;
        }

        /*public override bool OnScaleBegin(ScaleGestureDetector detector)
        {
            bool isScaling = base.OnScaleBegin(detector);

            return isScaling;
        }

        public override void OnScaleEnd(ScaleGestureDetector detector)
        {
            base.OnScaleEnd(detector);
        }*/

        public override bool OnScale
        (
            ScaleGestureDetector scaleDetector
        )
        {
            float scaleFactor = _getScaleFactor();

            scaleFactor *= scaleDetector.ScaleFactor;

            // Don't let the object get too small or too large:

            scaleFactor = Math.Max(_minScaleFactor,
                                   Math.Min(scaleFactor, _maxScaleFactor));
            
            _setScaleFactor(scaleFactor);

            return true;
        }
    }
}