#include "torrent.hpp"
#include <libtorrent/bencode.hpp>
#include <libtorrent/socket_io.hpp>
#include <libtorrent/string_util.hpp>
#include "lt_util.hpp"

static int parse_int(lt::string_view value)
{
	return std::atoi(std::string(value).c_str());
}

static lt::file_index_t parse_file_index(lt::string_view value)
{
	return static_cast<lt::file_index_t>(parse_int(value));
}

static std::pair<std::string, int> parse_endpoint(lt::string_view value)
{
	if (auto const divider = value.find_last_of(':'))
	{
		int const port = std::atoi(value.substr(divider + 1).to_string().c_str());
		if (port > 0 && port < int(std::numeric_limits<std::uint16_t>::max()))
			return { value.substr(0, divider).to_string(), port };
	}
	return {};
}

template <class Container>
static void read_urls(const lt::bdecode_node& node, Container& container)
{
	for (int i = 0; i < node.list_size(); i++)
		container.push_back(std::string(node.list_at(i).string_value()));
}

API void torrent_params_apply(torrent_add_params* p, char* data, int data_size)
{
	const auto entry = lt::bdecode(lt::span<const char>(data, data_size));

	// name
	if (const auto name = entry.dict_find_string_value("name"); !name.empty())
		p->name = std::string(name);

	// save_path
	if (const auto save_path = entry.dict_find_string_value("save_path"); !save_path.empty())
		p->save_path = std::string(save_path);

	// renamed_files
	if (const auto renamed_files = entry.dict_find_dict("renamed_files"))
	{
		for (int i = 0; i < renamed_files.dict_size(); i++)
		{
			const auto [k, v] = renamed_files.dict_at(i);
			p->renamed_files[parse_file_index(k)] = std::string(v.string_value());
		}
	}

	// file_priorities
	if (const auto file_priorities = entry.dict_find_dict("file_priorities"))
	{
		for (int i = 0; i < file_priorities.dict_size(); i++)
		{
			const auto [k, v] = file_priorities.dict_at(i);
			int index = parse_file_index(k);

			if (index >= static_cast<int>(p->file_priorities.size()))
				p->file_priorities.resize(index + 1, lt::default_priority);
			p->file_priorities[index] = static_cast<int>(v.int_value());
		}
	}

	// dht_nodes
	if (const auto dht_nodes = entry.dict_find_list("dht_nodes"))
		for (int i = 0; i < dht_nodes.list_size(); i++)
			p->dht_nodes.push_back(parse_endpoint(dht_nodes.list_at(i).string_value()));

	// peers
	if (const auto peers = entry.dict_find_list("peers"))
	{
		for (int i = 0; i < peers.list_size(); i++)
		{
			lt::error_code ec;
			p->peers.push_back(libtorrent::parse_endpoint(peers.list_at(i).string_value(), ec));
		}
	}

	// http_seeds
	if (const auto http_seeds = entry.dict_find_list("http_seeds"))
		read_urls(http_seeds, p->http_seeds);

	// url_seeds
	if (const auto url_seeds = entry.dict_find_list("url_seeds"))
		read_urls(url_seeds, p->url_seeds);

	// trackers
	if (const auto trackers = entry.dict_find_list("trackers"))
		read_urls(trackers, p->trackers);

	// flags
	p->flags = static_cast<lt::torrent_flags_t>(entry.dict_find_int_value("flags"));

	// download_limit
	p->download_limit = entry.dict_find_int_value("download_limit", -1);

	// upload_limit
	p->upload_limit = entry.dict_find_int_value("upload_limit", -1);
}
