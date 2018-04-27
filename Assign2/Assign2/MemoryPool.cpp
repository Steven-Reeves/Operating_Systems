// Steven Reeves
// Assignment 2 
// CST_352
// MemoryPool.cpp

#include "MemoryPool.h"


FirstFitPool::FirstFitPool(unsigned int poolSize) : MemoryPool(poolSize)
{
	pool = new unsigned char[poolSize];
	blocks.push_back({0, poolSize, false });	// one single free block
}

bool FirstFitPool::suitable_block(block b, unsigned int nBytes)
{
	return !b.isAllocated && nBytes <= b.size;
}

void * FirstFitPool::Allocate(unsigned int nBytes)
{
	// get first chuck that is at least nBytes
	// allocate it
	// return it

	unsigned int i;
	for (i = 0; i < blocks.size() && !suitable_block(blocks[i], nBytes); i++);

	if (i < blocks.size())
	{
		block b = blocks[i];
		void * address = (pool + b.index);

		// Split the block
		blocks.push_back({ b.index + nBytes, b.size - nBytes, false });

		b.isAllocated = true;
		b.size = nBytes;

		blocks[i] = b;
		return address;
			
	}

	// TODO: Throw exception here
	return nullptr;
}

void FirstFitPool::Free(void * block)
{

}

void FirstFitPool::DebugPrint()
{
	std::cout << "FirstFitPool, " << poolSize << " bytes:" << std::endl;

	for each (block b in blocks)
	{
		// TODO: add tabs
		std::cout << b.index << ", " << b.size << ", " << b.isAllocated << std::endl;
	}
}