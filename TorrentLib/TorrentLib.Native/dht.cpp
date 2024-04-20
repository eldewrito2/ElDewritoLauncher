#include "dht.hpp"
#include "session.hpp"
#include <array>
#include <libtorrent/kademlia/ed25519.hpp>
#include <libtorrent/kademlia/item.hpp>
#include <libtorrent/bencode.hpp>

int dht_gen_keypair(char* seed, int seed_size, char* pk, char* sk)
{
	std::array<char, 32> seed_arr;
	std::copy_n(seed, 32, seed_arr.begin());
	auto const [pk_tmp, sk_tmp] = libtorrent::dht::ed25519_create_keypair(seed_arr);
	memcpy(pk, &pk_tmp.bytes[0], pk_tmp.len);
	memcpy(sk, &sk_tmp.bytes[0], sk_tmp.len);
	return 0;
}

int dht_get_mutable_item(
	session* session,
	const char* pk,
	const char* salt,
	int salt_size)
{
	std::array<char, 32> pk_arr;
	memcpy(pk_arr.data(), pk, pk_arr.size());
	session->m_session->dht_get_item(pk_arr, std::string(salt, salt_size));
	return 0;
}


int dht_put_mutable_item(
	session* session,
	const char* pk,
	const char* sk,
	const char* salt,
	int salt_size,
	const char* data,
	int data_size)
{
	std::array<char, 32> pk_arr;
	std::array<char, 64> sk_arr;
	std::vector<char> data_copy(data, &data[data_size]);
	std::string salt_copy(salt, salt_size);
	memcpy(pk_arr.data(), pk, pk_arr.size());
	memcpy(sk_arr.data(), sk, sk_arr.size());

	session->m_session->dht_put_item(pk_arr, [=](libtorrent::entry& entry,
												 std::array<char, 64>& signature,
												 std::int64_t& sequence,
												 std::string const& salt)
	{
		lt::error_code ec;
		entry = lt::bdecode(data_copy, ec);

		std::vector<char> buffer;
		libtorrent::bencode(std::back_inserter(buffer), entry);

		sequence++;
		libtorrent::dht::signature sign = lt::dht::sign_mutable_item(
			buffer,
			salt,
			lt::dht::sequence_number(sequence),
			lt::dht::public_key(pk_arr.data()),
			lt::dht::secret_key(sk_arr.data()));

		signature = sign.bytes;
	}, salt_copy);

	return 0;
}


int dht_announce_mutable_item(
	session* session,
	const char* pk,
	const char* salt,
	int salt_size,
	const char* sig,
	const char* data,
	int data_size,
	long long item_sequence = -1)
{
	std::array<char, 32> pk_arr;
	libtorrent::dht::signature sign(sig);
	std::vector<char> data_copy(data, &data[data_size]);
	std::string salt_copy(salt, salt_size);
	memcpy(pk_arr.data(), pk, pk_arr.size());

	session->m_session->dht_put_item(pk_arr, [=](libtorrent::entry& entry,
		std::array<char, 64>& signature,
		std::int64_t& sequence,
		std::string const& salt)
		{
			lt::error_code ec;
			entry = lt::bdecode(data_copy, ec);

			// The sequence was part of the signature hash
			// so when reannouncing we must match the original sequence or else nodes will reject
			if (item_sequence != -1) //if unspecified sequence, use libtorrent supplied value
				sequence = item_sequence;

			std::vector<char> buffer;
			libtorrent::bencode(std::back_inserter(buffer), entry);
			signature = sign.bytes;
		}, salt);

	return 0;
}
