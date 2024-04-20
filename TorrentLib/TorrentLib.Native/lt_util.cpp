#include "lt_util.hpp"

namespace libtorrent
{
	string_view trim(string_view str)
	{
		auto const first = str.find_first_not_of(" \t\n\r");
		auto const last = str.find_last_not_of(" \t\n\r");
		return str.substr(first == string_view::npos ? str.size() : first, last - first + 1);
	}

	tcp::endpoint parse_endpoint(string_view str, error_code& ec)
	{
		tcp::endpoint ret;

		str = trim(str);

		string_view addr;
		string_view port;

		if (str.empty())
		{
			ec = errors::invalid_port;
			return ret;
		}

		// this is for IPv6 addresses
		if (str.front() == '[')
		{
			auto const close_bracket = str.find_first_of(']');
			if (close_bracket == string_view::npos)
			{
				ec = errors::expected_close_bracket_in_address;
				return ret;
			}
			addr = str.substr(1, close_bracket - 1);
			port = str.substr(close_bracket + 1);
			if (port.empty() || port.front() != ':')
			{
				ec = errors::invalid_port;
				return ret;
			}
			// shave off the ':'
			port = port.substr(1);
			ret.address(make_address_v6(addr.to_string(), ec));
			if (ec) return ret;
		}
		else
		{
			auto const port_pos = str.find_first_of(':');
			if (port_pos == string_view::npos)
			{
				ec = errors::invalid_port;
				return ret;
			}
			addr = str.substr(0, port_pos);
			port = str.substr(port_pos + 1);
			ret.address(make_address_v4(addr.to_string(), ec));
			if (ec) return ret;
		}

		if (port.empty())
		{
			ec = errors::invalid_port;
			return ret;
		}

		int const port_num = std::atoi(port.to_string().c_str());
		if (port_num <= 0 || port_num > std::numeric_limits<std::uint16_t>::max())
		{
			ec = errors::invalid_port;
			return ret;
		}
		ret.port(static_cast<std::uint16_t>(port_num));
		return ret;
	}
}