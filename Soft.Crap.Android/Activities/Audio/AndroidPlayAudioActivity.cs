﻿using System;

using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Media;
using Android.OS;
using Android.Views;
using Android.Widget;

using Object = Java.Lang.Object;

namespace Soft.Crap.Android.Activities.Video
{
    [Activity(Theme = "@style/Soft.Crap")]

    public class AndroidPlayAudioActivity : Activity
    {
        public const string DeviceOrientationExtra = "DeviceOrientation";
        public const string AudioPathExtra = "AudioPath";

        private string _audioPath;

        private VideoView _videoView;        
        MediaController _mediaController;

        private OrientationEventListener _orientationListener;
        private int? _deviceOrientation;

        protected override void OnCreate
        (
            Bundle bundle
        )
        {
            base.OnCreate(bundle);

            _audioPath = Intent.GetStringExtra(AudioPathExtra);

            if (_audioPath == null)
            {
                throw new InvalidOperationException(string.Format("'{0}' extra string missing.",
                                                    AudioPathExtra));
            }

            _orientationListener = new AndroidCameraActivity.DeviceOrientationEventListener
            (
                this,
                SensorDelay.Normal,
                OrientationChanged
            );

            _orientationListener.Enable();

            SetContentView(Resource.Layout.PlayVideoLayout);

            _videoView = FindViewById<VideoView>(Resource.Id.PlayedVideo);

            _mediaController = new MediaController(this,
                                                   useFastForward : false);            

            _mediaController.SetAnchorView(_videoView);
            _videoView.SetMediaController(_mediaController); 
            
            _videoView.SetOnPreparedListener                
            (
                new VideoPreparedListener
                (
                    () =>
                    {
                        _videoView.SeekTo(1);
                        _mediaController.Show();
                    }
                )
            );

            _videoView.SetOnCompletionListener
            (
                new VideoCompletionListener
                (
                    () => _mediaController.Show()
                )
            );

            _videoView.SetOnErrorListener
            (
                new VideoErrorListener
                (
                    this,
                    _audioPath,
                    getString : (resource) => { return GetString(resource); }
                )
            );           

            //_imageView.Touch -= TouchImage;
            //_imageView.Touch += TouchImage;            

            /*_videoView.Post // wait with image loading until view fully initialised
            (
               () => PlayVideoAsync()
            );*/

            _videoView.SetVideoPath(_audioPath);
        }

        protected override void OnResume()
        {
            base.OnResume();

            _orientationListener.Enable();            
        }

        protected override void OnPause()
        {
            _orientationListener.Disable();            

            base.OnPause();
        }

        protected override void OnDestroy()
        {            
            _orientationListener.Disable();

            base.OnDestroy();            
        }

        private void OrientationChanged
        (
            int deviceOrientation
        )
        {
            _deviceOrientation = deviceOrientation;
        }            

        private class VideoPreparedListener : Object, MediaPlayer.IOnPreparedListener
        {
            private readonly Action _onPrepared;

            public VideoPreparedListener
            (
                Action onPrepared
            )
            {
                _onPrepared = onPrepared;
            }

            void MediaPlayer.IOnPreparedListener.OnPrepared
            (
                MediaPlayer mediaPlayer
            )
            {
                mediaPlayer.SetScreenOnWhilePlaying(true);
                //mediaPlayer.Start();
                _onPrepared();
            }
        }

        private class VideoCompletionListener : Object, MediaPlayer.IOnCompletionListener
        {
            private readonly Action _onCompletion;

            public VideoCompletionListener
            (
                Action onCompletion
            )
            {
                _onCompletion = onCompletion;
            }

            void MediaPlayer.IOnCompletionListener.OnCompletion
            (
                MediaPlayer mediaPlayer
            )
            {
                _onCompletion();
            }
        }

        private class VideoErrorListener : Object, MediaPlayer.IOnErrorListener
        {
            private readonly Context _videoContext;            
            private readonly string _videoPath;
            private readonly Func<int, string> _getString;

            public VideoErrorListener
            (
                Context videoContext,
                string videoPath,
                Func<int, string> getString
            )
            {
                _videoContext = videoContext;
                _videoPath = videoPath;
                _getString = getString;
            }

            bool MediaPlayer.IOnErrorListener.OnError
            (
                MediaPlayer mediaPlayer,
                MediaError what,
                int extra
            )
            {
                string errorFormat = _getString(Resource.String.VideoErrorFormat);
                string errorMessage = string.Format(errorFormat,
                                                   _videoPath);
                Toast.MakeText
                (
                    _videoContext,
                    errorMessage,
                    ToastLength.Long
                )
                .Show();
                
                return false;
            }
        }        
    }
}

