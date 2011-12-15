using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: AssemblyCompany("nJupiter")]
[assembly: AssemblyProduct("nJupiter")]
[assembly: AssemblyCopyright("\x00a9 2005-2011 nJupiter. Licensed under the MIT license: http://www.opensource.org/licenses/mit-license.php")]
[assembly: AssemblyTrademark("\x00a9 2005-2011 nJupiter. Licensed under the MIT license: http://www.opensource.org/licenses/mit-license.php")]
[assembly: AssemblyCulture("")]

[assembly: SecurityTransparent]

#if DEBUG
[assembly: AssemblyVersion("4.0.1.*")]
[assembly: AssemblyConfiguration("Debug")]
[assembly: InternalsVisibleTo("nJupiter.Tests.UnitTests")]
[assembly: InternalsVisibleTo("nJupiter.Tests.IntegrationTests")]
#else
// Increment AssemblyVersion only on major and minor releases
[assembly: AssemblyVersion("4.0.1.0")]
// Increment AssemblyFileVersion on every release
[assembly: AssemblyFileVersion("4.0.1.0")]
[assembly: AssemblyConfiguration("Release")]
#if SIGN
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyFile(@"C:\Projects\nJupiter\Development\nJupiter.snk")]
[assembly: AssemblyKeyName("")]
#endif
#endif
[assembly: PermissionSet(SecurityAction.RequestMinimum, Name="Nothing")]

[assembly: AssemblyTitle("nJupiter "

#if SIGN
+ "Signed "
#endif

#if DEBUG
+ "Debug"
#else
+ "Release"
#endif

+ " Build"

)]