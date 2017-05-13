using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android.Content;
using Android.Graphics;

using Soft.Crap.Correlation;
using Soft.Crap.Objects;
using Soft.Crap.Rendering;
using Soft.Crap.Sources;

using File = System.IO.File;
using Path = System.IO.Path;

namespace Soft.Crap.Android.Objects
{
    public class AndroidAudioObject : DefaultFileObject,
                                      PortableSyncRenderer<Context>,
                                      PortableAsyncRenderer<Bitmap>
    {
        public AndroidAudioObject
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
            get { return Resource.Drawable.type_audio; }
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
            get { return Resource.Drawable.type_audio; }
        }

        int PortableSyncRenderer<Context>.TypeName
        {
            get { return Resource.String.AudioObjectType; }
        }

        void PortableSyncRenderer<Context>.EditObject
        (
            Context currentContext,
            int deviceOrientation
        )
        {
            /*var editImageIntent = new Intent(currentContext, typeof(AndroidEditImageActivity));

            editImageIntent.PutExtra(AndroidEditImageActivity.DeviceOrientationExtra,
                                     deviceOrientation);

            editImageIntent.PutExtra(AndroidEditImageActivity.PicturePathExtra,
                                     FilePath);

            currentContext.StartActivity(editImageIntent);*/
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

            /*
             
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
            string thumbnailPath = Path.Combine(DirectorySource.DirectoryPath,
                                                "AlbumArtSmall.jpg");

            if (File.Exists(thumbnailPath) == false)
            {
                return null;
            }

            return await correlatedEntity.GetThumbnailAsync(correlationTag,
                                                            viewWidth,
                                                            viewHeight,
                                                            //reusedThumbnail,
                                                            thumbnailPath);            
        }
    }
}