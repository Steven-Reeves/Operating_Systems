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
	void * block1 = pool.Allocate(9);
	pool.DebugPrint();
	void * block2 = pool.Allocate(1);
	pool.DebugPrint();
	pool.Free(block1);
	pool.DebugPrint();
	void * block3 = pool.Allocate(5);
	pool.DebugPrint();
	pool.Free(block2);
	pool.DebugPrint();
	pool.Free(block3);
	pool.DebugPrint();

    return 0;
}

