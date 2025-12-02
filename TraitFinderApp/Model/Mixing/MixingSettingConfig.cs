
using Newtonsoft.Json;
using TraitFinderApp.Client.Model;
using TraitFinderApp.Model.KleiClasses;
using TraitFinderApp.Model.KleiClasses.WorldGen;
using static MudBlazor.CategoryTypes;

namespace TraitFinderApp.Model.Mixing
{
	public class MixingSettingConfig
	{
		public string Id;
		public string Name;
		public string Description;
		public long coordinate_range;
		public string DlcIdFrom;
		public Dlc DlcFrom;
		public string Icon;
		public List<SettingLevel> Levels;
		public string[] MixingTags;
		public List<string> ForbiddenClusterTags;
		public string WorldMixing;
		public string SubworldMixing;
		public GameSettingType SettingType = GameSettingType.None;
		public List<SpaceMapPOIPlacement>? spacePois { get; set; }
		public List<SpaceDestinationMix>? spaceDesinations { get; set; }

		public SettingLevel CurrentLevel;
		public SettingLevel OnLevel, OffLevel, ThirdLevel; //mixings have either 2 or 3 settings levels
		public bool TwoLevels => OffLevel != null && ThirdLevel == null;

		public SettingLevel GetLevel()
		{
			return CurrentLevel;
		}
		public void SetLevel(long level)
		{
			foreach(var settingLevel in Levels)
			{
				if(settingLevel.coordinate_value == level)
				{
					CurrentLevel = settingLevel;
					return;
				}
			}
			Console.WriteLine("Could not set level "+level+ " on mixing setting "+Name+": no setting level exists with that coordinate value.");
		}
		public bool IsDlcMixing() => SettingType == GameSettingType.DLCMixing;	

		internal void InitBindings()
		{
			DlcFrom = Dlc.KeyValues[DlcIdFrom];
			if (Levels.Count == 3)
				ThirdLevel = Levels[1];
			OnLevel = Levels.Last();
			OffLevel = Levels.First();
			CurrentLevel = OffLevel;
		}
		public bool IsActive() => CurrentLevel != OffLevel;
		public bool IsGuaranteed() => CurrentLevel == OnLevel;
		public bool IsCurrentLevel(SettingLevel level) => CurrentLevel == level;

		public void ForceEnabledState(bool enabled) => CurrentLevel = (enabled ? OnLevel : OffLevel);

		public string GetIcon()
		{
			if (SettingType == GameSettingType.DLCMixing)
				return DlcFrom.Image;

			if (SettingType == GameSettingType.WorldMixing)
			{
				if (DataImport.SpacedOut?.asteroidsDict?.TryGetValue(WorldMixing, out var asteroid) ?? false)
				{
					return asteroid.Image;
				}
			}
			if (SettingType == GameSettingType.SubworldMixing)
			{
				return "./images/biomes/" + Icon + ".png";
			}

			return string.Empty;
		}

		internal Asteroid? GetMixingAsteroid()
		{
			if(SettingType == GameSettingType.WorldMixing)
			{
				if (DataImport.SpacedOut?.asteroidsDict?.TryGetValue(WorldMixing, out var asteroid) ?? false)
				{
					return asteroid;
				}
				Console.WriteLine("Could not find asteroid for world mixing: " + WorldMixing);
			}
			return null;
		}


		public List<SpaceMapPOIPlacement>? GetPOIs_SO()
		{
			if (!IsDlcMixing())
				return null;
			return spacePois;
		}
		public List<SpaceDestinationMix>? GetPOIs_BaseGame()
		{
			if(!IsDlcMixing()) return null;
			return spaceDesinations;
		}
	}
}
