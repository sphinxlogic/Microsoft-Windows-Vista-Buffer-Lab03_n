namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis;
 
    /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection"]/*' /> 
    /// <summary>
    /// <para> 
    /// A collection that stores <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> objects.
    /// </para>
    /// </summary>
    /// <seealso cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> 
    [SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface")]
    public sealed class BehaviorServiceAdornerCollection : CollectionBase { 
        private BehaviorService behaviorService; 

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.BehaviorServiceAdornerCollection"]/*' /> 
        /// <summary>
        /// <para>
        /// Initializes a new instance of <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/>.
        /// </para> 
        /// </summary>
        public BehaviorServiceAdornerCollection(BehaviorService behaviorService) { 
            this.behaviorService = behaviorService; 
        }
 
        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.BehaviorServiceAdornerCollection1"]/*' />
        /// <summary>
        /// <para>
        /// Initializes a new instance of <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> based on another <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/>. 
        /// </para>
        /// </summary> 
        /// <param name='value'> 
        /// A <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> from which the contents are copied
        /// </param> 
        public BehaviorServiceAdornerCollection(BehaviorServiceAdornerCollection value) {
            this.AddRange(value);
        }
 
        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.BehaviorServiceAdornerCollection2"]/*' />
        /// <summary> 
        /// <para> 
        /// Initializes a new instance of <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> containing any array of <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> objects.
        /// </para> 
        /// </summary>
        /// <param name='value'>
        /// A array of <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> objects with which to intialize the collection
        /// </param> 
        public BehaviorServiceAdornerCollection(Adorner[] value) {
            this.AddRange(value); 
        } 

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.this"]/*' /> 
        /// <summary>
        /// <para>Represents the entry at the specified index of the <see cref='System.Windows.Forms.Design.Behavior.Adorner'/>.</para>
        /// </summary>
        /// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param> 
        /// <value>
        /// <para> The entry at the specified index of the collection.</para> 
        /// </value> 
        /// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
        public Adorner this[int index] { 
            get {
                return ((Adorner)(List[index]));
            }
            set { 
                List[index] = value;
            } 
        } 

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.Add"]/*' /> 
        /// <summary>
        /// <para>Adds a <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> with the specified value to the
        /// <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> .</para>
        /// </summary> 
        /// <param name='value'>The <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> to add.</param>
        /// <returns> 
        /// <para>The index at which the new element was inserted.</para> 
        /// </returns>
        /// <seealso cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection.AddRange'/> 
        public int Add(Adorner value) {
            value.BehaviorService = behaviorService;
            return List.Add(value);
        } 

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.AddRange"]/*' /> 
        /// <summary> 
        /// <para>Copies the elements of an array to the end of the <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/>.</para>
        /// </summary> 
        /// <param name='value'>
        /// An array of type <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> containing the objects to add to the collection.
        /// </param>
        /// <returns> 
        /// <para>None.</para>
        /// </returns> 
        /// <seealso cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection.Add'/> 
        public void AddRange(Adorner[] value) {
            for (int i = 0; (i < value.Length); i = (i + 1)) { 
                this.Add(value[i]);
            }
        }
 
        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.AddRange1"]/*' />
        /// <summary> 
        /// <para> 
        /// Adds the contents of another <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> to the end of the collection.
        /// </para> 
        /// </summary>
        /// <param name='value'>
        /// A <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> containing the objects to add to the collection.
        /// </param> 
        /// <returns>
        /// <para>None.</para> 
        /// </returns> 
        /// <seealso cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection.Add'/>
        public void AddRange(BehaviorServiceAdornerCollection value) { 
            for (int i = 0; (i < value.Count); i = (i + 1)) {
                this.Add(value[i]);
            }
        } 

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.Contains"]/*' /> 
        /// <summary> 
        /// <para>Gets a value indicating whether the
        /// <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> contains the specified <see cref='System.Windows.Forms.Design.Behavior.Adorner'/>.</para> 
        /// </summary>
        /// <param name='value'>The <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> to locate.</param>
        /// <returns>
        /// <para><see langword='true'/> if the <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> is contained in the collection; 
        /// otherwise, <see langword='false'/>.</para>
        /// </returns> 
        /// <seealso cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection.IndexOf'/> 
        public bool Contains(Adorner value) {
            return List.Contains(value); 
        }

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.CopyTo"]/*' />
        /// <summary> 
        /// <para>Copies the <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
        /// specified index.</para> 
        /// </summary> 
        /// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> .</para></param>
        /// <param name='index'>The index in <paramref name='array'/> where copying begins.</param> 
        /// <returns>
        /// <para>None.</para>
        /// </returns>
        /// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception> 
        /// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
        /// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception> 
        /// <seealso cref='System.Array'/> 
        public void CopyTo(Adorner[] array, int index) {
            List.CopyTo(array, index); 
        }

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.IndexOf"]/*' />
        /// <summary> 
        /// <para>Returns the index of a <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> in
        /// the <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> .</para> 
        /// </summary> 
        /// <param name='value'>The <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> to locate.</param>
        /// <returns> 
        /// <para>The index of the <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> of <paramref name='value'/> in the
        /// <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/>, if found; otherwise, -1.</para>
        /// </returns>
        /// <seealso cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection.Contains'/> 
        public int IndexOf(Adorner value) {
            return List.IndexOf(value); 
        } 

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.Insert"]/*' /> 
        /// <summary>
        /// <para>Inserts a <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> into the <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> at the specified index.</para>
        /// </summary>
        /// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param> 
        /// <param name=' value'>The <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> to insert.</param>
        /// <returns><para>None.</para></returns> 
        /// <seealso cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection.Add'/> 
        public void Insert(int index, Adorner value) {
            List.Insert(index, value); 
        }

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.GetEnumerator"]/*' />
        /// <summary> 
        /// <para>Returns an enumerator that can iterate through
        /// the <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> .</para> 
        /// </summary> 
        /// <returns><para>None.</para></returns>
        /// <seealso cref='System.Collections.IEnumerator'/> 
        public new BehaviorServiceAdornerCollectionEnumerator GetEnumerator() {
            return new BehaviorServiceAdornerCollectionEnumerator(this);
        }
 
        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.Remove"]/*' />
        /// <summary> 
        /// <para> Removes a specific <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> from the 
        /// <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> .</para>
        /// </summary> 
        /// <param name='value'>The <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> to remove from the <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> .</param>
        /// <returns><para>None.</para></returns>
        /// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
        public void Remove(Adorner value) { 
            List.Remove(value);
        } 
 
    }
 
    /// <include file='doc\BehaviorServiceAdornerCollectionEnumerator.uex' path='docs/doc[@for="BehaviorServiceAdornerCollectionEnumerator"]/*' />
    public class BehaviorServiceAdornerCollectionEnumerator : object, IEnumerator {

        private IEnumerator baseEnumerator; 

        private IEnumerable temp; 
 
        /// <include file='doc\BehaviorServiceAdornerCollectionEnumerator.uex' path='docs/doc[@for="BehaviorServiceAdornerCollectionEnumerator.BehaviorServiceAdornerCollectionEnumerator"]/*' />
        public BehaviorServiceAdornerCollectionEnumerator(BehaviorServiceAdornerCollection mappings) { 
            this.temp = ((IEnumerable)(mappings));
            this.baseEnumerator = temp.GetEnumerator();
        }
 
        /// <include file='doc\BehaviorServiceAdornerCollectionEnumerator.uex' path='docs/doc[@for="BehaviorServiceAdornerCollectionEnumerator.Current"]/*' />
        public Adorner Current { 
            get { 
                return ((Adorner)(baseEnumerator.Current));
            } 
        }

        /// <include file='doc\BehaviorServiceAdornerCollectionEnumerator.uex' path='docs/doc[@for="BehaviorServiceAdornerCollectionEnumerator.IEnumerator.Current"]/*' />
        /// <internalonly/> 
        object IEnumerator.Current {
            get { 
                return baseEnumerator.Current; 
            }
        } 

        /// <include file='doc\BehaviorServiceAdornerCollectionEnumerator.uex' path='docs/doc[@for="BehaviorServiceAdornerCollectionEnumerator.MoveNext"]/*' />
        public bool MoveNext() {
            return baseEnumerator.MoveNext(); 
        }
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorServiceAdornerCollectionEnumerator.IEnumerator.MoveNext"]/*' /> 
        /// <internalonly/>
        bool IEnumerator.MoveNext() { 
            return baseEnumerator.MoveNext();
        }

        /// <include file='doc\BehaviorServiceAdornerCollectionEnumerator.uex' path='docs/doc[@for="BehaviorServiceAdornerCollectionEnumerator.Reset"]/*' /> 
        public void Reset() {
            baseEnumerator.Reset(); 
        } 

        /// <include file='doc\BehaviorServiceAdornerCollectionEnumerator.uex' path='docs/doc[@for="BehaviorServiceAdornerCollectionEnumerator.IEnumerator.Reset"]/*' /> 
        /// <internalonly/>
        void IEnumerator.Reset() {
            baseEnumerator.Reset();
        } 
    }
 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics; 
    using System.Diagnostics.CodeAnalysis;
 
    /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection"]/*' /> 
    /// <summary>
    /// <para> 
    /// A collection that stores <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> objects.
    /// </para>
    /// </summary>
    /// <seealso cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> 
    [SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface")]
    public sealed class BehaviorServiceAdornerCollection : CollectionBase { 
        private BehaviorService behaviorService; 

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.BehaviorServiceAdornerCollection"]/*' /> 
        /// <summary>
        /// <para>
        /// Initializes a new instance of <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/>.
        /// </para> 
        /// </summary>
        public BehaviorServiceAdornerCollection(BehaviorService behaviorService) { 
            this.behaviorService = behaviorService; 
        }
 
        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.BehaviorServiceAdornerCollection1"]/*' />
        /// <summary>
        /// <para>
        /// Initializes a new instance of <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> based on another <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/>. 
        /// </para>
        /// </summary> 
        /// <param name='value'> 
        /// A <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> from which the contents are copied
        /// </param> 
        public BehaviorServiceAdornerCollection(BehaviorServiceAdornerCollection value) {
            this.AddRange(value);
        }
 
        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.BehaviorServiceAdornerCollection2"]/*' />
        /// <summary> 
        /// <para> 
        /// Initializes a new instance of <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> containing any array of <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> objects.
        /// </para> 
        /// </summary>
        /// <param name='value'>
        /// A array of <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> objects with which to intialize the collection
        /// </param> 
        public BehaviorServiceAdornerCollection(Adorner[] value) {
            this.AddRange(value); 
        } 

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.this"]/*' /> 
        /// <summary>
        /// <para>Represents the entry at the specified index of the <see cref='System.Windows.Forms.Design.Behavior.Adorner'/>.</para>
        /// </summary>
        /// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param> 
        /// <value>
        /// <para> The entry at the specified index of the collection.</para> 
        /// </value> 
        /// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
        public Adorner this[int index] { 
            get {
                return ((Adorner)(List[index]));
            }
            set { 
                List[index] = value;
            } 
        } 

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.Add"]/*' /> 
        /// <summary>
        /// <para>Adds a <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> with the specified value to the
        /// <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> .</para>
        /// </summary> 
        /// <param name='value'>The <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> to add.</param>
        /// <returns> 
        /// <para>The index at which the new element was inserted.</para> 
        /// </returns>
        /// <seealso cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection.AddRange'/> 
        public int Add(Adorner value) {
            value.BehaviorService = behaviorService;
            return List.Add(value);
        } 

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.AddRange"]/*' /> 
        /// <summary> 
        /// <para>Copies the elements of an array to the end of the <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/>.</para>
        /// </summary> 
        /// <param name='value'>
        /// An array of type <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> containing the objects to add to the collection.
        /// </param>
        /// <returns> 
        /// <para>None.</para>
        /// </returns> 
        /// <seealso cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection.Add'/> 
        public void AddRange(Adorner[] value) {
            for (int i = 0; (i < value.Length); i = (i + 1)) { 
                this.Add(value[i]);
            }
        }
 
        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.AddRange1"]/*' />
        /// <summary> 
        /// <para> 
        /// Adds the contents of another <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> to the end of the collection.
        /// </para> 
        /// </summary>
        /// <param name='value'>
        /// A <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> containing the objects to add to the collection.
        /// </param> 
        /// <returns>
        /// <para>None.</para> 
        /// </returns> 
        /// <seealso cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection.Add'/>
        public void AddRange(BehaviorServiceAdornerCollection value) { 
            for (int i = 0; (i < value.Count); i = (i + 1)) {
                this.Add(value[i]);
            }
        } 

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.Contains"]/*' /> 
        /// <summary> 
        /// <para>Gets a value indicating whether the
        /// <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> contains the specified <see cref='System.Windows.Forms.Design.Behavior.Adorner'/>.</para> 
        /// </summary>
        /// <param name='value'>The <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> to locate.</param>
        /// <returns>
        /// <para><see langword='true'/> if the <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> is contained in the collection; 
        /// otherwise, <see langword='false'/>.</para>
        /// </returns> 
        /// <seealso cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection.IndexOf'/> 
        public bool Contains(Adorner value) {
            return List.Contains(value); 
        }

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.CopyTo"]/*' />
        /// <summary> 
        /// <para>Copies the <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
        /// specified index.</para> 
        /// </summary> 
        /// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> .</para></param>
        /// <param name='index'>The index in <paramref name='array'/> where copying begins.</param> 
        /// <returns>
        /// <para>None.</para>
        /// </returns>
        /// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception> 
        /// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
        /// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception> 
        /// <seealso cref='System.Array'/> 
        public void CopyTo(Adorner[] array, int index) {
            List.CopyTo(array, index); 
        }

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.IndexOf"]/*' />
        /// <summary> 
        /// <para>Returns the index of a <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> in
        /// the <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> .</para> 
        /// </summary> 
        /// <param name='value'>The <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> to locate.</param>
        /// <returns> 
        /// <para>The index of the <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> of <paramref name='value'/> in the
        /// <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/>, if found; otherwise, -1.</para>
        /// </returns>
        /// <seealso cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection.Contains'/> 
        public int IndexOf(Adorner value) {
            return List.IndexOf(value); 
        } 

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.Insert"]/*' /> 
        /// <summary>
        /// <para>Inserts a <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> into the <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> at the specified index.</para>
        /// </summary>
        /// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param> 
        /// <param name=' value'>The <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> to insert.</param>
        /// <returns><para>None.</para></returns> 
        /// <seealso cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection.Add'/> 
        public void Insert(int index, Adorner value) {
            List.Insert(index, value); 
        }

        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.GetEnumerator"]/*' />
        /// <summary> 
        /// <para>Returns an enumerator that can iterate through
        /// the <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> .</para> 
        /// </summary> 
        /// <returns><para>None.</para></returns>
        /// <seealso cref='System.Collections.IEnumerator'/> 
        public new BehaviorServiceAdornerCollectionEnumerator GetEnumerator() {
            return new BehaviorServiceAdornerCollectionEnumerator(this);
        }
 
        /// <include file='doc\BehaviorServiceAdornerCollection.uex' path='docs/doc[@for="BehaviorServiceAdornerCollection.Remove"]/*' />
        /// <summary> 
        /// <para> Removes a specific <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> from the 
        /// <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> .</para>
        /// </summary> 
        /// <param name='value'>The <see cref='System.Windows.Forms.Design.Behavior.Adorner'/> to remove from the <see cref='System.Windows.Forms.Design.Behavior.BehaviorServiceAdornerCollection'/> .</param>
        /// <returns><para>None.</para></returns>
        /// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
        public void Remove(Adorner value) { 
            List.Remove(value);
        } 
 
    }
 
    /// <include file='doc\BehaviorServiceAdornerCollectionEnumerator.uex' path='docs/doc[@for="BehaviorServiceAdornerCollectionEnumerator"]/*' />
    public class BehaviorServiceAdornerCollectionEnumerator : object, IEnumerator {

        private IEnumerator baseEnumerator; 

        private IEnumerable temp; 
 
        /// <include file='doc\BehaviorServiceAdornerCollectionEnumerator.uex' path='docs/doc[@for="BehaviorServiceAdornerCollectionEnumerator.BehaviorServiceAdornerCollectionEnumerator"]/*' />
        public BehaviorServiceAdornerCollectionEnumerator(BehaviorServiceAdornerCollection mappings) { 
            this.temp = ((IEnumerable)(mappings));
            this.baseEnumerator = temp.GetEnumerator();
        }
 
        /// <include file='doc\BehaviorServiceAdornerCollectionEnumerator.uex' path='docs/doc[@for="BehaviorServiceAdornerCollectionEnumerator.Current"]/*' />
        public Adorner Current { 
            get { 
                return ((Adorner)(baseEnumerator.Current));
            } 
        }

        /// <include file='doc\BehaviorServiceAdornerCollectionEnumerator.uex' path='docs/doc[@for="BehaviorServiceAdornerCollectionEnumerator.IEnumerator.Current"]/*' />
        /// <internalonly/> 
        object IEnumerator.Current {
            get { 
                return baseEnumerator.Current; 
            }
        } 

        /// <include file='doc\BehaviorServiceAdornerCollectionEnumerator.uex' path='docs/doc[@for="BehaviorServiceAdornerCollectionEnumerator.MoveNext"]/*' />
        public bool MoveNext() {
            return baseEnumerator.MoveNext(); 
        }
 
        /// <include file='doc\BehaviorService.uex' path='docs/doc[@for="BehaviorServiceAdornerCollectionEnumerator.IEnumerator.MoveNext"]/*' /> 
        /// <internalonly/>
        bool IEnumerator.MoveNext() { 
            return baseEnumerator.MoveNext();
        }

        /// <include file='doc\BehaviorServiceAdornerCollectionEnumerator.uex' path='docs/doc[@for="BehaviorServiceAdornerCollectionEnumerator.Reset"]/*' /> 
        public void Reset() {
            baseEnumerator.Reset(); 
        } 

        /// <include file='doc\BehaviorServiceAdornerCollectionEnumerator.uex' path='docs/doc[@for="BehaviorServiceAdornerCollectionEnumerator.IEnumerator.Reset"]/*' /> 
        /// <internalonly/>
        void IEnumerator.Reset() {
            baseEnumerator.Reset();
        } 
    }
 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
