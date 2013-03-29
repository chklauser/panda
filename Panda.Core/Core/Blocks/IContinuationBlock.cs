namespace Panda.Core.Blocks
{
    public interface IContinuationBlock : IBlock
    {
        /// <summary>
        /// Address of ContinuationBlock.
        /// </summary>
        BlockOffset? ContinuationBlockOffset { get; set; } 
    }
}