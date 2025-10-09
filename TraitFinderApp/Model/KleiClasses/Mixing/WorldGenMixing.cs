using System.Diagnostics;
using TraitFinderApp.Client.Model;
using TraitFinderApp.Client.Model.KleiClasses;
using TraitFinderApp.Model.KleiClasses.Mixing;
using TraitFinderApp.Model.Mixing;

namespace TraitFinderApp.Model.KleiClasses
{
	public static class WorldGenMixing
	{
		static Dictionary<WorldPlacement, MixingSettingConfig> MixingResults = new();
		
		public static IOrderedEnumerable<T> StableSort<T>(this IEnumerable<T> enumerable)
		{
			return enumerable.OrderBy((T t) => t);
		}
		public static Dictionary<WorldPlacement, MixingSettingConfig> DoWorldMixing(ClusterLayout layout, int seed, bool isRunningWorldgenDebug, bool muteErrors)
		{
			return DoWorldMixingInternal(new(layout), seed, isRunningWorldgenDebug, muteErrors);
		}
		public static Dictionary<WorldPlacement, MixingSettingConfig> DoWorldMixingInternal(MutatedClusterLayout mutatedClusterLayout, int seed, bool isRunningWorldgenDebug, bool muteErrors)
		{
			MixingResults.Clear();

			List<WorldMixingOption> MixingOptionCandidates = new List<WorldMixingOption>();

			foreach (MixingSettingConfig worldMixingSettings in GameSettingsInstance.WorldMixingSettings)
			{
				if (!worldMixingSettings.IsActive())
					continue;

				if (!mutatedClusterLayout.layout.HasAnyTags(worldMixingSettings.ForbiddenClusterTags))
				{
					int minCount = worldMixingSettings.IsGuaranteed() ? 1 : 0;
					Console.WriteLine("Adding world mixing to pool: " + worldMixingSettings.Name);
					MixingOptionCandidates.Add(new WorldMixingOption
					{
						//worldgenPath = worldMixingSettings.world,
						mixingSettings = worldMixingSettings,
						minCount = minCount,
						maxCount = 1,
						cachedWorld = worldMixingSettings.GetMixingAsteroid()
					});
				}
			}


			KRandom rng = new KRandom(seed);
			//foreach (WorldPlacement worldPlacement in mutatedClusterLayout.layout.worldPlacements)
			//{
			//	worldPlacement.UndoWorldMixing();
			//}


			List<WorldPlacement> ShuffledAsteroids = [..mutatedClusterLayout.layout.worldPlacements];
			ShuffledAsteroids.ShuffleSeeded(rng);
			foreach (WorldPlacement item in ShuffledAsteroids)
			{
				if (!item.IsMixingPlacement())
				{
					continue;
				}

				MixingOptionCandidates.ShuffleSeeded(rng);
				WorldMixingOption worldMixingOption = FindWorldMixingOption(item, MixingOptionCandidates);
				if (worldMixingOption != null)
				{
					Console.WriteLine("Mixing: Applied world substitution " + item.world + " -> " + worldMixingOption.cachedWorld.Name);
					//item.worldMixing.previousWorld = item.world;
					//item.worldMixing.mixingWasApplied = true;
					//item.world = worldMixingOption.worldgenPath;
					MixingResults[item] = worldMixingOption.mixingSettings;
					worldMixingOption.Consume();
					if (worldMixingOption.IsExhausted)
					{
						MixingOptionCandidates.Remove(worldMixingOption);
					}
				}
			}

			if (!ValidateWorldMixingOptions(MixingOptionCandidates, isRunningWorldgenDebug, muteErrors))
			{
				Console.WriteLine("Mixing was invalid");
				return null;
			}
			Console.WriteLine("Mixing was successful: mixed "+ MixingResults.Count);
			return MixingResults;
		}
		public static WorldMixingOption FindWorldMixingOption(WorldPlacement worldPlacement, List<WorldMixingOption> options)
		{
			options = options.StableSort().ToList();
			foreach (WorldMixingOption option in options)
			{
				if (option.IsExhausted)
				{
					continue;
				}

				bool flag = true;
				foreach (string requiredTag in worldPlacement.worldMixing.requiredTags)
				{
					if (!option.cachedWorld.worldTags.Contains(requiredTag))
					{
						flag = false;
						break;
					}
				}

				foreach (string forbiddenTag in worldPlacement.worldMixing.forbiddenTags)
				{
					if (option.cachedWorld.worldTags.Contains(forbiddenTag))
					{
						flag = false;
						break;
					}
				}

				if (flag)
				{
					return option;
				}
			}

			return null;
		}

		public static List<Asteroid> GetValidMixingTargets(ClusterLayout layout, MixingSettingConfig mixingSettings)
		{
			List<Asteroid> list = new List<Asteroid>();
			var mixingAsteroid = mixingSettings.GetMixingAsteroid();
			if (mixingAsteroid == null)
			{
				return list;
			}

			foreach (WorldPlacement worldPlacement in layout.worldPlacements)
			{
				if (!worldPlacement.IsMixingPlacement())
				{
					continue;
				}
				bool validTarget = true;
				foreach (string requiredTag in worldPlacement.worldMixing.requiredTags)
				{
					if (!mixingAsteroid.worldTags.Contains(requiredTag))
					{
						validTarget = false;
						break;
					}
				}
				foreach (string forbiddenTag in worldPlacement.worldMixing.forbiddenTags)
				{
					if (mixingAsteroid.worldTags.Contains(forbiddenTag))
					{
						validTarget = false;
						break;
					}
				}
				if (validTarget && !list.Contains(worldPlacement.Asteroid))
				{
					list.Add(worldPlacement.Asteroid);
				}
			}
			return list;
		}

		public static bool ValidateWorldMixingOptions(List<WorldMixingOption> options, bool isRunningWorldgenDebug, bool muteErrors)
		{
			List<string> list = new List<string>();
			foreach (WorldMixingOption option in options)
			{
				if (!option.IsSatisfied)
				{
					list.Add($"{option.worldgenPath} ({option.minCount})");
				}
			}

			if (list.Count > 0)
			{
				if (muteErrors)
				{
					return false;
				}

				string text = "WorldgenMixing: Could not guarantee these world mixings: " + string.Join("\n - ", list);
				Console.WriteLine(text);
				return false;
			}

			return true;
		}

	}
}

