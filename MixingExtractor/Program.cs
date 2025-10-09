// See https://aka.ms/new-console-template for more information
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using TraitFinderApp.Client.Model;
using TraitFinderApp.Model.Mixing;
using static System.Net.WebRequestMethods;

Console.WriteLine("Hello, World!");

var Http = new HttpClient();

Console.WriteLine("Current Path: " + System.Environment.ProcessPath);
string DataPath = System.IO.Path.GetFullPath("../../../../TraitFinderApp/wwwroot/data/");

Console.WriteLine("DataPath: " + DataPath);
DataImport.FetchOfflineData(DataPath);


GameSettingsInstance.ParseMixingSettingsCode("8Q1U5");
foreach (var mixing in GameSettingsInstance.AllMixingSettings)
{
	Console.WriteLine("Mixing: " + mixing.Name + " - " + mixing.GetLevel().Name);
}
Console.WriteLine();
Console.WriteLine();
GameSettingsInstance.ParseMixingSettingsCode("L72U5");
foreach (var mixing in GameSettingsInstance.AllMixingSettings)
{
	Console.WriteLine("Mixing: " + mixing.Name + " - " + mixing.GetLevel().Name);
}
return;
HashSet<string> possibleCombinations = IterateMixings();
StringBuilder sb = new();
foreach(var combination in possibleCombinations)
{
	sb.Append(combination);
	sb.Append(',');
}
Console.WriteLine("Found " + possibleCombinations.Count + " combinations.");
Console.WriteLine("Combinations: " + sb.ToString());




HashSet<string> IterateMixings()
{
	HashSet<string> possibleCombinations = new HashSet<string>();
	List<List<long>> sets = new();
	foreach (var mixing in GameSettingsInstance.AllMixingSettings)
	{
		sets.Add(mixing.Levels.Select(l => l.coordinate_value).ToList());		
	}
	var allCombinations = Cartesian(sets);
	foreach(var combination in allCombinations)
	{
		for(int i = 0; i < combination.Count; i++)
		{
			var mixing = GameSettingsInstance.AllMixingSettings[i];
			var setting = combination[i];
			mixing.SetLevel(setting);
		}
		possibleCombinations.Add(GameSettingsInstance.GetMixingSettingsCode());
	}
	return possibleCombinations;
}
static List<List<long>> Cartesian(List<List<long>> sets)
{
	List<List<long>> temp = new List<List<long>> { new List<long>() };
	for (int i = 0; i < sets.Count; i++)
	{
		List<List<long>> newTemp = new List<List<long>>();
		foreach (List<long> product in temp)
		{
			foreach (long element in sets[i])
			{
				List<long> tempCopy = new List<long>(product);
				tempCopy.Add(element);
				newTemp.Add(tempCopy);
			}
		}
		temp = newTemp;
	}
	return temp;
}


