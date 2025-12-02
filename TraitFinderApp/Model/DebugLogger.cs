namespace TraitFinderApp.Model
{
	public static class DebugLogger
	{
		public static void Error(string msg) => Console.WriteLine("[ERROR]: "+msg);
		public static void Warning(string msg) => Console.WriteLine("[WARNING]: "+ msg);
		public static void Log(string msg) => Console.WriteLine("[INFO]: "+ msg);
	}
}
