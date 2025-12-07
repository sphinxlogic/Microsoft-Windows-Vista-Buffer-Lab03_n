//------------------------------------------------------------------------------ 
// <copyright company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.Collections.Generic;
    using System.Text;
    using System.Data;
    using System.Diagnostics; 
    using System.ComponentModel;
 
    internal class TableAdapterManagerHelper { 

        /// <summary> 
        /// Find out the self referenced relation
        /// Rule: if there is multiple self-ref relations,
        ///       and if there is relations with FK, they and only they will be returned
        ///       otherewise, return all self-ref relations 
        ///
        ///       Example: table has RELFK1,RELFK2,REL3 --> return RELFK1,RELFK2 
        ///       Example: table has REL3, REL4 --> return REL3, REL4 
        ///       Example: table has FK1,FK2 --> return null
        /// </summary> 
        /// <param name="dataTable">the dataTable</param>
        /// <returns>the selfRef relations for this table.</returns>
        internal static DataRelation[] GetSelfRefRelations(DataTable dataTable) {
            Debug.Assert(dataTable != null); 

            List<DataRelation> selfRefs = new List<DataRelation>(); 
            List<DataRelation> selfRefWithFKs = new List<DataRelation>(); 

            foreach (DataRelation relation in dataTable.ParentRelations) { 
                if (relation.ChildTable == relation.ParentTable){
                    selfRefs.Add(relation);
                    if (relation.ChildKeyConstraint != null) {
                        selfRefWithFKs.Add(relation); 
                    }
                } 
            } 
            if (selfRefWithFKs.Count > 0) {
                return selfRefWithFKs.ToArray(); 
            }
            return selfRefs.ToArray();
        }
 
        /// <summary>
        /// Find out the hierarchical update order based on the first FKs then the relations. 
        /// Example, customer(parent of order), order(parent of orderdetail), orderdetail 
        ///          --> out put will be Customer, Order, OrderDetail
        /// Self-referece is not considered in the order 
        /// Circle referece will stop the searching once detected
        /// </summary>
        /// <param name="ds">the dataset</param>
        /// <returns>DataTable array with parents first</returns> 
        internal static DataTable[] GetUpdateOrder(DataSet ds) {
            // Find out the tables that get involved, then build a tree 
            HierarchicalObject[] orders = new HierarchicalObject[ds.Tables.Count]; 
            for (int i = 0; i < ds.Tables.Count; i++) {
                DataTable t = ds.Tables[i]; 
                HierarchicalObject ho = new HierarchicalObject(t);
                orders[i] = ho;
            }
 
            // First, build up the parent tree
            for (int i = 0; i < orders.Length; i++) { 
                DataTable t = orders[i].TheObject as DataTable; 

                // build HU parent relation tree based on FK 
                foreach (Constraint c in t.Constraints) {
                    ForeignKeyConstraint fc = c as ForeignKeyConstraint;
                    // We do not care if the rule is turned on or not
                    // We do not consider self referenced FK 
                    if (fc != null && !Object.ReferenceEquals(fc.RelatedTable, t)) {
                        int index = ds.Tables.IndexOf(fc.RelatedTable); 
                        Debug.Assert(index >= 0); 
                        orders[i].AddUniqueParent(orders[index]);
                    } 
                }
                // build HU parent relation tree based on relation
                foreach (DataRelation relation in t.ParentRelations) {
                    if (!object.ReferenceEquals(relation.ParentTable, t)) { 
                        int index = ds.Tables.IndexOf(relation.ParentTable);
                        Debug.Assert(index >= 0); 
                        orders[i].AddUniqueParent(orders[index]); 
                    }
                } 
            }

            // Work out the priorities
            for (int i = 0; i < orders.Length; i++) { 
                HierarchicalObject ho = orders[i];
                if (ho.HasParent) { 
                    ho.CheckParents(); 
                }
            } 

            // Get the result with sorted order
            DataTable[] dataTables = new DataTable[orders.Length];
            System.Array.Sort<HierarchicalObject>(orders); 
            for (int i = 0; i < orders.Length; i++) {
                HierarchicalObject ho = orders[i]; 
                dataTables[i] = (DataTable)ho.TheObject; 
            }
            return dataTables; 
        }
        /// <summary>
        /// The object with a list of parents
        /// </summary> 
        internal class HierarchicalObject : IComparable<HierarchicalObject> {
            internal int Height = 0; // the hierarchical priority 
            internal Object TheObject; 
            private List<HierarchicalObject> parents;
 
            internal List<HierarchicalObject> Parents {
                get {
                    if (parents == null) {
                        parents = new List<HierarchicalObject>(); 
                    }
                    return parents; 
                } 
            }
 
            internal bool HasParent {
                get {
                    return parents != null && parents.Count > 0;
                } 
            }
 
            internal HierarchicalObject(Object theObject) { 
                this.TheObject = theObject;
            } 

            /// <summary>
            /// Add the parent if it is not exist in the parent list
            /// </summary> 
            /// <param name="parent"></param>
            internal void AddUniqueParent(HierarchicalObject parent) { 
                if (!Parents.Contains(parent)) { 
                    Parents.Add(parent);
                } 
            }

            /// <summary>
            /// Check to see if it has parent or not in a loop and update its parent's value 
            /// </summary>
            internal void CheckParents() { 
                if (HasParent) { 
                    Stack<HierarchicalObject> path = new Stack<HierarchicalObject>();
                    Stack<HierarchicalObject> work = new Stack<HierarchicalObject>(); 
                    work.Push(this);
                    path.Push(this);
                    this.CheckParents(work, path);
                } 
            }
            /// <summary> 
            /// Check to see if it has parent or not in a loop and update its parent's value 
            /// </summary>
            internal void CheckParents(Stack<HierarchicalObject> work, Stack<HierarchicalObject> path) { 
                if (!HasParent || (!object.ReferenceEquals(this, path.Peek()) && path.Contains(this))) {
                    // Stop if there is no parent or it is in a loop
                    // path.Peek() is always this, so we need to exclude it.
                    // 
                    Debug.Assert(work.Count > 0 && path.Count > 0 && object.ReferenceEquals(path.Peek(), this));
                    HierarchicalObject topPath = path.Pop(); 
                    HierarchicalObject topWork = work.Pop(); 
                    while (work.Count > 0 && path.Count > 0 && object.ReferenceEquals(topPath, topWork)) {
                        topPath = path.Pop(); 
                        topWork = work.Pop();
                    }
                    if (topWork != topPath) {
                        path.Push(topWork); 
                        topWork.CheckParents(work, path);
                    } 
                    return; 
                }
                else if (this.HasParent) { 
                    // has parent
                    HierarchicalObject first = null;

                    // find out all parents that is not in a loop and has lower priority then this 
                    // increase their priority and push them to the work stack, we need to walk up to the tree
                    // one by one to update the grandparent's priority 
                    for (int i = Parents.Count - 1; i >= 0; i--) { 
                        HierarchicalObject current = Parents[i];
                        if (!path.Contains(current) && current.Height <= this.Height) { 
                            current.Height = this.Height + 1;
                            Debug.Assert(current.Height < 1000);
                            if (current.Height > 1000) {
                                return; 
                            }
                            work.Push(current); 
                            first = current; 
                        }
                    } 
                    // Now we walk up the first parent
                    if (first != null) {
                        path.Push(first);
                        first.CheckParents(work, path); 
                    }
                } 
            } 
            int IComparable<HierarchicalObject>.CompareTo(HierarchicalObject other) {
                return other.Height - this.Height; 
            }
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
 
namespace System.Data.Design { 

    using System; 
    using System.Collections.Generic;
    using System.Text;
    using System.Data;
    using System.Diagnostics; 
    using System.ComponentModel;
 
    internal class TableAdapterManagerHelper { 

        /// <summary> 
        /// Find out the self referenced relation
        /// Rule: if there is multiple self-ref relations,
        ///       and if there is relations with FK, they and only they will be returned
        ///       otherewise, return all self-ref relations 
        ///
        ///       Example: table has RELFK1,RELFK2,REL3 --> return RELFK1,RELFK2 
        ///       Example: table has REL3, REL4 --> return REL3, REL4 
        ///       Example: table has FK1,FK2 --> return null
        /// </summary> 
        /// <param name="dataTable">the dataTable</param>
        /// <returns>the selfRef relations for this table.</returns>
        internal static DataRelation[] GetSelfRefRelations(DataTable dataTable) {
            Debug.Assert(dataTable != null); 

            List<DataRelation> selfRefs = new List<DataRelation>(); 
            List<DataRelation> selfRefWithFKs = new List<DataRelation>(); 

            foreach (DataRelation relation in dataTable.ParentRelations) { 
                if (relation.ChildTable == relation.ParentTable){
                    selfRefs.Add(relation);
                    if (relation.ChildKeyConstraint != null) {
                        selfRefWithFKs.Add(relation); 
                    }
                } 
            } 
            if (selfRefWithFKs.Count > 0) {
                return selfRefWithFKs.ToArray(); 
            }
            return selfRefs.ToArray();
        }
 
        /// <summary>
        /// Find out the hierarchical update order based on the first FKs then the relations. 
        /// Example, customer(parent of order), order(parent of orderdetail), orderdetail 
        ///          --> out put will be Customer, Order, OrderDetail
        /// Self-referece is not considered in the order 
        /// Circle referece will stop the searching once detected
        /// </summary>
        /// <param name="ds">the dataset</param>
        /// <returns>DataTable array with parents first</returns> 
        internal static DataTable[] GetUpdateOrder(DataSet ds) {
            // Find out the tables that get involved, then build a tree 
            HierarchicalObject[] orders = new HierarchicalObject[ds.Tables.Count]; 
            for (int i = 0; i < ds.Tables.Count; i++) {
                DataTable t = ds.Tables[i]; 
                HierarchicalObject ho = new HierarchicalObject(t);
                orders[i] = ho;
            }
 
            // First, build up the parent tree
            for (int i = 0; i < orders.Length; i++) { 
                DataTable t = orders[i].TheObject as DataTable; 

                // build HU parent relation tree based on FK 
                foreach (Constraint c in t.Constraints) {
                    ForeignKeyConstraint fc = c as ForeignKeyConstraint;
                    // We do not care if the rule is turned on or not
                    // We do not consider self referenced FK 
                    if (fc != null && !Object.ReferenceEquals(fc.RelatedTable, t)) {
                        int index = ds.Tables.IndexOf(fc.RelatedTable); 
                        Debug.Assert(index >= 0); 
                        orders[i].AddUniqueParent(orders[index]);
                    } 
                }
                // build HU parent relation tree based on relation
                foreach (DataRelation relation in t.ParentRelations) {
                    if (!object.ReferenceEquals(relation.ParentTable, t)) { 
                        int index = ds.Tables.IndexOf(relation.ParentTable);
                        Debug.Assert(index >= 0); 
                        orders[i].AddUniqueParent(orders[index]); 
                    }
                } 
            }

            // Work out the priorities
            for (int i = 0; i < orders.Length; i++) { 
                HierarchicalObject ho = orders[i];
                if (ho.HasParent) { 
                    ho.CheckParents(); 
                }
            } 

            // Get the result with sorted order
            DataTable[] dataTables = new DataTable[orders.Length];
            System.Array.Sort<HierarchicalObject>(orders); 
            for (int i = 0; i < orders.Length; i++) {
                HierarchicalObject ho = orders[i]; 
                dataTables[i] = (DataTable)ho.TheObject; 
            }
            return dataTables; 
        }
        /// <summary>
        /// The object with a list of parents
        /// </summary> 
        internal class HierarchicalObject : IComparable<HierarchicalObject> {
            internal int Height = 0; // the hierarchical priority 
            internal Object TheObject; 
            private List<HierarchicalObject> parents;
 
            internal List<HierarchicalObject> Parents {
                get {
                    if (parents == null) {
                        parents = new List<HierarchicalObject>(); 
                    }
                    return parents; 
                } 
            }
 
            internal bool HasParent {
                get {
                    return parents != null && parents.Count > 0;
                } 
            }
 
            internal HierarchicalObject(Object theObject) { 
                this.TheObject = theObject;
            } 

            /// <summary>
            /// Add the parent if it is not exist in the parent list
            /// </summary> 
            /// <param name="parent"></param>
            internal void AddUniqueParent(HierarchicalObject parent) { 
                if (!Parents.Contains(parent)) { 
                    Parents.Add(parent);
                } 
            }

            /// <summary>
            /// Check to see if it has parent or not in a loop and update its parent's value 
            /// </summary>
            internal void CheckParents() { 
                if (HasParent) { 
                    Stack<HierarchicalObject> path = new Stack<HierarchicalObject>();
                    Stack<HierarchicalObject> work = new Stack<HierarchicalObject>(); 
                    work.Push(this);
                    path.Push(this);
                    this.CheckParents(work, path);
                } 
            }
            /// <summary> 
            /// Check to see if it has parent or not in a loop and update its parent's value 
            /// </summary>
            internal void CheckParents(Stack<HierarchicalObject> work, Stack<HierarchicalObject> path) { 
                if (!HasParent || (!object.ReferenceEquals(this, path.Peek()) && path.Contains(this))) {
                    // Stop if there is no parent or it is in a loop
                    // path.Peek() is always this, so we need to exclude it.
                    // 
                    Debug.Assert(work.Count > 0 && path.Count > 0 && object.ReferenceEquals(path.Peek(), this));
                    HierarchicalObject topPath = path.Pop(); 
                    HierarchicalObject topWork = work.Pop(); 
                    while (work.Count > 0 && path.Count > 0 && object.ReferenceEquals(topPath, topWork)) {
                        topPath = path.Pop(); 
                        topWork = work.Pop();
                    }
                    if (topWork != topPath) {
                        path.Push(topWork); 
                        topWork.CheckParents(work, path);
                    } 
                    return; 
                }
                else if (this.HasParent) { 
                    // has parent
                    HierarchicalObject first = null;

                    // find out all parents that is not in a loop and has lower priority then this 
                    // increase their priority and push them to the work stack, we need to walk up to the tree
                    // one by one to update the grandparent's priority 
                    for (int i = Parents.Count - 1; i >= 0; i--) { 
                        HierarchicalObject current = Parents[i];
                        if (!path.Contains(current) && current.Height <= this.Height) { 
                            current.Height = this.Height + 1;
                            Debug.Assert(current.Height < 1000);
                            if (current.Height > 1000) {
                                return; 
                            }
                            work.Push(current); 
                            first = current; 
                        }
                    } 
                    // Now we walk up the first parent
                    if (first != null) {
                        path.Push(first);
                        first.CheckParents(work, path); 
                    }
                } 
            } 
            int IComparable<HierarchicalObject>.CompareTo(HierarchicalObject other) {
                return other.Height - this.Height; 
            }
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
