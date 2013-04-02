using System;
using System.Runtime.Serialization;
namespace Panda.Core
{
    /// <summary>
    /// Illegal path exception.
    /// </summary>
    [Serializable]
    public class IllegalNodeNameException : PandaException
    {
        public IllegalNodeNameException()
        {
        }

        public IllegalNodeNameException(string message)
            : base(message)
        {
        }

        public IllegalNodeNameException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected IllegalNodeNameException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}