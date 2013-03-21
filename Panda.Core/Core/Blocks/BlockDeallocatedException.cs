using System;
using System.Runtime.Serialization;

namespace Panda.Core.Blocks
{
    [Serializable]
    public class BlockDeallocatedException : PandaException
    {
        public BlockDeallocatedException()
        {
        }

        public BlockDeallocatedException(string message) : base(message)
        {
        }

        public BlockDeallocatedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected BlockDeallocatedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}