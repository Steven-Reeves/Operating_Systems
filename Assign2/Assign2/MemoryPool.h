#pragma once
// Steven Reeves
// Assignment 2 
// CST_352
// MemoryPool.h

#include <list>
#include <iostream>

class MemoryPool
{
protected:
	unsigned int poolSize;

public:
	MemoryPool(unsigned int poolSize) : poolSize(poolSize) {};
	virtual void * Allocate(unsigned int nBytes) = 0;
	virtual void Free(void * block) = 0;
	virtual void DebugPrint() = 0;
};

class FirstFitPool : MemoryPool
{
private:
	unsigned char * pool;
	struct block
	{
		unsigned int index;
		unsigned int size;
		bool isAllocated;
		// block(int index, int size, bool allocated) TODO: remove this
	};
	std::list<block> blocks;

public:
	FirstFitPool(unsigned int poolSize);
	virtual void * Allocate(unsigned int nBytes) ;
	virtual void Free(void * block);
	virtual void DebugPrint();

private:
	bool suitable_block(block b, unsigned int nBytes);
};
