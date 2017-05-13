using System.Collections.Generic;
using System.Linq;

using Array = Java.Lang.Reflect.Array;
using Object = Java.Lang.Object;

namespace Soft.Crap.Android
{
    public static class JavaObjectExtensions
    {
        public static T[] ToArray<T>
        (
            this Object input
        )
        where T : Object
        {
            int length = Array.GetLength(input);
            var list = new List<Object>(length);

            for (int index = 0; index < length; index++)
            {
                Object item = Array.Get(input, index);
                list.Add(item);
            }

            T[] output = list.Cast<T>().ToArray();

            return output;
        }
    }
}