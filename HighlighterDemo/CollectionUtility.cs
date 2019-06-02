namespace Grimoire
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Text;
	using IEnumerable = System.Collections.IEnumerable;
	using IEnumerator = System.Collections.IEnumerator;

#if GRIMOIRELIB
	public
#else
	internal
#endif
	static partial class CollectionUtility
	{
		#region ListDictionary
		/// <summary>
		/// Represents an ordered, unindexed dictionary over a list.
		/// </summary>
		public sealed class ListDictionary<TKey,TValue> : IDictionary<TKey,TValue>,IList<KeyValuePair<TKey,TValue>>
		{
			IList<KeyValuePair<TKey, TValue>> _inner = new List<KeyValuePair<TKey,TValue>>();

			public KeyValuePair<TKey, TValue> this[int index] { get => _inner[index]; set => _inner[index] = value; }
			public TValue this[TKey key] {
				get {
					int c = _inner.Count;
					for(int i =0;i<c;++i)
					{
						var kvp = _inner[i];
						if (kvp.Key.Equals(key))
							return kvp.Value;
					}
					throw new KeyNotFoundException();
				}
				set {
					int c = _inner.Count;
					for (int i = 0; i < c; ++i)
					{
						var kvp = _inner[i];
						if (kvp.Key.Equals(key))
						{
							_inner[i] = new KeyValuePair<TKey, TValue>(key, value);
							break;
						}
					}
					throw new KeyNotFoundException();
				}

			}
			private static void _ThrowReadOnly() { throw new InvalidOperationException("The list is read-only"); }
			public int Count => _inner.Count;

			public bool IsReadOnly => _inner.IsReadOnly;

			public ICollection<TKey> Keys {
				get {
					return _EnumKeys().AsCollection();
				}
			}
			private IEnumerable<TKey> _EnumKeys()
			{
				int c = _inner.Count;
				for(int i =0;i<c;++i)
					yield return _inner[i].Key;
			}
			public ICollection<TValue> Values 
			{
				get {
					return _EnumValues().AsCollection();
				}
			}
			private IEnumerable<TValue> _EnumValues()
			{
				int c = _inner.Count;
				for (int i = 0; i < c; ++i)
					yield return _inner[i].Value;
			}
			public void Add(KeyValuePair<TKey, TValue> item)
			{
				_inner.Add(item);
			}

			public void Add(TKey key, TValue value)
			{
				Add(new KeyValuePair<TKey, TValue>(key, value));
			}

			public void Clear()
			{
				_inner.Clear();
			}

			public bool Contains(KeyValuePair<TKey, TValue> item)
			{
				return _inner.Contains(item);
			}

			public bool ContainsKey(TKey key)
			{
				int c = _inner.Count;
				for (int i = 0; i < c; ++i)
				{
					var kvp = _inner[i];
					if (kvp.Key.Equals(key))
						return true;
				}
				return false;
			}

			public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
			{
				_inner.CopyTo(array, arrayIndex);
			}

			public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
			{
				return _inner.GetEnumerator();
			}

			public int IndexOf(KeyValuePair<TKey, TValue> item)
			{
				return _inner.IndexOf(item);
			}

			public void Insert(int index, KeyValuePair<TKey, TValue> item)
			{
				_inner.Insert(index, item);
			}

			public bool Remove(KeyValuePair<TKey, TValue> item)
			{
				return _inner.Remove(item);
			}

			public bool Remove(TKey key)
			{
				int c = _inner.Count;
				for (int i = 0; i < c; ++i)
				{
					var kvp = _inner[i];
					if (kvp.Key.Equals(key))
					{
						_inner.RemoveAt(i);
						return true;
					}
				}
				return false;
			}

			public void RemoveAt(int index)
			{
				_inner.RemoveAt(index);
			}

			public bool TryGetValue(TKey key, out TValue value)
			{
				int c = _inner.Count;
				for (int i = 0; i < c; ++i)
				{
					var kvp = _inner[i];
					if (kvp.Key.Equals(key))
					{
						value = kvp.Value;
						return true;
					}
				}
				value = default(TValue);
				return false;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return _inner.GetEnumerator();
			}
		}
		#endregion ListDictionary
		#region CollectionAdapter
		internal sealed class CollectionAdapter : ICollection
		{
			internal CollectionAdapter(IEnumerable inner)
			{
				_inner = inner;
			}
			readonly IEnumerable _inner;
			public int Count { get { return Count(_inner); } }
			public bool IsSynchronized { get { return false; } }
			object ICollection.SyncRoot { get { return this; } }

			public void CopyTo(Array array, int index)
			{
				CollectionUtility.CopyTo(_inner,array, index);
			}

			public IEnumerator GetEnumerator()
			{
				return _inner.GetEnumerator();
			}
		}
		internal sealed class CollectionAdapter<T> : ICollection<T>
		{
			internal CollectionAdapter(IEnumerable<T> inner)
			{
				_inner = inner;
			}
			readonly IEnumerable<T> _inner;
			public int Count { get { return Count(_inner); } }

			public bool IsReadOnly { get { return true; } }

			public void CopyTo(T[] array, int index)
			{
				CollectionUtility.CopyTo(_inner, array, index);
			}

			public IEnumerator<T> GetEnumerator()
			{
				return _inner.GetEnumerator();
			}

			void ICollection<T>.Add(T item)
			{
				throw new NotSupportedException("The collection is read-only.");
			}
			bool ICollection<T>.Remove(T item)
			{
				throw new NotSupportedException("The collection is read-only.");
			}
			void ICollection<T>.Clear()
			{
				throw new NotSupportedException("The collection is read-only.");
			}

			public bool Contains(T item)
			{
				return Contains<T>(_inner, item);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return _inner.GetEnumerator();
			}
		}
		#endregion CollectionAdapter
		#region ListAdapter
		sealed class ListAdapter : IList
		{
			readonly IEnumerable _inner;
			internal ListAdapter(IEnumerable inner)
			{
				_inner = inner;
			}
			static void _ThrowReadOnly()
			{
				throw new NotSupportedException("The list is read only.");
			}
			public object this[int index] { get { return GetAt(_inner, index); } set { _ThrowReadOnly(); } }

			bool IList.IsFixedSize { get { return true; } }
			bool IList.IsReadOnly { get { return true; } }
			public int Count { get { return Count(_inner); } }
			bool ICollection.IsSynchronized { get { return false; } }
			object ICollection.SyncRoot { get { return this; } }
			int IList.Add(object value)
			{
				_ThrowReadOnly();
				return -1;
			}
			void IList.Clear()
			{
				_ThrowReadOnly();
			}

			public bool Contains(object value)
			{
				return _inner.Contains(value);
			}

			public void CopyTo(Array array, int index)
			{
				_inner.CopyTo(array, index);
			}

			public IEnumerator GetEnumerator()
			{
				return _inner.GetEnumerator();
			}

			public int IndexOf(object value)
			{
				return _inner.IndexOf(value);
			}

			void IList.Insert(int index, object value)
			{
				_ThrowReadOnly();
			}

			void IList.Remove(object value)
			{
				_ThrowReadOnly();
			}

			void IList.RemoveAt(int index)
			{
				_ThrowReadOnly();
			}
		}
		sealed class ListAdapter<T> : IList<T>
		{
			IEnumerable<T> _inner;
			internal ListAdapter(IEnumerable<T> inner)
			{
				_inner = inner;
			}
			static void _ThrowReadOnly()
			{
				throw new NotSupportedException("The list is read only.");
			}
			public T this[int index] { get { return GetAt(_inner, index); } set { _ThrowReadOnly(); } }

			public int Count { get { return Count(_inner); } }
			bool ICollection<T>.IsReadOnly { get { return true; } }

			void ICollection<T>.Add(T item)
			{
				_ThrowReadOnly();
			}

			void ICollection<T>.Clear()
			{
				_ThrowReadOnly();
			}

			public bool Contains(T item)
			{
				return _inner.Contains(item);
			}

			public void CopyTo(T[] array, int arrayIndex)
			{
				_inner.CopyTo(array, arrayIndex);
			}

			public IEnumerator<T> GetEnumerator()
			{
				return _inner.GetEnumerator();
			}

			public int IndexOf(T item)
			{
				return _inner.IndexOf(item);
			}

			void IList<T>.Insert(int index, T item)
			{
				_ThrowReadOnly();
			}

			bool ICollection<T>.Remove(T item)
			{
				_ThrowReadOnly();
				return false;
			}

			void IList<T>.RemoveAt(int index)
			{
				_ThrowReadOnly();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return _inner.GetEnumerator();
			}
		}
		#endregion ListAdapter
		#region ReadOnlyAdapter 
		sealed class ReadOnlyCollectionAdapter : ICollection
		{
			internal ReadOnlyCollectionAdapter(ICollection inner)
			{
				_inner = inner;
			}
			readonly ICollection _inner;
			public int Count { get { return _inner.Count; } }
			bool ICollection.IsSynchronized { get { return false; } }
			object ICollection.SyncRoot { get { return this; } }

			public void CopyTo(Array array, int index)
			{
				_inner.CopyTo(array, index);
			}

			public IEnumerator GetEnumerator()
			{
				return _inner.GetEnumerator();
			}
		}
		sealed class ReadOnlyCollectionAdapter<T> : ICollection<T>
		{
			static void _ThrowReadOnly()
			{
				throw new NotSupportedException("The collection is read-only.");
			}
			internal ReadOnlyCollectionAdapter(ICollection<T> inner)
			{
				_inner = inner;
			}
			readonly ICollection<T> _inner;

			public int Count { get { return _inner.Count; } }
			public bool IsReadOnly { get { return true; } }

			void ICollection<T>.Add(T item)
			{
				_ThrowReadOnly();
			}

			public void Clear()
			{
				_ThrowReadOnly();
			}

			public bool Contains(T item)
			{
				return _inner.Contains(item);
			}

			public void CopyTo(T[] array, int arrayIndex)
			{
				_inner.CopyTo(array, arrayIndex);
			}

			bool ICollection<T>.Remove(T item)
			{
				_ThrowReadOnly();
				return false;
			}

			public IEnumerator<T> GetEnumerator()
			{
				return _inner.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return _inner.GetEnumerator();
			}
		}
		sealed class ReadOnlyListAdapter : IList
		{
			static void _ThrowReadOnly()
			{
				throw new NotSupportedException("The collection is read-only.");
			}
			internal ReadOnlyListAdapter(IList inner)
			{
				_inner = inner;
			}
			readonly IList _inner;

			public object this[int index] { get { return _inner[index]; } set { _ThrowReadOnly(); } }

			public bool IsFixedSize { get { return true; } }
			public bool IsReadOnly { get { return true; } }
			public int Count { get { return _inner.Count; } }
			bool ICollection.IsSynchronized { get { return false; } }
			object ICollection.SyncRoot { get { return this; } }

			int IList.Add(object value)
			{
				_ThrowReadOnly();
				return -1;
			}

			void IList.Clear()
			{
				_ThrowReadOnly();
			}

			public bool Contains(object value)
			{
				return _inner.Contains(value);
			}

			public void CopyTo(Array array, int index)
			{
				_inner.CopyTo(array, index);
			}

			public IEnumerator GetEnumerator()
			{
				return _inner.GetEnumerator();
			}

			public int IndexOf(object value)
			{
				return _inner.IndexOf(value);
			}

			void IList.Insert(int index, object value)
			{
				_ThrowReadOnly();
			}

			void IList.Remove(object value)
			{
				_ThrowReadOnly();
			}

			void IList.RemoveAt(int index)
			{
				_ThrowReadOnly();
			}
		}
		sealed class ReadOnlyListAdapter<T> : IList<T>
		{
			static void _ThrowReadOnly()
			{
				throw new NotSupportedException("The collection is read-only.");
			}
			internal ReadOnlyListAdapter(IList<T> inner)
			{
				_inner = inner;
			}

			readonly IList<T> _inner;
			public T this[int index] { get { return _inner[index]; } set { _ThrowReadOnly(); } }

			public int Count { get { return _inner.Count; } }
			public bool IsReadOnly { get { return true; } }

			void ICollection<T>.Add(T item)
			{
				_ThrowReadOnly();
			}

			void ICollection<T>.Clear()
			{
				_ThrowReadOnly();
			}

			public bool Contains(T item)
			{
				return _inner.Contains(item);
			}

			public void CopyTo(T[] array, int arrayIndex)
			{
				_inner.CopyTo(array, arrayIndex);
			}

			public IEnumerator<T> GetEnumerator()
			{
				return _inner.GetEnumerator();
			}

			public int IndexOf(T item)
			{
				return _inner.IndexOf(item);
			}

			void IList<T>.Insert(int index, T item)
			{
				_ThrowReadOnly();
			}

			bool ICollection<T>.Remove(T item)
			{
				_ThrowReadOnly();
				return false;
			}

			void IList<T>.RemoveAt(int index)
			{
				_ThrowReadOnly();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return _inner.GetEnumerator();
			}
		}
		#endregion ReadOnlyAdapter
		public static ICollection AsCollection(this IEnumerable collection)
		{
			var result = collection as ICollection;
			if (null != result) return result;
			result = new CollectionAdapter(collection);
			return result;
		}
		public static ICollection<T> AsCollection<T>(this IEnumerable<T> collection)
		{
			var result = collection as ICollection<T>;
			if (null != result) return result;
			result = new CollectionAdapter<T>(collection);
			return result;
		}
		public static IList AsList(this IEnumerable collection)
		{
			var result = collection as IList;
			if (null != result) return result;
			result = new ListAdapter(collection);
			return result;
		}
		public static IList<T> AsList<T>(this IEnumerable<T> collection)
		{
			var result = collection as IList<T>;
			if (null != result) return result;
			result = new ListAdapter<T>(collection);
			return result;
		}
		public static IList AsReadOnly(IList list) { return list.IsReadOnly?list:new ReadOnlyListAdapter(list); }
		public static ICollection AsReadOnly(ICollection collection) { return new ReadOnlyCollectionAdapter(collection); }

		public static IList<T> AsReadOnly<T>(IList<T> list) { return list.IsReadOnly ? list : new ReadOnlyListAdapter<T>(list); }
		public static ICollection<T> AsReadOnly<T>(ICollection<T> collection) { return collection.IsReadOnly? collection:new ReadOnlyCollectionAdapter<T>(collection); }

		/// <summary>
		/// Tests whether the enumeration is null or empty.
		/// </summary>
		/// <param name="collection">The enumeration to test</param>
		/// <returns>True if the enumeration is null, or if enumerating ends before the first element. Otherwise, this method returns true.</returns>
		/// <remarks>For actual collections, testing the "Count" property should be slightly faster.</remarks>
		public static bool IsNullOrEmpty(this IEnumerable collection)
		{
			if (null == collection) return true;
			var e = collection.GetEnumerator();
			try
			{
				return !e.MoveNext();
			}
			finally
			{
				var d = e as IDisposable;
				if (null != d)
				{
					d.Dispose();
					d = null;
				}
				e = null;
			}
		}
		public static object First(this IEnumerable collection)
		{
			var e = collection.GetEnumerator();
			try
			{
				if (e.MoveNext())
					return e.Current;
				else
					throw new ArgumentException("The collection was empty.", "collection");
			}
			finally
			{
				var d = e as IDisposable;
				if (null != d)
				{
					d.Dispose();
					d = null;
				}
				e = null;
			}
		}
		public static bool TryFirst(this IEnumerable collection,out object first)
		{
			var e = collection.GetEnumerator();
			try
			{
				if (e.MoveNext())
				{
					first = e.Current;
					return true;
				}
				first = null;
				return false;
			}
			finally
			{
				var d = e as IDisposable;
				if (null != d)
				{
					d.Dispose();
					d = null;
				}
				e = null;
			}
		}
		public static T First<T>(this IEnumerable<T> collection)
		{
			using (var e = collection.GetEnumerator())
				if (e.MoveNext())
					return e.Current;
				else
					throw new ArgumentException("The collection was empty.", "collection");
		}
		public static bool TryFirst<T>(this IEnumerable<T> collection,out T first)
		{
			using (var e = collection.GetEnumerator())
				if (e.MoveNext())
				{
					first = e.Current;
					return true;
				}
				else
				{
					first = default(T);
					return false;
				}
		}
		public static bool HasSingleItem(this IEnumerable collection)
		{
			var e = collection.GetEnumerator();
			try
			{
				return e.MoveNext() && !e.MoveNext();
			}
			finally
			{
				var d = e as IDisposable;
				if (null != d)
					d.Dispose();
			}
		}
		public static IEnumerable<T> SubRange<T>(this IEnumerable<T> collection,int start=0,int count = 0)
		{
			var i = 0;
			var c = 0;
			foreach (var item in collection)
			{
				if (i >= start)
				{
					yield return item;
					++c;
				}
				if (0 < count && c == count)
					break;
				++i;
			}
			if (i < start)
				throw new ArgumentOutOfRangeException("start");
			if (0 < count && c < count)
				throw new ArgumentOutOfRangeException("count");
		}
		public static bool StartsWith<T>(this IEnumerable<T> collection,IEnumerable<T> values,IEqualityComparer<T> equalityComparer=null)
		{
			if (null == equalityComparer)
				equalityComparer = EqualityComparer<T>.Default;
			using(var x = collection.GetEnumerator())
			{
				using(var y = values.GetEnumerator())
				{
					while(y.MoveNext())
					{
						if (!x.MoveNext())
							return false;
						if (!equalityComparer.Equals(x.Current, y.Current))
							return false;
					}
				}
			}
			return true;
		}
		public static IList<T> GetLongestCommonPrefix<T>(this IEnumerable<IList<T>> ss)
		{
			IList<T> result = null;
			foreach(var list in ss)
			{
				foreach(var list2 in ss)
				{
					if (!ReferenceEquals(list, list2))
					{
						var pfx = GetCommonPrefix<T>(new IList<T>[] { list, list2 });
						if (null == result || (null != pfx && pfx.Count > result.Count))
							result = pfx;
					}
				}
			}
			if (null == result) return new T[0];
			return result;
		}
		public static IList<T> GetCommonPrefix<T>(this IEnumerable<IList<T>> ss)
		{
			// adaptation of solution found here: https://stackoverflow.com/questions/33709165/get-common-prefix-of-two-string
			if (ss.IsNullOrEmpty())
				return new T[0];
			var first = ss.First();
			if (ss.HasSingleItem())
				return first;

			int prefixLength = 0;

			foreach (object item in ss.First())
			{
				foreach (IList<T> s in ss)
				{
					if (s.Count <= prefixLength || !Equals(s[prefixLength], item))
					{
						var result = new T[prefixLength];
						for (var i = 0; i < result.Length; i++)
							result[i] = first[i];

						return result;
					}
				}
				++prefixLength;
			}

			return first; // all strings identical up to length of ss[0]
		}
		/// <summary>
		/// Attempts to add a unique item to a collection.
		/// </summary>
		/// <typeparam name="T">The element type of the collection and type of the item to add</typeparam>
		/// <param name="collection">The collection to add the item to</param>
		/// <param name="item">The item to add to the collection</param>
		/// <returns>True if the item was added, false if it already exists.</returns>
		public static bool TryAddUnique<T>(this ICollection<T> collection, T item)
		{
			if (null == collection)
				throw new ArgumentNullException("collection");
			if (!collection.Contains(item))
			{
				collection.Add(item);
				return true;
			}
			return false;
		}
		public static void AddRange<T>(this ICollection<T> collection,IEnumerable<T> values)
		{
			foreach (var item in values)
				collection.Add(item);
		}
		/// <summary>
		/// Creates an array of element type T from the items in the specified source collection, also of type T
		/// </summary>
		/// <typeparam name="T">The element type of the array and the collection</typeparam>
		/// <param name="source">The source collection</param>
		/// <returns>A new array with the values copied from the source.</returns>
		public static T[] ToArray<T>(this ICollection<T> source) {
			var arr = source as T[];
			if (null != arr) return arr;
			var result = new T[source.Count];
			source.CopyTo(result, 0);
			return result;
		}
		/// <summary>
		/// Creates an array of element type T from the items in the specified source enumeration, also of type T
		/// </summary>
		/// <typeparam name="T">The element type of the array and the enumeration</typeparam>
		/// <param name="source">The source enumeration</param>
		/// <returns>A new array with the values copied from the source.</returns>
		public static T[] ToArray<T>(this IEnumerable<T> source)
		{
			var arr = source as T[];
			if (null != arr) return arr;
			var result = new List<T>(source);
			return result.ToArray();
		}

		public static Array ToArray(this ICollection source,Type elementType)
		{
			var al = new ArrayList(source);
			return al.ToArray(elementType);
		}
		public static object[] ToArray(this ICollection source)
		{
			var al = new ArrayList(source);
			return al.ToArray();
		}
		public static Array ToArray(this IEnumerable source, Type elementType)
		{
			var al = new ArrayList();
			foreach(object o in source)
				al.Add(o);
			return al.ToArray(elementType);
		}
		public static object[] ToArray(this IEnumerable source)
		{
			var al = new ArrayList();
			foreach (object o in source)
				al.Add(o);
			return al.ToArray();
		}

		/// <summary>
		/// Copies an enumeration of type T to the specified array of type T, starting at the specified start index.
		/// </summary>
		/// <typeparam name="T">The element type of the array and enumeration</typeparam>
		/// <param name="source">The enumeration to copy from</param>
		/// <param name="destination">The array to copy to</param>
		/// <param name="destinationStartIndex">The start index at which copying begins</param>
		/// <returns>The count of items copied. This will be the same as the number of items in the enumeration.</returns>
		public static int CopyTo<T>(this IEnumerable<T> source,T[] destination,int destinationStartIndex)
		{
			if (null == source)
				throw new ArgumentNullException("value");
			if (null == destination)
				throw new ArgumentNullException("array");
			if (destinationStartIndex < destination.GetLowerBound(0) || destinationStartIndex > destination.GetUpperBound(0))
				throw new ArgumentOutOfRangeException("startIndex");
			int i = destinationStartIndex;
			foreach(T v in source)
			{
				destination[i] = v;
				++i;
			}
			return i;
		}
		/// <summary>
		/// Copies an enumeration to the specified array, starting at the specified start index.
		/// </summary>
		/// <param name="source">The enumeration to copy from</param>
		/// <param name="destination">The array to copy to</param>
		/// <param name="destinationStartIndex">The start index at which copying begins</param>
		/// <returns>The count of items copied. This will be the same as the number of items in the enumeration.</returns>
		public static int CopyTo(this IEnumerable source, Array destination, int destinationStartIndex)
		{
			if (null == source)
				throw new ArgumentNullException("value");
			if (null == destination)
				throw new ArgumentNullException("array");
			if (destinationStartIndex < destination.GetLowerBound(0) || destinationStartIndex > destination.GetUpperBound(0))
				throw new ArgumentOutOfRangeException("startIndex");
			int i = destinationStartIndex;
			foreach (object v in source)
			{
				destination.SetValue(v, i);
				++i;
			}
			return i;
		}
		public static IEnumerable<T> Cast<T>(this IEnumerable collection)
		{
			foreach(object o in collection)
			{
				yield return (T)o;
			}
		}
		public static IEnumerable<T> Convert<T>(this IEnumerable collection)
		{
			Type t = typeof(T);
			foreach (object o in collection)
			{
				yield return (T)System.Convert.ChangeType(o, t);
			}
		}
		public static int IndexOf(this IEnumerable collection, object item)
		{
			int result = 0;
			foreach (object cmp in collection)
			{
				if (Equals(item, cmp))
					return result;
				++result;
			}
			return -1;
		}
		public static int IndexOf(this IEnumerable collection, object item,IEqualityComparer comparer)
		{
			if (null == comparer)
				return IndexOf(collection, item);
			int result = 0;
			foreach (object cmp in collection)
			{
				if (comparer.Equals(item, cmp))
					return result;
				++result;
			}
			return -1;
		}
		public static int IndexOf<T>(this IEnumerable<T> collection, T item)
		{
			int result = 0;
			foreach(T cmp in collection)
			{
				if (Equals(item, cmp))
					return result;
				++result;
			}
			return -1;
		}
		public static int IndexOf<T>(this IEnumerable<T> collection, T item,IEqualityComparer<T> comparer)
		{
			if (null == comparer)
				return IndexOf(collection, item);
			int result = 0;
			foreach (T cmp in collection)
			{
				if (comparer.Equals(item, cmp))
					return result;
				++result;
			}

			return -1;
		}
		public static IEnumerable<TOutput> ForEach<TInput, TOutput>(this IEnumerable<TInput> collection, Func<TInput, TOutput> @do)
		{
			foreach (var item in collection)
				yield return @do(item);
		}
		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Func<T, T> @do)
		{
			foreach (var item in collection)
				yield return @do(item);
		}
		public static IEnumerable<T> Select<T>(this IEnumerable<T> axis, Func<T,bool> predicate)
		{
			foreach (var item in axis)
				if (predicate(item))
					yield return item;
		}
		public static IEnumerable<T> Unique<T>(this IEnumerable<T> collection)
		{
			var seen = new HashSet<T>();
			foreach(var item in collection)
				if (seen.Add(item))
					yield return item;
			seen.Clear();
		}
		public static bool Contains(this IEnumerable collection, object item)
		{
			foreach (object cmp in collection)
				if (Equals(item, cmp))
					return true;
			return false;
		}
		public static bool Contains(this IEnumerable collection, object item,IEqualityComparer comparer)
		{
			if (null == comparer)
				return Contains(collection, item);
			foreach (object cmp in collection)
				if (comparer.Equals(item, cmp))
					return true;
			return false;
		}
		public static bool Contains<T>(this IEnumerable<T> collection, T item)
		{
			foreach (T cmp in collection)
				if (Equals(item, cmp))
					return true;
			return false;
		}
		public static bool Contains<T>(this IEnumerable<T> collection, T item,IEqualityComparer<T> comparer)
		{
			if (null == comparer)
				return Contains(collection, item);
			foreach (T cmp in collection)
				if (comparer.Equals(item,cmp))
					return true;
			return false;
		}
		public static int Count(this IEnumerable collection)
		{
			IEnumerator e = collection.GetEnumerator();
			try
			{
				int result = 0;
				while (e.MoveNext())
					++result;
				return result;
			}
			finally
			{
				var d = e as IDisposable;
				if (null != d)
				{
					d.Dispose();
					d = null;
				}
				e = null;
			}
		}
		public static int MoveNext(this IEnumerator enumerator, int count)
		{
			try
			{
				int result = 0;
				while (result<count && enumerator.MoveNext())
					++result;
				return result;
			}
			finally
			{
				var d = enumerator as IDisposable;
				if (null != d)
				{
					d.Dispose();
					d = null;
				}
				enumerator = null;
			}
		}
		public static object GetAt(this IEnumerable collection, int index)
		{
			var e = collection.GetEnumerator();
			try
			{
				int i = -1;
				while (i < index && e.MoveNext())
					++i;
				if(i<index)
					throw new ArgumentOutOfRangeException("index");
				return e.Current;
			}
			finally
			{
				var d = e as IDisposable;
				if (null != d)
				{
					d.Dispose();
					d = null;
				}
				e = null;
			}
		}
		public static T GetAt<T>(this IEnumerable<T> collection, int index)
		{
			using (var e = collection.GetEnumerator())
			{
				int i = -1;
				while (i < index && e.MoveNext())
					++i;
				if(i<index)
					throw new ArgumentOutOfRangeException("index");
				return e.Current;
			}
				
		}
		public static IEnumerable GetKeys(this IEnumerable collection)
		{
			foreach (DictionaryEntry de in collection)
				yield return de.Key;
		}
		public static IEnumerable GetValues(this IEnumerable collection)
		{
			foreach (DictionaryEntry de in collection)
				yield return de.Value;
		}
		public static IEnumerable<TKey> GetKeys<TKey,TValue>(this IEnumerable<KeyValuePair<TKey,TValue>> collection)
		{
			foreach (var kvp in collection)
				yield return kvp.Key;
		}
		public static IEnumerable<TValue> GetValues<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> collection)
		{
			foreach (var kvp in collection)
				yield return kvp.Value;
		}
		public static IEnumerable<T> NonNulls<T>(this IEnumerable<T> collection)
		{
			foreach (var item in collection)
				if (null != item)
					yield return item;
		}
		public static IEnumerable NonNulls(this IEnumerable collection)
		{
			foreach (var item in collection)
				if (null != item)
					yield return item;
		}
		public static IEnumerable<Type> GetTypes(this IEnumerable collection)
		{
			foreach (var item in collection)
				if (null != item)
					yield return item.GetType();
				else
					yield return null;
		}
		public static Type InferElementType(this IEnumerable items)
		{
			Type result = _GetCommonType(NonNulls(GetTypes(items)));
			if(typeof(object)!=result && Contains(items,null))
			{
				if (result.IsValueType)
					result = typeof(object);
			}
			return result;
		}
		static Type _GetCommonType(IEnumerable<Type> types)
		{
			// based on a solution @ https://stackoverflow.com/questions/353430/easiest-way-to-get-a-common-base-class-from-a-collection-of-types
			List<Type> temp = new List<Type>(types);
			if (0==temp.Count)
				return (typeof(object));
			else if (1==temp.Count)
				return (temp[0]);

			bool checkPass = false;

			Type tested = null;

			while (!checkPass)
			{
				tested = temp[0];

				checkPass = true;

				for (int i = 1; i < temp.Count; i++)
				{
					if (tested.Equals(temp[i]))
						continue;
					else
					{
						// If the tested common basetype (current) is the indexed type's base type
						// then we can continue with the test by making the indexed type to be its base type
						if (tested.Equals(temp[i].BaseType))
						{
							temp[i] = temp[i].BaseType;
							continue;
						}
						// If the tested type is the indexed type's base type, then we need to change all indexed types
						// before the current type (which are all identical) to be that base type and restart this loop
						else if (tested.BaseType.Equals(temp[i]))
						{
							for (int j = 0; j <= i - 1; j++)
							{
								temp[j] = temp[j].BaseType;
							}

							checkPass = false;
							break;
						}
						// The indexed type and the tested type are not related
						// So make everything from index 0 up to and including the current indexed type to be their base type
						// because the common base type must be further back
						else
						{
							for (int j = 0; j <= i; j++)
							{
								temp[j] = temp[j].BaseType;
							}

							checkPass = false;
							break;
						}
					}
				}

				// If execution has reached here and checkPass is true, we have found our common base type, 
				// if checkPass is false, the process starts over with the modified types
			}

			// There's always at least object
			return tested;
		}
		public static bool Equals<T>(this IList<T> lhs,IList<T> rhs)
		{
			if (object.ReferenceEquals(lhs, rhs))
				return true;
			else if (object.ReferenceEquals(null, lhs) || object.ReferenceEquals(null,rhs))
				return false;
			int c = lhs.Count;
			if (c != rhs.Count) return false;
			for(int i = 0;i<c;++i)
				if (!object.Equals(lhs[i] , rhs[i]))
					return false;
			return true;
		}
		public static bool Equals<T>(this ICollection<T> lhs, ICollection<T> rhs)
		{
			if (ReferenceEquals(lhs, rhs))
				return true;
			else if (ReferenceEquals(null, lhs) || ReferenceEquals(null, rhs))
				return false;
			if (lhs.Count != rhs.Count)
				return false;
			using (var xe = lhs.GetEnumerator())
			using (var ye = rhs.GetEnumerator())
				while (xe.MoveNext() && ye.MoveNext())
					if (!rhs.Contains(xe.Current) || !lhs.Contains(ye.Current))
						return false;
			return true;
		}
		public static bool Equals<TKey,TValue>(this ICollection<KeyValuePair<TKey, TValue>> lhs, ICollection<KeyValuePair<TKey, TValue>> rhs)
		{
			if (ReferenceEquals(lhs, rhs))
				return true;
			else if (ReferenceEquals(null, lhs) || ReferenceEquals(null, rhs))
				return false;
			if (lhs.Count != rhs.Count)
				return false;
			using (var xe = lhs.GetEnumerator())
			using (var ye = rhs.GetEnumerator())
				while (xe.MoveNext() && ye.MoveNext())
					if (!rhs.Contains(xe.Current) || !lhs.Contains(ye.Current))
						return false;
			return true;
		}
		public static int GetHashCode<T>(this IEnumerable<T> collection)
		{
			int result = 0;
			if (!ReferenceEquals(null, collection))
			{
				foreach (T o in collection)
				{
					if (!ReferenceEquals(null, o))
					{
						result ^= o.GetHashCode();
					}
				}
			}
			return result;
		}
		public static int GetHashCode<T>(this IList<T> lhs)
		{
			if (object.ReferenceEquals(null,lhs))
				return int.MinValue;
			int result = 0;
			int c = lhs.Count;
			for (int i = 0; i < c; ++i)
				if (!object.ReferenceEquals(null, lhs[i]))
					result ^= lhs[i].GetHashCode();
			return result;
		}

		public static IEnumerable Concat(this IEnumerable lhs, IEnumerable rhs)
		{
			foreach (var v in lhs)
				yield return v;
			foreach (var v in rhs)
				yield return v;
		}
		public static IEnumerable<T> Concat<T>(this IEnumerable<T> lhs, IEnumerable<T> rhs)
		{
			foreach (var v in lhs)
				yield return v;
			foreach (var v in rhs)
				yield return v;
		}
		public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue @default=default(TValue))
		{
			TValue result;
			if (dictionary.TryGetValue(key, out result))
				return result;
			return @default;
		}
		public static string ToString(this IEnumerable e)
		{
			var sb = new StringBuilder();
			sb.Append("{ ");
			var d = "";
			bool appended = false;
			foreach(var i in e)
			{
				sb.Append(d);
				sb.Append(i);
				d = ", ";
				appended = true;
			}
			if (appended)
				sb.Append(" }");
			else
				sb.Append("}");
			return sb.ToString();
		} 
		public static IEnumerable<IList<T>> Split<T>(this IEnumerable<T> collection,T delim,IEqualityComparer<T> equalityComparer = null)
		{
			if (null == equalityComparer)
				equalityComparer = EqualityComparer<T>.Default;
			if (collection.IsNullOrEmpty())
				yield break;
			var l = new List<T>();
			foreach (var item in collection)
			{
				if(!equalityComparer.Equals(item,delim))
				{
					l.Add(item);
				} else
				{
					yield return l;
					l = new List<T>();
					
				}
			}
			yield return l;
		}
		public static IEnumerable<T> Join<T>(this IEnumerable<IList<T>> segments, IEnumerable<T> delim)
		{
			if (IsNullOrEmpty(delim))
			{
				foreach (var l in segments)
				{
					var ic = l.Count;
					for (var i = 0; i < ic; ++i)
						yield return l[i];
				}
				yield break;
			}
			var first = true;
			foreach(var l in segments)
			{
				if (first)
					first = false;
				else
					foreach (var i in delim)
						yield return i;
				var ic = l.Count;
				for (var i = 0; i < ic; ++i)
					yield return l[i];
			}
		}
		public static IEnumerable<T> Replace<T>(this IEnumerable<T> collection,T oldValue, IEnumerable<T> newValues, IEqualityComparer<T> equalityComparer = null)
		{
			return Join(Split(collection, oldValue,equalityComparer), newValues);
		}

	}
}
