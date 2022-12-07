﻿using System;
using System.Runtime.CompilerServices;

namespace CombatAI
{
	/// <summary>
	/// A faster implementation of a Queue based on arrays.
	/// </summary>    
	public class FastQueue<T>
	{
		private T[] nodes;
		private int start;
		private int end;
		private int count;

		/// <summary>
		/// Wether the queue is empty
		/// </summary>
		public bool IsEmpty
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => count == 0;
		}

		/// <summary>
		/// Number of elements queued.
		/// </summary>
		public int Count
		{
			get => count;
		}

		/// <summary>
		/// Allows list like access
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns>Item</returns>
		public T this[int index]
		{
			get => nodes[(start + index) % count];
			set => nodes[(start + index) % count] = value;
		}

		private FastQueue() { }
		/// <summary>
		/// Constructor for FastQueue.
		/// </summary>
		/// <param name="initialSize">The starting size of the internal array.</param>
		public FastQueue(int initialSize = 64)
		{
			nodes = new T[initialSize];
		}

		/// <summary>
		/// Enqueue new elements into the queue.
		/// </summary>
		/// <param name="element"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Enqueue(T element)
		{
			count++;
			nodes[end++] = element;
			if (end == start)
			{
				Expand();
			}
			if (end >= nodes.Length)
			{
				end = 0;
			}
		}

		/// <summary>
		/// Dequeue new elements from the queue.
		/// </summary>
		/// <param name="element"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Dequeue()
		{
			count--;
			T val = nodes[start];
			nodes[start++] = default(T);
			if (start >= nodes.Length)
			{
				start = 0;
			}
			return val;
		}

		/// <summary>
		///  Clear all elements in the queue.
		/// </summary>
		/// <param name="deep">Whether to do a deep clear.</param>
		public void Clear(bool deep = false)
		{
			if (deep)
			{
				for (int i = 0; i < count; i++)
				{
					nodes[(start + i) % count] = default(T);
				}
			}
			start = 0;
			end   = 0;
			count = 0;
		}

		/// <summary>
		/// Will expand the node array so more items can be added
		/// </summary>
		private void Expand()
		{
			T[] temp = new T[nodes.Length * 4];
			Array.Copy(nodes, start, temp, start, nodes.Length - start);
			Array.Copy(nodes, 0, temp, nodes.Length, end);
			end   = start + nodes.Length;
			nodes = temp;
		}
	}
}
