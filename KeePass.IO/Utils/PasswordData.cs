using System;
using System.Security.Cryptography;
using System.Text;
using KeePass.IO.Data;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

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
            var aesEcb = SymmetricKeyAlgorithmProvider
                .OpenAlgorithm(SymmetricAlgorithmNames.AesEcb);
            var key = aesEcb.CreateSymmetricKey(
                CryptographicBuffer.CreateFromByteArray(transformSeed));

            IBuffer blockBuffer = CryptographicBuffer
                .CreateFromByteArray(_hash);

            for (var i = 0; i < rounds; i++)
            {
                blockBuffer = CryptographicEngine
                    .Encrypt(key, blockBuffer, null);
            }

            byte[] block = null;
            CryptographicBuffer.CopyToByteArray(blockBuffer, out block);

            return BufferEx.GetHash(block);
        }
    }
}