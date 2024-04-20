#pragma once
#include "torrent.hpp"
#include <libtorrent/session.hpp>
#include <shared_mutex>

using torrent_status_updated_callback_t = void(*)(int torrent_id);
using session_poll_suggested_callback_t = void(*)();

struct session_callbacks
{
	void(*read_piece)(int err, int torrent_id, int piece_index, char* buffer, int size);
	void(*save_resume_data)(int err, int torrent_id, const torrent_add_params* params);
	void(*dht_put_item)(char* pk, char* sig, char* salt, int salt_size, int64_t seq, int num_success);
	void(*dht_get_item)(char* pk, char* sig, char* salt, int salt_size, int64_t seq, char* data, int data_size); ;
	void(*dht_bootstrap)();
	void(*torrent_status_updated)(int torrent_id, torrent_status* torrent_status);
	void(*torrent_removed)(int err, const char* infohash, bool deleted);
	void(*torrent_file_error)(int torrent_id, int err, const char* filename, const char* message);
	void(*peer_activty)(int type, int torrent_id, const char* ip, int port, int direction, int err, int close_reason);
	void(*log)(int type, const char* message);
};

struct session
{
public:
	void init(const lt::bdecode_node& settings_dict);
	void poll(int ms);

	int add_torrent(const torrent_add_params* atp, int* torrent_id);
	int remove_torrent(int id, bool delete_files);
	torrent* get_torrent(int id);

	int torrent_read_piece(int torrent_id, int piece_index);
	int torrent_save_resume_data(int torrent_id);

	std::unique_ptr<lt::session> m_session;
	session_callbacks m_callbacks;

	void debug_log(const std::string& message);

private:
	void handle_alert(lt::alert* p);
	void finish_save_resume_data(int torrent_id, int err, const torrent_add_params* params);
	void finish_read_piece(int torrent_id, int err, int piece_index, char* buffer, int size);

	

	std::shared_mutex m_mutex;
	std::unordered_map<int32_t, std::unique_ptr<torrent>> m_handle_map;
	bool m_logging_enabled = false;
};
