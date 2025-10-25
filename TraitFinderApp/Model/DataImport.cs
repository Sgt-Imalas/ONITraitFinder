using System.Diagnostics;
using System;
using TraitFinderApp.Client.Model.KleiClasses;
using TraitFinderApp.Client.Model.Search;
using TraitFinderApp.Model.Search;
using static System.Net.WebRequestMethods;
using System.Text.Json;
using static MudBlazor.CategoryTypes;
using Newtonsoft.Json;
using OniStarmapGenerator.Model.Search;
using TraitFinderApp.Model.KleiClasses;
using OniStarmapGenerator.Model;
using static MudBlazor.Icons.Custom;
using static System.Runtime.InteropServices.JavaScript.JSType;
using TraitFinderApp.Model.Mixing;
using System.Text.RegularExpressions;
using System.ComponentModel.Design;

namespace TraitFinderApp.Client.Model
{
	public class Data
	{
		public Data() { }

		public List<ClusterLayout> clusters { get; set; }
		public List<Asteroid> asteroids { get; set; }
		public List<WorldTrait> worldTraits { get; set; }

		public Dictionary<string, ClusterLayout> clustersDict = new();
		public Dictionary<string, Asteroid> asteroidsDict = new();
		public Dictionary<string, WorldTrait> worldTraitsDict = new();

		private Dictionary<Asteroid, List<WorldTrait>>? _compatibleTraits = null;

		public bool MapGameData()
		{
			asteroidsDict = new();
			Console.WriteLine(asteroids.Count + " asteroids");
			foreach (var asteroid in asteroids)
			{
				asteroidsDict[asteroid.Id] = asteroid;
				asteroid.InitBindings(this);
			}
			Console.WriteLine(asteroids.Count + " asteroids initialized");

			clustersDict = new();
			Console.WriteLine(clusters.Count + " clusters");
			foreach (var cluster in clusters)
			{
				clustersDict[cluster.Id] = cluster;
				cluster.InitBindings(this);
			}
			Console.WriteLine(clusters.Count + " clusters initialized");
			worldTraitsDict = new();
			Console.WriteLine(worldTraits.Count + " worldTraits");
			foreach (var trait in worldTraits)
			{
				worldTraitsDict[trait.Id] = trait;
			}
			Console.WriteLine(worldTraitsDict.Count + " worldTraits initialized");
			CalculateTraitCompatibilities();
			return true;
		}

		public void CalculateTraitCompatibilities()
		{
			_compatibleTraits = new();
			foreach (var asteroid in asteroids)
			{
				var allTraits = new List<WorldTrait>(worldTraits);
				if (asteroid.DisableWorldTraits)
				{
					_compatibleTraits[asteroid] = new();
					continue;
				}

				List<string> ExclusiveWithTags = new List<string>();

				if (asteroid.TraitRules != null)
				{
					foreach (var rule in asteroid.TraitRules)
					{
						TagSet? requiredTags = (rule.requiredTags != null) ? new TagSet(rule.requiredTags) : null;
						TagSet? forbiddenTags = ((rule.forbiddenTags != null) ? new TagSet(rule.forbiddenTags) : null);

						allTraits.RemoveAll((WorldTrait trait) =>
							  (requiredTags != null && !trait.traitTagsSet.ContainsAll(requiredTags))
							|| (forbiddenTags != null && trait.traitTagsSet.ContainsOne(forbiddenTags))
							|| (rule.forbiddenTraits != null && rule.forbiddenTraits.Contains(trait.Id)));
					}
				}
				allTraits.RemoveAll((WorldTrait trait) => !trait.IsValid(asteroid, logErrors: true));
				_compatibleTraits[asteroid] = allTraits;
			}
		}

