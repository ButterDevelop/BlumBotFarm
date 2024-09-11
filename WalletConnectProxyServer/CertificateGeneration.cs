using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace WalletConnectProxyServer
{
    public static class CertificateGenerator
    {
        public static X509Certificate2 GenerateRootCertificate(string subjectName)
        {
            var distinguishedName = new X500DistinguishedName($"CN={subjectName}");

            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(
                distinguishedName,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

                // Добавляем Basic Constraints, указывая, что это CA-сертификат
                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));

                // Добавляем ключевое использование
                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));

                // Добавляем расширение Authority Key Identifier
                request.CertificateExtensions.Add(
                    new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

                // Генерируем самоподписанный сертификат (корневой сертификат)
                var rootCert = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(10));

                return new X509Certificate2(rootCert.Export(X509ContentType.Pfx, "ANIME_NA_AVE"), "ANIME_NA_AVE", 
                    X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            }
        }

        public static void ExportGeneratedRootCertificate()
        {
            X509Certificate2 rootCert = GenerateRootCertificate("MyRootCA");
            File.WriteAllBytes("myrootca.pfx", rootCert.Export(X509ContentType.Pfx, "ANIME_NA_AVE"));

        }
    }
}
