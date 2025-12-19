![Alt text](Untitled.png "Optional title")

SET MSSdk=E:\WinDDK\6000\ or the Root Directory in SetEnv.bat

Read the Useage and execute the batch file.

<pre>:: --------------------------------------------------------------------------------------------
:: File    : SetEnv.cmd
::
:: Abstract: This batch file sets the appropriate environment
::           variables for the Windows SDK build environment with
::           respect to OS and platform type.
::
:: "Usage Setenv [/Debug | /Release][/x86 | /x64 | /ia64][/vista | /xp | /2003 ][-h] "
::
::                /Debug   - Create a Debug configuration build environment
::                /Release - Create a Release configuration build environment
::                /x86     - Create 32-bit x86 applications
::                /x64     - Create 64-bit x64 applications
::                /ia64    - Create 64-bit ia64 applications
::                /vista   - Windows Vista applications
::                /xp      - Create Windows XP SP2 applications
::                /2003    - Create Windows Server 2003 applications
::
:: --------------------------------------------------------------------------------------------
</pre>

To Build the Buffer Lab or Samples use NMAKE or MSBUILD from Microsoft or AT&T. MSBUILD may throw a 'cmd.exe' spawning error this can be ignored.


You will need to build sdkdiif or windiff first and compare NTDDSCSI.H

Extract the AllSamples.zip which are the Longhorn Samples And rename them OS->NT The shipping .NET Framework for Vista was 3.0. Here we use 4.5.1 Source. You can make a Vista 32-Bit Virtual Machine if you have the Vista RTM DVD or ISO and Product Key In Vista the SDKTOOLS Directory I probably was renamed Utilities taken form the last Singularity Codeplex changeset I've provided or now Midori. 

The WindowsAPICodePack namespace can be refactored to "Windows' I know they are samples but its a good starting point for you before I pay for the Enterprise Source Licensing program if I can find it. It won't build to begin with and the WDK is 2GB to large for GitHub it is in your Visual Studio Subscription.

- Make a Vista Virtual Machine
- Install the Vista SDK & WDK
- Install the Media Center SDK
- Install ActivePerl 5.28.1 for Vista
