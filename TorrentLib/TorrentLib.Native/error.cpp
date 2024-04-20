#include <libtorrent/error_code.hpp>
#include <libtorrent/upnp.hpp>
#include <libtorrent/bdecode.hpp>
#include "error.hpp"

enum class error_category_id
{
	unknown = 0,
	system,
	generic,
	libtorrent,
	http,
	upnp,
	bdecode,
	asio_netdb,
	asio_addrinfo,
	asio_misc,
	count
};

constexpr int log2(int n) { return ((n < 2) ? 1 : 1 + log2(n / 2)); }
constexpr int cateogry_bits = log2(int(error_category_id::count));
constexpr int code_bits = 32 - cateogry_bits;

static constexpr error_category_id get_category_id(const lt::error_code& ec)
{
	if (ec.category() == lt::system_category()) return error_category_id::system;
	else if (ec.category() == lt::generic_category()) return error_category_id::generic;
	else if (ec.category() == lt::libtorrent_category()) return error_category_id::libtorrent;
	else if (ec.category() == lt::http_category()) return error_category_id::http;
	else if (ec.category() == lt::upnp_category()) return error_category_id::upnp;
	else if (ec.category() == lt::bdecode_category()) return error_category_id::bdecode;
	else if (ec.category() == boost::asio::error::get_netdb_category()) return error_category_id::asio_netdb;
	else if (ec.category() == boost::asio::error::get_addrinfo_category()) return error_category_id::asio_addrinfo;
	else if (ec.category() == boost::asio::error::get_misc_category()) return error_category_id::asio_misc;
	return error_category_id::unknown;
}

static constexpr const boost::system::error_category* get_category(int error_code)
{
	switch (error_category_id(error_code >> code_bits))
	{
	case error_category_id::unknown: return nullptr;
	case error_category_id::system: return &lt::system_category();
	case error_category_id::generic: return &lt::generic_category();
	case error_category_id::libtorrent: return &lt::libtorrent_category();
	case error_category_id::http: return &lt::http_category();
	case error_category_id::upnp: return &lt::upnp_category();
	case error_category_id::bdecode: return &lt::bdecode_category();
	case error_category_id::asio_netdb: return &boost::asio::error::get_netdb_category();
	case error_category_id::asio_addrinfo: return &boost::asio::error::get_addrinfo_category();
	case error_category_id::asio_misc: return &boost::asio::error::get_misc_category();
	default:
		assert(!"unreachable");
		return nullptr;
	}
}

int make_error_code(const lt::error_code& ec)
{
	if (!ec) return 0;
	assert(ec.value() < (1 << code_bits));
	return (static_cast<int>(get_category_id(ec)) << code_bits) | ec.value();
}

API int format_error_message(int code, char* buffer, int buffer_size)
{
	std::string message;
	if (code == 0)
		message = std::system_category().message(0);
	else if (auto category = get_category(code))
		message = category->message(code << cateogry_bits >> cateogry_bits);
	else
		message = "Unknown error";

	int len = static_cast<int>(message.size());
	if (buffer && len > buffer_size)
		return -1;
	else if (buffer)
		memcpy(buffer, message.data(), message.size());

	return len;
}
