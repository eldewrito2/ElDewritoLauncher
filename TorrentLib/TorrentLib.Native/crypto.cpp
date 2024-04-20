#pragma once
#include "error.hpp"
#include <array>
#include <cassert>
#include <libtorrent/kademlia/ed25519.hpp>

API int ed25519_create_keypair(const char* seed, int seed_len, char* pk, char* sk)
{
	try
	{
		std::array<char, 32> seed_arr;
		std::copy_n(seed, 32, seed_arr.begin());
		auto const [pk_tmp, sk_tmp] = libtorrent::dht::ed25519_create_keypair(seed_arr);
		memcpy(pk, &pk_tmp.bytes[0], pk_tmp.len);
		memcpy(sk, &sk_tmp.bytes[0], sk_tmp.len);
		return 0;
	}
	catch (const lt::system_error& err)
	{
		return make_error_code(err.code());
	}
}

API int ed25519_sign(const char* msg, int msg_len, char* pk, const char* sk, char* sig)
{
	try
	{
		lt::dht::signature lt_sig = libtorrent::dht::ed25519_sign({ msg, msg_len }, lt::dht::public_key{pk}, lt::dht::secret_key{sk});
		memcpy(sig, lt_sig.bytes.data(), 64);
		return 0;
	}
	catch (const lt::system_error& err)
	{
		return make_error_code(err.code());
	}
}

API int ed25519_verify(bool* result, char* msg, int msg_len, const char* pk, const char* sig)
{
	try
	{
		*result = lt::dht::ed25519_verify(lt::dht::signature{sig}, { msg, msg_len }, lt::dht::public_key{ pk });
		return 0;
	}
	catch (const lt::system_error& err)
	{
		return make_error_code(err.code());
	}
}
