// Steven Reeves
// Assignment 2 
// CST_352
// StringQueue.cpp

#include "StringQueue.h"
#include <string.h>

StringQueue::StringQueue(MemoryPool * pool): pool(pool)
{
	
}

void StringQueue::Insert(char * s)
{
	// If pool is full, throw exception
	try {

		//copy string into pool
		unsigned int blocksize = (unsigned int)(strlen(s) + 1);
		char * block = (char *)pool->Allocate(blocksize);
		strcpy_s(block, blocksize, s);

		theQ.push(block);
	}
	catch (OutOfMemoryException)
	{
		throw FullException();
	}
}

char * StringQueue::Peek()
{
	char * s = theQ.front();

	return s;
}

void StringQueue::Remove()
{
	// get string at front
	char * s = theQ.front();

	// free from pool
	pool->Free(s);

	// Remove from queue
	theQ.pop();

}
