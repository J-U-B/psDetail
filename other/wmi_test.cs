using System;
using System.Management;
using System.Collections;

public class WMI_Test 
{
	private static string applic = "calc.exe";
	
	public static void Main() 
	{
		// Invocations();
		ProcOwner();
			
	}
	
    static void Invocations() 
    {
    //Get the object on which the method will be invoked
    ManagementClass processClass = new ManagementClass("Win32_Process");
	
    
    // Option 1: Invocation using parameter objects
    //================================================
    //Get an input parameters object for this method
    ManagementBaseObject inParams = processClass.GetMethodParameters("Create");
    //Fill in input parameter values
    inParams["CommandLine"] = applic;
    //Execute the method
    ManagementBaseObject outParams = processClass.InvokeMethod("Create", inParams, null);
    //Display results
    //Note: The return code of the method is provided
    // in the "returnValue" property of the outParams object
    Console.WriteLine("Creation of calculator " + "process returned: " + outParams["returnValue"]);
    Console.WriteLine("Process ID: " + outParams["processId"]);
	
	
	
    // Option 2: Invocation using args array
    //=======================================
    //Create an array containing all arguments for the method
    object[] methodArgs = {applic, null, null, 0};
    //Execute the method
    object result = processClass.InvokeMethod ("Create", methodArgs);
    //Display results
    Console.WriteLine ("Creation of process returned: " + result);
    Console.WriteLine ("Process id: " + methodArgs[3]);
	}

    static void ProcOwner()
    {
    	// Create a process using parameter objects
    	ManagementClass processClass = new ManagementClass("Win32_Process");
    	ManagementBaseObject param_create = processClass.GetMethodParameters("Create");
    	param_create["CommandLine"]= applic +" '42*42'";
    	param_create["CurrentDirectory"]="C:\\TEMP\\";
    	ManagementBaseObject create_ret = processClass.InvokeMethod("Create",param_create, null);
		Console.WriteLine("Creation of calculator " + "process returned: " + create_ret["returnValue"]);
		Console.WriteLine("Process ID  : " + create_ret["ProcessId"]);
		Console.WriteLine("\n");
		// Console.WriteLine(param_create.GetText(TextFormat.Mof));
		// Console.WriteLine(create_ret.GetText(TextFormat.Mof));
		
		System.Threading.Thread.Sleep(200);

		// Retrieve data:
		try
		{
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
            //ObjectQuery Query = new ObjectQuery("SELECT Handle FROM Win32_Process Where Name='" + applic + "'");
            //ObjectQuery Query = new ObjectQuery("SELECT Handle FROM Win32_Process Where ProcessId="+create_ret["ProcessId"]);
            ObjectQuery Query = new ObjectQuery("SELECT * FROM Win32_Process Where Name='" + applic + "'");
            ManagementObjectSearcher Searcher = new ManagementObjectSearcher(Scope, Query);
			
			foreach (ManagementObject WmiObject in Searcher.Get())
			{
				// -----------------
				// Get process owner
				// -----------------
				Console.WriteLine("Details for '" + applic + "'");
				Console.WriteLine("\tPID            : " + WmiObject["ProcessId"]);
				Console.WriteLine("\tExecutable     : " + WmiObject["ExecutablePath"]);
				Console.WriteLine("\tCommandLine    : " + WmiObject["CommandLine"]);
				Console.WriteLine("\tName           : " + WmiObject["Name"]);
				Console.WriteLine("\tThreadCount    : " + WmiObject["ThreadCount"]);
				Console.WriteLine("\tHandleCount    : " + WmiObject["HandleCount"]);
				Console.WriteLine("\tCreation date  : " + WmiObject["CreationDate"]);
				Console.WriteLine("\tUser mode time : " + WmiObject["UserModeTime"]);
				
				
				ManagementBaseObject owner = WmiObject.GetMethodParameters("GetOwner");
				owner = WmiObject.InvokeMethod("GetOwner",owner, null);
				//Console.WriteLine(owner.GetText(TextFormat.Mof));
				Console.WriteLine("\tDomain         : " + owner["Domain"]);
				Console.WriteLine("\tUser           : " + owner["User"]);
				
				ManagementBaseObject ownerSid = WmiObject.GetMethodParameters("GetOwner");
				ownerSid = WmiObject.InvokeMethod("GetOwnerSid",ownerSid, null);
				//Console.WriteLine(ownerSid.GetText(TextFormat.Mof));	
				if ( Convert.ToInt32(ownerSid["ReturnValue"])== 0 )	{ Console.WriteLine("\tSid            : " + ownerSid["Sid"]); }
				else { Console.WriteLine("Error " + ownerSid["ReturnValue"]+ " while obtaining Sid"); }
			
			    // ---------------------           
            	// Terminate the process
	            // ---------------------
               	object kill_ret = WmiObject.InvokeMethod("Terminate", null);
            	Console.WriteLine("\tTermination of process returned: " + kill_ret+"\n");
            }
		}
        catch (Exception e)
        {
        	Console.WriteLine(String.Format("Exception {0} Trace {1}",e.Message,e.StackTrace));
        }
         
        //Console.WriteLine("Press Enter to exit");
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
