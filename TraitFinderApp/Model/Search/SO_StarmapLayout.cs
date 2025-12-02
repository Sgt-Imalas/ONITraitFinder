using MudBlazor;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Numerics;
using TraitFinderApp.Client.Model;
using TraitFinderApp.Model.KleiClasses;
using TraitFinderApp.Model.KleiClasses.Util;
using TraitFinderApp.Model.Mixing;

namespace TraitFinderApp.Model.Search
{
	public class SO_StarmapLayout
	{
		public Dictionary<AxialI, string> OverridePlacements = new();
		public bool GenerationPossible => _generationPossible;
		private bool _generationPossible = false;
		public string FailedGenerationPlanetId => _failedGenerationPlanetId;
		private string _failedGenerationPlanetId = string.Empty;
		public ClusterLayout Origin { get; private set; }

		public Dictionary<string, string> MixingOverrides = [];

		public SO_StarmapLayout(ClusterLayout layout, int seed, string mixingCode)
		{
			AssignClusterLocations(layout, seed, mixingCode);
			var mixingResults = WorldGenMixing.DoWorldMixing(layout, seed, true, false);
			foreach(var result in mixingResults)
			{
				MixingOverrides[result.Key.world] = result.Value.WorldMixing;
			}
			foreach(var pos in OverridePlacements.Keys.ToArray())
			{
				var potentialMixingTarget = OverridePlacements[pos];
				if (MixingOverrides.TryGetValue(potentialMixingTarget, out var remixAsteroid))
				{
					OverridePlacements[pos] = remixAsteroid;
				}
			}
		}

