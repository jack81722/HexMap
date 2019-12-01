using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace HexMapServer
{
    public class IdGenerator
    {
        private int idCounter = 0;
        private ConcurrentQueue<int> idPool;
        private List<int> idTracker;

        public IdGenerator(int startIndex = 0)
        {
            idCounter = startIndex;
            idPool = new ConcurrentQueue<int>();
            idTracker = new List<int>();
        }

        public int NewId
        {
            get
            {
                if (idPool.TryDequeue(out int id))
                {
                    idTracker.Add(id);
                    return id;
                }
                int newId = idCounter++;
                idTracker.Add(newId);
                return newId++;
            }
        }

        public void RecycleId(int id)
        {
            lock (idTracker)
            {
                if (idTracker.Contains(id))
                {
                    idTracker.Remove(id);
                    idPool.Enqueue(id);
                }
                else
                {
                    throw new InvalidOperationException("Id is not be tracking.");
                }
            }
        }

        public void Reset()
        {
            foreach (int id in idTracker)
            {
                idPool.Enqueue(id);
            }
            idTracker.Clear();
        }
    }
}