		public List<WorldTrait> GetCompatibleTraits(Asteroid asteroid)
		{
			if (_compatibleTraits == null)
				return new();
			return _compatibleTraits[asteroid];
		}
	}
	public class StarmapData
	{
		public Dictionary<string, VanillaStarmapLocation> Locations;
		public Dictionary<string, VanillaStarmapLocation> VanillaLocations;
		public Dictionary<Dlc, Dictionary<string, VanillaStarmapLocation>> AllDlcLocations = [];
		public void MapGameData()
		{
			Console.WriteLine("Mapping Starmap destinations");
			Locations.Remove("SaltDesertPlanet"); //not found in starmap gen, disable it
			var dlcRegex = new Regex("DLC.");
			foreach (var location in Locations)
			{
				string locationId = location.Key;
				if (dlcRegex.IsMatch(locationId))
				{
					var dlcId = dlcRegex.Match(locationId).Value;
					if(Dlc.TryGetDlc(dlcId, out var dlc))
					{
						Console.WriteLine(locationId + " is part of dlc: " + dlc.Name);
						if (!AllDlcLocations.TryGetValue(dlc, out var dlcLocations))
						{
							dlcLocations = new Dictionary<string, VanillaStarmapLocation>();
							AllDlcLocations[dlc] = dlcLocations;
						}
						dlcLocations.Add(locationId,location.Value);
					}
				}
			}

			VanillaLocations = new(Locations);
			foreach(var dlcLocationList in AllDlcLocations.Values)
			{
				foreach (var loc in dlcLocationList)
					VanillaLocations.Remove(loc.Key);
			}

		}

	}

	public class DataImport
	{
		public static HashSet<string> FailedSeeds = [];
		public const string FAILED_SEEDS_URL = "https://oni-search.stefanoltmann.de/failed-worldgens";

		public static void FetchOfflineData(string dataPath)
		{
			var basegamejson = System.IO.File.ReadAllText(Path.Combine(dataPath, "gamedata_base.json"));
			BaseGame = JsonConvert.DeserializeObject<Data>(basegamejson);
			BaseGame.MapGameData();
			var sojson = System.IO.File.ReadAllText(Path.Combine(dataPath, "gamedata_so.json"));
			SpacedOut = JsonConvert.DeserializeObject<Data>(sojson);
			SpacedOut.MapGameData();
			var starmapjson = System.IO.File.ReadAllText(Path.Combine(dataPath, "BasegameStarmapDestinations.json"));
			StarmapImport = JsonConvert.DeserializeObject<StarmapData>(starmapjson);
			StarmapImport.MapGameData();
			var mixingjson = System.IO.File.ReadAllText(Path.Combine(dataPath, "mixing_data.json"));
			GameSettingsInstance.AllMixingSettings = JsonConvert.DeserializeObject<List<MixingSettingConfig>>(mixingjson);
			GameSettingsInstance.InitMixingSettings();

		}

		public static async Task FetchFailedSeeds(HttpClient Http)
		{
			try
			{
				var failedSeedFetch = await Http.GetAsync(FAILED_SEEDS_URL);
				if (failedSeedFetch == null)
					return;

				var fetchedFailedSeedsString = await failedSeedFetch.Content.ReadAsStringAsync();
				if (fetchedFailedSeedsString == null)
					return;

				FailedSeeds = fetchedFailedSeedsString.Split('\n').ToHashSet();
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed to fetch failed seeds: " + e.Message);
			}
			//Console.WriteLine(fetchedFailedSeedsString);

		}
		public static async Task FetchBaseGameData(HttpClient Http)
		{
			var baseGameResponse = await Http.GetAsync("./data/gamedata_base.json");
			if (baseGameResponse == null)
				return;
			var basegamejson = await baseGameResponse.Content.ReadAsStringAsync();
			if (basegamejson == null)
				return;

			var basegame = JsonConvert.DeserializeObject<Data>(basegamejson);
			if (basegame == null)
				return;
			BaseGame = basegame;
			basegame.MapGameData();
		}
		public static async Task FetchSpacedOutData(HttpClient Http)
		{
			var soResponse = await Http.GetAsync("./data/gamedata_so.json");
			if (soResponse == null)
				return;
			var sojson = await soResponse.Content.ReadAsStringAsync();
			if (sojson == null)
				return;
			var so = JsonConvert.DeserializeObject<Data>(sojson);
			if (so == null)
				return;
			SpacedOut = so;
			so.MapGameData();
		}
		public static async Task FetchStarmapDestinationData(HttpClient Http)
		{
			var basegame_starmap_file = await Http.GetAsync("./data/BasegameStarmapDestinations.json");
			if (basegame_starmap_file == null)
				return;
			var basegame_starmap_json = await basegame_starmap_file.Content.ReadAsStringAsync();
			if (basegame_starmap_json == null)
				return;

			var basegame_starmap = JsonConvert.DeserializeObject<StarmapData>(basegame_starmap_json);
			if (basegame_starmap == null)
				return;
			StarmapImport = basegame_starmap;
			basegame_starmap.MapGameData();
		}

