using System.Runtime.InteropServices;

namespace TorrentLib
{
    public static unsafe class NativeApi
    {
        const string LibName = "TorrentLib.Native";

#pragma warning disable 0649
        public struct session { };
        public struct torrent_info { };
        public struct torrent_add_params { }


        public struct peer_request
        {
            // The index of the piece in which the range starts.
            public int piece;
            // The byte offset within that piece where the range starts.
            public int start;
            // The size of the range, in bytes.
            public int length;
        }

        public enum torrent_state
        {
            unused,
            checking_files,
            downloading_metadata,
            downloading,
            finished,
            seeding,
            allocating,
            checking_resume_data
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct torrent_status
        {
            public int errc;
            public int error_file;
            public long total_download;
            public long total_upload;
            public long total_payload_download;
            public long total_payload_upload;
            public long total_failed_bytes;
            public long total_redundant_bytes;
            public long total_done;
            public long total;
            public long total_wanted_done;
            public long total_wanted;
            public long all_time_upload;
            public long all_time_download;
            public long added_time;
            public long completed_time;
            public long last_seen_complete;
            public float progress;
            public int progress_ppm;
            public int queue_position;
            public int download_rate;
            public int upload_rate;
            public int download_payload_rate;
            public int upload_payload_rate;
            public int num_seeds;
            public int num_peers;
            public int num_complete;
            public int num_incomplete;
            public int list_seeds;
            public int list_peers;
            public int connect_candidates;
            public int num_pieces;
            public int distributed_full_copies;
            public int distributed_fraction;
            public float distributed_copies;
            public int block_size;
            public int num_uploads;
            public int num_connections;
            public int uploads_limit;
            public int connections_limit;
            public int up_bandwidth_queue;
            public int down_bandwidth_queue;
            public int seed_rank;
            public torrent_state state;
            [MarshalAs(UnmanagedType.I1)] public bool need_save_resume;
            [MarshalAs(UnmanagedType.I1)] public bool is_seeding;
            [MarshalAs(UnmanagedType.I1)] public bool is_finished;
            [MarshalAs(UnmanagedType.I1)] public bool has_metadata;
            [MarshalAs(UnmanagedType.I1)] public bool has_incoming;
            [MarshalAs(UnmanagedType.I1)] public bool moving_storage;
            [MarshalAs(UnmanagedType.I1)] public bool announcing_to_trackers;
            [MarshalAs(UnmanagedType.I1)] public bool announcing_to_lsd;
            [MarshalAs(UnmanagedType.I1)] public bool announcing_to_dht;
            public long last_upload;
            public long last_download;
            public long active_duration;
            public long finished_duration;
            public long seeding_duration;
            public uint flags;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct session_callbacks
        {
            public delegate void read_piece_callback_t(int err, int torrent_id, int piece_index, byte* buffer, int size);
            public delegate void resume_data_callback_t(int err, int torrent_id, torrent_add_params* atp);
            public delegate void dht_put_item_callback_t(byte* pk, byte* sig, byte* salt, int salt_size, long seq, int num_success);
            public delegate void dht_get_item_callback_t(byte* pk, byte* sig, byte* salt, int salt_size, long seq, byte* data, int data_size);
            public delegate void dht_bootstrap_callback_t();
            public delegate void torrent_status_update_callback_t(int torrent_id, torrent_status* status);
            public delegate void torrent_removed_callback_t(int err, byte* infohash, bool deleted);
            public delegate void torrent_file_error_callback_t(
                int torrent_id,
                int err,
                [MarshalAs(UnmanagedType.LPUTF8Str)] string filename,
                [MarshalAs(UnmanagedType.LPUTF8Str)] string message);
            public delegate void peer_activty_callback_t(int type, int torrent_id, [MarshalAs(UnmanagedType.LPUTF8Str)] string ip, int port, int direction, int err, int close_reason);
            public delegate void log_callback_t(int type, [MarshalAs(UnmanagedType.LPUTF8Str)] string message);

            public read_piece_callback_t read_piece;
            public resume_data_callback_t save_resume_data;
            public dht_put_item_callback_t dht_put_item;
            public dht_get_item_callback_t dht_get_item;
            public dht_bootstrap_callback_t dht_bootstrap;
            public torrent_status_update_callback_t torrent_status_updated;
            public torrent_removed_callback_t torrent_removed;
            public torrent_file_error_callback_t torrent_file_Error;
            public peer_activty_callback_t peer_activity;
            public log_callback_t log;
        }


        public delegate void torrent_info_serialize_callback_t(int err, byte* buffer, int size);
        public delegate void create_torrent_file_callback_t(byte* buffer, int size);
        public delegate void torrent_enumerate_files_callback_t([MarshalAs(UnmanagedType.LPUTF8Str)] string path, long filesize);

        //
        // memory
        //
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern byte* memory_alloc(int size);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void memory_free(byte* ptr);

        //
        // session
        //

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int session_new(out session* p, byte[] settings_buf, int settings_buf_size);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void session_delete(session* p);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int session_add_torrent(session* p, torrent_add_params* atp, out int torrent_id);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int session_remove_torrent(session* p, int torrent_id, bool delete_files);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int session_poll(session* session, int timeout_ms);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int session_post_torrent_updates(session* session);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int session_set_callbacks(session* session, ref session_callbacks callbacks);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int session_find_torrent_by_info_hash(session* session, byte[] infohash);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int session_apply_settings_buf(session* session, byte[] buf, int size);
        
        //
        // torrent info
        //

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_info_new_from_file(out torrent_info* p, string filename);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_info_new_from_buffer(out torrent_info* p, byte[] data, int size);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void torrent_info_delete(torrent_info* p);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_info_get_file_name(torrent_info* p, int index, byte[] buffer, int buffer_size);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_info_get_file_path(torrent_info* p, int index, byte[] buffer, int buffer_size);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long torrent_info_get_file_size(torrent_info* p, int index);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_info_get_piece_size(torrent_info* p);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_info_get_piece_count(torrent_info* p);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_info_map_file(torrent_info* p, int file_index, long file_offset, int size, out peer_request request_out);

        // executes synchronously, however using a callback avoids having to allocate temporary memory for the buffer
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_info_serialize(torrent_info* p, torrent_info_serialize_callback_t callback);

        //
        // torrent add params
        //

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_params_new(out torrent_add_params* p);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void torrent_params_new_from_info(out torrent_add_params* p, torrent_info* info);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_params_new_from_torrent_buffer(out torrent_add_params* p, byte[] buffer, int size);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_params_new_from_magnet(out torrent_add_params* p, string magnet_uri);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_params_new_from_resume_data(out torrent_add_params* p, byte[] data, int size);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void torrent_params_delete(torrent_add_params* p);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void torrent_params_apply(torrent_add_params* p, byte[] data, int data_size);

        //
        // torrent
        //

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern torrent_info* torrent_get_info(session* session, int torrent_id);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_get_status(session* session, int torrent_id, out torrent_status status);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_save_resume_data(session* session, int torrent_id);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_read_piece(session* session, int torrent_id, int piece_index);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_set_piece_deadline(session* session, int torrent_id, int piece_index, int deadline, bool alert_when_available);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_set_piece_priority(session* session, int torrent_id, int piece_index, int priority);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_reset_piece_deadline(session* session, int torrent_id, int piece_index);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_pause(session* session, int torrent_id);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_resume(session* session, int torrent_id);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_clear_error(session* session, int torrent_id);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_force_recheck(session* session, int torrent_id);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_set_upload_limit(session* session, int torrent_id, int limit);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_get_upload_limit(session* session, int torrent_id);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_set_download_limit(session* session, int torrent_id, int limit);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_get_download_limit(session* session, int torrent_id);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_set_flags(session* session, int torrent_id, ulong add_flags, ulong remove_flags);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_set_queue_position(session* session, int torrent_id, int queue_index);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_get_queue_position(session* session, int torrent_id);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_query_pieces(session* session, int torrent_id, int start_piece, int end_piece, uint* result_bitset);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_get_name(session* session, int torrent_id, byte[] buffer, int buffer_size);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_enumerate_files(session* session, int torrent_id, torrent_enumerate_files_callback_t callback);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int make_magnet_uri(session* session, int torrent_id, torrent_info_serialize_callback_t cb);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int make_magnet_uri_adp(session* session, byte[] buffer, int size, torrent_info_serialize_callback_t cb);

        // returns the sha1 info hash (for v2, truncated sha256 hash)
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_get_info_hash(session* session, int torrent_id, byte* buffer);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_get_info_hash_v1(session* session, int torrent_id, byte* buffer);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_get_info_hash_v2(session* session, int torrent_id, byte* buffer);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int torrent_truncate_files(session* session, int torrent_id);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int write_torrent_file(torrent_add_params* atp, torrent_info_serialize_callback_t cb);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int write_resume_data(torrent_add_params* atp, torrent_info_serialize_callback_t cb);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int create_torrent(byte[] buffer, int size, create_torrent_file_callback_t cb);

        //
        // dht
        //

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dht_put_mutable_item(session* session, byte[] pk, byte[] sk, byte[] salt, int salt_size, byte[] data, int data_size);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dht_announce_mutable_item(session* session, byte[] pk, byte[] salt, int salt_size, byte[] sig, byte[] data, int data_size, long sequence);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dht_get_mutable_item(session* session, byte* pk, byte* salt, int salt_size);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dht_gen_keypair(byte[] seed, int seed_size, byte[] pk, byte[] sk);

        //
        // misc
        //

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int format_error_message(int error, byte[]? buffer, int buffer_size);

        //
        // crypto
        //

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ed25519_create_keypair(byte[] seed, int seed_size, byte[] pk, byte[] sk);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ed25519_sign(byte[] msg, int msg_len, byte[] pk, byte[] sk, byte[] sig);
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ed25519_verify(ref bool result, byte[] msg, int msg_len, byte[] pk, byte[] sig);

#pragma warning restore 0649
    }
}
