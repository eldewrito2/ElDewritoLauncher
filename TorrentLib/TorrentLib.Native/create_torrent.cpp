#include "error.hpp"
#include <libtorrent/version.hpp>
#include <libtorrent/create_torrent.hpp>
#include <libtorrent/entry.hpp>
#include <random>
#include <filesystem>


// copies from lt example
static std::string branch_path(std::string const& f)
{
	if (f.empty()) return f;

#ifdef TORRENT_WINDOWS
	if (f == "\\\\") return "";
#endif
	if (f == "/") return "";

	auto len = f.size();
	// if the last character is / or \ ignore it
	if (f[len - 1] == '/' || f[len - 1] == '\\') --len;
	while (len > 0) {
		--len;
		if (f[len] == '/' || f[len] == '\\')
			break;
	}

	if (f[len] == '/' || f[len] == '\\') ++len;
	return std::string(f.c_str(), len);
}

static void replace_string(std::string& data, std::string search, std::string replace_str)
{
	size_t pos = data.find(search);
	while (pos != std::string::npos)
	{
		data.replace(pos, search.size(), replace_str);
		pos = data.find(search, pos + replace_str.size());
	}
}

API int create_torrent(char* buffer, int size, void(*cb)(char* buffer, int size))
{
	try
	{
		auto entry = lt::bdecode(lt::span<const char>(buffer, size));

		int piece_size = static_cast<int>(entry.dict_find_int_value("piece_size"));

		lt::create_flags_t flags = {};
		if (entry.dict_find_int_value("modification_time") == 1)
			flags |= lt::create_torrent::modification_time;
		if (entry.dict_find_int_value("symlinks") == 1)
			flags |= lt::create_torrent::symlinks;
#if LIBTORRENT_VERSION_MAJOR >= 2
		if (entry.dict_find_int_value("v1_only") == 1)
			flags |= lt::create_torrent::v1_only;
		if (entry.dict_find_int_value("v2_only") == 1)
			flags |= lt::create_torrent::v2_only;
		if (entry.dict_find_int_value("canonical_files") == 1)
			flags |= lt::create_torrent::canonical_files;
		if (entry.dict_find_int_value("canonical_files_no_tail_padding") == 1)
			flags |= lt::create_torrent::canonical_files_no_tail_padding;
		if (entry.dict_find_int_value("no_attributes") == 1)
			flags |= lt::create_torrent::no_attributes;
#endif

		std::string full_path = std::string(entry.dict_find_string_value("full_path"));
		auto p = std::filesystem::absolute(full_path).u8string();

		lt::file_storage fs;
		lt::add_files(fs, p, flags);

		lt::create_torrent t(fs, piece_size, flags);

		int num = t.num_pieces();
		lt::error_code ec;
		lt::set_piece_hashes(t, branch_path(full_path), ec);
		if (ec)
			return make_error_code(ec);

		if (auto comment_entry = entry.dict_find_string("comment"))
		{
			const auto comment = std::string(comment_entry.string_value());
			t.set_comment(comment.c_str());
		}

		if (auto creator_entry = entry.dict_find_string("creator"))
		{
			const auto creator = std::string(creator_entry.string_value());
			t.set_creator(creator.c_str());
		}

		if (auto date_entry = entry.dict_find_int("creation_date"))
		{
#if LIBTORRENT_VERSION_MAJOR >= 2
			t.set_creation_date(date_entry.int_value());
#endif
		}

		if (auto http_seeds = entry.dict_find_list("http_seeds"))
		{
			for (int i = 0; i < http_seeds.list_size(); i++)
				t.add_http_seed(std::string(http_seeds.list_at(i).string_value()));
		}

		if (auto url_seeds = entry.dict_find_list("url_seeds"))
		{
			for (int i = 0; i < url_seeds.list_size(); i++)
				t.add_url_seed(std::string(url_seeds.list_at(i).string_value()));
		}

		if (auto trackers = entry.dict_find_list("trackers"))
		{
			for (int i = 0; i < trackers.list_size(); i++)
				t.add_tracker(std::string(trackers.list_at(i).string_value()));
		}

		if (auto nodes = entry.dict_find_list("nodes"))
		{
			for (int i = 0; i < nodes.list_size(); i++)
			{
				auto node = nodes.list_at(i).string_value();
				std::string hostname, portStr;
				if (auto sep = node.find(':'); sep != -1)
				{
					hostname = std::string(node.substr(0, sep));
					portStr = std::string(node.substr(sep + 1));
				}

				if (hostname.empty() || portStr.empty())
					continue;
				int port = std::atoi(portStr.c_str());
				t.add_node({ hostname, port });
			}
		}

		t.set_priv(entry.dict_find_int_value("private") == 1);

		std::vector<char> buff;
		lt::entry e = t.generate();
		lt::bencode(std::back_inserter(buff), e);

		cb(buff.data(), static_cast<int>(buff.size()));
		return 0;
	}
	catch (const lt::system_error& err)
	{
		return make_error_code(err.code());
	}
}
