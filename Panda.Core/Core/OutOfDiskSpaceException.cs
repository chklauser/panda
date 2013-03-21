using System;
using System.Runtime.Serialization;

namespace Panda.Core
{
    [Serializable]
    public class OutofDiskSpaceException : PandaException
    {

        public OutofDiskSpaceException()
        {
        }

        public OutofDiskSpaceException(string message) : base(message)
        {
        }

        public OutofDiskSpaceException(string message, Exception inner) : base(message, inner)
        {
        }

        protected OutofDiskSpaceException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}