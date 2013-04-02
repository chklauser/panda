using System;
using System.Runtime.Serialization;
namespace Panda.Core
{
    /// <summary>
    /// Illegal path exception.
    /// </summary>
    [Serializable]
    public class DontTouchRootException : PandaException
    {
        public DontTouchRootException()
        {
        }

        public DontTouchRootException(string message)
            : base(message)
        {
        }

        public DontTouchRootException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected DontTouchRootException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}