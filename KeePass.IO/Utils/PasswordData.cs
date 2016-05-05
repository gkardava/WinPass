using System;
using System.Text;
using KeePass.IO.Data;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Engines;

namespace KeePass.IO.Utils
{
    internal class PasswordData
    {
        private readonly byte[] _hash;

        public PasswordData(string password, byte[] keyFile)
        {
            if (!string.IsNullOrEmpty(password))
            {
                var utf8 = Encoding.UTF8.GetBytes(password);
                _hash = BufferEx.GetHash(utf8);
            }

            if (keyFile != null)
            {
                if (_hash != null)
                {
                    var current = _hash.Length;
                    Array.Resize(ref _hash, current + keyFile.Length);
                    Array.Copy(keyFile, 0, _hash,
                        current, keyFile.Length);
                }
                else
                    _hash = keyFile;
            }

            if (_hash == null)
            {
                throw new InvalidOperationException(
                    "At least password or key file must be provided");
            }

            _hash = BufferEx.GetHash(_hash);
        }

        public byte[] TransformKey(byte[] transformSeed, int rounds)
        {
            var block = BufferEx.Clone(_hash);

            var cipher = new AesEngine();
            cipher.Init(true, new KeyParameter(transformSeed));

            var aesEcb = SymmetricKeyAlgorithmProvider
                .OpenAlgorithm(SymmetricAlgorithmNames.AesEcb);
            var key = aesEcb.CreateSymmetricKey(
                CryptographicBuffer.CreateFromByteArray(transformSeed));

            for (int i = 0; i < rounds; i++)
            {
                cipher.ProcessBlock(block, 0, block, 0);
                cipher.ProcessBlock(block, 16, block, 16);
            }

            return BufferEx.GetHash(block);
        }
    }
}