// =========================================================
// Description: simple command line parser
// Author: Jens Boettge <jub-git@mpi-halle.mpg.de>
// Date: Jan. 2013 - 15.02.2013
// Release 0.2.1
// Idea taken from: http://www.codeproject.com/Articles/3111/C-NET-Command-Line-Arguments-Parser
// =========================================================

using System;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace CommandLine.Utility
{
    /// <summary>
    /// Arguments class
    /// </summary>
    public class Arguments{
        // Variables
        private StringDictionary Parameters;
        private StringCollection Operands;
        private StringCollection Unary;

        // Constructor
        public Arguments(string[] Args)
        {
            Parameters = new StringDictionary();
            Operands = new StringCollection();
            Unary = new StringCollection();
            
            // @"..." - Verbatim String; doesn't recognize backslash sequences except \"
            Regex Spliter = new Regex(@"^-{1,2}|^/|=",
                RegexOptions.IgnoreCase|RegexOptions.Compiled);

            Regex Remover = new Regex(@"^['""]?(.*?)['""]?$",
                RegexOptions.IgnoreCase|RegexOptions.Compiled);

            string Parameter = null;
            string[] Parts;
            
            // Valid parameters forms:
            // {-,/,--}param{ ,=,:}((",')value(",'))
            // Examples:
            // -param1 value1 --param2 /param3:"Test-:-work"
            //   /param4=happy -param5 '--=nice=--'
            foreach(string Txt in Args)
            {
                // Look for new parameters (-,/ or --) and a
                // possible enclosed value (=)
                Parts = Spliter.Split(Txt,3);
                //Console.Write("DEBUG: Arg: [" + Txt+"]\n");
                //Console.Write("DEBUG: '" + string.Join("|",Parts) + "' ("+Parts.Length+")\n");
                
                switch(Parts.Length){
                // Found a value (for the last parameter
                // found (space separator))
                case 1:
                    if(Parameter != null)
                    {
                        if(!Parameters.ContainsKey(Parameter))
                        {
                            Parts[0] =
                                Remover.Replace(Parts[0], "$1");

                            Parameters.Add(Parameter, Parts[0]);
                        }
                        Parameter=null;
                    }
                    // else Error: no parameter waiting for a value (skipped)
                   	else 
                   	{	Operands.Add(Parts[0]);
                   	}
                    break;

                // Found just a parameter
                case 2:
                    // The last parameter is still waiting.
                    // With no value, set it to true.
                    if(Parameter!=null)
                    {
                        if(!Parameters.ContainsKey(Parameter))
                        {
                            Parameters.Add(Parameter, "true");
                            Unary.Add(Parameter);
                        }
                    }
                    Parameter=Parts[1];
                    break;

                // Parameter with enclosed value
                case 3:
                    // The last parameter is still waiting.
                    // With no value, set it to true.
                    if(Parameter != null)
                    {
                        if(!Parameters.ContainsKey(Parameter))
                        {
                            Parameters.Add(Parameter, "true");
                            Unary.Add(Parameter);
                        }
                    }

                    Parameter = Parts[1];

                    // Remove possible enclosing characters (",')
                    if(!Parameters.ContainsKey(Parameter))
                    {
                        Parts[2] = Remover.Replace(Parts[2], "$1");
                        Parameters.Add(Parameter, Parts[2]);
                    }

                    Parameter=null;
                    break;
                }
            }
            // In case a parameter is still waiting
            if(Parameter != null)
            {
                if(!Parameters.ContainsKey(Parameter))
                {
                    Parameters.Add(Parameter, "true");
                    Unary.Add(Parameter);
                }
            }
        }

        // Retrieve a parameter value if it exists
        // (overriding C# indexer property)
        public string this [string Param]
        {
            get
            {
                return(Parameters[Param]);
            }
        }
        
        // Retrieve parameter keys
        public System.Collections.ICollection Keys()
        {
        	return(this.Parameters.Keys);
        }  
        
        // Retrieve numbert of parameters
        public int GetNumParams()
        {
        	return(this.Parameters.Count);
        }          
        
        // Retrieve operands
        public StringCollection GetOperands()
        {
        	return(this.Operands);
        }  
        
        // Retrieve unary parameters (not operands)
        public StringCollection GetUnary()
        {
        	return(this.Unary);
        }          

        // Retrieve number of operands
        public int GetNumOperands()
        {
        	return(this.Operands.Count);
        }    

        // Retrieve number of unary parameters
        public int GetNumUnary()
        {
        	return(this.Unary.Count);
        }  

        public bool isUnary(string Param)
        {
        	return this.Unary.Contains(Param);
        }        
        
        public bool isSet(string Param)
        {
        	string val=this.Parameters[Param];
        	if (val==null) return false;
        	if (val.ToLower()=="false") return false;
        	else return true;
        }
        
        public byte IisSet(string Param)
        {
        	string val=this.Parameters[Param];
        	if (val==null) return (byte)0;
        	if (val.ToLower()=="false") return (byte)0;
        	else return (byte)1;
        }        
        
        public bool isTrue(string Param)
        {
        	string val=this.Parameters[Param];
        	if (val==null) return false;
        	if (val.ToLower()=="true") return true;
        	else return false;
        }        
         
        public bool isFalse(string Param)
        {
        	string val=this.Parameters[Param];
        	if (val==null) return false;
        	if (val.ToLower()=="false") return true;
        	else return false;
        }          
    }
}