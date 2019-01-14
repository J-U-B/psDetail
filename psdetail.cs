// =========================================================
// Description: more details about running processes
// Author: Jens Boettge <jub-git@mpi-halle.mpg.de>
// Date: Dec. 2012 - 2019
// =========================================================

/* ==================== CHANGELOG ====================
* 0.1.11.0dev - 2019-01
 * neue Option: -b 32|64	show only 32 or 64 bit processes
  
* 0.1.10.0dev - 2018-01-20
	* zulaessige Laege fuer Request-String 'process name' auf 255 Zeichen verlaengert
	* "|" zulaessiges Zeichen als Listentrenner fuer 'process name'
	* Feature: 'name' darf auch Liste von Prozessen sein; Bsp.: "cmd.exe|cons%.exe"
  
* 0.1.9.0dev
	* neuer Parameter -c : Suche nach (Bestandteil) kompletter Commandline

* 0.1.8.3dev
	* Behandlung nicht existierender Pfad fuer Log-File
 
* 0.1.8.2dev
	* ueberarbeiteter Abfragemodus fuer Executable
	* Debugausgaben nur fuer Debug-Compilat
	* fix: valid_params

* 0.1.8.1dev
	* Abfangen von Exceptions und Messages

* 0.1.8.0dev
	* Erweiterung Optionen um Negation der Suchanfrage fuer Prozesse und/oder User

* 0.1.7.0dev
	* Abfrage Executable zusaetzlich ueber System.Diagnostics 
 	* psdWriteLine zusaetzlich mit verkuerztem Aufruf (nur Message, keine weiteren Argumente)

======================= CHANGELOG ====================*/

using System;
using System.Management;
using System.Diagnostics;
using System.Collections.Specialized;
using CommandLine.Utility;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;
using System.IO;
	
