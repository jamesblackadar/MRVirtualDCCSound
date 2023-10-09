namespace MRVirtualDCCSound.Core
{
    public interface IStorageRepository<T> where T : class, new()
    {
        string FilePath { get; }

        T Read();
        void Write(object value);
    }
}