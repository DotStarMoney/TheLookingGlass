using System;
using System.Collections;
using System.Collections.Generic;

namespace TheLookingGlass.Util
{
    public class ClaimCheck<T> : IEnumerable<T>
    {
        private const int DefaultCapacity = 16;

        private readonly List<Element> _elements;

        private int _lastValidIndex;

        private int _size;

        public ClaimCheck(in int capacity = DefaultCapacity)
        {
            _elements = new List<Element>(capacity);
        }

        public ClaimCheck(in ClaimCheck<T> other)
        {
            _size = other._size;
            Count = other.Count;
            _lastValidIndex = other._lastValidIndex;
            _elements = new List<Element>(other._elements);
        }

        public int Count { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            if (Count <= 0) yield break;
            var i = _lastValidIndex;
            for (;;)
            {
                yield return _elements[i].X;
                if (i == _elements[i].PrevIndex) yield break;
                i = _elements[i].PrevIndex;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Add(in T x)
        {
            int newElementIndex;
            if (_size == 0)
            {
                var elem = new Element();
                _elements.Add(elem);
                elem.PrevIndex = 0;
                elem.NextIndex = 1;
                newElementIndex = 0;
                _size = 1;
            }
            else if (Count == 0)
            {
                newElementIndex = _lastValidIndex;
            }
            else if (_elements[_lastValidIndex].NextIndex == _size)
            {
                var elem = new Element();
                _elements.Add(elem);
                elem.PrevIndex = _lastValidIndex;
                elem.NextIndex = _size + 1;
                newElementIndex = _size;
                _lastValidIndex = newElementIndex;
                ++_size;
            }
            else
            {
                newElementIndex = _elements[_lastValidIndex].NextIndex;
                _lastValidIndex = newElementIndex;
            }

            Count += 1;
            _elements[newElementIndex].X = x;
            return newElementIndex;
        }

        public T Get(in int id)
        {
            CheckIdExists(id);
            return _elements[id].X;
        }

        public void Clear()
        {
            var curIndex = _lastValidIndex;
            while (Count > 0)
            {
                var nextIndex = _elements[curIndex].PrevIndex;
                _elements[curIndex] = new Element(_elements[curIndex]);
                curIndex = nextIndex;
                --Count;
            }

            _lastValidIndex = curIndex;
        }

        public T Remove(in int id)
        {
            CheckIdExists(id);

            var removedElement = _elements[id].X;
            _elements[id] = new Element(_elements[id]);

            if (_elements[id].PrevIndex != id)
            {
                _elements[_elements[id].PrevIndex].NextIndex = _elements[id].NextIndex;
            }

            if (id != _lastValidIndex)
            {
                _elements[_elements[id].NextIndex].PrevIndex = _elements[id].PrevIndex == id
                    ? _elements[id].NextIndex
                    : _elements[id].PrevIndex;
            }

            --Count;
            if (Count == 0) return removedElement;

            if (id != _lastValidIndex)
            {
                _elements[id].PrevIndex = _lastValidIndex;
                _elements[id].NextIndex = _elements[_lastValidIndex].NextIndex;

                if (_elements[_lastValidIndex].NextIndex < _size)
                {
                    _elements[_elements[_lastValidIndex].NextIndex].PrevIndex = id;
                }

                _elements[_lastValidIndex].NextIndex = id;
            }
            else
            {
                _lastValidIndex = _elements[_lastValidIndex].PrevIndex;
                _elements[_lastValidIndex].NextIndex = id;
            }

            return removedElement;
        }

        private void CheckIdExists(in int id)
        {
            if (id >= _size || _elements[id].X == null)
            {
                throw new InvalidOperationException($"No element at Id={id} exists.");
            }
        }

        private class Element
        {
            internal int NextIndex;
            internal int PrevIndex;
            internal T X;

            internal Element() { }

            internal Element(in Element other)
            {
                NextIndex = other.NextIndex;
                PrevIndex = other.PrevIndex;
            }
        }
    }
}
