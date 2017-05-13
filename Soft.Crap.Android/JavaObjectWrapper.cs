using Object = Java.Lang.Object;

namespace Soft.Crap.Android
{
    public class JavaObjectWrapper<T> : Object
    {
        public JavaObjectWrapper
        (
            T wrappedObject
        )
        : base()
        {
            WrappedObject = wrappedObject;
        }

        public T WrappedObject { get; }
    }
}