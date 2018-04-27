// Steven Reeves
// Assignment 2 
// CST_352
// Assign2.cpp

#include <iostream>
#include "MemoryPool.h"
int main()
{
	FirstFitPool pool(100);
	pool.DebugPrint();
	void * block1 = pool.Allocate(5);
	pool.DebugPrint();
	pool.Free(block1);
	pool.DebugPrint();

    return 0;
}

