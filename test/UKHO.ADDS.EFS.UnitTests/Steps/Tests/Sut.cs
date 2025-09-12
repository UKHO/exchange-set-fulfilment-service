namespace UKHO.ADDS.EFS.UnitTests.Steps.Tests
{
    public class Sut
    {
        public bool IsReady { get; private set; }

        public void DoSomething()
        {
            IsReady = true;
        }

        public async Task DoSomethingAsync()
        {
            await Task.Delay(10);
            IsReady = true;
        }
    }
}
