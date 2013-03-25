using System;
using System.Runtime.Serialization;


namespace Panda.Core
{
    [Serializable]
    public class PathNotFoundException : PandaException
    {
        public PathNotFoundException() { }
        public PathNotFoundException(string message) : base(message) { }
        public PathNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected PathNotFoundException(
          SerializationInfo info,
          StreamingContext context)
            : base(info, context) { }
    }
}
