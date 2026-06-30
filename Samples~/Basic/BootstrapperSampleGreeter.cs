namespace SWeetJelly.Samples.Bootstrapper
{
    public sealed class BootstrapperSampleGreeter : IBootstrapperSampleGreeter
    {
        private readonly string message;

        public BootstrapperSampleGreeter(string message)
        {
            this.message = string.IsNullOrWhiteSpace(message)
                ? "Bootstrapper sample service is ready."
                : message;
        }

        public string GetGreeting()
        {
            return message;
        }
    }
}
