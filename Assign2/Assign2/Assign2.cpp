// Steven Reeves
// Assignment 2 
// CST_352
// Assign2.cpp

#include <iostream>
#include "MemoryPool.h"
#include "StringQueue.h"

int main()
{
	// TestFirstFillPool
	FirstFitPool pool(100);
	StringQueue q(&pool);
	q.Insert("foo");
	q.Insert("peek");
	char * s1 = q.Peek();
	std::cout << "s1 = " << s1 << std::endl;
	q.Remove();
	char * s2 = q.Peek();
	std::cout << "s2 = " << s2 << std::endl;
	q.Remove();


	return 0;
}

void TestFirstFitPool()
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

	// Allocating out of memory bounds
	try
	{
		FirstFitPool pool2(100);
		void * blockBig = pool2.Allocate(101);
		pool2.Free(blockBig);
	}
	catch (OutOfMemoryException)
	{
		std::cout << "Out of memory!" << std::endl;
	}

	// Allocating to fragmented space
	try
	{
		FirstFitPool pool3(100);
		void * block1 = pool3.Allocate(10);
		pool3.DebugPrint();
		void * block2 = pool3.Allocate(10);
		pool3.DebugPrint();
		void * block3 = pool3.Allocate(10);
		pool3.DebugPrint();
		pool3.Free(block1);
		pool3.DebugPrint();
		pool3.Allocate(75);

	}
	catch (OutOfMemoryException)
	{
		std::cout << "Out of memory!" << std::endl;
	}

}

