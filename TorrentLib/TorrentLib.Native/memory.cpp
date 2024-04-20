#include "memory.hpp"

API void* memory_alloc(int size)
{
	return operator new(size);
}

API void memory_free(void* ptr)
{
	operator delete(ptr);
}
