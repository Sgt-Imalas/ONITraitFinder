using System.Numerics;
using TraitFinderApp.Client.Model;

namespace TraitFinderApp.Model.Mixing
{
	/// <summary>
	/// Mixing settings store their values individually in their instance, each setting exists once
	/// </summary>
	public class GameSettingsInstance
	{
		/// <summary>
		/// Mirrors game list of mixing settings;
		/// ordering is relevant for coordinate calculations
		/// </summary>
		public static List<MixingSettingConfig> AllMixingSettings = new(0);

		public static List<MixingSettingConfig> DlcMixingSettings = new List<MixingSettingConfig>(4);
		public static List<MixingSettingConfig> WorldMixingSettings = new List<MixingSettingConfig>(4);
		public static List<MixingSettingConfig> SubworldMixingSettings = new List<MixingSettingConfig>(16);

		public static Dictionary<Dlc, MixingSettingConfig> DlcMixingSettingsDict = new Dictionary<Dlc, MixingSettingConfig>(4);

		public static bool TryGetDlcMixing(Dlc dlc, out MixingSettingConfig dlcSetting) => DlcMixingSettingsDict.TryGetValue(dlc, out dlcSetting);

		public static void SetMixingStateWhere(Func<MixingSettingConfig, bool> ConditionFulfilled, bool enabled) => DoForMixingsWhere(ConditionFulfilled, mixing => mixing.ForceEnabledState(enabled));

		public static void DoForMixingsWhere(Func<MixingSettingConfig, bool> ConditionFulfilled, Action<MixingSettingConfig> Action)
		{
			foreach (var mixing in AllMixingSettings)
			{
				if (ConditionFulfilled(mixing))
					Action(mixing);
			}
		}


		public static void InitMixingSettings()
		{
			foreach (var mixing in AllMixingSettings)
			{
				mixing.InitBindings();
				switch (mixing.SettingType)
				{
					case (GameSettingType.DLCMixing):
						mixing.DlcFrom = Dlc.KeyValues[mixing.Id];
						DlcMixingSettingsDict[mixing.DlcFrom] = mixing;
						DlcMixingSettings.Add(mixing);
						break;
					case (GameSettingType.WorldMixing):
						WorldMixingSettings.Add(mixing);
						break;
					case (GameSettingType.SubworldMixing):
						SubworldMixingSettings.Add(mixing);
						break;
				}
			}
		}


		public static bool ParseStorySettingsCode(string settingsCode)
		{
			return true;
		}
		public static bool ParseGameSettingsCode(string settingsCode)
		{
			return true;
		}

		public static string GetStorySettingsCode()
		{
			return "0";
		}
		public static string GetGameSettingsCode()
		{
			return "0";
		}

		public static List<MixingSettingConfig> GetActiveMixingsFor(string mixingsCode)
		{
			ParseMixingSettingsCode(mixingsCode);
			return AllMixingSettings.Where(mixing => mixing.IsActive()).ToList();
		}


		/// <summary>
		/// Mirrored from the game, parses a mixing code string to apply mixing settings
		/// </summary>
		/// <param name="mixingsCode"></param>
		/// <returns>bool: Is mixing string valid</returns>
		public static bool ParseMixingSettingsCode(string mixingsCode)
		{
			Console.WriteLine("Parsing Mixing Code: " + mixingsCode);
			BigInteger bigInteger = Base36toBinary(mixingsCode.ToUpperInvariant());

			for (int i = AllMixingSettings.Count - 1; i >= 0; i--)
			{
				var mixingSetting = AllMixingSettings[i];
				long level = (long)(bigInteger % (BigInteger)mixingSetting.coordinate_range);

				if (level > mixingSetting.Levels.Count)
				{
					Console.WriteLine("Invalid Mixing String: Level " + level + " higher than possible for " + mixingSetting.Name);
					return false;
				}

				bigInteger /= (BigInteger)mixingSetting.coordinate_range;
				mixingSetting.SetLevel(level);
			}
			if (bigInteger != 0)
			{
				Console.WriteLine("Invalid Mixing String: bigInteger not 0 after all settings extracted");
				return false;
			}
			Console.WriteLine("Mixing code parsed successful");
			return true;
		}
		public static string GetMixingSettingsCode()
		{
			BigInteger input = (BigInteger)0;
			foreach (MixingSettingConfig mixingSetting in AllMixingSettings)
			{
				input *= (BigInteger)mixingSetting.coordinate_range;
				input += (BigInteger)mixingSetting.GetLevel().coordinate_value;
			}
			return BinarytoBase36(input);
		}
		/// <summary>
		/// cloned from the game function that calculates this
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		private static string BinarytoBase36(BigInteger input)
		{
			if (input == 0L)
				return "0";
			BigInteger bigInteger = input;
			string result = "";
			while (bigInteger > 0L)
			{
				result += hexChars[(int)(bigInteger % (BigInteger)36)].ToString();
				bigInteger /= (BigInteger)36;
			}
			return result;
		}
		/// <summary>
		/// cloned from the game function that calculates this
		/// </summary>
		private static BigInteger Base36toBinary(string input)
		{
			if (input == "0")
				return 0;
			BigInteger output = 0;
			for (int index = input.Length - 1; index >= 0; --index)
				output = output * (BigInteger)36 + (BigInteger)(long)hexChars.IndexOf(input[index]);
			return output;
		}
		private static string hexChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	}
}
