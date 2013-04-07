using System;
using System.Runtime.Serialization;

namespace Panda.Core
{
    [Serializable]
    public class PathAlreadyExistsException : PandaException
    {
        public PathAlreadyExistsException()
        {
        }

        public PathAlreadyExistsException(string message) : base(message)
        {
        }

        public PathAlreadyExistsException(string message, Exception inner) : base(message, inner)
        {
        }

        protected PathAlreadyExistsException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}