		public static async Task FetchMixingData(HttpClient Http)
		{
			var data_file = await Http.GetAsync("./data/mixing_data.json");
			if (data_file == null)
				return;
			var mixing_data_json = await data_file.Content.ReadAsStringAsync();
			if (mixing_data_json == null)
				return;

			var mixing_settings = JsonConvert.DeserializeObject<List<MixingSettingConfig>>(mixing_data_json);
			if (mixing_settings == null)
				return;
			GameSettingsInstance.AllMixingSettings = mixing_settings;
			GameSettingsInstance.InitMixingSettings();
		}

		public static StarmapData StarmapImport;
		public static List<VanillaStarmapLocation> GetVanillaStarmapLocations(List<Dlc> ActiveDlcs, List<Dlc> requiredDlcs)
		{
			var validLocations = StarmapImport.VanillaLocations.Values.ToList();
			foreach (var dlcLocationList in StarmapImport.AllDlcLocations)
			{
				var dlc = dlcLocationList.Key;
				if(ActiveDlcs.Contains(dlc) && !requiredDlcs.Contains(dlc))
					validLocations.AddRange(dlcLocationList.Value.Values);
			}

			return validLocations;
		}


		public static Data BaseGame;
		public static Data SpacedOut;

		internal static async Task FetchSeeds(SearchQuery searchQuery, int startSeed, int targetCount = 4, int seedRange = 2000)
		{
			var cluster = searchQuery.SelectedCluster;
			bool checkForStarmap = searchQuery.HasStarmapFilters();
			bool isBaseGame = searchQuery.ActiveDlcs.Contains(Dlc.BASEGAME);

			var asteroids = new Tuple<Asteroid, int>[cluster.worldPlacements.Count];
			var dlcs = new List<Dlc>(searchQuery.ActiveDlcs);
			dlcs.RemoveAll(e => cluster.RequiredDlcs.Contains(e));

			bool mixingQueryParams = searchQuery.HasMixingQueryParams();

			Dictionary<MixingSettingConfig, HashSet<Asteroid>> validMixingTargets = searchQuery.FragmentMixingTargets;

			List<QueryResult> results = new List<QueryResult>(targetCount);

			for (int i = 0; i < cluster.worldPlacements.Count; i++)
			{
				//0 seed always generates all asteroids with no traits
				int offsetIndex = (startSeed > 0) ? i : 0;

				asteroids[i] = (new(cluster.worldPlacements[i].Asteroid, offsetIndex));
			}
			int queryableRange = startSeed + seedRange;

			int asteroidCount = asteroids.Length;

			//check asteroids first that have forbidden traits
			var asteroidsSortedByFilterCount = asteroids.OrderByDescending(searchEntry => searchQuery.GetTotalQueryParams(searchEntry.Item1)).ToArray();

			Dictionary<Asteroid, AsteroidQuery> queryParams = new(searchQuery.AsteroidParams);

			Dictionary<Asteroid, List<WorldTrait>> TraitStorage = new(asteroidCount);

			List<SpaceDestination> destinations = new();


			while (startSeed < queryableRange && results.Count < targetCount)
			{
				int localSeed = 0;
				bool seedFailedQuery = false;

				string fullSeed = string.Concat(cluster.Prefix, "-", startSeed, "-0-0-0"); //doesnt check for mixed seeds; adjust if mixed seeds ever become a thing in this finder

				if (FailedSeeds.Contains(fullSeed)) //if worldgen failed during generation, its in this hashset
					seedFailedQuery = true;

				//Console.Write("Checking seed: "+startSeed);
				if (isBaseGame && !seedFailedQuery)
				{
					destinations = GenerateRandomDestinations(startSeed, dlcs);
					if (checkForStarmap)
					{
						HashSet<VanillaStarmapLocation> requiredStarmapLocations = new(searchQuery.RequiredStarmapLocations);
						for (int i = 0; i < destinations.Count; i++)
						{
							var destination = destinations[i];
							requiredStarmapLocations.Remove(destination.Type);
							if (!requiredStarmapLocations.Any())
								break;
						}
						if (requiredStarmapLocations.Any())
							seedFailedQuery = true;
					}

				}
				if (!seedFailedQuery)
				{
					for (int i = 0; i < asteroidCount; ++i)
					{
						var asteroidWithOffset = asteroidsSortedByFilterCount[i];
						var asteroid = asteroidWithOffset.Item1;
						localSeed = asteroidWithOffset.Item2;
						var traits = GetRandomTraits(startSeed + localSeed, asteroid);
						TraitStorage[asteroid] = traits;

						if (queryParams.TryGetValue(asteroid, out var asteroidParams))
						{
							bool hasProhibited = asteroidParams.Prohibit.Any();
							bool hasGuaranteed = asteroidParams.Guarantee.Any();
							if (
								 //asteroid had prohibited traits
								 hasProhibited && asteroidParams.Prohibit.Intersect(traits).Any()
								//not all guaranteed traits are in asteroid
								|| hasGuaranteed && asteroidParams.Guarantee.Except(traits).Any()
								)
							{

								seedFailedQuery = true;
								break;
							}
						}
					}
				}
				if (!seedFailedQuery) //some asteroids were canceled, checking next
				{
					var asteroidQueryResults = new List<QueryAsteroidResult>(asteroidCount);
					Dictionary<WorldPlacement, MixingSettingConfig>? mixingResults = null;

					if (!isBaseGame)
					{
						mixingResults = WorldGenMixing.DoWorldMixing(cluster, startSeed, true, false);

						if (mixingQueryParams)
						{
							foreach (var mixResult in mixingResults)
							{
								if (validMixingTargets.TryGetValue(mixResult.Value, out var validAsteroids))
								{
									if (!validAsteroids.Contains(mixResult.Key.Asteroid))
									{
										seedFailedQuery = true;
										break;
									}
								}
							}
						}
					}
					if (!seedFailedQuery)
					{
						foreach (var asteroidWithIndex in asteroids)
						{
							var asteroid = asteroidWithIndex.Item1;
							int offset = asteroidWithIndex.Item2;

							if (!isBaseGame && mixingResults != null && mixingResults.TryGetValue(cluster.worldPlacements[offset], out var mixingResult))
							{
								asteroid = mixingResult.GetMixingAsteroid();

								if (asteroid != null)
									asteroidQueryResults.Add(new QueryAsteroidResult(searchQuery, asteroid, GetRandomTraits(startSeed + offset, asteroid), true));
							}
							else
							{
								if (TraitStorage.TryGetValue(asteroid, out var traitResults))
									asteroidQueryResults.Add(new(searchQuery, asteroid, new(traitResults)));
							}

						}
						var queryResult = new QueryResult()
						{
							seed = startSeed,
							cluster = cluster,
							asteroidsWithTraits = asteroidQueryResults
						};
						if (isBaseGame)
						{
							int maxDistance = destinations.Select(x => x.distance).Max();


							var bands = new DistanceBand[maxDistance + 1];
							for (int i = 0; i < bands.Length; i++)
							{
								bands[i] = new(i);
							}


							foreach (var entry in destinations)
							{
								bands[entry.distance].Destinations.Add(entry.Type);
							}
							queryResult.distanceBands = bands.ToList();
						}
						results.Add(queryResult);
					}
				}
				++startSeed;
			}
			searchQuery.AddQueryResults(results, startSeed);
		}

