namespace Panda.Core.Blocks
{
    public interface IContinuationBlock : IBlock
    {
        /// <summary>
        /// Address of ContinuationBlock.
        /// </summary>
        BlockOffset? ContinuationBlock { get; set; } 
    }
}