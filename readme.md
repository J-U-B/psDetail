#psDetail#
==========

**<code>psDetail</code>** ist ein Konsolenprogramm im Stile der Werkzeuge
der [Sysinternals-Suite](https://docs.microsoft.com/en-us/sysinternals/).  
Es liefert Details zu angefragten Prozessen und erlaubt es diese auch gleich
zu beenden.

Es wird das .NET-Framework (mind. Version 3.5) benötigt.

##Syntax##
<pre>
Usage:
	psdetail.exe name|pid
	psdetail.exe [-h] [-n name | -e executable | -c commandline(part)| -p pid] [-u user]
	-h				print this help
	-n name			retrieve details for process with this name
					(use double quotes for strings with more process names,
					separate them by pipe symbol \"|\")
	-e executable	retrieve details for process with this executable (full path)
					(use double quotes for strings with spaces)
	-c commandline	retrieve details for process which commandline matches the argument
					(use double quotes for strings with spaces)
	-p pid			retrieve details for process with this process id
	-u user			processes only for this user (without domain)
	-x				negate search request (except for user)
	-y				negate search for user
	-r				return code is pid; 0 if no process found,
					if negative, number of processes if more than one process found
	-k				try to kill the found process(es) (no effect on return code)
	-l log			write output of psdetail to log file

	return values (except of -r ): number of processes found; negative on errors and help
</pre>

-----
Jens Boettge  
20.01.2018
