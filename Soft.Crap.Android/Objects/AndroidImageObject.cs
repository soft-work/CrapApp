using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android.Content;
using Android.Graphics;
using Android.Media;

using Soft.Crap.Android.Activities.Images;
using Soft.Crap.Objects;
using Soft.Crap.Correlation;
using Soft.Crap.Rendering;
using Soft.Crap.Sources;

namespace Soft.Crap.Android.Objects
{
    public class AndroidImageObject : DefaultFileObject,
                                      PortableSyncRenderer<Context>,
                                      PortableAsyncRenderer<Bitmap>
    {
        public AndroidImageObject
        (
            PortableDirectorySource directorySource,
            string fileName
        )
        : base
        (
            directorySource,
            fileName
        )
        { }

        int? PortableSyncRenderer<Context>.ObjectDrawable
        {
            get { return null; }
        }

        string PortableSyncRenderer<Context>.ObjectDescription
        {
            get { return ObjectDescription; }
        }

        DateTime PortableSyncRenderer<Context>.ObjectTime
        {
            get { return ObjectTime; }
        }

        string PortableSyncRenderer<Context>.SourceDescription
        {
            get { return SourceDescription; }
        }

        string PortableSyncRenderer<Context>.TypeDescription
        {
            get { return TypeDescription; }
        }

        int PortableSyncRenderer<Context>.TypeDrawable
        {
            get { return Resource.Drawable.type_image; }
        }

        int PortableSyncRenderer<Context>.TypeName
        {
            get { return Resource.String.ImageObjectType; }
        }

        void PortableSyncRenderer<Context>.EditObject
        (
            Context currentContext,
            int deviceOrientation
        )
        {
            var editImageIntent = new Intent(currentContext,
                                             typeof(AndroidEditImageActivity));

            editImageIntent.PutExtra(AndroidEditImageActivity.DeviceOrientationExtra,
                                     deviceOrientation);

            editImageIntent.PutExtra(AndroidEditImageActivity.PicturePathExtra,
                                     FilePath);

            currentContext.StartActivity(editImageIntent);
        }

        async Task<IReadOnlyDictionary<string, object>> PortableAsyncRenderer<Bitmap>.GetAttributesAsync
        (
            PortableCorrelatedEntity correlatedEntity,
            Func<string> correlationTag
        )
        {
            if (correlatedEntity.CorrelationTag != correlationTag())
            {
                return null;
            }

            IReadOnlyDictionary<string, string> imageAttributes 
                = await AndroidExifHandler.GetPictureAttributesAsync
            (
                FilePath,
                attributeTags : new[]
                {
                    ExifInterface.TagOrientation,
                    ExifInterface.TagModel,
                    ExifInterface.TagDatetimeDigitized,

                    ExifInterface.TagDatetime,
                    ExifInterface.TagGpsAltitude,
                    ExifInterface.TagGpsAltitudeRef,
                    ExifInterface.TagGpsLatitude,
                    ExifInterface.TagGpsLatitudeRef,
                    ExifInterface.TagGpsLongitude,
                    ExifInterface.TagGpsLongitudeRef
                }
            );

            if (correlatedEntity.CorrelationTag != correlationTag())
            {
                return null;
            }

            var objectAttributes = new Dictionary<string, object>
            (
                imageAttributes.Count
            );

            foreach(KeyValuePair<string, string> imageAttribute in imageAttributes)
            {
                string attributeKey = imageAttribute.Key;

                object attributeValue = imageAttribute.Value;

                objectAttributes.Add(attributeKey,
                                     attributeValue);
            }            

            return objectAttributes;
        }

        async Task<Bitmap> PortableAsyncRenderer<Bitmap>.GetThumbnailAsync
        (
            PortableCorrelatedEntity correlatedEntity,
            Func<string> correlationTag,
            int viewWidth,
            int viewHeight//,
            //Bitmap reusedThumbnail
        )
        {
            return await correlatedEntity.GetThumbnailAsync(correlationTag,
                                                            viewWidth,
                                                            viewHeight,
                                                            //reusedThumbnail,
                                                            FilePath);            
        }
    }
}