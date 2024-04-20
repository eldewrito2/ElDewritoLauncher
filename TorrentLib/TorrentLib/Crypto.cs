namespace TorrentLib.Crypto
{
    public static class Ed25519
    {
        public static void CreateKeyPair(byte[] seed, out byte[] pk, out byte[] sk)
        {
            pk = new byte[32];
            sk = new byte[64];
            int result = NativeApi.ed25519_create_keypair(seed, seed.Length, pk, sk);
            if (result != 0)
                throw new TorrentException(result);
        }

        public static byte[] Sign(byte[] msg, byte[] pk, byte[] sk)
        {
            var sig = new byte[64];
            int result = NativeApi.ed25519_sign(msg, msg.Length, pk, sk, sig);
            if (result != 0)
                throw new TorrentException(result);
            return sig;
        }

        public static bool Verify(byte[] msg, byte[] pk, byte[] sig)
        {
            bool verified = false;
            int result = NativeApi.ed25519_verify(ref verified, msg, msg.Length, pk, sig);
            if (result != 0)
                throw new TorrentException(result);
            return verified;
        }
    }
}