		public bool AssignClusterLocations(ClusterLayout clusterLayout, int seed, string mixingCode = null)
		{
			Origin = clusterLayout;
			_failedGenerationPlanetId = string.Empty;
			//fields native to Cluster:
			var myRandom = new SeededRandom(seed);
			var worlds = new List<WorldPlacement>(clusterLayout.worldPlacements);
			int numRings = clusterLayout.numRings;
			//ProcGenGame.Cluster.AssignClusterLocations


			List<SpaceMapPOIPlacement> poiPlacements = clusterLayout.poiPlacements != null ? [.. clusterLayout.poiPlacements] : [];

			if(mixingCode!= null && mixingCode.Any())
			{
				foreach(var mixing in GameSettingsInstance.GetActiveMixingsFor(mixingCode))
				{
					if (mixing.IsDlcMixing() && !clusterLayout.RequiredDlcs.Contains(mixing.DlcFrom))
					{
						var additionalPois = mixing.GetPOIs_SO();
						if (additionalPois != null)
							poiPlacements.AddRange(additionalPois);
					}
				}
			}

			//currentWorld.SetClusterLocation(AxialI.ZERO);
			HashSet<AxialI> assignedLocations = [];
			HashSet<AxialI> worldForbiddenLocations = [];
			HashSet<AxialI> poiWorldAvoidance = [];
			int maxRadius = 2;
			for (int i = 0; i < worlds.Count; i++)
			{
				//WorldGen worldGen = new(worlds[i]);
				WorldPlacement worldPlacement = worlds[i];
				//DebugUtil.Assert(worldPlacement != null, "Somehow we're trying to generate a cluster with a world that isn't the cluster .yaml's world list!", worldGen.Settings.world.filePath);
				HashSet<AxialI> antiBuffer = [];
				foreach (AxialI item in assignedLocations)
				{
					antiBuffer.UnionWith(AxialUtil.GetRings(item, 1, worldPlacement.buffer));
				}

				List<AxialI> availableLocations = (from location in AxialUtil.GetRings(AxialI.ZERO, worldPlacement.allowedRings.min, Math.Min(worldPlacement.allowedRings.max, numRings - 1))
												   where !assignedLocations.Contains(location) && !worldForbiddenLocations.Contains(location) && !antiBuffer.Contains(location)
												   select location).ToList();
				if (availableLocations.Count > 0)
				{
					AxialI axialI = availableLocations[myRandom.RandomRange(0, availableLocations.Count)];
					OverridePlacements[axialI] = worldPlacement.world;
					assignedLocations.Add(axialI);
					worldForbiddenLocations.UnionWith(AxialUtil.GetRings(axialI, 1, worldPlacement.buffer));
					poiWorldAvoidance.UnionWith(AxialUtil.GetRings(axialI, 1, maxRadius));
					continue;
				}

				// DebugUtil.DevLogError("Could not find a spot in the cluster for " + worldPlacement.world + ". Check the placement settings in the custom cluster to ensure there are no conflicts.");
				HashSet<AxialI> minBuffers = [];
				foreach (AxialI item2 in assignedLocations)
				{
					minBuffers.UnionWith(AxialUtil.GetRings(item2, 1, 2));
				}

				availableLocations = (from location in AxialUtil.GetRings(AxialI.ZERO, worldPlacement.allowedRings.min, Math.Min(worldPlacement.allowedRings.max, numRings - 1))
									  where !assignedLocations.Contains(location) && !minBuffers.Contains(location)
									  select location).ToList();
				if (availableLocations.Count > 0)
				{
					AxialI axialI2 = availableLocations[myRandom.RandomRange(0, availableLocations.Count)];
					OverridePlacements[axialI2] = worldPlacement.world;
					assignedLocations.Add(axialI2);
					worldForbiddenLocations.UnionWith(AxialUtil.GetRings(axialI2, 1, worldPlacement.buffer));
					poiWorldAvoidance.UnionWith(AxialUtil.GetRings(axialI2, 1, maxRadius));
					continue;
				}

				string text = "Could not find a spot in the cluster for " + worldPlacement.world + " EVEN AFTER REDUCING BUFFERS. Check the placement settings in the custom cluster to ensure there are no conflicts.";
				DebugLogger.Error(text);
				_failedGenerationPlanetId = worldPlacement.world;
				return false;
			}

			if (poiPlacements != null)
			{
				HashSet<AxialI> poiClumpLocations = [];
				HashSet<AxialI> poiForbiddenLocations = [];

				float num = 0.5f;
				int num2 = 3;
				int num3 = 0;
				foreach (SpaceMapPOIPlacement poiPlacement in poiPlacements)
				{
					List<string> remainingPois = [.. poiPlacement.pois];
					for (int j = 0; j < poiPlacement.numToSpawn; j++)
					{
						bool num4 = myRandom.RandomRange(0f, 1f) <= num;
						List<AxialI>? axialIList = null;
						if (num4 && num3 < num2 && !poiPlacement.avoidClumping)
						{
							num3++;
							axialIList = (from location in AxialUtil.GetRings(AxialI.ZERO, poiPlacement.allowedRings.min, Math.Min(poiPlacement.allowedRings.max, numRings - 1))
										  where !assignedLocations.Contains(location) && poiClumpLocations.Contains(location) && !poiWorldAvoidance.Contains(location)
										  select location).ToList();
						}

						if (axialIList == null || axialIList.Count <= 0)
						{
							num3 = 0;
							poiClumpLocations.Clear();
							axialIList = (from location in AxialUtil.GetRings(AxialI.ZERO, poiPlacement.allowedRings.min, Math.Min(poiPlacement.allowedRings.max, numRings - 1))
										  where !assignedLocations.Contains(location) && !poiWorldAvoidance.Contains(location) && !poiForbiddenLocations.Contains(location)
										  select location).ToList();
						}

						if (poiPlacement.guarantee && (axialIList == null || axialIList.Count <= 0))
						{
							num3 = 0;
							poiClumpLocations.Clear();
							axialIList = (from location in AxialUtil.GetRings(AxialI.ZERO, poiPlacement.allowedRings.min, Math.Min(poiPlacement.allowedRings.max, numRings - 1))
										  where !assignedLocations.Contains(location) && !poiWorldAvoidance.Contains(location)
										  select location).ToList();
						}

						if (axialIList != null && axialIList.Count > 0)
						{
							AxialI axialI3 = axialIList[myRandom.RandomRange(0, axialIList.Count)];
							string text2 = remainingPois[myRandom.RandomRange(0, remainingPois.Count)];
							if (!poiPlacement.canSpawnDuplicates)
							{
								remainingPois.Remove(text2);
							}

							OverridePlacements[axialI3] = text2;
							poiForbiddenLocations.UnionWith(AxialUtil.GetRings(axialI3, 1, 3));
							poiClumpLocations.UnionWith(AxialUtil.GetRings(axialI3, 1, 1));
							assignedLocations.Add(axialI3);
						}
						else
						{
							Console.WriteLine(string.Format("WARNING: There is no room for a Space POI in ring range [{0}, {1}] with pois: {2}", poiPlacement.allowedRings.min, poiPlacement.allowedRings.max, string.Join("\n - ", poiPlacement.pois.ToArray())));
						}
					}
				}
			}
			return true;
		}

	}
}
