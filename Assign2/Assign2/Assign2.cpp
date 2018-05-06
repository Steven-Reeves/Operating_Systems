// Steven Reeves
// Assignment 2 
// CST_352
// Assign2.cpp

#include <iostream>
#include "MemoryPool.h"
#include "StringQueue.h"



class PoolFactory
{
public:
	virtual MemoryPool * Create(unsigned int size) = 0;
};

class FirstFitFactory: public PoolFactory
{
public:
	virtual MemoryPool * Create(unsigned int size) { return new FirstFitPool(size); }
};

class BestFitFactory : public PoolFactory
{
public:
	virtual MemoryPool * Create(unsigned int size) { return new BestFitPool(size); }
};

void TestPool(PoolFactory * factory);


int main()
{
	std::cout << "Test FirstFitPool......" << std::endl;
	TestPool(new FirstFitFactory());

	std::cout << "Test BestFitPool......" << std::endl;
	TestPool(new BestFitFactory());
	//TestStringQueue();

	return 0;
}
void TestStringQueue()
{
	// Test String Queue with FirstFitPool

	// Set pool size to small number to catch StringQueue's OutOfMemoryException
	// Test FirstFitPool at 7 bytes, exception thrown. Test success!
	FirstFitPool pool(100);
	StringQueue q(&pool);
	pool.DebugPrint();
	q.Insert("foo");
	pool.DebugPrint();
	q.Insert("bar");
	pool.DebugPrint();
	char * s1 = q.Peek();
	std::cout << "s1 = " << s1 << std::endl;
	q.Remove();
	pool.DebugPrint();
	char * s2 = q.Peek();
	std::cout << "s2 = " << s2 << std::endl;
	q.Remove();
	pool.DebugPrint();

}
void TestPool(PoolFactory * factory)
{

	MemoryPool * pool1 = factory->Create(100);
	pool1->DebugPrint();
	void * block1 = pool1->Allocate(9);
	pool1->DebugPrint();
	void * block2 = pool1->Allocate(1);
	pool1->DebugPrint();
	pool1->Free(block1);
	pool1->DebugPrint();
	void * block3 = pool1->Allocate(5);
	pool1->DebugPrint();
	pool1->Free(block2);
	pool1->DebugPrint();
	pool1->Free(block3);
	pool1->DebugPrint();

	// Allocating out of memory bounds
	try
	{
		MemoryPool * pool2 = factory->Create(100);
		void * blockBig = pool2->Allocate(101);
		pool2->Free(blockBig);
	}
	catch (OutOfMemoryException)
	{
		std::cout << "Out of memory!" << std::endl;
	}

	try
	{
		MemoryPool * pool3 = factory->Create(100);
		void * block1 = pool3->Allocate(50);
		pool3->DebugPrint();
		void * block2 = pool3->Allocate(60);

	}
	catch (OutOfMemoryException)
	{
		std::cout << "Out of memory!" << std::endl;
	}

	// Allocating to fragmented space
	try
	{
		MemoryPool * pool4 = factory->Create(100);
		void * block1 = pool4->Allocate(10);
		pool4->DebugPrint();
		void * block2 = pool4->Allocate(10);
		pool4->DebugPrint();
		void * block3 = pool4->Allocate(10);
		pool4->DebugPrint();
		pool4->Free(block1);
		pool4->DebugPrint();
		pool4->Allocate(75);

	}
	catch (OutOfMemoryException)
	{
		std::cout << "Out of memory!" << std::endl;
	}


	try
	{

		MemoryPool * pool5 = factory->Create(100);
		pool5->DebugPrint();
		void * block1 = pool5->Allocate(9);
		pool5->DebugPrint();
		void * block2 = pool5->Allocate(1);
		pool5->DebugPrint();
		void * block3 = pool5->Allocate(6);
		pool5->DebugPrint();
		void * block4 = pool5->Allocate(1);
		pool5->DebugPrint();
		pool5->Free(block3);
		pool5->DebugPrint();
		pool5->Free(block1);
		pool5->DebugPrint();
		void * block5 = pool5->Allocate(5);
		pool5->DebugPrint();
		pool5->Free(block2);
		pool5->DebugPrint();
		pool5->Free(block3);
		pool5->DebugPrint();
	}
	catch (OutOfMemoryException)
	{
		std::cout << "Out of memory!" << std::endl;
	}

}

