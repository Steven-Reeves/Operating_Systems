#pragma once
// Steven Reeves
// Assignment 2 
// CST_352
// StringQueue.h

#include <queue>
#include "MemoryPool.h"

class FullException
{

};

class StringQueue
{
private:
	MemoryPool * pool;
	std::queue<char *> theQ;

public:
	StringQueue(MemoryPool * pool);
	void Insert(char * s);
	char * Peek();
	void Remove();
};
