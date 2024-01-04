# psDetail
-----

**`psDetail`** ist ein Konsolenprogramm im Stile der Werkzeuge der
[Sysinternals-Suite](https://docs.microsoft.com/en-us/sysinternals/).

Das Programm liefert Details zu angefragten Prozessen und erlaubt es diese
optional zu beenden.


## Features

Die Filter für die Anfragen sind flexibel gehalten. Es kann gesucht werden
nach:
* Prozessnamen; auch mehrere gleichzeitig (siehe Hilfe)
* Pfad laufender Programme; auch mit Wildcard ("`%`")
* Fragmenten des Aufrufkommandos (z.B.: übergebenen Parametern); auch mit Wildcard
* Prozess-ID
* User

Weiterhin:
* Suchanfragen können negiert werden.
* der Exit-Code liefert regulär die Anzahl der gefundenen Prozessen (zwecks einfacher
  Weiterverarbeitung des Ergebnisses); im Fehlerfall ist der Code negativ
* der Exit-Code kann optional die Prozess-ID für *einen* gefundenen Prozess liefern;
  werden mehrere gefunden ist der Code neagtiv und liefert die Anzahl der Prozesse
* es kann ein Logfile geschrieben werden
* gefundene Prozesse können von `psdetail` direkt beendet werden


## Voraussetzungen

`psdetail.exe` benötigt das .NET-Framework in *Version 3.5*.  
Bei `psdetail4.exe` handelt es sich um die Version für das Framework in der *Version 4.x*.


## Syntax
```
Usage:
    psdetail.exe name|pid
    psdetail.exe [-h] [-n name | -e executable | -c commandline(part)| -p pid] [-u user]
    -h              print this help
    -n name         retrieve details for process with this name
                    (use double quotes for strings with more process names,
                    separate them by pipe symbol \"|\")
    -e executable   retrieve details for process with this executable (full path)
                    (use double quotes for strings with spaces)
    -c commandline  retrieve details for process which commandline matches the argument
                    (use double quotes for strings with spaces)
    -p pid          retrieve details for process with this process id
    -u user         processes only for this user (without domain)
    -x              negate search request (except for user)
    -y              negate search for user
    -r              return code is pid; 0 if no process found,
                    if negative, number of processes if more than one process found
    -k              try to kill the found process(es) (no effect on return code)
    -l log          write output of psdetail to log file

    return values (except of -r ): number of processes found; negative on errors and help
```

-----
Jens Böttge <<boettge@mpi-halle.mpg.de>>, 20.01.2018 - 04.01.2024
