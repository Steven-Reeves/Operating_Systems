// Steven Reeves
// Assignment 2 
// CST_352
// MemoryPool.cpp

#include "MemoryPool.h"
#include <iostream>

MemoryPool::MemoryPool(unsigned int poolSize) : poolSize(poolSize)
{
	pool = new unsigned char[poolSize];
	blocks.push_back({ 0, poolSize, false });	// one single free block
}

void MemoryPool::Free(void * address)
{
	// Get index into pool
	unsigned int index = (unsigned int)((unsigned char *)address - pool);

	for (std::list<block>::iterator i = blocks.begin(); i != blocks.end(); i++)
	{
		if (i->index == index)
		{
			i->isAllocated = false;

			//combine with previous available block if possible
			if (i != blocks.begin())
			{
				std::list<block>::iterator prev = i;
				prev--;
				if (!prev->isAllocated)
				{
					i->index = prev->index;
					i->size += prev->size;

					blocks.erase(prev);
				}

			}
			std::list<block>::iterator next = i;
			next++;

			//combine with next available block
			if (next != blocks.end() && !next->isAllocated)
			{
				i->size += next->size;
				//next block is out
				blocks.erase(next);
			}
			return;
		}
		else
		{
			// TODO throw exception
		}
	}
}


void MemoryPool::DebugPrint()
{
	std::cout << ClassName() <<", " << poolSize << " bytes:" << std::endl;

	for each (block b in blocks)
	{

		std::cout << "\t" << b.index << ", " << b.size << ", " << (b.isAllocated ? "Allocated!" : "Free!") << std::endl;
	}
}

FirstFitPool::FirstFitPool(unsigned int poolSize) : MemoryPool(poolSize)
{
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

	std::list<block>::iterator i = blocks.end();

	for (i = blocks.begin(); i != blocks.end() && !suitable_block(*i, nBytes); i++);

	if (i != blocks.end())
	{

		void * address = (pool + i->index);

		// Split the block
		block child = { i->index + nBytes, i->size - nBytes, false };

		i->isAllocated = true;
		i->size = nBytes;

		i++;
		blocks.insert(i, child);

		return address;
			
	}

	throw OutOfMemoryException();

	return nullptr;
}




BestFitPool::BestFitPool(unsigned int poolSize) : MemoryPool(poolSize)
{
}


void * BestFitPool::Allocate(unsigned int nBytes)
{
	// get first chunck that is closest to nBytes
	// allocate it
	// return it

	std::list<block>::iterator best = blocks.end();

	for (std::list<block>::iterator i = blocks.begin(); i != blocks.end(); i++)
	{
		if (!i->isAllocated && i->size >= nBytes)
		{
			if(best == blocks.end() || i->size < best->size)
				best = i;
		}
			
	}

	if (best != blocks.end())
	{

		void * address = (pool + best->index);

		// Split the block
		block child = { best->index + nBytes, best->size - nBytes, false };

		best->isAllocated = true;
		best->size = nBytes;

		best++;
		blocks.insert(best, child);

		return address;

	}

	throw OutOfMemoryException();

	return nullptr;
}
