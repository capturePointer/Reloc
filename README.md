# Reloc
A client tool that interfaces with a server we host (Thanks @IOActive) with over 200000 fragments of relocation data
that is compiled from various PE files.  This ensures when extracting data from memory dumps that you can match memory to 
disk files precisely. I've targeted [@dotnet/coreclr](https://github.com/dotnet/coreclr) and [@dotnet/wcf](https://github.com/dotnet/wcf) under the hood.

## CORECLR
This code target's coreclr to maximize portability.  Most development has 
taken place on Windows so there is a little bit of test that needs to be
done for Linux, OSX, FreeBSD, etc...  I'll likely be implementing some
workarounds for the WCF SOAP API calls to ensure an alternative mechanism
is in place to contact the server.

## TODO
I'm done with this code for a while. I attached an example report from 
WinMerge from a binary dumped with Volatility then block hashed to 512 bytes
sizes with Tiger 192 (the same as BlockWatch currently uses :).

### Extract relocations from .pdb's as an alternative for MS files.

As you can see Reloc enabled the dumped binary to match almost exactly.

Compare the difference [without using Reloc](without-Reloc.htm) and [using Reloc](with-Reloc.htm).

### Quick note to dumper writers
If your using Reloc make sure you validate precisely the sections so your not accidentally
missing code due to alignment/code caves.


~~Next will be a command for delocating a dumped binary.  That is, an image
extracted from memory to disk (one-to-one) delocation so that any position
dependent instructions/references are fixed (delocated) to their original
values.  Null pages are accounted for since we can not depend on a runtime
page fault to cause all of a given binary to load.  This does add a bit of 
complexity to the DeLocate routine.  It's currently implemented unsafe since
ported from C, will be moving to safe soon.~~

Delocation code is in place however is only exposed to API callers not CLI.

Program.cs has a set of upcoming features, feel free to contact or use 
github to give us some requests.  If nobody else does it I'll try to figure
out some python to integrate into @volatility.

## Examples
.NET coreclr restore/run like so;
```
dnu restore
dnx [command] [args]
dnx commands
Error: Unable to load application or execute command 'commands'. Available commands: Reloc, Extract.
```
Not all of our routines are optimized yet, it seems like the async IO in coreclr
is doing really well, as you can see below, pretty
respectable performance for non-native code _nearly 3500 read/writes in less
than 13 seconds_. 

### Example: **dnx run Extract c:\windows\system32 d:\temp\test**
```
X:\Reloc\src\Reloc>dnu restore
Microsoft .NET Development Utility CoreClr-x64-1.0.0-rc2-16249
...
Writing lock file X:\Reloc\src\Reloc\project.lock.json
Restore complete, 2714ms elapsed

X:\Reloc\src\Reloc>dnx run Extract c:\windows\system32 d:\temp\test
Scanning folder c:\windows\system32 and saving relocs into d:\temp\test.
...
extracted relocs into d:\temp\test\ztrace_maps.dll-180000000-564D22CC.reloc size 512
processing time: 00:00:12.9261101
Compiled 3493 new .reloc data fragments
```

### Example: DeLocate
This command string is a little out of control.  I'll add some switches to make it a bit
easier or something ;).   Specifying "dnvm exec default dnx " is an alternative way to get
the runtime version your looking for.

Delocate will attempt to automatically rebuild the complete executable on disk from memory.  The only differences
should be;
   1) Data section, if anybody has idea how to reverse the data, that will be interesting.
   2) .reloc missing, it dosent really matter since you download or captured .reloc locally in the first place, you can 
      cat >> the reloc to the end of your new binary if you would like.
   3) There may be some artifacts regarding the resources depending on the application specifics. I havent looked at resource handling in a minute.

```
dnvm exec default dnx DeLocate d:dumped.msctf.dll d:msctf.dll-10000000-564D1E7B.reloc 77740000 d:delocated.msctf.dll False
```

### Example: **dnx Reloc True ntdll 51DA4B7D**
After you clone into a directory

```
c:\git>git clone https://github.com/ShaneK2/Reloc.git
Cloning into 'Reloc'...
remote: Counting objects: 18, done.
remote: Compressing objects: 100% (16/16), done.
remote: Total 18 (delta 2), reused 13 (delta 1), pack-reused 0
Unpacking objects: 100% (18/18), done.
Checking connectivity... done.

c:\git>cd Reloc\src\Reloc

c:\git\Reloc\src\Reloc>dnu restore

Microsoft .NET Development Utility CoreClr-x64-1.0.0-rc2-16237

  CACHE https://www.nuget.org/api/v2/
  CACHE https://ci.appveyor.com/nuget/gemini-g84phgw340sm/
  CACHE https://www.myget.org/F/aspnetvnext/api/v2/
  CACHE https://www.myget.org/F/dotnet-core/api/v3/index.json
Restoring packages for C:\Temp\testing\Reloc\src\Reloc\project.json
  CACHE https://www.nuget.org/api/v2/FindPackagesById()?id='System.Reflection.TypeExtensions'
  CACHE https://ci.appveyor.com/nuget/gemini-g84phgw340sm/FindPackagesById()?id='System.Reflection.TypeExtensions'
  CACHE https://www.myget.org/F/aspnetvnext/api/v2/FindPackagesById()?id='System.Reflection.TypeExtensions'
  CACHE https://www.myget.org/F/dotnet-core/api/v3/flatcontainer/system.reflection.typeextensions/index.json
Writing lock file C:\Temp\testing\Reloc\src\Reloc\project.lock.json
Restore complete, 973ms elapsed

c:\git\Reloc\src\Reloc>dnx Reloc True ntdll 51DA4B7D
Contacting, dest file [ntdll-?####?-51DA4B7D.reloc.7z]: 64bit:True, Region(dll):ntdll, TimeDateStamp:51DA4B7D.
Downloaded to NTDLL.DLL-78E50000-51DA4B7D.reloc.7z, size 905.

c:\git\Reloc\src\Reloc>dir NTDLL.DLL-78E50000-51DA4B7D.reloc.7z
 
 Directory of \git\Reloc\src\Reloc

11/27/2015  02:52 PM               905 NTDLL.DLL-78E50000-51DA4B7D.reloc.7z
```

Simply extract and use the data to establish high assurances in your forensics
process.

### Bugs
  * Fixe'dm all ;)  
  * ~~For some reason when the last word is written out (the buffers are being delocated) they do not appear back on disk. 
    Currently I modify the delocation buffer in-place so in the event your not using disk files and call this method
	w/o hitting the disk we don't need to waste too much, it's probably a worthless micro-opt anyhow.~~
