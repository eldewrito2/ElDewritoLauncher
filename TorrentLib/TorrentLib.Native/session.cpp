#include "session.hpp"
#include "torrent.hpp"
#include "error.hpp"
#include <libtorrent/write_resume_data.hpp>
#include <libtorrent/extensions/ut_pex.hpp>
#include <libtorrent/extensions/ut_metadata.hpp>
#include <libtorrent/extensions/smart_ban.hpp>
#include <filesystem>
#include <fstream>
#include <codecvt>
#include "strings.hpp"

static bool valid_setting_index(int index)
{
	return index >= 0 && index < lt::settings_pack::max_bool_setting_internal;
}

static void apply_settings_dict(lt::settings_pack* pack, const lt::bdecode_node& dict)
{
	for (int i = 0; i < dict.dict_size(); i++)
	{
		auto entry = dict.dict_at(i);

		int setting_index = lt::setting_by_name(entry.first);
		if (!valid_setting_index(setting_index))
		{
			std::string setting_name = std::string(entry.first); // for debugging
			assert(!"apply_settings_dict() unknown setting");
			continue;
		}

		if (setting_index < lt::settings_pack::max_string_setting_internal)
		{
			pack->set_str(setting_index, std::string(entry.second.string_value()));
		}
		else if (setting_index < lt::settings_pack::max_int_setting_internal)
		{
			pack->set_int(setting_index, static_cast<int>(entry.second.int_value()));
		}
		else if (setting_index < lt::settings_pack::max_bool_setting_internal)
		{
			pack->set_bool(setting_index, entry.second.int_value() == 1);
		}
	}
}


static lt::session create_session(const lt::bdecode_node& settings_dict)
{
	lt::alert_category_t alertMask = lt::alert::error_notification
		| lt::alert::file_progress_notification
		| lt::alert::ip_block_notification
		| lt::alert::peer_notification
		| lt::alert::performance_warning
		| lt::alert::port_mapping_notification
		| lt::alert::status_notification
		| lt::alert::storage_notification
		| lt::alert::tracker_notification
		| lt::alert::dht_notification
		| lt::alert::dht_operation_notification
		| lt::alert::connect_notification;

	if (settings_dict.dict_find_int_value("use_logging", 0) == 1)
	{
		alertMask = lt::alert_category::all;
	}

	lt::settings_pack p = lt::default_settings();
	p.set_bool(lt::settings_pack::listen_system_port_fallback, false);
	p.set_int(lt::settings_pack::outgoing_port, 0);
	p.set_int(lt::settings_pack::num_outgoing_ports, 0);
	p.set_str(lt::settings_pack::dht_bootstrap_nodes, "router.bittorrent.com:6881,router.utorrent.com:6881,router.bitcomet.com:6881,dht.transmissionbt.com:6881,dht.aelitis.com:6881");
	p.set_bool(lt::settings_pack::enable_dht, true);
	p.set_bool(lt::settings_pack::enable_upnp, true);
	p.set_bool(lt::settings_pack::enable_natpmp, true);
	p.set_bool(lt::settings_pack::enable_lsd, true);
	p.set_int(lt::settings_pack::in_enc_policy, lt::settings_pack::pe_enabled);
	p.set_int(lt::settings_pack::out_enc_policy, lt::settings_pack::pe_enabled);
	p.set_int(lt::settings_pack::allowed_enc_level, lt::settings_pack::pe_both);
	p.set_bool(lt::settings_pack::prefer_rc4, true);
	p.set_int(lt::settings_pack::connections_limit, 200);
	p.set_int(lt::settings_pack::upload_rate_limit, -1);
	p.set_int(lt::settings_pack::download_rate_limit, -1);
	p.set_int(lt::settings_pack::unchoke_slots_limit, 8);
	p.set_int(lt::settings_pack::connection_speed, 20);
	p.set_bool(lt::settings_pack::ignore_limits_on_local_network, true);
	p.set_int(lt::settings_pack::active_seeds, 5);
	p.set_int(lt::settings_pack::active_downloads, 3);
	p.set_int(lt::settings_pack::active_limit, 8);
	p.set_bool(lt::settings_pack::dont_count_slow_torrents, false);
	p.set_int(lt::settings_pack::share_ratio_limit, 200);
	p.set_int(lt::settings_pack::seed_time_ratio_limit, 700);
	p.set_int(lt::settings_pack::seed_time_limit, 180 * 60);
	p.set_int(lt::settings_pack::peer_tos, 0);
	p.set_bool(lt::settings_pack::rate_limit_ip_overhead, true);
#if LIBTORRENT_VERSION_NUM < 2
	p.set_int(lt::settings_pack::cache_size, 512);
	p.set_int(lt::settings_pack::cache_expiry, 60);
#endif
	p.set_bool(lt::settings_pack::auto_manage_prefer_seeds, true);
	p.set_bool(lt::settings_pack::anonymous_mode, false);

	p.set_str(lt::settings_pack::listen_interfaces, "0.0.0.0:42305,[::]:42305");
	p.set_int(lt::settings_pack::alert_queue_size, 10000);
	p.set_int(lt::settings_pack::alert_mask, alertMask);

	// TODO: find a way to avoid this
	p.set_bool(lt::settings_pack::allow_multiple_connections_per_ip, true);

	if(auto dict = settings_dict.dict_find_dict("lt_settings"); dict)
		apply_settings_dict(&p, dict);

	auto session = lt::session(std::move(p));
	session.add_extension(&lt::create_smart_ban_plugin);
	session.add_extension(&lt::create_ut_metadata_plugin);
	session.add_extension(&lt::create_ut_pex_plugin);
	
	return session;
}


