using System;
using System.Collections;
using System.Collections.Generic;

namespace experimental
{
    public class ClaimCheck<T> : IEnumerable<T>
    {
        private const int DEFAULT_CAPACITY = 16;

        private int size = 0;

        private int usedSize = 0;

        private int lastValidIndex = 0;

        private readonly List<Element> elements;

        public ClaimCheck(in int capacity = DEFAULT_CAPACITY) => elements = new List<Element>(capacity);

        public ClaimCheck(in ClaimCheck<T> other)
        {
            this.size = other.size;
            this.usedSize = other.usedSize;
            this.lastValidIndex = other.lastValidIndex;
            this.elements = new List<Element>(other.elements);
        }

        public int Size { get { return usedSize; } }

        public int Add(in T x)
        {
            int newElementIndex;
            if (size == 0)
            {
                Element elem = new Element();
                elements.Add(elem);
                elem.prevIndex = 0;
                elem.nextIndex = 1;
                newElementIndex = 0;
                size = 1;
            }
            else if (usedSize == 0)
            {
                newElementIndex = lastValidIndex;
            }
            else if (elements[lastValidIndex].nextIndex == size)
            {
                Element elem = new Element();
                elements.Add(elem);
                elem.prevIndex = lastValidIndex;
                elem.nextIndex = size + 1;
                newElementIndex = size;
                lastValidIndex = newElementIndex;
                ++size;
            }
            else
            {
                newElementIndex = elements[lastValidIndex].nextIndex;
                lastValidIndex = newElementIndex;
            }
            usedSize += 1;
            elements[newElementIndex].x = x;
            return newElementIndex;
        }

        public T Get(in int id)
        {
            checkIdExists(id);
            return elements[id].x;
        }

        public void Clear()
        {
            int curIndex = lastValidIndex;
            while (usedSize > 0)
            {
                int nextIndex = elements[curIndex].prevIndex; 
                elements[curIndex] = new Element(elements[curIndex]);
                curIndex = nextIndex;
                --usedSize;
            }
            lastValidIndex = curIndex;
        }

        public T Remove(in int id)
        {
            checkIdExists(id);

            T removedElement = elements[id].x;
            elements[id] = new Element(elements[id]);
            
            if (elements[id].prevIndex != id)
            {
                elements[elements[id].prevIndex].nextIndex = elements[id].nextIndex;
            }

            if (id != lastValidIndex)
            {
                if (elements[id].prevIndex == id)
                {
                    elements[elements[id].nextIndex].prevIndex = elements[id].nextIndex;
                } 
                else
                {
                    elements[elements[id].nextIndex].prevIndex = elements[id].prevIndex;
                }
            }

            --usedSize;
            if (usedSize == 0) return removedElement;

            if (id != lastValidIndex)
            {
                elements[id].prevIndex = lastValidIndex;
                elements[id].nextIndex = elements[lastValidIndex].nextIndex;

                if (elements[lastValidIndex].nextIndex < size) {
                    elements[elements[lastValidIndex].nextIndex].prevIndex = id;
                }
                elements[lastValidIndex].nextIndex = id;
            }
            else
            {
                lastValidIndex = elements[lastValidIndex].prevIndex;
                elements[lastValidIndex].nextIndex = id;
            }
            return removedElement;
        }

        private void checkIdExists(in int id)
        {
            if ((id >= size) || (elements[id].x == null))
            {
                throw new InvalidOperationException(String.Format("No element at Id={0} exists.", id));
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (usedSize > 0)
            {
                for (int i = lastValidIndex; i != elements[i].prevIndex; i = elements[i].prevIndex)
                {
                    yield return elements[i].x;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class Element
        {
            internal T x;
            internal int prevIndex;
            internal int nextIndex;
            internal Element() { }
            internal Element(in Element other)
            {
                this.nextIndex = other.nextIndex;
                this.prevIndex = other.prevIndex;
            }
        }
    }
}
