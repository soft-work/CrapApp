using System;

using Android.Graphics;
using Android.Util;
using Android.Views;

namespace Soft.Crap.Android.Gestures
{
    // https://developer.android.com/training/gestures/scroll.html#term

    // https://developer.android.com/reference/android/support/v4/view/ViewPager.html

    // https://developer.android.com/training/implementing-navigation/lateral.html

    // http://stackoverflow.com/questions/4098198/adding-fling-gesture-to-an-image-view-android

    public class AndroidFlingListener : GestureDetector.SimpleOnGestureListener
    {
        private readonly Func<Size> _getViewSize;        
        private readonly Func<float> _getScaleFactor;
        private readonly Func<PointF> _getScrollPoint;

        private readonly float _minHorizontalDistance;
        private readonly float _minVerticalDistance;

        private readonly float _minHorizontalVelocity;
        private readonly float _minVerticalVelocity;

        private readonly Action<float, float> _moveScrolledView;        

        public AndroidFlingListener
        (
            Func<Size> getViewSize,
            Func<float> getScaleFactor,
            Func<PointF> getScrollPoint,
            float minHorizontalDistance,
            float minVerticalDistance,
            float minHorizontalVelocity, // (pixels per second)
            float minVerticalVelocity,
            Action<float, float> moveScrolledView                        
        )
        {
            _getViewSize = getViewSize;            

            _getScaleFactor = getScaleFactor;
            _getScrollPoint = getScrollPoint;

            _minHorizontalDistance = minHorizontalDistance;
            _minVerticalDistance = minVerticalDistance;

            _minHorizontalVelocity = minHorizontalVelocity;
            _minVerticalVelocity = minVerticalVelocity;

            _moveScrolledView = moveScrolledView;            
        }        

        public override bool OnFling
        (
            MotionEvent startFlingMotion, 
            MotionEvent stopFlingMotion, 
            float motionHorizontalVelocity, 
            float motionVerticalVelocity
        )
        {            
            float motionHorizontalDistance;
            int motionHorizontalDirection;

            GetDimentionDistanceAndDirection(startFlingMotion.GetX(),
                                             stopFlingMotion.GetX(),
                                             motionHorizontalVelocity,
                                             _minHorizontalDistance,
                                             _minHorizontalVelocity,
                                             out motionHorizontalDistance,
                                             out motionHorizontalDirection);
            float motionVerticalDistance;
            int motionVerticalDirection;

            GetDimentionDistanceAndDirection(startFlingMotion.GetY(),
                                             stopFlingMotion.GetY(),
                                             motionVerticalVelocity,
                                             _minVerticalDistance,
                                             _minVerticalVelocity,
                                             out motionVerticalDistance,
                                             out motionVerticalDirection);     

            bool isMotionHandled = (motionHorizontalDistance + motionVerticalDistance > 0);                                    

            if (isMotionHandled)
            {
                Size unscaledViewSize = _getViewSize();
                float viewScaleFactor = _getScaleFactor();
                PointF viewScrollPoint = _getScrollPoint();

                float horizontalScrollDistance = (motionHorizontalDirection == 0)
                                               ? 0
                                               : GetFeasibleDimentionDistance
                                                 (
                                                    motionHorizontalDistance,
                                                    motionHorizontalDirection,
                                                    viewScaleFactor,
                                                    viewScrollPoint.X,
                                                    unscaledViewSize.Width
                                                 );

                float verticalScrollDistance = (motionVerticalDirection == 0)
                                             ? 0
                                             : GetFeasibleDimentionDistance
                                               (
                                                   motionVerticalDistance,
                                                   motionVerticalDirection,
                                                   viewScaleFactor,
                                                   viewScrollPoint.Y,
                                                   unscaledViewSize.Height
                                               );

                if (horizontalScrollDistance + verticalScrollDistance > 0)
                {
                    _moveScrolledView
                    (
                        - motionHorizontalDirection * horizontalScrollDistance,

                        - motionVerticalDirection * verticalScrollDistance
                    );
                }                
            }            

            return isMotionHandled;
        }

        private void GetDimentionDistanceAndDirection
        (
            float startMotionPosition,
            float stopMotionPosition,
            float motionDimentionVelocity,
            float minDimentionDistance,
            float minDimentionVelocity,
            out float motionDimentionDistance,
            out int motionDimentionDirection
        )
        {
            float motionDimentionShift = stopMotionPosition - startMotionPosition;

            motionDimentionDistance = Math.Abs(motionDimentionShift);
            motionDimentionDirection = 0;            

            if ((motionDimentionDistance > minDimentionDistance) &&
                (Math.Abs(motionDimentionVelocity) > minDimentionVelocity))
            {                
                motionDimentionDirection = Math.Sign(motionDimentionShift);
            }
            else
            {
                motionDimentionDistance = 0;
            }
        }

        private float GetFeasibleDimentionDistance
        (
            float intendedDimentionDistance,
            int intendedDimentionDirection,
            float viewScaleFactor,
            float viewScrollPosition,
            float unscaledViewLength
        )
        {
            float feasibleDimentionDistance = 0;

            if (intendedDimentionDistance > 0)
            {
                float scaledViewLength = unscaledViewLength * viewScaleFactor;
                int viewScrollDirection = (viewScrollPosition != 0)
                                        ? Math.Sign(viewScrollPosition)
                                        : 1; // can be + or - 1, but not 0

                float distanceFromEdge = ((scaledViewLength - unscaledViewLength) / 2) +
                                         intendedDimentionDirection *
                                         viewScrollDirection *
                                         viewScrollPosition;

                feasibleDimentionDistance = (float)Math.Round
                (
                    Math.Min(intendedDimentionDistance,
                             distanceFromEdge)
                );
            }

            return feasibleDimentionDistance;
        }
    }
}