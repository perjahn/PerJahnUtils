using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace gethash
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: gethash <filename>");
                return 1;
            }

            var filename = args[0];

            var filehash = GetHash(filename);

            Console.WriteLine($"Hash: {filehash}");

            return 0;
        }

        static string GetHash(string filename)
        {
            Console.WriteLine($"Reading: '{filename}'");
            using var stream = File.OpenRead(filename);

            var blocksize = 64 * 1024 * 1024;

            var buf = new byte[blocksize];

            using var sha256Hash = SHA256.Create();

            for (long offset = 0; offset < stream.Length; offset += blocksize)
            {
                if (stream.Length < offset + blocksize)
                {
                    blocksize = (int)(stream.Length - offset);
                }

                Console.Write('.');
                var bytesRead = stream.Read(buf, 0, blocksize);
                if (bytesRead < blocksize)
                {
                    Console.WriteLine($"Tried to read {blocksize} bytes, got {bytesRead} bytes.");
                }
                _ = sha256Hash.TransformBlock(buf, 0, bytesRead, null, 0);
            }
            Console.WriteLine();

            _ = sha256Hash.TransformFinalBlock(buf, 0, 0);

            StringBuilder builder = new();

            for (var i = 0; i < sha256Hash.Hash.Length; i++)
            {
                _ = builder.Append(sha256Hash.Hash[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
