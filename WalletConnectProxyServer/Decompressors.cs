using System.Diagnostics;
using System.IO.Compression;

namespace WalletConnectProxyServer
{
    public class Decompressors
    {
        public static byte[] DecompressGzip(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var decompressedStream = new MemoryStream())
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            {
                gzipStream.CopyTo(decompressedStream);
                return decompressedStream.ToArray();
            }
        }

        public static byte[] DecompressDeflate(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var decompressedStream = new MemoryStream())
            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
            {
                deflateStream.CopyTo(decompressedStream);
                return decompressedStream.ToArray();
            }
        }

        public static byte[] DecompressBrotli(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var decompressedStream = new MemoryStream())
            using (var brotliStream = new BrotliStream(compressedStream, CompressionMode.Decompress))
            {
                brotliStream.CopyTo(decompressedStream);
                return decompressedStream.ToArray();
            }
        }
    }
}
