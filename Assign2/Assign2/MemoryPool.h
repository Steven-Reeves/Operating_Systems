#pragma once
// Steven Reeves
// Assignment 2 
// CST_352
// MemoryPool.h

#include <list>
#include <iostream>

class OutOfMemoryException 
{

};

class MemoryPool
{
protected:
	unsigned int poolSize;
	unsigned char * pool;
	struct block
	{
		unsigned int index;
		unsigned int size;
		bool isAllocated;
	};
	std::list<block> blocks;

public:
	MemoryPool(unsigned int poolSize);
	virtual void * Allocate(unsigned int nBytes) = 0;
	virtual void Free(void * block);
	virtual void DebugPrint();
protected:
	virtual char * ClassName() = 0;
};

class FirstFitPool : public MemoryPool
{
public:
	FirstFitPool(unsigned int poolSize);
	virtual void * Allocate(unsigned int nBytes) ;

protected:
	virtual char * ClassName() { return "FirstFitPool"; }

private:
	bool suitable_block(block b, unsigned int nBytes);
};

class BestFitPool : public MemoryPool
{

public:
	BestFitPool(unsigned int poolSize);
	virtual void * Allocate(unsigned int nBytes);

protected:
	virtual char * ClassName() { return "BestFitPool"; }
};
