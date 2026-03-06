namespace Nexus.Network
{
    public class PayPalSettings
    {
        public string ClientId { get; set; }
        public string Secret { get; set; }
        public string SandboxBaseUrl { get; set; }
        public string SandboxTokenUrl { get; set; }
        public string SandboxCreateOrderUrl { get; set; }
        public string SandboxCaptureOrderUrl { get; set; }
        public string SandboxRefundCaptureUrl { get; set; }
    }
}
