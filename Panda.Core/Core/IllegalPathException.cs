using System;
using System.Runtime.Serialization;
namespace Panda.Core
{
    /// <summary>
    /// Illegal path exception.
    /// </summary>
    [Serializable]
    public class IllegalPathException : PandaException
    {
        public IllegalPathException()
        {
        }

        public IllegalPathException(string message)
            : base(message)
        {
        }

        public IllegalPathException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected IllegalPathException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}