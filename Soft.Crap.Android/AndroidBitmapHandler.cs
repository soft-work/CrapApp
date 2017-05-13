using System;
using System.Threading.Tasks;

using Android.Graphics;
using Android.Util;

using Soft.Crap.Exceptions;

namespace Soft.Crap.Android
{
    // https://developer.xamarin.com/recipes/android/other_ux/camera_intent/take_a_picture_and_save_using_camera_app/

    // https://developer.android.com/training/displaying-bitmaps/load-bitmap.html

    public static class AndroidBitmapHandler
    {
        public static async Task<Size> GetBitmapSizeAsync
        (
            string fileName
        )
        {
            return await Task.Run
            (
                () =>

                {
                    Size bitmapSize = GetBitmapSizeSync(fileName);

                    return bitmapSize;
                }
            );
        }

        public static async Task<Bitmap> LoadAdjustedBitmapAsync
        (
            string fileName,
            int viewWidth,
            int viewHeight,
            int pictureOrientation,
            //Bitmap reusedBitmap,
            byte[] thumbnailData = null
        )
        {
            return await Task.Run
            (
                () =>

                {
                    Bitmap adjustedBitmap = LoadAdjustedBitmapSync(fileName,
                                                                   viewWidth,
                                                                   viewHeight,
                                                                   pictureOrientation,
                                                                   //reusedBitmap,
                                                                   thumbnailData);
                    return adjustedBitmap;
                }
            );
        }

        private static Size GetBitmapSizeSync
        (
            string fileName
        )
        {
            var bitmapOptions = new BitmapFactory.Options
            {
                InJustDecodeBounds = true
            };

            BitmapFactory.DecodeFile(fileName,
                                     bitmapOptions);

            // Next we calculate the ratio that we need to resize the image by
            // in order to fit the requested dimensions:

            var bitmapSize = new Size(bitmapOptions.OutWidth,
                                      bitmapOptions.OutHeight);
            return bitmapSize;
        }        

        private static Bitmap LoadAdjustedBitmapSync
        (
            string fileName,
            int viewWidth,
            int viewHeight,
            int pictureOrientation,            
            //Bitmap reusedBitmap,
            byte[] thumbnailData = null
        )
        {
            if (viewWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(viewWidth),
                                                      viewWidth,
                                                      "Should be greater than 0.");
            }

            if (viewHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(viewHeight),
                                                      viewHeight,
                                                      "Should be greater than 0.");
            }

            var reuseOptions = new BitmapFactory.Options();

            /*if (reusedBitmap != null)
            {
                reuseOptions.InMutable = true;
                reuseOptions.InBitmap = reusedBitmap;
            };*/

            Bitmap thumbnailBitmap = null;

            // If thumbnail data provided, use it instead of going to the file for full picture:

            if (thumbnailData != null)
            {                
                thumbnailBitmap = BitmapFactory.DecodeByteArray(thumbnailData,
                                                                0,
                                                                thumbnailData.Length);     
            }

            Bitmap sampleBitmap;

            if (thumbnailBitmap != null)
            {
                sampleBitmap = thumbnailBitmap;
                thumbnailBitmap = null;
            }
            else
            {
                // Get the the dimensions of the file on disk:

                Size bitmapSize = GetBitmapSizeSync(fileName);

                if ((bitmapSize.Width == -1) || (bitmapSize.Height == -1))
                {
                    // https://developer.android.com/reference/android/graphics/BitmapFactory.Options.html#outWidth
                    // outWidth will be set to -1 if there is an error trying to decode
                    // https://developer.android.com/reference/android/graphics/BitmapFactory.Options.html#outHeight
                    // outHeight will be set to - 1 if there is an error trying to decode

                    throw new CorruptObjectException(fileName);
                }

                // Next we calculate the ratio that we need to resize the image by
                // in order to fit the requested dimensions:

                int imageWidth = bitmapSize.Width;
                int imageHeight = bitmapSize.Height;

                int inSampleSize = 1;

                if ((viewWidth < imageWidth) || (viewHeight < imageHeight))
                {
                    int halfWidth = imageWidth / 2;
                    int halfHeight = imageHeight / 2;

                    // Calculate the largest inSampleSize value that is a power of 2 and keeps both
                    // height and width larger than the requested height and width:
                    while ((halfWidth / inSampleSize >= viewWidth) &&
                           (halfHeight / inSampleSize >= viewHeight))
                    {
                        inSampleSize *= 2;
                    }
                }

                // http://stackoverflow.com/questions/3331527/android-resize-a-large-bitmap-file-to-scaled-output-file/8497703#8497703

                // Now we will load the image and have BitmapFactory resize it for us:            

                var bitmapOptions = new BitmapFactory.Options
                {
                    InJustDecodeBounds = false,
                    InSampleSize = inSampleSize
                };                

                sampleBitmap = BitmapFactory.DecodeFile(fileName,
                                                        bitmapOptions);
            }                        

            Bitmap scaledBitmap = null;

            try
            {
                double sampleWidth = sampleBitmap.Width;
                double sampleHeight = sampleBitmap.Height;

                double widthRatio = viewWidth / sampleWidth;
                double heightRatio = viewHeight / sampleHeight;

                double scaledWidth;
                double scaledHeight;                

                if (widthRatio > heightRatio)
                {
                    scaledWidth = sampleWidth * heightRatio;
                    scaledHeight = sampleHeight * heightRatio;
                }
                else
                {
                    scaledWidth = sampleWidth * widthRatio;
                    scaledHeight = sampleHeight * widthRatio;
                }

                // This call may or may not produce a new bitmap, so input disposal
                // has to be conditional. It doesn't seem to produce a new bitmap
                // when scale is exactly 1. As what 'is exactly 1' really means depends
                // on internal implementation, it is better to compare input and output
                // bitmap references rather than try to compare scale with 1.0:
                scaledBitmap = Bitmap.CreateScaledBitmap(sampleBitmap,
                                                         (int)Math.Round(scaledWidth),
                                                         (int)Math.Round(scaledHeight),
                                                         filter : true);
            }
            finally
            {
                // As input and output bitmaps may refer to the same object, disposal
                // has to be made conditional (so 'finally' rather than 'using' here):
                if (scaledBitmap != sampleBitmap)
                {
                    sampleBitmap.Dispose();
                    sampleBitmap = null;
                }
            }            

            Bitmap rotatedBitmap;

            if (pictureOrientation == 0)
            {
                rotatedBitmap = scaledBitmap;
            }
            else
            {
                var transformationMatrix = new Matrix();
                transformationMatrix.PostRotate(pictureOrientation);

                using(transformationMatrix)
                using(scaledBitmap)
                {
                    rotatedBitmap = Bitmap.CreateBitmap(scaledBitmap,
                                                        0,
                                                        0,
                                                        scaledBitmap.Width,
                                                        scaledBitmap.Height,
                                                        transformationMatrix,
                                                        filter : true);
                }                
            }

            scaledBitmap = null;            

            return rotatedBitmap;
        }        
    }
}