void session::init(const lt::bdecode_node& settings_dict)
{
	m_session = std::make_unique<lt::session>(create_session(settings_dict));
	m_logging_enabled = settings_dict.dict_find_int_value("use_logging", 0) == 1;
}

void session::poll(int ms)
{
	if (ms != 0)
		m_session->wait_for_alert(lt::milliseconds(ms));

	std::vector<lt::alert*> alerts;
	m_session->pop_alerts(&alerts);
	for (lt::alert* a : alerts)
		handle_alert(a);
}


int session::add_torrent(const torrent_add_params* atp, int* torrent_id)
{
	lt::error_code ec;
	auto handle = m_session->add_torrent(*atp, ec);
	if (ec)
		return make_error_code(ec);

	std::scoped_lock lock(m_mutex);
	auto t = new torrent{ handle };
	m_handle_map.emplace(t->handle.id(), t);
	*torrent_id = handle.id();
	return 0;
}

int session::remove_torrent(int torrent_id, bool delete_files)
{
	torrent* torrent = get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	lt::remove_flags_t flags{};
	if (delete_files)
		flags |= lt::session_handle::delete_files;

	m_session->remove_torrent(torrent->handle, flags);
	std::scoped_lock lock(m_mutex);
	m_handle_map.erase(torrent_id);
	return 0;
}

torrent* session::get_torrent(int id)
{
	std::shared_lock lock(m_mutex);

	auto it = m_handle_map.find(id);
	if (it == m_handle_map.end())
		return nullptr;

	return it->second.get();
}

int session::torrent_save_resume_data(int torrent_id)
{
	torrent* torrent = get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	torrent->handle.save_resume_data(lt::torrent_handle::save_info_dict);
	return 0;
}

void session::finish_save_resume_data(int torrent_id, int err, const torrent_add_params* params)
{
	if (m_callbacks.save_resume_data)
		m_callbacks.save_resume_data(err, torrent_id, params);
}

int session::torrent_read_piece(int torrent_id, int piece_index)
{
	torrent* torrent = get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	torrent->handle.read_piece(lt::piece_index_t(piece_index));
	return 0;
}

void session::finish_read_piece(int torrent_id, int err, int piece_index, char* buffer, int size)
{
	if (m_callbacks.read_piece)
		m_callbacks.read_piece(err, torrent_id, piece_index, buffer, size);
}

