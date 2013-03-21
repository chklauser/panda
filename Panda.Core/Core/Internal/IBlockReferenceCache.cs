namespace Panda.Core.Blocks
{
    public interface IBlockReferenceCache
    {
        void RegisterAccess(IBlock reference);
        void EvictEarly(IBlock reference);
    }
}