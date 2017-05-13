using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.Provider;

using Soft.Crap.Android.Activities.Video;
using Soft.Crap.Correlation;
using Soft.Crap.Exceptions;
using Soft.Crap.Objects;
using Soft.Crap.Rendering;
using Soft.Crap.Sources;

namespace Soft.Crap.Android.Objects
{
    public class AndroidVideoObject : DefaultFileObject,
                                      PortableSyncRenderer<Context>,
                                      PortableAsyncRenderer<Bitmap>
    {
        public AndroidVideoObject
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
            get { return Resource.Drawable.type_video; }
        }

        int PortableSyncRenderer<Context>.TypeName
        {
            get { return Resource.String.VideoObjectType; }
        }

        void PortableSyncRenderer<Context>.EditObject
        (
            Context currentContext,
            int deviceOrientation
        )
        {
            var playVideoIntent = new Intent(currentContext, typeof(AndroidPlayVideoActivity));

            playVideoIntent.PutExtra(AndroidPlayVideoActivity.DeviceOrientationExtra,
                                     deviceOrientation);

            playVideoIntent.PutExtra(AndroidPlayVideoActivity.VideoPathExtra,
                                     FilePath);

            currentContext.StartActivity(playVideoIntent);
        }

        async Task<IReadOnlyDictionary<string, object>> PortableAsyncRenderer<Bitmap>.GetAttributesAsync
        (
            PortableCorrelatedEntity correlatedEntity,
            Func<string> correlationTag
        )
        {
            return await Task.Run
            (
                () =>

                {
                    IReadOnlyDictionary<string, object> objectAttributes = new Dictionary<string, object>();

                    return objectAttributes;
                }
            );

            /*if (correlatedEntity.CorrelationTag != correlationTag())
            {
                return null;
            }

            return null;

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

            return objectAttributes;*/
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
            if (correlatedEntity.CorrelationTag != correlationTag())
            {
                return null;
            }

            Task<Bitmap> thumbnailTask = ThumbnailUtils.CreateVideoThumbnailAsync
            (
                FilePath,
                ThumbnailKind.MicroKind
            );

            if (thumbnailTask.Result == null)
            {
                throw new CorruptObjectException(FileName);
            }

            return await thumbnailTask;
        }
    }
}