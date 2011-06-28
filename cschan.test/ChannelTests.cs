using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace cschan.test
{
    [TestFixture]
    public class ChannelTests
    {
        [Test]
        public void should_produce_and_consume_the_channel_in_order()
        {
            const int iteration = 100000;
            const int capacity = 100;
            var c = new Channel<int>(capacity);

            var startFlag = new ManualResetEvent(false);

            var t1 = new Thread(() =>
                                    {
                                        startFlag.WaitOne();
                                        for (int i = 0; i < iteration; i++)
                                            c.Put(i);
                                    });

            var t2 = new Thread(() =>
                                    {
                                        startFlag.WaitOne();
                                        for (int i = 0; i < iteration; i++)
                                            Assert.AreEqual(c.Get().Item, i);
                                    });
            
            t1.Start(); t2.Start();
            startFlag.Set();
            t1.Join(); t2.Join();
        }

        [Test]
        public void should_work_with_many_readers_and_writes()
        {
            const int numberOfThreads = 50;
            const int iteration = 1000;
            const int capacity = 100;
            var c = new Channel<int>(capacity);

            var writes = new List<Thread>();
            var readers= new List<Thread>();

            var startFlag = new ManualResetEvent(false);

            for (int n = 0; n < numberOfThreads; n++)
                writes.Add(new Thread(() =>
                                          {
                                              startFlag.WaitOne();
                                              for (int i = 0; i < iteration; i++)
                                                  c.Put(0);
                                          }));

            for (int n = 0; n < numberOfThreads; n++)
                readers.Add(new Thread(() =>
                                           {
                                               startFlag.WaitOne();
                                               for (int i = 0; i < iteration; i++)
                                                   c.Get();
                                           }));


            foreach (var t in readers) t.Start(); foreach (var t in writes) t.Start();
            startFlag.Set();
            foreach (var t in readers) t.Join(); foreach (var t in writes) t.Join();
        }

        [Test]
        public void should_get_same_object_as_put()
        {
            var c = new Channel<int>(1);
            c.Put(1);
            Assert.AreEqual(1, c.Get().Item);
        }

        [Test]
        public void should_block_on_get_when_empty()
        {
            var executionChecker = new OrderedExecutionChecker();
            var c = new Channel<int>(1);
            var thread = new Thread(() =>
                                        {
                                            Thread.Sleep(100);
                                            Console.WriteLine("[{0}] 1", Thread.CurrentThread.ManagedThreadId);
                                            executionChecker.Executing(1);
                                            c.Put(1);
                                        });
            thread.Start();

            c.Get();
            executionChecker.Executing(2);

            thread.Join();

            Assert.That(executionChecker.IsInOrder());
        }

        [Test]
        public void should_block_on_put_when_full()
        {
            var executionChecker = new OrderedExecutionChecker();
            var c = new Channel<int>(1);
            c.Put(1);

            var thread = new Thread(() =>
                                        {
                                            Thread.Sleep(100);
                                            Console.WriteLine("[{0}] 1", Thread.CurrentThread.ManagedThreadId);
                                            executionChecker.Executing(1);
                                            c.Get();
                                        });
            thread.Start();

            c.Put(1);
            executionChecker.Executing(2);

            thread.Join();

            Assert.That(executionChecker.IsInOrder());
        }

        [Test]
        public void test_put()
        {
            var semaphore = new Semaphore(2,2);

            var t1 = new Thread(() => SemaphoreWait(semaphore,"Put 1"));
            var t2 = new Thread(() => SemaphoreWait(semaphore,"Put 2"));
            var t3 = new Thread(() => SemaphoreWait(semaphore,"Put 3"));
            t1.Start();
            t2.Start();
            t3.Start();

            Thread.Sleep(100);
            Console.WriteLine("Get 1: {0}", semaphore.Release());
            Thread.Sleep(100);
            Console.WriteLine("Get 2: {0}", semaphore.Release());
            Thread.Sleep(100);
            Console.WriteLine("Get 3: {0}", semaphore.Release());
            Assert.Throws<SemaphoreFullException>(() => Console.WriteLine("Get 4: {0}", semaphore.Release()));

            t1.Join();
            t2.Join();
            t3.Join();
        }

        [Test]
        public void test_get()
        {
            var semaphore = new Semaphore(0,7);

            var t1 = new Thread(() => SemaphoreWait(semaphore,"Get 1"));
            var t2 = new Thread(() => SemaphoreWait(semaphore,"Get 2"));
            var t3 = new Thread(() => SemaphoreWait(semaphore,"Get 3"));
            t1.Start();
            t2.Start();
            t3.Start();

            Thread.Sleep(100);
            Console.WriteLine("Put 1: {0}", semaphore.Release());
            Thread.Sleep(100);
            Console.WriteLine("Put 2: {0}", semaphore.Release());
            Thread.Sleep(100);
            Console.WriteLine("Put 3: {0}", semaphore.Release());
            Console.WriteLine("Put 4: {0}", semaphore.Release());
            Console.WriteLine("Put 5: {0}", semaphore.Release());
            Console.WriteLine("Put 6: {0}", semaphore.Release());
            Console.WriteLine("Put 7: {0}", semaphore.Release());

            t1.Join();
            t2.Join();
            t3.Join();
        }

        [Test]
        public void test_capacity_empty()
        {
            var semaphore = new Semaphore(0,4);

            Console.WriteLine("Put 1: {0}", semaphore.Release());
            Console.WriteLine("Put 2: {0}", semaphore.Release());
            Console.WriteLine("Put 3: {0}", semaphore.Release());
            Console.WriteLine("Put 4: {0}", semaphore.Release());
            Assert.Throws<SemaphoreFullException>(() => Console.WriteLine("Put 5: {0}", semaphore.Release()));
        }

        [Test]
        public void test_capacity_full()
        {
            var semaphore = new Semaphore(4,4);

            Assert.Throws<SemaphoreFullException>(() => Console.WriteLine("Put 1: {0}", semaphore.Release()));
        }

        [Test]
        public void test_capacity_full_then_used()
        {
            var semaphore = new Semaphore(3,4);

            Console.WriteLine("Put 1: {0}", semaphore.Release());

            var t1 = new Thread(() => SemaphoreWait(semaphore, "Get 1"));
            t1.Start();
            Thread.Sleep(100);

            Console.WriteLine("Put 2: {0}", semaphore.Release());
            Assert.Throws<SemaphoreFullException>(() => Console.WriteLine("Put 3: {0}", semaphore.Release()));

            t1.Join();
        }

        private static void SemaphoreWait(WaitHandle semaphore, string i)
        {
            semaphore.WaitOne();
            Console.WriteLine("{0} [{1}] WaitOne - After", i, Thread.CurrentThread.ManagedThreadId);
        }
    }

    public class OrderedExecutionChecker
    {
        private bool _endFlag;
        private readonly object _guard = new object();
        private readonly List<int> _orderList = new List<int>();
        
        public void Executing(int order)
        {
            lock (_guard)
            {
                if (_endFlag)
                    throw new Exception();

                if (_orderList.Count > 1000)
                    throw new Exception("Too many executions");

                _orderList.Add(order);
            }
        }

        public bool IsInOrder()
        {
            lock (_guard)
            {
                _endFlag = true;
            }

            int i = -1;
            foreach (var order in _orderList)
            {
                if (order <= i)
                    return false;
                i = order;
            }
            return true;
        }
    }
}