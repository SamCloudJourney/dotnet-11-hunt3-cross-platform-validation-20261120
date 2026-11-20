#pragma warning disable SYSLIB5006
using System.Security.Cryptography;

Console.WriteLine($"supported={CompositeMLDsa.IsSupported}");
Console.WriteLine($"ecdsaP256={CompositeMLDsa.IsAlgorithmSupported(CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256)}");
Console.WriteLine($"rsaPss={CompositeMLDsa.IsAlgorithmSupported(CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss)}");
