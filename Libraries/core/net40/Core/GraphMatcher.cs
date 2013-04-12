/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2012 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is furnished
to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VDS.RDF
{
    /// <summary>
    /// Implements a Graph Isomorphism Algorithm
    /// </summary>
    class GraphMatcher
    {
        //The Unbound and Bound lists refers to the Nodes of the Target Graph
        private List<INode> _unbound;
        private List<INode> _bound;
        private Dictionary<INode, INode> _mapping;
        private HashSet<Triple> _sourceTriples;
        private HashSet<Triple> _targetTriples;

        /// <summary>
        /// Compares two Graphs for equality
        /// </summary>
        /// <param name="g">Graph</param>
        /// <param name="h">Graph</param>
        /// <returns></returns>
        public bool Equals(IGraph g, IGraph h)
        {
            Debug.WriteLine("Making simple equality checks");

            //If both are null then consider equal
            if (g == null && h == null)
            {
                Debug.WriteLine("[EQUAL] Both Graphs null");
            }
            //Graphs can't be equal to null
            if (g == null)
            {
                Debug.WriteLine("[NOT EQUAL] First Graph is null");
                return false;
            }
            if (h == null)
            {
                Debug.WriteLine("[NOT EQUAL] Second Graph is null");
                return false;
            }

            //If we're the same Graph (by reference) then we're trivially equal
            if (ReferenceEquals(g, h))
            {
                Debug.WriteLine("[EQUAL] Graphs equal be reference");
                return true;
            }

            //If different number of Triples then the Graphs can't be equal
            if (g.Triples.Count != h.Triples.Count)
            {
                Debug.WriteLine("[NOT EQUAL] Differing number of triples between graphs");
                return false;
            }

            int gtCount = 0;
            Dictionary<INode, int> gNodes = new Dictionary<INode, int>();
            Dictionary<INode, int> hNodes = new Dictionary<INode, int>();
            this._targetTriples = new HashSet<Triple>(h.Triples);
            foreach (Triple t in g.Triples)
            {
                if (t.IsGroundTriple)
                {
                    //If the other Graph doesn't contain the Ground Triple so can't be equal
                    //We make the contains call first as that is typically O(1) while the Remove call may be O(n)
                    if (!h.Triples.Contains(t))
                    {
                        Debug.WriteLine("[NOT EQUAL] First graph contains a ground triple which is not in the second graph");
                        return false;
                    }
                    if (!this._targetTriples.Remove(t))
                    {
                        Debug.WriteLine("[NOT EQUAL] First graph contains a ground triple which is not in the second graph");
                        return false;
                    }
                    gtCount++;
                }
                else
                {
                    //If not a Ground Triple remember which Blank Nodes we need to map
                    if (t.Subject.NodeType == NodeType.Blank)
                    {
                        if (!gNodes.ContainsKey(t.Subject))
                        {
                            gNodes.Add(t.Subject, 1);
                        }
                        else
                        {
                            gNodes[t.Subject]++;
                        }
                    }
                    if (t.Predicate.NodeType == NodeType.Blank)
                    {
                        if (!gNodes.ContainsKey(t.Predicate))
                        {
                            gNodes.Add(t.Predicate, 1);
                        }
                        else
                        {
                            gNodes[t.Predicate]++;
                        }
                    }
                    if (t.Object.NodeType == NodeType.Blank)
                    {
                        if (!gNodes.ContainsKey(t.Object))
                        {
                            gNodes.Add(t.Object, 1);
                        }
                        else
                        {
                            gNodes[t.Object]++;
                        }
                    }
                }
            }

            //If the other Graph still contains Ground Triples then the Graphs aren't equal
            if (this._targetTriples.Any(t => t.IsGroundTriple))
            {
                Debug.WriteLine("[NOT EQUAL] Second Graph contains ground triples not present in first graph");
                return false;
            }
            Debug.WriteLine("Validated that there are " + gtCount + " ground triples present in both graphs");

            //If there are no Triples left in the other Graph, all our Triples were Ground Triples and there are no Blank Nodes to map the Graphs are equal
            if (this._targetTriples.Count == 0 && gtCount == g.Triples.Count && gNodes.Count == 0)
            {
                Debug.WriteLine("[EQUAL] Graphs contain only ground triples and all triples are present in both graphs");
                return true;
            }

            //Now classify the remaining Triples from the other Graph
            foreach (Triple t in this._targetTriples)
            {
                if (t.Subject.NodeType == NodeType.Blank)
                {
                    if (!hNodes.ContainsKey(t.Subject))
                    {
                        hNodes.Add(t.Subject, 1);
                    }
                    else
                    {
                        hNodes[t.Subject]++;
                    }
                }
                if (t.Predicate.NodeType == NodeType.Blank)
                {
                    if (!hNodes.ContainsKey(t.Predicate))
                    {
                        hNodes.Add(t.Predicate, 1);
                    }
                    else
                    {
                        hNodes[t.Predicate]++;
                    }
                }
                if (t.Object.NodeType == NodeType.Blank)
                {
                    if (!hNodes.ContainsKey(t.Object))
                    {
                        hNodes.Add(t.Object, 1);
                    }
                    else
                    {
                        hNodes[t.Object]++;
                    }
                }
            }

            //First off we must have the same number of Blank Nodes in each Graph
            if (gNodes.Count != hNodes.Count)
            {
                Debug.WriteLine("[NOT EQUAL] Differing number of unique blank nodes between graphs");
                return false;
            }
            Debug.WriteLine("Both graphs contain " + gNodes.Count + " unique blank nodes");

            //Then we sub-classify by the number of Blank Nodes with each degree classification
            Dictionary<int, int> gDegrees = new Dictionary<int, int>();
            Dictionary<int, int> hDegrees = new Dictionary<int, int>();
            foreach (int degree in gNodes.Values)
            {
                if (gDegrees.ContainsKey(degree))
                {
                    gDegrees[degree]++;
                }
                else
                {
                    gDegrees.Add(degree, 1);
                }
            }
            foreach (int degree in hNodes.Values)
            {
                if (hDegrees.ContainsKey(degree))
                {
                    hDegrees[degree]++;
                }
                else
                {
                    hDegrees.Add(degree, 1);
                }
            }

            //Then we must have the same number of degree classifications
            if (gDegrees.Count != hDegrees.Count)
            {
                Debug.WriteLine("[NOT EQUAL] Degree classification of Blank Nodes indicates no possible equality mapping");
                return false;
            }
            Debug.WriteLine("Degree classification of blank nodes indicates there is a possible equality mapping");

            //Then for each degree classification there must be the same number of BNodes with that degree in both Graph
            if (gDegrees.All(pair => hDegrees.ContainsKey(pair.Key) && gDegrees[pair.Key] == hDegrees[pair.Key]))
            {
                Debug.WriteLine("Validated that degree classification does provide a possible equality mapping");

                //Try to do a Rules Based Mapping
                return this.TryRulesBasedMapping(g, h, gNodes, hNodes, gDegrees, hDegrees);
            }
            else
            {
                //There are degree classifications that don't have the same number of BNodes with that degree 
                //or the degree classification is not in the other Graph
                Debug.WriteLine("[NOT EQUAL] Degree classification of Blank Nodes indicates no possible equality mapping");
                return false;
            }

        }

        /// <summary>
        /// Uses a series of Rules to attempt to generate a mapping without the need for brute force guessing
        /// </summary>
        /// <param name="g">1st Graph</param>
        /// <param name="h">2nd Graph</param>
        /// <param name="gNodes">1st Graph Node classification</param>
        /// <param name="hNodes">2nd Graph Node classification</param>
        /// <param name="gDegrees">1st Graph Degree classification</param>
        /// <param name="hDegrees">2nd Graph Degree classification</param>
        /// <returns></returns>
        private bool TryRulesBasedMapping(IGraph g, IGraph h, Dictionary<INode, int> gNodes, Dictionary<INode, int> hNodes, Dictionary<int, int> gDegrees, Dictionary<int, int> hDegrees)
        {
            Debug.WriteLine("Attempting rules based equality mapping");

            //Start with new lists and dictionaries each time in case we get reused
            this._unbound = new List<INode>();
            this._bound = new List<INode>();
            this._mapping = new Dictionary<INode, INode>();

            //Initialise the Source Triples list
            this._sourceTriples = new HashSet<Triple>(from t in g.Triples
                                                      where !t.IsGroundTriple
                                                      select t);

            //First thing consider the trivial mapping
            Dictionary<INode, INode> trivialMapping = new Dictionary<INode, INode>();
            foreach (INode n in gNodes.Keys)
            {
                trivialMapping.Add(n, n);
            }
            HashSet<Triple> targets = new HashSet<Triple>(this._targetTriples);
            if (this._sourceTriples.All(t => targets.Remove(t.MapTriple(h, trivialMapping))))
            {
                this._mapping = trivialMapping;
                Debug.WriteLine("[EQUAL] Trivial Mapping (all Blank Nodes have identical IDs) is a valid equality mapping");
                return true;
            }
            Debug.WriteLine("Trivial Mapping (all Blank Nodes have identical IDs) did not hold");

            //Initialise the Unbound list
            this._unbound = (from n in hNodes.Keys
                             select n).ToList();

            //Map single use Nodes first to reduce the size of the overall mapping
            Debug.WriteLine("Mapping single use blank nodes");
            foreach (KeyValuePair<INode, int> pair in gNodes.Where(p => p.Value == 1))
            {
                //Find the Triple we need to map
                Triple toMap = (from t in this._sourceTriples
                                where t.Involves(pair.Key)
                                select t).First();

                foreach (INode n in this._unbound.Where(n => hNodes[n] == pair.Value))
                {
                    //See if this Mapping works
                    this._mapping.Add(pair.Key, n);
                    if (this._targetTriples.Remove(toMap.MapTriple(h, this._mapping)))
                    {
                        this._sourceTriples.Remove(toMap);
                        this._bound.Add(n);
                        break;
                    }
                    this._mapping.Remove(pair.Key);
                }

                //There is a pathological case where we can't map anything this way because
                //there are always dependencies between the blank nodes in which case we
                //still continue because we may figure out a mapping with the dependency based
                //rules or brute force approach :-(

                //Otherwise we can mark that Node as Bound
                if (this._mapping.ContainsKey(pair.Key)) this._unbound.Remove(this._mapping[pair.Key]);
            }
            Debug.WriteLine("Single use nodes allowed mapping " + this._mapping.Count + " nodes, " + this._mapping.Count + " mapped out of a total " + gNodes.Count);

            //If all the Nodes were used only once and we mapped them all then the Graphs are equal
            if (this._targetTriples.Count == 0)
            {
                Debug.WriteLine("[EQUAL] All Blank Nodes were single use and were successfully mapped");
                return true;
            }

            //Map any Nodes of unique degree next
            Debug.WriteLine("Trying to map unique blank nodes");
            int mappedSoFar = this._mapping.Count;
            foreach (KeyValuePair<int, int> degreeClass in gDegrees)
            {
                if (degreeClass.Key > 1 && degreeClass.Value == 1)
                {
                    //There is a Node of degree greater than 1 than has a unique degree
                    //i.e. there is only one Node with this degree so there can only ever be one
                    //possible mapping for this Node
                    INode x = gNodes.FirstOrDefault(p => p.Value == degreeClass.Key).Key;
                    INode y = hNodes.FirstOrDefault(p => p.Value == degreeClass.Key).Key;

                    //If either of these return null then the Graphs can't be equal
                    if (x == null || y == null)
                    {
                        Debug.WriteLine("[NOT EQUAL] Node with unique degree could not be mapped");
                        this._mapping = null;
                        return false;
                    }

                    //Add the Mapping
                    this._mapping.Add(x, y);
                    this._bound.Add(y);
                    this._unbound.Remove(y);
                }
            }
            Debug.WriteLine("Unique blank nodes allowing mapping of " + (this._mapping.Count - mappedSoFar) + " nodes, " + this._mapping.Count + " mapped out of a total " + gNodes.Count);
            mappedSoFar = this._mapping.Count;

            //Then look for nodes which are associated with unique constants
            //By this we mean nodes that occur as the subject/object of a triple where the other two parts are non-blank
            Debug.WriteLine("Trying to map blank nodes based on unique constants associated with the nodes");
            foreach (Triple t in this._sourceTriples)
            {
                if (t.Subject.NodeType == NodeType.Blank && t.Predicate.NodeType != NodeType.Blank && t.Object.NodeType != NodeType.Blank)
                {
                    //Ignore if already mapped
                    if (this._mapping.ContainsKey(t.Subject)) continue;

                    //Are there any possible matches?
                    //We only need to know about at most 2 possibilities since zero possiblities means non-equal graphs, one is a valid mapping and two or more is not mappable this way
                    List<Triple> possibles = new List<Triple>(this._targetTriples.Where(x => x.Subject.NodeType == NodeType.Blank && x.Predicate.Equals(t.Predicate) && x.Object.Equals(t.Object)).Take(2));
                    if (possibles.Count == 1)
                    {
                        //Precisely one possible match so map
                        INode x = t.Subject;
                        INode y = possibles.First().Subject;
                        this._mapping.Add(x, y);
                        this._bound.Add(y);
                        this._unbound.Remove(y);
                    }
                    else if (possibles.Count == 0)
                    {
                        //No possible matches so not equal graphs
                        Debug.WriteLine("[NOT EQUAL] Node used in a triple with two constants where no candidate mapping for that triple could be found");
                        this._mapping = null;
                        return false;
                    }
                }
                else if (t.Subject.NodeType != NodeType.Blank && t.Predicate.NodeType != NodeType.Blank && t.Object.NodeType == NodeType.Blank)
                {
                    //Ignore if already mapped
                    if (this._mapping.ContainsKey(t.Object)) continue;

                    //Are there any possible matches?
                    //We only need to know about at most 2 possibilities since zero possiblities means non-equal graphs, one is a valid mapping and two or more is not mappable this way
                    List<Triple> possibles = new List<Triple>(this._targetTriples.Where(x => x.Subject.Equals(t.Subject) && x.Predicate.Equals(t.Predicate) && x.Object.NodeType == NodeType.Blank).Take(2));
                    if (possibles.Count == 1)
                    {
                        //Precisely one possible match so map
                        INode x = t.Subject;
                        INode y = possibles.First().Subject;
                        this._mapping.Add(x, y);
                        this._bound.Add(y);
                        this._unbound.Remove(y);
                    }
                }
            }
            Debug.WriteLine("Using unique constants associated with blank nodes allowed mapping of " + (this._mapping.Count - mappedSoFar) + " blank nodes, " + this._mapping.Count + " mapped out of a total " + gNodes.Count);
            mappedSoFar = this._mapping.Count;

            //Work out which Nodes are paired up 
            //By this we mean any Nodes which appear with other Nodes in a Triple
            //If multiple nodes appear together we can use this information to restrict
            //the possible mappings we generate
            List<MappingPair> sourceDependencies = new List<MappingPair>();
            foreach (Triple t in this._sourceTriples)
            {
                if (t.Subject.NodeType == NodeType.Blank && t.Predicate.NodeType == NodeType.Blank && t.Object.NodeType == NodeType.Blank)
                {
                    throw new RdfException("GraphMatcher cannot compute mappings where a Triple is entirely composed of Blank Nodes");
                }
                if (t.Subject.NodeType == NodeType.Blank && t.Predicate.NodeType == NodeType.Blank)
                {
                    if (!t.Subject.Equals(t.Predicate)) sourceDependencies.Add(new MappingPair(t.Subject, t.Predicate, TripleIndexType.SubjectPredicate));
                }
                else if (t.Subject.NodeType == NodeType.Blank && t.Object.NodeType == NodeType.Blank)
                {
                    if (!t.Subject.Equals(t.Object)) sourceDependencies.Add(new MappingPair(t.Subject, t.Object, TripleIndexType.SubjectObject));
                }
                else if (t.Predicate.NodeType == NodeType.Blank && t.Object.NodeType == NodeType.Blank)
                {
                    if (!t.Predicate.Equals(t.Object)) sourceDependencies.Add(new MappingPair(t.Predicate, t.Object, TripleIndexType.PredicateObject));
                }
            }
            sourceDependencies = sourceDependencies.Distinct().ToList();
            List<MappingPair> targetDependencies = new List<MappingPair>();
            foreach (Triple t in this._targetTriples)
            {
                if (t.Subject.NodeType == NodeType.Blank && t.Predicate.NodeType == NodeType.Blank && t.Object.NodeType == NodeType.Blank)
                {
                    throw new RdfException("GraphMatcher cannot compute mappings where a Triple is entirely composed of Blank Nodes");
                }
                if (t.Subject.NodeType == NodeType.Blank && t.Predicate.NodeType == NodeType.Blank)
                {
                    if (!t.Subject.Equals(t.Predicate)) targetDependencies.Add(new MappingPair(t.Subject, t.Predicate, TripleIndexType.SubjectPredicate));
                }
                else if (t.Subject.NodeType == NodeType.Blank && t.Object.NodeType == NodeType.Blank)
                {
                    if (!t.Subject.Equals(t.Object)) targetDependencies.Add(new MappingPair(t.Subject, t.Object, TripleIndexType.SubjectObject));
                }
                else if (t.Predicate.NodeType == NodeType.Blank && t.Object.NodeType == NodeType.Blank)
                {
                    if (!t.Predicate.Equals(t.Object)) targetDependencies.Add(new MappingPair(t.Predicate, t.Object, TripleIndexType.PredicateObject));
                }
            }
            targetDependencies = targetDependencies.Distinct().ToList();

            //Once we know of dependencies we can then map independent nodes
            List<INode> sourceIndependents = (from n in gNodes.Keys
                                              where !sourceDependencies.Any(p => p.Contains(n))
                                              select n).ToList();
            List<INode> targetIndependents = (from n in hNodes.Keys
                                              where !targetDependencies.Any(p => p.Contains(n))
                                              select n).ToList();

            //If the number of independent nodes in the two Graphs is different we return false
            if (sourceIndependents.Count != targetIndependents.Count)
            {
                Debug.WriteLine("[NOT EQUAL] Graphs contain different number of independent blank nodes");
                return false;
            }
            Debug.WriteLine("Graphs contain " + sourceIndependents.Count + " indepdent blank nodes, attempting mapping");

            //Try to map the independent nodes
            foreach (INode x in sourceIndependents)
            {
                //They may already be mapped as they may be single use Triples
                if (this._mapping.ContainsKey(x)) continue;

                List<Triple> xs = this._sourceTriples.Where(t => t.Involves(x)).ToList();
                foreach (INode y in targetIndependents)
                {
                    if (gNodes[x] != hNodes[y]) continue;

                    this._mapping.Add(x, y);

                    //Test the mapping
                    HashSet<Triple> ys = new HashSet<Triple>(this._targetTriples.Where(t => t.Involves(y)));
                    if (xs.All(t => ys.Remove(t.MapTriple(h, this._mapping))))
                    {
                        //This is a valid mapping
                        xs.ForEach(t => this._targetTriples.Remove(t.MapTriple(h, this._mapping)));
                        xs.ForEach(t => this._sourceTriples.Remove(t));
                        break;
                    }
                    this._mapping.Remove(x);
                }

                //If we couldn't map an independent Node then we fail
                if (!this._mapping.ContainsKey(x))
                {
                    Debug.WriteLine("[NOT EQUAL] Independent blank node could not be mapped");
                    return false;
                }
            }
            Debug.WriteLine("Independent blank node mapping was able to map " + (this._mapping.Count - mappedSoFar) + " blank nodes, " + this._mapping.Count + " blank nodes out of a total " + gNodes.Count);
            mappedSoFar = this._mapping.Count;

            //Want to save our mapping so far here as if the mapping we produce using the dependency information
            //is flawed then we'll have to attempt brute force
            Dictionary<INode, INode> baseMapping = new Dictionary<INode, INode>(this._mapping);

            Debug.WriteLine("Using dependency information to try and map more blank nodes");

            //Now we use the dependency information to try and find mappings
            foreach (MappingPair dependency in sourceDependencies)
            {
                //If both dependent Nodes are already mapped we don't need to try mapping them again
                if (this._mapping.ContainsKey(dependency.X) && this._mapping.ContainsKey(dependency.Y)) continue;

                //Get all the Triples with this Pair in them
                List<Triple> xs;
                bool canonical = false;
                switch (dependency.Type)
                {
                    case TripleIndexType.SubjectPredicate:
                        xs = (from t in this._sourceTriples
                              where t.Subject.Equals(dependency.X) && t.Predicate.Equals(dependency.Y)
                              select t).ToList();
                        if (xs.Count == 1)
                        {
                            canonical = ((from t in this._sourceTriples
                                          where t.Subject.NodeType == NodeType.Blank && t.Predicate.NodeType == NodeType.Blank && t.Object.Equals(xs[0].Object)
                                          select t).Count() == 1);
                        }
                        break;
                    case TripleIndexType.SubjectObject:
                        xs = (from t in this._sourceTriples
                              where t.Subject.Equals(dependency.X) && t.Object.Equals(dependency.Y)
                              select t).ToList();
                        if (xs.Count == 1)
                        {
                            canonical = ((from t in this._sourceTriples
                                          where t.Subject.NodeType == NodeType.Blank && t.Predicate.Equals(xs[0].Predicate) && t.Object.NodeType == NodeType.Blank
                                          select t).Count() == 1);
                        }
                        break;
                    case TripleIndexType.PredicateObject:
                        xs = (from t in this._sourceTriples
                              where t.Predicate.Equals(dependency.X) && t.Object.Equals(dependency.Y)
                              select t).ToList();
                        if (xs.Count == 1)
                        {
                            canonical = ((from t in this._sourceTriples
                                          where t.Subject.Equals(xs[0].Subject) && t.Predicate.NodeType == NodeType.Blank && t.Object.NodeType == NodeType.Blank
                                          select t).Count() == 1);
                        }
                        break;
                    default:
                        //This means we've gone wrong somehow
                        throw new RdfException("Unknown exception occurred while trying to generate a Mapping between the two Graphs");
                }

                bool xbound = this._mapping.ContainsKey(dependency.X);
                bool ybound = this._mapping.ContainsKey(dependency.Y);

                //Look at all the possible Target Dependencies we could map to
                foreach (MappingPair target in targetDependencies)
                {
                    if (target.Type != dependency.Type) continue;

                    //If one of the Nodes we're trying to map was already mapped then we can further restrict
                    //candidate mappings
                    if (xbound)
                    {
                        if (!target.X.Equals(this._mapping[dependency.X])) continue;
                    }
                    if (ybound)
                    {
                        if (!target.Y.Equals(this._mapping[dependency.Y])) continue;
                    }

                    //If the Nodes in the Target have already been used then we can discard this possible mapping
                    if (!xbound && this._mapping.ContainsValue(target.X)) continue;
                    if (!ybound && this._mapping.ContainsValue(target.Y)) continue;

                    //Get the Triples with the Target Pair in them
                    List<Triple> ys;
                    switch (target.Type)
                    {
                        case TripleIndexType.SubjectPredicate:
                            ys = (from t in this._targetTriples
                                  where t.Subject.Equals(target.X) && t.Predicate.Equals(target.Y)
                                  select t).ToList();
                            break;
                        case TripleIndexType.SubjectObject:
                            ys = (from t in this._targetTriples
                                  where t.Subject.Equals(target.X) && t.Object.Equals(target.Y)
                                  select t).ToList();
                            break;
                        case TripleIndexType.PredicateObject:
                            ys = (from t in this._targetTriples
                                  where t.Predicate.Equals(target.X) && t.Object.Equals(target.Y)
                                  select t).ToList();
                            break;
                        default:
                            //This means we've gone wrong somehow
                            throw new RdfException("Unknown exception occurred while trying to generate a Mapping between the two Graphs");
                    }

                    //If the pairs are involved in different numbers of Triples then they aren't possible mappings
                    if (xs.Count != ys.Count) continue;

                    //If all the Triples in xs can be removed from ys then this is a valid mapping
                    if (!xbound) this._mapping.Add(dependency.X, target.X);
                    if (!ybound) this._mapping.Add(dependency.Y, target.Y);
                    if (xs.All(t => ys.Remove(t.MapTriple(h, this._mapping))))
                    {
                        if (canonical)
                        {
                            //If this was the only possible mapping for this Pair then this is a canonical mapping
                            //and can go in the base mapping so if we have to brute force we have fewer possibles
                            //to try
                            if (!baseMapping.ContainsKey(dependency.X)) baseMapping.Add(dependency.X, target.X);
                            if (!baseMapping.ContainsKey(dependency.Y)) baseMapping.Add(dependency.Y, target.Y);

                            this._bound.Add(target.X);
                            this._bound.Add(target.Y);
                            this._unbound.Remove(target.X);
                            this._unbound.Remove(target.Y);
                        }
                        break;
                    }
                    else
                    {
                        if (!xbound) this._mapping.Remove(dependency.X);
                        if (!ybound) this._mapping.Remove(dependency.Y);
                    }
                }
            }
            Debug.WriteLine("Dependency information allowed us to map a further " + (this._mapping.Count - mappedSoFar) + " nodes, we now have a possible mapping with " + this._mapping.Count + " blank nodes mapped (" + baseMapping.Count + " confirmed mappings) out of a total " + gNodes.Count + " blank nodes");

            //If we've filled in the Mapping fully then the Graphs are hopefully equal
            if (this._mapping.Count == gNodes.Count)
            {
                //Need to check we found a valid mapping
                List<Triple> ys = new List<Triple>(this._targetTriples);
                if (this._sourceTriples.All(t => ys.Remove(t.MapTriple(h, this._mapping))))
                {
                    Debug.WriteLine("[EQUAL] Generated a rules based mapping successfully");
                    return true;
                }
                else
                {
                    //Fall back should to a divide and conquer mapping
                    Debug.WriteLine("Had a potential rules based mapping but it was invalid, falling back to divide and conquer mapping with base mapping of " + baseMapping.Count + " nodes");
                    this._mapping = baseMapping;
                    return this.TryDivideAndConquerMapping(g, h, gNodes, hNodes, sourceDependencies, targetDependencies);
                }
            }
            else
            {
                Debug.WriteLine("Rules based mapping did not generate a complete mapping, falling back to divide and conquer mapping");
                return this.TryDivideAndConquerMapping(g, h, gNodes, hNodes, sourceDependencies, targetDependencies);
            }
        }

        /// <summary>
        /// Uses a divide and conquer based approach to generate a mapping without the need for brute force guessing
        /// </summary>
        /// <param name="g">1st Graph</param>
        /// <param name="h">2nd Graph</param>
        /// <param name="gNodes">1st Graph Node classification</param>
        /// <param name="hNodes">2nd Graph Node classification</param>
        /// <param name="gDegrees">1st Graph Degree classification</param>
        /// <param name="hDegrees">2nd Graph Degree classification</param>
        /// <returns></returns>
        private bool TryDivideAndConquerMapping(IGraph g, IGraph h, Dictionary<INode, int> gNodes, Dictionary<INode, int> hNodes, List<MappingPair> sourceDependencies, List<MappingPair> targetDependencies)
        {
            Debug.WriteLine("Attempting divide and conquer based mapping");

            //Need to try and split the BNodes into MSGs
            //Firstly we need a copy of the unassigned triples
            HashSet<Triple> gUnassigned = new HashSet<Triple>(this._sourceTriples);
            HashSet<Triple> hUnassigned = new HashSet<Triple>(this._targetTriples);

            //Remove already mapped nodes
            gUnassigned.RemoveWhere(t => this._mapping != null && this._mapping.Keys.Any(n => t.Involves(n)));
            hUnassigned.RemoveWhere(t => this._mapping != null && this._mapping.Any(kvp => t.Involves(kvp.Value)));

            //Compute MSGs
            List<IGraph> gMsgs = new List<IGraph>();
            GraphDiff.ComputeMSGs(g, gUnassigned, gMsgs);
            List<IGraph> hMsgs = new List<IGraph>();
            GraphDiff.ComputeMSGs(h, hUnassigned, hMsgs);

            if (gMsgs.Count != hMsgs.Count)
            {
                Debug.WriteLine("[NOT EQUAL] Blank Node portions of graphs decomposed into differing numbers of isolated sub-graphs");
                this._mapping = null;
                return false;
            }
            else if (gMsgs.Count == 1)
            {
                Debug.WriteLine("Blank Node potions of graphs did not decompose into isolated sub-graphs, falling back to brute force mapping");
                return this.TryBruteForceMapping(g, h, gNodes, hNodes, sourceDependencies, targetDependencies);
            }
            Debug.WriteLine("Blank Node portions of graphs decomposed into " + gMsgs.Count + " isolated sub-graphs");

            //Sort sub-graphs by triple count
            gMsgs.Sort(new GraphSizeComparer());
            hMsgs.Sort(new GraphSizeComparer());
            bool retry = true;
            int retryCount = 1;

            while (retry)
            {
                int found = 0;
                List<IGraph> lhsMsgs = new List<IGraph>(gMsgs);
                if (lhsMsgs.Count == 0) break;

                Debug.WriteLine("Divide and conquer attempt #" + retryCount);

                while (lhsMsgs.Count > 0)
                {
                    IGraph lhs = lhsMsgs[0];
                    lhsMsgs.RemoveAt(0);

                    //Find possible matches
                    List<IGraph> possibles = new List<IGraph>(hMsgs.Where(graph => graph.Triples.Count == lhs.Triples.Count));

                    if (possibles.Count == 0)
                    {
                        Debug.WriteLine("[NOT EQUAL] Isolated sub-graphs are of differing sizes");
                        this._mapping = null;
                        return false;
                    }

                    //Check each possible match
                    List<Dictionary<INode, INode>> partialMappings = new List<Dictionary<INode, INode>>();
                    List<IGraph> partialMappingSources = new List<IGraph>();
                    Debug.WriteLine("Dividing and conquering on isolated sub-graph with " + lhs.Triples.Count + " triples...");
                    Debug.Indent();
                    int i = 1;
                    foreach (IGraph rhs in possibles)
                    {
                        Debug.WriteLine("Testing possiblity " + i + " of " + possibles.Count);
                        Debug.Indent();
                        Dictionary<INode, INode> partialMapping;
                        if (lhs.Equals(rhs, out partialMapping))
                        {
                            partialMappings.Add(partialMapping);
                            partialMappingSources.Add(rhs);
                        }
                        Debug.Unindent();
                        i++;
                    }
                    Debug.Unindent();
                    Debug.WriteLine("Dividing and conquering done");

                    //Did we find a possible mapping for the sub-graph?
                    if (partialMappings.Count == 0)
                    {
                        // No possible mappings
                        Debug.WriteLine("[NOT EQUAL] Divide and conquer found an isolated sub-graph that has no possible matches");
                        this._mapping = null;
                        return false;
                    }
                    else if (partialMappings.Count == 1)
                    {
                        //Only one possible match
                        foreach (KeyValuePair<INode, INode> kvp in partialMappings[0])
                        {
                            if (this._mapping.ContainsKey(kvp.Key))
                            {
                                Debug.WriteLine("[NOT EQUAL] Divide and conque found a sub-graph with a single possible mapping that conflicts with the existing confirmed mappings");
                                this._mapping = null;
                                return false;
                            }
                            else
                            {
                                this._mapping.Add(kvp.Key, kvp.Value);
                            }
                        }
                        Debug.WriteLine("Divide and conquer found a unique mapping for an isolated sub-graph and confirmed an additional " + partialMappings[0].Count + " blank node mappings");

                        // Can eliminate the matched sub-graph from further consideration
                        gMsgs.RemoveAll(x => ReferenceEquals(x, lhs));
                        hMsgs.RemoveAll(x => ReferenceEquals(x, partialMappingSources[0]));
                        found++;
                    }
                    else
                    {
                        // Multiple possible mappings
                        Debug.WriteLine("Divide and conquer found " + partialMappings.Count + " possible mappings for an isolated sub-graph, unable to confirm any mappings");
                    }
                }

                retry = found > 0;

                if (retry)
                {
                    Debug.WriteLine("Divide and conquer Attempt #" + retryCount + " found " + found + " isolated sub-graph matches, will retry to see if confirmed matches permit further matches to be made");
                }
                else
                {
                    Debug.WriteLine("Divide and conquer Attempt #" + retryCount + " found no further isolated sub-graph matches");
                }
                retryCount++;
            }

            //If we now have a complete mapping test it
            if (this._mapping.Count == gNodes.Count)
            {
                //Need to check we found a valid mapping
                List<Triple> ys = new List<Triple>(this._targetTriples);
                if (this._sourceTriples.All(t => ys.Remove(t.MapTriple(h, this._mapping))))
                {
                    Debug.WriteLine("[EQUAL] Generated a divide and conquer based mapping successfully");
                    return true;
                }
                else
                {
                    //Invalid mapping
                    Debug.WriteLine("[NOT EQUAL] Divide and conquer led to an invalid mapping");
                    this._mapping = null;
                    return false;
                }
            }
            else
            {
                //If dvide and conquer fails fall back to brute force
                Debug.WriteLine("Divide and conquer based mapping did not succeed in mapping everything, have " + this._mapping.Count + " confirmed mappings out of " + gNodes.Count + " total blank nodes, falling back to brute force mapping");
                return this.TryBruteForceMapping(g, h, gNodes, hNodes, sourceDependencies, targetDependencies);
            }
        }

        /// <summary>
        /// Generates and Tests all possibilities in a brute force manner
        /// </summary>
        /// <param name="g">1st Graph</param>
        /// <param name="h">2nd Graph</param>
        /// <param name="gNodes">1st Graph Node classification</param>
        /// <param name="hNodes">2nd Graph Node classification</param>
        /// <param name="sourceDependencies">Dependencies in the 1st Graph</param>
        /// <param name="targetDependencies">Dependencies in the 2nd Graph</param>
        /// <returns></returns>
        private bool TryBruteForceMapping(IGraph g, IGraph h, Dictionary<INode, int> gNodes, Dictionary<INode, int> hNodes, List<MappingPair> sourceDependencies, List<MappingPair> targetDependencies)
        {
            Dictionary<INode, List<INode>> possibleMappings = new Dictionary<INode, List<INode>>();

            //Populate possibilities for each Node
            foreach (KeyValuePair<INode, int> gPair in gNodes)
            {
                if (!this._mapping.ContainsKey(gPair.Key))
                {
                    possibleMappings.Add(gPair.Key, new List<INode>());
                    foreach (KeyValuePair<INode, int> hPair in hNodes.Where(p => p.Value == gPair.Value && !this._bound.Contains(p.Key)))
                    {
                        possibleMappings[gPair.Key].Add(hPair.Key);
                    }

                    //If there's no possible matches for the Node we fail
                    if (possibleMappings[gPair.Key].Count == 0)
                    {
                        Debug.WriteLine("[NOT EQUAL] Unable to find any possible mappings for a blank node");
                        this._mapping = null;
                        return false;
                    }
                }
            }

            //Now start testing the possiblities
            IEnumerable<Dictionary<INode, INode>> possibles = this.GenerateMappings(new Dictionary<INode, INode>(this._mapping), possibleMappings, sourceDependencies, targetDependencies, h);
            int count = 0;
            foreach (Dictionary<INode, INode> mapping in possibles)
            {
                count++;
                if (mapping.Count != gNodes.Count) continue;

                HashSet<Triple> targets = new HashSet<Triple>(this._targetTriples);
                if (this._sourceTriples.Count != targets.Count) continue;

                // Validate the mapping
                if (this._sourceTriples.All(t => targets.Remove(t.MapTriple(h, mapping))))
                {
                    this._mapping = mapping;
                    Debug.WriteLine("[EQUAL] Succesfully brute forced a mapping on Attempt #" + count);
                    return true;
                }
            }

            Debug.WriteLine("[NOT EQUAL] No valid brute forced mappings (" + count + " were considered)");
            this._mapping = null;
            return false;
        }

        /// <summary>
        /// Helper method for brute forcing the possible mappings
        /// </summary>
        /// <param name="possibleMappings">Possible Mappings</param>
        /// <param name="sourceDependencies">Dependencies in the 1st Graph</param>
        /// <param name="targetDependencies">Dependencies in the 2nd Graph</param>
        /// <param name="target">Target Graph (2nd Graph)</param>
        /// <returns></returns>
        protected internal IEnumerable<Dictionary<INode, INode>> GenerateMappings(Dictionary<INode, INode> baseMapping, Dictionary<INode, List<INode>> possibleMappings, List<MappingPair> sourceDependencies, List<MappingPair> targetDependencies, IGraph target)
        {
            if (possibleMappings.Count == 0)
            {
                yield return baseMapping;
                yield break;
            }

            //Remove the first key and get its possibilities
            INode x = possibleMappings.Keys.First();
            List<INode> possibles = possibleMappings[x];
            possibleMappings.Remove(x);

            //For each possiblity build a possible mapping
            foreach (INode y in possibles)
            {
                Dictionary<INode, INode> test = new Dictionary<INode, INode>(baseMapping);
                test.Add(x, y);

                //Go ahead and recurse
                foreach (Dictionary<INode, INode> mapping in this.GenerateMappings(test, possibleMappings, sourceDependencies, targetDependencies, target))
                {
                    yield return mapping;
                }
            }
        }

        /// <summary>
        /// Gets the Blank Node Mapping found between the Graphs (if one was found)
        /// </summary>
        public Dictionary<INode, INode> Mapping
        {
            get
            {
                return this._mapping;
            }
        }
    }

    /// <summary>
    /// Represents a Pair of Nodes that occur in the same Triple
    /// </summary>
    class MappingPair
    {
        private INode _x, _y;
        private int _hash;
        private TripleIndexType _type;

        public MappingPair(INode x, INode y, TripleIndexType type)
        {
            this._x = x;
            this._y = y;
            this._type = type;
            this._hash = Tools.CombineHashCodes(x, y);
        }

        public INode X
        {
            get
            {
                return this._x;
            }
        }

        public INode Y
        {
            get
            {
                return this._y;
            }
        }

        public TripleIndexType Type
        {
            get
            {
                return this._type;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is MappingPair)
            {
                MappingPair p = (MappingPair)obj;
                return (this._x.Equals(p.X) && this._y.Equals(p.Y) && this._type == p.Type);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return this._hash;
        }

        public bool Contains(INode n)
        {
            return (this._x.Equals(n) || this._y.Equals(n));
        }
    }
}
