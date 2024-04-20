#pragma once
struct session;

API int dht_gen_keypair(char* seed, int seed_size, char* pk, char* sk);
API int dht_get_mutable_item(session* session, const char* pk, const char* salt, int salt_size);
API int dht_put_mutable_item(session* session, const char* pk, const char* sk, const char* salt, int salt_size, const char* data, int data_size);
API int dht_announce_mutable_item(session* session, const char* pk, const char* salt, int salt_size, const char* sig, const char* data, int data_size, long long item_sequence);