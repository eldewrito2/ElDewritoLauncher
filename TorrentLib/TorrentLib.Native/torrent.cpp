#include "session.hpp"
#include "memory.hpp"
#include "error.hpp"
#include <libtorrent/magnet_uri.hpp>
#include <libtorrent/create_torrent.hpp>
#include <libtorrent/read_resume_data.hpp>
#include <libtorrent/write_resume_data.hpp>
#include <libtorrent/truncate.hpp>
#if LIBTORRENT_VERSION_MAJOR >= 2
#include <libtorrent/load_torrent.hpp>
#endif

//#include <libtorrent/load_torrent.hpp>

torrent_status::torrent_status(const lt::torrent_status& stat)
{
	errc = make_error_code(stat.errc);
	error_file = static_cast<int>(stat.error_file);
	total_download = stat.total_download;
	total_upload = stat.total_upload;
	total_payload_download = stat.total_payload_download;
	total_payload_upload = stat.total_payload_upload;
	total_failed_bytes = stat.total_failed_bytes;
	total_redundant_bytes = stat.total_redundant_bytes;
	total_done = stat.total_done;
	total = stat.total;
	total_wanted_done = stat.total_wanted_done;
	total_wanted = stat.total_wanted;
	all_time_upload = stat.all_time_upload;
	all_time_download = stat.all_time_download;
	added_time = stat.added_time;
	completed_time = stat.completed_time;
	last_seen_complete = stat.last_seen_complete;
	progress = stat.progress;
	progress_ppm = stat.progress_ppm;
	queue_position = static_cast<int>(stat.queue_position);
	download_rate = stat.download_rate;
	upload_rate = stat.upload_rate;
	download_payload_rate = stat.download_payload_rate;
	upload_payload_rate = stat.upload_payload_rate;
	num_seeds = stat.num_seeds;
	num_peers = stat.num_peers;
	num_complete = stat.num_complete;
	num_incomplete = stat.num_incomplete;
	list_seeds = stat.list_seeds;
	list_peers = stat.list_peers;
	connect_candidates = stat.connect_candidates;
	num_pieces = stat.num_pieces;
	distributed_full_copies = stat.distributed_full_copies;
	distributed_fraction = stat.distributed_fraction;
	distributed_copies = stat.distributed_copies;
	block_size = stat.block_size;
	num_uploads = stat.num_uploads;
	num_connections = stat.num_connections;
	uploads_limit = stat.uploads_limit;
	connections_limit = stat.connections_limit;
	up_bandwidth_queue = stat.up_bandwidth_queue;
	down_bandwidth_queue = stat.down_bandwidth_queue;
	seed_rank = stat.seed_rank;
	state = stat.state;
	need_save_resume = stat.need_save_resume;
	is_seeding = stat.is_seeding;
	is_finished = stat.is_finished;
	has_metadata = stat.has_metadata;
	has_incoming = stat.has_incoming;
	moving_storage = stat.moving_storage;
	announcing_to_trackers = stat.announcing_to_trackers;
	announcing_to_lsd = stat.announcing_to_lsd;
	announcing_to_dht = stat.announcing_to_dht;
	last_upload = lt::time_point_cast<lt::seconds>(stat.last_upload).time_since_epoch().count();
	last_download = lt::time_point_cast<lt::seconds>(stat.last_download).time_since_epoch().count();
	active_duration =stat.active_duration.count();
	finished_duration = stat.finished_duration.count();
	seeding_duration = stat.seeding_duration.count();
	flags = stat.flags;
}

// ---------------------------------- torrent -----------------------------------------------

API int torrent_get_info_hash(session* session, int torrent_id, char* buffer)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	auto hash = torrent->handle.info_hash();
	memcpy(buffer, hash.data(), hash.size());
	return 0;
}

API int torrent_get_info_hash_v1(session* session, int torrent_id, char* buffer)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

#if LIBTORRENT_VERSION_MAJOR >= 2
	auto hashs = torrent->handle.info_hashes();
	memcpy(buffer, hashs.v1.data(), hashs.v1.size());
#else
	auto hashs = torrent->handle.info_hash();
	memcpy(buffer, hashs.data(), hashs.size());
