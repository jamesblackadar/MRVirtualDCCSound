namespace MRVirtualDCCSound.Core
{
    public interface IMRVirtualDCCSoundSingleton
    {
        IMobileObservable Iprovider { get; set; }
        MobileCollectionObserver Observer { get; set; }
        List<string> SoundFolders { get; set; }
    }
}