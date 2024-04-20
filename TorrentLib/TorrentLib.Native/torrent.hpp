#pragma once
#include <libtorrent/torrent.hpp>
#include <optional>

using torrent_info = lt::torrent_info;
using torrent_add_params = lt::add_torrent_params;

struct torrent_status
{
	int errc;
	int error_file;
	std::int64_t total_download;
	std::int64_t total_upload;
	std::int64_t total_payload_download;
	std::int64_t total_payload_upload;
	std::int64_t total_failed_bytes;
	std::int64_t total_redundant_bytes;
	std::int64_t total_done;
	std::int64_t total;
	std::int64_t total_wanted_done;
	std::int64_t total_wanted;
	std::int64_t all_time_upload;
	std::int64_t all_time_download;
	std::time_t added_time;
	std::time_t completed_time;
	std::time_t last_seen_complete;
	float progress;
	int progress_ppm;
	int queue_position{};
	int download_rate;
	int upload_rate;
	int download_payload_rate;
	int upload_payload_rate;
	int num_seeds;
	int num_peers;
	int num_complete;
	int num_incomplete;
	int list_seeds;
	int list_peers;
	int connect_candidates;
	int num_pieces;
	int distributed_full_copies;
	int distributed_fraction;
	float distributed_copies;
	int block_size;
	int num_uploads;
	int num_connections;
	int uploads_limit;
	int connections_limit;
	int up_bandwidth_queue;
	int down_bandwidth_queue;
	int seed_rank;
	int state;
	bool need_save_resume;
	bool is_seeding;
	bool is_finished;
	bool has_metadata;
	bool has_incoming;
	bool moving_storage;
	bool announcing_to_trackers;
	bool announcing_to_lsd;
	bool announcing_to_dht;
	int64_t last_upload;
	int64_t last_download;
	int64_t active_duration;
	int64_t finished_duration;
	int64_t seeding_duration;
	uint32_t flags{};

	torrent_status(const lt::torrent_status& stat);
};

struct torrent
{
	lt::torrent_handle handle;
};
