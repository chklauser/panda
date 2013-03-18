using System;
using System.Runtime.Serialization;

namespace Panda
{
    /// <summary>
    /// Common exception base type for exceptions specific to Panda VFS.
    /// </summary>
    [Serializable]
    public class PandaException : Exception
    {
        public PandaException()
        {
        }

        public PandaException(string message) : base(message)
        {
        }

        public PandaException(string message, Exception inner) : base(message, inner)
        {
        }

        protected PandaException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}