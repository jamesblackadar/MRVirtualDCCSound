namespace MRVirtualDCCSound.Core
{
    public interface IMobileObservable : IObservable<MobileEventArgs>
    {
        new IDisposable Subscribe(IObserver<MobileEventArgs> observer);
        Task Test(MobileEventArgs arg);
        Task TestLoop();
    }
}