#endif
	return 0;
}

API int torrent_get_info_hash_v2(session* session, int torrent_id, char* buffer)
{
#if LIBTORRENT_VERSION_MAJOR >= 2
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return -1;

	auto hashs = torrent->handle.info_hashes();
	memcpy(buffer, hashs.v2.data(), hashs.v2.size());
	return 0;
#else
	assert(!"not supported");
	return -1;
#endif

}


API const torrent_info* torrent_get_info(session* session, int torrent_id)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return nullptr;

	return torrent->handle.torrent_file().get();
}

API int torrent_get_status(session* session, int torrent_id, torrent_status* status)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	*status = torrent->handle.status();
	return 0;
}

API int torrent_read_piece(session* session, int torrent_id, int piece_index)
{
	return session->torrent_read_piece(torrent_id, piece_index);
}

API int torrent_set_piece_deadline(session* session, int torrent_id, int piece_index, int deadline, bool alert_when_available)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	lt::deadline_flags_t flags = {};
	if (alert_when_available)
		flags |= lt::torrent_handle::alert_when_available;
	torrent->handle.set_piece_deadline(static_cast<lt::piece_index_t>(piece_index), deadline, flags);
	return 0;
}

API int torrent_set_piece_priority(session* session, int torrent_id, int piece_index, int priority)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	torrent->handle.piece_priority(static_cast<lt::piece_index_t>(piece_index), priority);
	return 0;
}

API int torrent_reset_piece_deadline(session* session, int torrent_id, int piece_index, int deadline)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	torrent->handle.reset_piece_deadline(deadline);
	return 0;
}


API int torrent_save_resume_data(session* session, int torrent_id)
{
	return session->torrent_save_resume_data(torrent_id);
}

API int torrent_clear_error(session* session, int torrent_id)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	torrent->handle.clear_error();
	return 0;
}

API int torrent_force_recheck(session* session, int torrent_id)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	torrent->handle.force_recheck();
	return 0;
}

API int torrent_set_upload_limit(session* session, int torrent_id, int limit)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	torrent->handle.set_upload_limit(limit);
	return 0;
}

API int torrent_get_upload_limit(session* session, int torrent_id)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	return torrent->handle.download_limit();
}

API int torrent_set_download_limit(session* session, int torrent_id, int limit)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	torrent->handle.set_download_limit(limit);
	return 0;
}

API int torrent_get_download_limit(session* session, int torrent_id)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	return torrent->handle.download_limit();
}


API int torrent_pause(session* session, int torrent_id)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	torrent->handle.pause();
	return 0;
}

API int torrent_resume(session* session, int torrent_id)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	torrent->handle.resume();
	return 0;
}

API int torrent_set_flags(session* session, int torrent_id, uint64_t add_flags, uint64_t remove_flags)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	uint64_t mask = (add_flags | remove_flags);
	torrent->handle.set_flags(static_cast<lt::torrent_flags_t>(add_flags), static_cast<lt::torrent_flags_t>(mask));
	return 0;
}


API int torrent_query_pieces(session* session, int torrent_id, int start_piece, int end_piece, uint32_t* result_bitset)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	lt::torrent_status status = torrent->handle.status(lt::torrent_handle::query_pieces);

	for(int i = start_piece; i <= end_piece; i++)
	{
		if(status.pieces.get_bit(static_cast<lt::piece_index_t>(i)))
			result_bitset[i >> 5] |= (1u << (i & 31));
		else
			result_bitset[i >> 5] &= ~(1u << (i & 31));
	}

	return 0;
}

API int torrent_set_queue_position(session* session, int torrent_id, int queue_index)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	torrent->handle.queue_position_set(static_cast<lt::queue_position_t>(queue_index));
	return 0;
}

API int torrent_get_queue_position(session* session, int torrent_id)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	return torrent->handle.queue_position();
}


API int torrent_get_name(session* session, int torrent_id, char* buffer, int buffer_size)
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	const std::string value = torrent->handle.status(lt::torrent_handle::query_name).name;
	const int len = static_cast<int>(value.size());
	if(buffer == nullptr) return len;
	if(buffer_size < value.size()) return -1;
	memcpy(buffer, value.data(), len);
	return len;
}

