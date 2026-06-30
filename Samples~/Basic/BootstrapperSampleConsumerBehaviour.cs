namespace SWeetJelly.Samples.Bootstrapper
{
    using SWeetJelly.Bootstrapper.Framework.Unity;
    using UnityEngine;

    public sealed class BootstrapperSampleConsumerBehaviour : MonoBehaviour
    {
        private string status = "Waiting for Bootstrapper...";

        private void OnEnable()
        {
            BootstrapperBehaviour.Bootstrapped += OnBootstrapped;
            ResolveIfReady();
        }

        private void OnDisable()
        {
            BootstrapperBehaviour.Bootstrapped -= OnBootstrapped;
        }

        private void OnBootstrapped(object sender, System.EventArgs args)
        {
            ResolveIfReady();
        }

        private void ResolveIfReady()
        {
            if (!BootstrapperBehaviour.IsBootstrapped || BootstrapperBehaviour.Services == null)
                return;

            if (!BootstrapperBehaviour.Services.TryResolve<IBootstrapperSampleGreeter>(out var greeter))
            {
                status = "Sample greeter service is not registered.";
                Debug.LogWarning(status, this);
                return;
            }

            status = greeter.GetGreeting();
            Debug.Log(status, this);
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(16f, 16f, 520f, 24f), status);
        }
    }
}
