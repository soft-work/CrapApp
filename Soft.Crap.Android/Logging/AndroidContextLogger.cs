using Android.App;
using Android.Util;

using Soft.Crap.Logging;

namespace Soft.Crap.Android.Logging
{
    internal class AndroidContextLogger : AbstractContextLogger
    {
        private readonly string _logLabel;

        public AndroidContextLogger()
        {
            _logLabel = Application.Context.GetString(Resource.String.ApplicationName);
        }

        protected override string LogLabel
        {
            get { return _logLabel; }
        }

        protected override void LogDebug
        (
            string logTag,
            string logFormat,
            params object[] logArguments
        )
        {
            Log.Debug(logTag,
                      logFormat,
                      logArguments);
        }

        protected override void LogError
        (
            string logTag,
            string logError
        )
        {
            Log.Error(logTag, logError);
        }
    }
}