// ---------------------------------- torrent_info -----------------------------------------------

using torrent_info_serialize_callback_t = void(*)(int err, char* buffer, int size);


API int torrent_info_new_from_file(torrent_info** p, const char* filename)
{
	lt::error_code ec;
	auto ti = std::make_shared<lt::torrent_info>(filename, ec);
	if (ec)
		return make_error_code(ec);

	*p = new torrent_info { *ti };
	return 0;
}

API int torrent_info_new_from_buffer(torrent_info** p, char* buffer, int size)
{
	lt::error_code ec;
	auto info = std::make_shared<lt::torrent_info>(buffer, size, ec);
	if(ec)
		return make_error_code(ec);

	*p = new torrent_info { *info };
	return 0;
}


API int torrent_info_delete(torrent_info* p)
{
	delete p;
	return 0;
}

API int torrent_info_get_file_count(torrent_info* p)
{
	return p->files().num_files();
}

API int torrent_info_get_file_name(torrent_info* p, int file_index, char* buffer, int buffer_size)
{
	auto value = p->files().file_name(lt::file_index_t(file_index));
	int len = static_cast<int>(value.length());
	if (buffer == nullptr) return len;
	if(buffer_size < len) return -1;
	memcpy(buffer, value.data(), len);
	return len;
}

API int torrent_info_get_file_path(torrent_info* p, int file_index, char* buffer, int buffer_size)
{
	auto value = p->files().file_path(lt::file_index_t(file_index));
	int len = static_cast<int>(value.length());
	if (buffer == nullptr) return len;
	if (buffer_size < len) return -1;
	memcpy(buffer, value.data(), len);
	return len;
}

API int64_t torrent_info_get_file_size(torrent_info* p, int file_index)
{
	return p->files().file_size(lt::file_index_t(file_index));
}

API int64_t torrent_info_get_piece_size(torrent_info* p)
{
	return p->piece_length();
}

API int64_t torrent_info_get_piece_count(torrent_info* p)
{
	return p->num_pieces();
}

API int torrent_info_map_file(torrent_info* p, int file_index, int64_t offset, int size, lt::peer_request* request_out)
{
	*request_out = p->map_file(static_cast<lt::file_index_t>(file_index), offset, size);
	return 0;
}

API int torrent_info_serialize(torrent_info* p, torrent_info_serialize_callback_t cb)
{
	try 
	{
		const lt::create_torrent creator = lt::create_torrent(*p);
		const lt::entry entry = creator.generate();
		std::vector<char> bencoded;
		lt::bencode(std::back_inserter(bencoded), entry);
		cb(0, bencoded.data(), static_cast<int>(bencoded.size()));
		return 0;
	}
	catch (lt::system_error err)
	{
		return make_error_code(err.code());
	}
}

// ---------------------------------- torrent_add_params -----------------------------------------------

API int torrent_params_new(torrent_add_params** p)
{
	*p = new torrent_add_params();
	return 0;
}

API int torrent_params_delete(torrent_add_params* atp)
{
	delete atp;
	return 0;
}

API int torrent_params_new_from_info(torrent_add_params** p, torrent_info* info)
{
	auto atp = new torrent_add_params();
	atp->ti = std::make_shared<lt::torrent_info>(*info);
	*p = atp;
	return 0;
}

API int torrent_params_new_from_magnet(torrent_add_params** p, const char* magnet_link)
{	
	try 
	{
		auto atp = lt::parse_magnet_uri(magnet_link);
		*p = new torrent_add_params{ std::move(atp) };
		return 0;
	}
	catch(lt::system_error err)
	{
		return make_error_code(err.code());
	}
}

API int torrent_params_new_from_resume_data(torrent_add_params** p, const char* resume_data, int size)
{
	lt::error_code ec;
	lt::add_torrent_params atp = lt::read_resume_data(lt::span<const char>(resume_data, size), ec);
	if (ec)
		return make_error_code(ec);

	*p = new torrent_add_params { std::move(atp) };
	return 0;
}

