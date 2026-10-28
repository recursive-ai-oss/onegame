namespace OneGame.Net.Tests;

/// <summary>Plaintext settings for the tests that exercise the reliability layer and the sync stack, not the encryption.</summary>
internal static class Plain
{
    /// <summary>Fresh Insecure options, as the transports now refuse to open without a security choice.</summary>
    public static UdpOptions Udp => new() { Security = NetSecurity.Insecure };
}
