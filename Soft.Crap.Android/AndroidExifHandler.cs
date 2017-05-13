using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android.Media;

// http://www.thevalvepage.com/swmonkey/2013/12/23/exif-tags-in-c/

// http://stackoverflow.com/questions/35295284/how-to-add-custom-exif-tags-to-a-image

namespace Soft.Crap.Android
{
    public static class AndroidExifHandler
    {
        public static async Task<byte[]> GetThumbnailDataAsync
        (
            string fileName
        )
        {
            return await Task.Run
            (
                () =>

                {
                    byte[] thumbnailData = GetThumbnailDataSync(fileName);

                    return thumbnailData;
                }
            );
        }

        public static async Task<IReadOnlyDictionary<string, string>> GetPictureAttributesAsync
        (
            string fileName,
            IEnumerable<string> attributeTags
        )
        {
            return await Task.Run
            (
                () =>

                {
                    IReadOnlyDictionary<string, string> pictureAttributes = GetPictureAttributesSync
                    (
                        fileName,
                        attributeTags
                    );

                    return pictureAttributes;
                }
            );
        }

        public static async void SetPictureAttributesAsync
        (
            string fileName,
            IReadOnlyDictionary<string, string> pictureAttributes
        )
        {
            await Task.Run
            (
                () =>

                {
                    SetPictureAttributesSync(fileName,
                                             pictureAttributes);
                }
            );
        }

        public static async Task<int> GetPictureOrientationAsync
        (
            string fileName
        )
        {
            return await Task.Run
            (
                () =>

                {
                    int pictureOrientation = GetPictureOrientationSync(fileName);

                    return pictureOrientation;
                }
            );
        }

        public static async Task SetPictureOrientationAsync
        (
            string fileName,
            int pictureOrientation
        )
        {
            await Task.Run
            (
                () =>

                {
                    SetPictureOrientationSync(fileName,
                                              pictureOrientation);
                }
            );
        }

        private static int GetPictureOrientationSync
        (
            string fileName
        )
        {
            IReadOnlyDictionary<string, string> pictureAttributes = GetPictureAttributesSync
            (
                fileName,
                new[] { ExifInterface.TagOrientation }
            );

            string orientationAttribute = pictureAttributes[ExifInterface.TagOrientation];

            int pictureOrientation = 0;

            switch(int.Parse(orientationAttribute))
            {
                case (int)Orientation.Rotate270:
                {
                    pictureOrientation = 270;

                    break;
                }

                case (int)Orientation.Rotate180:
                {
                    pictureOrientation = 180;

                    break;
                }

                case (int)Orientation.Rotate90:
                {
                    pictureOrientation = 90;

                    break;
                }
            }

            return pictureOrientation;
        }

        private static void SetPictureOrientationSync
        (
            string fileName,
            int pictureOrientation
        )
        {
            Orientation orientationAttribute = Orientation.Undefined;

            switch(pictureOrientation % 360)
            {
                case 0:
                {
                    orientationAttribute = Orientation.Normal;

                    break;
                }

                case 90:
                {
                    orientationAttribute = Orientation.Rotate90;

                    break;
                }

                case 180:
                {
                    orientationAttribute = Orientation.Rotate180;

                    break;
                }

                case 270:
                {
                    orientationAttribute = Orientation.Rotate270;

                    break;
                }
            }

            if (orientationAttribute == Orientation.Undefined)
            {
                throw new ArgumentOutOfRangeException(nameof(pictureOrientation),
                                                      pictureOrientation,
                                                      "Should be 0, 90, 180 or 270.");
            }

            SetPictureAttributesSync
            (
                fileName,
                new Dictionary<string, string>
                {
                    [ExifInterface.TagOrientation] = ((int)orientationAttribute).ToString()
                }
            );
        }

        private static byte[] GetThumbnailDataSync
        (
            string fileName
        )
        {
            // http://stackoverflow.com/questions/8383377/android-get-thumbnail-of-image-stored-on-sdcard-whose-path-is-known

            var exifInterface = GetExifInterface(fileName);

            byte[] thumbnailData = null;

            if (exifInterface.HasThumbnail)
            {
                thumbnailData = exifInterface.GetThumbnail();
            }

            return thumbnailData;
        } 

        private static IReadOnlyDictionary<string, string> GetPictureAttributesSync
        (
            string fileName,
            IEnumerable<string> attributeTags
        )
        {            
            var pictureAttributes = new ConcurrentDictionary<string, string>();            

            var exifInterface = GetExifInterface(fileName);

            Parallel.ForEach
            (
                attributeTags,

                attributeTag =>
                {
                    string attributeValue = exifInterface.GetAttribute(attributeTag);

                    pictureAttributes.TryAdd(attributeTag,
                                             attributeValue);                    
                }
            );

            return pictureAttributes;
        }

        private static void SetPictureAttributesSync
        (
            string fileName,
            IReadOnlyDictionary<string, string> pictureAttributes
        )
        {
            var exifInterface = GetExifInterface(fileName);

            Parallel.ForEach
            (
                pictureAttributes,

                pictureAttribute =>
                {
                    exifInterface.SetAttribute(pictureAttribute.Key,
                                               pictureAttribute.Value);
                }
            );

            exifInterface.SaveAttributes();
        }

        private static ExifInterface GetExifInterface
        (
            string fileName
        )
        {
            try
            {
                var exifInterface = new ExifInterface(fileName);

                return exifInterface;
            }
            catch(Exception exception)
            {
                AndroidCrapApplication.ApplicationLogger.LogDebug("EXIF {0} ! {1}",
                                                                  fileName,
                                                                  exception);
                throw;
            }
        }        
    }
}

