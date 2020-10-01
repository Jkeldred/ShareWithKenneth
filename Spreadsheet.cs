using SpreadsheetUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml;

namespace SS
{
    /// <summary>
    /// This class is designed to track the contnts and values of the cells in the sheet, including their values and contents along with name. 
    /// Any updates to cells contents will be performed through this class. 
    /// </summary>
    public class Spreadsheet : AbstractSpreadsheet
    {

        private Dictionary<String, Cell> cells = new Dictionary<String, Cell>();
        private DependencyGraph cellDependencies = new DependencyGraph();

        /// <summary>
        /// Returns The contents of a specified cell based off te name provided. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override object GetCellContents(string name)
        {
            if (name == null)
                throw new InvalidNameException();

            IsValidName(name);
            if (!cells.ContainsKey(name))
                return "";

            if (cells[name].Contents.GetType().Equals(typeof(String)))
                return (String)cells[name].Contents;
            else if (cells[name].Contents.GetType().Equals(typeof(Formula)))
                return (Formula)cells[name].Contents;
            else
                return (Double)cells[name].Contents;
        }

        /// <summary>
        /// Returns all the names of the non-empty cells. 
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<string> GetNamesOfAllNonemptyCells()
        {
            return cells.Keys;
        }

        /// <summary>
        /// Sets the contnts of a cell to the number provided as an argument. If the cell name is already found in the cells dictionary, it will replace 
        /// whatever the cell is currently holding with the new value provided as an argument. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        public override IList<string> SetCellContents(string name, double number)
        {
            if (name == null)
                throw new InvalidNameException();

            IsValidName(name);
            Cell previousCell = null;
            if (cells.ContainsKey(name))
                previousCell = cells[name];

            AddCellDouble(name, number);

            ISet<String> directDependents = new HashSet<String>();
            IList<String> allDependees = new List<String>();
            IEnumerator<String> dependees = GetDirectDependents(name).GetEnumerator();

            while (dependees.MoveNext())
                directDependents.Add(dependees.Current);

            allDependees.Add(name);

            IEnumerator<String> dependeesRecalculated = GetCellsToRecalculate(directDependents).GetEnumerator();
            while (dependeesRecalculated.MoveNext())
                allDependees.Add(dependeesRecalculated.Current);


            return allDependees;
        }

        /// <summary>
        ///  Sets the contents of a cell object to the provided name and string value. It will verify that the cells dictionary doesn'talready contain this 
        ///  name and if so, it will replace whatever value is current there.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public override IList<string> SetCellContents(string name, string text)
        {
            IsValidName(name);
            if (name == null)
                throw new InvalidNameException();

            if (text == null)
                throw new ArgumentNullException();

            if (text == "")
                return new List<String>();


            AddCellString(name, text);

            ISet<String> directDependents = new HashSet<String>();
            IList<String> allDependees = new List<String>();
            IEnumerator<String> dependees = GetDirectDependents(name).GetEnumerator();

            while (dependees.MoveNext())
                directDependents.Add(dependees.Current);

            allDependees.Add(name);

            IEnumerator<String> dependeesRecalculated = GetCellsToRecalculate(directDependents).GetEnumerator();
            while (dependeesRecalculated.MoveNext())
                allDependees.Add(dependeesRecalculated.Current);

            return allDependees;
        }

        /// <summary>
        /// Used to create a copy of a cell that is contained in a dictionary. This prevents the issue of creating a reference to the dictionary cell 
        /// and not a new cell altogether. 
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        private Cell CellCopy(Cell cell)
        {
            Cell cellCopy = new Cell();
            cellCopy.Contents = cell.Contents;
            cellCopy.Name = cell.Name;
            return cellCopy;
        }


