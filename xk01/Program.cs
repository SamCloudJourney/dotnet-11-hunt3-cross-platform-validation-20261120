using System.Security.Cryptography;

try
{
    using var alice = X25519DiffieHellman.GenerateKey();
    using var bob = X25519DiffieHellman.GenerateKey();
    byte[] a = alice.DeriveRawSecretAgreement(bob);
    byte[] b = bob.DeriveRawSecretAgreement(alice);
    bool valid = a.AsSpan().SequenceEqual(b) && a.Any(x => x != 0);
    CryptographicOperations.ZeroMemory(a);
    CryptographicOperations.ZeroMemory(b);
    Console.WriteLine($"valid_equal_nonzero={valid}");

    bool zeroRejected = Probe(alice, new byte[32], "zero");
    byte[] one = new byte[32];
    one[0] = 1;
    bool oneRejected = Probe(alice, one, "one");
    bool shortRejected = Probe(alice, new byte[31], "short");
    return valid && zeroRejected && oneRejected && shortRejected ? 0 : 2;
}
catch (PlatformNotSupportedException)
{
    Console.WriteLine("platform_unsupported");
    return 3;
}

static bool Probe(X25519DiffieHellman alice, byte[] bytes, string label)
{
    try
    {
        using var peer = X25519DiffieHellman.ImportPublicKey(bytes);
        byte[] secret = alice.DeriveRawSecretAgreement(peer);
        bool allZero = secret.All(x => x == 0);
        CryptographicOperations.ZeroMemory(secret);
        Console.WriteLine($"{label}=accepted allzero={allZero}");
        return false;
    }
    catch (CryptographicException ex)
    {
        Console.WriteLine($"{label}=rejected {ex.GetType().Name}");
        return true;
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"{label}=rejected {ex.GetType().Name}");
        return true;
    }
}
