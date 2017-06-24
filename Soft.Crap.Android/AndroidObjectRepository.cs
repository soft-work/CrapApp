using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android.App;

using Soft.Crap.Android.Objects;
using Soft.Crap.IO;
using Soft.Crap.Logging;
using Soft.Crap.Objects;
using Soft.Crap.Sources;

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
            IReadOnlyDictionary<string, string> fileProviders = AndroidCrapApplication.GetFileProviders
            (
                phoneProviderKey : "Phone",

                phoneProviderRoot : "/storage/emulated/0", // TODO: how do I know it?

                cardProviderPrefix : "Card"
            );

            string sourceFile = AndroidCrapApplication.GetSourceFilePath();

            // https://developer.android.com/guide/topics/media/media-formats.html           

            Task imageTask = CreateObjectLoadingTask
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
            );

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

            await Task.WhenAll(imageTask,
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
