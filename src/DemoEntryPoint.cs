using Orders.Api;
using System.Reflection;

var demoName = Assembly.GetExecutingAssembly().GetName().Name ?? "Orders.Demo";
var database = Path.GetFullPath(Path.Combine(".runtime", demoName, "orders.db"));
Directory.CreateDirectory(Path.GetDirectoryName(database)!);
string[] defaults = ["--Orders:Mode", "fixed", "--Orders:DatabasePath", database];
Console.WriteLine($"{demoName}: synthetic local demo. Defaults are fixed; explicit CLI arguments take precedence.");
await OrdersApplication.Build([.. defaults, .. args]).RunAsync();
