using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentDictionaryTest
{
    class Program
    {
        static ConcurrentDictionary<CustomCacheKey, bool> _dict = new ConcurrentDictionary<CustomCacheKey, bool>();
        static void Main(string[] args)
        {
            Console.WriteLine(" ------------------------------------------------------------------------------------------------------------------ ");
            Console.WriteLine("This application was created to show time differences of iterating over ConcurrentDictionary Values using two methods.");
            Console.WriteLine();
            Console.WriteLine(" one that LOCKS other threads :");
            Console.WriteLine();
            Console.WriteLine(" ->  var values = concurrentDictionary.Values ; ");
            Console.WriteLine();
            Console.WriteLine(" And ");
            Console.WriteLine();
            Console.WriteLine(" one that DO NOT LOCKS other threads :");
            Console.WriteLine();
            Console.WriteLine(" -> var value = concurrentDictionary.Select(pair => pair.Value); ");
            Console.WriteLine();
            Console.WriteLine("Docs: https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getenumerator?redirectedfrom=MSDN&view=netframework-4.8#System_Collections_Concurrent_ConcurrentDictionary_2_GetEnumerator");
            Console.WriteLine(" ------------------------------------------------------------------------------------------------------------------ ");
            Console.WriteLine();

            int itemsInCache = 3000000;
            int numberOfThreads = 100;

            Console.WriteLine(" NUMBER OF ENTIES IN DICTIONARY : " + itemsInCache);
            Console.WriteLine(" NUMBER OF THREADS: " + numberOfThreads);
            Console.WriteLine();
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();

            FillDictionary(itemsInCache);

            Console.WriteLine();
            Console.WriteLine("Iterate over dictionary using   dictionary.Select(pair=> pair.Value)   : ");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();

            var notLockedTime = RunTasks(numberOfThreads, (dict) => dict.Select(pair => pair.Value).ToList());

            Console.WriteLine();
            Console.WriteLine("Iterate over dictionary using   dictionary.Values   : ");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();

            var lockedTime = RunTasks(numberOfThreads, (dict) => dict.Values);

            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
        }

        private static void FillDictionary(int itemsInCache)
        {
            CustomCacheKey key;
            Console.WriteLine("Filling up the dictionary...");
            for (int i = 0; i < itemsInCache; i++)
            {
                key = new CustomCacheKey(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "en-us", "web");
                _dict.TryAdd(key, false);
                if (i % 100000 == 0)
                {
                    Console.WriteLine("Items added: " + i);
                }
            }

            Console.WriteLine("Dictionary filled");
        }

        private static long RunTasks(int numberOfThreads, Func<ConcurrentDictionary<CustomCacheKey, bool>, IEnumerable<bool>> valuesFunc)
        {
            var totalStopwatch = Stopwatch.StartNew();

            IList<Task> tasks = new List<Task>();
            ConcurrentQueue<int> taskExecutionOrder = new ConcurrentQueue<int>();
            ConcurrentQueue<int> initialTaskExecutionOrder = new ConcurrentQueue<int>();
            long sumOfExecutinTimes = 0;
            for (int taskId = 0; taskId < numberOfThreads; taskId++)
            {
                var id = taskId;

                var task = new Task(() =>
                {
                    initialTaskExecutionOrder.Enqueue(id);
                    var stopwatch = Stopwatch.StartNew();
                    long counter = 0;
                    Console.WriteLine(id + " - Iterating over dictionary values using Values property");
                    var values = valuesFunc(_dict);
                    Console.WriteLine(id + " - values retrived : " + stopwatch.ElapsedMilliseconds + " [ms]");
                    foreach (var dictValue in values)
                    {
                        counter++;
                        if (dictValue)
                        {
                        }
                    }

                    taskExecutionOrder.Enqueue(id);
                    stopwatch.Stop();
                    sumOfExecutinTimes += stopwatch.ElapsedMilliseconds;
                    Console.WriteLine(id + " - Iterating over - " + stopwatch.ElapsedMilliseconds + " [ms] ");
                });

                tasks.Add(task);
            }

            tasks.ToList().ForEach(t=>t.Start());

            Task.WaitAll(tasks.ToArray());

            var order = string.Join(" , ", taskExecutionOrder.ToArray());
            var initialOrder = string.Join(" , ", initialTaskExecutionOrder.ToArray());

            totalStopwatch.Stop();
            Console.WriteLine();
            Console.WriteLine(" --------------------------------------------------------------------");
            Console.WriteLine(" TOTAL PROCESSING TIME : " + totalStopwatch.ElapsedMilliseconds + " [ms]");
            Console.WriteLine();
            Console.WriteLine(" Initial Order of task creation : " + initialOrder);
            Console.WriteLine(" Order of task execution : " + order);
            Console.WriteLine();
            Console.WriteLine(" Avarage execution time : " + (int)(sumOfExecutinTimes / numberOfThreads) + "[ms]");
            Console.WriteLine();
            Console.WriteLine(" ---------------------------------------------------------------------");

            return totalStopwatch.ElapsedMilliseconds;
        }
    }

    public class CustomCacheKey
    {
        private bool cacheable = true;
        private readonly string _object1Id;
        private readonly string _object2Id;
        private readonly string language;
        private readonly string database;

        public string Object1Id
        {
            get { return this._object1Id; }
        }

        public string Object2Id
        {
            get { return this._object2Id; }
        }

        public string Langauge
        {
            get { return this.language; }
        }

        public string Database
        {
            get { return this.database; }
        }

        public CustomCacheKey(
            string object1Id,
            string object2Id,
            string language,
            string database)
        {
            this._object1Id = object1Id;
            this._object2Id = object2Id;
            this.language = language;
            this.database = database;
        }

        public bool Equals(CustomCacheKey cacheKey)
        {
            if (object.ReferenceEquals((object)null, (object)cacheKey))
                return false;
            if (object.ReferenceEquals((object)this, (object)cacheKey))
                return true;
            if (string.Equals(cacheKey.Object2Id, this.Object2Id, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(cacheKey.Object1Id, this.Object1Id, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(cacheKey.Langauge, this.Langauge, StringComparison.OrdinalIgnoreCase))
                return string.Equals(cacheKey.Database, this.Database, StringComparison.OrdinalIgnoreCase);
            return false;
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals((object)null, obj))
                return false;
            if (object.ReferenceEquals((object)this, obj))
                return true;
            if (obj.GetType() == typeof(CustomCacheKey))
                return this.Equals((CustomCacheKey)obj);
            return false;
        }

        public override int GetHashCode()
        {
            return (this.Object1Id != null ? this.Object1Id.GetHashCode() : 0) * 397 ^
                   (this.Object2Id != null ? this.Object2Id.GetHashCode() : 0) ^
                   (this.Langauge != null ? this.Langauge.GetHashCode() : 0) ^
                   (this.Database != null ? this.Database.GetHashCode() : 0);
        }

        public override string ToString()
        {
            return this.Database + "-" + this.Object1Id + "-" + this.Langauge + "-" + this.Object2Id + "-";
        }

    }
}
