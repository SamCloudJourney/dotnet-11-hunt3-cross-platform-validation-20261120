#pragma warning disable SYSLIB5006
using System.Security.Cryptography;
using System.Text;

CompositeMLDsaAlgorithm[] algorithms =
[
    CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256,
    CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss,
];

int tested = 0;
bool passed = true;
foreach (CompositeMLDsaAlgorithm algorithm in algorithms)
{
    if (!CompositeMLDsa.IsAlgorithmSupported(algorithm))
    {
        continue;
    }

    tested++;
    using CompositeMLDsa key = CompositeMLDsa.GenerateKey(algorithm);
    using CompositeMLDsa otherKey = CompositeMLDsa.GenerateKey(algorithm);
    byte[] message = Encoding.UTF8.GetBytes("controlled composite-signature test");
    byte[] context = Encoding.UTF8.GetBytes("owned-lab");
    byte[] signature = key.SignData(message, context);

    passed &= key.VerifyData(message, signature, context);

    byte[] altered = (byte[])signature.Clone();
    altered[0] ^= 1;
    passed &= !key.VerifyData(message, altered, context);

    altered = (byte[])signature.Clone();
    altered[^1] ^= 1;
    passed &= !key.VerifyData(message, altered, context);

    byte[] changedMessage = (byte[])message.Clone();
    changedMessage[0] ^= 1;
    passed &= !key.VerifyData(changedMessage, signature, context);

    byte[] changedContext = (byte[])context.Clone();
    changedContext[0] ^= 1;
    passed &= !key.VerifyData(message, signature, changedContext);
    passed &= !otherKey.VerifyData(message, signature, context);
}

Console.WriteLine($"supported_sets_tested={tested}");
Console.WriteLine(passed && tested > 0 ? "conformance=pass" : "conformance=fail");
Environment.ExitCode = passed && tested > 0 ? 0 : 1;
