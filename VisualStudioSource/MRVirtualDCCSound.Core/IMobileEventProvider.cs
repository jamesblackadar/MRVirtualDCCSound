namespace MRVirtualDCCSound.Core
{
    public interface IMobileEventProvider
    {
        SerialConnectionHelper SerialConnectionHelper { get; }

        event EventHandler<MobileEventArgs> MobileEventArgEmit;

        Task Test(MobileEventArgs arg);
        Task TestLoop();
    }
}