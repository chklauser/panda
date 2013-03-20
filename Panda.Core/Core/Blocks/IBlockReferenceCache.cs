namespace Panda.Core.Blocks
{
    public interface IBlockReferenceCache
    {
        void RegisterAccess(Block reference);
    }
}