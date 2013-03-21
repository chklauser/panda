namespace Panda.Core.Blocks
{
    public interface IFileContinuationBlock : IOffsetListBlock
    {
        void ReplaceOffsets(int[] offsets);
    }
}