		private static List<SpaceDestination> GenerateFixedDestinations()
		{
			return new List<SpaceDestination>()
			{
				new SpaceDestination(0, "CarbonaceousAsteroid", 0),
				new SpaceDestination(1, "CarbonaceousAsteroid", 0),
				new SpaceDestination(2, "MetallicAsteroid", 1),
				new SpaceDestination(3, "RockyAsteroid", 2),
				new SpaceDestination(4, "IcyDwarf", 3),
				new SpaceDestination(5, "OrganicDwarf", 4)
			};
		}

		public class spaceDestinations
		{
			public spaceDestinations() { }
			public spaceDestinations(string type, int minTier, int maxTier)
			{
				this.type = type;
				this.minTier = minTier;
				this.maxTier = maxTier;
			}

			public string type;
			public int minTier, maxTier;
		}

		private static List<SpaceDestination> GenerateRandomDestinations(int seed, List<Dlc> mixingDlcs)
		{
			var destinations = GenerateFixedDestinations();


			KRandom rng = new KRandom(seed);
			List<List<string>> stringListList = new List<List<string>>()
			{
			  new List<string>(),
			  new List<string>() { "OilyAsteriod" },
			  new List<string>() { "Satellite" },
			  new List<string>()
			  {
				"Satellite",
				"RockyAsteroid",
				"CarbonaceousAsteroid",
				"ForestPlanet"
			  },
			  new List<string>()
			  {
				"MetallicAsteroid",
				"RockyAsteroid",
				"CarbonaceousAsteroid",
				"SaltDwarf"
			  },
			  new List<string>()
			  {
				"MetallicAsteroid",
				"RockyAsteroid",
				"CarbonaceousAsteroid",
				"IcyDwarf",
				"OrganicDwarf"
			  },
			  new List<string>()
			  {
				"IcyDwarf",
				"OrganicDwarf",
				"DustyMoon",
				"ChlorinePlanet",
				"RedDwarf"
			  },
			  new List<string>()
			  {
				"DustyMoon",
				"TerraPlanet",
				"VolcanoPlanet"
			  },
			  new List<string>()
			  {
				"TerraPlanet",
				"GasGiant",
				"IceGiant",
				"RustPlanet"
			  },
			  new List<string>()
			  {
				"GasGiant",
				"IceGiant",
				"HeliumGiant"
			  },
			  new List<string>()
			  {
				"RustPlanet",
				"VolcanoPlanet",
				"RockyAsteroid",
				"TerraPlanet",
				"MetallicAsteroid"
			  },
			  new List<string>()
			  {
				"ShinyPlanet",
				"MetallicAsteroid",
				"RockyAsteroid"
			  },
			  new List<string>()
			  {
				"GoldAsteroid",
				"OrganicDwarf",
				"ForestPlanet",
				"ChlorinePlanet"
			  },
			  new List<string>()
			  {
				"IcyDwarf",
				"MetallicAsteroid",
				"DustyMoon",
				"VolcanoPlanet",
				"IceGiant"
			  },
			  new List<string>()
			  {
				"ShinyPlanet",
				"RedDwarf",
				"RockyAsteroid",
				"GasGiant"
			  },
			  new List<string>()
			  {
				"HeliumGiant",
				"ForestPlanet",
				"OilyAsteriod"
			  },
			  new List<string>()
			  {
				"GoldAsteroid",
				"SaltDwarf",
				"TerraPlanet",
				"VolcanoPlanet"
			  }
			};
			List<int> list = new List<int>();
			int num1 = 3;
			int minValue = 15;
			int maxValue = 25;
			for (int index1 = 0; index1 < stringListList.Count; ++index1)
			{
				if (stringListList[index1].Count != 0)
				{
					for (int index2 = 0; index2 < num1; ++index2)
						list.Add(index1);
				}
			}
			int nextId = destinations.Count;
			int num2 = rng.Next(minValue, maxValue);
			List<SpaceDestination> collection1 = new List<SpaceDestination>();
			for (int index3 = 0; index3 < num2; ++index3)
			{
				int index4 = rng.Next(0, list.Count - 1);
				int num3 = list[index4];
				list.RemoveAt(index4);
				List<string> stringList = stringListList[num3];
				string type = stringList[rng.Next(0, stringList.Count)];
				SpaceDestination spaceDestination = new SpaceDestination(GetNextID(), type, num3);
				collection1.Add(spaceDestination);

			}
			list.ShuffleSeeded(rng);
			List<SpaceDestination> collection2 = new List<SpaceDestination>();
			var mixingDestinations = new List<spaceDestinations>();
			Dlc.AddMixingDestinations(mixingDestinations, mixingDlcs);

			foreach (var spaceDesination in mixingDestinations)
			{
				bool flag = false;
				if (list.Count > 0)
				{
					for (int index = 0; index < list.Count; ++index)
					{
						int distance = list[index];
						if (distance >= spaceDesination.minTier && distance <= spaceDesination.maxTier)
						{
							SpaceDestination spaceDestination = new SpaceDestination(GetNextID(), spaceDesination.type, distance);
							collection2.Add(spaceDestination);
							list.RemoveAt(index);
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					for (int index = 0; index < collection1.Count; ++index)
					{
						SpaceDestination spaceDestination = collection1[index];
						if (spaceDestination.distance >= spaceDesination.minTier && spaceDestination.distance <= spaceDesination.maxTier)
						{
							collection1[index] = new SpaceDestination(GetNextID(), spaceDesination.type, spaceDestination.distance);


							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					Console.WriteLine("error while placing the mixing destination");
				}
			}
			destinations.AddRange(collection1);
			destinations.Add(new SpaceDestination(GetNextID(), "Earth", 4));
			destinations.Add(new SpaceDestination(GetNextID(), "Wormhole", stringListList.Count));
			destinations.AddRange(collection2);



			return destinations;

			int GetNextID() => nextId++;


		}

		public static List<WorldTrait> GetRandomTraits(int seed, Asteroid world)
		{
			if (world.DisableWorldTraits || world.TraitRules == null || seed == 0)
			{
				return new List<WorldTrait>();
			}
			var worldTraits = GetActive().worldTraitsDict;


			KRandom kRandom = new KRandom(seed);
			List<WorldTrait> allTraits = new List<WorldTrait>(worldTraits.Values);
			List<WorldTrait> result = new List<WorldTrait>();
			TagSet tagSet = new TagSet();
			var rule = world.GetConsolidatedTraitRule();

			if (rule.specificTraits != null)
			{
				foreach (string specificTrait in rule.specificTraits)
				{
					result.Add(worldTraits[specificTrait]);
				}
			}

			List<WorldTrait> allTraitsLocal = new List<WorldTrait>(allTraits);
			TagSet requiredTags = ((rule.requiredTags != null) ? new TagSet(rule.requiredTags) : null);
			TagSet forbiddenTags = ((rule.forbiddenTags != null) ? new TagSet(rule.forbiddenTags) : null);

			allTraitsLocal.RemoveAll((WorldTrait trait) =>
			(requiredTags != null && !trait.traitTagsSet.ContainsAll(requiredTags))
			|| (forbiddenTags != null && trait.traitTagsSet.ContainsOne(forbiddenTags))
			|| (rule.forbiddenTraits != null && rule.forbiddenTraits.Contains(trait.Id))
			|| !trait.IsValid(world, logErrors: true));

			int randomNumber = kRandom.Next(rule.min, Math.Max(rule.min, rule.max + 1));
			int count = result.Count;
			while (result.Count < count + randomNumber && allTraitsLocal.Count > 0)
			{
				int index = kRandom.Next(allTraitsLocal.Count);
				WorldTrait worldTrait = allTraitsLocal[index];
				bool flag = false;
				foreach (string exclusiveId in worldTrait.exclusiveWith)
				{
					if (result.Find((WorldTrait t) => t.Id == exclusiveId) != null)
					{
						flag = true;
						break;
					}
				}
				foreach (string exclusiveWithTag in worldTrait.exclusiveWithTags)
				{
					if (tagSet.Contains(exclusiveWithTag))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					result.Add(worldTrait);
					allTraits.Remove(worldTrait);
					foreach (string exclusiveWithTag2 in worldTrait.exclusiveWithTags)
					{
						tagSet.Add(exclusiveWithTag2);
					}
				}

				allTraitsLocal.RemoveAt(index);
			}

			if (result.Count != count + randomNumber)
			{
				Debug.Fail($"TraitRule on {world.Name} tried to generate {randomNumber} but only generated {result.Count - count}");
			}
			return result;
		}

		internal static void SetActiveVersion(Dlc version)
		{
			if (version == Dlc.SPACEDOUT)
			{
				active = SpacedOut;
			}
			else
			{
				active = BaseGame;
			}
		}
		static Data active = BaseGame;
		internal static Data GetActive()
		{
			return active;
		}
	}
}
