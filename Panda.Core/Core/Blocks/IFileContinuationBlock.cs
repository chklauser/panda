namespace Panda.Core.Blocks
{
    public interface IFileContinuationBlock : IOffsetListBlock
    {
        void ReplaceOffsets(BlockOffset[] offsets);

        int TotalOffsetCapacity { get; }

        int RemainingOffsetCapacity { get; }
    }
}