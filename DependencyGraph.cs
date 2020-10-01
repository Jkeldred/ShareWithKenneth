// Skeleton implementation written by Joe Zachary for CS 3500, September 2013.
// Version 1.1 (Fixed error in comment for RemoveDependency.)
// Version 1.2 - Daniel Kopta 
//               (Clarified meaning of dependent and dependee.)
//               (Clarified names in solution/project structure.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SpreadsheetUtilities
{

    /// <summary>
    /// (s1,t1) is an ordered pair of strings
    /// t1 depends on s1; s1 must be evaluated before t1
    /// 
    /// A DependencyGraph can be modeled as a set of ordered pairs of strings.  Two ordered pairs
    /// (s1,t1) and (s2,t2) are considered equal if and only if s1 equals s2 and t1 equals t2.
    /// Recall that sets never contain duplicates.  If an attempt is made to add an element to a 
    /// set, and the element is already in the set, the set remains unchanged.
    /// 
    /// Given a DependencyGraph DG:
    /// 
    ///    (1) If s is a string, the set of all strings t such that (s,t) is in DG is called dependents(s).
    ///        (The set of things that depend on s)    
    ///        
    ///    (2) If s is a string, the set of all strings t such that (t,s) is in DG is called dependees(s).
    ///        (The set of things that s depends on) 
    //
    // For example, suppose DG = {("a", "b"), ("a", "c"), ("b", "d"), ("d", "d")}
    //     dependents("a") = {"b", "c"}
    //     dependents("b") = {"d"}
    //     dependents("c") = {}
    //     dependents("d") = {"d"}
    //     dependees("a") = {}
    //     dependees("b") = {"a"}
    //     dependees("c") = {"a"}
    //     dependees("d") = {"b", "d"}
    /// </summary>
    public class DependencyGraph
    {
        private Dictionary<String, HashSet<String>> dependents;
        private Dictionary<String, HashSet<String>> dependees;
        private int numOfOrderedPairs;

        /// <summary>
        /// Creates an empty DependencyGraph.
        /// </summary>
        public DependencyGraph()
        {
            dependents = new Dictionary<string, HashSet<string>>();
            dependees = new Dictionary<string, HashSet<string>>();
            numOfOrderedPairs = 0;
        }


        /// <summary>
        /// The number of ordered pairs in the DependencyGraph.
        /// </summary>

        public int Size
        {
            get { return numOfOrderedPairs; }
        }


        /// <summary>
        /// The size of dependees(s).
        /// This property is an example of an indexer.  If dg is a DependencyGraph, you would
        /// invoke it like this:
        /// dg["a"]
        /// It should return the size of dependees("a")
        /// </summary>
        public int this[string s]
        {
            get
            {
                if (dependees.ContainsKey(s))
                    return GetDependees(s).Count();
                else
                    return 0;
            }
        }


        /// <summary>
        /// Reports whether dependents(s) is non-empty.
        /// </summary>
        public bool HasDependents(string s)
        {
            if (dependents.ContainsKey(s))
                return dependents[s].Count > 0;
            else
                return false;
        }


        /// <summary>
        /// Reports whether dependees(s) is non-empty.
        /// </summary>
        public bool HasDependees(string s)
        {
            if (dependees.ContainsKey(s))
                return dependees[s].Count > 0;
            else
                return false;
        }


        /// <summary>
        /// Enumerates dependents(s).
        /// </summary>
        public IEnumerable<string> GetDependents(string s)
        {
            if (dependents.ContainsKey(s))
                return dependents[s].AsEnumerable<String>();
            else
                return new List<String>();

        }

        /// <summary>
        /// Enumerates dependees(s).
        /// </summary>
        public IEnumerable<string> GetDependees(string s)
        {
            if (dependees.ContainsKey(s))
                return dependees[s].AsEnumerable<String>();
            else
                return new List<String>();
        }


        /// <summary>
        /// <para>Adds the ordered pair (s,t), if it doesn't exist</para>
        /// 
        /// <para>This should be thought of as:</para>   
        /// 
        ///   t depends on s
        ///
        /// </summary>
        /// <param name="s"> s must be evaluated first. T depends on S</param>
        /// <param name="t"> t cannot be evaluated until s is</param>        /// 
        public void AddDependency(string s, string t)
        {
            if (dependents.ContainsKey(s) && !dependents[s].Contains(t))
                numOfOrderedPairs++;
            else if (!dependents.ContainsKey(s))
                numOfOrderedPairs++;

            dependents.VerifyAddDependents(s, t);
            dependees.VerifyAddDependees(s, t);
        }


        /// <summary>
        /// Removes the ordered pair (s,t), if it exists
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public void RemoveDependency(string s, string t)
        {
            //I need to use the HashSet of Dependees to remove all of the dependents 
            if (dependents.ContainsKey(s) && dependents[s].Contains(t))
                numOfOrderedPairs--;
            
            if (dependents.ContainsKey(s) && dependents[s].Contains(t))
            {
                dependents[s].Remove(t);
                dependees[t].Remove(s);
            }
        }


        /// <summary>
        /// Removes all existing ordered pairs of the form (s,r).  Then, for each
        /// t in newDependents, adds the ordered pair (s,t).
        /// </summary>
        public void ReplaceDependents(string s, IEnumerable<string> newDependents)
        {
            //IEnumerator<String> dependentsInSet = GetDependees(s).GetEnumerator();
            //Stack<String> currentDependents = new Stack<String>();

            //// I need to go to each of the dependees list and remove s.
            //while (dependentsInSet.MoveNext())
            //    currentDependents.Push(dependentsInSet.Current);

            //while (currentDependents.Count > 0)
            //    RemoveDependency(s, currentDependents.Pop());

            //foreach (String dependent in newDependents)
            //{
            //    AddDependency(s, dependent);
            //}


            //USE THESE 2 to pass tests - - this is how to get around the error for modifying an object you're currently iterating through.
            foreach (string r in new List<String>(GetDependents(s)))
            {
                RemoveDependency(s, r);
            }
            foreach (string t in new List<String>(newDependents))
            {
                AddDependency(s, t);
            }
        }


        /// <summary>
        /// Removes all existing ordered pairs of the form (r,s).  Then, for each 
        /// t in newDependees, adds the ordered pair (t,s).
        /// </summary>
        public void ReplaceDependees(string s, IEnumerable<string> newDependees)
        {
            //s is dependent on r 
            //I need to remove s from all r dependee HashSets
            IEnumerator<String> DependentsInSet = GetDependees(s).GetEnumerator();
            Stack<String> currentDependents = new Stack<String>();

            while (DependentsInSet.MoveNext())
                currentDependents.Push(DependentsInSet.Current);

            while (currentDependents.Count > 0)
                RemoveDependency(currentDependents.Pop(), s);

            foreach (string dependee in newDependees)
            {
                AddDependency(dependee, s);
            }
        }

    }

    /// <summary>
    /// PS2 Extensions is used to factor out larger portions of code for adding to both the dependents and dependees dictionaries. 
    /// </summary>
    static class PS2Extensions
    {
        /// <summary>
        /// Verifies that the values (s,t) can be added to the dependents dictionary. If s is already contained in dependents, 
        /// it will simply add it to the HashTable. If {s} isn't present, it will add it to the dictionary, create a new HashTable and
        /// add {t}.
        /// </summary>
        /// <param name="dependents"></param>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public static void VerifyAddDependents(this Dictionary<string, HashSet<string>> dependents, string s, string t)
        {
            if (!dependents.ContainsKey(s))
            {
                dependents.Add(s, new HashSet<string>());
                dependents[s].Add(t);
            }
            else
                dependents[s].Add(t);

            if (!dependents.ContainsKey(t))
                dependents.Add(t, new HashSet<string>());
        }

        /// <summary>
        /// Verifies that the values (t,s) can be added to the dependees dictionary. If {{t} is already in dependees, it will add {s}
        /// to the HashTable. If {t} isn't present, it will add it, create a new HashTable and add {s}. 
        /// </summary>
        /// <param name="dependees"></param>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public static void VerifyAddDependees(this Dictionary<string, HashSet<string>> dependees, string s, string t)
        {
            if (!dependees.ContainsKey(t))
            {
                dependees.Add(t, new HashSet<string>());
                dependees[t].Add(s);
            }
            else
                dependees[t].Add(s);

            if (!dependees.ContainsKey(s))
                dependees.Add(s, new HashSet<string>());
        }
    }
}