API int torrent_params_new_from_torrent_buffer(torrent_add_params** p, const char* buffer, int size)
{
	try
	{
#if LIBTORRENT_VERSION_MAJOR >= 2
		lt::add_torrent_params atp = lt::load_torrent_buffer(lt::span<const char>(buffer, size));
#else
		lt::add_torrent_params atp;
		atp.ti = std::make_shared<lt::torrent_info>(lt::span<const char>(buffer, size), lt::from_span);
#endif
		
		*p = new torrent_add_params{ std::move(atp) };
		return 0;
	}
	catch (lt::system_error err)
	{
		return make_error_code(err.code());
	}
}

API int write_torrent_file(torrent_add_params* atp, void(*cb)(int err, char* buffer, int size))
{
	try
	{
		if (atp->ti == nullptr)
		{
			cb(0, nullptr, 0);
			return 0;
		}

#if LIBTORRENT_VERSION_MAJOR >= 2
		lt::entry entry = lt::write_torrent_file(*atp);
#else
		lt::create_torrent creator{ *atp->ti };
		lt::entry entry = creator.generate();
#endif
		std::vector<char> data;
		lt::bencode(std::back_inserter(data), entry);
		cb(0, data.data(), static_cast<int>(data.size()));
		return 0;
	}
	catch (lt::system_error err)
	{
		return make_error_code(err.code());
	}
}

API int write_resume_data(torrent_add_params* atp, void(*cb)(int err, char* buffer, int size))
{
	try
	{
		lt::entry entry = lt::write_resume_data(*atp);
		std::vector<char> data;
		lt::bencode(std::back_inserter(data), entry);
		cb(0, data.data(), static_cast<int>(data.size()));
		return 0;
	}
	catch (lt::system_error err)
	{
		return make_error_code(err.code());
	}
}


API int torrent_enumerate_files(session* session, int torrent_id, void(*cb)(const char* path, int64_t size))
{
	torrent* torrent = session->get_torrent(torrent_id);
	if (!torrent)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	auto ti = torrent->handle.torrent_file();
	if (ti == nullptr)
		return make_error_code(lt::error_code(lt::errors::no_metadata));

	const auto& fs = torrent->handle.torrent_file()->files();
	for (lt::file_index_t i = 0; i < static_cast<lt::file_index_t>(fs.num_files()); i++)
	{
		std::string path = fs.file_path(i);
		cb(path.c_str(), fs.file_size(i));
	}
	return 0;
}

API int make_magnet_uri(session* session, int torrent_id, torrent_info_serialize_callback_t cb)
{
	try 
	{
		torrent* torrent = session->get_torrent(torrent_id);
		if (!torrent)
			return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

		std::string magnet_uri = lt::make_magnet_uri(torrent->handle);
		cb(0, magnet_uri.data(), static_cast<int>(magnet_uri.size()));
		return 0;
	}
	catch (lt::system_error err)
	{
		return make_error_code(err.code());
	}
}

API int make_magnet_uri_adp(session* session, char* buffer, int size, torrent_info_serialize_callback_t cb)
{
	try
	{
		auto ti = lt::torrent_info(lt::span<const char>(buffer, size), lt::from_span);
		//lt::add_torrent_params adp = lt::load_torrent_buffer(lt::span<const char>(buffer, size));
		std::string magnet_uri = lt::make_magnet_uri(ti);
		cb(0, magnet_uri.data(), static_cast<int>(magnet_uri.size()));
		return 0;
	}
	catch (lt::system_error err)
	{
		return make_error_code(err.code());
	}
}

API int torrent_truncate_files(session* session, int torrent_id)
{
	torrent* t = session->get_torrent(torrent_id);
	if (!t)
		return make_error_code(lt::error_code(lt::errors::invalid_torrent_handle));

	auto ti = t->handle.torrent_file();
	if (!ti)
		return make_error_code(lt::error_code(lt::errors::no_metadata));

	lt::storage_error storage_err;
	lt::truncate_files(ti->files(), t->handle.save_path(), storage_err);
	if (storage_err.ec)
		return make_error_code(storage_err.ec);

	return 0;
}
