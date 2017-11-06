using System;

namespace SharpBCI {

	/**
	 * Weiss implementation of Circular Buffer with addition of indexability
	 */
	public class IndexableQueue<T> {
		
		T[] arr;
		int start;
		int len;

		public IndexableQueue() : this(10) { }

		public IndexableQueue(int size) { 
			arr = new T[size];
			start = 0;
			len = 0;
		}

		public void Enqueue(T v) {
			if (len == arr.Length) {
				T[] newArr = new T[arr.Length * 2];
				for (int i = 0; i < len; i++) {
					newArr[i] = arr[(start + i) % len];
				}
				start = 0;
				arr = newArr;
			}
			arr[(start + len) % arr.Length] = v;
			len++;
		}

		public T Dequeue() {
			var v = arr[start % arr.Length];
			start++;
			len--;
			return v;
		}

		public int Count { get { return len; } }

		public T this[int idx] { 
			get {
				return arr[(start + idx) % arr.Length];
			}
		}
	}
}
