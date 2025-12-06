namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Windows.Forms.Design;
 
    /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection"]/*' />
    /// <devdoc>
    /// A strongly-typed collection that stores Behavior.Glyph objects.
    /// </devdoc> 
    public class GlyphCollection : CollectionBase {
 
        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.GlyphCollection"]/*' /> 
        /// <devdoc>
        /// Initializes a new instance of Behavior.GlyphCollection. 
        /// </devdoc>
        public GlyphCollection() {
        }
 
        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.GlyphCollection1"]/*' />
        /// <devdoc> 
        /// Initializes a new instance of Behavior.GlyphCollection based on another Behavior.GlyphCollection. 
        /// </devdoc>
        public GlyphCollection(GlyphCollection value) { 
            this.AddRange(value);
        }

        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.GlyphCollection2"]/*' /> 
        /// <devdoc>
        /// Initializes a new instance of Behavior.GlyphCollection containing any array of Behavior.Glyph objects. 
        /// </devdoc> 
        public GlyphCollection(Glyph[] value) {
            this.AddRange(value); 
        }

        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.this"]/*' />
        /// <devdoc> 
        /// Represents the entry at the specified index of the Behavior.Glyph.
        /// </devdoc> 
        public Glyph this[int index] { 
            get {
                return ((Glyph)(List[index])); 
            }
            set {
                List[index] = value;
            } 
        }
 
        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.Add"]/*' /> 
        /// <devdoc>
        /// Adds a Behavior.Glyph with the specified value to the 
        /// Behavior.GlyphCollection .
        /// </devdoc>
        public int Add(Glyph value) {
            return List.Add(value); 
        }
 
        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.AddRange"]/*' /> 
        /// <devdoc>
        /// Copies the elements of an array to the end of the Behavior.GlyphCollection. 
        /// </devdoc>
        public void AddRange(Glyph[] value) {
            for (int i = 0; (i < value.Length); i = (i + 1)) {
                this.Add(value[i]); 
            }
        } 
 
        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.AddRange1"]/*' />
        /// <devdoc> 
        /// Adds the contents of another Behavior.GlyphCollection to the end of the collection.
        /// </devdoc>
        public void AddRange(GlyphCollection value) {
            for (int i = 0; (i < value.Count); i = (i + 1)) { 
                this.Add(value[i]);
            } 
        } 

        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.Contains"]/*' /> 
        /// <devdoc>
        /// Gets a value indicating whether the
        /// Behavior.GlyphCollection contains the specified Behavior.Glyph.
        /// </devdoc> 
        public bool Contains(Glyph value) {
            return List.Contains(value); 
        } 

        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.CopyTo"]/*' /> 
        /// <devdoc>
        /// Copies the Behavior.GlyphCollection values to a one-dimensional <see cref='System.Array instance at the
        /// specified index.
        /// </devdoc> 
        public void CopyTo(Glyph[] array, int index) {
            List.CopyTo(array, index); 
        } 

        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.IndexOf"]/*' /> 
        /// <devdoc>
        /// Returns the index of a Behavior.Glyph in
        /// the Behavior.GlyphCollection .
        /// </devdoc> 
        public int IndexOf(Glyph value) {
            return List.IndexOf(value); 
        } 

        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.Insert"]/*' /> 
        /// <devdoc>
        /// Inserts a Behavior.Glyph into the Behavior.GlyphCollection at the specified index.
        /// </devdoc>
        public void Insert(int index, Glyph value) { 
            List.Insert(index, value);
        } 
 
        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.Remove"]/*' />
        /// <devdoc> 
        /// Removes a specific Behavior.Glyph from the
        /// Behavior.GlyphCollection .
        /// </devdoc>
        public void Remove(Glyph value) { 
            List.Remove(value);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Windows.Forms.Design;
 
    /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection"]/*' />
    /// <devdoc>
    /// A strongly-typed collection that stores Behavior.Glyph objects.
    /// </devdoc> 
    public class GlyphCollection : CollectionBase {
 
        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.GlyphCollection"]/*' /> 
        /// <devdoc>
        /// Initializes a new instance of Behavior.GlyphCollection. 
        /// </devdoc>
        public GlyphCollection() {
        }
 
        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.GlyphCollection1"]/*' />
        /// <devdoc> 
        /// Initializes a new instance of Behavior.GlyphCollection based on another Behavior.GlyphCollection. 
        /// </devdoc>
        public GlyphCollection(GlyphCollection value) { 
            this.AddRange(value);
        }

        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.GlyphCollection2"]/*' /> 
        /// <devdoc>
        /// Initializes a new instance of Behavior.GlyphCollection containing any array of Behavior.Glyph objects. 
        /// </devdoc> 
        public GlyphCollection(Glyph[] value) {
            this.AddRange(value); 
        }

        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.this"]/*' />
        /// <devdoc> 
        /// Represents the entry at the specified index of the Behavior.Glyph.
        /// </devdoc> 
        public Glyph this[int index] { 
            get {
                return ((Glyph)(List[index])); 
            }
            set {
                List[index] = value;
            } 
        }
 
        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.Add"]/*' /> 
        /// <devdoc>
        /// Adds a Behavior.Glyph with the specified value to the 
        /// Behavior.GlyphCollection .
        /// </devdoc>
        public int Add(Glyph value) {
            return List.Add(value); 
        }
 
        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.AddRange"]/*' /> 
        /// <devdoc>
        /// Copies the elements of an array to the end of the Behavior.GlyphCollection. 
        /// </devdoc>
        public void AddRange(Glyph[] value) {
            for (int i = 0; (i < value.Length); i = (i + 1)) {
                this.Add(value[i]); 
            }
        } 
 
        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.AddRange1"]/*' />
        /// <devdoc> 
        /// Adds the contents of another Behavior.GlyphCollection to the end of the collection.
        /// </devdoc>
        public void AddRange(GlyphCollection value) {
            for (int i = 0; (i < value.Count); i = (i + 1)) { 
                this.Add(value[i]);
            } 
        } 

        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.Contains"]/*' /> 
        /// <devdoc>
        /// Gets a value indicating whether the
        /// Behavior.GlyphCollection contains the specified Behavior.Glyph.
        /// </devdoc> 
        public bool Contains(Glyph value) {
            return List.Contains(value); 
        } 

        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.CopyTo"]/*' /> 
        /// <devdoc>
        /// Copies the Behavior.GlyphCollection values to a one-dimensional <see cref='System.Array instance at the
        /// specified index.
        /// </devdoc> 
        public void CopyTo(Glyph[] array, int index) {
            List.CopyTo(array, index); 
        } 

        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.IndexOf"]/*' /> 
        /// <devdoc>
        /// Returns the index of a Behavior.Glyph in
        /// the Behavior.GlyphCollection .
        /// </devdoc> 
        public int IndexOf(Glyph value) {
            return List.IndexOf(value); 
        } 

        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.Insert"]/*' /> 
        /// <devdoc>
        /// Inserts a Behavior.Glyph into the Behavior.GlyphCollection at the specified index.
        /// </devdoc>
        public void Insert(int index, Glyph value) { 
            List.Insert(index, value);
        } 
 
        /// <include file='doc\GlyphCollection.uex' path='docs/doc[@for="GlyphCollection.Remove"]/*' />
        /// <devdoc> 
        /// Removes a specific Behavior.Glyph from the
        /// Behavior.GlyphCollection .
        /// </devdoc>
        public void Remove(Glyph value) { 
            List.Remove(value);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
