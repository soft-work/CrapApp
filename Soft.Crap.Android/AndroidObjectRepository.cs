using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android.App;

using Soft.Crap.Android.Objects;
using Soft.Crap.IO;
using Soft.Crap.Logging;
using Soft.Crap.Objects;
using Soft.Crap.Sources;

using Environment = System.Environment;

namespace Soft.Crap.Android
{
    public static class AndroidObjectRepository
    {
        public static async Task LoadAndroidObjectsAsync
        (
            PortableContextLogger contextLogger,
            Action<int> updateCount
        )
        {
            // http://blog.wislon.io/posts/2014/09/28/xamarin-and-android-how-to-use-your-external-removable-sd-card

            //File[] externalMediaDirs = ACTIVITY GetExternalMediaDirs();
            //File externalFilesDir = GetExternalFilesDirs();

            //string cardPath = GetCardPath();

            IReadOnlyDictionary<string, string> fileProviders = new Dictionary<string, string>
            {
                ["Phone"] = "/storage/emulated/0",
                ["Card"] = "/storage/1E50-B617",
                //["Test"] = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures).Path + "/Test",
                //["Camera"] = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDcim).Path + "/Camera",
                //["Card"] = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDcim).Path.Replace("emulated/0", "1E50-B617") + "/Camera",
                //["Crap"] = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures).Path + "/CrapApp",
                //["WhatsApp Received"] = Environment.ExternalStorageDirectory.Path + "/WhatsApp/Media/WhatsApp Images",
                //["WhatsApp Sent"] = Environment.ExternalStorageDirectory.Path + "/WhatsApp/Media/WhatsApp Images/Sent"

                //Computer\Galaxy S5\Phone\WhatsApp\Media\WhatsApp Images
            };

            string sourceFile = AndroidCrapApplication.GetSourceFilePath();

            // https://developer.android.com/guide/topics/media/media-formats.html           

            /*Task imageTask = CreateObjectLoadingTask
            (
                contextLogger,
                updateCount,
                sourceFile,
                fileProviders,

                (
                    directorySource,
                    fileName
                )
                =>
                {
                    return new AndroidImageObject(directorySource,
                                                  fileName);
                },

                "bmp",
                "gif",
                "jpeg",
                "jpg",
                "png",
                "tiff",
                "webp"
            );*/

            Task videoTask = CreateObjectLoadingTask
            (
                contextLogger,
                updateCount,
                sourceFile,
                fileProviders,
                              
                (
                    directorySource,
                    fileName
                )
                =>
                {
                    return new AndroidVideoObject(directorySource,
                                                  fileName);
                },

                "avi",
                "mp4",
                "mpeg",
                "webm"
            );

            Task audioTask = CreateObjectLoadingTask
            (
                contextLogger,
                updateCount,
                sourceFile,
                fileProviders,

                (
                    directorySource,
                    fileName
                )
                =>
                {
                    return new AndroidAudioObject(directorySource,
                                                  fileName);
                },

                "aac",
                "flac",
                "imy",
                "m4a",
                "mid",
                "mkv",
                "mp3",
                // mp4 ?! and other possible overalaps between audio and video ^                          
                "mxmf",
                "ogg",
                "ota",
                "rtttl",
                "rtx",
                "ts",
                "wav",                
                "wma",
                "xmf"
            );            

            await Task.WhenAll(//imageTask,
                               videoTask,
                               audioTask);
        }

        private static Task CreateObjectLoadingTask
        (
            PortableContextLogger contextLogger,
            Action<int> updateCount,
            string sourceFile,
            IReadOnlyDictionary<string, string> fileProviders,                        
            Func<PortableDirectorySource, string, PortableFileObject> createObject,
            params string[] fileExtensions
        )
        {
            PortableFileEnumerator fileEnumerator = new DefaultFileEnumerator(fileProviders,
                                                                              fileExtensions);

            PortableFileDescriber fileDescriber = new DefaultFileDescriber();

            Task loadingTask = PortableObjectRepository<Activity>.LoadFileObjectsAsync
            (
                contextLogger,
                updateCount,
                sourceFile,
                fileEnumerator,
                fileDescriber,
                createObject                                
            );

            return loadingTask;
        }
    }      
}