void session::handle_alert(lt::alert* p)
{
	if (m_logging_enabled)
	{
		if(m_callbacks.log)
			m_callbacks.log(p->type(), p->message().c_str());
	}

	int type = p->type();

	// read_piece_alert
	if (auto a = lt::alert_cast<lt::read_piece_alert>(p))
	{
		finish_read_piece(a->handle.id(), make_error_code(a->error), (int)a->piece, a->buffer.get(), a->size);
	}
	// save_resume_data_alert
	else if (auto a = lt::alert_cast<lt::save_resume_data_alert>(p))
	{
		finish_save_resume_data(a->handle.id(), 0, &a->params);
	}
	// save_resume_data_failed_alert
	else if (auto a = lt::alert_cast<lt::save_resume_data_failed_alert>(p))
	{
		finish_save_resume_data(a->handle.id(), make_error_code(a->error), nullptr);
	}
	// dht_put_alert
	else if (auto a = lt::alert_cast<lt::dht_put_alert>(p))
	{
		if(m_callbacks.dht_put_item)
			m_callbacks.dht_put_item(a->public_key.data(), a->signature.data(), a->salt.data(), int(a->salt.size()), a->seq, a->num_success);
	}
	// dht_mutable_item_alert
	else if (auto a = lt::alert_cast<lt::dht_mutable_item_alert>(p))
	{
		std::vector<char> buffer;
		libtorrent::bencode(std::back_inserter(buffer), a->item);

		if (m_callbacks.dht_get_item)
			m_callbacks.dht_get_item(a->key.data(), a->signature.data(), a->salt.data(), int(a->salt.size()), a->seq, buffer.data(), int(buffer.size()));
	}
	// dht_bootstrap_alert
	else if (auto a = lt::alert_cast<lt::dht_bootstrap_alert>(p))
	{
		if (m_callbacks.dht_bootstrap)
			m_callbacks.dht_bootstrap();
	}
	// state_update_alert
	else if (auto a = lt::alert_cast<lt::state_update_alert>(p))
	{
		for (lt::torrent_status& stat : a->status)
		{
			if (auto torrent = get_torrent(stat.handle.id()))
			{
				auto status = torrent_status(stat);
				if (m_callbacks.torrent_status_updated)
					m_callbacks.torrent_status_updated(stat.handle.id(), &status);
			}
		}
	}
	// torrent_removed_alert
	else if (auto a = lt::alert_cast<lt::torrent_removed_alert>(p))
	{
		if (m_callbacks.torrent_removed)
		{
#if LIBTORRENT_VERSION_MAJOR >= 2
			auto infohash = a->info_hashes.get_best();
			
#else
			auto infohash = a->info_hash;
#endif
			m_callbacks.torrent_removed(0, infohash.data(), false);
		}
	}
	// torrent_deleted_alert
	else if (auto a = lt::alert_cast<lt::torrent_deleted_alert>(p))
	{
		if (m_callbacks.torrent_removed)
		{
#if LIBTORRENT_VERSION_MAJOR >= 2
			auto infohash = a->info_hashes.get_best();
#else
			auto infohash = a->info_hash;
#endif
			m_callbacks.torrent_removed(0, infohash.data(), true);
		}
	}
	// torrent_delete_failed_alert
	else if (auto a = lt::alert_cast<lt::torrent_delete_failed_alert>(p))
	{
		if (m_callbacks.torrent_removed)
		{
#if LIBTORRENT_VERSION_MAJOR >= 2
			auto infohash = a->info_hashes.get_best();
#else
			auto infohash = a->info_hash;
#endif
			m_callbacks.torrent_removed(make_error_code(a->error), infohash.data(), true);
		}
	}
	else if (auto a = lt::alert_cast<lt::file_error_alert>(p))
	{
		if (m_callbacks.torrent_file_error)
			m_callbacks.torrent_file_error(a->handle.id(), make_error_code(a->error), a->filename(), a->message().c_str());
	}
	else if(type == lt::peer_connect_alert::alert_type ||
			type == lt::peer_disconnected_alert::alert_type ||
			type == lt::peer_ban_alert::alert_type ||
			type == lt::peer_blocked_alert::alert_type ||
			type == lt::peer_error_alert::alert_type)
	{
		auto a = static_cast<lt::peer_alert*>(p);
		if (m_callbacks.peer_activty)
		{
			const auto ip = a->endpoint.address().to_string();
			const int port = a->endpoint.port();

			int direction = 0;
			if (auto connection_alert = lt::alert_cast<lt::peer_connect_alert>(p))
			{
				direction = static_cast<int>(connection_alert->direction) + 1;
			}

			int error = 0;
			int close_reason = 0;
			if (auto connection_alert = lt::alert_cast<lt::peer_disconnected_alert>(p))
			{
				error = make_error_code(connection_alert->error);
				close_reason = static_cast<int>(connection_alert->reason);
			}
			if (auto connection_alert = lt::alert_cast<lt::peer_error_alert>(p))
			{
				error = make_error_code(connection_alert->error);
			}

			int torrent_id = -1;
			if (a->handle.is_valid())
				torrent_id = a->handle.id();

			m_callbacks.peer_activty(type, a->handle.id(), ip.c_str(), port, direction, error, close_reason);
		}
	}
	
}

void session::debug_log(const std::string& message)
{
//#if(_WIN32 || _WIN64)
//	std::wofstream ofs(m_log_path, std::ios::app);
//	time_t now = time(0);
//	ofs << "[" << std::put_time<wchar_t>(std::localtime(&now), L"%Y-%m-%d %X") << "]: ";
//	ofs << utf8_decode(message) << std::endl;
//#endif
}

API int session_new(session** p, char* settings_buf, int settings_buf_size)
{
	try
	{
		lt::bdecode_node settings_dict = lt::bdecode(lt::span<const char>(settings_buf, settings_buf_size));

		auto s = std::make_unique<session>();
		s->init(settings_dict);
		*p = s.release();
		return 0;
	}
	catch (lt::system_error err)
	{
		return make_error_code(err.code());
	}
}

API void session_delete(session* session)
{
	delete session;
}

API int session_poll(session* session, int timeout)
{
	session->poll(timeout);
	return 0;
}

API int session_add_torrent(session* session, torrent_add_params* atp, int* torrent_id)
{
	return session->add_torrent(atp, torrent_id);
}

API int session_remove_torrent(session* session, int torrent_id, bool delete_files)
{
	return session->remove_torrent(torrent_id, delete_files);
}

API int session_post_torrent_updates(session* session)
{
	session->m_session->post_torrent_updates();
	return 0;
}

API int session_find_torrent_by_info_hash(session* session, char* info_hash)
{
	lt::torrent_handle handle = session->m_session->find_torrent(lt::sha1_hash(info_hash));
	if (!handle.is_valid())
		return -1;

	return handle.id();
}


API int session_set_callbacks(session* session, session_callbacks* callbacks)
{
	session->m_callbacks = *callbacks;
	return 0;
}

API int session_apply_settings_buf(session* session, char* buffer, int size)
{
	try
	{
		lt::settings_pack pack;
		apply_settings_dict(&pack, lt::bdecode(lt::span<const char>(buffer, size)));
		session->m_session->apply_settings(std::move(pack));
	}
	catch (lt::system_error err)
	{
		return make_error_code(err.code());
	}

	return 0;
}