        /// <summary>
        /// Sets the content of a Cell object to the name provided along with the contents given (Formula) as an argument. If the cell is already contained 
        /// in the dictionary, it will update it's contents and verify that no cycles exist after updating cell value. If a cycle does exist, it will
        /// revert back to the previous cell version. If the cell isn't contained, it will add cell to dictionary. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="formula"></param>
        /// <returns></returns>
        public override IList<string> SetCellContents(string name, Formula formula)
        {
            IsValidName(name);

            if (formula is null)
                throw new ArgumentNullException();

            Cell previousCell = null;
            if (cells.ContainsKey(name))
                previousCell = CellCopy(cells[name]);

            AddCellFormula(name, formula);

            ISet<String> directDependents = new HashSet<String>();
            IList<String> allDependees = new List<String>();
            IEnumerator<String> variables = formula.GetVariables().GetEnumerator();
            IEnumerator<String> dependees = GetDirectDependents(name).GetEnumerator();

            while (variables.MoveNext())
                cellDependencies.AddDependency(name, variables.Current);

            while (dependees.MoveNext())
                directDependents.Add(dependees.Current);

            allDependees.Add(name);

            try
            {
                IEnumerator<String> dependeesRecalculated = GetCellsToRecalculate(directDependents).GetEnumerator();
                while (dependeesRecalculated.MoveNext())
                    allDependees.Add(dependeesRecalculated.Current);
            }
            catch (CircularException)
            {
                if (previousCell is null)
                {
                    cells.Remove(name);
                    cellDependencies.ReplaceDependents(name, new List<String>());
                    cellDependencies.ReplaceDependees(name, new List<String>());
                    throw;
                }
                if (previousCell.Contents.GetType().Equals(typeof(Double)))
                    AddCellDouble(name, (Double)previousCell.Contents);
                else if (previousCell.Contents.GetType().Equals(typeof(Formula)))
                    AddCellFormula(name, (Formula)previousCell.Contents);
                else if (previousCell.Contents.GetType().Equals(typeof(String)))
                    AddCellString(name, (String)previousCell.Contents);
                throw;
            }
            return allDependees;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="contents"></param>
        private void AddCellDouble(String name, Double contents)
        {
            if (cells.ContainsKey(name))
            {
                cells[name].Contents = contents;
                if (cellDependencies.HasDependents(name))
                {
                    cells[name].Contents = contents;
                    IEnumerable<string> emptyDependents = new List<String>();
                    cellDependencies.ReplaceDependents(name, emptyDependents);
                }
            }
            else
            {
                Cell cell = new Cell
                {
                    Name = name,
                    Contents = contents
                };
                cells.Add(name, cell);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="formula"></param>
        private void AddCellFormula(String name, Formula formula)
        {
            if (cells.ContainsKey(name))
            {
                cells[name].Contents = formula;
                IEnumerable<String> variables = formula.GetVariables();
                cellDependencies.ReplaceDependents(name, variables);
            }
            else
            {
                Cell cell = new Cell
                {
                    Name = name,
                    Contents = formula
                };
                cells.Add(name, cell);
            }
        }

        /// <summary>
        /// Helper method for the SetCellContents method that takes in String values for contents. The method checks if the cell name is already contained 
        /// in the dictionary. If it is, it will see if it has dependencies. If dependencies are found, it will remove those since the content has been 
        /// removed and replaced with a String value. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="contents"></param>
        private void AddCellString(String name, String contents)
        {
            if (cells.ContainsKey(name))
            {
                cells[name].Contents = contents;
                if (cellDependencies.HasDependents(name))
                {
                    IEnumerable<String> emptyDependents = new List<String>();
                    cellDependencies.ReplaceDependents(name, emptyDependents);
                }
            }
            else
            {
                Cell cell = new Cell
                {
                    Name = name,
                    Contents = contents
                };
                cells.Add(name, cell);
            }
        }

        /// <summary>
        /// Returns an Enumerator object of all the direct dependencies of a given Cell. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected override IEnumerable<string> GetDirectDependents(string name)
        {
            return cellDependencies.GetDependees(name);
        }

        /// <summary>
        /// Regex that will validate whether or not a name is valid. The proper format is name begins with either a letter or '_', followed by 0 or more letters,
        /// underscores, and or digits. It cannot begin with a digit. If name is null, exception is thrown. 
        /// </summary>
        /// <param name="name"></param>
        private void IsValidName(String name)
        {
            if (name == null)
                throw new InvalidNameException();

            //verifies if the cell name is valid
            Regex validName = new Regex("^[_?a-zA-Z][_a-zA-Z0-9]*");
            if (!validName.IsMatch(name))
                throw new InvalidNameException();
        }

        /// <summary>
        /// The Cell class will be used to define what is contained in a cell, namely the contents and value
        /// </summary>
        private class Cell
        {
            public String Name { get; set; }

            public Object Contents
            {
                get;
                set;
            }
        }


    }

}
