namespace Panda.Core.Blocks
{
    public interface IContinuationBlock : IBlock
    {
        BlockOffset? ContinuationBlock { get; set; } 
    }
}