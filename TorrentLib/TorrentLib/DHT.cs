using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TorrentLib
{
    using static NativeApi;

    public class DHT
    {
        private readonly Session _session;
        private readonly List<DHTGetRequests> _searchRequests;
        private readonly List<DHTPutRequest> _storeRequests;
        private readonly TaskCompletionSource _dhtBootstrapped;

        internal DHT(Session session)
        {
            _session = session;
            _searchRequests = new List<DHTGetRequests>();
            _storeRequests = new List<DHTPutRequest>();
            _dhtBootstrapped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public static void GenerateKeyPair(byte[] seed, out byte[] publicKey, out byte[] secretKey)
        {
            publicKey = new byte[32];
            secretKey = new byte[64];
            dht_gen_keypair(seed, seed.Length, publicKey, secretKey);
        }

        public async Task<DHTItem?> GetLatestAsync(byte[] key, byte[] salt, TimeSpan timeout, CancellationToken cancelToken = default)
        {
            // wait for the dht to bootstrapped
            await _dhtBootstrapped.Task.WaitAsync(cancelToken);

            var results = new List<DHTItem>();
            await foreach (var result in SearchAsync(key, salt, timeout, cancelToken))
                results.Add(result);

            return results.OrderByDescending(x => x.Sequence).FirstOrDefault();
        }

        public async IAsyncEnumerable<DHTItem> SearchAsync(byte[] key, byte[] salt, TimeSpan timeout, [EnumeratorCancellation]CancellationToken cancelToken = default)
        {
            // wait for the dht to bootstrapped
            await _dhtBootstrapped.Task.WaitAsync(cancelToken);

            var request = new DHTGetRequests(key, salt);
            lock(_searchRequests)
                _searchRequests.Add(request);

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
            cts.CancelAfter(timeout);

            try
            {
                GetMutableItem(key, salt);

                while (true)
                {
                    try  { await request.Signal.WaitAsync(cts.Token).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }

                    while (request.Results.TryDequeue(out DHTItem result))
                        yield return result;
                }
            }
            finally
            {
                lock (_searchRequests)
                    _searchRequests.Remove(request);
            }
        }

        public Task<DHTPutResult> AnnounceAsync(byte[] publicKey, byte[] salt, byte[] signature, object value, long sequence, CancellationToken cancelToken = default)
        {
            byte[] data = Bencode.Encode(value);
            if (data.Length > 1000)
                throw new ArgumentOutOfRangeException(nameof(data), "data must be 1000 bytes or less");

            return PutMutableItemAsync(publicKey, salt, () =>
            {
                unsafe
                {
                    dht_announce_mutable_item(_session.Handle, publicKey, salt, salt.Length, signature, data, data.Length, sequence);
                }
            }, cancelToken);
        }

        public Task<DHTPutResult> StoreAsync(byte[] publicKey, byte[] secretKey, byte[] salt, object value, CancellationToken cancelToken = default)
        {
            byte[] data = Bencode.Encode(value);
            if (data.Length > 1000)
                throw new ArgumentOutOfRangeException(nameof(data), "data must be 1000 bytes or less");

            return PutMutableItemAsync(publicKey, salt,  () =>
            {
                unsafe
                {
                    dht_put_mutable_item(_session.Handle, publicKey, secretKey, salt, salt.Length, data, data.Length);
                }
            }, cancelToken);
        }

        private async Task<DHTPutResult> PutMutableItemAsync(byte[] publicKey, byte[] salt, Action putMethod, CancellationToken cancelToken = default)
        {
            // wait for the dht to bootstrapped
            await _dhtBootstrapped.Task;

            var request = new DHTPutRequest(publicKey, salt);
            lock (_storeRequests)
                _storeRequests.Add(request);

            try
            {
                int attempts = 0;
                while (true)
                {
                    putMethod();

                    var result = await request.CompletionSource.Task;

                    cancelToken.ThrowIfCancellationRequested();

                    if (result.SuccessCount > 0)
                        return result;

                    if(attempts > 24)
                    {
                        throw new TaskCanceledException($"DHT put failed, attempts: {attempts}");
                    }

                    int backoff = Math.Min((++attempts) * 100, 5000);

                    await Task.Delay(TimeSpan.FromMilliseconds(backoff));
                }
            }
            finally
            {
                lock (_storeRequests)
                    _storeRequests.Remove(request);
            }
        }

        private unsafe void GetMutableItem(byte[] key, byte[] salt)
        {
            fixed (byte* p_pk = key)
            fixed (byte* p_salt = salt)
                dht_get_mutable_item(_session.Handle, p_pk, p_salt, salt.Length);
        }


        private void OnDHTGetItemInternal(DHTItem result)
        {
            // check for an empty string that indicates no result, not really sure the best way to handle this yet.
            if (result.Value is string str && str.Length == 0)
                return;

            List<DHTGetRequests> requests;
            lock (_searchRequests)
                requests = _searchRequests.Where(x => x.Key.SequenceEqual(result.Key) && x.Salt.SequenceEqual(result.Salt)).ToList();

            foreach (var request in requests)
            {
                request.Results.Enqueue(result);
                request.Signal.Release();
            }
        }

        private void OnDHTPutItemInternal(DHTPutResult result)
        {
            List<DHTPutRequest> requests;
            lock (_storeRequests)
                requests = _storeRequests.Where(x => x.Key.SequenceEqual(result.Key) && x.Salt.SequenceEqual(result.Salt)).ToList();

            foreach (var request in requests)
                request.CompletionSource.TrySetResult(result);
        }

        internal unsafe void OnDHTGetItem(byte* pk, byte* sig, byte* salt, int salt_size, long seq, byte* data, int data_size)
        {
            // TODO: clean this up
            byte[] dataArr = new byte[data_size];
            Marshal.Copy((IntPtr)data, dataArr, 0, data_size);
            byte[] saltArr = new byte[salt_size];
            Marshal.Copy((IntPtr)salt, saltArr, 0, salt_size);
            byte[] pkArr = new byte[32];
            Marshal.Copy((IntPtr)pk, pkArr, 0, pkArr.Length);
            byte[] sigArr = new byte[64];
            Marshal.Copy((IntPtr)sig, sigArr, 0, sigArr.Length);

            OnDHTGetItemInternal(new DHTItem()
            {
                Value = Bencode.Decode(dataArr),
                Salt = saltArr,
                Key = pkArr,
                Sequence = seq,
                Signature = sigArr
            });
        }

        internal unsafe void OnDHTPutItem(byte* pk, byte* sig, byte* salt, int salt_size, long seq, int num_success)
        {
            // TODO: clean this up
            byte[] saltArr = new byte[salt_size];
            Marshal.Copy((IntPtr)salt, saltArr, 0, salt_size);
            byte[] pkArr = new byte[32];
            Marshal.Copy((IntPtr)pk, pkArr, 0, pkArr.Length);
            byte[] sigArr = new byte[64];
            Marshal.Copy((IntPtr)sig, sigArr, 0, sigArr.Length);

            OnDHTPutItemInternal(new DHTPutResult()
            {
                Salt = saltArr,
                Key = pkArr,
                Sequence = seq,
                Signature = sigArr,
                SuccessCount = num_success
            });
        }

        internal void OnDHTBootstrapped()
        {
            _dhtBootstrapped.TrySetResult();
        }

        class DHTGetRequests
        {
            public readonly byte[] Key;
            public readonly byte[] Salt;
            public readonly ConcurrentQueue<DHTItem> Results;
            public readonly SemaphoreSlim Signal;

            public DHTGetRequests(byte[] key, byte[] salt)
            {
                Key = key;
                Salt = salt;
                Results = new ConcurrentQueue<DHTItem>();
                Signal = new SemaphoreSlim(0);
            }
        }

        class DHTPutRequest
        {
            public readonly byte[] Key;
            public readonly byte[] Salt;
            public readonly TaskCompletionSource<DHTPutResult> CompletionSource;

            public DHTPutRequest(byte[] key, byte[] salt)
            {
                Key = key;
                Salt = salt;
                CompletionSource = new TaskCompletionSource<DHTPutResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }


        public struct DHTItem
        {
            public byte[] Key;
            public byte[] Signature;
            public byte[] Salt;
            public long Sequence;
            public object? Value;
        }

        public struct DHTPutResult
        {
            public byte[] Key;
            public byte[] Signature;
            public byte[] Salt;
            public long Sequence;
            public int SuccessCount;
        }
    }
}
