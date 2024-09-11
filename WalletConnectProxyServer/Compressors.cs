using System.IO.Compression;

namespace WalletConnectProxyServer
{
    public class Compressors
    {
        public static byte[] CompressGzip(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                gzipStream.Write(data, 0, data.Length);
                gzipStream.Close(); // Закрытие потока для завершения сжатия
                return compressedStream.ToArray();
            }
        }

        public static byte[] CompressDeflate(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Compress))
            {
                deflateStream.Write(data, 0, data.Length);
                deflateStream.Close(); // Закрытие потока для завершения сжатия
                return compressedStream.ToArray();
            }
        }

        public static byte[] CompressBrotli(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var brotliStream = new BrotliStream(compressedStream, CompressionMode.Compress))
            {
                brotliStream.Write(data, 0, data.Length);
                brotliStream.Close(); // Закрытие потока для завершения сжатия
                return compressedStream.ToArray();
            }
        }
    }
}
