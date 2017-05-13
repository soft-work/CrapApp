using System;

namespace Soft.Crap.Exceptions
{
    public class CorruptObjectException : Exception
    {
        public CorruptObjectException
        (
            string objectDescription
        )
        : base(objectDescription) { }        
    }    
}
