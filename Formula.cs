// Skeleton written by Joe Zachary for CS 3500, September 2013
// Read the entire skeleton carefully and completely before you
// do anything else!

// Version 1.1 (9/22/13 11:45 a.m.)

// Change log:
//  (Version 1.1) Repaired mistake in GetTokens
//  (Version 1.1) Changed specification of second constructor to
//                clarify description of how validation works

// (Daniel Kopta) 
// Version 1.2 (9/10/17) 

// Change log:
//  (Version 1.2) Changed the definition of equality with regards
//                to numeric tokens


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SpreadsheetUtilities
{
    /// <summary>
    /// Represents formulas written in standard infix notation using standard precedence
    /// rules.  The allowed symbols are non-negative numbers written using double-precision 
    /// floating-point syntax (without unary preceeding '-' or '+'); 
    /// variables that consist of a letter or underscore followed by 
    /// zero or more letters, underscores, or digits; parentheses; and the four operator 
    /// symbols +, -, *, and /.  
    /// 
    /// Spaces are significant only insofar that they delimit tokens.  For example, "xy" is
    /// a single variable, "x y" consists of two variables "x" and y; "x23" is a single variable; 
    /// and "x 23" consists of a variable "x" and a number "23".
    /// 
    /// Associated with every formula are two delegates:  a normalizer and a validator.  The
    /// normalizer is used to convert variables into a canonical form, and the validator is used
    /// to add extra restrictions on the validity of a variable (beyond the standard requirement 
    /// that it consist of a letter or underscore followed by zero or more letters, underscores,
    /// or digits.)  Their use is described in detail in the constructor and method comments.
    /// </summary>
    public class Formula
    {

        private string formula;
        private HashSet<String> variables;
        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically invalid,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer is the identity function, and the associated validator
        /// maps every string to true.  
        /// </summary>
        public Formula(String formula) :
            this(formula, s => s, s => true)
        {
        }

        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically incorrect,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer and validator are the second and third parameters,
        /// respectively.  
        /// 
        /// If the formula contains a variable v such that normalize(v) is not a legal variable, 
        /// throws a FormulaFormatException with an explanatory message. 
        /// 
        /// If the formula contains a variable v such that isValid(normalize(v)) is false,
        /// throws a FormulaFormatException with an explanatory message.
        /// 
        /// Suppose that N is a method that converts all the letters in a string to upper case, and
        /// that V is a method that returns true only if a string consists of one letter followed
        /// by one digit.  Then:
        /// 
        /// new Formula("x2+y3", N, V) should succeed
        /// new Formula("x+y3", N, V) should throw an exception, since V(N("x")) is false
        /// new Formula("2x+y3", N, V) should throw an exception, since "2x+y3" is syntactically incorrect.
        /// </summary>
        public Formula(String formula, Func<string, string> normalize, Func<string, bool> isValid)
        {
            this.formula = formula;
            StringBuilder sb = new StringBuilder();
            variables = new HashSet<String>();

            //After validating that the formula meets minimum requirements, it will then verify that each of the variables it is attempting to 
            //normalize are also still valid based off of the IsValid function.
            IEnumerator<String> tokens = GetTokens(formula).GetEnumerator();
            while (tokens.MoveNext())
            {
                sb.Append(normalize(tokens.Current));

                if (IsVarOnly(tokens.Current))
                {
                    if (!isValid(normalize(tokens.Current)))
                        throw new FormulaFormatException("Formula is invalid based on user defined rules.");
                    variables.Add(normalize(tokens.Current));
                }
            }

            //Verify that the formula is still valid after ithas been completely normalized. 
            IsValidFormula(sb.ToString());
            this.formula = sb.ToString();
        }

        /// <summary>
        /// Evaluates this Formula, using the lookup delegate to determine the values of
        /// variables.  When a variable symbol v needs to be determined, it should be looked up
        /// via lookup(normalize(v)). (Here, normalize is the normalizer that was passed to 
        /// the constructor.)
        /// 
        /// For example, if L("x") is 2, L("X") is 4, and N is a method that converts all the letters 
        /// in a string to upper case:
        /// 
        /// new Formula("x+7", N, s => true).Evaluate(L) is 11
        /// new Formula("x+7").Evaluate(L) is 9
        /// 
        /// Given a variable symbol as its parameter, lookup returns the variable's value 
        /// (if it has one) or throws an ArgumentException (otherwise).
        /// 
        /// If no undefined variables or divisions by zero are encountered when evaluating 
        /// this Formula, the value is returned.  Otherwise, a FormulaError is returned.  
        /// The Reason property of the FormulaError should have a meaningful explanation.
        ///
        /// This method should never throw an exception.
        /// </summary>
        public object Evaluate(Func<string, double> lookup)
        {
            //data structures needed for evaluation. 
            Stack<Char> operators = new Stack<Char>();
            Stack<double> values = new Stack<double>();

            IEnumerator<String> tokens = GetTokens(this.formula).GetEnumerator();
            while (tokens.MoveNext())
            {
                if (Double.TryParse(tokens.Current, out double result))
                {
                    if (operators.CheckOperators())
                    {
                        if (operators.Peek().Equals('/') && result == 0)
                            return new FormulaError("Error! Division by zero");

                        values.Push(ComputeValues(operators.Pop(), result, values.Pop()));
                    }
                    else
                        values.Push(result);
                }
                else if (IsOperator(tokens.Current[0].ToString()))
                {
                    if (tokens.Current.Equals("*") || tokens.Current.Equals("/"))
                        operators.Push(Convert.ToChar(tokens.Current));

                    else if (operators.VerifyPlusAndMinus())
                    {
                        values.Push(ComputeValues(operators.Pop(), values.Pop(), values.Pop()));
                        operators.Push(Convert.ToChar(tokens.Current));

                    }
                    else
                        operators.Push(Convert.ToChar(tokens.Current));
                }
                else if (tokens.Current.Equals(")"))
                {
                    if (operators.IfClosingParenthesesAddSub())
                        values.Push(ComputeValues(operators.Pop(), values.Pop(), values.Pop()));

                    operators.Pop();

                    if (operators.IfClosingParenthesesMultDiv())
                    {
                        double left = values.Pop();
                        double right = values.Pop();

                        if (right == 0 && operators.Peek().Equals('/'))
                            return new FormulaError("Error! Division by zero.");

                        values.Push(ComputeValues(operators.Pop(), left, right));
                    }
                }
                else if (tokens.Current.Equals("("))
                    operators.Push(Convert.ToChar(tokens.Current));
                else if (IsVarOrDigit(tokens.Current))
                {
                    if (operators.IsDivideOrMultiply())
                    {
                        try
                        {
                            lookup(tokens.Current);
                        }
                        catch (Exception)
                        {
                            return new FormulaError("Error! Unkown variable present");
                        }
                        values.Push(ComputeValues(operators.Pop(), values.Pop(), lookup(tokens.Current)));
                    }
                    else
                    {
                        try
                        {
                            lookup(tokens.Current);
                        }
                        catch (ArgumentException)
                        {
                            return new FormulaError("Error! Unkown variable present");
                        }
                        values.Push(lookup(tokens.Current));
                    }
                }
            }
            if (operators.VerifyFinalStateOperators() && values.Count == 2)
            {
                double right = values.Pop();
                double left = values.Pop();
                char op = operators.Pop();
                if (op.Equals('/') && right == 0)
                    return new FormulaError("Error! Division by 0");

                values.Push(ComputeValues(op, left, right));
            }
            else
                return values.Pop();

            return values.Pop();
        }

        /// <summary>
        /// This method will compute and return the value from a given operator and 2 values from the formula. 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        private double ComputeValues(char c, double left, double right)
        {
            double computedValue = 0;
            switch (c)
            {
                case '+':
                    computedValue = left + right;
                    break;
                case '-':
                    computedValue = right - left;
                    break;
                case '*':
                    computedValue = left * right;
                    break;
                case '/':
                    computedValue = right / left;
                    break;

            }
            return computedValue;
        }

        /// <summary>
        /// Determines whether a variable is a double, variable starting with "_" or a letter.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool IsVarOrDigit(String token)
        {
            if (Double.TryParse(token, out double result))
                return true;
            else if (Char.IsLetter(token[0]))
                return true;
            else if (token[0].Equals('_'))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Veifies whether token provided is a valid start to a variable. 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool IsVarOnly(String token)
        {
            if (Char.IsLetter(token[0]))
                return true;
            else if (token[0].Equals('_'))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Enumerates the normalized versions of all of the variables that occur in this 
        /// formula.  No normalization may appear more than once in the enumeration, even 
        /// if it appears more than once in this Formula.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x+y*z", N, s => true).GetVariables() should enumerate "X", "Y", and "Z"
        /// new Formula("x+X*z", N, s => true).GetVariables() should enumerate "X" and "Z".
        /// new Formula("x+X*z").GetVariables() should enumerate "x", "X", and "z".
        /// </summary>
        public IEnumerable<String> GetVariables()
        {
            return variables;
        }

        /// <summary>
        /// Returns a string containing no spaces which, if passed to the Formula
        /// constructor, will produce a Formula f such that this.Equals(f).  All of the
        /// variables in the string should be normalized.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x + y", N, s => true).ToString() should return "X+Y"
        /// new Formula("x + Y").ToString() should return "x+Y"
        /// </summary>
        public override string ToString()
        {
            return formula;
        }

        /// <summary>
        /// If obj is null or obj is not a Formula, returns false.  Otherwise, reports
        /// whether or not this Formula and obj are equal.
        /// 
        /// Two Formulae are considered equal if they consist of the same tokens in the
        /// same order.  To determine token equality, all tokens are compared as strings 
        /// except for numeric tokens and variable tokens.
        /// Numeric tokens are considered equal if they are equal after being "normalized" 
        /// by C#'s standard conversion from string to double, then back to string. This 
        /// eliminates any inconsistencies due to limited floating point precision.
        /// Variable tokens are considered equal if their normalized forms are equal, as 
        /// defined by the provided normalizer.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        ///  
        /// new Formula("x1+y2", N, s => true).Equals(new Formula("X1  +  Y2")) is true
        /// new Formula("x1+y2").Equals(new Formula("X1+Y2")) is false
        /// new Formula("x1+y2").Equals(new Formula("y2+x1")) is false
        /// new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Formula))
                return false;

            Formula otherForm = (Formula)obj;

            IEnumerator<String> currentFormula = GetTokens(this.formula).GetEnumerator();
            IEnumerator<String> otherFormula = GetTokens(otherForm.formula).GetEnumerator();

            while (currentFormula.MoveNext() && otherFormula.MoveNext())
            {
                if ((IsVarOrDigit(currentFormula.Current) && IsVarOrDigit(otherFormula.Current)) || (currentFormula.Current[0].Equals('_') && otherFormula.Current[0].Equals('_')))
                {
                    if (Double.TryParse(currentFormula.Current, out double result) && Double.TryParse(otherFormula.Current, out double otherResult))
                    {
                        String current = result.ToString();
                        string other = otherResult.ToString();

                        if (current == other)
                            continue;
                        else
                            return false;
                    }
                }
                else if (currentFormula.Current.Length == 1 && otherFormula.Current.Length == 1 && IsOperator(currentFormula.Current) && IsOperator(otherFormula.Current))
                {
                    continue;
                }
                else if ((currentFormula.Current.Equals("(") && otherFormula.Current.Equals("(")) || (currentFormula.Current.Equals(")") && otherFormula.Current.Equals(")")))
                    continue;
                else
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Reports whether f1 == f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return true.  If one is
        /// null and one is not, this method should return false.
        /// </summary>
        public static bool operator ==(Formula f1, Formula f2)
        {
            if (ReferenceEquals(f1, null))
                return false;
            if (ReferenceEquals(f2, null))
                return false;
            return f1.Equals(f2);
        }

        /// <summary>
        /// Reports whether f1 != f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return false.  If one is
        /// null and one is not, this method should return true.
        /// </summary>
        public static bool operator !=(Formula f1, Formula f2)
        {
            return !f1.Equals(f2);
        }

        /// <summary>
        /// Returns a hash code for this Formula.  If f1.Equals(f2), then it must be the
        /// case that f1.GetHashCode() == f2.GetHashCode().  Ideally, the probability that two 
        /// randomly-generated unequal Formulae have the same hash code should be extremely small.
        /// </summary>
        public override int GetHashCode()
        {
            return this.formula.GetHashCode();
        }

        private bool ValidVariable(char token)
        {
            if (char.IsLetterOrDigit(token) || token.Equals('_'))
                return true;
            return false;
        }

        /// <summary>
        /// Used to validate whether or not a formula provided by user is valid. This method relies heavily on other methods that are encapsulated in 
        /// the FormulaEvaluationHelpersExtension class. Based upon the boolean statements, each token will be evaluated and will either pass as a valid
        /// formula, or throw a Formula Format Exception. 
        /// </summary>
        /// <param name="s"></param>
        private void IsValidFormula(String s)
        {
            if (s == "")
                throw new FormulaFormatException("Formula can't be empty");

            int numOfOpeningParen = 0;
            int numOfClosingParen = 0;
            bool firstToken = true;

            string previousToken = s.Substring(0, 1);
            if (IsOperator(previousToken) || previousToken.Equals(")"))
                throw new FormulaFormatException("formula can't begin with " + previousToken);

            if (IsOperator(s.Substring(s.Length - 1)))
                throw new FormulaFormatException("Formula can't end with operator.");

            //how to rebuild this method to make it more effective? 
            foreach (string token in GetTokens(s))
            {
                if (firstToken)
                {
                    if (token.Equals("("))
                    {
                        numOfOpeningParen++;
                        previousToken = token;
                        firstToken = false;
                        continue;
                    }
                    else if (char.IsLetter(Convert.ToChar(token[0])))
                    {
                        for (int i = 0; i < token.Length; i++)
                        {
                            if (!ValidVariable(Convert.ToChar(token[i])))
                                throw new FormulaFormatException("INVALID FORMAT: The variable is in incorrect format.");
                        }
                    }
                    else if (!double.TryParse(token, out double value))
                        throw new FormulaFormatException("INVALID FORMAT: " + token + " is invalid.");

                    firstToken = false;
                    previousToken = token;
                    continue;
                }
                if (Char.IsLetterOrDigit(Convert.ToChar(token[0])) || token[0].Equals('.') || token[0].Equals('_')) //previous is (, 
                {
                    if (char.IsLetter(Convert.ToChar(token[0])) || token[0].Equals('_'))
                        for (int i = 0; i < token.Length; i++)
                        {
                            if (!ValidVariable(Convert.ToChar(token[i])))
                                throw new FormulaFormatException("INVALID FORMAT: " + token[i] + " is invalid.");
                        }
                    else if (!Double.TryParse(token, out double result))
                        throw new FormulaFormatException("INVALID FORMAT: " + token + " is in an invalid format.");

                    if (previousToken == "(")
                    {
                        previousToken = token;
                        continue;
                    }
                    else if (!IsOperator(previousToken))
                        throw new FormulaFormatException("INVALID FORMAT: operator missing.");

                    previousToken = token;
                }
                else if (IsOperator(token)) //previous is : num, var or )
                {
                    if (Char.IsLetterOrDigit(Convert.ToChar(previousToken[0])) || previousToken[0].Equals("_"))
                    {
                        previousToken = token;
                        continue;
                    }

                    else if (previousToken.Equals(")"))
                    {
                        previousToken = token;
                        continue;
                    }
                    else if (!double.TryParse(previousToken, out double value))
                        throw new FormulaFormatException("INVALID FORMAT: formula is invalid.");

                    previousToken = token;
                }
                else if (token.Equals("(")) //previous is : operator or "("
                {
                    numOfOpeningParen++;
                    if (previousToken.Equals("("))
                    {
                        previousToken = token;
                        continue;
                    }
                    if (!IsOperator(previousToken))
                        throw new FormulaFormatException("INVALID FORMAT: " + previousToken + " is causing incorrect format.");

                    previousToken = token;
                }
                else if (token.Equals(")"))
                {
                    numOfClosingParen++;
                    if (IsOperator(previousToken))
                        throw new FormulaFormatException("INVALID FORMAT: " + previousToken + " is invalid in this format.");
                    previousToken = token;
                }
                else
                    throw new FormulaFormatException("INVALID FORMAT: " + token + " is an invalid character.");
                previousToken = token;
            }
            if (numOfClosingParen != numOfOpeningParen)
                throw new FormulaFormatException("The number of closing parentheses doesn't match the number of opening.");
        }

        /// <summary>
        /// Verifies whether a token is a valid operator or not. 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        internal static bool IsOperator(string s)
        {
            switch (s)
            {
                case "*":
                    return true;
                case "/":
                    return true;
                case "+":
                    return true;
                case "-":
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Given an expression, enumerates the tokens that compose it.  Tokens are left paren;
        /// right paren; one of the four operator symbols; a string consisting of a letter or underscore
        /// followed by zero or more letters, digits, or underscores; a double literal; and anything that doesn't
        /// match one of those patterns.  There are no empty tokens, and no token contains white space.
        /// </summary>
        private static IEnumerable<string> GetTokens(String formula)
        {
            // Patterns for individual tokens
            String lpPattern = @"\(";
            String rpPattern = @"\)";
            String opPattern = @"[\+\-*/]";
            String varPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
            String doublePattern = @"(?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?";
            String spacePattern = @"\s+";

            // Overall pattern
            String pattern = String.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5})",
                                            lpPattern, rpPattern, opPattern, varPattern, doublePattern, spacePattern);

            // Enumerate matching tokens that don't consist solely of white space.
            foreach (String s in Regex.Split(formula, pattern, RegexOptions.IgnorePatternWhitespace))
            {
                if (!Regex.IsMatch(s, @"^\s*$", RegexOptions.Singleline))
                {
                    yield return s;
                }
            }

        }
    }


    /// <summary>
    /// Used to report syntactic errors in the argument to the Formula constructor.
    /// </summary>
    public class FormulaFormatException : Exception
    {
        /// <summary>
        /// Constructs a FormulaFormatException containing the explanatory message.
        /// </summary>
        public FormulaFormatException(String message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Used as a possible return value of the Formula.Evaluate method.
    /// </summary>
    public struct FormulaError
    {
        /// <summary>
        /// Constructs a FormulaError containing the explanatory reason.
        /// </summary>
        /// <param name="reason"></param>
        public FormulaError(String reason)
            : this()
        {
            Reason = reason;
        }

        /// <summary>
        ///  The reason why this FormulaError was created.
        /// </summary>
        public string Reason { get; private set; }
    }


    /// <summary>
    /// This class is used to encapsulate private and internal extension methods for a Stack object that allows for 
    /// refactoring of commonly used lines along with maintaining clean code coverage. 
    /// </summary>
    static class FormulaStackhelpers
    {
        internal static bool CheckOperators(this Stack<Char> operators)
        {
            if (operators.Count > 0 && (operators.Peek().Equals('*') || operators.Peek().Equals('/')))
                return true;
            else
                return false;

        }

        /// <summary>
        /// Verifies if "*" or "/" are at the top of the stack. 
        /// </summary>
        /// <param name="operators"></param>
        /// <returns></returns>
        internal static bool IsDivideOrMultiply(this Stack<Char> operators)
        {
            return operators.Count > 0 && (operators.Peek().Equals('*') || operators.Peek().Equals('/'));
        }

        /// <summary>
        /// Verifies that operator stack isn't empty and "+" or "-" are at the top.
        /// </summary>
        /// <param name="operators"></param>
        /// <returns></returns>
        internal static bool VerifyPlusAndMinus(this Stack<Char> operators)
        {
            return operators.Count > 0 && (operators.Peek().Equals('+') || operators.Peek().Equals('-'));
        }

        /// <summary>
        /// Verifies if ")" is present, that operator stack is greater than 1 and the next operator is "-" or "+"
        /// </summary>
        /// <param name="operators"></param>
        /// <returns></returns>
        internal static bool IfClosingParenthesesAddSub(this Stack<Char> operators)
        {
            return operators.Count > 1 && (operators.Peek().Equals('+') || operators.Peek().Equals('-'));
        }

        /// <summary>
        /// Verifies if closing parentheses is the current value, that operator count is greater than 0 and that the next operator is division or multiplication.
        /// </summary>
        /// <param name="operators"></param>
        /// <returns></returns>
        internal static bool IfClosingParenthesesMultDiv(this Stack<Char> operators)
        {
            return operators.Count > 1 && (operators.Peek().Equals('*') || operators.Peek().Equals('/'));
        }

        /// <summary>
        /// Verifies that if "(" is present, it will check operator stack to see if there is an operator and if it equals "(".
        /// </summary>
        /// <param name="operators"></param>
        /// <returns></returns>
        internal static bool IfOpeningParentheses(this Stack<Char> operators)
        {
            return operators.Count > 0 && operators.Peek().Equals('(');
        }

        /// <summary>
        /// Verifies that during the final possible computation, the only characters on the operator stack may be -, + and that there is an operator to pop.
        /// </summary>
        /// <param name="operators"></param>
        /// <returns></returns>
        internal static bool VerifyFinalStateOperators(this Stack<Char> operators)
        {
            return operators.Count > 0 && (operators.Peek().Equals('-') || operators.Peek().Equals('+') || operators.Peek().Equals('/') || operators.Peek().Equals('*'));
        }
    }




}

