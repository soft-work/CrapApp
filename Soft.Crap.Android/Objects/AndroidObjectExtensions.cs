using System;
using System.Threading.Tasks;

using Android.Graphics;

using Soft.Crap.Correlation;

namespace Soft.Crap.Android.Objects
{
    public static class AndroidObjectExtensions
    {        
        public static async Task<Bitmap> GetThumbnailAsync
        (            
            this PortableCorrelatedEntity correlatedEntity,
            Func<string> correlationTag,
            int viewWidth,
            int viewHeight,
            //Bitmap reusedThumbnail,
            string thumbnailPath
        )
        {
            if (correlatedEntity.CorrelationTag != correlationTag())
            {
                return null;
            }

            int pictureOrientation = await AndroidExifHandler.GetPictureOrientationAsync
            (
                thumbnailPath
            );

            if (correlatedEntity.CorrelationTag != correlationTag())
            {
                return null;
            }

            byte[] thumbnailData = await AndroidExifHandler.GetThumbnailDataAsync(thumbnailPath);

            if (correlatedEntity.CorrelationTag != correlationTag())
            {
                return null;
            }

            Bitmap thumbnailBitmap = await AndroidBitmapHandler.LoadAdjustedBitmapAsync
            (
                thumbnailPath,
                viewWidth,
                viewHeight,
                pictureOrientation,
                //reusedThumbnail,
                thumbnailData
            );

            if (correlatedEntity.CorrelationTag != correlationTag())
            {
                thumbnailBitmap?.Dispose();
                thumbnailBitmap = null;
            }

            return thumbnailBitmap;
        }
    }
}