public class PSDETAIL
{	
	
[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool IsWow64Process(
      [In] Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid hProcess,        
      [Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process
      );

[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool IsWow64Process(
	[In] IntPtr processHandle, 
	[Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process
	);
	
	
	const string VER="0.1.11.0dev";
	private static string applic = null;		// process name, executable or (part of) commandline
	private static string[] apps = null;		// list of process names
	private static bool full_exe =false;		// searching or executable?
	private static bool match_cmdline =false;   // ...or for a (part of ) the commandline?
	private static string log = null;
	private static string pid = null;
	private static string user = null;
	private static string arch = null;
	private static string neg = "";
	private static bool neg_user = false;			// negate search for processes for user
	public  static string[] valid_params = new string[] {"n", "e", "c", "p", "b", "u", "r", "k", "h", "l", "x", "y", "help", "?"};
	private static bool retPID = false;
	private static bool pKill = false;
	private static int wait_limit=2000;
	private static int errorlevel;
	private static bool isWin64 = false;
	public  static int myPID = Process.GetCurrentProcess().Id;
	const string ABOUT = "psdetail v" + VER + " for MPIMSP by Jens Boettge\n";
	
	public static void Main(string[] args)
	{
		psdWriteLine("--------------------------------------------------");
		psdWriteLine(ABOUT);
		psdWriteLine("*** Arguments: [" + string.Join("|",args)+"]");
		//foreach (string s in args) { psdWriteLine("\t'"+s+"'");}
		psdWriteLine("--------------------------------------------------");	
		
		Arguments CL=new Arguments(args);
		if (( args.Length==0)||(CL.isSet("h")||CL.isSet("help")||CL.isSet("?")))
		{	print_help();
          	Environment.Exit(-1);
   		}
		
		if (CL.isSet("l"))
		{	// modify is neccessary:
        	Regex r = new Regex(@"^[\w\s.\#\@\$%^+=\&\(\)\[\]\!\-\:\\]{1,128}$", RegexOptions.IgnoreCase);
      		Match m = r.Match(CL["l"]);
			if (m.Success) 
			{	log = CL["l"];

				FileInfo fInf = new FileInfo(log);
				DirectoryInfo	dInf = new DirectoryInfo(fInf.DirectoryName);
				if (!dInf.Exists)
				{ 	log=null;
					Console.Error.WriteLine("**E** Path [" + fInf.DirectoryName + "] for log file doess not exist; disable log");
				}
				else
				{	try 
					{ FileStream fs = File.OpenWrite(log);
					  fs.Close();
					}
					catch
					{	log=null;
						Console.Error.WriteLine("**E** can't create log file for writing ; disable log");
					}
				}
			}			
			else
			{
				print_help("Requested log file is not acceptable");
				Environment.Exit(-1);
			}
		}  
		
        if (( CL.GetNumOperands()>0 && (CL.isSet("p")||CL.isSet("n")||CL.isSet("e")||CL.isSet("c")||CL.isSet("u") )) || (CL.GetNumOperands()>1))
		{	print_help("Wrong parameter format");
          	Environment.Exit(-1);
		}	

        if (CL.IisSet("p")+ CL.IisSet("n") + CL.IisSet("e") + CL.IisSet("c") >1)
		{	print_help("Use of PID /program name / executable / commandline at the same time; use only one of this parameters");
          	Environment.Exit(-1);
		}

        if (CL.isUnary("p") || CL.isUnary("n")|| CL.isUnary("e")|| CL.isUnary("c")|| CL.isUnary("l"))
		{	print_help("Parameter value required");
          	Environment.Exit(-1);
		} 

        if (CL.isSet("p"))
		{	Regex r = new Regex(@"^\d{1,6}$", RegexOptions.IgnoreCase);
      		Match m = r.Match(CL["p"]);
			if (m.Success) pid = CL["p"];				
			else
			{
				print_help("Requested value for PID is no numeric");
				Environment.Exit(-1);
			}
		}  
        
        if (CL.isSet("b"))
		{	Regex r = new Regex(@"^\d+$", RegexOptions.IgnoreCase);
      		Match m = r.Match(CL["b"]);
			if (m.Success) arch = CL["b"];				
			else
			{
				print_help("Requested value for architecture is not numeric");
				Environment.Exit(-1);
			}
			if ((arch != "32") && (arch != "64"))
			{
				print_help("Valid value for architecture filter is either '32' or '64' ...however: '"+arch+"' is invalid!");
				Environment.Exit(-1);
			}
		}         
        
        if (CL.isSet("n"))
		{	// modify is neccessary:
        	Regex r = new Regex(@"^[\w\s.\#\@\$%^+=\&\(\)\[\]\!\-\|]{1,255}$", RegexOptions.IgnoreCase);
      		Match m = r.Match(CL["n"]);
			if (m.Success) 
			{	//applic = CL["n"];
				var pattern = @"^\|+(.*?)\|+$";
				applic = Regex.Replace(CL["n"], pattern, "$1");
				Regex rgx = new Regex(@"\|+");
				applic = rgx.Replace(applic, "↔");
				apps=applic.Split('↔');
			}
			else
			{
				print_help("Requested process name is not acceptable");
				Environment.Exit(-1);
			}
		}      

        if (CL.isSet("e"))
		{	// modify is neccessary:
        	Regex r = new Regex(@"^[\w\s.\#\@\$%^+=\&\(\)\[\]\!\-\:\\]{1,128}$", RegexOptions.IgnoreCase);
      		Match m = r.Match(CL["e"]);
			if (m.Success) 
			{	applic = CL["e"];
				full_exe = true;
			}			
			else
			{
				print_help("Requested executable is not acceptable");
				Environment.Exit(-1);
			}
		}  

        if (CL.isSet("c"))
		{	// modify is neccessary:
        	Regex r = new Regex(@"^[\w\s.\#\@\$%^+=\""\&\(\)\[\]\!\-\:\\]{1,128}$", RegexOptions.IgnoreCase);
      		Match m = r.Match(CL["c"]);
			if (m.Success)				
			{	applic = CL["c"];
				match_cmdline = true;
			}			
			else
			{
				print_help("Requested commandline string is not acceptable");
				Environment.Exit(-1);
			}
		}        

        if (CL.isSet("u"))
		{	// modify is neccessary:
        	// ToDo: domains
        	Regex r = new Regex(@"^[\w\-]{2,16}$", RegexOptions.IgnoreCase);
      		Match m = r.Match(CL["u"]);
			if (m.Success) user = CL["u"];				
			else
			{
				print_help("Requested user name is not acceptable");
				Environment.Exit(-1);
			}
		}  

		
		//psdWriteLine("**D** NumOperands: {0:D}", CL.GetNumOperands());
		if (CL.GetNumOperands()==1)
		{
			string val = CL.GetOperands()[0];
			// psdWriteLine("**D** Param: {0:S}", val);	
			Regex r = new Regex(@"^\d{1,6}$", RegexOptions.IgnoreCase);
      		Match m = r.Match(val);
			if (m.Success && !match_cmdline) 
      		{
				psdWriteLine("**D** seems to ba a process id");
				pid = val;				
			}
			else
			{
				applic = val;
			}	
		}

		
		if (CL.isSet("x") && ! CL.isUnary("x"))
		{	print_help("-x is an unary parameter");
          	Environment.Exit(-1);
		} 
		if (CL.isSet("x")) { neg = "NOT "; } 

		
		if (CL.isSet("y") && ! CL.isUnary("y"))
		{	print_help("-y is an unary parameter");
          	Environment.Exit(-1);
		} 
		if (CL.isSet("y")) { neg_user = true; } 
		
		
		if (CL.isSet("r") && ! CL.isUnary("r"))
		{	print_help("-r is an unary parameter");
          	Environment.Exit(-1);
		} 
		if (CL.isSet("r")) { retPID = true; } 

		
		if (CL.isSet("k") && ! CL.isUnary("k"))
		{	print_help("-k is an unary parameter");
          	Environment.Exit(-1);
		} 
		if (CL.isSet("k")) { pKill = true; } 
		
		
		foreach (string x in CL.Keys())	
		{ 
			if (Array.IndexOf(valid_params, x)<0) {psdWriteLine("-W- invalid parameter will be ignored: {0:S} = {1:S}", x, CL[x]);}
		}
		
		if (log != null)
		{
			string lt=DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
			psdWriteLine("[psdetail_log]\ncurrent time={0:S}\n--------------------------------------------------",lt);
		}
		#if(DEBUG)
		psdWriteLine("**D** applic      = [{0:S}]", applic);
		if (apps != null)
		{
			psdWriteLine("**D** #apps       = [{0:D}]", apps.Length);
		}
		psdWriteLine("**D** executable  = [{0:S}]", full_exe);
		psdWriteLine("**D** cmdline     = [{0:S}]", match_cmdline);
		psdWriteLine("**D** pid         = [{0:S}]", pid);
		psdWriteLine("**D** user        = [{0:S}]", user);
		psdWriteLine("**D** arch        = [{0:S}]", arch);
		if (log != null) { psdWriteLine("**D** log file   = [{0:S}]", log); }
		#endif
		getOSArchitecture();
		if (isWin64) psdWriteLine("**D** OS Architecture: 64-bit");
		else psdWriteLine("**D** OS Architecture: 32-bit ");
		if ((!isWin64) && (arch == "64"))  psdWriteLine("**W** You really expect a 64-bit application on a 32-bit OS???");
		
		//-------------------------------------------------
		ProcDetail(applic,apps,pid,arch,user,full_exe,match_cmdline);
		//-------------------------------------------------		
	}

	static void psdWriteLine(string msg)
	{	
		Console.WriteLine(msg);
		if (log != null) { File.AppendAllText(log,string.Format(msg)+"\n"); }
	}
		
	static void psdWriteLine(string msg, params object[] arg)
	{	
		Console.WriteLine(msg,arg);
		if (log != null) { File.AppendAllText(log,string.Format(msg,arg)+"\n"); }
	}
	
	static void print_help(string msg = null)
	{
		psdWriteLine(ABOUT);
		if (msg != null)
		{
			psdWriteLine("-----");
			psdWriteLine(msg);
			psdWriteLine("-----\n");
		}
		psdWriteLine("Usage:");
		psdWriteLine("\tpsdetail.exe name|pid");
		psdWriteLine("\tpsdetail.exe [-h] [-n name | -e executable | -c commandline(part)| -p pid] [-u user] [-b 32|64]\n");			
		psdWriteLine("\t-h\t\tprint this help");	
		psdWriteLine("\t-n name\t\tretrieve details for process with this name");
		psdWriteLine("\t\t\t(use double quotes for strings with more process names,");
		psdWriteLine("\t\t\tseparate them by pipe symbol \"|\")");
		psdWriteLine("\t-e executable\tretrieve details for process with this executable (full path)");
		psdWriteLine("\t\t\t(use double quotes for strings with spaces)");								
		psdWriteLine("\t-c commandline\tretrieve details for process which commandline matches the argument");
		psdWriteLine("\t\t\t(use double quotes for strings with spaces)");								
		psdWriteLine("\t-p pid\t\tretrieve details for process with this process id");
		psdWriteLine("\t-b 32|64\tshow only 32 or 64 bit processes");
		psdWriteLine("\t-u user\t\tprocesses only for this user (without domain)");
		psdWriteLine("\t-x\t\tnegate search request (except for user)");
		psdWriteLine("\t-y\t\tnegate search for user");
		psdWriteLine("\t-r\t\treturn code is pid; 0 if no process found, ");
		psdWriteLine("\t  \t\tif negative, number of processes if more than one process found");
		psdWriteLine("\t-k\t\ttry to kill the found process(es) (no effect on return code)");
		psdWriteLine("\t-l log\t\twrite output of psdetail to log file");
		psdWriteLine("\n\treturn values (except of -r ): number of processes found; negative on errors and help\n");
	}
	
	static void getOSArchitecture()
	{
		try
		{	string ComputerName = "localhost";
			ManagementScope Scope;  			
			if (!ComputerName.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
            	ConnectionOptions Conn = new ConnectionOptions();
                Conn.Username  = "";
                Conn.Password  = "";
                Conn.Authority = "ntlmdomain:DOMAIN";
                Scope = new ManagementScope(String.Format("\\\\{0}\\root\\CIMV2", ComputerName), Conn);
            }
            else Scope = new ManagementScope(String.Format("\\\\{0}\\root\\CIMV2", ComputerName), null);

           	/*
  			ManagementPath 	mgmt = new ManagementPath("Win32_OperatingSystem");
            ManagementClass osClass = new ManagementClass(Scope,mgmt,null);
            foreach (ManagementObject queryObj in osClass.GetInstances())
            {   try 
            	{
            		// psdWriteLine("**D** OSArchitecture: {0}", queryObj["OSArchitecture"]);
            		if (Convert.ToString(queryObj["OSArchitecture"])=="64-bit") isWin64=true;
            		else isWin64=false;
            	}
            	catch
            	{
            		isWin64=false;
            	}
            	return;
            	//foreach (PropertyData prop in queryObj.Properties)
                //{
            	//	psdWriteLine("{0}: {1}", prop.Name, prop.Value);
                //}                    
            }
            */
           
            ObjectQuery Query = new ObjectQuery("SELECT OSArchitecture FROM Win32_OperatingSystem");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(Scope,Query); 
            string arc=null;
            try {
            	foreach (ManagementObject queryObj in searcher.Get())
            	{             arc = Convert.ToString(queryObj["OSArchitecture"]);
            				  //psdWriteLine("**D** OS Architecture: {0}", arc);
            				  if (Convert.ToString(queryObj["OSArchitecture"])=="64-bit") isWin64=true;
            				  else isWin64=false;
            				  return;
            	}
            }
            catch (ManagementException e)
            {
                psdWriteLine("**E** an error occurred while querying for WMI data for architecture: " + e.Message);
                isWin64=false;
            }
            
		}
        catch (Exception e)
        {
        	psdWriteLine(String.Format("Exception:\n---------------\n{0}\nTrace:\n---------------\n{1}",e.Message,e.StackTrace));
        	Environment.Exit(-3);
        }		
	}

	static void ProcDetail(string p_name="", String[] apps=null, string p_pid="", string p_arch="", string p_user="", bool p_full=false, bool p_cmd=false)
    {	if (string.IsNullOrEmpty(p_name) && string.IsNullOrEmpty(p_pid)&& string.IsNullOrEmpty(p_user))
    	{ 	print_help("no parameters given to function ProcDetail"); 
    		Environment.Exit(-10);
    	}
          	
		try
		{	int cnt=0;
			int lastPID=0;
			int wait=0;
			Process prc,p;
			int sdPID;
			string exe=null;
			bool bExe;
			string sMatch;
			string sFull="";
			string sCmd="";
			string sMemW;
			string sMemV;
			string sProcessHandle="";
            string w_search="";
            string query_names="";			
			
        	string ComputerName = "localhost";
			ManagementScope Scope;  			
			if (!ComputerName.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
            	ConnectionOptions Conn = new ConnectionOptions();
                Conn.Username  = "";
                Conn.Password  = "";
                Conn.Authority = "ntlmdomain:DOMAIN";
                Scope = new ManagementScope(String.Format("\\\\{0}\\root\\CIMV2", ComputerName), Conn);
            }
            else Scope = new ManagementScope(String.Format("\\\\{0}\\root\\CIMV2", ComputerName), null);
            Scope.Connect();
            
            // -------------------
            // Get process details
            // -------------------
            if (! string.IsNullOrEmpty(p_pid))
            { 
            	w_search="SELECT * FROM Win32_Process Where " + neg + "ProcessId='" + p_pid + "'";
            }
            else if (! string.IsNullOrEmpty(p_name))
            {
            	//w_search="SELECT * FROM Win32_Process Where Name like '" + p_name + "%'";
            	if (p_full)
            	{   //Regex r = new Regex(@"\\", RegexOptions.None);
      				//string s = r.Replace(p_name,"\\\\");
      				//w_search="SELECT * FROM Win32_Process Where " + neg + "ExecutablePath like '" + s + "'";
      				
      				// ...does not return all matching processes if calling user is not the administrator
      				// workaround:
      				w_search="SELECT * FROM Win32_Process";
      				// ...and search later for matching executable
      				// translate WQL to regular expression:
      				sFull=Regex.Replace(p_name, @"\\", "\\\\");
					sFull=Regex.Replace(sFull, @"\.", "\\.");
					sFull=Regex.Replace(sFull, @"\[", "\\[");
					sFull=Regex.Replace(sFull, @"\]", "\\]");
					sFull=Regex.Replace(sFull, @"\(", "\\(");
					sFull=Regex.Replace(sFull, @"\)", "\\)");
					sFull=Regex.Replace(sFull, @"\$", "\\$");
					sFull=Regex.Replace(sFull, @"%", ".*");
					#if(DEBUG)
					Console.WriteLine("***D*** Regular expression for executable matching: [" + sFull + "]");
					#endif
            	}
            	else if (p_cmd)
            	{ 	// prepare seasrch string for WMI query: 
            		sCmd=Regex.Replace(p_name, @"\\", "\\\\");
					w_search="SELECT * FROM Win32_Process Where " + neg + "CommandLine like '%" + sCmd + "%' AND NOT ProcessId = '" + myPID + "'";
					#if(DEBUG)
					Console.WriteLine("***D*** Searching for command line (regexed): [" + sCmd + "]");
					#endif
            	}
            	else
            	{
            		// ...search for process names:
            		if (apps==null)
            		{
            			query_names = "Name like '" + p_name + "'";
            		}
            		else
            		{
            			query_names = "Name like '" + apps[apps.Length-1] + "'";	
            			if (apps.Length > 1) { for (int i=apps.Length-2 ; i==0; i--) { query_names = "Name like'" + apps[i] + "' OR " + query_names;}}
            			
            		}
            		w_search="SELECT * FROM Win32_Process Where " + neg + "(" + query_names +")";
            		#if(DEBUG)
					Console.WriteLine("***D*** Searching for process(es): [" + p_name + "]");
					#endif
            	}
            }
            else
            {
            	w_search="SELECT * FROM Win32_Process Where Name like '%'";
            }
            #if(DEBUG)
			Console.WriteLine("***D*** Query: [" + w_search + "]");
			#endif	
            ObjectQuery Query = new ObjectQuery(w_search);
            ManagementObjectSearcher Searcher = new ManagementObjectSearcher(Scope, Query);
			
			foreach (ManagementObject WmiObject in Searcher.Get())
			{
				if ((UInt32)WmiObject["ProcessId"]==0) continue;
				
				// ------------------------
				// Get executable with System.Diagnostics
				// ------------------------
				exe=null;
				if (WmiObject.GetPropertyValue("ExecutablePath") == null)
				{	sdPID = (Int32)(UInt32)WmiObject["ProcessId"];
					bExe=false;
				 	try
					{	if (ComputerName != "localhost") {p = Process.GetProcessById(sdPID,ComputerName);}
						else {p = Process.GetProcessById(sdPID);}
						exe=p.MainModule.FileName;
						if (exe == "") exe="{SYSTEM}";
					}
					catch(System.ComponentModel.Win32Exception ex)
					{	//sytemprocess without executable
						//exe="{{system}}";
						exe=string.Format("{{{{{0:S} \b}}}}", ex.Message);
					}
				}
				else 
				{
					exe=WmiObject["ExecutablePath"].ToString();
					bExe=true;
				}
				
				if (p_full)
				{ 						
					// ignore exception strings "{{ ... }}":
					if (Regex.Match(exe,@"^{{.*}}$",RegexOptions.IgnoreCase).ToString()!="") continue;
					
					sMatch=Regex.Match(exe,sFull,RegexOptions.IgnoreCase).ToString();
					#if(DEBUG)					
					Console.WriteLine("***D*** Regex.Match: [" + sMatch + "] for [" + exe + "]");
					#endif
					if (sMatch=="" && neg=="" ) continue;
					else if (sMatch!="" && neg!="" ) continue;
				}
				
				
				// -------------------------
				// Get process owner and SID
				// -------------------------
				ManagementBaseObject owner = WmiObject.GetMethodParameters("GetOwner");
				owner = WmiObject.InvokeMethod("GetOwner",owner, null);
				//psdWriteLine(owner.GetText(TextFormat.Mof));
				
				ManagementBaseObject ownerSid = WmiObject.GetMethodParameters("GetOwner");
				ownerSid = WmiObject.InvokeMethod("GetOwnerSid",ownerSid, null);
				//psdWriteLine(ownerSid.GetText(TextFormat.Mof));					
				
				if (p_user!=null)
				{ 	
				  	//psdWriteLine("**D**** Owner: ["+(string)owner["User"]+"]");					
					if ( ! neg_user && p_user != (string)owner["User"]) continue;
					if (   neg_user && p_user == (string)owner["User"]) continue;
				}
				
				
				// -------------------------
				// filter Architecture
				// -------------------------
				lastPID=Convert.ToInt32(WmiObject["ProcessId"]);
				bool isWow64 = false;
 				if ((System.Environment.OSVersion.Version.Major == 5 && System.Environment.OSVersion.Version.Minor >= 1) ||
       				System.Environment.OSVersion.Version.Major > 5)
 				{	try
					{ 
						//SafeProcessHandle processHandle = GetProcessHandle((uint)System.Diagnostics.Process.GetCurrentProcess().Id);
	      				//SafeProcessHandle processHandle = Process.GetProcessById((int)WmiObject["ProcessId"]);
	      				Process processHandle = Process.GetProcessById(Convert.ToInt32(WmiObject["ProcessId"]));
      					// sProcessHandle = String.Format("\tProcess Handle : " + processHandle.Handle);
      					sProcessHandle = String.Format("{0:S}" , processHandle.Handle);
					
	      				bool retVal;
	      				if (!IsWow64Process(processHandle.Handle, out retVal))
	      				{
	      					throw new Win32Exception(Marshal.GetLastWin32Error());
	      				}
	      				isWow64 = retVal;
					}					
					catch(Exception ex)
					{ 	//sProcessHandle = String.Format("\tProcess Handle : {{{{{0:S} \b}}}}", ex.Message);
						sProcessHandle = String.Format("{{{{{0:S} \b}}}}", ex.Message);
					}
 				}
				if (!string.IsNullOrEmpty(p_arch))
				{
					switch (p_arch)
				      {
				          case "32":
							if (isWin64 && (!isWow64)) continue;
							break;
				          case "64":
				          	if ((isWow64) || (!isWin64)) continue;
				            break;
				          default:
				            break;
				      }
					//if (isWow64) psdWriteLine("\tArchitecture   : W32 on W64");
					//else if (isWin64) psdWriteLine("\tArchitecture   : native 64 bit");
					//else psdWriteLine("\tArchitecture   : native 32 bit");				
				}
				
				
				cnt++;				
				// ------------------------
				// Display process details
				// ------------------------
				//psdWriteLine("Details for '" + p_name + "' (PID="+p_pid+")");
				psdWriteLine("--------------------------------------------------");
				if (! string.IsNullOrEmpty(p_pid)) psdWriteLine("Details for PID "+p_pid);
				#if(DEBUG)
            	else if (! string.IsNullOrEmpty(p_name)) psdWriteLine("Details for '" + p_name + "'");
            	#endif
            	else psdWriteLine("#{0:D}:", cnt);
				psdWriteLine("\tPID            : " + WmiObject["ProcessId"]);
				psdWriteLine("\tHandle         : " + WmiObject["Handle"]);
				if (bExe) psdWriteLine("\tExecutable     : " + WmiObject["ExecutablePath"]);
				else if (exe != null) { psdWriteLine("\tExecutable*    : " + exe); }
				if (WmiObject.GetPropertyValue("CommandLine") != null)
				{	psdWriteLine("\tCommandLine    : " + WmiObject["CommandLine"]);
				}
				psdWriteLine("\tName           : " + WmiObject["Name"]);
				psdWriteLine("\tThreadCount    : " + WmiObject["ThreadCount"]);
				psdWriteLine("\tHandleCount    : " + WmiObject["HandleCount"]);
				psdWriteLine("\tCreation date  : " + WmiObject["CreationDate"]);
				psdWriteLine("\tUser mode time : " + WmiObject["UserModeTime"]);
				sMemW=string.Format("{0,12:#,#}", WmiObject["WorkingSetSize"]);
				sMemV=string.Format("{0,12:#,#}", WmiObject["VirtualSize"]);
				psdWriteLine("\tWorking size   : " + sMemW);
				psdWriteLine("\tVirtual size   : " + sMemV);
				
				if (exe == "") { psdWriteLine("\tSystem process"); }

				psdWriteLine("\tProcess Handle : " + sProcessHandle);
				if (isWow64) psdWriteLine("\tArchitecture   : W32 on W64");
				else if (isWin64) psdWriteLine("\tArchitecture   : native 64 bit");
				else psdWriteLine("\tArchitecture   : native 32 bit");
				
				//if(p_user!=null) continue;
				if (p_user==null || neg_user)
				{
					// -----------------------------
					// Display owner name & domain
					// -----------------------------
					psdWriteLine("\tDomain         : " + owner["Domain"]);
					psdWriteLine("\tUser           : " + owner["User"]);
					// -----------------------------
					// Display owner SID
					// -----------------------------				
					if ( Convert.ToInt32(ownerSid["ReturnValue"])== 0 )	{ psdWriteLine("\tSid            : " + ownerSid["Sid"]); }
					else { psdWriteLine("Error " + ownerSid["ReturnValue"]+ " while obtaining Sid"); }

				}
				
				if (pKill)
				{	if (ComputerName.Equals("localhost", StringComparison.OrdinalIgnoreCase))
						prc=Process.GetProcessById(lastPID);
					else
						prc=Process.GetProcessById(lastPID,ComputerName);
					try 
					{
						wait=0;
						prc.Kill();
						if (! prc.HasExited)
						{ 
								Console.Write("\tWaiting for process kill ..");
								while(!prc.HasExited && wait<wait_limit)
								{
									prc.WaitForExit(100);
									wait_limit+=100;
									Console.Write(".");
								}
								psdWriteLine("");
									
						}
						if (prc.HasExited) psdWriteLine("-----> Process killed");
						else psdWriteLine("--W--> Process still running!");
					}
					catch(Exception kE)
					{	switch (kE.Message)
						{
							case "Access is denied":
								psdWriteLine("--E--> Kill process - Access denied");
								break;
							default:
								psdWriteLine(String.Format("Exception:\n---------------\n{0}",kE.Message));
								psdWriteLine(String.Format("Base:\n---------------\n{0}",kE.GetBaseException()));
								psdWriteLine(String.Format("Target:\n---------------\n{0}\n",kE.TargetSite));
								psdWriteLine(String.Format("Source:\n---------------\n{0}\n",kE.Source));
								psdWriteLine(String.Format("Hash:\n---------------\n{0} ({0:X})\n",kE.GetHashCode()));
								break;
						}
					}
				}
            }
			psdWriteLine("==================================================");
			psdWriteLine("Processes found: {0:D}", cnt);
			psdWriteLine("==================================================");
			if (retPID)
			{	if (cnt==1) errorlevel=lastPID;
				else if (cnt==0) errorlevel=0;
				else errorlevel=-cnt;
			}
			else errorlevel=cnt;
			if (log == null) psdWriteLine("Returning with errorlevel {0:D}\n",errorlevel);
			else psdWriteLine("\n[exit]\nerrorlevel={0:D}\n\n",errorlevel);
			Environment.Exit(errorlevel);
		}
        catch (Exception e)
        {
        	psdWriteLine(String.Format("Exception:\n---------------\n{0}\nTrace:\n---------------\n{1}",e.Message,e.StackTrace));
        	Environment.Exit(-3);
        }
         
        //psdWriteLine("Press Enter to exit");
        //Console.Read();
    }
    
    public string GetProcessOwner(int processId)
	{
    	string query = "Select * From Win32_Process Where ProcessID = " + processId;
    	ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
    	ManagementObjectCollection processList = searcher.Get();

    	foreach (ManagementObject obj in processList)
    	{
    	    string[] argList = new string[] { string.Empty, string.Empty };
    	    int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
    	    if (returnVal == 0)
    	    {
    	        // return DOMAIN\user
    	        return argList[1] + "\\" + argList[0];
    	    }
   	 	}  
     return "NO OWNER";
	}
    
    
    public string GetProcessOwner(string processName)
	{
    	string query = "Select * from Win32_Process Where Name = \"" + processName + "\"";
    	ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
    	ManagementObjectCollection processList = searcher.Get();

    	foreach (ManagementObject obj in processList)
    	{
    	    string[] argList = new string[] { string.Empty, string.Empty };
    	    int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
    	    if (returnVal == 0)
    	    {
    	        // return DOMAIN\user
    	        string owner = argList[1] + "\\" + argList[0];
    	        return owner;       
    	    }
    	}
    	return "NO OWNER";
	}
}
