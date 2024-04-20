namespace TorrentLib
{
    public static class TorrentStatusTracker
    {
        /// <summary>
        /// Tracks the status of a torrent over time.
        /// </summary>
        /// <remarks>
        /// <paramref name="callback"/> will be called when the status of the torrent has changed until one of four conditions:
        /// <br />
        /// 1. The torrent goes into an error state - throws <see cref="TorrentException"/>  <br />
        /// 2. The task is cancelled due to the <paramref name="cancelToken"/> being cancelled - throws <see cref="OperationCanceledException"/> <br />
        /// 3. The <paramref name="callback"/> returns true<br />
        /// 4. The <paramref name="callback"/> throws an exception <br />
        /// <br />
        /// Don't rely on the callback being called at some regular interval. If the torrent stalls you may not get a callback for some time.
        /// Pass a cancellation token to cancel if needed. You are however guaranteed at least one callback.
        /// <br />
        /// You can avoid the TorrentException being thrown by returning true from the callback when <see cref="TorrentStatus.ErrorCode"/> != 0
        /// </remarks>
        /// <param name="session">The Session</param>
        /// <param name="torrentId">The id of the torrent</param>
        /// <param name="callback">A callback to be called when the torrent status changes. return true to stop tracking.</param>
        /// <param name="cancelToken">A cancel token to cancel the task</param>
        /// <returns>The last observed <see cref="TorrentStatus"/></returns>
        /// <exception cref="TorrentException">Thrown when the torrent goes into an error state</exception>
        /// <exception cref="OperationCanceledException">Thrown when the task is cancelled due to the <paramref name="cancelToken"/> being cancelled</exception>
        public static async Task<TorrentStatus> TrackTorrentStatusAsync(this Session session, int torrentId, Func<TorrentStatus, bool> callback, CancellationToken cancelToken = default)
        {
            var tcs = new TaskCompletionSource<TorrentStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            var ctr = cancelToken.Register(() => tcs.TrySetCanceled());

            callback(session.GetTorrentStatus(torrentId));

            session.TorrentStatusUpdated += OnStatusUpdate;
            try { return await tcs.Task; }
            finally
            {
                session.TorrentStatusUpdated -= OnStatusUpdate;
                ctr.Unregister();
            }

            void OnStatusUpdate(object? sender, TorrentStatusUpdatedEventArgs e)
            {
                if (e.TorrentId != torrentId) return;

                bool finished;
                try
                {
                    finished = callback(e.Status); 
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                    return;
                }

                if(finished)
                    tcs.TrySetResult(e.Status);
                else if (e.Status.ErrorCode != 0)
                    tcs.TrySetException(new TorrentException(e.Status.ErrorCode));
            }
        }
    }
}
